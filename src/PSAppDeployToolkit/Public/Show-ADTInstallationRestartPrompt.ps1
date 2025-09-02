﻿#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationRestartPrompt
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationRestartPrompt
{
    <#
    .SYNOPSIS
        Displays a restart prompt with a countdown to a forced restart.

    .DESCRIPTION
        Displays a restart prompt with a countdown to a forced restart. The prompt can be customized with a title, countdown duration, and whether it should be topmost. It also supports silent mode where the restart can be triggered without user interaction.

    .PARAMETER CountdownSeconds
        Specifies the number of seconds to display the restart prompt.

    .PARAMETER CountdownNoHideSeconds
        Specifies the number of seconds to display the restart prompt without allowing the window to be hidden.

    .PARAMETER SilentCountdownSeconds
        Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and `-SilentRestart` isn't specified.

    .PARAMETER SilentRestart
        Specifies whether the restart should be triggered when DeployMode is silent or very silent.

    .PARAMETER NoCountdown
        Specifies whether the user should receive a prompt to immediately restart their workstation.

    .PARAMETER WindowLocation
        The location of the dialog on the screen.

    .PARAMETER CustomText
        Specify whether to display a custom message specified in the `strings.psd1` file. Custom message must be populated for each language section in the `strings.psd1` file.

    .PARAMETER NotTopMost
        Specifies whether the prompt shouldn't be topmost, above all other windows.

    .PARAMETER AllowMove
        Specifies that the user can move the dialog on the screen.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Show-ADTInstallationRestartPrompt -NoCountdown

        Displays a restart prompt without a countdown.

    .EXAMPLE
        Show-ADTInstallationRestartPrompt -CountdownSeconds 300

        Displays a restart prompt with a 300-second countdown.

    .EXAMPLE
        Show-ADTInstallationRestartPrompt -CountdownSeconds 600 -CountdownNoHideSeconds 60

        Displays a restart prompt with a 600-second countdown and triggers a silent restart with a 60-second countdown in silent mode.

    .NOTES
        Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationRestartPrompt
    #>

    [CmdletBinding(DefaultParameterSetName = 'Countdown')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'NoCountdown')]
        [System.Management.Automation.SwitchParameter]$NoCountdown,

        [Parameter(Mandatory = $false, ParameterSetName = 'Countdown')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$CountdownSeconds = 60,

        [Parameter(Mandatory = $false, ParameterSetName = 'Countdown')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$CountdownNoHideSeconds = 30,

        [Parameter(Mandatory = $true, ParameterSetName = 'SilentRestart')]
        [System.Management.Automation.SwitchParameter]$SilentRestart,

        [Parameter(Mandatory = $false, ParameterSetName = 'SilentRestart')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$SilentCountdownSeconds = 5,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogPosition]$WindowLocation,

        [Parameter(Mandatory = $false, ParameterSetName = 'NoCountdown')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Countdown')]
        [System.Management.Automation.SwitchParameter]$CustomText,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowMove
    )

    dynamicparam
    {
        # Initialize variables.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        $adtStrings = Get-ADTStringTable

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = "Title of the prompt. Optionally used to override the active DeploymentSession's `InstallTitle` value." }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Subtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Subtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = "Subtitle of the prompt. Optionally used to override the subtitle defined in the `strings.psd1` file." }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtConfig = Get-ADTConfig

        # Set up DeploymentType.
        [System.String]$deploymentType = if ($adtSession)
        {
            $adtSession.DeploymentType
        }
        else
        {
            [PSADT.Module.DeploymentType]::Install
        }

        # Set up remainder if not specified.
        if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $PSBoundParameters.Add('Title', $adtSession.InstallTitle)
        }
        if (!$PSBoundParameters.ContainsKey('Subtitle'))
        {
            $PSBoundParameters.Add('Subtitle', $adtStrings.RestartPrompt.Subtitle.$deploymentType)
        }
        if (!$PSBoundParameters.ContainsKey('CountdownSeconds'))
        {
            $PSBoundParameters.Add('CountdownSeconds', $CountdownSeconds)
        }
        if (!$PSBoundParameters.ContainsKey('CountdownNoHideSeconds'))
        {
            $PSBoundParameters.Add('CountdownNoHideSeconds', $CountdownNoHideSeconds)
        }
    }

    process
    {
        try
        {
            try
            {
                # Check if we are already displaying a restart prompt.
                if (Get-Process | & { process { if ($_.MainWindowTitle -match $adtStrings.RestartPrompt.Title) { return $_ } } } | Select-Object -First 1)
                {
                    Write-ADTLogEntry -Message "$($MyInvocation.MyCommand.Name) was invoked, but an existing restart prompt was detected. Cancelling restart prompt." -Severity 2
                    return
                }

                # If in non-interactive mode.
                if ($adtSession -and $adtSession.IsSilent())
                {
                    if ($SilentRestart)
                    {
                        Write-ADTLogEntry -Message "Triggering restart silently because the deploy mode is set to [$($adtSession.DeployMode)] and [-SilentRestart] has been specified. Timeout is set to [$SilentCountdownSeconds] seconds."
                        $Script:ADT.RestartOnExitCountdown = $SilentCountdownSeconds
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Skipping restart because the deploy mode is set to [$($adtSession.DeployMode)] and [-SilentRestart] was not specified."
                    }
                    return
                }

                # Just restart the computer if no one's logged on to answer the dialog.
                if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
                {
                    Write-ADTLogEntry -Message "Triggering restart silently because there is no active user logged onto the system."
                    if ($adtSession)
                    {
                        $Script:ADT.RestartOnExitCountdown = $SilentCountdownSeconds
                    }
                    else
                    {
                        Invoke-ADTSilentRestart -Delay $SilentCountdownSeconds
                    }
                    return
                }

                # Build out hashtable of parameters needed to construct the dialog.
                $dialogOptions = @{
                    AppTitle = $PSBoundParameters.Title
                    Subtitle = $PSBoundParameters.Subtitle
                    AppIconImage = $adtConfig.Assets.Logo
                    AppIconDarkImage = $adtConfig.Assets.LogoDark
                    AppBannerImage = $adtConfig.Assets.Banner
                    DialogTopMost = !$NotTopMost
                    Language = $Script:ADT.Language
                    Strings = $adtStrings.RestartPrompt
                }
                if (!$NoCountdown)
                {
                    $dialogOptions.Add('CountdownDuration', [System.TimeSpan]::FromSeconds($CountdownSeconds))
                    $dialogOptions.Add('CountdownNoMinimizeDuration', [System.TimeSpan]::FromSeconds($CountdownNoHideSeconds))
                }
                if ($PSBoundParameters.ContainsKey('WindowLocation'))
                {
                    $dialogOptions.Add('DialogPosition', $WindowLocation)
                }
                if ($PSBoundParameters.ContainsKey('AllowMove'))
                {
                    $dialogOptions.Add('DialogAllowMove', !!$AllowMove)
                }
                if ($CustomText)
                {
                    $dialogOptions.CustomMessageText = $adtStrings.RestartPrompt.CustomMessage
                }
                if ($null -ne $adtConfig.UI.FluentAccentColor)
                {
                    $dialogOptions.Add('FluentAccentColor', $adtConfig.UI.FluentAccentColor)
                }
                $dialogOptions = [PSADT.UserInterface.DialogOptions.RestartDialogOptions]::new($deploymentType, $dialogOptions)

                # If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously.
                if ($adtSession)
                {
                    if ($NoCountdown)
                    {
                        Write-ADTLogEntry -Message "Invoking $($MyInvocation.MyCommand.Name) asynchronously with no countdown..."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Invoking $($MyInvocation.MyCommand.Name) asynchronously with a [$CountdownSeconds] second countdown..."
                    }
                    Invoke-ADTClientServerOperation -ShowModalDialog -User $runAsActiveUser -DialogType RestartDialog -DialogStyle $adtConfig.UI.DialogStyle -Options $dialogOptions -NoWait
                    return
                }

                # Call the underlying function to open the restart prompt.
                Write-ADTLogEntry -Message "Displaying restart prompt with $(if ($NoCountdown) { 'no' } else { "a [$CountdownSeconds] second" }) countdown."
                $null = Invoke-ADTClientServerOperation -ShowModalDialog -User $runAsActiveUser -DialogType RestartDialog -DialogStyle $adtConfig.UI.DialogStyle -Options $dialogOptions
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
