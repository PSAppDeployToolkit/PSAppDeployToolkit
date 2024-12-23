using System;

namespace PSADT.PInvoke
{
    #region kernel32.dll

    /// <summary>
    /// The following process creation flags are used by the <c>CreateProcess</c>, <c>CreateProcessAsUser</c>,
    /// <c>CreateProcessWithLogonW</c>, and <c>CreateProcessWithTokenW</c> functions. They can be specified in any combination, except as noted.
    /// </summary>
    // https://msdn.microsoft.com/en-us/library/windows/desktop/ms684863(v=vs.85).aspx
    [Flags]
    public enum CREATE_PROCESS : uint
    {
        /// <summary>
        /// The child processes of a process associated with a job are not associated with the job. If the calling process is not associated
        /// with a job, this constant has no effect. If the calling process is associated with a job, the job must set the
        /// JOB_OBJECT_LIMIT_BREAKAWAY_OK limit.
        /// </summary>
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,

        /// <summary>
        /// The new process does not inherit the error mode of the calling process. Instead, the new process gets the default error mode.
        /// This feature is particularly useful for multithreaded shell applications that run with hard errors disabled.The default behavior
        /// is for the new process to inherit the error mode of the caller. Setting this flag changes that default behavior.
        /// </summary>
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,

        /// <summary>
        /// The new process has a new console, instead of inheriting its parent's console (the default). For more information, see Creation
        /// of a ConsoleHelper. This flag cannot be used with DETACHED_PROCESS.
        /// </summary>
        CREATE_NEW_CONSOLE = 0x00000010,

        /// <summary>
        /// The new process is the root process of a new process group. The process group includes all processes that are descendants of this
        /// root process. The process identifier of the new process group is the same as the process identifier, which is returned in the
        /// lpProcessInformation parameter. ProcessEx groups are used by the GenerateConsoleCtrlEvent function to enable sending a CTRL+BREAK
        /// signal to a group of console processes.If this flag is specified, CTRL+C signals will be disabled for all processes within the
        /// new process group.This flag is ignored if specified with CREATE_NEW_CONSOLE.
        /// </summary>
        CREATE_NEW_PROCESS_GROUP = 0x00000200,

        /// <summary>
        /// The process is a console application that is being run without a console window. Therefore, the console handle for the
        /// application is not set.This flag is ignored if the application is not a console application, or if it is used with either
        /// CREATE_NEW_CONSOLE or DETACHED_PROCESS.
        /// </summary>
        CREATE_NO_WINDOW = 0x08000000,

        /// <summary>
        /// The process is to be run as a protected process. The system restricts access to protected processes and the threads of protected
        /// processes. For more information on how processes can interact with protected processes, see ProcessEx Security and Access Rights.To
        /// activate a protected process, the binary must have a special signature. This signature is provided by Microsoft but not currently
        /// available for non-Microsoft binaries. There are currently four protected processes: media foundation, audio engine, Windows error
        /// reporting, and system. Components that load into these binaries must also be signed. Multimedia companies can leverage the first
        /// two protected processes. For more information, see Overview of the Protected Media Path.Windows Server 2003 and Windows XP: This
        /// value is not supported.
        /// </summary>
        CREATE_PROTECTED_PROCESS = 0x00040000,

        /// <summary>
        /// Allows the caller to execute a child process that bypasses the process restrictions that would normally be applied automatically
        /// to the process.
        /// </summary>
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,

        /// <summary>This flag allows secure processes, that run in the Virtualization-Based Security environment, to launch.</summary>
        CREATE_SECURE_PROCESS = 0x00400000,

        /// <summary>
        /// This flag is valid only when starting a 16-bit Windows-based application. If set, the new process runs in a private Virtual DOS
        /// Machine (VDM). By default, all 16-bit Windows-based applications run as threads in a single, shared VDM. The advantage of running
        /// separately is that a crash only terminates the single VDM; any other programs running in distinct VDMs continue to function
        /// normally. Also, 16-bit Windows-based applications that are run in separate VDMs have separate input queues. That means that if
        /// one application stops responding momentarily, applications in separate VDMs continue to receive input. The disadvantage of
        /// running separately is that it takes significantly more memory to do so. You should use this flag only if the user requests that
        /// 16-bit applications should run in their own VDM.
        /// </summary>
        CREATE_SEPARATE_WOW_VDM = 0x00000800,

        /// <summary>
        /// The flag is valid only when starting a 16-bit Windows-based application. If the DefaultSeparateVDM switch in the Windows section
        /// of WIN.INI is TRUE, this flag overrides the switch. The new process is run in the shared Virtual DOS Machine.
        /// </summary>
        CREATE_SHARED_WOW_VDM = 0x00001000,

        /// <summary>
        /// The primary thread of the new process is created in a suspended state, and does not run until the ResumeThread function is called.
        /// </summary>
        CREATE_SUSPENDED = 0x00000004,

        /// <summary>
        /// If this flag is set, the environment block pointed to by lpEnvironment uses Unicode characters. Otherwise, the environment block
        /// uses ANSI characters.
        /// </summary>
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,

        /// <summary>
        /// The calling thread starts and debugs the new process. It can receive all related debug events using the WaitForDebugEvent function.
        /// </summary>
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,

        /// <summary>
        /// The calling thread starts and debugs the new process and all child processes created by the new process. It can receive all
        /// related debug events using the WaitForDebugEvent function. A process that uses DEBUG_PROCESS becomes the root of a debugging
        /// chain. This continues until another process in the chain is created with DEBUG_PROCESS.If this flag is combined with
        /// DEBUG_ONLY_THIS_PROCESS, the caller debugs only the new process, not any child processes.
        /// </summary>
        DEBUG_PROCESS = 0x00000001,

        /// <summary>
        /// For console processes, the new process does not inherit its parent's console (the default). The new process can call the
        /// AllocConsole function at a later time to create a console. For more information, see Creation of a ConsoleHelper. This value cannot be
        /// used with CREATE_NEW_CONSOLE.
        /// </summary>
        DETACHED_PROCESS = 0x00000008,

        /// <summary>
        /// The process is created with extended startup information; the lpStartupInfo parameter specifies a STARTUPINFOEX structure.Windows
        /// Server 2003 and Windows XP: This value is not supported.
        /// </summary>
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,

        /// <summary>
        /// The process inherits its parent's affinity. If the parent process has threads in more than one processor group, the new process
        /// inherits the group-relative affinity of an arbitrary group in use by the parent.Windows Server 2008, Windows Vista, Windows
        /// Server 2003 and Windows XP: This value is not supported.
        /// </summary>
        INHERIT_PARENT_AFFINITY = 0x00010000,

        /// <summary>ProcessEx with no special scheduling needs.</summary>
        NORMAL_PRIORITY_CLASS = 0x00000020,

        /// <summary>
        /// ProcessEx whose threads run only when the system is idle and are preempted by the threads of any process running in a higher
        /// priority class. An example is a screen saver. The idle priority class is inherited by child processes.
        /// </summary>
        IDLE_PRIORITY_CLASS = 0x00000040,

        /// <summary>
        /// ProcessEx that performs time-critical tasks that must be executed immediately for it to run correctly. The threads of a
        /// high-priority class process preempt the threads of normal or idle priority class processes. An example is the Task List, which
        /// must respond quickly when called by the user, regardless of the load on the operating system. Use extreme care when using the
        /// high-priority class, because a high-priority class CPU-bound application can use nearly all available cycles.
        /// </summary>
        HIGH_PRIORITY_CLASS = 0x00000080,

        /// <summary>
        /// ProcessEx that has the highest possible priority. The threads of a real-time priority class process preempt the threads of all
        /// other processes, including operating system processes performing important tasks. For example, a real-time process that executes
        /// for more than a very brief interval can cause disk caches not to flush or cause the mouse to be unresponsive.
        /// </summary>
        REALTIME_PRIORITY_CLASS = 0x00000100,

        /// <summary>ProcessEx that has priority above IDLE_PRIORITY_CLASS but below NORMAL_PRIORITY_CLASS.</summary>
        BELOW_NORMAL_PRIORITY_CLASS = 0x00004000,

        /// <summary>ProcessEx that has priority above NORMAL_PRIORITY_CLASS but below HIGH_PRIORITY_CLASS.</summary>
        ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000,

        /// <summary>
        /// Begin background processing mode. The system lowers the resource scheduling priorities of the process (and its threads) so that
        /// it can perform background work without significantly affecting activity in the foreground.
        /// <para>
        /// This value can be specified only if hProcess is a handle to the current process. The function fails if the process is already in
        /// background processing mode.
        /// </para>
        /// <para>Windows Server 2003 and Windows XP: This value is not supported.</para>
        /// </summary>
        PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000,

        /// <summary>
        /// End background processing mode. The system restores the resource scheduling priorities of the process (and its threads) as they
        /// were before the process entered background processing mode.
        /// <para>
        /// This value can be specified only if hProcess is a handle to the current process. The function fails if the process is not in
        /// background processing mode.
        /// </para>
        /// <para>Windows Server 2003 and Windows XP: This value is not supported.</para>
        /// </summary>
        PROCESS_MODE_BACKGROUND_END = 0x00200000,
    }

    /// <summary>
    /// Flags used to specify how a process's main window and startup behavior are configured in the <see cref="STARTUPINFO"/> structure.
    /// </summary>
    [Flags]
    public enum STARTF : uint
    {
        /// <summary>
        /// The <see cref="STARTUPINFO.wShowWindow"/> member contains additional information.
        /// </summary>
        STARTF_USESHOWWINDOW = 0x00000001,

        /// <summary>
        /// The <see cref="STARTUPINFO.dwXSize"/> and <see cref="STARTUPINFO.dwYSize"/> members contain additional information.
        /// </summary>
        STARTF_USESIZE = 0x00000002,

        /// <summary>
        /// The <see cref="STARTUPINFO.dwX"/> and <see cref="STARTUPINFO.dwY"/> members contain additional information.
        /// </summary>
        STARTF_USEPOSITION = 0x00000004,

        /// <summary>
        /// The <see cref="STARTUPINFO.dwXCountChars"/> and <see cref="STARTUPINFO.dwYCountChars"/> members contain additional information.
        /// </summary>
        STARTF_USECOUNTCHARS = 0x00000008,

        /// <summary>
        /// The <see cref="STARTUPINFO.dwFillAttribute"/> member contains additional information.
        /// </summary>
        STARTF_USEFILLATTRIBUTE = 0x00000010,

        /// <summary>
        /// The process is running in full-screen mode.
        /// </summary>
        STARTF_RUNFULLSCREEN = 0x00000020,

        /// <summary>
        /// Indicates that the feedback cursor is forced on during the startup process.
        /// </summary>
        STARTF_FORCEONFEEDBACK = 0x00000040,

        /// <summary>
        /// Indicates that the feedback cursor is forced off during the startup process.
        /// </summary>
        STARTF_FORCEOFFFEEDBACK = 0x00000080,

        /// <summary>
        /// The standard handles <see cref="STARTUPINFO.hStdInput"/>, <see cref="STARTUPINFO.hStdOutput"/>, and <see cref="STARTUPINFO.hStdError"/> fields contain additional information.
        /// </summary>
        STARTF_USESTDHANDLES = 0x00000100,

        /// <summary>
        /// The <see cref="STARTUPINFO.hStdInput"/> member contains a hotkey value to be used with the process.
        /// </summary>
        STARTF_USEHOTKEY = 0x00000200,

        /// <summary>
        /// The window title contains the shortcut's link name.
        /// </summary>
        STARTF_TITLEISLINKNAME = 0x00000800,

        /// <summary>
        /// The <see cref="STARTUPINFO.lpTitle"/> field contains an AppUserModelID identifier for the process.
        /// </summary>
        STARTF_TITLEISAPPID = 0x00001000,

        /// <summary>
        /// Prevents the process from being pinned to the taskbar.
        /// </summary>
        STARTF_PREVENTPINNING = 0x00002000,

        /// <summary>
        /// Indicates that the source of the process is untrusted.
        /// </summary>
        STARTF_UNTRUSTEDSOURCE = 0x00008000,
    }


    /// <summary>
    /// Flags that control the behavior of the <see cref="NativeMethods.LoadLibraryEx"/> function, which is used to load a dynamic-link library (DLL).
    /// </summary>
    [Flags]
    public enum LoadLibraryExFlags : int
    {
        /// <summary>
        /// No flags. The default behavior for loading the library.
        /// </summary>
        None = 0,

        /// <summary>
        /// If this value is used, the system does not call <c>DllMain</c> for process and thread initialization and termination.
        /// This prevents the execution of any DLL initialization code, such as registering window classes.
        /// </summary>
        DONT_RESOLVE_DLL_REFERENCES = 0x00000001,

        /// <summary>
        /// If this value is used, the system does not check AppLocker rules or Software Restriction Policies for the DLL.
        /// This action applies only to the DLL being loaded and not to its dependencies.
        /// </summary>
        LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,

        /// <summary>
        /// If this value is used, the system maps the file into the calling process's address space as if it were a data file.
        /// The file is not executed, which allows read-only access.
        /// </summary>
        LOAD_LIBRARY_AS_DATAFILE = 0x00000002,

        /// <summary>
        /// Similar to <see cref="LOAD_LIBRARY_AS_DATAFILE"/>, but the file cannot be mapped by another process until it is closed.
        /// </summary>
        LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,

        /// <summary>
        /// If this value is used, the system maps the file into memory as an image resource.
        /// The file cannot be executed or directly read.
        /// </summary>
        LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,

        /// <summary>
        /// If this value is used, the system searches the application directory for the DLL and its dependencies.
        /// </summary>
        LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,

        /// <summary>
        /// If this value is used, the system uses the standard search strategy that is specified by the application.
        /// This flag includes the application directory, <c>System32</c>, and the user's directories.
        /// </summary>
        LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,

        /// <summary>
        /// If this value is used, the system searches the directory from which the application loaded before searching other directories.
        /// </summary>
        LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,

        /// <summary>
        /// If this value is used, the system searches the <c>System32</c> directory for the DLL and its dependencies.
        /// </summary>
        LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,

