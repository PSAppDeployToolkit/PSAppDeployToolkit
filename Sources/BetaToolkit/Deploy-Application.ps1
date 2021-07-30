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
	Changes to "user install mode" and back to "user execute mode" for installing/uninstalling applications for Remote Desktop Session Hosts/Citrix servers.
.EXAMPLE
	Deploy-Application.ps1
.EXAMPLE
	Deploy-Application.ps1 -DeployMode 'Silent'
.EXAMPLE
	Deploy-Application.ps1 -AllowRebootPassThru -AllowDefer
.EXAMPLE
	Deploy-Application.ps1 -DeploymentType Uninstall
.EXAMPLE
	psexec.exe -s "powershell.exe" "-ExecutionPolicy" "ByPass" "-Command" "&{ & 'E:\Testing\Deploy-Application.ps1' -DeploymentType Install; Exit $LastExitCode }"
	Use psexec.exe to launch script in the SYSTEM context for testing purposes.
.EXAMPLE
	psexec.exe -s "E:\Testing\Deploy-Application.exe"
	Use psexec.exe to launch script in the SYSTEM context for testing purposes.
.NOTES
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
	[switch]$TerminalServerMode = $false
)

Try {
	## Set the script execution policy for this process
	Try { Set-ExecutionPolicy -ExecutionPolicy 'ByPass' -Scope 'Process' -Force -ErrorAction 'Stop' } Catch {}
	
	##*===============================================
	##* VARIABLE DECLARATION
	##*===============================================
	
	## Variables: Application
	[scriptblock]$Variables_Application = {
		[string]$appVendor = 'PSAppDeployToolkit'
		[string]$appName = 'Test Script'
		[string]$appVersion = '1.0'
		[string]$appArch = ''
		[string]$appLang = 'EN'
		[string]$appRevision = '01'
		[string]$appScriptVersion = '3.5.0'
		[string]$appScriptDate = '11/03/2014'
		[string]$appScriptAuthor = 'Dan Cunningham'
	}
	.$Variables_Application
	
	## Variables: All Script Parameters (add all custom script parameters here if they need to be passed to toolkit when relaunched as user)
	[scriptblock]$Variables_AllScriptParams = {
		[string]$DeploymentType = $DeploymentType
		[string]$DeployMode = $DeployMode
		[switch]$AllowRebootPassThru = $AllowRebootPassThru
		[switch]$TerminalServerMode = $TerminalServerMode
	}
	
	##*===============================================
	##* Do not modify section below
	#region DoNotModify
	
	## Variables: Exit Code
	[int32]$mainExitCode = 0
	
	## Variables: Script
	$script:mainPSBoundParams = $PSBoundParameters
	[scriptblock]$Variables_Script = {
		#  Script Release/Version Info
		[string]$deployAppScriptFriendlyName = 'Deploy Application'
		[version]$deployAppScriptVersion = [version]'4.0.0'
		[string]$deployAppScriptDate = '11/12/2014'
		#  Script Bound Parameters
		[hashtable]$deployAppScriptParameters = $script:mainPSBoundParams
	}
	.$Variables_Script
	
	## Dot source the required App Deploy Toolkit Functions
	Try {
		[string]$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
		[string]$moduleAppDeployToolkitMain = "$scriptDirectory\AppDeployToolkit\AppDeployToolkitMain.ps1"
		If (-not (Test-Path -Path $moduleAppDeployToolkitMain -PathType Leaf)) { Throw "Module does not exist at the specified location [$moduleAppDeployToolkitMain]." }
		. $moduleAppDeployToolkitMain
	}
	Catch {
		[int32]$mainExitCode = 1
		Write-Error -Message "Module [$moduleAppDeployToolkitMain] failed to load: `n$($_.Exception.Message)`n `n$($_.InvocationInfo.PositionMessage)" -ErrorAction 'Continue'
		Exit $mainExitCode
	}
	
	#endregion
	##* Do not modify section above
	##*===============================================
	
	##*===============================================
	##* END VARIABLE DECLARATION
	##*===============================================
	
	If ($deploymentType -ine 'Uninstall') {
		##*===============================================
		##* PRE-INSTALLATION
		##*===============================================
		[scriptblock]$Variables_InstallPhase = { [string]$installPhase = 'Pre-Installation' }; .$Variables_InstallPhase
		
		## Installation Welcome: Defer test
		Show-InstallationWelcome -AllowDefer -DeferTimes 100
		
		
		## Installation Welcome: CloseApps, CloseAppsCountdown, Defer, CheckDiskspace, PersistentPrompt and BlockExecution test
		Show-InstallationWelcome -CloseApps 'iexplore' -CloseAppsCountdown 60 -CheckDiskSpace -PersistPrompt -BlockExecution
		
		
		##*===============================================
		##* INSTALLATION
		##*===============================================
		[scriptblock]$Variables_InstallPhase = { [string]$installPhase = 'Installation' }; .$Variables_InstallPhase
		
		## Progress Message and Block Execution Test
		Show-InstallationProgress -StatusMessage 'BlockExecution Test: Open Internet Explorer or an Office application within 10 seconds...'
		Start-Sleep -Seconds 10
		
		## MSI Installation and Removal Test
		Show-InstallationProgress -StatusMessage 'MSI Installation And Removal Test...'
		Execute-MSI -Action Install -Path 'PSAppDeployToolkit_TestInstallation_1.0.0_EN_01.msi'
		Remove-MSIApplications -Name 'Test Installation (Testing) [Testing]'
		
		## x86 File Manipulation and DLL Registration Test
		Show-InstallationProgress -StatusMessage 'x86 File Manipulation And DLL Registration Test...'
		Copy-File -Path "$dirSupportFiles\AutoItX3.dll" -Destination "$envWinDir\SysWOW64\AutoItx3.dll"
		Register-DLL -FilePath "$envWinDir\SysWOW64\AutoItx3.dll"
		Unregister-DLL -FilePath "$envWinDir\SysWOW64\AutoItx3.dll"
		Remove-File -Path "$envWinDir\SysWOW64\AutoItx3.dll"
		
		## x64 File Manipulation and DLL Registration Test
		Show-InstallationProgress -StatusMessage 'x64 File Manipulation And DLL Registration Test...'
		Copy-File -Path "$dirSupportFiles\AutoItX3_x64.dll" -Destination "$envWinDir\System32\AutoItx3.dll"
		Register-DLL -FilePath "$envWinDir\System32\AutoItx3.dll"
		Unregister-DLL -FilePath "$envWinDir\System32\AutoItx3.dll"
		Remove-File -Path "$envWinDir\System32\AutoItx3.dll"
		
		## Create Shortcut Test
		Show-InstallationProgress -StatusMessage 'Shortcut Creation Test...'
		New-Shortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk" -TargetPath "$envWinDir\system32\notepad.exe" -IconLocation "$envWinDir\system32\notepad.exe" -Description 'Notepad' -WorkingDirectory "$envHomeDrive\$envHomePath"
		
		## Pin to Start Menu Test
		Show-InstallationProgress -StatusMessage 'Pinned Application Test...'
		Set-PinnedApplication -Action 'PintoStartMenu' -FilePath "$envWinDir\Notepad.exe"
		Set-PinnedApplication -Action 'PintoTaskBar' -FilePath "$envWinDir\Notepad.exe"
		
		
		##*===============================================
		##* POST-INSTALLATION
		##*===============================================
		[scriptblock]$Variables_InstallPhase = { [string]$installPhase = 'Post-Installation' }; .$Variables_InstallPhase
		
		## Execute Process test
		Show-InstallationProgress -StatusMessage 'Execute Process Test: Close Notepad to proceed...'
		If (-not $IsProcessUserInteractive) {
			Invoke-PSCommandAsUser -Command { Execute-Process -FilePath 'Notepad' }
		}
		Else {
			Execute-Process -FilePath 'Notepad'
		}
		
		## Installation Prompt With NoWait Test
		Show-InstallationPrompt -Message 'Asynchronous Installation Prompt Test: The installation should complete in the background. Click OK to dismiss...' -ButtonRightText 'OK' -Icon 'Information' -NoWait
		Start-Sleep -Seconds 10
		
		## Remove Shortcut
		Remove-File -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"
		
		## Unpin From Start Menu
		Set-PinnedApplication -Action 'UnPinFromStartMenu' -FilePath "$envWinDir\Notepad.exe"
		Set-PinnedApplication -Action 'UnPinFromTaskBar' -FilePath "$envWinDir\Notepad.exe"
		
		## Installation Restart Prompt Test
#		Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60
	}
	ElseIf ($deploymentType -ieq 'Uninstall') {
		##*===============================================
		##* PRE-UNINSTALLATION
		##*===============================================
		[scriptblock]$Variables_InstallPhase = { [string]$installPhase = 'Post-Uninstallation' }; .$Variables_InstallPhase
		
		## Installation Welcome: CloseApps and CloseAppsCountdown Test
		Show-InstallationWelcome -CloseApps 'iexplore' -CloseAppsCountdown 60
		
		
		##*===============================================
		##* UNINSTALLATION
		##*===============================================
		[scriptblock]$Variables_InstallPhase = { [string]$installPhase = 'Uninstallation' }; .$Variables_InstallPhase
		
		## MSI Removal Test
		Show-InstallationProgress -StatusMessage 'MSI Uninstallation Test...'
		Execute-MSI -Action Uninstall -Path 'PSAppDeployToolkit_TestInstallation_1.0.0_EN_01.msi'
		
		
		##*===============================================
		##* POST-UNINSTALLATION
		##*===============================================
		[scriptblock]$Variables_InstallPhase = { [string]$installPhase = 'Post-Uninstallation' }; .$Variables_InstallPhase
		
		## Perform post-uninstallation tasks here
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
	Show-DialogBox -Text $mainErrorMessage -Icon 'Stop' | Out-Null
	Exit-Script -ExitCode $mainExitCode
}
