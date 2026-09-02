# Builds the Windows installer from the binaries published by build.ps1 (bin/qr2l.exe and bin/qr2l-gui.exe).
# Requires Inno Setup 6: https://jrsoftware.org/isinfo.php
param(
    [string]$Iscc = ""
)

$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..\..")

# Extract version from Directory.Build.props
[xml]$buildPropsXml = Get-Content "Directory.Build.props"
$version = $buildPropsXml.Project.PropertyGroup.Version

if (-not $version) {
    Write-Host "❌ Could not extract version from Directory.Build.props" -ForegroundColor Red
    exit 1
}

foreach ($binary in @("bin/qr2l.exe", "bin/qr2l-gui.exe")) {
    if (-not (Test-Path $binary)) {
        Write-Host "❌ $binary not found: run .\build.ps1 first" -ForegroundColor Red
        exit 1
    }
}

# Locate the Inno Setup compiler
if (-not $Iscc) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $Iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $Iscc) {
        $Iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
    }
}

if (-not $Iscc) {
    Write-Host "❌ Inno Setup 6 (ISCC.exe) not found" -ForegroundColor Red
    exit 1
}

$installer = "qr2l-v$version-win-x64-setup.exe"
Write-Host "📦 Building installer $installer"

& $Iscc /Q "/DAppVersion=$version" "packaging/windows/qr2l.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Installer build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅  Installer ready: bin/$installer" -ForegroundColor Green
