using Microsoft.Win32.SafeHandles;
using PSADT.UserInterface.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// This class provides an interface to the Kernel32 library.
    /// </summary>
    internal static class Kernel32
    {
        /// <summary>
        /// Opens a handle to a process with the specified access rights.
        /// </summary>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="bInheritHandle"></param>
        /// <param name="dwProcessId"></param>
        /// <returns></returns>
        internal static unsafe SafeFileHandle OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwProcessId)
        {
            var res = PInvoke.OpenProcess_SafeHandle(dwDesiredAccess, bInheritHandle, dwProcessId);
            if (res.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
