#-----------------------------------------------------------------------------
#
# MARK: Show-ADTBalloonTipClassic
#
#-----------------------------------------------------------------------------

function Show-ADTBalloonTipClassic
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipText,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipTitle,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Error', 'Info', 'None', 'Warning')]
        [System.Windows.Forms.ToolTipIcon]$BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$BalloonTipTime = 10000
    )

    # Initialize variables.
    $adtConfig = Get-ADTConfig

    # Build out parameters for Show-ADTBalloonTipClassicInternal.
    $nabtParams = [ordered]@{
        BalloonTitle = $adtConfig.UI.BalloonTitle
        BalloonTipTitle = $BalloonTipTitle
        BalloonTipText = $BalloonTipText
        BalloonTipIcon = $BalloonTipIcon
        BalloonTipTime = $BalloonTipTime
        TrayIcon = $adtConfig.Assets.Classic.Icon
    }

    # Create in an asynchronous process so that disposal is managed for us.
    Write-ADTLogEntry -Message "Displaying balloon tip notification with message [$BalloonTipText]."
    Start-ADTProcess -FilePath (Get-ADTPowerShellProcessPath) -ArgumentList "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(Out-ADTPowerShellEncodedCommand -Command "& {$($Script:CommandTable.'Show-ADTBalloonTipClassicInternal'.ScriptBlock)} $(($nabtParams | Resolve-ADTBoundParameters).Replace('"', '\"')) -ModuleAssembly '$(Get-ADTModuleAssemblyPath)'")" -NoWait -WindowStyle Hidden -CreateNoWindow
}
