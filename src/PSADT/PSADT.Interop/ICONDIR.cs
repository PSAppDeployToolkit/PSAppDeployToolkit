using System.Runtime.InteropServices;
using Windows.Win32;

namespace PSADT.Interop
{
    /// <summary>
    /// Represents the directory structure for icon resources, containing metadata and entries for associated icons.
    /// </summary>
    /// <remarks>The ICONDIR structure is used in resource files to describe a collection of icons. It
    /// includes identifiers for reserved and type fields, a count of icon entries, and a pointer to the array of icon
    /// directory entries. An instance is considered valid when the reserved identifier is zero, the type identifier is
    /// either 1 or 2, and the count is greater than zero. This structure is typically used when parsing or constructing
    /// icon resources in unmanaged memory contexts.</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct ICONDIR
    {
        /// <summary>
        /// Gets the reserved identifier used for internal purposes.
        /// </summary>
        internal readonly ushort idReserved;

        /// <summary>
        /// Gets the identifier type associated with the current instance.
        /// </summary>
        internal readonly ushort idType;

        /// <summary>
        /// Gets the number of identifiers associated with the current instance.
        /// </summary>
        internal readonly ushort idCount;

        /// <summary>
        /// Gets the array of icon directory entries that represent the icons associated with the resource.
        /// </summary>
        /// <remarks>This array is populated with instances of ICONDIRENTRY, each representing a specific
        /// icon's metadata. The entries are read-only and should not be modified directly.</remarks>
        internal readonly VariableLengthInlineArray<ICONDIRENTRY> idEntries;

        /// <summary>
        /// Gets a value indicating whether the current instance meets the criteria for validity.
        /// </summary>
        /// <remarks>The instance is considered valid when the reserved identifier is zero, the type
        /// identifier is either 1 or 2, and the count is greater than zero. This property can be used to check if the
        /// object is in a usable state before performing operations.</remarks>
        internal readonly bool IsValid => idReserved == 0 && (idType == 1 || idType == 2) && idCount > 0;
    }
}
