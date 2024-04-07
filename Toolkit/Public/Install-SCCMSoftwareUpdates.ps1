﻿Function Install-SCCMSoftwareUpdates {
	<#
.SYNOPSIS

Scans for outstanding SCCM updates to be installed and installs the pending updates.

.DESCRIPTION

Scans for outstanding SCCM updates to be installed and installs the pending updates.

Only compatible with SCCM 2012 Client or higher. This function can take several minutes to run.

.PARAMETER SoftwareUpdatesScanWaitInSeconds

The amount of time to wait in seconds for the software updates scan to complete. Default is: 180 seconds.

.PARAMETER WaitForPendingUpdatesTimeout

The amount of time to wait for missing and pending updates to install before exiting the function. Default is: 45 minutes.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Install-SCCMSoftwareUpdates

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Int32]$SoftwareUpdatesScanWaitInSeconds = 180,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Timespan]$WaitForPendingUpdatesTimeout = $(New-TimeSpan -Minutes 45),
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Scanning for and installing pending SCCM software updates.' -Source ${CmdletName}

			## Make sure SCCM client is installed and running
			Write-Log -Message 'Checking to see if SCCM Client service [ccmexec] is installed and running.' -Source ${CmdletName}
			If (Test-ServiceExists -Name 'ccmexec') {
				If ($(Get-Service -Name 'ccmexec' -ErrorAction 'SilentlyContinue').Status -ne 'Running') {
					Throw "SCCM Client Service [ccmexec] exists but it is not in a 'Running' state."
				}
			} Else {
				Throw 'SCCM Client Service [ccmexec] does not exist. The SCCM Client may not be installed.'
			}

			## Determine the SCCM Client Version
			Try {
				[Version]$SCCMClientVersion = Get-WmiObject -Namespace 'ROOT\CCM' -Class 'CCM_InstalledComponent' -ErrorAction 'Stop' | Where-Object { $_.Name -eq 'SmsClient' } | Select-Object -ExpandProperty 'Version' -ErrorAction 'Stop'
				If ($SCCMClientVersion) {
					Write-Log -Message "Installed SCCM Client Version Number [$SCCMClientVersion]." -Source ${CmdletName}
				} Else {
					Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
					Throw 'Failed to determine the SCCM client version number.'
				}
			} Catch {
				Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
				Throw 'Failed to determine the SCCM client version number.'
			}
			#  If SCCM 2007 Client or lower, exit function
			If ($SCCMClientVersion.Major -le 4) {
				Throw 'SCCM 2007 or lower, which is incompatible with this function, was detected on this system.'
			}

			$StartTime = Get-Date
			## Trigger SCCM client scan for Software Updates
			Write-Log -Message 'Triggering SCCM client scan for Software Updates...' -Source ${CmdletName}
			Invoke-SCCMTask -ScheduleId 'SoftwareUpdatesScan'

			Write-Log -Message "The SCCM client scan for Software Updates has been triggered. The script is suspended for [$SoftwareUpdatesScanWaitInSeconds] seconds to let the update scan finish." -Source ${CmdletName}
			Start-Sleep -Seconds $SoftwareUpdatesScanWaitInSeconds

			## Find the number of missing updates
			Try {
				Write-Log -Message 'Getting the number of missing updates...' -Source ${CmdletName}
				[Management.ManagementObject[]]$CMMissingUpdates = @(Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Query "SELECT * FROM CCM_SoftwareUpdate WHERE ComplianceState = '0'" -ErrorAction 'Stop')
			} Catch {
				Write-Log -Message "Failed to find the number of missing software updates. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
				Throw 'Failed to find the number of missing software updates.'
			}

			## Install missing updates and wait for pending updates to finish installing
			If ($CMMissingUpdates.Count) {
				#  Install missing updates
				Write-Log -Message "Installing missing updates. The number of missing updates is [$($CMMissingUpdates.Count)]." -Source ${CmdletName}
				$CMInstallMissingUpdates = (Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Class 'CCM_SoftwareUpdatesManager' -List).InstallUpdates($CMMissingUpdates)

				#  Wait for pending updates to finish installing or the timeout value to expire
				Do {
					Start-Sleep -Seconds 60
					[Array]$CMInstallPendingUpdates = @(Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Query 'SELECT * FROM CCM_SoftwareUpdate WHERE EvaluationState = 6 or EvaluationState = 7')
					Write-Log -Message "The number of updates pending installation is [$($CMInstallPendingUpdates.Count)]." -Source ${CmdletName}
				} While (($CMInstallPendingUpdates.Count -ne 0) -and ((New-TimeSpan -Start $StartTime -End $(Get-Date)) -lt $WaitForPendingUpdatesTimeout))
			} Else {
				Write-Log -Message 'There are no missing updates.' -Source ${CmdletName}
			}
		} Catch {
			Write-Log -Message "Failed to trigger installation of missing software updates. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to trigger installation of missing software updates: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
