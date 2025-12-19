using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a handle to a process, encapsulating the process, its module information, launch details, command
    /// line, and associated asynchronous task.
    /// </summary>
    /// <remarks>This record provides a structured way to manage and interact with a process, offering access
    /// to its core components and the ability to handle its asynchronous operations.</remarks>
    public sealed record ProcessHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessHandle"/> class with the specified process, module
        /// information, launch details, command line, and task.
        /// </summary>
        /// <param name="process">The process associated with this handle. Cannot be null.</param>
        /// <param name="launchInfo">The launch information for the process. Cannot be null.</param>
        /// <param name="commandLine">The command line used to start the process. Cannot be null or empty.</param>
        /// <param name="task">The task representing the asynchronous operation of the process. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the parameters are null or if <paramref name="commandLine"/> is empty.</exception>
        internal ProcessHandle(Process process, ProcessLaunchInfo launchInfo, string commandLine, Task<ProcessResult> task)
        {
            Process = process ?? throw new ArgumentNullException("Process cannot be null.", (Exception?)null);
            LaunchInfo = launchInfo ?? throw new ArgumentNullException("LaunchInfo cannot be null.", (Exception?)null);
            CommandLine = !string.IsNullOrWhiteSpace(commandLine) ? commandLine : throw new ArgumentNullException("CommandLine cannot be null or empty.", (Exception?)null);
            Task = task ?? throw new ArgumentNullException("Task cannot be null.", (Exception?)null);
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
        /// <remarks>This field holds a <see cref="Task{TResult}"/> that, when awaited, provides the
        /// result of a process. The task is read-only and should be awaited to retrieve the <see
        /// cref="ProcessResult"/>.</remarks>
        public Task<ProcessResult> Task { get; }
    }
}
