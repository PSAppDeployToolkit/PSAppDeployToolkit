using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace PSADT.Interop
{
    /// <summary>
    /// System information class for querying system handles.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
    {
        /// <summary>
        /// The kernel object's address.
        /// </summary>
        public readonly nint ObjectPtr;

        /// <summary>
        /// The owning process's identifier.
        /// </summary>
        public readonly nuint UniqueProcessId;

        /// <summary>
        /// The handle's numerical identifier.
        /// </summary>
        public readonly nuint HandleValue;

        /// <summary>
        /// The type of access granted to the handle.
        /// </summary>
        public readonly FileSystemRights GrantedAccess;

        /// <summary>
        /// The number of references to the object.
        /// </summary>
        public readonly ushort CreatorBackTraceIndex;

        /// <summary>
        /// The type of the object.
        /// </summary>
        public readonly ushort ObjectTypeIndex;

        /// <summary>
        /// The handle attributes.
        /// </summary>
        public readonly OBJECT_ATTRIBUTES HandleAttributes;

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        public readonly uint Reserved;
    }
}
