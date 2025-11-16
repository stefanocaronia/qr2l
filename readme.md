# qr2l
**A minimalistic QR Code Generator Tool**

---

## Projects

### **qr2l.Core**
Core library providing QR code generation functionality with support for multiple formats (PNG, SVG, PDF, BMP, JPEG, GIF, PostScript) and customization options (colors, logos, error correction levels, pixel shapes). Built on top of QRCoder.

### **qr2l.CLI**
Command-line interface for generating QR codes from the terminal. Supports all core features with an easy-to-use syntax for quick QR code generation.

### **qr2l.GUI**
Windows Forms desktop application with a graphical interface for creating and exporting QR codes. Features real-time preview, color customization, logo embedding, and clipboard support.

---

## Build Instructions

### Prerequisites
- .NET 9.0 SDK or later

### Build All Projects
```powershell
dotnet build qr2l.slnx
```

### Build Individual Projects
```powershell
# Core library
dotnet build qr2l.Core/qr2l.Core.csproj

# CLI application
dotnet build qr2l.CLI/qr2l.CLI.csproj

# GUI application
dotnet build qr2l.GUI/qr2l.GUI.csproj
```

### Release Build
```powershell
dotnet build qr2l.slnx -c Release
```

---

## CLI Usage Example
```powershell
qr2l "https://example.com" output.png --error-correction=high --dark-color=000000
qr2l "WIFI:networkname;password" output.svg
```

