using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Interop
{
    /// <summary>
    /// Flags for SHGetStockIconInfo function.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHSTOCKICONINFO
    {
        /// <summary>
        /// Size of the structure.
        /// </summary>
        internal uint cbSize;

        /// <summary>
        /// Handle to the icon.
        /// </summary>
        internal HICON hIcon;

        /// <summary>
        /// Index of the icon in the system image list.
        /// </summary>
        internal int iSysImageIndex;

        /// <summary>
        /// Represents the index of an icon within an internal collection.
        /// </summary>
        internal int iIcon;

        /// <summary>
        /// Index of the icon in the small image list.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string szPath;
    }
}
