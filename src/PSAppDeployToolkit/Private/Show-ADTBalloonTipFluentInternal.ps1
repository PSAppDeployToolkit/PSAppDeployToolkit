#-----------------------------------------------------------------------------
#
# MARK: Show-ADTBalloonTipFluentInternal
#
#-----------------------------------------------------------------------------

function Show-ADTBalloonTipFluentInternal
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is an internal worker function that requires no end user confirmation.')]
    [CmdletBinding(SupportsShouldProcess = $false)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ToolkitName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ModuleBase,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ToastName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ToastLogo,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ToastTitle,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ToastText
    )

    # Ensure script runs in strict mode since this may be called in a new scope.
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
    $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
    Set-StrictMode -Version 3

    # Add in required assemblies.
    if (!$PSVersionTable.PSEdition.Equals('Core'))
    {
        $null = [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]
        $null = [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime]
    }
    else
    {
        Add-Type -LiteralPath $ModuleBase\lib\net6.0\Microsoft.Windows.SDK.NET.dll, $ModuleBase\lib\net6.0\WinRT.Runtime.dll
    }

    # Configure the notification centre.
    $regPath = "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\$ToolkitName"
    [Microsoft.Win32.Registry]::SetValue($regPath, 'ShowInActionCenter', 1, [Microsoft.Win32.RegistryValueKind]::DWord)
    [Microsoft.Win32.Registry]::SetValue($regPath, 'Enabled', 1, [Microsoft.Win32.RegistryValueKind]::DWord)
    [Microsoft.Win32.Registry]::SetValue($regPath, 'SoundFile', [System.String]::Empty, [Microsoft.Win32.RegistryValueKind]::String)

    # Configure the toast notification.
    $regPath = "HKEY_CURRENT_USER\Software\Classes\AppUserModelId\$ToolkitName"
    [Microsoft.Win32.Registry]::SetValue($regPath, 'DisplayName', $ToastName, [Microsoft.Win32.RegistryValueKind]::String)
    [Microsoft.Win32.Registry]::SetValue($regPath, 'ShowInSettings', 0, [Microsoft.Win32.RegistryValueKind]::DWord)
    [Microsoft.Win32.Registry]::SetValue($regPath, 'IconUri', $ToastLogo, [Microsoft.Win32.RegistryValueKind]::ExpandString)
    [Microsoft.Win32.Registry]::SetValue($regPath, 'IconBackgroundColor', [System.String]::Empty, [Microsoft.Win32.RegistryValueKind]::ExpandString)

    # Build out toast XML and display it.
    ($toastXml = [Windows.Data.Xml.Dom.XmlDocument]::new()).LoadXml("<toast launch=`"app-defined-string`"><visual><binding template=`"ToastImageAndText02`"><text id=`"1`">$([System.Security.SecurityElement]::Escape($ToastTitle))</text><text id=`"2`">$([System.Security.SecurityElement]::Escape($ToastText))</text><image id=`"1`" src=`"file://$ToastLogo`" /></binding></visual></toast>")
    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($ToolkitName).Show($toastXml)
}
