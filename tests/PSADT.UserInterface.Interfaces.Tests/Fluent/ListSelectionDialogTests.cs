using System;
using System.Collections;
using System.Linq;
using System.Windows;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the Fluent dialog that asks the user to choose from a list.
    /// </summary>
    /// <remarks>
    /// Like the input dialog, this one has an answer that has to exist before it can be accepted - so
    /// the buttons that would accept it are unavailable until something is chosen. Unlike it, the
    /// deployment can choose on the user's behalf by nominating an index, and doing so makes the buttons
    /// available immediately.
    /// </remarks>
    public sealed class ListSelectionDialogTests
    {
        /// <summary>
        /// Verifies that the list section is shown.
        /// </summary>
        [Fact]
        public void Constructor_ShowsTheListSection()
        {
            // Act & Assert
            WithDialog(Options(), static dialog => Assert.Equal(Visibility.Visible, dialog.ListSelectionStackPanel.Visibility));
        }

        /// <summary>
        /// Verifies that the items offered are the ones the deployment supplied, in order.
        /// </summary>
        [Fact]
        public void Constructor_OffersTheItemsInOrder()
        {
            // Act & Assert
            WithDialog(Options("Personal", "Team", "Enterprise"), static dialog =>
                Assert.Equal(["Personal", "Team", "Enterprise"], dialog.ListSelectionComboBox.Items.Cast<string>(), StringComparer.Ordinal));
        }

        /// <summary>
        /// Verifies that the heading above the list is taken from the string table.
        /// </summary>
        [Fact]
        public void Constructor_TakesTheHeadingFromTheStringTable()
        {
            // Arrange
            Hashtable table = Options();
            SampleOptions.Nested(table, "Strings")["ListSelectionMessage"] = "Choose a licence type";

            // Act & Assert
            WithDialog(table, static dialog => Assert.Equal("Choose a licence type", dialog.ListSelectionHeadingTextBlock.Text));
        }

        /// <summary>
        /// Verifies that nothing is chosen to start with when the deployment did not choose.
        /// </summary>
        /// <remarks>
        /// Has to be cleared rather than simply left alone. A WPF item collection is itself a collection
        /// view, so adding the first item makes it current and the drop-down would otherwise open
        /// showing a selection nobody made - one the accepting buttons refuse, because they are disabled
        /// until the selection <i>changes</i>. A user who wanted the item already showing would have had
        /// to select something else and select back.
        /// </remarks>
        [Fact]
        public void Constructor_ChoosesNothingByDefault()
        {
            // Act & Assert
            WithDialog(Options("Personal", "Team"), static dialog =>
            {
                Assert.Equal(-1, dialog.ListSelectionComboBox.SelectedIndex);
                Assert.Null(dialog.ListSelectionComboBox.SelectedItem);
                Assert.False(dialog.ButtonLeft.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that the accepting buttons wait for a choice to be made.
        /// </summary>
        /// <remarks>
        /// The dismissing button stays available throughout - a user who does not want to choose has to
        /// be able to say so.
        /// </remarks>
        [Fact]
        public void Constructor_WaitsForAChoiceBeforeOfferingToAcceptOne()
        {
            // Act & Assert
            WithDialog(Options(), static dialog =>
            {
                Assert.False(dialog.ButtonLeft.IsEnabled);
                Assert.False(dialog.ButtonMiddle.IsEnabled);
                Assert.True(dialog.ButtonRight.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that making a choice makes the accepting buttons available.
        /// </summary>
        [Fact]
        public void Choosing_MakesTheAcceptingButtonsAvailable()
        {
            // Act & Assert
            WithDialog(Options(), static dialog =>
            {
                dialog.ListSelectionComboBox.SelectedIndex = 1;
                Assert.True(dialog.ButtonLeft.IsEnabled);
                Assert.True(dialog.ButtonMiddle.IsEnabled);
            });
        }

        /// <summary>
        /// Verifies that a choice made by the deployment is applied and needs no further action.
        /// </summary>
        /// <remarks>
        /// Nominating an index also skips wiring the handler that watches for a choice, on the grounds
        /// that one has already been made. That is why the buttons have to be available from the start
        /// in this case: nothing would ever enable them.
        /// </remarks>
        [Fact]
        public void Constructor_AppliesAChoiceTheDeploymentMade()
        {
            // Arrange
            Hashtable table = Options("Personal", "Team", "Enterprise");
            table["SelectedIndex"] = 2;

            // Act & Assert
            WithDialog(table, static dialog =>
            {
                Assert.Equal(2, dialog.ListSelectionComboBox.SelectedIndex);
                Assert.Equal("Enterprise", dialog.ListSelectionComboBox.SelectedItem);
                Assert.True(dialog.ButtonLeft.IsEnabled);
                Assert.True(dialog.ButtonMiddle.IsEnabled);
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
        /// Verifies that the dialog starts out reporting a timeout with nothing chosen.
        /// </summary>
        [Fact]
        public void Constructor_StartsOutReportingATimeout()
        {
            // Act & Assert
            WithDialog(Options(), static dialog => Assert.Equal(ListSelectionDialogResult.DefaultResult, dialog.DialogResult));
        }

        /// <summary>
        /// Verifies that accepting reports both which button was pressed and what was chosen.
        /// </summary>
        [Fact]
        public void ButtonClick_ReportsTheCaptionAndWhatWasChosen()
        {
            // Act & Assert
            WithDialog(Options("Personal", "Team", "Enterprise"), static dialog =>
            {
                dialog.ListSelectionComboBox.SelectedIndex = 1;
                FluentControls.Click(dialog.ButtonLeft);
                Assert.Equal(new ListSelectionDialogResult("OK", "Team"), dialog.DialogResult);
            });
        }

        /// <summary>
        /// Verifies that dismissing without choosing reports no choice.
        /// </summary>
        /// <remarks>
        /// The dismissing button is available from the start, and the answer it records reads whatever
        /// the drop-down is showing. That is only meaningful because nothing is showing until the user
        /// chooses: were the first item still selected on opening, a caller could not tell someone who
        /// chose it from someone who cancelled outright.
        /// </remarks>
        [Fact]
        public void ButtonClick_ReportsNoChoiceWhenDismissedWithoutOne()
        {
            // Act & Assert
            WithDialog(Options("Personal", "Team"), static dialog =>
            {
                FluentControls.Click(dialog.ButtonRight);
                Assert.Equal(new ListSelectionDialogResult("Cancel", selectedItem: null), dialog.DialogResult);
            });
        }

        /// <summary>
        /// The sample options with an accepting, a middle and a dismissing button.
        /// </summary>
        /// <param name="listItems">The items to offer, or none for a default set.</param>
        /// <returns>A new dictionary each call.</returns>
        private static Hashtable Options(params string[] listItems)
        {
            Hashtable table = SampleOptions.ListSelectionDialog(listItems);
            table["ButtonLeftText"] = "OK";
            table["ButtonMiddleText"] = "Apply";
            table["ButtonRightText"] = "Cancel";
            return table;
        }

        /// <summary>
        /// Builds a list selection dialog, runs a body against it and disposes it, within the apartment.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <param name="body">The assertions to run.</param>
        private static void WithDialog(Hashtable table, Action<ListSelectionDialog> body)
        {
            DialogHost.WithDialog(() => new ListSelectionDialog(new ListSelectionDialogOptions(table)), body);
        }
    }
}
