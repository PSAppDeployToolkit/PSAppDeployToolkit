using System;
using PSADT.PInvokes;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Installer
{
    public static class Msi
    {
        /// <summary>
        /// Retrieves the message string associated with an MSI exit code from the msimsg.dll resource.
        /// </summary>
        /// <param name="msiExitCode">The MSI exit code.</param>
        /// <returns>The message string associated with the given MSI exit code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the library cannot be loaded or the message cannot be retrieved.</exception>
        public static string GetMessageFromMsiExitCode(uint msiExitCode)
        {
            const string libraryName = "msimsg.dll";
            using SafeLibraryHandle hMsiMsgDll = NativeMethods.LoadLibraryEx(libraryName, SafeLibraryHandle.Null, LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE);

            if (hMsiMsgDll.IsInvalid || hMsiMsgDll.IsClosed)
            {
                ErrorHandler.ThrowSystemError($"Failed to load library [{libraryName}].", SystemErrorType.Win32);
            }
            if (!NativeMethods.LoadString(hMsiMsgDll, (int)msiExitCode, out string? message))
            {
                ErrorHandler.ThrowSystemError($"Failed to retrieve the message for MSI exit code {msiExitCode}.", SystemErrorType.Win32);
            }

            return message!;
        }
    }
}

