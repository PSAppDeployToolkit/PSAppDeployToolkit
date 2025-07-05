namespace PSADT.ConfigMgr
{
    /// <summary>
    /// Represents the identifiers for various scheduled tasks and cycles in a system.
    /// </summary>
    /// <remarks>This enumeration defines unique identifiers for different types of tasks and cycles, such as
    /// inventory collection,  policy evaluation, software updates, and other system operations. Each identifier
    /// corresponds to a specific scheduled operation that can be triggered within the system.</remarks>
    public enum TriggerScheduleId : ushort
    {
        /// <summary>
        /// Hardware Inventory Collection Task.
        /// </summary>
        HardwareInventory = (0x0 << 8) | (0x01 << 0),

        /// <summary>
        /// Software Inventory Collection Task
        /// </summary>
        SoftwareInventory = (0x0 << 8) | (0x02 << 0),

        /// <summary>
        /// Heartbeat Discovery Cycle
        /// </summary>
        HeartbeatDiscovery = (0x0 << 8) | (0x03 << 0),

        /// <summary>
        /// Software Inventory File Collection Task
        /// </summary>
        SoftwareInventoryFileCollection = (0x0 << 8) | (0x10 << 0),

        /// <summary>
        /// IDMID Collection
        /// </summary>
        IDMIFCollection = (0x0 << 8) | (0x11 << 0),

        /// <summary>
        /// Client Machine Authentication
        /// </summary>
        ClientMachineAuthentication = (0x0 << 8) | (0x12 << 0),

        /// <summary>
        /// Request Machine Policy Assignments
        /// </summary>
        RequestMachinePolicy = (0x0 << 8) | (0x21 << 0),

        /// <summary>
        /// Evaluate Machine Policy Assignments
        /// </summary>
        EvaluateMachinePolicy = (0x0 << 8) | (0x22 << 0),

        /// <summary>
        /// Refresh Default MP Task
        /// </summary>
        RefreshDefaultMp = (0x0 << 8) | (0x23 << 0),

        /// <summary>
        /// Refresh Location Services Task
        /// </summary>
        RefreshLocationServices = (0x0 << 8) | (0x24 << 0),

        /// <summary>
        /// Location Services Cleanup Task
        /// </summary>
        LocationServicesCleanup = (0x0 << 8) | (0x25 << 0),

        /// <summary>
        /// Policy Agent Request Assignment (User)
        /// </summary>
        PolicyAgentRequestAssignment = (0x0 << 8) | (0x26 << 0),

        /// <summary>
        /// Policy Agent Evaluate Assignment (User)
        /// </summary>
        PolicyAgentEvaluateAssignment = (0x0 << 8) | (0x27 << 0),

        /// <summary>
        /// Software Metering Report Cycle
        /// </summary>
        SoftwareMeteringReport = (0x0 << 8) | (0x31 << 0),

        /// <summary>
        /// Source Update Manage Update Cycle
        /// </summary>
        SourceUpdate = (0x0 << 8) | (0x32 << 0),

        /// <summary>
        /// Clear Proxy Settings Cache
        /// </summary>
        ClearProxySettingsCache = (0x0 << 8) | (0x37 << 0),

        /// <summary>
        /// Policy Agent Cleanup Cycle
        /// </summary>
        PolicyAgentCleanup = (0x0 << 8) | (0x40 << 0),

        /// <summary>
        /// User Policy Agent Cleanup Cycle
        /// </summary>
        UserPolicyAgentCleanup = (0x0 << 8) | (0x41 << 0),

        /// <summary>
        /// Request Machine Policy Assignments
        /// </summary>
        PolicyAgentValidateMachinePolicy = (0x0 << 8) | (0x42 << 0),

        /// <summary>
        /// Request User Policy Assignments
        /// </summary>
        PolicyAgentValidateUserPolicy = (0x0 << 8) | (0x43 << 0),

        /// <summary>
        /// Certificate Maintenance Cycle
        /// </summary>
        CertificateMaintenance = (0x0 << 8) | (0x51 << 0),

        /// <summary>
        /// Peer Distribution Point Status Task
        /// </summary>
        PeerDistributionPointStatus = (0x0 << 8) | (0x61 << 0),

        /// <summary>
        /// Peer Distribution Point Provisioning Status Task
        /// </summary>
        PeerDistributionPointProvisioning = (0x0 << 8) | (0x62 << 0),

        /// <summary>
        /// SUM Updates Install Schedule
        /// </summary>
        SUMUpdatesInstallSchedule = (0x0 << 8) | (0x63 << 0),

        /// <summary>
        /// Hardware Inventory Collection Cycle
        /// </summary>
        HardwareInventoryCollectionCycle = (0x1 << 8) | (0x01 << 0),

        /// <summary>
        /// Software Inventory Collection Cycle
        /// </summary>
        SoftwareInventoryCollectionCycle = (0x1 << 8) | (0x02 << 0),

        /// <summary>
        /// Discovery Data Collection Cycle
        /// </summary>
        DiscoveryDataCollectionCycle = (0x1 << 8) | (0x03 << 0),

        /// <summary>
        /// File Collection Cycle
        /// </summary>
        FileCollectionCycle = (0x1 << 8) | (0x04 << 0),

        /// <summary>
        /// IDMIF Collection Cycle
        /// </summary>
        IDMIFCollectionCycle = (0x1 << 8) | (0x05 << 0),

        /// <summary>
        /// Software Metering Usage Report Cycle
        /// </summary>
        SoftwareMeteringUsageReportCycle = (0x1 << 8) | (0x06 << 0),

        /// <summary>
        /// Windows Installer Source List Update Cycle
        /// </summary>
        WindowsInstallerSourceListUpdateCycle = (0x1 << 8) | (0x07 << 0),

        /// <summary>
        /// Software Updates Agent Assignment Evaluation Cycle
        /// </summary>
        SoftwareUpdatesAgentAssignmentEvaluation = (0x1 << 8) | (0x08 << 0),

        /// <summary>
        /// Branch Distribution Point Maintenance Task
        /// </summary>
        BranchDistributionPointMaintenanceTask = (0x1 << 8) | (0x09 << 0),

        /// <summary>
        /// Send Unsent State Messages
        /// </summary>
        UploadStateMessage = (0x1 << 8) | (0x11 << 0),

        /// <summary>
        /// State Message Manager Task
        /// </summary>
        StateMessageManager = (0x1 << 8) | (0x12 << 0),

        /// <summary>
        /// Force Update Scan
        /// </summary>
        SoftwareUpdatesScan = (0x1 << 8) | (0x13 << 0),

        /// <summary>
        /// Update Store Policy
        /// </summary>
        UpdateStorePolicy = (0x1 << 8) | (0x14 << 0),

        /// <summary>
        /// State system policy bulk send high
        /// </summary>
        StateSystemPolicyBulkSendHigh = (0x1 << 8) | (0x15 << 0),

        /// <summary>
        /// State system policy bulk send low
        /// </summary>
        StateSystemPolicyBulkSendLow = (0x1 << 8) | (0x16 << 0),

        /// <summary>
        /// Application manager policy action
        /// </summary>
        ApplicationManagerPolicyAction = (0x1 << 8) | (0x21 << 0),

        /// <summary>
        /// Application manager user policy action
        /// </summary>
        ApplicationManagerUserPolicyAction = (0x1 << 8) | (0x22 << 0),

        /// <summary>
        /// Application manager global evaluation action
        /// </summary>
        ApplicationManagerGlobalEvaluationAction = (0x1 << 8) | (0x23 << 0),

        /// <summary>
        /// Power management start summarizer
        /// </summary>
        PowerManagementStartSummarizer = (0x1 << 8) | (0x31 << 0),

        /// <summary>
        /// Endpoint deployment reevaluate
        /// </summary>
        EndpointDeploymentReevaluate = (0x2 << 8) | (0x21 << 0),

        /// <summary>
        /// Endpoint AM policy reevaluate
        /// </summary>
        EndpointAMPolicyReevaluate = (0x2 << 8) | (0x22 << 0),

        /// <summary>
        /// External event detection
        /// </summary>
        ExternalEventDetection = (0x2 << 8) | (0x23 << 0),
    }
}
