#!/usr/bin/env bash
# qr2l Build Script for Linux and macOS hosts. On Windows use build.ps1.
#
# Usage: ./build.sh [Configuration] [Runtime] [--skip-deb]
#   Configuration  Release (default) or Debug
#   Runtime        linux-x64 (default), linux-arm64, win-x64, ...
#   --skip-deb     Do not build the Debian package for Linux targets
set -euo pipefail

SKIP_DEB=0
ARGS=()
for arg in "$@"; do
    if [ "$arg" = "--skip-deb" ]; then SKIP_DEB=1; else ARGS+=("$arg"); fi
done

CONFIGURATION="${ARGS[0]:-Release}"
RUNTIME="${ARGS[1]:-linux-x64}"

cd "$(dirname "$0")"

# Extract version from Directory.Build.props
VERSION=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' Directory.Build.props | head -1)

if [ -z "$VERSION" ]; then
    echo "❌ Could not extract version from Directory.Build.props"
    exit 1
fi

# Windows binaries carry the .exe suffix and ship as zip; other targets ship as tar.gz
case "$RUNTIME" in
    win*) EXE_SUFFIX=".exe" ;;
    *)    EXE_SUFFIX="" ;;
esac
BINARIES=("qr2l$EXE_SUFFIX" "qr2l-gui$EXE_SUFFIX")

echo "🚀 Building qr2l v$VERSION for $RUNTIME"

# Clean previous build directory
echo "🧹 Cleaning bin directory"
rm -rf bin
mkdir -p bin

# Publish CLI
echo "📦 Publishing CLI application"
dotnet publish qr2l.CLI/qr2l.CLI.csproj -c "$CONFIGURATION" -r "$RUNTIME" --self-contained -p:PublishSingleFile=true -o bin --verbosity quiet --nologo

# Publish GUI
echo "📦 Publishing GUI application"
dotnet publish qr2l.GUI/qr2l.GUI.csproj -c "$CONFIGURATION" -r "$RUNTIME" --self-contained -p:PublishSingleFile=true -o bin --verbosity quiet --nologo

# Cleanup
echo "🧹 Cleaning up PDB files"
rm -f bin/*.pdb

# Create archive
case "$RUNTIME" in
    win*)
        ARCHIVE="qr2l-v$VERSION-$RUNTIME.zip"
        echo "📦 Creating ZIP archive"
        (cd bin && zip -q "$ARCHIVE" "${BINARIES[@]}")
        ;;
    *)
        ARCHIVE="qr2l-v$VERSION-$RUNTIME.tar.gz"
        echo "📦 Creating TAR.GZ archive"
        for name in "${BINARIES[@]}"; do chmod +x "bin/$name"; done
        # GNU tar can force the executable bit, which matters when cross-building from a Windows filesystem
        TAR_MODE=()
        if tar --version 2>/dev/null | grep -q "GNU tar"; then TAR_MODE=(--mode="a+rx"); fi
        tar "${TAR_MODE[@]}" -czf "bin/$ARCHIVE" -C bin "${BINARIES[@]}"
        ;;
esac

OUTPUTS=("$ARCHIVE")

# Debian package, when dpkg-deb is available
if [ "$SKIP_DEB" = "0" ]; then
    case "$RUNTIME" in
        linux-x64)   DEB_ARCH="amd64" ;;
        linux-arm64) DEB_ARCH="arm64" ;;
        *)           DEB_ARCH="" ;;
    esac

    if [ -n "$DEB_ARCH" ]; then
        if command -v dpkg-deb >/dev/null 2>&1; then
            packaging/deb/build-deb.sh "$DEB_ARCH"
            OUTPUTS+=("qr2l_${VERSION}_${DEB_ARCH}.deb")
        else
            echo "ℹ️  dpkg-deb not found, skipping the Debian package"
        fi
    fi
fi

# Summary
echo "✅  Build complete!"
echo "📌 Version: $VERSION"
echo "📁 Output: ${OUTPUTS[*]/#/bin/}"
echo "📦 Files: ${BINARIES[*]}"
