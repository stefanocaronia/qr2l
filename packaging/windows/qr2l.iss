; qr2l Windows installer (Inno Setup 6)
; Built by packaging/windows/build-installer.ps1 from the binaries published by build.ps1.
; The version is passed on the command line: ISCC /DAppVersion=1.1.0 qr2l.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
AppId={{7A1E6C0B-3F4D-4B7E-9C2A-5D8F1E2B6C41}
AppName=qr2l
AppVersion={#AppVersion}
AppVerName=qr2l {#AppVersion}
AppPublisher=Stefano Caronia
AppPublisherURL=https://github.com/stefanocaronia/qr2l
AppSupportURL=https://github.com/stefanocaronia/qr2l/issues
AppUpdatesURL=https://github.com/stefanocaronia/qr2l/releases
DefaultDirName={autopf}\qr2l
DefaultGroupName=qr2l
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\bin
OutputBaseFilename=qr2l-v{#AppVersion}-win-x64-setup
SetupIconFile=..\..\qr2l.GUI\Assets\app.ico
UninstallDisplayIcon={app}\qr2l-gui.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Per-user install by default (no UAC prompt); the user can still choose a machine-wide install
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ChangesEnvironment=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "addtopath"; Description: "Add the qr2l command-line tool to the PATH"; GroupDescription: "Command line:"

[Files]
Source: "..\..\bin\qr2l.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\bin\qr2l-gui.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\qr2l"; Filename: "{app}\qr2l-gui.exe"
Name: "{group}\Uninstall qr2l"; Filename: "{uninstallexe}"
Name: "{autodesktop}\qr2l"; Filename: "{app}\qr2l-gui.exe"; Tasks: desktopicon

[Registry]
; HKA resolves to HKLM for machine-wide installs and to HKCU for per-user ones
Root: HKA; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addtopath; Check: (not IsAdminInstallMode) and NeedsAddPath('{app}')
Root: HKA; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addtopath; Check: IsAdminInstallMode and NeedsAddPath('{app}')

[Run]
Filename: "{app}\qr2l-gui.exe"; Description: "{cm:LaunchProgram,qr2l}"; Flags: nowait postinstall skipifsilent

[Code]
function PathSubKey: string;
begin
  if IsAdminInstallMode then
    Result := 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
  else
    Result := 'Environment';
end;

function PathRootKey: Integer;
begin
  if IsAdminInstallMode then
    Result := HKEY_LOCAL_MACHINE
  else
    Result := HKEY_CURRENT_USER;
end;

{ True when the directory is not already part of the PATH }
function NeedsAddPath(Param: string): Boolean;
var
  CurrentPath: string;
begin
  if not RegQueryStringValue(PathRootKey, PathSubKey, 'Path', CurrentPath) then
  begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Uppercase(Param) + ';', ';' + Uppercase(CurrentPath) + ';') = 0;
end;

{ Removes the application directory from the PATH on uninstall }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  CurrentPath, AppDir: string;
  Position: Integer;
begin
  if CurUninstallStep <> usPostUninstall then
    exit;
  if not RegQueryStringValue(PathRootKey, PathSubKey, 'Path', CurrentPath) then
    exit;
  AppDir := ExpandConstant('{app}');
  Position := Pos(';' + Uppercase(AppDir), Uppercase(CurrentPath));
  if Position > 0 then
  begin
    Delete(CurrentPath, Position, Length(AppDir) + 1);
    RegWriteExpandStringValue(PathRootKey, PathSubKey, 'Path', CurrentPath);
  end;
end;
