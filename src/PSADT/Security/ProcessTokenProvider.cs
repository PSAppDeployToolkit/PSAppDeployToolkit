using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop;
using PSADT.Interop.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;

namespace PSADT.Security
{
    /// <summary>
    /// Finds an ordinary desktop token without relying on a shell process name or a secondary logon.
    /// </summary>
    internal static class ProcessTokenProvider
    {
        /// <summary>
        /// Searches accessible processes and their linked tokens for the requested capabilities.
        /// </summary>
        /// <param name="sessionId">The requested desktop session.</param>
        /// <param name="expectedSid">The optional expected owner.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <param name="token">The owned validated token on success.</param>
        /// <returns>Whether acquisition succeeded.</returns>
        internal static bool TryGetToken(uint sessionId, SecurityIdentifier? expectedSid, ElevatedTokenType elevatedTokenType, bool uiAccess, [NotNullWhen(true)] out SafeFileHandle? token)
        {
            // Internal worker method for repeated calling if `ElevatedTokenType.HighestAvailable`.
            static bool TryGetTokenForElevation(Process[] processes, uint sessionId, ElevatedTokenType selection, bool uiAccess, ProcessTokenSession session, ProcessTokenReference reference, [NotNullWhen(true)] out SafeFileHandle? selectedToken)
            {
                foreach (Process process in processes)
                {
                    try
                    {
                        if (process.Id is not 0 && process.SessionId == sessionId && TryGetProcessToken(checked((uint)process.Id), session, reference, selection, uiAccess, out selectedToken))
                        {
                            return true;
                        }
                    }
                    catch (Exception ex) when (IsCandidateFailure(ex))
                    {
                        continue;
                        throw;
                    }
                }
                selectedToken = null;
                return false;
            }

            // The outer method is responsible for reading the session and logon reference, and for handling unexpected failures.
            token = null;
            try
            {
                // Read the session and validate the expected SID if provided.
                ProcessTokenSession session = ReadSession(sessionId);
                if (expectedSid is not null && !session.Sid.Equals(expectedSid))
                {
                    return false;
                }
                if (FindReference(session, ReadLogons()) is not ProcessTokenReference reference)
                {
                    return false;
                }

                // Cycle through each process, returning the first suitable token.
                Process[] processes = Process.GetProcesses();
                try
                {
                    return (elevatedTokenType is ElevatedTokenType.HighestAvailable && TryGetTokenForElevation(processes, sessionId, ElevatedTokenType.HighestMandatory, uiAccess, session, reference, out token)) || TryGetTokenForElevation(processes, sessionId, elevatedTokenType, uiAccess, session, reference, out token);
                }
                finally
                {
                    foreach (Process process in processes)
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                token?.Dispose();
                token = null;
                if (IsCandidateFailure(ex))
                {
                    return false;
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        /// <summary>
        /// Validates both sides of duplication and transfers ownership only after a stable reference check.
        /// </summary>
        /// <param name="source">The borrowed candidate token.</param>
        /// <param name="session">The independent desktop owner.</param>
        /// <param name="reference">The Winlogon reference, which may be a split pair.</param>
        /// <param name="readToken">Reads token suitability and provenance.</param>
        /// <param name="duplicateToken">Duplicates with the required primary-token rights.</param>
        /// <param name="referenceIsStable">Rechecks the desktop and logon reference.</param>
        /// <param name="result">The accepted owned duplicate on success, or null if validation fails.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <param name="linkedToken">Metadata read from the source's kernel-provided linked token.</param>
        /// <returns>Whether the duplicate passed all validation checks.</returns>
        internal static bool TryDuplicate(SafeHandle source, ProcessTokenSession session, ProcessTokenReference reference, Func<SafeHandle, ProcessTokenMetadata> readToken, Func<SafeHandle, SafeFileHandle> duplicateToken, Func<bool> referenceIsStable, [NotNullWhen(true)] out SafeFileHandle? result, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None, bool uiAccess = false, ProcessTokenMetadata? linkedToken = null)
        {
            // Duplication can add UIAccess but not remove it, so only the copy must match exactly.
            ProcessTokenMetadata original = readToken(source); if (!IsSuitable(session, reference, original, elevatedTokenType, original.UIAccess, linkedToken) || (original.UIAccess && !uiAccess))
            {
                result = null;
                return false;
            }
            SafeFileHandle duplicate = duplicateToken(source);
            bool accepted = false;
            try
            {
                if (!duplicate.IsInvalid && !duplicate.IsClosed)
                {
                    ProcessTokenMetadata copied = readToken(duplicate);
                    accepted = IsSameLogon(in copied.AuthenticationId, in original.AuthenticationId) && copied.ElevationType == original.ElevationType && IsSuitable(session, reference, copied, elevatedTokenType, uiAccess, linkedToken) && referenceIsStable();
                }
                if (accepted)
                {
                    result = duplicate;
                    return true;
                }
                result = null;
                return false;
            }
            finally
            {
                if (!accepted)
                {
                    duplicate.Dispose();
                }
            }
        }

        /// <summary>
        /// Finds the original interactive Winlogon logon belonging to the WTS owner, which may be a UAC split pair.
        /// </summary>
        /// <remarks>A split-token administrator has two indistinguishable records for one logon, so both are kept; a
        /// third record, or the same record twice, is evidence that cannot be reconciled and is refused.</remarks>
        /// <param name="session">The independently resolved session owner.</param>
        /// <param name="logons">The complete LSA enumeration.</param>
        /// <returns>The matching logon, or null for missing or ambiguous evidence.</returns>
        internal static ProcessTokenReference? FindReference(ProcessTokenSession session, IEnumerable<ProcessTokenLogon> logons)
        {
            ProcessTokenLogon? reference = null; ProcessTokenLogon? counterpart = null;
            foreach (ProcessTokenLogon logon in logons)
            {
                if (!IsReferenceCandidate(session, logon))
                {
                    continue;
                }
                if (counterpart is not null || logon == reference)
                {
                    return null;
                }
                if (reference is null)
                {
                    reference = logon;
                }
                else
                {
                    counterpart = logon;
                }
            }
            return reference is not null
                ? new(reference, counterpart)
                : null;
        }

        /// <summary>
        /// Determines whether one LSA record is an original interactive Winlogon logon for the session owner.
        /// </summary>
        /// <param name="session">The independently resolved session owner.</param>
        /// <param name="logon">The record to test.</param>
        /// <returns>Whether the record can serve as a logon reference.</returns>
        private static bool IsReferenceCandidate(ProcessTokenSession session, ProcessTokenLogon logon)
        {
            return logon.SessionId == session.SessionId && logon.Sid is not null && session.Sid.Equals(logon.Sid) && logon.UserFlags.HasFlag(Interop.MSV_SUB_AUTHENTICATION_FILTER.LOGON_WINLOGON)
                && logon.LogonType is SECURITY_LOGON_TYPE.Interactive or SECURITY_LOGON_TYPE.RemoteInteractive or SECURITY_LOGON_TYPE.CachedInteractive or SECURITY_LOGON_TYPE.CachedRemoteInteractive;
        }

        /// <summary>
        /// Requires original-logon provenance and a primary token matching the requested capabilities.
        /// </summary>
        /// <remarks>Nothing in the LSA records says the two halves of a split pair belong together, so a candidate must prove
        /// it by presenting the other half as its kernel-linked counterpart. A UIAccess token is accepted at high integrity as
        /// well as medium-plus, because Windows raises these processes to high while leaving the token unelevated.</remarks>
        /// <param name="session">The WTS owner reference.</param>
        /// <param name="reference">The original interactive logon, which may be a split pair.</param>
        /// <param name="token">The copied candidate metadata.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <param name="linkedToken">The kernel-linked counterpart, if queried.</param>
        /// <returns>Whether every provenance and suitability requirement is met.</returns>
        internal static bool IsSuitable(ProcessTokenSession session, ProcessTokenReference reference, ProcessTokenMetadata token, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None, bool uiAccess = false, ProcessTokenMetadata? linkedToken = null)
        {
            return session.SessionId is not 0
                && IsReferenceCandidate(session, reference.Logon)
                && (reference.Counterpart is null || IsReferenceCandidate(session, reference.Counterpart))
                && token.SessionId == session.SessionId
                && session.Sid.Equals(token.Sid)
                && IsSameLogon(in token.AuthenticationId, in token.Logon.AuthenticationId)
                && token.Logon.SessionId == session.SessionId
                && token.Logon.Sid is not null && session.Sid.Equals(token.Logon.Sid)
                && token.Logon.LogonType is SECURITY_LOGON_TYPE.Interactive or SECURITY_LOGON_TYPE.RemoteInteractive or SECURITY_LOGON_TYPE.CachedInteractive or SECURITY_LOGON_TYPE.CachedRemoteInteractive
                && (reference.Counterpart is null ? token.Logon == reference.Logon || IsLinkedCounterpart(session, reference.Logon, token, linkedToken) : reference.Includes(token.Logon) && IsLinkedCounterpart(session, token.Logon == reference.Logon ? reference.Counterpart : reference.Logon, token, linkedToken))
                && token.TokenType is TOKEN_TYPE.TokenPrimary
                && !token.Restricted && !token.AppContainer && token.UIAccess == uiAccess
                && (token.Elevated ? token.IntegritySid.Equals(HighIntegritySid) : token.IntegritySid.Equals(MediumIntegritySid) || (uiAccess && (token.IntegritySid.Equals(MediumPlusIntegritySid) || token.IntegritySid.Equals(HighIntegritySid))))
                && elevatedTokenType switch
                {
                    ElevatedTokenType.None => !token.Elevated,
                    ElevatedTokenType.HighestAvailable => token.Elevated || token.ElevationType is TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault,
                    ElevatedTokenType.HighestMandatory => token.Elevated,
                    _ => false,
                };
        }

        /// <summary>
        /// Determines whether a kernel-linked counterpart ties a candidate to the expected logon.
        /// </summary>
        /// <remarks>The counterpart is read for its metadata only and is never the token handed back, so its token type is not
        /// constrained. Without SeTcbPrivilege Windows returns a linked token as an identification-level impersonation token,
        /// which metadata can be read from but which cannot be duplicated into anything usable.</remarks>
        /// <param name="session">The WTS owner reference.</param>
        /// <param name="expected">The logon the counterpart must belong to.</param>
        /// <param name="token">The candidate metadata.</param>
        /// <param name="linkedToken">The kernel-linked counterpart, if queried.</param>
        /// <returns>Whether the counterpart ties the candidate to the expected logon.</returns>
        private static bool IsLinkedCounterpart(ProcessTokenSession session, ProcessTokenLogon expected, ProcessTokenMetadata token, ProcessTokenMetadata? linkedToken)
        {
            return linkedToken is not null
                && linkedToken.Logon == expected
                && IsSameLogon(in linkedToken.AuthenticationId, in expected.AuthenticationId)
                && linkedToken.SessionId == session.SessionId
                && session.Sid.Equals(linkedToken.Sid)
                && !linkedToken.Restricted && !linkedToken.AppContainer
                && ((token.ElevationType is TOKEN_ELEVATION_TYPE.TokenElevationTypeFull && linkedToken.ElevationType is TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited) || (token.ElevationType is TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited && linkedToken.ElevationType is TOKEN_ELEVATION_TYPE.TokenElevationTypeFull));
        }

        /// <summary>
        /// Copies a stable desktop identity without using a candidate process to determine its owner.
        /// </summary>
        /// <param name="sessionId">The requested desktop session.</param>
        /// <returns>The resolved owner and WTS logon time.</returns>
        /// <exception cref="InvalidOperationException">WTS returned incomplete or mismatched session data.</exception>
        internal static ProcessTokenSession ReadSession(uint sessionId)
        {
            _ = NativeMethods.WTSQuerySessionInformation(sessionId, WTS_INFO_CLASS.WTSSessionInfoEx, out SafeWtsHandle buffer);
            using (buffer)
            {
                ref readonly WTSINFOEXW info = ref buffer.AsReadOnlyStructure<WTSINFOEXW>();
                if (info.Level is not 1)
                {
                    throw new InvalidOperationException("WTS returned an unsupported session-information level.");
                }
                ref readonly WTSINFOEX_LEVEL1_W session = ref info.Data.WTSInfoExLevel1;
                string user = session.UserName.ToString();
                string domain = session.DomainName.ToString();
                if (sessionId is 0 || session.SessionId != sessionId || string.IsNullOrWhiteSpace(user) || session.LogonTime <= 0)
                {
                    throw new InvalidOperationException("WTS returned incomplete or mismatched desktop owner information.");
                }
                string account = string.IsNullOrWhiteSpace(domain) ? user : $"{domain}\\{user}";
                return new(sessionId, (SecurityIdentifier)new NTAccount(account).Translate(typeof(SecurityIdentifier)), session.LogonTime);
            }
        }

        /// <summary>
        /// Reads all LSA records; an inaccessible or vanished record invalidates the observation.
        /// </summary>
        /// <returns>The copied logon records.</returns>
        private static IEnumerable<ProcessTokenLogon> ReadLogons()
        {
            _ = NativeMethods.LsaEnumerateLogonSessions(out uint count, out SafeLsaFreeReturnBufferHandle? identifiers);
            using (identifiers)
            {
                if (identifiers is null)
                {
                    yield break;
                }
                int luidSize = Unsafe.SizeOf<LUID>();
                for (int index = 0; index < count; ++index)
                {
                    yield return ReadLogon(in identifiers.AsReadOnlyStructure<LUID>(checked(index * luidSize)));
                }
            }
        }

        /// <summary>
        /// Opens and validates one candidate while retaining ownership of its source handles.
        /// </summary>
        /// <param name="processId">The candidate process identifier.</param>
        /// <param name="session">The desktop owner reference.</param>
        /// <param name="reference">The original logon, which may be a split pair.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <param name="duplicate">The owned duplicate on success, or null if the candidate is unsuitable.</param>
        /// <returns>Whether a suitable primary token was obtained.</returns>
        private static bool TryGetProcessToken(uint processId, ProcessTokenSession session, ProcessTokenReference reference, ElevatedTokenType elevatedTokenType, bool uiAccess, [NotNullWhen(true)] out SafeFileHandle? duplicate)
        {
            using SafeFileHandle processHandle = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, processId);
            _ = NativeMethods.OpenProcessToken(processHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out SafeFileHandle token);
            using (token)
            {
                // A split pair is only ever proven through the counterpart, so the routes that have one go first and the
                // one that cannot is the fallback. An unsplit token has no counterpart to read and reaches it directly.
                ProcessTokenMetadata primary = ReadToken(token);
                if (primary.ElevationType is not TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                {
                    // Duplicating from the linked token needs the primary token that only SeTcbPrivilege gets you. For everyone
                    // else it arrives at identification level, so it can corroborate the pair but cannot itself be copied.
                    using SafeFileHandle linked = TokenManager.GetLinkedToken(token);
                    ProcessTokenMetadata linkedMetadata = ReadToken(linked);
                    if (TryGetCandidateToken(token, primary, session, reference, elevatedTokenType, uiAccess, linkedMetadata, out duplicate) || TryGetCandidateToken(linked, linkedMetadata, session, reference, elevatedTokenType, uiAccess, primary, out duplicate))
                    {
                        return true;
                    }
                }
                return TryGetCandidateToken(token, primary, session, reference, elevatedTokenType, uiAccess, linkedToken: null, out duplicate);
            }
        }

        /// <summary>
        /// Attempts one source independently so duplication failure does not prevent inspecting its counterpart.
        /// </summary>
        /// <param name="source">The borrowed token.</param>
        /// <param name="metadata">The source metadata.</param>
        /// <param name="session">The desktop owner.</param>
        /// <param name="reference">The original logon, which may be a split pair.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <param name="linkedToken">The kernel-linked counterpart.</param>
        /// <param name="duplicate">The validated duplicate.</param>
        /// <returns>Whether acquisition succeeded.</returns>
        private static bool TryGetCandidateToken(SafeHandle source, ProcessTokenMetadata metadata, ProcessTokenSession session, ProcessTokenReference reference, ElevatedTokenType elevatedTokenType, bool uiAccess, ProcessTokenMetadata? linkedToken, [NotNullWhen(true)] out SafeFileHandle? duplicate)
        {
            // Without SeTcbPrivilege, UIAccess can't be added, so only a source that already has it qualifies.
            if (uiAccess && !metadata.UIAccess && !CanSetUIAccess)
            {
                duplicate = null;
                return false;
            }
            try
            {
                return TryDuplicate(source, session, reference, ReadToken, handle => TokenManager.GetPrimaryToken(handle, uiAccess && !metadata.UIAccess), () => ReadSession(session.SessionId) == session && FindReference(session, ReadLogons()) == reference, out duplicate, elevatedTokenType, uiAccess, linkedToken);
            }
            catch (Exception ex) when (IsCandidateFailure(ex))
            {
                duplicate = null;
                return false;
            }
        }

        /// <summary>
        /// Copies one logon record while its native buffer remains owned.
        /// </summary>
        /// <param name="id">The authentication identifier.</param>
        /// <returns>The copied LSA record.</returns>
        private static ProcessTokenLogon ReadLogon(in LUID id)
        {
            _ = NativeMethods.LsaGetLogonSessionData(in id, out SafeLsaFreeReturnBufferHandle buffer);
            using (buffer)
            {
                ref readonly SECURITY_LOGON_SESSION_DATA data = ref buffer.AsReadOnlyStructure<SECURITY_LOGON_SESSION_DATA>();
                return new(in data.LogonId, data.Session, !data.Sid.IsNull ? data.Sid.ToSecurityIdentifier() : null, (SECURITY_LOGON_TYPE)data.LogonType, (Interop.MSV_SUB_AUTHENTICATION_FILTER)data.UserFlags, data.LogonTime);
            }
        }

        /// <summary>
        /// Copies candidate token metadata including its independent LSA record.
        /// </summary>
        /// <param name="token">The borrowed token handle.</param>
        /// <returns>The copied candidate metadata.</returns>
        internal static ProcessTokenMetadata ReadToken(SafeHandle token)
        {
            TOKEN_STATISTICS statistics = TokenUtilities.GetTokenInformation<TOKEN_STATISTICS>(token, TOKEN_INFORMATION_CLASS.TokenStatistics);
            _ = NativeMethods.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, TokenInformation: null, out uint length);
            Span<byte> integrity = stackalloc byte[checked((int)length)];
            _ = NativeMethods.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, integrity, out _);
            ref readonly TOKEN_MANDATORY_LABEL tokenLabel = ref integrity.AsReadOnlyStructure<TOKEN_MANDATORY_LABEL>();
            return new(TokenUtilities.GetTokenSid(token), TokenUtilities.GetTokenSessionId(token), statistics.AuthenticationId, statistics.TokenType,
                TokenUtilities.GetTokenInformation<TOKEN_ELEVATION>(token, TOKEN_INFORMATION_CLASS.TokenElevation).TokenIsElevated is not 0,
                tokenLabel.Label.Sid.ToSecurityIdentifier(), NativeMethods.IsTokenRestricted(token),
                TokenUtilities.GetTokenInformation<uint>(token, TOKEN_INFORMATION_CLASS.TokenIsAppContainer) is not 0,
                TokenUtilities.GetTokenInformation<uint>(token, TOKEN_INFORMATION_CLASS.TokenUIAccess) is not 0, ReadLogon(in statistics.AuthenticationId),
                TokenUtilities.GetTokenInformation<TOKEN_ELEVATION_TYPE>(token, TOKEN_INFORMATION_CLASS.TokenElevationType));
        }

        /// <summary>
        /// Identifies expected access, process-exit, and incomplete-observation failures.
        /// </summary>
        /// <param name="exception">The failure encountered during observation.</param>
        /// <returns>Whether the failure should decline a candidate or the fast path.</returns>
        private static bool IsCandidateFailure(Exception exception)
        {
            return exception is Win32Exception or InvalidOperationException or ArgumentException or UnauthorizedAccessException or SecurityException or IdentityNotMappedException;
        }

        /// <summary>
        /// Compares both parts of a native logon identifier.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns>Whether the identifiers match.</returns>
        private static bool IsSameLogon(in LUID left, in LUID right)
        {
            return left.HighPart == right.HighPart && left.LowPart == right.LowPart;
        }

        /// <summary>
        /// The medium mandatory integrity label.
        /// </summary>
        private static readonly SecurityIdentifier MediumIntegritySid = new("S-1-16-8192");

        /// <summary>
        /// The medium-plus mandatory integrity label used by UIAccess tokens.
        /// </summary>
        private static readonly SecurityIdentifier MediumPlusIntegritySid = new("S-1-16-8448");

        /// <summary>
        /// The high mandatory integrity label.
        /// </summary>
        private static readonly SecurityIdentifier HighIntegritySid = new("S-1-16-12288");

        /// <summary>
        /// Indicates whether the current execution context can set UIAccess on a duplicate, which requires SeTcbPrivilege.
        /// </summary>
        private static readonly bool CanSetUIAccess = PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeTcbPrivilege);
    }
}
