<#
.SYNOPSIS
	This script contains the functions and logic engine for the Deploy-Application.ps1 script.
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
.PARAMETER ReferringApplication
	Title of the referring application that invoked the script externally.
	This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.
.NOTES
	The other parameters specified for this script that are not documented in this help section are for use only by functions in this script that call themselves by running this script again asynchronously.
.LINK
	http://psappdeploytoolkit.com
#>
[CmdletBinding()]
Param
(
	## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or because of an Image File Execution Options registry setting
	[switch]$ShowInstallationPrompt = $false,
	[switch]$ShowInstallationRestartPrompt = $false,
	[switch]$CleanupBlockedApps = $false,
	[switch]$ShowBlockedAppDialog = $false,
	[switch]$DisableLogging = $false,
	[string]$ReferringApplication = '',
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
	[int32]$CountdownSeconds,
	[int32]$CountdownNoHideSeconds,
	[switch]$NoCountdown = $false,
	[switch]$RelaunchToolkitAsUser = $false
)

##*=============================================
##* VARIABLE DECLARATION
##*=============================================
#region VariableDeclaration

## Variables: Toolkit Name
[string]$appDeployToolkitName = 'PSAppDeployToolkit'
[string]$appDeployMainScriptFriendlyName = 'App Deploy Toolkit Main'

## Variables: Script Info
[version]$appDeployMainScriptVersion = [version]'3.6.0'
[version]$appDeployMainScriptMinimumConfigVersion = [version]'3.6.0'
[string]$appDeployMainScriptDate = '12/18/2014'
[hashtable]$appDeployMainScriptParameters = $PSBoundParameters

## Variables: Datetime and Culture
[string]$currentTime = (Get-Date -UFormat '%T').ToString()
[string]$currentDate = (Get-Date -UFormat '%d-%m-%Y').ToString()
[timespan]$currentTimeZoneBias = [System.TimeZone]::CurrentTimeZone.GetUtcOffset([datetime]::Now)
[Globalization.CultureInfo]$culture = Get-Culture
[string]$currentLanguage = $culture.TwoLetterISOLanguageName.ToUpper()

## Variables: Environment Variables
[psobject]$envHost = $Host
[string]$envAllUsersProfile = $env:ALLUSERSPROFILE
[string]$envAppData = $env:APPDATA
[string]$envArchitecture = $env:PROCESSOR_ARCHITECTURE
[string]$envCommonProgramFiles = $env:CommonProgramFiles
[string]$envCommonProgramFilesX86 = ${env:CommonProgramFiles(x86)}
[string]$envComputerName = $env:COMPUTERNAME | Where-Object { $_ } | ForEach-Object { $_.ToUpper() }
[string]$envComputerNameFQDN = ([System.Net.Dns]::GetHostEntry('')).HostName
[string]$envHomeDrive = $env:HOMEDRIVE
[string]$envHomePath = $env:HOMEPATH
[string]$envHomeShare = $env:HOMESHARE
[string]$envLocalAppData = $env:LOCALAPPDATA
[string]$envProgramFiles = $env:PROGRAMFILES
[string]$envProgramFilesX86 = ${env:ProgramFiles(x86)}
[string]$envProgramData = $env:PROGRAMDATA
[string]$envPublic = $env:PUBLIC
[string]$envSystemDrive = $env:SYSTEMDRIVE
[string]$envSystemRoot = $env:SYSTEMROOT
[string]$envTemp = $env:TEMP
[string]$envUserName = $env:USERNAME
[string]$envUserProfile = $env:USERPROFILE
[string]$envWinDir = $env:WINDIR
#  Handle X86 environment variables so they are never empty
If (-not $envCommonProgramFilesX86) { [string]$envCommonProgramFilesX86 = $env:CommonProgramFiles }
If (-not $envProgramFilesX86) { [string]$envProgramFilesX86 = $env:PROGRAMFILES }

## Variables: Domain Membership
[boolean]$IsMachinePartOfDomain = (Get-WmiObject Win32_ComputerSystem -ErrorAction 'SilentlyContinue').PartOfDomain
[string]$envMachineWorkgroup = ''
[string]$envMachineADDomain = ''
[string]$envLogonServer = ''
[string]$MachineDomainController = ''
If ($IsMachinePartOfDomain) {
	[string]$envMachineADDomain = (Get-WmiObject -Class Win32_ComputerSystem -ErrorAction 'SilentlyContinue').Domain | Where-Object { $_ } | ForEach-Object { $_.ToLower() }
	Try {
		[string]$envLogonServer = $env:LOGONSERVER | Where-Object { (($_) -and (-not $_.Contains('\\MicrosoftAccount'))) } | ForEach-Object { $_.TrimStart('\') } | ForEach-Object { ([System.Net.Dns]::GetHostEntry($_)).HostName }
		[string]$MachineDomainController = [System.DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain().FindDomainController().Name
	}
	Catch { }
}
Else {
	[string]$envMachineWorkgroup = (Get-WmiObject -Class Win32_ComputerSystem -ErrorAction 'SilentlyContinue').Domain | Where-Object { $_ } | ForEach-Object { $_.ToUpper() }
}
[string]$envMachineDNSDomain = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().DomainName | Where-Object { $_ } | ForEach-Object { $_.ToLower() }
[string]$envUserDNSDomain = $env:USERDNSDOMAIN | Where-Object { $_ } | ForEach-Object { $_.ToLower() }
[string]$envUserDomain = $env:USERDOMAIN | Where-Object { $_ } | ForEach-Object { $_.ToUpper() }

## Variables: Operating System
[psobject]$envOS = Get-WmiObject -Class Win32_OperatingSystem -ErrorAction 'SilentlyContinue'
[string]$envOSName = $envOS.Caption.Trim()
[string]$envOSServicePack = $envOS.CSDVersion
[version]$envOSVersion = [System.Environment]::OSVersion.Version
[string]$envOSVersionMajor = $envOSVersion.Major
[string]$envOSVersionMinor = $envOSVersion.Minor
[string]$envOSVersionBuild = $envOSVersion.Build
[string]$envOSVersionRevision = $envOSVersion.Revision
[string]$envOSVersion = $envOSVersion.ToString()
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
[boolean]$Is64Bit = [boolean]((Get-WmiObject -Class Win32_Processor | Where-Object { $_.DeviceID -eq 'CPU0' } | Select-Object -ExpandProperty AddressWidth) -eq '64')
If ($Is64Bit) { [string]$envOSArchitecture = '64-bit' } Else { [string]$envOSArchitecture = '32-bit' }

## Variables: Current Process Architecture
[boolean]$Is64BitProcess = [boolean]([System.IntPtr]::Size -eq 8)
If ($Is64BitProcess) { [string]$psArchitecture = 'x64' } Else { [string]$psArchitecture = 'x86' }

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
[System.Security.Principal.WindowsIdentity]$CurrentProcessToken = [System.Security.Principal.WindowsIdentity]::GetCurrent()
[System.Security.Principal.SecurityIdentifier]$CurrentProcessSID = $CurrentProcessToken.User
[string]$ProcessNTAccount = $CurrentProcessToken.Name
[string]$ProcessNTAccountSID = $CurrentProcessSID.Value
[boolean]$IsAdmin = [boolean]($CurrentProcessToken.Groups -contains [System.Security.Principal.SecurityIdentifier]'S-1-5-32-544')
[boolean]$IsLocalSystemAccount = $CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalSystemSid')
[boolean]$IsLocalServiceAccount = $CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalServiceSid')
[boolean]$IsNetworkServiceAccount = $CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]'NetworkServiceSid')
[boolean]$IsServiceAccount = [boolean]($CurrentProcessToken.Groups -contains [System.Security.Principal.SecurityIdentifier]'S-1-5-6')
[boolean]$IsProcessUserInteractive = [System.Environment]::UserInteractive
[string]$LocalSystemNTAccount = (New-Object -TypeName System.Security.Principal.SecurityIdentifier -ArgumentList ([Security.Principal.WellKnownSidType]::'LocalSystemSid', $null)).Translate([System.Security.Principal.NTAccount]).Value
#  Check if script is running in session zero
If ($IsLocalSystemAccount -or $IsLocalServiceAccount -or $IsNetworkServiceAccount -or $IsServiceAccount) { $SessionZero = $true } Else { $SessionZero = $false }

## Variables: DPI Scale (property only exists if DPI scaling has been changed on the system at least once)
[int32]$dpiPixels = Get-ItemProperty -Path 'HKLM:SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontDPI' -ErrorAction 'SilentlyContinue' | Select-Object -ExpandProperty LogPixels -ErrorAction 'SilentlyContinue'
Switch ($dpiPixels) {
	96 { [int32]$dpiScale = 100 }
	120 { [int32]$dpiScale = 125 }
	144 { [int32]$dpiScale = 150 }
	192 { [int32]$dpiScale = 200 }
	Default { [int32]$dpiScale = 100 }
}

## Variables: Script Name and Script Paths
[string]$scriptPath = $MyInvocation.MyCommand.Definition
[string]$scriptName = [System.IO.Path]::GetFileNameWithoutExtension($scriptPath)
[string]$scriptFileName = Split-Path -Path $scriptPath -Leaf
[string]$scriptRoot = Split-Path -Path $scriptPath -Parent
[string]$invokingScript = (Get-Variable -Name MyInvocation).Value.ScriptName
#  Get the invoking script directory
If ($invokingScript) {
	#  If this script was invoked by another script
	[string]$scriptParentPath = Split-Path -Path $invokingScript -Parent
}
Else {
	#  If this script was not invoked by another script, fall back to the directory one level above this script
	[string]$scriptParentPath = (Get-Item -Path $scriptRoot).Parent.FullName
}

## Variables: App Deploy Script Dependency Files
[string]$appDeployLogoIcon = Join-Path -Path $scriptRoot -ChildPath 'AppDeployToolkitLogo.ico'
[string]$appDeployLogoBanner = Join-Path -Path $scriptRoot -ChildPath 'AppDeployToolkitBanner.png'
[string]$appDeployConfigFile = Join-Path -Path $scriptRoot -ChildPath 'AppDeployToolkitConfig.xml'
#  App Deploy Optional Extensions File
[string]$appDeployToolkitDotSourceExtensions = 'AppDeployToolkitExtensions.ps1'
#  Check that dependency files are present
If (-not (Test-Path -Path $AppDeployLogoIcon -PathType Leaf)) { Throw 'App Deploy logo icon file not found.' }
If (-not (Test-Path -Path $AppDeployLogoBanner -PathType Leaf)) { Throw 'App Deploy logo banner file not found.' }
If (-not (Test-Path -Path $AppDeployConfigFile -PathType Leaf)) { Throw 'App Deploy XML configuration file not found.' }

## Import variables from XML configuration file
[xml]$xmlConfigFile = Get-Content -Path $AppDeployConfigFile
$xmlConfig = $xmlConfigFile.AppDeployToolkit_Config
#  Get Config File Details
$configConfigDetails = $xmlConfig.Config_File
[string]$configConfigVersion = [version]$configConfigDetails.Config_Version
[string]$configConfigDate = $configConfigDetails.Config_Date
#  Get Toolkit Options
$xmlToolkitOptions = $xmlConfig.Toolkit_Options
[boolean]$configToolkitRequireAdmin = [boolean]::Parse($xmlToolkitOptions.Toolkit_RequireAdmin)
[boolean]$configToolkitAllowSystemInteraction = [boolean]::Parse($xmlToolkitOptions.Toolkit_AllowSystemInteraction)
[boolean]$configToolkitAllowSystemInteractionFallback = [boolean]::Parse($xmlToolkitOptions.Toolkit_AllowSystemInteractionFallback)
[boolean]$configToolkitAllowSystemInteractionForNonConsoleUser = [boolean]::Parse($xmlToolkitOptions.Toolkit_AllowSystemInteractionForNonConsoleUser)
[string]$configToolkitTempPath = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_TempPath)
[string]$configToolkitRegPath = $xmlToolkitOptions.Toolkit_RegPath
[string]$configToolkitLogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_LogPath)
[boolean]$configToolkitCompressLogs = [boolean]::Parse($xmlToolkitOptions.Toolkit_CompressLogs)
[string]$configToolkitLogStyle = $xmlToolkitOptions.Toolkit_LogStyle
[double]$configToolkitLogMaxSize = $xmlToolkitOptions.Toolkit_LogMaxSize
[boolean]$configToolkitLogWriteToHost = [boolean]::Parse($xmlToolkitOptions.Toolkit_LogWriteToHost)
[boolean]$configToolkitLogDebugMessage = [boolean]::Parse($xmlToolkitOptions.Toolkit_LogDebugMessage)
#  Get MSI Options
$xmlConfigMSIOptions = $xmlConfig.MSI_Options
[string]$configMSILoggingOptions = $xmlConfigMSIOptions.MSI_LoggingOptions
[string]$configMSIInstallParams = $xmlConfigMSIOptions.MSI_InstallParams
[string]$configMSISilentParams = $xmlConfigMSIOptions.MSI_SilentParams
[string]$configMSIUninstallParams = $xmlConfigMSIOptions.MSI_UninstallParams
[string]$configMSILogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_LogPath)
[int32]$configMSIMutexWaitTime = $xmlConfigMSIOptions.MSI_MutexWaitTime
#  Get UI Options
$xmlConfigUIOptions = $xmlConfig.UI_Options
[boolean]$configShowBalloonNotifications = [boolean]::Parse($xmlConfigUIOptions.ShowBalloonNotifications)
[int32]$configInstallationUITimeout = $xmlConfigUIOptions.InstallationUI_Timeout
[int32]$configInstallationUIExitCode = $xmlConfigUIOptions.InstallationUI_ExitCode
[int32]$configInstallationDeferExitCode = $xmlConfigUIOptions.InstallationDefer_ExitCode
[int32]$configInstallationPersistInterval = $xmlConfigUIOptions.InstallationPrompt_PersistInterval
[int32]$configInstallationRestartPersistInterval = $xmlConfigUIOptions.InstallationRestartPrompt_PersistInterval
#  Get Message UI Language Options (default for English if no localization found)
[string]$xmlUIMessageLanguage = "UI_Messages_$currentLanguage"
If (-not ($xmlConfig.$xmlUIMessageLanguage)) { [string]$xmlUIMessageLanguage = 'UI_Messages_EN' }
$xmlUIMessages = $xmlConfig.$xmlUIMessageLanguage
[string]$configDiskSpaceMessage = $xmlUIMessages.DiskSpace_Message
[string]$configBalloonTextStart = $xmlUIMessages.BalloonText_Start
[string]$configBalloonTextComplete = $xmlUIMessages.BalloonText_Complete
[string]$configBalloonTextRestartRequired = $xmlUIMessages.BalloonText_RestartRequired
[string]$configBalloonTextFastRetry = $xmlUIMessages.BalloonText_FastRetry
[string]$configBalloonTextError = $xmlUIMessages.BalloonText_Error
[string]$configProgressMessageInstall = $xmlUIMessages.Progress_MessageInstall
[string]$configProgressMessageUninstall = $xmlUIMessages.Progress_MessageUninstall
[string]$configClosePromptMessage = $xmlUIMessages.ClosePrompt_Message
[string]$configClosePromptButtonClose = $xmlUIMessages.ClosePrompt_ButtonClose
[string]$configClosePromptButtonDefer = $xmlUIMessages.ClosePrompt_ButtonDefer
[string]$configClosePromptButtonContinue = $xmlUIMessages.ClosePrompt_ButtonContinue
[string]$configClosePromptCountdownMessage = $xmlUIMessages.ClosePrompt_CountdownMessage
[string]$configDeferPromptWelcomeMessage = $xmlUIMessages.DeferPrompt_WelcomeMessage
[string]$configDeferPromptExpiryMessage = $xmlUIMessages.DeferPrompt_ExpiryMessage
[string]$configDeferPromptWarningMessage = $xmlUIMessages.DeferPrompt_WarningMessage
[string]$configDeferPromptRemainingDeferrals = $xmlUIMessages.DeferPrompt_RemainingDeferrals
[string]$configDeferPromptDeadline = $xmlUIMessages.DeferPrompt_Deadline
[string]$configBlockExecutionMessage = $xmlUIMessages.BlockExecution_Message
[string]$configDeploymentTypeInstall = $xmlUIMessages.DeploymentType_Install
[string]$configDeploymentTypeUnInstall = $xmlUIMessages.DeploymentType_UnInstall
[string]$configRestartPromptTitle = $xmlUIMessages.RestartPrompt_Title
[string]$configRestartPromptMessage = $xmlUIMessages.RestartPrompt_Message
[string]$configRestartPromptMessageTime = $xmlUIMessages.RestartPrompt_MessageTime
[string]$configRestartPromptMessageRestart = $xmlUIMessages.RestartPrompt_MessageRestart
[string]$configRestartPromptTimeRemaining = $xmlUIMessages.RestartPrompt_TimeRemaining
[string]$configRestartPromptButtonRestartLater = $xmlUIMessages.RestartPrompt_ButtonRestartLater
[string]$configRestartPromptButtonRestartNow = $xmlUIMessages.RestartPrompt_ButtonRestartNow

## Variables: Directories
[string]$dirFiles = Join-Path -Path $scriptParentPath -ChildPath 'Files'
[string]$dirSupportFiles = Join-Path -Path $scriptParentPath -ChildPath 'SupportFiles'
[string]$dirAppDeployTemp = Join-Path -Path $configToolkitTempPath -ChildPath $appDeployToolkitName

## Set up sample variables if Dot Sourcing the script, app details have not been specified, or InstallTitle not passed as parameter to the script
If (-not $appVendor) { [string]$appVendor = 'PS' }
If (-not $appName) { [string]$appName = $appDeployMainScriptFriendlyName }
If (-not $appVersion) { [string]$appVersion = $appDeployMainScriptVersion }
If (-not $appLang) { [string]$appLang = $currentLanguage }
If (-not $appRevision) { [string]$appRevision = '01' }
If (-not $appArch) { [string]$appArch = '' }
[string]$installTitle = "$appVendor $appName $appVersion"

## Sanitize the application details, as they can cause issues in the script
[char[]]$invalidFileNameChars = [System.IO.Path]::GetInvalidFileNamechars()
[string]$appVendor = $appVendor -replace "[$invalidFileNameChars]",'' -replace ' ',''
[string]$appName = $appName -replace "[$invalidFileNameChars]",'' -replace ' ',''
[string]$appVersion = $appVersion -replace "[$invalidFileNameChars]",'' -replace ' ',''
[string]$appArch = $appArch -replace "[$invalidFileNameChars]",'' -replace ' ',''
[string]$appLang = $appLang -replace "[$invalidFileNameChars]",'' -replace ' ',''
[string]$appRevision = $appRevision -replace "[$invalidFileNameChars]",'' -replace ' ',''

## Build the Installation Name
If ($appArch) {
	[string]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appArch + '_' + $appLang + '_' + $appRevision
}
Else {
	[string]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appLang + '_' + $appRevision
}
[string]$installName = $installName.Trim('_') -replace '[_]+','_'

## Set the deployment type to "Install" if it has not been specified
If (-not $deploymentType) { [string]$deploymentType = 'Install' }

## Variables: Executables
[string]$exeWusa = 'wusa.exe' # Installs Standalone Windows Updates
[string]$exeMsiexec = 'msiexec.exe' # Installs MSI Installers
[string]$exeSchTasks = "$envWinDir\System32\schtasks.exe" # Manages Scheduled Tasks

## Variables: RegEx Patterns
[string]$MSIProductCodeRegExPattern = '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$'

## Variables: Registry Keys
#  Registry keys for native and WOW64 applications
[string[]]$regKeyApplications = 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall','HKLM:SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
If ($is64Bit) {
	[string]$regKeyLotusNotes = 'HKLM:SOFTWARE\Wow6432Node\Lotus\Notes'
}
Else {
	[string]$regKeyLotusNotes = 'HKLM:SOFTWARE\Lotus\Notes'
}
[string]$regKeyAppExecution = 'HKLM:SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options'
[string]$regKeyDeferHistory = "$configToolkitRegPath\$appDeployToolkitName\DeferHistory\$installName"

## COM Objects: Initialize
[__comobject]$Shell = New-Object -ComObject WScript.Shell -ErrorAction 'SilentlyContinue'
[__comobject]$ShellApp = New-Object -ComObject Shell.Application -ErrorAction 'SilentlyContinue'

## Variables: Reset/Remove Variables
[boolean]$msiRebootDetected = $false
[boolean]$BlockExecution = $false
[boolean]$installationStarted = $false
[boolean]$runningTaskSequence = $false
If (Test-Path -Path 'variable:welcomeTimer') { Remove-Variable -Name welcomeTimer -Scope Script}
#  Reset the deferral history
If (Test-Path -Path 'variable:deferHistory') { Remove-Variable -Name deferHistory }
If (Test-Path -Path 'variable:deferTimes') { Remove-Variable -Name deferTimes }
If (Test-Path -Path 'variable:deferDays') { Remove-Variable -Name deferDays }

