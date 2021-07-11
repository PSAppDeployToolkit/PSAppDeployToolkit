Function Block-AppExecution {
<#
.SYNOPSIS
	Blocks the execution of one or multiple applications
.DESCRIPTION
	This function is called when you pass the -BlockExecution parameter to the Stop-RunningApplications function. It does the following:
	1. Makes a copy of this script in a temporary directory on the local machine.
	2. Checks for an existing scheduled task from previous failed installation attempt where apps were blocked and if found, calls the Unblock-AppExecution function to restore the original IFEO registry keys.
	   This is to prevent the function from overriding the backup of the original IFEO options.
	3. Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameter -CleanupBlockedApps.
	4. Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameter -ShowBlockedAppDialog.
	5. When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress.
	   The text of this message can be customized in the XML configuration file.
.PARAMETER ProcessNames
	array of process names 
.EXAMPLE
	Block-AppExecution -ProcessNames ('winword','excel')
.NOTES
	This is an internal script function and should typically not be called directly.
	It is used when the -BlockExecution parameter is specified with the Show-InstallationWelcome function to block applications.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string[]]$ProcessNames
	)

	Begin {
		## Get the name of this function and write header
		$CmdletName = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName $CmdletName -CmdletBoundParameters $PSBoundParameters -Header

		## Remove illegal characters from the scheduled task arguments string
		$InvalidScheduledTaskChars = "$!'`"();\``*?{}[]<>|&%#~@ ".ToCharArray()
		$SchInstallName = $installName

		ForEach ($invalidChar in $InvalidScheduledTaskChars) { 
			$SchInstallName = $SchInstallName -replace [regex]::Escape($invalidChar),'' 
		}

		$blockExecutionTempPath = Join-Path -Path $dirAppDeployTemp -ChildPath 'BlockExecution'
		$schTaskUnblockAppsCommand = @(
			"-ExecutionPolicy Bypass",
			"-NoProfile",
			"-NoLogo",
			"-WindowStyle Hidden",
			"-File '$blockExecutionTempPath\$scriptFileName'",
			"-CleanupBlockedApps",
			"-ReferredInstallName '$SchInstallName'",
			"-ReferredInstallTitle '$installTitle'",
			"-ReferredLogName '$logName'",
			"-AsyncToolkitLaunch"
		)
		
		## Specify the scheduled task configuration in XML format
		$xmlUnblockAppsSchTask = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
	<RegistrationInfo></RegistrationInfo>
	<Triggers>
		<BootTrigger>
			<Enabled>true</Enabled>
		</BootTrigger>
	</Triggers>
	<Principals>
		<Principal id="Author">
			<UserId>S-1-5-18</UserId>
		</Principal>
	</Principals>
	<Settings>
		<MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
		<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
		<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
		<AllowHardTerminate>true</AllowHardTerminate>
		<StartWhenAvailable>false</StartWhenAvailable>
		<RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
		<IdleSettings>
			<StopOnIdleEnd>false</StopOnIdleEnd>
			<RestartOnIdle>false</RestartOnIdle>
		</IdleSettings>
		<AllowStartOnDemand>true</AllowStartOnDemand>
		<Enabled>true</Enabled>
		<Hidden>false</Hidden>
		<RunOnlyIfIdle>false</RunOnlyIfIdle>
		<WakeToRun>false</WakeToRun>
		<ExecutionTimeLimit>PT1H</ExecutionTimeLimit>
		<Priority>7</Priority>
	</Settings>
	<Actions Context="Author">
		<Exec>
			<Command>$PSHome\powershell.exe</Command>
			<Arguments>$schTaskUnblockAppsCommand</Arguments>
		</Exec>
	</Actions>
</Task>
"@
	}

	Process {
		## Bypass if no Admin rights
		If (-not $configToolkitRequireAdmin) {
			Write-Log -Message "Bypassing Function [$CmdletName], because [Require Admin: $configToolkitRequireAdmin]." -Source $CmdletName
			Return
		}

		## Bypass if in NonInteractive mode
		If ($deployModeNonInteractive) {
			Write-Log -Message "Bypassing Function [$CmdletName], because [Mode: $deployMode]." -Source $CmdletName
			Return
		}

		$schTaskBlockedAppsName = "${installName}_BlockedApps"

		## Delete this file if it exists as it can cause failures (it is a bug from an older version of the toolkit)
		If (Test-Path -LiteralPath "$configToolkitTempPath\PSAppDeployToolkit" -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
			Remove-Item -LiteralPath "$configToolkitTempPath\PSAppDeployToolkit" -Force -ErrorAction 'SilentlyContinue'
		}

		If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
			Remove-Folder -Path $blockExecutionTempPath
		}

		Try {
			New-Item -Path $blockExecutionTempPath -ItemType 'Directory' -ErrorAction 'Stop'
		} Catch {
			Write-Log -Message "Unable to create [$blockExecutionTempPath]. Possible attempt to gain elevated rights." -Source ${CmdletName}
		}

		Copy-Item -Path "$scriptRoot\*.*" -Destination $blockExecutionTempPath -Exclude 'thumbs.db' -Force -Recurse -ErrorAction 'SilentlyContinue'

		## Build the debugger block value script
		$debuggerBlockScript = @(
			"strCommand = `"$PSHome\powershell.exe -ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `" & chr(34) & `"$blockExecutionTempPath\$scriptFileName`" & chr(34) & `" -ShowBlockedAppDialog -AsyncToolkitLaunch -ReferredInstallTitle `" & chr(34) & `"$installTitle`" & chr(34)",
			'set oWShell = CreateObject("WScript.Shell")',
			'oWShell.Run strCommand, 0, false'
		)

		$debuggerBlockScript | Out-File -FilePath "$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs" -Force -Encoding 'default' -ErrorAction 'SilentlyContinue'

		$debuggerBlockValue = "$envWinDir\System32\wscript.exe `"$blockExecutionTempPath\AppDeployToolkit_BlockAppExecutionMessage.vbs`""

		## Set contents to be readable for all users (BUILTIN\USERS)
		try {
			$Users = ConvertTo-NTAccountOrSID -SID "S-1-5-32-545"
			Set-ItemPermission -Path $blockExecutionTempPath -User $Users -Permission 'Read' -Inheritance "ObjectInherit","ContainerInherit"
		} catch {
			Write-Log -Message "Failed to set read permissions on path [$blockExecutionTempPath]. The function might not be able to work correctly." -Source ${CmdletName} -Severity 2
		}
			
		## Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
		Write-Log -Message 'Creating scheduled task to cleanup blocked applications in case the installation is interrupted.' -Source ${CmdletName}

		$ScheduledTasks = (Get-SchedulerTask -ContinueOnError $True)

		ForEach ($ScheduledTask In $ScheduledTasks) {
			If ($SchedulerTask.TaskName -eq "\$schTaskBlockedAppsName") {
				Write-Log -Message "Scheduled task [$schTaskBlockedAppsName] already exists." -Source ${CmdletName}
				$TaskFound = $True
				Break
			}
		}

		If (-not $TaskFound) {
			## Export the scheduled task XML to file
			Try {
				## Specify the filename to export the XML to
				## XML does not need to be user readable to stays in protected TEMP folder
				$xmlSchTaskFilePath = "$dirAppDeployTemp\SchTaskUnBlockApps.xml"
				$xmlUnblockAppsSchTask | Out-File -FilePath $xmlSchTaskFilePath -Force -ErrorAction 'Stop'
			}
			Catch {
				Write-Log -Message "Failed to export the scheduled task XML file [$xmlSchTaskFilePath]. `r`n$(Resolve-Error)" -Severity 3 -Source $CmdletName
				Return
			}

			## Import the Scheduled Task XML file to create the Scheduled Task
			$schTaskResultSplat = @(
				Path 				 = $exeSchTasks
				Parameters 			 = "/create /f /tn $schTaskBlockedAppsName /xml `"$xmlSchTaskFilePath`""
				WindowStyle          = "Hidden"
				CreateNoWindow       = $True
				PassThru             = $True
				ExitOnProcessFailure = $False
			)

			$schTaskResult = (Execute-Process @schTaskResultSplat)

			If ($schTaskResult.ExitCode -ne 0) {
				Write-Log -Message "Failed to create the scheduled task [$schTaskBlockedAppsName] by importing the scheduled task XML file [$xmlSchTaskFilePath]." -Severity 3 -Source $CmdletName
				Return
			}
		}

		## Append .exe to match registry keys
		ForEach ($processName In $ProcessNames) {
			$blockProcessName = "$processName.exe"

			Write-Log -Message "Setting the Image File Execution Option registry key to block execution of [$blockProcessName]." -Source $CmdletName

			$SetRegistryKeySplat = @(
				Key             = (Join-Path -Path $regKeyAppExecution -ChildPath $blockProcessName)
				Name            = 'Debugger'
				Value           = $debuggerBlockValue
				ContinueOnError = $true
			)
			Set-RegistryKey @SetRegistryKeySplat
		}
	}
	
	End {
		Write-FunctionInfo -CmdletName $CmdletName -Footer
	}
}