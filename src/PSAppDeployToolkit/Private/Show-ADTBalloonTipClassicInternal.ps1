#-----------------------------------------------------------------------------
#
# MARK: Show-ADTBalloonTipClassicInternal
#
#-----------------------------------------------------------------------------

function Show-ADTBalloonTipClassicInternal
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is an internal worker function that requires no end user confirmation.')]
    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTitle,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipTitle,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipText,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Error', 'Info', 'None', 'Warning')]
        [System.String]$BalloonTipIcon,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$BalloonTipTime,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$TrayIcon,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ModuleAssembly
    )

    # Ensure script runs in strict mode since this may be called in a new scope.
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
    $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
    Set-StrictMode -Version 3

    # Add in our module assembly if specified.
    if ($PSBoundParameters.ContainsKey('ModuleAssembly'))
    {
        Add-Type -LiteralPath $ModuleAssembly
    }

    # Add in required types.
    Add-Type -AssemblyName System.Windows.Forms, System.Drawing

    # Show the dialog and sleep until done.
    $null = [PSADT.PInvoke.NativeMethods]::SetCurrentProcessExplicitAppUserModelID($BalloonTitle)
    ([System.Windows.Forms.NotifyIcon]@{ BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::$BalloonTipIcon; BalloonTipText = $BalloonTipText; BalloonTipTitle = $BalloonTipTitle; Icon = [PSADT.Shared.Utility]::ConvertImageToIcon([System.Drawing.Image]::FromFile($TrayIcon)); Visible = $true }).ShowBalloonTip($BalloonTipTime)
    [System.Threading.Thread]::Sleep($BalloonTipTime)
}
