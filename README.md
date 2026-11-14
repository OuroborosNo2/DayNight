# DayNight（昼夜）

[![Release](https://img.shields.io/github/v/release/OuroborosNo2/DayNight)](https://github.com/OuroborosNo2/DayNight/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Portable Windows utility that **switches light / dark mode on a schedule** (like a phone), so apps set to **Follow system** — IDEs, Agent Harness, and more — actually follow. Optional accent color, software contrast/gamma, and content-adaptive brightness.

[中文说明](README.zh-CN.md) · [Releases](https://github.com/OuroborosNo2/DayNight/releases) · [Issues](https://github.com/OuroborosNo2/DayNight/issues)

## Why

Windows does not auto-flip light/dark by time of day. DayNight fills that gap with a tiny tray app: schedule or one-click switch, portable config, no installer.

## Privacy

- No ads, no usage analytics, no network uploads
- Settings stay in a local `config.json` next to the exe
- Locales and icons are embedded in the binary

## Downloads

From [Releases](https://github.com/OuroborosNo2/DayNight/releases). **Not sure? Get `DayNight-lite.zip`.** Details: [docs/WHICH-DOWNLOAD.md](docs/WHICH-DOWNLOAD.md).

| Artifact | Approx. size | Notes |
|----------|--------------|-------|
| **`DayNight-lite.zip`** | **~0.5 MB** | **Recommended.** Needs [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| `DayNight-full.zip` | ~60 MB | Offline single `.exe` (no runtime install) |
| `DayNight-dir.zip` | ~60 MB | Offline folder |
| `DayNight-src.zip` | ~1 MB | Source + lite exe |

## Quick start

```powershell
Expand-Archive DayNight-lite.zip -DestinationPath DayNight
.\DayNight\DayNight.exe
```

Config is created as `config.json` beside the exe on first run.

## Features

- Schedule or manual light ↔ dark (apps + taskbar/Start)
- Optional Windows accent (Start / title bars; Win11 taskbar usually stays translucent)
- Software contrast / brightness / gamma
- Optional Windows “brightness based on content” (CABC)
- Tray single-instance, English / 简体中文

## Build

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet build .\DayNight.sln -c Release
.\scripts\publish.ps1
```

See [docs/BUILD.md](docs/BUILD.md) and [docs/FEATURES.md](docs/FEATURES.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Please open an issue before large changes.

## Security

See [SECURITY.md](SECURITY.md). Do not report vulnerabilities in public issues.

## License

[MIT](LICENSE) © DayNight Contributors
