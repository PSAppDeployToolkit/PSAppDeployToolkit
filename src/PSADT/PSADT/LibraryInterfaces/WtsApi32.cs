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
        /// Enumerates processes running on a specified server and retrieves detailed process information.
        /// </summary>
        /// <remarks>This method wraps the Windows API function <c>WTSEnumerateProcessesEx</c> and
        /// provides a managed interface for enumerating processes. The level of detail returned is determined by the
        /// <paramref name="pLevel"/> parameter: <list type="bullet"> <item><description>Level 0 returns basic process
        /// information (<c>WTS_PROCESS_INFOW</c>).</description></item> <item><description>Level 1 returns extended
        /// process information (<c>WTS_PROCESS_INFO_EXW</c>).</description></item> </list> The <paramref
        /// name="pProcessInfo"/> parameter must be disposed to avoid memory leaks.</remarks>
        /// <param name="hServer">A handle to the server on which the processes are to be enumerated. Use <see langword="null"/> to specify
        /// the local server.</param>
        /// <param name="pLevel">The level of detail for the process information. Use 0 for basic information or 1 for extended information.</param>
        /// <param name="SessionId">The session ID for which processes are to be enumerated. Use 0 to enumerate processes for all sessions.</param>
        /// <param name="pProcessInfo">When the method returns, contains a <see cref="SafeWtsExHandle"/> object that holds the enumerated process
        /// information. The caller is responsible for disposing of this handle to release the allocated resources.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL WTSEnumerateProcessesEx(HANDLE hServer, uint pLevel, uint SessionId, out SafeWtsExHandle pProcessInfo)
        {
            if (pLevel > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pLevel), "pLevel must be 0 or 1.");
            }
            BOOL res;
            unsafe
            {
                PWSTR ppProcessInfo; uint pCount;
                res = PInvoke.WTSEnumerateProcessesEx(hServer, &pLevel, SessionId, &ppProcessInfo, &pCount);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                if (pLevel > 0)
                {
                    pProcessInfo = new((IntPtr)ppProcessInfo.Value, WTS_TYPE_CLASS.WTSTypeProcessInfoLevel1, (int)pCount * sizeof(WTS_PROCESS_INFO_EXW), true);
                }
                else
                {
                    pProcessInfo = new((IntPtr)ppProcessInfo.Value, WTS_TYPE_CLASS.WTSTypeProcessInfoLevel0, (int)pCount * sizeof(WTS_PROCESS_INFOW), true);
                }
            }
            return res;
        }

        /// <summary>
        /// Enumerates the sessions on the specified Remote Desktop Session Host (RD Session Host) server.
        /// </summary>
        /// <remarks>If the method returns <see langword="false"/>, an exception is thrown containing the
        /// relevant Win32 error information. The returned SafeWtsHandle must be disposed to free the associated
        /// unmanaged resources.</remarks>
        /// <param name="hServer">A handle to an RD Session Host server. This handle must be opened with appropriate access rights.</param>
        /// <param name="pSessionInfo">When this method returns, contains a SafeWtsHandle that encapsulates the session information buffer. The
        /// caller is responsible for releasing the handle when it is no longer needed.</param>
        /// <returns>A value that is <see langword="true"/> if the session enumeration succeeds; otherwise, <see
        /// langword="false"/>.</returns>
        internal static BOOL WTSEnumerateSessions(HANDLE hServer, out SafeWtsHandle pSessionInfo)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.WTSEnumerateSessions(hServer, 0, 1, out var ppSessionInfo, out var pCount);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                pSessionInfo = new((IntPtr)ppSessionInfo, (int)pCount * sizeof(WTS_SESSION_INFOW), true);
            }
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
        internal static BOOL WTSQuerySessionInformation(HANDLE hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass, out SafeWtsHandle pBuffer)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.WTSQuerySessionInformation(hServer, SessionId, WTSInfoClass, out var ppBuffer, out uint bytesReturned);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                pBuffer = new(new(ppBuffer), (int)bytesReturned, true);
            }
            return res;
        }

        /// <summary>
        /// Retrieves the primary access token of the user associated with the specified Remote Desktop Services
        /// session.
        /// </summary>
        /// <remarks>This method throws an exception if the underlying native call fails. The returned
        /// token can be used to impersonate the user or to launch processes in the user's context.</remarks>
        /// <param name="SessionId">The identifier of the Remote Desktop Services session for which to retrieve the user token.</param>
        /// <param name="phToken">When this method returns, contains a handle to the primary token of the user associated with the specified
        /// session. The caller is responsible for releasing the handle.</param>
        /// <returns>A value that indicates whether the operation succeeded. Returns <see langword="true"/> if the token was
        /// retrieved successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL WTSQueryUserToken(uint SessionId, out SafeFileHandle phToken)
        {
            BOOL res;
            unsafe
            {
                HANDLE phTokenLocal;
                res = PInvoke.WTSQueryUserToken(SessionId, &phTokenLocal);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                phToken = new(phTokenLocal, true);
            }
            return res;
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

    /// <summary>
    /// WTS protocol types.
    /// </summary>
    public enum WTS_PROTOCOL_TYPE : uint
    {
        /// <summary>
        /// Represents the console protocol type used in remote desktop services.
        /// </summary>
        /// <remarks>This value corresponds to the console session protocol type defined in the Windows
        /// Terminal Services API. It is typically used to identify a local console session.</remarks>
        Console = PInvoke.WTS_PROTOCOL_TYPE_CONSOLE,

        /// <summary>
        /// Represents the Independent Computing Architecture (ICA) protocol type.
        /// </summary>
        /// <remarks>The ICA protocol is commonly used for remote desktop and application virtualization
        /// scenarios. This value corresponds to the <see cref="PInvoke.WTS_PROTOCOL_TYPE_ICA"/> constant.</remarks>
        ICA = PInvoke.WTS_PROTOCOL_TYPE_ICA,

        /// <summary>
        /// Represents the Remote Desktop Protocol (RDP) protocol type.
        /// </summary>
        /// <remarks>This value corresponds to the RDP protocol type as defined in the Windows Terminal
        /// Services API. It is used to identify connections that utilize the Remote Desktop Protocol.</remarks>
        RDP = PInvoke.WTS_PROTOCOL_TYPE_RDP,
    }
}
