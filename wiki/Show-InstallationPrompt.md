# Show-InstallationPrompt

## SYNOPSIS

Displays a custom installation prompt with the toolkit branding and optional buttons.

## SYNTAX

 `Show-InstallationPrompt [[-Title] <String>] [[-Message] <String>] [[-MessageAlignment] <String>] [[-ButtonRightText] <String>] [[-ButtonLeftText] <String>] [[-ButtonMiddleText] <String>] [[-Icon] <String>] [-NoWait] [-PersistPrompt] [[-MinimizeWindows] <Boolean>] [[-Timeout] <Int32>] [[-ExitOnTimeout] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Any combination of Left, Middle or Right buttons can be displayed. The return value of the button clicked by the user is the button text specified.

## PARAMETERS

`-Title <String>`

Title of the prompt. Default: the application installation name.

`-Message <String>`

Message text to be included in the prompt

`-MessageAlignment <String>`

Alignment of the message text. Options: Left, Center, Right. Default: Center.

`-ButtonRightText <String>`

Show a button on the right of the prompt with the specified text

`-ButtonLeftText <String>`

Show a button on the left of the prompt with the specified text

`-ButtonMiddleText <String>`

Show a button in the middle of the prompt with the specified text

`-Icon <String>`

Show a system icon in the prompt. Options: Application, Asterisk, Error, Exclamation, Hand, Information, None, Question, Shield, Warning, WinLogo. Default: None.

`-NoWait [<SwitchParameter>]`

Specifies whether to show the prompt asynchronously (i.e. allow the script to continue without waiting for a response). Default: `$false`.

`-PersistPrompt [<SwitchParameter>]`

Specify whether to make the prompt persist in the center of the screen every 10 seconds. The user will have no option but to respond to the prompt - resistance is futile\!

`-MinimizeWindows <Boolean>`

Specifies whether to minimize other windows when displaying prompt. Default: `$false`.

`-Timeout <Int32>`

Specifies the time period in seconds after which the prompt should timeout. Default: UI timeout value set in the config XML file.

`-ExitOnTimeout <Boolean>`

Specifies whether to exit the script if the UI times out. Default: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Show-InstallationPrompt -Message 'Do you want to proceed with the installation?' -ButtonRightText 'Yes' -ButtonLeftText 'No'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Show-InstallationPrompt -Title 'Funny Prompt' -Message 'How are you feeling today?' -ButtonRightText 'Good' -ButtonLeftText 'Bad' -ButtonMiddleText 'Indifferent'`

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Show-InstallationPrompt -Message 'You can customize text to appear at the end of an install, or remove it completely for unattended installations.' -Icon Information -NoWait`

## REMARKS

To see the examples, type: `Get-Help Show-InstallationPrompt -Examples`

For more information, type: `Get-Help Show-InstallationPrompt -Detailed`

For technical information, type: `Get-Help Show-InstallationPrompt -Full`

For online help, type: `Get-Help Show-InstallationPrompt -Online`
