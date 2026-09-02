#!/usr/bin/env bash
# Builds the Debian package from the binaries published by build.sh (bin/qr2l and bin/qr2l-gui).
#
# Usage: packaging/deb/build-deb.sh [Architecture]
#   Architecture  amd64 (default) or arm64, matching the runtime used with build.sh
set -euo pipefail

cd "$(dirname "$0")/../.."

ARCH="${1:-amd64}"
VERSION=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' Directory.Build.props | head -1)

if [ -z "$VERSION" ]; then
    echo "❌ Could not extract version from Directory.Build.props"
    exit 1
fi

for binary in bin/qr2l bin/qr2l-gui; do
    if [ ! -f "$binary" ]; then
        echo "❌ $binary not found: run ./build.sh first"
        exit 1
    fi
done

PACKAGE="qr2l_${VERSION}_${ARCH}"

# Staged in a native temporary directory: dpkg-deb validates permissions, which
# mounted Windows filesystems (WSL, containers) cannot represent correctly.
STAGE_ROOT=$(mktemp -d)
STAGE="$STAGE_ROOT/$PACKAGE"
trap 'rm -rf "$STAGE_ROOT"' EXIT

echo "📦 Building $PACKAGE.deb"

mkdir -p "$STAGE/DEBIAN" \
         "$STAGE/usr/bin" \
         "$STAGE/usr/share/applications" \
         "$STAGE/usr/share/icons/hicolor/32x32/apps" \
         "$STAGE/usr/share/doc/qr2l"

install -m 755 bin/qr2l bin/qr2l-gui "$STAGE/usr/bin/"
install -m 644 packaging/deb/qr2l.desktop "$STAGE/usr/share/applications/qr2l.desktop"
install -m 644 qr2l.GUI/Assets/qr2l.png "$STAGE/usr/share/icons/hicolor/32x32/apps/qr2l.png"
install -m 644 LICENSE "$STAGE/usr/share/doc/qr2l/copyright"

# Installed-Size is expressed in KiB
SIZE=$(du -sk "$STAGE/usr" | cut -f1)
sed -e "s/@VERSION@/$VERSION/" -e "s/@ARCH@/$ARCH/" -e "s/@SIZE@/$SIZE/" packaging/deb/control > "$STAGE/DEBIAN/control"

dpkg-deb --build --root-owner-group "$STAGE" "bin/$PACKAGE.deb"

echo "✅  Package ready: bin/$PACKAGE.deb"
