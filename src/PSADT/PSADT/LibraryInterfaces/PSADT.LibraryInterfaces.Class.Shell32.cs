using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
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
    }
}
