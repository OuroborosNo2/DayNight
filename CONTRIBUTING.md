# Contributing

Thanks for helping improve DayNight.

## Principles

- Stay **portable** and dependency-light (no third-party NuGet unless strongly justified)
- Keep the UI bilingual: update both `locales/en.json` and `locales/zh-CN.json`
- Prefer small, reviewable PRs
- Do not commit personal `config.json`, logs, screenshots of private desktops, or credentials

## Development

```powershell
dotnet build .\DayNight.sln -c Release
dotnet run --project .\src\DayNight\DayNight.csproj
.\scripts\publish.ps1
```

## Commits

Use Conventional Commits where practical:

- `feat:` new feature
- `fix:` bug fix
- `docs:` documentation only
- `chore:` build / tooling

## Pull requests

1. Fork and branch from `main`
2. Describe *what* and *why*
3. Note how you tested (manual UI / publish smoke)

## Code of conduct

Be respectful. Harassment and personal attacks are not acceptable.
