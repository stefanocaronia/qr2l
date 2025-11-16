# qr2l Build Script
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

# Extract version from Directory.Build.props
[xml]$buildPropsXml = Get-Content "Directory.Build.props"
$version = $buildPropsXml.Project.PropertyGroup.Version

if (-not $version) {
    Write-Host "❌ Could not extract version from Directory.Build.props" -ForegroundColor Red
    exit 1
}

Write-Host "🚀 Building qr2l v$version" -ForegroundColor Cyan

# Clean previous build directory
Write-Host "🧹 Cleaning bin directory"
if (Test-Path "bin") {
    Remove-Item "bin" -Recurse -Force
}
New-Item -ItemType Directory -Path "bin" | Out-Null

# Publish CLI
Write-Host "📦 Publishing CLI application"
$null = dotnet publish qr2l.CLI\qr2l.CLI.csproj -c $Configuration -r $Runtime --self-contained -p:PublishSingleFile=true -o bin --verbosity quiet --nologo 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ CLI publish failed" -ForegroundColor Red
    exit 1
}

# Publish GUI
Write-Host "📦 Publishing GUI application"
$null = dotnet publish qr2l.GUI\qr2l.GUI.csproj -c $Configuration -r $Runtime --self-contained -p:PublishSingleFile=true -o bin --verbosity quiet --nologo 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ GUI publish failed" -ForegroundColor Red
    exit 1
}

# Cleanup
Write-Host "🧹 Cleaning up PDB files"
Get-ChildItem "bin\*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

# Create zip
$zipName = "qr2l-v$version-$Runtime.zip"
$zipPath = "bin\$zipName"

Write-Host "📦 Creating ZIP archive"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "bin\qr2l.exe", "bin\qr2l-gui.exe" -DestinationPath $zipPath -CompressionLevel Optimal -Force

# Summary
Write-Host "✅  Build complete!" -ForegroundColor Green
Write-Host "📌 Version: $version"
Write-Host "📁 Output: $zipPath"
Write-Host "📦 Files: qr2l.exe, qr2l-gui.exe"

