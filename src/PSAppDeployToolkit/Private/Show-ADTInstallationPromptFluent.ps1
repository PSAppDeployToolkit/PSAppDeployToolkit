﻿#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationPromptFluent
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationPromptFluent
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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Timeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup.
    $adtConfig = Get-ADTConfig

    # Send this straight out to the C# backend.
    return [PSADT.UserInterface.UnifiedADTApplication]::ShowCustomDialog(
        $adtConfig.UI.FluentAccentColorOverride,
        [System.TimeSpan]::FromSeconds($Timeout),
        $Title,
        $Subtitle,
        !$NotTopMost,
        (Get-ADTConfig).Assets.Logo,
        $Message,
        $ButtonLeftText,
        $ButtonMiddleText,
        $ButtonRightText
    )
}
