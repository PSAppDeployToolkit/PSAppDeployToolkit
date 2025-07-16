using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Principal;
using System.ServiceProcess;
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
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;

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
            bool cliApp;
            try
            {
                cliApp = ExecutableUtilities.GetExecutableInfo(launchInfo.FilePath).Subsystem != IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI;
            }
            catch
            {
                cliApp = launchInfo.CreateNoWindow || !launchInfo.UseShellExecute;
            }

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
            string commandLine;
            if ((cliApp && launchInfo.CreateNoWindow) || (!launchInfo.UseShellExecute) || (null != launchInfo.Username))
            {
                var startupInfo = new STARTUPINFOW { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };
                if (null != launchInfo.WindowStyle)
                {
                    startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                    startupInfo.wShowWindow = (ushort)launchInfo.WindowStyle.Value;
                }
                SafeFileHandle? hStdOutWrite = default;
                SafeFileHandle? hStdErrWrite = default;
                bool hStdOutWriteAddRef = false;
                bool hStdErrWriteAddRef = false;
                try
                {
                    // The process is created suspended so it can be assigned to the job object.
                    var creationFlags = PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                        PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP |
                        PROCESS_CREATION_FLAGS.CREATE_SUSPENDED;

                    // Set the process priority class if specified.
                    if (null != launchInfo.PriorityClass)
                    {
                        creationFlags |= (PROCESS_CREATION_FLAGS)launchInfo.PriorityClass.Value;
                    }

                    // We must create a console window for console apps when the window is shown.
                    if (cliApp)
                    {
                        if (launchInfo.CreateNoWindow)
                        {
                            startupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                        }
                        else
                        {
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
                        }
                    }

                    // If we're to read the output, we create pipes for stdout and stderr.
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

                    // Handle user process creation, otherwise just create the process for the running user.
                    PROCESS_INFORMATION pi = new();
                    if (null != launchInfo.Username && launchInfo.Username != AccountUtilities.CallerUsername && GetSessionForUsername(launchInfo.Username) is SessionInfo session)
                    {
                        // Get the user's token.
                        SafeFileHandle hUserToken = null!;
                        if (!AccountUtilities.CallerIsLocalSystem)
                        {
                            // When we're not local system, we need to find the user's Explorer process and get its token.
                            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeDebugPrivilege);
                            foreach (var explorerProcess in Process.GetProcessesByName("explorer").OrderBy(static p => p.StartTime))
                            {
                                using (explorerProcess) using (explorerProcess.SafeHandle)
                                {
                                    AdvApi32.OpenProcessToken(explorerProcess.SafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hProcessToken);
                                    if (TokenManager.GetTokenSid(hProcessToken) == session.SID)
                                    {
                                        hUserToken = hProcessToken;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // When we're local system, we can just get the primary token for the user.
                            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                            WtsApi32.WTSQueryUserToken(session.SessionId, out hUserToken);
                        }

                        // Throw if for whatever reason, we couldn't get a token.
                        if (null == hUserToken)
                        {
                            throw new InvalidOperationException($"Failed to retrieve a primary token for user [{session.NTAccount}]. Ensure the user is logged on and has an active session.");
                        }

                        // Get the primary token for the user, either linked or not.
                        SafeFileHandle hPrimaryToken;
                        using (hUserToken)
                        {
                            if (launchInfo.UseLinkedAdminToken)
                            {
                                try
                                {
                                    hPrimaryToken = TokenManager.GetLinkedPrimaryToken(hUserToken);
                                }
                                catch (Exception ex)
                                {
                                    throw new UnauthorizedAccessException($"Failed to get the linked admin token for user [{session.NTAccount}].", ex);
                                }
                            }
                            else
                            {
                                hPrimaryToken = TokenManager.GetPrimaryToken(hUserToken);
                            }
                        }

                        // Start the process with the user's token.
                        using (var lpDesktop = SafeCoTaskMemHandle.StringToUni(@"winsta0\default"))
                        using (hPrimaryToken)
                        {
                            // Without creating an environment block, the process will take on the environment of the SYSTEM account.
                            UserEnv.CreateEnvironmentBlock(out var lpEnvironment, hPrimaryToken, launchInfo.InheritEnvironmentVariables);
                            using (lpEnvironment)
                            {
                                startupInfo.lpDesktop = lpDesktop.ToPWSTR();
                                OutLaunchArguments(launchInfo, session.NTAccount, EnvironmentBlockToDictionary(lpEnvironment), out commandLine, out string? workingDirectory);
                                CreateProcessUsingToken(hPrimaryToken, commandLine, launchInfo.UsingAnonymousHandles, creationFlags, lpEnvironment, workingDirectory, startupInfo, out pi);
                            }
                        }
                    }
                    else if (launchInfo.UseUnelevatedToken && AccountUtilities.CallerIsAdmin && !AccountUtilities.CallerIsLocalSystem)
                    {
                        // We're running elevated but have been asked to de-elevate.
                        using (var hPrimaryToken = GetUnelevatedToken())
                        {
                            OutLaunchArguments(launchInfo, AccountUtilities.CallerUsername, GetCallerEnvironmentDictionary(), out commandLine, out string? workingDirectory);
                            CreateProcessUsingToken(hPrimaryToken, commandLine, launchInfo.UsingAnonymousHandles, creationFlags, SafeEnvironmentBlockHandle.Null, workingDirectory, startupInfo, out pi);
                        }
                    }
                    else
                    {
                        // No username was specified and we weren't asked to de-elevate, so we're just creating the process as this current user as-is.
                        OutLaunchArguments(launchInfo, AccountUtilities.CallerUsername, GetCallerEnvironmentDictionary(), out commandLine, out string? workingDirectory);
                        Kernel32.CreateProcess(null, commandLine, null, null, true, creationFlags, SafeEnvironmentBlockHandle.Null, workingDirectory, startupInfo, out pi);
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
                    hStdOutWrite = null;
                    hStdErrWrite = null;
                }
            }
            else
            {
                // Build the command line for the process.
                OutLaunchArguments(launchInfo, AccountUtilities.CallerUsername, GetCallerEnvironmentDictionary(), out commandLine, out string? workingDirectory);
                var argv = ProcessUtilities.CommandLineToArgv(commandLine);

                // Set up the shell execute info structure.
                var startupInfo = new Shell32.SHELLEXECUTEINFO
                {
                    cbSize = Marshal.SizeOf<Shell32.SHELLEXECUTEINFO>(),
                    fMask = SEE_MASK_FLAGS.SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAGS.SEE_MASK_FLAG_NO_UI | SEE_MASK_FLAGS.SEE_MASK_NOZONECHECKS,
                    lpVerb = launchInfo.Verb,
                    lpFile = argv[0],
                    lpParameters = ProcessUtilities.ArgvToCommandLine(argv.Skip(1)),
                    lpDirectory = workingDirectory,
                };
                if (null != launchInfo.WindowStyle)
                {
                    startupInfo.nShow = (int)launchInfo.WindowStyle.Value;
                }
                if (launchInfo.CreateNoWindow)
                {
                    startupInfo.fMask |= SEE_MASK_FLAGS.SEE_MASK_NO_CONSOLE;
                    startupInfo.nShow = (int)Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_HIDE;
                }

                // Start the process and assign it to the job object if we have a handle.
                Shell32.ShellExecuteEx(ref startupInfo);
                if (startupInfo.hProcess != IntPtr.Zero)
                {
                    hProcess = new SafeProcessHandle(startupInfo.hProcess, true);
                    processId = Kernel32.GetProcessId(hProcess);
                    Kernel32.AssignProcessToJobObject(job, hProcess);
                    if (null != launchInfo.PriorityClass && PrivilegeManager.TestProcessAccessRights(hProcess, PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION))
                    {
                        Kernel32.SetPriorityClass(hProcess, launchInfo.PriorityClass.Value);
                    }
                }
            }

            // If we don't have a process (shell action), return early.
            if (!(null != hProcess && null != processId))
            {
                return null;
            }

            // Get a Process object for the launched process and read its handle so System.Diagnostics gets a lock on it.
            Process process = Process.GetProcessById((int)processId); _ = process; _ = process.Handle;

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
                        launchInfo.CancellationToken?.WaitHandle.SafeWaitHandle.DangerousAddRef(ref ctsAddRef);
                        ReadOnlySpan<HANDLE> handles = null != launchInfo.CancellationToken ? [(HANDLE)iocp.DangerousGetHandle(), (HANDLE)launchInfo.CancellationToken.Value.WaitHandle.SafeWaitHandle.DangerousGetHandle()] : [(HANDLE)iocp.DangerousGetHandle()];
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
                                        tcs.SetResult(new(process, launchInfo, commandLine, ValueTypeConverter<int>.Convert(lpExitCode), stdout, stderr, interleaved));
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
                            launchInfo.CancellationToken!.Value.WaitHandle.SafeWaitHandle.DangerousRelease();
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

            // Return a ProcessHandle object with this process and its running task.
            return new(process, launchInfo, commandLine, tcs.Task);
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
                session = userSessions.FirstOrDefault(s => username.Value.Equals(s.UserName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                session = userSessions.FirstOrDefault(s => s.NTAccount == username);
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
            using (var cProcess = Process.GetProcessById((int)ShellUtilities.GetExplorerProcessId()))
            using (cProcess.SafeHandle)
            {
                AdvApi32.OpenProcessToken(cProcess.SafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hProcessToken);
                using (hProcessToken)
                {
                    if (TokenManager.GetTokenSid(hProcessToken) != AccountUtilities.CallerSid)
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
                    var text = encoding.GetString(buffer, (int)bytesRead).TrimEndRemoveNull();
                    interleaved.Enqueue(text);
                    output.Add(text);
                }
            }
        }

        /// <summary>
        /// Retrieves a read-only dictionary containing the current environment variables.
        /// </summary>
        /// <remarks>The method returns a snapshot of the environment variables at the time of the call.
        /// Subsequent changes to the environment variables will not be reflected in the returned dictionary.</remarks>
        /// <returns>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> where the keys are the names of the environment variables
        /// and the values are their corresponding values as strings.</returns>
        private static ReadOnlyDictionary<string, string> GetCallerEnvironmentDictionary() => new(Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().ToDictionary(static de => de.Key.ToString()!, static de => de.Value!.ToString()!));

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
            return EnvironmentVariableRegex.Replace(input, m => environment.TryGetValue(m.Groups[1].Value, out var envVar) ? envVar : throw new InvalidOperationException($"The user [{ntAccount}] does not have environment variable [{m.Value}] defined or available."));
        }

        /// <summary>
        /// Constructs the command line and working directory for launching a process based on the provided launch
        /// information.
        /// </summary>
        /// <remarks>If <paramref name="launchInfo"/> specifies that environment variables should be
        /// expanded, the method will replace any environment variable placeholders in the file path, arguments, and
        /// working directory with their corresponding values from <paramref name="environmentDictionary"/>.</remarks>
        /// <param name="launchInfo">The information required to launch the process, including file path and arguments.</param>
        /// <param name="username">The user account under which the process will be launched, used for expanding environment variables.</param>
        /// <param name="environmentDictionary">A dictionary of environment variables to be used for expanding variables in the command line and working
        /// directory.</param>
        /// <param name="commandLine">When this method returns, contains the constructed command line string for the process launch.</param>
        /// <param name="workingDirectory">When this method returns, contains the working directory for the process launch, or <see langword="null"/>
        /// if not specified.</param>
        private static void OutLaunchArguments(ProcessLaunchInfo launchInfo, NTAccount username, ReadOnlyDictionary<string, string> environmentDictionary, out string commandLine, out string? workingDirectory)
        {
            string[] argv = (new[] { launchInfo.FilePath }).Concat(launchInfo.ArgumentList ?? []).ToArray();
            if (launchInfo.ExpandEnvironmentVariables)
            {
                for (int i = 0; i < argv.Length; i++)
                {
                    argv[i] = ExpandEnvironmentVariables(username, argv[i], environmentDictionary);
                }
                commandLine = ProcessUtilities.ArgvToCommandLine(argv)!;
                workingDirectory = null != launchInfo.WorkingDirectory ? ExpandEnvironmentVariables(username, launchInfo.WorkingDirectory, environmentDictionary) : null;
            }
            else
            {
                commandLine = ProcessUtilities.ArgvToCommandLine(argv)!;
                workingDirectory = launchInfo.WorkingDirectory;
            }
        }

        /// <summary>
        /// Determines whether the current process can use the CreateProcessAsUser function.
        /// </summary>
        /// <remarks>This method checks if the current process has the necessary privileges and conditions
        /// to use the CreateProcessAsUser function. It verifies the presence of specific privileges and evaluates
        /// whether the process is part of a job object that allows breakaway.</remarks>
        /// <returns><see langword="true"/> if the process can use CreateProcessAsUser; otherwise, <see langword="false"/>.</returns>
        private static CreateProcessUsingTokenStatus CanUseCreateProcessAsUser()
        {
            // Test whether the caller has the required privileges to use CreateProcessAsUser.
            if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeIncreaseQuotaPrivilege))
            {
                return CreateProcessUsingTokenStatus.SeIncreaseQuotaPrivilege;
            }
            if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege))
            {
                return CreateProcessUsingTokenStatus.SeAssignPrimaryTokenPrivilege;
            }

            // Test whether the process is part of an existing job object.
            using (var cProcess = Process.GetCurrentProcess())
            using (cProcess.SafeHandle)
            {
                Kernel32.IsProcessInJob(cProcess.SafeHandle, null, out var inJob);
                if (!inJob)
                {
                    return CreateProcessUsingTokenStatus.OK;
                }
            }

            // Since we're part of a job object, we need to check if the job has the JOB_OBJECT_LIMIT_BREAKAWAY_OK flag set.
            using (var lpJobObjectInformation = SafeHGlobalHandle.Alloc(Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
            {
                Kernel32.QueryInformationJobObject(null, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, lpJobObjectInformation, out _);
                var jobFlags = lpJobObjectInformation.ToStructure<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>().BasicLimitInformation.LimitFlags;
                if (!(jobFlags.HasFlag(JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK) || jobFlags.HasFlag(JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_BREAKAWAY_OK)))
                {
                    return CreateProcessUsingTokenStatus.JobBreakawayNotPermitted;
                }
            }

            // If we're here, everything we need to be able to use CreateProcessAsUser() is available.
            return CreateProcessUsingTokenStatus.OK;
        }

        /// <summary>
        /// Determines whether the current process has the necessary privileges to use the CreateProcessWithToken
        /// function.
        /// </summary>
        /// <returns><see langword="true"/> if the current process has the SeImpersonatePrivilege; otherwise, <see
        /// langword="false"/>.</returns>
        private static CreateProcessUsingTokenStatus CanUseCreateProcessWithToken()
        {
            // Test whether the caller has the required privileges to use CreateProcessWithToken.
            if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeImpersonatePrivilege))
            {
                return CreateProcessUsingTokenStatus.SeImpersonatePrivilege;
            }

            // Test whether the "Secondary Log-on" service is running, which is required for CreateProcessWithToken.
            using (ServiceController serviceController = new("seclogon"))
            {
                // If the service does not exist or cannot be queried, we cannot use CreateProcessWithToken.
                try
                {
                    if (serviceController.Status != ServiceControllerStatus.Running)
                    {
                        return CreateProcessUsingTokenStatus.SecLogonServiceNotRunning;
                    }
                }
                catch (InvalidOperationException)
                {
                    return CreateProcessUsingTokenStatus.SecLogonServiceNotFound;
                }
            }

            // If we're here, everything we need to be able to use CreateProcessWithToken() is available.
            return CreateProcessUsingTokenStatus.OK;
        }

        /// <summary>
        /// Creates a new process using the specified primary token and command line.
        /// </summary>
        /// <remarks>This method attempts to create a process using <c>CreateProcessAsUser</c> if
        /// possible, falling back to <c>CreateProcessWithToken</c> if necessary. It requires specific privileges to be
        /// enabled, such as <c>SeIncreaseQuotaPrivilege</c> and <c>SeAssignPrimaryTokenPrivilege</c>.</remarks>
        /// <param name="hPrimaryToken">The primary token representing the user context under which the process will be created.</param>
        /// <param name="commandLine">The command line to be executed by the new process.</param>
        /// <param name="creationFlags">Flags that control the priority class and the creation of the process.</param>
        /// <param name="lpEnvironment">A handle to the environment block for the new process. Can be <see langword="null"/> to use the environment
        /// of the calling process.</param>
        /// <param name="workingDirectory">The full path to the current directory for the process. Can be <see langword="null"/> to use the current
        /// directory of the calling process.</param>
        /// <param name="startupInfo">A reference to a <see cref="STARTUPINFOW"/> structure that specifies the window station, desktop, standard
        /// handles, and appearance of the main window for the new process.</param>
        /// <param name="pi">When this method returns, contains a <see cref="PROCESS_INFORMATION"/> structure with information about the
        /// newly created process and its primary thread.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if the calling user account does not have the necessary privileges to create a process using the
        /// specified token.</exception>
        private static void CreateProcessUsingToken(SafeHandle hPrimaryToken, string commandLine, bool usingAnonymousHandles, PROCESS_CREATION_FLAGS creationFlags, SafeEnvironmentBlockHandle lpEnvironment, string? workingDirectory, in STARTUPINFOW startupInfo, out PROCESS_INFORMATION pi)
        {
            // Attempt to use CreateProcessAsUser() first as it's gold standard, otherwise fall back to CreateProcessWithToken().
            // When the caller provides anonymous handles, we need to use CreateProcessAsUser() since it has bInheritHandles.
            if (CanUseCreateProcessAsUser() is CreateProcessUsingTokenStatus canUseCreateProcessAsUser && (canUseCreateProcessAsUser == CreateProcessUsingTokenStatus.OK || usingAnonymousHandles))
            {
                // If the parent process is associated with an existing job object, using the CREATE_BREAKAWAY_FROM_JOB flag can help
                // with E_ACCESSDENIED errors from CreateProcessAsUser() as processes in a job all need to be in the same session.
                // The use of this flag has effect if the parent is part of a job and that job has JOB_OBJECT_LIMIT_BREAKAWAY_OK set.
                if (canUseCreateProcessAsUser != CreateProcessUsingTokenStatus.OK)
                {
                    throw new InvalidOperationException($"Unable to create a new process using CreateProcessAsUser(): {CreateProcessUsingTokenStatusMessages[canUseCreateProcessAsUser]}");
                }
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);
                AdvApi32.CreateProcessAsUser(hPrimaryToken, null, commandLine, null, null, true, creationFlags | PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB, lpEnvironment, workingDirectory, startupInfo, out pi);
            }
            else if (CanUseCreateProcessWithToken() is CreateProcessUsingTokenStatus canUseCreateProcessWithToken && canUseCreateProcessWithToken == CreateProcessUsingTokenStatus.OK)
            {
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeImpersonatePrivilege);
                AdvApi32.CreateProcessWithToken(hPrimaryToken, CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE, null, commandLine, creationFlags, lpEnvironment, workingDirectory, startupInfo, out pi);
            }
            else
            {
                throw new InvalidOperationException($"Unable to create a new process via token. CreateProcessAsUser() reason: {CreateProcessUsingTokenStatusMessages[canUseCreateProcessAsUser]}CreateProcessWithToken() reason: {CreateProcessUsingTokenStatusMessages[canUseCreateProcessWithToken]}");
            }
        }

        /// <summary>
        /// Represents the status codes returned by the process creation operation using a token.
        /// </summary>
        /// <remarks>This enumeration provides specific status codes that indicate the result of
        /// attempting to create a process using a token. Each value corresponds to a particular condition or
        /// requirement related to privilege or service availability necessary for the operation.</remarks>
        private enum CreateProcessUsingTokenStatus
        {
            OK,
            SeIncreaseQuotaPrivilege,
            SeAssignPrimaryTokenPrivilege,
            JobBreakawayNotPermitted,
            SeImpersonatePrivilege,
            SecLogonServiceNotRunning,
            SecLogonServiceNotFound,
        }

        /// <summary>
        /// Provides a read-only dictionary mapping <see cref="CreateProcessUsingTokenStatus"/> values to their
        /// corresponding error messages.
        /// </summary>
        /// <remarks>This dictionary contains predefined error messages for various statuses encountered
        /// when attempting to create a process using a token. It is used to provide descriptive error messages based on
        /// the status code returned by the operation.</remarks>
        private static readonly ReadOnlyDictionary<CreateProcessUsingTokenStatus, string> CreateProcessUsingTokenStatusMessages = new(new Dictionary<CreateProcessUsingTokenStatus, string>
        {
            { CreateProcessUsingTokenStatus.SeIncreaseQuotaPrivilege, "The calling process does not have the SeIncreaseQuotaPrivilege privilege." },
            { CreateProcessUsingTokenStatus.SeAssignPrimaryTokenPrivilege, "The calling process does not have the SeAssignPrimaryTokenPrivilege privilege." },
            { CreateProcessUsingTokenStatus.JobBreakawayNotPermitted, "The calling process is part of a job that does not allow breakaway." },
            { CreateProcessUsingTokenStatus.SeImpersonatePrivilege, "The calling process does not have the SeImpersonatePrivilege privilege." },
            { CreateProcessUsingTokenStatus.SecLogonServiceNotRunning, "The system's Secondary Log-on service is not running." },
            { CreateProcessUsingTokenStatus.SecLogonServiceNotFound, "The system's Secondary Log-on service could not be found." },
        });

        /// <summary>
        /// Represents a compiled, culture-invariant regular expression used to match environment variable patterns.
        /// </summary>
        /// <remarks>The pattern matches strings enclosed in percent signs, such as "%VARIABLE%". This
        /// regex is compiled for performance and is culture-invariant to ensure consistent behavior across different
        /// cultures.</remarks>
        private static readonly Regex EnvironmentVariableRegex = new(@"%([^%]+)%", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Special exit code used to signal when we're terminating a process due to timeout.
        /// The value is `'PSAppDeployToolkit'.GetHashCode()` under Windows PowerShell 5.1.
        /// </summary>
        public const int TimeoutExitCode = -443991205;
    }
}
