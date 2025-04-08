using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSADT.Execution
{
    /// <summary>
    /// Represents the result of a process execution, including exit code and standard output/error.
    /// </summary>
    public sealed class ProcessResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessResult"/> struct.
        /// </summary>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="stdOut">The standard output of the process.</param>
        /// <param name="stdErr">The standard error output of the process.</param>
        public ProcessResult(uint processId, uint? exitCode, ReadOnlyCollection<string> stdOut, ReadOnlyCollection<string> stdErr, ReadOnlyCollection<string> interleaved)
        {
            if (exitCode.HasValue)
            {
                unchecked
                {
                    ExitCode = (int)exitCode.Value;
                }
            }
            ProcessId = processId;
            StdOut = stdOut.SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse().ToList().AsReadOnly();
            StdErr = stdErr.SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse().ToList().AsReadOnly();
            Interleaved = interleaved.SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse().ToList().AsReadOnly();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessResult"/> struct.
        /// </summary>
        /// <param name="exitCode"></param>
        public ProcessResult(int exitCode)
        {
            ExitCode = exitCode;
        }

        /// <summary>
        /// Gets the process ID of the executed process.
        /// </summary>
        public readonly uint ProcessId;

        /// <summary>
        /// Gets the exit code of the process, if the process had exited.
        /// </summary>
        public readonly int? ExitCode;

        /// <summary>
        /// Gets the standard output of the process.
        /// </summary>
        public readonly ReadOnlyCollection<string> StdOut = new List<string>().AsReadOnly();

        /// <summary>
        /// Gets the standard error output of the process.
        /// </summary>
        public readonly ReadOnlyCollection<string> StdErr = new List<string>().AsReadOnly();

        /// <summary>
        /// Gets the combined standard output and error of the process.
        /// </summary>
        public readonly ReadOnlyCollection<string> Interleaved = new List<string>().AsReadOnly();
    }
}
