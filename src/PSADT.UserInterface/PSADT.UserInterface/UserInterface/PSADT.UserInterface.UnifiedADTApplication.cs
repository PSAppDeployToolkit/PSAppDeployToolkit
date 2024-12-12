using PSADT.UserInterface.Services;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Static class to manage WPF dialogs within a console application.
    /// Provides static methods to interact with dialogs and manages the internal AdtApplication instance.
    /// </summary>
    public static class UnifiedAdtApplication
    {
        private static AdtApplication? _instance;
        private static readonly object _lock = new();

        
        /// <summary>
        /// Ensures that the AdtApplication instance is initialized and not disposed.
        /// If disposed or null, creates a new instance.
        /// </summary>
        private static AdtApplication Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null || _instance.IsDisposed)
                    {
                        _instance = new AdtApplication();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Shows the WelcomeDialog synchronously and returns the user's response.
        /// </summary>
        public static string ShowWelcomeDialog(
            string? accentColorHexValue,
            TimeSpan? dialogExpiryDuration,
            string? appTitle,
            string? subtitle,
            bool? topMost,
            int? defersRemaining,
            List<AppProcessInfo>? appsToClose,
            string? appIconImage,
            string closeAppMessage,
            string altCloseAppMessage,
            string? deferRemainText,
            string? deferButtonText,
            string? continueButtonText,
            string? altContinueButtonText,
            IProcessEvaluationService? processEvaluationService = null)
        {
            return Instance.ShowWelcomeDialog(
                accentColorHexValue,
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
        }

        /// <summary>
        /// Shows the ProgressDialog synchronously.
        /// </summary>
        public static void ShowProgressDialog(
            string? accentColorHexValue,
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? progressMessage,
            string? progressMessageDetail)
        {
            Instance.ShowProgressDialog(
                accentColorHexValue,
                appTitle,
                subtitle,
                topMost,
                appIconImage,
                progressMessage,
                progressMessageDetail);
        }

        /// <summary>
        /// Shows the CustomDialog synchronously and returns the user's response.
        /// </summary>
        public static string ShowCustomDialog(
            string? accentColorHexValue,
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
            return Instance.ShowCustomDialog(
                accentColorHexValue,
                dialogExpiryDuration,
                appTitle,
                subtitle,
                topMost,
                appIconImage,
                customMessage,
                button1Text,
                button2Text,
                button3Text);
        }

        /// <summary>
        /// Shows the RestartDialog synchronously and returns the user's response.
        /// </summary>
        public static string ShowRestartDialog(
            string? accentColorHexValue,
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
            return Instance.ShowRestartDialog(
                accentColorHexValue,
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
        }

        /// <summary>
        /// Adds an application to the list of apps to close in the WelcomeDialog.
        /// </summary>
        public static void AddAppToClose(AppProcessInfo appToClose)
        {
            Instance.AddAppToClose(appToClose);
        }

        /// <summary>
        /// Removes an application from the list of apps to close in the WelcomeDialog.
        /// </summary>
        public static void RemoveAppToClose(AppProcessInfo appToClose)
        {
            Instance.RemoveAppToClose(appToClose);
        }

        /// <summary>
        /// Updates the progress in the ProgressDialog.
        /// </summary>
        public static void UpdateProgress(double value, string? message = null, string? detailMessage = null)
        {
            Instance.UpdateProgress(value, message, detailMessage);
        }

        /// <summary>
        /// Closes the currently open dialog if it's a ProgressDialog.
        /// </summary>
        public static void CloseProgressDialog()
        {
            Instance.CloseProgressDialog();
        }

        /// <summary>
        /// Closes the currently open dialog.
        /// </summary>
        public static void CloseCurrentDialog()
        {
            Instance.CloseCurrentDialog();
        }

        /// <summary>
        /// Returns whether the current window is visible or not.
        /// </summary>
        public static bool CurrentDialogVisible()
        {
            return Instance.CurrentDialogVisible();
        }

        /// <summary>
        /// Disposes the UnifiedAdtApplication and the internal AdtApplication.
        /// After disposal, the next call to any method will re-instantiate the AdtApplication.
        /// It is not necessary to call this method even when the application is closing.
        /// </summary>
        public static void Dispose()
        {
            if (_instance != null && !_instance.IsDisposed)
            {
                lock (_lock)
                {
                    if (_instance != null && !_instance.IsDisposed)
                    {
                        _instance.Dispose();
                        _instance = null;
                    }
                }
            }
        }
    }
}
