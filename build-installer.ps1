<#
.SYNOPSIS
    Builds a Velopack installer for DacPac.UI.

.DESCRIPTION
    Publishes DacPac.UI as a self-contained win-x64 app and packs it with
    Velopack (vpk). Output goes to .\releases (Setup.exe, full package, and
    delta packages against previous releases).

    Previous releases are first downloaded from GitHub Releases so vpk can
    build delta updates against them. With -UploadToGitHub the new release is
    uploaded (as a draft) to GitHub Releases, which is the feed the app's
    auto-updater checks.

    Requires the vpk global tool:  dotnet tool install -g vpk

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.1

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.1 -Title "DacPac Viewer"

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

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Error "vpk tool not found. Install it with: dotnet tool install -g vpk"
}
if ($UploadToGitHub -and -not $GitHubToken) {
    Write-Error "Uploading requires a GitHub token. Pass -GitHubToken or set GITHUB_TOKEN."
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
dotnet publish (Join-Path $root "source\DacPac.UI\DacPac.UI.csproj") `
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
    --mainExe DacPac.UI.exe `
    --icon (Join-Path $root "source\DacPac.UI\Assets\app-icon.ico") `
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
