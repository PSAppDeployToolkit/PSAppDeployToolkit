namespace PSADT.Interop
{
    /// <summary>
    /// Represents system command values used in Windows messages to perform various window operations.
    /// </summary>
    /// <remarks>The <see cref="WM_SYSCOMMAND"/> enumeration defines constants that specify system commands 
    /// sent to a window when the user selects a command from the window menu or when the user presses a key that is
    /// mapped to a system command. These commands can be used to control window behavior such as closing, minimizing,
    /// maximizing, and more.</remarks>
    internal enum WM_SYSCOMMAND : uint
    {
        /// <summary>
        /// Closes the window.
        /// </summary>
        SC_CLOSE = Windows.Win32.PInvoke.SC_CLOSE,

        /// <summary>
        /// Changes the cursor to a question mark with a pointer. If the user then clicks a control in the dialog box, the control receives a WM_HELP message.
        /// </summary>
        SC_CONTEXTHELP = Windows.Win32.PInvoke.SC_CONTEXTHELP,

        /// <summary>
        /// Selects the default item; the user double-clicked the window menu.
        /// </summary>
        SC_DEFAULT = Windows.Win32.PInvoke.SC_DEFAULT,

        /// <summary>
        /// Activates the window associated with the application-specified hot key. The lParam parameter identifies the window to activate.
        /// </summary>
        SC_HOTKEY = Windows.Win32.PInvoke.SC_HOTKEY,

        /// <summary>
        /// Scrolls horizontally.
        /// </summary>
        SC_HSCROLL = Windows.Win32.PInvoke.SC_HSCROLL,

        /// <summary>
        /// Indicates whether the screen saver is secure.
        /// </summary>
        SCF_ISSECURE = Windows.Win32.PInvoke.SCF_ISSECURE,

        /// <summary>
        /// Retrieves the window menu as a result of a keystroke. For more information, see the Remarks section.
        /// </summary>
        SC_KEYMENU = Windows.Win32.PInvoke.SC_KEYMENU,

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        SC_MAXIMIZE = Windows.Win32.PInvoke.SC_MAXIMIZE,

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        SC_MINIMIZE = Windows.Win32.PInvoke.SC_MINIMIZE,

        /// <summary>
        /// Sets the state of the display. This command supports devices that have power-saving features, such as a battery-powered personal computer.
        /// </summary>
        SC_MONITORPOWER = Windows.Win32.PInvoke.SC_MONITORPOWER,

        /// <summary>
        /// Retrieves the window menu as a result of a mouse click.
        /// </summary>
        SC_MOUSEMENU = Windows.Win32.PInvoke.SC_MOUSEMENU,

        /// <summary>
        /// Moves the window
        /// </summary>
        SC_MOVE = Windows.Win32.PInvoke.SC_MOVE,

        /// <summary>
        /// Moves to the next window.
        /// </summary>
        SC_NEXTWINDOW = Windows.Win32.PInvoke.SC_NEXTWINDOW,

        /// <summary>
        /// Moves to the previous window.
        /// </summary>
        SC_PREVWINDOW = Windows.Win32.PInvoke.SC_PREVWINDOW,

        /// <summary>
        /// Restores the window to its normal position and size.
        /// </summary>
        SC_RESTORE = Windows.Win32.PInvoke.SC_RESTORE,

        /// <summary>
        /// Executes the screen saver application specified in the [boot] section of the System.ini file.
        /// </summary>
        SC_SCREENSAVE = Windows.Win32.PInvoke.SC_SCREENSAVE,

        /// <summary>
        /// Sizes the window.
        /// </summary>
        SC_SIZE = Windows.Win32.PInvoke.SC_SIZE,

        /// <summary>
        /// Activates the Start menu.
        /// </summary>
        SC_TASKLIST = Windows.Win32.PInvoke.SC_TASKLIST,

        /// <summary>
        /// Scrolls vertically.
        /// </summary>
        SC_VSCROLL = Windows.Win32.PInvoke.SC_VSCROLL,
    }
}
