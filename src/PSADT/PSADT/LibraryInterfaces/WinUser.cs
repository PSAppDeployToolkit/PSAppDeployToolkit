using System;
using Windows.Win32.Foundation;

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
