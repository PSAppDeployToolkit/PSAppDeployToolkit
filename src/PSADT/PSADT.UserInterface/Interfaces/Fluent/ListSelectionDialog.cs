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
            ListSelectionComboBox.SelectedIndex = options.SelectedIndex;
            _ = ListSelectionComboBox.Focus();

            // Set heading text from localized strings if available.
            ListSelectionHeadingTextBlock.Text = options.Strings.ListSelectionMessage;

            // Set the dialog result to a default value.
            DialogResult = new ListSelectionDialogResult("Timeout", "\0");
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonLeft.Content).Text.Replace("_", null), (string)ListSelectionComboBox.SelectedItem);
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonMiddle.Content).Text.Replace("_", null), (string)ListSelectionComboBox.SelectedItem);
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonRight.Content).Text.Replace("_", null), (string)ListSelectionComboBox.SelectedItem);
            base.ButtonRight_Click(sender, e);
        }
    }
}
