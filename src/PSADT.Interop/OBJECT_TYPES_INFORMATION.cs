using System.Runtime.InteropServices;

namespace PSADT.Interop
{
    /// <summary>
    /// System information class for querying system object types.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct OBJECT_TYPES_INFORMATION
    {
        /// <summary>
        /// The number of object types in the system.
        /// </summary>
        internal readonly uint NumberOfTypes;
    }
}
