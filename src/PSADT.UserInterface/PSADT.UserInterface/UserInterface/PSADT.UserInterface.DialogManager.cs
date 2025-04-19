using System.Windows.Threading;
using PSADT.UserInterface.Dialogs;
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
        public static string ShowCloseAppsDialog(CloseAppsDialogOptions options)=> ShowModalDialog<CloseAppsDialog, CloseAppsDialogOptions, String>(options => new CloseAppsDialog(options), options);

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
            if (ProgressDialogOpen())
            {
                throw new InvalidOperationException("A progress dialog is already open. Close it before opening a new one.");
            }
            progressThread = new Thread(() =>
            {
                progressDispatcher = Dispatcher.CurrentDispatcher;
                using (progressDialog = new ProgressDialog(options))
                {
                    progressInitialized.Set();
                    progressDialog.ShowDialog();
                }
            });
            progressThread.SetApartmentState(ApartmentState.STA);
            progressThread.IsBackground = true;
            progressThread.Start();
            progressInitialized.Wait();
        }

        /// <summary>
        /// Updates the messages and optional progress percentage in the currently displayed Progress dialog.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressDetailMessage">Optional new detail message.</param>
        /// <param name="progressPercent">Optional progress percentage (0-100). If provided, the progress bar becomes determinate.</param>
        public static void UpdateProgressDialog(string? progressMessage = null, string? progressDetailMessage = null, double? progressPercent = null)
        {
            if (!ProgressDialogOpen())
            {
                throw new InvalidOperationException("No progress dialog is currently open.");
            }
            progressDispatcher!.Invoke(() =>
            {
                progressDialog!.UpdateProgress(progressMessage, progressDetailMessage, progressPercent);
            });
            ;
        }

        /// <summary>
        /// Closes the currently open dialog, if any. Safe to call even if no dialog is open.
        /// </summary>
        public static void CloseProgressDialog()
        {
            if (!ProgressDialogOpen())
            {
                throw new InvalidOperationException("No progress dialog is currently open.");
            }
            progressDispatcher!.Invoke(() =>
            {
                using (progressDialog)
                {
                    progressDialog!.CloseDialog(null);
                    progressDialog = null;
                }
            });
            progressThread!.Join();
            progressThread = null;
            progressDispatcher = null;
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
            using (var dialog = factory(options))
            {
                dialog.ShowDialog();
                return (TResult)(object)dialog.DialogResult;
            }
        }

        /// <summary>
        /// Checks if a Progress dialog is currently open.
        /// </summary>
        /// <returns></returns>
        public static bool ProgressDialogOpen() => progressInitialized.IsSet;

        /// <summary>
        /// The currently open Progress dialog, if any. Null if no dialog is open.
        /// </summary>
        private static ProgressDialog? progressDialog = null;

        /// <summary>
        /// Thread for the progress dialog.
        /// </summary>
        private static Thread? progressThread = null;

        /// <summary>
        /// Sets the dispatcher for the progress dialog.
        /// </summary>
        private static Dispatcher? progressDispatcher = null;

        /// <summary>
        /// Thread for the progress dialog.
        /// </summary>
        private static readonly ManualResetEventSlim progressInitialized = new ManualResetEventSlim(false);
    }
}
