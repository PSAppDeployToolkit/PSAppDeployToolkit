<#
.SYNOPSIS
	This script contains the functions and logic engine for the Deploy-Application.ps1 script.
.DESCRIPTION
	The script can be called directly to dot-source the toolkit functions for testing, but it is usually called by the Deploy-Application.ps1 script.
	The script can usually be updated to the latest version without impacting your per-application Deploy-Application scripts. Please check release notes before upgrading.   
.PARAMETER CleanupBlockedApps
	Clean up the blocked applications.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.PARAMETER ShowBlockedAppDialog
	Allows the 3010 return code (requires restart) to be passed back to the parent process (e.g. SCCM) if detected from an installation.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.PARAMETER BlockedAppInstallName
	Name of the application installation that blocked the apps initially.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com
"#>
Param (	
	## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or Image File Execution Options
	[switch]$CleanupBlockedApps = $false, 
	[switch]$ShowBlockedAppDialog = $false, # Display a dialog box showing that the application execution is blocked
	[string]$BlockedAppInstallName # Name of the application installation that blocked the apps initially
)

#*=============================================
#* VARIABLE DECLARATION
#*=============================================

# Variables: Script
$appDeployMainScriptFriendlyName = "App Deploy Toolkit Main"
$appDeployMainScriptVersion = "2.0.0"
$appDeployMainScriptDate = "08/07/2013"

# Variables: Environment
$currentDate = (Get-Date -UFormat "%d-%m-%Y")
$currentTime = (Get-Date -UFormat "%T")
$envHost = $host
$envAllUsersProfile = $env:ALLUSERSPROFILE
$envAppData = $env:APPDATA
$envArchitecture = $env:PROCESSOR_ARCHITECTURE
$envComputerName = $env:COMPUTERNAME
$envHomeDrive = $env:HOMEDRIVE
$envHomePath = $env:HOMEPATH
$envLocalAppData = $env:LOCALAPPDATA
$envLogonServer = $env:LOGONSERVER
$envOS = Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue
$envProgramFilesx86 = "${env:ProgramFiles(x86)}"
$envProgramData = $env:PROGRAMDATA
$envPublic = $env:PUBLIC
$envTemp = $env:TEMP
$envUserDNSDomain = $env:USERDNSDOMAIN
$envUserDomain = $env:USERDOMAIN
$envUserName = $env:USERNAME
$envUserProfile = $env:USERPROFILE
$envWinDir = $env:WINDIR
$currentLanguage = $PSUICulture.SubString(0,2).ToUpper()
$scriptName = [System.IO.Path]::GetFileNameWithoutExtension($MyInvocation.MyCommand.Definition)
$scriptPath = $MyInvocation.MyCommand.Definition
$scriptFileName = Split-Path -Leaf $MyInvocation.MyCommand.Definition
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
# Get the invoking script directory
If (((Get-Variable MyInvocation).Value).ScriptName) { 
	$scriptParentPath = Split-Path -Parent ((Get-Variable MyInvocation).Value).ScriptName
}
# Fall back on the directory one level above this script
Else {
	$scriptParentPath = (Get-Item $scriptRoot).Parent.FullName
}

# Variables: Directories
$dirSystemRoot = $env:SystemRoot
$dirAppDeployFiles = Join-Path $scriptParentPath "AppDeployToolkitFiles" # The AppDeployFiles directory should be relative to the parent invoking script
$dirFiles = Join-Path $scriptParentPath "Files" # The Files directory should be relative to the parent invoking script
$dirSupportFiles = Join-Path $scriptParentPath "SupportFiles"
$dirAppDeployTemp = Join-Path $env:PUBLIC ("PSAppDeployToolkit")
$dirBlockedApps = Join-Path $dirAppDeployTemp "BlockedApps" 

# Variables: App Deploy Dependency Files
$appDeployLogoIcon = Join-Path $scriptRoot "AppDeployToolkitLogo.ico"
$appDeployConfigFile = Join-Path $scriptRoot "AppDeployToolkitConfig.xml"

# Variables: App Deploy Optional Files
# Specify any additional PowerShell script files to be dot-sourced by this script, separated by commas.
$appDeployToolkitDotSources = "AppDeployToolkitExtensions.ps1" 

# Check that dependency files are present
If (!(Test-Path $AppDeployLogoIcon)) {
	Throw "Error: AppDeploy logo icon file required."
}
If (!(Test-Path $AppDeployConfigFile)) {
	Throw "Error: AppDeploy xml configuration file not found."
}

# Import variables from XML configuration file
[xml]$xmlConfigFile = Get-Content $AppDeployConfigFile
$xmlConfig = $xmlConfigFile.AppDeployToolkit_Config

# Get MSI Options
$xmlConfigMSI = $xmlConfig.MSI_Options
$configMSILoggingOptions = $xmlConfigMSI.MSI_LoggingOptions
$configMSIInstallParams = $xmlConfigMSI.MSI_InstallParams
$configMSISilentParams = $xmlConfigMSI.MSI_SilentParams
$configMSIUninstallParams = $xmlConfigMSI.MSI_UninstallParams
$configDirLogs = $xmlConfigMSI.MSI_LogPath

# Get Message UI Language Options (default for English if no localization found)
$xmlMessageUILanguage = "UI_Messages" + "_" + $currentLanguage
$xmlMessagesUI = $xmlConfig.$xmlMessageUILanguage
If ($xmlMessagesUI -eq $null) { 
	$xmlMessageUILanguage = "UI_Messages" + "_EN"
	$xmlMessagesUI = $xmlConfig.$xmlMessageUILanguage
}
$configBalloonTextStart = $xmlMessagesUI.BalloonText_Start
$configBalloonTextComplete = $xmlMessagesUI.BalloonText_Complete
$configBalloonTextRestartRequired = $xmlMessagesUI.BalloonText_RestartRequired
$configBalloonTextFastRetry = $xmlMessagesUI.BalloonText_FastRetry
$configBalloonTextError = $xmlMessagesUI.BalloonText_Error
$configProgressMessage = $xmlMessagesUI.Progress_Message
$configClosePromptConfirm = $xmlMessagesUI.ClosePrompt_Confirm
$configClosePromptMessage = $xmlMessagesUI.ClosePrompt_Message
$configClosePromptButtonClose = $xmlMessagesUI.ClosePrompt_ButtonClose
$configClosePromptButtonContinue = $xmlMessagesUI.ClosePrompt_ButtonContinue
$configClosePromptButtonDefer = $xmlMessagesUI.ClosePrompt_ButtonDefer
$configBlockExecutionMessage = $xmlMessagesUI.BlockExecution_Message
$configDeploymentTypeInstall = $xmlMessagesUI.DeploymentType_Install
$configDeploymentTypeUnInstall = $xmlMessagesUI.DeploymentType_UnInstall

# Variables: Executables
$exeWusa = "wusa.exe"
$exeMsiexec = "msiexec.exe"
$exeSchTasks = "schtasks.exe"

$psArchitecture = (Get-WmiObject -Class Win32_OperatingSystem -ea 0).OSArchitecture
$is64Bit = (Get-WmiObject -Class Win32_OperatingSystem -ea 0).OSArchitecture -eq '64-bit'
$is64BitProcess = [System.IntPtr]::Size -eq 8
$isServerOS =  (Get-WmiObject -Class Win32_operatingsystem -ErrorAction SilentlyContinue | Select Name -ExpandProperty Name) -match "Server"

# Reset Switches to false
$msiRebootDetected = $false
$BlockExecution = $false
$installationStarted = $false

# Assemblies: Load
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName Microsoft.VisualBasic
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

# COM Objects: Initialize
$shell = New-Object -ComObject WScript.Shell 

# Variables: Registry Keys
# Registry keys for native and WOW64 applications
$regKeyApplications = @( "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall" )
If ($is64Bit -eq $true) {
	$regKeyLotusNotes = "HKLM:\Software\Wow6432Node\Lotus\Notes"
}
Else {
	$regKeyLotusNotes = "HKLM:\Software\Lotus\Notes"
}
$regKeyAppExecution = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"

# Set up sample variables if Dot Sourcing the script or app details have not been specified
If ($appVendor -eq "" -and $appName -eq "" -and $appVersion -eq "") {
	$appVendor = "Test"
	$appName = $appDeployMainScriptFriendlyName
	$appVersion = $appDeployMainScriptVersion
	$installPhase = "Dot Sourcing"
}

# Build the Application Title and Name
$installTitle = "$appVendor $appName $appVersion"
# Remove spaces in the application vendor/name/version, as they can cause issues in the script
$appVendor = $appVendor -replace " ",""
$appName = $appName -replace " ",""
$appVersion = $appVersion -replace " ",""
If ($appArch -ne "") {
	$installName = "$appVendor" + "_" + "$appName" + "_" + "$appVersion" + "_" + "$appArch" + "_" + "$appLang" + "_" + "$appRevision"
}
Else  {
	$installName = "$appVendor" + "_" + "$appName" + "_" + "$appVersion" + "_" + "$appLang" + "_" + "$appRevision"
}

