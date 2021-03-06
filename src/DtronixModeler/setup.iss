; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#dim Version[4]
#define MainBinaryName "DtronixModeler.exe"
#expr ParseVersion(MainBinaryName, Version[0], Version[1], Version[2], Version[3])
#define AppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2]) + "." + Str(Version[3])
#define ShortAppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2])


[Tasks]
Name: ddlAssociation; Description: "Associate ""ddl"" extension"; GroupDescription: File extensions:

[Registry]
Root: HKCR; Subkey: ".ddl"; ValueType: string; ValueName: ""; ValueData: "DtronixModeler"; Flags: uninsdeletevalue; Tasks: ddlAssociation 
Root: HKCR; Subkey: "DtronixModeler"; ValueType: string; ValueName: ""; ValueData: "Dtronix Modeler"; Flags: uninsdeletekey; Tasks: ddlAssociation
Root: HKCR; Subkey: "DtronixModeler\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\DtronixModeler.exe,0"; Tasks: ddlAssociation
Root: HKCR; Subkey: "DtronixModeler\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\DtronixModeler.exe"" ""%1"""; Tasks: ddlAssociation

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{D516E71D-8297-41AC-9CB4-123FC50072AD}
AppName=DtronixModeler
AppVersion={#ShortAppVersion}
AppPublisher=Dtronix
AppPublisherURL=https://github.com/Dtronix/DtronixModel
AppSupportURL=https://github.com/Dtronix/DtronixModel
AppUpdatesURL=https://github.com/Dtronix/DtronixModel/releases
DefaultDirName={pf}\DtronixModeler
DefaultGroupName=DtronixModeler
OutputDir=/
OutputBaseFilename=DtronixModeler-{#ShortAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: {#MainBinaryName}; DestDir: "{app}"; Flags: ignoreversion
Source: "*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "runtimes\win-x86\*"; DestDir: "{app}\runtimes\win-x86\"; Flags: ignoreversion recursesubdirs
Source: "runtimes\win-x64\*"; DestDir: "{app}\runtimes\win-x64\"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\DtronixModeler"; Filename: "{app}\{#MainBinaryName}"
Name: "{group}\{cm:UninstallProgram,DtronixModeler}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\DtronixModeler.exe"; Description: "{cm:LaunchProgram,DtronixModeler}"; Flags: nowait postinstall skipifsilent
