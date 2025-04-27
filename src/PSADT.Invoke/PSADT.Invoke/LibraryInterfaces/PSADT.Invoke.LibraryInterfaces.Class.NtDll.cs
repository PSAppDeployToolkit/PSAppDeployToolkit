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
        /// <param name="ProcessHandle"></param>
        /// <param name="ProcessInformationClass"></param>
        /// <param name="ProcessInformation"></param>
        /// <param name="ProcessInformationLength"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationProcess")]
        private static extern int NtQueryInformationProcessNative(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Retrieves information about a process.
        /// </summary>
        /// <param name="processHandle"></param>
        /// <param name="processInformationClass"></param>
        /// <param name="processInformation"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static int NtQueryInformationProcess(IntPtr processHandle, PROCESSINFOCLASS processInformationClass, out PROCESS_BASIC_INFORMATION processInformation)
        {
            PROCESS_BASIC_INFORMATION processInformationLocal = new PROCESS_BASIC_INFORMATION();
            var status = NtQueryInformationProcessNative(processHandle, processInformationClass, ref processInformationLocal, Marshal.SizeOf(processInformationLocal), out _);
            if (status != 0)
            {
                throw new Win32Exception((int)RtlNtStatusToDosError(status));
            }
            processInformation = processInformationLocal;
            return status;
        }

        /// <summary>
        /// Retrieves the processor architecture of the system.
        /// </summary>
        /// <param name="Status"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll")]
        internal static extern uint RtlNtStatusToDosError(int Status);
    }
}
