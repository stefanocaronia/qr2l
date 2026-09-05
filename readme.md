# qr2l

**A minimalistic QR Code Generator Tool**

Generate QR codes from the command line or from a desktop app, on Windows and Linux. Export to PNG, SVG, PDF, BMP,
JPEG, WebP and PostScript, pick your colors, embed a logo, and let the tool detect what you are encoding: URLs, email,
phone numbers, SMS, WhatsApp, WiFi credentials, geolocation, contacts and calendar events.

🌐 **Web version**: [stefanocaronia.github.io/qr2l](https://stefanocaronia.github.io/qr2l/) — the full tool, in the browser

---

## Installation

Every release ships two self-contained executables, `qr2l` (command line) and `qr2l-gui` (desktop app), with no
runtime to install. Get them from the [Releases page](https://github.com/stefanocaronia/qr2l/releases):

### Windows

- **Installer**: `qr2l-v<version>-win-x64-setup.exe`
- **Portable**: `qr2l-v<version>-win-x64.zip`, extract and run

### Linux

- **Debian, Ubuntu and derivatives**: `qr2l_<version>_amd64.deb`

  ```bash
  sudo apt install ./qr2l_<version>_amd64.deb
  ```

- **Any other distribution**: `qr2l-v<version>-linux-x64.tar.gz`, extract and run. Requires `fontconfig`; the
  desktop app also needs the X11 libraries (`libx11-6 libice6 libsm6` on Debian-based systems).

---

## Usage

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
the test suite on Linux, and pushing a version tag publishes everything as a GitHub Release.

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

## Development notes

This project was developed with assistance from AI coding tools. Every released change is reviewed, tested, and
accepted by the project maintainer, who remains responsible for the software and its distribution.

## License

qr2l is released under the [Creative Commons Attribution-ShareAlike 4.0](LICENSE) license (CC-BY-SA-4.0). You may use,
share and adapt it, including commercially, as long as you give credit and distribute derivatives under the same
license. The software is provided as is, without warranties of any kind.
