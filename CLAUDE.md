# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

ZuAnBot（祖安助手）是一个 WPF 桌面应用程序，用于在英雄联盟游戏和客户端中快速发送预设词语。该项目使用 .NET Framework 4.7.2 和 Prism 框架，通过全局键盘钩子和模拟键盘输入实现功能。

## 核心架构

### 项目结构
- **ZuAnBot_Wpf**: 主应用程序（WPF 界面）
- **ZuAnBotUpdate**: 独立的更新程序，用于无缝升级主程序

### 技术栈
- **.NET Framework 4.7.2** (目标框架)
- **WPF** (UI 框架)
- **Prism.DryIoc** (MVVM 和依赖注入)
- **HandyControl** (UI 控件库)
- **WindowsInput** (键盘输入模拟)
- **RestSharp** (HTTP 客户端)
- **log4net** (日志记录)

### 关键组件

#### 1. 全局键盘钩子系统
- 文件: [ZuAnBot_Wpf/Helper/GlobalKeyboardHook.cs](ZuAnBot_Wpf/Helper/GlobalKeyboardHook.cs)
- 使用 Win32 API 实现全局键盘监听
- 监听快捷键: F2（默认词库）, F3（自定义词库）, F11（切换全员发送）, F12（切换逐字发送）

#### 2. 词库管理
- 文件: [ZuAnBot_Wpf/ViewModels/WordLibrary.cs](ZuAnBot_Wpf/ViewModels/WordLibrary.cs)
- 词库存储于本地 JSON 文件
- 首次运行时从嵌入资源 `wordsLibrary.json` 初始化
- 支持默认词库和自定义词库
- 词条内容限制: 非空且不超过 200 字符

#### 3. 输入模拟
- 位置: [ZuAnBot_Wpf/ViewModels/MainWindowViewModel.cs](ZuAnBot_Wpf/ViewModels/MainWindowViewModel.cs#L286-L341)
- 使用 WindowsInput 库模拟键盘输入
- 支持两种模式:
  - 整句发送: 一次性发送完整词条
  - 逐字发送: 每个字符单独发送（更安全，绕过屏蔽）

#### 4. 单实例控制
- 位置: [ZuAnBot_Wpf/App.xaml.cs](ZuAnBot_Wpf/App.xaml.cs#L43-L73)
- 使用 Mutex 确保只运行一个程序实例
- 当尝试启动第二个实例时，通过 IPC 激活已运行的实例

#### 5. 自动更新机制
- API 位置: [ZuAnBot_Wpf/Api/Apis.cs](ZuAnBot_Wpf/Api/Apis.cs)
- 从远程服务器检查最新版本
- 下载更新文件到临时目录
- 启动独立的 ZuAnBotUpdate.exe 完成更新（杀掉旧进程，覆盖文件，启动新进程）

#### 6. 环境配置
- 文件: [ZuAnBot_Wpf/Assets/Url.json](ZuAnBot_Wpf/Assets/Url.json)
- 支持多环境配置（sit/prod）
- API 地址可切换

## 常用开发命令

### 构建项目
```bash
# 在 Visual Studio 中直接构建
dotnet build ZuAnBot.sln --configuration Debug

# Release 构建
dotnet build ZuAnBot.sln --configuration Release
```

### 运行主程序
```bash
# 构建后的可执行文件位置
./ZuAnBot_Wpf/bin/Debug/net472/祖安助手.exe
# 或
./ZuAnBot_Wpf/bin/Release/net472/祖安助手.exe
```

### 打包发布
项目提供打包脚本 [ZuAnBot_Wpf/build.bat](ZuAnBot_Wpf/build.bat)（已从项目中排除但存在于文件系统）:
```bash
# 临时安装 Costura.Fody 用于打包
# 然后执行 Release 构建
# 最后卸载 Costura.Fody
cd ZuAnBot_Wpf
./build.bat
```

注意: build.bat 在 Visual Studio 中被设置为隐藏（见项目文件配置）

## 重要开发注意事项

### 平台配置
- 主程序目标平台: **x86**（见 ZuAnBot_Wpf.csproj:9）
- 原因: 使用了 32 位的 Win32 API 钩子

### 嵌入资源
主程序嵌入了以下资源:
- `wordsLibrary.json`: 默认词库
- `Url.json`: API 配置
- `log4net.config`: 日志配置
- `costura.zuanbotupdate.exe`: 更新程序（通过 Costura.Fody 嵌入）

### 全局异常处理
应用程序实现了三层异常捕获（见 [App.xaml.cs](ZuAnBot_Wpf/App.xaml.cs#L86-L111)）:
- DispatcherUnhandledException (UI 线程)
- TaskScheduler.UnobservedTaskException (Task 异常)
- AppDomain.CurrentDomain.UnhandledException (未处理异常)

所有异常通过 `ExceptionExtension.Show()` 方法展示给用户

### 本地数据存储
词库文件通过 `LocalConfigHelper.WordsLibraryPath` 指定路径存储

## MVVM 架构

项目使用 Prism 框架实现 MVVM:
- **Views**: MainWindow, WordsLibrarySet, WordEdit
- **ViewModels**: 对应的 ViewModel 类
- **Dialog 服务**: 通过 Prism 的 IDialogService 管理弹窗

Dialog 注册位置: [App.xaml.cs](ZuAnBot_Wpf/App.xaml.cs#L80-L84)

## API 通信

RestClient 配置:
- 超时: 120 秒
- JSON 序列化: Newtonsoft.Json
- 日期格式: "yyyy-MM-dd HH:mm:ss"
- 响应包装格式: `{ code, msg, data }`

主要 API 端点:
- `/api/auth/use`: 记录使用
- `/api/versions/latest`: 获取最新版本
- 文件下载: 直接通过 URL

## 调试技巧

- 开发模式下 ApiException 会被显示（通过 `#if DEBUG` 控制）
- 日志通过 log4net 记录
- 更新程序日志记录在系统临时目录
