namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies the different commands that can be used to show a window in a Windows environment.
    /// </summary>
    /// <remarks>This enumeration provides a set of constants that define how a window should be shown.  These
    /// commands are typically used with functions that manipulate window visibility and state,  such as showing,
    /// hiding, minimizing, or maximizing a window. Each command corresponds to a  specific action or state change for a
    /// window.</remarks>
    public enum SHOW_WINDOW_CMD
    {
        SW_HIDE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_HIDE,
        SW_SHOWNORMAL = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNORMAL,
        SW_NORMAL = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_NORMAL,
        SW_SHOWMINIMIZED = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMINIMIZED,
        SW_SHOWMAXIMIZED = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED,
        SW_MAXIMIZE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MAXIMIZE,
        SW_SHOWNOACTIVATE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE,
        SW_SHOW = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOW,
        SW_MINIMIZE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MINIMIZE,
        SW_SHOWMINNOACTIVE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE,
        SW_SHOWNA = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNA,
        SW_RESTORE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE,
        SW_SHOWDEFAULT = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWDEFAULT,
        SW_FORCEMINIMIZE = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_FORCEMINIMIZE,
        SW_MAX = Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MAX,
    }
}
