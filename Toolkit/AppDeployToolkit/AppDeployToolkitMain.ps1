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
	Display a dialog box showing that the application execution is blocked.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.PARAMETER ReferringApplication
	Name of the referring application that invoked the script externally.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.NOTES
	The other parameters specified for this script that do not are not documented in this help section are for use only by functions in this script that call themselves by running this script again asynchronously
.LINK
	Http://psappdeploytoolkit.codeplex.com 
"#>
Param (
	## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or Image File Execution Options
	[switch] $ShowInstallationPrompt = $false,
	[switch] $ShowInstallationRestartPrompt = $false,
	[switch] $CleanupBlockedApps = $false,
	[switch] $ShowBlockedAppDialog = $false,
	[switch] $DisableLogging = $false,
	[string] $ReferringApplication = $Null,
	[string] $Message = $null,
	[string] $MessageAlignment = $null,
	[string] $ButtonRightText = $null,
	[string] $ButtonLeftText = $null,
	[string] $ButtonMiddleText = $null,
	[string] $Icon = $null,
	[string] $Timeout = $null,
	[switch] $ExitOnTimeout = $null,
	[boolean] $MinimizeWindows = $false,
	[switch] $PersistPrompt = $false,
	[int] $CountdownSeconds,
	[int] $CountdownNoHideSeconds,
	[switch] $NoCountdown = $false
)

#*=============================================
#* VARIABLE DECLARATION
#*=============================================

# Variables: Toolkit
$appDeployToolkitName = "PSAppDeployToolkit"

# Variables: Script
$appDeployMainScriptFriendlyName = "App Deploy Toolkit Main"
$appDeployMainScriptVersion = [version]"3.1.5"
$appDeployMainScriptMinimumConfigVersion = [version]"3.1.5"
$appDeployMainScriptDate = "08/01/2014"
$appDeployMainScriptParameters = $psBoundParameters

# Variables: Environment
$currentDate = (Get-Date -UFormat "%d-%m-%Y")
$currentTime = (Get-Date -UFormat "%T")
$culture = Get-Culture
$envHost = $host
$envAllUsersProfile = $env:ALLUSERSPROFILE
$envAppData = $env:APPDATA
$envArchitecture = $env:PROCESSOR_ARCHITECTURE
$envCommonProgramFiles = $env:CommonProgramFiles
$envCommonProgramFilesX86 = "${env:CommonProgramFiles(x86)}"
$envComputerName = $env:COMPUTERNAME
$envHomeDrive = $env:HOMEDRIVE
$envHomePath = $env:HOMEPATH
$envHomeShare = $env:HOMESHARE
$envLocalAppData = $env:LOCALAPPDATA
$envLogonServer = $env:LOGONSERVER
$envOS = Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue
$envProgramFiles = $env:PROGRAMFILES
$envProgramFilesX86 = "${env:ProgramFiles(x86)}"
$envProgramData = $env:PROGRAMDATA
$envPublic = $env:PUBLIC
$envSystemDrive = $env:SYSTEMDRIVE
$envSystemRoot = $env:SYSTEMROOT
$envTemp = $env:TEMP
$envUserDNSDomain = $env:USERDNSDOMAIN
$envUserDomain = $env:USERDOMAIN
$envUserName = $env:USERNAME
$envUserProfile = $env:USERPROFILE
$envWinDir = $env:WINDIR
# Handle X86 environment variables so they are never empty
If ($envCommonProgramFilesX86 -eq $null -or $envCommonProgramFilesX86 -eq "") { $envCommonProgramFilesX86 = $env:CommonProgramFiles }
If ($envProgramFilesX86 -eq $null -or $envProgramFilesX86 -eq "") { $envProgramFilesX86 = $env:PROGRAMFILES }

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

# Variables: App Deploy Dependency Files
$appDeployLogoIcon = Join-Path $scriptRoot "AppDeployToolkitLogo.ico"
$appDeployLogoBanner = Join-Path $scriptRoot "AppDeployToolkitBanner.png"
$appDeployConfigFile = Join-Path $scriptRoot "AppDeployToolkitConfig.xml"

# Variables: App Deploy Optional Extensions File
$appDeployToolkitDotSourceExtensions = "AppDeployToolkitExtensions.ps1"

# Check that dependency files are present
If (!(Test-Path $AppDeployLogoIcon)) {
	Throw "Error: AppDeploy logo icon file required."
}
If (!(Test-Path $AppDeployLogoBanner)) {
	Throw "Error: AppDeploy logo banner file required."
}
If (!(Test-Path $AppDeployConfigFile)) {
	Throw "Error: AppDeploy xml configuration file not found."
}

# Import variables from XML configuration file
[xml]$xmlConfigFile = Get-Content $AppDeployConfigFile
$xmlConfig = $xmlConfigFile.AppDeployToolkit_Config

# Get Config File Details
$configConfigDetails = $xmlConfig.Config_File
[string]$configConfigVersion = [version]$configConfigDetails.Config_Version
[string]$configConfigDate = $configConfigDetails.Config_Date

# Get Config File Details
$xmlToolkitOptions = $xmlConfig.Toolkit_Options
[bool]$configToolkitRequireAdmin = [boolean]::Parse($xmlToolkitOptions.Toolkit_RequireAdmin)
[bool]$configToolkitAllowSystemInteraction = [boolean]::Parse($xmlToolkitOptions.Toolkit_AllowSystemInteraction)
[bool]$configToolkitCompressLogs = [boolean]::Parse($xmlToolkitOptions.Toolkit_CompressLogs)
[string]$configToolkitLogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_LogPath)
[string]$configToolkitTempPath = $ExecutionContext.InvokeCommand.ExpandString($xmlToolkitOptions.Toolkit_TempPath)
[string]$configToolkitRegPath = $xmlToolkitOptions.Toolkit_RegPath

# Get MSI Options
$xmlConfigMSIOptions = $xmlConfig.MSI_Options
[string]$configMSILoggingOptions = $xmlConfigMSIOptions.MSI_LoggingOptions
[string]$configMSIInstallParams = $xmlConfigMSIOptions.MSI_InstallParams
[string]$configMSISilentParams = $xmlConfigMSIOptions.MSI_SilentParams
[string]$configMSIUninstallParams = $xmlConfigMSIOptions.MSI_UninstallParams
[string]$configMSILogDir = $ExecutionContext.InvokeCommand.ExpandString($xmlConfigMSIOptions.MSI_LogPath)
# Get UI Options
$xmlConfigUIOptions = $xmlConfig.UI_Options
[bool]$configShowBalloonNotifications = [boolean]::Parse($xmlConfigUIOptions.ShowBalloonNotifications)
[int]$configInstallationUITimeout = $xmlConfigUIOptions.InstallationUI_Timeout
[int]$configInstallationUIExitCode = $xmlConfigUIOptions.InstallationUI_ExitCode
[int]$configInstallationDeferExitCode = $xmlConfigUIOptions.InstallationDefer_ExitCode
[int]$configInstallationPersistInterval = $xmlConfigUIOptions.InstallationPrompt_PersistInterval
[int]$configInstallationRestartPersistInterval = $xmlConfigUIOptions.InstallationRestartPrompt_PersistInterval
# Get Message UI Language Options (default for English if no localization found)
$xmlUIMessageLanguage = "UI_Messages_" + $currentLanguage
If (($xmlConfig.$xmlUIMessageLanguage) -eq $null) {
	$xmlUIMessageLanguage = "UI_Messages_EN"
}
$xmlUIMessages = $xmlConfig.$xmlUIMessageLanguage
[string]$configDiskSpaceMessage = $xmlUIMessages.DiskSpace_Message
[string]$configBalloonTextStart = $xmlUIMessages.BalloonText_Start
[string]$configBalloonTextComplete = $xmlUIMessages.BalloonText_Complete
[string]$configBalloonTextRestartRequired = $xmlUIMessages.BalloonText_RestartRequired
[string]$configBalloonTextFastRetry = $xmlUIMessages.BalloonText_FastRetry
[string]$configBalloonTextError = $xmlUIMessages.BalloonText_Error
[string]$configProgressMessageInstall = $xmlUIMessages.Progress_MessageInstall
[string]$configProgressMessageUninstall = $xmlUIMessages.Progress_MessageUninstall
[string]$configClosePromptConfirm = $xmlUIMessages.ClosePrompt_Confirm
[string]$configClosePromptMessage = $xmlUIMessages.ClosePrompt_Message
[string]$configClosePromptButtonClose = $xmlUIMessages.ClosePrompt_ButtonClose
[string]$configClosePromptButtonDefer = $xmlUIMessages.ClosePrompt_ButtonDefer
[string]$configClosePromptButtonContinue = $xmlUIMessages.ClosePrompt_ButtonContinue
[string]$configClosePromptCountdownMessage = $xmlUIMessages.ClosePrompt_CountdownMessage
[string]$configDeferPromptWelcomeMessage = $xmlUIMessages.DeferPrompt_WelcomeMessage
[string]$configDeferPromptExpiryMessage = $xmlUIMessages.DeferPrompt_ExpiryMessage
[string]$configDeferPromptWarningMessage = $xmlUIMessages.DeferPrompt_WarningMessage
[string]$configDeferPromptRemainingDeferrals = $xmlUIMessages.DeferPrompt_RemainingDeferrals
[string]$configDeferPromptRemainingDays = $xmlUIMessages.DeferPrompt_RemainingDays
[string]$configDeferPromptDeadline = $xmlUIMessages.DeferPrompt_Deadline
[string]$configDeferPromptNoDeadline = $xmlUIMessages.DeferPrompt_NoDeadline
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

# Variables: Directories
$dirSystemRoot = $env:SystemRoot
$dirAppDeployFiles = Join-Path $scriptParentPath "AppDeployToolkitFiles" # The AppDeployFiles directory should be relative to the parent invoking script
$dirFiles = Join-Path $scriptParentPath "Files" # The Files directory should be relative to the parent invoking script
$dirSupportFiles = Join-Path $scriptParentPath "SupportFiles"
$dirAppDeployTemp = Join-Path $configToolkitTempPath ($appDeployToolkitName)
$dirBlockedApps = Join-Path $dirAppDeployTemp "BlockedApps"

# Variables: Executables
$exeWusa = "wusa.exe"
$exeMsiexec = "msiexec.exe"
$exeSchTasks = "$envWinDir\System32\schtasks.exe"

$is64Bit = (Get-WmiObject -Class Win32_OperatingSystem -ea 0).OSArchitecture -eq '64-bit'
$is64BitProcess = [System.IntPtr]::Size -eq 8
If ($is64BitProcess -eq $true) { $psArchitecture = "x64" } Else { $psArchitecture = "x86" }
$isServerOS = (Get-WmiObject -Class Win32_operatingsystem -ErrorAction SilentlyContinue | Select Name -ExpandProperty Name) -match "Server"

# Reset Switches to false
$msiRebootDetected = $false
$BlockExecution = $false
$installationStarted = $false
$sessionZero = $false
$runningTaskSequence = $false
$script:welcomeTimer = $null
# Reset the deferral history
$deferHistory = $deferTimes = $deferDays = $null

# COM Objects: Initialize
$shell = New-Object -ComObject WScript.Shell -ErrorAction SilentlyContinue
$shellApp = New-Object -ComObject Shell.Application -ErrorAction SilentlyContinue

# Set up sample variables if Dot Sourcing the script or app details have not been specified
If ((!$appVendor) -and (!$appName) -and (!$appVersion)) {
	$appVendor = "PS"
	$appName = $appDeployMainScriptFriendlyName
	$appVersion = $appDeployMainScriptVersion
	$appLang = $currentLanguage
	$appRevision = "01"
	$appArch = ""
}

# Build the Application Title and Name
$installTitle = "$appVendor $appName $appVersion"

# Sanitize the application details, as they can cause issues in the script
$invalidFileNameChars = [IO.Path]::GetInvalidFileNamechars()
$appVendor = $appVendor -replace "[$invalidFileNameChars]","" -replace " ",""
$appName = $appName -replace "[$invalidFileNameChars]","" -replace " ",""
$appVersion = $appVersion -replace "[$invalidFileNameChars]","" -replace " ",""
$appArch = $appArch -replace "[$invalidFileNameChars]","" -replace " ",""
$appLang = $appLang -replace "[$invalidFileNameChars]","" -replace " ",""
$appRevision = $appRevision -replace "[$invalidFileNameChars]","" -replace " ",""

# Build the Installation Name
If ($appArch -ne "") {
	$installName = "$appVendor" + "_" + "$appName" + "_" + "$appVersion" + "_" + "$appArch" + "_" + "$appLang" + "_" + "$appRevision"
}
Else {
	$installName = "$appVendor" + "_" + "$appName" + "_" + "$appVersion" + "_" + "$appLang" + "_" + "$appRevision"
}

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
$regKeyDeferHistory = "$configToolkitRegPath\$appDeployToolkitName\DeferHistory\$installName"

# Variables: Log Files
$logTempFolder = Join-Path $envTemp $installName
If ($deploymentType -eq $Null) {$deploymentType = "Install" }
If ($configToolkitCompressLogs) {
	$logFile = Join-Path $logTempFolder ("$installName" + "_$appDeployToolkitName" + "_$deploymentType.log")
	$zipFileDate = (Get-Date).ToString("yyyy-MM-dd-hh-mm-ss")
	$zipFileName = Join-Path $configToolkitLogDir ("$installName" + "_$deploymentType" + "_$zipFileDate.zip")
	If (Test-Path -Path $logTempFolder -ErrorAction SilentlyContinue ) { Remove-Item $logTempFolder -Recurse -Force -ErrorAction SilentlyContinue | Out-Null }
	If (!(Test-Path -Path $logTempFolder -ErrorAction SilentlyContinue )) { New-Item $logTempFolder -Type Directory -ErrorAction SilentlyContinue | Out-Null }
}
Else {
	$logFile = Join-Path $configToolkitLogDir ("$installName" + "_$appDeployToolkitName" + "_$deploymentType.log")
}

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
.PARAMETER PassThru
	Passes the text back to the PowerShell pipeline
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param(
		[Parameter(Mandatory = $true,ValueFromPipeline=$True,ValueFromPipelinebyPropertyName=$True)]
		[array] $Text,
		[switch] $PassThru = $false
	)
	Process {
		$Text = $Text -join (" ")
		$currentDate = (Get-Date -UFormat "%d-%m-%Y")
		$currentTime = (Get-Date -UFormat "%T")
		$logEntry = "[$currentDate $currentTime] [$installPhase] $Text"
		Write-Host $logEntry
		If ($DisableLogging -eq $false) {
			# Create the Log directory and file if it doesn't already exist
			If (!(Test-Path -Path $configToolkitLogDir -ErrorAction SilentlyContinue )) { New-Item $configToolkitLogDir -Type Directory -ErrorAction SilentlyContinue | Out-Null }
			If (!(Test-Path -Path $logFile -ErrorAction SilentlyContinue )) { New-Item $logFile -Type File -ErrorAction SilentlyContinue | Out-Null }
			Try {
				"$logEntry" | Out-File $logFile -Append -ErrorAction SilentlyContinue
			}
			Catch {
				$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)"
				Write-Host "$exceptionMessage"
			}
		}
		If ($PassThru -eq $true) {
			Return $Text
		}
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
		[string] $ExitCode = 0
	)

	# Stop the Close Program Dialog if running
	If ($formCloseApps -ne $null) {
		$formCloseApps.Close
	}

	# Close the Installation Progress Dialog if running
	Close-InstallationProgress

	# If block execution switch is true, call the function to unblock execution
	If ($BlockExecution -eq $true) {
		Unblock-AppExecution
	}

	# If Terminal Server mode was set, turn it off
	If ($terminalServerMode) {
		Disable-TerminalServerInstallMode
	}

	# Determine action based on exit code
	Switch ($exitCode) {
		$configInstallationUIExitCode { $installSuccess = $false }
		$configInstallationDeferExitCode { $installSuccess = $false }
		3010 { $installSuccess = $true }
		1641 { $installSuccess = $true }
		0 { $installSuccess = $true }
		Default { $installSuccess = $false }
	}

	If ($installSuccess -eq $true) {
		If (Test-Path $regKeyDeferHistory -ErrorAction SilentlyContinue) {
			Write-Log "Removing deferral history..."
			Remove-RegistryKey -Key $regKeyDeferHistory
		}
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
		Write-Log "$installName $deploymentTypeName completed with exit code [$exitcode]."
		If ($exitCode -eq $configInstallationUIExitCode -or $exitCode -eq $configInstallationDeferExitCode) {
			$balloonText = "$deploymentTypeName $configBalloonTextFastRetry"
			Show-BalloonTip -BalloonTipIcon "Warning" -BalloonTipText "$balloonText"
		}
		Else {
			$balloonText = "$deploymentTypeName $configBalloonTextError"
			Show-BalloonTip -BalloonTipIcon "Error" -BalloonTipText "$balloonText"
		}
	}

	Write-Log "----------------------------------------------------------------------------------------------------------"

	# Compress the log files and remove the temporary folder
	If ($configToolkitCompressLogs) {
		Try {
			Set-Content $zipFileName ("PK" + [char]5 + [char]6 + ("$([char]0)" * 18))
			$zipFile = $shellApp.NameSpace($zipFileName)
			ForEach ($file in (Get-ChildItem $logTempFolder)) {
				Write-Host "Compressing log file [$($file.Name)] to [$($zipFileName)]..."
				$zipFile.CopyHere($file.FullName)
				Start-Sleep -Milliseconds 500
			}
			If (Test-Path -Path $logTempFolder -ErrorAction SilentlyContinue ) { Remove-Item $logTempFolder -Recurse -Force -ErrorAction SilentlyContinue | Out-Null }
		}
		Catch {
			Write-Log "An error occurred while attempting to compress the log files: $($_.Exception.Message)"
		}
	}

	# Exit the script returning the exit code to SCCM
	Exit $exitCode
}

