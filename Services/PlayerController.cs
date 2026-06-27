using System;
using System.Windows.Forms;
using FinalTest.Models;

namespace FinalTest.Services
{
    /// <summary>
    /// 播放器控制器
    /// 封装 Windows Media Player ActiveX 控件，提供统一的播放控制接口
    ///
    /// 设计思路：
    /// - 采用外观模式（Facade），将 WMP COM 控件的复杂接口封装为简单的播放控制方法
    /// - 通过 PlaylistManager 获取下一首/上一首，实现自动切歌
    /// - 使用 Timer 定时更新播放进度，驱动 UI 进度条刷新
    /// - 通过事件机制通知 UI 层状态变化（曲目切换、播放状态变化、进度更新）
    ///
    /// 核心实现逻辑：
    /// - PlayStateChange 事件监听 WMP 播放状态，当媒体播放完毕（MediaEnded）时自动切歌
    /// - 切歌逻辑根据 PlayMode 决定下一首索引，索引为 -1 则停止播放
    /// - 音量映射：WMP 音量范围为 0-100（整数），与 UI 滑块 0-100 直接对应
    /// </summary>
    public class PlayerController : IDisposable
    {
        // ==================== 私有字段 ====================

        /// <summary>
        /// Windows Media Player ActiveX 控件实例
        /// uiMode 设置为 "none" 以隐藏 WMP 自带 UI，使用自定义控件
        /// </summary>
        private readonly AxWMPLib.AxWindowsMediaPlayer _wmPlayer;

        /// <summary>
        /// 播放列表管理器引用
        /// </summary>
        private readonly PlaylistManager _playlistManager;

        /// <summary>
        /// 当前播放模式
        /// </summary>
        private PlayMode _playMode;

        /// <summary>
        /// 进度更新定时器
        /// 每 500ms 触发一次，驱动进度条和时间显示更新
        /// </summary>
        private readonly Timer _progressTimer;

        /// <summary>
        /// 标记是否正在执行切歌操作（防止递归触发）
        /// </summary>
        private bool _isChangingTrack;

        /// <summary>
        /// 标记资源是否已释放
        /// </summary>
        private bool _disposed;

        // ==================== 事件 ====================

        /// <summary>
        /// 曲目切换事件：当播放曲目发生变化时触发
        /// 参数为新的 MediaItem（可能为 null 表示无曲目）
        /// </summary>
        public event Action<MediaItem> TrackChanged;

        /// <summary>
        /// 播放状态变化事件：播放/暂停/停止时触发
        /// 参数为 WMP 播放状态枚举字符串
        /// </summary>
        public event Action<string> PlayStateChanged;

        /// <summary>
        /// 进度更新事件：由定时器触发，UI 层订阅以更新进度条
        /// 参数：Item1 = 当前秒数(double), Item2 = 总秒数(double)
        /// </summary>
        public event Action<double, double> ProgressUpdated;

        /// <summary>
        /// 播放模式变化事件
        /// </summary>
        public event Action<PlayMode> PlayModeChanged;

        // ==================== 属性 ====================

        /// <summary>
        /// 获取底层 WMP 控件引用（供 MainForm 添加到窗体控件树）
        /// </summary>
        public AxWMPLib.AxWindowsMediaPlayer WmPlayerControl => _wmPlayer;

        /// <summary>
        /// 获取当前播放的媒体项
        /// </summary>
        public MediaItem CurrentTrack => _playlistManager.CurrentItem;

        /// <summary>
        /// 获取或设置当前播放模式
        /// 设置时会触发 PlayModeChanged 事件
        /// </summary>
        public PlayMode CurrentPlayMode
        {
            get => _playMode;
            set
            {
                if (_playMode != value)
                {
                    _playMode = value;
                    PlayModeChanged?.Invoke(_playMode);
                }
            }
        }

        /// <summary>
        /// 判断播放器当前是否正在播放
        /// </summary>
        public bool IsPlaying => _wmPlayer.playState == WMPLib.WMPPlayState.wmppsPlaying;

        /// <summary>
        /// 获取当前播放进度（秒）
        /// </summary>
        public double CurrentPosition => _wmPlayer.Ctlcontrols.currentPosition;

        /// <summary>
        /// 获取当前媒体总时长（秒）
        /// </summary>
        public double Duration => _wmPlayer.currentMedia?.duration ?? 0;

        /// <summary>
        /// 获取或设置音量（0-100）
        /// </summary>
        public int Volume
        {
            get => _wmPlayer.settings.volume;
            set => _wmPlayer.settings.volume = Math.Max(0, Math.Min(100, value));
        }

        /// <summary>
        /// 获取当前播放列表项数
        /// </summary>
        public int PlaylistCount => _playlistManager.Count;

