using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        /// <param name="startInfo"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<ProcessResult> LaunchAsync(ProcessLaunchInfo startInfo)
        {
            // Declare all handles C-style so we can close them in the finally block for cleanup.
            HANDLE hStdOutRead = default;
            HANDLE hStdErrRead = default;
            HANDLE hProcess = default;
            HANDLE iocp = default;
            HANDLE job = default;

            // Lists for output streams to be read into.
            ConcurrentQueue<string> interleaved = [];
            List<string> stdout = [];
            List<string> stderr = [];

            // Tasks for reading the output streams.
            var stdOutTask = Task.CompletedTask;
            var stdErrTask = Task.CompletedTask;

            // Determine whether the process we're starting is a console app or not. This is important
            // because under ShellExecuteEx() invocations, stdout/stderr will attach to the running console.
            bool noWindow = startInfo.NoNewWindow || ((SHOW_WINDOW_CMD)startInfo.WindowStyle == SHOW_WINDOW_CMD.SW_HIDE);
            bool consoleApp;
            try
            {
                consoleApp = ExecutableUtilities.GetExecutableInfo(startInfo.FilePath).ExecutableType == ExecutableType.Console;
            }
            catch
            {
                consoleApp = false;
            }

            try
            {
                // Set up the job object and I/O completion port for the process.
                iocp = Kernel32.CreateIoCompletionPort(HANDLE.INVALID_HANDLE_VALUE, HANDLE.Null, UIntPtr.Zero, 1);
                job = Kernel32.CreateJobObject(null, default);
                Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, new JOBOBJECT_ASSOCIATE_COMPLETION_PORT { CompletionPort = iocp, CompletionKey = null });

                // We only let console apps run via ShellExecuteEx() when there's a window shown for it.
                // Invoking processes as user has no ShellExecute capability, so it always comes through here.
                if ((consoleApp && noWindow) || !startInfo.UseShellExecute || (null != startInfo.Username))
                {
                    var startupInfo = new STARTUPINFOW { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };
                    try
                    {
                        // The process is created suspended so it can be assigned to the job object.
                        var creationFlags = (PROCESS_CREATION_FLAGS)startInfo.PriorityClass |
                            PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                            PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP |
                            PROCESS_CREATION_FLAGS.CREATE_SUSPENDED;

                        // We must create a console window for console apps when the window is shown.
                        if (!noWindow)
                        {
                            startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                            startupInfo.wShowWindow = startInfo.WindowStyle;
                            if (consoleApp)
                            {
                                creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
                            }
                            else
                            {
                                creationFlags |= PROCESS_CREATION_FLAGS.DETACHED_PROCESS;
                            }
                        }
                        else
                        {
                            startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                            if (!consoleApp)
                            {
                                creationFlags |= PROCESS_CREATION_FLAGS.DETACHED_PROCESS;
                            }
                        }

                        // If we're to read the output, we create pipes for stdout and stderr.
                        if ((startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                        {
                            CreatePipe(out hStdOutRead, out startupInfo.hStdOutput);
                            CreatePipe(out hStdErrRead, out startupInfo.hStdError);
                            stdOutTask = Task.Run(() => ReadPipe(hStdOutRead, stdout, interleaved, startInfo.CancellationToken));
                            stdErrTask = Task.Run(() => ReadPipe(hStdErrRead, stderr, interleaved, startInfo.CancellationToken));
                        }

                        // Handle user process creation, otherwise just create the process for the running user.
                        var pi = new PROCESS_INFORMATION();
                        if (null != startInfo.Username)
                        {
                            using (WindowsIdentity caller = WindowsIdentity.GetCurrent())
                            {
                                if (!caller.User!.IsWellKnown(WellKnownSidType.LocalSystemSid))
                                {
                                    throw new UnauthorizedAccessException("Launching processes as other users is only supported when running as SYSTEM.");
                                }
                            }

                            // SYSTEM usually has these privileges, but locked down environments via WDAC may require specific enablement.
                            PrivilegeManager.EnsurePrivilegeEnabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                            PrivilegeManager.EnsurePrivilegeEnabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);

                            // You can only run a process as a user if they're logged on.
                            var userSessions = SessionManager.GetSessionInfo();
                            if (userSessions.Count == 0)
                            {
                                throw new InvalidOperationException("No user sessions are available to launch the process in.");
                            }

                            // You can only run a process as a user if they're active.
                            var session = userSessions.Where(s => (null != s.UserName) && s.UserName.Equals(startInfo.Username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            if (null == session)
                            {
                                throw new InvalidOperationException($"No session found for user {startInfo.Username}.");
                            }
                            if (session.ConnectState != LibraryInterfaces.WTS_CONNECTSTATE_CLASS.WTSActive)
                            {
                                throw new InvalidOperationException($"The session for user {startInfo.Username} is not active.");
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
                                    if (startInfo.UseLinkedAdminToken)
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
                                if (!noWindow)
                                {
                                    startupInfo.lpDesktop = new PWSTR(Marshal.StringToCoTaskMemUni("winsta0\\default"));
                                }

                                // Finally, start the process off for the user.
                                UserEnv.CreateEnvironmentBlock(out var lpEnvironment, hPrimaryToken, startInfo.InheritEnvironmentVariables);
                                try
                                {
                                    Kernel32.CreateProcessAsUser(hPrimaryToken, null, startInfo.GetCreateProcessCommandLine(), null, null, true, creationFlags, lpEnvironment, startInfo.WorkingDirectory, startupInfo, out pi);
                                }
                                finally
                                {
                                    UserEnv.DestroyEnvironmentBlock(lpEnvironment);
                                }
                            }
                            finally
                            {
                                Kernel32.CloseHandle(ref hPrimaryToken);
                            }
                        }
                        else
                        {
                            Kernel32.CreateProcess(null, startInfo.GetCreateProcessCommandLine(), null, null, true, creationFlags, IntPtr.Zero, startInfo.WorkingDirectory, startupInfo, out pi);
                        }

                        // Start tracking the process and allow it to resume execution.
                        try
                        {
                            Kernel32.AssignProcessToJobObject(job, (hProcess = pi.hProcess));
                            Kernel32.ResumeThread(pi.hThread);
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
                        lpFile = startInfo.FilePath,
                        lpParameters = startInfo.Arguments,
                        lpDirectory = startInfo.WorkingDirectory,
                    };
                    if (noWindow)
                    {
                        startupInfo.fMask |= SEE_MASK_FLAGS.SEE_MASK_NO_CONSOLE;
                        startupInfo.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;
                    }
                    else
                    {
                        startupInfo.nShow = startInfo.WindowStyle;
                    }
                    if (null != startInfo.Verb)
                    {
                        startupInfo.lpVerb = startInfo.Verb;
                    }

                    Shell32.ShellExecuteEx(ref startupInfo);
                    if (startupInfo.hProcess != IntPtr.Zero)
                    {
                        hProcess = (HANDLE)startupInfo.hProcess;
                        Kernel32.SetPriorityClass(hProcess, startInfo.PriorityClass);
                        Kernel32.AssignProcessToJobObject(job, hProcess);
                    }
                }

                // These tasks read all outputs and wait for the process to complete.
                await Task.WhenAll(stdOutTask, stdErrTask, (hProcess == default) ? Task.CompletedTask : Task.Run(() =>
                {
                    while (true)
                    {
                        Kernel32.GetQueuedCompletionStatus(iocp, out var lpCompletionCode, out _, out _, PInvoke.INFINITE);
                        if (lpCompletionCode == PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO)
                        {
                            break;
                        }
                        if (startInfo.CancellationToken.IsCancellationRequested)
                        {
                            startInfo.CancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                }));

                uint exitCode = 0;
                if (hProcess != default)
                {
                    Kernel32.GetExitCodeProcess(hProcess, out exitCode);
                }
                return new ProcessResult(ValueTypeConverter<int>.Convert(exitCode), stdout.AsReadOnly(), stderr.AsReadOnly(), interleaved.ToList().AsReadOnly());
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
        private static void ReadPipe(HANDLE handle, List<string> output, ConcurrentQueue<string> interleaved, CancellationToken token)
        {
            var buffer = new byte[4096];
            uint bytesRead = 0;
            while (!token.IsCancellationRequested)
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
    }
}
