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
        protected CustomDialog(CustomDialogOptions options, CustomDialogResult dialogResult) : base(options, dialogResult)
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
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Only set DialogResult if it hasn't been set by a derived class (still has default "Timeout" value).
            if (DialogResult is CustomDialogResult result && result.Equals("Timeout"))
            {
                DialogResult = new CustomDialogResult(((AccessText)ButtonLeft.Content).Text.Replace("_", null));
            }
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            // Only set DialogResult if it hasn't been set by a derived class (still has default "Timeout" value).
            if (DialogResult is CustomDialogResult result && result.Equals("Timeout"))
            {
                DialogResult = new CustomDialogResult(((AccessText)ButtonMiddle.Content).Text.Replace("_", null));
            }
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
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
