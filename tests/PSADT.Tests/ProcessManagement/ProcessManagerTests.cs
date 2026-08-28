using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests launching a process and collecting what it did.
    /// </summary>
    /// <remarks>
    /// Every launch here is the command interpreter running a single built-in command and exiting, in the
    /// caller's own account and with no window. Nothing installed, nothing configured, nothing left
    /// behind - the machine is in the same state afterwards as before, and the one test that starts a
    /// long-running process cancels it, which terminates it.
    /// <para>
    /// Launching as another user is not exercised. It brokers a token, which registers a scheduled task
    /// to run a broker as the local system account, and it cannot succeed on a machine with no second
    /// session signed in regardless.
    /// </para>
    /// <para>
    /// Note that the streams are only captured for a console application launched with no window: a
    /// launch that puts a console on screen leaves the output on that console, where there is nothing to
    /// read it from. So every test that reads output asks for no window, which is also what keeps a
    /// window from appearing while the suite runs.
    /// </para>
    /// </remarks>
    public sealed class ProcessManagerTests
    {
        /// <summary>
        /// Verifies that the exit code a process ends with is the exit code reported.
        /// </summary>
        /// <param name="exitCode">The code for the process to exit with.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(42)]
        public async Task LaunchAsync_ReportsTheExitCodeAsync(int exitCode)
        {
            // Act
            using ProcessResult result = await RunAsync($"exit {exitCode.ToString(CultureInfo.InvariantCulture)}").ConfigureAwait(true);

            // Assert
            Assert.Equal(exitCode, result.ExitCode);
        }

        /// <summary>
        /// Verifies that what a process writes to its output stream is captured.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_CapturesStandardOutputAsync()
        {
            // Act
            using ProcessResult result = await RunAsync("echo out-one& echo out-two").ConfigureAwait(true);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(["out-one", "out-two"], result.StdOut);
            Assert.Empty(result.StdErr);
        }

        /// <summary>
        /// Verifies that what a process writes to its error stream is captured separately from its
        /// output, since a caller decides whether something went wrong by looking at them apart.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_CapturesStandardErrorAsync()
        {
            // Act
            using ProcessResult result = await RunAsync("echo to-error 1>&2").ConfigureAwait(true);

            // Assert
            Assert.Equal(["to-error"], result.StdErr);
            Assert.Empty(result.StdOut);
        }

        /// <summary>
        /// Verifies that both streams are also collected together, which is what a log wants: the two in
        /// the order they were actually written rather than one after the other.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_CollectsBothStreamsTogetherAsync()
        {
            // Act
            using ProcessResult result = await RunAsync("echo to-output& echo to-error 1>&2").ConfigureAwait(true);

            // Assert
            Assert.Contains("to-output", result.Interleaved, StringComparer.Ordinal);
            Assert.Contains("to-error", result.Interleaved, StringComparer.Ordinal);
            Assert.Equal(result.StdOut.Count + result.StdErr.Count, result.Interleaved.Count);
        }

        /// <summary>
        /// Verifies that what a caller supplies as standard input reaches the process.
        /// </summary>
        /// <remarks>
        /// Sorting is used because it has to read its input to completion before it can write anything,
        /// so a test that passes proves the whole input arrived rather than merely the first line of it.
        /// <para>
        /// It also catches a byte-order mark at the head of the stream, which is what this found when it
        /// was first written. A mark is not skipped by the process reading it - it arrives as part of the
        /// first line - and sorting moves that line to the end, where it is unmistakable.
        /// </para>
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_WritesStandardInputAsync()
        {
            // Arrange
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "sort"], standardInput: ["charlie", "alpha", "bravo"], createNoWindow: true);

            // Act
            using ProcessResult result = await LaunchAsync(launchInfo).ConfigureAwait(true);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(["alpha", "bravo", "charlie"], result.StdOut);
        }

        /// <summary>
        /// Verifies that a process is started in the working directory it was given, since a launch that
        /// ignored it would run an installer against the wrong folder.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_StartsInTheWorkingDirectoryItWasGivenAsync()
        {
            // Arrange
            using TempDirectory temp = new();
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "cd"], temp.Directory.FullName, createNoWindow: true);

            // Act
            using ProcessResult result = await LaunchAsync(launchInfo).ConfigureAwait(true);

            // Assert
            Assert.Equal(temp.Directory.FullName, Assert.Single(result.StdOut), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the arguments reach the process as separate arguments rather than as one string
        /// the process has to split again.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_PassesArgumentsThroughAsync()
        {
            // Act: a value with a space in it, which is where quoting either works or does not
            using ProcessResult result = await RunAsync("echo a value with spaces").ConfigureAwait(true);

            // Assert
            Assert.Equal("a value with spaces", Assert.Single(result.StdOut));
        }

        /// <summary>
        /// Verifies that a process inherits the caller's environment, which is how a deployment passes
        /// context down to what it launches.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_PassesTheCallersEnvironmentDownAsync()
        {
            // Arrange
            string name = $"PSADT_TESTS_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
            try
            {
                Environment.SetEnvironmentVariable(name, "inherited");

                // Act
                using ProcessResult result = await RunAsync($"echo %{name}%").ConfigureAwait(true);

                // Assert
                Assert.Equal("inherited", Assert.Single(result.StdOut));
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, value: null);
            }
        }

        /// <summary>
        /// Verifies that cancelling a launch ends it with the timeout code and terminates the process,
        /// rather than leaving it running with nothing waiting on it.
        /// </summary>
        /// <remarks>
        /// Cancellation is watched for through the job object, which is only set up when the launch was
        /// asked to account for child processes - so that is asked for here. The process launched sleeps
        /// far longer than the test will wait, so the only way the test finishes is by the cancellation
        /// being acted on.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_CancellingTerminatesTheProcessAsync()
        {
            // Arrange
            using CancellationTokenSource cancellation = new();
            ProcessLaunchInfo launchInfo = new(
                CommandInterpreter,
                ["/c", "ping -n 120 127.0.0.1"],
                createNoWindow: true,
                waitForChildProcesses: true,
                cancellationToken: cancellation.Token);

            // Act
            ProcessHandle? handle = ProcessManager.LaunchAsync(launchInfo);
            Assert.NotNull(handle);
            int processId = handle.Process.Id;
            await cancellation.CancelAsync().ConfigureAwait(true);
            using ProcessResult result = await handle.Task.ConfigureAwait(true);

            // Assert
            Assert.Equal(ProcessManager.TimeoutExitCode, result.ExitCode);
            Assert.True(handle.Process.HasExited, $"Process {processId.ToString(CultureInfo.InvariantCulture)} was left running after being cancelled.");
        }

        /// <summary>
        /// Verifies that a launch with nothing to launch is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void LaunchAsync_RefusesANullLaunchInfo()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ProcessManager.LaunchAsync(null!));
        }

        /// <summary>
        /// Verifies that a path to something that is not there fails at the launch rather than producing
        /// a handle to a process that does not exist.
        /// </summary>
        [Fact]
        public void LaunchAsync_FailsForAnExecutableThatIsNotThere()
        {
            // Arrange
            ProcessLaunchInfo launchInfo = new(Path.Join(Environment.SystemDirectory, "PSADTNoSuchExecutable.exe"), createNoWindow: true);

            // Act & Assert
            Assert.NotNull(Record.Exception(() => ProcessManager.LaunchAsync(launchInfo)));
        }

        /// <summary>
        /// Runs a single command through the interpreter with its output captured.
        /// </summary>
        /// <param name="command">The command for the interpreter to run.</param>
        /// <returns>What the process did.</returns>
        private static Task<ProcessResult> RunAsync(string command)
        {
            return LaunchAsync(new(CommandInterpreter, ["/c", command], createNoWindow: true));
        }

        /// <summary>
        /// Launches a process and waits for it, failing the test rather than returning nothing if it
        /// could not be started at all.
        /// </summary>
        /// <param name="launchInfo">What to launch.</param>
        /// <returns>What the process did.</returns>
        private static Task<ProcessResult> LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            ProcessHandle? handle = ProcessManager.LaunchAsync(launchInfo);
            Assert.NotNull(handle);
            return handle.Task;
        }

        /// <summary>
        /// The command interpreter, which every Windows installation has and which can be made to produce
        /// a known exit code and known output without anything being installed to do it.
        /// </summary>
        private static readonly string CommandInterpreter = Path.Join(Environment.SystemDirectory, "cmd.exe");
    }
}