        /// <summary>
        /// If this value is used, the system searches directories that are specified in the registry key for per-user paths.
        /// </summary>
        LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,

        /// <summary>
        /// If this value is used, the system uses an altered search path to load the library, which can be specified in the lpFileName parameter of <see cref="NativeMethods.LoadLibraryEx"/>.
        /// </summary>
        LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
    }

    /// <summary>Processor architecture</summary>
    public enum ProcessorArchitecture : ushort
    {
        /// <summary>x86</summary>
        PROCESSOR_ARCHITECTURE_INTEL = 0,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_MIPS = 1,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ALPHA = 2,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_PPC = 3,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_SHX = 4,

        /// <summary>ARM</summary>
        PROCESSOR_ARCHITECTURE_ARM = 5,

        /// <summary>Intel Itanium-based</summary>
        PROCESSOR_ARCHITECTURE_IA64 = 6,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ALPHA64 = 7,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_MSIL = 8,

        /// <summary>x64 (AMD or Intel)</summary>
        PROCESSOR_ARCHITECTURE_AMD64 = 9,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_NEUTRAL = 11,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ARM64 = 12,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13,

        /// <summary>Unknown architecture.</summary>
        PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF
    }

    /// <summary>Describes possible machine architectures. Used in <c>GetSystemWow64Directory2</c> and <c>IsWow64GuestMachineSupported</c>.</summary>
    // https://msdn.microsoft.com/en-us/library/windows/desktop/mt804345(v=vs.85).aspx
    public enum IMAGE_FILE_MACHINE : ushort
    {
        /// <summary>Unknown</summary>
        IMAGE_FILE_MACHINE_UNKNOWN = 0,

        /// <summary>
        /// Interacts with the host and not a WOW64 guest. <note>This constant is available starting with Windows 10, version 1607 and
        /// Windows Server 2016.</note>
        /// </summary>
        IMAGE_FILE_MACHINE_TARGET_HOST = 0x0001,

        /// <summary>Intel 386</summary>
        IMAGE_FILE_MACHINE_I386 = 0x014c,

        /// <summary>MIPS little-endian, 0x160 big-endian</summary>
        IMAGE_FILE_MACHINE_R3000 = 0x0162,

        /// <summary>MIPS little-endian</summary>
        IMAGE_FILE_MACHINE_R4000 = 0x0166,

        /// <summary>MIPS little-endian</summary>
        IMAGE_FILE_MACHINE_R10000 = 0x0168,

        /// <summary>MIPS little-endian WCE v2</summary>
        IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x0169,

        /// <summary>Alpha_AXP</summary>
        IMAGE_FILE_MACHINE_ALPHA = 0x0184,

        /// <summary>SH3 little-endian</summary>
        IMAGE_FILE_MACHINE_SH3 = 0x01a2,

        /// <summary>SH3DSP</summary>
        IMAGE_FILE_MACHINE_SH3DSP = 0x01a3,

        /// <summary>SH3E little-endian</summary>
        IMAGE_FILE_MACHINE_SH3E = 0x01a4,

        /// <summary>SH4 little-endian</summary>
        IMAGE_FILE_MACHINE_SH4 = 0x01a6,

        /// <summary>SH5</summary>
        IMAGE_FILE_MACHINE_SH5 = 0x01a8,

        /// <summary>ARM Little-Endian</summary>
        IMAGE_FILE_MACHINE_ARM = 0x01c0,

        /// <summary>ARM Thumb/Thumb-2 Little-Endian</summary>
        IMAGE_FILE_MACHINE_THUMB = 0x01c2,

        /// <summary>ARM Thumb-2 Little-Endian <note>This constant is available starting with Windows 7 and Windows Server 2008 R2.</note></summary>
        IMAGE_FILE_MACHINE_ARMNT = 0x01c4,

        /// <summary>TAM33BD</summary>
        IMAGE_FILE_MACHINE_AM33 = 0x01d3,

        /// <summary>IBM PowerPC Little-Endian</summary>
        IMAGE_FILE_MACHINE_POWERPC = 0x01F0,

        /// <summary>POWERPCFP</summary>
        IMAGE_FILE_MACHINE_POWERPCFP = 0x01f1,

        /// <summary>Intel 64</summary>
        IMAGE_FILE_MACHINE_IA64 = 0x0200,

        /// <summary>MIPS</summary>
        IMAGE_FILE_MACHINE_MIPS16 = 0x0266,

        /// <summary>ALPHA64</summary>
        IMAGE_FILE_MACHINE_ALPHA64 = 0x0284,

        /// <summary>MIPS</summary>
        IMAGE_FILE_MACHINE_MIPSFPU = 0x0366,

        /// <summary>MIPS</summary>
        IMAGE_FILE_MACHINE_MIPSFPU16 = 0x0466,

        /// <summary>AXP64</summary>
        IMAGE_FILE_MACHINE_AXP64 = 0x0284,

        /// <summary>Infineon</summary>
        IMAGE_FILE_MACHINE_TRICORE = 0x0520,

        /// <summary>CEF</summary>
        IMAGE_FILE_MACHINE_CEF = 0x0CEF,

        /// <summary>EFI Byte Code</summary>
        IMAGE_FILE_MACHINE_EBC = 0x0EBC,

        /// <summary>AMD64 (K8)</summary>
        IMAGE_FILE_MACHINE_AMD64 = 0x8664,

        /// <summary>M32R little-endian</summary>
        IMAGE_FILE_MACHINE_M32R = 0x9041,

        /// <summary>ARM64 Little-Endian <note>This constant is available starting with Windows 8.1 and Windows Server 2012 R2.</note></summary>
        IMAGE_FILE_MACHINE_ARM64 = 0xAA64,

        /// <summary>CEE</summary>
        IMAGE_FILE_MACHINE_CEE = 0xC0EE,
    }

    #endregion

    #region wtsapi32.dll

    public enum WTS_CLIENT_PROTOCOL_TYPE
    {
        Console,
        Legacy,
        RDP
    }

    /// <summary>The address family specification.</summary>
    public enum ADDRESS_FAMILY
    {
        /// <summary>Unknown address family.</summary>
        AF_UNKNOWN = -1,

        /// <summary>Unspecified address family.</summary>
        AF_UNSPEC = 0,

        /// <summary>Unix local to host address.</summary>
        AF_UNIX = 1,

        /// <summary>Address for IP version 4.</summary>
        AF_INET = 2,

        /// <summary>ARPANET IMP address.</summary>
        AF_IMPLINK = 3,

        /// <summary>Address for PUP protocols.</summary>
        AF_PUP = 4,

        /// <summary>Address for MIT CHAOS protocols.</summary>
        AF_CHAOS = 5,

        /// <summary>Address for Xerox NS protocols.</summary>
        AF_NS = 6,

        /// <summary>IPX or SPX address.</summary>
        AF_IPX = AF_NS,

        /// <summary>Address for ISO protocols.</summary>
        AF_ISO = 7,

        /// <summary>Address for OSI protocols.</summary>
        AF_OSI = AF_ISO,

        /// <summary>European Computer Manufacturers Association (ECMA) address.</summary>
        AF_ECMA = 8,

        /// <summary>Address for Datakit protocols.</summary>
        AF_DATAKIT = 9,

        /// <summary>Addresses for CCITT protocols, such as X.25.</summary>
        AF_CCITT = 10,

        /// <summary>IBM SNA address.</summary>
        AF_SNA = 11,

        /// <summary>DECnet address.</summary>
        AF_DECnet = 12,

        /// <summary>Direct data-link interface address.</summary>
        AF_DLI = 13,

        /// <summary>LAT address.</summary>
        AF_LAT = 14,

        /// <summary>NSC Hyperchannel address.</summary>
        AF_HYLINK = 15,

        /// <summary>AppleTalk address.</summary>
        AF_APPLETALK = 16,

        /// <summary>NetBios address.</summary>
        AF_NETBIOS = 17,

        /// <summary>VoiceView address.</summary>
        AF_VOICEVIEW = 18,

        /// <summary>FireFox address.</summary>
        AF_FIREFOX = 19,

        /// <summary>Undocumented.</summary>
        AF_UNKNOWN1 = 20,

        /// <summary>Banyan address.</summary>
        AF_BAN = 21,

        /// <summary>Native ATM services address.</summary>
        AF_ATM = 22,

        /// <summary>Address for IP version 6.</summary>
        AF_INET6 = 23,

        /// <summary>Address for Microsoft cluster products.</summary>
        AF_CLUSTER = 24,

        /// <summary>IEEE 1284.4 workgroup address.</summary>
        AF_12844 = 25,

        /// <summary>IrDA address.</summary>
        AF_IRDA = 26,

        /// <summary>Address for Network Designers OSI gateway-enabled protocols.</summary>
        AF_NETDES = 28,

        /// <summary>Undocumented.</summary>
        AF_TCNPROCESS = 29,

        /// <summary>Undocumented.</summary>
        AF_TCNMESSAGE = 30,

        /// <summary>Undocumented.</summary>
        AF_ICLFXBM = 31,

        /// <summary>Bluetooth RFCOMM/L2CAP protocols.</summary>
        AF_BTH = 32,

        /// <summary>Link layer interface.</summary>
        AF_LINK = 33,

        /// <summary>Windows Hyper-V.</summary>
        AF_HYPERV = 34,
    }

    public enum WINSTATIONINFOCLASS
    {
        WinStationInformation = 8
    }

    /// <summary>
    /// Specifies the connection state of a Terminal Services session.
    /// </summary>
    public enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    /// <summary>
    /// Contains values that indicate the type of session information to retrieve in a call to the WTSQuerySessionInformation function.
    /// </summary>
    public enum WTS_INFO_CLASS
    {
        /// <summary>
        /// A null-terminated string that contains the name of the initial program that Remote Desktop Services runs when the user logs on.
        /// </summary>
        WTSInitialProgram,

        /// <summary>
        /// A null-terminated string that contains the published name of the application that the session is running.Windows Server 2008
        /// R2, Windows 7, Windows Server 2008 and Windows Vista: This value is not supported
        /// </summary>
        WTSApplicationName,

        /// <summary>A null-terminated string that contains the default directory used when launching the initial program.</summary>
        WTSWorkingDirectory,

        /// <summary>This value is not used.</summary>
        WTSOEMId,

        /// <summary>A ULONG value that contains the session identifier.</summary>
        WTSSessionId,

        /// <summary>A null-terminated string that contains the name of the user associated with the session.</summary>
        WTSUserName,

        /// <summary>A null-terminated string that contains the name of the Remote Desktop Services session.</summary>
        WTSWinStationName,

        /// <summary>A null-terminated string that contains the name of the domain to which the logged-on user belongs.</summary>
        WTSDomainName,

        /// <summary>The session's current connection state. For more information, see WTS_CONNECTSTATE_CLASS.</summary>
        WTSConnectState,

        /// <summary>A ULONG value that contains the build number of the client.</summary>
        WTSClientBuildNumber,

        /// <summary>A null-terminated string that contains the name of the client.</summary>
        WTSClientName,

        /// <summary>A null-terminated string that contains the directory in which the client is installed.</summary>
        WTSClientDirectory,

        /// <summary>A USHORT client-specific product identifier.</summary>
        WTSClientProductId,

        /// <summary>
        /// A ULONG value that contains a client-specific hardware identifier. This option is reserved for future use.
        /// WTSQuerySessionInformation will always return a value of 0.
        /// </summary>
        WTSClientHardwareId,

        /// <summary>
        /// The network type and network address of the client. For more information, see WTS_CLIENT_ADDRESS. The IP address is offset
        /// by two bytes from the start of the Address member of the WTS_CLIENT_ADDRESS structure.
        /// </summary>
        WTSClientAddress,

        /// <summary>Information about the display resolution of the client. For more information, see WTS_CLIENT_DISPLAY.</summary>
        WTSClientDisplay,

        /// <summary>
        /// A USHORT value that specifies information about the protocol type for the session. This is one of the following values.
        /// </summary>
        WTSClientProtocolType,

        /// <summary>
        /// This value returns FALSE. If you call GetLastError to get extended error information, GetLastError returns
        /// ERROR_NOT_SUPPORTED.Windows Server 2008 and Windows Vista: This value is not used.
        /// </summary>
        WTSIdleTime,

        /// <summary>
        /// This value returns FALSE. If you call GetLastError to get extended error information, GetLastError returns
        /// ERROR_NOT_SUPPORTED.Windows Server 2008 and Windows Vista: This value is not used.
        /// </summary>
        WTSLogonTime,

        /// <summary>
        /// This value returns FALSE. If you call GetLastError to get extended error information, GetLastError returns
        /// ERROR_NOT_SUPPORTED.Windows Server 2008 and Windows Vista: This value is not used.
        /// </summary>
        WTSIncomingBytes,

        /// <summary>
        /// This value returns FALSE. If you call GetLastError to get extended error information, GetLastError returns
        /// ERROR_NOT_SUPPORTED.Windows Server 2008 and Windows Vista: This value is not used.
        /// </summary>
        WTSOutgoingBytes,

        /// <summary>
        /// This value returns FALSE. If you call GetLastError to get extended error information, GetLastError returns
        /// ERROR_NOT_SUPPORTED.Windows Server 2008 and Windows Vista: This value is not used.
        /// </summary>
        WTSIncomingFrames,

        /// <summary>
        /// This value returns FALSE. If you call GetLastError to get extended error information, GetLastError returns
        /// ERROR_NOT_SUPPORTED.Windows Server 2008 and Windows Vista: This value is not used.
        /// </summary>
        WTSOutgoingFrames,

        /// <summary>Information about a Remote Desktop Connection (RDC) client. For more information, see WTSCLIENT.</summary>
        WTSClientInfo,

        /// <summary>Information about a client session on a RD Session Host server. For more information, see WTSINFO.</summary>
        WTSSessionInfo,

        /// <summary>
        /// Extended information about a session on a RD Session Host server. For more information, see WTSINFOEX. Windows Server 2008
        /// and Windows Vista: This value is not supported.
        /// </summary>
        WTSSessionInfoEx,

        /// <summary>
        /// A WTSCONFIGINFO structure that contains information about the configuration of a RD Session Host server. Windows Server 2008
        /// and Windows Vista: This value is not supported.
        /// </summary>
        WTSConfigInfo,

