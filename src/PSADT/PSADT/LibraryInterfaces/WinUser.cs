namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies the different commands that can be used to show a window in a Windows environment.
    /// </summary>
    /// <remarks>This enumeration provides a set of constants that define how a window should be shown.  These
    /// commands are typically used with functions that manipulate window visibility and state,  such as showing,
    /// hiding, minimizing, or maximizing a window. Each command corresponds to a specific action or state change for a
    /// window.</remarks>
    public enum SHOW_WINDOW_CMD
    {
        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        SW_HIDE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_HIDE,

        /// <summary>
        /// Activates and displays a window. If the window is minimized, maximized, or arranged, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
        /// </summary>
        SW_SHOWNORMAL = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNORMAL,
        SW_NORMAL = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_NORMAL,

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        SW_SHOWMINIMIZED = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMINIMIZED,

        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>
        SW_SHOWMAXIMIZED = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED,
        SW_MAXIMIZE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MAXIMIZE,

        /// <summary>
        /// Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated.
        /// </summary>
        SW_SHOWNOACTIVATE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE,

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        SW_SHOW = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOW,

        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        SW_MINIMIZE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MINIMIZE,

        /// <summary>
        /// Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
        /// </summary>
        SW_SHOWMINNOACTIVE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE,

        /// <summary>
        /// Displays the window in its current size and position. This value is similar to SW_SHOW, except that the window is not activated.
        /// </summary>
        SW_SHOWNA = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNA,

        /// <summary>
        /// Activates and displays the window. If the window is minimized, maximized, or arranged, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
        /// </summary>
        SW_RESTORE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE,

        /// <summary>
        /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
        /// </summary>
        SW_SHOWDEFAULT = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWDEFAULT,

        /// <summary>
        /// Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
        /// </summary>
        SW_FORCEMINIMIZE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_FORCEMINIMIZE,

        /// <summary>
        /// The count of all available SHOW_WINDOW_CMD values.
        /// </summary>
        SW_MAX = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MAX,
    }
}
