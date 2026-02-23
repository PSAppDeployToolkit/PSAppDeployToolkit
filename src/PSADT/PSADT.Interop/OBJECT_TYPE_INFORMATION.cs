using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Windows.Wdk.Foundation;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace PSADT.Interop
{
    /// <summary>
    /// System information class for querying system handle information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct OBJECT_TYPE_INFORMATION
    {
        /// <summary>
        /// The name of the type/
        /// </summary>
        internal readonly UNICODE_STRING TypeName;

        /// <summary>
        /// The type's object count.
        /// </summary>
        internal readonly uint TotalNumberOfObjects;

        /// <summary>
        /// The type's handle count.
        /// </summary>
        internal readonly uint TotalNumberOfHandles;

        /// <summary>
        /// The type's paged pool usage.
        /// </summary>
        internal readonly uint TotalPagedPoolUsage;

        /// <summary>
        /// The type's non-paged pool usage.
        /// </summary>
        internal readonly uint TotalNonPagedPoolUsage;

        /// <summary>
        /// The type's name pool usage.
        /// </summary>
        internal readonly uint TotalNamePoolUsage;

        /// <summary>
        /// The type's handle table usage.
        /// </summary>
        internal readonly uint TotalHandleTableUsage;

        /// <summary>
        /// The type's high-water mark for object count.
        /// </summary>
        internal readonly uint HighWaterNumberOfObjects;

        /// <summary>
        /// The type's high-water mark for handle count.
        /// </summary>
        internal readonly uint HighWaterNumberOfHandles;

        /// <summary>
        /// The type's high-water mark for paged pool usage.
        /// </summary>
        internal readonly uint HighWaterPagedPoolUsage;

        /// <summary>
        /// The type's high-water mark for non-paged pool usage.
        /// </summary>
        internal readonly uint HighWaterNonPagedPoolUsage;

        /// <summary>
        /// The type's high-water mark for name pool usage.
        /// </summary>
        internal readonly uint HighWaterNamePoolUsage;

        /// <summary>
        /// The type's high-water mark for handle table usage.
        /// </summary>
        internal readonly uint HighWaterHandleTableUsage;

        /// <summary>
        /// The type's invalid attributes.
        /// </summary>
        internal readonly OBJECT_ATTRIBUTES InvalidAttributes;

        /// <summary>
        /// The type's generic mapping.
        /// </summary>
        internal readonly GENERIC_MAPPING GenericMapping;

        /// <summary>
        /// The type's valid access mask.
        /// </summary>
        internal readonly FileSystemRights ValidAccessMask;

        /// <summary>
        /// The type's security required flag.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        internal readonly bool SecurityRequired;

        /// <summary>
        /// The type's security descriptor present flag.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        internal readonly bool MaintainHandleCount;

        /// <summary>
        /// The object type's index.
        /// </summary>
        internal readonly byte TypeIndex;

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        internal readonly sbyte ReservedByte;

        /// <summary>
        /// The object type's pool type.
        /// </summary>
        internal readonly POOL_TYPE PoolType;

        /// <summary>
        /// The default paged pool charge for the object type.
        /// </summary>
        internal readonly uint DefaultPagedPoolCharge;

        /// <summary>
        /// The default non-paged pool charge for the object type.
        /// </summary>
        internal readonly uint DefaultNonPagedPoolCharge;
    }
}
