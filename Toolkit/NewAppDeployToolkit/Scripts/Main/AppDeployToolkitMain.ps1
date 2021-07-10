<#
.SYNOPSIS
	This script contains the functions and logic engine for the Deploy-Application.ps1 script.
	# LICENSE #
	PowerShell App Deployment Toolkit - Provides a set of functions to perform common application deployment tasks on Windows.
	Copyright (C) 2017 - Sean Lillis, Dan Cunningham, Muhammad Mashwani, Aman Motazedian.
	This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
	You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.
.DESCRIPTION
	The script can be called directly to dot-source the toolkit functions for testing, but it is usually called by the Deploy-Application.ps1 script.
	The script can usually be updated to the latest version without impacting your per-application Deploy-Application scripts.
	Please check release notes before upgrading.
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
.NOTES
	The other parameters specified for this script that are not documented in this help section are for use only by functions in this script that call themselves by running this script again asynchronously.
.LINK
	http://psappdeploytoolkit.com
#>
[CmdletBinding()]
Param (
	## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or because of an Image File Execution Options registry setting
	[switch]$ShowInstallationPrompt = $false,
	[switch]$ShowInstallationRestartPrompt = $false,
	[switch]$CleanupBlockedApps = $false,
	[switch]$ShowBlockedAppDialog = $false,
	[switch]$DisableLogging = $false,
	[string]$ReferredInstallName = '',
	[string]$ReferredInstallTitle = '',
	[string]$ReferredLogName = '',
	[string]$Title = '',
	[string]$Message = '',
	[string]$MessageAlignment = '',
	[string]$ButtonRightText = '',
	[string]$ButtonLeftText = '',
	[string]$ButtonMiddleText = '',
	[string]$Icon = '',
	[string]$Timeout = '',
	[switch]$ExitOnTimeout = $false,
	[boolean]$MinimizeWindows = $false,
	[switch]$PersistPrompt = $false,
	[int32]$CountdownSeconds = 60,
	[int32]$CountdownNoHideSeconds = 30,
	[switch]$NoCountdown = $false,
	[switch]$AsyncToolkitLaunch = $false,
	[bool]$TopMost = $true
)

##*=============================================
##* VARIABLE DECLARATION
##*=============================================
#region VariableDeclaration

## Variables: Toolkit Name
[string]$appDeployToolkitName = 'PSAppDeployToolkit'
[string]$appDeployMainScriptFriendlyName = 'App Deploy Toolkit Main'

## Variables: Script Info
[version]$appDeployMainScriptVersion = [version]'3.8.4'
[version]$appDeployMainScriptMinimumConfigVersion = [version]'3.8.4'
[string]$appDeployMainScriptDate = '26/01/2021'
[hashtable]$appDeployMainScriptParameters = $PSBoundParameters

## Variables: Datetime and Culture
[datetime]$currentDateTime = Get-Date
[string]$currentTime = Get-Date -Date $currentDateTime -UFormat '%T'
[string]$currentDate = Get-Date -Date $currentDateTime -UFormat '%d-%m-%Y'
[timespan]$currentTimeZoneBias = [timezone]::CurrentTimeZone.GetUtcOffset($currentDateTime)
[Globalization.CultureInfo]$culture = Get-Culture
[string]$currentLanguage = $culture.TwoLetterISOLanguageName.ToUpper()
[Globalization.CultureInfo]$uiculture = Get-UICulture
[string]$currentUILanguage = $uiculture.TwoLetterISOLanguageName.ToUpper()

## Variables: Environment Variables
[psobject]$envHost = $Host
[psobject]$envShellFolders = Get-ItemProperty -Path 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -ErrorAction 'SilentlyContinue'
[string]$envAllUsersProfile = $env:ALLUSERSPROFILE
[string]$envAppData = [Environment]::GetFolderPath('ApplicationData')
[string]$envArchitecture = $env:PROCESSOR_ARCHITECTURE
[string]$envCommonDesktop   = $envShellFolders.'Common Desktop'
[string]$envCommonDocuments = $envShellFolders.'Common Documents'
[string]$envCommonStartMenuPrograms  = $envShellFolders.'Common Programs'
[string]$envCommonStartMenu = $envShellFolders.'Common Start Menu'
[string]$envCommonStartUp   = $envShellFolders.'Common Startup'
[string]$envCommonTemplates = $envShellFolders.'Common Templates'
[string]$envComputerName = [Environment]::MachineName.ToUpper()
[string]$envHomeDrive = $env:HOMEDRIVE
[string]$envHomePath = $env:HOMEPATH
[string]$envHomeShare = $env:HOMESHARE
[string]$envLocalAppData = [Environment]::GetFolderPath('LocalApplicationData')
[string[]]$envLogicalDrives = [Environment]::GetLogicalDrives()
[string]$envProgramData = [Environment]::GetFolderPath('CommonApplicationData')
[string]$envPublic = $env:PUBLIC
[string]$envSystemDrive = $env:SYSTEMDRIVE
[string]$envSystemRoot = $env:SYSTEMROOT
[string]$envTemp = [IO.Path]::GetTempPath()
[string]$envUserCookies = [Environment]::GetFolderPath('Cookies')
[string]$envUserDesktop = [Environment]::GetFolderPath('DesktopDirectory')
[string]$envUserFavorites = [Environment]::GetFolderPath('Favorites')
[string]$envUserInternetCache = [Environment]::GetFolderPath('InternetCache')
[string]$envUserInternetHistory = [Environment]::GetFolderPath('History')
[string]$envUserMyDocuments = [Environment]::GetFolderPath('MyDocuments')
[string]$envUserName = [Environment]::UserName
[string]$envUserPictures = [Environment]::GetFolderPath('MyPictures')
[string]$envUserProfile = $env:USERPROFILE
[string]$envUserSendTo = [Environment]::GetFolderPath('SendTo')
[string]$envUserStartMenu = [Environment]::GetFolderPath('StartMenu')
[string]$envUserStartMenuPrograms = [Environment]::GetFolderPath('Programs')
[string]$envUserStartUp = [Environment]::GetFolderPath('StartUp')
[string]$envUserTemplates = [Environment]::GetFolderPath('Templates')
[string]$envSystem32Directory = [Environment]::SystemDirectory
[string]$envWinDir = $env:WINDIR

