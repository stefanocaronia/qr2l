# Submits the winget package for a published GitHub release.
#
# The manifests live next to this script: package name, description and installer are decided there,
# and this script only fills in the version, the release date and the installer checksum.
#
# Usage: packaging/winget/update.ps1 -Tag 1.2.0 [-Installer path\to\setup.exe]
# Submitting requires the WINGET_TOKEN environment variable: a GitHub token with the public_repo and
# workflow scopes, used by wingetcreate to fork microsoft/winget-pkgs and open the pull request.
# Without it the manifests are only written, to check them with winget validate.
param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    # Local copy of the installer to compute the checksum; by default it is downloaded from the release
    [string]$Installer
)

$ErrorActionPreference = "Stop"

$package = "StefanoCaronia.qr2l"
$version = $Tag.TrimStart("v")
$temp = [System.IO.Path]::GetTempPath()
$out = Join-Path $temp "$package-$version"

if (-not $Installer) {
    $Installer = Join-Path $temp "qr2l-v$version-win-x64-setup.exe"
    Invoke-WebRequest -Uri "https://github.com/stefanocaronia/qr2l/releases/download/$Tag/qr2l-v$version-win-x64-setup.exe" -OutFile $Installer
}

$sha256 = (Get-FileHash -Path $Installer -Algorithm SHA256).Hash

# Day the release was published on GitHub; today for a local check of a release not published yet
$headers = @{ "User-Agent" = "qr2l-release" }

if ($env:GITHUB_TOKEN) {
    $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
}

try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/stefanocaronia/qr2l/releases/tags/$Tag" -Headers $headers
    $releaseDate = ([datetime]$release.published_at).ToUniversalTime().ToString("yyyy-MM-dd")
} catch {
    $releaseDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
}

New-Item -ItemType Directory -Force -Path $out | Out-Null

foreach ($template in Get-ChildItem -Path $PSScriptRoot -Filter "$package*.yaml") {
    $manifest = (Get-Content -Path $template.FullName -Raw).
        Replace('$(Version)', $version).
        Replace('$(Tag)', $Tag).
        Replace('$(ReleaseDate)', $releaseDate).
        Replace('$(InstallerSha256)', $sha256).
        Replace("`r`n", "`n")

    Set-Content -Path (Join-Path $out $template.Name) -Value $manifest -NoNewline -Encoding utf8NoBOM
}

if (-not $env:WINGET_TOKEN) {
    Write-Host "Manifests written to $out (WINGET_TOKEN not set, nothing submitted)"
    exit 0
}

Write-Host "📦 Submitting $package $version to winget-pkgs"
Invoke-WebRequest -Uri "https://aka.ms/wingetcreate/latest" -OutFile "wingetcreate.exe"

& .\wingetcreate.exe submit $out --prtitle "New version: $package version $version" --token $env:WINGET_TOKEN

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ winget submission failed" -ForegroundColor Red
    exit 1
}
