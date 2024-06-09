function Show-ADTClassicBalloonTip
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
    Show-ADTClassicBalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

    .EXAMPLE
    Show-ADTClassicBalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
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

    begin {
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }

    process {
        # Skip balloon if in silent mode, disabled in the config or presentation is detected.
        if ($adtSession.DeployModeSilent -or !$adtConfig.UI.BalloonNotifications)
        {
            Write-ADTLogEntry -Message "Bypassing Show-ADTClassicBalloonTip [Mode:$($adtSession.GetPropertyValue('deployMode')), Config Show Balloon Notifications:$($adtConfig.UI.BalloonNotifications)]. BalloonTipText:$BalloonTipText"
            return
        }
        if (Test-ADTPowerPoint)
        {
            Write-ADTLogEntry -Message "Bypassing Show-ADTClassicBalloonTip [Mode:$($adtSession.GetPropertyValue('deployMode')), Presentation Detected:$true]. BalloonTipText:$BalloonTipText"
            return
        }

        # Read all form assets into memory.
        Read-ADTAssetsIntoMemory

        # Create in separate process if -NoWait is passed.
        if ($NoWait)
        {
            Write-ADTLogEntry -Message "Displaying balloon tip notification asynchronously with message [$BalloonTipText]."
            Execute-Process -Path $adtEnv.envPSProcessPath -Parameters "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Add-Type -AssemblyName System.Windows.Forms, System.Drawing; ([System.Windows.Forms.NotifyIcon]@{BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::$BalloonTipIcon; BalloonTipText = '$($BalloonTipText.Replace("'","''"))'; BalloonTipTitle = '$($BalloonTipTitle.Replace("'","''"))'; Icon = [System.Drawing.Icon]::new('$($adtConfig.Assets.Icon)'); Visible = `$true}).ShowBalloonTip($BalloonTipTime); [System.Threading.Thread]::Sleep($BalloonTipTime)" -NoWait -WindowStyle Hidden -CreateNoWindow
            return
        }
        Write-ADTLogEntry -Message "Displaying balloon tip notification with message [$BalloonTipText]."
        ($Script:FormData.NotifyIcon = [System.Windows.Forms.NotifyIcon]@{BalloonTipIcon = $BalloonTipIcon; BalloonTipText = $BalloonTipText; BalloonTipTitle = $BalloonTipTitle; Icon = $Script:FormData.Assets.Icon; Visible = $true}).ShowBalloonTip($BalloonTipTime)
    }

    end {
        Write-ADTDebugFooter
    }
}
