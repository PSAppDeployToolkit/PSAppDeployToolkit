using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Extensions;
using PSADT.FileSystem;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using PSADT.Security;
using PSADT.Utilities;
using Windows.Win32;
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
        /// Launches a process with the specified start info and waits for it to complete.
        /// </summary>
        /// <param name="launchInfo"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static ProcessHandle? LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            // Set up initial variables needed throughout method.
            Task hStdOutTask = Task.CompletedTask; Task hStdErrTask = Task.CompletedTask;
            List<string> stdout = []; List<string> stderr = [];
            ConcurrentQueue<string> interleaved = [];
            SafeProcessHandle? hProcess = null;
            Process process = null!;
            uint? processId = null;
            string commandLine;

            // Determine whether the process we're starting is a console app or not. This is important
            // because under ShellExecuteEx() invocations, stdout/stderr will attach to the running console.
            bool cliApp;
            try
            {
                cliApp = ExecutableInfo.Get(launchInfo.FilePath).Subsystem != IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI;
            }
            catch
            {
                cliApp = launchInfo.CreateNoWindow || !launchInfo.UseShellExecute;
            }

            // Set up the job object and I/O completion port for the process.
            // No using statements here, they're disposed of in the final task.
            bool assignProcessToJob = launchInfo.WaitForChildProcesses || launchInfo.KillChildProcessesWithParent;
            var iocp = Kernel32.CreateIoCompletionPort(SafeBaseHandle.InvalidHandle, SafeBaseHandle.NullHandle, UIntPtr.Zero, 1);
            var job = Kernel32.CreateJobObject(null, default); bool iocpAddRef = false; iocp.DangerousAddRef(ref iocpAddRef);
            Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, new JOBOBJECT_ASSOCIATE_COMPLETION_PORT { CompletionPort = (HANDLE)iocp.DangerousGetHandle(), CompletionKey = null });

            // Set up the required job limit if child processes must be killed with the parent.
            if (launchInfo.KillChildProcessesWithParent)
            {
                Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, new JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION { LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE } });
            }

            // We only let console apps run via ShellExecuteEx() when there's a window shown for it.
            // Invoking processes as user has no ShellExecute capability, so it always comes through here.
            if ((cliApp && launchInfo.CreateNoWindow) || (!launchInfo.UseShellExecute) || (null != launchInfo.RunAsActiveUser))
            {
                AnonymousPipeServerStream? hStdOutRead = null;
                AnonymousPipeServerStream? hStdErrRead = null;
                SafePipeHandle? hStdOutWrite = null;
                SafePipeHandle? hStdErrWrite = null;
                bool hStdOutWriteAddRef = false;
                bool hStdErrWriteAddRef = false;
                try
                {
                    // Set up the startup information for the process.
                    var startupInfo = new STARTUPINFOW { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };
                    if (null != launchInfo.WindowStyle)
                    {
                        startupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                        startupInfo.wShowWindow = (ushort)launchInfo.WindowStyle.Value;
                    }

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

                    // If we're to read the output, we create pipes for stdout and stderr.
                    bool inheritHandles = launchInfo.InheritHandles;
                    if ((startupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                    {
                        hStdOutRead = new(PipeDirection.In, HandleInheritability.Inheritable);
                        hStdErrRead = new(PipeDirection.In, HandleInheritability.Inheritable);
                        hStdOutTask = Task.Run(() => ReadPipe(hStdOutRead, stdout, interleaved, launchInfo.StreamEncoding));
                        hStdErrTask = Task.Run(() => ReadPipe(hStdErrRead, stderr, interleaved, launchInfo.StreamEncoding));
                        hStdOutWrite = hStdOutRead.ClientSafePipeHandle;
                        hStdErrWrite = hStdErrRead.ClientSafePipeHandle;
                        hStdOutWrite.DangerousAddRef(ref hStdOutWriteAddRef);
                        hStdErrWrite.DangerousAddRef(ref hStdErrWriteAddRef);
                        startupInfo.hStdOutput = (HANDLE)hStdOutWrite.DangerousGetHandle();
                        startupInfo.hStdError = (HANDLE)hStdErrWrite.DangerousGetHandle();
                        inheritHandles = true;
                    }

                    // Handle user process creation, otherwise just create the process for the running user.
                    PROCESS_INFORMATION pi = new();
                    if (null != launchInfo.RunAsActiveUser && launchInfo.RunAsActiveUser.SID != AccountUtilities.CallerSid)
                    {
                        // Start the process with the user's token.
                        using (var hPrimaryToken = ProcessToken.GetUserPrimaryToken(launchInfo.RunAsActiveUser, launchInfo.UseLinkedAdminToken, launchInfo.UseHighestAvailableToken))
                        {
                            // Without creating an environment block, the process will take on the environment of the SYSTEM account.
                            UserEnv.CreateEnvironmentBlock(out var lpEnvironment, hPrimaryToken, launchInfo.InheritEnvironmentVariables);
                            using (var lpDesktop = SafeHGlobalHandle.StringToUni(@"winsta0\default"))
                            using (lpEnvironment)
                            {
                                bool lpDesktopAddRef = false;
                                try
                                {
                                    lpDesktop.DangerousAddRef(ref lpDesktopAddRef);
                                    startupInfo.lpDesktop = new(lpDesktop.DangerousGetHandle());
                                    OutLaunchArguments(launchInfo, launchInfo.RunAsActiveUser.NTAccount, launchInfo.ExpandEnvironmentVariables ? EnvironmentBlockToDictionary(lpEnvironment) : null, out var filePath, out _, out commandLine, out string? workingDirectory); Span<char> commandSpan = commandLine.ToCharArray();
                                    CreateProcessUsingToken(hPrimaryToken, filePath, ref commandSpan, inheritHandles, launchInfo.InheritHandles, creationFlags, lpEnvironment, workingDirectory, startupInfo, out pi); commandLine = commandSpan.ToString().TrimRemoveNull();
                                    startupInfo.lpDesktop = null;
                                }
                                finally
                                {
                                    if (lpDesktopAddRef)
                                    {
                                        lpDesktop.DangerousRelease();
                                    }
                                }
                            }
                        }
                    }
                    else if ((null != launchInfo.RunAsActiveUser && launchInfo.RunAsActiveUser != AccountUtilities.CallerRunAsActiveUser && !launchInfo.UseLinkedAdminToken && !launchInfo.UseHighestAvailableToken) || (launchInfo.UseUnelevatedToken && AccountUtilities.CallerIsAdmin))
                    {
                        // We're running elevated but have been asked to de-elevate.
                        using (var hPrimaryToken = ProcessToken.GetUnelevatedToken())
                        {
                            OutLaunchArguments(launchInfo, AccountUtilities.CallerUsername, launchInfo.ExpandEnvironmentVariables ? GetCallerEnvironmentDictionary() : null, out var filePath, out _, out commandLine, out string? workingDirectory); Span<char> commandSpan = commandLine.ToCharArray();
                            CreateProcessUsingToken(hPrimaryToken, filePath, ref commandSpan, inheritHandles, launchInfo.InheritHandles, creationFlags, SafeEnvironmentBlockHandle.Null, workingDirectory, startupInfo, out pi); commandLine = commandSpan.ToString().TrimRemoveNull();
                        }
                    }
                    else
                    {
                        // No username was specified and we weren't asked to de-elevate, so we're just creating the process as this current user as-is.
                        OutLaunchArguments(launchInfo, AccountUtilities.CallerUsername, launchInfo.ExpandEnvironmentVariables ? GetCallerEnvironmentDictionary() : null, out var filePath, out _, out commandLine, out string? workingDirectory); Span<char> commandSpan = commandLine.ToCharArray();
                        Kernel32.CreateProcess(filePath, ref commandSpan, null, null, inheritHandles, creationFlags, SafeEnvironmentBlockHandle.Null, workingDirectory, startupInfo, out pi); commandLine = commandSpan.ToString().TrimRemoveNull();
                    }

                    // Start tracking the process and allow it to resume execution.
                    process = GetProcessFromId((processId = pi.dwProcessId).Value);
                    hProcess = new(pi.hProcess, true);
                    using (SafeThreadHandle hThread = new(pi.hThread, true))
                    {
                        if (assignProcessToJob)
                        {
                            Kernel32.AssignProcessToJobObject(job, hProcess);
                        }
                        Kernel32.ResumeThread(hThread);
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
                    hStdOutWrite?.Dispose(); hStdOutWrite = null;
                    hStdErrWrite?.Dispose(); hStdErrWrite = null;
                    hStdOutRead?.DisposeLocalCopyOfClientHandle();
                    hStdErrRead?.DisposeLocalCopyOfClientHandle();
                }
            }
            else
            {
                // Build the command line for the process.
                OutLaunchArguments(launchInfo, AccountUtilities.CallerUsername, launchInfo.ExpandEnvironmentVariables ? GetCallerEnvironmentDictionary() : null, out var filePath, out var arguments, out commandLine, out string? workingDirectory);
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = launchInfo.UseShellExecute,
                        Verb = launchInfo.Verb,
                    }
                };
                if (null != launchInfo.ProcessWindowStyle)
                {
                    process.StartInfo.WindowStyle = launchInfo.ProcessWindowStyle.Value;
                }
                if (launchInfo.CreateNoWindow)
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                // Start the process and assign the handle to our job if we have one.
                // For a pure shell action, we won't ever be able to get one.
                process.Start();
                try
                {
                    if (null != (hProcess = process.SafeHandle))
                    {
                        processId = (uint)process.Id;
                        if (assignProcessToJob)
                        {
                            Kernel32.AssignProcessToJobObject(job, hProcess);
                        }
                        if (null != launchInfo.PriorityClass && PrivilegeManager.TestProcessAccessRights(hProcess, PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION))
                        {
                            process.PriorityClass = launchInfo.PriorityClass.Value;
                        }
                    }
                }
                catch
                {
                    hProcess = null;
                    processId = null;
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
                // Set up the cancellation token source and registration if needed.
                uint timeoutExitCode = ValueTypeConverter<uint>.Convert(TimeoutExitCode);
                CancellationTokenRegistration ctr = default;
                if (null != launchInfo.CancellationToken)
                {
                    ctr = launchInfo.CancellationToken.Value.Register(() => Kernel32.PostQueuedCompletionStatus(iocp, timeoutExitCode, UIntPtr.Zero, null));
                }

                // Spin until complete or cancelled.
                bool disposeJob = true;
                try
                {
                    int exitCode;
                    if (assignProcessToJob)
                    {
                        while (true)
                        {
                            Kernel32.GetQueuedCompletionStatus(iocp, out var lpCompletionCode, out _, out var lpOverlapped, PInvoke.INFINITE);
                            if (lpCompletionCode == timeoutExitCode)
                            {
                                if (launchInfo.NoTerminateOnTimeout)
                                {
                                    if (launchInfo.KillChildProcessesWithParent)
                                    {
                                        disposeJob = false;
                                    }
                                    exitCode = TimeoutExitCode;
                                    break;
                                }
                                Kernel32.TerminateJobObject(job, timeoutExitCode);
                            }
                            else if ((lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_EXIT_PROCESS && !launchInfo.WaitForChildProcesses && (uint)lpOverlapped == processId) || (lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO))
                            {
                                await Task.WhenAll(hStdOutTask, hStdErrTask);
                                Kernel32.GetExitCodeProcess(hProcess, out var lpExitCode);
                                exitCode = ValueTypeConverter<int>.Convert(lpExitCode);
                                break;
                            }
                        }
                    }
                    else
                    {
                        process.WaitForExit();
                        exitCode = process.ExitCode;
                    }
                    tcs.SetResult(new(process, launchInfo, commandLine, exitCode, stdout, stderr, interleaved));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    ctr.Dispose(); hProcess.Dispose();
                    if (iocpAddRef)
                    {
                        iocp.DangerousRelease();
                    }
                    iocp.Dispose();
                    if (disposeJob)
                    {
                        job.Dispose();
                    }
                }
            });

            // Return a ProcessHandle object with this process and its running task.
            return new(process, launchInfo, commandLine, tcs.Task);
        }

        /// <summary>
        /// Reads from a pipe until the pipe is closed.
        /// </summary>
        /// <param name="pipeStream"></param>
        /// <param name="output"></param>
        /// <param name="interleaved"></param>
        /// <param name="encoding"></param>
        private static void ReadPipe(AnonymousPipeServerStream pipeStream, List<string> output, ConcurrentQueue<string> interleaved, Encoding encoding)
        {
            using (pipeStream) using (StreamReader streamReader = new(pipeStream, encoding))
            {
                while (streamReader.ReadLine()?.TrimEndRemoveNull() is string line)
                {
                    interleaved.Enqueue(line);
                    output.Add(line);
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
        /// <param name="filePath">When this method returns, contains the fully qualified file path of the executable to launch.</param>
        /// <param name="arguments">When this method returns, contains the command line arguments for the process launch, or <see langword="null"/>
        /// <param name="commandLine">When this method returns, contains the constructed command line string for the process launch.</param>
        /// <param name="workingDirectory">When this method returns, contains the working directory for the process launch, or <see langword="null"/>
        /// if not specified.</param>
        private static void OutLaunchArguments(ProcessLaunchInfo launchInfo, NTAccount username, ReadOnlyDictionary<string, string>? environmentDictionary, out string filePath, out string? arguments, out string commandLine, out string? workingDirectory)
        {
            if (null != environmentDictionary)
            {
                var argv = launchInfo.ArgumentList?.ToArray() ?? [];
                for (int i = 0; i < argv.Length; i++)
                {
                    argv[i] = ExpandEnvironmentVariables(username, argv[i], environmentDictionary);
                }
                filePath = ExpandEnvironmentVariables(username, launchInfo.FilePath, environmentDictionary);
                arguments = argv.Length > 1 ? CommandLineUtilities.ArgumentListToCommandLine(argv) : argv.Length > 0 ? argv[0] : null;
                workingDirectory = null != launchInfo.WorkingDirectory ? ExpandEnvironmentVariables(username, launchInfo.WorkingDirectory, environmentDictionary) : null;
            }
            else
            {
                filePath = launchInfo.FilePath;
                arguments = null != launchInfo.ArgumentList ? launchInfo.ArgumentList.Count > 1 ? CommandLineUtilities.ArgumentListToCommandLine(launchInfo.ArgumentList) : launchInfo.ArgumentList.Count > 0 ? launchInfo.ArgumentList[0] : null : null;
                workingDirectory = launchInfo.WorkingDirectory;
            }
            commandLine = $"\"{filePath}\"{(!string.IsNullOrWhiteSpace(arguments) ? $" {arguments}" : null)}\0";
        }

        /// <summary>
        /// Determines whether the current process can use the CreateProcessAsUser function.
        /// </summary>
        /// <remarks>This method checks if the current process has the necessary privileges and conditions
        /// to use the CreateProcessAsUser function. It verifies the presence of specific privileges and evaluates
        /// whether the process is part of a job object that allows breakaway.</remarks>
        /// <returns><see langword="true"/> if the process can use CreateProcessAsUser; otherwise, <see langword="false"/>.</returns>
        private static CreateProcessUsingTokenStatus CanUseCreateProcessAsUser(SafeFileHandle hPrimaryToken)
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

            // Test whether the token's SID is the same as the caller's SID.
            // If it is, the following job object checks are not necessary.
            if (TokenManager.GetTokenSid(hPrimaryToken) == AccountUtilities.CallerSid)
            {
                return CreateProcessUsingTokenStatus.OK;
            }

            // Test whether the process is part of an existing job object.
            using (var cProcessSafeHandle = Kernel32.GetCurrentProcess())
            {
                Kernel32.IsProcessInJob(cProcessSafeHandle, null, out var inJob);
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
                // If the service is disabled, we cannot use CreateProcessWithToken. This
                // property will fail if the service is not found, so catch that as well.
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
        /// <param name="inheritHandles">Specifies whether the new process inherits handles from the calling process.</param>
        /// <param name="callerUsingHandles">The caller is passing anonymous handles to the process, so cannot use CreateProcessWithToken().</param>
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
        private static void CreateProcessUsingToken(SafeFileHandle hPrimaryToken, string filePath, ref Span<char> commandLine, bool inheritHandles, bool callerUsingHandles, PROCESS_CREATION_FLAGS creationFlags, SafeEnvironmentBlockHandle lpEnvironment, string? workingDirectory, in STARTUPINFOW startupInfo, out PROCESS_INFORMATION pi)
        {
            // Attempt to use CreateProcessAsUser() first as it's gold standard, otherwise fall back to CreateProcessWithToken().
            // When the caller provides anonymous handles, we need to use CreateProcessAsUser() since it has bInheritHandles.
            if (CanUseCreateProcessAsUser(hPrimaryToken) is CreateProcessUsingTokenStatus canUseCreateProcessAsUser && (canUseCreateProcessAsUser == CreateProcessUsingTokenStatus.OK || canUseCreateProcessAsUser == CreateProcessUsingTokenStatus.JobBreakawayNotPermitted))
            {
                if (canUseCreateProcessAsUser == CreateProcessUsingTokenStatus.JobBreakawayNotPermitted && TokenManager.GetTokenSid(hPrimaryToken) != AccountUtilities.CallerSid)
                {
                    // When creating a process for another user, if the token's Session Id differs from the caller's and
                    // the current process is part of a job object, we can only do so if JOB_OBJECT_LIMIT_BREAKAWAY_OK
                    // was specified by the process that set up the job in the first place. Some vendors like VMware do
                    // not specify this flag when setting up their job object, therefore we can't run our client/server code.
                    // Since Windows 8.1, there is a (highly) undocumented flag to force job breakaway irrespective of the
                    // flags on the parent job object. We attempt to use this here for circumstances where it's necessary.
                    // A massive thank you to jborean93 for advising me of this flag's existence so we can make PSADT better.
                    if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeTcbPrivilege))
                    {
                        throw new UnauthorizedAccessException(CreateProcessUsingTokenStatusMessages[CreateProcessUsingTokenStatus.SeTcbPrivilege]);
                    }
                    using var hExtendedFlags = SafeHGlobalHandle.Alloc(sizeof(EXTENDED_PROCESS_CREATION_FLAG));
                    using var hAttributeList = SafeProcThreadAttributeListHandle.Create(1);
                    hExtendedFlags.WriteInt32((int)EXTENDED_PROCESS_CREATION_FLAG.EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY);
                    Kernel32.UpdateProcThreadAttribute(hAttributeList, PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_EXTENDED_FLAGS, hExtendedFlags);
                    bool hAttributeListAddRef = false;
                    try
                    {
                        hAttributeList.DangerousAddRef(ref hAttributeListAddRef);
                        var startupInfoEx = new STARTUPINFOEXW { StartupInfo = startupInfo };
                        startupInfoEx.StartupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOEXW>();
                        startupInfoEx.lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST)hAttributeList.DangerousGetHandle();
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege);
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);
                        AdvApi32.CreateProcessAsUser(hPrimaryToken, filePath, ref commandLine, null, null, inheritHandles || callerUsingHandles, creationFlags | PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT, lpEnvironment, workingDirectory, startupInfoEx, out pi);
                    }
                    finally
                    {
                        if (hAttributeListAddRef)
                        {
                            hAttributeList.DangerousRelease();
                        }
                    }
                }
                else if (canUseCreateProcessAsUser == CreateProcessUsingTokenStatus.OK)
                {
                    // If the parent process is associated with an existing job object, using the CREATE_BREAKAWAY_FROM_JOB flag can help
                    // with E_ACCESSDENIED errors from CreateProcessAsUser() as processes in a job all need to be in the same session.
                    // The use of this flag has effect if the parent is part of a job and that job has JOB_OBJECT_LIMIT_BREAKAWAY_OK set.
                    if (TokenManager.GetTokenSid(hPrimaryToken) != AccountUtilities.CallerSid)
                    {
                        creationFlags |= PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB;
                    }
                    PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeIncreaseQuotaPrivilege); PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege);
                    AdvApi32.CreateProcessAsUser(hPrimaryToken, filePath, ref commandLine, null, null, inheritHandles || callerUsingHandles, creationFlags, lpEnvironment, workingDirectory, startupInfo, out pi);
                }
                else
                {
                    throw new InvalidOperationException($"Unable to create a new process using CreateProcessAsUser(): {CreateProcessUsingTokenStatusMessages[canUseCreateProcessAsUser]}");
                }
            }
            else if (CanUseCreateProcessWithToken() is CreateProcessUsingTokenStatus canUseCreateProcessWithToken && canUseCreateProcessWithToken == CreateProcessUsingTokenStatus.OK && !callerUsingHandles)
            {
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeImpersonatePrivilege);
                AdvApi32.CreateProcessWithToken(hPrimaryToken, CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE, filePath, ref commandLine, creationFlags, lpEnvironment, workingDirectory, startupInfo, out pi);
            }
            else
            {
                StringBuilder exceptionMessage = new("Unable to create a new process via token.");
                if (canUseCreateProcessAsUser != CreateProcessUsingTokenStatus.OK)
                {
                    exceptionMessage.Append($" CreateProcessAsUser() reason: {CreateProcessUsingTokenStatusMessages[canUseCreateProcessAsUser]}");
                }
                if (canUseCreateProcessWithToken != CreateProcessUsingTokenStatus.OK)
                {
                    exceptionMessage.Append($" CreateProcessWithToken() reason: {CreateProcessUsingTokenStatusMessages[canUseCreateProcessWithToken]}");
                }
                throw new InvalidOperationException(exceptionMessage.ToString());
            }
        }

        /// <summary>
        /// Retrieves a <see cref="Process"/> object that is associated with the specified process identifier.
        /// </summary>
        /// <param name="processId">The unique identifier of the process to retrieve.</param>
        /// <returns>A <see cref="Process"/> object that represents the process with the specified identifier.</returns>
        private static Process GetProcessFromId(uint processId)
        {
            var process = Process.GetProcessById((int)processId);
            _ = process; _ = process.Handle;
            return process;
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
        private static readonly ReadOnlyDictionary<CreateProcessUsingTokenStatus, string> CreateProcessUsingTokenStatusMessages = new(new Dictionary<CreateProcessUsingTokenStatus, string>
        {
            { CreateProcessUsingTokenStatus.SeIncreaseQuotaPrivilege, "The calling process does not have the necessary SeIncreaseQuotaPrivilege privilege." },
            { CreateProcessUsingTokenStatus.SeAssignPrimaryTokenPrivilege, "The calling process does not have the necessary SeAssignPrimaryTokenPrivilege privilege." },
            { CreateProcessUsingTokenStatus.JobBreakawayNotPermitted, "The calling process is part of a job that does not allow breakaway." },
            { CreateProcessUsingTokenStatus.SeTcbPrivilege, "The calling process does not have the necessary SeTcbPrivilege privilege." },
            { CreateProcessUsingTokenStatus.SeImpersonatePrivilege, "The calling process does not have the necessary SeImpersonatePrivilege privilege." },
            { CreateProcessUsingTokenStatus.SecLogonServiceNotFound, "The system's Secondary Log-on service (seclogon) could not be found." },
            { CreateProcessUsingTokenStatus.SecLogonServiceDisabled, "The system's Secondary Log-on service (seclogon) is disabled." },
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
