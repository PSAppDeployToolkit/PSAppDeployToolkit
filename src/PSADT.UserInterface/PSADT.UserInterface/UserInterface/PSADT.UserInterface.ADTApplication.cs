using System;
using System.Windows;
using System.Windows.Threading;
using PSADT.UserInterface.Services;
using Wpf.Ui.Markup;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Helper class to manage WPF dialogs within a console application.
    /// </summary>
    internal class AdtApplication : IDisposable
    {
        private Thread? _appThread;
        private Application? _app;
        private Window? _currentWindow;
        private readonly ManualResetEvent _initEvent = new(false);
        private Exception? _startupException;
        private bool _isDisposed = false;


        /// <summary>
        /// Indicates whether the AdtApplication has been disposed.
        /// </summary>
        internal bool IsDisposed => _isDisposed;

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
        /// Shows the UnifiedDialog with Welcome Dialog content synchronously and returns the user's response.
        /// </summary>
        public string ShowWelcomeDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            AppProcessInfo[]? appsToClose,
            TimeSpan? countdownDuration,
            int? deferralsRemaining,
            TimeSpan? deferralDeadline,
            string? closeAppsMessageText,
            string? alternativeCloseAppsMessageText,
            string? deferralsRemainingText,
            string? deferralDeadlineText,
            string? automaticStartCountdownText,
            string? deferButtonText,
            string? continueButtonText,
            string? alternativeContinueButtonText,
            IProcessEvaluationService? processEvaluationService
            )
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app!.Dispatcher.Invoke(() =>
            {
                // Create new CloseApps dialog
                var dialog = new UnifiedDialog(DialogType.CloseApps, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost);
                dialog.InitializeCloseAppsDialog(
                    appTitle: appTitle,
                    subtitle: subtitle,
                    appIconImage: appIconImage,
                    appsToClose: appsToClose != null ? new List<AppProcessInfo>(appsToClose) : null,
                    countdownDuration: countdownDuration,
                    deferralsRemaining: deferralsRemaining,
                    deferralDeadline: deferralDeadline,
                    closeAppsMessageText: closeAppsMessageText,
                    alternativeCloseAppsMessageText: alternativeCloseAppsMessageText,
                    deferralsRemainingText: deferralsRemainingText,
                    deferralDeadlineText: deferralDeadlineText,
                    automaticStartCountdownText: automaticStartCountdownText,
                    deferButtonText: deferButtonText,
                    continueButtonText: continueButtonText,
                    alternativeContinueButtonText: alternativeContinueButtonText,
                    processEvaluationService: processEvaluationService
                    );

                _currentWindow = dialog;
                dialog.ShowDialog();
                result = dialog.DialogResult ?? "Cancel";
            });

            return result;
        }


        /// <summary>
        /// Shows the UnifiedDialog with ProgressDialog content synchronously.
        /// </summary>
        public void ShowProgressDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            bool? topMost,
            string? progressMessage,
            string? progressDetailMessage)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                // Create new Progress dialog
                var dialog = new UnifiedDialog(DialogType.Progress, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost);
                dialog.InitializeProgressDialog(
                    appTitle: appTitle,
                    subtitle: subtitle,
                    appIconImage: appIconImage,
                    progressMessage: progressMessage,
                    progressDetailMessage: progressDetailMessage
                    );

                _currentWindow = dialog;
                dialog.Show(); // Non-modal
            });
        }

        /// <summary>
        /// Shows the UnifiedDialog with CustomDialog content synchronously and returns the user's response.
        /// </summary>
        public string ShowCustomDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            bool? topMost,
            string customMessage,
            string? ButtonLeftText,
            string? ButtonMiddleText,
            string? ButtonRightText
            )
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app!.Dispatcher.Invoke(() =>
            {
                var dialog = new UnifiedDialog(DialogType.Custom, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost);
                dialog.InitializeCustomDialog(
                    appTitle: appTitle,
                    subtitle: subtitle,
                    appIconImage: appIconImage,
                    customMessage: customMessage,
                    ButtonLeftText: ButtonLeftText,
                    ButtonMiddleText: ButtonMiddleText,
                    ButtonRightText: ButtonRightText
                );

                _currentWindow = dialog;
                dialog.ShowDialog();
                result = dialog.DialogResult ?? "Cancel";
            });

            return result;
        }

        /// <summary>
        /// Shows the UnifiedDialog with RestartDialog content synchronously and returns the user's response.
        /// </summary>
        public string ShowRestartDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            bool? topMost,
            TimeSpan? countdownDuration,
            TimeSpan? countdownNoMinimizeDuration,
            string? restartMessageText,
            string? countdownRestartMessageText,
            string? countdownAutomaticRestartText,
            string? dismissButtonText,
            string? restartButtonText)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            string result = "Cancel";

            _app!.Dispatcher.Invoke(() =>
            {
                var dialog = new UnifiedDialog(DialogType.Restart, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost);
                dialog.InitializeRestartDialog(
                    appTitle: appTitle,
                    subtitle: subtitle,
                    appIconImage: appIconImage,
                    countdownDuration: countdownDuration,
                    countdownNoMinimizeDuration: countdownNoMinimizeDuration,
                    restartMessageText: restartMessageText,
                    countdownRestartMessageText: countdownRestartMessageText,
                    countdownAutomaticRestartText: countdownAutomaticRestartText,
                    dismissButtonText: dismissButtonText,
                    restartButtonText: restartButtonText
                );

                _currentWindow = dialog;
                dialog.ShowDialog();
                result = dialog.DialogResult ?? "Cancel";
            });

            return result;
        }

        /// <summary>
        /// Adds an application to the list of apps to close in the CloseApps Dialog.
        /// </summary>
        /// <param name="appToClose">The application process info to add.</param>
        public void AddAppToClose(AppProcessInfo appToClose)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is UnifiedDialog dialog && dialog.DialogType == DialogType.CloseApps) // Ensure it's a CloseApps Dialog
                {
                    dialog.AppsToCloseCollection.Add(appToClose);
                }
            });
        }

        /// <summary>
        /// Removes an application from the list of apps to close in the CloseApps Dialog.
        /// </summary>
        /// <param name="appToClose">The application process info to remove.</param>
        public void RemoveAppToClose(AppProcessInfo appToClose)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is UnifiedDialog dialog && dialog.DialogType == DialogType.CloseApps) // Ensure it's a CloseApps Dialog
                {
                    dialog.AppsToCloseCollection.Remove(appToClose);
                }
            });
        }


        /// <summary>
        /// Updates the progress in the ProgressDialog.
        /// </summary>
        /// <param name="value">Progress value (0 to 100).</param>
        /// <param name="message">Optional main progress message.</param>
        /// <param name="detailMessage">Optional detailed progress message.</param>
        public void UpdateProgress(string? message = null, string? detailMessage = null, double? value = null)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is UnifiedDialog dialog && dialog.DialogType == DialogType.Progress) // Ensure it's a Progress Dialog
                {
                    dialog.UpdateProgress(message, detailMessage, value);
                }
            });
        }

        /// <summary>
        /// Closes the currently open dialog if it's a ProgressDialog.
        /// </summary>
        public void CloseProgressDialog()
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException("WPF Application is not initialized.");
            }

            _app!.Dispatcher.Invoke(() =>
            {
                if (_currentWindow is UnifiedDialog dialog && dialog.DialogType == DialogType.Progress) // Ensure it's a Progress Dialog
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
            if (_isDisposed)
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
            if (_isDisposed)
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
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (_app != null)
                {
                    _app.Dispatcher.Invoke(() =>
                    {
                        _currentWindow?.Close();
                        _app.Dispatcher.InvokeShutdown();
                    });

                    // Wait for the application thread to exit
                    _appThread?.Join();
                    _app = null!;
                }
            }
        }
    }
}
