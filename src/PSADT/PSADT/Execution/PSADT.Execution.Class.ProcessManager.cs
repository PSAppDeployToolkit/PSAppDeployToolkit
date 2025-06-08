using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using PSADT.Security;
using PSADT.TerminalServices;
using PSADT.Types;
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
        public static ProcessHandle? LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            // Set up the job object and I/O completion port for the process.
            var iocp = Kernel32.CreateIoCompletionPort(SafeBaseHandle.InvalidHandle, SafeBaseHandle.NullHandle, UIntPtr.Zero, 1);
            var job = Kernel32.CreateJobObject(null, default);
            bool iocpAddRefOuter = false;
            try
            {
                // Set up the job object to use the I/O completion port.
                iocp.DangerousAddRef(ref iocpAddRefOuter);
                Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, new JOBOBJECT_ASSOCIATE_COMPLETION_PORT { CompletionPort = (HANDLE)iocp.DangerousGetHandle(), CompletionKey = null });

                // Declare all handles C-style so we can close them in the finally block for cleanup.
                SafeProcessHandle? hProcess = null;
                uint? processId = null;

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
                        var pi = new PROCESS_INFORMATION();
                        if (null != launchInfo.Username)
                        {
                            // Perform initial tests prior to trying to query a user token.
                            using WindowsIdentity caller = WindowsIdentity.GetCurrent();
                            SessionInfo? session = null;

                            // You can only run a process as a user if they're logged on.
                            var userSessions = SessionManager.GetSessionInfo();
                            if (userSessions.Count == 0)
                            {
                                throw new InvalidOperationException("No user sessions are available to launch the process in.");
                            }

                            // You can only run a process as a user if they're active.
                            if (!launchInfo.Username.Value.Contains('\\'))
                            {
                                session = userSessions.First(s => launchInfo.Username.Value.Equals(s.UserName, StringComparison.OrdinalIgnoreCase));
                            }
                            else
                            {
                                session = userSessions.First(s => s.NTAccount == launchInfo.Username);
                            }
                            if (null == session)
                            {
                                throw new InvalidOperationException($"No session found for user {launchInfo.Username}.");
                            }
                            if (session.ConnectState != LibraryInterfaces.WTS_CONNECTSTATE_CLASS.WTSActive)
                            {
                                throw new InvalidOperationException($"The session for user {launchInfo.Username} is not active.");
                            }

                            // We can only run a process as a user if it's different from the caller.
                            if (!session.NTAccount!.Value.Equals(caller.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                // We can only run a process if we can act as part of the operating system.
                                if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeTcbPrivilege))
                                {
                                    throw new UnauthorizedAccessException($"The calling account of [{caller.Name}] does not hold the necessary [SeTcbPrivilege] privilege (Act as part of the operating system) for this operation.");
                                }

                                // Enable the required tokens. SYSTEM usually has these privileges, but locked down environments via WDAC may require specific enablement.
                                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);

                                // First we get the user's token.
                                WtsApi32.WTSQueryUserToken(session.SessionId, out var userToken);
                                SafeFileHandle hPrimaryToken;
                                using (userToken)
                                {
                                    // If we're to get their linked token, we get it via their user token.
                                    // Once done, we duplicate the linked token to get a primary token to create the new process.
                                    if (launchInfo.UseLinkedAdminToken)
                                    {
                                        using (var buffer = SafeHGlobalHandle.Alloc(Marshal.SizeOf<TOKEN_LINKED_TOKEN>()))
                                        {
                                            AdvApi32.GetTokenInformation(userToken, TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, out _);
                                            AdvApi32.DuplicateTokenEx(new SafeAccessTokenHandle(buffer.ToStructure<TOKEN_LINKED_TOKEN>().LinkedToken), TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hPrimaryToken);
                                        }
                                    }
                                    else
                                    {
                                        AdvApi32.DuplicateTokenEx(userToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hPrimaryToken);
                                    }
                                }

                                // Finally, start the process off for the user.
                                using (hPrimaryToken)
                                {
                                    UserEnv.CreateEnvironmentBlock(out var lpEnvironment, hPrimaryToken, launchInfo.InheritEnvironmentVariables);
                                    using (lpEnvironment)
                                    {
                                        // This is important so that a windowed application can be shown.
                                        using (var lpDesktop = SafeCoTaskMemHandle.StringToUni(@"winsta0\default"))
                                        {
                                            startupInfo.lpDesktop = lpDesktop.ToPWSTR();
                                            Kernel32.CreateProcessAsUser(hPrimaryToken, null, launchInfo.CommandLine, null, null, true, creationFlags, lpEnvironment, launchInfo.WorkingDirectory, startupInfo, out pi);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // The caller is the same as the user, so we can just create the process as the current user.
                                Kernel32.CreateProcess(null, launchInfo.CommandLine, null, null, true, creationFlags, SafeEnvironmentBlockHandle.Null, launchInfo.WorkingDirectory, startupInfo, out pi);
                            }
                        }
                        else
                        {
                            // No username was specified, so we're just creating the process as the current user.
                            Kernel32.CreateProcess(null, launchInfo.CommandLine, null, null, true, creationFlags, SafeEnvironmentBlockHandle.Null, launchInfo.WorkingDirectory, startupInfo, out pi);
                        }

                        // Start tracking the process and allow it to resume execution.
                        using (var hThread = new SafeThreadHandle(pi.hThread, true))
                        {
                            Kernel32.AssignProcessToJobObject(job, (hProcess = new SafeProcessHandle(pi.hProcess, true)));
                            Kernel32.ResumeThread(hThread);
                            processId = pi.dwProcessId;
                        }
                    }
                    finally
                    {
                        if (hStdOutWriteAddRef)
                        {
                            hStdOutWrite?.DangerousRelease();
                        }
                        if (hStdErrWriteAddRef)
                        {
                            hStdErrWrite?.DangerousRelease();
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
                    if (launchInfo.CreateNoWindow || ((SHOW_WINDOW_CMD)launchInfo.WindowStyle == SHOW_WINDOW_CMD.SW_HIDE))
                    {
                        startupInfo.fMask |= SEE_MASK_FLAGS.SEE_MASK_NO_CONSOLE;
                        startupInfo.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;
                    }
                    else
                    {
                        startupInfo.nShow = launchInfo.WindowStyle;
                    }

                    // Start the process and assign it to the job object if we have a handle.
                    Shell32.ShellExecuteEx(ref startupInfo);
                    if (startupInfo.hProcess != IntPtr.Zero)
                    {
                        hProcess = new SafeProcessHandle(startupInfo.hProcess, true);
                        processId = Kernel32.GetProcessId(hProcess);
                        Kernel32.AssignProcessToJobObject(job, hProcess);
                        if ((launchInfo.PriorityClass != ProcessPriorityClass.Normal) && PrivilegeManager.TestProcessAccessRights(hProcess, PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION))
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
                var tcs = new TaskCompletionSource<ProcessResult>(TaskCreationOptions.RunContinuationsAsynchronously);
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
                                        if ((lpCompletionCode == PInvoke.JOB_OBJECT_MSG_EXIT_PROCESS && !launchInfo.WaitForChildProcesses && lpOverlapped.ToInt32() == processId) || (lpCompletionCode == PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO))
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
                        }
                    }
                });

                // Return a ProcessResult object with the result of the process.
                return new ProcessHandle(processId, tcs.Task);
            }
            finally
            {
                if (iocpAddRefOuter)
                {
                    iocp.DangerousRelease();
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
        /// The architecture of the current process.
        /// </summary>
        public static readonly SystemArchitecture ProcessArchitecture = ExecutableUtilities.GetExecutableInfo(Process.GetCurrentProcess().MainModule!.FileName).Architecture;

        /// <summary>
        /// Special exit code used to signal when we're terminating a process due to timeout.
        /// The value is `'PSAppDeployToolkit'.GetHashCode()` under Windows PowerShell 5.1.
        /// </summary>
        public const int TimeoutExitCode = -443991205;
    }
}
