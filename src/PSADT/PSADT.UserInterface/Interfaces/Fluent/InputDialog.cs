using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;

namespace PSADT.UserInterface.Interfaces.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's Input dialog.
    /// </summary>
    internal sealed class InputDialog : CustomDialog, IModalDialog
    {
        /// <summary>
        /// Initializes the UI elements and behavior for the Input dialog type.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        internal InputDialog(InputDialogOptions options) : base(options)
        {
            // Enable input box within the dialog
            InputBoxStackPanel.Visibility = Visibility.Visible;
            SetDefaultButton(ButtonLeft);
            SetAccentButton(ButtonLeft);
            SetCancelButton(ButtonRight);

            // Configure based on secure input mode.
            if (_secureInput = options.SecureInput)
            {
                InputBoxText.Visibility = Visibility.Collapsed;
                InputBoxPassword.Visibility = Visibility.Visible;
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
                {
                    _ = InputBoxPassword.Focus();
                    InputBoxPassword.SelectAll();
                });
            }
            else
            {
                InputBoxText.Text = options.InitialInputText;
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
                {
                    _ = InputBoxText.Focus();
                    InputBoxText.SelectAll();
                });
            }

            // Set the dialog result to a default value.
            DialogResult = new InputDialogResult("Timeout", null);
        }

        /// <summary>
        /// Handles the click event for the left button, setting the dialog result based on the button's content and the
        /// current input value.
        /// </summary>
        /// <remarks>This method replaces underscores in the button's content text with null and sets the
        /// dialog result before calling the base method to handle window closure.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new InputDialogResult(((AccessText)ButtonLeft.Content).Text.Replace("_", null), CurrentInputValue);
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the middle button, setting the dialog result based on the button's content and
        /// the current input value.
        /// </summary>
        /// <remarks>This method replaces underscores in the button's content text with null before
        /// setting the dialog result. It also calls the base class implementation to handle window closure.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new InputDialogResult(((AccessText)ButtonMiddle.Content).Text.Replace("_", null), CurrentInputValue);
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the right button in the input dialog, setting the dialog result based on the
        /// button's content and the current input value.
        /// </summary>
        /// <remarks>This method overrides the base implementation to assign a new dialog result using the
        /// current input value and the button's displayed text. It then calls the base method to ensure standard window
        /// closure behavior.</remarks>
        /// <param name="sender">The source of the event, typically the right button that was clicked.</param>
        /// <param name="e">The event data associated with the button click.</param>
        private protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new InputDialogResult(((AccessText)ButtonRight.Content).Text.Replace("_", null), CurrentInputValue);
            base.ButtonRight_Click(sender, e);
        }

        /// <summary>
        /// Gets the current input value from either the TextBox or PasswordBox.
        /// </summary>
        private string? CurrentInputValue => _secureInput ? InputBoxPassword.Password : InputBoxText.Text;

        /// <summary>
        /// Indicates whether the input box is in secure input mode.
        /// </summary>
        private readonly bool _secureInput;
    }
}
