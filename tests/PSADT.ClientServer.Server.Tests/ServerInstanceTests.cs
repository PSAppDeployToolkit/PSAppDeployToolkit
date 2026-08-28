using System;
using System.Security.Principal;
using System.Threading.Tasks;
using PSADT.AccountManagement;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using PSADT.Foundation;
using PSADT.Utilities;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the server side of a client session.
    /// </summary>
    /// <remarks>
    /// Two kinds of test live here. Most need no client: an instance can be built, asked about itself and
    /// disposed without one ever being started, and everything about its lifecycle before and after that
    /// point is covered that way.
    /// <para>
    /// The rest start a real client and talk to it, which is the only thing that shows the wire protocol
    /// actually works end to end - the pieces are covered separately, but nothing else puts them together
    /// with a process on the other side. They are gated on the machine being able to run one at all, and
    /// they ask it only for things it can answer by reading: the commands that change the user's
    /// environment or take over their desktop are deliberately left alone, and are listed below.
    /// </para>
    /// <para>
    /// Not covered: every command that would change the machine or interrupt whoever is using it. Setting
    /// and removing environment variables writes to the user's registry; minimising and restoring windows,
    /// and sending keystrokes, take over their desktop; running a process on their behalf starts one; and
    /// every dialog puts something on their screen and waits. Each of those still has its payload and its
    /// disposal guard covered - what is missing is the round trip to a client, and there is no way to have
    /// that without doing the thing.
    /// </para>
    /// <para>
    /// Also not covered: the handler that disposes the instance when the process exits. It runs on the
    /// application domain shutting down, which cannot be brought about without ending the test run.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class ServerInstanceTests
    {
        /// <summary>
        /// Verifies that no user at all is refused, since an instance has nobody to run a client as without
        /// one.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ServerInstance_RefusesNoUserAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new ServerInstance(null!));
        }

        /// <summary>
        /// Verifies that the user it was built for is the user it reports.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ServerInstance_CarriesTheUserItWasBuiltFor()
        {
            // Arrange
            RunAsActiveUser user = SomeUser();

            // Act
            await using ServerInstance instance = new(user);

            // Assert
            Assert.Equal(user, instance.RunAsActiveUser);
        }

        /// <summary>
        /// Verifies that an instance with no client started reports that nothing is running, rather than
        /// failing on the client it does not have.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ServerInstance_IsNotRunningBeforeItIsOpened()
        {
            await using ServerInstance instance = new(SomeUser());
            Assert.False(instance.IsRunning);
        }

        /// <summary>
        /// Verifies that an instance with no log reader started reports no failure from it.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task GetLogWriterException_ReportsNothingBeforeItIsOpened()
        {
            await using ServerInstance instance = new(SomeUser());
            Assert.Null(instance.GetLogWriterException());
        }

        /// <summary>
        /// Verifies that the sentinel a client writes to mark success is the unit separator.
        /// </summary>
        /// <remarks>
        /// Part of the agreement between the two halves rather than an implementation detail, and a control
        /// character precisely so that it cannot occur in anything a client would legitimately write.
        /// </remarks>
        [Fact]
        public void SuccessSentinel_IsTheUnitSeparator()
        {
            Assert.Equal("\u001F", ServerInstance.SuccessSentinel);
        }

        /// <summary>
        /// Verifies that disposing an instance that never started a client succeeds, and that disposing it
        /// again does nothing.
        /// </summary>
        /// <remarks>
        /// Both matter. The first is the path taken when opening fails part way, which disposes the
        /// instance from inside its own opening; the second is what a caller's using block does afterwards.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task DisposeAsync_SucceedsOnAnInstanceThatNeverOpened()
        {
            // Arrange
            ServerInstance instance = new(SomeUser());

            // Act
            await instance.DisposeAsync().ConfigureAwait(true);

            // Assert
            Assert.Null(await Record.ExceptionAsync(async () => await instance.DisposeAsync().ConfigureAwait(true)).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that a disposed instance refuses everything rather than writing to pipes it has closed.
        /// </summary>
        /// <remarks>
        /// The five below are not a sample. The check appears in exactly six places - opening, updating a
        /// progress dialog, asking after the log reader, and each of the two methods every command is sent
        /// through - and one of the six is only reachable behind another. So a command with no payload and
        /// a command with one stand for every other command there is, because that is the whole of how they
        /// reach the pipe.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ServerInstance_RefusesEverythingAfterDisposal()
        {
            // Arrange
            ServerInstance instance = new(SomeUser());
            await instance.DisposeAsync().ConfigureAwait(true);

            // Assert
            _ = Assert.Throws<ObjectDisposedException>(instance.GetLogWriterException);
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await instance.OpenAsync().ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await instance.UpdateProgressDialogAsync("a message").ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await instance.ProgressDialogOpenAsync().ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await instance.ShowBalloonTipAsync(SampleOptions.BalloonTip()).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a progress update carrying a message of nothing is refused before anything is sent.
        /// </summary>
        /// <remarks>
        /// Asserted on an instance that never opened, which is what makes it meaningful: there is no client
        /// and no agreed key, so anything reaching the pipe would fail differently. Getting an argument
        /// exception back proves the check ran first.
        /// </remarks>
        /// <param name="text">The message to refuse.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateProgressDialogAsync_RefusesAMessageOfNothing(string text)
        {
            // Arrange
            await using ServerInstance instance = new(SomeUser());

            // Assert
            _ = await Assert.ThrowsAsync<ArgumentException>(async () => await instance.UpdateProgressDialogAsync(text).ConfigureAwait(true)).ConfigureAwait(true);
            _ = await Assert.ThrowsAsync<ArgumentException>(async () => await instance.UpdateProgressDialogAsync(progressDetailMessage: text).ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a progress update carrying nothing at all reaches the pipe, since every part of it
        /// is optional and a caller changing only the percentage sends the rest as nothing.
        /// </summary>
        /// <remarks>
        /// What is asserted is that it gets past the checks, not that it succeeds: with no client on the
        /// other end it cannot. Failing for want of an agreed key is the proof that nothing refused it
        /// earlier.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task UpdateProgressDialogAsync_AcceptsAskingForNothing()
        {
            // Arrange
            await using ServerInstance instance = new(SomeUser());

            // Act
            Exception? failure = await Record.ExceptionAsync(async () => await instance.UpdateProgressDialogAsync().ConfigureAwait(true)).ConfigureAwait(true);

            // Assert
            _ = Assert.IsType<ServerException>(failure);
        }

        /// <summary>
        /// Verifies that a session can be opened, asked questions and closed again.
        /// </summary>
        /// <remarks>
        /// The only test that puts the whole thing together: a real client process, the key exchange, a
        /// command in each of the two shapes, and a graceful shutdown that reports the client exited
        /// cleanly. Everything it asks for is answered by reading the machine - the state of a dialog that
        /// was never shown, an environment variable, the notification state - so the user's session is left
        /// as it was found.
        /// <para>
        /// Both command shapes are covered deliberately. One sends a bare command byte and one sends a
        /// serialized payload behind it, and they are separate methods on the server, so a session that
        /// only exercised one would leave the other's framing unproven.
        /// </para>
        /// <para>
        /// The environment variable read is of the <em>user's</em> environment rather than the process's,
        /// which is the whole reason the command exists - the server runs as the local system account and
        /// cannot see it. So the expectation is read from the same place the client reads it, and a machine
        /// variable would come back as nothing however plainly the server could see one. A variable nobody
        /// has set is asked for alongside, because a client that found nothing answers with the sentinel
        /// rather than with nothing at all, and that convention is worth pinning down.
        /// </para>
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = "Requires the client executables and a caller that is the logged-on user.", SkipUnless = nameof(TestEnvironment.CanLaunchClient), SkipType = typeof(TestEnvironment))]
        public async Task ServerInstance_TalksToARealClient()
        {
            // Arrange
            await using ServerInstance instance = new(AccountUtilities.CallerRunAsActiveUser);

            // Act
            await instance.OpenAsync().ConfigureAwait(true);

            // Assert: the client is up, and answers both shapes of command
            Assert.True(instance.IsRunning);
            Assert.False(await instance.ProgressDialogOpenAsync().ConfigureAwait(true));
            Assert.False(await instance.NotifyIconOpenAsync().ConfigureAwait(true));
            Assert.Equal(
                EnvironmentUtilities.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User) ?? ServerInstance.SuccessSentinel,
                await instance.GetEnvironmentVariableAsync("TEMP").ConfigureAwait(true));
            Assert.Equal(ServerInstance.SuccessSentinel, await instance.GetEnvironmentVariableAsync("PSADT_A_VARIABLE_NOBODY_HAS_SET").ConfigureAwait(true));
            Assert.Null(instance.GetLogWriterException());
        }

        /// <summary>
        /// Verifies that opening a session that already has a client is refused, rather than starting a
        /// second one and losing the first.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = "Requires the client executables and a caller that is the logged-on user.", SkipUnless = nameof(TestEnvironment.CanLaunchClient), SkipType = typeof(TestEnvironment))]
        public async Task OpenAsync_RefusesToOpenTwice()
        {
            // Arrange
            await using ServerInstance instance = new(AccountUtilities.CallerRunAsActiveUser);
            await instance.OpenAsync().ConfigureAwait(true);

            // Assert
            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await instance.OpenAsync().ConfigureAwait(true)).ConfigureAwait(true);
        }

        /// <summary>
        /// Builds a user to hand an instance that is never going to start a client.
        /// </summary>
        /// <remarks>
        /// Made up rather than taken from the machine, so that the tests using it read the same wherever
        /// they run. Nothing is done with it beyond being carried and compared.
        /// </remarks>
        /// <returns>The user.</returns>
        private static RunAsActiveUser SomeUser()
        {
            return new(new NTAccount(@"CONTOSO\jbloggs"), new SecurityIdentifier(WellKnownSidType.LocalSystemSid, domainSid: null), sessionId: 3, isLocalAdmin: true);
        }
    }
}
