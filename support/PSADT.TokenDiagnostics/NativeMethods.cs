using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;

namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// Query-only wrappers for the diagnostic's generated Windows APIs.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Reads the WTS session owner without using a candidate token to determine their identity.
        /// </summary>
        /// <param name="sessionId">The desktop session to inspect.</param>
        /// <exception cref="InvalidOperationException">WTS returned incomplete or unsupported session data.</exception>
        internal static SessionSnapshot ReadSession(uint sessionId)
        {
            unsafe
            {
                CheckWin32(PInvoke.WTSQuerySessionInformation(HANDLE.WTS_CURRENT_SERVER_HANDLE, sessionId, WTS_INFO_CLASS.WTSSessionInfoEx, out PWSTR buffer, out uint length));
                try
                {
                    if (buffer.Value is null || length < sizeof(WTSINFOEXW))
                    {
                        throw new InvalidOperationException("WTS returned an incomplete session-information buffer.");
                    }
                    WTSINFOEXW info = *(WTSINFOEXW*)buffer.Value;
                    if (info.Level is not 1)
                    {
                        throw new InvalidOperationException("WTS returned an unsupported session-information level.");
                    }
                    WTSINFOEX_LEVEL1_W session = info.Data.WTSInfoExLevel1;
                    string user = session.UserName.ToString();
                    string domain = session.DomainName.ToString();
                    string account = string.IsNullOrWhiteSpace(domain) ? user : $"{domain}\\{user}";
                    string? sid = null;
                    string? sidError = null;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(user))
                        {
                            throw new InvalidOperationException("WTS reports no logged-on user for this session.");
                        }
                        sid = ((SecurityIdentifier)new NTAccount(account).Translate(typeof(SecurityIdentifier))).Value;
                    }
                    catch (Exception ex) when (ex is IdentityNotMappedException or InvalidOperationException or ArgumentException or System.Security.SecurityException)
                    {
                        sidError = DescribeError(ex);
                    }
                    return new(session.SessionId, account, sid, session.SessionState.ToString(), session.LogonTime, sidError);
                }
                finally
                {
                    PInvoke.WTSFreeMemory(buffer.Value);
                }
            }
        }

        /// <summary>
        /// Enumerates LSA records independently of process selection, reporting incomplete observations.
        /// </summary>
        /// <param name="sessionId">The desktop session whose LSA records should be included.</param>
        /// <param name="errors">Receives failures to query individual logon records.</param>
        /// <exception cref="InvalidOperationException">LSA returned an invalid enumeration buffer.</exception>
        internal static IReadOnlyList<LogonSnapshot> ReadLogons(uint sessionId, ICollection<string> errors)
        {
            List<LogonSnapshot> result = [];
            unsafe
            {
                CheckLsa(PInvoke.LsaEnumerateLogonSessions(out uint count, out LUID* ids), nameof(PInvoke.LsaEnumerateLogonSessions));
                using SafeLsaReturnBufferHandle buffer = new((nint)ids);
                if (count is not 0 && buffer.IsInvalid)
                {
                    throw new InvalidOperationException("LSA returned logon identifiers without a valid buffer.");
                }
                for (uint index = 0; index < count; ++index)
                {
                    try
                    {
                        LogonSnapshot logon = ReadLogon(ids[index]);
                        if (logon.SessionId == sessionId)
                        {
                            result.Add(logon);
                        }
                    }
                    catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException)
                    {
                        errors.Add($"{FormatLuid(ids[index])}: {DescribeError(ex)}");
                    }
                }
                GC.KeepAlive(buffer);
            }
            return result;
        }

        /// <summary>
        /// Opens a process token with query access only and independently inspects its linked token.
        /// </summary>
        /// <param name="processId">The process to inspect.</param>
        /// <param name="name">The previously observed process name.</param>
        /// <exception cref="Win32Exception">A process handle could not be opened; recorded in the snapshot.</exception>
        /// <exception cref="InvalidOperationException">A linked-token handle was invalid; recorded in the snapshot.</exception>
        internal static ProcessSnapshot ReadProcess(int processId, string name)
        {
            TokenSnapshot? token = null;
            TokenSnapshot? linked = null;
            string? error = null;
            string? linkedError = null;
            try
            {
                using SafeFileHandle process = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, (uint)processId);
                if (process.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }
                CheckWin32(PInvoke.OpenProcessToken(process, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle tokenHandle));
                using (tokenHandle)
                {
                    token = ReadToken(tokenHandle);
                    if (ReadValue<TOKEN_ELEVATION_TYPE>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType) is not TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                    {
                        try
                        {
                            TOKEN_LINKED_TOKEN link = ReadValue<TOKEN_LINKED_TOKEN>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenLinkedToken);
                            unsafe
                            {
                                using SafeFileHandle linkedHandle = new((nint)link.LinkedToken.Value, ownsHandle: true);
                                if (linkedHandle.IsInvalid)
                                {
                                    throw new InvalidOperationException("Windows returned an invalid linked-token handle.");
                                }
                                linked = ReadToken(linkedHandle);
                            }
                        }
                        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or UnauthorizedAccessException)
                        {
                            linkedError = DescribeError(ex);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or UnauthorizedAccessException)
            {
                error = DescribeError(ex);
            }
            return new(processId, name, token, linked, error, linkedError);
        }

        /// <summary>
        /// Copies token identity, elevation, integrity, and its independently queried LSA record.
        /// </summary>
        /// <param name="token">A token opened for querying.</param>
        private static TokenSnapshot ReadToken(SafeHandle token)
        {
            TOKEN_STATISTICS statistics = ReadValue<TOKEN_STATISTICS>(token, TOKEN_INFORMATION_CLASS.TokenStatistics);
            LogonSnapshot? logon = null;
            string? logonError = null;
            try
            {
                logon = ReadLogon(statistics.AuthenticationId);
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException)
            {
                logonError = DescribeError(ex);
            }
            return new(
                ReadSid(token, TOKEN_INFORMATION_CLASS.TokenUser),
                ReadValue<uint>(token, TOKEN_INFORMATION_CLASS.TokenSessionId),
                FormatLuid(statistics.AuthenticationId),
                FormatLuid(statistics.TokenId),
                statistics.TokenType.ToString(),
                ReadValue<TOKEN_ELEVATION_TYPE>(token, TOKEN_INFORMATION_CLASS.TokenElevationType).ToString(),
                ReadValue<TOKEN_ELEVATION>(token, TOKEN_INFORMATION_CLASS.TokenElevation).TokenIsElevated is not 0,
                ReadSid(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel),
                PInvoke.IsTokenRestricted(token),
                ReadValue<uint>(token, TOKEN_INFORMATION_CLASS.TokenIsAppContainer) is not 0,
                ReadValue<uint>(token, TOKEN_INFORMATION_CLASS.TokenUIAccess) is not 0,
                logon,
                logonError);
        }

        /// <summary>
        /// Reads fixed-size token information and verifies the returned length.
        /// </summary>
        /// <typeparam name="T">The native fixed-size structure.</typeparam>
        /// <param name="token">The token to query.</param>
        /// <param name="informationClass">The information structure to request.</param>
        /// <exception cref="InvalidOperationException">The returned structure length is unexpected.</exception>
        private static T ReadValue<T>(SafeHandle token, TOKEN_INFORMATION_CLASS informationClass) where T : unmanaged
        {
            Span<byte> bytes = stackalloc byte[Unsafe.SizeOf<T>()];
            CheckWin32(PInvoke.GetTokenInformation(token, informationClass, bytes, out uint length));
            return length == bytes.Length ? MemoryMarshal.Read<T>(bytes) : throw new InvalidOperationException($"Unexpected token information length for {informationClass}.");
        }

        /// <summary>
        /// Copies a user or integrity SID while the entire variable-length buffer remains pinned.
        /// </summary>
        /// <param name="token">The token to query.</param>
        /// <param name="informationClass">TokenUser or TokenIntegrityLevel.</param>
        /// <exception cref="Win32Exception">Windows could not determine the required buffer length.</exception>
        /// <exception cref="InvalidOperationException">The required buffer is too short for a SID descriptor.</exception>
        private static string ReadSid(SafeHandle token, TOKEN_INFORMATION_CLASS informationClass)
        {
            bool queried = PInvoke.GetTokenInformation(token, informationClass, [], out uint length);
            if (!queried && Marshal.GetLastPInvokeError() != (int)WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            if (length < Unsafe.SizeOf<SID_AND_ATTRIBUTES>())
            {
                throw new InvalidOperationException($"Unexpected SID information length for {informationClass}.");
            }
            byte[] bytes = new byte[checked((int)length)];
            unsafe
            {
                fixed (byte* buffer = bytes)
                {
                    CheckWin32(PInvoke.GetTokenInformation(token, informationClass, bytes, out _));
                    SID_AND_ATTRIBUTES* sid = (SID_AND_ATTRIBUTES*)buffer;
                    return new SecurityIdentifier((nint)sid->Sid.Value).Value;
                }
            }
        }

        /// <summary>
        /// Reads one LSA record; a missing provenance field is an error, not a false flag.
        /// </summary>
        /// <param name="id">The authentication LUID to query.</param>
        /// <exception cref="InvalidOperationException">LSA supplied no record or no UserFlags field.</exception>
        private static LogonSnapshot ReadLogon(LUID id)
        {
            unsafe
            {
                SECURITY_LOGON_SESSION_DATA* data = null;
                CheckLsa(PInvoke.LsaGetLogonSessionData(&id, &data), nameof(PInvoke.LsaGetLogonSessionData));
                using SafeLsaReturnBufferHandle buffer = new((nint)data);
                long requiredLength = Marshal.OffsetOf<SECURITY_LOGON_SESSION_DATA>(nameof(SECURITY_LOGON_SESSION_DATA.UserFlags)).ToInt64() + sizeof(uint);
                if (buffer.IsInvalid || data->Size < requiredLength)
                {
                    throw new InvalidOperationException("LSA returned no record or a record without the UserFlags field.");
                }
                LogonSnapshot snapshot = new(FormatLuid(data->LogonId), data->Session, data->Sid.Value is null ? null : new SecurityIdentifier((nint)data->Sid.Value).Value, data->LogonType, data->UserFlags, (data->UserFlags & 0x8000u) is not 0, data->LogonTime);
                GC.KeepAlive(buffer);
                return snapshot;
            }
        }

        /// <summary>
        /// Formats both halves of a LUID without signed conversion or culture-dependent output.
        /// </summary>
        /// <param name="id">The identifier to format.</param>
        private static string FormatLuid(LUID id)
        {
            return $"{unchecked((uint)id.HighPart):X8}:{id.LowPart:X8}";
        }

        /// <summary>
        /// Retains the native status alongside its Win32 translation.
        /// </summary>
        /// <param name="status">The NTSTATUS returned by LSA.</param>
        /// <param name="operation">The operation for diagnostic reporting.</param>
        /// <exception cref="Win32Exception">LSA reported failure.</exception>
        private static void CheckLsa(NTSTATUS status, string operation)
        {
            if (status.Value < 0)
            {
                throw new Win32Exception(checked((int)PInvoke.LsaNtStatusToWinError(status)), $"{operation} failed with NTSTATUS 0x{unchecked((uint)status.Value):X8}.");
            }
        }

        /// <summary>
        /// Converts a failed Win32 query to a diagnostic exception.
        /// </summary>
        /// <param name="succeeded">The immediately preceding query result.</param>
        /// <exception cref="Win32Exception">The native query reported failure.</exception>
        private static void CheckWin32(bool succeeded)
        {
            if (!succeeded)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }

        /// <summary>
        /// Includes native error codes without recording command lines or credentials.
        /// </summary>
        /// <param name="exception">The query failure to describe.</param>
        internal static string DescribeError(Exception exception)
        {
            return exception is Win32Exception native
                ? string.Create(CultureInfo.InvariantCulture, $"{exception.GetType().Name}: {exception.Message} (Win32 {native.NativeErrorCode})")
                : $"{exception.GetType().Name}: {exception.Message}";
        }
    }
}
