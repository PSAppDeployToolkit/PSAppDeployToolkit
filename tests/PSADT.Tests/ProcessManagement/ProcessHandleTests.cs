using System;
using System.IO;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the handle a launch hands back.
    /// </summary>
    /// <remarks>
    /// This is what a caller holds between starting a process and finding out what it did, so what
    /// matters is that it describes the launch it came from and that every way of waiting on it arrives
    /// at the same answer. It is awaitable directly as well as through the task it wraps, and a caller
    /// may reasonably use either.
    /// <para>
    /// The subject is the command interpreter exiting immediately, in the caller's own account and with
    /// no window, so nothing is left behind.
    /// </para>
    /// </remarks>
    public sealed class ProcessHandleTests
    {
        /// <summary>
        /// Verifies that the handle describes the launch it came from, so a caller holding one can say
        /// what it started without having kept the request.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ProcessHandle_DescribesTheLaunchAsync()
        {
            // Arrange
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "exit 0"], createNoWindow: true);

            // Act
            ProcessHandle handle = LaunchAsync(launchInfo);
            try
            {
                // Assert
                Assert.Same(launchInfo, handle.LaunchInfo);
                Assert.False(string.IsNullOrWhiteSpace(handle.CommandLine));
                Assert.Contains("cmd.exe", handle.CommandLine, StringComparison.OrdinalIgnoreCase);
                Assert.True(handle.Process.Id > 0);
            }
            finally
            {
                (await handle.Task.ConfigureAwait(true)).Dispose();
            }
        }

        /// <summary>
        /// Verifies that the command line reported is the one the launch was assembled into, so a log
        /// records what actually ran rather than what was asked for.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task CommandLine_IsTheAssembledCommandLineAsync()
        {
            // Arrange
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "exit 0"], createNoWindow: true);

            // Act
            ProcessHandle handle = LaunchAsync(launchInfo);
            using ProcessResult result = await handle.Task.ConfigureAwait(true);

            // Assert: the handle and the result agree, and both agree with the request
            Assert.Equal(launchInfo.MakeCommandLine(), handle.CommandLine, StringComparer.Ordinal);
            Assert.Equal(handle.CommandLine, result.CommandLine, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that awaiting the handle directly gives the same result as awaiting the task it
        /// wraps, since the two are interchangeable to a caller.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetAwaiter_GivesTheSameResultAsTheTaskAsync()
        {
            // Arrange
            ProcessHandle handle = LaunchAsync(new(CommandInterpreter, ["/c", "exit 7"], createNoWindow: true));

            // Act
            using ProcessResult awaited = await handle.ConfigureAwait(true);

            // Assert: the same object, not merely an equal one
            Assert.Same(await handle.Task.ConfigureAwait(true), awaited);
            Assert.Equal(7, awaited.ExitCode);
        }

        /// <summary>
        /// Verifies that the handle reports itself as finished once the process has been waited for, and
        /// as neither failed nor cancelled.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Status_ReportsCompletionOnceTheProcessHasBeenWaitedForAsync()
        {
            // Arrange
            ProcessHandle handle = LaunchAsync(new(CommandInterpreter, ["/c", "exit 0"], createNoWindow: true));

            // Act
            using ProcessResult result = await handle.Task.ConfigureAwait(true);

            // Assert
            Assert.True(handle.IsCompleted);
            Assert.False(handle.IsFaulted);
            Assert.False(handle.IsCanceled);
            Assert.Equal(TaskStatus.RanToCompletion, handle.Status);
        }

        /// <summary>
        /// Verifies that the result carries back the launch it came from, so a caller that kept only the
        /// result can still say what produced it.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Task_ResultCarriesBackTheLaunchAsync()
        {
            // Arrange
            ProcessLaunchInfo launchInfo = new(CommandInterpreter, ["/c", "exit 0"], createNoWindow: true);

            // Act
            using ProcessResult result = await LaunchAsync(launchInfo).Task.ConfigureAwait(true);

            // Assert
            Assert.Same(launchInfo, result.LaunchInfo);
            Assert.NotNull(result.Process);
        }

        /// <summary>
        /// Launches a process, failing the test rather than returning nothing if it could not be started.
        /// </summary>
        /// <param name="launchInfo">What to launch.</param>
        /// <returns>The handle to it.</returns>
        private static ProcessHandle LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            ProcessHandle? handle = ProcessManager.LaunchAsync(launchInfo);
            Assert.NotNull(handle);
            return handle;
        }

        /// <summary>
        /// The command interpreter, which every Windows installation has and which can be made to exit
        /// with a known code without anything being installed to do it.
        /// </summary>
        private static readonly string CommandInterpreter = Path.Join(Environment.SystemDirectory, "cmd.exe");
    }
}