# Variables: 
$debuggerBlockValue = "powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -file $scriptRoot\$scriptFileName -ShowBlockedAppDialog -BlockedAppInstallName `"$installName`""

# Variables: Log Files
$logFile = Join-Path $configDirLogs ("$installName" + "_AppDeployToolkit.log")

#*=============================================
#* END VARIABLE DECLARATION
#*=============================================

#*=============================================
#* FUNCTION LISTINGS
#*=============================================

Function Write-Log { 
<# 
.SYNOPSIS
	Writes output to the console and log file simultaneously
.DESCRIPTION
	This functions outputs text to the console and to the log file specified in the XML configuration.
	The date, time and installation phase is pre-pended to the text, e.g. [30-07-2013 11:27:07] [Initialization] "Deploy Application script version is [2.0.0]"
.EXAMPLE
	Write-Log -Text "This is a custom message..."
.PARAMETER Text
	The text to display in the console and to write to the log file
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param( 
		[Parameter(Mandatory = $true)]
		[array]$Text
	)
	$Text = $Text -join (" ")
	$currentDate = (Get-Date -UFormat "%d-%m-%Y")
	$currentTime = (Get-Date -UFormat "%T")
	Write-Host "[$currentDate $currentTime] [$installPhase] $Text"
	# Create the Log directory if it doesn't already exist
	If (!(Test-Path -path $configDirLogs -ErrorAction SilentlyContinue )) { New-Item $configDirLogs -type directory | Out-Null }
	# Create the Log directory if it doesn't already exist
	If (!(Test-Path -path $logFile -ErrorAction SilentlyContinue )) { New-Item $logFile -type file | Out-Null }
	Try {
		"[$currentDate $currentTime] [$installPhase] $Text" | Out-File $logFile -Append -ErrorAction SilentlyContinue
	}
	Catch {
		$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)" 
		Write-Host "$exceptionMessage" 	
	}
}

Function Exit-Script {
<# 
.SYNOPSIS
	This function exits the scripts, performs cleanup actions and passes an exit code to the parent process.
.DESCRIPTION
	This function should always be used when exiting the script, to ensure cleanup actions are performed.
	This function performs cleanup actions, such as closing down dialogs and unblocking blocked applications.
	It displays a balloon tip notification to indicate the setup is complete and whether it was a success or a failure.
	The function determines what exit code to pass to the parent process depending on the the options specified in the deployment script, e.g. 
	If $AllowRebootPassThru is set to False, it will suppress any "3010" exit codes detected during the installation and instead pass the "0" exit code.
.EXAMPLE
	Exit-Script -ExitCode "0"
.EXAMPLE
	Exit-Script -ExitCode "1618"
.PARAMETER ExitCode
	The exit code to be passed from the script to the parent process, e.g. SCCM
.NOTES	
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[string]$ExitCode = 0
	)

	# Stop the Close Program Dialog if running
	If ($formClosePrograms -ne $null) { 
		$formClosePrograms.Close 
	}	 

	# Close the Installation Progress Dialog if running
	Close-InstallationProgress

	# If block execution switch is true, call the function to unblock execution
	If ($BlockExecution -eq $true) {		
		Unblock-AppExecution
	}

	# Determine action based on exit code
	Switch ($exitCode) {		
		1618 { $installSuccess = $false ; }
		3010 { $installSuccess = $true; }
		0 { $installSuccess = $true}
		Default { $installSuccess = $false }
	}	

	If ($installSuccess -eq $true) {
		$balloonText = "$deploymentTypeName $configBalloonTextComplete"
		# Handle reboot prompts on successful script completion
		If ($msiRebootDetected -eq $true -and $AllowRebootPassThru -eq $true) { 
			Write-Log "A restart has been flagged as required."
			$balloonText = "$deploymentTypeName $configBalloonTextRestartRequired"
			$exitCode = 3010
		}
		Else {
			$exitCode = 0
		}
		Write-Log "$installName $deploymentTypeName completed with exit code [$exitcode]."
		Show-BalloonTip -BalloonTipIcon "Info" -BalloonTipText "$balloonText"
	} 
	ElseIf ($installSuccess -eq $false) {
		$balloonText = "$deploymentTypeName $configBalloonTextError"
		If ($exitCode -eq 1618) {
			$balloonText = "$deploymentTypeName $configBalloonTextFastRetry"
		}
		Write-Log "$installName $deploymentTypeName completed with exit code [$exitcode]."
		Show-BalloonTip -BalloonTipIcon "Error" -BalloonTipText "$balloonText"
	}

	Write-Log "----------------------------------------------------------------------------------------------------------"

	# Exit the script returning the exit code to SCCM
	Exit $exitCode 
}

Function Show-DialogBox {
<# 
.SYNOPSIS
	This function displays a custom dialog box with optional title, buttons, icon and timeout.
.DESCRIPTION
	This function displays a custom dialog box with optional title, buttons, icon and timeout. The default button is "OK", the default Icon is "None" and the default Timeout is none.
.EXAMPLE
	Show-DialogBox -Title "Installed Complete" -Text "Installation has completed. Please click OK and restart your computer." -Icon "Information"
.EXAMPLE
	Show-DialogBox -Title "Installation Notice" -Text "Installation will take approximately 30 mintues. Do you wish to proceed?" -Buttons "OKCancel" -Icon "Exclamation" -Timeout 600
.PARAMETER Text
	Text in the message dialog box
.PARAMETER Title
	Title of the message dialog box
.PARAMETER Buttons
	Buttons to be included on the dialog box [Default is "OK"]
	"OK"
	"OKCancel"
	"AbortRetryIgnore"
	"YesNoCancel"
	"YesNo"
	"RetryCancel"
	"CancelTryAgainContinue"
.PARAMETER Icon
	Icon to display on the dialog box [Default is "None"]
	Acceptable valures are: "None",	"Stop", "Question", "Exclamation", "Information", 
.PARAMETER Timeout
	Timeout period in seconds before automatically closing the dialog box with the return message "Timeout" [Default is None]
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
	[ValidateNotNullorEmpty()]
	[Parameter(Position=0,Mandatory=$True,HelpMessage="Enter a message for the dialog box")]
	[string]$Text,
	[string]$Title = $installTitle,	
	[string]$Buttons = "OK",
	[string]$Icon = "None",
	[string]$Timeout = 0 # Never times out
	)

	# Bypass if in totall silent mode
	If ($deployModeNonInteractive -eq $true) { 
		Write-Log "Bypassing Dialog Box [Mode: $deployMode]... $Text"
		Return 
	}

	Write-Log "Displaying Dialog Box... $Taxt"

	$dialogButtons = @{ 
		"OK" = 0
		"OKCancel" = 1
		"AbortRetryIgnore" = 2
		"YesNoCancel" = 3
		"YesNo" = 4
		"RetryCancel" = 5
		"CancelTryAgainContinue" = 6
	}

	$dialogIcons = @{ 
		"None" = 0
		"Stop" = 16
		"Question" = 32
		"Exclamation" = 48
		"Information" = 64
	} 

	$wshell = New-Object -COMObject WScript.Shell
	$response = $wshell.Popup($Text,$Timeout,$Title,$dialogButtons[$Buttons]+$dialogIcons[$Icon])

	Switch ($response) { 
		1 {	
			Write-Log "Dialog Box Response: OK"
			Return "OK"
		} 
		2 {	
			Write-Log "Dialog Box Response: Cancel"
			Return "Cancel"
		} 
		3 {	
			Write-Log "Dialog Box Response: Abort"
			Return "Abort"
		} 
		4 {	
			Write-Log "Dialog Box Response: Retry"
			Return "Retry"
		} 
		5 {	
			Write-Log "Dialog Box Response: Ignore"
			Return "Ignore"
		} 
		6 {	
			Write-Log "Dialog Box Response: Yes"
			Return "Yes"
		} 
		7 {	
			Write-Log "Dialog Box Response: No"
			Return "No"
		} 
		-1 {	
			Write-Log "Dialog Box timed out..."
			Return "Timeout"
		} 
	}
}

Function Get-HardwarePlatform {
<# 
.SYNOPSIS
	Retrieves information about the hardware platform (physical or virtual)
.DESCRIPTION
	Retrieves information about the hardware platform (physical or virtual)
.EXAMPLE
	Get-HardwarePlatform
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Try {
		$hwBios = Get-WmiObject Win32_BIOS | Select-Object "Version","SerialNnumber"
		$hwMakeModel = Get-WMIObject Win32_ComputerSystem | Select-Object "Model","Manufacturer"
	}
	Catch {
		Write-Log: "Error retrieving hardware platform information."
	}

	If ($hwBIOS.Version -match "VRTUAL") {$hwType = "Virtual:Hyper-V"}
	ElseIf ($hwBIOS.Version -match "A M I") {$hwType = "Virtual:Virtual PC"}
	ElseIf ($hwBIOS.Version -like "*Xen*") {$hwType = "Virtual:Xen"}
	ElseIf ($hwBIOS.SerialNumber -like "*VMware*") {$hwType = "Virtual:VMWare"}
	ElseIf ($hwMakeModel.manufacturer -like "*Microsoft*") {$hwType = "Virtual:Hyper-V"}
	ElseIf ($hwMakeModel.manufacturer -like "*VMWare*") {$hwType = "Virtual:VMWare"}
	ElseIf ($hwMakeModel.model -like "*Virtual*") {$hwType = "Virtual"}
	Else {$hwType = "Physical"}
	Return $hwType
}

Function Get-InstalledApplication {
<# 
.SYNOPSIS
	Retrieves information about installed applications.
.DESCRIPTION
	Retrieves information about installed applications by querying the registry. You can specify an application name, a product code, or both.
	Returns information about application publisher, name & version, product code, uninstall string, install source, location & date.
.EXAMPLE
	Get-InstalledApplication -Name "Adobe Flash"
.EXAMPLE
	Get-InstalledApplication -ProductCode "{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}"
.PARAMETER ApplicationName
	The name of the application you want to retrieve information on. Performs a wildcard match on the application display name.
.PARAMETER ProductCode
	The product code of the application you want to retrieve information on.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[array]$Name = "",
		[string]$ProductCode = ""
	)	

	If ($name -ne "") { Write-Log "Getting information for installed Application Name [$name]..."}
	If ($productCode -ne "") { Write-Log "Getting information for installed Product Code [$ProductCode]..."}
	If ($name -eq "" -and $ProductCode -eq "") { Write-Log "Get-InstalledApplication Error: Please provide an Application Name or Product Code."; Return $null }
	# Replace special characters in product code that interfere with regex match
	$productCode = $productCode -replace "}","" -replace "{",""
	$applications = $name -split (",")
	$installedApplication = @()
	Foreach ($regKey in $regKeyApplications ) {
		If (Test-Path $regKey -ErrorAction SilentlyContinue) {		  
		$regKeyApplication = Get-ChildItem $regKey -ErrorAction SilentlyContinue | ForEach-Object {Get-ItemProperty $_.PsPath}
			Foreach ($regKeyApp in $regKeyApplication) {
				If ($ProductCode -ne "") {			   
					# Replace special characters in product code that interfere with regex match
					$regKeyProductCode = $($regKeyApp.PSChildName) -replace "}","" -replace "{",""
					# Verify if there is a match with the product code passed to the script
					If ($regKeyProductCode -match $productCode) {
						Write-Log "Found installed application [$($regKeyApp.DisplayName)] version [$($regKeyApp.DisplayVersion))] matching product code [$productCode]"
						$installedApplication += New-Object PSObject -Property @{
							ProductCode	=		$regKeyApp.PSChildName
							DisplayName	= 		$regKeyApp.DisplayName
							DisplayVersion =	$regKeyApp.DisplayVersion
							UninstallString =	$regKeyApp.UninstallString
							InstallSource =		$regKeyApp.InstallSource
							InstallLocation =	$regKeyApp.InstallLocation
							InstallDate =		$regKeyApp.InstallDate
							Publisher =			$regKeyApp.Publisher
						}   
					}
				}				
				If ($name -ne "") {
					# Verify if there is a match with the application name(s) passed to the script
					Foreach ($application in $applications) {
						If ($regKeyApp.DisplayName -match $application ) {
							Write-Log "Found installed application [$($regKeyApp.DisplayName)] version [$($regKeyApp.DisplayVersion))] matching application name [$application]"
							$installedApplication += New-Object PSObject -Property @{
								ProductCode	=		$regKeyApp.PSChildName
								DisplayName	= 		$regKeyApp.DisplayName
								DisplayVersion =	$regKeyApp.DisplayVersion
								UninstallString =	$regKeyApp.UninstallString
								InstallSource =		$regKeyApp.InstallSource
								InstallLocation =	$regKeyApp.InstallLocation
								InstallDate =		$regKeyApp.InstallDate
								Publisher =			$regKeyApp.Publisher
							}
						}
					}
				}
			}
		}
	}
	Return $installedApplication
}

Function Execute-MSI {
<# 
.SYNOPSIS
	Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.
.DESCRIPTION
	Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.
	Sets default switches to be passed to msiexec based on the preferences in the XML configuration file, e.g. "REBOOT=ReallySuppress /QB!"
	Automatically generates a log file name and creates a verbose log file for all msiexec operations.
	NB: Expects the MSI or MSP file to be located in the "Files" sub directory of the App Deploy Toolkit. Expects transform files to be in the same directory as the MSI file.
.EXAMPLE
	Give an example of how to use it
.EXAMPLE
	Give another example of how to use it
.PARAMETER Action
	The action to perform ["Install","Uninstall","Patch","Repair","ActiveSetup"]
.PARAMETER Path
	The path to the MSI/MSP file or the product code of the installed MSI.
.PARAMETER Transform
	The name of the transform file(s). The transform file is expected to be in the same directory as the MSI file.
.PARAMETER Parameters
	Overrides the default parameters specified in the XML configuration file. Install default is "REBOOT=ReallySuppress /QB!", uninstall default is "REBOOT=ReallySuppress /QN"
.PARAMETER LogName
	Overrides the default log file name. 
	The default log file name is generated from the MSI file name or for uninstallations, the product code is resolved to the displayname and version of the application.
.PARAMETER WorkingDirectory
	Overrides the working directory.
	The working directory is set to the location of the MSI file.
.PARAMETER ContinueOnError
	Continue if an exit code is returned by msiexec that is not recognised by the App Deploy Toolkit.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param(
		[ValidateSet("Install","Uninstall","Patch","Repair","ActiveSetup")] 
		[string] $Action = $null,
		[string] $Path = $(throw "Path to MSI/MSP file required or Product Code required"),
		[string] $Transform = $null,
		[string] $Parameters = $null,
		[string] $LogName = $null,
		[string] $WorkingDirectory,
		[switch] $ContinueOnError = $false
	)   

	# Build the log file name
	If (!($logName)) {
		# If the path matches a product code, resolve the product code to an application name and version
		If ($path -match "^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$") {
			Write-Log "Execute-MSI: Product code specified, attempting to resolve product code to an application name and version..."
			$productCodeNameVersion = (Get-InstalledApplication -ProductCode $path | Select DisplayName,DisplayVersion -ErrorAction SilentlyContinue)
			If ($productCodeNameVersion -ne $null) {
				If ($($productCodeNameVersion.Publisher) -ne "" -and $($productCodeNameVersion.Publisher) -ne $null) {
					$logName = ($productCodeNameVersion.Publisher + "_" + $productCodeNameVersion.DisplayName + "_" + $productCodeNameVersion.DisplayVersion) -replace " ",""
				}
				Else {
					$logName = ( $productCodeNameVersion.DisplayName + "_" + $productCodeNameVersion.DisplayVersion) -replace " ",""
				}
			}
			Else {
				$logName = (([System.IO.FileInfo]$path).BaseName)
			}		   
		}
		Else {
			$logName = (([System.IO.FileInfo]$path).BaseName)
		}		
	}

	# Build the log file path
	$logPath = Join-Path $configDirLogs $logName
	
	# Set the installation Parameters
	$msiUninstallDefaultParams = $configMSISilentParams
	If ($deployModeSilent -eq $true) {
		$msiInstallDefaultParams = $configMSISilentParams
	}
	Else {
		$msiInstallDefaultParams = $configMSIInstallParams
	}	

	# Build the MSI Parameters
	Switch ($action) {
		"Install" 			{ $option = "/i"; $msiLogFile = $logPath + "_Install"; $msiDefaultParams = $msiInstallDefaultParams }
		"Uninstall" 		{ $option = "/x"; $msiLogFile = $logPath + "_Uninstall"; $msiDefaultParams = $msiUninstallDefaultParams }
		"Patch" 			{ $option = "/update"; $msiLogFile = $logPath + "_Patch"; $msiDefaultParams = $msiInstallDefaultParams }
		"Repair" 			{ $option = "/f"; $msiLogFile = $logPath + "_Repair"; $msiDefaultParams = $msiInstallDefaultParams }
		"ActiveSetup" 		{ $option = "/fups"; $msiLogFile = $logPath + "_ActiveSetup" }
	}

	# Append .log to the logfile path and enclose in quotes
	If (([System.IO.FileInfo]$msiLogFile).Extension -ne "log") {
		$msiLogFile = $msiLogFile + ".log"
		$msiLogFile = "`"$msiLogFile`""
	}

	# If the MSI is in the Files directory, set the full path to the MSI
	If (Test-Path (Join-Path $dirFiles $path -ErrorAction SilentlyContinue) -ErrorAction SilentlyContinue) {
		$msiFile = (Join-Path $dirFiles $path)
	}
	Else { 
		$msiFile = $Path
	}

	# Set the working directory of the MSI
	$workingDirectory = Split-Path $msiFile -Parent

	# Enclose the MSI file in quotes to avoid issues with spaces when running msiexec
	$msiFile = "`"$msiFile`""
	# Enclose the MST file in quotes to avoid issues with spaces when running msiexec
	$mstFile = "`"$transform`""

	If ($transform -and $Parameters) {
		$argsMSI = "$option $msiFile TRANSFORMS=$mstFile $Parameters $configMSILoggingOptions $msiLogFile"		
	}
	ElseIf ($transform)	{
		$argsMSI = "$option $msiFile TRANSFORMS=$mstFile $msiDefaultParams $configMSILoggingOptions $msiLogFile"
	}
	ElseIf ($Parameters) {
		$argsMSI = "$option $msiFile $Parameters $configMSILoggingOptions $msiLogFile"		
	}
	Else {
		$argsMSI = "$option $msiFile $msiDefaultParams $configMSILoggingOptions $msiLogFile"
	}

	# Call the Execute-Process function
	If ($ContinueOnError -eq $true) {
		Execute-Process -FilePath $exeMsiexec -Arguments $argsMSI -WorkingDirectory $WorkingDirectory -WindowStyle Normal -ContinueOnError
	}
	Else {
		Execute-Process -FilePath $exeMsiexec -Arguments $argsMSI -WorkingDirectory $WorkingDirectory -WindowStyle Normal
	}
}

