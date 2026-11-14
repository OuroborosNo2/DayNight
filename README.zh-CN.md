# 昼夜（DayNight）

[![Release](https://img.shields.io/github/v/release/OuroborosNo2/DayNight)](https://github.com/OuroborosNo2/DayNight/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

便携 Windows 小工具：让电脑像手机一样**按时自动切换浅色 / 深色**，让 IDE、Agent Harness 等设为「跟随系统」的软件真正跟着变——白天清爽、夜晚更护眼。可选系统强调色、软件对比度/伽马，以及「根据内容调节亮度」。

[English](README.md) · [发布页](https://github.com/OuroborosNo2/DayNight/releases) · [Issues](https://github.com/OuroborosNo2/DayNight/issues)

## 为什么做

Windows 不会按时间自动切深浅色。昼夜用极小的托盘程序补上：日程或一键切换，便携配置，无需安装。

## 隐私

- 无广告、不收集使用数据、不联网上传
- 配置仅保存在 exe 旁的本地 `config.json`
- 语言包与图标打进 exe

## 下载

请到 [Releases](https://github.com/OuroborosNo2/DayNight/releases)。**不确定？下 `DayNight-lite.zip`。** 对照：[docs/WHICH-DOWNLOAD.md](docs/WHICH-DOWNLOAD.md)。

| 产物 | 大约体积 | 说明 |
|------|----------|------|
| **`DayNight-lite.zip`** | **约 0.5 MB** | **首选。** 需已安装 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0) |
| `DayNight-full.zip` | 约 60 MB | 离线单文件（无需另装运行时） |
| `DayNight-dir.zip` | 约 60 MB | 离线文件夹 |
| `DayNight-src.zip` | 约 1 MB | 源码 + lite 同款 exe |

## 快速开始

```powershell
Expand-Archive DayNight-lite.zip -DestinationPath DayNight
.\DayNight\DayNight.exe
```

首次运行会在 exe 旁生成 `config.json`。

## 功能

- 按时或手动切换浅色 ↔ 深色（应用 + 任务栏/开始菜单）
- 可选系统强调色（开始菜单/标题栏等；Win11 任务栏通常不会整条染色）
- 软件对比度 / 亮度 / 伽马
- 可选管理「根据内容调节亮度」
- 托盘单实例、中英文界面

## 构建

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```powershell
dotnet build .\DayNight.sln -c Release
.\scripts\publish.ps1
```

详见 [docs/BUILD.md](docs/BUILD.md)、[docs/FEATURES.md](docs/FEATURES.md)。

## 参与贡献

见 [CONTRIBUTING.md](CONTRIBUTING.md)。较大改动请先开 Issue。

## 安全

见 [SECURITY.md](SECURITY.md)。请勿在公开 Issue 中披露未修复的漏洞细节。

## 许可证

[MIT](LICENSE) © DayNight Contributors
