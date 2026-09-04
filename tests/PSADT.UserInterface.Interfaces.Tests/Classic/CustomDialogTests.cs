using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the Classic dialog that asks a question and reports which button answered it.
    /// </summary>
    /// <remarks>
    /// Configuration here is subtractive: a button the deployment did not ask for is taken out of the
    /// layout rather than merely hidden, and so is the icon. That matters because the panel these
    /// controls sit in sizes itself to what remains, so leaving an unwanted control in place would show
    /// as a gap rather than as a stray button.
    /// <para>
    /// The buttons are wired to their handlers by the designer, so the answers a click records are
    /// reached by clicking rather than by calling the handler directly.
    /// </para>
    /// </remarks>
    public sealed class CustomDialogTests
    {
        /// <summary>
        /// Verifies that a button the deployment asked for is shown with the text it asked for.
        /// </summary>
        [Fact]
        public void Constructor_ShowsAButtonThatWasAskedFor()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["ButtonRightText"] = "Continue";

            // Act
            using CustomDialog dialog = Build(table);

            // Assert
            Assert.Equal("Continue", FormControls.Find<Button>(dialog, "buttonRight").Text);
        }

        /// <summary>
        /// Verifies that button text has any markup removed.
        /// </summary>
        /// <remarks>
        /// A button face cannot render markup, and a deployment reusing a string that carries it would
        /// otherwise show the tags to the user.
        /// </remarks>
        [Fact]
        public void Constructor_StripsMarkupFromButtonText()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["ButtonRightText"] = "[bold]Continue[/bold]";

            // Act
            using CustomDialog dialog = Build(table);

            // Assert
            Assert.Equal("Continue", FormControls.Find<Button>(dialog, "buttonRight").Text);
        }

        /// <summary>
        /// Verifies that a button the deployment did not ask for is taken out of the layout.
        /// </summary>
        /// <remarks>
        /// The sample options define only the right-hand button, so the other two are the ones expected
        /// to have gone.
        /// </remarks>
        [Fact]
        public void Constructor_RemovesTheButtonsThatWereNotAskedFor()
        {
            // Act
            using CustomDialog dialog = Build(SampleOptions.CustomDialog());

            // Assert
            Assert.False(FormControls.Holds(dialog, "buttonLeft"));
            Assert.False(FormControls.Holds(dialog, "buttonMiddle"));
            Assert.True(FormControls.Holds(dialog, "buttonRight"));
        }

        /// <summary>
        /// Verifies that all three buttons can be shown at once.
        /// </summary>
        [Fact]
        public void Constructor_ShowsAllThreeButtonsWhenAllThreeAreAskedFor()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["ButtonLeftText"] = "Left";
            table["ButtonMiddleText"] = "Middle";
            table["ButtonRightText"] = "Right";

            // Act
            using CustomDialog dialog = Build(table);

            // Assert
            Assert.Equal("Left", FormControls.Find<Button>(dialog, "buttonLeft").Text);
            Assert.Equal("Middle", FormControls.Find<Button>(dialog, "buttonMiddle").Text);
            Assert.Equal("Right", FormControls.Find<Button>(dialog, "buttonRight").Text);
        }

        /// <summary>
        /// Verifies that the message is shown with any markup removed.
        /// </summary>
        [Fact]
        public void Constructor_StripsMarkupFromTheMessage()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["MessageText"] = "This will take [bold]about ten minutes[/bold].";

            // Act
            using CustomDialog dialog = Build(table);

            // Assert
            Assert.Equal("This will take about ten minutes.", FormControls.Find<Label>(dialog, "labelMessage").Text);
        }

        /// <summary>
        /// Verifies that no icon means the icon's space is given back to the message.
        /// </summary>
        /// <remarks>
        /// The message and the icon share a two-column panel. Removing the icon without also widening
        /// the message would leave the message in its original column and the dialog with an empty
        /// gutter down its left-hand side, so the two changes belong together.
        /// </remarks>
        [Fact]
        public void Constructor_GivesTheIconsSpaceToTheMessageWhenThereIsNoIcon()
        {
            // Act
            using CustomDialog dialog = Build(SampleOptions.CustomDialog());

            // Assert
            Assert.False(FormControls.Holds(dialog, "pictureIcon"));
            TableLayoutPanel panel = FormControls.Find<TableLayoutPanel>(dialog, "tableLayoutPanelIconMessage");
            Label message = FormControls.Find<Label>(dialog, "labelMessage");
            Assert.Equal(0, panel.GetColumn(message));
            Assert.Equal(2, panel.GetColumnSpan(message));
        }

        /// <summary>
        /// Verifies that an icon the deployment asked for is shown.
        /// </summary>
        [Fact]
        public void Constructor_ShowsAnIconThatWasAskedFor()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["Icon"] = DialogSystemIcon.Warning;

            // Act
            using CustomDialog dialog = Build(table);

            // Assert
            Assert.NotNull(FormControls.Find<PictureBox>(dialog, "pictureIcon").Image);
        }

        /// <summary>
        /// Verifies that the requested message alignment reaches the label.
        /// </summary>
        /// <remarks>
        /// The alignment is resolved by pasting the requested value onto "Top" and parsing the result as
        /// a Windows Forms content alignment, which works only because the two enumerations happen to
        /// agree on their names. A value added to one and not the other would stop parsing and fall back
        /// silently, so each is checked rather than a representative one.
        /// </remarks>
        /// <param name="alignment">The alignment the deployment asked for.</param>
        /// <param name="expected">The alignment the label should end up with.</param>
        [Theory]
        [InlineData(DialogMessageAlignment.Left, ContentAlignment.TopLeft)]
        [InlineData(DialogMessageAlignment.Center, ContentAlignment.TopCenter)]
        [InlineData(DialogMessageAlignment.Right, ContentAlignment.TopRight)]
        public void Constructor_AppliesTheRequestedMessageAlignment(DialogMessageAlignment alignment, ContentAlignment expected)
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["MessageAlignment"] = alignment;

            // Act
            using CustomDialog dialog = Build(table);

            // Assert
            Assert.Equal(expected, FormControls.Find<Label>(dialog, "labelMessage").TextAlign);
        }

        /// <summary>
        /// Verifies that the dialog starts out reporting a timeout.
        /// </summary>
        /// <remarks>
        /// The result has to mean something before any button is pressed, because an expiry timer can
        /// close the dialog without one ever being pressed. Timeout is that value, and a caller reads it
        /// to tell an unanswered dialog from an answered one.
        /// </remarks>
        [Fact]
        public void Constructor_StartsOutReportingATimeout()
        {
            // Act
            using CustomDialog dialog = Build(SampleOptions.CustomDialog());

            // Assert
            Assert.Equal(CustomDialogResult.DefaultResult, dialog.DialogResult);
        }

        /// <summary>
        /// Verifies that clicking a button records which one was clicked.
        /// </summary>
        /// <remarks>
        /// The answer is the button's own caption rather than a position, so a deployment reads back the
        /// text it supplied. That is why the caption has to have had its markup stripped by the time it
        /// is read: whatever is on the button face is what the caller is told.
        /// </remarks>
        /// <param name="name">The designer's name for the button to click.</param>
        /// <param name="caption">The caption that button was given.</param>
        [Theory]
        [InlineData("buttonLeft", "Left")]
        [InlineData("buttonMiddle", "Middle")]
        [InlineData("buttonRight", "Right")]
        public void ButtonClick_RecordsTheButtonsCaptionAsTheAnswer(string name, string caption)
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["ButtonLeftText"] = "Left";
            table["ButtonMiddleText"] = "Middle";
            table["ButtonRightText"] = "Right";
            using CustomDialog dialog = Build(table);

            // Act
            DialogHost.Run(() => FormControls.Click(FormControls.Find<Button>(dialog, name)));

            // Assert
            Assert.Equal(new CustomDialogResult(caption), dialog.DialogResult);
        }

        /// <summary>
        /// Verifies that clicking a button also releases the dialog to close.
        /// </summary>
        /// <remarks>
        /// Recording the answer and permitting the close are separate steps, and a click has to do both:
        /// the dialog refuses every close it did not authorise, so an answer recorded without the
        /// release would leave the window on screen with the user's choice already taken.
        /// </remarks>
        [Fact]
        public void ButtonClick_ReleasesTheDialogToClose()
        {
            // Arrange
            using CustomDialog dialog = Build(SampleOptions.CustomDialog());

            // Act
            DialogHost.Run(() => FormControls.Click(FormControls.Find<Button>(dialog, "buttonRight")));

            // Assert
            Assert.True(NonPublic.Field<bool>(dialog, "canClose"));
        }

        /// <summary>
        /// Builds a custom dialog on the shared apartment from the given options.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <returns>The dialog, which the caller owns.</returns>
        private static CustomDialog Build(Hashtable table)
        {
            return DialogHost.Run(() => new CustomDialog(new CustomDialogOptions(table)));
        }
    }
}
