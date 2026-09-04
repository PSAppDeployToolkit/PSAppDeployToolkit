using System;
using System.Collections;
using System.Threading.Tasks;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests
{
    /// <summary>
    /// Tests the state the dialog manager keeps about what is currently on screen.
    /// </summary>
    /// <remarks>
    /// The manager owns two things that outlive a single call - the open progress dialog and the tray
    /// icon - and every method that touches either begins by checking whether it exists. Those checks
    /// are what stops a second progress dialog being created over the first, and what turns "update the
    /// dialog nobody opened" into a clear error rather than a null reference somewhere further in.
    /// <para>
    /// Only the checks are covered. Everything that would actually put something on screen is left
    /// alone: a modal dialog blocks on a message loop until a user answers it, and the tray icon writes
    /// the application's identity into the registry and drops an icon file in the temporary directory,
    /// which these tests are not permitted to do.
    /// </para>
    /// <para>
    /// The manager starts itself the moment any member is named, which is why every test here goes
    /// through <see cref="DialogHost"/>. See that class for the ordering this depends on.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Asynchronous method name should end with 'Async'", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class DialogManagerTests
    {
        /// <summary>
        /// Verifies that no progress dialog is reported open when none has been shown.
        /// </summary>
        /// <remarks>
        /// This is also what starts the manager for the whole assembly, since it is the cheapest member
        /// that neither throws nor shows anything.
        /// </remarks>
        [Fact]
        public void ProgressDialogOpen_IsFalseWhenNothingIsShowing()
        {
            // Act & Assert
            Assert.False(DialogHost.Run(DialogManager.ProgressDialogOpen));
        }

        /// <summary>
        /// Verifies that no notify icon is reported open when none has been shown.
        /// </summary>
        [Fact]
        public void NotifyIconOpen_IsFalseWhenNothingIsShowing()
        {
            // Act & Assert
            Assert.False(DialogHost.Run(DialogManager.NotifyIconOpen));
        }

        /// <summary>
        /// Verifies that updating a progress dialog nobody opened is refused.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task UpdateProgressDialog_RefusesWhenNoneIsOpen()
        {
            // Act & Assert
            InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                static () => DialogManager.UpdateProgressDialogAsync("a message"));
            Assert.Contains("not open", thrown.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that closing a progress dialog nobody opened is refused.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task CloseProgressDialog_RefusesWhenNoneIsOpen()
        {
            // Act & Assert
            InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                static () => DialogManager.CloseProgressDialogAsync());
            Assert.Contains("not open", thrown.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that updating a notify icon nobody opened is refused.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task UpdateNotifyIcon_RefusesWhenNoneIsOpen()
        {
            // Act & Assert
            InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                static () => DialogManager.UpdateNotifyIconAsync("a tooltip"));
            Assert.Contains("not open", thrown.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that closing a notify icon nobody opened is refused.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task CloseNotifyIcon_RefusesWhenNoneIsOpen()
        {
            // Act & Assert
            InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                static () => DialogManager.CloseNotifyIconAsync());
            Assert.Contains("not open", thrown.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that showing a balloon tip with no tray icon behind it is refused.
        /// </summary>
        /// <remarks>
        /// A balloon tip is drawn by the tray icon, so without one there is nothing to draw it. The
        /// options are built and handed over so the refusal is shown to come from the missing icon
        /// rather than from anything wrong with the request.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ShowBalloonTip_RefusesWhenNoNotifyIconIsOpen()
        {
            // Arrange
            BalloonTipOptions options = new(new Hashtable
            {
                ["Title"] = "a title",
                ["Text"] = "the balloon text",
                ["Icon"] = BalloonTipIcon.Info,
            });

            // Act & Assert
            InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                () => DialogManager.ShowBalloonTipAsync(options));
            Assert.Contains("no notify icon is open", thrown.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the state checks run before the request is marshalled to the dialog thread.
        /// </summary>
        /// <remarks>
        /// Each of these methods returns a task, so a check made on the dialog thread rather than by the
        /// caller would surface as a faulted task instead of a thrown exception - and a caller that
        /// never awaited would not see it at all. Asserting that the call itself throws, without any
        /// awaiting, is what pins the difference.
        /// </remarks>
        [Fact]
        public void StateChecks_ThrowBeforeReturningATask()
        {
            // Act & Assert
            _ = Assert.Throws<InvalidOperationException>(Calling(static () => DialogManager.UpdateProgressDialogAsync("a message")));
            _ = Assert.Throws<InvalidOperationException>(Calling(static () => DialogManager.CloseProgressDialogAsync()));
            _ = Assert.Throws<InvalidOperationException>(Calling(static () => DialogManager.UpdateNotifyIconAsync("a tooltip")));
            _ = Assert.Throws<InvalidOperationException>(Calling(static () => DialogManager.CloseNotifyIconAsync()));
        }

        /// <summary>
        /// Verifies that a bad request is reported as a bad request, not as a missing dialog.
        /// </summary>
        /// <remarks>
        /// Both update methods validate what they were asked to set before checking whether the thing to
        /// set it on exists. A caller who gets both wrong is therefore told what is wrong with the
        /// request rather than discovering the second fault only after fixing the first. It also keeps
        /// these guards reachable at all: while the state check answered first, nothing that was not
        /// already on screen could ever reach them.
        /// </remarks>
        [Fact]
        public void ArgumentChecks_AreMadeBeforeTheStateIsLookedAt()
        {
            // Act & Assert
            _ = Assert.Throws<ArgumentException>(Calling(static () => DialogManager.UpdateProgressDialogAsync("   ")));
            _ = Assert.Throws<ArgumentException>(Calling(static () => DialogManager.UpdateProgressDialogAsync(progressDetailMessage: "   ")));
            _ = Assert.Throws<ArgumentOutOfRangeException>(Calling(static () => DialogManager.UpdateProgressDialogAsync(progressPercentage: 150.0)));
            _ = Assert.Throws<ArgumentException>(Calling(static () => DialogManager.UpdateNotifyIconAsync("   ")));
        }

        /// <summary>
        /// Verifies that a percentage inside the range gets past the argument checks to the state check.
        /// </summary>
        /// <remarks>
        /// The other half of the pair above. Without it, a guard that rejected everything would pass the
        /// rejection tests and nothing would notice.
        /// </remarks>
        /// <param name="percentage">The percentage to send.</param>
        [Theory]
        [InlineData(0.0)]
        [InlineData(50.0)]
        [InlineData(100.0)]
        public void ArgumentChecks_LetAValidRequestReachTheStateCheck(double percentage)
        {
            // Act & Assert
            _ = Assert.Throws<InvalidOperationException>(Calling(() => DialogManager.UpdateProgressDialogAsync(progressPercentage: percentage)));
        }

        /// <summary>
        /// Wraps a call that returns a task so that only a synchronous throw escapes it.
        /// </summary>
        /// <remarks>
        /// The point of the two tests above is that the exception arrives before a task does. Handing
        /// the call to xunit as something task-returning would assert the opposite - that awaiting it
        /// faults - so the task is dropped here and the call presented as an ordinary action.
        /// </remarks>
        /// <param name="call">The call to make.</param>
        /// <returns>An action that makes the call and discards whatever it returned.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0134:Observe result of async calls", Justification = "Dropping the task is the point. These tests assert that the exception arrives before the task does, so a task that is never observed is exactly the scenario under test.")]
        private static Action Calling(Func<Task> call)
        {
            return () => _ = call();
        }
    }
}
