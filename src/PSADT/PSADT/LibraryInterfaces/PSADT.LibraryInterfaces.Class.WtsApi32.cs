using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the wtsapi32.dll library.
    /// </summary>
    internal static class WtsApi32
    {
        /// <summary>
        /// Wrapper around WTSEnumerateSessions to manage error handling.
        /// </summary>
        /// <param name="hServer"></param>
        /// <param name="pSessionInfo"></param>
        /// <param name="pCount"></param>
        /// <returns></returns>
        internal static unsafe BOOL WTSEnumerateSessions(HANDLE hServer, out IntPtr pSessionInfo, out uint pCount)
        {
            fixed (IntPtr* ppSessionInfo = &pSessionInfo)
            fixed (uint* ppCount = &pCount)
            {
                var res = PInvoke.WTSEnumerateSessions(hServer, 0, 1, (WTS_SESSION_INFOW**)ppSessionInfo, ppCount);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Wrapper around WTSQuerySessionInformation to manage error handling.
        /// </summary>
        /// <param name="hServer"></param>
        /// <param name="SessionId"></param>
        /// <param name="WTSInfoClass"></param>
        /// <param name="ppBuffer"></param>
        /// <param name="pBytesReturned"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL WTSQuerySessionInformation(HANDLE hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass, out IntPtr pBuffer, out uint bytesReturned)
        {
            fixed (IntPtr* ppBuffer = &pBuffer)
            fixed (uint* pBytesReturned = &bytesReturned)
            {
                var res = PInvoke.WTSQuerySessionInformation(hServer, SessionId, WTSInfoClass, (PWSTR*)ppBuffer, pBytesReturned);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Wrapper around WTSQueryUserToken to manage error handling.
        /// </summary>
        /// <param name="SessionId"></param>
        /// <param name="phToken"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL WTSQueryUserToken(uint SessionId, out SafeAccessTokenHandle phToken)
        {
            HANDLE phTokenLocal;
            var res = PInvoke.WTSQueryUserToken(SessionId, &phTokenLocal);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            phToken = new SafeAccessTokenHandle(phTokenLocal);
            return res;
        }

        /// <summary>
        /// Wrapper around WTSFreeMemory to manage error handling.
        /// </summary>
        /// <param name="pMemory"></param>
        internal static unsafe void WTSFreeMemory(ref IntPtr pMemory)
        {
            if (pMemory != default && IntPtr.Zero == pMemory)
            {
                PInvoke.WTSFreeMemory(pMemory.ToPointer());
            }
        }
    }
}
