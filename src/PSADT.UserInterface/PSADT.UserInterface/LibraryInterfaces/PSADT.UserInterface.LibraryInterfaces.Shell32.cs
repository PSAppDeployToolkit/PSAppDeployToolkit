using System;
using System.Linq;
using System.Runtime.InteropServices;
using PSADT.UserInterface.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;

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
            internal IntPtr hIcon;

            /// <summary>
            /// Index of the icon in the system image list.
            /// </summary>
            internal int iSysImageIndex;

            /// <summary>
            /// Index of the icon in the small image list.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string szPath;
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

        /// <summary>
        /// Retrieves information about a stock icon.
        /// </summary>
        /// <param name="siid"></param>
        /// <param name="uFlags"></param>
        /// <param name="psii"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetStockIconInfo")]
        private static extern HRESULT SHGetStockIconInfoNative(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, ref SHSTOCKICONINFO psii);

        /// <summary>
        /// Retrieves information about a stock icon.
        /// </summary>
        /// <param name="siid"></param>
        /// <param name="uFlags"></param>
        /// <param name="psii"></param>
        /// <returns></returns>
        internal static HRESULT SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, out SHSTOCKICONINFO psii)
        {
            psii = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO)) };
            return SHGetStockIconInfoNative(siid, uFlags, ref psii).ThrowOnFailure();
        }

        /// <summary>
        /// Retrieves a handle to an image list that contains the system icons.
        /// </summary>
        /// <param name="iImageList"></param>
        /// <param name="ppvObj"></param>
        /// <returns></returns>
        internal static HRESULT SHGetImageList(SHIL_SIZE iImageList, out IImageList ppvObj)
        {
            var res = PInvoke.SHGetImageList((int)iImageList, new System.Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"), out var ppvObjLocal).ThrowOnFailure();
            ppvObj = (IImageList)ppvObjLocal;
            return res;
        }

        /// <summary>
        /// Converts a command line string into an array of arguments.
        /// </summary>
        /// <param name="lpCmdLine"></param>
        /// <returns></returns>
        internal static unsafe string[] CommandLineToArgv(string lpCmdLine)
        {
            var res = PInvoke.CommandLineToArgv(lpCmdLine, out var pNumArgs);
            if (null == res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            try
            {
                var args = new string[pNumArgs];
                for (var i = 0; i < pNumArgs; i++)
                {
                    args[i] = res[i].ToString().Replace("\0", string.Empty).Trim();
                }
                return args.Where(static str => !string.IsNullOrWhiteSpace(str)).ToArray();
            }
            finally
            {
                Kernel32.LocalFree((HLOCAL)res);
            }
        }
    }
}
