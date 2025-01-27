using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PSADT.UserInterface.Utilities;
using System.Windows.Interop;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Interaction logic for BaseDialog.xaml
    /// </summary>
    public abstract class BaseDialog : FluentWindow, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Timer? _timer;
        private bool _disposed = false;

        /// <summary>
        /// Constructor for BaseDialog
        /// </summary>
        /// <param name="dialogExpiryDuration"></param>
        protected BaseDialog(TimeSpan? dialogExpiryDuration)
        {
            DataContext = this;

            // Set up Cancellation Token and Timer
            _cancellationTokenSource = new CancellationTokenSource();
            if (null != dialogExpiryDuration)
            {
                _timer = new Timer(CloseDialog, null, (TimeSpan)dialogExpiryDuration, Timeout.InfiniteTimeSpan);
            }

            // Ensure WindowStartupLocation is Manual
            WindowStartupLocation = WindowStartupLocation.Manual;

            Loaded += BaseDialog_Loaded;

            SizeChanged += BaseDialog_SizeChanged;

        }

        /// <summary>
        /// CancellationToken for the dialog
        /// </summary>
        protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private void CloseDialog(object? state)
        {
            Dispatcher.Invoke(() =>
            {
                _cancellationTokenSource.Cancel();
                Close();
            });
        }

        /// <summary>
        /// Override OnSourceInitialized to position the window
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e)
        {

            base.OnSourceInitialized(e);

            // Force layout update
            UpdateLayout();

            // Position the window
            PositionWindow();
        }

        private void BaseDialog_Loaded(object sender, RoutedEventArgs e)
        {


        }

        private void BaseDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionWindow();

            WindowInteropHelper helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MOVE = 0xF010;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            switch (msg)
            {
                case WM_SYSCOMMAND:
                    int command = wParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
                    {
                        handled = true;
                    }
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Positions the window on the screen
        /// </summary>
        protected virtual void PositionWindow()
        {
            IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            WPFScreen screen = WPFScreen.FromHandle(windowHandle);

            // Get the working area in DIPs
            Rect workingArea = screen.GetWorkingAreaInDips(this);

            // Ensure layout is updated to get ActualWidth and ActualHeight
            double windowWidth = ActualWidth;
            double windowHeight = ActualHeight;

            // Calculate positions in DIPs
            double left = workingArea.Left + (workingArea.Width - windowWidth);
            double top = workingArea.Top + (workingArea.Height - windowHeight);

            // Apply a margin to prevent overlap
            const double margin = 4; // Adjust as needed
            left -= margin;
            top -= margin;

            // Ensure the window is within the screen bounds
            left = Math.Max(workingArea.Left, Math.Min(left, workingArea.Right - windowWidth));
            top = Math.Max(workingArea.Top, Math.Min(top, workingArea.Bottom - windowHeight));

            // Align positions to whole pixels
            left = Math.Floor(left);
            top = Math.Floor(top);

            // Debug output
            Debug.WriteLine($"Monitor: {screen.DeviceName}");
            Debug.WriteLine($"Working Area (DIPs): Left={workingArea.Left}, Top={workingArea.Top}, Right={workingArea.Right}, Bottom={workingArea.Bottom}");
            Debug.WriteLine($"Window Actual Width: {windowWidth}, Actual Height: {windowHeight}");
            Debug.WriteLine($"Calculated Left: {left}, Top: {top}");

            // Set positions in DIPs
            Left = left;
            Top = top;
        }

        /// <summary>
        /// Center the window on the screen
        /// </summary>
        protected virtual void CenterWindowOnScreen()
        {
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var screen = WPFScreen.FromHandle(windowHandle);

            // Get the bounds in DIPs
            var bounds = screen.GetBoundsInDips(this);

            // Calculate center positions
            double left = bounds.Left + ((bounds.Width - Width) / 2);
            double top = bounds.Top + ((bounds.Height - Height) / 2);

            // Set positions
            Left = left;
            Top = top;
        }

        /// <summary>
        /// Dispose of managed resources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Loaded -= BaseDialog_Loaded;
                SizeChanged -= BaseDialog_SizeChanged;
            }

            _disposed = true;
        }

        /// <summary>
        /// Dispose of managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destructor for BaseDialog
        /// </summary>
        ~BaseDialog()
        {
            Dispose(false);
        }
    }
}
