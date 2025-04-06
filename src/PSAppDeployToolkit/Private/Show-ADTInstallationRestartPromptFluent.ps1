#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationRestartPromptFluent
#
#-----------------------------------------------------------------------------

function Private:Show-ADTInstallationRestartPromptFluent
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

    # Send this straight out to the C# backend.
    Write-ADTLogEntry -Message "Displaying restart prompt with $(if ($NoCountdown) { 'no' } else { "a [$CountdownSeconds] second" }) countdown."
    $result = [PSADT.UserInterface.UnifiedADTApplication]::ShowRestartDialog(
        $Title,
        $Subtitle,
        !$NotTopMost,
        $adtConfig.Assets.Logo,
        $adtStrings.RestartPrompt.TimeRemaining,
        $(if (!$NoCountdown) { [System.TimeSpan]::FromSeconds($CountdownSeconds) }),
        $adtStrings.RestartPrompt.Message.$DeploymentType,
        $adtStrings.RestartPrompt.MessageRestart,
        $adtStrings.RestartPrompt.ButtonRestartLater,
        $adtStrings.RestartPrompt.ButtonRestartNow
    )

    # Restart the computer if the button was pushed.
    if ($result.Equals('Restart'))
    {
        Write-ADTLogEntry -Message 'Forcefully restarting the computer...'
        Restart-Computer -Force
    }

    # Return the button's result to the caller.
    return $result
}