Function Remove-MSIApplications { 
<# 
.SYNOPSIS
	Removes all MSI applications matching the specified application name
.DESCRIPTION
	Removes all MSI applications matching the specified application name. 
	Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code, provided the uninstall string matches "msiexec"
.EXAMPLE
	Remove-MSIApplications "Adobe Flash"
	Removes all versions of software that match the name "Adobe Flash"
.EXAMPLE
	Remove-MSIApplications "Adobe"
	Removes all versions of software that match the name "Adobe"
.PARAMETER Name
	The name of the application you want to uninstall.
.PARAMETER ContinueOnError
	Continue if an exit code is returned by msiexec that is not recognised by the App Deploy Toolkit.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param( 
		[Parameter(Mandatory = $true)]
		[string] $Name,
		[switch] $ContinueOnError = $false
	)

	$installedApplications = Get-InstalledApplication $name
	If ($installedApplications -ne "") {
		Foreach ($installedApplication in $installedApplications) {
			If ($installedApplication.UninstallString -match "msiexec") {
				Write-Log "Removing Application [$($installedApplication.DisplayName) $($installedApplication.Version)]..."  			
				If ($ContinueOnError -eq $true) {
					Execute-MSI -Action Uninstall -Path $installedApplication.ProductCode -ContinueOnError
				}
				Else {
					Execute-MSI -Action Uninstall -Path $installedApplication.ProductCode
				}
			}
			Else {
				Write-Log "$($installedApplication.DisplayName) uninstall string [$($installedApplication.UninstallString)] does not match `"msiexec`", so removal will not proceed."
			}
		}
	}
}

Function Execute-Process {
<# 
.SYNOPSIS
	Function to execute a process, with optional arguments, working directory, window style.
.DESCRIPTION
	Executes a process, e.g. a file included in the Files directory of the App Deploy Toolkit, or a file on the local machine.
	Provides various options for handling the return codes (see Parameters)
.EXAMPLE
	Execute-Process -FilePath "uninstall_flash_player_64bit.exe" -Arguments "/uninstall" -WindowStyle Hidden
	If the file is in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.
.EXAMPLE
	Execute-Process -FilePath "$dirFiles\Bin\setup.exe" -Arguments "/S" -WindowStyle Hidden
.EXAMPLE
	Execute-Process -FilePath "setup.exe" -Arguments "/S" -IgnoreExitCodes "1,2"
.PARAMETER FilePath
	Path of the file you want to execute. 
	If the file is located directly in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.
	Otherwise, the full path of the file must be specified. If the files is in a subdirectory of "Files", use the "$dirFiles" variable as shown in the example above.
.PARAMETER Arguments
	Arguments to be passed to the executable
.PARAMETER WindowStyle
	Style of the window of the process executed: "Normal","Hidden","Maximized","Minimized" [Default is "Normal"]
.PARAMETER WorkingDirectory
	The working directory used for executing the process.
	Defaults to the directory of the file being executed.
.PARAMETER PassThru
	Returns STDOut and STDErr output from the process.
.PARAMETER IgnoreExitCodes
	List the exit codes you want to ignore.
.PARAMETER ContinueOnError
	Continue if an exit code is returned by the process that is not recognised by the App Deploy Toolkit.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param(
	[string] $FilePath = $(throw "Command Param required"),
	[Array] $Arguments = @(),
	[ValidateSet("Normal","Hidden","Maximized","Minimized")]
	[System.Diagnostics.ProcessWindowStyle] $WindowStyle = "Normal",
	[string] $WorkingDirectory = (Split-Path $FilePath -Parent),
	[switch] $PassThru = $false,
	[string] $IgnoreExitCodes = $false,
	[switch] $ContinueOnError = $false
	)

	# If the file is in the Files subdirectory of the App Deploy Toolkit, set the full path to the file
	If (Test-Path (Join-Path $dirFiles $FilePath -ErrorAction SilentlyContinue) -ErrorAction SilentlyContinue) {
		$FilePath = (Join-Path $dirFiles $FilePath)
	}

	Write-Log "Executing [$FilePath $Arguments]..." 
	If ($workingDirectory -ne "") { Write-Log "Working Directory is [$WorkingDirectory]" }

	# Disable Zone checking to prevent warnings when running executables from a Distribution Point
	$env:SEE_MASK_NOZONECHECKS = 1

	$processStartInfo = New-Object System.Diagnostics.ProcessStartInfo
	$processStartInfo.FileName = "$FilePath"
	$processStartInfo.WorkingDirectory = "$WorkingDirectory"
	$processStartInfo.UseShellExecute = $false
	$processStartInfo.RedirectStandardOutput = $true
	$processStartInfo.RedirectStandardError = $true
	If ($arguments.Length -gt 0) { $processStartInfo.Arguments = $Arguments }
	If ($windowStyle) {$processStartInfo.WindowStyle = $WindowStyle}

	$process = [System.Diagnostics.Process]::Start($processStartInfo)

	$stdOut = $process.StandardOutput.ReadToEnd() -replace "`0",""
	$stdErr = $process.StandardError.ReadToEnd() -replace "`0",""
	$processName = $process.ProcessName

	If($stdOut.length -gt 0) { Write-Log $stdOut}
	If($stdErr.length -gt 0) { Write-Log $stdErr}

	$returnCode = $process.ExitCode
	$process.WaitForExit()	

	# Re-enable Zone checking
	Remove-Item env:\SEE_MASK_NOZONECHECKS -ErrorAction SilentlyContinue

	# Check to see whether we should ignore exit codes
	$ignoreExitCodeMatch = $false
	If ($ignoreExitCodes -ne "") {
		# Create array to store the exit codes
		$ignoreExitCodesArray = @()	
		# Split the processes on a comma
		$ignoreExitCodesArray = $IgnoreExitCodes -split(",")
		ForEach ($ignoreCode in $ignoreExitCodesArray) {
			If ($returnCode -eq $ignoreCode) { 
				$ignoreExitCodeMatch = $true 
			}
		}
	}
	# Or always ignore exit codes
	If ($ContinueOnError -eq $true) {
		$ignoreExitCodeMatch = $true 
	}

	# If the passthru switch is specified, return the exit code and any output from process
	If ($PassThru -eq $true) {
		New-Object PSObject -Property @{
			ExitCode	= $returnCode
			StdOut		= $stdOut
			StdErr		= $stdErr
		}
		Write-Log "Execution completed with return code $returnCode."
	}
	ElseIf ($ignoreExitCodeMatch -eq $true) {
		Write-Log "Execution complete and the return code $returncode has been ignored."
	}
	ElseIf ( ($returnCode -eq 3010) -or ($returnCode -eq 1641) ) {
		Write-Log "Execution completed successfully with return code $returnCode. A reboot is required."
		Set-Variable -Name msiRebootDetected -Value $true -Scope Script
	}
	ElseIf ( ($returnCode -eq 1605) -and ($filePath -eq $exeMsiexec)) {
		Write-Log "Execution did not complete, because the product is not currently installed."
	}
	ElseIf ( ($returnCode -eq -2145124329) -and ($filePath -eq $exeWusa)) {
		Write-Log "Execution did not complete, because this Windows Update is not applicable to this system."
	}
	ElseIf ( ($returnCode -eq 17025) -and ($filePath -match "fullfile")) {
		Write-Log "Execution did not complete, because the Office Update is not applicable to this system."
	}
	ElseIf ($returnCode -eq 0) {
		Write-Log "Execution completed successfully with return code $returnCode."
	}
	Else {
		Write-Log ("Execution failed with code: " + $returnCode)
		Exit-Script $returnCode 
	}

	Trap [Exception] {
	Write-Log ("Execution failed: " + $_.Exception.Message)
	Exit-Script $returnCode
	}   
}

Function Copy-File {
<# 
.SYNOPSIS
	Function to copy a file to a destination path.
.DESCRIPTION
	Function to copy a file to a destination path.
.EXAMPLE
	Copy-File -Path "$dirSupportFiles\MyApp.ini" -Destination "$envWindir\MyApp.ini"
.PARAMETER Path
	Path of the file you want to copy
.PARAMETER Destination
	Destination Path of the file to copy
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>	Param(
		[Parameter(Mandatory = $true)]
		[string]$Path = $(throw "Path param required"),
		[Parameter(Mandatory = $true)]
		[string]$Destination = $(throw "Destination param required")
	)

	Write-Log "Copying File [$path] to [$destination]..."

	Copy-Item -Path "$Path" -Destination "$destination" -ErrorAction "STOP" -Force | Out-Null

	Trap [Exception] {
		Write-Log $("Could not copy file [$path] to [$destination]:" + $_.Exception.Message)
		Continue
	}
}

Function Remove-File {
<# 
.SYNOPSIS
	Function to remove a file or all files recursively in a given path.
.DESCRIPTION
	Function to remove a file or all files recursively in a given path.
.EXAMPLE
	Remove-File -Path "C:\Windows\Downloaded Program Files\Temp.inf"
.EXAMPLE
	Remove-File -Path "C:\Windows\Downloaded Program Files" -Recurse
.PARAMETER Path
	Path of the file you want to remove
.PARAMETER Recurse
	Optionally, remove all files recursively in a directory
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param(
		[Parameter(Mandatory = $true)]
		[string]$Path = $(throw "Path Param required"),
		[switch]$Recurse
	)

	Write-Log "Deleting File(s) [$path]..."
	If ($Recurse) {
		Remove-Item -Path "$path" -ErrorAction "STOP" -Force -Recurse | Out-Null
	}
	Else {
		Remove-Item -Path "$path" -ErrorAction "STOP" -Force | Out-Null
	}
	Trap [Exception] {
		Write-Log $("Could not delete file [$path]:" + $_.Exception.Message)
		Continue
	}
}

Function Convert-RegistryPath {
<# 
.SYNOPSIS
	Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.
.DESCRIPTION
	Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.
	Converts registry key hives to their full paths, e.g. HKLM is converted to "HKEY_LOCAL_MACHINE" and prepends "Registry::" to the path
.EXAMPLE
	Convert-RegistryPath -Key "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}"
.EXAMPLE
	Convert-RegistryPath -Key "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}"
.PARAMETER Key
	Path to the registry key to convert (can be a registry hive or fully qualified path)
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
Param ( 
		[Parameter(Mandatory = $true)]
		$Key			
	)
	# Convert the registry key hive to the full path
	If ($Key -match "HKLM|HKCU|HKCR|HKU|HKCC|HKPD") {
		$key = $key -Replace("HKLM","HKEY_LOCAL_MACHINE")
		$key = $key -Replace("HKCR","HKEY_CLASSES_ROOT")
		$key = $key -Replace("HKCU","HKEY_CURRENT_USER")
		$key = $key -Replace("HKU","HKEY_USERS")  
	}
	$key = $key -Replace(":","")
	# Append the PowerShell drive to the registry key path   
	$key = Join-Path "Registry::" $key
	Return $key
}

Function Get-RegistryKey {
<# 
.SYNOPSIS
	Retrieves value names and value data for a specified registry key
.DESCRIPTION
	Retrieves value names and value data for a specified registry key.
	If the registry key does not contain any values, the function will return $null. If you need to test for existence of a registry key path, use the built-in Test-Path cmdlet
.EXAMPLE
	Get-RegistryKey "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}"
.EXAMPLE
	Get-RegistryKey "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\iexplore.exe"
.PARAMETER Key
	Path of the registry key
.NOTES	
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param ( 
		[Parameter(Mandatory = $true)]
		$Key
	)

	$key = Convert-RegistryPath -Key $key

	Write-Log "Getting Registry key [$key] ..."

	# Check if the registry key exists
	If (Test-Path -Path $key -ErrorAction SilentlyContinue) {
		$regKeyValue = Get-ItemProperty -Path $key 
		If ($regKeyValue -ne "") {
			Return $regKeyValue
		}
		Else {
			Return $null
		}
		Trap [Exception] {
			Write-Log $("Registry key does not exist: [$key]" + $_.Exception.Message)
			Continue
		}		
	}
	Else {
		Write-Host "Registry key does not exist: [$key]"
	}
}

Function Set-RegistryKey {
<# 
.SYNOPSIS
	Creates a registry key name, value or value data or sets the same if it does not already exist.
.DESCRIPTION
	Creates a registry key name, value or value data or sets the same if it does not already exist.
.EXAMPLE
	Set-RegistryKey -Key $blockedAppPath -Name "Debugger" -Value $blockedAppDebuggerValue
.EXAMPLE
	Set-RegistryKey -Key "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce" -Name "Debugger" -Value $blockedAppDebuggerValue -Type String
.PARAMETER Key
	The registry key path
.PARAMETER Name
	The value name
.PARAMETER Value
	The value data
.PARAMETER Type
	The type of registry value to create or set [Default is "String" 
	Acceptable values are: "Binary","DWord","ExpandString","MultiString","None","QWord","String","Unknown"
	Object type: [Microsoft.Win32.RegistryValueKind]
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (		
		[Parameter(Mandatory = $true)] 
		[System.String]$Key, 	
		[System.String]$Name, 
		[System.String]$Value, 
		[Microsoft.Win32.RegistryValueKind]$Type="String"
	)

	$key = Convert-RegistryPath -Key $Key

	# Create registry key if it doesn't exist
	If (!(Test-Path $key -ErrorAction SilentlyContinue)) { 
		Write-Log "Creating Registry key [$key]..."
		New-Item -Path $key -ItemType Registry -Force | Out-Null 
		Trap [Exception] {
			Write-Log $("Failed to create registry key [$Key]" + $_.Exception.Message)
			Continue
		}
	}

	If ($Name) {
		# Set registry value if it doesn't exist
		If ((Get-ItemProperty -Path $key -Name $Name -ErrorAction SilentlyContinue) -eq $null) {
			Write-Log "Setting registry key [$key] [$name = $value]..."
			New-ItemProperty -Path $key -Name $name -Value $value -PropertyType $type | Out-Null
		}
		# Update registry value if it does exist
		Else { 
			Write-Log "Updating registry key: [$key] [$name = $value]..."
			Set-ItemProperty -Path $key -Name $name -Value $value | Out-Null
		}
		Trap [Exception] {
			Write-Log $("Failed to set registry value [$value] for registry key [$key] [$name]" + $_.Exception.Message)
			Continue
		}
	}
}

Function Remove-RegistryKey {
<# 
.SYNOPSIS
	Deletes the specified registry key or value
.DESCRIPTION
	Deletes the specified registry key or value
.EXAMPLE
	Remove-RegistryKey -Key "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
.EXAMPLE
	Remove-RegistryKey -Key "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "RunAppInstall"
.PARAMETER Key
	Path of the registry key to delete
.PARAMETER Name
	Name of the registry key value to delete
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param(
		[Parameter(Mandatory = $true)]
		[string]$Key = $(throw "Key Param required"),
		[string]$Name,
		[switch]$Recurse
	)

	$key = Convert-RegistryPath -Key $key

	If (!($name)) {
		Write-Log "Deleting Registry Key [$key]..."
		If ($Recurse) {
			Remove-Item -Path $Key -ErrorAction "STOP" -Force -Recurse | Out-Null
		}
		Else {
			Remove-Item -Path $Key -ErrorAction "STOP" -Force | Out-Null
		}
		Trap [Exception] {
			Write-Log $("Failed to delete registry key [$Key]:" + $_.Exception.Message)
			Continue
		}
	}
	Else {
		Write-Log "Deleting Registry Value [$Key] [$name] ..."
		Remove-ItemProperty -Path $Key -Name $Name -ErrorAction "STOP" -Force | Out-Null
		Trap [Exception] {
			Write-Log $("Failed to delete registry value [$Key] [$name]:" + $_.Exception.Message)
			Continue
		}
	}
}

Function Get-FileVersion {
<# 
.SYNOPSIS
	Gets the version of the specified file
.DESCRIPTION
	Gets the version of the specified file
.EXAMPLE
	Get-FileVersion "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"	
.PARAMETER File
	Path of the file
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[Parameter(Mandatory = $true)]
		[string]$File
	)

	Write-Log "Getting file version info for [$file]..."

	If (Test-Path $File) {
		Try {
			$fileVersion = (Get-Command $file).FileVersionInfo.FileVersion
			If ($fileVersion -ne $null) {
				# Remove product information to leave only the file version
				$fileVersion = ($fileVersion -split " " | Select -First 1)
				Write-Log "File version is [$fileVersion]"
				Return $fileVersion
				}
			}
		Catch {
			Write-Log "Error getting file version info."
		}
	}
	Else {
		Write-Log "File could not be found."
	}
}

Function New-Shortcut {
<# 
.SYNOPSIS
	Creates a new shortcut .lnk file, which can be used for example on the start menu.
.DESCRIPTION
	Creates a new shortcut .lnk file, with configurable options.
.EXAMPLE
	New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu" -TargetPath "$envWinDir\system32\notepad.exe" -IconLocation "$envWinDir\system32\notepad.exe" -Description "Notepad" -WorkingDirectory "$envHomeDrive\$envHomePath"
.PARAMETER Path
	Path to save the shortcut
.PARAMETER TargetPath
	Target path that the shortcut launches
.PARAMETER Arguments
	Arguments to be passed to the target path
.PARAMETER IconLocation
	Location of the icon used for the shortcut
.PARAMETER Description
	Description of the shortcut
.PARAMETER WorkingDirectory
	Working Directory to be used for the target path
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
	[Parameter(Mandatory = $true)]
	[string]$Path,
	[Parameter(Mandatory = $true)]
	[string]$TargetPath,
	[string]$Arguments,
	[string]$IconLocation,
	[string]$Description,
	[string]$WorkingDirectory
	)

	$PathDirectory = ([System.IO.FileInfo]$Path).DirectoryName
	If (!(Test-Path -Path $PathDirectory)) {
		Write-Log "Creating Shortcut Directory..."
		New-Item -ItemType Directory -Path $PathDirectory -ErrorAction SilentlyContinue -Force | Out-Null
	}

	Write-Log "Creating shortcut [$path]..."
	$shortcut = $shell.CreateShortcut($path)
	$shortcut.TargetPath = $targetPath
	$shortcut.Arguments = $arguments
	$shortcut.IconLocation = $iconLocation
	$shortcut.Description = $description
	$shortcut.WorkingDirectory = $workingDirectory
	$shortcut.Save()
}

# Function to refresh the Windows Explorer Desktop (forces icons to refresh)
Function Refresh-Desktop {
<# 
.SYNOPSIS
	Forces the Windows Exporer Shell to refresh, which causes desktop icons to be reloaded
.DESCRIPTION
	Forces the Windows Exporer Shell to refresh, which causes desktop icons to be reloaded.
	Informs the Explorer Shell to refresh its settings after you change registry values or other settings to avoid a reboot.
.EXAMPLE
	Refresh-Desktop
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Write-Log "Refreshing Desktop..."

	$refreshDesktopCode = @'
private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff); 
private const int WM_SETTINGCHANGE = 0x1a; 
private const int SMTO_ABORTIFHUNG = 0x0002; 
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError=true, CharSet=CharSet.Auto)] static extern bool SendNotifyMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SendMessageTimeout ( IntPtr hWnd, int Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult ); 
[System.Runtime.InteropServices.DllImport("Shell32.dll")] private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

