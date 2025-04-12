#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationRestartPromptFluent
#
#-----------------------------------------------------------------------------

function Private:Show-ADTInstallationRestartPromptFluent
{
    <#
    .SYNOPSIS
        Internal function to display the Restart prompt using the Fluent UI.

    .DESCRIPTION
        Called by Show-ADTInstallationRestartPrompt. Uses the UnifiedAdtApplication C# class
        to display a Restart dialog. Handles parameter mapping and result translation.

    .PARAMETER Title
        Dialog title.

    .PARAMETER Subtitle
        Dialog subtitle.

    .PARAMETER CustomMessageText
        Custom custom message text to display in the dialog.

    .PARAMETER DeploymentType
        Type of deployment ('Install', 'Uninstall', 'Repair'). Used for string selection.

    .PARAMETER CountdownSeconds
        Duration of the countdown in seconds.

    .PARAMETER NoCountdown
        Switch to disable the countdown.

    .PARAMETER NotTopMost
        Switch to prevent the dialog from being topmost.

    .OUTPUTS
        String
        Returns 'RestartNow' or 'RestartLater'.
    #>
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Subtitle,

        [Parameter(Mandatory = $false)]
        [System.String]$CustomMessageText,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeploymentType,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CountdownSeconds,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoCountdown,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup.
    $adtConfig = Get-ADTConfig
    $adtStrings = Get-ADTStringTable

    # Map parameters for the C# ShowRestartDialog method.
    $dialogParams = @{
        dialogExpiryDuration            = [System.TimeSpan]::FromMinutes($adtConfig.UI.DialogStyleFluentOptions.ExpiryDuration)
        dialogAccentColor               = $adtConfig.UI.DialogStyleFluentOptions.AccentColor
        dialogPosition                  = $adtConfig.UI.DialogStyleFluentOptions.Position
        dialogTopMost                   = !$NotTopMost
        dialogAllowMove                 = $adtConfig.UI.DialogStyleFluentOptions.AllowMove
        appTitle                        = $Title
        subtitle                        = $Subtitle
        appIconImage                    = $adtConfig.Assets.Logo
        countdownDuration               = $(if (!$NoCountdown) { [System.TimeSpan]::FromSeconds($CountdownSeconds) } else { $null })
        countdownNoMinimizeDuration     = $(if ($adtConfig.UI.RestartCountdownNoMinimizeSeconds -gt 0) { [System.TimeSpan]::FromSeconds($adtConfig.UI.RestartCountdownNoMinimizeSeconds) } else { $null })
        restartMessageText              = $adtStrings.RestartPrompt.Message.$DeploymentType # Main message
        customMessageText               = $(if ($PSBoundParameters.ContainsKey('CustomMessageText')) { $CustomMessageText }) #  Pass Custom Text directly
        countdownRestartMessageText     = $adtStrings.RestartPrompt.MessageRestart # Message shown when countdown active
        countdownAutomaticRestartText   = $adtStrings.RestartPrompt.TimeRemaining # Heading for countdown timer
        dismissButtonText               = $adtStrings.RestartPrompt.ButtonRestartLater
        restartButtonText               = $adtStrings.RestartPrompt.ButtonRestartNow
    }

    # Send this straight out to the C# backend.
    Write-ADTLogEntry -Message "Displaying restart prompt with $(if ($NoCountdown) { 'no' } else { "a [$CountdownSeconds] second" }) countdown."
    $result = [PSADT.UserInterface.UnifiedADTApplication]::ShowRestartDialog(
        $dialogParams.dialogExpiryDuration,
        $dialogParams.dialogAccentColor,
        $dialogParams.dialogPosition,
        $dialogParams.dialogTopMost,
        $dialogParams.dialogAllowMove,
        $dialogParams.appTitle,
        $dialogParams.subtitle,
        $dialogParams.appIconImage,
        $dialogParams.countdownDuration,
        $dialogParams.countdownNoMinimizeDuration,
        $dialogParams.restartMessageText,
        $dialogParams.customMessageText,
        $dialogParams.countdownRestartMessageText,
        $dialogParams.countdownAutomaticRestartText,
        $dialogParams.dismissButtonText,
        $dialogParams.restartButtonText
    )

    # Handle the result
    switch ($result) # Possible results: Restart, Dismiss, Cancel, Error, Disposed
    {
        'Restart'
        {
            Write-ADTLogEntry -Message 'User chose to restart now.'
            # The public function Show-InstallationRestartPrompt handles the actual restart.
            return 'RestartNow'
        }
        'Dismiss'
        {
            Write-ADTLogEntry -Message 'User dismissed the restart prompt.'
            return 'RestartLater'
        }
        'Cancel'
        {
            return 'RestartLater' # Treat timeout/cancel as dismiss
        }
        'Error'
        {
            Write-ADTLogEntry "An error occurred while displaying the restart prompt (Fluent)." -Severity Warning
            return 'RestartLater' # Treat errors like dismiss for safety
        }
        'Disposed'
        {
            Write-ADTLogEntry "The UI application was disposed before the restart prompt could be shown." -Severity Warning
            return 'RestartLater' # Treat as dismiss
        }
        default
        {
            $naerParams = @{
                Exception    = [System.InvalidOperationException]::new("The returned dialog result of [$result] is invalid and cannot be processed.")
                Category     = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId      = "RestartDialogInvalidResultFluent"
                TargetObject = $result
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
    }

    # Old logic removed as handling is now in the switch statement above.
    # Restart the computer if the button was pushed.
    # if ($result.Equals('Restart'))
    #{
    #    Write-ADTLogEntry -Message 'Forcefully restarting the computer...'
    #    Restart-Computer -Force
    #}

}
