<#
.SYNOPSIS

PSApppDeployToolkit - This script contains the PSADT core runtime and functions using by a Deploy-Application.ps1 script.

.DESCRIPTION

The script can be called directly to dot-source the toolkit functions for testing, but it is usually called by the Deploy-Application.ps1 script.

The script can usually be updated to the latest version without impacting your per-application Deploy-Application scripts. Please check release notes before upgrading.

PSApppDeployToolkit is licensed under the GNU LGPLv3 License - (C) 2023 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham and Muhammad Mashwani).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.PARAMETER CleanupBlockedApps

Clean up the blocked applications.

This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ShowBlockedAppDialog

Display a dialog box showing that the application execution is blocked.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ReferredInstallName

Name of the referring application that invoked the script externally.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ReferredInstallTitle

Title of the referring application that invoked the script externally.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ReferredLogname

Logfile name of the referring application that invoked the script externally.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER AsyncToolkitLaunch

This parameter is passed to the script when it is being called externally, e.g. from a scheduled task or asynchronously.

.INPUTS

None

You cannot pipe objects to this script.

.OUTPUTS

None

This script does not generate any output.

.NOTES

The other parameters specified for this script that are not documented in this help section are for use only by functions in this script that call themselves by running this script again asynchronously.

.LINK

https://psappdeploytoolkit.com
#>


[CmdletBinding()]
Param (
    ## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or because of an Image File Execution Options registry setting
    [Switch]$ShowInstallationPrompt = $false,
    [Switch]$ShowInstallationRestartPrompt = $false,
    [Switch]$CleanupBlockedApps = $false,
    [Switch]$ShowBlockedAppDialog = $false,
    [Switch]$DisableLogging = $false,
    [String]$ReferredInstallName = '',
    [String]$ReferredInstallTitle = '',
    [String]$ReferredLogName = '',
    [String]$Title = '',
    [String]$Message = '',
    [String]$MessageAlignment = '',
    [String]$ButtonRightText = '',
    [String]$ButtonLeftText = '',
    [String]$ButtonMiddleText = '',
    [String]$Icon = '',
    [String]$Timeout = '',
    [Switch]$ExitOnTimeout = $false,
    [Boolean]$MinimizeWindows = $false,
    [Switch]$PersistPrompt = $false,
    [Int32]$CountdownSeconds = 60,
    [Int32]$CountdownNoHideSeconds = 30,
    [Switch]$NoCountdown = $false,
    [Switch]$AsyncToolkitLaunch = $false,
    [Boolean]$TopMost = $true
)

##*=============================================
##* VARIABLE DECLARATION
##*=============================================
#region VariableDeclaration

## Variables: Toolkit Name
[String]$appDeployToolkitName = 'PSAppDeployToolkit'
[String]$appDeployMainScriptFriendlyName = 'App Deploy Toolkit Main'

## Variables: Script Info
[Version]$appDeployMainScriptVersion = [Version]'3.9.1'
[Version]$appDeployMainScriptMinimumConfigVersion = [Version]'3.9.0'
[String]$appDeployMainScriptDate = '20/01/2023'
[Hashtable]$appDeployMainScriptParameters = $PSBoundParameters

## Variables: Datetime and Culture
[DateTime]$currentDateTime = Get-Date
[String]$currentTime = Get-Date -Date $currentDateTime -UFormat '%T'
[String]$currentDate = Get-Date -Date $currentDateTime -UFormat '%d-%m-%Y'
[Timespan]$currentTimeZoneBias = [TimeZone]::CurrentTimeZone.GetUtcOffset($currentDateTime)
[Globalization.CultureInfo]$culture = Get-Culture
[String]$currentLanguage = $culture.TwoLetterISOLanguageName.ToUpper()
[Globalization.CultureInfo]$uiculture = Get-UICulture
[String]$currentUILanguage = $uiculture.TwoLetterISOLanguageName.ToUpper()

## Variables: Environment Variables
[PSObject]$envHost = $Host
[PSObject]$envShellFolders = Get-ItemProperty -Path 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -ErrorAction 'SilentlyContinue'
[String]$envAllUsersProfile = $env:ALLUSERSPROFILE
[String]$envAppData = [Environment]::GetFolderPath('ApplicationData')
[String]$envArchitecture = $env:PROCESSOR_ARCHITECTURE
[String]$envCommonDesktop = $envShellFolders | Select-Object -ExpandProperty 'Common Desktop' -ErrorAction 'SilentlyContinue'
[String]$envCommonDocuments = $envShellFolders | Select-Object -ExpandProperty 'Common Documents' -ErrorAction 'SilentlyContinue'
[String]$envCommonStartMenuPrograms = $envShellFolders | Select-Object -ExpandProperty 'Common Programs' -ErrorAction 'SilentlyContinue'
[String]$envCommonStartMenu = $envShellFolders | Select-Object -ExpandProperty 'Common Start Menu' -ErrorAction 'SilentlyContinue'
[String]$envCommonStartUp = $envShellFolders | Select-Object -ExpandProperty 'Common Startup' -ErrorAction 'SilentlyContinue'
[String]$envCommonTemplates = $envShellFolders | Select-Object -ExpandProperty 'Common Templates' -ErrorAction 'SilentlyContinue'
[String]$envComputerName = [Environment]::MachineName.ToUpper()
[String]$envHomeDrive = $env:HOMEDRIVE
[String]$envHomePath = $env:HOMEPATH
[String]$envHomeShare = $env:HOMESHARE
[String]$envLocalAppData = [Environment]::GetFolderPath('LocalApplicationData')
[String[]]$envLogicalDrives = [Environment]::GetLogicalDrives()
[String]$envProgramData = [Environment]::GetFolderPath('CommonApplicationData')
[String]$envPublic = $env:PUBLIC
[String]$envSystemDrive = $env:SYSTEMDRIVE
[String]$envSystemRoot = $env:SYSTEMROOT
[String]$envTemp = [IO.Path]::GetTempPath()
[String]$envUserCookies = [Environment]::GetFolderPath('Cookies')
[String]$envUserDesktop = [Environment]::GetFolderPath('DesktopDirectory')
[String]$envUserFavorites = [Environment]::GetFolderPath('Favorites')
[String]$envUserInternetCache = [Environment]::GetFolderPath('InternetCache')
[String]$envUserInternetHistory = [Environment]::GetFolderPath('History')
[String]$envUserMyDocuments = [Environment]::GetFolderPath('MyDocuments')
[String]$envUserName = [Environment]::UserName
[String]$envUserPictures = [Environment]::GetFolderPath('MyPictures')
[String]$envUserProfile = $env:USERPROFILE
[String]$envUserSendTo = [Environment]::GetFolderPath('SendTo')
[String]$envUserStartMenu = [Environment]::GetFolderPath('StartMenu')
[String]$envUserStartMenuPrograms = [Environment]::GetFolderPath('Programs')
[String]$envUserStartUp = [Environment]::GetFolderPath('StartUp')
[String]$envUserTemplates = [Environment]::GetFolderPath('Templates')
[String]$envSystem32Directory = [Environment]::SystemDirectory
[String]$envWinDir = $env:WINDIR

## Variables: Domain Membership
[Boolean]$IsMachinePartOfDomain = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'SilentlyContinue').PartOfDomain
[String]$envMachineWorkgroup = ''
[String]$envMachineADDomain = ''
[String]$envLogonServer = ''
[String]$MachineDomainController = ''
[String]$envComputerNameFQDN = $envComputerName
If ($IsMachinePartOfDomain) {
    [String]$envMachineADDomain = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'SilentlyContinue').Domain | Where-Object { $_ } | ForEach-Object { $_.ToLower() }
    Try {
        $envComputerNameFQDN = ([Net.Dns]::GetHostEntry('localhost')).HostName
    }
    Catch {
        # Function GetHostEntry failed, but we can construct the FQDN in another way
        $envComputerNameFQDN = $envComputerNameFQDN + '.' + $envMachineADDomain
    }

    Try {
        [String]$envLogonServer = $env:LOGONSERVER | Where-Object { (($_) -and (-not $_.Contains('\\MicrosoftAccount'))) } | ForEach-Object { $_.TrimStart('\') } | ForEach-Object { ([Net.Dns]::GetHostEntry($_)).HostName }
    }
    Catch {
    }
    # If running in system context or if GetHostEntry fails, fall back on the logonserver value stored in the registry
    If (-not $envLogonServer) {
        [String]$envLogonServer = Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\History' -ErrorAction 'SilentlyContinue' | Select-Object -ExpandProperty 'DCName' -ErrorAction 'SilentlyContinue'
    }
    ## Remove backslashes at the beginning
    While ($envLogonServer.StartsWith('\')) {
        $envLogonServer = $envLogonServer.Substring(1)
    }

    Try {
        [String]$MachineDomainController = [DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain().FindDomainController().Name
    }
    Catch {
    }
}
Else {
    [String]$envMachineWorkgroup = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'SilentlyContinue').Domain | Where-Object { $_ } | ForEach-Object { $_.ToUpper() }
}
[String]$envMachineDNSDomain = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().DomainName | Where-Object { $_ } | ForEach-Object { $_.ToLower() }
[String]$envUserDNSDomain = $env:USERDNSDOMAIN | Where-Object { $_ } | ForEach-Object { $_.ToLower() }
Try {
    [String]$envUserDomain = [Environment]::UserDomainName.ToUpper()
}
Catch {
}

## Variables: Operating System
[PSObject]$envOS = Get-WmiObject -Class 'Win32_OperatingSystem' -ErrorAction 'SilentlyContinue'
[String]$envOSName = $envOS.Caption.Trim()
[String]$envOSServicePack = $envOS.CSDVersion
[Version]$envOSVersion = $envOS.Version
[String]$envOSVersionMajor = $envOSVersion.Major
[String]$envOSVersionMinor = $envOSVersion.Minor
[String]$envOSVersionBuild = $envOSVersion.Build
If ((Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -ErrorAction 'SilentlyContinue').PSObject.Properties.Name -contains 'UBR') {
    [String]$envOSVersionRevision = (Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'UBR' -ErrorAction 'SilentlyContinue').UBR
}
ElseIf ((Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -ErrorAction 'SilentlyContinue').PSObject.Properties.Name -contains 'BuildLabEx') {
    [String]$envOSVersionRevision = , ((Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'BuildLabEx' -ErrorAction 'SilentlyContinue').BuildLabEx -split '\.') | ForEach-Object { $_[1] }
}
If ($envOSVersionRevision -notmatch '^[\d\.]+$') { $envOSVersionRevision = '' }
If ($envOSVersionRevision) { [string]$envOSVersion = "$($envOSVersion.ToString()).$envOSVersionRevision" } Else { [string]$envOSVersion = "$($envOSVersion.ToString())" }
#  Get the operating system type
[int32]$envOSProductType = $envOS.ProductType
[boolean]$IsServerOS = [boolean]($envOSProductType -eq 3)
[boolean]$IsDomainControllerOS = [boolean]($envOSProductType -eq 2)
[boolean]$IsWorkStationOS = [boolean]($envOSProductType -eq 1)
[boolean]$IsMultiSessionOS = [boolean]($envOSName -match '^Microsoft Windows \d+ Enterprise for Virtual Desktops$')
Switch ($envOSProductType) {
	3 { [string]$envOSProductTypeName = 'Server' }
	2 { [string]$envOSProductTypeName = 'Domain Controller' }
	1 { [string]$envOSProductTypeName = 'Workstation' }
	Default { [string]$envOSProductTypeName = 'Unknown' }
}
#  Get the OS Architecture
[Boolean]$Is64Bit = [Boolean]((Get-WmiObject -Class 'Win32_Processor' -ErrorAction 'SilentlyContinue' | Where-Object { $_.DeviceID -eq 'CPU0' } | Select-Object -ExpandProperty 'AddressWidth') -eq 64)
If ($Is64Bit) {
    [String]$envOSArchitecture = '64-bit'
}
Else {
    [String]$envOSArchitecture = '32-bit'
}

## Variables: Current Process Architecture
[Boolean]$Is64BitProcess = [Boolean]([IntPtr]::Size -eq 8)
If ($Is64BitProcess) {
    [String]$psArchitecture = 'x64'
}
Else {
    [String]$psArchitecture = 'x86'
}

## Variables: Get Normalized ProgramFiles and CommonProgramFiles Paths
[String]$envProgramFiles = ''
[String]$envProgramFilesX86 = ''
[String]$envCommonProgramFiles = ''
[String]$envCommonProgramFilesX86 = ''
If ($Is64Bit) {
    If ($Is64BitProcess) {
        [String]$envProgramFiles = [Environment]::GetFolderPath('ProgramFiles')
        [String]$envCommonProgramFiles = [Environment]::GetFolderPath('CommonProgramFiles')
    }
    Else {
        [String]$envProgramFiles = [Environment]::GetEnvironmentVariable('ProgramW6432')
        [String]$envCommonProgramFiles = [Environment]::GetEnvironmentVariable('CommonProgramW6432')
    }
    ## Powershell 2 doesn't support X86 folders so need to use variables instead
    Try {
        [String]$envProgramFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
        [String]$envCommonProgramFilesX86 = [Environment]::GetFolderPath('CommonProgramFilesX86')
    }
    Catch {
        [String]$envProgramFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
        [String]$envCommonProgramFilesX86 = [Environment]::GetEnvironmentVariable('CommonProgramFiles(x86)')
    }
}
Else {
    [String]$envProgramFiles = [Environment]::GetFolderPath('ProgramFiles')
    [String]$envProgramFilesX86 = $envProgramFiles
    [String]$envCommonProgramFiles = [Environment]::GetFolderPath('CommonProgramFiles')
    [String]$envCommonProgramFilesX86 = $envCommonProgramFiles
}

## Variables: Hardware
[Int32]$envSystemRAM = Get-WmiObject -Class 'Win32_PhysicalMemory' -ErrorAction 'SilentlyContinue' | Measure-Object -Property 'Capacity' -Sum -ErrorAction 'SilentlyContinue' | ForEach-Object { [Math]::Round(($_.Sum / 1GB), 2) }

## Variables: PowerShell And CLR (.NET) Versions
[Hashtable]$envPSVersionTable = $PSVersionTable
#  PowerShell Version
[Version]$envPSVersion = $envPSVersionTable.PSVersion
[String]$envPSVersionMajor = $envPSVersion.Major
[String]$envPSVersionMinor = $envPSVersion.Minor
[String]$envPSVersionBuild = $envPSVersion.Build
[String]$envPSVersionRevision = $envPSVersion.Revision
[String]$envPSVersion = $envPSVersion.ToString()
#  CLR (.NET) Version used by PowerShell
[Version]$envCLRVersion = $envPSVersionTable.CLRVersion
[String]$envCLRVersionMajor = $envCLRVersion.Major
[String]$envCLRVersionMinor = $envCLRVersion.Minor
[String]$envCLRVersionBuild = $envCLRVersion.Build
[String]$envCLRVersionRevision = $envCLRVersion.Revision
[String]$envCLRVersion = $envCLRVersion.ToString()

## Variables: Permissions/Accounts
[Security.Principal.WindowsIdentity]$CurrentProcessToken = [Security.Principal.WindowsIdentity]::GetCurrent()
[Security.Principal.SecurityIdentifier]$CurrentProcessSID = $CurrentProcessToken.User
[String]$ProcessNTAccount = $CurrentProcessToken.Name
[String]$ProcessNTAccountSID = $CurrentProcessSID.Value
[Boolean]$IsAdmin = [Boolean]($CurrentProcessToken.Groups -contains [Security.Principal.SecurityIdentifier]'S-1-5-32-544')
[Boolean]$IsLocalSystemAccount = $CurrentProcessSID.IsWellKnown([Security.Principal.WellKnownSidType]'LocalSystemSid')
[Boolean]$IsLocalServiceAccount = $CurrentProcessSID.IsWellKnown([Security.Principal.WellKnownSidType]'LocalServiceSid')
[Boolean]$IsNetworkServiceAccount = $CurrentProcessSID.IsWellKnown([Security.Principal.WellKnownSidType]'NetworkServiceSid')
[Boolean]$IsServiceAccount = [Boolean]($CurrentProcessToken.Groups -contains [Security.Principal.SecurityIdentifier]'S-1-5-6')
[Boolean]$IsProcessUserInteractive = [Environment]::UserInteractive
$GetAccountNameUsingSid = [ScriptBlock] {
    Param (
        [String]$SecurityIdentifier = $null
    )

    Try {
        Return (New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ([Security.Principal.WellKnownSidType]::"$SecurityIdentifier", $null)).Translate([System.Security.Principal.NTAccount]).Value
    }
    Catch {
        Return ($null)
    }
}
[String]$LocalSystemNTAccount = & $GetAccountNameUsingSid 'LocalSystemSid'
[String]$LocalUsersGroup = & $GetAccountNameUsingSid 'BuiltinUsersSid'
[String]$LocalPowerUsersGroup = & $GetAccountNameUsingSid 'BuiltinPowerUsersSid'
[String]$LocalAdministratorsGroup = & $GetAccountNameUsingSid 'BuiltinAdministratorsSid'
#  Check if script is running in session zero
If ($IsLocalSystemAccount -or $IsLocalServiceAccount -or $IsNetworkServiceAccount -or $IsServiceAccount) {
    $SessionZero = $true
}
Else {
    $SessionZero = $false
}

## Variables: Script Name and Script Paths
[String]$scriptPath = $MyInvocation.MyCommand.Definition
[String]$scriptName = [IO.Path]::GetFileNameWithoutExtension($scriptPath)
[String]$scriptFileName = Split-Path -Path $scriptPath -Leaf
[String]$scriptRoot = Split-Path -Path $scriptPath -Parent
[String]$invokingScript = (Get-Variable -Name 'MyInvocation').Value.ScriptName
#  Get the invoking script directory
If ($invokingScript) {
    #  If this script was invoked by another script
    [String]$scriptParentPath = Split-Path -Path $invokingScript -Parent
}
Else {
    #  If this script was not invoked by another script, fall back to the directory one level above this script
    [String]$scriptParentPath = (Get-Item -LiteralPath $scriptRoot).Parent.FullName
}

## Variables: App Deploy Script Dependency Files
[String]$appDeployConfigFile = Join-Path -Path $scriptRoot -ChildPath 'AppDeployToolkitConfig.xml'
[String]$appDeployCustomTypesSourceCode = Join-Path -Path $scriptRoot -ChildPath 'AppDeployToolkitMain.cs'
If (-not (Test-Path -LiteralPath $appDeployConfigFile -PathType 'Leaf')) {
    Throw 'App Deploy XML configuration file not found.'
}
If (-not (Test-Path -LiteralPath $appDeployCustomTypesSourceCode -PathType 'Leaf')) {
    Throw 'App Deploy custom types source code file not found.'
}

#  App Deploy Optional Extensions File
[String]$appDeployToolkitDotSourceExtensions = 'AppDeployToolkitExtensions.ps1'

## Import variables from XML configuration file
[Xml.XmlDocument]$xmlConfigFile = Get-Content -LiteralPath $AppDeployConfigFile -Encoding 'UTF8'
[Xml.XmlElement]$xmlConfig = $xmlConfigFile.AppDeployToolkit_Config
#  Get Config File Details
[Xml.XmlElement]$configConfigDetails = $xmlConfig.Config_File
[String]$configConfigVersion = [Version]$configConfigDetails.Config_Version
[String]$configConfigDate = $configConfigDetails.Config_Date

# Get Banner and Icon details
[Xml.XmlElement]$xmlBannerIconOptions = $xmlConfig.BannerIcon_Options
[String]$configBannerIconFileName = $xmlBannerIconOptions.Icon_Filename
[String]$configBannerLogoImageFileName = $xmlBannerIconOptions.LogoImage_Filename
[String]$configBannerIconBannerName = $xmlBannerIconOptions.Banner_Filename
[Int32]$appDeployLogoBannerMaxHeight = $xmlBannerIconOptions.Banner_MaxHeight

[String]$appDeployLogoIcon = Join-Path -Path $scriptRoot -ChildPath $configBannerIconFileName
[String]$appDeployLogoImage = Join-Path -Path $scriptRoot -ChildPath $configBannerLogoImageFileName
[String]$appDeployLogoBanner = Join-Path -Path $scriptRoot -ChildPath $configBannerIconBannerName
#  Check that dependency files are present
If (-not (Test-Path -LiteralPath $appDeployLogoIcon -PathType 'Leaf')) {
    Throw 'App Deploy logo icon file not found.'
}
If (-not (Test-Path -LiteralPath $appDeployLogoBanner -PathType 'Leaf')) {
    Throw 'App Deploy logo banner file not found.'
}

#  Get Toolkit Options
[Xml.XmlElement]$xmlToolkitOptions = $xmlConfig.Toolkit_Options
[Boolean]$configToolkitRequireAdmin = [Boolean]::Parse($xmlToolkitOptions.Toolkit_RequireAdmin)
[String]$configToolkitTempPath = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_TempPath)
[String]$configToolkitRegPath = $xmlToolkitOptions.Toolkit_RegPath
[String]$configToolkitLogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_LogPath)
[Boolean]$configToolkitCompressLogs = [Boolean]::Parse($xmlToolkitOptions.Toolkit_CompressLogs)
[String]$configToolkitLogStyle = $xmlToolkitOptions.Toolkit_LogStyle
[Double]$configToolkitLogMaxSize = $xmlToolkitOptions.Toolkit_LogMaxSize
[Boolean]$configToolkitLogWriteToHost = [Boolean]::Parse($xmlToolkitOptions.Toolkit_LogWriteToHost)
[Boolean]$configToolkitLogDebugMessage = [Boolean]::Parse($xmlToolkitOptions.Toolkit_LogDebugMessage)
#  Get MSI Options
[Xml.XmlElement]$xmlConfigMSIOptions = $xmlConfig.MSI_Options
[String]$configMSILoggingOptions = $xmlConfigMSIOptions.MSI_LoggingOptions
[String]$configMSIInstallParams = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_InstallParams)
[String]$configMSISilentParams = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_SilentParams)
[String]$configMSIUninstallParams = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_UninstallParams)
[String]$configMSILogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_LogPath)
[Int32]$configMSIMutexWaitTime = $xmlConfigMSIOptions.MSI_MutexWaitTime
#  Change paths to user accessible ones if RequireAdmin is false
If ($configToolkitRequireAdmin -eq $false) {
    If ($xmlToolkitOptions.Toolkit_TempPathNoAdminRights) {
        [String]$configToolkitTempPath = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_TempPathNoAdminRights)
    }
    If ($xmlToolkitOptions.Toolkit_RegPathNoAdminRights) {
        [String]$configToolkitRegPath = $xmlToolkitOptions.Toolkit_RegPathNoAdminRights
    }
    If ($xmlToolkitOptions.Toolkit_LogPathNoAdminRights) {
        [String]$configToolkitLogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_LogPathNoAdminRights)
    }
    If ($xmlConfigMSIOptions.MSI_LogPathNoAdminRights) {
        [String]$configMSILogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_LogPathNoAdminRights)
    }
}
#  Get UI Options
[Xml.XmlElement]$xmlConfigUIOptions = $xmlConfig.UI_Options
[String]$configInstallationUILanguageOverride = $xmlConfigUIOptions.InstallationUI_LanguageOverride
[Boolean]$configShowBalloonNotifications = [Boolean]::Parse($xmlConfigUIOptions.ShowBalloonNotifications)
[Int32]$configInstallationUITimeout = $xmlConfigUIOptions.InstallationUI_Timeout
[Int32]$configInstallationUIExitCode = $xmlConfigUIOptions.InstallationUI_ExitCode
[Int32]$configInstallationDeferExitCode = $xmlConfigUIOptions.InstallationDefer_ExitCode
[Int32]$configInstallationPersistInterval = $xmlConfigUIOptions.InstallationPrompt_PersistInterval
[Int32]$configInstallationRestartPersistInterval = $xmlConfigUIOptions.InstallationRestartPrompt_PersistInterval
[Int32]$configInstallationPromptToSave = $xmlConfigUIOptions.InstallationPromptToSave_Timeout
[Boolean]$configInstallationWelcomePromptDynamicRunningProcessEvaluation = [Boolean]::Parse($xmlConfigUIOptions.InstallationWelcomePrompt_DynamicRunningProcessEvaluation)
[Int32]$configInstallationWelcomePromptDynamicRunningProcessEvaluationInterval = $xmlConfigUIOptions.InstallationWelcomePrompt_DynamicRunningProcessEvaluationInterval
#  Define ScriptBlock for Loading Message UI Language Options (default for English if no localization found)
[ScriptBlock]$xmlLoadLocalizedUIMessages = {
    #  If a user is logged on, then get primary UI language for logged on user (even if running in session 0)
    If ($RunAsActiveUser) {
        #  Read language defined by Group Policy
        If (-not $HKULanguages) {
            [String[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MUI\Settings' -Value 'PreferredUILanguages'
        }
        If (-not $HKULanguages) {
            [String[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Control Panel\Desktop' -Value 'PreferredUILanguages' -SID $RunAsActiveUser.SID
        }
        #  Read language for Win Vista & higher machines
        If (-not $HKULanguages) {
            [String[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop' -Value 'PreferredUILanguages' -SID $RunAsActiveUser.SID
        }
        If (-not $HKULanguages) {
            [String[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop\MuiCached' -Value 'MachinePreferredUILanguages' -SID $RunAsActiveUser.SID
        }
        If (-not $HKULanguages) {
            [String[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\International' -Value 'LocaleName' -SID $RunAsActiveUser.SID
        }
        #  Read language for Win XP machines
        If (-not $HKULanguages) {
            [String]$HKULocale = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\International' -Value 'Locale' -SID $RunAsActiveUser.SID
            If ($HKULocale) {
                [Int32]$HKULocale = [Convert]::ToInt32('0x' + $HKULocale, 16)
                [String[]]$HKULanguages = ([Globalization.CultureInfo]($HKULocale)).Name
            }
        }
        If ($HKULanguages) {
            [Globalization.CultureInfo]$PrimaryWindowsUILanguage = [Globalization.CultureInfo]($HKULanguages[0])
            [String]$HKUPrimaryLanguageShort = $PrimaryWindowsUILanguage.TwoLetterISOLanguageName.ToUpper()

            #  If the detected language is Chinese, determine if it is simplified or traditional Chinese
            If ($HKUPrimaryLanguageShort -eq 'ZH') {
                If ($PrimaryWindowsUILanguage.EnglishName -match 'Simplified') {
                    [String]$HKUPrimaryLanguageShort = 'ZH-Hans'
                }
                If ($PrimaryWindowsUILanguage.EnglishName -match 'Traditional') {
                    [String]$HKUPrimaryLanguageShort = 'ZH-Hant'
                }
            }

            #  If the detected language is Portuguese, determine if it is Brazilian Portuguese
            If ($HKUPrimaryLanguageShort -eq 'PT') {
                If ($PrimaryWindowsUILanguage.ThreeLetterWindowsLanguageName -eq 'PTB') {
                    [String]$HKUPrimaryLanguageShort = 'PT-BR'
                }
            }
        }
    }

    If ($HKUPrimaryLanguageShort) {
        #  Use the primary UI language of the logged in user
        [String]$xmlUIMessageLanguage = "UI_Messages_$HKUPrimaryLanguageShort"
    }
    Else {
        #  Default to UI language of the account executing current process (even if it is the SYSTEM account)
        [String]$xmlUIMessageLanguage = "UI_Messages_$currentLanguage"
    }
    #  Default to English if the detected UI language is not available in the XMl config file
    If (-not ($xmlConfig.$xmlUIMessageLanguage)) {
        [String]$xmlUIMessageLanguage = 'UI_Messages_EN'
    }
    #  Override the detected language if the override option was specified in the XML config file
    If ($configInstallationUILanguageOverride) {
        [String]$xmlUIMessageLanguage = "UI_Messages_$configInstallationUILanguageOverride"
    }

    [Xml.XmlElement]$xmlUIMessages = $xmlConfig.$xmlUIMessageLanguage
    [String]$configDiskSpaceMessage = $xmlUIMessages.DiskSpace_Message
    [String]$configBalloonTextStart = $xmlUIMessages.BalloonText_Start
    [String]$configBalloonTextComplete = $xmlUIMessages.BalloonText_Complete
    [String]$configBalloonTextRestartRequired = $xmlUIMessages.BalloonText_RestartRequired
    [String]$configBalloonTextFastRetry = $xmlUIMessages.BalloonText_FastRetry
    [String]$configBalloonTextError = $xmlUIMessages.BalloonText_Error
    [String]$configProgressMessageInstall = $xmlUIMessages.Progress_MessageInstall
    [String]$configProgressMessageUninstall = $xmlUIMessages.Progress_MessageUninstall
    [String]$configProgressMessageRepair = $xmlUIMessages.Progress_MessageRepair
    [String]$configClosePromptMessage = $xmlUIMessages.ClosePrompt_Message
    [String]$configClosePromptButtonClose = $xmlUIMessages.ClosePrompt_ButtonClose
    [String]$configClosePromptButtonDefer = $xmlUIMessages.ClosePrompt_ButtonDefer
    [String]$configClosePromptButtonContinue = $xmlUIMessages.ClosePrompt_ButtonContinue
    [String]$configClosePromptButtonContinueTooltip = $xmlUIMessages.ClosePrompt_ButtonContinueTooltip
    [String]$configClosePromptCountdownMessage = $xmlUIMessages.ClosePrompt_CountdownMessage
    [String]$configDeferPromptWelcomeMessage = $xmlUIMessages.DeferPrompt_WelcomeMessage
    [String]$configDeferPromptExpiryMessage = $xmlUIMessages.DeferPrompt_ExpiryMessage
    [String]$configDeferPromptWarningMessage = $xmlUIMessages.DeferPrompt_WarningMessage
    [String]$configDeferPromptRemainingDeferrals = $xmlUIMessages.DeferPrompt_RemainingDeferrals
    [String]$configDeferPromptDeadline = $xmlUIMessages.DeferPrompt_Deadline
    [String]$configBlockExecutionMessage = $xmlUIMessages.BlockExecution_Message
    [String]$configDeploymentTypeInstall = $xmlUIMessages.DeploymentType_Install
    [String]$configDeploymentTypeUnInstall = $xmlUIMessages.DeploymentType_UnInstall
    [String]$configDeploymentTypeRepair = $xmlUIMessages.DeploymentType_Repair
    [String]$configRestartPromptTitle = $xmlUIMessages.RestartPrompt_Title
    [String]$configRestartPromptMessage = $xmlUIMessages.RestartPrompt_Message
    [String]$configRestartPromptMessageTime = $xmlUIMessages.RestartPrompt_MessageTime
    [String]$configRestartPromptMessageRestart = $xmlUIMessages.RestartPrompt_MessageRestart
    [String]$configRestartPromptTimeRemaining = $xmlUIMessages.RestartPrompt_TimeRemaining
    [String]$configRestartPromptButtonRestartLater = $xmlUIMessages.RestartPrompt_ButtonRestartLater
    [String]$configRestartPromptButtonRestartNow = $xmlUIMessages.RestartPrompt_ButtonRestartNow
    [String]$configWelcomePromptCountdownMessage = $xmlUIMessages.WelcomePrompt_CountdownMessage
    [String]$configWelcomePromptCustomMessage = $xmlUIMessages.WelcomePrompt_CustomMessage
}

## Variables: Script Directories
[String]$dirFiles = Join-Path -Path $scriptParentPath -ChildPath 'Files'
[String]$dirSupportFiles = Join-Path -Path $scriptParentPath -ChildPath 'SupportFiles'
[String]$dirAppDeployTemp = Join-Path -Path $configToolkitTempPath -ChildPath $appDeployToolkitName

If (-not (Test-Path -LiteralPath $dirAppDeployTemp -PathType 'Container' -ErrorAction 'SilentlyContinue')) {
    New-Item -Path $dirAppDeployTemp -ItemType 'Directory' -Force -ErrorAction 'SilentlyContinue'
}

## Set the deployment type to "Install" if it has not been specified
If (-not $deploymentType) {
    [String]$deploymentType = 'Install'
}

## Variables: Executables
[String]$exeWusa = "$envWinDir\System32\wusa.exe" # Installs Standalone Windows Updates
[String]$exeMsiexec = "$envWinDir\System32\msiexec.exe" # Installs MSI Installers
[String]$exeSchTasks = "$envWinDir\System32\schtasks.exe" # Manages Scheduled Tasks

## Variables: RegEx Patterns
[String]$MSIProductCodeRegExPattern = '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$'

## Variables: Invalid FileName Characters
[Char[]]$invalidFileNameChars = [IO.Path]::GetinvalidFileNameChars()

## Variables: Registry Keys
#  Registry keys for native and WOW64 applications
[String[]]$regKeyApplications = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall', 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
If ($is64Bit) {
    [String]$regKeyLotusNotes = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Lotus\Notes'
}
Else {
    [String]$regKeyLotusNotes = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Lotus\Notes'
}
[String]$regKeyAppExecution = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options'

## COM Objects: Initialize
[__ComObject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'SilentlyContinue'
[__ComObject]$ShellApp = New-Object -ComObject 'Shell.Application' -ErrorAction 'SilentlyContinue'

## Variables: Reset/Remove Variables
[Boolean]$msiRebootDetected = $false
[Boolean]$BlockExecution = $false
[Boolean]$installationStarted = $false
[Boolean]$runningTaskSequence = $false
If (Test-Path -LiteralPath 'variable:welcomeTimer') {
    Remove-Variable -Name 'welcomeTimer' -Scope 'Script'
}
#  Reset the deferral history
If (Test-Path -LiteralPath 'variable:deferHistory') {
    Remove-Variable -Name 'deferHistory'
}
If (Test-Path -LiteralPath 'variable:deferTimes') {
    Remove-Variable -Name 'deferTimes'
}
If (Test-Path -LiteralPath 'variable:deferDays') {
    Remove-Variable -Name 'deferDays'
}

## Variables: System DPI Scale Factor (Requires PSADT.UiAutomation loaded)
[ScriptBlock]$GetDisplayScaleFactor = {
    #  If a user is logged on, then get display scale factor for logged on user (even if running in session 0)
    [Boolean]$UserDisplayScaleFactor = $false
    [System.Drawing.Graphics]$GraphicsObject = $null
    [IntPtr]$DeviceContextHandle = [IntPtr]::Zero
    [Int32]$dpiScale = 0
    [Int32]$dpiPixels = 0

    Try {
        # Get Graphics Object from the current Window Handle
        [System.Drawing.Graphics]$GraphicsObject = [System.Drawing.Graphics]::FromHwnd([IntPtr]::Zero)
        # Get Device Context Handle
        [IntPtr]$DeviceContextHandle = $GraphicsObject.GetHdc()
        # Get Logical and Physical screen height
        [Int32]$LogicalScreenHeight = [PSADT.UiAutomation]::GetDeviceCaps($DeviceContextHandle, [Int32][PSADT.UiAutomation+DeviceCap]::VERTRES)
        [Int32]$PhysicalScreenHeight = [PSADT.UiAutomation]::GetDeviceCaps($DeviceContextHandle, [Int32][PSADT.UiAutomation+DeviceCap]::DESKTOPVERTRES)
        # Calculate dpi scale and pixels
        [Int32]$dpiScale = [Math]::Round([Double]$PhysicalScreenHeight / [Double]$LogicalScreenHeight, 2) * 100
        [Int32]$dpiPixels = [Math]::Round(($dpiScale / 100) * 96, 0)
    }
    Catch {
        [Int32]$dpiScale = 0
        [Int32]$dpiPixels = 0
    }
    Finally {
        # Release the device context handle and dispose of the graphics object
        If ($null -ne $GraphicsObject) {
            If ($DeviceContextHandle -ne [IntPtr]::Zero) {
                $GraphicsObject.ReleaseHdc($DeviceContextHandle)
            }
            $GraphicsObject.Dispose()
        }
    }
    # Failed to get dpi, try to read them from registry - Might not be accurate
    If ($RunAsActiveUser) {
        If ($dpiPixels -lt 1) {
            [Int32]$dpiPixels = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics' -Value 'AppliedDPI' -SID $RunAsActiveUser.SID
        }
        If ($dpiPixels -lt 1) {
            [Int32]$dpiPixels = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop' -Value 'LogPixels' -SID $RunAsActiveUser.SID
        }
        [Boolean]$UserDisplayScaleFactor = $true
    }
    # Failed to get dpi from first two registry entries, try to read FontDPI - Usually inaccurate
    If ($dpiPixels -lt 1) {
        #  This registry setting only exists if system scale factor has been changed at least once
        [Int32]$dpiPixels = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontDPI' -Value 'LogPixels'
        [Boolean]$UserDisplayScaleFactor = $false
    }
    # Calculate dpi scale if its empty and we have dpi pixels
    If (($dpiScale -lt 1) -and ($dpiPixels -gt 0)) {
        [Int32]$dpiScale = [Math]::Round(($dpiPixels * 100) / 96)
    }
}
## Variables: Resolve Parameters. For use in a pipeline
[ScriptBlock]$ResolveParameters = {
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]$Parameter
    )

    Switch ($Parameter.Value.GetType().Name) {
        'SwitchParameter' {
            "-$($Parameter.Key):`$$($Parameter.Value.ToString().ToLower())"
        }
        'Boolean' {
            "-$($Parameter.Key):`$$($Parameter.Value.ToString().ToLower())"
        }
        'Int16' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'Int32' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'Int64' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'UInt16' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'UInt32' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'UInt64' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'Single' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'Double' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        'Decimal' {
            "-$($Parameter.Key):$($Parameter.Value)"
        }
        default {
            "-$($Parameter.Key):`'$($Parameter.Value)`'"
        }
    }
}
#endregion
##*=============================================
##* END VARIABLE DECLARATION
##*=============================================

##*=============================================
##* FUNCTION LISTINGS
##*=============================================
#region FunctionListings

#region Function Write-FunctionHeaderOrFooter
Function Write-FunctionHeaderOrFooter {
    <#
.SYNOPSIS

Write the function header or footer to the log upon first entering or exiting a function.

.DESCRIPTION

Write the "Function Start" message, the bound parameters the function was invoked with, or the "Function End" message when entering or exiting a function.

Messages are debug messages so will only be logged if LogDebugMessage option is enabled in XML config file.

.PARAMETER CmdletName

The name of the function this function is invoked from.

.PARAMETER CmdletBoundParameters

The bound parameters of the function this function is invoked from.

.PARAMETER Header

Write the function header.

.PARAMETER Footer

Write the function footer.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

.EXAMPLE

Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$CmdletName,
        [Parameter(Mandatory = $true, ParameterSetName = 'Header')]
        [AllowEmptyCollection()]
        [Hashtable]$CmdletBoundParameters,
        [Parameter(Mandatory = $true, ParameterSetName = 'Header')]
        [Switch]$Header,
        [Parameter(Mandatory = $true, ParameterSetName = 'Footer')]
        [Switch]$Footer
    )

    If ($Header) {
        Write-Log -Message 'Function Start' -Source ${CmdletName} -DebugMessage

        ## Get the parameters that the calling function was invoked with
        [String]$CmdletBoundParameters = $CmdletBoundParameters | Format-Table -Property @{ Label = 'Parameter'; Expression = { "[-$($_.Key)]" } }, @{ Label = 'Value'; Expression = { $_.Value }; Alignment = 'Left' }, @{ Label = 'Type'; Expression = { $_.Value.GetType().Name }; Alignment = 'Left' } -AutoSize -Wrap | Out-String
        If ($CmdletBoundParameters) {
            Write-Log -Message "Function invoked with bound parameter(s): `r`n$CmdletBoundParameters" -Source ${CmdletName} -DebugMessage
        }
        Else {
            Write-Log -Message 'Function invoked without any bound parameters.' -Source ${CmdletName} -DebugMessage
        }
    }
    ElseIf ($Footer) {
        Write-Log -Message 'Function End' -Source ${CmdletName} -DebugMessage
    }
}
#endregion


#region Function Execute-MSP
Function Execute-MSP {
    <#
.SYNOPSIS

Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products
If a valid installed product is found, triggers the Execute-MSI function to patch the installation.
Uses default config MSI parameters. You can use -AddParameters to add additional parameters.

.PARAMETER Path

Path to the msp file

.PARAMETER AddParameters

Additional parameters

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Execute-MSP -Path 'Adobe_Reader_11.0.3_EN.msp'

.EXAMPLE

Execute-MSP -Path 'AcroRdr2017Upd1701130143_MUI.msp' -AddParameters 'ALLUSERS=1'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, HelpMessage = 'Please enter the path to the MSP file')]
        [ValidateScript({ ('.msp' -contains [IO.Path]::GetExtension($_)) })]
        [Alias('FilePath')]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## If the MSP is in the Files directory, set the full path to the MSP
        If (Test-Path -LiteralPath (Join-Path -Path $dirFiles -ChildPath $path -ErrorAction 'SilentlyContinue') -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
            [String]$mspFile = Join-Path -Path $dirFiles -ChildPath $path
        }
        ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'SilentlyContinue') {
            [String]$mspFile = (Get-Item -LiteralPath $Path).FullName
        }
        Else {
            Write-Log -Message "Failed to find MSP file [$path]." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to find MSP file [$path]."
            }
            Continue
        }
        Write-Log -Message 'Checking MSP file for valid product codes.' -Source ${CmdletName}

        [Boolean]$IsMSPNeeded = $false

        ## Create a Windows Installer object
        [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'

        ## Define properties for how the MSI database is opened
        [Int32]$msiOpenDatabaseModePatchFile = 32
        [Int32]$msiOpenDatabaseMode = $msiOpenDatabaseModePatchFile
        ## Open database in read only mode
        [__ComObject]$Database = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($mspFile, $msiOpenDatabaseMode)
        ## Get the SummaryInformation from the windows installer database
        [__ComObject]$SummaryInformation = Get-ObjectProperty -InputObject $Database -PropertyName 'SummaryInformation'
        [Hashtable]$SummaryInfoProperty = @{}
        $AllTargetedProductCodes = (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(7)).Split(';')
        ForEach ($FormattedProductCode in $AllTargetedProductCodes) {
            [PSObject]$MSIInstalled = Get-InstalledApplication -ProductCode $FormattedProductCode
            If ($MSIInstalled) {
                [Boolean]$IsMSPNeeded = $true
            }
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SummaryInformation)
        }
        Catch {
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Database)
        }
        Catch {
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
        }
        Catch {
        }
        If ($IsMSPNeeded) {
            If ($AddParameters) {
                Execute-MSI -Action 'Patch' -Path $Path -AddParameters $AddParameters
            }
            Else {
                Execute-MSI -Action 'Patch' -Path $Path
            }
        }
    }
}
#endregion


#region Function Write-Log
Function Write-Log {
    <#
.SYNOPSIS

Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.

.DESCRIPTION

Write messages to a log file in CMTrace.exe compatible format or Legacy text file format and optionally display in the console.

.PARAMETER Message

The message to write to the log file or output to the console.

.PARAMETER Severity

Defines message type. When writing to console or CMTrace.exe log format, it allows highlighting of message type.
Options: 1 = Information (default), 2 = Warning (highlighted in yellow), 3 = Error (highlighted in red)

.PARAMETER Source

The source of the message being logged.

.PARAMETER ScriptSection

The heading for the portion of the script that is being executed. Default is: $script:installPhase.

.PARAMETER LogType

Choose whether to write a CMTrace.exe compatible log file or a Legacy text log file.

.PARAMETER LogFileDirectory

Set the directory where the log file will be saved.

.PARAMETER LogFileName

Set the name of the log file.

.PARAMETER MaxLogFileSizeMB

Maximum file size limit for log file in megabytes (MB). Default is 10 MB.

.PARAMETER WriteHost

Write the log message to the console.

.PARAMETER ContinueOnError

Suppress writing log message to console on failure to write message to log file. Default is: $true.

.PARAMETER PassThru

Return the message that was passed to the function

.PARAMETER DebugMessage

Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.

.PARAMETER LogDebugMessage

Debug messages only get logged if this parameter is set to $true in the config XML file.

.INPUTS

System.String

The message to write to the log file or output to the console.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Write-Log -Message "Installing patch MS15-031" -Source 'Add-Patch' -LogType 'CMTrace'

.EXAMPLE

Write-Log -Message "Script is running on Windows 8" -Source 'Test-ValidOS' -LogType 'Legacy'

.EXAMPLE

Write-Log -Message "Log only message" -WriteHost $false

.NOTES

.LINK
https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [Alias('Text')]
        [String[]]$Message,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateRange(1, 3)]
        [Int16]$Severity = 1,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNull()]
        [String]$Source = $([String]$parentFunctionName = [IO.Path]::GetFileNameWithoutExtension((Get-Variable -Name 'MyInvocation' -Scope 1 -ErrorAction 'SilentlyContinue').Value.MyCommand.Name); If ($parentFunctionName) {
                $parentFunctionName
            }
            Else {
                'Unknown'
            }),
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [String]$ScriptSection = $script:installPhase,
        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateSet('CMTrace', 'Legacy')]
        [String]$LogType = $configToolkitLogStyle,
        [Parameter(Mandatory = $false, Position = 5)]
        [ValidateNotNullorEmpty()]
        [String]$LogFileDirectory = $(If ($configToolkitCompressLogs) {
                $logTempFolder
            }
            Else {
                $configToolkitLogDir
            }),
        [Parameter(Mandatory = $false, Position = 6)]
        [ValidateNotNullorEmpty()]
        [String]$LogFileName = $logName,
        [Parameter(Mandatory = $false, Position = 7)]
        [ValidateNotNullorEmpty()]
        [Decimal]$MaxLogFileSizeMB = $configToolkitLogMaxSize,
        [Parameter(Mandatory = $false, Position = 8)]
        [ValidateNotNullorEmpty()]
        [Boolean]$WriteHost = $configToolkitLogWriteToHost,
        [Parameter(Mandatory = $false, Position = 9)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true,
        [Parameter(Mandatory = $false, Position = 10)]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false, Position = 11)]
        [Switch]$DebugMessage = $false,
        [Parameter(Mandatory = $false, Position = 12)]
        [Boolean]$LogDebugMessage = $configToolkitLogDebugMessage
    )

    Begin {
        ## Get the name of this function
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

        ## Logging Variables
        #  Log file date/time
        [DateTime]$DateTimeNow = Get-Date
        [String]$LogTime = $DateTimeNow.ToString('HH\:mm\:ss.fff')
        [String]$LogDate = $DateTimeNow.ToString('MM-dd-yyyy')
        If (-not (Test-Path -LiteralPath 'variable:LogTimeZoneBias')) {
            [Int32]$script:LogTimeZoneBias = [TimeZone]::CurrentTimeZone.GetUtcOffset($DateTimeNow).TotalMinutes
        }
        [String]$LogTimePlusBias = $LogTime + $script:LogTimeZoneBias
        #  Initialize variables
        [Boolean]$ExitLoggingFunction = $false
        If (-not (Test-Path -LiteralPath 'variable:DisableLogging')) {
            $DisableLogging = $false
        }
        #  Check if the script section is defined
        [Boolean]$ScriptSectionDefined = [Boolean](-not [String]::IsNullOrEmpty($ScriptSection))
        #  Get the file name of the source script
        Try {
            If ($script:MyInvocation.Value.ScriptName) {
                [String]$ScriptSource = Split-Path -Path $script:MyInvocation.Value.ScriptName -Leaf -ErrorAction 'Stop'
            }
            Else {
                [String]$ScriptSource = Split-Path -Path $script:MyInvocation.MyCommand.Definition -Leaf -ErrorAction 'Stop'
            }
        }
        Catch {
            $ScriptSource = ''
        }

        ## Create script block for generating CMTrace.exe compatible log entry
        [ScriptBlock]$CMTraceLogString = {
            Param (
                [String]$lMessage,
                [String]$lSource,
                [Int16]$lSeverity
            )
            "<![LOG[$lMessage]LOG]!>" + "<time=`"$LogTimePlusBias`" " + "date=`"$LogDate`" " + "component=`"$lSource`" " + "context=`"$([Security.Principal.WindowsIdentity]::GetCurrent().Name)`" " + "type=`"$lSeverity`" " + "thread=`"$PID`" " + "file=`"$ScriptSource`">"
        }

        ## Create script block for writing log entry to the console
        [ScriptBlock]$WriteLogLineToHost = {
            Param (
                [String]$lTextLogLine,
                [Int16]$lSeverity
            )
            If ($WriteHost) {
                #  Only output using color options if running in a host which supports colors.
                If ($Host.UI.RawUI.ForegroundColor) {
                    Switch ($lSeverity) {
                        3 {
                            Write-Host -Object $lTextLogLine -ForegroundColor 'Red' -BackgroundColor 'Black'
                        }
                        2 {
                            Write-Host -Object $lTextLogLine -ForegroundColor 'Yellow' -BackgroundColor 'Black'
                        }
                        1 {
                            Write-Host -Object $lTextLogLine
                        }
                    }
                }
                #  If executing "powershell.exe -File <filename>.ps1 > log.txt", then all the Write-Host calls are converted to Write-Output calls so that they are included in the text log.
                Else {
                    Write-Output -InputObject ($lTextLogLine)
                }
            }
        }

        ## Exit function if it is a debug message and logging debug messages is not enabled in the config XML file
        If (($DebugMessage) -and (-not $LogDebugMessage)) {
            [Boolean]$ExitLoggingFunction = $true; Return
        }
        ## Exit function if logging to file is disabled and logging to console host is disabled
        If (($DisableLogging) -and (-not $WriteHost)) {
            [Boolean]$ExitLoggingFunction = $true; Return
        }
        ## Exit Begin block if logging is disabled
        If ($DisableLogging) {
            Return
        }
        ## Exit function function if it is an [Initialization] message and the toolkit has been relaunched
        If (($AsyncToolkitLaunch) -and ($ScriptSection -eq 'Initialization')) {
            [Boolean]$ExitLoggingFunction = $true; Return
        }

        ## Create the directory where the log file will be saved
        If (-not (Test-Path -LiteralPath $LogFileDirectory -PathType 'Container')) {
            Try {
                $null = New-Item -Path $LogFileDirectory -Type 'Directory' -Force -ErrorAction 'Stop'
            }
            Catch {
                [Boolean]$ExitLoggingFunction = $true
                #  If error creating directory, write message to console
                If (-not $ContinueOnError) {
                    Write-Host -Object "[$LogDate $LogTime] [${CmdletName}] $ScriptSection :: Failed to create the log directory [$LogFileDirectory]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
                }
                Return
            }
        }

        ## Assemble the fully qualified path to the log file
        [String]$LogFilePath = Join-Path -Path $LogFileDirectory -ChildPath $LogFileName
    }
    Process {
        ## Exit function if logging is disabled
        If ($ExitLoggingFunction) {
            Return
        }

        ForEach ($Msg in $Message) {
            ## If the message is not $null or empty, create the log entry for the different logging methods
            [String]$CMTraceMsg = ''
            [String]$ConsoleLogLine = ''
            [String]$LegacyTextLogLine = ''
            If ($Msg) {
                #  Create the CMTrace log message
                If ($ScriptSectionDefined) {
                    [String]$CMTraceMsg = "[$ScriptSection] :: $Msg"
                }

                #  Create a Console and Legacy "text" log entry
                [String]$LegacyMsg = "[$LogDate $LogTime]"
                If ($ScriptSectionDefined) {
                    [String]$LegacyMsg += " [$ScriptSection]"
                }
                If ($Source) {
                    [String]$ConsoleLogLine = "$LegacyMsg [$Source] :: $Msg"
                    Switch ($Severity) {
                        3 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [$Source] [Error] :: $Msg"
                        }
                        2 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [$Source] [Warning] :: $Msg"
                        }
                        1 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [$Source] [Info] :: $Msg"
                        }
                    }
                }
                Else {
                    [String]$ConsoleLogLine = "$LegacyMsg :: $Msg"
                    Switch ($Severity) {
                        3 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [Error] :: $Msg"
                        }
                        2 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [Warning] :: $Msg"
                        }
                        1 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [Info] :: $Msg"
                        }
                    }
                }
            }

            ## Execute script block to create the CMTrace.exe compatible log entry
            [String]$CMTraceLogLine = & $CMTraceLogString -lMessage $CMTraceMsg -lSource $Source -lSeverity $Severity

            ## Choose which log type to write to file
            If ($LogType -ieq 'CMTrace') {
                [String]$LogLine = $CMTraceLogLine
            }
            Else {
                [String]$LogLine = $LegacyTextLogLine
            }

            ## Write the log entry to the log file if logging is not currently disabled
            If (-not $DisableLogging) {
                Try {
                    $LogLine | Out-File -FilePath $LogFilePath -Append -NoClobber -Force -Encoding 'UTF8' -ErrorAction 'Stop'
                }
                Catch {
                    If (-not $ContinueOnError) {
                        Write-Host -Object "[$LogDate $LogTime] [$ScriptSection] [${CmdletName}] :: Failed to write message [$Msg] to the log file [$LogFilePath]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
                    }
                }
            }

            ## Execute script block to write the log entry to the console if $WriteHost is $true
            & $WriteLogLineToHost -lTextLogLine $ConsoleLogLine -lSeverity $Severity
        }
    }
    End {
        ## Archive log file if size is greater than $MaxLogFileSizeMB and $MaxLogFileSizeMB > 0
        Try {
            If ((-not $ExitLoggingFunction) -and (-not $DisableLogging)) {
                [IO.FileInfo]$LogFile = Get-ChildItem -LiteralPath $LogFilePath -ErrorAction 'Stop'
                [Decimal]$LogFileSizeMB = $LogFile.Length / 1MB
                If (($LogFileSizeMB -gt $MaxLogFileSizeMB) -and ($MaxLogFileSizeMB -gt 0)) {
                    ## Change the file extension to "lo_"
                    [String]$ArchivedOutLogFile = [IO.Path]::ChangeExtension($LogFilePath, 'lo_')
                    [Hashtable]$ArchiveLogParams = @{ ScriptSection = $ScriptSection; Source = ${CmdletName}; Severity = 2; LogFileDirectory = $LogFileDirectory; LogFileName = $LogFileName; LogType = $LogType; MaxLogFileSizeMB = 0; WriteHost = $WriteHost; ContinueOnError = $ContinueOnError; PassThru = $false }

                    ## Log message about archiving the log file
                    $ArchiveLogMessage = "Maximum log file size [$MaxLogFileSizeMB MB] reached. Rename log file to [$ArchivedOutLogFile]."
                    Write-Log -Message $ArchiveLogMessage @ArchiveLogParams

                    ## Archive existing log file from <filename>.log to <filename>.lo_. Overwrites any existing <filename>.lo_ file. This is the same method SCCM uses for log files.
                    Move-Item -LiteralPath $LogFilePath -Destination $ArchivedOutLogFile -Force -ErrorAction 'Stop'

                    ## Start new log file and Log message about archiving the old log file
                    $NewLogMessage = "Previous log file was renamed to [$ArchivedOutLogFile] because maximum log file size of [$MaxLogFileSizeMB MB] was reached."
                    Write-Log -Message $NewLogMessage @ArchiveLogParams
                }
            }
        }
        Catch {
            ## If renaming of file fails, script will continue writing to log file even if size goes over the max file size
        }
        Finally {
            If ($PassThru) {
                Write-Output -InputObject ($Message)
            }
        }
    }
}
#endregion


#region Function Remove-InvalidFileNameChars
Function Remove-InvalidFileNameChars {
    <#
.SYNOPSIS

Remove invalid characters from the supplied string.

.DESCRIPTION

Remove invalid characters from the supplied string and returns a valid filename as a string.

.PARAMETER Name

Text to remove invalid filename characters from.

.INPUTS

System.String

A string containing invalid filename characters.

.OUTPUTS

System.String

Returns the input string with the invalid characters removed.

.EXAMPLE

Remove-InvalidFileNameChars -Name "Filename/\1"

.NOTES

This functions always returns a string however it can be empty if the name only contains invalid characters.
Do no use this command for an entire path as '\' is not a valid filename character.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyString()]
        [String]$Name
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Output -InputObject (([Char[]]$Name | Where-Object { $invalidFileNameChars -notcontains $_ }) -join '')
        }
        Catch {
            Write-Log -Message "Failed to remove invalid characters from the supplied filename. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function New-ZipFile
Function New-ZipFile {
    <#
.SYNOPSIS

Create a new zip archive or add content to an existing archive.

.DESCRIPTION

Create a new zip archive or add content to an existing archive by using the Shell object .CopyHere method.

.PARAMETER DestinationArchiveDirectoryPath

The path to the directory path where the zip archive will be saved.

.PARAMETER DestinationArchiveFileName

The name of the zip archive.

.PARAMETER SourceDirectoryPath

The path to the directory to be archived, specified as absolute paths.

.PARAMETER SourceFilePath

The path to the file to be archived, specified as absolute paths.

.PARAMETER RemoveSourceAfterArchiving

Remove the source path after successfully archiving the content. Default is: $false.

.PARAMETER OverWriteArchive

Overwrite the destination archive path if it already exists. Default is: $false.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

New-ZipFile -DestinationArchiveDirectoryPath 'E:\Testing' -DestinationArchiveFileName 'TestingLogs.zip' -SourceDirectory 'E:\Testing\Logs'

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'CreateFromDirectory')]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [String]$DestinationArchiveDirectoryPath,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$DestinationArchiveFileName,
        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'CreateFromDirectory')]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Container' })]
        [String[]]$SourceDirectoryPath,
        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'CreateFromFile')]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String[]]$SourceFilePath,
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [Switch]$RemoveSourceAfterArchiving = $false,
        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateNotNullorEmpty()]
        [Switch]$OverWriteArchive = $false,
        [Parameter(Mandatory = $false, Position = 5)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## Remove invalid characters from the supplied filename
            $DestinationArchiveFileName = Remove-InvalidFileNameChars -Name $DestinationArchiveFileName
            If ($DestinationArchiveFileName.length -eq 0) {
                Throw 'Invalid filename characters replacement resulted into an empty string.'
            }
            ## Get the full destination path where the archive will be stored
            [String]$DestinationPath = Join-Path -Path $DestinationArchiveDirectoryPath -ChildPath $DestinationArchiveFileName -ErrorAction 'Stop'
            Write-Log -Message "Creating a zip archive with the requested content at destination path [$DestinationPath]." -Source ${CmdletName}

            ## If the destination archive already exists, delete it if the -OverWriteArchive option was selected
            If (($OverWriteArchive) -and (Test-Path -LiteralPath $DestinationPath)) {
                Write-Log -Message "An archive at the destination path already exists, deleting file [$DestinationPath]." -Source ${CmdletName}
                $null = Remove-Item -LiteralPath $DestinationPath -Force -ErrorAction 'Stop'
            }

            ## If archive file does not exist, then create a zero-byte zip archive
            If (-not (Test-Path -LiteralPath $DestinationPath)) {
                ## Create a zero-byte file
                Write-Log -Message "Creating a zero-byte file [$DestinationPath]." -Source ${CmdletName}
                $null = New-Item -Path $DestinationArchiveDirectoryPath -Name $DestinationArchiveFileName -ItemType 'File' -Force -ErrorAction 'Stop'

                ## Write the file header for a zip file to the zero-byte file
                [Byte[]]$ZipArchiveByteHeader = 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                [IO.FileStream]$FileStream = New-Object -TypeName 'System.IO.FileStream' -ArgumentList ($DestinationPath, ([IO.FileMode]::Create))
                [IO.BinaryWriter]$BinaryWriter = New-Object -TypeName 'System.IO.BinaryWriter' -ArgumentList ($FileStream)
                Write-Log -Message "Write the file header for a zip archive to the zero-byte file [$DestinationPath]." -Source ${CmdletName}
                $null = $BinaryWriter.Write($ZipArchiveByteHeader)
                $BinaryWriter.Close()
                $FileStream.Close()
            }

            ## Create a Shell object
            [__ComObject]$ShellApp = New-Object -ComObject 'Shell.Application' -ErrorAction 'Stop'
            ## Create an object representing the archive file
            [__ComObject]$Archive = $ShellApp.NameSpace($DestinationPath)

            ## Create the archive file
            If ($PSCmdlet.ParameterSetName -eq 'CreateFromDirectory') {
                ## Create the archive file from a source directory
                ForEach ($Directory in $SourceDirectoryPath) {
                    Try {
                        #  Create an object representing the source directory
                        [__ComObject]$CreateFromDirectory = $ShellApp.NameSpace($Directory)
                        #  Copy all of the files and folders from the source directory to the archive
                        $null = $Archive.CopyHere($CreateFromDirectory.Items())
                        #  Wait for archive operation to complete. Archive file count property returns 0 if archive operation is in progress.
                        Write-Log -Message "Compressing [$($CreateFromDirectory.Count)] file(s) in source directory [$Directory] to destination path [$DestinationPath]..." -Source ${CmdletName}
                        Do {
                            Start-Sleep -Milliseconds 250
                        } While ($Archive.Items().Count -eq 0)
                    }
                    Finally {
                        #  Release the ComObject representing the source directory
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($CreateFromDirectory)
                    }

                    #  If option was selected, recursively delete the source directory after successfully archiving the contents
                    If ($RemoveSourceAfterArchiving) {
                        Try {
                            Write-Log -Message "Recursively deleting the source directory [$Directory] as contents have been successfully archived." -Source ${CmdletName}
                            $null = Remove-Item -LiteralPath $Directory -Recurse -Force -ErrorAction 'Stop'
                        }
                        Catch {
                            Write-Log -Message "Failed to recursively delete the source directory [$Directory]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                        }
                    }
                }
            }
            Else {
                ## Create the archive file from a list of one or more files
                [IO.FileInfo[]]$SourceFilePath = [IO.FileInfo[]]$SourceFilePath
                ForEach ($File in $SourceFilePath) {
                    #  Copy the files and folders from the source directory to the archive
                    $null = $Archive.CopyHere($File.FullName)
                    #  Wait for archive operation to complete. Archive file count property returns 0 if archive operation is in progress.
                    Write-Log -Message "Compressing file [$($File.FullName)] to destination path [$DestinationPath]..." -Source ${CmdletName}
                    Do {
                        Start-Sleep -Milliseconds 250
                    } While ($Archive.Items().Count -eq 0)

                    #  If option was selected, delete the source file after successfully archiving the content
                    If ($RemoveSourceAfterArchiving) {
                        Try {
                            Write-Log -Message "Deleting the source file [$($File.FullName)] as it has been successfully archived." -Source ${CmdletName}
                            $null = Remove-Item -LiteralPath $File.FullName -Force -ErrorAction 'Stop'
                        }
                        Catch {
                            Write-Log -Message "Failed to delete the source file [$($File.FullName)]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                        }
                    }
                }
            }

            ## If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users.
            #  Apply the parent folder's permissions to the archive file to fix the problem.
            Write-Log -Message "If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users. Apply permissions from parent folder [$DestinationArchiveDirectoryPath] to file [$DestinationPath]." -Source ${CmdletName}
            Try {
                [Security.AccessControl.DirectorySecurity]$DestinationArchiveDirectoryPathAcl = Get-Acl -Path $DestinationArchiveDirectoryPath -ErrorAction 'Stop'
                Set-Acl -Path $DestinationPath -AclObject $DestinationArchiveDirectoryPathAcl -ErrorAction 'Stop'
            }
            Catch {
                Write-Log -Message "Failed to apply parent folder's [$DestinationArchiveDirectoryPath] permissions to file [$DestinationPath]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
            }
        }
        Catch {
            Write-Log -Message "Failed to archive the requested file(s). `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to archive the requested file(s): $($_.Exception.Message)"
            }
        }
        Finally {
            ## Release the ComObject representing the archive
            If ($Archive) {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Archive)
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Exit-Script
Function Exit-Script {
    <#
.SYNOPSIS

Exit the script, perform cleanup actions, and pass an exit code to the parent process.

.DESCRIPTION

Always use when exiting the script to ensure cleanup actions are performed.

.PARAMETER ExitCode

The exit code to be passed from the script to the parent process, e.g. SCCM

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Exit-Script

.EXAMPLE

Exit-Script -ExitCode 1618

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$ExitCode = 0
    )

    ## Get the name of this function
    [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

    ## Stop the Close Program Dialog if running
    If ($formCloseApps) {
        $formCloseApps.Close
    }

    ## Close the Installation Progress Dialog if running
    Close-InstallationProgress

    ## If block execution variable is true, call the function to unblock execution
    If ($BlockExecution) {
        Unblock-AppExecution
    }

    ## If Terminal Server mode was set, turn it off
    If ($terminalServerMode) {
        Disable-TerminalServerInstallMode
    }

    ## Determine action based on exit code
    Switch ($exitCode) {
        $configInstallationUIExitCode {
            $installSuccess = $false
        }
        $configInstallationDeferExitCode {
            $installSuccess = $false
        }
        3010 {
            $installSuccess = $true
        }
        1641 {
            $installSuccess = $true
        }
        0 {
            $installSuccess = $true
        }
        Default {
            $installSuccess = $false
        }
    }

    ## Determine if balloon notification should be shown
    If ($deployModeSilent) {
        [Boolean]$configShowBalloonNotifications = $false
    }

    If ($installSuccess) {
        If (Test-Path -LiteralPath $regKeyDeferHistory -ErrorAction 'SilentlyContinue') {
            Write-Log -Message 'Removing deferral history...' -Source ${CmdletName}
            Remove-RegistryKey -Key $regKeyDeferHistory -Recurse
        }

        [String]$balloonText = "$deploymentTypeName $configBalloonTextComplete"
        ## Handle reboot prompts on successful script completion
        If (($AllowRebootPassThru) -and ((($msiRebootDetected) -or ($exitCode -eq 3010)) -or ($exitCode -eq 1641))) {
            Write-Log -Message 'A restart has been flagged as required.' -Source ${CmdletName}
            [String]$balloonText = "$deploymentTypeName $configBalloonTextRestartRequired"
            If (($msiRebootDetected) -and ($exitCode -ne 1641)) {
                [Int32]$exitCode = 3010
            }
        }
        Else {
            [Int32]$exitCode = 0
        }

        Write-Log -Message "$installName $deploymentTypeName completed with exit code [$exitcode]." -Source ${CmdletName}
        If ($configShowBalloonNotifications) {
            Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText $balloonText -NoWait
        }
    }
    ElseIf (-not $installSuccess) {
        Write-Log -Message "$installName $deploymentTypeName completed with exit code [$exitcode]." -Source ${CmdletName}
        If (($exitCode -eq $configInstallationUIExitCode) -or ($exitCode -eq $configInstallationDeferExitCode)) {
            [String]$balloonText = "$deploymentTypeName $configBalloonTextFastRetry"
            If ($configShowBalloonNotifications) {
                Show-BalloonTip -BalloonTipIcon 'Warning' -BalloonTipText $balloonText -NoWait
            }
        }
        Else {
            [String]$balloonText = "$deploymentTypeName $configBalloonTextError"
            If ($configShowBalloonNotifications) {
                Show-BalloonTip -BalloonTipIcon 'Error' -BalloonTipText $balloonText -NoWait
            }
        }
    }

    [String]$LogDash = '-' * 79
    Write-Log -Message $LogDash -Source ${CmdletName}

    ## Archive the log files to zip format and then delete the temporary logs folder
    If ($configToolkitCompressLogs) {
        ## Disable logging to file so that we can archive the log files
        . $DisableScriptLogging

        [String]$DestinationArchiveFileName = $installName + '_' + $deploymentType + '_' + ((Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').ToString()) + '.zip'
        New-ZipFile -DestinationArchiveDirectoryPath $configToolkitLogDir -DestinationArchiveFileName $DestinationArchiveFileName -SourceDirectory $logTempFolder -RemoveSourceAfterArchiving
    }

    If ($script:notifyIcon) {
        Try {
            $script:notifyIcon.Dispose()
        }
        Catch {
        }
    }
    ## Reset powershell window title to its previous title
    $Host.UI.RawUI.WindowTitle = $oldPSWindowTitle
    ## Reset variables in case another toolkit is being run in the same session
    $global:logName = $null
    $global:installTitle = $null
    $global:installName = $null
    $global:appName = $null
    ## Exit the script, returning the exit code to SCCM
    If (Test-Path -LiteralPath 'variable:HostInvocation') {
        $script:ExitCode = $exitCode; Exit
    }
    Else {
        Exit $exitCode
    }
}
#endregion


#region Function Resolve-Error
Function Resolve-Error {
    <#
.SYNOPSIS

Enumerate error record details.

.DESCRIPTION

Enumerate an error record, or a collection of error record, properties. By default, the details for the last error will be enumerated.

.PARAMETER ErrorRecord

The error record to resolve. The default error record is the latest one: $global:Error[0]. This parameter will also accept an array of error records.

.PARAMETER Property

The list of properties to display from the error record. Use "*" to display all properties.

Default list of error properties is: Message, FullyQualifiedErrorId, ScriptStackTrace, PositionMessage, InnerException

.PARAMETER GetErrorRecord

Get error record details as represented by $_.

.PARAMETER GetErrorInvocation

Get error record invocation information as represented by $_.InvocationInfo.

.PARAMETER GetErrorException

Get error record exception details as represented by $_.Exception.

.PARAMETER GetErrorInnerException

Get error record inner exception details as represented by $_.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.

.INPUTS

System.Array.

Accepts an array of error records.

.OUTPUTS

System.String

Displays the error record details.

.EXAMPLE

Resolve-Error

.EXAMPLE

Resolve-Error -Property *

.EXAMPLE

Resolve-Error -Property InnerException

.EXAMPLE

Resolve-Error -GetErrorInvocation:$false

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [Array]$ErrorRecord,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String[]]$Property = ('Message', 'InnerException', 'FullyQualifiedErrorId', 'ScriptStackTrace', 'PositionMessage'),
        [Parameter(Mandatory = $false, Position = 2)]
        [Switch]$GetErrorRecord = $true,
        [Parameter(Mandatory = $false, Position = 3)]
        [Switch]$GetErrorInvocation = $true,
        [Parameter(Mandatory = $false, Position = 4)]
        [Switch]$GetErrorException = $true,
        [Parameter(Mandatory = $false, Position = 5)]
        [Switch]$GetErrorInnerException = $true
    )

    Begin {
        ## If function was called without specifying an error record, then choose the latest error that occurred
        If (-not $ErrorRecord) {
            If ($global:Error.Count -eq 0) {
                #Write-Warning -Message "The `$Error collection is empty"
                Return
            }
            Else {
                [Array]$ErrorRecord = $global:Error[0]
            }
        }

        ## Allows selecting and filtering the properties on the error object if they exist
        [ScriptBlock]$SelectProperty = {
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                $InputObject,
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String[]]$Property
            )

            [String[]]$ObjectProperty = $InputObject | Get-Member -MemberType '*Property' | Select-Object -ExpandProperty 'Name'
            ForEach ($Prop in $Property) {
                If ($Prop -eq '*') {
                    [String[]]$PropertySelection = $ObjectProperty
                    Break
                }
                ElseIf ($ObjectProperty -contains $Prop) {
                    [String[]]$PropertySelection += $Prop
                }
            }
            Write-Output -InputObject ($PropertySelection)
        }

        #  Initialize variables to avoid error if 'Set-StrictMode' is set
        $LogErrorRecordMsg = $null
        $LogErrorInvocationMsg = $null
        $LogErrorExceptionMsg = $null
        $LogErrorMessageTmp = $null
        $LogInnerMessage = $null
    }
    Process {
        If (-not $ErrorRecord) {
            Return
        }
        ForEach ($ErrRecord in $ErrorRecord) {
            ## Capture Error Record
            If ($GetErrorRecord) {
                [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord -Property $Property
                $LogErrorRecordMsg = $ErrRecord | Select-Object -Property $SelectedProperties
            }

            ## Error Invocation Information
            If ($GetErrorInvocation) {
                If ($ErrRecord.InvocationInfo) {
                    [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.InvocationInfo -Property $Property
                    $LogErrorInvocationMsg = $ErrRecord.InvocationInfo | Select-Object -Property $SelectedProperties
                }
            }

            ## Capture Error Exception
            If ($GetErrorException) {
                If ($ErrRecord.Exception) {
                    [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.Exception -Property $Property
                    $LogErrorExceptionMsg = $ErrRecord.Exception | Select-Object -Property $SelectedProperties
                }
            }

            ## Display properties in the correct order
            If ($Property -eq '*') {
                #  If all properties were chosen for display, then arrange them in the order the error object displays them by default.
                If ($LogErrorRecordMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorRecordMsg
                }
                If ($LogErrorInvocationMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
                }
                If ($LogErrorExceptionMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
                }
            }
            Else {
                #  Display selected properties in our custom order
                If ($LogErrorExceptionMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
                }
                If ($LogErrorRecordMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorRecordMsg
                }
                If ($LogErrorInvocationMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
                }
            }

            If ($LogErrorMessageTmp) {
                $LogErrorMessage = 'Error Record:'
                $LogErrorMessage += "`n-------------"
                $LogErrorMsg = $LogErrorMessageTmp | Format-List | Out-String
                $LogErrorMessage += $LogErrorMsg
            }

            ## Capture Error Inner Exception(s)
            If ($GetErrorInnerException) {
                If ($ErrRecord.Exception -and $ErrRecord.Exception.InnerException) {
                    $LogInnerMessage = 'Error Inner Exception(s):'
                    $LogInnerMessage += "`n-------------------------"

                    $ErrorInnerException = $ErrRecord.Exception.InnerException
                    $Count = 0

                    While ($ErrorInnerException) {
                        [String]$InnerExceptionSeperator = '~' * 40

                        [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrorInnerException -Property $Property
                        $LogErrorInnerExceptionMsg = $ErrorInnerException | Select-Object -Property $SelectedProperties | Format-List | Out-String

                        If ($Count -gt 0) {
                            $LogInnerMessage += $InnerExceptionSeperator
                        }
                        $LogInnerMessage += $LogErrorInnerExceptionMsg

                        $Count++
                        $ErrorInnerException = $ErrorInnerException.InnerException
                    }
                }
            }

            If ($LogErrorMessage) {
                $Output = $LogErrorMessage
            }
            If ($LogInnerMessage) {
                $Output += $LogInnerMessage
            }

            Write-Output -InputObject $Output

            If (Test-Path -LiteralPath 'variable:Output') {
                Clear-Variable -Name 'Output'
            }
            If (Test-Path -LiteralPath 'variable:LogErrorMessage') {
                Clear-Variable -Name 'LogErrorMessage'
            }
            If (Test-Path -LiteralPath 'variable:LogInnerMessage') {
                Clear-Variable -Name 'LogInnerMessage'
            }
            If (Test-Path -LiteralPath 'variable:LogErrorMessageTmp') {
                Clear-Variable -Name 'LogErrorMessageTmp'
            }
        }
    }
    End {
    }
}
#endregion


#region Function Show-InstallationPrompt
Function Show-InstallationPrompt {
    <#
.SYNOPSIS

Displays a custom installation prompt with the toolkit branding and optional buttons.

.DESCRIPTION

Any combination of Left, Middle or Right buttons can be displayed. The return value of the button clicked by the user is the button text specified.

.PARAMETER Title

Title of the prompt. Default: the application installation name.

.PARAMETER Message

Message text to be included in the prompt

.PARAMETER MessageAlignment

Alignment of the message text. Options: Left, Center, Right. Default: Center.

.PARAMETER ButtonLeftText

Show a button on the left of the prompt with the specified text

.PARAMETER ButtonRightText

Show a button on the right of the prompt with the specified text

.PARAMETER ButtonMiddleText

Show a button in the middle of the prompt with the specified text

.PARAMETER Icon

Show a system icon in the prompt. Options: Application, Asterisk, Error, Exclamation, Hand, Information, None, Question, Shield, Warning, WinLogo. Default: None

.PARAMETER NoWait

Specifies whether to show the prompt asynchronously (i.e. allow the script to continue without waiting for a response). Default: $false.

.PARAMETER PersistPrompt

Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml. The user will have no option but to respond to the prompt - resistance is futile!

.PARAMETER MinimizeWindows

Specifies whether to minimize other windows when displaying prompt. Default: $false.

.PARAMETER Timeout

Specifies the time period in seconds after which the prompt should timeout. Default: UI timeout value set in the config XML file.

.PARAMETER ExitOnTimeout

Specifies whether to exit the script if the UI times out. Default: $true.

.PARAMETER TopMost

Specifies whether the progress window should be topmost. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Show-InstallationPrompt -Message 'Do you want to proceed with the installation?' -ButtonRightText 'Yes' -ButtonLeftText 'No'

.EXAMPLE

Show-InstallationPrompt -Title 'Funny Prompt' -Message 'How are you feeling today?' -ButtonRightText 'Good' -ButtonLeftText 'Bad' -ButtonMiddleText 'Indifferent'

.EXAMPLE

Show-InstallationPrompt -Message 'You can customize text to appear at the end of an install, or remove it completely for unattended installations.' -Icon Information -NoWait

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Title = $installTitle,
        [Parameter(Mandatory = $false)]
        [String]$Message = '',
        [Parameter(Mandatory = $false)]
        [ValidateSet('Left', 'Center', 'Right')]
        [String]$MessageAlignment = 'Center',
        [Parameter(Mandatory = $false)]
        [String]$ButtonRightText = '',
        [Parameter(Mandatory = $false)]
        [String]$ButtonLeftText = '',
        [Parameter(Mandatory = $false)]
        [String]$ButtonMiddleText = '',
        [Parameter(Mandatory = $false)]
        [ValidateSet('Application', 'Asterisk', 'Error', 'Exclamation', 'Hand', 'Information', 'None', 'Question', 'Shield', 'Warning', 'WinLogo')]
        [String]$Icon = 'None',
        [Parameter(Mandatory = $false)]
        [Switch]$NoWait = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$PersistPrompt = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$MinimizeWindows = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$Timeout = $configInstallationUITimeout,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExitOnTimeout = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$TopMost = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Bypass if in non-interactive mode
        If ($deployModeSilent) {
            Write-Log -Message "Bypassing Show-InstallationPrompt [Mode: $deployMode]. Message:$Message" -Source ${CmdletName}
            Return
        }

        ## Get parameters for calling function asynchronously
        [Hashtable]$installPromptParameters = $PSBoundParameters

        ## Check if the countdown was specified
        If ($timeout -gt $configInstallationUITimeout) {
            [String]$CountdownTimeoutErr = 'The installation UI dialog timeout cannot be longer than the timeout specified in the XML configuration file.'
            Write-Log -Message $CountdownTimeoutErr -Severity 3 -Source ${CmdletName}
            Throw $CountdownTimeoutErr
        }

        ## If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously
        If ($NoWait) {
            # Remove the NoWait parameter so that the script is run synchronously in the new PowerShell session. This also prevents the function to loop indefinitely.
            $installPromptParameters.Remove('NoWait')
            # Format the parameters as a string
            [String]$installPromptParameters = ($installPromptParameters.GetEnumerator() | ForEach-Object { & $ResolveParameters $_ }) -join ' '


            Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & {& `'$scriptPath`' -ReferredInstallTitle `'$Title`' -ReferredInstallName `'$installName`' -ReferredLogName `'$logName`' -ShowInstallationPrompt $installPromptParameters -AsyncToolkitLaunch}" -WindowStyle 'Hidden' -ErrorAction 'SilentlyContinue'
            Return
        }

        [Windows.Forms.Application]::EnableVisualStyles()
        $formInstallationPrompt = New-Object -TypeName 'System.Windows.Forms.Form'
        $pictureBanner = New-Object -TypeName 'System.Windows.Forms.PictureBox'
        If ($Icon -ne 'None') {
            $pictureIcon = New-Object -TypeName 'System.Windows.Forms.PictureBox'
        }
        $labelText = New-Object -TypeName 'System.Windows.Forms.Label'
        $buttonRight = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonMiddle = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonLeft = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonAbort = New-Object -TypeName 'System.Windows.Forms.Button'
        $flowLayoutPanel = New-Object -TypeName 'System.Windows.Forms.FlowLayoutPanel'
        $panelButtons = New-Object -TypeName 'System.Windows.Forms.Panel'

        [ScriptBlock]$Install_Prompt_Form_Cleanup_FormClosed = {
            ## Remove all event handlers from the controls
            Try {
                $labelText.remove_Click($handler_labelText_Click)
                $buttonLeft.remove_Click($buttonLeft_OnClick)
                $buttonRight.remove_Click($buttonRight_OnClick)
                $buttonMiddle.remove_Click($buttonMiddle_OnClick)
                $buttonAbort.remove_Click($buttonAbort_OnClick)
                $installPromptTimer.remove_Tick($installPromptTimer_Tick)
                $installPromptTimer.Dispose()
                $installPromptTimer = $null
                $installPromptTimerPersist.remove_Tick($installPromptTimerPersist_Tick)
                $installPromptTimerPersist.Dispose()
                $installPromptTimerPersist = $null
                $formInstallationPrompt.remove_Load($Install_Prompt_Form_StateCorrection_Load)
                $formInstallationPrompt.remove_FormClosed($Install_Prompt_Form_Cleanup_FormClosed)
            }
            Catch {
            }
        }

        [ScriptBlock]$Install_Prompt_Form_StateCorrection_Load = {
            # Disable the X button
            Try {
                $windowHandle = $formInstallationPrompt.Handle
                If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
                    $menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
                    If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
                        [PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
                        [PSADT.UiAutomation]::DestroyMenu($menuHandle)
                    }
                }
            }
            Catch {
                # Not a terminating error if we can't disable the button. Just disable the Control Box instead
                Write-Log 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2 -Source ${CmdletName}
                $formInstallationPrompt.ControlBox = $false
            }
            $formInstallationPrompt.WindowState = 'Normal'
            $formInstallationPrompt.AutoSize = $true
            $formInstallationPrompt.AutoScaleMode = 'Font'
            $formInstallationPrompt.AutoScaleDimensions = New-Object System.Drawing.SizeF(6, 13) #Set as if using 96 DPI
            $formInstallationPrompt.TopMost = $TopMost
            $formInstallationPrompt.BringToFront()
            # Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
            Set-Variable -Name 'formInstallationPromptStartPosition' -Value $formInstallationPrompt.Location -Scope 'Script'
        }

        ## Form

        ##----------------------------------------------
        ## Create padding object
        $paddingNone = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 0)

        ## Default control size
        $DefaultControlSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 0)

        ## Generic Button properties
        $buttonSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (130, 24)

        ## Picture Banner
        $pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
        $pictureBanner.ImageLocation = $appDeployLogoBanner
        $pictureBanner.Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, $appDeployLogoBannerHeight)
        $pictureBanner.MinimumSize = $DefaultControlSize
        $pictureBanner.SizeMode = 'CenterImage'
        $pictureBanner.Margin = $paddingNone
        $pictureBanner.TabStop = $false
        $pictureBanner.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)

        ## Picture Icon
        If ($Icon -ne 'None') {
            $pictureIcon.DataBindings.DefaultDataSourceUpdateMode = 0
            $pictureIcon.Image = ([Drawing.SystemIcons]::$Icon).ToBitmap()
            $pictureIcon.Name = 'pictureIcon'
            $pictureIcon.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (64, 32)
            $pictureIcon.Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (64, 32)
            $pictureIcon.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (24, 0, 8, 0)
            $pictureIcon.SizeMode = 'CenterImage'
            $pictureIcon.TabStop = $false
            $pictureIcon.Anchor = 'None'
            $pictureIcon.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 5)
        }

        ## Label Text
        $labelText.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelText.Font = $defaultFont
        $labelText.Name = 'labelText'
        $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (386, 0)
        $labelText.Size = $System_Drawing_Size
        If ($Icon -ne 'None') {
            $labelText.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (386, $pictureIcon.Height)
        }
        Else {
            $labelText.MinimumSize = $System_Drawing_Size
        }
        $labelText.MaximumSize = $System_Drawing_Size
        $labelText.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 5)
        $labelText.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (20, 0, 20, 0)
        $labelText.TabStop = $false
        $labelText.Text = $message
        $labelText.TextAlign = "Middle$($MessageAlignment)"
        $labelText.Anchor = 'None'
        $labelText.AutoSize = $true
        $labelText.add_Click($handler_labelText_Click)

        If ($Icon -ne 'None') {
            # Add margin for the icon based on labelText Height so its centered
            $pictureIcon.Height = $labelText.Height
        }
        ## Button Left
        $buttonLeft.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonLeft.Name = 'buttonLeft'
        $buttonLeft.Font = $defaultFont
        $buttonLeft.Size = $buttonSize
        $buttonLeft.MinimumSize = $buttonSize
        $buttonLeft.MaximumSize = $buttonSize
        $buttonLeft.TabIndex = 0
        $buttonLeft.Text = $buttonLeftText
        $buttonLeft.DialogResult = 'No'
        $buttonLeft.AutoSize = $false
        $buttonLeft.Margin = $paddingNone
        $buttonLeft.Padding = $paddingNone
        $buttonLeft.UseVisualStyleBackColor = $true
        $buttonLeft.Location = '14,4'
        $buttonLeft.add_Click($buttonLeft_OnClick)

        ## Button Middle
        $buttonMiddle.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonMiddle.Name = 'buttonMiddle'
        $buttonMiddle.Font = $defaultFont
        $buttonMiddle.Size = $buttonSize
        $buttonMiddle.MinimumSize = $buttonSize
        $buttonMiddle.MaximumSize = $buttonSize
        $buttonMiddle.TabIndex = 1
        $buttonMiddle.Text = $buttonMiddleText
        $buttonMiddle.DialogResult = 'Ignore'
        $buttonMiddle.AutoSize = $true
        $buttonMiddle.Margin = $paddingNone
        $buttonMiddle.Padding = $paddingNone
        $buttonMiddle.UseVisualStyleBackColor = $true
        $buttonMiddle.Location = '160,4'
        $buttonMiddle.add_Click($buttonMiddle_OnClick)

        ## Button Right
        $buttonRight.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonRight.Name = 'buttonRight'
        $buttonRight.Font = $defaultFont
        $buttonRight.Size = $buttonSize
        $buttonRight.MinimumSize = $buttonSize
        $buttonRight.MaximumSize = $buttonSize
        $buttonRight.TabIndex = 2
        $buttonRight.Text = $ButtonRightText
        $buttonRight.DialogResult = 'Yes'
        $buttonRight.AutoSize = $true
        $buttonRight.Margin = $paddingNone
        $buttonRight.Padding = $paddingNone
        $buttonRight.UseVisualStyleBackColor = $true
        $buttonRight.Location = '306,4'
        $buttonRight.add_Click($buttonRight_OnClick)

        ## Button Abort (Hidden)
        $buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonAbort.Name = 'buttonAbort'
        $buttonAbort.Font = $defaultFont
        $buttonAbort.Size = '0,0'
        $buttonAbort.MinimumSize = '0,0'
        $buttonAbort.MaximumSize = '0,0'
        $buttonAbort.BackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.ForeColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.BorderSize = 0
        $buttonAbort.FlatAppearance.MouseDownBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.MouseOverBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatStyle = [System.Windows.Forms.FlatStyle]::System
        $buttonAbort.DialogResult = 'Abort'
        $buttonAbort.TabStop = $false
        $buttonAbort.Visible = $true # Has to be set visible so we can call Click on it
        $buttonAbort.Margin = $paddingNone
        $buttonAbort.Padding = $paddingNone
        $buttonAbort.UseVisualStyleBackColor = $true
        $buttonAbort.add_Click($buttonAbort_OnClick)

        ## FlowLayoutPanel
        $flowLayoutPanel.MinimumSize = $DefaultControlSize
        $flowLayoutPanel.MaximumSize = $DefaultControlSize
        $flowLayoutPanel.Size = $DefaultControlSize
        $flowLayoutPanel.AutoSize = $true
        $flowLayoutPanel.AutoSizeMode = 'GrowAndShrink'
        $flowLayoutPanel.Anchor = 'Top,Left'
        $flowLayoutPanel.FlowDirection = 'LeftToRight'
        $flowLayoutPanel.WrapContents = $true
        $flowLayoutPanel.Margin = $paddingNone
        $flowLayoutPanel.Padding = $paddingNone
        ## Make sure label text is positioned correctly
        If ($Icon -ne 'None') {
            $labelText.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 10, 0)
            $pictureIcon.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
            $labelText.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (64, 0)
        }
        Else {
            $labelText.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
            $labelText.MinimumSize = $DefaultControlSize
            $labelText.MaximumSize = $DefaultControlSize
            $labelText.Size = $DefaultControlSize
            $labelText.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
        }
        If ($Icon -ne 'None') {
            $flowLayoutPanel.Controls.Add($pictureIcon)
        }
        $flowLayoutPanel.Controls.Add($labelText)
        $flowLayoutPanel.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, $appDeployLogoBannerHeight)

        ## ButtonsPanel
        $panelButtons.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        If ($Icon -ne 'None') {
            $panelButtons.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (64, 0)
        }
        Else {
            $panelButtons.Padding = $paddingNone
        }
        $panelButtons.Margin = $paddingNone
        $panelButtons.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.AutoSize = $true
        If ($buttonLeftText) {
            $panelButtons.Controls.Add($buttonLeft)
        }
        If ($buttonMiddleText) {
            $panelButtons.Controls.Add($buttonMiddle)
        }
        If ($buttonRightText) {
            $panelButtons.Controls.Add($buttonRight)
        }
        ## Add the ButtonsPanel to the flowLayoutPanel if any buttons are present
        If ($buttonLeftText -or $buttonMiddleText -or $buttonRightText) {
            $flowLayoutPanel.Controls.Add($panelButtons)
        }

        ## Form Installation Prompt
        $formInstallationPrompt.MinimumSize = $DefaultControlSize
        $formInstallationPrompt.Size = $DefaultControlSize
        $formInstallationPrompt.Padding = $paddingNone
        $formInstallationPrompt.Margin = $paddingNone
        $formInstallationPrompt.DataBindings.DefaultDataSourceUpdateMode = 0
        $formInstallationPrompt.Name = 'InstallPromptForm'
        $formInstallationPrompt.Text = $title
        $formInstallationPrompt.StartPosition = 'CenterScreen'
        # $formInstallationPrompt.FormBorderStyle = 'FixedDialog'
        $formInstallationPrompt.MaximizeBox = $false
        $formInstallationPrompt.MinimizeBox = $false
        $formInstallationPrompt.TopMost = $TopMost
        $formInstallationPrompt.TopLevel = $true
        $formInstallationPrompt.AutoSize = $true
        $formInstallationPrompt.AutoScaleMode = 'Font'
        $formInstallationPrompt.AutoScaleDimensions = New-Object System.Drawing.SizeF(6, 13) #Set as if using 96 DPI
        $formInstallationPrompt.Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
        $formInstallationPrompt.Controls.Add($pictureBanner)
        $formInstallationPrompt.Controls.Add($buttonAbort)
        $formInstallationPrompt.Controls.Add($flowLayoutPanel)
        ## Timer
        $installPromptTimer = New-Object -TypeName 'System.Windows.Forms.Timer'
        $installPromptTimer.Interval = ($timeout * 1000)
        $installPromptTimer.Add_Tick({
                Write-Log -Message 'Installation action not taken within a reasonable amount of time.' -Source ${CmdletName}
                $buttonAbort.PerformClick()
            })
        ## Init the OnLoad event to correct the initial state of the form
        $formInstallationPrompt.add_Load($Install_Prompt_Form_StateCorrection_Load)
        ## Clean up the control events
        $formInstallationPrompt.add_FormClosed($Install_Prompt_Form_Cleanup_FormClosed)

        ## Start the timer
        $installPromptTimer.Start()

        ## Persistence Timer
        If ($persistPrompt) {
            $installPromptTimerPersist = New-Object -TypeName 'System.Windows.Forms.Timer'
            $installPromptTimerPersist.Interval = ($configInstallationPersistInterval * 1000)
            [ScriptBlock]$installPromptTimerPersist_Tick = {
                $formInstallationPrompt.WindowState = 'Normal'
                $formInstallationPrompt.TopMost = $TopMost
                $formInstallationPrompt.BringToFront()
                $formInstallationPrompt.Location = "$($formInstallationPromptStartPosition.X),$($formInstallationPromptStartPosition.Y)"
            }
            $installPromptTimerPersist.add_Tick($installPromptTimerPersist_Tick)
            $installPromptTimerPersist.Start()
        }

        If (-not $AsyncToolkitLaunch) {
            ## Close the Installation Progress Dialog if running
            Close-InstallationProgress
        }

        [String]$installPromptLoggedParameters = ($installPromptParameters.GetEnumerator() | ForEach-Object { & $ResolveParameters $_ }) -join ' '
        Write-Log -Message "Displaying custom installation prompt with the parameters: [$installPromptLoggedParameters]." -Source ${CmdletName}


        ## Show the prompt synchronously. If user cancels, then keep showing it until user responds using one of the buttons.
        $showDialog = $true
        While ($showDialog) {
            # Minimize all other windows
            If ($minimizeWindows) {
                $null = $shellApp.MinimizeAll()
            }
            # Show the Form
            $result = $formInstallationPrompt.ShowDialog()
            If (($result -eq 'Yes') -or ($result -eq 'No') -or ($result -eq 'Ignore') -or ($result -eq 'Abort')) {
                $showDialog = $false
            }
        }
        $formInstallationPrompt.Dispose()

        Switch ($result) {
            'Yes' {
                Write-Output -InputObject ($buttonRightText)
            }
            'No' {
                Write-Output -InputObject ($buttonLeftText)
            }
            'Ignore' {
                Write-Output -InputObject ($buttonMiddleText)
            }
            'Abort' {
                # Restore minimized windows
                $null = $shellApp.UndoMinimizeAll()
                If ($ExitOnTimeout) {
                    Exit-Script -ExitCode $configInstallationUIExitCode
                }
                Else {
                    Write-Log -Message 'UI timed out but `$ExitOnTimeout set to `$false. Continue...' -Source ${CmdletName}
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Show-DialogBox
Function Show-DialogBox {
    <#
.SYNOPSIS

Display a custom dialog box with optional title, buttons, icon and timeout.

Show-InstallationPrompt is recommended over this function as it provides more customization and uses consistent branding with the other UI components.

.DESCRIPTION

Display a custom dialog box with optional title, buttons, icon and timeout. The default button is "OK", the default Icon is "None", and the default Timeout is None

.PARAMETER Text

Text in the message dialog box

.PARAMETER Title

Title of the message dialog box

.PARAMETER Buttons

Buttons to be included on the dialog box. Options: OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel, CancelTryAgainContinue. Default: OK.

.PARAMETER DefaultButton

The Default button that is selected. Options: First, Second, Third. Default: First.

.PARAMETER Icon

Icon to display on the dialog box. Options: None, Stop, Question, Exclamation, Information. Default: None

.PARAMETER Timeout

Timeout period in seconds before automatically closing the dialog box with the return message "Timeout". Default: UI timeout value set in the config XML file.

.PARAMETER TopMost

Specifies whether the message box is a system modal message box and appears in a topmost window. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the text of the button that was clicked.

.EXAMPLE

Show-DialogBox -Title 'Installed Complete' -Text 'Installation has completed. Please click OK and restart your computer.' -Icon 'Information'

.EXAMPLE

Show-DialogBox -Title 'Installation Notice' -Text 'Installation will take approximately 30 minutes. Do you wish to proceed?' -Buttons 'OKCancel' -DefaultButton 'Second' -Icon 'Exclamation' -Timeout 600 -Topmost $false

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter a message for the dialog box')]
        [ValidateNotNullorEmpty()]
        [String]$Text,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Title = $installTitle,
        [Parameter(Mandatory = $false)]
        [ValidateSet('OK', 'OKCancel', 'AbortRetryIgnore', 'YesNoCancel', 'YesNo', 'RetryCancel', 'CancelTryAgainContinue')]
        [String]$Buttons = 'OK',
        [Parameter(Mandatory = $false)]
        [ValidateSet('First', 'Second', 'Third')]
        [String]$DefaultButton = 'First',
        [Parameter(Mandatory = $false)]
        [ValidateSet('Exclamation', 'Information', 'None', 'Stop', 'Question')]
        [String]$Icon = 'None',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Timeout = $configInstallationUITimeout,
        [Parameter(Mandatory = $false)]
        [Boolean]$TopMost = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        #  Bypass if in non-interactive mode
        If ($deployModeNonInteractive) {
            Write-Log -Message "Bypassing Show-DialogBox [Mode: $deployMode]. Text:$Text" -Source ${CmdletName}
            Return
        }

        Write-Log -Message "Displaying Dialog Box with message: $Text..." -Source ${CmdletName}

        [Hashtable]$dialogButtons = @{
            'OK'                     = 0
            'OKCancel'               = 1
            'AbortRetryIgnore'       = 2
            'YesNoCancel'            = 3
            'YesNo'                  = 4
            'RetryCancel'            = 5
            'CancelTryAgainContinue' = 6
        }

        [Hashtable]$dialogIcons = @{
            'None'        = 0
            'Stop'        = 16
            'Question'    = 32
            'Exclamation' = 48
            'Information' = 64
        }

        [Hashtable]$dialogDefaultButton = @{
            'First'  = 0
            'Second' = 256
            'Third'  = 512
        }

        Switch ($TopMost) {
            $true {
                $dialogTopMost = 4096
            }
            $false {
                $dialogTopMost = 0
            }
        }

        $response = $Shell.Popup($Text, $Timeout, $Title, ($dialogButtons[$Buttons] + $dialogIcons[$Icon] + $dialogDefaultButton[$DefaultButton] + $dialogTopMost))

        Switch ($response) {
            1 {
                Write-Log -Message 'Dialog Box Response: OK' -Source ${CmdletName}
                Write-Output -InputObject ('OK')
            }
            2 {
                Write-Log -Message 'Dialog Box Response: Cancel' -Source ${CmdletName}
                Write-Output -InputObject ('Cancel')
            }
            3 {
                Write-Log -Message 'Dialog Box Response: Abort' -Source ${CmdletName}
                Write-Output -InputObject ('Abort')
            }
            4 {
                Write-Log -Message 'Dialog Box Response: Retry' -Source ${CmdletName}
                Write-Output -InputObject ('Retry')
            }
            5 {
                Write-Log -Message 'Dialog Box Response: Ignore' -Source ${CmdletName}
                Write-Output -InputObject ('Ignore')
            }
            6 {
                Write-Log -Message 'Dialog Box Response: Yes' -Source ${CmdletName}
                Write-Output -InputObject ('Yes')
            }
            7 {
                Write-Log -Message 'Dialog Box Response: No' -Source ${CmdletName}
                Write-Output -InputObject ('No')
            }
            10 {
                Write-Log -Message 'Dialog Box Response: Try Again' -Source ${CmdletName}
                Write-Output -InputObject ('Try Again')
            }
            11 {
                Write-Log -Message 'Dialog Box Response: Continue' -Source ${CmdletName}
                Write-Output -InputObject ('Continue')
            }
            -1 {
                Write-Log -Message 'Dialog Box Timed Out...' -Source ${CmdletName}
                Write-Output -InputObject ('Timeout')
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-HardwarePlatform
Function Get-HardwarePlatform {
    <#
.SYNOPSIS

Retrieves information about the hardware platform (physical or virtual)

.DESCRIPTION

Retrieves information about the hardware platform (physical or virtual)

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the hardware platform (physical or virtual)

.EXAMPLE

Get-HardwarePlatform

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Retrieving hardware platform information.' -Source ${CmdletName}
            $hwBios = Get-WmiObject -Class 'Win32_BIOS' -ErrorAction 'Stop' | Select-Object -Property 'Version', 'SerialNumber'
            $hwMakeModel = Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'Stop' | Select-Object -Property 'Model', 'Manufacturer'

            If ($hwBIOS.Version -match 'VRTUAL') {
                $hwType = 'Virtual:Hyper-V'
            }
            ElseIf ($hwBIOS.Version -match 'A M I') {
                $hwType = 'Virtual:Virtual PC'
            }
            ElseIf ($hwBIOS.Version -like '*Xen*') {
                $hwType = 'Virtual:Xen'
            }
            ElseIf ($hwBIOS.SerialNumber -like '*VMware*') {
                $hwType = 'Virtual:VMWare'
            }
            ElseIf (($hwMakeModel.Manufacturer -like '*Microsoft*') -and ($hwMakeModel.Model -notlike '*Surface*')) {
                $hwType = 'Virtual:Hyper-V'
            }
            ElseIf ($hwMakeModel.Manufacturer -like '*VMWare*') {
                $hwType = 'Virtual:VMWare'
            }
            ElseIf ($hwMakeModel.Model -like '*Virtual*') {
                $hwType = 'Virtual'
            }
            Else {
                $hwType = 'Physical'
            }

            Write-Output -InputObject ($hwType)
        }
        Catch {
            Write-Log -Message "Failed to retrieve hardware platform information. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to retrieve hardware platform information: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-FreeDiskSpace
Function Get-FreeDiskSpace {
    <#
.SYNOPSIS

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

.DESCRIPTION

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

.PARAMETER Drive

Drive to check free disk space on

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Double

Returns the free disk space in MB

.EXAMPLE

Get-FreeDiskSpace -Drive 'C:'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Drive = $envSystemDrive,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Retrieving free disk space for drive [$Drive]." -Source ${CmdletName}
            $disk = Get-WmiObject -Class 'Win32_LogicalDisk' -Filter "DeviceID='$Drive'" -ErrorAction 'Stop'
            [Double]$freeDiskSpace = [Math]::Round($disk.FreeSpace / 1MB)

            Write-Log -Message "Free disk space for drive [$Drive]: [$freeDiskSpace MB]." -Source ${CmdletName}
            Write-Output -InputObject ($freeDiskSpace)
        }
        Catch {
            Write-Log -Message "Failed to retrieve free disk space for drive [$Drive]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to retrieve free disk space for drive [$Drive]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-InstalledApplication
Function Get-InstalledApplication {
    <#
.SYNOPSIS

Retrieves information about installed applications.

.DESCRIPTION

Retrieves information about installed applications by querying the registry. You can specify an application name, a product code, or both.

Returns information about application publisher, name & version, product code, uninstall string, install source, location, date, and application architecture.

.PARAMETER Name

The name of the application to retrieve information for. Performs a contains match on the application display name by default.

.PARAMETER Exact

Specifies that the named application must be matched using the exact name.

.PARAMETER WildCard

Specifies that the named application must be matched using a wildcard search.

.PARAMETER RegEx

Specifies that the named application must be matched using a regular expression search.

.PARAMETER ProductCode

The product code of the application to retrieve information for.

.PARAMETER IncludeUpdatesAndHotfixes

Include matches against updates and hotfixes in results.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns a PSObject with information about an installed application
- Publisher
- DisplayName
- DisplayVersion
- ProductCode
- UninstallString
- InstallSource
- InstallLocation
- InstallDate
- Architecture

.EXAMPLE

Get-InstalledApplication -Name 'Adobe Flash'

.EXAMPLE
	Get-InstalledApplication -ProductCode '{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
.Outputs
	For every detected matching Application the Function puts out a custom Object containing the following Properties:
	DisplayName, DisplayVersion, InstallDate, Publisher, Is64BitApplication, ProductCode, InstallLocation, UninstallSubkey, UninstallString, InstallSource.
.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String[]]$Name,
        [Parameter(Mandatory = $false)]
        [Switch]$Exact = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$WildCard = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$RegEx = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$ProductCode,
        [Parameter(Mandatory = $false)]
        [Switch]$IncludeUpdatesAndHotfixes
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If ($name) {
            Write-Log -Message "Getting information for installed Application Name(s) [$($name -join ', ')]..." -Source ${CmdletName}
        }
        If ($productCode) {
            Write-Log -Message "Getting information for installed Product Code [$ProductCode]..." -Source ${CmdletName}
        }

        ## Enumerate the installed applications from the registry for applications that have the "DisplayName" property
        [PSObject[]]$regKeyApplication = @()
        ForEach ($regKey in $regKeyApplications) {
            If (Test-Path -LiteralPath $regKey -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorUninstallKeyPath') {
                [PSObject[]]$UninstallKeyApps = Get-ChildItem -LiteralPath $regKey -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorUninstallKeyPath'
                ForEach ($UninstallKeyApp in $UninstallKeyApps) {
                    Try {
                        [PSObject]$regKeyApplicationProps = Get-ItemProperty -LiteralPath $UninstallKeyApp.PSPath -ErrorAction 'Stop'
                        If ($regKeyApplicationProps.DisplayName) {
                            [PSObject[]]$regKeyApplication += $regKeyApplicationProps
                        }
                    }
                    Catch {
                        Write-Log -Message "Unable to enumerate properties from registry key path [$($UninstallKeyApp.PSPath)]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                        Continue
                    }
                }
            }
        }
        If ($ErrorUninstallKeyPath) {
            Write-Log -Message "The following error(s) took place while enumerating installed applications from the registry. `r`n$(Resolve-Error -ErrorRecord $ErrorUninstallKeyPath)" -Severity 2 -Source ${CmdletName}
        }

        $UpdatesSkippedCounter = 0
        ## Create a custom object with the desired properties for the installed applications and sanitize property details
        [PSObject[]]$installedApplication = @()
        ForEach ($regKeyApp in $regKeyApplication) {
            Try {
                [String]$appDisplayName = ''
                [String]$appDisplayVersion = ''
                [String]$appPublisher = ''

                ## Bypass any updates or hotfixes
                If ((-not $IncludeUpdatesAndHotfixes) -and (($regKeyApp.DisplayName -match '(?i)kb\d+') -or ($regKeyApp.DisplayName -match 'Cumulative Update') -or ($regKeyApp.DisplayName -match 'Security Update') -or ($regKeyApp.DisplayName -match 'Hotfix'))) {
                    $UpdatesSkippedCounter += 1
                    Continue
                }

                ## Remove any control characters which may interfere with logging and creating file path names from these variables
                $appDisplayName = $regKeyApp.DisplayName -replace '[^\p{L}\p{Nd}\p{Z}\p{P}]', ''
                $appDisplayVersion = $regKeyApp.DisplayVersion -replace '[^\p{L}\p{Nd}\p{Z}\p{P}]', ''
                $appPublisher = $regKeyApp.Publisher -replace '[^\p{L}\p{Nd}\p{Z}\p{P}]', ''


                ## Determine if application is a 64-bit application
                [Boolean]$Is64BitApp = If (($is64Bit) -and ($regKeyApp.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node')) {
                    $true
                }
                Else {
                    $false
                }

                If ($ProductCode) {
                    ## Verify if there is a match with the product code passed to the script
                    If ($regKeyApp.PSChildName -match [RegEx]::Escape($productCode)) {
                        Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] matching product code [$productCode]." -Source ${CmdletName}
                        $installedApplication += New-Object -TypeName 'PSObject' -Property @{
                            UninstallSubkey    = $regKeyApp.PSChildName
                            ProductCode        = If ($regKeyApp.PSChildName -match $MSIProductCodeRegExPattern) {
                                $regKeyApp.PSChildName
                            }
                            Else {
                                [String]::Empty
                            }
                            DisplayName        = $appDisplayName
                            DisplayVersion     = $appDisplayVersion
                            UninstallString    = $regKeyApp.UninstallString
                            InstallSource      = $regKeyApp.InstallSource
                            InstallLocation    = $regKeyApp.InstallLocation
                            InstallDate        = $regKeyApp.InstallDate
                            Publisher          = $appPublisher
                            Is64BitApplication = $Is64BitApp
                        }
                    }
                }

                If ($name) {
                    ## Verify if there is a match with the application name(s) passed to the script
                    ForEach ($application in $Name) {
                        $applicationMatched = $false
                        If ($exact) {
                            #  Check for an exact application name match
                            If ($regKeyApp.DisplayName -eq $application) {
                                $applicationMatched = $true
                                Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using exact name matching for search term [$application]." -Source ${CmdletName}
                            }
                        }
                        ElseIf ($WildCard) {
                            #  Check for wildcard application name match
                            If ($regKeyApp.DisplayName -like $application) {
                                $applicationMatched = $true
                                Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using wildcard matching for search term [$application]." -Source ${CmdletName}
                            }
                        }
                        ElseIf ($RegEx) {
                            #  Check for a regex application name match
                            If ($regKeyApp.DisplayName -match $application) {
                                $applicationMatched = $true
                                Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using regex matching for search term [$application]." -Source ${CmdletName}
                            }
                        }
                        #  Check for a contains application name match
                        ElseIf ($regKeyApp.DisplayName -match [RegEx]::Escape($application)) {
                            $applicationMatched = $true
                            Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using contains matching for search term [$application]." -Source ${CmdletName}
                        }

                        If ($applicationMatched) {
                            $installedApplication += New-Object -TypeName 'PSObject' -Property @{
                                UninstallSubkey    = $regKeyApp.PSChildName
                                ProductCode        = If ($regKeyApp.PSChildName -match $MSIProductCodeRegExPattern) {
                                    $regKeyApp.PSChildName
                                }
                                Else {
                                    [String]::Empty
                                }
                                DisplayName        = $appDisplayName
                                DisplayVersion     = $appDisplayVersion
                                UninstallString    = $regKeyApp.UninstallString
                                InstallSource      = $regKeyApp.InstallSource
                                InstallLocation    = $regKeyApp.InstallLocation
                                InstallDate        = $regKeyApp.InstallDate
                                Publisher          = $appPublisher
                                Is64BitApplication = $Is64BitApp
                            }
                        }
                    }
                }
            }
            Catch {
                Write-Log -Message "Failed to resolve application details from registry for [$appDisplayName]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                Continue
            }
        }

        If (-not $IncludeUpdatesAndHotfixes) {
            ## Write to log the number of entries skipped due to them being considered updates
            If ($UpdatesSkippedCounter -eq 1) {
                Write-Log -Message 'Skipped 1 entry while searching, because it was considered a Microsoft update.' -Source ${CmdletName}
            }
            Else {
                Write-Log -Message "Skipped $UpdatesSkippedCounter entries while searching, because they were considered Microsoft updates." -Source ${CmdletName}
            }
        }

        If (-not $installedApplication) {
            Write-Log -Message 'Found no application based on the supplied parameters.' -Source ${CmdletName}
        }

        Write-Output -InputObject ($installedApplication)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Execute-MSI
Function Execute-MSI {
    <#
.SYNOPSIS

Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.

.DESCRIPTION

Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.

If the -Action parameter is set to "Install" and the MSI is already installed, the function will exit.

Sets default switches to be passed to msiexec based on the preferences in the XML configuration file.

Automatically generates a log file name and creates a verbose log file for all msiexec operations.

Expects the MSI or MSP file to be located in the "Files" sub directory of the App Deploy Toolkit. Expects transform files to be in the same directory as the MSI file.

.PARAMETER Action

The action to perform. Options: Install, Uninstall, Patch, Repair, ActiveSetup.

.PARAMETER Path

The path to the MSI/MSP file or the product code of the installed MSI.

.PARAMETER Transform

The name of the transform file(s) to be applied to the MSI. The transform file is expected to be in the same directory as the MSI file. Multiple transforms have to be separated by a semi-colon.

.PARAMETER Patch

The name of the patch (msp) file(s) to be applied to the MSI for use with the "Install" action. The patch file is expected to be in the same directory as the MSI file. Multiple patches have to be separated by a semi-colon.

.PARAMETER Parameters

Overrides the default parameters specified in the XML configuration file. Install default is: "REBOOT=ReallySuppress /QB!". Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER AddParameters

Adds to the default parameters specified in the XML configuration file. Install default is: "REBOOT=ReallySuppress /QB!". Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER SecureParameters

Hides all parameters passed to the MSI or MSP file from the toolkit Log file.

.PARAMETER LoggingOptions

Overrides the default logging options specified in the XML configuration file. Default options are: "/L*v".

.PARAMETER LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.

For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER WorkingDirectory

Overrides the working directory. The working directory is set to the location of the MSI file.

.PARAMETER SkipMSIAlreadyInstalledCheck

Skips the check to determine if the MSI is already installed on the system. Default is: $false.

.PARAMETER IncludeUpdatesAndHotfixes

Include matches against updates and hotfixes in results.

.PARAMETER NoWait

Immediately continue after executing the process.

.PARAMETER PassThru

Returns ExitCode, STDOut, and STDErr output from the process.

.PARAMETER IgnoreExitCodes

List the exit codes to ignore or * to ignore all exit codes.

.PARAMETER PriorityClass

Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime. Default: Normal

.PARAMETER ExitOnProcessFailure

Specifies whether the function should call Exit-Script when the process returns an exit code that is considered an error/failure. Default: $true

.PARAMETER RepairFromSource

Specifies whether we should repair from source. Also rewrites local cache. Default: $false

.PARAMETER ContinueOnError

Continue if an error occurred while trying to start the process. Default: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns a PSObject with the results of the installation
- ExitCode
- STDOut
- STDErr

.EXAMPLE

Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'

Installs an MSI

.EXAMPLE

Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transform 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -Parameters '/QN'

Installs an MSI, applying a transform and overriding the default MSI toolkit parameters

.EXAMPLE

[PSObject]$ExecuteMSIResult = Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru

Installs an MSI and stores the result of the execution into a variable by using the -PassThru option

.EXAMPLE

Execute-MSI -Action 'Uninstall' -Path '{26923b43-4d38-484f-9b9e-de460746276c}'

Uninstalls an MSI using a product code

.EXAMPLE

Execute-MSI -Action 'Patch' -Path 'Adobe_Reader_11.0.3_EN.msp'

Installs an MSP

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Patch', 'Repair', 'ActiveSetup')]
        [String]$Action = 'Install',
        [Parameter(Mandatory = $true, HelpMessage = 'Please enter either the path to the MSI/MSP file or the ProductCode')]
        [ValidateScript({ ($_ -match $MSIProductCodeRegExPattern) -or ('.msi', '.msp' -contains [IO.Path]::GetExtension($_)) })]
        [Alias('FilePath')]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Transform,
        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [String]$Parameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$SecureParameters = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Patch,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$LoggingOptions,
        [Parameter(Mandatory = $false)]
        [Alias('LogName')]
        [String]$private:LogName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$SkipMSIAlreadyInstalledCheck = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$IncludeUpdatesAndHotfixes = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$NoWait = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IgnoreExitCodes,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [Diagnostics.ProcessPriorityClass]$PriorityClass = 'Normal',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExitOnProcessFailure = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$RepairFromSource = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Initialize variable indicating whether $Path variable is a Product Code or not
        [Boolean]$PathIsProductCode = $false

        ## If the path matches a product code
        If ($Path -match $MSIProductCodeRegExPattern) {
            #  Set variable indicating that $Path variable is a Product Code
            [Boolean]$PathIsProductCode = $true

            #  Resolve the product code to a publisher, application name, and version
            Write-Log -Message 'Resolving product code to a publisher, application name, and version.' -Source ${CmdletName}

            If ($IncludeUpdatesAndHotfixes) {
                [PSObject]$productCodeNameVersion = Get-InstalledApplication -ProductCode $path -IncludeUpdatesAndHotfixes | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'SilentlyContinue'
            }
            Else {
                [PSObject]$productCodeNameVersion = Get-InstalledApplication -ProductCode $path | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'SilentlyContinue'
            }

            #  Build the log file name
            If (-not $logName) {
                If ($productCodeNameVersion) {
                    If ($productCodeNameVersion.Publisher) {
                        $logName = (Remove-InvalidFileNameChars -Name ($productCodeNameVersion.Publisher + '_' + $productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ', ''
                    }
                    Else {
                        $logName = (Remove-InvalidFileNameChars -Name ($productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ', ''
                    }
                }
                Else {
                    #  Out of other options, make the Product Code the name of the log file
                    $logName = $Path
                }
            }
        }
        Else {
            #  Get the log file name without file extension
            If (-not $logName) {
                $logName = ([IO.FileInfo]$path).BaseName
            }
            ElseIf ('.log', '.txt' -contains [IO.Path]::GetExtension($logName)) {
                $logName = [IO.Path]::GetFileNameWithoutExtension($logName)
            }
        }

        If ($configToolkitCompressLogs) {
            ## Build the log file path
            [String]$logPath = Join-Path -Path $logTempFolder -ChildPath $logName
        }
        Else {
            ## Create the Log directory if it doesn't already exist
            If (-not (Test-Path -LiteralPath $configMSILogDir -PathType 'Container' -ErrorAction 'SilentlyContinue')) {
                $null = New-Item -Path $configMSILogDir -ItemType 'Directory' -ErrorAction 'SilentlyContinue'
            }
            ## Build the log file path
            [String]$logPath = Join-Path -Path $configMSILogDir -ChildPath $logName
        }

        ## Set the installation Parameters
        If ($deployModeSilent) {
            $msiInstallDefaultParams = $configMSISilentParams
            $msiUninstallDefaultParams = $configMSISilentParams
        }
        Else {
            $msiInstallDefaultParams = $configMSIInstallParams
            $msiUninstallDefaultParams = $configMSIUninstallParams
        }

        ## Build the MSI Parameters
        Switch ($action) {
            'Install' {
                $option = '/i'; [String]$msiLogFile = "$logPath" + '_Install'; $msiDefaultParams = $msiInstallDefaultParams
            }
            'Uninstall' {
                $option = '/x'; [String]$msiLogFile = "$logPath" + '_Uninstall'; $msiDefaultParams = $msiUninstallDefaultParams
            }
            'Patch' {
                $option = '/update'; [String]$msiLogFile = "$logPath" + '_Patch'; $msiDefaultParams = $msiInstallDefaultParams
            }
            'Repair' {
                $option = '/f'; If ($RepairFromSource) {
                    $option += 'vomus'
                } [String]$msiLogFile = "$logPath" + '_Repair'; $msiDefaultParams = $msiInstallDefaultParams
            }
            'ActiveSetup' {
                $option = '/fups'; [String]$msiLogFile = "$logPath" + '_ActiveSetup'
            }
        }

        ## Append ".log" to the MSI logfile path and enclose in quotes
        If ([IO.Path]::GetExtension($msiLogFile) -ne '.log') {
            [String]$msiLogFile = $msiLogFile + '.log'
            [String]$msiLogFile = "`"$msiLogFile`""
        }

        ## If the MSI is in the Files directory, set the full path to the MSI
        If (Test-Path -LiteralPath (Join-Path -Path $dirFiles -ChildPath $path -ErrorAction 'SilentlyContinue') -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
            [String]$msiFile = Join-Path -Path $dirFiles -ChildPath $path
        }
        ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'SilentlyContinue') {
            [String]$msiFile = (Get-Item -LiteralPath $Path).FullName
        }
        ElseIf ($PathIsProductCode) {
            [String]$msiFile = $Path
        }
        Else {
            Write-Log -Message "Failed to find MSI file [$path]." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to find MSI file [$path]."
            }
            Continue
        }

        ## Set the working directory of the MSI
        If ((-not $PathIsProductCode) -and (-not $workingDirectory)) {
            [String]$workingDirectory = Split-Path -Path $msiFile -Parent
        }

        ## Enumerate all transforms specified, qualify the full path if possible and enclose in quotes
        If ($transform) {
            [String[]]$transforms = $transform -replace "`"", '' -split ';'
            For ($i = 0; $i -lt $transforms.Length; $i++) {
                [String]$FullPath = $null
                [String]$FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $transforms[$i].Replace('.\', '')
                If ($FullPath -and (Test-Path -LiteralPath $FullPath -PathType 'Leaf')) {
                    $transforms[$i] = $FullPath
                }
            }
            [String]$mstFile = "`"$($transforms -join ';')`""
        }

        ## Enumerate all patches specified, qualify the full path if possible and enclose in quotes
        If ($patch) {
            [String[]]$patches = $patch -replace "`"", '' -split ';'
            For ($i = 0; $i -lt $patches.Length; $i++) {
                [String]$FullPath = $null
                [String]$FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $patches[$i].Replace('.\', '')
                If ($FullPath -and (Test-Path -LiteralPath $FullPath -PathType 'Leaf')) {
                    $patches[$i] = $FullPath
                }
            }
            [String]$mspFile = "`"$($patches -join ';')`""
        }

        ## Get the ProductCode of the MSI
        If ($PathIsProductCode) {
            [String]$MSIProductCode = $path
        }
        ElseIf ([IO.Path]::GetExtension($msiFile) -eq '.msi') {
            Try {
                [Hashtable]$GetMsiTablePropertySplat = @{ Path = $msiFile; Table = 'Property'; ContinueOnError = $false }
                If ($transforms) {
                    $GetMsiTablePropertySplat.Add( 'TransformPath', $transforms )
                }
                [String]$MSIProductCode = Get-MsiTableProperty @GetMsiTablePropertySplat | Select-Object -ExpandProperty 'ProductCode' -ErrorAction 'Stop'
            }
            Catch {
                Write-Log -Message "Failed to get the ProductCode from the MSI file. Continue with requested action [$Action]..." -Source ${CmdletName}
            }
        }

        ## Enclose the MSI file in quotes to avoid issues with spaces when running msiexec
        [String]$msiFile = "`"$msiFile`""

        ## Start building the MsiExec command line starting with the base action and file
        [String]$argsMSI = "$option $msiFile"
        #  Add MST
        If ($transform) {
            $argsMSI = "$argsMSI TRANSFORMS=$mstFile TRANSFORMSSECURE=1"
        }
        #  Add MSP
        If ($patch) {
            $argsMSI = "$argsMSI PATCH=$mspFile"
        }
        #  Replace default parameters if specified.
        If ($Parameters) {
            $argsMSI = "$argsMSI $Parameters"
        }
        Else {
            $argsMSI = "$argsMSI $msiDefaultParams"
        }
        #  Add reinstallmode and reinstall variable for Patch
        If ($action -eq 'Patch') {
            $argsMSI += ' REINSTALLMODE=ecmus REINSTALL=ALL'
        }
        #  Append parameters to default parameters if specified.
        If ($AddParameters) {
            $argsMSI = "$argsMSI $AddParameters"
        }
        #  Add custom Logging Options if specified, otherwise, add default Logging Options from Config file
        If ($LoggingOptions) {
            $argsMSI = "$argsMSI $LoggingOptions $msiLogFile"
        }
        Else {
            $argsMSI = "$argsMSI $configMSILoggingOptions $msiLogFile"
        }

        ## Check if the MSI is already installed. If no valid ProductCode to check, then continue with requested MSI action.
        If ($MSIProductCode) {
            If ($SkipMSIAlreadyInstalledCheck) {
                [Boolean]$IsMsiInstalled = $false
            }
            Else {
                If ($IncludeUpdatesAndHotfixes) {
                    [PSObject]$MsiInstalled = Get-InstalledApplication -ProductCode $MSIProductCode -IncludeUpdatesAndHotfixes
                }
                Else {
                    [PSObject]$MsiInstalled = Get-InstalledApplication -ProductCode $MSIProductCode
                }
                If ($MsiInstalled) {
                    [Boolean]$IsMsiInstalled = $true
                }
            }
        }
        Else {
            If ($Action -eq 'Install') {
                [Boolean]$IsMsiInstalled = $false
            }
            Else {
                [Boolean]$IsMsiInstalled = $true
            }
        }

        If (($IsMsiInstalled) -and ($Action -eq 'Install')) {
            Write-Log -Message "The MSI is already installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
            [PSObject]$ExecuteResults = @{ ExitCode = 1638; StdOut = 0; StdErr = '' }
        }
        ElseIf (((-not $IsMsiInstalled) -and ($Action -eq 'Install')) -or ($IsMsiInstalled)) {
            Write-Log -Message "Executing MSI action [$Action]..." -Source ${CmdletName}
            #  Build the hashtable with the options that will be passed to Execute-Process using splatting
            [Hashtable]$ExecuteProcessSplat = @{
                Path                 = $exeMsiexec
                Parameters           = $argsMSI
                WindowStyle          = 'Normal'
                ExitOnProcessFailure = $ExitOnProcessFailure
                ContinueOnError      = $ContinueOnError
            }
            If ($WorkingDirectory) {
                $ExecuteProcessSplat.Add( 'WorkingDirectory', $WorkingDirectory)
            }
            If ($SecureParameters) {
                $ExecuteProcessSplat.Add( 'SecureParameters', $SecureParameters)
            }
            If ($PassThru) {
                $ExecuteProcessSplat.Add( 'PassThru', $PassThru)
            }
            If ($IgnoreExitCodes) {
                $ExecuteProcessSplat.Add( 'IgnoreExitCodes', $IgnoreExitCodes)
            }
            If ($PriorityClass) {
                $ExecuteProcessSplat.Add( 'PriorityClass', $PriorityClass)
            }
            If ($NoWait) {
                $ExecuteProcessSplat.Add( 'NoWait', $NoWait)
            }

            #  Call the Execute-Process function
            If ($PassThru) {
                [PSObject]$ExecuteResults = Execute-Process @ExecuteProcessSplat 
            }
            Else {
                Execute-Process @ExecuteProcessSplat
            }
            #  Refresh environment variables for Windows Explorer process as Windows does not consistently update environment variables created by MSIs
            Update-Desktop
        }
        Else {
            Write-Log -Message "The MSI is not installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
        }
    }
    End {
        If ($PassThru) {
            Write-Output -InputObject ($ExecuteResults)
        }
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Remove-MSIApplications
Function Remove-MSIApplications {
    <#
.SYNOPSIS

Removes all MSI applications matching the specified application name.

.DESCRIPTION

Removes all MSI applications matching the specified application name.
Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code, provided the uninstall string matches "msiexec".

.PARAMETER Name

The name of the application to uninstall. Performs a contains match on the application display name by default.

.PARAMETER Exact

Specifies that the named application must be matched using the exact name.

.PARAMETER WildCard

Specifies that the named application must be matched using a wildcard search.

.PARAMETER Parameters

Overrides the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER AddParameters

Adds to the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER FilterApplication

Two-dimensional array that contains one or more (property, value, match-type) sets that should be used to filter the list of results returned by Get-InstalledApplication to only those that should be uninstalled.
Properties that can be filtered upon: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

.PARAMETER ExcludeFromUninstall

Two-dimensional array that contains one or more (property, value, match-type) sets that should be excluded from uninstall if found.
Properties that can be excluded: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

.PARAMETER IncludeUpdatesAndHotfixes

Include matches against updates and hotfixes in results.

.PARAMETER LoggingOptions

Overrides the default logging options specified in the XML configuration file. Default options are: "/L*v".

.PARAMETER LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER PassThru

Returns ExitCode, STDOut, and STDErr output from the process.

.PARAMETER ContinueOnError

Continue if an error occured while trying to start the processes. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns an object with the following properties:
- ExitCode
- StdOut
- StdErr

.EXAMPLE

Remove-MSIApplications -Name 'Adobe Flash'

Removes all versions of software that match the name "Adobe Flash"

.EXAMPLE

Remove-MSIApplications -Name 'Adobe'

Removes all versions of software that match the name "Adobe"

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(
		@('Is64BitApplication', $false, 'Exact'),
		@('Publisher', 'Oracle Corporation', 'Exact')
	)

Removes all versions of software that match the name "Java 8 Update" where the software is 32-bits and the publisher is "Oracle Corporation".

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(, @('Publisher', 'Oracle Corporation', 'Exact')) -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))

Removes all versions of software that match the name "Java 8 Update" and also have "Oracle Corporation" as the Publisher; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional arrays, the array must have the extra parentheses and leading comma as in this example.

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional array, the array must have the extra parentheses and leading comma as in this example.

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(
	@('Is64BitApplication', $true, 'Exact'),
	@('DisplayName', 'Java 8 Update 45', 'Exact'),
	@('DisplayName', 'Java 8 Update 4*', 'WildCard'),
	@('DisplayName', 'Java \d Update \d{3}', 'RegEx'),
	@('DisplayName', 'Java 8 Update', 'Contains'))

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall 64-bit versions of the software, Update 45 of the software, or any Update that starts with 4.

.NOTES

More reading on how to create arrays if having trouble with -FilterApplication or -ExcludeFromUninstall parameter: http://blogs.msdn.com/b/powershell/archive/2007/01/23/array-literals-in-powershell.aspx

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [Switch]$Exact = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$WildCard = $false,
        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [String]$Parameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Array]$FilterApplication = @(@()),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Array]$ExcludeFromUninstall = @(@()),
        [Parameter(Mandatory = $false)]
        [Switch]$IncludeUpdatesAndHotfixes = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$LoggingOptions,
        [Parameter(Mandatory = $false)]
        [Alias('LogName')]
        [String]$private:LogName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Build the hashtable with the options that will be passed to Get-InstalledApplication using splatting
        [Hashtable]$GetInstalledApplicationSplat = @{ Name = $name }
        If ($Exact) {
            $GetInstalledApplicationSplat.Add( 'Exact', $Exact)
        }
        ElseIf ($WildCard) {
            $GetInstalledApplicationSplat.Add( 'WildCard', $WildCard)
        }
        If ($IncludeUpdatesAndHotfixes) {
            $GetInstalledApplicationSplat.Add( 'IncludeUpdatesAndHotfixes', $IncludeUpdatesAndHotfixes)
        }

        [PSObject[]]$installedApplications = Get-InstalledApplication @GetInstalledApplicationSplat

        Write-Log -Message "Found [$($installedApplications.Count)] application(s) that matched the specified criteria [$Name]." -Source ${CmdletName}

        ## Filter the results from Get-InstalledApplication
        [Collections.ArrayList]$removeMSIApplications = New-Object -TypeName 'System.Collections.ArrayList'
        If (($null -ne $installedApplications) -and ($installedApplications.Count)) {
            ForEach ($installedApplication in $installedApplications) {
                If ([String]::IsNullOrEmpty($installedApplication.ProductCode)) {
                    Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName)] because unable to discover MSI ProductCode from application's registry Uninstall subkey [$($installedApplication.UninstallSubkey)]." -Severity 2 -Source ${CmdletName}
                    Continue
                }

                #  Filter the results from Get-InstalledApplication to only those that should be uninstalled
                [Boolean]$addAppToRemoveList = $true
                If (($null -ne $FilterApplication) -and ($FilterApplication.Count)) {
                    Write-Log -Message 'Filter the results to only those that should be uninstalled as specified in parameter [-FilterApplication].' -Source ${CmdletName}
                    ForEach ($Filter in $FilterApplication) {
                        If ($Filter[2] -eq 'RegEx') {
                            If ($installedApplication.($Filter[0]) -match $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of regex match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'Contains') {
                            If ($installedApplication.($Filter[0]) -match [RegEx]::Escape($Filter[1])) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of contains match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'WildCard') {
                            If ($installedApplication.($Filter[0]) -like $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of wildcard match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'Exact') {
                            If ($installedApplication.($Filter[0]) -eq $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of exact match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                    }
                }

                #  Filter the results from Get-InstalledApplication to remove those that should never be uninstalled
                If (($null -ne $ExcludeFromUninstall) -and ($ExcludeFromUninstall.Count)) {
                    ForEach ($Exclude in $ExcludeFromUninstall) {
                        If ($Exclude[2] -eq 'RegEx') {
                            If ($installedApplication.($Exclude[0]) -match $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of regex match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'Contains') {
                            If ($installedApplication.($Exclude[0]) -match [RegEx]::Escape($Exclude[1])) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of contains match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'WildCard') {
                            If ($installedApplication.($Exclude[0]) -like $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of wildcard match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'Exact') {
                            If ($installedApplication.($Exclude[0]) -eq $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of exact match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                    }
                }

                If ($addAppToRemoveList) {
                    Write-Log -Message "Adding application to list for removal: [$($installedApplication.DisplayName) $($installedApplication.Version)]." -Source ${CmdletName}
                    $removeMSIApplications.Add($installedApplication)
                }
            }
        }

        ## Build the hashtable with the options that will be passed to Execute-MSI using splatting
        [Hashtable]$ExecuteMSISplat = @{
            Action          = 'Uninstall'
            Path            = ''
            ContinueOnError = $ContinueOnError
        }
        If ($Parameters) {
            $ExecuteMSISplat.Add( 'Parameters', $Parameters)
        }
        ElseIf ($AddParameters) {
            $ExecuteMSISplat.Add( 'AddParameters', $AddParameters)
        }
        If ($LoggingOptions) {
            $ExecuteMSISplat.Add( 'LoggingOptions', $LoggingOptions)
        }
        If ($LogName) {
            $ExecuteMSISplat.Add( 'LogName', $LogName)
        }
        If ($PassThru) {
            $ExecuteMSISplat.Add( 'PassThru', $PassThru)
        }
        If ($IncludeUpdatesAndHotfixes) {
            $ExecuteMSISplat.Add( 'IncludeUpdatesAndHotfixes', $IncludeUpdatesAndHotfixes)
        }

        If (($null -ne $removeMSIApplications) -and ($removeMSIApplications.Count)) {
            ForEach ($removeMSIApplication in $removeMSIApplications) {
                Write-Log -Message "Removing application [$($removeMSIApplication.DisplayName) $($removeMSIApplication.Version)]." -Source ${CmdletName}
                $ExecuteMSISplat.Path = $removeMSIApplication.ProductCode
                If ($PassThru) {
                    [PSObject[]]$ExecuteResults += Execute-MSI @ExecuteMSISplat
                }
                Else {
                    Execute-MSI @ExecuteMSISplat
                }
            }
        }
        Else {
            Write-Log -Message 'No applications found for removal. Continue...' -Source ${CmdletName}
        }
    }
    End {
        If ($PassThru) {
            Write-Output -InputObject ($ExecuteResults)
        }
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Execute-Process
Function Execute-Process {
    <#
.SYNOPSIS

Execute a process with optional arguments, working directory, window style.

.DESCRIPTION

Executes a process, e.g. a file included in the Files directory of the App Deploy Toolkit, or a file on the local machine.
Provides various options for handling the return codes (see Parameters).

.PARAMETER Path

Path to the file to be executed. If the file is located directly in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.
Otherwise, the full path of the file must be specified. If the files is in a subdirectory of "Files", use the "$dirFiles" variable as shown in the example.

.PARAMETER Parameters

Arguments to be passed to the executable

.PARAMETER SecureParameters

Hides all parameters passed to the executable from the Toolkit log file

.PARAMETER WindowStyle

Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Default: Normal.
Note: Not all processes honor WindowStyle. WindowStyle is a recommendation passed to the process. They can choose to ignore it.
Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

.PARAMETER CreateNoWindow

Specifies whether the process should be started with a new window to contain it. Only works for Console mode applications. UseShellExecute should be set to $false.
Default is false.

.PARAMETER WorkingDirectory

The working directory used for executing the process. Defaults to the directory of the file being executed.
Parameter UseShellExecute affects this parameter.

.PARAMETER NoWait

Immediately continue after executing the process.

.PARAMETER PassThru

If NoWait is not specified, returns an object with ExitCode, STDOut and STDErr output from the process. If NoWait is specified, returns an object with Id, Handle and ProcessName.

.PARAMETER WaitForMsiExec

Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that
this function waits for the msiexec engine to become available before starting the install.

.PARAMETER MsiExecWaitTime

Specify the length of time in seconds to wait for the msiexec engine to become available. Default: 600 seconds (10 minutes).

.PARAMETER IgnoreExitCodes

List the exit codes to ignore or * to ignore all exit codes.

.PARAMETER PriorityClass

Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime. Default: Normal

.PARAMETER ExitOnProcessFailure

Specifies whether the function should call Exit-Script when the process returns an exit code that is considered an error/failure. Default: $true

.PARAMETER UseShellExecute

Specifies whether to use the operating system shell to start the process. $true if the shell should be used when starting the process; $false if the process should be created directly from the executable file.

The word "Shell" in this context refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, bash or sh) and lets users launch graphical applications or open documents.
It lets you open a file or a url and the Shell will figure out the program to open it with.
The WorkingDirectory property behaves differently depending on the value of the UseShellExecute property. When UseShellExecute is true, the WorkingDirectory property specifies the location of the executable.
When UseShellExecute is false, the WorkingDirectory property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process.
If you set UseShellExecute to $true, there will be no available output from the process.

Default: $false

.PARAMETER ContinueOnError

Continue if an error occured while trying to start the process. Default: $false.

.EXAMPLE

Execute-Process -Path 'uninstall_flash_player_64bit.exe' -Parameters '/uninstall' -WindowStyle 'Hidden'

If the file is in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Execute-Process -Path "$dirFiles\Bin\setup.exe" -Parameters '/S' -WindowStyle 'Hidden'

.EXAMPLE

Execute-Process -Path 'setup.exe' -Parameters '/S' -IgnoreExitCodes '1,2'

.EXAMPLE

Execute-Process -Path 'setup.exe' -Parameters "-s -f2`"$configToolkitLogDir\$installName.log`""

Launch InstallShield "setup.exe" from the ".\Files" sub-directory and force log files to the logging folder.

.EXAMPLE

Execute-Process -Path 'setup.exe' -Parameters "/s /v`"ALLUSERS=1 /qn /L* \`"$configToolkitLogDir\$installName.log`"`""

Launch InstallShield "setup.exe" with embedded MSI and force log files to the logging folder.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [Alias('FilePath')]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [String[]]$Parameters,
        [Parameter(Mandatory = $false)]
        [Switch]$SecureParameters = $false,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Hidden', 'Maximized', 'Minimized')]
        [Diagnostics.ProcessWindowStyle]$WindowStyle = 'Normal',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$CreateNoWindow = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [Switch]$NoWait = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$WaitForMsiExec = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$MsiExecWaitTime = $configMSIMutexWaitTime,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IgnoreExitCodes,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [Diagnostics.ProcessPriorityClass]$PriorityClass = 'Normal',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExitOnProcessFailure = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$UseShellExecute = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            $private:returnCode = $null

            ## Validate and find the fully qualified path for the $Path variable.
            If (([IO.Path]::IsPathRooted($Path)) -and ([IO.Path]::HasExtension($Path))) {
                Write-Log -Message "[$Path] is a valid fully qualified path, continue." -Source ${CmdletName}
                If (-not (Test-Path -LiteralPath $Path -PathType 'Leaf' -ErrorAction 'Stop')) {
                    Write-Log -Message "File [$Path] not found." -Severity 3 -Source ${CmdletName}
                    If (-not $ContinueOnError) {
                        Throw "File [$Path] not found."
                    }
                    Return
                }
            }
            Else {
                #  The first directory to search will be the 'Files' subdirectory of the script directory
                [String]$PathFolders = $dirFiles
                #  Add the current location of the console (Windows always searches this location first)
                [String]$PathFolders = $PathFolders + ';' + (Get-Location -PSProvider 'FileSystem').Path
                #  Add the new path locations to the PATH environment variable
                $env:PATH = $PathFolders + ';' + $env:PATH

                #  Get the fully qualified path for the file. Get-Command searches PATH environment variable to find this value.
                [String]$FullyQualifiedPath = Get-Command -Name $Path -CommandType 'Application' -TotalCount 1 -Syntax -ErrorAction 'Stop'

                #  Revert the PATH environment variable to it's original value
                $env:PATH = $env:PATH -replace [RegEx]::Escape($PathFolders + ';'), ''

                If ($FullyQualifiedPath) {
                    Write-Log -Message "[$Path] successfully resolved to fully qualified path [$FullyQualifiedPath]." -Source ${CmdletName}
                    $Path = $FullyQualifiedPath
                }
                Else {
                    Write-Log -Message "[$Path] contains an invalid path or file name." -Severity 3 -Source ${CmdletName}
                    If (-not $ContinueOnError) {
                        Throw "[$Path] contains an invalid path or file name."
                    }
                    Return
                }
            }

            ## Set the Working directory (if not specified)
            If (-not $WorkingDirectory) {
                $WorkingDirectory = Split-Path -Path $Path -Parent -ErrorAction 'Stop'
            }

            ## If MSI install, check to see if the MSI installer service is available or if another MSI install is already underway.
            ## Please note that a race condition is possible after this check where another process waiting for the MSI installer
            ##  to become available grabs the MSI Installer mutex before we do. Not too concerned about this possible race condition.
            If (($Path -match 'msiexec') -or ($WaitForMsiExec)) {
                [Timespan]$MsiExecWaitTimeSpan = New-TimeSpan -Seconds $MsiExecWaitTime
                [Boolean]$MsiExecAvailable = Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds $MsiExecWaitTimeSpan.TotalMilliseconds
                Start-Sleep -Seconds 1
                If (-not $MsiExecAvailable) {
                    #  Default MSI exit code for install already in progress
                    [Int32]$returnCode = 1618
                    Write-Log -Message 'Another MSI installation is already in progress and needs to be completed before proceeding with this installation.' -Severity 3 -Source ${CmdletName}
                    If (-not $ContinueOnError) {
                        Throw 'Another MSI installation is already in progress and needs to be completed before proceeding with this installation.'
                    }
                    Return
                }
            }

            Try {
                ## Disable Zone checking to prevent warnings when running executables
                $env:SEE_MASK_NOZONECHECKS = 1

                ## Using this variable allows capture of exceptions from .NET methods. Private scope only changes value for current function.
                $private:previousErrorActionPreference = $ErrorActionPreference
                $ErrorActionPreference = 'Stop'

                ## Define process
                $processStartInfo = New-Object -TypeName 'System.Diagnostics.ProcessStartInfo' -ErrorAction 'Stop'
                $processStartInfo.FileName = $Path
                $processStartInfo.WorkingDirectory = $WorkingDirectory
                $processStartInfo.UseShellExecute = $UseShellExecute
                $processStartInfo.ErrorDialog = $false
                $processStartInfo.RedirectStandardOutput = $true
                $processStartInfo.RedirectStandardError = $true
                $processStartInfo.CreateNoWindow = $CreateNoWindow
                If ($Parameters) {
                    $processStartInfo.Arguments = $Parameters
                }
                $processStartInfo.WindowStyle = $WindowStyle
                If ($processStartInfo.UseShellExecute -eq $true) {
                    Write-Log -Message 'UseShellExecute is set to true, standard output and error will not be available.' -Source ${CmdletName}
                    $processStartInfo.RedirectStandardOutput = $false
                    $processStartInfo.RedirectStandardError = $false
                }
                $process = New-Object -TypeName 'System.Diagnostics.Process' -ErrorAction 'Stop'
                $process.StartInfo = $processStartInfo

                If ($processStartInfo.UseShellExecute -eq $false) {
                    ## Add event handler to capture process's standard output redirection
                    [ScriptBlock]$processEventHandler = { If (-not [String]::IsNullOrEmpty($EventArgs.Data)) {
                            $Event.MessageData.AppendLine($EventArgs.Data)
                        } }
                    $stdOutBuilder = New-Object -TypeName 'System.Text.StringBuilder' -ArgumentList ('')
                    $stdOutEvent = Register-ObjectEvent -InputObject $process -Action $processEventHandler -EventName 'OutputDataReceived' -MessageData $stdOutBuilder -ErrorAction 'Stop'
                    $stdErrBuilder = New-Object -TypeName 'System.Text.StringBuilder' -ArgumentList ('')
                    $stdErrEvent = Register-ObjectEvent -InputObject $process -Action $processEventHandler -EventName 'ErrorDataReceived' -MessageData $stdErrBuilder -ErrorAction 'Stop'
                }

                ## Start Process
                Write-Log -Message "Working Directory is [$WorkingDirectory]." -Source ${CmdletName}
                If ($Parameters) {
                    If ($Parameters -match '-Command \&') {
                        Write-Log -Message "Executing [$Path [PowerShell ScriptBlock]]..." -Source ${CmdletName}
                    }
                    Else {
                        If ($SecureParameters) {
                            Write-Log -Message "Executing [$Path (Parameters Hidden)]..." -Source ${CmdletName}
                        }
                        Else {
                            Write-Log -Message "Executing [$Path $Parameters]..." -Source ${CmdletName}
                        }
                    }
                }
                Else {
                    Write-Log -Message "Executing [$Path]..." -Source ${CmdletName}
                }

                $null = $process.Start()
                ## Set priority
                If ($PriorityClass -ne 'Normal') {
                    Try {
                        If ($process.HasExited -eq $false) {
                            Write-Log -Message "Changing the priority class for the process to [$PriorityClass]" -Source ${CmdletName}
                            $process.PriorityClass = $PriorityClass
                        }
                        Else {
                            Write-Log -Message "Cannot change the priority class for the process to [$PriorityClass], because the process has exited already." -Severity 2 -Source ${CmdletName}
                        }

                    }
                    Catch {
                        Write-Log -Message 'Failed to change the priority class for the process.' -Severity 2 -Source ${CmdletName}
                    }
                }
                ## NoWait specified, return process details. If it isn't specified, start reading standard Output and Error streams
                If ($NoWait) {
                    Write-Log -Message 'NoWait parameter specified. Continuing without waiting for exit code...' -Source ${CmdletName}

                    If ($PassThru) {
                        If ($process.HasExited -eq $false) {
                            Write-Log -Message 'PassThru parameter specified, returning process details object.' -Source ${CmdletName}
                            [PSObject]$ProcessDetails = New-Object -TypeName 'PSObject' -Property @{ Id = If ($process.Id) {
                                    $process.Id
                                }
                                Else {
                                    $null
                                } ; Handle                                                              = If ($process.Handle) {
                                    $process.Handle
                                }
                                Else {
                                    [IntPtr]::Zero
                                }; ProcessName                                                          = If ($process.ProcessName) {
                                    $process.ProcessName
                                }
                                Else {
                                    ''
                                }
                            }
                            Write-Output -InputObject ($ProcessDetails)
                        }
                        Else {
                            Write-Log -Message 'PassThru parameter specified, however the process has already exited.' -Source ${CmdletName}
                        }
                    }
                }
                Else {
                    If ($processStartInfo.UseShellExecute -eq $false) {
                        $process.BeginOutputReadLine()
                        $process.BeginErrorReadLine()
                    }
                    ## Instructs the Process component to wait indefinitely for the associated process to exit.
                    $process.WaitForExit()

                    ## HasExited indicates that the associated process has terminated, either normally or abnormally. Wait until HasExited returns $true.
                    While (-not $process.HasExited) {
                        $process.Refresh(); Start-Sleep -Seconds 1
                    }

                    ## Get the exit code for the process
                    Try {
                        [Int32]$returnCode = $process.ExitCode
                    }
                    Catch [System.Management.Automation.PSInvalidCastException] {
                        #  Catch exit codes that are out of int32 range
                        [Int32]$returnCode = 60013
                    }

                    If ($processStartInfo.UseShellExecute -eq $false) {
                        ## Unregister standard output and error event to retrieve process output
                        If ($stdOutEvent) {
                            Unregister-Event -SourceIdentifier $stdOutEvent.Name -ErrorAction 'Stop'; $stdOutEvent = $null
                        }
                        If ($stdErrEvent) {
                            Unregister-Event -SourceIdentifier $stdErrEvent.Name -ErrorAction 'Stop'; $stdErrEvent = $null
                        }
                        $stdOut = $stdOutBuilder.ToString() -replace $null, ''
                        $stdErr = $stdErrBuilder.ToString() -replace $null, ''

                        If ($stdErr.Length -gt 0) {
                            Write-Log -Message "Standard error output from the process: $stdErr" -Severity 3 -Source ${CmdletName}
                        }
                    }
                }
            }
            Finally {
                If ($processStartInfo.UseShellExecute -eq $false) {
                    ## Make sure the standard output and error event is unregistered
                    If ($stdOutEvent) {
                        Unregister-Event -SourceIdentifier $stdOutEvent.Name -ErrorAction 'SilentlyContinue'; $stdOutEvent = $null
                    }
                    If ($stdErrEvent) {
                        Unregister-Event -SourceIdentifier $stdErrEvent.Name -ErrorAction 'SilentlyContinue'; $stdErrEvent = $null
                    }
                }
                ## Free resources associated with the process, this does not cause process to exit
                If ($process) {
                    $process.Dispose()
                }

                ## Re-enable Zone checking
                Remove-Item -LiteralPath 'env:SEE_MASK_NOZONECHECKS' -ErrorAction 'SilentlyContinue'

                If ($private:previousErrorActionPreference) {
                    $ErrorActionPreference = $private:previousErrorActionPreference
                }
            }

            If (-not $NoWait) {
                ## Check to see whether we should ignore exit codes
                $ignoreExitCodeMatch = $false
                If ($ignoreExitCodes) {
                    ## Check whether * was specified, which would tell us to ignore all exit codes
                    If ($ignoreExitCodes.Trim() -eq '*') {
                        $ignoreExitCodeMatch = $true
                    }
                    Else {
                        ## Split the processes on a comma
                        [Int32[]]$ignoreExitCodesArray = $ignoreExitCodes -split ','
                        ForEach ($ignoreCode in $ignoreExitCodesArray) {
                            If ($returnCode -eq $ignoreCode) {
                                $ignoreExitCodeMatch = $true
                            }
                        }
                    }
                }

                ## If the passthru switch is specified, return the exit code and any output from process
                If ($PassThru) {
                    Write-Log -Message 'PassThru parameter specified, returning execution results object.' -Source ${CmdletName}
                    [PSObject]$ExecutionResults = New-Object -TypeName 'PSObject' -Property @{ ExitCode = $returnCode; StdOut = If ($stdOut) {
                            $stdOut
                        }
                        Else {
                            ''
                        }; StdErr = If ($stdErr) {
                            $stdErr
                        }
                        Else {
                            ''
                        }
                    }
                    Write-Output -InputObject ($ExecutionResults)
                }

                If ($ignoreExitCodeMatch) {
                    Write-Log -Message "Execution completed and the exit code [$returncode] is being ignored." -Source ${CmdletName}
                }
                ElseIf (($returnCode -eq 3010) -or ($returnCode -eq 1641)) {
                    Write-Log -Message "Execution completed successfully with exit code [$returnCode]. A reboot is required." -Severity 2 -Source ${CmdletName}
                    Set-Variable -Name 'msiRebootDetected' -Value $true -Scope 'Script'
                }
                ElseIf (($returnCode -eq 1605) -and ($Path -match 'msiexec')) {
                    Write-Log -Message "Execution failed with exit code [$returnCode] because the product is not currently installed." -Severity 3 -Source ${CmdletName}
                }
                ElseIf (($returnCode -eq -2145124329) -and ($Path -match 'wusa')) {
                    Write-Log -Message "Execution failed with exit code [$returnCode] because the Windows Update is not applicable to this system." -Severity 3 -Source ${CmdletName}
                }
                ElseIf (($returnCode -eq 17025) -and ($Path -match 'fullfile')) {
                    Write-Log -Message "Execution failed with exit code [$returnCode] because the Office Update is not applicable to this system." -Severity 3 -Source ${CmdletName}
                }
                ElseIf ($returnCode -eq 0) {
                    Write-Log -Message "Execution completed successfully with exit code [$returnCode]." -Source ${CmdletName}
                }
                Else {
                    [String]$MsiExitCodeMessage = ''
                    If ($Path -match 'msiexec') {
                        [String]$MsiExitCodeMessage = Get-MsiExitCodeMessage -MsiExitCode $returnCode
                    }

                    If ($MsiExitCodeMessage) {
                        Write-Log -Message "Execution failed with exit code [$returnCode]: $MsiExitCodeMessage" -Severity 3 -Source ${CmdletName}
                    }
                    Else {
                        Write-Log -Message "Execution failed with exit code [$returnCode]." -Severity 3 -Source ${CmdletName}
                    }

                    If ($ExitOnProcessFailure) {
                        Exit-Script -ExitCode $returnCode
                    }
                }
            }
        }
        Catch {
            If ([String]::IsNullOrEmpty([String]$returnCode)) {
                [Int32]$returnCode = 60002
                Write-Log -Message "Function failed, setting exit code to [$returnCode]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Function failed, setting exit code to [$returnCode]. $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Execution completed with exit code [$returnCode]. Function failed. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }

            If ($PassThru) {
                [PSObject]$ExecutionResults = New-Object -TypeName 'PSObject' -Property @{ ExitCode = $returnCode; StdOut = If ($stdOut) {
                        $stdOut
                    }
                    Else {
                        ''
                    }; StdErr = If ($stdErr) {
                        $stdErr
                    }
                    Else {
                        ''
                    }
                }
                Write-Output -InputObject ($ExecutionResults)
            }

            If ($ExitOnProcessFailure) {
                Exit-Script -ExitCode $returnCode
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-MsiExitCodeMessage
Function Get-MsiExitCodeMessage {
    <#
.SYNOPSIS

	Get message for MSI error code

.DESCRIPTION

	Get message for MSI error code by reading it from msimsg.dll

.PARAMETER MsiErrorCode

	MSI error code

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the message for the MSI error code.

.EXAMPLE

	Get-MsiExitCodeMessage -MsiErrorCode 1618

.NOTES

	This is an internal script function and should typically not be called directly.

.LINK

	http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx

.LINK

	https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [Int32]$MsiExitCode
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Getting message for exit code [$MsiExitCode]." -Source ${CmdletName}
            [String]$MsiExitCodeMsg = [PSADT.Msi]::GetMessageFromMsiExitCode($MsiExitCode)
            Write-Output -InputObject ($MsiExitCodeMsg)
        }
        Catch {
            Write-Log -Message "Failed to get message for exit code [$MsiExitCode]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Test-IsMutexAvailable
Function Test-IsMutexAvailable {
    <#
.SYNOPSIS

Wait, up to a timeout value, to check if current thread is able to acquire an exclusive lock on a system mutex.

.DESCRIPTION

A mutex can be used to serialize applications and prevent multiple instances from being opened at the same time.
Wait, up to a timeout (default is 1 millisecond), for the mutex to become available for an exclusive lock.

.PARAMETER MutexName

The name of the system mutex.

.PARAMETER MutexWaitTime

The number of milliseconds the current thread should wait to acquire an exclusive lock of a named mutex. Default is: 1 millisecond.
A wait timeof -1 milliseconds means to wait indefinitely. A wait time of zero does not acquire an exclusive lock but instead tests the state of the wait handle and returns immediately.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if the current thread acquires an exclusive lock on the named mutex, $false otherwise.

.EXAMPLE

Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds 500

.EXAMPLE

Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds (New-TimeSpan -Minutes 5).TotalMilliseconds

.EXAMPLE

Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds (New-TimeSpan -Seconds 60).TotalMilliseconds

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

	http://msdn.microsoft.com/en-us/library/aa372909(VS.85).asp

.LINK

	https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateLength(1, 260)]
        [String]$MutexName,
        [Parameter(Mandatory = $false)]
        [ValidateScript({ ($_ -ge -1) -and ($_ -le [Int32]::MaxValue) })]
        [Int32]$MutexWaitTimeInMilliseconds = 1
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Initialize Variables
        [Timespan]$MutexWaitTime = [Timespan]::FromMilliseconds($MutexWaitTimeInMilliseconds)
        If ($MutexWaitTime.TotalMinutes -ge 1) {
            [String]$WaitLogMsg = "$($MutexWaitTime.TotalMinutes) minute(s)"
        }
        ElseIf ($MutexWaitTime.TotalSeconds -ge 1) {
            [String]$WaitLogMsg = "$($MutexWaitTime.TotalSeconds) second(s)"
        }
        Else {
            [String]$WaitLogMsg = "$($MutexWaitTime.Milliseconds) millisecond(s)"
        }
        [Boolean]$IsUnhandledException = $false
        [Boolean]$IsMutexFree = $false
        [Threading.Mutex]$OpenExistingMutex = $null
    }
    Process {
        Write-Log -Message "Checking to see if mutex [$MutexName] is available. Wait up to [$WaitLogMsg] for the mutex to become available." -Source ${CmdletName}
        Try {
            ## Using this variable allows capture of exceptions from .NET methods. Private scope only changes value for current function.
            $private:previousErrorActionPreference = $ErrorActionPreference
            $ErrorActionPreference = 'Stop'

            ## Open the specified named mutex, if it already exists, without acquiring an exclusive lock on it. If the system mutex does not exist, this method throws an exception instead of creating the system object.
            [Threading.Mutex]$OpenExistingMutex = [Threading.Mutex]::OpenExisting($MutexName)
            ## Attempt to acquire an exclusive lock on the mutex. Use a Timespan to specify a timeout value after which no further attempt is made to acquire a lock on the mutex.
            $IsMutexFree = $OpenExistingMutex.WaitOne($MutexWaitTime, $false)
        }
        Catch [Threading.WaitHandleCannotBeOpenedException] {
            ## The named mutex does not exist
            $IsMutexFree = $true
        }
        Catch [ObjectDisposedException] {
            ## Mutex was disposed between opening it and attempting to wait on it
            $IsMutexFree = $true
        }
        Catch [UnauthorizedAccessException] {
            ## The named mutex exists, but the user does not have the security access required to use it
            $IsMutexFree = $false
        }
        Catch [Threading.AbandonedMutexException] {
            ## The wait completed because a thread exited without releasing a mutex. This exception is thrown when one thread acquires a mutex object that another thread has abandoned by exiting without releasing it.
            $IsMutexFree = $true
        }
        Catch {
            $IsUnhandledException = $true
            ## Return $true, to signify that mutex is available, because function was unable to successfully complete a check due to an unhandled exception. Default is to err on the side of the mutex being available on a hard failure.
            Write-Log -Message "Unable to check if mutex [$MutexName] is available due to an unhandled exception. Will default to return value of [$true]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            $IsMutexFree = $true
        }
        Finally {
            If ($IsMutexFree) {
                If (-not $IsUnhandledException) {
                    Write-Log -Message "Mutex [$MutexName] is available for an exclusive lock." -Source ${CmdletName}
                }
            }
            Else {
                If ($MutexName -eq 'Global\_MSIExecute') {
                    ## Get the command line for the MSI installation in progress
                    Try {
                        [String]$msiInProgressCmdLine = Get-WmiObject -Class 'Win32_Process' -Filter "name = 'msiexec.exe'" -ErrorAction 'Stop' | Where-Object { $_.CommandLine } | Select-Object -ExpandProperty 'CommandLine' | Where-Object { $_ -match '\.msi' } | ForEach-Object { $_.Trim() }
                    }
                    Catch {
                    }
                    Write-Log -Message "Mutex [$MutexName] is not available for an exclusive lock because the following MSI installation is in progress [$msiInProgressCmdLine]." -Severity 2 -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message "Mutex [$MutexName] is not available because another thread already has an exclusive lock on it." -Source ${CmdletName}
                }
            }

            If (($null -ne $OpenExistingMutex) -and ($IsMutexFree)) {
                ## Release exclusive lock on the mutex
                $null = $OpenExistingMutex.ReleaseMutex()
                $OpenExistingMutex.Close()
            }
            If ($private:previousErrorActionPreference) {
                $ErrorActionPreference = $private:previousErrorActionPreference
            }
        }
    }
    End {
        Write-Output -InputObject ($IsMutexFree)

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function New-Folder
Function New-Folder {
    <#
.SYNOPSIS

Create a new folder.

.DESCRIPTION

Create a new folder if it does not exist.

.PARAMETER Path

Path to the new folder to create.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

New-Folder -Path "$envWinDir\System32"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            If (-not (Test-Path -LiteralPath $Path -PathType 'Container')) {
                Write-Log -Message "Creating folder [$Path]." -Source ${CmdletName}
                $null = New-Item -Path $Path -ItemType 'Directory' -ErrorAction 'Stop' -Force
            }
            Else {
                Write-Log -Message "Folder [$Path] already exists." -Source ${CmdletName}
            }
        }
        Catch {
            Write-Log -Message "Failed to create folder [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to create folder [$Path]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Remove-Folder
Function Remove-Folder {
    <#
.SYNOPSIS

Remove folder and files if they exist.

.DESCRIPTION

Remove folder and all files with or without recursion in a given path.

.PARAMETER Path

Path to the folder to remove.

.PARAMETER DisableRecursion

Disables recursion while deleting.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-Folder -Path "$envWinDir\Downloaded Program Files"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [Switch]$DisableRecursion,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If (Test-Path -LiteralPath $Path -PathType 'Container' -ErrorAction 'SilentlyContinue') {
            Try {
                If ($DisableRecursion) {
                    Write-Log -Message "Deleting folder [$path] without recursion..." -Source ${CmdletName}
                    # Without recursion we have to go through the subfolder ourselves because Remove-Item asks for confirmation if we are trying to delete a non-empty folder without -Recurse
                    [Array]$ListOfChildItems = Get-ChildItem -LiteralPath $Path -Force
                    If ($ListOfChildItems) {
                        $SubfoldersSkipped = 0
                        ForEach ($item in $ListOfChildItems) {
                            # Check whether this item is a folder
                            If (Test-Path -LiteralPath $item.FullName -PathType Container) {
                                # Item is a folder. Check if its empty
                                # Get list of child items in the folder
                                [Array]$ItemChildItems = Get-ChildItem -LiteralPath $item.FullName -Force -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
                                If ($ItemChildItems.Count -eq 0) {
                                    # The folder is empty, delete it
                                    Remove-Item -LiteralPath $item.FullName -Force -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
                                }
                                Else {
                                    # Folder is not empty, skip it
                                    $SubfoldersSkipped++
                                    Continue
                                }
                            }
                            Else {
                                # Item is a file. Delete it
                                Remove-Item -LiteralPath $item.FullName -Force -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
                            }
                        }
                        If ($SubfoldersSkipped -gt 0) {
                            Throw "[$SubfoldersSkipped] subfolders are not empty!"
                        }
                    }
                    Else {
                        Remove-Item -LiteralPath $Path -Force -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
                    }
                }
                Else {
                    Write-Log -Message "Deleting folder [$path] recursively..." -Source ${CmdletName}
                    Remove-Item -LiteralPath $Path -Force -Recurse -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
                }

                If ($ErrorRemoveFolder) {
                    Throw $ErrorRemoveFolder
                }
            }
            Catch {
                Write-Log -Message "Failed to delete folder(s) and file(s) from path [$path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to delete folder(s) and file(s) from path [$path]: $($_.Exception.Message)"
                }
            }
        }
        Else {
            Write-Log -Message "Folder [$Path] does not exist." -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Copy-File
Function Copy-File {
    <#
.SYNOPSIS

Copy a file or group of files to a destination path.

.DESCRIPTION

Copy a file or group of files to a destination path.

.PARAMETER Path

Path of the file to copy.

.PARAMETER Destination

Destination Path of the file to copy.

.PARAMETER Recurse

Copy files in subdirectories.

.PARAMETER Flatten

Flattens the files into the root destination directory.

.PARAMETER ContinueOnError

Continue if an error is encountered. This will continue the deployment script, but will not continue copying files if an error is encountered. Default is: $true.

.PARAMETER ContinueFileCopyOnError

Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Copy-File -Path "$dirSupportFiles\MyApp.ini" -Destination "$envWinDir\MyApp.ini"

.EXAMPLE

Copy-File -Path "$dirSupportFiles\*.*" -Destination "$envTemp\tempfiles"

Copy all of the files in a folder to a destination folder.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String[]]$Path,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Destination,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$Flatten,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true,
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueFileCopyOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            If ((-not ([IO.Path]::HasExtension($Destination))) -and (-not (Test-Path -LiteralPath $Destination -PathType 'Container'))) {
                Write-Log -Message "Destination folder does not exist, creating destination folder [$destination]." -Source ${CmdletName}
                $null = New-Item -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop'
            }

            If ($Flatten) {
                If ($Recurse) {
                    Write-Log -Message "Copying file(s) recursively in path [$path] to destination [$destination] root folder, flattened." -Source ${CmdletName}
                    If ($ContinueFileCopyOnError) {
                        $null = Get-ChildItem -Path $path -Recurse -Force -ErrorAction 'SilentlyContinue' | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
                            Copy-Item -Path ($_.FullName) -Destination $destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
                        }
                    }
                    Else {
                        $null = Get-ChildItem -Path $path -Recurse -Force -ErrorAction 'SilentlyContinue' | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
                            Copy-Item -Path ($_.FullName) -Destination $destination -Force -ErrorAction 'Stop'
                        }
                    }
                }
                Else {
                    Write-Log -Message "Copying file in path [$path] to destination [$destination]." -Source ${CmdletName}
                    If ($ContinueFileCopyOnError) {
                        $null = Copy-Item -Path $path -Destination $destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
                    }
                    Else {
                        $null = Copy-Item -Path $path -Destination $destination -Force -ErrorAction 'Stop'
                    }
                }
            }
            Else {
                If ($Recurse) {
                    Write-Log -Message "Copying file(s) recursively in path [$path] to destination [$destination]." -Source ${CmdletName}
                    If ($ContinueFileCopyOnError) {
                        $null = Copy-Item -Path $Path -Destination $Destination -Force -Recurse -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
                    }
                    Else {
                        $null = Copy-Item -Path $Path -Destination $Destination -Force -Recurse -ErrorAction 'Stop'
                    }
                }
                Else {
                    Write-Log -Message "Copying file in path [$path] to destination [$destination]." -Source ${CmdletName}
                    If ($ContinueFileCopyOnError) {
                        $null = Copy-Item -Path $Path -Destination $Destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
                    }
                    Else {
                        $null = Copy-Item -Path $Path -Destination $Destination -Force -ErrorAction 'Stop'
                    }
                }
            }

            If ($FileCopyError) {
                Write-Log -Message "The following warnings were detected while copying file(s) in path [$path] to destination [$destination]. `r`n$FileCopyError" -Severity 2 -Source ${CmdletName}
            }
            Else {
                Write-Log -Message 'File copy completed successfully.' -Source ${CmdletName}
            }
        }
        Catch {
            Write-Log -Message "Failed to copy file(s) in path [$path] to destination [$destination]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to copy file(s) in path [$path] to destination [$destination]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Remove-File
Function Remove-File {
    <#
.SYNOPSIS

Removes one or more items from a given path on the filesystem.

.DESCRIPTION

Removes one or more items from a given path on the filesystem.

.PARAMETER Path

Specifies the path on the filesystem to be resolved. The value of Path will accept wildcards. Will accept an array of values.

.PARAMETER LiteralPath

Specifies the path on the filesystem to be resolved. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

.PARAMETER Recurse

Deletes the files in the specified location(s) and in all child items of the location(s).

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-File -Path 'C:\Windows\Downloaded Program Files\Temp.inf'

.EXAMPLE

Remove-File -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullorEmpty()]
        [String[]]$Path,
        [Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullorEmpty()]
        [String[]]$LiteralPath,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Build hashtable of parameters/value pairs to be passed to Remove-Item cmdlet
        [Hashtable]$RemoveFileSplat = @{ 'Recurse' = $Recurse
										  'Force'                                = $true
										  'ErrorVariable'                        = '+ErrorRemoveItem'
        }
        If ($ContinueOnError) {
            $RemoveFileSplat.Add('ErrorAction', 'SilentlyContinue')
        }
        Else {
            $RemoveFileSplat.Add('ErrorAction', 'Stop')
        }

        ## Resolve the specified path, if the path does not exist, display a warning instead of an error
        If ($PSCmdlet.ParameterSetName -eq 'Path') {
            [String[]]$SpecifiedPath = $Path
        }
        Else {
            [String[]]$SpecifiedPath = $LiteralPath
        }
        ForEach ($Item in $SpecifiedPath) {
            Try {
                If ($PSCmdlet.ParameterSetName -eq 'Path') {
                    [String[]]$ResolvedPath += Resolve-Path -Path $Item -ErrorAction 'Stop' | Where-Object { $_.Path } | Select-Object -ExpandProperty 'Path' -ErrorAction 'Stop'
                }
                Else {
                    [String[]]$ResolvedPath += Resolve-Path -LiteralPath $Item -ErrorAction 'Stop' | Where-Object { $_.Path } | Select-Object -ExpandProperty 'Path' -ErrorAction 'Stop'
                }
            }
            Catch [System.Management.Automation.ItemNotFoundException] {
                Write-Log -Message "Unable to resolve file(s) for deletion in path [$Item] because path does not exist." -Severity 2 -Source ${CmdletName}
            }
            Catch {
                Write-Log -Message "Failed to resolve file(s) for deletion in path [$Item]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to resolve file(s) for deletion in path [$Item]: $($_.Exception.Message)"
                }
            }
        }

        ## Delete specified path if it was successfully resolved
        If ($ResolvedPath) {
            ForEach ($Item in $ResolvedPath) {
                Try {
                    If (($Recurse) -and (Test-Path -LiteralPath $Item -PathType 'Container')) {
                        Write-Log -Message "Deleting file(s) recursively in path [$Item]..." -Source ${CmdletName}
                    }
                    ElseIf ((-not $Recurse) -and (Test-Path -LiteralPath $Item -PathType 'Container')) {
                        Write-Log -Message "Skipping folder [$Item] because the Recurse switch was not specified." -Source ${CmdletName}
                        Continue
                    }
                    Else {
                        Write-Log -Message "Deleting file in path [$Item]..." -Source ${CmdletName}
                    }
                    $null = Remove-Item @RemoveFileSplat -LiteralPath $Item
                }
                Catch {
                    Write-Log -Message "Failed to delete file(s) in path [$Item]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                    If (-not $ContinueOnError) {
                        Throw "Failed to delete file(s) in path [$Item]: $($_.Exception.Message)"
                    }
                }
            }
        }

        If ($ErrorRemoveItem) {
            Write-Log -Message "The following error(s) took place while removing file(s) in path [$SpecifiedPath]. `r`n$(Resolve-Error -ErrorRecord $ErrorRemoveItem)" -Severity 2 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Convert-RegistryPath
Function Convert-RegistryPath {
    <#
.SYNOPSIS

Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

.DESCRIPTION

Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

Converts registry key hives to their full paths. Example: HKLM is converted to "Registry::HKEY_LOCAL_MACHINE".

.PARAMETER Key

Path to the registry key to convert (can be a registry hive or fully qualified path)

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER DisableFunctionLogging

Disables logging of this function. Default: $true

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the converted registry key path.

.EXAMPLE

Convert-RegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

.EXAMPLE

Convert-RegistryPath -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$DisableFunctionLogging = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Convert the registry key hive to the full path, only match if at the beginning of the line
        If ($Key -match '^HKLM') {
            $Key = $Key -replace '^HKLM:\\', 'HKEY_LOCAL_MACHINE\' -replace '^HKLM:', 'HKEY_LOCAL_MACHINE\' -replace '^HKLM\\', 'HKEY_LOCAL_MACHINE\'
        }
        ElseIf ($Key -match '^HKCR') {
            $Key = $Key -replace '^HKCR:\\', 'HKEY_CLASSES_ROOT\' -replace '^HKCR:', 'HKEY_CLASSES_ROOT\' -replace '^HKCR\\', 'HKEY_CLASSES_ROOT\'
        }
        ElseIf ($Key -match '^HKCU') {
            $Key = $Key -replace '^HKCU:\\', 'HKEY_CURRENT_USER\' -replace '^HKCU:', 'HKEY_CURRENT_USER\' -replace '^HKCU\\', 'HKEY_CURRENT_USER\'
        }
        ElseIf ($Key -match '^HKU') {
            $Key = $Key -replace '^HKU:\\', 'HKEY_USERS\' -replace '^HKU:', 'HKEY_USERS\' -replace '^HKU\\', 'HKEY_USERS\'
        }
        ElseIf ($Key -match '^HKCC') {
            $Key = $Key -replace '^HKCC:\\', 'HKEY_CURRENT_CONFIG\' -replace '^HKCC:', 'HKEY_CURRENT_CONFIG\' -replace '^HKCC\\', 'HKEY_CURRENT_CONFIG\'
        }
        ElseIf ($Key -match '^HKPD') {
            $Key = $Key -replace '^HKPD:\\', 'HKEY_PERFORMANCE_DATA\' -replace '^HKPD:', 'HKEY_PERFORMANCE_DATA\' -replace '^HKPD\\', 'HKEY_PERFORMANCE_DATA\'
        }

        ## Append the PowerShell provider to the registry key path
        If ($key -notmatch '^Registry::') {
            [String]$key = "Registry::$key"
        }

        If ($PSBoundParameters.ContainsKey('SID')) {
            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($key -match '^Registry::HKEY_CURRENT_USER\\') {
                $key = $key -replace '^Registry::HKEY_CURRENT_USER\\', "Registry::HKEY_USERS\$SID\"
            }
            ElseIf (-not $DisableFunctionLogging) {
                Write-Log -Message 'SID parameter specified but the registry hive of the key is not HKEY_CURRENT_USER.' -Source ${CmdletName} -Severity 2
            }
        }

        If ($Key -match '^Registry::HKEY_LOCAL_MACHINE|^Registry::HKEY_CLASSES_ROOT|^Registry::HKEY_CURRENT_USER|^Registry::HKEY_USERS|^Registry::HKEY_CURRENT_CONFIG|^Registry::HKEY_PERFORMANCE_DATA') {
            ## Check for expected key string format
            If (-not $DisableFunctionLogging) {
                Write-Log -Message "Return fully qualified registry key path [$key]." -Source ${CmdletName}
            }
            Write-Output -InputObject ($key)
        }
        Else {
            #  If key string is not properly formatted, throw an error
            Throw "Unable to detect target registry hive in string [$key]."
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Test-RegistryValue
Function Test-RegistryValue {
    <#
.SYNOPSIS

Test if a registry value exists.

.DESCRIPTION

Checks a registry key path to see if it has a value with a given name. Can correctly handle cases where a value simply has an empty or null value.

.PARAMETER Key

Path of the registry key.

.PARAMETER Value

Specify the registry key value to check the existence of.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.INPUTS

System.String

Accepts a string value for the registry key path.

.OUTPUTS

System.String

Returns $true if the registry value exists, $false if it does not.

.EXAMPLE

Test-RegistryValue -Key 'HKLM:SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations'

.NOTES

To test if registry key exists, use Test-Path function like so:

Test-Path -Path $Key -PathType 'Container'

.LINK

https://psappdeploytoolkit.com
#>
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]$Key,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]$Value,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullorEmpty()]
        [String]$SID
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
        Try {
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$Key = Convert-RegistryPath -Key $Key -SID $SID
            }
            Else {
                [String]$Key = Convert-RegistryPath -Key $Key
            }
        }
        Catch {
            Throw
        }
        [Boolean]$IsRegistryValueExists = $false
        Try {
            If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
                [String[]]$PathProperties = Get-Item -LiteralPath $Key -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Property' -ErrorAction 'Stop'
                If ($PathProperties -contains $Value) {
                    $IsRegistryValueExists = $true
                }
            }
        }
        Catch {
        }

        If ($IsRegistryValueExists) {
            Write-Log -Message "Registry key value [$Key] [$Value] does exist." -Source ${CmdletName}
        }
        Else {
            Write-Log -Message "Registry key value [$Key] [$Value] does not exist." -Source ${CmdletName}
        }
        Write-Output -InputObject ($IsRegistryValueExists)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-RegistryKey
Function Get-RegistryKey {
    <#
.SYNOPSIS

Retrieves value names and value data for a specified registry key or optionally, a specific value.

.DESCRIPTION

Retrieves value names and value data for a specified registry key or optionally, a specific value.

If the registry key does not exist or contain any values, the function will return $null by default. To test for existence of a registry key path, use built-in Test-Path cmdlet.

.PARAMETER Key

Path of the registry key.

.PARAMETER Value

Value to retrieve (optional).

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER ReturnEmptyKeyIfExists

Return the registry key if it exists but it has no property/value pairs underneath it. Default is: $false.

.PARAMETER DoNotExpandEnvironmentNames

Return unexpanded REG_EXPAND_SZ values. Default is: $false.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the value of the registry key or value.

.EXAMPLE

Get-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

.EXAMPLE

Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\iexplore.exe'

.EXAMPLE

Get-RegistryKey -Key 'HKLM:Software\Wow6432Node\Microsoft\Microsoft SQL Server Compact Edition\v3.5' -Value 'Version'

.EXAMPLE

Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Value 'Path' -DoNotExpandEnvironmentNames

Returns %ProgramFiles%\Java instead of C:\Program Files\Java

.EXAMPLE

Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Value '(Default)'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Value,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$ReturnEmptyKeyIfExists = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$DoNotExpandEnvironmentNames = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$key = Convert-RegistryPath -Key $key -SID $SID
            }
            Else {
                [String]$key = Convert-RegistryPath -Key $key
            }

            ## Check if the registry key exists
            If (-not (Test-Path -LiteralPath $key -ErrorAction 'Stop')) {
                Write-Log -Message "Registry key [$key] does not exist. Return `$null." -Severity 2 -Source ${CmdletName}
                $regKeyValue = $null
            }
            Else {
                If ($PSBoundParameters.ContainsKey('Value')) {
                    Write-Log -Message "Getting registry key [$key] value [$value]." -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message "Getting registry key [$key] and all property values." -Source ${CmdletName}
                }

                ## Get all property values for registry key
                $regKeyValue = Get-ItemProperty -LiteralPath $key -ErrorAction 'Stop'
                [Int32]$regKeyValuePropertyCount = $regKeyValue | Measure-Object | Select-Object -ExpandProperty 'Count'

                ## Select requested property
                If ($PSBoundParameters.ContainsKey('Value')) {
                    #  Check if registry value exists
                    [Boolean]$IsRegistryValueExists = $false
                    If ($regKeyValuePropertyCount -gt 0) {
                        Try {
                            [string[]]$PathProperties = Get-Item -LiteralPath $Key -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Property' -ErrorAction 'Stop'
                            If ($PathProperties -contains $Value) {
                                $IsRegistryValueExists = $true
                            }
                        }
                        Catch {
                        }
                    }

                    #  Get the Value (do not make a strongly typed variable because it depends entirely on what kind of value is being read)
                    If ($IsRegistryValueExists) {
                        If ($DoNotExpandEnvironmentNames) {
                            #Only useful on 'ExpandString' values
                            If ($Value -like '(Default)') {
                                $regKeyValue = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').GetValue($null, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                            }
                            Else {
                                $regKeyValue = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').GetValue($Value, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                            }
                        }
                        ElseIf ($Value -like '(Default)') {
                            $regKeyValue = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').GetValue($null)
                        }
                        Else {
                            $regKeyValue = $regKeyValue | Select-Object -ExpandProperty $Value -ErrorAction 'SilentlyContinue'
                        }
                    }
                    Else {
                        Write-Log -Message "Registry key value [$Key] [$Value] does not exist. Return `$null." -Source ${CmdletName}
                        $regKeyValue = $null
                    }
                }
                ## Select all properties or return empty key object
                Else {
                    If ($regKeyValuePropertyCount -eq 0) {
                        If ($ReturnEmptyKeyIfExists) {
                            Write-Log -Message "No property values found for registry key. Return empty registry key object [$key]." -Source ${CmdletName}
                            $regKeyValue = Get-Item -LiteralPath $key -Force -ErrorAction 'Stop'
                        }
                        Else {
                            Write-Log -Message "No property values found for registry key. Return `$null." -Source ${CmdletName}
                            $regKeyValue = $null
                        }
                    }
                }
            }
            Write-Output -InputObject ($regKeyValue)
        }
        Catch {
            If (-not $Value) {
                Write-Log -Message "Failed to read registry key [$key]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to read registry key [$key]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Failed to read registry key [$key] value [$value]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to read registry key [$key] value [$value]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Set-RegistryKey
Function Set-RegistryKey {
    <#
.SYNOPSIS

Creates a registry key name, value, and value data; it sets the same if it already exists.

.DESCRIPTION

Creates a registry key name, value, and value data; it sets the same if it already exists.

.PARAMETER Key

The registry key path.

.PARAMETER Name

The value name.

.PARAMETER Value

The value data.

.PARAMETER Type

The type of registry value to create or set. Options: 'Binary','DWord','ExpandString','MultiString','None','QWord','String','Unknown'. Default: String.

DWord should be specified as a decimal.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-RegistryKey -Key $blockedAppPath -Name 'Debugger' -Value $blockedAppDebuggerValue

.EXAMPLE

Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' -Name 'Application' -Type 'DWord' -Value '1'

.EXAMPLE

Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Name 'Debugger' -Value $blockedAppDebuggerValue -Type String

.EXAMPLE

Set-RegistryKey -Key 'HKCU\Software\Microsoft\Example' -Name 'Data' -Value (0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x00,0x01,0x01,0x01,0x02,0x02,0x02) -Type 'Binary'

.EXAMPLE

Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)' -Value "Text"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        $Value,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Binary', 'DWord', 'ExpandString', 'MultiString', 'None', 'QWord', 'String', 'Unknown')]
        [Microsoft.Win32.RegistryValueKind]$Type = 'String',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            [String]$RegistryValueWriteAction = 'set'

            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$key = Convert-RegistryPath -Key $key -SID $SID
            }
            Else {
                [String]$key = Convert-RegistryPath -Key $key
            }

            ## Create registry key if it doesn't exist
            If (-not (Test-Path -LiteralPath $key -ErrorAction 'Stop')) {
                Try {
                    Write-Log -Message "Creating registry key [$key]." -Source ${CmdletName}
                    # No forward slash found in Key. Use New-Item cmdlet to create registry key
                    If ((($Key -split '/').Count - 1) -eq 0) {
                        $null = New-Item -Path $key -ItemType 'Registry' -Force -ErrorAction 'Stop'
                    }
                    # Forward slash was found in Key. Use REG.exe ADD to create registry key
                    Else {
                        [String]$CreateRegkeyResult = & "$envWinDir\System32\reg.exe" Add "$($Key.Substring($Key.IndexOf('::') + 2))"
                        If ($global:LastExitCode -ne 0) {
                            Throw "Failed to create registry key [$Key]"
                        }
                    }
                }
                Catch {
                    Throw
                }
            }

            If ($Name) {
                ## Set registry value if it doesn't exist
                If (-not (Get-ItemProperty -LiteralPath $key -Name $Name -ErrorAction 'SilentlyContinue')) {
                    Write-Log -Message "Setting registry key value: [$key] [$name = $value]." -Source ${CmdletName}
                    $null = New-ItemProperty -LiteralPath $key -Name $name -Value $value -PropertyType $Type -ErrorAction 'Stop'
                }
                ## Update registry value if it does exist
                Else {
                    [String]$RegistryValueWriteAction = 'update'
                    If ($Name -eq '(Default)') {
                        ## Set Default registry key value with the following workaround, because Set-ItemProperty contains a bug and cannot set Default registry key value
                        $null = $(Get-Item -LiteralPath $key -ErrorAction 'Stop').OpenSubKey('', 'ReadWriteSubTree').SetValue($null, $value)
                    }
                    Else {
                        Write-Log -Message "Updating registry key value: [$key] [$name = $value]." -Source ${CmdletName}
                        $null = Set-ItemProperty -LiteralPath $key -Name $name -Value $value -ErrorAction 'Stop'
                    }
                }
            }
        }
        Catch {
            If ($Name) {
                Write-Log -Message "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Failed to set registry key [$key]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to set registry key [$key]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Remove-RegistryKey
Function Remove-RegistryKey {
    <#
.SYNOPSIS

Deletes the specified registry key or value.

.DESCRIPTION

Deletes the specified registry key or value.

.PARAMETER Key

Path of the registry key to delete.

.PARAMETER Name

Name of the registry value to delete.

.PARAMETER Recurse

Delete registry key recursively.

.PARAMETER SID

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-RegistryKey -Key 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce'

.EXAMPLE

Remove-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'RunAppInstall'

.EXAMPLE

Remove-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Name '(Default)'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
            If ($PSBoundParameters.ContainsKey('SID')) {
                [String]$Key = Convert-RegistryPath -Key $Key -SID $SID
            }
            Else {
                [String]$Key = Convert-RegistryPath -Key $Key
            }

            If (-not $Name) {
                If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
                    If ($Recurse) {
                        Write-Log -Message "Deleting registry key recursively [$Key]." -Source ${CmdletName}
                        $null = Remove-Item -LiteralPath $Key -Force -Recurse -ErrorAction 'Stop'
                    }
                    Else {
                        If ($null -eq (Get-ChildItem -LiteralPath $Key -ErrorAction 'Stop')) {
                            ## Check if there are subkeys of $Key, if so, executing Remove-Item will hang. Avoiding this with Get-ChildItem.
                            Write-Log -Message "Deleting registry key [$Key]." -Source ${CmdletName}
                            $null = Remove-Item -LiteralPath $Key -Force -ErrorAction 'Stop'
                        }
                        Else {
                            Throw "Unable to delete child key(s) of [$Key] without [-Recurse] switch."
                        }
                    }
                }
                Else {
                    Write-Log -Message "Unable to delete registry key [$Key] because it does not exist." -Severity 2 -Source ${CmdletName}
                }
            }
            Else {
                If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
                    Write-Log -Message "Deleting registry value [$Key] [$Name]." -Source ${CmdletName}

                    If ($Name -eq '(Default)') {
                        ## Remove (Default) registry key value with the following workaround because Remove-ItemProperty cannot remove the (Default) registry key value
                        $null = (Get-Item -LiteralPath $Key -ErrorAction 'Stop').OpenSubKey('', 'ReadWriteSubTree').DeleteValue('')
                    }
                    Else {
                        $null = Remove-ItemProperty -LiteralPath $Key -Name $Name -Force -ErrorAction 'Stop'
                    }
                }
                Else {
                    Write-Log -Message "Unable to delete registry value [$Key] [$Name] because registry key does not exist." -Severity 2 -Source ${CmdletName}
                }
            }
        }
        Catch [System.Management.Automation.PSArgumentException] {
            Write-Log -Message "Unable to delete registry value [$Key] [$Name] because it does not exist." -Severity 2 -Source ${CmdletName}
        }
        Catch {
            If (-not $Name) {
                Write-Log -Message "Failed to delete registry key [$Key]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to delete registry key [$Key]: $($_.Exception.Message)"
                }
            }
            Else {
                Write-Log -Message "Failed to delete registry value [$Key] [$Name]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to delete registry value [$Key] [$Name]: $($_.Exception.Message)"
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Invoke-HKCURegistrySettingsForAllUsers
Function Invoke-HKCURegistrySettingsForAllUsers {
    <#
.SYNOPSIS

Set current user registry settings for all current users and any new users in the future.

.DESCRIPTION

Set HKCU registry settings for all current and future users by loading their NTUSER.dat registry hive file, and making the modifications.

This function will modify HKCU settings for all users even when executed under the SYSTEM account.

To ensure new users in the future get the registry edits, the Default User registry hive used to provision the registry for new users is modified.

This function can be used as an alternative to using ActiveSetup for registry settings.

The advantage of using this function over ActiveSetup is that a user does not have to log off and log back on before the changes take effect.

.PARAMETER RegistrySettings

Script block which contains HKCU registry settings which should be modified for all users on the system. Must specify the -SID parameter for all HKCU settings.

.PARAMETER UserProfiles

Specify the user profiles to modify HKCU registry settings for. Default is all user profiles except for system profiles.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

[ScriptBlock]$HKCURegistrySettings = {

Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $UserProfile.SID

Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $UserProfile.SID

}

Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $HKCURegistrySettings

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [ScriptBlock]$RegistrySettings,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [PSObject[]]$UserProfiles = (Get-UserProfiles)
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ForEach ($UserProfile in $UserProfiles) {
            Try {
                #  Set the path to the user's registry hive when it is loaded
                [String]$UserRegistryPath = "Registry::HKEY_USERS\$($UserProfile.SID)"

                #  Set the path to the user's registry hive file
                [String]$UserRegistryHiveFile = Join-Path -Path $UserProfile.ProfilePath -ChildPath 'NTUSER.DAT'

                #  Load the User profile registry hive if it is not already loaded because the User is logged in
                [Boolean]$ManuallyLoadedRegHive = $false
                If (-not (Test-Path -LiteralPath $UserRegistryPath)) {
                    #  Load the User registry hive if the registry hive file exists
                    If (Test-Path -LiteralPath $UserRegistryHiveFile -PathType 'Leaf') {
                        Write-Log -Message "Loading the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                        [String]$HiveLoadResult = & "$envWinDir\System32\reg.exe" load "`"HKEY_USERS\$($UserProfile.SID)`"" "`"$UserRegistryHiveFile`""

                        If ($global:LastExitCode -ne 0) {
                            Throw "Failed to load the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Failure message [$HiveLoadResult]. Continue..."
                        }

                        [Boolean]$ManuallyLoadedRegHive = $true
                    }
                    Else {
                        Throw "Failed to find the registry hive file [$UserRegistryHiveFile] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Continue..."
                    }
                }
                Else {
                    Write-Log -Message "The user [$($UserProfile.NTAccount)] registry hive is already loaded in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                }

                ## Execute ScriptBlock which contains code to manipulate HKCU registry.
                #  Make sure read/write calls to the HKCU registry hive specify the -SID parameter or settings will not be changed for all users.
                #  Example: Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $UserProfile.SID
                Write-Log -Message 'Executing ScriptBlock to modify HKCU registry settings for all users.' -Source ${CmdletName}
                & $RegistrySettings
            }
            Catch {
                Write-Log -Message "Failed to modify the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)] `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
            Finally {
                If ($ManuallyLoadedRegHive) {
                    Try {
                        Write-Log -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                        [String]$HiveLoadResult = & "$envWinDir\System32\reg.exe" unload "`"HKEY_USERS\$($UserProfile.SID)`""

                        If ($global:LastExitCode -ne 0) {
                            Write-Log -Message "REG.exe failed to unload the registry hive and exited with exit code [$($global:LastExitCode)]. Performing manual garbage collection to ensure successful unloading of registry hive." -Severity 2 -Source ${CmdletName}
                            [GC]::Collect()
                            [GC]::WaitForPendingFinalizers()
                            Start-Sleep -Seconds 5

                            Write-Log -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]." -Source ${CmdletName}
                            [String]$HiveLoadResult = & "$envWinDir\System32\reg.exe" unload "`"HKEY_USERS\$($UserProfile.SID)`""
                            If ($global:LastExitCode -ne 0) {
                                Throw "REG.exe failed with exit code [$($global:LastExitCode)] and result [$HiveLoadResult]."
                            }
                        }
                    }
                    Catch {
                        Write-Log -Message "Failed to unload the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                    }
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function ConvertTo-NTAccountOrSID
Function ConvertTo-NTAccountOrSID {
    <#
.SYNOPSIS

Convert between NT Account names and their security identifiers (SIDs).

.DESCRIPTION

Specify either the NT Account name or the SID and get the other. Can also convert well known sid types.

.PARAMETER AccountName

The Windows NT Account name specified in <domain>\<username> format.
Use fully qualified account names (e.g., <domain>\<username>) instead of isolated names (e.g, <username>) because they are unambiguous and provide better performance.

.PARAMETER SID

The Windows NT Account SID.

.PARAMETER WellKnownSIDName

Specify the Well Known SID name translate to the actual SID (e.g., LocalServiceSid).

To get all well known SIDs available on system: [Enum]::GetNames([Security.Principal.WellKnownSidType])

.PARAMETER WellKnownToNTAccount

Convert the Well Known SID to an NTAccount name

.INPUTS

System.String

Accepts a string containing the NT Account name or SID.

.OUTPUTS

System.String

Returns the NT Account name or SID.

.EXAMPLE

ConvertTo-NTAccountOrSID -AccountName 'CONTOSO\User1'

Converts a Windows NT Account name to the corresponding SID

.EXAMPLE

ConvertTo-NTAccountOrSID -SID 'S-1-5-21-1220945662-2111687655-725345543-14012660'

Converts a Windows NT Account SID to the corresponding NT Account Name

.EXAMPLE

ConvertTo-NTAccountOrSID -WellKnownSIDName 'NetworkServiceSid'

Converts a Well Known SID name to a SID

.NOTES

This is an internal script function and should typically not be called directly.

The conversion can return an empty result if the user account does not exist anymore or if translation fails.

http://blogs.technet.com/b/askds/archive/2011/07/28/troubleshooting-sid-translation-failures-from-the-obvious-to-the-not-so-obvious.aspx

.LINK

https://psappdeploytoolkit.com

.LINK

http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx

#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'NTAccountToSID', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$AccountName,
        [Parameter(Mandatory = $true, ParameterSetName = 'SIDToNTAccount', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$SID,
        [Parameter(Mandatory = $true, ParameterSetName = 'WellKnownName', ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$WellKnownSIDName,
        [Parameter(Mandatory = $false, ParameterSetName = 'WellKnownName')]
        [ValidateNotNullOrEmpty()]
        [Switch]$WellKnownToNTAccount
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Switch ($PSCmdlet.ParameterSetName) {
                'SIDToNTAccount' {
                    [String]$msg = "the SID [$SID] to an NT Account name"
                    Write-Log -Message "Converting $msg." -Source ${CmdletName}

                    $NTAccountSID = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($SID)
                    $NTAccount = $NTAccountSID.Translate([Security.Principal.NTAccount])
                    Write-Output -InputObject ($NTAccount)
                }
                'NTAccountToSID' {
                    [String]$msg = "the NT Account [$AccountName] to a SID"
                    Write-Log -Message "Converting $msg." -Source ${CmdletName}

                    $NTAccount = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($AccountName)
                    $NTAccountSID = $NTAccount.Translate([Security.Principal.SecurityIdentifier])
                    Write-Output -InputObject ($NTAccountSID)
                }
                'WellKnownName' {
                    If ($WellKnownToNTAccount) {
                        [String]$ConversionType = 'NTAccount'
                    }
                    Else {
                        [String]$ConversionType = 'SID'
                    }
                    [String]$msg = "the Well Known SID Name [$WellKnownSIDName] to a $ConversionType"
                    Write-Log -Message "Converting $msg." -Source ${CmdletName}

                    #  Get the SID for the root domain
                    Try {
                        $MachineRootDomain = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'Stop').Domain.ToLower()
                        $ADDomainObj = New-Object -TypeName 'System.DirectoryServices.DirectoryEntry' -ArgumentList ("LDAP://$MachineRootDomain")
                        $DomainSidInBinary = $ADDomainObj.ObjectSid
                        $DomainSid = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($DomainSidInBinary[0], 0)
                    }
                    Catch {
                        Write-Log -Message 'Unable to get Domain SID from Active Directory. Setting Domain SID to $null.' -Severity 2 -Source ${CmdletName}
                        $DomainSid = $null
                    }

                    #  Get the SID for the well known SID name
                    $WellKnownSidType = [Security.Principal.WellKnownSidType]::$WellKnownSIDName
                    $NTAccountSID = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($WellKnownSidType, $DomainSid)

                    If ($WellKnownToNTAccount) {
                        $NTAccount = $NTAccountSID.Translate([Security.Principal.NTAccount])
                        Write-Output -InputObject ($NTAccount)
                    }
                    Else {
                        Write-Output -InputObject ($NTAccountSID)
                    }
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-UserProfiles
Function Get-UserProfiles {
    <#
.SYNOPSIS

Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does not log on).

.DESCRIPTION

Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does  not log on).

Please note that the NTAccount property may be empty for some user profiles but the SID and ProfilePath properties will always be populated.

.PARAMETER ExcludeNTAccount

Specify NT account names in Domain\Username format to exclude from the list of user profiles.

.PARAMETER ExcludeSystemProfiles

Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $true.

.PARAMETER ExcludeDefaultUser

Exclude the Default User. Default is: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject. Returns a PSObject with the following properties: NTAccount, SID, ProfilePath

.EXAMPLE

Get-UserProfiles

Returns the following properties for each user profile on the system: NTAccount, SID, ProfilePath

.EXAMPLE

Get-UserProfiles -ExcludeNTAccount 'CONTOSO\Robot','CONTOSO\ntadmin'

.EXAMPLE

[String[]]$ProfilePaths = Get-UserProfiles | Select-Object -ExpandProperty 'ProfilePath'

Returns the user profile path for each user on the system. This information can then be used to make modifications under the user profile on the filesystem.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String[]]$ExcludeNTAccount,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeSystemProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$ExcludeDefaultUser = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Getting the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.' -Source ${CmdletName}

            ## Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine
            [String]$UserProfileListRegKey = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
            [PSObject[]]$UserProfiles = Get-ChildItem -LiteralPath $UserProfileListRegKey -ErrorAction 'Stop' |
                ForEach-Object {
                    Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'Stop' | Where-Object { ($_.ProfileImagePath) } |
                        Select-Object @{ Label = 'NTAccount'; Expression = { $(ConvertTo-NTAccountOrSID -SID $_.PSChildName).Value } }, @{ Label = 'SID'; Expression = { $_.PSChildName } }, @{ Label = 'ProfilePath'; Expression = { $_.ProfileImagePath } }
                    } |
                    Where-Object { $_.NTAccount } # This removes the "defaultuser0" account, which is a Windows 10 bug
            If ($ExcludeSystemProfiles) {
                [String[]]$SystemProfiles = 'S-1-5-18', 'S-1-5-19', 'S-1-5-20'
                [PSObject[]]$UserProfiles = $UserProfiles | Where-Object { $SystemProfiles -notcontains $_.SID }
            }
            If ($ExcludeNTAccount) {
                [PSObject[]]$UserProfiles = $UserProfiles | Where-Object { $ExcludeNTAccount -notcontains $_.NTAccount }
            }

            ## Find the path to the Default User profile
            If (-not $ExcludeDefaultUser) {
                [String]$UserProfilesDirectory = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'ProfilesDirectory' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'ProfilesDirectory'

                #  On Windows Vista or higher
                If (([Version]$envOSVersion).Major -gt 5) {
                    # Path to Default User Profile directory on Windows Vista or higher: By default, C:\Users\Default
                    [string]$DefaultUserProfileDirectory = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'Default' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Default'
                }
                #  On Windows XP or lower
                Else {
                    #  Default User Profile Name: By default, 'Default User'
                    [string]$DefaultUserProfileName = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'DefaultUserProfile' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'DefaultUserProfile'

                    #  Path to Default User Profile directory: By default, C:\Documents and Settings\Default User
                    [String]$DefaultUserProfileDirectory = Join-Path -Path $UserProfilesDirectory -ChildPath $DefaultUserProfileName
                }

                ## Create a custom object for the Default User profile.
                #  Since the Default User is not an actual User account, it does not have a username or a SID.
                #  We will make up a SID and add it to the custom object so that we have a location to load the default registry hive into later on.
                [PSObject]$DefaultUserProfile = New-Object -TypeName 'PSObject' -Property @{
                    NTAccount   = 'Default User'
                    SID         = 'S-1-5-21-Default-User'
                    ProfilePath = $DefaultUserProfileDirectory
                }

                ## Add the Default User custom object to the User Profile list.
                $UserProfiles += $DefaultUserProfile
            }

            Write-Output -InputObject ($UserProfiles)
        }
        Catch {
            Write-Log -Message "Failed to create a custom object representing all user profiles on the machine. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-FileVersion
Function Get-FileVersion {
    <#
.SYNOPSIS

Gets the version of the specified file

.DESCRIPTION

Gets the version of the specified file

.PARAMETER File

Path of the file

.PARAMETER ProductVersion

Switch that makes the command return ProductVersion instead of FileVersion

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Get-FileVersion -File "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$File,
        [Parameter(Mandatory = $false)]
        [Switch]$ProductVersion,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Getting version info for file [$file]." -Source ${CmdletName}

            If (Test-Path -LiteralPath $File -PathType 'Leaf') {
                $fileVersionInfo = (Get-Command -Name $file -ErrorAction 'Stop').FileVersionInfo
                If ($ProductVersion) {
                    $fileVersion = $fileVersionInfo.ProductVersion
                }
                Else {
                    $fileVersion = $fileVersionInfo.FileVersion
                }

                If ($fileVersion) {
                    If ($ProductVersion) {
                        Write-Log -Message "Product version is [$fileVersion]." -Source ${CmdletName}
                    }
                    Else {
                        Write-Log -Message "File version is [$fileVersion]." -Source ${CmdletName}
                    }

                    Write-Output -InputObject ($fileVersion)
                }
                Else {
                    Write-Log -Message 'No version information found.' -Source ${CmdletName}
                }
            }
            Else {
                Throw "File path [$file] does not exist."
            }
        }
        Catch {
            Write-Log -Message "Failed to get version info. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to get version info: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function New-Shortcut
Function New-Shortcut {
    <#
.SYNOPSIS

Creates a new .lnk or .url type shortcut

.DESCRIPTION

Creates a new shortcut .lnk or .url file, with configurable options

.PARAMETER Path

Path to save the shortcut

.PARAMETER TargetPath

Target path or URL that the shortcut launches

.PARAMETER Arguments

Arguments to be passed to the target path

.PARAMETER IconLocation

Location of the icon used for the shortcut

.PARAMETER IconIndex

The index of the icon. Executables, DLLs, ICO files with multiple icons need the icon index to be specified. This parameter is an Integer. The first index is 0.

.PARAMETER Description

Description of the shortcut

.PARAMETER WorkingDirectory

Working Directory to be used for the target path

.PARAMETER WindowStyle

Windows style of the application. Options: Normal, Maximized, Minimized. Default is: Normal.

.PARAMETER RunAsAdmin

Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut.

.PARAMETER Hotkey

Create a Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F"

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return any output.

.EXAMPLE

New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\System32\notepad.exe" -IconLocation "$envWinDir\System32\notepad.exe" -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"

.NOTES

Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$TargetPath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Arguments,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IconLocation,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$IconIndex,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Description,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Maximized', 'Minimized')]
        [String]$WindowStyle,
        [Parameter(Mandatory = $false)]
        [Switch]$RunAsAdmin,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Hotkey,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        If (-not $Shell) {
            [__ComObject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'Stop'
        }
    }
    Process {
        Try {
            $extension = [IO.Path]::GetExtension($Path).ToLower()
            If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
                Write-Log -Message "Specified file [$Path] does not have a valid shortcut extension: .url .lnk" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            Try {
                # Make sure Net framework current dir is synced with powershell cwd
                [IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider 'FileSystem').ProviderPath)
                # Get full path
                [String]$FullPath = [IO.Path]::GetFullPath($Path)
            }
            Catch {
                Write-Log -Message "Specified path [$Path] is not valid." -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }

            Try {
                [String]$PathDirectory = [IO.Path]::GetDirectoryName($FullPath)
                If (-not $PathDirectory) {
                    # The path is root or no filename supplied
                    If (-not [IO.Path]::GetFileNameWithoutExtension($FullPath)) {
                        # No filename supplied
                        If (-not $ContinueOnError) {
                            Throw
                        }
                        Return
                    }
                    # Continue without creating a folder because the path is root
                }
                ElseIf (-not (Test-Path -LiteralPath $PathDirectory -PathType 'Container' -ErrorAction 'Stop')) {
                    Write-Log -Message "Creating shortcut directory [$PathDirectory]." -Source ${CmdletName}
                    $null = New-Item -Path $PathDirectory -ItemType 'Directory' -Force -ErrorAction 'Stop'
                }
            }
            Catch {
                Write-Log -Message "Failed to create shortcut directory [$PathDirectory]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                Throw
            }

            If (Test-Path -Path $FullPath -PathType 'Leaf') {
                Write-Log -Message "The shortcut [$FullPath] already exists. Deleting the file..." -Source ${CmdletName}
                Remove-File -Path $FullPath
            }

            Write-Log -Message "Creating shortcut [$FullPath]." -Source ${CmdletName}
            If ($extension -eq '.url') {
                [String[]]$URLFile = '[InternetShortcut]'
                $URLFile += "URL=$targetPath"
                If ($null -ne $IconIndex) {
                    $URLFile += "IconIndex=$IconIndex"
                }
                If ($IconLocation) {
                    $URLFile += "IconFile=$IconLocation"
                }
                [IO.File]::WriteAllLines($FullPath, $URLFile, (New-Object -TypeName 'Text.UTF8Encoding' -ArgumentList ($false)))
            }
            Else {
                $shortcut = $shell.CreateShortcut($FullPath)
                ## TargetPath
                $shortcut.TargetPath = $targetPath
                ## Arguments
                If ($arguments) {
                    $shortcut.Arguments = $arguments
                }
                ## Description
                If ($description) {
                    $shortcut.Description = $description
                }
                ## Working directory
                If ($workingDirectory) {
                    $shortcut.WorkingDirectory = $workingDirectory
                }
                ## Window Style
                Switch ($windowStyle) {
                    'Normal' {
                        $windowStyleInt = 1
                    }
                    'Maximized' {
                        $windowStyleInt = 3
                    }
                    'Minimized' {
                        $windowStyleInt = 7
                    }
                    Default {
                        $windowStyleInt = 1
                    }
                }
                $shortcut.WindowStyle = $windowStyleInt
                ## Hotkey
                If ($Hotkey) {
                    $shortcut.Hotkey = $Hotkey
                }
                ## Icon
                If ($null -eq $IconIndex) {
                    $IconIndex = 0
                }
                If ($IconLocation) {
                    $shortcut.IconLocation = $IconLocation + ",$IconIndex"
                }
                ## Save the changes
                $shortcut.Save()

                ## Set shortcut to run program as administrator
                If ($RunAsAdmin) {
                    Write-Log -Message 'Setting shortcut to run program as administrator.' -Source ${CmdletName}
                    [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($FullPath)
                    $filebytes[21] = $filebytes[21] -bor 32
                    [IO.FIle]::WriteAllBytes($FullPath, $filebytes)
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to create shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to create shortcut [$Path]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion

#region Function Set-Shortcut
Function Set-Shortcut {
    <#
.SYNOPSIS

Modifies a .lnk or .url type shortcut

.DESCRIPTION

Modifies a shortcut - .lnk or .url file, with configurable options.

Only specify the parameters that you want to change.

.PARAMETER Path

Path to the shortcut to be changed

.PARAMETER TargetPath

Changes target path or URL that the shortcut launches

.PARAMETER Arguments

Changes Arguments to be passed to the target path

.PARAMETER IconLocation

Changes location of the icon used for the shortcut

.PARAMETER IconIndex

Change the index of the icon. Executables, DLLs, ICO files with multiple icons need the icon index to be specified. This parameter is an Integer. The first index is 0.

.PARAMETER Description

Changes description of the shortcut

.PARAMETER WorkingDirectory

Changes Working Directory to be used for the target path

.PARAMETER WindowStyle

Changes the Windows style of the application. Options: Normal, Maximized, Minimized, DontChange. Default is: DontChange.

.PARAMETER RunAsAdmin

Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut. If not specified or set to $null, the flag will not be changed.

.PARAMETER Hotkey

Changes the Hotkey to launch the shortcut, e.g. "CTRL+SHIFT+F"

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

PSOjbect

Path to the shortcut to be changed or a hashtable of parameters to be changed

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\System32\notepad.exe" -IconLocation "$envWinDir\System32\notepad.exe" -IconIndex 0 -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"

.NOTES

Url shortcuts only support TargetPath, IconLocation and IconIndex. Other parameters are ignored.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'Default')]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0, ParameterSetName = 'Default')]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0, ParameterSetName = 'Pipeline')]
        [ValidateNotNullorEmpty()]
        [Hashtable]$PathHash,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$TargetPath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Arguments,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IconLocation,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IconIndex,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$Description,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Maximized', 'Minimized', 'DontChange')]
        [String]$WindowStyle = 'DontChange',
        [Parameter(Mandatory = $false)]
        [System.Nullable[Boolean]]$RunAsAdmin,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Hotkey,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        If (-not $Shell) {
            [__ComObject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'Stop'
        }
    }
    Process {
        Try {
            If ($PsCmdlet.ParameterSetName -eq 'Pipeline') {
                $Path = $PathHash.Path
            }

            If (-not (Test-Path -LiteralPath $Path -PathType 'Leaf' -ErrorAction 'Stop')) {
                Write-Log -Message "Failed to find the file [$Path]." -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            $extension = [IO.Path]::GetExtension($Path).ToLower()
            If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
                Write-Log -Message "Specified file [$Path] is not a valid shortcut." -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            # Make sure Net framework current dir is synced with powershell cwd
            [IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider 'FileSystem').ProviderPath)
            Write-Log -Message "Changing shortcut [$Path]." -Source ${CmdletName}
            If ($extension -eq '.url') {
                [String[]]$URLFile = [IO.File]::ReadAllLines($Path)
                For ($i = 0; $i -lt $URLFile.Length; $i++) {
                    $URLFile[$i] = $URLFile[$i].TrimStart()
                    If ($URLFile[$i].StartsWith('URL=') -and $targetPath) {
                        $URLFile[$i] = "URL=$targetPath"
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconIndex=') -and ($null -ne $IconIndex)) {
                        $URLFile[$i] = "IconIndex=$IconIndex"
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconFile=') -and $IconLocation) {
                        $URLFile[$i] = "IconFile=$IconLocation"
                    }
                }
                [IO.File]::WriteAllLines($Path, $URLFile, (New-Object -TypeName 'Text.UTF8Encoding' -ArgumentList ($false)))
            }
            Else {
                $shortcut = $shell.CreateShortcut($Path)
                ## TargetPath
                If ($targetPath) {
                    $shortcut.TargetPath = $targetPath
                }
                ## Arguments
                If ($arguments) {
                    $shortcut.Arguments = $arguments
                }
                ## Description
                If ($description) {
                    $shortcut.Description = $description
                }
                ## Working directory
                If ($workingDirectory) {
                    $shortcut.WorkingDirectory = $workingDirectory
                }
                ## Window Style
                Switch ($windowStyle) {
                    'Normal' {
                        $windowStyleInt = 1
                    }
                    'Maximized' {
                        $windowStyleInt = 3
                    }
                    'Minimized' {
                        $windowStyleInt = 7
                    }
                    'DontChange' {
                        $windowStyleInt = 0
                    }
                    Default {
                        $windowStyleInt = 1
                    }
                }
                If ($windowStyleInt -ne 0) {
                    $shortcut.WindowStyle = $windowStyleInt
                }
                ## Hotkey
                If ($Hotkey) {
                    $shortcut.Hotkey = $Hotkey
                }
                ## Icon
                # Retrieve previous value and split the path from the index
                [String[]]$Split = $shortcut.IconLocation.Split(',')
                $TempIconLocation = $Split[0]
                $TempIconIndex = $Split[1]
                # Check whether a new icon path was specified
                If ($IconLocation) {
                    # New icon path was specified. Check whether new icon index was also specified
                    If ($null -ne $IconIndex) {
                        # Create new icon path from new icon path and new icon index
                        $IconLocation = $IconLocation + ",$IconIndex"
                    }
                    Else {
                        # No new icon index was specified as a parameter. We will keep the old one
                        $IconLocation = $IconLocation + ",$TempIconIndex"
                    }
                }
                ElseIf ($null -ne $IconIndex) {
                    # New icon index was specified, but not the icon location. Append it to the icon path from the shortcut
                    $IconLocation = $TempIconLocation + ",$IconIndex"
                }
                If ($IconLocation) {
                    $shortcut.IconLocation = $IconLocation
                }
                ## Save the changes
                $shortcut.Save()

                ## Set shortcut to run program as administrator
                If ($RunAsAdmin -eq $true) {
                    Write-Log -Message 'Setting shortcut to run program as administrator.' -Source ${CmdletName}
                    [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($Path)
                    $filebytes[21] = $filebytes[21] -bor 32
                    [IO.FIle]::WriteAllBytes($Path, $filebytes)
                }
                ElseIf ($RunAsAdmin -eq $false) {
                    [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($Path)
                    Write-Log -Message 'Setting shortcut to not run program as administrator.' -Source ${CmdletName}
                    $filebytes[21] = $filebytes[21] -band (-bnot 32)
                    [IO.FIle]::WriteAllBytes($Path, $filebytes)
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to change the shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to change the shortcut [$Path]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion

#region Function Get-Shortcut
Function Get-Shortcut {
    <#
.SYNOPSIS

Get information from a new .lnk or .url type shortcut

.DESCRIPTION

Get information from a new .lnk or .url type shortcut. Returns a hashtable.

.PARAMETER Path

Path to the shortcut to get information from

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Collections.Hashtable.

Returns a hashtable with the following keys
- TargetPath
- Arguments
- Description
- WorkingDirectory
- WindowStyle
- Hotkey
- IconLocation
- IconIndex
- RunAsAdmin

.EXAMPLE

Get-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"

.NOTES

Url shortcuts only support TargetPath, IconLocation and IconIndex.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        If (-not $Shell) {
            [__ComObject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'Stop'
        }
    }
    Process {
        Try {
            $extension = [IO.Path]::GetExtension($Path).ToLower()
            If ((-not $extension) -or (($extension -ne '.lnk') -and ($extension -ne '.url'))) {
                Write-Log -Message "Specified file [$Path] does not have a valid shortcut extension: .url .lnk" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }
            Try {
                # Make sure Net framework current dir is synced with powershell cwd
                [IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider 'FileSystem').ProviderPath)
                # Get full path
                [String]$FullPath = [IO.Path]::GetFullPath($Path)
            }
            Catch {
                Write-Log -Message "Specified path [$Path] is not valid." -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw
                }
                Return
            }

            $Output = @{ Path = $FullPath }
            If ($extension -eq '.url') {
                [String[]]$URLFile = [IO.File]::ReadAllLines($Path)
                For ($i = 0; $i -lt $URLFile.Length; $i++) {
                    $URLFile[$i] = $URLFile[$i].TrimStart()
                    If ($URLFile[$i].StartsWith('URL=')) {
                        $Output.TargetPath = $URLFile[$i].Replace('URL=', '')
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconIndex=')) {
                        $Output.IconIndex = $URLFile[$i].Replace('IconIndex=', '')
                    }
                    ElseIf ($URLFile[$i].StartsWith('IconFile=')) {
                        $Output.IconLocation = $URLFile[$i].Replace('IconFile=', '')
                    }
                }
            }
            Else {
                $shortcut = $shell.CreateShortcut($FullPath)
                ## TargetPath
                $Output.TargetPath = $shortcut.TargetPath
                ## Arguments
                $Output.Arguments = $shortcut.Arguments
                ## Description
                $Output.Description = $shortcut.Description
                ## Working directory
                $Output.WorkingDirectory = $shortcut.WorkingDirectory
                ## Window Style
                Switch ($shortcut.WindowStyle) {
                    1 {
                        $Output.WindowStyle = 'Normal'
                    }
                    3 {
                        $Output.WindowStyle = 'Maximized'
                    }
                    7 {
                        $Output.WindowStyle = 'Minimized'
                    }
                    Default {
                        $Output.WindowStyle = 'Normal'
                    }
                }
                ## Hotkey
                $Output.Hotkey = $shortcut.Hotkey
                ## Icon
                [String[]]$Split = $shortcut.IconLocation.Split(',')
                $Output.IconLocation = $Split[0]
                $Output.IconIndex = $Split[1]
                ## Remove the variable
                $shortcut = $null
                ## Run as admin
                [Byte[]]$filebytes = [IO.FIle]::ReadAllBytes($FullPath)
                If ($filebytes[21] -band 32) {
                    $Output.RunAsAdmin = $true
                }
                Else {
                    $Output.RunAsAdmin = $false
                }
            }
            Write-Output -InputObject ($Output)
        }
        Catch {
            Write-Log -Message "Failed to read the shortcut [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to read the shortcut [$Path]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion

#region Function Execute-ProcessAsUser
Function Execute-ProcessAsUser {
    <#
.SYNOPSIS

Execute a process with a logged in user account, by using a scheduled task, to provide interaction with user in the SYSTEM context.

.DESCRIPTION

Execute a process with a logged in user account, by using a scheduled task, to provide interaction with user in the SYSTEM context.

.PARAMETER UserName

Logged in Username under which to run the process from. Default is: The active console user. If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.

.PARAMETER Path

Path to the file being executed.

.PARAMETER Parameters

Arguments to be passed to the file being executed.

.PARAMETER SecureParameters

Hides all parameters passed to the executable from the Toolkit log file.

.PARAMETER RunLevel

Specifies the level of user rights that Task Scheduler uses to run the task. The acceptable values for this parameter are:

- HighestAvailable: Tasks run by using the highest available privileges (Admin privileges for Administrators). Default Value.

- LeastPrivilege: Tasks run by using the least-privileged user account (LUA) privileges.

.PARAMETER Wait

Wait for the process, launched by the scheduled task, to complete execution before accepting more input. Default is $false.

.PARAMETER PassThru

Returns the exit code from this function or the process launched by the scheduled task.

.PARAMETER WorkingDirectory

Set working directory for the process.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Int32.

Returns the exit code from this function or the process launched by the scheduled task.

.EXAMPLE

Execute-ProcessAsUser -UserName 'CONTOSO\User' -Path "$PSHOME\powershell.exe" -Parameters "-Command & { & `"C:\Test\Script.ps1`"; Exit `$LastExitCode }" -Wait

Execute process under a user account by specifying a username under which to execute it.

.EXAMPLE

Execute-ProcessAsUser -Path "$PSHOME\powershell.exe" -Parameters "-Command & { & `"C:\Test\Script.ps1`"; Exit `$LastExitCode }" -Wait

Execute process under a user account by using the default active logged in user that was detected when the toolkit was launched.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$UserName = $RunAsActiveUser.NTAccount,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Parameters = '',
        [Parameter(Mandatory = $false)]
        [Switch]$SecureParameters = $false,
        [Parameter(Mandatory = $false)]
        [ValidateSet('HighestAvailable', 'LeastPrivilege')]
        [String]$RunLevel = 'HighestAvailable',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$Wait = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
        [String]$executeAsUserTempPath = Join-Path -Path $dirAppDeployTemp -ChildPath 'ExecuteAsUser'
    }
    Process {
        ## Initialize exit code variable
        [Int32]$executeProcessAsUserExitCode = 0

        ## Confirm that the username field is not empty
        If (-not $UserName) {
            [Int32]$executeProcessAsUserExitCode = 60009
            Write-Log -Message "The function [${CmdletName}] has a -UserName parameter that has an empty default value because no logged in users were detected when the toolkit was launched." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "The function [${CmdletName}] has a -UserName parameter that has an empty default value because no logged in users were detected when the toolkit was launched."
            }
            Return
        }

        ## Confirm if the toolkit is running with administrator privileges
        If (($RunLevel -eq 'HighestAvailable') -and (-not $IsAdmin)) {
            [Int32]$executeProcessAsUserExitCode = 60003
            Write-Log -Message "The function [${CmdletName}] requires the toolkit to be running with Administrator privileges if the [-RunLevel] parameter is set to 'HighestAvailable'." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "The function [${CmdletName}] requires the toolkit to be running with Administrator privileges if the [-RunLevel] parameter is set to 'HighestAvailable'."
            }
            Return
        }

        ## Check whether the specified Working Directory exists
        If ($WorkingDirectory -and (-not (Test-Path -LiteralPath $WorkingDirectory -PathType 'Container'))) {
            Write-Log -Message 'The specified working directory does not exist or is not a directory. The scheduled task might not work as expected.' -Severity 2 -Source ${CmdletName}
        }

        ## Build the scheduled task XML name
        [String]$schTaskName = "$appDeployToolkitName-ExecuteAsUser"

        ##  Remove and recreate the temporary folder
        If (Test-Path -LiteralPath $executeAsUserTempPath -PathType 'Container') {
            Write-Log -Message "Previous [$executeAsUserTempPath] found. Attempting removal." -Source ${CmdletName}
            Remove-Folder -Path $executeAsUserTempPath
        }
        Write-Log -Message "Creating [$executeAsUserTempPath]." -Source ${CmdletName}
        Try {
            $null = New-Item -Path $executeAsUserTempPath -ItemType 'Directory' -ErrorAction 'Stop'
        }
        Catch {
            Write-Log -Message "Unable to create [$executeAsUserTempPath]. Possible attempt to gain elevated rights." -Source ${CmdletName} -Severity 2
        }

        ## Escape XML characters
        $EscapedPath = [System.Security.SecurityElement]::Escape($Path)
        $EscapedParameters = [System.Security.SecurityElement]::Escape($Parameters)

        ## If PowerShell.exe is being launched, then create a VBScript to launch PowerShell so that we can suppress the console window that flashes otherwise
        If (((Split-Path -Path $Path -Leaf) -like 'PowerShell*') -or ((Split-Path -Path $Path -Leaf) -like 'cmd*')) {
            If ($SecureParameters) {
                Write-Log -Message "Preparing a VBScript that will start [$Path] (Parameters Hidden) as the logged-on user [$userName] and suppress the console window..." -Source ${CmdletName}
            }
            Else {
                Write-Log -Message "Preparing a VBScript that will start [$Path $Parameters] as the logged-on user [$userName] and suppress the console window..." -Source ${CmdletName}
            }

            # Permit inclusion of double quotes in parameters
            $QuotesIndex = $Parameters.Length - 1
            If ($QuotesIndex -lt 0) {
                $QuotesIndex = 0
            }

            If ($($Parameters.Substring($QuotesIndex)) -eq '"') {
                [String]$executeProcessAsUserParametersVBS = 'chr(34) & ' + "`"$($Path)`"" + ' & chr(34) & ' + '" ' + ($Parameters -replace "`r`r`n", ';' -replace "`r`n", ';' -replace '"', "`" & chr(34) & `"" -replace ' & chr\(34\) & "$', '') + ' & chr(34)'
            }
            Else {
                [String]$executeProcessAsUserParametersVBS = 'chr(34) & ' + "`"$($Path)`"" + ' & chr(34) & ' + '" ' + ($Parameters -replace "`r`r`n", ';' -replace "`r`n", ';' -replace '"', "`" & chr(34) & `"" -replace ' & chr\(34\) & "$', '') + '"'
            }

            [String[]]$executeProcessAsUserScript = "strCommand = $executeProcessAsUserParametersVBS"
            $executeProcessAsUserScript += 'set oWShell = CreateObject("WScript.Shell")'
            $executeProcessAsUserScript += 'intReturn = oWShell.Run(strCommand, 0, true)'
            $executeProcessAsUserScript += 'WScript.Quit intReturn'
            $executeProcessAsUserScript | Out-File -FilePath "$executeAsUserTempPath\$($schTaskName).vbs" -Force -Encoding 'Default' -ErrorAction 'SilentlyContinue'
            $Path = "$envWinDir\System32\wscript.exe"
            $Parameters = "`"$executeAsUserTempPath\$($schTaskName).vbs`""
            $EscapedPath = [System.Security.SecurityElement]::Escape($Path)
            $EscapedParameters = [System.Security.SecurityElement]::Escape($Parameters)

            Try {
                Set-ItemPermission -Path "$executeAsUserTempPath\$schTaskName.vbs" -User $UserName -Permission 'Read'
            }
            Catch {
                Write-Log -Message "Failed to set read permissions on path [$executeAsUserTempPath\$schTaskName.vbs]. The function might not be able to work correctly." -Source ${CmdletName} -Severity 2
            }
        }

        ## Prepare working directory insert
        [String]$WorkingDirectoryInsert = ''
        If ($WorkingDirectory) {
            $WorkingDirectoryInsert = "`r`n	  <WorkingDirectory>$WorkingDirectory</WorkingDirectory>"
        }

        ## Specify the scheduled task configuration in XML format
        [String]$xmlSchTask = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo />
  <Triggers />
  <Settings>
	<MultipleInstancesPolicy>StopExisting</MultipleInstancesPolicy>
	<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
	<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
	<AllowHardTerminate>true</AllowHardTerminate>
	<StartWhenAvailable>false</StartWhenAvailable>
	<RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
	<IdleSettings>
	  <StopOnIdleEnd>false</StopOnIdleEnd>
	  <RestartOnIdle>false</RestartOnIdle>
	</IdleSettings>
	<AllowStartOnDemand>true</AllowStartOnDemand>
	<Enabled>true</Enabled>
	<Hidden>false</Hidden>
	<RunOnlyIfIdle>false</RunOnlyIfIdle>
	<WakeToRun>false</WakeToRun>
	<ExecutionTimeLimit>PT72H</ExecutionTimeLimit>
	<Priority>7</Priority>
  </Settings>
  <Actions Context="Author">
	<Exec>
	  <Command>$EscapedPath</Command>
	  <Arguments>$EscapedParameters</Arguments>$WorkingDirectoryInsert
	</Exec>
  </Actions>
  <Principals>
	<Principal id="Author">
	  <UserId>$UserName</UserId>
	  <LogonType>InteractiveToken</LogonType>
	  <RunLevel>$RunLevel</RunLevel>
	</Principal>
  </Principals>
</Task>
"@
        ## Export the XML to file
        Try {
            #  Specify the filename to export the XML to
            [String]$xmlSchTaskFilePath = "$dirAppDeployTemp\$schTaskName.xml"
            [String]$xmlSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction 'Stop'
            Set-ItemPermission -Path $xmlSchTaskFilePath -User $UserName -Permission 'Read'
        }
        Catch {
            [Int32]$executeProcessAsUserExitCode = 60007
            Write-Log -Message "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]: $($_.Exception.Message)"
            }
            Return
        }

        ## Create Scheduled Task to run the process with a logged-on user account
        If ($Parameters) {
            If ($SecureParameters) {
                Write-Log -Message "Creating scheduled task to run the process [$Path] (Parameters Hidden) as the logged-on user [$userName]..." -Source ${CmdletName}
            }
            Else {
                Write-Log -Message "Creating scheduled task to run the process [$Path $Parameters] as the logged-on user [$userName]..." -Source ${CmdletName}
            }
        }
        Else {
            Write-Log -Message "Creating scheduled task to run the process [$Path] as the logged-on user [$userName]..." -Source ${CmdletName}
        }
        [PSObject]$schTaskResult = Execute-Process -Path $exeSchTasks -Parameters "/create /f /tn $schTaskName /xml `"$xmlSchTaskFilePath`"" -WindowStyle 'Hidden' -CreateNoWindow -PassThru -ExitOnProcessFailure $false
        If ($schTaskResult.ExitCode -ne 0) {
            [Int32]$executeProcessAsUserExitCode = $schTaskResult.ExitCode
            Write-Log -Message "Failed to create the scheduled task by importing the scheduled task XML file [$xmlSchTaskFilePath]." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to create the scheduled task by importing the scheduled task XML file [$xmlSchTaskFilePath]."
            }
            Return
        }

        ## Trigger the Scheduled Task
        If ($Parameters) {
            If ($SecureParameters) {
                Write-Log -Message "Triggering execution of scheduled task with command [$Path] (Parameters Hidden) as the logged-on user [$userName]..." -Source ${CmdletName}
            }
            Else {
                Write-Log -Message "Triggering execution of scheduled task with command [$Path $Parameters] as the logged-on user [$userName]..." -Source ${CmdletName}
            }
        }
        Else {
            Write-Log -Message "Triggering execution of scheduled task with command [$Path] as the logged-on user [$userName]..." -Source ${CmdletName}
        }
        [PSObject]$schTaskResult = Execute-Process -Path $exeSchTasks -Parameters "/run /i /tn $schTaskName" -WindowStyle 'Hidden' -CreateNoWindow -Passthru -ExitOnProcessFailure $false
        If ($schTaskResult.ExitCode -ne 0) {
            [Int32]$executeProcessAsUserExitCode = $schTaskResult.ExitCode
            Write-Log -Message "Failed to trigger scheduled task [$schTaskName]." -Severity 3 -Source ${CmdletName}
            #  Delete Scheduled Task
            Write-Log -Message 'Deleting the scheduled task which did not trigger.' -Source ${CmdletName}
            Execute-Process -Path $exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle 'Hidden' -CreateNoWindow -ExitOnProcessFailure $false
            If (-not $ContinueOnError) {
                Throw "Failed to trigger scheduled task [$schTaskName]."
            }
            Return
        }

        ## Wait for the process launched by the scheduled task to complete execution
        If ($Wait) {
            Write-Log -Message "Waiting for the process launched by the scheduled task [$schTaskName] to complete execution (this may take some time)..." -Source ${CmdletName}
            Start-Sleep -Seconds 1
            #If on Windows Vista or higer, Windows Task Scheduler 2.0 is supported. 'Schedule.Service' ComObject output is UI language independent
            If (([Version]$envOSVersion).Major -gt 5) {
                Try {
                    [__ComObject]$ScheduleService = New-Object -ComObject 'Schedule.Service' -ErrorAction 'Stop'
                    $ScheduleService.Connect()
                    $RootFolder = $ScheduleService.GetFolder('\')
                    $Task = $RootFolder.GetTask("$schTaskName")
                    # Task State(Status) 4 = 'Running'
                    While ($Task.State -eq 4) {
                        Start-Sleep -Seconds 5
                    }
                    #  Get the exit code from the process launched by the scheduled task
                    [Int32]$executeProcessAsUserExitCode = $Task.LastTaskResult
                }
                Catch {
                    Write-Log -Message "Failed to retrieve information from Task Scheduler. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                }
                Finally {
                    Try {
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($ScheduleService)
                    }
                    Catch {
                    }
                }
            }
            #Windows Task Scheduler 1.0
            Else {
                While ((($exeSchTasksResult = & $exeSchTasks /query /TN $schTaskName /V /FO CSV) | ConvertFrom-Csv | Select-Object -ExpandProperty 'Status' -First 1) -eq 'Running') {
                    Start-Sleep -Seconds 5
                }
                #  Get the exit code from the process launched by the scheduled task
                [Int32]$executeProcessAsUserExitCode = ($exeSchTasksResult = & $exeSchTasks /query /TN $schTaskName /V /FO CSV) | ConvertFrom-Csv | Select-Object -ExpandProperty 'Last Result' -First 1
            }
            Write-Log -Message "Exit code from process launched by scheduled task [$executeProcessAsUserExitCode]." -Source ${CmdletName}
        }
        Else {
            Start-Sleep -Seconds 1
        }

        ## Delete scheduled task
        Try {
            Write-Log -Message "Deleting scheduled task [$schTaskName]." -Source ${CmdletName}
            Execute-Process -Path $exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle 'Hidden' -CreateNoWindow -ErrorAction 'Stop'
        }
        Catch {
            Write-Log -Message "Failed to delete scheduled task [$schTaskName]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Remove the XML scheduled task file
        If (Test-Path -LiteralPath $xmlSchTaskFilePath -PathType 'Leaf') {
            Remove-File -Path $xmlSchTaskFilePath
        }

        ##  Remove the temporary folder
        If (Test-Path -LiteralPath $executeAsUserTempPath -PathType 'Container') {
            Remove-Folder -Path $executeAsUserTempPath
        }
    }
    End {
        If ($PassThru) {
            Write-Output -InputObject ($executeProcessAsUserExitCode)
        }

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Update-Desktop
Function Update-Desktop {
    <#
.SYNOPSIS

Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

.DESCRIPTION

Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return objects.

.EXAMPLE

Update-Desktop

.NOTES

This function has an alias: Refresh-Desktop

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Refreshing the Desktop and the Windows Explorer environment process block.' -Source ${CmdletName}
            [PSADT.Explorer]::RefreshDesktopAndEnvironmentVariables()
        }
        Catch {
            Write-Log -Message "Failed to refresh the Desktop and the Windows Explorer environment process block. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to refresh the Desktop and the Windows Explorer environment process block: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
Set-Alias -Name 'Refresh-Desktop' -Value 'Update-Desktop' -Scope 'Script' -Force -ErrorAction 'SilentlyContinue'
#endregion


#region Function Update-SessionEnvironmentVariables
Function Update-SessionEnvironmentVariables {
    <#
.SYNOPSIS

Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.

.DESCRIPTION

Environment variable changes that take place during script execution are not visible to the current PowerShell session.

Use this function to refresh the current PowerShell session with all environment variable settings.

.PARAMETER LoadLoggedOnUserEnvironmentVariables

If script is running in SYSTEM context, this option allows loading environment variables from the active console user. If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return objects.

.EXAMPLE

Update-SessionEnvironmentVariables

.NOTES

This function has an alias: Refresh-SessionEnvironmentVariables

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$LoadLoggedOnUserEnvironmentVariables = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        [ScriptBlock]$GetEnvironmentVar = {
            Param (
                $Key,
                $Scope
            )
            [Environment]::GetEnvironmentVariable($Key, $Scope)
        }
    }
    Process {
        Try {
            Write-Log -Message 'Refreshing the environment variables for this PowerShell session.' -Source ${CmdletName}

            If ($LoadLoggedOnUserEnvironmentVariables -and $RunAsActiveUser) {
                [String]$CurrentUserEnvironmentSID = $RunAsActiveUser.SID
            }
            Else {
                [String]$CurrentUserEnvironmentSID = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
            }
            [String]$MachineEnvironmentVars = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
            [String]$UserEnvironmentVars = "Registry::HKEY_USERS\$CurrentUserEnvironmentSID\Environment"

            ## Update all session environment variables. Ordering is important here: $UserEnvironmentVars comes second so that we can override $MachineEnvironmentVars.
            $MachineEnvironmentVars, $UserEnvironmentVars | Get-Item | Where-Object { $_ } | ForEach-Object { $envRegPath = $_.PSPath; $_ | Select-Object -ExpandProperty 'Property' | ForEach-Object { Set-Item -LiteralPath "env:$($_)" -Value (Get-ItemProperty -LiteralPath $envRegPath -Name $_).$_ } }

            ## Set PATH environment variable separately because it is a combination of the user and machine environment variables
            [String[]]$PathFolders = 'Machine', 'User' | ForEach-Object { (& $GetEnvironmentVar -Key 'PATH' -Scope $_) } | Where-Object { $_ } | ForEach-Object { $_.Trim(';').Split(';').Trim().Trim('"') } | Select-Object -Unique
            $env:PATH = $PathFolders -join ';'
        }
        Catch {
            Write-Log -Message "Failed to refresh the environment variables for this PowerShell session. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to refresh the environment variables for this PowerShell session: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
Set-Alias -Name 'Refresh-SessionEnvironmentVariables' -Value 'Update-SessionEnvironmentVariables' -Scope 'Script' -Force -ErrorAction 'SilentlyContinue'
#endregion


#region Function Get-SchedulerTask
Function Get-SchedulerTask {
    <#
.SYNOPSIS

Retrieve all details for scheduled tasks on the local computer.

.DESCRIPTION

Retrieve all details for scheduled tasks on the local computer using schtasks.exe. All property names have spaces and colons removed.

.PARAMETER TaskName

Specify the name of the scheduled task to retrieve details for. Uses regex match to find scheduled task.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSOjbect. This function returns a PSObject with all scheduled task properties.

.EXAMPLE

Get-SchedulerTask

To display a list of all scheduled task properties.

.EXAMPLE

Get-SchedulerTask | Out-GridView

To display a grid view of all scheduled task properties.

.EXAMPLE

Get-SchedulerTask | Select-Object -Property TaskName

To display a list of all scheduled task names.

.NOTES

This function has an alias: Get-ScheduledTask if Get-ScheduledTask is not defined

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$TaskName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        [PSObject[]]$ScheduledTasks = @()
    }
    Process {
        Try {
            Write-Log -Message 'Retrieving Scheduled Tasks...' -Source ${CmdletName}
            [String[]]$exeSchtasksResults = & $exeSchTasks /Query /V /FO CSV
            If ($global:LastExitCode -ne 0) {
                Throw "Failed to retrieve scheduled tasks using [$exeSchTasks]."
            }
            [PSObject[]]$SchtasksResults = $exeSchtasksResults | ConvertFrom-Csv -Header 'HostName', 'TaskName', 'Next Run Time', 'Status', 'Logon Mode', 'Last Run Time', 'Last Result', 'Author', 'Task To Run', 'Start In', 'Comment', 'Scheduled Task State', 'Idle Time', 'Power Management', 'Run As User', 'Delete Task If Not Rescheduled', 'Stop Task If Runs X Hours and X Mins', 'Schedule', 'Schedule Type', 'Start Time', 'Start Date', 'End Date', 'Days', 'Months', 'Repeat: Every', 'Repeat: Until: Time', 'Repeat: Until: Duration', 'Repeat: Stop If Still Running' -ErrorAction 'Stop'

            If ($SchtasksResults) {
                ForEach ($SchtasksResult in $SchtasksResults) {
                    If ($SchtasksResult.TaskName -match $TaskName) {
                        $SchtasksResult | Get-Member -MemberType 'Properties' |
                            ForEach-Object -Begin {
                                [Hashtable]$Task = @{}
                            } -Process {
                                ## Remove spaces and colons in property names. Do not set property value if line being processed is a column header (this will only work on English language machines).
							($Task.($($_.Name).Replace(' ', '').Replace(':', ''))) = If ($_.Name -ne $SchtasksResult.($_.Name)) {
                                    $SchtasksResult.($_.Name)
                                }
                            } -End {
                                ## Only add task to the custom object if all property values are not empty
                                If (($Task.Values | Select-Object -Unique | Measure-Object).Count) {
                                    $ScheduledTasks += New-Object -TypeName 'PSObject' -Property $Task
                                }
                            }
                    }
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to retrieve scheduled tasks. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to retrieve scheduled tasks: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-Output -InputObject ($ScheduledTasks)
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
# If Get-ScheduledTask doesn't exist, add alias Get-ScheduledTask
If (-not (Get-Command -Name 'Get-ScheduledTask' -ErrorAction 'SilentlyContinue')) {
    New-Alias -Name 'Get-ScheduledTask' -Value 'Get-SchedulerTask'
}
#endregion


#region Function Block-AppExecution
Function Block-AppExecution {
    <#
.SYNOPSIS

Block the execution of an application(s)

.DESCRIPTION

This function is called when you pass the -BlockExecution parameter to the Stop-RunningApplications function. It does the following:

1.  Makes a copy of this script in a temporary directory on the local machine.
2.  Checks for an existing scheduled task from previous failed installation attempt where apps were blocked and if found, calls the Unblock-AppExecution function to restore the original IFEO registry keys.
		This is to prevent the function from overriding the backup of the original IFEO options.
3.  Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameter -CleanupBlockedApps.
4.  Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameter -ShowBlockedAppDialog.
5.  When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress.
		The text of this message can be customized in the XML configuration file.

.PARAMETER ProcessName

Name of the process or processes separated by commas

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Block-AppExecution -ProcessName ('winword','excel')

.NOTES

This is an internal script function and should typically not be called directly.

It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to block applications.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        ## Specify process names separated by commas
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String[]]$ProcessName
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		## Remove illegal characters from the scheduled task arguments string
		[char[]]$invalidScheduledTaskChars = '$', '!', '''', '"', '(', ')', ';', '\', '`', '*', '?', '{', '}', '[', ']', '<', '>', '|', '&', '%', '#', '~', '@', ' '
		[string]$SchInstallName = $installName
		ForEach ($invalidChar in $invalidScheduledTaskChars) {
            [string]$SchInstallName = $SchInstallName -replace [regex]::Escape($invalidChar),'' 
        }
		[string]$blockExecutionTempPath = Join-Path -Path $dirAppDeployTemp -ChildPath 'BlockExecution'
		[string]$schTaskUnblockAppsCommand += "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `"$blockExecutionTempPath\$scriptFileName`" -CleanupBlockedApps -ReferredInstallName `"$SchInstallName`" -ReferredInstallTitle `"$installTitle`" -ReferredLogName `"$logName`" -AsyncToolkitLaunch"
		## Specify the scheduled task configuration in XML format
		[string]$xmlUnblockAppsSchTask = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
	<RegistrationInfo></RegistrationInfo>
	<Triggers>
		<BootTrigger>
			<Enabled>true</Enabled>
		</BootTrigger>
	</Triggers>
	<Principals>
		<Principal id="Author">
			<UserId>S-1-5-18</UserId>
		</Principal>
	</Principals>
	<Settings>
		<MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
		<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
		<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
		<AllowHardTerminate>true</AllowHardTerminate>
		<StartWhenAvailable>false</StartWhenAvailable>
		<RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
		<IdleSettings>
			<StopOnIdleEnd>false</StopOnIdleEnd>
			<RestartOnIdle>false</RestartOnIdle>
		</IdleSettings>
		<AllowStartOnDemand>true</AllowStartOnDemand>
		<Enabled>true</Enabled>
		<Hidden>false</Hidden>
		<RunOnlyIfIdle>false</RunOnlyIfIdle>
		<WakeToRun>false</WakeToRun>
		<ExecutionTimeLimit>PT1H</ExecutionTimeLimit>
		<Priority>7</Priority>
	</Settings>
	<Actions Context="Author">
		<Exec>
			<Command>$PSHome\powershell.exe</Command>
			<Arguments>$schTaskUnblockAppsCommand</Arguments>
		</Exec>
	</Actions>
</Task>
"@
    }
    Process {
        ## Bypass if no Admin rights
        If ($configToolkitRequireAdmin -eq $false) {
            Write-Log -Message "Bypassing Function [${CmdletName}], because [Require Admin: $configToolkitRequireAdmin]." -Source ${CmdletName}
            Return
        }

        [String]$schTaskBlockedAppsName = $installName + '_BlockedApps'

        ## Delete this file if it exists as it can cause failures (it is a bug from an older version of the toolkit)
        If (Test-Path -LiteralPath "$configToolkitTempPath\PSAppDeployToolkit" -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
            $null = Remove-Item -LiteralPath "$configToolkitTempPath\PSAppDeployToolkit" -Force -ErrorAction 'SilentlyContinue'
        }

        If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
            Remove-Folder -Path $blockExecutionTempPath
        }

        Try {
            $null = New-Item -Path $blockExecutionTempPath -ItemType 'Directory' -ErrorAction 'Stop'
        }
        Catch {
            Write-Log -Message "Unable to create [$blockExecutionTempPath]. Possible attempt to gain elevated rights." -Source ${CmdletName}
        }

        Copy-Item -Path "$scriptRoot\*.*" -Destination $blockExecutionTempPath -Exclude 'thumbs.db' -Force -Recurse -ErrorAction 'SilentlyContinue'

        ## Build the debugger block value script
        [String[]]$debuggerBlockScript = "strCommand = `"$PSHome\powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `" & chr(34) & `"$blockExecutionTempPath\$scriptFileName`" & chr(34) & `" -ShowBlockedAppDialog -AsyncToolkitLaunch -ReferredInstallTitle `" & chr(34) & `"$installTitle`" & chr(34)"
        $debuggerBlockScript += 'set oWShell = CreateObject("WScript.Shell")'
        $debuggerBlockScript += 'oWShell.Run strCommand, 0, false'
        $debuggerBlockScript | Out-File -FilePath "$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs" -Force -Encoding 'Default' -ErrorAction 'SilentlyContinue'
        [String]$debuggerBlockValue = "$envWinDir\System32\wscript.exe `"$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs`""

        ## Set contents to be readable for all users (BUILTIN\USERS)
        Try {
            $Users = ConvertTo-NTAccountOrSID -SID 'S-1-5-32-545'
            Set-ItemPermission -Path $blockExecutionTempPath -User $Users -Permission 'Read' -Inheritance ('ObjectInherit', 'ContainerInherit')
        }
        Catch {
            Write-Log -Message "Failed to set read permissions on path [$blockExecutionTempPath]. The function might not be able to work correctly." -Source ${CmdletName} -Severity 2
        }

        ## Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
        Write-Log -Message 'Creating scheduled task to cleanup blocked applications in case the installation is interrupted.' -Source ${CmdletName}
        If (Get-SchedulerTask -ContinueOnError $true | Select-Object -Property 'TaskName' | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
            Write-Log -Message "Scheduled task [$schTaskBlockedAppsName] already exists." -Source ${CmdletName}
        }
        Else {
            ## Export the scheduled task XML to file
            Try {
                ## Specify the filename to export the XML to
                ## XML does not need to be user readable to stays in protected TEMP folder
                [String]$xmlSchTaskFilePath = "$dirAppDeployTemp\SchTaskUnBlockApps.xml"
                [String]$xmlUnblockAppsSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction 'Stop'
            }
            Catch {
                Write-Log -Message "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                Return
            }

            ## Import the Scheduled Task XML file to create the Scheduled Task
            [PSObject]$schTaskResult = Execute-Process -Path $exeSchTasks -Parameters "/create /f /tn $schTaskBlockedAppsName /xml `"$xmlSchTaskFilePath`"" -WindowStyle 'Hidden' -CreateNoWindow -PassThru -ExitOnProcessFailure $false
            If ($schTaskResult.ExitCode -ne 0) {
                Write-Log -Message "Failed to create the scheduled task [$schTaskBlockedAppsName] by importing the scheduled task XML file [$xmlSchTaskFilePath]." -Severity 3 -Source ${CmdletName}
                Return
            }
        }

        [String[]]$blockProcessName = $processName
        ## Append .exe to match registry keys
        [String[]]$blockProcessName = $blockProcessName | ForEach-Object { $_ + '.exe' } -ErrorAction 'SilentlyContinue'

        ## Enumerate each process and set the debugger value to block application execution
        ForEach ($blockProcess in $blockProcessName) {
            Write-Log -Message "Setting the Image File Execution Option registry key to block execution of [$blockProcess]." -Source ${CmdletName}
            Set-RegistryKey -Key (Join-Path -Path $regKeyAppExecution -ChildPath $blockProcess) -Name 'Debugger' -Value $debuggerBlockValue -ContinueOnError $true
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Unblock-AppExecution
Function Unblock-AppExecution {
    <#
.SYNOPSIS

Unblocks the execution of applications performed by the Block-AppExecution function

.DESCRIPTION

This function is called by the Exit-Script function or when the script itself is called with the parameters -CleanupBlockedApps

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Unblock-AppExecution

.NOTES

This is an internal script function and should typically not be called directly.

It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to undo the actions performed by Block-AppExecution.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Bypass if no Admin rights
        If ($configToolkitRequireAdmin -eq $false) {
            Write-Log -Message "Bypassing Function [${CmdletName}], because [Require Admin: $configToolkitRequireAdmin]." -Source ${CmdletName}
            Return
        }

        ## Remove Debugger values to unblock processes
        [PSObject[]]$unblockProcesses = $null
        [PSObject[]]$unblockProcesses += (Get-ChildItem -LiteralPath $regKeyAppExecution -Recurse -ErrorAction 'SilentlyContinue' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'SilentlyContinue' })
        ForEach ($unblockProcess in ($unblockProcesses | Where-Object { $_.Debugger -like '*AppDeployToolkit_BlockAppExecutionMessage*' })) {
            Write-Log -Message "Removing the Image File Execution Options registry key to unblock execution of [$($unblockProcess.PSChildName)]." -Source ${CmdletName}
            $unblockProcess | Remove-ItemProperty -Name 'Debugger' -ErrorAction 'SilentlyContinue'
        }

        ## If block execution variable is $true, set it to $false
        If ($BlockExecution) {
            #  Make this variable globally available so we can check whether we need to call Unblock-AppExecution
            Set-Variable -Name 'BlockExecution' -Value $false -Scope 'Script'
        }

        ## Remove the scheduled task if it exists
        [String]$schTaskBlockedAppsName = $installName + '_BlockedApps'
        Try {
            If (Get-SchedulerTask -ContinueOnError $true | Select-Object -Property 'TaskName' | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
                Write-Log -Message "Deleting Scheduled Task [$schTaskBlockedAppsName]." -Source ${CmdletName}
                Execute-Process -Path $exeSchTasks -Parameters "/Delete /TN $schTaskBlockedAppsName /F"
            }
        }
        Catch {
            Write-Log -Message "Error retrieving/deleting Scheduled Task.`r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Remove BlockAppExecution Schedule Task XML file
        [String]$xmlSchTaskFilePath = "$dirAppDeployTemp\SchTaskUnBlockApps.xml"
        If (Test-Path -LiteralPath $xmlSchTaskFilePath) {
            $null = Remove-Item -LiteralPath $xmlSchTaskFilePath -Force -ErrorAction 'SilentlyContinue'
        }

        ## Remove BlockAppExection Temporary directory
        [String]$blockExecutionTempPath = Join-Path -Path $dirAppDeployTemp -ChildPath 'BlockExecution'
        If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
            Remove-Folder -Path $blockExecutionTempPath
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-DeferHistory
Function Get-DeferHistory {
    <#
.SYNOPSIS

Get the history of deferrals from the registry for the current application, if it exists.

.DESCRIPTION

Get the history of deferrals from the registry for the current application, if it exists.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the history of deferrals from the registry for the current application, if it exists.

.EXAMPLE

Get-DeferHistory

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Write-Log -Message 'Getting deferral history...' -Source ${CmdletName}
        Get-RegistryKey -Key $regKeyDeferHistory -ContinueOnError $true
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Set-DeferHistory
Function Set-DeferHistory {
    <#
.SYNOPSIS

Set the history of deferrals in the registry for the current application.

.DESCRIPTION

Set the history of deferrals in the registry for the current application.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return any objects.

.EXAMPLE

Set-DeferHistory

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [String]$deferTimesRemaining,
        [Parameter(Mandatory = $false)]
        [String]$deferDeadline
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If ($deferTimesRemaining -and ($deferTimesRemaining -ge 0)) {
            Write-Log -Message "Setting deferral history: [DeferTimesRemaining = $deferTimesRemaining]." -Source ${CmdletName}
            Set-RegistryKey -Key $regKeyDeferHistory -Name 'DeferTimesRemaining' -Value $deferTimesRemaining -ContinueOnError $true
        }
        If ($deferDeadline) {
            Write-Log -Message "Setting deferral history: [DeferDeadline = $deferDeadline]." -Source ${CmdletName}
            Set-RegistryKey -Key $regKeyDeferHistory -Name 'DeferDeadline' -Value $deferDeadline -ContinueOnError $true
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-UniversalDate
Function Get-UniversalDate {
    <#
.SYNOPSIS

Returns the date/time for the local culture in a universal sortable date time pattern.

.DESCRIPTION

Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z

.PARAMETER DateTime

Specify the DateTime in the current culture.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the date/time for the local culture in a universal sortable date time pattern.

.EXAMPLE

Get-UniversalDate

Returns the current date in a universal sortable date time pattern.

.EXAMPLE

Get-UniversalDate -DateTime '25/08/2013'

Returns the date for the current culture in a universal sortable date time pattern.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        #  Get the current date
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$DateTime = ((Get-Date -Format ($culture).DateTimeFormat.UniversalDateTimePattern).ToString()),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## If a universal sortable date time pattern was provided, remove the Z, otherwise it could get converted to a different time zone.
            If ($DateTime -match 'Z$') {
                $DateTime = $DateTime -replace 'Z$', ''
            }
            [DateTime]$DateTime = [DateTime]::Parse($DateTime, $culture)

            ## Convert the date to a universal sortable date time pattern based on the current culture
            Write-Log -Message "Converting the date [$DateTime] to a universal sortable date time pattern based on the current culture [$($culture.Name)]." -Source ${CmdletName}
            [String]$universalDateTime = (Get-Date -Date $DateTime -Format ($culture).DateTimeFormat.UniversalSortableDateTimePattern -ErrorAction 'Stop').ToString()
            Write-Output -InputObject ($universalDateTime)
        }
        Catch {
            Write-Log -Message "The specified date/time [$DateTime] is not in a format recognized by the current culture [$($culture.Name)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "The specified date/time [$DateTime] is not in a format recognized by the current culture: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-RunningProcesses
Function Get-RunningProcesses {
    <#
.SYNOPSIS

Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

.DESCRIPTION

Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

.PARAMETER ProcessObjects

Custom object containing the process objects to search for. If not supplied, the function just returns $null

.PARAMETER DisableLogging

Disables function logging

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

Syste.Boolean.

Rettuns $true if the process is running, otherwise $false.

.EXAMPLE

Get-RunningProcesses -ProcessObjects $ProcessObjects

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0)]
        [PSObject[]]$ProcessObjects,
        [Parameter(Mandatory = $false, Position = 1)]
        [Switch]$DisableLogging
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If ($processObjects -and $processObjects[0].ProcessName) {
            [String]$runningAppsCheck = $processObjects.ProcessName -join ','
            If (-not $DisableLogging) {
                Write-Log -Message "Checking for running applications: [$runningAppsCheck]" -Source ${CmdletName}
            }
            ## Prepare a filter for Where-Object
            [ScriptBlock]$whereObjectFilter = {
                ForEach ($processObject in $processObjects) {
                    If ($_.ProcessName -ieq $processObject.ProcessName) {
                        If ($processObject.ProcessDescription) {
                            #  The description of the process provided as a Parameter to the function, e.g. -ProcessName "winword=Microsoft Office Word".
                            Add-Member -InputObject $_ -MemberType 'NoteProperty' -Name 'ProcessDescription' -Value $processObject.ProcessDescription -Force -PassThru -ErrorAction 'SilentlyContinue'
                        }
                        ElseIf ($_.Description) {
                            #  If the process already has a description field specified, then use it
                            Add-Member -InputObject $_ -MemberType 'NoteProperty' -Name 'ProcessDescription' -Value $_.Description -Force -PassThru -ErrorAction 'SilentlyContinue'
                        }
                        Else {
                            #  Fall back on the process name if no description is provided by the process or as a parameter to the function
                            Add-Member -InputObject $_ -MemberType 'NoteProperty' -Name 'ProcessDescription' -Value $_.ProcessName -Force -PassThru -ErrorAction 'SilentlyContinue'
                        }
                        Write-Output -InputObject ($true)
                        Return
                    }
                }

                Write-Output -InputObject ($false)
                Return
            }
            ## Get all running processes and escape special characters. Match against the process names to search for to find running processes.
            [Diagnostics.Process[]]$runningProcesses = Get-Process | Where-Object -FilterScript $whereObjectFilter | Sort-Object -Property 'ProcessName'

            If (-not $DisableLogging) {
                If ($runningProcesses) {
                    [String]$runningProcessList = ($runningProcesses.ProcessName | Select-Object -Unique) -join ','
                    Write-Log -Message "The following processes are running: [$runningProcessList]." -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message 'Specified applications are not running.' -Source ${CmdletName}
                }
            }
            Write-Output -InputObject ($runningProcesses)
        }
        Else {
            Write-Output -InputObject ($null)
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Show-InstallationWelcome
Function Show-InstallationWelcome {
    <#
.SYNOPSIS

Show a welcome dialog prompting the user with information about the installation and actions to be performed before the installation can begin.

.DESCRIPTION

The following prompts can be included in the welcome dialog:
	a) Close the specified running applications, or optionally close the applications without showing a prompt (using the -Silent switch).
	b) Defer the installation a certain number of times, for a certain number of days or until a deadline is reached.
	c) Countdown until applications are automatically closed.
	d) Prevent users from launching the specified applications while the installation is in progress.

Notes:
	The process descriptions are retrieved from WMI, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.
	The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

.PARAMETER CloseApps

Name of the process to stop (do not include the .exe). Specify multiple processes separated by a comma. Specify custom descriptions like this: "winword=Microsoft Office Word,excel=Microsoft Office Excel"

.PARAMETER Silent

Stop processes without prompting the user.

.PARAMETER CloseAppsCountdown

Option to provide a countdown in seconds until the specified applications are automatically closed. This only takes effect if deferral is not allowed or has expired.

.PARAMETER ForceCloseAppsCountdown

Option to provide a countdown in seconds until the specified applications are automatically closed regardless of whether deferral is allowed.

.PARAMETER PromptToSave

Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button. Option does not work in SYSTEM context unless toolkit launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

.PARAMETER PersistPrompt

Specify whether to make the Show-InstallationWelcome prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml. The user will have no option but to respond to the prompt. This only takes effect if deferral is not allowed or has expired.

.PARAMETER BlockExecution

Option to prevent the user from launching processes/applications, specified in -CloseApps, during the installation.

.PARAMETER AllowDefer

Enables an optional defer button to allow the user to defer the installation.

.PARAMETER AllowDeferCloseApps

Enables an optional defer button to allow the user to defer the installation only if there are running applications that need to be closed. This parameter automatically enables -AllowDefer

.PARAMETER DeferTimes

Specify the number of times the installation can be deferred.

.PARAMETER DeferDays

Specify the number of days since first run that the installation can be deferred. This is converted to a deadline.

.PARAMETER DeferDeadline

Specify the deadline date until which the installation can be deferred.

Specify the date in the local culture if the script is intended for that same culture.

If the script is intended to run on EN-US machines, specify the date in the format: "08/25/2013" or "08-25-2013" or "08-25-2013 18:00:00"

If the script is intended for multiple cultures, specify the date in the universal sortable date/time format: "2013-08-22 11:51:52Z"

The deadline date will be displayed to the user in the format of their culture.

.PARAMETER CheckDiskSpace

Specify whether to check if there is enough disk space for the installation to proceed.

If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.

.PARAMETER RequiredDiskSpace

Specify required disk space in MB, used in combination with CheckDiskSpace.

.PARAMETER MinimizeWindows

Specifies whether to minimize other windows when displaying prompt. Default: $true.

.PARAMETER TopMost

Specifies whether the windows is the topmost window. Default: $true.

.PARAMETER ForceCountdown

Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled.

.PARAMETER CustomText

Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return objects.

.EXAMPLE

Show-InstallationWelcome -CloseApps 'iexplore,winword,excel'

Prompt the user to close Internet Explorer, Word and Excel.

.EXAMPLE

Show-InstallationWelcome -CloseApps 'winword,excel' -Silent

Close Word and Excel without prompting the user.

.EXAMPLE

Show-InstallationWelcome -CloseApps 'winword,excel' -BlockExecution

Close Word and Excel and prevent the user from launching the applications while the installation is in progress.

.EXAMPLE

Show-InstallationWelcome -CloseApps 'winword=Microsoft Office Word,excel=Microsoft Office Excel' -CloseAppsCountdown 600

Prompt the user to close Word and Excel, with customized descriptions for the applications and automatically close the applications after 10 minutes.

.EXAMPLE

Show-InstallationWelcome -CloseApps 'winword,msaccess,excel' -PersistPrompt

Prompt the user to close Word, MSAccess and Excel.

By using the PersistPrompt switch, the dialog will return to the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml, so the user cannot ignore it by dragging it aside.

.EXAMPLE

Show-InstallationWelcome -AllowDefer -DeferDeadline '25/08/2013'

Allow the user to defer the installation until the deadline is reached.

.EXAMPLE

Show-InstallationWelcome -CloseApps 'winword,excel' -BlockExecution -AllowDefer -DeferTimes 10 -DeferDeadline '25/08/2013' -CloseAppsCountdown 600

Close Word and Excel and prevent the user from launching the applications while the installation is in progress.

Allow the user to defer the installation a maximum of 10 times or until the deadline is reached, whichever happens first.

When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParametersetName = 'None')]

    Param (
        ## Specify process names separated by commas. Optionally specify a process description with an equals symbol, e.g. "winword=Microsoft Office Word"
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$CloseApps,
        ## Specify whether to prompt user or force close the applications
        [Parameter(Mandatory = $false)]
        [Switch]$Silent = $false,
        ## Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$CloseAppsCountdown = 0,
        ## Specify a countdown to display before automatically closing applications whether or not deferral is allowed
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$ForceCloseAppsCountdown = 0,
        ## Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button
        [Parameter(Mandatory = $false)]
        [Switch]$PromptToSave = $false,
        ## Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml.
        [Parameter(Mandatory = $false)]
        [Switch]$PersistPrompt = $false,
        ## Specify whether to block execution of the processes during installation
        [Parameter(Mandatory = $false)]
        [Switch]$BlockExecution = $false,
        ## Specify whether to enable the optional defer button on the dialog box
        [Parameter(Mandatory = $false)]
        [Switch]$AllowDefer = $false,
        ## Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed
        [Parameter(Mandatory = $false)]
        [Switch]$AllowDeferCloseApps = $false,
        ## Specify the number of times the deferral is allowed
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$DeferTimes = 0,
        ## Specify the number of days since first run that the deferral is allowed
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$DeferDays = 0,
        ## Specify the deadline (in format dd/mm/yyyy) for which deferral will expire as an option
        [Parameter(Mandatory = $false)]
        [String]$DeferDeadline = '',
        ## Specify whether to check if there is enough disk space for the installation to proceed. If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.
        [Parameter(ParameterSetName = 'CheckDiskSpaceParameterSet', Mandatory = $true)]
        [ValidateScript({ $_.IsPresent -eq ($true -or $false) })]
        [Switch]$CheckDiskSpace,
        ## Specify required disk space in MB, used in combination with $CheckDiskSpace.
        [Parameter(ParameterSetName = 'CheckDiskSpaceParameterSet', Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$RequiredDiskSpace = 0,
        ## Specify whether to minimize other windows when displaying prompt
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$MinimizeWindows = $true,
        ## Specifies whether the window is the topmost window
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$TopMost = $true,
        ## Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$ForceCountdown = 0,
        ## Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.
        [Parameter(Mandatory = $false)]
        [Switch]$CustomText = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## If running in NonInteractive mode, force the processes to close silently
        If ($deployModeNonInteractive) {
            $Silent = $true
        }

        ## If using Zero-Config MSI Deployment, append any executables found in the MSI to the CloseApps list
        If ($useDefaultMsi) {
            $CloseApps = "$CloseApps,$defaultMsiExecutablesList"
        }

        ## Check disk space requirements if specified
        If ($CheckDiskSpace) {
            Write-Log -Message 'Evaluating disk space requirements.' -Source ${CmdletName}
            [Double]$freeDiskSpace = Get-FreeDiskSpace
            If ($RequiredDiskSpace -eq 0) {
                Try {
                    #  Determine the size of the Files folder
                    $fso = New-Object -ComObject 'Scripting.FileSystemObject' -ErrorAction 'Stop'
                    $RequiredDiskSpace = [Math]::Round((($fso.GetFolder($scriptParentPath).Size) / 1MB))
                }
                Catch {
                    Write-Log -Message "Failed to calculate disk space requirement from source files. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                }
                Finally {
                    Try {
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($fso)
                    }
                    Catch {
                    }
                }
            }
            If ($freeDiskSpace -lt $RequiredDiskSpace) {
                Write-Log -Message "Failed to meet minimum disk space requirement. Space Required [$RequiredDiskSpace MB], Space Available [$freeDiskSpace MB]." -Severity 3 -Source ${CmdletName}
                If (-not $Silent) {
                    Show-InstallationPrompt -Message ($configDiskSpaceMessage -f $installTitle, $RequiredDiskSpace, ($freeDiskSpace)) -ButtonRightText 'OK' -Icon 'Error'
                }
                Exit-Script -ExitCode $configInstallationUIExitCode
            }
            Else {
                Write-Log -Message 'Successfully passed minimum disk space requirement check.' -Source ${CmdletName}
            }
        }

        If ($CloseApps) {
            ## Create a Process object with custom descriptions where they are provided (split on an '=' sign)
            [PSObject[]]$processObjects = @()
            #  Split multiple processes on a comma, then split on equal sign, then create custom object with process name and description
            ForEach ($process in ($CloseApps -split ',' | Where-Object { $_ })) {
                If ($process.Contains('=')) {
                    [String[]]$ProcessSplit = $process -split '='
                    $processObjects += New-Object -TypeName 'PSObject' -Property @{
                        ProcessName        = $ProcessSplit[0]
                        ProcessDescription = $ProcessSplit[1]
                    }
                }
                Else {
                    [String]$ProcessInfo = $process
                    $processObjects += New-Object -TypeName 'PSObject' -Property @{
                        ProcessName        = $process
                        ProcessDescription = ''
                    }
                }
            }
        }

        ## Check Deferral history and calculate remaining deferrals
        If (($allowDefer) -or ($AllowDeferCloseApps)) {
            #  Set $allowDefer to true if $AllowDeferCloseApps is true
            $allowDefer = $true

            #  Get the deferral history from the registry
            $deferHistory = Get-DeferHistory
            $deferHistoryTimes = $deferHistory | Select-Object -ExpandProperty 'DeferTimesRemaining' -ErrorAction 'SilentlyContinue'
            $deferHistoryDeadline = $deferHistory | Select-Object -ExpandProperty 'DeferDeadline' -ErrorAction 'SilentlyContinue'

            #  Reset Switches
            $checkDeferDays = $false
            $checkDeferDeadline = $false
            If ($DeferDays -ne 0) {
                $checkDeferDays = $true
            }
            If ($DeferDeadline) {
                $checkDeferDeadline = $true
            }
            If ($DeferTimes -ne 0) {
                If ($deferHistoryTimes -ge 0) {
                    Write-Log -Message "Defer history shows [$($deferHistory.DeferTimesRemaining)] deferrals remaining." -Source ${CmdletName}
                    $DeferTimes = $deferHistory.DeferTimesRemaining - 1
                }
                Else {
                    $DeferTimes = $DeferTimes - 1
                }
                Write-Log -Message "The user has [$deferTimes] deferrals remaining." -Source ${CmdletName}
                If ($DeferTimes -lt 0) {
                    Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
                    $AllowDefer = $false
                }
            }
            Else {
                If (Test-Path -LiteralPath 'variable:deferTimes') {
                    Remove-Variable -Name 'deferTimes'
                }
                $DeferTimes = $null
            }
            If ($checkDeferDays -and $allowDefer) {
                If ($deferHistoryDeadline) {
                    Write-Log -Message "Defer history shows a deadline date of [$deferHistoryDeadline]." -Source ${CmdletName}
                    [String]$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferHistoryDeadline
                }
                Else {
                    [String]$deferDeadlineUniversal = Get-UniversalDate -DateTime (Get-Date -Date ((Get-Date).AddDays($deferDays)) -Format ($culture).DateTimeFormat.UniversalDateTimePattern).ToString()
                }
                Write-Log -Message "The user has until [$deferDeadlineUniversal] before deferral expires." -Source ${CmdletName}
                If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
                    Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
                    $AllowDefer = $false
                }
            }
            If ($checkDeferDeadline -and $allowDefer) {
                #  Validate Date
                Try {
                    [String]$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferDeadline -ErrorAction 'Stop'
                }
                Catch {
                    Write-Log -Message "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z'. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                    Throw "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z': $($_.Exception.Message)"
                }
                Write-Log -Message "The user has until [$deferDeadlineUniversal] remaining." -Source ${CmdletName}
                If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
                    Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
                    $AllowDefer = $false
                }
            }
        }
        If (($deferTimes -lt 0) -and (-not $deferDeadlineUniversal)) {
            $AllowDefer = $false
        }

        ## Prompt the user to close running applications and optionally defer if enabled
        If ((-not $deployModeSilent) -and (-not $silent)) {
            If ($forceCloseAppsCountdown -gt 0) {
                #  Keep the same variable for countdown to simplify the code:
                $closeAppsCountdown = $forceCloseAppsCountdown
                #  Change this variable to a boolean now to switch the countdown on even with deferral
                [Boolean]$forceCloseAppsCountdown = $true
            }
            ElseIf ($forceCountdown -gt 0) {
                #  Keep the same variable for countdown to simplify the code:
                $closeAppsCountdown = $forceCountdown
                #  Change this variable to a boolean now to switch the countdown on
                [Boolean]$forceCountdown = $true
            }
            Set-Variable -Name 'closeAppsCountdownGlobal' -Value $closeAppsCountdown -Scope 'Script'

            While ((Get-RunningProcesses -ProcessObjects $processObjects -OutVariable 'runningProcesses') -or (($promptResult -ne 'Defer') -and ($promptResult -ne 'Close'))) {
                [String]$runningProcessDescriptions = ($runningProcesses | Where-Object { $_.ProcessDescription } | Select-Object -ExpandProperty 'ProcessDescription' | Sort-Object -Unique) -join ','
                #  Check if we need to prompt the user to defer, to defer and close apps, or not to prompt them at all
                If ($allowDefer) {
                    #  If there is deferral and closing apps is allowed but there are no apps to be closed, break the while loop
                    If ($AllowDeferCloseApps -and (-not $runningProcessDescriptions)) {
                        Break
                    }
                    #  Otherwise, as long as the user has not selected to close the apps or the processes are still running and the user has not selected to continue, prompt user to close running processes with deferral
                    ElseIf (($promptResult -ne 'Close') -or (($runningProcessDescriptions) -and ($promptResult -ne 'Continue'))) {
                        [String]$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -ForceCloseAppsCountdown $forceCloseAppsCountdown -ForceCountdown $forceCountdown -PersistPrompt $PersistPrompt -AllowDefer -DeferTimes $deferTimes -DeferDeadline $deferDeadlineUniversal -MinimizeWindows $MinimizeWindows -CustomText:$CustomText -TopMost $TopMost
                    }
                }
                #  If there is no deferral and processes are running, prompt the user to close running processes with no deferral option
                ElseIf (($runningProcessDescriptions) -or ($forceCountdown)) {
                    [String]$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -ForceCloseAppsCountdown $forceCloseAppsCountdown -ForceCountdown $forceCountdown -PersistPrompt $PersistPrompt -MinimizeWindows $minimizeWindows -CustomText:$CustomText -TopMost $TopMost
                }
                #  If there is no deferral and no processes running, break the while loop
                Else {
                    Break
                }

                #  If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again
                If ($promptResult -eq 'Continue') {
                    Write-Log -Message 'The user selected to continue...' -Source ${CmdletName}
                    Start-Sleep -Seconds 2

                    #  Break the while loop if there are no processes to close and the user has clicked OK to continue
                    If (-not $runningProcesses) {
                        Break
                    }
                }
                #  Force the applications to close
                ElseIf ($promptResult -eq 'Close') {
                    Write-Log -Message 'The user selected to force the application(s) to close...' -Source ${CmdletName}
                    If (($PromptToSave) -and ($SessionZero -and (-not $IsProcessUserInteractive))) {
                        Write-Log -Message 'Specified [-PromptToSave] option will not be available, because current process is running in session zero and is not interactive.' -Severity 2 -Source ${CmdletName}
                    }
                    # Update the process list right before closing, in case it changed
                    $runningProcesses = Get-RunningProcesses -ProcessObjects $processObjects
                    # Close running processes
                    ForEach ($runningProcess in $runningProcesses) {
                        [PSObject[]]$AllOpenWindowsForRunningProcess = Get-WindowTitle -GetAllWindowTitles -DisableFunctionLogging | Where-Object { $_.ParentProcess -eq $runningProcess.ProcessName }
                        #  If the PromptToSave parameter was specified and the process has a window open, then prompt the user to save work if there is work to be saved when closing window
                        If (($PromptToSave) -and (-not ($SessionZero -and (-not $IsProcessUserInteractive))) -and ($AllOpenWindowsForRunningProcess) -and ($runningProcess.MainWindowHandle -ne [IntPtr]::Zero)) {
                            [Timespan]$PromptToSaveTimeout = New-TimeSpan -Seconds $configInstallationPromptToSave
                            [Diagnostics.StopWatch]$PromptToSaveStopWatch = [Diagnostics.StopWatch]::StartNew()
                            $PromptToSaveStopWatch.Reset()
                            ForEach ($OpenWindow in $AllOpenWindowsForRunningProcess) {
                                Try {
                                    Write-Log -Message "Stopping process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)] and prompt to save if there is work to be saved (timeout in [$configInstallationPromptToSave] seconds)..." -Source ${CmdletName}
                                    [Boolean]$IsBringWindowToFrontSuccess = [PSADT.UiAutomation]::BringWindowToFront($OpenWindow.WindowHandle)
                                    [Boolean]$IsCloseWindowCallSuccess = $runningProcess.CloseMainWindow()
                                    If (-not $IsCloseWindowCallSuccess) {
                                        Write-Log -Message "Failed to call the CloseMainWindow() method on process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)] because the main window may be disabled due to a modal dialog being shown." -Severity 3 -Source ${CmdletName}
                                    }
                                    Else {
                                        $PromptToSaveStopWatch.Start()
                                        Do {
                                            [Boolean]$IsWindowOpen = [Boolean](Get-WindowTitle -GetAllWindowTitles -DisableFunctionLogging | Where-Object { $_.WindowHandle -eq $OpenWindow.WindowHandle })
                                            If (-not $IsWindowOpen) {
                                                Break
                                            }
                                            Start-Sleep -Seconds 3
                                        } While (($IsWindowOpen) -and ($PromptToSaveStopWatch.Elapsed -lt $PromptToSaveTimeout))
                                        $PromptToSaveStopWatch.Reset()
                                        If ($IsWindowOpen) {
                                            Write-Log -Message "Exceeded the [$configInstallationPromptToSave] seconds timeout value for the user to save work associated with process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)]." -Severity 2 -Source ${CmdletName}
                                        }
                                        Else {
                                            Write-Log -Message "Window [$($OpenWindow.WindowTitle)] for process [$($runningProcess.ProcessName)] was successfully closed." -Source ${CmdletName}
                                        }
                                    }
                                }
                                Catch {
                                    Write-Log -Message "Failed to close window [$($OpenWindow.WindowTitle)] for process [$($runningProcess.ProcessName)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                                    Continue
                                }
                                Finally {
                                    $runningProcess.Refresh()
                                }
                            }
                        }
                        Else {
                            Write-Log -Message "Stopping process $($runningProcess.ProcessName)..." -Source ${CmdletName}
                            Stop-Process -Name $runningProcess.ProcessName -Force -ErrorAction 'SilentlyContinue'
                        }
                    }

                    If ($runningProcesses = Get-RunningProcesses -ProcessObjects $processObjects -DisableLogging) {
                        # Apps are still running, give them 2s to close. If they are still running, the Welcome Window will be displayed again
                        Write-Log -Message 'Sleeping for 2 seconds because the processes are still not closed...' -Source ${CmdletName}
                        Start-Sleep -Seconds 2
                    }
                }
                #  Stop the script (if not actioned before the timeout value)
                ElseIf ($promptResult -eq 'Timeout') {
                    Write-Log -Message 'Installation not actioned before the timeout value.' -Source ${CmdletName}
                    $BlockExecution = $false

                    If (($deferTimes -ge 0) -or ($deferDeadlineUniversal)) {
                        Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal
                    }
                    ## Dispose the welcome prompt timer here because if we dispose it within the Show-WelcomePrompt function we risk resetting the timer and missing the specified timeout period
                    If ($script:welcomeTimer) {
                        Try {
                            $script:welcomeTimer.Dispose()
                            $script:welcomeTimer = $null
                        }
                        Catch {
                        }
                    }

                    #  Restore minimized windows
                    $null = $shellApp.UndoMinimizeAll()

                    Exit-Script -ExitCode $configInstallationUIExitCode
                }
                #  Stop the script (user chose to defer)
                ElseIf ($promptResult -eq 'Defer') {
                    Write-Log -Message 'Installation deferred by the user.' -Source ${CmdletName}
                    $BlockExecution = $false

                    Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal

                    #  Restore minimized windows
                    $null = $shellApp.UndoMinimizeAll()

                    Exit-Script -ExitCode $configInstallationDeferExitCode
                }
            }
        }

        ## Force the processes to close silently, without prompting the user
        If (($Silent -or $deployModeSilent) -and $CloseApps) {
            [Array]$runningProcesses = $null
            [Array]$runningProcesses = Get-RunningProcesses $processObjects
            If ($runningProcesses) {
                [String]$runningProcessDescriptions = ($runningProcesses | Where-Object { $_.ProcessDescription } | Select-Object -ExpandProperty 'ProcessDescription' | Sort-Object -Unique) -join ','
                Write-Log -Message "Force closing application(s) [$($runningProcessDescriptions)] without prompting user." -Source ${CmdletName}
                $runningProcesses.ProcessName | ForEach-Object -Process { Stop-Process -Name $_ -Force -ErrorAction 'SilentlyContinue' }
                Start-Sleep -Seconds 2
            }
        }

        ## Force nsd.exe to stop if Notes is one of the required applications to close
        If (($processObjects | Select-Object -ExpandProperty 'ProcessName') -contains 'notes') {
            ## Get the path where Notes is installed
            [String]$notesPath = Get-Item -LiteralPath $regKeyLotusNotes -ErrorAction 'SilentlyContinue' | Get-ItemProperty | Select-Object -ExpandProperty 'Path'

            ## Ensure we aren't running as a Local System Account and Notes install directory was found
            If ((-not $IsLocalSystemAccount) -and ($notesPath)) {
                #  Get a list of all the executables in the Notes folder
                [string[]]$notesPathExes = Get-ChildItem -LiteralPath $notesPath -Filter '*.exe' -Recurse | Select-Object -ExpandProperty 'BaseName' | Sort-Object
                ## Check for running Notes executables and run NSD if any are found
                $notesPathExes | ForEach-Object {
                    If ((Get-Process | Select-Object -ExpandProperty 'Name') -contains $_) {
                        [String]$notesNSDExecutable = Join-Path -Path $notesPath -ChildPath 'NSD.exe'
                        Try {
                            If (Test-Path -LiteralPath $notesNSDExecutable -PathType 'Leaf' -ErrorAction 'Stop') {
                                Write-Log -Message "Executing [$notesNSDExecutable] with the -kill argument..." -Source ${CmdletName}
                                [Diagnostics.Process]$notesNSDProcess = Start-Process -FilePath $notesNSDExecutable -ArgumentList '-kill' -WindowStyle 'Hidden' -PassThru -ErrorAction 'SilentlyContinue'

                                If (-not $notesNSDProcess.WaitForExit(10000)) {
                                    Write-Log -Message "[$notesNSDExecutable] did not end in a timely manner. Force terminate process." -Source ${CmdletName}
                                    Stop-Process -Name 'NSD' -Force -ErrorAction 'SilentlyContinue'
                                }
                            }
                        }
                        Catch {
                            Write-Log -Message "Failed to launch [$notesNSDExecutable]. `r`n$(Resolve-Error)" -Source ${CmdletName}
                        }

                        Write-Log -Message "[$notesNSDExecutable] returned exit code [$($notesNSDProcess.ExitCode)]." -Source ${CmdletName}

                        #  Force NSD process to stop in case the previous command was not successful
                        Stop-Process -Name 'NSD' -Force -ErrorAction 'SilentlyContinue'
                    }
                }
            }

            #  Strip all Notes processes from the process list except notes.exe, because the other notes processes (e.g. notes2.exe) may be invoked by the Notes installation, so we don't want to block their execution.
            If ($notesPathExes) {
                [Array]$processesIgnoringNotesExceptions = Compare-Object -ReferenceObject ($processObjects | Select-Object -ExpandProperty 'ProcessName' | Sort-Object) -DifferenceObject $notesPathExes -IncludeEqual | Where-Object { ($_.SideIndicator -eq '<=') -or ($_.InputObject -eq 'notes') } | Select-Object -ExpandProperty 'InputObject'
                [Array]$processObjects = $processObjects | Where-Object { $processesIgnoringNotesExceptions -contains $_.ProcessName }
            }
        }

        ## If block execution switch is true, call the function to block execution of these processes
        If ($BlockExecution) {
            #  Make this variable globally available so we can check whether we need to call Unblock-AppExecution
            Set-Variable -Name 'BlockExecution' -Value $BlockExecution -Scope 'Script'
            Write-Log -Message '[-BlockExecution] parameter specified.' -Source ${CmdletName}
            Block-AppExecution -ProcessName ($processObjects | Select-Object -ExpandProperty 'ProcessName')
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Show-WelcomePrompt
Function Show-WelcomePrompt {
    <#
.SYNOPSIS

Called by Show-InstallationWelcome to prompt the user to optionally do the following:
	1) Close the specified running applications.
	2) Provide an option to defer the installation.
	3) Show a countdown before applications are automatically closed.

.DESCRIPTION

The user is presented with a Windows Forms dialog box to close the applications themselves and continue or to have the script close the applications for them.
If the -AllowDefer option is set to true, an optional "Defer" button will be shown to the user. If they select this option, the script will exit and return a 1618 code (SCCM fast retry code).
The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

.PARAMETER ProcessDescriptions

The descriptive names of the applications that are running and need to be closed.

.PARAMETER CloseAppsCountdown

Specify the countdown time in seconds before running applications are automatically closed when deferral is not allowed or expired.

.PARAMETER ForceCloseAppsCountdown

Specify whether to show the countdown regardless of whether deferral is allowed.

.PARAMETER PersistPrompt

Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml.

.PARAMETER AllowDefer

Specify whether to provide an option to defer the installation.

.PARAMETER DeferTimes

Specify the number of times the user is allowed to defer.

.PARAMETER DeferDeadline

Specify the deadline date before the user is allowed to defer.

.PARAMETER MinimizeWindows

Specifies whether to minimize other windows when displaying prompt. Default: $true.

.PARAMETER TopMost

Specifies whether the windows is the topmost window. Default: $true.

.PARAMETER ForceCountdown

Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled.

.PARAMETER CustomText

Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the user's selection.

.EXAMPLE

Show-WelcomePrompt -ProcessDescriptions 'Lotus Notes, Microsoft Word' -CloseAppsCountdown 600 -AllowDefer -DeferTimes 10

.NOTES

This is an internal script function and should typically not be called directly. It is used by the Show-InstallationWelcome prompt to display a custom prompt.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [String]$ProcessDescriptions,
        [Parameter(Mandatory = $false)]
        [Int32]$CloseAppsCountdown,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ForceCloseAppsCountdown,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$PersistPrompt = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$AllowDefer = $false,
        [Parameter(Mandatory = $false)]
        [String]$DeferTimes,
        [Parameter(Mandatory = $false)]
        [String]$DeferDeadline,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$MinimizeWindows = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$TopMost = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$ForceCountdown = 0,
        [Parameter(Mandatory = $false)]
        [Switch]$CustomText = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Reset switches
        [Boolean]$showCloseApps = $false
        [Boolean]$showDefer = $false
        [Boolean]$persistWindow = $false

        ## Reset times
        [DateTime]$startTime = Get-Date
        [DateTime]$countdownTime = $startTime

        ## Check if the countdown was specified
        If ($CloseAppsCountdown -and ($CloseAppsCountdown -gt $configInstallationUITimeout)) {
            Throw 'The close applications countdown time cannot be longer than the timeout specified in the XML configuration for installation UI dialogs to timeout.'
        }

        ## Initial form layout: Close Applications / Allow Deferral
        If ($processDescriptions) {
            Write-Log -Message "Prompting the user to close application(s) [$processDescriptions]..." -Source ${CmdletName}
            $showCloseApps = $true
        }
        If (($allowDefer) -and (($deferTimes -ge 0) -or ($deferDeadline))) {
            Write-Log -Message 'The user has the option to defer.' -Source ${CmdletName}
            $showDefer = $true
            If ($deferDeadline) {
                #  Remove the Z from universal sortable date time format, otherwise it could be converted to a different time zone
                $deferDeadline = $deferDeadline -replace 'Z', ''
                #  Convert the deadline date to a string
                [String]$deferDeadline = (Get-Date -Date $deferDeadline).ToString()
            }
        }

        ## If deferral is being shown and 'close apps countdown' or 'persist prompt' was specified, enable those features.
        If (-not $showDefer) {
            If ($closeAppsCountdown -gt 0) {
                Write-Log -Message "Close applications countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
                $showCountdown = $true
            }
        }
        Else {
            If ($persistPrompt) {
                $persistWindow = $true
            }
        }
        ## If 'force close apps countdown' was specified, enable that feature.
        If ($forceCloseAppsCountdown -eq $true) {
            Write-Log -Message "Close applications countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
            $showCountdown = $true
        }
        ## If 'force countdown' was specified, enable that feature.
        If ($forceCountdown -eq $true) {
            Write-Log -Message "Countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
            $showCountdown = $true
        }

        [String[]]$processDescriptions = $processDescriptions -split ','
        [Windows.Forms.Application]::EnableVisualStyles()

        $formWelcome = New-Object -TypeName 'System.Windows.Forms.Form'
        $pictureBanner = New-Object -TypeName 'System.Windows.Forms.PictureBox'
        $labelWelcomeMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelAppName = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCustomMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCloseAppsMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCountdownMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCountdown = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelDefer = New-Object -TypeName 'System.Windows.Forms.Label'
        $listBoxCloseApps = New-Object -TypeName 'System.Windows.Forms.ListBox'
        $buttonContinue = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonDefer = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonCloseApps = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonAbort = New-Object -TypeName 'System.Windows.Forms.Button'
        $flowLayoutPanel = New-Object -TypeName 'System.Windows.Forms.FlowLayoutPanel'
        $panelButtons = New-Object -TypeName 'System.Windows.Forms.Panel'
        $toolTip = New-Object -TypeName 'System.Windows.Forms.ToolTip'

        ## Remove all event handlers from the controls
        [ScriptBlock]$Welcome_Form_Cleanup_FormClosed = {
            Try {
                $labelWelcomeMessage.remove_Click($handler_labelWelcomeMessage_Click)
                $labelAppName.remove_Click($handler_labelAppName_Click)
                $labelCustomMessage.remove_Click($handler_labelCustomMessage_Click)
                $labelCloseAppsMessage.remove_Click($handler_labelCloseAppsMessage_Click)
                $labelDefer.remove_Click($handler_labelDefer_Click)
                $labelCountdownMessage.remove_Click($handler_labelCountdownMessage_Click)
                $buttonCloseApps.remove_Click($buttonCloseApps_OnClick)
                $buttonContinue.remove_Click($buttonContinue_OnClick)
                $buttonDefer.remove_Click($buttonDefer_OnClick)
                $buttonAbort.remove_Click($buttonAbort_OnClick)
                $script:welcomeTimer.remove_Tick($welcomeTimer_Tick)
                $welcomeTimerPersist.remove_Tick($welcomeTimerPersist_Tick)
                $timerRunningProcesses.remove_Tick($timerRunningProcesses_Tick)
                $formWelcome.remove_Load($Welcome_Form_StateCorrection_Load)
                $formWelcome.remove_FormClosed($Welcome_Form_Cleanup_FormClosed)
            }
            Catch {
            }
        }

        [ScriptBlock]$Welcome_Form_StateCorrection_Load = {
            # Disable the X button
            Try {
                $windowHandle = $formWelcome.Handle
                If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
                    $menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
                    If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
                        [PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
                        [PSADT.UiAutomation]::DestroyMenu($menuHandle)
                    }
                }
            }
            Catch {
                # Not a terminating error if we can't disable the button. Just disable the Control Box instead
                Write-Log 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2 -Source ${CmdletName}
                $formWelcome.ControlBox = $false
            }
            ## Correct the initial state of the form to prevent the .NET maximized form issue
            $formWelcome.WindowState = 'Normal'
            $formWelcome.AutoSize = $true
            $formWelcome.AutoScaleMode = 'Font'
            $formWelcome.AutoScaleDimensions = New-Object System.Drawing.SizeF(6, 13) #Set as if using 96 DPI
            $formWelcome.TopMost = $TopMost
            $formWelcome.BringToFront()
            #  Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
            Set-Variable -Name 'formWelcomeStartPosition' -Value $formWelcome.Location -Scope 'Script'

            ## Initialize the countdown timer
            [DateTime]$currentTime = Get-Date
            [DateTime]$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
            $script:welcomeTimer.Start()

            ## Set up the form
            [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
            $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
        }

        ## Add the timer if it doesn't already exist - this avoids the timer being reset if the continue button is clicked
        If (-not $script:welcomeTimer) {
            $script:welcomeTimer = New-Object -TypeName 'System.Windows.Forms.Timer'
        }

        If ($showCountdown) {
            [ScriptBlock]$welcomeTimer_Tick = {
                ## Get the time information
                [DateTime]$currentTime = Get-Date
                [DateTime]$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
                [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
                Set-Variable -Name 'closeAppsCountdownGlobal' -Value $remainingTime.TotalSeconds -Scope 'Script'

                ## If the countdown is complete, close the application(s) or continue
                If ($countdownTime -le $currentTime) {
                    If ($forceCountdown -eq $true) {
                        Write-Log -Message 'Countdown timer has elapsed. Force continue.' -Source ${CmdletName}
                        $buttonContinue.PerformClick()
                    }
                    Else {
                        Write-Log -Message 'Close application(s) countdown timer has elapsed. Force closing application(s).' -Source ${CmdletName}
                        If ($buttonCloseApps.CanFocus) {
                            $buttonCloseApps.PerformClick()
                        }
                        Else {
                            $buttonContinue.PerformClick()
                        }
                    }
                }
                Else {
                    #  Update the form
                    $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
                }
            }
        }
        Else {
            $script:welcomeTimer.Interval = ($configInstallationUITimeout * 1000)
            [ScriptBlock]$welcomeTimer_Tick = { $buttonAbort.PerformClick() }
        }

        $script:welcomeTimer.add_Tick($welcomeTimer_Tick)

        ## Persistence Timer
        If ($persistWindow) {
            $welcomeTimerPersist = New-Object -TypeName 'System.Windows.Forms.Timer'
            $welcomeTimerPersist.Interval = ($configInstallationPersistInterval * 1000)
            [ScriptBlock]$welcomeTimerPersist_Tick = {
                $formWelcome.WindowState = 'Normal'
                $formWelcome.TopMost = $TopMost
                $formWelcome.BringToFront()
                $formWelcome.Location = "$($formWelcomeStartPosition.X),$($formWelcomeStartPosition.Y)"
            }
            $welcomeTimerPersist.add_Tick($welcomeTimerPersist_Tick)
            $welcomeTimerPersist.Start()
        }

        ## Process Re-Enumeration Timer
        If ($configInstallationWelcomePromptDynamicRunningProcessEvaluation) {
            $timerRunningProcesses = New-Object -TypeName 'System.Windows.Forms.Timer'
            $timerRunningProcesses.Interval = ($configInstallationWelcomePromptDynamicRunningProcessEvaluationInterval * 1000)
            [ScriptBlock]$timerRunningProcesses_Tick = {
                Try {
                    $dynamicRunningProcesses = $null
                    $dynamicRunningProcesses = Get-RunningProcesses -ProcessObjects $processObjects -DisableLogging
                    [String]$dynamicRunningProcessDescriptions = ($dynamicRunningProcesses | Where-Object { $_.ProcessDescription } | Select-Object -ExpandProperty 'ProcessDescription' | Sort-Object -Unique) -join ','
                    If ($dynamicRunningProcessDescriptions -ne $script:runningProcessDescriptions) {
                        # Update the runningProcessDescriptions variable for the next time this function runs
                        Set-Variable -Name 'runningProcessDescriptions' -Value $dynamicRunningProcessDescriptions -Force -Scope 'Script'
                        If ($dynamicRunningProcesses) {
                            Write-Log -Message "The running processes have changed. Updating the apps to close: [$script:runningProcessDescriptions]..." -Source ${CmdletName}
                        }
                        # Update the list box with the processes to close
                        $listboxCloseApps.Items.Clear()
                        $script:runningProcessDescriptions -split ',' | ForEach-Object { $null = $listboxCloseApps.Items.Add($_) }
                    }
                    # If CloseApps processes were running when the prompt was shown, and they are subsequently detected to be closed while the form is showing, then close the form. The deferral and CloseApps conditions will be re-evaluated.
                    If ($ProcessDescriptions) {
                        If (-not $dynamicRunningProcesses) {
                            Write-Log -Message 'Previously detected running processes are no longer running.' -Source ${CmdletName}
                            $formWelcome.Dispose()
                        }
                    }
                    # If CloseApps processes were not running when the prompt was shown, and they are subsequently detected to be running while the form is showing, then close the form for relaunch. The deferral and CloseApps conditions will be re-evaluated.
                    Else {
                        If ($dynamicRunningProcesses) {
                            Write-Log -Message 'New running processes detected. Updating the form to prompt to close the running applications.' -Source ${CmdletName}
                            $formWelcome.Dispose()
                        }
                    }
                }
                Catch {
                }
            }
            $timerRunningProcesses.add_Tick($timerRunningProcesses_Tick)
            $timerRunningProcesses.Start()
        }

        ## Form

        ##----------------------------------------------
        ## Create zero px padding object
        $paddingNone = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 0)
        ## Create basic control size
        $defaultControlSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 0)

        ## Generic Button properties
        $buttonSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (130, 24)

        ## Picture Banner
        $pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
        $pictureBanner.ImageLocation = $appDeployLogoBanner
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
        $pictureBanner.Location = $System_Drawing_Point
        $pictureBanner.Name = 'pictureBanner'
        $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, $appDeployLogoBannerHeight)
        $pictureBanner.Size = $System_Drawing_Size
        $pictureBanner.SizeMode = 'CenterImage'
        $pictureBanner.Margin = $paddingNone
        $pictureBanner.TabStop = $false

        ## Label Welcome Message
        $labelWelcomeMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelWelcomeMessage.Font = $defaultFont
        $labelWelcomeMessage.Name = 'labelWelcomeMessage'
        $labelWelcomeMessage.Size = $defaultControlSize
        $labelWelcomeMessage.MinimumSize = $defaultControlSize
        $labelWelcomeMessage.MaximumSize = $defaultControlSize
        $labelWelcomeMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 0)
        $labelWelcomeMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelWelcomeMessage.TabStop = $false
        $labelWelcomeMessage.Text = $configDeferPromptWelcomeMessage
        $labelWelcomeMessage.TextAlign = 'MiddleCenter'
        $labelWelcomeMessage.Anchor = 'Top'
        $labelWelcomeMessage.AutoSize = $true
        $labelWelcomeMessage.add_Click($handler_labelWelcomeMessage_Click)

        ## Label App Name
        $labelAppName.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelAppName.Font = "$($defaultFont.Name), $($defaultFont.Size + 2), style=Bold"
        $labelAppName.Name = 'labelAppName'
        $labelAppName.Size = $defaultControlSize
        $labelAppName.MinimumSize = $defaultControlSize
        $labelAppName.MaximumSize = $defaultControlSize
        $labelAppName.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 5, 0, 5)
        $labelAppName.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelAppName.TabStop = $false
        $labelAppName.Text = $installTitle
        $labelAppName.TextAlign = 'MiddleCenter'
        $labelAppName.Anchor = 'Top'
        $labelAppName.AutoSize = $true
        $labelAppName.add_Click($handler_labelAppName_Click)

        ## Label CustomMessage
        $labelCustomMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCustomMessage.Font = $defaultFont
        $labelCustomMessage.Name = 'labelCustomMessage'
        $labelCustomMessage.Size = $defaultControlSize
        $labelCustomMessage.MinimumSize = $defaultControlSize
        $labelCustomMessage.MaximumSize = $defaultControlSize
        $labelCustomMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 5)
        $labelCustomMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCustomMessage.TabStop = $false
        $labelCustomMessage.Text = $configClosePromptMessage
        $labelCustomMessage.TextAlign = 'MiddleCenter'
        $labelCustomMessage.Anchor = 'Top'
        $labelCustomMessage.AutoSize = $true
        $labelCustomMessage.add_Click($handler_labelCustomMessage_Click)

        ## Label CloseAppsMessage
        $labelCloseAppsMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCloseAppsMessage.Font = $defaultFont
        $labelCloseAppsMessage.Name = 'labelCloseAppsMessage'
        $labelCloseAppsMessage.Size = $defaultControlSize
        $labelCloseAppsMessage.MinimumSize = $defaultControlSize
        $labelCloseAppsMessage.MaximumSize = $defaultControlSize
        $labelCloseAppsMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 5)
        $labelCloseAppsMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCloseAppsMessage.TabStop = $false
        $labelCloseAppsMessage.Text = $configClosePromptMessage
        $labelCloseAppsMessage.TextAlign = 'MiddleCenter'
        $labelCloseAppsMessage.Anchor = 'Top'
        $labelCloseAppsMessage.AutoSize = $true
        $labelCloseAppsMessage.add_Click($handler_labelCloseAppsMessage_Click)

        ## Listbox Close Applications
        $listBoxCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
        $listboxCloseApps.Font = $defaultFont
        $listBoxCloseApps.FormattingEnabled = $true
        $listBoxCloseApps.HorizontalScrollbar = $true
        $listBoxCloseApps.Name = 'listBoxCloseApps'
        $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (420, 100)
        $listBoxCloseApps.Size = $System_Drawing_Size
        $listBoxCloseApps.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (15, 0, 15, 0)
        $listBoxCloseApps.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $listBoxCloseApps.TabIndex = 3
        $ProcessDescriptions | ForEach-Object { $null = $listboxCloseApps.Items.Add($_) }

        ## Label Defer
        $labelDefer.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelDefer.Font = $defaultFont
        $labelDefer.Name = 'labelDefer'
        $labelDefer.Size = $defaultControlSize
        $labelDefer.MinimumSize = $defaultControlSize
        $labelDefer.MaximumSize = $defaultControlSize
        $labelDefer.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 5)
        $labelDefer.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelDefer.TabStop = $false
        $deferralText = "$configDeferPromptExpiryMessage`r`n"

        If ($deferTimes -ge 0) {
            $deferralText = "$deferralText `r`n$configDeferPromptRemainingDeferrals $([Int32]$deferTimes + 1)"
        }
        If ($deferDeadline) {
            $deferralText = "$deferralText `r`n$configDeferPromptDeadline $deferDeadline"
        }
        If (($deferTimes -lt 0) -and (-not $DeferDeadline)) {
            $deferralText = "$deferralText `r`n$configDeferPromptNoDeadline"
        }
        $deferralText = "$deferralText `r`n`r`n$configDeferPromptWarningMessage"
        $labelDefer.Text = $deferralText
        $labelDefer.TextAlign = 'MiddleCenter'
        $labelDefer.AutoSize = $true
        $labelDefer.add_Click($handler_labelDefer_Click)

        ## Label CountdownMessage
        $labelCountdownMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCountdownMessage.Name = 'labelCountdownMessage'
        $labelCountdownMessage.Font = "$($defaultFont.Name), $($defaultFont.Size + 2), style=Regular"
        $labelCountdownMessage.Size = $defaultControlSize
        $labelCountdownMessage.MinimumSize = $defaultControlSize
        $labelCountdownMessage.MaximumSize = $defaultControlSize
        $labelCountdownMessage.Margin = $paddingNone
        $labelCountdownMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCountdownMessage.TabStop = $false
        If (($forceCountdown -eq $true) -or (-not $script:runningProcessDescriptions)) {
            Switch ($deploymentType) {
                'Uninstall' {
                    $labelCountdownMessage.Text = ($configWelcomePromptCountdownMessage -f $configDeploymentTypeUninstall); Break
                }
                'Repair' {
                    $labelCountdownMessage.Text = ($configWelcomePromptCountdownMessage -f $configDeploymentTypeRepair); Break
                }
                Default {
                    $labelCountdownMessage.Text = ($configWelcomePromptCountdownMessage -f $configDeploymentTypeInstall); Break
                }
            }
        }
        Else {
            $labelCountdownMessage.Text = $configClosePromptCountdownMessage
        }
        $labelCountdownMessage.TextAlign = 'MiddleCenter'
        $labelCountdownMessage.Anchor = 'Top'
        $labelCountdownMessage.AutoSize = $true
        $labelCountdownMessage.add_Click($handler_labelCountdownMessage_Click)

        ## Label Countdown
        $labelCountdown.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCountdown.Name = 'labelCountdown'
        $labelCountdown.Font = "$($defaultFont.Name), $($defaultFont.Size + 9), style=Bold"
        $labelCountdown.Size = $defaultControlSize
        $labelCountdown.MinimumSize = $defaultControlSize
        $labelCountdown.MaximumSize = $defaultControlSize
        $labelCountdown.Margin = $paddingNone
        $labelCountdown.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCountdown.TabStop = $false
        $labelCountdown.Text = '00:00:00'
        $labelCountdown.TextAlign = 'MiddleCenter'
        $labelCountdown.AutoSize = $true
        $labelCountdown.add_Click($handler_labelDefer_Click)

        ## Panel Flow Layout
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, $appDeployLogoBannerHeight)
        $flowLayoutPanel.Location = $System_Drawing_Point
        $flowLayoutPanel.MinimumSize = $DefaultControlSize
        $flowLayoutPanel.MaximumSize = $DefaultControlSize
        $flowLayoutPanel.Size = $DefaultControlSize
        $flowLayoutPanel.Margin = $paddingNone
        $flowLayoutPanel.Padding = $paddingNone
        $flowLayoutPanel.AutoSizeMode = 'GrowAndShrink'
        $flowLayoutPanel.AutoSize = $true
        $flowLayoutPanel.Anchor = 'Top'
        $flowLayoutPanel.FlowDirection = 'TopDown'
        $flowLayoutPanel.WrapContents = $true
        $flowLayoutPanel.Controls.Add($labelWelcomeMessage)
        $flowLayoutPanel.Controls.Add($labelAppName)

        If ($CustomText -and $configWelcomePromptCustomMessage) {
            $labelCustomMessage.Text = $configWelcomePromptCustomMessage
            $flowLayoutPanel.Controls.Add($labelCustomMessage)
        }
        If ($showCloseApps) {
            $flowLayoutPanel.Controls.Add($labelCloseAppsMessage)
            $flowLayoutPanel.Controls.Add($listBoxCloseApps)
        }
        If ($showDefer) {
            $flowLayoutPanel.Controls.Add($labelDefer)
        }
        If ($showCountdown) {
            $flowLayoutPanel.Controls.Add($labelCountdownMessage)
            $flowLayoutPanel.Controls.Add($labelCountdown)
        }

        ## Button Close For Me
        $buttonCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonCloseApps.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (14, 4)
        $buttonCloseApps.Font = $defaultFont
        $buttonCloseApps.Name = 'buttonCloseApps'
        $buttonCloseApps.Size = $buttonSize
        $buttonCloseApps.MinimumSize = $buttonSize
        $buttonCloseApps.MaximumSize = $buttonSize
        $buttonCloseApps.TabIndex = 1
        $buttonCloseApps.Text = $configClosePromptButtonClose
        $buttonCloseApps.DialogResult = 'Yes'
        $buttonCloseApps.AutoSize = $true
        $buttonCloseApps.Margin = $paddingNone
        $buttonCloseApps.Padding = $paddingNone
        $buttonCloseApps.UseVisualStyleBackColor = $true
        $buttonCloseApps.add_Click($buttonCloseApps_OnClick)

        ## Button Defer
        $buttonDefer.DataBindings.DefaultDataSourceUpdateMode = 0
        If (-not $showCloseApps) {
            $buttonDefer.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (14, 4)
        }
        Else {
            $buttonDefer.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (160, 4)
        }
        $buttonDefer.Name = 'buttonDefer'
        $buttonDefer.Font = $defaultFont
        $buttonDefer.Size = $buttonSize
        $buttonDefer.MinimumSize = $buttonSize
        $buttonDefer.MaximumSize = $buttonSize
        $buttonDefer.TabIndex = 0
        $buttonDefer.Text = $configClosePromptButtonDefer
        $buttonDefer.DialogResult = 'No'
        $buttonDefer.AutoSize = $true
        $buttonDefer.Margin = $paddingNone
        $buttonDefer.Padding = $paddingNone
        $buttonDefer.UseVisualStyleBackColor = $true
        $buttonDefer.add_Click($buttonDefer_OnClick)

        ## Button Continue
        $buttonContinue.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonContinue.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (306, 4)
        $buttonContinue.Name = 'buttonContinue'
        $buttonContinue.Font = $defaultFont
        $buttonContinue.Size = $buttonSize
        $buttonContinue.MinimumSize = $buttonSize
        $buttonContinue.MaximumSize = $buttonSize
        $buttonContinue.TabIndex = 2
        $buttonContinue.Text = $configClosePromptButtonContinue
        $buttonContinue.DialogResult = 'OK'
        $buttonContinue.AutoSize = $true
        $buttonContinue.Margin = $paddingNone
        $buttonContinue.Padding = $paddingNone
        $buttonContinue.UseVisualStyleBackColor = $true
        $buttonContinue.add_Click($buttonContinue_OnClick)
        If ($showCloseApps) {
            #  Add tooltip to Continue button
            $toolTip.BackColor = [Drawing.Color]::LightGoldenrodYellow
            $toolTip.IsBalloon = $false
            $toolTip.InitialDelay = 100
            $toolTip.ReshowDelay = 100
            $toolTip.SetToolTip($buttonContinue, $configClosePromptButtonContinueTooltip)
        }

        ## Button Abort (Hidden)
        $buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonAbort.Name = 'buttonAbort'
        $buttonAbort.Font = $defaultFont
        $buttonAbort.Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (0, 0)
        $buttonAbort.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (0, 0)
        $buttonAbort.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (0, 0)
        $buttonAbort.BackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.ForeColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.BorderSize = 0
        $buttonAbort.FlatAppearance.MouseDownBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.MouseOverBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatStyle = [System.Windows.Forms.FlatStyle]::System
        $buttonAbort.TabStop = $false
        $buttonAbort.DialogResult = 'Abort'
        $buttonAbort.Visible = $true # Has to be set visible so we can call Click on it
        $buttonAbort.Margin = $paddingNone
        $buttonAbort.Padding = $paddingNone
        $buttonAbort.UseVisualStyleBackColor = $true
        $buttonAbort.add_Click($buttonAbort_OnClick)

        ## Form Welcome
        $formWelcome.Size = $defaultControlSize
        $formWelcome.MinimumSize = $defaultControlSize
        $formWelcome.Padding = $paddingNone
        $formWelcome.Margin = $paddingNone
        $formWelcome.DataBindings.DefaultDataSourceUpdateMode = 0
        $formWelcome.Name = 'WelcomeForm'
        $formWelcome.Text = $installTitle
        $formWelcome.StartPosition = 'CenterScreen'
        # $formWelcome.FormBorderStyle = 'FixedDialog'
        $formWelcome.MaximizeBox = $false
        $formWelcome.MinimizeBox = $false
        $formWelcome.TopMost = $TopMost
        $formWelcome.TopLevel = $true
        $formWelcome.Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
        $formWelcome.AutoSize = $true
        $formWelcome.AutoScaleMode = 'Font'
        $formWelcome.AutoScaleDimensions = New-Object System.Drawing.SizeF(6, 13) #Set as if using 96 DPI
        $formWelcome.Controls.Add($pictureBanner)
        $formWelcome.Controls.Add($buttonAbort)
        ## Panel Button
        $panelButtons.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.AutoSize = $true
        $panelButtons.Padding = $paddingNone
        $panelButtons.Margin = $paddingNone
        If ($showCloseApps) {
            $panelButtons.Controls.Add($buttonCloseApps)
        }
        If ($showDefer) {
            $panelButtons.Controls.Add($buttonDefer)
        }
        $panelButtons.Controls.Add($buttonContinue)

        ## Add the Buttons Panel to the flowPanel
        $flowLayoutPanel.Controls.Add($panelButtons)
        ## Add FlowPanel to the form
        $formWelcome.Controls.Add($flowLayoutPanel)
        #  Init the OnLoad event to correct the initial state of the form
        $formWelcome.add_Load($Welcome_Form_StateCorrection_Load)
        #  Clean up the control events
        $formWelcome.add_FormClosed($Welcome_Form_Cleanup_FormClosed)

        ## Minimize all other windows
        If ($minimizeWindows) {
            $null = $shellApp.MinimizeAll()
        }

        ## Show the form
        $result = $formWelcome.ShowDialog()
        $formWelcome.Dispose()

        Switch ($result) {
            OK {
                $result = 'Continue'
            }
            No {
                $result = 'Defer'
            }
            Yes {
                $result = 'Close'
            }
            Abort {
                $result = 'Timeout'
            }
        }

        If ($configInstallationWelcomePromptDynamicRunningProcessEvaluation) {
            $timerRunningProcesses.Stop()
        }

        Write-Output -InputObject ($result)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Show-InstallationRestartPrompt
Function Show-InstallationRestartPrompt {
    <#
.SYNOPSIS

Displays a restart prompt with a countdown to a forced restart.

.DESCRIPTION

Displays a restart prompt with a countdown to a forced restart.

.PARAMETER CountdownSeconds

Specifies the number of seconds to countdown before the system restart. Default: 60

.PARAMETER CountdownNoHideSeconds

Specifies the number of seconds to display the restart prompt without allowing the window to be hidden. Default: 30

.PARAMETER NoSilentRestart

Specifies whether the restart should be triggered when Deploy mode is silent or very silent. Default: $true

.PARAMETER NoCountdown

Specifies not to show a countdown.

The UI will restore/reposition itself persistently based on the interval value specified in the config file.

.PARAMETER SilentCountdownSeconds

Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and NoSilentRestart is $false. Default: 5

.PARAMETER TopMost

Specifies whether the windows is the topmost window. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60

.EXAMPLE

Show-InstallationRestartPrompt -NoCountdown

.EXAMPLE

Show-InstallationRestartPrompt -Countdownseconds 300 -NoSilentRestart $false -SilentCountdownSeconds 10

.NOTES

Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$CountdownSeconds = 60,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$CountdownNoHideSeconds = 30,
        [Parameter(Mandatory = $false)]
        [Boolean]$NoSilentRestart = $true,
        [Parameter(Mandatory = $false)]
        [Switch]$NoCountdown = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$SilentCountdownSeconds = 5,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$TopMost = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## If in non-interactive mode
        If ($deployModeSilent) {
            If ($NoSilentRestart -eq $false) {
                Write-Log -Message "Triggering restart silently, because the deploy mode is set to [$deployMode] and [NoSilentRestart] is disabled. Timeout is set to [$SilentCountdownSeconds] seconds." -Source ${CmdletName}
                Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `'& { Start-Sleep -Seconds $SilentCountdownSeconds; Restart-Computer -Force; }`'" -WindowStyle 'Hidden' -ErrorAction 'SilentlyContinue'
            }
            Else {
                Write-Log -Message "Skipping restart, because the deploy mode is set to [$deployMode] and [NoSilentRestart] is enabled." -Source ${CmdletName}
            }
            Return
        }
        ## Get the parameters passed to the function for invoking the function asynchronously
        [Hashtable]$installRestartPromptParameters = $PSBoundParameters

        ## Check if we are already displaying a restart prompt
        If (Get-Process | Where-Object { $_.MainWindowTitle -match $configRestartPromptTitle }) {
            Write-Log -Message "${CmdletName} was invoked, but an existing restart prompt was detected. Cancelling restart prompt." -Severity 2 -Source ${CmdletName}
            Return
        }

        ## If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously
        If ($deployAppScriptFriendlyName) {
            If ($NoCountdown) {
                Write-Log -Message "Invoking ${CmdletName} asynchronously with no countdown..." -Source ${CmdletName}
            }
            Else {
                Write-Log -Message "Invoking ${CmdletName} asynchronously with a [$countDownSeconds] second countdown..." -Source ${CmdletName}
            }
            ## Remove Silent reboot parameters from the list that is being forwarded to the main script for asynchronous function execution. This is only for Interactive mode so we dont need silent mode reboot parameters.
            $installRestartPromptParameters.Remove('NoSilentRestart')
            $installRestartPromptParameters.Remove('SilentCountdownSeconds')
            ## Prepare a list of parameters of this function as a string
            [String]$installRestartPromptParameters = ($installRestartPromptParameters.GetEnumerator() | ForEach-Object { & $ResolveParameters $_ }) -join ' '
            ## Start another powershell instance silently with function parameters from this function
            Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & {& `'$scriptPath`' -ReferredInstallTitle `'$installTitle`' -ReferredInstallName `'$installName`' -ReferredLogName `'$logName`' -ShowInstallationRestartPrompt $installRestartPromptParameters -AsyncToolkitLaunch}" -WindowStyle 'Hidden' -ErrorAction 'SilentlyContinue'
            Return
        }

        [DateTime]$startTime = Get-Date
        [DateTime]$countdownTime = $startTime

        [Windows.Forms.Application]::EnableVisualStyles()
        $formRestart = New-Object -TypeName 'System.Windows.Forms.Form'
        $labelCountdown = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelTimeRemaining = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $buttonRestartLater = New-Object -TypeName 'System.Windows.Forms.Button'
        $pictureBanner = New-Object -TypeName 'System.Windows.Forms.PictureBox'
        $buttonRestartNow = New-Object -TypeName 'System.Windows.Forms.Button'
        $timerCountdown = New-Object -TypeName 'System.Windows.Forms.Timer'
        $flowLayoutPanel = New-Object -TypeName 'System.Windows.Forms.FlowLayoutPanel'
        $panelButtons = New-Object -TypeName 'System.Windows.Forms.Panel'

        [ScriptBlock]$RestartComputer = {
            Write-Log -Message 'Forcefully restarting the computer...' -Source ${CmdletName}
            Restart-Computer -Force
        }

        [ScriptBlock]$Restart_Form_StateCorrection_Load = {
            # Disable the X button
            Try {
                $windowHandle = $formRestart.Handle
                If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
                    $menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
                    If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
                        [PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
                        [PSADT.UiAutomation]::DestroyMenu($menuHandle)
                    }
                }
            }
            Catch {
                # Not a terminating error if we can't disable the button. Just disable the Control Box instead
                Write-Log 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2 -Source ${CmdletName}
                $formRestart.ControlBox = $false
            }
            ## Initialize the countdown timer
            [DateTime]$currentTime = Get-Date
            [DateTime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
            $timerCountdown.Start()
            ## Set up the form
            [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
            $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
            If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) {
                $buttonRestartLater.Enabled = $false
            }
            $formRestart.WindowState = 'Normal'
            $formRestart.AutoSize = $true
            $formRestart.AutoScaleMode = 'Font'
            $formRestart.AutoScaleDimensions = New-Object System.Drawing.SizeF(6, 13) #Set as if using 96 DPI
            $formRestart.TopMost = $TopMost
            $formRestart.BringToFront()
            ## Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
            Set-Variable -Name 'formInstallationRestartPromptStartPosition' -Value $formRestart.Location -Scope 'Script'
        }

        ## Persistence Timer
        If ($NoCountdown) {
            $restartTimerPersist = New-Object -TypeName 'System.Windows.Forms.Timer'
            $restartTimerPersist.Interval = ($configInstallationRestartPersistInterval * 1000)
            [ScriptBlock]$restartTimerPersist_Tick = {
                #  Show the Restart Popup
                $formRestart.WindowState = 'Normal'
                $formRestart.TopMost = $TopMost
                $formRestart.BringToFront()
                $formRestart.Location = "$($formInstallationRestartPromptStartPosition.X),$($formInstallationRestartPromptStartPosition.Y)"
            }
            $restartTimerPersist.add_Tick($restartTimerPersist_Tick)
            $restartTimerPersist.Start()
        }

        [ScriptBlock]$buttonRestartLater_Click = {
            ## Minimize the form
            $formRestart.WindowState = 'Minimized'
            If ($NoCountdown) {
                ## Reset the persistence timer
                $restartTimerPersist.Stop()
                $restartTimerPersist.Start()
            }
        }

        ## Restart the computer
        [ScriptBlock]$buttonRestartNow_Click = { & $RestartComputer }

        ## Hide the form if minimized
        [ScriptBlock]$formRestart_Resize = { If ($formRestart.WindowState -eq 'Minimized') {
                $formRestart.WindowState = 'Minimized'
            } }

        [ScriptBlock]$timerCountdown_Tick = {
            ## Get the time information
            [DateTime]$currentTime = Get-Date
            [DateTime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
            [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
            ## If the countdown is complete, restart the machine
            If ($countdownTime -le $currentTime) {
                $buttonRestartNow.PerformClick()
            }
            Else {
                ## Update the form
                $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
                If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) {
                    $buttonRestartLater.Enabled = $false
                    #  If the form is hidden when we hit the "No Hide", bring it back up
                    If ($formRestart.WindowState -eq 'Minimized') {
                        #  Show Popup
                        $formRestart.WindowState = 'Normal'
                        $formRestart.TopMost = $TopMost
                        $formRestart.BringToFront()
                        $formRestart.Location = "$($formInstallationRestartPromptStartPosition.X),$($formInstallationRestartPromptStartPosition.Y)"
                    }
                }
            }
        }

        ## Remove all event handlers from the controls
        [ScriptBlock]$Restart_Form_Cleanup_FormClosed = {
            Try {
                $buttonRestartLater.remove_Click($buttonRestartLater_Click)
                $buttonRestartNow.remove_Click($buttonRestartNow_Click)
                $formRestart.remove_Load($Restart_Form_StateCorrection_Load)
                $formRestart.remove_Resize($formRestart_Resize)
                $timerCountdown.remove_Tick($timerCountdown_Tick)
                $restartTimerPersist.remove_Tick($restartTimerPersist_Tick)
                $formRestart.remove_FormClosed($Restart_Form_Cleanup_FormClosed)
            }
            Catch {
            }
        }

        ## Form
        ##----------------------------------------------
        ## Create zero px padding object
        $paddingNone = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 0)
        ## Create basic control size
        $defaultControlSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 0)

        ## Generic Button properties
        $buttonSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (195, 24)

        ## Picture Banner
        $pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
        $pictureBanner.ImageLocation = $appDeployLogoBanner
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
        $pictureBanner.Location = $System_Drawing_Point
        $pictureBanner.Name = 'pictureBanner'
        $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, $appDeployLogoBannerHeight)
        $pictureBanner.Size = $System_Drawing_Size
        $pictureBanner.SizeMode = 'CenterImage'
        $pictureBanner.Margin = $paddingNone
        $pictureBanner.TabStop = $false

        ## Label Message
        $labelMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelMessage.Font = $defaultFont
        $labelMessage.Name = 'labelMessage'
        $labelMessage.Size = $defaultControlSize
        $labelMessage.MinimumSize = $defaultControlSize
        $labelMessage.MaximumSize = $defaultControlSize
        $labelMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 5)
        $labelMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelMessage.Text = "$configRestartPromptMessage $configRestartPromptMessageTime `r`n`r`n$configRestartPromptMessageRestart"
        If ($NoCountdown) {
            $labelMessage.Text = $configRestartPromptMessage
        }
        $labelMessage.TextAlign = 'MiddleCenter'
        $labelMessage.Anchor = 'Top'
        $labelMessage.TabStop = $false
        $labelMessage.AutoSize = $true

        ## Label Time remaining message
        $labelTimeRemaining.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelTimeRemaining.Font = "$($defaultFont.Name), $($defaultFont.Size + 2), style=Regular"
        $labelTimeRemaining.Name = 'labelTimeRemaining'
        $labelTimeRemaining.Size = $defaultControlSize
        $labelTimeRemaining.MinimumSize = $defaultControlSize
        $labelTimeRemaining.MaximumSize = $defaultControlSize
        $labelTimeRemaining.Margin = $paddingNone
        $labelTimeRemaining.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelTimeRemaining.TabStop = $false
        $labelTimeRemaining.Text = $configRestartPromptTimeRemaining
        $labelTimeRemaining.TextAlign = 'MiddleCenter'
        $labelTimeRemaining.Anchor = 'Top'
        $labelTimeRemaining.AutoSize = $true

        ## Label Countdown
        $labelCountdown.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCountdown.Font = "$($defaultFont.Name), $($defaultFont.Size + 9), style=Bold"
        $labelCountdown.Name = 'labelCountdown'
        $labelCountdown.Size = $defaultControlSize
        $labelCountdown.MinimumSize = $defaultControlSize
        $labelCountdown.MaximumSize = $defaultControlSize
        $labelCountdown.Margin = $paddingNone
        $labelCountdown.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCountdown.TabStop = $false
        $labelCountdown.Text = '00:00:00'
        $labelCountdown.TextAlign = 'MiddleCenter'
        $labelCountdown.AutoSize = $true

        ## Panel Flow Layout
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, $appDeployLogoBannerHeight)
        $flowLayoutPanel.Location = $System_Drawing_Point
        $flowLayoutPanel.MinimumSize = $DefaultControlSize
        $flowLayoutPanel.MaximumSize = $DefaultControlSize
        $flowLayoutPanel.Size = $DefaultControlSize
        $flowLayoutPanel.Margin = $paddingNone
        $flowLayoutPanel.Padding = $paddingNone
        $flowLayoutPanel.AutoSizeMode = 'GrowAndShrink'
        $flowLayoutPanel.AutoSize = $true
        $flowLayoutPanel.Anchor = 'Top'
        $flowLayoutPanel.FlowDirection = 'TopDown'
        $flowLayoutPanel.WrapContents = $true
        $flowLayoutPanel.Controls.Add($labelMessage)
        If (-not $NoCountdown) {
            $flowLayoutPanel.Controls.Add($labelTimeRemaining)
            $flowLayoutPanel.Controls.Add($labelCountdown)
        }

        ## Button Minimize
        $buttonRestartLater.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonRestartLater.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (240, 4)
        $buttonRestartLater.Name = 'buttonRestartLater'
        $buttonRestartLater.Font = $defaultFont
        $buttonRestartLater.Size = $buttonSize
        $buttonRestartLater.MinimumSize = $buttonSize
        $buttonRestartLater.MaximumSize = $buttonSize
        $buttonRestartLater.TabIndex = 0
        $buttonRestartLater.Text = $configRestartPromptButtonRestartLater
        $buttonRestartLater.AutoSize = $true
        $buttonRestartLater.Margin = $paddingNone
        $buttonRestartLater.Padding = $paddingNone
        $buttonRestartLater.UseVisualStyleBackColor = $true
        $buttonRestartLater.add_Click($buttonRestartLater_Click)

        ## Button Restart Now
        $buttonRestartNow.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonRestartNow.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (14, 4)
        $buttonRestartNow.Name = 'buttonRestartNow'
        $buttonRestartNow.Font = $defaultFont
        $buttonRestartNow.Size = $buttonSize
        $buttonRestartNow.MinimumSize = $buttonSize
        $buttonRestartNow.MaximumSize = $buttonSize
        $buttonRestartNow.TabIndex = 1
        $buttonRestartNow.Text = $configRestartPromptButtonRestartNow
        $buttonRestartNow.Margin = $paddingNone
        $buttonRestartNow.Padding = $paddingNone
        $buttonRestartNow.UseVisualStyleBackColor = $true
        $buttonRestartNow.add_Click($buttonRestartNow_Click)

        ## Form Restart
        $formRestart.Size = $defaultControlSize
        $formRestart.MinimumSize = $defaultControlSize
        $formRestart.Padding = $paddingNone
        $formRestart.Margin = $paddingNone
        $formRestart.DataBindings.DefaultDataSourceUpdateMode = 0
        $formRestart.Name = 'formRestart'
        $formRestart.Text = $installTitle
        $formRestart.StartPosition = 'CenterScreen'
        # $formRestart.FormBorderStyle = 'FixedDialog'
        $formRestart.MaximizeBox = $false
        $formRestart.MinimizeBox = $false
        $formRestart.TopMost = $TopMost
        $formRestart.TopLevel = $true
        $formRestart.Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
        $formRestart.AutoSize = $true
        $formRestart.AutoScaleMode = 'Font'
        $formRestart.AutoScaleDimensions = New-Object System.Drawing.SizeF(6, 13) #Set as if using 96 DPI
        $formRestart.ControlBox = $true
        $formRestart.Controls.Add($pictureBanner)

        ## Button Panel
        $panelButtons.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.AutoSize = $true
        $panelButtons.Padding = $paddingNone
        $panelButtons.Margin = $paddingNone
        $panelButtons.Controls.Add($buttonRestartNow)
        $panelButtons.Controls.Add($buttonRestartLater)
        ## Add the Buttons Panel to the flowPanel
        $flowLayoutPanel.Controls.Add($panelButtons)
        ## Add FlowPanel to the form
        $formRestart.Controls.Add($flowLayoutPanel)
        $formRestart.add_Resize($formRestart_Resize)
        ## Timer Countdown
        If (-not $NoCountdown) {
            $timerCountdown.add_Tick($timerCountdown_Tick)
        }
        ##----------------------------------------------
        # Init the OnLoad event to correct the initial state of the form
        $formRestart.add_Load($Restart_Form_StateCorrection_Load)
        # Clean up the control events
        $formRestart.add_FormClosed($Restart_Form_Cleanup_FormClosed)
        $formRestartClosing = [Windows.Forms.FormClosingEventHandler] { If ($_.CloseReason -eq 'UserClosing') {
                $_.Cancel = $true
            } }
        $formRestart.add_FormClosing($formRestartClosing)

        If ($NoCountdown) {
            Write-Log -Message 'Displaying restart prompt with no countdown.' -Source ${CmdletName}
        }
        Else {
            Write-Log -Message "Displaying restart prompt with a [$countDownSeconds] second countdown." -Source ${CmdletName}
        }

        #  Show the Form
        Write-Output -InputObject ($formRestart.ShowDialog())
        $formRestart.Dispose()
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Show-BalloonTip
Function Show-BalloonTip {
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

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Show-BalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

.EXAMPLE

Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000

.NOTES

For Windows 10 OS and above a Toast notification is displayed in place of a balloon tip. The toast notification does not use tte BalloonTipIcon if specified.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [String]$BalloonTipText,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$BalloonTipTitle = $installTitle,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateSet('Error', 'Info', 'None', 'Warning')]
        [Windows.Forms.ToolTipIcon]$BalloonTipIcon = 'Info',
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [Int32]$BalloonTipTime = 10000,
        [Parameter(Mandatory = $false, Position = 4)]
        [Switch]$NoWait = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Skip balloon if in silent mode, disabled in the config or presentation is detected
        $presentationDetected = Test-PowerPoint
        If (($deployModeSilent) -or (-not $configShowBalloonNotifications) -or $presentationDetected) {
            Write-Log -Message "Bypassing Show-BalloonTip [Mode:$deployMode, Config Show Balloon Notifications:$configShowBalloonNotifications, Presentation Detected:$presentationDetected]. BalloonTipText:$BalloonTipText" -Source ${CmdletName}
            Return
        }
        ## Dispose of previous balloon
        If ($script:notifyIcon) {
            Try {
                $script:notifyIcon.Dispose()
            }
            Catch {
            }
        }

        If ($envOSVersionMajor -lt 10) {
            ## NoWait - Create the balloontip icon asynchronously
            If ($NoWait) {
                Write-Log -Message "Displaying balloon tip notification asynchronously with message [$BalloonTipText]." -Source ${CmdletName}
                ## Create a script block to display the balloon notification in a new PowerShell process so that we can wait to cleanly dispose of the balloon tip without having to make the deployment script wait
                ## Scriptblock text has to be as short as possible because it is passed as a parameter to powershell
                ## Don't strongly type parameter BalloonTipIcon as System.Drawing assembly not loaded yet in asynchronous scriptblock so will throw error
                [ScriptBlock]$notifyIconScriptBlock = {
                    Param(
                        [Parameter(Mandatory = $true, Position = 0)]
                        [ValidateNotNullOrEmpty()]
                        [String]$BalloonTipText,
                        [Parameter(Mandatory = $false, Position = 1)]
                        [ValidateNotNullorEmpty()]
                        [String]$BalloonTipTitle,
                        [Parameter(Mandatory = $false, Position = 2)]
                        [ValidateSet('Error', 'Info', 'None', 'Warning')]
                        $BalloonTipIcon = 'Info',
                        [Parameter(Mandatory = $false, Position = 3)]
                        [ValidateNotNullorEmpty()]
                        [Int32]$BalloonTipTime,
                        [Parameter(Mandatory = $false, Position = 4)]
                        [ValidateNotNullorEmpty()]
                        [String]$AppDeployLogoIcon
                    )
                    Add-Type -AssemblyName 'System.Windows.Forms', 'System.Drawing' -ErrorAction 'Stop'
                    $BalloonTipIconText = [String]::Concat($BalloonTipTitle, ' - ', $BalloonTipText)
                    If ($BalloonTipIconText.Length -gt 63) {
                        $BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0, 60), '...')
                    }
                    [Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
                    $script:notifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
                        BalloonTipIcon  = $BalloonTipIcon
                        BalloonTipText  = $BalloonTipText
                        BalloonTipTitle = $BalloonTipTitle
                        Icon            = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
                        Text            = $BalloonTipIconText
                        Visible         = $true
                    }

                    $script:notifyIcon.ShowBalloonTip($BalloonTipTime)
                    Start-Sleep -Milliseconds ($BalloonTipTime)
                    $script:notifyIcon.Dispose() }

                ## Invoke a separate PowerShell process passing the script block as a command and associated parameters to display the balloon tip notification asynchronously
                Try {
                    Execute-Process -Path "$PSHOME\powershell.exe" -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & {$notifyIconScriptBlock} `'$BalloonTipText`' `'$BalloonTipTitle`' `'$BalloonTipIcon`' `'$BalloonTipTime`' `'$AppDeployLogoIcon`'" -NoWait -WindowStyle 'Hidden' -CreateNoWindow
                }
                Catch {
                }
            }
            ## Otherwise create the balloontip icon synchronously
            Else {
                Write-Log -Message "Displaying balloon tip notification with message [$BalloonTipText]." -Source ${CmdletName}
                ## Prepare Text - Cut it if longer than 63 chars
                $BalloonTipIconText = [String]::Concat($BalloonTipTitle, ' - ', $BalloonTipText)
                If ($BalloonTipIconText.Length -gt 63) {
                    $BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0, 60), '...')
                }
                ## Create the BalloonTip
                [Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
                $script:notifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
                    BalloonTipIcon  = $BalloonTipIcon
                    BalloonTipText  = $BalloonTipText
                    BalloonTipTitle = $BalloonTipTitle
                    Icon            = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
                    Text            = $BalloonTipIconText
                    Visible         = $true
                }
                ## Display the balloon tip notification
                $script:notifyIcon.ShowBalloonTip($BalloonTipTime)
            }
        }
        Else {
            Write-Log -Message "Displaying toast notification with message [$BalloonTipText]." -Source ${CmdletName}
            
            [scriptblock]$toastScriptBlock  = {
                Param(
                    [Parameter(Mandatory = $true, Position = 0)]
                    [ValidateNotNullOrEmpty()]
                    [String]$BalloonTipText,
                    [Parameter(Mandatory = $false, Position = 1)]
                    [ValidateNotNullorEmpty()]
                    [String]$BalloonTipTitle,                                 
                    [Parameter(Mandatory = $false, Position = 4)]
                    [ValidateNotNullorEmpty()]
                    [String]$AppDeployLogoImage
                )
            
                # Check for required entries in registry for when using Powershell as application for the toast
                # Register the AppID in the registry for use with the Action Center, if required
                $regPathNotificationSettings = 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings'
                $toastAppId =  '{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe'

                # Create the registry entries if they don't exist
                If (-not (Test-Path -Path "$regPathNotificationSettings\$toastAppId") ) {
                    $null = New-Item -Path "$regPathNotificationSettings\$toastAppId" -Force
                    $null = New-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'ShowInActionCenter' -Value 1 -PropertyType 'DWORD'
                    $null = New-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'Enabled' -Value 1 -PropertyType 'DWORD'
                    $null = New-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'SoundFile' -PropertyType 'STRING'
                }
                # Make sure the app used with the action center is enabled
                If ((Get-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'ShowInActionCenter' -ErrorAction 'SilentlyContinue').ShowInActionCenter -ne '1') {
                    $null = New-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'ShowInActionCenter' -Value 1 -PropertyType 'DWORD' -Force
                }
                If ((Get-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'Enabled' -ErrorAction 'SilentlyContinue').Enabled -ne '1') {
                    $null = New-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'Enabled' -Value 1 -PropertyType 'DWORD' -Force
                }
                If (!(Get-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'SoundFile' -ErrorAction 'SilentlyContinue')) {
                    $null = New-ItemProperty -Path "$regPathNotificationSettings\$toastAppId" -Name 'SoundFile' -PropertyType 'STRING' -Force
                }
                
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

                ## Gets the Template XML so we can manipulate the values
                $Template = [Windows.UI.Notifications.ToastTemplateType]::ToastImageAndText01
                [xml] $ToastTemplate = ([Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent($Template).GetXml())
                [xml] $ToastTemplate = @"
<toast launch="app-defined-string">
	<visual>
		<binding template="ToastImageAndText02">
			<text id="1">$BalloonTipTitle</text>
			<text id="2">$BalloonTipText</text>
			<image id="1" src="file://$appDeployLogoImage" />
		</binding>
	</visual>
</toast>
"@

                $ToastXml = New-Object -TypeName Windows.Data.Xml.Dom.XmlDocument
                $ToastXml.LoadXml($ToastTemplate.OuterXml)

                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($toastAppId)
                $notifier.Show($toastXml)
  
            }

            ## Invoke a separate PowerShell process as the current user passing the script block as a command and associated parameters to display the toast notification in the user context
            Try {                
                $executeToastAsUserScript = "$configToolkitTempPath\$($appDeployToolkitName)-ToastNotification.ps1"
                Set-Content -Path $executeToastAsUserScript -Value $toastScriptBlock -Force
                Execute-ProcessAsUser -Path "$PSHOME\powershell.exe" -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & { & `"$executeToastAsUserScript `'$BalloonTipText`' `'$BalloonTipTitle`' `'$AppDeployLogoImage`'`"; Exit `$LastExitCode }" -Wait
            }
            Catch {
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Show-InstallationProgress
Function Show-InstallationProgress {
    <#
.SYNOPSIS

Displays a progress dialog in a separate thread with an updateable custom message.

.DESCRIPTION

Create a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated.

The status message supports line breaks.

The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).

.PARAMETER StatusMessage

The status message to be displayed. The default status message is taken from the XML configuration file.

.PARAMETER WindowLocation

The location of the progress window. Default: center of the screen.

.PARAMETER TopMost

Specifies whether the progress window should be topmost. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Show-InstallationProgress

Uses the default status message from the XML configuration file.

.EXAMPLE

Show-InstallationProgress -StatusMessage 'Installation in Progress...'

.EXAMPLE

Show-InstallationProgress -StatusMessage "Installation in Progress...`r`nThe installation may take 20 minutes to complete."

.EXAMPLE

Show-InstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost $false

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$StatusMessage = $configProgressMessageInstall,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Default', 'BottomRight', 'TopCenter')]
        [String]$WindowLocation = 'Default',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$TopMost = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If ($deployModeSilent) {
            Write-Log -Message "Bypassing Show-InstallationProgress [Mode: $deployMode]. Status message:$StatusMessage" -Source ${CmdletName}
            Return
        }

        ## If the default progress message hasn't been overridden and the deployment type is uninstall, use the default uninstallation message
        If ($StatusMessage -eq $configProgressMessageInstall) {
            If ($deploymentType -eq 'Uninstall') {
                $StatusMessage = $configProgressMessageUninstall
            }
            ElseIf ($deploymentType -eq 'Repair') {
                $StatusMessage = $configProgressMessageRepair
            }
        }

        If ($envHost.Name -match 'PowerGUI') {
            Write-Log -Message "$($envHost.Name) is not a supported host for WPF multi-threading. Progress dialog with message [$statusMessage] will not be displayed." -Severity 2 -Source ${CmdletName}
            Return
        }

        ## Check if the progress thread is running before invoking methods on it
        If ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -ne 'Running') {
            #  Notify user that the software installation has started
            $balloonText = "$deploymentTypeName $configBalloonTextStart"
            Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText $balloonText
            #  Create a synchronized hashtable to share objects between runspaces
            $script:ProgressSyncHash = [Hashtable]::Synchronized(@{ })
            #  Create a new runspace for the progress bar
            $script:ProgressRunspace = [runspacefactory]::CreateRunspace()
            $script:ProgressRunspace.ApartmentState = 'STA'
            $script:ProgressRunspace.ThreadOptions = 'ReuseThread'
            $script:ProgressRunspace.Open()
            #  Add the sync hash to the runspace
            $script:ProgressRunspace.SessionStateProxy.SetVariable('progressSyncHash', $script:ProgressSyncHash)
            #  Add other variables from the parent thread required in the progress runspace
            $script:ProgressRunspace.SessionStateProxy.SetVariable('installTitle', $installTitle)
            $script:ProgressRunspace.SessionStateProxy.SetVariable('windowLocation', $windowLocation)
            $script:ProgressRunspace.SessionStateProxy.SetVariable('topMost', $topMost.ToString())
            $script:ProgressRunspace.SessionStateProxy.SetVariable('appDeployLogoBanner', $appDeployLogoBanner)
            $script:ProgressRunspace.SessionStateProxy.SetVariable('ProgressStatusMessage', $statusMessage)
            $script:ProgressRunspace.SessionStateProxy.SetVariable('AppDeployLogoIcon', $AppDeployLogoIcon)

            #  Add the script block to be executed in the progress runspace
            $progressCmd = [PowerShell]::Create().AddScript({
                    [String]$xamlProgressString = @'
				<Window
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				x:Name="Window" Title="PSAppDeployToolkit"
				Padding="0,0,0,0" Margin="0,0,0,0"
				WindowStartupLocation = "Manual"
				Icon=""
				Top="0"
				Left="0"
				Topmost="True"
				ResizeMode="NoResize"
				ShowInTaskbar="True" VerticalContentAlignment="Center" HorizontalContentAlignment="Center" SizeToContent="WidthAndHeight">
					<Window.Resources>
					<Storyboard x:Key="Storyboard1" RepeatBehavior="Forever">
					<DoubleAnimationUsingKeyFrames BeginTime="00:00:00" Storyboard.TargetName="ellipse" Storyboard.TargetProperty="(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)">
						<SplineDoubleKeyFrame KeyTime="00:00:02" Value="360"/>
					</DoubleAnimationUsingKeyFrames>
					</Storyboard>
					</Window.Resources>
					<Window.Triggers>
					<EventTrigger RoutedEvent="FrameworkElement.Loaded">
					<BeginStoryboard Storyboard="{StaticResource Storyboard1}"/>
					</EventTrigger>
					</Window.Triggers>
					<Grid Background="#F0F0F0" MinWidth="450" MaxWidth="450" Width="450">
					<Grid.RowDefinitions>
					<RowDefinition Height="*"/>
					<RowDefinition Height="*"/>
					</Grid.RowDefinitions>
					<Grid.ColumnDefinitions>
						<ColumnDefinition MinWidth="100" MaxWidth="100" Width="100"></ColumnDefinition>
						<ColumnDefinition MinWidth="350" MaxWidth="350" Width="350"></ColumnDefinition>
					</Grid.ColumnDefinitions>
					<Image x:Name = "ProgressBanner" Grid.ColumnSpan="2" Margin="0,0,0,0" Source="" Grid.Row="0"/>
					<TextBlock x:Name = "ProgressText" Grid.Row="1" Grid.Column="1" Margin="0,30,20,30" Text="Installation in progress" FontSize="14" HorizontalAlignment="Center" VerticalAlignment="Center" TextAlignment="Center" Padding="10,0,10,0" TextWrapping="Wrap"></TextBlock>
					<Ellipse x:Name = "ellipse" Grid.Row="1" Grid.Column="0" Margin="0,0,0,0" StrokeThickness="5" RenderTransformOrigin="0.5,0.5" Height="32" Width="32" HorizontalAlignment="Center" VerticalAlignment="Center">
					<Ellipse.RenderTransform>
						<TransformGroup>
							<ScaleTransform/>
							<SkewTransform/>
							<RotateTransform/>
						</TransformGroup>
					</Ellipse.RenderTransform>
					<Ellipse.Stroke>
						<LinearGradientBrush EndPoint="0.445,0.997" StartPoint="0.555,0.003">
							<GradientStop Color="White" Offset="0"/>
							<GradientStop Color="#0078d4" Offset="1"/>
						</LinearGradientBrush>
					</Ellipse.Stroke>
					</Ellipse>
					</Grid>
				</Window>
'@
                    [Xml.XmlDocument]$xamlProgress = New-Object -TypeName 'System.Xml.XmlDocument'
                    $xamlProgress.LoadXml($xamlProgressString)
                    ## Set the configurable values using variables added to the runspace from the parent thread
                    $xamlProgress.Window.TopMost = $topMost
                    $xamlProgress.Window.Icon = $AppDeployLogoIcon
                    $xamlProgress.Window.Grid.Image.Source = $appDeployLogoBanner
                    $xamlProgress.Window.Grid.TextBlock.Text = $ProgressStatusMessage
                    $xamlProgress.Window.Title = $installTitle
                    #  Parse the XAML
                    $progressReader = New-Object -TypeName 'System.Xml.XmlNodeReader' -ArgumentList ($xamlProgress)
                    $script:ProgressSyncHash.Window = [Windows.Markup.XamlReader]::Load($progressReader)
                    #  Grey out the X button
                    $script:ProgressSyncHash.Window.add_Loaded({
                            #  Calculate the position on the screen where the progress dialog should be placed
                            [Int32]$screenWidth = [System.Windows.SystemParameters]::WorkArea.Width
                            [Int32]$screenHeight = [System.Windows.SystemParameters]::WorkArea.Height
                            [Int32]$script:screenCenterWidth = $screenWidth - $script:ProgressSyncHash.Window.ActualWidth
                            [Int32]$script:screenCenterHeight = $screenHeight - $script:ProgressSyncHash.Window.ActualHeight
                            #  Set the start position of the Window based on the screen size
                            If ($windowLocation -eq 'BottomRight') {
                                $script:ProgressSyncHash.Window.Left = [Double]($screenCenterWidth)
                                $script:ProgressSyncHash.Window.Top = [Double]($screenCenterHeight - 100) #-100 Needed to not overlap system tray Toasts
                            }
                            ElseIf ($windowLocation -eq 'TopCenter') {
                                $script:ProgressSyncHash.Window.Left = [Double]($screenCenterWidth / 2)
                                $script:ProgressSyncHash.Window.Top = [Double]($screenCenterHeight / 6)
                            }
                            Else {
                                #  Center the progress window by calculating the center of the workable screen based on the width of the screen minus half the width of the progress bar
                                $script:ProgressSyncHash.Window.Left = [Double]($screenCenterWidth / 2)
                                $script:ProgressSyncHash.Window.Top = [Double]($screenCenterHeight / 2)
                            }
                            #  Disable the X button
                            Try {
                                $windowHandle = (New-Object -TypeName System.Windows.Interop.WindowInteropHelper -ArgumentList ($this)).Handle
                                If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
                                    $menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
                                    If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
                                        [PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
                                        [PSADT.UiAutomation]::DestroyMenu($menuHandle)
                                    }
                                }
                            }
                            Catch {
                                # Not a terminating error if we can't disable the close button
                                Write-Log 'Failed to disable the Close button.' -Severity 2 -Source ${CmdletName}
                            }
                        })
                    #  Prepare the ProgressText variable so we can use it to change the text in the text area
                    $script:ProgressSyncHash.ProgressText = $script:ProgressSyncHash.Window.FindName('ProgressText')
                    #  Add an action to the Window.Closing event handler to disable the close button
                    $script:ProgressSyncHash.Window.Add_Closing({ $_.Cancel = $true })
                    #  Allow the window to be dragged by clicking on it anywhere
                    $script:ProgressSyncHash.Window.Add_MouseLeftButtonDown({ $script:ProgressSyncHash.Window.DragMove() })
                    #  Add a tooltip
                    $script:ProgressSyncHash.Window.ToolTip = $installTitle
                    $null = $script:ProgressSyncHash.Window.ShowDialog()
                    $script:ProgressSyncHash.Error = $Error
                })

            $progressCmd.Runspace = $script:ProgressRunspace
            Write-Log -Message "Creating the progress dialog in a separate thread with message: [$statusMessage]." -Source ${CmdletName}
            #  Invoke the progress runspace
            $null = $progressCmd.BeginInvoke()
            #  Allow the thread to be spun up safely before invoking actions against it.
            Start-Sleep -Seconds 1
            If ($script:ProgressSyncHash.Error) {
                Write-Log -Message "Failure while displaying progress dialog. `r`n$(Resolve-Error -ErrorRecord $script:ProgressSyncHash.Error)" -Severity 3 -Source ${CmdletName}
            }
        }
        ## Check if the progress thread is running before invoking methods on it
        ElseIf ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -eq 'Running') {
            Try {
                #  Update the progress text
                $script:ProgressSyncHash.Window.Dispatcher.Invoke([Windows.Threading.DispatcherPriority]::Send, [Windows.Input.InputEventHandler] { $script:ProgressSyncHash.ProgressText.Text = $statusMessage }, $null, $null)
                #  Calculate the position on the screen where the progress dialog should be placed
                $script:ProgressSyncHash.Window.Dispatcher.Invoke([Windows.Threading.DispatcherPriority]::Send, [Windows.Input.InputEventHandler] {
                        [Int32]$screenWidth = [System.Windows.SystemParameters]::WorkArea.Width
                        [Int32]$screenHeight = [System.Windows.SystemParameters]::WorkArea.Height
                        #  Set the start position of the Window based on the screen size
                        If ($windowLocation -eq 'BottomRight') {
                            #  Put the window in the corner
                            $script:ProgressSyncHash.Window.Left = ($screenWidth - $script:ProgressSyncHash.Window.ActualWidth)
                            $script:ProgressSyncHash.Window.Top = ($screenHeight - $script:ProgressSyncHash.Window.ActualHeight - 100) #-100 Needed to not overlap system tray Toasts
                        }
                        ElseIf ($windowLocation -eq 'TopCenter') {
                            $script:ProgressSyncHash.Window.Left = [Double](($screenWidth - $script:ProgressSyncHash.Window.ActualWidth) / 2)
                            $script:ProgressSyncHash.Window.Top = [Double](($screenHeight - $script:ProgressSyncHash.Window.ActualHeight) / 6)
                        }
                        Else {
                            #  Center the progress window by calculating the center of the workable screen based on the width of the screen minus half the width of the progress bar
                            $script:ProgressSyncHash.Window.Left = [Double](($screenWidth - $script:ProgressSyncHash.Window.ActualWidth) / 2)
                            $script:ProgressSyncHash.Window.Top = [Double](($screenHeight - $script:ProgressSyncHash.Window.ActualHeight) / 2)
                        }
                    }, $null, $null)

                Write-Log -Message "Updated the progress message: [$statusMessage]." -Source ${CmdletName}
            }
            Catch {
                Write-Log -Message "Unable to update the progress message. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Close-InstallationProgress
Function Close-InstallationProgress {
    <#
.SYNOPSIS

Closes the dialog created by Show-InstallationProgress.

.DESCRIPTION

Closes the dialog created by Show-InstallationProgress.

This function is called by the Exit-Script function to close a running instance of the progress dialog if found.

.PARAMETER WaitingTime

How many seconds to wait, at most, for the InstallationProgress window to be initialized, before the function returns, without closing anything. Range: 1 - 60  Default: 5

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Close-InstallationProgress

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateRange(1, 60)]
        [Int32]$WaitingTime = 5
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If ($deployModeSilent) {
            Write-Log -Message "Bypassing Close-InstallationProgress [Mode: $deployMode]" -Source ${CmdletName}
            Return
        }
        # Check whether the window has been created and wait for up to $WaitingTime seconds if it does not
        [Int32]$Timeout = $WaitingTime
        While ((-not $script:ProgressSyncHash.Window.IsInitialized) -and ($Timeout -gt 0)) {
            If ($Timeout -eq $WaitingTime) {
                Write-Log -Message "The installation progress dialog does not exist. Waiting up to $WaitingTime seconds..." -Source ${CmdletName}
            }
            $Timeout -= 1
            Start-Sleep -Seconds 1
        }
        # Return if we still have no window
        If (-not $script:ProgressSyncHash.Window.IsInitialized) {
            Write-Log -Message "The installation progress dialog was not created within $WaitingTime seconds." -Source ${CmdletName} -Severity 2
            Return
        }
        # If the thread is suspended, resume it
        If ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Suspended) {
            Write-Log -Message 'The thread for the installation progress dialog is suspended. Resuming the thread.' -Source ${CmdletName}
            Try {
                $script:ProgressSyncHash.Window.Dispatcher.Thread.Resume()
            }
            Catch {
                Write-Log -Message 'Failed to resume the thread for the installation progress dialog.' -Source ${CmdletName} -Severity 2
            }
        }
        # If the thread is changing its state, wait
        [Int32]$Timeout = 0
        While ((($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Aborted) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::AbortRequested) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::StopRequested) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Unstarted) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::WaitSleepJoin)) -and ($Timeout -le $WaitingTime)) {
            If (-not $Timeout) {
                Write-Log -Message "The thread for the installation progress dialog is changing its state. Waiting up to $WaitingTime seconds..." -Source ${CmdletName} -Severity 2
            }
            $Timeout += 1
            Start-Sleep -Seconds 1
        }
        # If the thread is running, stop it
        If ((-not ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Stopped)) -and (-not ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Unstarted))) {
            Write-Log -Message 'Closing the installation progress dialog.' -Source ${CmdletName}
            $script:ProgressSyncHash.Window.Dispatcher.InvokeShutdown()
        }

        If ($script:ProgressRunspace) {
            # If the runspace is still opening, wait
            [Int32]$Timeout = 0
            While ((($script:ProgressRunspace.RunspaceStateInfo.State -eq [System.Management.Automation.Runspaces.RunspaceState]::Opening) -or ($script:ProgressRunspace.RunspaceStateInfo.State -eq [System.Management.Automation.Runspaces.RunspaceState]::BeforeOpen)) -and ($Timeout -le $WaitingTime)) {
                If (-not $Timeout) {
                    Write-Log -Message "The runspace for the installation progress dialog is still opening. Waiting up to $WaitingTime seconds..." -Source ${CmdletName} -Severity 2
                }
                $Timeout += 1
                Start-Sleep -Seconds 1
            }
            # If the runspace is opened, close it
            If ($script:ProgressRunspace.RunspaceStateInfo.State -eq [System.Management.Automation.Runspaces.RunspaceState]::Opened) {
                Write-Log -Message "Closing the installation progress dialog`'s runspace." -Source ${CmdletName}
                $script:ProgressRunspace.Close()
            }
        }
        Else {
            Write-Log -Message 'The runspace for the installation progress dialog is already closed.' -Source ${CmdletName} -Severity 2
        }

        If ($script:ProgressSyncHash) {
            # Clear sync hash
            $script:ProgressSyncHash.Clear()
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Set-PinnedApplication
Function Set-PinnedApplication {
    <#
.SYNOPSIS

Pins or unpins a shortcut to the start menu or task bar.

.DESCRIPTION

Pins or unpins a shortcut to the start menu or task bar.

This should typically be run in the user context, as pinned items are stored in the user profile.

.PARAMETER Action

Action to be performed. Options: 'PinToStartMenu','UnpinFromStartMenu','PinToTaskbar','UnpinFromTaskbar'.

.PARAMETER FilePath

Path to the shortcut file to be pinned or unpinned.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-PinnedApplication -Action 'PinToStartMenu' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.EXAMPLE

Set-PinnedApplication -Action 'UnpinFromTaskbar' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.NOTES

Windows 10 logic borrowed from Stuart Pearson (https://pinto10blog.wordpress.com/2016/09/10/pinto10/)

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateSet('PinToStartMenu', 'UnpinFromStartMenu', 'PinToTaskbar', 'UnpinFromTaskbar')]
        [String]$Action,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$FilePath
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        #region Function Get-PinVerb
        Function Get-PinVerb {
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [Int32]$VerbId
            )

            [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

            Write-Log -Message "Get localized pin verb for verb id [$VerbID]." -Source ${CmdletName}
            [String]$PinVerb = [PSADT.FileVerb]::GetPinVerb($VerbId)
            Write-Log -Message "Verb ID [$VerbID] has a localized pin verb of [$PinVerb]." -Source ${CmdletName}
            Write-Output -InputObject ($PinVerb)
        }
        #endregion

        #region Function Invoke-Verb
        Function Invoke-Verb {
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String]$FilePath,
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String]$Verb
            )

            Try {
                [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
                $Verb = $Verb.Replace('&', '')
                $path = Split-Path -Path $FilePath -Parent -ErrorAction 'Stop'
                $folder = $shellApp.Namespace($path)
                $item = $folder.ParseName((Split-Path -Path $FilePath -Leaf -ErrorAction 'Stop'))
                $itemVerb = $item.Verbs() | Where-Object { $_.Name.Replace('&', '') -eq $Verb } -ErrorAction 'Stop'

                If ($null -eq $itemVerb) {
                    Write-Log -Message "Performing action [$Verb] is not programmatically supported for this file [$FilePath]." -Severity 2 -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message "Performing action [$Verb] on [$FilePath]." -Source ${CmdletName}
                    $itemVerb.DoIt()
                }
            }
            Catch {
                Write-Log -Message "Failed to perform action [$Verb] on [$FilePath]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
            }
        }
        #endregion

        If (([Version]$envOSVersion).Major -ge 10) {
            Write-Log -Message 'Detected Windows 10 or higher, using Windows 10 verb codes.' -Source ${CmdletName}
            [Hashtable]$Verbs = @{
                'PinToStartMenu'     = 51201
                'UnpinFromStartMenu' = 51394
                'PinToTaskbar'       = 5386
                'UnpinFromTaskbar'   = 5387
            }
        }
        Else {
            [Hashtable]$Verbs = @{
                'PinToStartMenu'     = 5381
                'UnpinFromStartMenu' = 5382
                'PinToTaskbar'       = 5386
                'UnpinFromTaskbar'   = 5387
            }
        }

    }
    Process {
        Try {
            Write-Log -Message "Execute action [$Action] for file [$FilePath]." -Source ${CmdletName}

            If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf' -ErrorAction 'Stop')) {
                Throw "Path [$filePath] does not exist."
            }

            If (-not ($Verbs.$Action)) {
                Throw "Action [$Action] not supported. Supported actions are [$($Verbs.Keys -join ', ')]."
            }

            If ($Action.Contains('StartMenu')) {
                If ([Int32]$envOSVersionMajor -ge 10)	{
                    If ((Get-Item -Path $FilePath).Extension -ne '.lnk') {
                        Throw 'Only shortcut files (.lnk) are supported on Windows 10 and higher.'
                    }
                    ElseIf (-not ($FilePath.StartsWith($envUserStartMenu, 'OrdinalIgnoreCase') -or $FilePath.StartsWith($envCommonStartMenu, 'OrdinalIgnoreCase'))) {
                        Throw "Only shortcut files (.lnk) in [$envUserStartMenu] and [$envCommonStartMenu] are supported on Windows 10 and higher."
                    }
                }

                [String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
                If (-not $PinVerbAction) {
                    Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
                }

                Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
            }
            ElseIf ($Action.Contains('Taskbar')) {
                If ([Int32]$envOSVersionMajor -ge 10) {
                    $FileNameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
                    $PinExists = Test-Path -Path "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk"

                    If (($Action -eq 'PinToTaskbar') -and ($PinExists)) {
                        If ($(Invoke-ObjectMethod -InputObject $Shell -MethodName 'CreateShortcut' -ArgumentList "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk").TargetPath -eq $FilePath) {
                            Write-Log -Message "Pin [$FileNameWithoutExtension] already exists." -Source ${CmdletName}
                            Return
                        }
                    }
                    ElseIf (($Action -eq 'UnpinFromTaskbar') -and ($PinExists -eq $false)) {
                        Write-Log -Message "Pin [$FileNameWithoutExtension] does not exist." -Source ${CmdletName}
                        Return
                    }

                    $ExplorerCommandHandler = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\Windows.taskbarpin' -Value 'ExplorerCommandHandler'
                    $classesStarKey = (Get-Item "Registry::HKEY_USERS\$($RunasActiveUser.SID)\SOFTWARE\Classes").OpenSubKey('*', $true)
                    $shellKey = $classesStarKey.CreateSubKey('shell', $true)
                    $specialKey = $shellKey.CreateSubKey('{:}', $true)
                    $specialKey.SetValue('ExplorerCommandHandler', $ExplorerCommandHandler)

                    $Folder = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'Namespace' -ArgumentList $(Split-Path -Path $FilePath -Parent)
                    $Item = Invoke-ObjectMethod -InputObject $Folder -MethodName 'ParseName' -ArgumentList $(Split-Path -Path $FilePath -Leaf)

                    $Item.InvokeVerb('{:}')

                    $shellKey.DeleteSubKey('{:}')
                    If ($shellKey.SubKeyCount -eq 0 -and $shellKey.ValueCount -eq 0) {
                        $classesStarKey.DeleteSubKey('shell')
                    }
                }
                Else {
                    [String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
                    If (-not $PinVerbAction) {
                        Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
                    }

                    Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to execute action [$Action]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
        }
        Finally {
            Try {
                If ($shellKey) {
                    $shellKey.Close()
                }
            }
            Catch {
            }
            Try {
                If ($classesStarKey) {
                    $classesStarKey.Close()
                }
            }
            Catch {
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-IniValue
Function Get-IniValue {
    <#
.SYNOPSIS

Parses an INI file and returns the value of the specified section and key.

.DESCRIPTION

Parses an INI file and returns the value of the specified section and key.

.PARAMETER FilePath

Path to the INI file.

.PARAMETER Section

Section within the INI file.

.PARAMETER Key

Key within the section of the INI file.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the value of the specified section and key.

.EXAMPLE

Get-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$FilePath,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Section,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Reading INI Key: [Section = $Section] [Key = $Key]." -Source ${CmdletName}

            If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf')) {
                Throw "File [$filePath] could not be found."
            }

            $IniValue = [PSADT.IniFile]::GetIniValue($Section, $Key, $FilePath)
            Write-Log -Message "INI Key Value: [Section = $Section] [Key = $Key] [Value = $IniValue]." -Source ${CmdletName}

            Write-Output -InputObject ($IniValue)
        }
        Catch {
            Write-Log -Message "Failed to read INI file key value. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to read INI file key value: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Set-IniValue
Function Set-IniValue {
    <#
.SYNOPSIS

Opens an INI file and sets the value of the specified section and key.

.DESCRIPTION

Opens an INI file and sets the value of the specified section and key.

.PARAMETER FilePath

Path to the INI file.

.PARAMETER Section

Section within the INI file.

.PARAMETER Key

Key within the section of the INI file.

.PARAMETER Value

Value for the key within the section of the INI file. To remove a value, set this variable to $null.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any output.

.EXAMPLE

Set-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName' -Value 'MyFile.ID'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$FilePath,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Section,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Key,
        # Don't strongly type this variable as [String] b/c PowerShell replaces [String]$Value = $null with an empty string
        [Parameter(Mandatory = $true)]
        [AllowNull()]
        $Value,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Writing INI Key Value: [Section = $Section] [Key = $Key] [Value = $Value]." -Source ${CmdletName}

            If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf')) {
                Throw "File [$filePath] could not be found."
            }

            [PSADT.IniFile]::SetIniValue($Section, $Key, ([Text.StringBuilder]$Value), $FilePath)
        }
        Catch {
            Write-Log -Message "Failed to write INI file key value. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to write INI file key value: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-PEFileArchitecture
Function Get-PEFileArchitecture {
    <#
.SYNOPSIS

Determine if a PE file is a 32-bit or a 64-bit file.

.DESCRIPTION

Determine if a PE file is a 32-bit or a 64-bit file by examining the file's image file header.

PE file extensions: .exe, .dll, .ocx, .drv, .sys, .scr, .efi, .cpl, .fon

.PARAMETER FilePath

Path to the PE file to examine.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.PARAMETER PassThru

Get the file object, attach a property indicating the file binary type, and write to pipeline

.INPUTS

System.IO.FileInfo.

Accepts a FileInfo object from the pipeline.

.OUTPUTS

System.String

Returns a string indicating the file binary type.

.EXAMPLE

Get-PEFileArchitecture -FilePath "$env:windir\notepad.exe"

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [IO.FileInfo[]]$FilePath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true,
        [Parameter(Mandatory = $false)]
        [Switch]$PassThru
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        [String[]]$PEFileExtensions = '.exe', '.dll', '.ocx', '.drv', '.sys', '.scr', '.efi', '.cpl', '.fon'
        [Int32]$MACHINE_OFFSET = 4
        [Int32]$PE_POINTER_OFFSET = 60
    }
    Process {
        ForEach ($Path in $filePath) {
            Try {
                If ($PEFileExtensions -notcontains $Path.Extension) {
                    Throw "Invalid file type. Please specify one of the following PE file types: $($PEFileExtensions -join ', ')"
                }

                [Byte[]]$data = New-Object -TypeName 'System.Byte[]' -ArgumentList (4096)
                $stream = New-Object -TypeName 'System.IO.FileStream' -ArgumentList ($Path.FullName, 'Open', 'Read')
                $null = $stream.Read($data, 0, 4096)
                $stream.Flush()
                $stream.Close()

                [Int32]$PE_HEADER_ADDR = [BitConverter]::ToInt32($data, $PE_POINTER_OFFSET)
                [UInt16]$PE_IMAGE_FILE_HEADER = [BitConverter]::ToUInt16($data, $PE_HEADER_ADDR + $MACHINE_OFFSET)
                Switch ($PE_IMAGE_FILE_HEADER) {
                    0 {
                        $PEArchitecture = 'Native'
                    } # The contents of this file are assumed to be applicable to any machine type
                    0x014c {
                        $PEArchitecture = '32BIT'
                    } # File for Windows 32-bit systems
                    0x0200 {
                        $PEArchitecture = 'Itanium-x64'
                    } # File for Intel Itanium x64 processor family
                    0x8664 {
                        $PEArchitecture = '64BIT'
                    } # File for Windows 64-bit systems
                    Default {
                        $PEArchitecture = 'Unknown'
                    }
                }
                Write-Log -Message "File [$($Path.FullName)] has a detected file architecture of [$PEArchitecture]." -Source ${CmdletName}

                If ($PassThru) {
                    #  Get the file object, attach a property indicating the type, and write to pipeline
                    Get-Item -LiteralPath $Path.FullName -Force | Add-Member -MemberType 'NoteProperty' -Name 'BinaryType' -Value $PEArchitecture -Force -PassThru | Write-Output
                }
                Else {
                    Write-Output -InputObject ($PEArchitecture)
                }
            }
            Catch {
                Write-Log -Message "Failed to get the PE file architecture. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "Failed to get the PE file architecture: $($_.Exception.Message)"
                }
                Continue
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Invoke-RegisterOrUnregisterDLL
Function Invoke-RegisterOrUnregisterDLL {
    <#
.SYNOPSIS

Register or unregister a DLL file.

.DESCRIPTION

Register or unregister a DLL file using regsvr32.exe. Function can be invoked using alias: 'Register-DLL' or 'Unregister-DLL'.

.PARAMETER FilePath

Path to the DLL file.

.PARAMETER DLLAction

Specify whether to register or unregister the DLL. Optional if function is invoked using 'Register-DLL' or 'Unregister-DLL' alias.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return objects.

.EXAMPLE

Register-DLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

Register DLL file using the "Register-DLL" alias for this function

.EXAMPLE

UnRegister-DLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

Unregister DLL file using the "Unregister-DLL" alias for this function

.EXAMPLE

Invoke-RegisterOrUnregisterDLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll" -DLLAction 'Register'

Register DLL file using the actual name of this function

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$FilePath,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Register', 'Unregister')]
        [String]$DLLAction,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Get name used to invoke this function in case the 'Register-DLL' or 'Unregister-DLL' alias was used and set the correct DLL action
        [String]${InvokedCmdletName} = $MyInvocation.InvocationName
        #  Set the correct register/unregister action based on the alias used to invoke this function
        If (${InvokedCmdletName} -ne ${CmdletName}) {
            Switch (${InvokedCmdletName}) {
                'Register-DLL' {
                    [String]$DLLAction = 'Register'
                }
                'Unregister-DLL' {
                    [String]$DLLAction = 'Unregister'
                }
            }
        }
        #  Set the correct DLL register/unregister action parameters
        If (-not $DLLAction) {
            Throw 'Parameter validation failed. Please specify the [-DLLAction] parameter to determine whether to register or unregister the DLL.'
        }
        [String]$DLLAction = ((Get-Culture).TextInfo).ToTitleCase($DLLAction.ToLower())
        Switch ($DLLAction) {
            'Register' {
                [String]$DLLActionParameters = "/s `"$FilePath`""
            }
            'Unregister' {
                [String]$DLLActionParameters = "/s /u `"$FilePath`""
            }
        }
    }
    Process {
        Try {
            Write-Log -Message "$DLLAction DLL file [$filePath]." -Source ${CmdletName}
            If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf')) {
                Throw "File [$filePath] could not be found."
            }

            [String]$DLLFileBitness = Get-PEFileArchitecture -FilePath $filePath -ContinueOnError $false -ErrorAction 'Stop'
            If (($DLLFileBitness -ne '64BIT') -and ($DLLFileBitness -ne '32BIT')) {
                Throw "File [$filePath] has a detected file architecture of [$DLLFileBitness]. Only 32-bit or 64-bit DLL files can be $($DLLAction.ToLower() + 'ed')."
            }

            If ($Is64Bit) {
                If ($DLLFileBitness -eq '64BIT') {
                    If ($Is64BitProcess) {
                        [String]$RegSvr32Path = "$envWinDir\System32\regsvr32.exe"
                    }
                    Else {
                        [String]$RegSvr32Path = "$envWinDir\Sysnative\regsvr32.exe"
                    }
                }
                ElseIf ($DLLFileBitness -eq '32BIT') {
                    [String]$RegSvr32Path = "$envWinDir\SysWOW64\regsvr32.exe"
                }
            }
            Else {
                If ($DLLFileBitness -eq '64BIT') {
                    Throw "File [$filePath] cannot be $($DLLAction.ToLower()) because it is a 64-bit file on a 32-bit operating system."
                }
                ElseIf ($DLLFileBitness -eq '32BIT') {
                    [String]$RegSvr32Path = "$envWinDir\System32\regsvr32.exe"
                }
            }

            [PSObject]$ExecuteResult = Execute-Process -Path $RegSvr32Path -Parameters $DLLActionParameters -WindowStyle 'Hidden' -PassThru -ExitOnProcessFailure $false

            If ($ExecuteResult.ExitCode -ne 0) {
                If ($ExecuteResult.ExitCode -eq 60002) {
                    Throw "Execute-Process function failed with exit code [$($ExecuteResult.ExitCode)]."
                }
                Else {
                    Throw "regsvr32.exe failed with exit code [$($ExecuteResult.ExitCode)]."
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to $($DLLAction.ToLower()) DLL file. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to $($DLLAction.ToLower()) DLL file: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
Set-Alias -Name 'Register-DLL' -Value 'Invoke-RegisterOrUnregisterDLL' -Scope 'Script' -Force -ErrorAction 'SilentlyContinue'
Set-Alias -Name 'Unregister-DLL' -Value 'Invoke-RegisterOrUnregisterDLL' -Scope 'Script' -Force -ErrorAction 'SilentlyContinue'
#endregion


#region Function Invoke-ObjectMethod
Function Invoke-ObjectMethod {
    <#
.SYNOPSIS

Invoke method on any object.

.DESCRIPTION

Invoke method on any object with or without using named parameters.

.PARAMETER InputObject

Specifies an object which has methods that can be invoked.

.PARAMETER MethodName

Specifies the name of a method to invoke.

.PARAMETER ArgumentList

Argument to pass to the method being executed. Allows execution of method without specifying named parameters.

.PARAMETER Parameter

Argument to pass to the method being executed. Allows execution of method by using named parameters.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Object.

The object returned by the method being invoked.

.EXAMPLE

$ShellApp = New-Object -ComObject 'Shell.Application'
$null = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'MinimizeAll'

Minimizes all windows.

.EXAMPLE

$ShellApp = New-Object -ComObject 'Shell.Application'

$null = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'Explore' -Parameter @{'vDir'='C:\Windows'}

Opens the C:\Windows folder in a Windows Explorer window.

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'Positional')]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNull()]
        [Object]$InputObject,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$MethodName,
        [Parameter(Mandatory = $false, Position = 2, ParameterSetName = 'Positional')]
        [Object[]]$ArgumentList,
        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'Named')]
        [ValidateNotNull()]
        [Hashtable]$Parameter
    )

    Begin {
    }
    Process {
        If ($PSCmdlet.ParameterSetName -eq 'Named') {
            ## Invoke method by using parameter names
            Write-Output -InputObject ($InputObject.GetType().InvokeMember($MethodName, [Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, ([Object[]]($Parameter.Values)), $null, $null, ([String[]]($Parameter.Keys))))
        }
        Else {
            ## Invoke method without using parameter names
            Write-Output -InputObject ($InputObject.GetType().InvokeMember($MethodName, [Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, $ArgumentList, $null, $null, $null))
        }
    }
    End {
    }
}
#endregion


#region Function Get-ObjectProperty
Function Get-ObjectProperty {
    <#
.SYNOPSIS

Get a property from any object.

.DESCRIPTION

Get a property from any object.

.PARAMETER InputObject

Specifies an object which has properties that can be retrieved.

.PARAMETER PropertyName

Specifies the name of a property to retrieve.

.PARAMETER ArgumentList

Argument to pass to the property being retrieved.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Object.

Returns the value of the property being retrieved.

.EXAMPLE

Get-ObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @(1)

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNull()]
        [Object]$InputObject,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$PropertyName,
        [Parameter(Mandatory = $false, Position = 2)]
        [Object[]]$ArgumentList
    )

    Begin {
    }
    Process {
        ## Retrieve property
        Write-Output -InputObject ($InputObject.GetType().InvokeMember($PropertyName, [Reflection.BindingFlags]::GetProperty, $null, $InputObject, $ArgumentList, $null, $null, $null))
    }
    End {
    }
}
#endregion


#region Function Get-MsiTableProperty
Function Get-MsiTableProperty {
    <#
.SYNOPSIS

Get all of the properties from a Windows Installer database table or the Summary Information stream and return as a custom object.

.DESCRIPTION

Use the Windows Installer object to read all of the properties from a Windows Installer database table or the Summary Information stream.

.PARAMETER Path

The fully qualified path to an database file. Supports .msi and .msp files.

.PARAMETER TransformPath

The fully qualified path to a list of MST file(s) which should be applied to the MSI file.

.PARAMETER Table

The name of the the MSI table from which all of the properties must be retrieved. Default is: 'Property'.

.PARAMETER TablePropertyNameColumnNum

Specify the table column number which contains the name of the properties. Default is: 1 for MSIs and 2 for MSPs.

.PARAMETER TablePropertyValueColumnNum

Specify the table column number which contains the value of the properties. Default is: 2 for MSIs and 3 for MSPs.

.PARAMETER GetSummaryInformation

Retrieves the Summary Information for the Windows Installer database.

Summary Information property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Management.Automation.PSObject

Returns a custom object with the following properties: 'Name' and 'Value'.

.EXAMPLE

Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst'

Retrieve all of the properties from the default 'Property' table.

.EXAMPLE

Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst' -Table 'Property' | Select-Object -ExpandProperty ProductCode

Retrieve all of the properties from the 'Property' table and then pipe to Select-Object to select the ProductCode property.

.EXAMPLE

Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -GetSummaryInformation

Retrieves the Summary Information for the Windows Installer database.

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'TableInfo')]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String[]]$TransformPath,
        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullOrEmpty()]
        [String]$Table = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') {
                'Property'
            }
            Else {
                'MsiPatchMetadata'
            }),
        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullorEmpty()]
        [Int32]$TablePropertyNameColumnNum = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') {
                1
            }
            Else {
                2
            }),
        [Parameter(Mandatory = $false, ParameterSetName = 'TableInfo')]
        [ValidateNotNullorEmpty()]
        [Int32]$TablePropertyValueColumnNum = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') {
                2
            }
            Else {
                3
            }),
        [Parameter(Mandatory = $true, ParameterSetName = 'SummaryInfo')]
        [ValidateNotNullorEmpty()]
        [Switch]$GetSummaryInformation = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            If ($PSCmdlet.ParameterSetName -eq 'TableInfo') {
                Write-Log -Message "Reading data from Windows Installer database file [$Path] in table [$Table]." -Source ${CmdletName}
            }
            Else {
                Write-Log -Message "Reading the Summary Information from the Windows Installer database file [$Path]." -Source ${CmdletName}
            }

            ## Create a Windows Installer object
            [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'
            ## Determine if the database file is a patch (.msp) or not
            If ([IO.Path]::GetExtension($Path) -eq '.msp') {
                [Boolean]$IsMspFile = $true
            }
            ## Define properties for how the MSI database is opened
            [Int32]$msiOpenDatabaseModeReadOnly = 0
            [Int32]$msiSuppressApplyTransformErrors = 63
            [Int32]$msiOpenDatabaseMode = $msiOpenDatabaseModeReadOnly
            [Int32]$msiOpenDatabaseModePatchFile = 32
            If ($IsMspFile) {
                [Int32]$msiOpenDatabaseMode = $msiOpenDatabaseModePatchFile
            }
            ## Open database in read only mode
            [__ComObject]$Database = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($Path, $msiOpenDatabaseMode)
            ## Apply a list of transform(s) to the database
            If (($TransformPath) -and (-not $IsMspFile)) {
                ForEach ($Transform in $TransformPath) {
                    $null = Invoke-ObjectMethod -InputObject $Database -MethodName 'ApplyTransform' -ArgumentList @($Transform, $msiSuppressApplyTransformErrors)
                }
            }

            ## Get either the requested windows database table information or summary information
            If ($PSCmdlet.ParameterSetName -eq 'TableInfo') {
                ## Open the requested table view from the database
                [__ComObject]$View = Invoke-ObjectMethod -InputObject $Database -MethodName 'OpenView' -ArgumentList @("SELECT * FROM $Table")
                $null = Invoke-ObjectMethod -InputObject $View -MethodName 'Execute'

                ## Create an empty object to store properties in
                [PSObject]$TableProperties = New-Object -TypeName 'PSObject'

                ## Retrieve the first row from the requested table. If the first row was successfully retrieved, then save data and loop through the entire table.
                #  https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
                [__ComObject]$Record = Invoke-ObjectMethod -InputObject $View -MethodName 'Fetch'
                While ($Record) {
                    #  Read string data from record and add property/value pair to custom object
                    $TableProperties | Add-Member -MemberType 'NoteProperty' -Name (Get-ObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @($TablePropertyNameColumnNum)) -Value (Get-ObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @($TablePropertyValueColumnNum)) -Force
                    #  Retrieve the next row in the table
                    [__ComObject]$Record = Invoke-ObjectMethod -InputObject $View -MethodName 'Fetch'
                }
                Write-Output -InputObject ($TableProperties)
            }
            Else {
                ## Get the SummaryInformation from the windows installer database
                [__ComObject]$SummaryInformation = Get-ObjectProperty -InputObject $Database -PropertyName 'SummaryInformation'
                [Hashtable]$SummaryInfoProperty = @{}
                ## Summary property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx
                $SummaryInfoProperty.Add('CodePage', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(1)))
                $SummaryInfoProperty.Add('Title', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(2)))
                $SummaryInfoProperty.Add('Subject', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(3)))
                $SummaryInfoProperty.Add('Author', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(4)))
                $SummaryInfoProperty.Add('Keywords', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(5)))
                $SummaryInfoProperty.Add('Comments', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(6)))
                $SummaryInfoProperty.Add('Template', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(7)))
                $SummaryInfoProperty.Add('LastSavedBy', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(8)))
                $SummaryInfoProperty.Add('RevisionNumber', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(9)))
                $SummaryInfoProperty.Add('LastPrinted', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(11)))
                $SummaryInfoProperty.Add('CreateTimeDate', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(12)))
                $SummaryInfoProperty.Add('LastSaveTimeDate', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(13)))
                $SummaryInfoProperty.Add('PageCount', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(14)))
                $SummaryInfoProperty.Add('WordCount', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(15)))
                $SummaryInfoProperty.Add('CharacterCount', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(16)))
                $SummaryInfoProperty.Add('CreatingApplication', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(18)))
                $SummaryInfoProperty.Add('Security', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(19)))
                [PSObject]$SummaryInfoProperties = New-Object -TypeName 'PSObject' -Property $SummaryInfoProperty
                Write-Output -InputObject ($SummaryInfoProperties)
            }
        }
        Catch {
            Write-Log -Message "Failed to get the MSI table [$Table]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to get the MSI table [$Table]: $($_.Exception.Message)"
            }
        }
        Finally {
            Try {
                If ($View) {
                    $null = Invoke-ObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
                    Try {
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)
                    }
                    Catch {
                    }
                }
                ElseIf ($SummaryInformation) {
                    Try {
                        $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SummaryInformation)
                    }
                    Catch {
                    }
                }
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($DataBase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
            }
            Catch {
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Set-MsiProperty
Function Set-MsiProperty {
    <#
.SYNOPSIS

Set a property in the MSI property table.

.DESCRIPTION

Set a property in the MSI property table.

.PARAMETER DataBase

Specify a ComObject representing an MSI database opened in view/modify/update mode.

.PARAMETER PropertyName

The name of the property to be set/modified.

.PARAMETER PropertyValue

The value of the property to be set/modified.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-MsiProperty -DataBase $TempMsiPathDatabase -PropertyName 'ALLUSERS' -PropertyValue '1'

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [__ComObject]$DataBase,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$PropertyName,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$PropertyValue,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Setting the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]." -Source ${CmdletName}

            ## Open the requested table view from the database
            [__ComObject]$View = Invoke-ObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("SELECT * FROM Property WHERE Property='$PropertyName'")
            $null = Invoke-ObjectMethod -InputObject $View -MethodName 'Execute'

            ## Retrieve the requested property from the requested table.
            #  https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
            [__ComObject]$Record = Invoke-ObjectMethod -InputObject $View -MethodName 'Fetch'

            ## Close the previous view on the MSI database
            $null = Invoke-ObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)

            ## Set the MSI property
            If ($Record) {
                #  If the property already exists, then create the view for updating the property
                [__ComObject]$View = Invoke-ObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("UPDATE Property SET Value='$PropertyValue' WHERE Property='$PropertyName'")
            }
            Else {
                #  If property does not exist, then create view for inserting the property
                [__ComObject]$View = Invoke-ObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("INSERT INTO Property (Property, Value) VALUES ('$PropertyName','$PropertyValue')")
            }
            #  Execute the view to set the MSI property
            $null = Invoke-ObjectMethod -InputObject $View -MethodName 'Execute'
        }
        Catch {
            Write-Log -Message "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]: $($_.Exception.Message)"
            }
        }
        Finally {
            Try {
                If ($View) {
                    $null = Invoke-ObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)
                }
            }
            Catch {
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function New-MsiTransform
Function New-MsiTransform {
    <#
.SYNOPSIS

Create a transform file for an MSI database.

.DESCRIPTION

Create a transform file for an MSI database and create/modify properties in the Properties table.

.PARAMETER MsiPath

Specify the path to an MSI file.

.PARAMETER ApplyTransformPath

Specify the path to a transform which should be applied to the MSI database before any new properties are created or modified.

.PARAMETER NewTransformPath

Specify the path where the new transform file with the desired properties will be created. If a transform file of the same name already exists, it will be deleted before a new one is created.

Default is: a) If -ApplyTransformPath was specified but not -NewTransformPath, then <ApplyTransformPath>.new.mst
				b) If only -MsiPath was specified, then <MsiPath>.mst

.PARAMETER TransformProperties

Hashtable which contains calls to Set-MsiProperty for configuring the desired properties which should be included in new transform file.

Example hashtable: [Hashtable]$TransformProperties = @{ 'ALLUSERS' = '1' }

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE
	[Hashtable]$TransformProperties = {
		'ALLUSERS' = '1'
		'AgreeToLicense' = 'Yes'
		'REBOOT' = 'ReallySuppress'
		'RebootYesNo' = 'No'
		'ROOTDRIVE' = 'C:'
	}
	New-MsiTransform -MsiPath 'C:\Temp\PSADTInstall.msi' -TransformProperties $TransformProperties

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$MsiPath,
        [Parameter(Mandatory = $false)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$ApplyTransformPath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$NewTransformPath,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [Hashtable]$TransformProperties,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Define properties for how the MSI database is opened
        [Int32]$msiOpenDatabaseModeReadOnly = 0
        [Int32]$msiOpenDatabaseModeTransact = 1
        [Int32]$msiViewModifyUpdate = 2
        [Int32]$msiViewModifyReplace = 4
        [Int32]$msiViewModifyDelete = 6
        [Int32]$msiTransformErrorNone = 0
        [Int32]$msiTransformValidationNone = 0
        [Int32]$msiSuppressApplyTransformErrors = 63
    }
    Process {
        Try {
            Write-Log -Message "Creating a transform file for MSI [$MsiPath]." -Source ${CmdletName}

            ## Discover the parent folder that the MSI file resides in
            [String]$MsiParentFolder = Split-Path -Path $MsiPath -Parent -ErrorAction 'Stop'

            ## Create a temporary file name for storing a second copy of the MSI database
            [String]$TempMsiPath = Join-Path -Path $MsiParentFolder -ChildPath ([IO.Path]::GetFileName(([IO.Path]::GetTempFileName()))) -ErrorAction 'Stop'

            ## Create a second copy of the MSI database
            Write-Log -Message "Copying MSI database in path [$MsiPath] to destination [$TempMsiPath]." -Source ${CmdletName}
            $null = Copy-Item -LiteralPath $MsiPath -Destination $TempMsiPath -Force -ErrorAction 'Stop'

            ## Create a Windows Installer object
            [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'

            ## Open both copies of the MSI database
            #  Open the original MSI database in read only mode
            Write-Log -Message "Opening the MSI database [$MsiPath] in read only mode." -Source ${CmdletName}
            [__ComObject]$MsiPathDatabase = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($MsiPath, $msiOpenDatabaseModeReadOnly)
            #  Open the temporary copy of the MSI database in view/modify/update mode
            Write-Log -Message "Opening the MSI database [$TempMsiPath] in view/modify/update mode." -Source ${CmdletName}
            [__ComObject]$TempMsiPathDatabase = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($TempMsiPath, $msiViewModifyUpdate)

            ## If a MSI transform file was specified, then apply it to the temporary copy of the MSI database
            If ($ApplyTransformPath) {
                Write-Log -Message "Applying transform file [$ApplyTransformPath] to MSI database [$TempMsiPath]." -Source ${CmdletName}
                $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'ApplyTransform' -ArgumentList @($ApplyTransformPath, $msiSuppressApplyTransformErrors)
            }

            ## Determine the path for the new transform file that will be generated
            If (-not $NewTransformPath) {
                If ($ApplyTransformPath) {
                    [String]$NewTransformFileName = [IO.Path]::GetFileNameWithoutExtension($ApplyTransformPath) + '.new' + [IO.Path]::GetExtension($ApplyTransformPath)
                }
                Else {
                    [String]$NewTransformFileName = [IO.Path]::GetFileNameWithoutExtension($MsiPath) + '.mst'
                }
                [String]$NewTransformPath = Join-Path -Path $MsiParentFolder -ChildPath $NewTransformFileName -ErrorAction 'Stop'
            }

            ## Set the MSI properties in the temporary copy of the MSI database
            $TransformProperties.GetEnumerator() | ForEach-Object { Set-MsiProperty -DataBase $TempMsiPathDatabase -PropertyName $_.Key -PropertyValue $_.Value }

            ## Commit the new properties to the temporary copy of the MSI database
            $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'Commit'

            ## Reopen the temporary copy of the MSI database in read only mode
            #  Release the database object for the temporary copy of the MSI database
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
            #  Open the temporary copy of the MSI database in read only mode
            Write-Log -Message "Re-opening the MSI database [$TempMsiPath] in read only mode." -Source ${CmdletName}
            [__ComObject]$TempMsiPathDatabase = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($TempMsiPath, $msiOpenDatabaseModeReadOnly)

            ## Delete the new transform file path if it already exists
            If (Test-Path -LiteralPath $NewTransformPath -PathType 'Leaf' -ErrorAction 'Stop') {
                Write-Log -Message "A transform file of the same name already exists. Deleting transform file [$NewTransformPath]." -Source ${CmdletName}
                $null = Remove-Item -LiteralPath $NewTransformPath -Force -ErrorAction 'Stop'
            }

            ## Generate the new transform file by taking the difference between the temporary copy of the MSI database and the original MSI database
            Write-Log -Message "Generating new transform file [$NewTransformPath]." -Source ${CmdletName}
            $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'GenerateTransform' -ArgumentList @($MsiPathDatabase, $NewTransformPath)
            $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'CreateTransformSummaryInfo' -ArgumentList @($MsiPathDatabase, $NewTransformPath, $msiTransformErrorNone, $msiTransformValidationNone)

            If (Test-Path -LiteralPath $NewTransformPath -PathType 'Leaf' -ErrorAction 'Stop') {
                Write-Log -Message "Successfully created new transform file in path [$NewTransformPath]." -Source ${CmdletName}
            }
            Else {
                Throw "Failed to generate transform file in path [$NewTransformPath]."
            }
        }
        Catch {
            Write-Log -Message "Failed to create new transform file in path [$NewTransformPath]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to create new transform file in path [$NewTransformPath]: $($_.Exception.Message)"
            }
        }
        Finally {
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($MsiPathDatabase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
            }
            Catch {
            }
            Try {
                ## Delete the temporary copy of the MSI database
                If (Test-Path -LiteralPath $TempMsiPath -PathType 'Leaf' -ErrorAction 'Stop') {
                    $null = Remove-Item -LiteralPath $TempMsiPath -Force -ErrorAction 'Stop'
                }
            }
            Catch {
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Test-MSUpdates
Function Test-MSUpdates {
    <#
.SYNOPSIS

Test whether a Microsoft Windows update is installed.

.DESCRIPTION

Test whether a Microsoft Windows update is installed.

.PARAMETER KBNumber

KBNumber of the update.

.PARAMETER ContinueOnError

Suppress writing log message to console on failure to write message to log file. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if the update is installed, otherwise returns $false.

.EXAMPLE

Test-MSUpdates -KBNumber 'KB2549864'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter the KB Number for the Microsoft Update')]
        [ValidateNotNullorEmpty()]
        [String]$KBNumber,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Checking if Microsoft Update [$kbNumber] is installed." -Source ${CmdletName}

            ## Default is not found
            [Boolean]$kbFound = $false

            ## Check for update using built in PS cmdlet which uses WMI in the background to gather details
            Get-HotFix -Id $kbNumber -ErrorAction 'SilentlyContinue' | ForEach-Object { $kbFound = $true }

            If (-not $kbFound) {
                Write-Log -Message 'Unable to detect Windows update history via Get-Hotfix cmdlet. Trying via COM object.' -Source ${CmdletName}

                ## Check for update using ComObject method (to catch Office updates)
                [__ComObject]$UpdateSession = New-Object -ComObject 'Microsoft.Update.Session'
                [__ComObject]$UpdateSearcher = $UpdateSession.CreateUpdateSearcher()
                #  Indicates whether the search results include updates that are superseded by other updates in the search results
                $UpdateSearcher.IncludePotentiallySupersededUpdates = $false
                #  Indicates whether the UpdateSearcher goes online to search for updates.
                $UpdateSearcher.Online = $false
                [Int32]$UpdateHistoryCount = $UpdateSearcher.GetTotalHistoryCount()
                If ($UpdateHistoryCount -gt 0) {
                    [PSObject]$UpdateHistory = $UpdateSearcher.QueryHistory(0, $UpdateHistoryCount) |
                        Select-Object -Property 'Title', 'Date',
                        @{Name = 'Operation'; Expression = { Switch ($_.Operation) {
                                    1 {
                                        'Installation'
                                    }; 2 {
                                        'Uninstallation'
                                    }; 3 {
                                        'Other'
                                    }
                                } }
                        },
                        @{Name = 'Status'; Expression = { Switch ($_.ResultCode) {
                                    0 {
                                        'Not Started'
                                    }; 1 {
                                        'In Progress'
                                    }; 2 {
                                        'Successful'
                                    }; 3 {
                                        'Incomplete'
                                    }; 4 {
                                        'Failed'
                                    }; 5 {
                                        'Aborted'
                                    }
                                } }
                        },
                        'Description' |
                        Sort-Object -Property 'Date' -Descending
                    ForEach ($Update in $UpdateHistory) {
                        If (($Update.Operation -ne 'Other') -and ($Update.Title -match "\($KBNumber\)")) {
                            $LatestUpdateHistory = $Update
                            Break
                        }
                    }
                    If (($LatestUpdateHistory.Operation -eq 'Installation') -and ($LatestUpdateHistory.Status -eq 'Successful')) {
                        Write-Log -Message "Discovered the following Microsoft Update: `r`n$($LatestUpdateHistory | Format-List | Out-String)" -Source ${CmdletName}
                        $kbFound = $true
                    }
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($UpdateSession)
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($UpdateSearcher)
                }
                Else {
                    Write-Log -Message 'Unable to detect Windows update history via COM object.' -Source ${CmdletName}
                }
            }

            ## Return Result
            If (-not $kbFound) {
                Write-Log -Message "Microsoft Update [$kbNumber] is not installed." -Source ${CmdletName}
                Write-Output -InputObject ($false)
            }
            Else {
                Write-Log -Message "Microsoft Update [$kbNumber] is installed." -Source ${CmdletName}
                Write-Output -InputObject ($true)
            }
        }
        Catch {
            Write-Log -Message "Failed discovering Microsoft Update [$kbNumber]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed discovering Microsoft Update [$kbNumber]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Install-MSUpdates
Function Install-MSUpdates {
    <#
.SYNOPSIS

Install all Microsoft Updates in a given directory.

.DESCRIPTION

Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory).

.PARAMETER Directory

Directory containing the updates.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Install-MSUpdates -Directory "$dirFiles\MSUpdates"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Directory
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Write-Log -Message "Recursively installing all Microsoft Updates in directory [$Directory]." -Source ${CmdletName}

        ## KB Number pattern match
        $kbPattern = '(?i)kb\d{6,8}'

        ## Get all hotfixes and install if required
        [IO.FileInfo[]]$files = Get-ChildItem -LiteralPath $Directory -Recurse -Include ('*.exe', '*.msu', '*.msp')
        ForEach ($file in $files) {
            If ($file.Name -match 'redist') {
                [Version]$redistVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).ProductVersion
                [String]$redistDescription = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).FileDescription

                Write-Log -Message "Installing [$redistDescription $redistVersion]..." -Source ${CmdletName}
                #  Handle older redistributables (ie, VC++ 2005)
                If ($redistDescription -match 'Win32 Cabinet Self-Extractor') {
                    Execute-Process -Path $file.FullName -Parameters '/q' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
                Else {
                    Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
            }
            Else {
                #  Get the KB number of the file
                [String]$kbNumber = [RegEx]::Match($file.Name, $kbPattern).ToString()
                If (-not $kbNumber) {
                    Continue
                }

                #  Check to see whether the KB is already installed
                If (-not (Test-MSUpdates -KBNumber $kbNumber)) {
                    Write-Log -Message "KB Number [$KBNumber] was not detected and will be installed." -Source ${CmdletName}
                    Switch ($file.Extension) {
                        #  Installation type for executables (i.e., Microsoft Office Updates)
                        '.exe' {
                            Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                        #  Installation type for Windows updates using Windows Update Standalone Installer
                        '.msu' {
                            Execute-Process -Path $exeWusa -Parameters "`"$($file.FullName)`" /quiet /norestart" -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                        #  Installation type for Windows Installer Patch
                        '.msp' {
                            Execute-MSI -Action 'Patch' -Path $file.FullName -IgnoreExitCodes '*'
                        }
                    }
                }
                Else {
                    Write-Log -Message "KB Number [$kbNumber] is already installed. Continue..." -Source ${CmdletName}
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-WindowTitle
Function Get-WindowTitle {
    <#
.SYNOPSIS

Search for an open window title and return details about the window.

.DESCRIPTION

Search for a window title. If window title searched for returns more than one result, then details for each window will be displayed.

Returns the following properties for each window: WindowTitle, WindowHandle, ParentProcess, ParentProcessMainWindowHandle, ParentProcessId.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

.PARAMETER WindowTitle

The title of the application window to search for using regex matching.

.PARAMETER GetAllWindowTitles

Get titles for all open windows on the system.

.PARAMETER DisableFunctionLogging

Disables logging messages to the script log file.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Management.Automation.PSObject

Returns a PSObject with the following properties: WindowTitle, WindowHandle, ParentProcess, ParentProcessMainWindowHandle, ParentProcessId.

.EXAMPLE

Get-WindowTitle -WindowTitle 'Microsoft Word'

Gets details for each window that has the words "Microsoft Word" in the title.

.EXAMPLE

Get-WindowTitle -GetAllWindowTitles

Gets details for all windows with a title.

.EXAMPLE

Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.ParentProcess -eq 'WINWORD' }

Get details for all windows belonging to Microsoft Word process with name "WINWORD".

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinTitle')]
        [AllowEmptyString()]
        [String]$WindowTitle,
        [Parameter(Mandatory = $true, ParameterSetName = 'GetAllWinTitles')]
        [ValidateNotNullorEmpty()]
        [Switch]$GetAllWindowTitles = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$DisableFunctionLogging = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            If ($PSCmdlet.ParameterSetName -eq 'SearchWinTitle') {
                If (-not $DisableFunctionLogging) {
                    Write-Log -Message "Finding open window title(s) [$WindowTitle] using regex matching." -Source ${CmdletName}
                }
            }
            ElseIf ($PSCmdlet.ParameterSetName -eq 'GetAllWinTitles') {
                If (-not $DisableFunctionLogging) {
                    Write-Log -Message 'Finding all open window title(s).' -Source ${CmdletName}
                }
            }

            ## Get all window handles for visible windows
            [IntPtr[]]$VisibleWindowHandles = [PSADT.UiAutomation]::EnumWindows() | Where-Object { [PSADT.UiAutomation]::IsWindowVisible($_) }

            ## Discover details about each visible window that was discovered
            ForEach ($VisibleWindowHandle in $VisibleWindowHandles) {
                If (-not $VisibleWindowHandle) {
                    Continue
                }
                ## Get the window title
                [String]$VisibleWindowTitle = [PSADT.UiAutomation]::GetWindowText($VisibleWindowHandle)
                If ($VisibleWindowTitle) {
                    ## Get the process that spawned the window
                    [Diagnostics.Process]$Process = Get-Process -ErrorAction 'Stop' | Where-Object { $_.Id -eq [PSADT.UiAutomation]::GetWindowThreadProcessId($VisibleWindowHandle) }
                    If ($Process) {
                        ## Build custom object with details about the window and the process
                        [PSObject]$VisibleWindow = New-Object -TypeName 'PSObject' -Property @{
                            WindowTitle                   = $VisibleWindowTitle
                            WindowHandle                  = $VisibleWindowHandle
                            ParentProcess                 = $Process.ProcessName
                            ParentProcessMainWindowHandle = $Process.MainWindowHandle
                            ParentProcessId               = $Process.Id
                        }

                        ## Only save/return the window and process details which match the search criteria
                        If ($PSCmdlet.ParameterSetName -eq 'SearchWinTitle') {
                            $MatchResult = $VisibleWindow.WindowTitle -match $WindowTitle
                            If ($MatchResult) {
                                [PSObject[]]$VisibleWindows += $VisibleWindow
                            }
                        }
                        ElseIf ($PSCmdlet.ParameterSetName -eq 'GetAllWinTitles') {
                            [PSObject[]]$VisibleWindows += $VisibleWindow
                        }
                    }
                }
            }
        }
        Catch {
            If (-not $DisableFunctionLogging) {
                Write-Log -Message "Failed to get requested window title(s). `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
        }
    }
    End {
        Write-Output -InputObject ($VisibleWindows)

        If ($DisableFunctionLogging) {
            . $RevertScriptLogging
        }
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Send-Keys
Function Send-Keys {
    <#
.SYNOPSIS

Send a sequence of keys to one or more application windows.

.DESCRIPTION

Send a sequence of keys to one or more application window. If window title searched for returns more than one window, then all of them will receive the sent keys.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

.PARAMETER WindowTitle

The title of the application window to search for using regex matching.

.PARAMETER GetAllWindowTitles

Get titles for all open windows on the system.

.PARAMETER WindowHandle

Send keys to a specific window where the Window Handle is already known.

.PARAMETER Keys

The sequence of keys to send. Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

.PARAMETER WaitSeconds

An optional number of seconds to wait after the sending of the keys.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world'

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".

.EXAMPLE

Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world' -WaitSeconds 5

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

.EXAMPLE

Send-Keys -WindowHandle ([IntPtr]17368294) -Key 'Hello world'

Send the sequence of keys "Hello world" to the application with a Window Handle of '17368294'.

.NOTES

.LINK

http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0)]
        [AllowEmptyString()]
        [ValidateNotNull()]
        [String]$WindowTitle,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [Switch]$GetAllWindowTitles = $false,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullorEmpty()]
        [IntPtr]$WindowHandle,
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [String]$Keys,
        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateNotNullorEmpty()]
        [Int32]$WaitSeconds
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        [ScriptBlock]$SendKeys = {
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [IntPtr]$WindowHandle
            )
            Try {
                ## Bring the window to the foreground
                [Boolean]$IsBringWindowToFrontSuccess = [PSADT.UiAutomation]::BringWindowToFront($WindowHandle)
                If (-not $IsBringWindowToFrontSuccess) {
                    Throw 'Failed to bring window to foreground.'
                }

                ## Send the Key sequence
                If ($Keys) {
                    [Boolean]$IsWindowModal = If ([PSADT.UiAutomation]::IsWindowEnabled($WindowHandle)) {
                        $false
                    }
                    Else {
                        $true
                    }
                    If ($IsWindowModal) {
                        Throw 'Unable to send keys to window because it may be disabled due to a modal dialog being shown.'
                    }
                    Write-Log -Message "Sending key(s) [$Keys] to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]." -Source ${CmdletName}
                    [Windows.Forms.SendKeys]::SendWait($Keys)
                    If ($WaitSeconds) {
                        Write-Log -Message "Sleeping for [$WaitSeconds] seconds." -Source ${CmdletName}
                        Start-Sleep -Seconds $WaitSeconds
                    }
                }
            }
            Catch {
                Write-Log -Message "Failed to send keys to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
        }
    }
    Process {
        Try {
            If ($WindowHandle) {
                [PSObject]$Window = Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.WindowHandle -eq $WindowHandle }
                If (-not $Window) {
                    Write-Log -Message "No windows with Window Handle [$WindowHandle] were discovered." -Severity 2 -Source ${CmdletName}
                    Return
                }
                & $SendKeys -WindowHandle $Window.WindowHandle
            }
            Else {
                [Hashtable]$GetWindowTitleSplat = @{}
                If ($GetAllWindowTitles) {
                    $GetWindowTitleSplat.Add( 'GetAllWindowTitles', $GetAllWindowTitles)
                }
                Else {
                    $GetWindowTitleSplat.Add( 'WindowTitle', $WindowTitle)
                }
                [PSObject[]]$AllWindows = Get-WindowTitle @GetWindowTitleSplat
                If (-not $AllWindows) {
                    Write-Log -Message 'No windows with the specified details were discovered.' -Severity 2 -Source ${CmdletName}
                    Return
                }

                ForEach ($Window in $AllWindows) {
                    & $SendKeys -WindowHandle $Window.WindowHandle
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to send keys to specified window. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Test-Battery
Function Test-Battery {
    <#
.SYNOPSIS

Tests whether the local machine is running on AC power or not.

.DESCRIPTION

Tests whether the local machine is running on AC power and returns true/false. For detailed information, use -PassThru option.

.PARAMETER PassThru

Outputs a hashtable containing the following properties:

IsLaptop, IsUsingACPower, ACPowerLineStatus, BatteryChargeStatus, BatteryLifePercent, BatteryLifeRemaining, BatteryFullLifetime

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Hashtable.

Returns a hashtable containing the following properties
- IsLaptop
- IsUsingACPower
- ACPowerLineStatus
- BatteryChargeStatus
- BatteryLifePercent
- BatteryLifeRemaining
- BatteryFullLifetime

.EXAMPLE

Test-Battery

.EXAMPLE

(Test-Battery -PassThru).IsLaptop

Determines if the current system is a laptop or not.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$PassThru = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Initialize a hashtable to store information about system type and power status
        [Hashtable]$SystemTypePowerStatus = @{ }
    }
    Process {
        Write-Log -Message 'Checking if system is using AC power or if it is running on battery...' -Source ${CmdletName}

        [Windows.Forms.PowerStatus]$PowerStatus = [Windows.Forms.SystemInformation]::PowerStatus

        ## Get the system power status. Indicates whether the system is using AC power or if the status is unknown. Possible values:
        #	Offline : The system is not using AC power.
        #	Online  : The system is using AC power.
        #	Unknown : The power status of the system is unknown.
        [String]$PowerLineStatus = $PowerStatus.PowerLineStatus
        $SystemTypePowerStatus.Add('ACPowerLineStatus', $PowerStatus.PowerLineStatus)

        ## Get the current battery charge status. Possible values: High, Low, Critical, Charging, NoSystemBattery, Unknown.
        [String]$BatteryChargeStatus = $PowerStatus.BatteryChargeStatus
        $SystemTypePowerStatus.Add('BatteryChargeStatus', $PowerStatus.BatteryChargeStatus)

        ## Get the approximate amount, from 0.00 to 1.0, of full battery charge remaining.
        #  This property can report 1.0 when the battery is damaged and Windows can't detect a battery.
        #  Therefore, this property is only indicative of battery charge remaining if 'BatteryChargeStatus' property is not reporting 'NoSystemBattery' or 'Unknown'.
        [Single]$BatteryLifePercent = $PowerStatus.BatteryLifePercent
        If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
            [Single]$BatteryLifePercent = 0.0
        }
        $SystemTypePowerStatus.Add('BatteryLifePercent', $PowerStatus.BatteryLifePercent)

        ## The reported approximate number of seconds of battery life remaining. It will report -1 if the remaining life is unknown because the system is on AC power.
        [Int32]$BatteryLifeRemaining = $PowerStatus.BatteryLifeRemaining
        $SystemTypePowerStatus.Add('BatteryLifeRemaining', $PowerStatus.BatteryLifeRemaining)

        ## Get the manufacturer reported full charge lifetime of the primary battery power source in seconds.
        #  The reported number of seconds of battery life available when the battery is fully charged, or -1 if it is unknown.
        #  This will only be reported if the battery supports reporting this information. You will most likely get -1, indicating unknown.
        [Int32]$BatteryFullLifetime = $PowerStatus.BatteryFullLifetime
        $SystemTypePowerStatus.Add('BatteryFullLifetime', $PowerStatus.BatteryFullLifetime)

        ## Determine if the system is using AC power
        [Boolean]$OnACPower = $false
        Switch ($PowerLineStatus) {
            'Online' {
                Write-Log -Message 'System is using AC power.' -Source ${CmdletName}
                $OnACPower = $true
            }
            'Offline' {
                Write-Log -Message 'System is using battery power.' -Source ${CmdletName}
            }
            'Unknown' {
                If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
                    Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. This is most likely due to a damaged battery so we will report system is using AC power." -Source ${CmdletName}
                    $OnACPower = $true
                }
                Else {
                    Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. Therefore, we will report system is using battery power." -Source ${CmdletName}
                }
            }
        }
        $SystemTypePowerStatus.Add('IsUsingACPower', $OnACPower)

        ## Determine if the system is a laptop
        [Boolean]$IsLaptop = $false
        If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
            $IsLaptop = $false
        }
        Else {
            $IsLaptop = $true
        }
        #  Chassis Types
        [Int32[]]$ChassisTypes = Get-WmiObject -Class 'Win32_SystemEnclosure' | Where-Object { $_.ChassisTypes } | Select-Object -ExpandProperty 'ChassisTypes'
        Write-Log -Message "The following system chassis types were detected [$($ChassisTypes -join ',')]." -Source ${CmdletName}
        ForEach ($ChassisType in $ChassisTypes) {
            Switch ($ChassisType) {
                9 {
                    $IsLaptop = $true
                } # 9=Laptop
                10 {
                    $IsLaptop = $true
                } # 10=Notebook
                14 {
                    $IsLaptop = $true
                } # 14=Sub Notebook
                3 {
                    $IsLaptop = $false
                } # 3=Desktop
            }
        }
        #  Add IsLaptop property to hashtable
        $SystemTypePowerStatus.Add('IsLaptop', $IsLaptop)

        If ($PassThru) {
            Write-Output -InputObject ($SystemTypePowerStatus)
        }
        Else {
            Write-Output -InputObject ($OnACPower)
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Test-NetworkConnection
Function Test-NetworkConnection {
    <#
.SYNOPSIS

Tests for an active local network connection, excluding wireless and virtual network adapters.

.DESCRIPTION

Tests for an active local network connection, excluding wireless and virtual network adapters, by querying the Win32_NetworkAdapter WMI class.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if a wired network connection is detected, otherwise returns $false.

.EXAMPLE

Test-NetworkConnection

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Write-Log -Message 'Checking if system is using a wired network connection...' -Source ${CmdletName}

        [PSObject[]]$networkConnected = Get-WmiObject -Class 'Win32_NetworkAdapter' | Where-Object { ($_.NetConnectionStatus -eq 2) -and ($_.NetConnectionID -match 'Local' -or $_.NetConnectionID -match 'Ethernet') -and ($_.NetConnectionID -notmatch 'Wireless') -and ($_.Name -notmatch 'Virtual') } -ErrorAction 'SilentlyContinue'
        [Boolean]$onNetwork = $false
        If ($networkConnected) {
            Write-Log -Message 'Wired network connection found.' -Source ${CmdletName}
            [Boolean]$onNetwork = $true
        }
        Else {
            Write-Log -Message 'Wired network connection not found.' -Source ${CmdletName}
        }

        Write-Output -InputObject ($onNetwork)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Test-PowerPoint
Function Test-PowerPoint {
    <#
.SYNOPSIS

Tests whether PowerPoint is running in either fullscreen slideshow mode or presentation mode.

.DESCRIPTION

Tests whether someone is presenting using PowerPoint in either fullscreen slideshow mode or presentation mode.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if PowerPoint is running in either fullscreen slideshow mode or presentation mode, otherwise returns $false.

.EXAMPLE

Test-PowerPoint

.NOTES

This function can only execute detection logic if the process is in interactive mode.

There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show".

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Checking if PowerPoint is in either fullscreen slideshow mode or presentation mode...' -Source ${CmdletName}
            Try {
                [Diagnostics.Process[]]$PowerPointProcess = Get-Process -ErrorAction 'Stop' | Where-Object { $_.ProcessName -eq 'POWERPNT' }
                If ($PowerPointProcess) {
                    [Boolean]$IsPowerPointRunning = $true
                    Write-Log -Message 'PowerPoint application is running.' -Source ${CmdletName}
                }
                Else {
                    [Boolean]$IsPowerPointRunning = $false
                    Write-Log -Message 'PowerPoint application is not running.' -Source ${CmdletName}
                }
            }
            Catch {
                Throw
            }

            [Nullable[Boolean]]$IsPowerPointFullScreen = $false
            If ($IsPowerPointRunning) {
                ## Detect if PowerPoint is in fullscreen mode or Presentation Mode, detection method only works if process is interactive
                If ([Environment]::UserInteractive) {
                    #  Check if "POWERPNT" process has a window with a title that begins with "PowerPoint Slide Show" or "Powerpoint-" for non-English language systems.
                    #  There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show"
                    [PSObject]$PowerPointWindow = Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.WindowTitle -match '^PowerPoint Slide Show' -or $_.WindowTitle -match '^PowerPoint-' } | Where-Object { $_.ParentProcess -eq 'POWERPNT' } | Select-Object -First 1
                    If ($PowerPointWindow) {
                        [Nullable[Boolean]]$IsPowerPointFullScreen = $true
                        Write-Log -Message 'Detected that PowerPoint process [POWERPNT] has a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-].' -Source ${CmdletName}
                    }
                    Else {
                        Write-Log -Message 'Detected that PowerPoint process [POWERPNT] does not have a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-].' -Source ${CmdletName}
                        Try {
                            [Int32[]]$PowerPointProcessIDs = $PowerPointProcess | Select-Object -ExpandProperty 'Id' -ErrorAction 'Stop'
                            Write-Log -Message "PowerPoint process [POWERPNT] has process id(s) [$($PowerPointProcessIDs -join ', ')]." -Source ${CmdletName}
                        }
                        Catch {
                            Write-Log -Message "Unable to retrieve process id(s) for [POWERPNT] process. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                        }
                    }

                    ## If previous detection method did not detect PowerPoint in fullscreen mode, then check if PowerPoint is in Presentation Mode (check only works on Windows Vista or higher)
                    If ((-not $IsPowerPointFullScreen) -and (([Version]$envOSVersion).Major -gt 5)) {
                        #  Note: below method does not detect PowerPoint presentation mode if the presentation is on a monitor that does not have current mouse input control
                        [String]$UserNotificationState = [PSADT.UiAutomation]::GetUserNotificationState()
                        Write-Log -Message "Detected user notification state [$UserNotificationState]." -Source ${CmdletName}
                        Switch ($UserNotificationState) {
                            'PresentationMode' {
                                Write-Log -Message 'Detected that system is in [Presentation Mode].' -Source ${CmdletName}
                                [Nullable[Boolean]]$IsPowerPointFullScreen = $true
                            }
                            'FullScreenOrPresentationModeOrLoginScreen' {
                                If (([String]$PowerPointProcessIDs) -and ($PowerPointProcessIDs -contains [PSADT.UIAutomation]::GetWindowThreadProcessID([PSADT.UIAutomation]::GetForeGroundWindow()))) {
                                    Write-Log -Message 'Detected that fullscreen foreground window matches PowerPoint process id.' -Source ${CmdletName}
                                    [Nullable[Boolean]]$IsPowerPointFullScreen = $true
                                }
                            }
                        }
                    }
                }
                Else {
                    [Nullable[Boolean]]$IsPowerPointFullScreen = $null
                    Write-Log -Message 'Unable to run check to see if PowerPoint is in fullscreen mode or Presentation Mode because current process is not interactive. Configure script to run in interactive mode in your deployment tool. If using SCCM Application Model, then make sure "Allow users to view and interact with the program installation" is selected. If using SCCM Package Model, then make sure "Allow users to interact with this program" is selected.' -Severity 2 -Source ${CmdletName}
                }
            }
        }
        Catch {
            [Nullable[Boolean]]$IsPowerPointFullScreen = $null
            Write-Log -Message "Failed check to see if PowerPoint is running in fullscreen slideshow mode. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-Log -Message "PowerPoint is running in fullscreen mode [$IsPowerPointFullScreen]." -Source ${CmdletName}
        Write-Output -InputObject ($IsPowerPointFullScreen)
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Invoke-SCCMTask
Function Invoke-SCCMTask {
    <#
.SYNOPSIS

Triggers SCCM to invoke the requested schedule task id.

.DESCRIPTION

Triggers SCCM to invoke the requested schedule task id.

.PARAMETER ScheduleId

Name of the schedule id to trigger.

Options: HardwareInventory, SoftwareInventory, HeartbeatDiscovery, SoftwareInventoryFileCollection, RequestMachinePolicy, EvaluateMachinePolicy,
LocationServicesCleanup, SoftwareMeteringReport, SourceUpdate, PolicyAgentCleanup, RequestMachinePolicy2, CertificateMaintenance, PeerDistributionPointStatus,
PeerDistributionPointProvisioning, ComplianceIntervalEnforcement, SoftwareUpdatesAgentAssignmentEvaluation, UploadStateMessage, StateMessageManager,
SoftwareUpdatesScan, AMTProvisionCycle, UpdateStorePolicy, StateSystemBulkSend, ApplicationManagerPolicyAction, PowerManagementStartSummarizer

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Invoke-SCCMTask 'SoftwareUpdatesScan'

.EXAMPLE

Invoke-SCCMTask

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateSet('HardwareInventory', 'SoftwareInventory', 'HeartbeatDiscovery', 'SoftwareInventoryFileCollection', 'RequestMachinePolicy', 'EvaluateMachinePolicy', 'LocationServicesCleanup', 'SoftwareMeteringReport', 'SourceUpdate', 'PolicyAgentCleanup', 'RequestMachinePolicy2', 'CertificateMaintenance', 'PeerDistributionPointStatus', 'PeerDistributionPointProvisioning', 'ComplianceIntervalEnforcement', 'SoftwareUpdatesAgentAssignmentEvaluation', 'UploadStateMessage', 'StateMessageManager', 'SoftwareUpdatesScan', 'AMTProvisionCycle', 'UpdateStorePolicy', 'StateSystemBulkSend', 'ApplicationManagerPolicyAction', 'PowerManagementStartSummarizer')]
        [String]$ScheduleID,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Invoke SCCM Schedule Task ID [$ScheduleId]..." -Source ${CmdletName}

            ## Make sure SCCM client is installed and running
            Write-Log -Message 'Checking to see if SCCM Client service [ccmexec] is installed and running.' -Source ${CmdletName}
            If (Test-ServiceExists -Name 'ccmexec') {
                If ($(Get-Service -Name 'ccmexec' -ErrorAction 'SilentlyContinue').Status -ne 'Running') {
                    Throw "SCCM Client Service [ccmexec] exists but it is not in a 'Running' state."
                }
            }
            Else {
                Throw 'SCCM Client Service [ccmexec] does not exist. The SCCM Client may not be installed.'
            }

            ## Determine the SCCM Client Version
            Try {
                [Version]$SCCMClientVersion = Get-WmiObject -Namespace 'ROOT\CCM' -Class 'CCM_InstalledComponent' -ErrorAction 'Stop' | Where-Object { $_.Name -eq 'SmsClient' } | Select-Object -ExpandProperty 'Version' -ErrorAction 'Stop'
                If ($SCCMClientVersion) {
                    Write-Log -Message "Installed SCCM Client Version Number [$SCCMClientVersion]." -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                    Throw 'Failed to determine the SCCM client version number.'
                }
            }
            Catch {
                Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                Throw 'Failed to determine the SCCM client version number.'
            }

            ## Create a hashtable of Schedule IDs compatible with SCCM Client 2007
            [Hashtable]$ScheduleIds = @{
                HardwareInventory                        = '{00000000-0000-0000-0000-000000000001}'; # Hardware Inventory Collection Task
                SoftwareInventory                        = '{00000000-0000-0000-0000-000000000002}'; # Software Inventory Collection Task
                HeartbeatDiscovery                       = '{00000000-0000-0000-0000-000000000003}'; # Heartbeat Discovery Cycle
                SoftwareInventoryFileCollection          = '{00000000-0000-0000-0000-000000000010}'; # Software Inventory File Collection Task
                RequestMachinePolicy                     = '{00000000-0000-0000-0000-000000000021}'; # Request Machine Policy Assignments
                EvaluateMachinePolicy                    = '{00000000-0000-0000-0000-000000000022}'; # Evaluate Machine Policy Assignments
                RefreshDefaultMp                         = '{00000000-0000-0000-0000-000000000023}'; # Refresh Default MP Task
                RefreshLocationServices                  = '{00000000-0000-0000-0000-000000000024}'; # Refresh Location Services Task
                LocationServicesCleanup                  = '{00000000-0000-0000-0000-000000000025}'; # Location Services Cleanup Task
                SoftwareMeteringReport                   = '{00000000-0000-0000-0000-000000000031}'; # Software Metering Report Cycle
                SourceUpdate                             = '{00000000-0000-0000-0000-000000000032}'; # Source Update Manage Update Cycle
                PolicyAgentCleanup                       = '{00000000-0000-0000-0000-000000000040}'; # Policy Agent Cleanup Cycle
                RequestMachinePolicy2                    = '{00000000-0000-0000-0000-000000000042}'; # Request Machine Policy Assignments
                CertificateMaintenance                   = '{00000000-0000-0000-0000-000000000051}'; # Certificate Maintenance Cycle
                PeerDistributionPointStatus              = '{00000000-0000-0000-0000-000000000061}'; # Peer Distribution Point Status Task
                PeerDistributionPointProvisioning        = '{00000000-0000-0000-0000-000000000062}'; # Peer Distribution Point Provisioning Status Task
                ComplianceIntervalEnforcement            = '{00000000-0000-0000-0000-000000000071}'; # Compliance Interval Enforcement
                SoftwareUpdatesAgentAssignmentEvaluation = '{00000000-0000-0000-0000-000000000108}'; # Software Updates Agent Assignment Evaluation Cycle
                UploadStateMessage                       = '{00000000-0000-0000-0000-000000000111}'; # Send Unsent State Messages
                StateMessageManager                      = '{00000000-0000-0000-0000-000000000112}'; # State Message Manager Task
                SoftwareUpdatesScan                      = '{00000000-0000-0000-0000-000000000113}'; # Force Update Scan
                AMTProvisionCycle                        = '{00000000-0000-0000-0000-000000000120}'; # AMT Provision Cycle
            }

            ## If SCCM 2012 Client or higher, modify hashtabe containing Schedule IDs so that it only has the ones compatible with this version of the SCCM client
            If ($SCCMClientVersion.Major -ge 5) {
                $ScheduleIds.Remove('PeerDistributionPointStatus')
                $ScheduleIds.Remove('PeerDistributionPointProvisioning')
                $ScheduleIds.Remove('ComplianceIntervalEnforcement')
                $ScheduleIds.Add('UpdateStorePolicy', '{00000000-0000-0000-0000-000000000114}') # Update Store Policy
                $ScheduleIds.Add('StateSystemBulkSend', '{00000000-0000-0000-0000-000000000116}') # State System Policy Bulk Send Low
                $ScheduleIds.Add('ApplicationManagerPolicyAction', '{00000000-0000-0000-0000-000000000121}') # Application Manager Policy Action
                $ScheduleIds.Add('PowerManagementStartSummarizer', '{00000000-0000-0000-0000-000000000131}') # Power Management Start Summarizer
            }

            ## Determine if the requested Schedule ID is available on this version of the SCCM Client
            If (-not $ScheduleIds.ContainsKey($ScheduleId)) {
                Throw "The requested ScheduleId [$ScheduleId] is not available with this version of the SCCM Client [$SCCMClientVersion]."
            }

            ## Trigger SCCM task
            Write-Log -Message "Triggering SCCM Task ID [$ScheduleId]." -Source ${CmdletName}
            [Management.ManagementClass]$SmsClient = [WMIClass]'ROOT\CCM:SMS_Client'
            $null = $SmsClient.TriggerSchedule($ScheduleIds.$ScheduleID)
        }
        Catch {
            Write-Log -Message "Failed to trigger SCCM Schedule Task ID [$($ScheduleIds.$ScheduleId)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to trigger SCCM Schedule Task ID [$($ScheduleIds.$ScheduleId)]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Install-SCCMSoftwareUpdates
Function Install-SCCMSoftwareUpdates {
    <#
.SYNOPSIS

Scans for outstanding SCCM updates to be installed and installs the pending updates.

.DESCRIPTION

Scans for outstanding SCCM updates to be installed and installs the pending updates.

Only compatible with SCCM 2012 Client or higher. This function can take several minutes to run.

.PARAMETER SoftwareUpdatesScanWaitInSeconds

The amount of time to wait in seconds for the software updates scan to complete. Default is: 180 seconds.

.PARAMETER WaitForPendingUpdatesTimeout

The amount of time to wait for missing and pending updates to install before exiting the function. Default is: 45 minutes.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Install-SCCMSoftwareUpdates

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$SoftwareUpdatesScanWaitInSeconds = 180,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Timespan]$WaitForPendingUpdatesTimeout = $(New-TimeSpan -Minutes 45),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Scanning for and installing pending SCCM software updates.' -Source ${CmdletName}

            ## Make sure SCCM client is installed and running
            Write-Log -Message 'Checking to see if SCCM Client service [ccmexec] is installed and running.' -Source ${CmdletName}
            If (Test-ServiceExists -Name 'ccmexec') {
                If ($(Get-Service -Name 'ccmexec' -ErrorAction 'SilentlyContinue').Status -ne 'Running') {
                    Throw "SCCM Client Service [ccmexec] exists but it is not in a 'Running' state."
                }
            }
            Else {
                Throw 'SCCM Client Service [ccmexec] does not exist. The SCCM Client may not be installed.'
            }

            ## Determine the SCCM Client Version
            Try {
                [Version]$SCCMClientVersion = Get-WmiObject -Namespace 'ROOT\CCM' -Class 'CCM_InstalledComponent' -ErrorAction 'Stop' | Where-Object { $_.Name -eq 'SmsClient' } | Select-Object -ExpandProperty 'Version' -ErrorAction 'Stop'
                If ($SCCMClientVersion) {
                    Write-Log -Message "Installed SCCM Client Version Number [$SCCMClientVersion]." -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                    Throw 'Failed to determine the SCCM client version number.'
                }
            }
            Catch {
                Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                Throw 'Failed to determine the SCCM client version number.'
            }
            #  If SCCM 2007 Client or lower, exit function
            If ($SCCMClientVersion.Major -le 4) {
                Throw 'SCCM 2007 or lower, which is incompatible with this function, was detected on this system.'
            }

            $StartTime = Get-Date
            ## Trigger SCCM client scan for Software Updates
            Write-Log -Message 'Triggering SCCM client scan for Software Updates...' -Source ${CmdletName}
            Invoke-SCCMTask -ScheduleId 'SoftwareUpdatesScan'

            Write-Log -Message "The SCCM client scan for Software Updates has been triggered. The script is suspended for [$SoftwareUpdatesScanWaitInSeconds] seconds to let the update scan finish." -Source ${CmdletName}
            Start-Sleep -Seconds $SoftwareUpdatesScanWaitInSeconds

            ## Find the number of missing updates
            Try {
                Write-Log -Message 'Getting the number of missing updates...' -Source ${CmdletName}
                [Management.ManagementObject[]]$CMMissingUpdates = @(Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Query "SELECT * FROM CCM_SoftwareUpdate WHERE ComplianceState = '0'" -ErrorAction 'Stop')
            }
            Catch {
                Write-Log -Message "Failed to find the number of missing software updates. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                Throw 'Failed to find the number of missing software updates.'
            }

            ## Install missing updates and wait for pending updates to finish installing
            If ($CMMissingUpdates.Count) {
                #  Install missing updates
                Write-Log -Message "Installing missing updates. The number of missing updates is [$($CMMissingUpdates.Count)]." -Source ${CmdletName}
                $CMInstallMissingUpdates = (Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Class 'CCM_SoftwareUpdatesManager' -List).InstallUpdates($CMMissingUpdates)

                #  Wait for pending updates to finish installing or the timeout value to expire
                Do {
                    Start-Sleep -Seconds 60
                    [Array]$CMInstallPendingUpdates = @(Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Query 'SELECT * FROM CCM_SoftwareUpdate WHERE EvaluationState = 6 or EvaluationState = 7')
                    Write-Log -Message "The number of updates pending installation is [$($CMInstallPendingUpdates.Count)]." -Source ${CmdletName}
                } While (($CMInstallPendingUpdates.Count -ne 0) -and ((New-TimeSpan -Start $StartTime -End $(Get-Date)) -lt $WaitForPendingUpdatesTimeout))
            }
            Else {
                Write-Log -Message 'There are no missing updates.' -Source ${CmdletName}
            }
        }
        Catch {
            Write-Log -Message "Failed to trigger installation of missing software updates. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to trigger installation of missing software updates: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Update-GroupPolicy
Function Update-GroupPolicy {
    <#
.SYNOPSIS

Performs a gpupdate command to refresh Group Policies on the local machine.

.DESCRIPTION

Performs a gpupdate command to refresh Group Policies on the local machine.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Update-GroupPolicy

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        [String[]]$GPUpdateCmds = '/C echo N | gpupdate.exe /Target:Computer /Force', '/C echo N | gpupdate.exe /Target:User /Force'
        [Int32]$InstallCount = 0
        ForEach ($GPUpdateCmd in $GPUpdateCmds) {
            Try {
                If ($InstallCount -eq 0) {
                    [String]$InstallMsg = 'Updating Group Policies for the Machine'
                }
                Else {
                    [String]$InstallMsg = 'Updating Group Policies for the User'
                }
                Write-Log -Message "$($InstallMsg)..." -Source ${CmdletName}
                [PSObject]$ExecuteResult = Execute-Process -Path "$envWinDir\System32\cmd.exe" -Parameters $GPUpdateCmd -WindowStyle 'Hidden' -PassThru -ExitOnProcessFailure $false

                If ($ExecuteResult.ExitCode -ne 0) {
                    If ($ExecuteResult.ExitCode -eq 60002) {
                        Throw "Execute-Process function failed with exit code [$($ExecuteResult.ExitCode)]."
                    }
                    Else {
                        Throw "gpupdate.exe failed with exit code [$($ExecuteResult.ExitCode)]."
                    }
                }
                $InstallCount++
            }
            Catch {
                Write-Log -Message "$($InstallMsg) failed. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "$($InstallMsg) failed: $($_.Exception.Message)"
                }
                Continue
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Enable-TerminalServerInstallMode
Function Enable-TerminalServerInstallMode {
    <#
.SYNOPSIS

Changes to user install mode for Remote Desktop Session Host/Citrix servers.

.DESCRIPTION

Changes to user install mode for Remote Desktop Session Host/Citrix servers.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Enable-TerminalServerInstallMode

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Changing terminal server into user install mode...' -Source ${CmdletName}
            $terminalServerResult = & "$envWinDir\System32\change.exe" User /Install

            If ($global:LastExitCode -ne 1) {
                Throw $terminalServerResult
            }
        }
        Catch {
            Write-Log -Message "Failed to change terminal server into user install mode. `r`n$(Resolve-Error) " -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to change terminal server into user install mode: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Disable-TerminalServerInstallMode
Function Disable-TerminalServerInstallMode {
    <#
.SYNOPSIS

Changes to user install mode for Remote Desktop Session Host/Citrix servers.

.DESCRIPTION

Changes to user install mode for Remote Desktop Session Host/Citrix servers.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Disable-TerminalServerInstallMode

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Changing terminal server into user execute mode...' -Source ${CmdletName}
            $terminalServerResult = & "$envWinDir\System32\change.exe" User /Execute

            If ($global:LastExitCode -ne 1) {
                Throw $terminalServerResult
            }
        }
        Catch {
            Write-Log -Message "Failed to change terminal server into user execute mode. `r`n$(Resolve-Error) " -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to change terminal server into user execute mode: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Set-ActiveSetup
Function Set-ActiveSetup {
    <#
.SYNOPSIS

Creates an Active Setup entry in the registry to execute a file for each user upon login.

.DESCRIPTION

Active Setup allows handling of per-user changes registry/file changes upon login.
A registry key is created in the HKLM registry hive which gets replicated to the HKCU hive when a user logs in.
If the "Version" value of the Active Setup entry in HKLM is higher than the version value in HKCU, the file referenced in "StubPath" is executed.

This Function:
	- Creates the registry entries in HKLM:SOFTWARE\Microsoft\Active Setup\Installed Components\$installName.
	- Creates StubPath value depending on the file extension of the $StubExePath parameter.
	- Handles Version value with YYYYMMDDHHMMSS granularity to permit re-installs on the same day and still trigger Active Setup after Version increase.
	- Copies/overwrites the StubPath file to $StubExePath destination path if file exists in 'Files' subdirectory of script directory.
	- Executes the StubPath file for the current user based on $ExecuteForCurrentUser (no need to logout/login to trigger Active Setup).

.PARAMETER StubExePath

Full destination path to the file that will be executed for each user that logs in.

If this file exists in the 'Files' subdirectory of the script directory, it will be copied to the destination path.

.PARAMETER Arguments

Arguments to pass to the file being executed.

.PARAMETER Description

Description for the Active Setup. Users will see "Setting up personalized settings for: $Description" at logon. Default is: $installName.

.PARAMETER Key

Name of the registry key for the Active Setup entry. Default is: $installName.

.PARAMETER Version

Optional. Specify version for Active setup entry. Active Setup is not triggered if Version value has more than 8 consecutive digits. Use commas to get around this limitation. Default: YYYYMMDDHHMMSS

.PARAMETER Locale

Optional. Arbitrary string used to specify the installation language of the file being executed. Not replicated to HKCU.

.PARAMETER PurgeActiveSetupKey

Remove Active Setup entry from HKLM registry hive. Will also load each logon user's HKCU registry hive to remove Active Setup entry. Function returns after purging.

.PARAMETER DisableActiveSetup

Disables the Active Setup entry so that the StubPath file will not be executed. This also disables -ExecuteForCurrentUser

.PARAMETER ExecuteForCurrentUser

Specifies whether the StubExePath should be executed for the current user. Since this user is already logged in, the user won't have the application started without logging out and logging back in. Default: $true

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if Active Setup entry was created or updated, $false if Active Setup entry was not created or updated.

.EXAMPLE

Set-ActiveSetup -StubExePath 'C:\Users\Public\Company\ProgramUserConfig.vbs' -Arguments '/Silent' -Description 'Program User Config' -Key 'ProgramUserConfig' -Locale 'en'

.EXAMPLE

Set-ActiveSetup -StubExePath "$envWinDir\regedit.exe" -Arguments "/S `"%SystemDrive%\Program Files (x86)\PS App Deploy\PSAppDeployHKCUSettings.reg`"" -Description 'PS App Deploy Config' -Key 'PS_App_Deploy_Config' -ContinueOnError $true

.EXAMPLE

Set-ActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey

Deletes "ProgramUserConfig" active setup entry from all registry hives.

.NOTES

Original code borrowed from: Denis St-Pierre (Ottawa, Canada), Todd MacNaught (Ottawa, Canada)

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$StubExePath,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Arguments,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Description = $installName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Key = $installName,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Version = ((Get-Date -Format 'yyMM,ddHH,mmss').ToString()), # Ex: 1405,1515,0522
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Locale,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [Switch]$DisableActiveSetup = $false,
        [Parameter(Mandatory = $true, ParameterSetName = 'Purge')]
        [Switch]$PurgeActiveSetupKey,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExecuteForCurrentUser = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            [String]$ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\$Key"
            [String]$HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Microsoft\Active Setup\Installed Components\$Key"

            ## Delete Active Setup registry entry from the HKLM hive and for all logon user registry hives on the system
            If ($PurgeActiveSetupKey) {
                Write-Log -Message "Removing Active Setup entry [$ActiveSetupKey]." -Source ${CmdletName}
                Remove-RegistryKey -Key $ActiveSetupKey -Recurse

                Write-Log -Message "Removing Active Setup entry [$HKCUActiveSetupKey] for all log on user registry hives on the system." -Source ${CmdletName}
                [ScriptBlock]$RemoveHKCUActiveSetupKey = {
                    If (Get-RegistryKey -Key $HKCUActiveSetupKey -SID $RunAsActiveUser.SID) {
                        Remove-RegistryKey -Key $HKCUActiveSetupKey -SID $RunAsActiveUser.SID -Recurse
                    }
                }
                Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $RemoveHKCUActiveSetupKey -UserProfiles (Get-UserProfiles -ExcludeDefaultUser)
                Return
            }

            ## Verify a file with a supported file extension was specified in $StubExePath
            [String[]]$StubExePathFileExtensions = '.exe', '.vbs', '.cmd', '.ps1', '.js'
            [String]$StubExeExt = [IO.Path]::GetExtension($StubExePath)
            If ($StubExePathFileExtensions -notcontains $StubExeExt) {
                Throw "Unsupported Active Setup StubPath file extension [$StubExeExt]."
            }

            ## Copy file to $StubExePath from the 'Files' subdirectory of the script directory (if it exists there)
            [String]$StubExePath = [Environment]::ExpandEnvironmentVariables($StubExePath)
            [String]$ActiveSetupFileName = [IO.Path]::GetFileName($StubExePath)
            [String]$StubExeFile = Join-Path -Path $dirFiles -ChildPath $ActiveSetupFileName
            If (Test-Path -LiteralPath $StubExeFile -PathType 'Leaf') {
                #  This will overwrite the StubPath file if $StubExePath already exists on target
                Copy-File -Path $StubExeFile -Destination $StubExePath -ContinueOnError $false
            }

            ## Check if the $StubExePath file exists
            If (-not (Test-Path -LiteralPath $StubExePath -PathType 'Leaf')) {
                Throw "Active Setup StubPath file [$ActiveSetupFileName] is missing."
            }

            ## Define Active Setup StubPath according to file extension of $StubExePath
            Switch ($StubExeExt) {
                '.exe' {
                    [String]$CUStubExePath = "$StubExePath"
                    [String]$CUArguments = $Arguments
                    [String]$StubPath = "`"$CUStubExePath`""
                }
                '.js' {
                    [String]$CUStubExePath = "$envWinDir\System32\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.vbs' {
                    [String]$CUStubExePath = "$envWinDir\System32\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.cmd' {
                    [String]$CUStubExePath = "$envWinDir\System32\cmd.exe"
                    [String]$CUArguments = "/C `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.ps1' {
                    [String]$CUStubExePath = "$PSHOME\powershell.exe"
                    [String]$CUArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"& {& `\`"$StubExePath`\`"}`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
            }
            If ($Arguments) {
                [String]$StubPath = "$StubPath $Arguments"
                If ($StubExeExt -ne '.exe') {
                    [String]$CUArguments = "$CUArguments $Arguments"
                }
            }

            [ScriptBlock]$TestActiveSetup = {
                Param (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$HKLMKey,
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$HKCUKey,
                    [Parameter(Mandatory = $false)]
                    [ValidateNotNullorEmpty()]
                    [String]$UserSID
                )
                If ($UserSID) {
                    $HKCUProps = (Get-RegistryKey -Key $HKCUKey -SID $UserSID -ContinueOnError $true)
                }
                Else {
                    $HKCUProps = (Get-RegistryKey -Key $HKCUKey -ContinueOnError $true)
                }
                $HKLMProps = (Get-RegistryKey -Key $HKLMKey -ContinueOnError $true)
                [String]$HKCUVer = $HKCUProps.Version
                [String]$HKLMVer = $HKLMProps.Version
                [Int32]$HKLMInst = $HKLMProps.IsInstalled
                # HKLM entry not present. Nothing to run
                If (-not $HKLMProps) {
                    Write-Log 'HKLM active setup entry is not present.' -Source ${CmdletName}
                    Return ($false)
                }
                # HKLM entry present, but disabled. Nothing to run
                If ($HKLMInst -eq 0) {
                    Write-Log 'HKLM active setup entry is present, but it is disabled (IsInstalled set to 0).' -Source ${CmdletName}
                    Return ($false)
                }
                # HKLM entry present and HKCU entry is not. Run the StubPath
                If (-not $HKCUProps) {
                    Write-Log 'HKLM active setup entry is present. HKCU active setup entry is not present.' -Source ${CmdletName}
                    Return ($true)
                }
                # Both entries present. HKLM entry does not have Version property. Nothing to run
                If (-not $HKLMVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKLM Version property is missing.' -Source ${CmdletName}
                    Return ($false)
                }
                # Both entries present. HKLM entry has Version property, but HKCU entry does not. Run the StubPath
                If (-not $HKCUVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKCU Version property is missing.' -Source ${CmdletName}
                    Return ($true)
                }
                # Both entries present, with a Version property. Compare the Versions
                ## Remove invalid characters from Version. Only digits and commas are allowed
                [String]$HKLMValidVer = ''
                For ($i = 0; $i -lt $HKLMVer.Length; $i++) {
                    If ([Char]::IsDigit($HKLMVer[$i]) -or ($HKLMVer[$i] -eq ',')) {
                        $HKLMValidVer += $HKLMVer[$i]
                    }
                }

                [String]$HKCUValidVer = ''
                For ($i = 0; $i -lt $HKCUVer.Length; $i++) {
                    If ([Char]::IsDigit($HKCUVer[$i]) -or ($HKCUVer[$i] -eq ',')) {
                        $HKCUValidVer += $HKCUVer[$i]
                    }
                }
                # After cleanup, the HKLM Version is empty. Considering it missing. HKCU is present so nothing to run.
                If (-not $HKLMValidVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKLM Version property is invalid.' -Source ${CmdletName}
                    Return ($false)
                }
                # the HKCU Version property is empty while HKLM Version property is not. Run the StubPath
                If (-not $HKCUValidVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKCU Version property is invalid.' -Source ${CmdletName}
                    Return ($true)
                }
                # Both Version properties are present
                # Split the version by commas
                [String[]]$SplitHKLMValidVer = $HKLMValidVer.Split(',')
                [String[]]$SplitHKCUValidVer = $HKCUValidVer.Split(',')
                # Check whether the Versions were split in the same number of strings
                If ($SplitHKLMValidVer.Count -ne $SplitHKCUValidVer.Count) {
                    # The versions are different length - more commas
                    If ($SplitHKLMValidVer.Count -gt $SplitHKCUValidVer.Count) {
                        #HKLM is longer, Run the StubPath
                        Write-Log "HKLM and HKCU active setup entries are present. Both contain Version properties, however they don't contain the same amount of sub versions. HKLM Version has more sub versions." -Source ${CmdletName}
                        Return ($true)
                    }
                    Else {
                        #HKCU is longer, Nothing to run
                        Write-Log "HKLM and HKCU active setup entries are present. Both contain Version properties, however they don't contain the same amount of sub versions. HKCU Version has more sub versions." -Source ${CmdletName}
                        Return ($false)
                    }
                }
                # The Versions have the same number of strings. Compare them
                Try {
                    For ($i = 0; $i -lt $SplitHKLMValidVer.Count; $i++) {
                        # Parse the version is UINT64
                        [UInt64]$ParsedHKLMVer = [UInt64]::Parse($SplitHKLMValidVer[$i])
                        [UInt64]$ParsedHKCUVer = [UInt64]::Parse($SplitHKCUValidVer[$i])
                        # The HKCU ver is lower, Run the StubPath
                        If ($ParsedHKCUVer -lt $ParsedHKLMVer) {
                            Write-Log 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid, however HKCU Version property is lower.' -Source ${CmdletName}
                            Return ($true)
                        }
                    }
                    # The HKCU version is equal or higher than HKLM version, Nothing to run
                    Write-Log 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid, however they are either the same or HKCU Version property is higher.' -Source ${CmdletName}
                    Return ($false)
                }
                Catch {
                    # Failed to parse strings as UInts, Run the StubPath
                    Write-Log 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid, however parsing strings to uintegers failed.' -Severity 2 -Source ${CmdletName}
                    Return ($true)
                }
            }

            ## Create the Active Setup entry in the registry
            [ScriptBlock]$SetActiveSetupRegKeys = {
                Param (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$ActiveSetupRegKey,
                    [Parameter(Mandatory = $false)]
                    [ValidateNotNullorEmpty()]
                    [String]$SID
                )
                If ($SID) {
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -SID $SID -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -SID $SID -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -SID $SID -ContinueOnError $false
                    If ($Locale) {
                        Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -SID $SID -ContinueOnError $false
                    }
                    # Only Add IsInstalled to HKLM
                    If ($ActiveSetupRegKey.Contains('HKEY_LOCAL_MACHINE')) {
                        If ($DisableActiveSetup) {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -SID $SID -ContinueOnError $false
                        }
                        Else {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -SID $SID -ContinueOnError $false
                        }
                    }
                }
                Else {
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -ContinueOnError $false
                    If ($Locale) {
                        Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -ContinueOnError $false
                    }
                    # Only Add IsInstalled to HKLM
                    If ($ActiveSetupRegKey.Contains('HKEY_LOCAL_MACHINE')) {
                        If ($DisableActiveSetup) {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -ContinueOnError $false
                        }
                        Else {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -ContinueOnError $false
                        }
                    }
                }
            }

            Write-Log -Message "Adding Active Setup Key for local machine: [$ActiveSetupKey]." -Source ${CmdletName}
            & $SetActiveSetupRegKeys -ActiveSetupRegKey $ActiveSetupKey

            ## Execute the StubPath file for the current user as long as not in Session 0
            If ($ExecuteForCurrentUser) {
                If ($SessionZero) {
                    If ($RunAsActiveUser) {
                        # Skip if Active Setup reg key is present and Version is equal or higher
                        [Boolean]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey -UserSID $RunAsActiveUser.SID)
                        If ($InstallNeeded) {
                            Write-Log -Message "Session 0 detected: Executing Active Setup StubPath file for currently logged in user [$($RunAsActiveUser.NTAccount)]." -Source ${CmdletName}
                            If ($CUArguments) {
                                Execute-ProcessAsUser -Path $CUStubExePath -Parameters $CUArguments -Wait -ContinueOnError $true
                            }
                            Else {
                                Execute-ProcessAsUser -Path $CUStubExePath -Wait -ContinueOnError $true
                            }

                            Write-Log -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]." -Source ${CmdletName}
                            & $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey -SID $RunAsActiveUser.SID
                        }
                        Else {
                            Write-Log -Message "Session 0 detected: Skipping executing Active Setup StubPath file for currently logged in user [$($RunAsActiveUser.NTAccount)]." -Source ${CmdletName} -Severity 2
                        }
                    }
                    Else {
                        Write-Log -Message 'Session 0 detected: No logged in users detected. Active Setup StubPath file will execute when users first log into their account.' -Source ${CmdletName}
                    }
                }
                Else {
                    # Skip if Active Setup reg key is present and Version is equal or higher
                    [Boolean]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey)
                    If ($InstallNeeded) {
                        Write-Log -Message 'Executing Active Setup StubPath file for the current user.' -Source ${CmdletName}
                        If ($CUArguments) {
                            Execute-Process -FilePath $CUStubExePath -Parameters $CUArguments -ExitOnProcessFailure $false
                        }
                        Else {
                            Execute-Process -FilePath $CUStubExePath -ExitOnProcessFailure $false
                        }

                        Write-Log -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]." -Source ${CmdletName}
                        & $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey
                    }
                    Else {
                        Write-Log -Message 'Skipping executing Active Setup StubPath file for current user.' -Source ${CmdletName} -Severity 2
                    }
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to set Active Setup registry entry. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to set Active Setup registry entry: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Test-ServiceExists
Function Test-ServiceExists {
    <#
.SYNOPSIS

Check to see if a service exists.

.DESCRIPTION

Check to see if a service exists (using WMI method because Get-Service will generate ErrorRecord if service doesn't exist).

.PARAMETER Name

Specify the name of the service.

Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER PassThru

Return the WMI service object. To see all the properties use: Test-ServiceExists -Name 'spooler' -PassThru | Get-Member

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Test-ServiceExists -Name 'wuauserv'

.EXAMPLE

Test-ServiceExists -Name 'testservice' -PassThru | Where-Object { $_ } | ForEach-Object { $_.Delete() }

Check if a service exists and then delete it by using the -PassThru parameter.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$ComputerName = $env:ComputerName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$PassThru,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            $ServiceObject = Get-WmiObject -ComputerName $ComputerName -Class 'Win32_Service' -Filter "Name='$Name'" -ErrorAction 'Stop'
            # If nothing is returned from Win32_Service, check Win32_BaseService
            If (-not $ServiceObject) {
                $ServiceObject = Get-WmiObject -ComputerName $ComputerName -Class 'Win32_BaseService' -Filter "Name='$Name'" -ErrorAction 'Stop'
            }

            If ($ServiceObject) {
                Write-Log -Message "Service [$Name] exists." -Source ${CmdletName}
                If ($PassThru) {
                    Write-Output -InputObject ($ServiceObject)
                }
                Else {
                    Write-Output -InputObject ($true)
                }
            }
            Else {
                Write-Log -Message "Service [$Name] does not exist." -Source ${CmdletName}
                If ($PassThru) {
                    Write-Output -InputObject ($ServiceObject)
                }
                Else {
                    Write-Output -InputObject ($false)
                }
            }
        }
        Catch {
            Write-Log -Message "Failed check to see if service [$Name] exists." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed check to see if service [$Name] exists: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Stop-ServiceAndDependencies
Function Stop-ServiceAndDependencies {
    <#
.SYNOPSIS

Stop Windows service and its dependencies.

.DESCRIPTION

Stop Windows service and its dependencies.

.PARAMETER Name

Specify the name of the service.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER SkipServiceExistsTest

Choose to skip the test to check whether or not the service exists if it was already done outside of this function.

.PARAMETER SkipDependentServices

Choose to skip checking for and stopping dependent services. Default is: $false.

.PARAMETER PendingStatusWait

The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

.PARAMETER PassThru

Return the System.ServiceProcess.ServiceController service object.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.ServiceProcess.ServiceController.

Returns the service object.

.EXAMPLE

Stop-ServiceAndDependencies -Name 'wuauserv'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$ComputerName = $env:ComputerName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$SkipServiceExistsTest,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$SkipDependentServices,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Timespan]$PendingStatusWait = (New-TimeSpan -Seconds 60),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$PassThru,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## Check to see if the service exists
            If ((-not $SkipServiceExistsTest) -and (-not (Test-ServiceExists -ComputerName $ComputerName -Name $Name -ContinueOnError $false))) {
                Write-Log -Message "Service [$Name] does not exist." -Source ${CmdletName} -Severity 2
                Throw "Service [$Name] does not exist."
            }

            ## Get the service object
            Write-Log -Message "Getting the service object for service [$Name]." -Source ${CmdletName}
            [ServiceProcess.ServiceController]$Service = Get-Service -ComputerName $ComputerName -Name $Name -ErrorAction 'Stop'
            ## Wait up to 60 seconds if service is in a pending state
            [String[]]$PendingStatus = 'ContinuePending', 'PausePending', 'StartPending', 'StopPending'
            If ($PendingStatus -contains $Service.Status) {
                Switch ($Service.Status) {
                    'ContinuePending' {
                        $DesiredStatus = 'Running'
                    }
                    'PausePending' {
                        $DesiredStatus = 'Paused'
                    }
                    'StartPending' {
                        $DesiredStatus = 'Running'
                    }
                    'StopPending' {
                        $DesiredStatus = 'Stopped'
                    }
                }
                Write-Log -Message "Waiting for up to [$($PendingStatusWait.TotalSeconds)] seconds to allow service pending status [$($Service.Status)] to reach desired status [$DesiredStatus]." -Source ${CmdletName}
                $Service.WaitForStatus([ServiceProcess.ServiceControllerStatus]$DesiredStatus, $PendingStatusWait)
                $Service.Refresh()
            }
            ## Discover if the service is currently running
            Write-Log -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]." -Source ${CmdletName}
            If ($Service.Status -ne 'Stopped') {
                #  Discover all dependent services that are running and stop them
                If (-not $SkipDependentServices) {
                    Write-Log -Message "Discovering all dependent service(s) for service [$Name] which are not 'Stopped'." -Source ${CmdletName}
                    [ServiceProcess.ServiceController[]]$DependentServices = Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -DependentServices -ErrorAction 'Stop' | Where-Object { $_.Status -ne 'Stopped' }
                    If ($DependentServices) {
                        ForEach ($DependentService in $DependentServices) {
                            Write-Log -Message "Stopping dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]." -Source ${CmdletName}
                            Try {
                                Stop-Service -InputObject (Get-Service -ComputerName $ComputerName -Name $DependentService.ServiceName -ErrorAction 'Stop') -Force -WarningAction 'SilentlyContinue' -ErrorAction 'Stop'
                            }
                            Catch {
                                Write-Log -Message "Failed to stop dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]. Continue..." -Severity 2 -Source ${CmdletName}
                                Continue
                            }
                        }
                    }
                    Else {
                        Write-Log -Message "Dependent service(s) were not discovered for service [$Name]." -Source ${CmdletName}
                    }
                }
                #  Stop the parent service
                Write-Log -Message "Stopping parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]." -Source ${CmdletName}
                [ServiceProcess.ServiceController]$Service = Stop-Service -InputObject (Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -ErrorAction 'Stop') -Force -PassThru -WarningAction 'SilentlyContinue' -ErrorAction 'Stop'
            }
        }
        Catch {
            Write-Log -Message "Failed to stop the service [$Name]. `r`n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to stop the service [$Name]: $($_.Exception.Message)"
            }
        }
        Finally {
            #  Return the service object if option selected
            If ($PassThru -and $Service) {
                Write-Output -InputObject ($Service)
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Start-ServiceAndDependencies
Function Start-ServiceAndDependencies {
    <#
.SYNOPSIS

Start Windows service and its dependencies.

.DESCRIPTION

Start Windows service and its dependencies.

.PARAMETER Name

Specify the name of the service.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER SkipServiceExistsTest

Choose to skip the test to check whether or not the service exists if it was already done outside of this function.

.PARAMETER SkipDependentServices

Choose to skip checking for and starting dependent services. Default is: $false.

.PARAMETER PendingStatusWait

The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

.PARAMETER PassThru

Return the System.ServiceProcess.ServiceController service object.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.ServiceProcess.ServiceController.

Returns the service object.

.EXAMPLE

Start-ServiceAndDependencies -Name 'wuauserv'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$ComputerName = $env:ComputerName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$SkipServiceExistsTest,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$SkipDependentServices,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Timespan]$PendingStatusWait = (New-TimeSpan -Seconds 60),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$PassThru,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## Check to see if the service exists
            If ((-not $SkipServiceExistsTest) -and (-not (Test-ServiceExists -ComputerName $ComputerName -Name $Name -ContinueOnError $false))) {
                Write-Log -Message "Service [$Name] does not exist." -Source ${CmdletName} -Severity 2
                Throw "Service [$Name] does not exist."
            }

            ## Get the service object
            Write-Log -Message "Getting the service object for service [$Name]." -Source ${CmdletName}
            [ServiceProcess.ServiceController]$Service = Get-Service -ComputerName $ComputerName -Name $Name -ErrorAction 'Stop'
            ## Wait up to 60 seconds if service is in a pending state
            [String[]]$PendingStatus = 'ContinuePending', 'PausePending', 'StartPending', 'StopPending'
            If ($PendingStatus -contains $Service.Status) {
                Switch ($Service.Status) {
                    'ContinuePending' {
                        $DesiredStatus = 'Running'
                    }
                    'PausePending' {
                        $DesiredStatus = 'Paused'
                    }
                    'StartPending' {
                        $DesiredStatus = 'Running'
                    }
                    'StopPending' {
                        $DesiredStatus = 'Stopped'
                    }
                }
                Write-Log -Message "Waiting for up to [$($PendingStatusWait.TotalSeconds)] seconds to allow service pending status [$($Service.Status)] to reach desired status [$DesiredStatus]." -Source ${CmdletName}
                $Service.WaitForStatus([ServiceProcess.ServiceControllerStatus]$DesiredStatus, $PendingStatusWait)
                $Service.Refresh()
            }
            ## Discover if the service is currently stopped
            Write-Log -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]." -Source ${CmdletName}
            If ($Service.Status -ne 'Running') {
                #  Start the parent service
                Write-Log -Message "Starting parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]." -Source ${CmdletName}
                [ServiceProcess.ServiceController]$Service = Start-Service -InputObject (Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -ErrorAction 'Stop') -PassThru -WarningAction 'SilentlyContinue' -ErrorAction 'Stop'

                #  Discover all dependent services that are stopped and start them
                If (-not $SkipDependentServices) {
                    Write-Log -Message "Discover all dependent service(s) for service [$Name] which are not 'Running'." -Source ${CmdletName}
                    [ServiceProcess.ServiceController[]]$DependentServices = Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -DependentServices -ErrorAction 'Stop' | Where-Object { $_.Status -ne 'Running' }
                    If ($DependentServices) {
                        ForEach ($DependentService in $DependentServices) {
                            Write-Log -Message "Starting dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]." -Source ${CmdletName}
                            Try {
                                Start-Service -InputObject (Get-Service -ComputerName $ComputerName -Name $DependentService.ServiceName -ErrorAction 'Stop') -WarningAction 'SilentlyContinue' -ErrorAction 'Stop'
                            }
                            Catch {
                                Write-Log -Message "Failed to start dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]. Continue..." -Severity 2 -Source ${CmdletName}
                                Continue
                            }
                        }
                    }
                    Else {
                        Write-Log -Message "Dependent service(s) were not discovered for service [$Name]." -Source ${CmdletName}
                    }
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to start the service [$Name]. `r`n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to start the service [$Name]: $($_.Exception.Message)"
            }
        }
        Finally {
            #  Return the service object if option selected
            If ($PassThru -and $Service) {
                Write-Output -InputObject ($Service)
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-ServiceStartMode
Function Get-ServiceStartMode {
    <#
.SYNOPSIS

Get the service startup mode.

.DESCRIPTION

Get the service startup mode.

.PARAMETER Name

Specify the name of the service.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.ServiceProcess.ServiceController.

Returns the service object.

.EXAMPLE

Get-ServiceStartMode -Name 'wuauserv'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdLetBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$ComputerName = $env:ComputerName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Getting the service [$Name] startup mode." -Source ${CmdletName}
            [String]$ServiceStartMode = (Get-WmiObject -ComputerName $ComputerName -Class 'Win32_Service' -Filter "Name='$Name'" -Property 'StartMode' -ErrorAction 'Stop').StartMode
            ## If service start mode is set to 'Auto', change value to 'Automatic' to be consistent with 'Set-ServiceStartMode' function
            If ($ServiceStartMode -eq 'Auto') {
                $ServiceStartMode = 'Automatic'
            }

            ## If on Windows Vista or higher, check to see if service is set to Automatic (Delayed Start)
            If (($ServiceStartMode -eq 'Automatic') -and (([Version]$envOSVersion).Major -gt 5)) {
                Try {
                    [String]$ServiceRegistryPath = "Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$Name"
                    [Int32]$DelayedAutoStart = Get-ItemProperty -LiteralPath $ServiceRegistryPath -ErrorAction 'Stop' | Select-Object -ExpandProperty 'DelayedAutoStart' -ErrorAction 'Stop'
                    If ($DelayedAutoStart -eq 1) {
                        $ServiceStartMode = 'Automatic (Delayed Start)'
                    }
                }
                Catch {
                }
            }

            Write-Log -Message "Service [$Name] startup mode is set to [$ServiceStartMode]." -Source ${CmdletName}
            Write-Output -InputObject ($ServiceStartMode)
        }
        Catch {
            Write-Log -Message "Failed to get the service [$Name] startup mode. `r`n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to get the service [$Name] startup mode: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Set-ServiceStartMode
Function Set-ServiceStartMode {
    <#
.SYNOPSIS

Set the service startup mode.

.DESCRIPTION

Set the service startup mode.

.PARAMETER Name

Specify the name of the service.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER StartMode

Specify startup mode for the service. Options: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, System.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Set-ServiceStartMode -Name 'wuauserv' -StartMode 'Automatic (Delayed Start)'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdLetBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$ComputerName = $env:ComputerName,
        [Parameter(Mandatory = $true)]
        [ValidateSet('Automatic', 'Automatic (Delayed Start)', 'Manual', 'Disabled', 'Boot', 'System')]
        [String]$StartMode,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## If on lower than Windows Vista and 'Automatic (Delayed Start)' selected, then change to 'Automatic' because 'Delayed Start' is not supported.
            If (($StartMode -eq 'Automatic (Delayed Start)') -and (([Version]$envOSVersion).Major -lt 6)) {
                $StartMode = 'Automatic'
            }

            Write-Log -Message "Set service [$Name] startup mode to [$StartMode]." -Source ${CmdletName}

            ## Set the name of the start up mode that will be passed to sc.exe
            [String]$ScExeStartMode = $StartMode
            Switch ($StartMode) {
                'Automatic' {
                    $ScExeStartMode = 'Auto'; Break
                }
                'Automatic (Delayed Start)' {
                    $ScExeStartMode = 'Delayed-Auto'; Break
                }
                'Manual' {
                    $ScExeStartMode = 'Demand'; Break
                }
            }

            ## Set the start up mode using sc.exe. Note: we found that the ChangeStartMode method in the Win32_Service WMI class set services to 'Automatic (Delayed Start)' even when you specified 'Automatic' on Win7, Win8, and Win10.
            $ChangeStartMode = & "$envWinDir\System32\sc.exe" config $Name start= $ScExeStartMode

            If ($global:LastExitCode -ne 0) {
                Throw "sc.exe failed with exit code [$($global:LastExitCode)] and message [$ChangeStartMode]."
            }

            Write-Log -Message "Successfully set service [$Name] startup mode to [$StartMode]." -Source ${CmdletName}
        }
        Catch {
            Write-Log -Message "Failed to set service [$Name] startup mode to [$StartMode]. `r`n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to set service [$Name] startup mode to [$StartMode]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-LoggedOnUser
Function Get-LoggedOnUser {
    <#
.SYNOPSIS

Get session details for all local and RDP logged on users.

.DESCRIPTION

Get session details for all local and RDP logged on users using Win32 APIs. Get the following session details:
	NTAccount, SID, UserName, DomainName, SessionId, SessionName, ConnectState, IsCurrentSession, IsConsoleSession, IsUserSession, IsActiveUserSession
	IsRdpSession, IsLocalAdmin, LogonTime, IdleTime, DisconnectTime, ClientName, ClientProtocolType, ClientDirectory, ClientBuildNumber

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Get-LoggedOnUser

.NOTES

Description of ConnectState property:

Value		 Description
-----		 -----------
Active		 A user is logged on to the session.
ConnectQuery The session is in the process of connecting to a client.
Connected	 A client is connected to the session.
Disconnected The session is active, but the client has disconnected from it.
Down		 The session is down due to an error.
Idle		 The session is waiting for a client to connect.
Initializing The session is initializing.
Listening 	 The session is listening for connections.
Reset		 The session is being reset.
Shadowing	 This session is shadowing another session.

Description of IsActiveUserSession property:

- If a console user exists, then that will be the active user session.
- If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that has ConnectState either 'Active' or 'Connected' is the active user.

Description of IsRdpSession property:
- Gets a value indicating whether the user is associated with an RDP client session.

Description of IsLocalAdmin property:
- Checks whether the user is a member of the Administrators group

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Getting session information for all logged on users.' -Source ${CmdletName}
            Write-Output -InputObject ([PSADT.QueryUser]::GetUserSessionInfo("$env:ComputerName"))
        }
        Catch {
            Write-Log -Message "Failed to get session information for all logged on users. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion


#region Function Get-PendingReboot
Function Get-PendingReboot {
    <#
.SYNOPSIS

Get the pending reboot status on a local computer.

.DESCRIPTION

Check WMI and the registry to determine if the system has a pending reboot operation from any of the following:
a) Component Based Servicing (Vista, Windows 2008)
b) Windows Update / Auto Update (XP, Windows 2003 / 2008)
c) SCCM 2012 Clients (DetermineIfRebootPending WMI method)
d) App-V Pending Tasks (global based Appv 5.0 SP2)
e) Pending File Rename Operations (XP, Windows 2003 / 2008)

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns a custom object with the following properties
- ComputerName
- LastBootUpTime
- IsSystemRebootPending
- IsCBServicingRebootPending
- IsWindowsUpdateRebootPending
- IsSCCMClientRebootPending
- IsFileRenameRebootPending
- PendingFileRenameOperations
- ErrorMsg

.EXAMPLE

Get-PendingReboot

Returns caustom object with following properties:
- ComputerName
- LastBootUpTime
- IsSystemRebootPending
- IsCBServicingRebootPending
- IsWindowsUpdateRebootPending
- IsSCCMClientRebootPending
- IsFileRenameRebootPending
- PendingFileRenameOperations
- ErrorMsg

.EXAMPLE

(Get-PendingReboot).IsSystemRebootPending

Returns boolean value determining whether or not there is a pending reboot operation.

.NOTES

ErrorMsg only contains something if an error occurred

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Initialize variables
        [String]$private:ComputerName = $envComputerNameFQDN
        $PendRebootErrorMsg = $null
    }
    Process {
        Write-Log -Message "Getting the pending reboot status on the local computer [$ComputerName]." -Source ${CmdletName}

        ## Get the date/time that the system last booted up
        Try {
            [Nullable[DateTime]]$LastBootUpTime = (Get-Date -ErrorAction 'Stop') - ([Timespan]::FromMilliseconds([Math]::Abs([Environment]::TickCount)))
        }
        Catch {
            [Nullable[DateTime]]$LastBootUpTime = $null
            [String[]]$PendRebootErrorMsg += "Failed to get LastBootUpTime: $($_.Exception.Message)"
            Write-Log -Message "Failed to get LastBootUpTime. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if a Windows Vista/Server 2008 and above machine has a pending reboot from a Component Based Servicing (CBS) operation
        Try {
            If (([Version]$envOSVersion).Major -ge 5) {
                If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending' -ErrorAction 'Stop') {
                    [Nullable[Boolean]]$IsCBServicingRebootPending = $true
                }
                Else {
                    [Nullable[Boolean]]$IsCBServicingRebootPending = $false
                }
            }
        }
        Catch {
            [Nullable[Boolean]]$IsCBServicingRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsCBServicingRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsCBServicingRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot from a Windows Update
        Try {
            If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired' -ErrorAction 'Stop') {
                [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $true
            }
            Else {
                [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $false
            }
        }
        Catch {
            [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsWindowsUpdateRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsWindowsUpdateRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot from a pending file rename operation
        [Boolean]$IsFileRenameRebootPending = $false
        $PendingFileRenameOperations = $null
        If (Test-RegistryValue -Key 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations') {
            #  If PendingFileRenameOperations value exists, set $IsFileRenameRebootPending variable to $true
            [Boolean]$IsFileRenameRebootPending = $true
            #  Get the value of PendingFileRenameOperations
            Try {
                [String[]]$PendingFileRenameOperations = Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'PendingFileRenameOperations' -ErrorAction 'Stop'
            }
            Catch {
                [String[]]$PendRebootErrorMsg += "Failed to get PendingFileRenameOperations: $($_.Exception.Message)"
                Write-Log -Message "Failed to get PendingFileRenameOperations. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
        }

        ## Determine SCCM 2012 Client reboot pending status
        Try {
            [Boolean]$IsSccmClientNamespaceExists = $false
            [PSObject]$SCCMClientRebootStatus = Invoke-WmiMethod -ComputerName $ComputerName -Namespace 'ROOT\CCM\ClientSDK' -Class 'CCM_ClientUtilities' -Name 'DetermineIfRebootPending' -ErrorAction 'Stop'
            [Boolean]$IsSccmClientNamespaceExists = $true
            If ($SCCMClientRebootStatus.ReturnValue -ne 0) {
                Throw "'DetermineIfRebootPending' method of 'ROOT\CCM\ClientSDK\CCM_ClientUtilities' class returned error code [$($SCCMClientRebootStatus.ReturnValue)]"
            }
            Else {
                Write-Log -Message 'Successfully queried SCCM client for reboot status.' -Source ${CmdletName}
                [Nullable[Boolean]]$IsSCCMClientRebootPending = $false
                If ($SCCMClientRebootStatus.IsHardRebootPending -or $SCCMClientRebootStatus.RebootPending) {
                    [Nullable[Boolean]]$IsSCCMClientRebootPending = $true
                    Write-Log -Message 'Pending SCCM reboot detected.' -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message 'Pending SCCM reboot not detected.' -Source ${CmdletName}
                }
            }
        }
        Catch [System.Management.ManagementException] {
            [Nullable[Boolean]]$IsSCCMClientRebootPending = $null
            [Boolean]$IsSccmClientNamespaceExists = $false
            Write-Log -Message 'Failed to get IsSCCMClientRebootPending. Failed to detect the SCCM client WMI class.' -Severity 3 -Source ${CmdletName}
        }
        Catch {
            [Nullable[Boolean]]$IsSCCMClientRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsSCCMClientRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsSCCMClientRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot from an App-V global Pending Task. (User profile based tasks will complete on logoff/logon)
        Try {
            If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Software\Microsoft\AppV\Client\PendingTasks' -ErrorAction 'Stop') {
                [Nullable[Boolean]]$IsAppVRebootPending = $true
            }
            Else {
                [Nullable[Boolean]]$IsAppVRebootPending = $false
            }
        }
        Catch {
            [Nullable[Boolean]]$IsAppVRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsAppVRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsAppVRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot for the system
        [Boolean]$IsSystemRebootPending = $false
        If ($IsCBServicingRebootPending -or $IsWindowsUpdateRebootPending -or $IsSCCMClientRebootPending -or $IsFileRenameRebootPending) {
            [Boolean]$IsSystemRebootPending = $true
        }

        ## Create a custom object containing pending reboot information for the system
        [PSObject]$PendingRebootInfo = New-Object -TypeName 'PSObject' -Property @{
            ComputerName                 = $ComputerName
            LastBootUpTime               = $LastBootUpTime
            IsSystemRebootPending        = $IsSystemRebootPending
            IsCBServicingRebootPending   = $IsCBServicingRebootPending
            IsWindowsUpdateRebootPending = $IsWindowsUpdateRebootPending
            IsSCCMClientRebootPending    = $IsSCCMClientRebootPending
            IsAppVRebootPending          = $IsAppVRebootPending
            IsFileRenameRebootPending    = $IsFileRenameRebootPending
            PendingFileRenameOperations  = $PendingFileRenameOperations
            ErrorMsg                     = $PendRebootErrorMsg
        }
        Write-Log -Message "Pending reboot status on the local computer [$ComputerName]: `r`n$($PendingRebootInfo | Format-List | Out-String)" -Source ${CmdletName}
    }
    End {
        Write-Output -InputObject ($PendingRebootInfo | Select-Object -Property 'ComputerName', 'LastBootUpTime', 'IsSystemRebootPending', 'IsCBServicingRebootPending', 'IsWindowsUpdateRebootPending', 'IsSCCMClientRebootPending', 'IsAppVRebootPending', 'IsFileRenameRebootPending', 'PendingFileRenameOperations', 'ErrorMsg')

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion

#region Function Set-ItemPermission
Function Set-ItemPermission {
    <#
.SYNOPSIS

	Allow you to easily change permissions on files or folders

.PARAMETER Path

	Path to the folder or file you want to modify (ex: C:\Temp)

.PARAMETER User

	One or more user names (ex: BUILTIN\Users, DOMAIN\Admin) to give the permissions to. If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)

.PARAMETER Permission

	Permission or list of permissions to be set/added/removed/replaced. To see all the possible permissions go to 'http://technet.microsoft.com/fr-fr/library/ff730951.aspx'.

	Permission DeleteSubdirectoriesAndFiles does not apply to files.

.PARAMETER PermissionType

	Sets Access Control Type of the permissions. Allowed options: Allow, Deny   Default: Allow

.PARAMETER Inheritance

	Sets permission inheritance. Does not apply to files. Multiple options can be specified. Allowed options: ObjectInherit, ContainerInherit, None  Default: None

	None - The permission entry is not inherited by child objects, ObjectInherit - The permission entry is inherited by child leaf objects. ContainerInherit - The permission entry is inherited by child container objects.

.PARAMETER Propagation

	Sets how to propagate inheritance. Does not apply to files. Allowed options: None, InheritOnly, NoPropagateInherit  Default: None

	None - Specifies that no inheritance flags are set. NoPropagateInherit - Specifies that the permission entry is not propagated to child objects. InheritOnly - Specifies that the permission entry is propagated only to child objects. This includes both container and leaf child objects.

.PARAMETER Method

	Specifies which method will be used to apply the permissions. Allowed options: Add, Set, Reset.

	Add - adds permissions rules but it does not remove previous permissions, Set - overwrites matching permission rules with new ones, Reset - removes matching permissions rules and then adds permission rules, Remove - Removes matching permission rules, RemoveSpecific - Removes specific permissions, RemoveAll - Removes all permission rules for specified user/s
	Default: Add

.PARAMETER EnableInheritance

	Enables inheritance on the files/folders.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

	Will grant FullControl permissions to 'John' and 'Users' on 'C:\Temp' and its files and folders children.

	PS C:\>Set-ItemPermission -Path 'C:\Temp' -User 'DOMAIN\John', 'BUILTIN\Utilisateurs' -Permission FullControl -Inheritance ObjectInherit,ContainerInherit

.EXAMPLE

	Will grant Read permissions to 'John' on 'C:\Temp\pic.png'

	PS C:\>Set-ItemPermission -Path 'C:\Temp\pic.png' -User 'DOMAIN\John' -Permission 'Read'

.EXAMPLE

	Will remove all permissions to 'John' on 'C:\Temp\Private'

	PS C:\>Set-ItemPermission -Path 'C:\Temp\Private' -User 'DOMAIN\John' -Permission 'None' -Method 'RemoveAll'

.NOTES

	Original Author: Julian DA CUNHA - dacunha.julian@gmail.com, used with permission

.LINK

	https://psappdeploytoolkit.com
#>

    [CmdletBinding()]
    Param (
        [Parameter( Mandatory = $true, Position = 0, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'DisableInheritance' )]
        [Parameter( Mandatory = $true, Position = 0, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'EnableInheritance' )]
        [ValidateNotNullOrEmpty()]
        [Alias('File', 'Folder')]
        [String]$Path,

        [Parameter( Mandatory = $true, Position = 1, HelpMessage = 'One or more user names (ex: BUILTIN\Users, DOMAIN\Admin). If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)', ParameterSetName = 'DisableInheritance')]
        [Alias('Username', 'Users', 'SID', 'Usernames')]
        [String[]]$User,

        [Parameter( Mandatory = $true, Position = 2, HelpMessage = "Permission or list of permissions to be set/added/removed/replaced. To see all the possible permissions go to 'http://technet.microsoft.com/fr-fr/library/ff730951.aspx'", ParameterSetName = 'DisableInheritance')]
        [Alias('Acl', 'Grant', 'Permissions', 'Deny')]
        [ValidateSet('AppendData', 'ChangePermissions', 'CreateDirectories', 'CreateFiles', 'Delete', `
                'DeleteSubdirectoriesAndFiles', 'ExecuteFile', 'FullControl', 'ListDirectory', 'Modify', `
                'Read', 'ReadAndExecute', 'ReadAttributes', 'ReadData', 'ReadExtendedAttributes', 'ReadPermissions', `
                'Synchronize', 'TakeOwnership', 'Traverse', 'Write', 'WriteAttributes', 'WriteData', 'WriteExtendedAttributes', 'None')]
        [String[]]$Permission,

        [Parameter( Mandatory = $false, Position = 3, HelpMessage = 'Whether you want to set Allow or Deny permissions', ParameterSetName = 'DisableInheritance')]
        [Alias('AccessControlType')]
        [ValidateSet('Allow', 'Deny')]
        [String]$PermissionType = 'Allow',

        [Parameter( Mandatory = $false, Position = 4, HelpMessage = 'Sets how permissions are inherited', ParameterSetName = 'DisableInheritance')]
        [ValidateSet('ContainerInherit', 'None', 'ObjectInherit')]
        [String[]]$Inheritance = 'None',

        [Parameter( Mandatory = $false, Position = 5, HelpMessage = 'Sets how to propage inheritance flags', ParameterSetName = 'DisableInheritance')]
        [ValidateSet('None', 'InheritOnly', 'NoPropagateInherit')]
        [String]$Propagation = 'None',

        [Parameter( Mandatory = $false, Position = 6, HelpMessage = 'Specifies which method will be used to add/remove/replace permissions.', ParameterSetName = 'DisableInheritance')]
        [ValidateSet('Add', 'Set', 'Reset', 'Remove', 'RemoveSpecific', 'RemoveAll')]
        [Alias('ApplyMethod', 'ApplicationMethod')]
        [String]$Method = 'Add',

        [Parameter( Mandatory = $true, Position = 1, HelpMessage = 'Enables inheritance, which removes explicit permissions.', ParameterSetName = 'EnableInheritance')]
        [Switch]$EnableInheritance
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }

    Process {
        # Test elevated perms
        If (-not $IsAdmin) {
            Write-Log -Message 'Unable to use the function [Set-ItemPermission] without elevated permissions.' -Source ${CmdletName}
            Throw 'Unable to use the function [Set-ItemPermission] without elevated permissions.'
        }

        # Check path existence
        If (-not (Test-Path -Path $Path -ErrorAction 'Stop')) {
            Write-Log -Message "Specified path does not exist [$Path]." -Source ${CmdletName}
            Throw "Specified path does not exist [$Path]."
        }

        If ($EnableInheritance) {
            # Get object acls
            $Acl = (Get-Item -Path $Path -ErrorAction 'Stop').GetAccessControl('Access')
            # Enable inherance
            $Acl.SetAccessRuleProtection($false, $true)
            Write-Log -Message "Enabling Inheritance on path [$Path]." -Source ${CmdletName}
            $null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
            Return
        }
        # Permissions
        [System.Security.AccessControl.FileSystemRights]$FileSystemRights = New-Object -TypeName 'System.Security.AccessControl.FileSystemRights'
        If ($Permission -ne 'None') {
            ForEach ($Entry in $Permission) {
                $FileSystemRights = $FileSystemRights -bor [System.Security.AccessControl.FileSystemRights]$Entry
            }
        }

        # InheritanceFlags
        $InheritanceFlag = New-Object -TypeName 'System.Security.AccessControl.InheritanceFlags'
        ForEach ($IFlag in $Inheritance) {
            $InheritanceFlag = $InheritanceFlag -bor [System.Security.AccessControl.InheritanceFlags]$IFlag
        }

        # PropagationFlags
        $PropagationFlag = [System.Security.AccessControl.PropagationFlags]$Propagation

        # Access Control Type
        $Allow = [System.Security.AccessControl.AccessControlType]$PermissionType

        # Modify variables to remove file incompatible flags if this is a file
        If (Test-Path -Path $Path -ErrorAction 'Stop' -PathType 'Leaf') {
            $FileSystemRights = $FileSystemRights -band (-bnot [System.Security.AccessControl.FileSystemRights]::DeleteSubdirectoriesAndFiles)
            $InheritanceFlag = [System.Security.AccessControl.InheritanceFlags]::None
            $PropagationFlag = [System.Security.AccessControl.PropagationFlags]::None
        }

        # Get object acls
        $Acl = (Get-Item -Path $Path -ErrorAction 'Stop').GetAccessControl('Access')
        # Disable inherance, Preserve inherited permissions
        $Acl.SetAccessRuleProtection($true, $true)
        $null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
        # Get updated acls - without inheritance
        $Acl = $null
        $Acl = (Get-Item -Path $Path -ErrorAction 'Stop').GetAccessControl('Access')
        # Apply permissions on Users
        ForEach ($U in $User) {
            # Trim whitespace and skip if empty
            $U = $U.Trim()
            If ($U.Length -eq 0) {
                Continue
            }
            # Set Username
            If ($U.StartsWith('*')) {
                # This is a SID, remove the *
                $U = $U.remove(0, 1)
                Try {
                    # Translate the SID
                    $UsersAccountName = ConvertTo-NTAccountOrSID -SID $U
                }
                Catch {
                    Write-Log "Failed to translate SID [$U]. Skipping..." -Source ${CmdletName} -Severity 2
                    Continue
                }

                $Username = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($UsersAccountName)
            }
            Else {
                $Username = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($U)
            }

            # Set/Add/Remove/Replace permissions and log the changes
            $Rule = New-Object -TypeName 'System.Security.AccessControl.FileSystemAccessRule' -ArgumentList ($Username, $FileSystemRights, $InheritanceFlag, $PropagationFlag, $Allow)
            Switch ($Method) {
                'Add' {
                    Write-Log -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
                    $Acl.AddAccessRule($Rule)
                    Break
                }
                'Set' {
                    Write-Log -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
                    $Acl.SetAccessRule($Rule)
                    Break
                }
                'Reset' {
                    Write-Log -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
                    $Acl.ResetAccessRule($Rule)
                    Break
                }
                'Remove' {
                    Write-Log -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
                    $Acl.RemoveAccessRule($Rule)
                    Break
                }
                'RemoveSpecific' {
                    Write-Log -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
                    $Acl.RemoveAccessRuleSpecific($Rule)
                    Break
                }
                'RemoveAll' {
                    Write-Log -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
                    $Acl.RemoveAccessRuleAll($Rule)
                    Break
                }
            }
        }
        # Use the prepared ACL
        $null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
    }

    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
#endregion

#endregion
##*=============================================
##* END FUNCTION LISTINGS
##*=============================================

##*=============================================
##* SCRIPT BODY
##*=============================================
#region ScriptBody

## If the script was invoked by the Help Console, exit the script now
If ($invokingScript) {
    If ((Split-Path -Path $invokingScript -Leaf) -eq 'AppDeployToolkitHelp.ps1') {
        Return
    }
}

## Add the custom types required for the toolkit
If (-not ([Management.Automation.PSTypeName]'PSADT.UiAutomation').Type) {
    [String[]]$ReferencedAssemblies = 'System.Drawing', 'System.Windows.Forms', 'System.DirectoryServices'
    Add-Type -Path $appDeployCustomTypesSourceCode -ReferencedAssemblies $ReferencedAssemblies -IgnoreWarnings -ErrorAction 'Stop'
}

## Define ScriptBlocks to disable/revert script logging
[ScriptBlock]$DisableScriptLogging = { $OldDisableLoggingValue = $DisableLogging ; $DisableLogging = $true }
[ScriptBlock]$RevertScriptLogging = { $DisableLogging = $OldDisableLoggingValue }

## Define ScriptBlock for getting details for all logged on users
[ScriptBlock]$GetLoggedOnUserDetails = {
    [PSObject[]]$LoggedOnUserSessions = Get-LoggedOnUser
    [String[]]$usersLoggedOn = $LoggedOnUserSessions | ForEach-Object { $_.NTAccount }

    If ($usersLoggedOn) {
        #  Get account and session details for the logged on user session that the current process is running under. Note that the account used to execute the current process may be different than the account that is logged into the session (i.e. you can use "RunAs" to launch with different credentials when logged into an account).
        [PSObject]$CurrentLoggedOnUserSession = $LoggedOnUserSessions | Where-Object { $_.IsCurrentSession }

        #  Get account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
        [PSObject]$CurrentConsoleUserSession = $LoggedOnUserSessions | Where-Object { $_.IsConsoleSession }

        ## Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
        #  If a console user exists, then that will be the active user session.
        #  If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is either 'Active' or 'Connected' is the active user.
        [PSObject]$RunAsActiveUser = $LoggedOnUserSessions | Where-Object { $_.IsActiveUserSession }
    }
}

## Disable logging until log file details are available
. $DisableScriptLogging

## If the default Deploy-Application.ps1 hasn't been modified, and the main script was not called by a referring script, check for MSI / MST and modify the install accordingly
If ((-not $appName) -and (-not $ReferredInstallName)) {
    # Build properly formatted Architecture String
    Switch ($Is64Bit) {
        $false {
            $formattedOSArch = 'x86'
        }
        $true {
            $formattedOSArch = 'x64'
        }
    }
    #  Find the first MSI file in the Files folder and use that as our install
    If ([String]$defaultMsiFile = (Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq '.msi') -and ($_.Name.EndsWith(".$formattedOSArch.msi")) } | Select-Object -ExpandProperty 'FullName' -First 1)) {
        Write-Log -Message "Discovered $formattedOSArch Zerotouch MSI under $defaultMSIFile" -Source $appDeployToolkitName
    }
    ElseIf ([String]$defaultMsiFile = (Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq '.msi') } | Select-Object -ExpandProperty 'FullName' -First 1)) {
        Write-Log -Message "Discovered Arch-Independent Zerotouch MSI under $defaultMSIFile" -Source $appDeployToolkitName
    }
    If ($defaultMsiFile) {
        Try {
            [Boolean]$useDefaultMsi = $true
            Write-Log -Message "Discovered Zero-Config MSI installation file [$defaultMsiFile]." -Source $appDeployToolkitName
            #  Discover if there is a zero-config MST file
            [String]$defaultMstFile = [IO.Path]::ChangeExtension($defaultMsiFile, 'mst')
            If (Test-Path -LiteralPath $defaultMstFile -PathType 'Leaf') {
                Write-Log -Message "Discovered Zero-Config MST installation file [$defaultMstFile]." -Source $appDeployToolkitName
            }
            Else {
                [String]$defaultMstFile = ''
            }
            #  Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
            [String[]]$defaultMspFiles = Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq '.msp') } | Select-Object -ExpandProperty 'FullName'
            If ($defaultMspFiles) {
                Write-Log -Message "Discovered Zero-Config MSP installation file(s) [$($defaultMspFiles -join ',')]." -Source $appDeployToolkitName
            }

            ## Read the MSI and get the installation details
            [Hashtable]$GetDefaultMsiTablePropertySplat = @{ Path = $defaultMsiFile; Table = 'Property'; ContinueOnError = $false; ErrorAction = 'Stop' }
            If ($defaultMstFile) {
                $GetDefaultMsiTablePropertySplat.Add('TransformPath', $defaultMstFile)
            }
            [PSObject]$defaultMsiPropertyList = Get-MsiTableProperty @GetDefaultMsiTablePropertySplat
            [String]$appVendor = $defaultMsiPropertyList.Manufacturer
            [String]$appName = $defaultMsiPropertyList.ProductName
            [String]$appVersion = $defaultMsiPropertyList.ProductVersion
            $GetDefaultMsiTablePropertySplat.Set_Item('Table', 'File')
            [PSObject]$defaultMsiFileList = Get-MsiTableProperty @GetDefaultMsiTablePropertySplat
            [String[]]$defaultMsiExecutables = Get-Member -InputObject $defaultMsiFileList -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Name' -ErrorAction 'Stop' | Where-Object { [IO.Path]::GetExtension($_) -eq '.exe' } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) }
            [String]$defaultMsiExecutablesList = $defaultMsiExecutables -join ','
            Write-Log -Message "App Vendor [$appVendor]." -Source $appDeployToolkitName
            Write-Log -Message "App Name [$appName]." -Source $appDeployToolkitName
            Write-Log -Message "App Version [$appVersion]." -Source $appDeployToolkitName
            Write-Log -Message "MSI Executable List [$defaultMsiExecutablesList]." -Source $appDeployToolkitName
        }
        Catch {
            Write-Log -Message "Failed to process Zero-Config MSI Deployment. `r`n$(Resolve-Error)" -Source $appDeployToolkitName
            $useDefaultMsi = $false ; $appVendor = '' ; $appName = '' ; $appVersion = ''
        }
    }
}

## Set up sample variables if Dot Sourcing the script, app details have not been specified
If (-not $appName) {
    [String]$appName = $appDeployMainScriptFriendlyName
    If (-not $appVendor) {
        [String]$appVendor = 'PS'
    }
    If (-not $appVersion) {
        [String]$appVersion = $appDeployMainScriptVersion
    }
    If (-not $appLang) {
        [String]$appLang = $currentLanguage
    }
    If (-not $appRevision) {
        [String]$appRevision = '01'
    }
    If (-not $appArch) {
        [String]$appArch = ''
    }
}
Else {
    If (-not $appVendor) {
        [String]$appVendor = ''
    }
    If (-not $appVersion) {
        [String]$appVersion = ''
    }
    If (-not $appLang) {
        [String]$appLang = ''
    }
    If (-not $appRevision) {
        [String]$appRevision = ''
    }
    If (-not $appArch) {
        [String]$appArch = ''
    }
}

## Sanitize the application details, as they can cause issues in the script
[String]$appVendor = (Remove-InvalidFileNameChars -Name ($appVendor.Trim()))
[String]$appName = (Remove-InvalidFileNameChars -Name ($appName.Trim()))
[String]$appVersion = (Remove-InvalidFileNameChars -Name ($appVersion.Trim()))
[String]$appArch = (Remove-InvalidFileNameChars -Name ($appArch.Trim()))
[String]$appLang = (Remove-InvalidFileNameChars -Name ($appLang.Trim()))
[String]$appRevision = (Remove-InvalidFileNameChars -Name ($appRevision.Trim()))

## Build the Installation Title
If ($ReferredInstallTitle) {
    [String]$installTitle = (Remove-InvalidFileNameChars -Name ($ReferredInstallTitle.Trim()))
}
If (-not $installTitle) {
    [String]$installTitle = "$appVendor $appName $appVersion"
}

## Set Powershell window title, in case the window is visible
[String]$oldPSWindowTitle = $Host.UI.RawUI.WindowTitle
$Host.UI.RawUI.WindowTitle = "$installTitle - $DeploymentType"

## Build the Installation Name
If ($ReferredInstallName) {
    [String]$installName = (Remove-InvalidFileNameChars -Name $ReferredInstallName)
}
If (-not $installName) {
    If ($appArch) {
        [String]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appArch + '_' + $appLang + '_' + $appRevision
    }
    Else {
        [String]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appLang + '_' + $appRevision
    }
}
[String]$installName = (($installName -replace ' ', '').Trim('_') -replace '[_]+', '_')

## Set the Defer History registry path
[String]$regKeyDeferHistory = "$configToolkitRegPath\$appDeployToolkitName\DeferHistory\$installName"

## Variables: Log Files
If ($ReferredLogName) {
    [String]$logName = $ReferredLogName
}
If (-not $logName) {
    [String]$logName = $installName + '_' + $appDeployToolkitName + '_' + $deploymentType + '.log'
}
#  If option to compress logs is selected, then log will be created in temp log folder ($logTempFolder) and then copied to actual log folder ($configToolkitLogDir) after being zipped.
[String]$logTempFolder = Join-Path -Path $envTemp -ChildPath "${installName}_$deploymentType"
If ($configToolkitCompressLogs) {
    #  If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues
    If (Test-Path -LiteralPath $logTempFolder -PathType 'Container' -ErrorAction 'SilentlyContinue') {
        $null = Remove-Item -LiteralPath $logTempFolder -Recurse -Force -ErrorAction 'SilentlyContinue'
    }
}

## Revert script logging to original setting
. $RevertScriptLogging

## Initialize Logging
$installPhase = 'Initialization'
$scriptSeparator = '*' * 79
Write-Log -Message ($scriptSeparator, $scriptSeparator) -Source $appDeployToolkitName
Write-Log -Message "[$installName] setup started." -Source $appDeployToolkitName

## Assemblies: Load
Try {
    Add-Type -AssemblyName ('System.Drawing', 'System.Windows.Forms', 'PresentationFramework', 'Microsoft.VisualBasic', 'PresentationCore', 'WindowsBase') -ErrorAction 'Stop'
}
Catch {
    Write-Log -Message "Failed to load assembly. `r`n$(Resolve-Error)" -Severity 3 -Source $appDeployToolkitName
    If ($deployModeNonInteractive) {
        Write-Log -Message "Continue despite assembly load error since deployment mode is [$deployMode]." -Source $appDeployToolkitName
    }
    Else {
        Exit-Script -ExitCode 60004
    }
}

# Calculate banner height
[Int32]$appDeployLogoBannerHeight = 0
Try {
    [System.Drawing.Bitmap]$appDeployLogoBannerObject = New-Object -TypeName 'System.Drawing.Bitmap' -ArgumentList ($appDeployLogoBanner)
    [Int32]$appDeployLogoBannerHeight = $appDeployLogoBannerObject.Height
    If ($appDeployLogoBannerHeight -gt $appDeployLogoBannerMaxHeight) {
        $appDeployLogoBannerHeight = $appDeployLogoBannerMaxHeight
    }
    $appDeployLogoBannerObject.Dispose($true) # Must dispose() when installing from local cache or else AppDeployToolkitBanner.png is locked and cannot be removed
}
Catch {
}

## Get the default font to use in the user interface
[System.Drawing.Font]$defaultFont = [System.Drawing.SystemFonts]::MessageBoxFont

## Check how the script was invoked
If ($invokingScript) {
    Write-Log -Message "Script [$scriptPath] dot-source invoked by [$invokingScript]" -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "Script [$scriptPath] invoked directly" -Source $appDeployToolkitName
}

## Dot Source script extensions
If (Test-Path -LiteralPath "$scriptRoot\$appDeployToolkitDotSourceExtensions" -PathType 'Leaf') {
    . "$scriptRoot\$appDeployToolkitDotSourceExtensions"
}

## Evaluate non-default parameters passed to the scripts
If ($deployAppScriptParameters) {
    [String]$deployAppScriptParameters = ($deployAppScriptParameters.GetEnumerator() | ForEach-Object { & $ResolveParameters $_ }) -join ' '
}
#  Save main script parameters hashtable for async execution of the toolkit
[Hashtable]$appDeployMainScriptAsyncParameters = $appDeployMainScriptParameters
If ($appDeployMainScriptParameters) {
    [String]$appDeployMainScriptParameters = ($appDeployMainScriptParameters.GetEnumerator() | ForEach-Object { & $ResolveParameters $_ }) -join ' '
}
If ($appDeployExtScriptParameters) {
    [String]$appDeployExtScriptParameters = ($appDeployExtScriptParameters.GetEnumerator() | ForEach-Object { & $ResolveParameters $_ }) -join ' '
}

## Check the XML config file version
If ($configConfigVersion -lt $appDeployMainScriptMinimumConfigVersion) {
    [String]$XMLConfigVersionErr = "The XML configuration file version [$configConfigVersion] is lower than the supported version required by the Toolkit [$appDeployMainScriptMinimumConfigVersion]. Please upgrade the configuration file."
    Write-Log -Message $XMLConfigVersionErr -Severity 3 -Source $appDeployToolkitName
    Throw $XMLConfigVersionErr
}

## Log system/script information
If ($appScriptVersion) {
    Write-Log -Message "[$installName] script version is [$appScriptVersion]" -Source $appDeployToolkitName
}
If ($appScriptDate) {
    Write-Log -Message "[$installName] script date is [$appScriptDate]" -Source $appDeployToolkitName
}
If ($appScriptAuthor) {
    Write-Log -Message "[$installName] script author is [$appScriptAuthor]" -Source $appDeployToolkitName
}
If ($deployAppScriptFriendlyName) {
    Write-Log -Message "[$deployAppScriptFriendlyName] script version is [$deployAppScriptVersion]" -Source $appDeployToolkitName
}
If ($deployAppScriptParameters) {
    Write-Log -Message "The following non-default parameters were passed to [$deployAppScriptFriendlyName]: [$deployAppScriptParameters]" -Source $appDeployToolkitName
}
If ($appDeployMainScriptFriendlyName) {
    Write-Log -Message "[$appDeployMainScriptFriendlyName] script version is [$appDeployMainScriptVersion]" -Source $appDeployToolkitName
}
If ($appDeployMainScriptParameters) {
    Write-Log -Message "The following non-default parameters were passed to [$appDeployMainScriptFriendlyName]: [$appDeployMainScriptParameters]" -Source $appDeployToolkitName
}
If ($appDeployExtScriptFriendlyName) {
    Write-Log -Message "[$appDeployExtScriptFriendlyName] version is [$appDeployExtScriptVersion]" -Source $appDeployToolkitName
}
If ($appDeployExtScriptParameters) {
    Write-Log -Message "The following non-default parameters were passed to [$appDeployExtScriptFriendlyName]: [$appDeployExtScriptParameters]" -Source $appDeployToolkitName
}
Write-Log -Message "Computer Name is [$envComputerNameFQDN]" -Source $appDeployToolkitName
Write-Log -Message "Current User is [$ProcessNTAccount]" -Source $appDeployToolkitName
If ($envOSServicePack) {
    Write-Log -Message "OS Version is [$envOSName $envOSServicePack $envOSArchitecture $envOSVersion]" -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "OS Version is [$envOSName $envOSArchitecture $envOSVersion]" -Source $appDeployToolkitName
}
Write-Log -Message "OS Type is [$envOSProductTypeName]" -Source $appDeployToolkitName
Write-Log -Message "Current Culture is [$($culture.Name)], language is [$currentLanguage] and UI language is [$currentUILanguage]" -Source $appDeployToolkitName
Write-Log -Message "Hardware Platform is [$(. $DisableScriptLogging; Get-HardwarePlatform; . $RevertScriptLogging)]" -Source $appDeployToolkitName
Write-Log -Message "PowerShell Host is [$($envHost.Name)] with version [$($envHost.Version)]" -Source $appDeployToolkitName
Write-Log -Message "PowerShell Version is [$envPSVersion $psArchitecture]" -Source $appDeployToolkitName
Write-Log -Message "PowerShell CLR (.NET) version is [$envCLRVersion]" -Source $appDeployToolkitName
Write-Log -Message $scriptSeparator -Source $appDeployToolkitName

## Disable logging
. $DisableScriptLogging

## Dot source ScriptBlock to get a list of all users logged on to the system (both local and RDP users), and discover session details for account executing script
. $GetLoggedOnUserDetails

## Dot source ScriptBlock to load localized UI messages from config XML
. $xmlLoadLocalizedUIMessages

## Dot source ScriptBlock to get system DPI scale factor
. $GetDisplayScaleFactor

## Revert script logging to original setting
. $RevertScriptLogging

## Set the install phase to asynchronous if the script was not dot sourced, i.e. called with parameters
If ($AsyncToolkitLaunch) {
    $installPhase = 'Asynchronous'
}

## If the ShowInstallationPrompt Parameter is specified, only call that function.
If ($showInstallationPrompt) {
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationPrompt]." -Source $appDeployToolkitName
    $appDeployMainScriptAsyncParameters.Remove('ShowInstallationPrompt')
    $appDeployMainScriptAsyncParameters.Remove('AsyncToolkitLaunch')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallName')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallTitle')
    $appDeployMainScriptAsyncParameters.Remove('ReferredLogName')
    Show-InstallationPrompt @appDeployMainScriptAsyncParameters
    Exit 0
}

## If the ShowInstallationRestartPrompt Parameter is specified, only call that function.
If ($showInstallationRestartPrompt) {
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationRestartPrompt]." -Source $appDeployToolkitName
    $appDeployMainScriptAsyncParameters.Remove('ShowInstallationRestartPrompt')
    $appDeployMainScriptAsyncParameters.Remove('AsyncToolkitLaunch')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallName')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallTitle')
    $appDeployMainScriptAsyncParameters.Remove('ReferredLogName')
    Show-InstallationRestartPrompt @appDeployMainScriptAsyncParameters
    Exit 0
}

## If the CleanupBlockedApps Parameter is specified, only call that function.
If ($cleanupBlockedApps) {
    $deployModeSilent = $true
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-CleanupBlockedApps]." -Source $appDeployToolkitName
    Unblock-AppExecution
    Exit 0
}

## If the ShowBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog) {
    Try {
        . $DisableScriptLogging
        Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowBlockedAppDialog]." -Source $appDeployToolkitName
        #  Create a mutex and specify a name without acquiring a lock on the mutex
        [Boolean]$showBlockedAppDialogMutexLocked = $false
        [String]$showBlockedAppDialogMutexName = 'Global\PSADT_ShowBlockedAppDialog_Message'
        [Threading.Mutex]$showBlockedAppDialogMutex = New-Object -TypeName 'System.Threading.Mutex' -ArgumentList ($false, $showBlockedAppDialogMutexName)
        #  Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock
        If ((Test-IsMutexAvailable -MutexName $showBlockedAppDialogMutexName -MutexWaitTimeInMilliseconds 1) -and ($showBlockedAppDialogMutex.WaitOne(1))) {
            [Boolean]$showBlockedAppDialogMutexLocked = $true
            Show-InstallationPrompt -Title $installTitle -Message $configBlockExecutionMessage -Icon 'Warning' -ButtonRightText 'OK'
            Exit 0
        }
        Else {
            #  If attempt to acquire an exclusive lock on the mutex failed, then exit script as another blocked app dialog window is already open
            Write-Log -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2 -Source $appDeployToolkitName
            Exit 0
        }
    }
    Catch {
        Write-Log -Message "There was an error in displaying the Installation Prompt. `r`n$(Resolve-Error)" -Severity 3 -Source $appDeployToolkitName
        Exit 60005
    }
    Finally {
        If ($showBlockedAppDialogMutexLocked) {
            $null = $showBlockedAppDialogMutex.ReleaseMutex()
        }
        If ($showBlockedAppDialogMutex) {
            $showBlockedAppDialogMutex.Close()
        }
    }
}

## Log details for all currently logged in users
Write-Log -Message "Display session information for all logged on users: `r`n$($LoggedOnUserSessions | Format-List | Out-String)" -Source $appDeployToolkitName
If ($usersLoggedOn) {
    Write-Log -Message "The following users are logged on to the system: [$($usersLoggedOn -join ', ')]." -Source $appDeployToolkitName

    #  Check if the current process is running in the context of one of the logged in users
    If ($CurrentLoggedOnUserSession) {
        Write-Log -Message "Current process is running with user account [$ProcessNTAccount] under logged in user session for [$($CurrentLoggedOnUserSession.NTAccount)]." -Source $appDeployToolkitName
    }
    Else {
        Write-Log -Message "Current process is running under a system account [$ProcessNTAccount]." -Source $appDeployToolkitName
    }

    # Check if user session is running under defaultuser0 account (Autopilot OOBE) or if application is installing during ESP and if so change deployment to run silently
    If ($CurrentLoggedOnUserSession.UserName -match 'defaultuser0' -or (((Get-Process -Name 'wwahost' -ErrorAction 'SilentlyContinue').count) -gt 0)) {
        Write-Log -Message "Autopilot OOBE user [$($CurrentLoggedOnUserSession.UserName)] or ESP process 'wwahost' detected, changing deployment mode to silent." -Source $appDeployToolkitExtName
        $deployMode = 'Silent'
    }

    #  Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
    If ($CurrentConsoleUserSession) {
        Write-Log -Message "The following user is the console user [$($CurrentConsoleUserSession.NTAccount)] (user with control of physical monitor, keyboard, and mouse)." -Source $appDeployToolkitName
    }
    Else {
        Write-Log -Message 'There is no console user logged in (user with control of physical monitor, keyboard, and mouse).' -Source $appDeployToolkitName
    }

    #  Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
    If ($RunAsActiveUser) {
        Write-Log -Message "The active logged on user is [$($RunAsActiveUser.NTAccount)]." -Source $appDeployToolkitName
    }
}
Else {
    Write-Log -Message 'No users are logged on to the system.' -Source $appDeployToolkitName
}

## Log which language's UI messages are loaded from the config XML file
If ($HKUPrimaryLanguageShort) {
    Write-Log -Message "The active logged on user [$($RunAsActiveUser.NTAccount)] has a primary UI language of [$HKUPrimaryLanguageShort]." -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "The current system account [$ProcessNTAccount] has a primary UI language of [$currentLanguage]." -Source $appDeployToolkitName
}
If ($configInstallationUILanguageOverride) {
    Write-Log -Message "The config XML file was configured to override the detected primary UI language with the following UI language: [$configInstallationUILanguageOverride]." -Source $appDeployToolkitName
}
Write-Log -Message "The following UI messages were imported from the config XML file: [$xmlUIMessageLanguage]." -Source $appDeployToolkitName

## Log system DPI scale factor of active logged on user
If ($UserDisplayScaleFactor) {
    Write-Log -Message "The active logged on user [$($RunAsActiveUser.NTAccount)] has a DPI scale factor of [$dpiScale] with DPI pixels [$dpiPixels]." -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "The system has a DPI scale factor of [$dpiScale] with DPI pixels [$dpiPixels]." -Source $appDeployToolkitName
}

## Check if script is running from a SCCM Task Sequence
Try {
    [__ComObject]$SMSTSEnvironment = New-Object -ComObject 'Microsoft.SMS.TSEnvironment' -ErrorAction 'Stop'
    Write-Log -Message 'Successfully loaded COM Object [Microsoft.SMS.TSEnvironment]. Therefore, script is currently running from a SCCM Task Sequence.' -Source $appDeployToolkitName
    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SMSTSEnvironment)
    $runningTaskSequence = $true
}
Catch {
    Write-Log -Message 'Unable to load COM Object [Microsoft.SMS.TSEnvironment]. Therefore, script is not currently running from a SCCM Task Sequence.' -Source $appDeployToolkitName
    $runningTaskSequence = $false
}

## Check to see if the Task Scheduler service is in a healthy state by checking its services to see if they exist, are currently running, and have a start mode of 'Automatic'.
## The task scheduler service and the services it is dependent on can/should only be started/stopped/modified when running in the SYSTEM context.
[Boolean]$IsTaskSchedulerHealthy = $true
If ($IsLocalSystemAccount) {
    #  Check the health of the 'Task Scheduler' service
    Try {
        If (Test-ServiceExists -Name 'Schedule' -ContinueOnError $false) {
            If ((Get-ServiceStartMode -Name 'Schedule' -ContinueOnError $false) -ne 'Automatic') {
                Set-ServiceStartMode -Name 'Schedule' -StartMode 'Automatic' -ContinueOnError $false
            }
            Start-ServiceAndDependencies -Name 'Schedule' -SkipServiceExistsTest -ContinueOnError $false
        }
        Else {
            [Boolean]$IsTaskSchedulerHealthy = $false
        }
    }
    Catch {
        [Boolean]$IsTaskSchedulerHealthy = $false
    }
    #  Log the health of the 'Task Scheduler' service
    Write-Log -Message "The task scheduler service is in a healthy state: $IsTaskSchedulerHealthy." -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "Skipping attempt to check for and make the task scheduler services healthy, because the App Deployment Toolkit is not running under the [$LocalSystemNTAccount] account." -Source $appDeployToolkitName
}

## If script is running in session zero
If ($SessionZero) {
    ##  If the script was launched with deployment mode set to NonInteractive, then continue
    If ($deployMode -eq 'NonInteractive') {
        Write-Log -Message "Session 0 detected but deployment mode was manually set to [$deployMode]." -Source $appDeployToolkitName
    }
    Else {
        ##  If the process is not able to display a UI, enable NonInteractive mode
        If (-not $IsProcessUserInteractive) {
            $deployMode = 'NonInteractive'
            Write-Log -Message "Session 0 detected, process not running in user interactive mode; deployment mode set to [$deployMode]." -Source $appDeployToolkitName
        }
        Else {
            If (-not $usersLoggedOn) {
                $deployMode = 'NonInteractive'
                Write-Log -Message "Session 0 detected, process running in user interactive mode, no users logged in; deployment mode set to [$deployMode]." -Source $appDeployToolkitName
            }
            Else {
                Write-Log -Message 'Session 0 detected, process running in user interactive mode, user(s) logged in.' -Source $appDeployToolkitName
            }
        }
    }
}
Else {
    Write-Log -Message 'Session 0 not detected.' -Source $appDeployToolkitName
}

## Set Deploy Mode switches
If ($deployMode) {
    Write-Log -Message "Installation is running in [$deployMode] mode." -Source $appDeployToolkitName
}
Switch ($deployMode) {
    'Silent' {
        $deployModeSilent = $true
    }
    'NonInteractive' {
        $deployModeNonInteractive = $true; $deployModeSilent = $true
    }
    Default {
        $deployModeNonInteractive = $false; $deployModeSilent = $false
    }
}

## Check deployment type (install/uninstall)
Switch ($deploymentType) {
    'Install' {
        $deploymentTypeName = $configDeploymentTypeInstall
    }
    'Uninstall' {
        $deploymentTypeName = $configDeploymentTypeUnInstall
    }
    'Repair' {
        $deploymentTypeName = $configDeploymentTypeRepair
    }
    Default {
        $deploymentTypeName = $configDeploymentTypeInstall
    }
}
If ($deploymentTypeName) {
    Write-Log -Message "Deployment type is [$deploymentTypeName]." -Source $appDeployToolkitName
}

If ($useDefaultMsi) {
    Write-Log -Message "Discovered Zero-Config MSI installation file [$defaultMsiFile]." -Source $appDeployToolkitName
}

## Check current permissions and exit if not running with Administrator rights
If ($configToolkitRequireAdmin) {
    #  Check if the current process is running with elevated administrator permissions
    If ((-not $IsAdmin) -and (-not $ShowBlockedAppDialog)) {
        [String]$AdminPermissionErr = "[$appDeployToolkitName] has an XML config file option [Toolkit_RequireAdmin] set to [True] so as to require Administrator rights for the toolkit to function. Please re-run the deployment script as an Administrator or change the option in the XML config file to not require Administrator rights."
        Write-Log -Message $AdminPermissionErr -Severity 3 -Source $appDeployToolkitName
        Show-DialogBox -Text $AdminPermissionErr -Icon 'Stop'
        Throw $AdminPermissionErr
    }
}

## If terminal server mode was specified, change the installation mode to support it
If ($terminalServerMode) {
    Enable-TerminalServerInstallMode
}

## If not in install phase Asynchronous, change the install phase so we dont have Initialization phase when we are done initializing
## This should get overwritten shortly, unless this is not dot sourced by Deploy-Application.ps1
If (-not $AsyncToolkitLaunch) {
    $installPhase = 'Execution'
}

#endregion
##*=============================================
##* END SCRIPT BODY
##*=============================================
