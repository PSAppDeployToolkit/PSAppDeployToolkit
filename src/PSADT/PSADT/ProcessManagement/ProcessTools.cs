using System;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides utility methods for working with process handles and access rights.
    /// </summary>
    /// <remarks>This class is intended for internal use and is not intended to be used directly from
    /// application code.</remarks>
    internal static class ProcessTools
    {
        /// <summary>
        /// Determines whether the specified process token grants the requested access rights.
        /// </summary>
        /// <remarks>This method attempts to duplicate the provided process token with the requested
        /// access rights. If the operation fails due to insufficient permissions or an unauthorized access, the method
        /// returns false instead of throwing an exception.</remarks>
        /// <param name="token">A handle to the process token to test. The handle must be valid and have appropriate permissions for
        /// duplication.</param>
        /// <param name="accessRights">The access rights to test for the specified process token.</param>
        /// <returns>true if the process token grants the specified access rights; otherwise, false.</returns>
        internal static bool TestProcessAccessRights(SafeProcessHandle token, PROCESS_ACCESS_RIGHTS accessRights)
        {
            using SafeProcessHandle cProcessSafeHandle = Kernel32.GetCurrentProcess();
            try
            {
                BOOL res = Kernel32.DuplicateHandle(cProcessSafeHandle, token, cProcessSafeHandle, out SafeFileHandle newHandle, accessRights, false, 0);
                using (newHandle)
                {
                    return res;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
