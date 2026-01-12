using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides managed wrappers for selected native Gdi32.dll Windows API functions.
    /// </summary>
    internal static class Gdi32
    {
        /// <summary>
        /// Adds the font resource from the specified file to the system font table.
        /// </summary>
        /// <param name="lpFilename">A null-terminated character string that contains a valid font file name.</param>
        /// <returns>If the function succeeds, the return value specifies the number of fonts added. If the function fails, the return value is zero.</returns>
        internal static int AddFontResource(string lpFilename)
        {
            unsafe
            {
                fixed (char* lpFilenamePtr = lpFilename)
                {
                    int res = PInvoke.AddFontResource((PCWSTR)lpFilenamePtr);
                    // AddFontResource returns 0 on failure, but does not SetLastError.
                    return res;
                }
            }
        }

        /// <summary>
        /// Removes the fonts in the specified file from the system font table.
        /// </summary>
        /// <param name="lpFileName">A null-terminated character string that names a font resource file.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        internal static BOOL RemoveFontResource(string lpFileName)
        {
            unsafe
            {
                fixed (char* lpFileNamePtr = lpFileName)
                {
                    BOOL res = PInvoke.RemoveFontResource((PCWSTR)lpFileNamePtr);
                    // RemoveFontResource returns 0 on failure, but does not SetLastError.
                    return res;
                }
            }
        }
    }
}
