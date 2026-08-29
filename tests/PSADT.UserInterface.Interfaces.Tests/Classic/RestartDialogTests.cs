using System;
using System.Collections;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the Classic dialog that asks a user to restart.
    /// </summary>
    /// <remarks>
    /// The one dialog whose primary button does something irreversible: it restarts the machine, with no
    /// further confirmation, from a handler wired up by the designer. Nothing here clicks it, and
    /// nothing here lets a countdown reach zero, because both routes end in the same call. What is
    /// covered is everything up to that point - how the dialog is assembled, and the branch of the
    /// countdown that disables the minimize button rather than the one that restarts.
    /// </remarks>
    public sealed class RestartDialogTests
    {
        /// <summary>
        /// Verifies that the title comes from the string table rather than the application title.
        /// </summary>
        /// <remarks>
        /// The base sets the title from the application name, and this dialog overwrites it. It is not
        /// announcing an application, it is announcing a restart, and the restart's own wording is what
        /// belongs in the caption.
        /// </remarks>
        [Fact]
        public void Constructor_TakesItsTitleFromTheStringTable()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["AppTitle"] = "Contoso Suite";
            SampleOptions.Nested(table, "Strings")["Title"] = "Restart required";

            // Act
            using RestartDialog dialog = Build(table);

            // Assert
            Assert.Equal("Restart required", dialog.Text);
        }

        /// <summary>
        /// Verifies that the wording a dialog always shows comes from the string table.
        /// </summary>
        /// <remarks>
        /// Everything a user reads here is localized, so the set is checked at once: a string wired to
        /// the wrong control shows as one label carrying another's text, which no single assertion would
        /// catch. The message is the per-deployment-type one, so it also confirms that the install
        /// wording is what an install dialog picks up.
        /// </remarks>
        [Fact]
        public void Constructor_TakesItsWordingFromTheStringTable()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            Hashtable strings = SampleOptions.Nested(table, "Strings");
            strings["ButtonRestartNow"] = "Restart now";
            strings["ButtonRestartLater"] = "Later";

            // Act
            using RestartDialog dialog = Build(table);

            // Assert
            Assert.Equal("a message (Install)", FormControls.Find<Label>(dialog, "labelMessage").Text);
            Assert.Equal("Restart now", FormControls.Find<Button>(dialog, "buttonRestartNow").Text);
            Assert.Equal("Later", FormControls.Find<Button>(dialog, "buttonMinimize").Text);
        }

        /// <summary>
        /// Verifies that the countdown's own wording is shown, and only when there is a countdown.
        /// </summary>
        /// <remarks>
        /// These two labels live inside the countdown panel, so they leave with it when no countdown was
        /// asked for. That is why the wording they carry cannot be checked alongside the rest: without a
        /// duration there is nothing to find. The restart line is assembled from two separate strings
        /// with a space between them, which is the part worth pinning - it is the only place in the
        /// dialog where the wording a user reads is not a single table entry.
        /// </remarks>
        [Fact]
        public void Constructor_ShowsTheCountdownWordingOnlyWithACountdown()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(10);
            Hashtable strings = SampleOptions.Nested(table, "Strings");
            strings["MessageTime"] = "The time is now";
            strings["MessageRestart"] = "please restart";
            strings["TimeRemaining"] = "Time remaining";

            // Act
            using RestartDialog dialog = Build(table);

            // Assert
            Assert.Equal("The time is now please restart", FormControls.Find<Label>(dialog, "labelRestartMessage").Text);
            Assert.Equal("Time remaining", FormControls.Find<Label>(dialog, "labelTimeRemaining").Text);
        }

        /// <summary>
        /// Verifies that no countdown means the countdown panel is taken out of the layout.
        /// </summary>
        [Fact]
        public void Constructor_RemovesTheCountdownWhenThereIsNone()
        {
            // Act
            using RestartDialog dialog = Build(SampleOptions.RestartDialog());

            // Assert
            Assert.False(FormControls.Holds(dialog, "flowLayoutPanelCountdown"));
        }

        /// <summary>
        /// Verifies that a countdown is shown and ticks once a second.
        /// </summary>
        /// <remarks>
        /// The interval is what makes the displayed time move; a countdown left at the designer's
        /// default would show its starting value and never change it.
        /// </remarks>
        [Fact]
        public void Constructor_ShowsACountdownThatTicksEachSecond()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(10);

            // Act
            using RestartDialog dialog = Build(table);

            // Assert
            Assert.True(FormControls.Holds(dialog, "flowLayoutPanelCountdown"));
            Assert.Equal(1000, NonPublic.Field<Timer>(dialog, "countdownTimer").Interval);
        }

        /// <summary>
        /// Verifies that no custom message means the custom message label is taken out of the layout.
        /// </summary>
        [Fact]
        public void Constructor_RemovesTheCustomMessageWhenThereIsNone()
        {
            // Act
            using RestartDialog dialog = Build(SampleOptions.RestartDialog());

            // Assert
            Assert.False(FormControls.Holds(dialog, "labelCustomMessage"));
        }

        /// <summary>
        /// Verifies that a custom message is shown with any markup removed.
        /// </summary>
        [Fact]
        public void Constructor_ShowsACustomMessageWithoutItsMarkup()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CustomMessageText"] = "Save your work [bold]first[/bold].";

            // Act
            using RestartDialog dialog = Build(table);

            // Assert
            Assert.Equal("Save your work first.", FormControls.Find<Label>(dialog, "labelCustomMessage").Text);
        }

        /// <summary>
        /// Verifies that allowing cancellation shows a Cancel button.
        /// </summary>
        [Fact]
        public void Constructor_ShowsCancelWhenCancellingIsAllowed()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["DialogAllowCancel"] = true;
            SampleOptions.Nested(table, "Strings")["ButtonCancel"] = "Not now";

            // Act
            using RestartDialog dialog = Build(table);

            // Assert
            Assert.Equal("Not now", FormControls.Find<Button>(dialog, "buttonCancel").Text);
        }

        /// <summary>
        /// Verifies that refusing cancellation removes the button and closes the gap it leaves.
        /// </summary>
        /// <remarks>
        /// The three buttons sit in fixed columns, so removing the middle one without moving its
        /// neighbour would leave the dialog with a hole where Cancel used to be and the Later button
        /// stranded in the centre. Moving it to the right-hand column is what keeps the row looking
        /// deliberate, which is why the removal and the move are asserted together.
        /// </remarks>
        [Fact]
        public void Constructor_ClosesTheGapWhenCancellingIsRefused()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["DialogAllowCancel"] = false;

            // Act
            using RestartDialog dialog = Build(table);

            // Assert
            Assert.False(FormControls.Holds(dialog, "buttonCancel"));
            TableLayoutPanel buttons = FormControls.Find<TableLayoutPanel>(dialog, "tableLayoutPanelButton");
            Assert.Equal(2, buttons.GetColumn(FormControls.Find<Button>(dialog, "buttonMinimize")));
        }

        /// <summary>
        /// Verifies that cancellation is refused unless it was asked for.
        /// </summary>
        /// <remarks>
        /// The safe default for a dialog whose purpose is to get a machine restarted.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesCancellationByDefault()
        {
            // Act
            using RestartDialog dialog = Build(SampleOptions.RestartDialog());

            // Assert
            Assert.False(FormControls.Holds(dialog, "buttonCancel"));
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
            _ = Assert.Throws<ArgumentException>(() => Build(table).Dispose());
        }

        /// <summary>
        /// Records that this dialog reports no result.
        /// </summary>
        /// <remarks>
        /// Deliberate, and confirmed as such. The dialog either restarts the machine or is put aside;
        /// neither outcome is an answer a caller reads, so the result it is constructed with is null and
        /// the manager hands that null back. Pinned so that the null is understood as a decision rather
        /// than mistaken for something that was never wired up.
        /// </remarks>
        [Fact]
        public void Constructor_ReportsNoResult()
        {
            // Act
            using RestartDialog dialog = Build(SampleOptions.RestartDialog());

            // Assert
            Assert.Null(dialog.DialogResult);
        }

        /// <summary>
        /// Verifies that the minimize button is taken away once the countdown gets close to zero.
        /// </summary>
        /// <remarks>
        /// The last chance to put the dialog aside has to expire before the restart does, or a user
        /// could minimize it and be restarted with the window out of sight. The stopwatch is wound
        /// forward rather than waited on, so the branch is reached in the moment it takes to call the
        /// tick handler.
        /// <para>
        /// Only this branch. The other one restarts the machine, so a test may not go near it, which is
        /// why the countdown here is left with time on it rather than run down to zero.
        /// </para>
        /// </remarks>
        [Fact]
        public void CountdownTick_TakesAwayMinimizeAsTheDeadlineApproaches()
        {
            // Arrange: a warning window longer than the countdown itself, so the dialog is inside it
            // from the first tick. Winding a stopwatch forward is not possible, and waiting for one to
            // reach a deadline would make the test as slow as the deadline; setting the deadline to
            // cover the whole countdown reaches the same branch immediately, and is a configuration a
            // deployment can legitimately ask for - a restart that may never be put aside.
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(10);
            table["CountdownNoMinimizeDuration"] = TimeSpan.FromMinutes(20);
            using RestartDialog dialog = Build(table);

            // Act
            DialogHost.Run(() => NonPublic.Call(dialog, "CountdownTimer_Tick", null, EventArgs.Empty));

            // Assert
            Assert.False(FormControls.Find<Button>(dialog, "buttonMinimize").Enabled);
        }

        /// <summary>
        /// Verifies that the minimize button stays available while there is still time.
        /// </summary>
        [Fact]
        public void CountdownTick_LeavesMinimizeAloneWhileThereIsTime()
        {
            // Arrange: a ten minute countdown that only stops being dismissable in its last minute.
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(10);
            table["CountdownNoMinimizeDuration"] = TimeSpan.FromMinutes(1);
            using RestartDialog dialog = Build(table);

            // Act
            DialogHost.Run(() => NonPublic.Call(dialog, "CountdownTimer_Tick", null, EventArgs.Empty));

            // Assert
            Assert.True(FormControls.Find<Button>(dialog, "buttonMinimize").Enabled);
        }

        /// <summary>
        /// Verifies that the countdown label shows the time left.
        /// </summary>
        [Fact]
        public void CountdownTick_ShowsTheTimeRemaining()
        {
            // Arrange
            Hashtable table = SampleOptions.RestartDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(10);
            using RestartDialog dialog = Build(table);

            // Act
            DialogHost.Run(() => NonPublic.Call(dialog, "CountdownTimer_Tick", null, EventArgs.Empty));

            // Assert
            Assert.Equal("0:10:00", FormControls.Find<Label>(dialog, "labelCountdown").Text);
        }

        /// <summary>
        /// Builds a restart dialog on the shared apartment from the given options.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <returns>The dialog, which the caller owns.</returns>
        private static RestartDialog Build(Hashtable table)
        {
            return DialogHost.Run(() => new RestartDialog(new RestartDialogOptions(DeploymentType.Install, table)));
        }
    }
}
