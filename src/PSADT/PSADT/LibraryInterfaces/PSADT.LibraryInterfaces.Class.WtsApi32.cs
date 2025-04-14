using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
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
        internal static unsafe BOOL WTSEnumerateSessions(HANDLE hServer, out SafeWTSHandle pSessionInfo, out uint pCount)
        {
            var res = PInvoke.WTSEnumerateSessions(hServer, 0, 1, out var ppSessionInfo, out pCount);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            pSessionInfo = new SafeWTSHandle(new IntPtr(ppSessionInfo), (int)pCount * sizeof(WTS_SESSION_INFOW), true);
            return res;
        }

        /// <summary>
        /// Wrapper around WTSQuerySessionInformation to manage error handling.
        /// </summary>
        /// <param name="hServer"></param>
        /// <param name="SessionId"></param>
        /// <param name="WTSInfoClass"></param>
        /// <param name="pBuffer"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL WTSQuerySessionInformation(HANDLE hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass, out SafeWTSHandle pBuffer)
        {
            var res = PInvoke.WTSQuerySessionInformation(hServer, SessionId, WTSInfoClass, out var ppBuffer, out uint bytesReturned);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            pBuffer = new SafeWTSHandle(new IntPtr(ppBuffer), (int)bytesReturned, true);
            return res;
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
