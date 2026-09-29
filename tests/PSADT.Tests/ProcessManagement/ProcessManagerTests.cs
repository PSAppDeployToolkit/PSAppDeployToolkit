using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PSADT.Interop;
using PSADT.ProcessManagement;
using PSADT.Tests.TestHelpers;
using Windows.Win32.Foundation;
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
        /// Verifies that output whose last line has no line break is captured, which is how output ends when a
        /// process is terminated part-way through writing a line.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_CapturesALastLineWithNoLineBreakAsync()
        {
            // Act
            using ProcessResult result = await RunAsync("<nul set /p =no-line-break& exit 0").ConfigureAwait(true);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(["no-line-break"], result.StdOut);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "The variable has to be seeded on this process without the wrapper, since what is under test is that the launch carries this process's environment down.")]
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
        /// Verifies that cancelling a launch ends it promptly with the timeout code and terminates the process,
        /// rather than leaving it or anything it started running with nothing waiting on it.
        /// </summary>
        /// <remarks>
        /// Cancellation is watched for through the job object, which is only set up when the launch was
        /// asked to account for child processes - so that is asked for here. The process launched starts a
        /// ping that runs for two minutes, so a launch that ends well inside that is one whose cancellation
        /// reached the ping as well.
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            await cancellation.CancelAsync().ConfigureAwait(true);
            using ProcessResult result = await handle.Task.ConfigureAwait(true);
            TimeSpan elapsed = stopwatch.Elapsed;

            // Assert
            Assert.Equal(ProcessManager.TimeoutExitCode, result.ExitCode);
            Assert.True(handle.Process.HasExited, $"Process {processId.ToString(CultureInfo.InvariantCulture)} was left running after being cancelled.");
            Assert.True(elapsed < TimeSpan.FromSeconds(30), $"The launch took {elapsed.TotalSeconds.ToString("N0", CultureInfo.InvariantCulture)}s to end after being cancelled, so something it started outlived the cancellation.");
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
        /// Verifies that a process created to bypass image file execution options runs to completion. The flag
        /// that bypasses them also makes this process its debugger, and a debuggee nobody releases never runs.
        /// </summary>
        /// <remarks>
        /// The launch is given a timeout rather than an open-ended wait, so a process left debugged fails the
        /// test rather than hanging it.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_RunsAProcessCreatedToBypassImageFileExecutionOptionsAsync()
        {
            // Arrange
            using CancellationTokenSource timeout = new(LaunchTimeout);
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "echo ran& exit 7"], bypassIfeo: true, createNoWindow: true, cancellationToken: timeout.Token);

            // Act
            using ProcessResult result = await LaunchAsync(launchInfo).ConfigureAwait(true);

            // Assert
            Assert.Equal(7, result.ExitCode);
            Assert.Equal(["ran"], result.StdOut);
        }

        /// <summary>
        /// Verifies that a launch through the shell runs to completion and reports its exit code, which is
        /// what proves the process the shell was asked to hold suspended is released once it has been set up.
        /// </summary>
        /// <remarks>
        /// The shell is used for a console application only when a window is not asked to be suppressed, so
        /// the window is hidden through the style instead. The launch is given a timeout rather than an
        /// open-ended wait, so a process left suspended fails the test rather than hanging it.
        /// </remarks>
        /// <param name="exitCode">The code for the process to exit with.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        public async Task LaunchAsync_ShellExecute_ReportsTheExitCodeAsync(int exitCode)
        {
            // Arrange
            using CancellationTokenSource timeout = new(LaunchTimeout);
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", $"exit {exitCode.ToString(CultureInfo.InvariantCulture)}"], useShellExecute: true, windowStyle: ProcessWindowStyle.Hidden, cancellationToken: timeout.Token);

            // Act
            using ProcessResult result = await LaunchAsync(launchInfo).ConfigureAwait(true);

            // Assert
            Assert.Equal(exitCode, result.ExitCode);
        }

        /// <summary>
        /// Verifies that the working directory, arguments and verb all reach the shell, from a caller already
        /// on a single-threaded apartment, which is where the module calls from.
        /// </summary>
        /// <remarks>
        /// Output cannot be captured through the shell, so the process creates a file instead, by a relative
        /// name, so that the file turning up in the directory is itself the proof.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "The launch is started by this test, on the apartment it hops onto to do so.")]
        [Fact]
        public async Task LaunchAsync_ShellExecute_StartsInTheWorkingDirectoryItWasGivenAsync()
        {
            // Arrange
            using TempDirectory temp = new();
            using CancellationTokenSource timeout = new(LaunchTimeout);
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "cd > cwd.txt"], temp.FullName, useShellExecute: true, verb: "open", windowStyle: ProcessWindowStyle.Hidden, cancellationToken: timeout.Token);

            // Act
            ProcessHandle? handle = null;
            StaThread.Run(() => handle = ProcessManager.LaunchAsync(launchInfo));
            Assert.NotNull(handle);
            using ProcessResult result = await handle.Task.ConfigureAwait(true);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Join(temp.FullName, "cwd.txt")), "The process did not run in the directory it was given.");
        }

        /// <summary>
        /// Verifies that a process launched through the shell is in its job before it can start anything, so
        /// a child it starts straight away is killed with it rather than escaping.
        /// </summary>
        /// <remarks>
        /// The interpreter starts a ping that would run for two minutes, and cancelling the launch has to end
        /// the ping as well. The job is private to the handle, so membership is checked against any job at
        /// all; that says something only when this host is outside every job, which is the usual case.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_ShellExecute_KillsChildProcessesWithTheParentAsync()
        {
            // Arrange
            using CancellationTokenSource cancellation = new(LaunchTimeout);
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "ping -n 120 127.0.0.1"], useShellExecute: true, killChildProcessesWithParent: true, windowStyle: ProcessWindowStyle.Hidden, cancellationToken: cancellation.Token);

            // Act
            ProcessHandle? handle = ProcessManager.LaunchAsync(launchInfo);
            Assert.NotNull(handle);
            using Process ping = await FindChildAsync(handle.Process.Id, "PING").ConfigureAwait(true);
            try
            {
                using Process host = Process.GetCurrentProcess();
                _ = NativeMethods.IsProcessInJob(host.SafeHandle, out BOOL hostInJob);
                _ = NativeMethods.IsProcessInJob(ping.SafeHandle, out BOOL pingInJob);
                await cancellation.CancelAsync().ConfigureAwait(true);
                using ProcessResult result = await handle.Task.ConfigureAwait(true);

                // Assert
                Assert.Equal(ProcessManager.TimeoutExitCode, result.ExitCode);
                Assert.True(hostInJob || pingInJob, "The ping was outside every job, so the launch's job did not contain it.");
                Assert.True(ping.WaitForExit(10000), "The ping outlived the launch it belonged to, so the launch's job did not contain it.");
            }
            finally
            {
                if (!ping.HasExited)
                {
                    ping.Kill();
                }
            }
        }

        /// <summary>
        /// Verifies that the priority class reaches a process launched through the shell, which is applied
        /// after the launch rather than through the shell itself.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_ShellExecute_AppliesThePriorityClassAsync()
        {
            // Arrange: the interpreter runs long enough to be looked at, and its ping goes with it
            using CancellationTokenSource cancellation = new(LaunchTimeout);
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "ping -n 120 127.0.0.1"], useShellExecute: true, killChildProcessesWithParent: true, windowStyle: ProcessWindowStyle.Hidden, priorityClass: ProcessPriorityClass.BelowNormal, cancellationToken: cancellation.Token);

            // Act
            ProcessHandle? handle = ProcessManager.LaunchAsync(launchInfo);
            Assert.NotNull(handle);
            try
            {
                // Assert
                Assert.Equal(ProcessPriorityClass.BelowNormal, handle.Process.PriorityClass);
            }
            finally
            {
                await cancellation.CancelAsync().ConfigureAwait(true);
                (await handle.Task.ConfigureAwait(true)).Dispose();
            }
        }

        /// <summary>
        /// Verifies that a path to something that is not there fails at the launch through the shell as
        /// well, rather than producing a handle to a process that does not exist.
        /// </summary>
        [Fact]
        public void LaunchAsync_ShellExecute_FailsForAFileThatIsNotThere()
        {
            // Arrange
            ProcessLaunchInfo launchInfo = new(Path.Join(Environment.SystemDirectory, "PSADTNoSuchExecutable.exe"), useShellExecute: true, windowStyle: ProcessWindowStyle.Hidden);

            // Act & Assert
            Assert.NotNull(Record.Exception(() => ProcessManager.LaunchAsync(launchInfo)));
        }

        /// <summary>
        /// Verifies that a process the shell creates to bypass image file execution options runs to completion,
        /// which needs the debugger the flag attaches to be detached on the thread that made the call.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LaunchAsync_ShellExecute_RunsAProcessCreatedToBypassImageFileExecutionOptionsAsync()
        {
            // Arrange
            using CancellationTokenSource timeout = new(LaunchTimeout);
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "exit 7"], bypassIfeo: true, useShellExecute: true, windowStyle: ProcessWindowStyle.Hidden, cancellationToken: timeout.Token);

            // Act
            using ProcessResult result = await LaunchAsync(launchInfo).ConfigureAwait(true);

            // Assert
            Assert.Equal(7, result.ExitCode);
        }

        /// <summary>
        /// Verifies that bypassing image file execution options is refused for a launch the shell would perform
        /// through DDE, since the shell waits on that inside the call and a debugged process could never answer.
        /// </summary>
        /// <remarks>
        /// The launch is only attempted once the detector has agreed the target is a DDE launch, since a launch
        /// it did not refuse would go through to whatever handles the association on this machine.
        /// </remarks>
        [Fact(Skip = "Requires a DDE association to be registered.", SkipUnless = nameof(TestEnvironment.HasDdeAssociation), SkipType = typeof(TestEnvironment))]
        public void LaunchAsync_ShellExecute_RefusesToBypassImageFileExecutionOptionsForADdeLaunch()
        {
            // Arrange
            string? target = TestEnvironment.DdeLaunchTarget;
            Assert.NotNull(target);
            ProcessLaunchInfo launchInfo = new(target, bypassIfeo: true, useShellExecute: true, windowStyle: ProcessWindowStyle.Hidden);
            Assert.True(HasDdeCommand(launchInfo), "The target is not taken for a DDE launch, so launching it would not be refused.");

            // Act & Assert
            _ = Assert.Throws<NotSupportedException>(() => ProcessManager.LaunchAsync(launchInfo));
        }

        /// <summary>
        /// Verifies that a launch is not taken for a DDE conversation where nothing is registered for it:
        /// an executable, under the default verb and another; something with no extension at all; and an
        /// extension and a protocol that exist nowhere.
        /// </summary>
        /// <param name="filePath">The file or protocol to launch.</param>
        /// <param name="verb">The verb to launch it with, or null for the default.</param>
        [Theory]
        [InlineData("cmd.exe", null)]
        [InlineData("cmd.exe", "runas")]
        [InlineData("cmd", null)]
        [InlineData("file.psadt-nosuch", "open")]
        [InlineData("psadt-nosuch:launch", null)]
        public void HasDdeCommand_IsFalseWhereNothingIsRegistered(string filePath, string? verb)
        {
            Assert.False(HasDdeCommand(new(filePath, useShellExecute: true, verb: verb)));
        }

        /// <summary>
        /// Verifies that a launch the shell would carry out through a DDE conversation is recognised as one,
        /// since that is the launch that must not be held suspended.
        /// </summary>
        [Fact(Skip = "Requires a DDE association to be registered.", SkipUnless = nameof(TestEnvironment.HasDdeAssociation), SkipType = typeof(TestEnvironment))]
        public void HasDdeCommand_IsTrueForARegisteredAssociation()
        {
            // Arrange
            string? target = TestEnvironment.DdeLaunchTarget;
            Assert.NotNull(target);

            // Act & Assert
            Assert.True(HasDdeCommand(new(target, useShellExecute: true)));
        }

        /// <summary>
        /// Asks the launcher's own DDE detector, which stays private rather than being widened for the tests.
        /// </summary>
        /// <param name="launchInfo">The launch to inspect.</param>
        /// <returns><see langword="true"/> if the shell would perform the launch through DDE; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the detector cannot be found or does not return a boolean.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "The detector is deliberately private, so the tests reach it by reflection rather than widening it.")]
        private static bool HasDdeCommand(ProcessLaunchInfo launchInfo)
        {
            Type api = typeof(ProcessManager).GetNestedType("ShellExecuteExApi", BindingFlags.NonPublic) ?? throw new InvalidOperationException("The ShellExecuteExApi type was not found.");
            MethodInfo detector = api.GetMethod("HasDdeCommand", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("The HasDdeCommand method was not found.");
            return detector.Invoke(null, [launchInfo]) is bool result ? result : throw new InvalidOperationException("The HasDdeCommand method did not return a boolean.");
        }

        /// <summary>
        /// Waits for a process to start a child of the given name, which one held suspended by its launch can
        /// only do once it has been released.
        /// </summary>
        /// <param name="parentId">The identifier of the process expected to start the child.</param>
        /// <param name="name">The child's process name.</param>
        /// <returns>The child, which the caller disposes.</returns>
        private static async Task<Process> FindChildAsync(int parentId, string name)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (true)
            {
                Process[] candidates = Process.GetProcessesByName(name);
                Process? child = Array.Find(candidates, candidate => ProcessUtilities.GetParentProcessId(candidate.Id) == parentId);
                foreach (Process candidate in candidates.Where(candidate => candidate != child))
                {
                    using (candidate)
                    {
                        // CodeQL prefers using statements.
                    }
                }
                if (child is not null)
                {
                    return child;
                }
                Assert.True(stopwatch.Elapsed < LaunchTimeout, $"Process {parentId.ToString(CultureInfo.InvariantCulture)} did not start a {name} process.");
                await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken).ConfigureAwait(true);
            }
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

        /// <summary>
        /// How long a launch through the shell is given before it is cancelled, which ends a process that was
        /// never released as a failure rather than a hang.
        /// </summary>
        private static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(60);
    }
}
