using System;
using System.Security;
using System.Windows;
using System.Windows.Controls;
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
        internal InputDialog(InputDialogOptions options) : base(options, DefaultResultFor(options))
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
                InputBoxPassword.PasswordChanged += OnInputChanged;
                Loaded += static (sender, __) =>
                {
                    if (sender is not InputDialog dialog)
                    {
                        throw new InvalidProgramException("Unexpected Loaded event sender type. Expected InputDialog.");
                    }
                    _ = dialog.InputBoxPassword.Focus();
                    dialog.InputBoxPassword.SelectAll();
                };
            }
            else
            {
                InputBoxText.Text = options.InitialInputText;
                InputBoxText.TextChanged += OnTextInputChanged;
                Loaded += static (sender, __) =>
                {
                    if (sender is not InputDialog dialog)
                    {
                        throw new InvalidProgramException("Unexpected Loaded event sender type. Expected InputDialog.");
                    }
                    _ = dialog.InputBoxText.Focus();
                    dialog.InputBoxText.SelectAll();
                };
            }
            UpdateContinueButtonState();
        }

        /// <summary>
        /// Event handler for changes in the text input, triggered when the user modifies the text in the input box. This method updates the state of the continue button based on the current input value, enabling it only when the input is not null, empty, or whitespace.
        /// </summary>
        /// <param name="sender">The source of the event, typically the input control that was modified.</param>
        /// <param name="e">The event data associated with the text change event.</param>
        private void OnTextInputChanged(object sender, TextChangedEventArgs e)
        {
            UpdateContinueButtonState();
        }

        /// <summary>
        /// Event handler for changes in the input value, triggered when the user modifies the text in the input box. This method updates the state of the continue button based on the current input value, enabling it only when the input is not null, empty, or whitespace.
        /// </summary>
        /// <param name="sender">The source of the event, typically the input control that was modified.</param>
        /// <param name="e">The event data associated with the input change event.</param>
        private void OnInputChanged(object? sender, EventArgs e)
        {
            UpdateContinueButtonState();
        }

        /// <summary>
        /// Enables or disables the continue button based on whether the current input value is null, empty, or consists solely of whitespace.
        /// </summary>
        /// <remarks>Masked input is tested by length alone. Reading the box as a string to test it for whitespace
        /// would leave an unzeroable copy of every prefix the user typed on the managed heap.</remarks>
        private void UpdateContinueButtonState()
        {
            if (_secureInput)
            {
                using SecureString value = InputBoxPassword.SecurePassword;
                ButtonLeft.IsEnabled = value.Length > 0;
            }
            else
            {
                ButtonLeft.IsEnabled = !string.IsNullOrWhiteSpace(InputBoxText.Text);
            }
        }

        /// <summary>
        /// Handles the click event for the left button, setting the dialog result based on the button's content and the
        /// current input value.
        /// </summary>
        /// <remarks>This method replaces underscores in the button's content text with null and sets the
        /// dialog result before calling the base method to handle window closure.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonLeft_Click(object? sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = CreateResult(ButtonLeft);
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
        private protected override void ButtonMiddle_Click(object? sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = CreateResult(ButtonMiddle);
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
        private protected override void ButtonRight_Click(object? sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = CreateResult(ButtonRight);
            base.ButtonRight_Click(sender, e);
        }

        /// <summary>
        /// Builds the dialog result for the specified button, reading the answer from whichever input box is in use.
        /// </summary>
        /// <remarks>Masked input is taken as a <see cref="SecureString"/> so the value never becomes an
        /// immutable string the process cannot subsequently zero.</remarks>
        /// <param name="button">The button that was clicked.</param>
        /// <returns>A result pairing the button's caption with what the user entered.</returns>
        private CustomDialogDerivative CreateResult(Fluence.Wpf.Controls.Button button)
        {
            string caption = ((AccessText)button.Content).Text.Replace("_", newValue: null, StringComparison.Ordinal);
            return _secureInput ? new SecureInputDialogResult(caption, InputBoxPassword.SecurePassword) : new InputDialogResult(caption, InputBoxText.Text);
        }

        /// <summary>
        /// Gets the timeout result matching the kind of answer the supplied options ask for.
        /// </summary>
        /// <param name="options">The options the dialog is being constructed with.</param>
        /// <returns>The default result of whichever type this dialog will report.</returns>
        private static CustomDialogDerivative DefaultResultFor(InputDialogOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return options.SecureInput ? SecureInputDialogResult.DefaultResult : InputDialogResult.DefaultResult;
        }

        /// <summary>
        /// Indicates whether the input box is in secure input mode.
        /// </summary>
        private readonly bool _secureInput;
    }
}
