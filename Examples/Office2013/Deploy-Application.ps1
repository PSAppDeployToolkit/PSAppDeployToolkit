<#
.SYNOPSIS
	This script performs the installation or uninstallation of an application(s).
.DESCRIPTION
	The script is provided as a template to perform an install or uninstall of an application(s).
	The script either performs an "Install" deployment type or an "Uninstall" deployment type.
	The install deployment type is broken down into 3 main sections/phases: Pre-Install, Install, and Post-Install.
	The script dot-sources the AppDeployToolkitMain.ps1 script which contains the logic and functions required to install or uninstall an application.
.PARAMETER DeploymentType
	The type of deployment to perform. Default is: Install.
.PARAMETER DeployMode
	Specifies whether the installation should be run in Interactive, Silent, or NonInteractive mode. Default is: Interactive. Options: Interactive = Shows dialogs, Silent = No dialogs, NonInteractive = Very silent, i.e. no blocking apps. NonInteractive mode is automatically set if it is detected that the process is not user interactive.
.PARAMETER AllowRebootPassThru
	Allows the 3010 return code (requires restart) to be passed back to the parent process (e.g. SCCM) if detected from an installation. If 3010 is passed back to SCCM, a reboot prompt will be triggered.
.PARAMETER TerminalServerMode
	Changes to "user install mode" and back to "user execute mode" for installing/uninstalling applications for Remote Destkop Session Hosts/Citrix servers.
.PARAMETER DisableLogging
	Disables logging to file for the script. Default is: $false.
.EXAMPLE
	Deploy-Application.ps1
.EXAMPLE
	Deploy-Application.ps1 -DeployMode 'Silent'
.EXAMPLE
	Deploy-Application.ps1 -AllowRebootPassThru -AllowDefer
.EXAMPLE
	Deploy-Application.ps1 -DeploymentType Uninstall
.NOTES
	Toolkit Exit Code Ranges:
	60000 - 68999: Reserved for built-in exit codes in Deploy-Application.ps1, Deploy-Application.exe, and AppDeployToolkitMain.ps1
	69000 - 69999: Recommended for user customized exit codes in Deploy-Application.ps1
	70000 - 79999: Recommended for user customized exit codes in AppDeployToolkitExtensions.ps1
.LINK
	http://psappdeploytoolkit.com
#>
[CmdletBinding()]
Param (
	[Parameter(Mandatory=$false)]
	[ValidateSet('Install','Uninstall')]
	[string]$DeploymentType = 'Install',
	[Parameter(Mandatory=$false)]
	[ValidateSet('Interactive','Silent','NonInteractive')]
	[string]$DeployMode = 'Interactive',
	[Parameter(Mandatory=$false)]
	[switch]$AllowRebootPassThru = $false,
	[Parameter(Mandatory=$false)]
	[switch]$TerminalServerMode = $false,
	[Parameter(Mandatory=$false)]
	[switch]$DisableLogging = $false,
	[switch]$addComponentsOnly = $false, # Specify whether running in Component Only Mode
	[switch]$addInfoPath = $false, # Add InfoPath to the install
	[switch]$addOneNote = $false, # Add OneNote to the install
	[switch]$addOutlook = $false, # Add Outlook to the install
	[switch]$addPublisher = $false, # Add Publisher to the install
	[switch]$addSharepointWorkspace = $false # Add Sharepoint Workspace to the install
)

