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

    /// <summary>
    /// Specifies the connection state of a Remote Desktop Services session.
    /// </summary>
    /// <remarks>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtsapi32/ne-wtsapi32-wts_connectstate_class">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    public enum WTS_CONNECTSTATE_CLASS
    {
        /// <summary>
        /// A user is logged on to the WinStation. This state occurs when a user is signed in and actively connected to the device.
        /// </summary>
        WTSActive = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSActive,

        /// <summary>
        /// The WinStation is connected to the client.
        /// </summary>
        WTSConnected = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSConnected,

        /// <summary>
        /// The WinStation is in the process of connecting to the client.
        /// </summary>
        WTSConnectQuery = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSConnectQuery,

        /// <summary>
        /// The WinStation is shadowing another WinStation.
        /// </summary>
        WTSShadow = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSShadow,

        /// <summary>
        /// The WinStation is active but the client is disconnected. This state occurs when a user is signed in but not actively connected to the device, such as when the user has chosen to exit to the lock screen.
        /// </summary>
        WTSDisconnected = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSDisconnected,

        /// <summary>
        /// The WinStation is waiting for a client to connect.
        /// </summary>
        WTSIdle = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSIdle,

        /// <summary>
        /// The WinStation is listening for a connection. A listener session waits for requests for new client connections. No user is logged on a listener session. A listener session cannot be reset, shadowed, or changed to a regular client session.
        /// </summary>
        WTSListen = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSListen,

        /// <summary>
        /// The WinStation is being reset.
        /// </summary>
        WTSReset = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSReset,

        /// <summary>
        /// The WinStation is down due to an error.
        /// </summary>
        WTSDown = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSDown,

        /// <summary>
        /// The WinStation is initializing.
        /// </summary>
        WTSInit = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSInit,
    }
}