        /// <summary>This value is not supported.</summary>
        WTSValidationInfo,

        /// <summary>
        /// A WTS_SESSION_ADDRESS structure that contains the IPv4 address assigned to the session. If the session does not have a
        /// virtual IP address, the WTSQuerySessionInformation function returns ERROR_NOT_SUPPORTED.Windows Server 2008 and Windows
        /// Vista: This value is not supported.
        /// </summary>
        WTSSessionAddressV4,

        /// <summary>
        /// Determines whether the current session is a remote session. The WTSQuerySessionInformation function returns a value of TRUE
        /// to indicate that the current session is a remote session, and FALSE to indicate that the current session is a local session.
        /// This value can only be used for the local machine, so the hServer parameter of the WTSQuerySessionInformation function must
        /// contain WTS_CURRENT_SERVER_HANDLE. Windows Server 2008 and Windows Vista: This value is not supported.
        /// </summary>
        WTSIsRemoteSession,
    }

    #endregion

    #region advapi32.dll

    /// <summary>
    /// The <see cref="TOKEN_TYPE"/> enumeration contains values that differentiate between a primary token and an
    /// impersonation token.
    /// </summary>
    public enum TOKEN_TYPE
    {
        /// <summary>
        ///     The new token is a primary token that you can use in the CreateProcessAsUser function.
        /// </summary>
        TokenPrimary = 1,

        /// <summary>
        /// The new token is an impersonation token.
        /// </summary>
        TokenImpersonation
    }

    /// <summary>
    /// The TOKEN_ELEVATION_TYPE enumeration indicates the elevation type of token being queried by the GetTokenInformation function.
    /// </summary>
    public enum TOKEN_ELEVATION_TYPE : int
    {
        /// <summary>The token does not have a linked token.</summary>
        TokenElevationTypeDefault = 1,

        /// <summary>The token is an elevated token.</summary>
        TokenElevationTypeFull,

        /// <summary>The token is a limited token.</summary>
        TokenElevationTypeLimited
    }

    /// <summary>
    /// The TOKEN_INFORMATION_CLASS enumeration contains values that specify the type of information being assigned to or retrieved from
    /// an access token.
    /// <para>The GetTokenInformation function uses these values to indicate the type of token information to retrieve.</para>
    /// <para>The SetTokenInformation function uses these values to set the token information.</para>
    /// </summary>
    public enum TOKEN_INFORMATION_CLASS
    {
        /// <summary>The buffer receives a TOKEN_USER structure that contains the user account of the token.</summary>
        TokenUser = 1,

        /// <summary>The buffer receives a TOKEN_GROUPS structure that contains the group accounts associated with the token.</summary>
        TokenGroups,

        /// <summary>The buffer receives a TOKEN_PRIVILEGES structure that contains the privileges of the token.</summary>
        TokenPrivileges,

        /// <summary>
        /// The buffer receives a TOKEN_OWNER structure that contains the default owner security identifier (SID) for newly created objects.
        /// </summary>
        TokenOwner,

        /// <summary>
        /// The buffer receives a TOKEN_PRIMARY_GROUP structure that contains the default primary group SID for newly created objects.
        /// </summary>
        TokenPrimaryGroup,

        /// <summary>The buffer receives a TOKEN_DEFAULT_DACL structure that contains the default DACL for newly created objects.</summary>
        TokenDefaultDacl,

        /// <summary>
        /// The buffer receives a TOKEN_SOURCE structure that contains the source of the token. TOKEN_QUERY_SOURCE access is needed to
        /// retrieve this information.
        /// </summary>
        TokenSource,

        /// <summary>The buffer receives a TOKEN_TYPE value that indicates whether the token is a primary or impersonation token.</summary>
        TokenType,

        /// <summary>
        /// The buffer receives a SECURITY_IMPERSONATION_LEVEL value that indicates the impersonation level of the token. If the access
        /// token is not an impersonation token, the function fails.
        /// </summary>
        TokenImpersonationLevel,

        /// <summary>The buffer receives a TOKEN_STATISTICS structure that contains various token statistics.</summary>
        TokenStatistics,

        /// <summary>The buffer receives a TOKEN_GROUPS structure that contains the list of restricting SIDs in a restricted token.</summary>
        TokenRestrictedSids,

        /// <summary>
        /// The buffer receives a DWORD value that indicates the Terminal Services session identifier that is associated with the token.
        /// <para>If the token is associated with the terminal server client session, the session identifier is nonzero.</para>
        /// <para>
        /// Windows Server 2003 and Windows XP: If the token is associated with the terminal server console session, the session
        /// identifier is zero.
        /// </para>
        /// <para>In a non-Terminal Services environment, the session identifier is zero.</para>
        /// <para>
        /// If TokenSessionId is set with SetTokenInformation, the application must have the Act As Part Of the Operating System
        /// privilege, and the application must be enabled to set the session ID in a token.
        /// </para>
        /// </summary>
        TokenSessionId,

        /// <summary>
        /// The buffer receives a TOKEN_GROUPS_AND_PRIVILEGES structure that contains the user SID, the group accounts, the restricted
        /// SIDs, and the authentication ID associated with the token.
        /// </summary>
        TokenGroupsAndPrivileges,

        /// <summary>Reserved.</summary>
        TokenSessionReference,

        /// <summary>The buffer receives a DWORD value that is nonzero if the token includes the SANDBOX_INERT flag.</summary>
        TokenSandBoxInert,

        /// <summary>Reserved.</summary>
        TokenAuditPolicy,

        /// <summary>
        /// The buffer receives a TOKEN_ORIGIN value.
        /// <para>
        /// If the token resulted from a logon that used explicit credentials, such as passing a name, domain, and password to the
        /// LogonUser function, then the TOKEN_ORIGIN structure will contain the ID of the logon session that created it.
        /// </para>
        /// <para>
        /// If the token resulted from network authentication, such as a call to AcceptSecurityContext or a call to LogonUser with
        /// dwLogonType set to LOGON32_LOGON_NETWORK or LOGON32_LOGON_NETWORK_CLEARTEXT, then this value will be zero.
        /// </para>
        /// </summary>
        TokenOrigin,

        /// <summary>The buffer receives a TOKEN_ELEVATION_TYPE value that specifies the elevation level of the token.</summary>
        TokenElevationType,

        /// <summary>
        /// The buffer receives a TOKEN_LINKED_TOKEN structure that contains a handle to another token that is linked to this token.
        /// </summary>
        TokenLinkedToken,

        /// <summary>The buffer receives a TOKEN_ELEVATION structure that specifies whether the token is elevated.</summary>
        TokenElevation,

        /// <summary>The buffer receives a DWORD value that is nonzero if the token has ever been filtered.</summary>
        TokenHasRestrictions,

        /// <summary>
        /// The buffer receives a TOKEN_ACCESS_INFORMATION structure that specifies security information contained in the token.
        /// </summary>
        TokenAccessInformation,

        /// <summary>The buffer receives a DWORD value that is nonzero if virtualization is allowed for the token.</summary>
        TokenVirtualizationAllowed,

        /// <summary>The buffer receives a DWORD value that is nonzero if virtualization is enabled for the token.</summary>
        TokenVirtualizationEnabled,

        /// <summary>The buffer receives a TOKEN_MANDATORY_LABEL structure that specifies the token's integrity level.</summary>
        TokenIntegrityLevel,

        /// <summary>The buffer receives a DWORD value that is nonzero if the token has the UIAccess flag set.</summary>
        TokenUIAccess,

        /// <summary>The buffer receives a TOKEN_MANDATORY_POLICY structure that specifies the token's mandatory integrity policy.</summary>
        TokenMandatoryPolicy,

        /// <summary>The buffer receives a TOKEN_GROUPS structure that specifies the token's logon SID.</summary>
        TokenLogonSid,

        /// <summary>
        /// The buffer receives a DWORD value that is nonzero if the token is an application container token. Any callers who check the
        /// TokenIsAppContainer and have it return 0 should also verify that the caller token is not an identify level impersonation
        /// token. If the current token is not an application container but is an identity level token, you should return AccessDenied.
        /// </summary>
        TokenIsAppContainer,

        /// <summary>The buffer receives a TOKEN_GROUPS structure that contains the capabilities associated with the token.</summary>
        TokenCapabilities,

        /// <summary>
        /// The buffer receives a TOKEN_APPCONTAINER_INFORMATION structure that contains the AppContainerSid associated with the token.
        /// If the token is not associated with an application container, the TokenAppContainer member of the
        /// TOKEN_APPCONTAINER_INFORMATION structure points to NULL.
        /// </summary>
        TokenAppContainerSid,

        /// <summary>
        /// The buffer receives a DWORD value that includes the application container number for the token. For tokens that are not
        /// application container tokens, this value is zero.
        /// </summary>
        TokenAppContainerNumber,

        /// <summary>
        /// The buffer receives a CLAIM_SECURITY_ATTRIBUTES_INFORMATION structure that contains the user claims associated with the token.
        /// </summary>
        TokenUserClaimAttributes,

        /// <summary>
        /// The buffer receives a CLAIM_SECURITY_ATTRIBUTES_INFORMATION structure that contains the device claims associated with the token.
        /// </summary>
        TokenDeviceClaimAttributes,

        /// <summary>This value is reserved.</summary>
        TokenRestrictedUserClaimAttributes,

        /// <summary>This value is reserved.</summary>
        TokenRestrictedDeviceClaimAttributes,

        /// <summary>The buffer receives a TOKEN_GROUPS structure that contains the device groups that are associated with the token.</summary>
        TokenDeviceGroups,

        /// <summary>
        /// The buffer receives a TOKEN_GROUPS structure that contains the restricted device groups that are associated with the token.
        /// </summary>
        TokenRestrictedDeviceGroups,

        /// <summary>This value is reserved.</summary>
        TokenSecurityAttributes,

        /// <summary>This value is reserved.</summary>
        TokenIsRestricted
    }

    /// <summary>Token access flags.</summary>
    [Flags]
    public enum TokenAccess : uint
    {
        /// <summary>
        /// Required to attach a primary token to a process. The SE_ASSIGNPRIMARYTOKEN_NAME privilege is also required to accomplish this task.
        /// </summary>
        TOKEN_ASSIGN_PRIMARY = 0x0001,

        /// <summary>Required to duplicate an access token.</summary>
        TOKEN_DUPLICATE = 0x0002,

        /// <summary>Required to attach an impersonation access token to a process.</summary>
        TOKEN_IMPERSONATE = 0x0004,

        /// <summary>Required to query an access token.</summary>
        TOKEN_QUERY = 0x0008,

        /// <summary>Required to query the source of an access token.</summary>
        TOKEN_QUERY_SOURCE = 0x0010,

        /// <summary>Required to enable or disable the privileges in an access token.</summary>
        TOKEN_ADJUST_PRIVILEGES = 0x0020,

        /// <summary>Required to adjust the attributes of the groups in an access token.</summary>
        TOKEN_ADJUST_GROUPS = 0x0040,

        /// <summary>Required to change the default owner, primary group, or DACL of an access token.</summary>
        TOKEN_ADJUST_DEFAULT = 0x0080,

        /// <summary>Required to adjust the session ID of an access token. The SE_TCB_NAME privilege is required.</summary>
        TOKEN_ADJUST_SESSIONID = 0x0100,

        /// <summary>The token all access p</summary>
        TOKEN_ALL_ACCESS_P = 0x000F00FF,

        /// <summary>Combines all possible access rights for a token.</summary>
        TOKEN_ALL_ACCESS = 0x000F01FF,

        /// <summary>Combines STANDARD_RIGHTS_READ and TOKEN_QUERY.</summary>
        TOKEN_READ = 0x00020008,

        /// <summary>Combines STANDARD_RIGHTS_WRITE, TOKEN_ADJUST_PRIVILEGES, TOKEN_ADJUST_GROUPS, and TOKEN_ADJUST_DEFAULT.</summary>
        TOKEN_WRITE = 0x000200E0,

        /// <summary>Combines STANDARD_RIGHTS_EXECUTE and TOKEN_IMPERSONATE.</summary>
        TOKEN_EXECUTE = 0x00020000
    }

    public enum PRIVILEGE_SET_CONTROL : uint
    {
        PRIVILEGE_SET_ALL_NECESSARY = 1
    }

    /// <summary>
    /// Contains values that specify security impersonation levels. Security impersonation levels govern the degree to which a server process can act on behalf of a client process.
    /// </summary>
    public enum SECURITY_IMPERSONATION_LEVEL
    {
        /// <summary>
        /// The server process cannot obtain identification information about the client, and it cannot impersonate the client. It is defined with no value given, and thus, by ANSI C rules, defaults to a value of zero.
        /// </summary>
        SecurityAnonymous,

        /// <summary>
        /// The server process can obtain information about the client, such as security identifiers and privileges, but it cannot impersonate the client. This is useful for servers that export their own objects, for example, database products that export tables and views. Using the retrieved client-security information, the server can make access-validation decisions without being able to use other services that are using the client's security context.
        /// </summary>
        SecurityIdentification,

        /// <summary>
        /// The server process can impersonate the client's security context on its local system. The server cannot impersonate the client on remote systems.
        /// </summary>
        SecurityImpersonation,

        /// <summary>
        /// The server process can impersonate the client's security context on remote systems.
        /// </summary>
        SecurityDelegation,
    }

    /// <summary>Privilege determining the type of system operations that can be performed.</summary>
    public enum TokenPrivilege
    {
        /// <summary>Required to logon interactively.</summary>
        InteractiveLogon = 0x00000001,

        /// <summary>Required for an account to log on using the network logon type.</summary>
        NetworkLogon = 0x00000002,

        /// <summary>Required for an account to log on using the batch logon type.</summary>
        BatchLogon = 0x00000004,

        /// <summary>Required for an account to log on using the service logon type.</summary>
        ServiceLogon = 0x00000010,

        /// <summary>Explicitly denies an account the right to log on using the interactive logon type.</summary>
        DenyInteractiveLogon = 0x00000040,

        /// <summary>Explicitly denies an account the right to log on using the network logon type.</summary>
        DenyNetworkLogon = 0x00000080,