Function Show-InstallationPrompt {
<#
.SYNOPSIS
	Displays a custom installation prompt with the toolkit branding and optional buttons.
.DESCRIPTION
	Any combination of Left, Middle or Right buttons can be displayed. The return value of the button clicked by the user is the button text specified.
.EXAMPLE
	Show-InstallationPrompt -Message "Do you want to proceed with the installation?" -buttonRightText "Yes" -buttonLeftText "No"
.EXAMPLE
	Show-InstallationPrompt -Title "Funny Prompt" -Message "How are you feeling today?" -ButtonRightText "Good" -ButtonLeftText "Bad" -ButtonMiddleText "Indifferent"
.EXAMPLE
	Show-InstallationPrompt -Message "You can customise text to appear at the end of an install, or remove it completely for unattended installations." -Icon Information -NoWait
.PARAMETER Title
	Title of the prompt
	[Default is the application installation name]
.PARAMETER Message
	Message text to be included in the prompt
.PARAMETER MessageAlignment
	Alignment of the message text (Left,Center,Right) [Default is Center]
.PARAMETER ButtonLeftText
	Show a button on the left of the prompt with the specified text
.PARAMETER ButtonRightText
	Show a button on the right of the prompt with the specified text
.PARAMETER ButtonMiddleText
	Show a button in the middle of the prompt with the specified text
.PARAMETER Icon
	Show a system icon in the prompt ("Application","Asterisk","Error","Exclamation","Hand","Information","None","Question","Shield","Warning","WinLogo") [Default is "None"]
.PARAMETER NoWait
	Specifies whether to show the prompt asynchronously (i.e. allow the script to continue without waiting for a response) [Default is $false]
.PARAMETER PersistPrompt
	Specify whether to make the prompt persist in the center of the screen every 10 seconds. The user will have no option but to respond to the prompt - resistance is futile!
.PARAMETER MinimizeWindows
	Specifies whether to minimize other windows when displaying prompt [Default is false]
.PARAMETER Timeout
	Specifies the period in seconds after which the prompt should timeout [Default is the UI timeout value set in the config XML file]
.PARAMETER ExitOnTimeout
	Specifies whether to exit the script if the UI times out [Default True]
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		$Title = $installTitle,
		$Message = $null,
		[ValidateSet("Left","Center","Right")]
		$MessageAlignment = "Center",
		$ButtonRightText = $null,
		$ButtonLeftText = $null,
		$ButtonMiddleText = $null,
		[ValidateSet("Application","Asterisk","Error","Exclamation","Hand","Information","None","Question","Shield","Warning","WinLogo")]
		[string] $Icon = "None",
		[switch] $NoWait = $false,
		[switch] $PersistPrompt = $false,
		[boolean] $MinimizeWindows = $false,
		$Timeout = $configInstallationUITimeout,
		$ExitOnTimeout = $true

	)

	# Bypass if in non-interactive mode
	If ($deployModeNonInteractive -eq $true) {
		Write-Log "Bypassing Installation Prompt [Mode: $deployMode]... $Message"
		Return
	}

	# Get parameters for calling function asynchronously
	$installPromptParameters = $psBoundParameters

	# Check if the countdown was specified
	If ($timeout -gt $configInstallationUITimeout) {
			Throw "Error: The Show-InstallationPrompt timeout can not be longer than the timeout specified in the XML configuration for installation UI dialogs to timeout."
	}

	[System.Windows.Forms.Application]::EnableVisualStyles()
	$formInstallationPrompt = New-Object System.Windows.Forms.Form
	$pictureBanner = New-Object System.Windows.Forms.PictureBox
	$pictureIcon = New-Object System.Windows.Forms.PictureBox
	$labelText = New-Object System.Windows.Forms.Label
	$buttonRight = New-Object System.Windows.Forms.Button
	$buttonMiddle = New-Object System.Windows.Forms.Button
	$buttonLeft = New-Object System.Windows.Forms.Button
	$buttonAbort = New-Object System.Windows.Forms.Button
	$InitialFormInstallationPromptWindowState = New-Object System.Windows.Forms.FormWindowState

	$Form_Cleanup_FormClosed=
	{
		# Remove all event handlers from the controls
		Try
		{
			$labelText.remove_Click($handler_labelText_Click)
			$buttonLeft.remove_Click($buttonLeft_OnClick)
			$buttonRight.remove_Click($buttonRight_OnClick)
			$buttonMiddle.remove_Click($buttonMiddle_OnClick)
			$buttonAbort.remove_Click($buttonAbort_OnClick)
			$timer.remove_Tick($timer_Tick)
			$timerPersist.remove_Tick($timerPersist_Tick)
			$formInstallationPrompt.remove_Load($Form_StateCorrection_Load)
			$formInstallationPrompt.remove_FormClosed($Form_Cleanup_FormClosed)
		}
		Catch [Exception]
		{ }
	}

	$Form_StateCorrection_Load=
	{
		# Correct the initial state of the form to prevent the .Net maximized form issue
		$formInstallationPrompt.WindowState = 'Normal'
		$formInstallationPrompt.AutoSize = $true
		$formInstallationPrompt.TopMost = $true
		$formInstallationPrompt.BringToFront()
		# Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
		Set-Variable -Name formInstallationPromptStartPosition -Value $($formInstallationPrompt.Location) -Scope Script
	}

	# Form
	$formInstallationPrompt.Controls.Add($pictureBanner)

	#----------------------------------------------
	# Create padding object
	$paddingNone = New-Object System.Windows.Forms.Padding
	$paddingNone.Top = 0
	$paddingNone.Bottom = 0
	$paddingNone.Left = 0
	$paddingNone.Right = 0

	# Generic Label properties
	$labelPadding = "20,0,20,0"

	# Generic Button properties
	$buttonWidth = 110
	$buttonHeight = 23
	$buttonPadding = 50
	$buttonSize = New-Object System.Drawing.Size
	$buttonSize.Width = $buttonWidth
	$buttonSize.Height = $buttonHeight
	$buttonPadding = New-Object System.Windows.Forms.Padding
	$buttonPadding.Top = 0
	$buttonPadding.Bottom = 5
	$buttonPadding.Left = 50
	$buttonPadding.Right = 0

	# Picture Banner
	$pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
	$pictureBanner.ImageLocation = $appDeployLogoBanner
	$System_Drawing_Point = New-Object System.Drawing.Point
	$System_Drawing_Point.X = 0
	$System_Drawing_Point.Y = 0
	$pictureBanner.Location = $System_Drawing_Point
	$pictureBanner.Name = "pictureBanner"
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 50
	$System_Drawing_Size.Width = 450
	$pictureBanner.Size = $System_Drawing_Size
	$pictureBanner.Margin = $paddingNone
	$pictureBanner.TabIndex = 0
	$pictureBanner.TabStop = $False

	# Picture Icon
	$pictureIcon.DataBindings.DefaultDataSourceUpdateMode = 0
	If ($icon -ne "None") {
		$pictureIcon.Image = ([System.Drawing.SystemIcons]::$Icon).ToBitmap()
	}
	$System_Drawing_Point = New-Object System.Drawing.Point
	$System_Drawing_Point.X = 15
	$System_Drawing_Point.Y = 105
	$pictureIcon.Location = $System_Drawing_Point
	$pictureIcon.Name = "pictureIcon"
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 32
	$System_Drawing_Size.Width = 32
	$pictureIcon.Size = $System_Drawing_Size
	$pictureIcon.Margin = $paddingNone
	$pictureIcon.TabIndex = 0
	$pictureIcon.TabStop = $False

	# Label Text
	$labelText.DataBindings.DefaultDataSourceUpdateMode = 0
	$labelText.Name = "labelText"
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 148
	$System_Drawing_Size.Width = 385
	$labelText.Size = $System_Drawing_Size
	$System_Drawing_Point = New-Object System.Drawing.Point
	$System_Drawing_Point.X = 25
	$System_Drawing_Point.Y = 50
	$labelText.Location = $System_Drawing_Point
	$labelText.Margin = "0,0,0,0"
	$labelText.Padding = $labelPadding
	$labelText.TabIndex = 1
	$labelText.Text = $message
	$labelText.TextAlign = "Middle$($MessageAlignment)"
	$labelText.Anchor = "Top"
	$labelText.add_Click($handler_labelText_Click)

	# Button Left
	$buttonLeft.DataBindings.DefaultDataSourceUpdateMode = 0
	$buttonLeft.Location = "15,200"
	$buttonLeft.Name = "buttonLeft"
	$buttonLeft.Size = $buttonSize
	$buttonLeft.TabIndex = 5
	$buttonLeft.Text = $buttonLeftText
	$buttonLeft.DialogResult = 'No'
	$buttonLeft.AutoSize = $false
	$buttonLeft.UseVisualStyleBackColor = $True
	$buttonLeft.add_Click($buttonLeft_OnClick)

	# Button Middle
	$buttonMiddle.DataBindings.DefaultDataSourceUpdateMode = 0
	$buttonMiddle.Location = "170,200"
	$buttonMiddle.Name = "buttonMiddle"
	$buttonMiddle.Size = $buttonSize
	$buttonMiddle.TabIndex = 6
	$buttonMiddle.Text = $buttonMiddleText
	$buttonMiddle.DialogResult = 'Ignore'
	$buttonMiddle.AutoSize = $true
	$buttonMiddle.UseVisualStyleBackColor = $True
	$buttonMiddle.add_Click($buttonMiddle_OnClick)

	# Button Right
	$buttonRight.DataBindings.DefaultDataSourceUpdateMode = 0
	$buttonRight.Location = "325,200"
	$buttonRight.Name = "buttonRight"
	$buttonRight.Size = $buttonSize
	$buttonRight.TabIndex = 7
	$buttonRight.Text = $ButtonRightText
	$buttonRight.DialogResult = 'Yes'
	$buttonRight.AutoSize = $true
	$buttonRight.UseVisualStyleBackColor = $True
	$buttonRight.add_Click($buttonRight_OnClick)

	# Button Abort (Hidden)
	$buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
	$buttonAbort.Name = "buttonAbort"
	$buttonAbort.Size = "1,1"
	$buttonAbort.DialogResult = 'Abort'
	$buttonAbort.TabIndex = 5
	$buttonAbort.UseVisualStyleBackColor = $True
	$buttonAbort.add_Click($buttonAbort_OnClick)

	# Form Installation Prompt
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 270
	$System_Drawing_Size.Width = 450
	$formInstallationPrompt.Size = $System_Drawing_Size
	$formInstallationPrompt.Padding = "0,0,0,10"
	$formInstallationPrompt.Margin = $paddingNone
	$formInstallationPrompt.DataBindings.DefaultDataSourceUpdateMode = 0
	$formInstallationPrompt.Name = "WelcomeForm"
	$formInstallationPrompt.Text = $title
	$formInstallationPrompt.StartPosition = 'CenterScreen'
	$formInstallationPrompt.FormBorderStyle = 'FixedDialog'
	$formInstallationPrompt.MaximizeBox = $false
	$formInstallationPrompt.MinimizeBox = $false
	$formInstallationPrompt.TopMost = $True
	$formInstallationPrompt.TopLevel = $True
	$formInstallationPrompt.Icon = New-Object System.Drawing.Icon ($AppDeployLogoIcon)
	$formInstallationPrompt.Controls.Add($pictureBanner)
	$formInstallationPrompt.Controls.Add($pictureIcon)
	$formInstallationPrompt.Controls.Add($labelText)
	$formInstallationPrompt.Controls.Add($buttonAbort)
	If ($buttonLeftText) {
		$formInstallationPrompt.Controls.Add($buttonLeft)
	}
	If ($buttonMiddleText) {
		$formInstallationPrompt.Controls.Add($buttonMiddle)
	}
	If ($buttonRightText) {
		$formInstallationPrompt.Controls.Add($buttonRight)
	}

	# Timer
	$timer = New-Object 'System.Windows.Forms.Timer'
	$timer.Interval = ($timeout * 1000)
	$timer.Add_Tick({
		Write-Log "Installation not actioned within a reasonable amount of time."
		$buttonAbort.PerformClick()
	})

	# Persistence Timer
	If ($persistPrompt) {
		$timerPersist = New-Object 'System.Windows.Forms.Timer'
		$timerPersist.Interval = ($configInstallationPersistInterval * 1000)
		$timerPersist_Tick = {
			Refresh-InstallationPrompt
		}
		$timerPersist.add_Tick($timerPersist_Tick)
		$timerPersist.Start()
	}

	# Save the initial state of the form
	$InitialFormInstallationPromptWindowState = $formInstallationPrompt.WindowState
	# Init the OnLoad event to correct the initial state of the form
	$formInstallationPrompt.add_Load($Form_StateCorrection_Load)
	# Clean up the control events
	$formInstallationPrompt.add_FormClosed($Form_Cleanup_FormClosed)

	# Start the timer
	$timer.Start()

	Function Refresh-InstallationPrompt {
		$formInstallationPrompt.BringToFront()
		$formInstallationPrompt.Location = "$($formInstallationPromptStartPosition.X),$($formInstallationPromptStartPosition.Y)"
		$formInstallationPrompt.Refresh()
	}

	# Close the Installation Progress Dialog if running
	Close-InstallationProgress

	$installPromptLoggedParameters = ($installPromptParameters.GetEnumerator() | % { "($($_.Key)=$($_.Value))" }) -join " "
	Write-Log "Displaying custom installation prompt with the non-default parameters: [$installPromptLoggedParameters]..."

	# If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously
	If ($NoWait -eq $true) {
		# Remove the NoWait parameter so that the script is run synchronously in the new PowerShell session
		$installPromptParameters.Remove("NoWait")
		# Format the parameters as a string
		$installPromptParameters = ($installPromptParameters.GetEnumerator() | % { "-$($_.Key) `"$($_.Value)`""}) -join " "
		Start-Process $PSHOME\powershell.exe -ArgumentList "-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File `"$scriptPath`" -ReferringApplication `"$installName`" -ShowInstallationPrompt $installPromptParameters" -WindowStyle Hidden -ErrorAction SilentlyContinue
	}
	# Otherwise show the prompt synchronously, and keep showing it if the user cancels it until the respond using one of the buttons
	Else {
		$showDialog = $true
		While ($showDialog -eq $true) {
			If ($minimizeWindows -eq $true) {
				# Minimize all other windows
				$shellApp.MinimizeAll()
			}
			# Show the Form
			$result = $formInstallationPrompt.ShowDialog()
			If ($result -eq "Yes" -or $result -eq "No" -or $result -eq "Ignore" -or $result -eq "Abort") {
				$showDialog = $false
			}
		}

		Switch ($result) {
			"Yes" { Return $buttonRightText}
			"No" { Return $buttonLeftText}
			"Ignore" { Return $buttonMiddleText}
			"Abort" {
				# Restore minimized windows
				$shellApp.UndoMinimizeAll()
				If ($ExitOnTimeout -eq $true) {
					Exit-Script $configInstallationUIExitCode
				}
				Else {
					Write-Log "UI timed out but ExitOnTimeout set to false. Continuing..."
				}
			}
		}
	}

} #End Function

Function Show-DialogBox {
<#
.SYNOPSIS
	This function displays a custom dialog box with optional title, buttons, icon and timeout.
	The Show-InstallationPrompt function is recommended over this as it provides more customization and uses consistent branding with the other UI components.
.DESCRIPTION
	This function displays a custom dialog box with optional title, buttons, icon and timeout. The default button is "OK", the default Icon is "None" and the default Timeout is none.
.EXAMPLE
	Show-DialogBox -Title "Installed Complete" -Text "Installation has completed. Please click OK and restart your computer." -Icon "Information"
.EXAMPLE
	Show-DialogBox -Title "Installation Notice" -Text "Installation will take approximately 30 mintues. Do you wish to proceed?" -Buttons "OKCancel" -DefaultButton "Second" -Icon "Exclamation" -Timeout 600
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
.PARAMETER DefaultButton
	The Default button that is selected [Default is "First"]
	"First"
	"Second"
	"Third"
.PARAMETER Icon
	Icon to display on the dialog box [Default is "None"]
	Acceptable valures are: "None",	"Stop", "Question", "Exclamation", "Information",
.PARAMETER Timeout
	Timeout period in seconds before automatically closing the dialog box with the return message "Timeout" [Default is the UI timeout value set in the config XML file]
.PARAMETER TopMost
	Specifies whether the message box is a system modal message box and appears in a topmost window. [Default is True]
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
	[ValidateNotNullorEmpty()]
	[Parameter(Position=0,Mandatory=$True,HelpMessage="Enter a message for the dialog box")]
	[string] $Text,
	[string] $Title = $installTitle,
	[ValidateSet("OK","OKCancel","AbortRetryIgnore","YesNoCancel","YesNo","RetryCancel","CancelTryAgainContinue")]
	[string] $Buttons = "OK",
	[ValidateSet("First","Second","Third")]
	[string] $DefaultButton = "First",
	[ValidateSet("Exclamation","Information","None","Stop","Question")]
	[string] $Icon = "None",
	[string] $Timeout = $configInstallationUITimeout,
	[switch] $TopMost = $true
 	)

	# Bypass if in non-interactive mode
	If ($deployModeNonInteractive -eq $true) {
		Write-Log "Bypassing Dialog Box [Mode: $deployMode]... $Text"
		Return
	}

	Write-Log "Displaying Dialog Box with message: [$Text]..."

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

	$dialogDefaultButton = @{
		"First" = 0
		"Second" = 256
		"Third" = 512
	}

	Switch ($TopMost) {
		$true { $dialogTopMost = 4096 }
		$false { $dialogTopMost = 0 }
	}

	$wshell = New-Object -COMObject WScript.Shell
	$response = $wshell.Popup($Text,$Timeout,$Title,$dialogButtons[$Buttons]+$dialogIcons[$Icon]+$dialogDefaultButton[$DefaultButton]+$dialogTopMost)

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
		10 {
			Write-Log "Dialog Box Response: Try Again"
			Return "Try Again"
		}
		11 {
			Write-Log "Dialog Box Response: Continue"
			Return "Copnt"
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.EXAMPLE
	Get-HardwarePlatform
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[boolean] $ContinueOnError = $true
	)

	Try {
		$hwBios = Get-WmiObject Win32_BIOS | Select-Object "Version","SerialNnumber"
		$hwMakeModel = Get-WMIObject Win32_ComputerSystem | Select-Object "Model","Manufacturer"
	}
	Catch {
		Write-Log "Error retrieving hardware platform information."
		If ($ContinueOnError -eq $false) { Throw "Error retrieving hardware platform information." }
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
	Get-FreeDiskSpace -Drive "C:"
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[string] $Drive = $envSystemDrive,
		[boolean] $ContinueOnError = $true
	)

	Try {
		$disk = Get-WmiObject Win32_LogicalDisk -Filter "DeviceID='$Drive'" -ErrorAction SilentlyContinue
		$freeDiskSpace = [Math]::Round($disk.Freespace / 1MB)
	}
	Catch {
		Write-Log "Error retrieving free disk space for drive $Drive."
		If ($ContinueOnError -eq $false) { Throw "Error retrieving free disk space for drive $Drive." }
	}

	Return $freeDiskSpace
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
		[array] $Name = "",
		[string] $ProductCode = ""
	)

	If ($name -ne "") { Write-Log "Getting information for installed Application Name [$name]..."}
	If ($productCode -ne "") { Write-Log "Getting information for installed Product Code [$ProductCode]..."}
	If ($name -eq "" -and $ProductCode -eq "") { Write-Log "Get-InstalledApplication Error: Please provide an Application Name or Product Code."; Return $null }
	# Replace special characters in product code that interfere with regex match
	$productCode = $productCode -replace "}","" -replace "{",""
	$applications = $name -split (",")
	# Replace special characters in application name that interfere with regex match
	$applications = $applications -replace "\.","dot" -replace "\*","asterix" -replace "\+","plus" -replace "\(","openbracket" -replace "\)","closebracket" -replace "\[","opensquarebracket" -replace "\]","closesquarebracket"
	$installedApplication = @()
	Foreach ($regKey in $regKeyApplications ) {
		If (Test-Path $regKey -ErrorAction SilentlyContinue) {
		$regKeyApplication = Get-ChildItem $regKey -ErrorAction SilentlyContinue | ForEach-Object { Get-ItemProperty -LiteralPath $_.PsPath }
			Foreach ($regKeyApp in $regKeyApplication) {
				$appDisplayName = $null
				$appDisplayVersion = $null
				$appPublisher = $null
				# Bypass any updates or hotfixes
				If ([RegEx]::Match($regKeyApp.DisplayName, "(?i)kb\d+") -eq $true) { Continue }
				If ($regKeyApp.DisplayName -match "Cumulative Update") { Continue }
				If ($regKeyApp.DisplayName -match "Security Update") { Continue }
				If ($regKeyApp.DisplayName -match "Hotfix") { Continue }
				# Remove any non-standard characters from the name / version which may interfere with logging
				$appDisplayName = [RegEx]::Replace($regKeyApp.DisplayName, "[^\u001F-\u007F]", "")
				$appDisplayVersion = [RegEx]::Replace($regKeyApp.DisplayVersion, "[^\u001F-\u007F]", "")
				$appPublisher = [RegEx]::Replace($regKeyApp.Publisher, "[^\u001F-\u007F]", "")
				If ($ProductCode -ne "") {
					# Replace special characters in product code that interfere with regex match
					$regKeyProductCode = $($regKeyApp.PSChildName) -replace "}","" -replace "{",""
					# Verify if there is a match with the product code passed to the script
					If ($regKeyProductCode -match $productCode) {
						Write-Log "Found installed application [$($appDisplayName)] version [$($appDisplayVersion)] matching product code [$productCode]"
						$installedApplication += New-Object PSObject -Property @{
							ProductCode	=		$regKeyApp.PSChildName
							DisplayName	= 		$appDisplayName
							DisplayVersion =	$appDisplayVersion
							UninstallString =	$regKeyApp.UninstallString
							InstallSource =		$regKeyApp.InstallSource
							InstallLocation =	$regKeyApp.InstallLocation
							InstallDate =		$regKeyApp.InstallDate
							Publisher =			$appPublisher
						}
					}
				}
				If ($name -ne "") {
					# Verify if there is a match with the application name(s) passed to the script
					Foreach ($application in $applications) {
						If (($regKeyApp.DisplayName -replace "\.","dot" -replace "\*","asterix" -replace "\+","plus" -replace "\(","openbracket" -replace "\)","closebracket" -replace "\[","opensquarebracket" -replace "\]","closesquarebracket") -match $application ) {
							Write-Log "Found installed application [$($appDisplayName)] version [$($appDisplayVersion)] matching application name [$application]"
							$regKeyApp.DisplayName = $regKeyApp.DisplayName
							$installedApplication += New-Object PSObject -Property @{
								ProductCode	=		$regKeyApp.PSChildName
								DisplayName =		$appDisplayName
								DisplayVersion =	$appDisplayVersion
								UninstallString =	$regKeyApp.UninstallString
								InstallSource =		$regKeyApp.InstallSource
								InstallLocation =	$regKeyApp.InstallLocation
								InstallDate =		$regKeyApp.InstallDate
								Publisher =			$appPublisher
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
	Execute-MSI -Action Install -Path "Adobe_FlashPlayer_11.2.202.233_x64_EN.msi"
	Installs an MSI
.EXAMPLE
	Execute-MSI -Action Install -Path "Adobe_FlashPlayer_11.2.202.233_x64_EN.msi" -Transform "Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst" -Parameters "/QN"
	Installs an MSI, applying a transform and overriding the default MSI toolkit parameters
.EXAMPLE
	Execute-MSI -Action Uninstall -Path "{26923b43-4d38-484f-9b9e-de460746276c}"
	Uninstalls an MSI using a product code
.EXAMPLE
	Execute-MSI -Action Patch -Path "Adobe_Reader_11.0.3_EN.msp"
	Installs an MSP
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
		[boolean] $ContinueOnError = $false # Fail if there is an error (Default)
	)

	# Build the log file name
	If (!($logName)) {
		# If the path matches a product code, resolve the product code to an application name and version
		If ($path -match "^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$") {
			Write-Log "Execute-MSI: Product code specified, attempting to resolve product code to an application name and version..."
			$productCodeNameVersion = (Get-InstalledApplication -ProductCode $path | Select DisplayName,DisplayVersion -First 1 -ErrorAction SilentlyContinue)
			If ($productCodeNameVersion -ne $null) {
				If ($($productCodeNameVersion.Publisher) -ne $null) {
					$logName = ($productCodeNameVersion.Publisher + "_" + $productCodeNameVersion.DisplayName + "_" + $productCodeNameVersion.DisplayVersion) -replace " ","" -replace "\\","" -replace "/",""
				}
				Else {
					$logName = ( $productCodeNameVersion.DisplayName + "_" + $productCodeNameVersion.DisplayVersion) -replace " ","" -replace "\\","" -replace "/",""
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

	If ($configToolkitCompressLogs) {
		# Build the log file path
		$logPath = Join-Path $logTempFolder $logName
	}
	Else {
		# Create the Log directory if it doesn't already exist
		If (!(Test-Path -path $configMSILogDir -ErrorAction SilentlyContinue )) { New-Item $configMSILogDir -Type directory -ErrorAction SilentlyContinue | Out-Null }
		# Build the log file path
		$logPath = Join-Path $configMSILogDir $logName
	}

	# Set the installation Parameters
	If ($deployModeSilent -eq $true) {
		$msiInstallDefaultParams = $configMSISilentParams
		$msiUninstallDefaultParams = $configMSISilentParams
	}
	Else {
		$msiInstallDefaultParams = $configMSIInstallParams
		$msiUninstallDefaultParams = $configMSIUninstallParams
	}

	# Build the MSI Parameters
	Switch ($action) {
		"Install" 			{ $option = "/i"; $msiLogFile = $logPath + "_Install"; $msiDefaultParams = $msiInstallDefaultParams }
		"Uninstall"			{ $option = "/x"; $msiLogFile = $logPath + "_Uninstall"; $msiDefaultParams = $msiUninstallDefaultParams }
		"Patch" 			{ $option = "/update"; $msiLogFile = $logPath + "_Patch"; $msiDefaultParams = $msiInstallDefaultParams }
		"Repair"			{ $option = "/f"; $msiLogFile = $logPath + "_Repair"; $msiDefaultParams = $msiInstallDefaultParams }
		"ActiveSetup"		{ $option = "/fups"; $msiLogFile = $logPath + "_ActiveSetup" }
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
	ElseIf ($transform) {
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
		Execute-Process -FilePath $exeMsiexec -Arguments $argsMSI -WorkingDirectory $WorkingDirectory -WindowStyle Normal -ContinueOnError $true
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
		[boolean] $ContinueOnError = $true
	)

	$installedApplications = Get-InstalledApplication $name
	If ($installedApplications -ne "") {
		Foreach ($installedApplication in $installedApplications) {
			If ($installedApplication.UninstallString -match "msiexec") {
				Write-Log "Removing Application [$($installedApplication.DisplayName) $($installedApplication.Version)]..."
				If ($ContinueOnError -eq $true) {
					Execute-MSI -Action Uninstall -Path $installedApplication.ProductCode -ContinueOnError $true
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
.PARAMETER NoWait
    Immediately continue after executing the process.
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
        [string] $WorkingDirectory = $null,
        [switch] $NoWait = $false,
        [switch] $PassThru = $false,
        [string] $IgnoreExitCodes = $false,
        [boolean] $ContinueOnError = $false # Fail if there is an error (Default)
    )

    # If the file is in the Files subdirectory of the App Deploy Toolkit, set the full path to the file
    If (Test-Path (Join-Path $dirFiles $FilePath -ErrorAction SilentlyContinue) -ErrorAction SilentlyContinue) {
        $FilePath = (Join-Path $dirFiles $FilePath)
    }

    # Set the Working directory (if not specified)
    If ($WorkingDirectory -eq $null -or $WorkingDirectory -eq "") {
        $WorkingDirectory = (Split-Path $FilePath -Parent)
    }

    Write-Log "Working Directory is [$WorkingDirectory]"

    Try {
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

        Write-Log "Executing [$FilePath $Arguments]..."
        $process = [System.Diagnostics.Process]::Start($processStartInfo)

        If ($NoWait -eq $true) {
            Write-Log ("NoWait parameter specified. Continuing without checking exit code...")
            # Free resources associated with the process, this does not cause process to exit
            $process.Close()
        }
        Else {
            $stdOut = $process.BeginOutputReadLine() -replace "`0",""       # `0 = Null
            $stdErr = $process.StandardError.ReadToEnd() -replace "`0",""   # `0 = Null

            $processName = $process.ProcessName

            If($stdErr.length -gt 0) {Write-Log "Standard error output from the process [$processName]: $stdErr"}

            # Instructs the Process component to wait indefinitely for the associated process to exit.
            $process.WaitForExit()
            Do
            {
                # HasExited indicates that the associated process has terminated, either normally or abnormally
                # We will wait until HasExited returns true
                If (-not $process.HasExited)
                {
                    Start-Sleep -Seconds 1
                }
            } Until ($process.HasExited)
            
            # Get the exit code for the process
            $returnCode = $process.ExitCode
            
            # Free resources associated with the process, this does not cause process to exit
            $process.Close()

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
                    ExitCode = $returnCode
                    StdOut = $stdOut
                    StdErr = $stdErr
                }
                Write-Log "Execution completed with exit code [$returnCode]"
            }
            ElseIf ($ignoreExitCodeMatch -eq $true) {
                Write-Log "Execution complete and the exit code [$returncode] is being ignored"
            }
            ElseIf (($returnCode -eq 3010) -or ($returnCode -eq 1641) ) {
                Write-Log "Execution completed successfully with exit code [$returnCode]. A reboot is required."
                Set-Variable -Name msiRebootDetected -Value $true -Scope Script
            }
            ElseIf (($returnCode -eq 1605) -and ($filePath -match 'msiexec')) {
                Write-Log "Execution failed with exit code [$returnCode] because the product is not currently installed."
            }
            ElseIf (($returnCode -eq -2145124329) -and ($filePath -match 'wusa')) {
                Write-Log "Execution failed with exit code [$returnCode] because the Windows Update is not applicable to this system."
            }
            ElseIf (($returnCode -eq 17025) -and ($filePath -match "fullfile")) {
                Write-Log "Execution failed with exit code [$returnCode] because the Office Update is not applicable to this system."
            }
            ElseIf ($returnCode -eq 0) {
                Write-Log "Execution completed successfully with exit code [$returnCode]"
            }
            Else {
                Write-Log ("Execution failed with exit code [$returnCode]")
                Exit-Script $returnCode
            }
        }
    }
    Catch [Exception] {
        Write-Log ("Execution failed: " + $_.Exception.Message)
        If ($returnCode -eq $null) { $returnCode = 999 }
        Exit-Script $returnCode
    }
    Finally
    {
        # Re-enable Zone checking
        Remove-Item env:SEE_MASK_NOZONECHECKS -ErrorAction SilentlyContinue
    }
}

Function New-Folder { 
<# 
.SYNOPSIS
	Function to create a new folder.
.DESCRIPTION
	Function to create a new folder if it does not exist.
.EXAMPLE
	New-Folder -Path "$envWinDir\System32"
.PARAMETER Path
	Path of the folder you want to create
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES 
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>	Param(
		[Parameter(Mandatory = $true)]
		[string]$Path = $(Throw "Path param required"),
		[boolean] $ContinueOnError = $true
	)

	Try {
		Write-Log "Testing if folder [$Path] exists..."
		$CheckFolder = Test-Path -PathType Container $Path

		If ($CheckFolder -eq $False) {
			Write-Log "Creating Folder [$Path]..."
			New-Item $Path -type Directory
		}
		Else {
			Write-Log "$Path folder already exists..."
		}
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Could not create folder [$path]:" + $_.Exception.Message)
			Continue
		}
		Else {
			Throw $("Could not create folder [$path]:" + $_.Exception.Message)
		}
	}
}

Function Remove-Folder {
<# 
.SYNOPSIS
	Function to remove a folder and files if they exist.
.DESCRIPTION
	Function to remove a folder and all files recursively in a given path.
.EXAMPLE
	Remove-Folder -Path "$envWinDir\Downloaded Program Files"
.PARAMETER Path
	Path of the folder you want to remove
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES 
.LINK
Http://psappdeploytoolkit.codeplex.com
#>	Param(
		[Parameter(Mandatory = $true)]
		[string] $Path = $(throw "Path Param required"),
		[boolean] $ContinueOnError = $true
	)

	Try {
		Write-Log "Testing if folder [$Path] exists..."
		$CheckFolder = Test-Path -PathType Container $Path

		If ($CheckFolder -ne $False) {
			Write-Log "Deleting Folder(s) and Files [$path]..."
			Remove-Item -Path "$path" -ErrorAction "STOP" -Force -Recurse | Out-Null
		}
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Could not delete folder and files [$path]:" + $_.Exception.Message)
			Continue
		}
		Else {
			Throw $("Could not delete folder and files [$path]:" + $_.Exception.Message)
		}
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
.PARAMETER Recurse
	Copy files in subdirectories
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>	Param(
		[Parameter(Mandatory = $true)]
		[string]$Path = $(throw "Path param required"),
		[Parameter(Mandatory = $true)]
		[string]$Destination = $(throw "Destination param required"),
		[switch]$Recurse = $false,
		[boolean] $ContinueOnError = $true
	)

	Try {
		If ($Recurse) {
			Write-Log "Copying File [$path] to [$destination] recursively..."
			Copy-Item -Path "$Path" -Destination "$destination" -ErrorAction "STOP" -Force -Recurse | Out-Null
		}
		Else {
			Write-Log "Copying File [$path] to [$destination]..."
			Copy-Item -Path "$Path" -Destination "$destination" -ErrorAction "STOP" -Force | Out-Null
		}
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Could not copy file [$path] to [$destination]:" + $_.Exception.Message)
		}
		Else {
			Throw $("Could not copy file [$path] to [$destination]:" + $_.Exception.Message)
		}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param(
		[Parameter(Mandatory = $true)]
		[string] $Path = $(throw "Path Param required"),
		[switch] $Recurse,
		[boolean] $ContinueOnError = $true
	)

	Write-Log "Deleting File(s) [$path]..."
	Try {
		If ($Recurse) {
			Remove-Item -Path "$path" -ErrorAction "STOP" -Force -Recurse | Out-Null
		}
		Else {
			Remove-Item -Path "$path" -ErrorAction "STOP" -Force | Out-Null
		}
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Could not delete file [$path]:" + $_.Exception.Message)
		}
		Else {
			Throw $("Could not delete file [$path]:" + $_.Exception.Message)
		}
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
	Retrieves value names and value data for a specified registry key or optionally, a specific value
.DESCRIPTION
	Retrieves value names and value data for a specified registry key or optionally, a specific value
	If the registry key does not contain any values, the function will return $null. If you need to test for existence of a registry key path, use the built-in Test-Path cmdlet
.EXAMPLE
	Get-RegistryKey "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}"
.EXAMPLE
	Get-RegistryKey "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\iexplore.exe"
.EXAMPLE
	Get-RegistryKey "HKLM:\Software\Wow6432Node\Microsoft\Microsoft SQL Server Compact Edition\v3.5" -Value "Version"
.PARAMETER Key
	Path of the registry key
.PARAMETER Value
	Value to retrieve (optional)
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		$Key,
		$Value,
		[boolean] $ContinueOnError = $true
	)

	$key = Convert-RegistryPath -Key $key

	If ($Value -eq $null) {
		Write-Log "Getting Registry key [$key] ..."
	}
	Else {
		Write-Log "Getting Registry key [$key] value [$value] ..."
	}

	# Check if the registry key exists
	If (Test-Path -Path $key -ErrorAction SilentlyContinue) {
		Try {
			If ($Value -eq $null) {
				# Get the Key
				$regKeyValue = Get-ItemProperty -Path $key
			}
			Else {
				# Get the Value
				$regKeyValue = Get-ItemProperty -Path $key | Select $Value -ExpandProperty $Value
			}
			If ($regKeyValue -ne "") {
				Return $regKeyValue
			}
			Else {
				Return $null
			}
		}
		Catch [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log $("Registry key does not exist: [$key]" + $_.Exception.Message)
			}
			Else {
				Throw $("Registry key does not exist: [$key]" + $_.Exception.Message)
			}
		}
	}
	Else {
		Write-Log "Registry key does not exist: [$key]"
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		[string] $Key,
		[string] $Name,
		$Value,
		[Microsoft.Win32.RegistryValueKind] $Type = "String",
		[boolean] $ContinueOnError = $true
	)

	$key = Convert-RegistryPath -Key $Key

	# Create registry key if it doesn't exist
	If (!(Test-Path $key -ErrorAction SilentlyContinue)) {
		Write-Log "Creating Registry key [$key]..."
		Try {
			New-Item -Path $key -ItemType Registry -Force | Out-Null
		}
		Catch [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to create registry key [$Key]:" + $_.Exception.Message)
			}
			Else {
				Throw $("Failed to create registry key [$Key]: " + $_.Exception.Message)
			}
		}
	}

	If ($Name) {
		Try {
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
		}
		Catch [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to set registry value [$value] for registry key [$key] [$name]: " + $_.Exception.Message)
			}
			Else {
				Throw $("Failed to set registry value [$value] for registry key [$key] [$name]: " + $_.Exception.Message)
			}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param(
		[Parameter(Mandatory = $true)]
		[string] $Key = $(throw "Key Param required"),
		[string] $Name,
		[switch] $Recurse,
		[boolean] $ContinueOnError = $true
	)

	$key = Convert-RegistryPath -Key $key

	If (!($name)) {
		Write-Log "Deleting Registry Key [$key]..."
		Try {
			If ($Recurse) {
				Remove-Item -Path $Key -ErrorAction "STOP" -Force -Recurse | Out-Null
			}
			Else {
				Remove-Item -Path $Key -ErrorAction "STOP" -Force | Out-Null
			}
		}
		Catch [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to delete registry key [$Key]: " + $_.Exception.Message)
			}
			Else {
				Throw $("Failed to delete registry key [$Key]: " + $_.Exception.Message)
			}
		}
	}
	Else {
		Write-Log "Deleting Registry Value [$Key] [$name] ..."
		Try {
			Remove-ItemProperty -Path $Key -Name $Name -ErrorAction "STOP" -Force | Out-Null
		}
		Catch [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to delete registry value [$Key] [$name]: " + $_.Exception.Message)
			}
			Else {
				Throw $("Failed to delete registry value [$Key] [$name]: " + $_.Exception.Message)
			}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		[string] $File,
		[boolean] $ContinueOnError = $true
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
			Else {
				Write-Log "No file version information found."
			}
		}
		Catch [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log "Error getting file version info: " + $_.Exception.Message
			}
			Else {
				Throw "Error getting file version info: " + $_.Exception.Message
			}
		}
	}
	Else {
		If ($ContinueOnError -eq $true) {
			Write-Log "File could not be found."
		}
		Else {
			Throw "File could not be found."
		}
	}
}

Function New-Shortcut {
<#
.SYNOPSIS
	Creates a new shortcut .lnk or .url file, which can be used for example on the start menu.
.DESCRIPTION
	Creates a new shortcut .lnk or .url file, with configurable options.
.EXAMPLE
	New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\system32\notepad.exe" -IconLocation "$envWinDir\system32\notepad.exe" -Description "Notepad" -WorkingDirectory "$envHomeDrive\$envHomePath"
.PARAMETER Path
	Path to save the shortcut
.PARAMETER TargetPath
	Target path or URL that the shortcut launches
.PARAMETER Arguments
	Arguments to be passed to the target path
.PARAMETER IconLocation
	Location of the icon used for the shortcut
.PARAMETER Description
	Description of the shortcut
.PARAMETER WorkingDirectory
	Working Directory to be used for the target path
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		[string] $Path,
		[Parameter(Mandatory = $true)]
		[string] $TargetPath,
		[string] $Arguments,
		[string] $IconLocation,
		[string] $Description,
		[string] $WorkingDirectory,
		[boolean] $ContinueOnError = $true
	)

	$PathDirectory = ([System.IO.FileInfo]$Path).DirectoryName
	Try {
		If (!(Test-Path -Path $PathDirectory)) {
			Write-Log "Creating shortcut directory..."
			New-Item -ItemType Directory -Path $PathDirectory -ErrorAction SilentlyContinue -Force | Out-Null
		}
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Failed to create shortcut directory [$PathDirectory]: " + $_.Exception.Message)
		}
		Else {
			Throw $("Failed to create shortcut directory [$PathDirectory]: " + $_.Exception.Message)
		}
	}


	Write-Log "Creating shortcut [$path]..."
	Try { 
		$shortcut = $shell.CreateShortcut($path)
		$shortcut.TargetPath = $targetPath
		$shortcut.Arguments = $arguments
		$shortcut.IconLocation = $iconLocation
		$shortcut.Description = $description
		$shortcut.WorkingDirectory = $workingDirectory
		$shortcut.Save()
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Failed to create shortcut [$path]: " + $_.Exception.Message)
		}
		Else {
			Throw $("Failed to create shortcut [$path]: " + $_.Exception.Message)
		}
	}

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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[boolean] $ContinueOnError = $true
	)

	Write-Log "Refreshing Desktop..."

	$refreshDesktopCode = @'
private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
private const int WM_SETTINGCHANGE = 0x1a;
private const int SMTO_ABORTIFHUNG = 0x0002;
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError=true, CharSet=CharSet.Auto)] static extern bool SendNotifyMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SendMessageTimeout ( IntPtr hWnd, int Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult );
[System.Runtime.InteropServices.DllImport("Shell32.dll")] private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

