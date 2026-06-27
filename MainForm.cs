using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FinalTest.Models;
using FinalTest.Services;

namespace FinalTest
{
    /// <summary>
    /// 多媒体播放器主窗体
    ///
    /// 界面布局（自上而下）：
    /// ┌──────────────────────────────────────────────────┐
    /// │ MenuStrip: 文件 | 播放 | 模式 | 帮助              │
    /// ├──────────────────────────────────────────────────┤
    /// │ ToolStrip: ▶ ⏸ ⏹ ⏮ ⏭ | 音量[=====] | 时间      │
    /// ├────────────────────────┬─────────────────────────┤
    /// │   视频显示区域          │   播放列表 (ListView)    │
    /// │   (WMP ActiveX 控件)    │   [添加] [移除] [清空]   │
    /// │                        │                         │
    /// ├────────────────────────┴─────────────────────────┤
    /// │ StatusStrip: 曲目名 | ======进度条====== | 模式  │
    /// └──────────────────────────────────────────────────┘
    ///
    /// 设计思路：
    /// - 使用 PlayerController 封装所有播放逻辑，MainForm 仅负责 UI 交互
    /// - 通过事件订阅实现 UI 与播放逻辑的解耦
    /// - 播放列表使用 ListView 的 Detail 模式，支持多列显示和拖拽添加
    /// - 快捷键支持增强操作效率
    /// </summary>
    public class MainForm : Form
    {
        // ==================== 核心组件 ====================

        /// <summary>
        /// 播放器控制器（业务逻辑层）
        /// </summary>
        private readonly PlayerController _playerController;

        // ==================== 菜单栏 ====================
        private MenuStrip _menuStrip;
        private ToolStripMenuItem _fileMenu;
        private ToolStripMenuItem _playMenu;
        private ToolStripMenuItem _modeMenu;
        private ToolStripMenuItem _helpMenu;

        // ==================== 工具栏 ====================
        private ToolStrip _toolStrip;
        private ToolStripButton _btnPlay;
        private ToolStripButton _btnPause;
        private ToolStripButton _btnStop;
        private ToolStripButton _btnPrevious;
        private ToolStripButton _btnNext;
        private ToolStripLabel _lblVolume;
        private ToolStripTrackBar _trackBarVolume;
        private ToolStripLabel _lblTimeDisplay;

        // ==================== 分割容器 ====================
        private SplitContainer _splitContainer;

        // ==================== 视频显示区域 ====================
        /// <summary>
        /// 视频面板：用于承载 Windows Media Player ActiveX 控件
        /// </summary>
        private Panel _videoPanel;

        // ==================== 音频显示区域 ====================
        /// <summary>
        /// 音频信息面板：播放纯音频时替代视频面板显示
        /// 包含大号曲目标题、音频图标、时长等信息
        /// </summary>
        private Panel _audioInfoPanel;
        private Label _lblAudioTitle;
        private Label _lblAudioIcon;
        private Label _lblAudioDuration;

        // ==================== 播放列表面板 ====================
        private Panel _playlistPanel;
        private ListView _listViewPlaylist;
        private ColumnHeader _colIndex;
        private ColumnHeader _colTitle;
        private ColumnHeader _colDuration;
        private Button _btnAddFiles;
        private Button _btnRemove;
        private Button _btnClear;
        private Label _lblPlaylist;

        // ==================== 底部状态栏 ====================
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblTrackName;
        private ToolStripProgressBar _progressBar;
        private ToolStripStatusLabel _lblTrackTime;
        private ToolStripStatusLabel _lblPlayMode;

        // ==================== 拖拽支持 ====================
        /// <summary>
        /// 标记是否正在拖动进度条（防止定时器覆盖用户操作）
        /// </summary>
        private bool _isSeeking;

        /// <summary>
        /// 标记窗体是否正在关闭（防止关闭时访问已释放资源）
        /// </summary>
        private bool _isClosing;

        // ==================== 构造函数 ====================

        public MainForm()
        {
            _playerController = new PlayerController();
            // InitializeComponent() 由 VS 设计器管理，仅包含窗体基本属性
            InitializeComponent();
            // InitializePlayerUI() 包含所有自定义控件创建，不受设计器影响
            InitializePlayerUI();
            RegisterEvents();
            SetupKeyboardShortcuts();
            // Form.Load 在窗体显示后触发，此时所有 HWND 已就绪，执行延迟初始化
            this.Load += MainForm_Load;
        }

        // ==================== 延迟初始化（Form.Load） ====================

        /// <summary>
        /// 窗体加载完成后执行延迟初始化
        /// 此时窗体已显示、所有控件 HWND 已创建，ActiveX COM 属性写入安全
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // 1. 根据实际窗体大小计算 SplitContainer 分割比例（构造时 ClientSize 不准确）
            _splitContainer.SplitterDistance = (int)(this.ClientSize.Width * 0.65);

            // 2. 初始化 WMP ActiveX 控件的 COM 属性（需要有效 HWND）
            var wmp = _playerController.WmPlayerControl;
            if (wmp != null)
            {
                wmp.uiMode = "none";          // 隐藏 WMP 自带 UI
                wmp.stretchToFit = true;      // 视频拉伸填充
                wmp.settings.volume = 80;     // 初始音量 80%
                wmp.settings.mute = false;    // 取消静音
            }
        }