        /// <summary>Explicitly denies an account the right to log on using the batch logon type.</summary>
        DenyBatchLogon = 0x00000100,

        /// <summary>Explicitly denies an account the right to log on using the service logon type.</summary>
        DenyServiceLogon = 0x00000200,

        /// <summary>Remote interactive logon</summary>
        RemoteInteractiveLogon = 0x00000400,

        /// <summary>Explicitly denies an account the right to log on remotely using the interactive logon type.</summary>
        DenyRemoteInteractiveLogon = 0x00000800,

        /// <summary>Privilege to replace a process-level token.</summary>
        AssignPrimaryToken = 0x00001000,

        /// <summary>Privilege to generate security audits.</summary>
        Audit,

        /// <summary>Privilege to backup files and directories.</summary>
        Backup,

        /// <summary>Privilege to bypass traverse checking.</summary>
        ChangeNotify,

        /// <summary>Privilege to create global objects.</summary>
        CreateGlobal,

        /// <summary>Privilege to create a pagefile.</summary>
        CreatePageFile,

        /// <summary>Privilege to create permanent shared objects.</summary>
        CreatePermanent,

        /// <summary>Privilege to create symbolic links.</summary>
        CreateSymbolicLink,

        /// <summary>Privilege to create a token object.</summary>
        CreateToken,

        /// <summary>Privilege to debug programs.</summary>
        Debug,

        /// <summary>Privilege to delegate session using user impersonation.</summary>
        DelegateSessionUserImpersonate,

        /// <summary>Privilege to enable computer and user accounts to be trusted for delegation.</summary>
        EnableDelegation,

        /// <summary>Privilege to impersonate a client after authentication.</summary>
        Impersonate,

        /// <summary>Privilege to increase scheduling priority.</summary>
        IncreaseBasePriority,

        /// <summary>Privilege to adjust memory quotas for a process.</summary>
        IncreaseQuota,

        /// <summary>Privilege to increase a process working set.</summary>
        IncreaseWorkingSet,

        /// <summary>Privilege to load and unload device drivers.</summary>
        LoadDriver,

        /// <summary>Privilege to lock pages in memory.</summary>
        LockMemory,

        /// <summary>Privilege to add workstations to domain.</summary>
        MachineAccount,

        /// <summary>Privilege to manage the files on a volume.</summary>
        ManageVolume,

        /// <summary>Privilege to profile single process.</summary>
        ProfileSingleProcess,

        /// <summary>Privilege to modify an object label.</summary>
        Relabel,

        /// <summary>Privilege to force shutdown from a remote system.</summary>
        RemoteShutdown,

        /// <summary>Privilege to restore files and directories.</summary>
        Restore,

        /// <summary>Privilege to manage auditing and security log.</summary>
        Security,

        /// <summary>Privilege to shut down the system.</summary>
        Shutdown,

        /// <summary>Privilege to synchronize directory service data.</summary>
        SyncAgent,

        /// <summary>Privilege to modify firmware environment values.</summary>
        SystemEnvironment,

        /// <summary>Privilege to profile system performance.</summary>
        SystemProfile,

        /// <summary>Privilege to change the system time.</summary>
        SystemTime,

        /// <summary>Privilege to take ownership of files or other objects.</summary>
        TakeOwnership,

        /// <summary>Privilege to act as part of the operating system.</summary>
        TrustedComputerBase,

        /// <summary>Privilege to change the time zone.</summary>
        TimeZone,

        /// <summary>Privilege to access Credential Manager as a trusted caller.</summary>
        TrustedCredentialManagerAccess,

        /// <summary>Privilege to remove computer from docking station.</summary>
        Undock,

        /// <summary>Privilege to read unsolicited input from a terminal device.</summary>
        UnsolicitedInput
    }

    /// <summary>The logon option.</summary>
    [Flags]
    public enum ProcessLogonFlags : uint
    {
        /// <summary>
        /// Log on, then load the user profile in the HKEY_USERS registry key. The function returns after the profile is loaded. Loading
        /// the profile can be time-consuming, so it is best to use this value only if you must access the information in the
        /// HKEY_CURRENT_USER registry key.
        /// <para>
        /// Windows Server 2003: The profile is unloaded after the new process is terminated, whether or not it has created child processes.
        /// </para>
        /// <para>Windows XP: The profile is unloaded after the new process and all child processes it has created are terminated.</para>
        /// </summary>
        LOGON_WITH_PROFILE = 0x00000001,

        /// <summary>
        /// Log on, but use the specified credentials on the network only. The new process uses the same token as the caller, but the
        /// system creates a new logon session within LSA, and the process uses the specified credentials as the default credentials.
        /// <para>
        /// This value can be used to create a process that uses a different set of credentials locally than it does remotely. This is
        /// useful in inter-domain scenarios where there is no trust relationship.
        /// </para>
        /// <para>
        /// The system does not validate the specified credentials. Therefore, the process can start, but it may not have access to
        /// network resources.
        /// </para>
        /// </summary>
        LOGON_NETCREDENTIALS_ONLY = 0x00000002,

        /// <summary>The logon zero password buffer</summary>
        LOGON_ZERO_PASSWORD_BUFFER = 0x80000000
    }

    #endregion

    #region user32.dll

    /// <summary>
    /// The system metric or configuration setting to be retrieved by <see cref="NativeMethods.GetSystemMetrics"/>.
    /// </summary>
    public enum SystemMetric
    {
        /// <summary>
        /// The flags that specify how the system arranged minimized windows. For more information, see the Remarks section in this topic.
        /// </summary>
        SM_ARRANGE = 56,

        /// <summary>
        /// The value that specifies how the system is started:
        /// <para>0 Normal boot</para>
        /// <para>1 Fail-safe boot</para>
        /// <para>2 Fail-safe with network boot</para>
        /// <para>A fail-safe boot (also called SafeBoot, Safe Mode, or Clean Boot) bypasses the user startup files.</para>
        /// </summary>
        SM_CLEANBOOT = 67,

        /// <summary>The number of display monitors on a desktop. For more information, see the Remarks section in this topic.</summary>
        SM_CMONITORS = 80,

        /// <summary>The number of buttons on a mouse, or zero if no mouse is installed.</summary>
        SM_CMOUSEBUTTONS = 43,

        /// <summary>
        /// Reflects the state of the laptop or slate mode, 0 for Slate Mode and non-zero otherwise. When this system metric changes, the
        /// system sends a broadcast message via WM_SETTINGCHANGE with "ConvertibleSlateMode" in the LPARAM. Note that this system metric
        /// doesn't apply to desktop PCs. In that case, use GetAutoRotationState.
        /// </summary>
        SM_CONVERTIBLESLATEMODE = 0x2003,

        /// <summary>
        /// The width of a window border, in pixels. This is equivalent to the SM_CXEDGE value for windows with the 3-D look.
        /// </summary>
        SM_CXBORDER = 5,

        /// <summary>The width of a cursor, in pixels. The system cannot create cursors of other sizes.</summary>
        SM_CXCURSOR = 13,

        /// <summary>This value is the same as SM_CXFIXEDFRAME.</summary>
        SM_CXDLGFRAME = 7,

        /// <summary>
        /// The width of the rectangle around the location of a first click in a double-click sequence, in pixels. The second click must
        /// occur within the rectangle that is defined by SM_CXDOUBLECLK and SM_CYDOUBLECLK for the system to consider the two clicks a
        /// double-click. The two clicks must also occur within a specified time.
        /// <para>To set the width of the double-click rectangle, call SystemParametersInfo with SPI_SETDOUBLECLKWIDTH.</para>
        /// </summary>
        SM_CXDOUBLECLK = 36,

        /// <summary>
        /// The number of pixels on either side of a mouse-down point that the mouse pointer can move before a drag operation begins.
        /// This allows the user to click and release the mouse button easily without unintentionally starting a drag operation. If this
        /// value is negative, it is subtracted from the left of the mouse-down point and added to the right of it.
        /// </summary>
        SM_CXDRAG = 68,

        /// <summary>The width of a 3-D border, in pixels. This metric is the 3-D counterpart of SM_CXBORDER.</summary>
        SM_CXEDGE = 45,

        /// <summary>
        /// The thickness of the frame around the perimeter of a window that has a caption but is not sizable, in pixels. SM_CXFIXEDFRAME
        /// is the height of the horizontal border, and SM_CYFIXEDFRAME is the width of the vertical border.
        /// <para>This value is the same as SM_CXDLGFRAME.</para>
        /// </summary>
        SM_CXFIXEDFRAME = 7,

        /// <summary>
        /// The width of the left and right edges of the focus rectangle that the DrawFocusRect draws. This value is in pixels.
        /// <para>Windows 2000: This value is not supported.</para>
        /// </summary>
        SM_CXFOCUSBORDER = 83,

        /// <summary>This value is the same as SM_CXSIZEFRAME.</summary>
        SM_CXFRAME = 32,

        /// <summary>
        /// The width of the client area for a full-screen window on the primary display monitor, in pixels. To get the coordinates of
        /// the portion of the screen that is not obscured by the system taskbar or by application desktop toolbars, call the
        /// SystemParametersInfo function with the SPI_GETWORKAREA value.
        /// </summary>
        SM_CXFULLSCREEN = 16,

        /// <summary>The width of the arrow bitmap on a horizontal scroll bar, in pixels.</summary>
        SM_CXHSCROLL = 21,

        /// <summary>The width of the thumb box in a horizontal scroll bar, in pixels.</summary>
        SM_CXHTHUMB = 10,

        /// <summary>
        /// The default width of an icon, in pixels. The LoadIcon function can load only icons with the dimensions that SM_CXICON and
        /// SM_CYICON specifies.
        /// </summary>
        SM_CXICON = 11,

        /// <summary>
        /// The width of a grid cell for items in large icon view, in pixels. Each item fits into a rectangle of size SM_CXICONSPACING by
        /// SM_CYICONSPACING when arranged. This value is always greater than or equal to SM_CXICON.
        /// </summary>
        SM_CXICONSPACING = 38,

        /// <summary>The default width, in pixels, of a maximized top-level window on the primary display monitor.</summary>
        SM_CXMAXIMIZED = 61,

        /// <summary>
        /// The default maximum width of a window that has a caption and sizing borders, in pixels. This metric refers to the entire
        /// desktop. The user cannot drag the window frame to a size larger than these dimensions. A window can override this value by
        /// processing the WM_GETMINMAXINFO message.
        /// </summary>
        SM_CXMAXTRACK = 59,

        /// <summary>The width of the default menu check-mark bitmap, in pixels.</summary>
        SM_CXMENUCHECK = 71,

        /// <summary>
        /// The width of menu bar buttons, such as the child window close button that is used in the multiple document interface, in pixels.
        /// </summary>
        SM_CXMENUSIZE = 54,

        /// <summary>The minimum width of a window, in pixels.</summary>
        SM_CXMIN = 28,

        /// <summary>The width of a minimized window, in pixels.</summary>
        SM_CXMINIMIZED = 57,

        /// <summary>
        /// The width of a grid cell for a minimized window, in pixels. Each minimized window fits into a rectangle this size when
        /// arranged. This value is always greater than or equal to SM_CXMINIMIZED.
        /// </summary>
        SM_CXMINSPACING = 47,

        /// <summary>
        /// The minimum tracking width of a window, in pixels. The user cannot drag the window frame to a size smaller than these
        /// dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
        /// </summary>
        SM_CXMINTRACK = 34,

        /// <summary>
        /// The amount of border padding for captioned windows, in pixels.
        /// <para>Windows XP/2000: This value is not supported.</para>
        /// </summary>
        SM_CXPADDEDBORDER = 92,

        /// <summary>
        /// The width of the screen of the primary display monitor, in pixels. This is the same value obtained by calling GetDeviceCaps
        /// as follows: GetDeviceCaps( hdcPrimaryMonitor, HORZRES).
        /// </summary>
        SM_CXSCREEN = 0,

        /// <summary>The width of a button in a window caption or title bar, in pixels.</summary>
        SM_CXSIZE = 30,

        /// <summary>
        /// The thickness of the sizing border around the perimeter of a window that can be resized, in pixels. SM_CXSIZEFRAME is the
        /// width of the horizontal border, and SM_CYSIZEFRAME is the height of the vertical border.
        /// <para>This value is the same as SM_CXFRAME.</para>
        /// </summary>
        SM_CXSIZEFRAME = 32,

        /// <summary>
        /// The recommended width of a small icon, in pixels. Small icons typically appear in window captions and in small icon view.
        /// </summary>
        SM_CXSMICON = 49,

        /// <summary>The width of small caption buttons, in pixels.</summary>
        SM_CXSMSIZE = 52,

        /// <summary>
        /// The width of the virtual screen, in pixels. The virtual screen is the bounding rectangle of all display monitors. The
        /// SM_XVIRTUALSCREEN metric is the coordinates for the left side of the virtual screen.
        /// </summary>
        SM_CXVIRTUALSCREEN = 78,

        /// <summary>The width of a vertical scroll bar, in pixels.</summary>
        SM_CXVSCROLL = 2,

        /// <summary>
        /// The height of a window border, in pixels. This is equivalent to the SM_CYEDGE value for windows with the 3-D look.
        /// </summary>
        SM_CYBORDER = 6,

        /// <summary>The height of a caption area, in pixels.</summary>
        SM_CYCAPTION = 4,

        /// <summary>The height of a cursor, in pixels. The system cannot create cursors of other sizes.</summary>
        SM_CYCURSOR = 14,

        /// <summary>This value is the same as SM_CYFIXEDFRAME.</summary>
        SM_CYDLGFRAME = 8,

        /// <summary>
        /// The height of the rectangle around the location of a first click in a double-click sequence, in pixels. The second click must
        /// occur within the rectangle defined by SM_CXDOUBLECLK and SM_CYDOUBLECLK for the system to consider the two clicks a
        /// double-click. The two clicks must also occur within a specified time.
        /// <para>To set the height of the double-click rectangle, call SystemParametersInfo with SPI_SETDOUBLECLKHEIGHT.</para>
        /// </summary>
        SM_CYDOUBLECLK = 37,

