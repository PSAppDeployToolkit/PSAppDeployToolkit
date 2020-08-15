# Show-DialogBox

## SYNOPSIS

Display a custom dialog box with optional title, buttons, icon and timeout.

Show-InstallationPrompt is recommended over this function as it provides more customization and uses consistent branding with the other UI components.

## SYNTAX

 `Show-DialogBox [-Text] <String> [-Title <String>] [-Buttons <String>] [-DefaultButton <String>] [-Icon <String>] [-Timeout <String>] [-TopMost <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Display a custom dialog box with optional title, buttons, icon and timeout. The default button is "OK", the default Icon is "None", and the default Timeout is none.

## PARAMETERS

`-Text <String>`

Text in the message dialog box

`-Title <String>`

Title of the message dialog box

`-Buttons <String>`

Buttons to be included on the dialog box. Options: OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel, CancelTryAgainContinue. Default: OK.

`-DefaultButton <String>`

The Default button that is selected. Options: First, Second, Third. Default: First.

`-Icon <String>`

Icon to display on the dialog box. Options: None, Stop, Question, Exclamation, Information. Default: None.

`-Timeout <String>`

Timeout period in seconds before automatically closing the dialog box with the return message "Timeout". Default: UI timeout value set in the config XML file.

`-TopMost <Boolean>`

Specifies whether the message box is a system modal message box and appears in a topmost window. Default: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Show-DialogBox -Title 'Installed Complete' -Text 'Installation has completed. Please click OK and restart your computer.' -Icon 'Information'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Show-DialogBox -Title 'Installation Notice' -Text 'Installation will take approximately 30 minutes. Do you wish to proceed?' -Buttons 'OKCancel' -DefaultButton 'Second' -Icon`

'Exclamation' -Timeout 600 -Topmost `$false`

## REMARKS

To see the examples, type: `Get-Help Show-DialogBox -Examples`

For more information, type: `Get-Help Show-DialogBox -Detailed`

For technical information, type: `Get-Help Show-DialogBox -Full`

For online help, type: `Get-Help Show-DialogBox -Online`
