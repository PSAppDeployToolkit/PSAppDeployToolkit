#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-PSDebug -Strict
Set-StrictMode -Version Latest

# Add the custom types required for the toolkit.
Add-Type -LiteralPath "$PSScriptRoot\PSAppDeployToolkit.cs" -ReferencedAssemblies $(
    'System.Drawing', 'System.Windows.Forms', 'System.DirectoryServices'
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        'System.Collections', 'System.Text.RegularExpressions', 'System.Security.Principal.Windows', 'System.ComponentModel.Primitives', 'Microsoft.Win32.Primitives'
    }
)

# Add system types required for the toolkit.
Add-Type -AssemblyName ('System.Drawing', 'System.Windows.Forms', 'PresentationFramework', 'Microsoft.VisualBasic', 'PresentationCore', 'WindowsBase')

# Dot-source our imports.
(Get-ChildItem -Path $PSScriptRoot\*\*.ps1).FullName.ForEach({. $_})

# Define aliases for certain module functions. These need to disappear.
Set-Alias -Name 'Register-DLL' -Value 'Invoke-RegisterOrUnregisterDLL'
Set-Alias -Name 'Unregister-DLL' -Value 'Invoke-RegisterOrUnregisterDLL'
Set-Alias -Name 'Refresh-Desktop' -Value 'Update-Desktop'
Set-Alias -Name 'Refresh-SessionEnvironmentVariables' -Value 'Update-SessionEnvironmentVariables'
if (!(Get-Command -Name 'Get-ScheduledTask')) {New-Alias -Name 'Get-ScheduledTask' -Value 'Get-SchedulerTask'}

# Set process as DPI-aware for better dialog rendering.
[System.Void][PSADT.UiAutomation]::SetProcessDPIAware()

# Define object for holding all PSADT variables.
New-Variable -Name ADT -Option Constant -Value @{
    Sessions = [System.Collections.Generic.List[ADTSession]]::new()
    CurrentSession = $null
    Environment = $null
    Language = $null
    Config = $null
    Strings = $null
    Progress = [ordered]@{
        Runspace = [runspacefactory]::CreateRunspace()
        SyncHash = [hashtable]::Synchronized(@{})
    }
}

# Define exports. It should be done here and in the psd1 to cover all bases.
Export-ModuleMember -Function @(
    'New-ADTSession'
    'Get-ADTSession'
    'Configure-EdgeExtension'
    'Convert-RegistryPath'
    'Copy-ContentToCache'
    'Copy-File'
    'Copy-FileToUserProfiles'
    'Disable-TerminalServerInstallMode'
    'Enable-TerminalServerInstallMode'
    'Execute-MSI'
    'Execute-MSP'
    'Execute-Process'
    'Execute-ProcessAsUser'
    'Exit-Script'
    'Get-FileVersion'
    'Get-FreeDiskSpace'
    'Get-HardwarePlatform'
    'Get-IniValue'
    'Get-InstalledApplication'
    'Get-LoggedOnUser'
    'Get-PendingReboot'
    'Get-RegistryKey'
    'Get-SchedulerTask'
    'Get-ServiceStartMode'
    'Get-Shortcut'
    'Get-SidTypeAccountName'
    'Get-UniversalDate'
    'Get-UserProfiles'
    'Get-WindowTitle'
    'Install-MSUpdates'
    'Install-SCCMSoftwareUpdates'
    'Invoke-HKCURegistrySettingsForAllUsers'
    'Invoke-RegisterOrUnregisterDLL'
    'Invoke-SCCMTask'
    'New-Folder'
    'New-MsiTransform'
    'New-Shortcut'
    'Remove-ContentFromCache'
    'Remove-File'
    'Remove-FileFromUserProfiles'
    'Remove-Folder'
    'Remove-InvalidFileNameChars'
    'Remove-MSIApplications'
    'Remove-RegistryKey'
    'Resolve-Error'
    'Send-Keys'
    'Set-ActiveSetup'
    'Set-IniValue'
    'Set-ItemPermission'
    'Set-PinnedApplication'
    'Set-RegistryKey'
    'Set-ServiceStartMode'
    'Set-Shortcut'
    'Show-BalloonTip'
    'Show-DialogBox'
    'Show-InstallationProgress'
    'Show-InstallationPrompt'
    'Show-InstallationRestartPrompt'
    'Show-InstallationWelcome'
    'Start-ServiceAndDependencies'
    'Stop-ServiceAndDependencies'
    'Test-Battery'
    'Test-MSUpdates'
    'Test-NetworkConnection'
    'Test-PowerPoint'
    'Test-RegistryValue'
    'Test-ServiceExists'
    'Update-Desktop'
    'Update-GroupPolicy'
    'Update-SessionEnvironmentVariables'
    'Write-Log'
)