public static void Refresh()  {
	SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
	SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 100, IntPtr.Zero); 
}
'@

	Add-Type -MemberDefinition $refreshDesktopCode -Namespace MyWinAPI -Name Explorer 
	[MyWinAPI.Explorer]::Refresh()

	Trap [Exception] {
		Write-Log $("Error refreshing Desktop:" + $_.Exception.Message)
		Continue
	}

}

# Function to get scheduled task information
Function Get-ScheduledTask { 
<# 
.SYNOPSIS
	Retrieves a list of the scheduled tasks on the local computer
.DESCRIPTION
	Retrieves a list of the scheduled tasks on the local computer and returns them as an array
.EXAMPLE
	Get-ScheduledTask
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	SchTasks.exe /Query /FO CSV | ConvertFrom-Csv
}

Function Block-AppExecution {
<# 
.SYNOPSIS
	Function to block the execution of an application(s)
.DESCRIPTION
	This function is called when you pass the -BlockExecution parameter to the Stop-RunningApplications function. It does the following:
	1. Makes a copy of this script in a temporary directory on the local machine.
	2. Backs up the "Image File Execution Options" (IFEO) as a PowerShell CLIXML file.
	3. Checks for an existing scheduled task from previous failed installation attemp where apps were blocked and if found, calls the Unblock-AppExecution function to restore the original IFEO registry keys. 
	   This is to prevent the function from overriding the backup of the original IFEO options.
	4. Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameters -CleanupBlockedApps and -BlockedAppInstallName  
	5. Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameters -ShowBlockedAppDialog and -BlockedAppInstallName
	6. When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress. 
	   The text of this message can be customized in the XML configuration file.
.EXAMPLE
	Block-AppExecution -ProcessName "winword,excel"
.PARAMETER ProcessName
	Name of the process or processes separated by commas
.NOTES
	This is an internal script function and should typically not be called directly. It is used by the Stop-RunningApplications function when the -BlockExecution parameter is specified.
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
Param(
	[Parameter(Mandatory = $true)]
	$ProcessName # Specify process names separated by commas 
	)

	# Bypass if in NonInteractive mode
	If ($deployModeNonInteractive -eq $true) { 
		Write-Log "Bypassing Block-AppExecution Function [Mode: $deployMode]"
		Return
	}

	Write-Log "Invoking Block-AppExecution Function..."
	$schTaskBlockedAppsName = "$installName" + "_BlockedApps"
	$xmlBlockedApps = Join-Path $dirBlockedApps ($installName + "_BlockedApps.xml") 
	# If there is an existing scheduled task from a failed installation for this application, run that now to restore the original IFEO keys before we back them up again.
	If ((Get-ScheduledTask | Select TaskName | Where { $_.TaskName -eq "\$schTaskBlockedAppsName" } ) -ne $null) {
		Write-Log "Existing Scheduled Task detected [$schTaskBlockedAppsName]. UnBlock-AppExecution will be called." 
		Unblock-AppExecution
	}

	# Create array to store the state of the registry keys we need to change so that we can restore them later
	$blockedApps = @()

	$blockProcessName = $processName
	# Append .exe to match registry keys
	$blockProcessName = $blockProcessName | Foreach-Object { $_ + ".exe" } -ErrorAction SilentlyContinue

	# Enumerate the Image File Execution Options registry keys
	$regKeyAppExePath = Get-ChildItem -Path $regKeyAppExecution -ErrorAction SilentlyContinue
	# Enumerate each process we want to block
	Foreach ($blockProcess in $blockProcessName) {
		# Reset variables for each loop
		$regKeyExeExists = $false
		$regValueDebugger = ""
		# Enumerate each process in the IFEO reg key to see if they match our block processes
		Foreach ($regKeyAppExe in $regKeyAppExePath ) {
			$appExeName = ($regKeyAppExe | Select PSChildName -ExpandProperty PSChildName -ErrorAction SilentlyContinue)
			If ($blockProcess -eq $appExeName) {
				$regKeyExeExists = $true
				# Get the current debugger value if it exists and replace null values with empty strings to prevent null values in XML which cause errors on import of XML
				$regValueDebugger = ($regKeyAppExe | ForEach-Object {Get-ItemProperty $_.PsPath} | Where {$_.Debugger} | Select Debugger -ExpandProperty Debugger -ErrorAction SilentlyContinue) -replace ($null,"")
			}
		}
		# Create PS objects to store the state of the registry key
		$blockedApps += New-Object PSobject -Property @{
			Name = $blockProcess
			Path = (Join-Path $regKeyAppExecution $blockProcess)
			KeyExists = $regKeyExeExists
			DebuggerValue = $regValueDebugger
		}
	}
	# Make this variable available to the script
	Set-Variable -Name blockedApps -Value $blockedApps -Scope Script
	If (!(Test-Path $dirBlockedApps)) {
		New-Item -Path $dirBlockedApps -ItemType Directory -Force -ErrorAction SilentlyContinue | Out-Null
	} 
	Write-Log "Exporting original IFEO registry keys to XML [$xmlBlockedApps]..."
	$blockedApps | Export-Clixml -Path $xmlBlockedApps -Force

	# Copy Script to Temporary directory so it can be called by scheduled task later if required
	Copy-Item -Path "$scriptRoot\*.*" -Destination $dirAppDeployTemp -Force -Recurse -ErrorAction SilentlyContinue

	# Create a scheduled task to run on startup to call this script and cleanup blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
	Write-Log "Creating Scheduled task to cleanup blocked applications in case installation is interrupted..."
	If (Get-ScheduledTask | Select TaskName | Where { $_.TaskName -eq "\$schTaskBlockedAppsName" } ) {
		Write-Log "Scheduled task $schTaskBlockedAppsName already exists."
	}
	Else { 
		$schTaskCreation = Execute-Process -FilePath "schtasks.exe" -Arguments "/Create /TN $schTaskBlockedAppsName /RU System /SC ONSTART /TR `"powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -file $dirAppDeployTemp\$scriptFileName -CleanupBlockedApps -BlockedAppInstallName $installName`" " -PassThru
	}

	# Foreach blocked app, set a RunOnce Key to restore the original value in case of interruption (e.g. user shuts down during installation).
	# Then change the debugger value to block execution of the application.
	Foreach ($blockedApp in $blockedApps) {	
		$blockedAppName = $blockedApp | Select Name -ExpandProperty Name -ErrorAction SilentlyContinue
		$blockedAppPath = $blockedApp | Select Path -ExpandProperty Path -ErrorAction SilentlyContinue
		$blockedAppKeyExists = $blockedApp | Select KeyExists -ExpandProperty KeyExists -ErrorAction SilentlyContinue
		$blockedAppDebuggerValue = $blockedApp | Select DebuggerValue -Expand DebuggerValue -ErrorAction SilentlyContinue

		# Set the debugger value to block application execution
		Write-Log "Setting the Image File Execution Options registry keys to block execution of $blockedAppName..."	
		Set-RegistryKey -Key $blockedAppPath -Name "Debugger" -Value $debuggerBlockValue	
	}
}

Function UnBlock-AppExecution {
<# 
.SYNOPSIS
	Unblocks the execution of applications performed by the Block-AppExecution function
.DESCRIPTION
	This function is called by the Exit-Script function or when the script itself is called with the parameters -CleanupBlockedApps and -BlockedAppInstallName  
.EXAMPLE
	UnblockAppExecution
.NOTES
	This is an internal script function and should typically not be called directly.
	It is used when the -BlockExecution parameter is specified with the Stop-RunningApplications function to undo the acitons performed by Block-AppExecution.
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	# Bypass if in NonInteractive mode
	If ($deployModeNonInteractive -eq $true) { 
		Write-Log "Bypassing UnBlock-AppExecution Function [Mode: $deployMode]"
		Return
	}

	# Set these variables here so the function can be called on its own
	$schTaskBlockedAppsName = "$installName" + "_BlockedApps"
	$xmlBlockedApps = Join-Path $dirBlockedApps ($installName + "_BlockedApps.xml")

	# If the CleanupBlockedApps Parameter is specified, import the XML file to get the list of processes and the previous state of the registry
	If ($CleanupBlockedApps -eq $true) {
		Write-Log "CleanupBlockedApps specified."		
		If (Test-Path $xmlBlockedApps) {			
			Write-Log "Importing CliXML [$xmlBlockedApps]..."
			$blockedApps = Import-Clixml $xmlBlockedApps -ErrorAction SilentlyContinue
		}
		Else {
			Write-Log "Error: Could not find file [$xmlBlockedApps]..."
			Return
		}
	}

	Write-Log "Invoking UnBlock-AppExecution Function..."
	# Restore the original state of the IFEO registry key then remove the RunOnce Key that was set previousl
	Foreach ($blockedApp in $blockedApps) {	
		$blockedAppName = $blockedApp | Select Name -ExpandProperty Name -ErrorAction SilentlyContinue
		$blockedAppPath = $blockedApp | Select Path -ExpandProperty Path -ErrorAction SilentlyContinue
		$blockedAppKeyExists = $blockedApp | Select KeyExists -ExpandProperty KeyExists -ErrorAction SilentlyContinue
		$blockedAppDebuggerValue = $blockedApp | Select DebuggerValue -Expand DebuggerValue -ErrorAction SilentlyContinue

		Write-Log "Restoring the original Image File Execution Options registry key for $blockedAppName..."
		If ($blockedAppKeyExists -eq $true) {
			# If the Debugger value was previously set, restore the original value
			If ($blockedAppDebuggerValue -ne "" -and $blockedAppDebuggerValue -ne $null) {
				Set-RegistryKey -Key $blockedAppPath -Name "Debugger" -Value $blockedAppDebuggerValue
			}
			# If the Debugger value was not previously set, but the parent registry key existed, remove the value
			Else {
				Remove-RegistryKey -Key $blockedAppPath -Name "Debugger"
			}
		}
		# Otherwise, remove the registry key
		Else {
			Remove-RegistryKey -Key $blockedAppPath
		}
	}

	# Remove the XML file if it exists
	If (Test-Path $xmlBlockedApps) {
		Write-Log "Removing CliXML [$xmlBlockedApps]..."
		Remove-File -Path $xmlBlockedApps
	}

	# Remove the scheduled task if it exists
	If (Get-ScheduledTask | Select TaskName | Where { $_.TaskName -eq "\$schTaskBlockedAppsName" } ) {
		Write-Log "Deleting Scheduled Task [$schTaskBlockedAppsName] ..."
		Execute-Process -FilePath "schtasks.exe" -Arguments "/Delete /TN $schTaskBlockedAppsName /F"
	}
}

Function Stop-RunningApplications {
<# 
.SYNOPSIS
	This function prompts the user to close the specified running applications or optionally closes the applications without showing a prompt (using the -Silent" switch).   
.DESCRIPTION
	The user is presented with a dialog box to close the applications themselves and continue or to have the script close the applications for them.
	If the -AllowDefer option is specified, an optional "Defer" button will be shown to the user. If the user selects this option, the script will exit and return a 1618 code (SCCM fast retry code)
	Optionally, by using the -Silent switch, you can stop running processes without prompting the user at all.
	By specifying the -BlockExecution option, the user will be prevented from launching those same applications while the installation is in progress.
	The process descriptions are retrieved from WMI, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.
	The dialog box will timeout after 1 hour and 55 minutes to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).
.EXAMPLE
	Stop-RunningApplications "iexplore,winword,excel"
	Prompt the user to close Internet Explorer, Word and Excel.
.EXAMPLE
	Stop-RunningApplications "winword,excel" -Silent
	Close Word and Excel without prompting the user.
.EXAMPLE
	Stop-RunningApplications "winword,excel" -BlockExecution
	Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
.EXAMPLE
	Stop-RunningApplications "winword=Microsoft Office Word,excel=Microsoft Office Excel"
	Prompt the user to close Word and Excel, with customized descriptions for the applications.
.PARAMETER ProcessName
	Name of the process to stop (do not include the .exe)
.PARAMETER BlockExecution
	Option to prevent the user from launching the process/application
.PARAMETER AllowDefer
	Enables an optional defer button to allow the user to defer the installation if they do not want to close running applications.
.PARAMETER Silent
	Stop processes without prompting the user
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param(
	[Parameter(Mandatory = $true)]
	[string]$ProcessName, # Specify process names separated by commas. Optionally specify a process description with an equals symobol, e.g. "winword=Microsoft Office Word" 
	[switch]$BlockExecution = $false, # Specify whether to block execution of the processes during installation
	[switch]$AllowDefer = $false, # Specify whether to enable the optional defer button on the dialog box
	[switch]$Silent = $false # Specify whether to prompt user or force close the applications
	)

	# Create a Process object with custom descriptions where they are provided (split on a "=" sign)
	$processObjects = @()
	Foreach ($process in ($processName -split(",") | Where { $_ -ne ""})) { # Split multiple processes on a comma and join with the regex operator '|' to perform "or" match against multiple applications 
		$process = $process -split("=")	 
		$processObjects += New-Object PSObject -Property @{
			ProcessName =		   $process[0]
			ProcessDescription =	$process[1]  
		}
	}

	Write-Log "Checking for running applications [$(($processObjects.ProcessName) -Join ",")] ..."   

	# Join the process names with the regex operator '|' to perform "or" match against multiple applications
	$processNames = ($processObjects | Select ProcessName -ExpandProperty ProcessName) -join ("|")  
 
	# Replace escape characters that interfere with Regex and might cause false positive matches
	$processNames = $processNames -replace "\.","" -replace "\*",""	

	# If running in NonInteractive mode, force the processes to close silently
	If ($deployModeNonInteractive -eq $true) { $Silent = $true }

	# Force the processes to close silently, without prompting the user
	If ($Silent -eq $true) {		
		$runningProcesses = Get-Process | Where { ($_.ProcessName -replace "\.","" -replace "\*","") -match $processNames }
		If ($runningProcesses -ne $null) {
			Write-Log "Force closing application(s) [$processNames] without prompting user..."
			$runningProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
			Sleep -Seconds 2
		}		
	}
	
	# Otherwise prompt the user to close the applications
	Else {
		# Prompt the user as long as one of the matching processes are found running and store the processes description
		While (Get-Process | Where { ($_.ProcessName -replace "\.","" -replace "\*","") -match $processNames } | Select Name,Description,ID -OutVariable runningProcesses) {
			Write-Log "The following processes are running: [$(($runningProcesses.Name) -Join ",")]"
			Write-Log "Resolving process descriptions..."
			# Resolve the running process names to descriptions in the following precedence:
			# 1. The description of the process provided as a Parameter to the function, e.g. -ProcessName "winword=Microsoft Office Word".
			# 2. The description of the process provided by WMI
			# 3. Fall back on the process name	 
			Foreach ($runningProcess in $runningProcesses) {
				Foreach ($processObject in $processObjects) {
					If ($runningProcess.Name -eq $processObject.ProcessName) {
						If ( $processObject.ProcessDescription -ne $null ) {
							$runningProcess | Add-Member -type NoteProperty -name Description -value $processObject.ProcessDescription -Force -ErrorAction SilentlyContinue
						}
					}
				}
				# Fall back on the process name if no description is provided by the process or as a Parameter to the function
				If (!($runningProcess.Description)) {
					$runningProcess | Add-Member -type NoteProperty -name Description -value $runningProcess.Name -Force -ErrorAction SilentlyContinue
				}
			}
			$runningProcessDescriptions	= ($runningProcesses | Select Description -ExpandProperty Description | Select -Unique | Sort) -join ","

			# Minimize all open Windows (bring the script to the foreground)
			$shellApp = New-Object -ComObject "Shell.Application"
			$shellApp.MinimizeAll()

			Write-Log "Prompting user to close application(s) [$runningProcessDescriptions]"

			If ($allowDefer -eq $true) {
				$promptResult = Show-CloseProgramPrompt -ProcessDescriptions $runningProcessDescriptions -AllowDefer
			}
			Else {
				$promptResult = Show-CloseProgramPrompt -ProcessDescriptions $runningProcessDescriptions
			}

			# If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again
			If ($promptResult -eq "OK") {
				Write-Log "User selected to continue..."
				Sleep -Seconds 2
			}
			# Force the applications to close
			ElseIf ($promptResult -eq "Yes") {
				Write-Log "User selected to force the applications to close..."
				Stop-Process ($runningProcesses | Select ID -ExpandProperty ID) -Force -ErrorAction SilentlyContinue
				Sleep -Seconds 2
			}
			# Force the application to close (not actioned within a reasonable amount of time)
			ElseIf ($promptResult -eq "Abort") {
				Write-Log "Installation not actioned within a reasonable amount of time."
				$BlockExecution = $false
				Exit-Script 1618
			}
			# Force the application to close (user chose to defer)
			ElseIf ($promptResult -eq "No") {
				Write-Log "Installation deferred by the user."
				$BlockExecution = $false
				Exit-Script 1618
			}
		}		
	}
	# Force nsd.exe to stop if Notes is one of the required applications to close
	If ($processObjects.ProcessName -match "notes") {
		$notesPath = Get-Item $regKeyLotusNotes -ErrorAction SilentlyContinue | Get-ItemProperty | Select "Path" -ExpandProperty "Path"
		If ($notesPath -ne $null) {
			$notesNSDExecutable = Join-Path $notesPath "NSD.Exe"
			Try {
				If (Test-Path $notesNSDExecutable) {
					Write-Log "Executing $notesNSDExecutable with the -kill argument..."
					$notesNSDProcess = Start-Process -FilePath $notesNSDExecutable -ArgumentList "-kill" -WindowStyle Hidden -Wait -PassThru
				}
			}
			Catch {
				Write-Log "Failed to launch $notesNSDExecutable."
			}
			Write-Log @("$notesNSDExecutable returned exit code " + $notesNSDProcess.Exitcode)
			# Force NSD process to stop in case the previous command was not successful
			Stop-Process -Name "NSD" -Force -ErrorAction SilentlyContinue
		}

		# Get a list of all the executables in the Notes folder
		$notesPathExes = Get-ChildItem $notesPath -Filter "*.exe" -Recurse | Select BaseName -ExpandProperty BaseName
		# Strip all Notes processes from the process list except notes.exe, because the other notes processes (e.g. notes2.exe) may be invoked by the Notes installation, so we don't want to block their execution.
		$processesIgnoringNotesExceptions = Compare-Object -ReferenceObject ($processObjects | Select ProcessName -ExpandProperty ProcessName | Sort) -DifferenceObject ($notesPathExes | Sort) -IncludeEqual | Where {$_.SideIndicator -eq "<=" -or $_.InputObject -eq "notes" } | Select-Object InputObject -ExpandProperty InputObject		
		$processObjects = $processObjects | Where { $processesIgnoringNotesExceptions -contains $_.ProcessName }	  
	}

	Write-Log "Finished checking running applications."

	# If block execution switch is true, call the function to block execution of these processes
	If ($BlockExecution -eq $true) {
		# Make this variable globally available so we can check whether we need to call Unblock-AppExecution 
		Set-Variable -Name BlockExecution -Value $BlockExecution -Scope Script
		Write-Log "Block Execution Parameter specified."
		Block-AppExecution -ProcessName $processObjects.ProcessName
	}
}

#region
Function Show-CloseProgramPrompt {
<# 
.SYNOPSIS
	This function is called by Stop-RunningApplications to prompts the user to close the specified running applications
.DESCRIPTION
	The user is presented with a Windows Forms dialog box to close the applications themselves and continue or to have the script close the applications for them.
	If the -AllowDefer option is set to true, an optional "Defer" button will be shown to the user. If they select this option, the script will exit and return a 1618 code (SCCM fast retry code)
	The dialog box will timeout after 1 hour and 55 minutes to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code)
.EXAMPLE
	Show-CloseProgramPrompt "Internet Explorer,Microsoft Office Word"
	Prompt the user to close Word and Excel.
.PARAMETER ProcessNames
	Name of the process to stop (do not include the .exe)
.PARAMETER AllowDefer
	Enable the optional defer button 
.NOTES
	This is an internal script function and should typically not be called directly. It is used by Stop-RunningApplications to prompt the user.
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
	[Parameter(Mandatory = $true)]
	[string]$ProcessDescriptions,
	[switch]$AllowDefer = $false
	)

	[array]$ProcessDescriptions = $ProcessDescriptions.split(",")
	[System.Windows.Forms.Application]::EnableVisualStyles()

	$formClosePrograms = New-Object 'System.Windows.Forms.Form'
	$labelWorkSavedAlready = New-Object 'System.Windows.Forms.Label'
	$listboxPrograms = New-Object 'System.Windows.Forms.ListBox'
	$labelMessage = New-Object 'System.Windows.Forms.Label'
	$buttonCloseForMe = New-Object 'System.Windows.Forms.Button'
	$buttonContinue = New-Object 'System.Windows.Forms.Button'
	$buttonDefer = New-Object 'System.Windows.Forms.Button'
	$buttonAbort = New-Object 'System.Windows.Forms.Button'
	$InitialFormWindowState = New-Object 'System.Windows.Forms.FormWindowState'

	$FormEvent_Load={
		#TODO: Initialize Form Controls here
	}

	$buttonContinue_Click={ 
		#TODO: Place custom script here
	}

	$buttonAbort_Click={	
		#TODO: Place custom script here
	}

	$buttonDefer_Click={
		#TODO: Place custom script here
	}

	$buttonCloseForMe_Click={
		#TODO: Place custom script here
	}

	$groupbox1_Enter={
		#TODO: Place custom script here
	}

	$panel1_Paint=[System.Windows.Forms.PaintEventHandler]{
	#Event Argument: $_ = [System.Windows.Forms.PaintEventArgs]
		#TODO: Place custom script here
	}

	$listboxPrograms_SelectedIndexChanged={
		#TODO: Place custom script here
	}

	$labelWorkSavedAlready_Click={
		#TODO: Place custom script here
	}

	$Form_StateCorrection_Load=
	{
		#Correct the initial state of the form to prevent the .Net maximized form issue
		$formClosePrograms.WindowState = $InitialFormWindowState
	}

	$Form_Cleanup_FormClosed=
	{
		#Remove all event handlers from the controls
		Try
		{
			$labelWorkSavedAlready.remove_Click($labelWorkSavedAlready_Click)
			$listboxPrograms.remove_SelectedIndexChanged($listboxPrograms_SelectedIndexChanged)
			$buttonContinue.remove_Click($buttonContinue_Click)
			$buttonCloseForMe.remove_Click($buttonCloseForMe_Click)
			$buttonDefer.remove_Click($buttonDefer_Click)
			$buttonAbort.remove_Click($buttonCloseForMe_Click)
			$formClosePrograms.remove_Load($FormEvent_Load)
			$formClosePrograms.remove_Load($Form_StateCorrection_Load)
			$formClosePrograms.remove_FormClosed($Form_Cleanup_FormClosed)
		}
		catch [Exception]
		{ }
	}

	# Form
	$formClosePrograms.Controls.Add($labelWorkSavedAlready)
	$formClosePrograms.Controls.Add($listboxPrograms)
	$formClosePrograms.Controls.Add($labelMessage)
	$formClosePrograms.Controls.Add($buttonCloseForMe)
	$formClosePrograms.Controls.Add($buttonContinue)
	# Hide the Defer button on the form if specified
	If ($AllowDefer -eq $true) {
		$formClosePrograms.Controls.Add($buttonDefer)
	}
	$formClosePrograms.Controls.Add($buttonAbort)
	$formClosePrograms.AcceptButton = $buttonContinue
	$formClosePrograms.ClientSize = '366, 296'
	#$formClosePrograms.ControlBox = $false
	$formClosePrograms.ForeColor = 'Black'
	$formClosePrograms.FormBorderStyle = 'FixedDialog'
	$formClosePrograms.MaximizeBox = $False
	$formClosePrograms.MinimizeBox = $False
	$formClosePrograms.Name = "formClosePrograms"
	$formClosePrograms.StartPosition = 'CenterScreen'
	$formClosePrograms.Icon = New-Object System.Drawing.Icon ($AppDeployLogoIcon)
	$formClosePrograms.Tag = ""
	$formClosePrograms.Text = "$installTitle"
	$formClosePrograms.TopMost = $True
	$formClosePrograms.TopLevel = $True
	$formClosePrograms.add_Load($FormEvent_Load)   

	# Label Work saved already
	$labelWorkSavedAlready.Location = '194, 260'
	$labelWorkSavedAlready.Name = "labelWorkSavedAlready"
	$labelWorkSavedAlready.Size = '162, 23'
	$labelWorkSavedAlready.TabIndex = 4
	$labelWorkSavedAlready.Text = " ($configClosePromptConfirm)"
	$labelWorkSavedAlready.add_Click($labelWorkSavedAlready_Click)

	# Listbox Applications
	$listboxPrograms.FormattingEnabled = $True
	$listboxPrograms.Location = '26, 107'
	$listboxPrograms.Name = "listboxPrograms"
	$listboxPrograms.Size = '311, 108'
	$listboxPrograms.TabIndex = 3
	$listboxPrograms.add_SelectedIndexChanged($listboxPrograms_SelectedIndexChanged)
	Foreach ($processDescription in $ProcessDescriptions) {
		$listboxPrograms.Items.Add("$processDescription")
	}

	# Label Message
	$labelMessage.Location = '26, 18'
	$labelMessage.Name = "labelMessage"
	$labelMessage.Size = '311, 86'
	$labelMessage.TabIndex = 3
	$labelMessage.Text = $configClosePromptMessage
	$labelMessage.TextAlign = 'MiddleCenter'

	# Button CloseForMe
	$buttonCloseForMe.Anchor = 'Bottom, Right'
	$buttonCloseForMe.DialogResult = 'Yes'
	$buttonCloseForMe.Font = "Microsoft Sans Serif, 8.25pt"
	$buttonCloseForMe.ForeColor = 'Black'
	$buttonCloseForMe.Location = '175, 225'
	$buttonCloseForMe.Name = "buttonCloseForMe"
	$buttonCloseForMe.Size = '162, 32'
	$buttonCloseForMe.TabIndex = 1
	$buttonCloseForMe.Text = $configClosePromptButtonClose
	$buttonCloseForMe.UseVisualStyleBackColor = $True
	$buttonCloseForMe.add_Click($buttonCloseForMe_Click)

	# Button Continue
	$buttonContinue.Anchor = 'Bottom, Left'
	$buttonContinue.DialogResult = 'OK'
	$buttonContinue.Location = '26, 225'
	$buttonContinue.Name = "buttonContinue"
	$buttonContinue.Size = '125, 32'
	$buttonContinue.TabIndex = 0
	$buttonContinue.Text = $configClosePromptButtonContinue
	$buttonContinue.UseVisualStyleBackColor = $True
	$buttonContinue.add_Click($buttonContinue_Click)

	# Button Defer
	$buttonDefer.Enabled = $true
	$buttonDefer.Anchor = 'Bottom, Left'
	$buttonDefer.DialogResult = 'No'
	$buttonDefer.Location = '26, 260'
	$buttonDefer.Name = "buttonDefer"
	$buttonDefer.Size = '125, 32'
	$buttonDefer.TabIndex = 2
	$buttonDefer.Text = $configClosePromptButtonDefer
	$buttonDefer.UseVisualStyleBackColor = $True
	$buttonDefer.add_Click($buttonDefer_Click)

	$buttonAbort.Anchor = 'Top, Left'
	$buttonAbort.DialogResult = 'Abort'
	$buttonAbort.Location = '0, 0'
	$buttonAbort.Name = "buttonAbort"
	$buttonAbort.Size = '1, 1'
	$buttonAbort.TabIndex = 2
	$buttonAbort.Text = "Abort"
	$buttonAbort.UseVisualStyleBackColor = $True
	$buttonAbort.add_Click($buttonAbort_Click)
	# $buttonAbort.Visible = $false

	# Timer (set for 1 hour 55 mins)
	$timer = New-Object 'System.Windows.Forms.Timer'
	$timer.Interval = 4140000
	$timer.Add_Tick({$buttonAbort.PerformClick()})

	#Save the initial state of the form
	$InitialFormWindowState = $formClosePrograms.WindowState
	#Init the OnLoad event to correct the initial state of the form
	$formClosePrograms.add_Load($Form_StateCorrection_Load)
	#Clean up the control events
	$formClosePrograms.add_FormClosed($Form_Cleanup_FormClosed)

	# Start the timer
	$timer.Start()

	#Show the Form
	Return $formClosePrograms.ShowDialog()

	# Activate the Window
	$powershellProcess = Get-Process | Where { $_.MainWindowTitle -match $installTitle }
	[Microsoft.VisualBasic.Interaction]::AppActivate($powershellProcess.ID)

} #End Function
#endregion

# Function to display a balloon tip notification
Function Show-BalloonTip  {
<# 
.SYNOPSIS
	Displays a balloon tip notification in the system tray
.DESCRIPTION
	Displays a balloon tip notification in the system tray
.EXAMPLE
	Show-BalloonTip -BalloonTipText "Installation Started" -BalloonTipTitle "Application Name"
.EXAMPLE
	Show-BalloonTip -BalloonTipIcon "Info" -BalloonTipText "Installation Started" -BalloonTipTitle "Application Name" -BalloonTipTime "1000"
.PARAMETER BalloonTipText
	Text of the balloon tip
.PARAMETER BalloonTipTitle
	Title of the balloon tip
.PARAMETER BalloonTipIcon
	Icon to be used [Default is Info]
	Accepted values: 'Error', 'Info', 'None', 'Warning'
.PARAMETER BalloonTipTime
	Time in milliseconds to display the balloon tip [Default 500]
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param(	
	[Parameter(Mandatory = $true, Position = 0)]
	[ValidateNotNullOrEmpty()]
	[String]$BalloonTipText,
	[Parameter(Position = 1)]
	[String]$BalloonTipTitle = $installTitle,
	[Parameter(Position = 2)]
	[ValidateSet('Error', 'Info', 'None', 'Warning')]
	[System.Windows.Forms.ToolTipIcon]$BalloonTipIcon = 'Info',
	[Parameter(Position = 3)]
	[int]$BalloonTipTime = 500
	)
	
	If ($deployModeSilent -ne $true) {

		[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
		$NotifyIcon = New-Object Windows.Forms.NotifyIcon -Property @{
			BalloonTipIcon = $BalloonTipIcon
			BalloonTipText = $BalloonTipText
			BalloonTipTitle = $BalloonTipTitle
			Icon = New-Object System.Drawing.Icon ($AppDeployLogoIcon)
			Text = -join $BalloonTipText[0..62]
			Visible = $true
		}

		$NotifyIcon.ShowBalloonTip($BalloonTipTime)

		Switch ($Host.Runspace.ApartmentState) {
			STA {
				# Register a click event with action to take based on event for balloon message clicked
				Register-ObjectEvent $NotifyIcon -EventName BalloonTipClicked -Action {$sender.Visible = $False; Unregister-Event $EventSubscriber.SourceIdentifier; Remove-Job $EventSubscriber.Action; $sender.Dispose()} | Out-Null
				# Register a click event with action to take based on event for balloon message closed
				Register-ObjectEvent $NotifyIcon -EventName BalloonTipClosed  -Action {$sender.Visible = $False; Unregister-Event $EventSubscriber.SourceIdentifier; Remove-Job $EventSubscriber.Action; $sender.Dispose()} | Out-Null
				}
			Default {
				Continue
			}
		}
	}
} 

Function Show-InstallationProgress {
<# 
.SYNOPSIS
	Displays a progress dialog in a separate thread with an updatable custom message. 
.DESCRIPTION
	Create a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated.
	The status message supports line breaks.
	The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started.
.EXAMPLE
	Show-InstallationProgress
	Uses the default status message from the XML configuration file.
.EXAMPLE
	Show-InstallationProgress "Installation in Progress..."
.EXAMPLE
	Show-InstallationProgress "Installation in Progress...`nThe installation may take 20 minutes to complete."
.PARAMETER StatusMessage
	The Status Message to be displayed. The default status message is taken from the XML configuration file.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[string]$StatusMessage = $configProgressMessage
	)
	If ($deployModeSilent -ne $true) {
		If ($envhost.Name -match "PowerGUI") {
			Write-Log "Warning: $($envhost.Name) is not a supported host for WPF multithreading. Progress dialog with message [$statusMessage] will not be displayed."
			Return
		}
		# Check if the progress thread is running before invoking methods on it
		If ($Global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -ne "Running") {
			# Notify user that the software installation has started
			$balloonText = "$deploymentTypeName $configBalloonTextStart"
			Show-BalloonTip -BalloonTipIcon "Info" -BalloonTipText "$balloonText"			   
			# Calculate the position on the screen to place the progress dialog			
			$screenBounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds	
			# Create a synchronized hashtable to share objects between runspaces
			$Global:ProgressSyncHash = [hashtable]::Synchronized(@{})
			# Create a new runspace for the progress bar
			$Global:ProgressRunspace =[runspacefactory]::CreateRunspace()
			$Global:ProgressRunspace.ApartmentState = "STA"
			$Global:ProgressRunspace.ThreadOptions = "ReuseThread"		  
			$Global:ProgressRunspace.Open()
			# Add the sync hash to the runspace
			$Global:ProgressRunspace.SessionStateProxy.SetVariable("progressSyncHash",$Global:ProgressSyncHash)   
			# Add other variables from the parent thread required in the progress runspace
			$Global:ProgressRunspace.SessionStateProxy.SetVariable("installTitle",$installTitle) 
			$Global:ProgressRunspace.SessionStateProxy.SetVariable("screenBounds",$screenBounds)  
			$Global:ProgressRunspace.SessionStateProxy.SetVariable("progressStatusMessage",$statusMessage)   
			$Global:ProgressRunspace.SessionStateProxy.SetVariable("AppDeployLogoIcon",$AppDeployLogoIcon)	  

			# Add the script block to be execution in the progress runspace		  
			$progressCmd = [PowerShell]::Create().AddScript({   

			[xml]$xamlProgress = @"
			<Window
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" 
			x:Name="Window" Title=""
			MaxHeight="160" MinHeight="160" Height="160" 
			MaxWidth="520" MinWidth="500" Width="500"
			WindowStartupLocation = "Manual"
			Top=""
			Left=""
			Topmost="True"   
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
			<Grid Background="White">
				<Grid.RowDefinitions>
					<RowDefinition Height="80"/>
					<RowDefinition Height="80"/>
				</Grid.RowDefinitions>
				<TextBlock x:Name = "ProgressText" Grid.Row="0" Grid.Column="0" Margin="0,0,0,0" Text="" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center" TextAlignment="Center" TextWrapping="Wrap"></TextBlock>
				<Ellipse x:Name="ellipse" Grid.Row="1" Grid.Column="0" Margin="0,0,0,50" StrokeThickness="5" RenderTransformOrigin="0.5,0.5" Height="25" Width="25">
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
"@

				## Set the configurable values based using variables addded to the runspace from the parent thread   
				# Select the screen heigth and width   
				$screenWidth = $screenBounds | Select Width -ExpandProperty Width	
				$screenHeight = $screenBounds | Select Height -ExpandProperty Height	
				# Set the start position of the Window based on the screen size
				$xamlProgress.Window.Left =  [string](($screenWidth / 2) - ($xamlProgress.Window.Width /2))
				$xamlProgress.Window.Top = [string]($screenHeight / 10)
				$xamlProgress.Window.Icon = $AppDeployLogoIcon 
				$xamlProgress.Window.Grid.TextBlock.Text = $ProgressStatusMessage  
				$xamlProgress.Window.Title = $installTitle
				# Parse the XAML
				$progressReader = (New-Object System.Xml.XmlNodeReader $xamlProgress)
				$Global:ProgressSyncHash.Window = [Windows.Markup.XamlReader]::Load( $progressReader )
				$Global:ProgressSyncHash.ProgressText = $Global:ProgressSyncHash.Window.FindName("ProgressText")		   
				# Add an action to the Window.Closing event handler to disable the close button				
				$Global:ProgressSyncHash.Window.Add_Closing({$_.Cancel = $true}) 
				# Allow the window to be dragged by clicking on it anywhere
				$Global:ProgressSyncHash.Window.Add_MouseLeftButtonDown({$Global:ProgressSyncHash.Window.DragMove()})  
				# Add a tooltip  
				$Global:ProgressSyncHash.Window.ToolTip = $installTitle
				$Global:ProgressSyncHash.Window.ShowDialog() | Out-Null
				$Global:ProgressSyncHash.Error = $Error
			})

			$progressCmd.Runspace = $Global:ProgressRunspace
			Write-Log "Spinning up Progress Dialog in a separate thread with message: [$statusMessage]"
			# Invoke the progress runspace
			$progressData = $progressCmd.BeginInvoke()
			# Allow the thread to be spin up safely before invoking actions against it.
			Sleep -Seconds 1
			If ($Global:ProgressSyncHash.Error -ne $null) {
				Write-Log "Show-InstallationProgress Error: $($Global:ProgressSyncHash.Error)"
			}
		}
		ElseIf ($Global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -eq "Running") {
			# Allow time between updating the thread
			Sleep -Seconds 1 			
			# Check if the progress thread is running before invoking methods on it					  
			Write-Log "Displaying Progress Message: [$statusMessage]"
			# Update the progress text
			$Global:ProgressSyncHash.Window.Dispatcher.Invoke("Normal",[action]{$Global:ProgressSyncHash.ProgressText.Text =$statusMessage})		   
		}
	}
}

