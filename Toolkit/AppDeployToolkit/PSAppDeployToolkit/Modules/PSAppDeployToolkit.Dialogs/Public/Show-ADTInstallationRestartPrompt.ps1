function Show-ADTInstallationRestartPrompt
{
    <#

    .SYNOPSIS
    Displays a restart prompt with a countdown to a forced restart.

    .DESCRIPTION
    Displays a restart prompt with a countdown to a forced restart.

    .PARAMETER CountdownSeconds
    Specifies the number of seconds to countdown before the system restart. Default: 60

    .PARAMETER CountdownNoHideSeconds
    Specifies the number of seconds to display the restart prompt without allowing the window to be hidden. Default: 30

    .PARAMETER SilentRestart
    Specifies whether the restart should be triggered when Deploy mode is silent or very silent. Default: $false

    .PARAMETER NoCountdown
    Specifies not to show a countdown.

    The UI will restore/reposition itself persistently based on the interval value specified in the config file.

    .PARAMETER SilentCountdownSeconds
    Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and NoSilentRestart is $false. Default: 5

    .PARAMETER NotTopMost
    Specifies whether the windows is the topmost window. Default: $false.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the version of the specified file.

    .EXAMPLE
    Show-ADTInstallationRestartPrompt -CountdownSeconds 600 -CountdownNoHideSeconds 60

    .EXAMPLE
    Show-ADTInstallationRestartPrompt -NoCountdown

    .EXAMPLE
    Show-ADTInstallationRestartPrompt -Countdownseconds 300 -NoSilentRestart $false -SilentCountdownSeconds 10

    .NOTES
    Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$CountdownSeconds = 60,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$CountdownNoHideSeconds = 30,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$SilentCountdownSeconds = 5,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SilentRestart,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoCountdown,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
    )

    Show-ADTInstallationRestartPromptClassic @PSBoundParameters
}
