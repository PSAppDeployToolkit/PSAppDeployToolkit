using System;
using System.Runtime.InteropServices;
using PSADT.UserInterface.Utilities;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// This class provides an interface to the NtDll library, specifically for querying process information.
    /// </summary>
    internal static class NtDll
    {
        /// <summary>
        /// Queries information about a specified process.
        /// </summary>
        /// <param name="ProcessHandle"></param>
        /// <param name="ProcessInformationClass"></param>
        /// <param name="ProcessInformation"></param>
        /// <param name="ProcessInformationLength"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        internal static unsafe NTSTATUS NtQueryInformationProcess(SafeHandle ProcessHandle, PROCESSINFOCLASS ProcessInformationClass, void* ProcessInformation, uint ProcessInformationLength, out uint ReturnLength)
        {
            if (ProcessHandle is not object || ProcessHandle.IsClosed || ProcessHandle.IsInvalid)
            {
                throw new ArgumentNullException(nameof(ProcessHandle));
            }

            bool ProcessHandleAddRef = false;
            try
            {
                ProcessHandle.DangerousAddRef(ref ProcessHandleAddRef);
                fixed (uint* ReturnLengthLocal = &ReturnLength)
                {
                    var res = Windows.Wdk.PInvoke.NtQueryInformationProcess((HANDLE)ProcessHandle.DangerousGetHandle(), ProcessInformationClass, ProcessInformation, ProcessInformationLength, ReturnLengthLocal);
                    if (res != NTSTATUS.STATUS_SUCCESS && (res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || !ProcessInformationLength.Equals(0)))
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                    }
                    return res;
                }
            }
            finally
            {
                if (ProcessHandleAddRef)
                {
                    ProcessHandle.DangerousRelease();
                }
            }
        }
    }
}
