#-----------------------------------------------------------------------------
#
# MARK: Show-ADTWelcomePromptFluent
#
#-----------------------------------------------------------------------------

function Show-ADTWelcomePromptFluent
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ProcessObjects', Justification = 'This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.')]
    param
    (
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.ProcessObject[]]$ProcessObjects,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_ -gt (& $Script:CommandTable.'Get-ADTConfig').UI.DefaultTimeout) {
                    $PSCmdlet.ThrowTerminatingError((& $Script:CommandTable.'New-ADTValidateScriptErrorRecord' -ParameterName CloseAppsCountdown -ProvidedValue $_ -ExceptionMessage 'The close applications countdown time cannot be longer than the timeout specified in the config file.'))
                }
                return ($_ -ge 0)
            })]
        [System.Double]$CloseAppsCountdown,

        [ValidateNotNullOrEmpty()]
        [System.UInt32]$DeferTimes,

        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [System.Management.Automation.SwitchParameter]$AllowDefer,
        [System.Management.Automation.SwitchParameter]$NoMinimizeWindows,
        [System.Management.Automation.SwitchParameter]$NotTopMost,
        [System.Management.Automation.SwitchParameter]$CustomText,
        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup.
    $adtConfig = & $Script:CommandTable.'Get-ADTConfig'
    $adtStrings = & $Script:CommandTable.'Get-ADTStringTable'
    $adtSession = & $Script:CommandTable.'Get-ADTSession'

    # Convert the incoming ProcessObject objects into AppProcessInfo objects.
    $appsToClose = Get-ADTRunningProcesses -ProcessObjects $ProcessObjects -InformationAction SilentlyContinue | & {
        process
        {
            [PSADT.UserInterface.Services.AppProcessInfo]::new(
                    $_.Name,
                    $_.ProcessDescription,
                    $_.Product,
                    $_.Company,
                    $null,
                    $_.StartTime
                )
        }
    }

    # Send this out to the C# code.
    $result = [PSADT.UserInterface.UnifiedADTApplication]::ShowWelcomeDialog(
            $adtSession.GetPropertyValue('InstallTitle'),
            [System.String]::Format($adtStrings.WelcomePrompt.Fluent.Subtitle, $adtSession.GetPropertyValue('DeploymentType')),
            !$NotTopMost,
            $DeferTimes,
            $appsToClose,
            $adtConfig.Assets.Fluent.Logo,
            $adtConfig.Assets.Fluent.Banner.Light,
            $adtConfig.Assets.Fluent.Banner.Dark,
            $adtStrings.WelcomePrompt.Fluent.DialogMessage,
            $adtStrings.WelcomePrompt.Fluent.ButtonDeferRemaining,
            $adtStrings.WelcomePrompt.Fluent.ButtonLeftText,
            $adtStrings.WelcomePrompt.Fluent.ButtonRightText,
            $(if ($adtConfig.UI.DynamicProcessEvaluation) {[PSADT.UserInterface.Services.ProcessEvaluationService]::new()})
        )

    # Return a translated value that's compatible with the toolkit.
    switch ($result)
    {
        Continue
        {
            return 'Close'
            break
        }
        Defer
        {
            return 'Defer'
            break
        }
        default
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The returned dialog result of [$_] is invalid and cannot be processed.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = "WelcomeDialogInvalidResult"
                TargetObject = $_
            }
            throw (New-ADTErrorRecord @naerParams)
        }
    }
}
