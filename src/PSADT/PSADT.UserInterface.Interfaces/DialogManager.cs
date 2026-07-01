using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;
using PSADT.Utilities;
using PSADT.WindowManagement;
using PSAppDeployToolkit.Logging;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Interfaces
{
    /// <summary>
    /// Static class to manage WPF dialogs within a console application.
    /// </summary>
    [SuppressMessage("Usage", "VSTHRD001:Await JoinableTaskFactory.SwitchToMainThreadAsync() to switch to the UI thread instead of APIs that can deadlock or require specifying a priority", Justification = "DialogManager marshals to its own dedicated WPF dispatcher thread outside any JoinableTaskFactory context.")]
    [SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal static class DialogManager
    {
        /// <summary>
        /// Initializes the WPF application when the class is first accessed.
        /// </summary>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "These exceptions will never fire under normal, expected circumstances.")]
        [SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "An exception throwing in this event is truly exceptional, so we want it to propagate.")]
        [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "Access to System.Windows.Application.Current is appropriate here while we're setting up.")]
        static DialogManager()
        {
            // Set up the required dispatcher exception handler first. If it's not present, the setup is wrong and we won't proceed.
            Action<Exception> unhandledExceptionHandler = (Action<Exception>?)AppDomain.CurrentDomain.GetData("PSADT.UserInterface.DialogManager.UnhandledExceptionHandler") ?? throw new InvalidProgramException("Failed to initialize DialogManager: Unhandled exception handler not found in AppDomain data.");

            // Register process exit handler to ensure WPF is properly shut down. This prevents ~2.5 second delays during shutdown.
            // Use Dispatcher.InvokeShutdown() instead of Application.Shutdown() to avoid a race with WPF's
            // internal ManagedWndProcTracker, which has its own AppDomain shutdown listener that iterates
            // tracked window handles. Application.Shutdown() destroys windows (invalidating HWNDs) before
            // ManagedWndProcTracker runs, causing an unhandled Win32Exception ("Invalid window handle") in
            // PostMessage. InvokeShutdown() stops the dispatcher pump without destroying windows, letting
            // ManagedWndProcTracker clean them up safely.
            AppDomain.CurrentDomain.ProcessExit += static (_, _) => System.Windows.Application.Current?.Dispatcher.InvokeShutdown();

            // Configure WinForms modernisations here the NotifyIcon can create a IWin32Window which will make this throw if called afterwards.
            System.Windows.Forms.Application.EnableVisualStyles(); System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(defaultValue: false);

            // Force all WPF dialogs into software mode for remoting apps (https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues/1762)
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            // Create and start the WPF application thread.
            using ManualResetEvent dispatcherRunning = new(initialState: false);
            Exception? appThreadException = null;
            Thread appThread = new(() =>
            {
                try
                {
                    // Create the application and start the message pump (this will set dispatcherRunning when fully instantiated).
                    System.Windows.Application app = new() { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown, };
                    app.Dispatcher.UnhandledException += (_, e) => unhandledExceptionHandler(e.Exception);
                    app.Startup += async (_, _) =>
                    {
                        if (!await app.Dispatcher.InvokeAsync(dispatcherRunning.Set, System.Windows.Threading.DispatcherPriority.Normal, default))
                        {
                            throw new InvalidProgramException("Failed to signal that the WPF dispatcher is running.");
                        }
                    };
                    _ = app.Run();
                }
                catch (Exception exception) when (exception.Message is not null)
                {
                    // We capture the error for later rethrowing so that we can ensure the dispatcher is signaled to avoid deadlocks.
                    appThreadException = exception;
                    if (!dispatcherRunning.Set())
                    {
                        Environment.FailFast($"Failed to initialize WPF application and failed to signal dispatcher.{Environment.NewLine}Exception Info: {exception}", exception);
                    }
                }
            });
            appThread.SetApartmentState(ApartmentState.STA);
            appThread.Name = "WPF Dialog STA Thread";
            appThread.IsBackground = true;
            appThread.Start();

            // Confirm the validity of the application instance, then set the static reference.
            if (!dispatcherRunning.WaitOne())
            {
                throw new InvalidProgramException("Failed to initialize WPF application: Dispatcher failed to start.");
            }
            if (appThreadException is not null)
            {
                throw new InvalidProgramException("Failed to initialize WPF application: Dispatcher threw an exception.", appThreadException);
            }
            if (System.Windows.Application.Current?.Dispatcher is not System.Windows.Threading.Dispatcher dispatcher)
            {
                throw new InvalidProgramException("Failed to initialize WPF application: Application instance is null.");
            }

            // Refresh desktop icons to ensure any changes are reflected (https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues/1846).
            _ = dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, DesktopUtilities.RefreshDesktop);
        }

        /// <summary>
        /// Displays a dialog prompting the user to close specific applications.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options specifying the applications to be closed and other dialog configurations.</param>
        /// <param name="state">The current state of the dialog, including services for tracking running processes and logging.</param>
        /// <returns>A string representing the user's response or selection from the dialog.</returns>
        /// <exception cref="InvalidProgramException">Thrown if the WPF application fails to initialize or the dispatcher throws an exception.</exception>
        internal static async ValueTask<CloseAppsDialogResult> ShowCloseAppsDialogAsync(DialogStyle dialogStyle, CloseAppsDialogOptions options, CloseAppsDialogState state)
        {
            // Start the RunningProcessService if it is not already running.
            bool stopProcessService = false;
            if ((state.RunningProcessService?.IsRunning) is false)
            {
                state.RunningProcessService.Start();
                stopProcessService = true;
            }

            // Announce whether there's apps to close.
            IReadOnlyList<ProcessToClose>? processesToClose = null;
            if (state.RunningProcessService is not null)
            {
                if ((processesToClose = state.RunningProcessService.ProcessesToClose).Count is 0 && options.ContinueOnProcessClosure)
                {
                    // No processes are running and ContinueOnProcessClosure is set -> skip the dialog
                    // entirely. Avoids constructing a WPF window only to immediately close it (which
                    // also produced an InvalidOperationException prior to the CloseAppsDialog fix).
                    await state.LogAction("Previously detected running processes are no longer running.", LogSeverity.Info).ConfigureAwait(false);
                    if (stopProcessService)
                    {
                        await state.RunningProcessService.StopAsync().ConfigureAwait(false);
                    }
                    return CloseAppsDialogResult.Continue;
                }
                if (processesToClose.Count > 0)
                {
                    await state.LogAction($"Prompting the user to close application(s) ['{string.Join("', '", processesToClose.Select(static p => p.Description))}']...", LogSeverity.Info).ConfigureAwait(false);
                }
            }

            // Announce the current countdown information.
            if (options.CountdownDuration is not null)
            {
                TimeSpan? elapsed = options.CountdownDuration - state.CountdownStopwatch.Elapsed;
                if (elapsed < TimeSpan.Zero)
                {
                    elapsed = TimeSpan.Zero;
                }
                if (processesToClose?.Count > 0)
                {
                    await state.LogAction($"Close applications countdown has [{((int)Math.Ceiling(elapsed.Value.TotalSeconds)).ToString(CultureInfo.InvariantCulture)}] seconds remaining.", LogSeverity.Info).ConfigureAwait(false);
                }
                else
                {
                    await state.LogAction($"Countdown has [{((int)Math.Ceiling(elapsed.Value.TotalSeconds)).ToString(CultureInfo.InvariantCulture)}] seconds remaining.", LogSeverity.Info).ConfigureAwait(false);
                }
            }

            // Show the dialog and get the result.
            CloseAppsDialogResult result = await ShowModalDialogAsync<CloseAppsDialogResult>(DialogType.CloseAppsDialog, dialogStyle, options, state).ConfigureAwait(false);

            // Perform some result logging before returning.
            if (options.CountdownDuration is not null && (options.CountdownDuration - state.CountdownStopwatch.Elapsed) <= TimeSpan.Zero)
            {
                if (result.Equals(CloseAppsDialogResult.Close))
                {
                    await state.LogAction("Close application(s) countdown timer has elapsed. Force closing application(s).", LogSeverity.Info).ConfigureAwait(false);
                }
                else if (result.Equals(CloseAppsDialogResult.Defer))
                {
                    await state.LogAction("Countdown timer has elapsed and deferrals remaining. Force deferral.", LogSeverity.Info).ConfigureAwait(false);
                }
                else if (result.Equals(CloseAppsDialogResult.Continue))
                {
                    await state.LogAction("Countdown timer has elapsed and no processes running. Force continue.", LogSeverity.Info).ConfigureAwait(false);
                }
            }

            // If we started the RunningProcessService, stop it now before returning the result.
            if (stopProcessService)
            {
                if (state.RunningProcessService is null)
                {
                    throw new InvalidProgramException("Unexpected null RunningProcessService. This should never happen.");
                }
                await state.RunningProcessService.StopAsync().ConfigureAwait(false);
            }
            return result;
        }

        /// <summary>
        /// Displays a custom dialog with the specified style and options, and returns the result as a string.
        /// </summary>
        /// <remarks>This method displays a modal dialog of type <see cref="DialogType.CustomDialog"/>. The dialog's behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the dialog, such as title, message, and buttons.</param>
        /// <returns>A string representing the result of the dialog interaction. The value depends on the dialog's configuration and user input.</returns>
        internal static async ValueTask<CustomDialogResult> ShowCustomDialogAsync(DialogStyle dialogStyle, CustomDialogOptions options)
        {
            if (options.MinimizeWindows)
            {
                DesktopUtilities.MinimizeAllWindows();
            }
            try
            {
                return await ShowModalDialogAsync<CustomDialogResult>(DialogType.CustomDialog, dialogStyle, options).ConfigureAwait(false);
            }
            finally
            {
                if (options.MinimizeWindows)
                {
                    DesktopUtilities.RestoreAllWindows();
                }
            }
        }

        /// <summary>
        /// Displays a list selection dialog with the specified style and options, and returns the result.
        /// </summary>
        /// <remarks>This method displays a modal dialog of type <see cref="DialogType.ListSelectionDialog"/>. The dialog's
        /// behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the dialog, such as title, message, buttons, and list items.</param>
        /// <returns>A <see cref="ListSelectionDialogResult"/> object containing the button clicked and the selected list item.</returns>
        internal static async ValueTask<ListSelectionDialogResult> ShowListSelectionDialogAsync(DialogStyle dialogStyle, ListSelectionDialogOptions options)
        {
            if (options.MinimizeWindows)
            {
                DesktopUtilities.MinimizeAllWindows();
            }
            try
            {
                return await ShowModalDialogAsync<ListSelectionDialogResult>(DialogType.ListSelectionDialog, dialogStyle, options).ConfigureAwait(false);
            }
            finally
            {
                if (options.MinimizeWindows)
                {
                    DesktopUtilities.RestoreAllWindows();
                }
            }
        }

        /// <summary>
        /// Displays an input dialog box with the specified style and options, and returns the result.
        /// </summary>
        /// <remarks>Use this method to prompt the user for input in a modal dialog. The dialog's behavior and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options for configuring the input dialog, such as the prompt text, default value, and validation rules.</param>
        /// <returns>An <see cref="InputDialogResult"/> object containing the user's input and the dialog result (e.g., OK or Cancel).</returns>
        /// <exception cref="NotSupportedException">Thrown if the caller is using ServiceUI, as input dialogs are not supported in that context.</exception>
        internal static async ValueTask<InputDialogResult> ShowInputDialogAsync(DialogStyle dialogStyle, InputDialogOptions options)
        {
            if (AccountUtilities.CallerUsingServiceUI)
            {
                throw new NotSupportedException("The input dialog is only permitted when ServiceUI is not used to start the toolkit.");
            }
            if (options.MinimizeWindows)
            {
                DesktopUtilities.MinimizeAllWindows();
            }
            try
            {
                return await ShowModalDialogAsync<InputDialogResult>(DialogType.InputDialog, dialogStyle, options).ConfigureAwait(false);
            }
            finally
            {
                if (options.MinimizeWindows)
                {
                    DesktopUtilities.RestoreAllWindows();
                }
            }
        }

        /// <summary>
        /// Displays a modal dialog prompting the user to restart the application.
        /// </summary>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">Options that configure the restart dialog, such as title, message, and button labels.</param>
        /// <returns>A string representing the user's response to the dialog. The value depends on the implementation of the dialog and the options provided.</returns>
        internal static Task<IDialogResult> ShowRestartDialogAsync(DialogStyle dialogStyle, RestartDialogOptions options)
        {
            return ShowModalDialogAsync<IDialogResult>(DialogType.RestartDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays a progress dialog with the specified style and options.
        /// </summary>
        /// <remarks>This method initializes and displays a progress dialog based on the provided style and options. Only one progress dialog can be displayed at a time. Attempting to open a new dialog while another is active will result in an exception.</remarks>
        /// <param name="dialogStyle">The style of the dialog to display. This determines the visual appearance and behavior of the progress dialog.</param>
        /// <param name="options">The configuration options for the progress dialog, such as title, message, and progress settings.</param>
        /// <exception cref="InvalidOperationException">Thrown if a progress dialog is already open. Ensure the current progress dialog is closed before attempting to open a new one.</exception>
        internal static Task ShowProgressDialogAsync(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            return progressDialog is not null ? throw new InvalidOperationException("Cannot show a progress dialog while one is already open.") : InvokeDialogActionAsync(() =>
            {
                progressDialog = dialogStyle switch
                {
                    DialogStyle.Classic => new Classic.ProgressDialog(options),
                    DialogStyle.Fluent => new Fluent.ProgressDialog(options),
                    _ => throw new NotSupportedException($"Dialog style '{dialogStyle}' is not supported for dialog type 'ProgressDialog'."),
                };
                try
                {
                    progressDialog.Show();
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    using (progressDialog)
                    {
                        progressDialog = null;
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// Determines whether the progress dialog is currently open.
        /// </summary>
        /// <remarks>This method checks the internal state to determine if the progress dialog has been initialized and is currently displayed.</remarks>
        /// <returns><see langword="true"/> if the progress dialog is open; otherwise, <see langword="false"/>.</returns>
        internal static bool ProgressDialogOpen()
        {
            return progressDialog is not null;
        }

        /// <summary>
        /// Updates the messages and optional progress percentage in the currently displayed Progress dialog.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressDetailMessage">Optional new detail message.</param>
        /// <param name="progressPercentage">Optional progress percentage (0-100). If provided, the progress bar becomes determinate.</param>
        /// <param name="messageAlignment">Optional message alignment. If provided, the message alignment is updated.</param>
        /// <exception cref="InvalidOperationException">Thrown if no progress dialog is currently open. Ensure a progress dialog is displayed before attempting to update it.</exception>
        internal static Task UpdateProgressDialogAsync(string? progressMessage = null, string? progressDetailMessage = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            if (progressDialog is null)
            {
                throw new InvalidOperationException("Cannot update a progress dialog while one is not open.");
            }
            if (progressMessage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(progressMessage);
            }
            if (progressDetailMessage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(progressDetailMessage);
            }
            return InvokeDialogActionAsync(() => progressDialog.UpdateProgress(progressMessage, progressDetailMessage, progressPercentage, messageAlignment));
        }

        /// <summary>
        /// Closes the currently open dialog, if any.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no progress dialog is currently open. Ensure a progress dialog is displayed before attempting to close it.</exception>
        internal static Task CloseProgressDialogAsync()
        {
            return progressDialog is null ? throw new InvalidOperationException("Cannot close a progress dialog while one is not open.") : InvokeDialogActionAsync(() =>
            {
                try
                {
                    using (progressDialog)
                    {
                        progressDialog.CloseDialog();
                    }
                }
                finally
                {
                    progressDialog = null;
                }
            });
        }

        /// <summary>
        /// Displays a notify icon in the system tray with the specified icon and tooltip text.
        /// </summary>
        /// <param name="options">The configuration options for the notify icon, including title, icon, and tooltip text.</param>
        /// <exception cref="InvalidOperationException">A notify icon is already displayed.</exception>
        internal static Task ShowNotifyIconAsync(NotifyIconOptions options)
        {
            // Ensure there's not already a notify icon open.
            return notifyIcon is not null ? throw new InvalidOperationException("Cannot show a notify icon while one is already open.") : InvokeDialogActionAsync(async () =>
            {
                // Set the AUMID for this process so the Windows 10 toast has the correct title.
                _ = NativeMethods.SetCurrentProcessExplicitAppUserModelID(options.AppTitle);

                // Correct the registry data for the AUMID. This can reference stale info from a previous run.
                string appIconPath = options.AppTaskbarIconImage ?? options.AppIconImage;
                System.Drawing.Icon iconObj = await Classic.ClassicDialog.GetIconAsync(appIconPath).ConfigureAwait(true);
                string regKey = $@"{(AccountUtilities.CallerIsAdmin ? "HKEY_CLASSES_ROOT" : @"HKEY_CURRENT_USER\Software\Classes")}\AppUserModelId\{options.AppTitle}";
                Registry.SetValue(regKey, "DisplayName", options.AppTitle, RegistryValueKind.String);
                if (MiscUtilities.GetBase64StringBytes(appIconPath) is not null)
                {
                    string tempIcon = Path.Join(Path.GetTempPath(), "PSADT.UserInterface.TrayIcon.ico");
                    using FileStream fs = new(tempIcon, FileMode.Create, FileAccess.Write, FileShare.None);
                    iconObj.Save(fs); Registry.SetValue(regKey, "IconUri", tempIcon, RegistryValueKind.ExpandString);
                }
                else
                {
                    Registry.SetValue(regKey, "IconUri", appIconPath, RegistryValueKind.ExpandString);
                }
                notifyIcon = new() { Icon = iconObj, Text = options.MessageText, Visible = true, };
                notifyIcon.BalloonTipShown += static (_, _) => ClientServerUtilities.SetOperationSuccessFlag();
                notifyIcon.Click += static (sender, e) =>
                {
                    if (sender is not System.Windows.Forms.NotifyIcon icon)
                    {
                        throw new InvalidProgramException("Unexpected event sender type. Expected NotifyIcon.");
                    }
                    if (lastBalloonTip is not null)
                    {
                        icon.ShowBalloonTip(0, lastBalloonTip.Title, lastBalloonTip.Text, (System.Windows.Forms.ToolTipIcon)lastBalloonTip.Icon);
                    }
                };
            });
        }

        /// <summary>
        /// Determines whether the notify icon is open.
        /// </summary>
        /// <returns><see langword="true"/> if the notify icon is open; otherwise, <see langword="false"/>.</returns>
        internal static bool NotifyIconOpen()
        {
            return notifyIcon is not null;
        }

        /// <summary>
        /// Updates the text displayed in the notification area icon.
        /// </summary>
        /// <param name="messageText">The message text to display in the notification area icon.</param>
        /// <exception cref="InvalidOperationException">Thrown when no notify icon is currently open.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="messageText"/> is null or whitespace.</exception>
        internal static Task UpdateNotifyIconAsync(string messageText)
        {
            if (notifyIcon is null)
            {
                throw new InvalidOperationException("Cannot update a notify icon while one is not open.");
            }
            ArgumentException.ThrowIfNullOrWhiteSpace(messageText);
            return InvokeDialogActionAsync(() => notifyIcon.Text = messageText);
        }

        /// <summary>
        /// Displays a balloon tip notification using the current notify icon.
        /// </summary>
        /// <param name="options">The configuration options for the balloon tip, including title, text, and icon.</param>
        /// <exception cref="InvalidOperationException">Thrown when no notify icon is currently open.</exception>
        /// <exception cref="InvalidProgramException">Thrown if the notify icon becomes null during balloon tip cleanup.</exception>
        internal static Task ShowBalloonTipAsync(BalloonTipOptions options)
        {
            return notifyIcon is not null
                ? InvokeDialogActionAsync(() => { notifyIcon.ShowBalloonTip(0, options.Title, options.Text, (System.Windows.Forms.ToolTipIcon)options.Icon); lastBalloonTip = options; })
                : throw new InvalidOperationException("Cannot show a balloon tip while no notify icon is open.");
        }

        /// <summary>
        /// Closes and disposes the currently open notify icon.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no notify icon is currently open.</exception>
        internal static Task CloseNotifyIconAsync()
        {
            return notifyIcon is null ? throw new InvalidOperationException("Cannot close a notify icon while one is not open.") : InvokeDialogActionAsync(() =>
            {
                using (notifyIcon)
                {
                    lastBalloonTip = null;
                    notifyIcon = null;
                }
            });
        }

        /// <summary>
        /// Displays a modal dialog of the specified type and style, and returns the result of the dialog interaction.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the modal dialog.</typeparam>
        /// <param name="dialogType">Specifies the type of dialog to display. Determines the content and behavior of the modal dialog.</param>
        /// <param name="dialogStyle">Specifies the visual style to apply to the dialog, affecting its appearance and layout.</param>
        /// <param name="options">An object containing options that configure the dialog's behavior and appearance, such as title and
        /// available buttons. Cannot be null.</param>
        /// <param name="state">An optional object that holds state information for the dialog. This parameter must be provided when
        /// displaying a dialog of type 'CloseAppsDialog'.</param>
        /// <returns>The result of the dialog interaction, cast to the specified type parameter.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is null and <paramref name="dialogType"/> is <see
        /// cref="DialogType.CloseAppsDialog"/>.</exception>
        private static Task<TResult> ShowModalDialogAsync<TResult>(DialogType dialogType, DialogStyle dialogStyle, BaseDialogOptions options, BaseDialogState? state = null)
        {
            return InvokeDialogActionAsync(() =>
            {
                using IModalDialog dialog = (dialogStyle, dialogType) switch
                {
                    (DialogStyle.Classic, DialogType.CloseAppsDialog) => new Classic.CloseAppsDialog((CloseAppsDialogOptions)options, (CloseAppsDialogState?)state ?? throw new ArgumentNullException(nameof(state))),
                    (DialogStyle.Classic, DialogType.CustomDialog) => new Classic.CustomDialog((CustomDialogOptions)options),
                    (DialogStyle.Classic, DialogType.InputDialog) => new Classic.InputDialog((InputDialogOptions)options),
                    (DialogStyle.Classic, DialogType.ListSelectionDialog) => new Classic.ListSelectionDialog((ListSelectionDialogOptions)options),
                    (DialogStyle.Classic, DialogType.RestartDialog) => new Classic.RestartDialog((RestartDialogOptions)options),
                    (DialogStyle.Fluent, DialogType.CloseAppsDialog) => new Fluent.CloseAppsDialog((CloseAppsDialogOptions)options, (CloseAppsDialogState?)state ?? throw new ArgumentNullException(nameof(state))),
                    (DialogStyle.Fluent, DialogType.CustomDialog) => new Fluent.CustomDialog((CustomDialogOptions)options),
                    (DialogStyle.Fluent, DialogType.InputDialog) => new Fluent.InputDialog((InputDialogOptions)options),
                    (DialogStyle.Fluent, DialogType.ListSelectionDialog) => new Fluent.ListSelectionDialog((ListSelectionDialogOptions)options),
                    (DialogStyle.Fluent, DialogType.RestartDialog) => new Fluent.RestartDialog((RestartDialogOptions)options),
                    _ => throw new NotSupportedException($"Dialog style '{dialogStyle}' is not supported for dialog type '{dialogType}'."),
                };
                dialog.ShowDialog(); return (TResult)dialog.DialogResult;
            });
        }

        /// <summary>
        /// Displays a message box with the specified title, prompt, and options.
        /// </summary>
        /// <remarks>The behavior and appearance of the message box are determined by the properties of the <paramref name="options"/> parameter.</remarks>
        /// <param name="options">The options for configuring the message box, such as title, message text, buttons, icon, default button, topmost behavior, and expiry duration.</param>
        /// <returns>A <see cref="DialogBoxResult"/> value indicating the button that was clicked by the user.</returns>
        [SuppressMessage("Usage", "MA0099:Use Explicit enum value instead of 0", Justification = "There's no zero value for this enum.")]
        internal static ValueTask<DialogBoxResult> ShowDialogBoxAsync(DialogBoxOptions options)
        {
            return ShowDialogBoxAsync(options.AppTitle, options.MessageText, options.DialogButtons, options.DialogDefaultButton, options.DialogIcon ?? 0, options.DialogTopMost, options.DialogExpiryDuration);
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
        /// <returns>A <see cref="DialogBoxResult"/> value indicating the button clicked by the user.</returns>
        internal static async ValueTask<DialogBoxResult> ShowDialogBoxAsync(string Title, string Prompt, DialogBoxButtons Buttons, DialogBoxDefaultButton DefaultButton, DialogBoxIcon Icon, bool TopMost, uint Timeout)
        {
            return DialogBoxResult.FromMessageBoxResult(await ShowDialogBoxAsync(Title, Prompt, (MESSAGEBOX_STYLE)Buttons | (MESSAGEBOX_STYLE)Icon | (MESSAGEBOX_STYLE)DefaultButton | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_SETFOREGROUND | (TopMost ? MESSAGEBOX_STYLE.MB_SYSTEMMODAL | MESSAGEBOX_STYLE.MB_TOPMOST : MESSAGEBOX_STYLE.MB_OK), Timeout).ConfigureAwait(false));
        }

        /// <summary>
        /// Displays a message box with the specified prompt, buttons, and title.
        /// </summary>
        /// <param name="Title">The title text to display in the message box's title bar.</param>
        /// <param name="Prompt">The text to display in the message box.</param>
        /// <param name="Options">A MESSAGEBOX_RESULT value that specifies the buttons and icons to display in the message box.</param>
        /// <param name="Timeout">An optional <see cref="TimeSpan"/> value that specifies the duration after which the message box will automatically close. If not specified, the message box will remain open until the user interacts with it.</param>
        /// <returns>A MESSAGEBOX_RESULT value that indicates which button the user clicked in the message box.</returns>
        internal static Task<MESSAGEBOX_RESULT> ShowDialogBoxAsync(string Title, string Prompt, MESSAGEBOX_STYLE Options, uint Timeout = 0)
        {
            return InvokeDialogActionAsync(() =>
            {
                ClientServerUtilities.SetOperationSuccessFlag();
                return NativeMethods.MessageBoxTimeout(Prompt, Title, Options, Timeout);
            });
        }

        /// <summary>
        /// Displays a task dialog box with the specified title, subtitle, prompt, buttons, and icon.
        /// </summary>
        /// <remarks>This method internally invokes a task dialog using the Windows Common Controls library. The dialog is modal and blocks execution until the user interacts with it.</remarks>
        /// <param name="Title">The title of the task dialog box. This appears in the title bar of the dialog.</param>
        /// <param name="Subtitle">The subtitle of the task dialog box. This appears as a header in the dialog.</param>
        /// <param name="Prompt">The main prompt or message displayed in the dialog box.</param>
        /// <param name="Buttons">A combination of flags specifying the buttons to display in the dialog. This must be a valid TASKDIALOG_COMMON_BUTTON_FLAGS value.</param>
        /// <param name="Icon">The icon to display in the dialog box. This must be a valid <see cref="TASKDIALOG_ICON"/> value.</param>
        /// <returns>A MESSAGEBOX_RESULT value indicating the button that the user clicked to close the dialog.</returns>
        [SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "This remains here for a potential feature in the future.")]
        private static Task<MESSAGEBOX_RESULT> ShowTaskBoxAsync(string Title, string Subtitle, string Prompt, TASKDIALOG_COMMON_BUTTON_FLAGS Buttons, TASKDIALOG_ICON Icon)
        {
            return InvokeDialogActionAsync(() =>
            {
                ClientServerUtilities.SetOperationSuccessFlag();
                return NativeMethods.TaskDialog(Title, Subtitle, Prompt, Buttons, Icon);
            });
        }

        /// <summary>
        /// Displays the Help Console dialog with the specified options.
        /// </summary>
        /// <remarks>This method invokes the Help Console dialog using the provided <paramref
        /// name="options"/> to customize its appearance and functionality.</remarks>
        /// <param name="options">The configuration options for the Help Console dialog, including display settings and behavior.</param>
        /// <returns>A <see cref="DialogBoxResult"/> value indicating the result of the dialog interaction.</returns>
        internal static Task<DialogBoxResult> ShowHelpConsoleAsync(HelpConsoleOptions options)
        {
            return InvokeDialogActionAsync(() =>
            {
                using Classic.HelpConsole helpConsole = new(options);
                _ = helpConsole.ShowDialog();
                return DialogBoxResult.OK;
            });
        }

        /// <summary>
        /// Sends keystrokes to the specified window asynchronously.
        /// </summary>
        /// <param name="options">Options specifying the target window handle and the keys to send.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">The target window is disabled, possibly due to a modal dialog being displayed.</exception>
        internal static Task SendKeysAsync(SendKeysOptions options)
        {
            return InvokeDialogActionAsync(() =>
            {
                HWND hwnd = (HWND)options.WindowHandle;
                WindowTools.BringWindowToFront(hwnd);
                if (!NativeMethods.IsWindowEnabled(hwnd))
                {
                    throw new InvalidOperationException("Unable to send keys to window because it may be disabled due to a modal dialog being shown.");
                }
                System.Windows.Forms.SendKeys.SendWait(options.Keys);
            });
        }

        /// <summary>
        /// Invokes the specified action on the WPF UI thread.
        /// </summary>
        /// <param name="callback">The action to invoke on the WPF UI thread.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "This is our safe implementation.")]
        private static Task InvokeDialogActionAsync(Action callback)
        {
            return System.Windows.Application.Current.Dispatcher.InvokeAsync(callback, System.Windows.Threading.DispatcherPriority.Normal, default).Task;
        }

        /// <summary>
        /// Invokes the specified asynchronous action on the WPF UI thread.
        /// </summary>
        /// <param name="callback">The asynchronous action to invoke on the WPF UI thread.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "This is our safe implementation.")]
        private static Task InvokeDialogActionAsync(Func<Task> callback)
        {
            return System.Windows.Application.Current.Dispatcher.InvokeAsync(callback, System.Windows.Threading.DispatcherPriority.Normal, default).Task.Unwrap();
        }

        /// <summary>
        /// Invokes the specified function on the WPF UI thread.
        /// </summary>
        /// <param name="callback">The function to invoke on the WPF UI thread.</param>
        /// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
        /// <returns>A task that represents the asynchronous operation, containing the result of the function.</returns>
        [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "This is our safe implementation.")]
        private static Task<TResult> InvokeDialogActionAsync<TResult>(Func<TResult> callback)
        {
            return System.Windows.Application.Current.Dispatcher.InvokeAsync(callback, System.Windows.Threading.DispatcherPriority.Normal, default).Task;
        }

        /// <summary>
        /// The currently open Progress dialog, if any. Null if no dialog is open.
        /// </summary>
        private static IProgressDialog? progressDialog;

        /// <summary>
        /// The currently active NotifyIcon for balloon tips.
        /// </summary>
        private static System.Windows.Forms.NotifyIcon? notifyIcon;

        /// <summary>
        /// A cached value of the last balloon tip options used.
        /// </summary>
        private static BalloonTipOptions? lastBalloonTip;
    }
}
