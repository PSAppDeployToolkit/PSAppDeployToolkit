using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogState;
using PSAppDeployToolkit.Logging;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogState
{
    /// <summary>
    /// Tests the state a close-applications dialog keeps while it is open.
    /// </summary>
    /// <remarks>
    /// The one type in this project that is neither a value nor a static helper: it owns a process
    /// monitoring service and a stopwatch, and has to be disposed. Nothing here starts the monitoring -
    /// the service only polls once <c language="csharp">Start</c> is called, which these tests never do - so no process on
    /// the machine is looked at or touched.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class CloseAppsDialogStateTests
    {
        /// <summary>
        /// Verifies that no monitoring service is created when there is nothing to monitor.
        /// </summary>
        /// <remarks>
        /// Both spellings of "nothing" are covered because they arrive from different places: null is what
        /// a dialog shown with no process list produces, and an empty list is what a payload carrying an
        /// empty collection deserializes to. The service refuses an empty collection outright, so passing
        /// one through would turn an ordinary dialog into an exception.
        /// </remarks>
        /// <param name="empty">Whether to pass an empty list rather than null.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Constructor_CreatesNoServiceWhenThereIsNothingToMonitor(bool empty)
        {
            // Act
            await using CloseAppsDialogState state = new(empty ? [] : null, NoOpLogAsync);

            // Assert
            Assert.Null(state.RunningProcessService);
        }

        /// <summary>
        /// Verifies that a monitoring service is created when there are processes to watch.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_CreatesAServiceWhenThereAreProcessesToMonitor()
        {
            // Act
            await using CloseAppsDialogState state = new([new ProcessDefinition("notepad")], NoOpLogAsync);

            // Assert
            Assert.NotNull(state.RunningProcessService);
            Assert.False(state.RunningProcessService.IsRunning);
        }

        /// <summary>
        /// Verifies that the process definitions are copied rather than referenced.
        /// </summary>
        /// <remarks>
        /// The source says the definitions are copied into the collection the service holds them in. That
        /// matters because the caller's list is an <see cref="IReadOnlyList{T}"/>, which promises only
        /// that the caller will not change it through that reference - a <see cref="List{T}"/> behind it
        /// is still mutable, and a dialog whose watch list changed underneath it would start reporting
        /// processes nobody asked about.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_CopiesTheProcessDefinitions()
        {
            // Arrange
            List<ProcessDefinition> source = [new ProcessDefinition("notepad")];
            await using CloseAppsDialogState state = new(source, NoOpLogAsync);

            // Act
            source.Add(new ProcessDefinition("calc"));
            source.Clear();

            // Assert
            Assert.Equal([new ProcessDefinition("notepad")], HeldDefinitions(state));
        }

        /// <summary>
        /// Verifies that a state built without a way to log is refused.
        /// </summary>
        /// <remarks>
        /// The delegate is captured into a closure rather than called during construction, so without a
        /// guard a null one surfaces much later as a null reference from inside the first log call the
        /// dialog makes - by which point the dialog is already on screen.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RefusesANullLogAction()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new CloseAppsDialogState(closeProcesses: null, logAction: null!));
        }

        /// <summary>
        /// Verifies that the log action is given the dialog's own source name.
        /// </summary>
        /// <remarks>
        /// The state narrows a three-argument log delegate to two by supplying the source itself, which is
        /// what makes every line the dialog writes attributable to the command the user ran rather than to
        /// the dialog class. Nothing else states that name.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task LogAction_SuppliesTheDialogsOwnSourceName()
        {
            // Arrange
            string? capturedMessage = null;
            LogSeverity? capturedSeverity = null;
            string? capturedSource = null;
            await using CloseAppsDialogState state = new(closeProcesses: null, (message, severity, source) =>
            {
                capturedMessage = message;
                capturedSeverity = severity;
                capturedSource = source;
                return default;
            });

            // Act
            await state.LogAction("a message", LogSeverity.Warning);

            // Assert
            Assert.Equal("a message", capturedMessage);
            Assert.Equal(LogSeverity.Warning, capturedSeverity);
            Assert.Equal("Show-ADTInstallationWelcome", capturedSource);
        }

        /// <summary>
        /// Verifies that the countdown stopwatch has not started counting.
        /// </summary>
        /// <remarks>
        /// The dialog starts it when a countdown begins. One that arrived already running would show a
        /// countdown already partly elapsed, which for a forced countdown means closing a user's
        /// applications sooner than they were told.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task CountdownStopwatch_HasNotStarted()
        {
            // Act
            await using CloseAppsDialogState state = new(closeProcesses: null, NoOpLogAsync);

            // Assert
            Assert.False(state.CountdownStopwatch.IsRunning);
            Assert.Equal(TimeSpan.Zero, state.CountdownStopwatch.Elapsed);
        }

        /// <summary>
        /// Verifies that disposing twice is harmless, whether or not there is a service to dispose.
        /// </summary>
        /// <remarks>
        /// Both paths through <c language="csharp">DisposeAsync</c> set the flag - the early return when there is no service
        /// and the one that disposes it - and a dialog closed while its own cleanup is already running can
        /// reach either twice.
        /// </remarks>
        /// <param name="withProcesses">Whether to give the state something to monitor.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposeAsync_IsIdempotent(bool withProcesses)
        {
            // Arrange
            CloseAppsDialogState state = new(withProcesses ? [new ProcessDefinition("notepad")] : null, NoOpLogAsync);

            // Act
            await state.DisposeAsync();
            bool afterFirst = state._disposed;
            await state.DisposeAsync();

            // Assert
            Assert.True(afterFirst);
            Assert.True(state._disposed);
        }

        /// <summary>
        /// Verifies that a freshly built state does not consider itself disposed.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Disposed_IsFalseUntilDisposed()
        {
            // Act
            await using CloseAppsDialogState state = new(closeProcesses: null, NoOpLogAsync);

            // Assert
            Assert.False(state._disposed);
        }

        /// <summary>
        /// A log action that records nothing, for the tests that are not about logging.
        /// </summary>
        /// <param name="message">Ignored.</param>
        /// <param name="severity">Ignored.</param>
        /// <param name="source">Ignored.</param>
        /// <returns>A completed task.</returns>
        private static ValueTask NoOpLogAsync(string message, LogSeverity severity, string source)
        {
            return default;
        }

        /// <summary>
        /// Reads the definitions the monitoring service was handed.
        /// </summary>
        /// <remarks>
        /// Reflection, because the service keeps them private and offers no way to ask - the only public
        /// route to them is starting the polling loop, which would go and look at the machine's processes.
        /// </remarks>
        /// <param name="state">The state whose service to read.</param>
        /// <returns>The definitions it holds.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no service or the field has moved.</exception>
        private static IReadOnlyList<ProcessDefinition> HeldDefinitions(CloseAppsDialogState state)
        {
            object service = state.RunningProcessService ?? throw new InvalidOperationException("The state holds no monitoring service.");
            FieldInfo field = service.GetType().GetField("_processDefinitions", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("RunningProcessService no longer has a '_processDefinitions' field.");
            return field.GetValue(service) as ReadOnlyCollection<ProcessDefinition> ?? throw new InvalidOperationException("The definitions field did not hold a read-only collection.");
        }
    }
}
