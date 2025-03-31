using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Public P/Invokes from the shell32.dll library.
    /// </summary>
    public static class Shell32
    {
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
        internal static unsafe HRESULT SHQueryUserNotificationState(out QUERY_USER_NOTIFICATION_STATE pquns)
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
    }
}
