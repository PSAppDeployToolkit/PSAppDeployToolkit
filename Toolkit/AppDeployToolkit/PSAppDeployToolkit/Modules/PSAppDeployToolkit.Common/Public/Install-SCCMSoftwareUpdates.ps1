Function Install-SCCMSoftwareUpdates {
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
        Write-ADTDebugHeader
    }
    Process {
        Try {
            Write-ADTLogEntry -Message 'Scanning for and installing pending SCCM software updates.'

            ## Make sure SCCM client is installed and running
            Write-ADTLogEntry -Message 'Checking to see if SCCM Client service [ccmexec] is installed and running.'
            If (Test-ADTServiceExists -Name 'ccmexec') {
                If ($(Get-Service -Name 'ccmexec' -ErrorAction 'Ignore').Status -ne 'Running') {
                    Throw "SCCM Client Service [ccmexec] exists but it is not in a 'Running' state."
                }
            }
            Else {
                Throw 'SCCM Client Service [ccmexec] does not exist. The SCCM Client may not be installed.'
            }

            ## Determine the SCCM Client Version
            Try {
                [Version]$SCCMClientVersion = Get-WmiObject -Namespace 'ROOT\CCM' -Class 'CCM_InstalledComponent' -ErrorAction 'Stop' | Where-Object { $_.Name -eq 'SmsClient' } | Select-Object -ExpandProperty 'Version' -ErrorAction 'Stop'
                If ($SCCMClientVersion) {
                    Write-ADTLogEntry -Message "Installed SCCM Client Version Number [$SCCMClientVersion]."
                }
                Else {
                    Write-ADTLogEntry -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2
                    Throw 'Failed to determine the SCCM client version number.'
                }
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2
                Throw 'Failed to determine the SCCM client version number.'
            }
            #  If SCCM 2007 Client or lower, exit function
            If ($SCCMClientVersion.Major -le 4) {
                Throw 'SCCM 2007 or lower, which is incompatible with this function, was detected on this system.'
            }

            $StartTime = Get-Date
            ## Trigger SCCM client scan for Software Updates
            Write-ADTLogEntry -Message 'Triggering SCCM client scan for Software Updates...'
            Invoke-SCCMTask -ScheduleId 'SoftwareUpdatesScan'

            Write-ADTLogEntry -Message "The SCCM client scan for Software Updates has been triggered. The script is suspended for [$SoftwareUpdatesScanWaitInSeconds] seconds to let the update scan finish."
            Start-Sleep -Seconds $SoftwareUpdatesScanWaitInSeconds

            ## Find the number of missing updates
            Try {
                Write-ADTLogEntry -Message 'Getting the number of missing updates...'
                [Management.ManagementObject[]]$CMMissingUpdates = @(Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Query "SELECT * FROM CCM_SoftwareUpdate WHERE ComplianceState = '0'" -ErrorAction 'Stop')
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to find the number of missing software updates. `r`n$(Resolve-Error)" -Severity 2
                Throw 'Failed to find the number of missing software updates.'
            }

            ## Install missing updates and wait for pending updates to finish installing
            If ($CMMissingUpdates.Count) {
                #  Install missing updates
                Write-ADTLogEntry -Message "Installing missing updates. The number of missing updates is [$($CMMissingUpdates.Count)]."
                $CMInstallMissingUpdates = (Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Class 'CCM_SoftwareUpdatesManager' -List).InstallUpdates($CMMissingUpdates)

                #  Wait for pending updates to finish installing or the timeout value to expire
                Do {
                    Start-Sleep -Seconds 60
                    [Array]$CMInstallPendingUpdates = @(Get-WmiObject -Namespace 'ROOT\CCM\ClientSDK' -Query 'SELECT * FROM CCM_SoftwareUpdate WHERE EvaluationState = 6 or EvaluationState = 7')
                    Write-ADTLogEntry -Message "The number of updates pending installation is [$($CMInstallPendingUpdates.Count)]."
                } While (($CMInstallPendingUpdates.Count -ne 0) -and ((New-TimeSpan -Start $StartTime -End $(Get-Date)) -lt $WaitForPendingUpdatesTimeout))
            }
            Else {
                Write-ADTLogEntry -Message 'There are no missing updates.'
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to trigger installation of missing software updates. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to trigger installation of missing software updates: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
