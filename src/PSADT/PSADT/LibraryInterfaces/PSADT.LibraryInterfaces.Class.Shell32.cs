using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using PSADT.Extensions;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;


namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the shell32.dll library.
    /// </summary>
    public static class Shell32
    {
        /// <summary>
        /// Execution information for the ShellExecuteEx function.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SHELLEXECUTEINFO
        {
            /// <summary>
            /// Union member for the icon or monitor handle.
            /// </summary>
            [StructLayout(LayoutKind.Explicit)]
            internal struct HICON_HMONITOR_UNION
            {
                [FieldOffset(0)]
                internal IntPtr hIcon;

                [FieldOffset(0)]
                internal IntPtr hMonitor;
            }

            /// <summary>
            /// Size of the structure.
            /// </summary>
            internal int cbSize;

            /// <summary>
            /// Flags that specify the behavior of the function.
            /// </summary>
            internal SEE_MASK_FLAGS fMask;

            /// <summary>
            /// Handle to the parent window used for displaying a UI or error messages.
            /// </summary>
            internal IntPtr hwnd;

            /// <summary>
            /// String that specifies the verb for the execution.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string? lpVerb;

            /// <summary>
            /// String that specifies the name of the file or object on which to execute the specified verb.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string lpFile;

            /// <summary>
            /// String that specifies the parameters to be passed to the application.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string? lpParameters;

            /// <summary>
            /// String that specifies the default directory.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string? lpDirectory;

            /// <summary>
            /// Flags that specify how an application is to be shown when it is opened.
            /// </summary>
            internal int nShow;

            /// <summary>
            /// Handle to the application that is calling the ShellExecuteEx function.
            /// </summary>
            internal IntPtr hInstApp;

            /// <summary>
            /// Union member for the ID list.
            /// </summary>
            internal IntPtr lpIDList;

            /// <summary>
            /// String that specifies the class.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string? lpClass;

            /// <summary>
            /// Handle to the key that identifies the file type.
            /// </summary>
            internal IntPtr hkeyClass;

            /// <summary>
            /// Flags that specify the input and output values of this structure.
            /// </summary>
            internal uint dwHotKey;

            /// <summary>
            /// Union member for the icon or monitor handle.
            /// </summary>
            internal HICON_HMONITOR_UNION Anonymous;

            /// <summary>
            /// Handle to the newly started application.
            /// </summary>
            internal IntPtr hProcess;
        }

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
        /// Sets the AUMID for the current process (used for naming balloon tips).
        /// </summary>
        /// <param name="AppID"></param>
        /// <returns></returns>
        public static int SetCurrentProcessExplicitAppUserModelID(string AppID)
        {
            return PInvoke.SetCurrentProcessExplicitAppUserModelID(AppID).ThrowOnFailure();
        }

        /// <summary>
        /// Queries the user notification state.
        /// </summary>
        /// <param name="pquns"></param>
        /// <returns></returns>
        internal static unsafe HRESULT SHQueryUserNotificationState(out Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE pquns)
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
        internal static unsafe void SHChangeNotify([MarshalAs(UnmanagedType.I4)] SHCNE_ID wEventId, SHCNF_FLAGS uFlags, [Optional] IntPtr dwItem1, [Optional] IntPtr dwItem2)
        {
            PInvoke.SHChangeNotify(wEventId, uFlags, dwItem1.ToPointer(), dwItem2.ToPointer());
        }

        /// <summary>
        /// Invokes an executable or action via the shell.
        /// </summary>
        /// <param name="lpExecInfo"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ShellExecuteExW")]
        private static extern bool ShellExecuteExNative(ref SHELLEXECUTEINFO lpExecInfo);

        /// <summary>
        /// Invokes an executable or action via the shell.
        /// </summary>
        /// <param name="lpExecInfo"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo)
        {
            var res = ShellExecuteExNative(ref lpExecInfo);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
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
                    args[i] = res[i].ToString().TrimRemoveNull();
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
