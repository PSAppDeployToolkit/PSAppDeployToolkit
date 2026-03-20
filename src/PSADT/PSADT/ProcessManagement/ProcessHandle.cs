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
        /// Initializes a new instance of the <see cref="ProcessHandle"/> class with the specified launch state.
        /// </summary>
        /// <param name="launchState">The process launch state containing information about the started process, its launch parameters, and the
        /// result to be tracked. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the parameters are null</exception>
        internal ProcessHandle(ProcessLaunchState launchState)
        {
            ArgumentNullException.ThrowIfNull(launchState);
            async Task<ProcessResult> GetTaskAsync()
            {
                using (launchState)
                {
                    return await launchState.GetProcessResultAsync().ConfigureAwait(false);
                }
            }
            Process = launchState.Process;
            LaunchInfo = launchState.LaunchInfo;
            CommandLine = launchState.CommandLine;
            Task = GetTaskAsync();
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
