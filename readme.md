# qr2l

**A minimalistic QR Code Generator Tool**

Generate QR codes from the command line or from a desktop app, on Windows and Linux. Export to PNG, SVG, PDF, BMP,
JPEG, WebP and PostScript, pick your colors, embed a logo, and let the tool detect what you are encoding: URLs, email,
phone numbers, SMS, WhatsApp, WiFi credentials, geolocation, contacts and calendar events.

🌐 **Try it online**: [stefanocaronia.github.io/qr2l](https://stefanocaronia.github.io/qr2l/)

---

## Installation

Every release ships two self-contained executables, with no runtime to install:

| File       | What it is                                            |
|------------|-------------------------------------------------------|
| `qr2l`     | Command-line tool                                     |
| `qr2l-gui` | Desktop application (light/dark theme, 10 languages)  |

### Windows

**Installer**: download `qr2l-v<version>-win-x64-setup.exe` from the
[Releases page](https://github.com/stefanocaronia/qr2l/releases) and run it. It adds qr2l to the Start menu,
optionally to the desktop and to the `PATH`, and can be removed from *Installed apps*. No administrator rights are
needed for a per-user install.

**winget**: installs the portable tools and puts `qr2l` and `qr2l-gui` on the `PATH`, without Start menu entries:

```powershell
winget install StefanoCaronia.qr2l
```

**Portable**: download `qr2l-v<version>-win-x64.zip`, extract it anywhere and run `qr2l.exe` or `qr2l-gui.exe`.

### Linux

**Debian, Ubuntu and derivatives**: download `qr2l_<version>_amd64.deb` from the
[Releases page](https://github.com/stefanocaronia/qr2l/releases) and install it with apt, which also resolves the
dependencies. Both tools land in `/usr/bin` and the desktop app appears in the applications menu.

```bash
sudo apt install ./qr2l_<version>_amd64.deb
```

**Any other distribution**: download `qr2l-v<version>-linux-x64.tar.gz` and extract it:

```bash
tar -xzf qr2l-v<version>-linux-x64.tar.gz
./qr2l "Hello World" hello.png
./qr2l-gui
```

To make the tools available system-wide, move them to a directory in your `PATH`, for example:

```bash
sudo install -m 755 qr2l qr2l-gui /usr/local/bin/
```

**Requirements**: a 64-bit glibc-based distribution. The CLI needs `fontconfig`; the desktop app additionally needs
an X11 or Wayland session with the usual X libraries. On Debian/Ubuntu (already handled by the `.deb`):

```bash
sudo apt install libfontconfig1 libx11-6 libice6 libsm6
```

These libraries are already present on any regular desktop installation.

---

## Usage

### Desktop application

Run `qr2l-gui`: type or paste the content, pick the colors, optionally add a logo, and the preview updates as you type.
Save in any supported format or copy the image to the clipboard. The interface follows your system language and
offers a light and a dark theme.

### Command line

```
qr2l <text|url> <output file> [options]
```

The output format is chosen from the file extension: `.png`, `.svg`, `.pdf`, `.bmp`, `.jpg`, `.jpeg`, `.webp`, `.ps`.

| Option                        | Description                                                            |
|-------------------------------|------------------------------------------------------------------------|
| `--error-correction=<level>`  | `low`, `medium`, `high`, `maximum` (default: `medium`)                 |
| `--dark-color=<hex>`          | Pixel color, e.g. `1F3A93` (default: `000000`)                         |
| `--light-color=<hex>`         | Background color (default: `FFFFFF`)                                   |
| `--logo=<path>`               | Image to embed in the center (error correction is raised to maximum)   |
| `--pixels-per-module=<n>`     | Size of each module in pixels (default: `20`)                          |
| `--shape=<shape>`             | `square` or `circle` (default: `square`)                               |
| `--payload-mode=<mode>`       | Force a content type instead of auto-detecting it                      |
| `--wifi-auth=<type>`          | `wpa`, `wep`, `nopass` for WiFi payloads (default: `wpa`)              |
| `--wifi-hidden`               | Mark the WiFi network as hidden                                        |

Examples:

```bash
# Basic text QR code
qr2l "Hello World" output.png

# URL with custom colors
qr2l "https://example.com" qr.svg --dark-color=FF0000 --light-color=FFFF00

# Branded QR code with a logo
qr2l "https://github.com" branded.png --logo=logo.png

# Email with subject and body
qr2l "info@example.com;Hello;Email body text" email.png

# WiFi network (simplified format)
qr2l "WIFI:MyNetwork;password123" wifi.png

# WiFi network (complete format with WPA)
qr2l "WIFI:T:WPA;S:MyNetwork;P:secret123;" wifi-secure.svg

# Phone number
qr2l "+1234567890" phone.png

# SMS with message
qr2l "1234567890;Hello from QR code" sms.png

# WhatsApp message
qr2l "+1234567890;Hi there!" whatsapp.png

# Geolocation coordinates
qr2l "45.4642,9.1900" location.png

# Contact (vCard format)
qr2l "John;Doe;+1234567890;john@example.com" contact.png

# Custom pixel size and shape
qr2l "https://example.com" custom.png --pixels-per-module=10 --shape=circle

# Other export formats
qr2l "Sample text" output.pdf
qr2l "Sample text" output.webp
qr2l "Sample text" output.jpg
```

> The `circle` shape is decorative: dotted modules are harder for some scanners to read than square ones.
> Use it with plenty of contrast and test the result with your target devices.

---

## Projects

### **qr2l.Core**

Core library providing QR code generation with support for multiple formats (PNG, SVG, PDF, BMP, JPEG, WebP,
PostScript) and customization options (colors, logos, error correction levels, pixel shapes). QR codes are computed
by QRCoder and rendered with SkiaSharp, so the library has no platform-specific dependency.

### **qr2l.CLI**

Command-line interface for generating QR codes from the terminal. Supports all core features with an easy-to-use
syntax for quick QR code generation.

### **qr2l.GUI**

Cross-platform desktop application built with Avalonia UI. Features real-time preview, color customization, logo
embedding, clipboard support, a light/dark theme and a multi-language interface.

### **qr2l.Tests**

xUnit test suite covering payload detection, payload formats and export.

---

## Dependencies

- [QRCoder](https://github.com/codebude/QRCoder) 1.7.0 - QR code generation
- [SkiaSharp](https://github.com/mono/SkiaSharp) 3.119.4 - cross-platform 2D graphics, used for raster rendering
- [Avalonia UI](https://avaloniaui.net/) 12.1 - cross-platform desktop UI framework (GUI only)

All packages are restored automatically by `dotnet build` or `dotnet restore`.

---

## Building from source

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

### Build and test

```bash
dotnet build qr2l.slnx
dotnet test qr2l.Tests/qr2l.Tests.csproj
```

### Distribution packages

A single command publishes both executables as self-contained single files and produces every package for the target
platform in `bin/`: the archive plus the Windows installer or the Debian package.

On Windows (PowerShell):

```powershell
.\build.ps1                       # zip + installer (the installer needs Inno Setup 6)
.\build.ps1 -SkipInstaller        # zip only
.\build.ps1 -Runtime linux-x64    # tar.gz, cross-built from Windows
```

On Linux:

```bash
./build.sh                                 # tar.gz + .deb (the package needs dpkg-deb)
./build.sh Release linux-x64 --skip-deb    # tar.gz only
./build.sh Release win-x64                 # zip, cross-built from Linux
```

Missing optional tooling is not an error: the scripts skip the installer or the Debian package with a note and still
produce the archive.

The scripts are also what the GitHub Actions workflow runs: every push builds and smoke-tests both platforms and runs
the test suite on Linux, and pushing a version tag publishes everything as a GitHub Release and updates the winget
package.

### Manual publishing

Equivalent to the scripts, one project at a time. Replace `win-x64` with `linux-x64` for the Linux binaries:

```bash
dotnet publish qr2l.CLI/qr2l.CLI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o bin/publish
dotnet publish qr2l.GUI/qr2l.GUI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o bin/publish
```

### Version management

The version is defined once in `Directory.Build.props` and inherited by all projects. To release a new version,
update the `<Version>` property, commit, then push a tag with the same number:

```bash
git tag 1.1.0 && git push origin 1.1.0
```

---

## Development transparency

This project was developed with assistance from AI coding tools. Every released change is reviewed, tested, and
accepted by the project maintainer, who remains responsible for the software and its distribution.

## License

qr2l is released under the [Creative Commons Attribution-ShareAlike 4.0](LICENSE) license (CC-BY-SA-4.0). You may use,
share and adapt it, including commercially, as long as you give credit and distribute derivatives under the same
license. The software is provided as is, without warranties of any kind.
