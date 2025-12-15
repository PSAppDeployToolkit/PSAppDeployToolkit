using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PSADT.Utilities;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents the result of a process execution, including exit code and standard output/error.
    /// </summary>
    public sealed record ProcessResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessResult"/> struct.
        /// </summary>
        /// <param name="process">The process that was executed.</param>
        /// <param name="launchInfo">The launch information of the process.</param>
        /// <param name="commandLine">The command line used to launch the process.</param>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="stdOut">The standard output of the process.</param>
        /// <param name="stdErr">The standard error output of the process.</param>
        /// <param name="interleaved">The interleaved output of the process.</param>
        public ProcessResult(Process process, ProcessLaunchInfo launchInfo, string commandLine, int exitCode, IReadOnlyList<string> stdOut, IReadOnlyList<string> stdErr, IReadOnlyList<string> interleaved) : this(exitCode, stdOut, stdErr, interleaved)
        {
            Process = process ?? throw new ArgumentNullException("Process cannot be null.", (Exception?)null);
            LaunchInfo = launchInfo ?? throw new ArgumentNullException("LaunchInfo cannot be null.", (Exception?)null);
            CommandLine = !string.IsNullOrWhiteSpace(commandLine) ? commandLine : throw new ArgumentNullException("CommandLine cannot be null.", (Exception?)null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessResult"/> struct.
        /// </summary>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="stdOut">The standard output of the process.</param>
        /// <param name="stdErr">The standard error output of the process.</param>
        /// <param name="interleaved">The interleaved output of the process.</param>
        public ProcessResult(int exitCode, IReadOnlyList<string> stdOut, IReadOnlyList<string> stdErr, IReadOnlyList<string> interleaved) : this(exitCode)
        {
            StdOut = new ReadOnlyCollection<string>([.. MiscUtilities.TrimLeadingTrailingLines(stdOut)]);
            StdErr = new ReadOnlyCollection<string>([.. MiscUtilities.TrimLeadingTrailingLines(stdErr)]);
            Interleaved = new ReadOnlyCollection<string>([.. MiscUtilities.TrimLeadingTrailingLines(interleaved)]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessResult"/> struct.
        /// This is only here as sometimes we need to falsify a result in the module.
        /// </summary>
        /// <param name="exitCode">The exit code of the process.</param>
        public ProcessResult(int exitCode)
        {
            ExitCode = exitCode;
        }

        /// <summary>
        /// Represents the process associated with the current operation.
        /// </summary>
        public Process? Process { get; }

        /// <summary>
        /// Gets the information required to launch a process.
        /// </summary>
        public ProcessLaunchInfo? LaunchInfo { get; }

        /// <summary>
        /// Gets the command line string associated with the current process.
        /// </summary>
        public string? CommandLine { get; }

        /// <summary>
        /// Gets the exit code of the process, if the process had exited.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Gets the standard output of the process.
        /// </summary>
        public IReadOnlyList<string>? StdOut { get; }

        /// <summary>
        /// Gets the standard error output of the process.
        /// </summary>
        public IReadOnlyList<string>? StdErr { get; }

        /// <summary>
        /// Gets the combined standard output and error of the process.
        /// </summary>
        public IReadOnlyList<string>? Interleaved { get; }
    }
}
