# Example - VLC Media Player

## Description

This is an example script to deploy VLC Media Player. You will need to add the rest of the toolkit files, as well as the latest VLC setup.exe in the Files folder.

This application requires a config file to be placed in each user's AppData\Roaming folder to disable automatic updates and network communication. This is handled with the new `Copy-FileToUserProfiles` command.

## Pre-Installation

```ps
## Show Welcome Message, close VLC if required, allow up to 3 deferrals, and persist the prompt
Show-InstallationWelcome -CloseProcesses 'vlc' -AllowDeferCloseProcesses -DeferTimes 3 -PersistPrompt
```

If VLC is running, the user will be prompted to either close the app or defer the installation.

## Installation

```ps
Start-ADTProcess -FilePath 'vlc-3.0.20-win64.exe' -ArgumentList '/L=1033 /S'
```

Runs the setup with the required silent switches.

## Post-Installation

```ps
Remove-ADTFile -Path "$envCommonDesktop\VLC media player.lnk","$envCommonStartMenuPrograms\VideoLAN\Release Notes.lnk","$envCommonStartMenuPrograms\VideoLAN\Documentation.lnk","$envCommonStartMenuPrograms\VideoLAN\VideoLAN Website.lnk"

Copy-ADTFileToUserProfiles -Path "$adtSession.DirSupportFiles\vlc" -Destination 'AppData\Roaming' -Recurse
```

This deletes unwanted shortcuts from the desktop and start menu, then copies the **vlc** folder from SupportFiles to AppData\Roaming for every user.
