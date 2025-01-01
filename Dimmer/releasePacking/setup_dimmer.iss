; Script generated for Dimmer-MAUI

[Setup]
; Basic application information
AppName=Dimmer
AppVersion=1.0.0-Release
AppPublisher=Yvan Brunel
AppPublisherURL=https://github.com/YBTopaz8/Dimmer-MAUI
DefaultDirName={autopf}\Dimmer-MAUI
DefaultGroupName=Dimmer-MAUI
OutputBaseFilename=Setup_Dimmer
OutputDir=.\Output ; Explicitly define the output directory
Compression=lzma
SolidCompression=yes

; Show a modern wizard interface
;SetupIconFile={src}\appicon.ico ; Optional: Add an icon for the installer if available

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Add a shortcut to the Start Menu
Name: "{group}\Dimmer"; Filename: "{app}\Dimmer-MAUI.exe"
; Add a shortcut to the Desktop
Name: "{commondesktop}\Dimmer"; Filename: "{app}\Dimmer-MAUI.exe"
; Add an uninstaller shortcut
Name: "{group}\Uninstall Dimmer"; Filename: "{uninstallexe}"

[Run]
; Launch the application after installation
Filename: "{app}\Dimmer-MAUI.exe"; Description: "Launch Dimmer"; Flags: nowait postinstall skipifsilent
; Open the GitHub page after installation
Filename: "cmd.exe"; Description: "View Project on GitHub"; Parameters: "/c start https://github.com/YBTopaz8/Dimmer-MAUI"; Flags: postinstall shellexec