// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace iNKORE.UI.WPF.Modern.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x;
        public int y;

        public POINT()
        {
        }

        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
#if DEBUG
            public override string ToString() {
                return "{x=" + x + ", y=" + y + "}";
            }
#endif
    }

    // NOTE:  this replaces the RECT struct in NativeMethodsCLR.cs because it adds an extra method IsEmpty
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public int Width
        {
            get { return right - left; }
        }

        public int Height
        {
            get { return bottom - top; }
        }

        public void Offset(int dx, int dy)
        {
            left += dx;
            top += dy;
            right += dx;
            bottom += dy;
        }

        public bool IsEmpty
        {
            get
            {
                return left >= right || top >= bottom;
            }
        }
    }

    internal partial class NativeMethods
    {
        public const int SWP_NOSIZE = 0x0001,
        SWP_NOMOVE = 0x0002,
        SWP_NOZORDER = 0x0004,
        SWP_NOACTIVATE = 0x0010,
        SWP_SHOWWINDOW = 0x0040,
        SWP_HIDEWINDOW = 0x0080,
        SWP_DRAWFRAME = 0x0020;

        public const uint HTCLIENT = 1U;
        public const uint HTMAXBUTTON = 9U;
        public const uint MONITORINFOF_PRIMARY = 1U;
        public const uint WM_NCHITTEST = 132U;
        public const uint WM_NCLBUTTONDOWN = 161U;
        public const uint WM_NCLBUTTONUP = 162U;
        public const uint WM_NCMOUSELEAVE = 674U;
        public const uint WM_SETTINGCHANGE = 26U;
        public const uint WM_WINDOWPOSCHANGED = 71U;
        public const uint WM_WINDOWPOSCHANGING = 70U;

        internal static unsafe HWND FindWindow(string lpClassName, string lpWindowName)
        {
            fixed (char* lpWindowNameLocal = lpWindowName)
            {
                fixed (char* lpClassNameLocal = lpClassName)
                {
                    HWND __result = User32.FindWindow(lpClassNameLocal, lpWindowNameLocal);
                    return __result;
                }
            }
        }

    }

    [DebuggerDisplay("{Value}")]
    internal readonly partial struct HWND
    : IEquatable<HWND>
    {
        internal readonly IntPtr Value;
        internal HWND(IntPtr value) => this.Value = value;

        internal static HWND Null => default;

        internal bool IsNull => Value == default;
        public static implicit operator IntPtr(HWND value) => value.Value;
        public static explicit operator HWND(IntPtr value) => new HWND(value);
        public static bool operator ==(HWND left, HWND right) => left.Value == right.Value;
        public static bool operator !=(HWND left, HWND right) => !(left == right);

        public bool Equals(HWND other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is HWND other && this.Equals(other);

        public override int GetHashCode() => this.Value.GetHashCode();
    }

    /// <summary>Contains information about the size and position of a window.</summary>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowpos">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    internal partial struct WINDOWPOS
    {
        /// <summary>
        /// <para>Type: <b>HWND</b> A handle to the window.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal HWND hwnd;
        /// <summary>
        /// <para>Type: <b>HWND</b> The position of the window in Z order (front-to-back position). This member can be a handle to the window behind which this window is placed, or can be one of the special values listed with the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-setwindowpos">SetWindowPos</a> function.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal HWND hwndInsertAfter;
        /// <summary>
        /// <para>Type: <b>int</b> The position of the left edge of the window.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal int x;
        /// <summary>
        /// <para>Type: <b>int</b> The position of the top edge of the window.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal int y;
        /// <summary>
        /// <para>Type: <b>int</b> The window width, in pixels.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal int cx;
        /// <summary>
        /// <para>Type: <b>int</b> The window height, in pixels.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal int cy;
        /// <summary>Type: <b>UINT</b></summary>
        internal SET_WINDOW_POS_FLAGS flags;
    }

    /// <summary>Contains information about the placement of a window on the screen.</summary>
    /// <remarks>
    /// <para>If the window is a top-level window that does not have the <b>WS_EX_TOOLWINDOW</b> window style, then the coordinates represented by the following members are in workspace coordinates: <b>ptMinPosition</b>, <b>ptMaxPosition</b>, and <b>rcNormalPosition</b>. Otherwise, these members are in screen coordinates. Workspace coordinates differ from screen coordinates in that they take the locations and sizes of application toolbars (including the taskbar) into account. Workspace coordinate (0,0) is the upper-left corner of the workspace area, the area of the screen not being used by application toolbars. The coordinates used in a <b>WINDOWPLACEMENT</b> structure should be used only by the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-getwindowplacement">GetWindowPlacement</a> and <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-setwindowplacement">SetWindowPlacement</a> functions. Passing workspace coordinates to functions which expect screen coordinates (such as <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-setwindowpos">SetWindowPos</a>) will result in the window appearing in the wrong location. For example, if the taskbar is at the top of the screen, saving window coordinates using <b>GetWindowPlacement</b> and restoring them using <b>SetWindowPos</b> causes the window to appear to "creep" up the screen.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowplacement#">Read more on docs.microsoft.com</see>.</para>
    /// </remarks>
    internal partial struct WINDOWPLACEMENT
    {
        /// <summary>
        /// <para>Type: <b>UINT</b> The length of the structure, in bytes. Before calling the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-getwindowplacement">GetWindowPlacement</a> or <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-setwindowplacement">SetWindowPlacement</a> functions, set this member to <c>sizeof(WINDOWPLACEMENT)</c>.</para>
        /// <para><a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-getwindowplacement">GetWindowPlacement</a> and <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-setwindowplacement">SetWindowPlacement</a> fail if this member is not set correctly.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowplacement#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal uint length;
        /// <summary>Type: <b>UINT</b></summary>
        internal WINDOWPLACEMENT_FLAGS flags;
        /// <summary>
        /// <para>Type: <b>UINT</b> The current show state of the window. It can be any of the values that can be specified in the <i>nCmdShow</i> parameter for the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-showwindow">ShowWindow</a> function.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowplacement#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal SHOW_WINDOW_CMD showCmd;
        /// <summary>
        /// <para>Type: <b><a href="https://docs.microsoft.com/previous-versions/dd162805(v=vs.85)">POINT</a></b> The coordinates of the window's upper-left corner when the window is minimized.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowplacement#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal System.Drawing.Point ptMinPosition;
        /// <summary>
        /// <para>Type: <b><a href="https://docs.microsoft.com/previous-versions/dd162805(v=vs.85)">POINT</a></b> The coordinates of the window's upper-left corner when the window is maximized.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowplacement#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal System.Drawing.Point ptMaxPosition;
        /// <summary>
        /// <para>Type: <b><a href="https://docs.microsoft.com/windows/desktop/api/windef/ns-windef-rect">RECT</a></b> The window's coordinates when the window is in the restored position.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-windowplacement#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal RECT rcNormalPosition;
    }

    /// <summary>The MONITORINFO structure contains information about a display monitor.The GetMonitorInfo function stores information in a MONITORINFO structure or a MONITORINFOEX structure.The MONITORINFO structure is a subset of the MONITORINFOEX structure.</summary>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-monitorinfo">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    internal partial struct MONITORINFO
    {
        /// <summary>
        /// <para>The size of the structure, in bytes. Set this member to <c>sizeof ( MONITORINFO )</c> before calling the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-getmonitorinfoa">GetMonitorInfo</a> function. Doing so lets the function determine the type of structure you are passing to it.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-monitorinfo#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal uint cbSize;
        /// <summary>A <a href="https://docs.microsoft.com/windows/desktop/api/windef/ns-windef-rect">RECT</a> structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates. Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.</summary>
        internal RECT rcMonitor;
        /// <summary>A <a href="https://docs.microsoft.com/windows/desktop/api/windef/ns-windef-rect">RECT</a> structure that specifies the work area rectangle of the display monitor, expressed in virtual-screen coordinates. Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.</summary>
        internal RECT rcWork;
        /// <summary>
        /// <para>A set of flags that represent attributes of the display monitor. The following flag is defined. </para>
        /// <para>This doc was truncated.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-monitorinfo#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal uint dwFlags;
    }

    /// <summary>
    /// A pointer to a null-terminated, constant character string.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    internal unsafe readonly partial struct PCWSTR
        : IEquatable<PCWSTR>
    {
        /// <summary>
        /// A pointer to the first character in the string. The content should be considered readonly, as it was typed as constant in the SDK.
        /// </summary>
        internal readonly char* Value;
        internal PCWSTR(char* value) => this.Value = value;
        public static explicit operator char*(PCWSTR value) => value.Value;
        public static implicit operator PCWSTR(char* value) => new PCWSTR(value);

        public bool Equals(PCWSTR other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is PCWSTR other && this.Equals(other);

        public override int GetHashCode() => unchecked((int)this.Value);


        /// <summary>
        /// Gets the number of characters up to the first null character (exclusive).
        /// </summary>
        internal int Length
        {
            get
            {
                char* p = this.Value;
                if (p is null)
                    return 0;
                while (*p != '\0')
                    p++;
                return checked((int)(p - this.Value));
            }
        }


        /// <summary>
        /// Returns a <see langword="string"/> with a copy of this character array, up to the first null character (exclusive).
        /// </summary>
        /// <returns>A <see langword="string"/>, or <see langword="null"/> if <see cref="Value"/> is <see langword="null"/>.</returns>
        public override string ToString() => this.Value is null ? null : new string(this.Value);



        private string DebuggerDisplay => this.ToString();
    }

    [DebuggerDisplay("{Value}")]
    internal readonly partial struct HICON
    : IEquatable<HICON>
    {
        internal readonly IntPtr Value;
        internal HICON(IntPtr value) => this.Value = value;

        internal static HICON Null => default;

        internal bool IsNull => Value == default;
        public static implicit operator IntPtr(HICON value) => value.Value;
        public static explicit operator HICON(IntPtr value) => new HICON(value);
        public static bool operator ==(HICON left, HICON right) => left.Value == right.Value;
        public static bool operator !=(HICON left, HICON right) => !(left == right);

        public bool Equals(HICON other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is HICON other && this.Equals(other);

        public override int GetHashCode() => this.Value.GetHashCode();
    }

}
