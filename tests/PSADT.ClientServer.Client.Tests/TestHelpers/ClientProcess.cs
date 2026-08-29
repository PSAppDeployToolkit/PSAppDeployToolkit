using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PSADT.ClientServer.Client.Tests.TestHelpers
{
    /// <summary>
    /// Runs the client executable and collects what it exited with and wrote.
    /// </summary>
    /// <remarks>
    /// The exit code is the contract the PowerShell module reads through
    /// <c>Invoke-ADTClientServerOperation</c>, so it is what these tests assert on. Reaching it means
    /// running the real executable: <c>Main</c> cannot be called in process, because it answers an
    /// empty argument list with a modal dialog and answers a failure in a launcher with
    /// <c>Environment.FailFast</c>.
    /// <para>
    /// Every run is bounded and the process killed if it outstays that bound, so a switch that blocks
    /// on a dialog fails its own test rather than hanging the suite.
    /// </para>
    /// </remarks>
    internal static class ClientProcess
    {
        /// <summary>
        /// How long a client is given before it is killed.
        /// </summary>
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Runs the client with the given arguments and waits for it to exit.
        /// </summary>
        /// <param name="arguments">The arguments to pass.</param>
        /// <returns>What the client exited with and wrote.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the client is absent or could not be started.</exception>
        /// <exception cref="TimeoutException">Thrown if the client did not exit within the time allowed.</exception>
        public static async Task<ClientResult> RunAsync(params string[] arguments)
        {
            if (TestEnvironment.ClientExecutable is not FileInfo executable)
            {
                throw new InvalidOperationException("The client executable was not found beside the test assembly.");
            }
            using Process process = Process.Start(new ProcessStartInfo(executable.FullName, Quote(arguments))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }) ?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Failed to start [{executable.FullName}]."));

            // Both streams are drained before the wait so a client writing more than a pipe buffer
            // holds cannot block on a write nobody is reading.
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            if (!await Task.Run(() => process.WaitForExit((int)Timeout.TotalMilliseconds), TestContext.Current.CancellationToken).ConfigureAwait(false))
            {
                TryKill(process);
                throw new TimeoutException(string.Create(CultureInfo.InvariantCulture, $"The client did not exit within {Timeout.TotalSeconds} seconds. Arguments: [{Quote(arguments)}]."));
            }
            return new(process.ExitCode, await standardOutput.ConfigureAwait(false), await standardError.ConfigureAwait(false));
        }

        /// <summary>
        /// Joins arguments into a command line, quoting any that contain whitespace.
        /// </summary>
        /// <remarks>
        /// .NET Framework has no <c>ArgumentList</c>, so the command line is assembled here. Serialized
        /// arguments dictionaries are Base64 and the switches are single words, so nothing these tests
        /// pass contains a quote of its own to escape.
        /// </remarks>
        /// <param name="arguments">The arguments to join.</param>
        /// <returns>The assembled command line.</returns>
        private static string Quote(string[] arguments)
        {
            return string.Join(" ", (arguments ?? []).Select(static argument =>
                argument.Any(char.IsWhiteSpace) ? $"\"{argument}\"" : argument));
        }

        /// <summary>
        /// Kills a process that outstayed its bound, ignoring a race with its own exit.
        /// </summary>
        /// <param name="process">The process to kill.</param>
        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between the check and the kill, which is the outcome wanted anyway.
            }
        }
    }
}