        /// <summary>
        /// The number of pixels above and below a mouse-down point that the mouse pointer can move before a drag operation begins. This
        /// allows the user to click and release the mouse button easily without unintentionally starting a drag operation. If this value
        /// is negative, it is subtracted from above the mouse-down point and added below it.
        /// </summary>
        SM_CYDRAG = 69,

        /// <summary>The height of a 3-D border, in pixels. This is the 3-D counterpart of SM_CYBORDER.</summary>
        SM_CYEDGE = 46,

        /// <summary>
        /// The thickness of the frame around the perimeter of a window that has a caption but is not sizable, in pixels. SM_CXFIXEDFRAME
        /// is the height of the horizontal border, and SM_CYFIXEDFRAME is the width of the vertical border.
        /// <para>This value is the same as SM_CYDLGFRAME.</para>
        /// </summary>
        SM_CYFIXEDFRAME = 8,

        /// <summary>
        /// The height of the top and bottom edges of the focus rectangle drawn by DrawFocusRect. This value is in pixels.
        /// <para>Windows 2000: This value is not supported.</para>
        /// </summary>
        SM_CYFOCUSBORDER = 84,

        /// <summary>This value is the same as SM_CYSIZEFRAME.</summary>
        SM_CYFRAME = 33,

        /// <summary>
        /// The height of the client area for a full-screen window on the primary display monitor, in pixels. To get the coordinates of
        /// the portion of the screen not obscured by the system taskbar or by application desktop toolbars, call the
        /// SystemParametersInfo function with the SPI_GETWORKAREA value.
        /// </summary>
        SM_CYFULLSCREEN = 17,

        /// <summary>The height of a horizontal scroll bar, in pixels.</summary>
        SM_CYHSCROLL = 3,

        /// <summary>
        /// The default height of an icon, in pixels. The LoadIcon function can load only icons with the dimensions SM_CXICON and SM_CYICON.
        /// </summary>
        SM_CYICON = 12,

        /// <summary>
        /// The height of a grid cell for items in large icon view, in pixels. Each item fits into a rectangle of size SM_CXICONSPACING
        /// by SM_CYICONSPACING when arranged. This value is always greater than or equal to SM_CYICON.
        /// </summary>
        SM_CYICONSPACING = 39,

        /// <summary>
        /// For double byte character set versions of the system, this is the height of the Kanji window at the bottom of the screen, in pixels.
        /// </summary>
        SM_CYKANJIWINDOW = 18,

        /// <summary>The default height, in pixels, of a maximized top-level window on the primary display monitor.</summary>
        SM_CYMAXIMIZED = 62,

        /// <summary>
        /// The default maximum height of a window that has a caption and sizing borders, in pixels. This metric refers to the entire
        /// desktop. The user cannot drag the window frame to a size larger than these dimensions. A window can override this value by
        /// processing the WM_GETMINMAXINFO message.
        /// </summary>
        SM_CYMAXTRACK = 60,

        /// <summary>The height of a single-line menu bar, in pixels.</summary>
        SM_CYMENU = 15,

        /// <summary>The height of the default menu check-mark bitmap, in pixels.</summary>
        SM_CYMENUCHECK = 72,

        /// <summary>
        /// The height of menu bar buttons, such as the child window close button that is used in the multiple document interface, in pixels.
        /// </summary>
        SM_CYMENUSIZE = 55,

        /// <summary>The minimum height of a window, in pixels.</summary>
        SM_CYMIN = 29,

        /// <summary>The height of a minimized window, in pixels.</summary>
        SM_CYMINIMIZED = 58,

        /// <summary>
        /// The height of a grid cell for a minimized window, in pixels. Each minimized window fits into a rectangle this size when
        /// arranged. This value is always greater than or equal to SM_CYMINIMIZED.
        /// </summary>
        SM_CYMINSPACING = 48,

        /// <summary>
        /// The minimum tracking height of a window, in pixels. The user cannot drag the window frame to a size smaller than these
        /// dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
        /// </summary>
        SM_CYMINTRACK = 35,

        /// <summary>
        /// The height of the screen of the primary display monitor, in pixels. This is the same value obtained by calling GetDeviceCaps
        /// as follows: GetDeviceCaps( hdcPrimaryMonitor, VERTRES).
        /// </summary>
        SM_CYSCREEN = 1,

        /// <summary>The height of a button in a window caption or title bar, in pixels.</summary>
        SM_CYSIZE = 31,

        /// <summary>
        /// The thickness of the sizing border around the perimeter of a window that can be resized, in pixels. SM_CXSIZEFRAME is the
        /// width of the horizontal border, and SM_CYSIZEFRAME is the height of the vertical border.
        /// <para>This value is the same as SM_CYFRAME.</para>
        /// </summary>
        SM_CYSIZEFRAME = 33,

        /// <summary>The height of a small caption, in pixels.</summary>
        SM_CYSMCAPTION = 51,

        /// <summary>
        /// The recommended height of a small icon, in pixels. Small icons typically appear in window captions and in small icon view.
        /// </summary>
        SM_CYSMICON = 50,

        /// <summary>The height of small caption buttons, in pixels.</summary>
        SM_CYSMSIZE = 53,

        /// <summary>
        /// The height of the virtual screen, in pixels. The virtual screen is the bounding rectangle of all display monitors. The
        /// SM_YVIRTUALSCREEN metric is the coordinates for the top of the virtual screen.
        /// </summary>
        SM_CYVIRTUALSCREEN = 79,

        /// <summary>The height of the arrow bitmap on a vertical scroll bar, in pixels.</summary>
        SM_CYVSCROLL = 20,

        /// <summary>The height of the thumb box in a vertical scroll bar, in pixels.</summary>
        SM_CYVTHUMB = 9,

        /// <summary>Nonzero if User32.dll supports DBCS; otherwise, 0.</summary>
        SM_DBCSENABLED = 42,

        /// <summary>Nonzero if the debug version of User.exe is installed; otherwise, 0.</summary>
        SM_DEBUG = 22,

        /// <summary>
        /// Nonzero if the current operating system is Windows 7 or Windows Server 2008 R2 and the Tablet PC Input service is started;
        /// otherwise, 0. The return value is a bitmask that specifies the type of digitizer input supported by the device. For more
        /// information, see Remarks.
        /// <para>Windows Server 2008, Windows Vista and Windows XP/2000: This value is not supported.</para>
        /// </summary>
        SM_DIGITIZER = 94,

        /// <summary>
        /// Nonzero if Input Method Manager/Input Method Editor features are enabled; otherwise, 0.
        /// <para>
        /// SM_IMMENABLED indicates whether the system is ready to use a Unicode-based IME on a Unicode application. To ensure that a
        /// language-dependent IME works, check SM_DBCSENABLED and the system ANSI code page. Otherwise the ANSI-to-Unicode conversion
        /// may not be performed correctly, or some components like fonts or registry settings may not be present.
        /// </para>
        /// </summary>
        SM_IMMENABLED = 82,

        /// <summary>
        /// Nonzero if there are digitizers in the system; otherwise, 0.
        /// <para>
        /// SM_MAXIMUMTOUCHES returns the aggregate maximum of the maximum number of contacts supported by every digitizer in the system.
        /// If the system has only single-touch digitizers, the return value is 1. If the system has multi-touch digitizers, the return
        /// value is the number of simultaneous contacts the hardware can provide.
        /// </para>
        /// <para>Windows Server 2008, Windows Vista and Windows XP/2000: This value is not supported.</para>
        /// </summary>
        SM_MAXIMUMTOUCHES = 95,

        /// <summary>Nonzero if the current operating system is the Windows XP, Media Center Edition, 0 if not.</summary>
        SM_MEDIACENTER = 87,

        /// <summary>Nonzero if drop-down menus are right-aligned with the corresponding menu-bar item; 0 if the menus are left-aligned.</summary>
        SM_MENUDROPALIGNMENT = 40,

        /// <summary>Nonzero if the system is enabled for Hebrew and Arabic languages, 0 if not.</summary>
        SM_MIDEASTENABLED = 74,

        /// <summary>
        /// Nonzero if a mouse is installed; otherwise, 0. This value is rarely zero, because of support for virtual mice and because
        /// some systems detect the presence of the port instead of the presence of a mouse.
        /// </summary>
        SM_MOUSEPRESENT = 19,

        /// <summary>Nonzero if a mouse with a horizontal scroll wheel is installed; otherwise 0.</summary>
        SM_MOUSEHORIZONTALWHEELPRESENT = 91,

        /// <summary>Nonzero if a mouse with a vertical scroll wheel is installed; otherwise 0.</summary>
        SM_MOUSEWHEELPRESENT = 75,

        /// <summary>
        /// The least significant bit is set if a network is present; otherwise, it is cleared. The other bits are reserved for future use.
        /// </summary>
        SM_NETWORK = 63,

        /// <summary>Nonzero if the Microsoft Windows for Pen computing extensions are installed; zero otherwise.</summary>
        SM_PENWINDOWS = 41,

        /// <summary>
        /// This system metric is used in a Terminal Services environment to determine if the current Terminal Server session is being
        /// remotely controlled. Its value is nonzero if the current session is remotely controlled; otherwise, 0.
        /// <para>
        /// You can use terminal services management tools such as Terminal Services Manager (tsadmin.msc) and shadow.exe to control a
        /// remote session. When a session is being remotely controlled, another user can view the contents of that session and
        /// potentially interact with it.
        /// </para>
        /// </summary>
        SM_REMOTECONTROL = 0x2001,

        /// <summary>
        /// This system metric is used in a Terminal Services environment. If the calling process is associated with a Terminal Services
        /// client session, the return value is nonzero. If the calling process is associated with the Terminal Services console session,
        /// the return value is 0.
        /// <para>
        /// Windows Server 2003 and Windows XP: The console session is not necessarily the physical console. For more information, see WTSGetActiveConsoleSessionId.
        /// </para>
        /// </summary>
        SM_REMOTESESSION = 0x1000,

        /// <summary>
        /// Nonzero if all the display monitors have the same color format, otherwise, 0. Two displays can have the same bit depth, but
        /// different color formats. For example, the red, green, and blue pixels can be encoded with different numbers of bits, or those
        /// bits can be located in different places in a pixel color value.
        /// </summary>
        SM_SAMEDISPLAYFORMAT = 81,

        /// <summary>This system metric should be ignored; it always returns 0.</summary>
        SM_SECURE = 44,

        /// <summary>The build number if the system is Windows Server 2003 R2; otherwise, 0.</summary>
        SM_SERVERR2 = 89,

        /// <summary>
        /// Nonzero if the user requires an application to present information visually in situations where it would otherwise present
        /// the information only in audible form; otherwise, 0.
        /// </summary>
        SM_SHOWSOUNDS = 70,

        /// <summary>
        /// Nonzero if the current session is shutting down; otherwise, 0.
        /// <para>Windows 2000: This value is not supported.</para>
        /// </summary>
        SM_SHUTTINGDOWN = 0x2000,

        /// <summary>Nonzero if the computer has a low-end (slow) processor; otherwise, 0.</summary>
        SM_SLOWMACHINE = 73,

        /// <summary>
        /// Nonzero if the current operating system is Windows 7 Starter Edition, Windows Vista Starter, or Windows XP Starter Edition;
        /// otherwise, 0.
        /// </summary>
        SM_STARTER = 88,

        /// <summary>Nonzero if the meanings of the left and right mouse buttons are swapped; otherwise, 0.</summary>
        SM_SWAPBUTTON = 23,

        /// <summary>
        /// Reflects the state of the docking mode, 0 for Undocked Mode and non-zero otherwise. When this system metric changes, the
        /// system sends a broadcast message via WM_SETTINGCHANGE with "SystemDockMode" in the LPARAM.
        /// </summary>
        SM_SYSTEMDOCKED = 0x2004,

        /// <summary>
        /// Nonzero if the current operating system is the Windows XP Tablet PC edition or if the current operating system is Windows
        /// Vista or Windows 7 and the Tablet PC Input service is started; otherwise, 0. The SM_DIGITIZER setting indicates the type of
        /// digitizer input supported by a device running Windows 7 or Windows Server 2008 R2. For more information, see Remarks.
        /// </summary>
        SM_TABLETPC = 86,

        /// <summary>
        /// The coordinates for the left side of the virtual screen. The virtual screen is the bounding rectangle of all display
        /// monitors. The SM_CXVIRTUALSCREEN metric is the width of the virtual screen.
        /// </summary>
        SM_XVIRTUALSCREEN = 76,

        /// <summary>
        /// The coordinates for the top of the virtual screen. The virtual screen is the bounding rectangle of all display monitors. The
        /// SM_CYVIRTUALSCREEN metric is the height of the virtual screen.
        /// </summary>
        SM_YVIRTUALSCREEN = 77,
    }

    /// <summary>
    /// Specifies how the window should be shown.
    /// </summary>
    public enum ShowWindowEnum
    {
        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        Hide = 0,

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, it is restored to its original size and position.
        /// </summary>
        ShowNormal = 1,

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,

        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>
        ShowMaximized = 3,

        /// <summary>
        /// Displays the window in its most recent size and position. This value is similar to ShowNormal, except the window is not activated.
        /// </summary>
        ShowNormalNoActivate = 4,

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,

        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        Minimize = 6,

        /// <summary>
        /// Displays the window as a minimized window. This value is similar to ShowMinimized, except the window is not activated.
        /// </summary>
        ShowMinNoActivate = 7,

        /// <summary>
        /// Displays the window in its current size and position. This value is similar to Show, except the window is not activated.
        /// </summary>
        ShowNoActivate = 8,

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, it is restored to its original size and position. 
        /// </summary>
        Restore = 9,

        /// <summary>
        /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
        /// </summary>
        ShowDefault = 10,

        /// <summary>
        /// Minimizes a window, even if the thread that owns the window is not responding. Use this only when minimizing windows from a different thread.
        /// </summary>
        ForceMinimized = 11
    }

    /// <summary>The relationship between the specified window and the window whose handle is to be retrieved. Used with GetWindow method.</summary>
    public enum GetWindowCmd : int
    {
        /// <summary>
        /// The retrieved handle identifies the window of the same type that is highest in the Z order. If the specified window is a
        /// topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies
        /// a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDFIRST = 0,

