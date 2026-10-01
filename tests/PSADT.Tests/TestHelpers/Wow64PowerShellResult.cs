using System;
using System.Globalization;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// How a script run by <see cref="Wow64PowerShell"/> ended.
    /// </summary>
    /// <remarks>
    /// Deliberately not a positional record. That form generates <c language="csharp">init</c> accessors, which are not
    /// used anywhere in this repository.
    /// </remarks>
    internal sealed class Wow64PowerShellResult
    {
        /// <summary>
        /// Reads how the script ended out of what the process exited with and wrote.
        /// </summary>
        /// <param name="exitCode">The process exit code.</param>
        /// <param name="standardOutput">Everything written to standard output.</param>
        /// <param name="standardError">Everything written to standard error.</param>
        internal Wow64PowerShellResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
            foreach (string line in standardOutput.TrimStart('\uFEFF').Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("OK\t", StringComparison.Ordinal))
                {
                    Value = line[3..];
                    ExceptionType = null;
                }
                else if (line.StartsWith("EX\t", StringComparison.Ordinal))
                {
                    Value = null;
                    ExceptionType = line[3..].Split('\t')[0];
                }
            }
        }

        /// <summary>
        /// What the script output, or <see langword="null"/> if it threw.
        /// </summary>
        public string? Value { get; }

        /// <summary>
        /// The full name of the exception the script threw, or <see langword="null"/> if it did not.
        /// </summary>
        public string? ExceptionType { get; }

        /// <summary>
        /// The process exit code.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Everything written to standard output.
        /// </summary>
        public string StandardOutput { get; }

        /// <summary>
        /// Everything written to standard error.
        /// </summary>
        public string StandardError { get; }

        /// <summary>
        /// Describes the run, so a failed assertion names what the script actually did.
        /// </summary>
        /// <returns>The exit code and both streams.</returns>
        public string Describe()
        {
            return string.Create(CultureInfo.InvariantCulture, $"ExitCode: {ExitCode}{Environment.NewLine}StdOut: {StandardOutput}{Environment.NewLine}StdErr: {StandardError}");
        }
    }
}
