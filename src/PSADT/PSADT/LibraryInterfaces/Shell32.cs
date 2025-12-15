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
    /// Provides managed wrappers and supporting structures for selected Windows Shell (shell32.dll) API functions,
    /// enabling access to shell features such as file information, stock icons, and system image lists.
    /// </summary>
    /// <remarks>This class is intended for internal use to facilitate interoperation with Windows Shell APIs.
    /// It exposes methods and structures that allow managed code to interact with native shell functionality, such as
    /// retrieving file icons, setting application user model IDs, and notifying the shell of changes. All members are
    /// static and are not intended for use outside of the containing assembly.</remarks>
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
        /// Sets the explicit Application User Model ID (AppUserModelID) for the current process.
        /// </summary>
        /// <remarks>Setting an explicit AppUserModelID allows the application's windows to be grouped and
        /// managed together in the Windows taskbar and Start menu. This method should be called before creating any
        /// windows to ensure consistent behavior.</remarks>
        /// <param name="AppID">The AppUserModelID to assign to the current process. This value is used by Windows to group windows and
        /// taskbar buttons. Cannot be null.</param>
        /// <returns>An HRESULT indicating the success or failure of the operation. A value of 0 indicates success; any other
        /// value indicates an error.</returns>
        internal static HRESULT SetCurrentProcessExplicitAppUserModelID(string AppID)
        {
            return PInvoke.SetCurrentProcessExplicitAppUserModelID(AppID).ThrowOnFailure();
        }

        /// <summary>
        /// Retrieves the current user notification state, indicating the user's availability for receiving
        /// notifications.
        /// </summary>
        /// <remarks>This method wraps the native SHQueryUserNotificationState function and throws an
        /// exception if the operation fails. The notification state can be used to determine whether it is appropriate
        /// to display notifications to the user.</remarks>
        /// <param name="pquns">When this method returns, contains a value from the QUERY_USER_NOTIFICATION_STATE enumeration that specifies
        /// the user's current notification state.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static HRESULT SHQueryUserNotificationState(out Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE pquns)
        {
            return PInvoke.SHQueryUserNotificationState(out pquns).ThrowOnFailure();
        }

        /// <summary>
        /// Notifies the system of a change to the shell namespace, such as the creation, deletion, or modification of
        /// files or folders.
        /// </summary>
        /// <remarks>This method is typically used to inform the Windows shell of changes made to the file
        /// system or shell items by applications, ensuring that the shell and other components remain in sync with the
        /// current state. The caller is responsible for ensuring that the parameters are valid and that the
        /// notification is appropriate for the event.</remarks>
        /// <param name="wEventId">A value that specifies the type of event that has occurred. This determines the kind of notification to
        /// send.</param>
        /// <param name="uFlags">Flags that indicate the meaning of the dwItem1 and dwItem2 parameters and how the notification is to be
        /// handled.</param>
        /// <param name="dwItem1">A pointer to an item or structure relevant to the event, as defined by the event type and flags. The
        /// interpretation depends on the values of wEventId and uFlags. This parameter is optional and may be
        /// IntPtr.Zero if not required.</param>
        /// <param name="dwItem2">A pointer to a second item or structure relevant to the event, as defined by the event type and flags. The
        /// interpretation depends on the values of wEventId and uFlags. This parameter is optional and may be
        /// IntPtr.Zero if not required.</param>
        internal static void SHChangeNotify([MarshalAs(UnmanagedType.I4)] SHCNE_ID wEventId, SHCNF_FLAGS uFlags, [Optional] IntPtr dwItem1, [Optional] IntPtr dwItem2)
        {
            unsafe
            {
                PInvoke.SHChangeNotify(wEventId, uFlags, (void*)dwItem1, (void*)dwItem2);
            }
        }

        /// <summary>
        /// Retrieves information about a file or folder, such as its icon, display name, and type, using the Windows
        /// Shell API.
        /// </summary>
        /// <remarks>This method is a managed wrapper for the native SHGetFileInfo function in
        /// shell32.dll. The caller is responsible for managing any resources associated with the returned handle, such
        /// as destroying icon handles if applicable. The method may return different types of handles depending on the
        /// flags specified in uFlags.</remarks>
        /// <param name="pszPath">The path to the file or folder for which to retrieve information. Can be null or an empty string if used
        /// with certain flags that do not require a path.</param>
        /// <param name="psfi">When this method returns, contains a structure that receives the file information retrieved by the function.</param>
        /// <param name="uFlags">A combination of flags that specify which file information to retrieve. These flags determine the attributes
        /// and details returned.</param>
        /// <param name="dwFileAttributes">File attribute flags to use when retrieving information. Used only if the uFlags parameter includes a flag
        /// indicating that file attributes are provided. The default is 0.</param>
        /// <returns>A handle to the system image list or icon, depending on the flags specified. The handle is valid only while
        /// the image list exists. Returns a non-zero value on success.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the file information could not be retrieved.</exception>
        internal static IntPtr SHGetFileInfo(string pszPath, out SHFILEINFO psfi, SHGFI_FLAGS uFlags, FILE_FLAGS_AND_ATTRIBUTES dwFileAttributes = 0)
        {
            [DllImport("shell32.dll", CharSet = CharSet.Auto), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern IntPtr SHGetFileInfoW(string pszPath, FILE_FLAGS_AND_ATTRIBUTES dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, SHGFI_FLAGS uFlags);
            psfi = new(); IntPtr res = SHGetFileInfoW(pszPath, dwFileAttributes, ref psfi, (uint)Marshal.SizeOf(psfi), uFlags);
            if (res == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to retrieve file information.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves information about a specified stock icon, such as its handle, path, or index, based on the
        /// provided flags.
        /// </summary>
        /// <remarks>The caller should check the returned HRESULT to determine whether the operation
        /// succeeded. The contents of the returned SHSTOCKICONINFO structure depend on the flags specified in the
        /// uFlags parameter.</remarks>
        /// <param name="siid">The identifier of the stock icon to retrieve information for.</param>
        /// <param name="uFlags">A combination of flags that specify which information about the stock icon to retrieve.</param>
        /// <param name="psii">When this method returns, contains a structure that receives the requested stock icon information.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static HRESULT SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, out SHSTOCKICONINFO psii)
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern HRESULT SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, ref SHSTOCKICONINFO psii);
            psii = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>() };
            return SHGetStockIconInfo(siid, uFlags, ref psii).ThrowOnFailure();
        }

        /// <summary>
        /// Retrieves a handle to the system image list of the specified size.
        /// </summary>
        /// <remarks>This method is typically used to obtain a system image list for use with
        /// shell-related controls, such as list views or tree views. The caller is responsible for releasing the
        /// returned image list interface when it is no longer needed.</remarks>
        /// <param name="iImageList">The size of the image list to retrieve. This value specifies which system image list is returned.</param>
        /// <param name="ppvObj">When this method returns, contains the interface pointer to the retrieved image list. This parameter is
        /// passed uninitialized.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static HRESULT SHGetImageList(SHIL_SIZE iImageList, out IImageList ppvObj)
        {
            HRESULT res = PInvoke.SHGetImageList((int)iImageList, new("46EB5926-582E-4017-9FDF-E8998DAA0950"), out object ppvObjLocal).ThrowOnFailure();
            ppvObj = (IImageList)ppvObjLocal;
            return res;
        }
    }
}