Function Close-InstallationProgress {
<# 
.SYNOPSIS
	Closes the dialog created by Show-InstallationProgress
.DESCRIPTION
	Closes the dialog created by Show-InstallationProgress
	This function is called by the Exit-Script function to close a running instance of the progress dialog if found.
.EXAMPLE
	Close-InstallationProgress
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	If ($Global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -eq "Running") {
		# Close the progress thread 
		$Global:ProgressSyncHash.Window.Dispatcher.InvokeShutdown()
		$Global:ProgressSyncHash.Clear()		
	}
}

Function Set-PinnedApplication {
<# 
.SYNOPSIS
	Pins or unpins a shortcut to the start menu or task bar.
.DESCRIPTION
	Pins or unpins a shortcut to the start menu or task bar.
	This should typically be run in the user context, as pinned items are stored in the user profile. 
.EXAMPLE
	Set-PinnedApplication -Action "PintoStartMenu" -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"
.EXAMPLE
	Set-PinnedApplication -Action "UnpinfromTaskbar" -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"
.PARAMETER Action
	Action to be performed: "PintoStartMenu","UnpinfromStartMenu","PintoTaskbar","UnpinfromTaskbar"
.PARAMETER FilePath
	Path to the shortcut file to be pinned or unpinned
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	[CmdletBinding()]
	Param(
	[ValidateSet("PintoStartMenu","UnpinfromStartMenu","PintoTaskbar","UnpinfromTaskbar")]
	[Parameter(Mandatory = $true)][string]$Action,
	[Parameter(Mandatory = $true)][string]$FilePath
	)
	Write-Log "Set-Pinned Application function called with Parameters: [$Action] [$FilePath]" 
	
	If(!(Test-Path $FilePath)) {
		Write-Log "Warning: Path [$filePath] does not exist. Action [$action] will not be performed."
		Return
	}

	Function Invoke-Verb {
		Param([string]$FilePath,$verb)
		$verb = $verb.Replace("&","")
		$path = Split-Path $FilePath -ErrorAction SilentlyContinue
		$shell = New-Object -Com "Shell.Application" -ErrorAction SilentlyContinue
		$folder = $shell.Namespace($path)	   
		$item = $folder.Parsename((Split-Path $FilePath -leaf -ErrorAction SilentlyContinue))
		$itemVerb = $item.Verbs() | ? {$_.Name.Replace("&","") -eq $verb} -ErrorAction SilentlyContinue
		If ($itemVerb -eq $null) {
			Write-Log "Error performing action [$verb] on [$filePath]."
		} 
		Else {
			Write-Log "Performing [$verb] on [$filePath]..."
			$itemVerb.DoIt()
		}
	}
	Function Get-PinVerb {
		Param([int]$verbId)
		Try {
			$t = [type]"CosmosKey.Util.MuiHelper"
		} 
		Catch {
			$def = [Text.StringBuilder]""
			[void]$def.AppendLine(‘[DllImport("user32.dll")]‘)
			[void]$def.AppendLine(‘public static extern int LoadString(IntPtr h,uint id, System.Text.StringBuilder sb,int maxBuffer);’)
			[void]$def.AppendLine(‘[DllImport("kernel32.dll")]‘)
			[void]$def.AppendLine(‘public static extern IntPtr LoadLibrary(string s);’)
			Add-Type -MemberDefinition $def.ToString() -name MuiHelper -namespace CosmosKey.Util -ErrorAction SilentlyContinue
		}
		If ($global:CosmosKey_Utils_MuiHelper_Shell32 -eq $null){
			$global:CosmosKey_Utils_MuiHelper_Shell32 = [CosmosKey.Util.MuiHelper]::LoadLibrary("shell32.dll")
		}
		$maxVerbLength=255
		$verbBuilder = New-Object Text.StringBuilder "",$maxVerbLength -ErrorAction SilentlyContinue
		[void][CosmosKey.Util.MuiHelper]::LoadString($CosmosKey_Utils_MuiHelper_Shell32,$verbId,$verbBuilder,$maxVerbLength)
		Return $verbBuilder.ToString()
	}

	$verbs = @{
	"PintoStartMenu"=5381
	"UnpinfromStartMenu"=5382
	"PintoTaskbar"=5386
	"UnpinfromTaskbar"=5387
	}

	If($verbs.$Action -eq $null){
		Throw "Action $action not supported`nSupported actions are:`n`tPintoStartMenu`n`tUnpinfromStartMenu`n`tPintoTaskbar`n`tUnpinfromTaskbar"
	}
	Invoke-Verb -FilePath $FilePath -Verb $(Get-PinVerb -VerbId $verbs.$action)
}

