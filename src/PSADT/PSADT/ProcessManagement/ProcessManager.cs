using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Execution;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using PSADT.Security;
using PSADT.TerminalServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Com;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.TaskScheduler;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides methods for launching processes with more control over input/output.
    /// </summary>
    public static class ProcessManager
    {
        /// <summary>
        /// Launches a process with the specified start info and waits for it to complete.
        /// </summary>
        /// <param name="launchInfo"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static ProcessHandle? LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            // Set up initial variables needed throughout method.
            Task stdOutTask = Task.CompletedTask; Task stdErrTask = Task.CompletedTask;
            List<string> stdout = []; List<string> stderr = [];
            ConcurrentQueue<string> interleaved = [];
            SafeProcessHandle? hProcess = null;
            uint? processId = null;

            // Determine whether the process we're starting is a console app or not. This is important
            // because under ShellExecuteEx() invocations, stdout/stderr will attach to the running console.
            bool cliApp = File.Exists(launchInfo.FilePath) ? ExecutableUtilities.GetExecutableInfo(launchInfo.FilePath).Subsystem != IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI : launchInfo.CreateNoWindow || !launchInfo.UseShellExecute;

            // Set up the job object and I/O completion port for the process.
            // No using statements here, they're disposed of in the final task.
            var iocp = Kernel32.CreateIoCompletionPort(SafeBaseHandle.InvalidHandle, SafeBaseHandle.NullHandle, UIntPtr.Zero, 1);
            var job = Kernel32.CreateJobObject(null, default);

            // Set up the job object to use the I/O completion port.
            bool iocpAddRefOuter = false; iocp.DangerousAddRef(ref iocpAddRefOuter);
            Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, new JOBOBJECT_ASSOCIATE_COMPLETION_PORT { CompletionPort = (HANDLE)iocp.DangerousGetHandle(), CompletionKey = null });

            // Set up the required job limit if child processes must be killed with the parent.
            if (launchInfo.KillChildProcessesWithParent)
            {
                Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, new JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION { LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE } });
            }

            // We only let console apps run via ShellExecuteEx() when there's a window shown for it.
            // Invoking processes as user has no ShellExecute capability, so it always comes through here.
            if (cliApp && launchInfo.CreateNoWindow || !launchInfo.UseShellExecute || null != launchInfo.Username)
            {
                var startupInfo = new STARTUPINFOW { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };
                if (null != launchInfo.WindowStyle)
                {
                    startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                    startupInfo.wShowWindow = launchInfo.WindowStyle.Value;
                }
                SafeFileHandle? hStdOutWrite = default;
                SafeFileHandle? hStdErrWrite = default;
                bool hStdOutWriteAddRef = false;
                bool hStdErrWriteAddRef = false;
                try
                {
                    // The process is created suspended so it can be assigned to the job object.
                    var creationFlags = (PROCESS_CREATION_FLAGS)launchInfo.PriorityClass |
                        PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                        PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP |
                        PROCESS_CREATION_FLAGS.CREATE_SUSPENDED;

                    // We must create a console window for console apps when the window is shown.
                    if (cliApp)
                    {
                        if (launchInfo.CreateNoWindow)
                        {
                            if (launchInfo.CancellationToken != default && launchInfo.NoTerminateOnTimeout)
                            {
                                throw new InvalidOperationException("The NoTerminateOnTimeout option is not supported for console apps while reading stdout/stderr.");
                            }
                            startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                        }
                        else
                        {
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
                        }
                    }

                    // If we're to read the output, we create pipes for stdout and stderr.
                    void SetupStreamPipes()
                    {
                        if ((startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                        {
                            CreatePipe(out var hStdOutRead, out hStdOutWrite);
                            CreatePipe(out var hStdErrRead, out hStdErrWrite);
                            stdOutTask = Task.Run(() => ReadPipe(hStdOutRead, stdout, interleaved, launchInfo.StreamEncoding));
                            stdErrTask = Task.Run(() => ReadPipe(hStdErrRead, stderr, interleaved, launchInfo.StreamEncoding));
                            hStdOutWrite.DangerousAddRef(ref hStdOutWriteAddRef);
                            hStdErrWrite.DangerousAddRef(ref hStdErrWriteAddRef);
                            startupInfo.hStdOutput = (HANDLE)hStdOutWrite.DangerousGetHandle();
                            startupInfo.hStdError = (HANDLE)hStdErrWrite.DangerousGetHandle();
                        }
                    }

                    // Handle user process creation, otherwise just create the process for the running user.
                    PROCESS_INFORMATION pi = new();
                    if (null != launchInfo.Username && GetSessionForUsername(launchInfo.Username) is SessionInfo session && !AccountUtilities.CallerUsername.Equals(session.NTAccount))
                    {
                        // We can only run a process if we can act as part of the operating system.
                        if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeTcbPrivilege))
                        {
                            throw new UnauthorizedAccessException($"The calling account of [{AccountUtilities.CallerUsername}] does not hold the necessary [SeTcbPrivilege] privilege (Act as part of the operating system) for this operation.");
                        }

                        // Enable the required tokens. SYSTEM usually has these privileges, but locked down environments via WDAC may require specific enablement.
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);

                        // Get the user's token and create the process.
                        using (var hPrimaryToken = GetTokenViaBroker(session.SessionId, launchInfo.UseLinkedAdminToken))
                        {
                            UserEnv.CreateEnvironmentBlock(out var lpEnvironment, hPrimaryToken, launchInfo.InheritEnvironmentVariables);
                            using (lpEnvironment)
                            {
                                // This is important so that a windowed application can be shown.
                                using (var lpDesktop = SafeCoTaskMemHandle.StringToUni(@"winsta0\default"))
                                {
                                    startupInfo.lpDesktop = lpDesktop.ToPWSTR();
                                    string? workingDirectory = launchInfo.WorkingDirectory;
                                    string commandLine = launchInfo.CommandLine;
                                    if (launchInfo.ExpandEnvironmentVariables)
                                    {
                                        var environmentDictionary = EnvironmentBlockToDictionary(lpEnvironment);
                                        commandLine = ExpandEnvironmentVariables(session.NTAccount, launchInfo.CommandLine, environmentDictionary);
                                        if (null != workingDirectory)
                                        {
                                            workingDirectory = ExpandEnvironmentVariables(session.NTAccount, workingDirectory, environmentDictionary);
                                        }
                                    }
                                    SetupStreamPipes(); AdvApi32.CreateProcessAsUser(hPrimaryToken, null, commandLine, null, null, true, creationFlags, lpEnvironment, workingDirectory, startupInfo, out pi);
                                }
                            }
                        }
                    }
                    else if (launchInfo.UseUnelevatedToken && AccountUtilities.CallerIsAdmin)
                    {
                        // Throw if the caller is expecting to be able to capture stdout/stderr but is running elevated.
                        if ((startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                        {
                            throw new InvalidOperationException("The underlying API to create a process using an unelevated token does not support capturing stdout/stderr.");
                        }

                        // We're running elevated but have been asked to de-elevate.
                        using (var hPrimaryToken = GetUnelevatedToken())
                        {
                            AdvApi32.CreateProcessWithToken(hPrimaryToken, CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE, null, launchInfo.CommandLine, creationFlags, SafeEnvironmentBlockHandle.Null, launchInfo.WorkingDirectory, startupInfo, out pi);
                        }
                    }
                    else
                    {
                        // No username was specified and we weren't asked to de-elevate, so we're just creating the process as this current user as-is.
                        SetupStreamPipes(); Kernel32.CreateProcess(null, launchInfo.CommandLine, null, null, true, creationFlags, SafeEnvironmentBlockHandle.Null, launchInfo.WorkingDirectory, startupInfo, out pi);
                    }

                    // Start tracking the process and allow it to resume execution.
                    using (SafeThreadHandle hThread = new(pi.hThread, true))
                    {
                        Kernel32.AssignProcessToJobObject(job, hProcess = new SafeProcessHandle(pi.hProcess, true));
                        Kernel32.ResumeThread(hThread);
                        processId = pi.dwProcessId;
                    }
                }
                finally
                {
                    if (hStdOutWriteAddRef)
                    {
                        hStdOutWrite!.DangerousRelease();
                    }
                    if (hStdErrWriteAddRef)
                    {
                        hStdErrWrite!.DangerousRelease();
                    }
                    hStdOutWrite?.Dispose();
                    hStdErrWrite?.Dispose();
                }
            }
            else
            {
                // Set up the shell execute info structure.
                var startupInfo = new Shell32.SHELLEXECUTEINFO
                {
                    cbSize = Marshal.SizeOf<Shell32.SHELLEXECUTEINFO>(),
                    fMask = SEE_MASK_FLAGS.SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAGS.SEE_MASK_FLAG_NO_UI | SEE_MASK_FLAGS.SEE_MASK_NOZONECHECKS,
                    lpVerb = launchInfo.Verb,
                    lpFile = launchInfo.FilePath,
                    lpParameters = launchInfo.Arguments,
                    lpDirectory = launchInfo.WorkingDirectory,
                };
                if (null != launchInfo.WindowStyle)
                {
                    startupInfo.nShow = launchInfo.WindowStyle.Value;
                }
                if (launchInfo.CreateNoWindow)
                {
                    startupInfo.fMask |= SEE_MASK_FLAGS.SEE_MASK_NO_CONSOLE;
                    startupInfo.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;
                }

                // Start the process and assign it to the job object if we have a handle.
                Shell32.ShellExecuteEx(ref startupInfo);
                if (startupInfo.hProcess != IntPtr.Zero)
                {
                    hProcess = new SafeProcessHandle(startupInfo.hProcess, true);
                    processId = Kernel32.GetProcessId(hProcess);
                    Kernel32.AssignProcessToJobObject(job, hProcess);
                    if (launchInfo.PriorityClass != ProcessPriorityClass.Normal && PrivilegeManager.TestProcessAccessRights(hProcess, PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION))
                    {
                        Kernel32.SetPriorityClass(hProcess, launchInfo.PriorityClass);
                    }
                }
            }

            // If we don't have a process (shell action), return early.
            if (!(null != hProcess && null != processId))
            {
                return null;
            }

            // These tasks read all outputs and wait for the process to complete.
            TaskCompletionSource<ProcessResult> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task.Run(async () =>
            {
                using (iocp)
                using (job)
                {
                    bool ctsAddRef = false;
                    bool iocpAddRefInner = false;
                    try
                    {
                        iocp.DangerousAddRef(ref iocpAddRefInner);
                        launchInfo.CancellationToken.WaitHandle.SafeWaitHandle.DangerousAddRef(ref ctsAddRef);
                        ReadOnlySpan<HANDLE> handles = [(HANDLE)iocp.DangerousGetHandle(), (HANDLE)launchInfo.CancellationToken.WaitHandle.SafeWaitHandle.DangerousGetHandle()];
                        using (hProcess)
                        {
                            while (true)
                            {
                                var index = (uint)Kernel32.WaitForMultipleObjects(handles, false, PInvoke.INFINITE);
                                if (index == 0)
                                {
                                    Kernel32.GetQueuedCompletionStatus(iocp, out var lpCompletionCode, out _, out var lpOverlapped, PInvoke.INFINITE);
                                    if (lpCompletionCode == PInvoke.JOB_OBJECT_MSG_EXIT_PROCESS && !launchInfo.WaitForChildProcesses && lpOverlapped.ToInt32() == processId || lpCompletionCode == PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO)
                                    {
                                        await Task.WhenAll(stdOutTask, stdErrTask);
                                        Kernel32.GetExitCodeProcess(hProcess, out var lpExitCode);
                                        tcs.SetResult(new ProcessResult(ValueTypeConverter<int>.Convert(lpExitCode), stdout.AsReadOnly(), stderr.AsReadOnly(), interleaved.ToList().AsReadOnly()));
                                        break;
                                    }
                                }
                                else if (index == 1)
                                {
                                    if (launchInfo.NoTerminateOnTimeout)
                                    {
                                        break;
                                    }
                                    Kernel32.TerminateJobObject(job, ValueTypeConverter<uint>.Convert(TimeoutExitCode));
                                }
                                else
                                {
                                    throw new InvalidOperationException($"An invalid result was received while waiting for post-launch handles. Result: {index}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        if (ctsAddRef)
                        {
                            launchInfo.CancellationToken.WaitHandle.SafeWaitHandle.DangerousRelease();
                        }
                        if (iocpAddRefInner)
                        {
                            iocp.DangerousRelease();
                        }
                        if (iocpAddRefOuter)
                        {
                            iocp.DangerousRelease();
                        }
                    }
                }
            });

            // Return a ProcessResult object with the result of the process.
            return new(processId, tcs.Task);
        }

        /// <summary>
        /// Retrieves the session information for the specified user.
        /// </summary>
        /// <remarks>This method queries the system for session information associated with the specified
        /// user. The user must be logged on and their session must be active for the method to succeed.</remarks>
        /// <param name="username">The <see cref="NTAccount"/> representing the user whose session information is to be retrieved. The account
        /// must correspond to a logged-on and active user.</param>
        /// <returns>A <see cref="SessionInfo"/> object containing details about the user's session.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no user sessions are available, if no session is found for the specified user, or if the user's
        /// session is not active.</exception>
        private static SessionInfo GetSessionForUsername(NTAccount username)
        {
            // You can only run a process as a user if they're logged on.
            var userSessions = SessionManager.GetSessionInfo();
            if (userSessions.Count == 0)
            {
                throw new InvalidOperationException("No user sessions are available to launch the process in.");
            }

            // You can only run a process as a user if they're active.
            SessionInfo? session = null;
            if (!username!.Value.Contains('\\'))
            {
                session = userSessions.First(s => username.Value.Equals(s.UserName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                session = userSessions.First(s => s.NTAccount == username);
            }
            if (null == session)
            {
                throw new InvalidOperationException($"No session found for user {username}.");
            }
            if (session.ConnectState != WTS_CONNECTSTATE_CLASS.WTSActive)
            {
                throw new InvalidOperationException($"The session for user {username} is not active.");
            }

            // Return the session information for the user.
            return session;
        }

        /// <summary>
        /// Retrieves a primary token for the Explorer process with limited access rights.
        /// </summary>
        /// <remarks>This method obtains a token associated with the Explorer process and duplicates it to
        /// create a primary token. The returned token can be used for operations requiring an unelevated
        /// context.</remarks>
        /// <returns>A <see cref="SafeFileHandle"/> representing the primary token for the Explorer process, or <see
        /// langword="null"/> if the operation fails.</returns>
        private static SafeFileHandle GetUnelevatedToken()
        {
            using (var hProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, ShellUtilities.GetExplorerProcessId()))
            {
                AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
                using (hProcessToken)
                {
                    if (!TokenManager.GetTokenSid(hProcessToken).Equals(AccountUtilities.CallerSid))
                    {
                        throw new InvalidOperationException("Failed to retrieve an unelevated token for the calling account.");
                    }
                    if (TokenManager.IsTokenElevated(hProcessToken))
                    {
                        throw new InvalidOperationException("The calling account's shell is running elevated, therefore unable to get unelevated token.");
                    }
                    return TokenManager.GetPrimaryToken(hProcessToken);
                }
            }
        }

        /// <summary>
        /// Creates a pipe for reading or writing.
        /// </summary>
        /// <param name="readPipe"></param>
        /// <param name="writePipe"></param>
        /// <exception cref="Win32Exception"></exception>
        private static void CreatePipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe)
        {
            Kernel32.CreatePipe(out readPipe, out writePipe, new SECURITY_ATTRIBUTES { nLength = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>(), bInheritHandle = true });
            Kernel32.SetHandleInformation(readPipe, HANDLE_FLAGS.HANDLE_FLAG_INHERIT, 0);
        }

        /// <summary>
        /// Reads from a pipe until the pipe is closed.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="output"></param>
        /// <param name="interleaved"></param>
        /// <param name="encoding"></param>
        private static void ReadPipe(SafeFileHandle handle, List<string> output, ConcurrentQueue<string> interleaved, Encoding encoding)
        {
            Span<byte> buffer = stackalloc byte[4096];
            uint bytesRead = 0;
            using (handle)
            {
                while (true)
                {
                    try
                    {
                        Kernel32.ReadFile(handle, buffer, out bytesRead, IntPtr.Zero);
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_BROKEN_PIPE)
                    {
                        break;
                    }
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    var text = encoding.GetString(buffer, (int)bytesRead).Replace("\0", string.Empty).TrimEnd();
                    interleaved.Enqueue(text);
                    output.Add(text);
                }
            }
        }

        /// <summary>
        /// Converts a native environment block into a read-only dictionary of environment variables.
        /// </summary>
        /// <remarks>This method processes a native environment block, which is a contiguous block of
        /// memory containing null-terminated strings in the format "Name=Value". It extracts these strings and converts
        /// them into a dictionary for easier access in managed code.</remarks>
        /// <param name="environmentBlock">A handle to the native environment block. The handle must be valid and contain environment variables in the
        /// format "Name=Value".</param>
        /// <returns>A read-only dictionary containing the environment variables as key-value pairs. The keys are
        /// case-insensitive.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="environmentBlock"/> is invalid, empty, or contains entries that are not in the
        /// expected "Name=Value" format.</exception>
        private static ReadOnlyDictionary<string, string> EnvironmentBlockToDictionary(SafeEnvironmentBlockHandle environmentBlock)
        {
            if (environmentBlock.IsInvalid)
            {
                throw new ArgumentException("The environment block is invalid.", nameof(environmentBlock));
            }
            bool envBlockAddRef = false;
            try
            {
                environmentBlock.DangerousAddRef(ref envBlockAddRef);
                var envBlockPtr = environmentBlock.DangerousGetHandle();
                var envDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (true)
                {
                    // Marshal.PtrToStringUni will read up to the first null terminator.
                    string entry = Marshal.PtrToStringUni(envBlockPtr)!;
                    if (string.IsNullOrWhiteSpace(entry))
                    {
                        break;
                    }

                    // Split into name and value (only on the first '=').
                    int idx = entry.IndexOf('=');
                    if (idx < 0)
                    {
                        throw new ArgumentException($"Invalid environment variable entry: '{entry}'. Expected format is 'Name=Value'.", nameof(environmentBlock));
                    }

                    // Add the valid entry and advance pointer past this string + its null terminator.
                    envDict.Add(entry.Substring(0, idx), entry.Substring(idx + 1));
                    envBlockPtr += (entry.Length + 1) * sizeof(char);
                }
                if (envDict.Count == 0)
                {
                    throw new ArgumentException("The environment block is empty.", nameof(environmentBlock));
                }
                return new(envDict);
            }
            finally
            {
                if (envBlockAddRef)
                {
                    environmentBlock.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Expands environment variable placeholders in the specified input string using the provided environment
        /// block.
        /// </summary>
        /// <remarks>This method uses the provided environment block to resolve environment variable
        /// placeholders. Placeholders are expected to be in the format <c>%VariableName%</c>. If a placeholder does not
        /// match any environment variable in the block, it remains unchanged in the output.</remarks>
        /// <param name="input">The input string containing environment variable placeholders in the format <c>%VariableName%</c>.</param>
        /// <param name="environment">A handle to the environment block used for resolving environment variables. The handle must be valid and not
        /// invalid.</param>
        /// <returns>A string with all recognized environment variable placeholders replaced by their corresponding values.
        /// Placeholders that cannot be resolved are left unchanged.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="input"/> is <see langword="null"/>, empty, or consists only of whitespace. Thrown
        /// if <paramref name="environment"/> is invalid.</exception>
        private static string ExpandEnvironmentVariables(NTAccount ntAccount, string input, ReadOnlyDictionary<string, string> environment)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            }
            if (null == environment)
            {
                throw new ArgumentException("The environment block is invalid.", nameof(environment));
            }
            return Regex.Replace(input, "%([^%]+)%", m => environment.TryGetValue(m.Groups[1].Value, out var envVar) ? envVar : throw new InvalidOperationException($"The user [{ntAccount}] does not have environment variable [{m.Value}] defined or available."));
        }

        /// <summary>
        /// Retrieves a security token for the specified session using a token broker.
        /// </summary>
        /// <remarks>This method establishes a named pipe connection with a token broker process to
        /// retrieve the security token. The token broker process is started with the specified session ID and optional
        /// linked administrative token. The method ensures that appropriate security settings are applied to the named
        /// pipe.</remarks>
        /// <param name="sessionId">The ID of the session for which the token is requested.</param>
        /// <param name="useLinkedAdminToken">A value indicating whether to use a linked administrative token. <see langword="true"/> to use a linked
        /// administrative token; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the security token for the specified session. The caller is
        /// responsible for disposing of the handle when it is no longer needed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no token is received from the token broker.</exception>
        private static SafeFileHandle GetTokenViaBroker(uint sessionId, bool useLinkedAdminToken = false)
        {
            // Set up the required security for the named pipe.
            PipeSecurity pipeSecurity = new(); pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite,
                AccessControlType.Allow
            ));

            // Create a named pipe for the token broker.
            string pipeName = $"PSADT.TokenBroker_{CryptographicUtilities.SecureNewGuid()}";
            using (var pipe = NamedPipeServerStreamAcl.Create(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 8, 8, pipeSecurity))
            {
                // Create a new token broker process and wait for a connection.
                StartTokenBroker($"-PipeName {pipeName} -ProcessId {AccountUtilities.CallerProcessId} -SessionId {sessionId} -UseLinkedAdminToken {useLinkedAdminToken}");
                pipe.WaitForConnection();

                // Read the token from the pipe.
                byte[] tokenBuf = new byte[sizeof(long)];
                if (pipe.Read(tokenBuf, 0, tokenBuf.Length) == 0)
                {
                    throw new InvalidOperationException("No token received from the token broker.");
                }

                // Return the token handle.
                return new SafeFileHandle((nint)BitConverter.ToInt64(tokenBuf, 0), true);
            }
        }

        /// <summary>
        /// Starts a temporary token broker process by creating and executing a scheduled task.
        /// </summary>
        /// <remarks>This method creates a scheduled task that runs a token broker executable with the
        /// specified arguments. The task is registered with the SYSTEM account, executed immediately, and then deleted
        /// to ensure no persistent artifacts remain. The method initializes and uninitializes the COM library for the
        /// current thread as part of its operation.</remarks>
        /// <param name="arguments">The command-line arguments to pass to the token broker executable.</param>
        private static void StartTokenBroker(string arguments)
        {
            // Initialize the COM library for the current thread.
            Ole32.CoInitializeEx(Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA) ? COINIT.COINIT_APARTMENTTHREADED : COINIT.COINIT_MULTITHREADED);
            try
            {
                // Create an instance of the TaskService to manage scheduled tasks and connect on localhost.
                Ole32.CoCreateInstance(CLSID_TaskScheduler, null!, CLSCTX.CLSCTX_INPROC_SERVER, out ITaskService servicePtr);
                servicePtr.Connect(null, null, null, null);

                // Set up the task as required.
                BSTR folderName = (BSTR)Marshal.StringToBSTR(@"\");
                BSTR taskName = (BSTR)Marshal.StringToBSTR($"PSADT.TokenBroker_{CryptographicUtilities.SecureNewGuid()}");
                BSTR userId = (BSTR)Marshal.StringToBSTR("NT AUTHORITY\\SYSTEM");
                BSTR path = (BSTR)Marshal.StringToBSTR(typeof(ProcessManager).Assembly.Location.Replace(".dll", ".TokenBroker.exe"));
                BSTR args = (BSTR)Marshal.StringToBSTR(arguments);
                try
                {
                    // Create a new task definition.
                    servicePtr.GetFolder(folderName, out var rootFolder);
                    servicePtr.NewTask(0, out ITaskDefinition taskDefinition);
                    taskDefinition.Actions.Create(TASK_ACTION_TYPE.TASK_ACTION_EXEC, out IAction action);
                    IExecAction execAction = (IExecAction)action;
                    taskDefinition.Principal.UserId = userId;
                    taskDefinition.Principal.LogonType = TASK_LOGON_TYPE.TASK_LOGON_SERVICE_ACCOUNT;
                    execAction.Path = path;
                    execAction.Arguments = args;

                    // Define task permissions so only SYSTEM has visibility of it.
                    const string sddl = "O:SY" + "G:SY" + "D:(A;;FA;;;SY)";

                    // Register the task, start it, then delete it.
                    rootFolder.RegisterTaskDefinition(taskName, taskDefinition, (int)TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null, TASK_LOGON_TYPE.TASK_LOGON_SERVICE_ACCOUNT, sddl, out var task);
                    task.Run(null, out _); rootFolder.DeleteTask(taskName, 0);
                }
                finally
                {
                    // Free all binary strings.
                    Marshal.FreeBSTR(args);
                    Marshal.FreeBSTR(path);
                    Marshal.FreeBSTR(userId);
                    Marshal.FreeBSTR(taskName);
                    Marshal.FreeBSTR(folderName);
                }
            }
            finally
            {
                // Uninitialize the COM library for the current thread.
                PInvoke.CoUninitialize();
            }
        }

        /// <summary>
        /// Represents the globally unique identifier (GUID) for the Task Scheduler COM class.
        /// </summary>
        /// <remarks>This GUID is used to identify the Task Scheduler COM class when interacting with
        /// COM-based APIs.</remarks>
        private static readonly Guid CLSID_TaskScheduler = new("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");

        /// <summary>
        /// Special exit code used to signal when we're terminating a process due to timeout.
        /// The value is `'PSAppDeployToolkit'.GetHashCode()` under Windows PowerShell 5.1.
        /// </summary>
        public const int TimeoutExitCode = -443991205;
    }
}