        /// <summary>
        /// The retrieved handle identifies the window of the same type that is lowest in the Z order. If the specified window is a
        /// topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies
        /// a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDLAST = 1,

        /// <summary>
        /// The retrieved handle identifies the window below the specified window in the Z order. If the specified window is a topmost
        /// window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a
        /// top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDNEXT = 2,

        /// <summary>
        /// The retrieved handle identifies the window above the specified window in the Z order. If the specified window is a topmost
        /// window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a
        /// top-level window. If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDPREV = 3,

        /// <summary>
        /// The retrieved handle identifies the specified window's owner window, if any. For more information, see Owned Windows.
        /// </summary>
        GW_OWNER = 4,

        /// <summary>
        /// The retrieved handle identifies the child window at the top of the Z order, if the specified window is a parent window;
        /// otherwise, the retrieved handle is NULL. The function examines only child windows of the specified window. It does not
        /// examine descendant windows.
        /// </summary>
        GW_CHILD = 5,

        /// <summary>
        /// The retrieved handle identifies the enabled popup window owned by the specified window (the search uses the first such window
        /// found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is that of the specified window.
        /// </summary>
        GW_ENABLEDPOPUP = 6
    }

    #endregion

    #region ntdll.dll

    public enum PEB_OFFSET
    {
        CurrentDirectory,
        CommandLine
    }

    public enum PROCESSINFOCLASS : int
    {
        ProcessBasicInformation = 0,
        ProcessWow64Information = 26
    }

    /// <summary>
    /// Enum representing different Windows product types.
    /// </summary>
    public enum PRODUCT_SKU : uint
    {
        /// <summary>An unknown product type.</summary>
        PRODUCT_UNDEFINED = 0x00000000,

        /// <summary>Windows Ultimate edition.</summary>
        PRODUCT_ULTIMATE = 0x00000001,

        /// <summary>Windows Home Basic edition.</summary>
        PRODUCT_HOME_BASIC = 0x00000002,

        /// <summary>Windows Home Premium edition.</summary>
        PRODUCT_HOME_PREMIUM = 0x00000003,

        /// <summary>Windows Enterprise edition.</summary>
        PRODUCT_ENTERPRISE = 0x00000004,

        /// <summary>Windows Home Basic N edition.</summary>
        PRODUCT_HOME_BASIC_N = 0x00000005,

        /// <summary>Windows Business edition.</summary>
        PRODUCT_BUSINESS = 0x00000006,

        /// <summary>Windows Server Standard edition.</summary>
        PRODUCT_STANDARD_SERVER = 0x00000007,

        /// <summary>Windows Server Datacenter edition.</summary>
        PRODUCT_DATACENTER_SERVER = 0x00000008,

        /// <summary>Windows Small Business Server.</summary>
        PRODUCT_SMALLBUSINESS_SERVER = 0x00000009,

        /// <summary>Windows Server Enterprise edition.</summary>
        PRODUCT_ENTERPRISE_SERVER = 0x0000000A,

        /// <summary>Windows Starter edition.</summary>
        PRODUCT_STARTER = 0x0000000B,

        /// <summary>Windows Server Datacenter (core installation).</summary>
        PRODUCT_DATACENTER_SERVER_CORE = 0x0000000C,

        /// <summary>Windows Server Standard (core installation).</summary>
        PRODUCT_STANDARD_SERVER_CORE = 0x0000000D,

        /// <summary>Windows Server Enterprise (core installation).</summary>
        PRODUCT_ENTERPRISE_SERVER_CORE = 0x0000000E,

        /// <summary>Windows Server Enterprise for Itanium-based systems.</summary>
        PRODUCT_ENTERPRISE_SERVER_IA64 = 0x0000000F,

        /// <summary>Windows Business N edition.</summary>
        PRODUCT_BUSINESS_N = 0x00000010,

        /// <summary>Windows Web Server edition.</summary>
        PRODUCT_WEB_SERVER = 0x00000011,

        /// <summary>Windows HPC Edition (Cluster Server).</summary>
        PRODUCT_CLUSTER_SERVER = 0x00000012,

        /// <summary>Windows Home Server.</summary>
        PRODUCT_HOME_SERVER = 0x00000013,

        /// <summary>Windows Storage Server Express edition.</summary>
        PRODUCT_STORAGE_EXPRESS_SERVER = 0x00000014,

        /// <summary>Windows Storage Server Standard edition.</summary>
        PRODUCT_STORAGE_STANDARD_SERVER = 0x00000015,

        /// <summary>Windows Storage Server Workgroup edition.</summary>
        PRODUCT_STORAGE_WORKGROUP_SERVER = 0x00000016,

        /// <summary>Windows Storage Server Enterprise edition.</summary>
        PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x00000017,

        /// <summary>Windows Server for Small Business.</summary>
        PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x00000018,

        /// <summary>Windows Small Business Server Premium edition.</summary>
        PRODUCT_SMALLBUSINESS_SERVER_PREMIUM = 0x00000019,

        /// <summary>Windows Home Premium N edition.</summary>
        PRODUCT_HOME_PREMIUM_N = 0x0000001A,

        /// <summary>Windows Enterprise N edition.</summary>
        PRODUCT_ENTERPRISE_N = 0x0000001B,

        /// <summary>Windows Ultimate N edition.</summary>
        PRODUCT_ULTIMATE_N = 0x0000001C,

        /// <summary>Windows Web Server (core installation).</summary>
        PRODUCT_WEB_SERVER_CORE = 0x0000001D,

        /// <summary>Windows Medium Business Server Management.</summary>
        PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT = 0x0000001E,

        /// <summary>Windows Medium Business Server Security.</summary>
        PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY = 0x0000001F,

        /// <summary>Windows Medium Business Server Messaging.</summary>
        PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING = 0x00000020,

        /// <summary>Windows Server Foundation.</summary>
        PRODUCT_SERVER_FOUNDATION = 0x00000021,

        /// <summary>Windows Home Premium Server.</summary>
        PRODUCT_HOME_PREMIUM_SERVER = 0x00000022,

        /// <summary>Windows Server for Small Business (with Hyper-V).</summary>
        PRODUCT_SERVER_FOR_SMALLBUSINESS_V = 0x00000023,

        /// <summary>Windows Server Standard (without Hyper-V).</summary>
        PRODUCT_STANDARD_SERVER_V = 0x00000024,

        /// <summary>Windows Server Datacenter (without Hyper-V).</summary>
        PRODUCT_DATACENTER_SERVER_V = 0x00000025,

        /// <summary>Windows Server Enterprise (without Hyper-V).</summary>
        PRODUCT_ENTERPRISE_SERVER_V = 0x00000026,

        /// <summary>Windows Server Datacenter (core installation without Hyper-V).</summary>
        PRODUCT_DATACENTER_SERVER_CORE_V = 0x00000027,

        /// <summary>Windows Server Standard (core installation without Hyper-V).</summary>
        PRODUCT_STANDARD_SERVER_CORE_V = 0x00000028,

        /// <summary>Windows Server Enterprise (core installation without Hyper-V).</summary>
        PRODUCT_ENTERPRISE_SERVER_CORE_V = 0x00000029,

        /// <summary>Windows Hyper-V Server.</summary>
        PRODUCT_HYPERV_SERVER = 0x0000002A,

        /// <summary>Windows Storage Server Express (core installation).</summary>
        PRODUCT_STORAGE_EXPRESS_SERVER_CORE = 0x0000002B,

        /// <summary>Windows Storage Server Standard (core installation).</summary>
        PRODUCT_STORAGE_STANDARD_SERVER_CORE = 0x0000002C,

        /// <summary>Windows Storage Server Workgroup (core installation).</summary>
        PRODUCT_STORAGE_WORKGROUP_SERVER_CORE = 0x0000002D,

        /// <summary>Windows Storage Server Enterprise (core installation).</summary>
        PRODUCT_STORAGE_ENTERPRISE_SERVER_CORE = 0x0000002E,

        /// <summary>Windows Starter N edition.</summary>
        PRODUCT_STARTER_N = 0x0000002F,

        /// <summary>Windows Professional edition.</summary>
        PRODUCT_PROFESSIONAL = 0x00000030,

        /// <summary>Windows Professional N edition.</summary>
        PRODUCT_PROFESSIONAL_N = 0x00000031,

        /// <summary>Windows Small Business Server Solution.</summary>
        PRODUCT_SB_SOLUTION_SERVER = 0x00000032,

        /// <summary>Windows Server for Small Business Solutions.</summary>
        PRODUCT_SERVER_FOR_SB_SOLUTIONS = 0x00000033,

        /// <summary>Windows Standard Server Solutions.</summary>
        PRODUCT_STANDARD_SERVER_SOLUTIONS = 0x00000034,

        /// <summary>Windows Standard Server Solutions (core installation).</summary>
        PRODUCT_STANDARD_SERVER_SOLUTIONS_CORE = 0x00000035,

        /// <summary>Windows Small Business Server Solution (EM version).</summary>
        PRODUCT_SB_SOLUTION_SERVER_EM = 0x00000036,

        /// <summary>Windows Server for Small Business Solutions (EM version).</summary>
        PRODUCT_SERVER_FOR_SB_SOLUTIONS_EM = 0x00000037,

        /// <summary>Windows Embedded Server Solution.</summary>
        PRODUCT_SOLUTION_EMBEDDEDSERVER = 0x00000038,

        /// <summary>Windows Embedded Server Solution (core installation).</summary>
        PRODUCT_SOLUTION_EMBEDDEDSERVER_CORE = 0x00000039,

        /// <summary>Windows Essential Business Server Management.</summary>
        PRODUCT_ESSENTIALBUSINESS_SERVER_MGMT = 0x0000003B,

        /// <summary>Windows Essential Business Server Additional.</summary>
        PRODUCT_ESSENTIALBUSINESS_SERVER_ADDL = 0x0000003C,

        /// <summary>Windows Essential Business Server Management SVC.</summary>
        PRODUCT_ESSENTIALBUSINESS_SERVER_MGMTSVC = 0x0000003D,

        /// <summary>Windows Essential Business Server Additional SVC.</summary>
        PRODUCT_ESSENTIALBUSINESS_SERVER_ADDLSVC = 0x0000003E,

        /// <summary>Windows Small Business Server Premium (core installation).</summary>
        PRODUCT_SMALLBUSINESS_SERVER_PREMIUM_CORE = 0x0000003F,

        /// <summary>Windows HPC Edition (Cluster Server V).</summary>
        PRODUCT_CLUSTER_SERVER_V = 0x00000040,

        /// <summary>Windows Embedded edition.</summary>
        PRODUCT_EMBEDDED = 0x00000041,

        /// <summary>Windows Starter E edition (not supported).</summary>
        PRODUCT_STARTER_E = 0x00000042,

        /// <summary>Windows Home Basic E edition (not supported).</summary>
        PRODUCT_HOME_BASIC_E = 0x00000043,

        /// <summary>Windows Home Premium E edition (not supported).</summary>
        PRODUCT_HOME_PREMIUM_E = 0x00000044,

        /// <summary>Windows Professional E edition (not supported).</summary>
        PRODUCT_PROFESSIONAL_E = 0x00000045,

        /// <summary>Windows Enterprise E edition.</summary>
        PRODUCT_ENTERPRISE_E = 0x00000046,

        /// <summary>Windows Ultimate E edition (not supported).</summary>
        PRODUCT_ULTIMATE_E = 0x00000047,

        /// <summary>Windows Enterprise Evaluation edition.</summary>
        PRODUCT_ENTERPRISE_EVALUATION = 0x00000048,

        /// <summary>Windows MultiPoint Server Standard edition.</summary>
        PRODUCT_MULTIPOINT_STANDARD_SERVER = 0x0000004C,

        /// <summary>Windows MultiPoint Server Premium edition.</summary>
        PRODUCT_MULTIPOINT_PREMIUM_SERVER = 0x0000004D,

        /// <summary>Windows Server Standard Evaluation edition.</summary>
        PRODUCT_STANDARD_EVALUATION_SERVER = 0x0000004F,

        /// <summary>Windows Server Datacenter Evaluation edition.</summary>
        PRODUCT_DATACENTER_EVALUATION_SERVER = 0x00000050,

        /// <summary>Windows Enterprise N Evaluation edition.</summary>
        PRODUCT_ENTERPRISE_N_EVALUATION = 0x00000054,

        /// <summary>Windows Embedded Automotive edition.</summary>
        PRODUCT_EMBEDDED_AUTOMOTIVE = 0x00000055,

        /// <summary>Windows Embedded Industry A edition.</summary>
        PRODUCT_EMBEDDED_INDUSTRY_A = 0x00000056,

        /// <summary>Windows ThinPC edition.</summary>
        PRODUCT_THINPC = 0x00000057,

        /// <summary>Windows Embedded A edition.</summary>
        PRODUCT_EMBEDDED_A = 0x00000058,

        /// <summary>Windows Embedded Industry edition.</summary>
        PRODUCT_EMBEDDED_INDUSTRY = 0x00000059,

        /// <summary>Windows Embedded E edition.</summary>
        PRODUCT_EMBEDDED_E = 0x0000005A,

        /// <summary>Windows Embedded Industry E edition.</summary>
        PRODUCT_EMBEDDED_INDUSTRY_E = 0x0000005B,

        /// <summary>Windows Embedded Industry A E edition.</summary>
        PRODUCT_EMBEDDED_INDUSTRY_A_E = 0x0000005C,

        /// <summary>Windows Storage Server Workgroup Evaluation edition.</summary>
        PRODUCT_STORAGE_WORKGROUP_EVALUATION_SERVER = 0x0000005F,

        /// <summary>Windows Storage Server Standard Evaluation edition.</summary>
        PRODUCT_STORAGE_STANDARD_EVALUATION_SERVER = 0x00000060,

        /// <summary>Windows Core ARM edition.</summary>
        PRODUCT_CORE_ARM = 0x00000061,

        /// <summary>Windows Core N edition.</summary>
        PRODUCT_CORE_N = 0x00000062,

        /// <summary>Windows Core Country Specific edition.</summary>
        PRODUCT_CORE_COUNTRYSPECIFIC = 0x00000063,

