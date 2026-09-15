using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;

namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// Collects provenance evidence and optionally runs a fixed-command launch probe without invoking the toolkit.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Reused serializer settings for human-readable local reports.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Runs the diagnostic for the current desktop session or an explicitly supplied session.
        /// </summary>
        /// <param name="args">Optional --session and --probe-process arguments, or --help.</param>
        private static async Task<int> Main(string[] args)
        {
            if (args.Length is 1 && (string.Equals(args[0], "--help", StringComparison.Ordinal) || string.Equals(args[0], "-h", StringComparison.Ordinal)))
            {
                await Console.Out.WriteLineAsync("Usage: PSADT.TokenDiagnostics [--session <nonzero session ID>] [--probe-process <positive PID>]\nDefault: read-only JSON report.\nExplicit --probe-process: validate and duplicate that process's token, then launch only cmd.exe /d /c exit 0 in the caller's session. The child is inspected while suspended, then resumed with a bounded wait.\nNo broker, impersonation, explicit privilege enabling, ACL changes, or credential-based logons.\nReports contain account names, SIDs and process names; keep them private.").ConfigureAwait(false);
                return 0;
            }
            using Process currentProcess = Process.GetCurrentProcess();
            uint sessionId = checked((uint)currentProcess.SessionId);
            int? probeProcessId = null;
            bool sessionSpecified = false;
            bool invalidArguments = false;
            for (int index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length)
                {
                    invalidArguments = true;
                    break;
                }
                if (string.Equals(args[index], "--session", StringComparison.Ordinal) && !sessionSpecified && uint.TryParse(args[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out uint requestedSession))
                {
                    sessionId = requestedSession;
                    sessionSpecified = true;
                }
                else if (string.Equals(args[index], "--probe-process", StringComparison.Ordinal) && probeProcessId is null && int.TryParse(args[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int requestedProcess) && requestedProcess > 0)
                {
                    probeProcessId = requestedProcess;
                }
                else
                {
                    invalidArguments = true;
                    break;
                }
            }
            if (invalidArguments)
            {
                await Console.Error.WriteLineAsync("Expected optional --session <nonzero session ID> and --probe-process <positive PID>, each at most once; use --help for details.").ConfigureAwait(false);
                return 2;
            }
            if (sessionId is 0 or uint.MaxValue)
            {
                await Console.Error.WriteLineAsync("Select an interactive user's session; session zero and the no-session sentinel are not supported.").ConfigureAwait(false);
                return 2;
            }

            try
            {
                DateTimeOffset startedAt = DateTimeOffset.UtcNow;
                using WindowsIdentity identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
                ProcessSnapshot caller = NativeMethods.ReadProcess(currentProcess.Id, currentProcess.ProcessName);
                SessionSnapshot sessionBefore = NativeMethods.ReadSession(sessionId);
                List<string> lsaErrors = [];
                IReadOnlyList<LogonSnapshot> logons = NativeMethods.ReadLogons(sessionId, lsaErrors);
                List<string> enumerationErrors = [];
                List<ProcessSnapshot> processes = [];
                foreach (Process process in Process.GetProcesses())
                {
                    using (process)
                    {
                        try
                        {
                            if (process.SessionId == sessionId)
                            {
                                processes.Add(NativeMethods.ReadProcess(process.Id, process.ProcessName));
                            }
                        }
                        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or UnauthorizedAccessException or System.Security.SecurityException)
                        {
                            enumerationErrors.Add(NativeMethods.DescribeError(ex));
                        }
                    }
                }
                LaunchProbeResult? launchProbe = probeProcessId is int sourceProcessId ? NativeMethods.RunLaunchProbe(sessionBefore, sourceProcessId) : null;
                SessionSnapshot sessionAfter = NativeMethods.ReadSession(sessionId);
                TokenSnapshot[] tokens = [.. processes.SelectMany(static process => new[] { process.Token, process.LinkedToken }).OfType<TokenSnapshot>()];
                bool ownerStable = sessionBefore == sessionAfter && sessionBefore.Sid is not null;
                bool? callerMatchesOwner = sessionBefore.Sid is null || caller.Token is null ? null : string.Equals(caller.Token.Sid, sessionBefore.Sid, StringComparison.Ordinal);
                var report = new
                {
                    SchemaVersion = 2,
                    StartedAtUtc = startedAt,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    OperatingSystem = Environment.OSVersion.VersionString,
                    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    ReadOnly = probeProcessId is null,
                    LaunchProbe = launchProbe,
                    CallerAccount = identity.Name,
                    Caller = caller,
                    SessionBefore = sessionBefore,
                    SessionAfter = sessionAfter,
                    SessionOwnerUnchangedDuringSnapshot = ownerStable,
                    CallerSidMatchesSessionOwner = callerMatchesOwner,
                    LogonTypeLegend = "2=Interactive, 3=Network, 4=Batch, 5=Service, 7=Unlock, 8=NetworkCleartext, 9=NewCredentials, 10=RemoteInteractive, 11=CachedInteractive, 12=CachedRemoteInteractive, 13=CachedUnlock",
                    WinlogonFlagMask = "0x00008000",
                    Logons = logons,
                    LsaQueryErrors = lsaErrors,
                    ProcessEnumerationErrors = enumerationErrors,
                    Summary = new
                    {
                        ProcessesObserved = processes.Count,
                        ProcessTokenQueryFailures = processes.Count(static process => process.Token is null),
                        LinkedTokenQueryFailures = processes.Count(static process => process.LinkedTokenError is not null),
                        OwnerPrimaryTokens = processes.Count(process => sessionBefore.Sid is not null && process.Token is not null && string.Equals(process.Token.Sid, sessionBefore.Sid, StringComparison.Ordinal) && process.Token.SessionId == sessionId),
                        OtherAccountPrimaryTokens = processes.Count(process => sessionBefore.Sid is not null && process.Token is not null && !string.Equals(process.Token.Sid, sessionBefore.Sid, StringComparison.Ordinal)),
                        OwnerLogonsWithWinlogonFlag = logons.Count(logon => sessionBefore.Sid is not null && string.Equals(logon.Sid, sessionBefore.Sid, StringComparison.Ordinal) && logon.WinlogonFlagPresent),
                        OwnerTokenObservationsWithWinlogonFlag = tokens.Count(token => sessionBefore.Sid is not null && string.Equals(token.Sid, sessionBefore.Sid, StringComparison.Ordinal) && token.SessionId == sessionId && token.Logon is { WinlogonFlagPresent: true } logon && string.Equals(logon.Sid, token.Sid, StringComparison.Ordinal) && logon.SessionId == token.SessionId),
                    },
                    Processes = processes.OrderBy(static process => process.ProcessId).ToArray(),
                    Limitations = new[]
                    {
                        "An observed LOGON_WINLOGON flag is provenance evidence, not a safe-token verdict or proof of a uniquely identified original logon.",
                        "The summary includes linked-token observations and may count the same token/logon more than once.",
                        "Process, WTS and LSA queries are not an atomic snapshot; inaccessible or exiting processes are reported, not treated as matches.",
                        probeProcessId is null ? "No token duplication, primary-token assignment, environment creation or process launch is attempted." : "Explicit probe mode attempts duplication and a fixed harmless child launch; this does not validate the toolkit's integrated launch path or cross-session launches.",
                        "Absent scenarios (same-account runas, netonly, RDP, other account providers) are not validated by this run.",
                    },
                };
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(report, JsonOptions)).ConfigureAwait(false);
                return ownerStable && caller.Token is not null && (launchProbe?.Succeeded ?? true) ? 0 : 1;
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                await Console.Error.WriteLineAsync(NativeMethods.DescribeError(ex)).ConfigureAwait(false);
                return 1;
            }
        }
    }
}
