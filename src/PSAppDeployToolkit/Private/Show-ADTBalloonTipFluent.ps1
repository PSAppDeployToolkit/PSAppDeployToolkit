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
    $adtEnv = Get-ADTEnvironment
    $adtConfig = Get-ADTConfig

    # Build out parameters for Show-ADTBalloonTipFluentInternal.
    $natnParams = [ordered]@{
        ToolkitName = $adtEnv.appDeployToolkitName
        ModuleBase = $Script:PSScriptRoot
        ToastName = $adtConfig.UI.BalloonTitle
        ToastLogo = $adtConfig.Assets.Logo
        ToastTitle = $BalloonTipTitle
        ToastText = $BalloonTipText
    }

    # If we're running as the active user, display directly; otherwise, run via Start-ADTProcessAsUser.
    if ($adtEnv.ProcessNTAccount -ne $adtEnv.runAsActiveUser.NTAccount)
    {
        Write-ADTLogEntry -Message "Displaying toast notification with message [$BalloonTipText] using Start-ADTProcessAsUser."
        Start-ADTProcessAsUser -FilePath $adtEnv.envPSProcessPath -ArgumentList "-NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(Out-ADTPowerShellEncodedCommand -Command "& {$($Script:CommandTable.'Show-ADTBalloonTipFluentInternal'.ScriptBlock)} $($natnParams | Resolve-ADTBoundParameters)")" -Wait -HideWindow
        return
    }
    Write-ADTLogEntry -Message "Displaying toast notification with message [$BalloonTipText]."
    Show-ADTBalloonTipFluentInternal @natnParams
}
