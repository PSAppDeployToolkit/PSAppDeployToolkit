using System;
using System.Runtime.InteropServices;
using PSADT.PInvoke;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Shared
{
    public static class Utility
    {
        /// <summary>
        /// Determines if the Out of Box Experience (OOBE) process is complete on a Windows system.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the OOBE process is complete; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The OOBE (Out of Box Experience) process is the initial setup that users go through when starting a new Windows
        /// device for the first time or after a system reset. It includes tasks such as setting up language preferences,
        /// creating a user account, agreeing to license terms, and configuring other initial settings.
        ///
        /// Some system operations or configurations may depend on the completion of the OOBE process. Knowing whether OOBE
        /// is complete can help ensure the system is fully initialized and ready for additional configurations or software
        /// deployments.
        ///
        /// If the underlying native method call fails, a system error will be thrown. Use this method in contexts where
        /// system initialization is critical to the next steps of execution.
        ///
        /// <para>If an error occurs, the method will throw a system error after a call to <see cref="GetLastWin32Error"/>.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the native method call to check the OOBE status fails./>
        /// for additional error information.
        /// </exception>
        public static bool IsOOBEComplete()
        {
            if (!NativeMethods.OOBEComplete(out int isOobeComplete))
            {
                ErrorHandler.ThrowSystemError("Failed to check OOBE status.", SystemErrorType.Win32);
            }

            return isOobeComplete != 0;
        }

        /// <summary>
        /// Returns the OS architecture of the current system.
        /// </summary>
        public static Architecture GetOperatingSystemArchitecture()
        {
            return RuntimeInformation.OSArchitecture;
        }
    }
}
