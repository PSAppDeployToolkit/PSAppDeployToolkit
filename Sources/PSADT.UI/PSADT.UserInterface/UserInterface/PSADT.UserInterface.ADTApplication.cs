using System.Windows;
using System.Windows.Threading;
using PSADT.UserInterface.Services;
using Wpf.Ui.Markup;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Helper class to manage WPF dialogs within a console application.
    /// </summary>
    public class AdtApplication : IDisposable
    {
        private readonly Thread _appThread;
        private Application? _app;
        private Window? _currentWindow;
        private readonly ManualResetEvent _initEvent = new(false);
        private Exception? _startupException;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdtApplication"/> class.
        /// </summary>
        public AdtApplication()
        {
            _appThread = new Thread(InitializeApplication)
            {
                IsBackground = true
            };

            // Set the apartment state to STA before starting the thread
            _appThread.SetApartmentState(ApartmentState.STA);
            _appThread.Start();

            // Wait until the application is initialized
            _initEvent.WaitOne();

            if (_startupException != null)
            {
                throw new InvalidOperationException("Failed to initialize WPF Application.", _startupException);
            }
        }

        /// <summary>
        /// Initializes the WPF Application on a separate STA thread.
        /// </summary>
        private void InitializeApplication()
        {
            try
            {
                _app = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };

                _app.Resources.MergedDictionaries.Add(new ControlsDictionary());
                _app.Resources.MergedDictionaries.Add(new ThemesDictionary());

                // Signal that the application is ready
                _initEvent.Set();

                // Start the Dispatcher processing
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                _startupException = ex;
                _initEvent.Set();
            }
        }

        /// <summary>
        /// Shows the WelcomeDialog synchronously and returns the user's response.
        /// </summary>
        /// <param name="appTitle">Title of the application.</param>
        /// <param name="subtitle">Subtitle of the application.</param>
        /// <param name="topMost">Whether the dialog should be topmost.</param>
        /// <param name="defersRemaining">Number of defers remaining.</param>
        /// <param name="appsToClose">List of applications to close.</param>
        /// <param name="appIconImage">URI of the application icon.</param>
        /// <param name="bannerImageLight">URI of the light banner image.</param>
        /// <param name="bannerImageDark">URI of the dark banner image.</param>
        /// <param name="closeAppMessage">Message prompting users to close apps.</param>
        /// <param name="deferRemainText">Text for the word remain in, deferrals remaining.</param>
        /// <param name="deferButtonText">Text for the defer button.</param>
        /// <param name="continueButtonText">Text for the continue button.</param>
        /// <param name="processEvaluationService">Optional process evaluation service.</param>
        /// <returns>User's response as a string.</returns>
        public string ShowWelcomeDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            int? defersRemaining,
            List<AppProcessInfo>? appsToClose,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string closeAppMessage,
            string? deferRemainText,
            string? deferButtonText,
            string? continueButtonText,
            IProcessEvaluationService? processEvaluationService = null)
        {
            if (_app == null)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app.Dispatcher.Invoke(() =>
            {
                var welcomeDialog = new WelcomeDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    defersRemaining,
                    appsToClose,
                    appIconImage,
                    bannerImageLight,
                    bannerImageDark,
                    closeAppMessage,
                    deferRemainText,
                    deferButtonText,
                    continueButtonText,
                    processEvaluationService);

                // Show the dialog modally
                bool? dialogResult = welcomeDialog.ShowDialog();
                result = welcomeDialog.Result ?? "Cancel";
            });

            return result;
        }

        /// <summary>
        /// _currentWindow = welcomeDialog;
        /// Shows the ProgressDialog synchronously.
        /// </summary>
        /// <param name="appTitle">Title of the application.</param>
        /// <param name="subtitle">Subtitle of the application.</param>
        /// <param name="topMost">Whether the dialog should be topmost.</param>
        /// <param name="appIconImage">URI of the application icon.</param>
        /// <param name="bannerImageLight">URI of the light banner image.</param>
        /// <param name="bannerImageDark">URI of the dark banner image.</param>
        /// <param name="progressMessage">Main progress message.</param>
        /// <param name="progressMessageDetail">Detailed progress message.</param>
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
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app.Dispatcher.Invoke(() =>
            {
                var progressDialog = new ProgressDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    appIconImage,
                    bannerImageLight,
                    bannerImageDark,
                    progressMessage,
                    progressMessageDetail);

                _currentWindow = progressDialog;

                // Show the dialog non-modally
                progressDialog.Show();
            });
        }

        /// <summary>
        /// Shows the RestartDialog synchronously and returns the user's response.
        /// </summary>
        /// <param name="appTitle">Title of the application.</param>
        /// <param name="subtitle">Subtitle of the application.</param>
        /// <param name="topMost">Whether the dialog should be topmost.</param>
        /// <param name="appIconImage">URI of the application icon.</param>
        /// <param name="bannerImageLight">URI of the light banner image.</param>
        /// <param name="bannerImageDark">URI of the dark banner image.</param>
        /// <param name="customMessage">Message prompting users to close apps.</param>
        /// <param name="button1Text">Text for the word remain in, deferrals remaining.</param>
        /// <param name="button2Text">Text for the word remain in, deferrals remaining.</param>
        /// <param name="button3Text">Text for the word remain in, deferrals remaining.</param>
        /// <returns>User's response as a string.</returns>
        public string ShowCustomDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string customMessage,
            string? button1Text,
            string? button2Text,
            string? button3Text)
        {
            if (_app == null)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app.Dispatcher.Invoke(() =>
            {
                var customDialog = new CustomDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    appIconImage,
                    bannerImageLight,
                    bannerImageDark,
                    customMessage,
                    button1Text,
                    button2Text,
                    button3Text);

                _currentWindow = customDialog;

                // Show the dialog modally
                bool? dialogResult = customDialog.ShowDialog();
                result = customDialog.Result ?? "Cancel";
            });

            return result;
        }

        /// <summary>
        /// Shows the RestartDialog synchronously and returns the user's response.
        /// </summary>
        /// <param name="appTitle">Title of the application.</param>
        /// <param name="subtitle">Subtitle of the application.</param>
        /// <param name="topMost">Whether the dialog should be topmost.</param>
        /// <param name="appIconImage">URI of the application icon.</param>
        /// <param name="bannerImageLight">URI of the light banner image.</param>
        /// <param name="bannerImageDark">URI of the dark banner image.</param>
        /// <param name="restartCountdownMins">Message prompting users to close apps.</param>
        /// <param name="restartMessage">Text for the word remain in, deferrals remaining.</param>
        /// <param name="dismissButtonText">Text for the defer button.</param>
        /// <param name="restartButtonText">Text for the continue button.</param>
        /// <returns>User's response as a string.</returns>
        public string ShowRestartDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            double restartCountdownMins,
            string restartMessage,
            string? dismissButtonText,
            string? restartButtonText)
        {
            if (_app == null)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app.Dispatcher.Invoke(() =>
            {
                var restartDialog = new RestartDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    appIconImage,
                    bannerImageLight,
                    bannerImageDark,
                    restartCountdownMins,
                    restartMessage,
                    dismissButtonText,
                    restartButtonText);

                _currentWindow = restartDialog;

                // Show the dialog modally
                bool? dialogResult = restartDialog.ShowDialog();
                result = restartDialog.Result ?? "Cancel";
            });

            return result;
        }

        /// <summary>
        /// Adds an application to the list of apps to close in the WelcomeDialog.
        /// </summary>
        /// <param name="appToClose">The application process info to add.</param>
        public void AddAppToClose(AppProcessInfo appToClose)
        {
            if (_app == null)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is WelcomeDialog welcomeDialog)
                {
                    welcomeDialog.AppsToCloseCollection.Add(appToClose);
                }
            });
        }

        /// <summary>
        /// Removes an application from the list of apps to close in the WelcomeDialog.
        /// </summary>
        /// <param name="appToClose">The application process info to remove.</param>
        public void RemoveAppToClose(AppProcessInfo appToClose)
        {
            if (_app == null)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is WelcomeDialog welcomeDialog)
                {
                    welcomeDialog.AppsToCloseCollection.Remove(appToClose);
                }
            });
        }

        /// <summary>
        /// Updates the progress in the ProgressDialog.
        /// </summary>
        /// <param name="value">Progress value (0 to 100).</param>
        /// <param name="message">Optional main progress message.</param>
        /// <param name="detailMessage">Optional detailed progress message.</param>
        public void UpdateProgress(double value, string? message = null, string? detailMessage = null)
        {
            if (_app == null)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is ProgressDialog progressDialog)
                {
                    progressDialog.UpdateProgress(value, message, detailMessage);
                }
            });
        }

        /// <summary>
        /// Closes the currently open dialog.
        /// </summary>
        public void CloseCurrentDialog()
        {
            if (_app == null)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app.Dispatcher.Invoke(() =>
            {
                _currentWindow?.Close();
                _currentWindow = null;
            });
        }

        /// <summary>
        /// Disposes the WPF Application and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_app != null)
                {
                    _app.Dispatcher.Invoke(() =>
                    {
                        _currentWindow?.Close();
                        _app.Dispatcher.InvokeShutdown();
                    });

                    // Wait for the application thread to exit
                    _appThread.Join();
                    _app = null!;
                }

                _disposed = true;
            }
        }
    }
}
