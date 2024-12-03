using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.PE;
using PSADT.PathEx;
using PSADT.PInvoke;
using PSADT.Logging;
using PSADT.WTSSession;
using PSADT.AccessToken;
using PSADT.Diagnostics.Exceptions;


namespace PSADT.ProcessEx
{
    /// <summary>
    /// Manages the execution and monitoring of processes across different user sessions.
    /// </summary>
    public class StartProcess : IDisposable
    {
        private readonly ExecutionManager _executionManager;
        private bool _disposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="StartProcess"/> class.
        /// </summary>
        public StartProcess()
        {
            _executionManager = new ExecutionManager();
        }

        /// <summary>
        /// Launches and monitors processes based on the provided options.
        /// </summary>
        /// <param name="options">The launch options for the processes.</param>
        /// <returns>An exit code indicating the result of the operation.</returns>
        public async Task<int> ExecuteAndMonitorAsync(LaunchOptions options)
        {
            try
            {
                options.FilePath = PathHelper.ResolveExecutableFullPath(options.FilePath) ?? throw new InvalidOperationException("Resolved file path is null or empty.");

                UnifiedLogger.Create().Message($"Executing process [{options.FilePath}].").Severity(LogLevel.Information);

                if (options.AllActiveUserSessions)
                {
                    UnifiedLogger.Create().Message("Executing process in all active user sessions.").Severity(LogLevel.Information);
                    await ExecuteInAllActiveSessionsAsync(options);
                }
                else if (options.PrimaryActiveUserSession)
                {
                    UnifiedLogger.Create().Message("Executing process in primary active user session.").Severity(LogLevel.Information);
                    await ExecuteInPrimaryActiveSessionAsync(options);
                }
                else if (options.SessionId.HasValue)
                {
                    UnifiedLogger.Create().Message($"Executing process in specified sessionwith id [{options.SessionId.Value}].").Severity(LogLevel.Information);
                    await ExecuteProcessInSessionAsync(options);
                }
                else
                {
                    options.SessionId = SessionManager.GetCurrentProcessSessionId();
                    options.UpdateUsername(@$"{Environment.UserDomainName}\{Environment.UserName}");
                    UnifiedLogger.Create().Message($"Executing process in current session with id [{options.SessionId.Value}].").Severity(LogLevel.Information);
                    await ExecuteProcessInCurrentSessionAsync(options);
                }

                if (options.Wait)
                {
                    return await WaitForProcessesAsync(options);
                }

                return 0;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Process execution failed: {ex.Message}").Error(ex);
                return 60103; // ProcessEx execution failed
            }
            finally
            {
                _executionManager.Clear();
            }
        }

        private async Task<int> WaitForProcessesAsync(LaunchOptions options)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(options.ConsoleTimeoutInSeconds);
            using var waitForProcessCancellationTokenSource = new CancellationTokenSource(timeout);

            try
            {
                ExecutionResult exitInfo = await _executionManager.WaitForAllProcessExitsAsync(options.WaitOption,
                                                                                               timeout,
                                                                                               waitForProcessCancellationTokenSource.Token);

                if (exitInfo.HasTimedOut)
                {
                    UnifiedLogger.Create().Message("Timeout reached. Stopping redirection monitors and terminating processes.").Severity(LogLevel.Information);
                    await _executionManager.StopAllRedirectionMonitorsAsync(options.TerminateOnTimeout);
                }

                await _executionManager.WaitForAllRedirectionMonitorsAsync();

                if (options.Debug && exitInfo.ExitedProcessInfo != null)
                {
                    foreach (ExecutionDetails process in exitInfo.ExitedProcessInfo)
                    {
                        UnifiedLogger.Create().Message($"Process [{process.ProcessName}] with process id [{process.ProcessId}] started in session id [{process.SessionId}] as user [{process.Username}] returned with exit code [{process.ExitCode}].").Severity(LogLevel.Debug);
                    }
                }

                if (exitInfo?.ExitedProcessInfo?.Any(p => options?.SuccessExitCodes?.Contains(p.ExitCode) == false) == true)
                {
                    return 60102; // At least one process exited with a non-success code
                }

                return 0;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error while waiting for processes:{Environment.NewLine}{ex.Message}").Error(ex);
                return 60104; // Error during process wait
            }
        }

