using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Navigation;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one.
    /// </summary>
    public partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Handles the loaded event of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FluentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayout();

            // Initialize countdown display if needed
            if ((_dialogType == DialogType.Restart || _dialogType == DialogType.CloseApps) && _countdownDuration.HasValue)
            {
                UpdateCountdownDisplay();
            }

            // Update row definitions based on current content
            UpdateRowDefinition();

            // Position the window
            PositionWindow();
        }

        /// <summary>
        /// Handles the size changed event of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FluentDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Only reposition window - no animations
            PositionWindow();

            // Add hook to prevent window movement
            WindowInteropHelper helper = new(this);
            HwndSource? source = HwndSource.FromHwnd(helper.Handle);
            if (source != null)
            {
                source.AddHook(new HwndSourceHook(WndProc));
            }
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                switch (DialogType)
                {
                    case DialogType.CloseApps:
                        if (_deferralsRemaining.HasValue && _deferralsRemaining > 0)
                        {
                            _deferralsRemaining--;
                            UpdateDeferralValues();
                        }
                        DialogResult = "Defer";
                        break;

                    case DialogType.Restart:
                        DialogResult = "Dismiss";
                        // Just minimize the window instead of closing
                        this.WindowState = WindowState.Minimized;
                        return; // Don't close the dialog

                    case DialogType.Input:
                        DialogResult = (ButtonLeft.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonLeft"; // Store button text as result
                        _inputTextResult = InputBoxText.Text; // Capture input text
                        break;

                    case DialogType.Custom:
                    default:
                        DialogResult = (ButtonLeft.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonLeft"; // Store button text as result
                        break;
                }

                // Only close if not minimizing (Restart dialog)
                CloseDialog(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ButtonLeft_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                DialogResult = (ButtonMiddle.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonMiddle"; // Store button text as result
                if (DialogType == DialogType.Input)
                {
                    _inputTextResult = InputBoxText.Text; // Capture input text
                }
                CloseDialog(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ButtonMiddle_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                switch (DialogType)
                {
                    case DialogType.CloseApps:
                        DialogResult = "Continue";
                        break;

                    case DialogType.Restart:
                        DialogResult = "Restart";
                        break;

                    case DialogType.Input:
                        DialogResult = (ButtonRight.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonRight"; // Store button text as result
                        _inputTextResult = InputBoxText.Text; // Capture input text
                        break;

                    case DialogType.Custom:
                    default:
                        DialogResult = (ButtonRight.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonRight"; // Store button text as result
                        break;
                }

                CloseDialog(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ButtonRight_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Prevents the user from closing the app via the taskbar
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isDisposed)
                return;

            e.Cancel = !_canClose; // Prevent the window from closing unless explicitly allowed in code
                                   // This is to prevent the user from closing the dialog via taskbar
        }

        /// <summary>
        /// Clean up resources when the window is closed
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }

        /// <summary>
        /// Handles the request navigate event of the hyperlink.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Use ShellExecute to open the URL in the default browser/handler
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Log or handle the error (e.g., show a message box)
                Debug.WriteLine($"Could not open hyperlink: {e.Uri}. Error: {ex.Message}");
            }
            e.Handled = true; // Mark the event as handled
        }
    }
}
