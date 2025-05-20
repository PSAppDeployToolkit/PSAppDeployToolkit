using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Dialogs;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Static class to manage WPF dialogs within a console application.
    /// </summary>
    public static class DialogManager
    {
        /// <summary>
        /// Static constructor to properly initialise WinForms dialogs.
        /// </summary>
        static DialogManager()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        }

        /// <summary>
        /// Displays a dialog prompting the user to close specific applications.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options specifying the applications to be closed and other dialog configurations.</param>
        /// <returns>A string representing the user's response or selection from the dialog.</returns>
        public static string ShowCloseAppsDialog(DialogStyle dialogStyle, CloseAppsDialogOptions options) => ShowModalDialog<string>(DialogType.CloseApps, dialogStyle, options);

        /// <summary>
        /// Displays a custom dialog with the specified style and options, and returns the result as a string.
        /// </summary>
        /// <remarks>This method displays a modal dialog of type <see cref="DialogType.Custom"/>. The dialog's behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the dialog, such as title, message, and buttons.</param>
        /// <returns>A string representing the result of the dialog interaction. The value depends on the dialog's configuration and user input.</returns>
        public static string ShowCustomDialog(DialogStyle dialogStyle, CustomDialogOptions options) => ShowModalDialog<string>(DialogType.Custom, dialogStyle, options);

        /// <summary>
        /// Displays an input dialog box with the specified style and options, and returns the result.
        /// </summary>
        /// <remarks>Use this method to prompt the user for input in a modal dialog. The dialog's behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options for configuring the input dialog, such as the prompt text, default value, and validation rules.</param>
        /// <returns>An <see cref="InputDialogResult"/> object containing the user's input and the dialog result (e.g., OK or Cancel).</returns>
        public static InputDialogResult ShowInputDialog(DialogStyle dialogStyle, InputDialogOptions options) => ShowModalDialog<InputDialogResult>(DialogType.Input, dialogStyle, options);

        /// <summary>
        /// Displays a modal dialog prompting the user to restart the application.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">Options that configure the restart dialog, such as title, message, and button labels.</param>
        /// <returns>A string representing the user's response to the dialog. The value depends on the implementation of the dialog and the options provided.</returns>
        public static string ShowRestartDialog(DialogStyle dialogStyle, RestartDialogOptions options) => ShowModalDialog<string>(DialogType.Restart, dialogStyle, options);

        /// <summary>
        /// Shows a non-modal Progress dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        public static void ShowProgressDialog(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            if (progressInitialized.IsSet)
            {
                throw new InvalidOperationException("A progress dialog is already open. Close it before opening a new one.");
            }
            InvokeDialogAction(() =>
            {
                progressDialog = (IProgressDialog)dialogDispatcher[dialogStyle][DialogType.Progress](options);
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
                    progressDialog!.CloseDialog();
                    progressDialog = null;
                }
            });
            progressInitialized.Reset();
        }

        /// <summary>
        /// Shows a modal dialog of the specified type with the provided options.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="dialogType"></param>
        /// <param name="dialogStyle"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static TResult ShowModalDialog<TResult>(DialogType dialogType, DialogStyle dialogStyle, BaseOptions options)
        {
            TResult? result = default;
            InvokeDialogAction(() =>
            {
                using (var dialog = (IModalDialog)dialogDispatcher[dialogStyle][dialogType](options))
                {
                    dialog.ShowDialog();
                    result = (TResult)dialog.DialogResult;
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
                    app = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
                    appInitialized.Set(); System.Windows.Threading.Dispatcher.Run();
                });
                appThread.SetApartmentState(ApartmentState.STA);
                appThread.IsBackground = true;
                appThread.Start();
                appInitialized.Wait();
            }
            app!.Dispatcher.Invoke(callback);
        }

        /// <summary>
        /// Dialog lookup table for dispatching to the correct dialog based on the style and type.
        /// </summary>
        private static readonly ReadOnlyDictionary<DialogStyle, ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>> dialogDispatcher = new(new Dictionary<DialogStyle, ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>>
        {
            {
                DialogStyle.Classic, new ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>(new Dictionary<DialogType, Func<BaseOptions, IDialogBase>>
                {
                    { DialogType.CloseApps, options => new Dialogs.Classic.CloseAppsDialog((CloseAppsDialogOptions)options) },
                    { DialogType.Custom, options => new Dialogs.Classic.CustomDialog((CustomDialogOptions)options) },
                    { DialogType.Input, options => new Dialogs.Classic.InputDialog((InputDialogOptions)options) },
                    { DialogType.Progress, options => new Dialogs.Classic.ProgressDialog((ProgressDialogOptions)options) },
                    { DialogType.Restart, options => new Dialogs.Classic.RestartDialog((RestartDialogOptions)options) },
                })
            },
            {
                DialogStyle.Fluent, new ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>(new Dictionary<DialogType, Func<BaseOptions, IDialogBase>>
                {
                    { DialogType.CloseApps, options => new Dialogs.Fluent.CloseAppsDialog((CloseAppsDialogOptions)options) },
                    { DialogType.Custom, options => new Dialogs.Fluent.CustomDialog((CustomDialogOptions)options) },
                    { DialogType.Input, options => new Dialogs.Fluent.InputDialog((InputDialogOptions)options) },
                    { DialogType.Progress, options => new Dialogs.Fluent.ProgressDialog((ProgressDialogOptions)options) },
                    { DialogType.Restart, options => new Dialogs.Fluent.RestartDialog((RestartDialogOptions)options) },
                })
            }
        });

        /// <summary>
        /// The currently open Progress dialog, if any. Null if no dialog is open.
        /// </summary>
        private static IProgressDialog? progressDialog = null;

        /// <summary>
        /// Event to signal that the progress dialog has been initialized.
        /// </summary>
        private static readonly ManualResetEventSlim progressInitialized = new(false);

        /// <summary>
        /// Application instance for the WPF dialog.
        /// </summary>
        private static System.Windows.Application? app;

        /// <summary>
        /// Thread for the WPF dialog.
        /// </summary>
        private static Thread? appThread;

        /// <summary>
        /// Event to signal that the application has been initialized.
        /// </summary>
        private static readonly ManualResetEventSlim appInitialized = new(false);
    }
}
