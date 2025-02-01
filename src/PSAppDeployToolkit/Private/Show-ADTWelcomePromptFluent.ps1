﻿#-----------------------------------------------------------------------------
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
                        $(if ($icon) { [PSADT.UserInterface.Utilities.BitmapExtensions]::ConvertToImageSource($icon.ToBitmap()) }),
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
