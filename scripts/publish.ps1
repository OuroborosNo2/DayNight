#Requires -Version 5.1
<#
.SYNOPSIS
  Build portable DayNight artifacts (lite / full / dir / src).
#>
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Artifacts = Join-Path $Root 'artifacts'

$LiteDir = Join-Path $Artifacts 'build-lite'
$FullDir = Join-Path $Artifacts 'build-full'
$DirDir = Join-Path $Artifacts 'build-dir'
$SrcDir = Join-Path $Artifacts 'build-src'

$ZipLite = Join-Path $Artifacts 'DayNight-lite.zip'
$ZipFull = Join-Path $Artifacts 'DayNight-full.zip'
$ZipDir = Join-Path $Artifacts 'DayNight-dir.zip'
$ZipSrc = Join-Path $Artifacts 'DayNight-src.zip'

Write-Host "Root: $Root"
Set-Location $Root

if (Test-Path $Artifacts) { Remove-Item -Recurse -Force $Artifacts }
New-Item -ItemType Directory -Force -Path $LiteDir, $FullDir, $DirDir, $SrcDir | Out-Null

$proj = '.\src\DayNight\DayNight.csproj'
$common = @(
    '-c', $Configuration,
    '-r', 'win-x64',
    '-p:DebugType=none',
    '-p:DebugSymbols=false'
)

Write-Host '==> lite (needs .NET 8)...'
dotnet publish $proj @common `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $LiteDir

Write-Host '==> full (offline single exe)...'
dotnet publish $proj @common `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $FullDir

Write-Host '==> dir (offline folder)...'
dotnet publish $proj @common `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $DirDir

foreach ($dir in @($LiteDir, $FullDir, $DirDir)) {
    Copy-Item (Join-Path $Root 'README.md') (Join-Path $dir 'README.md') -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $Root 'README.zh-CN.md') (Join-Path $dir 'README.zh-CN.md') -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $Root 'LICENSE') (Join-Path $dir 'LICENSE') -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $Root 'docs\WHICH-DOWNLOAD.md') (Join-Path $dir 'WHICH-DOWNLOAD.md') -Force -ErrorAction SilentlyContinue
}

Write-Host '==> src (source + lite binary)...'
& robocopy $Root $SrcDir /E /NFL /NDL /NJH /NJS /nc /ns /np `
    /XD .git artifacts .vs bin obj | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed $LASTEXITCODE" }

New-Item -ItemType Directory -Force -Path (Join-Path $SrcDir 'bin') | Out-Null
Copy-Item (Join-Path $LiteDir 'DayNight.exe') (Join-Path $SrcDir 'bin\DayNight.exe') -Force
Copy-Item (Join-Path $Root 'LICENSE') (Join-Path $SrcDir 'bin\LICENSE') -Force
Copy-Item (Join-Path $Root 'docs\WHICH-DOWNLOAD.md') (Join-Path $SrcDir 'bin\WHICH-DOWNLOAD.md') -Force -ErrorAction SilentlyContinue
@"
DayNight — lite binary (needs .NET 8 Desktop Runtime)

1) Install: https://dotnet.microsoft.com/download/dotnet/8.0
2) Run: bin\DayNight.exe
3) Config: bin\config.json (created on first launch)

No .NET? Download DayNight-full.zip instead.
"@ | Set-Content (Join-Path $SrcDir 'bin\README.txt') -Encoding UTF8

Write-Host '==> Zipping...'
Compress-Archive -Path (Join-Path $LiteDir '*') -DestinationPath $ZipLite -Force
Compress-Archive -Path (Join-Path $FullDir '*') -DestinationPath $ZipFull -Force
Compress-Archive -Path $DirDir -DestinationPath $ZipDir -Force
Compress-Archive -Path $SrcDir -DestinationPath $ZipSrc -Force

Write-Host ''
Write-Host 'Done. Most users: DayNight-lite.zip'
Get-ChildItem $Artifacts -File -Filter '*.zip' | ForEach-Object {
    [pscustomobject]@{ Name = $_.Name; MB = [math]::Round($_.Length / 1MB, 2) }
} | Format-Table -AutoSize
