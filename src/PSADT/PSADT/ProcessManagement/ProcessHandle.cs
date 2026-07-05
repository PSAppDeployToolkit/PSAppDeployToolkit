using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.Foundation;
using PSADT.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.JobObjects;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a handle to a process, encapsulating the process, its module information, launch details, command
    /// line, and associated asynchronous task.
    /// </summary>
    /// <remarks>This record provides a structured way to manage and interact with a process, offering access
    /// to its core components and the ability to handle its asynchronous operations.</remarks>
    public sealed class ProcessHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessHandle"/> record with the specified process launch information, process, process ID, process handle, command line, caller privileges, and optional standard output/error handles and interleaved buffer.
        /// </summary>
        /// <param name="launchInfo">The launch configuration and metadata used to start the process.</param>
        /// <param name="process">The Process object representing the running process.</param>
        /// <param name="processId">The unique identifier of the started process.</param>
        /// <param name="processHandle">A safe handle to the process, used for resource management and native operations.</param>
        /// <param name="commandLine">The full command line used to launch the process.</param>
        /// <param name="stdOutHandle">The handle responsible for asynchronously reading the standard output stream of the process.</param>
        /// <param name="stdErrHandle">The handle responsible for asynchronously reading the standard error stream of the process.</param>
        /// <param name="interleavedBuffer">A read-only collection containing the combined output from both standard output and standard error streams.</param>
        /// <param name="stdInHandle">An optional handle for writing to the standard input stream of the process, if input is being provided.</param>
        /// <exception cref="InvalidProgramException">Thrown if the IO completion port or job object is not initialized when required.</exception>
        internal ProcessHandle(ProcessLaunchInfo launchInfo, Process process, uint processId, SafeProcessHandle processHandle, string commandLine, ProcessReadStream? stdOutHandle = null, ProcessReadStream? stdErrHandle = null, IReadOnlyCollection<string>? interleavedBuffer = null, ProcessWriteStream? stdInHandle = null)
        {
            // Internal worker to satisfy S4457 so that the error handling works properly.
            async System.Threading.Tasks.Task<ProcessResult> GetTaskAsync()
            {
                // Ensure that the process is disposed of when the task completes, regardless of success or failure.
                using (processHandle)
                {
                    // Set the client/server success flag if the client started a ShellExecuteEx process invocation.
                    if (ClientServerUtilities.CallerIsClientServerExecutable && launchInfo.UseShellExecute)
                    {
                        ClientServerUtilities.SetOperationSuccessFlag();
                    }

                    // Wait for the process to exit or for a cancellation request, and handle the exit code accordingly.
                    CancellationToken cancellationToken = launchInfo.CancellationToken ?? CancellationToken.None;
                    const uint timeoutExitCode = unchecked((uint)ProcessManager.TimeoutExitCode);
                    int exitCode = ProcessManager.TimeoutExitCode; bool processFinished = false;
                    if (launchInfo.WaitForChildProcesses || launchInfo.KillChildProcessesWithParent)
                    {
                        // Set up a job object and an IO completion port to monitor the process and its child processes.
                        using SafeFileHandle ioCompletionPort = NativeMethods.CreateIoCompletionPort(0);
                        using SafeFileHandle jobObject = NativeMethods.CreateJobObject();
                        JOBOBJECT_ASSOCIATE_COMPLETION_PORT completionPort = new()
                        {
                            CompletionPort = (HANDLE)ioCompletionPort.DangerousGetHandle(),
                            CompletionKey = null,
                        };
                        _ = NativeMethods.SetInformationJobObject(jobObject, in completionPort);
                        if (launchInfo.KillChildProcessesWithParent)
                        {
                            JOBOBJECT_EXTENDED_LIMIT_INFORMATION extendedLimitInformation = new()
                            {
                                BasicLimitInformation = new()
                                {
                                    LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
                                },
                            };
                            _ = NativeMethods.SetInformationJobObject(jobObject, in extendedLimitInformation);
                        }
                        _ = NativeMethods.AssignProcessToJobObject(jobObject, processHandle);

                        // Start a task to monitor the IO completion port for process exit or timeout events.
                        await System.Threading.Tasks.Task.Run(() =>
                        {
                            using CancellationTokenRegistration? ctr = cancellationToken.CanBeCanceled ? cancellationToken.Register(() => NativeMethods.PostQueuedCompletionStatus(ioCompletionPort, timeoutExitCode, default)) : null;
                            while (true)
                            {
                                _ = NativeMethods.GetQueuedCompletionStatus(ioCompletionPort, out uint lpCompletionCode, out _, out nuint lpOverlapped, PInvoke.INFINITE);
                                if (lpCompletionCode == timeoutExitCode)
                                {
                                    if (launchInfo.NoTerminateOnTimeout)
                                    {
                                        // When KillChildProcessesWithParent is true, the job has JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE set.
                                        // Disposing the job would terminate the process we're supposed to let run, so we intentionally
                                        // leak the job handle in this specific scenario to honor the NoTerminateOnTimeout request.
                                        if (launchInfo.KillChildProcessesWithParent)
                                        {
                                            jobObject.SetHandleAsInvalid();
                                        }
                                        break;
                                    }
                                    _ = NativeMethods.TerminateJobObject(jobObject, timeoutExitCode);
                                }
                                else if ((lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_EXIT_PROCESS && (uint)lpOverlapped == processId && !launchInfo.WaitForChildProcesses) || (lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO))
                                {
                                    _ = NativeMethods.GetExitCodeProcess(processHandle, out uint lpExitCode);
                                    exitCode = unchecked((int)lpExitCode);
                                    processFinished = true;
                                    break;
                                }
                            }
                        }, default).ConfigureAwait(false);
                    }
                    else
                    {
                        try
                        {
                            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                            exitCode = process.ExitCode;
                            processFinished = true;
                        }
                        catch (OperationCanceledException) when (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested)
                        {
                            if (!launchInfo.NoTerminateOnTimeout)
                            {
                                try
                                {
                                    process.Kill();
                                }
                                catch (InvalidOperationException)
                                {
                                    // Already exited.
                                    exitCode = process.ExitCode;
                                    processFinished = true;
                                }
                                if (!processFinished)
                                {
                                    await process.WaitForExitAsync(default).ConfigureAwait(false);
                                    exitCode = ProcessManager.TimeoutExitCode;
                                    processFinished = true;
                                }
                            }
                            else
                            {
                                exitCode = ProcessManager.TimeoutExitCode;
                            }
                        }
                    }
                    if (processFinished)
                    {
                        await System.Threading.Tasks.Task.WhenAll(stdOutHandle?.Task ?? System.Threading.Tasks.Task.CompletedTask, stdErrHandle?.Task ?? System.Threading.Tasks.Task.CompletedTask, stdInHandle?.Task ?? System.Threading.Tasks.Task.CompletedTask).WaitAsync(cancellationToken.CanBeCanceled && !cancellationToken.IsCancellationRequested ? cancellationToken : CancellationToken.None).ConfigureAwait(false);
                    }
                    return new(process, launchInfo, commandLine, exitCode, stdOutHandle?.Buffer, stdErrHandle?.Buffer, interleavedBuffer);
                }
            }

            // Confirm all inputs are valid.
            ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);
            ArgumentNullException.ThrowIfNull(processHandle);
            ArgumentNullException.ThrowIfNull(launchInfo);
            ArgumentNullException.ThrowIfNull(process);

            // Store off the incoming parameters.
            Process = process;
            LaunchInfo = launchInfo;
            CommandLine = commandLine;
            Task = GetTaskAsync();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessHandle"/> record with the specified process launch information and process.
        /// </summary>
        /// <param name="launchInfo">The launch information that describes how the process was started.</param>
        /// <param name="process">The Process object representing the running process.</param>
        internal ProcessHandle(ProcessLaunchInfo launchInfo, Process process) : this(launchInfo, process, (uint)process.Id, new(process.Handle, ownsHandle: false), launchInfo.MakeCommandLine())
        {
        }

        /// <summary>
        /// Represents the process associated with the current operation.
        /// </summary>
        /// <remarks>This field provides access to the underlying <see cref="System.Diagnostics.Process"/>
        /// instance. It is read-only and should be used to retrieve information about the process or to perform
        /// operations on it.</remarks>
        public Process Process { get; }

        /// <summary>
        /// Gets the information required to launch a process.
        /// </summary>
        public ProcessLaunchInfo LaunchInfo { get; }

        /// <summary>
        /// Gets the command line string associated with the current process.
        /// </summary>
        public string CommandLine { get; }

        /// <summary>
        /// Represents an asynchronous operation that returns a <see cref="ProcessResult"/>.
        /// </summary>
        /// <remarks>This field holds a <see cref="System.Threading.Tasks.Task{TResult}"/> that, when awaited, provides the
        /// result of a process. The task is read-only and should be awaited to retrieve the <see
        /// cref="ProcessResult"/>.</remarks>
        public System.Threading.Tasks.Task<ProcessResult> Task { get; }

        /// <summary>
        /// Gets the current status of the process completion task.
        /// </summary>
        public System.Threading.Tasks.TaskStatus Status => Task.Status;

        /// <summary>
        /// Gets a value indicating whether the process completion task has been canceled.
        /// </summary>
        public bool IsCanceled => Task.IsCanceled;

        /// <summary>
        /// Gets a value indicating whether the process completion task has completed.
        /// </summary>
        public bool IsCompleted => Task.IsCompleted;

        /// <summary>
        /// Gets a value indicating whether the process completion task has completed with an error.
        /// </summary>
        public bool IsFaulted => Task.IsFaulted;

        /// <summary>
        /// Gets an awaiter for the process completion task.
        /// </summary>
        /// <returns>An awaiter for the process result.</returns>
        public TaskAwaiter<ProcessResult> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        /// <summary>
        /// Configures an awaiter used to await this process handle.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// <see langword="true"/> to attempt to marshal the continuation back to the original context captured;
        /// otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>A configured task awaitable.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "This task is started within our context.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1046:Asynchronous method name should end with 'Async'", Justification = "This isn't appropriate here.")]
        public ConfiguredTaskAwaitable<ProcessResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            return Task.ConfigureAwait(continueOnCapturedContext);
        }
    }
}
