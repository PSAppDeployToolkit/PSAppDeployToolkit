using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PSADT.Interop.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Interop
{
    /// <summary>
    /// Contains information about a system stock icon, including its handle, image list index, and path.
    /// </summary>
    /// <remarks>This structure is used with the SHGetStockIconInfo function. When the SHGSI_ICON flag is specified,
    /// the hIcon field contains a handle that must be destroyed when no longer needed. Call <see cref="Dispose"/>
    /// to release the icon handle.</remarks>
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Interop struct requires explicit field layout.")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching native API naming convention.")]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHSTOCKICONINFO : IDisposable
    {
        /// <summary>
        /// Size of the structure.
        /// </summary>
        internal uint cbSize;

        /// <summary>
        /// Backing field for the icon handle.
        /// </summary>
        private HICON _hIcon;

        /// <summary>
        /// Gets the handle to the icon. Must be destroyed with DestroyIcon when no longer needed.
        /// Call <see cref="Dispose"/> to release the icon handle.
        /// </summary>
        internal readonly HICON hIcon => _hIcon;

        /// <summary>
        /// Index of the icon in the system image list.
        /// </summary>
        internal readonly int iSysImageIndex;

        /// <summary>
        /// Represents the index of an icon within an internal collection.
        /// </summary>
        internal readonly int iIcon;

        /// <summary>
        /// The path to the file containing the icon.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal readonly string szPath;

        /// <summary>
        /// Releases the icon handle if one was retrieved.
        /// </summary>
        public void Dispose()
        {
            if (default == _hIcon)
            {
                return;
            }
            BOOL res = PInvoke.DestroyIcon(_hIcon);
            try
            {
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
            }
            finally
            {
                _hIcon = default;
            }
        }
    }
}
