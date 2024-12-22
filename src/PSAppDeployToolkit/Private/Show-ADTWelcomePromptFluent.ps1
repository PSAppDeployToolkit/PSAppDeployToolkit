#-----------------------------------------------------------------------------
#
# MARK: Show-ADTWelcomePromptFluent
#
#-----------------------------------------------------------------------------

function Show-ADTWelcomePromptFluent
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.WelcomeState]$WelcomeState,

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
        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferTimes,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoMinimizeWindows,

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
    $appsToClose = if ($WelcomeState.RunningProcesses)
    {
        $WelcomeState.RunningProcesses | & {
            process
            {
                $_.Refresh(); if (!$_.HasExited)
                {
                    # Get icon so we can convert it into a media image for the UI.
                    $icon = try
                    {
                        [PSADT.UserInterface.Utilities.ProcessExtensions]::GetIcon($_, $true)
                    }
                    catch
                    {
                        $null = $null
                    }

                    # Instantiate and return a new AppProcessInfo object.
                    return [PSADT.UserInterface.Services.AppProcessInfo]::new(
                        $_.ProcessName,
                        $_.ProcessDescription,
                        $_.Product,
                        $_.Company,
                        $(if ($icon) { [PSADT.UserInterface.Utilities.BitmapExtensions]::ConvertToImageSource($icon.ToBitmap()) }),
                        $_.StartTime
                    )
                }
            }
        }
    }

    # Minimize all other windows.
    if (!$NoMinimizeWindows)
    {
        $null = (Get-ADTEnvironmentTable).ShellApp.MinimizeAll()
    }

    # Send this out to the C# code.
    $result = [PSADT.UserInterface.UnifiedADTApplication]::ShowWelcomeDialog(
        [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultTimeout),
        $Title,
        $Subtitle,
        !$NotTopMost,
        $(if ($PSBoundParameters.ContainsKey('DeferTimes')) { $DeferTimes + 1 }),
        $appsToClose,
        $adtConfig.Assets.Logo,
        $adtStrings.WelcomePrompt.Fluent.DialogMessage,
        $adtStrings.WelcomePrompt.Fluent.DialogMessageNoProcesses.$DeploymentType,
        $adtStrings.WelcomePrompt.Fluent.ButtonDeferRemaining,
        $adtStrings.WelcomePrompt.Fluent.ButtonLeftText,
        $adtStrings.WelcomePrompt.Fluent.ButtonRightText.$DeploymentType,
        $adtStrings.WelcomePrompt.Fluent.ButtonRightTextNoProcesses.$DeploymentType,
        $(if ($adtConfig.UI.DynamicProcessEvaluation) { [PSADT.UserInterface.Services.ProcessEvaluationService]::new() })
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
        Cancel
        {
            return 'Timeout'
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
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
    }
}
