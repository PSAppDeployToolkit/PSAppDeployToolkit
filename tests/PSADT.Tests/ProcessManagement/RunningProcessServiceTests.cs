using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the service that watches a set of process definitions and reports when what matches changes.
    /// </summary>
    /// <remarks>
    /// The subject is always the test host, which is running throughout, so no process is started or
    /// stopped to drive the tests. The service polls once a second; the test that needs a poll to happen
    /// waits on the service's own event rather than sleeping for a fixed period, and fails rather than
    /// hanging if nothing arrives.
    /// </remarks>
    public sealed class RunningProcessServiceTests
    {
        /// <summary>
        /// Verifies that the service reports the processes matching its definitions before it is started,
        /// so a caller that only wants one answer need not start it at all.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task RunningProcesses_AnswerBeforeTheServiceIsStartedAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            RunningProcessService service = NewService(current.ProcessName);

            // Act & Assert
            try
            {
                Assert.False(service.IsRunning);
                Assert.Contains(service.RunningProcesses, info => info.Process.Id == current.Id);
                Assert.Contains(service.ProcessesToClose, info => info.Name.Equals(current.ProcessName, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                await service.DisposeAsync().ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Verifies that processes sharing an image are reported once between them, since a prompt asking
        /// a person to close an application should name it once however many copies are running.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ProcessesToClose_ReportsOneEntryPerImageAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            RunningProcessService service = NewService(current.ProcessName);

            // Act & Assert
            try
            {
                IReadOnlyList<ProcessToClose> toClose = service.ProcessesToClose;
                Assert.Equal(toClose.Count, toClose.Select(static info => info.Path.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            }
            finally
            {
                await service.DisposeAsync().ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Verifies that starting the service makes it report itself as running, that stopping it settles
        /// it again, and that it can then be started a second time.
        /// </summary>
        /// <remarks>
        /// Restarting is worth asserting because stopping cancels the token the polling task runs under,
        /// and a cancelled token cannot be reused - so a restart only works if a fresh one is issued.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Start_AndStopAsync_MoveTheServiceBetweenStatesAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            RunningProcessService service = NewService(current.ProcessName);

            // Act & Assert
            try
            {
                service.Start();
                Assert.True(service.IsRunning);
                await service.StopAsync().ConfigureAwait(true);
                Assert.False(service.IsRunning);
                service.Start();
                Assert.True(service.IsRunning);
            }
            finally
            {
                await service.DisposeAsync().ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Verifies that starting a service that is already running is refused, rather than leaving a
        /// second polling task running that nothing holds a reference to.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Start_RefusesToStartTwiceAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            RunningProcessService service = NewService(current.ProcessName);

            // Act & Assert
            try
            {
                service.Start();
                _ = Assert.Throws<InvalidOperationException>(service.Start);
            }
            finally
            {
                await service.DisposeAsync().ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Verifies that stopping a service that was never started is refused, since a caller doing so has
        /// lost track of what it started.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task StopAsync_RefusesAServiceThatIsNotRunningAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            RunningProcessService service = NewService(current.ProcessName);

            // Act & Assert
            try
            {
                _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.StopAsync().ConfigureAwait(true)).ConfigureAwait(true);
            }
            finally
            {
                await service.DisposeAsync().ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Verifies that the service raises its change event while polling, which is how a prompt showing
        /// the processes to close learns that the list has moved on.
        /// </summary>
        /// <remarks>
        /// The event fires when the set of processes to close differs from the previous poll. Nothing has
        /// been reported before the service starts, so the first poll after starting is the one that
        /// reports a change - which is what makes this observable without starting a process to be found.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ProcessesToCloseChanged_IsRaisedWhilePollingAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            TaskCompletionSource<IReadOnlyList<ProcessToClose>> raised = new();
            RunningProcessService service = NewService(current.ProcessName);
            service.ProcessesToCloseChanged += (_, e) => raised.TrySetResult(e.ProcessesToClose);

            // Act
            IReadOnlyList<ProcessToClose>? reported = null;
            try
            {
                service.Start();
                Task finished = await Task.WhenAny(raised.Task, Task.Delay(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken)).ConfigureAwait(true);
                Assert.True(ReferenceEquals(finished, raised.Task), "The service did not report a change within thirty seconds.");
                reported = await raised.Task.ConfigureAwait(true);
            }
            finally
            {
                await service.DisposeAsync().ConfigureAwait(true);
            }

            // Assert
            Assert.NotNull(reported);
            Assert.Contains(reported, info => info.Name.Equals(current.ProcessName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verifies that a disposed service refuses to be read or restarted, rather than answering from a
        /// snapshot that is no longer being kept up to date.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task DisposeAsync_LeavesTheServiceUnusableAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            RunningProcessService service = NewService(current.ProcessName);
            service.Start();

            // Act
            await service.DisposeAsync().ConfigureAwait(true);

            // Assert: settled, unreadable, unstartable, and safe to dispose again
            Assert.False(service.IsRunning);
            _ = Assert.Throws<ObjectDisposedException>(() => service.RunningProcesses);
            _ = Assert.Throws<ObjectDisposedException>(() => service.ProcessesToClose);
            _ = Assert.Throws<ObjectDisposedException>(service.Start);
            await service.DisposeAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a service built with no definitions is refused, since it would poll forever
        /// looking for nothing.
        /// </summary>
        [Fact]
        public void RunningProcessService_RefusesAnEmptyDefinitionList()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => new RunningProcessService(new ReadOnlyCollection<ProcessDefinition>([])));
        }

        /// <summary>
        /// Verifies that a null definition list is refused as null, rather than failing on the attempt to
        /// count it and leaving the caller to work out which argument was at fault.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void RunningProcessService_RefusesANullDefinitionList()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new RunningProcessService(null!));
        }

        /// <summary>
        /// Builds a service watching for a single named process.
        /// </summary>
        /// <param name="processName">The process to watch for.</param>
        /// <returns>The service.</returns>
        private static RunningProcessService NewService(string processName)
        {
            return new(new ReadOnlyCollection<ProcessDefinition>([new(processName)]));
        }
    }
}
