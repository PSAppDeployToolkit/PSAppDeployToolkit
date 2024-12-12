using System.Windows;
using System.Windows.Threading;
using PSADT.UserInterface.Services;
using Wpf.Ui.Markup;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Helper class to manage WPF dialogs within a console application.
    /// </summary>
    internal class AdtApplication : IDisposable
    {
        private readonly Thread _appThread;
        private Application? _app;
        private Window? _currentWindow;
        private readonly ManualResetEvent _initEvent = new(false);
        private Exception? _startupException;
        private bool _disposed = false;

        /// <summary>
        /// Indicates whether the AdtApplication has been disposed.
        /// </summary>
        internal bool IsDisposed => _disposed;

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
        /// <param name="dialogExpiryDuration">Duration of the dialog.</param>
        /// <param name="appTitle">Title of the application.</param>
        /// <param name="subtitle">Subtitle of the application.</param>
        /// <param name="topMost">Whether the dialog should be topmost.</param>
        /// <param name="defersRemaining">Number of defers remaining.</param>
        /// <param name="appsToClose">List of applications to close.</param>
        /// <param name="appIconImage">URI of the application icon.</param>
        /// <param name="closeAppMessage">Message prompting users to close apps.</param>
        /// <param name="altCloseAppMessage">Alternative message when no apps need to be closed.</param> <!-- New Parameter -->
        /// <param name="deferRemainText">Text for the word remain in, deferrals remaining.</param>
        /// <param name="deferButtonText">Text for the defer button.</param>
        /// <param name="continueButtonText">Text for the continue button.</param>
        /// <param name="altContinueButtonText">Alternative text for the continue button when no apps need to be closed.</param> <!-- New Parameter -->
        /// <param name="processEvaluationService">Optional process evaluation service.</param>
        /// <returns>User's response as a string.</returns>
        public string ShowWelcomeDialog(
            TimeSpan? dialogExpiryDuration,
            string? appTitle,
            string? subtitle,
            bool? topMost,
            int? defersRemaining,
            AppProcessInfo[]? appsToClose,
            string? appIconImage,
            string closeAppMessage,
            string altCloseAppMessage,
            string? deferRemainText,
            string? deferButtonText,
            string? continueButtonText,
            string? altContinueButtonText,
            IProcessEvaluationService? processEvaluationService = null)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app!.Dispatcher.Invoke(() =>
            {
                var welcomeDialog = new WelcomeDialog(
                    dialogExpiryDuration,
                    appTitle,
                    subtitle,
                    topMost,
                    defersRemaining,
                    appsToClose,
                    appIconImage,
                    closeAppMessage,
                    altCloseAppMessage,
                    deferRemainText,
                    deferButtonText,
                    continueButtonText,
                    altContinueButtonText,
                    processEvaluationService);

                // Show the dialog modally
                welcomeDialog.ShowDialog();
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
        /// <param name="progressMessage">Main progress message.</param>
        /// <param name="progressMessageDetail">Detailed progress message.</param>
        public void ShowProgressDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? progressMessage,
            string? progressMessageDetail)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                var progressDialog = new ProgressDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    appIconImage,
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
        /// <param name="dialogExpiryDuration">How long before the dialog should expire.</param>
        /// <param name="appTitle">Title of the application.</param>
        /// <param name="subtitle">Subtitle of the application.</param>
        /// <param name="topMost">Whether the dialog should be topmost.</param>
        /// <param name="appIconImage">URI of the application icon.</param>
        /// <param name="customMessage">Message prompting users to close apps.</param>
        /// <param name="button1Text">Text for the word remain in, deferrals remaining.</param>
        /// <param name="button2Text">Text for the word remain in, deferrals remaining.</param>
        /// <param name="button3Text">Text for the word remain in, deferrals remaining.</param>
        /// <returns>User's response as a string.</returns>
        public string ShowCustomDialog(
            TimeSpan dialogExpiryDuration,
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string customMessage,
            string? button1Text,
            string? button2Text,
            string? button3Text)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app!.Dispatcher.Invoke(() =>
            {
                var customDialog = new CustomDialog(
                    dialogExpiryDuration,
                    appTitle,
                    subtitle,
                    topMost,
                    appIconImage,
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
        /// <param name="timeRemainingText">Main progress message.</param>
        /// <param name="restartCountdown">Message prompting users to close apps.</param>
        /// <param name="restartMessageText">Text for the word remain in, deferrals remaining.</param>
        /// <param name="restartMessageCountdownText">Text for the word remain in, deferrals remaining.</param>
        /// <param name="dismissButtonText">Text for the defer button.</param>
        /// <param name="restartButtonText">Text for the continue button.</param>
        /// <returns>User's response as a string.</returns>
        public string ShowRestartDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? timeRemainingText,
            TimeSpan? restartCountdown,
            string restartMessageText,
            string restartMessageCountdownText,
            string? dismissButtonText,
            string? restartButtonText)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app!.Dispatcher.Invoke(() =>
            {
                var restartDialog = new RestartDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    appIconImage,
                    timeRemainingText,
                    restartCountdown,
                    restartMessageText,
                    restartMessageCountdownText,
                    dismissButtonText,
                    restartButtonText);

                _currentWindow = restartDialog;

                // Show the dialog modally
                restartDialog.ShowDialog();
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
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
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
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
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
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is ProgressDialog progressDialog)

                {
                    progressDialog.UpdateProgress(value, message, detailMessage);
                }
            });
        }

        /// <summary>
        /// Closes the currently open dialog if it's a ProgressDialog.
        /// </summary>
        public void CloseProgressDialog()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is ProgressDialog progressDialog)
                {
                    _currentWindow.Close();
                    _currentWindow = null;
                }
            });
        }

        /// <summary>
        /// Closes the currently open dialog.
        /// </summary>
        public void CloseCurrentDialog()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                _currentWindow?.Close();
                _currentWindow = null;
            });
        }

        /// <summary>
        /// Returns whether the current window is visible or not.
        /// </summary>
        public bool CurrentDialogVisible()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            bool isVisible = false;

            _app!.Dispatcher.Invoke(() =>
            {
                isVisible = _currentWindow != null && _currentWindow.IsVisible;
            });

            return isVisible;
        }

        /// <summary>
        /// Disposes the WPF Application and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

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
            }
        }
    }
}
