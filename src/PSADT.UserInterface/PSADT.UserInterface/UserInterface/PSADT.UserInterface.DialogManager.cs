using PSADT.UserInterface.Dialogs.Fluent;
using PSADT.UserInterface.Services;
using System.Windows;
using System.Windows.Threading;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Static class to manage WPF dialogs within a console application.
    /// Provides static methods to interact with dialogs and manages the internal AdtApplication instance.
    /// </summary>
    public static class DialogManager
    {
        private static readonly object _lock = new();
        private static Application? _app;
        private static FluentDialog? _currentDialog;
        private static Thread? _appThread;
        private static readonly ManualResetEvent _initEvent = new(false);
        private static Exception? _startupException;
        private static bool _isDisposed = false;

        /// <summary>
        /// Initializes the WPF application and environment
        /// </summary>
        private static void EnsureApplicationInitialized()
        {
            lock (_lock)
            {
                if (_app == null && !_isDisposed)
                {
                    _appThread = new Thread(InitializeApplication)
                    {
                        IsBackground = true,
                        Name = "PSADT UI Thread"
                    };
                    _appThread.SetApartmentState(ApartmentState.STA);
                    _appThread.Start();

                    // Wait for initialization to complete
                    _initEvent.WaitOne();

                    if (_startupException != null)
                    {
                        throw new InvalidOperationException("Failed to initialize WPF Application.", _startupException);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the WPF Application on a separate STA thread
        /// </summary>
        private static void InitializeApplication()
        {
            try
            {
                _app = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };



                // Add WPF-UI resources
                _app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary { Theme = Wpf.Ui.Appearance.ApplicationTheme.Dark });
                _app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());

                // Signal that the application is ready
                _initEvent.Set();

                // Start the dispatcher processing
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                _startupException = ex;
                _initEvent.Set();
            }
        }

        /// <summary>
        /// Shows the CloseApps dialog, prompting the user to close specified applications.
        /// </summary>
        /// <param name="dialogExpiryDuration">Optional duration after which the dialog automatically closes, returning "Cancel".</param>
        /// <param name="dialogAccentColor">Optional accent color (hex string, e.g., "#FF0078D4").</param>
        /// <param name="dialogPosition">Position of the dialog on screen.</param>
        /// <param name="dialogTopMost">Whether the dialog should stay on top.</param>
        /// <param name="dialogAllowMove">Whether the user can move the dialog.</param>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="appsToClose">Array of applications the user needs to close.</param>
        /// <param name="countdownDuration">Optional duration for a countdown timer before automatic action.</param>
        /// <param name="deferralsRemaining">Optional number of deferrals allowed.</param>
        /// <param name="deferralDeadline">Optional deadline until which deferral is allowed.</param>
        /// <param name="closeAppsMessageText">Message displayed when apps need closing.</param>
        /// <param name="customMessageText">A custom message displayed underneath the CloseApps message</param>
        /// <param name="alternativeCloseAppsMessageText">Message displayed when no apps need closing.</param>
        /// <param name="deferralsRemainingText">Text displayed next to the deferral count.</param>
        /// <param name="deferralDeadlineText">Text displayed next to the deferral deadline.</param>
        /// <param name="automaticStartCountdownText">Heading text for the countdown timer.</param>
        /// <param name="deferButtonText">Text for the defer button.</param>
        /// <param name="continueButtonText">Text for the continue/close apps button.</param>
        /// <param name="alternativeContinueButtonText">Text for the continue button when no apps need closing.</param>
        /// <param name="processEvaluationService">Optional service for dynamic process evaluation.</param>
        /// <returns>A string indicating the user's choice: "Continue", "Defer", "Cancel", "Error", or "Disposed".</returns>
        public static string ShowCloseAppsDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            bool? dialogAllowMove,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            AppProcessInfo[]? appsToClose,
            TimeSpan? countdownDuration,
            int? deferralsRemaining,
            DateTime? deferralDeadline,
            string? closeAppsMessageText,
            string? alternativeCloseAppsMessageText,
            string? customMessageText,
            string? deferralsRemainingText,
            string? deferralDeadlineText,
            string? automaticStartCountdownText,
            string? deferButtonText,
            string? continueButtonText,
            string? alternativeContinueButtonText,
            IProcessEvaluationService? processEvaluationService = null
            )
        {
            return ShowModalDialogInternal(() =>
            {
                var dialog = new FluentDialog(DialogType.CloseApps, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost, dialogAllowMove);
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
                    customMessageText: customMessageText,
                    deferralsRemainingText: deferralsRemainingText,
                    deferralDeadlineText: deferralDeadlineText,
                    automaticStartCountdownText: automaticStartCountdownText,
                    deferButtonText: deferButtonText,
                    continueButtonText: continueButtonText,
                    alternativeContinueButtonText: alternativeContinueButtonText,
                    processEvaluationService: processEvaluationService
                );
                return dialog;
            });
        }

        /// <summary>
        /// Internal helper to show modal dialogs, handling common logic like initialization, dispatching, and result retrieval.
        /// </summary>
        /// <param name="createDialog">A function that creates and initializes the specific <see cref="FluentDialog"/> instance.</param>
        /// <param name="defaultResult">The default result string if the dialog closes unexpectedly.</param>
        /// <param name="errorResult">The result string to return if an error occurs during dialog display.</param>
        /// <param name="disposedResult">The result string to return if the application is disposed.</param>
        /// <returns>The result string from the dialog interaction.</returns>
        private static string ShowModalDialogInternal(
            Func<FluentDialog> createDialog,
            string defaultResult = "Cancel",
            string errorResult = "Error",
            string disposedResult = "Disposed")
        {
            if (_isDisposed)
                return disposedResult;

            EnsureApplicationInitialized();

            string result = defaultResult;

            try
            {
                _app!.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Close any existing dialog
                        CloseCurrentDialog();

                        // Create and initialize the specific dialog
                        _currentDialog = createDialog();

                        // Show dialog modally and get result
                        _currentDialog.ShowDialog();
                        result = _currentDialog.DialogResult ?? defaultResult;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error showing dialog: {ex.Message}");
                        result = errorResult;
                    }
                });
            }
            catch (Exception ex)
            {
                // Catch exceptions during Dispatcher.Invoke itself (e.g., thread abort)
                System.Diagnostics.Debug.WriteLine($"Critical error showing dialog: {ex.Message}");
                result = errorResult;
            }

            return result;
        }


        /// <summary>
        /// Shows a non-modal Progress dialog.
        /// </summary>
        /// <param name="dialogExpiryDuration">Optional duration after which the dialog automatically closes.</param>
        /// <param name="dialogAccentColor">Optional accent color (hex string).</param>
        /// <param name="dialogPosition">Position of the dialog on screen.</param>
        /// <param name="dialogTopMost">Whether the dialog should stay on top.</param>
        /// <param name="dialogAllowMove">Whether the user can move the dialog.</param>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="progressMessage">The main progress message text.</param>
        /// <param name="progressDetailMessage">A secondary message providing more detail.</param>
        public static void ShowProgressDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            bool? dialogAllowMove,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? progressMessage,
            string? progressDetailMessage)
        {
            if (_isDisposed)
                return;

            EnsureApplicationInitialized();

            try
            {
                _app!.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Close any existing dialog
                        CloseCurrentDialog();

                        // Create new progress dialog
                        _currentDialog = new FluentDialog(DialogType.Progress, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost, dialogAllowMove);
                        _currentDialog.InitializeProgressDialog(
                            appTitle: appTitle,
                            subtitle: subtitle,
                            appIconImage: appIconImage,
                            progressMessage: progressMessage,
                            progressDetailMessage: progressDetailMessage
                            );

                        // Show dialog non-modally
                        _currentDialog.Show();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error showing Progress dialog: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in ShowProgressDialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a modal Custom dialog with configurable buttons and message.
        /// </summary>
        /// <param name="dialogExpiryDuration">Optional duration after which the dialog automatically closes, returning "Cancel".</param>
        /// <param name="dialogAccentColor">Optional accent color (hex string).</param>
        /// <param name="dialogPosition">Position of the dialog on screen.</param>
        /// <param name="dialogTopMost">Whether the dialog should stay on top.</param>
        /// <param name="dialogAllowMove">Whether the user can move the dialog.</param>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="customMessage">The main message text to display.</param>
        /// <param name="ButtonLeftText">Text for the left button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonMiddleText">Text for the middle button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonRightText">Text for the right button. If null or empty, the button is hidden.</param>
        /// <returns>A string representing the text of the button clicked by the user (without accelerator underscores), or "Cancel", "Error", "Disposed".</returns>
        public static string ShowCustomDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            bool? dialogAllowMove,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? customMessage,
            string? ButtonLeftText,
            string? ButtonMiddleText,
            string? ButtonRightText
            )
        {
            return ShowModalDialogInternal(() =>
            {
                var dialog = new FluentDialog(DialogType.Custom, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost, dialogAllowMove);
                dialog.InitializeCustomDialog(
                    appTitle: appTitle,
                    subtitle: subtitle,
                    appIconImage: appIconImage,
                    customMessage: customMessage,
                    ButtonLeftText: ButtonLeftText,
                    ButtonMiddleText: ButtonMiddleText,
                    ButtonRightText: ButtonRightText
                );
                return dialog;
            });
        }

        /// <summary>
        /// Shows a modal Input dialog, prompting the user for text input.
        /// </summary>
        /// <param name="dialogExpiryDuration">Optional duration after which the dialog automatically closes, returning ("Cancel", null).</param>
        /// <param name="dialogAccentColor">Optional accent color (hex string).</param>
        /// <param name="dialogPosition">Position of the dialog on screen.</param>
        /// <param name="dialogTopMost">Whether the dialog should stay on top.</param>
        /// <param name="dialogAllowMove">Whether the user can move the dialog.</param>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="customMessage">The message text displayed above the input box.</param>
        /// <param name="initialInputText">The initial text pre-filled in the input box.</param>
        /// <param name="ButtonLeftText">Text for the left button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonMiddleText">Text for the middle button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonRightText">Text for the right button. If null or empty, the button is hidden.</param>
        /// <returns>A tuple containing the result string (button text clicked, "Cancel", "Error", or "Disposed") and the text entered by the user (string?).</returns>
        public static (string Result, string? InputText) ShowInputDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            bool? dialogAllowMove,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? customMessage, // Renamed from inputBoxTextBlock
            string? initialInputText, // Renamed from inputBoxText
            string? ButtonLeftText,
            string? ButtonMiddleText,
            string? ButtonRightText
            )
        {
            if (_isDisposed)
                return ("Disposed", null);

            EnsureApplicationInitialized();

            string result = "Cancel";
            string? inputText = null;

            try
            {
                _app!.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Close any existing dialog
                        CloseCurrentDialog();

                        // Create new input dialog
                        _currentDialog = new FluentDialog(DialogType.Input, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost, dialogAllowMove);
                        _currentDialog.InitializeInputDialog(
                            appTitle: appTitle,
                            subtitle: subtitle,
                            appIconImage: appIconImage,
                            customMessage: customMessage,
                            initialInputText: initialInputText,
                            ButtonLeftText: ButtonLeftText,
                            ButtonMiddleText: ButtonMiddleText,
                            ButtonRightText: ButtonRightText
                            );

                        // Show dialog and get result
                        _currentDialog.ShowDialog();
                        result = _currentDialog.DialogResult ?? "Cancel";
                        inputText = _currentDialog.InputTextResult; // Get the input text
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error showing Input dialog: {ex.Message}");
                        result = "Error";
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in ShowInputDialog: {ex.Message}");
                result = "Error";
            }

            return (result, inputText);
        }

        /// <summary>
        /// Shows a modal Restart dialog, prompting the user to restart the system.
        /// </summary>
        /// <param name="dialogExpiryDuration">Optional duration after which the dialog automatically closes, returning "Cancel".</param>
        /// <param name="dialogAccentColor">Optional accent color (hex string).</param>
        /// <param name="dialogPosition">Position of the dialog on screen.</param>
        /// <param name="dialogTopMost">Whether the dialog should stay on top.</param>
        /// <param name="dialogAllowMove">Whether the user can move the dialog.</param>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="countdownDuration">Optional duration for a countdown timer before automatic restart.</param>
        /// <param name="countdownNoMinimizeDuration">Optional duration before the end of the countdown when the 'Dismiss' button is disabled.</param>
        /// <param name="restartMessageText">The main message text asking for restart confirmation.</param>
        /// <param name="customMessageText">A custom message displayed underneath the CloseApps message</param>
        /// <param name="countdownRestartMessageText">Message text displayed when the countdown is active.</param>
        /// <param name="countdownAutomaticRestartText">Heading text for the countdown timer.</param>
        /// <param name="dismissButtonText">Text for the dismiss/restart later button.</param>
        /// <param name="restartButtonText">Text for the restart now button.</param>
        /// <returns>A string indicating the user's choice: "Restart", "Dismiss", "Cancel", "Error", or "Disposed".</returns>
        public static string ShowRestartDialog(
            TimeSpan? dialogExpiryDuration,
            String? dialogAccentColor,
            DialogPosition? dialogPosition,
            bool? dialogTopMost,
            bool? dialogAllowMove,
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            TimeSpan? countdownDuration,
            TimeSpan? countdownNoMinimizeDuration,
            string restartMessageText,
            string? customMessageText,
            string? countdownRestartMessageText,
            string? countdownAutomaticRestartText,
            string? dismissButtonText,
            string? restartButtonText
            )
        {
            return ShowModalDialogInternal(() =>
            {
                var dialog = new FluentDialog(DialogType.Restart, dialogExpiryDuration, dialogAccentColor, dialogPosition, dialogTopMost, dialogAllowMove);
                dialog.InitializeRestartDialog(
                    appTitle: appTitle,
                    subtitle: subtitle,
                    appIconImage: appIconImage,
                    countdownDuration: countdownDuration,
                    countdownNoMinimizeDuration: countdownNoMinimizeDuration,
                    restartMessageText: restartMessageText,
                    customMessageText: customMessageText,
                    countdownRestartMessageText: countdownRestartMessageText,
                    countdownAutomaticRestartText: countdownAutomaticRestartText,
                    dismissButtonText: dismissButtonText,
                    restartButtonText: restartButtonText
                );
                return dialog;
            });
        }

        /// <summary>
        /// Updates the messages and optional progress percentage in the currently displayed Progress dialog.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressMessageDetail">Optional new detail message.</param>
        /// <param name="progressPercent">Optional progress percentage (0-100). If provided, the progress bar becomes determinate.</param>
        public static void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? progressPercent = null)
        {
            if (_isDisposed)
                return;

            System.Diagnostics.Debug.WriteLine($"Updating Progress Information - Message: {progressMessage}. Detail: {progressMessageDetail}. Percent Complete: {progressPercent}.");

            Application? app = null;
            FluentDialog? dialog = null;

            // Safely capture references under lock
            lock (_lock)
            {
                if (_isDisposed || _app == null)
                    return;

                app = _app;
                dialog = _currentDialog;
            }

            if (app == null || dialog == null)
                return;

            try
            {
                // Use the captured references outside the lock
                app.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (dialog != null && dialog.DialogType == DialogType.Progress && !dialog.IsDisposed)
                        {
                            dialog.UpdateProgress(progressMessage, progressMessageDetail, progressPercent);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating progress: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in UpdateProgress: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds an application to the list of apps to close in the currently displayed CloseApps dialog.
        /// </summary>
        /// <param name="appToClose">The application process information to add.</param>
        public static void AddAppToClose(AppProcessInfo appToClose)
        {
            if (_isDisposed)
                return;

            Application? app = null;
            FluentDialog? dialog = null;

            // Safely capture references under lock
            lock (_lock)
            {
                if (_isDisposed || _app == null)
                    return;

                app = _app;
                dialog = _currentDialog;
            }

            if (app == null || dialog == null)
                return;

            try
            {
                app.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (dialog != null && dialog.DialogType == DialogType.CloseApps && !dialog.IsDisposed)
                        {
                            dialog.AppsToCloseCollection.Add(appToClose);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding app to close: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in AddAppToClose: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes an application from the list of apps to close in the currently displayed CloseApps dialog, triggering a fade-out animation.
        /// </summary>
        /// <param name="appToClose">The application process information to remove.</param>
        public static void RemoveAppToClose(AppProcessInfo appToClose)
        {
            if (_isDisposed)
                return;

            Application? app = null;
            FluentDialog? dialog = null;

            // Safely capture references under lock
            lock (_lock)
            {
                if (_isDisposed || _app == null)
                    return;

                app = _app;
                dialog = _currentDialog;
            }

            if (app == null || dialog == null)
                return;

            try
            {
                app.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (dialog != null && dialog.DialogType == DialogType.CloseApps && !dialog.IsDisposed)
                        {
                            var processToRemove = dialog.AppsToCloseCollection.FirstOrDefault(a =>
                                a.ProcessName.Equals(appToClose.ProcessName, StringComparison.OrdinalIgnoreCase));

                            if (processToRemove != null)
                            {
                                // Get the ListView and find the container
                                var listView = dialog.FindName("CloseAppsListView") as System.Windows.Controls.ListView;
                                if (listView != null)
                                {
                                    var container = listView.ItemContainerGenerator.ContainerFromItem(processToRemove) as FrameworkElement;
                                    if (container != null)
                                    {
                                        // Create fade-out animation
                                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                                        {
                                            From = 1.0,
                                            To = 0.0,
                                            Duration = TimeSpan.FromMilliseconds(300),
                                            EasingFunction = new System.Windows.Media.Animation.QuadraticEase
                                            {
                                                EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn
                                            }
                                        };

                                        // Set completed callback
                                        animation.Completed += (s, args) =>
                                        {
                                            dialog.AppsToCloseCollection.Remove(processToRemove);
                                        };

                                        // Start animation
                                        container.BeginAnimation(UIElement.OpacityProperty, animation);
                                    }
                                    else
                                    {
                                        // Fallback if container not found
                                        dialog.AppsToCloseCollection.Remove(processToRemove);
                                    }
                                }
                                else
                                {
                                    // Fallback if ListView not found
                                    dialog.AppsToCloseCollection.Remove(processToRemove);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error removing app to close: {ex.Message}");

                        // Ensure item is removed even if animation fails
                        try
                        {
                            if (dialog != null && dialog.DialogType == DialogType.CloseApps && !dialog.IsDisposed)
                            {
                                var processToRemove = dialog.AppsToCloseCollection.FirstOrDefault(a =>
                                    a.ProcessName.Equals(appToClose.ProcessName, StringComparison.OrdinalIgnoreCase));

                                if (processToRemove != null)
                                {
                                    dialog.AppsToCloseCollection.Remove(processToRemove);
                                }
                            }
                        }
                        catch
                        {
                            // Suppress any errors in the fallback removal
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in RemoveAppToClose: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the currently open dialog, if any. Safe to call even if no dialog is open.
        /// </summary>
        public static void CloseCurrentDialog()
        {
            if (_isDisposed)
                return;

            Application? app = null;
            FluentDialog? dialog = null;

            // Safely capture references under lock
            lock (_lock)
            {
                if (_isDisposed || _app == null)
                    return;

                app = _app;
                dialog = _currentDialog;
            }

            if (app == null)
                return;

            try
            {
                app.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (dialog != null && !dialog.IsDisposed)
                        {
                            dialog.CloseDialog(null);
                            lock (_lock)
                            {
                                if (_currentDialog == dialog)
                                {
                                    _currentDialog = null;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error closing current dialog: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in CloseCurrentDialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a dialog managed by this class is currently open and visible.
        /// </summary>
        /// <returns>True if a dialog is currently visible, false otherwise.</returns>
        public static bool CurrentDialogVisible()
        {
            if (_isDisposed)
                return false;

            Application? app = null;
            FluentDialog? dialog = null;

            // Safely capture references under lock
            lock (_lock)
            {
                if (_isDisposed || _app == null)
                    return false;

                app = _app;
                dialog = _currentDialog;
            }

            if (app == null || dialog == null)
                return false;

            bool isVisible = false;

            try
            {
                app.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        isVisible = dialog != null && !dialog.IsDisposed && dialog.IsVisible;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error checking dialog visibility: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in CurrentDialogVisible: {ex.Message}");
            }

            return isVisible;
        }

        /// <summary>
        /// Disposes the underlying WPF Application, closes any open dialog, and cleans up resources.
        /// </summary>
        public static void Dispose()
        {
            if (_isDisposed)
                return;

            lock (_lock)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                try
                {
                    if (_app != null)
                    {
                        try
                        {
                            _app.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    CloseCurrentDialog();
                                    _app.Shutdown();
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error during application shutdown: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error invoking application dispatcher: {ex.Message}");
                        }

                        // Wait for thread to complete
                        if (_appThread != null && _appThread.IsAlive)
                        {
                            try
                            {
                                _appThread.Join(1000);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error joining thread: {ex.Message}");
                            }
                        }

                        _app = null;
                    }

                    // Clear cached resources
                    // Dispose the ManualResetEvent
                    _initEvent.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during UnifiedAdtApplication disposal: {ex.Message}");
                }
            }
        }
    }
}
