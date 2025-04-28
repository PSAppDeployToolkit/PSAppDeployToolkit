using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's Input dialog.
    /// </summary>
    internal sealed class InputDialog : CustomDialog
    {
        /// <summary>
        /// Initializes the UI elements and behavior for the Input dialog type.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        internal InputDialog(InputDialogOptions options) : base (options, false)
        {
            // Enable input box within the dialog
            InputBoxStackPanel.Visibility = Visibility.Visible;
            InputBoxText.Text = options.InitialInputText;

            // Focus the input box initially
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                InputBoxText.Focus();
                InputBoxText.SelectAll();
            });
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            Result = new InputDialogResult(((AccessText)ButtonLeft.Content).Text.Replace("_", ""), InputBoxText.Text);
            base.Result = "Bypass";
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
            Result = new InputDialogResult(((AccessText)ButtonMiddle.Content).Text.Replace("_", ""), InputBoxText.Text);
            base.Result = "Bypass";
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
            Result = new InputDialogResult(((AccessText)ButtonRight.Content).Text.Replace("_", ""), InputBoxText.Text);
            base.Result = "Bypass";
            base.ButtonRight_Click(sender, e);
        }

        /// <summary>
        /// The result of the dialog interaction.
        /// </summary>
        internal new InputDialogResult Result
        {
            get => _result;
            private set
            {
                _result = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The cancellation token source for the dialog.
        /// </summary>
        private InputDialogResult _result = new InputDialogResult("Timeout", null);
    }
}
