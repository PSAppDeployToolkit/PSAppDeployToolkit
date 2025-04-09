using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemInformation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the ntdll.dll library.
    /// </summary>
    internal static class Ntdll
    {
        /// <summary>
        /// Gets the version info of the current operating system from the kernel.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe NTSTATUS RtlGetVersion(out OSVERSIONINFOEXW lpVersionInformation)
        {
            lpVersionInformation = new() { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEXW>() };
            NTSTATUS status = Windows.Wdk.PInvoke.RtlGetVersion((OSVERSIONINFOW*)Unsafe.AsPointer(ref lpVersionInformation));
            if (status.Value < 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.RtlNtStatusToDosError(status));
            }
            return status;
        }
    }
}
