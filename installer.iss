; Bright Grammar School Portal - separate application installer
#define MyAppName "Bright Grammar School Portal"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Rana Abdullah"
#define MyAppExeName "BrightGrammarSchoolPortal.exe"
#define MyAppPort "5000"

[Setup]
AppId={{E3A1F9B2-9D4C-4A2E-8F31-6C2B4E7A19D4}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\BrightGrammarSchoolPortal
DefaultGroupName=Bright Grammar School Portal
DisableProgramGroupPage=yes
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
OutputDir=output
OutputBaseFilename=BrightGrammarSchoolPortal_Setup_{#MyAppVersion}
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{commonappdata}\BrightGrammarSchoolPortal"; Permissions: users-modify

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "LaunchBrightGrammarSchoolPortal.vbs"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Bright Grammar School Portal"; Filename: "{sys}\wscript.exe"; Parameters: """{app}\LaunchBrightGrammarSchoolPortal.vbs"""; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall Bright Grammar School Portal"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Bright Grammar School Portal"; Filename: "{sys}\wscript.exe"; Parameters: """{app}\LaunchBrightGrammarSchoolPortal.vbs"""; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Bright Grammar School Portal"" dir=in action=allow protocol=TCP localport={#MyAppPort}"; Flags: runhidden
Filename: "{sys}\wscript.exe"; Parameters: """{app}\LaunchBrightGrammarSchoolPortal.vbs"""; Description: "Launch Bright Grammar School Portal"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillApp"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Bright Grammar School Portal"""; Flags: runhidden; RunOnceId: "DelFirewallRule"

[Code]
procedure PointDbAtProgramData;
var
  SettingsFile: string;
  RawContents: AnsiString;
  Contents: string;
  DbPath: string;
begin
  SettingsFile := ExpandConstant('{app}\appsettings.json');
  DbPath := ExpandConstant('{commonappdata}\BrightGrammarSchoolPortal\SchoolPortal.db');
  StringChangeEx(DbPath, '\', '\\', True);
  if LoadStringFromFile(SettingsFile, RawContents) then
  begin
    Contents := String(RawContents);
    StringChangeEx(Contents, 'Data Source=SchoolPortal.db', 'Data Source=' + DbPath, True);
    RawContents := AnsiString(Contents);
    SaveStringToFile(SettingsFile, RawContents, False);
  end;
end;

function InitializeSetup(): Boolean;
var ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then PointDbAtProgramData;
end;
