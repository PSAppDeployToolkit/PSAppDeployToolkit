#-----------------------------------------------------------------------------
#
# MARK: Show-ADTWelcomePromptFluent
#
#-----------------------------------------------------------------------------

function Private:Show-ADTWelcomePromptFluent
{
    <#
    .SYNOPSIS
        Internal function to display the Welcome/CloseApps prompt using the Fluent UI.

    .DESCRIPTION
        Called by Show-ADTInstallationWelcome. Uses the UnifiedAdtApplication C# class
        to display a CloseApps dialog. Handles parameter mapping and result translation.

    .PARAMETER Title
        Dialog title.

    .PARAMETER Subtitle
        Dialog subtitle.

    .PARAMETER CustomMessageText
        Custom custom message text to display in the dialog.

    .PARAMETER DeploymentType
        Type of deployment ('Install', 'Uninstall', 'Repair'). Used for string selection.

    .PARAMETER DeferTimes
        Number of deferrals remaining.

    .PARAMETER MinimizeWindows
        Switch to minimize other windows.

    .PARAMETER NotTopMost
        Switch to prevent the dialog from being topmost.

    .OUTPUTS
        String
        Returns 'Close', 'Defer', or 'Timeout'.
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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeploymentType,

        [Parameter(Mandatory = $false)]
        [System.String]$CustomMessageText,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_.TotalSeconds -gt (Get-ADTConfig).UI.DefaultTimeout)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName CloseProcessesCountdown -ProvidedValue $_ -ExceptionMessage 'The close applications countdown time cannot be longer than the timeout specified in the config file.'))
                }
                return ($_ -ge 0)
            })]
        [System.TimeSpan]$CloseProcessesCountdown,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferTimes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup.
    $adtConfig = Get-ADTConfig
    $adtStrings = Get-ADTStringTable

    # Convert the incoming ProcessObject objects into AppProcessInfo objects.
    $appsToClose = if ($welcomeState.RunningApps)
    {
        $welcomeState.RunningApps | & {
            process
            {
                $_.Process.Refresh(); if (!$_.Process.HasExited)
                {
                    # Get icon so we can convert it into a media image for the UI.
                    $icon = try
                    {
                        [PSADT.UserInterface.Utilities.ProcessExtensions]::GetIcon($_.Process, $true)
                    }
                    catch
                    {
                        $null = $null
                    }

                    # Instantiate and return a new AppProcessInfo object.
                    return [PSADT.UserInterface.Services.AppProcessInfo]::new(
                        $_.Process.ProcessName,
                        $_.Description,
                        $_.Process.Product,
                        $_.Process.Company,
                        $(if ($icon) { [PSADT.UserInterface.Extensions.BitmapExtensions]::ConvertToImageSource($icon.ToBitmap()) }),
                        $_.Process.StartTime
                    )
                }
            }
        }
    }

    # Minimize all other windows.
    if ($MinimizeWindows)
    {
        $null = (Get-ADTEnvironmentTable).ShellApp.MinimizeAll()
    }

    # Map parameters and call the updated C# ShowCloseAppsDialog method.
    $dialogParams = @{
        dialogExpiryDuration            = [System.TimeSpan]::FromMinutes($adtConfig.UI.DialogStyleFluentOptions.ExpiryDuration)
        dialogAccentColor               = $adtConfig.UI.DialogStyleFluentOptions.AccentColor
        dialogPosition                  = $adtConfig.UI.DialogStyleFluentOptions.Position
        dialogTopMost                   = !$NotTopMost
        dialogAllowMove                 = $adtConfig.UI.DialogStyleFluentOptions.AllowMove
        appTitle                        = $Title
        subtitle                        = $Subtitle
        appIconImage                    = $adtConfig.Assets.Logo
        appsToClose                     = $appsToClose # Array of [PSADT.UserInterface.Services.AppProcessInfo]
        countdownDuration               = $(if ($PSBoundParameters.ContainsKey('CloseProcessesCountdown')) { $CloseProcessesCountdown }) #  Pass ForceCloseProcessCountdown directly
        deferralsRemaining              = $(if ($PSBoundParameters.ContainsKey('DeferTimes')) { $DeferTimes }) # Pass DeferTimes directly
        deferralDeadline                = $(if ($PSBoundParameters.ContainsKey('DeferDeadline')) { $DeferDeadline }) # Pass DeferDeadline directly)
        closeAppsMessageText            = $adtStrings.WelcomePrompt.Fluent.DialogMessage
        alternativeCloseAppsMessageText = $adtStrings.WelcomePrompt.Fluent.DialogMessageNoProcesses.$DeploymentType
        customMessageText               = $(if ($PSBoundParameters.ContainsKey('CustomMessageText')) { $CustomMessageText }) #  Pass Custom Text directly
        deferralsRemainingText          = $adtStrings.WelcomePrompt.Fluent.TextBlockDeferralsRemaining
        deferralDeadlineText            = $adtStrings.WelcomePrompt.Fluent.TextBlockDeferralDeadline
        automaticStartCountdownText     = $adtStrings.WelcomePrompt.Fluent.TextBlockAutomaticStartCountdown
        deferButtonText                 = $adtStrings.WelcomePrompt.Fluent.ButtonLeftText
        continueButtonText              = $adtStrings.WelcomePrompt.Fluent.ButtonRightText.$DeploymentType
        alternativeContinueButtonText   = $adtStrings.WelcomePrompt.Fluent.ButtonRightTextNoProcesses.$DeploymentType
        processEvaluationService        = $(if ($adtConfig.UI.DynamicProcessEvaluation) { [PSADT.UserInterface.Services.ProcessEvaluationService]::new() })
    }

    # Call the C# method with positional parameters
    $result = [PSADT.UserInterface.UnifiedADTApplication]::ShowCloseAppsDialog(
        $dialogParams.dialogExpiryDuration,
        $dialogParams.dialogAccentColor,
        $dialogParams.dialogPosition,
        $dialogParams.dialogTopMost,
        $dialogParams.dialogAllowMove,
        $dialogParams.appTitle,
        $dialogParams.subtitle,
        $dialogParams.appIconImage,
        $dialogParams.appsToClose,
        $dialogParams.countdownDuration,
        $dialogParams.deferralsRemaining,
        $dialogParams.deferralDeadline,
        $dialogParams.closeAppsMessageText,
        $dialogParams.alternativeCloseAppsMessageText,
        $dialogParams.customMessageText,
        $dialogParams.deferralsRemainingText,
        $dialogParams.deferralDeadlineText,
        $dialogParams.automaticStartCountdownText,
        $dialogParams.deferButtonText,
        $dialogParams.continueButtonText,
        $dialogParams.alternativeContinueButtonText,
        $dialogParams.processEvaluationService
    )

    # Return a translated value that's compatible with the toolkit.
    switch ($result) # Possible results: Continue, Defer, Cancel, Error, Disposed
    {
        'Continue'
        {
            # User clicked Continue/Install
            return 'Close' # Maps to the legacy return value expected by Show-InstallationWelcome
        }
        'Defer'
        {
            # User clicked Defer
            return 'Defer'
        }
        'Cancel'
        {
            # Dialog timed out or was closed unexpectedly (e.g., via Dispose)
            return 'Timeout'
        }
        'Error'
        {
            # An error occurred within the C# dialog code
            Write-ADTLogEntry "An error occurred while displaying the welcome prompt (Fluent)." -Severity Warning
            return 'Timeout' # Treat errors like timeouts for safety
        }
        'Disposed'
        {
            # The application was disposed before the dialog could be shown
            Write-ADTLogEntry "The UI application was disposed before the welcome prompt could be shown." -Severity Warning
            # This is a non-fatal error, but we can't continue without the dialog.
            return 'Timeout' # Treat as timeout
        }
        default
        {
            $naerParams = @{
                Exception    = [System.InvalidOperationException]::new("The returned dialog result of [$result] is invalid and cannot be processed.")
                Category     = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId      = "WelcomeDialogInvalidResultFluent"
                TargetObject = $result
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
    }
}