Try {
	## Set the script execution policy for this process
	Try { Set-ExecutionPolicy -ExecutionPolicy 'ByPass' -Scope 'Process' -Force -ErrorAction 'Stop' } Catch {}
	
	##*===============================================
	##* VARIABLE DECLARATION
	##*===============================================
	## Variables: Application
	[string]$appVendor = 'Microsoft'
	[string]$appName = 'Office'
	[string]$appVersion = '2013 SP1'
	[string]$appArch = 'x86'
	[string]$appLang = 'EN'
	[string]$appRevision = '01'
	[string]$appScriptVersion = '3.6.8'
	[string]$appScriptDate = '11/22/2015'
	[string]$appScriptAuthor = 'Dan Cunningham'
	##*===============================================
	
	##* Do not modify section below
	#region DoNotModify
	
	## Variables: Exit Code
	[int32]$mainExitCode = 0
	
	## Variables: Script
	[string]$deployAppScriptFriendlyName = 'Deploy Application'
	[version]$deployAppScriptVersion = [version]'3.6.5'
	[string]$deployAppScriptDate = '08/17/2015'
	[hashtable]$deployAppScriptParameters = $psBoundParameters
	
	## Variables: Environment
	If (Test-Path -LiteralPath 'variable:HostInvocation') { $InvocationInfo = $HostInvocation } Else { $InvocationInfo = $MyInvocation }
	[string]$scriptDirectory = Split-Path -Path $InvocationInfo.MyCommand.Definition -Parent
	
	## Dot source the required App Deploy Toolkit Functions
	Try {
		[string]$moduleAppDeployToolkitMain = "$scriptDirectory\AppDeployToolkit\AppDeployToolkitMain.ps1"
		If (-not (Test-Path -LiteralPath $moduleAppDeployToolkitMain -PathType 'Leaf')) { Throw "Module does not exist at the specified location [$moduleAppDeployToolkitMain]." }
		If ($DisableLogging) { . $moduleAppDeployToolkitMain -DisableLogging } Else { . $moduleAppDeployToolkitMain }
	}
	Catch {
		If ($mainExitCode -eq 0){ [int32]$mainExitCode = 60008 }
		Write-Error -Message "Module [$moduleAppDeployToolkitMain] failed to load: `n$($_.Exception.Message)`n `n$($_.InvocationInfo.PositionMessage)" -ErrorAction 'Continue'
		## Exit the script, returning the exit code to SCCM
		If (Test-Path -LiteralPath 'variable:HostInvocation') { $script:ExitCode = $mainExitCode; Exit } Else { Exit $mainExitCode }
	}
	
	#endregion
	##* Do not modify section above
	##*===============================================
	##* END VARIABLE DECLARATION
	##*===============================================
	
	#  Set the initial Office folder
	[string] $dirOffice = Join-Path -Path "$envProgramFilesX86" -ChildPath 'Microsoft Office'
	
	If ($deploymentType -ine 'Uninstall') {
		##*===============================================
		##* PRE-INSTALLATION
		##*===============================================
		[string]$installPhase = 'Pre-Installation'
		
		## Check whether running in Add Components Only mode
		If ($addComponentsOnly) {
			#  Verify that components were specified on the command-line
			If ((-not $addInfoPath) -and (-not $addSharepointWorkspace) -and (-not $addOneNote) -and (-not $addOutlook) -and (-not $addPublisher)) {
				Show-InstallationPrompt -Message 'No addon components were specified' -ButtonRightText 'OK' -Icon 'Error'
				Exit-Script -ExitCode 9
			}
			
			#  Verify that Office 2013 is already installed
			$officeVersion = Get-ItemProperty -LiteralPath 'HKLM:SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{90150000-0011-0000-0000-0000000FF1CE}' -ErrorAction 'SilentlyContinue' | Select-Object -ExpandProperty DisplayName
			
			#  If not found, display an error and exit
			If (-not $officeVersion) {
				Show-InstallationPrompt -Message 'Unable to add the requested components as Office 2013 is not currently installed' -ButtonRightText 'OK' -Icon 'Error'
			}
		}
		
		## Show Welcome Message, close Internet Explorer if required, allow up to 3 deferrals, and verify there is enough disk space to complete the install
		Show-InstallationWelcome -CloseApps "excel,groove,onenote,infopath,onenote,outlook,mspub,powerpnt,winword,winproj,visio" -AllowDefer -DeferTimes 3 -CheckDiskSpace
		
		## Check whether anything might prevent us from running the cleanup
		If (($addComponentsOnly) -or ($isServerOS)) {
			Write-Log -Message "Installation of components has been skipped as one of the following options are enabled. addComponentsOnly: $addComponentsOnly isServerOS: $isServerOS" -Source $deployAppScriptFriendlyName
		}
		Else {
			## Check for components and make sure we reinstall them during the upgrade if necessary
			[string[]]$officeFolders = 'Office12', 'Office13', 'Office14', 'Office15'
			ForEach ($officeFolder in $officeFolders) {
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "$officeFolder\groove.exe") -PathType 'Leaf') {
					Write-Log -Message 'Sharepoint Workspace / Groove was previously installed. Will be reinstalled' -Source $deployAppScriptFriendlyName
					$addSharepointWorkspace = $true
				}
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "$officeFolder\infopath.exe") -PathType 'Leaf') {
					Write-Log -Message 'InfoPath was previously installed. Will be reinstalled' -Source $deployAppScriptFriendlyName
					$addInfoPath = $true
				}
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "$officeFolder\onenote.exe") -PathType 'Leaf') {
					Write-Log -Message 'OneNote was previously installed. Will be reinstalled' -Source $deployAppScriptFriendlyName
					$addOneNote = $true
				}
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "$officeFolder\outlook.exe") -PathType 'Leaf') {
					Write-Log -Message 'Outlook was previously installed. Will be reinstalled' -Source $deployAppScriptFriendlyName
					$addOutlook = $true
				}
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "$officeFolder\mspub.exe") -PathType 'Leaf') {
					Write-Log -Message 'Publisher was previously installed. Will be reinstalled' -Source $deployAppScriptFriendlyName
					$addPublisher = $true
				}
			}
			
			## Display Pre-Install cleanup status
			Show-InstallationProgress -StatusMessage 'Performing Pre-Install cleanup. This may take some time. Please wait...'
			
			# Remove any previous version of Office (if required)
			[string[]]$officeExecutables = 'excel.exe', 'groove.exe', 'infopath.exe', 'onenote.exe', 'outlook.exe', 'mspub.exe', 'powerpnt.exe', 'winword.exe', 'winproj.exe', 'visio.exe'
			ForEach ($officeExecutable in $officeExecutables) {
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "Office12\$officeExecutable") -PathType 'Leaf') {
					Write-Log -Message 'Microsoft Office 2007 was detected. Will be uninstalled.' -Source $deployAppScriptFriendlyName
					Execute-Process -Path 'cscript.exe' -Parameters "`"$dirSupportFiles\OffScrub07.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes '1,2,3'
					Break
				}
			}
			ForEach ($officeExecutable in $officeExecutables) {
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "Office14\$officeExecutable") -PathType 'Leaf') {
					Write-Log -Message 'Microsoft Office 2010 was detected. Will be uninstalled.' -Source $deployAppScriptFriendlyName
					Execute-Process -Path "cscript.exe" -Parameters "`"$dirSupportFiles\OffScrub10.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes '1,2,3'
					Break
				}
			}
			ForEach ($officeExecutable in $officeExecutables) {
				If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath "Office15\$officeExecutable") -PathType 'Leaf') {
					Write-Log -Message 'Microsoft Office 2013 was detected. Will be uninstalled.' -Source $deployAppScriptFriendlyName
					Execute-Process -Path "cscript.exe" -Parameters "`"$dirSupportFiles\OffScrub13.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes '1,2,3'
					Break
				}
			}
		}
		
		
		##*===============================================
		##* INSTALLATION
		##*===============================================
		[string]$installPhase = 'Installation'
		
		## Check whether running in Add Components Only mode
		If (-not $addComponentsOnly) {
	  		Show-InstallationProgress -StatusMessage 'Installing Office Professional. This may take some time. Please wait...'
			Execute-Process -Path "$dirFiles\Setup.exe" -Parameters "/adminfile `"$dirFiles\Config\Office2013ProPlus.MSP`" /config `"$dirFiles\ProPlus.WW\Config.xml`"" -WindowStyle Hidden -IgnoreExitCodes '3010'
		}
		
		# Install InfoPath if required
		If ($addInfoPath) {
			Show-InstallationProgress -StatusMessage 'Installing Office Infopath. This may take some time. Please wait...'
			Execute-Process -Path "$dirFiles\Setup.exe" -Parameters "/modify ProPlus /config `"$dirSupportFiles\AddInfoPath.xml`"" -WindowStyle Hidden
		}
		
		# Install Sharepoint Designer if required
		If ($addSharepointWorkspace) {
			Show-InstallationProgress -StatusMessage 'Installing Office Sharepoint Workspace. This may take some time. Please wait...'
			Execute-Process -Path "$dirFiles\Setup.exe" -Parameters "/modify ProPlus /config `"$dirSupportFiles\AddSharePointWorkspace.xml`"" -WindowStyle Hidden
		}
		
		# Install OneNote if required
		If ($addOneNote) {
			Show-InstallationProgress -StatusMessage "Installing Office OneNote. This may take some time. Please wait..."
			Execute-Process -Path "$dirFiles\Setup.exe" -Parameters "/modify ProPlus /config `"$dirSupportFiles\AddOneNote.xml`"" -WindowStyle Hidden
		}
		
		# Install Outlook if required
		If ($addOutlook) {
			Show-InstallationProgress -StatusMessage 'Installing Office Outlook. This may take some time. Please wait...'
			Execute-Process -Path "$dirFiles\Setup.exe" -Parameters "/modify ProPlus /config `"$dirSupportFiles\AddOutlook.xml`"" -WindowStyle Hidden
		}
		
		# Install Publisher if required
		If ($addPublisher) {
			Show-InstallationProgress -StatusMessage 'Installing Office Publisher. This may take some time. Please wait...'
			Execute-Process -Path "$dirFiles\Setup.exe" -Parameters "/modify ProPlus /config `"$dirSupportFiles\AddPublisher.xml`"" -WindowStyle Hidden
		}
		
		
		##*===============================================
		##* POST-INSTALLATION
		##*===============================================
		[string]$installPhase = 'Post-Installation'
		
		# Activate Office components (if running as a user)
		If ($CurrentLoggedOnUserSession -or $CurrentConsoleUserSession -or $RunAsActiveUser) {
			If (Test-Path -LiteralPath (Join-Path -Path $dirOffice -ChildPath 'Office15\OSPP.VBS') -PathType 'Leaf') {
				Show-InstallationProgress -StatusMessage 'Activating Microsoft Office components. This may take some time. Please wait...'
				Execute-Process -Path 'cscript.exe' -Parameters "`"$dirOffice\Office15\OSPP.VBS`" /ACT" -WindowStyle Hidden
			}
		}
		
		# Prompt for a restart (if running as a user, not installing components and not running on a server)
		If ((-not $addComponentsOnly) -and ($deployMode -eq 'Interactive') -and (-not $IsServerOS)) {
			Show-InstallationRestartPrompt
		}
	}
	ElseIf ($deploymentType -ieq 'Uninstall')
	{
		##*===============================================
		##* PRE-UNINSTALLATION
		##*===============================================
		[string]$installPhase = 'Pre-Uninstallation'
		
		## Show Welcome Message, close applications that cause uninstall to fail
		Show-InstallationWelcome -CloseApps 'excel,groove,infopath,onenote,outlook,mspub,powerpnt,winword,winproj,visio'
		
		## Show Progress Message (with the default message)
		Show-InstallationProgress
		
		
		##*===============================================
		##* UNINSTALLATION
		##*===============================================
		[string]$installPhase = 'Uninstallation'
		
		Execute-Process -Path "cscript.exe" -Parameters "`"$dirSupportFiles\OffScrub13.vbs`" ClientAll /S /Q /NoCancel" -WindowStyle Hidden -IgnoreExitCodes '1,2,3'
		
		
		##*===============================================
		##* POST-UNINSTALLATION
		##*===============================================
		[string]$installPhase = 'Post-Uninstallation'
		
		## <Perform Post-Uninstallation tasks here>
	}

	##*===============================================
	##* END SCRIPT BODY
	##*===============================================

	## Call the Exit-Script function to perform final cleanup operations
	Exit-Script -ExitCode $mainExitCode
}
Catch {
	[int32]$mainExitCode = 1
	[string]$mainErrorMessage = "$(Resolve-Error)"
	Write-Log -Message $mainErrorMessage -Severity 3 -Source $deployAppScriptFriendlyName
	Show-DialogBox -Text $mainErrorMessage -Icon 'Stop'
	Exit-Script -ExitCode $mainExitCode
}
