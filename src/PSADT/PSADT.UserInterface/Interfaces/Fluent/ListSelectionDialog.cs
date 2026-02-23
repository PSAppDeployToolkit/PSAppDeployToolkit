using System.Windows;
using System.Windows.Controls;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;

namespace PSADT.UserInterface.Interfaces.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's List Selection dialog.
    /// </summary>
    internal sealed class ListSelectionDialog : CustomDialog, IModalDialog
    {
        /// <summary>
        /// Initializes the UI elements and behavior for the List Selection dialog type.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        internal ListSelectionDialog(ListSelectionDialogOptions options) : base(options)
        {
            // Enable the ListSelectionStackPanel within the dialog
            ListSelectionStackPanel.Visibility = Visibility.Visible;

            // Set up UI
            SetDefaultButton(ButtonLeft);
            SetAccentButton(ButtonLeft);
            SetCancelButton(ButtonRight);

            // Populate and show the List Selection ComboBox.
            foreach (string item in options.ListItems)
            {
                _ = ListSelectionComboBox.Items.Add(item);
            }

            // Disable all buttons until an item is selected.
            if (!options.SelectedIndex.HasValue)
            {
                ListSelectionComboBox.SelectionChanged += (sender, e) =>
                {
                    ButtonLeft.IsEnabled = ListSelectionComboBox.SelectedIndex >= 0;
                    ButtonMiddle.IsEnabled = ListSelectionComboBox.SelectedIndex >= 0;
                    ButtonRight.IsEnabled = ListSelectionComboBox.SelectedIndex >= 0;
                };
                ButtonLeft.IsEnabled = false;
                ButtonMiddle.IsEnabled = false;
                ButtonRight.IsEnabled = false;
            }
            else
            {
                ListSelectionComboBox.SelectedIndex = options.SelectedIndex.Value;
            }
            _ = ListSelectionComboBox.Focus();

            // Set heading text from localized strings if available.
            ListSelectionHeadingTextBlock.Text = options.Strings.ListSelectionMessage;

            // Set the dialog result to a default value.
            DialogResult = new ListSelectionDialogResult("Timeout", "\0");
        }

        /// <summary>
        /// Handles the click event for the left button, setting the dialog result based on the selected item and the
        /// button's content.
        /// </summary>
        /// <remarks>This method sets the dialog result to a new instance of ListSelectionDialogResult,
        /// which includes the text of the button and the selected item from the ListSelectionComboBox. It then calls
        /// the base class implementation to handle the window closure.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonLeft.Content).Text.Replace("_", null), (string)ListSelectionComboBox.SelectedItem);
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the middle button, setting the dialog result based on the selected item and the
        /// button's content.
        /// </summary>
        /// <remarks>This method sets the dialog result to a new instance of ListSelectionDialogResult,
        /// which includes the button's text and the selected item from the combo box. It then calls the base class
        /// implementation to handle any additional processing required for closing the dialog.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonMiddle.Content).Text.Replace("_", null), (string)ListSelectionComboBox.SelectedItem);
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the right button, setting the dialog result based on the selected item and the
        /// button's content.
        /// </summary>
        /// <remarks>This method sets the dialog result to a new instance of ListSelectionDialogResult,
        /// which includes the button's text and the selected item from the combo box. It then calls the base class
        /// implementation to handle the window closure.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonRight.Content).Text.Replace("_", null), (string)ListSelectionComboBox.SelectedItem);
            base.ButtonRight_Click(sender, e);
        }
    }
}