Function Get-IniContent {
<# 
.SYNOPSIS
	Parses an ini file and returns the contents as objects with ini section, name and value properties
.DESCRIPTION
	Parses an ini file and returns the contents as objects with ini section, name and value properties
.EXAMPLE
	Get-IniContent "$envProgramFilesX86\IBM\Lotus\Notes\notes.ini"
.EXAMPLE
	Get-IniContent "$envProgramFilesX86\IBM\Lotus\Notes\notes.ini" | Where { $_.Name -eq "KeyFileName" } | Select Value -ExpandProperty Value 
.PARAMETER FilePath
	Path to the ini file
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[Parameter(Mandatory = $true)]
		$FilePath
	) 
	If (Test-Path $FilePath) {
		Switch -Regex -File $FilePath {
			"^\[(.+)\]" {
				$section = $matches[1]
			}
			"(.+?)\s*=(.*)" { 
				$name,$value = $matches[1..2]
				New-Object PSObject -Property @{
					Section = $section
					Name = $name
					Value = $value
				}			
			}
		}
	}
	Else {
		Write-Log "File [$filePath] could not be found."
	}
}

Function Set-IniContent {
<# 
.SYNOPSIS
	Adds or sets the value of a property in an ini file
.DESCRIPTION
	Adds or sets the value of a property in an ini file
.EXAMPLE
	Set-IniContent "$envProgramFilesX86\IBM\Lotus\Notes\notes.ini" -Key "AutoLogoffMinutes" -Value "10"
.PARAMETER FilePath
	Path to the inin file
.PARAMETER Key
	The ini property name
.PARAMETER Value
	The ini property value
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
	[Parameter(Mandatory = $true)]
	[string]$FilePath,
	[Parameter(Mandatory = $true)]
	[string]$Key,
	[string]$Value  
	)
	If (Test-Path $FilePath) {
		$iniContent = Get-Content $FilePath
		If ($iniContent -match "($key)=(.*)") {
			Write-Log "Setting key/value [$key=$value] in INI file [$filePath]..."
			$iniContent | ForEach-Object {$_ -replace "($key)=(.*)", "$key=$value" } | Set-Content -Path $FilePath -Force -ErrorAction SilentlyContinue			   
		}
		Else {
			Write-Log "Adding key/value [$key=$value] to INI file [$filePath]..."
			Add-Content -Path $filePath -Value "$key=$value" -Force -ErrorAction SilentlyContinue  
		}
	}
	Else {
		Write-Log "File [$filePath] could not be found."
	}
}

