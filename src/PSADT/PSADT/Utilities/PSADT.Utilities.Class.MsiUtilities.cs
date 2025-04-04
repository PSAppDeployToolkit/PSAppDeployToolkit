using System;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.System.LibraryLoader;

namespace PSADT.Utilities
{
    /// <summary>
    /// Public P/Invokes from the msi.dll library.
    /// </summary>
    public static class Msi
    {
        /// <summary>
        /// Retrieves the message string associated with an MSI exit code from the msimsg.dll resource.
        /// </summary>
        /// <param name="msiExitCode">The MSI exit code.</param>
        /// <returns>The message string associated with the given MSI exit code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the library cannot be loaded or the message cannot be retrieved.</exception>
        public static string? GetMessageFromMsiExitCode(uint msiExitCode)
        {
            if (Kernel32.LoadLibraryEx("msimsg.dll", LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE) is FreeLibrarySafeHandle hMsiMsgDll)
            {
                try
                {
                    if (!hMsiMsgDll.IsInvalid && !hMsiMsgDll.IsClosed)
                    {
                        var buffer = new char[4096];
                        User32.LoadString(hMsiMsgDll, msiExitCode, buffer, buffer.Length);
                        var msiMsgString = new string(buffer).Trim();
                        if (!string.IsNullOrWhiteSpace(msiMsgString))
                        {
                            return msiMsgString;
                        }
                    }
                }
                finally
                {
                    hMsiMsgDll.Dispose();
                }
            }
            return null;
        }
    }
}

