using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;
using PSADT.AccessToken;
using PSADT.LibraryInterfaces;
using PSADT.Types;
using PSADT.Shared;
using PSADT.WTSSession;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Provides methods for launching processes with more control over input/output.
    /// </summary>
    public static class ProcessExecutor
    {
        /// <summary>
        /// Launches a process with the specified start info and waits for it to complete.
        /// </summary>
        /// <param name="startInfo"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<ProcessResult> LaunchAsync(ProcessOptions startInfo)
        {
            // Declare all variables C-style so we can close them in the finally block.
            IntPtr lpEnvironment = IntPtr.Zero;
            HANDLE hPrimaryToken = default;
            HANDLE hStdOutWrite = default;
            HANDLE hStdErrWrite = default;
            HANDLE hStdOutRead = default;
            HANDLE hStdErrRead = default;
            HANDLE hProcess = default;
            HANDLE iocp = default;
            HANDLE job = default;

            // Determine whether the process we're starting is a console app or not.
            bool noWindow = startInfo.NoNewWindow || ((SHOW_WINDOW_CMD)startInfo.WindowStyle == SHOW_WINDOW_CMD.SW_HIDE);
            bool consoleApp;
            try
            {
                consoleApp = GeneralUtilities.GetExecutableInfo(startInfo.FilePath).ExecutableType == ExecutableType.Console;
            }
            catch
            {
                consoleApp = false;
            }

            try
            {
                iocp = Kernel32.CreateIoCompletionPort(HANDLE.INVALID_HANDLE_VALUE, HANDLE.Null, UIntPtr.Zero, 1);
                job = Kernel32.CreateJobObject(out _, default);

                var assoc = new JOBOBJECT_ASSOCIATE_COMPLETION_PORT
                {
                    CompletionPort = iocp,
                    CompletionKey = null,
                };
                Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, ref assoc, (uint)Marshal.SizeOf<JOBOBJECT_ASSOCIATE_COMPLETION_PORT>());

                if ((consoleApp && noWindow) || !startInfo.UseShellExecute || (null != startInfo.Username))
                {
                    CreatePipe(out hStdOutRead, out hStdOutWrite);
                    CreatePipe(out hStdErrRead, out hStdErrWrite);

                    var pi = new PROCESS_INFORMATION();
                    var startupInfo = new STARTUPINFOW
                    {
                        cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                        hStdOutput = hStdOutWrite,
                        hStdError = hStdErrWrite,
                    };

                    var creationFlags = PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                        PROCESS_CREATION_FLAGS.CREATE_SUSPENDED |
                        PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP;

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

                    if (null != startInfo.Username)
                    {
                        using (WindowsIdentity caller = WindowsIdentity.GetCurrent())
                        {
                            if (!caller.User!.IsWellKnown(WellKnownSidType.LocalSystemSid))
                            {
                                throw new UnauthorizedAccessException("Launching processes as other users is only supported when running as SYSTEM.");
                            }
                        }

                        PrivilegeManager.EnsurePrivilegeEnabled(SE_TOKEN.SeIncreaseQuotaPrivilege);
                        PrivilegeManager.EnsurePrivilegeEnabled(SE_TOKEN.SeAssignPrimaryTokenPrivilege);

                        var userSessions = SessionManager.GetSessionInfo();
                        if (userSessions.Count == 0)
                        {
                            throw new InvalidOperationException("No user sessions are available to launch the process in.");
                        }

                        var session = userSessions.Where(s => (null != s.UserName) && s.UserName.Equals(startInfo.Username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (null == session)
                        {
                            throw new InvalidOperationException($"No session found for user {startInfo.Username}.");
                        }
                        if (session.ConnectState != LibraryInterfaces.WTS_CONNECTSTATE_CLASS.WTSActive)
                        {
                            throw new InvalidOperationException($"The session for user {startInfo.Username} is not active.");
                        }

                        WtsApi32.WTSQueryUserToken(session.SessionId, out var userToken);
                        try
                        {
                            AdvApi32.DuplicateTokenEx(userToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hPrimaryToken);
                        }
                        finally
                        {
                            Kernel32.CloseHandle(ref userToken);
                        }

                        startupInfo.lpDesktop = new PWSTR(Marshal.StringToCoTaskMemUni("winsta0\\default"));
                        UserEnv.CreateEnvironmentBlock(out lpEnvironment, hPrimaryToken, true);
                        Kernel32.CreateProcessAsUser(hPrimaryToken, null, startInfo.GetCreateProcessCommandLine(), null, null, true, creationFlags, lpEnvironment, startInfo.WorkingDirectory, startupInfo, out pi);
                    }
                    else
                    {
                        Kernel32.CreateProcess(null, startInfo.GetCreateProcessCommandLine(), null, null, true, creationFlags, lpEnvironment, startInfo.WorkingDirectory, startupInfo, out pi);
                    }

                    Kernel32.AssignProcessToJobObject(job, (hProcess = pi.hProcess));
                    Kernel32.ResumeThread(pi.hThread);
                    Kernel32.CloseHandle(ref pi.hThread);
                    Kernel32.CloseHandle(ref hStdOutWrite);
                    Kernel32.CloseHandle(ref hStdErrWrite);
                }
                else
                {
                    var startupInfo = new Shell32.SHELLEXECUTEINFO
                    {
                        cbSize = Marshal.SizeOf<Shell32.SHELLEXECUTEINFO>(),
                        fMask = SEE_MASK_FLAGS.SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAGS.SEE_MASK_FLAG_NO_UI,
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
                    Shell32.ShellExecuteEx(ref startupInfo);
                    if (startupInfo.hProcess != IntPtr.Zero)
                    {
                        Kernel32.AssignProcessToJobObject(job, (hProcess = (HANDLE)startupInfo.hProcess));
                    }
                }

                List<string> stdout = []; List<string> stderr = [];
                Task readOut = (hStdOutRead != default) ? Task.Run(() => ReadPipe(hStdOutRead, stdout, startInfo.CancellationToken)) : Task.CompletedTask;
                Task readErr = (hStdErrRead != default) ? Task.Run(() => ReadPipe(hStdErrRead, stderr, startInfo.CancellationToken)) : Task.CompletedTask;
                Task waitForJob = (hProcess == default) ? Task.CompletedTask : Task.Run(() =>
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
                            throw new TaskCanceledException();
                        }
                    }
                });
                await Task.WhenAll(waitForJob, readOut, readErr);

                uint exitCode = 0;
                if (hProcess != default)
                {
                    Kernel32.GetExitCodeProcess(hProcess, out exitCode);
                }
                return new ProcessResult(ValueTypeConverter<int>.Convert(exitCode), stdout.AsReadOnly(), stderr.AsReadOnly());
            }
            finally
            {
                UserEnv.DestroyEnvironmentBlock(lpEnvironment);
                Kernel32.CloseHandle(ref hPrimaryToken);
                Kernel32.CloseHandle(ref hStdOutWrite);
                Kernel32.CloseHandle(ref hStdErrWrite);
                Kernel32.CloseHandle(ref hStdOutRead);
                Kernel32.CloseHandle(ref hStdErrRead);
                Kernel32.CloseHandle(ref hProcess);
                Kernel32.CloseHandle(ref iocp);
                Kernel32.CloseHandle(ref job);
            }
        }

        /// <summary>
        /// Launches a process with the specified start info and waits for it to complete.
        /// </summary>
        /// <param name="startInfo"></param>
        /// <returns></returns>
        public static ProcessResult Launch(ProcessOptions startInfo)
        {
            return LaunchAsync(startInfo).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a pipe for reading or writing.
        /// </summary>
        /// <param name="readPipe"></param>
        /// <param name="writePipe"></param>
        /// <exception cref="Win32Exception"></exception>
        private static void CreatePipe(out HANDLE readPipe, out HANDLE writePipe)
        {
            Kernel32.CreatePipe(out readPipe, out writePipe, new SECURITY_ATTRIBUTES { nLength = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>(), bInheritHandle = true }, 0);
            Kernel32.SetHandleInformation(readPipe, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, 0);
        }

        /// <summary>
        /// Reads from a pipe until the pipe is closed or the token is cancelled.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="output"></param>
        /// <param name="token"></param>
        /// <exception cref="Win32Exception"></exception>
        private static void ReadPipe(HANDLE handle, List<string> output, CancellationToken token)
        {
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                Kernel32.ReadFile(handle, buffer, out var bytesRead, out _);
                if (bytesRead == 0)
                {
                    break;
                }
                output.Add(Encoding.Default.GetString(buffer, 0, (int)bytesRead).TrimEnd());
            }
        }
    }
}