Function Register-DLL {
<# 
.SYNOPSIS
	Registers a DLL file
.DESCRIPTION
	Registers a DLL file using regsvr32.exe
.EXAMPLE
	Register-DLL "$envProgramFiles\Documentum\Shared\DcTLSFileToDMSComp.dll"	
.PARAMETER FilePath
	Path to the DLL file
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
	[Parameter(Mandatory = $true)]
	[string]$FilePath
	)

	Write-Log "Registering DLL file [$filePath]..."   

	If (Test-Path $FilePath ) {
		Execute-Process "regsvr32.exe" -Arguments "/s '$FilePath'" -WindowStyle Hidden -PassThru
	}
	Else {
		Write-Log "DLL file [$filePath] not found."
	}

}

Function Unregister-DLL {
<# 
.SYNOPSIS
	Unregisters a DLL file
.DESCRIPTION
	Unregisters a DLL file using regsvr32.exe
.EXAMPLE
	Unregister-DLL "$envProgramFiles\Documentum\Shared\DcTLSFileToDMSComp.dll"	
.PARAMETER FilePath
	Path to the DLL file
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[Parameter(Mandatory = $true)]
		[string]$FilePath
	)

	Write-Log "Unregistering DLL file [$filePath]..." 

	If (Test-Path $FilePath ) {
		Execute-Process "regsvr32.exe" -Arguments "/s /u '$FilePath'" -WindowStyle Hidden -PassThru
	}
	Else {
		Write-Log "DLL file [$filePath] not found."
	}

}

