using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides methods for interacting with Remote Desktop Session Host (RDSH) sessions, including querying session
    /// information.
    /// </summary>
    /// <remarks>This class serves as a wrapper for native Windows APIs related to session management, such as
    /// querying session details using the <c>WinStationQueryInformationW</c> function from the <c>winsta.dll</c>
    /// library. It is intended for use in scenarios where detailed information about RDSH sessions is required, such as
    /// session state, user details, or performance metrics. <para> The methods in this class are designed for advanced
    /// use cases and require proper handling of unmanaged resources. Callers must ensure that any allocated memory or
    /// handles are properly managed to avoid resource leaks. </para> <para> This class is not intended for
    /// general-purpose use and is typically used in administrative or diagnostic tools that interact with Remote
    /// Desktop Services. </para></remarks>
    internal static class WinSta
    {
        /// <summary>
        /// Represents a structure containing information about a user token and the associated process and thread IDs.
        /// </summary>
        /// <remarks>This structure is used to encapsulate the process ID, thread ID, and user token
        /// handle for a specific user session.</remarks>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct WINSTATIONUSERTOKEN
        {
            /// <summary>
            /// Represents the handle to the process identifier (PID) of a system process.
            /// </summary>
            /// <remarks>This handle is typically used to uniquely identify a process in the system. 
            /// The value is read-only and cannot be modified after initialization.</remarks>
            internal readonly HANDLE ProcessId;

            /// <summary>
            /// Represents the handle of the thread associated with this instance.
            /// </summary>
            /// <remarks>This handle uniquely identifies a thread within the operating system. It is
            /// typically used for thread management or interoperation with native APIs.</remarks>
            internal readonly HANDLE ThreadId;

            /// <summary>
            /// Represents the user token associated with the current operation or context.
            /// </summary>
            /// <remarks>This token is typically used to identify or authenticate a user in scenarios
            /// where user-specific operations are performed. The value is read-only and cannot be modified after
            /// initialization.</remarks>
            internal readonly HANDLE UserToken;
        }

        /// <summary>
        /// Queries information about a specific session on a Remote Desktop Session Host (RDSH) server.
        /// </summary>
        /// <remarks>This method wraps the native <c>WinStationQueryInformationW</c> function from the
        /// <c>winsta.dll</c> library. It is used to retrieve various types of information about a session, such as
        /// session state, user details, or performance metrics. <para> The caller is responsible for ensuring that the
        /// <paramref name="pWinStationInformation"/> handle is properly allocated and released. If the operation fails,
        /// an exception is thrown with details about the error. </para> <exception
        /// cref="System.ComponentModel.Win32Exception"> Thrown if the underlying native call fails. The exception
        /// contains the error code returned by the operating system. </exception> <exception
        /// cref="ArgumentNullException"> Thrown if <paramref name="pWinStationInformation"/> is <see langword="null"/>.
        /// </exception></remarks>
        /// <param name="hServer">A handle to the server on which the session resides. This handle must be valid and obtained through
        /// appropriate API calls.</param>
        /// <param name="LogonId">The logon ID of the session for which information is being queried.</param>
        /// <param name="WinStationInformationClass">The type of information to query, specified as a value from the <see cref="WINSTATIONINFOCLASS"/>
        /// enumeration.</param>
        /// <param name="pWinStationInformation">A <see cref="SafeMemoryHandle"/> that receives the requested session information. The caller must ensure
        /// that the handle is properly allocated and has sufficient capacity to store the requested data.</param>
        /// <param name="pReturnLength">When the method returns, contains the size, in bytes, of the data written to <paramref
        /// name="pWinStationInformation"/>.</param>
        /// <returns><see langword="true"/> if the query operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOLEAN WinStationQueryInformation(HANDLE hServer, uint LogonId, WINSTATIONINFOCLASS WinStationInformationClass, Span<byte> pWinStationInformation, out uint pReturnLength)
        {
            BOOLEAN res;
            unsafe
            {
                fixed (byte* pWinStationInformationPtr = pWinStationInformation)
                {
                    [DllImport("winsta.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                    static extern BOOLEAN WinStationQueryInformationW(HANDLE hServer, uint LogonId, WINSTATIONINFOCLASS WinStationInformationClass, void* pWinStationInformation, uint WinStationInformationLength, out uint pReturnLength);
                    res = WinStationQueryInformationW(hServer, LogonId, WinStationInformationClass, pWinStationInformationPtr, (uint)pWinStationInformation.Length, out pReturnLength);
                }
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves the user access token associated with the specified session.
        /// Refer to https://medium.com/@omribaso/wts-api-wasteland-remote-token-impersonation-in-another-level-a23965e8227e for details.
        /// </summary>
        /// <param name="sessionId">The identifier of the session for which to retrieve the user token.</param>
        /// <param name="hUserToken">When this method returns, contains a SafeFileHandle representing the user token for the specified session.</param>
        /// <exception cref="InvalidOperationException">Thrown if the user token cannot be retrieved for the specified session.</exception>
        internal static void GetUserTokenForSession(uint sessionId, out SafeFileHandle hUserToken)
        {
            HANDLE hUserTokenLocal = default;
            unsafe
            {
                [DllImport("winsta.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                static extern void GetUserTokenForSession(uint sessionId, HANDLE* hUserToken);
                GetUserTokenForSession(sessionId, &hUserTokenLocal);
            }
            if (hUserTokenLocal == default || hUserTokenLocal.IsNull)
            {
                throw new InvalidOperationException("Failed to retrieve user token for session.");
            }
            hUserToken = new(hUserTokenLocal, true);
        }
    }

    /// <summary>
    /// Represents the various types of information that can be queried or set for a Windows Station (WinStation).
    /// </summary>
    /// <remarks>This enumeration defines the different classes of information that can be used to interact
    /// with a WinStation. Each value corresponds to a specific category of data, such as configuration, client
    /// information, or session state. These values are typically used in conjunction with system APIs that manage or
    /// query WinStation details.</remarks>
    internal enum WINSTATIONINFOCLASS : uint
    {
        WinStationCreateData,
        WinStationConfiguration,
        WinStationPdParams,
        WinStationWd,
        WinStationPd,
        WinStationPrinter,
        WinStationClient,
        WinStationModules,
        WinStationInformation,
        WinStationTrace,
        WinStationBeep,
        WinStationEncryptionOff,
        WinStationEncryptionPerm,
        WinStationNtSecurity,
        WinStationUserToken,
        WinStationUnused1,
        WinStationVideoData,
        WinStationInitialProgram,
        WinStationCd,
        WinStationSystemTrace,
        WinStationVirtualData,
        WinStationClientData,
        WinStationSecureDesktopEnter,
        WinStationSecureDesktopExit,
        WinStationLoadBalanceSessionTarget,
        WinStationLoadIndicator,
        WinStationShadowInfo,
        WinStationDigProductId,
        WinStationLockedState,
        WinStationRemoteAddress,
        WinStationIdleTime,
        WinStationLastReconnectType,
        WinStationDisallowAutoReconnect,
        WinStationUnused2,
        WinStationUnused3,
        WinStationUnused4,
        WinStationUnused5,
        WinStationReconnectedFromId,
        WinStationEffectsPolicy,
        WinStationType,
        WinStationInformationEx,
    }
}
