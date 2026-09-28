using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PSADT.Interop
{
    /// <summary>
    /// Contains information used by ShellExecuteEx.
    /// </summary>
    /// <remarks>Hand-written because the native structure is packed to one byte on x86 and eight on x64, which CsWin32
    /// refuses to generate for an AnyCPU assembly. Every field is four bytes or pointer-sized, so sequential layout matches both.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHELLEXECUTEINFOW
    {
        /// <summary>
        /// Size of the structure, which the wrapper stamps.
        /// </summary>
        internal uint cbSize;

        /// <summary>
        /// The SEE_MASK flags that indicate the content and validity of the other members.
        /// </summary>
        internal SEE_MASK_FLAGS fMask;

        /// <summary>
        /// A handle to the parent window used to display any UI or error messages.
        /// </summary>
        internal HWND hwnd;

        /// <summary>
        /// The action to perform, or null for the default verb.
        /// </summary>
        internal PCWSTR lpVerb;

        /// <summary>
        /// The file or object on which to perform the action.
        /// </summary>
        internal PCWSTR lpFile;

        /// <summary>
        /// The parameters to pass to the application, or null for none.
        /// </summary>
        internal PCWSTR lpParameters;

        /// <summary>
        /// The working directory for the action, or null for the current one.
        /// </summary>
        internal PCWSTR lpDirectory;

        /// <summary>
        /// The SW_ value that specifies how the application is shown.
        /// </summary>
        internal SHOW_WINDOW_CMD nShow;

        /// <summary>
        /// The site to consult when SEE_MASK_FLAG_HINST_IS_SITE is set; otherwise, receives an SE_ERR value on failure.
        /// </summary>
        internal HINSTANCE hInstApp;

        /// <summary>
        /// The item identifier list of the item to act on, when SEE_MASK_IDLIST is set.
        /// </summary>
        internal nint lpIDList;

        /// <summary>
        /// The file class or GUID to act under, when SEE_MASK_CLASSNAME is set.
        /// </summary>
        internal PCWSTR lpClass;

        /// <summary>
        /// A handle to the registry key for the file class, when SEE_MASK_CLASSKEY is set.
        /// </summary>
        internal HKEY hkeyClass;

        /// <summary>
        /// The hot key to associate with the application, when SEE_MASK_HOTKEY is set.
        /// </summary>
        internal uint dwHotKey;

        /// <summary>
        /// The icon or monitor for the application, when SEE_MASK_ICON or SEE_MASK_HMONITOR is set.
        /// </summary>
        internal HANDLE hIcon;

        /// <summary>
        /// Receives a handle to the new process when SEE_MASK_NOCLOSEPROCESS is set and the shell created one.
        /// </summary>
        internal HANDLE hProcess;
    }
}
