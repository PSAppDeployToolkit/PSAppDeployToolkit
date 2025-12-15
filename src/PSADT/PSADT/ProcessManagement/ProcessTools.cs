using System;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.ProcessManagement
{
    internal static class ProcessTools
    {
        /// <summary>
        /// Checks if a process is running by its process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal static bool IsProcessRunning(int processId)
        {
            // Opens a handle to a process and tests whether it's exit code is still active or not.
            // If we fail to open the process because of invalid input, we assume it is not running.
            try
            {
                using SafeFileHandle hProc = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE, false, (uint)processId);
                _ = Kernel32.GetExitCodeProcess(hProc, out uint exitCode);
                return exitCode == NTSTATUS.STILL_ACTIVE;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }
    }
}
