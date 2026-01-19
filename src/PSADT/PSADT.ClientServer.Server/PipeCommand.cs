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
        Open,

        /// <summary>
        /// Closes the client-server communication channel.
        /// </summary>
        Close,

        /// <summary>
        /// Initializes the close applications dialog with process definitions.
        /// </summary>
        InitCloseAppsDialog,

        /// <summary>
        /// Prompts the user to close running applications.
        /// </summary>
        PromptToCloseApps,

        /// <summary>
        /// Displays a modal dialog of a specified type.
        /// </summary>
        ShowModalDialog,

        /// <summary>
        /// Displays a progress dialog.
        /// </summary>
        ShowProgressDialog,

        /// <summary>
        /// Checks if a progress dialog is currently open.
        /// </summary>
        ProgressDialogOpen,

        /// <summary>
        /// Updates an existing progress dialog.
        /// </summary>
        UpdateProgressDialog,

        /// <summary>
        /// Closes the progress dialog.
        /// </summary>
        CloseProgressDialog,

        /// <summary>
        /// Displays a balloon tip notification.
        /// </summary>
        ShowBalloonTip,

        /// <summary>
        /// Minimizes all windows on the desktop.
        /// </summary>
        MinimizeAllWindows,

        /// <summary>
        /// Restores all minimized windows.
        /// </summary>
        RestoreAllWindows,

        /// <summary>
        /// Sends keystrokes to a window.
        /// </summary>
        SendKeys,

        /// <summary>
        /// Retrieves information about process windows.
        /// </summary>
        GetProcessWindowInfo,

        /// <summary>
        /// Refreshes the desktop and environment variables.
        /// </summary>
        RefreshDesktopAndEnvironmentVariables,

        /// <summary>
        /// Gets the current user notification state.
        /// </summary>
        GetUserNotificationState,

        /// <summary>
        /// Gets the process ID of the foreground window.
        /// </summary>
        GetForegroundWindowProcessId,

        /// <summary>
        /// Gets the value of an environment variable.
        /// </summary>
        GetEnvironmentVariable,

        /// <summary>
        /// Sets the value of an environment variable.
        /// </summary>
        SetEnvironmentVariable,

        /// <summary>
        /// Removes an environment variable.
        /// </summary>
        RemoveEnvironmentVariable,
    }
}
