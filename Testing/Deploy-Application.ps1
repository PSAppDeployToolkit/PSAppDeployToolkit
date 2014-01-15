<#
.SYNOPSIS
	This script performs the installation or uninstallation of an application(s).  
.DESCRIPTION
	The script is provided as a template to perform an install or uninstall of an application(s). 
	The script either performs an "Install" deployment type or an "Uninstall" deployment type.
	The install deployment type is broken down in to 3 main sections/phases: Pre-Install, Install, and Post-Install.
	The script dot-sources the AppDeployToolkitMain.ps1 script which contains the logic and functions required to install or uninstall an application.
	To access the help section,
.EXAMPLE
	Deploy-Application.ps1
.EXAMPLE
	Deploy-Application.ps1 -DeploymentMode "Silent"
.EXAMPLE
	Deploy-Application.ps1 -AllowRebootPassThru -AllowDefer
.EXAMPLE
	Deploy-Application.ps1 -Uninstall 
.PARAMETER DeploymentType
	The type of deployment to perform. [Default is "Install"]
.PARAMETER DeployMode
	Specifies whether the installation should be run in Interactive, Silent or NonInteractive mode.
	Interactive = Default mode
	Silent = No dialogs
	NonInteractive = Very silent, i.e. no blocking apps. Noninteractive mode is automatically set if an SCCM task sequence or session 0 is detected.
.PARAMETER AllowRebootPassThru
	Allows the 3010 return code (requires restart) to be passed back to the parent process (e.g. SCCM) if detected from an installation. 
	If 3010 is passed back to SCCM a reboot prompt will be triggered.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com
"#>
Param (
	[ValidateSet("Install","Uninstall")] 
	[string] $DeploymentType = "Install",
	[ValidateSet("Interactive","Silent","NonInteractive")]
	[string] $DeployMode = "Interactive",
	[switch] $AllowRebootPassThru = $false
)

#*===============================================
#* VARIABLE DECLARATION
Try {
#*===============================================

#*===============================================
# Variables: Application

$appVendor = "PSAppDeployToolkit"
$appName = "Test Script"
$appVersion = "1.0"
$appArch = ""
$appLang = "EN"
$appRevision = "01"
$appScriptVersion = "1.0.0"
$appScriptDate = "11/29/2013"
$appScriptAuthor = "Dan Cunningham"

#*===============================================
# Variables: Script - Do not modify this section

$deployAppScriptFriendlyName = "Deploy Application"
$deployAppScriptVersion = "3.0.9"
$deployAppScriptDate = "11/28/2013"
$deployAppScriptParameters = $psBoundParameters

# Variables: Environment
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
# Dot source the App Deploy Toolkit Functions
."$scriptDirectory\AppDeployToolkit\AppDeployToolkitMain.ps1"

#*===============================================
#* END VARIABLE DECLARATION
#*===============================================

#*===============================================
#* PRE-INSTALLATION
If ($deploymentType -ne "uninstall") { $installPhase = "Pre-Installation"
#*===============================================

	# Installation Welcome: Defer test
	Show-InstallationWelcome -AllowDefer -DeferTimes 100

	# Installation Welcome: CloseApps, CloseAppsCountdown, Defer, CheckDiskspace, PersistentPrompt and BlockExecution test
	Show-InstallationWelcome -CloseApps "iexplore,winword,excel,powerpnt" -CloseAppsCountdown "60" -CheckDiskSpace -PersistPrompt -BlockExecution

#*===============================================
#* INSTALLATION 
$installPhase = "Installation"
#*===============================================

	# Progress message and Block Execution test
	Show-InstallationProgress "BlockExecution test: Open Internet Explorer or an Office application within 10 seconds..."
	Sleep -Seconds 10

	# MSI installation and removal test
	Show-InstallationProgress "MSI installation and removal test..."
	Execute-MSI -Action Install -Path "PSAppDeployToolkit_TestInstallation_1.0.0_EN_01.msi"
	Remove-MSIApplications "PSAppDeployToolkit Test Installation"

	# x86 file manipulation and DLL Registration test
	Show-InstallationProgress "x86 file manipulation and DLL Registration test..."
	Copy-File -Path "$dirSupportFiles\AutoItX3.dll" -Destination "$envWinDir\SysWOW64\AutoItx3.dll"
	Register-DLL "$envWinDir\SysWOW64\AutoItx3.dll"
	Unregister-DLL "$envWinDir\SysWOW64\AutoItx3.dll"
	Remove-File -Path "$envWinDir\SysWOW64\AutoItx3.dll"

	# x64 file manipulation and DLL registration test
	Show-InstallationProgress "x64 file manipulation and DLL Registration test..."
	Copy-File -Path "$dirSupportFiles\AutoItX3_x64.dll" -Destination "$envWinDir\System32\AutoItx3.dll"
	Register-DLL "$envWinDir\System32\AutoItx3.dll"
	Unregister-DLL "$envWinDir\System32\AutoItx3.dll"
	Remove-File -Path "$envWinDir\System32\AutoItx3.dll"

    # Create Shortcut test
	Show-InstallationProgress "Shortcut Creation Test..."
    New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\system32\notepad.exe" -IconLocation "$envWinDir\system32\notepad.exe" -Description "Notepad" -WorkingDirectory "$envHomeDrive\$envHomePath"

	# Pin to Start Menu test
	Show-InstallationProgress "Pinned Application test..."
	Set-PinnedApplication -Action "PintoStartMenu" -FilePath "$envWinDir\Notepad.exe"
	Set-PinnedApplication -Action "PintoTaskBar" -FilePath "$envWinDir\Notepad.exe"
    

#*===============================================
#* POST-INSTALLATION
$installPhase = "Post-Installation"
#*===============================================

	# Execute Process test
	Show-InstallationProgress "Execute Process test. Close Notepad to proceed..."
	Execute-Process "Notepad"

	# Installation Prompt with NoWait test
	Show-InstallationPrompt -Message "Asynchronous installation prompt test. The installation should complete in the background. Click Ok to dismiss..." -ButtonRightText "Ok" -Icon Information -NoWait
	Sleep -Seconds 10

    # Remove Shortcut
    Remove-File -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"

    # Unpin from Start Menu
	Set-PinnedApplication -Action "UnPinFromStartMenu" -FilePath "$envWinDir\Notepad.exe"
	Set-PinnedApplication -Action "UnPinFromTaskBar" -FilePath "$envWinDir\Notepad.exe"

	# Installation Restart Prompt test
	# Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60

#*===============================================
#* UNINSTALLATION
} ElseIf ($deploymentType -eq "uninstall") { $installPhase = "Uninstallation"
#*===============================================

	# Installation Welcome: CloseApps and CloseAppsCountdown test
	Show-InstallationWelcome -CloseApps "iexplore,winword,excel,powerpnt" -CloseAppsCountdown "60"

	# MSI removal test
	Show-InstallationProgress "MSI uninstallation test..."
	Execute-MSI -Action Uninstall -Path "PSAppDeployToolkit_TestInstallation_1.0.0_EN_01.msi"


#*===============================================
#* END SCRIPT BODY
} } Catch {$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)"; Write-Log "$exceptionMessage"; Show-DialogBox -Text $exceptionMessage -Icon "Stop"; Exit-Script -ExitCode 1} # Catch any errors in this script 
Exit-Script -ExitCode 0 # Otherwise call the Exit-Script function to perform final cleanup operations
#*===============================================