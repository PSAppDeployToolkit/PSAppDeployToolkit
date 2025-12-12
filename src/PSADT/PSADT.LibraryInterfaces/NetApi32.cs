using System;
using PSADT.LibraryInterfaces.SafeHandles;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides interop methods for querying network join information on Windows systems.
    /// </summary>
    /// <remarks>This class contains static methods that wrap native Windows networking APIs. It is intended
    /// for internal use and is not thread-safe. Callers are responsible for managing any unmanaged resources returned
    /// by these methods, such as releasing buffer handles to prevent memory leaks.</remarks>
    internal static class NetApi32
    {
        /// <summary>
        /// Retrieves the join status and name of the domain or workgroup for the specified computer.
        /// </summary>
        /// <remarks>The caller must release the buffer referenced by lpNameBuffer to avoid memory leaks.
        /// This method throws an exception if the underlying Windows API call fails.</remarks>
        /// <param name="lpServer">The name of the remote server to query, or null to specify the local computer. The name must begin with \\
        /// if specified.</param>
        /// <param name="lpNameBuffer">When this method returns, contains a handle to a buffer that receives the name of the domain or workgroup.
        /// The caller is responsible for releasing this handle.</param>
        /// <param name="BufferType">When this method returns, contains a value that indicates the join status of the computer.</param>
        /// <returns>A WIN32_ERROR value that indicates the result of the operation. Returns NERR_Success if successful.</returns>
        internal static WIN32_ERROR NetGetJoinInformation(string? lpServer, out SafeNetApiBufferFreeHandle lpNameBuffer, out Windows.Win32.NetworkManagement.NetManagement.NETSETUP_JOIN_STATUS BufferType)
        {
            WIN32_ERROR res = (WIN32_ERROR)PInvoke.NetGetJoinInformation(lpServer, out PWSTR lpNameBufferLocal, out BufferType);
            if (res != PInvoke.NERR_Success)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            unsafe
            {
                lpNameBuffer = new((IntPtr)lpNameBufferLocal.Value, lpNameBufferLocal.Length, true);
            }
            return res;
        }

        /// <summary>
        /// Retrieves information about the join status of the local computer to a domain or workgroup.
        /// </summary>
        /// <param name="lpNameBuffer">When this method returns, contains a handle to a buffer that receives the name of the domain or workgroup.
        /// The caller is responsible for freeing this buffer.</param>
        /// <param name="BufferType">When this method returns, contains a value that specifies the join status of the local computer.</param>
        /// <returns>A WIN32_ERROR value that indicates the result of the operation. Returns ERROR_SUCCESS if the information is
        /// retrieved successfully; otherwise, returns a system error code.</returns>
        internal static WIN32_ERROR NetGetJoinInformation(out SafeNetApiBufferFreeHandle lpNameBuffer, out Windows.Win32.NetworkManagement.NetManagement.NETSETUP_JOIN_STATUS BufferType)
        {
            return NetGetJoinInformation(null, out lpNameBuffer, out BufferType);
        }
    }
}
