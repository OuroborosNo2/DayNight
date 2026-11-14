# Build & publish

最后编辑时间：2026-11-14 23:25

## Prerequisites

- Windows 10/11 x64
- .NET 8 SDK

## Run (dev)

```powershell
dotnet run --project .\src\DayNight\DayNight.csproj
```

## Release packages

```powershell
.\scripts\publish.ps1
```

| Zip | Meaning |
|-----|---------|
| `DayNight-lite.zip` | Framework-dependent; needs .NET 8 Desktop Runtime |
| `DayNight-full.zip` | Self-contained single file |
| `DayNight-dir.zip` | Self-contained folder |
| `DayNight-src.zip` | Source + lite binary |

Chooser: [WHICH-DOWNLOAD.md](WHICH-DOWNLOAD.md).
