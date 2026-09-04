using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the Fluent dialog that asks a question and reports which button answered it.
    /// </summary>
    /// <remarks>
    /// Where the Classic dialog removes the buttons it was not asked for, this one hides them, and the
    /// layout pass then shares the width between whichever are left. The answer is the button's caption
    /// either way, which is why the accelerator marker has to come back off before the result is built:
    /// a deployment reads back the text it supplied, not the text with an underscore in it.
    /// </remarks>
    public sealed class CustomDialogTests
    {
        /// <summary>
        /// Verifies that the message is rendered with its markup applied.
        /// </summary>
        [Fact]
        public void Constructor_RendersTheMessage()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["MessageText"] = "This takes [bold]ten minutes[/bold].";

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Run[] runs = [.. dialog.MessageTextBlock.Inlines.OfType<Run>()];
                Assert.Equal("This takes ten minutes.", string.Concat(runs.Select(static r => r.Text)));
                Assert.Equal(FontWeights.Bold, runs[1].FontWeight);
            });
        }

        /// <summary>
        /// Verifies that the button row is shown.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheButtonRow()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog => Assert.Equal(Visibility.Visible, dialog.ButtonPanel.Visibility));
        }

        /// <summary>
        /// Verifies that only the buttons the deployment asked for are shown.
        /// </summary>
        /// <remarks>
        /// The sample options define the right-hand button alone, so the other two are the ones expected
        /// to stay hidden.
        /// </remarks>
        [Fact]
        public void Constructor_ShowsOnlyTheButtonsThatWereAskedFor()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                Assert.Equal(Visibility.Collapsed, dialog.ButtonLeft.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.ButtonMiddle.Visibility);
                Assert.Equal(Visibility.Visible, dialog.ButtonRight.Visibility);
            });
        }

        /// <summary>
        /// Verifies that all three buttons can be shown at once, each with its own caption.
        /// </summary>
        [Fact]
        public void Constructor_ShowsAllThreeButtonsWhenAllThreeAreAskedFor()
        {
            // Act & Assert
            WithDialog(ThreeButtons(), static dialog =>
            {
                Assert.Equal("Left", FluentControls.Caption(dialog.ButtonLeft));
                Assert.Equal("Middle", FluentControls.Caption(dialog.ButtonMiddle));
                Assert.Equal("Right", FluentControls.Caption(dialog.ButtonRight));
            });
        }

        /// <summary>
        /// Verifies that a button's caption is announced to assistive technology.
        /// </summary>
        /// <remarks>
        /// The announced name is the text as the deployment wrote it, accelerator marker and all, while
        /// the caption on the button face has the marker interpreted. That difference matters twice
        /// over, because the close-applications dialog reads the announced name back to decide which of
        /// its two possible captions the button is currently showing.
        /// </remarks>
        [Fact]
        public void Constructor_AnnouncesTheButtonCaption()
        {
            // Arrange
            Hashtable table = SampleOptions.CustomDialog();
            table["ButtonRightText"] = "_Continue";

            // Act & Assert
            WithDialog(table, static dialog => Assert.Equal("_Continue", AutomationProperties.GetName(dialog.ButtonRight)));
        }

        /// <summary>
        /// Verifies that the button named as the default is the one Enter activates and the one accented.
        /// </summary>
        /// <param name="defaultButton">The button the deployment nominated.</param>
        /// <param name="expectedName">The designer's name for the button that should end up default.</param>
        [Theory]
        [InlineData(DialogDefaultButton.Left, "ButtonLeft")]
        [InlineData(DialogDefaultButton.Middle, "ButtonMiddle")]
        [InlineData(DialogDefaultButton.Right, "ButtonRight")]
        public void Constructor_MakesTheNominatedButtonTheDefault(DialogDefaultButton defaultButton, string expectedName)
        {
            // Arrange
            Hashtable table = ThreeButtons();
            table["DefaultButton"] = defaultButton;

            // Act & Assert
            WithDialog(table, dialog =>
            {
                Fluence.Wpf.Controls.Button expected = expectedName switch
                {
                    "ButtonLeft" => dialog.ButtonLeft,
                    "ButtonMiddle" => dialog.ButtonMiddle,
                    _ => dialog.ButtonRight,
                };
                Assert.True(expected.IsDefault);
                Assert.Equal(Fluence.Wpf.ControlAppearance.Accent, expected.Appearance);
            });
        }

        /// <summary>
        /// Verifies that the dialog starts out reporting a timeout.
        /// </summary>
        /// <remarks>
        /// The result has to mean something before any button is pressed, because an expiry timer can
        /// close the dialog without one ever being pressed.
        /// </remarks>
        [Fact]
        public void Constructor_StartsOutReportingATimeout()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog => Assert.Equal(CustomDialogResult.DefaultResult, dialog.DialogResult));
        }

        /// <summary>
        /// Verifies that clicking a button records its caption as the answer.
        /// </summary>
        /// <remarks>
        /// The accelerator marker is taken back out, so a deployment that wrote "_Continue" to give the
        /// button a keyboard shortcut reads back "Continue" and not the marker it used to place it.
        /// </remarks>
        /// <param name="which">Which of the three buttons to click.</param>
        /// <param name="expected">The answer that click should record.</param>
        [Theory]
        [InlineData("left", "Left")]
        [InlineData("middle", "Middle")]
        [InlineData("right", "Right")]
        public void ButtonClick_RecordsTheCaptionAsTheAnswer(string which, string expected)
        {
            // Arrange
            Hashtable table = ThreeButtons();
            table["ButtonLeftText"] = "_Left";
            table["ButtonMiddleText"] = "_Middle";
            table["ButtonRightText"] = "_Right";

            // Act & Assert
            WithDialog(table, dialog =>
            {
                FluentControls.Click(Pick(dialog, which));
                Assert.Equal(new CustomDialogResult(expected), dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that a result already recorded is not overwritten by the base handler.
        /// </summary>
        /// <remarks>
        /// The base only fills the result in while it still reads as a timeout, which is what lets the
        /// input and list selection dialogs record a richer answer of their own and then call through
        /// for the window closing. Reached here by clicking twice: the second click finds a result that
        /// is no longer the default and leaves it alone.
        /// </remarks>
        [Fact]
        public void ButtonClick_DoesNotOverwriteAnAnswerAlreadyRecorded()
        {
            // Act & Assert
            WithDialog(ThreeButtons(), static dialog =>
            {
                FluentControls.Click(dialog.ButtonLeft);
                FluentControls.Click(dialog.ButtonRight);
                Assert.Equal(new CustomDialogResult("Left"), dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that clicking a button also releases the dialog to close.
        /// </summary>
        [Fact]
        public void ButtonClick_ReleasesTheDialogToClose()
        {
            // Act & Assert
            WithDialog(SampleOptions.CustomDialog(), static dialog =>
            {
                FluentControls.Click(dialog.ButtonRight);
                Assert.True(FluentControls.WouldAllowClosing(dialog));
            });
        }

        /// <summary>
        /// The sample options with all three buttons defined.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        private static Hashtable ThreeButtons()
        {
            Hashtable table = SampleOptions.CustomDialog();
            table["ButtonLeftText"] = "Left";
            table["ButtonMiddleText"] = "Middle";
            table["ButtonRightText"] = "Right";
            return table;
        }

        /// <summary>
        /// Picks one of the dialog's three buttons by name.
        /// </summary>
        /// <param name="dialog">The dialog to pick from.</param>
        /// <param name="which">Which button to pick.</param>
        /// <returns>The button.</returns>
        private static Fluence.Wpf.Controls.Button Pick(CustomDialog dialog, string which)
        {
            return which switch
            {
                "left" => dialog.ButtonLeft,
                "middle" => dialog.ButtonMiddle,
                _ => dialog.ButtonRight,
            };
        }

        /// <summary>
        /// Builds a custom dialog, runs a body against it and disposes it, all within the apartment.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="body">The assertions to run.</param>
        private static void WithDialog(Hashtable table, System.Action<CustomDialog> body)
        {
            DialogHost.WithDialog(() => new CustomDialog(new CustomDialogOptions(table)), body);
        }
    }
}
