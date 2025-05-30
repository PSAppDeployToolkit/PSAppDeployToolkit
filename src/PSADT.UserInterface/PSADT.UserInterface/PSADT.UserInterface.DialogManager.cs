using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using PSADT.LibraryInterfaces;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Dialogs;
using PSADT.Utilities;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Static class to manage WPF dialogs within a console application.
    /// </summary>
    public static class DialogManager
    {
        /// <summary>
        /// Displays a dialog prompting the user to close specific applications.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options specifying the applications to be closed and other dialog configurations.</param>
        /// <returns>A string representing the user's response or selection from the dialog.</returns>
        public static string ShowCloseAppsDialog(DialogStyle dialogStyle, CloseAppsDialogOptions options)
        {
            bool stopProcessService = false;
            if (null != options.RunningProcessService && !options.RunningProcessService.IsRunning)
            {
                options.RunningProcessService.Start();
                stopProcessService = true;
            }
            var result = ShowModalDialog<string>(DialogType.CloseAppsDialog, dialogStyle, options);
            if (stopProcessService)
            {
                options.RunningProcessService!.Stop();
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
        public static string ShowCustomDialog(DialogStyle dialogStyle, CustomDialogOptions options) => ShowModalDialog<string>(DialogType.CustomDialog, dialogStyle, options);

        /// <summary>
        /// Displays an input dialog box with the specified style and options, and returns the result.
        /// </summary>
        /// <remarks>Use this method to prompt the user for input in a modal dialog. The dialog's behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options for configuring the input dialog, such as the prompt text, default value, and validation rules.</param>
        /// <returns>An <see cref="InputDialogResult"/> object containing the user's input and the dialog result (e.g., OK or Cancel).</returns>
        public static InputDialogResult ShowInputDialog(DialogStyle dialogStyle, InputDialogOptions options) => ShowModalDialog<InputDialogResult>(DialogType.InputDialog, dialogStyle, options);

        /// <summary>
        /// Displays a modal dialog prompting the user to restart the application.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">Options that configure the restart dialog, such as title, message, and button labels.</param>
        /// <returns>A string representing the user's response to the dialog. The value depends on the implementation of the dialog and the options provided.</returns>
        public static string ShowRestartDialog(DialogStyle dialogStyle, RestartDialogOptions options) => ShowModalDialog<string>(DialogType.RestartDialog, dialogStyle, options);

        /// <summary>
        /// Displays a progress dialog with the specified style and options.
        /// </summary>
        /// <remarks>This method initializes and displays a progress dialog based on the provided style and options.  Only one progress dialog can be displayed at a time. Attempting to open a new dialog while another is active will result in an exception.</remarks>
        /// <param name="dialogStyle">The style of the dialog to display. This determines the visual appearance and behavior of the progress dialog.</param>
        /// <param name="options">The configuration options for the progress dialog, such as title, message, and progress settings.</param>
        /// <exception cref="InvalidOperationException">Thrown if a progress dialog is already open. Ensure the current progress dialog is closed before attempting to open a new one.</exception>
        public static void ShowProgressDialog(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            if (progressInitialized.IsSet)
            {
                throw new InvalidOperationException("A progress dialog is already open. Close it before opening a new one.");
            }
            InvokeDialogAction(() =>
            {
                progressDialog = (IProgressDialog)dialogDispatcher[dialogStyle][DialogType.ProgressDialog](options);
                progressDialog.Show();
            });
            progressInitialized.Set();
        }

        /// <summary>
        /// Determines whether the progress dialog is currently open.
        /// </summary>
        /// <remarks>This method checks the internal state to determine if the progress dialog has been initialized and is currently displayed.</remarks>
        /// <returns><see langword="true"/> if the progress dialog is open; otherwise, <see langword="false"/>.</returns>
        public static bool ProgressDialogOpen()
        {
            return progressInitialized.IsSet;
        }

        /// <summary>
        /// Updates the messages and optional progress percentage in the currently displayed Progress dialog.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressDetailMessage">Optional new detail message.</param>
        /// <param name="progressPercentage">Optional progress percentage (0-100). If provided, the progress bar becomes determinate.</param>
        /// <param name="messageAlignment">Optional message alignment. If provided, the message alignment is updated.</param>
        public static void UpdateProgressDialog(string? progressMessage = null, string? progressDetailMessage = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            if (!progressInitialized.IsSet)
            {
                throw new InvalidOperationException("No progress dialog is currently open.");
            }
            InvokeDialogAction(() =>
            {
                progressDialog!.UpdateProgress(progressMessage, progressDetailMessage, progressPercentage, messageAlignment);
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
        internal static TResult ShowModalDialog<TResult>(DialogType dialogType, DialogStyle dialogStyle, BaseOptions options)
        {
            return (TResult)InvokeDialogAction(() =>
            {
                using (var dialog = (IModalDialog)dialogDispatcher[dialogStyle][dialogType](options))
                {
                    dialog.ShowDialog();
                    return dialog.DialogResult;
                }
            });
        }

        /// <summary>
        /// Displays a balloon tip notification in the system tray with the specified title, text, and icon.
        /// </summary>
        /// <remarks>This method sets the AppUserModelID for the current process to ensure compatibility with Windows 10 toast notifications. It also updates the registry with the provided application title and icon to correct stale information from previous runs. The balloon tip is displayed for a default duration of 7 seconds or until the user closes it.</remarks>
        /// <param name="TrayTitle">The title of the application to associate with the notification. This is used to set the AppUserModelID for Windows toast notifications.</param>
        /// <param name="TrayIcon">The file path or resource identifier of the icon to display in the system tray and notification.</param>
        /// <param name="BalloonTipTitle">The title of the balloon tip notification.</param>
        /// <param name="BalloonTipText">The text content of the balloon tip notification.</param>
        /// <param name="BalloonTipIcon">The icon to display in the balloon tip, such as <see cref="System.Windows.Forms.ToolTipIcon.Info"/> or <see cref="System.Windows.Forms.ToolTipIcon.Error"/>.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task completes when the balloon tip is closed.</returns>
        public static void ShowBalloonTip(string TrayTitle, string TrayIcon, string BalloonTipTitle, string BalloonTipText, System.Windows.Forms.ToolTipIcon BalloonTipIcon)
        {
            // Set the AUMID for this process so the Windows 10 toast has the correct title.
            Shell32.SetCurrentProcessExplicitAppUserModelID(TrayTitle);

            // Correct the registry data for the AUMID. This can reference stale info from a previous run.
            var regKey = $@"{(AccountUtilities.CallerIsAdmin ? @"HKEY_CLASSES_ROOT" : @"HKEY_CURRENT_USER\Software\Classes")}\AppUserModelId\{TrayTitle}";
            Registry.SetValue(regKey, "DisplayName", TrayTitle, RegistryValueKind.String);
            Registry.SetValue(regKey, "IconUri", TrayIcon, RegistryValueKind.ExpandString);

            // Create a new NotifyIcon instance and set its properties. We don't
            // have this in a using statement because if disposal occurs too soon,
            // the resulting toast notification on Windows 10/11 renders incorrectly.
            // The NotifyIcon object will still be disposed at some point, either
            // by the garbage collector, or when our BalloonTipClosed event fires.
            System.Windows.Forms.NotifyIcon notifyIcon = new()
            {
                Icon = Dialogs.Classic.ClassicAssets.GetIcon(TrayIcon),
                BalloonTipTitle = BalloonTipTitle,
                BalloonTipText = BalloonTipText,
                BalloonTipIcon = BalloonTipIcon,
                Visible = true,
            };
            notifyIcon.BalloonTipClosed += (s, _) => ((System.Windows.Forms.NotifyIcon?)s)?.Dispose();
            notifyIcon.ShowBalloonTip(7000); // Default timeout for a Windows 10 toast is 7 seconds.
        }

        /// <summary>
        /// Displays a message box with the specified title, prompt, and options.
        /// </summary>
        /// <remarks>The behavior and appearance of the message box are determined by the properties of the <paramref name="options"/> parameter.</remarks>ews
        /// <param name="options">The options for configuring the message box, such as title, message text, buttons, icon, default button, topmost behavior, and expiry duration.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value indicating the button that was clicked by the user.</returns>
        public static MessageBoxResult ShowMessageBox(DialogBoxOptions options)
        {
            return ShowMessageBox(options.AppTitle, options.MessageText, options.DialogButtons, options.DialogDefaultButton, options.DialogIcon, options.DialogTopMost, options.DialogExpiryDuration);
        }

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
        internal static MessageBoxResult ShowMessageBox(string Title, string Prompt, MessageBoxButtons Buttons, MessageBoxDefaultButton DefaultButton, MessageBoxIcon Icon, bool TopMost, TimeSpan Timeout)
        {
            return (MessageBoxResult)ShowMessageBox(Title, Prompt, (MESSAGEBOX_STYLE)Buttons | (MESSAGEBOX_STYLE)Icon | (MESSAGEBOX_STYLE)DefaultButton | (TopMost ? MESSAGEBOX_STYLE.MB_SYSTEMMODAL | MESSAGEBOX_STYLE.MB_TOPMOST | MESSAGEBOX_STYLE.MB_SETFOREGROUND : 0), Timeout);
        }

        /// <summary>
        /// Displays a message box with the specified prompt, buttons, and title.
        /// </summary>
        /// <param name="Title">The title text to display in the message box's title bar.</param>
        /// <param name="Prompt">The text to display in the message box.</param>
        /// <param name="Options">A <see cref="MESSAGEBOX_STYLE"/> value that specifies the buttons and icons to display in the message box.</param>
        /// <param name="Timeout">An optional <see cref="TimeSpan"/> value that specifies the duration after which the message box will automatically close. If not specified, the message box will remain open until the user interacts with it.</param>
        /// <returns>A <see cref="MESSAGEBOX_RESULT"/> value that indicates which button the user clicked in the message box.</returns>
        internal static MESSAGEBOX_RESULT ShowMessageBox(string Title, string Prompt, MESSAGEBOX_STYLE Options, TimeSpan Timeout = default)
        {
            return (MESSAGEBOX_RESULT)InvokeDialogAction(() => User32.MessageBoxTimeout(IntPtr.Zero, Prompt, Title, Options, 0, Timeout));
        }

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
        internal static MESSAGEBOX_RESULT ShowTaskBox(string Title, string Subtitle, string Prompt, TASKDIALOG_COMMON_BUTTON_FLAGS Buttons, TASKDIALOG_ICON Icon)
        {
            return (MESSAGEBOX_RESULT)InvokeDialogAction(() => ComCtl32.TaskDialog(HWND.Null, HINSTANCE.Null, Title, Subtitle, Prompt, Buttons, Icon));
        }

        /// <summary>
        /// Initializes the WPF application and invokes the specified action on the UI thread.
        /// </summary>
        private static object InvokeDialogAction(Delegate callback)
        {
            // Initialize the WPF application if necessary, otherwise just invoke the callback.
            if (!appInitialized.IsSet)
            {
                appThread = new Thread(() =>
                {
                    app = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
                    app.Startup += (_, _) => appInitialized.Set();
                    app.Run();
                });
                appThread.SetApartmentState(ApartmentState.STA);
                appThread.IsBackground = true;
                appThread.Start();
                appInitialized.Wait();
            }
            return app!.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, callback);
        }

        /// <summary>
        /// Dialog lookup table for dispatching to the correct dialog based on the style and type.
        /// </summary>
        private static readonly ReadOnlyDictionary<DialogStyle, ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>> dialogDispatcher = new(new Dictionary<DialogStyle, ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>>
        {
            {
                DialogStyle.Classic, new ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>(new Dictionary<DialogType, Func<BaseOptions, IDialogBase>>
                {
                    { DialogType.CloseAppsDialog, options => new Dialogs.Classic.CloseAppsDialog((CloseAppsDialogOptions)options) },
                    { DialogType.CustomDialog, options => new Dialogs.Classic.CustomDialog((CustomDialogOptions)options) },
                    { DialogType.InputDialog, options => new Dialogs.Classic.InputDialog((InputDialogOptions)options) },
                    { DialogType.ProgressDialog, options => new Dialogs.Classic.ProgressDialog((ProgressDialogOptions)options) },
                    { DialogType.RestartDialog, options => new Dialogs.Classic.RestartDialog((RestartDialogOptions)options) },
                })
            },
            {
                DialogStyle.Fluent, new ReadOnlyDictionary<DialogType, Func<BaseOptions, IDialogBase>>(new Dictionary<DialogType, Func<BaseOptions, IDialogBase>>
                {
                    { DialogType.CloseAppsDialog, options => new Dialogs.Fluent.CloseAppsDialog((CloseAppsDialogOptions)options) },
                    { DialogType.CustomDialog, options => new Dialogs.Fluent.CustomDialog((CustomDialogOptions)options) },
                    { DialogType.InputDialog, options => new Dialogs.Fluent.InputDialog((InputDialogOptions)options) },
                    { DialogType.ProgressDialog, options => new Dialogs.Fluent.ProgressDialog((ProgressDialogOptions)options) },
                    { DialogType.RestartDialog, options => new Dialogs.Fluent.RestartDialog((RestartDialogOptions)options) },
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
