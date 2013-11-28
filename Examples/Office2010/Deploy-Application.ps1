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
	[switch] $AllowRebootPassThru = $false,
	[string]$addComponentsOnly = $false, # Specify whether running in Component Only Mode
	[string]$addInfoPath = $false, # Add InfoPath to the install
	[string]$addOneNote = $false, # Add OneNote to the install
	[string]$addOutlook = $false, # Add Outlook to the install
	[string]$addPublisher = $false, # Add Publisher to the install
	[string]$addSharepointWorkspace = $false # Add Sharepoint Workspace to the install
)

#*===============================================
#* VARIABLE DECLARATION
Try {
#*===============================================

#*===============================================
# Variables: Application

$appVendor = "Microsoft"
$appName = "Office"
$appVersion = "2010 SP2"
$appArch = "x86"
$appLang = "EN"
$appRevision = "01"
$appScriptVersion = "2.0.1"
$appScriptDate = "11/28/2013"
$appScriptAuthor = "Dan Cunningham"

#*===============================================
# Variables: Script - Do not modify this section

$deployAppScriptFriendlyName = "Deploy Application"
$deployAppScriptVersion = "3.0.6"
$deployAppScriptDate = "10/10/2013"
$deployAppScriptParameters = $psBoundParameters

# Variables: Environment
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
# Dot source the App Deploy Toolkit Functions
."$scriptDirectory\AppDeployToolkit\AppDeployToolkitMain.ps1"

# Office Directory
$dirOffice = Join-Path "$envProgramFilesX86" "Microsoft Office"

#*===============================================
#* END VARIABLE DECLARATION
#*===============================================

#*===============================================
#* PRE-INSTALLATION
If ($deploymentType -ne "uninstall") { $installPhase = "Pre-Installation"
#*===============================================

	# Check whether running in Add Components Only mode
	If ($addComponentsOnly -eq $true) {
		# Verify that components were specified on the command-line
		If ($addInfoPath -eq $false -and $addSharepointWorkspace -eq $false -and $addOneNote -eq $false -and $addOutlook -eq $false -and $addPublisher -eq $false) {
			Show-InstallationPrompt -message "No addon components were specified" -ButtonRightText "OK" -Icon "Error"
			Exit-Script 9
		}
		
		# Verify that Office 2010 is already installed
		$officeVersion = Get-ItemProperty 'HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{90140000-0011-0000-0000-0000000FF1CE}' -ErrorAction SilentlyContinue | Select DisplayName -ExpandProperty DisplayName
		
		# If not found, display an error and exit
		If ($officeVersion -eq $null) {
			Show-InstallationPrompt -message "Unable to add the requested components as Office 2010 is not currently installed" -ButtonRightText "OK" -Icon "Error"
		}
	}

	# Show Welcome Message, close Internet Explorer if required, allow up to 3 deferrals, and verify there is enough disk space to complete the install
	Show-InstallationWelcome -CloseApps "excel,groove,onenote,infopath,onenote,outlook,mspub,powerpnt,winword,winproj,visio" -AllowDefer -DeferTimes 3 -CheckDiskSpace

	# Check whether anything might prevent us from running the cleanup
	If (($addComponentsOnly -eq $true) -or ($isServerOS -eq $true)) {
		Write-Log "Installation of components has been skipped as one of the following options are enabled. addComponentsOnly: $addComponentsOnly isServerOS: $isServerOS"
	}
	Else {
		# Check for components and make sure we reinstall them during the upgrade if necessary
		$officeFolders = @("Office12", "Office13", "Office14", "Office15")
		ForEach ($officeFolder in $officeFolders) {
			If (Test-Path (Join-Path $dirOffice "$officeFolder\Groove.Exe")) { 
				Write-Log "Sharepoint Workspace / Groove was previously installed. Will be reinstalled"
				$addSharepointWorkspace = $true 
			}
			If (Test-Path (Join-Path $dirOffice "$officeFolder\Infopath.Exe")) { 
				Write-Log "InfoPath was previously installed. Will be reinstalled"
				$addInfoPath = $true 
			}
			If (Test-Path (Join-Path $dirOffice "$officeFolder\OneNote.Exe")) { 
				Write-Log "OneNote was previously installed. Will be reinstalled"
				$addOneNote = $true
			}
			If (Test-Path (Join-Path $dirOffice "$officeFolder\Outlook.Exe")) { 
				Write-Log "Outlook was previously installed. Will be reinstalled"
				$addOutlook = $true
			}
			If (Test-Path (Join-Path $dirOffice "$officeFolder\MSPub.Exe")) { 
				Write-Log "Publisher was previously installed. Will be reinstalled"
				$addOutlook = $true
			}
		}
		
		# Display Pre-Install cleanup status
		Show-InstallationProgress "Performing Pre-Install cleanup. This may take some time. Please wait..."

		# Remove any previous version of Office (if required)
		$officeExecutables = @("excel.exe", "groove.exe", "onenote.exe", "infopath.exe", "onenote.exe", "outlook.exe", "mspub.exe", "powerpnt.exe", "winword.exe", "winproj.exe" ,"visio.exe")
		ForEach ($officeExecutable in $officeExecutables) {
			If (Test-Path (Join-Path $dirOffice "Office12\$officeExecutable")) { 
				Write-Log "Microsoft Office 2007 was detected. Will be uninstalled."
				Execute-Process -FilePath "CScript.Exe" -Arguments "`"$dirSupportFiles\OffScrub07.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes "1,2,3"
				Break
			}
		}
		ForEach ($officeExecutable in $officeExecutables) {
			If (Test-Path (Join-Path $dirOffice "Office14\$officeExecutable")) { 
				Write-Log "Microsoft Office 2010 was detected. Will be uninstalled."
				Execute-Process -FilePath "CScript.Exe" -Arguments "`"$dirSupportFiles\OffScrub10.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes "1,2,3"
				Break
			}
		}
		ForEach ($officeExecutable in $officeExecutables) {
			If (Test-Path (Join-Path $dirOffice "Office15\$officeExecutable")) { 
				Write-Log "Microsoft Office 2013 was detected. Will be uninstalled."
				Execute-Process -FilePath "CScript.Exe" -Arguments "`"$dirSupportFiles\OffScrub13.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes "1,2,3"
				Break
			}
		}
	}

#*===============================================
#* INSTALLATION 
$installPhase = "Installation"
#*===============================================

	# Check whether running in Add Components Only mode
	If ($addComponentsOnly -eq $false) {
  		Show-InstallationProgress "Installing Office Professional. This may take some time. Please wait..."
		Execute-Process -FilePath "$dirFiles\Office\Setup.exe" -Arguments "/adminfile `"$dirFiles\Config\Office2010ProPlus.MSP`" /config `"$dirFiles\ProPlus.WW\Config.xml`"" -WindowStyle Hidden -IgnoreExitCodes "3010"
	}

	# Install InfoPath if required
	If ($addInfoPath -eq $true) {
		Show-InstallationProgress "Installing Office Infopath. This may take some time. Please wait..."
		Execute-Process -FilePath "$dirFiles\Setup.exe" -Arguments "/modify ProPlus /config `"$dirSupportFiles\AddInfoPath.xml`"" -WindowStyle Hidden	
	}

	# Install Sharepoint Designer if required
	If ($addSharepointWorkspace -eq $true) {
		Show-InstallationProgress "Installing Office Sharepoint Workspace. This may take some time. Please wait..."
		Execute-Process -FilePath "$dirFiles\Setup.exe" -Arguments "/modify ProPlus /config `"$dirSupportFiles\AddSharePointWorkspace.xml`"" -WindowStyle Hidden	
	}

	# Install OneNote if required
	If ($addOneNote -eq $true) {
		Show-InstallationProgress "Installing Office OneNote. This may take some time. Please wait..."
		Execute-Process -FilePath "$dirFiles\Setup.exe" -Arguments "/modify ProPlus /config `"$dirSupportFiles\AddOneNote.xml`"" -WindowStyle Hidden	
	}

	# Install Outlook if required
	If ($addOutlook -eq $true) {
		Show-InstallationProgress "Installing Office Outlook. This may take some time. Please wait..."
		Execute-Process -FilePath "$dirFiles\Setup.exe" -Arguments "/modify ProPlus /config `"$dirSupportFiles\AddOutlook.xml`"" -WindowStyle Hidden	
	}

	# Install Publisher if required
	If ($addPublisher -eq $true) {
		Show-InstallationProgress "Installing Office Publisher. This may take some time. Please wait..."
		Execute-Process -FilePath "$dirFiles\Setup.exe" -Arguments "/modify ProPlus /config `"$dirSupportFiles\AddPublisher.xml`"" -WindowStyle Hidden	
	}

#*===============================================
#* POST-INSTALLATION
$installPhase = "Post-Installation"
#*===============================================

	# Activate Office components (if running as a user)
	If ($osdMode -eq $false) {
		If (Test-Path (Join-Path $dirOffice "Office14\OSPP.VBS")) { 
			Show-InstallationProgress "Activating Microsoft Office components. This may take some time. Please wait..."
			Execute-Process -FilePath "CScript.Exe" -Arguments "`"$dirOffice\Office14\OSPP.VBS`" /ACT" -WindowStyle Hidden	
		}
	}

	# Prompt for a restart (if running as a user, not installing components and not running on a server)
	If (($addComponentsOnly -eq $false) -and ($deployMode -eq "Interactive") -and ($IsServerOS -eq $false)) {
		Show-InstallationRestartPrompt
	}

#*===============================================
#* UNINSTALLATION
} ElseIf ($deploymentType -eq "uninstall") { $installPhase = "Uninstallation"
#*===============================================

	# Show Welcome Message, close applications if required with a 60 second countdown before automatically closing
	Show-InstallationWelcome -CloseApps "excel,groove,onenote,infopath,onenote,outlook,mspub,powerpnt,winword,winproj,visio"

	# Show Progress Message (with the default message)
	Show-InstallationProgress

	Execute-Process -FilePath "CScript.Exe" -Arguments "`"$dirSupportFiles\OffScrub10.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes "1,2,3"

#*===============================================
#* END SCRIPT BODY
} } Catch {$exceptionMessage = "$($_.Exception.Message) `($($_.ScriptStackTrace)`)"; Write-Log "$exceptionMessage"; Show-DialogBox -Text $exceptionMessage -Icon "Stop"; Exit-Script -ExitCode 1} # Catch any errors in this script 
Exit-Script -ExitCode 0 # Otherwise call the Exit-Script function to perform final cleanup operations
#*===============================================