        /// <summary>Windows Core Single Language edition.</summary>
        PRODUCT_CORE_SINGLELANGUAGE = 0x00000064,

        /// <summary>Windows Core edition.</summary>
        PRODUCT_CORE = 0x00000065,

        /// <summary>Windows Professional with Media Center edition.</summary>
        PRODUCT_PROFESSIONAL_WMC = 0x00000067,

        /// <summary>Windows Mobile Core edition.</summary>
        PRODUCT_MOBILE_CORE = 0x00000068,

        /// <summary>Windows Embedded Industry Evaluation edition.</summary>
        PRODUCT_EMBEDDED_INDUSTRY_EVAL = 0x00000069,

        /// <summary>Windows Embedded Industry E Evaluation edition.</summary>
        PRODUCT_EMBEDDED_INDUSTRY_E_EVAL = 0x0000006A,

        /// <summary>Windows Embedded Evaluation edition.</summary>
        PRODUCT_EMBEDDED_EVAL = 0x0000006B,

        /// <summary>Windows Embedded E Evaluation edition.</summary>
        PRODUCT_EMBEDDED_E_EVAL = 0x0000006C,

        /// <summary>Windows Nano Server edition.</summary>
        PRODUCT_NANO_SERVER = 0x0000006D,

        /// <summary>Windows Cloud Storage Server edition.</summary>
        PRODUCT_CLOUD_STORAGE_SERVER = 0x0000006E,

        /// <summary>Windows Core Connected edition.</summary>
        PRODUCT_CORE_CONNECTED = 0x0000006F,

        /// <summary>Windows Professional Student edition.</summary>
        PRODUCT_PROFESSIONAL_STUDENT = 0x00000070,

        /// <summary>Windows Core Connected N edition.</summary>
        PRODUCT_CORE_CONNECTED_N = 0x00000071,

        /// <summary>Windows Professional Student N edition.</summary>
        PRODUCT_PROFESSIONAL_STUDENT_N = 0x00000072,

        /// <summary>Windows Core Connected Single Language edition.</summary>
        PRODUCT_CORE_CONNECTED_SINGLELANGUAGE = 0x00000073,

        /// <summary>Windows Core Connected Country Specific edition.</summary>
        PRODUCT_CORE_CONNECTED_COUNTRYSPECIFIC = 0x00000074,

        /// <summary>Windows Connected Car edition.</summary>
        PRODUCT_CONNECTED_CAR = 0x00000075,

        /// <summary>Windows Industry Handheld edition.</summary>
        PRODUCT_INDUSTRY_HANDHELD = 0x00000076,

        /// <summary>Windows PPI Pro edition.</summary>
        PRODUCT_PPI_PRO = 0x00000077,

        /// <summary>Windows ARM64 Server edition.</summary>
        PRODUCT_ARM64_SERVER = 0x00000078,

        /// <summary>Windows Education edition.</summary>
        PRODUCT_EDUCATION = 0x00000079,

        /// <summary>Windows Education N edition.</summary>
        PRODUCT_EDUCATION_N = 0x0000007A,

        /// <summary>Windows IoT Core edition.</summary>
        PRODUCT_IOTUAP = 0x0000007B,

        /// <summary>Windows Cloud Host Infrastructure Server edition.</summary>
        PRODUCT_CLOUD_HOST_INFRASTRUCTURE_SERVER = 0x0000007C,

        /// <summary>Windows Enterprise S edition.</summary>
        PRODUCT_ENTERPRISE_S = 0x0000007D,

        /// <summary>Windows Enterprise S N edition.</summary>
        PRODUCT_ENTERPRISE_S_N = 0x0000007E,

        /// <summary>Windows Professional S edition.</summary>
        PRODUCT_PROFESSIONAL_S = 0x0000007F,

        /// <summary>Windows Professional S N edition.</summary>
        PRODUCT_PROFESSIONAL_S_N = 0x00000080,

        /// <summary>Windows Enterprise S Evaluation edition.</summary>
        PRODUCT_ENTERPRISE_S_EVALUATION = 0x00000081,

        /// <summary>Windows Enterprise S N Evaluation edition.</summary>
        PRODUCT_ENTERPRISE_S_N_EVALUATION = 0x00000082,

        /// <summary>Windows Mobile Enterprise edition.</summary>
        PRODUCT_MOBILE_ENTERPRISE = 0x00000085,

        /// <summary>Windows Holographic edition.</summary>
        PRODUCT_HOLOGRAPHIC = 0x00000087,

        /// <summary>Windows Holographic Business edition.</summary>
        PRODUCT_HOLOGRAPHIC_BUSINESS = 0x00000088,

        /// <summary>Windows Server RDSH (Remote Desktop Session Host).</summary>
        PRODUCT_SERVERRDSH = 0x000000AF,

        /// <summary>Windows Cloud edition.</summary>
        PRODUCT_CLOUD = 0x000000B2,

        /// <summary>Windows Cloud N edition.</summary>
        PRODUCT_CLOUDN = 0x000000B3,

        /// <summary>Windows Hub OS edition.</summary>
        PRODUCT_HUBOS = 0x000000B4,

        /// <summary>Windows OneCore Update OS edition.</summary>
        PRODUCT_ONECOREUPDATEOS = 0x000000B6,

        /// <summary>Windows Cloud E edition.</summary>
        PRODUCT_CLOUDE = 0x000000B7,

        /// <summary>Windows IoT OS edition.</summary>
        PRODUCT_IOTOS = 0x000000B8,

        /// <summary>Windows Cloud EN edition.</summary>
        PRODUCT_CLOUDEN = 0x000000BA,

        /// <summary>Windows Lite edition.</summary>
        PRODUCT_LITE = 0x000000BD,

        /// <summary>An unlicensed product.</summary>
        PRODUCT_UNLICENSED = 0xABCDABCD,

        /// <summary>Windows Cloud Shell edition.</summary>
        PRODUCT_CLOUD_SHELL = 0x000000F3,

        /// <summary>Windows 10 IoT Core Commercial</summary>
        PRODUCT_IOTENTERPRISE = 0x00000083,

        /// <summary>Windows Core Server edition.</summary>
        PRODUCT_CORE_SERVER = 0x0000012A,

        /// <summary>Windows Core Server (core installation) edition.</summary>
        PRODUCT_CORE_SERVER_CORRE = 0x0000012B,

        /// <summary>Windows Core Server Country Specific edition.</summary>
        PRODUCT_CORE_SERVER_COUNTRYSPECIFIC = 0x0000012C,

        /// <summary>Windows Core Server Enterprise edition.</summary>
        PRODUCT_CORE_SERVER_ENTERPRISE = 0x0000012D,

        /// <summary>Windows Core Server Single Language edition.</summary>
        PRODUCT_CORE_SERVER_SINGLELANGUAGE = 0x0000012E,

        /// <summary>Windows Core Server (core installation) edition.</summary>
        PRODUCT_CORE_SERVER_CORE = 0x0000012F,

        /// <summary>Windows Hub Server edition.</summary>
        PRODUCT_HUB_SERVER = 0x00000131,

        /// <summary>Windows Holographic Enterprise edition.</summary>
        PRODUCT_HOLOGRAPHIC_ENTERPRISE = 0x00000134,

        /// <summary>Windows Azure Server (core installation) edition.</summary>
        PRODUCT_AZURE_SERVER_CORE = 0x00000135,

        /// <summary>Windows Azure Server with Cloud Hyper-V edition.</summary>
        PRODUCT_AZURE_SERVER_CLOUDHYPERV = 0x00000136,

        /// <summary>Windows Azure Server edition.</summary>
        PRODUCT_AZURE_SERVER = 0x00000137,

        /// <summary>Windows Server Datacenter Core edition.</summary>
        PRODUCT_DATACENTER_CORE_SERVER = 0x00000138,

        /// <summary>Windows Server Standard Core edition.</summary>
        PRODUCT_STANDARD_CORE_SERVER = 0x00000139,

        /// <summary>Windows Server Enterprise Core edition.</summary>
        PRODUCT_ENTERPRISE_CORE_SERVER = 0x0000013A
    }

    public enum ProductType : byte
    {
        /// <summary>
        /// The operating system is Windows 10, Windows 8, Windows 7,...
        /// </summary>
        /// <remarks>VER_NT_WORKSTATION</remarks>
        Workstation = 0x0000001,
        /// <summary>
        /// The system is a domain controller and the operating system is Windows Server.
        /// </summary>
        /// <remarks>VER_NT_DOMAIN_CONTROLLER</remarks>
        DomainController = 0x0000002,
        /// <summary>
        /// The operating system is Windows Server. Note that a server that is also a domain controller
        /// is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
        /// </summary>
        /// <remarks>VER_NT_SERVER</remarks>
        Server = 0x0000003
    }

    [Flags]
    public enum SuiteMask : ushort
    {
        /// <summary>
        /// Microsoft BackOffice components are installed. 
        /// </summary>
        VER_SUITE_BACKOFFICE = 0x00000004,
        /// <summary>
        /// Windows Server 2003, Web Edition is installed
        /// </summary>
        VER_SUITE_BLADE = 0x00000400,
        /// <summary>
        /// Windows Server 2003, Compute Cluster Edition is installed.
        /// </summary>
        VER_SUITE_COMPUTE_SERVER = 0x00004000,
        /// <summary>
        /// Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, or Windows 2000 Datacenter Server is installed. 
        /// </summary>
        VER_SUITE_DATACENTER = 0x00000080,
        /// <summary>
        /// Windows Server 2008 Enterprise, Windows Server 2003, Enterprise Edition, or Windows 2000 Advanced Server is installed.
        /// Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_ENTERPRISE = 0x00000002,
        /// <summary>
        /// Windows XP Embedded is installed. 
        /// </summary>
        VER_SUITE_EMBEDDEDNT = 0x00000040,
        /// <summary>
        /// Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition is installed. 
        /// </summary>
        VER_SUITE_PERSONAL = 0x00000200,
        /// <summary>
        /// Remote Desktop is supported, but only one interactive session is supported. This value is set unless the system is running in application server mode. 
        /// </summary>
        VER_SUITE_SINGLEUSERTS = 0x00000100,
        /// <summary>
        /// Microsoft Small Business Server was once installed on the system, but may have been upgraded to another version of Windows.
        /// Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_SMALLBUSINESS = 0x00000001,
        /// <summary>
        /// Microsoft Small Business Server is installed with the restrictive client license in force. Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_SMALLBUSINESS_RESTRICTED = 0x00000020,
        /// <summary>
        /// Windows Storage Server 2003 R2 or Windows Storage Server 2003 is installed. 
        /// </summary>
        VER_SUITE_STORAGE_SERVER = 0x00002000,
        /// <summary>
        /// Terminal Services is installed. This value is always set.
        /// If VER_SUITE_TERMINAL is set but VER_SUITE_SINGLEUSERTS is not set, the system is running in application server mode.
        /// </summary>
        VER_SUITE_TERMINAL = 0x00000010,
        /// <summary>
        /// Windows Home Server is installed. 
        /// </summary>
        VER_SUITE_WH_SERVER = 0x00008000
    }

    #endregion

    #region shell32.dll

    /// <summary>
	/// The flags that specify the file information to retrieve from <see cref="NativeMethods.SHGetFileInfo"/>.
	/// </summary>
    [Flags]
    public enum SHGFI
    {
        /// <summary>
        /// Retrieve the handle to the icon that represents the file and the index of the icon within the system image list. The handle
        /// is copied to the hIcon member of the structure specified by psfi, and the index is copied to the iIcon member.
        /// </summary>
        SHGFI_ICON = 0x000000100,

        /// <summary>
        /// Retrieve the display name for the file, which is the name as it appears in Windows Explorer. The name is copied to the
        /// szDisplayName member of the structure specified in psfi. The returned display name uses the long file name, if there is one,
        /// rather than the 8.3 form of the file name. Note that the display name can be affected by settings such as whether extensions
        /// are shown.
        /// </summary>
        SHGFI_DISPLAYNAME = 0x000000200,

        /// <summary>
        /// Retrieve the string that describes the file's type. The string is copied to the szTypeName member of the structure specified
        /// in psfi.
        /// </summary>
        SHGFI_TYPENAME = 0x000000400,

        /// <summary>
        /// Retrieve the item attributes. The attributes are copied to the dwAttributes member of the structure specified in the psfi
        /// parameter. These are the same attributes that are obtained from IShellFolder::GetAttributesOf.
        /// </summary>
        SHGFI_ATTRIBUTES = 0x000000800,

        /// <summary>
        /// Retrieve the name of the file that contains the icon representing the file specified by pszPath, as returned by the
        /// IExtractIcon::GetIconLocation method of the file's icon handler. Also retrieve the icon index within that file. The name of
        /// the file containing the icon is copied to the szDisplayName member of the structure specified by psfi. The icon's index is
        /// copied to that structure's iIcon member.
        /// </summary>
        SHGFI_ICONLOCATION = 0x000001000,

        /// <summary>
        /// Retrieve the type of the executable file if pszPath identifies an executable file. The information is packed into the return
        /// value. This flag cannot be specified with any other flags.
        /// </summary>
        SHGFI_EXETYPE = 0x000002000,

        /// <summary>
        /// Retrieve the index of a system image list icon. If successful, the index is copied to the iIcon member of psfi. The return
        /// value is a handle to the system image list. Only those images whose indices are successfully copied to iIcon are valid.
        /// Attempting to access other images in the system image list will result in undefined behavior.
        /// </summary>
        SHGFI_SYSICONINDEX = 0x000004000,

        /// <summary>
        /// Modify SHGFI_ICON, causing the function to add the link overlay to the file's icon. The SHGFI_ICON flag must also be set.
        /// </summary>
        SHGFI_LINKOVERLAY = 0x000008000,

        /// <summary>
        /// Modify SHGFI_ICON, causing the function to blend the file's icon with the system highlight color. The SHGFI_ICON flag must
        /// also be set.
        /// </summary>
        SHGFI_SELECTED = 0x000010000,

