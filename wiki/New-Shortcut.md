# New-Shortcut

## SYNOPSIS

Creates a new .lnk or .url type shortcut

## SYNTAX

 `New-Shortcut [-Path] <String> [-TargetPath] <String> [[-Arguments] <String>] [[-IconLocation] <String>] [[-IconIndex] <String>] [[-Description] <String>] [[-WorkingDirectory] <String>]`

[[-WindowStyle] <String>] [-RunAsAdmin] [[-Hotkey] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]

## DESCRIPTION

Creates a new shortcut .lnk or .url file, with configurable options

## PARAMETERS

`-Path <String>`

Path to save the shortcut

`-TargetPath <String>`

Target path or URL that the shortcut launches

`-Arguments <String>`

Arguments to be passed to the target path

`-IconLocation <String>`

Location of the icon used for the shortcut

`-IconIndex <String>`

Executables, DLLs, ICO files with multiple icons need the icon index to be specified

`-Description <String>`

Description of the shortcut

`-WorkingDirectory <String>`

Working Directory to be used for the target path

`-WindowStyle <String>`

Windows style of the application. Options: Normal, Maximized, Minimized. Default is: Normal.

`-RunAsAdmin [<SwitchParameter>]`

Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut.

`-Hotkey <String>`

Create a Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F"

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\system32\notepad.exe" -IconLocation "$envWinDir\system32\notepad.exe" -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"`

## REMARKS

To see the examples, type: `Get-Help New-Shortcut -Examples`

For more information, type: `Get-Help New-Shortcut -Detailed`

For technical information, type: `Get-Help New-Shortcut -Full`

For online help, type: `Get-Help New-Shortcut -Online`
