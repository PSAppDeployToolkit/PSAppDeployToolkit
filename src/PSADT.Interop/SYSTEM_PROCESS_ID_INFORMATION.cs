using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace PSADT.Interop
{
    /// <summary>
    /// System information class for querying the image name of a process by its identifier.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_PROCESS_ID_INFORMATION
    {
        /// <summary>
        /// The identifier of the process to query.
        /// </summary>
        internal nint ProcessId;

        /// <summary>
        /// Receives the NT path of the process's image, in a buffer the caller supplies.
        /// </summary>
        internal UNICODE_STRING ImageName;
    }
}
