; Script generated for Dimmer-MAUI

[Setup]
; Basic application information
AppName=Dimmer
AppVersion=1.9.9-Release
AppPublisher=Yvan Brunel
AppPublisherURL=https://github.com/YBTopaz8/Dimmer-MAUI
DefaultDirName={autopf}\Dimmer-MAUI
DefaultGroupName=Dimmer-MAUI
OutputBaseFilename=Setup_Dimmer1.9.9
Compression=lzma
SolidCompression=yes

; Show a modern wizard interface
;SetupIconFile={src}\appicon.ico ; Optional: Add an icon for the installer if available

[Files]
; Include all files from the output folder
Source: "C:\dimm199dAddRemove\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Add a shortcut to the Start Menu
Name: "{group}\Dimmer"; Filename: "{app}\Dimmer.WinUI.exe"
; Add a shortcut to the Desktop
Name: "{commondesktop}\Dimmer"; Filename: "{app}\Dimmer.WinUI.exe"
; Add an uninstaller shortcut
Name: "{group}\Uninstall Dimmer"; Filename: "{uninstallexe}"

[Run]
; Launch the application after installation
Filename: "{app}\Dimmer.WinUI.exe"; Description: "Launch Dimmer"; Flags: nowait postinstall skipifsilent
; Open the GitHub page after installation
Filename: "cmd.exe"; Description: "View Project on GitHub"; Parameters: "/c start https://github.com/YBTopaz8/Dimmer-MAUI"; Flags: postinstall shellexec


[Registry]

; — Universal “Open with Dimmer” on **any** file
Root: HKCR; Subkey: "*\shell\Open with Dimmer";          ValueType: string; ValueName: "";   ValueData: "Open with Dimmer";  Flags: uninsdeletekey
Root: HKCR; Subkey: "*\shell\Open with Dimmer\command";  ValueType: string; ValueName: "";   ValueData: """{app}\Dimmer.WinUI.exe"" ""%1"""; Flags: uninsdeletekey
