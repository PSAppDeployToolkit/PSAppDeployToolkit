#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationPromptFluent
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationPromptFluent
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonRightText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonLeftText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonMiddleText,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$ADTConfig,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Send this straight out to the C# backend.
    Write-ADTLogEntry -Message "Displaying custom installation prompt with the parameters: [$($PSBoundParameters | Resolve-ADTBoundParameters -Exclude ADTConfig)]."
    return [PSADT.UserInterface.UnifiedADTApplication]::ShowCustomDialog(
            $Title,
            $null,
            !$NotTopMost,
            $ADTConfig.Assets.Fluent.Logo,
            $ADTConfig.Assets.Fluent.Banner.Light,
            $ADTConfig.Assets.Fluent.Banner.Dark,
            $Message,
            $ButtonLeftText,
            $ButtonMiddleText,
            $ButtonRightText
        )
}
