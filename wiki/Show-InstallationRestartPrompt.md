# Show-InstallationRestartPrompt

## SYNOPSIS

Displays a restart prompt with a countdown to a forced restart.

## SYNTAX

 `Show-InstallationRestartPrompt [[-CountdownSeconds] <Int32>] [[-CountdownNoHideSeconds] <Int32>] [-NoCountdown] [<CommonParameters>]`

## DESCRIPTION

Displays a restart prompt with a countdown to a forced restart.

## PARAMETERS

`-CountdownSeconds <Int32>`

Specifies the number of seconds to countdown before the system restart.

`-CountdownNoHideSeconds <Int32>`

Specifies the number of seconds to display the restart prompt without allowing the window to be hidden.

`-NoCountdown [<SwitchParameter>]`

Specifies not to show a countdown, just the Restart Now and Restart Later buttons.

The UI will restore/reposition itself persistently based on the interval value specified in the config file.

`-NoSilentRestart <bool>`

Specifies whether the restart should be triggered when Deploy mode is silent or very silent. Default: $true

`-SilentCountdownSeconds <int32>`

Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and NoSilentRestart is $false. Default: 5

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Show-InstallationRestartPrompt -NoCountdown`

-------------------------- EXAMPLE 3 --------------------------

`PS Show-InstallationRestartPrompt -Countdownseconds 300 -NoSilentRestart $false -SilentCountdownSeconds 10`

*NOTES:	Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

## REMARKS

To see the examples, type: `Get-Help Show-InstallationRestartPrompt -Examples`

For more information, type: `Get-Help Show-InstallationRestartPrompt -Detailed`

For technical information, type: `Get-Help Show-InstallationRestartPrompt -Full`

For online help, type: `Get-Help Show-InstallationRestartPrompt -Online`
