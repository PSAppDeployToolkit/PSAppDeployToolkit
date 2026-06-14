namespace PSADT.Interop
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
        SystemProcessorInformation = 1,

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
        SystemPathInformation = 4,

        /// <summary>
        /// SYSTEM_PROCESS_INFORMATION
        /// </summary>
        SystemProcessInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemProcessInformation,

        /// <summary>
        /// SYSTEM_CALL_COUNT_INFORMATION
        /// </summary>
        SystemCallCountInformation = 6,

        /// <summary>
        /// SYSTEM_DEVICE_INFORMATION
        /// </summary>
        SystemDeviceInformation = 7,

        /// <summary>
        /// SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION
        /// </summary>
        SystemProcessorPerformanceInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemProcessorPerformanceInformation,

        /// <summary>
        /// SYSTEM_FLAGS_INFORMATION
        /// </summary>
        SystemFlagsInformation = 9,

        /// <summary>
        /// SYSTEM_CALL_TIME_INFORMATION (Not implemented).
        /// </summary>
        SystemCallTimeInformation = 10,

        /// <summary>
        /// RTL_PROCESS_MODULES
        /// </summary>
        SystemModuleInformation = 11,

        /// <summary>
        /// RTL_PROCESS_LOCKS
        /// </summary>
        SystemLocksInformation = 12,

        /// <summary>
        /// RTL_PROCESS_BACKTRACES
        /// </summary>
        SystemStackTraceInformation = 13,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemPagedPoolInformation = 14,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemNonPagedPoolInformation = 15,

        /// <summary>
        /// SYSTEM_HANDLE_INFORMATION
        /// </summary>
        SystemHandleInformation = 16,

        /// <summary>
        /// SYSTEM_OBJECTTYPE_INFORMATION/SYSTEM_OBJECT_INFORMATION
        /// </summary>
        SystemObjectInformation = 17,

        /// <summary>
        /// SYSTEM_PAGEFILE_INFORMATION
        /// </summary>
        SystemPageFileInformation = 18,

        /// <summary>
        /// SYSTEM_VDM_INSTEMUL_INFO
        /// </summary>
        SystemVdmInstemulInformation = 19,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemVdmBopInformation = 20,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (info for WorkingSetTypeSystemCache).
        /// </summary>
        SystemFileCacheInformation = 21,

        /// <summary>
        /// SYSTEM_POOLTAG_INFORMATION
        /// </summary>
        SystemPoolTagInformation = 22,

        /// <summary>
        /// SYSTEM_INTERRUPT_INFORMATION
        /// </summary>
        SystemInterruptInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemInterruptInformation,

        /// <summary>
        /// SYSTEM_DPC_BEHAVIOR_INFORMATION (Requires SeLoadDriverPrivilege).
        /// </summary>
        SystemDpcBehaviorInformation = 24,

        /// <summary>
        /// SYSTEM_MEMORY_USAGE_INFORMATION (Not implemented).
        /// </summary>
        SystemFullMemoryInformation = 25,

        /// <summary>
        /// Kernel-mode only.
        /// </summary>
        SystemLoadGdiDriverInformation = 26,

        /// <summary>
        /// Kernel-mode only.
        /// </summary>
        SystemUnloadGdiDriverInformation = 27,

        /// <summary>
        /// Query: SYSTEM_QUERY_TIME_ADJUST_INFORMATION;
        /// Set: SYSTEM_SET_TIME_ADJUST_INFORMATION
        /// (Requires SeSystemtimePrivilege).
        /// </summary>
        SystemTimeAdjustmentInformation = 28,

        /// <summary>
        /// SYSTEM_MEMORY_USAGE_INFORMATION (Not implemented).
        /// </summary>
        SystemSummaryMemoryInformation = 29,

        /// <summary>
        /// Undocumented (Requires license value "Kernel-MemoryMirroringSupported") (Requires SeShutdownPrivilege).
        /// </summary>
        SystemMirrorMemoryInformation = 30,

        /// <summary>
        /// EVENT_TRACE_INFORMATION_CLASS
        /// </summary>
        SystemPerformanceTraceInformation = 31,

        /// <summary>
        /// Not implemented (previously SystemCrashDumpInformation).
        /// </summary>
        SystemObsolete0 = 32,

        /// <summary>
        /// SYSTEM_EXCEPTION_INFORMATION
        /// </summary>
        SystemExceptionInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemExceptionInformation,

        /// <summary>
        /// SYSTEM_CRASH_DUMP_STATE_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemCrashDumpStateInformation = 34,

        /// <summary>
        /// SYSTEM_KERNEL_DEBUGGER_INFORMATION
        /// </summary>
        SystemKernelDebuggerInformation = 35,

        /// <summary>
        /// SYSTEM_CONTEXT_SWITCH_INFORMATION
        /// </summary>
        SystemContextSwitchInformation = 36,

        /// <summary>
        /// SYSTEM_REGISTRY_QUOTA_INFORMATION (Requires SeIncreaseQuotaPrivilege).
        /// </summary>
        SystemRegistryQuotaInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemRegistryQuotaInformation,

        /// <summary>
        /// Undocumented (Requires SeLoadDriverPrivilege).
        /// </summary>
        SystemExtendServiceTableInformation = 38,

        /// <summary>
        /// Undocumented (Requires SeTcbPrivilege).
        /// </summary>
        SystemPrioritySeparation = 39,

        /// <summary>
        /// UNICODE_STRING (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierAddDriverInformation = 40,

        /// <summary>
        /// UNICODE_STRING (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierRemoveDriverInformation = 41,

        /// <summary>
        /// SYSTEM_PROCESSOR_IDLE_INFORMATION
        /// </summary>
        SystemProcessorIdleInformation = 42,

        /// <summary>
        /// SYSTEM_LEGACY_DRIVER_INFORMATION
        /// </summary>
        SystemLegacyDriverInformation = 43,

        /// <summary>
        /// RTL_TIME_ZONE_INFORMATION
        /// </summary>
        SystemCurrentTimeZoneInformation = 44,

        /// <summary>
        /// SYSTEM_LOOKASIDE_INFORMATION
        /// </summary>
        SystemLookasideInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemLookasideInformation,

        /// <summary>
        /// HANDLE (NtCreateEvent) (Requires SeSystemtimePrivilege).
        /// </summary>
        SystemTimeSlipNotification = 46,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemSessionCreate = 47,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemSessionDetach = 48,

        /// <summary>
        /// SYSTEM_SESSION_INFORMATION (Not implemented).
        /// </summary>
        SystemSessionInformation = 49,

        /// <summary>
        /// SYSTEM_RANGE_START_INFORMATION
        /// </summary>
        SystemRangeStartInformation = 50,

        /// <summary>
        /// SYSTEM_VERIFIER_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierInformation = 51,

        /// <summary>
        /// Kernel-mode only.
        /// </summary>
        SystemVerifierThunkExtend = 52,

        /// <summary>
        /// SYSTEM_SESSION_PROCESS_INFORMATION
        /// </summary>
        SystemSessionProcessInformation = 53,

        /// <summary>
        /// SYSTEM_GDI_DRIVER_INFORMATION (Kernel-mode only) (Same as SystemLoadGdiDriverInformation).
        /// </summary>
        SystemLoadGdiDriverInSystemSpace = 54,

        /// <summary>
        /// SYSTEM_NUMA_INFORMATION
        /// </summary>
        SystemNumaProcessorMap = 55,

        /// <summary>
        /// PREFETCHER_INFORMATION (PfSnQueryPrefetcherInformation).
        /// </summary>
        SystemPrefetcherInformation = 56,

        /// <summary>
        /// SYSTEM_EXTENDED_PROCESS_INFORMATION
        /// </summary>
        SystemExtendedProcessInformation = 57,

        /// <summary>
        /// ULONG (KeGetRecommendedSharedDataAlignment).
        /// </summary>
        SystemRecommendedSharedDataAlignment = 58,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemComPlusPackage = 59,

        /// <summary>
        /// SYSTEM_NUMA_INFORMATION
        /// </summary>
        SystemNumaAvailableMemory = 60,

        /// <summary>
        /// SYSTEM_PROCESSOR_POWER_INFORMATION
        /// </summary>
        SystemProcessorPowerInformation = 61,

        /// <summary>
        /// SYSTEM_BASIC_INFORMATION
        /// </summary>
        SystemEmulationBasicInformation = 62,

        /// <summary>
        /// SYSTEM_PROCESSOR_INFORMATION
        /// </summary>
        SystemEmulationProcessorInformation = 63,

        /// <summary>
        /// SYSTEM_HANDLE_INFORMATION_EX
        /// </summary>
        SystemExtendedHandleInformation = 64,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemLostDelayedWriteInformation = 65,

        /// <summary>
        /// SYSTEM_BIGPOOL_INFORMATION
        /// </summary>
        SystemBigPoolInformation = 66,

        /// <summary>
        /// SYSTEM_SESSION_POOLTAG_INFORMATION
        /// </summary>
        SystemSessionPoolTagInformation = 67,

        /// <summary>
        /// SYSTEM_SESSION_MAPPED_VIEW_INFORMATION
        /// </summary>
        SystemSessionMappedViewInformation = 68,

        /// <summary>
        /// SYSTEM_HOTPATCH_CODE_INFORMATION
        /// </summary>
        SystemHotpatchInformation = 69,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemObjectSecurityMode = 70,

        /// <summary>
        /// SYSTEM_WATCHDOG_HANDLER_INFORMATION (Kernel-mode only).
        /// </summary>
        SystemWatchdogTimerHandler = 71,

        /// <summary>
        /// SYSTEM_WATCHDOG_TIMER_INFORMATION (NtQuerySystemInformationEx) (Kernel-mode only).
        /// </summary>
        SystemWatchdogTimerInformation = 72,

        /// <summary>
        /// SYSTEM_LOGICAL_PROCESSOR_INFORMATION (NtQuerySystemInformationEx).
        /// </summary>
        SystemLogicalProcessorInformation = 73,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemWow64SharedInformationObsolete = 74,

        /// <summary>
        /// SYSTEM_FIRMWARE_TABLE_HANDLER (Kernel-mode only).
        /// </summary>
        SystemRegisterFirmwareTableInformationHandler = 75,

        /// <summary>
        /// SYSTEM_FIRMWARE_TABLE_INFORMATION
        /// </summary>
        SystemFirmwareTableInformation = 76,

        /// <summary>
        /// RTL_PROCESS_MODULE_INFORMATION_EX (Since VISTA).
        /// </summary>
        SystemModuleInformationEx = 77,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemVerifierTriageInformation = 78,

        /// <summary>
        /// SUPERFETCH_INFORMATION (PfQuerySuperfetchInformation).
        /// </summary>
        SystemSuperfetchInformation = 79,

        /// <summary>
        /// Query: SYSTEM_MEMORY_LIST_INFORMATION;
        /// Set: SYSTEM_MEMORY_LIST_COMMAND
        /// Requires SeProfileSingleProcessPrivilege.
        /// </summary>
        SystemMemoryListInformation = 80,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (Same as SystemFileCacheInformation).
        /// </summary>
        SystemFileCacheInformationEx = 81,

        /// <summary>
        /// SYSTEM_THREAD_CID_PRIORITY_INFORMATION (Requires SeIncreaseBasePriorityPrivilege) (NtQuerySystemInformationEx).
        /// </summary>
        SystemThreadPriorityClientIdInformation = 82,

        /// <summary>
        /// SYSTEM_PROCESSOR_IDLE_CYCLE_TIME_INFORMATION[] (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorIdleCycleTimeInformation = 83,

        /// <summary>
        /// SYSTEM_VERIFIER_CANCELLATION_INFORMATION (WOW64 name: whNT32QuerySystemVerifierCancellationInformation).
        /// </summary>
        SystemVerifierCancellationInformation = 84,

        /// <summary>
        /// Not implemented.
        /// </summary>
        SystemProcessorPowerInformationEx = 85,

        /// <summary>
        /// SYSTEM_REF_TRACE_INFORMATION (ObQueryRefTraceInformation).
        /// </summary>
        SystemRefTraceInformation = 86,

        /// <summary>
        /// SYSTEM_SPECIAL_POOL_INFORMATION (Requires SeDebugPrivilege) (MmSpecialPoolTag, then MmSpecialPoolCatchOverruns != 0).
        /// </summary>
        SystemSpecialPoolInformation = 87,

        /// <summary>
        /// SYSTEM_PROCESS_ID_INFORMATION
        /// </summary>
        SystemProcessIdInformation = 88,

        /// <summary>
        /// Undocumented (Requires SeTcbPrivilege).
        /// </summary>
        SystemErrorPortInformation = 89,

        /// <summary>
        /// SYSTEM_BOOT_ENVIRONMENT_INFORMATION
        /// </summary>
        SystemBootEnvironmentInformation = 90,

        /// <summary>
        /// SYSTEM_HYPERVISOR_QUERY_INFORMATION
        /// </summary>
        SystemHypervisorInformation = 91,

        /// <summary>
        /// SYSTEM_VERIFIER_INFORMATION_EX
        /// </summary>
        SystemVerifierInformationEx = 92,

        /// <summary>
        /// RTL_TIME_ZONE_INFORMATION (Requires SeTimeZonePrivilege).
        /// </summary>
        SystemTimeZoneInformation = 93,

        /// <summary>
        /// SYSTEM_IMAGE_FILE_EXECUTION_OPTIONS_INFORMATION (Requires SeTcbPrivilege).
        /// </summary>
        SystemImageFileExecutionOptionsInformation = 94,

        /// <summary>
        /// Query: COVERAGE_MODULES
        /// Set: COVERAGE_MODULE_REQUEST
        /// (ExpCovQueryInformation)
        /// (Requires SeDebugPrivilege)
        /// </summary>
        SystemCoverageInformation = 95,

        /// <summary>
        /// SYSTEM_PREFETCH_PATCH_INFORMATION
        /// </summary>
        SystemPrefetchPatchInformation = 96,

        /// <summary>
        /// SYSTEM_VERIFIER_FAULTS_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemVerifierFaultsInformation = 97,

        /// <summary>
        /// SYSTEM_SYSTEM_PARTITION_INFORMATION
        /// </summary>
        SystemSystemPartitionInformation = 98,

        /// <summary>
        /// SYSTEM_SYSTEM_DISK_INFORMATION
        /// </summary>
        SystemSystemDiskInformation = 99,

        /// <summary>
        /// SYSTEM_PROCESSOR_PERFORMANCE_DISTRIBUTION (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorPerformanceDistribution = 100,

        /// <summary>
        /// SYSTEM_NUMA_PROXIMITY_MAP
        /// </summary>
        SystemNumaProximityNodeInformation = 101,

        /// <summary>
        /// RTL_DYNAMIC_TIME_ZONE_INFORMATION (Requires SeTimeZonePrivilege).
        /// </summary>
        SystemDynamicTimeZoneInformation = 102,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_INFORMATION (SeCodeIntegrityQueryInformation).
        /// </summary>
        SystemCodeIntegrityInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemCodeIntegrityInformation,

        /// <summary>
        /// SYSTEM_PROCESSOR_MICROCODE_UPDATE_INFORMATION
        /// </summary>
        SystemProcessorMicrocodeUpdateInformation = 104,

        /// <summary>
        /// CHAR[] (HaliQuerySystemInformation -> HalpGetProcessorBrandString; Info class 23).
        /// </summary>
        SystemProcessorBrandString = 105,

        /// <summary>
        /// SYSTEM_VA_LIST_INFORMATION[] (Requires SeIncreaseQuotaPrivilege) (MmQuerySystemVaInformation).
        /// </summary>
        SystemVirtualAddressInformation = 106,

        /// <summary>
        /// SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX (Since WIN7) (NtQuerySystemInformationEx) (KeQueryLogicalProcessorRelationship).
        /// </summary>
        SystemLogicalProcessorAndGroupInformation = 107,

        /// <summary>
        /// SYSTEM_PROCESSOR_CYCLE_TIME_INFORMATION[] (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorCycleTimeInformation = 108,

        /// <summary>
        /// SYSTEM_STORE_INFORMATION (Requires SeProfileSingleProcessPrivilege) (SmQueryStoreInformation).
        /// </summary>
        SystemStoreInformation = 109,

        /// <summary>
        /// SYSTEM_REGISTRY_APPEND_STRING_PARAMETERS
        /// </summary>
        SystemRegistryAppendString = 110,

        /// <summary>
        /// ULONG (Requires SeProfileSingleProcessPrivilege).
        /// </summary>
        SystemAitSamplingValue = 111,

        /// <summary>
        /// SYSTEM_VHD_BOOT_INFORMATION
        /// </summary>
        SystemVhdBootInformation = 112,

        /// <summary>
        /// PS_CPU_QUOTA_QUERY_INFORMATION
        /// </summary>
        SystemCpuQuotaInformation = 113,

        /// <summary>
        /// SYSTEM_BASIC_INFORMATION
        /// </summary>
        SystemNativeBasicInformation = 114,

        /// <summary>
        /// SYSTEM_ERROR_PORT_TIMEOUTS
        /// </summary>
        SystemErrorPortTimeouts = 115,

        /// <summary>
        /// SYSTEM_LOW_PRIORITY_IO_INFORMATION
        /// </summary>
        SystemLowPriorityIoInformation = 116,

        /// <summary>
        /// BOOT_ENTROPY_NT_RESULT (ExQueryBootEntropyInformation).
        /// </summary>
        SystemTpmBootEntropyInformation = 117,

        /// <summary>
        /// SYSTEM_VERIFIER_COUNTERS_INFORMATION
        /// </summary>
        SystemVerifierCountersInformation = 118,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (Info for WorkingSetTypePagedPool).
        /// </summary>
        SystemPagedPoolInformationEx = 119,

        /// <summary>
        /// SYSTEM_FILECACHE_INFORMATION (Requires SeIncreaseQuotaPrivilege) (Info for WorkingSetTypeSystemPtes).
        /// </summary>
        SystemSystemPtesInformationEx = 120,

        /// <summary>
        /// USHORT[4*NumaNodes] (NtQuerySystemInformationEx).
        /// </summary>
        SystemNodeDistanceInformation = 121,

        /// <summary>
        /// SYSTEM_ACPI_AUDIT_INFORMATION (HaliQuerySystemInformation -> HalpAuditQueryResults; Info class 26).
        /// </summary>
        SystemAcpiAuditInformation = 122,

        /// <summary>
        /// SYSTEM_BASIC_PERFORMANCE_INFORMATION (WOW64 name: whNtQuerySystemInformation_SystemBasicPerformanceInformation).
        /// </summary>
        SystemBasicPerformanceInformation = 123,

        /// <summary>
        /// SYSTEM_QUERY_PERFORMANCE_COUNTER_INFORMATION (Since WIN7 SP1).
        /// </summary>
        SystemQueryPerformanceCounterInformation = 124,

        /// <summary>
        /// SYSTEM_SESSION_POOLTAG_INFORMATION (Since WIN8).
        /// </summary>
        SystemSessionBigPoolInformation = 125,

        /// <summary>
        /// SYSTEM_BOOT_GRAPHICS_INFORMATION (Kernel-mode only).
        /// </summary>
        SystemBootGraphicsInformation = 126,

        /// <summary>
        /// MEMORY_SCRUB_INFORMATION
        /// </summary>
        SystemScrubPhysicalMemoryInformation = 127,

        /// <summary>
        /// SYSTEM_BAD_PAGE_INFORMATION
        /// </summary>
        SystemBadPageInformation = 128,

        /// <summary>
        /// SYSTEM_PROCESSOR_PROFILE_CONTROL_AREA
        /// </summary>
        SystemProcessorProfileControlArea = 129,

        /// <summary>
        /// MEMORY_COMBINE_INFORMATION, MEMORY_COMBINE_INFORMATION_EX, MEMORY_COMBINE_INFORMATION_EX2
        /// </summary>
        SystemCombinePhysicalMemoryInformation = 130,

        /// <summary>
        /// SYSTEM_ENTROPY_TIMING_INFORMATION
        /// </summary>
        SystemEntropyInterruptTimingInformation = 131,

        /// <summary>
        /// SYSTEM_CONSOLE_INFORMATION
        /// </summary>
        SystemConsoleInformation = 132,

        /// <summary>
        /// SYSTEM_PLATFORM_BINARY_INFORMATION (Requires SeTcbPrivilege).
        /// </summary>
        SystemPlatformBinaryInformation = 133,

        /// <summary>
        /// SYSTEM_POLICY_INFORMATION (Warbird/Encrypt/Decrypt/Execute).
        /// </summary>
        SystemPolicyInformation = Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS.SystemPolicyInformation,

        /// <summary>
        /// SYSTEM_HYPERVISOR_PROCESSOR_COUNT_INFORMATION.
        /// </summary>
        SystemHypervisorProcessorCountInformation = 135,

        /// <summary>
        /// SYSTEM_DEVICE_DATA_INFORMATION
        /// </summary>
        SystemDeviceDataInformation = 136,

        /// <summary>
        /// SYSTEM_DEVICE_DATA_INFORMATION
        /// </summary>
        SystemDeviceDataEnumerationInformation = 137,

        /// <summary>
        /// SYSTEM_MEMORY_TOPOLOGY_INFORMATION
        /// </summary>
        SystemMemoryTopologyInformation = 138,

        /// <summary>
        /// SYSTEM_MEMORY_CHANNEL_INFORMATION
        /// </summary>
        SystemMemoryChannelInformation = 139,

        /// <summary>
        /// SYSTEM_BOOT_LOGO_INFORMATION
        /// </summary>
        SystemBootLogoInformation = 140,

        /// <summary>
        /// SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION_EX (NtQuerySystemInformationEx) (Since WINBLUE).
        /// </summary>
        SystemProcessorPerformanceInformationEx = 141,

        /// <summary>
        /// CRITICAL_PROCESS_EXCEPTION_DATA
        /// </summary>
        SystemCriticalProcessErrorLogInformation = 142,

        /// <summary>
        /// SYSTEM_SECUREBOOT_POLICY_INFORMATION
        /// </summary>
        SystemSecureBootPolicyInformation = 143,

        /// <summary>
        /// SYSTEM_PAGEFILE_INFORMATION_EX
        /// </summary>
        SystemPageFileInformationEx = 144,

        /// <summary>
        /// SYSTEM_SECUREBOOT_INFORMATION
        /// </summary>
        SystemSecureBootInformation = 145,

        /// <summary>
        /// SYSTEM_ENTROPY_TIMING_INFORMATION
        /// </summary>
        SystemEntropyInterruptTimingRawInformation = 146,

        /// <summary>
        /// SYSTEM_PORTABLE_WORKSPACE_EFI_LAUNCHER_INFORMATION
        /// </summary>
        SystemPortableWorkspaceEfiLauncherInformation = 147,

        /// <summary>
        /// SYSTEM_EXTENDED_PROCESS_INFORMATION with SYSTEM_PROCESS_INFORMATION_EXTENSION (Requires admin).
        /// </summary>
        SystemFullProcessInformation = 148,

        /// <summary>
        /// SYSTEM_KERNEL_DEBUGGER_INFORMATION_EX
        /// </summary>
        SystemKernelDebuggerInformationEx = 149,

        /// <summary>
        /// Undocumented (Requires SeTcbPrivilege).
        /// </summary>
        SystemBootMetadataInformation = 150,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemSoftRebootInformation = 151,

        /// <summary>
        /// SYSTEM_ELAM_CERTIFICATE_INFORMATION
        /// </summary>
        SystemElamCertificateInformation = 152,

        /// <summary>
        /// OFFLINE_CRASHDUMP_CONFIGURATION_TABLE_V2
        /// </summary>
        SystemOfflineDumpConfigInformation = 153,

        /// <summary>
        /// SYSTEM_PROCESSOR_FEATURES_INFORMATION
        /// </summary>
        SystemProcessorFeaturesInformation = 154,

        /// <summary>
        /// NULL (Requires admin) (Flushes registry hives).
        /// </summary>
        SystemRegistryReconciliationInformation = 155,

        /// <summary>
        /// SYSTEM_EDID_INFORMATION
        /// </summary>
        SystemEdidInformation = 156,

        /// <summary>
        /// SYSTEM_MANUFACTURING_INFORMATION (Since THRESHOLD).
        /// </summary>
        SystemManufacturingInformation = 157,

        /// <summary>
        /// SYSTEM_ENERGY_ESTIMATION_CONFIG_INFORMATION
        /// </summary>
        SystemEnergyEstimationConfigInformation = 158,

        /// <summary>
        /// SYSTEM_HYPERVISOR_DETAIL_INFORMATION
        /// </summary>
        SystemHypervisorDetailInformation = 159,

        /// <summary>
        /// SYSTEM_PROCESSOR_CYCLE_STATS_INFORMATION (NtQuerySystemInformationEx).
        /// </summary>
        SystemProcessorCycleStatsInformation = 160,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemVmGenerationCountInformation = 161,

        /// <summary>
        /// SYSTEM_TPM_INFORMATION
        /// </summary>
        SystemTrustedPlatformModuleInformation = 162,

        /// <summary>
        /// SYSTEM_KERNEL_DEBUGGER_FLAGS
        /// </summary>
        SystemKernelDebuggerFlags = 163,

        /// <summary>
        /// SYSTEM_CODEINTEGRITYPOLICY_INFORMATION
        /// </summary>
        SystemCodeIntegrityPolicyInformation = 164,

        /// <summary>
        /// SYSTEM_ISOLATED_USER_MODE_INFORMATION
        /// </summary>
        SystemIsolatedUserModeInformation = 165,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemHardwareSecurityTestInterfaceResultsInformation = 166,

        /// <summary>
        /// SYSTEM_SINGLE_MODULE_INFORMATION
        /// </summary>
        SystemSingleModuleInformation = 167,

        /// <summary>
        /// SYSTEM_WORKLOAD_ALLOWED_CPU_SET_INFORMATION
        /// </summary>
        SystemAllowedCpuSetsInformation = 168,

        /// <summary>
        /// SYSTEM_VSM_PROTECTION_INFORMATION (previously SystemDmaProtectionInformation).
        /// </summary>
        SystemVsmProtectionInformation = 169,

        /// <summary>
        /// SYSTEM_INTERRUPT_CPU_SET_INFORMATION
        /// </summary>
        SystemInterruptCpuSetsInformation = 170,

        /// <summary>
        /// SYSTEM_SECUREBOOT_POLICY_FULL_INFORMATION
        /// </summary>
        SystemSecureBootPolicyFullInformation = 171,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemCodeIntegrityPolicyFullInformation = 172,

        /// <summary>
        /// KAFFINITY_EX (Requires SeIncreaseBasePriorityPrivilege).
        /// </summary>
        SystemAffinitizedInterruptProcessorInformation = 173,

        /// <summary>
        /// SYSTEM_ROOT_SILO_INFORMATION
        /// </summary>
        SystemRootSiloInformation = 174,

        /// <summary>
        /// SYSTEM_CPU_SET_INFORMATION (Since THRESHOLD2).
        /// </summary>
        SystemCpuSetInformation = 175,

        /// <summary>
        /// SYSTEM_CPU_SET_TAG_INFORMATION
        /// </summary>
        SystemCpuSetTagInformation = 176,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemWin32WerStartCallout = 177,

        /// <summary>
        /// SYSTEM_SECURE_KERNEL_HYPERGUARD_PROFILE_INFORMATION
        /// </summary>
        SystemSecureKernelProfileInformation = 178,

        /// <summary>
        /// SYSTEM_SECUREBOOT_PLATFORM_MANIFEST_INFORMATION (NtQuerySystemInformationEx) (Since REDSTONE).
        /// </summary>
        SystemCodeIntegrityPlatformManifestInformation = 179,

        /// <summary>
        /// Input: SYSTEM_INTERRUPT_STEERING_INFORMATION_INPUT
        /// Output: SYSTEM_INTERRUPT_STEERING_INFORMATION_OUTPUT
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemInterruptSteeringInformation = 180,

        /// <summary>
        /// Input (optional): HANDLE
        /// Output: SYSTEM_SUPPORTED_PROCESSOR_ARCHITECTURES_INFORMATION[]
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemSupportedProcessorArchitectures = 181,

        /// <summary>
        /// SYSTEM_MEMORY_USAGE_INFORMATION
        /// </summary>
        SystemMemoryUsageInformation = 182,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_CERTIFICATE_INFORMATION
        /// </summary>
        SystemCodeIntegrityCertificateInformation = 183,

        /// <summary>
        /// SYSTEM_PHYSICAL_MEMORY_INFORMATION (REDSTONE2).
        /// </summary>
        SystemPhysicalMemoryInformation = 184,

        /// <summary>
        /// Undocumented (Warbird/Encrypt/Decrypt/Execute).
        /// </summary>
        SystemControlFlowTransition = 185,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemKernelDebuggingAllowed = 186,

        /// <summary>
        /// SYSTEM_ACTIVITY_MODERATION_EXE_STATE
        /// </summary>
        SystemActivityModerationExeState = 187,

        /// <summary>
        /// SYSTEM_ACTIVITY_MODERATION_USER_SETTINGS
        /// </summary>
        SystemActivityModerationUserSettings = 188,

        /// <summary>
        /// Undocumented (NtQuerySystemInformationEx).
        /// </summary>
        SystemCodeIntegrityPoliciesFullInformation = 189,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_UNLOCK_INFORMATION
        /// </summary>
        SystemCodeIntegrityUnlockInformation = 190,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemIntegrityQuotaInformation = 191,

        /// <summary>
        /// SYSTEM_FLUSH_INFORMATION
        /// </summary>
        SystemFlushInformation = 192,

        /// <summary>
        /// ULONG_PTR[ActiveGroupCount] (Since REDSTONE3).
        /// </summary>
        SystemProcessorIdleMaskInformation = 193,

        /// <summary>
        /// Undocumented (NtQuerySystemInformationEx).
        /// </summary>
        SystemSecureDumpEncryptionInformation = 194,

        /// <summary>
        /// SYSTEM_WRITE_CONSTRAINT_INFORMATION
        /// </summary>
        SystemWriteConstraintInformation = 195,

        /// <summary>
        /// SYSTEM_KERNEL_VA_SHADOW_INFORMATION
        /// </summary>
        SystemKernelVaShadowInformation = 196,

        /// <summary>
        /// SYSTEM_HYPERVISOR_SHARED_PAGE_INFORMATION (Since REDSTONE4).
        /// </summary>
        SystemHypervisorSharedPageInformation = 197,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemFirmwareBootPerformanceInformation = 198,

        /// <summary>
        /// SYSTEM_CODEINTEGRITYVERIFICATION_INFORMATION
        /// </summary>
        SystemCodeIntegrityVerificationInformation = 199,

        /// <summary>
        /// SYSTEM_FIRMWARE_PARTITION_INFORMATION
        /// </summary>
        SystemFirmwarePartitionInformation = 200,

        /// <summary>
        /// SYSTEM_SPECULATION_CONTROL_INFORMATION (CVE-2017-5715) (Since REDSTONE3).
        /// </summary>
        SystemSpeculationControlInformation = 201,

        /// <summary>
        /// SYSTEM_DMA_GUARD_POLICY_INFORMATION
        /// </summary>
        SystemDmaGuardPolicyInformation = 202,

        /// <summary>
        /// SYSTEM_ENCLAVE_LAUNCH_CONTROL_INFORMATION
        /// </summary>
        SystemEnclaveLaunchControlInformation = 203,

        /// <summary>
        /// SYSTEM_WORKLOAD_ALLOWED_CPU_SET_INFORMATION (Since REDSTONE5).
        /// </summary>
        SystemWorkloadAllowedCpuSetsInformation = 204,

        /// <summary>
        /// SYSTEM_CODEINTEGRITY_UNLOCK_INFORMATION
        /// </summary>
        SystemCodeIntegrityUnlockModeInformation = 205,

        /// <summary>
        /// SYSTEM_LEAP_SECOND_INFORMATION
        /// </summary>
        SystemLeapSecondInformation = 206,

        /// <summary>
        /// SYSTEM_FLAGS_INFORMATION
        /// </summary>
        SystemFlags2Information = 207,

        /// <summary>
        /// SYSTEM_SECURITY_MODEL_INFORMATION (Since 19H1).
        /// </summary>
        SystemSecurityModelInformation = 208,

        /// <summary>
        /// Undocumented (NtQuerySystemInformationEx).
        /// </summary>
        SystemCodeIntegritySyntheticCacheInformation = 209,

        /// <summary>
        /// Query input: SYSTEM_FEATURE_CONFIGURATION_QUERY
        /// Query output: SYSTEM_FEATURE_CONFIGURATION_INFORMATION
        /// Set: SYSTEM_FEATURE_CONFIGURATION_UPDATE
        /// (NtQuerySystemInformationEx)
        /// (Since 20H1).
        /// </summary>
        SystemFeatureConfigurationInformation = 210,

        /// <summary>
        /// Input: SYSTEM_FEATURE_CONFIGURATION_SECTIONS_REQUEST
        /// Output: SYSTEM_FEATURE_CONFIGURATION_SECTIONS_INFORMATION
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemFeatureConfigurationSectionInformation = 211,

        /// <summary>
        /// Query: SYSTEM_FEATURE_USAGE_SUBSCRIPTION_DETAILS
        /// Set: SYSTEM_FEATURE_USAGE_SUBSCRIPTION_UPDATE
        /// </summary>
        SystemFeatureUsageSubscriptionInformation = 212,

        /// <summary>
        /// SECURE_SPECULATION_CONTROL_INFORMATION
        /// </summary>
        SystemSecureSpeculationControlInformation = 213,

        /// <summary>
        /// Undocumented (Since 20H2).
        /// </summary>
        SystemSpacesBootInformation = 214,

        /// <summary>
        /// SYSTEM_FIRMWARE_RAMDISK_INFORMATION
        /// </summary>
        SystemFwRamdiskInformation = 215,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemWheaIpmiHardwareInformation = 216,

        /// <summary>
        /// SYSTEM_DIF_VOLATILE_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifSetRuleClassInformation = 217,

        /// <summary>
        /// NULL (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifClearRuleClassInformation = 218,

        /// <summary>
        /// SYSTEM_DIF_PLUGIN_DRIVER_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifApplyPluginVerificationOnDriver = 219,

        /// <summary>
        /// SYSTEM_DIF_PLUGIN_DRIVER_INFORMATION (Requires SeDebugPrivilege).
        /// </summary>
        SystemDifRemovePluginVerificationOnDriver = 220,

        /// <summary>
        /// SYSTEM_SHADOW_STACK_INFORMATION
        /// </summary>
        SystemShadowStackInformation = 221,

        /// <summary>
        /// Input: ULONG (LayerNumber)
        /// Output: SYSTEM_BUILD_VERSION_INFORMATION
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemBuildVersionInformation = 222,

        /// <summary>
        /// SYSTEM_POOL_LIMIT_INFORMATION (Requires SeIncreaseQuotaPrivilege) (NtQuerySystemInformationEx).
        /// </summary>
        SystemPoolLimitInformation = 223,

        /// <summary>
        /// CodeIntegrity-AllowConfigurablePolicy-CustomKernelSigners
        /// </summary>
        SystemCodeIntegrityAddDynamicStore = 224,

        /// <summary>
        /// CodeIntegrity-AllowConfigurablePolicy-CustomKernelSigners
        /// </summary>
        SystemCodeIntegrityClearDynamicStores = 225,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemDifPoolTrackingInformation = 226,

        /// <summary>
        /// SYSTEM_POOL_ZEROING_INFORMATION
        /// </summary>
        SystemPoolZeroingInformation = 227,

        /// <summary>
        /// SYSTEM_DPC_WATCHDOG_CONFIGURATION_INFORMATION
        /// </summary>
        SystemDpcWatchdogInformation = 228,

        /// <summary>
        /// SYSTEM_DPC_WATCHDOG_CONFIGURATION_INFORMATION_V2
        /// </summary>
        SystemDpcWatchdogInformation2 = 229,

        /// <summary>
        /// Input (optional): HANDLE
        /// Output: SYSTEM_SUPPORTED_PROCESSOR_ARCHITECTURES_INFORMATION[] (NtQuerySystemInformationEx).
        /// </summary>
        SystemSupportedProcessorArchitectures2 = 230,

        /// <summary>
        /// SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX (NtQuerySystemInformationEx).
        /// </summary>
        SystemSingleProcessorRelationshipInformation = 231,

        /// <summary>
        /// SYSTEM_XFG_FAILURE_INFORMATION
        /// </summary>
        SystemXfgCheckFailureInformation = 232,

        /// <summary>
        /// SYSTEM_IOMMU_STATE_INFORMATION (Since 22H1).
        /// </summary>
        SystemIommuStateInformation = 233,

        /// <summary>
        /// SYSTEM_HYPERVISOR_MINROOT_INFORMATION
        /// </summary>
        SystemHypervisorMinrootInformation = 234,

        /// <summary>
        /// SYSTEM_HYPERVISOR_BOOT_PAGES_INFORMATION
        /// </summary>
        SystemHypervisorBootPagesInformation = 235,

        /// <summary>
        /// SYSTEM_POINTER_AUTH_INFORMATION
        /// </summary>
        SystemPointerAuthInformation = 236,

        /// <summary>
        /// NtQuerySystemInformationEx
        /// </summary>
        SystemSecureKernelDebuggerInformation = 237,

        /// <summary>
        /// Input: SYSTEM_ORIGINAL_IMAGE_FEATURE_INFORMATION_INPUT
        /// Output: SYSTEM_ORIGINAL_IMAGE_FEATURE_INFORMATION_OUTPUT
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemOriginalImageFeatureInformation = 238,

        /// <summary>
        /// Input: SYSTEM_MEMORY_NUMA_INFORMATION_INPUT
        /// Output: SYSTEM_MEMORY_NUMA_INFORMATION_OUTPUT
        /// (NtQuerySystemInformationEx).
        /// </summary>
        SystemMemoryNumaInformation = 239,

        /// <summary>
        /// Input: SYSTEM_MEMORY_NUMA_PERFORMANCE_INFORMATION_INPUT
        /// Output: SYSTEM_MEMORY_NUMA_PERFORMANCE_INFORMATION_OUTPUT
        /// (Since 24H2).
        /// </summary>
        SystemMemoryNumaPerformanceInformation = 240,

        /// <summary>
        /// Requires NtQuerySystemInformationEx().
        /// </summary>
        SystemCodeIntegritySignedPoliciesFullInformation = 241,

        /// <summary>
        /// SystemSecureSecretsInformation
        /// </summary>
        SystemSecureCoreInformation = 242,

        /// <summary>
        /// SYSTEM_TRUSTEDAPPS_RUNTIME_INFORMATION
        /// </summary>
        SystemTrustedAppsRuntimeInformation = 243,

        /// <summary>
        /// SYSTEM_BAD_PAGE_INFORMATION
        /// </summary>
        SystemBadPageInformationEx = 244,

        /// <summary>
        /// ULONG
        /// </summary>
        SystemResourceDeadlockTimeout = 245,

        /// <summary>
        /// ULONG (Requires SeDebugPrivilege).
        /// </summary>
        SystemBreakOnContextUnwindFailureInformation = 246,

        /// <summary>
        /// SYSTEM_OSL_RAMDISK_INFORMATION
        /// </summary>
        SystemOslRamdiskInformation = 247,

        /// <summary>
        /// SYSTEM_CODEINTEGRITYPOLICY_MANAGEMENT (Since 25H2).
        /// </summary>
        SystemCodeIntegrityPolicyManagementInformation = 248,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemMemoryNumaCacheInformation = 249,

        /// <summary>
        /// Undocumented.
        /// </summary>
        SystemProcessorFeaturesBitMapInformation = 250,

        /// <summary>
        /// SYSTEM_REF_TRACE_INFORMATION_EX
        /// </summary>
        SystemRefTraceInformationEx = 251,

        /// <summary>
        /// SYSTEM_BASICPROCESS_INFORMATION
        /// </summary>
        SystemBasicProcessInformation = 252,

        /// <summary>
        /// SYSTEM_HANDLECOUNT_INFORMATION
        /// </summary>
        SystemHandleCountInformation = 253,

        /// <summary>
        /// Represents the maximum value for system information classes.
        /// </summary>
        /// <remarks>This enumeration is used to define the upper limit for system information classes
        /// that can be queried or set. It is typically used in conjunction with system-level APIs that require
        /// specifying a class of system information.</remarks>
        MaxSystemInfoClass = 254,
    }
}
