using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one.
    /// </summary>
    public partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes the UI elements and behavior for the Custom dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="customMessage">The main message text to display.</param>
        /// <param name="ButtonLeftText">Text for the left button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonMiddleText">Text for the middle button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonRightText">Text for the right button. If null or empty, the button is hidden.</param>
        public void InitializeCustomDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? customMessage,
            string? ButtonLeftText,
            string? ButtonMiddleText,
            string? ButtonRightText)
        {
            // Set basic properties
            Title = appTitle ?? "Message";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Message Dialog");

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, customMessage ?? string.Empty); // Use helper method
            CustomMessageTextBlock.Visibility = Visibility.Collapsed;
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed; // Ensure hidden by default
            DeferStackPanel.Visibility = Visibility.Collapsed;
            CountdownStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons based on provided texts
            SetButtonContentWithAccelerator(ButtonLeft, ButtonLeftText ?? "_OK");
            ButtonLeft.Visibility = string.IsNullOrWhiteSpace(ButtonLeftText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonLeft, ButtonLeftText ?? "OK");

            SetButtonContentWithAccelerator(ButtonMiddle, ButtonMiddleText ?? "_Cancel");
            ButtonMiddle.Visibility = string.IsNullOrWhiteSpace(ButtonMiddleText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonMiddle, ButtonMiddleText ?? "Cancel");

            SetButtonContentWithAccelerator(ButtonRight, ButtonRightText ?? "_Continue");
            ButtonRight.Visibility = string.IsNullOrWhiteSpace(ButtonRightText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonRight, ButtonRightText ?? "Continue");

            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Focus the default button
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (ButtonRight.Visibility == Visibility.Visible)
                    ButtonRight.Focus();
                else if (ButtonLeft.Visibility == Visibility.Visible)
                    ButtonLeft.Focus();
                else if (ButtonMiddle.Visibility == Visibility.Visible)
                    ButtonMiddle.Focus();
            });
        }
    }
}