        // ==================== UI 初始化 ====================

        /// <summary>
        /// 初始化所有 UI 控件
        ///
        /// 布局设计原则：
        /// - 菜单栏固定在顶部
        /// - 工具栏紧跟菜单栏
        /// - 中间区域使用 SplitContainer 分割为视频区和播放列表区（默认 65:35）
        /// - 底部状态栏显示播放状态和进度
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1831, 925);
            this.Name = "MainForm";
            this.ResumeLayout(false);

        }

        /// <summary>
        /// 初始化所有自定义播放器 UI 控件（不受 VS 设计器管理）
        ///
        /// 设计说明：
        /// - WinForms 设计器会覆盖 InitializeComponent() 的内容
        /// - 将所有自定义控件创建独立于此方法，避免被设计器清除
        /// - 此方法仅在运行时被构造函数调用
        ///
        /// 布局设计原则：
        /// - 菜单栏固定在顶部
        /// - 工具栏紧跟菜单栏
        /// - 中间区域使用 SplitContainer 分割为视频区和播放列表区（默认 65:35）
        /// - 底部状态栏显示播放状态和进度
        /// </summary>
        private void InitializePlayerUI()
        {
            // ========== 菜单栏 ==========
            _menuStrip = new MenuStrip();

            // 文件菜单
            _fileMenu = new ToolStripMenuItem("文件(&F)");
            _fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("打开文件(&O)...", (s, e) => OpenMediaFiles()),
                CreateMenuItem("添加文件夹(&D)...", (s, e) => AddFolder()),
                new ToolStripSeparator(),
                CreateMenuItem("清空播放列表(&C)", (s, e) => ClearPlaylist()),
                new ToolStripSeparator(),
                CreateMenuItem("退出(&X)", (s, e) => Close())
            });

            // 播放菜单
            _playMenu = new ToolStripMenuItem("播放(&P)");
            _playMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("播放/暂停", (s, e) => _playerController.Pause()),
                CreateMenuItem("停止(&S)", (s, e) => _playerController.Stop()),
                new ToolStripSeparator(),
                CreateMenuItem("上一首(&B)", (s, e) => _playerController.Previous()),
                CreateMenuItem("下一首(&N)", (s, e) => _playerController.Next()),
                new ToolStripSeparator(),
                CreateMenuItem("全屏(&F)", (s, e) => ToggleFullscreen())
            });

            // 模式菜单
            _modeMenu = new ToolStripMenuItem("模式(&M)");
            _modeMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateModeMenuItem("顺序播放", PlayMode.Sequential),
                CreateModeMenuItem("单曲循环", PlayMode.SingleLoop),
                CreateModeMenuItem("列表循环", PlayMode.ListLoop),
                CreateModeMenuItem("随机播放", PlayMode.Shuffle)
            });

            // 帮助菜单
            _helpMenu = new ToolStripMenuItem("帮助(&H)");
            _helpMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("关于(&A)...", (s, e) => ShowAbout())
            });

            _menuStrip.Items.AddRange(new ToolStripItem[] { _fileMenu, _playMenu, _modeMenu, _helpMenu });

            // ========== 工具栏 ==========
            _toolStrip = new ToolStrip();
            _toolStrip.GripStyle = ToolStripGripStyle.Hidden;

            _btnPlay = CreateToolButton("▶ 播放", (s, e) => PlayCurrent());
            _btnPause = CreateToolButton("⏸ 暂停", (s, e) => _playerController.Pause());
            _btnStop = CreateToolButton("⏹ 停止", (s, e) => _playerController.Stop());
            _btnPrevious = CreateToolButton("⏮ 上一首", (s, e) => _playerController.Previous());
            _btnNext = CreateToolButton("⏭ 下一首", (s, e) => _playerController.Next());

            _lblVolume = new ToolStripLabel("音量:");
            _trackBarVolume = new ToolStripTrackBar();
            _trackBarVolume.Minimum = 0;
            _trackBarVolume.Maximum = 100;
            _trackBarVolume.Value = 50;
            _trackBarVolume.TickFrequency = 10;
            _trackBarVolume.Size = new Size(120, 25);
            _trackBarVolume.Scroll += (s, e) => _playerController.Volume = _trackBarVolume.Value;

            _lblTimeDisplay = new ToolStripLabel("00:00 / 00:00");
            _lblTimeDisplay.Alignment = ToolStripItemAlignment.Right;

            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                _btnPlay, _btnPause, _btnStop, _btnPrevious, _btnNext,
                new ToolStripSeparator(),
                _lblVolume, _trackBarVolume,
                new ToolStripSeparator(),
                _lblTimeDisplay
            });

            // ========== 分割容器 ==========
            _splitContainer = new SplitContainer();
            _splitContainer.Dock = DockStyle.Fill;
            // SplitterDistance 在 Form.Load 中根据实际窗体大小计算

            // ---- 视频显示面板 ----
            _videoPanel = new Panel();
            _videoPanel.Dock = DockStyle.Fill;
            _videoPanel.BackColor = Color.Black;
            _videoPanel.AllowDrop = true;
            _videoPanel.DragEnter += VideoPanel_DragEnter;
            _videoPanel.DragDrop += VideoPanel_DragDrop;

            // WMP ActiveX 控件（或设计器占位标签）
            // 【关键】必须先 Add 到父容器（创建窗口句柄），再设置 COM 属性
            // ActiveX 控件需要有效的 HWND 才能响应 uiMode/stretchToFit 等属性写入
            var wmpControl = _playerController.WmPlayerControl;
            if (wmpControl != null)
            {
                // 步骤1：先设置 .NET 托管属性（不需要 HWND）
                wmpControl.Dock = DockStyle.Fill;
                wmpControl.ContextMenuStrip = CreateVideoContextMenu();

                // 步骤2：添加到父容器
                _videoPanel.Controls.Add(wmpControl);

                // 【重要】uiMode/stretchToFit/volume 等 COM 属性需要 HWND
                // 在构造阶段 HWND 未创建，一律推迟到 Form.Load 事件处理
            }
            else
            {
                var placeholder = new Label();
                placeholder.Text = "Windows Media Player 视频区域\n\n请生成项目 (Ctrl+Shift+B) 后\n重新打开窗体设计器";
                placeholder.Dock = DockStyle.Fill;
                placeholder.TextAlign = ContentAlignment.MiddleCenter;
                placeholder.ForeColor = Color.Gray;
                placeholder.Font = new Font("Microsoft YaHei", 11F);
                placeholder.BackColor = Color.Black;
                _videoPanel.Controls.Add(placeholder);
            }

            // ---- 播放列表面板 ----
            _playlistPanel = new Panel();
            _playlistPanel.Dock = DockStyle.Fill;
            _playlistPanel.AllowDrop = true;
            _playlistPanel.DragEnter += PlaylistPanel_DragEnter;
            _playlistPanel.DragDrop += PlaylistPanel_DragDrop;

            _lblPlaylist = new Label();
            _lblPlaylist.Text = "播放列表";
            _lblPlaylist.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            _lblPlaylist.Dock = DockStyle.Top;
            _lblPlaylist.Height = 25;
            _lblPlaylist.TextAlign = ContentAlignment.MiddleLeft;
            _lblPlaylist.Padding = new Padding(5, 0, 0, 0);

            _listViewPlaylist = new ListView();
            _listViewPlaylist.Dock = DockStyle.Fill;
            _listViewPlaylist.View = View.Details;
            _listViewPlaylist.FullRowSelect = true;
            _listViewPlaylist.MultiSelect = true;
            _listViewPlaylist.HideSelection = false;
            _listViewPlaylist.AllowDrop = true;
            _listViewPlaylist.DragEnter += PlaylistPanel_DragEnter;
            _listViewPlaylist.DragDrop += PlaylistPanel_DragDrop;
            _listViewPlaylist.DoubleClick += ListViewPlaylist_DoubleClick;
            _listViewPlaylist.KeyDown += ListViewPlaylist_KeyDown;

            _colIndex = new ColumnHeader();
            _colIndex.Text = "#";
            _colIndex.Width = 40;

            _colTitle = new ColumnHeader();
            _colTitle.Text = "标题";
            _colTitle.Width = 200;

            _colDuration = new ColumnHeader();
            _colDuration.Text = "时长";
            _colDuration.Width = 60;

            _listViewPlaylist.Columns.AddRange(new ColumnHeader[] { _colIndex, _colTitle, _colDuration });

            var buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 35;

            _btnAddFiles = new Button();
            _btnAddFiles.Text = "＋ 添加文件";
            _btnAddFiles.Size = new Size(90, 28);
            _btnAddFiles.Location = new Point(5, 4);
            _btnAddFiles.Click += (s, e) => OpenMediaFiles();

            _btnRemove = new Button();
            _btnRemove.Text = "－ 移除所选";
            _btnRemove.Size = new Size(90, 28);
            _btnRemove.Location = new Point(100, 4);
            _btnRemove.Click += (s, e) => RemoveSelectedItems();

            _btnClear = new Button();
            _btnClear.Text = "✕ 清空列表";
            _btnClear.Size = new Size(90, 28);
            _btnClear.Location = new Point(195, 4);
            _btnClear.Click += (s, e) => ClearPlaylist();

            buttonPanel.Controls.AddRange(new Control[] { _btnAddFiles, _btnRemove, _btnClear });
            _playlistPanel.Controls.AddRange(new Control[] { _lblPlaylist, _listViewPlaylist, buttonPanel });

            // ---- 音频信息面板（播放纯音频时显示，替代视频面板） ----
            _audioInfoPanel = new Panel();
            _audioInfoPanel.Dock = DockStyle.Fill;
            _audioInfoPanel.BackColor = Color.FromArgb(30, 30, 30);
            _audioInfoPanel.Visible = false; // 默认隐藏，等播放音频时再显示

            _lblAudioIcon = new Label();
            _lblAudioIcon.Text = "🎵";
            _lblAudioIcon.Font = new Font("Segoe UI Symbol", 72F, FontStyle.Regular);
            _lblAudioIcon.ForeColor = Color.DodgerBlue;
            _lblAudioIcon.TextAlign = ContentAlignment.MiddleCenter;
            _lblAudioIcon.Dock = DockStyle.Top;
            _lblAudioIcon.Height = 150;

            _lblAudioTitle = new Label();
            _lblAudioTitle.Text = "未在播放";
            _lblAudioTitle.Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold);
            _lblAudioTitle.ForeColor = Color.White;
            _lblAudioTitle.TextAlign = ContentAlignment.MiddleCenter;
            _lblAudioTitle.Dock = DockStyle.Top;
            _lblAudioTitle.Height = 50;

            _lblAudioDuration = new Label();
            _lblAudioDuration.Text = "";
            _lblAudioDuration.Font = new Font("Microsoft YaHei", 11F);
            _lblAudioDuration.ForeColor = Color.Gray;
            _lblAudioDuration.TextAlign = ContentAlignment.MiddleCenter;
            _lblAudioDuration.Dock = DockStyle.Fill;

            _audioInfoPanel.Controls.Add(_lblAudioDuration);
            _audioInfoPanel.Controls.Add(_lblAudioTitle);
            _audioInfoPanel.Controls.Add(_lblAudioIcon);

            // Panel1 中同时存在视频面板和音频面板，根据媒体类型切换可见性
            _splitContainer.Panel1.Controls.Add(_videoPanel);
            _splitContainer.Panel1.Controls.Add(_audioInfoPanel);
            _splitContainer.Panel2.Controls.Add(_playlistPanel);

            // ========== 状态栏 ==========
            _statusStrip = new StatusStrip();

            _lblTrackName = new ToolStripStatusLabel();
            _lblTrackName.Text = "就绪";
            _lblTrackName.Spring = true;
            _lblTrackName.TextAlign = ContentAlignment.MiddleLeft;

            _progressBar = new ToolStripProgressBar();
            _progressBar.Size = new Size(200, 16);
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 1000;
            _progressBar.MouseDown += ProgressBar_MouseDown;
            _progressBar.MouseUp += ProgressBar_MouseUp;
            _progressBar.MouseMove += ProgressBar_MouseMove;

            _lblTrackTime = new ToolStripStatusLabel();
            _lblTrackTime.Text = "00:00 / 00:00";

            _lblPlayMode = new ToolStripStatusLabel();
            _lblPlayMode.Text = "顺序播放";
            _lblPlayMode.BorderStyle = Border3DStyle.Etched;

            _statusStrip.Items.AddRange(new ToolStripItem[]
            {
                _lblTrackName, _progressBar, _lblTrackTime, _lblPlayMode
            });

            // ========== 组装主窗体控件 ==========
            Controls.Add(_splitContainer);
            Controls.Add(_toolStrip);
            Controls.Add(_menuStrip);
            Controls.Add(_statusStrip);
        }

        // ==================== UI 辅助方法 ====================

        /// <summary>
        /// 创建菜单项（快捷键统一通过 KeyDown 事件处理，避免 ShortcutKeys 属性的兼容性问题）
        /// </summary>
        private ToolStripMenuItem CreateMenuItem(string text, EventHandler onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += onClick;
            return item;
        }

        /// <summary>
        /// 创建播放模式菜单项（带选中标记）
        /// </summary>
        private ToolStripMenuItem CreateModeMenuItem(string text, PlayMode mode)
        {
            var item = new ToolStripMenuItem(text);
            item.Tag = mode;
            item.Click += ModeMenuItem_Click;
            // 默认顺序播放已选中
            if (mode == PlayMode.Sequential)
                item.Checked = true;
            return item;
        }

        /// <summary>
        /// 创建工具栏按钮
        /// </summary>
        private ToolStripButton CreateToolButton(string text, EventHandler onClick)
        {
            var btn = new ToolStripButton(text);
            btn.Click += onClick;
            btn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            return btn;
        }

        /// <summary>
        /// 创建视频区域右键菜单
        /// </summary>
        private ContextMenuStrip CreateVideoContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("全屏", null, (s, e) => ToggleFullscreen());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("画面比例 4:3", null, (s, e) =>
            {
                if (_playerController.WmPlayerControl != null)
                    _playerController.WmPlayerControl.stretchToFit = false;
            });
            menu.Items.Add("拉伸填充", null, (s, e) =>
            {
                if (_playerController.WmPlayerControl != null)
                    _playerController.WmPlayerControl.stretchToFit = true;
            });
            return menu;
        }

        // ==================== 事件注册 ====================

        /// <summary>
        /// 注册 PlayerController 的所有事件回调
        /// 实现 UI 层与逻辑层的解耦：Controller 发出事件，MainForm 更新 UI
        /// </summary>
        private void RegisterEvents()
        {
            // 曲目切换：更新状态栏曲目名，高亮当前播放项
            _playerController.TrackChanged += (item) =>
            {
                // 使用 BeginInvoke 确保 UI 更新在主线程执行
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => OnTrackChanged(item)));
                }
                else
                {
                    OnTrackChanged(item);
                }
            };

            // 播放状态变化：更新播放按钮状态
            _playerController.PlayStateChanged += (state) =>
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => OnPlayStateChanged(state)));
                }
                else
                {
                    OnPlayStateChanged(state);
                }
            };

            // 进度更新：更新进度条和时间标签
            _playerController.ProgressUpdated += (current, total) =>
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => OnProgressUpdated(current, total)));
                }
                else
                {
                    OnProgressUpdated(current, total);
                }
            };

            // 播放模式变化：更新菜单选中状态和状态栏
            _playerController.PlayModeChanged += (mode) =>
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => OnPlayModeChanged(mode)));
                }
                else
                {
                    OnPlayModeChanged(mode);
                }
            };
        }

        // ==================== 事件处理方法 ====================

        /// <summary>
        /// 曲目切换处理
        /// 1. 更新状态栏显示的曲目名称
        /// 2. 在播放列表中高亮当前播放项
        /// 3. 清空进度条
        /// </summary>
        private void OnTrackChanged(MediaItem item)
        {
            if (item != null)
            {
                _lblTrackName.Text = $"正在播放: {item.Title}";
                this.Text = $"多媒体播放器 - {item.Title}";

                // 在 ListView 中高亮当前项
                HighlightCurrentItem();

                // 根据媒体类型切换布局：视频 → 视频面板，音频 → 音频信息面板
                if (item.IsVideo)
                    SwitchToVideoMode();
                else
                    SwitchToAudioMode(item);
            }
            else
            {
                _lblTrackName.Text = "就绪";
                this.Text = "多媒体播放器";
                _progressBar.Value = 0;
                _lblTrackTime.Text = "00:00 / 00:00";

                // 无曲目时默认显示视频面板
                SwitchToVideoMode();
            }
        }

        /// <summary>
        /// 切换到音频模式：音频面板覆盖在视频面板上方（WMP 控件保持可见不中断播放）
        /// </summary>
        private void SwitchToAudioMode(MediaItem item)
        {
            // WMP 控件始终保持 Visible，仅将音频信息面板提到最上层遮挡视频区
            _audioInfoPanel.Visible = true;
            _audioInfoPanel.BringToFront();
            _lblAudioTitle.Text = item?.Title ?? "";
            _lblAudioDuration.Text = item?.Duration ?? "00:00";
            // 禁用全屏菜单项（音频无全屏需求）
            foreach (ToolStripItem item in _playMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Text.Contains("全屏"))
                    menuItem.Enabled = false;
            }
        }

        /// <summary>
        /// 切换到视频模式：视频面板提到最上层，隐藏音频面板
        /// </summary>
        private void SwitchToVideoMode()
        {
            _videoPanel.BringToFront();
            _audioInfoPanel.Visible = false;
            // 恢复全屏菜单项
            foreach (ToolStripItem item in _playMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Text.Contains("全屏"))
                    menuItem.Enabled = true;
            }
        }

        /// <summary>
        /// 播放状态变化处理
        /// 根据播放状态更新按钮文本和进度条状态
        /// </summary>
        private void OnPlayStateChanged(string state)
        {
            switch (state)
            {
                case "Playing":
                    _btnPlay.Text = "⏸ 暂停";
                    _btnPause.Text = "⏸ 暂停";
                    break;
                case "Paused":
                case "Stopped":
                    _btnPlay.Text = "▶ 播放";
                    _btnPause.Text = "⏸ 暂停";
                    break;
            }
        }

        /// <summary>
        /// 进度更新处理
        /// 更新状态栏进度条和时间标签，同步工具栏时间显示
        ///
        /// 注意：当用户手动拖动进度条时（_isSeeking == true），跳过更新以避免冲突
        /// </summary>
        private void OnProgressUpdated(double current, double total)
        {
            if (_isSeeking) return;

            string timeText = $"{PlayerController.FormatTime(current)} / {PlayerController.FormatTime(total)}";
            _lblTrackTime.Text = timeText;
            _lblTimeDisplay.Text = timeText;

            // 播放音频时同步更新音频面板上的时长
            if (_audioInfoPanel.Visible)
            {
                _lblAudioDuration.Text = timeText;
            }

            // 更新进度条（使用 1000 刻度精度）
            if (total > 0 && !double.IsNaN(total))
            {
                int progressValue = (int)(current / total * 1000);
                _progressBar.Value = Math.Min(progressValue, 1000);
            }
        }

        /// <summary>
        /// 播放模式变化处理
        /// 更新菜单项选中状态和状态栏模式显示
        /// </summary>
        private void OnPlayModeChanged(PlayMode mode)
        {
            // 更新菜单选中状态
            foreach (ToolStripItem item in _modeMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Tag is PlayMode itemMode)
                {
                    menuItem.Checked = (itemMode == mode);
                }
            }

            // 更新状态栏 — 使用反射获取 DescriptionAttribute 显示中文名称
            var field = typeof(PlayMode).GetField(mode.ToString());
            var descAttr = (System.ComponentModel.DescriptionAttribute)
                Attribute.GetCustomAttribute(field, typeof(System.ComponentModel.DescriptionAttribute));
            _lblPlayMode.Text = descAttr?.Description ?? mode.ToString();
        }

        /// <summary>
        /// 播放模式菜单项点击处理
        /// </summary>
        private void ModeMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is PlayMode mode)
            {
                _playerController.CurrentPlayMode = mode;
            }
        }

        // ==================== 核心功能方法 ====================

        /// <summary>
        /// 播放当前选中的曲目
        /// 如果当前未播放过，则从播放列表的第一首开始
        /// </summary>
        private void PlayCurrent()
        {
            if (_playerController.CurrentPlaylistIndex < 0 && _playerController.PlaylistCount > 0)
            {
                // 未选过曲目，从第一首开始
                _playerController.PlayAtIndex(0);
                RefreshPlaylistDisplay();
            }
            else
            {
                _playerController.Play();
            }
        }

        /// <summary>
        /// 打开媒体文件对话框，添加文件到播放列表
        ///
        /// 支持的格式：
        /// 音频 — *.mp3;*.wav;*.wma;*.flac;*.aac;*.ogg;*.m4a;*.odd
        /// 视频 — *.mp4;*.avi;*.wmv;*.mov;*.mkv;*.flv;*.mpeg;*.mpg;*.webm
        /// </summary>
        private void OpenMediaFiles()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "选择媒体文件";
                openFileDialog.Filter = "所有媒体文件|*.mp3;*.wav;*.wma;*.flac;*.aac;*.ogg;*.m4a;*.odd;*.mp4;*.avi;*.wmv;*.mov;*.mkv;*.flv;*.mpeg;*.mpg;*.webm|音频文件|*.mp3;*.wav;*.wma;*.flac;*.aac;*.ogg;*.m4a;*.odd|视频文件|*.mp4;*.avi;*.wmv;*.mov;*.mkv;*.flv;*.mpeg;*.mpg;*.webm|所有文件|*.*";
                openFileDialog.Multiselect = true;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _playerController.AddRangeToPlaylist(openFileDialog.FileNames);
                    RefreshPlaylistDisplay();
                }
            }
        }

        /// <summary>
        /// 添加整个文件夹中的媒体文件
        /// </summary>
        private void AddFolder()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "选择包含媒体文件的文件夹";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // 搜索常见媒体格式文件
                    string[] patterns = { "*.mp3", "*.wav", "*.wma", "*.flac", "*.aac", "*.ogg", "*.m4a", "*.odd",
                                          "*.mp4", "*.avi", "*.wmv", "*.mov", "*.mkv", "*.flv", "*.mpeg", "*.mpg", "*.webm" };

                    var files = new System.Collections.Generic.List<string>();
                    foreach (var pattern in patterns)
                    {
                        try
                        {
                            files.AddRange(Directory.GetFiles(folderDialog.SelectedPath, pattern, SearchOption.TopDirectoryOnly));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // 跳过无权限访问的文件
                        }
                    }

                    if (files.Count > 0)
                    {
                        _playerController.AddRangeToPlaylist(files.ToArray());
                        RefreshPlaylistDisplay();
                        MessageBox.Show($"已添加 {files.Count} 个媒体文件到播放列表。", "添加完成",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("所选文件夹中未找到支持的媒体文件。", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        /// <summary>
        /// 清空播放列表
        /// </summary>
        private void ClearPlaylist()
        {
            if (_playerController.PlaylistCount == 0) return;

            var result = MessageBox.Show("确定要清空播放列表吗？", "确认清空",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _playerController.ClearPlaylist();
                RefreshPlaylistDisplay();
                _lblTrackName.Text = "就绪";
                this.Text = "多媒体播放器";
            }
        }

        /// <summary>
        /// 移除播放列表中选中的项
        /// 支持多选删除，从后往前删防止索引偏移
        /// </summary>
        private void RemoveSelectedItems()
        {
            if (_listViewPlaylist.SelectedIndices.Count == 0) return;

            var indices = new System.Collections.Generic.List<int>();
            foreach (int index in _listViewPlaylist.SelectedIndices)
            {
                indices.Add(index);
            }
            // 从后往前删除，避免索引偏移影响
            indices.Sort((a, b) => b.CompareTo(a));

            foreach (int idx in indices)
            {
                _playerController.RemoveFromPlaylist(idx);
            }

            RefreshPlaylistDisplay();
        }

        /// <summary>
        /// 切换全屏模式
        /// 使用 WMP 自带的全屏功能
        /// </summary>
        private void ToggleFullscreen()
        {
            var wmp = _playerController.WmPlayerControl;
            if (wmp == null) return;

            try
            {
                wmp.fullScreen = !wmp.fullScreen;
            }
            catch
            {
                // 若 WMP 全屏不可用，手动模拟
                if (this.FormBorderStyle == FormBorderStyle.None)
                {
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Normal;
                    _menuStrip.Visible = true;
                    _toolStrip.Visible = true;
                    _statusStrip.Visible = true;
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                    _menuStrip.Visible = false;
                    _toolStrip.Visible = false;
                    _statusStrip.Visible = false;
                }
            }
        }

        /// <summary>
        /// 显示关于对话框
        /// </summary>
        private void ShowAbout()
        {
            MessageBox.Show(
                "多媒体播放器 v1.0\n\n" +
                "基于 C# WinForms + Windows Media Player COM 组件开发\n" +
                "支持常见音视频格式的播放与管理\n\n" +
                "开发环境：Visual Studio 2022\n" +
                "目标框架：.NET Framework 4.7.2",
                "关于多媒体播放器",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ==================== 播放列表 UI 刷新 ====================

        /// <summary>
        /// 刷新播放列表 ListView 显示
        ///
        /// 实现逻辑：
        /// 1. 清空 ListView 所有项
        /// 2. 从 PlaylistManager 获取最新的列表数据
        /// 3. 逐项添加到 ListView（序号、标题、时长）
        /// 4. 保持当前选中项高亮
        /// </summary>
        private void RefreshPlaylistDisplay()
        {
            _listViewPlaylist.Items.Clear();

            var items = _playerController.PlaylistItems;
            if (items == null) return;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var listViewItem = new ListViewItem((i + 1).ToString());
                listViewItem.SubItems.Add(item.Title);
                listViewItem.SubItems.Add(item.Duration ?? "00:00");
                listViewItem.Tag = i; // 存储索引便于后续操作

                // 高亮当前播放项
                if (i == _playerController.CurrentPlaylistIndex)
                {
                    listViewItem.BackColor = Color.LightSteelBlue;
                    listViewItem.Selected = true;
                    listViewItem.EnsureVisible();
                }

                _listViewPlaylist.Items.Add(listViewItem);
            }
        }

        /// <summary>
        /// 在 ListView 中高亮当前播放项
        /// </summary>
        private void HighlightCurrentItem()
        {
            int currentIdx = _playerController.CurrentPlaylistIndex;

            foreach (ListViewItem item in _listViewPlaylist.Items)
            {
                if (item.Tag is int idx && idx == currentIdx)
                {
                    item.BackColor = Color.LightSteelBlue;
                    item.Selected = true;
                    item.EnsureVisible();
                }
                else
                {
                    item.BackColor = SystemColors.Window;
                }
            }
        }

        // ==================== 键盘快捷键 ====================

        /// <summary>
        /// 设置全局键盘快捷键
        /// 全部快捷键通过 KeyDown 事件处理，避免 ToolStripMenuItem.ShortcutKeys
        /// 在手动创建菜单时的兼容性问题（如 Keys.Space 抛异常）。
        /// </summary>
        private void SetupKeyboardShortcuts()
        {
            this.KeyDown += (s, e) =>
            {
                // Ctrl 组合键
                if (e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.O:
                            e.SuppressKeyPress = true;
                            OpenMediaFiles();
                            break;
                        case Keys.D:
                            e.SuppressKeyPress = true;
                            AddFolder();
                            break;
                        case Keys.L:
                            e.SuppressKeyPress = true;
                            ClearPlaylist();
                            break;
                        case Keys.S:
                            e.SuppressKeyPress = true;
                            _playerController.Stop();
                            break;
                        case Keys.Left:
                            e.SuppressKeyPress = true;
                            _playerController.Previous();
                            break;
                        case Keys.Right:
                            e.SuppressKeyPress = true;
                            _playerController.Next();
                            break;
                    }
                    return;
                }

                // 单键
                switch (e.KeyCode)
                {
                    case Keys.Space:
                        e.SuppressKeyPress = true;
                        _playerController.Pause();
                        break;
                    case Keys.Escape:
                        if (this.FormBorderStyle == FormBorderStyle.None)
                        {
                            ToggleFullscreen();
                        }
                        break;
                    case Keys.F11:
                        e.SuppressKeyPress = true;
                        // 仅视频模式支持全屏
                        if (_videoPanel.Visible)
                            ToggleFullscreen();
                        break;
                    case Keys.F1:
                        e.SuppressKeyPress = true;
                        ShowAbout();
                        break;
                }
            };
        }

        // ==================== 拖拽支持 ====================

        /// <summary>
        /// 拖拽进入事件：检测是否为文件拖拽，设置拖拽效果
        /// </summary>
        private void VideoPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        /// <summary>
        /// 拖拽放置事件：将拖入的文件添加到播放列表
        /// </summary>
        private void VideoPanel_DragDrop(object sender, DragEventArgs e)
        {
            HandleFileDrop(e);
        }

        private void PlaylistPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void PlaylistPanel_DragDrop(object sender, DragEventArgs e)
        {
            HandleFileDrop(e);
        }

        /// <summary>
        /// 处理文件拖放：过滤媒体文件并添加到播放列表
        /// 同时支持递归添加文件夹（用户拖入文件夹时自动扫描）
        /// </summary>
        private void HandleFileDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var mediaFiles = new System.Collections.Generic.List<string>();

            foreach (var path in files)
            {
                if (Directory.Exists(path))
                {
                    // 递归搜索文件夹中的媒体文件
                    string[] patterns = { "*.mp3", "*.wav", "*.wma", "*.flac", "*.aac", "*.odd",
                                          "*.mp4", "*.avi", "*.wmv", "*.mov", "*.mkv", "*.flv", "*.mpeg", "*.webm" };
                    foreach (var pattern in patterns)
                    {
                        try
                        {
                            mediaFiles.AddRange(Directory.GetFiles(path, pattern, SearchOption.AllDirectories));
                        }
                        catch { /* 跳过不可访问的目录 */ }
                    }
                }
                else if (File.Exists(path))
                {
                    string ext = Path.GetExtension(path).ToLower();
                    // 简单扩展名过滤
                    if (ext == ".mp3" || ext == ".wav" || ext == ".wma" || ext == ".flac" ||
                        ext == ".aac" || ext == ".ogg" || ext == ".m4a" || ext == ".odd" ||
                        ext == ".mp4" || ext == ".avi" || ext == ".wmv" || ext == ".mov" ||
                        ext == ".mkv" || ext == ".flv" || ext == ".mpeg" || ext == ".mpg" || ext == ".webm")
                    {
                        mediaFiles.Add(path);
                    }
                }
            }

            if (mediaFiles.Count > 0)
            {
                _playerController.AddRangeToPlaylist(mediaFiles.ToArray());
                RefreshPlaylistDisplay();
            }
        }

        // ==================== ListView 事件 ====================

        /// <summary>
        /// 双击播放列表项：切换到该曲目并开始播放
        /// </summary>
        private void ListViewPlaylist_DoubleClick(object sender, EventArgs e)
        {
            if (_listViewPlaylist.SelectedItems.Count > 0)
            {
                int index = (int)_listViewPlaylist.SelectedItems[0].Tag;
                _playerController.PlayAtIndex(index);
                RefreshPlaylistDisplay();
            }
        }

        /// <summary>
        /// 播放列表键盘操作：Delete 键删除选中项
        /// </summary>
        private void ListViewPlaylist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveSelectedItems();
                e.Handled = true;
            }
        }

        // ==================== 进度条点击拖动支持 ====================

        /// <summary>
        /// 鼠标按下进度条：开始手动拖动跳转
        /// </summary>
        private void ProgressBar_MouseDown(object sender, MouseEventArgs e)
        {
            _isSeeking = true;
            SeekFromMouse(e.X);
        }

        /// <summary>
        /// 鼠标释放进度条：结束拖动，跳转到目标位置
        /// </summary>
        private void ProgressBar_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isSeeking)
            {
                SeekFromMouse(e.X);
                _isSeeking = false;
            }
        }

        /// <summary>
        /// 鼠标在进度条上移动：拖动中实时更新跳转位置
        /// </summary>
        private void ProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSeeking && e.Button == MouseButtons.Left)
            {
                SeekFromMouse(e.X);
            }
        }

        /// <summary>
        /// 根据鼠标 X 坐标计算目标进度位置并跳转
        ///
        /// 换算逻辑：
        /// - 鼠标 X 坐标 / 进度条总宽度 = 目标位置比例
        /// - 目标位置比例 × 媒体总时长 = 跳转秒数
        /// </summary>
        private void SeekFromMouse(int mouseX)
        {
            double total = _playerController.Duration;
            if (total <= 0) return;

            double ratio = (double)mouseX / _progressBar.Width;
            ratio = Math.Max(0, Math.Min(1, ratio)); // 钳制到 [0,1]

            double seekTime = ratio * total;
            _playerController.Seek(seekTime);

            // 即时更新进度条和时间显示
            _progressBar.Value = (int)(ratio * 1000);
            _lblTrackTime.Text = $"{PlayerController.FormatTime(seekTime)} / {PlayerController.FormatTime(total)}";
        }

        // ==================== 窗体事件 ====================

        /// <summary>
        /// 窗体关闭事件：确保播放器资源被正确释放
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            _playerController.Stop();
            _playerController.Dispose();
        }

        /// <summary>
        /// 重写 WndProc 处理系统消息（预留扩展点，如全局热键等）
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
        }
    }

    /// <summary>
    /// 工具栏 TrackBar 包装类
    /// 使得 TrackBar 可以作为 ToolStrip 子项嵌入工具栏
    /// </summary>
    public class ToolStripTrackBar : ToolStripControlHost
    {
        public ToolStripTrackBar() : base(new TrackBar())
        {
            TrackBarControl.Height = 25;
            TrackBarControl.TickStyle = TickStyle.None;
        }

        public TrackBar TrackBarControl => (TrackBar)Control;

        public int Minimum
        {
            get => TrackBarControl.Minimum;
            set => TrackBarControl.Minimum = value;
        }

        public int Maximum
        {
            get => TrackBarControl.Maximum;
            set => TrackBarControl.Maximum = value;
        }

        public int Value
        {
            get => TrackBarControl.Value;
            set => TrackBarControl.Value = value;
        }

        public int TickFrequency
        {
            get => TrackBarControl.TickFrequency;
            set => TrackBarControl.TickFrequency = value;
        }

        public event EventHandler Scroll
        {
            add => TrackBarControl.Scroll += value;
            remove => TrackBarControl.Scroll -= value;
        }
    }
}
