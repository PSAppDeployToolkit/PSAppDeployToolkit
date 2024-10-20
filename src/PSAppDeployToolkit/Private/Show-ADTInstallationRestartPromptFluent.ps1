#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationRestartPromptFluent
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationRestartPromptFluent
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CountdownSeconds = 60,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup.
    $adtConfig = & $Script:CommandTable.'Get-ADTConfig'
    $adtStrings = & $Script:CommandTable.'Get-ADTStringTable'

    # Send this straight out to the C# backend.
    Write-ADTLogEntry -Message "Displaying restart prompt with a [$countDownSeconds] second countdown."
    return [PSADT.UserInterface.UnifiedADTApplication]::ShowRestartDialog(
            $Title,
            $null,
            !$NotTopMost,
            $adtConfig.Assets.Fluent.Logo,
            $adtConfig.Assets.Fluent.Banner.Light,
            $adtConfig.Assets.Fluent.Banner.Dark,
            $CountdownSeconds / 60,
            $adtStrings.RestartPrompt.MessageRestart,
            $adtStrings.RestartPrompt.ButtonRestartLater,
            $adtStrings.RestartPrompt.ButtonRestartNow
        )
}
