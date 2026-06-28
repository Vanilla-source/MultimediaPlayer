# 多媒体播放器 — 项目方案文档

## 项目背景

本地音视频播放是桌面用户的刚需场景。市面主流播放器（VLC、PotPlayer）功能全面但配置复杂；部分小众格式（如 `.odd`）在系统自带播放器中无法解码。本项目旨在构建一款轻量级 Windows 桌面多媒体播放器，兼顾**常见格式的即开即播**与**非标格式的兼容能力**，同时作为 C# WinForms 桌面应用开发的完整实践案例。

## 功能需求分析

### 核心功能矩阵

| 模块 | 功能点 | 交互方式 |
|------|--------|----------|
| 播放控制 | 播放/暂停切换、上一首、下一首 | 大按钮（图标+文字）+ 键盘快捷键 |
| 进度控制 | 进度条点击跳转、实时时间显示 | 状态栏进度条 + 双处时间同步 |
| 播放列表 | 添加/移除/清空、双击切歌、拖拽导入 | ListView 多列 + 文件对话框 + 文件夹扫描 |
| 播放模式 | 顺序播放、单曲循环、列表循环、随机播放 | 菜单栏切换，状态栏显示当前模式 |
| 视频功能 | 全屏切换、画面比例调整 | 右键菜单 + F11 快捷键 |
| 音量控制 | 0-100 连续调节 | 控制栏滑块 |

### 支持格式

| 类型 | 扩展名 |
|------|--------|
| 音频 | `.mp3` `.wav` `.wma` `.flac` `.aac` `.ogg` `.m4a` `.odd` |
| 视频 | `.mp4` `.avi` `.wmv` `.mov` `.mkv` `.flv` `.mpeg` `.mpg` `.webm` |

### 界面布局

```
┌──────────────────────────────────────────────────────┐
│ MenuStrip:  文件  播放  模式  帮助                     │
├────────────────────────┬─────────────────────────────┤
│   视频 / 音频显示区     │      播放列表                 │
│       (55%)           │      (45%)                  │
│                       │                             │
│                       │                             │
├────────────────────────┤                             │
│  ⏮        ▶        ⏭  │  🔊 [=========]  00:00/00:00 │
│ 上一首  播放/暂停  下一首│                             │
├────────────────────────┴─────────────────────────────┤
│ 正在播放: xxx | ======进度条====== | 顺序播放          │
└──────────────────────────────────────────────────────┘
```

**音/视频自适应**：音频文件播放时，左侧自动切换为深色信息面板（曲目标题 + 实时时长），视频文件播放时显示画面。

### 快捷键

| 按键 | 功能 |
|------|------|
| `Space` | 播放/暂停 |
| `Ctrl + ← / →` | 上一首 / 下一首 |
| `Ctrl + O` | 打开文件 |
| `Ctrl + D` | 添加文件夹 |
| `F11` / `Esc` | 全屏切换 / 退出 |
| `Delete` | 删除列表中选中项 |

## 技术选型理由

### 技术栈

| 技术 | 版本 | 选型依据 |
|------|------|----------|
| .NET Framework | 4.7.2 | WinForms 最稳定运行时，Windows 原生支持 |
| C# | 7.3 | 模式匹配、元组、null 传播等特性 |
| WinForms | — | 快速 GUI 开发，可视化设计，控件丰富 |
| LibVLCSharp | 3.9.3 | 跨平台 VLC 绑定，内置 FFmpeg 解码库 |
| VideoLAN.LibVLC.Windows | 3.0.21 | VLC 原生二进制 |

### 关键决策：LibVLCSharp vs Windows Media Player COM

| 维度 | WMP ActiveX | LibVLCSharp |
|------|-------------|-------------|
| 格式支持 | 依赖系统解码器，`.odd` 等小众格式不支持 | FFmpeg 内置 500+ 编解码器 |
| 互操作 | COM 封送，需 ActiveX 句柄管理 | 纯 .NET 托管 API |
| 初始化 | 需 `CreateControl()`/`uiMode`/HWND 同步 | `new LibVLC()` 一行 |
| 稳定性 | 属性写入顺序敏感、全屏句柄重建崩溃 | 无 COM 相关副作用 |
| 分发 | 依赖系统 WMP 组件 | NuGet 自动拉取 VLC 运行时 |

