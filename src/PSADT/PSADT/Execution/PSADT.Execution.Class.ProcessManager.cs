using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using PSADT.LibraryInterfaces;
using PSADT.Security;
using PSADT.TerminalServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Execution
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
        public static async Task<ProcessResult?> LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            // Declare all handles C-style so we can close them in the finally block for cleanup.
            HANDLE hStdOutRead = default;
            HANDLE hStdErrRead = default;
            HANDLE hProcess = default;
            HANDLE iocp = default;
            HANDLE job = default;
            uint? processId = null;
            int? exitCode = null;

            // Lists for output streams to be read into.
            ConcurrentQueue<string> interleaved = [];
            List<string> stdout = [];
            List<string> stderr = [];

            // Tasks for reading the output streams.
            var stdOutTask = Task.CompletedTask;
            var stdErrTask = Task.CompletedTask;

            // Determine whether the process we're starting is a console app or not. This is important
            // because under ShellExecuteEx() invocations, stdout/stderr will attach to the running console.
            bool guiApp;
            try
            {
                guiApp = ExecutableUtilities.GetExecutableInfo(launchInfo.FilePath).ExecutableType == ExecutableType.GUI;
            }
            catch
            {
                guiApp = false;
            }

            try
            {
                // Set up the job object and I/O completion port for the process.
                iocp = Kernel32.CreateIoCompletionPort(HANDLE.INVALID_HANDLE_VALUE, HANDLE.Null, UIntPtr.Zero, 1);
                job = Kernel32.CreateJobObject(null, default);
                Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, new JOBOBJECT_ASSOCIATE_COMPLETION_PORT { CompletionPort = iocp, CompletionKey = null });

                // We only let console apps run via ShellExecuteEx() when there's a window shown for it.
                // Invoking processes as user has no ShellExecute capability, so it always comes through here.
                if ((!guiApp && launchInfo.CreateNoWindow) || !launchInfo.UseShellExecute || (null != launchInfo.Username))
                {
                    var startupInfo = new STARTUPINFOW
                    {
                        cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                        dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW,
                        wShowWindow = launchInfo.WindowStyle,
                    };
                    try
                    {
                        // The process is created suspended so it can be assigned to the job object.
                        var creationFlags = (PROCESS_CREATION_FLAGS)launchInfo.PriorityClass |
                            PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                            PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP |
                            PROCESS_CREATION_FLAGS.CREATE_SUSPENDED;

                        // We must create a console window for console apps when the window is shown.
                        if (!guiApp)
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
                        else
                        {
                            creationFlags |= PROCESS_CREATION_FLAGS.DETACHED_PROCESS;
                        }

                        // If we're to read the output, we create pipes for stdout and stderr.
                        if ((startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                        {
                            CreatePipe(out hStdOutRead, out startupInfo.hStdOutput);
                            CreatePipe(out hStdErrRead, out startupInfo.hStdError);
                            stdOutTask = Task.Run(() => ReadPipe(hStdOutRead, stdout, interleaved));
                            stdErrTask = Task.Run(() => ReadPipe(hStdErrRead, stderr, interleaved));
                        }

                        // Handle user process creation, otherwise just create the process for the running user.
                        var pi = new PROCESS_INFORMATION();
                        if (null != launchInfo.Username)
                        {
                            using (WindowsIdentity caller = WindowsIdentity.GetCurrent())
                            {
                                if (!caller.User!.IsWellKnown(WellKnownSidType.LocalSystemSid))
                                {
                                    throw new UnauthorizedAccessException("Launching processes as other users is only supported when running as SYSTEM.");
                                }
                            }

                            // SYSTEM usually has these privileges, but locked down environments via WDAC may require specific enablement.
                            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);

                            // You can only run a process as a user if they're logged on.
                            var userSessions = SessionManager.GetSessionInfo();
                            if (userSessions.Count == 0)
                            {
                                throw new InvalidOperationException("No user sessions are available to launch the process in.");
                            }

                            // You can only run a process as a user if they're active.
                            SessionInfo? session = null;
                            if (!launchInfo.Username.Value.Contains("\\"))
                            {
                                session = userSessions.Where(s => launchInfo.Username.Value.Equals(s.UserName, StringComparison.OrdinalIgnoreCase)).First();
                            }
                            else
                            {
                                session = userSessions.Where(s => s.NTAccount == launchInfo.Username).First();
                            }
                            if (null == session)
                            {
                                throw new InvalidOperationException($"No session found for user {launchInfo.Username}.");
                            }
                            if (session.ConnectState != LibraryInterfaces.WTS_CONNECTSTATE_CLASS.WTSActive)
                            {
                                throw new InvalidOperationException($"The session for user {launchInfo.Username} is not active.");
                            }

                            // First we get the user's token.
                            HANDLE hPrimaryToken = default;
                            try
                            {
                                WtsApi32.WTSQueryUserToken(session.SessionId, out var userToken);
                                try
                                {
                                    // If we're to get their linked token, we get it via their user token.
                                    // Once done, we duplicate the linked token to get a primary token to create the new process.
                                    if (launchInfo.UseLinkedAdminToken)
                                    {
                                        var length = Marshal.SizeOf(typeof(TOKEN_LINKED_TOKEN));
                                        var buffer = Marshal.AllocHGlobal(length);
                                        try
                                        {
                                            AdvApi32.GetTokenInformation(userToken, TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, (uint)length, out _);
                                            AdvApi32.DuplicateTokenEx(Marshal.PtrToStructure<TOKEN_LINKED_TOKEN>(buffer).LinkedToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hPrimaryToken);
                                        }
                                        finally
                                        {
                                            Marshal.FreeHGlobal(buffer);
                                        }
                                    }
                                    else
                                    {
                                        AdvApi32.DuplicateTokenEx(userToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hPrimaryToken);
                                    }
                                }
                                finally
                                {
                                    Kernel32.CloseHandle(ref userToken);
                                }

                                // This is important so that a windowed application can be shown.
                                if (!((creationFlags & PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW) == PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW || (SHOW_WINDOW_CMD)launchInfo.WindowStyle == SHOW_WINDOW_CMD.SW_HIDE))
                                {
                                    startupInfo.lpDesktop = new PWSTR(Marshal.StringToCoTaskMemUni("winsta0\\default"));
                                }

                                // Finally, start the process off for the user.
                                UserEnv.CreateEnvironmentBlock(out var lpEnvironment, hPrimaryToken, launchInfo.InheritEnvironmentVariables);
                                try
                                {
                                    Kernel32.CreateProcessAsUser(hPrimaryToken, null, launchInfo.CommandLine, null, null, true, creationFlags, lpEnvironment, launchInfo.WorkingDirectory, startupInfo, out pi);
                                }
                                finally
                                {
                                    UserEnv.DestroyEnvironmentBlock(ref lpEnvironment);
                                }
                            }
                            finally
                            {
                                Kernel32.CloseHandle(ref hPrimaryToken);
                            }
                        }
                        else
                        {
                            Kernel32.CreateProcess(null, launchInfo.CommandLine, null, null, true, creationFlags, IntPtr.Zero, launchInfo.WorkingDirectory, startupInfo, out pi);
                        }

                        // Start tracking the process and allow it to resume execution.
                        try
                        {
                            Kernel32.AssignProcessToJobObject(job, (hProcess = pi.hProcess));
                            Kernel32.ResumeThread(pi.hThread);
                            processId = pi.dwProcessId;
                        }
                        finally
                        {
                            Kernel32.CloseHandle(ref pi.hThread);
                        }
                    }
                    finally
                    {
                        Kernel32.CloseHandle(ref startupInfo.hStdOutput);
                        Kernel32.CloseHandle(ref startupInfo.hStdError);
                    }
                }
                else
                {
                    var startupInfo = new Shell32.SHELLEXECUTEINFO
                    {
                        cbSize = Marshal.SizeOf<Shell32.SHELLEXECUTEINFO>(),
                        fMask = SEE_MASK_FLAGS.SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAGS.SEE_MASK_FLAG_NO_UI | SEE_MASK_FLAGS.SEE_MASK_NOZONECHECKS,
                        lpFile = launchInfo.FilePath,
                        lpParameters = launchInfo.Arguments,
                        lpDirectory = launchInfo.WorkingDirectory,
                    };
                    if (launchInfo.CreateNoWindow || ((SHOW_WINDOW_CMD)launchInfo.WindowStyle == SHOW_WINDOW_CMD.SW_HIDE))
                    {
                        startupInfo.fMask |= SEE_MASK_FLAGS.SEE_MASK_NO_CONSOLE;
                        startupInfo.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;
                    }
                    else
                    {
                        startupInfo.nShow = launchInfo.WindowStyle;
                    }
                    if (null != launchInfo.Verb)
                    {
                        startupInfo.lpVerb = launchInfo.Verb;
                    }

                    Shell32.ShellExecuteEx(ref startupInfo);
                    if (startupInfo.hProcess != IntPtr.Zero)
                    {
                        hProcess = (HANDLE)startupInfo.hProcess;
                        processId = Kernel32.GetProcessId(hProcess);
                        Kernel32.AssignProcessToJobObject(job, hProcess);
                        if (PrivilegeManager.TestProcessAccessRights(hProcess, PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION))
                        {
                            Kernel32.SetPriorityClass(hProcess, launchInfo.PriorityClass);
                        }
                    }
                }

                // These tasks read all outputs and wait for the process to complete.
                await Task.WhenAll(stdOutTask, stdErrTask, (hProcess == default) ? Task.CompletedTask : Task.Run(() =>
                {
                    ReadOnlySpan<HANDLE> handles = [iocp, (HANDLE)launchInfo.CancellationToken.WaitHandle.SafeWaitHandle.DangerousGetHandle()];
                    while (true)
                    {
                        var index = (uint)Kernel32.WaitForMultipleObjects(handles, false, PInvoke.INFINITE);
                        if (index == 0)
                        {
                            Kernel32.GetQueuedCompletionStatus(iocp, out var lpCompletionCode, out _, out _, PInvoke.INFINITE);
                            if (lpCompletionCode == PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO)
                            {
                                Kernel32.GetExitCodeProcess(hProcess, out var lpExitCode);
                                exitCode = ValueTypeConverter<int>.Convert(lpExitCode);
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
                }));

                // Return a ProcessResult object if this was a real process (i.e. not a shell action).
                if (processId.HasValue)
                {
                    return new ProcessResult(processId.Value, exitCode, stdout.AsReadOnly(), stderr.AsReadOnly(), interleaved.ToList().AsReadOnly());
                }
                return null;
            }
            finally
            {
                Kernel32.CloseHandle(ref hStdOutRead);
                Kernel32.CloseHandle(ref hStdErrRead);
                Kernel32.CloseHandle(ref hProcess);
                Kernel32.CloseHandle(ref iocp);
                Kernel32.CloseHandle(ref job);
            }
        }

        /// <summary>
        /// Creates a pipe for reading or writing.
        /// </summary>
        /// <param name="readPipe"></param>
        /// <param name="writePipe"></param>
        /// <exception cref="Win32Exception"></exception>
        private static void CreatePipe(out HANDLE readPipe, out HANDLE writePipe)
        {
            Kernel32.CreatePipe(out readPipe, out writePipe, new SECURITY_ATTRIBUTES { nLength = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>(), bInheritHandle = true });
            Kernel32.SetHandleInformation(readPipe, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, 0);
        }

        /// <summary>
        /// Reads from a pipe until the pipe is closed or the token is cancelled.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="output"></param>
        /// <param name="token"></param>
        /// <exception cref="Win32Exception"></exception>
        private static void ReadPipe(HANDLE handle, List<string> output, ConcurrentQueue<string> interleaved)
        {
            var buffer = new byte[4096];
            uint bytesRead = 0;
            while (true)
            {
                try
                {
                    Kernel32.ReadFile(handle, buffer, out bytesRead);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_BROKEN_PIPE)
                {
                    break;
                }
                var text = Encoding.Default.GetString(buffer, 0, (int)bytesRead).TrimEnd();
                interleaved.Enqueue(text);
                output.Add(text);
            }
        }

        /// <summary>
        /// Special exit code used to signal when we're terminating a process due to timeout.
        /// The value is `'PSAppDeployToolkit'.GetHashCode()` under Windows PowerShell 5.1.
        /// </summary>
        public const int TimeoutExitCode = -443991205;
    }
}