        /// <summary>
        /// 获取当前播放列表索引
        /// </summary>
        public int CurrentPlaylistIndex => _playlistManager.CurrentIndex;

        /// <summary>
        /// 获取只读播放列表
        /// </summary>
        public IReadOnlyList<MediaItem> PlaylistItems => _playlistManager.Items;

        // ==================== 构造函数 ====================

        /// <summary>
        /// 初始化播放器控制器
        /// 创建 WMP 控件、播放列表管理器、进度定时器，并注册事件
        /// </summary>
        public PlayerController()
        {
            // 创建 WMP ActiveX 控件
            _wmPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            _wmPlayer.Enabled = true;

            // 初始化播放列表管理器
            _playlistManager = new PlaylistManager();

            // 默认播放模式：顺序播放
            _playMode = PlayMode.Sequential;

            // 初始化进度定时器：每 500ms 更新一次
            _progressTimer = new Timer();
            _progressTimer.Interval = 500;
            _progressTimer.Tick += ProgressTimer_Tick;

            _isChangingTrack = false;

            // 注册 WMP 事件
            _wmPlayer.PlayStateChange += WmPlayer_PlayStateChange;
            _wmPlayer.MediaError += WmPlayer_MediaError;
        }

        // ==================== 播放控制方法 ====================

        /// <summary>
        /// 播放当前选中的媒体文件
        /// 实现逻辑：
        /// 1. 获取当前 MediaItem
        /// 2. 将文件 URL 设置给 WMP 控件
        /// 3. 调用 WMP 的 Ctlcontrols.play() 开始播放
        /// 4. 启动进度更新定时器
        /// </summary>
        public void Play()
        {
            var item = _playlistManager.CurrentItem;
            if (item == null) return;

            // WMP 使用 URL 属性加载媒体文件
            _wmPlayer.URL = item.FilePath;
            _wmPlayer.Ctlcontrols.play();

            // 启动进度更新定时器
            _progressTimer.Start();
        }

        /// <summary>
        /// 暂停/继续播放切换
        /// </summary>
        public void Pause()
        {
            if (_wmPlayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                _wmPlayer.Ctlcontrols.pause();
                _progressTimer.Stop();
            }
            else if (_wmPlayer.playState == WMPLib.WMPPlayState.wmppsPaused)
            {
                _wmPlayer.Ctlcontrols.play();
                _progressTimer.Start();
            }
        }

        /// <summary>
        /// 停止播放
        /// 停止后关闭进度更新定时器
        /// </summary>
        public void Stop()
        {
            _wmPlayer.Ctlcontrols.stop();
            _progressTimer.Stop();
        }

        /// <summary>
        /// 播放下一首
        /// 实现逻辑：
        /// 1. 通过 PlaylistManager 获取下一首索引
        /// 2. 若索引为 -1（顺序模式到末尾），停止播放
        /// 3. 否则切换当前索引并调用 Play()
        /// </summary>
        public void Next()
        {
            int nextIndex = _playlistManager.GetNextIndex(_playMode);
            if (nextIndex < 0)
            {
                // 顺序模式播完最后一首，停止
                Stop();
                return;
            }

            _isChangingTrack = true;
            _playlistManager.CurrentIndex = nextIndex;
            Play();
            _isChangingTrack = false;

            TrackChanged?.Invoke(_playlistManager.CurrentItem);
        }

        /// <summary>
        /// 播放上一首
        /// 逻辑与 Next() 对称
        /// </summary>
        public void Previous()
        {
            int prevIndex = _playlistManager.GetPreviousIndex(_playMode);
            if (prevIndex < 0)
            {
                Stop();
                return;
            }

            _isChangingTrack = true;
            _playlistManager.CurrentIndex = prevIndex;
            Play();
            _isChangingTrack = false;

            TrackChanged?.Invoke(_playlistManager.CurrentItem);
        }

        /// <summary>
        /// 跳转到播放列表中的指定索引并开始播放
        /// </summary>
        /// <param name="index">目标索引</param>
        public void PlayAtIndex(int index)
        {
            if (index < 0 || index >= _playlistManager.Count) return;

            _isChangingTrack = true;
            _playlistManager.CurrentIndex = index;
            Play();
            _isChangingTrack = false;

            TrackChanged?.Invoke(_playlistManager.CurrentItem);
        }

        /// <summary>
        /// 跳转到指定播放位置
        /// </summary>
        /// <param name="seconds">目标位置（秒）</param>
        public void Seek(double seconds)
        {
            if (_wmPlayer.currentMedia != null)
            {
                _wmPlayer.Ctlcontrols.currentPosition = seconds;
            }
        }

        // ==================== 播放列表操作方法 ====================

        /// <summary>
        /// 向播放列表添加文件
        /// </summary>
        public void AddToPlaylist(string filePath)
        {
            _playlistManager.Add(filePath);
        }

