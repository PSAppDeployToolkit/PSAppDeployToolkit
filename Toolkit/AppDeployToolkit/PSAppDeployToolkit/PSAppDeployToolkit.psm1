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
New-Variable -Name ADT -Option ReadOnly -Value @{
    CurrentSession = $null
    BannerHeight = $null
    Environment = $null
    Language = $null
    Config = $null
    Strings = $null
    LastExitCode = 0
}

# State data used by Show-InstallationProgress.
New-Variable -Name ProgressWindow -Option Constant -Value @{
    Runspace = $null
    SyncHash = $null
    Running = $false
}

# Variables to track multiple sessions and each session's caller.
New-Variable -Name SessionBuffer -Option Constant -Value ([System.Collections.Generic.List[ADTSession]]::new())
New-Variable -Name SessionCallers -Option Constant -Value @{}

# Values used for ADT module serialisation.
New-Variable -Name Serialisation -Option Constant -Value ([ordered]@{
    KeyName = "HKEY_LOCAL_MACHINE\SOFTWARE\$($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name)"
    ValueName = 'ModuleState'
}).AsReadOnly()

# Logging constants used within an [ADTSession] object.
New-Variable -Name Logging -Option Constant -Value ([ordered]@{
    Formats = ([ordered]@{
        CMTrace = "<![LOG[[{1}] :: {0}]LOG]!><time=`"{2}`" date=`"{3}`" component=`"{4}`" context=`"$([Security.Principal.WindowsIdentity]::GetCurrent().Name)`" type=`"{5}`" thread=`"$PID`" file=`"{6}`">"
        Legacy = '[{1} {2}] [{3}] [{4}] [{5}] :: {0}'
    }).AsReadOnly()
    SeverityNames = [System.Array]::AsReadOnly(@(
        'Success'
        'Info'
        'Warning'
        'Error'
    ))
    SeverityColours = [System.Array]::AsReadOnly(@(
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Green; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
        ([ordered]@{}).AsReadOnly()
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Yellow; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Red; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
    ))
}).AsReadOnly()

# Define exports. It should be done here and in the psd1 to cover all bases.
Export-ModuleMember -Function @(
    'Open-ADTSession'
    'Close-ADTSession'
    'Get-ADTSession'
    'Export-ADTModuleState'
    'Import-ADTModuleState'
    'Block-AppExecution'
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
    'Get-FileVersion'
    'Get-ADTFreeDiskSpace'
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
    'Invoke-ADTAllUsersRegistryChange'
    'Invoke-RegisterOrUnregisterDLL'
    'Invoke-SCCMTask'
    'New-Folder'
    'New-MsiTransform'
    'New-Shortcut'
    'Remove-ContentFromCache'
    'Remove-File'
    'Remove-FileFromUserProfiles'
    'Remove-Folder'
    'Remove-ADTInvalidFileNameChars'
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
    'Show-HelpConsole'
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
    'Unblock-AppExecution'
    'Update-Desktop'
    'Update-GroupPolicy'
    'Update-SessionEnvironmentVariables'
    'Write-ADTLogEntry'
)
