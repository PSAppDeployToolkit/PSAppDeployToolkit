#-----------------------------------------------------------------------------
#
# MARK: Show-ADTBalloonTipFluent
#
#-----------------------------------------------------------------------------

function Show-ADTBalloonTipFluent
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipText,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipTitle,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Initialize variables.
    $adtEnv = & $Script:CommandTable.'Get-ADTEnvironment'
    $adtConfig = & $Script:CommandTable.'Get-ADTConfig'

    # Build out parameters for Show-ADTBalloonTipFluentInternal.
    $natnParams = [ordered]@{
        ToolkitName = $adtEnv.appDeployToolkitName
        ModuleBase = $Script:PSScriptRoot
        ToastName = $adtConfig.UI.ToastName
        ToastLogo = $adtConfig.Assets.Logo
        ToastTitle = $BalloonTipTitle
        ToastText = $BalloonTipText
    }

    # If we're running as the active user, display directly; otherwise, run via Start-ADTProcessAsUser.
    if ($adtEnv.ProcessNTAccount -ne $adtEnv.runAsActiveUser.NTAccount)
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Displaying toast notification with message [$BalloonTipText] using Start-ADTProcessAsUser."
        & $Script:CommandTable.'Start-ADTProcessAsUser' -FilePath $adtEnv.envPSProcessPath -ArgumentList "-NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(& $Script:CommandTable.'Out-ADTPowerShellEncodedCommand' -Command "& {$($Script:CommandTable.'Show-ADTBalloonTipFluentInternal'.ScriptBlock)} $($natnParams | & $Script:CommandTable.'Resolve-ADTBoundParameters')")" -Wait -HideWindow
        return
    }
    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Displaying toast notification with message [$BalloonTipText]."
    & $Script:CommandTable.'Show-ADTBalloonTipFluentInternal' @natnParams
}