        /// <summary>
        /// 批量向播放列表添加文件
        /// </summary>
        public void AddRangeToPlaylist(string[] filePaths)
        {
            _playlistManager.AddRange(filePaths);
        }

        /// <summary>
        /// 从播放列表移除指定索引的项
        /// </summary>
        public void RemoveFromPlaylist(int index)
        {
            _playlistManager.RemoveAt(index);
        }

        /// <summary>
        /// 清空播放列表并停止播放
        /// </summary>
        public void ClearPlaylist()
        {
            Stop();
            _playlistManager.Clear();
            TrackChanged?.Invoke(null);
        }

        // ==================== 事件处理 ====================

        /// <summary>
        /// WMP 播放状态变化事件处理
        ///
        /// 核心逻辑：
        /// - MediaEnded（媒体播放完毕）：自动播放下一首
        ///   使用 _isChangingTrack 标志防止递归（Next() 方法中的 Play() 会再次触发此事件）
        /// - Playing（开始播放）：通知 UI 更新播放按钮状态
        /// - Stopped/Paused：更新定时器状态
        /// </summary>
        private void WmPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            var state = (WMPLib.WMPPlayState)e.newState;

            switch (state)
            {
                case WMPLib.WMPPlayState.wmppsMediaEnded:
                    // 媒体播放完毕，自动切换到下一首
                    if (!_isChangingTrack)
                    {
                        int nextIndex = _playlistManager.GetNextIndex(_playMode);
                        if (nextIndex < 0)
                        {
                            // 顺序模式播完停止
                            _progressTimer.Stop();
                            PlayStateChanged?.Invoke("Stopped");
                        }
                        else
                        {
                            _isChangingTrack = true;
                            _playlistManager.CurrentIndex = nextIndex;
                            Play(); // 递归触发 PlayStateChange
                            _isChangingTrack = false;
                            TrackChanged?.Invoke(_playlistManager.CurrentItem);
                        }
                    }
                    break;

                case WMPLib.WMPPlayState.wmppsPlaying:
                    // 播放中：更新媒体项时长信息
                    if (_wmPlayer.currentMedia != null)
                    {
                        var item = _playlistManager.CurrentItem;
                        if (item != null)
                        {
                            double dur = _wmPlayer.currentMedia.duration;
                            item.Duration = FormatTime(dur);
                        }
                    }
                    PlayStateChanged?.Invoke("Playing");
                    break;

                case WMPLib.WMPPlayState.wmppsPaused:
                    PlayStateChanged?.Invoke("Paused");
                    break;

                case WMPLib.WMPPlayState.wmppsStopped:
                    _progressTimer.Stop();
                    PlayStateChanged?.Invoke("Stopped");
                    break;

                case WMPLib.WMPPlayState.wmppsReady:
                    PlayStateChanged?.Invoke("Ready");
                    break;
            }
        }

        /// <summary>
        /// 媒体出错事件处理
        /// 当文件无法播放时，尝试跳到下一首
        /// </summary>
        private void WmPlayer_MediaError(object sender, AxWMPLib._WMPOCXEvents_MediaErrorEvent e)
        {
            // 文件出错时跳过当前曲目
            if (!_isChangingTrack && _playlistManager.Count > 0)
            {
                int nextIndex = _playlistManager.GetNextIndex(_playMode);
                if (nextIndex >= 0)
                {
                    _isChangingTrack = true;
                    _playlistManager.CurrentIndex = nextIndex;
                    Play();
                    _isChangingTrack = false;
                }
            }
        }

        /// <summary>
        /// 进度定时器 Tick 事件：定期上报当前播放进度
        /// </summary>
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_wmPlayer.currentMedia != null && IsPlaying)
            {
                double current = _wmPlayer.Ctlcontrols.currentPosition;
                double total = _wmPlayer.currentMedia.duration;
                ProgressUpdated?.Invoke(current, total);
            }
        }

        // ==================== 工具方法 ====================

        /// <summary>
        /// 将秒数格式化为 mm:ss 或 hh:mm:ss 字符串
        /// </summary>
        /// <param name="seconds">秒数</param>
        /// <returns>格式化时间字符串</returns>
        public static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                return "00:00";

            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)
                return ts.ToString(@"hh\:mm\:ss");
            else
                return ts.ToString(@"mm\:ss");
        }

        /// <summary>
        /// 获取播放列表管理器引用（供 UI 层直接操作列表）
        /// </summary>
        public PlaylistManager GetPlaylistManager()
        {
            return _playlistManager;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _progressTimer?.Stop();
                _progressTimer?.Dispose();

                // 停止 WMP 播放
                try { _wmPlayer?.Ctlcontrols.stop(); } catch { }
                try { _wmPlayer?.Dispose(); } catch { }

                _disposed = true;
            }
        }
    }
}
