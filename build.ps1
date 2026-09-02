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

# Windows binaries carry the .exe suffix and ship as zip; other targets ship as tar.gz
$isWindowsTarget = $Runtime.StartsWith("win")
$exeSuffix = if ($isWindowsTarget) { ".exe" } else { "" }
$binaries = @("qr2l$exeSuffix", "qr2l-gui$exeSuffix")

Write-Host "🚀 Building qr2l v$version for $Runtime" -ForegroundColor Cyan

# Clean previous build directory
Write-Host "🧹 Cleaning bin directory"
if (Test-Path "bin") {
    Remove-Item "bin" -Recurse -Force
}
New-Item -ItemType Directory -Path "bin" | Out-Null

# Publish CLI
Write-Host "📦 Publishing CLI application"
$null = dotnet publish qr2l.CLI/qr2l.CLI.csproj -c $Configuration -r $Runtime --self-contained -p:PublishSingleFile=true -o bin --verbosity quiet --nologo 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ CLI publish failed" -ForegroundColor Red
    exit 1
}

# Publish GUI
Write-Host "📦 Publishing GUI application"
$null = dotnet publish qr2l.Avalonia/qr2l.Avalonia.csproj -c $Configuration -r $Runtime --self-contained -p:PublishSingleFile=true -o bin --verbosity quiet --nologo 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ GUI publish failed" -ForegroundColor Red
    exit 1
}

# Cleanup
Write-Host "🧹 Cleaning up PDB files"
Get-ChildItem "bin/*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

# Create archive
if ($isWindowsTarget) {
    $archiveName = "qr2l-v$version-$Runtime.zip"
    Write-Host "📦 Creating ZIP archive"
    $paths = $binaries | ForEach-Object { "bin/$_" }
    Compress-Archive -Path $paths -DestinationPath "bin/$archiveName" -CompressionLevel Optimal -Force
} else {
    $archiveName = "qr2l-v$version-$Runtime.tar.gz"
    Write-Host "📦 Creating TAR.GZ archive"

    # Written with .NET's TarWriter so the executable bit is set explicitly:
    # an external tar run on Windows would drop it, producing binaries Linux cannot launch.
    $root = (Get-Location).Path
    $executableMode = [System.IO.UnixFileMode]"UserRead, UserWrite, UserExecute, GroupRead, GroupExecute, OtherRead, OtherExecute"
    $fileStream = [System.IO.File]::Create((Join-Path $root "bin/$archiveName"))
    $gzipStream = [System.IO.Compression.GZipStream]::new($fileStream, [System.IO.Compression.CompressionLevel]::Optimal)
    $tarWriter = [System.Formats.Tar.TarWriter]::new($gzipStream, [System.Formats.Tar.TarEntryFormat]::Pax, $false)

    try {
        foreach ($name in $binaries) {
            $entry = [System.Formats.Tar.PaxTarEntry]::new([System.Formats.Tar.TarEntryType]::RegularFile, $name)
            $entry.Mode = $executableMode
            $entry.DataStream = [System.IO.File]::OpenRead((Join-Path $root "bin/$name"))
            $tarWriter.WriteEntry($entry)
            $entry.DataStream.Dispose()
        }
    } finally {
        $tarWriter.Dispose()
        $gzipStream.Dispose()
        $fileStream.Dispose()
    }
}

# Summary
Write-Host "✅  Build complete!" -ForegroundColor Green
Write-Host "📌 Version: $version"
Write-Host "📁 Output: bin/$archiveName"
Write-Host "📦 Files: $($binaries -join ', ')"
