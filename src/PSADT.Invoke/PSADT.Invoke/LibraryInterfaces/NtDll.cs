using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.Invoke.LibraryInterfaces
{
    /// <summary>
    /// Contains methods for interacting with the Windows NT kernel.
    /// </summary>
    internal static class NtDll
    {
        /// <summary>
        /// Retrieves basic information about the specified process using the native NtQueryInformationProcess API.
        /// </summary>
        /// <remarks>This method wraps the native NtQueryInformationProcess function to obtain basic
        /// process information, such as the process ID and parent process ID. The method converts NTSTATUS error codes
        /// to Win32 error codes for exception handling.</remarks>
        /// <param name="ProcessHandle">A handle to the process for which information is to be retrieved. The handle must have appropriate access
        /// rights, such as PROCESS_QUERY_INFORMATION.</param>
        /// <param name="ProcessBasicInformation">When this method returns, contains a PROCESS_BASIC_INFORMATION structure with information about the
        /// specified process.</param>
        /// <returns>An NTSTATUS code indicating the result of the operation. A value of 0 indicates success.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying NtQueryInformationProcess call fails. The exception's error code corresponds to the
        /// converted NTSTATUS value.</exception>
        internal static int NtQueryInformationProcess(SafeProcessHandle ProcessHandle, out PROCESS_BASIC_INFORMATION ProcessBasicInformation)
        {
            uint ReturnLengthLocal = 0; NTSTATUS res;
            bool ProcessHandleAddRef = false;
            try
            {
                ProcessHandle.DangerousAddRef(ref ProcessHandleAddRef);
                unsafe
                {
                    fixed (PROCESS_BASIC_INFORMATION* pProcessBasicInformation = &ProcessBasicInformation)
                    {
                        res = Windows.Wdk.PInvoke.NtQueryInformationProcess((HANDLE)ProcessHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation, pProcessBasicInformation, (uint)Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), ref ReturnLengthLocal);
                    }
                }
            }
            finally
            {
                if (ProcessHandleAddRef)
                {
                    ProcessHandle.DangerousRelease();
                }
            }
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw new Win32Exception((int)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
        }
    }
}