public static void Refresh() {
	SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
	SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 100, IntPtr.Zero);
}
'@

	Try {
		Add-Type -MemberDefinition $refreshDesktopCode -Namespace MyWinAPI -Name Explorer
		[MyWinAPI.Explorer]::Refresh()
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Error refreshing Desktop: " + $_.Exception.Message)
		}
		Else {
			Throw $("Error refreshing Desktop: " + $_.Exception.Message)
		}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered [Default is false]
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>

	Param (
		[boolean] $ContinueOnError = $true
	)

	Write-Log "Retrieving Scheduled Tasks..."
	Try {
		&$exeSchTasks /Query /FO CSV | ConvertFrom-Csv –Header “TaskName”
	}
	Catch {
		If ($ContinueOnError -eq $false) {
			Throw "Error retrieving scheduled tasks."
		}
	}
}

Function Block-AppExecution {
<#
.SYNOPSIS
	Function to block the execution of an application(s)
.DESCRIPTION
	This function is called when you pass the -BlockExecution parameter to the Stop-RunningApplications function. It does the following:
	1. Makes a copy of this script in a temporary directory on the local machine.
	2. Checks for an existing scheduled task from previous failed installation attemp where apps were blocked and if found, calls the Unblock-AppExecution function to restore the original IFEO registry keys.
		This is to prevent the function from overriding the backup of the original IFEO options.
	3. Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameters -CleanupBlockedApps and -ReferringApplication
	4. Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameters -ShowBlockedAppDialog and -ReferringApplication
	5. When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress.
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

	# Create Temporary directory (if required) and copy Toolkit  so it can be called by scheduled task later if required
	If (!(Test-Path -path $dirAppDeployTemp -ErrorAction SilentlyContinue )) { New-Item $dirAppDeployTemp -Type Directory -ErrorAction SilentlyContinue | Out-Null }
	Copy-Item -Path "$scriptRoot\*.*" -Destination $dirAppDeployTemp -Exclude "thumbs.db" -Force -Recurse -ErrorAction SilentlyContinue

	# Built the debugger block value
	$debuggerBlockValue = "powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File `"$dirAppDeployTemp\$scriptFileName`" -ShowBlockedAppDialog -ReferringApplication `"$installName`""

	# Create a scheduled task to run on startup to call this script and cleanup blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
	Write-Log "Creating Scheduled task to cleanup blocked applications in case installation is interrupted..."
	If (Get-ScheduledTask -ContinueOnError $true | Select TaskName | Where { $_.TaskName -eq "\$schTaskBlockedAppsName" } ) {
		Write-Log "Scheduled task $schTaskBlockedAppsName already exists."
	}
	Else { 
		$schTaskCreation = Execute-Process -FilePath $exeSchTasks -Arguments "/Create /TN $schTaskBlockedAppsName /RU System /SC ONSTART /TR `"powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File `'$dirAppDeployTemp\$scriptFileName`' -CleanupBlockedApps -ReferringApplication `'$installName`'`"" -PassThru
	}

	$blockProcessName = $processName
	# Append .exe to match registry keys
	$blockProcessName = $blockProcessName | ForEach-Object { $_ + ".exe" } -ErrorAction SilentlyContinue

	# Enumerate each process we want to block
	Foreach ($blockProcess in $blockProcessName) {
		# Set the debugger value to block application execution
		Write-Log "Setting the Image File Execution Options registry keys to block execution of $blockProcess..."
		Set-RegistryKey -Key (Join-Path $regKeyAppExecution $blockProcess) -Name "Debugger" -Value $debuggerBlockValue -ContinueOnError $true
	}
}

