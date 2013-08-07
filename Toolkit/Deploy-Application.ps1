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
	Deploy-Application.ps1 -DeploymentType "Silent"
.EXAMPLE
	Deploy-Application.ps1 -AllowSCCMReboot -AllowDefer
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
	[string]$DeploymentType = "Install", # Default deployment type is Install	 
	[ValidateSet("Interactive","Silent","NonInteractive")]
	[string]$DeployMode = "Interactive",  # Interactive=Default,Silent=No dialogs,NonInteractive=VerySilent,no blocking apps(automatically set in SCCM task sequence or session 0).
	[switch]$AllowRebootPassThru = $false # Allow the 3010 return code to be passed back to the parent process, e.g. if passed back to SCCM a reboot prompt will be triggered.
)

#*===============================================
#* VARIABLE DECLARATION
Try {
#*===============================================

#*===============================================
# Variables: Application

$appVendor = ""
$appName = ""
$appVersion = ""
$appArch = ""
$appLang = "EN"
$appRevision = "01"
$appScriptVersion = "1.0.0"
$appScriptDate = "01/01/2013"
$appScriptAuthor = "<author name>"

#*===============================================
# Variables: Script - Do not modify this section

$deployAppScriptFriendlyName = "Deploy Application"
$deployAppScriptVersion = "2.0.0"
$deployAppScriptDate = "08/07/2013"

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

	# Show Progress Message (with the default message)
	Show-InstallationProgress 


#*===============================================
#* INSTALLATION 
$installPhase = "Installation"
#*===============================================


#*===============================================
#* POST-INSTALLATION
$installPhase = "Post-Installation"
#*===============================================


#*===============================================
#* UNINSTALLATION
} ElseIf ($deploymentType -eq "uninstall") { $installPhase = "Uninstallation"
#*===============================================

	# Show Progress Message (with the default message)
	Show-InstallationProgress


#*===============================================
#* END SCRIPT BODY
} } Catch {$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)"; Write-Log "$exceptionMessage"; Show-DialogBox -Text $exceptionMessage -Icon "Stop"; Exit-Script -ExitCode 1} # Catch any errors in this script 
Exit-Script -ExitCode 0 # Otherwise call the Exit-Script function to perform final cleanup operations
#*===============================================