Function Test-MSUpdates {
<# 
.SYNOPSIS
	Test whether an Microsoft Windows update is installed
.DESCRIPTION
	Test whether an Microsoft Windows update is installed
.EXAMPLE
	Test-MSUpdate "KB2549864"
.PARAMETER KBNumber
	KBNumber
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[ValidateNotNullorEmpty()]
		[Parameter(Position=0,Mandatory=$True,HelpMessage="Enter a KB Number for the MS update")]
		[string]$KBNumber
	)
	
	Write-Log "Testing for Microsoft Update $kbNumber..."

	# Default is not found
	$kbFound = $false

	# Check using Update method (to catch Office updates)
	$Session = New-Object -ComObject Microsoft.Update.Session
	$Collection = New-Object -ComObject Microsoft.Update.UpdateColl
	$Installer = $Session.CreateUpdateInstaller()
	$Searcher = $Session.CreateUpdateSearcher()
	$Searcher.QueryHistory(0, $Searcher.GetTotalHistoryCount()) | Where-Object { $_.Title -match $kbNumber } | ForEach-Object { $kbFound = $true }
	
	# Check using standard method
	If ($kbFound -eq $false) { Get-Hotfix -id $kbNumber -ErrorAction SilentlyContinue | ForEach-Object { $kbFound = $true } }

	# Return Result
	If ($kbFound -eq $false) { 
		Write-Log "Update $kbNumber is not installed"
		Return $false
	} 
	Else { 
		Write-Log "Update $kbNumber is installed"
		Return $true 
	}

}

Function Install-MSUpdates ($Directory) {
<# 
.SYNOPSIS
	Installs all Microsft Updates in a given directory
.DESCRIPTION
	Installs all Microsft Updates in a given directory of type ".exe", ".msu" or ".msp"
.EXAMPLE
	Install-MSUpdate "$dirFiles\MSUpdates"
.PARAMETER Directory
	Directory containing the updates
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>

	Write-Log "Installing Microsoft Updates from directory [$Directory]"

	# KB Number pattern match
	$kbPattern = 'KB\d{6,8}'

	# Get all hotfixes and install if required
	$files = Get-ChildItem $Directory -Recurse -Include @("*.exe", "*.msu", "*.msp")
	ForEach ($file in $files) {
		# Get the KB number of the file
		$kbNumber = [regex]::match($file, $kbPattern).ToString()
		# Check to see whether the KB is already installed
		If ((Test-MSUpdates -kbNumber $kbNumber) -eq $false) {
			Write-Log "$kbNumber was not detected and will be installed."
			Switch ($file.Extension) {
				# Installation type for executables (ie, Microsoft Office Updates)
				".exe" { Execute-Process -FilePath $file -Arguments "/quiet /norestart" -WindowStyle Hidden }
				# Installation type for Windows updates using Windows Update Standalone Installer
				".msu" { Execute-Process -FilePath "wusa.exe" -Arguments "`"$file`" /quiet /norestart" -WindowStyle Hidden }
				# Installation type for Windows Installer Patch
				".msp" { Execute-MSI -Action "Patch" -Path $file }
			}
		}
		Else {
			Write-Log "$kbNumber was already installed. Skipping..."
		}
	}
}

# Function to test whether the laptop is on power or battery
Function Test-Battery { 
<# 
.SYNOPSIS
	Tests whether the local machine is running on battery
.DESCRIPTION
	Tests whether the local machine is running on battery and returns true/false 
.EXAMPLE
	Test-Battery
.EXAMPLE
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Write-Log "Testing power connection status..."

	$batteryStatus = Get-WmiObject -Class BatteryStatus -Namespace root\wmi -ComputerName . -ErrorAction SilentlyContinue
	If ($batteryStatus) {
		$power = $batteryStatus.PowerOnLine
		If ($power) {
			Write-Log "Power connection found."
			$onPower = $true
			Return $true
		}
	}

	Write-Log "Power connection not found"
	Return $false
} 

Function Test-NetworkConnection {
<# 
.SYNOPSIS
	Tests for an active network connection
.DESCRIPTION
	Tests for an active network connection by querying the Win32_NetworkAdapter WMI class.
.EXAMPLE
	Test-NetworkConnection
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>

	Write-Log "Testing Network connection status..."

	$networkConnected = Get-WmiObject Win32_NetworkAdapter | Where { $_.NetConnectionStatus -eq "2" -and $_.NetConnectionID -match "Local" -and $_.NetConnectionID -notmatch "Wireless" -and $_.Name -notmatch "Virtual" } -ErrorAction SilentlyContinue
	If ($networkConnected) {
		Write-Log "Network connection found."
		$onNetwork = $true
		Return $true
	}
	
	Write-Log "Network connection not found."
	Return $false
}

Function Test-PowerPoint {
<# 
.SYNOPSIS
	Tests whether Power point is running in presentation mode
.DESCRIPTION
	Tests whether Power point is running in presentation mode
.EXAMPLE
	Test-PowerPoint
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>

	Write-Log "Testing Powerpoint status..."

	Try {
		$powerPoint = [System.Runtime.InteropServices.Marshal]::GetActiveObject("Powerpoint.Application")
		$slideshow = $powerPoint.SlideShowWindows
		# detects if a PPT slideshow is in progress
		If ($slideshow.Count -gt 0) {
			Write-Log "Presentation Mode is enabled."
			Return $true
		} 
		Else {
			Write-Log "Presentation Mode is not enabled."
			Return $false
		}
	}
	Catch [Exception] {
		# If no powerpoint instances are found
		Write-Log "Presentation Mode is not enabled."
		Return $false
	}
}

Function Update-SCCMInventory {
<# 
.SYNOPSIS
	Performs an SCCM Hardware Inventory Collection Cycle
.DESCRIPTION
	Performs an SCCM Hardware Inventory Collection Cycle
.EXAMPLE
	Update-SCCMInventory
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	# Connect to the SCCM COM Object
	Write-Log "Connecting to SCCM COM Object"
	$cpAppletMgr = New-Object -ComObject CPApplet.CPAppletMgr

	# Request & Evaluate the Machine Policy
	Write-Log "Performing SCCM Hardware Inventory..."
	$machinePolicy = $cpAppletMgr.GetClientActions() | Where-Object { $_.Name -eq "Hardware Inventory Collection Cycle" }
	If ($machinePolicy -ne $null) {
		$machinePolicy.PerformAction()
	}
}

Function Update-SCCMDeployments {
<# 
.SYNOPSIS
	Performs an SCCM "Request & Evaluate Machine Policy" and "Request & Evaluate User Policy" action
.DESCRIPTION
	Performs an SCCM "Request & Evaluate Machine Policy" and "Request & Evaluate User Policy" action
.EXAMPLE
	Update-SCCMDeployments
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	# Connect to the SCCM COM Object
	Write-Log "Connecting to SCCM COM Object"
	$cpAppletMgr = New-Object -ComObject CPApplet.CPAppletMgr

	# Request & Evaluate the Machine Policy
	Write-Log "Requesting & Evaluating the SCCM Machine Policy..."
	$machinePolicy = $cpAppletMgr.GetClientActions() | Where-Object { $_.Name -eq "Request & Evaluate Machine Policy" }
	If ($machinePolicy -ne $null) {
		$machinePolicy.PerformAction()
		}

	# Request & Evaluate the User Policy
	Write-Log "Requesting & Evaluating the SCCM User Policy..."
	$userPolicy = $cpAppletMgr.GetClientActions() | Where-Object { $_.Name -eq "Request & Evaluate User Policy" }
	If ($userPolicy -ne $null) {
		$userPolicy.PerformAction()
		}
}

Function Install-SCCMSoftwareUpdates {
<# 
.SYNOPSIS
	Scans for outstanding SCCM updates to be installed and installed the pending updates
.DESCRIPTION
	Scans for outstanding SCCM updates to be installed and installed the pending updates
	This function can take several minutes to run
.EXAMPLE
	Install-SCCMSoftwareUpdates
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	# Scan for updates
	Write-Log "Scanning for SCCM Software Updates..."
	([wmiclass]‘ROOT\ccm:SMS_Client’).TriggerSchedule(‘{00000000-0000-0000-0000-000000000113}’) | Out-Null
	Write-Log "Sleeping 180 seconds..."
	Sleep -Seconds 180

	# Install pending updates
	Write-Log "Installing pending software updates..."
	([wmiclass]‘ROOT\ccm\ClientSDK:CCM_SoftwareUpdatesManager’).InstallUpdates([System.Management.ManagementObject[]] (get-wmiobject -query “SELECT * FROM CCM_SoftwareUpdate” -namespace “ROOT\ccm\ClientSDK”)) | Out-Null
}

Function Update-GroupPolicy {
<# 
.SYNOPSIS
	Performs a gpupdate command to refresh Group Policies on the local machine
.DESCRIPTION
	Performs a gpupdate command to refresh Group Policies on the local machine
.EXAMPLE
	Update-GroupPolicy
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Write-Log "Updating Group Policies..."
	$gpUpdatePath = Join-Path $env:SystemRoot "System32\gpupdate"
	Execute-Process -FilePath $gpUpdatePath -WindowStyle Hidden
}

#*=============================================
#* END FUNCTION LISTINGS
#*=============================================

#*=============================================
#* SCRIPT BODY
#*=============================================

# If the cleanupBlockedApps Parameter is specified, only call that function.
If ($cleanupBlockedApps -eq $true) {
	$deployModeSilent = $true
	$installName = $blockedAppInstallName
	Write-Log "$appDeployMainScriptFriendlyName called with switch CleanupBlockedApps"
	Unblock-AppExecution
	Exit-Script -exitCode 0
}

# If the showBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog -eq $true) {
	Try {
		$deployModeSilent = $true
		$installName = $blockedAppInstallName
		Write-Log "$appDeployMainScriptFriendlyName called with switch ShowBlockedAppDialog"
		Show-DialogBox -Title $blockedAppInstallName -Text $configBlockExecutionMessage -Icon "Exclamation" -Timeout 600
		Exit-Script -exitCode 0
	} 
	Catch {
		$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)" 
		Write-Log "$exceptionMessage" 
		Show-DialogBox -Text $exceptionMessage -Icon "Stop"
		Exit-Script -ExitCode 1
	}
}

# Initialization Logging
$installPhase = "Initialization"

# Check how the script was invoked
If ($(((Get-Variable MyInvocation).Value).ScriptName) -ne "") {  
	Write-Log "Script [$($MyInvocation.MyCommand.Definition)] dot-source invoked by [$(((Get-Variable MyInvocation).Value).ScriptName)]" 
	# If the script was invoked by the Help console, exit the script now because we don't need initialization logging.
	If ($(((Get-Variable MyInvocation).Value).ScriptName) -match "Help") {
		Return
	}
}
Else {
	Write-Log "Script [$($MyInvocation.MyCommand.Definition)] invoked directly"
}

# Dot Source script extensions
If ($appDeployToolkitDotSources -ne "") { 
	Get-ChildItem "$scriptRoot\*.*" -Include $appDeployToolkitDotSources -ErrorAction SilentlyContinue | Sort Name -Descending | Select FullName -ExpandProperty FullName -ErrorAction Stop | % { .$_ }
}

Write-Log "$installName setup started."
If ($appScriptVersion -ne $null ) { Write-Log "$installName script version is [$appScriptVersion]" }
If ($deployAppScriptFriendlyName -ne $null ) { Write-Log "$deployAppScriptFriendlyName script version is [$deployAppScriptVersion]" }
If ($appDeployMainScriptFriendlyName -ne $null ) { Write-Log "$appDeployMainScriptFriendlyName script version is [$appDeployMainScriptVersion]" }
If ($appDeployExtScriptFriendlyName -ne $null ) { Write-Log "$appDeployExtScriptFriendlyName version is [$appDeployExtScriptVersion]" }
Write-Log "PowerShell version is [$($PSVersionTable.PSVersion) $psArchitecture]"
Write-Log "PowerShell host is [$($envHost.name) version $($envHost.version)]"
Write-Log "OS version is [$($envOS.Caption) $($envOS.OSArchitecture) $($envOS.Version)]"
Write-Log "Hardware platform is [$(Get-HardwarePlatform)]"
Write-Log "Computer name is [$envComputerName]"
If ($envUserName -ne $null ) { Write-Log "Current user is [$envUserDomain\$envUserName]" }
Write-Log "Current UI language is [$currentLanguage]"

# Check deployment type (install/uninstall)
Switch ($deploymentType) {
	"Install" { $deploymentTypeName = $configDeploymentTypeInstall }
	"Uninstall" { $deploymentTypeName = $configDeploymentTypeUnInstall }
}
If ($deploymentTypeName -ne $null ) { Write-Log "Deployment type is [$deploymentTypeName]" }

# Check if we are running a task sequence, and enable NonInteractive mode
If (Get-Process -Name "TSManager" -ErrorAction SilentlyContinue) {
	$deployMode = "NonInteractive"  
	Write-Log "Running task sequence detected. Setting Mode to [$deployMode]."
}
# Check if we are running in session zero, and enable NonInteractive mode
ElseIf (!(Get-Process -Name "explorer" -ErrorAction SilentlyContinue)) { 
	$deployMode = "NonInteractive"  
	Write-Log "Session 0 detected. Setting Mode to [$deployMode]."  
 }

If ($deployMode -ne $null) {
	Write-Log "Installation is running in [$deployMode] mode" 
}

# Set Deploy Mode switches
Switch ($deployMode) {
	"Silent" { $deployModeSilent = $true }
	"NonInteractive" { $deployModeNonInteractive = $true; $deployModeSilent = $true }
	Default {$deployModeNonInteractive = $false; $deployModeSilent = $false}
}

# Check current permissions and exit if not running with Administrator rights
If (!([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
	If ($ShowBlockedAppDialog -ne $true) {
		Throw "$appDeployMainScriptFriendlyName requires Administrator rights to function. Please re-run the deployment script as an Administrator."
	}
}

#*=============================================
#* END SCRIPT BODY
#*=============================================