## Variables: Domain Membership
[boolean]$IsMachinePartOfDomain = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'SilentlyContinue').PartOfDomain
[string]$envMachineWorkgroup = ''
[string]$envMachineADDomain = ''
[string]$envLogonServer = ''
[string]$MachineDomainController = ''
[string]$envComputerNameFQDN = $envComputerName
If ($IsMachinePartOfDomain) {
	[string]$envMachineADDomain = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'SilentlyContinue').Domain | ForEach-Object { if($_){$_.ToLower()} }
	try {
		$envComputerNameFQDN = ([Net.Dns]::GetHostEntry('localhost')).HostName
	}
	catch {
		# Function GetHostEntry failed, but we can construct the FQDN in another way
		$envComputerNameFQDN = $envComputerNameFQDN + "." + $envMachineADDomain
	}

	Try {
		[string]$envLogonServer = $env:LOGONSERVER | ForEach-Object { if(($_) -and (-not $_.Contains('\\MicrosoftAccount'))) { ([Net.Dns]::GetHostEntry($_.TrimStart('\'))).HostName } }
	}
	Catch { }
	# If running in system context or if GetHostEntry fails, fall back on the logonserver value stored in the registry
	If (-not $envLogonServer) { [string]$envLogonServer = (Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\History' -ErrorAction 'SilentlyContinue').DCName }
	## Remove backslashes at the beginning
	while ($envLogonServer.StartsWith('\')) {
		$envLogonServer = $envLogonServer.Substring(1)
	}

	try {
		[string]$MachineDomainController = [DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain().FindDomainController().Name
	} 
	catch {	}
}
Else {
	[string]$envMachineWorkgroup = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'SilentlyContinue').Domain | ForEach-Object { if($_){$_.ToUpper()} }
}
[string]$envMachineDNSDomain = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().DomainName | ForEach-Object { if($_){$_.ToLower()} }
[string]$envUserDNSDomain = $env:USERDNSDOMAIN | ForEach-Object { if($_){$_.ToLower()} }
Try {
	[string]$envUserDomain = [Environment]::UserDomainName.ToUpper()
}
Catch { }

## Variables: Operating System
[psobject]$envOS = Get-WmiObject -Class 'Win32_OperatingSystem' -ErrorAction 'SilentlyContinue'
[string]$envOSName = $envOS.Caption.Trim()
[string]$envOSServicePack = $envOS.CSDVersion
[version]$envOSVersion = $envOS.Version
[string]$envOSVersionMajor = $envOSVersion.Major
[string]$envOSVersionMinor = $envOSVersion.Minor
[string]$envOSVersionBuild = $envOSVersion.Build
If ((Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -ErrorAction 'SilentlyContinue').PSObject.Properties.Name -contains 'UBR') {
	[string]$envOSVersionRevision = (Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'UBR' -ErrorAction 'SilentlyContinue').UBR
}
ElseIf ((Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -ErrorAction 'SilentlyContinue').PSObject.Properties.Name -contains 'BuildLabEx') {
	[string]$envOSVersionRevision = ,((Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'BuildLabEx' -ErrorAction 'SilentlyContinue').BuildLabEx -split '\.') | ForEach-Object { $_[1] }
}
If ($envOSVersionRevision -notmatch '^[\d\.]+$') { $envOSVersionRevision = '' }
If ($envOSVersionRevision) { [string]$envOSVersion = "$($envOSVersion.ToString()).$envOSVersionRevision" } Else { [string]$envOSVersion = "$($envOSVersion.ToString())" }
#  Get the operating system type
[int32]$envOSProductType = $envOS.ProductType
[boolean]$IsServerOS = [boolean]($envOSProductType -eq 3)
[boolean]$IsDomainControllerOS = [boolean]($envOSProductType -eq 2)
[boolean]$IsWorkStationOS = [boolean]($envOSProductType -eq 1)
Switch ($envOSProductType) {
	3 { [string]$envOSProductTypeName = 'Server' }
	2 { [string]$envOSProductTypeName = 'Domain Controller' }
	1 { [string]$envOSProductTypeName = 'Workstation' }
	Default { [string]$envOSProductTypeName = 'Unknown' }
}
#  Get the OS Architecture
[boolean]$Is64Bit = [boolean]((Get-WmiObject -Class 'Win32_Processor' -ErrorAction 'SilentlyContinue' | ForEach-Object { if($_.DeviceID -eq 'CPU0') { $_.AddressWidth} }) -eq 64)
If ($Is64Bit) { [string]$envOSArchitecture = '64-bit' } Else { [string]$envOSArchitecture = '32-bit' }

## Variables: Current Process Architecture
[boolean]$Is64BitProcess = [boolean]([IntPtr]::Size -eq 8)
If ($Is64BitProcess) { [string]$psArchitecture = 'x64' } Else { [string]$psArchitecture = 'x86' }

## Variables: Get Normalized ProgramFiles and CommonProgramFiles Paths
[string]$envProgramFiles = ''
[string]$envProgramFilesX86 = ''
[string]$envCommonProgramFiles = ''
[string]$envCommonProgramFilesX86 = ''
If ($Is64Bit) {
	If ($Is64BitProcess) {
		[string]$envProgramFiles = [Environment]::GetFolderPath('ProgramFiles')
		[string]$envCommonProgramFiles = [Environment]::GetFolderPath('CommonProgramFiles')
	}
	Else {
		[string]$envProgramFiles = [Environment]::GetEnvironmentVariable('ProgramW6432')
		[string]$envCommonProgramFiles = [Environment]::GetEnvironmentVariable('CommonProgramW6432')
	}
	## Powershell 2 doesn't support X86 folders so need to use variables instead
	try {
		[string]$envProgramFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
		[string]$envCommonProgramFilesX86 = [Environment]::GetFolderPath('CommonProgramFilesX86')
	}
	catch {
		[string]$envProgramFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
		[string]$envCommonProgramFilesX86 = [Environment]::GetEnvironmentVariable('CommonProgramFiles(x86)')
	}
}
Else {
	[string]$envProgramFiles = [Environment]::GetFolderPath('ProgramFiles')
	[string]$envProgramFilesX86 = $envProgramFiles
	[string]$envCommonProgramFiles = [Environment]::GetFolderPath('CommonProgramFiles')
	[string]$envCommonProgramFilesX86 = $envCommonProgramFiles
}

## Variables: Hardware
[int32]$envSystemRAM = Get-WMIObject -Class Win32_PhysicalMemory -ComputerName $env:COMPUTERNAME -ErrorAction 'SilentlyContinue' | Measure-Object -Property Capacity -Sum -ErrorAction SilentlyContinue | ForEach-Object {[Math]::Round(($_.sum / 1GB),2)}

## Variables: PowerShell And CLR (.NET) Versions
[hashtable]$envPSVersionTable = $PSVersionTable
#  PowerShell Version
[version]$envPSVersion = $envPSVersionTable.PSVersion
[string]$envPSVersionMajor = $envPSVersion.Major
[string]$envPSVersionMinor = $envPSVersion.Minor
[string]$envPSVersionBuild = $envPSVersion.Build
[string]$envPSVersionRevision = $envPSVersion.Revision
[string]$envPSVersion = $envPSVersion.ToString()
#  CLR (.NET) Version used by PowerShell
[version]$envCLRVersion = $envPSVersionTable.CLRVersion
[string]$envCLRVersionMajor = $envCLRVersion.Major
[string]$envCLRVersionMinor = $envCLRVersion.Minor
[string]$envCLRVersionBuild = $envCLRVersion.Build
[string]$envCLRVersionRevision = $envCLRVersion.Revision
[string]$envCLRVersion = $envCLRVersion.ToString()

## Variables: Permissions/Accounts
[Security.Principal.WindowsIdentity]$CurrentProcessToken = [Security.Principal.WindowsIdentity]::GetCurrent()
[Security.Principal.SecurityIdentifier]$CurrentProcessSID = $CurrentProcessToken.User
[string]$ProcessNTAccount = $CurrentProcessToken.Name
[string]$ProcessNTAccountSID = $CurrentProcessSID.Value
[boolean]$IsAdmin = [boolean]($CurrentProcessToken.Groups -contains [Security.Principal.SecurityIdentifier]'S-1-5-32-544')
[boolean]$IsLocalSystemAccount = $CurrentProcessSID.IsWellKnown([Security.Principal.WellKnownSidType]'LocalSystemSid')
[boolean]$IsLocalServiceAccount = $CurrentProcessSID.IsWellKnown([Security.Principal.WellKnownSidType]'LocalServiceSid')
[boolean]$IsNetworkServiceAccount = $CurrentProcessSID.IsWellKnown([Security.Principal.WellKnownSidType]'NetworkServiceSid')
[boolean]$IsServiceAccount = [boolean]($CurrentProcessToken.Groups -contains [Security.Principal.SecurityIdentifier]'S-1-5-6')
[boolean]$IsProcessUserInteractive = [Environment]::UserInteractive
$GetAccountNameUsingSid = [scriptblock]{ 
	param (
		[string]$SecurityIdentifier = $null
	)
	
	Try {
		return (New-Object -TypeName System.Security.Principal.SecurityIdentifier -ArgumentList ([Security.Principal.WellKnownSidType]::"$SecurityIdentifier", $null)).Translate([System.Security.Principal.NTAccount]).Value
	}
	Catch { 
		return $null 
	}
}	
[string]$LocalSystemNTAccount = & $GetAccountNameUsingSid  'LocalSystemSid'
[string]$LocalUsersGroup = & $GetAccountNameUsingSid 'BuiltinUsersSid'
[string]$LocalPowerUsersGroup = & $GetAccountNameUsingSid  'BuiltinPowerUsersSid'
[string]$LocalAdministratorsGroup = & $GetAccountNameUsingSid 'BuiltinAdministratorsSid'
#  Check if script is running in session zero
If ($IsLocalSystemAccount -or $IsLocalServiceAccount -or $IsNetworkServiceAccount -or $IsServiceAccount) { $SessionZero = $true } Else { $SessionZero = $false }

## Variables: Script Name and Script Paths
[string]$scriptPath = $MyInvocation.MyCommand.Definition
[string]$scriptName = [IO.Path]::GetFileNameWithoutExtension($scriptPath)
[string]$scriptFileName = Split-Path -Path $scriptPath -Leaf
[string]$scriptRoot = Split-Path -Path $scriptPath -Parent
[string]$invokingScript = (Get-Variable -Name 'MyInvocation').Value.ScriptName
#  Get the invoking script directory
If ($invokingScript) {
	#  If this script was invoked by another script
	[string]$scriptParentPath = Split-Path -Path $invokingScript -Parent
}
Else {
	#  If this script was not invoked by another script, fall back to the directory one level above this script
	[string]$scriptParentPath = (Get-Item -LiteralPath $scriptRoot).Parent.FullName
}

## Variables: App Deploy Script Dependency Files
[string]$appDeployConfigFile = Join-Path -Path $scriptRoot -ChildPath 'AppDeployToolkitConfig.xml'
[string]$appDeployCustomTypesSourceCode = Join-Path -Path $scriptRoot -ChildPath 'AppDeployToolkitMain.cs'
If (-not (Test-Path -LiteralPath $appDeployConfigFile -PathType 'Leaf')) { Throw 'App Deploy XML configuration file not found.' }
If (-not (Test-Path -LiteralPath $appDeployCustomTypesSourceCode -PathType 'Leaf')) { Throw 'App Deploy custom types source code file not found.' }

#  App Deploy Optional Extensions File
[string]$appDeployToolkitDotSourceExtensions = 'AppDeployToolkitExtensions.ps1'

## Import variables from XML configuration file
[Xml.XmlDocument]$xmlConfigFile = Get-Content -LiteralPath $AppDeployConfigFile -Encoding UTF8
[Xml.XmlElement]$xmlConfig = $xmlConfigFile.AppDeployToolkit_Config
#  Get Config File Details
[Xml.XmlElement]$configConfigDetails = $xmlConfig.Config_File
[string]$configConfigVersion = [version]$configConfigDetails.Config_Version
[string]$configConfigDate = $configConfigDetails.Config_Date

# Get Banner and Icon details
[Xml.XmlElement]$xmlBannerIconOptions = $xmlConfig.BannerIcon_Options
[string]$configBannerIconFileName = $xmlBannerIconOptions.Icon_Filename
[string]$configBannerIconBannerName = $xmlBannerIconOptions.Banner_Filename
[Int32]$appDeployLogoBannerMaxHeight = $xmlBannerIconOptions.Banner_MaxHeight

[string]$appDeployLogoIcon = Join-Path -Path $scriptRoot -ChildPath $configBannerIconFileName
[string]$appDeployLogoBanner = Join-Path -Path $scriptRoot -ChildPath $configBannerIconBannerName
#  Check that dependency files are present
If (-not (Test-Path -LiteralPath $appDeployLogoIcon -PathType 'Leaf')) { Throw 'App Deploy logo icon file not found.' }
If (-not (Test-Path -LiteralPath $appDeployLogoBanner -PathType 'Leaf')) { Throw 'App Deploy logo banner file not found.' }

#  Get Toolkit Options
[Xml.XmlElement]$xmlToolkitOptions = $xmlConfig.Toolkit_Options
[boolean]$configToolkitRequireAdmin = [boolean]::Parse($xmlToolkitOptions.Toolkit_RequireAdmin)
[string]$configToolkitTempPath = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_TempPath)
[string]$configToolkitRegPath = $xmlToolkitOptions.Toolkit_RegPath
[string]$configToolkitLogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_LogPath)
[boolean]$configToolkitCompressLogs = [boolean]::Parse($xmlToolkitOptions.Toolkit_CompressLogs)
[string]$configToolkitLogStyle = $xmlToolkitOptions.Toolkit_LogStyle
[double]$configToolkitLogMaxSize = $xmlToolkitOptions.Toolkit_LogMaxSize
[boolean]$configToolkitLogWriteToHost = [boolean]::Parse($xmlToolkitOptions.Toolkit_LogWriteToHost)
[boolean]$configToolkitLogDebugMessage = [boolean]::Parse($xmlToolkitOptions.Toolkit_LogDebugMessage)
#  Get MSI Options
[Xml.XmlElement]$xmlConfigMSIOptions = $xmlConfig.MSI_Options
[string]$configMSILoggingOptions = $xmlConfigMSIOptions.MSI_LoggingOptions
[string]$configMSIInstallParams = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_InstallParams)
[string]$configMSISilentParams = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_SilentParams)
[string]$configMSIUninstallParams = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_UninstallParams)
[string]$configMSILogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_LogPath)
[int32]$configMSIMutexWaitTime = $xmlConfigMSIOptions.MSI_MutexWaitTime
#  Change paths to user accessible ones if RequireAdmin is false
If ($configToolkitRequireAdmin -eq $false){
	If ($xmlToolkitOptions.Toolkit_TempPathNoAdminRights) {
		[string]$configToolkitTempPath = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_TempPathNoAdminRights)
	}
	If ($xmlToolkitOptions.Toolkit_RegPathNoAdminRights) {
		[string]$configToolkitRegPath = $xmlToolkitOptions.Toolkit_RegPathNoAdminRights
	}
	If ($xmlToolkitOptions.Toolkit_LogPathNoAdminRights) {
		[string]$configToolkitLogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_LogPathNoAdminRights)
	}
	If ($xmlConfigMSIOptions.MSI_LogPathNoAdminRights) {
		[string]$configMSILogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_LogPathNoAdminRights)
	}
}
#  Get UI Options
[Xml.XmlElement]$xmlConfigUIOptions = $xmlConfig.UI_Options
[string]$configInstallationUILanguageOverride = $xmlConfigUIOptions.InstallationUI_LanguageOverride
[boolean]$configShowBalloonNotifications = [boolean]::Parse($xmlConfigUIOptions.ShowBalloonNotifications)
[int32]$configInstallationUITimeout = $xmlConfigUIOptions.InstallationUI_Timeout
[int32]$configInstallationUIExitCode = $xmlConfigUIOptions.InstallationUI_ExitCode
[int32]$configInstallationDeferExitCode = $xmlConfigUIOptions.InstallationDefer_ExitCode
[int32]$configInstallationPersistInterval = $xmlConfigUIOptions.InstallationPrompt_PersistInterval
[int32]$configInstallationRestartPersistInterval = $xmlConfigUIOptions.InstallationRestartPrompt_PersistInterval
[int32]$configInstallationPromptToSave = $xmlConfigUIOptions.InstallationPromptToSave_Timeout
[boolean]$configInstallationWelcomePromptDynamicRunningProcessEvaluation = [boolean]::Parse($xmlConfigUIOptions.InstallationWelcomePrompt_DynamicRunningProcessEvaluation)
[int32]$configInstallationWelcomePromptDynamicRunningProcessEvaluationInterval = $xmlConfigUIOptions.InstallationWelcomePrompt_DynamicRunningProcessEvaluationInterval
#  Define ScriptBlock for Loading Message UI Language Options (default for English if no localization found)
[scriptblock]$xmlLoadLocalizedUIMessages = {
	#  If a user is logged on, then get primary UI language for logged on user (even if running in session 0)
	If ($RunAsActiveUser) {
		#  Read language defined by Group Policy
		If (-not $HKULanguages) {
			[string[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MUI\Settings' -Value 'PreferredUILanguages'
		}
		If (-not $HKULanguages) {
			[string[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Control Panel\Desktop' -Value 'PreferredUILanguages' -SID $RunAsActiveUser.SID
		}
		#  Read language for Win Vista & higher machines
		If (-not $HKULanguages) {
			[string[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop' -Value 'PreferredUILanguages' -SID $RunAsActiveUser.SID
		}
		If (-not $HKULanguages) {
			[string[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop\MuiCached' -Value 'MachinePreferredUILanguages' -SID $RunAsActiveUser.SID
		}
		If (-not $HKULanguages) {
			[string[]]$HKULanguages = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\International' -Value 'LocaleName' -SID $RunAsActiveUser.SID
		}
		#  Read language for Win XP machines
		If (-not $HKULanguages) {
			[string]$HKULocale = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\International' -Value 'Locale' -SID $RunAsActiveUser.SID
			If ($HKULocale) {
				[int32]$HKULocale = [Convert]::ToInt32('0x' + $HKULocale, 16)
				[string[]]$HKULanguages = ([Globalization.CultureInfo]($HKULocale)).Name
			}
		}
		If ($HKULanguages) {
			[Globalization.CultureInfo]$PrimaryWindowsUILanguage = [Globalization.CultureInfo]($HKULanguages[0])
			[string]$HKUPrimaryLanguageShort = $PrimaryWindowsUILanguage.TwoLetterISOLanguageName.ToUpper()

			#  If the detected language is Chinese, determine if it is simplified or traditional Chinese
			If ($HKUPrimaryLanguageShort -eq 'ZH') {
				If ($PrimaryWindowsUILanguage.EnglishName -match 'Simplified') {
					[string]$HKUPrimaryLanguageShort = 'ZH-Hans'
				}
				If ($PrimaryWindowsUILanguage.EnglishName -match 'Traditional') {
					[string]$HKUPrimaryLanguageShort = 'ZH-Hant'
				}
			}

			#  If the detected language is Portuguese, determine if it is Brazilian Portuguese
			If ($HKUPrimaryLanguageShort -eq 'PT') {
				If ($PrimaryWindowsUILanguage.ThreeLetterWindowsLanguageName -eq 'PTB') {
					[string]$HKUPrimaryLanguageShort = 'PT-BR'
				}
			}
		}
	}

	If ($HKUPrimaryLanguageShort) {
		#  Use the primary UI language of the logged in user
		[string]$xmlUIMessageLanguage = "UI_Messages_$HKUPrimaryLanguageShort"
	}
	Else {
		#  Default to UI language of the account executing current process (even if it is the SYSTEM account)
		[string]$xmlUIMessageLanguage = "UI_Messages_$currentLanguage"
	}
	#  Default to English if the detected UI language is not available in the XMl config file
	If (-not ($xmlConfig.$xmlUIMessageLanguage)) { [string]$xmlUIMessageLanguage = 'UI_Messages_EN' }
	#  Override the detected language if the override option was specified in the XML config file
	If ($configInstallationUILanguageOverride) { [string]$xmlUIMessageLanguage = "UI_Messages_$configInstallationUILanguageOverride" }

	[Xml.XmlElement]$xmlUIMessages = $xmlConfig.$xmlUIMessageLanguage
	[string]$configDiskSpaceMessage = $xmlUIMessages.DiskSpace_Message
	[string]$configBalloonTextStart = $xmlUIMessages.BalloonText_Start
	[string]$configBalloonTextComplete = $xmlUIMessages.BalloonText_Complete
	[string]$configBalloonTextRestartRequired = $xmlUIMessages.BalloonText_RestartRequired
	[string]$configBalloonTextFastRetry = $xmlUIMessages.BalloonText_FastRetry
	[string]$configBalloonTextError = $xmlUIMessages.BalloonText_Error
	[string]$configProgressMessageInstall = $xmlUIMessages.Progress_MessageInstall
	[string]$configProgressMessageUninstall = $xmlUIMessages.Progress_MessageUninstall
	[string]$configProgressMessageRepair = $xmlUIMessages.Progress_MessageRepair
	[string]$configClosePromptMessage = $xmlUIMessages.ClosePrompt_Message
	[string]$configClosePromptButtonClose = $xmlUIMessages.ClosePrompt_ButtonClose
	[string]$configClosePromptButtonDefer = $xmlUIMessages.ClosePrompt_ButtonDefer
	[string]$configClosePromptButtonContinue = $xmlUIMessages.ClosePrompt_ButtonContinue
	[string]$configClosePromptButtonContinueTooltip = $xmlUIMessages.ClosePrompt_ButtonContinueTooltip
	[string]$configClosePromptCountdownMessage = $xmlUIMessages.ClosePrompt_CountdownMessage
	[string]$configDeferPromptWelcomeMessage = $xmlUIMessages.DeferPrompt_WelcomeMessage
	[string]$configDeferPromptExpiryMessage = $xmlUIMessages.DeferPrompt_ExpiryMessage
	[string]$configDeferPromptWarningMessage = $xmlUIMessages.DeferPrompt_WarningMessage
	[string]$configDeferPromptRemainingDeferrals = $xmlUIMessages.DeferPrompt_RemainingDeferrals
	[string]$configDeferPromptDeadline = $xmlUIMessages.DeferPrompt_Deadline
	[string]$configBlockExecutionMessage = $xmlUIMessages.BlockExecution_Message
	[string]$configDeploymentTypeInstall = $xmlUIMessages.DeploymentType_Install
	[string]$configDeploymentTypeUnInstall = $xmlUIMessages.DeploymentType_UnInstall
	[string]$configDeploymentTypeRepair = $xmlUIMessages.DeploymentType_Repair
	[string]$configRestartPromptTitle = $xmlUIMessages.RestartPrompt_Title
	[string]$configRestartPromptMessage = $xmlUIMessages.RestartPrompt_Message
	[string]$configRestartPromptMessageTime = $xmlUIMessages.RestartPrompt_MessageTime
	[string]$configRestartPromptMessageRestart = $xmlUIMessages.RestartPrompt_MessageRestart
	[string]$configRestartPromptTimeRemaining = $xmlUIMessages.RestartPrompt_TimeRemaining
	[string]$configRestartPromptButtonRestartLater = $xmlUIMessages.RestartPrompt_ButtonRestartLater
	[string]$configRestartPromptButtonRestartNow = $xmlUIMessages.RestartPrompt_ButtonRestartNow
	[string]$configWelcomePromptCountdownMessage = $xmlUIMessages.WelcomePrompt_CountdownMessage
	[string]$configWelcomePromptCustomMessage = $xmlUIMessages.WelcomePrompt_CustomMessage
}

## Variables: Script Directories
[string]$dirFiles = Join-Path -Path $scriptParentPath -ChildPath 'Files'
[string]$dirSupportFiles = Join-Path -Path $scriptParentPath -ChildPath 'SupportFiles'
[string]$dirAppDeployTemp = Join-Path -Path $configToolkitTempPath -ChildPath $appDeployToolkitName

## Set the deployment type to "Install" if it has not been specified
If (-not $deploymentType) { [string]$deploymentType = 'Install' }

## Variables: Executables
[string]$exeWusa = "$envWinDir\System32\wusa.exe" # Installs Standalone Windows Updates
[string]$exeMsiexec = "$envWinDir\System32\msiexec.exe" # Installs MSI Installers
[string]$exeSchTasks = "$envWinDir\System32\schtasks.exe" # Manages Scheduled Tasks

## Variables: RegEx Patterns
[string]$MSIProductCodeRegExPattern = '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$'

## Variables: Invalid FileName Characters
[char[]]$invalidFileNameChars = [IO.Path]::GetinvalidFileNameChars()

## Variables: Registry Keys
#  Registry keys for native and WOW64 applications
[string[]]$regKeyApplications = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall','Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
If ($is64Bit) {
	[string]$regKeyLotusNotes = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Lotus\Notes'
}
Else {
	[string]$regKeyLotusNotes = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Lotus\Notes'
}
[string]$regKeyAppExecution = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options'

## COM Objects: Initialize
[__comobject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'SilentlyContinue'
[__comobject]$ShellApp = New-Object -ComObject 'Shell.Application' -ErrorAction 'SilentlyContinue'

## Variables: Reset/Remove Variables
[boolean]$msiRebootDetected = $false
[boolean]$BlockExecution = $false
[boolean]$installationStarted = $false
[boolean]$runningTaskSequence = $false
If (Test-Path -LiteralPath 'variable:welcomeTimer') { Remove-Variable -Name 'welcomeTimer' -Scope 'Script'}
#  Reset the deferral history
If (Test-Path -LiteralPath 'variable:deferHistory') { Remove-Variable -Name 'deferHistory' }
If (Test-Path -LiteralPath 'variable:deferTimes') { Remove-Variable -Name 'deferTimes' }
If (Test-Path -LiteralPath 'variable:deferDays') { Remove-Variable -Name 'deferDays' }

## Variables: System DPI Scale Factor (Requires PSADT.UiAutomation loaded)
[scriptblock]$GetDisplayScaleFactor = {
	#  If a user is logged on, then get display scale factor for logged on user (even if running in session 0)
	[boolean]$UserDisplayScaleFactor = $false
	[System.Drawing.Graphics]$GraphicsObject = $null
	[IntPtr]$DeviceContextHandle = [IntPtr]::Zero
	[int32]$dpiScale = 0
	[int32]$dpiPixels = 0

	try {
		# Get Graphics Object from the current Window Handle
		[System.Drawing.Graphics]$GraphicsObject = [System.Drawing.Graphics]::FromHwnd([IntPtr]::Zero);
		# Get Device Context Handle
		[IntPtr]$DeviceContextHandle = $GraphicsObject.GetHdc();
		# Get Logical and Physical screen height
		[int32]$LogicalScreenHeight = [PSADT.UiAutomation]::GetDeviceCaps($DeviceContextHandle, [int][PSADT.UiAutomation+DeviceCap]::VERTRES);
		[int32]$PhysicalScreenHeight = [PSADT.UiAutomation]::GetDeviceCaps($DeviceContextHandle, [int][PSADT.UiAutomation+DeviceCap]::DESKTOPVERTRES);
		# Calculate dpi scale and pixels
		[int32]$dpiScale = [Math]::Round([double]$PhysicalScreenHeight / [double]$LogicalScreenHeight, 2) * 100;
		[int32]$dpiPixels = [Math]::Round(($dpiScale / 100)*96,0)
	}
	catch {
		[int32]$dpiScale = 0
		[int32]$dpiPixels = 0
	}
	finally {
		# Release the device context handle and dispose of the graphics object
		if ($GraphicsObject -ne $null) {
			if ($DeviceContextHandle -ne [IntPtr]::Zero) {
				$GraphicsObject.ReleaseHdc($DeviceContextHandle);
			}
			$GraphicsObject.Dispose();
		}
	}
	# Failed to get dpi, try to read them from registry - Might not be accurate
	If ($RunAsActiveUser) {
		If ($dpiPixels -lt 1) {
			[int32]$dpiPixels = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics' -Value 'AppliedDPI' -SID $RunAsActiveUser.SID
		}
		If ($dpiPixels -lt 1) {
			[int32]$dpiPixels = Get-RegistryKey -Key 'Registry::HKEY_CURRENT_USER\Control Panel\Desktop' -Value 'LogPixels' -SID $RunAsActiveUser.SID
		}
		[boolean]$UserDisplayScaleFactor = $true
	}
	# Failed to get dpi from first two registry entries, try to read FontDPI - Usually inaccurate
	If ($dpiPixels -lt 1) {
		#  This registry setting only exists if system scale factor has been changed at least once
		[int32]$dpiPixels = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontDPI' -Value 'LogPixels'
		[boolean]$UserDisplayScaleFactor = $false
	}
	# Calculate dpi scale if its empty and we have dpi pixels
	if (($dpiScale -lt 1) -and ($dpiPixels -gt 0)) {
		[int32]$dpiScale = [Math]::Round(($dpiPixels * 100)/96)
	}
}
## Variables: Resolve Parameters. For use in a pipeline
[scriptblock]$ResolveParameters = {
	# We have to save current pipeline object $_ because switch has its own $_
	$item = $_
	switch ($item.Value.GetType().Name) {
		'SwitchParameter' {
			"-$($item.Key):`$$($item.Value.tostring().toLower())"
		}
		'Boolean' {
			"-$($item.Key):`$$($item.Value.tostring().toLower())"
		}
		'Int16' {
			"-$($item.Key):$($item.Value)"
		}
		'Int32' {
			"-$($item.Key):$($item.Value)"
		}
		'Int64' {
			"-$($item.Key):$($item.Value)"
		}
		'UInt16' {
			"-$($item.Key):$($item.Value)"
		}
		'UInt32' {
			"-$($item.Key):$($item.Value)"
		}
		'UInt64' {
			"-$($item.Key):$($item.Value)"
		}
		'Single' {
			"-$($item.Key):$($item.Value)"
		}
		'Double' {
			"-$($item.Key):$($item.Value)"
		}
		'Decimal' {
			"-$($item.Key):$($item.Value)"
		}
		default {
			"-$($item.Key):`'$($item.Value)`'"
		}
	}
}
#endregion
##*=============================================
##* END VARIABLE DECLARATION
##*=============================================

##*=============================================
##* SCRIPT BODY
##*=============================================
#region ScriptBody

## If the script was invoked by the Help Console, exit the script now
If ($invokingScript) {
	If ((Split-Path -Path $invokingScript -Leaf) -eq 'AppDeployToolkitHelp.ps1') { Return }
}

## Add the custom types required for the toolkit
If (-not ([Management.Automation.PSTypeName]'PSADT.UiAutomation').Type) {
	[string[]]$ReferencedAssemblies = 'System.Drawing', 'System.Windows.Forms', 'System.DirectoryServices'
	Add-Type -Path $appDeployCustomTypesSourceCode -ReferencedAssemblies $ReferencedAssemblies -IgnoreWarnings -ErrorAction 'Stop'
}

## Define ScriptBlocks to disable/revert script logging
[scriptblock]$DisableScriptLogging = { $OldDisableLoggingValue = $DisableLogging ; $DisableLogging = $true }
[scriptblock]$RevertScriptLogging = { $DisableLogging = $OldDisableLoggingValue }

## Define ScriptBlock for getting details for all logged on users
[scriptblock]$GetLoggedOnUserDetails = {
	[psobject[]]$LoggedOnUserSessions = Get-LoggedOnUser
	[string[]]$usersLoggedOn = $LoggedOnUserSessions | ForEach-Object { $_.NTAccount }

	If ($usersLoggedOn) {
		#  Get account and session details for the logged on user session that the current process is running under. Note that the account used to execute the current process may be different than the account that is logged into the session (i.e. you can use "RunAs" to launch with different credentials when logged into an account).
		[psobject]$CurrentLoggedOnUserSession = $LoggedOnUserSessions | Where-Object { $_.IsCurrentSession }

		#  Get account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
		[psobject]$CurrentConsoleUserSession = $LoggedOnUserSessions | Where-Object { $_.IsConsoleSession }

		## Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
		#  If a console user exists, then that will be the active user session.
		#  If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is either 'Active' or 'Connected' is the active user.
		[psobject]$RunAsActiveUser = $LoggedOnUserSessions | Where-Object { $_.IsActiveUserSession }
	}
}

## Disable logging until log file details are available
. $DisableScriptLogging

## If the default Deploy-Application.ps1 hasn't been modified, and the main script was not called by a referring script, check for MSI / MST and modify the install accordingly
If ((-not $appName) -and (-not $ReferredInstallName)){
	# Build properly formatted Architecture String
	switch ($Is64Bit) {
       	$false { $formattedOSArch = "x86" }
       	$true { $formattedOSArch = "x64" }
    }
	#  Find the first MSI file in the Files folder and use that as our install
	if ([string]$defaultMsiFile = (Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq ".msi") -and ($_.Name.EndsWith(".$formattedOSArch.msi")) } | Select-Object -ExpandProperty 'FullName' -First 1)) {
		Write-Log -Message "Discovered $formattedOSArch Zerotouch MSI under $defaultMSIFile" -Source $appDeployToolkitName
	}
	elseif ([string]$defaultMsiFile = (Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq ".msi") } | Select-Object -ExpandProperty 'FullName' -First 1)) {
		Write-Log -Message "Discovered Arch-Independent Zerotouch MSI under $defaultMSIFile" -Source $appDeployToolkitName
	}
	If ($defaultMsiFile) {
		Try {
			[boolean]$useDefaultMsi = $true
			Write-Log -Message "Discovered Zero-Config MSI installation file [$defaultMsiFile]." -Source $appDeployToolkitName
			#  Discover if there is a zero-config MST file
			[string]$defaultMstFile = [IO.Path]::ChangeExtension($defaultMsiFile, 'mst')
			If (Test-Path -LiteralPath $defaultMstFile -PathType 'Leaf') {
				Write-Log -Message "Discovered Zero-Config MST installation file [$defaultMstFile]." -Source $appDeployToolkitName
			}
			Else {
				[string]$defaultMstFile = ''
			}
			#  Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
			[string[]]$defaultMspFiles = Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | ForEach-Object { if((-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq '.msp')) {$_.FullName} }
			If ($defaultMspFiles) {
				Write-Log -Message "Discovered Zero-Config MSP installation file(s) [$($defaultMspFiles -join ',')]." -Source $appDeployToolkitName
			}

			## Read the MSI and get the installation details
			[hashtable]$GetDefaultMsiTablePropertySplat = @{ Path = $defaultMsiFile; Table = 'Property'; ContinueOnError = $false; ErrorAction = 'Stop' }
			If ($defaultMstFile) { $GetDefaultMsiTablePropertySplat.Add('TransformPath', $defaultMstFile) }
			[psobject]$defaultMsiPropertyList = Get-MsiTableProperty @GetDefaultMsiTablePropertySplat
			[string]$appVendor = $defaultMsiPropertyList.Manufacturer
			[string]$appName = $defaultMsiPropertyList.ProductName
			[string]$appVersion = $defaultMsiPropertyList.ProductVersion
			$GetDefaultMsiTablePropertySplat.Set_Item('Table', 'File')
			[psobject]$defaultMsiFileList = Get-MsiTableProperty @GetDefaultMsiTablePropertySplat
			[string[]]$defaultMsiExecutables = Get-Member -InputObject $defaultMsiFileList -ErrorAction 'Stop' | ForEach-Object { if([IO.Path]::GetExtension($_.Name) -eq '.exe') {[IO.Path]::GetFileNameWithoutExtension($_.Name)} }
			[string]$defaultMsiExecutablesList = $defaultMsiExecutables -join ','
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
	[string]$appName = $appDeployMainScriptFriendlyName
	If (-not $appVendor) { [string]$appVendor = 'PS' }
	If (-not $appVersion) { [string]$appVersion = $appDeployMainScriptVersion }
	If (-not $appLang) { [string]$appLang = $currentLanguage }
	If (-not $appRevision) { [string]$appRevision = '01' }
	If (-not $appArch) { [string]$appArch = '' }
} else {
	If (-not $appVendor) { [string]$appVendor = '' }
	If (-not $appVersion) { [string]$appVersion = '' }
	If (-not $appLang) { [string]$appLang = '' }
	If (-not $appRevision) { [string]$appRevision = '' }
	If (-not $appArch) { [string]$appArch = '' }
}

## Sanitize the application details, as they can cause issues in the script
[string]$appVendor = (Remove-InvalidFileNameChars -Name ($appVendor.Trim()))
[string]$appName = (Remove-InvalidFileNameChars -Name ($appName.Trim()))
[string]$appVersion = (Remove-InvalidFileNameChars -Name ($appVersion.Trim()))
[string]$appArch = (Remove-InvalidFileNameChars -Name ($appArch.Trim()))
[string]$appLang = (Remove-InvalidFileNameChars -Name ($appLang.Trim()))
[string]$appRevision = (Remove-InvalidFileNameChars -Name ($appRevision.Trim()))

## Build the Installation Title
If ($ReferredInstallTitle) { [string]$installTitle = (Remove-InvalidFileNameChars -Name ($ReferredInstallTitle.Trim())) }
If (-not $installTitle) {
	[string]$installTitle = "$appVendor $appName $appVersion"
}

## Set Powershell window title, in case the window is visible
[string]$oldPSWindowTitle = $Host.UI.RawUI.WindowTitle
$Host.UI.RawUI.WindowTitle = "$installTitle - $DeploymentType"

## Build the Installation Name
If ($ReferredInstallName) { [string]$installName = (Remove-InvalidFileNameChars -Name $ReferredInstallName) }
If (-not $installName) {
	If ($appArch) {
		[string]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appArch + '_' + $appLang + '_' + $appRevision
	}
	Else {
		[string]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appLang + '_' + $appRevision
	}
}
[string]$installName = (($installName -replace ' ','').Trim('_') -replace '[_]+','_')

## Set the Defer History registry path
[string]$regKeyDeferHistory = "$configToolkitRegPath\$appDeployToolkitName\DeferHistory\$installName"

## Variables: Log Files
If ($ReferredLogName) { [string]$logName = $ReferredLogName }
If (-not $logName) { [string]$logName = $installName + '_' + $appDeployToolkitName + '_' + $deploymentType + '.log' }
#  If option to compress logs is selected, then log will be created in temp log folder ($logTempFolder) and then copied to actual log folder ($configToolkitLogDir) after being zipped.
[string]$logTempFolder = Join-Path -Path $envTemp -ChildPath "${installName}_$deploymentType"
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
Write-Log -Message ($scriptSeparator,$scriptSeparator) -Source $appDeployToolkitName
Write-Log -Message "[$installName] setup started." -Source $appDeployToolkitName

## Assemblies: Load
Try {
	Add-Type -AssemblyName 'System.Drawing','System.Windows.Forms','PresentationFramework','Microsoft.VisualBasic','PresentationCore','WindowsBase' -ErrorAction 'Stop'
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
try {
	[System.Drawing.Bitmap]$appDeployLogoBannerObject = New-Object System.Drawing.Bitmap $appDeployLogoBanner
	[Int32]$appDeployLogoBannerHeight = $appDeployLogoBannerObject.Height
	if ($appDeployLogoBannerHeight -gt $appDeployLogoBannerMaxHeight) {
		$appDeployLogoBannerHeight = $appDeployLogoBannerMaxHeight
	}
}
catch { }

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
If ($deployAppScriptParameters) { [string]$deployAppScriptParameters = ($deployAppScriptParameters.GetEnumerator() | ForEach-Object $ResolveParameters) -join ' ' }
#  Save main script parameters hashtable for async execution of the toolkit
[hashtable]$appDeployMainScriptAsyncParameters = $appDeployMainScriptParameters
If ($appDeployMainScriptParameters) { [string]$appDeployMainScriptParameters = ($appDeployMainScriptParameters.GetEnumerator() | ForEach-Object $ResolveParameters) -join ' ' }
If ($appDeployExtScriptParameters) { [string]$appDeployExtScriptParameters = ($appDeployExtScriptParameters.GetEnumerator() | ForEach-Object $ResolveParameters) -join ' ' }

## Check the XML config file version
If ($configConfigVersion -lt $appDeployMainScriptMinimumConfigVersion) {
	[string]$XMLConfigVersionErr = "The XML configuration file version [$configConfigVersion] is lower than the supported version required by the Toolkit [$appDeployMainScriptMinimumConfigVersion]. Please upgrade the configuration file."
	Write-Log -Message $XMLConfigVersionErr -Severity 3 -Source $appDeployToolkitName
	Throw $XMLConfigVersionErr
}

## Log system/script information
If ($appScriptVersion) { Write-Log -Message "[$installName] script version is [$appScriptVersion]" -Source $appDeployToolkitName }
If ($appScriptDate) { Write-Log -Message "[$installName] script date is [$appScriptDate]" -Source $appDeployToolkitName }
If ($appScriptAuthor) { Write-Log -Message "[$installName] script author is [$appScriptAuthor]" -Source $appDeployToolkitName }
If ($deployAppScriptFriendlyName) { Write-Log -Message "[$deployAppScriptFriendlyName] script version is [$deployAppScriptVersion]" -Source $appDeployToolkitName }
If ($deployAppScriptParameters) { Write-Log -Message "The following non-default parameters were passed to [$deployAppScriptFriendlyName]: [$deployAppScriptParameters]" -Source $appDeployToolkitName }
If ($appDeployMainScriptFriendlyName) { Write-Log -Message "[$appDeployMainScriptFriendlyName] script version is [$appDeployMainScriptVersion]" -Source $appDeployToolkitName }
If ($appDeployMainScriptParameters) { Write-Log -Message "The following non-default parameters were passed to [$appDeployMainScriptFriendlyName]: [$appDeployMainScriptParameters]" -Source $appDeployToolkitName }
If ($appDeployExtScriptFriendlyName) { Write-Log -Message "[$appDeployExtScriptFriendlyName] version is [$appDeployExtScriptVersion]" -Source $appDeployToolkitName }
If ($appDeployExtScriptParameters) { Write-Log -Message "The following non-default parameters were passed to [$appDeployExtScriptFriendlyName]: [$appDeployExtScriptParameters]" -Source $appDeployToolkitName }
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
		[boolean]$showBlockedAppDialogMutexLocked = $false
		[string]$showBlockedAppDialogMutexName = 'Global\PSADT_ShowBlockedAppDialog_Message'
		[Threading.Mutex]$showBlockedAppDialogMutex = New-Object -TypeName 'System.Threading.Mutex' -ArgumentList ($false, $showBlockedAppDialogMutexName)
		#  Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock
		If ((Test-IsMutexAvailable -MutexName $showBlockedAppDialogMutexName -MutexWaitTimeInMilliseconds 1) -and ($showBlockedAppDialogMutex.WaitOne(1))) {
			[boolean]$showBlockedAppDialogMutexLocked = $true
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
		If ($showBlockedAppDialogMutexLocked) { $null = $showBlockedAppDialogMutex.ReleaseMutex() }
		If ($showBlockedAppDialogMutex) { $showBlockedAppDialogMutex.Close() }
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
If ($configInstallationUILanguageOverride) { Write-Log -Message "The config XML file was configured to override the detected primary UI language with the following UI language: [$configInstallationUILanguageOverride]." -Source $appDeployToolkitName }
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
	[__comobject]$SMSTSEnvironment = New-Object -ComObject 'Microsoft.SMS.TSEnvironment' -ErrorAction 'Stop'
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
[boolean]$IsTaskSchedulerHealthy = $true
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
			[boolean]$IsTaskSchedulerHealthy = $false
		}
	}
	Catch {
		[boolean]$IsTaskSchedulerHealthy = $false
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
	'Silent' { $deployModeSilent = $true }
	'NonInteractive' { $deployModeNonInteractive = $true; $deployModeSilent = $true }
	Default { $deployModeNonInteractive = $false; $deployModeSilent = $false }
}

## Check deployment type (install/uninstall)
Switch ($deploymentType) {
	'Install'   { $deploymentTypeName = $configDeploymentTypeInstall }
	'Uninstall' { $deploymentTypeName = $configDeploymentTypeUnInstall }
	'Repair' { $deploymentTypeName = $configDeploymentTypeRepair }
	Default { $deploymentTypeName = $configDeploymentTypeInstall }
}
If ($deploymentTypeName) { Write-Log -Message "Deployment type is [$deploymentTypeName]." -Source $appDeployToolkitName }

If ($useDefaultMsi) { Write-Log -Message "Discovered Zero-Config MSI installation file [$defaultMsiFile]." -Source $appDeployToolkitName }

## Check current permissions and exit if not running with Administrator rights
If ($configToolkitRequireAdmin) {
	#  Check if the current process is running with elevated administrator permissions
	If ((-not $IsAdmin) -and (-not $ShowBlockedAppDialog)) {
		[string]$AdminPermissionErr = "[$appDeployToolkitName] has an XML config file option [Toolkit_RequireAdmin] set to [True] so as to require Administrator rights for the toolkit to function. Please re-run the deployment script as an Administrator or change the option in the XML config file to not require Administrator rights."
		Write-Log -Message $AdminPermissionErr -Severity 3 -Source $appDeployToolkitName
		Show-DialogBox -Text $AdminPermissionErr -Icon 'Stop'
		Throw $AdminPermissionErr
	}
}

## If terminal server mode was specified, change the installation mode to support it
If ($terminalServerMode) { Enable-TerminalServerInstallMode }

## If not in install phase Asynchronous, change the install phase so we dont have Initialization phase when we are done initializing
## This should get overwritten shortly, unless this is not dot sourced by Deploy-Application.ps1
If (-not $AsyncToolkitLaunch) {
	$installPhase = 'Execution'
}

#endregion
##*=============================================
##* END SCRIPT BODY
##*=============================================
