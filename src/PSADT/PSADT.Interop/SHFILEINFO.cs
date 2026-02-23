using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Interop
{
    /// <summary>
    /// Struct for retrieving information about a file, including its icon and display name.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal readonly struct SHFILEINFO
    {
        /// <summary>
        /// The file system icon handle (must be destroyed with DestroyIcon when no longer needed).
        /// </summary>
        internal readonly HICON hIcon;

        /// <summary>
        /// The index of the icon in the system image list.
        /// </summary>
        internal readonly int iIcon;

        /// <summary>
        /// The file attributes (e.g., read-only, hidden, system).
        /// </summary>
        internal readonly FileAttributes dwAttributes;

        /// <summary>
        /// The display name of the file.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal readonly string szDisplayName;

        /// <summary>
        /// The type name string.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        internal readonly string szTypeName;
    }
}
