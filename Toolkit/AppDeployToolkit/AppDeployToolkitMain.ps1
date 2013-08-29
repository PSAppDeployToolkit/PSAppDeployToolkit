<#
.SYNOPSIS
	This script contains the functions and logic engine for the Deploy-Application.ps1 script.
.DESCRIPTION
	The script can be called directly to dot-source the toolkit functions for testing, but it is usually called by the Deploy-Application.ps1 script.
	The script can usually be updated to the latest version without impacting your per-application Deploy-Application scripts. Please check release notes before upgrading.   
.PARAMETER ContinueOnErrorGlobalPreference
	Sets the global preference for the -ContinueOnError. This global preference is set on most functions to True by default. 
	The purpose of having this global variable is to assist with script debugging so that you can stop the script if any functions throw an error. 
	To debug the script, set to $false or add the parameter to the dot-sourcing line in the Deploy-Application.ps1 script, e.g.
	."$scriptDirectory\AppDeployToolkit\AppDeployToolkitMain.ps1" -ContinueOnErrorGlobalPreference $false
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
.LINK 
	Http://psappdeploytoolkit.codeplex.com
"#>
Param (
	## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or Image File Execution Options
	[switch] $ContinueOnErrorGlobalPreference = $true,
	[switch] $CleanupBlockedApps = $false, 
	[switch] $ShowBlockedAppDialog = $false, 
	[switch] $DisableLogging = $false,  
	[string] $ReferringApplication = $Null
)

#*=============================================
#* VARIABLE DECLARATION
#*=============================================

# Variables: Toolkit 
$appDeployToolkitName = "PSAppDeployToolkit"

# Variables: Script
$appDeployMainScriptFriendlyName = "App Deploy Toolkit Main"
$appDeployMainScriptVersion = "3.0.2"
$appDeployMainScriptDate = "08/28/2013"
$appDeployMainScriptParameters = $psBoundParameters

# Variables: Environment
$currentDate = (Get-Date -UFormat "%d-%m-%Y")
$currentTime = (Get-Date -UFormat "%T")
$culture = Get-Culture
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
$dirAppDeployTemp = Join-Path $env:PUBLIC ($appDeployToolkitName)
$dirBlockedApps = Join-Path $dirAppDeployTemp "BlockedApps" 

# Variables: App Deploy Dependency Files
$appDeployLogoIcon = Join-Path $scriptRoot "AppDeployToolkitLogo.ico"
$appDeployLogoBanner = Join-Path $scriptRoot "AppDeployToolkitBanner.png"
$appDeployConfigFile = Join-Path $scriptRoot "AppDeployToolkitConfig.xml"

# Variables: App Deploy Optional Files
# Specify any additional PowerShell script files to be dot-sourced by this script, separated by commas.
$appDeployToolkitDotSources = "AppDeployToolkitExtensions.ps1" 

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

# Get MSI Options
$xmlConfigMSIOptions = $xmlConfig.MSI_Options
$configMSILoggingOptions = $xmlConfigMSIOptions.MSI_LoggingOptions
$configMSIInstallParams = $xmlConfigMSIOptions.MSI_InstallParams
$configMSISilentParams = $xmlConfigMSIOptions.MSI_SilentParams
$configMSIUninstallParams = $xmlConfigMSIOptions.MSI_UninstallParams
$configDirLogs = $xmlConfigMSIOptions.MSI_LogPath
# Get UI Options
$xmlConfigUIOptions = $xmlConfig.UI_Options
$configShowBalloonNotifications = $xmlConfigUIOptions.ShowBalloonNotifications
$configInstallationUITimeout = $xmlConfigUIOptions.InstallationUI_Timeout
# Get Message UI Language Options (default for English if no localization found)
$xmlUIMessageLanguage = "UI_Messages_" + $currentLanguage
$xmlUIMessages = $xmlConfig.$xmlUIMessageLanguage
If ($xmlUIMessages -eq $null) { 
	$xmlUIMessageLanguage = "UI_Messages_EN"
	$xmlUIMessages = $xmlConfig.$xmlMessageUILanguage
}
$configBalloonTextStart = $xmlUIMessages.BalloonText_Start
$configBalloonTextComplete = $xmlUIMessages.BalloonText_Complete
$configBalloonTextRestartRequired = $xmlUIMessages.BalloonText_RestartRequired
$configBalloonTextFastRetry = $xmlUIMessages.BalloonText_FastRetry
$configBalloonTextError = $xmlUIMessages.BalloonText_Error
$configProgressMessage = $xmlUIMessages.Progress_Message
$configClosePromptConfirm = $xmlUIMessages.ClosePrompt_Confirm
$configClosePromptMessage = $xmlUIMessages.ClosePrompt_Message
$configClosePromptButtonClose = $xmlUIMessages.ClosePrompt_ButtonClose
$configClosePromptButtonDefer = $xmlUIMessages.ClosePrompt_ButtonDefer
$configClosePromptButtonContinue = $xmlUIMessages.ClosePrompt_ButtonContinue
$configClosePromptCountdownMessage = $xmlUIMessages.ClosePrompt_CountdownMessage
$configDeferPromptWelcomeMessage = $xmlUIMessages.DeferPrompt_WelcomeMessage
$configDeferPromptExpiryMessage = $xmlUIMessages.DeferPrompt_ExpiryMessage
$configDeferPromptWarningMessage = $xmlUIMessages.DeferPrompt_WarningMessage
$configDeferPromptRemainingDeferrals = $xmlUIMessages.DeferPrompt_RemainingDeferrals
$configDeferPromptRemainingDays = $xmlUIMessages.DeferPrompt_RemainingDays
$configDeferPromptDeadline = $xmlUIMessages.DeferPrompt_Deadline
$configDeferPromptNoDeadline = $xmlUIMessages.DeferPrompt_NoDeadline
$configBlockExecutionMessage = $xmlUIMessages.BlockExecution_Message
$configDeploymentTypeInstall = $xmlUIMessages.DeploymentType_Install
$configDeploymentTypeUnInstall = $xmlUIMessages.DeploymentType_UnInstall
$configRestartPromptTitle = $xmlUIMessages.RestartPrompt_Title
$configRestartPromptMessage = $xmlUIMessages.RestartPrompt_Message
$configRestartPromptTimeRemaining = $xmlUIMessages.RestartPrompt_TimeRemaining
$configRestartPromptButtonRestartLater = $xmlUIMessages.RestartPrompt_ButtonRestartLater
$configRestartPromptButtonRestartNow = $xmlUIMessages.RestartPrompt_ButtonRestartNow

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
# Reset the deferral history
$deferHistory = $deferTimes = $deferDays = $null

