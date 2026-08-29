using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Logging;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the Fluent dialog that asks a user to close applications before a deployment starts.
    /// </summary>
    /// <remarks>
    /// The most conditional dialog in the project. What it says, what its primary button offers and what
    /// a countdown expiring decides all depend on whether anything is running and whether deferrals are
    /// left, and those interact.
    /// <para>
    /// It also decides its own outcome by reading the name announced for its primary button, which is a
    /// slightly indirect way of asking which of two states it is in. That makes the announced names
    /// worth pinning in their own right: they are not only an accessibility concern here, they are load
    /// bearing.
    /// </para>
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
        /// Verifies that a running application is listed for the user to close.
        /// </summary>
        /// <remarks>
        /// The test host stands in for the running application because it is the one process a test can
        /// be certain about. What is listed is the description the deployment gave, not the executable
        /// name, since that is what a user would recognise - and the name is lowercased, because it is
        /// used to find the icon rather than to be read.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_ListsWhatIsRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), state, static dialog =>
            {
                CloseAppsDialog.AppToClose app = Assert.Single(dialog.AppsToCloseCollection);
                Assert.Equal(RunningApplicationDescription, app.Description);
                Assert.Equal(app.Name, app.Name.ToLowerInvariant(), ignoreCase: false);
                Assert.NotNull(app.Icon);
            });
        }

        /// <summary>
        /// Verifies that the applications section is shown when there is something in it.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_ShowsTheApplicationsSectionWhenSomethingIsRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), state, static dialog =>
                Assert.Equal(Visibility.Visible, dialog.CloseAppsStackPanel.Visibility));
        }

        /// <summary>
        /// Verifies that the applications section stays hidden when nothing is running.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_HidesTheApplicationsSectionWhenNothingIsRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), state, static dialog =>
            {
                Assert.Empty(dialog.AppsToCloseCollection);
                Assert.Equal(Visibility.Collapsed, dialog.CloseAppsStackPanel.Visibility);
            });
        }

        /// <summary>
        /// Verifies that the dialog says something different depending on whether anything is running.
        /// </summary>
        /// <remarks>
        /// Telling a user to close their applications when none are open would be confusing, so the two
        /// states have wording of their own. The primary button changes with the message, because in one
        /// state it closes applications and in the other it simply proceeds.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_SaysSomethingDifferentWhenSomethingIsRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState running = SomethingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), running, static dialog =>
            {
                Assert.Equal("dialog message (Install)", Text(dialog.MessageTextBlock));
                Assert.Equal("left (Install)", AutomationProperties.GetName(dialog.ButtonLeft));
            });

            await using CloseAppsDialogState idle = NothingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), idle, static dialog =>
            {
                Assert.Equal("nothing is running (Install)", Text(dialog.MessageTextBlock));
                Assert.Equal("left with nothing running (Install)", AutomationProperties.GetName(dialog.ButtonLeft));
            });
        }

        /// <summary>
        /// Verifies that hiding the close button leaves the primary action unavailable.
        /// </summary>
        /// <remarks>
        /// This option is for a deployment that will not let the user close the applications itself, so
        /// the button offers the wording that does not promise to and is disabled with it.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_DisablesThePrimaryActionWhenTheCloseButtonIsHidden()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["HideCloseButton"] = true;

            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(table, state, static dialog =>
            {
                Assert.False(dialog.ButtonLeft.IsEnabled);
                Assert.Equal("left with nothing running (Install)", AutomationProperties.GetName(dialog.ButtonLeft));
            });
        }

        /// <summary>
        /// Verifies that a deployment offering no deferrals hides the deferring button and its panels.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_HidesDeferringWhenNothingCanBeDeferred()
        {
            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), state, static dialog =>
            {
                Assert.Equal(Visibility.Collapsed, dialog.ButtonRight.Visibility);
                Assert.False(dialog.ButtonRight.IsEnabled);
                Assert.Equal(Visibility.Collapsed, dialog.DeferRemainingStackPanel.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.DeferDeadlineStackPanel.Visibility);
            });
        }

        /// <summary>
        /// Verifies that the number of deferrals left is shown and deferring is offered.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_ShowsHowManyDeferralsAreLeft()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 3u;

            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(table, state, static dialog =>
            {
                Assert.Equal(Visibility.Visible, dialog.DeferRemainingStackPanel.Visibility);
                Assert.Equal("3", dialog.DeferRemainingValueTextBlock.Text);
                Assert.Equal(Visibility.Visible, dialog.ButtonRight.Visibility);
                Assert.True(dialog.ButtonRight.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that running out of deferrals takes the option away and says so.
        /// </summary>
        /// <remarks>
        /// The count is recoloured and emboldened at zero rather than merely reading nought, because the
        /// button going grey is easy to miss.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_RefusesToDeferWhenNoneAreLeft()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 0u;

            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(table, state, static dialog =>
            {
                Assert.False(dialog.ButtonRight.IsEnabled);
                Assert.Equal("0", dialog.DeferRemainingValueTextBlock.Text);
                Assert.Equal(FontWeights.ExtraBold, dialog.DeferRemainingValueTextBlock.FontWeight);
            });
        }

        /// <summary>
        /// Verifies that unlimited deferrals means the count is not shown, though deferring is offered.
        /// </summary>
        /// <remarks>
        /// A number is only worth showing when it is running out.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_ShowsNoCountWhenDeferralsAreUnlimited()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 3u;
            table["UnlimitedDeferrals"] = true;

            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(table, state, static dialog =>
            {
                Assert.Equal(Visibility.Collapsed, dialog.DeferRemainingStackPanel.Visibility);
                Assert.Equal(Visibility.Visible, dialog.ButtonRight.Visibility);
                Assert.True(dialog.ButtonRight.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that a deadline still in the future is shown and deferring is offered.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_OffersDeferralUntilTheDeadlinePasses()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralDeadline"] = DateTime.Now.AddDays(7);

            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(table, state, static dialog =>
            {
                Assert.Equal(Visibility.Visible, dialog.DeferDeadlineStackPanel.Visibility);
                Assert.NotEmpty(dialog.DeferDeadlineValueTextBlock.Text);
                Assert.True(dialog.ButtonRight.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that a deadline already past takes the option away.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_RefusesToDeferOnceTheDeadlineHasPassed()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralDeadline"] = DateTime.Now.AddDays(-1);

            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(table, state, static dialog =>
            {
                Assert.False(dialog.ButtonRight.IsEnabled);
                Assert.Equal(FontWeights.ExtraBold, dialog.DeferDeadlineValueTextBlock.FontWeight);
            });
        }

        /// <summary>
        /// Verifies that the dialog starts out reporting a timeout.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_StartsOutReportingATimeout()
        {
            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), state, static dialog =>
                Assert.Equal(CloseAppsDialogResult.Timeout, dialog.DialogResult));
        }

        /// <summary>
        /// Verifies that the primary button reports closing when there are applications to close.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PrimaryClick_ReportsClosingWhenSomethingIsRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), state, static dialog =>
            {
                FluentControls.Click(dialog.ButtonLeft);
                Assert.Equal(CloseAppsDialogResult.Close, dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that the primary button reports continuing when nothing is running.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task PrimaryClick_ReportsContinuingWhenNothingIsRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(SampleOptions.CloseAppsDialog(), state, static dialog =>
            {
                FluentControls.Click(dialog.ButtonLeft);
                Assert.Equal(CloseAppsDialogResult.Continue, dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that the deferring button reports a deferral.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task DeferClick_ReportsADeferral()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["DeferralsRemaining"] = 3u;

            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(table, state, static dialog =>
            {
                FluentControls.Click(dialog.ButtonRight);
                Assert.Equal(CloseAppsDialogResult.Defer, dialog.DialogResult);
                Assert.True(FluentControls.WouldAllowClosing(dialog));
            });
        }

        /// <summary>
        /// Verifies that an expired countdown closes applications when some are running.
        /// </summary>
        /// <remarks>
        /// The countdown is set to no time at all so that the first tick is already past its end. That
        /// is safe for this dialog in a way it would not be for the restart one: what expiry does here
        /// is record an answer and close the window.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ExpiredCountdown_ClosesApplicationsWhenSomeAreRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(Expired(), state, static dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.Equal(CloseAppsDialogResult.Close, dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that an expired countdown simply continues when nothing is running.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ExpiredCountdown_ContinuesWhenNothingIsRunning()
        {
            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(Expired(), state, static dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.Equal(CloseAppsDialogResult.Continue, dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that a forced countdown defers rather than closing when a deferral is available.
        /// </summary>
        /// <remarks>
        /// A forced countdown is one whose expiry must not act on the user's applications, so it spends
        /// a deferral instead. This is the case where the outcome disagrees with what the button says.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ExpiredForcedCountdown_DefersWhenADeferralIsAvailable()
        {
            // Arrange
            Hashtable table = Expired();
            table["ForcedCountdown"] = true;
            table["DeferralsRemaining"] = 3u;

            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(table, state, static dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.Equal(CloseAppsDialogResult.Defer, dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that a forced countdown with nothing left to defer closes applications instead.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ExpiredForcedCountdown_ClosesApplicationsWithNoDeferralLeft()
        {
            // Arrange
            Hashtable table = Expired();
            table["ForcedCountdown"] = true;

            // Act & Assert
            await using CloseAppsDialogState state = SomethingRunning();
            WithDialog(table, state, static dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.Equal(CloseAppsDialogResult.Close, dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that an expired countdown also releases the dialog to close.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ExpiredCountdown_ReleasesTheDialogToClose()
        {
            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(Expired(), state, static dialog =>
            {
                FluentControls.Tick(dialog);
                Assert.True(FluentControls.WouldAllowClosing(dialog));
            });
        }

        /// <summary>
        /// Verifies that a custom message is shown with its markup rendered.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task Constructor_ShowsACustomMessage()
        {
            // Arrange
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["CustomMessageText"] = "Save your work [bold]first[/bold].";

            // Act & Assert
            await using CloseAppsDialogState state = NothingRunning();
            WithDialog(table, state, static dialog =>
            {
                Assert.Equal(Visibility.Visible, dialog.CustomMessageTextBlock.Visibility);
                Assert.Equal("Save your work first.", Text(dialog.CustomMessageTextBlock));
            });
        }

        /// <summary>
        /// The description given to the process the tests use as a running application.
        /// </summary>
        private const string RunningApplicationDescription = "The Test Host";

        /// <summary>
        /// The sample options with a countdown that has already run out.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        private static Hashtable Expired()
        {
            Hashtable table = SampleOptions.CloseAppsDialog();
            table["CountdownDuration"] = TimeSpan.Zero;
            return table;
        }

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
        /// Reads the text a block is showing, with any markup already rendered.
        /// </summary>
        /// <param name="block">The block to read.</param>
        /// <returns>The visible text.</returns>
        private static string Text(System.Windows.Controls.TextBlock block)
        {
            return string.Concat(block.Inlines.OfType<Run>().Select(static r => r.Text));
        }

        /// <summary>
        /// Builds a close-applications dialog, runs a body against it and disposes it, within the
        /// apartment.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="state">The state to build it with.</param>
        /// <param name="body">The assertions to run.</param>
        private static void WithDialog(Hashtable table, CloseAppsDialogState state, Action<CloseAppsDialog> body)
        {
            DialogHost.WithDialog(() => new CloseAppsDialog(new CloseAppsDialogOptions(DeploymentType.Install, table), state), body);
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