        /// <summary>
        /// Launches the process in all active user sessions.
        /// </summary>
        /// <param name="options">The launch options for the processes.</param>
        private async Task ExecuteInAllActiveSessionsAsync(LaunchOptions options)
        {
            List<SessionInfo>? sessions = SessionManager.GetAllActiveUserSessions();
            if (sessions != null && sessions.Count != 0)
            {
                foreach (SessionInfo session in sessions)
                {
                    options.SessionId = session.SessionId;
                    try
                    {
                        options.UpdateUsername(SessionManager.GetWtsUsernameAndDomainById(session.SessionId));
                        await ExecuteProcessInSessionAsync(options);
                    }
                    catch (Exception ex)
                    {
                        UnifiedLogger.Create().Message($"Failed to execute process in session {session.SessionId}:{Environment.NewLine}{ex.Message}").Error(ex);
                        // Decide whether to continue with other sessions or throw
                        // For now, we'll log the error and continue with other sessions
                    }
                }
            }
            else
            {
                UnifiedLogger.Create().Message("No active user sessions found.").Severity(LogLevel.Warning);
                throw new InvalidOperationException("No active user sessions found.");
            }
        }

        /// <summary>
        /// Launches the process in the primary active user session.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        private async Task ExecuteInPrimaryActiveSessionAsync(LaunchOptions options)
        {
            SessionInfo? session = SessionManager.GetPrimaryActiveUserSession();
            if (session != null)
            {
                options.SessionId = session.SessionId;
                try
                {
                    options.UpdateUsername(SessionManager.GetWtsUsernameAndDomainById(session.SessionId));
                    await ExecuteProcessInSessionAsync(options);
                }
                catch (Exception ex)
                {
                    UnifiedLogger.Create().Message($"Failed to execute process in primary active session:{Environment.NewLine}{ex.Message}").Error(ex);
                    throw;
                }
            }
            else
            {
                UnifiedLogger.Create().Message("No primary active user session found.").Severity(LogLevel.Warning);
                throw new InvalidOperationException("No primary active user session found.");
            }
        }

