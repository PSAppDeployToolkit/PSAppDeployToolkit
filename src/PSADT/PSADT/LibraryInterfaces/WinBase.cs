using Windows.Win32;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies attributes that can be used in process and thread attribute lists for configuring various behaviors
    /// and policies in Windows operating systems.
    /// </summary>
    /// <remarks>The <see cref="PROC_THREAD_ATTRIBUTE"/> enumeration defines a set of constants that represent
    /// different attributes applicable to processes and threads. These attributes are used in conjunction with
    /// functions like <c>UpdateProcThreadAttribute</c> to modify or specify the behavior of processes and threads
    /// during their creation or execution. Each attribute corresponds to a specific policy or configuration option,
    /// such as security capabilities, processor affinity, or mitigation policies. Extra values not from CsWin32
    /// are from https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2722-L2757 and
    /// https://github.com/tpn/winsdk-10/blob/9b69fd26ac0c7d0b83d378dba01080e93349c2ed/Include/10.0.16299.0/um/WinBase.h#L3372-L3376</remarks>
    internal enum PROC_THREAD_ATTRIBUTE : uint
    {
        /// <summary>
        /// Represents the attribute for all application packages policy used in process and thread attribute lists.
        /// </summary>
        /// <remarks>This field is used to specify the policy for all application packages when creating
        /// or modifying process and thread attribute lists. It is a constant value that corresponds to the underlying
        /// platform invocation (P/Invoke) definition.</remarks>
        PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY = PInvoke.PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY,

        /// <summary>
        /// Represents the attribute value for BNO isolation in process and thread attribute settings.
        /// </summary>
        /// <remarks>This field is used to specify the BNO isolation attribute when configuring process
        /// and thread attributes. It is a constant value derived from the <see cref="ProcThreadAttributeValue"/>
        /// method, which combines various attribute flags. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2744</remarks>
        PROC_THREAD_ATTRIBUTE_BNO_ISOLATION = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeBnoIsolation | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <b>DWORD</b> value that specifies the child process policy. The policy specifies whether to allow a child process to be created. For information on the possible values for the <b>DWORD</b> to which <i>lpValue</i> points, see Remarks. Supported in Windows 10 and newer and Windows Server 2016 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY = PInvoke.PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY,

        /// <summary>
        /// Represents the attribute used to specify a component filter for a process or thread.
        /// </summary>
        /// <remarks>This field is used in conjunction with process or thread attribute lists to filter
        /// components based on specific criteria. It is a constant value that corresponds to the underlying platform
        /// invocation definition.</remarks>
        PROC_THREAD_ATTRIBUTE_COMPONENT_FILTER = PInvoke.PROC_THREAD_ATTRIBUTE_COMPONENT_FILTER,

        /// <summary>
        /// Represents a console reference attribute for a process or thread.
        /// </summary>
        /// <remarks>This attribute is used to specify a console reference when creating a process or
        /// thread. It is a constant value that combines the attribute number and flags using the <see
        /// cref="ProcThreadAttributeValue"/> method. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2732</remarks>
        PROC_THREAD_ATTRIBUTE_CONSOLE_REFERENCE = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeConsoleReference | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// Represents the attribute value used to create a new attribute store for a process or thread.
        /// </summary>
        /// <remarks>This field is used in process and thread attribute lists to specify that a new
        /// attribute store should be created. It is a constant value derived from the <see
        /// cref="ProcThreadAttributeValue"/> method, which combines various attribute flags. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2752</remarks>
        PROC_THREAD_ATTRIBUTE_CREATE_STORE = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeCreateStore | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// <para>This attribute is relevant only to win32 applications that have been converted to UWP packages by using the <a href="https://developer.microsoft.com/windows/bridges/desktop">Desktop Bridge</a>. The <i>lpValue</i> parameter is a pointer to a <b>DWORD</b> value that specifies the desktop app policy. The policy specifies whether descendant processes should continue to run in the desktop environment. For information about the possible values for the <b>DWORD</b> to which <i>lpValue</i> points, see Remarks. Supported in Windows 10 Version 1703 and newer and Windows Server Version 1709 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY = PInvoke.PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <b>DWORD64</b> value that specifies the set of optional XState features to enable for the new thread. Supported in Windows 11 and newer and Windows Server 2022 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_ENABLE_OPTIONAL_XSTATE_FEATURES = PInvoke.PROC_THREAD_ATTRIBUTE_ENABLE_OPTIONAL_XSTATE_FEATURES,

        /// <summary>
        /// Represents the attribute value for extended flags in a process or thread context.
        /// </summary>
        /// <remarks>This field is used to specify extended flags when creating or modifying a process or
        /// thread. The value is determined by combining specific attributes using the <see
        /// cref="ProcThreadAttributeValue"/> method. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2724</remarks>
        PROC_THREAD_ATTRIBUTE_EXTENDED_FLAGS = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeExtendedFlags | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT | PInvoke.PROC_THREAD_ATTRIBUTE_ADDITIVE,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <a href="https://docs.microsoft.com/windows/desktop/api/winnt/ns-winnt-group_affinity">GROUP_AFFINITY</a> structure that specifies the processor group affinity for the new thread. Supported in Windows 7 and newer and Windows Server 2008 R2 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_GROUP_AFFINITY = PInvoke.PROC_THREAD_ATTRIBUTE_GROUP_AFFINITY,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a list of handles to be inherited by the child process. These handles must be created as inheritable handles and must not include pseudo handles such as those returned by the <a href="https://docs.microsoft.com/windows/desktop/api/processthreadsapi/nf-processthreadsapi-getcurrentprocess">GetCurrentProcess</a> or <a href="https://docs.microsoft.com/windows/desktop/api/processthreadsapi/nf-processthreadsapi-getcurrentthread">GetCurrentThread</a> function. <div class="alert"><b>Note</b>  if you use this attribute, pass in a value of TRUE for the <i>bInheritHandles</i> parameter of the <a href="https://docs.microsoft.com/windows/desktop/api/processthreadsapi/nf-processthreadsapi-createprocessa">CreateProcess</a> function.</div> <div> </div></para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_HANDLE_LIST = PInvoke.PROC_THREAD_ATTRIBUTE_HANDLE_LIST,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a  <a href="https://docs.microsoft.com/windows/desktop/api/winnt/ns-winnt-processor_number">PROCESSOR_NUMBER</a> structure that specifies the ideal processor for the new thread. Supported in Windows 7 and newer and Windows Server 2008 R2 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_IDEAL_PROCESSOR = PInvoke.PROC_THREAD_ATTRIBUTE_IDEAL_PROCESSOR,

        /// <summary>
        /// Represents the attribute value for process thread isolation manifest.
        /// </summary>
        /// <remarks>This field is used to specify the isolation manifest attribute for a process thread.
        /// It is a constant value calculated using the <see cref="ProcThreadAttributeValue"/> method with specific
        /// parameters to define the isolation manifest attribute. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2748</remarks>
        PROC_THREAD_ATTRIBUTE_ISOLATION_MANIFEST = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeIsolationManifest | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a list of job handles to be assigned to the child process, in the order specified. Supported in Windows 10 and newer and Windows Server 2016 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_JOB_LIST = PInvoke.PROC_THREAD_ATTRIBUTE_JOB_LIST,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <b>WORD</b> that specifies the machine architecture of the child process. Supported in Windows 11 and newer. The  <b>WORD</b> pointed to by <i>lpValue</i> can be a value listed on <a href="https://docs.microsoft.com/windows/win32/sysinfo/image-file-machine-constants">IMAGE FILE MACHINE CONSTANTS</a>.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_MACHINE_TYPE = PInvoke.PROC_THREAD_ATTRIBUTE_MACHINE_TYPE,

        /// <summary>
        /// Represents the attribute for configuring mitigation audit policy in a process or thread.
        /// </summary>
        /// <remarks>This field is used to specify the mitigation audit policy attribute when creating or
        /// updating a process or thread. It is a constant value that corresponds to the underlying platform's
        /// definition of the mitigation audit policy attribute.</remarks>
        PROC_THREAD_ATTRIBUTE_MITIGATION_AUDIT_POLICY = PInvoke.PROC_THREAD_ATTRIBUTE_MITIGATION_AUDIT_POLICY,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <b>DWORD</b> or <b>DWORD64</b> that specifies the exploit mitigation policy for the child process. Starting in Windows 10, version 1703, this parameter can also be a pointer to a two-element <b>DWORD64</b> array. The specified policy overrides the policies set for the application and the system and cannot be changed after the child process starts running. The  <b>DWORD</b> or <b>DWORD64</b> pointed to by <i>lpValue</i> can be one or more of the values listed in the remarks. Supported in Windows 7 and newer and Windows Server 2008 R2 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY = PInvoke.PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY,

        /// <summary>
        /// Represents the attribute value for specifying the maximum OS version tested for a process or thread.
        /// </summary>
        /// <remarks>This field is used to indicate the highest version of the operating system that the
        /// application has been tested on. It is a constant value that combines the attribute number with flags
        /// indicating its usage context. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2736</remarks>
        PROC_THREAD_ATTRIBUTE_OSMAXVERSIONTESTED = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeOsMaxVersionTested | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// Represents the attribute value for the full package name of a process or thread.
        /// </summary>
        /// <remarks>This field is used to specify the full package name attribute when creating or
        /// modifying a process or thread. It is a constant value that can be used in conjunction with process or thread
        /// attribute functions. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2728</remarks>
        PROC_THREAD_ATTRIBUTE_PACKAGE_FULL_NAME = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributePackageFullName | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a handle to a process to use instead of the calling process as the parent for the process being created. The process to use must have the <b>PROCESS_CREATE_PROCESS</b> access right. Attributes inherited from the specified process include handles, the device map, processor affinity, priority, quotas, the process token, and job object. (Note that some attributes such as the debug port will come from the creating process, not the process specified by this handle.)</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = PInvoke.PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to the node number of the preferred NUMA node for the new process. Supported in Windows 7 and newer and Windows Server 2008 R2 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_PREFERRED_NODE = PInvoke.PROC_THREAD_ATTRIBUTE_PREFERRED_NODE,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <b>DWORD</b> value of <b>PROTECTION_LEVEL_SAME</b>. This specifies the protection level of the child process to be the same as the protection level of its parent process. Supported in Windows 8.1 and newer and Windows Server 2012 R2 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_PROTECTION_LEVEL = PInvoke.PROC_THREAD_ATTRIBUTE_PROTECTION_LEVEL,

        /// <summary>
        /// Represents the attribute for creating a pseudo console in a process or thread.
        /// </summary>
        /// <remarks>This field is used to specify the pseudo console attribute when creating a new
        /// process or thread. It is a constant value that corresponds to the pseudo console attribute in the Windows
        /// API.</remarks>
        PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = PInvoke.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,

        /// <summary>
        /// Represents a constant value used to specify the attribute for safe open prompt origin claim in process and
        /// thread attribute operations.
        /// </summary>
        /// <remarks>This field is used internally to define a specific attribute related to process and
        /// thread management. It combines several flags to create a unique attribute value. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2740</remarks>
        PROC_THREAD_ATTRIBUTE_SAFE_OPEN_PROMPT_ORIGIN_CLAIM = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeSafeOpenPromptOriginClaim | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <a href="https://docs.microsoft.com/windows/desktop/api/winnt/ns-winnt-security_capabilities">SECURITY_CAPABILITIES</a> structure that defines the security capabilities of an app container. If this attribute is set the new process will be created as an AppContainer process. Supported in Windows 8 and newer and Windows Server 2012 and newer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES = PInvoke.PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES,

        /// <summary>
        /// Represents a trusted application attribute for process and thread creation.
        /// </summary>
        /// <remarks>This field is used to specify that a process or thread is a trusted application.  It
        /// is a constant value derived from the <see cref="ProcThreadAttributeValue"/> method  with specific parameters
        /// indicating its trusted status. https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2756</remarks>
        PROC_THREAD_ATTRIBUTE_TRUSTED_APP = PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeTrustedApp | PInvoke.PROC_THREAD_ATTRIBUTE_INPUT,

        /// <summary>
        /// <para>The <i>lpValue</i> parameter is a pointer to a <a href="https://docs.microsoft.com/windows/desktop/api/winnt/ns-winnt-ums_create_thread_attributes">UMS_CREATE_THREAD_ATTRIBUTES</a> structure that specifies a user-mode scheduling (UMS) thread context and a UMS completion list to associate with the thread. After the UMS thread is created, the system queues it to the specified completion list. The UMS thread runs only when an application's UMS scheduler retrieves the UMS thread from the completion list and selects it to run.  For more information, see <a href="https://docs.microsoft.com/windows/desktop/ProcThread/user-mode-scheduling">User-Mode Scheduling</a>. Supported in Windows 7 and newer and Windows Server 2008 R2 and newer. Not supported in Windows 11 and newer (see [User-Mode Scheduling](/windows/win32/procthread/user-mode-scheduling)).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute#">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        /// <remarks>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        PROC_THREAD_ATTRIBUTE_UMS_THREAD = PInvoke.PROC_THREAD_ATTRIBUTE_UMS_THREAD,

        /// <summary>
        /// Represents the attribute used to specify a Win32k filter for a process or thread.
        /// </summary>
        /// <remarks>This attribute is used in process or thread creation to apply a Win32k filter, which
        /// can restrict the set of Win32k system calls that the process or thread can make. It is typically used in
        /// environments where security and resource management are critical.</remarks>
        PROC_THREAD_ATTRIBUTE_WIN32K_FILTER = PInvoke.PROC_THREAD_ATTRIBUTE_WIN32K_FILTER,
    }

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
