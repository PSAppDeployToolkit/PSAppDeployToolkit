#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipTitle,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Error', 'Info', 'None', 'Warning')]
        [System.Windows.Forms.ToolTipIcon]$BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$BalloonTipTime = 10000,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    # Define internal worker function.
    function New-ADTBalloonTip
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is an internal worker function that requires no end user confirmation.')]
        [CmdletBinding(SupportsShouldProcess = $false)]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.String]$BalloonTipText,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.String]$BalloonTipTitle,

            [Parameter(Mandatory = $true)]
            [ValidateSet('Error', 'Info', 'None', 'Warning')]
            [System.String]$BalloonTipIcon,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.UInt32]$BalloonTipTime,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.String]$TrayIcon
        )

        # Ensure script runs in strict mode since this may be called in a new scope.
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
        $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
        Set-StrictMode -Version 3

        # Add in required types.
        Add-Type -AssemblyName System.Windows.Forms, System.Drawing

        # Show the dialog and sleep until done.
        ([System.Windows.Forms.NotifyIcon]@{BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::$BalloonTipIcon; BalloonTipText = $BalloonTipText; BalloonTipTitle = $BalloonTipTitle; Icon = [System.Drawing.Icon]$TrayIcon; Visible = $true}).ShowBalloonTip($BalloonTipTime)
        [System.Threading.Thread]::Sleep($BalloonTipTime)
    }

    # Build out parameters for internal worker function.
    $nabtParams = [ordered]@{
        BalloonTipText = $BalloonTipText
        BalloonTipTitle = $BalloonTipTitle
        BalloonTipIcon = $BalloonTipIcon
        BalloonTipTime = $BalloonTipTime
        TrayIcon = (Get-ADTConfig).Assets.Icon
    }

    # Create in separate process if -NoWait is passed.
    if ($NoWait)
    {
        Write-ADTLogEntry -Message "Displaying balloon tip notification asynchronously with message [$BalloonTipText]."
        Start-ADTProcess -Path (Get-ADTPowerShellProcessPath) -Parameters "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(Out-ADTPowerShellEncodedCommand -Command "& {${Function:New-ADTBalloonTip}} $(($nabtParams | Resolve-ADTBoundParameters).Replace('"', '\"'))")" -NoWait -WindowStyle Hidden -CreateNoWindow
        return
    }

    # Create in an asynchronous job so that disposal is managed for us.
    Write-ADTLogEntry -Message "Displaying balloon tip notification with message [$BalloonTipText]."
    $null = Start-Job -ScriptBlock ${Function:New-ADTBalloonTip} -ArgumentList $($nabtParams.Values)
}
