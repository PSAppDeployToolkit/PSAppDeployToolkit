namespace PSADT.Interop
{
    /// <summary>
    /// Specifies the attributes that can be applied to a process or thread during its creation.
    /// </summary>
    /// <remarks>This enumeration is used to define various attributes that can be set when creating a process
    /// or thread. Each attribute corresponds to a specific behavior or configuration, such as setting the parent
    /// process, defining handle lists, or specifying security capabilities. These attributes are typically used in
    /// advanced scenarios where precise control over process or thread creation is required. Extra values not from
    /// CsWin32 are from https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2691-L2720</remarks>
    internal enum PROC_THREAD_ATTRIBUTE_NUM : uint
    {
        ProcThreadAttributeParentProcess = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeParentProcess,
        ProcThreadAttributeExtendedFlags = 1U,  // in ULONG (EXTENDED_PROCESS_CREATION_FLAG_*)  // Since winblue (Windows 8.1)
        ProcThreadAttributeHandleList = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeHandleList,
        ProcThreadAttributeGroupAffinity = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeGroupAffinity,
        ProcThreadAttributePreferredNode = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributePreferredNode,
        ProcThreadAttributeIdealProcessor = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeIdealProcessor,
        ProcThreadAttributeUmsThread = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeUmsThread,
        ProcThreadAttributeMitigationPolicy = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeMitigationPolicy,
        ProcThreadAttributePackageFullName = 8U,  // in WCHAR[]  // Since win8
        ProcThreadAttributeSecurityCapabilities = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeSecurityCapabilities,
        ProcThreadAttributeConsoleReference = 10U,  // BaseGetConsoleReference (kernelbase.dll)
        ProcThreadAttributeProtectionLevel = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeProtectionLevel,
        ProcThreadAttributeOsMaxVersionTested = 12U,  // in MAXVERSIONTESTED_INFO // Since threshold (Windows 10 1507)
        ProcThreadAttributeJobList = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeJobList,
        ProcThreadAttributeChildProcessPolicy = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeChildProcessPolicy,
        ProcThreadAttributeAllApplicationPackagesPolicy = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeAllApplicationPackagesPolicy,
        ProcThreadAttributeWin32kFilter = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeWin32kFilter,
        ProcThreadAttributeSafeOpenPromptOriginClaim = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeSafeOpenPromptOriginClaim,
        ProcThreadAttributeDesktopAppPolicy = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeDesktopAppPolicy,
        ProcThreadAttributeBnoIsolation = 19U,  // in PROC_THREAD_BNOISOLATION_ATTRIBUTE
        ProcThreadAttributeIsolationManifest = 23U,  // in HANDLE (HPCON)  // Since rs5 (Windows 10 1809)
        ProcThreadAttributePseudoConsole = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributePseudoConsole,
        ProcThreadAttributeMitigationAuditPolicy = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeMitigationAuditPolicy,
        ProcThreadAttributeMachineType = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeMachineType,
        ProcThreadAttributeComponentFilter = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeComponentFilter,
        ProcThreadAttributeEnableOptionalXStateFeatures = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeEnableOptionalXStateFeatures,
        ProcThreadAttributeCreateStore = 28U,  // ULONG
        ProcThreadAttributeTrustedApp = Windows.Win32.System.Threading.PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeTrustedApp,
    }
}
