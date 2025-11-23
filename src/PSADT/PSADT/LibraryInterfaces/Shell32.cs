using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the shell32.dll library.
    /// </summary>
    internal static class Shell32
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
            internal readonly FILE_FLAGS_AND_ATTRIBUTES dwAttributes;

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

        /// <summary>
        /// Sets the AUMID for the current process (used for naming balloon tips).
        /// </summary>
        /// <param name="AppID"></param>
        /// <returns></returns>
        internal static int SetCurrentProcessExplicitAppUserModelID(string AppID)
        {
            return PInvoke.SetCurrentProcessExplicitAppUserModelID(AppID).ThrowOnFailure();
        }

        /// <summary>
        /// Queries the user notification state.
        /// </summary>
        /// <param name="pquns"></param>
        /// <returns></returns>
        internal static HRESULT SHQueryUserNotificationState(out Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE pquns)
        {
            return PInvoke.SHQueryUserNotificationState(out pquns).ThrowOnFailure();
        }

        /// <summary>
        /// Notifies the system of an event that an application has performed.
        /// </summary>
        /// <param name="wEventId"></param>
        /// <param name="uFlags"></param>
        /// <param name="dwItem1"></param>
        /// <param name="dwItem2"></param>
        internal unsafe static void SHChangeNotify([MarshalAs(UnmanagedType.I4)] SHCNE_ID wEventId, SHCNF_FLAGS uFlags, [Optional] IntPtr dwItem1, [Optional] IntPtr dwItem2)
        {
            PInvoke.SHChangeNotify(wEventId, uFlags, dwItem1.ToPointer(), dwItem2.ToPointer());
        }

        /// <summary>
        /// Retrieves information about a file, including its icon and display name.
        /// </summary>
        /// <param name="pszPath"></param>
        /// <param name="psfi"></param>
        /// <param name="uFlags"></param>
        /// <param name="dwFileAttributes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static IntPtr SHGetFileInfo(string pszPath, out SHFILEINFO psfi, SHGFI_FLAGS uFlags, FILE_FLAGS_AND_ATTRIBUTES dwFileAttributes = 0)
        {
            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            static extern IntPtr SHGetFileInfo(string pszPath, FILE_FLAGS_AND_ATTRIBUTES dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, SHGFI_FLAGS uFlags);
            psfi = new(); var res = SHGetFileInfo(pszPath, dwFileAttributes, ref psfi, (uint)Marshal.SizeOf(psfi), uFlags);
            if (res == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to retrieve file information.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves information about a stock icon.
        /// </summary>
        /// <param name="siid"></param>
        /// <param name="uFlags"></param>
        /// <param name="psii"></param>
        /// <returns></returns>
        internal static HRESULT SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, out SHSTOCKICONINFO psii)
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            static extern HRESULT SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, ref SHSTOCKICONINFO psii);
            psii = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO)) };
            return SHGetStockIconInfo(siid, uFlags, ref psii).ThrowOnFailure();
        }

        /// <summary>
        /// Retrieves a handle to an image list that contains the system icons.
        /// </summary>
        /// <param name="iImageList"></param>
        /// <param name="ppvObj"></param>
        /// <returns></returns>
        internal static HRESULT SHGetImageList(SHIL_SIZE iImageList, out IImageList ppvObj)
        {
            var res = PInvoke.SHGetImageList((int)iImageList, new("46EB5926-582E-4017-9FDF-E8998DAA0950"), out var ppvObjLocal).ThrowOnFailure();
            ppvObj = (IImageList)ppvObjLocal;
            return res;
        }
    }
}
