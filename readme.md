# qr2l

**A minimalistic QR Code Generator Tool**

🌐 **Try it online**: [stefanocaronia.github.io/qr2l](https://stefanocaronia.github.io/qr2l/)

---

## Projects

### **qr2l.Core**

Core library providing QR code generation functionality with support for multiple formats (PNG, SVG, PDF, BMP, JPEG,
WebP, PostScript) and customization options (colors, logos, error correction levels, pixel shapes). Built on top of
QRCoder.

### **qr2l.CLI**

Command-line interface for generating QR codes from the terminal. Supports all core features with an easy-to-use syntax
for quick QR code generation.

### **qr2l.Avalonia**

Cross-platform desktop application (Avalonia UI) with a graphical interface for creating and exporting QR codes.
Features real-time preview, color customization, logo embedding, clipboard support, light/dark theme and a
multi-language interface.

---

## Dependencies

### **qr2l.Core**

- [QRCoder](https://github.com/codebude/QRCoder) 1.7.0 - QR code generation library
- [SkiaSharp](https://github.com/mono/SkiaSharp) 3.119.4 - cross-platform 2D graphics, used for raster rendering

The dependency is automatically restored when building the solution or individual projects via `dotnet build` or
`dotnet restore`.

---

## Build Instructions

### Prerequisites

- .NET 9.0 SDK or later

### Build All Projects

```powershell
dotnet build qr2l.slnx
dotnet build qr2l.slnx -c Release
```

---

## Publishing

### Version Management

The application version is centrally defined in `Directory.Build.props` and automatically inherited by all projects. To update the version, edit the `<Version>` property in this file.

### Automated Build (Recommended)

Use the provided PowerShell build script to create a complete distribution package:

```powershell
.\build.ps1
```

The script extracts the version from `Directory.Build.props` and creates `bin/qr2l-v<version>-win-x64.zip`.

For Linux, pass the runtime identifier: the output is a `tar.gz` archive with the same two binaries.

```powershell
.uild.ps1 -Runtime linux-x64
```

### Manual Publishing

Create self-contained, single-file executables for distribution:

#### CLI Application

```powershell
dotnet publish qr2l.CLI/qr2l.CLI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o bin/publish
```

#### GUI Application

```powershell
dotnet publish qr2l.Avalonia/qr2l.Avalonia.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o bin/publish
```

Replace `win-x64` with `linux-x64` to build the Linux binaries.

---

## CLI Usage Examples

```powershell
# Basic text QR code
qr2l "Hello World" output.png

# URL with custom colors
qr2l "https://example.com" qr.svg --dark-color=FF0000 --light-color=FFFF00

# High error correction with logo
qr2l "https://github.com" branded.png --error-correction=high --logo=logo.png

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

# Export to different formats
qr2l "Sample text" output.pdf
qr2l "Sample text" output.bmp
qr2l "Sample text" output.jpg
```

---

## Development transparency

This project was developed with assistance from AI coding tools. Every released change is reviewed, tested, and accepted by the project maintainer, who remains responsible for the software and its distribution.
