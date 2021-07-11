#region Function Invoke-SCCMTask
Function Invoke-SCCMTask {
<#
.SYNOPSIS
	Triggers SCCM to invoke the requested schedule task id.
.DESCRIPTION
	Triggers SCCM to invoke the requested schedule task id.
.PARAMETER ScheduleId
	Name of the schedule id to trigger.
	Options: HardwareInventory, SoftwareInventory, HeartbeatDiscovery, SoftwareInventoryFileCollection, RequestMachinePolicy, EvaluateMachinePolicy,
	LocationServicesCleanup, SoftwareMeteringReport, SourceUpdate, PolicyAgentCleanup, RequestMachinePolicy2, CertificateMaintenance, PeerDistributionPointStatus,
	PeerDistributionPointProvisioning, ComplianceIntervalEnforcement, SoftwareUpdatesAgentAssignmentEvaluation, UploadStateMessage, StateMessageManager,
	SoftwareUpdatesScan, AMTProvisionCycle, UpdateStorePolicy, StateSystemBulkSend, ApplicationManagerPolicyAction, PowerManagementStartSummarizer
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Invoke-SCCMTask 'SoftwareUpdatesScan'
.EXAMPLE
	Invoke-SCCMTask
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateSet('HardwareInventory','SoftwareInventory','HeartbeatDiscovery','SoftwareInventoryFileCollection','RequestMachinePolicy','EvaluateMachinePolicy','LocationServicesCleanup','SoftwareMeteringReport','SourceUpdate','PolicyAgentCleanup','RequestMachinePolicy2','CertificateMaintenance','PeerDistributionPointStatus','PeerDistributionPointProvisioning','ComplianceIntervalEnforcement','SoftwareUpdatesAgentAssignmentEvaluation','UploadStateMessage','StateMessageManager','SoftwareUpdatesScan','AMTProvisionCycle','UpdateStorePolicy','StateSystemBulkSend','ApplicationManagerPolicyAction','PowerManagementStartSummarizer')]
		[string]$ScheduleID,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Invoke SCCM Schedule Task ID [$ScheduleId]..." -Source ${CmdletName}

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
				[version]$SCCMClientVersion = Get-WmiObject -Namespace 'ROOT\CCM' -Class 'CCM_InstalledComponent' -ErrorAction 'Stop' | ForEach-Object { if($_.Name -eq 'SmsClient') {$_.Version} }
				if ($SCCMClientVersion) {
					Write-Log -Message "Installed SCCM Client Version Number [$SCCMClientVersion]." -Source ${CmdletName}
				} else {
					Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
					Throw 'Failed to determine the SCCM client version number.'	
				}
			}
			Catch {
				Write-Log -Message "Failed to determine the SCCM client version number. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
				Throw 'Failed to determine the SCCM client version number.'
			}

			## Create a hashtable of Schedule IDs compatible with SCCM Client 2007
			[hashtable]$ScheduleIds = @{
				HardwareInventory = '{00000000-0000-0000-0000-000000000001}'; # Hardware Inventory Collection Task
				SoftwareInventory = '{00000000-0000-0000-0000-000000000002}'; # Software Inventory Collection Task
				HeartbeatDiscovery = '{00000000-0000-0000-0000-000000000003}'; # Heartbeat Discovery Cycle
				SoftwareInventoryFileCollection = '{00000000-0000-0000-0000-000000000010}'; # Software Inventory File Collection Task
				RequestMachinePolicy = '{00000000-0000-0000-0000-000000000021}'; # Request Machine Policy Assignments
				EvaluateMachinePolicy = '{00000000-0000-0000-0000-000000000022}'; # Evaluate Machine Policy Assignments
				RefreshDefaultMp = '{00000000-0000-0000-0000-000000000023}'; # Refresh Default MP Task
				RefreshLocationServices = '{00000000-0000-0000-0000-000000000024}'; # Refresh Location Services Task
				LocationServicesCleanup = '{00000000-0000-0000-0000-000000000025}'; # Location Services Cleanup Task
				SoftwareMeteringReport = '{00000000-0000-0000-0000-000000000031}'; # Software Metering Report Cycle
				SourceUpdate = '{00000000-0000-0000-0000-000000000032}'; # Source Update Manage Update Cycle
				PolicyAgentCleanup = '{00000000-0000-0000-0000-000000000040}'; # Policy Agent Cleanup Cycle
				RequestMachinePolicy2 = '{00000000-0000-0000-0000-000000000042}'; # Request Machine Policy Assignments
				CertificateMaintenance = '{00000000-0000-0000-0000-000000000051}'; # Certificate Maintenance Cycle
				PeerDistributionPointStatus = '{00000000-0000-0000-0000-000000000061}'; # Peer Distribution Point Status Task
				PeerDistributionPointProvisioning = '{00000000-0000-0000-0000-000000000062}'; # Peer Distribution Point Provisioning Status Task
				ComplianceIntervalEnforcement = '{00000000-0000-0000-0000-000000000071}'; # Compliance Interval Enforcement
				SoftwareUpdatesAgentAssignmentEvaluation = '{00000000-0000-0000-0000-000000000108}'; # Software Updates Agent Assignment Evaluation Cycle
				UploadStateMessage = '{00000000-0000-0000-0000-000000000111}'; # Send Unsent State Messages
				StateMessageManager = '{00000000-0000-0000-0000-000000000112}'; # State Message Manager Task
				SoftwareUpdatesScan = '{00000000-0000-0000-0000-000000000113}'; # Force Update Scan
				AMTProvisionCycle = '{00000000-0000-0000-0000-000000000120}'; # AMT Provision Cycle
			}

			## If SCCM 2012 Client or higher, modify hashtabe containing Schedule IDs so that it only has the ones compatible with this version of the SCCM client
			If ($SCCMClientVersion.Major -ge 5) {
				$ScheduleIds.Remove('PeerDistributionPointStatus')
				$ScheduleIds.Remove('PeerDistributionPointProvisioning')
				$ScheduleIds.Remove('ComplianceIntervalEnforcement')
				$ScheduleIds.Add('UpdateStorePolicy','{00000000-0000-0000-0000-000000000114}') # Update Store Policy
				$ScheduleIds.Add('StateSystemBulkSend','{00000000-0000-0000-0000-000000000116}') # State System Policy Bulk Send Low
				$ScheduleIds.Add('ApplicationManagerPolicyAction','{00000000-0000-0000-0000-000000000121}') # Application Manager Policy Action
				$ScheduleIds.Add('PowerManagementStartSummarizer','{00000000-0000-0000-0000-000000000131}') # Power Management Start Summarizer
			}

			## Determine if the requested Schedule ID is available on this version of the SCCM Client
			If (-not ($ScheduleIds.ContainsKey($ScheduleId))) {
				Throw "The requested ScheduleId [$ScheduleId] is not available with this version of the SCCM Client [$SCCMClientVersion]."
			}

			## Trigger SCCM task
			Write-Log -Message "Triggering SCCM Task ID [$ScheduleId]." -Source ${CmdletName}
			[Management.ManagementClass]$SmsClient = [WMIClass]'ROOT\CCM:SMS_Client'
			$null = $SmsClient.TriggerSchedule($ScheduleIds.$ScheduleID)
		}
		Catch {
			Write-Log -Message "Failed to trigger SCCM Schedule Task ID [$($ScheduleIds.$ScheduleId)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to trigger SCCM Schedule Task ID [$($ScheduleIds.$ScheduleId)]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
