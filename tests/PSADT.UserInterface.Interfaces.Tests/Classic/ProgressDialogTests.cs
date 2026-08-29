using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the Classic dialog that reports how far along a deployment is.
    /// </summary>
    /// <remarks>
    /// The only dialog that is updated while it is on screen rather than answered and dismissed, so its
    /// update method is as much of its surface as its constructor is. Both go through the same private
    /// implementation, which is why the constructor's handling of the initial values and the update
    /// method's handling of later ones are checked against the same expectations.
    /// </remarks>
    public sealed class ProgressDialogTests
    {
        /// <summary>
        /// Verifies that the messages supplied up front are shown, with any markup removed.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheMessagesItWasGiven()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressMessageText"] = "Installing [bold]Contoso[/bold]";
            table["ProgressDetailMessageText"] = "Copying files";

            // Act
            using ProgressDialog dialog = Build(table);

            // Assert
            Assert.Equal("Installing Contoso", FormControls.Find<Label>(dialog, "labelMessage").Text);
            Assert.Equal("Copying files", FormControls.Find<Label>(dialog, "labelDetail").Text);
        }

        /// <summary>
        /// Verifies that no percentage means a bar that shows activity rather than progress.
        /// </summary>
        /// <remarks>
        /// Most of a deployment cannot say how far along it is, so the marquee is the ordinary case
        /// rather than the exceptional one.
        /// </remarks>
        [Fact]
        public void Constructor_ShowsAMarqueeWhenThereIsNoPercentage()
        {
            // Act
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Assert
            Assert.Equal(ProgressBarStyle.Marquee, FormControls.Find<ProgressBar>(dialog, "progressBar").Style);
        }

        /// <summary>
        /// Verifies that a percentage supplied up front produces a determinate bar at that value.
        /// </summary>
        [Fact]
        public void Constructor_ShowsADeterminateBarWhenGivenAPercentage()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = 40.0;

            // Act
            using ProgressDialog dialog = Build(table);

            // Assert
            ProgressBar bar = FormControls.Find<ProgressBar>(dialog, "progressBar");
            Assert.Equal(ProgressBarStyle.Blocks, bar.Style);
            Assert.Equal(40, bar.Value);
        }

        /// <summary>
        /// Verifies that the messages default to centred.
        /// </summary>
        /// <remarks>
        /// Alignment is the one setting that is reapplied on every update rather than only when it
        /// changes, so an update that does not mention it resets both labels to centre. This is that
        /// default arriving by the constructor's route.
        /// </remarks>
        [Fact]
        public void Constructor_CentresTheMessagesByDefault()
        {
            // Act
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Assert
            Assert.Equal(ContentAlignment.TopCenter, FormControls.Find<Label>(dialog, "labelMessage").TextAlign);
            Assert.Equal(ContentAlignment.TopCenter, FormControls.Find<Label>(dialog, "labelDetail").TextAlign);
        }

        /// <summary>
        /// Verifies that an update changes the message it was given and leaves the other alone.
        /// </summary>
        /// <remarks>
        /// Each part of an update is optional, so a caller reporting only that the detail changed must
        /// not have the main message blanked underneath it.
        /// </remarks>
        [Fact]
        public void UpdateProgress_ChangesOnlyWhatItWasGiven()
        {
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Act
            DialogHost.Run(() => dialog.UpdateProgress(progressMessageDetail: "Now copying files"));

            // Assert
            Assert.Equal("the progress message", FormControls.Find<Label>(dialog, "labelMessage").Text);
            Assert.Equal("Now copying files", FormControls.Find<Label>(dialog, "labelDetail").Text);
        }

        /// <summary>
        /// Verifies that an update carrying a percentage switches the bar to showing it.
        /// </summary>
        [Fact]
        public void UpdateProgress_SwitchesTheBarToADeterminateValue()
        {
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Act
            DialogHost.Run(() => dialog.UpdateProgress(progressPercentage: 75.0));

            // Assert
            ProgressBar bar = FormControls.Find<ProgressBar>(dialog, "progressBar");
            Assert.Equal(ProgressBarStyle.Blocks, bar.Style);
            Assert.Equal(75, bar.Value);
        }

        /// <summary>
        /// Verifies that an update carrying no percentage puts the bar back to showing activity.
        /// </summary>
        /// <remarks>
        /// A deployment that reported a percentage and then stopped being able to is put back into the
        /// marquee rather than left frozen at whatever it last said.
        /// </remarks>
        [Fact]
        public void UpdateProgress_PutsTheBarBackToAMarqueeWithoutAPercentage()
        {
            // Arrange
            Hashtable table = SampleOptions.ProgressDialog();
            table["ProgressPercentage"] = 40.0;
            using ProgressDialog dialog = Build(table);

            // Act
            DialogHost.Run(() => dialog.UpdateProgress("Still working"));

            // Assert
            Assert.Equal(ProgressBarStyle.Marquee, FormControls.Find<ProgressBar>(dialog, "progressBar").Style);
        }

        /// <summary>
        /// Verifies that a percentage is rounded towards zero rather than to the nearest whole.
        /// </summary>
        /// <remarks>
        /// The bar counts in whole numbers and the caller supplies a fraction, so something has to give.
        /// Recorded because it is a cast rather than a rounding decision anybody made: 49.9 shows as 49.
        /// </remarks>
        [Fact]
        public void UpdateProgress_RoundsAFractionalPercentageTowardsZero()
        {
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Act
            DialogHost.Run(() => dialog.UpdateProgress(progressPercentage: 49.9));

            // Assert
            Assert.Equal(49, FormControls.Find<ProgressBar>(dialog, "progressBar").Value);
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
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => DialogHost.Run(() => dialog.UpdateProgress(text)));
            _ = Assert.Throws<ArgumentException>(() => DialogHost.Run(() => dialog.UpdateProgress(progressMessageDetail: text)));
        }

        /// <summary>
        /// Verifies that a percentage outside the range a bar can show is refused.
        /// </summary>
        /// <remarks>
        /// The bar itself throws for a value outside nought to a hundred, so without this check the
        /// caller's mistake surfaced from inside Windows Forms with nothing naming the percentage. The
        /// same check guards the options type and the client/server payload; this is the last one, on
        /// the path a caller reaches by holding the dialog directly.
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
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => DialogHost.Run(() => dialog.UpdateProgress(progressPercentage: percentage)));
        }

        /// <summary>
        /// Verifies that the ends of the range are accepted.
        /// </summary>
        /// <remarks>
        /// Nought and a hundred are the two values a deployment is most likely to send - one at the
        /// start and one at the end - so a check written with the wrong comparison would fail exactly
        /// where it is least acceptable.
        /// </remarks>
        /// <param name="percentage">The percentage to try.</param>
        [Theory]
        [InlineData(0.0)]
        [InlineData(100.0)]
        public void UpdateProgress_AcceptsTheEndsOfTheRange(double percentage)
        {
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Act
            DialogHost.Run(() => dialog.UpdateProgress(progressPercentage: percentage));

            // Assert
            Assert.Equal((int)percentage, FormControls.Find<ProgressBar>(dialog, "progressBar").Value);
        }

        /// <summary>
        /// Verifies that the requested message alignment reaches both labels.
        /// </summary>
        /// <param name="alignment">The alignment the caller asked for.</param>
        /// <param name="expected">The alignment the labels should end up with.</param>
        [Theory]
        [InlineData(DialogMessageAlignment.Left, ContentAlignment.TopLeft)]
        [InlineData(DialogMessageAlignment.Center, ContentAlignment.TopCenter)]
        [InlineData(DialogMessageAlignment.Right, ContentAlignment.TopRight)]
        public void UpdateProgress_AppliesTheRequestedAlignmentToBothLabels(DialogMessageAlignment alignment, ContentAlignment expected)
        {
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());

            // Act
            DialogHost.Run(() => dialog.UpdateProgress(messageAlignment: alignment));

            // Assert
            Assert.Equal(expected, FormControls.Find<Label>(dialog, "labelMessage").TextAlign);
            Assert.Equal(expected, FormControls.Find<Label>(dialog, "labelDetail").TextAlign);
        }

        /// <summary>
        /// Verifies that an update mentioning no alignment leaves the alignment alone.
        /// </summary>
        /// <remarks>
        /// Absent means "leave it alone" for every part of an update, and alignment is no exception. It
        /// used to be: the alignment was reapplied on every update and fell back to centred whenever one
        /// was not supplied, so an update that only changed the message silently re-centred a dialog the
        /// caller had left-aligned.
        /// </remarks>
        [Fact]
        public void UpdateProgress_LeavesAlignmentAloneWhenAnUpdateDoesNotMentionIt()
        {
            // Arrange
            using ProgressDialog dialog = Build(SampleOptions.ProgressDialog());
            DialogHost.Run(() => dialog.UpdateProgress(messageAlignment: DialogMessageAlignment.Left));

            // Act
            DialogHost.Run(() => dialog.UpdateProgress("Still working"));

            // Assert
            Assert.Equal(ContentAlignment.TopLeft, FormControls.Find<Label>(dialog, "labelMessage").TextAlign);
            Assert.Equal(ContentAlignment.TopLeft, FormControls.Find<Label>(dialog, "labelDetail").TextAlign);
        }

        /// <summary>
        /// Builds a progress dialog on the shared apartment from the given options.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <returns>The dialog, which the caller owns.</returns>
        private static ProgressDialog Build(Hashtable table)
        {
            return DialogHost.Run(() => new ProgressDialog(new ProgressDialogOptions(table)));
        }
    }
}
