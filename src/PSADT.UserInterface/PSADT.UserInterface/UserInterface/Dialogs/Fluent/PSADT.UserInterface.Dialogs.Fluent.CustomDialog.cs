using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's Custom dialog.
    /// </summary>
    public class CustomDialog : FluentDialog
    {
        /// <summary>
        /// Instantiates a new Custom dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        public CustomDialog(CustomDialogOptions options, bool setFocus = true) : base(options)
        {
            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, options.MessageText);
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons based on provided texts
            if (null != options.ButtonLeftText)
            {
                SetButtonContentWithAccelerator(ButtonLeft, options.ButtonLeftText);
                ButtonLeft.Visibility = Visibility.Visible;
                AutomationProperties.SetName(ButtonLeft, options.ButtonLeftText);
            }
            if (null != options.ButtonMiddleText)
            {
                SetButtonContentWithAccelerator(ButtonMiddle, options.ButtonMiddleText);
                ButtonMiddle.Visibility = Visibility.Visible;
                AutomationProperties.SetName(ButtonMiddle, options.ButtonMiddleText);
            }
            if (null != options.ButtonRightText)
            {
                SetButtonContentWithAccelerator(ButtonRight, options.ButtonRightText);
                ButtonRight.Visibility = Visibility.Visible;
                AutomationProperties.SetName(ButtonRight, options.ButtonRightText);
            }

            // Focus the default button
            if (setFocus)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
                {
                    if (ButtonRight.Visibility == Visibility.Visible)
                    {
                        ButtonRight.Focus();
                    }
                    else if (ButtonLeft.Visibility == Visibility.Visible)
                    {
                        ButtonLeft.Focus();
                    }
                    else if (ButtonMiddle.Visibility == Visibility.Visible)
                    {
                        ButtonMiddle.Focus();
                    }
                });
            }
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            if (this.DialogResult != "Bypass")
            {
                DialogResult = ((AccessText)ButtonLeft.Content).Text.Replace("_", "");
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
            // Set the result and call base method to handle window closure.
            if (this.DialogResult != "Bypass")
            {
                DialogResult = ((AccessText)ButtonMiddle.Content).Text.Replace("_", "");
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
            // Set the result and call base method to handle window closure.
            if (this.DialogResult != "Bypass")
            {
                DialogResult = ((AccessText)ButtonRight.Content).Text.Replace("_", "");
            }
            base.ButtonRight_Click(sender, e);
        }
    }
}
