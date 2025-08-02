using System;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies the different commands that can be used to show a window in a Windows environment.
    /// </summary>
    /// <remarks>This enumeration provides a set of constants that define how a window should be shown. These
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

    /// <summary>
    /// Represents the Windows message identifiers used in the Windows API for handling various system and application
    /// events.
    /// </summary>
    /// <remarks>The <see cref="WINDOW_MESSAGE"/> enumeration defines a comprehensive set of message constants
    /// that are used by the Windows operating system to communicate with applications. These messages cover a wide
    /// range of events, including window creation, destruction, input handling, and system commands. Each message is
    /// associated with a unique identifier, which is used to determine the type of event or action that has occurred. 
    /// This enumeration is primarily used in the context of Windows message loops, where applications process incoming
    /// messages to perform appropriate actions. The messages are categorized into several groups, such as core window
    /// messages, non-client area messages, keyboard and input method editor (IME) messages, dialog and command
    /// messages, mouse messages, and more. Developers typically handle these messages in a window procedure function,
    /// which processes each message and executes the corresponding application logic. Understanding and correctly
    /// handling these messages is crucial for developing responsive and well-behaved Windows applications.</remarks>
    internal enum WINDOW_MESSAGE : uint
    {
        // Core Window Messages (0x0000–0x0055).
        WM_NULL = Windows.Win32.PInvoke.WM_NULL,
        WM_CREATE = Windows.Win32.PInvoke.WM_CREATE,
        WM_DESTROY = Windows.Win32.PInvoke.WM_DESTROY,
        WM_MOVE = Windows.Win32.PInvoke.WM_MOVE,
        WM_SIZE = Windows.Win32.PInvoke.WM_SIZE,
        WM_ACTIVATE = Windows.Win32.PInvoke.WM_ACTIVATE,
        WM_SETFOCUS = Windows.Win32.PInvoke.WM_SETFOCUS,
        WM_KILLFOCUS = Windows.Win32.PInvoke.WM_KILLFOCUS,
        WM_ENABLE = Windows.Win32.PInvoke.WM_ENABLE,
        WM_SETREDRAW = Windows.Win32.PInvoke.WM_SETREDRAW,
        WM_SETTEXT = Windows.Win32.PInvoke.WM_SETTEXT,
        WM_GETTEXT = Windows.Win32.PInvoke.WM_GETTEXT,
        WM_GETTEXTLENGTH = Windows.Win32.PInvoke.WM_GETTEXTLENGTH,
        WM_PAINT = Windows.Win32.PInvoke.WM_PAINT,
        WM_CLOSE = Windows.Win32.PInvoke.WM_CLOSE,
        WM_QUERYENDSESSION = Windows.Win32.PInvoke.WM_QUERYENDSESSION,
        WM_QUIT = Windows.Win32.PInvoke.WM_QUIT,
        WM_QUERYOPEN = Windows.Win32.PInvoke.WM_QUERYOPEN,
        WM_ERASEBKGND = Windows.Win32.PInvoke.WM_ERASEBKGND,
        WM_SYSCOLORCHANGE = Windows.Win32.PInvoke.WM_SYSCOLORCHANGE,
        WM_ENDSESSION = Windows.Win32.PInvoke.WM_ENDSESSION,
        WM_SHOWWINDOW = Windows.Win32.PInvoke.WM_SHOWWINDOW,
        WM_CTLCOLOR = Windows.Win32.PInvoke.WM_CTLCOLOR,
        WM_WININICHANGE = Windows.Win32.PInvoke.WM_WININICHANGE,
        WM_SETTINGCHANGE = Windows.Win32.PInvoke.WM_SETTINGCHANGE,
        WM_DEVMODECHANGE = Windows.Win32.PInvoke.WM_DEVMODECHANGE,
        WM_ACTIVATEAPP = Windows.Win32.PInvoke.WM_ACTIVATEAPP,
        WM_FONTCHANGE = Windows.Win32.PInvoke.WM_FONTCHANGE,
        WM_TIMECHANGE = Windows.Win32.PInvoke.WM_TIMECHANGE,
        WM_CANCELMODE = Windows.Win32.PInvoke.WM_CANCELMODE,
        WM_SETCURSOR = Windows.Win32.PInvoke.WM_SETCURSOR,
        WM_MOUSEACTIVATE = Windows.Win32.PInvoke.WM_MOUSEACTIVATE,
        WM_CHILDACTIVATE = Windows.Win32.PInvoke.WM_CHILDACTIVATE,
        WM_QUEUESYNC = Windows.Win32.PInvoke.WM_QUEUESYNC,
        WM_GETMINMAXINFO = Windows.Win32.PInvoke.WM_GETMINMAXINFO,
        WM_PAINTICON = Windows.Win32.PInvoke.WM_PAINTICON,
        WM_ICONERASEBKGND = Windows.Win32.PInvoke.WM_ICONERASEBKGND,
        WM_NEXTDLGCTL = Windows.Win32.PInvoke.WM_NEXTDLGCTL,
        WM_SPOOLERSTATUS = Windows.Win32.PInvoke.WM_SPOOLERSTATUS,
        WM_DRAWITEM = Windows.Win32.PInvoke.WM_DRAWITEM,
        WM_MEASUREITEM = Windows.Win32.PInvoke.WM_MEASUREITEM,
        WM_DELETEITEM = Windows.Win32.PInvoke.WM_DELETEITEM,
        WM_VKEYTOITEM = Windows.Win32.PInvoke.WM_VKEYTOITEM,
        WM_CHARTOITEM = Windows.Win32.PInvoke.WM_CHARTOITEM,
        WM_SETFONT = Windows.Win32.PInvoke.WM_SETFONT,
        WM_GETFONT = Windows.Win32.PInvoke.WM_GETFONT,
        WM_SETHOTKEY = Windows.Win32.PInvoke.WM_SETHOTKEY,
        WM_GETHOTKEY = Windows.Win32.PInvoke.WM_GETHOTKEY,
        WM_QUERYDRAGICON = Windows.Win32.PInvoke.WM_QUERYDRAGICON,
        WM_COMPAREITEM = Windows.Win32.PInvoke.WM_COMPAREITEM,
        WM_COMPACTING = Windows.Win32.PInvoke.WM_COMPACTING,
        WM_WINDOWPOSCHANGING = Windows.Win32.PInvoke.WM_WINDOWPOSCHANGING,
        WM_WINDOWPOSCHANGED = Windows.Win32.PInvoke.WM_WINDOWPOSCHANGED,
        WM_POWER = Windows.Win32.PInvoke.WM_POWER,
        WM_COPYDATA = Windows.Win32.PInvoke.WM_COPYDATA,
        WM_CANCELJOURNAL = Windows.Win32.PInvoke.WM_CANCELJOURNAL,
        WM_NOTIFY = Windows.Win32.PInvoke.WM_NOTIFY,
        WM_INPUTLANGCHANGEREQUEST = Windows.Win32.PInvoke.WM_INPUTLANGCHANGEREQUEST,
        WM_INPUTLANGCHANGE = Windows.Win32.PInvoke.WM_INPUTLANGCHANGE,
        WM_TCARD = Windows.Win32.PInvoke.WM_TCARD,
        WM_HELP = Windows.Win32.PInvoke.WM_HELP,
        WM_USERCHANGED = Windows.Win32.PInvoke.WM_USERCHANGED,
        WM_NOTIFYFORMAT = Windows.Win32.PInvoke.WM_NOTIFYFORMAT,

        // Non‑Client Area Messages (0x0081–0x00A9).
        WM_NCCREATE = Windows.Win32.PInvoke.WM_NCCREATE,
        WM_NCDESTROY = Windows.Win32.PInvoke.WM_NCDESTROY,
        WM_NCCALCSIZE = Windows.Win32.PInvoke.WM_NCCALCSIZE,
        WM_NCHITTEST = Windows.Win32.PInvoke.WM_NCHITTEST,
        WM_NCPAINT = Windows.Win32.PInvoke.WM_NCPAINT,
        WM_NCACTIVATE = Windows.Win32.PInvoke.WM_NCACTIVATE,
        WM_GETDLGCODE = Windows.Win32.PInvoke.WM_GETDLGCODE,
        WM_NCMOUSEMOVE = Windows.Win32.PInvoke.WM_NCMOUSEMOVE,
        WM_NCLBUTTONDOWN = Windows.Win32.PInvoke.WM_NCLBUTTONDOWN,
        WM_NCLBUTTONUP = Windows.Win32.PInvoke.WM_NCLBUTTONUP,
        WM_NCLBUTTONDBLCLK = Windows.Win32.PInvoke.WM_NCLBUTTONDBLCLK,
        WM_NCRBUTTONDOWN = Windows.Win32.PInvoke.WM_NCRBUTTONDOWN,
        WM_NCRBUTTONUP = Windows.Win32.PInvoke.WM_NCRBUTTONUP,
        WM_NCRBUTTONDBLCLK = Windows.Win32.PInvoke.WM_NCRBUTTONDBLCLK,
        WM_NCMBUTTONDOWN = Windows.Win32.PInvoke.WM_NCMBUTTONDOWN,
        WM_NCMBUTTONUP = Windows.Win32.PInvoke.WM_NCMBUTTONUP,
        WM_NCMBUTTONDBLCLK = Windows.Win32.PInvoke.WM_NCMBUTTONDBLCLK,

        // Keyboard & IME Messages (0x0100–0x010F).
        WM_KEYFIRST = Windows.Win32.PInvoke.WM_KEYFIRST,
        WM_KEYDOWN = Windows.Win32.PInvoke.WM_KEYDOWN,
        WM_KEYUP = Windows.Win32.PInvoke.WM_KEYUP,
        WM_CHAR = Windows.Win32.PInvoke.WM_CHAR,
        WM_DEADCHAR = Windows.Win32.PInvoke.WM_DEADCHAR,
        WM_SYSKEYDOWN = Windows.Win32.PInvoke.WM_SYSKEYDOWN,
        WM_SYSKEYUP = Windows.Win32.PInvoke.WM_SYSKEYUP,
        WM_SYSCHAR = Windows.Win32.PInvoke.WM_SYSCHAR,
        WM_SYSDEADCHAR = Windows.Win32.PInvoke.WM_SYSDEADCHAR,
        WM_KEYLAST = Windows.Win32.PInvoke.WM_KEYLAST,
        WM_IME_STARTCOMPOSITION = Windows.Win32.PInvoke.WM_IME_STARTCOMPOSITION,
        WM_IME_ENDCOMPOSITION = Windows.Win32.PInvoke.WM_IME_ENDCOMPOSITION,
        WM_IME_COMPOSITION = Windows.Win32.PInvoke.WM_IME_COMPOSITION,
        WM_IME_KEYLAST = Windows.Win32.PInvoke.WM_IME_KEYLAST,

        // Dialog, Command & System‑Control (0x0110–0x0138).
        WM_INITDIALOG = Windows.Win32.PInvoke.WM_INITDIALOG,
        WM_COMMAND = Windows.Win32.PInvoke.WM_COMMAND,
        WM_SYSCOMMAND = Windows.Win32.PInvoke.WM_SYSCOMMAND,
        WM_TIMER = Windows.Win32.PInvoke.WM_TIMER,
        WM_HSCROLL = Windows.Win32.PInvoke.WM_HSCROLL,
        WM_VSCROLL = Windows.Win32.PInvoke.WM_VSCROLL,
        WM_INITMENU = Windows.Win32.PInvoke.WM_INITMENU,
        WM_INITMENUPOPUP = Windows.Win32.PInvoke.WM_INITMENUPOPUP,
        WM_MENUSELECT = Windows.Win32.PInvoke.WM_MENUSELECT,
        WM_MENUCHAR = Windows.Win32.PInvoke.WM_MENUCHAR,
        WM_ENTERIDLE = Windows.Win32.PInvoke.WM_ENTERIDLE,
        WM_CTLCOLORMSGBOX = Windows.Win32.PInvoke.WM_CTLCOLORMSGBOX,
        WM_CTLCOLOREDIT = Windows.Win32.PInvoke.WM_CTLCOLOREDIT,
        WM_CTLCOLORLISTBOX = Windows.Win32.PInvoke.WM_CTLCOLORLISTBOX,
        WM_CTLCOLORBTN = Windows.Win32.PInvoke.WM_CTLCOLORBTN,
        WM_CTLCOLORDLG = Windows.Win32.PInvoke.WM_CTLCOLORDLG,
        WM_CTLCOLORSCROLLBAR = Windows.Win32.PInvoke.WM_CTLCOLORSCROLLBAR,
        WM_CTLCOLORSTATIC = Windows.Win32.PInvoke.WM_CTLCOLORSTATIC,

        // Mouse Messages (0x0200–0x020E).
        WM_MOUSEFIRST = Windows.Win32.PInvoke.WM_MOUSEFIRST,
        WM_MOUSEMOVE = Windows.Win32.PInvoke.WM_MOUSEMOVE,
        WM_LBUTTONDOWN = Windows.Win32.PInvoke.WM_LBUTTONDOWN,
        WM_LBUTTONUP = Windows.Win32.PInvoke.WM_LBUTTONUP,
        WM_LBUTTONDBLCLK = Windows.Win32.PInvoke.WM_LBUTTONDBLCLK,
        WM_RBUTTONDOWN = Windows.Win32.PInvoke.WM_RBUTTONDOWN,
        WM_RBUTTONUP = Windows.Win32.PInvoke.WM_RBUTTONUP,
        WM_RBUTTONDBLCLK = Windows.Win32.PInvoke.WM_RBUTTONDBLCLK,
        WM_MBUTTONDOWN = Windows.Win32.PInvoke.WM_MBUTTONDOWN,
        WM_MBUTTONUP = Windows.Win32.PInvoke.WM_MBUTTONUP,
        WM_MBUTTONDBLCLK = Windows.Win32.PInvoke.WM_MBUTTONDBLCLK,
        WM_MOUSEWHEEL = Windows.Win32.PInvoke.WM_MOUSEWHEEL,
        WM_MOUSEHWHEEL = Windows.Win32.PInvoke.WM_MOUSEHWHEEL,

        // Other Window & MDI Notifications (0x0210–0x0234).
        WM_PARENTNOTIFY = Windows.Win32.PInvoke.WM_PARENTNOTIFY,
        WM_ENTERMENULOOP = Windows.Win32.PInvoke.WM_ENTERMENULOOP,
        WM_EXITMENULOOP = Windows.Win32.PInvoke.WM_EXITMENULOOP,
        WM_NEXTMENU = Windows.Win32.PInvoke.WM_NEXTMENU,
        WM_SIZING = Windows.Win32.PInvoke.WM_SIZING,
        WM_CAPTURECHANGED = Windows.Win32.PInvoke.WM_CAPTURECHANGED,
        WM_MOVING = Windows.Win32.PInvoke.WM_MOVING,
        WM_POWERBROADCAST = Windows.Win32.PInvoke.WM_POWERBROADCAST,
        WM_DEVICECHANGE = Windows.Win32.PInvoke.WM_DEVICECHANGE,
        WM_MDICREATE = Windows.Win32.PInvoke.WM_MDICREATE,
        WM_MDIDESTROY = Windows.Win32.PInvoke.WM_MDIDESTROY,
        WM_MDIACTIVATE = Windows.Win32.PInvoke.WM_MDIACTIVATE,
        WM_MDIRESTORE = Windows.Win32.PInvoke.WM_MDIRESTORE,
        WM_MDINEXT = Windows.Win32.PInvoke.WM_MDINEXT,
        WM_MDIMAXIMIZE = Windows.Win32.PInvoke.WM_MDIMAXIMIZE,
        WM_MDITILE = Windows.Win32.PInvoke.WM_MDITILE,
        WM_MDICASCADE = Windows.Win32.PInvoke.WM_MDICASCADE,
        WM_MDIICONARRANGE = Windows.Win32.PInvoke.WM_MDIICONARRANGE,
        WM_MDIGETACTIVE = Windows.Win32.PInvoke.WM_MDIGETACTIVE,
        WM_MDISETMENU = Windows.Win32.PInvoke.WM_MDISETMENU,
        WM_ENTERSIZEMOVE = Windows.Win32.PInvoke.WM_ENTERSIZEMOVE,
        WM_EXITSIZEMOVE = Windows.Win32.PInvoke.WM_EXITSIZEMOVE,
        WM_DROPFILES = Windows.Win32.PInvoke.WM_DROPFILES,
        WM_MDIREFRESHMENU = Windows.Win32.PInvoke.WM_MDIREFRESHMENU,

        // IME Control & Hover/Leave (0x0281–0x02A3).
        WM_IME_SETCONTEXT = Windows.Win32.PInvoke.WM_IME_SETCONTEXT,
        WM_IME_NOTIFY = Windows.Win32.PInvoke.WM_IME_NOTIFY,
        WM_IME_CONTROL = Windows.Win32.PInvoke.WM_IME_CONTROL,
        WM_IME_COMPOSITIONFULL = Windows.Win32.PInvoke.WM_IME_COMPOSITIONFULL,
        WM_IME_SELECT = Windows.Win32.PInvoke.WM_IME_SELECT,
        WM_IME_CHAR = Windows.Win32.PInvoke.WM_IME_CHAR,
        WM_IME_KEYDOWN = Windows.Win32.PInvoke.WM_IME_KEYDOWN,
        WM_IME_KEYUP = Windows.Win32.PInvoke.WM_IME_KEYUP,
        WM_MOUSEHOVER = Windows.Win32.PInvoke.WM_MOUSEHOVER,
        WM_NCMOUSELEAVE = Windows.Win32.PInvoke.WM_NCMOUSELEAVE,
        WM_MOUSELEAVE = Windows.Win32.PInvoke.WM_MOUSELEAVE,

        // Clipboard & Printing (0x0300–0x0318).
        WM_CUT = Windows.Win32.PInvoke.WM_CUT,
        WM_COPY = Windows.Win32.PInvoke.WM_COPY,
        WM_PASTE = Windows.Win32.PInvoke.WM_PASTE,
        WM_CLEAR = Windows.Win32.PInvoke.WM_CLEAR,
        WM_UNDO = Windows.Win32.PInvoke.WM_UNDO,
        WM_RENDERFORMAT = Windows.Win32.PInvoke.WM_RENDERFORMAT,
        WM_RENDERALLFORMATS = Windows.Win32.PInvoke.WM_RENDERALLFORMATS,
        WM_DESTROYCLIPBOARD = Windows.Win32.PInvoke.WM_DESTROYCLIPBOARD,
        WM_DRAWCLIPBOARD = Windows.Win32.PInvoke.WM_DRAWCLIPBOARD,
        WM_PAINTCLIPBOARD = Windows.Win32.PInvoke.WM_PAINTCLIPBOARD,
        WM_VSCROLLCLIPBOARD = Windows.Win32.PInvoke.WM_VSCROLLCLIPBOARD,
        WM_SIZECLIPBOARD = Windows.Win32.PInvoke.WM_SIZECLIPBOARD,
        WM_ASKCBFORMATNAME = Windows.Win32.PInvoke.WM_ASKCBFORMATNAME,
        WM_CHANGECBCHAIN = Windows.Win32.PInvoke.WM_CHANGECBCHAIN,
        WM_HSCROLLCLIPBOARD = Windows.Win32.PInvoke.WM_HSCROLLCLIPBOARD,
        WM_QUERYNEWPALETTE = Windows.Win32.PInvoke.WM_QUERYNEWPALETTE,
        WM_PALETTEISCHANGING = Windows.Win32.PInvoke.WM_PALETTEISCHANGING,
        WM_PALETTECHANGED = Windows.Win32.PInvoke.WM_PALETTECHANGED,
        WM_HOTKEY = Windows.Win32.PInvoke.WM_HOTKEY,
        WM_PRINT = Windows.Win32.PInvoke.WM_PRINT,
        WM_PRINTCLIENT = Windows.Win32.PInvoke.WM_PRINTCLIENT,

        // Handheld, Pen & DDE (0x0358–0x03E8).
        WM_HANDHELDFIRST = Windows.Win32.PInvoke.WM_HANDHELDFIRST,
        WM_HANDHELDLAST = Windows.Win32.PInvoke.WM_HANDHELDLAST,
        WM_PENWINFIRST = Windows.Win32.PInvoke.WM_PENWINFIRST,
        WM_PENWINLAST = Windows.Win32.PInvoke.WM_PENWINLAST,
        WM_DDE_FIRST = Windows.Win32.PInvoke.WM_DDE_FIRST,
        WM_DDE_INITIATE = Windows.Win32.PInvoke.WM_DDE_INITIATE,
        WM_DDE_TERMINATE = Windows.Win32.PInvoke.WM_DDE_TERMINATE,
        WM_DDE_ADVISE = Windows.Win32.PInvoke.WM_DDE_ADVISE,
        WM_DDE_UNADVISE = Windows.Win32.PInvoke.WM_DDE_UNADVISE,
        WM_DDE_ACK = Windows.Win32.PInvoke.WM_DDE_ACK,
        WM_DDE_DATA = Windows.Win32.PInvoke.WM_DDE_DATA,
        WM_DDE_REQUEST = Windows.Win32.PInvoke.WM_DDE_REQUEST,
        WM_DDE_POKE = Windows.Win32.PInvoke.WM_DDE_POKE,
        WM_DDE_EXECUTE = Windows.Win32.PInvoke.WM_DDE_EXECUTE,
        WM_DDE_LAST = Windows.Win32.PInvoke.WM_DDE_LAST,

        // Private & Application‑Defined Ranges.
        WM_USER = Windows.Win32.PInvoke.WM_USER,
        WM_APP = Windows.Win32.PInvoke.WM_APP,
    }

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

    /// <summary>
    /// Represents a type of resource used in Windows applications, such as bitmaps, icons, or menus.
    /// </summary>
    /// <remarks>This struct provides a set of predefined resource types that correspond to standard Windows
    /// resource types. It includes operators for comparison and conversion to and from <see cref="IntPtr"/>.</remarks>
    internal readonly record struct RESOURCE_TYPE
    {
        /// <summary>
        /// Accelerator table.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ACCELERATOR = Windows.Win32.PInvoke.RT_ACCELERATOR;

        /// <summary>
        /// Animated cursor.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ANICURSOR = Windows.Win32.PInvoke.RT_ANICURSOR;

        /// <summary>
        /// Animated icon.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ANIICON = Windows.Win32.PInvoke.RT_ANIICON;

        /// <summary>
        /// Bitmap resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_BITMAP = Windows.Win32.PInvoke.RT_BITMAP;

        /// <summary>
        /// Hardware-dependent cursor resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_CURSOR = Windows.Win32.PInvoke.RT_CURSOR;

        /// <summary>
        /// Dialog box.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_DIALOG = Windows.Win32.PInvoke.RT_DIALOG;

        /// <summary>
        /// Allows a resource editing tool to associate a string with an .rc file. Typically, the string is the name of the header file that provides symbolic names. The resource compiler parses the string but otherwise ignores the value.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_DLGINCLUDE = Windows.Win32.PInvoke.RT_DLGINCLUDE;

        /// <summary>
        /// Font resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_FONT = Windows.Win32.PInvoke.RT_FONT;

        /// <summary>
        /// Font directory resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_FONTDIR = Windows.Win32.PInvoke.RT_FONTDIR;

        /// <summary>
        /// Hardware-independent cursor resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_GROUP_CURSOR = Windows.Win32.PInvoke.RT_GROUP_CURSOR;

        /// <summary>
        /// Hardware-independent icon resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_GROUP_ICON = Windows.Win32.PInvoke.RT_GROUP_ICON;

        /// <summary>
        /// HTML resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_HTML = Windows.Win32.PInvoke.RT_HTML;

        /// <summary>
        /// Hardware-dependent icon resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_ICON = Windows.Win32.PInvoke.RT_ICON;

        /// <summary>
        /// Side-by-Side Assembly Manifest.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_MANIFEST = Windows.Win32.PInvoke.RT_MANIFEST;

        /// <summary>
        /// Menu resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_MENU = Windows.Win32.PInvoke.RT_MENU;

        /// <summary>
        /// Message-table entry.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_MESSAGETABLE = Windows.Win32.PInvoke.RT_MESSAGETABLE;

        /// <summary>
        /// Plug and Play resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_PLUGPLAY = Windows.Win32.PInvoke.RT_PLUGPLAY;

        /// <summary>
        /// Application-defined resource (raw data).
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_RCDATA = Windows.Win32.PInvoke.RT_RCDATA;

        /// <summary>
        /// String-table entry.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_STRING = Windows.Win32.PInvoke.RT_STRING;

        /// <summary>
        /// Version resource.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_VERSION = Windows.Win32.PInvoke.RT_VERSION;

        /// <summary>
        /// VXD.
        /// </summary>
        internal static readonly RESOURCE_TYPE RT_VXD = Windows.Win32.PInvoke.RT_VXD;

        /// <summary>
        /// Initializes a new instance of the <see cref="RESOURCE_TYPE"/> class with the specified handle.
        /// </summary>
        /// <param name="value">The handle to be associated with this instance.</param>
        private RESOURCE_TYPE(IntPtr value)
        {
            Value = value;
        }

        /// <summary>
        /// Converts a <see cref="RESOURCE_TYPE"/> instance to an <see cref="IntPtr"/>.
        /// </summary>
        /// <param name="h">The <see cref="RESOURCE_TYPE"/> instance to convert.</param>
        public static explicit operator IntPtr(RESOURCE_TYPE h)
        {
            return h.Value;
        }

        /// <summary>
        /// Converts a <see cref="RESOURCE_TYPE"/> instance to an <see cref="PCWSTR"/>.
        /// </summary>
        /// <param name="h">The <see cref="RESOURCE_TYPE"/> instance to convert.</param>
        public unsafe static explicit operator PCWSTR(RESOURCE_TYPE h)
        {
            return (PCWSTR)h.Value.ToPointer();
        }

        /// <summary>
        /// Converts a <see cref="RESOURCE_TYPE"/> instance to an <see cref="uint"/>.
        /// </summary>
        /// <param name="h">The <see cref="RESOURCE_TYPE"/> instance to convert.</param>
        public static explicit operator uint(RESOURCE_TYPE h)
        {
            return (uint)h.Value;
        }

        /// <summary>
        /// Converts an <see cref="IntPtr"/> to a <see cref="RESOURCE_TYPE"/>.
        /// </summary>
        /// <param name="h">The handle represented as an <see cref="IntPtr"/> to be converted.</param>
        public static implicit operator RESOURCE_TYPE(IntPtr h)
        {
            return new RESOURCE_TYPE(h);
        }

        /// <summary>
        /// Converts an <see cref="PCWSTR"/> to a <see cref="RESOURCE_TYPE"/>.
        /// </summary>
        /// <param name="h">The handle represented as an <see cref="PCWSTR"/> to be converted.</param>
        public unsafe static implicit operator RESOURCE_TYPE(PCWSTR h)
        {
            return new RESOURCE_TYPE((IntPtr)h.Value);
        }

        /// <summary>
        /// Converts an <see cref="uint"/> to a <see cref="RESOURCE_TYPE"/>.
        /// </summary>
        /// <param name="h">The handle represented as an <see cref="uint"/> to be converted.</param>
        public static implicit operator RESOURCE_TYPE(uint h)
        {
            return new RESOURCE_TYPE((IntPtr)h);
        }

        /// <summary>
        /// Determines whether two specified objects, an <see cref="IntPtr"/> and a <see cref="RESOURCE_TYPE"/>, are not
        /// equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The <see cref="RESOURCE_TYPE"/> object to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is not equal to <paramref name="h1"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(IntPtr h1, RESOURCE_TYPE h2)
        {
            return h1 != h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects are equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The second resource type to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is equal to <paramref name="h1"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool operator ==(IntPtr h1, RESOURCE_TYPE h2)
        {
            return h1 == h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects, an <see cref="PCWSTR"/> and a <see cref="RESOURCE_TYPE"/>, are not
        /// equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The <see cref="RESOURCE_TYPE"/> object to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is not equal to <paramref name="h1"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public unsafe static bool operator !=(PCWSTR h1, RESOURCE_TYPE h2)
        {
            return (IntPtr)h1.Value != h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects are equal.
        /// </summary>
        /// <param name="h1">The first pointer to compare.</param>
        /// <param name="h2">The second resource type to compare, which contains a handle.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is equal to <paramref name="h1"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public unsafe static bool operator ==(PCWSTR h1, RESOURCE_TYPE h2)
        {
            return (IntPtr)h1.Value == h2.Value;
        }

        /// <summary>
        /// Determines whether two specified objects, a <see cref="uint"/> and a <see cref="RESOURCE_TYPE"/>, are not
        /// equal.
        /// </summary>
        /// <param name="h1">The first operand, a 32-bit unsigned integer representing a resource handle.</param>
        /// <param name="h2">The second operand, a <see cref="RESOURCE_TYPE"/> object containing a resource handle.</param>
        /// <returns><see langword="true"/> if the handle represented by <paramref name="h1"/> is not equal to the handle
        /// contained in <paramref name="h2"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(uint h1, RESOURCE_TYPE h2)
        {
            return (IntPtr)h1 != h2;
        }

        /// <summary>
        /// Determines whether the specified <see cref="uint"/> and <see cref="RESOURCE_TYPE"/> are equal.
        /// </summary>
        /// <param name="h1">The unsigned integer handle to compare.</param>
        /// <param name="h2">The <see cref="RESOURCE_TYPE"/> instance to compare.</param>
        /// <returns><see langword="true"/> if the handle of <paramref name="h2"/> is equal to <paramref name="h1"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool operator ==(uint h1, RESOURCE_TYPE h2)
        {
            return (IntPtr)h1 == h2;
        }

        /// <summary>
        /// Represents a handle to a system resource.
        /// </summary>
        /// <remarks>This field is used to store a pointer to a native resource. It is important to ensure
        /// that the handle is properly managed to prevent resource leaks. Typically, this involves releasing the handle
        /// when it is no longer needed.</remarks>
        private readonly IntPtr Value;
    }
}
