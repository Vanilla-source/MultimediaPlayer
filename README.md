# 多媒体播放器 (Multimedia Player)

基于 **C# WinForms + Windows Media Player COM 组件** 开发的 Windows 桌面多媒体播放器，支持常见音视频格式的播放与管理。

---

## 目录

- [项目背景](#项目背景)
- [功能需求分析](#功能需求分析)
- [技术选型理由](#技术选型理由)
- [系统架构设计](#系统架构设计)
- [开发环境说明](#开发环境说明)
- [项目结构](#项目结构)
- [核心设计思路](#核心设计思路)
- [使用说明](#使用说明)
- [后续扩展方向](#后续扩展方向)

---

## 项目背景

在日常工作和学习中，用户经常需要快速播放本地音视频文件。市面上的播放器（如 VLC、PotPlayer）功能强大但较为复杂，部分场景下需要一款轻量、简洁、可定制的播放器。本项目作为 C# WinForms 桌面应用开发的实践项目，旨在构建一个功能实用、代码结构清晰的本地多媒体播放器。

**适用场景**：本地音视频文件播放、播放列表管理、轻量级媒体浏览。

---

## 功能需求分析

### 核心功能

| 模块 | 功能 | 说明 |
|------|------|------|
| **播放控制** | 播放/暂停、停止、上一首/下一首 | 工具栏按钮 + 菜单项 + 键盘快捷键（Space 暂停/播放） |
| **进度控制** | 进度条拖动跳转、实时时间显示 | 状态栏进度条支持点击定位，工具栏和状态栏双时间显示 |
| **播放列表** | 添加/移除/清空/双击播放 | 支持文件多选、文件夹批量导入、拖拽添加 |
| **播放模式** | 顺序播放、单曲循环、列表循环、随机播放 | 菜单栏切换，状态栏显示当前模式 |
| **视频控制** | 全屏切换、画面比例调整 | 右键菜单 + F11 快捷键 |
| **音量调节** | 工具栏滑块调节 | 范围 0-100，实时同步到 WMP |

### 支持的媒体格式

| 类型 | 扩展名 |
|------|--------|
| 音频 | `.mp3` `.wav` `.wma` `.flac` `.aac` `.ogg` `.m4a` |
| 视频 | `.mp4` `.avi` `.wmv` `.mov` `.mkv` `.flv` `.mpeg` `.mpg` `.webm` |

> **格式限制说明**：实际能否播放取决于 Windows 系统安装的解码器。对于 H.265/HEVC 等需要额外解码包的格式，WMP 可能无法播放，此时程序会自动跳过该文件并尝试下一首。

---

## 技术选型理由

### 技术栈

| 技术 | 版本 | 选型理由 |
|------|------|----------|
| **.NET Framework** | 4.7.2 | WinForms 最稳定的运行时，COM 互操作成熟，Windows 原生支持 |
| **C#** | 7.3 | 与 .NET Framework 4.7.2 匹配的语言版本，语法简洁 |
| **WinForms** | — | 快速开发桌面 GUI，可视化设计，控件丰富 |
| **Windows Media Player COM** | — | Windows 内置，无需额外安装，支持常见音视频格式 |

### 为什么不选其他方案？

| 候选方案 | 不选的理由 |
|----------|------------|
| **VLC.DotNet / LibVLCSharp** | 需要用户安装 VLC 或打包 VLC 运行时（~100MB+），安装包体积大 |
| **NAudio** | 纯音频库，不支持视频播放，无法满足项目需求 |
| **DirectShow** | API 复杂度高，开发周期长，适合专业级播放器而非本项目 |
| **MediaElement (WPF)** | 需要切换到 WPF 技术栈，不符合 WinForms 要求 |
| **.NET 8+ WinForms** | COM 组件兼容性不如 .NET Framework 稳定，需额外配置 |

### 关键设计决策

1. **外观模式封装 WMP**：`PlayerController` 将 WMP COM 的复杂 API 封装为简洁的播放控制接口，降低 UI 层与 COM 组件的耦合
2. **事件驱动 UI 更新**：Controller 通过事件通知 Form 状态变化，Form 只负责 UI 渲染，不直接操作 WMP
3. **播放模式策略**：`PlaylistManager` 使用 switch-case 根据 `PlayMode` 枚举计算下一首索引，清晰且易扩展

---

## 系统架构设计

### 分层架构

```
┌─────────────────────────────────────────────┐
│              表示层 (MainForm)                │
│  ┌───────────────────────────────────────┐  │
│  │ 菜单栏 │ 工具栏 │ 视频区 │ 播放列表 │ 状态栏 │  │
│  └───────────────────────────────────────┘  │
├─────────────────────────────────────────────┤
│          服务层 (Services)                    │
│  ┌──────────────────┐ ┌──────────────────┐  │
│  │ PlayerController │ │ PlaylistManager  │  │
│  │ (播放控制外观)     │ │ (播放列表管理)    │  │
│  │ 封装 WMP COM 组件 │ │ 增删改查+导航逻辑 │  │
│  └──────┬───────────┘ └──────────────────┘  │
│         │                                     │
│  ┌──────▼──────────────────────────────────┐  │
│  │  AxWindowsMediaPlayer (WMP ActiveX)     │  │
│  │  Windows 系统内置媒体播放引擎            │  │
│  └─────────────────────────────────────────┘  │
├─────────────────────────────────────────────┤
│           模型层 (Models)                      │
│  ┌──────────────────┐ ┌──────────────────┐  │
│  │   MediaItem      │ │   PlayMode       │  │
│  │ (媒体文件模型)     │ │ (播放模式枚举)    │  │
│  └──────────────────┘ └──────────────────┘  │
└─────────────────────────────────────────────┘
```

### 核心类职责

| 类 | 文件 | 职责 |
|----|------|------|
| `MainForm` | `MainForm.cs` | 主窗体 UI：菜单栏、工具栏、播放列表、状态栏、进度条，所有用户交互 |
| `PlayerController` | `Services/PlayerController.cs` | 播放器外观：封装 WMP ActiveX，管理播放状态，定时更新进度，事件通知 UI |
| `PlaylistManager` | `Services/PlaylistManager.cs` | 播放列表逻辑：增删改查，基于播放模式计算上一首/下一首索引 |
| `MediaItem` | `Models/MediaItem.cs` | 媒体文件数据模型：路径、标题、时长、格式判断 |
| `PlayMode` | `Models/PlayMode.cs` | 播放模式枚举：顺序/单曲循环/列表循环/随机 |

### 事件流

```
用户点击"下一首"
       │
       ▼
MainForm ──► PlayerController.Next()
                    │
                    ▼
            PlaylistManager.GetNextIndex(mode)  ← 根据播放模式计算索引
                    │
                    ▼
            PlayerController.Play()  ──► WMP 加载并播放新曲目
                    │
                    ▼
            TrackChanged 事件 ──► MainForm 更新 UI（曲目名、高亮列表项）
```

---

## 开发环境说明

| 项目 | 说明 |
|------|------|
| **IDE** | Visual Studio 2022 (v17.14+) |
| **语言** | C# 7.3 |
| **目标框架** | .NET Framework 4.7.2 |
| **操作系统** | Windows 10 / Windows 11 |
| **依赖** | Windows Media Player（Windows 内置，无需额外安装） |
| **版本控制** | Git |

### 运行要求

- Windows 10 或 Windows 11
- .NET Framework 4.7.2 Runtime（通常系统已自带）
- Windows Media Player 功能已启用

### 打开项目步骤

1. 克隆仓库到本地
2. 用 Visual Studio 2022 打开 `FinalTest.sln`
3. 确保 `FinalTest.csproj` 中的 COM 引用 `WMPLib` 正确解析（右键引用 → 检查 Windows Media Player 是否已注册）
4. 按 `F5` 编译运行

---

## 项目结构

```
FinalTest/
├── FinalTest.sln                  # Visual Studio 解决方案文件
├── FinalTest.csproj               # 项目文件 (含 WMP COM 引用)
├── App.config                     # 应用配置文件
├── Program.cs                     # 程序入口 (Main 方法)
│
├── MainForm.cs                    # 主窗体：所有 UI 控件 + 事件处理
│   └── ToolStripTrackBar          # 工具栏音量滑块包装类
│
├── Models/
│   ├── MediaItem.cs               # 媒体文件数据模型
│   └── PlayMode.cs                # 播放模式枚举
│
├── Services/
│   ├── PlayerController.cs        # 播放器控制器 (外观模式封装 WMP)
│   └── PlaylistManager.cs         # 播放列表管理器 (CRUD + 导航逻辑)
│
├── Properties/                    # 程序集信息、资源、设置 (VS 生成)
│   ├── AssemblyInfo.cs
│   ├── Resources.resx
│   └── Settings.settings
│
└── .gitignore                     # Git 忽略规则
```

---

## 核心设计思路

### 1. 外观模式封装 WMP（PlayerController）

`PlayerController` 采用**外观模式（Facade）**，将 Windows Media Player COM 对象的复杂 API 封装为 6 个简单方法：

- `Play()`, `Pause()`, `Stop()`, `Next()`, `Previous()`, `Seek()`

UI 层不需要知道 WMP 的 `Ctlcontrols`、`currentMedia`、`playState` 等内部细节，只需调用 Controller 的方法即可。具体实现见 `Services/PlayerController.cs` 中的详细注释。

### 2. 播放模式策略（PlaylistManager）

四种播放模式通过 `GetNextIndex(PlayMode mode)` 方法中的 switch-case 分支实现：

- **顺序播放**：`currentIndex + 1`，超出范围返回 `-1`（停止播放）
- **列表循环**：`(currentIndex + 1) % Count`，取模实现环形导航
- **单曲循环**：始终返回 `currentIndex` 不变
- **随机播放**：`Random.Next(Count)` 随机索引

上一首逻辑对称处理。具体实现见 `Services/PlaylistManager.cs` 中的详细注释。

### 3. 事件驱动 UI 更新

Controller 定义了 4 个事件，MainForm 订阅这些事件来更新 UI：

| 事件 | 触发时机 | UI 响应 |
|------|----------|---------|
| `TrackChanged` | 曲目切换 | 更新窗体标题和状态栏曲目名，高亮当前播放项 |
| `PlayStateChanged` | 播放/暂停/停止 | 切换播放按钮文字 |
| `ProgressUpdated` | 每 500ms | 更新进度条和双时间显示 |
| `PlayModeChanged` | 切换播放模式 | 更新菜单选中状态和状态栏模式标签 |

### 4. 进度条交互

状态栏的 `ToolStripProgressBar` 支持点击和拖动跳转：鼠标按下时设置 `_isSeeking = true` 暂停车自动更新；根据鼠标 X 坐标计算跳转比例；释放鼠标时执行 `Seek()` 并恢复自动更新。

### 5. 文件拖拽支持

视频面板和播放列表面板均支持从资源管理器拖入文件或文件夹。拖入文件夹时会递归搜索支持的媒体格式文件。

---

## 使用说明

### 基本操作

| 操作 | 方法 |
|------|------|
| 添加媒体文件 | 菜单「文件 → 打开文件」、工具栏、或从资源管理器拖拽文件到窗口 |
| 播放/暂停 | 工具栏 ▶/⏸ 按钮，或按 `Space` 键 |
| 停止 | 工具栏 ⏹ 按钮 |
| 上一首/下一首 | 工具栏 ⏮/⏭ 按钮，或 `Ctrl+←` / `Ctrl+→` |
| 跳转播放 | 双击播放列表中的曲目 |
| 音量调节 | 工具栏滑块拖动 |
| 进度跳转 | 状态栏进度条点击或拖动 |
| 全屏 | 右键视频区域 → 全屏，或按 `F11` |
| 退出全屏 | 按 `Esc` |

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Space` | 播放/暂停 |
| `Ctrl + →` | 下一首 |
| `Ctrl + ←` | 上一首 |
| `Ctrl + O` | 打开文件 |
| `Ctrl + D` | 添加文件夹 |
| `F11` | 全屏切换 |
| `Esc` | 退出全屏 |
| `Delete` | 删除播放列表中选中的项 |

---

## 后续扩展方向

以下功能在本次项目中未实现，可作为后续扩展：

- [ ] 均衡器（EQ）可视化调节
- [ ] 字幕文件加载与显示
- [ ] 播放历史记录
- [ ] 播放列表持久化（保存/加载 .m3u 文件）
- [ ] 换肤/主题切换（深色模式）
- [ ] 迷你模式（仅显示播放控制栏）
- [ ] 媒体信息查看（比特率、分辨率、编码格式等）
- [ ] 快捷键自定义

---

## 项目地址

```
https://github.com/Vanilla/MultimediaPlayer
```

---

## 许可证

MIT License
