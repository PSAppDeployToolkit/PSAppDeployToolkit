using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using Fluence.Wpf;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the Fluent dialog that reports how far along a deployment is.
    /// </summary>
    /// <remarks>
    /// The only dialog that is updated while it is on screen rather than answered and dismissed, so its
    /// update method is as much of its surface as its constructor is. Both go through the same private
    /// implementation, which is why the initial values and the later ones are held to the same
    /// expectations.
    /// <para>
    /// Unlike its Classic counterpart this one takes no alignment - the parameter is on the interface
    /// and ignored here - and its progress bar takes a fraction rather than a whole number, so the two
    /// differ in what they do with a value between the marks.
    /// </para>
    /// </remarks>
    public sealed class ProgressDialogTests
    {
        /// <summary>
        /// Verifies that the progress section is shown.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheProgressSection()
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), static dialog => Assert.Equal(Visibility.Visible, dialog.ProgressStackPanel.Visibility));
        }

        /// <summary>
        /// Verifies that the messages supplied up front are shown with their markup applied.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheMessagesItWasGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressMessageText"] = "Installing [bold]Contoso[/bold]";
            table["ProgressDetailMessageText"] = "Copying files";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal("Installing Contoso", Text(dialog.MessageTextBlock));
                Assert.Equal("Copying files", Text(dialog.ProgressMessageDetailTextBlock));
            });
        }

        /// <summary>
        /// Verifies that no percentage means a bar that shows activity rather than progress.
        /// </summary>
        /// <remarks>
        /// Most of a deployment cannot say how far along it is, so this is the ordinary case rather than
        /// the exceptional one.
        /// </remarks>
        [Fact]
        public void Constructor_ShowsAnIndeterminateBarWhenThereIsNoPercentage()
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), static dialog => Assert.Equal(ProgressBarMode.Indeterminate, dialog.ProgressBar.ProgressMode));
        }

        /// <summary>
        /// Verifies that a percentage supplied up front produces a bar showing it.
        /// </summary>
        [Fact]
        public void Constructor_ShowsAMeasuredBarWhenGivenAPercentage()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = 40.0;

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal(ProgressBarMode.StepProgress, dialog.ProgressBar.ProgressMode);
                Assert.Equal(40.0, dialog.ProgressBar.Value);
            });
        }

        /// <summary>
        /// Verifies that an update changes the message it was given and leaves the other alone.
        /// </summary>
        [Fact]
        public void UpdateProgress_ChangesOnlyWhatItWasGiven()
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), static dialog =>
            {
                dialog.UpdateProgress(progressMessageDetail: "Now copying files");
                Assert.Equal("the progress message", Text(dialog.MessageTextBlock));
                Assert.Equal("Now copying files", Text(dialog.ProgressMessageDetailTextBlock));
            });
        }

        /// <summary>
        /// Verifies that an update carrying a percentage switches the bar to showing it.
        /// </summary>
        [Fact]
        public void UpdateProgress_SwitchesTheBarToAMeasuredValue()
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), static dialog =>
            {
                dialog.UpdateProgress(progressPercentage: 75.0);
                Assert.Equal(ProgressBarMode.StepProgress, dialog.ProgressBar.ProgressMode);
                Assert.Equal(75.0, dialog.ProgressBar.Value);
            });
        }

        /// <summary>
        /// Verifies that a fraction of a percent is kept rather than rounded away.
        /// </summary>
        /// <remarks>
        /// The Classic bar counts in whole numbers and so loses this; the Fluent one does not. Pinned
        /// because it is a real difference between the two styles rather than an accident of either.
        /// </remarks>
        [Fact]
        public void UpdateProgress_KeepsAFractionalPercentage()
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), static dialog =>
            {
                dialog.UpdateProgress(progressPercentage: 49.9);
                Assert.Equal(49.9, dialog.ProgressBar.Value);
            });
        }

        /// <summary>
        /// Verifies that an update carrying no percentage puts the bar back to showing activity.
        /// </summary>
        [Fact]
        public void UpdateProgress_PutsTheBarBackToIndeterminateWithoutAPercentage()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = 40.0;

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                dialog.UpdateProgress("Still working");
                Assert.Equal(ProgressBarMode.Indeterminate, dialog.ProgressBar.ProgressMode);
            });
        }

        /// <summary>
        /// Verifies that the progress is announced to assistive technology.
        /// </summary>
        /// <remarks>
        /// A bar conveys its meaning by how full it is, which is no help to a screen reader. The
        /// percentage is therefore restated as a name whenever it changes, and rounded to whole percent
        /// so it is short enough to be worth hearing.
        /// </remarks>
        [Fact]
        public void UpdateProgress_AnnouncesTheProgress()
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), static dialog =>
            {
                dialog.UpdateProgress(progressPercentage: 42.6);
                Assert.Equal("Progress: 43%", AutomationProperties.GetName(dialog.ProgressBar));
            });
        }

        /// <summary>
        /// Verifies that the messages are announced to assistive technology.
        /// </summary>
        /// <remarks>
        /// The announced text is the message as the deployment wrote it, markup and all, while the
        /// visible text has the markup rendered. Recorded because the two deliberately differ.
        /// </remarks>
        [Fact]
        public void UpdateProgress_AnnouncesTheMessagesAsTheyWereWritten()
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), static dialog =>
            {
                dialog.UpdateProgress("Installing [bold]Contoso[/bold]");
                Assert.Equal("Installing [bold]Contoso[/bold]", AutomationProperties.GetName(dialog.MessageTextBlock));
                Assert.Equal("Installing Contoso", Text(dialog.MessageTextBlock));
            });
        }

        /// <summary>
        /// Verifies that a message present but blank is refused.
        /// </summary>
        /// <remarks>
        /// Absent means "leave it alone" and is the ordinary case, so blank cannot also mean that
        /// without a caller losing the ability to tell the two apart. It is refused instead.
        /// </remarks>
        /// <param name="text">The blank text to try.</param>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t\r\n")]
        public void UpdateProgress_RefusesABlankMessage(string text)
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), dialog =>
            {
                _ = Assert.Throws<ArgumentException>(() => dialog.UpdateProgress(text));
                _ = Assert.Throws<ArgumentException>(() => dialog.UpdateProgress(progressMessageDetail: text));
            });
        }

        /// <summary>
        /// Verifies that a percentage outside the range a bar can show is refused.
        /// </summary>
        /// <remarks>
        /// The same check guards the options type and the client/server payload; this is the one on the
        /// path a caller reaches by holding the dialog directly. It matters more here than it looks:
        /// this bar would accept a nonsensical value rather than throwing, so without the check a
        /// deployment reporting 150 percent would simply draw a bar past its own end.
        /// </remarks>
        /// <param name="percentage">The percentage to try.</param>
        [Theory]
        [InlineData(-0.1)]
        [InlineData(-1.0)]
        [InlineData(100.1)]
        [InlineData(150.0)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void UpdateProgress_RefusesAPercentageOutsideTheBarsRange(double percentage)
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), dialog =>
                _ = Assert.Throws<ArgumentOutOfRangeException>(() => dialog.UpdateProgress(progressPercentage: percentage)));
        }

        /// <summary>
        /// Verifies that the ends of the range are accepted.
        /// </summary>
        /// <param name="percentage">The percentage to try.</param>
        [Theory]
        [InlineData(0.0)]
        [InlineData(100.0)]
        public void UpdateProgress_AcceptsTheEndsOfTheRange(double percentage)
        {
            // Act & Assert
            WithDialog(SampleOptions.ProgressDialog(), dialog =>
            {
                dialog.UpdateProgress(progressPercentage: percentage);
                Assert.Equal(percentage, dialog.ProgressBar.Value);
            });
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
        /// Builds a progress dialog, runs a body against it and disposes it, all within the apartment.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="body">The assertions to run.</param>
        private static void WithDialog(Hashtable table, Action<ProgressDialog> body)
        {
            DialogHost.WithDialog(() => new ProgressDialog(new ProgressDialogOptions(table)), body);
        }
    }
}
