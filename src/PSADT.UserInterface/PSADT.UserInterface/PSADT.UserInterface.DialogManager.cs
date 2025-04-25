using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Dialogs.Fluent;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Static class to manage WPF dialogs within a console application.
    /// </summary>
    public static class DialogManager
    {
        /// <summary>
        /// Shows the CloseApps dialog, prompting the user to close specified applications.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        /// <returns>A string indicating the user's choice: "Continue", "Defer", "Cancel", "Error", or "Disposed".</returns>
        public static string ShowCloseAppsDialog(CloseAppsDialogOptions options) => ShowModalDialog<CloseAppsDialog, CloseAppsDialogOptions, String>(options => new CloseAppsDialog(options), options);

        /// <summary>
        /// Shows a modal Custom dialog with configurable buttons and message.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        /// <returns>A string representing the text of the button clicked by the user (without accelerator underscores), or "Cancel", "Error", "Disposed".</returns>
        public static string ShowCustomDialog(CustomDialogOptions options) => ShowModalDialog<CustomDialog, CustomDialogOptions, String>(options => new CustomDialog(options), options);

        /// <summary>
        /// Shows a modal Input dialog, prompting the user for text input.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        /// <returns>A tuple containing the result string (button text clicked, "Cancel", "Error", or "Disposed") and the text entered by the user (string?).</returns>
        public static InputDialogResult ShowInputDialog(InputDialogOptions options) => ShowModalDialog<InputDialog, InputDialogOptions, InputDialogResult>(options => new InputDialog(options), options);

        /// <summary>
        /// Shows a modal Restart dialog, prompting the user to restart the system.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        /// <returns>A string indicating the user's choice: "Restart", "Dismiss", "Cancel", "Error", or "Disposed".</returns>
        public static string ShowRestartDialog(RestartDialogOptions options) => ShowModalDialog<RestartDialog, RestartDialogOptions, String>(options => new RestartDialog(options), options);

        /// <summary>
        /// Shows a non-modal Progress dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        public static void ShowProgressDialog(ProgressDialogOptions options)
        {
            if (progressInitialized.IsSet)
            {
                throw new InvalidOperationException("A progress dialog is already open. Close it before opening a new one.");
            }
            InvokeDialogAction(() =>
            {
                progressDialog = new ProgressDialog(options);
                progressDialog.Show();
            });
            progressInitialized.Set();
        }

        /// <summary>
        /// Updates the messages and optional progress percentage in the currently displayed Progress dialog.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressDetailMessage">Optional new detail message.</param>
        /// <param name="progressPercent">Optional progress percentage (0-100). If provided, the progress bar becomes determinate.</param>
        public static void UpdateProgressDialog(string? progressMessage = null, string? progressDetailMessage = null, double? progressPercent = null)
        {
            if (!progressInitialized.IsSet)
            {
                throw new InvalidOperationException("No progress dialog is currently open.");
            }
            InvokeDialogAction(() =>
            {
                progressDialog!.UpdateProgress(progressMessage, progressDetailMessage, progressPercent);
            });
        }

        /// <summary>
        /// Closes the currently open dialog, if any. Safe to call even if no dialog is open.
        /// </summary>
        public static void CloseProgressDialog()
        {
            if (!progressInitialized.IsSet)
            {
                throw new InvalidOperationException("No progress dialog is currently open.");
            }
            InvokeDialogAction(() =>
            {
                using (progressDialog)
                {
                    progressDialog!.CloseDialog(null);
                    progressDialog = null;
                }
            });
            progressInitialized.Reset();
        }

        /// <summary>
        /// Shows a modal dialog of the specified type with the provided options.
        /// </summary>
        /// <typeparam name="TDialog"></typeparam>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="factory"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static TResult ShowModalDialog<TDialog, TOptions, TResult>(Func<TOptions, TDialog> factory, TOptions options) where TDialog : FluentDialog
        {
            TResult? result = default;
            InvokeDialogAction(() =>
            {
                using (var dialog = factory(options))
                {
                    dialog.ShowDialog();
                    result = (TResult)(object)dialog.DialogResult;
                }
            });
            #warning "TODO: DialogExpiryDuration?"
            #warning "TODO: MinimizeWindows?"
            return result!;
        }

        /// <summary>
        /// Initializes the WPF application and invokes the specified action on the UI thread.
        /// </summary>
        private static void InvokeDialogAction(Action callback)
        {
            // Initialize the WPF application if necessary, otherwise just invoke the callback.
            if (!appInitialized.IsSet)
            {
                appThread = new Thread(() =>
                {
                    app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    appInitialized.Set();
                    Dispatcher.Run();
                });
                appThread.SetApartmentState(ApartmentState.STA);
                appThread.IsBackground = true;
                appThread.Start();
                appInitialized.Wait();
            }
            app!.Dispatcher.Invoke(callback);
        }

        /// <summary>
        /// The currently open Progress dialog, if any. Null if no dialog is open.
        /// </summary>
        private static ProgressDialog? progressDialog = null;

        /// <summary>
        /// Event to signal that the progress dialog has been initialized.
        /// </summary>
        private static readonly ManualResetEventSlim progressInitialized = new ManualResetEventSlim(false);

        /// <summary>
        /// Application instance for the WPF dialog.
        /// </summary>
        private static Application? app;

        /// <summary>
        /// Thread for the WPF dialog.
        /// </summary>
        private static Thread? appThread;

        /// <summary>
        /// Event to signal that the application has been initialized.
        /// </summary>
        private static readonly ManualResetEventSlim appInitialized = new ManualResetEventSlim(false);
    }
}
