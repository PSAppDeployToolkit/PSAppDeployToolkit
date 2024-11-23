function Invoke-ADTSCCMTask
{
    <#

    .SYNOPSIS
    Triggers SCCM to invoke the requested schedule task ID.

    .DESCRIPTION
    Triggers SCCM to invoke the requested schedule task ID.

    .PARAMETER ScheduleId
    Name of the schedule id to trigger.

    Options: HardwareInventory, SoftwareInventory, HeartbeatDiscovery, SoftwareInventoryFileCollection, RequestMachinePolicy, EvaluateMachinePolicy,
    LocationServicesCleanup, SoftwareMeteringReport, SourceUpdate, PolicyAgentCleanup, RequestMachinePolicy2, CertificateMaintenance, PeerDistributionPointStatus,
    PeerDistributionPointProvisioning, ComplianceIntervalEnforcement, SoftwareUpdatesAgentAssignmentEvaluation, UploadStateMessage, StateMessageManager,
    SoftwareUpdatesScan, AMTProvisionCycle, UpdateStorePolicy, StateSystemBulkSend, ApplicationManagerPolicyAction, PowerManagementStartSummarizer

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Invoke-ADTSCCMTask 'SoftwareUpdatesScan'

    .EXAMPLE
    Invoke-ADTSCCMTask

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateSet('HardwareInventory', 'SoftwareInventory', 'HeartbeatDiscovery', 'SoftwareInventoryFileCollection', 'RequestMachinePolicy', 'EvaluateMachinePolicy', 'LocationServicesCleanup', 'SoftwareMeteringReport', 'SourceUpdate', 'PolicyAgentCleanup', 'RequestMachinePolicy2', 'CertificateMaintenance', 'PeerDistributionPointStatus', 'PeerDistributionPointProvisioning', 'ComplianceIntervalEnforcement', 'SoftwareUpdatesAgentAssignmentEvaluation', 'UploadStateMessage', 'StateMessageManager', 'SoftwareUpdatesScan', 'AMTProvisionCycle', 'UpdateStorePolicy', 'StateSystemBulkSend', 'ApplicationManagerPolicyAction', 'PowerManagementStartSummarizer')]
        [System.String]$ScheduleID
    )

    begin {
        # Make this function continue on error.
        $OriginalErrorAction = if ($PSBoundParameters.ContainsKey('ErrorAction'))
        {
            $PSBoundParameters.ErrorAction
        }
        else
        {
            [System.Management.Automation.ActionPreference]::Continue
        }
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop

        # Create a hashtable of Schedule IDs compatible with SCCM Client 2007.
        [Hashtable]$ScheduleIds = @{
            HardwareInventory                        = '{00000000-0000-0000-0000-000000000001}'; # Hardware Inventory Collection Task
            SoftwareInventory                        = '{00000000-0000-0000-0000-000000000002}'; # Software Inventory Collection Task
            HeartbeatDiscovery                       = '{00000000-0000-0000-0000-000000000003}'; # Heartbeat Discovery Cycle
            SoftwareInventoryFileCollection          = '{00000000-0000-0000-0000-000000000010}'; # Software Inventory File Collection Task
            RequestMachinePolicy                     = '{00000000-0000-0000-0000-000000000021}'; # Request Machine Policy Assignments
            EvaluateMachinePolicy                    = '{00000000-0000-0000-0000-000000000022}'; # Evaluate Machine Policy Assignments
            RefreshDefaultMp                         = '{00000000-0000-0000-0000-000000000023}'; # Refresh Default MP Task
            RefreshLocationServices                  = '{00000000-0000-0000-0000-000000000024}'; # Refresh Location Services Task
            LocationServicesCleanup                  = '{00000000-0000-0000-0000-000000000025}'; # Location Services Cleanup Task
            SoftwareMeteringReport                   = '{00000000-0000-0000-0000-000000000031}'; # Software Metering Report Cycle
            SourceUpdate                             = '{00000000-0000-0000-0000-000000000032}'; # Source Update Manage Update Cycle
            PolicyAgentCleanup                       = '{00000000-0000-0000-0000-000000000040}'; # Policy Agent Cleanup Cycle
            RequestMachinePolicy2                    = '{00000000-0000-0000-0000-000000000042}'; # Request Machine Policy Assignments
            CertificateMaintenance                   = '{00000000-0000-0000-0000-000000000051}'; # Certificate Maintenance Cycle
            PeerDistributionPointStatus              = '{00000000-0000-0000-0000-000000000061}'; # Peer Distribution Point Status Task
            PeerDistributionPointProvisioning        = '{00000000-0000-0000-0000-000000000062}'; # Peer Distribution Point Provisioning Status Task
            ComplianceIntervalEnforcement            = '{00000000-0000-0000-0000-000000000071}'; # Compliance Interval Enforcement
            SoftwareUpdatesAgentAssignmentEvaluation = '{00000000-0000-0000-0000-000000000108}'; # Software Updates Agent Assignment Evaluation Cycle
            UploadStateMessage                       = '{00000000-0000-0000-0000-000000000111}'; # Send Unsent State Messages
            StateMessageManager                      = '{00000000-0000-0000-0000-000000000112}'; # State Message Manager Task
            SoftwareUpdatesScan                      = '{00000000-0000-0000-0000-000000000113}'; # Force Update Scan
            AMTProvisionCycle                        = '{00000000-0000-0000-0000-000000000120}'; # AMT Provision Cycle
        }
        Write-ADTDebugHeader
    }

    process {
        try
        {
            # Make sure SCCM client is installed and running.
            Write-ADTLogEntry -Message "Invoke SCCM Schedule Task ID [$ScheduleId]..."
            Write-ADTLogEntry -Message 'Checking to see if SCCM Client service [ccmexec] is installed and running.'
            if (!(Test-ADTServiceExists -Name ccmexec))
            {
                $naerParams = @{
                    Exception = [System.ApplicationException]::new('SCCM Client Service [ccmexec] does not exist. The SCCM Client may not be installed.')
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'CcmExecServiceMissing'
                    RecommendedAction = "Please check the availability of this service and try again."
                }
                throw (New-ADTErrorRecord @naerParams)
            } 
            if (($svc = Get-Service -Name ccmexec).Status -ne 'Running')
            {
                $naerParams = @{
                    Exception = [System.ApplicationException]::new("SCCM Client Service [ccmexec] exists but it is not in a 'Running' state.")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'CcmExecServiceNotRunning'
                    TargetObject = $svc
                    RecommendedAction = "Please check the status of this service and try again."
                }
                throw (New-ADTErrorRecord @naerParams)
            }

            # Determine the SCCM Client Version.
            try
            {
                if ([System.Version]$SCCMClientVersion = Get-CimInstance -Namespace ROOT\CCM -ClassName CCM_InstalledComponent | Where-Object {$_.Name -eq 'SmsClient'} | Select-Object -ExpandProperty Version)
                {
                    Write-ADTLogEntry -Message "Installed SCCM Client Version Number [$SCCMClientVersion]."
                }
                else
                {
                    $naerParams = @{
                        Exception = [System.ApplicationException]::new('Failed to determine the SCCM client version number.')
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'CcmExecVersionNullOrEmpty'
                        RecommendedAction = "Please check the installed version and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to determine the SCCM client version number.`n$(Resolve-ADTError)" -Severity 2
                throw
            }

            # If SCCM 2012 Client or higher, modify hashtabe containing Schedule IDs so that it only has the ones compatible with this version of the SCCM client.
            if ($SCCMClientVersion.Major -ge 5)
            {
                $ScheduleIds.Remove('PeerDistributionPointStatus')
                $ScheduleIds.Remove('PeerDistributionPointProvisioning')
                $ScheduleIds.Remove('ComplianceIntervalEnforcement')
                $ScheduleIds.Add('UpdateStorePolicy', '{00000000-0000-0000-0000-000000000114}') # Update Store Policy
                $ScheduleIds.Add('StateSystemBulkSend', '{00000000-0000-0000-0000-000000000116}') # State System Policy Bulk Send Low
                $ScheduleIds.Add('ApplicationManagerPolicyAction', '{00000000-0000-0000-0000-000000000121}') # Application Manager Policy Action
                $ScheduleIds.Add('PowerManagementStartSummarizer', '{00000000-0000-0000-0000-000000000131}') # Power Management Start Summarizer
            }

            # Determine if the requested Schedule ID is available on this version of the SCCM Client.
            if (!$ScheduleIds.ContainsKey($ScheduleId))
            {
                $naerParams = @{
                    Exception = [System.ApplicationException]::new("The requested ScheduleId [$ScheduleId] is not available with this version of the SCCM Client [$SCCMClientVersion].")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidData
                    ErrorId = 'CcmExecInvalidScheduleId'
                    RecommendedAction = "Please check the supplied ScheduleId and try again."
                }
                throw (New-ADTErrorRecord @naerParams)
            }

            # Trigger SCCM task.
            Write-ADTLogEntry -Message "Triggering SCCM Task ID [$ScheduleId]."
            [System.Void](Get-CimInstance -Namespace ROOT\CCM -ClassName SMS_Client).TriggerSchedule($ScheduleIds.$ScheduleID)
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to trigger SCCM Schedule Task ID [$($ScheduleIds.$ScheduleId)].`n$(Resolve-ADTError)" -Severity 3
            $ErrorActionPreference = $OriginalErrorAction
            $PSCmdlet.WriteError($_)
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