**结论**：为满足 `.odd` 等非标格式的播放需求，且避免 COM ActiveX 在手动 WinForms 中的脆弱性，选用 LibVLCSharp。

## 系统架构设计

### 分层架构

```
┌─────────────────────────────────────────────────┐
│                  表示层 (MainForm)                │
│  MenuStrip │ ControlBar(3按钮) │ Video/Audio    │
│  Playlist │ StatusStrip(进度/时间/模式)           │
├─────────────────────────────────────────────────┤
│               服务层 (Services)                   │
│  ┌──────────────────┐  ┌──────────────────┐     │
│  │ PlayerController │  │ PlaylistManager  │     │
│  │ (Facade 封装 VLC) │  │ (列表 CRUD+导航)  │     │
│  └────────┬─────────┘  └──────────────────┘     │
│           │                                       │
│  ┌────────▼─────────────────────────────────┐    │
│  │  LibVLC + MediaPlayer (NuGet 托管库)     │    │
│  │  FFmpeg 解码 → 音视频渲染                │    │
│  └──────────────────────────────────────────┘    │
├─────────────────────────────────────────────────┤
│               模型层 (Models)                     │
│  MediaItem (媒体文件)  /  PlayMode (播放模式枚举)  │
└─────────────────────────────────────────────────┘
```

### 核心类职责

| 类 | 职责 | 设计模式 |
|----|------|----------|
| `MainForm` | 主窗体 UI：菜单、控制栏、视频/音频区、播放列表、状态栏、全部用户交互 | — |
| `PlayerController` | 封装 LibVLC API 为 6 个播放控制方法；管理进度定时器；通过事件通知 UI | Facade |
| `PlaylistManager` | 播放列表增删改查；基于播放模式计算上一首/下一首索引 | — |
| `MediaItem` | 媒体文件模型：路径、标题、时长、音/视频类型判断 | — |
| `PlayMode` | 顺序播放 / 单曲循环 / 列表循环 / 随机播放 | Enum |

### 事件流

```
用户点击"▶"按钮
    │
    ▼
MainForm.BtnPlay_Click()
    │
    ├─ 从未播过 → PlayerController.PlayAtIndex(0)
    └─ 已播放   → PlayerController.Pause()
                    │
                    ▼
            MediaPlayer.Pause()     ← 暂停/继续
            PlayStateChanged 事件   ← "Playing" / "Paused"
                    │
                    ▼
            MainForm.OnPlayStateChanged()
            → 按钮图标切换 ▶/⏸
```

```
曲目播放完毕 (EndReached)
    │
    ▼
PlayerController → PlaylistManager.GetNextIndex(mode)
    │
    ├─ 有下一首 → SwitchTo(index) → Play() → TrackChanged 事件 → UI 高亮
    └─ 无下一首 → Stop() → PlayStateChanged("Stopped")
```

## 开发环境说明

| 项目 | 说明 |
|------|------|
| IDE | Visual Studio 2022 (v17.14+) |
| 语言版本 | C# 7.3 |
| 目标框架 | .NET Framework 4.7.2 |
| 操作系统 | Windows 10 / 11 |
| 依赖库 | LibVLCSharp.WinForms 3.9.3 / VideoLAN.LibVLC.Windows 3.0.21 |
| 版本控制 | Git |

### 构建与运行

```bash
# 1. 克隆仓库
git clone https://github.com/Vanilla/MultimediaPlayer.git

# 2. Visual Studio 2022 打开 FinalTest.sln

# 3. 还原 NuGet 包：右键项目 → 管理 NuGet 包 → 还原

# 4. F5 运行
```

### 项目结构

```
FinalTest/
├── FinalTest.sln / FinalTest.csproj    # 解决方案与项目文件
├── Program.cs                          # 入口（Core.Initialize + MainForm）
├── MainForm.cs                         # 主窗体 (菜单/控制栏/播放列表/状态栏)
├── Models/
│   ├── MediaItem.cs                    # 媒体文件模型
│   └── PlayMode.cs                     # 播放模式枚举
├── Services/
│   ├── PlayerController.cs             # VLC 播放控制 (外观模式)
│   └── PlaylistManager.cs              # 播放列表管理
└── Properties/                         # 程序集信息/资源
```

## 项目地址

```
https://github.com/Vanilla/MultimediaPlayer
```
