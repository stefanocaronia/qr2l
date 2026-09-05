# Contributing to qr2l

Notes for building, packaging and releasing the project.

## Project structure

| Project      | What it is                                                                                   |
|--------------|----------------------------------------------------------------------------------------------|
| `qr2l.Core`  | QR generation library. Codes are computed by QRCoder and rendered with SkiaSharp, so it has no platform-specific dependency. Also holds the shared localization and user settings. |
| `qr2l.CLI`   | Command-line tool (`qr2l`)                                                                    |
| `qr2l.GUI`   | Desktop application (`qr2l-gui`), built with Avalonia UI                                     |
| `qr2l.Tests` | xUnit test suite covering payload detection, payload formats and export                      |
| `html/`      | Web version, published to GitHub Pages                                                       |
| `packaging/` | Debian package, Windows installer and winget update script                                   |

Dependencies:

- [QRCoder](https://github.com/codebude/QRCoder) 1.7.0, QR code generation
- [SkiaSharp](https://github.com/mono/SkiaSharp) 3.119.4, cross-platform 2D graphics used for raster rendering
- [Avalonia UI](https://avaloniaui.net/) 12.1, cross-platform desktop UI framework (GUI only)

## Building and testing

Requires the [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
dotnet build qr2l.slnx
dotnet test qr2l.Tests/qr2l.Tests.csproj
```

## Distribution packages

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

To publish one project by hand, replace `win-x64` with `linux-x64` for the Linux binaries:

```bash
dotnet publish qr2l.CLI/qr2l.CLI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o bin/publish
dotnet publish qr2l.GUI/qr2l.GUI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o bin/publish
```

## Continuous integration

The GitHub Actions workflow runs the same scripts. Every push to `master` runs the tests on Linux and builds and
smoke-tests both platforms: the Windows installer is installed silently and removed, the Debian package is installed
with apt and the CLI is executed. Pushing a version tag additionally publishes all packages as a GitHub Release.

## Releasing

The version is defined once in `Directory.Build.props` and inherited by all projects. To release:

1. Update the `<Version>` property and commit.
2. Push a tag with the same number:

   ```bash
   git tag 1.1.0 && git push origin 1.1.0
   ```

The release workflow does the rest. The winget package is updated automatically once it is in the community
repository; until then, or to submit a specific version on demand, run the *Update winget package* workflow from the
Actions tab with the release tag.
