; LayoutFlag — Inno Setup installer script
; Build: ISCC.exe installer\LayoutFlag.iss
; Output: installer\LayoutFlag-Setup.exe

#define MyAppName "LayoutFlag"
#define MyAppVersion "1.2.0"
#define MyAppPublisher "LayoutFlag"
#define MyAppExeName "LayoutFlag.exe"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppId={{B3E9F2A1-4C5D-4E6F-8A9B-0C1D2E3F4A5B}}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=.
OutputBaseFilename={#MyAppName}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Закрываем запущенный LayoutFlag.exe перед установкой/обновлением
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
AllowNoIcons=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
russian.AutostartGroup=Автозапуск:
russian.AutostartTask=Запускать {#MyAppName} при входе в Windows
russian.UninstallShortcut=Удалить {#MyAppName}
russian.LaunchApp=Запустить {#MyAppName}

english.AutostartGroup=Startup:
english.AutostartTask=Run {#MyAppName} when Windows starts
english.UninstallShortcut=Uninstall {#MyAppName}
english.LaunchApp=Launch {#MyAppName}

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutostartTask}"; GroupDescription: "{cm:AutostartGroup}"

[Files]
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallShortcut}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--lang={language}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\LayoutFlag"
Type: filesandordirs; Name: "{localappdata}\LayoutFlag"
