<#
.SYNOPSIS
    Builds a Velopack installer for DacPac.UI.

.DESCRIPTION
    Publishes DacPac.UI as a self-contained app for the given runtime and
    packs it with Velopack (vpk). Output goes to .\releases (installer/
    AppImage, full package, and delta packages against previous releases).

    Supports win-x64 (produces a Setup.exe) and linux-x64 (produces an
    .AppImage). vpk must be run on the target OS, so building linux-x64
    requires running this script on a Linux machine (e.g. an ubuntu-latest
    CI runner).

    Previous releases are first downloaded from GitHub Releases so vpk can
    build delta updates against them. With -UploadToGitHub the new release is
    uploaded (as a draft) to GitHub Releases, which is the feed the app's
    auto-updater checks. Each runtime publishes to its own Velopack channel
    (win / linux by default), so both platforms can share the same GitHub
    release without colliding.

    Requires the vpk global tool:  dotnet tool install -g vpk

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.1

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.1 -Title "DacPac Viewer"

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.1 -Runtime linux-x64

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.1 -UploadToGitHub -GitHubToken $env:GITHUB_TOKEN
#>
param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64",
    [string]$Title = "DacPac Viewer",
    [switch]$UploadToGitHub,
    [string]$GitHubToken = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$publishDir = Join-Path $root "publish"
$releaseDir = Join-Path $root "releases"
$repoUrl = "https://github.com/jeppevammenkristensen/DacPac_Viewer"

$isLinuxTarget = $Runtime -like "linux-*"
$mainExeName = if ($isLinuxTarget) { "DacPac.UI" } else { "DacPac.UI.exe" }
$iconFile = if ($isLinuxTarget) { "app-icon.png" } else { "app-icon.ico" }
$iconPath = Join-Path $root "source/DacPac.UI/Assets/$iconFile"

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Error "vpk tool not found. Install it with: dotnet tool install -g vpk"
}
if ($UploadToGitHub -and -not $GitHubToken) {
    Write-Error "Uploading requires a GitHub token. Pass -GitHubToken or set GITHUB_TOKEN."
}
if ($isLinuxTarget -and -not $IsLinux) {
    Write-Error "Building $Runtime must run on Linux (vpk packs AppImages on the target OS)."
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Downloading previous releases from GitHub (for delta packages)..." -ForegroundColor Cyan
vpk download github --repoUrl $repoUrl --outputDir $releaseDir
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Could not download previous releases (first release?). Continuing without deltas."
}

Write-Host "Publishing DacPac.UI ($Runtime, self-contained)..." -ForegroundColor Cyan
dotnet publish (Join-Path $root "source/DacPac.UI/DacPac.UI.csproj") `
    -c Release `
    -r $Runtime `
    --self-contained `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed." }

Write-Host "Packing with Velopack (version $Version)..." -ForegroundColor Cyan
vpk pack `
    --packId DacPac.UI `
    --packVersion $Version `
    --packTitle $Title `
    --packDir $publishDir `
    --mainExe $mainExeName `
    --icon $iconPath `
    --outputDir $releaseDir
if ($LASTEXITCODE -ne 0) { Write-Error "vpk pack failed." }

if ($UploadToGitHub) {
    Write-Host "Uploading release $Version to GitHub (draft)..." -ForegroundColor Cyan
    vpk upload github `
        --repoUrl $repoUrl `
        --token $GitHubToken `
        --releaseName "DacPac Viewer $Version" `
        --tag "v$Version" `
        --outputDir $releaseDir
    if ($LASTEXITCODE -ne 0) { Write-Error "vpk upload failed." }
    Write-Host "Uploaded as a draft release. Publish it on GitHub to make it available to the auto-updater." -ForegroundColor Yellow
}

Write-Host "Done. Installer written to $releaseDir" -ForegroundColor Green
