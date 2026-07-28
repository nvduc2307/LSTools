#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef OutputBaseFilename
  #define OutputBaseFilename "LSTools-Setup"
#endif

[Setup]
AppId={{D58E4AE4-849D-4ABC-8E7A-9AE4B7F443E6}
AppName=LSTools
AppVersion={#AppVersion}
AppPublisher=LSTools
DefaultDirName={localappdata}\Programs\LSTools
DefaultGroupName=LSTools
SourceDir={#SourcePath}
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename={#OutputBaseFilename}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
Uninstallable=yes
SetupLogging=yes
RestartApplications=no
CloseApplications=no

[Files]
Source: "manifests\LSTool.R24.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; DestName: "LSTool.addin"; Flags: ignoreversion; Check: ShouldInstallVersion('2024')
Source: "..\LSTool\bin\Release R24\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024\LSTool"; Excludes: "publish\*,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: ShouldInstallVersion('2024')
Source: "manifests\LSTool.R25.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; DestName: "LSTool.addin"; Flags: ignoreversion; Check: ShouldInstallVersion('2025')
Source: "..\LSTool\bin\Release R25\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025\LSTool"; Excludes: "publish\*,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: ShouldInstallVersion('2025')
Source: "manifests\LSTool.R26.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2026"; DestName: "LSTool.addin"; Flags: ignoreversion; Check: ShouldInstallVersion('2026')
Source: "..\LSTool\bin\Release R26\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2026\LSTool"; Excludes: "publish\*,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: ShouldInstallVersion('2026')

[InstallDelete]
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2024\LSTool\Resources\Settings\LicenseServer.json"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2025\LSTool\Resources\Settings\LicenseServer.json"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2026\LSTool\Resources\Settings\LicenseServer.json"

[UninstallDelete]
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2024\LSTool.addin"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2024\LSTool"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2025\LSTool.addin"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2025\LSTool"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2026\LSTool.addin"
Type: filesandordirs; Name: "{userappdata}\Autodesk\Revit\Addins\2026\LSTool"

[Code]
function RevitRegistryInstallationExists(Version: String): Boolean;
var
  InstallationLocation: String;
begin
  Result :=
    RegKeyExists(
      HKLM64,
      'SOFTWARE\Autodesk\Revit\Autodesk Revit ' + Version) or
    RegKeyExists(
      HKLM64,
      'SOFTWARE\Autodesk\Revit\' + Version);

  if (not Result) and
     RegQueryStringValue(
       HKLM64,
       'SOFTWARE\Autodesk\Revit\Autodesk Revit ' + Version,
       'InstallationLocation',
       InstallationLocation) then
  begin
    Result :=
      FileExists(AddBackslash(InstallationLocation) + 'Revit.exe');
  end;
end;

function RevitExecutableExists(Version: String): Boolean;
begin
  Result :=
    FileExists(
      ExpandConstant(
        '{pf64}\Autodesk\Revit ' + Version + '\Revit.exe'));
end;

function BooleanText(Value: Boolean): String;
begin
  if Value then
    Result := 'true'
  else
    Result := 'false';
end;

function ShouldInstallVersion(Version: String): Boolean;
var
  RegistryDetected: Boolean;
  ExecutableDetected: Boolean;
begin
  RegistryDetected := RevitRegistryInstallationExists(Version);
  ExecutableDetected := RevitExecutableExists(Version);
  Result :=
    RegistryDetected or
    ExecutableDetected;

  Log(
    'Revit ' + Version +
    ' detection: registry=' + BooleanText(RegistryDetected) +
    ', executable=' + BooleanText(ExecutableDetected) +
    ', install=' + BooleanText(Result));
end;

function AnySupportedRevitInstalled(): Boolean;
begin
  Result :=
    ShouldInstallVersion('2024') or
    ShouldInstallVersion('2025') or
    ShouldInstallVersion('2026');
end;

function IsRevitRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result :=
    Exec(
      ExpandConstant('{cmd}'),
      '/C tasklist /FI "IMAGENAME eq Revit.exe" /NH | find /I "Revit.exe" >nul',
      '',
      SW_HIDE,
      ewWaitUntilTerminated,
      ResultCode) and
    (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := False;

  if IsRevitRunning() then
  begin
    MsgBox(
      'Vui lòng đóng toàn bộ cửa sổ Revit trước khi cài đặt LSTools.',
      mbError,
      MB_OK);
    Exit;
  end;

  if not AnySupportedRevitInstalled() then
  begin
    MsgBox(
      'Không tìm thấy Revit 2024, 2025 hoặc 2026 trên máy này.',
      mbError,
      MB_OK);
    Exit;
  end;

  Result := True;
end;
