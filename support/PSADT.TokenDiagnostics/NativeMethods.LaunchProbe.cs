using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace PSADT.TokenDiagnostics
{
    internal static partial class NativeMethods
    {
        /// <summary>
        /// Duplicates a validated owner token and launches only the fixed, immediately exiting Windows command.
        /// </summary>
        /// <param name="session">The independently resolved WTS owner snapshot.</param>
        /// <param name="sourceProcessId">The explicitly selected process whose token should be tested.</param>
        /// <exception cref="InvalidOperationException">A provenance or consistency check failed; recorded in the result.</exception>
        /// <exception cref="Win32Exception">A source process could not be opened; recorded in the result.</exception>
        internal static LaunchProbeResult RunLaunchProbe(SessionSnapshot session, int sourceProcessId)
        {
            TokenSnapshot? source = null;
            TokenSnapshot? duplicate = null;
            List<LaunchAttempt> attempts = [];
            string? error = null;
            try
            {
                using Process caller = Process.GetCurrentProcess();
                if (session.Sid is null || caller.SessionId != session.SessionId || ReadSession(session.SessionId) != session)
                {
                    throw new InvalidOperationException("The launch probe requires a stable, resolved WTS owner on the caller's own desktop.");
                }
                List<string> lsaErrors = [];
                LogonSnapshot[] references = [.. ReadLogons(session.SessionId, lsaErrors).Where(logon => string.Equals(logon.Sid, session.Sid, StringComparison.Ordinal) && logon.WinlogonFlagPresent && logon.LogonType is 2 or 10 or 11 or 12)];
                if (lsaErrors.Count is not 0 || references.Length is not 1)
                {
                    throw new InvalidOperationException("The probe requires exactly one Winlogon owner reference and complete LSA enumeration; ambiguous or incomplete evidence is refused.");
                }
                using SafeFileHandle process = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, checked((uint)sourceProcessId));
                if (process.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }
                CheckWin32(PInvoke.OpenProcessToken(process, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out SafeFileHandle sourceHandle));
                using (sourceHandle)
                {
                    source = ReadToken(sourceHandle);
                    ValidateProbeToken(session, references[0], source);
                    const TOKEN_ACCESS_MASK rights = TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE | TOKEN_ACCESS_MASK.TOKEN_ASSIGN_PRIMARY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_DEFAULT | TOKEN_ACCESS_MASK.TOKEN_ADJUST_SESSIONID;
                    CheckWin32(PInvoke.DuplicateTokenEx(sourceHandle, rights, lpTokenAttributes: null, SECURITY_IMPERSONATION_LEVEL.SecurityAnonymous, TOKEN_TYPE.TokenPrimary, out SafeFileHandle duplicateHandle));
                    using (duplicateHandle)
                    {
                        duplicate = ReadToken(duplicateHandle);
                        ValidateProbeToken(session, references[0], duplicate);
                        unsafe
                        {
                            CheckWin32(PInvoke.CreateEnvironmentBlock(out void* environment, duplicateHandle, bInherit: false));
                            try
                            {
                                LaunchAttempt first = LaunchProbeChild(duplicateHandle, environment, session, references[0], useCreateProcessAsUser: true);
                                attempts.Add(first);
                                if (first.ChildProcessId is null)
                                {
                                    attempts.Add(LaunchProbeChild(duplicateHandle, environment, session, references[0], useCreateProcessAsUser: false));
                                }
                            }
                            finally
                            {
                                CheckWin32(PInvoke.DestroyEnvironmentBlock(environment));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                error = DescribeError(ex);
            }
            return new(sourceProcessId, source, duplicate, attempts, error);
        }

        /// <summary>
        /// Requires a primary, medium-integrity, unelevated ordinary token tied to the unambiguous owner reference.
        /// </summary>
        /// <param name="session">The independent WTS owner snapshot.</param>
        /// <param name="reference">The independently enumerated Winlogon record.</param>
        /// <param name="token">The source, duplicate, or suspended child's token.</param>
        /// <exception cref="InvalidOperationException">The token does not satisfy the probe's conservative selection criteria.</exception>
        private static void ValidateProbeToken(SessionSnapshot session, LogonSnapshot reference, TokenSnapshot token)
        {
            if (token.SessionId != session.SessionId || !string.Equals(token.Sid, session.Sid, StringComparison.Ordinal)
                || !string.Equals(token.AuthenticationId, reference.AuthenticationId, StringComparison.Ordinal)
                || !string.Equals(token.TokenType, nameof(TOKEN_TYPE.TokenPrimary), StringComparison.Ordinal)
                || token.Elevated || token.Restricted || token.AppContainer || token.UIAccess
                || !string.Equals(token.IntegritySid, "S-1-16-8192", StringComparison.Ordinal)
                || token.Logon is not { WinlogonFlagPresent: true } logon || token.LogonError is not null
                || logon.SessionId != session.SessionId || !string.Equals(logon.Sid, session.Sid, StringComparison.Ordinal)
                || !string.Equals(logon.AuthenticationId, reference.AuthenticationId, StringComparison.Ordinal)
                || logon.LogonType is not (2 or 10 or 11 or 12))
            {
                throw new InvalidOperationException("Probe token refused: expected the WTS owner's original Winlogon LUID, matching token/LSA SID and desktop identifier, and a primary, unelevated, medium-integrity token without AppContainer, restricting SIDs, or UIAccess.");
            }
        }

        /// <summary>
        /// Creates a fixed suspended child, validates its token before resuming, and bounds its lifetime.
        /// </summary>
        /// <param name="token">The validated duplicate primary token.</param>
        /// <param name="environment">The separately created user environment block.</param>
        /// <param name="session">The WTS owner snapshot to recheck before launch and resume.</param>
        /// <param name="reference">The expected Winlogon authentication identity.</param>
        /// <param name="useCreateProcessAsUser">Whether to try CreateProcessAsUser rather than CreateProcessWithToken.</param>
        /// <exception cref="InvalidOperationException">The WTS owner changed; recorded in the attempt.</exception>
        /// <exception cref="Win32Exception">A native launch, resume, or wait failed; recorded in the attempt.</exception>
        /// <exception cref="TimeoutException">The child did not exit within ten seconds; recorded in the attempt.</exception>
        private static unsafe LaunchAttempt LaunchProbeChild(SafeHandle token, void* environment, SessionSnapshot session, LogonSnapshot reference, bool useCreateProcessAsUser)
        {
            string api = useCreateProcessAsUser ? "CreateProcessAsUser" : "CreateProcessWithToken";
            uint? childId = null;
            TokenSnapshot? childToken = null;
            uint? exitCode = null;
            string? error = null;
            string? cleanupError = null;
            try
            {
                if (ReadSession(session.SessionId) != session)
                {
                    throw new InvalidOperationException("The WTS owner snapshot changed before process creation.");
                }
                string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string executable = Path.Join(systemDirectory, "cmd.exe");
                Span<char> commandLine = $"\"{executable}\" /d /c exit 0\0".ToCharArray();
                STARTUPINFOW startup = new() { cb = checked((uint)sizeof(STARTUPINFOW)) };
                PROCESS_INFORMATION information;
                fixed (char* desktop = @"winsta0\default")
                {
                    startup.lpDesktop = new(desktop);
                    const PROCESS_CREATION_FLAGS flags = PROCESS_CREATION_FLAGS.CREATE_SUSPENDED | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT | PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                    if (useCreateProcessAsUser)
                    {
                        CheckWin32(PInvoke.CreateProcessAsUser(token, executable, ref commandLine, lpProcessAttributes: null, lpThreadAttributes: null, bInheritHandles: false, flags, environment, systemDirectory, in startup, out information));
                    }
                    else
                    {
                        CheckWin32(PInvoke.CreateProcessWithToken(token, CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE, executable, ref commandLine, flags, environment, systemDirectory, in startup, out information));
                    }
                }
                childId = information.dwProcessId;
                using SafeFileHandle process = new((nint)information.hProcess.Value, ownsHandle: true);
                using SafeFileHandle thread = new((nint)information.hThread.Value, ownsHandle: true);
                bool exited = false;
                try
                {
                    CheckWin32(PInvoke.OpenProcessToken(process, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle childHandle));
                    using (childHandle)
                    {
                        childToken = ReadToken(childHandle);
                        ValidateProbeToken(session, reference, childToken);
                    }
                    if (ReadSession(session.SessionId) != session)
                    {
                        throw new InvalidOperationException("The WTS owner snapshot changed before resuming the child.");
                    }
                    if (PInvoke.ResumeThread(thread) is uint.MaxValue)
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }
                    WAIT_EVENT wait = PInvoke.WaitForSingleObject(process, 10000);
                    if (wait is WAIT_EVENT.WAIT_FAILED)
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }
                    if (wait is not WAIT_EVENT.WAIT_OBJECT_0)
                    {
                        throw new TimeoutException("The fixed probe child did not exit within ten seconds.");
                    }
                    exited = true;
                    CheckWin32(PInvoke.GetExitCodeProcess(process, out uint actualExitCode));
                    exitCode = actualExitCode;
                }
                finally
                {
                    if (!exited)
                    {
                        try
                        {
                            CheckWin32(PInvoke.TerminateProcess(process, 1));
                            WAIT_EVENT cleanupWait = PInvoke.WaitForSingleObject(process, 2000);
                            if (cleanupWait is not WAIT_EVENT.WAIT_OBJECT_0)
                            {
                                cleanupError = $"Probe child termination wait returned {cleanupWait}.";
                            }
                        }
                        catch (Win32Exception ex)
                        {
                            cleanupError = DescribeError(ex);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or UnauthorizedAccessException or TimeoutException or System.Security.SecurityException)
            {
                error = DescribeError(ex);
            }
            return new(api, childId, childToken, exitCode, error, cleanupError);
        }
    }
}
