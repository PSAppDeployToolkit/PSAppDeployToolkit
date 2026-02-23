namespace PSADT.Interop
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
    internal enum WM_NCHITTEST : long
    {
        /// <summary>
        /// In the border of a window that does not have a sizing border.
        /// </summary>
        HTBORDER = Windows.Win32.PInvoke.HTBORDER,

        /// <summary>
        /// In the lower-horizontal border of a resizable window (the user can click the mouse to resize the window vertically).
        /// </summary>
        HTBOTTOM = Windows.Win32.PInvoke.HTBOTTOM,

        /// <summary>
        /// In the lower-left corner of a border of a resizable window (the user can click the mouse to resize the window diagonally).
        /// </summary>
        HTBOTTOMLEFT = Windows.Win32.PInvoke.HTBOTTOMLEFT,

        /// <summary>
        /// In the lower-right corner of a border of a resizable window (the user can click the mouse to resize the window diagonally).
        /// </summary>
        HTBOTTOMRIGHT = Windows.Win32.PInvoke.HTBOTTOMRIGHT,

        /// <summary>
        /// In a title bar.
        /// </summary>
        HTCAPTION = Windows.Win32.PInvoke.HTCAPTION,

        /// <summary>
        /// In a client area.
        /// </summary>
        HTCLIENT = Windows.Win32.PInvoke.HTCLIENT,

        /// <summary>
        /// In a Close button.
        /// </summary>
        HTCLOSE = Windows.Win32.PInvoke.HTCLOSE,

        /// <summary>
        /// On the screen background or on a dividing line between windows (same as HTNOWHERE, except that the DefWindowProc function produces a system beep to indicate an error).
        /// </summary>
        HTERROR = Windows.Win32.PInvoke.HTERROR,

        /// <summary>
        /// In a size box (same as HTSIZE).
        /// </summary>
        HTGROWBOX = Windows.Win32.PInvoke.HTGROWBOX,

        /// <summary>
        /// In a Help button.
        /// </summary>
        HTHELP = Windows.Win32.PInvoke.HTHELP,

        /// <summary>
        /// In a horizontal scroll bar.
        /// </summary>
        HTHSCROLL = Windows.Win32.PInvoke.HTHSCROLL,

        /// <summary>
        /// In the left border of a resizable window (the user can click the mouse to resize the window horizontally).
        /// </summary>
        HTLEFT = Windows.Win32.PInvoke.HTLEFT,

        /// <summary>
        /// In a menu.
        /// </summary>
        HTMENU = Windows.Win32.PInvoke.HTMENU,

        /// <summary>
        /// In a Maximize button.
        /// </summary>
        HTMAXBUTTON = Windows.Win32.PInvoke.HTMAXBUTTON,

        /// <summary>
        /// In a Minimize button.
        /// </summary>
        HTMINBUTTON = Windows.Win32.PInvoke.HTMINBUTTON,

        /// <summary>
        /// On the screen background or on a dividing line between windows.
        /// </summary>
        HTNOWHERE = Windows.Win32.PInvoke.HTNOWHERE,

        /// <summary>
        /// In a Minimize button.
        /// </summary>
        HTREDUCE = Windows.Win32.PInvoke.HTREDUCE,

        /// <summary>
        /// In the right border of a resizable window (the user can click the mouse to resize the window horizontally).
        /// </summary>
        HTRIGHT = Windows.Win32.PInvoke.HTRIGHT,

        /// <summary>
        /// In a size box (same as HTGROWBOX).
        /// </summary>
        HTSIZE = Windows.Win32.PInvoke.HTSIZE,

        /// <summary>
        /// In a window menu or in a Close button in a child window.
        /// </summary>
        HTSYSMENU = Windows.Win32.PInvoke.HTSYSMENU,

        /// <summary>
        /// In the upper-horizontal border of a window.
        /// </summary>
        HTTOP = Windows.Win32.PInvoke.HTTOP,

        /// <summary>
        /// In the upper-left corner of a window border.
        /// </summary>
        HTTOPLEFT = Windows.Win32.PInvoke.HTTOPLEFT,

        /// <summary>
        /// In the upper-right corner of a window border.
        /// </summary>
        HTTOPRIGHT = Windows.Win32.PInvoke.HTTOPRIGHT,

        /// <summary>
        /// In a window currently covered by another window in the same thread (the message will be sent to underlying windows in the same thread until one of them returns a code that is not HTTRANSPARENT).
        /// </summary>
        HTTRANSPARENT = Windows.Win32.PInvoke.HTTRANSPARENT,

        /// <summary>
        /// In the vertical scroll bar.
        /// </summary>
        HTVSCROLL = Windows.Win32.PInvoke.HTVSCROLL,

        /// <summary>
        /// In a Maximize button.
        /// </summary>
        HTZOOM = Windows.Win32.PInvoke.HTZOOM,
    }
}
