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
        /// Contains basic information about a process, as returned by native Windows API calls.
        /// </summary>
        /// <remarks>This structure is typically used with low-level Windows APIs to retrieve process
        /// details such as the process identifier and parent process identifier. It is intended for advanced scenarios
        /// involving interop with native code and is not commonly used in standard .NET application
        /// development.</remarks>
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
        /// Retrieves basic information about the specified process using the native NtQueryInformationProcess API.
        /// </summary>
        /// <remarks>This method wraps the native NtQueryInformationProcess function to obtain basic
        /// process information, such as the process ID and parent process ID. The method converts NTSTATUS error codes
        /// to Win32 error codes for exception handling.</remarks>
        /// <param name="processHandle">A handle to the process for which information is to be retrieved. The handle must have appropriate access
        /// rights, such as PROCESS_QUERY_INFORMATION.</param>
        /// <param name="processInformation">When this method returns, contains a PROCESS_BASIC_INFORMATION structure with information about the
        /// specified process.</param>
        /// <returns>An NTSTATUS code indicating the result of the operation. A value of 0 indicates success.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying NtQueryInformationProcess call fails. The exception's error code corresponds to the
        /// converted NTSTATUS value.</exception>
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
        /// Specifies the types of process information that can be queried or set using native Windows API calls.
        /// </summary>
        /// <remarks>This enumeration is typically used with low-level Windows functions such as
        /// NtQueryInformationProcess to indicate which class of information to retrieve or modify for a process. The
        /// available values correspond to specific structures or data returned by the operating system. This API is
        /// intended for advanced scenarios and may require platform invocation (P/Invoke) to use from managed
        /// code.</remarks>
        private enum PROCESSINFOCLASS : int
        {
            /// <summary>
            /// Retrieves the process basic information.
            /// </summary>
            ProcessBasicInformation = 0,
        }
    }
}
