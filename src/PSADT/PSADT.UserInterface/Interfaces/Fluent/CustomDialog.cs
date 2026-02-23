using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;

namespace PSADT.UserInterface.Interfaces.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's Custom dialog.
    /// </summary>
    internal class CustomDialog : FluentDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the CustomDialog class using the specified dialog options.
        /// </summary>
        /// <remarks>This constructor sets the default dialog result to "Timeout".</remarks>
        /// <param name="options">The options that configure the behavior and appearance of the dialog.</param>
        internal CustomDialog(CustomDialogOptions options) : this(options, new CustomDialogResult("Timeout"))
        {
        }

        /// <summary>
        /// Instantiates a new Custom dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        /// <param name="dialogResult">An object to store the dialog result in.</param>
        private protected CustomDialog(CustomDialogOptions options, CustomDialogResult dialogResult) : base(options, dialogResult)
        {
            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, options.MessageText);
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons based on provided texts
            if (options.ButtonLeftText is not null)
            {
                SetButtonContentWithAccelerator(ButtonLeft, options.ButtonLeftText);
                ButtonLeft.Visibility = Visibility.Visible;
                AutomationProperties.SetName(ButtonLeft, options.ButtonLeftText);
            }
            if (options.ButtonMiddleText is not null)
            {
                SetButtonContentWithAccelerator(ButtonMiddle, options.ButtonMiddleText);
                ButtonMiddle.Visibility = Visibility.Visible;
                AutomationProperties.SetName(ButtonMiddle, options.ButtonMiddleText);
            }
            if (options.ButtonRightText is not null)
            {
                SetButtonContentWithAccelerator(ButtonRight, options.ButtonRightText);
                ButtonRight.Visibility = Visibility.Visible;
                AutomationProperties.SetName(ButtonRight, options.ButtonRightText);
            }
        }

        /// <summary>
        /// Handles the click event for the left button, updating the dialog result if it is still set to the default
        /// value.
        /// </summary>
        /// <remarks>This method checks if the DialogResult is still set to the default 'Timeout' value
        /// before updating it with the button's text. It ensures that derived classes can set the DialogResult without
        /// interference.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Only set DialogResult if it hasn't been set by a derived class (still has default "Timeout" value).
            if (DialogResult is CustomDialogResult result && result.Equals("Timeout"))
            {
                DialogResult = new CustomDialogResult(((AccessText)ButtonLeft.Content).Text.Replace("_", null));
            }
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the middle button in the dialog, updating the dialog result if it has not
        /// already been set by a derived class.
        /// </summary>
        /// <remarks>If the dialog result is still set to its default value, this method updates it based
        /// on the button's displayed text. Derived classes can override this behavior by setting the dialog result
        /// before this method is called.</remarks>
        /// <param name="sender">The source of the event, typically the middle button that was clicked.</param>
        /// <param name="e">The event data associated with the button click.</param>
        private protected override void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            // Only set DialogResult if it hasn't been set by a derived class (still has default "Timeout" value).
            if (DialogResult is CustomDialogResult result && result.Equals("Timeout"))
            {
                DialogResult = new CustomDialogResult(((AccessText)ButtonMiddle.Content).Text.Replace("_", null));
            }
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the right button in the dialog, updating the dialog result if it has not already
        /// been set by a derived class.
        /// </summary>
        /// <remarks>If the dialog result is still set to its default value, this method assigns it based
        /// on the right button's content. Derived classes can override this behavior by setting the dialog result
        /// before this method is called.</remarks>
        /// <param name="sender">The source of the event, typically the right button that was clicked.</param>
        /// <param name="e">The event data associated with the button click.</param>
        private protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Only set DialogResult if it hasn't been set by a derived class (still has default "Timeout" value).
            if (DialogResult is CustomDialogResult result && result.Equals("Timeout"))
            {
                DialogResult = new CustomDialogResult(((AccessText)ButtonRight.Content).Text.Replace("_", null));
            }
            base.ButtonRight_Click(sender, e);
        }
    }
}
