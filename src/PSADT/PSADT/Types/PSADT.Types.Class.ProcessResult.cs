using System.Linq;
using System.Collections.ObjectModel;

namespace PSADT.Types
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
        public ProcessResult(int exitCode, ReadOnlyCollection<string> stdOut, ReadOnlyCollection<string> stdErr)
        {
            ExitCode = exitCode;
            StdOut = stdOut.SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse().ToList().AsReadOnly();
            StdErr = stdErr.SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse().ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        public readonly int ExitCode;

        /// <summary>
        /// Gets the standard output of the process.
        /// </summary>
        public readonly ReadOnlyCollection<string> StdOut;

        /// <summary>
        /// Gets the standard error output of the process.
        /// </summary>
        public readonly ReadOnlyCollection<string> StdErr;
    }
}
