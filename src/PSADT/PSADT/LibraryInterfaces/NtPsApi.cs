using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies extended flags for process creation, allowing customization of process behavior.
    /// </summary>
    /// <remarks>Sourced from https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2781-L2783</remarks>
    [Flags]
    internal enum EXTENDED_PROCESS_CREATION_FLAG : uint
    {
        /// <summary>
        /// Indicates that process elevation has been handled during extended process creation.
        /// </summary>
        /// <remarks>This flag is used to specify that elevation requirements have already been addressed
        /// and that no further elevation prompts should occur during the process creation. It is typically used in
        /// scenarios where the caller has explicitly managed elevation before invoking the process creation
        /// API.</remarks>
        EXTENDED_PROCESS_CREATION_FLAG_ELEVATION_HANDLED = 0x1,

        /// <summary>
        /// Specifies that the process should be created with forced User Account Control (UAC) virtualization enabled.
        /// </summary>
        /// <remarks>This flag is typically used when creating processes that require UAC virtualization,
        /// ensuring that file and registry operations are redirected for compatibility with legacy applications. Use
        /// this flag only when necessary, as it may affect how the process interacts with system resources.</remarks>
        EXTENDED_PROCESS_CREATION_FLAG_FORCELUA = 0x2,

        /// <summary>
        /// Specifies that the process should be created with a force breakaway flag, allowing it to break away from any
        /// job object it would otherwise inherit.
        /// </summary>
        /// <remarks>Use this flag when creating a process that must not be associated with the job object
        /// of its parent, even if the parent is running within a job. This is typically relevant in advanced process
        /// management scenarios on Windows platforms.</remarks>
        EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY = 0x4,
    }
}
