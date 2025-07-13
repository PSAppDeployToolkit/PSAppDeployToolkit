using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
using Windows.Win32.System.JobObjects;
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
                    if (null != launchInfo.Username && launchInfo.Username != AccountUtilities.CallerUsername && GetSessionForUsername(launchInfo.Username) is SessionInfo session)
                    {
                        // Enable the required privileges.
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);

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

                                // If the parent process is associated with an existing job object, using the CREATE_BREAKAWAY_FROM_JOB flag can help
                                // with E_ACCESSDENIED errors from CreateProcessAsUser() as processes in a job all need to be in the same session.
                                // The use of this flag has effect if the parent is part of a job and that job has JOB_OBJECT_LIMIT_BREAKAWAY_OK set.
                                SetupStreamPipes(); AdvApi32.CreateProcessAsUser(hPrimaryToken, null, commandLine, null, null, true, creationFlags | PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB, lpEnvironment, workingDirectory, startupInfo, out pi);
                            }
                        }
                    }
                    else if (launchInfo.UseUnelevatedToken && AccountUtilities.CallerIsAdmin && !AccountUtilities.CallerIsLocalSystem)
                    {
                        // We're running elevated but have been asked to de-elevate.
                        using (var hPrimaryToken = GetUnelevatedToken())
                        {
                            if ((startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                            {
                                // Throw if the caller is expecting to be able to capture stdout/stderr but doesn't have the privileges for CreateProcessAsUser().
                                try
                                {
                                    PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                                    PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);
                                }
                                catch (Exception ex)
                                {
                                    throw new UnauthorizedAccessException("Failed to enable one or more privileges needed to create a process with an unelevated token and stream capture.", ex);
                                }
                                SetupStreamPipes(); AdvApi32.CreateProcessAsUser(hPrimaryToken, null, launchInfo.CommandLine, null, null, true, creationFlags | PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB, SafeEnvironmentBlockHandle.Null, launchInfo.WorkingDirectory, startupInfo, out pi);
                            }
                            else
                            {
                                AdvApi32.CreateProcessWithToken(hPrimaryToken, CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE, null, launchInfo.CommandLine, creationFlags, SafeEnvironmentBlockHandle.Null, launchInfo.WorkingDirectory, startupInfo, out pi);
                            }
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
                    hStdOutWrite = null;
                    hStdErrWrite = null;
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

            // Get a Process object for the launched process and read its handle so System.Diagnostics gets a lock on it.
            Process process = Process.GetProcessById((int)processId); _ = process; _ = process.Handle;
            ProcessModule mainModule = process.MainModule!;

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
                                        tcs.SetResult(new(process, mainModule, launchInfo, ValueTypeConverter<int>.Convert(lpExitCode), stdout.AsReadOnly(), stderr.AsReadOnly(), interleaved.ToList().AsReadOnly()));
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

            // Return a ProcessHandle object with this process and its running task.
            return new(process, mainModule, launchInfo, tcs.Task);
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
        /// Special exit code used to signal when we're terminating a process due to timeout.
        /// The value is `'PSAppDeployToolkit'.GetHashCode()` under Windows PowerShell 5.1.
        /// </summary>
        public const int TimeoutExitCode = -443991205;
    }
}
