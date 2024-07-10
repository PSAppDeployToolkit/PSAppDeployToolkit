function Show-ADTBalloonTipClassic
{
    <#

    .SYNOPSIS
    Displays a balloon tip notification in the system tray.

    .DESCRIPTION
    Displays a balloon tip notification in the system tray.

    .PARAMETER BalloonTipText
    Text of the balloon tip.

    .PARAMETER BalloonTipTitle
    Title of the balloon tip.

    .PARAMETER BalloonTipIcon
    Icon to be used. Options: 'Error', 'Info', 'None', 'Warning'. Default is: Info.

    .PARAMETER BalloonTipTime
    Time in milliseconds to display the balloon tip. Default: 10000.

    .PARAMETER NoWait
    Create the balloontip asynchronously. Default: $false

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the version of the specified file.

    .EXAMPLE
    Show-ADTBalloonTipClassic -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

    .EXAMPLE
    Show-ADTBalloonTipClassic -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipTitle = (Get-ADTSession).GetPropertyValue('InstallTitle'),

        [Parameter(Mandatory = $false)]
        [ValidateSet('Error', 'Info', 'None', 'Warning')]
        [System.Windows.Forms.ToolTipIcon]$BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$BalloonTipTime = 10000,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    # Balloon tip script to run and handle disposal for us. The sleep of 7 seconds is the maximum amount of time a notification will display in Windows 10+.
    $balloonScript = [System.Management.Automation.ScriptBlock]::Create("Add-Type -AssemblyName System.Windows.Forms, System.Drawing; ([System.Windows.Forms.NotifyIcon]@{BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::$BalloonTipIcon; BalloonTipText = '$($BalloonTipText.Replace("'","''"))'; BalloonTipTitle = '$($BalloonTipTitle.Replace("'","''"))'; Icon = [System.Drawing.Icon]::new('$((Get-ADTConfig).Assets.Icon)'); Visible = `$true}).ShowBalloonTip($BalloonTipTime); [System.Threading.Thread]::Sleep(7000)")

    # Create in separate process if -NoWait is passed.
    if ($NoWait)
    {
        Write-ADTLogEntry -Message "Displaying balloon tip notification asynchronously with message [$BalloonTipText]."
        Start-ADTProcess -Path (Get-ADTPowerShellProcessPath) -Parameters "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command $balloonScript" -NoWait -WindowStyle Hidden -CreateNoWindow
        return
    }

    # Create in an asynchronous job so that disposal is managed for us.
    Write-ADTLogEntry -Message "Displaying balloon tip notification with message [$BalloonTipText]."
    [System.Void](Start-Job -ScriptBlock $balloonScript)
}
