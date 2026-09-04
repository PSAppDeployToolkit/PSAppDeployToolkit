using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the Fluent dialog that asks a user to restart.
    /// </summary>
    /// <remarks>
    /// The one dialog whose primary button does something irreversible: it restarts the machine, with no
    /// further confirmation. Nothing here clicks it.
    /// <para>
    /// The countdown needs the same care. Its tick handler restarts the machine the moment the elapsed
    /// time reaches the duration, so every countdown in this file is set to a duration far longer than a
    /// test could take and the stopwatch behind it is never started - the dialog only starts it when the
    /// window loads, which these tests never reach. Where a test needs the branch that runs near the
    /// end of a countdown, it gets there by setting the warning threshold longer than the countdown
    /// itself rather than by letting any time pass.
    /// </para>
    /// </remarks>
    public sealed class RestartDialogTests
    {
        /// <summary>
        /// Verifies that the title comes from the string table rather than the application title.
        /// </summary>
        [Fact]
        public void Constructor_TakesItsTitleFromTheStringTable()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["AppTitle"] = "Contoso Suite";
            SampleOptions.Nested(table, "Strings")["Title"] = "Restart required";

            // Act & Assert
            WithDialog(table, static dialog => Assert.Equal("Restart required", dialog.Title));
        }

        /// <summary>
        /// Verifies that a dialog with no countdown shows the plain message.
        /// </summary>
        /// <remarks>
        /// The two messages say different things: one explains that a restart is needed, the other that
        /// one is about to happen. Showing the second without a countdown beside it would be a threat
        /// with no timer attached.
        /// </remarks>
        [Fact]
        public void Constructor_ShowsThePlainMessageWithoutACountdown()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            Hashtable strings = SampleOptions.Nested(table, "Strings");
            strings["Message"] = SampleOptions.PerDeploymentType("a restart is needed");
            strings["MessageRestart"] = "restarting shortly";

            // Act & Assert
            WithDialog(table, static dialog => Assert.Equal("a restart is needed (Install)", Text(dialog.MessageTextBlock)));
        }

        /// <summary>
        /// Verifies that a dialog with a countdown shows the impending-restart message instead.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheRestartingMessageWithACountdown()
        {
            // Arrange
            Hashtable table = WithCountdown();
            Hashtable strings = SampleOptions.Nested(table, "Strings");
            strings["Message"] = SampleOptions.PerDeploymentType("a restart is needed");
            strings["MessageRestart"] = "restarting shortly";
            strings["TimeRemaining"] = "Time remaining";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal("restarting shortly", Text(dialog.MessageTextBlock));
                Assert.Equal("Time remaining", dialog.CountdownHeadingTextBlock.Text);
                Assert.Equal(Visibility.Visible, dialog.CountdownStackPanel.Visibility);
            });
        }

        /// <summary>
        /// Verifies that the restart button is shown, captioned and made the default.
        /// </summary>
        [Fact]
        public void Constructor_MakesRestartingNowThePrimaryAction()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            SampleOptions.Nested(table, "Strings")["ButtonRestartNow"] = "Restart now";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal(Visibility.Visible, dialog.ButtonLeft.Visibility);
                Assert.Equal("Restart now", FluentControls.Caption(dialog.ButtonLeft));
                Assert.True(dialog.ButtonLeft.IsDefault);
                Assert.Equal(Fluence.Wpf.ControlAppearance.Accent, dialog.ButtonLeft.Appearance);
            });
        }

        /// <summary>
        /// Verifies that refusing cancellation puts the deferring action on the right and offers no
        /// third button.
        /// </summary>
        /// <remarks>
        /// This is the default arrangement, and the safe one for a dialog whose purpose is to get a
        /// machine restarted: the user can put it aside but not dismiss it.
        /// </remarks>
        [Fact]
        public void Constructor_OffersOnlyRestartingAndDeferringByDefault()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            SampleOptions.Nested(table, "Strings")["ButtonRestartLater"] = "Later";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal(Visibility.Collapsed, dialog.ButtonMiddle.Visibility);
                Assert.Equal(Visibility.Visible, dialog.ButtonRight.Visibility);
                Assert.Equal("Later", FluentControls.Caption(dialog.ButtonRight));
                Assert.True(dialog.ButtonRight.IsCancel);
            });
        }

        /// <summary>
        /// Verifies that allowing cancellation adds a third button and moves the deferring one along.
        /// </summary>
        /// <remarks>
        /// Cancelling and deferring are different outcomes - one closes the dialog for good, the other
        /// puts it out of the way - so with both on offer they need a button each, and the deferring one
        /// moves to the middle to make room.
        /// </remarks>
        [Fact]
        public void Constructor_AddsACancelButtonWhenCancellingIsAllowed()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["DialogAllowCancel"] = true;
            Hashtable strings = SampleOptions.Nested(table, "Strings");
            strings["ButtonRestartLater"] = "Later";
            strings["ButtonCancel"] = "Not now";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal(Visibility.Visible, dialog.ButtonMiddle.Visibility);
                Assert.Equal("Later", FluentControls.Caption(dialog.ButtonMiddle));
                Assert.Equal(Visibility.Visible, dialog.ButtonRight.Visibility);
                Assert.Equal("Not now", FluentControls.Caption(dialog.ButtonRight));
                Assert.True(dialog.ButtonRight.IsCancel);
            });
        }

        /// <summary>
        /// Verifies that the dialog offers a minimize control either way.
        /// </summary>
        /// <remarks>
        /// Putting the dialog aside is always on offer, whether the deferring action sits in the middle
        /// or on the right.
        /// </remarks>
        /// <param name="allowCancel">Whether cancelling was allowed.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_OffersAMinimizeControl(bool allowCancel)
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["DialogAllowCancel"] = allowCancel;

            // Act & Assert
            WithDialog(table, static dialog => Assert.Equal(Visibility.Visible, dialog.IsMinimizeButtonVisible));
        }

        /// <summary>
        /// Verifies that deferring puts the dialog out of the way rather than closing it.
        /// </summary>
        /// <remarks>
        /// Which button defers depends on whether cancelling is allowed, so both arrangements are
        /// checked. Neither should release the dialog to close: the restart is still required, and the
        /// persistence timer will bring the window back.
        /// </remarks>
        /// <param name="allowCancel">Whether cancelling was allowed.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DeferringClick_MinimizesRatherThanClosing(bool allowCancel)
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["DialogAllowCancel"] = allowCancel;

            // Act & Assert
            WithDialog(table, dialog =>
            {
                FluentControls.Click(allowCancel ? dialog.ButtonMiddle : dialog.ButtonRight);
                Assert.Equal(WindowState.Minimized, dialog.WindowState);
                Assert.False(FluentControls.WouldAllowClosing(dialog));
            });
        }

        /// <summary>
        /// Verifies that cancelling releases the dialog to close.
        /// </summary>
        /// <remarks>
        /// The one way out of this dialog that does not restart the machine, and it exists only when the
        /// deployment allowed it.
        /// </remarks>
        [Fact]
        public void CancelClick_ReleasesTheDialogToClose()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["DialogAllowCancel"] = true;

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                FluentControls.Click(dialog.ButtonRight);
                Assert.True(FluentControls.WouldAllowClosing(dialog));
            });
        }

        /// <summary>
        /// Records that this dialog reports no result.
        /// </summary>
        /// <remarks>
        /// Deliberate, and confirmed as such. The dialog either restarts the machine or is put aside;
        /// neither outcome is an answer a caller reads, so the result it is constructed with is null and
        /// the manager hands that null back. Pinned so the null is understood as a decision rather than
        /// mistaken for something that was never wired up.
        /// </remarks>
        [Fact]
        public void Constructor_ReportsNoResult()
        {
            // Act & Assert
            WithDialog(SampleOptions.RestartDialog(), static dialog => Assert.Null(dialog.DialogResult));
        }

        /// <summary>
        /// Verifies that a custom message is shown with its markup rendered.
        /// </summary>
        [Fact]
        public void Constructor_ShowsACustomMessage()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CustomMessageText"] = "Save your work [bold]first[/bold].";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal(Visibility.Visible, dialog.CustomMessageTextBlock.Visibility);
                Assert.Equal("Save your work first.", Text(dialog.CustomMessageTextBlock));
            });
        }

        /// <summary>
        /// Verifies that a custom message present but blank is refused.
        /// </summary>
        [Fact]
        public void Constructor_RefusesABlankCustomMessage()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CustomMessageText"] = "   ";

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => WithDialog(table, static _ => { }));
        }

        /// <summary>
        /// Verifies that the countdown shows the time remaining.
        /// </summary>
        /// <remarks>
        /// The stopwatch has not been started, so the whole duration is still to run - which is also
        /// what the dialog shows on its first frame, before the timer has ticked once.
        /// </remarks>
        [Fact]
        public void CountdownTick_ShowsTheTimeRemaining()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromSeconds(3725);

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.Equal("1h 2m 5s", dialog.CountdownValueTextBlock.Text);
            });
        }

        /// <summary>
        /// Verifies that the way out is taken away once the countdown gets close to zero.
        /// </summary>
        /// <remarks>
        /// The last chance to put the dialog aside has to expire before the restart does, or a user
        /// could minimize it and be restarted with the window out of sight. Reached by setting the
        /// warning threshold longer than the countdown itself, so the dialog is inside it from the first
        /// tick - a configuration a deployment can legitimately ask for, meaning a restart that may
        /// never be put aside, and one that reaches the branch without any time passing.
        /// </remarks>
        /// <param name="allowCancel">Whether cancelling was allowed, which decides which button defers.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CountdownTick_TakesAwayTheWayOutAsTheDeadlineApproaches(bool allowCancel)
        {
            // Arrange
            Hashtable table = WithCountdown();
            table["DialogAllowCancel"] = allowCancel;
            table["CountdownNoMinimizeDuration"] = TimeSpan.FromMinutes(120);

            // Act & Assert
            WithDialog(table, dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.Equal(Visibility.Collapsed, dialog.IsMinimizeButtonVisible);
                Assert.False((allowCancel ? dialog.ButtonMiddle : dialog.ButtonRight).IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that the way out stays available while there is still time.
        /// </summary>
        [Fact]
        public void CountdownTick_LeavesTheWayOutAloneWhileThereIsTime()
        {
            // Arrange
            Hashtable table = WithCountdown();
            table["CountdownNoMinimizeDuration"] = TimeSpan.FromMinutes(1);

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.Equal(Visibility.Visible, dialog.IsMinimizeButtonVisible);
                Assert.True(dialog.ButtonRight.IsEnabled);
            });
        }

        /// <summary>
        /// The sample options with a countdown far longer than any test could take.
        /// </summary>
        /// <remarks>
        /// The length matters. A countdown that could elapse during a test would take the branch that
        /// restarts the machine.
        /// </remarks>
        /// <returns>A new dictionary each call.</returns>
        private static Hashtable WithCountdown()
        {
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(60);
            return table;
        }

        /// <summary>
        /// Reads the text a block is showing, with any markup already rendered.
        /// </summary>
        /// <param name="block">The block to read.</param>
        /// <returns>The visible text.</returns>
        private static string Text(System.Windows.Controls.TextBlock block)
        {
            return string.Concat(block.Inlines.OfType<Run>().Select(static r => r.Text));
        }

        /// <summary>
        /// Builds a restart dialog, runs a body against it and disposes it, all within the apartment.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="body">The assertions to run.</param>
        private static void WithDialog(Hashtable table, Action<RestartDialog> body)
        {
            DialogHost.WithDialog(() => new RestartDialog(new RestartDialogOptions(DeploymentType.Install, table)), body);
        }
    }
}
