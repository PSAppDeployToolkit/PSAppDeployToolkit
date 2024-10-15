using PSADT.UserInterface.Utilities;
using Wpf.Ui.Markup;

namespace PSADT.UserInterface
{
    public class AdtApplication
    {
        private Application _app = null!;
        private Exception? _startupException;
        private Window? _currentWindow;

        public AdtApplication()
        {
            ManualResetEvent loadedEvent = new(false);

            Thread appThread = new(() =>
            {
                try
                {
                    _app = new();
                    _app.Resources.MergedDictionaries.Add(new ControlsDictionary());
                    _app.Resources.MergedDictionaries.Add(new ThemesDictionary());
                    _app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    _app.Startup += (o, e) => { loadedEvent.Set(); };
                    _app.Run();
                }
                catch (Exception ex)
                {
                    _startupException = ex;
                    loadedEvent.Set();
                }
            });
            appThread.SetApartmentState(ApartmentState.STA);
            appThread.Start();

            loadedEvent.WaitOne();

            if (_startupException != null)
            {
                throw _startupException;
            }
        }

        public Window? CurrentWindow => _currentWindow;



        public void ShowWelcomeDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            int? defersRemaining,
            List<AppProcessInfo>? appsToClose,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string closeAppMessage,
            string? buttonLeftText,
            string? buttonRightText)
        {
            if (_app == null)
            {
                throw new ArgumentNullException(nameof(_app));
            }

            _app.Dispatcher.Invoke(() =>
            {
                _currentWindow = new WelcomeDialog(appTitle, subtitle, topMost, defersRemaining, appsToClose, appIconImage, bannerImageLight, bannerImageDark, closeAppMessage, buttonLeftText, buttonRightText);
                _currentWindow.Show();


            });
        }

        public void ShowProgressDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string? progressMessage,
            string? progressMessageDetail)
        {
            if (_app == null)
            {
                throw new ArgumentNullException(nameof(_app));
            }

            _app.Dispatcher.Invoke(() =>
            {
                _currentWindow = new ProgressDialog(appTitle, subtitle, topMost, appIconImage, bannerImageLight, bannerImageDark, progressMessage, progressMessageDetail);
                _currentWindow.Show();
            });
        }

        public void ShowCustomDialog(string? title, string? logo, string? customMessage, string button1,
            string button2, string button3)
        {
            if (_app == null)
            {
                throw new ArgumentNullException(nameof(_app));
            }

            _app.Dispatcher.Invoke(() =>
            {
                _currentWindow = new CustomDialog(title, logo, customMessage, button1, button2, button3);
                _currentWindow.Show();
            });
        }

        public void CloseCurrentDialog()
        {
            if (_app == null)
            {
                throw new ArgumentNullException(nameof(_app));
            }

            if (_currentWindow == null)
            {
                throw new ArgumentNullException(nameof(_currentWindow));
            }

            _app.Dispatcher.Invoke(() =>
            {
                _currentWindow.Close();
                _currentWindow = null;
            });
        }


    }
}