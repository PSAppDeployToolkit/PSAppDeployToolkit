using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Security;
using Windows.Win32.Foundation;
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
        /// Launches a new process asynchronously using the specified launch configuration and returns a handle for
        /// process management and monitoring.
        /// </summary>
        /// <remarks>This method supports advanced process launching scenarios, such as running as a
        /// different user, redirecting standard input/output streams, setting process priority, and managing child
        /// processes. It also allows for process cancellation and custom access control. The returned ProcessHandle
        /// enables monitoring process completion and retrieving output streams asynchronously.</remarks>
        /// <param name="launchInfo">An object that specifies the parameters for launching the process, including command line arguments, window
        /// style, user context, input/output redirection, process priority, and other process control options. Cannot
        /// be null.</param>
        /// <returns>A ProcessHandle object that provides access to the launched process and its associated asynchronous task, or
        /// null if the process could not be started.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="launchInfo"/> is null.</exception>
        public static ProcessHandle? LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            // Only use ShellExecuteEx for non-console apps if we're not capturing stdio.
            ArgumentNullException.ThrowIfNull(launchInfo);
            return launchInfo.UseShellExecute && (!launchInfo.IsCliApplication() || !launchInfo.CreateNoWindow)
                ? LaunchWithShellExecuteExAsync(launchInfo)
                : LaunchWithCreateProcessAsync(launchInfo);
        }

        /// <summary>
        /// Launches a new process asynchronously using the Windows CreateProcess API, applying the specified launch
        /// options and user context.
        /// </summary>
        /// <remarks>This method supports advanced process launch scenarios, such as running under a
        /// different user token, customizing standard input/output/error streams, and controlling window and console
        /// behavior. The process is initially created in a suspended state and then resumed after setup. Callers are
        /// responsible for disposing of the returned ProcessHandle to release system resources.</remarks>
        /// <param name="launchInfo">An object containing the configuration and parameters for the process to be launched, including file path,
        /// arguments, user context, environment variables, and standard stream handling.</param>
        /// <returns>A handle to the launched process, encapsulated in a ProcessHandle object, which provides access to process
        /// state and standard streams as configured.</returns>
        private static ProcessHandle LaunchWithCreateProcessAsync(ProcessLaunchInfo launchInfo)
        {
            // Perform initial setup and get started with the process creation.
            ProcessReadStream? stdOutHandle = null, stdErrHandle = null; ProcessWriteStream? stdInHandle = null;
            AnonymousPipeServerStream? stdOutStream = null, stdErrStream = null, stdInStream = null;
            ReadOnlyCollection<SE_PRIVILEGE> callerPrivileges = PrivilegeManager.GetPrivileges();
            Span<char> commandSpan = launchInfo.MakeCommandLine(true).ToCharArray();
            SafeProcessHandle hProcess; SafeThreadHandle hThread; uint processId;
            ConcurrentQueue<string> interleavedData = [];
            try
            {
                // Set up the startup information for the process.
                PROCESS_INFORMATION pi; STARTUPINFOW startupInfo = new()
                {
                    cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                };
                PROCESS_CREATION_FLAGS creationFlags = ((PROCESS_CREATION_FLAGS?)launchInfo.PriorityClass ?? 0) |
                    PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                    PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP |
                    PROCESS_CREATION_FLAGS.CREATE_SEPARATE_WOW_VDM |
                    PROCESS_CREATION_FLAGS.CREATE_SUSPENDED;

                // Set up the window style if the caller's provided a value.
                if (launchInfo.WindowStyle is not null)
                {
                    startupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                    startupInfo.wShowWindow = (ushort)launchInfo.WindowStyle.Value;
                }

                // We must create a console window for console apps when the window is shown.
                if (launchInfo.IsCliApplication())
                {
                    if (launchInfo.CreateNoWindow)
                    {
                        // If STARTF_USESHOWWINDOW is set, a console app showing UI elements
                        // won't appear. Because we have CREATE_NO_WINDOW, the console window
                        // (aka. the window we actually want hidden) will be hidden as expected.
                        startupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                        startupInfo.dwFlags &= ~STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                        creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                    }
                    else
                    {
                        creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
                    }
                }

                // Set up required stdio stuff if we're configured to capture these streams.
                List<nint> handlesToInherit = [.. launchInfo.HandlesToInherit]; bool hasExternalHandles = handlesToInherit.Count > 0;
                if ((startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                {
                    if (launchInfo.StandardInput.Count > 0)
                    {
                        (stdInStream, startupInfo.hStdInput, stdInHandle) = CreateWritePipe(launchInfo.StandardInput, launchInfo.StreamEncoding);
                        handlesToInherit.Add(startupInfo.hStdInput);
                    }
                    (stdOutStream, startupInfo.hStdOutput, stdOutHandle) = CreateReadPipe(interleavedData, launchInfo.StreamEncoding);
                    (stdErrStream, startupInfo.hStdError, stdErrHandle) = CreateReadPipe(interleavedData, launchInfo.StreamEncoding);
                    handlesToInherit.Add(startupInfo.hStdOutput);
                    handlesToInherit.Add(startupInfo.hStdError);
                }

                // Attempt to launch the process with the specified user's token if the necessary information was provided, otherwise just directly create the process.
                if (launchInfo.RunAsActiveUser is not null && (launchInfo.RunAsActiveUser != AccountUtilities.CallerRunAsActiveUser || (AccountUtilities.CallerIsAdmin && CanUseCreateProcessAsUser(true, callerPrivileges) == CreateProcessUsingTokenStatus.OK)))
                {
                    // Start the process with the user's token. Without creating an environment block, the process will take on the environment of the SYSTEM account.
                    using SafeFileHandle hPrimaryToken = TokenManager.GetUserPrimaryToken(launchInfo.RunAsActiveUser.SessionId, launchInfo.ElevatedTokenType, launchInfo.FilePath == ClientServerUtilities.ClientPath.FullName || launchInfo.FilePath == ClientServerUtilities.ClientLauncherPath.FullName);
                    _ = NativeMethods.CreateEnvironmentBlock(out SafeEnvironmentBlockHandle lpEnvironment, hPrimaryToken, launchInfo.InheritEnvironmentVariables);
                    using (lpEnvironment)
                    {
                        unsafe
                        {
                            fixed (char* pDesktop = @"winsta0\default")
                            {
                                startupInfo.lpDesktop = new(pDesktop);
                                _ = CreateProcessUsingToken(hPrimaryToken, callerPrivileges, launchInfo.FilePath, ref commandSpan, handlesToInherit, hasExternalHandles, creationFlags, lpEnvironment, launchInfo.WorkingDirectory?.FullName, launchInfo.RunAsInvoker, in startupInfo, out pi);
                            }
                        }
                    }
                }
                else if (AccountUtilities.CallerIsAdmin && ((launchInfo.RunAsActiveUser == AccountUtilities.CallerRunAsActiveUser && launchInfo.ElevatedTokenType == ElevatedTokenType.None) || launchInfo.UseUnelevatedToken))
                {
                    // We're running elevated but have been asked to de-elevate.
                    if (!AccountUtilities.CallerIsLoggedOnUser)
                    {
                        throw new InvalidOperationException("Cannot create process using unelevated token when running in a different user's session.");
                    }
                    using SafeFileHandle hPrimaryToken = TokenManager.GetUserPrimaryToken(AccountUtilities.CallerSessionId);
                    _ = CreateProcessUsingToken(hPrimaryToken, callerPrivileges, launchInfo.FilePath, ref commandSpan, handlesToInherit, hasExternalHandles, creationFlags, null, launchInfo.WorkingDirectory?.FullName, launchInfo.RunAsInvoker, in startupInfo, out pi);
                }
                else
                {
                    // No username was specified and we weren't asked to de-elevate, so we're just creating the process as this current user as-is.
                    if (launchInfo.RunAsInvoker || handlesToInherit.Count > 0)
                    {
                        (STARTUPINFOEXW startupInfoEx, SafeProcThreadAttributeListHandle hAttributeList) = CreateStartupInfoEx(in startupInfo, handlesToInherit, forceBreakaway: false, launchInfo.RunAsInvoker, out SafePinnedGCHandle? pinnedHandles);
                        using (hAttributeList)
                        using (pinnedHandles)
                        {
                            _ = NativeMethods.CreateProcess(launchInfo.FilePath, ref commandSpan, null, null, true, creationFlags | PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT, null, launchInfo.WorkingDirectory?.FullName, in startupInfoEx, out pi);
                        }
                    }
                    else
                    {
                        _ = NativeMethods.CreateProcess(launchInfo.FilePath, ref commandSpan, null, null, false, creationFlags, null, launchInfo.WorkingDirectory?.FullName, in startupInfo, out pi);
                    }
                }
                hProcess = new(pi.hProcess, true);
                hThread = new(pi.hThread, true);
                processId = pi.dwProcessId;
            }
            catch (Exception ex)
            {
                stdOutHandle?.Dispose();
                stdOutStream?.Dispose();
                stdErrHandle?.Dispose();
                stdErrStream?.Dispose();
                stdInHandle?.Dispose();
                stdInStream?.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
            finally
            {
                stdOutStream?.DisposeLocalCopyOfClientHandle();
                stdErrStream?.DisposeLocalCopyOfClientHandle();
                stdInStream?.DisposeLocalCopyOfClientHandle();
            }

            // Finalise the process creation and return the handle to the caller.
            try
            {
                Process process = Process.GetProcessById((int)processId);
                try
                {
                    if (stdOutHandle is not null && stdErrHandle is not null)
                    {
                        stdOutHandle.Task.Start(TaskScheduler.Default);
                        stdErrHandle.Task.Start(TaskScheduler.Default);
                        using (hThread)
                        {
                            _ = NativeMethods.ResumeThread(hThread);
                        }
                        stdInHandle?.Task.Start(TaskScheduler.Default);
                        return new(new(launchInfo, process, commandSpan.ToString(), callerPrivileges, processId, hProcess, stdOutHandle, stdErrHandle, interleavedData, stdInHandle));
                    }
                    else
                    {
                        using (hThread)
                        {
                            _ = NativeMethods.ResumeThread(hThread);
                        }
                        return new(new(launchInfo, process, commandSpan.ToString(), callerPrivileges, processId, hProcess));
                    }
                }
                catch (Exception ex)
                {
                    process.Dispose();
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
            catch (Exception ex)
            {
                stdOutHandle?.Dispose();
                stdOutStream?.Dispose();
                stdErrHandle?.Dispose();
                stdErrStream?.Dispose();
                stdInHandle?.Dispose();
                stdInStream?.Dispose();
                hProcess.Dispose();
                hThread.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        /// <summary>
        /// Starts a new process using ShellExecuteEx with the specified launch parameters and returns a handle to the
        /// created process, or null if the operation is a pure shell action.
        /// </summary>
        /// <remarks>If the process cannot be started or an error occurs during initialization, an
        /// exception is thrown. The caller is responsible for disposing of the returned ProcessHandle when it is no
        /// longer needed.</remarks>
        /// <param name="launchInfo">An object containing the parameters required to launch the process, including file path, arguments, working
        /// directory, window style, and other process options.</param>
        /// <returns>A handle to the started process if the process was successfully created; otherwise, null if the operation
        /// was a pure shell action and no process was started.</returns>
        /// <exception cref="NotSupportedException">Thrown if the RunAsActiveUser property of launchInfo is set, as running as a different user is not supported
        /// with ShellExecuteEx.</exception>
        private static ProcessHandle? LaunchWithShellExecuteExAsync(ProcessLaunchInfo launchInfo)
        {
            // Throw if RunAsActiveUser is populated as it's not supported.
            if (!(launchInfo.RunAsActiveUser is null || launchInfo.RunAsActiveUser == AccountUtilities.CallerRunAsActiveUser))
            {
                throw new NotSupportedException("Running as a different user is not supported with ShellExecuteEx.");
            }

            // Set up the process object and start it.
            Process process = new();
            try
            {
                process.StartInfo = new()
                {
                    FileName = launchInfo.FilePath,
                    UseShellExecute = launchInfo.UseShellExecute,
                };
                if (!string.IsNullOrWhiteSpace(launchInfo.Arguments))
                {
                    process.StartInfo.Arguments = launchInfo.Arguments;
                }
                if (launchInfo.WorkingDirectory is not null)
                {
                    process.StartInfo.WorkingDirectory = launchInfo.WorkingDirectory.FullName;
                }
                if (!string.IsNullOrWhiteSpace(launchInfo.Verb))
                {
                    process.StartInfo.Verb = launchInfo.Verb;
                }
                if (launchInfo.ProcessWindowStyle is not null)
                {
                    process.StartInfo.WindowStyle = launchInfo.ProcessWindowStyle.Value;
                }
                if (launchInfo.CreateNoWindow)
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                if (!process.Start() && File.Exists(launchInfo.FilePath))
                {
                    throw new InvalidProgramException("Failed to start the process.");
                }
            }
            catch (Exception ex)
            {
                process.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }

            // Try to get the process's handle and process Id. For a pure
            // shell action, the calls will throw so just return null here.
            ClientServerUtilities.SetClientServerOperationSuccess();
            SafeProcessHandle hProcess;
            try
            {
                hProcess = process.SafeHandle;
            }
            catch
            {
                process.Dispose();
                return null;
                throw;
            }

            // If this wasn't a pure shell action, assign the handle to our job and set the priority class.
            try
            {
                if (launchInfo.PriorityClass is not null && ProcessTools.TestProcessAccessRights(hProcess, PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION))
                {
                    process.PriorityClass = launchInfo.PriorityClass.Value;
                }
                return new(new(launchInfo, process));
            }
            catch (Exception ex)
            {
                hProcess.Dispose();
                process.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        /// <summary>
        /// Creates a read pipe server stream and a corresponding task that consumes output from the child process.
        /// </summary>
        /// <param name="interleaved">The shared interleaved output buffer.</param>
        /// <param name="encoding">The text encoding for the stream.</param>
        /// <returns>The server stream, client handle, and read task.</returns>
        private static (AnonymousPipeServerStream stream, HANDLE pipe, ProcessReadStream handle) CreateReadPipe(ConcurrentQueue<string> interleaved, Encoding encoding)
        {
            AnonymousPipeServerStream stream = new(PipeDirection.In, HandleInheritability.Inheritable);
            List<string> output = [];
            try
            {
                return (stream, (HANDLE)stream.ClientSafePipeHandle.DangerousGetHandle(), new(output, new(() =>
                {
                    using (stream)
                    {
                        using StreamReader reader = new(stream, encoding);
                        while (reader.ReadLine()?.TrimEnd() is string line)
                        {
                            interleaved.Enqueue(line); output.Add(line);
                        }
                    }
                })));
            }
            catch (Exception ex)
            {
                stream.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        /// <summary>
        /// Creates a write pipe server stream and a corresponding task that writes stdin data to the child process.
        /// </summary>
        /// <param name="input">The input lines to write.</param>
        /// <param name="encoding">The text encoding for the stream.</param>
        /// <returns>The server stream, client handle, and write task.</returns>
        private static (AnonymousPipeServerStream stream, HANDLE pipe, ProcessWriteStream handle) CreateWritePipe(IReadOnlyList<string> input, Encoding encoding)
        {
            AnonymousPipeServerStream stream = new(PipeDirection.Out, HandleInheritability.Inheritable);
            try
            {
                return (stream, (HANDLE)stream.ClientSafePipeHandle.DangerousGetHandle(), new(new(() =>
                {
                    using (stream)
                    {
                        try
                        {
                            using StreamWriter writer = new(stream, encoding);
                            foreach (string line in input)
                            {
                                writer.WriteLine(line);
                            }
                        }
                        catch (IOException)
                        {
                            // The child process didn't read all input before exiting.
                            return;
                        }
                    }
                })));
            }
            catch (Exception ex)
            {
                stream.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        /// <summary>
        /// Determines whether the current process can use the CreateProcessAsUser function.
        /// </summary>
        /// <remarks>This method checks if the current process has the necessary privileges and conditions
        /// to use the CreateProcessAsUser function. It verifies the presence of specific privileges and evaluates
        /// whether the process is part of a job object that allows breakaway.</remarks>
        /// <returns><see langword="true"/> if the process can use CreateProcessAsUser; otherwise, <see langword="false"/>.</returns>
        private static CreateProcessUsingTokenStatus CanUseCreateProcessAsUser(bool isCallerToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges)
        {
            // Test whether the caller has the required privileges to use CreateProcessAsUser.
            if (!callerPrivilges.Contains(SE_PRIVILEGE.SeIncreaseQuotaPrivilege))
            {
                return CreateProcessUsingTokenStatus.SeIncreaseQuotaPrivilege;
            }
            if (!callerPrivilges.Contains(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege))
            {
                return CreateProcessUsingTokenStatus.SeAssignPrimaryTokenPrivilege;
            }

            // Perform common job object checks.
            return CanCreateProcessUsingToken(isCallerToken, callerPrivilges);
        }

        /// <summary>
        /// Determines whether the current process has the necessary privileges to use the CreateProcessWithToken
        /// function.
        /// </summary>
        /// <returns><see langword="true"/> if the current process has the SeImpersonatePrivilege; otherwise, <see
        /// langword="false"/>.</returns>
        private static CreateProcessUsingTokenStatus CanUseCreateProcessWithToken(bool isCallerToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges, ReadOnlySpan<char> commandLine)
        {
            // If the command line exceeds 1024 characters, we can't use CreateProcessWithToken at all.
            if (commandLine.Length > 1024)
            {
                return CreateProcessUsingTokenStatus.CommandLineTooLong;
            }

            // Test whether the caller has the required privileges to use CreateProcessWithToken.
            if (!callerPrivilges.Contains(SE_PRIVILEGE.SeImpersonatePrivilege))
            {
                return CreateProcessUsingTokenStatus.SeImpersonatePrivilege;
            }

            // If the service is disabled, we cannot use CreateProcessWithToken. This
            // property will fail if the service is not found, so catch that as well.
            using ServiceController serviceController = new("seclogon");
            try
            {
                if (serviceController.StartType == ServiceStartMode.Disabled)
                {
                    return CreateProcessUsingTokenStatus.SecLogonServiceDisabled;
                }
            }
            catch
            {
                return CreateProcessUsingTokenStatus.SecLogonServiceNotFound;
                throw;
            }

            // Perform common job object checks.
            return CanCreateProcessUsingToken(isCallerToken, callerPrivilges);
        }

        /// <summary>
        /// Determines whether the current process can create a new process using a specified security token, based on
        /// job object and privilege constraints.
        /// </summary>
        /// <remarks>This method checks whether the process is running within a job object and whether the
        /// necessary job object limits and privileges are present to allow process creation using a different token. If
        /// the process is restricted by job object settings or lacks the required privileges, the returned status will
        /// indicate the specific limitation.</remarks>
        /// <param name="isCallerToken">true to indicate that the token represents the current caller; false if the token represents a different
        /// user or security context.</param>
        /// <param name="callerPrivilges">A read-only collection of the privileges held by the caller, used to determine if specific
        /// privileges are present that may allow process creation even when job object restrictions are in place.</param>
        /// <returns>A CreateProcessUsingTokenStatus value indicating whether process creation is permitted, or the reason it is
        /// not allowed.</returns>
        private static CreateProcessUsingTokenStatus CanCreateProcessUsingToken(bool isCallerToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges)
        {
            // Test whether the token's SID is the same as the caller's SID.
            // If it is, the following job object checks are not necessary.
            if (isCallerToken)
            {
                return CreateProcessUsingTokenStatus.OK;
            }

            // Test whether the process is part of an existing job object.
            using (SafeProcessHandle hProcess = NativeMethods.GetCurrentProcess())
            {
                _ = NativeMethods.IsProcessInJob(hProcess, out BOOL inJob);
                if (!inJob)
                {
                    return CreateProcessUsingTokenStatus.OK;
                }
            }

            // Since we're part of a job object, we need to check if the job has the JOB_OBJECT_LIMIT_BREAKAWAY_OK flag set.
            Span<byte> lpJobObjectInformation = stackalloc byte[Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()];
            _ = NativeMethods.QueryInformationJobObject(JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, lpJobObjectInformation, out _);
            ref readonly JOBOBJECT_EXTENDED_LIMIT_INFORMATION jobObjectInfo = ref lpJobObjectInformation.AsReadOnlyStructure<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            if (!(jobObjectInfo.BasicLimitInformation.LimitFlags.HasFlag(JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK) || jobObjectInfo.BasicLimitInformation.LimitFlags.HasFlag(JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_BREAKAWAY_OK)))
            {
                return !callerPrivilges.Contains(SE_PRIVILEGE.SeTcbPrivilege) ? CreateProcessUsingTokenStatus.SeTcbPrivilege : CreateProcessUsingTokenStatus.JobBreakawayNotPermitted;
            }

            // If we're here, everything we need to be able to use CreateProcessAsUser() is available.
            return CreateProcessUsingTokenStatus.OK;
        }

        /// <summary>
        /// Creates a new process using the specified primary token and command line.
        /// </summary>
        /// <remarks>This method attempts to create a process using <c>CreateProcessAsUser</c> if
        /// possible, falling back to <c>CreateProcessWithToken</c> if necessary. It requires specific privileges to be
        /// enabled, such as <c>SeIncreaseQuotaPrivilege</c> and <c>SeAssignPrimaryTokenPrivilege</c>.</remarks>
        /// <param name="hPrimaryToken">The primary token representing the user context under which the process will be created.</param>
        /// <param name="callerPrivilges">A read-only collection of the privileges held by the caller, used to determine if specific
        /// privileges are present that may allow process creation even when job object restrictions are in place.</param>
        /// <param name="filePath">The fully qualified path to the executable file for the new process.</param>
        /// <param name="commandLine">The command line to be executed by the new process.</param>
        /// <param name="handlesToInherit">An array of specific handles that the child process should inherit. When specified,
        /// a STARTUPINFOEX with PROC_THREAD_ATTRIBUTE_HANDLE_LIST is used to limit inheritance to these handles only.</param>
        /// <param name="hasExternalHandles">Indicates whether there are any external handles to inherit, which would require
        /// the use of CreateProcessAsUser even if the caller token is the same as the current process token.</param>
        /// <param name="creationFlags">Flags that control the priority class and the creation of the process.</param>
        /// <param name="lpEnvironment">A handle to the environment block for the new process. Can be <see langword="null"/> to use the environment
        /// of the calling process.</param>
        /// <param name="workingDirectory">The full path to the current directory for the process. Can be <see langword="null"/> to use the current
        /// directory of the calling process.</param>
        /// <param name="runAsInvoker">Indicates that the process must be created with the EXTENDED_PROCESS_CREATION_FLAG_FORCELUA flag.</param>
        /// <param name="startupInfo">A reference to a <see cref="STARTUPINFOW"/> structure that specifies the window station, desktop, standard
        /// handles, and appearance of the main window for the new process.</param>
        /// <param name="pi">When this method returns, contains a <see cref="PROCESS_INFORMATION"/> structure with information about the
        /// newly created process and its primary thread.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if the calling user account does not have the necessary privileges to create a process using the
        /// specified token.</exception>
        private static BOOL CreateProcessUsingToken(SafeFileHandle hPrimaryToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges, string filePath, ref Span<char> commandLine, List<nint> handlesToInherit, bool hasExternalHandles, PROCESS_CREATION_FLAGS creationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? workingDirectory, bool runAsInvoker, in STARTUPINFOW startupInfo, out PROCESS_INFORMATION pi)
        {
            // Attempt to use CreateProcessAsUser() first as it's gold standard, otherwise fall back to CreateProcessWithToken().
            // When the caller provides handles to inherit, we need to use CreateProcessAsUser() since it has bInheritHandles.
            bool isCallerToken = TokenUtilities.GetTokenSid(hPrimaryToken) == AccountUtilities.CallerSid;
            CreateProcessUsingTokenStatus createProcessAsUserAbility = CanUseCreateProcessAsUser(isCallerToken, callerPrivilges);
            bool forceBreakaway = createProcessAsUserAbility == CreateProcessUsingTokenStatus.JobBreakawayNotPermitted;
            if (createProcessAsUserAbility == CreateProcessUsingTokenStatus.OK || forceBreakaway || runAsInvoker)
            {
                // Use STARTUPINFOEX when we need to specify handle inheritance or force breakaway.
                if (forceBreakaway || runAsInvoker || handlesToInherit.Count > 0)
                {
                    // Create the extended startup info with the necessary attributes.
                    (STARTUPINFOEXW startupInfoEx, SafeProcThreadAttributeListHandle hAttributeList) = CreateStartupInfoEx(in startupInfo, handlesToInherit, forceBreakaway, runAsInvoker, out SafePinnedGCHandle? pinnedHandles);
                    using (hAttributeList)
                    using (pinnedHandles)
                    {
                        return NativeMethods.CreateProcessAsUser(hPrimaryToken, filePath, ref commandLine, null, null, true, creationFlags | PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT, lpEnvironment, workingDirectory, in startupInfoEx, out pi);
                    }
                }
                else
                {
                    // If the parent process is associated with an existing job object, using the CREATE_BREAKAWAY_FROM_JOB flag can help
                    // with E_ACCESSDENIED errors from CreateProcessWithToken() as processes in a job all need to be in the same session.
                    // The use of this flag has effect if the parent is part of a job and that job has JOB_OBJECT_LIMIT_BREAKAWAY_OK set.
                    if (!isCallerToken)
                    {
                        creationFlags |= PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB;
                    }
                    return NativeMethods.CreateProcessAsUser(hPrimaryToken, filePath, ref commandLine, null, null, false, creationFlags, lpEnvironment, workingDirectory, in startupInfo, out pi);
                }
            }
            else if (hasExternalHandles)
            {
                throw new InvalidOperationException($"Unable to create a new process using CreateProcessAsUser(): {CreateProcessUsingTokenStatusMessages[createProcessAsUserAbility]}");
            }

            // Using CreateProcessAsUser() is not possible, so fall back to CreateProcessWithToken().
            CreateProcessUsingTokenStatus createProcessWithTokenAbility = CanUseCreateProcessWithToken(isCallerToken, callerPrivilges, commandLine);
            if (createProcessWithTokenAbility != CreateProcessUsingTokenStatus.OK)
            {
                throw new InvalidOperationException($"Unable to create a new process using CreateProcessWithToken(): {CreateProcessUsingTokenStatusMessages[createProcessWithTokenAbility]}");
            }
            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeImpersonatePrivilege);

            // If the parent process is associated with an existing job object, using the CREATE_BREAKAWAY_FROM_JOB flag can help
            // with E_ACCESSDENIED errors from CreateProcessWithToken() as processes in a job all need to be in the same session.
            // The use of this flag has effect if the parent is part of a job and that job has JOB_OBJECT_LIMIT_BREAKAWAY_OK set.
            if (!isCallerToken)
            {
                creationFlags |= PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB;
            }
            return NativeMethods.CreateProcessWithToken(hPrimaryToken, CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE, filePath, ref commandLine, creationFlags, lpEnvironment, workingDirectory, in startupInfo, out pi);
        }

        /// <summary>
        /// Creates a STARTUPINFOEX structure with the specified process thread attributes.
        /// </summary>
        /// <remarks>This method allocates and initializes a STARTUPINFOEX structure with the specified
        /// attributes. The attribute list can include handle inheritance lists and extended process creation flags.
        /// The caller is responsible for disposing of the returned attribute list handle.</remarks>
        /// <param name="startupInfo">The base STARTUPINFOW structure to extend.</param>
        /// <param name="handlesToInherit">An array of handles that the child process should inherit.</param>
        /// <param name="forceBreakaway">If true, adds the EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY attribute.</param>
        /// <param name="runAsInvoker">If true, adds the EXTENDED_PROCESS_CREATION_FLAG_FORCELUA attribute.</param>
        /// <param name="pinnedHandles">When this method returns, contains the pinned GC handle for the handles array, or null if no handles were specified.</param>
        /// <returns>A tuple containing the STARTUPINFOEXW structure and the SafeProcThreadAttributeListHandle.</returns>
        private static (STARTUPINFOEXW startupInfoEx, SafeProcThreadAttributeListHandle hAttributeList) CreateStartupInfoEx(in STARTUPINFOW startupInfo, List<nint> handlesToInherit, bool forceBreakaway, bool runAsInvoker, out SafePinnedGCHandle? pinnedHandles)
        {
            // Calculate the number of attributes needed.
            bool hasHandleInheritance = handlesToInherit.Count > 0;
            uint attributeCount = 0;
            if (hasHandleInheritance)
            {
                attributeCount++;
            }
            if (forceBreakaway || runAsInvoker)
            {
                attributeCount++;
            }

            // Validate that at least one attribute is specified.
            if (attributeCount == 0)
            {
                throw new InvalidOperationException("At least one attribute must be specified.");
            }

            // Allocate the attribute list.
            SafeProcThreadAttributeListHandle hAttributeList = SafeProcThreadAttributeListHandle.Alloc(attributeCount);
            try
            {
                // Add handle list attribute if handles are specified.
                pinnedHandles = hasHandleInheritance ? SafePinnedGCHandle.Alloc([.. handlesToInherit]) : null;
                try
                {
                    // Add the handle list attribute if handles to inherit were specified.
                    if (pinnedHandles is not null)
                    {
                        // The handle list needs to be passed as a pointer to an array of handles.
                        nint handlesPtr = pinnedHandles.DangerousGetHandle();
                        int handleListSize = handlesToInherit.Count * IntPtr.Size;
                        unsafe
                        {
                            _ = hAttributeList.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_HANDLE_LIST, new((void*)handlesPtr, handleListSize));
                        }
                    }

                    // Add extended flags attribute if force breakaway is requested.
                    EXTENDED_PROCESS_CREATION_FLAG extendedFlags = 0;
                    if (runAsInvoker)
                    {
                        extendedFlags |= EXTENDED_PROCESS_CREATION_FLAG.EXTENDED_PROCESS_CREATION_FLAG_FORCELUA;
                    }
                    if (forceBreakaway)
                    {
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                        extendedFlags |= EXTENDED_PROCESS_CREATION_FLAG.EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY;
                    }
                    if (extendedFlags != 0)
                    {
                        unsafe
                        {
                            _ = hAttributeList.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_EXTENDED_FLAGS, MemoryMarshal.AsBytes(new ReadOnlySpan<int>(&extendedFlags, 1)));
                        }
                    }

                    // Create the STARTUPINFOEXW structure.
                    STARTUPINFOEXW startupInfoEx = new() { StartupInfo = startupInfo };
                    startupInfoEx.StartupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOEXW>();
                    startupInfoEx.lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST)hAttributeList.DangerousGetHandle();
                    return (startupInfoEx, hAttributeList);
                }
                catch (Exception ex)
                {
                    pinnedHandles?.Dispose();
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
            catch (Exception ex)
            {
                hAttributeList.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
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
            SeTcbPrivilege,
            CommandLineTooLong,
            SeImpersonatePrivilege,
            SecLogonServiceNotFound,
            SecLogonServiceDisabled,
        }

        /// <summary>
        /// Provides a read-only dictionary mapping <see cref="CreateProcessUsingTokenStatus"/> values to their
        /// corresponding error messages.
        /// </summary>
        /// <remarks>This dictionary contains predefined error messages for various statuses encountered
        /// when attempting to create a process using a token. It is used to provide descriptive error messages based on
        /// the status code returned by the operation.</remarks>
        private static readonly ReadOnlyDictionary<CreateProcessUsingTokenStatus, string> CreateProcessUsingTokenStatusMessages = new(new Dictionary<CreateProcessUsingTokenStatus, string>()
        {
            { CreateProcessUsingTokenStatus.SeIncreaseQuotaPrivilege, "The calling process does not have the necessary SeIncreaseQuotaPrivilege privilege." },
            { CreateProcessUsingTokenStatus.SeAssignPrimaryTokenPrivilege, "The calling process does not have the necessary SeAssignPrimaryTokenPrivilege privilege." },
            { CreateProcessUsingTokenStatus.JobBreakawayNotPermitted, "The calling process is part of a job that does not allow breakaway." },
            { CreateProcessUsingTokenStatus.SeTcbPrivilege, "The calling process does not have the necessary SeTcbPrivilege privilege." },
            { CreateProcessUsingTokenStatus.CommandLineTooLong, "The process command line exceeds the API limitation of 1024 characters." },
            { CreateProcessUsingTokenStatus.SeImpersonatePrivilege, "The calling process does not have the necessary SeImpersonatePrivilege privilege." },
            { CreateProcessUsingTokenStatus.SecLogonServiceNotFound, "The system's Secondary Log-on service (seclogon) could not be found." },
            { CreateProcessUsingTokenStatus.SecLogonServiceDisabled, "The system's Secondary Log-on service (seclogon) is disabled." },
        });

        /// <summary>
        /// Special exit code used to signal when we're terminating a process due to timeout.
        /// The value is `'PSAppDeployToolkit'.GetHashCode()` under Windows PowerShell 5.1.
        /// </summary>
        public const int TimeoutExitCode = -443991205;
    }
}
