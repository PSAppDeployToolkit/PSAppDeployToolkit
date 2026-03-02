using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.Interop.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Interop
{
    /// <summary>
    /// Contains information about a file object, including its icon, display name, and attributes.
    /// </summary>
    /// <remarks>This structure is used with the SHGetFileInfo function. When the SHGFI_ICON flag is specified,
    /// the hIcon field contains a handle that must be destroyed when no longer needed. Call <see cref="Dispose"/>
    /// to release the icon handle.</remarks>
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Interop struct requires explicit field layout.")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching native API naming convention.")]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct SHFILEINFO : IDisposable
    {
        /// <summary>
        /// Backing field for the icon handle.
        /// </summary>
        private HICON _hIcon;

        /// <summary>
        /// Gets the file system icon handle. Must be destroyed with DestroyIcon when no longer needed.
        /// Call <see cref="Dispose"/> to release the icon handle.
        /// </summary>
        internal readonly HICON hIcon => _hIcon;

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
