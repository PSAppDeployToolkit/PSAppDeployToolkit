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
            // Populate and show the list selection ComboBox.
            foreach (string item in options.ListItems)
            {
                _ = ListSelectionComboBox.Items.Add(item);
            }
            ListSelectionComboBox.SelectedIndex = 0;
            ListSelectionStackPanel.Visibility = Visibility.Visible;

            // Set the dialog result to a default value.
            DialogResult = new ListSelectionDialogResult("Timeout", null);
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonLeft.Content).Text.Replace("_", null), ListSelectionComboBox.SelectedItem as string);
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
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonMiddle.Content).Text.Replace("_", null), ListSelectionComboBox.SelectedItem as string);
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
            DialogResult = new ListSelectionDialogResult(((AccessText)ButtonRight.Content).Text.Replace("_", null), ListSelectionComboBox.SelectedItem as string);
            base.ButtonRight_Click(sender, e);
        }
    }
}
