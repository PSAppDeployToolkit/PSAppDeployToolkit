using System;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.System.LibraryLoader;

namespace PSADT.Utilities
{
    /// <summary>
    /// Public P/Invokes from the msi.dll library.
    /// </summary>
    public static class MsiUtilities
    {
        /// <summary>
        /// Retrieves the message string associated with an MSI exit code from the msimsg.dll resource.
        /// </summary>
        /// <param name="msiExitCode">The MSI exit code.</param>
        /// <returns>The message string associated with the given MSI exit code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the library cannot be loaded or the message cannot be retrieved.</exception>
        public static string? GetMessageFromMsiExitCode(uint msiExitCode)
        {
            var hMsiMsgDll = Kernel32.LoadLibraryEx("msimsg.dll", default, LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                var buffer = new char[4096];
                var bufspan = new Span<char>(buffer);
                User32.LoadString(hMsiMsgDll, msiExitCode, bufspan);
                var msiMsgString = bufspan.ToString().Trim('\0').Trim();
                return !string.IsNullOrWhiteSpace(msiMsgString) ? msiMsgString : null;
            }
            finally
            {
                Kernel32.FreeLibrary(ref hMsiMsgDll);
            }
        }
    }
}

