#region Function Unblock-AppExecution
Function Unblock-AppExecution {
<#
.SYNOPSIS
	Unblocks the execution of applications performed by the Block-AppExecution function
.DESCRIPTION
	This function is called by the Exit-Script function or when the script itself is called with the parameters -CleanupBlockedApps
.EXAMPLE
	Unblock-AppExecution
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
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Bypass if no Admin rights
		If ($configToolkitRequireAdmin -eq $false) {
			Write-Log -Message "Bypassing Function [${CmdletName}], because [Require Admin: $configToolkitRequireAdmin]." -Source ${CmdletName}
			Return
		}
		## Bypass if in NonInteractive mode
		If ($deployModeNonInteractive) {
			Write-Log -Message "Bypassing Function [${CmdletName}], because [Mode: $deployMode]." -Source ${CmdletName}
			Return
		}

		## Remove Debugger values to unblock processes
		[psobject[]]$unblockProcesses = $null
		[psobject[]]$unblockProcesses += (Get-ChildItem -LiteralPath $regKeyAppExecution -Recurse -ErrorAction 'SilentlyContinue' | ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'SilentlyContinue'})
		ForEach ($unblockProcess in ($unblockProcesses | Where-Object { $_.Debugger -like '*AppDeployToolkit_BlockAppExecutionMessage*' })) {
			Write-Log -Message "Removing the Image File Execution Options registry key to unblock execution of [$($unblockProcess.PSChildName)]." -Source ${CmdletName}
			$unblockProcess | Remove-ItemProperty -Name 'Debugger' -ErrorAction 'SilentlyContinue'
		}

		## If block execution variable is $true, set it to $false
		If ($BlockExecution) {
			#  Make this variable globally available so we can check whether we need to call Unblock-AppExecution
			Set-Variable -Name 'BlockExecution' -Value $false -Scope 'Script'
		}

		## Remove the scheduled task if it exists
		[string]$schTaskBlockedAppsName = $installName + '_BlockedApps'
		Try {
			If (Get-SchedulerTask -ContinueOnError $true | ForEach-Object { if($_.TaskName -eq "\$schTaskBlockedAppsName") {$_.TaskName} }) {
				Write-Log -Message "Deleting Scheduled Task [$schTaskBlockedAppsName]." -Source ${CmdletName}
				Execute-Process -Path $exeSchTasks -Parameters "/Delete /TN $schTaskBlockedAppsName /F"
			}
		}
		Catch {
			Write-Log -Message "Error retrieving/deleting Scheduled Task.`r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}

		## Remove BlockAppExecution Schedule Task XML file
		[string]$xmlSchTaskFilePath = "$dirAppDeployTemp\SchTaskUnBlockApps.xml"
		If (Test-Path -LiteralPath $xmlSchTaskFilePath) {
			Remove-Item -Path $xmlSchTaskFilePath
		}

		## Remove BlockAppExection Temporary directory
		[string]$blockExecutionTempPath = Join-Path -Path $dirAppDeployTemp -ChildPath 'BlockExecution'
		If (Test-Path -LiteralPath $blockExecutionTempPath -PathType 'Container') {
			Remove-Folder -Path $blockExecutionTempPath
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
