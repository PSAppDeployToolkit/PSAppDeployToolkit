using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.HiDpi;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the shcore.dll library.
    /// </summary>
    internal static class SHCore
    {
        /// <summary>
        /// Sets the current process as DPI-aware (Windows 8.1-onwards).
        /// </summary>
        /// <param name="dpiAwareness"></param>
        /// <returns></returns>
        internal static HRESULT SetProcessDpiAwareness(PROCESS_DPI_AWARENESS dpiAwareness)
        {
            return PInvoke.SetProcessDpiAwareness(dpiAwareness).ThrowOnFailure();
        }
    }
}