Function UnBlock-AppExecution {
<#
.SYNOPSIS
	Unblocks the execution of applications performed by the Block-AppExecution function
.DESCRIPTION
	This function is called by the Exit-Script function or when the script itself is called with the parameters -CleanupBlockedApps and -ReferringApplication
.EXAMPLE
	UnblockAppExecution
.NOTES
	This is an internal script function and should typically not be called directly.
	It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to undo the acitons performed by Block-AppExecution.
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	# Bypass if in NonInteractive mode
	If ($deployModeNonInteractive -eq $true) {
		Write-Log "Bypassing UnBlock-AppExecution Function [Mode: $deployMode]"
		Return
	}

	Write-Log "Invoking UnBlock-AppExecution Function..."
	$unblockProcessName = Get-ChildItem $regKeyAppExecution -Recurse -ErrorAction SilentlyContinue | ForEach-Object { Get-ItemProperty $_.PSPath } | Where-Object {$_.Debugger -like "*ShowBlockedAppDialog*"}
	Foreach ($unblockProcess in $unblockProcessName) {
		Write-Log "Removing the Image File Execution Options registry keys to unblock execution of [$($unblockProcess.PSChildName)]..."
		$unblockProcess | Remove-ItemProperty -Name Debugger -ErrorAction SilentlyContinue
	}

	# Remove the scheduled task if it exists
	$schTaskBlockedAppsName = "$installName" + "_BlockedApps"
	If (Get-ScheduledTask -ContinueOnError $true | Select TaskName | Where { $_.TaskName -eq "\$schTaskBlockedAppsName" } ) {
		Write-Log "Deleting Scheduled Task [$schTaskBlockedAppsName] ..."
		Execute-Process -FilePath $exeSchTasks -Arguments "/Delete /TN $schTaskBlockedAppsName /F"
	}
}

Function Get-DeferHistory {
<#
.SYNOPSIS
	Gets the history of deferrals from the registry for the current application, if it exists.
.DESCRIPTION
	Gets the history of deferrals from the registry for the current application, if it exists.
.EXAMPLE
	Get-DeferHistory
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Write-Log "Getting deferral history..."
	Get-RegistryKey -Key $regKeyDeferHistory -ContinueOnError $true
}

Function Set-DeferHistory {
<#
.SYNOPSIS
	Sets the history of deferrals in the registry for the current application.
.DESCRIPTION
	Sets the history of deferrals in the registry for the current application.
.EXAMPLE
	Set-DeferHistory
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[string] $deferTimesRemaining = $null,
		[string] $deferDeadline = $null
	)

	If ($deferTimesRemaining -and ($deferTimesRemaining -ge 0)) {
		Write-Log "Setting deferral history...[DeferTimesRemaining = $deferTimes]"
		Set-RegistryKey -Key $regKeyDeferHistory -Name "DeferTimesRemaining" -Value $deferTimesRemaining -ContinueOnError $true
	}
	If ($deferDeadline) {
		Write-Log "Setting deferral history...[DeferDeadline = $deferDeadline]"
		Set-RegistryKey -Key $regKeyDeferHistory -Name "DeferDeadline" -Value $deferDeadline -ContinueOnError $true
	}
}

Function Get-UniversalDate {
<#
.SYNOPSIS
	Returns the date/time for the local culture in a universal sortable date time pattern.
.DESCRIPTION
	Converts the current datetime or a datetime string for the current culture in to a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z
.EXAMPLE
	Get-UniversalDate
	Returns the current date in a universal sortable date time pattern.
.EXAMPLE
	Get-UniversalDate -DateTime "25/08/2013"
	Returns the date for the current culture in a universal sortable date time pattern.
.PARAMETER
	Specify the DateTime in the current culture
.PARAMETER ContinueOnError
	Continue if an error is encountered [Default is false]
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		$DateTime = (Get-Date -Format ($culture).DateTimeFormat.FullDateTimePattern), # Get the current date
		$ContinueOnError = $false
	)
	Try {
		# If a universal sortable date time pattern was provided, remove the Z, otherwise it could get converted to a different time zone.
		If ($dateTime -match "Z$") { $dateTime = $dateTime -replace "Z$","" }
		$dateTime = [DateTime]::Parse($dateTime, $culture)
		# Convert the date in a universal sortable date time pattern based on the current culture
		$universalDateTime = Get-Date $dateTime -Format ($culture).DateTimeFormat.UniversalSortableDateTimePattern -ErrorAction SilentlyContinue
		Return $universalDateTime
	}
	Catch {
		If ($ContinueOnError -eq $false) {
			Throw "The date/time specified [$dateTime] is not specified in a format recognised by the current culture [$culture]"
		}
	}
}

