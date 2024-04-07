Function Exit-Script {
	<#
.SYNOPSIS

Exit the script, perform cleanup actions, and pass an exit code to the parent process.

.DESCRIPTION

Always use when exiting the script to ensure cleanup actions are performed.

.PARAMETER ExitCode

The exit code to be passed from the script to the parent process, e.g. SCCM

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Exit-Script

.EXAMPLE

Exit-Script -ExitCode 1618

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Int32]$ExitCode = 0
	)

	## Get the name of this function
	[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

	## Close the Installation Progress Dialog if running
	Close-InstallationProgress

	## If block execution variable is true, call the function to unblock execution
	If ($BlockExecution) {
		Unblock-AppExecution
	}

	## If Terminal Server mode was set, turn it off
	If ($terminalServerMode) {
		Disable-TerminalServerInstallMode
	}

	## Determine action based on exit code
	Switch ($exitCode) {
		$configInstallationUIExitCode {
			$installSuccess = $false
		}
		$configInstallationDeferExitCode {
			$installSuccess = $false
		}
		3010 {
			$installSuccess = $true
		}
		1641 {
			$installSuccess = $true
		}
		0 {
			$installSuccess = $true
		}
		Default {
			$installSuccess = $false
		}
	}

	## Determine if balloon notification should be shown
	If ($deployModeSilent) {
		[Boolean]$configShowBalloonNotifications = $false
	}

	If ($installSuccess) {
		If (Test-Path -LiteralPath $regKeyDeferHistory -ErrorAction 'SilentlyContinue') {
			Write-Log -Message 'Removing deferral history...' -Source ${CmdletName}
			Remove-RegistryKey -Key $regKeyDeferHistory -Recurse
		}

		[String]$balloonText = "$deploymentTypeName $configBalloonTextComplete"
		## Handle reboot prompts on successful script completion
		If (($AllowRebootPassThru) -and ((($msiRebootDetected) -or ($exitCode -eq 3010)) -or ($exitCode -eq 1641))) {
			Write-Log -Message 'A restart has been flagged as required.' -Source ${CmdletName}
			[String]$balloonText = "$deploymentTypeName $configBalloonTextRestartRequired"
			If (($msiRebootDetected) -and ($exitCode -ne 1641)) {
				[Int32]$exitCode = 3010
			}
		} Else {
			[Int32]$exitCode = 0
		}

		Write-Log -Message "$installName $deploymentTypeName completed with exit code [$exitcode]." -Source ${CmdletName}
		If ($configShowBalloonNotifications) {
			Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText $balloonText -NoWait
		}
	} ElseIf (-not $installSuccess) {
		Write-Log -Message "$installName $deploymentTypeName completed with exit code [$exitcode]." -Source ${CmdletName}
		If (($exitCode -eq $configInstallationUIExitCode) -or ($exitCode -eq $configInstallationDeferExitCode)) {
			[String]$balloonText = "$deploymentTypeName $configBalloonTextFastRetry"
			If ($configShowBalloonNotifications) {
				Show-BalloonTip -BalloonTipIcon 'Warning' -BalloonTipText $balloonText -NoWait
			}
		} Else {
			[String]$balloonText = "$deploymentTypeName $configBalloonTextError"
			If ($configShowBalloonNotifications) {
				Show-BalloonTip -BalloonTipIcon 'Error' -BalloonTipText $balloonText -NoWait
			}
		}
	}

	[String]$LogDash = '-' * 79
	Write-Log -Message $LogDash -Source ${CmdletName}

	## Archive the log files to zip format and then delete the temporary logs folder
	If ($configToolkitCompressLogs) {
		## Disable logging to file so that we can archive the log files
		. $DisableScriptLogging

		Try {
			# Get all archive files sorted by last write time
			$ArchiveFiles = Get-ChildItem -LiteralPath $configToolkitLogDir -Filter ($installName + '_' + $deploymentType + '_*.zip') | Sort-Object LastWriteTime

			# Keep only the max number of archive files
			if ($ArchiveFiles.Count -gt $configToolkitLogMaxHistory) {
				$ArchiveFiles | Select-Object -First ($ArchiveFiles.Count - $configToolkitLogMaxHistory) | Remove-Item -ErrorAction 'Stop'
			}

			[String]$DestinationArchiveFileName = $installName + '_' + $deploymentType + '_' + ((Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').ToString()) + '.zip'
			New-ZipFile -DestinationArchiveDirectoryPath $configToolkitLogDir -DestinationArchiveFileName $DestinationArchiveFileName -SourceDirectory $logTempFolder -RemoveSourceAfterArchiving
		} Catch {
			Write-Host -Object "[$LogDate $LogTime] [${CmdletName}] $ScriptSection :: Failed to manage archive file [$DestinationArchiveFileName]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
		}

	}

	If (Test-Path -LiteralPath 'variable:notifyIcon') {
		Try {
			$script:notifyIcon.Dispose()
		} Catch {
		}
	}
	## Reset powershell window title to its previous title
	$Host.UI.RawUI.WindowTitle = $oldPSWindowTitle
	## Reset variables in case another toolkit is being run in the same session
	$global:logName = $null
	$global:installTitle = $null
	$global:installName = $null
	$global:appName = $null
	## Exit the script, returning the exit code to SCCM
	If (Test-Path -LiteralPath 'variable:HostInvocation') {
		$script:ExitCode = $exitCode; Exit
	} Else {
		Exit $exitCode
	}
}
