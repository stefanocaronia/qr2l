# Submits a version update of the winget package for an existing GitHub release.
#
# Usage: packaging/winget/update.ps1 -Tag 1.1.0
# Requires the WINGET_TOKEN environment variable: a GitHub token with the public_repo scope,
# used by wingetcreate to fork microsoft/winget-pkgs and open the pull request.
param(
    [Parameter(Mandatory = $true)]
    [string]$Tag
)

$ErrorActionPreference = "Stop"

$package = "StefanoCaronia.qr2l"
$version = $Tag.TrimStart("v")
$installerUrl = "https://github.com/stefanocaronia/qr2l/releases/download/$Tag/qr2l-v$version-win-x64.zip"

if (-not $env:WINGET_TOKEN) {
    Write-Host "WINGET_TOKEN not set, skipping the winget update"
    exit 0
}

# A version update only works once the package is in the community repository:
# the initial submission is a separate, manually reviewed pull request.
$manifestUrl = "https://api.github.com/repos/microsoft/winget-pkgs/contents/manifests/s/StefanoCaronia/qr2l"
$headers = @{ "User-Agent" = "qr2l-release" }

if ($env:GITHUB_TOKEN) {
    $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
}

try {
    Invoke-RestMethod -Uri $manifestUrl -Headers $headers | Out-Null
} catch {
    Write-Host "ℹ️  $package is not in winget-pkgs yet (initial submission still pending), skipping the update"
    exit 0
}

Write-Host "📦 Submitting $package $version to winget-pkgs"
Invoke-WebRequest -Uri "https://aka.ms/wingetcreate/latest" -OutFile "wingetcreate.exe"

& .\wingetcreate.exe update $package --version $version --urls $installerUrl --submit --token $env:WINGET_TOKEN

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ winget update failed" -ForegroundColor Red
    exit 1
}
