using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// The architecture of the executable..
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum IMAGE_FILE_MACHINE : ushort
    {
        /// <summary>
        /// Alpha
        /// </summary>
        IMAGE_FILE_MACHINE_AXP64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AXP64,

        /// <summary>
        /// x86
        /// </summary>
        IMAGE_FILE_MACHINE_I386 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,

        /// <summary>
        /// Itanium
        /// </summary>
        IMAGE_FILE_MACHINE_IA64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_IA64,

        /// <summary>
        /// AMD64
        /// </summary>
        IMAGE_FILE_MACHINE_AMD64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64,

        /// <summary>
        /// Unknown
        /// </summary>
        IMAGE_FILE_MACHINE_UNKNOWN = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN,

        /// <summary>
        /// Target host
        /// </summary>
        IMAGE_FILE_MACHINE_TARGET_HOST = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TARGET_HOST,

        /// <summary>
        /// R3000
        /// </summary>
        IMAGE_FILE_MACHINE_R3000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R3000,

        /// <summary>
        /// R4000
        /// </summary>
        IMAGE_FILE_MACHINE_R4000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R4000,

        /// <summary>
        /// R10000
        /// </summary>
        IMAGE_FILE_MACHINE_R10000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R10000,

        /// <summary>
        /// Windows CE MIPS
        /// </summary>
        IMAGE_FILE_MACHINE_WCEMIPSV2 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_WCEMIPSV2,

        /// <summary>
        /// Alpha
        /// </summary>
        IMAGE_FILE_MACHINE_ALPHA = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA,

        /// <summary>
        /// SH3
        /// </summary>
        IMAGE_FILE_MACHINE_SH3 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3,

        /// <summary>
        /// SH3DSP
        /// </summary>
        IMAGE_FILE_MACHINE_SH3DSP = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3DSP,

        /// <summary>
        /// SH3E
        /// </summary>
        IMAGE_FILE_MACHINE_SH3E = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3E,

        /// <summary>
        /// SH4
        /// </summary>
        IMAGE_FILE_MACHINE_SH4 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH4,

        /// <summary>
        /// SH5
        /// </summary>
        IMAGE_FILE_MACHINE_SH5 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH5,

        /// <summary>
        /// ARM
        /// </summary>
        IMAGE_FILE_MACHINE_ARM = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM,

        /// <summary>
        /// Thumb
        /// </summary>
        IMAGE_FILE_MACHINE_THUMB = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_THUMB,

        /// <summary>
        /// ARMNT
        /// </summary>
        IMAGE_FILE_MACHINE_ARMNT = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARMNT,

        /// <summary>
        /// AM33
        /// </summary>
        IMAGE_FILE_MACHINE_AM33 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AM33,

        /// <summary>
        /// PowerPC
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPC = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPC,

        /// <summary>
        /// PowerPCFP
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPCFP = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPCFP,

        /// <summary>
        /// MIPS16
        /// </summary>
        IMAGE_FILE_MACHINE_MIPS16 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPS16,

        /// <summary>
        /// Alpha64
        /// </summary>
        IMAGE_FILE_MACHINE_ALPHA64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA64,

        /// <summary>
        /// MIPSFPU
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU,

        /// <summary>
        /// MIPSFPU16
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU16 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU16,

        /// <summary>
        /// Tricore
        /// </summary>
        IMAGE_FILE_MACHINE_TRICORE = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TRICORE,

        /// <summary>
        /// CEF
        /// </summary>
        IMAGE_FILE_MACHINE_CEF = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEF,

        /// <summary>
        /// EBC
        /// </summary>
        IMAGE_FILE_MACHINE_EBC = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_EBC,

        /// <summary>
        /// M32R
        /// </summary>
        IMAGE_FILE_MACHINE_M32R = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_M32R,

        /// <summary>
        /// ARM64
        /// </summary>
        IMAGE_FILE_MACHINE_ARM64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64,

        /// <summary>
        /// CEE
        /// </summary>
        IMAGE_FILE_MACHINE_CEE = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEE,
    }

    /// <summary>
    /// The subsystem of the executable.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum IMAGE_SUBSYSTEM : ushort
    {
        /// <summary>
        /// Unknown
        /// </summary>
        IMAGE_SUBSYSTEM_UNKNOWN = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_UNKNOWN,

        /// <summary>
        /// Native
        /// </summary>
        IMAGE_SUBSYSTEM_NATIVE = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_NATIVE,

        /// <summary>
        /// Windows GUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_GUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI,

        /// <summary>
        /// Windows CUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI,

        /// <summary>
        /// OS/2 CUI
        /// </summary>
        IMAGE_SUBSYSTEM_OS2_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_OS2_CUI,

        /// <summary>
        /// POSIX CUI
        /// </summary>
        IMAGE_SUBSYSTEM_POSIX_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_POSIX_CUI,

        /// <summary>
        /// Windows CE GUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_CE_GUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CE_GUI,

        /// <summary>
        /// EFI Application
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_APPLICATION = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_APPLICATION,

        /// <summary>
        /// EFI Boot Service Driver
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER,

        /// <summary>
        /// EFI Runtime Driver
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER,

        /// <summary>
        /// EFI ROM
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_ROM = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_ROM,

        /// <summary>
        /// Xbox
        /// </summary>
        IMAGE_SUBSYSTEM_XBOX = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_XBOX,

        /// <summary>
        /// Windows boot application
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION,
    }

    /// <summary>
    /// Represents the various messages that can be sent by the system to a job object.
    /// </summary>
    /// <remarks>These messages are used to notify about specific events or conditions related to the
    /// processes associated with a job object. Each message corresponds to a particular event, such as a process
    /// exceeding a time limit or the job reaching its memory limit. These notifications can be used to monitor and
    /// manage the behavior of processes within a job.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These values are precisely as they're defined in the Win32 API.")]
    internal enum JOB_OBJECT_MSG : uint
    {
        /// <summary>
        /// Indicates that a process associated with the job exited with an exit code that indicates an abnormal exit (see the list following this table).
        /// </summary>
        JOB_OBJECT_MSG_ABNORMAL_EXIT_PROCESS = Windows.Win32.PInvoke.JOB_OBJECT_MSG_ABNORMAL_EXIT_PROCESS,

        /// <summary>
        /// Indicates that the active process limit has been exceeded.
        /// </summary>
        JOB_OBJECT_MSG_ACTIVE_PROCESS_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_LIMIT,

        /// <summary>
        /// Indicates that the active process count has been decremented to 0. For example, if the job currently has two active processes, the system sends this message after they both terminate.
        /// </summary>
        JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO = Windows.Win32.PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO,

        /// <summary>
        /// Indicates that the JOB_OBJECT_POST_AT_END_OF_JOB option is in effect and the end-of-job time limit has been reached. Upon posting this message, the time limit is canceled and the job's processes can continue to run.
        /// </summary>
        JOB_OBJECT_MSG_END_OF_JOB_TIME = Windows.Win32.PInvoke.JOB_OBJECT_MSG_END_OF_JOB_TIME,

        /// <summary>
        /// Indicates that a process has exceeded a per-process time limit. The system sends this message after the process termination has been requested.
        /// </summary>
        JOB_OBJECT_MSG_END_OF_PROCESS_TIME = Windows.Win32.PInvoke.JOB_OBJECT_MSG_END_OF_PROCESS_TIME,

        /// <summary>
        /// Indicates that a process associated with the job has exited.
        /// </summary>
        JOB_OBJECT_MSG_EXIT_PROCESS = Windows.Win32.PInvoke.JOB_OBJECT_MSG_EXIT_PROCESS,

        /// <summary>
        /// Indicates that a process associated with the job caused the job to exceed the job-wide memory limit (if one is in effect).
        /// </summary>
        JOB_OBJECT_MSG_JOB_MEMORY_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_JOB_MEMORY_LIMIT,

        /// <summary>
        /// Indicates that a process has been added to the job. Processes added to a job at the time a completion port is associated are also reported.
        /// </summary>
        JOB_OBJECT_MSG_NEW_PROCESS = Windows.Win32.PInvoke.JOB_OBJECT_MSG_NEW_PROCESS,

        /// <summary>
        /// Indicates that a process associated with a job that has registered for resource limit notifications has exceeded one or more limits. Use the QueryInformationJobObject function with JobObjectLimitViolationInformation to determine which limit was exceeded.
        /// </summary>
        JOB_OBJECT_MSG_NOTIFICATION_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_NOTIFICATION_LIMIT,

        /// <summary>
        /// Indicates that a process associated with the job has exceeded its memory limit (if one is in effect).
        /// </summary>
        JOB_OBJECT_MSG_PROCESS_MEMORY_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_PROCESS_MEMORY_LIMIT,
    }

    /// <summary>
    /// Values for determining a product's type.
    /// </summary>
    internal enum PRODUCT_TYPE : byte
    {
        /// <summary>
        /// The operating system is Windows 10, Windows 8, Windows 7,...
        /// </summary>
        VER_NT_WORKSTATION = (byte)Windows.Win32.PInvoke.VER_NT_WORKSTATION,

        /// <summary>
        /// The system is a domain controller and the operating system is Windows Server.
        /// </summary>
        VER_NT_DOMAIN_CONTROLLER = (byte)Windows.Win32.PInvoke.VER_NT_DOMAIN_CONTROLLER,

        /// <summary>
        /// The operating system is Windows Server. Note that a server that is also a domain controller
        /// is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
        /// </summary>
        VER_NT_SERVER = (byte)Windows.Win32.PInvoke.VER_NT_SERVER,
    }

    /// <summary>
    /// Flags for determining a product's paricular SKU features.
    /// </summary>
    [Flags]
    internal enum SUITE_MASK : ushort
    {
        /// <summary>
        /// Microsoft BackOffice components are installed. 
        /// </summary>
        VER_SUITE_BACKOFFICE = (ushort)Windows.Win32.PInvoke.VER_SUITE_BACKOFFICE,

        /// <summary>
        /// Windows Server 2003, Web Edition is installed
        /// </summary>
        VER_SUITE_BLADE = (ushort)Windows.Win32.PInvoke.VER_SUITE_BLADE,

        /// <summary>
        /// Windows Server 2003, Compute Cluster Edition is installed.
        /// </summary>
        VER_SUITE_COMPUTE_SERVER = (ushort)Windows.Win32.PInvoke.VER_SUITE_COMPUTE_SERVER,

        /// <summary>
        /// Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, or Windows 2000 Datacenter Server is installed. 
        /// </summary>
        VER_SUITE_DATACENTER = (ushort)Windows.Win32.PInvoke.VER_SUITE_DATACENTER,

        /// <summary>
        /// Windows Server 2008 Enterprise, Windows Server 2003, Enterprise Edition, or Windows 2000 Advanced Server is installed.
        /// Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_ENTERPRISE = (ushort)Windows.Win32.PInvoke.VER_SUITE_ENTERPRISE,

        /// <summary>
        /// Windows XP Embedded is installed. 
        /// </summary>
        VER_SUITE_EMBEDDEDNT = (ushort)Windows.Win32.PInvoke.VER_SUITE_EMBEDDEDNT,

        /// <summary>
        /// Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition is installed. 
        /// </summary>
        VER_SUITE_PERSONAL = (ushort)Windows.Win32.PInvoke.VER_SUITE_PERSONAL,

        /// <summary>
        /// Remote Desktop is supported, but only one interactive session is supported. This value is set unless the system is running in application server mode. 
        /// </summary>
        VER_SUITE_SINGLEUSERTS = (ushort)Windows.Win32.PInvoke.VER_SUITE_SINGLEUSERTS,

        /// <summary>
        /// Microsoft Small Business Server was once installed on the system, but may have been upgraded to another version of Windows.
        /// Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_SMALLBUSINESS = (ushort)Windows.Win32.PInvoke.VER_SUITE_SMALLBUSINESS,

        /// <summary>
        /// Microsoft Small Business Server is installed with the restrictive client license in force. Refer to the Remarks section for more information about this bit flag. 
        /// </summary>
        VER_SUITE_SMALLBUSINESS_RESTRICTED = (ushort)Windows.Win32.PInvoke.VER_SUITE_SMALLBUSINESS_RESTRICTED,

        /// <summary>
        /// Windows Storage Server 2003 R2 or Windows Storage Server 2003 is installed. 
        /// </summary>
        VER_SUITE_STORAGE_SERVER = (ushort)Windows.Win32.PInvoke.VER_SUITE_STORAGE_SERVER,

        /// <summary>
        /// Terminal Services is installed. This value is always set.
        /// If VER_SUITE_TERMINAL is set but VER_SUITE_SINGLEUSERTS is not set, the system is running in application server mode.
        /// </summary>
        VER_SUITE_TERMINAL = (ushort)Windows.Win32.PInvoke.VER_SUITE_TERMINAL,

        /// <summary>
        /// Windows Home Server is installed. 
        /// </summary>
        VER_SUITE_WH_SERVER = (ushort)Windows.Win32.PInvoke.VER_SUITE_WH_SERVER,
    }

    /// <summary>
    /// Enumeration of all available security privileges.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum SE_PRIVILEGE
    {
        /// <summary>
        /// The right to attach a primary token to a process.
        /// </summary>
        SeAssignPrimaryTokenPrivilege,

        /// <summary>
        /// The right to generate security audits.
        /// </summary>
        SeAuditPrivilege,

        /// <summary>
        /// The right to back up files and directories.
        /// </summary>
        SeBackupPrivilege,

        /// <summary>
        /// The right to receive notifications of changes to files or directories.
        /// </summary>
        SeChangeNotifyPrivilege,

        /// <summary>
        /// The right to create global objects.
        /// </summary>
        SeCreateGlobalPrivilege,

        /// <summary>
        /// The right to create a pagefile.
        /// </summary>
        SeCreatePagefilePrivilege,

        /// <summary>
        /// The right to create a permanent object.
        /// </summary>
        SeCreatePermanentPrivilege,

        /// <summary>
        /// The right to create symbolic links.
        /// </summary>
        SeCreateSymbolicLinkPrivilege,

        /// <summary>
        /// The right to create a token object.
        /// </summary>
        SeCreateTokenPrivilege,

        /// <summary>
        /// The right to debug programs.
        /// </summary>
        SeDebugPrivilege,

        /// <summary>
        /// The right to impersonate a client after authentication.
        /// </summary>
        SeDelegateSessionUserImpersonatePrivilege,

        /// <summary>
        /// The right to enable computer and user accounts to be trusted for delegation.
        /// </summary>
        SeEnableDelegationPrivilege,

        /// <summary>
        /// The right to impersonate.
        /// </summary>
        SeImpersonatePrivilege,

        /// <summary>
        /// The right to increase the base priority of a process.
        /// </summary>
        SeIncreaseBasePriorityPrivilege,

        /// <summary>
        /// The right to increase quotas.
        /// </summary>
        SeIncreaseQuotaPrivilege,

        /// <summary>
        /// The right to increase non-paged pool quotas.
        /// </summary>
        SeIncreaseWorkingSetPrivilege,

        /// <summary>
        /// The right to load and unload device drivers.
        /// </summary>
        SeLoadDriverPrivilege,

        /// <summary>
        /// The right to lock pages in memory.
        /// </summary>
        SeLockMemoryPrivilege,

        /// <summary>
        /// The right to create a computer account.
        /// </summary>
        SeMachineAccountPrivilege,

        /// <summary>
        /// The right to manage the files on a volume.
        /// </summary>
        SeManageVolumePrivilege,

        /// <summary>
        /// The right to profile single process.
        /// </summary>
        SeProfileSingleProcessPrivilege,

        /// <summary>
        /// The right to relabel.
        /// </summary>
        SeRelabelPrivilege,

        /// <summary>
        /// The right to shut down the system.
        /// </summary>
        SeRemoteShutdownPrivilege,

        /// <summary>
        /// The right to restore files and directories.
        /// </summary>
        SeRestorePrivilege,

        /// <summary>
        /// The right to manage auditing and security log.
        /// </summary>
        SeSecurityPrivilege,

        /// <summary>
        /// The right to shut down the system.
        /// </summary>
        SeShutdownPrivilege,

        /// <summary>
        /// The right to synchronize directory service data.
        /// </summary>
        SeSyncAgentPrivilege,

        /// <summary>
        /// The right to modify the system environment.
        /// </summary>
        SeSystemEnvironmentPrivilege,

        /// <summary>
        /// The right to profile system performance.
        /// </summary>
        SeSystemProfilePrivilege,

        /// <summary>
        /// The right to change the system time.
        /// </summary>
        SeSystemtimePrivilege,

        /// <summary>
        /// The right to take ownership of files or other objects.
        /// </summary>
        SeTakeOwnershipPrivilege,

        /// <summary>
        /// The right to act as part of the operating system.
        /// </summary>
        SeTcbPrivilege,

        /// <summary>
        /// The right to change the time zone.
        /// </summary>
        SeTimeZonePrivilege,

        /// <summary>
        /// The right to access Credential Manager as a trusted caller.
        /// </summary>
        SeTrustedCredManAccessPrivilege,

        /// <summary>
        /// The right to undock a laptop.
        /// </summary>
        SeUndockPrivilege,

        /// <summary>
        /// The right to generate input for the system.
        /// </summary>
        SeUnsolicitedInputPrivilege
    }
}
