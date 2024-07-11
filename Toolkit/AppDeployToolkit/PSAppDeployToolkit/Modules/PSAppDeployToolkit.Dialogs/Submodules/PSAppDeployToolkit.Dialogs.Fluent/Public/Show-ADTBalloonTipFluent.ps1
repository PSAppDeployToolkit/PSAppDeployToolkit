function Show-ADTBalloonTipFluent
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
    Show-ADTBalloonTipFluent -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

    .EXAMPLE
    Show-ADTBalloonTipFluent -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000

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

    # Initialise variables.
    $adtEnv = Get-ADTEnvironment
    $adtConfig = Get-ADTConfig

    # Define script block for toast notifications, pre-injecting variables and values.
    $toastScriptBlock = [System.Management.Automation.ScriptBlock]::Create($ExecutionContext.InvokeCommand.ExpandString({
        # Ensure script runs in strict mode since its in a new scope.
        (Get-Variable -Name ErrorActionPreference).Value = [System.Management.Automation.ActionPreference]::Stop
        (Get-Variable -Name ProgressPreference).Value = [System.Management.Automation.ActionPreference]::SilentlyContinue
        Set-StrictMode -Version 3

        # Add in required assemblies.
        if ((Get-Variable -Name PSVersionTable -ValueOnly).PSEdition.Equals('Core'))
        {
            Add-Type -AssemblyName (Get-ChildItem -Path '$Script:PSScriptRoot\lib\net6.0\*.dll').FullName
        }
        else
        {
            [System.Void][Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]
            [System.Void][Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime]
        }

        # Configure the notification centre.
        Remove-Item -LiteralPath 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\$($adtEnv.appDeployToolkitName)' -Force -Confirm:`$false -ErrorAction Ignore
        [Microsoft.Win32.Registry]::SetValue('HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\$($adtEnv.appDeployToolkitName)', 'ShowInActionCenter', 1, 'DWord')
        [Microsoft.Win32.Registry]::SetValue('HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\$($adtEnv.appDeployToolkitName)', 'Enabled', 1, 'DWord')
        [Microsoft.Win32.Registry]::SetValue('HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\$($adtEnv.appDeployToolkitName)', 'SoundFile', '', 'String')

        # Configure the toast notification.
        Remove-Item -LiteralPath 'Registry::HKEY_CURRENT_USER\Software\Classes\AppUserModelId\$($adtEnv.appDeployToolkitName)' -Force -Confirm:`$false -ErrorAction Ignore
        [Microsoft.Win32.Registry]::SetValue('HKEY_CURRENT_USER\Software\Classes\AppUserModelId\$($adtEnv.appDeployToolkitName)', 'DisplayName', '$($adtConfig.UI.ToastName)', 'String')
        [Microsoft.Win32.Registry]::SetValue('HKEY_CURRENT_USER\Software\Classes\AppUserModelId\$($adtEnv.appDeployToolkitName)', 'ShowInSettings', 0, 'DWord')
        [Microsoft.Win32.Registry]::SetValue('HKEY_CURRENT_USER\Software\Classes\AppUserModelId\$($adtEnv.appDeployToolkitName)', 'IconUri', '$($adtConfig.Assets.Logo)', 'ExpandString')
        [Microsoft.Win32.Registry]::SetValue('HKEY_CURRENT_USER\Software\Classes\AppUserModelId\$($adtEnv.appDeployToolkitName)', 'IconBackgroundColor', '', 'ExpandString')

        # Build out toast XML and display it.
        (New-Variable -Name toastXml -Value ([Windows.Data.Xml.Dom.XmlDocument]::new()) -PassThru).Value.LoadXml('<toast launch="app-defined-string"><visual><binding template="ToastImageAndText02"><text id="1">$BalloonTipTitle</text><text id="2">$BalloonTipText</text><image id="1" src="file://$($adtConfig.Assets.Logo)" /></binding></visual></toast>')
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('$($adtEnv.appDeployToolkitName)').Show((Get-Variable -Name toastXml -ValueOnly))
    }))

    # If we're running as the active user, display directly; otherwise, run via Execute-ProcessAsUser.
    if ($adtEnv.ProcessNTAccount -eq $adtEnv.runAsActiveUser.NTAccount)
    {
        Write-ADTLogEntry -Message "Displaying toast notification with message [$BalloonTipText]."
        & $toastScriptBlock
    }
    else
    {
        Write-ADTLogEntry -Message "Displaying toast notification with message [$BalloonTipText] using Execute-ProcessAsUser."
        Execute-ProcessAsUser -Path $adtEnv.envPSProcessPath -Parameters "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $([System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes([System.String]::Join("`n", $toastScriptBlock.ToString().Trim().Split("`n").Trim()))))" -Wait -RunLevel LeastPrivilege
    }
}