## Variables: Log Files
[string]$logName = $installName + '_' + $appDeployToolkitName + '_' + $deploymentType + '.log'
[string]$logTempFolder = Join-Path -Path $envTemp -ChildPath $installName
If ($configToolkitCompressLogs) {
	## If option to compress logs is selected, then log will be created in temp log folder and then copied to actual log folder after being zipped.
	#  Set log file directory to temp log folder
	[string]$logDirectory = $logTempFolder
	#  The path to the zipped log file in the actual logs folder defined in App Deploy XML config file
	[string]$zipFileDate = (Get-Date -Format 'yyyy-MM-dd-hh-mm-ss').ToString()
	[string]$zipFileName = Join-Path -Path $configToolkitLogDir -ChildPath ($installName + '_' + $deploymentType + '_' + $zipFileDate + '.zip')
	
	#  If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues
	If (Test-Path -Path $logTempFolder -PathType Container -ErrorAction 'SilentlyContinue') {
		Remove-Item -Path $logTempFolder -Recurse -Force -ErrorAction 'SilentlyContinue' | Out-Null
	}
}
Else {
	## Path to log directory defined in AppDeploy XML config file
	[string]$logDirectory = $configToolkitLogDir
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
.EXAMPLE
	Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
.EXAMPLE
	Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$CmdletName,
		[Parameter(Mandatory=$true,ParameterSetName='Header')]
		[AllowEmptyCollection()]
		[hashtable]$CmdletBoundParameters,
		[Parameter(Mandatory=$true,ParameterSetName='Header')]
		[switch]$Header,
		[Parameter(Mandatory=$true,ParameterSetName='Footer')]
		[switch]$Footer
	)
	
	If ($Header) {
		Write-Log -Message 'Function Start' -Source ${CmdletName} -DebugMessage
		
		## Get the parameters that the calling function was invoked with
		[string]$CmdletBoundParameters = $CmdletBoundParameters | Format-Table -Property @{ Label = 'Parameter'; Expression = { "[-$($_.Key)]" } }, @{ Label = 'Value'; Expression = { $_.Value }; Alignment = 'Left' } -AutoSize -Wrap | Out-String
		If ($CmdletBoundParameters) {
			Write-Log -Message "Function invoked with bound parameter(s): `n$CmdletBoundParameters" -Source ${CmdletName} -DebugMessage
		}
		Else {
			Write-Log -Message 'Function invoked without any bound parameters' -Source ${CmdletName} -DebugMessage
		}
	}
	ElseIf ($Footer) {
		Write-Log -Message 'Function End' -Source ${CmdletName} -DebugMessage
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
	Suppress writing log message to console on failure to write message to log file.
.PARAMETER PassThru
	Return the message that was passed to the function
.PARAMETER DebugMessage
	Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.
.PARAMETER LogDebugMessage
	Debug messages only get logged if this parameter is set to $true in the config XML file.
.EXAMPLE
	Write-Log -Message "Installing patch MS15-031" -Source 'Add-Patch' -LogType 'CMTrace'
.EXAMPLE
	Write-Log -Message "Script is running on Windows 8" -Source 'Test-ValidOS' -LogType 'Legacy'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[AllowEmptyCollection()]
		[string[]]$Message,
		[Parameter(Mandatory=$false,Position=1)]
		[ValidateRange(1,3)]
		[int16]$Severity = 1,
		[Parameter(Mandatory=$false,Position=2)]
		[ValidateNotNull()]
		[string]$Source = '',
		[Parameter(Mandatory=$false,Position=3)]
		[ValidateNotNullorEmpty()]
		[string]$ScriptSection = $script:installPhase,
		[Parameter(Mandatory=$false,Position=4)]
		[ValidateSet('CMTrace','Legacy')]
		[string]$LogType = $configToolkitLogStyle,
		[Parameter(Mandatory=$false,Position=5)]
		[ValidateNotNullorEmpty()]
		[string]$LogFileDirectory = $logDirectory,
		[Parameter(Mandatory=$false,Position=6)]
		[ValidateNotNullorEmpty()]
		[string]$LogFileName = $logName,
		[Parameter(Mandatory=$false,Position=7)]
		[ValidateNotNullorEmpty()]
		[decimal]$MaxLogFileSizeMB = $configToolkitLogMaxSize,
		[Parameter(Mandatory=$false,Position=8)]
		[ValidateNotNullorEmpty()]
		[boolean]$WriteHost = $configToolkitLogWriteToHost,
		[Parameter(Mandatory=$false,Position=9)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true,
		[Parameter(Mandatory=$false,Position=10)]
		[switch]$PassThru = $false,
		[Parameter(Mandatory=$false,Position=11)]
		[switch]$DebugMessage = $false,
		[Parameter(Mandatory=$false,Position=12)]
		[boolean]$LogDebugMessage = $configToolkitLogDebugMessage,
		[Parameter(Mandatory=$false,Position=13)]
		[switch]$DisableOnRelaunchToolkitAsUser = $false
	)
	
	Begin {
		## Get the name of this function
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

		## Initialize Variables
		[string]$LogTime = (Get-Date -Format HH:mm:ss.fff).ToString()
		[string]$LogDate = (Get-Date -Format MM-dd-yyyy).ToString()
		If (-not (Test-Path -Path 'variable:LogTimeZoneBias')) { [int32]$script:LogTimeZoneBias = [System.TimeZone]::CurrentTimeZone.GetUtcOffset([datetime]::Now).TotalMinutes }
		[string]$LogTimePlusBias = $LogTime + $script:LogTimeZoneBias
		[boolean]$ExitLoggingFunction = $false

		## Exit function if logging was disabled because toolkit was dot sourced again so that a command could be executed in the user context
		If ($DisableOnRelaunchToolkitAsUser -and $RelaunchToolkitAsUser) { [boolean]$ExitLoggingFunction = $true; Return }

		## Exit function if it is a debug message and logging debug messages is not enabled in the config XML file
		If (($DebugMessage) -and (-not $LogDebugMessage)) { [boolean]$ExitLoggingFunction = $true; Return }
		
		## Create the directory where the log file will be saved
		If (-not (Test-Path -Path $LogFileDirectory -PathType Container)) {
			Try {
				New-Item -Path $LogFileDirectory -Type 'Directory' -Force -ErrorAction 'Stop' | Out-Null
			}
			Catch {
				[boolean]$ExitLoggingFunction = $true
				#  If error creating directory, write message to console
				If (-not $ContinueOnError) {
					Write-Host "[$LogDate $LogTime] [${CmdletName}] $ScriptSection :: Failed to create the log directory [$LogFileDirectory]. `n$(Resolve-Error)" -ForegroundColor 'Red'
				}
				Return
			}
		}

		## Get the file name of the source script
		If ($script:MyInvocation.Value.ScriptName) { [string]$ScriptSource = Split-Path -Path $script:MyInvocation.Value.ScriptName -Leaf } Else { [string]$ScriptSource = Split-Path -Path $script:MyInvocation.MyCommand.Definition -Leaf }
		
		## Check if the script section is defined
		[boolean]$ScriptSectionDefined = [boolean](-not [string]::IsNullOrEmpty($ScriptSection))
		
		## Initialize $DisableLogging variable to avoid error if 'Set-StrictMode' is set
		If (-not (Test-Path -Path 'variable:DisableLogging')) { $DisableLogging = $false }
		
		## Create script block for generating CMTrace.exe compatible log entry
		[scriptblock]$CMTraceLogString = {
			Param (
				[string]$lMessage,
				[string]$lSource,
				[int16]$lSeverity
			)
			"<![LOG[$lMessage]LOG]!>" + "<time=`"$LogTimePlusBias`" " + "date=`"$LogDate`" " + "component=`"$lSource`" " + "context=`"$([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)`" " + "type=`"$lSeverity`" " + "thread=`"$PID`" " + "file=`"$ScriptSource`">"
		}
		
		## Create script block for writing log entry to the console
		[scriptblock]$WriteLogLineToHost = {
			Param (
				[string]$lTextLogLine,
				[int16]$lSeverity
			)
			If ($WriteHost) {
				#  Only output using color options if running in a host which supports colors.
				If ($Host.UI.RawUI.ForegroundColor) {
					Switch ($lSeverity) {
						3 { Write-Host $lTextLogLine -ForegroundColor 'Red' -BackgroundColor 'Black' }
						2 { Write-Host $lTextLogLine -ForegroundColor 'Yellow' -BackgroundColor 'Black' }
						1 { Write-Host $lTextLogLine }
					}
				}
				#  If executing "powershell.exe -File <filename>.ps1 > log.txt", then all the Write-Host calls are converted to Write-Output calls so that they are included in the text log.
				Else {
					Write-Output $lTextLogLine
				}
			}
		}
		
		#  Assemble the fully qualified path to the log file
		[string]$LogFilePath = Join-Path -Path $LogFileDirectory -ChildPath $LogFileName
	}
	Process {
		## Exit function if logging is disabled or if the log directory was not successfully created in 'Begin' block.
		If ($ExitLoggingFunction) { Return }
		
		ForEach ($Msg in $Message) {
			## If the message is not $null or empty, create the log entry for the different logging methods
			[string]$CMTraceMsg = ''
			[string]$ConsoleLogLine = ''
			[string]$LegacyTextLogLine = ''
			If ($Msg) {
				#  Create the CMTrace log message
				If ($ScriptSectionDefined) { [string]$CMTraceMsg = "[$ScriptSection] :: $Msg" }
				
				#  Create a Console and Legacy "text" log entry
				[string]$LegacyMsg = "[$LogDate $LogTime]"
				If ($ScriptSectionDefined) { [string]$LegacyMsg += " [$ScriptSection]" }
				If ($Source) {
					[string]$ConsoleLogLine = "$LegacyMsg [$Source] :: $Msg"
					Switch ($Severity) {
						3 { [string]$LegacyTextLogLine = "$LegacyMsg [$Source] [Error] :: $Msg" }
						2 { [string]$LegacyTextLogLine = "$LegacyMsg [$Source] [Warning] :: $Msg" }
						1 { [string]$LegacyTextLogLine = "$LegacyMsg [$Source] [Info] :: $Msg" }
					}
				}
				Else {
					[string]$ConsoleLogLine = "$LegacyMsg :: $Msg"
					Switch ($Severity) {
						3 { [string]$LegacyTextLogLine = "$LegacyMsg [Error] :: $Msg" }
						2 { [string]$LegacyTextLogLine = "$LegacyMsg [Warning] :: $Msg" }
						1 { [string]$LegacyTextLogLine = "$LegacyMsg [Info] :: $Msg" }
					}
				}
			}
			
			## Execute script block to create the CMTrace.exe compatible log entry
			[string]$CMTraceLogLine = & $CMTraceLogString -lMessage $CMTraceMsg -lSource $Source -lSeverity $Severity
			
			## Choose which log type to write to file
			If ($LogType -ieq 'CMTrace') {
				[string]$LogLine = $CMTraceLogLine
			}
			Else {
				[string]$LogLine = $LegacyTextLogLine
			}
			
			## Write the log entry to the log file if logging is not currently disabled
			If (-not $DisableLogging) {
				Try {
					$LogLine | Out-File -FilePath $LogFilePath -Append -NoClobber -Force -Encoding 'UTF8' -ErrorAction 'Stop'
				}
				Catch {
					If (-not $ContinueOnError) {
						Write-Host "[$LogDate $LogTime] [$ScriptSection] [${CmdletName}] :: Failed to write message [$Msg] to the log file [$LogFilePath]. `n$(Resolve-Error)" -ForegroundColor 'Red'
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
			If (-not $ExitLoggingFunction) {
				[System.IO.FileInfo]$LogFile = Get-ChildItem -Path $LogFilePath -ErrorAction 'Stop'
				[decimal]$LogFileSizeMB = $LogFile.Length/1MB
				If (($LogFileSizeMB -gt $MaxLogFileSizeMB) -and ($MaxLogFileSizeMB -gt 0)) {
					## Change the file extension to "lo_"
					[string]$ArchivedOutLogFile = [System.IO.Path]::ChangeExtension($LogFilePath, 'lo_')
					[hashtable]$ArchiveLogParams = @{ ScriptSection = $ScriptSection; Source = ${CmdletName}; Severity = 2; LogFileDirectory = $LogFileDirectory; LogFileName = $LogFilePath; LogType = $LogType; MaxLogFileSizeMB = 0; WriteHost = $WriteHost; ContinueOnError = $ContinueOnError; PassThru = $false }
					
					## Log message about archiving the log file
					$ArchiveLogMessage = "Maximum log file size [$MaxLogFileSizeMB MB] reached. Rename log file to [$ArchivedOutLogFile]."
					Write-Log -Message $ArchiveLogMessage @ArchiveLogParams
					
					## Archive existing log file from <filename>.log to <filename>.lo_. Overwrites any existing <filename>.lo_ file. This is the same method SCCM uses for log files.
					Move-Item -Path $LogFilePath -Destination $ArchivedOutLogFile -Force -ErrorAction 'Stop'
					
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
			If ($PassThru) { Write-Output $Message }
		}
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
.EXAMPLE
	Exit-Script -ExitCode 0
.EXAMPLE
	Exit-Script -ExitCode 1618
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$ExitCode = 0
	)
	
	## Get the name of this function
	[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
	
	## Stop the Close Program Dialog if running
	If ($formCloseApps) { $formCloseApps.Close }
	
	
	## Close the Installation Progress Dialog if running
	If (Test-Path -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -PathType 'Leaf') {
		[string]$StatusMessage = '_CloseRunspace'
		$StatusMessage | Export-Clixml -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -Force
	}
	Start-Sleep -Seconds 5
	Close-InstallationProgress
	
	## If block execution variable is true, call the function to unblock execution
	If (($BlockExecution) -and (-not $RelaunchToolkitAsUser)) { Unblock-AppExecution }
	
	## If Terminal Server mode was set, turn it off
	If (($terminalServerMode) -and (-not $RelaunchToolkitAsUser)) { Disable-TerminalServerInstallMode }
	
	## Determine action based on exit code
	Switch ($exitCode) {
		$configInstallationUIExitCode { $installSuccess = $false }
		$configInstallationDeferExitCode { $installSuccess = $false }
		3010 { $installSuccess = $true }
		1641 { $installSuccess = $true }
		0 { $installSuccess = $true }
		Default { $installSuccess = $false }
	}
	
	## Determine if baloon notification should be shown
	If ($deployModeSilent) { [boolean]$configShowBalloonNotifications = $false }
	
	If ($installSuccess) {
		If (Test-Path -Path $regKeyDeferHistory -ErrorAction 'SilentlyContinue') {
			Write-Log -Message 'Remove deferral history...' -Source ${CmdletName}
			Remove-RegistryKey -Key $regKeyDeferHistory
		}
		
		[string]$balloonText = "$deploymentTypeName $configBalloonTextComplete"
		## Handle reboot prompts on successful script completion
		If (($msiRebootDetected) -and ($AllowRebootPassThru)) {
			Write-Log -Message 'A restart has been flagged as required.' -Source ${CmdletName}
			[string]$balloonText = "$deploymentTypeName $configBalloonTextRestartRequired"
			[int32]$exitCode = 3010
		}
		Else {
			[int32]$exitCode = 0
		}
		
		Write-Log -Message "$installName $deploymentTypeName completed with exit code [$exitcode]." -Source ${CmdletName}
		If ($configShowBalloonNotifications) { Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText $balloonText }
	}
	ElseIf (-not $installSuccess) {
		Write-Log -Message "$installName $deploymentTypeName completed with exit code [$exitcode]." -Source ${CmdletName}
		If (($exitCode -eq $configInstallationUIExitCode) -or ($exitCode -eq $configInstallationDeferExitCode)) {
			[string]$balloonText = "$deploymentTypeName $configBalloonTextFastRetry"
			If ($configShowBalloonNotifications) { Show-BalloonTip -BalloonTipIcon 'Warning' -BalloonTipText $balloonText }
		}
		Else {
			[string]$balloonText = "$deploymentTypeName $configBalloonTextError"
			If ($configShowBalloonNotifications) { Show-BalloonTip -BalloonTipIcon 'Error' -BalloonTipText $balloonText }
		}
	}
	
	[string]$LogDash = '-' * 79
	Write-Log -Message $LogDash -Source ${CmdletName}
	
	## Compress the log files and remove the temporary folder
	If (($configToolkitCompressLogs) -and (-not $RelaunchToolkitAsUser)) {
		Try {
			#  Add the file header for zip files to a file and create a 0 byte .zip file
			Set-Content -Path $zipFileName -Value ('PK' + [char]5 + [char]6 + ("$([char]0)" * 18)) -ErrorAction 'Stop'
			
			$zipFile = $shellApp.NameSpace($zipFileName)
			ForEach ($file in (Get-ChildItem -Path $logTempFolder -ErrorAction 'Stop')) {
				Write-Log -Message "Compress log file [$($file.Name)] to [$zipFileName]..." -Source ${CmdletName}
				$zipFile.CopyHere($file.FullName)
				Start-Sleep -Milliseconds 500
			}
			
			If (Test-Path -Path $logTempFolder -PathType Container -ErrorAction 'Stop') {
				Remove-Item -Path $logTempFolder -Recurse -Force -ErrorAction 'Stop' | Out-Null
			}
		}
		Catch {
			Write-Log -Message "Failed to compress the log file(s). `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	
	## Exit the script, returning the exit code to SCCM
	Exit $exitCode
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
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false,Position=0,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[AllowEmptyCollection()]
		[array]$ErrorRecord,
		[Parameter(Mandatory=$false,Position=1)]
		[ValidateNotNullorEmpty()]
		[string[]]$Property = ('Message','InnerException','FullyQualifiedErrorId','ScriptStackTrace','PositionMessage'),
		[Parameter(Mandatory=$false,Position=2)]
		[switch]$GetErrorRecord = $true,
		[Parameter(Mandatory=$false,Position=3)]
		[switch]$GetErrorInvocation = $true,
		[Parameter(Mandatory=$false,Position=4)]
		[switch]$GetErrorException = $true,
		[Parameter(Mandatory=$false,Position=5)]
		[switch]$GetErrorInnerException = $true
	)
	
	Begin {
		## If function was called without specifying an error record, then choose the latest error that occurred
		If (-not $ErrorRecord) {
			If ($global:Error.Count -eq 0) {
				#Write-Warning -Message "The `$Error collection is empty"
				Return
			}
			Else {
				[array]$ErrorRecord = $global:Error[0]
			}
		}
		
		## Allows selecting and filtering the properties on the error object if they exist
		[scriptblock]$SelectProperty = {
			Param (
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				$InputObject,
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				[string[]]$Property
			)
			
			[string[]]$ObjectProperty = $InputObject | Get-Member -MemberType *Property | Select-Object -ExpandProperty Name
			ForEach ($Prop in $Property) {
				If ($Prop -eq '*') {
					[string[]]$PropertySelection = $ObjectProperty
					Break
				}
				ElseIf ($ObjectProperty -contains $Prop) {
					[string[]]$PropertySelection += $Prop
				}
			}
			Write-Output $PropertySelection
		}
		
		#  Initialize variables to avoid error if 'Set-StrictMode' is set
		$LogErrorRecordMsg = $null
		$LogErrorInvocationMsg = $null
		$LogErrorExceptionMsg = $null
		$LogErrorMessageTmp = $null
		$LogInnerMessage = $null
	}
	Process {
		If (-not $ErrorRecord) { Return }
		ForEach ($ErrRecord in $ErrorRecord) {
			## Capture Error Record
			If ($GetErrorRecord) {
				[string[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord -Property $Property
				$LogErrorRecordMsg = $ErrRecord | Select-Object -Property $SelectedProperties
			}
			
			## Error Invocation Information
			If ($GetErrorInvocation) {
				If ($ErrRecord.InvocationInfo) {
					[string[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.InvocationInfo -Property $Property
					$LogErrorInvocationMsg = $ErrRecord.InvocationInfo | Select-Object -Property $SelectedProperties
				}
			}
			
			## Capture Error Exception
			If ($GetErrorException) {
				If ($ErrRecord.Exception) {
					[string[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.Exception -Property $Property
					$LogErrorExceptionMsg = $ErrRecord.Exception | Select-Object -Property $SelectedProperties
				}
			}
			
			## Display properties in the correct order
			If ($Property -eq '*') {
				#  If all properties were chosen for display, then arrange them in the order the error object displays them by default.
				If ($LogErrorRecordMsg) { [array]$LogErrorMessageTmp += $LogErrorRecordMsg }
				If ($LogErrorInvocationMsg) { [array]$LogErrorMessageTmp += $LogErrorInvocationMsg }
				If ($LogErrorExceptionMsg) { [array]$LogErrorMessageTmp += $LogErrorExceptionMsg }
			}
			Else {
				#  Display selected properties in our custom order
				If ($LogErrorExceptionMsg) { [array]$LogErrorMessageTmp += $LogErrorExceptionMsg }
				If ($LogErrorRecordMsg) { [array]$LogErrorMessageTmp += $LogErrorRecordMsg }
				If ($LogErrorInvocationMsg) { [array]$LogErrorMessageTmp += $LogErrorInvocationMsg }
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
						[string]$InnerExceptionSeperator = '~' * 40
						
						[string[]]$SelectedProperties = & $SelectProperty -InputObject $ErrorInnerException -Property $Property
						$LogErrorInnerExceptionMsg = $ErrorInnerException | Select-Object -Property $SelectedProperties | Format-List | Out-String
						
						If ($Count -gt 0) { $LogInnerMessage += $InnerExceptionSeperator }
						$LogInnerMessage += $LogErrorInnerExceptionMsg
						
						$Count++
						$ErrorInnerException = $ErrorInnerException.InnerException
					}
				}
			}
			
			If ($LogErrorMessage) { $Output = $LogErrorMessage }
			If ($LogInnerMessage) { $Output += $LogInnerMessage }
			
			Write-Output $Output
			
			If (Test-Path -Path 'variable:Output') { Clear-Variable -Name Output }
			If (Test-Path -Path 'variable:LogErrorMessage') { Clear-Variable -Name LogErrorMessage }
			If (Test-Path -Path 'variable:LogInnerMessage') { Clear-Variable -Name LogInnerMessage }
			If (Test-Path -Path 'variable:LogErrorMessageTmp') { Clear-Variable -Name LogErrorMessageTmp }
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
	Show a system icon in the prompt. Options: Application, Asterisk, Error, Exclamation, Hand, Information, None, Question, Shield, Warning, WinLogo. Default: None.
.PARAMETER NoWait
	Specifies whether to show the prompt asynchronously (i.e. allow the script to continue without waiting for a response). Default: $false.
.PARAMETER PersistPrompt
	Specify whether to make the prompt persist in the center of the screen every 10 seconds. The user will have no option but to respond to the prompt - resistance is futile!
.PARAMETER MinimizeWindows
	Specifies whether to minimize other windows when displaying prompt. Default: $false.
.PARAMETER Timeout
	Specifies the time period in seconds after which the prompt should timeout. Default: UI timeout value set in the config XML file.
.PARAMETER ExitOnTimeout
	Specifies whether to exit the script if the UI times out. Default: $true.
.EXAMPLE
	Show-InstallationPrompt -Message 'Do you want to proceed with the installation?' -ButtonRightText 'Yes' -ButtonLeftText 'No'
.EXAMPLE
	Show-InstallationPrompt -Title 'Funny Prompt' -Message 'How are you feeling today?' -ButtonRightText 'Good' -ButtonLeftText 'Bad' -ButtonMiddleText 'Indifferent'
.EXAMPLE
	Show-InstallationPrompt -Message 'You can customize text to appear at the end of an install, or remove it completely for unattended installations.' -Icon Information -NoWait
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Title = $installTitle,
		[Parameter(Mandatory=$false)]
		[string]$Message = '',
		[Parameter(Mandatory=$false)]
		[ValidateSet('Left','Center','Right')]
		[string]$MessageAlignment = 'Center',
		[Parameter(Mandatory=$false)]
		[string]$ButtonRightText = '',
		[Parameter(Mandatory=$false)]
		[string]$ButtonLeftText = '',
		[Parameter(Mandatory=$false)]
		[string]$ButtonMiddleText = '',
		[Parameter(Mandatory=$false)]
		[ValidateSet('Application','Asterisk','Error','Exclamation','Hand','Information','None','Question','Shield','Warning','WinLogo')]
		[string]$Icon = 'None',
		[Parameter(Mandatory=$false)]
		[switch]$NoWait = $false,
		[Parameter(Mandatory=$false)]
		[switch]$PersistPrompt = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$MinimizeWindows = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$Timeout = $configInstallationUITimeout,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ExitOnTimeout = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Bypass if in non-interactive mode
		If ($deployModeNonInteractive) {
			Write-Log -Message "Bypassing Installation Prompt [Mode: $deployMode]... $Message" -Source ${CmdletName}
			Return
		}

		## If the current process is not interactive, re-launch the function with a user account
		If (-not $IsProcessUserInteractive) {
			$ShowInstallPromptResult = Invoke-PSCommandAsUser -PassThru -Command ([scriptblock]::Create("Show-InstallationPrompt -Title '$Title' -Message '$Message' -MessageAlignment '$MessageAlignment' -ButtonRightText '$ButtonRightText' -ButtonLeftText '$ButtonLeftText' -ButtonMiddleText '$ButtonMiddleText' -Icon '$Icon' -NoWait:`$$NoWait -PersistPrompt:`$$PersistPrompt -MinimizeWindows `$$MinimizeWindows -Timeout $Timeout -ExitOnTimeout `$$ExitOnTimeout"))
			If ($ShowInstallPromptResult) {
				Return $ShowInstallPromptResult
			}
			Else {
				Return
			}
		}

		## Get parameters for calling function asynchronously
		[hashtable]$installPromptParameters = $psBoundParameters
		
		## Check if the countdown was specified
		If ($timeout -gt $configInstallationUITimeout) {
			[string]$CountdownTimeoutErr = "The installation UI dialog timeout cannot be longer than the timeout specified in the XML configuration file."
			Write-Log -Message $CountdownTimeoutErr -Severity 3 -Source ${CmdletName}
			Throw $CountdownTimeoutErr
		}
		
		[System.Windows.Forms.Application]::EnableVisualStyles()
		$formInstallationPrompt = New-Object -TypeName System.Windows.Forms.Form
		$pictureBanner = New-Object -TypeName System.Windows.Forms.PictureBox
		$pictureIcon = New-Object -TypeName System.Windows.Forms.PictureBox
		$labelText = New-Object -TypeName System.Windows.Forms.Label
		$buttonRight = New-Object -TypeName System.Windows.Forms.Button
		$buttonMiddle = New-Object -TypeName System.Windows.Forms.Button
		$buttonLeft = New-Object -TypeName System.Windows.Forms.Button
		$buttonAbort = New-Object -TypeName System.Windows.Forms.Button
		$InitialFormInstallationPromptWindowState = New-Object -TypeName System.Windows.Forms.FormWindowState
		
		[scriptblock]$Form_Cleanup_FormClosed = {
			## Remove all event handlers from the controls
			Try {
				$labelText.remove_Click($handler_labelText_Click)
				$buttonLeft.remove_Click($buttonLeft_OnClick)
				$buttonRight.remove_Click($buttonRight_OnClick)
				$buttonMiddle.remove_Click($buttonMiddle_OnClick)
				$buttonAbort.remove_Click($buttonAbort_OnClick)
				$timer.remove_Tick($timer_Tick)
				$timer.dispose()
				$timer = $null
				$timerPersist.remove_Tick($timerPersist_Tick)
				$timerPersist.dispose()
				$timerPersist = $null
				$formInstallationPrompt.remove_Load($Form_StateCorrection_Load)
				$formInstallationPrompt.remove_FormClosed($Form_Cleanup_FormClosed)
			}
			Catch { }
		}
		
		[scriptblock]$Form_StateCorrection_Load = {
			## Correct the initial state of the form to prevent the .NET maximized form issue
			$formInstallationPrompt.WindowState = 'Normal'
			$formInstallationPrompt.AutoSize = $true
			$formInstallationPrompt.TopMost = $true
			$formInstallationPrompt.BringToFront()
			# Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
			Set-Variable -Name formInstallationPromptStartPosition -Value $formInstallationPrompt.Location -Scope Script
		}
		
		## Form
		$formInstallationPrompt.Controls.Add($pictureBanner)
		
		##----------------------------------------------
		## Create padding object
		$paddingNone = New-Object -TypeName System.Windows.Forms.Padding
		$paddingNone.Top = 0
		$paddingNone.Bottom = 0
		$paddingNone.Left = 0
		$paddingNone.Right = 0
		
		## Generic Label properties
		$labelPadding = '20,0,20,0'
		
		## Generic Button properties
		$buttonWidth = 110
		$buttonHeight = 23
		$buttonPadding = 50
		$buttonSize = New-Object -TypeName System.Drawing.Size
		$buttonSize.Width = $buttonWidth
		$buttonSize.Height = $buttonHeight
		$buttonPadding = New-Object -TypeName System.Windows.Forms.Padding
		$buttonPadding.Top = 0
		$buttonPadding.Bottom = 5
		$buttonPadding.Left = 50
		$buttonPadding.Right = 0
		
		## Picture Banner
		$pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
		$pictureBanner.ImageLocation = $appDeployLogoBanner
		$System_Drawing_Point = New-Object -TypeName System.Drawing.Point
		$System_Drawing_Point.X = 0
		$System_Drawing_Point.Y = 0
		$pictureBanner.Location = $System_Drawing_Point
		$pictureBanner.Name = 'pictureBanner'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 50
		$System_Drawing_Size.Width = 450
		$pictureBanner.Size = $System_Drawing_Size
		$pictureBanner.SizeMode = 'CenterImage'
		$pictureBanner.Margin = $paddingNone
		$pictureBanner.TabIndex = 0
		$pictureBanner.TabStop = $false
		
		## Picture Icon
		$pictureIcon.DataBindings.DefaultDataSourceUpdateMode = 0
		If ($icon -ne 'None') { $pictureIcon.Image = ([System.Drawing.SystemIcons]::$Icon).ToBitmap() }
		$System_Drawing_Point = New-Object -TypeName System.Drawing.Point
		$System_Drawing_Point.X = 15
		$System_Drawing_Point.Y = 105
		$pictureIcon.Location = $System_Drawing_Point
		$pictureIcon.Name = 'pictureIcon'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 32
		$System_Drawing_Size.Width = 32
		$pictureIcon.Size = $System_Drawing_Size
		$pictureIcon.AutoSize = $true
		$pictureIcon.Margin = $paddingNone
		$pictureIcon.TabIndex = 0
		$pictureIcon.TabStop = $false
		
		## Label Text
		$labelText.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelText.Name = 'labelText'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 148
		$System_Drawing_Size.Width = 385
		$labelText.Size = $System_Drawing_Size
		$System_Drawing_Point = New-Object -TypeName System.Drawing.Point
		$System_Drawing_Point.X = 25
		$System_Drawing_Point.Y = 50
		$labelText.Location = $System_Drawing_Point
		$labelText.Margin = '0,0,0,0'
		$labelText.Padding = $labelPadding
		$labelText.TabIndex = 1
		$labelText.Text = $message
		$labelText.TextAlign = "Middle$($MessageAlignment)"
		$labelText.Anchor = 'Top'
		$labelText.add_Click($handler_labelText_Click)
		
		## Button Left
		$buttonLeft.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonLeft.Location = '15,200'
		$buttonLeft.Name = 'buttonLeft'
		$buttonLeft.Size = $buttonSize
		$buttonLeft.TabIndex = 5
		$buttonLeft.Text = $buttonLeftText
		$buttonLeft.DialogResult = 'No'
		$buttonLeft.AutoSize = $false
		$buttonLeft.UseVisualStyleBackColor = $true
		$buttonLeft.add_Click($buttonLeft_OnClick)
		
		## Button Middle
		$buttonMiddle.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonMiddle.Location = '170,200'
		$buttonMiddle.Name = 'buttonMiddle'
		$buttonMiddle.Size = $buttonSize
		$buttonMiddle.TabIndex = 6
		$buttonMiddle.Text = $buttonMiddleText
		$buttonMiddle.DialogResult = 'Ignore'
		$buttonMiddle.AutoSize = $true
		$buttonMiddle.UseVisualStyleBackColor = $true
		$buttonMiddle.add_Click($buttonMiddle_OnClick)
		
		## Button Right
		$buttonRight.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonRight.Location = '325,200'
		$buttonRight.Name = 'buttonRight'
		$buttonRight.Size = $buttonSize
		$buttonRight.TabIndex = 7
		$buttonRight.Text = $ButtonRightText
		$buttonRight.DialogResult = 'Yes'
		$buttonRight.AutoSize = $true
		$buttonRight.UseVisualStyleBackColor = $true
		$buttonRight.add_Click($buttonRight_OnClick)
		
		## Button Abort (Hidden)
		$buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonAbort.Name = 'buttonAbort'
		$buttonAbort.Size = '1,1'
		$buttonAbort.DialogResult = 'Abort'
		$buttonAbort.TabIndex = 5
		$buttonAbort.UseVisualStyleBackColor = $true
		$buttonAbort.add_Click($buttonAbort_OnClick)
		
		## Form Installation Prompt
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 270
		$System_Drawing_Size.Width = 450
		$formInstallationPrompt.Size = $System_Drawing_Size
		$formInstallationPrompt.Padding = '0,0,0,10'
		$formInstallationPrompt.Margin = $paddingNone
		$formInstallationPrompt.DataBindings.DefaultDataSourceUpdateMode = 0
		$formInstallationPrompt.Name = 'WelcomeForm'
		$formInstallationPrompt.Text = $title
		$formInstallationPrompt.StartPosition = 'CenterScreen'
		$formInstallationPrompt.FormBorderStyle = 'FixedDialog'
		$formInstallationPrompt.MaximizeBox = $false
		$formInstallationPrompt.MinimizeBox = $false
		$formInstallationPrompt.TopMost = $true
		$formInstallationPrompt.TopLevel = $true
		$formInstallationPrompt.Icon = New-Object -TypeName System.Drawing.Icon -ArgumentList $AppDeployLogoIcon
		$formInstallationPrompt.Controls.Add($pictureBanner)
		$formInstallationPrompt.Controls.Add($pictureIcon)
		$formInstallationPrompt.Controls.Add($labelText)
		$formInstallationPrompt.Controls.Add($buttonAbort)
		If ($buttonLeftText) { $formInstallationPrompt.Controls.Add($buttonLeft) }
		If ($buttonMiddleText) { $formInstallationPrompt.Controls.Add($buttonMiddle) }
		If ($buttonRightText) { $formInstallationPrompt.Controls.Add($buttonRight) }
		
		## Timer
		$timer = New-Object -TypeName System.Windows.Forms.Timer
		$timer.Interval = ($timeout * 1000)
		$timer.Add_Tick({
			Write-Log -Message 'Installation action not taken within a reasonable amount of time.' -Source ${CmdletName}
			$buttonAbort.PerformClick()
		})
		
		## Persistence Timer
		If ($persistPrompt) {
			$timerPersist = New-Object -TypeName System.Windows.Forms.Timer
			$timerPersist.Interval = ($configInstallationPersistInterval * 1000)
			[scriptblock]$timerPersist_Tick = { Refresh-InstallationPrompt }
			$timerPersist.add_Tick($timerPersist_Tick)
			$timerPersist.Start()
		}
		
		## Save the initial state of the form
		$InitialFormInstallationPromptWindowState = $formInstallationPrompt.WindowState
		## Init the OnLoad event to correct the initial state of the form
		$formInstallationPrompt.add_Load($Form_StateCorrection_Load)
		## Clean up the control events
		$formInstallationPrompt.add_FormClosed($Form_Cleanup_FormClosed)
		
		## Start the timer
		$timer.Start()
		
		Function Refresh-InstallationPrompt {
			$formInstallationPrompt.BringToFront()
			$formInstallationPrompt.Location = "$($formInstallationPromptStartPosition.X),$($formInstallationPromptStartPosition.Y)"
			$formInstallationPrompt.Refresh()
		}
		
		## Close the Installation Progress Dialog if running
		Close-InstallationProgress
		
		[string]$installPromptLoggedParameters = ($installPromptParameters.GetEnumerator() | ForEach-Object { If ($_.Value.GetType().Name -eq 'SwitchParameter') { "-$($_.Key):`$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Boolean') { "-$($_.Key) `$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Int32') { "-$($_.Key) $($_.Value)" } Else { "-$($_.Key) `"$($_.Value)`"" } }) -join ' '
		Write-Log -Message "Displaying custom installation prompt with the non-default parameters: [$installPromptLoggedParameters]..." -Source ${CmdletName}
		
		## If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously
		If ($NoWait) {
			#  Remove the NoWait parameter so that the script is run synchronously in the new PowerShell session
			$installPromptParameters.Remove('NoWait')
			#  Format the parameters as a string
			[string]$installPromptParameters = ($installPromptParameters.GetEnumerator() | ForEach-Object { If ($_.Value.GetType().Name -eq 'SwitchParameter') { "-$($_.Key):`$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Boolean') { "-$($_.Key) `$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Int32') { "-$($_.Key) $($_.Value)" } Else { "-$($_.Key) `"$($_.Value)`"" } }) -join ' '
			Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"$scriptPath`" -ReferringApplication `"$installName`" -ShowInstallationPrompt $installPromptParameters" -WindowStyle Hidden -ErrorAction 'SilentlyContinue'
		}
		## Otherwise, show the prompt synchronously. If user cancels, then keep showing it until user responds using one of the buttons.
		Else {
			$showDialog = $true
			While ($showDialog) {
				#  Minimize all other windows
				If ($minimizeWindows) { $shellApp.MinimizeAll() | Out-Null }
				#  Show the Form
				$result = $formInstallationPrompt.ShowDialog()
				If (($result -eq 'Yes') -or ($result -eq 'No') -or ($result -eq 'Ignore') -or ($result -eq 'Abort')) {
					$showDialog = $false
				}
			}
			$formInstallationPrompt.Dispose()
			Switch ($result) {
				'Yes' { Write-Output $buttonRightText }
				'No' { Write-Output $buttonLeftText }
				'Ignore' { Write-Output $buttonMiddleText }
				'Abort' {
					# Restore minimized windows
					$shellApp.UndoMinimizeAll() | Out-Null
					If ($ExitOnTimeout) {
						Exit-Script -ExitCode $configInstallationUIExitCode
					}
					Else {
						Write-Log -Message "UI timed out but `$ExitOnTimeout set to `$false. Continue..." -Source ${CmdletName}
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


#region Function Show-DialogBox
Function Show-DialogBox {
<#
.SYNOPSIS
	Display a custom dialog box with optional title, buttons, icon and timeout.
	Show-InstallationPrompt is recommended over this function as it provides more customization and uses consistent branding with the other UI components.
.DESCRIPTION
	Display a custom dialog box with optional title, buttons, icon and timeout. The default button is "OK", the default Icon is "None", and the default Timeout is none.
.PARAMETER Text
	Text in the message dialog box
.PARAMETER Title
	Title of the message dialog box
.PARAMETER Buttons
	Buttons to be included on the dialog box. Options: OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel, CancelTryAgainContinue. Default: OK.
.PARAMETER DefaultButton
	The Default button that is selected. Options: First, Second, Third. Default: First.
.PARAMETER Icon
	Icon to display on the dialog box. Options: None, Stop, Question, Exclamation, Information. Default: None.
.PARAMETER Timeout
	Timeout period in seconds before automatically closing the dialog box with the return message "Timeout". Default: UI timeout value set in the config XML file.
.PARAMETER TopMost
	Specifies whether the message box is a system modal message box and appears in a topmost window. Default: $true.
.EXAMPLE
	Show-DialogBox -Title 'Installed Complete' -Text 'Installation has completed. Please click OK and restart your computer.' -Icon 'Information'
.EXAMPLE
	Show-DialogBox -Title 'Installation Notice' -Text 'Installation will take approximately 30 minutes. Do you wish to proceed?' -Buttons 'OKCancel' -DefaultButton 'Second' -Icon 'Exclamation' -Timeout 600
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0,HelpMessage='Enter a message for the dialog box')]
		[ValidateNotNullorEmpty()]
		[string]$Text,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Title = $installTitle,
		[Parameter(Mandatory=$false)]
		[ValidateSet('OK','OKCancel','AbortRetryIgnore','YesNoCancel','YesNo','RetryCancel','CancelTryAgainContinue')]
		[string]$Buttons = 'OK',
		[Parameter(Mandatory=$false)]
		[ValidateSet('First','Second','Third')]
		[string]$DefaultButton = 'First',
		[Parameter(Mandatory=$false)]
		[ValidateSet('Exclamation','Information','None','Stop','Question')]
		[string]$Icon = 'None',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Timeout = $configInstallationUITimeout,
		[Parameter(Mandatory=$false)]
		[switch]$TopMost = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Bypass if in non-interactive mode
		If ($deployModeNonInteractive) {
			Write-Log -Message "Bypassing Dialog Box [Mode: $deployMode]: $Text..." -Source ${CmdletName}
			Return
		}
		
		## If the current process is not interactive, re-launch the function with a user account
		If (-not $IsProcessUserInteractive) {
			[string]$DialogBoxResponse = Invoke-PSCommandAsUser -PassThru -Command ([scriptblock]::Create("Show-DialogBox -Text '$Test' -Title '$Title' -Buttons '$Buttons' -DefaultButton '$DefaultButton' -Icon '$Icon' -Timeout '$Timeout' -Topmost:`$$Topmost"))
			Return $DialogBoxResponse
		}
		
		Write-Log -Message "Display Dialog Box with message: $Text..." -Source ${CmdletName}
		
		[hashtable]$dialogButtons = @{
			'OK' = 0
			'OKCancel' = 1
			'AbortRetryIgnore' = 2
			'YesNoCancel' = 3
			'YesNo' = 4
			'RetryCancel' = 5
			'CancelTryAgainContinue' = 6
		}
		
		[hashtable]$dialogIcons = @{
			'None' = 0
			'Stop' = 16
			'Question' = 32
			'Exclamation' = 48
			'Information' = 64
		}
		
		[hashtable]$dialogDefaultButton = @{
			'First' = 0
			'Second' = 256
			'Third' = 512
		}
		
		Switch ($TopMost) {
			$true { $dialogTopMost = 4096 }
			$false { $dialogTopMost = 0 }
		}
		
		$response = $Shell.Popup($Text, $Timeout, $Title, ($dialogButtons[$Buttons] + $dialogIcons[$Icon] + $dialogDefaultButton[$DefaultButton] + $dialogTopMost))
		
		Switch ($response) {
			1 {
				Write-Log -Message 'Dialog Box Response: OK' -Source ${CmdletName}
				Write-Output 'OK'
			}
			2 {
				Write-Log -Message 'Dialog Box Response: Cancel' -Source ${CmdletName}
				Write-Output 'Cancel'
			}
			3 {
				Write-Log -Message 'Dialog Box Response: Abort' -Source ${CmdletName}
				Write-Output 'Abort'
			}
			4 {
				Write-Log -Message 'Dialog Box Response: Retry' -Source ${CmdletName}
				Write-Output 'Retry'
			}
			5 {
				Write-Log -Message 'Dialog Box Response: Ignore' -Source ${CmdletName}
				Write-Output 'Ignore'
			}
			6 {
				Write-Log -Message 'Dialog Box Response: Yes' -Source ${CmdletName}
				Write-Output 'Yes'
			}
			7 {
				Write-Log -Message 'Dialog Box Response: No' -Source ${CmdletName}
				Write-Output 'No'
			}
			10 {
				Write-Log -Message 'Dialog Box Response: Try Again' -Source ${CmdletName}
				Write-Output 'Try Again'
			}
			11 {
				Write-Log -Message 'Dialog Box Response: Continue' -Source ${CmdletName}
				Write-Output 'Continue'
			}
			-1 {
				Write-Log -Message 'Dialog Box Timed Out...' -Source ${CmdletName}
				Write-Output 'Timeout'
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
	Continue if an error is encountered
.EXAMPLE
	Get-HardwarePlatform
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Retrieve hardware platform information.' -Source ${CmdletName}
			$hwBios = Get-WmiObject -Class Win32_BIOS -ErrorAction 'Stop' | Select-Object -Property Version, SerialNnumber
			$hwMakeModel = Get-WMIObject -Class Win32_ComputerSystem -ErrorAction 'Stop' | Select-Object -Property Model, Manufacturer
			
			If ($hwBIOS.Version -match 'VRTUAL') { $hwType = 'Virtual:Hyper-V' }
			ElseIf ($hwBIOS.Version -match 'A M I') { $hwType = 'Virtual:Virtual PC' }
			ElseIf ($hwBIOS.Version -like '*Xen*') { $hwType = 'Virtual:Xen' }
			ElseIf ($hwBIOS.SerialNumber -like '*VMware*') { $hwType = 'Virtual:VMWare' }
			ElseIf ($hwMakeModel.Manufacturer -like '*Microsoft*') { $hwType = 'Virtual:Hyper-V' }
			ElseIf ($hwMakeModel.Manufacturer -like '*VMWare*') { $hwType = 'Virtual:VMWare' }
			ElseIf ($hwMakeModel.Model -like '*Virtual*') { $hwType = 'Virtual' }
			Else { $hwType = 'Physical' }
			
			Write-Output $hwType
		}
		Catch {
			Write-Log -Message "Failed to retrieve hardware platform information. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Continue if an error is encountered
.EXAMPLE
	Get-FreeDiskSpace -Drive 'C:'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Drive = $envSystemDrive,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Retrieve free disk space for drive [$Drive]." -Source ${CmdletName}
			$disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$Drive'" -ErrorAction 'Stop'
			[double]$freeDiskSpace = [math]::Round($disk.FreeSpace / 1MB)

			Write-Log -Message "Free disk space for drive [$Drive]: [$freeDiskSpace MB]." -Source ${CmdletName}
			Write-Output $freeDiskSpace
		}
		Catch {
			Write-Log -Message "Failed to retrieve free disk space for drive [$Drive]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	The name of the application to retrieve information for. Performs a wild card match on the application display name.
.PARAMETER Exact
	Specifies to only match the exact name of the application.
.PARAMETER ProductCode
	The product code of the application to retrieve information for.
.PARAMETER IncludeUpdatesAndHotfixes
	Include matches against updates and hotfixes in results.
.EXAMPLE
	Get-InstalledApplication -Name 'Adobe Flash'
.EXAMPLE
	Get-InstalledApplication -ProductCode '{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string[]]$Name,
		[Parameter(Mandatory=$false)]
		[switch]$Exact = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$ProductCode,
		[Parameter(Mandatory=$false)]
		[switch]$IncludeUpdatesAndHotfixes
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($name) {
			Write-Log -Message "Get information for installed Application Name(s) [$($name -join ', ')]..." -Source ${CmdletName}
		}
		If ($productCode) {
			Write-Log -Message "Get information for installed Product Code [$ProductCode]..." -Source ${CmdletName}
		}
		
		[psobject[]]$installedApplication = @()
		ForEach ($regKey in $regKeyApplications) {
			Try {
				If (Test-Path -Path $regKey -ErrorAction 'Stop') {
					[psobject[]]$regKeyApplication = Get-ChildItem -Path $regKey -ErrorAction 'Stop' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'SilentlyContinue' | Where-Object { $_.DisplayName } }
					ForEach ($regKeyApp in $regKeyApplication) {
						Try {
							[string]$appDisplayName = ''
							[string]$appDisplayVersion = ''
							[string]$appPublisher = ''
							
							## Bypass any updates or hotfixes
							If (-not $IncludeUpdatesAndHotfixes) {
								If ($regKeyApp.DisplayName -match '(?i)kb\d+') { Continue }
								If ($regKeyApp.DisplayName -match 'Cumulative Update') { Continue }
								If ($regKeyApp.DisplayName -match 'Security Update') { Continue }
								If ($regKeyApp.DisplayName -match 'Hotfix') { Continue }
							}
							
							## Remove any control characters which may interfere with logging and creating file path names from these variables
							$appDisplayName = $regKeyApp.DisplayName -replace '[^\u001F-\u007F]',''
							$appDisplayVersion = $regKeyApp.DisplayVersion -replace '[^\u001F-\u007F]',''
							$appPublisher = $regKeyApp.Publisher -replace '[^\u001F-\u007F]',''

							## Determine if application is a 64-bit application
							[boolean]$Is64BitApp = If (($is64Bit) -and ($regKey -notmatch '^HKLM:SOFTWARE\\Wow6432Node')) { $true } Else { $false }
							
							If ($ProductCode) {
								## Verify if there is a match with the product code passed to the script
								If ($regKeyApp.PSChildName -match [regex]::Escape($productCode)) {
									Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] matching product code [$productCode]" -Source ${CmdletName}
									$installedApplication += New-Object -TypeName PSObject -Property @{
										ProductCode = $regKeyApp.PSChildName
										DisplayName = $appDisplayName
										DisplayVersion = $appDisplayVersion
										UninstallString = $regKeyApp.UninstallString
										InstallSource = $regKeyApp.InstallSource
										InstallLocation = $regKeyApp.InstallLocation
										InstallDate = $regKeyApp.InstallDate
										Publisher = $appPublisher
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
											Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] exactly matching application name [$application]" -Source ${CmdletName}
										}
									}
									#  Check for a partial application name match
									ElseIf ($regKeyApp.DisplayName -match [regex]::Escape($application)) {
										$applicationMatched = $true
										Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] matching application name [$application]" -Source ${CmdletName}
									}
									
									If ($applicationMatched) {
										$installedApplication += New-Object -TypeName PSObject -Property @{
											ProductCode = $regKeyApp.PSChildName
											DisplayName = $appDisplayName
											DisplayVersion = $appDisplayVersion
											UninstallString = $regKeyApp.UninstallString
											InstallSource = $regKeyApp.InstallSource
											InstallLocation = $regKeyApp.InstallLocation
											InstallDate = $regKeyApp.InstallDate
											Publisher = $appPublisher
											Is64BitApplication = $Is64BitApp
										}
									}
								}
							}
						}
						Catch {
							Write-Log -Message "Failed to resolve application details from registry for [$appDisplayName]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
							Continue
						}
					}
				}
			}
			Catch {
				Write-Log -Message "Failed to resolve registry path [$regKey]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				Continue
			}
		}
		Write-Output $installedApplication
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
	Sets default switches to be passed to msiexec based on the preferences in the XML configuration file.
	Automatically generates a log file name and creates a verbose log file for all msiexec operations.
	Expects the MSI or MSP file to be located in the "Files" sub directory of the App Deploy Toolkit. Expects transform files to be in the same directory as the MSI file.
.PARAMETER Action
	The action to perform. Options: Install, Uninstall, Patch, Repair, ActiveSetup.
.PARAMETER Path
	The path to the MSI/MSP file or the product code of the installed MSI.
.PARAMETER Transform
	The name of the transform file(s) to be applied to the MSI. The transform file is expected to be in the same directory as the MSI file.
.PARAMETER Patch
	The name of the patch (msp) file(s) to be applied to the MSI for use with the "Install" action. The patch file is expected to be in the same directory as the MSI file.
.PARAMETER Parameters
	Overrides the default parameters specified in the XML configuration file. Install default is: "REBOOT=ReallySuppress /QB!". Uninstall default is: "REBOOT=ReallySuppress /QN".
.PARAMETER LogName
	Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.
	For uninstallations, the product code is resolved to the displayname and version of the application.
.PARAMETER WorkingDirectory
	Overrides the working directory. The working directory is set to the location of the MSI file.
.PARAMETER ContinueOnError
	Continue if an exit code is returned by msiexec that is not recognized by the App Deploy Toolkit.
.EXAMPLE
	Execute-MSI -Action Install -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'
	Installs an MSI
.EXAMPLE
	Execute-MSI -Action Install -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transform 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -Parameters '/QN'
	Installs an MSI, applying a transform and overriding the default MSI toolkit parameters
.EXAMPLE
	Execute-MSI -Action Uninstall -Path '{26923b43-4d38-484f-9b9e-de460746276c}'
	Uninstalls an MSI using a product code
.EXAMPLE
	Execute-MSI -Action Patch -Path 'Adobe_Reader_11.0.3_EN.msp'
	Installs an MSP
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateSet('Install','Uninstall','Patch','Repair','ActiveSetup')]
		[string]$Action,
		[Parameter(Mandatory=$true,HelpMessage='Please enter either the path to the MSI/MSP file or the ProductCode')]
		[ValidateScript({($_ -match $MSIProductCodeRegExPattern) -or ('.msi','.msp' -contains [System.IO.Path]::GetExtension($_))})]
		[Alias('FilePath')]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Transform,
		[Parameter(Mandatory=$false)]
		[Alias('Arguments')]
		[ValidateNotNullorEmpty()]
		[string]$Parameters,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Patch,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$private:LogName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$WorkingDirectory,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $false
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Initialize variable indicating whether $Path variable is a Product Code or not
		[boolean]$PathIsProductCode = $false
		
		## If the path matches a product code
		If ($Path -match $MSIProductCodeRegExPattern) {
			#  Set variable indicating that $Path variable is a Product Code
			[boolean]$PathIsProductCode = $true
			
			#  Resolve the product code to a publisher, application name, and version
			Write-Log -Message 'Resolve product code to a publisher, application name, and version.' -Source ${CmdletName}
			[psobject]$productCodeNameVersion = Get-InstalledApplication -ProductCode $path | Select-Object -Property Publisher, DisplayName, DisplayVersion -First 1 -ErrorAction 'SilentlyContinue'
			
			#  Build the log file name
			If (-not $logName) {
				If ($productCodeNameVersion) {
					If ($productCodeNameVersion.Publisher) {
						$logName = ($productCodeNameVersion.Publisher + '_' + $productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion) -replace "[$invalidFileNameChars]",'' -replace ' ',''
					}
					Else {
						$logName = ($productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion) -replace "[$invalidFileNameChars]",'' -replace ' ',''
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
			If (-not $logName) { $logName = ([System.IO.FileInfo]$path).BaseName } ElseIf ('.log','.txt' -contains [System.IO.Path]::GetExtension($logName)) { $logName = [System.IO.Path]::GetFileNameWithoutExtension($logName) }
		}
		
		If ($configToolkitCompressLogs) {
			## Build the log file path
			[string]$logPath = Join-Path -Path $logTempFolder -ChildPath $logName
		}
		Else {
			## Create the Log directory if it doesn't already exist
			If (-not (Test-Path -Path $configMSILogDir -PathType Container -ErrorAction 'SilentlyContinue')) {
				New-Item -Path $configMSILogDir -ItemType Directory -ErrorAction 'SilentlyContinue' | Out-Null
			}
			## Build the log file path
			[string]$logPath = Join-Path -Path $configMSILogDir -ChildPath $logName
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
			'Install' { $option = '/i'; [string]$msiLogFile = "$logPath" + '_Install'; $msiDefaultParams = $msiInstallDefaultParams }
			'Uninstall' { $option = '/x'; [string]$msiLogFile = "$logPath" + '_Uninstall'; $msiDefaultParams = $msiUninstallDefaultParams }
			'Patch' { $option = '/update'; [string]$msiLogFile = "$logPath" + '_Patch'; $msiDefaultParams = $msiInstallDefaultParams }
			'Repair' { $option = '/f'; [string]$msiLogFile = "$logPath" + '_Repair'; $msiDefaultParams = $msiInstallDefaultParams }
			'ActiveSetup' { $option = '/fups'; [string]$msiLogFile = "$logPath" + '_ActiveSetup' }
		}
		
		## Append ".log" to the MSI logfile path and enclose in quotes
		If ([System.IO.Path]::GetExtension($msiLogFile) -ne '.log') {
			[string]$msiLogFile = $msiLogFile + '.log'
			[string]$msiLogFile = "`"$msiLogFile`""
		}
		
		## If the MSI is in the Files directory, set the full path to the MSI
		If (Test-Path -Path (Join-Path -Path $dirFiles -ChildPath $path -ErrorAction 'SilentlyContinue') -PathType Leaf -ErrorAction 'SilentlyContinue') {
			[string]$msiFile = Join-Path -Path $dirFiles -ChildPath $path
		}
		Else {
			[string]$msiFile = $Path
		}
		
		## Set the working directory of the MSI
		If ((-not $PathIsProductCode) -and (-not $workingDirectory)) { [string]$workingDirectory = Split-Path -Path $msiFile -Parent }
		
		## Get the ProductCode of the MSI
		If ($PathIsProductCode) {
			[string]$MSIProductCode = $path
		}
		Else {
			Try {
				[string]$MSIProductCode = Get-MsiTableProperty -Path $msiFile -Table 'Property' -ContinueOnError $false | Select-Object -ExpandProperty ProductCode -ErrorAction 'Stop'
			}
			Catch {
				Write-Log -Message "Failed to get the ProductCode from the MSI file. Continue with requested action [$Action]..." -Source ${CmdletName}
			}
		}
		
		## Enclose the MSI file in quotes to avoid issues with spaces when running msiexec
		[string]$msiFile = "`"$msiFile`""
		## Enclose the MST file in quotes to avoid issues with spaces when running msiexec
		[string]$mstFile = "`"$transform`""
		## Enclose the MSP file in quotes to avoid issues with spaces when running msiexec
		[string]$mspFile = "`"$patch`""

		## Start building the MsiExec command line starting with the base action and file
		[string]$argsMSI = "$option $msiFile"
		# Add MST
		If ($transform) { $argsMSI = "$argsMSI TRANSFORMS=$mstFile TRANSFORMSSECURE=1" }
		# Add MSP
		If ($patch) { $argsMSI = "$argsMSI PATCH=$mspFile" }
		# Add custom Params if specified. Otherwise, add Default Params.
		If ($Parameters) { $argsMSI = "$argsMSI $Parameters" } Else { $argsMSI = "$argsMSI $msiDefaultParams" }
		# Finally add the logging options
		$argsMSI = "$argsMSI $configMSILoggingOptions $msiLogFile"
		
		## Check if the MSI is already installed. If no valid ProductCode to check, then continue with requested MSI action.
		If ($MSIProductCode) {
			[psobject]$IsMsiInstalled = Get-InstalledApplication -ProductCode $MSIProductCode
		}
		Else {
			If ($Action -eq 'Install') { [boolean]$IsMsiInstalled = $false } Else { [boolean]$IsMsiInstalled = $true }
		}
		
		If (($IsMsiInstalled) -and ($Action -eq 'Install')) {
			Write-Log -Message "The MSI is already installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
		}
		ElseIf (((-not $IsMsiInstalled) -and ($Action -eq 'Install')) -or ($IsMsiInstalled)) {
			## Call the Execute-Process function
			Write-Log -Message "Executing MSI action [$Action]..." -Source ${CmdletName}
			If ($ContinueOnError) {
				If ($WorkingDirectory) {
					Execute-Process -Path $exeMsiexec -Parameters $argsMSI -WorkingDirectory $WorkingDirectory -WindowStyle Normal -ContinueOnError $true
				}
				Else {
					Execute-Process -Path $exeMsiexec -Parameters $argsMSI -WindowStyle Normal -ContinueOnError $true
				}
			}
			Else {
				If ($WorkingDirectory) {
					Execute-Process -Path $exeMsiexec -Parameters $argsMSI -WorkingDirectory $WorkingDirectory -WindowStyle Normal
				}
				Else {
					Execute-Process -Path $exeMsiexec -Parameters $argsMSI -WindowStyle Normal
				}
			}
		}
		Else {
			Write-Log -Message "The MSI is not installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
		}
	}
	End {
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
	The name of the application to uninstall.
.PARAMETER Exact
	Specifies whether to exactly match the name of the application
.PARAMETER ContinueOnError
	Continue if an exit code is returned by msiexec that is not recognized by the App Deploy Toolkit.
.EXAMPLE
	Remove-MSIApplications -Name 'Adobe Flash'
	Removes all versions of software that match the name "Adobe Flash"
.EXAMPLE
	Remove-MSIApplications -Name 'Adobe'
	Removes all versions of software that match the name "Adobe"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		[switch]$Exact = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($Exact) {
			[psobject[]]$installedApplications = Get-InstalledApplication -Name $name -Exact
		}
		Else {
			[psobject[]]$installedApplications = Get-InstalledApplication -Name $name
		}
		
		If (($null -ne $installedApplications) -and ($installedApplications.Count)) {
			ForEach ($installedApplication in $installedApplications) {
				If ($installedApplication.UninstallString -match 'msiexec') {
					Write-Log -Message "Remove application [$($installedApplication.DisplayName) $($installedApplication.Version)]." -Source ${CmdletName}
					If ($ContinueOnError) {
						Execute-MSI -Action Uninstall -Path $installedApplication.ProductCode -ContinueOnError $true
					}
					Else {
						Execute-MSI -Action Uninstall -Path $installedApplication.ProductCode
					}
				}
				Else {
					Write-Log -Message "[$($installedApplication.DisplayName)] uninstall string [$($installedApplication.UninstallString)] does not match `"msiexec`", so removal will not proceed." -Severity 2 -Source ${CmdletName}
				}
			}
		}
		Else {
			Write-Log -Message 'No applications found for removal. Continue...' -Source ${CmdletName}
		}
	}
	End {
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
.PARAMETER WindowStyle
	Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Default: Normal.
.PARAMETER CreateNoWindow
	Specifies whether the process should be started with a new window to contain it. Default is false.
.PARAMETER WorkingDirectory
	The working directory used for executing the process. Defaults to the directory of the file being executed.
.PARAMETER NoWait
	Immediately continue after executing the process.
.PARAMETER PassThru
	Returns ExitCode, STDOut, and STDErr output from the process.
.PARAMETER WaitForMsiExec
	Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that
	that this function waits for the msiexec engine to become available before starting the install.
.PARAMETER MsiExecWaitTime
	Specify the length of time in seconds to wait for the msiexec engine to become available. Default: 600 seconds (10 minutes).
.PARAMETER IgnoreExitCodes
	List the exit codes to ignore.
.PARAMETER ContinueOnError
	Continue if an exit code is returned by the process that is not recognized by the App Deploy Toolkit. Default: $false (fail on error).
.EXAMPLE
	Execute-Process -Path 'uninstall_flash_player_64bit.exe' -Parameters '/uninstall' -WindowStyle Hidden
	If the file is in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.
.EXAMPLE
	Execute-Process -Path "$dirFiles\Bin\setup.exe" -Parameters '/S' -WindowStyle Hidden
.EXAMPLE
	Execute-Process -Path 'setup.exe' -Parameters '/S' -IgnoreExitCodes '1,2'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[Alias('FilePath')]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[Alias('Arguments')]
		[ValidateNotNullorEmpty()]
		[string[]]$Parameters,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Normal','Hidden','Maximized','Minimized')]
		[System.Diagnostics.ProcessWindowStyle]$WindowStyle = 'Normal',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[switch]$CreateNoWindow = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$WorkingDirectory,
		[Parameter(Mandatory=$false)]
		[switch]$NoWait = $false,
		[Parameter(Mandatory=$false)]
		[switch]$PassThru = $false,
		[Parameter(Mandatory=$false)]
		[switch]$WaitForMsiExec = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[timespan]$MsiExecWaitTime = $(New-TimeSpan -Seconds $configMSIMutexWaitTime),
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$IgnoreExitCodes,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $false
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			$returnCode = $null
			
			## Validate and find the fully qualified path for the $Path variable.
			If (([System.IO.Path]::IsPathRooted($Path)) -and ([System.IO.Path]::HasExtension($Path))) {
				Write-Log -Message "[$Path] is a valid fully qualified path, continue." -Source ${CmdletName}
				If (-not (Test-Path -Path $Path -PathType Leaf -ErrorAction 'Stop')) {
					Throw "File [$Path] not found."
				}
			}
			Else {
				#  The first directory to search will be the 'Files' subdirectory of the script directory
				[string]$PathFolders = $dirFiles
				#  Add the current location of the console (Windows always searches this location first)
				[string]$PathFolders = $PathFolders + ';' + (Get-Location -PSProvider 'FileSystem').Path
				#  Add the new path locations to the PATH environment variable
				$env:PATH = $PathFolders + ';' + $env:PATH
				
				#  Get the fully qualified path for the file. Get-Command searches PATH environment variable to find this value.
				[string]$FullyQualifiedPath = Get-Command -Name $Path -CommandType 'Application' -TotalCount 1 -Syntax -ErrorAction 'SilentlyContinue'
				
				#  Revert the PATH environment variable to it's original value
				$env:PATH = $env:PATH -replace [regex]::Escape($PathFolders + ';'), ''
				
				If ($FullyQualifiedPath) {
					Write-Log -Message "[$Path] successfully resolved to fully qualified path [$FullyQualifiedPath]." -Source ${CmdletName}
					$Path = $FullyQualifiedPath
				}
				Else {
					Throw "[$Path] contains an invalid path or file name."
				}
			}
			
			## Set the Working directory (if not specified)
			If (-not $WorkingDirectory) { $WorkingDirectory = Split-Path -Path $Path -Parent -ErrorAction 'Stop' }
			
			## If MSI install, check to see if the MSI installer service is available or if another MSI install is already underway.
			## Please note that a race condition is possible after this check where another process waiting for the MSI installer
			##  to become available grabs the MSI Installer mutex before we do. Not too concerned about this possible race condition.
			If (($Path -match 'msiexec') -or ($WaitForMsiExec)) {
				[boolean]$MsiExecAvailable = Test-MsiExecMutex -MsiExecWaitTime $MsiExecWaitTime
				Start-Sleep -Seconds 1
				If (-not $MsiExecAvailable) {
					#  Default MSI exit code for install already in progress
					[int32]$returnCode = 1618
					Throw 'Please complete in progress MSI installation before proceeding with this install.'
				}
			}
			
			Try {
				## Disable Zone checking to prevent warnings when running executables
				$env:SEE_MASK_NOZONECHECKS = 1
				
				## Using this variable allows capture of exceptions from .NET methods. Private scope only changes value for current function.
				$private:ErrorActionPreference = 'Stop'
				
				## Define process
				$processStartInfo = New-Object -TypeName System.Diagnostics.ProcessStartInfo -ErrorAction 'Stop'
				$processStartInfo.FileName = $Path
				$processStartInfo.WorkingDirectory = $WorkingDirectory
				$processStartInfo.UseShellExecute = $false
				$processStartInfo.ErrorDialog = $false
				$processStartInfo.RedirectStandardOutput = $true
				$processStartInfo.RedirectStandardError = $true
				$processStartInfo.CreateNoWindow = $CreateNoWindow
				If ($Parameters) { $processStartInfo.Arguments = $Parameters }
				If ($windowStyle) { $processStartInfo.WindowStyle = $WindowStyle }
				$process = New-Object -TypeName System.Diagnostics.Process -ErrorAction 'Stop'
				$process.StartInfo = $processStartInfo
				
				## Add event handler to capture process's standard output redirection
				[scriptblock]$processEventHandler = { If (-not [string]::IsNullOrEmpty($EventArgs.Data)) { $Event.MessageData.AppendLine($EventArgs.Data) } }
				$stdOutBuilder = New-Object -TypeName System.Text.StringBuilder -ArgumentList ''
				$stdOutEvent = Register-ObjectEvent -InputObject $process -Action $processEventHandler -EventName 'OutputDataReceived' -MessageData $stdOutBuilder -ErrorAction 'Stop'
				
				## Start Process
				Write-Log -Message "Working Directory is [$WorkingDirectory]" -Source ${CmdletName}
				If ($Parameters) {
					If ($Parameters -match '-Command \&') {
						Write-Log -Message "Executing [$Path [PowerShell ScriptBlock]]..." -Source ${CmdletName}
					}
					Else{
						Write-Log -Message "Executing [$Path $Parameters]..." -Source ${CmdletName}
					}
				}
				Else {
					Write-Log -Message "Executing [$Path]..." -Source ${CmdletName}
				}
				[boolean]$processStarted = $process.Start()
				
				If ($NoWait) {
					Write-Log -Message 'NoWait parameter specified. Continuing without waiting for exit code...' -Source ${CmdletName}
				}
				Else {
					$process.BeginOutputReadLine()
					$stdErr = $($process.StandardError.ReadToEnd()).ToString() -replace $null,''
					
					## Instructs the Process component to wait indefinitely for the associated process to exit.
					$process.WaitForExit()
					
					## HasExited indicates that the associated process has terminated, either normally or abnormally. Wait until HasExited returns $true.
					While (-not ($process.HasExited)) { $process.Refresh(); Start-Sleep -Seconds 1 }
					
					## Get the exit code for the process
					[int32]$returnCode = $process.ExitCode
					
					## Unregister standard output event to retrieve process output
					If ($stdOutEvent) { Unregister-Event -SourceIdentifier $stdOutEvent.Name -ErrorAction 'Stop'; $stdOutEvent = $null }
					$stdOut = $stdOutBuilder.ToString() -replace $null,''
					
					If ($stdErr.Length -gt 0) {
						Write-Log -Message "Standard error output from the process: $stdErr" -Severity 3 -Source ${CmdletName}
					}
				}
			}
			Finally {
				## Make sure the standard output event is unregistered
				If ($stdOutEvent) { Unregister-Event -SourceIdentifier $stdOutEvent.Name -ErrorAction 'Stop'}
				
				## Free resources associated with the process, this does not cause process to exit
				If ($process) { $process.Close() }
				
				## Re-enable Zone checking
				Remove-Item -Path env:SEE_MASK_NOZONECHECKS -ErrorAction 'SilentlyContinue'
			}
			
			If (-not $NoWait) {
				## Check to see whether we should ignore exit codes
				$ignoreExitCodeMatch = $false
				If ($ignoreExitCodes) {
					#  Split the processes on a comma
					[int32[]]$ignoreExitCodesArray = $ignoreExitCodes -split ','
					ForEach ($ignoreCode in $ignoreExitCodesArray) {
						If ($returnCode -eq $ignoreCode) { $ignoreExitCodeMatch = $true }
					}
				}
				#  Or always ignore exit codes
				If ($ContinueOnError) { $ignoreExitCodeMatch = $true }
				
				## If the passthru switch is specified, return the exit code and any output from process
				If ($PassThru) {
					Write-Log -Message "Execution completed with exit code [$returnCode]" -Source ${CmdletName}
					[psobject]$ExecutionResults = New-Object -TypeName PSObject -Property @{ ExitCode = $returnCode; StdOut = $stdOut; StdErr = $stdErr }
					Write-Output $ExecutionResults
				}
				ElseIf ($ignoreExitCodeMatch) {
					Write-Log -Message "Execution complete and the exit code [$returncode] is being ignored" -Source ${CmdletName}
				}
				ElseIf (($returnCode -eq 3010) -or ($returnCode -eq 1641)) {
					Write-Log -Message "Execution completed successfully with exit code [$returnCode]. A reboot is required." -Severity 2 -Source ${CmdletName}
					Set-Variable -Name msiRebootDetected -Value $true -Scope Script
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
					Write-Log -Message "Execution completed successfully with exit code [$returnCode]" -Source ${CmdletName}
				}
				Else {
					[string]$MsiExitCodeMessage = ''
					If ($Path -match 'msiexec') {
						[string]$MsiExitCodeMessage = Get-MsiExitCodeMessage -MsiExitCode $returnCode
					}
					
					If ($MsiExitCodeMessage) {
						Write-Log -Message "Execution failed with exit code [$returnCode]: $MsiExitCodeMessage" -Severity 3 -Source ${CmdletName}
					}
					Else {
						Write-Log -Message "Execution failed with exit code [$returnCode]" -Severity 3 -Source ${CmdletName}
					}
					Exit-Script -ExitCode $returnCode
				}
			}
		}
		Catch {
			If ([string]::IsNullOrEmpty([string]$returnCode)) {
				[int32]$returnCode = 999
				Write-Log -Message "Function failed, setting exit code to [$returnCode]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "Execution completed with exit code [$returnCode]. Function failed. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			}
			If ($PassThru) {
				[psobject]$ExecutionResults = New-Object -TypeName PSObject -Property @{ ExitCode = $returnCode; StdOut = If ($stdOut) { $stdOut } Else { '' }; StdErr = If ($stdErr) { $stdErr } Else { '' } }
				Write-Output $ExecutionResults
			}
			Else {
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
.EXAMPLE
	Get-MsiExitCodeMessage -MsiErrorCode 1618
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[int32]$MsiExitCode
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		$MsiExitCodeMsgSource = @'
		using System;
		using System.Text;
		using System.Runtime.InteropServices;
		public class MsiExitCode
		{
			enum LoadLibraryFlags : int
			{
				DONT_RESOLVE_DLL_REFERENCES         = 0x00000001,
				LOAD_IGNORE_CODE_AUTHZ_LEVEL        = 0x00000010,
				LOAD_LIBRARY_AS_DATAFILE            = 0x00000002,
				LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE  = 0x00000040,
				LOAD_LIBRARY_AS_IMAGE_RESOURCE      = 0x00000020,
				LOAD_WITH_ALTERED_SEARCH_PATH       = 0x00000008
			}
			
			[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
			static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, LoadLibraryFlags dwFlags);
			
			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
			static extern int LoadString(IntPtr hInstance, int uID, StringBuilder lpBuffer, int nBufferMax);
			
			// Get MSI exit code message from msimsg.dll resource dll
			public static string GetMessageFromMsiExitCode(int errCode)
			{
				IntPtr hModuleInstance = LoadLibraryEx("msimsg.dll", IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
				
				StringBuilder sb = new StringBuilder(255);
				LoadString(hModuleInstance, errCode, sb, sb.Capacity + 1);
				
				return sb.ToString();
			}
		}
'@
		If (-not ([System.Management.Automation.PSTypeName]'MsiExitCode').Type) {
			Add-Type -TypeDefinition $MsiExitCodeMsgSource -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			Write-Log -Message "Get message for exit code [$MsiExitCode]." -Source ${CmdletName}
			[string]$MsiExitCodeMsg = [MsiExitCode]::GetMessageFromMsiExitCode($MsiExitCode)
			Write-Output $MsiExitCodeMsg
		}
		Catch {
			Write-Log -Message "Failed to get message for exit code [$MsiExitCode]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion


#region Function Test-MsiExecMutex
Function Test-MsiExecMutex {
<#
.SYNOPSIS
	Wait, up to a timeout, for the MSI installer service to become free.
.DESCRIPTION
	The _MSIExecute mutex is used by the MSI installer service to serialize installations and prevent multiple MSI based installations happening at the same time.
	Wait, up to a timeout (default is 10 minutes), for the MSI installer service to become free by checking to see if the MSI mutex, "Global\\_MSIExecute", is available.
.PARAMETER MsiExecWaitTime
	The length of time to wait for the MSI installer service to become available.
.EXAMPLE
	Test-MsiExecMutex
.EXAMPLE
	Test-MsiExecMutex -MsiExecWaitTime $(New-TimeSpan -Minutes 5)
.EXAMPLE
	Test-MsiExecMutex -MsiExecWaitTime $(New-TimeSpan -Seconds 60)
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://msdn.microsoft.com/en-us/library/aa372909(VS.85).asp
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[timespan]$MsiExecWaitTime = $(New-TimeSpan -Seconds $configMSIMutexWaitTime)
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		$IsMsiExecFreeSource = @'
		using System;
		using System.Threading;
		public class MsiExec
		{
			public static bool IsMsiExecFree(TimeSpan maxWaitTime)
			{
				// Wait (up to a timeout) for the MSI installer service to become free.
				// Returns true for a successful wait, when the installer service has become free.
				// Returns false when waiting for the installer service has exceeded the timeout.
				const string installerServiceMutexName = "Global\\_MSIExecute";
				Mutex MSIExecuteMutex = null;
				bool isMsiExecFree = false;
				
				try
				{
					MSIExecuteMutex = Mutex.OpenExisting(installerServiceMutexName, System.Security.AccessControl.MutexRights.Synchronize);
					isMsiExecFree   = MSIExecuteMutex.WaitOne(maxWaitTime, false);
				}
				catch (WaitHandleCannotBeOpenedException)
				{
					// Mutex doesn't exist, do nothing
					isMsiExecFree = true;
				}
				catch (ObjectDisposedException)
				{
					// Mutex was disposed between opening it and attempting to wait on it, do nothing
					isMsiExecFree = true;
				}
				finally
				{
					if (MSIExecuteMutex != null && isMsiExecFree)
					MSIExecuteMutex.ReleaseMutex();
				}
				return isMsiExecFree;
			}
		}
'@
		If (-not ([System.Management.Automation.PSTypeName]'MsiExec').Type) {
			Add-Type -TypeDefinition $IsMsiExecFreeSource -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			If ($MsiExecWaitTime.TotalMinutes -gt 1) {
				[string]$WaitLogMsg = "$($MsiExecWaitTime.TotalMinutes) minutes"
			}
			ElseIf ($MsiExecWaitTime.TotalMinutes -eq 1) {
				[string]$WaitLogMsg = "$($MsiExecWaitTime.TotalMinutes) minute"
			}
			Else {
				[string]$WaitLogMsg = "$($MsiExecWaitTime.TotalSeconds) seconds"
			}
			Write-Log -Message "Check to see if mutex [Global\\_MSIExecute] is available. Wait up to [$WaitLogMsg] for the mutex to become available." -Source ${CmdletName}
			[boolean]$IsMsiExecInstallFree = [MsiExec]::IsMsiExecFree($MsiExecWaitTime)
			
			If ($IsMsiExecInstallFree) {
				Write-Log -Message 'Mutex [Global\\_MSIExecute] is available.' -Source ${CmdletName}
			}
			Else {
				## Get the command line for the MSI installation in progress
				[string]$msiInProgressCmdLine = Get-WmiObject -Class Win32_Process -Filter "name = 'msiexec.exe'" | Select-Object -ExpandProperty CommandLine | Where-Object { $_ -match '\.msi' } | ForEach-Object { $_.Trim() }
				Write-Log -Message "Mutex [Global\\_MSIExecute] is not available because the following MSI installation is in progress [$msiInProgressCmdLine]" -Severity 2 -Source ${CmdletName}
			}
			Write-Output $IsMsiExecInstallFree
		}
		Catch {
			Write-Log -Message "Failed check for availability of mutex [Global\\_MSIExecute]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			#  We return $true on an error so that an attempt is made to install MSI
			Write-Output $true
		}
	}
	End {
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
	Continue if an error is encountered
.EXAMPLE
	New-Folder -Path "$envWinDir\System32"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			If (-not (Test-Path -Path $Path -PathType Container)) {
				Write-Log -Message "Create folder [$Path]." -Source ${CmdletName}
				New-Item -Path $Path -ItemType Directory -ErrorAction 'Stop'
			}
			Else {
				Write-Log -Message "Folder [$Path] already exists." -Source ${CmdletName}
			}
		}
		Catch {
			Write-Log -Message "Failed to create folder [$Path]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Remove folder and all files recursively in a given path.
.PARAMETER Path
	Path to the folder to remove.
.PARAMETER ContinueOnError
	Continue if an error is encountered
.EXAMPLE
	Remove-Folder -Path "$envWinDir\Downloaded Program Files"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			If (Test-Path -Path $Path -PathType Container) {
				Write-Log -Message "Delete folder(s) and file(s) recursively from path [$path]..." -Source ${CmdletName}
				Remove-Item -Path $Path -Force -Recurse -ErrorAction 'Stop' | Out-Null
			}
			Else {
				Write-Log -Message "Folder [$Path] does not exists..." -Source ${CmdletName}
			}
		}
		Catch {
			Write-Log -Message "Failed to delete folder(s) and file(s) recursively from path [$path]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to delete folder(s) and file(s) recursively from path [$path]: $($_.Exception.Message)"
			}
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
	Copy a file to a destination path.
.DESCRIPTION
	Copy a file to a destination path.
.PARAMETER Path
	Path of the file to copy.
.PARAMETER Destination
	Destination Path of the file to copy.
.PARAMETER Recurse
	Copy files in subdirectories.
.PARAMETER ContinueOnError
	Continue if an error is encountered
.EXAMPLE
	Copy-File -Path "$dirSupportFiles\MyApp.ini" -Destination "$envWindir\MyApp.ini"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Destination,
		[Parameter(Mandatory=$false)]
		[switch]$Recurse = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			If ((-not ([System.IO.Path]::HasExtension($Destination))) -and (-not (Test-Path -Path $Destination -PathType Container))) {
				New-Item -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop' | Out-Null
			}
			
			If ($Recurse) {
				Write-Log -Message "Copy file(s) recursively in path [$path] to destination [$destination]" -Source ${CmdletName}
				Copy-Item -Path $Path -Destination $destination -Force -Recurse -ErrorAction 'Stop' | Out-Null
			}
			Else {
				Write-Log -Message "Copy file in path [$path] to destination [$destination]" -Source ${CmdletName}
				Copy-Item -Path $Path -Destination $destination -Force -ErrorAction 'Stop' | Out-Null
			}
		}
		Catch {
			Write-Log -Message "Failed to copy file(s) in path [$path] to destination [$destination]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Remove a file or all files recursively in a given path.
.DESCRIPTION
	Remove a file or all files recursively in a given path.
.PARAMETER Path
	Path of the file to remove.
.PARAMETER Recurse
	Optionally, remove all files recursively in a directory.
.PARAMETER ContinueOnError
	Continue if an error is encountered.
.EXAMPLE
	Remove-File -Path 'C:\Windows\Downloaded Program Files\Temp.inf'
.EXAMPLE
	Remove-File -Path 'C:\Windows\Downloaded Program Files' -Recurse
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[switch]$Recurse,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			If ($Recurse) {
				Write-Log -Message "Delete file(s) recursively in path [$path]..." -Source ${CmdletName}
				Remove-Item -Path $path -Force -Recurse -ErrorAction 'Stop' | Out-Null
			}
			Else {
				Write-Log -Message "Delete file in path [$path]..." -Source ${CmdletName}
				Remove-Item -Path $path -Force -ErrorAction 'Stop' | Out-Null
			}
		}
		Catch {
			Write-Log -Message "Failed to delete file(s) in path [$path]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to delete file(s) in path [$path]: $($_.Exception.Message)"
			}
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
.EXAMPLE
	Convert-RegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
.EXAMPLE
	Convert-RegistryPath -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Key,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$SID
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Convert the registry key hive to the full path, only match if at the beginning of the line
		If ($Key -match '^HKLM:\\|^HKCU:\\|^HKCR:\\|^HKU:\\|^HKCC:\\|^HKPD:\\') {
			# Converts registry paths that start with, e.g.: HKLM:\
			$key = $key -replace '^HKLM:\\', 'HKEY_LOCAL_MACHINE\'
			$key = $key -replace '^HKCR:\\', 'HKEY_CLASSES_ROOT\'
			$key = $key -replace '^HKCU:\\', 'HKEY_CURRENT_USER\'
			$key = $key -replace '^HKU:\\', 'HKEY_USERS\'
			$key = $key -replace '^HKCC:\\', 'HKEY_CURRENT_CONFIG\'
			$key = $key -replace '^HKPD:\\', 'HKEY_PERFORMANCE_DATA\'
		}
		ElseIf ($Key -match '^HKLM:|^HKCU:|^HKCR:|^HKU:|^HKCC:|^HKPD:') {
			# Converts registry paths that start with, e.g.: HKLM:
			$key = $key -replace '^HKLM:', 'HKEY_LOCAL_MACHINE\'
			$key = $key -replace '^HKCR:', 'HKEY_CLASSES_ROOT\'
			$key = $key -replace '^HKCU:', 'HKEY_CURRENT_USER\'
			$key = $key -replace '^HKU:', 'HKEY_USERS\'
			$key = $key -replace '^HKCC:', 'HKEY_CURRENT_CONFIG\'
			$key = $key -replace '^HKPD:', 'HKEY_PERFORMANCE_DATA\'
		}
		ElseIf ($Key -match '^HKLM\\|^HKCU\\|^HKCR\\|^HKU\\|^HKCC\\|^HKPD\\') {
			# Converts registry paths that start with, e.g.: HKLM\
			$key = $key -replace '^HKLM\\', 'HKEY_LOCAL_MACHINE\'
			$key = $key -replace '^HKCR\\', 'HKEY_CLASSES_ROOT\'
			$key = $key -replace '^HKCU\\', 'HKEY_CURRENT_USER\'
			$key = $key -replace '^HKU\\', 'HKEY_USERS\'
			$key = $key -replace '^HKCC\\', 'HKEY_CURRENT_CONFIG\'
			$key = $key -replace '^HKPD\\', 'HKEY_PERFORMANCE_DATA\'
		}
		
		## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
		If ($PSBoundParameters.ContainsKey('SID')) {
			If ($key -match '^HKEY_CURRENT_USER\\') { $key = $key -replace '^HKEY_CURRENT_USER\\', "HKEY_USERS\$SID\" }
		}
		
		## Append the PowerShell drive to the registry key path
		If ($key -notmatch '^Registry::') { [string]$key = "Registry::$key" }
		
		Write-Log -Message "Return fully qualified registry key path [$key]" -Source ${CmdletName}
		Write-Output $key
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
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Get-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
.EXAMPLE
	Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\iexplore.exe'
.EXAMPLE
	Get-RegistryKey -Key 'HKLM:Software\Wow6432Node\Microsoft\Microsoft SQL Server Compact Edition\v3.5' -Value 'Version'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Key,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Value,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$SID,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[switch]$ReturnEmptyKeyIfExists,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
			If ($PSBoundParameters.ContainsKey('SID')) {
				[string]$key = Convert-RegistryPath -Key $key -SID $SID
			}
			Else {
				[string]$key = Convert-RegistryPath -Key $key
			}
			
			## Check if the registry key exists
			If (-not (Test-Path -Path $key -ErrorAction 'Stop')) {
				Write-Log -Message "Registry key [$key] does not exist" -Severity 2 -Source ${CmdletName}
				$regKeyValue = $null
			}
			Else {
				If (-not $Value) {
					#  Get the registry key and all property values
					Write-Log -Message "Get registry key [$key] and all property values" -Source ${CmdletName}
					$regKeyValue = Get-ItemProperty -Path $key -ErrorAction 'Stop'
					If ((-not $regKeyValue) -and ($ReturnEmptyKeyIfExists)) {
						Write-Log -Message "No property values found for registry key. Get registry key [$key]" -Source ${CmdletName}
						$regKeyValue = Get-Item -Path $key -Force -ErrorAction 'Stop'
					}
				}
				Else {
					#  Get the Value (do not make a strongly typed variable because it depends entirely on what kind of value is being read)
					Write-Log -Message "Get registry key [$key] value [$value]" -Source ${CmdletName}
					$regKeyValue = Get-ItemProperty -Path $key -ErrorAction 'Stop' | Select-Object -ExpandProperty $Value -ErrorAction 'SilentlyContinue'
				}
			}
			
			If ($regKeyValue) { Write-Output $regKeyValue } Else { Write-Output $null }
		}
		Catch {
			If (-not $Value) {
				Write-Log -Message "Failed to read registry key [$key]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to read registry key [$key]: $($_.Exception.Message)"
				}
			}
			Else {
				Write-Log -Message "Failed to read registry key [$key] value [$value]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
.PARAMETER SID
	The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.
	Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Set-RegistryKey -Key $blockedAppPath -Name 'Debugger' -Value $blockedAppDebuggerValue
.EXAMPLE
	Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Name 'Debugger' -Value $blockedAppDebuggerValue -Type String
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Key,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		$Value,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Binary','DWord','ExpandString','MultiString','None','QWord','String','Unknown')]
		[Microsoft.Win32.RegistryValueKind]$Type = 'String',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$SID,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			[string]$RegistryValueWriteAction = 'set'
			
			## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
			If ($PSBoundParameters.ContainsKey('SID')) {
				[string]$key = Convert-RegistryPath -Key $key -SID $SID
			}
			Else {
				[string]$key = Convert-RegistryPath -Key $key
			}
			
			## Create registry key if it doesn't exist
			If (-not (Test-Path -Path $key -ErrorAction 'Stop')) {
				Try {
					Write-Log -Message "Create registry key [$key]." -Source ${CmdletName}
					New-Item -Path $key -ItemType Registry -Force -ErrorAction 'Stop' | Out-Null
				}
				Catch {
					Throw
				}
			}
			
			If ($Name) {
				## Set registry value if it doesn't exist
				If (-not (Get-ItemProperty -Path $key -Name $Name -ErrorAction 'SilentlyContinue')) {
					Write-Log -Message "Set registry key value: [$key] [$name = $value]" -Source ${CmdletName}
					New-ItemProperty -Path $key -Name $name -Value $value -PropertyType $Type -ErrorAction 'Stop' | Out-Null
				}
				## Update registry value if it does exist
				Else {
					[string]$RegistryValueWriteAction = 'update'
					Write-Log -Message "Update registry key value: [$key] [$name = $value]" -Source ${CmdletName}
					Set-ItemProperty -Path $key -Name $name -Value $value -ErrorAction 'Stop' | Out-Null
				}
			}
		}
		Catch {
			If ($Name) {
				Write-Log -Message "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to $RegistryValueWriteAction value [$value] for registry key [$key] [$name]: $($_.Exception.Message)"
				}
			}
			Else {
				Write-Log -Message "Failed to set registry key [$key]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Name of the registry key value to delete.
.PARAMETER Recurse
	Delete registry key recursively.
.PARAMETER SID
	The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.
	Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Remove-RegistryKey -Key 'HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce'
.EXAMPLE
	Remove-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'RunAppInstall'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Key,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		[switch]$Recurse,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$SID,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
			If ($PSBoundParameters.ContainsKey('SID')) {
				[string]$key = Convert-RegistryPath -Key $key -SID $SID
			}
			Else {
				[string]$key = Convert-RegistryPath -Key $key
			}
			
			If (-not ($name)) {
				If ($Recurse) {
					Write-Log -Message "Delete registry key recursively [$key]" -Source ${CmdletName}
					Remove-Item -Path $Key -ErrorAction 'Stop' -Force -Recurse | Out-Null
				}
				Else {
					Write-Log -Message "Delete registry key [$key]" -Source ${CmdletName}
					Remove-Item -Path $Key -ErrorAction 'Stop' -Force | Out-Null
				}
			}
			Else {
				Write-Log -Message "Delete registry value [$key] [$name]" -Source ${CmdletName}
				Remove-ItemProperty -Path $Key -Name $Name -ErrorAction 'Stop' -Force | Out-Null
			}
		}
		Catch {
			If (-not ($name)) {
				Write-Log -Message "Failed to delete registry key [$key]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to delete registry key [$key]: $($_.Exception.Message)"
				}
			}
			Else {
				Write-Log -Message "Failed to delete registry value [$key] [$name]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to delete registry value [$key] [$name]: $($_.Exception.Message)"
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
.EXAMPLE
	[scriptblock]$HKCURegistrySettings = {
		Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $UserProfile.SID
		Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $UserProfile.SID
	}
	Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $HKCURegistrySettings
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[scriptblock]$RegistrySettings,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[psobject[]]$UserProfiles = (Get-UserProfiles)
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		ForEach ($UserProfile in $UserProfiles) {
			Try {
				#  Set the path to the user's registry hive when it is loaded
				[string]$UserRegistryPath = "Registry::HKEY_USERS\$($UserProfile.SID)"
				
				#  Set the path to the user's registry hive file
				[string]$UserRegistryHiveFile = Join-Path -Path $UserProfile.ProfilePath -ChildPath 'NTUSER.DAT'
				
				#  Load the User profile registry hive if it is not already loaded because the User is logged in
				[boolean]$ManuallyLoadedRegHive = $false
				If (-not (Test-Path -Path $UserRegistryPath)) {
					#  Load the User registry hive if the registry hive file exists
					If (Test-Path -Path $UserRegistryHiveFile -PathType Leaf) {
						Write-Log -Message "Load the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]" -Source ${CmdletName}
						[string]$HiveLoadResult = & reg.exe load "`"HKEY_USERS\$($UserProfile.SID)`"" "`"$UserRegistryHiveFile`""
						
						If ($global:LastExitCode -ne 0) {
							Throw "Failed to load the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Failure message [$HiveLoadResult]. Continue..."
						}
						
						[boolean]$ManuallyLoadedRegHive = $true
					}
					Else {
						Throw "Failed to find the registry hive file [$UserRegistryHiveFile] for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. Continue..."
					}
				}
				Else {
					Write-Log -Message "The User [$($UserProfile.NTAccount)] registry hive is already loaded in path [HKEY_USERS\$($UserProfile.SID)]" -Source ${CmdletName}
				}
				
				## Execute ScriptBlock which contains code to manipulate HKCU registry.
				#  Make sure read/write calls to the HKCU registry hive specify the -SID parameter or settings will not be changed for all users.
				#  Example: Set-RegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $UserProfile.SID
				Write-Log -Message 'Execute ScriptBlock to modify HKCU registry settings for all users.' -Source ${CmdletName}
				& $RegistrySettings
			}
			Catch {
				Write-Log -Message "Failed to modify the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)] `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			}
			Finally {
				If ($ManuallyLoadedRegHive) {
					Try {
						Write-Log -Message "Unload the User [$($UserProfile.NTAccount)] registry hive in path [HKEY_USERS\$($UserProfile.SID)]" -Source ${CmdletName}
						[string]$HiveLoadResult = & reg.exe unload "`"HKEY_USERS\$($UserProfile.SID)`""
						
						If ($global:LastExitCode -ne 0) { Throw "$HiveLoadResult" }
					}
					Catch {
						Write-Log -Message "Failed to unload the registry hive for User [$($UserProfile.NTAccount)] with SID [$($UserProfile.SID)]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	To get all well known SIDs available on system: [enum]::GetNames([Security.Principal.WellKnownSidType])
.PARAMETER WellKnownToNTAccount
	Convert the Well Known SID to an NTAccount name
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
	http://psappdeploytoolkit.com
	List of Well Known SIDs: http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,ParameterSetName='NTAccountToSID',ValueFromPipelineByPropertyName=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$AccountName,
		[Parameter(Mandatory=$true,ParameterSetName='SIDToNTAccount',ValueFromPipelineByPropertyName=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$SID,
		[Parameter(Mandatory=$true,ParameterSetName='WellKnownName',ValueFromPipelineByPropertyName=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$WellKnownSIDName,
		[Parameter(Mandatory=$false,ParameterSetName='WellKnownName')]
		[ValidateNotNullOrEmpty()]
		[switch]$WellKnownToNTAccount
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Switch ($PSCmdlet.ParameterSetName) {
				'SIDToNTAccount' {
					[string]$msg = "the SID [$SID] to an NT Account name"
					Write-Log -Message "Convert $msg." -Source ${CmdletName}
					
					$NTAccountSID = New-Object -TypeName System.Security.Principal.SecurityIdentifier -ArgumentList $SID
					$NTAccount = $NTAccountSID.Translate([System.Security.Principal.NTAccount])
					Write-Output $NTAccount
				}
				'NTAccountToSID' {
					[string]$msg = "the NT Account [$AccountName] to a SID"
					Write-Log -Message "Convert $msg." -Source ${CmdletName}
					
					$NTAccount = New-Object -TypeName System.Security.Principal.NTAccount -ArgumentList $AccountName
					$NTAccountSID = $NTAccount.Translate([System.Security.Principal.SecurityIdentifier])
					Write-Output $NTAccountSID
				}
				'WellKnownName' {
					If ($WellKnownToNTAccount) {
						[string]$ConversionType = 'NTAccount'
					}
					Else {
						[string]$ConversionType = 'SID'
					}
					[string]$msg = "the Well Known SID Name [$WellKnownSIDName] to a $ConversionType"
					Write-Log -Message "Convert $msg." -Source ${CmdletName}
					
					#  Get the SID for the root domain
					Try {
						$MachineRootDomain = (Get-WmiObject -Class Win32_ComputerSystem -ErrorAction 'Stop').Domain.ToLower()
						$ADDomainObj = New-Object -TypeName System.DirectoryServices.DirectoryEntry -ArgumentList "LDAP://$MachineRootDomain"
						$DomainSidInBinary = $ADDomainObj.ObjectSid
						$DomainSid = New-Object -TypeName System.Security.Principal.SecurityIdentifier -ArgumentList ($DomainSidInBinary[0], 0)
					}
					Catch {
						Write-Log -Message 'Unable to get Domain SID from Active Directory. Setting Domain SID to $null.' -Severity 2 -Source ${CmdletName}
						$DomainSid = $null
					}
					
					#  Get the SID for the well known SID name
					$WellKnownSidType = [Security.Principal.WellKnownSidType]::$WellKnownSIDName
					$NTAccountSID = New-Object -TypeName System.Security.Principal.SecurityIdentifier -ArgumentList ($WellKnownSidType, $DomainSid)
					
					If ($WellKnownToNTAccount) {
						$NTAccount = $NTAccountSID.Translate([System.Security.Principal.NTAccount])
						Write-Output $NTAccount
					}
					Else {
						Write-Output $NTAccountSID
					}
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to convert $msg. It may not be a valid account anymore or there is some other problem. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
.EXAMPLE
	Get-UserProfiles
.EXAMPLE
	Get-UserProfiles -ExcludeNTAccount 'CONTOSO\Robot','CONTOSO\ntadmin'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string[]]$ExcludeNTAccount,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ExcludeSystemProfiles = $true,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$ExcludeDefaultUser = $false
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Get the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.' -Source ${CmdletName}
			
			## Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine
			[string]$UserProfileListRegKey = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
			[psobject[]]$UserProfiles = Get-ChildItem -Path $UserProfileListRegKey -ErrorAction 'Stop' |
			ForEach-Object {
				Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'Stop' | Where-Object { ($_.ProfileImagePath) } |
				Select-Object @{ Label = 'NTAccount'; Expression = { $(ConvertTo-NTAccountOrSID -SID $_.PSChildName).Value } }, @{ Label = 'SID'; Expression = { $_.PSChildName } }, @{ Label = 'ProfilePath'; Expression = { $_.ProfileImagePath } }
			}
			If ($ExcludeSystemProfiles) {
				[string[]]$SystemProfiles = 'S-1-5-18', 'S-1-5-19', 'S-1-5-20'
				[psobject[]]$UserProfiles = $UserProfiles | Where-Object { $SystemProfiles -notcontains $_.SID }
			}
			If ($ExcludeNTAccount) {
				[psobject[]]$UserProfiles = $UserProfiles | Where-Object { $ExcludeNTAccount -notcontains $_.NTAccount }
			}
			
			## Find the path to the Default User profile
			If (-not $ExcludeDefaultUser) {
				[string]$UserProfilesDirectory = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name ProfilesDirectory -ErrorAction 'Stop' | Select-Object -ExpandProperty ProfilesDirectory

				#  On Windows Vista or higher
				If ([System.Environment]::OSVersion.Version.Major -gt 5) {
					# Path to Default User Profile directory on Windows Vista or higher: By default, C:\Users\Default
					[string]$DefaultUserProfileDirectory = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'Default' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Default'
				}
				#  On Windows XP or lower
				Else {
					#  Default User Profile Name: By default, 'Default User'
					[string]$DefaultUserProfileName = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'DefaultUsersProfile' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'DefaultUsersProfile'
					
					#  Path to Default User Profile directory: By default, C:\Documents and Settings\Default User
					[string]$DefaultUserProfileDirectory = Join-Path -Path $UserProfilesDirectory -ChildPath $DefaultUserProfileName
				}
				
				## Create a custom object for the Default User profile.
				#  Since the Default User is not an actual User account, it does not have a username or a SID.
				#  We will make up a SID and add it to the custom object so that we have a location to load the default registry hive into later on.
				$DefaultUserProfile = New-Object -TypeName PSObject
				$DefaultUserProfile | Add-Member -MemberType NoteProperty -Name NTAccount -Value 'Default User' -Force -ErrorAction 'Stop'
				$DefaultUserProfile | Add-Member -MemberType NoteProperty -Name SID -Value 'S-1-5-21-Default-User' -Force -ErrorAction 'Stop'
				$DefaultUserProfile | Add-Member -MemberType NoteProperty -Name ProfilePath -Value $DefaultUserProfileDirectory -Force -ErrorAction 'Stop'
				
				## Add the Default User custom object to the User Profile list.
				$UserProfiles += $DefaultUserProfile
			}
			
			Write-Output $UserProfiles
		}
		Catch {
			Write-Log -Message "Failed to create a custom object representing all user profiles on the machine. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.EXAMPLE
	Get-FileVersion -File "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$File,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Get file version info for file [$file]" -Source ${CmdletName}
			
			If (Test-Path -Path $File -PathType Leaf) {
				$fileVersion = (Get-Command -Name $file -ErrorAction 'Stop').FileVersionInfo.FileVersion
				If ($fileVersion) {
					## Remove product information to leave only the file version
					$fileVersion = ($fileVersion -split ' ' | Select-Object -First 1)
					
					Write-Log -Message "File version is [$fileVersion]" -Source ${CmdletName}
					Write-Output $fileVersion
				}
				Else {
					Write-Log -Message 'No file version information found.' -Source ${CmdletName}
				}
			}
			Else {
				Throw "File path [$file] does not exist."
			}
		}
		Catch {
			Write-Log -Message "Failed to get file version info. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to get file version info: $($_.Exception.Message)"
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
	Executables, DLLs, ICO files with multiple icons need the icon index to be specified
.PARAMETER Description
	Description of the shortcut
.PARAMETER WorkingDirectory
	Working Directory to be used for the target path
.PARAMETER WindowStyle
	Windows style of the application. Options: Normal, Maximized, Minimized. Default is: Normal.
.PARAMETER RunAsAdmin
	Set shortcut to run program as administrator. This option will prompt user to elevate when executing shortcut.
.PARAMETER ContinueOnError
	Continue if an error is encountered
.EXAMPLE
	New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\system32\notepad.exe" -IconLocation "$envWinDir\system32\notepad.exe" -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$TargetPath,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Arguments,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$IconLocation,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$IconIndex,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Description,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$WorkingDirectory,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Normal','Maximized','Minimized')]
		[string]$WindowStyle,
		[Parameter(Mandatory=$false)]
		[switch]$RunAsAdmin,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		If (-not $Shell) { [__comobject]$Shell = New-Object -ComObject WScript.Shell -ErrorAction 'Stop' }
	}
	Process {
		Try {
			Try {
				[System.IO.FileInfo]$Path = [System.IO.FileInfo]$Path
				[string]$PathDirectory = $Path.DirectoryName
				
				If (-not (Test-Path -Path $PathDirectory -PathType Container -ErrorAction 'Stop')) {
					Write-Log -Message "Create shortcut directory [$PathDirectory]" -Source ${CmdletName}
					New-Item -Path $PathDirectory -ItemType Directory -Force -ErrorAction 'Stop' | Out-Null
				}
			}
			Catch {
				Write-Log -Message "Failed to create shortcut directory [$PathDirectory]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				Throw
			}
			
			Write-Log -Message "Create shortcut [$($path.FullName)]" -Source ${CmdletName}
			If (($path.FullName).EndsWith('.url')) {
				[string[]]$URLFile = '[InternetShortcut]'
				$URLFile += "URL=$targetPath"
				If ($iconIndex) { $URLFile += "IconIndex=$iconIndex" }
				If ($IconLocation) { $URLFile += "IconFile=$iconLocation" }
				$URLFile | Out-File -FilePath $path.FullName -Force -Encoding default -ErrorAction 'Stop'
			}
			ElseIf (($path.FullName).EndsWith('.lnk')) {
				If (($iconLocation -and $iconIndex) -and (-not ($iconLocation.Contains(',')))) {
					$iconLocation = $iconLocation + ",$iconIndex"
				}
				Switch ($windowStyle) {
					'Normal' { $windowStyleInt = 1 }
					'Maximized' { $windowStyleInt = 3 }
					'Minimized' { $windowStyleInt = 7 }
					Default { $windowStyleInt = 1 }
				}
				$shortcut = $shell.CreateShortcut($path.FullName)
				$shortcut.TargetPath = $targetPath
				$shortcut.Arguments = $arguments
				$shortcut.Description = $description
				$shortcut.WorkingDirectory = $workingDirectory
				$shortcut.WindowStyle = $windowStyleInt
				If ($iconLocation) { $shortcut.IconLocation = $iconLocation }
				$shortcut.Save()
				
				## Set shortcut to run program as administrator
				If ($RunAsAdmin) {
					Write-Log -Message 'Set shortcut to run program as administrator.' -Source ${CmdletName}
					$TempFileName = [System.IO.Path]::GetRandomFileName()
					$TempFile = [System.IO.FileInfo][IO.Path]::Combine($Path.Directory, $TempFileName)
					$Writer = New-Object -TypeName System.IO.FileStream -ArgumentList ($TempFile, ([System.IO.FileMode]::Create)) -ErrorAction 'Stop'
					$Reader = $Path.OpenRead()
					While ($Reader.Position -lt $Reader.Length) {
						$Byte = $Reader.ReadByte()
						If ($Reader.Position -eq 22) { $Byte = 34 }
						$Writer.WriteByte($Byte)
					}
					$Reader.Close()
					$Writer.Close()
					$Path.Delete()
					Rename-Item -Path $TempFile -NewName $Path.Name -Force -ErrorAction 'Stop' | Out-Null
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to create shortcut [$($path.FullName)]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to create shortcut [$($path.FullName)]: $($_.Exception.Message)"
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
	Logged in Username under which to run the process from.
.PARAMETER Path
	Path to the file being executed.
.PARAMETER Parameters
	Arguments to be passed to the file being executed.
.PARAMETER RunLevel
	Specifies the level of user rights that Task Scheduler uses to run the task. The acceptable values for this parameter are:
	- HighestAvailable: Tasks run by using the highest available privileges (Admin privileges for Administrators). Default Value.
	- LeastPrivilege: Tasks run by using the least-privileged user account (LUA) privileges.
.PARAMETER Wait
	Wait for the process, launched by the scheduled task, to complete execution before accepting more input. Default is $false.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is $true.
.EXAMPLE
	Execute-ProcessAsUser -UserName 'CONTOSO\User' -Path "$PSHOME\powershell.exe" -Parameters '-Command `"C:\Test\Script.ps1`"; Exit `$LastExitCode' -Wait
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$UserName,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Parameters = '',
		[Parameter(Mandatory=$false)]
		[ValidateSet('HighestAvailable','LeastPrivilege')]
		[string]$RunLevel = 'HighestAvailable',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$Wait = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Reset exit code variable
		If (Test-Path -Path 'variable:executeProcessAsUserExitCode') { Remove-Variable -Name executeProcessAsUserExitCode -Scope Global}
		$global:executeProcessAsUserExitCode = $null
		
		## Confirm if the toolkit is running with administrator privileges
		If (($RunLevel -eq 'HighestAvailable') -and (-not $IsAdmin)) {
			Write-Log -Message "The function [${CmdletName}] requires the toolkit to be running with Administrator privileges if the [-RunLevel] parameter is set to 'HighestAvailable'." -Severity 3 -Source ${CmdletName}
			If ($ContinueOnError) {
				Return
			}
			Else {
				[int32]$global:executeProcessAsUserExitCode = 1
				Exit
			}
		}
		
		## Build the scheduled task XML name
		[string]$schTaskName = "$appDeployToolkitName-ExecuteAsUser"
		
		##  Create the temporary folder if it doesn't already exist
		If (-not (Test-Path -Path $dirAppDeployTemp -PathType Container)) {
			New-Item -Path $dirAppDeployTemp -ItemType Directory -Force -ErrorAction 'Stop'
		}
		
		## If PowerShell.exe is being launched, then create a VBScript to launch PowerShell so that we can suppress the console window that flashes otherwise
		If (($Path -eq 'PowerShell.exe') -or ((Split-Path -Path $Path -Leaf) -eq 'PowerShell.exe')) {
			[string]$executeProcessAsUserParametersVBS = 'chr(34) & ' + "`"$($Path)`"" + ' & chr(34) & ' + '" ' + ($Parameters -replace '"', "`" & chr(34) & `"" -replace ' & chr\(34\) & "$','') + '"'
			[string[]]$executeProcessAsUserScript = "strCommand = $executeProcessAsUserParametersVBS"
			$executeProcessAsUserScript += 'set oWShell = CreateObject("WScript.Shell")'
			$executeProcessAsUserScript += 'intReturn = oWShell.Run(strCommand, 0, true)'
			$executeProcessAsUserScript += 'WScript.Quit intReturn'
			$executeProcessAsUserScript | Out-File -FilePath "$dirAppDeployTemp\$($schTaskName).vbs" -Force -Encoding default -ErrorAction 'SilentlyContinue'
			$Path = 'wscript.exe'
			$Parameters = "`"$dirAppDeployTemp\$($schTaskName).vbs`""
		}
		
		## Specify the scheduled task configuration in XML format
		[string]$xmlSchTask = @"
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
	<IdleSettings />
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
	  <Command>$Path</Command>
	  <Arguments>$Parameters</Arguments>
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
			[string]$xmlSchTaskFilePath = "$dirAppDeployTemp\$schTaskName.xml"
			[string]$xmlSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction Stop
		}
		Catch {
			Write-Log -Message "Failed to export the scheduled task XML file. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If ($ContinueOnError) {
				Return
			}
			Else {
				[int32]$global:executeProcessAsUserExitCode = $schTaskResult.ExitCode
				Exit
			}
		}
		
		## Create Scheduled Task to run the process with a logged-on user account
		Try {
			If ($Parameters) {
				Write-Log -Message "Create scheduled task to run the process [$Path $Parameters] as the logged-on user [$userName]..." -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "Create scheduled task to run the process [$Path] as the logged-on user [$userName]..." -Source ${CmdletName}
			}
			
			[psobject]$schTaskResult = Execute-Process -Path $exeSchTasks -Parameters "/create /f /tn $schTaskName /xml `"$xmlSchTaskFilePath`"" -WindowStyle Hidden -CreateNoWindow -PassThru
			If ($schTaskResult.ExitCode -ne 0) {
				If ($ContinueOnError) {
					Return
				}
				Else {
					[int32]$global:executeProcessAsUserExitCode = $schTaskResult.ExitCode
					Exit
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to create scheduled task. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If ($ContinueOnError) {
				Return
			}
			Else {
				[int32]$global:executeProcessAsUserExitCode = $schTaskResult.ExitCode
				Exit
			}
		}
		
		## Trigger the Scheduled Task
		Try {
			If ($Parameters) {
				Write-Log -Message "Trigger execution of scheduled task with command [$Path $Parameters] as the logged-on user [$userName]..." -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "Trigger execution of scheduled task with command [$Path] as the logged-on user [$userName]..." -Source ${CmdletName}
			}
			[psobject]$schTaskResult = Execute-Process -Path $exeSchTasks -Parameters "/run /i /tn $schTaskName" -WindowStyle Hidden -CreateNoWindow -Passthru
			If ($schTaskResult.ExitCode -ne 0) {
				If ($ContinueOnError) {
					Return
				}
				Else {
					[int32]$global:executeProcessAsUserExitCode = $schTaskResult.ExitCode
					Exit
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to trigger scheduled task. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			#  Delete Scheduled Task
			Write-Log -Message 'Delete the scheduled task which did not to trigger.' -Source ${CmdletName}
			Execute-Process -Path $exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle Hidden -CreateNoWindow -ContinueOnError $true
			If ($ContinueOnError) {
				Return
			}
			Else {
				[int32]$global:executeProcessAsUserExitCode = $schTaskResult.ExitCode
				Exit
			}
		}
		
		## Wait for the process launched by the scheduled task to complete execution
		If ($Wait) {
			Write-Log -Message "Waiting for the process launched by the scheduled task [$schTaskName] to complete execution (this may take some time)..." -Source ${CmdletName}
			Start-Sleep -Seconds 1
			While ((($exeSchTasksResult = & $exeSchTasks /query /TN $schTaskName /V /FO CSV) | ConvertFrom-CSV | Select-Object -ExpandProperty 'Status' | Select-Object -First 1) -eq 'Running') {
				Start-Sleep -Seconds 5
			}
			#  Get the exit code from the process launched by the scheduled task
			[int32]$global:executeProcessAsUserExitCode = ($exeSchTasksResult = & $exeSchTasks /query /TN $schTaskName /V /FO CSV) | ConvertFrom-CSV | Select-Object -ExpandProperty 'Last Result' | Select-Object -First 1
			Write-Log -Message "Exit code from process launched by scheduled task [$global:executeProcessAsUserExitCode]" -Source ${CmdletName}
		}
		
		## Delete scheduled task
		Try {
			Write-Log -Message "Delete scheduled task [$schTaskName]." -Source ${CmdletName}
			Execute-Process -Path $exeSchTasks -Parameters "/delete /tn $schTaskName /f" -WindowStyle Hidden -CreateNoWindow -ErrorAction 'Stop'
		}
		Catch {
			Write-Log -Message "Failed to delete scheduled task [$schTaskName]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
		
		## Exit back to the deployment script which will read the $global:executeProcessAsUserExitCode value to determine the exit code from this function.
		## We need to call 'Exit' because calling 'Exit-Script' directly from the dot-sourced script will only return to the deployment script without exiting the script successfully.
		Exit
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion


#region Function Refresh-Desktop
Function Refresh-Desktop {
<#
.SYNOPSIS
	Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.
.DESCRIPTION
	Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Refresh-Desktop
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		$refreshDesktopSource = @'
		private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
		private const int WM_SETTINGCHANGE = 0x1a;
		private const int SMTO_ABORTIFHUNG = 0x0002;
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		static extern bool SendNotifyMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, IntPtr lpdwResult);
		
		[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
		
		public static void Refresh()
		{
			// Update desktop icons
			SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
			// Update environment variables
			SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 100, IntPtr.Zero);
		}
'@
		If (-not ([System.Management.Automation.PSTypeName]'MyWinAPI.Explorer').Type) {
			Add-Type -MemberDefinition $refreshDesktopSource -Namespace MyWinAPI -Name Explorer -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			Write-Log -Message 'Refresh the Desktop and the Windows Explorer environment process block' -Source ${CmdletName}
			[MyWinAPI.Explorer]::Refresh()
		}
		Catch {
			Write-Log -Message "Failed to refresh the Desktop and the Windows Explorer environment process block. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to refresh the Desktop and the Windows Explorer environment process block: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion


#region Function Refresh-SessionEnvironmentVariables
Function Refresh-SessionEnvironmentVariables {
<#
.SYNOPSIS
	Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.
.DESCRIPTION
	Environment variable changes that take place during script execution are not visible to the current PowerShell session.
	Use this function to refresh the current PowerShell session with all environment variable settings.
.PARAMETER ContinueOnError
	Continue if an error is encountered
.EXAMPLE
	Refresh-SessionEnvironmentVariables
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		[scriptblock]$GetEnvironmentVar = {
			Param (
				$Key,
				$Scope
			)
			[System.Environment]::GetEnvironmentVariable($Key, $Scope)
		}
	}
	Process {
		Try {
			Write-Log -Message 'Refresh the environment variables for this PowerShell session.' -Source ${CmdletName}
			
			[string]$CurrentUserEnvironmentSID = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
			[string]$MachineEnvironmentVars = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
			[string]$UserEnvironmentVars = "Registry::HKEY_USERS\$CurrentUserEnvironmentSID\Environment"
			
			## Update all session environment variables. Ordering is important here: $UserEnvironmentVars comes second so that we can override $MachineEnvironmentVars.
			$MachineEnvironmentVars, $UserEnvironmentVars | Get-Item | Where-Object { $_ } | ForEach-Object { $envRegPath = $_.PSPath; $_ | Select-Object -ExpandProperty Property | ForEach-Object { Set-Item -Path "env:$($_)" -Value (Get-ItemProperty -Path $envRegPath -Name $_).$_ } }
			
			## Set PATH environment variable separately because it is a combination of the user and machine environment variables
			[string[]]$PathFolders = 'Machine', 'User' | ForEach-Object { (& $GetEnvironmentVar -Key 'PATH' -Scope $_) } | Where-Object { $_ } | ForEach-Object { $_.Trim(';') } | ForEach-Object { $_.Split(';') } | ForEach-Object { $_.Trim() } | ForEach-Object { $_.Trim('"') } | Select-Object -Unique
			$env:PATH = $PathFolders -join ';'
		}
		Catch {
			Write-Log -Message "Failed to refresh the environment variables for this PowerShell session. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to refresh the environment variables for this PowerShell session: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion


#region Function Get-ScheduledTask
Function Get-ScheduledTask {
<#
.SYNOPSIS
	Retrieve all details for scheduled tasks on the local computer.
.DESCRIPTION
	Retrieve all details for scheduled tasks on the local computer using schtasks.exe. All property names have spaces and colons removed.
.PARAMETER TaskName
	Specify the name of the scheduled task to retrieve details for. Uses regex match to find scheduled task.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default: $false.
.EXAMPLE
	Get-ScheduledTask
	To display a list of all scheduled task properties.
.EXAMPLE
	Get-ScheduledTask | Out-GridView
	To display a grid view of all scheduled task properties.
.EXAMPLE
	Get-ScheduledTask | Select-Object -Property TaskName
	To display a list of all scheduled task names.
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$TaskName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		If (-not $exeSchTasks) { [string]$exeSchTasks = "$env:WINDIR\system32\schtasks.exe" }
		[psobject[]]$ScheduledTasks = @()
	}
	Process {
		Try {
			Write-Log -Message 'Retrieve Scheduled Tasks' -Source ${CmdletName}
			[string[]]$exeSchtasksResults = & $exeSchTasks /Query /V /FO CSV
			If ($global:LastExitCode -ne 0) { Throw "Failed to retrieve scheduled tasks using [$exeSchTasks]." }
			[psobject[]]$SchtasksResults = $exeSchtasksResults | ConvertFrom-CSV -ErrorAction 'Stop'
			
			If ($SchtasksResults) {
				ForEach ($SchtasksResult in $SchtasksResults) {
					If ($SchtasksResult.TaskName -match $TaskName) {
						$SchtasksResult  | Get-Member -MemberType Properties |
						ForEach -Begin { 
							[hashtable]$Task = @{}
						} -Process {
							## Remove spaces and colons in property names. Do not set property value if line being processed is a column header.
							($Task.($($_.Name).Replace(' ','').Replace(':',''))) = If ($_.Name -ne $SchtasksResult.($_.Name)) { $SchtasksResult.($_.Name) }
						} -End {
							## Only add task to the custom object if all property values are not empty
							If (($Task.Values | Select-Object -Unique | Measure-Object).Count) {
								$ScheduledTasks += New-Object -TypeName PSObject -Property $Task
							}
						}
					}
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to retrieve scheduled tasks. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to retrieve scheduled tasks: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-Output $ScheduledTasks
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion


#region Function Block-AppExecution
Function Block-AppExecution {
<#
.SYNOPSIS
	Block the execution of an application(s)
.DESCRIPTION
	This function is called when you pass the -BlockExecution parameter to the Stop-RunningApplications function. It does the following:
	1. Makes a copy of this script in a temporary directory on the local machine.
	2. Checks for an existing scheduled task from previous failed installation attempt where apps were blocked and if found, calls the Unblock-AppExecution function to restore the original IFEO registry keys.
	   This is to prevent the function from overriding the backup of the original IFEO options.
	3. Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameter -CleanupBlockedApps.
	4. Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameter -ShowBlockedAppDialog.
	5. When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress.
	   The text of this message can be customized in the XML configuration file.
.PARAMETER ProcessName
	Name of the process or processes separated by commas
.EXAMPLE
	Block-AppExecution -ProcessName 'winword,excel'
.NOTES
	This is an internal script function and should typically not be called directly.
	It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to block applications.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		## Specify process names separated by commas
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string[]]$ProcessName
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Bypass if in NonInteractive mode
		If ($deployModeNonInteractive) {
			Write-Log -Message "Bypassing Function [${CmdletName}] [Mode: $deployMode]" -Source ${CmdletName}
			Return
		}
		
		[string]$schTaskBlockedAppsName = $installName + '_BlockedApps'
		
		## Create Temporary directory (if required) and copy Toolkit so it can be called by scheduled task later if required
		If (-not (Test-Path -Path $dirAppDeployTemp -PathType Container -ErrorAction 'SilentlyContinue')) {
			New-Item -Path $dirAppDeployTemp -ItemType Directory -ErrorAction 'SilentlyContinue' | Out-Null
		}
		Copy-Item -Path "$scriptRoot\*.*" -Destination $dirAppDeployTemp -Exclude 'thumbs.db' -Force -Recurse -ErrorAction 'SilentlyContinue'
		
		## Build the debugger block value script
		[string]$debuggerBlockMessageCmd = "`"powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `" & chr(34) & `"$dirAppDeployTemp\$scriptFileName`" & chr(34) & `" -ShowBlockedAppDialog -ReferringApplication `" & chr(34) & `"$installName`" & chr(34)"
		[string[]]$debuggerBlockScript = "strCommand = $debuggerBlockMessageCmd"
		$debuggerBlockScript += 'set oWShell = CreateObject("WScript.Shell")'
		$debuggerBlockScript += 'oWShell.Run strCommand, 0, false'
		$debuggerBlockScript | Out-File -FilePath "$dirAppDeployTemp\AppDeployToolkit_BlockAppExecutionMessage.vbs" -Force -Encoding default -ErrorAction 'SilentlyContinue'
		[string]$debuggerBlockValue = "wscript.exe `"$dirAppDeployTemp\AppDeployToolkit_BlockAppExecutionMessage.vbs`""
		
		## Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
		Write-Log -Message 'Create scheduled task to cleanup blocked applications in case installation is interrupted.' -Source ${CmdletName}
		If (Get-ScheduledTask -ContinueOnError $true | Select-Object -Property TaskName | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
			Write-Log -Message "Scheduled task [$schTaskBlockedAppsName] already exists." -Source ${CmdletName}
		}
		Else {
			[string[]]$schTaskCreationBatchFile = '@ECHO OFF'
			$schTaskCreationBatchFile += "powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `"$dirAppDeployTemp\$scriptFileName`" -CleanupBlockedApps -ReferringApplication `"$installName`""
			$schTaskCreationBatchFile | Out-File -FilePath "$dirAppDeployTemp\AppDeployToolkit_UnBlockApps.bat" -Force -Encoding default -ErrorAction 'SilentlyContinue'
			$schTaskCreation = Execute-Process -Path $exeSchTasks -Parameters "/Create /TN $schTaskBlockedAppsName /RU `"$LocalSystemNTAccount`" /SC ONSTART /TR `"$dirAppDeployTemp\AppDeployToolkit_UnBlockApps.bat`"" -PassThru
		}
		
		[string[]]$blockProcessName = $processName
		## Append .exe to match registry keys
		[string[]]$blockProcessName = $blockProcessName | ForEach-Object { $_ + '.exe' } -ErrorAction 'SilentlyContinue'
		
		## Enumerate each process and set the debugger value to block application execution
		ForEach ($blockProcess in $blockProcessName) {
			Write-Log -Message "Set the Image File Execution Option registry key to block execution of [$blockProcess]." -Source ${CmdletName}
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
.EXAMPLE
	UnblockAppExecution
.NOTES
	This is an internal script function and should typically not be called directly.
	It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to undo the actions performed by Block-AppExecution.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Bypass if in NonInteractive mode
		If ($deployModeNonInteractive) {
			Write-Log -Message "Bypassing Function [${CmdletName}] [Mode: $deployMode]" -Source ${CmdletName}
			Return
		}
		
		## Remove Debugger values to unblock processes
		[psobject[]]$unblockProcesses = $null
		[psobject[]]$unblockProcesses += (Get-ChildItem -Path $regKeyAppExecution -Recurse -ErrorAction 'SilentlyContinue' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'SilentlyContinue'})
		ForEach ($unblockProcess in ($unblockProcesses | Where-Object { $_.Debugger -like '*AppDeployToolkit_BlockAppExecutionMessage*' })) {
			Write-Log -Message "Remove the Image File Execution Options registry key to unblock execution of [$($unblockProcess.PSChildName)]." -Source ${CmdletName} 
			$unblockProcess | Remove-ItemProperty -Name Debugger -ErrorAction 'SilentlyContinue'
		}
		
		## If block execution variable is $true, set it to $false
		If ($BlockExecution) {
			#  Make this variable globally available so we can check whether we need to call Unblock-AppExecution
			Set-Variable -Name BlockExecution -Value $false -Scope Script
		}
		
		## Remove the scheduled task if it exists
		[string]$schTaskBlockedAppsName = $installName + '_BlockedApps'
		If (Get-ScheduledTask -ContinueOnError $true | Select-Object -Property TaskName | Where-Object { $_.TaskName -eq "\$schTaskBlockedAppsName" }) {
			Write-Log -Message "Delete Scheduled Task [$schTaskBlockedAppsName]." -Source ${CmdletName}
			Execute-Process -Path $exeSchTasks -Parameters "/Delete /TN $schTaskBlockedAppsName /F"
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
.EXAMPLE
	Get-DeferHistory
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Write-Log -Message 'Get deferral history...' -Source ${CmdletName}
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
.EXAMPLE
	Set-DeferHistory
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[string]$deferTimesRemaining,
		[Parameter(Mandatory=$false)]
		[string]$deferDeadline
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($deferTimesRemaining -and ($deferTimesRemaining -ge 0)) {
			Write-Log -Message "Set deferral history: [DeferTimesRemaining = $deferTimes]" -Source ${CmdletName}
			Set-RegistryKey -Key $regKeyDeferHistory -Name 'DeferTimesRemaining' -Value $deferTimesRemaining -ContinueOnError $true
		}
		If ($deferDeadline) {
			Write-Log -Message "Set deferral history: [DeferDeadline = $deferDeadline]" -Source ${CmdletName}
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
.EXAMPLE
	Get-UniversalDate
	Returns the current date in a universal sortable date time pattern.
.EXAMPLE
	Get-UniversalDate -DateTime '25/08/2013'
	Returns the date for the current culture in a universal sortable date time pattern.
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		#  Get the current date
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$DateTime = ((Get-Date -Format ($culture).DateTimeFormat.FullDateTimePattern).ToString()),
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		$ContinueOnError = $false
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## If a universal sortable date time pattern was provided, remove the Z, otherwise it could get converted to a different time zone.
			If ($DateTime -match 'Z$') { $DateTime = $DateTime -replace 'Z$', '' }
			[datetime]$DateTime = [datetime]::Parse($DateTime, $culture)
			
			## Convert the date to a universal sortable date time pattern based on the current culture
			Write-Log -Message "Convert the date [$DateTime] to a universal sortable date time pattern based on the current culture [$($culture.Name)]" -Source ${CmdletName}
			[string]$universalDateTime = (Get-Date -Date $DateTime -Format ($culture).DateTimeFormat.UniversalSortableDateTimePattern -ErrorAction 'Stop').ToString()
			Write-Output $universalDateTime
		}
		Catch {
			Write-Log -Message "The specified date/time [$DateTime] is not in a format recognized by the current culture [$($culture.Name)]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Gets the processes that are running from a custom list of process objects.
.DESCRIPTION
	Gets the processes that are running from a custom list of process objects.
.PARAMETER ProcessObjects
	Custom object containing the process objects to search for.
.EXAMPLE
	Get-RunningProcesses
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[psobject[]]$ProcessObjects
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($processObjects) {
			[string]$runningAppsCheck = ($processObjects | ForEach-Object { $_.ProcessName }) -join ','
			Write-Log -Message "Check for running application(s) [$runningAppsCheck]..." -Source ${CmdletName}
			
			## Escape special characters that interfere with regex and might cause false positive matches
			## Join the process names with the regex operator '|' to perform "or" match against multiple applications
			[string]$processNames = ($processObjects | ForEach-Object { [regex]::Escape($_.ProcessName) }) -join '|'
			
			## Get all running processes and escape special characters. Match against the process names to search for to find running processes.
			[System.Diagnostics.Process[]]$runningProcesses = Get-Process | Where-Object { $_.ProcessName -match $processNames }
			
			[array]$runningProcesses = $runningProcesses | ForEach-Object { $_ } | Select-Object -Property ProcessName, Description, ID
			If ($runningProcesses) {
				[string]$runningProcessList = ($runningProcesses | ForEach-Object { $_.ProcessName } | Select-Object -Unique) -join ','
				Write-Log -Message "The following processes are running: [$runningProcessList]" -Source ${CmdletName}
				Write-Log -Message 'Resolve process descriptions...' -Source ${CmdletName}
				## Resolve the running process names to descriptions using the following precedence:
				#  1. The description of the process provided as a Parameter to the function, e.g. -ProcessName "winword=Microsoft Office Word".
				#  2. The description of the process provided by WMI.
				#  3. Fall back on the process name.
				ForEach ($runningProcess in $runningProcesses) {
					ForEach ($processObject in $processObjects) {
						If ($runningProcess.ProcessName -eq ($processObject.ProcessName -replace '.exe', '')) {
							If ($processObject.ProcessDescription) {
								$runningProcess | Add-Member -MemberType NoteProperty -Name Description -Value $processObject.ProcessDescription -Force -ErrorAction 'SilentlyContinue'
							}
						}
					}
					#  Fall back on the process name if no description is provided by the process or as a parameter to the function
					If (-not ($runningProcess.Description)) {
						$runningProcess | Add-Member -MemberType NoteProperty -Name Description -Value $runningProcess.ProcessName -Force -ErrorAction 'SilentlyContinue'
					}
				}
			}
			Else {
				Write-Log -Message 'Application(s) are not running.' -Source ${CmdletName}
			}
			
			Write-Log -Message 'Finished checking running application(s).' -Source ${CmdletName}
			Write-Output $runningProcesses
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
.PARAMETER PersistPrompt
	Specify whether to make the prompt persist in the center of the screen every 10 seconds. The user will have no option but to respond to the prompt. This only takes effect if deferral is not allowed or has expired.
.PARAMETER BlockExecution
	Option to prevent the user from launching the process/application during the installation.
.PARAMETER AllowDefer
	Enables an optional defer button to allow the user to defer the installation.
.PARAMETER AllowDeferCloseApps
	Enables an optional defer button to allow the user to defer the installation only if there are running applications that need to be closed.
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
	Show-InstallationWelcome -CloseApps 'winword.exe,msaccess.exe,excel.exe' -PersistPrompt
	Prompt the user to close Word, MSAccess and Excel if the processes match the exact name specified (use .exe for exact matches).
	By using the PersistPrompt switch, the dialog will return to the center of the screen every 10 seconds so the user cannot ignore it by dragging it aside.
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
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		## Specify process names separated by commas. Optionally specify a process description with an equals symbol, e.g. "winword=Microsoft Office Word"
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$CloseApps,
		## Specify whether to prompt user or force close the applications
		[Parameter(Mandatory=$false)]
		[switch]$Silent = $false,
		## Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$CloseAppsCountdown = 0,
		## Specify a countdown to display before automatically closing applications whether or not deferral is allowed
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$ForceCloseAppsCountdown = 0,
		## Specify whether to make the prompt persist in the center of the screen every 10 seconds.
		[Parameter(Mandatory=$false)]
		[switch]$PersistPrompt = $false,
		## Specify whether to block execution of the processes during installation
		[Parameter(Mandatory=$false)]
		[switch]$BlockExecution = $false,
		## Specify whether to enable the optional defer button on the dialog box
		[Parameter(Mandatory=$false)]
		[switch]$AllowDefer = $false,
		## Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed
		[Parameter(Mandatory=$false)]
		[switch]$AllowDeferCloseApps = $false,
		## Specify the number of times the deferral is allowed
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$DeferTimes = 0,
		## Specify the number of days since first run that the deferral is allowed
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$DeferDays = 0,
		## Specify the deadline (in format dd/mm/yyyy) for which deferral will expire as an option
		[Parameter(Mandatory=$false)]
		[string]$DeferDeadline = '',
		## Specify whether to check if there is enough disk space for the installation to proceed. If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.
		[Parameter(Mandatory=$false)]
		[switch]$CheckDiskSpace = $false,
		## Specify required disk space in MB, used in combination with $CheckDiskSpace.
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$RequiredDiskSpace = 0,
		## Specify whether to minimize other windows when displaying prompt
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$MinimizeWindows = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## If running in NonInteractive mode, force the processes to close silently
		If ($deployModeNonInteractive) { $Silent = $true }
		
		## Check disk space requirements if specified
		If ($CheckDiskSpace) {
			Write-Log -Message 'Evaluate disk space requirements.' -Source ${CmdletName}
			[double]$freeDiskSpace = Get-FreeDiskSpace
			If ($RequiredDiskSpace -eq 0) {
				Try {
					#  Determine the size of the Files folder
					$fso = New-Object -ComObject Scripting.FileSystemObject -ErrorAction 'Stop'
					$RequiredDiskSpace = [math]::Round((($fso.GetFolder($scriptParentPath).Size) / 1MB))
				}
				Catch {
					Write-Log -Message "Failed to calculate disk space requirement from source files. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
			[psobject[]]$processObjects = @()
			#  Split multiple processes on a comma, then split on equal sign, then create custom object with process name and description
			ForEach ($process in ($CloseApps -split ',' | Where-Object { -not ([string]::IsNullOrEmpty($_)) })) {
				$process = $process -split '='
				$processObjects += New-Object -TypeName PSObject -Property @{
					ProcessName = $process[0]
					ProcessDescription = $process[1]
				}
			}
		}
		
		## Check Deferral history and calculate remaining deferrals
		If (($allowDefer) -or ($AllowDeferCloseApps)) {
			#  Set $allowDefer to true if $AllowDeferCloseApps is true
			$allowDefer = $true
			
			#  Get the deferral history from the registry
			$deferHistory = Get-DeferHistory
			$deferHistoryTimes = $deferHistory | Select-Object -ExpandProperty DeferTimesRemaining -ErrorAction 'SilentlyContinue'
			$deferHistoryDeadline = $deferHistory | Select-Object -ExpandProperty DeferDeadline -ErrorAction 'SilentlyContinue'
			
			#  Reset Switches
			$checkDeferDays = $false
			$checkDeferDeadline = $false
			If ($DeferDays -ne 0) { $checkDeferDays = $true }
			If ($DeferDeadline) { $checkDeferDeadline = $true }
			If ($DeferTimes -ne 0) {
				If ($deferHistoryTimes -ge 0) {
					Write-Log -Message "Defer history shows [$($deferHistory.DeferTimesRemaining)] deferrals remaining." -Source ${CmdletName}
					$DeferTimes = $deferHistory.DeferTimesRemaining - 1
				}
				Else {
					$DeferTimes = $DeferTimes - 1
				}
				Write-Log -Message "User has [$deferTimes] deferrals remaining." -Source ${CmdletName}
				If ($DeferTimes -lt 0) {
					Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
					$AllowDefer = $false
				}
			}
			Else {
				[string]$DeferTimes = ''
			}
			If ($checkDeferDays -and $allowDefer) {
				If ($deferHistoryDeadline) {
					Write-Log -Message "Defer history shows a deadline date of [$deferHistoryDeadline]." -Source ${CmdletName}
					[string]$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferHistoryDeadline
				}
				Else {
					[string]$deferDeadlineUniversal = Get-UniversalDate -DateTime (Get-Date -Date ((Get-Date).AddDays($deferDays)) -Format ($culture).DateTimeFormat.FullDateTimePattern)
				}
				Write-Log -Message "User has until [$deferDeadlineUniversal] before deferral expires." -Source ${CmdletName}
				If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
					Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
					$AllowDefer = $false
				}
			}
			If ($checkDeferDeadline -and $allowDefer) {
				#  Validate Date
				Try {
					[string]$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferDeadline -ErrorAction 'Stop'
				}
				Catch {
					Write-Log -Message "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z'. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					Throw "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z': $($_.Exception.Message)"
				}
				Write-Log -Message "User has until [$deferDeadlineUniversal] remaining." -Source ${CmdletName}
				If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
					Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
					$AllowDefer = $false
				}
			}
		}
		If (($deferTimes -lt 0) -and (-not ($deferDeadlineUniversal))) { $AllowDefer = $false }
		
		## Prompt the user to close running applications and optionally defer if enabled
		If (-not ($deployModeSilent) -and (-not ($silent))) {
			If ($forceCloseAppsCountdown -gt 0) {
				#  Keep the same variable for countdown to simplify the code:
				$closeAppsCountdown = $forceCloseAppsCountdown
				#  Change this variable to a boolean now to switch the countdown on even with deferral
				[boolean]$forceCloseAppsCountdown = $true
			}
			Set-Variable -Name closeAppsCountdownGlobal -Value $closeAppsCountdown -Scope Script
			While ((Get-RunningProcesses -ProcessObjects $processObjects | Select-Object -Property * -OutVariable RunningProcesses) -or (($promptResult -ne 'Defer') -and ($promptResult -ne 'Close'))) {
				[string]$runningProcessDescriptions = ($runningProcesses | Select-Object -ExpandProperty Description | Select-Object -Unique | Sort-Object) -join ','
				#  Check if we need to prompt the user to defer, to defer and close apps, or not to prompt them at all
				If ($allowDefer) {
					#  If there is deferral and closing apps is allowed but there are no apps to be closed, break the while loop
					If ($AllowDeferCloseApps -and ($runningProcessDescriptions -eq '')) {
						Break
					}
					#  Otherwise, as long as the user has not selected to close the apps or the processes are still running and the user has not selected to continue, prompt user to close running processes with deferral
					ElseIf (($promptResult -ne 'Close') -or (($runningProcessDescriptions -ne '') -and ($promptResult -ne 'Continue'))) {
						[string]$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -ForceCloseAppsCountdown $forceCloseAppsCountdown -PersistPrompt $PersistPrompt -AllowDefer -DeferTimes $deferTimes -DeferDeadline $deferDeadlineUniversal -MinimizeWindows $minimizeWindows
					}
				}
				#  If there is no deferral and processes are running, prompt the user to close running processes with no deferral option
				ElseIf ($runningProcessDescriptions -ne '') {
					[string]$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -ForceCloseAppsCountdown $forceCloseAppsCountdown -PersistPrompt $PersistPrompt -MinimizeWindows $minimizeWindows
				}
				#  If there is no deferral and no processes running, break the while loop
				Else {
					Break
				}
				
				#  If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again
				If ($promptResult -eq 'Continue') {
					Write-Log -Message 'User selected to continue...' -Source ${CmdletName}
					Start-Sleep -Seconds 2
					
					#  Break the while loop if there are no processes to close and the user has clicked OK to continue
					If (-not ($runningProcesses)) { Break }
				}
				#  Force the applications to close
				ElseIf ($promptResult -eq 'Close') {
					Write-Log -Message 'User selected to force the application(s) to close...' -Source ${CmdletName}
					ForEach ($runningProcess in $runningProcesses) {
						Write-Log -Message "Stop process $($runningProcess.Name)..." -Source ${CmdletName}
						Stop-Process -Id ($runningProcess | Select-Object -ExpandProperty Id) -Force -ErrorAction 'SilentlyContinue'
					}
					Start-Sleep -Seconds 2
				}
				#  Stop the script (if not actioned before the timeout value)
				ElseIf ($promptResult -eq 'Timeout') {
					Write-Log -Message 'Installation not actioned before the timeout value.' -Source ${CmdletName}
					$BlockExecution = $false
					
					If (($deferTimes) -or ($deferDeadlineUniversal)) {
						Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal
					}
					## Dispose the welcome prompt timer here because if we dispose it within the Show-WelcomePrompt function we risk resetting the timer and missing the specified timeout period
					If ($script:welcomeTimer) {
						Try {
							$script:welcomeTimer.Dispose()
							$script:welcomeTimer = $null
						}
						Catch { }
					}
					
					Exit-Script -ExitCode $configInstallationUIExitCode
				}
				#  Stop the script (user chose to defer)
				ElseIf ($promptResult -eq 'Defer') {
					Write-Log -Message 'Installation deferred by the user.' -Source ${CmdletName}
					$BlockExecution = $false
					
					Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal
					
					Exit-Script -ExitCode $configInstallationDeferExitCode
				}
			}
		}
		
		## Force the processes to close silently, without prompting the user
		If (($Silent -or $deployModeSilent) -and $CloseApps) {
			[array]$runningProcesses = $null
			[array]$runningProcesses = Get-RunningProcesses $processObjects
			If ($runningProcesses) {
				[string]$runningProcessDescriptions = ($runningProcesses | Select-Object -ExpandProperty Description | Select-Object -Unique | Sort-Object) -join ','
				Write-Log -Message "Force close application(s) [$($runningProcessDescriptions)] without prompting user." -Source ${CmdletName}
				$runningProcesses | Stop-Process -Force -ErrorAction 'SilentlyContinue'
				Start-Sleep -Seconds 2
			}
		}
		
		## Force nsd.exe to stop if Notes is one of the required applications to close
		If (($processObjects | ForEach-Object { $_.ProcessName }) -match 'notes') {
			[string]$notesPath = Get-Item -Path $regKeyLotusNotes -ErrorAction 'SilentlyContinue' | Get-ItemProperty | Select-Object -ExpandProperty Path
			
			If ($notesPath) {
				[string]$notesNSDExecutable = Join-Path -Path $notesPath -ChildPath 'NSD.Exe'
				Try {
					If (Test-Path -Path $notesNSDExecutable -PathType Leaf -ErrorAction 'Stop') {
						Write-Log -Message "Execute [$notesNSDExecutable] with the -kill argument..." -Source ${CmdletName}
						[System.Diagnostics.Process]$notesNSDProcess = Start-Process -FilePath $notesNSDExecutable -ArgumentList '-kill' -WindowStyle Hidden -PassThru -ErrorAction 'Stop'
						
						If (-not ($notesNSDProcess.WaitForExit(10000))) {
							Write-Log -Message "[$notesNSDExecutable] did not end in a timely manner. Force terminate process." -Source ${CmdletName}
							Stop-Process -Name 'NSD' -Force -ErrorAction 'SilentlyContinue'
						}
					}
				}
				Catch {
					Write-Log -Message "Failed to launch [$notesNSDExecutable]. `n$(Resolve-Error)" -Source ${CmdletName}
				}
				
				Write-Log -Message "[$notesNSDExecutable] returned exit code [$($notesNSDProcess.Exitcode)]" -Source ${CmdletName}
				
				#  Force NSD process to stop in case the previous command was not successful
				Stop-Process -Name 'NSD' -Force -ErrorAction 'SilentlyContinue'
			}
			
			#  Get a list of all the executables in the Notes folder
			[string[]]$notesPathExes = Get-ChildItem -Path $notesPath -Filter '*.exe' -Recurse | Select-Object -ExpandProperty BaseName | Sort-Object
			#  Strip all Notes processes from the process list except notes.exe, because the other notes processes (e.g. notes2.exe) may be invoked by the Notes installation, so we don't want to block their execution.
			If ($notesPathExes) {
				[array]$processesIgnoringNotesExceptions = Compare-Object -ReferenceObject ($processObjects | Select-Object -ExpandProperty ProcessName | Sort-Object) -DifferenceObject $notesPathExes -IncludeEqual | Where-Object { ($_.SideIndicator -eq '<=') -or ($_.InputObject -eq 'notes') } | Select-Object -ExpandProperty InputObject
				[array]$processObjects = $processObjects | Where-Object { $processesIgnoringNotesExceptions -contains $_.ProcessName }
			}
		}
		
		## If block execution switch is true, call the function to block execution of these processes
		If ($BlockExecution) {
			#  Make this variable globally available so we can check whether we need to call Unblock-AppExecution
			Set-Variable -Name BlockExecution -Value $BlockExecution -Scope Script
			Write-Log -Message '[-BlockExecution] parameter specified.' -Source ${CmdletName}
			Block-AppExecution -ProcessName ($processObjects | Select-Object -ExpandProperty ProcessName)
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
	Specify whether to make the prompt persist in the center of the screen every 10 seconds.
.PARAMETER AllowDefer
	Specify whether to provide an option to defer the installation.
.PARAMETER DeferTimes
	Specify the number of times the user is allowed to defer.
.PARAMETER DeferDeadline
	Specify the deadline date before the user is allowed to defer.
.PARAMETER MinimizeWindows
	Specifies whether to minimize other windows when displaying prompt. Default: $true.
.EXAMPLE
	Show-WelcomePrompt -ProcessDescriptions 'Lotus Notes, Microsoft Word' -CloseAppsCountdown 600 -AllowDefer -DeferTimes 10
.NOTES
	This is an internal script function and should typically not be called directly. It is used by the Show-InstallationWelcome prompt to display a custom prompt.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[string]$ProcessDescriptions,
		[Parameter(Mandatory=$false)]
		[int32]$CloseAppsCountdown,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ForceCloseAppsCountdown,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$PersistPrompt = $false,
		[Parameter(Mandatory=$false)]
		[switch]$AllowDefer = $false,
		[Parameter(Mandatory=$false)]
		[int32]$DeferTimes,
		[Parameter(Mandatory=$false)]
		[string]$DeferDeadline,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$MinimizeWindows = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## If the current process is not interactive, re-launch the function for a user account
		If (-not $IsProcessUserInteractive) {
			[string]$promptResult = Invoke-PSCommandAsUser -PassThru -Command ([scriptblock]::Create("Show-WelcomePrompt -ProcessDescriptions '$ProcessDescriptions' -CloseAppsCountdown $CloseAppsCountdown -ForceCloseAppsCountdown `$$ForceCloseAppsCountdown -PersistPrompt `$$PersistPrompt -AllowDefer:`$$AllowDefer -DeferTimes $DeferTimes -DeferDeadline '$DeferDeadline' -MinimizeWindows `$$MinimizeWindows"))
			Return $promptResult
		}

		## Reset switches
		[boolean]$showCloseApps = $false
		[boolean]$showDefer = $false
		[boolean]$persistWindow = $false
		
		## Reset times
		[datetime]$startTime = Get-Date
		[datetime]$countdownTime = $startTime
		
		## Check if the countdown was specified
		If ($CloseAppsCountdown) {
			If ($CloseAppsCountdown -gt $configInstallationUITimeout) {
				Throw 'The close applications countdown time cannot be longer than the timeout specified in the XML configuration for installation UI dialogs to timeout.'
			}
		}
		
		## Initial form layout: Close Applications / Allow Deferral
		If ($processDescriptions) {
			Write-Log -Message "Prompt user to close application(s) [$runningProcessDescriptions]..." -Source ${CmdletName}
			$showCloseApps = $true
		}
		If (($allowDefer) -and (($deferTimes -ge 0) -or ($deferDeadline))) {
			Write-Log -Message 'User has the option to defer.' -Source ${CmdletName}
			$showDefer = $true
			If ($deferDeadline) {
				#  Remove the Z from universal sortable date time format, otherwise it could be converted to a different time zone
				$deferDeadline = $deferDeadline -replace 'Z',''
				#  Convert the deadline date to a string
				[string]$deferDeadline = (Get-Date -Date $deferDeadline).ToString()
			}
		}
		
		## If deferral is being shown and 'close apps countdown' or 'persist prompt' was specified, enable those features.
		If ($showDefer) {
			If ($closeAppsCountdown -gt 0) {
				Write-Log -Message "Close applications countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
				$showCountdown = $true
			}
			If ($persistPrompt) { $persistWindow = $true }
		}
		
		## If 'force close apps countdown' was specified, enable that feature.
		If ($forceCloseAppsCountdown -eq $true) {
			Write-Log -Message "Close applications countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
			$showCountdown = $true
		}
		
		[string[]]$processDescriptions = $processDescriptions.Split(',')
		[System.Windows.Forms.Application]::EnableVisualStyles()
		
		$formWelcome = New-Object -TypeName System.Windows.Forms.Form
		$pictureBanner = New-Object -TypeName System.Windows.Forms.PictureBox
		$labelAppName = New-Object -TypeName System.Windows.Forms.Label
		$labelCountdown = New-Object -TypeName System.Windows.Forms.Label
		$labelDefer = New-Object -TypeName System.Windows.Forms.Label
		$listBoxCloseApps = New-Object -TypeName System.Windows.Forms.ListBox
		$buttonContinue = New-Object -TypeName System.Windows.Forms.Button
		$buttonDefer = New-Object -TypeName System.Windows.Forms.Button
		$buttonCloseApps = New-Object -TypeName System.Windows.Forms.Button
		$buttonAbort = New-Object -TypeName System.Windows.Forms.Button
		$formWelcomeWindowState = New-Object -TypeName System.Windows.Forms.FormWindowState
		$flowLayoutPanel = New-Object -TypeName System.Windows.Forms.FlowLayoutPanel
		$panelButtons = New-Object -TypeName System.Windows.Forms.Panel
		
		## Remove all event handlers from the controls
		[scriptblock]$Form_Cleanup_FormClosed = {
			Try {
				$labelAppName.remove_Click($handler_labelAppName_Click)
				$labelDefer.remove_Click($handler_labelDefer_Click)
				$buttonCloseApps.remove_Click($buttonCloseApps_OnClick)
				$buttonContinue.remove_Click($buttonContinue_OnClick)
				$buttonDefer.remove_Click($buttonDefer_OnClick)
				$buttonAbort.remove_Click($buttonAbort_OnClick)
				$script:welcomeTimer.remove_Tick($timer_Tick)
				$timerPersist.remove_Tick($timerPersist_Tick)
				$formWelcome.remove_Load($Form_StateCorrection_Load)
				$formWelcome.remove_FormClosed($Form_Cleanup_FormClosed)
			}
			Catch {
			}
		}
		
		[scriptblock]$Form_StateCorrection_Load = {
			## Correct the initial state of the form to prevent the .NET maximized form issue
			$formWelcome.WindowState = 'Normal'
			$formWelcome.AutoSize = $true
			$formWelcome.TopMost = $true
			$formWelcome.BringToFront()
			#  Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
			Set-Variable -Name formWelcomeStartPosition -Value $formWelcome.Location -Scope Script
			
			## Initialize the countdown timer
			[datetime]$currentTime = Get-Date
			[datetime]$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
			$script:welcomeTimer.Start()
			
			## Set up the form
			[timespan]$remainingTime = $countdownTime.Subtract($currentTime)
			[string]$labelCountdownSeconds = [string]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
			$labelCountdown.Text = "$configClosePromptCountdownMessage`n$labelCountdownSeconds"
		}
		
		## Add the timer if it doesn't already exist - this avoids the timer being reset if the continue button is clicked
		If (-not ($script:welcomeTimer)) {
			$script:welcomeTimer = New-Object -TypeName System.Windows.Forms.Timer
		}
		
		If ($showCountdown) {
			[scriptblock]$timer_Tick = {
				## Get the time information
				[datetime]$currentTime = Get-Date
				[datetime]$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
				[timespan]$remainingTime = $countdownTime.Subtract($currentTime)
				Set-Variable -Name closeAppsCountdownGlobal -Value $remainingTime.TotalSeconds -Scope Script
				
				## If the countdown is complete, close the application(s)
				If ($countdownTime -lt $currentTime) {
					Write-Log -Message 'Close application(s) countdown timer has elapsed. Force closing application(s).' -Source ${CmdletName}
					$buttonCloseApps.PerformClick()
				}
				Else {
					#  Update the form
					[string]$labelCountdownSeconds = [string]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
					$labelCountdown.Text = "$configClosePromptCountdownMessage`n$labelCountdownSeconds"
					[System.Windows.Forms.Application]::DoEvents()
				}
			}
		}
		Else {
			$script:welcomeTimer.Interval = ($configInstallationUITimeout * 1000)
			[scriptblock]$timer_Tick = { $buttonAbort.PerformClick() }
		}
		
		$script:welcomeTimer.add_Tick($timer_Tick)
		
		## Persistence Timer
		If ($persistWindow) {
			$timerPersist = New-Object -TypeName System.Windows.Forms.Timer
			$timerPersist.Interval = ($configInstallationPersistInterval * 1000)
			[scriptblock]$timerPersist_Tick = { Refresh-InstallationWelcome }
			$timerPersist.add_Tick($timerPersist_Tick)
			$timerPersist.Start()
		}
		
		## Form
		$formWelcome.Controls.Add($pictureBanner)
		$formWelcome.Controls.Add($buttonAbort)
		
		##----------------------------------------------
		## Create padding object
		$paddingNone = New-Object -TypeName System.Windows.Forms.Padding
		$paddingNone.Top = 0
		$paddingNone.Bottom = 0
		$paddingNone.Left = 0
		$paddingNone.Right = 0
		
		## Generic Label properties
		$labelPadding = '20,0,20,0'
		
		## Generic Button properties
		$buttonWidth = 110
		$buttonHeight = 23
		$buttonPadding = 50
		$buttonSize = New-Object -TypeName System.Drawing.Size
		$buttonSize.Width = $buttonWidth
		$buttonSize.Height = $buttonHeight
		$buttonPadding = New-Object -TypeName System.Windows.Forms.Padding
		$buttonPadding.Top = 0
		$buttonPadding.Bottom = 5
		$buttonPadding.Left = 50
		$buttonPadding.Right = 0
		
		## Picture Banner
		$pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
		$pictureBanner.ImageLocation = $appDeployLogoBanner
		$System_Drawing_Point = New-Object -TypeName System.Drawing.Point
		$System_Drawing_Point.X = 0
		$System_Drawing_Point.Y = 0
		$pictureBanner.Location = $System_Drawing_Point
		$pictureBanner.Name = 'pictureBanner'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 50
		$System_Drawing_Size.Width = 450
		$pictureBanner.Size = $System_Drawing_Size
		$pictureBanner.SizeMode = 'CenterImage'
		$pictureBanner.Margin = $paddingNone
		$pictureBanner.TabIndex = 0
		$pictureBanner.TabStop = $false
		
		## Label App Name
		$labelAppName.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelAppName.Name = 'labelAppName'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		If (-not $showCloseApps) {
			$System_Drawing_Size.Height = 40
		}
		Else {
			$System_Drawing_Size.Height = 65
		}
		$System_Drawing_Size.Width = 450
		$labelAppName.Size = $System_Drawing_Size
		$System_Drawing_Size.Height = 0
		$labelAppName.MaximumSize = $System_Drawing_Size
		$labelAppName.Margin = '0,15,0,15'
		$labelAppName.Padding = $labelPadding
		$labelAppName.TabIndex = 1
		
		## Initial form layout: Close Applications / Allow Deferral
		If ($showCloseApps) {
			$labelAppNameText = $configClosePromptMessage
		}
		ElseIf ($showDefer) {
			$labelAppNameText = "$configDeferPromptWelcomeMessage `n$installTitle"
		}
		$labelAppName.Text = $labelAppNameText
		$labelAppName.TextAlign = 'TopCenter'
		$labelAppName.Anchor = 'Top'
		$labelAppName.AutoSize = $true
		$labelAppName.add_Click($handler_labelAppName_Click)
		
		## Listbox Close Applications
		$listBoxCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
		$listBoxCloseApps.FormattingEnabled = $true
		$listBoxCloseApps.Name = 'listBoxCloseApps'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 100
		$System_Drawing_Size.Width = 300
		$listBoxCloseApps.Size = $System_Drawing_Size
		$listBoxCloseApps.Margin = '75,0,0,0'
		$listBoxCloseApps.TabIndex = 3
		ForEach ($processDescription in $ProcessDescriptions) {
			$listboxCloseApps.Items.Add($processDescription) | Out-Null
		}
		
		## Label Defer
		$labelDefer.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelDefer.Name = 'labelDefer'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 90
		$System_Drawing_Size.Width = 450
		$labelDefer.Size = $System_Drawing_Size
		$System_Drawing_Size.Height = 0
		$labelDefer.MaximumSize = $System_Drawing_Size
		$labelDefer.Margin = $paddingNone
		$labelDefer.Padding = $labelPadding
		$labelDefer.TabIndex = 4
		$deferralText = "$configDeferPromptExpiryMessage`n"
		If ($deferTimes -ge 0) {
			$deferralText = "$deferralText `n$configDeferPromptRemainingDeferrals $($deferTimes + 1)"
		}
		If ($deferDeadline) {
			$deferralText = "$deferralText `n$configDeferPromptDeadline $deferDeadline"
		}
		If (($deferTimes -lt 0) -and (-not $DeferDeadline)) {
			$deferralText = "$deferralText `n$configDeferPromptNoDeadline"
		}
		$deferralText = "$deferralText `n`n$configDeferPromptWarningMessage"
		$labelDefer.Text = $deferralText
		$labelDefer.TextAlign = 'MiddleCenter'
		$labelDefer.AutoSize = $true
		$labelDefer.add_Click($handler_labelDefer_Click)
		
		## Label Countdown
		$labelCountdown.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelCountdown.Name = 'labelCountdown'
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 40
		$System_Drawing_Size.Width = 450
		$labelCountdown.Size = $System_Drawing_Size
		$System_Drawing_Size.Height = 0
		$labelCountdown.MaximumSize = $System_Drawing_Size
		$labelCountdown.Margin = $paddingNone
		$labelCountdown.Padding = $labelPadding
		$labelCountdown.TabIndex = 4
		$labelCountdown.Font = 'Microsoft Sans Serif, 9pt, style=Bold'
		$labelCountdown.Text = '00:00:00'
		$labelCountdown.TextAlign = 'MiddleCenter'
		$labelCountdown.AutoSize = $true
		$labelCountdown.add_Click($handler_labelDefer_Click)
		
		## Panel Flow Layout
		$System_Drawing_Point = New-Object -TypeName System.Drawing.Point
		$System_Drawing_Point.X = 0
		$System_Drawing_Point.Y = 50
		$flowLayoutPanel.Location = $System_Drawing_Point
		$flowLayoutPanel.AutoSize = $true
		$flowLayoutPanel.Anchor = 'Top'
		$flowLayoutPanel.FlowDirection = 'TopDown'
		$flowLayoutPanel.WrapContents = $true
		$flowLayoutPanel.Controls.Add($labelAppName)
		If ($showCloseApps) { $flowLayoutPanel.Controls.Add($listBoxCloseApps) }
		If ($showDefer) {
			$flowLayoutPanel.Controls.Add($labelDefer)
		}
		If ($showCloseApps -and $showCountdown) {
			$flowLayoutPanel.Controls.Add($labelCountdown)
		}
		
		## Button Close For Me
		$buttonCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonCloseApps.Location = '15,0'
		$buttonCloseApps.Name = 'buttonCloseApps'
		$buttonCloseApps.Size = $buttonSize
		$buttonCloseApps.TabIndex = 5
		$buttonCloseApps.Text = $configClosePromptButtonClose
		$buttonCloseApps.DialogResult = 'Yes'
		$buttonCloseApps.AutoSize = $true
		$buttonCloseApps.UseVisualStyleBackColor = $true
		$buttonCloseApps.add_Click($buttonCloseApps_OnClick)
		
		## Button Defer
		$buttonDefer.DataBindings.DefaultDataSourceUpdateMode = 0
		If (-not $showCloseApps) {
			$buttonDefer.Location = '15,0'
		}
		Else {
			$buttonDefer.Location = '170,0'
		}
		$buttonDefer.Name = 'buttonDefer'
		$buttonDefer.Size = $buttonSize
		$buttonDefer.TabIndex = 6
		$buttonDefer.Text = $configClosePromptButtonDefer
		$buttonDefer.DialogResult = 'No'
		$buttonDefer.AutoSize = $true
		$buttonDefer.UseVisualStyleBackColor = $true
		$buttonDefer.add_Click($buttonDefer_OnClick)
		
		## Button Continue
		$buttonContinue.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonContinue.Location = '325,0'
		$buttonContinue.Name = 'buttonContinue'
		$buttonContinue.Size = $buttonSize
		$buttonContinue.TabIndex = 7
		$buttonContinue.Text = $configClosePromptButtonContinue
		$buttonContinue.DialogResult = 'OK'
		$buttonContinue.AutoSize = $true
		$buttonContinue.UseVisualStyleBackColor = $true
		$buttonContinue.add_Click($buttonContinue_OnClick)
		
		## Button Abort (Hidden)
		$buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonAbort.Name = 'buttonAbort'
		$buttonAbort.Size = '1,1'
		$buttonAbort.DialogResult = 'Abort'
		$buttonAbort.TabIndex = 5
		$buttonAbort.UseVisualStyleBackColor = $true
		$buttonAbort.add_Click($buttonAbort_OnClick)
		
		## Form Welcome
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 0
		$System_Drawing_Size.Width = 0
		$formWelcome.Size = $System_Drawing_Size
		$formWelcome.Padding = $paddingNone
		$formWelcome.Margin = $paddingNone
		$formWelcome.DataBindings.DefaultDataSourceUpdateMode = 0
		$formWelcome.Name = 'WelcomeForm'
		$formWelcome.Text = $installTitle
		$formWelcome.StartPosition = 'CenterScreen'
		$formWelcome.FormBorderStyle = 'FixedDialog'
		$formWelcome.MaximizeBox = $false
		$formWelcome.MinimizeBox = $false
		$formWelcome.TopMost = $true
		$formWelcome.TopLevel = $true
		$formWelcome.Icon = New-Object -TypeName System.Drawing.Icon -ArgumentList $AppDeployLogoIcon
		$formWelcome.AutoSize = $true
		$formWelcome.Controls.Add($pictureBanner)
		$formWelcome.Controls.Add($flowLayoutPanel)
		
		## Panel Button
		$System_Drawing_Point = New-Object -TypeName System.Drawing.Point
		$System_Drawing_Point.X = 0
		# Calculate the position of the panel relative to the size of the form
		$System_Drawing_Point.Y = (($formWelcome.Size | Select-Object -ExpandProperty Height) - 10)
		$panelButtons.Location = $System_Drawing_Point
		$System_Drawing_Size = New-Object -TypeName System.Drawing.Size
		$System_Drawing_Size.Height = 40
		$System_Drawing_Size.Width = 450
		$panelButtons.Size = $System_Drawing_Size
		$panelButtons.AutoSize = $true
		$panelButtons.Anchor = 'Top'
		$padding = New-Object -TypeName System.Windows.Forms.Padding
		$padding.Top = 0
		$padding.Bottom = 0
		$padding.Left = 0
		$padding.Right = 0
		$panelButtons.Margin = $padding
		If ($showCloseApps) { $panelButtons.Controls.Add($buttonCloseApps) }
		If ($showDefer) { $panelButtons.Controls.Add($buttonDefer) }
		$panelButtons.Controls.Add($buttonContinue)
		
		## Add the Buttons Panel to the form
		$formWelcome.Controls.Add($panelButtons)
		
		## Save the initial state of the form
		$formWelcomeWindowState = $formWelcome.WindowState
		#  Init the OnLoad event to correct the initial state of the form
		$formWelcome.add_Load($Form_StateCorrection_Load)
		#  Clean up the control events
		$formWelcome.add_FormClosed($Form_Cleanup_FormClosed)
		
		Function Refresh-InstallationWelcome {
			$formWelcome.BringToFront()
			$formWelcome.Location = "$($formWelcomeStartPosition.X),$($formWelcomeStartPosition.Y)"
			$formWelcome.Refresh()
		}
		
		## Minimize all other windows
		If ($minimizeWindows) { $shellApp.MinimizeAll() | Out-Null }
		
		## Show the form
		$result = $formWelcome.ShowDialog()
		$formWelcome.Dispose()
		Switch ($result) {
			OK { $result = 'Continue' }
			No { $result = 'Defer'; $shellApp.UndoMinimizeAll() | Out-Null }
			Yes { $result = 'Close'}
			Abort { $result = 'Timeout'; $shellApp.UndoMinimizeAll() | Out-Null }
		}
		
		Write-Output $result
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
	Specifies the number of seconds to countdown before the system restart.
.PARAMETER CountdownNoHideSeconds
	Specifies the number of seconds to display the restart prompt without allowing the window to be hidden.
.PARAMETER NoCountdown
	Specifies not to show a countdown, just the Restart Now and Restart Later buttons.
	The UI will restore/reposition itself persistently based on the interval value specified in the config file.
.EXAMPLE
	Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60
.EXAMPLE
	Show-InstallationRestartPrompt -NoCountdown
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$CountdownSeconds = 60,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$CountdownNoHideSeconds = 30,
		[Parameter(Mandatory=$false)]
		[switch]$NoCountdown = $false
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Bypass if in non-interactive mode
		If ($deployModeNonInteractive) {
			Write-Log -Message "Bypass Installation Restart Prompt [Mode: $deployMode]" -Source ${CmdletName}
			Return
		}

		## If the current process is not interactive, re-launch the function with a user account
		If (-not $IsProcessUserInteractive) {
			Invoke-PSCommandAsUser -Command ([scriptblock]::Create("Show-InstallationRestartPrompt -CountdownSeconds $CountdownSeconds -CountdownNoHideSeconds $CountdownNoHideSeconds -NoCountdown:`$$NoCountdown"))
			Return
		}

		## Get the parameters passed to the function for invoking the function asynchronously
		[hashtable]$installRestartPromptParameters = $psBoundParameters
		
		## Check if we are already displaying a restart prompt
		If (Get-Process | Where-Object { $_.MainWindowTitle -match $configRestartPromptTitle }) {
			Write-Log -Message "${CmdletName} was invoked, but an existing restart prompt was detected. Canceling restart prompt." -Severity 2 -Source ${CmdletName}
			Return
		}
		
		[datetime]$startTime = Get-Date
		[datetime]$countdownTime = $startTime
		
		[System.Windows.Forms.Application]::EnableVisualStyles()
		$formRestart = New-Object -TypeName System.Windows.Forms.Form
		$labelCountdown = New-Object -TypeName System.Windows.Forms.Label
		$labelTimeRemaining = New-Object -TypeName System.Windows.Forms.Label
		$labelMessage = New-Object -TypeName System.Windows.Forms.Label
		$buttonRestartLater = New-Object -TypeName System.Windows.Forms.Button
		$picturebox = New-Object -TypeName System.Windows.Forms.PictureBox
		$buttonRestartNow = New-Object -TypeName System.Windows.Forms.Button
		$timerCountdown = New-Object -TypeName System.Windows.Forms.Timer
		$InitialFormWindowState = New-Object -TypeName System.Windows.Forms.FormWindowState
		
		Function Perform-Restart {
			Write-Log -Message 'Force restart the computer...' -Source ${CmdletName}
			Restart-Computer -Force
		}
		
		[scriptblock]$FormEvent_Load = {
			## Initialize the countdown timer
			[datetime]$currentTime = Get-Date
			[datetime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
			$timerCountdown.Start()
			## Set up the form
			[timespan]$remainingTime = $countdownTime.Subtract($currentTime)
			$labelCountdown.Text = [string]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
			If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) { $buttonRestartLater.Enabled = $false }
			$formRestart.WindowState = 'Normal'
			$formRestart.TopMost = $true
			$formRestart.BringToFront()
		}
		
		[scriptblock]$Form_StateCorrection_Load = {
			## Correct the initial state of the form to prevent the .NET maximized form issue
			$formRestart.WindowState = $InitialFormWindowState
			$formRestart.AutoSize = $true
			$formRestart.TopMost = $true
			$formRestart.BringToFront()
			## Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
			Set-Variable -Name formInstallationRestartPromptStartPosition -Value $formRestart.Location -Scope Script
		}
		
		## Persistence Timer
		If ($NoCountdown) {
			$timerPersist = New-Object -TypeName System.Windows.Forms.Timer
			$timerPersist.Interval = ($configInstallationRestartPersistInterval * 1000)
			[scriptblock]$timerPersist_Tick = {
				#  Show the Restart Popup
				$formRestart.WindowState = 'Normal'
				$formRestart.TopMost = $true
				$formRestart.BringToFront()
				$formRestart.Location = "$($formInstallationRestartPromptStartPosition.X),$($formInstallationRestartPromptStartPosition.Y)"
				$formRestart.Refresh()
				[System.Windows.Forms.Application]::DoEvents()
			}
			$timerPersist.add_Tick($timerPersist_Tick)
			$timerPersist.Start()
		}
		
		[scriptblock]$buttonRestartLater_Click = {
			## Minimize the form
			$formRestart.WindowState = 'Minimized'
			## Reset the persistence timer
			$timerPersist.Stop()
			$timerPersist.Start()
		}
		
		## Restart the computer
		[scriptblock]$buttonRestartNow_Click = { Perform-Restart }
		
		## Hide the form if minimized
		[scriptblock]$formRestart_Resize = { If ($formRestart.WindowState -eq 'Minimized') { $formRestart.WindowState = 'Minimized' } }
		
		[scriptblock]$timerCountdown_Tick = {
			## Get the time information
			[datetime]$currentTime = Get-Date
			[datetime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
			[timespan]$remainingTime = $countdownTime.Subtract($currentTime)
			## If the countdown is complete, restart the machine
			If ($countdownTime -lt $currentTime) {
				$buttonRestartNow.PerformClick()
			}
			Else {
				## Update the form
				$labelCountdown.Text = [string]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
				If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) {
					$buttonRestartLater.Enabled = $false
					#  If the form is hidden when we hit the "No Hide", bring it back up
					If ($formRestart.WindowState -eq 'Minimized') {
						#  Show Popup
						$formRestart.WindowState = 'Normal'
						$formRestart.TopMost = $true
						$formRestart.BringToFront()
						$formRestart.Location = "$($formInstallationRestartPromptStartPosition.X),$($formInstallationRestartPromptStartPosition.Y)"
						$formRestart.Refresh()
						[System.Windows.Forms.Application]::DoEvents()
					}
				}
				[System.Windows.Forms.Application]::DoEvents()
			}
		}
		
		## Remove all event handlers from the controls
		[scriptblock]$Form_Cleanup_FormClosed = {
			Try {
				$buttonRestartLater.remove_Click($buttonRestartLater_Click)
				$buttonRestartNow.remove_Click($buttonRestartNow_Click)
				$formRestart.remove_Load($FormEvent_Load)
				$formRestart.remove_Resize($formRestart_Resize)
				$timerCountdown.remove_Tick($timerCountdown_Tick)
				$timerPersist.remove_Tick($timerPersist_Tick)
				$formRestart.remove_Load($Form_StateCorrection_Load)
				$formRestart.remove_FormClosed($Form_Cleanup_FormClosed)
			}
			Catch {
			}
		}
		
		## Form
		If (-not $NoCountdown) {
			$formRestart.Controls.Add($labelCountdown)
			$formRestart.Controls.Add($labelTimeRemaining)
		}
		$formRestart.Controls.Add($labelMessage)
		$formRestart.Controls.Add($buttonRestartLater)
		$formRestart.Controls.Add($picturebox)
		$formRestart.Controls.Add($buttonRestartNow)
		$formRestart.ClientSize = '450,260'
		$formRestart.ControlBox = $false
		$formRestart.FormBorderStyle = 'FixedDialog'
		$formRestart.Icon = New-Object -TypeName System.Drawing.Icon -ArgumentList $AppDeployLogoIcon
		$formRestart.MaximizeBox = $false
		$formRestart.MinimizeBox = $false
		$formRestart.Name = 'formRestart'
		$formRestart.StartPosition = 'CenterScreen'
		$formRestart.Text = "$($configRestartPromptTitle): $installTitle"
		$formRestart.add_Load($FormEvent_Load)
		$formRestart.add_Resize($formRestart_Resize)
		
		## Banner
		$picturebox.Anchor = 'Top'
		$picturebox.Image = [System.Drawing.Image]::Fromfile($AppDeployLogoBanner)
		$picturebox.Location = '0,0'
		$picturebox.Name = 'picturebox'
		$picturebox.Size = '450,50'
		$picturebox.SizeMode = 'CenterImage'
		$picturebox.TabIndex = 1
		$picturebox.TabStop = $false
		
		## Label Message
		$labelMessage.Location = '20,58'
		$labelMessage.Name = 'labelMessage'
		$labelMessage.Size = '400,79'
		$labelMessage.TabIndex = 3
		$labelMessage.Text = "$configRestartPromptMessage $configRestartPromptMessageTime `n`n$configRestartPromptMessageRestart"
		If ($NoCountdown) { $labelMessage.Text = $configRestartPromptMessage }
		$labelMessage.TextAlign = 'MiddleCenter'
		
		## Label Time Remaining
		$labelTimeRemaining.Location = '20,138'
		$labelTimeRemaining.Name = 'labelTimeRemaining'
		$labelTimeRemaining.Size = '400,23'
		$labelTimeRemaining.TabIndex = 4
		$labelTimeRemaining.Text = $configRestartPromptTimeRemaining
		$labelTimeRemaining.TextAlign = 'MiddleCenter'
		
		## Label Countdown
		$labelCountdown.Font = 'Microsoft Sans Serif, 18pt, style=Bold'
		$labelCountdown.Location = '20,165'
		$labelCountdown.Name = 'labelCountdown'
		$labelCountdown.Size = '400,30'
		$labelCountdown.TabIndex = 5
		$labelCountdown.Text = '00:00:00'
		$labelCountdown.TextAlign = 'MiddleCenter'
		
		## Label Restart Later
		$buttonRestartLater.Anchor = 'Bottom,Left'
		$buttonRestartLater.Location = '20,216'
		$buttonRestartLater.Name = 'buttonRestartLater'
		$buttonRestartLater.Size = '159,23'
		$buttonRestartLater.TabIndex = 0
		$buttonRestartLater.Text = $configRestartPromptButtonRestartLater
		$buttonRestartLater.UseVisualStyleBackColor = $true
		$buttonRestartLater.add_Click($buttonRestartLater_Click)
		
		## Label Restart Now
		$buttonRestartNow.Anchor = 'Bottom,Right'
		$buttonRestartNow.Location = '265,216'
		$buttonRestartNow.Name = 'buttonRestartNow'
		$buttonRestartNow.Size = '159,23'
		$buttonRestartNow.TabIndex = 2
		$buttonRestartNow.Text = $configRestartPromptButtonRestartNow
		$buttonRestartNow.UseVisualStyleBackColor = $true
		$buttonRestartNow.add_Click($buttonRestartNow_Click)
		
		## Timer Countdown
		If (-not $NoCountdown) { $timerCountdown.add_Tick($timerCountdown_Tick) }
		
		##----------------------------------------------
		
		## Save the initial state of the form
		$InitialFormWindowState = $formRestart.WindowState
		# Init the OnLoad event to correct the initial state of the form
		$formRestart.add_Load($Form_StateCorrection_Load)
		# Clean up the control events
		$formRestart.add_FormClosed($Form_Cleanup_FormClosed)
		$formRestartClosing = [System.Windows.Forms.FormClosingEventHandler]{ $_.Cancel = $true }
		$formRestart.add_FormClosing($formRestartClosing)
		
		## If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously
		If ($deployAppScriptFriendlyName) {
			If ($NoCountdown) {
				Write-Log -Message "Invoking ${CmdletName} asynchronously with no countdown..." -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "Invoking ${CmdletName} asynchronously with a [$countDownSeconds] second countdown..." -Source ${CmdletName}
			}
			[string]$installRestartPromptParameters = ($installRestartPromptParameters.GetEnumerator() | ForEach-Object { If ($_.Value.GetType().Name -eq 'SwitchParameter') { "-$($_.Key):`$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Boolean') { "-$($_.Key) `$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Int32') { "-$($_.Key) $($_.Value)" } Else { "-$($_.Key) `"$($_.Value)`"" } }) -join ' '
			Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"$scriptPath`" -ReferringApplication `"$installName`" -ShowInstallationRestartPrompt $installRestartPromptParameters" -WindowStyle Hidden -ErrorAction 'SilentlyContinue'
		}
		Else {
			If ($NoCountdown) {
				Write-Log -Message 'Display restart prompt with no countdown.' -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "Display restart prompt with a [$countDownSeconds] second countdown." -Source ${CmdletName}
			}
			
			#  Show the Form
			Write-Output $formRestart.ShowDialog()
			$formRestart.Dispose()
			#  Activate the Window
			[System.Diagnostics.Process]$powershellProcess = Get-Process | Where-Object { $_.MainWindowTitle -match $installTitle }
			[Microsoft.VisualBasic.Interaction]::AppActivate($powershellProcess.ID)
		}
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
	Time in milliseconds to display the balloon tip. Default: 500.
.EXAMPLE
	Show-BalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'
.EXAMPLE
	Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0)]
		[ValidateNotNullOrEmpty()]
		[string]$BalloonTipText,
		[Parameter(Mandatory=$false,Position=1)]
		[ValidateNotNullorEmpty()]
		[string]$BalloonTipTitle = $installTitle,
		[Parameter(Mandatory=$false,Position=2)]
		[ValidateSet('Error','Info','None','Warning')]
		[System.Windows.Forms.ToolTipIcon]$BalloonTipIcon = 'Info',
		[Parameter(Mandatory=$false,Position=3)]
		[ValidateNotNullorEmpty()]
		[int32]$BalloonTipTime = 10000
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Skip balloon if in silent mode
		If (($deployModeSilent) -or (-not $configShowBalloonNotifications)) { Return }

		## If the current process is not interactive, re-launch the function with a user account
		If (-not $IsProcessUserInteractive) {
			Invoke-PSCommandAsUser -Command ([scriptblock]::Create("Show-BalloonTip -BalloonTipText '$BalloonTipText' -BalloonTipTitle '$BalloonTipTitle' -BalloonTipIcon '$BalloonTipIcon' -BalloonTipTime $BalloonTipTime"))
			Return
		}
		
		## Dispose of previous balloon
		If ($global:notifyIcon) { Try { $global:notifyIcon.Dispose() } Catch {} }
		
		## Get the calling function so we know when to display the exiting balloon tip notification in an asynchronous script
		Try {
			[string]$callingFunction = (Get-Variable -Name MyInvocation -Scope 1 -ErrorAction 'SilentlyContinue').Value.MyCommand.Name
		}
		Catch { }
		
		If ($callingFunction -eq 'Exit-Script') {
			Write-Log -Message "Display balloon tip notification asyhchronously with message [$BalloonTipText]" -Source ${CmdletName}
			## Create a script block to display the balloon notification in a new PowerShell process so that we can wait to cleanly dispose of the balloon tip without having to make the deployment script wait
			[scriptblock]$notifyIconScriptBlock = {
				Param (
					[Parameter(Mandatory=$true,Position=0)]
					[ValidateNotNullOrEmpty()]
					[string]$BalloonTipText,
					[Parameter(Mandatory=$false,Position=1)]
					[ValidateNotNullorEmpty()]
					[string]$BalloonTipTitle,
					[Parameter(Mandatory=$false,Position=2)]
					[ValidateSet('Error','Info','None','Warning')]
					$BalloonTipIcon, # Don't strongly type variable as System.Drawing; assembly not loaded yet in asynchronous scriptblock so will throw error
					[Parameter(Mandatory=$false,Position=3)]
					[ValidateNotNullorEmpty()]
					[int32]$BalloonTipTime,
					[Parameter(Mandatory=$false,Position=4)]
					[ValidateNotNullorEmpty()]
					[string]$AppDeployLogoIcon
				)
				
				## Load assembly containing class System.Windows.Forms and System.Drawing
				Add-Type -AssemblyName System.Windows.Forms -ErrorAction 'Stop'
				Add-Type -AssemblyName System.Drawing -ErrorAction 'Stop'
				
				[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
				$global:notifyIcon = New-Object -TypeName Windows.Forms.NotifyIcon -Property @{
					BalloonTipIcon = $BalloonTipIcon
					BalloonTipText = $BalloonTipText
					BalloonTipTitle = $BalloonTipTitle
					Icon = New-Object -TypeName System.Drawing.Icon -ArgumentList $AppDeployLogoIcon
					Text = -join $BalloonTipText[0..62]
					Visible = $true
				}
				
				## Display the balloon tip notification asynchronously
				$global:NotifyIcon.ShowBalloonTip($BalloonTipTime)
				
				## Keep the asynchronous PowerShell process running so that we can dispose of the balloon tip icon
				Start-Sleep -Milliseconds ($BalloonTipTime)
				$global:notifyIcon.Dispose()
			}
			
			## Invoke a separate PowerShell process passing the script block as a command and associated parameters to display the balloon tip notification asynchronously
			Try {
				Execute-Process -Path "$PSHOME\powershell.exe" -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & {$notifyIconScriptBlock} '$BalloonTipText' '$BalloonTipTitle' '$BalloonTipIcon' '$BalloonTipTime' '$AppDeployLogoIcon'" -NoWait -WindowStyle Hidden -CreateNoWindow
			}
			Catch { }
		}
		## Otherwise create the balloontip icon synchronously
		Else {
			Write-Log -Message "Display balloon tip notification with message [$BalloonTipText]" -Source ${CmdletName}
			[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
			$global:notifyIcon = New-Object -TypeName Windows.Forms.NotifyIcon -Property @{
				BalloonTipIcon = $BalloonTipIcon
				BalloonTipText = $BalloonTipText
				BalloonTipTitle = $BalloonTipTitle
				Icon = New-Object -TypeName System.Drawing.Icon -ArgumentList $AppDeployLogoIcon
				Text = -join $BalloonTipText[0..62]
				Visible = $true
			}
			
			## Display the balloon tip notification
			$global:NotifyIcon.ShowBalloonTip($BalloonTipTime)
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
	The location of the progress window. Default: just below top, centered.
.PARAMETER TopMost
	Specifies whether the progress window should be topmost. Default: $true.
.EXAMPLE
	Show-InstallationProgress
	Uses the default status message from the XML configuration file.
.EXAMPLE
	Show-InstallationProgress -StatusMessage 'Installation in Progress...'
.EXAMPLE
	Show-InstallationProgress -StatusMessage "Installation in Progress...`nThe installation may take 20 minutes to complete."
.EXAMPLE
	Show-InstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost $false
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$StatusMessage = $configProgressMessageInstall,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Default','BottomRight')]
		[string]$WindowLocation = 'Default',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$TopMost = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($deployModeSilent) { Return }

		## If the default progress message hasn't been overridden and the deployment type is uninstall, use the default uninstallation message
		If (($statusMessage -eq $configProgressMessageInstall) -and ($deploymentType -eq 'Uninstall')) {
			$StatusMessage = $configProgressMessageUninstall
		}
		
		If ($envHost.Name -match 'PowerGUI') {
			Write-Log -Message "$($envHost.Name) is not a supported host for WPF multithreading. Progress dialog with message [$statusMessage] will not be displayed." -Severity 2 -Source ${CmdletName}
			Return
		}
		
		## If the current process is not interactive, re-launch the function with a user account
		If (-not $IsProcessUserInteractive) {
			Invoke-PSCommandAsUser -NoWait -Command ([scriptblock]::Create("Show-InstallationProgress -StatusMessage '$StatusMessage' -WindowLocation '$WindowLocation' -TopMost `$$TopMost"))
			Return
		}
		
		## If a PowerShell window is already showing an installation progress message, then just updated the message
		If (Test-Path -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -PathType 'Leaf') {
			$StatusMessage | Export-Clixml -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -Force
			Return
		}
		Else {
			$StatusMessage | Export-Clixml -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -Force
			#  Notify user that the software installation has started
			$balloonText = "$deploymentTypeName $configBalloonTextStart"
			Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText $balloonText
		}
		
		## Check if the progress thread is running before invoking methods on it
		If ($global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -ne 'Running') {
			#  Create a synchronized hashtable to share objects between runspaces
			$global:ProgressSyncHash = [hashtable]::Synchronized(@{ })
			$global:ProgressSyncHash.StatusMessage = $statusMessage
			#  Create a new runspace for the progress bar
			$global:ProgressRunspace = [runspacefactory]::CreateRunspace()
			$global:ProgressRunspace.ApartmentState = 'STA'
			$global:ProgressRunspace.ThreadOptions = 'ReuseThread'
			$global:ProgressRunspace.Open()
			#  Add the sync hash to the runspace
			$global:ProgressRunspace.SessionStateProxy.SetVariable('progressSyncHash', $global:ProgressSyncHash)
			#  Add other variables from the parent thread required in the progress runspace
			$global:ProgressRunspace.SessionStateProxy.SetVariable('installTitle', $installTitle)
			$global:ProgressRunspace.SessionStateProxy.SetVariable('windowLocation', $windowLocation)
			$global:ProgressRunspace.SessionStateProxy.SetVariable('topMost', [string]$topMost)
			$global:ProgressRunspace.SessionStateProxy.SetVariable('appDeployLogoBanner', $appDeployLogoBanner)
			$global:ProgressRunspace.SessionStateProxy.SetVariable('statusMessage', $statusMessage)
			$global:ProgressRunspace.SessionStateProxy.SetVariable('AppDeployLogoIcon', $AppDeployLogoIcon)
			$global:ProgressRunspace.SessionStateProxy.SetVariable('dpiScale', $dpiScale)
			
			#  Add the script block to be executed in the progress runspace
			$powershell = [PowerShell]::Create()
			$powershell.Runspace = $global:ProgressRunspace
			$powershell.AddScript({
				[Xml.XmlDocument]$xamlProgress = @'
				<Window
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				x:Name="Window" Title=""
				MaxHeight="200" MinHeight="180" Height="180"
				MaxWidth="456" MinWidth="456" Width="456" Padding="0,0,0,0" Margin="0,0,0,0"
				WindowStartupLocation = "Manual"
				Top=""
				Left=""
				Topmost=""
				ResizeMode="NoResize"
				Icon=""
				ShowInTaskbar="True" >
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
				<Grid Background="#F0F0F0">
					<Grid.RowDefinitions>
						<RowDefinition Height="50"/>
						<RowDefinition Height="100"/>
					</Grid.RowDefinitions>
					<Grid.ColumnDefinitions>
						<ColumnDefinition Width="45"></ColumnDefinition>
						<ColumnDefinition Width="*"></ColumnDefinition>
					</Grid.ColumnDefinitions>
					<Image x:Name = "ProgressBanner" Grid.ColumnSpan="2" Margin="0,0,0,0" Source=""></Image>
					<TextBlock x:Name = "ProgressText" Grid.Row="1" Grid.Column="1" Margin="0,5,45,10" Text="" FontSize="15" FontFamily="Microsoft Sans Serif" HorizontalAlignment="Center" VerticalAlignment="Center" TextAlignment="Center" Padding="15" TextWrapping="Wrap"></TextBlock>
					<Ellipse x:Name = "ellipse" Grid.Row="1" Grid.Column="0" Margin="0,0,0,0" StrokeThickness="5" RenderTransformOrigin="0.5,0.5" Height="25" Width="25" HorizontalAlignment="Right" VerticalAlignment="Center">
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
								<GradientStop Color="#008000" Offset="1"/>
							</LinearGradientBrush>
						</Ellipse.Stroke>
					</Ellipse>
					</Grid>
				</Window>
'@
				## Set the configurable values using variables added to the runspace from the parent thread
				#  Calculate the position on the screen where the progress dialog should be placed
				$screen = [System.Windows.Forms.Screen]::PrimaryScreen
				$screenWorkingArea = $screen.WorkingArea
				[int32]$screenWidth = $screenWorkingArea | Select-Object -ExpandProperty Width
				[int32]$screenHeight = $screenWorkingArea | Select-Object -ExpandProperty Height
				#  Set the start position of the Window based on the screen size
				If ($windowLocation -eq 'BottomRight') {
					$xamlProgress.Window.Left = [string]($screenWidth - $xamlProgress.Window.Width - 10)
					$xamlProgress.Window.Top = [string]($screenHeight - $xamlProgress.Window.Height - 10)
				}
				#  Show the default location (Top center)
				Else {
					#  Center the progress window by calculating the center of the workable screen based on the width of the screen relative to the DPI scale minus half the width of the progress bar
					$xamlProgress.Window.Left = [string](($screenWidth / (2 * ($dpiscale / 100) )) - (($xamlProgress.Window.Width / 2)))
					$xamlProgress.Window.Top = [string]($screenHeight / 9.5)
				}
				$xamlProgress.Window.TopMost = $topMost
				$xamlProgress.Window.Icon = $AppDeployLogoIcon
				$xamlProgress.Window.Grid.Image.Source = $appDeployLogoBanner
				$xamlProgress.Window.Grid.TextBlock.Text = $statusMessage
				$xamlProgress.Window.Title = $installTitle
				#  Parse the XAML
				$progressReader = New-Object -TypeName System.Xml.XmlNodeReader -ArgumentList $xamlProgress
				$global:ProgressSyncHash.Window = [Windows.Markup.XamlReader]::Load($progressReader)
				$global:ProgressSyncHash.ProgressText = $global:ProgressSyncHash.Window.FindName('ProgressText')
				#  Add an action to the Window.Closing event handler to disable the close button
				$global:ProgressSyncHash.Window.Add_Closing({ $_.Cancel = $true })
				#  Allow the window to be dragged by clicking on it anywhere
				$global:ProgressSyncHash.Window.Add_MouseLeftButtonDown({ $global:ProgressSyncHash.Window.DragMove() })
				#  Add a tooltip
				$global:ProgressSyncHash.Window.ToolTip = $installTitle
				$global:ProgressSyncHash.Window.ShowDialog() | Out-Null
				$global:ProgressSyncHash.Error = $Error
			}) | Out-Null
			
			#  Invoke the progress runspace
			Write-Log -Message "Spin up progress dialog in a separate thread with message: [$statusMessage]" -Source ${CmdletName}
			$progressData = $powershell.BeginInvoke()
			#  Allow the thread to be spun up safely before invoking actions against it.
			Start-Sleep -Seconds 3
			#  Wait for the runspace to complete and update progress message
			While ($global:ProgressSyncHash.StatusMessage -ne '_CloseRunspace') {
				Try {
					#  Get the progress message
					If (Test-Path -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -PathType 'Leaf') {
						$global:ProgressSyncHash.StatusMessage = Import-Clixml -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -ErrorAction 'Stop'
					}
					
					If ($global:ProgressSyncHash.StatusMessage -eq '_CloseRunspace') { Break }
					
					#  Update the progress text
					$global:ProgressSyncHash.Window.Dispatcher.Invoke([System.Windows.Threading.DispatcherPriority]'Normal', [Windows.Input.InputEventHandler]{ $global:ProgressSyncHash.ProgressText.Text = $global:ProgressSyncHash.StatusMessage }, $null, $null)
					
					#  Allow time between updating the thread
					Start-Sleep -Seconds 1
				}
				Catch {
					Write-Log -Message "Unable to update the progress message. `n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
					Break
				}
			}
			
			#  Log any errors
			If ($global:ProgressSyncHash.Error) {
				Write-Log -Message "Failure while displaying progress dialog. `n$(Resolve-Error -ErrorRecord $global:ProgressSyncHash.Error)" -Severity 3 -Source ${CmdletName}
			}
			
			#  Clean up
			$global:ProgressSyncHash.Window.Close()
			$global:ProgressSyncHash.Window.Dispose()
			$powershell.Dispose()
			
			#  Cleanup file containing the progress message
			If (Test-Path -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -PathType 'Leaf') {
				Remove-Item -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -Force -ErrorAction 'SilentlyContinue' | Out-Null
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
.EXAMPLE
	Close-InstallationProgress
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -eq 'Running') {
			## Close the progress thread
			Write-Log -Message 'Close the installation progress dialog.' -Source ${CmdletName}
			$global:ProgressSyncHash.Window.Dispatcher.InvokeShutdown()
			$global:ProgressRunspace.Close()
			$global:ProgressSyncHash.Clear()
		}
		#  Cleanup file containing the progress message
		If (Test-Path -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -PathType 'Leaf') {
			Remove-Item -Path "$dirAppDeployTemp\StatusMsgFrom_ShowInstallProgress.xml" -Force -ErrorAction 'SilentlyContinue' | Out-Null
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
	Action to be performed. Options: 'PintoStartMenu','UnpinfromStartMenu','PintoTaskbar','UnpinfromTaskbar'.
.PARAMETER FilePath
	Path to the shortcut file to be pinned or unpinned.
.EXAMPLE
	Set-PinnedApplication -Action 'PintoStartMenu' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"
.EXAMPLE
	Set-PinnedApplication -Action 'UnpinfromTaskbar' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateSet('PintoStartMenu','UnpinfromStartMenu','PintoTaskbar','UnpinfromTaskbar')]
		[string]$Action,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$FilePath
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		#region Function Get-PinVerb
		Function Get-PinVerb {
			[CmdletBinding()]
			Param (
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				[int32]$VerbId
			)
			
			[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
			
			$GetPinVerbSource = @'
			using System;
			using System.Text;
			using System.Runtime.InteropServices;
			namespace Verb
			{
				public sealed class Load
				{
					[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
					public static extern int LoadString(IntPtr h, int id, StringBuilder sb, int maxBuffer);
					
					[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
					public static extern IntPtr LoadLibrary(string s);
					
					public static string PinVerb(int VerbId)
					{
						IntPtr hShell32 = LoadLibrary("shell32.dll");
						const int nChars  = 255;
						StringBuilder Buff = new StringBuilder("", nChars);
						
						LoadString(hShell32, VerbId, Buff, Buff.Capacity);
						return Buff.ToString();
					}
				}
			}
'@
			If (-not ([System.Management.Automation.PSTypeName]'Verb.Load').Type) {
				Add-Type -TypeDefinition $GetPinVerbSource -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
			}
			
			Write-Log -Message "Get localized pin verb for verb id [$VerbID]." -Source ${CmdletName}
			[string]$PinVerb = [Verb.Load]::PinVerb($VerbId)
			Write-Log -Message "Verb ID [$VerbID] has a localized pin verb of [$PinVerb]." -Source ${CmdletName}
			Write-Output $PinVerb
		}
		#endregion
		
		#region Function Invoke-Verb
		Function Invoke-Verb {
			[CmdletBinding()]
			Param (
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				[string]$FilePath,
				[Parameter(Mandatory=$true)]
				[ValidateNotNullorEmpty()]
				[string]$Verb
			)
			
			Try {
				[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
				$verb = $verb.Replace('&','')
				$path = Split-Path -Path $FilePath -Parent -ErrorAction 'Stop'
				$folder = $shellApp.Namespace($path)
				$item = $folder.ParseName((Split-Path -Path $FilePath -Leaf -ErrorAction 'Stop'))
				$itemVerb = $item.Verbs() | Where-Object { $_.Name.Replace('&','') -eq $verb } -ErrorAction 'Stop'
				
				If ($null -eq $itemVerb) {
					Write-Log -Message "Performing action [$verb] is not programatically supported for this file [$FilePath]." -Severity 2 -Source ${CmdletName}
				}
				Else {
					Write-Log -Message "Perform action [$verb] on [$FilePath]." -Source ${CmdletName}
					$itemVerb.DoIt()
				}
			}
			Catch {
				Write-Log -Message "Failed to perform action [$verb] on [$FilePath]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			}
		}
		#endregion
		
		[hashtable]$Verbs = @{
			'PintoStartMenu' = 5381
			'UnpinfromStartMenu' = 5382
			'PintoTaskbar' = 5386
			'UnpinfromTaskbar' = 5387
		}
	}
	Process {
		Try {
			Write-Log -Message "Execute action [$Action] for file [$FilePath]." -Source ${CmdletName}
			
			If (-not (Test-Path -Path $FilePath -PathType Leaf -ErrorAction 'Stop')) {
				Throw "Path [$filePath] does not exist."
			}
			
			If (-not ($Verbs.$Action)) {
				Throw "Action [$Action] not supported. Supported actions are [$($Verbs.Keys -join ', ')]."
			}
			
			[string]$PinVerbAction = Get-PinVerb -VerbId $Verbs.$Action
			If (-not ($PinVerbAction)) {
				Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
			}
			
			Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
		}
		Catch {
			Write-Log -Message "Failed to execute action [$Action]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Continue if an error is encountered.
.EXAMPLE
	Get-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$FilePath,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Section,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Key,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		$GetIniValueSource = @'
		using System;
		using System.Text;
		using System.Runtime.InteropServices;
		namespace IniFile
		{
			public sealed class GetValue
			{
				[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				public static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
				
				public static string GetIniValue(string section, string key, string filepath)
				{
					string sDefault    = "";
					const int  nChars  = 1024;
					StringBuilder Buff = new StringBuilder(nChars);
					
					GetPrivateProfileString(section, key, sDefault, Buff, Buff.Capacity, filepath);
					return Buff.ToString();
				}
			}
		}
'@
		If (-not ([System.Management.Automation.PSTypeName]'IniFile.GetValue').Type) {
			Add-Type -TypeDefinition $GetIniValueSource -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			Write-Log -Message "Read INI Key:  [Section = $Section] [Key = $Key]" -Source ${CmdletName}
			
			If (-not (Test-Path -Path $FilePath -PathType Leaf)) { Throw "File [$filePath] could not be found." }
			
			$IniValue = [IniFile.GetValue]::GetIniValue($Section, $Key, $FilePath)
			Write-Log -Message "INI Key Value: [Section = $Section] [Key = $Key] [Value = $IniValue]" -Source ${CmdletName}
			
			Write-Output $IniValue
		}
		Catch {
			Write-Log -Message "Failed to read INI file key value. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Continue if an error is encountered.
.EXAMPLE
	Set-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName' -Value 'MyFile.ID'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$FilePath,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Section,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Key,
		# Don't strongly type this variable as [string] b/c PowerShell replaces [string]$Value = $null with an empty string
		[Parameter(Mandatory=$true)]
		[AllowNull()]
		$Value,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		$SetIniValueSource = @'
		using System;
		using System.Text;
		using System.Runtime.InteropServices;
		namespace IniFile
		{
			public sealed class SetValue
			{
				[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				[return: MarshalAs(UnmanagedType.Bool)]
				public static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, StringBuilder lpString, string lpFileName);
				
				public static void SetIniValue(string section, string key, StringBuilder value, string filepath)
				{
					WritePrivateProfileString(section, key, value, filepath);
				}
			}
		}
'@
		If (-not ([System.Management.Automation.PSTypeName]'IniFile.SetValue').Type) {
			Add-Type -TypeDefinition $SetIniValueSource -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			Write-Log -Message "Write INI Key Value: [Section = $Section] [Key = $Key] [Value = $Value]" -Source ${CmdletName}
			
			If (-not (Test-Path -Path $FilePath -PathType Leaf)) { Throw "File [$filePath] could not be found." }
			
			[IniFile.SetValue]::SetIniValue($Section, $Key, ([System.Text.StringBuilder]$Value), $FilePath)
		}
		Catch {
			Write-Log -Message "Failed to write INI file key value. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Continue if an error is encountered.
.PARAMETER PassThru
	Get the file object, attach a property indicating the file binary type, and write to pipeline
.EXAMPLE
	Get-PEFileArchitecture -FilePath "$env:windir\notepad.exe"
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[ValidateScript({$_ | Test-Path -PathType Leaf})]
		[System.IO.FileInfo[]]$FilePath,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true,
		[Parameter(Mandatory=$false)]
		[switch]$PassThru
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		[string[]]$PEFileExtensions = '.exe', '.dll', '.ocx', '.drv', '.sys', '.scr', '.efi', '.cpl', '.fon'
		[int32]$MACHINE_OFFSET = 4
		[int32]$PE_POINTER_OFFSET = 60
	}
	Process {
		ForEach ($Path in $filePath) {
			Try {
				If ($PEFileExtensions -notcontains $Path.Extension) {
					Throw "Invalid file type. Please specify one of the following PE file types: $($PEFileExtensions -join ', ')"
				}
				
				[byte[]]$data = New-Object -TypeName System.Byte[] -ArgumentList 4096
				$stream = New-Object -TypeName System.IO.FileStream -ArgumentList ($Path.FullName, 'Open', 'Read')
				$stream.Read($data, 0, 4096) | Out-Null
				$stream.Flush()
				$stream.Close()
				
				[int32]$PE_HEADER_ADDR = [System.BitConverter]::ToInt32($data, $PE_POINTER_OFFSET)
				[uint16]$PE_IMAGE_FILE_HEADER = [System.BitConverter]::ToUInt16($data, $PE_HEADER_ADDR + $MACHINE_OFFSET)
				Switch ($PE_IMAGE_FILE_HEADER) {
					0 { $PEArchitecture = 'Native' } # The contents of this file are assumed to be applicable to any machine type
					0x014c { $PEArchitecture = '32BIT' } # File for Windows 32-bit systems
					0x0200 { $PEArchitecture = 'Itanium-x64' } # File for Intel Itanium x64 processor family
					0x8664 { $PEArchitecture = '64BIT' } # File for Windows 64-bit systems
					Default { $PEArchitecture = 'Unknown' }
				}
				Write-Log -Message "File [$($Path.FullName)] has a detected file architecture of [$PEArchitecture]." -Source ${CmdletName}
				
				If ($PassThru) {
					#  Get the file object, attach a property indicating the type, and write to pipeline
					Get-Item -Path $Path.FullName -Force | Add-Member -MemberType 'NoteProperty' -Name 'BinaryType' -Value $PEArchitecture -Force -PassThru | Write-Output
				}
				Else {
					Write-Output $PEArchitecture
				}
			}
			Catch {
				Write-Log -Message "Failed to get the PE file architecture. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Continue if an error is encountered.
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
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$FilePath,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Register','Unregister')]
		[string]$DLLAction,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		## Get name used to invoke this function in case the 'Register-DLL' or 'Unregister-DLL' alias was used and set the correct DLL action
		[string]${InvokedCmdletName} = $MyInvocation.InvocationName
		#  Set the correct register/unregister action based on the alias used to invoke this function
		If (${InvokedCmdletName} -ne ${CmdletName}) {
			Switch (${InvokedCmdletName}) {
				'Register-DLL' { [string]$DLLAction = 'Register' }
				'Unregister-DLL' { [string]$DLLAction = 'Unregister' }
			}
		}
		#  Set the correct DLL register/unregister action parameters
		If (-not $DLLAction) { Throw 'Parameter validation failed. Please specify the [-DLLAction] parameter to determine whether to register or unregister the DLL.' }
		[string]$DLLAction = (Get-Culture).TextInfo | ForEach-Object { $_.ToTitleCase($DLLAction.ToLower()) }
		Switch ($DLLAction) {
			'Register' { [string]$DLLActionParameters = "/s `"$FilePath`"" }
			'Unregister' { [string]$DLLActionParameters = "/s /u `"$FilePath`"" }
		}
	}
	Process {
		Try {
			Write-Log -Message "$DLLAction DLL file [$filePath]." -Source ${CmdletName}
			If (-not (Test-Path -Path $FilePath -PathType Leaf)) { Throw "File [$filePath] could not be found." }
			
			[string]$DLLFileBitness = Get-PEFileArchitecture -FilePath $filePath -ContinueOnError $false -ErrorAction 'Stop'
			If (($DLLFileBitness -ne '64BIT') -and ($DLLFileBitness -ne '32BIT')) {
				Throw "File [$filePath] has a detected file architecture of [$DLLFileBitness]. Only 32-bit or 64-bit DLL files can be $($DLLAction.ToLower() + 'ed')."
			}
			
			If ($Is64Bit) {
				If ($DLLFileBitness -eq '64BIT') {
					If ($Is64BitProcess) {
						[psobject]$ExecuteResult = Execute-Process -Path "$envWinDir\system32\regsvr32.exe" -Parameters $DLLActionParameters -WindowStyle Hidden -PassThru
					}
					Else {
						[psobject]$ExecuteResult = Execute-Process -Path "$envWinDir\sysnative\regsvr32.exe" -Parameters $DLLActionParameters -WindowStyle Hidden -PassThru
					}
				}
				ElseIf ($DLLFileBitness -eq '32BIT') {
					[psobject]$ExecuteResult = Execute-Process -Path "$envWinDir\SysWOW64\regsvr32.exe" -Parameters $DLLActionParameters -WindowStyle Hidden -PassThru
				}
			}
			Else {
				If ($DLLFileBitness -eq '64BIT') {
					Throw "File [$filePath] cannot be $($DLLAction.ToLower()) because it is a 64-bit file on a 32-bit operating system."
				}
				ElseIf ($DLLFileBitness -eq '32BIT') {
					[psobject]$ExecuteResult = Execute-Process -Path "$envWinDir\system32\regsvr32.exe" -Parameters $DLLActionParameters -WindowStyle Hidden -PassThru
				}
			}
			
			If ($ExecuteResult.ExitCode -ne 0) {
				If ($ExecuteResult.ExitCode -eq 999) {
					Throw "Execute-Process function failed with exit code [$($ExecuteResult.ExitCode)]."
				}
				Else {
					Throw "regsvr32.exe failed with exit code [$($ExecuteResult.ExitCode)]."
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to $($DLLAction.ToLower()) DLL file. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to $($DLLAction.ToLower()) DLL file: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
Set-Alias -Name 'Register-DLL' -Value 'Invoke-RegisterOrUnregisterDLL' -Scope Script -Force -ErrorAction 'SilentlyContinue'
Set-Alias -Name 'Unregister-DLL' -Value 'Invoke-RegisterOrUnregisterDLL' -Scope Script -Force -ErrorAction 'SilentlyContinue'
#endregion


#region Function Get-MsiTableProperty
Function Get-MsiTableProperty {
<#
.SYNOPSIS
	Get all of the properties from an MSI table and return as a custom object.
.DESCRIPTION
	Use the Windows Installer object to read all of the properties from a MSI table.
.PARAMETER Path
	The fully qualified path to an MSI file.
.PARAMETER Table
	The name of the the MSI table from which all of the properties must be retrieved. Default is: 'Property'.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi'
	Retrieve all of the properties from the default 'Property' table.
.EXAMPLE
	Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -Table 'Property' | Select-Object -ExpandProperty ProductCode
	Retrieve all of the properties from the 'Property' table and then pipe to Select-Object to select the ProductCode property.
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateScript({ $_ | Test-Path -PathType Leaf })]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$Table = 'Property',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		[scriptblock]$InvokeMethod = {
			Param (
				[__comobject]$Object,
				[string]$MethodName,
				[object[]]$ArgumentList
			)
			Write-Output $Object.GetType().InvokeMember($MethodName, [System.Reflection.BindingFlags]::InvokeMethod, $null, $Object, $ArgumentList, $null, $null, $null)
		}
		
		[scriptblock]$GetProperty = {
			Param (
				[__comobject]$Object,
				[string]$PropertyName,
				[object[]]$ArgumentList
			)
			Write-Output $Object.GetType().InvokeMember($PropertyName, [System.Reflection.BindingFlags]::GetProperty, $null, $Object, $ArgumentList, $null, $null, $null)
		}
	}
	Process {
		Try {
			Write-Log -Message "Get properties from MSI file [$Path] in table [$Table]" -Source ${CmdletName}
			
			## Create an empty object to store properties in
			[psobject]$TableProperties = New-Object -TypeName PSObject
			## Create a Windows Installer object
			[__comobject]$Installer = New-Object -ComObject WindowsInstaller.Installer -ErrorAction 'Stop'
			## Open MSI database in read only mode
			[int32]$OpenMSIReadOnly = 0
			[__comobject]$Database = &$InvokeMethod -Object $Installer -MethodName 'OpenDatabase' -ArgumentList @($Path, $OpenMSIReadOnly)
			## Open the "Property" table view
			[__comobject]$View = &$InvokeMethod -Object $Database -MethodName 'OpenView' -ArgumentList @("SELECT * FROM $Table")
			&$InvokeMethod -Object $View -MethodName 'Execute' | Out-Null
			
			## Retrieve the first row from the "Properties" table
			[__comobject]$Record = &$InvokeMethod -Object $View -MethodName 'Fetch'
			## If the first row was successfully retrieved, then save data and loop through the entire table
			While ($Record) {
				#  Add property and value to custom object
				$TableProperties | Add-Member -MemberType NoteProperty -Name (& $GetProperty -Object $Record -PropertyName 'StringData' -ArgumentList @(1)) -Value (& $GetProperty -Object $Record -PropertyName 'StringData' -ArgumentList @(2))
				#  Retrieve the next row in the table
				[__comobject]$Record = & $InvokeMethod -Object $View -MethodName 'Fetch'
			}
			
			Write-Output $TableProperties
		}
		Catch {
			Write-Log -Message "Failed to get the MSI table [$Table]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to get the MSI table [$Table]: $($_.Exception.Message)"
			}
		}
		Finally {
			If ($View) {
				& $InvokeMethod -Object $View -MethodName 'Close' -ArgumentList @() | Out-Null
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
.EXAMPLE
	Test-MSUpdates -KBNumber 'KB2549864'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0,HelpMessage='Enter the KB Number for the Microsoft Update')]
		[ValidateNotNullorEmpty()]
		[string]$KBNumber
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Write-Log -Message "Check if Microsoft Update [$kbNumber] is installed." -Source ${CmdletName}
		
		## Default is not found
		[boolean]$kbFound = $false
		
		## Check using Update method (to catch Office updates)
		[__comobject]$Session = New-Object -ComObject Microsoft.Update.Session
		[__comobject]$Collection = New-Object -ComObject Microsoft.Update.UpdateColl
		[__comobject]$Installer = $Session.CreateUpdateInstaller()
		[__comobject]$Searcher = $Session.CreateUpdateSearcher()
		[int32]$updateCount = $Searcher.GetTotalHistoryCount()
		If ($updateCount -gt 0) {
			$Searcher.QueryHistory(0, $updateCount) | Where-Object { $_.Title -match $kbNumber } | ForEach-Object { $kbFound = $true }
		}
		
		## Check using standard method
		If (-not $kbFound) {
			Get-Hotfix -Id $kbNumber -ErrorAction 'SilentlyContinue' | ForEach-Object { $kbFound = $true }
		}
		
		## Return Result
		If (-not $kbFound) {
			Write-Log -Message "Microsoft Update [$kbNumber] is not installed" -Source ${CmdletName}
			Write-Output $false
		}
		Else {
			Write-Log -Message "Microsoft Update [$kbNumber] is installed" -Source ${CmdletName}
			Write-Output $true
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
.EXAMPLE
	Install-MSUpdates -Directory "$dirFiles\MSUpdates"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Directory
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Write-Log -Message "Recursively install all Microsoft Updates in directory [$Directory]." -Source ${CmdletName}
		
		## KB Number pattern match
		$kbPattern = '(?i)kb\d{6,8}'
		
		## Get all hotfixes and install if required
		[System.IO.FileInfo[]]$files = Get-ChildItem -Path $Directory -Recurse -Include ('*.exe','*.msu','*.msp')
		ForEach ($file in $files) {
			If ($file.Name -match 'redist') {
				[version]$redistVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($file).ProductVersion
				[string]$redistDescription = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($file).FileDescription
				
				Write-Log -Message "Install [$redistDescription $redistVersion]..." -Source ${CmdletName}
				#  Handle older redistributables (ie, VC++ 2005)
				If ($redistDescription -match 'Win32 Cabinet Self-Extractor') {
					Execute-Process -Path $file -Parameters '/q' -WindowStyle Hidden -ContinueOnError $true
				}
				Else {
					Execute-Process -Path $file -Parameters '/quiet /norestart' -WindowStyle Hidden -ContinueOnError $true
				}
			}
			Else {
				#  Get the KB number of the file
				[string]$kbNumber = [regex]::Match($file, $kbPattern).ToString()
				If (-not $kbNumber) { Continue }
				
				#  Check to see whether the KB is already installed
				If (-not (Test-MSUpdates -KBNumber $kbNumber)) {
					Write-Log -Message "KB Number [$KBNumber] was not detected and will be installed." -Source ${CmdletName}
					Switch ($file.Extension) {
						#  Installation type for executables (i.e., Microsoft Office Updates)
						'.exe' { Execute-Process -Path $file -Parameters '/quiet /norestart' -WindowStyle Hidden -ContinueOnError $true }
						#  Installation type for Windows updates using Windows Update Standalone Installer
						'.msu' { Execute-Process -Path 'wusa.exe' -Parameters "`"$file`" /quiet /norestart" -WindowStyle Hidden -ContinueOnError $true }
						#  Installation type for Windows Installer Patch
						'.msp' { Execute-MSI -Action 'Patch' -Path $file -ContinueOnError $true }
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


#region Function Send-Keys
Function Send-Keys {
<#
.SYNOPSIS
	Send a sequence of keys to an application window.
.DESCRIPTION
	Send a sequence of keys to an application window.
.PARAMETER WindowTitle
	The title of the application window. This can be a partial title.
.PARAMETER Keys
	The sequence of keys to send.
.PARAMETER WaitSeconds
	An optional number of seconds to wait after the sending of the keys.
.EXAMPLE
	Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world'
	Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".
.EXAMPLE
	Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world' -WaitSeconds 5
	Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.
.NOTES
.LINK
	http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0)]
		[ValidateNotNullorEmpty()]
		[string]$WindowTitle,
		[Parameter(Mandatory=$true,Position=1)]
		[ValidateNotNullorEmpty()]
		[string]$Keys,
		[Parameter(Mandatory=$false,Position=2)]
		[ValidateNotNullorEmpty()]
		[int32]$WaitSeconds
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		## Load assembly containing class System.Windows.Forms.SendKeys
		Add-Type -AssemblyName System.Windows.Forms -ErrorAction 'Stop'
		
		$SetForegroundWindowSource = @'
			using System;
			using System.Runtime.InteropServices;
			public class GUIWindow
			{
				[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				[return: MarshalAs(UnmanagedType.Bool)]
				public static extern bool SetForegroundWindow(IntPtr hWnd);
			}
'@
		If (-not ([System.Management.Automation.PSTypeName]'GUIWindow').Type) {
			Add-Type -TypeDefinition $SetForegroundWindowSource -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			## Get the process with the specified window title
			[System.Diagnostics.Process[]]$Process = Get-Process -ErrorAction 'Stop' | Where-Object { $_.MainWindowTitle.Contains($WindowTitle) }
			If ($Process) {
				Write-Log -Message "Match window title found running under process [$($process.name)]..." -Source ${CmdletName}
				## Get the window handle of the first process only if there is more than one process returned
				[IntPtr]$ProcessHandle = $Process[0].MainWindowHandle
				
				Write-Log -Message 'Bring window to foreground.' -Source ${CmdletName}
				## Bring the process to the foreground
				[boolean]$ActivateWindow = [GUIWindow]::SetForegroundWindow($ProcessHandle)
				
				## Send the Key sequence
				#  Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx
				If ($ActivateWindow) {
					Write-Log -Message 'Send key(s) [$Keys] to window.' -Source ${CmdletName}
					[System.Windows.Forms.SendKeys]::SendWait($Keys)
				}
				Else {
					Write-Log -Message 'Failed to bring window to foreground.' -Source ${CmdletName}
					# Failed to bring the window to the foreground. Do nothing.
				}
				
				If ($WaitSeconds) { Start-Sleep -Seconds $WaitSeconds }
			}
		}
		Catch {
			Write-Log -Message "Failed to send keys to window [$WindowTitle]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	Tests whether the local machine is running on battery.
.DESCRIPTION
	Tests whether the local machine is running on battery and returns true/false.
.EXAMPLE
	Test-Battery
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		## PowerStatus class found in this assembly is more reliable than WMI in cases where the battery is failing.
		Add-Type -Assembly System.Windows.Forms -ErrorAction 'SilentlyContinue'
	}
	Process {
		Write-Log -Message 'Check if system is using AC power or if it is running on battery...' -Source ${CmdletName}
		
		[System.Windows.Forms.PowerStatus]$PowerStatus = [System.Windows.Forms.SystemInformation]::PowerStatus
		
		## Get the system power status. Indicates whether the system is using AC power or if the status is unknown. Possible values:
		#    Offline : The system is not using AC power.
		#    Online  : The system is using AC power.
		#    Unknown : The power status of the system is unknown.
		[string]$PowerLineStatus = $PowerStatus.PowerLineStatus
		
		## Get the current battery charge status. Possible values: High, Low, Critical, Charging, NoSystemBattery, Unknown.
		[string]$BatteryChargeStatus = $PowerStatus.BatteryChargeStatus
		
		## Get the approximate amount, from 0.00 to 1.0, of full battery charge remaining.
		#  This property can report 1.0 when the battery is damaged and Windows can't detect a battery.
		#  Therefore, this property is only indicative of battery charge remaining if 'BatteryChargeStatus' property is not reporting 'NoSystemBattery' or 'Unknown'.
		[single]$BatteryLifePercent = $PowerStatus.BatteryLifePercent
		If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
			[single]$BatteryLifePercent = 0.0
		}
		
		## The reported approximate number of seconds of battery life remaining. It will report –1 if the remaining life is unknown because the system is on AC power.
		[int32]$BatteryLifeRemaining = $PowerStatus.BatteryLifeRemaining
		
		## Get the manufacturer reported full charge lifetime of the primary battery power source in seconds.
		#  The reported number of seconds of battery life available when the battery is fully charged, or -1 if it is unknown.
		#  This will only be reported if the battery supports reporting this information. You will most likely get -1, indicating unknown.
		[int32]$BatteryFullLifetime = $PowerStatus.BatteryFullLifetime
		
		## Determine if the system is using AC power
		[boolean]$OnACPower = $false
		If ($PowerLineStatus -eq 'Online') {
			Write-Log -Message 'System is using AC power.' -Source ${CmdletName}
			$OnACPower = $true
		}
		ElseIf ($PowerLineStatus -eq 'Offline') {
			Write-Log -Message 'System is using battery power.' -Source ${CmdletName}
		}
		ElseIf ($PowerLineStatus -eq 'Unknown') {
			If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
				Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. This is most likely due to a damaged battery so we will report system is using AC power." -Source ${CmdletName}
				$OnACPower = $true
			}
			Else {
				Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. Therefore, we will report system is using battery power." -Source ${CmdletName}
			}
		}
		
		Write-Output $OnACPower
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
.EXAMPLE
	Test-NetworkConnection
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Write-Log -Message 'Check if system is using a wired network connection...' -Source ${CmdletName}
		
		[psobject[]]$networkConnected = Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { ($_.NetConnectionStatus -eq 2) -and ($_.NetConnectionID -match 'Local') -and ($_.NetConnectionID -notmatch 'Wireless') -and ($_.Name -notmatch 'Virtual') } -ErrorAction 'SilentlyContinue'
		[boolean]$onNetwork = $false
		If ($networkConnected) {
			Write-Log -Message 'Wired network connection found.' -Source ${CmdletName}
			[boolean]$onNetwork = $true
		}
		Else {
			Write-Log -Message 'Wired network connection not found.' -Source ${CmdletName}
		}
		
		Write-Output $onNetwork
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
	Tests whether PowerPoint is running in fullscreen slideshow mode.
.DESCRIPTION
	Tests whether PowerPoint is running in fullscreen slideshow mode to see if someone is presenting.
.EXAMPLE
	Test-PowerPoint
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		$FullScreenWindowSource = @'
		using System;
		using System.Text;
		using System.Text.RegularExpressions;
		using System.Runtime.InteropServices;
		namespace ScreenDetection
		{
			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int Left;
				public int Top;
				public int Right;
				public int Bottom;
			}
			
			public class FullScreen
			{
				[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				private static extern IntPtr GetForegroundWindow();
				
				[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				private static extern IntPtr GetDesktopWindow();
				
				[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				private static extern IntPtr GetShellWindow();
				
				[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				private static extern int GetWindowRect(IntPtr hWnd, out RECT rc);
				
				[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
				
				private static IntPtr desktopHandle;
				private static IntPtr shellHandle;
				
				public static bool IsFullScreenWindow(string fullScreenWindowTitle)
				{
					desktopHandle = GetDesktopWindow();
					shellHandle   = GetShellWindow();
					
					bool runningFullScreen = false;
					RECT appBounds;
					System.Drawing.Rectangle screenBounds;
					const int nChars = 256;
					StringBuilder Buff = new StringBuilder(nChars);
					string mainWindowTitle = "";
					IntPtr hWnd;
					hWnd = GetForegroundWindow();
					
					if (hWnd != null && !hWnd.Equals(IntPtr.Zero))
					{
						if (!(hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)))
						{
							if (GetWindowText(hWnd, Buff, nChars) > 0)
							{
								mainWindowTitle = Buff.ToString();
								//Console.WriteLine(mainWindowTitle);
							}
							
							// If the main window title contains the text being searched for, then check to see if the window is in fullscreen mode.
							Match match  = Regex.Match(mainWindowTitle, fullScreenWindowTitle, RegexOptions.IgnoreCase);
							if ((!string.IsNullOrEmpty(fullScreenWindowTitle)) && match.Success)
							{
								GetWindowRect(hWnd, out appBounds);
								screenBounds = System.Windows.Forms.Screen.FromHandle(hWnd).Bounds;
								if ((appBounds.Bottom - appBounds.Top) == screenBounds.Height && (appBounds.Right - appBounds.Left) == screenBounds.Width)
								{
									runningFullScreen = true;
								}
							}
						}
					}
					return runningFullScreen;
				}
			}
		}
'@
		If (-not ([System.Management.Automation.PSTypeName]'ScreenDetection.FullScreen').Type) {
			[string[]]$ReferencedAssemblies = 'System.Drawing', 'System.Windows.Forms'
			Add-Type -TypeDefinition $FullScreenWindowSource -ReferencedAssemblies $ReferencedAssemblies -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			Write-Log -Message 'Check if PowerPoint is in fullscreen slideshow mode...' -Source ${CmdletName}
			[boolean]$IsPowerPointFullScreen = $false
			If (Get-Process -Name 'POWERPNT' -ErrorAction 'SilentlyContinue') {
				Write-Log -Message 'PowerPoint application is running.' -Source ${CmdletName}
				
				#  Case insensitive match for "PowerPoint Slide Show" at start of window title using regex matching
				[boolean]$IsPowerPointFullScreen = [ScreenDetection.FullScreen]::IsFullScreenWindow('^PowerPoint Slide Show')
				
				Write-Log -Message "PowerPoint is running in fullscreen mode: $IsPowerPointFullScreen" -Source ${CmdletName}
			}
			Else {
				Write-Log -Message 'PowerPoint application is not running.' -Source ${CmdletName}
			}
			
			Write-Output $IsPowerPointFullScreen
		}
		Catch {
			Write-Log -Message "Failed check to see if PowerPoint is running in fullscreen slideshow mode. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			Write-Output $false
		}
	}
	End {
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
	Schedule Id.
.PARAMETER ContinueOnError
	Continue if an error is encountered.
.EXAMPLE
	Invoke-SCCMTask 'SoftwareUpdatesScan'
.EXAMPLE
	Invoke-SCCMTask
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateSet('HardwareInventory','SoftwareInventory','HeartbeatDiscovery','SoftwareInventoryFileCollection','RequestMachinePolicy','EvaluateMachinePolicy','LocationServicesCleanup','SoftwareMeteringReport','SourceUpdate','PolicyAgentCleanup','RequestMachinePolicy2','CertificateMaintenance','PeerDistributionPointStatus','PeerDistributionPointProvisioning','ComplianceIntervalEnforcement','SoftwareUpdatesAgentAssignmentEvaluation','UploadStateMessage','StateMessageManager','SoftwareUpdatesScan','AMTProvisionCycle')]
		[string]$ScheduleID,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		[hashtable]$ScheduleIds = @{
			HardwareInventory = '{00000000-0000-0000-0000-000000000001}'; # Hardware Inventory Collection Task
			SoftwareInventory = '{00000000-0000-0000-0000-000000000002}'; # Software Inventory Collection Task
			HeartbeatDiscovery = '{00000000-0000-0000-0000-000000000003}'; # Heartbeat Discovery Cycle
			SoftwareInventoryFileCollection = '{00000000-0000-0000-0000-000000000010}'; # Software Inventory File Collection Task
			RequestMachinePolicy = '{00000000-0000-0000-0000-000000000021}'; # Request Machine Policy Assignments
			EvaluateMachinePolicy = '{00000000-0000-0000-0000-000000000022}'; # Evaluate Machine Policy Assignments
			RefreshDefaultMp = '{00000000-0000-0000-0000-000000000023}'; # Refresh Default MP Task
			RefreshLocationServices = '{00000000-0000-0000-0000-000000000024}'; # Refresh Location Services Task
			LocationServicesCleanup = '{00000000-0000-0000-0000-000000000025}'; # Location Services Cleanup Task
			SoftwareMeteringReport = '{00000000-0000-0000-0000-000000000031}'; # Software Metering Report Cycle
			SourceUpdate = '{00000000-0000-0000-0000-000000000032}'; # Source Update Manage Update Cycle
			PolicyAgentCleanup = '{00000000-0000-0000-0000-000000000040}'; # Policy Agent Cleanup Cycle
			RequestMachinePolicy2 = '{00000000-0000-0000-0000-000000000042}'; # Request Machine Policy Assignments
			CertificateMaintenance = '{00000000-0000-0000-0000-000000000051}'; # Certificate Maintenance Cycle
			PeerDistributionPointStatus = '{00000000-0000-0000-0000-000000000061}'; # Peer Distribution Point Status Task
			PeerDistributionPointProvisioning = '{00000000-0000-0000-0000-000000000062}'; # Peer Distribution Point Provisioning Status Task
			ComplianceIntervalEnforcement = '{00000000-0000-0000-0000-000000000071}'; # Compliance Interval Enforcement
			SoftwareUpdatesAgentAssignmentEvaluation = '{00000000-0000-0000-0000-000000000108}'; # Software Updates Agent Assignment Evaluation Cycle
			UploadStateMessage = '{00000000-0000-0000-0000-000000000111}'; # Send Unsent State Messages
			StateMessageManager = '{00000000-0000-0000-0000-000000000112}'; # State Message Manager Task
			SoftwareUpdatesScan = '{00000000-0000-0000-0000-000000000113}'; # Force Update Scan
			AMTProvisionCycle = '{00000000-0000-0000-0000-000000000120}'; # AMT Provision Cycle
		}
	}
	Process {
		Write-Log -Message "Invoke SCCM Schedule Task ID [$ScheduleId]..." -Source ${CmdletName}
		
		## Trigger SCCM task
		Try {
			[System.Management.ManagementClass]$SmsClient = [WMIClass]'ROOT\CCM:SMS_Client'
			$SmsClient.TriggerSchedule($ScheduleIds.$ScheduleID) | Out-Null
		}
		Catch {
			Write-Log -Message "Failed to trigger SCCM Schedule Task ID [$($ScheduleIds.$ScheduleId)]. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
	This function can take several minutes to run.
.PARAMETER ContinueOnError
	Continue if an error is encountered.
.EXAMPLE
	Install-SCCMSoftwareUpdates
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Scan for updates
		Write-Log -Message 'Scan for pending SCCM software updates...' -Source ${CmdletName}
		Invoke-SCCMTask -ScheduleId 'SoftwareUpdatesScan'
		
		Write-Log -Message 'Sleep for 180 seconds...' -Source ${CmdletName}
		Start-Sleep -Seconds 180
		
		Write-Log -Message 'Install pending software updates...' -Source ${CmdletName}
		Try {
			[System.Management.ManagementClass]$SmsSoftwareUpdates = [WMIClass]'ROOT\CCM:SMS_Client'
			$SmsSoftwareUpdates.InstallUpdates([System.Management.ManagementObject[]](Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Query 'SELECT * FROM CCM_SoftwareUpdate' -ErrorAction 'Stop')) | Out-Null
		}
		Catch {
			Write-Log -Message "Failed to trigger installation of pending SCCM software updates. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to trigger installation of pending SCCM software updates: $($_.Exception.Message)"
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
	Continue if an error is encountered.
.EXAMPLE
	Update-GroupPolicy
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		[string[]]$GPUpdateCmds = '/C echo N | gpupdate.exe /Target:Computer /Force', '/C echo N | gpupdate.exe /Target:User /Force'
		[int32]$InstallCount = 0
		ForEach ($GPUpdateCmd in $GPUpdateCmds) {
			Try {
				If ($InstallCount -eq 0) {
					[string]$InstallMsg = 'Update Group Policies for the Machine'
					Write-Log -Message $InstallMsg -Source ${CmdletName}
				}
				Else {
					[string]$InstallMsg = 'Update Group Policies for the User'
					Write-Log -Message $InstallMsg -Source ${CmdletName}
				}
				[psobject]$ExecuteResult = Execute-Process -Path "$envWindir\system32\cmd.exe" -Parameters $GPUpdateCmd -WindowStyle Hidden -PassThru
				
				If ($ExecuteResult.ExitCode -ne 0) {
					If ($ExecuteResult.ExitCode -eq 999) {
						Throw "Execute-Process function failed with exit code [$($ExecuteResult.ExitCode)]."
					}
					Else {
						Throw "gpupdate.exe failed with exit code [$($ExecuteResult.ExitCode)]."
					}
				}
				$InstallCount++
			}
			Catch {
				Write-Log -Message "Failed to $($InstallMsg). `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to $($InstallMsg): $($_.Exception.Message)"
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
	Continue if an error is encountered.
.EXAMPLE
	Enable-TerminalServerInstall
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Change terminal server into user install mode...' -Source ${CmdletName}
			$terminalServerResult = change.exe User /Install
			
			If ($global:LastExitCode -ne 0) { Throw $terminalServerResult }
		}
		Catch {
			Write-Log -Message "Failed to change terminal server into user install mode. `n$(Resolve-Error) " -Severity 3 -Source ${CmdletName}
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
	Continue if an error is encountered.
.EXAMPLE
	Enable-TerminalServerInstall
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Change terminal server into user execute mode...' -Source ${CmdletName}
			$terminalServerResult = change.exe User /Execute
			
			If ($global:LastExitCode -ne 0) { Throw $terminalServerResult }
		}
		Catch {
			Write-Log -Message "Failed to change terminal server into user execute mode. `n$(Resolve-Error) " -Severity 3 -Source ${CmdletName}
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
	- Executes the StubPath file for the current user as long as not in Session 0 (no need to logout/login to trigger Active Setup).
.PARAMETER StubExePath
	Full destination path to the file that will be executed for each user that logs in.
	If this file exists in the 'Files' subdirectory of the script directory, it will be copied to the destination path.
.PARAMETER Arguments
	Arguments to pass to the file being executed.
.PARAMETER Description
	Description for the Active Setup. Users will see "Setting up personalised settings for: $Description" at logon. Default is: $installName.
.PARAMETER Key
	Name of the registry key for the Active Setup entry. Default is: $installName.
.PARAMETER Version
	Optional. Specify version for Active setup entry. Active Setup is not triggered if Version value has more than 8 consecutive digits. Use commas to get around this limitation.
.PARAMETER Locale
	Optional. Arbitrary string used to specify the installation language of the file being executed. Not replicated to HKCU.
.PARAMETER PurgeActiveSetupKey
	Remove Active Setup entry from HKLM registry hive. Will also load each logon user's HKCU registry hive to remove Active Setup entry.
.PARAMETER DisableActiveSetup
	Disables the Active Setup entry so that the StubPath file will not be executed.
.PARAMETER ContinueOnError
	Continue if an error is encountered.
.EXAMPLE
	Set-ActiveSetup -StubExePath 'C:\Users\Public\Company\ProgramUserConfig.vbs' -Arguments '/Silent' -Description 'Program User Config' -Key 'ProgramUserConfig' -Locale 'en'
.EXAMPLE
	Set-ActiveSetup -StubExePath 'C:\Program Files\MyApp\MyApp_v1r1_HKCU.exe'
.NOTES
	Original code borrowed from: Denis St-Pierre (Ottawa, Canada), Todd MacNaught (Ottawa, Canada)
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param(
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$StubExePath,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Arguments,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Description = $installName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Key = $installName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Version = ((Get-Date -Format 'yyMM,ddHH,mmss').ToString()), # Ex: 1405,1515,0522
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Locale,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[switch]$DisableActiveSetup = $false,
		[Parameter(Mandatory=$false)]
		[switch]$PurgeActiveSetupKey,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			[string]$ActiveSetupKey = "HKLM:SOFTWARE\Microsoft\Active Setup\Installed Components\$Key"
			[string]$HKCUActiveSetupKey = "HKCU:Software\Microsoft\Active Setup\Installed Components\$Key"
			
			## Delete Active Setup registry entry from the HKLM hive and for all logon user registry hives on the system
			If ($PurgeActiveSetupKey) {
				Write-Log -Message "Remove Active Setup entry [$ActiveSetupKey]." -Source ${CmdletName}
				Remove-RegistryKey -Key $ActiveSetupKey
				
				Write-Log -Message "Remove Active Setup entry [$HKCUActiveSetupKey] for all log on user registry hives on the system." -Source ${CmdletName}
				[scriptblock]$RemoveHKCUActiveSetupKey = { Remove-RegistryKey -Key $HKCUActiveSetupKey -SID $UserProfile.SID }
				Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $RemoveHKCUActiveSetupKey -UserProfiles (Get-UserProfiles -ExcludeDefaultUser)
				Return
			}
			
			## Verify a file with a supported file extension was specified in $StubExePath
			[string[]]$StubExePathFileExtensions = '.exe', '.vbs', '.cmd', '.ps1', '.js'
			[string]$StubExeExt = [System.IO.Path]::GetExtension($StubExePath)
			If ($StubExePathFileExtensions -notcontains $StubExeExt) {
				Throw "Unsupported Active Setup StubPath file extension [$StubExeExt]."
			}
			
			## Copy file to $StubExePath from the 'Files' subdirectory of the script directory (if it exists there)
			[string]$StubExePath = [Environment]::ExpandEnvironmentVariables($StubExePath)
			[string]$ActiveSetupFileName = [System.IO.Path]::GetFileName($StubExePath)
			[string]$StubExeFile = Join-Path -Path $dirFiles -ChildPath $ActiveSetupFileName
			If (Test-Path -Path $StubExeFile -PathType Leaf) {
				#  This will overwrite the StubPath file if $StubExePath already exists on target
				Copy-File -Path $StubExeFile -Destination $StubExePath -ContinueOnError $false
			}
			
			## Check if the $StubExePath file exists
			If (-not (Test-Path -Path $StubExePath -PathType Leaf)) { Throw "Active Setup StubPath file [$ActiveSetupFileName] is missing." }
			
			## Define Active Setup StubPath according to file extension of $StubExePath
			Switch ($StubExeExt) {
				'.exe' {
					[string]$CUStubExePath = $StubExePath
					[string]$CUArguments = $Arguments
					[string]$StubPath = "$CUStubExePath"
				}
				{'.vbs','.js' -contains $StubExeExt} {
					[string]$CUStubExePath = "$envWinDir\system32\cscript.exe"
					[string]$CUArguments = "//nologo `"$StubExePath`""
					[string]$StubPath = "$CUStubExePath $CUArguments"
				}
				'.cmd' {
					[string]$CUStubExePath = "$envWinDir\system32\CMD.exe"
					[string]$CUArguments = "/C `"$StubExePath`""
					[string]$StubPath = "$CUStubExePath $CUArguments"
				}
				'.ps1' {
					[string]$CUStubExePath = "$PSHOME\powershell.exe"
					[string]$CUArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"$StubExePath`""
					[string]$StubPath = "$CUStubExePath $CUArguments"
				}
			}
			If ($Arguments) {
				[string]$StubPath = "$StubPath $Arguments"
				If ($StubExeExt -ne '.exe') { [string]$CUArguments = "$CUArguments $Arguments" }
			}
			
			## Create the Active Setup entry in the registry
			Set-RegistryKey -Key $ActiveSetupKey -Name '(Default)' -Value $Description -ContinueOnError $false
			Set-RegistryKey -Key $ActiveSetupKey -Name 'StubPath' -Value $StubPath -Type 'ExpandString' -ContinueOnError $false
			Set-RegistryKey -Key $ActiveSetupKey -Name 'Version' -Value $Version -ContinueOnError $false
			If ($Locale) { Set-RegistryKey -Key $ActiveSetupKey -Name 'Locale' -Value $Locale -ContinueOnError $false }
			If ($DisableActiveSetup) {
				Set-RegistryKey -Key $ActiveSetupKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -ContinueOnError $false
			}
			Else {
				Set-RegistryKey -Key $ActiveSetupKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -ContinueOnError $false
			}
			
			## Execute the StubPath file for the current user as long as not in Session 0
			If ($SessionZero) {
				Write-Log -Message 'Session 0 detected: Will not execute Active Setup StubPath file. Users will have to log off and log back into their account to execute Active Setup entry.' -Source ${CmdletName}
			}
			Else {
				Write-Log -Message 'Execute Active Setup StubPath file for the current user.' -Source ${CmdletName}
				If ($CUArguments) {
					$ExecuteResults = Execute-Process -FilePath $CUStubExePath -Arguments $CUArguments -PassThru
				}
				Else {
					$ExecuteResults = Execute-Process -FilePath $CUStubExePath -PassThru
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to set Active Setup registry entry. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
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
.PARAMETER ComputerName
	Specify the name of the computer. Default is: the local computer.
.PARAMETER PassThru
	Return the WMI service object.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Test-ServiceExists -Name 'wuauserv'
.EXAMPLE
	Test-ServiceExists -Name 'testservice' -PassThru | Where-Object { $_ } | ForEach-Object { $_.Delete() }
	Check if a service exists and then delete it by using the -PassThru parameter.
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$ComputerName = $env:ComputerName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$PassThru,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	Begin {
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			$ServiceObject = Get-WmiObject -ComputerName $ComputerName -Class Win32_Service -Filter "Name='$Name'" -ErrorAction 'Stop'
			If ($ServiceObject) {
				Write-Log -Message "Service [$Name] exists" -Source ${CmdletName}
				If ($PassThru) { Write-Output $ServiceObject } Else { Write-Output $true }
			}
			Else {
				Write-Log -Message "Service [$Name] does not exist" -Source ${CmdletName}
				If ($PassThru) { Write-Output $ServiceObject } Else { Write-Output $false }
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
.PARAMETER PassThru
	Return the System.ServiceProcess.ServiceController service object.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Stop-ServiceAndDependencies -Name 'wuauserv'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$ComputerName = $env:ComputerName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$SkipServiceExistsTest,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$SkipDependentServices,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$PassThru,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	Begin {
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## Check to see if the service exists
			If ((-not $SkipServiceExistsTest) -and (-not (Test-ServiceExists -ComputerName $ComputerName -Name $Name -ContinueOnError $false))) {
				Write-Log -Message "Service [$Name] does not exist" -Source ${CmdletName} -Severity 2
				Throw "Service [$Name] does not exist."
			}
			
			## Get the service object
			Write-Log -Message "Get the service object for service [$Name]" -Source ${CmdletName}
			[System.ServiceProcess.ServiceController]$Service = Get-Service -ComputerName $ComputerName -Name $Name -ErrorAction 'Stop'
			## Wait up to 60 seconds if service is in a pending state
			[string[]]$PendingStatus = 'ContinuePending', 'PausePending', 'StartPending', 'StopPending'
			If ($PendingStatus -contains $Service.Status) {
				Switch ($Service.Status) {
					{'ContinuePending'} { $DesiredStatus = 'Running' }
					{'PausePending'} { $DesiredStatus = 'Paused' }
					{'StartPending'} { $DesiredStatus = 'Running' }
					{'StopPending'} { $DesiredStatus = 'Stopped' }
				}
				[timespan]$WaitForStatusTime = New-TimeSpan -Seconds 60
				Write-Log -Message "Waiting for up to [$($WaitForStatusTime.TotalSeconds)] seconds to allow service pending status [$($Service.Status)] to reach desired status [$DesiredStatus]." -Source ${CmdletName}
				$Service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]$DesiredStatus, $WaitForStatusTime)
				$Service.Refresh()
			}
			## Discover if the service is currently running
			Write-Log -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]" -Source ${CmdletName}
			If ($Service.Status -ne 'Stopped') {
				#  Discover all dependent services that are running and stop them
				If (-not $SkipDependentServices) {
					Write-Log -Message "Discover all dependent service(s) for service [$Name] which are not 'Stopped'." -Source ${CmdletName}
					[System.ServiceProcess.ServiceController[]]$DependentServices = Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -DependentServices -ErrorAction 'Stop' | Where-Object { $_.Status -ne 'Stopped' }
					If ($DependentServices) {
						ForEach ($DependentService in $DependentServices) {
							Write-Log -Message "Stop dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]." -Source ${CmdletName}
							Try {
								Stop-Service -InputObject (Get-Service -ComputerName $ComputerName -Name $DependentService.ServiceName -ErrorAction 'Stop') -Force -WarningAction 'SilentlyContinue' -ErrorAction 'Stop'
							}
							Catch {
								Write-Log -Message "Failed to start dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]. Continue..." -Severity 2 -Source ${CmdletName}
								Continue
							}
						}
					}
					Else {
						Write-Log -Message "Dependent service(s) were not discovered for service [$Name]" -Source ${CmdletName}
					}
				}
				#  Stop the parent service
				Write-Log -Message "Stop parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]" -Source ${CmdletName}
				[System.ServiceProcess.ServiceController]$Service = Stop-Service -InputObject (Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -ErrorAction 'Stop') -Force -PassThru -WarningAction 'SilentlyContinue' -ErrorAction 'Stop'
			}
		}
		Catch {
			Write-Log -Message "Failed to stop the service [$Name]. `n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
			If (-not $ContinueOnError) {
				Throw "Failed to stop the service [$Name]: $($_.Exception.Message)"
			}
		}
		Finally {
			#  Return the service object if option selected
			If ($PassThru -and $Service) { Write-Output $Service }
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
.PARAMETER PassThru
	Return the System.ServiceProcess.ServiceController service object.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Start-ServiceAndDependencies -Name 'wuauserv'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$ComputerName = $env:ComputerName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$SkipServiceExistsTest,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$SkipDependentServices,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$PassThru,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	Begin {
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## Check to see if the service exists
			If ((-not $SkipServiceExistsTest) -and (-not (Test-ServiceExists -ComputerName $ComputerName -Name $Name -ContinueOnError $false))) {
				Write-Log -Message "Service [$Name] does not exist" -Source ${CmdletName} -Severity 2
				Throw "Service [$Name] does not exist."
			}
			
			## Get the service object
			Write-Log -Message "Get the service object for service [$Name]" -Source ${CmdletName}
			[System.ServiceProcess.ServiceController]$Service = Get-Service -ComputerName $ComputerName -Name $Name -ErrorAction 'Stop'
			## Wait up to 60 seconds if service is in a pending state
			[string[]]$PendingStatus = 'ContinuePending', 'PausePending', 'StartPending', 'StopPending'
			If ($PendingStatus -contains $Service.Status) {
				Switch ($Service.Status) {
					'ContinuePending' { $DesiredStatus = 'Running' }
					'PausePending' { $DesiredStatus = 'Paused' }
					'StartPending' { $DesiredStatus = 'Running' }
					'StopPending' { $DesiredStatus = 'Stopped' }
				}
				[timespan]$WaitForStatusTime = New-TimeSpan -Seconds 60
				Write-Log -Message "Waiting for up to [$($WaitForStatusTime.TotalSeconds)] seconds to allow service pending status [$($Service.Status)] to reach desired status [$DesiredStatus]." -Source ${CmdletName}
				$Service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]$DesiredStatus, $WaitForStatusTime)
				$Service.Refresh()
			}
			## Discover if the service is currently stopped
			Write-Log -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]" -Source ${CmdletName}
			If ($Service.Status -ne 'Running') {
				#  Start the parent service
				Write-Log -Message "Start parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]" -Source ${CmdletName}
				[System.ServiceProcess.ServiceController]$Service = Start-Service -InputObject (Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -ErrorAction 'Stop') -PassThru -WarningAction 'SilentlyContinue' -ErrorAction 'Stop'
				
				#  Discover all dependent services that are stopped and start them
				If (-not $SkipDependentServices) {
					Write-Log -Message "Discover all dependent service(s) for service [$Name] which are not 'Running'." -Source ${CmdletName}
					[System.ServiceProcess.ServiceController[]]$DependentServices = Get-Service -ComputerName $ComputerName -Name $Service.ServiceName -DependentServices -ErrorAction 'Stop' | Where-Object { $_.Status -ne 'Running' }
					If ($DependentServices) {
						ForEach ($DependentService in $DependentServices) {
							Write-Log -Message "Start dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]." -Source ${CmdletName}
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
						Write-Log -Message "Dependent service(s) were not discovered for service [$Name]" -Source ${CmdletName}
					}
				}
			}
		}
		Catch {
			Write-Log -Message "Failed to start the service [$Name]. `n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
			If (-not $ContinueOnError) {
				Throw "Failed to start the service [$Name]: $($_.Exception.Message)"
			}
		}
		Finally {
			#  Return the service object if option selected
			If ($PassThru -and $Service) { Write-Output $Service }
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion


#region Function Get-ServiceStartMode
Function Get-ServiceStartMode
{
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
.EXAMPLE
	Get-ServiceStartMode -Name 'wuauserv'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdLetBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$ComputerName = $env:ComputerName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	Begin {
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Get the service [$Name] startup mode." -Source ${CmdletName}
			[string]$ServiceStartMode = (Get-WmiObject -ComputerName $ComputerName -Class 'Win32_Service' -Filter "Name='$Name'" -Property 'StartMode' -ErrorAction 'Stop').StartMode
			## If service start mode is set to 'Auto', change value to 'Automatic' to be consistent with 'Set-ServiceStartMode' function
			If ($ServiceStartMode -eq 'Auto') { $ServiceStartMode = 'Automatic'}
			
			## If on Windows Vista or higher, check to see if service is set to Automatic (Delayed Start)
			If (($ServiceStartMode -eq 'Automatic') -and ([System.Environment]::OSVersion.Version.Major -gt 5)) {
				Try {
					[string]$ServiceRegistryPath = "HKLM:SYSTEM\CurrentControlSet\Services\$Name"
					[int32]$DelayedAutoStart = Get-ItemProperty -Path $ServiceRegistryPath -ErrorAction 'SilentlyContinue' | Select-Object -ExpandProperty 'DelayedAutoStart' -ErrorAction 'Stop'
					If ($DelayedAutoStart -eq 1) { $ServiceStartMode = 'Automatic (Delayed Start)' }
				}
				Catch { }
			}
			
			Write-Log -Message "Service [$Name] startup mode is set to [$ServiceStartMode]" -Source ${CmdletName}
			Write-Output $ServiceStartMode
		}
		Catch {
			Write-Log -Message "Failed to get the service [$Name] startup mode. `n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
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
Function Set-ServiceStartMode
{
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
.EXAMPLE
	Set-ServiceStartMode -Name 'wuauserv' -StartMode 'Automatic (Delayed Start)'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdLetBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullOrEmpty()]
		[string]$Name,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string]$ComputerName = $env:ComputerName,
		[Parameter(Mandatory=$true)]
		[ValidateSet('Automatic','Automatic (Delayed Start)','Manual','Disabled','Boot','System')]
		[string]$StartMode,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)
	Begin {
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## If on lower than Windows Vista and 'Automatic (Delayed Start)' selected, then change to 'Automatic' because 'Delayed Start' is not supported.
			If (($StartMode -eq 'Automatic (Delayed Start)') -and ([System.Environment]::OSVersion.Version.Major -lt 6)) { $StartMode = 'Automatic' }
			
			Write-Log -Message "Set service [$Name] startup mode to [$StartMode]" -Source ${CmdletName}
			If ($StartMode -eq 'Automatic (Delayed Start)') {
				$ChangeStartMode = & sc.exe config $Name start= delayed-auto
				If ($global:LastExitCode -ne 0) {
					Throw "sc.exe failed with exit code [$($global:LastExitCode)] and message [$ChangeStartMode]."
				}
			}
			Else {
				$ChangeStartMode = (Get-WmiObject -ComputerName $ComputerName -Class Win32_Service -Filter "Name='$Name'" -ErrorAction 'Stop').ChangeStartMode($StartMode)
				If($ChangeStartMode.ReturnValue -ne 0) {
					Throw "The 'ChangeStartMode' method of the 'Win32_Service' WMI class failed with a return value of [$($ChangeStartMode.ReturnValue)]."
				}
			}
			Write-Log -Message "Successfully set service [$Name] startup mode to [$StartMode]" -Source ${CmdletName}
		}
		Catch {
			Write-Log -Message "Failed to set service [$Name] startup mode to [$StartMode]. `n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
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
	 NTAccount, UserName, DomainName, SessionId, SessionName, ConnectState, IsCurrentSession, IsConsoleSession, IsUserSession,
	 IsLocalAdmin, LogonTime, IdleTime, DisconnectTime, ClientName, ClientProtocolType, ClientDirectory, ClientBuildNumber
.PARAMETER SkipIsLocalAdminCheck
	Skip check to see if user is a local admin. IsLocalAdmin property will be empty for all users.
.EXAMPLE
	Get-LoggedOnUser
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$SkipIsLocalAdminCheck = $false
	)
	
	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
		
		$QueryUserSessionSource = @'
		using System;
		using System.Collections.Generic;
		using System.Text;
		using System.Runtime.InteropServices;
		using System.ComponentModel;
		using FILETIME=System.Runtime.InteropServices.ComTypes.FILETIME;
		namespace QueryUser
		{
			public class Session
			{
				[DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				public static extern IntPtr WTSOpenServer(string pServerName);
				[DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				public static extern void WTSCloseServer(IntPtr hServer);
				[DllImport("wtsapi32.dll", CharSet = CharSet.Ansi, SetLastError = false)]
				public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr pBuffer, out int pBytesReturned);
				[DllImport("wtsapi32.dll", CharSet = CharSet.Ansi, SetLastError = false)]
				public static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, out IntPtr pSessionInfo, out int pCount);
				[DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				public static extern void WTSFreeMemory(IntPtr pMemory);
				[DllImport("winsta.dll", CharSet = CharSet.Auto, SetLastError = false)]
				public static extern int WinStationQueryInformation(IntPtr hServer, int sessionId, int information, ref WINSTATIONINFORMATIONW pBuffer, int bufferLength, ref int returnedLength);
				[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				public static extern int GetCurrentProcessId();
				[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
				public static extern bool ProcessIdToSessionId(int processId, ref int pSessionId);

				[StructLayout(LayoutKind.Sequential)]
				private struct WTS_SESSION_INFO
				{
					public Int32 SessionId; [MarshalAs(UnmanagedType.LPStr)] public string SessionName; public WTS_CONNECTSTATE_CLASS State;
				}

				[StructLayout(LayoutKind.Sequential)]
				public struct WINSTATIONINFORMATIONW
				{
					[MarshalAs(UnmanagedType.ByValArray, SizeConst = 70)] private byte[] Reserved1;
					public int SessionId;
					[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] private byte[] Reserved2;
					public FILETIME ConnectTime;
					public FILETIME DisconnectTime;
					public FILETIME LastInputTime;
					public FILETIME LoginTime;
					[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1096)] private byte[] Reserved3;
					public FILETIME CurrentTime;
				}

				public enum WINSTATIONINFOCLASS { WinStationInformation = 8 }
				public enum WTS_CONNECTSTATE_CLASS { Active, Connected, ConnectQuery, Shadow, Disconnected, Idle, Listen, Reset, Down, Init }
				public enum WTS_INFO_CLASS { SessionId=4, UserName, SessionName, DomainName, ConnectState, ClientBuildNumber, ClientName, ClientDirectory, ClientProtocolType=16 }

				private static IntPtr OpenServer(string Name) { IntPtr server = WTSOpenServer(Name); return server; }
				private static void CloseServer(IntPtr ServerHandle) { WTSCloseServer(ServerHandle); }
				
				private static IList<T> PtrToStructureList<T>(IntPtr ppList, int count) where T : struct
				{
					List<T> result = new List<T>(); long pointer = ppList.ToInt64(); int sizeOf = Marshal.SizeOf(typeof(T));
					for (int index = 0; index < count; index++)
					{
						T item = (T) Marshal.PtrToStructure(new IntPtr(pointer), typeof(T)); result.Add(item); pointer += sizeOf;
					}
					return result;
				}

				public static DateTime? FileTimeToDateTime(FILETIME ft)
				{
					if (ft.dwHighDateTime == 0 && ft.dwLowDateTime == 0) { return null; }
					long hFT = (((long) ft.dwHighDateTime) << 32) + ft.dwLowDateTime;
					return DateTime.FromFileTime(hFT);
				}

				public static WINSTATIONINFORMATIONW GetWinStationInformation(IntPtr server, int sessionId)
				{
					int retLen = 0;
					WINSTATIONINFORMATIONW wsInfo = new WINSTATIONINFORMATIONW();
					WinStationQueryInformation(server, sessionId, (int) WINSTATIONINFOCLASS.WinStationInformation, ref wsInfo, Marshal.SizeOf(typeof(WINSTATIONINFORMATIONW)), ref retLen);
					return wsInfo;
				}
				
				public static TerminalSessionData[] ListSessions(string ServerName)
				{
					IntPtr server = IntPtr.Zero;
					if (ServerName != "localhost" && ServerName != String.Empty) {server = OpenServer(ServerName);}
					List<TerminalSessionData> results = new List<TerminalSessionData>();
					try
					{
						IntPtr ppSessionInfo = IntPtr.Zero; int count; bool _isUserSession = false; IList<WTS_SESSION_INFO> sessionsInfo;
						
						if (WTSEnumerateSessions(server, 0, 1, out ppSessionInfo, out count) == 0) { throw new Win32Exception(); }
						try { sessionsInfo = PtrToStructureList<WTS_SESSION_INFO>(ppSessionInfo, count); }
						finally { WTSFreeMemory(ppSessionInfo); }
						
						foreach (WTS_SESSION_INFO sessionInfo in sessionsInfo)
						{
							if (sessionInfo.SessionName != "Services" && sessionInfo.SessionName != "RDP-Tcp") { _isUserSession = true; }
							results.Add(new TerminalSessionData(sessionInfo.SessionId, sessionInfo.State, sessionInfo.SessionName, _isUserSession));
							_isUserSession = false;
						}
					}
					finally { CloseServer(server); }
					TerminalSessionData[] returnData = results.ToArray();
					return returnData;
				}
				
				public static TerminalSessionInfo GetSessionInfo(string ServerName, int SessionId)
				{
					IntPtr server = IntPtr.Zero;
					IntPtr buffer = IntPtr.Zero;
					int bytesReturned;
					TerminalSessionInfo data = new TerminalSessionInfo();
					bool _IsCurrentSessionId = false;
					bool _IsConsoleSession = false;
					bool _IsUserSession = false;
					int currentSessionID = 0;
					string _NTAccount = String.Empty;

					if (ServerName != "localhost" && ServerName != String.Empty) { server = OpenServer(ServerName); }
					if (ProcessIdToSessionId(GetCurrentProcessId(), ref currentSessionID) == false) { currentSessionID = -1; }
					try
					{
						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientBuildNumber, out buffer, out bytesReturned) == false) { return data; }
						int lData = Marshal.ReadInt32(buffer);
						data.ClientBuildNumber = lData;

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientDirectory, out buffer, out bytesReturned) == false) { return data; }
						string strData = Marshal.PtrToStringAnsi(buffer);
						data.ClientDirectory = strData;

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientName, out buffer, out bytesReturned) == false) { return data; }
						strData = Marshal.PtrToStringAnsi(buffer);
						data.ClientName = strData;

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientProtocolType, out buffer, out bytesReturned) == false) { return data; }
						Int16 intData = Marshal.ReadInt16(buffer);
						if (intData == 2) {strData = "RDP";} else {strData = "";}
						data.ClientProtocolType = strData;

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ConnectState, out buffer, out bytesReturned) == false) { return data; }
						lData = Marshal.ReadInt32(buffer);
						data.ConnectState = (WTS_CONNECTSTATE_CLASS)Enum.ToObject(typeof(WTS_CONNECTSTATE_CLASS), lData);

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.SessionId, out buffer, out bytesReturned) == false) { return data; }
						lData = Marshal.ReadInt32(buffer);
						data.SessionId = lData;

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.DomainName, out buffer, out bytesReturned) == false) { return data; }
						strData = Marshal.PtrToStringAnsi(buffer);
						data.DomainName = strData;
						if (strData != String.Empty) {_NTAccount = strData;}

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.UserName, out buffer, out bytesReturned) == false) { return data; }
						strData = Marshal.PtrToStringAnsi(buffer);
						data.UserName = strData;
						if (strData != String.Empty) {data.NTAccount = _NTAccount + "\\" + strData;}

						if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.SessionName, out buffer, out bytesReturned) == false) { return data; }
						strData = Marshal.PtrToStringAnsi(buffer);
						data.SessionName = strData;
						if (strData != "Services" && strData != "RDP-Tcp") { _IsUserSession = true; }
						data.IsUserSession = _IsUserSession;
						if (strData == "Console") { _IsConsoleSession = true; }
						data.IsConsoleSession = _IsConsoleSession;

						WINSTATIONINFORMATIONW wsInfo = GetWinStationInformation(server, SessionId);
						DateTime? _loginTime = FileTimeToDateTime(wsInfo.LoginTime);
						DateTime? _lastInputTime = FileTimeToDateTime(wsInfo.LastInputTime);
						DateTime? _disconnectTime = FileTimeToDateTime(wsInfo.DisconnectTime);
						DateTime? _currentTime = FileTimeToDateTime(wsInfo.CurrentTime);
						TimeSpan? _idleTime = (_currentTime != null && _lastInputTime != null) ? _currentTime.Value - _lastInputTime.Value : TimeSpan.Zero;
						data.LogonTime = _loginTime;
						data.IdleTime = _idleTime;
						data.DisconnectTime = _disconnectTime;

						if (currentSessionID == SessionId) { _IsCurrentSessionId = true; }
						data.IsCurrentSession = _IsCurrentSessionId;
					}
					finally
					{
						WTSFreeMemory(buffer); buffer = IntPtr.Zero; CloseServer(server);
					}
					return data;
				}
			}

			public class TerminalSessionData
			{
				public int SessionId; public Session.WTS_CONNECTSTATE_CLASS ConnectionState; public string SessionName; public bool IsUserSession;
				public TerminalSessionData(int sessionId, Session.WTS_CONNECTSTATE_CLASS connState, string sessionName, bool isUserSession)
				{
					SessionId = sessionId; ConnectionState = connState; SessionName = sessionName; IsUserSession = isUserSession;
				}
			}

			public class TerminalSessionInfo
			{
				public string NTAccount; public string UserName; public string DomainName; public int SessionId; public string SessionName;
				public Session.WTS_CONNECTSTATE_CLASS ConnectState; public bool IsCurrentSession; public bool IsConsoleSession;
				public bool IsUserSession; public bool IsLocalAdmin; public DateTime? LogonTime; public TimeSpan? IdleTime; public DateTime? DisconnectTime;
				public string ClientName; public string ClientProtocolType; public string ClientDirectory; public int ClientBuildNumber;
			}
		}
'@
		If (-not ([System.Management.Automation.PSTypeName]'QueryUser.Session').Type) {
			Add-Type -TypeDefinition $QueryUserSessionSource -Language CSharp -IgnoreWarnings -ErrorAction 'Stop'
		}
	}
	Process {
		Try {
			If (-not $SkipIsLocalAdminCheck) {
				Try {
					## Get NTAccount names in DOMAIN\Username format for the local Administrators security group
					$LocalAdminGroupSID = New-Object -TypeName System.Security.Principal.SecurityIdentifier -ArgumentList 'S-1-5-32-544'
					$LocalAdminGroupNTAccount = $LocalAdminGroupSID.Translate([System.Security.Principal.NTAccount])
					$LocalAdminGroupName = ($LocalAdminGroupNTAccount.Value).Split('\')[1]
					$LocalAdminGroup =[ADSI]"WinNT://$($env:COMPUTERNAME)/$LocalAdminGroupName" 
					$LocalAdminGroupMembers = @($LocalAdminGroup.PSBase.Invoke('Members'))
					[string[]]$LocalAdminGroupUserName = ''
					$LocalAdminGroupMembers | ForEach { [string[]]$LocalAdminGroupUserName += $_.GetType().InvokeMember('Name', 'GetProperty', $null, $_, $null) }
					[string[]]$LocalAdminGroupUserName = $LocalAdminGroupUserName | Where-Object { -not [string]::IsNullOrEmpty($_) }
					[string[]]$LocalAdminGroupNTAccounts = @()
					[string[]]$LocalAdminGroupNTAccounts = $LocalAdminGroupUserName | ForEach-Object { (New-Object -TypeName System.Security.Principal.NTAccount -ArgumentList $_).Translate([System.Security.Principal.SecurityIdentifier]).Translate([System.Security.Principal.NTAccount]).Value }
					[boolean]$IsLocalAdminCheckSuccess = $true
				}
				Catch {
					[boolean]$IsLocalAdminCheckSuccess = $false
					[string[]]$LocalAdminGroupNTAccounts = @()
				}
			}
			
			Write-Log -Message 'Get session information for all logged on users.' -Source ${CmdletName} -DisableOnRelaunchToolkitAsUser
			[psobject[]]$TerminalSessions = [QueryUser.Session]::ListSessions('localhost')
			ForEach ($TerminalSession in $TerminalSessions) {
				If (($TerminalSession.IsUserSession)) {
					[psobject]$SessionInfo = [QueryUser.Session]::GetSessionInfo('localhost', $TerminalSession.SessionId)
					If ($SessionInfo.UserName) {
						If ((-not $SkipIsLocalAdminCheck) -and ($IsLocalAdminCheckSuccess)) {
							If ($LocalAdminGroupNTAccounts -contains $SessionInfo.NTAccount) {
								$SessionInfo.IsLocalAdmin = $true
							}
							Else {
								$SessionInfo.IsLocalAdmin = $false
							}
						}
						[psobject[]]$TerminalSessionInfo += $SessionInfo
					}
				}
			}
			Write-Output $TerminalSessionInfo
		}
		Catch {
			Write-Log -Message "Failed to get session information for all logged on users. `n$(Resolve-Error)" -Severity 3 -Source ${CmdletName} -DisableOnRelaunchToolkitAsUser
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion


#region Function Invoke-PSCommandAsUser
Function Invoke-PSCommandAsUser {
	Param (
		[string]$UserName = $RelaunchToolkitAsNTAccount,
		[string]$PSPath = "$PSHOME\powershell.exe",
		[scriptblock]$Command,
		[switch]$NoWait = $false,
		[switch]$NoExit = $false,
		[switch]$ExitAfterCommandExecution = $false,
		[switch]$PassThru = $false,
		[boolean]$ContinueOnError = $true
	)

	If (-not $UserName) { Throw "No valid username [$UserName] specified." }

	## Variables: Application
	If (-not $Variables_Application) {
		[scriptblock]$Variables_Application = {
			[string]$appVendor = $appVendor
			[string]$appName = $appName
			[string]$appVersion = $appVersion
			[string]$appArch = $appArch
			[string]$appLang = $appLang
			[string]$appRevision = $appRevision
		}
	}
	## Variables: All Script Parameters
	If (-not $Variables_AllScriptParams) {
		[scriptblock]$Variables_AllScriptParams = {
			[string]$DeploymentType = $DeploymentType
			[string]$DeployMode = $DeployMode
		}
	}
	## Variables: PowerShell.exe Parameters
	If ($NoExit) {
		[string]$Variables_PowerShellExeParams = "-ExecutionPolicy Bypass -NoProfile -NoLogo -NoExit -WindowStyle Hidden"
	}
	Else {
		[string]$Variables_PowerShellExeParams = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden"
	}
	## Variables: Skip Admin Check
	[scriptblock]$Variables_SkipAdminCheck = { [boolean]$SkipAdminCheck = $true }
	## Variables: Exit With LastExitCode
	[scriptblock]$Variables_ExitWithLastExitCode = { Exit $LastExitCode }
	## Variables: Dot Source Toolkit For User
	[string]$Variables_DotSourceToolkitForUser = ". `"$scriptPath`" -RelaunchToolkitAsUser $appDeployMainScriptParameters"
	
	## If -PassThru option selected, then configure command so that it saves the output from the command as serialized XML output
	If ($PassThru) {
		If (Test-Path -Path "$dirAppDeployTemp\ResultsFrom_InvokePSCommandAsUser.xml" -PathType 'Leaf') {
			Remove-Item -Path "$dirAppDeployTemp\ResultsFrom_InvokePSCommandAsUser.xml" -Force -ErrorAction 'SilentlyContinue' | Out-Null
		}
		[scriptblock]$Command = [scriptblock]::Create($Command.ToString() + " | Export-Clixml -Path '$dirAppDeployTemp\ResultsFrom_InvokePSCommandAsUser.xml' -Force" )
	}
	
	## Define the command line for launching the process with a user account
	[scriptblock]$PSPrameters = { "$Variables_PowerShellExeParams -Command `".{ $Variables_InstallPhase; $Variables_Application; $Variables_AllScriptParams; $Variables_Script; $Variables_SkipAdminCheck; $Variables_DotSourceToolkitForUser; $Command; $Variables_ExitWithLastExitCode }`"" }
	
	[System.Diagnostics.Process]$PSProcess = Invoke-ProcessWithLogonToken -PassThru -Username $UserName -CreateProcess $PSPath -ProcessArgs (& $PSPrameters) -WarningAction 'SilentlyContinue'
	If (-not $NoWait) {
		$PSProcess.WaitForExit()
		[int32]$PSExitCode = $PSProcess.ExitCode
	}
	If ($PSProcess) { $PSProcess.Close() }
	
	If ($PassThru) {
		If (Test-Path -Path "$dirAppDeployTemp\ResultsFrom_InvokePSCommandAsUser.xml" -PathType 'Leaf') {
			$CommandOutput = Import-Clixml -Path "$dirAppDeployTemp\ResultsFrom_InvokePSCommandAsUser.xml" -ErrorAction 'Stop'
		}
		Write-Output $CommandOutput
	}

	## Determine action based on exit code
	# Switch ($PSExitCode) {
	# 	$configInstallationUIExitCode { Exit-Script -ExitCode $PSExitCode }
	# 	$configInstallationDeferExitCode { Exit-Script -ExitCode $PSExitCode }
	# 	3010 { Exit-Script -ExitCode $PSExitCode }
	# 	1641 { Exit-Script -ExitCode $PSExitCode }
	# 	0 { If ($ExitAfterCommandExecution) { Exit-Script -ExitCode $PSExitCode } }
	# 	Default { If ($ExitAfterCommandExecution) { Exit-Script -ExitCode $PSExitCode } }
	# }
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
	If ((Split-Path -Path $invokingScript -Leaf) -eq 'AppDeployToolkitHelp.ps1') { Return }
}

## Set the install phase to asynchronous if the script was not dot sourced, i.e. called with parameters
If ($ReferringApplication) {
	$installName = $ReferringApplication
	$installTitle = $ReferringApplication -replace '_',' '
	$installPhase = 'Asynchronous'
}

## Assemblies: Load
Try {
	Add-Type -AssemblyName System.Windows.Forms -ErrorAction 'Stop'
	Add-Type -AssemblyName PresentationFramework -ErrorAction 'Stop'
	Add-Type -AssemblyName Microsoft.VisualBasic -ErrorAction 'Stop'
	Add-Type -AssemblyName System.Drawing -ErrorAction 'Stop'
	Add-Type -AssemblyName PresentationFramework -ErrorAction 'Stop'
	Add-Type -AssemblyName PresentationCore -ErrorAction 'Stop'
	Add-Type -AssemblyName WindowsBase -ErrorAction 'Stop'
}
Catch {
	Write-Log -Message "Failed to load assembly. `n$(Resolve-Error)" -Severity 3 -Source $appDeployToolkitName
	If ($deployModeNonInteractive) {
		Write-Log -Message "Continue despite assembly load error since deployment mode is [$deployMode]" -Source $appDeployToolkitName
	}
	Else {
		Exit-Script -ExitCode 1
	}
}

## If the ShowInstallationPrompt Parameter is specified, only call that function.
If ($showInstallationPrompt) {
	$deployModeSilent = $true
	Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationPrompt]" -Source $appDeployToolkitName
	$appDeployMainScriptParameters.Remove('ShowInstallationPrompt')
	$appDeployMainScriptParameters.Remove('ReferringApplication')
	Show-InstallationPrompt @appDeployMainScriptParameters
	Exit 0
}

## If the ShowInstallationRestartPrompt Parameter is specified, only call that function.
If ($showInstallationRestartPrompt) {
	$deployModeSilent = $true
	Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationRestartPrompt]" -Source $appDeployToolkitName
	$appDeployMainScriptParameters.Remove('ShowInstallationRestartPrompt')
	$appDeployMainScriptParameters.Remove('ReferringApplication')
	Show-InstallationRestartPrompt @appDeployMainScriptParameters
	Exit 0
}

## If the CleanupBlockedApps Parameter is specified, only call that function.
If ($cleanupBlockedApps) {
	$deployModeSilent = $true
	Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-CleanupBlockedApps]" -Source $appDeployToolkitName
	Unblock-AppExecution
	Exit 0
}

## If the ShowBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog) {
	$DisableLogging = $true
	Try {
		$deployModeSilent = $true
		Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowBlockedAppDialog]" -Source $appDeployToolkitName
		Show-InstallationPrompt -Title $installTitle -Message $configBlockExecutionMessage -Icon Warning -ButtonRightText 'OK'
		Exit 0
	}
	Catch {
		$InstallPromptErrMsg = "There was an error in displaying the Installation Prompt. `n$(Resolve-Error)"
		Write-Log -Message $InstallPromptErrMsg -Severity 3 -Source $appDeployToolkitName
		Show-DialogBox -Text $InstallPromptErrMsg -Icon 'Stop' | Out-Null
		Exit 1
	}
}

## Initialization Logging
If ($RelaunchToolkitAsUser) {
	Write-Log -Message "Dot-sourcing [$scriptFileName] in a separate PowerShell.exe process running under user account [$ProcessNTAccount] to allow execution of PowerShell commands in a user session." -Source $appDeployToolkitName
}
If (-not $RelaunchToolkitAsUser) {
	[scriptblock]$Variables_InstallPhase = { [string]$installPhase = 'Initialization' }; .$Variables_InstallPhase
	$scriptSeparator = '*' * 79
	Write-Log -Message ($scriptSeparator,$scriptSeparator) -Source $appDeployToolkitName
	Write-Log -Message "[$installName] setup started." -Source $appDeployToolkitName
	
	## Check how the script was invoked
	If ($invokingScript) {
		Write-Log -Message "Script [$scriptPath] dot-source invoked by [$invokingScript]" -Source $appDeployToolkitName
	}
	Else {
		Write-Log -Message "Script [$scriptPath] invoked directly" -Source $appDeployToolkitName
	}
}
## Dot Source script extensions
If (Test-Path -Path "$scriptRoot\$appDeployToolkitDotSourceExtensions" -PathType Leaf) {
	If ($RelaunchToolkitAsUser) {
		. "$scriptRoot\$appDeployToolkitDotSourceExtensions" -RelaunchToolkitAsUser
	}
	Else {
		. "$scriptRoot\$appDeployToolkitDotSourceExtensions"
	}
}

## Dot Source Invoke-ProcessWithLogonToken.ps1
If (Test-Path -Path "$scriptRoot\Invoke-ProcessWithLogonToken.ps1" -PathType Leaf) {
	. "$scriptRoot\Invoke-ProcessWithLogonToken.ps1"
}

## Evaluate non-default parameters passed to the scripts
If ($deployAppScriptParameters) { [string]$deployAppScriptParameters = ($deployAppScriptParameters.GetEnumerator() | ForEach-Object { If ($_.Value.GetType().Name -eq 'SwitchParameter') { "-$($_.Key):`$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Boolean') { "-$($_.Key) `$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Int32') { "-$($_.Key) $($_.Value)" } Else { "-$($_.Key) `"$($_.Value)`"" } }) -join ' ' }
If ($appDeployMainScriptParameters) { [string]$appDeployMainScriptParameters = ($appDeployMainScriptParameters.GetEnumerator() | ForEach-Object { If ($_.Value.GetType().Name -eq 'SwitchParameter') { "-$($_.Key):`$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Boolean') { "-$($_.Key) `$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Int32') { "-$($_.Key) $($_.Value)" } Else { "-$($_.Key) `"$($_.Value)`"" } }) -join ' ' }
If ($appDeployExtScriptParameters) { [string]$appDeployExtScriptParameters = ($appDeployExtScriptParameters.GetEnumerator() | ForEach-Object { If ($_.Value.GetType().Name -eq 'SwitchParameter') { "-$($_.Key):`$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Boolean') { "-$($_.Key) `$" + "$($_.Value)".ToLower() } ElseIf ($_.Value.GetType().Name -eq 'Int32') { "-$($_.Key) $($_.Value)" } Else { "-$($_.Key) `"$($_.Value)`"" } }) -join ' ' }

## Check the XML config file version
If ($configConfigVersion -lt $appDeployMainScriptMinimumConfigVersion) {
	[string]$XMLConfigVersionErr = "The XML configuration file version [$configConfigVersion] is lower than the supported version required by the Toolkit [$appDeployMainScriptMinimumConfigVersion]. Please upgrade the configuration file."
	Write-Log -Message $XMLConfigVersionErr -Severity 3 -Source $appDeployToolkitName
	Throw $XMLConfigVersionErr
}

## Log system/script information
If (-not $RelaunchToolkitAsUser) {
	If ($appScriptVersion) { Write-Log -Message "[$installName] script version is [$appScriptVersion]" -Source $appDeployToolkitName }
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
	Write-Log -Message "Current Culture is [$($culture.Name)] and UI language is [$currentLanguage]" -Source $appDeployToolkitName
	Write-Log -Message "Hardware Platform is [$($OriginalDisableLogging = $DisableLogging; $DisableLogging = $true; Get-HardwarePlatform; $DisableLogging = $OriginalDisableLogging)]" -Source $appDeployToolkitName
	Write-Log -Message "PowerShell Host is [$($envHost.Name)] with version [$($envHost.Version)]" -Source $appDeployToolkitName
	Write-Log -Message "PowerShell Version is [$envPSVersion $psArchitecture]" -Source $appDeployToolkitName
	Write-Log -Message "PowerShell CLR (.NET) version is [$envCLRVersion]" -Source $appDeployToolkitName
	Write-Log -Message "System has a DPI scale of [$dpiScale]." -Source $appDeployToolkitName
	Write-Log -Message $scriptSeparator -Source $appDeployToolkitName
}

## Get a list of all users logged on to the system (both local and RDP users), and discover session details for account executing script
If (-not $RelaunchToolkitAsUser) {
	[psobject[]]$LoggedOnUserSessions = Get-LoggedOnUser
}
Else {
	[psobject[]]$LoggedOnUserSessions = Get-LoggedOnUser -SkipIsLocalAdminCheck
}
Write-Log -Message "Logged on user session details: `n$($LoggedOnUserSessions | Format-List | Out-String)" -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
[string[]]$usersLoggedOn = $LoggedOnUserSessions | ForEach-Object { $_.NTAccount }

If ($usersLoggedOn) {
	Write-Log -Message "The following users are logged on to the system: $($usersLoggedOn -join ', ')" -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
	
	## Check if the current process is running in the context of one of the logged in users
	[psobject]$CurrentLoggedOnUserSession = $LoggedOnUserSessions | Where-Object { $_.IsCurrentSession }
	If ($CurrentLoggedOnUserSession) {
		Write-Log -Message "Current process is running under a user account [$($CurrentLoggedOnUserSession.NTAccount)]" -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
	}
	Else {
		Write-Log -Message "Current process is running under a system account [$ProcessNTAccount]" -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
	}

	## Get account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
	[psobject]$CurrentConsoleUserSession = $LoggedOnUserSessions | Where-Object { $_.IsConsoleSession }
	If ($CurrentConsoleUserSession) {
		Write-Log -Message "The following user is the console user [$($CurrentConsoleUserSession.NTAccount)] (user with control of physical monitor, keyboard, and mouse)." -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
	}
	Else {
		Write-Log -Message 'There is no console user logged in (user with control of physical monitor, keyboard, and mouse).' -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
	}

	## Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
	If ($CurrentConsoleUserSession) {
		[string]$RelaunchToolkitAsNTAccount = $CurrentConsoleUserSession.NTAccount
	}
	ElseIf ($configToolkitAllowSystemInteractionForNonConsoleUser) {
		[string]$FirstLoggedInNonConsoleUser = $LoggedOnUserSessions | Select-Object -First 1
		If ($FirstLoggedInNonConsoleUser) { [string]$RelaunchToolkitAsNTAccount = $FirstLoggedInNonConsoleUser.NTAccount }
	}
	Else {
		[string]$RelaunchToolkitAsNTAccount = ''
	}
}
Else {
	Write-Log -Message 'No users are logged on to the system' -Source $appDeployToolkitName
}

## Check if script is running on a Terminal Services client session
Try { [boolean]$IsTerminalServerSession = [System.Windows.Forms.SystemInformation]::TerminalServerSession } Catch { }
Write-Log -Message "The process is running in a terminal server session: [$IsTerminalServerSession]." -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser

## Check if script is running from a SCCM Task Sequence
Try {
	[__comobject]$SMSTSEnvironment = New-Object -ComObject Microsoft.SMS.TSEnvironment -ErrorAction 'Stop'
	Write-Log -Message 'Successfully loaded COM Object [Microsoft.SMS.TSEnvironment]. Therefore, script is currently running from a SCCM Task Sequence.' -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
	[boolean]$runningTaskSequence = $true
}
Catch {
	Write-Log -Message 'Unable to load COM Object [Microsoft.SMS.TSEnvironment]. Therefore, script is not currently running from a SCCM Task Sequence.' -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
	[boolean]$runningTaskSequence = $false
}

## Check to see if the Task Scheduler service is in a healthy state
## The task scheduler service and the services it is dependent on can/should only be started/stopped/modified when running in the SYSTEM context.
[boolean]$IsTaskSchedulerHealthy = $true
If ($IsLocalSystemAccount) {
	[scriptblock]$TestServiceHealth = {
		Param (
			[string]$ServiceName
		)
		Try {
			If (Test-ServiceExists -Name $ServiceName -ContinueOnError $false) {
				If ((Get-ServiceStartMode -Name $ServiceName -ContinueOnError $false) -ne 'Automatic') {
					Set-ServiceStartMode -Name $ServiceName -StartMode 'Automatic' -ContinueOnError $false
				}
				Start-ServiceAndDependencies -Name $ServiceName -SkipServiceExistsTest -ContinueOnError $false
			}
			Else {
				[boolean]$IsTaskSchedulerHealthy = $false
			}
		}
		Catch {
			[boolean]$IsTaskSchedulerHealthy = $false
		}
	}
	#  Check the health of the 'COM+ Event System' service
	& $TestServiceHealth -ServiceName 'EventSystem'
	#  Check the health of the 'Remote Procedure Call (RPC)' service
	& $TestServiceHealth -ServiceName 'RpcSs'
	#  Check the health of the 'Windows Event Log' service
	& $TestServiceHealth -ServiceName 'EventLog'
	#  Check the health of the Task Scheduler service
	& $TestServiceHealth -ServiceName 'Schedule'

	Write-Log -Message "The task scheduler service is in a healthy state: $IsTaskSchedulerHealthy" -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
}

## If script is running in session zero
If (-not $RelaunchToolkitAsUser) {
	If ($SessionZero) {
		##  If the script was launched with deployment mode set to NonInteractive, then continue
		If ($deployMode -eq 'NonInteractive') {
			Write-Log -Message "Session 0 detected. Deployment mode was manually set to [$deployMode]." -Source $appDeployToolkitName
		}
		Else {
			##  If the process is not able to display a UI, enable NonInteractive mode
			If (-not $IsProcessUserInteractive) {
				Write-Log -Message 'Session 0 detected, process not running in user interactive mode.' -Source $appDeployToolkitName
				If ($configToolkitAllowSystemInteraction) {
					Write-Log -Message "'Allow System Interaction' option is enabled in the toolkit XML configuration file." -Source $appDeployToolkitName
					If ($CurrentConsoleUserSession) {
						$deployMode = 'Interactive'
						Write-Log -Message "Toolkit will use a console user account [$RelaunchToolkitAsNTAccount] to provide interaction in the SYSTEM context..." -Source $appDeployToolkitName
					}
					ElseIf ($configToolkitAllowSystemInteractionForNonConsoleUser) {
						Write-Log -Message "'Allow System Interaction' for non-console user is enabled in the toolkit XML configuration file." -Source $appDeployToolkitName
						If ($FirstLoggedInNonConsoleUser) {
							$deployMode = 'Interactive'
							Write-Log -Message "Toolkit will use a non-console user account [$RelaunchToolkitAsNTAccount] to provide interaction in the SYSTEM context..." -Source $appDeployToolkitName
						}
						Else {
							Write-Log -Message 'No users are currently logged in to allow relaunching the toolkit to provide interaction in the SYSTEM context.' -Source $appDeployToolkitName
						}
					}
					Else {
						$deployMode = 'NonInteractive'
						Write-Log -Message 'No users are logged on to be able to run in interactive mode.' -Source $appDeployToolkitName
					}
				}
				Else {
					Write-Log -Message "'Allow System Interaction' option is disabled in the toolkit XML configuration file." -Source $appDeployToolkitName
					$deployMode = 'NonInteractive'
					Write-Log -Message "Deployment mode set to [$deployMode]." -Source $appDeployToolkitName
				}
			}
			Else {
				If (-not $RelaunchToolkitAsNTAccount) {
					$deployMode = 'NonInteractive'
					Write-Log -Message "Session 0 detected, process running in user interactive mode, no users logged in: deployment mode set to [$deployMode]." -Source $appDeployToolkitName
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
}

## Set Deploy Mode switches
If ($deployMode) {
	Write-Log -Message "Installation is running in [$deployMode] mode." -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser
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
	Default { $deploymentTypeName = $configDeploymentTypeInstall }
}
If ($deploymentTypeName) { Write-Log -Message "Deployment type is [$deploymentTypeName]" -Source $appDeployToolkitName -DisableOnRelaunchToolkitAsUser }

## Check current permissions and exit if not running with Administrator rights
If ($configToolkitRequireAdmin) {
	#  Check if the current process is running with elevated administrator permissions
	If ((-not $IsAdmin) -and (-not $ShowBlockedAppDialog) -and (-not $SkipAdminCheck)) {
		[string]$AdminPermissionErr = "[$appDeployToolkitName] has an XML config file option [Toolkit_RequireAdmin] set to [True] so as to require Administrator rights for the toolkit to function. Please re-run the deployment script as an Administrator or change the option in the XML config file to not require Administrator rights."
		Write-Log -Message $AdminPermissionErr -Severity 3 -Source $appDeployToolkitName
		Show-DialogBox -Text $AdminPermissionErr -Icon 'Stop' | Out-Null
		Throw $AdminPermissionErr
	}
}

## If terminal server mode was specified, change the installation mode to support it
If ($terminalServerMode) { Enable-TerminalServerInstallMode }

#endregion
##*=============================================
##* END SCRIPT BODY
##*=============================================