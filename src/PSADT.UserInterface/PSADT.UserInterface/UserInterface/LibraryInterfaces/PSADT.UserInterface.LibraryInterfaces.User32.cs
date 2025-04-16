using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// User32 interface P/Invoke wrappers.
    /// </summary>
    internal static class User32
    {
        /// <summary>
        /// Destroys an icon.
        /// </summary>
        /// <param name="hIcon"></param>
        /// <returns></returns>
        internal static BOOL DestroyIcon(HICON hIcon)
        {
            var res = PInvoke.DestroyIcon(hIcon);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Destroys an icon.
        /// </summary>
        /// <param name="hIcon"></param>
        /// <returns></returns>
        internal static BOOL DestroyIcon(IntPtr hIcon)
        {
            return DestroyIcon((HICON)hIcon);
        }
    }
}
