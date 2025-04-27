using System;
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

        /// <summary>
        /// Allocates a specified number of bytes in the local heap.
        /// </summary>
        /// <param name="hMem"></param>
        /// <returns></returns>
        internal static HLOCAL LocalFree(HLOCAL hMem)
        {
            var res = PInvoke.LocalFree(hMem);
            if (!res.IsNull)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Wrapper around QueryDosDevice to manage error handling.
        /// </summary>
        /// <param name="lpDeviceName"></param>
        /// <param name="lpTargetPath"></param>
        /// <returns></returns>
        internal static uint QueryDosDevice(string lpDeviceName, Span<char> lpTargetPath)
        {
            var res = PInvoke.QueryDosDevice(lpDeviceName, lpTargetPath);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
