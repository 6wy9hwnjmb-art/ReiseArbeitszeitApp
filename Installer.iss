#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif

#ifndef PublishDir
  #define PublishDir "publish\win-x64-v0.1.0"
#endif

#ifndef InstallerOutputDir
  #define InstallerOutputDir "publish\installer"
#endif

#define MyAppName "Reise- und Arbeitszeitrechner"
#define MyAppExeName "ReiseArbeitszeitApp.exe"
#define MyAppPublisher "Sebe"
#define MyAppId "{{B1BF5C78-5FB1-4FA2-91BB-92D99D4F6EC9}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\ReiseArbeitszeitApp
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#InstallerOutputDir}
OutputBaseFilename=ReiseArbeitszeitApp-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile=Assets\app-icon.ico
WizardSmallImageFile=Assets\app-icon.png
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"; GroupDescription: "Zusätzliche Verknüpfungen:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} starten"; Flags: nowait postinstall skipifsilent
