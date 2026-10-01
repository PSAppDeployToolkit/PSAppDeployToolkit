using System.Runtime.InteropServices;

namespace PSADT.Interop
{
    /// <summary>
    /// System information class for querying system handle information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct SYSTEM_HANDLE_INFORMATION_EX
    {
        /// <summary>
        /// The number of handles in the system.
        /// </summary>
        internal readonly nuint NumberOfHandles;

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        internal readonly nuint Reserved;
    }
}
