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
        /// Initializes the UI elements and behavior for the Input dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="customMessage">The message text displayed above the input box.</param>
        /// <param name="initialInputText">The initial text pre-filled in the input box.</param>
        /// <param name="ButtonLeftText">Text for the left button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonMiddleText">Text for the middle button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonRightText">Text for the right button. If null or empty, the button is hidden.</param>
        public void InitializeInputDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? customMessage, // Renamed from inputBoxTextBlock for consistency
            string? initialInputText, // Renamed from inputBoxText
            string? ButtonLeftText,
            string? ButtonMiddleText,
            string? ButtonRightText)
        {
            // Set basic properties
            Title = appTitle ?? "Input Required";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Input Dialog");

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, customMessage ?? "Please enter a value:"); // Use helper method
            CustomMessageTextBlock.Visibility = Visibility.Collapsed; // Hide the custom message block
            InputBoxText.Text = initialInputText ?? string.Empty; // Set initial text
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Visible; // Show the input controls
            DeferStackPanel.Visibility = Visibility.Collapsed;
            CountdownStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons based on provided texts
            SetButtonContentWithAccelerator(ButtonLeft, ButtonLeftText ?? "_Cancel"); // Default Left to Cancel
            ButtonLeft.Visibility = string.IsNullOrWhiteSpace(ButtonLeftText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonLeft, ButtonLeftText ?? "Cancel");

            SetButtonContentWithAccelerator(ButtonMiddle, ButtonMiddleText); // No default for Middle
            ButtonMiddle.Visibility = string.IsNullOrWhiteSpace(ButtonMiddleText) ? Visibility.Collapsed : Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(ButtonMiddleText)) AutomationProperties.SetName(ButtonMiddle, ButtonMiddleText);


            SetButtonContentWithAccelerator(ButtonRight, ButtonRightText ?? "_OK"); // Default Right to OK
            ButtonRight.Visibility = string.IsNullOrWhiteSpace(ButtonRightText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonRight, ButtonRightText ?? "OK");

            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Focus the input box initially
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                InputBoxText.Focus();
                InputBoxText.SelectAll();
            });
        }
    }
}
