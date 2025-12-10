namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// System information classes.
    /// </summary>
    internal enum SYSTEM_INFORMATION_CLASS
    {
        /// <summary>
        /// SYSTEM_BASIC_INFORMATION
        /// </summary>
        SystemBasicInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemBasicInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_INFORMATION
        /// </summary>
        SystemProcessorInformation,

        /// <summary>
        /// SYSTEM_PERFORMANCE_INFORMATION
        /// </summary>
        SystemPerformanceInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemPerformanceInformation,

        /// <summary>
        /// SYSTEM_TIMEOFDAY_INFORMATION
        /// </summary>
        SystemTimeOfDayInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemTimeOfDayInformation,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemPathInformation,

        /// <summary>
        /// SYSTEM_PROCESS_INFORMATION
        /// </summary>
        SystemProcessInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemProcessInformation,

        /// <summary>
        /// SYSTEM_CALL_COUNT_INFORMATION
        /// </summary>
        SystemCallCountInformation,

        /// <summary>
        /// SYSTEM_DEVICE_INFORMATION
        /// </summary>
        SystemDeviceInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION
        /// </summary>
        SystemProcessorPerformanceInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemProcessorPerformanceInformation,

        /// <summary>
        /// SYSTEM_FLAGS_INFORMATION
        /// </summary>
        SystemFlagsInformation,

        /// <summary>
        /// SYSTEM_CALL_TIME_INFORMATION (Not implemented).
        /// </summary>
        SystemCallTimeInformation,

        /// <summary>
        /// RTL_PROCESS_MODULES
        /// </summary>
        SystemModuleInformation,

        /// <summary>
        /// RTL_PROCESS_LOCKS
        /// </summary>
        SystemLocksInformation,

        /// <summary>
        /// RTL_PROCESS_BACKTRACES
        /// </summary>
        SystemStackTraceInformation,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemPagedPoolInformation,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemNonPagedPoolInformation,

        /// <summary>
        /// SYSTEM_HANDLE_INFORMATION
        /// </summary>
        SystemHandleInformation,

        /// <summary>
        /// SYSTEM_OBJECTTYPE_INFORMATION/SYSTEM_OBJECT_INFORMATION
        /// </summary>
        SystemObjectInformation,

        /// <summary>
        /// SYSTEM_PAGEFILE_INFORMATION
        /// </summary>
        SystemPageFileInformation,

        /// <summary>
        /// SYSTEM_VDM_INSTEMUL_INFO
        /// </summary>
        SystemVdmInstemulInformation,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemVdmBopInformation,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (info for WorkingSetTypeSystemCache).
        /// </summary>
        SystemFileCacheInformation,

        /// <summary>
        /// SYSTEM_POOLTAG_INFORMATION
        /// </summary>
        SystemPoolTagInformation,

        /// <summary>
        /// SYSTEM_INTERRUPT_INFORMATION
        /// </summary>
        SystemInterruptInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemInterruptInformation,

        /// <summary>
        /// SYSTEM_DPC_BEHAVIOR_INFORMATION (Requires SeLoadDriverPrivilege).
        /// </summary>
        SystemDpcBehaviorInformation,

        /// <summary>
        /// SYSTEM_MEMORY_USAGE_INFORMATION (Not implemented).
        /// </summary>
        SystemFullMemoryInformation,

        /// <summary>
        /// Kernel-mode only.
        /// </summary>
        SystemLoadGdiDriverInformation,

        /// <summary>
        /// Kernel-mode only.
        /// </summary>
        SystemUnloadGdiDriverInformation,

        /// <summary>
        /// Query: SYSTEM_QUERY_TIME_ADJUST_INFORMATION;
        /// Set: SYSTEM_SET_TIME_ADJUST_INFORMATION
        /// (Requires SeSystemtimePrivilege).
        /// </summary>
        SystemTimeAdjustmentInformation,

        /// <summary>
        /// SYSTEM_MEMORY_USAGE_INFORMATION (Not implemented).
        /// </summary>
        SystemSummaryMemoryInformation,

        /// <summary>
        /// Undocumented (Requires license value "Kernel-MemoryMirroringSupported") (Requires SeShutdownPrivilege).
        /// </summary>
        SystemMirrorMemoryInformation,

        /// <summary>
        /// EVENT_TRACE_INFORMATION_CLASS
        /// </summary>
        SystemPerformanceTraceInformation,

        /// <summary>
        /// Not implemented (previously SystemCrashDumpInformation).
        /// </summary>
        SystemObsolete0,

        /// <summary>
        /// SYSTEM_EXCEPTION_INFORMATION
        /// </summary>
        SystemExceptionInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemExceptionInformation,

        /// <summary>
        /// SYSTEM_CRASH_DUMP_STATE_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemCrashDumpStateInformation,

        /// <summary>
        /// SYSTEM_KERNEL_DEBUGGER_INFORMATION
        /// </summary>
        SystemKernelDebuggerInformation,

        /// <summary>
        /// SYSTEM_CONTEXT_SWITCH_INFORMATION
        /// </summary>
        SystemContextSwitchInformation,

        /// <summary>
        /// SYSTEM_REGISTRY_QUOTA_INFORMATION (Requires SeIncreaseQuotaPrivilege).
        /// </summary>
        SystemRegistryQuotaInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemRegistryQuotaInformation,

        /// <summary>
        /// Undocumented (Requires SeLoadDriverPrivilege).
        /// </summary>
        SystemExtendServiceTableInformation,

        /// <summary>
        /// Undocumented (Requires SeTcbPrivilege).
        /// </summary>
        SystemPrioritySeparation,

        /// <summary>
        /// UNICODE_STRING (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierAddDriverInformation,

        /// <summary>
        /// UNICODE_STRING (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierRemoveDriverInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_IDLE_INFORMATION
        /// </summary>
        SystemProcessorIdleInformation,

        /// <summary>
        /// SYSTEM_LEGACY_DRIVER_INFORMATION
        /// </summary>
        SystemLegacyDriverInformation,

        /// <summary>
        /// RTL_TIME_ZONE_INFORMATION
        /// </summary>
        SystemCurrentTimeZoneInformation,

        /// <summary>
        /// SYSTEM_LOOKASIDE_INFORMATION
        /// </summary>
        SystemLookasideInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemLookasideInformation,

        /// <summary>
        /// HANDLE (NtCreateEvent) (Requires SeSystemtimePrivilege).
        /// </summary>
        SystemTimeSlipNotification,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemSessionCreate,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemSessionDetach,

        /// <summary>
        /// SYSTEM_SESSION_INFORMATION (Not implemented).
        /// </summary>
        SystemSessionInformation,

        /// <summary>
        /// SYSTEM_RANGE_START_INFORMATION
        /// </summary>
        SystemRangeStartInformation,

        /// <summary>
        /// SYSTEM_VERIFIER_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierInformation,

        /// <summary>
        /// Kernel-mode only.
        /// </summary>
        SystemVerifierThunkExtend,

        /// <summary>
        /// SYSTEM_SESSION_PROCESS_INFORMATION
        /// </summary>
        SystemSessionProcessInformation,

        /// <summary>
        /// SYSTEM_GDI_DRIVER_INFORMATION (Kernel-mode only) (Same as SystemLoadGdiDriverInformation).
        /// </summary>
        SystemLoadGdiDriverInSystemSpace,

        /// <summary>
        /// SYSTEM_NUMA_INFORMATION
        /// </summary>
        SystemNumaProcessorMap,

        /// <summary>
        /// PREFETCHER_INFORMATION (PfSnQueryPrefetcherInformation).
        /// </summary>
        SystemPrefetcherInformation,

        /// <summary>
        /// SYSTEM_EXTENDED_PROCESS_INFORMATION
        /// </summary>
        SystemExtendedProcessInformation,

        /// <summary>
        /// ULONG (KeGetRecommendedSharedDataAlignment).
        /// </summary>
        SystemRecommendedSharedDataAlignment,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemComPlusPackage,

        /// <summary>
        /// SYSTEM_NUMA_INFORMATION
        /// </summary>
        SystemNumaAvailableMemory,

        /// <summary>
        /// SYSTEM_PROCESSOR_POWER_INFORMATION
        /// </summary>
        SystemProcessorPowerInformation,

        /// <summary>
        /// SYSTEM_BASIC_INFORMATION
        /// </summary>
        SystemEmulationBasicInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_INFORMATION
        /// </summary>
        SystemEmulationProcessorInformation,

        /// <summary>
        /// SYSTEM_HANDLE_INFORMATION_EX
        /// </summary>
        SystemExtendedHandleInformation,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemLostDelayedWriteInformation,

        /// <summary>
        /// SYSTEM_BIGPOOL_INFORMATION
        /// </summary>
        SystemBigPoolInformation,

        /// <summary>
        /// SYSTEM_SESSION_POOLTAG_INFORMATION
        /// </summary>
        SystemSessionPoolTagInformation,

        /// <summary>
        /// SYSTEM_SESSION_MAPPED_VIEW_INFORMATION
        /// </summary>
        SystemSessionMappedViewInformation,

        /// <summary>
        /// SYSTEM_HOTPATCH_CODE_INFORMATION
        /// </summary>
        SystemHotpatchInformation,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemObjectSecurityMode,

        /// <summary>
        /// SYSTEM_WATCHDOG_HANDLER_INFORMATION (Kernel-mode only).
        /// </summary>
        SystemWatchdogTimerHandler,

        /// <summary>
        /// SYSTEM_WATCHDOG_TIMER_INFORMATION (NtQuerySystemInformationEx) (Kernel-mode only).
        /// </summary>
        SystemWatchdogTimerInformation,

        /// <summary>
        /// SYSTEM_LOGICAL_PROCESSOR_INFORMATION (NtQuerySystemInformationEx).
        /// </summary>
        SystemLogicalProcessorInformation,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemWow64SharedInformationObsolete,

        /// <summary>
        /// SYSTEM_FIRMWARE_TABLE_HANDLER (Kernel-mode only).
        /// </summary>
        SystemRegisterFirmwareTableInformationHandler,

        /// <summary>
        /// SYSTEM_FIRMWARE_TABLE_INFORMATION
        /// </summary>
        SystemFirmwareTableInformation,

        /// <summary>
        /// RTL_PROCESS_MODULE_INFORMATION_EX (Since VISTA).
        /// </summary>
        SystemModuleInformationEx,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemVerifierTriageInformation,

        /// <summary>
        /// SUPERFETCH_INFORMATION (PfQuerySuperfetchInformation).
        /// </summary>
        SystemSuperfetchInformation,

        /// <summary>
        /// Query: SYSTEM_MEMORY_LIST_INFORMATION;
        /// Set: SYSTEM_MEMORY_LIST_COMMAND
        /// Requires SeProfileSingleProcessPrivilege.
        /// </summary>
        SystemMemoryListInformation,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (Same as SystemFileCacheInformation).
        /// </summary>
        SystemFileCacheInformationEx,

        /// <summary>
        /// SYSTEM_THREAD_CID_PRIORITY_INFORMATION (Requires SeIncreaseBasePriorityPrivilege) (NtQuerySystemInformationEx).
        /// </summary>
        SystemThreadPriorityClientIdInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_IDLE_CYCLE_TIME_INFORMATION[] (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorIdleCycleTimeInformation,

        /// <summary>
        /// SYSTEM_VERIFIER_CANCELLATION_INFORMATION (WOW64 name: whNT32QuerySystemVerifierCancellationInformation).
        /// </summary>
        SystemVerifierCancellationInformation,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemProcessorPowerInformationEx,

        /// <summary>
        /// SYSTEM_REF_TRACE_INFORMATION (ObQueryRefTraceInformation).
        /// </summary>
        SystemRefTraceInformation,

        /// <summary>
        /// SYSTEM_SPECIAL_POOL_INFORMATION (Requires SeDebugPrivilege) (MmSpecialPoolTag, then MmSpecialPoolCatchOverruns != 0).
        /// </summary>
        SystemSpecialPoolInformation,

        /// <summary>
        /// SYSTEM_PROCESS_ID_INFORMATION
        /// </summary>
        SystemProcessIdInformation,

        /// <summary>
        /// Undocumented (Requires SeTcbPrivilege).
        /// </summary>
        SystemErrorPortInformation,

        /// <summary>
        /// SYSTEM_BOOT_ENVIRONMENT_INFORMATION
        /// </summary>
        SystemBootEnvironmentInformation,

        /// <summary>
        /// SYSTEM_HYPERVISOR_QUERY_INFORMATION
        /// </summary>
        SystemHypervisorInformation,

        /// <summary>
        /// SYSTEM_VERIFIER_INFORMATION_EX
        /// </summary>
        SystemVerifierInformationEx,

        /// <summary>
        /// RTL_TIME_ZONE_INFORMATION (Requires SeTimeZonePrivilege).
        /// </summary>
        SystemTimeZoneInformation,

        /// <summary>
        /// SYSTEM_IMAGE_FILE_EXECUTION_OPTIONS_INFORMATION (Requires SeTcbPrivilege).
        /// </summary>
        SystemImageFileExecutionOptionsInformation,

        /// <summary>
        /// Query: COVERAGE_MODULES
        /// Set: COVERAGE_MODULE_REQUEST
        /// (ExpCovQueryInformation)
        /// (Requires SeDebugPrivilege)
        /// </summary>
        SystemCoverageInformation,

        /// <summary>
        /// SYSTEM_PREFETCH_PATCH_INFORMATION
        /// </summary>
        SystemPrefetchPatchInformation,

        /// <summary>
        /// SYSTEM_VERIFIER_FAULTS_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierFaultsInformation,

        /// <summary>
        /// SYSTEM_SYSTEM_PARTITION_INFORMATION
        /// </summary>
        SystemSystemPartitionInformation,

        /// <summary>
        /// SYSTEM_SYSTEM_DISK_INFORMATION
        /// </summary>
        SystemSystemDiskInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_PERFORMANCE_DISTRIBUTION (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorPerformanceDistribution,

        /// <summary>
        /// SYSTEM_NUMA_PROXIMITY_MAP
        /// </summary>
        SystemNumaProximityNodeInformation,

        /// <summary>
        /// RTL_DYNAMIC_TIME_ZONE_INFORMATION (Requires SeTimeZonePrivilege).
        /// </summary>
        SystemDynamicTimeZoneInformation,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_INFORMATION (SeCodeIntegrityQueryInformation).
        /// </summary>
        SystemCodeIntegrityInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemCodeIntegrityInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_MICROCODE_UPDATE_INFORMATION
        /// </summary>
        SystemProcessorMicrocodeUpdateInformation,

        /// <summary>
        /// CHAR[] (HaliQuerySystemInformation -> HalpGetProcessorBrandString; Info class 23).
        /// </summary>
        SystemProcessorBrandString,

        /// <summary>
        /// SYSTEM_VA_LIST_INFORMATION[] (Requires SeIncreaseQuotaPrivilege) (MmQuerySystemVaInformation).
        /// </summary>
        SystemVirtualAddressInformation,

        /// <summary>
        /// SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX (Since WIN7) (NtQuerySystemInformationEx) (KeQueryLogicalProcessorRelationship).
        /// </summary>
        SystemLogicalProcessorAndGroupInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_CYCLE_TIME_INFORMATION[] (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorCycleTimeInformation,

        /// <summary>
        /// SYSTEM_STORE_INFORMATION (Requires SeProfileSingleProcessPrivilege) (SmQueryStoreInformation).
        /// </summary>
        SystemStoreInformation,

        /// <summary>
        /// SYSTEM_REGISTRY_APPEND_STRING_PARAMETERS
        /// </summary>
        SystemRegistryAppendString,

        /// <summary>
        /// ULONG (Requires SeProfileSingleProcessPrivilege).
        /// </summary>
        SystemAitSamplingValue,

        /// <summary>
        /// SYSTEM_VHD_BOOT_INFORMATION
        /// </summary>
        SystemVhdBootInformation,

        /// <summary>
        /// PS_CPU_QUOTA_QUERY_INFORMATION
        /// </summary>
        SystemCpuQuotaInformation,

        /// <summary>
        /// SYSTEM_BASIC_INFORMATION
        /// </summary>
        SystemNativeBasicInformation,

        /// <summary>
        /// SYSTEM_ERROR_PORT_TIMEOUTS
        /// </summary>
        SystemErrorPortTimeouts,

        /// <summary>
        /// SYSTEM_LOW_PRIORITY_IO_INFORMATION
        /// </summary>
        SystemLowPriorityIoInformation,

        /// <summary>
        /// BOOT_ENTROPY_NT_RESULT (ExQueryBootEntropyInformation).
        /// </summary>
        SystemTpmBootEntropyInformation,

        /// <summary>
        /// SYSTEM_VERIFIER_COUNTERS_INFORMATION
        /// </summary>
        SystemVerifierCountersInformation,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (Info for WorkingSetTypePagedPool).
        /// </summary>
        SystemPagedPoolInformationEx,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (Info for WorkingSetTypeSystemPtes).
        /// </summary>
        SystemSystemPtesInformationEx,

        /// <summary>
        /// USHORT[4*NumaNodes] (NtQuerySystemInformationEx).
        /// </summary>
        SystemNodeDistanceInformation,

        /// <summary>
        /// SYSTEM_ACPI_AUDIT_INFORMATION (HaliQuerySystemInformation -> HalpAuditQueryResults; Info class 26).
        /// </summary>
        SystemAcpiAuditInformation,

        /// <summary>
        /// SYSTEM_BASIC_PERFORMANCE_INFORMATION (WOW64 name: whNtQuerySystemInformation_SystemBasicPerformanceInformation).
        /// </summary>
        SystemBasicPerformanceInformation,

        /// <summary>
        /// SYSTEM_QUERY_PERFORMANCE_COUNTER_INFORMATION (Since WIN7 SP1).
        /// </summary>
        SystemQueryPerformanceCounterInformation,

        /// <summary>
        /// SYSTEM_SESSION_POOLTAG_INFORMATION (Since WIN8).
        /// </summary>
        SystemSessionBigPoolInformation,

        /// <summary>
        /// SYSTEM_BOOT_GRAPHICS_INFORMATION (Kernel-mode only).
        /// </summary>
        SystemBootGraphicsInformation,

        /// <summary>
        /// MEMORY_SCRUB_INFORMATION
        /// </summary>
        SystemScrubPhysicalMemoryInformation,

        /// <summary>
        /// SYSTEM_BAD_PAGE_INFORMATION
        /// </summary>
        SystemBadPageInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_PROFILE_CONTROL_AREA
        /// </summary>
        SystemProcessorProfileControlArea,

        /// <summary>
        /// MEMORY_COMBINE_INFORMATION, MEMORY_COMBINE_INFORMATION_EX, MEMORY_COMBINE_INFORMATION_EX2
        /// </summary>
        SystemCombinePhysicalMemoryInformation,

        /// <summary>
        /// SYSTEM_ENTROPY_TIMING_INFORMATION
        /// </summary>
        SystemEntropyInterruptTimingInformation,

        /// <summary>
        /// SYSTEM_CONSOLE_INFORMATION
        /// </summary>
        SystemConsoleInformation,

        /// <summary>
        /// SYSTEM_PLATFORM_BINARY_INFORMATION (Requires SeTcbPrivilege).
        /// </summary>
        SystemPlatformBinaryInformation,

        /// <summary>
        /// SYSTEM_POLICY_INFORMATION (Warbird/Encrypt/Decrypt/Execute).
        /// </summary>
        SystemPolicyInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemPolicyInformation,

        /// <summary>
        /// SYSTEM_HYPERVISOR_PROCESSOR_COUNT_INFORMATION.
        /// </summary>
        SystemHypervisorProcessorCountInformation,

        /// <summary>
        /// SYSTEM_DEVICE_DATA_INFORMATION
        /// </summary>
        SystemDeviceDataInformation,

        /// <summary>
        /// SYSTEM_DEVICE_DATA_INFORMATION
        /// </summary>
        SystemDeviceDataEnumerationInformation,

        /// <summary>
        /// SYSTEM_MEMORY_TOPOLOGY_INFORMATION
        /// </summary>
        SystemMemoryTopologyInformation,

        /// <summary>
        /// SYSTEM_MEMORY_CHANNEL_INFORMATION
        /// </summary>
        SystemMemoryChannelInformation,

        /// <summary>
        /// SYSTEM_BOOT_LOGO_INFORMATION
        /// </summary>
        SystemBootLogoInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION_EX (NtQuerySystemInformationEx) (Since WINBLUE).
        /// </summary>
        SystemProcessorPerformanceInformationEx,

        /// <summary>
        /// CRITICAL_PROCESS_EXCEPTION_DATA
        /// </summary>
        SystemCriticalProcessErrorLogInformation,

        /// <summary>
        /// SYSTEM_SECUREBOOT_POLICY_INFORMATION
        /// </summary>
        SystemSecureBootPolicyInformation,

        /// <summary>
        /// SYSTEM_PAGEFILE_INFORMATION_EX
        /// </summary>
        SystemPageFileInformationEx,

        /// <summary>
        /// SYSTEM_SECUREBOOT_INFORMATION
        /// </summary>
        SystemSecureBootInformation,

        /// <summary>
        /// SYSTEM_ENTROPY_TIMING_INFORMATION
        /// </summary>
        SystemEntropyInterruptTimingRawInformation,

        /// <summary>
        /// SYSTEM_PORTABLE_WORKSPACE_EFI_LAUNCHER_INFORMATION
        /// </summary>
        SystemPortableWorkspaceEfiLauncherInformation,

        /// <summary>
        /// SYSTEM_EXTENDED_PROCESS_INFORMATION with SYSTEM_PROCESS_INFORMATION_EXTENSION (Requires admin).
        /// </summary>
        SystemFullProcessInformation,

        /// <summary>
        /// SYSTEM_KERNEL_DEBUGGER_INFORMATION_EX
        /// </summary>
        SystemKernelDebuggerInformationEx,

        /// <summary>
        /// Undocumented (Requires SeTcbPrivilege).
        /// </summary>
        SystemBootMetadataInformation,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemSoftRebootInformation,

        /// <summary>
        /// SYSTEM_ELAM_CERTIFICATE_INFORMATION
        /// </summary>
        SystemElamCertificateInformation,

        /// <summary>
        /// OFFLINE_CRASHDUMP_CONFIGURATION_TABLE_V2
        /// </summary>
        SystemOfflineDumpConfigInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_FEATURES_INFORMATION
        /// </summary>
        SystemProcessorFeaturesInformation,

        /// <summary>
        /// NULL (Requires admin) (Flushes registry hives).
        /// </summary>
        SystemRegistryReconciliationInformation,

        /// <summary>
        /// SYSTEM_EDID_INFORMATION
        /// </summary>
        SystemEdidInformation,

        /// <summary>
        /// SYSTEM_MANUFACTURING_INFORMATION (Since THRESHOLD).
        /// </summary>
        SystemManufacturingInformation,

        /// <summary>
        /// SYSTEM_ENERGY_ESTIMATION_CONFIG_INFORMATION
        /// </summary>
        SystemEnergyEstimationConfigInformation,

        /// <summary>
        /// SYSTEM_HYPERVISOR_DETAIL_INFORMATION
        /// </summary>
        SystemHypervisorDetailInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_CYCLE_STATS_INFORMATION (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorCycleStatsInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemVmGenerationCountInformation,

        /// <summary>
        /// SYSTEM_TPM_INFORMATION
        /// </summary>
        SystemTrustedPlatformModuleInformation,

        /// <summary>
        /// SYSTEM_KERNEL_DEBUGGER_FLAGS
        /// </summary>
        SystemKernelDebuggerFlags,

        /// <summary>
        /// SYSTEM_CODEINTEGRITYPOLICY_INFORMATION
        /// </summary>
        SystemCodeIntegrityPolicyInformation,

        /// <summary>
        /// SYSTEM_ISOLATED_USER_MODE_INFORMATION
        /// </summary>
        SystemIsolatedUserModeInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemHardwareSecurityTestInterfaceResultsInformation,

        /// <summary>
        /// SYSTEM_SINGLE_MODULE_INFORMATION
        /// </summary>
        SystemSingleModuleInformation,

        /// <summary>
        /// SYSTEM_WORKLOAD_ALLOWED_CPU_SET_INFORMATION
        /// </summary>
        SystemAllowedCpuSetsInformation,

        /// <summary>
        /// SYSTEM_VSM_PROTECTION_INFORMATION (previously SystemDmaProtectionInformation).
        /// </summary>
        SystemVsmProtectionInformation,

        /// <summary>
        /// SYSTEM_INTERRUPT_CPU_SET_INFORMATION
        /// </summary>
        SystemInterruptCpuSetsInformation,

        /// <summary>
        /// SYSTEM_SECUREBOOT_POLICY_FULL_INFORMATION
        /// </summary>
        SystemSecureBootPolicyFullInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemCodeIntegrityPolicyFullInformation,

        /// <summary>
        /// KAFFINITY_EX (Requires SeIncreaseBasePriorityPrivilege).
        /// </summary>
        SystemAffinitizedInterruptProcessorInformation,

        /// <summary>
        /// SYSTEM_ROOT_SILO_INFORMATION
        /// </summary>
        SystemRootSiloInformation,

        /// <summary>
        /// SYSTEM_CPU_SET_INFORMATION (Since THRESHOLD2).
        /// </summary>
        SystemCpuSetInformation,

        /// <summary>
        /// SYSTEM_CPU_SET_TAG_INFORMATION
        /// </summary>
        SystemCpuSetTagInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemWin32WerStartCallout,

        /// <summary>
        /// SYSTEM_SECURE_KERNEL_HYPERGUARD_PROFILE_INFORMATION
        /// </summary>
        SystemSecureKernelProfileInformation,

        /// <summary>
        /// SYSTEM_SECUREBOOT_PLATFORM_MANIFEST_INFORMATION (NtQuerySystemInformationEx) (Since REDSTONE).
        /// </summary>
        SystemCodeIntegrityPlatformManifestInformation,

        /// <summary>
        /// Input: SYSTEM_INTERRUPT_STEERING_INFORMATION_INPUT
        /// Output: SYSTEM_INTERRUPT_STEERING_INFORMATION_OUTPUT
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemInterruptSteeringInformation,

        /// <summary>
        /// Input (optional): HANDLE
        /// Output: SYSTEM_SUPPORTED_PROCESSOR_ARCHITECTURES_INFORMATION[]
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemSupportedProcessorArchitectures,

        /// <summary>
        /// SYSTEM_MEMORY_USAGE_INFORMATION
        /// </summary>
        SystemMemoryUsageInformation,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_CERTIFICATE_INFORMATION
        /// </summary>
        SystemCodeIntegrityCertificateInformation,

        /// <summary>
        /// SYSTEM_PHYSICAL_MEMORY_INFORMATION (REDSTONE2).
        /// </summary>
        SystemPhysicalMemoryInformation,

        /// <summary>
        /// Undocumented (Warbird/Encrypt/Decrypt/Execute).
        /// </summary>
        SystemControlFlowTransition,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemKernelDebuggingAllowed,

        /// <summary>
        /// SYSTEM_ACTIVITY_MODERATION_EXE_STATE
        /// </summary>
        SystemActivityModerationExeState,

        /// <summary>
        /// SYSTEM_ACTIVITY_MODERATION_USER_SETTINGS
        /// </summary>
        SystemActivityModerationUserSettings,

        /// <summary>
        /// Undocumented (NtQuerySystemInformationEx).
        /// </summary>
        SystemCodeIntegrityPoliciesFullInformation,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_UNLOCK_INFORMATION
        /// </summary>
        SystemCodeIntegrityUnlockInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemIntegrityQuotaInformation,

        /// <summary>
        /// SYSTEM_FLUSH_INFORMATION
        /// </summary>
        SystemFlushInformation,

        /// <summary>
        /// ULONG_PTR[ActiveGroupCount] (Since REDSTONE3).
        /// </summary>
        SystemProcessorIdleMaskInformation,

        /// <summary>
        /// Undocumented (NtQuerySystemInformationEx).
        /// </summary>
        SystemSecureDumpEncryptionInformation,

        /// <summary>
        /// SYSTEM_WRITE_CONSTRAINT_INFORMATION
        /// </summary>
        SystemWriteConstraintInformation,

        /// <summary>
        /// SYSTEM_KERNEL_VA_SHADOW_INFORMATION
        /// </summary>
        SystemKernelVaShadowInformation,

        /// <summary>
        /// SYSTEM_HYPERVISOR_SHARED_PAGE_INFORMATION (Since REDSTONE4).
        /// </summary>
        SystemHypervisorSharedPageInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemFirmwareBootPerformanceInformation,

        /// <summary>
        /// SYSTEM_CODEINTEGRITYVERIFICATION_INFORMATION
        /// </summary>
        SystemCodeIntegrityVerificationInformation,

        /// <summary>
        /// SYSTEM_FIRMWARE_PARTITION_INFORMATION
        /// </summary>
        SystemFirmwarePartitionInformation,

        /// <summary>
        /// SYSTEM_SPECULATION_CONTROL_INFORMATION (CVE-2017-5715) (Since REDSTONE3).
        /// </summary>
        SystemSpeculationControlInformation,

        /// <summary>
        /// SYSTEM_DMA_GUARD_POLICY_INFORMATION
        /// </summary>
        SystemDmaGuardPolicyInformation,

        /// <summary>
        /// SYSTEM_ENCLAVE_LAUNCH_CONTROL_INFORMATION
        /// </summary>
        SystemEnclaveLaunchControlInformation,

        /// <summary>
        /// SYSTEM_WORKLOAD_ALLOWED_CPU_SET_INFORMATION (Since REDSTONE5).
        /// </summary>
        SystemWorkloadAllowedCpuSetsInformation,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_UNLOCK_INFORMATION
        /// </summary>
        SystemCodeIntegrityUnlockModeInformation,

        /// <summary>
        /// SYSTEM_LEAP_SECOND_INFORMATION
        /// </summary>
        SystemLeapSecondInformation,

        /// <summary>
        /// SYSTEM_FLAGS_INFORMATION
        /// </summary>
        SystemFlags2Information,

        /// <summary>
        /// SYSTEM_SECURITY_MODEL_INFORMATION (Since 19H1).
        /// </summary>
        SystemSecurityModelInformation,

        /// <summary>
        /// Undocumented (NtQuerySystemInformationEx).
        /// </summary>
        SystemCodeIntegritySyntheticCacheInformation,

        /// <summary>
        /// Query input: SYSTEM_FEATURE_CONFIGURATION_QUERY
        /// Query output: SYSTEM_FEATURE_CONFIGURATION_INFORMATION
        /// Set: SYSTEM_FEATURE_CONFIGURATION_UPDATE
        /// (NtQuerySystemInformationEx)
        /// (Since 20H1).
        /// </summary>
        SystemFeatureConfigurationInformation,

        /// <summary>
        /// Input: SYSTEM_FEATURE_CONFIGURATION_SECTIONS_REQUEST
        /// Output: SYSTEM_FEATURE_CONFIGURATION_SECTIONS_INFORMATION
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemFeatureConfigurationSectionInformation,

        /// <summary>
        /// Query: SYSTEM_FEATURE_USAGE_SUBSCRIPTION_DETAILS
        /// Set: SYSTEM_FEATURE_USAGE_SUBSCRIPTION_UPDATE
        /// </summary>
        SystemFeatureUsageSubscriptionInformation,

        /// <summary>
        /// SECURE_SPECULATION_CONTROL_INFORMATION
        /// </summary>
        SystemSecureSpeculationControlInformation,

        /// <summary>
        /// Undocumented (Since 20H2).
        /// </summary>
        SystemSpacesBootInformation,

        /// <summary>
        /// SYSTEM_FIRMWARE_RAMDISK_INFORMATION
        /// </summary>
        SystemFwRamdiskInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemWheaIpmiHardwareInformation,

        /// <summary>
        /// SYSTEM_DIF_VOLATILE_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifSetRuleClassInformation,

        /// <summary>
        /// NULL (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifClearRuleClassInformation,

        /// <summary>
        /// SYSTEM_DIF_PLUGIN_DRIVER_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifApplyPluginVerificationOnDriver,

        /// <summary>
        /// SYSTEM_DIF_PLUGIN_DRIVER_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifRemovePluginVerificationOnDriver,

        /// <summary>
        /// SYSTEM_SHADOW_STACK_INFORMATION
        /// </summary>
        SystemShadowStackInformation,

        /// <summary>
        /// Input: ULONG (LayerNumber)
        /// Output: SYSTEM_BUILD_VERSION_INFORMATION
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemBuildVersionInformation,

        /// <summary>
        /// SYSTEM_POOL_LIMIT_INFORMATION (Requires SeIncreaseQuotaPrivilege) (NtQuerySystemInformationEx).
        /// </summary>
        SystemPoolLimitInformation,

        /// <summary>
        /// CodeIntegrity-AllowConfigurablePolicy-CustomKernelSigners
        /// </summary>
        SystemCodeIntegrityAddDynamicStore,

        /// <summary>
        /// CodeIntegrity-AllowConfigurablePolicy-CustomKernelSigners
        /// </summary>
        SystemCodeIntegrityClearDynamicStores,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemDifPoolTrackingInformation,

        /// <summary>
        /// SYSTEM_POOL_ZEROING_INFORMATION
        /// </summary>
        SystemPoolZeroingInformation,

        /// <summary>
        /// SYSTEM_DPC_WATCHDOG_CONFIGURATION_INFORMATION
        /// </summary>
        SystemDpcWatchdogInformation,

        /// <summary>
        /// SYSTEM_DPC_WATCHDOG_CONFIGURATION_INFORMATION_V2
        /// </summary>
        SystemDpcWatchdogInformation2,

        /// <summary>
        /// Input (optional): HANDLE
        /// Output: SYSTEM_SUPPORTED_PROCESSOR_ARCHITECTURES_INFORMATION[] (NtQuerySystemInformationEx).
        /// </summary>
        SystemSupportedProcessorArchitectures2,

        /// <summary>
        /// SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX (NtQuerySystemInformationEx).
        /// </summary>
        SystemSingleProcessorRelationshipInformation,

        /// <summary>
        /// SYSTEM_XFG_FAILURE_INFORMATION
        /// </summary>
        SystemXfgCheckFailureInformation,

        /// <summary>
        /// SYSTEM_IOMMU_STATE_INFORMATION (Since 22H1).
        /// </summary>
        SystemIommuStateInformation,

        /// <summary>
        /// SYSTEM_HYPERVISOR_MINROOT_INFORMATION
        /// </summary>
        SystemHypervisorMinrootInformation,

        /// <summary>
        /// SYSTEM_HYPERVISOR_BOOT_PAGES_INFORMATION
        /// </summary>
        SystemHypervisorBootPagesInformation,

        /// <summary>
        /// SYSTEM_POINTER_AUTH_INFORMATION
        /// </summary>
        SystemPointerAuthInformation,

        /// <summary>
        /// NtQuerySystemInformationEx
        /// </summary>
        SystemSecureKernelDebuggerInformation,

        /// <summary>
        /// Input: SYSTEM_ORIGINAL_IMAGE_FEATURE_INFORMATION_INPUT
        /// Output: SYSTEM_ORIGINAL_IMAGE_FEATURE_INFORMATION_OUTPUT
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemOriginalImageFeatureInformation,

        /// <summary>
        /// Input: SYSTEM_MEMORY_NUMA_INFORMATION_INPUT
        /// Output: SYSTEM_MEMORY_NUMA_INFORMATION_OUTPUT
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemMemoryNumaInformation,

        /// <summary>
        /// Input: SYSTEM_MEMORY_NUMA_PERFORMANCE_INFORMATION_INPUT
        /// Output: SYSTEM_MEMORY_NUMA_PERFORMANCE_INFORMATION_OUTPUT
        /// (Since 24H2).
        /// </summary>
        SystemMemoryNumaPerformanceInformation,

        /// <summary>
        /// Requires NtQuerySystemInformationEx().
        /// </summary>
        SystemCodeIntegritySignedPoliciesFullInformation,

        /// <summary>
        /// SystemSecureSecretsInformation
        /// </summary>
        SystemSecureCoreInformation,

        /// <summary>
        /// SYSTEM_TRUSTEDAPPS_RUNTIME_INFORMATION
        /// </summary>
        SystemTrustedAppsRuntimeInformation,

        /// <summary>
        /// SYSTEM_BAD_PAGE_INFORMATION
        /// </summary>
        SystemBadPageInformationEx,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemResourceDeadlockTimeout,

        /// <summary>
        /// ULONG (Requires SeDebugPrivilege).
        /// </summary>
        SystemBreakOnContextUnwindFailureInformation,

        /// <summary>
        /// SYSTEM_OSL_RAMDISK_INFORMATION
        /// </summary>
        SystemOslRamdiskInformation,

        /// <summary>
        /// SYSTEM_CODEINTEGRITYPOLICY_MANAGEMENT (Since 25H2).
        /// </summary>
        SystemCodeIntegrityPolicyManagementInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemMemoryNumaCacheInformation,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemProcessorFeaturesBitMapInformation,

        /// <summary>
        /// SYSTEM_REF_TRACE_INFORMATION_EX
        /// </summary>
        SystemRefTraceInformationEx,

        /// <summary>
        /// SYSTEM_BASICPROCESS_INFORMATION
        /// </summary>
        SystemBasicProcessInformation,

        /// <summary>
        /// SYSTEM_HANDLECOUNT_INFORMATION
        /// </summary>
        SystemHandleCountInformation,

        /// <summary>
        /// Represents the maximum value for system information classes.
        /// </summary>
        /// <remarks>This enumeration is used to define the upper limit for system information classes
        /// that can be queried or set. It is typically used in conjunction with system-level APIs that require
        /// specifying a class of system information.</remarks>
        MaxSystemInfoClass,
    }
}
