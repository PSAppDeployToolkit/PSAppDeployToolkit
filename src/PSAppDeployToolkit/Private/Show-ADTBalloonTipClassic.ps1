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

    # Build out parameters for Show-ADTBalloonTipClassicInternal.
    $nabtParams = [ordered]@{
        BalloonTipText = $BalloonTipText
        BalloonTipTitle = $BalloonTipTitle
        BalloonTipIcon = $BalloonTipIcon
        BalloonTipTime = $BalloonTipTime
        TrayIcon = (& $Script:CommandTable.'Get-ADTConfig').Assets.Icon
    }

    # Create in an asynchronous process so that disposal is managed for us.
    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Displaying balloon tip notification with message [$BalloonTipText]."
    & $Script:CommandTable.'Start-ADTProcess' -Path (& $Script:CommandTable.'Get-ADTPowerShellProcessPath') -Parameters "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(& $Script:CommandTable.'Out-ADTPowerShellEncodedCommand' -Command "& {$($Script:CommandTable.'Show-ADTBalloonTipClassicInternal'.ScriptBlock)} $(($nabtParams | & $Script:CommandTable.'Resolve-ADTBoundParameters').Replace('"', '\"'))")" -NoWait -WindowStyle Hidden -CreateNoWindow
}
