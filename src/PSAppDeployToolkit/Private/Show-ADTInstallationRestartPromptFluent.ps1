#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationRestartPromptFluent
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationRestartPromptFluent
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
        [System.UInt32]$CountdownSeconds,

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
    Write-ADTLogEntry -Message "Displaying restart prompt with a [$countDownSeconds] second countdown."
    return [PSADT.UserInterface.UnifiedADTApplication]::ShowRestartDialog(
        $Title,
        $Subtitle,
        !$NotTopMost,
        $adtConfig.Assets.Logo,
        $adtStrings.RestartPrompt.TimeRemaining,
        $CountdownSeconds / 60,
        $adtStrings.RestartPrompt.MessageRestart,
        $adtStrings.RestartPrompt.ButtonRestartLater,
        $adtStrings.RestartPrompt.ButtonRestartNow
    )
}