Function Get-RunningProcesses {
<#
.SYNOPSIS
	Gets the processes that are running from a custom list of process objects.
.DESCRIPTION
	Gets the processes that are running from a custom list of process objects.
.EXAMPLE
	Get-RunningProcesses
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		$processObjects
	)

	If ($processObjects -ne $null) {
		Write-Log "Checking for running applications [$(($processObjects | Select ProcessName -ExpandProperty ProcessName) -Join ",")]..."

		# Join the process names with the regex operator '|' to perform "or" match against multiple applications
		$processNames = ($processObjects | Select ProcessName -ExpandProperty ProcessName -ErrorAction SilentlyContinue) -join ("|")

		# Replace escape characters that interfere with Regex and might cause false positive matches
		$processNames = $processNames -replace "\.","dot" -replace "\*","asterix" -replace "\+","plus" -replace "\(","openbracket" -replace "\)","closebracket"
		# Get running processes and replace escape characters. Also, append exe so that we can match exact processes.
		$runningProcesses = Get-Process | Where { ($_.ProcessName -replace "\.","dot" -replace "\*","asterix" -replace "\+","plus" -replace "\(","openbracket" -replace "\)","closebracket" -replace "$","dotexe") -match $processNames }
		$runningProcesses = $runningProcesses | Select Name,Description,ID
        If ($runningProcesses) {
			Write-Log "The following processes are running: [$(($runningProcesses.Name) -Join ",")]"
			Write-Log "Resolving process descriptions..."
			# Resolve the running process names to descriptions in the following precedence:
			# 1. The description of the process provided as a Parameter to the function, e.g. -ProcessName "winword=Microsoft Office Word".
			# 2. The description of the process provided by WMI
			# 3. Fall back on the process name
			Foreach ($runningProcess in $runningProcesses) {
				Foreach ($processObject in $processObjects) {
					If ($runningProcess.Name -eq ($processObject.ProcessName -replace ".exe","")) {
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
		}
		Else {
			Write-Log "Applications are not running."
		}
		Write-Log "Finished checking running applications."
		Return $runningProcesses
	}
}

Function Show-InstallationWelcome {
<#
.SYNOPSIS
	This function provides a welcome dialog prompting the user with information about the installation and actions to be performed before the installation can begin.
.DESCRIPTION
	The following prompts can be included in the welcome dialog:
	Close the specified running applications, or optionally close the applications without showing a prompt (using the -Silent switch).
	Defer the installation a certain number of times, for a certain number of days or until a deadline is reached.
	Countdown until applications are automatically closed.
	Prevent users from launching the specified applications while the installation is in progress.
	Notes:
	The process descriptions are retrieved from WMI, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.
	The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).
.EXAMPLE
	Show-InstallationWelcome -CloseApps "iexplore,winword,excel"
	Prompt the user to close Internet Explorer, Word and Excel.
.EXAMPLE
	Show-InstallationWelcome -CloseApps "winword,excel" -Silent
	Close Word and Excel without prompting the user.
.EXAMPLE
	Show-InstallationWelcome -CloseApps "winword,excel" -BlockExecution
	Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
.EXAMPLE
	Show-InstallationWelcome -CloseApps "winword=Microsoft Office Word,excel=Microsoft Office Excel" -CloseAppsCountdown "600"
	Prompt the user to close Word and Excel, with customized descriptions for the applications and automatically close the applications after 10 minutes.
Show-InstallationWelcome -CloseApps "winword.exe,msaccess.exe,excel.exe" -PersistPrompt
	Prompt the user to close Word, MSAccess and Excel if the processes match the exact name specified (use .exe for exact matches).
	By using the PersistPrompt switch, the dialog will return to the center of the screen every 10 seconds so the user cannot ignore it by dragging it aside.
.EXAMPLE
	Show-InstallationWelcome -AllowDefer -DeferDeadline "25/08/2013"
	Allow the user to defer the installation until the deadline is reached.
.EXAMPLE
	Show-InstallationWelcome -CloseApps "winword,excel" -BlockExecution -AllowDefer -DeferTimes "10" -DeferDeadline "25/08/2013" -CloseAppsCountdown "600"
	Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
	Allow the user to defer the installation a maximum of 10 times or until the deadline is reached, whichever happens first.
	When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes.
.PARAMETER CloseApps
	Name of the process to stop (do not include the .exe). Specify multiple processes separated by a comma. Specify custom descriptions like this: "winword=Microsoft Office Word,excel=Microsoft Office Excel"
.PARAMETER Silent
	Stop processes without prompting the user.
.PARAMETER CloseAppsCountdown
	Option to provide a countdown in seconds until the specified applications are automatically closed. This only takes effect if deferral is not allowed or has expired.
.PARAMETER PersistPrompt
	Specify whether to make the prompt persist in the center of the screen every 10 seconds. The user will have no option but to respond to the prompt - resistance is futile! This only takes effect if deferral is not allowed or has expired.
.PARAMETER BlockExecution
	Option to prevent the user from launching the process/application during the installation
.PARAMETER AllowDefer
	Enables an optional defer button to allow the user to defer the installation.
.PARAMETER AllowDeferCloseApps
	Enables an optional defer button to allow the user to defer the installation only if there are running applications that need to be closed.
.PARAMETER DeferTimes
	Specify the number of times the installation can be deferred
.PARAMETER DeferDays
	Specify the number of days since first run that the installation can be deferred. This is converted to a deadline.
.PARAMETER DeferDeadline
	Specify the deadline date up until which the installation can be deferred.
	Specify the date in the local culture if the script is intended for that same culture, e.g.
	If the script is intended to run on EN-US machines, specify the date in the format "08/25/2013" or "08-25-2013" or "08-25-2013 18:00:00".
	If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. "2013-08-22 11:51:52Z"
	The deadline date will be displayed to the user in the format of their culture.
.PARAMETER CheckDiskSpace
	Specify whether to check if there is enough disk space for the installation to proceed.
	If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.
.PARAMETER RequiredDiskSpace
	Specify required disk space in MB, used in combination with CheckDiskSpace.
.PARAMETER MinimizeWindows
	Specifies whether to minimize other windows when displaying prompt [Default is true]
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param(
	[string] $CloseApps, # Specify process names separated by commas. Optionally specify a process description with an equals symobol, e.g. "winword=Microsoft Office Word"
	[switch] $Silent = $false, # Specify whether to prompt user or force close the applications
	[int] $CloseAppsCountdown = 0, # Specify a countdown to display before automatically closing applications where defferal is not allowed or has expired
	[switch] $PersistPrompt = $false, # Specify whether to make the prompt persist in the center of the screen every 10 seconds.
	[switch] $BlockExecution = $false, # Specify whether to block execution of the processes during installation
	[switch] $AllowDefer = $false, # Specify whether to enable the optional defer button on the dialog box
	[switch] $AllowDeferCloseApps = $false, # Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed
	[int] $DeferTimes = 0, # Specify the number of times the deferral is allowed
	[int] $DeferDays = 0, # Specify the number of days since first run that the deferral is allowed
	[string] $DeferDeadline = $null, # Specify the deadline (in format dd/mm/yyyy) for which deferral will expire as an option
	[switch] $CheckDiskSpace = $false, # Specify whether to check if there is enough disk space for the installation to proceed. If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.
	[int] $RequiredDiskSpace = 0, # Specify required disk space in MB, used in combination with $CheckDiskSpace.
	[boolean] $MinimizeWindows = $true # Specify whether to minimize other windows when displaying prompt
	)

	# If running in NonInteractive mode, force the processes to close silently
	If ($deployModeNonInteractive -eq $true) { $Silent = $true }

	# Check disk space requirements if specified
	If ($CheckDiskSpace -eq $true) {
		Write-Log "Evaluating disk space requirements..."
		$freeDiskSpace = Get-FreeDiskSpace
		If ($RequiredDiskSpace -eq 0) {
			Try {
				# Determine the size of the Files folder
				$fso = New-Object -COM Scripting.FileSystemObject -ErrorAction SilentlyContinue
				$RequiredDiskSpace = [Math]::Round((($fso.GetFolder($scriptParentPath).Size) / 1MB))
			}
			Catch {
				Write-Log "Error calculating required disk space from source files."
			}
		}
		If (($freeDiskSpace) -lt $RequiredDiskSpace) {
			Write-Log "Error: Minimum hard disk space requirement not met. Space Required [$($RequiredDiskSpace)MB], Space Available [$($freeDiskSpace)MB]."
			If ($Silent -eq $false) {
				Show-InstallationPrompt -Message ($configDiskSpaceMessage -f $installTitle,$RequiredDiskSpace,($freeDiskSpace)) -ButtonRightText "Ok" -Icon "Error"
			}
			Exit-Script $configInstallationUIExitCode
		}
		Else {
				Write-Log "Disk space requirements are met."
			}
	}

	If ($CloseApps -ne "") {
		# Create a Process object with custom descriptions where they are provided (split on a "=" sign)
		$processObjects = @()
		Foreach ($process in ($CloseApps -split(",") | Where { $_ -ne ""})) { # Split multiple processes on a comma and join with the regex operator '|' to perform "or" match against multiple applications
			$process = $process -split("=")
			$processObjects += New-Object PSObject -Property @{
				ProcessName = $process[0]
				ProcessDescription = $process[1]
			}
		}
	}

	# Check Deferral history and calculate deferrals remaining
	If ($allowDefer -eq $true -or $AllowDeferCloseApps -eq $true) {
		# Set the allowDefer to true if AllowDeferCloseApps is true
		$allowDefer = $true
		# Get the deferral history from the registry
		$deferHistory = Get-DeferHistory
		$deferHistoryTimes = $deferHistory | Select DeferTimesRemaining -ExpandProperty DeferTimesRemaining -ErrorAction SilentlyContinue
		$deferHistoryDeadline = $deferHistory | Select DeferDeadline -ExpandProperty DeferDeadline -ErrorAction SilentlyContinue
		# Reset Switches
		$checkDeferDays = $checkDeferDeadline = $false
		If ($DeferDays -ne 0) {$checkDeferDays = $true}
		If ($DeferDeadline) {$checkDeferDeadline = $true}
		If ($DeferTimes -ne 0) {
			If ($deferHistoryTimes -ge 0) {
				Write-Log "Defer history shows [$($deferHistory.DeferTimesRemaining)] deferrals remaining."
				$DeferTimes = $deferHistory.DeferTimesRemaining -1
			}
			Else {
				$DeferTimes = $DeferTimes -1
			}
			Write-Log "User now has [$deferTimes] deferrals remaining."
			If ($DeferTimes -lt 0) {
				Write-Log "Deferral has expired."
				$AllowDefer = $false
			}
		}
		Else {
			[string]$DeferTimes = $null
		}
		If ($checkDeferDays -and $allowDefer -eq $true) {
			If ($deferHistoryDeadline) {
				Write-Log "Defer history shows [$deferHistoryDeadline] deadline date."
				$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferHistoryDeadline
			}
			Else {
				$deferDeadlineUniversal = Get-UniversalDate -DateTime (Get-Date ((Get-Date).AddDays($deferDays)) -Format ($culture).DateTimeFormat.FullDateTimePattern)
			}
			Write-Log "User has until [$deferDeadlineUniversal] remaining."
			If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
				Write-Log "Deferral has expired."
				$AllowDefer = $false
			}
		}
		If ($checkDeferDeadline -and $allowDefer -eq $true) {
			# Validate Date
			Try {
				$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferDeadline
			}
			Catch {
				Throw "Date is not in the correct format for the current culture .Type the date in the format of current locale, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z'"
			}
			Write-Log "User has until [$deferDeadlineUniversal] remaining."
			If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
				Write-Log "Deferral has expired."
				$AllowDefer = $false
			}
		}
	}
	If (($deferTimes -lt 0) -and !($deferDeadlineUniversal)) {
		$AllowDefer = $false
	}

	# Prompt the user to close running applications and optionally defer if enabled
	If (!($deployModeSilent) -and !($silent)) {
		Set-Variable -Name closeAppsCountdownGlobal -Value $closeAppsCountdown -Scope Script
		While ((Get-RunningProcesses $processObjects | Select * -OutVariable RunningProcesses) -or ($promptResult -ne "Defer" -and $promptResult -ne "Close")) {
			$runningProcessDescriptions	= ($runningProcesses | Select Description -ExpandProperty Description | Select -Unique | Sort) -join ","
			# Check if we need to prompt the user to defer, to defer and close apps or not to prompt them at all
			If ($allowDefer) {
				# If there is deferral but only for apps to be closed and there are no apps to be closed, break the while loop
				If ($AllowDeferCloseApps -and $runningProcessDescriptions -eq "") {
					Break
				}
				# Otherwise, as long as the user has not selected to close the apps or the processes are still running and the user has not selected to continue, prompt user to close running processes with deferral
				ElseIf ($promptResult -ne "Close" -or ($runningProcessDescriptions -ne "" -and $promptResult -ne "Continue")) {
					$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -PersistPrompt $PersistPrompt -AllowDefer -DeferTimes $deferTimes -DeferDeadline $deferDeadlineUniversal -MinimizeWindows $minimizeWindows
				}
			}
			# If there is no deferral and processes are running, prompt the user to close running processes with no deferral option
			ElseIf ($runningProcessDescriptions -ne "") {
				$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -PersistPrompt $PersistPrompt -MinimizeWindows $minimizeWindows
			}
			# If there is no deferral and no processes running, break the while loop
			Else {
				Break
			}

			# If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again
			If ($promptResult -eq "Continue") {
				Write-Log "User selected to continue..."
				Sleep -Seconds 2
				# Break the while loop if there are no processes to close and the user has clicked ok to continue
				If (!($runningProcesses)) {
					Break
				}
			}
			# Force the applications to close
			ElseIf ($promptResult -eq "Close") {
				Write-Log "User selected to force the applications to close..."
				ForEach ($runningProcess in $runningProcesses) {
					Write-Log "Stopping Process $($runningProcess.Name)..."
					Stop-Process ($runningProcess | Select ID -ExpandProperty ID) -Force -ErrorAction SilentlyContinue
				}
				Sleep -Seconds 2
			}
			# Stop the script (not actioned within a reasonable amount of time)
			ElseIf ($promptResult -eq "Timeout") {
				Write-Log "Installation not actioned within a reasonable amount of time."
				$BlockExecution = $false
				If ($deferTimes -ne "" -or $deferDeadlineUniversal -ne "") {
					Set-DeferHistory -deferTimesRemaining $DeferTimes -deferDeadline $deferDeadlineUniversal
				}
				# Restore minimized windows
				$shellApp.UndoMinimizeAll()
				Exit-Script $configInstallationUIExitCode
			}
			# Stop the script (user chose to defer)
			ElseIf ($promptResult -eq "Defer") {
				Write-Log "Installation deferred by the user."
				$BlockExecution = $false
				Set-DeferHistory -deferTimesRemaining $DeferTimes -deferDeadline $deferDeadlineUniversal
				# Restore minimized windows
				$shellApp.UndoMinimizeAll()
				Exit-Script $configInstallationDeferExitCode
			}
		}
	}

	# Force the processes to close silently, without prompting the user
	If (($Silent -or $deployModeSilent) -and $CloseApps) {
		$runningProcesses = $null
		$runningProcesses = Get-RunningProcesses $processObjects
		If ($runningProcesses -ne $null) {
			$runningProcessDescriptions	= ($runningProcesses | Select Description -ExpandProperty Description | Select -Unique | Sort) -join ","
			Write-Log "Force closing application(s) [$($runningProcessDescriptions)] without prompting user..."
			$runningProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
			Sleep -Seconds 2
		}
	}

	# Force nsd.exe to stop if Notes is one of the required applications to close
	If (($processObjects | Select ProcessName -ExpandProperty ProcessName) -match "notes") {
		$notesPath = Get-Item $regKeyLotusNotes -ErrorAction SilentlyContinue | Get-ItemProperty | Select "Path" -ExpandProperty "Path"
		If ($notesPath -ne $null) {
			$notesNSDExecutable = Join-Path $notesPath "NSD.Exe"
			Try {
				If (Test-Path $notesNSDExecutable) {
					Write-Log "Executing $notesNSDExecutable with the -kill argument..."
					$notesNSDProcess = Start-Process -FilePath $notesNSDExecutable -ArgumentList "-kill" -WindowStyle Hidden -PassThru
					If (!$notesNSDProcess.WaitForExit(10000)) {
						Write-Log "$notesNSDExecutable did not end in a timely manner. Terminating process..."
						Stop-Process -Name "NSD" -Force -ErrorAction SilentlyContinue
					}
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

	# If block execution switch is true, call the function to block execution of these processes
	If ($BlockExecution -eq $true) {
		# Make this variable globally available so we can check whether we need to call Unblock-AppExecution
		Set-Variable -Name BlockExecution -Value $BlockExecution -Scope Script
		Write-Log "Block Execution Parameter specified."
		Block-AppExecution -ProcessName ($processObjects | Select ProcessName -ExpandProperty ProcessName)
	}
}

Function Show-WelcomePrompt {
<#
.SYNOPSIS
	This function is called by Show-InstallationWelcome to prompts the user to optionally do the following:
	Close the specified running applications.
	Provide an option to defer the installation.
	Show a countdown before applications are automatically closed.
.DESCRIPTION
	The user is presented with a Windows Forms dialog box to close the applications themselves and continue or to have the script close the applications for them.
	If the -AllowDefer option is set to true, an optional "Defer" button will be shown to the user. If they select this option, the script will exit and return a 1618 code (SCCM fast retry code)
	The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code)
.EXAMPLE
	Show-WelcomePrompt -ProcessDescriptions "Lotus Notes, Microsoft Word" -CloseAppsCountdown "600" -AllowDefer -DeferTimes 10
.PARAMETER ProcessDescriptions
	The descriptive names of the applications that are running and need to be closed.
.PARAMETER CloseAppsCountdown
	Specify the countdown time in seconds before running applications are automatically closed.
.PARAMETER PersistPrompt
	Specify whether to make the prompt persist in the center of the screen every 10 seconds
.PARAMETER AllowDefer
	Specify whether to provide an option to defer the installation
.PARAMETER DeferTimes
	Specify the number of times the user is allowed to defer
.PARAMETER DeferDeadline
	Specify the deadline date before the user is allowed to defer
.PARAMETER MinimizeWindows
	Specifies whether to minimize other windows when displaying prompt [Default is true]
.NOTES
	This is an internal script function and should typically not be called directly. It is used by the Show-InstallationWelcome prompt to display a custom prompt.
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[string] $ProcessDescriptions = $null,
		[int] $CloseAppsCountdown = $null,
		[boolean] $PersistPrompt = $false,
		[switch] $AllowDefer = $false,
		$DeferTimes = $null,
		$DeferDeadline = $null,
		[boolean]$minimizeWindows = $true
	)
	# Reset switches
	$showCloseApps = $showDefer = $persistWindow = $false
	# Reset times
	$startTime = $countdownTime = Get-Date
 
 	# Check if the countdown was specified
	If ($CloseAppsCountdown) {
		If ($CloseAppsCountdown -gt $configInstallationUITimeout) {
			Throw "Error: The close applications countdown time can not be longer than the timeout specified in the XML configuration for installation UI dialogs to timeout."
		}
	}

	# Initial form layout: Close Applications / Allow Deferral
	If ($ProcessDescriptions -ne "") {
		Write-Log "Prompting user to close application(s) [$runningProcessDescriptions]..."
		$showCloseApps = $true
	}
	If ($AllowDefer -eq $true -and ($DeferTimes -ge 0 -or $DeferDeadline)) {
		Write-Log "User has the option to defer."
		$showDefer = $true
		If ($deferDeadline) {
			# Remove the Z from universal sortable date time format, otherwise it could be converted to a different time zone
			$deferDeadline = $deferDeadline -replace "Z",""
			# Convert the deadline date to a string
			[string]$deferDeadline = Get-Date $deferDeadline | Out-String -Stream
		}
	}

	# If deferral is not being showed and close apps countdown or persist prompt was specified, enable those features.
	If ($showDefer -ne $true) {
		If ($CloseAppsCountdown -gt 0) {
			Write-Log "Displaying close applications countdown with [$CloseAppsCountdown] seconds."
			$showCountdown = $true
		}
		If ($PersistPrompt) {
			$persistWindow = $true
		}
	}

	[Array]$ProcessDescriptions = $ProcessDescriptions.split(",")
	[System.Windows.Forms.Application]::EnableVisualStyles()

	$formWelcome = New-Object System.Windows.Forms.Form
	$pictureBanner = New-Object System.Windows.Forms.PictureBox
	$labelAppName = New-Object System.Windows.Forms.Label
	$labelCountdown = New-Object System.Windows.Forms.Label
	$labelDefer = New-Object System.Windows.Forms.Label
	$listBoxCloseApps = New-Object System.Windows.Forms.ListBox
	$buttonContinue = New-Object System.Windows.Forms.Button
	$buttonDefer = New-Object System.Windows.Forms.Button
	$buttonCloseApps = New-Object System.Windows.Forms.Button
	$buttonAbort = New-Object System.Windows.Forms.Button
	$formWelcomeWindowState = New-Object System.Windows.Forms.FormWindowState
	$flowLayoutPanel = New-Object System.Windows.Forms.FlowLayoutPanel
	$panelButtons = New-Object System.Windows.Forms.Panel

	$Form_Cleanup_FormClosed = {
		# Remove all event handlers from the controls
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
		Catch [Exception]
		{ }
	}

	$Form_StateCorrection_Load = {
		# Correct the initial state of the form to prevent the .Net maximized form issue
		$formWelcome.WindowState = 'Normal'
		$formWelcome.AutoSize = $true
		$formWelcome.TopMost = $true
		$formWelcome.BringToFront()
		# Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
		Set-Variable -Name formWelcomeStartPosition -Value $($formWelcome.Location) -Scope Script

		# Initialize the countdown timer
		$currentTime = Get-Date
		$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
		$script:welcomeTimer.Start()

		# Set up the form
		$remainingTime = $countdownTime.Subtract($currentTime)
		$labelCountdownSeconds = [String]::Format("{0}:{1:d2}:{2:d2}", $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
		$labelCountdown.Text = "$configClosePromptCountdownMessage`n$labelCountdownSeconds"
	}

	# Add the timer if it doesn't already exist - this avoids the timer being reset if the continue button is clicked
	If (!($script:welcomeTimer)) {
		$script:welcomeTimer = New-Object 'System.Windows.Forms.Timer'
	}

	If ($showCountdown -eq $true) {
		$timer_Tick = {
			# Get the time information
			$currentTime = Get-Date
			$countdownTime = $startTime.AddSeconds($CloseAppsCountdown )
			$remainingTime = $countdownTime.Subtract($currentTime)
			Set-Variable -Name closeAppsCountdownGlobal -Value ($remainingTime.TotalSeconds) -Scope Script
			# If the countdown is complete, close the Applicationss
			If ($countdownTime -lt $currentTime) {
				Write-Log "Close Applications countdown timer has elapsed. Force closing applications..."
				$buttonCloseApps.PerformClick()
			}
			Else {
				# Update the form
				$labelCountdownSeconds = [String]::Format("{0}:{1:d2}:{2:d2}", $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
				$labelCountdown.Text = "$configClosePromptCountdownMessage`n$labelCountdownSeconds"
				[System.Windows.Forms.Application]::DoEvents()
			}
		}
	}
	Else {
		$script:welcomeTimer.Interval = ($configInstallationUITimeout * 1000)
		$timer_Tick = {
			$buttonAbort.PerformClick()
		}
	}

	$script:welcomeTimer.add_Tick($timer_Tick)

	# Persistence Timer
	If ($persistWindow) {
		$timerPersist = New-Object 'System.Windows.Forms.Timer'
		$timerPersist.Interval = ($configInstallationPersistInterval * 1000)
		$timerPersist_Tick = {
			Refresh-InstallationWelcome
		}
		$timerPersist.add_Tick($timerPersist_Tick)
		$timerPersist.Start()
	}

	# Form
	$formWelcome.Controls.Add($pictureBanner)
	$formWelcome.Controls.Add($buttonAbort)

	#----------------------------------------------
	# Create padding object
	$paddingNone = New-Object System.Windows.Forms.Padding
	$paddingNone.Top = 0
	$paddingNone.Bottom = 0
	$paddingNone.Left = 0
	$paddingNone.Right = 0

	# Generic Label properties
	$labelPadding = "20,0,20,0"

	# Generic Button properties
	$buttonWidth = 110
	$buttonHeight = 23
	$buttonPadding = 50
	$buttonSize = New-Object System.Drawing.Size
	$buttonSize.Width = $buttonWidth
	$buttonSize.Height = $buttonHeight
	$buttonPadding = New-Object System.Windows.Forms.Padding
	$buttonPadding.Top = 0
	$buttonPadding.Bottom = 5
	$buttonPadding.Left = 50
	$buttonPadding.Right = 0

	# Picture Banner
	$pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
	$pictureBanner.ImageLocation = $appDeployLogoBanner
	$System_Drawing_Point = New-Object System.Drawing.Point
	$System_Drawing_Point.X = 0
	$System_Drawing_Point.Y = 0
	$pictureBanner.Location = $System_Drawing_Point
	$pictureBanner.Name = "pictureBanner"
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 50
	$System_Drawing_Size.Width = 450
	$pictureBanner.Size = $System_Drawing_Size
	$pictureBanner.Margin = $paddingNone
	$pictureBanner.TabIndex = 0
	$pictureBanner.TabStop = $False

	# Label App Name
	$labelAppName.DataBindings.DefaultDataSourceUpdateMode = 0
	$labelAppName.Name = "labelAppName"
	$System_Drawing_Size = New-Object System.Drawing.Size
	If ($showCloseApps -ne $true) {
		$System_Drawing_Size.Height = 40
	}
	Else {
		$System_Drawing_Size.Height = 65
	}
	$System_Drawing_Size.Width = 450
	$labelAppName.Size = $System_Drawing_Size
	$labelAppName.Margin = "0,15,0,15"
	$labelAppName.Padding = $labelPadding
	$labelAppName.TabIndex = 1

	 # Initial form layout: Close Applications / Allow Deferral
	If ($showCloseApps -eq $true) {
		$labelAppNameText = "$configClosePromptMessage"
	}
	ElseIf ($showDefer -eq $true) {
		$labelAppNameText = "$configDeferPromptWelcomeMessage `n$installTitle"
	}

	$labelAppName.Text = $labelAppNameText
	$labelAppName.TextAlign = 'TopCenter'
	$labelAppName.Anchor = "Top"
	$labelAppName.AutoSize = $false
	$labelAppName.add_Click($handler_labelAppName_Click)

	# Listbox Close Applications
	$listBoxCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
	$listBoxCloseApps.FormattingEnabled = $True
	$listBoxCloseApps.Name = "listBoxCloseApps"
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 100
	$System_Drawing_Size.Width = 300
	$listBoxCloseApps.Size = $System_Drawing_Size
	$listBoxCloseApps.Margin = "75,0,0,0"
	$listBoxCloseApps.TabIndex = 3
	Foreach ($processDescription in $ProcessDescriptions) {
		# Assign the return values to a variable to suppress them from being returned from the function, which can cause issues
		$listboxCloseAppsDescriptions = $listboxCloseApps.Items.Add("$processDescription")
	}

	# Label Defer
	$labelDefer.DataBindings.DefaultDataSourceUpdateMode = 0
	$labelDefer.Name = "labelDefer"
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 90
	$System_Drawing_Size.Width = 450
	$labelDefer.Size = $System_Drawing_Size
	$labelDefer.Margin = $paddingNone
	$labelDefer.Padding = $labelPadding
	$labelDefer.TabIndex = 4
	$deferralText = "$configDeferPromptExpiryMessage`n"
	If ($deferTimes -ge 0) {
		$deferralText = "$deferralText `n$configDeferPromptRemainingDeferrals $($deferTimes + 1)"
	}
	If ($DeferDeadline) {
		$deferralText = "$deferralText `n$configDeferPromptDeadline $deferDeadline"
	}
	If ($DeferTimes -lt 0 -and !($DeferDeadline)) {
		$deferralText = "$deferralText `n$configDeferPromptNoDeadline"
	}
	$deferralText = "$deferralText `n`n$configDeferPromptWarningMessage"
	$labelDefer.Text = $deferralText
	$labelDefer.TextAlign = 'MiddleCenter'
	$labelDefer.add_Click($handler_labelDefer_Click)

	# Label Countdown
	$labelCountdown.DataBindings.DefaultDataSourceUpdateMode = 0
	$labelCountdown.Name = "labelCountdown"
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 40
	$System_Drawing_Size.Width = 450
	$labelCountdown.Size = $System_Drawing_Size
	$labelCountdown.Margin = $paddingNone
	$labelCountdown.Padding = $labelPadding
	$labelCountdown.TabIndex = 4
	$labelCountdown.Font = "Microsoft Sans Serif, 9pt, style=Bold"
	$labelCountdown.Text = "00:00:00"
	$labelCountdown.TextAlign = 'MiddleCenter'
	$labelCountdown.add_Click($handler_labelDefer_Click)

	# Panel Flow Layout
	$System_Drawing_Point = New-Object System.Drawing.Point
	$System_Drawing_Point.X = 0
	$System_Drawing_Point.Y = 50
	$flowLayoutPanel.Location = $System_Drawing_Point
	$flowLayoutPanel.AutoSize = $True
	$flowLayoutPanel.Anchor = "Top"
	$flowLayoutPanel.FlowDirection = 'TopDown'
	$flowLayoutPanel.WrapContents = $true
	$flowLayoutPanel.Controls.Add($labelAppName)
	If ($showCloseApps -eq $true) {
		$flowLayoutPanel.Controls.Add($listBoxCloseApps)
	}
	If ($showDefer -eq $true) {
		$flowLayoutPanel.Controls.Add($labelDefer)
	}
	ElseIf ($showCountdown -eq $true) {
		$flowLayoutPanel.Controls.Add($labelCountdown)
	}

	# Button Close For Me
	$buttonCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
	$buttonCloseApps.Location = "15,0"
	$buttonCloseApps.Name = "buttonCloseApps"
	$buttonCloseApps.Size = $buttonSize
	$buttonCloseApps.TabIndex = 5
	$buttonCloseApps.Text = $configClosePromptButtonClose
	$buttonCloseApps.DialogResult = 'Yes'
	$buttonCloseApps.AutoSize = $true
	$buttonCloseApps.UseVisualStyleBackColor = $True
	$buttonCloseApps.add_Click($buttonCloseApps_OnClick)

	# Button Defer
	$buttonDefer.DataBindings.DefaultDataSourceUpdateMode = 0
	If ($showCloseApps -ne $true) {
		$buttonDefer.Location = "15,0"
	}
	Else {
		$buttonDefer.Location = "170,0"
	}
	$buttonDefer.Name = "buttonDefer"
	$buttonDefer.Size = $buttonSize
	$buttonDefer.TabIndex = 6
	$buttonDefer.Text = $configClosePromptButtonDefer
	$buttonDefer.DialogResult = 'No'
	$buttonDefer.AutoSize = $true
	$buttonDefer.UseVisualStyleBackColor = $True
	$buttonDefer.add_Click($buttonDefer_OnClick)

	# Button Continue
	$buttonContinue.DataBindings.DefaultDataSourceUpdateMode = 0
	$buttonContinue.Location = "325,0"
	$buttonContinue.Name = "buttonContinue"
	$buttonContinue.Size = $buttonSize
	$buttonContinue.TabIndex = 7
	$buttonContinue.Text = $configClosePromptButtonContinue
	$buttonContinue.DialogResult = 'OK'
	$buttonContinue.AutoSize = $true
	$buttonContinue.UseVisualStyleBackColor = $True
	$buttonContinue.add_Click($buttonContinue_OnClick)

	# Button Abort (Hidden)
	$buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
	$buttonAbort.Name = "buttonAbort"
	$buttonAbort.Size = "1,1"
	$buttonAbort.DialogResult = 'Abort'
	$buttonAbort.TabIndex = 5
	$buttonAbort.UseVisualStyleBackColor = $True
	$buttonAbort.add_Click($buttonAbort_OnClick)

	# Form Welcome
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 0
	$System_Drawing_Size.Width = 0
	$formWelcome.Size = $System_Drawing_Size
	$formWelcome.Padding = $paddingNone
	$formWelcome.Margin = $paddingNone
	$formWelcome.DataBindings.DefaultDataSourceUpdateMode = 0
	$formWelcome.Name = "WelcomeForm"
	$formWelcome.Text = $installTitle
	$formWelcome.StartPosition = 'CenterScreen'
	$formWelcome.FormBorderStyle = 'FixedDialog'
	$formWelcome.MaximizeBox = $False
	$formWelcome.MinimizeBox = $False
	$formWelcome.TopMost = $True
	$formWelcome.TopLevel = $True
	$formWelcome.Icon = New-Object System.Drawing.Icon ($AppDeployLogoIcon)
	$formWelcome.AutoSize = $true
	$formWelcome.Controls.Add($pictureBanner)
	$formWelcome.Controls.Add($flowLayoutPanel)

	# Panel Button
	$System_Drawing_Point = New-Object System.Drawing.Point
	$System_Drawing_Point.X = 0
	# Calculate the position of the panel relative to the size of the form
	$System_Drawing_Point.Y = (($formWelcome.Size | Select Height -ExpandProperty Height) -10)
	$panelButtons.Location = $System_Drawing_Point
	$System_Drawing_Size = New-Object System.Drawing.Size
	$System_Drawing_Size.Height = 40
	$System_Drawing_Size.Width = 450
	$panelButtons.Size = $System_Drawing_Size
	$panelButtons.AutoSize = $True
	$panelButtons.Anchor = "Top"
	$padding = New-Object System.Windows.Forms.Padding
	$padding.Top = 0
	$padding.Bottom = 0
	$padding.Left = 0
	$padding.Right = 0
	$panelButtons.Margin = $padding
	If ($showCloseApps -eq $true) {
		$panelButtons.Controls.Add($buttonCloseApps)
	}
	If ($showDefer -eq $true) {
		$panelButtons.Controls.Add($buttonDefer)
	}
	$panelButtons.Controls.Add($buttonContinue)

	# Add the Buttons Panel to the form
	$formWelcome.Controls.Add($panelButtons)

	# Save the initial state of the form
	$formWelcomeWindowState = $formWelcome.WindowState
	# Init the OnLoad event to correct the initial state of the form
	$formWelcome.add_Load($Form_StateCorrection_Load)
	# Clean up the control events
	$formWelcome.add_FormClosed($Form_Cleanup_FormClosed)

	Function Refresh-InstallationWelcome {
		$formWelcome.BringToFront()
		$formWelcome.Location = "$($formWelcomeStartPosition.X),$($formWelcomeStartPosition.Y)"
		$formWelcome.Refresh()
	}

	If ($minimizeWindows -eq $true) {
		# Minimize all other windows
		$shellApp.MinimizeAll()
	}

	# Show the form
	$result = $formWelcome.ShowDialog()

	Switch ($result) {
		OK { $result = "Continue" }
		No { $result = "Defer" }
		Yes { $result = "Close" }
		Abort { $result = "Timeout" }
	}

	Return $result

} # End Function

Function Show-InstallationRestartPrompt {
<#
.SYNOPSIS
	Displays a restart prompt with a countdown to a forced restart.
.DESCRIPTION
	Displays a restart prompt with a countdown to a forced restart.
.EXAMPLE
	Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60
.EXAMPLE
	Show-InstallationRestartPrompt -NoCountdown
.PARAMETER CountdownSeconds
	Specifies the number of seconds to countdown to the system restart.
.PARAMETER CountdownNoHideSeconds
	Specifies the number of seconds to display the restart prompt without allowing the window to be hidden.
.PARAMETER NoCountdown
	Specifies not to show a countdown, just the Restart Now and Restart Later buttons. 
	The UI will restore/reposition itself persistently based on the interval value specified in the config file.
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[int] $CountdownSeconds = 60,
		[int] $CountdownNoHideSeconds = 30,
		[switch] $NoCountdown = $false
	)

	# Bypass if in non-interactive mode
	If ($deployModeNonInteractive -eq $true) {
		Write-Log "Bypassing Installation Restart Prompt [Mode: $deployMode]..."
		Return
	}

	# Get the parameters passed to the function for invoking the function asynchronously
	$installRestartPromptParameters = $psBoundParameters

	# Check if we are already displaying a restart prompt
	If (Get-Process | Where { $_.MainWindowTitle -match $configRestartPromptTitle }) {
		Write-Log "Show-InstallationRestartPrompt invoked, but an existing restart prompt was detected. Cancelling restart prompt..."
		Return
	}

	$startTime = $countdownTime = Get-Date

	[System.Windows.Forms.Application]::EnableVisualStyles()
	$formRestart = New-Object 'System.Windows.Forms.Form'
	$labelCountdown = New-Object 'System.Windows.Forms.Label'
	$labelTimeRemaining = New-Object 'System.Windows.Forms.Label'
	$labelMessage = New-Object 'System.Windows.Forms.Label'
	$buttonRestartLater = New-Object 'System.Windows.Forms.Button'
	$picturebox = New-Object 'System.Windows.Forms.PictureBox'
	$buttonRestartNow = New-Object 'System.Windows.Forms.Button'
	$timerCountdown = New-Object 'System.Windows.Forms.Timer'
	$InitialFormWindowState = New-Object 'System.Windows.Forms.FormWindowState'

	Function Perform-Restart {
		Write-Log "Force restarting computer..."
		Restart-Computer -Force
	}

	$FormEvent_Load={
		# Initialize the countdown timer
		$currentTime = Get-Date
		$countdownTime = $startTime.AddSeconds($countdownSeconds)
		$timerCountdown.Start()
		# Set up the form
		$remainingTime = $countdownTime.Subtract($currentTime)
		$labelCountdown.Text = [String]::Format("{0}:{1:d2}:{2:d2}", $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
		If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) {
			$buttonRestartLater.Enabled = $false
		}
		$formRestart.WindowState = 'Normal'
		$formRestart.TopMost = $true
		$formRestart.BringToFront()
	}

	$Form_StateCorrection_Load=
	{
		# Correct the initial state of the form to prevent the .Net maximized form issue
		$formRestart.WindowState = $InitialFormWindowState
 		$formRestart.AutoSize = $true
		$formRestart.TopMost = $true
		$formRestart.BringToFront()
		# Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
		Set-Variable -Name formInstallationRestartPromptStartPosition -Value $($formRestart.Location) -Scope Script
  	}

	# Persistence Timer
	If ($NoCountdown) {
		$timerPersist = New-Object 'System.Windows.Forms.Timer'
		$timerPersist.Interval = ($configInstallationRestartPersistInterval * 1000)
		$timerPersist_Tick = {
			# Show the Restart Popup
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

	$buttonRestartLater_Click={
		# Minimize the form
		$formRestart.WindowState = 'Minimized'
		# Reset the persistence timer
		$timerPersist.Stop()
		$timerPersist.Start()
	}

	$buttonRestartNow_Click={
		# Restart the computer
		Perform-Restart
	}

	$formRestart_Resize={
		# Hide the form if minimized
		If ($formRestart.WindowState -eq 'Minimized') {
			$formRestart.WindowState = 'Minimized'
		}
	}

	$timerCountdown_Tick={
		# Get the time information
		$currentTime = Get-Date
		$countdownTime = $startTime.AddSeconds($countdownSeconds)
		$remainingTime = $countdownTime.Subtract($currentTime)
		# If the countdown is complete, restart the machine
		If ($countdownTime -lt $currentTime) {
			$buttonRestartNow.PerformClick()
		}
		Else {
			# Update the form
			$labelCountdown.Text = [String]::Format("{0}:{1:d2}:{2:d2}", $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
			If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) {
				$buttonRestartLater.Enabled = $false
				# If the form is hidden when we hit the No Hide, bring it back up
				If ($formRestart.WindowState -eq 'Minimized') {
					# Show Popup
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

	$Form_Cleanup_FormClosed=
	{
		# Remove all event handlers from the controls
		Try
		{
			$buttonRestartLater.remove_Click($buttonRestartLater_Click)
			$buttonRestartNow.remove_Click($buttonRestartNow_Click)
			$formRestart.remove_Load($FormEvent_Load)
			$formRestart.remove_Resize($formRestart_Resize)
			$timerCountdown.remove_Tick($timerCountdown_Tick)
			$timerPersist.remove_Tick($timerPersist_Tick)
			$formRestart.remove_Load($Form_StateCorrection_Load)
			$formRestart.remove_FormClosed($Form_Cleanup_FormClosed)
		}
		Catch [Exception]
		{ }
	}

	# Form
	If ($NoCountdown -eq $false) {
		$formRestart.Controls.Add($labelCountdown)
		$formRestart.Controls.Add($labelTimeRemaining)
	}
	$formRestart.Controls.Add($labelMessage)
	$formRestart.Controls.Add($buttonRestartLater)
	$formRestart.Controls.Add($picturebox)
	$formRestart.Controls.Add($buttonRestartNow)
	$formRestart.ClientSize = '450, 260'
	$formRestart.ControlBox = $False
	$formRestart.FormBorderStyle = 'FixedDialog'
	$formRestart.Icon = New-Object System.Drawing.Icon ($AppDeployLogoIcon)
	$formRestart.MaximizeBox = $False
	$formRestart.MinimizeBox = $False
	$formRestart.Name = "formRestart"
	$formRestart.StartPosition = 'CenterScreen'
	$formRestart.Text = "$configRestartPromptTitle" + ": " + "$installTitle"
	$formRestart.add_Load($FormEvent_Load)
	$formRestart.add_Resize($formRestart_Resize)

	# Banner
	$picturebox.Anchor = 'Top'
	$picturebox.Image = [System.Drawing.Image]::Fromfile($AppDeployLogoBanner)
	$picturebox.Location = '0,0'
	$picturebox.Name = "picturebox"
	$picturebox.Size = '450, 50'
	$picturebox.SizeMode = 'AutoSize'
	$picturebox.TabIndex = 1
	$picturebox.TabStop = $False

	# Label Message
	$labelMessage.Location = '20, 58'
	$labelMessage.Name = "labelMessage"
	$labelMessage.Size = '400, 79'
	$labelMessage.TabIndex = 3
	$labelMessage.Text = "$configRestartPromptMessage $configRestartPromptMessageTime `n`n$configRestartPromptMessageRestart"
	If ($NoCountdown) {
		$labelMessage.Text = $configRestartPromptMessage
	}
	$labelMessage.TextAlign = 'MiddleCenter'

	# Label Time Remaining
	$labelTimeRemaining.Location = '20, 138'
	$labelTimeRemaining.Name = "labelTimeRemaining"
	$labelTimeRemaining.Size = '400, 23'
	$labelTimeRemaining.TabIndex = 4
	$labelTimeRemaining.Text = $configRestartPromptTimeRemaining
	$labelTimeRemaining.TextAlign = 'MiddleCenter'

	# Label Countdown
	$labelCountdown.Font = "Microsoft Sans Serif, 18pt, style=Bold"
	$labelCountdown.Location = '20, 165'
	$labelCountdown.Name = "labelCountdown"
	$labelCountdown.Size = '400, 30'
	$labelCountdown.TabIndex = 5
	$labelCountdown.Text = "00:00:00"
	$labelCountdown.TextAlign = 'MiddleCenter'

	# Label Restart Later
	$buttonRestartLater.Anchor = 'Bottom, Left'
	$buttonRestartLater.Location = '20, 216'
	$buttonRestartLater.Name = "buttonRestartLater"
	$buttonRestartLater.Size = '159, 23'
	$buttonRestartLater.TabIndex = 2
	$buttonRestartLater.Text = $configRestartPromptButtonRestartLater
	$buttonRestartLater.UseVisualStyleBackColor = $True
	$buttonRestartLater.add_Click($buttonRestartLater_Click)

	# Label Restart Now
	$buttonRestartNow.Anchor = 'Bottom, Right'
	$buttonRestartNow.Location = '265, 216'
	$buttonRestartNow.Name = "buttonRestartNow"
	$buttonRestartNow.Size = '159, 23'
	$buttonRestartNow.TabIndex = 0
	$buttonRestartNow.Text = $configRestartPromptButtonRestartNow
	$buttonRestartNow.UseVisualStyleBackColor = $True
	$buttonRestartNow.add_Click($buttonRestartNow_Click)

	# Timer Countdown
	If ($NoCountdown -eq $false) {
		$timerCountdown.add_Tick($timerCountdown_Tick)
	}

	#----------------------------------------------

	# Save the initial state of the form
	$InitialFormWindowState = $formRestart.WindowState
	# Init the OnLoad event to correct the initial state of the form
	$formRestart.add_Load($Form_StateCorrection_Load)
	# Clean up the control events
	$formRestart.add_FormClosed($Form_Cleanup_FormClosed)
	$formRestartClosing =[System.Windows.Forms.FormClosingEventHandler]{
		$_.Cancel = $true
	}
	$formRestart.add_FormClosing($formRestartClosing)

	# If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously
	If ($deployAppScriptFriendlyName) {
		If ($NoCountdown -eq $true) {
			Write-Log "Invoking Show-InstallationRestartPrompt asynchronously with no countdown..."
		}
		Else {
			Write-Log "Invoking Show-InstallationRestartPrompt asynchronously with [$countDownSeconds] countdown seconds..."
		}$installRestartPromptParameters = ($installRestartPromptParameters.GetEnumerator() | % { "-$($_.Key) `"$($_.Value)`""}) -join " "
		Start-Process $PSHOME\powershell.exe -ArgumentList "-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File `"$scriptPath`" -ReferringApplication `"$installName`" -ShowInstallationRestartPrompt $installRestartPromptParameters" -WindowStyle Hidden -ErrorAction SilentlyContinue
	}
	Else {
		If ($NoCountdown -eq $true) {
			Write-Log "Displaying restart prompt with no countdown."
		}
		Else {
			Write-Log "Displaying restart prompt with [$countDownSeconds] countdown seconds."
		}

		# Show the Form
		Return $formRestart.ShowDialog()

		# Activate the Window
		$powershellProcess = Get-Process | Where { $_.MainWindowTitle -match $installTitle }
		[Microsoft.VisualBasic.Interaction]::AppActivate($powershellProcess.ID)
	}

} #End Function

# Function to display a balloon tip notification
Function Show-BalloonTip {
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

	# Skip balloon if in silent mode
	If ($deployModeSilent -eq $true -or $configShowBalloonNotifications -eq $false) {
		Return
	}

	# Dispose of any previous balloon tip notifications
	If ($notifyIcon -ne $null) {
		Try {
			$NotifyIcon.Dispose()
		}
		Catch {}
	}

	[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
	$NotifyIcon = New-Object Windows.Forms.NotifyIcon -Property @{
		BalloonTipIcon = $BalloonTipIcon
		BalloonTipText = $BalloonTipText
		BalloonTipTitle = $BalloonTipTitle
		Icon = New-Object System.Drawing.Icon ($AppDeployLogoIcon)
		Text = -join $BalloonTipText[0..62]
		Visible = $true
	}

	Set-Variable -Name NotifyIcon -Value $NotifyIcon -Scope Global
	$NotifyIcon.ShowBalloonTip($BalloonTipTime)

	Switch ($Host.Runspace.ApartmentState) {
		STA {
			# Register a click event with action to take based on event for balloon message clicked
			Register-ObjectEvent $NotifyIcon -EventName BalloonTipClicked -Action {$sender.Visible = $False; $NotifyIcon.Dispose(); Unregister-Event $EventSubscriber.SourceIdentifier; Remove-Job $EventSubscriber.Action; $sender.Dispose();} | Out-Null
			# Register a click event with action to take based on event for balloon message closed
			Register-ObjectEvent $NotifyIcon -EventName BalloonTipClosed -Action {$sender.Visible = $False; $NotifyIcon.Dispose(); Unregister-Event $EventSubscriber.SourceIdentifier; Remove-Job $EventSubscriber.Action; $sender.Dispose()} | Out-Null
			}
		Default {
			Continue
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
	The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).
.EXAMPLE
	Show-InstallationProgress
	Uses the default status message from the XML configuration file.
.EXAMPLE
	Show-InstallationProgress "Installation in Progress..."
.EXAMPLE
	Show-InstallationProgress "Installation in Progress...`nThe installation may take 20 minutes to complete."
.EXAMPLE
	Show-InstallationProgress "Installation in Progress..." -WindowLocation "BottomRight" -TopMost $false
.PARAMETER StatusMessage
	The Status Message to be displayed. The default status message is taken from the XML configuration file.
.PARAMETER WindowLocation
	The location of the progress window [default is just below top, centered]
.PARAMETER TopMost
	Specificies whether the progress window should be topmost [default is true]
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[string] $StatusMessage = $configProgressMessageInstall,
		[ValidateSet("Default","BottomRight")]
		[string] $WindowLocation = "Default",
		[boolean] $TopMost = $true
	)
	If ($deployModeSilent -eq $true) {
		Return
	}

	# If the default progress message hasn't been overriden and the deployment type is uninstall, use the default uninstallation message
	If ($StatusMessage -eq $configProgressMessageInstall -and $deploymentType -eq "Uninstall") {
		$StatusMessage = $configProgressMessageUninstall
	}

	If ($envhost.Name -match "PowerGUI") {
		Write-Log "Warning: $($envhost.Name) is not a supported host for WPF multithreading. Progress dialog with message [$statusMessage] will not be displayed."
		Return
	}
	# Check if the progress thread is running before invoking methods on it
	If ($Global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -ne "Running") {
		# Notify user that the software installation has started
		$balloonText = "$deploymentTypeName $configBalloonTextStart"
		Show-BalloonTip -BalloonTipIcon "Info" -BalloonTipText "$balloonText"
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
		$Global:ProgressRunspace.SessionStateProxy.SetVariable("windowLocation",$windowLocation)
		$Global:ProgressRunspace.SessionStateProxy.SetVariable("topMost",[string]$topMost)
		$Global:ProgressRunspace.SessionStateProxy.SetVariable("appDeployLogoBanner",$appDeployLogoBanner)
		$Global:ProgressRunspace.SessionStateProxy.SetVariable("progressStatusMessage",$statusMessage)
		$Global:ProgressRunspace.SessionStateProxy.SetVariable("AppDeployLogoIcon",$AppDeployLogoIcon)

		# Add the script block to be execution in the progress runspace
		$progressCmd = [PowerShell]::Create().AddScript({

			[xml]$xamlProgress = @'
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

			## Set the configurable values based using variables addded to the runspace from the parent thread
			# Calculate the position on the screen to place the progress dialog
			$screen = [System.Windows.Forms.Screen]::PrimaryScreen
			$screenWorkingArea = $screen.WorkingArea
			$screenWidth = $screenWorkingArea | Select Width -ExpandProperty Width
			$screenHeight = $screenWorkingArea | Select Height -ExpandProperty Height
			# Set the start position of the Window based on the screen size
			If ($windowLocation -eq "BottomRight"){
				$xamlProgress.Window.Left = [string]($screenWidth - $xamlProgress.Window.Width - 10)
				$xamlProgress.Window.Top = [string]($screenHeight - $xamlProgress.Window.Height - 10)
			}
			# Show the default location (Top center)
			Else {
				$xamlProgress.Window.Left = [string](($screenWidth / 2) - ($xamlProgress.Window.Width /2))
				$xamlProgress.Window.Top = [string]($screenHeight / 9.5)
			}
			$xamlProgress.Window.TopMost = $topMost
			$xamlProgress.Window.Icon = $AppDeployLogoIcon
			$xamlProgress.Window.Grid.Image.Source = $appDeployLogoBanner
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
	# Check if the progress thread is running before invoking methods on it
	ElseIf ($Global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -eq "Running") {
		# Allow time between updating the thread
		Sleep -Seconds 1
		Write-Log "Updating Progress Message: [$statusMessage]"
		# Update the progress text
		Try {
			$Global:ProgressSyncHash.Window.Dispatcher.Invoke([System.Windows.Threading.DispatcherPriority]"Normal",[Windows.Input.InputEventHandler]{$Global:ProgressSyncHash.ProgressText.Text = $statusMessage},$null,$null)
		}
		Catch {
			$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)"
			Write-Log "Warning: $exceptionMessage"
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
	[Parameter(Mandatory = $true)][string] $Action,
	[Parameter(Mandatory = $true)][string] $FilePath
	)
	Write-Log "Set-Pinned Application function called with Parameters: [$Action] [$FilePath]"

	If(!(Test-Path $FilePath)) {
		Write-Log "Warning: Path [$filePath] does not exist. Action [$action] will not be performed."
		Return
	}

	Function Invoke-Verb {
		Param([string]$FilePath,$verb)
		Try {
		$verb = $verb.Replace("&","")
		$path = Split-Path $FilePath -ErrorAction SilentlyContinue
		$folder = $shellApp.Namespace($path)
		$item = $folder.Parsename((Split-Path $FilePath -leaf -ErrorAction SilentlyContinue))
		$itemVerb = $item.Verbs() | ? {$_.Name.Replace("&","") -eq $verb} -ErrorAction SilentlyContinue
		 If (($itemVerb | Select Name -ExpandProperty Name) -eq "") {
				Write-Log "Performing action [$verb] on [$FilePath] is not progamtically supported on this system."
		}
		Else {
				Write-Log "Performing [$verb] on [$FilePath]..."
			$itemVerb.DoIt()
		}
	}
		Catch {
			Write-Log "Unable to perform [$verb] on [$FilePath]."
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

Function Get-IniValue {
<#
.SYNOPSIS
	Parses an ini file and returns the value of the specified section and key
.DESCRIPTION
	Parses an ini file and returns the value of the specified section and key
.EXAMPLE
	Get-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section "Notes" -Key "KeyFileName"
.PARAMETER FilePath
	Path to the ini file
.PARAMETER Section
	Section within the ini file
.PARAMETER Key
	Key within the section of the ini file
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		[String] $FilePath,
		[String] $Section,
		[String] $Key,
		[boolean] $ContinueOnError = $true
	)

$signature = @'
[DllImport("kernel32.dll")]
public static extern uint GetPrivateProfileString(
	string lpAppName,
	string lpKeyName,
	string lpDefault,
	StringBuilder lpReturnedString,
	uint nSize,
	string lpFileName);
'@

	If (Test-Path $FilePath) {
		$type = Add-Type -MemberDefinition $signature -Name Win32Utils -Namespace GetPrivateProfileString -Using System.Text -PassThru
		$builder = New-Object System.Text.StringBuilder 1024
		$null = $type::GetPrivateProfileString($Section, $Key, "", $builder, $builder.Capacity, $FilePath)
		Return $builder.ToString()
	}
	Else {
		If ($ContinueOnError -eq $true) {
			Write-Log "File [$filePath] could not be found."
		}
		Else {
			Throw "File [$filePath] could not be found."
		}
	}
}

Function Set-IniValue {
<#
.SYNOPSIS
	Opens an ini file and sets the value of the specified section and key
.DESCRIPTION
	Opens an ini file and sets the value of the specified section and key
.EXAMPLE
	Set-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section "Notes" -Key "KeyFileName" -Value "MyFile.ID"
.PARAMETER FilePath
	Path to the ini file
.PARAMETER Section
	Section within the ini file
.PARAMETER Key
	Key within the section of the ini file
.PARAMETER Value
	Value for the key within the section of the ini file
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		[String] $FilePath,
		[String] $Section,
		[String] $Key,
		[String] $Value,
		[boolean] $ContinueOnError = $true
	)

$signature = @'
[DllImport("kernel32.dll")]
public static extern uint WritePrivateProfileString(
	string lpSectionName,
	string lpKeyName,
	string lpValue,
	string lpFileName);
'@

	If (Test-Path $FilePath) {
		## Create a new type that lets us access the Windows API function
		$type = Add-Type -MemberDefinition $signature -Name Win32Utils -Namespace WritePrivateProfileString -Using System.Text -PassThru

		$null = $type::WritePrivateProfileString($Section, $Key, $Value, $FilePath)
	}
	Else {
		If ($ContinueOnError -eq $true) {
			Write-Log "File [$filePath] could not be found."
		}
		Else {
			Throw "File [$filePath] could not be found."
		}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		[String] $FilePath,
		[boolean] $ContinueOnError = $true
	)

	Write-Log "Registering DLL file [$filePath]..."

	If (Test-Path $FilePath ) {
		If ($FilePath -Match "SysWOW64") {
			Execute-Process "$envWinDir\SysWOW64\RegSVR32.exe" -Arguments "/s `"$FilePath`"" -WindowStyle Hidden -PassThru
		}
		Else {
			Execute-Process "$envWinDir\System32\RegSVR32.exe" -Arguments "/s `"$FilePath`"" -WindowStyle Hidden -PassThru
		}
	}
	Else {
		If ($ContinueOnError -eq $true) {
			Write-Log "File [$filePath] could not be found."
		}
		Else {
			Throw "File [$filePath] could not be found."
		}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[Parameter(Mandatory = $true)]
		[String] $FilePath,
		[boolean] $ContinueOnError = $true
	)

	Write-Log "Unregistering DLL file [$filePath]..."

	If (Test-Path $FilePath ) {
		Try {
			If ($FilePath -Match "SysWOW64") {
				Execute-Process "$envWinDir\SysWOW64\RegSVR32.exe" -Arguments "/s /u `"$FilePath`"" -WindowStyle Hidden
			}
			Else {
				Execute-Process "$envWinDir\System32\RegSVR32.exe" -Arguments "/s /u `"$FilePath`"" -WindowStyle Hidden
			}
		}
		Catch {
			If ($ContinueOnError -eq $false) {
				Throw "Failed to register DLL file [$FilePath]."
			}
		}
	}
	Else {
		If ($ContinueOnError -eq $true) {
			Write-Log "File [$filePath] could not be found."
		}
		Else {
			Throw "File [$filePath] could not be found."
		}
	}

}

Function Test-MSUpdates {
<#
.SYNOPSIS
	Test whether an Microsoft Windows update is installed
.DESCRIPTION
	Test whether an Microsoft Windows update is installed
.EXAMPLE
	Test-MSUpdates "KB2549864"
.PARAMETER KBNumber
	KBNumber
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[ValidateNotNullorEmpty()]
		[Parameter(Position=0,Mandatory=$True,HelpMessage="Enter a KB Number for the MS update")]
		[string] $KBNumber
	)

	Write-Log "Testing for Microsoft Update $kbNumber..."

	# Default is not found
	$kbFound = $false

	# Check using Update method (to catch Office updates)
	$Session = New-Object -ComObject Microsoft.Update.Session
	$Collection = New-Object -ComObject Microsoft.Update.UpdateColl
	$Installer = $Session.CreateUpdateInstaller()
	$Searcher = $Session.CreateUpdateSearcher()
	$updateCount = $Searcher.GetTotalHistoryCount()
	If ($updateCount -gt 0) {
		$Searcher.QueryHistory(0, $updateCount) | Where-Object { $_.Title -match $kbNumber } | ForEach-Object { $kbFound = $true }
	}

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
	Install-MSUpdates "$dirFiles\MSUpdates"
.PARAMETER Directory
	Directory containing the updates
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>

	Write-Log "Installing Microsoft Updates from directory [$Directory]"

	# KB Number pattern match
	$kbPattern = '(?i)kb\d{6,8}'

	# Get all hotfixes and install if required
	$files = Get-ChildItem $Directory -Recurse -Include @("*.exe", "*.msu", "*.msp")
	ForEach ($file in $files) {
		# Get the KB number of the file
		$kbNumber = [regex]::match($file, $kbPattern).ToString()
		If ($kbNumber -eq "" -or $kbNumber -eq $null) { Continue }
		# Check to see whether the KB is already installed
		If ((Test-MSUpdates -kbNumber $kbNumber) -eq $false) {
			Write-Log "$kbNumber was not detected and will be installed."
			Switch ($file.Extension) {
				# Installation type for executables (ie, Microsoft Office Updates)
				".exe" { Execute-Process -FilePath $file -Arguments "/quiet /norestart" -WindowStyle Hidden -ContinueOnError $true }
				# Installation type for Windows updates using Windows Update Standalone Installer
				".msu" { Execute-Process -FilePath "wusa.exe" -Arguments "`"$file`" /quiet /norestart" -WindowStyle Hidden -ContinueOnError $true }
				# Installation type for Windows Installer Patch
				".msp" { Execute-MSI -Action "Patch" -Path $file -ContinueOnError $true }
			}
		}
		Else {
			Write-Log "$kbNumber was already installed. Skipping..."
		}
	}
}

Function Send-Keys
{
<#
.SYNOPSIS
    Send a sequence of keys to an application window.
    
.DESCRIPTION
    Send a sequence of keys to an application window. 
    
.PARAMETER WindowTitle
    The title of the application window. This can be a partial title.
    
.PARAMETER Keys
    The sequence of keys to send
    
.PARAMETER WaitSeconds
    An optional number of seconds to wait after the sending of the keys
    
.EXAMPLE
    Send-Keys "foobar - Notepad" "Hello world"
    
Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".
    
.EXAMPLE
    Send-Keys "foobar - Notepad" "Hello world" -WaitSeconds 5
    
    Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.
    
.EXAMPLE
    New-Item foobar.txt -ItemType File
    notepad foobar.txt
    Send-Keys "foobar - Notepad" "Hello world{ENTER}Ciao mondo{ENTER}" -WaitSeconds 1
    Send-Keys "foobar - Notepad" "^s"

    This command sequence creates a new text file called foobar.txt, opens the file using notepad.exe,
    writes some text, and saves the file using notepad.
    
.LINK
    http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx
	Http://psappdeploytoolkit.codeplex.com
#>
    
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true,Position=1)]
        [string]$WindowTitle,

        [Parameter(Mandatory=$true,Position=2)]
        [string]$Keys,

        [Parameter(Mandatory=$false)]
        [int]$WaitSeconds
    )
    
    Begin {
        ${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        $PSParameters = New-Object -TypeName PSObject -Property $PSBoundParameters
        
        If (-not [string]::IsNullOrEmpty($PSParameters)) {
            Write-Log "Function Send-Keys invoked with bound parameters [$PSParameters]"
        }
        Else {
            Write-Log "Function Send-Keys invoked without any bound parameters"
        }
    }
    Process {
        Try {
            # Load assembly containing class System.Windows.Forms.SendKeys
            Add-Type -AssemblyName System.Windows.Forms -ErrorAction 'Stop'
            
            # Add a C# class to access the WIN32 API SetForegroundWindow
            $SetForegroundWindow = @"
                using System;
                using System.Runtime.InteropServices;
                public class WINDOW
                {
                    [DllImport("user32.dll")]
                    [return: MarshalAs(UnmanagedType.Bool)]
                    public static extern bool SetForegroundWindow(IntPtr hWnd);
                }
"@
            If (-not ([System.Management.Automation.PSTypeName]'WINDOW').Type) {
                Add-Type $SetForegroundWindow -ErrorAction 'Stop'
            }
            
            # Get the process with the specified window title
            $Process = Get-Process -ErrorAction 'Stop' | Where-Object { $_.MainWindowTitle.Contains($WindowTitle) }
            If ($Process) {
                Write-Log "Matching window title found running under process [$($process.name)]..."
                # Get the window handle of the first process only if there is more than one process returned
                $ProcessHandle = $Process[0].MainWindowHandle
                Write-Log "Bringing window to foreground..."
                # Bring the process to the foreground
                $ActivateWindow = [WINDOW]::SetForegroundWindow($ProcessHandle)
                
                # Send the Key sequence
                # Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx
                If ($ActivateWindow) {
                    Write-Log "Sending keys to window..."
                    [System.Windows.Forms.SendKeys]::SendWait($Keys)
                }
                Else {
                    Write-Log "Failed to bring window to foreground."
                    # Failed to bring the window to the foreground. Do nothing.
                }
                
                If ($WaitSeconds) {
                    Start-Sleep -Seconds $WaitSeconds
                }
            }
        }
        Catch {
            Write-Log "Failed to send keys `n$_.Exception.Message"
            Exit
        }
    }
    End {
        Write-Log "Send keys action complete."
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

	$onPower = $false
	$batteryStatus = Get-WmiObject -Class BatteryStatus -Namespace root\wmi -ComputerName . -ErrorAction SilentlyContinue
	If ($batteryStatus) {
		ForEach ($battery in $batteryStatus) {
			$power = $battery.PowerOnLine
			If ($power) {
				Write-Log "AC Power connection found."
				$onPower = $true
			}
		}
		If ($onPower) {
			Return $false
		}
	}

	Write-Log "AC Power connection not found"
	Return $true
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
	Tests whether PowerPoint is running
.DESCRIPTION
	Tests whether PowerPoint is running
.EXAMPLE
	Test-PowerPoint
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>

	Write-Log "Testing Powerpoint status..."

	Try {
		If (Get-Process "powerpnt" -ErrorAction SilentlyContinue) {
			Write-Log "PowerPoint is running."
			Return $true
		}
		Else {
			Write-Log "PowerPoint is not running."
			Return $false
		}
	}
	Catch [Exception] {
		Write-Log "PowerPoint is not running."
		Return $false
	}
}

Function Invoke-SCCMTask {
<#
.SYNOPSIS
	Triggers SCCM to invoke the relevant task
.DESCRIPTION
	Triggers SCCM to invoke the relevant task
.EXAMPLE
	Invoke-SCCMTask "SoftwareUpdatesScan"
.PARAMETER ScheduleId
	ScheduleId
.EXAMPLE
	Invoke-SCCMTask
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	[CmdletBinding()]
	Param(
		[ValidateSet("HardwareInventory","SoftwareInventory","HeartbeatDiscovery","SoftwareInventoryFileCollection","RequestMachinePolicy","EvaluateMachinePolicy","LocationServicesCleanup","SoftwareMeteringReport","SourceUpdate","PolicyAgentCleanup","RequestMachinePolicy2","CertificateMaintenance","PeerDistributionPointStatus","PeerDistributionPointProvisioning","ComplianceIntervalEnforcement","SoftwareUpdatesAgentAssignmentEvaluation","UploadStateMessage","StateMessageManager","SoftwareUpdatesScan","AMTProvisionCycle")]
		[string] $ScheduleID,
		[boolean] $ContinueOnError = $true
	)

	$ScheduleIds = @{
		HardwareInventory = "{00000000-0000-0000-0000-000000000001}";							# Hardware Inventory Collection Task
		SoftwareInventory = "{00000000-0000-0000-0000-000000000002}"; 							# Software Inventory Collection Task
		HeartbeatDiscovery = "{00000000-0000-0000-0000-000000000003}"; 							# Heartbeat Discovery Cycle
		SoftwareInventoryFileCollection = "{00000000-0000-0000-0000-000000000010}"; 			# Software Inventory File Collection Task
		RequestMachinePolicy = "{00000000-0000-0000-0000-000000000021}"; 						# Request Machine Policy Assignments
		EvaluateMachinePolicy = "{00000000-0000-0000-0000-000000000022}"; 						# Evaluate Machine Policy Assignments
		RefreshDefaultMp = "{00000000-0000-0000-0000-000000000023}"; 							# Refresh Default MP Task
		RefreshLocationServices = "{00000000-0000-0000-0000-000000000024}"; 					# Refresh Location Services Task
		LocationServicesCleanup = "{00000000-0000-0000-0000-000000000025}"; 					# Location Services Cleanup Task
		SoftwareMeteringReport = "{00000000-0000-0000-0000-000000000031}"; 						# Software Metering Report Cycle
		SourceUpdate = "{00000000-0000-0000-0000-000000000032}"; 								# Source Update Manage Update Cycle
		PolicyAgentCleanup = "{00000000-0000-0000-0000-000000000040}"; 							# Policy Agent Cleanup Cycle
		RequestMachinePolicy2 = "{00000000-0000-0000-0000-000000000042}"; 						# Request Machine Policy Assignments
		CertificateMaintenance = "{00000000-0000-0000-0000-000000000051}"; 						# Certificate Maintenance Cycle
		PeerDistributionPointStatus = "{00000000-0000-0000-0000-000000000061}"; 				# Peer Distribution Point Status Task
		PeerDistributionPointProvisioning = "{00000000-0000-0000-0000-000000000062}"; 			# Peer Distribution Point Provisioning Status Task
		ComplianceIntervalEnforcement = "{00000000-0000-0000-0000-000000000071}"; 				# Compliance Interval Enforcement
		SoftwareUpdatesAgentAssignmentEvaluation = "{00000000-0000-0000-0000-000000000108}"; 	# Software Updates Agent Assignment Evaluation Cycle
		UploadStateMessage = "{00000000-0000-0000-0000-000000000111}"; 							# Send Unsent State Messages
		StateMessageManager = "{00000000-0000-0000-0000-000000000112}"; 						# State Message Manager Task
		SoftwareUpdatesScan = "{00000000-0000-0000-0000-000000000113}"; 						# Force Update Scan
		AMTProvisionCycle = "{00000000-0000-0000-0000-000000000120}"; 							# AMT Provision Cycle
	}

	Write-Log "Invoking SCCM Task [$ScheduleId]..."

	# Trigger SCCM task
	Try {
		$SmsClient = [wmiclass]"ROOT\ccm:SMS_Client"
		$SmsClient.TriggerSchedule($ScheduleIds.$ScheduleID) | Out-Null
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
		    Write-Log "Trigger SCCM Schedule failed for Schedule ID $($ScheduleIds.$ScheduleId): $($_.Exception.Message)"
        }
		Else {
			Throw "Trigger SCCM Schedule failed for Schedule ID $($ScheduleIds.$ScheduleId): $($_.Exception.Message)"
		}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[boolean] $ContinueOnError = $true
	)

	# Scan for updates
	Write-Log "Scanning for SCCM Software Updates..."
	Invoke-SCCMTask -ScheduleId "SoftwareUpdatesScan"

	Write-Log "Sleeping 180 seconds..."
	Sleep -Seconds 180

	Write-Log "Installing pending software updates..."
	Try {
		$SmsSoftwareUpdates = [wmiclass]"ROOT\ccm:SMS_Client"
		$SmsSoftwareUpdates.InstallUpdates([System.Management.ManagementObject[]] (Get-WmiObject -Query “SELECT * FROM CCM_SoftwareUpdate” -Namespace “ROOT\ccm\ClientSDK”)) | Out-Null
	}
	Catch [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log "Trigger SCCM Install Updates failed: " + $_.Exception.Message
		}
		Else {
			Throw "Trigger SCCM Install Updates failed: " + $_.Exception.Message
		}
	}
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
	Execute-Process -FilePath "$envWindir\system32\cmd.exe" -Arguments "/C Echo N | GPUpdate /Target:Computer /Force" -WindowStyle Hidden
	Execute-Process -FilePath "$envWindir\system32\cmd.exe" -Arguments "/C Echo N | GPUpdate /Target:User /Force" -WindowStyle Hidden
}

Function Enable-TerminalServerInstallMode {
<#
.SYNOPSIS
	Changes to user install mode for Remote Desktop Session Host/Citrix servers
.DESCRIPTION
	Changes to user install mode for Remote Desktop Session Host/Citrix servers
.EXAMPLE
	Enable-TerminalServerInstall
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Param (
		[boolean] $ContinueOnError = $true
	)

	Write-Log "Changing to user install mode for Terminal Server..."
	$terminalServerResult = Change User /Install
	If ($terminalServerResult -notmatch "User Session is ready to install applications" -and $ContinueOnError -ne $true) {
		Throw $terminalServerResult
	}
	Else {
		Write-Log $terminalServerResult
	}
}

Function Disable-TerminalServerInstallMode {
<#
.SYNOPSIS
	Changes to user install mode for Remote Desktop Session Host/Citrix servers
.DESCRIPTION
	Changes to user install mode for Remote Desktop Session Host/Citrix servers
.EXAMPLE
	Enable-TerminalServerInstall
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
#>
	Write-Log "Changing to user execute mode for Terminal Server..."
	$terminalServerResult = Change User /Execute
	Write-Log $terminalServerResult
}


#*=============================================
#* END FUNCTION LISTINGS
#*=============================================

#*=============================================
#* SCRIPT BODY
#*=============================================

# Assemblies: Load
Try {
	Add-Type -AssemblyName System.Windows.Forms
	Add-Type -AssemblyName PresentationFramework
	Add-Type -AssemblyName Microsoft.VisualBasic
	Add-Type -AssemblyName System.Drawing
	Add-Type -AssemblyName PresentationFramework
	Add-Type -AssemblyName PresentationCore
	Add-Type -AssemblyName WindowsBase
}
Catch [Exception] {
	$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)"
	Write-Log "Error Loading Assembly: $exceptionMessage"
	If ($deployModeNonInteractive -eq $true) {
		Write-Log "Continuing despite assembly error since deployment mode is [$deployMode]"
	}
	Else {
		Exit-Script 1
	}
}

# Set the install name if the referring application parameter was specified
If ($ReferringApplication -ne "") {
	$installName = $ReferringApplication
	$installTitle = $ReferringApplication -replace "_"," "
	$installPhase = "Asynchronous"
}

# If the ShowInstallationPrompt Parameter is specified, only call that function.
If ($showInstallationPrompt -eq $true) {
	$deployModeSilent = $true
	Write-Log "$appDeployMainScriptFriendlyName called with switch ShowInstallationPrompt"
	$appDeployMainScriptParameters.Remove("ShowInstallationPrompt")
	$appDeployMainScriptParameters.Remove("ReferringApplication")
	Show-InstallationPrompt @appDeployMainScriptParameters
	Exit 0
}

# If the ShowInstallationRestartPrompt Parameter is specified, only call that function.
If ($showInstallationRestartPrompt -eq $true) {
	$deployModeSilent = $true
	Write-Log "$appDeployMainScriptFriendlyName called with switch ShowInstallationRestartPrompt"
	$appDeployMainScriptParameters.Remove("ShowInstallationRestartPrompt")
	$appDeployMainScriptParameters.Remove("ReferringApplication")
	Show-InstallationRestartPrompt @appDeployMainScriptParameters
	Exit 0
}

# If the cleanupBlockedApps Parameter is specified, only call that function.
If ($cleanupBlockedApps -eq $true) {
	$deployModeSilent = $true
	Write-Log "$appDeployMainScriptFriendlyName called with switch CleanupBlockedApps"
	Unblock-AppExecution
	Exit 0
}

# If the showBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog -eq $true) {
	$DisableLogging = $true
	Try {
		$deployModeSilent = $true
		Write-Log "$appDeployMainScriptFriendlyName called with switch ShowBlockedAppDialog"
		Show-InstallationPrompt -Title $installTitle -Message $configBlockExecutionMessage -Icon Warning -ButtonRightText "OK"
		Exit 0
	}
	Catch {
		$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)"
		Write-Log "$exceptionMessage"
		Show-DialogBox -Text $exceptionMessage -Icon "Stop"
		Exit 1
	}
}

# Initialization Logging
$installPhase = "Initialization"

Write-Log "$installName setup started."

# Dot Source script extensions
If ($appDeployToolkitDotSourceExtensions -ne "") {
	."$scriptRoot\$appDeployToolkitDotSourceExtensions"
}

# Evaluate non-default parameters passed to the scripts
If ($deployAppScriptParameters) { $deployAppScriptParameters = $deployAppScriptParameters.GetEnumerator() | % { "-$($_.Key) $($_.Value)" } }
If ($appDeployMainScriptParameters) { $appDeployMainScriptParameters = $appDeployMainScriptParameters.GetEnumerator() | % { "-$($_.Key) $($_.Value)" } }
If ($appDeployExtScriptParameters) { $appDeployExtScriptParameters = $appDeployExtScriptParameters.GetEnumerator() | % { "-$($_.Key) $($_.Value)" } }

$invokingScript = $(((Get-Variable MyInvocation).Value).ScriptName)

# Check the XML config file version
If ($configConfigVersion -lt $appDeployMainScriptMinimumConfigVersion) {
	Throw "The XML configuration file version [$configConfigVersion] is lower than the supported version required by the Toolkit [$appDeployMainScriptMinimumConfigVersion]. Please upgrade the configuration file."
}

If ($appScriptVersion -ne $null ) { Write-Log "$installName script version is [$appScriptVersion]" }
If ($deployAppScriptFriendlyName -ne $null ) { Write-Log "$deployAppScriptFriendlyName script version is [$deployAppScriptVersion]" }
If ($deployAppScriptParameters -ne $null) { Write-Log "The following non-default parameters were passed to [$deployAppScriptFriendlyName]: [$deployAppScriptParameters]" }
If ($appDeployMainScriptFriendlyName -ne $null ) { Write-Log "$appDeployMainScriptFriendlyName script version is [$appDeployMainScriptVersion]" }
If ($appDeployMainScriptParameters -ne $null) { Write-Log "The following non-default parameters were passed to [$appDeployMainScriptFriendlyName]: [$appDeployMainScriptParameters]" }
If ($appDeployExtScriptFriendlyName -ne $null ) { Write-Log "$appDeployExtScriptFriendlyName version is [$appDeployExtScriptVersion]" }
If ($appDeployExtScriptParameters -ne $null) { Write-Log "The following non-default parameters were passed to [$appDeployExtScriptFriendlyName]: [$appDeployExtScriptParameters]" }
Write-Log "PowerShell version is [$($PSVersionTable.PSVersion) $psArchitecture]"
Write-Log "PowerShell host is [$($envHost.name) version $($envHost.version)]"
Write-Log "OS version is [$($envOS.Caption) $($envOS.OSArchitecture) $($envOS.Version)]"
Write-Log "Hardware platform is [$(Get-HardwarePlatform)]"
Write-Log "Computer name is [$envComputerName]"
If ($envUserName -ne $null ) { Write-Log "Current user is [$envUserDomain\$envUserName]" }
Write-Log "Current Culture is [$($culture | Select Name -ExpandProperty Name)] and UI language is [$currentLanguage]"

# Check deployment type (install/uninstall)
Switch ($deploymentType) {
	"Install" { $deploymentTypeName = $configDeploymentTypeInstall }
	"Uninstall" { $deploymentTypeName = $configDeploymentTypeUnInstall }
	Default { $deploymentTypeName = $configDeploymentTypeInstall }
}
If ($deploymentTypeName -ne $null ) { Write-Log "Deployment type is [$deploymentTypeName]" }

# Check how the script was invoked
If ($invokingScript -ne "") {
	Write-Log "Script [$($MyInvocation.MyCommand.Definition)] dot-source invoked by [$invokingScript]"
	# If the script was invoked by the Help console, exit the script now because we don't need to initialize logging.
	If ($(((Get-Variable MyInvocation).Value).ScriptName) -match "Help") {
		Return
	}
	Else { 
		# Check if a user is logged on to the system
		$usersLoggedOn = Get-WmiObject -Class "Win32_ComputerSystem" -Property UserName | Where-Object {$_.UserName -ne $null} | Select UserName -ExpandProperty UserName
		If ($usersLoggedOn -ne $Null) {
			Write-Log "The following users are logged on to the system: $($usersLoggedOn | % {$_ -join ","})"
		}
		Else {
			Write-Log "No User is logged on"
		}

		# Check if we are running in the logged in user context (ie, PowerShell is running in the same context as Explorer)
		If (Get-WmiObject -Class Win32_Process -Filter "Name='explorer.exe'" | Where { $_.GetOwner().User -eq $envUsername }) {
			Write-Log "Running as [$envUsername] in user context."
		}
		# Check if we are running a task sequence, and enable NonInteractive mode
		ElseIf (Get-Process -Name "TSManager" -ErrorAction SilentlyContinue) {
			Write-Log "Running in SCCM Task Sequence."
			$runningTaskSequence = $true
			$sessionZero = $true
		}
		# Check if we are running in session zero on XP or lower
		ElseIf ($envOS.Version -le "5.2") {
			If ((Get-WmiObject -Class Win32_Process -Filter "Name='explorer.exe'") -eq $null) {
				$sessionZero = $true
			}
		}
		# Check if we are running in session zero on all OS higher than XP
		ElseIf (([System.Diagnostics.Process]::GetCurrentProcess() | Select "SessionID" -ExpandProperty "SessionID") -eq 0) {
			$sessionZero = $true
		}
		
		# If we are running in Session zero and the deployment mode has not been set to NonInteractive by the admin or because we are running in task sequence  
		If ($sessionZero -eq $true) {
			Write-Log "Session 0 detected."
			If ($deployMode -ne "NonInteractive") {
				If ($runningTaskSequence -ne $true) {
					$deployMode = "NonInteractive"
				}
				Else {
					$deployMode = "NonInteractive"
					Write-Log "Session 0 detected but a task sequence is running, setting deployment mode to [$deployMode]."
				}
			}
			Else {
				Write-Log "Session 0 detected but deployment mode is set to NonInteractive."
			}
		}
		Else {
			Write-Log "Session 0 not detected."
		}
	}
}
Else {
	Write-Log "Script [$($MyInvocation.MyCommand.Definition)] invoked directly"
}


If ($deployMode -ne $null) {
	Write-Log "Installation is running in [$deployMode] mode."
}

# Set Deploy Mode switches
Switch ($deployMode) {
	"Silent" { $deployModeSilent = $true }
	"NonInteractive" { $deployModeNonInteractive = $true; $deployModeSilent = $true }
	Default {$deployModeNonInteractive = $false; $deployModeSilent = $false}
}

# Check current permissions and exit if not running with Administrator rights
If ($configToolkitRequireAdmin) {
	If (!([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
		If ($ShowBlockedAppDialog -ne $true) {
			Throw "Error: $appDeployMainScriptFriendlyName requires Administrator rights to function. Please re-run the deployment script as an Administrator."
		}
	}
}

# If terminal server mode was specified, change the installation mode to support it
If ($terminalServerMode) { Enable-TerminalServerInstallMode }

#*=============================================
#* END SCRIPT BODY
#*=============================================