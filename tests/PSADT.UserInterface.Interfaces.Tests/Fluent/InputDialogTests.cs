using System;
using System.Collections;
using System.Windows;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the Fluent dialog that asks the user to type something.
    /// </summary>
    /// <remarks>
    /// The only dialog whose answer carries data rather than just a choice, and the only one whose
    /// primary button is enabled and disabled as the user types: an empty answer is no answer, so the
    /// button that accepts it stays unavailable until there is something to accept.
    /// <para>
    /// Two input controls sit on top of one another and exactly one is shown, depending on whether the
    /// deployment asked for the typing to be masked. Which of them the answer is read from follows the
    /// same flag, so a dialog showing one box and reading the other would return an empty answer however
    /// much the user typed - which is what makes the pairing worth testing rather than assuming.
    /// </para>
    /// </remarks>
    public sealed class InputDialogTests
    {
        /// <summary>
        /// Verifies that the input section is shown.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheInputSection()
        {
            // Act & Assert
            WithDialog(Options(), static dialog => Assert.Equal(Visibility.Visible, dialog.InputBoxStackPanel.Visibility));
        }

        /// <summary>
        /// Verifies that ordinary input shows the plain box and hides the masked one.
        /// </summary>
        [Fact]
        public void Constructor_ShowsThePlainBoxByDefault()
        {
            // Act & Assert
            WithDialog(Options(), static dialog =>
            {
                Assert.Equal(Visibility.Visible, dialog.InputBoxText.Visibility);
                Assert.Equal(Visibility.Collapsed, dialog.InputBoxPassword.Visibility);
            });
        }

        /// <summary>
        /// Verifies that asking for secure input swaps the boxes over.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheMaskedBoxForSecureInput()
        {
            // Arrange
            Hashtable table = Options();
            table["SecureInput"] = true;

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal(Visibility.Collapsed, dialog.InputBoxText.Visibility);
                Assert.Equal(Visibility.Visible, dialog.InputBoxPassword.Visibility);
            });
        }

        /// <summary>
        /// Verifies that text supplied up front is put in the box.
        /// </summary>
        [Fact]
        public void Constructor_StartsWithTheTextItWasGiven()
        {
            // Arrange
            Hashtable table = Options();
            table["InitialInputText"] = "server1.contoso.test";

            // Act & Assert
            WithDialog(table, static dialog => Assert.Equal("server1.contoso.test", dialog.InputBoxText.Text));
        }

        /// <summary>
        /// Verifies that an empty box leaves the accepting button unavailable.
        /// </summary>
        [Fact]
        public void Constructor_LeavesTheAcceptingButtonDisabledWhileTheBoxIsEmpty()
        {
            // Act & Assert
            WithDialog(Options(), static dialog => Assert.False(dialog.ButtonLeft.IsEnabled));
        }

        /// <summary>
        /// Verifies that text supplied up front makes the accepting button available immediately.
        /// </summary>
        [Fact]
        public void Constructor_EnablesTheAcceptingButtonWhenItStartsWithText()
        {
            // Arrange
            Hashtable table = Options();
            table["InitialInputText"] = "server1";

            // Act & Assert
            WithDialog(table, static dialog => Assert.True(dialog.ButtonLeft.IsEnabled));
        }

        /// <summary>
        /// Verifies that typing makes the accepting button available.
        /// </summary>
        [Fact]
        public void Typing_MakesTheAcceptingButtonAvailable()
        {
            // Act & Assert
            WithDialog(Options(), static dialog =>
            {
                Assert.False(dialog.ButtonLeft.IsEnabled);
                dialog.InputBoxText.Text = "server1";
                Assert.True(dialog.ButtonLeft.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that clearing the box takes the accepting button away again.
        /// </summary>
        /// <remarks>
        /// The state follows the box rather than latching once, so a user who types and then thinks
        /// better of it cannot submit the empty answer that is left.
        /// </remarks>
        /// <param name="cleared">What the user left in the box.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Typing_TakesTheAcceptingButtonAwayAgainWhenTheBoxIsEmptied(string cleared)
        {
            // Act & Assert
            WithDialog(Options(), dialog =>
            {
                dialog.InputBoxText.Text = "server1";
                dialog.InputBoxText.Text = cleared;
                Assert.False(dialog.ButtonLeft.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that the masked box drives the accepting button too.
        /// </summary>
        [Fact]
        public void Typing_MakesTheAcceptingButtonAvailableForSecureInput()
        {
            // Arrange
            Hashtable table = Options();
            table["SecureInput"] = true;

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.False(dialog.ButtonLeft.IsEnabled);
                dialog.InputBoxPassword.Password = "a secret";
                Assert.True(dialog.ButtonLeft.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that the accepting button is the one Enter activates and the dismissing one is Escape.
        /// </summary>
        [Fact]
        public void Constructor_WiresTheKeyboardToTheAcceptAndCancelButtons()
        {
            // Act & Assert
            WithDialog(Options(), static dialog =>
            {
                Assert.True(dialog.ButtonLeft.IsDefault);
                Assert.Equal(Fluence.Wpf.ControlAppearance.Accent, dialog.ButtonLeft.Appearance);
                Assert.True(dialog.ButtonRight.IsCancel);
            });
        }

        /// <summary>
        /// Verifies that the dialog starts out reporting a timeout with nothing typed.
        /// </summary>
        [Fact]
        public void Constructor_StartsOutReportingATimeout()
        {
            // Act & Assert
            WithDialog(Options(), static dialog => Assert.Equal(InputDialogResult.DefaultResult, dialog.DialogResult));
        }

        /// <summary>
        /// Verifies that accepting reports both which button was pressed and what was typed.
        /// </summary>
        [Fact]
        public void ButtonClick_ReportsTheCaptionAndWhatWasTyped()
        {
            // Act & Assert
            WithDialog(Options(), static dialog =>
            {
                dialog.InputBoxText.Text = "server1.contoso.test";
                FluentControls.Click(dialog.ButtonLeft);
                Assert.Equal(new InputDialogResult("Continue", "server1.contoso.test"), dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that dismissing still reports what was typed alongside the button.
        /// </summary>
        /// <remarks>
        /// Every button records the input, not only the accepting one, so a caller can tell a user who
        /// typed and then cancelled from one who cancelled straight away.
        /// </remarks>
        [Fact]
        public void ButtonClick_ReportsWhatWasTypedEvenWhenDismissing()
        {
            // Act & Assert
            WithDialog(Options(), static dialog =>
            {
                dialog.InputBoxText.Text = "half an answer";
                FluentControls.Click(dialog.ButtonRight);
                Assert.Equal(new InputDialogResult("Cancel", "half an answer"), dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that the answer comes from the masked box when the typing was masked.
        /// </summary>
        [Fact]
        public void ButtonClick_ReadsTheAnswerFromTheMaskedBoxForSecureInput()
        {
            // Arrange
            Hashtable table = Options();
            table["SecureInput"] = true;

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                dialog.InputBoxPassword.Password = "a secret";
                FluentControls.Click(dialog.ButtonLeft);
                Assert.Equal(new InputDialogResult("Continue", "a secret"), dialog.DialogResult);
            });
        }

        /// <summary>
        /// The sample options with an accepting and a dismissing button.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        private static Hashtable Options()
        {
            Hashtable table = SampleOptions.InputDialog();
            table["ButtonLeftText"] = "Continue";
            table["ButtonRightText"] = "Cancel";
            return table;
        }

        /// <summary>
        /// Builds an input dialog, runs a body against it and disposes it, all within the apartment.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="body">The assertions to run.</param>
        private static void WithDialog(Hashtable table, Action<InputDialog> body)
        {
            DialogHost.WithDialog(() => new InputDialog(new InputDialogOptions(table)), body);
        }
    }
}