        /// <summary>
        /// Launches a process in a specific session.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        private async Task ExecuteProcessInSessionAsync(LaunchOptions options)
        {
            if (!options.SessionId.HasValue)
            {
                throw new ArgumentException("SessionId must be specified.", nameof(options));
            }

            try
            {
                ManagedProcess managedProcess = _executionManager.InitializeManagedProcess(new SessionDetails(options.SessionId.Value, options.Username));
                managedProcess.IsGuiApplication = ExecutableType.IsGuiApplication(options.FilePath);
                options.IsGuiApplication = managedProcess.IsGuiApplication;

                ProcessStartInfo startInfo = CreateProcessStartInfo(options);

                managedProcess.RedirectStandardOutput = startInfo.RedirectStandardOutput;
                managedProcess.RedirectStandardError = startInfo.RedirectStandardError;
                managedProcess.MergeStdErrAndStdOut = options.MergeStdErrAndStdOut;

                managedProcess.Process = await StartProcessInSessionAsync(
                    options.SessionId.Value,
                    startInfo,
                    options.CreateProcessCreationFlags(),
                    options.UseLinkedAdminToken,
                    options.InheritEnvironmentVariables ?? false,
                    options.IsGuiApplication
                );

                if (managedProcess.Process == null)
                {
                    throw new InvalidOperationException($"Failed to start process in session [{options.SessionId.Value}].");
                }

                UnifiedLogger.Create().Message($"Started process [{managedProcess.Process.ProcessName}] with process id [{managedProcess.Process.Id}] in session id [{options.SessionId.Value}] as user [{options.Username}].").Severity(LogLevel.Information);

                //managedProcess.ConfigureOutputRedirection(options);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error executing process in session [{options.SessionId.Value}]:{Environment.NewLine}{ex.Message}").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Launches a process in the current session.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        private async Task ExecuteProcessInCurrentSessionAsync(LaunchOptions options)
        {
            try
            {
                if (!options.SessionId.HasValue)
                {
                    options.SessionId = SessionManager.GetCurrentProcessSessionId();
                }

                if (string.IsNullOrWhiteSpace(options.Username))
                {
                    options.UpdateUsername($@"{Environment.UserDomainName}\{Environment.UserName}");
                }

                UnifiedLogger.Create().Message($"Executing process in current session with id [{options.SessionId.Value}].").Severity(LogLevel.Information);

                ManagedProcess managedProcess = _executionManager.InitializeManagedProcess(new SessionDetails(options.SessionId.Value, options.Username));
                managedProcess.IsGuiApplication = ExecutableType.IsGuiApplication(options.FilePath);
                options.IsGuiApplication = managedProcess.IsGuiApplication;

                ProcessStartInfo startInfo = CreateProcessStartInfo(options);

                managedProcess.RedirectStandardOutput = startInfo.RedirectStandardOutput;
                managedProcess.RedirectStandardError = startInfo.RedirectStandardError;
                managedProcess.MergeStdErrAndStdOut = options.MergeStdErrAndStdOut;

                managedProcess.Process = await StartProcessAsync(startInfo, options.IsGuiApplication);

                if (managedProcess.Process == null)
                {
                    throw new InvalidOperationException($"Failed to start process in current session with id [{options.SessionId.Value}].");
                }

                UnifiedLogger.Create().Message($"Started process [{managedProcess.Process.ProcessName}] with process id [{managedProcess.Process.Id}] in session id [{options.SessionId.Value}] as user [{options.Username}].").Severity(LogLevel.Information);

                //managedProcess.ConfigureOutputRedirection(options);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error executing process in current session:{Environment.NewLine}{ex.Message}").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a ProcessStartInfo object based on the provided options.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        /// <returns>A ProcessStartInfo object configured with the provided options.</returns>
        private static ProcessStartInfo CreateProcessStartInfo(LaunchOptions options)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = options.FilePath,
                    Arguments = string.Join(" ", options.ArgumentList),
                    WorkingDirectory = options.WorkingDirectory ?? Path.GetDirectoryName(Environment.CurrentDirectory),
                    UseShellExecute = false,
                    CreateNoWindow = options.HideWindow,
                    RedirectStandardOutput = options.RedirectOutput && !options.IsGuiApplication,
                    RedirectStandardError = options.RedirectOutput && !options.MergeStdErrAndStdOut && !options.IsGuiApplication
                };

                // Add additional environment variables
                foreach (var kvp in options.AdditionalEnvironmentVariables)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
                UnifiedLogger.Create().Message($"Created ProcessStartInfo: FileName={startInfo.FileName}, Arguments={startInfo.Arguments}").Severity(LogLevel.Information);
                return startInfo;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error creating ProcessStartInfo:{Environment.NewLine}{ex.Message}").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Converts a <see cref="System.Collections.Specialized.StringDictionary"/> of environment variables into an 
        /// <see cref="IDictionary{TKey, TValue}"/> of key-value pairs.
        /// </summary>
        /// <param name="environmentVariables">
        /// The environment variables to convert. This is a <see cref="StringDictionary"/> where both keys and values are strings. 
        /// If <paramref name="environmentVariables"/> is <c>null</c>, an empty dictionary is returned.
        /// </param>
        /// <returns>
        /// A case-insensitive <see cref="IDictionary{TKey, TValue}"/> containing the environment variables from the input.
        /// Keys are the environment variable names, and values are the corresponding environment variable values.
        /// </returns>
        /// <remarks>
        /// This method ensures that only non-null values are included in the result.
        /// If <paramref name="environmentVariables"/> is <c>null</c>, an empty dictionary with a case-insensitive key comparison 
        /// is returned.
        /// </remarks>
        public static IDictionary<string, string> ConvertEnvironmentVariables(StringDictionary? environmentVariables)
        {
            if (environmentVariables == null)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return environmentVariables
                .Cast<DictionaryEntry>()
                .Where(entry => entry.Value != null)
                .ToDictionary(
                    entry => (string)entry.Key,
                    entry => (string)entry.Value!,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        /// <summary>
        /// Starts a process in the specified session.
        /// </summary>
        /// <param name="sessionId">The ID of the session to start the process in.</param>
        /// <param name="startInfo">The ProcessStartInfo for the process to start.</param>
        /// <param name="processCreationFlags">Flags to control the creation of the process.</param>
        /// <param name="useLinkedAdminToken">If true, uses the linked admin token when available.</param>
        /// <param name="inheritEnvironment">Specifies whether to inherit the parent's environment variables.</param>
        /// <returns>The started ProcessEx object.</returns>
        private static async Task<Process> StartProcessInSessionAsync(
            uint sessionId,
            ProcessStartInfo startInfo,
            CREATE_PROCESS processCreationFlags,
            bool useLinkedAdminToken,
            bool inheritEnvironment,
            bool isGuiApplication)
        {
            SafeAccessToken primaryToken = SafeAccessToken.Invalid;
            SafeAccessToken tokenToUse = SafeAccessToken.Invalid;

            try
            {
                if (!TokenManager.GetSecurityIdentificationTokenForSessionId(sessionId, out SafeAccessToken securityIdentificationToken))
                {
                    throw new InvalidOperationException($"Failed to obtain the security identification token for session id [{sessionId}].");
                }

                using (securityIdentificationToken)
                {
                    if (!TokenManager.CreatePrimaryToken(securityIdentificationToken, out primaryToken))
                    {
                        throw new InvalidOperationException("Failed to duplicate token as a primary token.");
                    }
                }

                using (primaryToken)
                {
                    tokenToUse = primaryToken;
                    if (useLinkedAdminToken && TokenManager.GetLinkedElevatedToken(primaryToken, out SafeAccessToken linkedAdminToken))
                    {
                        if (linkedAdminToken.IsInvalid)
                        {
                            throw new InvalidOperationException($"Failed to get the linked elevated token for specified session id [{sessionId}].");
                        }
                        tokenToUse = linkedAdminToken;
                    }

                    var startupInfo = new STARTUPINFO();
                    startupInfo.cb = Marshal.SizeOf(startupInfo);
                    startupInfo.lpDesktop = @"WinSta0\Default";

                    if (startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                    {
                        startupInfo.dwFlags |= (uint)STARTF.STARTF_USESTDHANDLES;

                        SafeFileHandle standardOutput = CreatePipe();
                        SafeFileHandle standardError = CreatePipe();

                        startupInfo.hStdOutput = startInfo.RedirectStandardOutput ? standardOutput.DangerousGetHandle() : IntPtr.Zero;
                        startupInfo.hStdError = startInfo.RedirectStandardError ? standardError.DangerousGetHandle() : IntPtr.Zero;
                        startupInfo.hStdInput = IntPtr.Zero;
                    }

                    PROCESS_INFORMATION processInfo;

                    IDictionary<string, string> envVars = ConvertEnvironmentVariables(startInfo.EnvironmentVariables);

                    using (var environmentBlock = TokenManager.CreateTokenEnvironmentBlock(tokenToUse, envVars, inheritEnvironment))
                    {
                        UnifiedLogger.Create().Message($"Attempting to start process with CreateProcessAsUser with: FileName [{startInfo.FileName}], Arguments [{string.Join(", ", startInfo.Arguments)}], WorkingDirectory [{startInfo.WorkingDirectory}].").Severity(LogLevel.Information);

                        if (!NativeMethods.CreateProcessAsUser(
                            tokenToUse,
                            startInfo.FileName,
                            new StringBuilder(startInfo.Arguments),
                            default,
                            default,
                            false,
                            processCreationFlags,
                            environmentBlock.DangerousGetHandle(),
                            startInfo.WorkingDirectory,
                            in startupInfo,
                            out processInfo))
                        {
                            ErrorHandler.ThrowSystemError($"'CreateProcessAsUser' failed to start the process [{startInfo.FileName}].", SystemErrorType.Win32);
                        }
                    }

                    Process? process = null;
                    try
                    {
                        process = Process.GetProcessById((int)processInfo.dwProcessId);

                        // Only wait for input idle if it's a GUI application
                        if (isGuiApplication)
                        {
                            await Task.Run(() =>
                            {
                                try
                                {
                                    process.WaitForInputIdle(5000);
                                }
                                catch (InvalidOperationException)
                                {
                                    // The process has exited or is a console application
                                    UnifiedLogger.Create().Message("Process exited before becoming idle or is a console application.").Severity(LogLevel.Warning);
                                }
                            });
                        }

                        return process;
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(processInfo.hProcess);
                        NativeMethods.CloseHandle(processInfo.hThread);
                    }
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to start process in session id [{sessionId}]:{Environment.NewLine}{ex.Message}").Error(ex);
                throw;
            }
            finally
            {
                if (!tokenToUse.IsInvalid || !tokenToUse.IsClosed)
                {
                    tokenToUse.Dispose();
                }
            }
        }

        private static SafeFileHandle CreatePipe()
        {
            SECURITY_ATTRIBUTES securityAttributes = new SECURITY_ATTRIBUTES();
            securityAttributes.nLength = Marshal.SizeOf(securityAttributes);
            securityAttributes.bInheritHandle = 1;
            securityAttributes.lpSecurityDescriptor = IntPtr.Zero;

            if (!NativeMethods.CreatePipe(out _, out SafeFileHandle writeHandle, securityAttributes, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create pipe.");
            }

            return writeHandle;
        }

        /// <summary>
        /// Starts a process asynchronously with the specified <see cref="ProcessStartInfo"/>.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> containing the settings to start the process with.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the started <see cref="ProcessEx"/> object.</returns>
        /// <example>
        /// <code>
        /// var startInfo = new ProcessStartInfo
        /// {
        ///     CallerFileName = "example.exe",
        ///     Arguments = "-someArgument",
        ///     RedirectStandardOutput = true,
        ///     RedirectStandardError = true,
        ///     UseShellExecute = false,
        ///     CreateNoWindow = true
        /// };
        /// 
        /// var process = await StartProcessAsync(startInfo);
        /// ConsoleHelper.DebugWrite($"ProcessEx started with ID {process.Id}");
        /// </code>
        /// </example>
        private static async Task<Process> StartProcessAsync(ProcessStartInfo startInfo, bool isGuiApplication)
        {
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            try
            {
                UnifiedLogger.Create().Message($"Attempting to start process: FileName [{startInfo.FileName}], Arguments [{string.Join(", ", startInfo.Arguments)}].").Severity(LogLevel.Information);

                if (!process.Start())
                {
                    throw new InvalidOperationException("Process.Start() returned false.");
                }

                // Only wait for input idle if it's a GUI application
                if (isGuiApplication)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            process.WaitForInputIdle(5000);
                        }
                        catch (InvalidOperationException)
                        {
                            // The process has exited or is a console application
                            UnifiedLogger.Create().Message("Process exited before becoming idle or is a console application.").Severity(LogLevel.Warning);
                        }
                    });
                }

                return process;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to start process: {ex.Message}").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="StartProcess"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="StartProcess"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose() or from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _executionManager.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Destructor for <see cref="StartProcess"/>.
        /// </summary>
        ~StartProcess()
        {
            Dispose(false);
        }
    }
}
