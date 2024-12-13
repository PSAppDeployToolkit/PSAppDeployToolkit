using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using PSADT.PInvoke;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.GUI
{
    public static class Explorer
    {
        /// <summary>
        /// Refreshes the desktop icons and updates the environment variables in the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails.</exception>
        public static void RefreshDesktopAndEnvironmentVariables()
        {
            // Update desktop icons using SHChangeNotify
            NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSHNOWAIT, IntPtr.Zero, IntPtr.Zero);

            // Notify all top-level windows that the environment variables have changed
            if (NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST,
                                                 NativeMethods.WM_SETTINGCHANGE,
                                                 IntPtr.Zero,
                                                 null,
                                                 NativeMethods.SMTO_ABORTIFHUNG,
                                                 100,
                                                 IntPtr.Zero) == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to send WM_SETTINGCHANGE message. Error code [{errorCode}].");
            }

            if (NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE,
                                                 IntPtr.Zero, "Environment", NativeMethods.SMTO_ABORTIFHUNG, 100,
                                                 IntPtr.Zero) == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to send WM_SETTINGCHANGE message for environment variables. Error code [{errorCode}].");
            }
        }

        /// <summary>
        /// Retrieves a string resource from the shell32.dll library.
        /// </summary>
        /// <param name="verbId">The identifier of the string resource.</param>
        /// <returns>The loaded string resource.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the library or string resource fails to load.</exception>
        public static string GetPinVerb(int verbId)
        {
            const string libraryName = "shell32.dll";

            // Load the shell32 library with the appropriate flags
            using SafeLibraryHandle hShell32Dll = NativeMethods.LoadLibraryEx(libraryName, SafeLibraryHandle.Null, LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE);
            if (hShell32Dll.IsInvalid || hShell32Dll.IsClosed)
            {
                ErrorHandler.ThrowSystemError($"Failed to load library [{libraryName}].", SystemErrorType.Win32);
            }

            // Load the string resource
            if(!NativeMethods.LoadString(hShell32Dll, verbId, out string? stringResource))
            {
                ErrorHandler.ThrowSystemError($"Failed to load string resource with verb id [{verbId}] from library [{libraryName}].", SystemErrorType.Win32);
            }

            return stringResource!;
        }

    }
}