# Assemblies: Load
# Reset Assembly Errors & Warnings
$AssemblyError = $AssemblyWarning = $null
Add-Type -AssemblyName System.Windows.Forms -ErrorVariable +AssemblyError -WarningVariable +AssemblyWarning
Add-Type -AssemblyName PresentationFramework -ErrorVariable +AssemblyError -WarningVariable +AssemblyWarning
Add-Type -AssemblyName Microsoft.VisualBasic -ErrorVariable +AssemblyError -WarningVariable +AssemblyWarning
Add-Type -AssemblyName System.Drawing -ErrorVariable +AssemblyError -WarningVariable +AssemblyWarning
Add-Type -AssemblyName PresentationFramework -ErrorVariable +AssemblyError -WarningVariable +AssemblyWarning
Add-Type -AssemblyName PresentationCore -ErrorVariable +AssemblyError -WarningVariable +AssemblyWarning
Add-Type -AssemblyName WindowsBase -ErrorVariable +AssemblyError -WarningVariable +AssemblyWarning

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
$regKeyDeferHistory = "HKLM:\SOFTWARE\$appDeployToolkitName\DeferHistory\$installName"

# Variables: 
$debuggerBlockValue = "powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -file $scriptRoot\$scriptFileName -ShowBlockedAppDialog -ReferringApplication `"$installName`""

# Variables: Log Files
$logFile = Join-Path $configDirLogs ("$installName" + "_$appDeployToolkitName.log")

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
		Write-Host "[$currentDate $currentTime] [$installPhase] $Text"
		If ($DisableLogging -eq $false) {
			# Create the Log directory if it doesn't already exist
			If (!(Test-Path -path $configDirLogs -ErrorAction SilentlyContinue )) { New-Item $configDirLogs -Type directory -ErrorAction SilentlyContinue | Out-Null }
			# Create the Log directory if it doesn't already exist
			If (!(Test-Path -path $logFile -ErrorAction SilentlyContinue )) { New-Item $logFile -Type File -ErrorAction SilentlyContinue | Out-Null }
			Try {
				"[$currentDate $currentTime] [$installPhase] $Text" | Out-File $logFile -Append -ErrorAction SilentlyContinue
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
		Write-Log "$installName $deploymentTypeName completed with exit code [$exitcode]."		
		If ($exitCode -eq 1618) {
			$balloonText = "$deploymentTypeName $configBalloonTextFastRetry"			   
			Show-BalloonTip -BalloonTipIcon "Warning" -BalloonTipText "$balloonText"
		}
		Else {
			$balloonText = "$deploymentTypeName $configBalloonTextError"			
			Show-BalloonTip -BalloonTipIcon "Error" -BalloonTipText "$balloonText"
		}
	}

	Write-Log "----------------------------------------------------------------------------------------------------------"

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
	Show a button on the right of the prompt with the specified text [Default is OK]
.PARAMETER ButtonMiddleText
	Show a button in the middle of the prompt with the specified text
.PARAMETER Icon
	Show a system icon in the prompt ("Application","Asterisk","Error","Exclamation","Hand","Information","None","Question","Shield","Warning","WinLogo") [Default is "None"]
.PARAMETER NoWait
	Specifies whether to show the prompt asynchronously (i.e. allow the script to continue without waiting for a response) [Default is $false]
.NOTES	
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		$Title = $installTitle,
		$Message = $null,
        [ValidateSet("Left","Center","Right")]
        $MessageAlignment = "Center",
		$ButtonRightText = "OK",
		$ButtonLeftText = $null,
		$ButtonMiddleText = $null,
        [ValidateSet("Application","Asterisk","Error","Exclamation","Hand","Information","None","Question","Shield","Warning","WinLogo")] 
        [string] $Icon = "None",
        [switch] $NoWait = $false
	)	

    # Get parameters for calling function asynchronously
    $installPromptParameters = $psBoundParameters

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
	$System_Drawing_Point.X = 10
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
	$System_Drawing_Size.Width = 390
	$labelText.Size = $System_Drawing_Size
	$System_Drawing_Point = New-Object System.Drawing.Point
	$System_Drawing_Point.X = 20
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
	$timer.Interval = $configInstallationUITimeout
	$timer.Add_Tick({
		Write-Log "Installation not actioned within a reasonable amount of time."
		$buttonAbort.PerformClick()
	})

	# Save the initial state of the form
	$InitialFormInstallationPromptWindowState = $formInstallationPrompt.WindowState
	# Init the OnLoad event to correct the initial state of the form
	$formInstallationPrompt.add_Load($Form_StateCorrection_Load)
	# Clean up the control events
	$formInstallationPrompt.add_FormClosed($Form_Cleanup_FormClosed)

	# Start the timer
	$timer.Start()

	# Close the Installation Progress Dialog if running
	Close-InstallationProgress
    
    $installPromptLoggedParameters = ($installPromptParameters.GetEnumerator() | % { "($($_.Key)=$($_.Value))" }) -join " " 
    Write-Log "Displaying custom installation prompt with the non-default parameters: [$installPromptLoggedParameters]..."

    # If the NoWait parameter is specified, show the prompt asynchronously
	If ($NoWait -eq $true) {
        $installationPromptJob = [PowerShell]::Create().AddScript({
            Param($scriptPath,$installPromptParameters)
            .$scriptPath        
            $installPromptParameters.Remove("NoWait")
            Show-InstallationPrompt @installPromptParameters
        }).AddArgument($scriptPath).AddArgument($installPromptParameters)
        # Show the form asynchronously
        $installationPromptJobResult = $installationPromptJob.BeginInvoke()
    }
    # Otherwise show the prompt synchronously, and keep showing it if the user cancels it until the respond using one of the buttons
    Else {
	    $showDialog = $true
	    While ($showDialog -eq $true) {
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
		    "Abort" { Exit-Script 1618 }
	    }
    }
  
} #End Function

Function Show-DialogBox {
<# 
.SYNOPSIS
	This function displays a custom dialog box with optional title, buttons, icon and timeout.
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
	Timeout period in seconds before automatically closing the dialog box with the return message "Timeout" [Default the UI timeout value set in the config XML file]
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

	# Bypass if in totall silent mode
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
							# Bypass any updates or hotfixes
							If ([regex]::match($regKeyApp.DisplayName, "(?i)kb\d+") -eq $true) { Continue }
							If ($regKeyApp.DisplayName -match "Cumulative Update") { Continue }
							If ($regKeyApp.DisplayName -match "Security Update") { Continue }
							If ($regKeyApp.DisplayName -match "Hotfix") { Continue }
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
		[switch] $ContinueOnError = $false # Do not use Global $ContinueOnErrorGlobalPreference parameter as the script should default to an overall fail if an MSI fails to install
	)

	# Build the log file name
	If (!($logName)) {
		# If the path matches a product code, resolve the product code to an application name and version
		If ($path -match "^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$") {
			Write-Log "Execute-MSI: Product code specified, attempting to resolve product code to an application name and version..."
			$productCodeNameVersion = (Get-InstalledApplication -ProductCode $path | Select DisplayName,DisplayVersion -First 1 -ErrorAction SilentlyContinue)
			If ($productCodeNameVersion -ne $null) {
				If ($($productCodeNameVersion.Publisher) -ne $null) {
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
		[switch] $NoWait = $false,
		[switch] $PassThru = $false,
		[string] $IgnoreExitCodes = $false,
		[switch] $ContinueOnError = $false # Do not use Global $ContinueOnErrorGlobalPreference parameter as the script should default to an overall fail if a process execution fails
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
	)

	Write-Log "Copying File [$path] to [$destination]..."

	Copy-Item -Path "$Path" -Destination "$destination" -ErrorAction "STOP" -Force | Out-Null

	Trap [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Could not copy file [$path] to [$destination]:" + $_.Exception.Message)
			Continue
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
	)

	Write-Log "Deleting File(s) [$path]..."
	If ($Recurse) {
		Remove-Item -Path "$path" -ErrorAction "STOP" -Force -Recurse | Out-Null
	}
	Else {
		Remove-Item -Path "$path" -ErrorAction "STOP" -Force | Out-Null
	}
	Trap [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Could not delete file [$path]:" + $_.Exception.Message)
			Continue
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param ( 
		[Parameter(Mandatory = $true)]
		$Key,
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
			If ($ContinueOnError -eq $true) {
				Write-Log $("Registry key does not exist: [$key]" + $_.Exception.Message)
				Continue
			}
			Else {
				Throw $("Registry key does not exist: [$key]" + $_.Exception.Message)
			}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[Parameter(Mandatory = $true)] 
		[System.String] $Key,
		[System.String] $Name, 
		[System.String] $Value, 
		[Microsoft.Win32.RegistryValueKind]$Type="String",
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
	)

	$key = Convert-RegistryPath -Key $Key

	# Create registry key if it doesn't exist
	If (!(Test-Path $key -ErrorAction SilentlyContinue)) { 
		Write-Log "Creating Registry key [$key]..."
		New-Item -Path $key -ItemType Registry -Force | Out-Null 
		Trap [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to create registry key [$Key]" + $_.Exception.Message)
				Continue
			}
			Else {
				Throw $("Failed to create registry key [$Key]" + $_.Exception.Message)
			}
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
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to set registry value [$value] for registry key [$key] [$name]" + $_.Exception.Message)
				Continue
			}
			Else {
				Throw $("Failed to set registry value [$value] for registry key [$key] [$name]" + $_.Exception.Message)
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to delete registry key [$Key]:" + $_.Exception.Message)
				Continue
			}
			Else {
				Throw $("Failed to delete registry key [$Key]:" + $_.Exception.Message)
			}
		}
	}
	Else {
		Write-Log "Deleting Registry Value [$Key] [$name] ..."
		Remove-ItemProperty -Path $Key -Name $Name -ErrorAction "STOP" -Force | Out-Null
		Trap [Exception] {
			If ($ContinueOnError -eq $true) {
				Write-Log $("Failed to delete registry value [$Key] [$name]:" + $_.Exception.Message)
				Continue
			}
			Else {
				Throw $("Failed to delete registry value [$Key] [$name]:" + $_.Exception.Message)
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
			If ($ContinueOnError -eq $true) {
				Write-Log "Error getting file version info."
				Continue
			}
			Else {
				Throw "Error getting file version info."
			}
		}
	}
	Else {
		If ($ContinueOnError -eq $true) {
			Write-Log "File could not be found."
			Continue
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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

	Trap [Exception] {
		If ($ContinueOnError -eq $true) {
			Write-Log $("Failed to create shortcut [$path]:" + $_.Exception.Message)
			Continue
		}
		Else {
			Throw $("Failed to create shortcut [$path]:" + $_.Exception.Message)
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
	)

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
		If ($ContinueOnError -eq $true) {
			Write-Log $("Error refreshing Desktop:" + $_.Exception.Message)
			Continue
		}
		Else {
			Throw $("Error refreshing Desktop:" + $_.Exception.Message)
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
	4. Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameters -CleanupBlockedApps and -ReferringApplication  
	5. Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameters -ShowBlockedAppDialog and -ReferringApplication
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
		$schTaskCreation = Execute-Process -FilePath "schtasks.exe" -Arguments "/Create /TN $schTaskBlockedAppsName /RU System /SC ONSTART /TR `"powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -file $dirAppDeployTemp\$scriptFileName -CleanupBlockedApps -ReferringApplication $installName`" " -PassThru
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
		Set-RegistryKey -Key $blockedAppPath -Name "Debugger" -Value $debuggerBlockValue -ContinueOnError
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
				Set-RegistryKey -Key $blockedAppPath -Name "Debugger" -Value $blockedAppDebuggerValue -ContinueOnError
			}
			# If the Debugger value was not previously set, but the parent registry key existed, remove the value
			Else {
				Remove-RegistryKey -Key $blockedAppPath -Name "Debugger" -ContinueOnError
			}
		}
		# Otherwise, remove the registry key
		Else {
			Remove-RegistryKey -Key $blockedAppPath -ContinueOnError
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
	Get-RegistryKey -Key $regKeyDeferHistory -ContinueOnError
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
		Set-RegistryKey -Key $regKeyDeferHistory -Name "DeferTimesRemaining" -Value $deferTimesRemaining -ContinueOnError
	}
	If ($deferDeadline) {
		Write-Log "Setting deferral history...[DeferDeadline = $deferDeadline]"
		Set-RegistryKey -Key $regKeyDeferHistory -Name "DeferDeadline" -Value $deferDeadline -ContinueOnError
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
		$dateTime = [DateTime]::Parse($dateTime, $culture)
		# Convert the date in a universal sortable date time pattern based on the current culture
		Get-Date $dateTime -Format ($culture).DateTimeFormat.UniversalSortableDateTimePattern -ErrorAction SilentlyContinue
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
		$processNames = $processNames -replace "\.","" -replace "\*",""	
	
		$runningProcesses = Get-Process | Where { ($_.ProcessName -replace "\.","" -replace "\*","") -match $processNames } 
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
	Show-IntallationWelcome -CloseApps "iexplore,winword,excel"
	Prompt the user to close Internet Explorer, Word and Excel.
.EXAMPLE
	Show-IntallationWelcome -CloseApps "winword,excel" -Silent
	Close Word and Excel without prompting the user.
.EXAMPLE
	Show-IntallationWelcome -CloseApps "winword,excel" -BlockExecution
	Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
.EXAMPLE
	Show-IntallationWelcome -CloseApps "winword=Microsoft Office Word,excel=Microsoft Office Excel" -CloseAppsCountdown "600"
	Prompt the user to close Word and Excel, with customized descriptions for the applications and automatically close the applications after 10 minutes.
.EXAMPLE
	Show-IntallationWelcome -AllowDefer -DeferDeadline "25/08/2013"
	Allow the user to defer the installation until the deadline is reached. 
.EXAMPLE
	Show-IntallationWelcome -CloseApps "winword,excel" -BlockExecution -AllowDefer -DeferTimes "10" -DeferDeadline "25/08/2013" -CloseAppsCountdown "600"
	Close Word and Excel and prevent the user from launching the applications while the installation is in progress. 
	Allow the user to defer the installation a maximum of 10 times or until the deadline is reached, whichever happens first. 
	When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes. 
.PARAMETER CloseApps
	Name of the process to stop (do not include the .exe). Specify multiple processes separated by a comma. Specify custom descriptions like this: "winword=Microsoft Office Word,excel=Microsoft Office Excel"
.PARAMETER Silent
	Stop processes without prompting the user.
.PARAMETER CloseAppsCountdown
	Option to provide a countdown in seconds until the specified applications are automatically closed. This only takes effect if deferral is now allowed or has expired.
.PARAMETER BlockExecution
	Option to prevent the user from launching the process/application during the installation
.PARAMETER AllowDefer
	Enables an optional defer button to allow the user to defer the installation if they do not want to close running applications.
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
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param(
	[string]$CloseApps, # Specify process names separated by commas. Optionally specify a process description with an equals symobol, e.g. "winword=Microsoft Office Word" 
	[switch]$Silent = $false, # Specify whether to prompt user or force close the applications
	[int]$CloseAppsCountdown = $null, # Specify a countdown to display before automatically closing applications where defferal is not allowed or has expired
	[switch]$BlockExecution = $false, # Specify whether to block execution of the processes during installation	
	[switch]$AllowDefer = $false, # Specify whether to enable the optional defer button on the dialog box
	[int]$DeferTimes = $null, # Specify the number of times the deferral is allowed
	[int]$DeferDays = $null, # Specify the number of days since first run that the deferral is allowed
	[string]$DeferDeadline = $null # Specify the deadline (in format dd/mm/yyyy) for which deferral will expire as an option
	)

	# If running in NonInteractive mode, force the processes to close silently
	If ($deployModeNonInteractive -eq $true) { $Silent = $true }
	
	If ($CloseApps -ne "") {
		# Create a Process object with custom descriptions where they are provided (split on a "=" sign)
		$processObjects = @()
		Foreach ($process in ($CloseApps -split(",") | Where { $_ -ne ""})) { # Split multiple processes on a comma and join with the regex operator '|' to perform "or" match against multiple applications 
			$process = $process -split("=")	 
			$processObjects += New-Object PSObject -Property @{
				ProcessName =		   $process[0]
				ProcessDescription =	$process[1]  
			}
		}
	}
	# Check Deferral history and calculate deferrals remaining
	If ($allowDefer -eq $true) {
		$deferHistory = Get-DeferHistory
		$deferHistoryTimes = $deferHistory | Select DeferTimesRemaining -ExpandProperty DeferTimesRemaining -ErrorAction SilentlyContinue
		$deferHistoryDeadline = $deferHistory | Select DeferDeadline -ExpandProperty DeferDeadline -ErrorAction SilentlyContinue  
		# Reset Switches 
		$checkDeferDays = $checkDeferDeadline = $false
		If ($DeferDays) {$checkDeferDays = $true}
		If ($DeferDeadline) {$checkDeferDeadline = $true} 
		If ($DeferTimes) {
			If ($deferHistoryTimes -ge 0) {
				Write-Log "Defer history shows [$($deferHistory.DeferTimesRemaining)] deferrals remaining."
				$DeferTimes = $deferHistory.DeferTimesRemaining -1
			}
			Else { 
				$DeferTimes = $DeferTimes -1
			}
			Write-Log "User now has [$deferTimes] deferrals remaining."
			If ($DeferTimes -lt 0)  {
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
	If (!($deferTimes) -and !($deferDeadlineUniversal)) {
		$AllowDefer = $false
	}

	# Prompt the user to close running applications and optionally defer if enabled
	If (!($deployModeSilent) -and !($silent)) { 
		While ((Get-RunningProcesses $processObjects | Select * -OutVariable RunningProcesses) -or ($promptResult -ne "Defer")) {
			$runningProcessDescriptions	= ($runningProcesses | Select Description -ExpandProperty Description | Select -Unique | Sort) -join "," 
            # Prompt the user to close running processes with deferral option
			If ($allowDefer -and ((!($promptResult -eq "Close")) -or ($runningProcessDescriptions -ne "" -and $promptResult -ne "Continue"))) { 
				$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdown -AllowDefer -DeferTimes $deferTimes -DeferDeadline $deferDeadlineUniversal	
			}
			# Prompt the user to close running processes with no deferral option
			ElseIf ($runningProcessDescriptions -ne "") {
				$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdown   
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
				Stop-Process ($runningProcesses | Select ID -ExpandProperty ID) -Force -ErrorAction SilentlyContinue
				Sleep -Seconds 2
			}
			# Force the application to close (not actioned within a reasonable amount of time)
			ElseIf ($promptResult -eq "Timeout") {
				Write-Log "Installation not actioned within a reasonable amount of time."
				$BlockExecution = $false
				If ($deferTimes -or $deferDeadlineUniversal) {
					Set-DeferHistory -deferTimesRemaining $DeferTimes -deferDeadline $deferDeadlineUniversal
				}
				Exit-Script 1618
			}
			# Force the application to close (user chose to defer)
			ElseIf ($promptResult -eq "Defer") {
				Write-Log "Installation deferred by the user."
				$BlockExecution = $false
				Set-DeferHistory -deferTimesRemaining $DeferTimes -deferDeadline $deferDeadlineUniversal
				# Restore minimized windows
				$shellApp.UndoMinimizeAll()
				Exit-Script 1618
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
.PARAMETER AllowDefer
	Specify whether to provide an option to defer the installation
.PARAMETER DeferTimes
	Specify the number of times the user is allowed to defer
.PARAMETER DeferDeadline
	Specify the deadline date before the user is allowed to defer
.NOTES
	This is an internal script function and should typically not be called directly. It is used by the Show-InstallationWelcome prompt to display a custom prompt.
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
	[string] $ProcessDescriptions = $null,
	[int] $CloseAppsCountdown = $null,
	[switch] $AllowDefer = $false,
	$DeferTimes = $null,
	$DeferDeadline = $null
	)
	# Reset switches
	$showCloseApps = $showDefer = $false 
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
		# Convert the deadline date to a string   
		If ($DeferDeadline) { 
			[string]$DeferDeadline = Get-Date $DeferDeadline | Out-String -Stream
		}
	}
	ElseIf ($CloseAppsCountdown -gt 0) {
		Write-Log "Displaying alose applications countdown with [$CloseAppsCountdown] seconds."
		$showCountdown = $true
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

	$Form_Cleanup_FormClosed=
	{
		# Remove all event handlers from the controls
		Try
		{
			$labelAppName.remove_Click($handler_labelAppName_Click)
			$labelDefer.remove_Click($handler_labelDefer_Click)
			$buttonCloseApps.remove_Click($buttonCloseApps_OnClick)
			$buttonContinue.remove_Click($buttonContinue_OnClick)
			$buttonDefer.remove_Click($buttonDefer_OnClick)
			$buttonAbort.remove_Click($buttonAbort_OnClick)
			$timer.remove_Tick($timer_Tick)
			$formWelcome.remove_Load($Form_StateCorrection_Load)
			$formWelcome.remove_FormClosed($Form_Cleanup_FormClosed)
		}
		Catch [Exception]
		{ }
	}

	$Form_StateCorrection_Load=
	{
		#Correct the initial state of the form to prevent the .Net maximized form issue
		$formWelcome.WindowState = 'Normal'
		$formWelcome.AutoSize = $true
		$formWelcome.TopMost = $true
		$formWelcome.BringToFront()

		# Initialize the countdown timer
		$currentTime = Get-Date
		$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
		$timer.Start()
		# Set up the form
		$remainingTime = $countdownTime.Subtract($currentTime)
		$labelCountdownSeconds = [String]::Format("{0}:{1:d2}:{2:d2}", $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
		$labelCountdown.Text = "$configClosePromptCountdownMessage`n$labelCountdownSeconds"		
	}

	# Timer
	$timer = New-Object 'System.Windows.Forms.Timer'
	If ($showCountdown -eq $true) {
		$timer_Tick={
			# Get the time information
			$currentTime = Get-Date
			$countdownTime = $startTime.AddSeconds($CloseAppsCountdown )
			$remainingTime = $countdownTime.Subtract($currentTime)
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
		$timer.Interval = $configInstallationUITimeout
		$timer_Tick={
			$buttonAbort.PerformClick()
		}
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
		$System_Drawing_Size.Height = 30
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
		$listboxCloseApps.Items.Add("$processDescription")
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
		$deferralText = "$deferralText `n$configDeferPromptRemainingDeferrals $deferTimes"
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

	# Add the Timer Countdown
	$timer.add_Tick($timer_Tick)

	# Save the initial state of the form
	$formWelcomeWindowState = $formWelcome.WindowState
	# Init the OnLoad event to correct the initial state of the form
	$formWelcome.add_Load($Form_StateCorrection_Load)
	# Clean up the control events
	$formWelcome.add_FormClosed($Form_Cleanup_FormClosed)

	# Start the timer
	$timer.Start()

	# Minimize all other windows
	$shellApp.MinimizeAll()

	# Show the form
	$result = $formWelcome.ShowDialog()  

	Switch ($result) {
        OK { $result = "Continue" }
        No { $result = "Defer" }
        Yes { $result = "Close" }
        Abort { $result = "Timeout" }
    }

    Return $result
       
} #End Function

Function Show-InstallationRestartPrompt {
<# 
.SYNOPSIS
	Displays a restart prompt with a countdown to a forced restart.
.DESCRIPTION
	Displays a restart prompt with a countdown to a forced restart.
.EXAMPLE
	Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60
.PARAMETER CountdownSeconds
	Specifies the number of seconds to countdown to the system restart.
.PARAMETER CountdownNoHideSeconds
	Specifies the number of seconds to display the restart prompt without allowing the window to be hidden.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[int] $CountdownSeconds = 60,
		[int] $CountdownNoHideSeconds = 30
	)

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

	Function Show-RestartPopup {
		# Show the Restart Popup
		$formRestart.WindowState = 'Normal'
		$formRestart.TopMost = $true
		$formRestart.TopMost = $false
		$formRestart.BringToFront()
		[System.Windows.Forms.Application]::DoEvents()
	}

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
		# Show Popup
		Show-RestartPopup
	}
	

	$buttonRestartLater_Click={
		# Minimize the form
		$formRestart.WindowState = 'Minimized'
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
					Show-RestartPopup
				}
			}
			[System.Windows.Forms.Application]::DoEvents()
		}
	}

	$Form_StateCorrection_Load=
	{
		# Correct the initial state of the form to prevent the .Net maximized form issue
		$formRestart.WindowState = $InitialFormWindowState
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
			$formRestart.remove_Load($Form_StateCorrection_Load)
			$formRestart.remove_FormClosed($Form_Cleanup_FormClosed)
		}
		Catch [Exception]
		{ }
	}

	# Form
	$formRestart.Controls.Add($labelCountdown)
	$formRestart.Controls.Add($labelTimeRemaining)
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
	$formRestart.Text = "$configRestartPromptTitle"
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
	$labelMessage.Text = $configRestartPromptMessage
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
	$buttonRestartNow.Name = $configRestartPromptButtonRestartNow
	$buttonRestartNow.Size = '159, 23'
	$buttonRestartNow.TabIndex = 0
	$buttonRestartNow.Text = "Restart &Now"
	$buttonRestartNow.UseVisualStyleBackColor = $True
	$buttonRestartNow.add_Click($buttonRestartNow_Click)

	# Timer Countdown
	$timerCountdown.add_Tick($timerCountdown_Tick)

	#----------------------------------------------

	# Save the initial state of the form
	$InitialFormWindowState = $formRestart.WindowState
	# Init the OnLoad event to correct the initial state of the form
	$formRestart.add_Load($Form_StateCorrection_Load)
	# Clean up the control events
	$formRestart.add_FormClosed($Form_Cleanup_FormClosed)


    # If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously   
	If ($deployAppScriptFriendlyName) {
        Write-Log "Invoking Show-InstallationRestartPrompt asynchronously with [$countDownSeconds] countdown seconds..."
        $installationRestartPromptJob = [PowerShell]::Create().AddScript({
            Param($scriptPath,$installRestartPromptParameters)
            .$scriptPath        
            Show-InstallationRestartPrompt @installRestartPromptParameters
        }).AddArgument($scriptPath).AddArgument($installRestartPromptParameters)
        # Show the form asynchronously
        $installationRestartPromptJobResult = $installationRestartPromptJob.BeginInvoke()
    }
    Else {	
	    Write-Log "Displaying restart prompt with [$countDownSeconds] countdown seconds."   
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
			Register-ObjectEvent $NotifyIcon -EventName BalloonTipClicked -Action {$sender.Visible = $False; $NotifyIcon.Dispose(); Unregister-Event $EventSubscriber.SourceIdentifier; Remove-Job $EventSubscriber.Action;  $sender.Dispose();} | Out-Null
			# Register a click event with action to take based on event for balloon message closed
			Register-ObjectEvent $NotifyIcon -EventName BalloonTipClosed  -Action {$sender.Visible = $False; $NotifyIcon.Dispose(); Unregister-Event $EventSubscriber.SourceIdentifier; Remove-Job $EventSubscriber.Action; $sender.Dispose()} | Out-Null
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
		[string] $StatusMessage = $configProgressMessage
	)
	If ($deployModeSilent -eq $true) { 
		Return 
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
			# Select the screen heigth and width   
			$screenWidth = $screenBounds | Select Width -ExpandProperty Width
			$screenHeight = $screenBounds | Select Height -ExpandProperty Height
			# Set the start position of the Window based on the screen size
			$xamlProgress.Window.Left =  [string](($screenWidth / 2) - ($xamlProgress.Window.Width /2))
			$xamlProgress.Window.Top = [string]($screenHeight / 10)
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
		$verb = $verb.Replace("&","")
		$path = Split-Path $FilePath -ErrorAction SilentlyContinue
		$folder = $shellApp.Namespace($path)
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[Parameter(Mandatory = $true)]
		[String] $FilePath,
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
		If ($ContinueOnError -eq $true) {
			Write-Log "File [$filePath] could not be found."
			Continue
		}
		Else {
			Throw "File [$filePath] could not be found."
		}
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
.PARAMETER ContinueOnError
	Continue if an error is encountered
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com 
#>
	Param (
		[Parameter(Mandatory = $true)]
		[string] $FilePath,
		[Parameter(Mandatory = $true)]
		[string] $Key,
		[string] $Value,
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
		If ($ContinueOnError -eq $true) {
			Write-Log "File [$filePath] could not be found."
			Continue
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
	)

	Write-Log "Registering DLL file [$filePath]..."   

	If (Test-Path $FilePath ) {
		Execute-Process "regsvr32.exe" -Arguments "/s '$FilePath'" -WindowStyle Hidden -PassThru
	}
	Else {
		If ($ContinueOnError -eq $true) {
			Write-Log "File [$filePath] could not be found."
			Continue
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
	)

	Write-Log "Unregistering DLL file [$filePath]..." 

	If (Test-Path $FilePath ) {
		Try {
			Execute-Process "regsvr32.exe" -Arguments "/s /u '$FilePath'" -WindowStyle Hidden -PassThru
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
			Continue
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
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
	Catch {
		If ($ContinueOnError -eq $true) {
			Write-Log "Trigger SCCM Schedule failed for Schedule ID $($ScheduleIds.$ScheduleId)"
			Continue
		}
		Else {
			Throw "Trigger SCCM Schedule failed for Schedule ID $($ScheduleIds.$ScheduleId)"
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
		[switch] $ContinueOnError = $Global:ContinueOnErrorGlobalPreference
	)

	# Scan for updates
	Write-Log "Scanning for SCCM Software Updates..."
	Invoke-SCCMTask -ScheduleId "SoftwareUpdatesScan"

	Write-Log "Sleeping 180 seconds..."
	Sleep -Seconds 180

	Try {
		Write-Log "Installing pending software updates..."
		$SmsSoftwareUpdates = [wmiclass]"ROOT\ccm:SMS_Client"
		$SmsSoftwareUpdates.InstallUpdates([System.Management.ManagementObject[]] (Get-WmiObject -Query “SELECT * FROM CCM_SoftwareUpdate” -Namespace “ROOT\ccm\ClientSDK”)) | Out-Null
	}
	Catch {
		If ($ContinueOnError -eq $true) {
			Write-Log "Trigger SCCM Install Updates failed"
			Continue
		}
		Else {
			Throw "Trigger SCCM Install Updates failed"
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
	$installName = $ReferringApplication
	Write-Log "$appDeployMainScriptFriendlyName called with switch CleanupBlockedApps"
	Unblock-AppExecution
	Exit-Script -ExitCode 0
}

# If the showBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog -eq $true) {
	Try {
		$deployModeSilent = $true
		$installName = $ReferringApplication
		Write-Log "$appDeployMainScriptFriendlyName called with switch ShowBlockedAppDialog"
		Show-InstallationPrompt -Title $ReferringApplication -Message $configBlockExecutionMessage -Icon Warning
		Exit-Script -ExitCode 0
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

# Check for errors or warnings loading assemblies.
If ($AssemblyError -ne $null) {
	Write-Log "Errors detected loading assemblies."
	$AssemblyError | Where { $_.Exception.Message -ne $null } | % { Write-Log "$($_.Exception.Message) $($_.ScriptStackTrace)"}
}
If ($AssemblyWarning -ne $null) {  
	Write-Log "Warnings detected loading assemblies."
}

# Evaluate non-default parameters passed to the scripts
If ($deployAppScriptParameters) { $deployAppScriptParameters = $deployAppScriptParameters.GetEnumerator() | % { "($($_.Key)=$($_.Value))" } }
If ($appDeployMainScriptParameters) { $appDeployMainScriptParameters = $appDeployMainScriptParameters.GetEnumerator() | % { "($($_.Key)=$($_.Value))" } }
If ($appDeployExtScriptParameters) { $appDeployExtScriptParameters = $appDeployExtScriptParameters.GetEnumerator() | % { "($($_.Key)=$($_.Value))" } }

Write-Log "$installName setup started."
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

