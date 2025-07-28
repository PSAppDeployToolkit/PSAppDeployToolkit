using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSADT.Invoke.LibraryInterfaces
{
    /// <summary>
    /// Contains methods for interacting with the Windows NT kernel.
    /// </summary>
    internal static class NtDll
    {
        /// <summary>
        /// Contains information for a given process handle.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_BASIC_INFORMATION
        {
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        /// <summary>
        /// Retrieves information about a process.
        /// </summary>
        /// <param name="processHandle"></param>
        /// <param name="processInformation"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static int NtQueryInformationProcess(IntPtr processHandle, out PROCESS_BASIC_INFORMATION processInformation)
        {
            // Import the NtQueryInformationProcess function from ntdll.dll.
            [DllImport("ntdll.dll", ExactSpelling = true)]
            static extern int NtQueryInformationProcess(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

            // Import the RtlNtStatusToDosError function from ntdll.dll to convert NT status codes to Win32 error codes.
            [DllImport("ntdll.dll", ExactSpelling = true)]
            static extern uint RtlNtStatusToDosError(int Status);

            // Perform the query to get the process information.
            PROCESS_BASIC_INFORMATION processInformationLocal = new();
            var status = NtQueryInformationProcess(processHandle, PROCESSINFOCLASS.ProcessBasicInformation, ref processInformationLocal, Marshal.SizeOf(processInformationLocal), out _);
            if (status != 0)
            {
                throw new Win32Exception((int)RtlNtStatusToDosError(status));
            }
            processInformation = processInformationLocal;
            return status;
        }

        /// <summary>
        /// Process information classes for querying and setting process information.
        /// </summary>
        private enum PROCESSINFOCLASS : int
        {
            /// <summary>
            /// Retrieves the process basic information.
            /// </summary>
            ProcessBasicInformation = 0,
        }
    }
}
