# Set-PinnedApplication

## SYNOPSIS

Pins or unpins a shortcut to the start menu or task bar.

## SYNTAX

 `Set-PinnedApplication [-Action] <String> [-FilePath] <String> [<CommonParameters>]`

## DESCRIPTION

Pins or unpins a shortcut to the start menu or task bar.

This should typically be run in the user context, as pinned items are stored in the user profile.

## PARAMETERS

`-Action <String>`

Action to be performed. Options: 'PintoStartMenu','UnpinfromStartMenu','PintoTaskbar','UnpinfromTaskbar'.

`-FilePath <String>`

Path to the shortcut file to be pinned or unpinned.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Set-PinnedApplication -Action 'PintoStartMenu' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Set-PinnedApplication -Action 'UnpinfromTaskbar' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"`

## REMARKS

To see the examples, type: `Get-Help Set-PinnedApplication -Examples`

For more information, type: `Get-Help Set-PinnedApplication -Detailed`

For technical information, type: `Get-Help Set-PinnedApplication -Full`

For online help, type: `Get-Help Set-PinnedApplication -Online`
