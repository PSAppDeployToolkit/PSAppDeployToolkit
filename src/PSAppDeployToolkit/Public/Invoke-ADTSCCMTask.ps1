#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTSCCMTask
#
#-----------------------------------------------------------------------------

function Invoke-ADTSCCMTask
{
    <#
    .SYNOPSIS
        Triggers SCCM to invoke the requested schedule task ID.

    .DESCRIPTION
        Triggers SCCM to invoke the requested schedule task ID. This function supports a variety of schedule IDs compatible with different versions of the SCCM client. It ensures that the correct schedule IDs are used based on the SCCM client version.

    .PARAMETER ScheduleId
        Name of the schedule id to trigger.

        Options: HardwareInventory, SoftwareInventory, HeartbeatDiscovery, SoftwareInventoryFileCollection, RequestMachinePolicy, EvaluateMachinePolicy, LocationServicesCleanup, SoftwareMeteringReport, SourceUpdate, PolicyAgentCleanup, RequestMachinePolicy2, CertificateMaintenance, PeerDistributionPointStatus, PeerDistributionPointProvisioning, ComplianceIntervalEnforcement, SoftwareUpdatesAgentAssignmentEvaluation, UploadStateMessage, StateMessageManager, SoftwareUpdatesScan, AMTProvisionCycle, UpdateStorePolicy, StateSystemBulkSend, ApplicationManagerPolicyAction, PowerManagementStartSummarizer

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Invoke-ADTSCCMTask -ScheduleId 'SoftwareUpdatesScan'

        Triggers the 'SoftwareUpdatesScan' schedule task in SCCM.

    .EXAMPLE
        Invoke-ADTSCCMTask -ScheduleId 'HardwareInventory'

        Triggers the 'HardwareInventory' schedule task in SCCM.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Invoke-ADTSCCMTask
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('HardwareInventory', 'SoftwareInventory', 'HeartbeatDiscovery', 'SoftwareInventoryFileCollection', 'RequestMachinePolicy', 'EvaluateMachinePolicy', 'LocationServicesCleanup', 'SoftwareMeteringReport', 'SourceUpdate', 'PolicyAgentCleanup', 'RequestMachinePolicy2', 'CertificateMaintenance', 'PeerDistributionPointStatus', 'PeerDistributionPointProvisioning', 'ComplianceIntervalEnforcement', 'SoftwareUpdatesAgentAssignmentEvaluation', 'UploadStateMessage', 'StateMessageManager', 'SoftwareUpdatesScan', 'AMTProvisionCycle', 'UpdateStorePolicy', 'StateSystemBulkSend', 'ApplicationManagerPolicyAction', 'PowerManagementStartSummarizer')]
        [System.String]$ScheduleID
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Create a hashtable of Schedule IDs compatible with SCCM Client 2007.
        $ScheduleIds = @{
            HardwareInventory = '{00000000-0000-0000-0000-000000000001}'  # Hardware Inventory Collection Task
            SoftwareInventory = '{00000000-0000-0000-0000-000000000002}'  # Software Inventory Collection Task
            HeartbeatDiscovery = '{00000000-0000-0000-0000-000000000003}'  # Heartbeat Discovery Cycle
            SoftwareInventoryFileCollection = '{00000000-0000-0000-0000-000000000010}'  # Software Inventory File Collection Task
            RequestMachinePolicy = '{00000000-0000-0000-0000-000000000021}'  # Request Machine Policy Assignments
            EvaluateMachinePolicy = '{00000000-0000-0000-0000-000000000022}'  # Evaluate Machine Policy Assignments
            RefreshDefaultMp = '{00000000-0000-0000-0000-000000000023}'  # Refresh Default MP Task
            RefreshLocationServices = '{00000000-0000-0000-0000-000000000024}'  # Refresh Location Services Task
            LocationServicesCleanup = '{00000000-0000-0000-0000-000000000025}'  # Location Services Cleanup Task
            SoftwareMeteringReport = '{00000000-0000-0000-0000-000000000031}'  # Software Metering Report Cycle
            SourceUpdate = '{00000000-0000-0000-0000-000000000032}'  # Source Update Manage Update Cycle
            PolicyAgentCleanup = '{00000000-0000-0000-0000-000000000040}'  # Policy Agent Cleanup Cycle
            RequestMachinePolicy2 = '{00000000-0000-0000-0000-000000000042}'  # Request Machine Policy Assignments
            CertificateMaintenance = '{00000000-0000-0000-0000-000000000051}'  # Certificate Maintenance Cycle
            PeerDistributionPointStatus = '{00000000-0000-0000-0000-000000000061}'  # Peer Distribution Point Status Task
            PeerDistributionPointProvisioning = '{00000000-0000-0000-0000-000000000062}'  # Peer Distribution Point Provisioning Status Task
            ComplianceIntervalEnforcement = '{00000000-0000-0000-0000-000000000071}'  # Compliance Interval Enforcement
            SoftwareUpdatesAgentAssignmentEvaluation = '{00000000-0000-0000-0000-000000000108}'  # Software Updates Agent Assignment Evaluation Cycle
            UploadStateMessage = '{00000000-0000-0000-0000-000000000111}'  # Send Unsent State Messages
            StateMessageManager = '{00000000-0000-0000-0000-000000000112}'  # State Message Manager Task
            SoftwareUpdatesScan = '{00000000-0000-0000-0000-000000000113}'  # Force Update Scan
            AMTProvisionCycle = '{00000000-0000-0000-0000-000000000120}'  # AMT Provision Cycle
        }
    }

    process
    {
        try
        {
            try
            {
                # If SCCM 2012 Client or higher, modify hashtabe containing Schedule IDs so that it only has the ones compatible with this version of the SCCM client.
                Write-ADTLogEntry -Message "Invoke SCCM Schedule Task ID [$ScheduleId]..."
                if ((Get-ADTSCCMClientVersion).Major -ge 5)
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
                        RecommendedAction = 'Please check the supplied ScheduleId and try again.'
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Trigger SCCM task.
                Write-ADTLogEntry -Message "Triggering SCCM Task ID [$ScheduleId]."
                $null = Invoke-CimMethod -Namespace ROOT\CCM -ClassName SMS_Client -MethodName TriggerSchedule -Arguments @{ sScheduleID = $ScheduleIds.$ScheduleID }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to trigger SCCM Schedule Task ID [$($ScheduleIds.$ScheduleId)]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
