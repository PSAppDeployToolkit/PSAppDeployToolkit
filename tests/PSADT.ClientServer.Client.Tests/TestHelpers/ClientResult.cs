using System;
using System.Globalization;

namespace PSADT.ClientServer.Client.Tests.TestHelpers
{
    /// <summary>
    /// What a client run exited with and wrote.
    /// </summary>
    /// <remarks>
    /// Deliberately not a positional record. That form generates <c>init</c> accessors, which are not
    /// used anywhere in this repository: the setter is enforced by the compiler alone, so reflection
    /// writes straight through it.
    /// </remarks>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="standardOutput">Everything written to standard output.</param>
    /// <param name="standardError">Everything written to standard error.</param>
    internal sealed class ClientResult(int exitCode, string standardOutput, string standardError)
    {
        /// <summary>
        /// The process exit code.
        /// </summary>
        public int ExitCode { get; } = exitCode;

        /// <summary>
        /// Everything written to standard output.
        /// </summary>
        public string StandardOutput { get; } = standardOutput;

        /// <summary>
        /// Everything written to standard error.
        /// </summary>
        public string StandardError { get; } = standardError;

        /// <summary>
        /// The exit code as the enumeration the client reports it with.
        /// </summary>
        public ClientExitCode ExitCodeAsEnum => (ClientExitCode)ExitCode;

        /// <summary>
        /// Describes the run, so a failed assertion names what the client actually did.
        /// </summary>
        /// <returns>The exit code and both streams.</returns>
        public string Describe()
        {
            return string.Create(CultureInfo.InvariantCulture, $"ExitCode: {ExitCode}{Environment.NewLine}StdOut: {StandardOutput}{Environment.NewLine}StdErr: {StandardError}");
        }
    }
}
