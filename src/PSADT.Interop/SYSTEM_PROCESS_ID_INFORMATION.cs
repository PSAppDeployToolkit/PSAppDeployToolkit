using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace PSADT.Interop
{
    /// <summary>
    /// Enumeration of process information classes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_PROCESS_ID_INFORMATION
    {
        /// <summary>
        /// The number of processes in the system.
        /// </summary>
        internal nint ProcessId;

        /// <summary>
        /// The number of threads in the system.
        /// </summary>
        internal UNICODE_STRING ImageName;
    }
}
