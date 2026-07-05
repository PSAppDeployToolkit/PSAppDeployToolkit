namespace PSADT.ClientServer
{
    /// <summary>
    /// Defines the available commands that can be sent between the server and client processes.
    /// </summary>
    internal enum PipeCommand
    {
        /// <summary>
        /// Opens the client-server communication channel.
        /// </summary>
        Open = 0,

        /// <summary>
        /// Closes the client-server communication channel.
        /// </summary>
        Close = 1,

        /// <summary>
        /// Initializes the close applications dialog with process definitions.
        /// </summary>
        InitCloseAppsDialog = 2,

        /// <summary>
        /// Prompts the user to close running applications.
        /// </summary>
        PromptToCloseApps = 3,

        /// <summary>
        /// Displays a modal dialog of a specified type.
        /// </summary>
        ShowModalDialog = 4,

        /// <summary>
        /// Displays a progress dialog.
        /// </summary>
        ShowProgressDialog = 5,

        /// <summary>
        /// Checks if a progress dialog is currently open.
        /// </summary>
        ProgressDialogOpen = 6,

        /// <summary>
        /// Updates an existing progress dialog.
        /// </summary>
        UpdateProgressDialog = 7,

        /// <summary>
        /// Closes the progress dialog.
        /// </summary>
        CloseProgressDialog = 8,

        /// <summary>
        /// Creates a new notification icon.
        /// </summary>
        ShowNotifyIcon = 9,

        /// <summary>
        /// Checks if a notification icon is currently open.
        /// </summary>
        NotifyIconOpen = 10,

        /// <summary>
        /// Updates an existing notification icon.
        /// </summary>
        UpdateNotifyIcon = 11,

        /// <summary>
        /// Displays a balloon tip notification.
        /// </summary>
        ShowBalloonTip = 12,

        /// <summary>
        /// Closes the active notification icon.
        /// </summary>
        CloseNotifyIcon = 13,

        /// <summary>
        /// Minimizes all windows on the desktop.
        /// </summary>
        MinimizeAllWindows = 14,

        /// <summary>
        /// Restores all minimized windows.
        /// </summary>
        RestoreAllWindows = 15,

        /// <summary>
        /// Sends keystrokes to a window.
        /// </summary>
        SendKeys = 16,

        /// <summary>
        /// Retrieves information about process windows.
        /// </summary>
        GetProcessWindowInfo = 17,

        /// <summary>
        /// Refreshes the desktop and environment variables.
        /// </summary>
        RefreshDesktopAndEnvironmentVariables = 18,

        /// <summary>
        /// Gets the current user notification state.
        /// </summary>
        GetUserNotificationState = 19,

        /// <summary>
        /// Gets the process ID of the foreground window.
        /// </summary>
        GetForegroundWindowProcessId = 20,

        /// <summary>
        /// Gets the value of an environment variable.
        /// </summary>
        GetEnvironmentVariable = 21,

        /// <summary>
        /// Sets the value of an environment variable.
        /// </summary>
        SetEnvironmentVariable = 22,

        /// <summary>
        /// Removes an environment variable.
        /// </summary>
        RemoveEnvironmentVariable = 23,

        /// <summary>
        /// Performs a ShellExecuteEx invocation for the user.
        /// </summary>
        ShellExecuteProcess = 24,

        /// <summary>
        /// Gets the current focus mode state for the user.
        /// </summary>
        GetUserFocusModeState = 25,

        /// <summary>
        /// Gets the current toast notification mode for the user.
        /// </summary>
        GetUserToastNotificationMode = 26,
    }
}
