# Invoke-SCCMTask

## SYNOPSIS

Triggers SCCM to invoke the requested schedule task id.

## SYNTAX

 `Invoke-SCCMTask [-ScheduleID] <String> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Triggers SCCM to invoke the requested schedule task id.

## PARAMETERS

`-ScheduleID <String>`

Name of the schedule id to trigger.

Options: HardwareInventory, SoftwareInventory, HeartbeatDiscovery, SoftwareInventoryFileCollection, RequestMachinePolicy, EvaluateMachinePolicy,

LocationServicesCleanup, SoftwareMeteringReport, SourceUpdate, PolicyAgentCleanup, RequestMachinePolicy2, CertificateMaintenance, PeerDistributionPointStatus,

PeerDistributionPointProvisioning, ComplianceIntervalEnforcement, SoftwareUpdatesAgentAssignmentEvaluation, UploadStateMessage, StateMessageManager,

SoftwareUpdatesScan, AMTProvisionCycle, UpdateStorePolicy, StateSystemBulkSend, ApplicationManagerPolicyAction, PowerManagementStartSummarizer

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Invoke-SCCMTask 'SoftwareUpdatesScan'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Invoke-SCCMTask`

## REMARKS

To see the examples, type: `Get-Help Invoke-SCCMTask -Examples`

For more information, type: `Get-Help Invoke-SCCMTask -Detailed`

For technical information, type: `Get-Help Invoke-SCCMTask -Full`

For online help, type: `Get-Help Invoke-SCCMTask -Online`
