using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// The architecture of the executable..
    /// </summary>
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
    /// Specifies the access rights for file and directory objects.
    /// </summary>
    /// <remarks>This enumeration defines various access rights that can be granted to file and directory
    /// objects. Each value represents a specific permission, such as reading, writing, or executing files, as well as
    /// managing directories and their contents. These rights are used to control access to file system objects and are
    /// typically combined to form a comprehensive access control policy.</remarks>
    [Flags]
    public enum FILE_ACCESS_RIGHTS : uint
    {
        /// <summary>
        /// For a file object, the right to read the corresponding file data. For a directory object, the right to read the corresponding directory data.
        /// </summary>
        FILE_READ_DATA = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_READ_DATA,

        /// <summary>
        /// For a directory, the right to list the contents of the directory.
        /// </summary>
        FILE_LIST_DIRECTORY = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_LIST_DIRECTORY,

        /// <summary>
        /// For a file object, the right to write data to the file. For a directory object, the right to create a file in the directory (FILE_ADD_FILE).
        /// </summary>
        FILE_WRITE_DATA = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_WRITE_DATA,

        /// <summary>
        /// For a directory, the right to create a file in the directory.
        /// </summary>
        FILE_ADD_FILE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_ADD_FILE,

        /// <summary>
        /// For a file object, the right to append data to the file. (For local files, write operations will not overwrite existing data if this flag is specified without FILE_WRITE_DATA.) For a directory object, the right to create a subdirectory (FILE_ADD_SUBDIRECTORY).
        /// </summary>
        FILE_APPEND_DATA = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_APPEND_DATA,

        /// <summary>
        /// For a directory, the right to create a subdirectory.
        /// </summary>
        FILE_ADD_SUBDIRECTORY = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_ADD_SUBDIRECTORY,

        /// <summary>
        /// For a named pipe, the right to create a pipe.
        /// </summary>
        FILE_CREATE_PIPE_INSTANCE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_CREATE_PIPE_INSTANCE,

        /// <summary>
        /// The right to read extended file attributes.
        /// </summary>
        FILE_READ_EA = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_READ_EA,

        /// <summary>
        /// The right to write extended file attributes.
        /// </summary>
        FILE_WRITE_EA = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_WRITE_EA,

        /// <summary>
        /// For a native code file, the right to execute the file. This access right given to scripts may cause the script to be executable, depending on the script interpreter.
        /// </summary>
        FILE_EXECUTE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_EXECUTE,

        /// <summary>
        /// For a directory, the right to traverse the directory. By default, users are assigned the BYPASS_TRAVERSE_CHECKING privilege, which ignores the FILE_TRAVERSE access right. See the remarks in File Security and Access Rights for more information.
        /// </summary>
        FILE_TRAVERSE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_TRAVERSE,

        /// <summary>
        /// For a directory, the right to delete a directory and all the files it contains, including read-only files.
        /// </summary>
        FILE_DELETE_CHILD = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_DELETE_CHILD,

        /// <summary>
        /// The right to read file attributes.
        /// </summary>
        FILE_READ_ATTRIBUTES = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES,

        /// <summary>
        /// The right to write file attributes.
        /// </summary>
        FILE_WRITE_ATTRIBUTES = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES,

        /// <summary>
        /// The right to delete the object.
        /// </summary>
        DELETE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.DELETE,

        /// <summary>
        /// The right to read the information in the object's security descriptor, not including the information in the system access control list (SACL).
        /// </summary>
        READ_CONTROL = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.READ_CONTROL,

        /// <summary>
        /// The right to modify the discretionary access control list (DACL) in the object's security descriptor.
        /// </summary>
        WRITE_DAC = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.WRITE_DAC,

        /// <summary>
        /// The right to change the owner in the object's security descriptor.
        /// </summary>
        WRITE_OWNER = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.WRITE_OWNER,

        /// <summary>
        /// The right to use the object for synchronization. This enables a thread to wait until the object is in the signaled state. Some object types do not support this access right.
        /// </summary>
        SYNCHRONIZE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.SYNCHRONIZE,

        /// <summary>
        /// Combines DELETE, READ_CONTROL, WRITE_DAC, and WRITE_OWNER access.
        /// </summary>
        STANDARD_RIGHTS_REQUIRED = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_REQUIRED,

        /// <summary>
        /// Includes READ_CONTROL, which is the right to read the information in the file or directory object's security descriptor. This does not include the information in the SACL.
        /// </summary>
        STANDARD_RIGHTS_READ = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_READ,

        /// <summary>
        /// Same as STANDARD_RIGHTS_READ.
        /// </summary>
        STANDARD_RIGHTS_WRITE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_WRITE,

        /// <summary>
        /// Same as STANDARD_RIGHTS_READ.
        /// </summary>
        STANDARD_RIGHTS_EXECUTE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_EXECUTE,

        /// <summary>
        /// Combines DELETE, READ_CONTROL, WRITE_DAC, WRITE_OWNER, and SYNCHRONIZE access.
        /// </summary>
        STANDARD_RIGHTS_ALL = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_ALL,

        /// <summary>
        /// All specific rights. This value MUST NOT be passed over the wire. It SHOULD only be used locally, as a mask to determine the protocol-specific subset of access values.
        /// </summary>
        SPECIFIC_RIGHTS_ALL = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.SPECIFIC_RIGHTS_ALL,

        /// <summary>
        /// All possible access rights for a file.
        /// </summary>
        FILE_ALL_ACCESS = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_ALL_ACCESS,

        /// <summary>
        /// Combines FILE_READ_ATTRIBUTES, FILE_READ_DATA, FILE_READ_EA, STANDARD_RIGHTS_READ, and SYNCHRONIZE.
        /// </summary>
        FILE_GENERIC_READ = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_GENERIC_READ,

        /// <summary>
        /// Combines FILE_APPEND_DATA, FILE_WRITE_ATTRIBUTES, FILE_WRITE_DATA, FILE_WRITE_EA, STANDARD_RIGHTS_WRITE, and SYNCHRONIZE.
        /// </summary>
        FILE_GENERIC_WRITE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE,

        /// <summary>
        /// Combines FILE_EXECUTE, FILE_READ_ATTRIBUTES, STANDARD_RIGHTS_EXECUTE, and SYNCHRONIZE.
        /// </summary>
        FILE_GENERIC_EXECUTE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.FILE_GENERIC_EXECUTE,
    }
}
