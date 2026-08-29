using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Logging;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the Classic dialog that asks a user to close applications before a deployment starts.
    /// </summary>
    /// <remarks>
    /// The most conditional dialog in the project: what it shows depends on whether anything is
    /// running, whether deferrals are left, whether a deadline has passed and whether a countdown is
    /// forced, and several of those interact. The tests pick those apart one at a time.
    /// <para>
    /// The process list is real rather than stubbed - the monitoring service enumerates what is actually
    /// running, and there is no way in to substitute that. So the two states are reached by naming
    /// processes whose presence is known either way: the test host itself, which is certainly running,
    /// and a name nothing could be running under. Nothing is ever started or stopped.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Asynchronous method name should end with 'Async'", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class CloseAppsDialogTests
    {
        /// <summary>
        /// Verifies that an ampersand in the application name is shown rather than swallowed.
        /// </summary>
        /// <remarks>
        /// Windows Forms reads a single ampersand in a label as the marker for an access key and hides
        /// it, underlining the letter after it instead. So "AT&amp;T Connect" would show as "ATT Connect"
        /// with a T underlined. Doubling the ampersand is what escapes it, and it has to leave one that
        /// was already doubled alone or the escape would show up as a literal pair.
        /// </remarks>
        /// <param name="title">The application title as the deployment supplied it.</param>
        /// <param name="expected">The text the label should carry.</param>
        [Theory]
        [InlineData("AT&T Connect", "AT&&T Connect")]
        [InlineData("Research & Development Suite", "Research && Development Suite")]
        [InlineData("Already && Escaped", "Already && Escaped")]
        [InlineData("No ampersands here", "No ampersands here")]
        [InlineData("&Leading", "&&Leading")]
        [InlineData("Trailing&", "Trailing&&")]
        public async Task Constructor_EscapesAmpersandsInTheApplicationName(string title, string expected)
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["AppTitle"] = title;

            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.Equal(expected, FormControls.Find<Label>(dialog, "labelAppName").Text);
        }

        /// <summary>
        /// Verifies that a deployment watching nothing has no close-applications section at all.
        /// </summary>
        /// <remarks>
        /// Watching nothing and watching something that turns out not to be running are different
        /// states: the first has no list to show ever, so the section is taken out of the layout, while
        /// the second keeps it in place because a process could start while the dialog is open.
        /// </remarks>
        [Fact]
        public async Task Constructor_RemovesTheProcessSectionWhenNothingIsBeingWatched()
        {
            // Act
            await using CloseAppsDialogState state = new(closeProcesses: null, NoOpLogAsync);
            using CloseAppsDialog dialog = Build(SampleOptions.CloseAppsDialog(), state);

            // Assert
            Assert.False(FormControls.Holds(dialog, "flowLayoutPanelCloseApps"));
        }

        /// <summary>
        /// Verifies that watching something not currently running keeps the section but hides it.
        /// </summary>
        [Fact]
        public async Task Constructor_KeepsTheProcessSectionHiddenWhenNothingIsRunning()
        {
            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(SampleOptions.CloseAppsDialog(), state);

            // Assert
            Assert.True(FormControls.Holds(dialog, "flowLayoutPanelCloseApps"));
            Assert.False(FormControls.Find<FlowLayoutPanel>(dialog, "flowLayoutPanelCloseApps").Visible);
            Assert.False(FormControls.Find<Button>(dialog, "buttonCloseProcesses").Enabled);
        }

        /// <summary>
        /// Verifies that a running application is listed for the user to close.
        /// </summary>
        /// <remarks>
        /// The test host is used as the running application because it is the one process a test can be
        /// certain about. What is listed is the description the deployment gave, not the executable
        /// name, since that is what a user would recognise.
        /// </remarks>
        [Fact]
        public async Task Constructor_ListsWhatIsRunning()
        {
            // Act
            await using CloseAppsDialogState state = SomethingRunning();
            using CloseAppsDialog dialog = Build(SampleOptions.CloseAppsDialog(), state);

            // Assert
            ListBox list = FormControls.Find<ListBox>(dialog, "listBoxCloseProcesses");
            Assert.Equal(RunningApplicationDescription, Assert.Single(list.Items));
            Assert.True(FormControls.Find<Button>(dialog, "buttonCloseProcesses").Enabled);
        }

        /// <summary>
        /// Verifies that hiding the close button also takes away the way past it.
        /// </summary>
        /// <remarks>
        /// The pair is the point. This option is for a deployment that will not let the user close the
        /// applications itself, so leaving Continue enabled would let them past a requirement the
        /// deployment means to enforce.
        /// </remarks>
        [Fact]
        public async Task Constructor_DisablesContinueAsWellWhenTheCloseButtonIsHidden()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["HideCloseButton"] = true;

            // Act
            await using CloseAppsDialogState state = SomethingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.False(FormControls.Find<Button>(dialog, "buttonCloseProcesses").Enabled);
            Assert.False(FormControls.Find<Button>(dialog, "buttonContinue").Enabled);
        }

        /// <summary>
        /// Verifies that a deployment offering no deferrals has no deferral section and cannot defer.
        /// </summary>
        [Fact]
        public async Task Constructor_RemovesTheDeferralSectionWhenNothingCanBeDeferred()
        {
            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(SampleOptions.CloseAppsDialog(), state);

            // Assert
            Assert.False(FormControls.Holds(dialog, "flowLayoutPanelDeferral"));
            Assert.False(FormControls.Find<Button>(dialog, "buttonDefer").Enabled);
        }

        /// <summary>
        /// Verifies that the number of deferrals left is shown and deferring is offered.
        /// </summary>
        [Fact]
        public async Task Constructor_ShowsHowManyDeferralsAreLeft()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 3u;

            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.Equal("deferrals remaining 3", FormControls.Find<Label>(dialog, "labelDeferDeadline").Text);
            Assert.True(FormControls.Find<Button>(dialog, "buttonDefer").Enabled);
        }

        /// <summary>
        /// Verifies that running out of deferrals takes the option away.
        /// </summary>
        [Fact]
        public async Task Constructor_RefusesToDeferWhenNoneAreLeft()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 0u;

            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.False(FormControls.Find<Button>(dialog, "buttonDefer").Enabled);
        }

        /// <summary>
        /// Verifies that unlimited deferrals means the count is not shown.
        /// </summary>
        /// <remarks>
        /// A number is only worth showing when it is running out. With the section left empty it is
        /// dropped altogether, which is what the emptiness check afterwards is for.
        /// </remarks>
        [Fact]
        public async Task Constructor_ShowsNoCountWhenDeferralsAreUnlimited()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 3u;
            table["UnlimitedDeferrals"] = true;

            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.False(FormControls.Holds(dialog, "flowLayoutPanelDeferral"));
        }

        /// <summary>
        /// Verifies that a deadline still in the future is shown and deferring is offered.
        /// </summary>
        [Fact]
        public async Task Constructor_OffersDeferralUntilTheDeadlinePasses()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralDeadline"] = DateTime.Now.AddDays(1);

            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.Contains("deferral deadline", FormControls.Find<Label>(dialog, "labelDeferDeadline").Text, StringComparison.Ordinal);
            Assert.True(FormControls.Find<Button>(dialog, "buttonDefer").Enabled);
        }

        /// <summary>
        /// Verifies that a deadline already past takes the option away.
        /// </summary>
        [Fact]
        public async Task Constructor_RefusesToDeferOnceTheDeadlineHasPassed()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralDeadline"] = DateTime.Now.AddDays(-1);

            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.False(FormControls.Find<Button>(dialog, "buttonDefer").Enabled);
        }

        /// <summary>
        /// Verifies that no countdown means the countdown panel is taken out of the layout.
        /// </summary>
        [Fact]
        public async Task Constructor_RemovesTheCountdownWhenThereIsNone()
        {
            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(SampleOptions.CloseAppsDialog(), state);

            // Assert
            Assert.False(FormControls.Holds(dialog, "flowLayoutPanelCountdown"));
        }

        /// <summary>
        /// Verifies that the countdown says it will close applications when there are some to close.
        /// </summary>
        /// <remarks>
        /// The countdown means different things depending on what is running, so it carries different
        /// wording: with applications listed it is counting down to closing them, and with none it is
        /// counting down to deferring. Telling a user their applications are about to be closed when
        /// none are open would be alarming and wrong, which is what makes the pair worth checking.
        /// </remarks>
        [Fact]
        public async Task Constructor_SaysItIsCountingDownToCloseWhenSomethingIsRunning()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(5);

            // Act
            await using CloseAppsDialogState state = SomethingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.Equal("counting down to close (Install)", FormControls.Find<Label>(dialog, "labelCountdownMessage").Text);
        }

        /// <summary>
        /// Verifies that the countdown says it will defer when there is nothing to close.
        /// </summary>
        [Fact]
        public async Task Constructor_SaysItIsCountingDownToDeferWhenNothingIsRunning()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(5);

            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.Equal("counting down to defer (Install)", FormControls.Find<Label>(dialog, "labelCountdownMessage").Text);
        }

        /// <summary>
        /// Verifies that a forced countdown says it will defer even with applications running.
        /// </summary>
        /// <remarks>
        /// A forced countdown ends in a deferral rather than a closure, so the wording follows the
        /// outcome rather than the list. This is the case where the two disagree.
        /// </remarks>
        [Fact]
        public async Task Constructor_SaysItIsCountingDownToDeferWhenTheCountdownIsForced()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["CountdownDuration"] = TimeSpan.FromMinutes(5);
            table["ForcedCountdown"] = true;

            // Act
            await using CloseAppsDialogState state = SomethingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Assert
            Assert.Equal("counting down to defer (Install)", FormControls.Find<Label>(dialog, "labelCountdownMessage").Text);
        }

        /// <summary>
        /// Verifies that no custom message means the custom message label is taken out of the layout.
        /// </summary>
        [Fact]
        public async Task Constructor_RemovesTheCustomMessageWhenThereIsNone()
        {
            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(SampleOptions.CloseAppsDialog(), state);

            // Assert
            Assert.False(FormControls.Holds(dialog, "labelCustomMessage"));
        }

        /// <summary>
        /// Verifies that each button records the answer it stands for.
        /// </summary>
        /// <remarks>
        /// Unlike the custom dialog, the answers here are fixed values rather than the button captions,
        /// because a caller acts on them: close means the deployment closes the applications itself,
        /// defer means it stops and comes back later, and continue means it proceeds regardless.
        /// </remarks>
        /// <param name="name">The designer's name for the button to click.</param>
        /// <param name="expectedResult">The answer that click should record.</param>
        [Theory]
        [InlineData("buttonCloseProcesses", "Close")]
        [InlineData("buttonDefer", "Defer")]
        [InlineData("buttonContinue", "Continue")]
        public async Task ButtonClick_RecordsTheAnswerItStandsFor(string name, string expectedResult)
        {
            // Arrange: deferrals present so that every button is in the layout to be clicked.
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 3u;
            await using CloseAppsDialogState state = SomethingRunning();
            using CloseAppsDialog dialog = Build(table, state);

            // Act
            DialogHost.Run(() => FormControls.Click(FormControls.Find<Button>(dialog, name)));

            // Assert
            Assert.Equal(expectedResult, dialog.DialogResult.ToString());
            Assert.True(NonPublic.Field<bool>(dialog, "canClose"));
        }

        /// <summary>
        /// Verifies that the dialog starts out reporting a timeout.
        /// </summary>
        [Fact]
        public async Task Constructor_StartsOutReportingATimeout()
        {
            // Act
            await using CloseAppsDialogState state = NothingRunning();
            using CloseAppsDialog dialog = Build(SampleOptions.CloseAppsDialog(), state);

            // Assert
            Assert.Equal(CloseAppsDialogResult.Timeout, dialog.DialogResult);
        }

        /// <summary>
        /// The description given to the process the tests use as a running application.
        /// </summary>
        private const string RunningApplicationDescription = "The Test Host";

        /// <summary>
        /// A state watching a process that is certainly running, namely the one running the tests.
        /// </summary>
        /// <returns>The state, which the caller owns.</returns>
        private static CloseAppsDialogState SomethingRunning()
        {
            using Process current = Process.GetCurrentProcess();
            return new([new ProcessDefinition(current.ProcessName, RunningApplicationDescription)], NoOpLogAsync);
        }

        /// <summary>
        /// A state watching a process nothing could be running under.
        /// </summary>
        /// <returns>The state, which the caller owns.</returns>
        private static CloseAppsDialogState NothingRunning()
        {
            return new([new ProcessDefinition("psadt-no-such-process-4f2b9c", "Nothing At All")], NoOpLogAsync);
        }

        /// <summary>
        /// Builds a close-applications dialog on the shared apartment from the given options and state.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="state">The state to build it with.</param>
        /// <returns>The dialog, which the caller owns.</returns>
        private static CloseAppsDialog Build(Hashtable table, CloseAppsDialogState state)
        {
            return DialogHost.Run(() => new CloseAppsDialog(new CloseAppsDialogOptions(DeploymentType.Install, table), state));
        }

        /// <summary>
        /// A logging action that records nothing, since none of these tests are about what was logged.
        /// </summary>
        /// <param name="message">The message that would have been logged.</param>
        /// <param name="severity">The severity it would have been logged at.</param>
        /// <param name="source">The command that would have logged it.</param>
        /// <returns>A completed task.</returns>
        private static ValueTask NoOpLogAsync(string message, LogSeverity severity, string source)
        {
            return default;
        }
    }
}