        /// <summary>
        /// Modify SHGFI_ATTRIBUTES to indicate that the dwAttributes member of the SHFILEINFO structure at psfi contains the specific
        /// attributes that are desired. These attributes are passed to IShellFolder::GetAttributesOf. If this flag is not specified,
        /// 0xFFFFFFFF is passed to IShellFolder::GetAttributesOf, requesting all attributes. This flag cannot be specified with the
        /// SHGFI_ICON flag.
        /// </summary>
        SHGFI_ATTR_SPECIFIED = 0x000020000,

        /// <summary>
        /// Modify SHGFI_ICON, causing the function to retrieve the file's large icon. The SHGFI_ICON flag must also be set.
        /// </summary>
        SHGFI_LARGEICON = 0x000000000,

        /// <summary>
        /// Modify SHGFI_ICON, causing the function to retrieve the file's small icon. Also used to modify SHGFI_SYSICONINDEX, causing
        /// the function to return the handle to the system image list that contains small icon images. The SHGFI_ICON and/or
        /// SHGFI_SYSICONINDEX flag must also be set.
        /// </summary>
        SHGFI_SMALLICON = 0x000000001,

        /// <summary>
        /// Modify SHGFI_ICON, causing the function to retrieve the file's open icon. Also used to modify SHGFI_SYSICONINDEX, causing the
        /// function to return the handle to the system image list that contains the file's small open icon. A container object displays
        /// an open icon to indicate that the container is open. The SHGFI_ICON and/or SHGFI_SYSICONINDEX flag must also be set.
        /// </summary>
        SHGFI_OPENICON = 0x000000002,

        /// <summary>
        /// Modify SHGFI_ICON, causing the function to retrieve a Shell-sized icon. If this flag is not specified the function sizes the
        /// icon according to the system metric values. The SHGFI_ICON flag must also be set.
        /// </summary>
        SHGFI_SHELLICONSIZE = 0x000000004,

        /// <summary>Indicate that pszPath is the address of an ITEMIDLIST structure rather than a path name.</summary>
        SHGFI_PIDL = 0x000000008,

        /// <summary>
        /// Indicates that the function should not attempt to access the file specified by pszPath. Rather, it should act as if the file
        /// specified by pszPath exists with the file attributes passed in dwFileAttributes. This flag cannot be combined with the
        /// SHGFI_ATTRIBUTES, SHGFI_EXETYPE, or SHGFI_PIDL flags.
        /// </summary>
        SHGFI_USEFILEATTRIBUTES = 0x000000010,

        /// <summary>Apply the appropriate overlays to the file's icon. The SHGFI_ICON flag must also be set.</summary>
        SHGFI_ADDOVERLAYS = 0x000000020,

        /// <summary>
        /// Return the index of the overlay icon. The value of the overlay index is returned in the upper eight bits of the iIcon member
        /// of the structure specified by psfi. This flag requires that the SHGFI_ICON be set as well.
        /// </summary>
        SHGFI_OVERLAYINDEX = 0x000000040
    }

    /// <summary>
    /// Enum representing the state of user notifications.
    /// </summary>
    public enum QUERY_USER_NOTIFICATION_STATE
    {
        /// <summary>
        /// A screen saver is displayed, the machine is locked, or a nonactive Fast User Switching session is in progress.
        /// </summary>
        QUNS_NOT_PRESENT = 1,

        /// <summary>
        /// A full-screen application is running or Presentation Settings are applied. Presentation Settings allow a user to put their machine into a state fit for an uninterrupted presentation, such as a set of PowerPoint slides, with a single click.
        /// </summary>
        QUNS_BUSY = 2,

        /// <summary>
        /// A full-screen (exclusive mode) Direct3D application is running.
        /// </summary>
        QUNS_RUNNING_D3D_FULL_SCREEN = 3,

        /// <summary>
        /// The user has activated Windows presentation settings to block notifications and pop-up messages.
        /// </summary>
        QUNS_PRESENTATION_MODE = 4,

        /// <summary>
        /// None of the other states are found, notifications can be freely sent.
        /// </summary>
        QUNS_ACCEPTS_NOTIFICATIONS = 5,

        /// <summary>
        /// The current user is in "quiet time", which is the first hour after a new user logs into his or her account for the first time. During this time, most notifications should not be sent or shown. This lets a user become accustomed to a new computer system without those distractions. Quiet time also occurs for each user after an operating system upgrade or clean installation.
        /// </summary>
        QUNS_QUIET_TIME = 6,

        /// <summary>
        /// Introduced in Windows 8. A Windows Store app is running.
        /// </summary>
        QUNS_APP = 7
    }

    #endregion

    #region shcore.dll

    /// <summary>
    /// <para>
    /// Identifies dots per inch (dpi) awareness values. DPI awareness indicates how much scaling work an application performs for DPI
    /// versus how much is done by the system.
    /// </para>
    /// <para>
    /// Users have the ability to set the DPI scale factor on their displays independent of each other. Some legacy applications are not
    /// able to adjust their scaling for multiple DPI settings. In order for users to use these applications without content appearing
    /// too large or small on displays, Windows can apply DPI virtualization to an application, causing it to be automatically be scaled
    /// by the system to match the DPI of the current display. The <c>PROCESS_DPI_AWARENESS</c> value indicates what level of scaling
    /// your application handles on its own and how much is provided by Windows. Keep in mind that applications scaled by the system may
    /// appear blurry and will read virtualized data about the monitor to maintain compatibility.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para><c>Important</c>
    /// <para></para>
    /// Previous versions of Windows required you to set the DPI awareness for the entire application. Now the DPI awareness is tied to
    /// individual threads, processes, or windows. This means that the DPI awareness can change while the app is running and that
    /// multiple windows can have their own independent DPI awareness values. See DPI_AWARENESS for more information about how DPI
    /// awareness currently works. The recommendations below about setting the DPI awareness in the application manifest are still
    /// supported, but the current recommendation is to use the <c>DPI_AWARENESS_CONTEXT</c>.
    /// </para>
    /// <para>
    /// The DPI awareness for an application should be set through the application manifest so that it is determined before any actions
    /// are taken which depend on the DPI of the system. Alternatively, you can set the DPI awareness using SetProcessDpiAwareness, but
    /// if you do so, you need to make sure to set it before taking any actions dependent on the system DPI. Once you set the DPI
    /// awareness for a process, it cannot be changed.
    /// </para>
    /// <para><c>Tip</c>
    /// <para></para>
    /// If your app is <c>PROCESS_DPI_UNAWARE</c>, there is no need to set any value in the application manifest.
    /// <c>PROCESS_DPI_UNAWARE</c> is the default value for apps unless another value is specified.
    /// </para>
    /// <para>
    /// <c>PROCESS_DPI_UNAWARE</c> and <c>PROCESS_SYSTEM_DPI_AWARE</c> apps do not need to respond to WM_DPICHANGED and are not expected
    /// to handle changes in DPI. The system will automatically scale these types of apps up or down as necessary when the DPI changes.
    /// <c>PROCESS_PER_MONITOR_DPI_AWARE</c> apps are responsible for recognizing and responding to changes in DPI, signaled by
    /// <c>WM_DPICHANGED</c>. These will not be scaled by the system. If an app of this type does not resize the window and its content,
    /// it will appear to grow or shrink by the relative DPI changes as the window is moved from one display to the another with a
    /// different DPI setting.
    /// </para>
    /// <para><c>Tip</c>
    /// <para></para>
    /// In previous versions of Windows, there was no setting for <c>PROCESS_PER_MONITOR_DPI_AWARE</c>. Apps were either DPI unaware or
    /// DPI aware. Legacy applications that were classified as DPI aware before Windows 8.1 are considered to have a
    /// <c>PROCESS_DPI_AWARENESS</c> setting of <c>PROCESS_SYSTEM_DPI_AWARE</c> in current versions of Windows.
    /// </para>
    /// <para>
    /// To understand the importance and impact of the different DPI awareness values, consider a user who has three displays: A, B, and
    /// C. Display A is set to 100% scaling factor (96 DPI), display B is set to 200% scaling factor (192 DPI), and display C is set to
    /// 300% scaling factor (288 DPI). The system DPI is set to 200%.
    /// </para>
    /// <para>
    /// An application that is <c>PROCESS_DPI_UNAWARE</c> will always use a scaling factor of 100% (96 DPI). In this scenario, a
    /// <c>PROCESS_DPI_UNAWARE</c> window is created with a size of 500 by 500. On display A, it will render natively with no scaling.
    /// On displays B and C, it will be scaled up by the system automatically by a factor of 2 and 3 respectively. This is because a
    /// <c>PROCESS_DPI_UNAWARE</c> always assumes a DPI of 96, and the system accounts for that. If the app queries for window size, it
    /// will always get a value of 500 by 500 regardless of what display it is in. If this app were to ask for the DPI of any of the
    /// three monitors, it will receive 96.
    /// </para>
    /// <para>
    /// Now consider an application that is <c>PROCESS_SYSTEM_DPI_AWARE</c>. Remember that in the sample, the system DPI is 200% or 192
    /// DPI. This means that any windows created by this app will render natively on display B. It the window moves to display A, it
    /// will automatically be scaled down by a factor of 2. This is because a <c>PROCESS_SYSTEM_DPI_AWARE</c> app in this scenario
    /// assumes that the DPI will always be 192. It queries for the DPI on startup, and then never changes it. The system accommodates
    /// this by automatically scaling down when moving to display A. Likewise, if the window moves to display C, the system will
    /// automatically scale up by a factor of 1.5. If the app queries for window size, it will always get the same value, similar to
    /// <c>PROCESS_DPI_UNAWARE</c>. If it asks for the DPI of any of the three monitors, it will receive 192.
    /// </para>
    /// <para>
    /// Unlike the other awareness values, <c>PROCESS_PER_MONITOR_DPI_AWARE</c> should adapt to the display that it is on. This means
    /// that it is always rendered natively and is never scaled by the system. The responsibility is on the app to adjust the scale
    /// factor when receiving the WM_DPICHANGED message. Part of this message includes a suggested rect for the window. This suggestion
    /// is the current window scaled from the old DPI value to the new DPI value. For example, a window that is 500 by 500 on display A
    /// and moved to display B will receive a suggested window rect that is 1000 by 1000. If that same window is moved to display C, the
    /// suggested window rect attached to <c>WM_DPICHANGED</c> will be 1500 by 1500. Furthermore, when this app queries for the window
    /// size, it will always get the actual native value. Likewise, if it asks for the DPI of any of the three monitors, it will receive
    /// 96, 192, and 288 respectively.
    /// </para>
    /// <para>
    /// Because of DPI virtualization, if one application queries another with a different awareness level for DPI-dependent
    /// information, the system will automatically scale values to match the awareness level of the caller. One example of this is if
    /// you call GetWindowRect and pass in a window created by another application. Using the situation described above, assume that a
    /// <c>PROCESS_DPI_UNAWARE</c> app created a 500 by 500 window on display C. If you query for the window rect from a different
    /// application, the size of the rect will vary based upon the DPI awareness of your app.
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>PROCESS_DPI_UNAWARE</term>
    /// <term>
    /// You will get a 500 by 500 rect because the system will assume a DPI of 96 and automatically scale the actual rect down by a
    /// factor of 3.
    /// </term>
    /// </listheader>
    /// <item>
    /// <term>PROCESS_SYSTEM_DPI_AWARE</term>
    /// <term>
    /// You will get a 1000 by 1000 rect because the system will assume a DPI of 192 and automatically scale the actual rect down by a
    /// factor of 3/2.
    /// </term>
    /// </item>
    /// <item>
    /// <term>PROCESS_PER_MONITOR_DPI_AWARE</term>
    /// <term>
    /// You will get a 1500 by 1500 rect because the system will use the actual DPI of the display and not do any scaling behind the scenes.
    /// </term>
    /// </item>
    /// </list>
    /// <para>Examples</para>
    /// <para>This snippet demonstrates how to set a value of <c>PROCESS_SYSTEM_DPI_AWARE</c> in your application manifest.</para>
    /// <para>
    /// <code>&lt;dpiAware&gt;true&lt;/dpiAware&gt;</code>
    /// </para>
    /// <para>This snippet demonstrates how to set a value of <c>PROCESS_PER_MONITOR_DPI_AWARE</c> in your application manifest.</para>
    /// <para>
    /// <code>&lt;dpiAware&gt;true/PM&lt;/dpiAware&gt;</code>
    /// </para>
    /// </remarks>
    public enum PROCESS_DPI_AWARENESS
    {
        /// <summary>
        /// DPI unaware. This app does not scale for DPI changes and is always assumed to have a scale factor of 100% (96 DPI). It will
        /// be automatically scaled by the system on any other DPI setting.
        /// </summary>
        PROCESS_DPI_UNAWARE = 0,

        /// <summary>
        /// System DPI aware. This app does not scale for DPI changes. It will query for the DPI once and use that value for the
        /// lifetime of the app. If the DPI changes, the app will not adjust to the new DPI value. It will be automatically scaled up or
        /// down by the system when the DPI changes from the system value.
        /// </summary>
        PROCESS_SYSTEM_DPI_AWARE,

        /// <summary>
        /// Per monitor DPI aware. This app checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes.
        /// These applications are not automatically scaled by the system.
        /// </summary>
        PROCESS_PER_MONITOR_DPI_AWARE,
    }

    #endregion

    #region gdi32.dll

    /// <summary>
    /// Enum representing device capabilities.
    /// </summary>
    public enum DeviceCap
    {
        /// <summary>
        /// The horizontal width, in pixels, of the screen or display device.
        /// </summary>
        HORZRES = 8,

        /// <summary>
        /// The vertical height, in pixels, of the screen or display device.
        /// </summary>
        VERTRES = 10,

        /// <summary>
        /// The vertical height, in pixels, of the entire desktop for a display device. This includes all monitors in the desktop.
        /// </summary>
        DESKTOPVERTRES = 117,

        /// <summary>
        /// The horizontal width, in pixels, of the entire desktop for a display device. This includes all monitors in the desktop.
        /// </summary>
        DESKTOPHORZRES = 118
    }

    #endregion

    #region shared_pinvoke

    public enum NTSTATUS : uint
    {
        /// <summary>
        /// The operation completed successfully. 
        /// </summary>
        STATUS_SUCCESS = 0x00000000
    }

    #endregion
}
