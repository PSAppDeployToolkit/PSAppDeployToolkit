using System;
using System.Runtime.InteropServices;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.UserInterface.Utilities
{
    internal static class ProcessUtilities
    {
        /// <summary>
        /// Retrieves the command line arguments of a process given its process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal static unsafe string GetProcessCommandLine(int processId)
        {
            // Open the process's handle with the relevant access rights.
            using (var hProc = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)processId))
            {
                // Get the required length we need for the buffer, then retrieve the actual command line string.
                NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, null, 0, out var requiredLength);
                IntPtr buffer = Marshal.AllocHGlobal((int)requiredLength);
                try
                {
                    NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, buffer.ToPointer(), requiredLength, out _);
                    return Marshal.PtrToStructure<UNICODE_STRING>(buffer).Buffer.ToString().Replace("\0", string.Empty).Trim();
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }
}
