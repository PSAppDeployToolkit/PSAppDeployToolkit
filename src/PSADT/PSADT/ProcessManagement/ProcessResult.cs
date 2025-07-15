using System;
using System.Collections.Generic;
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
        /// <param name="moduleInfo">The module information of the process.</param>
        /// <param name="launchInfo">The launch information of the process.</param>
        /// <param name="commandLine">The command line used to launch the process.</param>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="stdOut">The standard output of the process.</param>
        /// <param name="stdErr">The standard error output of the process.</param>
        /// <param name="interleaved">The interleaved output of the process.</param>
        public ProcessResult(Process process, ProcessModule moduleInfo, ProcessLaunchInfo launchInfo, string commandLine, int exitCode, IReadOnlyCollection<string> stdOut, IReadOnlyCollection<string> stdErr, IReadOnlyCollection<string> interleaved) : this(exitCode, stdOut, stdErr, interleaved)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            ModuleInfo = moduleInfo ?? throw new ArgumentNullException(nameof(moduleInfo));
            LaunchInfo = launchInfo ?? throw new ArgumentNullException(nameof(launchInfo));
            CommandLine = !string.IsNullOrWhiteSpace(commandLine) ? commandLine : throw new ArgumentNullException(nameof(commandLine));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessResult"/> struct.
        /// </summary>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="stdOut">The standard output of the process.</param>
        /// <param name="stdErr">The standard error output of the process.</param>
        /// <param name="interleaved">The interleaved output of the process.</param>
        public ProcessResult(int exitCode, IReadOnlyCollection<string> stdOut, IReadOnlyCollection<string> stdErr, IReadOnlyCollection<string> interleaved)
        {
            ExitCode = exitCode;
            StdOut = MiscUtilities.TrimLeadingTrailingLines(stdOut);
            StdErr = MiscUtilities.TrimLeadingTrailingLines(stdErr);
            Interleaved = MiscUtilities.TrimLeadingTrailingLines(interleaved);
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
        public readonly Process? Process;

        /// <summary>
        /// Gets the module information associated with the process.
        /// </summary>
        public readonly ProcessModule? ModuleInfo;

        /// <summary>
        /// Gets the information required to launch a process.
        /// </summary>
        public readonly ProcessLaunchInfo? LaunchInfo;

        /// <summary>
        /// Gets the command line string associated with the current process.
        /// </summary>
        public readonly string? CommandLine;

        /// <summary>
        /// Gets the exit code of the process, if the process had exited.
        /// </summary>
        public readonly int ExitCode;

        /// <summary>
        /// Gets the standard output of the process.
        /// </summary>
        public readonly IReadOnlyList<string>? StdOut;

        /// <summary>
        /// Gets the standard error output of the process.
        /// </summary>
        public readonly IReadOnlyList<string>? StdErr;

        /// <summary>
        /// Gets the combined standard output and error of the process.
        /// </summary>
        public readonly IReadOnlyList<string>? Interleaved;
    }
}
