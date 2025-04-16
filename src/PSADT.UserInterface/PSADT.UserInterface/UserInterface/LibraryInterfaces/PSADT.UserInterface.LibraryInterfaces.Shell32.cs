using PSADT.Utilities;
using Windows.Win32;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// Shell32 interface P/Invoke wrappers.
    /// </summary>
    internal static class Shell32
    {
        /// <summary>
        /// Extracts an icon from a file.
        /// </summary>
        /// <param name="lpszFile"></param>
        /// <param name="nIconIndex"></param>
        /// <param name="phiconLarge"></param>
        /// <param name="phiconSmall"></param>
        /// <param name="nIcons"></param>
        /// <returns></returns>
        internal static unsafe uint ExtractIconEx(string lpszFile, int nIconIndex, out DestroyIconSafeHandle phiconLarge, out DestroyIconSafeHandle phiconSmall, uint nIcons)
        {
            var res = PInvoke.ExtractIconEx(lpszFile, nIconIndex, out phiconLarge, out phiconSmall, nIcons);
            if (res == uint.MaxValue)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
