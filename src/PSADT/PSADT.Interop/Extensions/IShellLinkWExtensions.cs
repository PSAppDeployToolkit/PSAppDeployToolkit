using System;
using Windows.Win32.UI.Shell;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for the IShellLinkW interface to enhance shell link functionality.
    /// </summary>
    /// <remarks>This class contains static methods that extend the capabilities of the IShellLinkW interface,
    /// particularly for use in unmanaged code contexts. It is important to ensure that any spans used are properly
    /// allocated to prevent buffer overflows.</remarks>
    internal static class IShellLinkWExtensions
    {
        /// <summary>
        /// Retrieves the path associated with the specified shell link and stores it in the provided character span.
        /// </summary>
        /// <remarks>This extension method simplifies obtaining the path from an IShellLinkW instance by
        /// allowing direct use of a character span, eliminating the need to manually manage pointers.</remarks>
        /// <param name="this">The instance of the IShellLinkW interface from which to retrieve the path.</param>
        /// <param name="pszFile">A span of characters that receives the path of the shell link. The span must be large enough to hold the
        /// resulting path.</param>
        /// <param name="fFlags">Flags that determine how the path is retrieved. These may modify the behavior of the retrieval process.</param>
        internal static void GetPath(this IShellLinkW @this, Span<char> pszFile, uint fFlags)
        {
            unsafe
            {
                fixed (char* pszFileLocal = pszFile)
                {
                    @this.GetPath(pszFileLocal, pszFile.Length, null, fFlags);
                }
            }
        }
    }
}
