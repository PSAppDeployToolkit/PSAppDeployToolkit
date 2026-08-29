using System;
using System.Reflection;
using System.Threading.Tasks;
using PSADT.ClientServer.Client.Tests.TestHelpers;
using PSAppDeployToolkit.Logging;
using Xunit;

namespace PSADT.ClientServer.Client.Tests
{
    /// <summary>
    /// Tests for <c>ClientExecutable.CloseAppsDialogStateManager</c>, which owns the close applications
    /// dialog state across one client/server session.
    /// </summary>
    /// <remarks>
    /// The type is a private nested class, so it is created and driven entirely through reflection. The
    /// state it holds is also internal to another assembly this one has no friend access to, so a test
    /// asserts on whether a state is there and what it holds rather than on its declared type.
    /// <para>
    /// Every reset here is given no process definitions. With them, the state starts a running process
    /// service that watches real processes, which is more than a test of this type needs and more than
    /// its rule against touching machine state allows.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class CloseAppsDialogStateManagerTests
    {
        /// <summary>
        /// The nested type under test.
        /// </summary>
        private static readonly Type Subject = NonPublic.Nested(typeof(ClientExecutable), "CloseAppsDialogStateManager");

        /// <summary>
        /// A log action that discards what it is given, because these tests never make one log.
        /// </summary>
        private static readonly Func<string, LogSeverity, string, ValueTask> DiscardLog = static (_, _, _) => default;

        /// <summary>
        /// Confirms a fresh manager holds no state.
        /// </summary>
        [Fact]
        public void State_IsAbsentUntilSomethingResetsIt()
        {
            Assert.Null(StateOf(NonPublic.Create(Subject)));
        }

        /// <summary>
        /// Confirms a reset leaves a state behind.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ResetAsync_MakesAStateAvailable()
        {
            object manager = NonPublic.Create(Subject);
            await NonPublic.CallAsync(manager, "ResetAsync", null, DiscardLog);
            Assert.NotNull(StateOf(manager));
        }

        /// <summary>
        /// Confirms a second reset installs a different state rather than reusing the first.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ResetAsync_ReplacesAStateThatWasAlreadyThere()
        {
            object manager = NonPublic.Create(Subject);
            await NonPublic.CallAsync(manager, "ResetAsync", null, DiscardLog);
            object? first = StateOf(manager);
            await NonPublic.CallAsync(manager, "ResetAsync", null, DiscardLog);
            object? second = StateOf(manager);
            Assert.NotNull(second);
            Assert.NotSame(first, second);
        }

        /// <summary>
        /// Confirms the state a reset displaces is disposed, so a session that reopens the dialog does not leak the previous one.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ResetAsync_DisposesTheStateItReplaced()
        {
            object manager = NonPublic.Create(Subject);
            await NonPublic.CallAsync(manager, "ResetAsync", null, DiscardLog);
            object replaced = StateOf(manager)!;
            await NonPublic.CallAsync(manager, "ResetAsync", null, DiscardLog);
            Assert.True(WasDisposed(replaced));
        }

        /// <summary>
        /// Confirms disposal leaves no state behind.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task DisposeAsync_ClearsTheState()
        {
            object manager = NonPublic.Create(Subject);
            await NonPublic.CallAsync(manager, "ResetAsync", null, DiscardLog);
            await NonPublic.CallAsync(manager, "DisposeAsync");
            Assert.Null(StateOf(manager));
        }

        /// <summary>
        /// Confirms disposing a manager that never held a state is not an error.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task DisposeAsync_IsHarmlessWhenThereIsNoState()
        {
            object manager = NonPublic.Create(Subject);
            await NonPublic.CallAsync(manager, "DisposeAsync");
            Assert.Null(StateOf(manager));
        }

        /// <summary>
        /// Confirms no running process service is started when no process definitions are given.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ResetAsync_WatchesNoProcessesWhenGivenNoDefinitions()
        {
            object manager = NonPublic.Create(Subject);
            await NonPublic.CallAsync(manager, "ResetAsync", null, DiscardLog);
            Assert.Null(FieldOf(StateOf(manager)!, "RunningProcessService"));
        }

        /// <summary>
        /// Reads the state a manager is holding.
        /// </summary>
        /// <param name="manager">The manager to read.</param>
        /// <returns>The state, or <see langword="null"/> if it holds none.</returns>
        private static object? StateOf(object manager)
        {
            return NonPublic.Property<object>(manager, "State");
        }

        /// <summary>
        /// Reads whether a state has been disposed.
        /// </summary>
        /// <param name="state">The state to read.</param>
        /// <returns><see langword="true"/> if it has; otherwise, <see langword="false"/>.</returns>
        private static bool WasDisposed(object state)
        {
            return FieldOf(state, "_disposed") is true;
        }

        /// <summary>
        /// Reads a field from a type this assembly has no friend access to.
        /// </summary>
        /// <param name="instance">The object to read from.</param>
        /// <param name="name">The field's name.</param>
        /// <returns>The field's value.</returns>
        /// <exception cref="MissingFieldException">Thrown if the type declares no such field.</exception>
        private static object? FieldOf(object instance, string name)
        {
            for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
            {
                if (type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo field)
                {
                    return field.GetValue(instance);
                }
            }
            throw new MissingFieldException(instance.GetType().FullName, name);
        }
    }
}
