using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FinalTest.Models;
using LibVLCSharp.Shared;

namespace FinalTest.Services
{
    /// <summary>
    /// 播放器控制器 — 基于 LibVLCSharp（替代原 Windows Media Player COM 方案）
    ///
    /// 为什么换 VLC：
    /// - WMP COM 依赖系统 DirectShow 解码器，不支持 .odd 等非标格式
    /// - ActiveX 的 uiMode/HWND/COM 封送在手动 WinForms 中极不稳定
    /// - VLC 内置 FFmpeg 解码库，原生支持 500+ 音视频格式（包括 .odd）
    /// - VLC 是纯 .NET 托管 API，没有 COM 互操作的脆弱性
    ///
    /// 设计思路（不变）：
    /// - 外观模式封装播放引擎
    /// - PlaylistManager 管理列表与导航
    /// - Timer 驱动进度更新
    /// - 事件通知 UI 层
    /// </summary>
    public class PlayerController : IDisposable
    {
        // ==================== 核心引擎 ====================
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;

        // ==================== 业务组件 ====================
        private readonly PlaylistManager _playlistManager;
        private PlayMode _playMode;

        // ==================== 定时器 ====================
        private readonly Timer _progressTimer;

        // ==================== 状态标志 ====================
        private bool _isChangingTrack;
        private bool _disposed;

        // ==================== 事件 ====================
        public event Action<MediaItem> TrackChanged;
        public event Action<string> PlayStateChanged;
        public event Action<double, double> ProgressUpdated;
        public event Action<PlayMode> PlayModeChanged;
        public event Action<string> MediaErrorOccurred;

        // ==================== 属性 ====================

        /// <summary>VLC 媒体播放器实例（供 VideoView 绑定）</summary>
        public MediaPlayer MediaPlayer => _mediaPlayer;

        public MediaItem CurrentTrack => _playlistManager.CurrentItem;

        public PlayMode CurrentPlayMode
        {
            get => _playMode;
            set { if (_playMode != value) { _playMode = value; PlayModeChanged?.Invoke(_playMode); } }
        }

        public bool IsPlaying => _mediaPlayer.IsPlaying;

        public double CurrentPosition =>
            _mediaPlayer.Time >= 0 ? TimeSpan.FromMilliseconds(_mediaPlayer.Time).TotalSeconds : 0;

        public double Duration =>
            _mediaPlayer.Length > 0 ? TimeSpan.FromMilliseconds(_mediaPlayer.Length).TotalSeconds : 0;

        public int Volume
        {
            get => _mediaPlayer.Volume;
            set => _mediaPlayer.Volume = Math.Max(0, Math.Min(100, value));
        }

        public int PlaylistCount => _playlistManager.Count;
        public int CurrentPlaylistIndex => _playlistManager.CurrentIndex;
        public IReadOnlyList<MediaItem> PlaylistItems => _playlistManager.Items;

        // ==================== 构造函数 ====================

        public PlayerController()
        {
            // 初始化 VLC 引擎（与操作系统匹配的架构）
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            _playlistManager = new PlaylistManager();
            _playMode = PlayMode.Sequential;

            // 进度定时器
            _progressTimer = new Timer { Interval = 500 };
            _progressTimer.Tick += ProgressTimer_Tick;

            // 注册 VLC 事件
            _mediaPlayer.Playing += (s, e) => { PlayStateChanged?.Invoke("Playing"); };
            _mediaPlayer.Paused += (s, e) => { _progressTimer.Stop(); PlayStateChanged?.Invoke("Paused"); };
            _mediaPlayer.Stopped += (s, e) => { _progressTimer.Stop(); PlayStateChanged?.Invoke("Stopped"); };
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
        }

        // ==================== 播放控制 ====================

        public void Play()
        {
            var item = _playlistManager.CurrentItem;
            if (item == null) return;

            _mediaPlayer.Volume = 100;
            var media = new Media(_libVLC, new Uri(item.FilePath));
            _mediaPlayer.Play(media);
            _progressTimer.Start();
        }

        public void Pause() { _mediaPlayer.Pause(); }
        public void Stop() { _mediaPlayer.Stop(); _progressTimer.Stop(); }

        public void Next()
        {
            int idx = _playlistManager.GetNextIndex(_playMode);
            if (idx < 0) { Stop(); return; }
            SwitchTo(idx);
        }

        public void Previous()
        {
            int idx = _playlistManager.GetPreviousIndex(_playMode);
            if (idx < 0) { Stop(); return; }
            SwitchTo(idx);
        }

        public void PlayAtIndex(int index)
        {
            if (index < 0 || index >= _playlistManager.Count) return;
            SwitchTo(index);
        }

        public void Seek(double seconds)
        {
            if (_mediaPlayer.Length > 0)
                _mediaPlayer.Time = (long)(seconds * 1000);
        }

        // ==================== 切歌逻辑 ====================

        private void SwitchTo(int index)
        {
            _isChangingTrack = true;
            _playlistManager.CurrentIndex = index;
            Play();
            _isChangingTrack = false;
            TrackChanged?.Invoke(_playlistManager.CurrentItem);
        }

        // ==================== VLC 事件 ====================

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            if (!_isChangingTrack)
            {
                int idx = _playlistManager.GetNextIndex(_playMode);
                if (idx < 0) { Stop(); PlayStateChanged?.Invoke("Stopped"); }
                else SwitchTo(idx);
            }
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            string failed = _playlistManager.CurrentItem?.Title ?? "未知文件";
            if (!_isChangingTrack && _playlistManager.Count > 0)
            {
                int idx = _playlistManager.GetNextIndex(_playMode);
                if (idx >= 0)
                {
                    SwitchTo(idx);
                    MediaErrorOccurred?.Invoke($"已跳过: {failed}（无法解码）");
                }
                else
                {
                    Stop();
                    PlayStateChanged?.Invoke("Stopped");
                    MediaErrorOccurred?.Invoke($"无法播放: {failed}，且列表已无下一首");
                }
            }
        }

        // ==================== 进度定时器 ====================

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer.IsPlaying && _mediaPlayer.Length > 0)
            {
                double cur = TimeSpan.FromMilliseconds(_mediaPlayer.Time).TotalSeconds;
                double total = TimeSpan.FromMilliseconds(_mediaPlayer.Length).TotalSeconds;
                ProgressUpdated?.Invoke(cur, total);
            }
        }

        // ==================== 播放列表操作(透传) ====================

        public void AddToPlaylist(string path) => _playlistManager.Add(path);
        public void AddRangeToPlaylist(string[] paths) => _playlistManager.AddRange(paths);
        public void RemoveFromPlaylist(int idx) => _playlistManager.RemoveAt(idx);

        public void ClearPlaylist()
        {
            Stop();
            _playlistManager.Clear();
            TrackChanged?.Invoke(null);
        }

        public PlaylistManager GetPlaylistManager() => _playlistManager;

        // ==================== 工具 ====================

        public static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                return "00:00";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        // ==================== 释放 ====================

        public void Dispose()
        {
            if (!_disposed)
            {
                _progressTimer?.Stop();
                _progressTimer?.Dispose();
                _mediaPlayer?.Dispose();
                _libVLC?.Dispose();
                _disposed = true;
            }
        }
    }
}
