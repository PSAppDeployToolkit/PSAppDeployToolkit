using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one.
    /// </summary>
    internal abstract partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }
            CloseDialog(null);
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }
            CloseDialog(null);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }
            CloseDialog(null);
        }

        /// <summary>
        /// Prevents the user from closing the app via the taskbar
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            // Prevent the window from closing unless explicitly allowed in code
            // This is to prevent the user from closing the dialog via taskbar
            e.Cancel = !_canClose;
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
        /// Handles the loaded event of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FluentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Update dialog layout
            UpdateButtonLayout();
            UpdateLayout();

            // Initialize countdown display if needed
            if (_countdownDuration.HasValue)
            {
                //UpdateCountdownDisplay();
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
        /// Handles the request navigate event of the hyperlink.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Use ShellExecute to open the URL in the default browser/handler
            if (_disposed)
            {
                return;
            }
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
