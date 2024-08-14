function Show-ADTWelcomePrompt
{
    <#

    .SYNOPSIS
    Called by Show-ADTInstallationWelcome to prompt the user to optionally do the following:
        1) Close the specified running applications.
        2) Provide an option to defer the installation.
        3) Show a countdown before applications are automatically closed.

    .DESCRIPTION
    The user is presented with a Windows Forms dialog box to close the applications themselves and continue or to have the script close the applications for them.

    If the -AllowDefer option is set to true, an optional "Defer" button will be shown to the user. If they select this option, the script will exit and return a 1618 code (SCCM fast retry code).

    The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

    .PARAMETER CloseAppsCountdown
    Specify the countdown time in seconds before running applications are automatically closed when deferral is not allowed or expired.

    .PARAMETER ForceCloseAppsCountdown
    Specify whether to show the countdown regardless of whether deferral is allowed.

    .PARAMETER PersistPrompt
    Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml.

    .PARAMETER AllowDefer
    Specify whether to provide an option to defer the installation.

    .PARAMETER DeferTimes
    Specify the number of times the user is allowed to defer.

    .PARAMETER DeferDeadline
    Specify the deadline date before the user is allowed to defer.

    .PARAMETER MinimizeWindows
    Specifies whether to minimize other windows when displaying prompt. Default: $true.

    .PARAMETER TopMost
    Specifies whether the windows is the topmost window. Default: $true.

    .PARAMETER ForceCountdown
    Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled.

    .PARAMETER CustomText
    Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the user's selection.

    .EXAMPLE
    Show-ADTWelcomePromptClassic -CloseAppsCountdown 600 -AllowDefer -DeferTimes 10

    .NOTES
    This is an internal script function and should typically not be called directly. It is used by the Show-ADTInstallationWelcome prompt to display a custom prompt.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param
    (
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.ProcessObject[]]$ProcessObjects,

        [ValidateScript({
            if ($_ -gt (Get-ADTConfig).UI.DefaultTimeout)
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName CloseAppsCountdown -ProvidedValue $_ -ExceptionMessage 'The close applications countdown time cannot be longer than the timeout specified in the config file.'))
            }
            return !!$_
        })]
        [System.UInt32]$CloseAppsCountdown,

        [ValidateNotNullOrEmpty()]
        [System.String]$DeferTimes,

        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ForceCountdown,

        [System.Management.Automation.SwitchParameter]$ForceCloseAppsCountdown,
        [System.Management.Automation.SwitchParameter]$PersistPrompt,
        [System.Management.Automation.SwitchParameter]$AllowDefer,
        [System.Management.Automation.SwitchParameter]$NoMinimizeWindows,
        [System.Management.Automation.SwitchParameter]$NotTopMost,
        [System.Management.Automation.SwitchParameter]$CustomText
    )

    # Dispatch this straight to classic until we write a replacement.
    Show-ADTWelcomePromptClassic @PSBoundParameters
}
