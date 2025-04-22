using System;
using System.Runtime.InteropServices;
using Windows.Win32.Storage.FileSystem;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// Shell32 interface P/Invoke wrappers.
    /// </summary>
    internal static class Shell32
    {
        /// <summary>
        /// Struct for retrieving information about a file, including its icon and display name.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct SHFILEINFO
        {
            /// <summary>
            /// The file system icon handle (must be destroyed with DestroyIcon when no longer needed).
            /// </summary>
            internal IntPtr hIcon;

            /// <summary>
            /// The index of the icon in the system image list.
            /// </summary>
            internal int iIcon;

            /// <summary>
            /// The file attributes (e.g., read-only, hidden, system).
            /// </summary>
            internal FILE_FLAGS_AND_ATTRIBUTES dwAttributes;

            /// <summary>
            /// The display name of the file.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string szDisplayName;

            /// <summary>
            /// The type name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            internal string szTypeName;
        }

        /// <summary>
        /// Flags for SHGetFileInfo function.
        /// </summary>
        /// <param name="pszPath"></param>
        /// <param name="dwFileAttributes"></param>
        /// <param name="psfi"></param>
        /// <param name="cbFileInfo"></param>
        /// <param name="uFlags"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, FILE_FLAGS_AND_ATTRIBUTES dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, SHGFI_FLAGS uFlags);

        /// <summary>
        /// Retrieves information about a file, including its icon and display name.
        /// </summary>
        /// <param name="pszPath"></param>
        /// <param name="uFlags"></param>
        /// <param name="dwFileAttributes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static SHFILEINFO SHGetFileInfo(string pszPath, SHGFI_FLAGS uFlags, FILE_FLAGS_AND_ATTRIBUTES dwFileAttributes = 0)
        {
            SHFILEINFO shinfo = new();
            IntPtr hImg = SHGetFileInfo(pszPath, dwFileAttributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), uFlags);
            if (hImg == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to retrieve file information.");
            }
            return shinfo;
        }
    }
}
