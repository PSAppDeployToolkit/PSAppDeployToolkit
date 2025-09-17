using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using PSADT.AccountManagement;
using PSADT.LibraryInterfaces;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Dialogs;
using PSADT.UserInterface.DialogState;
using PSADT.Utilities;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Static class to manage WPF dialogs within a console application.
    /// </summary>
    internal static class DialogManager
    {
        /// <summary>
        /// Displays a dialog prompting the user to close specific applications.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options specifying the applications to be closed and other dialog configurations.</param>
        /// <param name="state">The current state of the dialog, including services for tracking running processes and logging.</param>
        /// <returns>A string representing the user's response or selection from the dialog.</returns>
        internal static CloseAppsDialogResult ShowCloseAppsDialog(DialogStyle dialogStyle, CloseAppsDialogOptions options, CloseAppsDialogState state)
        {
            // Start the RunningProcessService if it is not already running.
            bool stopProcessService = false;
            if (null != state.RunningProcessService && !state.RunningProcessService.IsRunning)
            {
                state.RunningProcessService.Start();
                stopProcessService = true;
            }

            // Perform logging if we have a log writer.
            if (null != state.LogWriter)
            {
                // Announce whether there's apps to close.
                var procsRunning = state.RunningProcessService?.ProcessesToClose;
                if (procsRunning?.Count > 0)
                {
                    state.LogWriter.Write($"Prompting the user to close application(s) ['{string.Join("', '", procsRunning.Select(static p => p.Description))}']...");
                    state.LogWriter.Flush();
                }

                // Announce the current countdown information.
                if (null != options.CountdownDuration)
                {
                    var elapsed = options.CountdownDuration - state.CountdownStopwatch.Elapsed;
                    if (elapsed < TimeSpan.Zero)
                    {
                        elapsed = TimeSpan.Zero;
                    }
                    if (procsRunning?.Count > 0)
                    {
                        state.LogWriter.Write($"Close applications countdown has [{elapsed}] seconds remaining.");
                        state.LogWriter.Flush();
                    }
                    else
                    {
                        state.LogWriter.Write($"Countdown has [{elapsed}] seconds remaining.");
                        state.LogWriter.Flush();
                    }
                }
            }

            // Show the dialog and get the result.
            var result = ShowModalDialog<CloseAppsDialogResult>(DialogType.CloseAppsDialog, dialogStyle, options, state);

            // Perform some result logging before returning.
            if ((null != state.LogWriter) && (null != options.CountdownDuration) && (options.CountdownDuration - state.CountdownStopwatch.Elapsed) <= TimeSpan.Zero)
            {
                switch (result)
                {
                    case CloseAppsDialogResult.Close:
                        state.LogWriter.Write("Close application(s) countdown timer has elapsed. Force closing application(s).");
                        state.LogWriter.Flush();
                        break;
                    case CloseAppsDialogResult.Defer:
                        state.LogWriter.Write("Countdown timer has elapsed and deferrals remaining. Force deferral.");
                        state.LogWriter.Flush();
                        break;
                    case CloseAppsDialogResult.Continue:
                        state.LogWriter.Write("Countdown timer has elapsed and no processes running. Force continue.");
                        state.LogWriter.Flush();
                        break;
                }
            }

            // If we started the RunningProcessService, stop it now before returning the result.
            if (stopProcessService)
            {
                state.RunningProcessService!.Stop();
            }
            return result;
        }

        /// <summary>
        /// Displays a custom dialog with the specified style and options, and returns the result as a string.
        /// </summary>
        /// <remarks>This method displays a modal dialog of type <see cref="DialogType.Custom"/>. The dialog's behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the dialog, such as title, message, and buttons.</param>
        /// <returns>A string representing the result of the dialog interaction. The value depends on the dialog's configuration and user input.</returns>
        internal static string ShowCustomDialog(DialogStyle dialogStyle, CustomDialogOptions options)
        {
            if (options.MinimizeWindows)
            {
                ShellUtilities.MinimizeAllWindows();
            }
            var res = ShowModalDialog<string>(DialogType.CustomDialog, dialogStyle, options);
            if (options.MinimizeWindows)
            {
                ShellUtilities.RestoreAllWindows();
            }
            return res;
        }

        /// <summary>
        /// Displays an input dialog box with the specified style and options, and returns the result.
        /// </summary>
        /// <remarks>Use this method to prompt the user for input in a modal dialog. The dialog's behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options for configuring the input dialog, such as the prompt text, default value, and validation rules.</param>
        /// <returns>An <see cref="InputDialogResult"/> object containing the user's input and the dialog result (e.g., OK or Cancel).</returns>
        internal static InputDialogResult ShowInputDialog(DialogStyle dialogStyle, InputDialogOptions options)
        {
            if (AccountUtilities.CallerUsingServiceUI)
            {
                throw new InvalidOperationException("The input dialog is only permitted when ServiceUI is not used to start the toolkit.");
            }
            if (options.MinimizeWindows)
            {
                ShellUtilities.MinimizeAllWindows();
            }
            var res = ShowModalDialog<InputDialogResult>(DialogType.InputDialog, dialogStyle, options);
            if (options.MinimizeWindows)
            {
                ShellUtilities.RestoreAllWindows();
            }
            return res;
        }

        /// <summary>
        /// Displays a modal dialog prompting the user to restart the application.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">Options that configure the restart dialog, such as title, message, and button labels.</param>
        /// <returns>A string representing the user's response to the dialog. The value depends on the implementation of the dialog and the options provided.</returns>
        internal static string ShowRestartDialog(DialogStyle dialogStyle, RestartDialogOptions options) => ShowModalDialog<string>(DialogType.RestartDialog, dialogStyle, options);

        /// <summary>
        /// Displays a progress dialog with the specified style and options.
        /// </summary>
        /// <remarks>This method initializes and displays a progress dialog based on the provided style and options. Only one progress dialog can be displayed at a time. Attempting to open a new dialog while another is active will result in an exception.</remarks>
        /// <param name="dialogStyle">The style of the dialog to display. This determines the visual appearance and behavior of the progress dialog.</param>
        /// <param name="options">The configuration options for the progress dialog, such as title, message, and progress settings.</param>
        /// <exception cref="InvalidOperationException">Thrown if a progress dialog is already open. Ensure the current progress dialog is closed before attempting to open a new one.</exception>
        internal static void ShowProgressDialog(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            if (progressInitialized.IsSet)
            {
                throw new InvalidOperationException("A progress dialog is already open. Close it before opening a new one.");
            }
            InvokeDialogAction<object>(() =>
            {
                progressDialog = (IProgressDialog)dialogDispatcher[dialogStyle][DialogType.ProgressDialog](options, null);
                progressDialog.Show();
                return null!;
            });
            progressInitialized.Set();
        }

        /// <summary>
        /// Determines whether the progress dialog is currently open.
        /// </summary>
        /// <remarks>This method checks the internal state to determine if the progress dialog has been initialized and is currently displayed.</remarks>
        /// <returns><see langword="true"/> if the progress dialog is open; otherwise, <see langword="false"/>.</returns>
        internal static bool ProgressDialogOpen() => progressInitialized.IsSet;

        /// <summary>
        /// Updates the messages and optional progress percentage in the currently displayed Progress dialog.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressDetailMessage">Optional new detail message.</param>
        /// <param name="progressPercentage">Optional progress percentage (0-100). If provided, the progress bar becomes determinate.</param>
        /// <param name="messageAlignment">Optional message alignment. If provided, the message alignment is updated.</param>
        internal static void UpdateProgressDialog(string? progressMessage = null, string? progressDetailMessage = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            if (!progressInitialized.IsSet)
            {
                throw new InvalidOperationException("No progress dialog is currently open.");
            }
            InvokeDialogAction<object>(() =>
            {
                progressDialog!.UpdateProgress(progressMessage, progressDetailMessage, progressPercentage, messageAlignment);
                return null!;
            });
        }

        /// <summary>
        /// Closes the currently open dialog, if any. Safe to call even if no dialog is open.
        /// </summary>
        internal static void CloseProgressDialog()
        {
            if (!progressInitialized.IsSet)
            {
                throw new InvalidOperationException("No progress dialog is currently open.");
            }
            InvokeDialogAction<object>(() =>
            {
                using (progressDialog)
                {
                    progressDialog!.CloseDialog();
                }
                progressDialog = null;
                return null!;
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
        private static TResult ShowModalDialog<TResult>(DialogType dialogType, DialogStyle dialogStyle, BaseOptions options, BaseState? state = null)
        {
            return InvokeDialogAction(() =>
            {
                using var dialog = (IModalDialog)dialogDispatcher[dialogStyle][dialogType](options, state);
                dialog.ShowDialog(); return (TResult)dialog.DialogResult;
            });
        }

        /// <summary>
        /// Displays a balloon tip notification in the system tray with the specified title, text, and icon.
        /// </summary>
        /// <remarks>This method sets the AppUserModelID for the current process to ensure compatibility with Windows 10 toast notifications. It also updates the registry with the provided application title and icon to correct stale information from previous runs. The balloon tip is displayed for a default duration of 7 seconds or until the user closes it.</remarks>
        /// <param name="options">The configuration options for the balloon tip, including title, text, icon, and other settings.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task completes when the balloon tip is closed.</returns>
        internal static void ShowBalloonTip(BalloonTipOptions options)
        {
            // Set the AUMID for this process so the Windows 10 toast has the correct title.
            Shell32.SetCurrentProcessExplicitAppUserModelID(options.TrayTitle);

            // Correct the registry data for the AUMID. This can reference stale info from a previous run.
            var regKey = $@"{(AccountUtilities.CallerIsAdmin ? @"HKEY_CLASSES_ROOT" : @"HKEY_CURRENT_USER\Software\Classes")}\AppUserModelId\{options.TrayTitle}";
            Registry.SetValue(regKey, "DisplayName", options.TrayTitle, RegistryValueKind.String);
            Registry.SetValue(regKey, "IconUri", options.TrayIcon, RegistryValueKind.ExpandString);

            // Don't let this dispose until the balloon tip closes. If it disposes too early, Windows won't show the BalloonTipIcon properly.
            // It's worth noting that while a timeout can be specified, Windows doesn't necessarily honour it and will likely show for ~7 seconds only.
            using var notifyIcon = new System.Windows.Forms.NotifyIcon { Icon = Dialogs.Classic.ClassicAssets.GetIcon(options.TrayIcon), Visible = true };
            ManualResetEventSlim balloonTipClosed = new();
            notifyIcon.BalloonTipClosed += (_, _) => balloonTipClosed.Set();
            notifyIcon.BalloonTipClicked += (_, _) => balloonTipClosed.Set();
            notifyIcon.ShowBalloonTip(ValueTypeConverter.ToInt(options.BalloonTipTime), options.BalloonTipTitle, options.BalloonTipText, options.BalloonTipIcon);
            balloonTipClosed.Wait();
        }

        /// <summary>
        /// Displays a message box with the specified title, prompt, and options.
        /// </summary>
        /// <remarks>The behavior and appearance of the message box are determined by the properties of the <paramref name="options"/> parameter.</remarks>ews
        /// <param name="options">The options for configuring the message box, such as title, message text, buttons, icon, default button, topmost behavior, and expiry duration.</param>
        /// <returns>A <see cref="DialogBoxResult"/> value indicating the button that was clicked by the user.</returns>
        internal static DialogBoxResult ShowDialogBox(DialogBoxOptions options) => ShowDialogBox(options.AppTitle, options.MessageText, options.DialogButtons, options.DialogDefaultButton, options.DialogIcon, options.DialogTopMost, options.DialogExpiryDuration);

        /// <summary>
        /// Displays a message box with the specified title, prompt, buttons, icon, default button, and topmost
        /// behavior.
        /// </summary>
        /// <param name="Title">The title of the message box.</param>
        /// <param name="Prompt">The message to display in the message box.</param>
        /// <param name="Buttons">The set of buttons to display in the message box, such as OK, Cancel, or Yes/No.</param>
        /// <param name="DefaultButton">The button that is selected by default when the message box is displayed.</param>
        /// <param name="Icon">The icon to display in the message box, such as Information, Warning, or Error.</param>
        /// <param name="TopMost">A value indicating whether the message box should appear as a topmost window. <see langword="true"/> to make the message box topmost; otherwise, <see langword="false"/>.</param>
        /// <param name="Timeout">Optional timeout for the message box. If specified, the message box will automatically close after the given duration.</param>
        /// <returns>A <see cref="MsgBoxResult"/> value indicating the button clicked by the user.</returns>
        internal static DialogBoxResult ShowDialogBox(string Title, string Prompt, DialogBoxButtons Buttons, DialogBoxDefaultButton DefaultButton, DialogBoxIcon Icon, bool TopMost, TimeSpan Timeout) => (DialogBoxResult)ShowDialogBox(Title, Prompt, (MESSAGEBOX_STYLE)Buttons | (MESSAGEBOX_STYLE)Icon | (MESSAGEBOX_STYLE)DefaultButton | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_SETFOREGROUND | (TopMost ? MESSAGEBOX_STYLE.MB_SYSTEMMODAL | MESSAGEBOX_STYLE.MB_TOPMOST : 0), Timeout);

        /// <summary>
        /// Displays a message box with the specified prompt, buttons, and title.
        /// </summary>
        /// <param name="Title">The title text to display in the message box's title bar.</param>
        /// <param name="Prompt">The text to display in the message box.</param>
        /// <param name="Options">A <see cref="MESSAGEBOX_STYLE"/> value that specifies the buttons and icons to display in the message box.</param>
        /// <param name="Timeout">An optional <see cref="TimeSpan"/> value that specifies the duration after which the message box will automatically close. If not specified, the message box will remain open until the user interacts with it.</param>
        /// <returns>A <see cref="MESSAGEBOX_RESULT"/> value that indicates which button the user clicked in the message box.</returns>
        internal static MESSAGEBOX_RESULT ShowDialogBox(string Title, string Prompt, MESSAGEBOX_STYLE Options, TimeSpan Timeout = default) => InvokeDialogAction<MESSAGEBOX_RESULT>(() => User32.MessageBoxTimeout(IntPtr.Zero, Prompt, Title, Options, 0, Timeout));

        /// <summary>
        /// Displays a task dialog box with the specified title, subtitle, prompt, buttons, and icon.
        /// </summary>
        /// <remarks>This method internally invokes a task dialog using the Windows Common Controls library. The dialog is modal and blocks execution until the user interacts with it.</remarks>
        /// <param name="Title">The title of the task dialog box. This appears in the title bar of the dialog.</param>
        /// <param name="Subtitle">The subtitle of the task dialog box. This appears as a header in the dialog.</param>
        /// <param name="Prompt">The main prompt or message displayed in the dialog box.</param>
        /// <param name="Buttons">A combination of flags specifying the buttons to display in the dialog. This must be a valid <see cref="TASKDIALOG_COMMON_BUTTON_FLAGS"/> value.</param>
        /// <param name="Icon">The icon to display in the dialog box. This must be a valid <see cref="TASKDIALOG_ICON"/> value.</param>
        /// <returns>A <see cref="MESSAGEBOX_RESULT"/> value indicating the button that the user clicked to close the dialog.</returns>
        private static MESSAGEBOX_RESULT ShowTaskBox(string Title, string Subtitle, string Prompt, TASKDIALOG_COMMON_BUTTON_FLAGS Buttons, TASKDIALOG_ICON Icon) => InvokeDialogAction<MESSAGEBOX_RESULT>(() => ComCtl32.TaskDialog(HWND.Null, HINSTANCE.Null, Title, Subtitle, Prompt, Buttons, Icon));

        /// <summary>
        /// Displays the Help Console dialog with the specified options.
        /// </summary>
        /// <remarks>This method invokes the Help Console dialog using the provided <paramref
        /// name="options"/> to customize its appearance and functionality.</remarks>
        /// <param name="options">The configuration options for the Help Console dialog, including display settings and behavior.</param>
        /// <returns>A <see cref="System.Windows.Forms.DialogResult"/> value indicating the result of the dialog interaction. For
        /// example, <see cref="System.Windows.Forms.DialogResult.OK"/> if the user confirmed, or <see
        /// cref="System.Windows.Forms.DialogResult.Cancel"/> if the user canceled.</returns>
        internal static System.Windows.Forms.DialogResult ShowHelpConsole(HelpConsoleOptions options) => InvokeDialogAction<System.Windows.Forms.DialogResult>(() =>
        {
            using Dialogs.Classic.HelpConsole helpConsole = new(options);
            return helpConsole.ShowDialog();
        });

        /// <summary>
        /// Initializes the WPF application and invokes the specified action on the UI thread.
        /// </summary>
        private static TResult InvokeDialogAction<TResult>(Func<TResult> callback)
        {
            // Initialize the WPF application if necessary, otherwise just invoke the callback.
            if (!appInitialized.IsSet)
            {
                appThread = new(() =>
                {
                    app = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
                    app.Startup += (_, _) =>
                    {
                        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                        appInitialized.Set();
                    };
                    app.Run();
                });
                appThread.SetApartmentState(ApartmentState.STA);
                appThread.IsBackground = true;
                appThread.Start();
                appInitialized.Wait();
            }
            return app!.Dispatcher.Invoke(callback);
        }

        /// <summary>
        /// Dialog lookup table for dispatching to the correct dialog based on the style and type.
        /// </summary>
        private static readonly ReadOnlyDictionary<DialogStyle, ReadOnlyDictionary<DialogType, Func<BaseOptions, BaseState?, IDialogBase>>> dialogDispatcher = new(new Dictionary<DialogStyle, ReadOnlyDictionary<DialogType, Func<BaseOptions, BaseState?, IDialogBase>>>
        {
            {
                DialogStyle.Classic, new(new Dictionary<DialogType, Func<BaseOptions, BaseState?, IDialogBase>>
                {
                    { DialogType.CloseAppsDialog, (options, state) => new Dialogs.Classic.CloseAppsDialog((CloseAppsDialogOptions)options, (CloseAppsDialogState)state!) },
                    { DialogType.CustomDialog, (options, state) => new Dialogs.Classic.CustomDialog((CustomDialogOptions)options) },
                    { DialogType.InputDialog, (options, state) => new Dialogs.Classic.InputDialog((InputDialogOptions)options) },
                    { DialogType.ProgressDialog, (options, state) => new Dialogs.Classic.ProgressDialog((ProgressDialogOptions)options) },
                    { DialogType.RestartDialog, (options, state) => new Dialogs.Classic.RestartDialog((RestartDialogOptions)options) },
                })
            },
            {
                DialogStyle.Fluent, new(new Dictionary<DialogType, Func<BaseOptions, BaseState?, IDialogBase>>
                {
                    { DialogType.CloseAppsDialog, (options, state) => new Dialogs.Fluent.CloseAppsDialog((CloseAppsDialogOptions)options, (CloseAppsDialogState)state!) },
                    { DialogType.CustomDialog, (options, state) => new Dialogs.Fluent.CustomDialog((CustomDialogOptions)options) },
                    { DialogType.InputDialog, (options, state) => new Dialogs.Fluent.InputDialog((InputDialogOptions)options) },
                    { DialogType.ProgressDialog, (options, state) => new Dialogs.Fluent.ProgressDialog((ProgressDialogOptions)options) },
                    { DialogType.RestartDialog, (options, state) => new Dialogs.Fluent.RestartDialog((RestartDialogOptions)options) },
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
