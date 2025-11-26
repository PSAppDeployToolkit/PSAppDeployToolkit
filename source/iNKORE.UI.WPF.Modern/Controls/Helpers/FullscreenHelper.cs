// Ported from https://github.com/lindexi/lindexi_gd/blob/master/KenafearcuweYemjecahee/FullscreenHelper.cs

using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace iNKORE.UI.WPF.Modern.Controls.Primitives
{
    /// <summary>
    /// Helper class for making a window fullscreen.
    /// Achieves fullscreen by setting window position and size to cover the entire screen.
    /// Known requirements: window must cover the entire screen, must not have WS_THICKFRAME style, must not have a title bar and be maximized.
    /// </summary>
    public static partial class FullscreenHelper
    {
        /// <summary>
        /// Attached property to store window placement before entering fullscreen.
        /// </summary>
        private static readonly DependencyProperty BeforeFullscreenWindowPlacementProperty =
        DependencyProperty.RegisterAttached("BeforeFullscreenWindowPlacement", typeof(WINDOWPLACEMENT?),
        typeof(Window));

        /// <summary>
        /// Attached property to store window style before entering fullscreen.
        /// </summary>
        private static readonly DependencyProperty BeforeFullscreenWindowStyleProperty =
        DependencyProperty.RegisterAttached("BeforeFullscreenWindowStyle", typeof(WindowStyles?), typeof(Window));

        /// <summary>
        /// Start fullscreen mode.
        /// After entering fullscreen, the window can be moved or resized via API (or Win + Shift + Left/Right), but will be reset to fullscreen based on the target rectangle and monitor.
        /// Do not modify window styles or properties while in fullscreen; they will be restored when exiting.
        /// DWM transition animations are disabled in fullscreen mode.
        /// </summary>
        /// <param name="window"></param>
        public static void StartFullscreen(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window), $"{nameof(window)} cannot be null");
            }

            // Ensure not already in fullscreen mode
            if (window.GetValue(BeforeFullscreenWindowPlacementProperty) == null &&
            window.GetValue(BeforeFullscreenWindowStyleProperty) == null)
            {
                var hwnd = new WindowInteropHelper(window).EnsureHandle();
                var hwndSource = HwndSource.FromHwnd(hwnd);

                // Get and save current window placement
                var placement = new WINDOWPLACEMENT();
                placement.Size = (uint)Marshal.SizeOf(placement);
                Win32.User32.GetWindowPlacement(hwnd, ref placement);
                window.SetValue(BeforeFullscreenWindowPlacementProperty, placement);

                // Modify window style
                var style = (WindowStyles)Win32.User32.GetWindowLongPtr(hwnd, GetWindowLongFields.GWL_STYLE);
                window.SetValue(BeforeFullscreenWindowStyleProperty, style);
                // Restore window to normal state; cannot fullscreen with title bar in maximized mode.
                // Use restore, do not modify title bar.
                // On exit, original state will be restored.
                // Remove WS_THICKFRAME; cannot fullscreen with this style.
                // Remove WS_MAXIMIZEBOX; disables maximize button, maximizing will exit fullscreen.
                // Remove WS_MAXIMIZE; restore window state, do not use ShowWindow(hwnd, ShowWindowCommands.SW_RESTORE) to avoid visible state change and affecting Visible property.
                style &= (~(WindowStyles.WS_THICKFRAME | WindowStyles.WS_MAXIMIZEBOX | WindowStyles.WS_MAXIMIZE));
                Win32.User32.SetWindowLongPtr(hwnd, GetWindowLongFields.GWL_STYLE, (IntPtr)style);

                // Disable DWM transition animations; ignore return value if DWM is off
                Win32.Dwmapi.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED, 1,
                sizeof(int));

                // Add hook to keep window fullscreen when position/size changes
                hwndSource.AddHook(KeepFullscreenHook);

                if (Win32.User32.GetWindowRect(hwnd, out var rect))
                {
                    // Do not use placement coordinates; placement is work area, not screen coordinates.

                    // Use current window rectangle to set position and size; hook will adjust to fullscreen.
                    Win32.User32.SetWindowPos(hwnd, (IntPtr)HwndZOrder.HWND_TOP, rect.Left, rect.Top, rect.Width,
                    rect.Height, (int)WindowPositionFlags.SWP_NOZORDER);
                }
            }
        }

        /// <summary>
        /// Exit fullscreen mode.
        /// Window will return to the state saved before entering fullscreen.
        /// DWM transition animations are re-enabled after exiting fullscreen.
        /// </summary>
        /// <param name="window"></param>
        public static void EndFullscreen(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window), $"{nameof(window)} cannot be null");
            }

            // Ensure in fullscreen mode and get previously saved state
            if (window.GetValue(BeforeFullscreenWindowPlacementProperty) is WINDOWPLACEMENT placement
            && window.GetValue(BeforeFullscreenWindowStyleProperty) is WindowStyles style)
            {
                var hwnd = new WindowInteropHelper(window).Handle;

                if (hwnd == IntPtr.Zero)
                {
                    // Handle is zero in two cases:
                    //1. Window was closed after entering fullscreen.
                    //2. Called before window initialization and before StartFullscreen.
                    // Just return in both cases.
                    return;
                }


                var hwndSource = HwndSource.FromHwnd(hwnd);

                // Remove hook
                hwndSource.RemoveHook(KeepFullscreenHook);

                // Restore saved state
                // Do not change WS_MAXIMIZE in style, or window will maximize with incorrect size
                // Do not set WS_MINIMIZE, or minimize button will show as restore
                Win32.User32.SetWindowLongPtr(hwnd, GetWindowLongFields.GWL_STYLE,
                (IntPtr)(style & (~(WindowStyles.WS_MAXIMIZE | WindowStyles.WS_MINIMIZE))));

                if ((style & WindowStyles.WS_MINIMIZE) != 0)
                {
                    // If window was minimized before fullscreen, restore to normal instead of minimized.
                    // Usually, users do not expect to restore to minimized after exiting fullscreen.
                    placement.ShowCmd = Win32.ShowWindowCommands.SW_RESTORE;
                }

                if ((style & WindowStyles.WS_MAXIMIZE) != 0)
                {
                    // Call ShowWindow to restore maximized state; using SetWindowPlacement alone causes flicker, only rely on it for RestoreBounds.
                    Win32.User32.ShowWindow(hwnd, Win32.ShowWindowCommands.SW_MAXIMIZE);
                }

                Win32.User32.SetWindowPlacement(hwnd, ref placement);

                if ((style & WindowStyles.WS_MAXIMIZE) ==
               0) // If window is maximized, do not modify WPF properties, or RestoreBounds will be broken; WPF does not change Left/Top/Width/Height when maximized
                {
                    if (Win32.User32.GetWindowRect(hwnd, out var rect))
                    {
                        // Do not use placement coordinates; placement is work area, not screen coordinates.

                        // Ensure WPF properties match Win32 position
                        var logicalPos =
                        hwndSource.CompositionTarget.TransformFromDevice.Transform(
                        new System.Windows.Point(rect.Left, rect.Top));
                        var logicalSize =
                        hwndSource.CompositionTarget.TransformFromDevice.Transform(
                        new System.Windows.Point(rect.Width, rect.Height));
                        window.Left = logicalPos.X;
                        window.Top = logicalPos.Y;
                        window.Width = logicalSize.X;
                        window.Height = logicalSize.Y;
                    }
                }

                // Re-enable DWM transition animations; ignore return value if DWM is off
                Win32.Dwmapi.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED, 0,
                sizeof(int));

                // Clear saved state
                window.ClearValue(BeforeFullscreenWindowPlacementProperty);
                window.ClearValue(BeforeFullscreenWindowStyleProperty);
            }
        }

        /// <summary>
        /// Hook to keep window fullscreen.
        /// Uses HandleProcessCorruptedStateExceptions to prevent crashes from fatal exceptions during memory access.
        /// </summary>
        // [HandleProcessCorruptedStateExceptions]
        private static IntPtr KeepFullscreenHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle WM_WINDOWPOSCHANGING message
            const int WINDOWPOSCHANGING = 0x0046;
            if (msg == WINDOWPOSCHANGING)
            {
                try
                {
                    // Get WINDOWPOS structure
                    var pos = (WindowPosition)Marshal.PtrToStructure(lParam, typeof(WindowPosition));

                    if ((pos.Flags & WindowPositionFlags.SWP_NOMOVE) != 0 &&
                    (pos.Flags & WindowPositionFlags.SWP_NOSIZE) != 0)
                    {
                        // If neither position nor size is changing, do nothing.
                        return IntPtr.Zero;
                    }

                    if (Win32.User32.IsIconic(hwnd))
                    {
                        // If window is minimized during fullscreen, ignore subsequent position changes.
                        // Otherwise, incorrect target position may be calculated and window will jump to primary screen.
                        return IntPtr.Zero;
                    }

                    // Get current window rectangle for reference
                    if (Win32.User32.GetWindowRect(hwnd, out var rect))
                    {
                        var targetRect = rect; // Target rectangle for window change

                        if ((pos.Flags & WindowPositionFlags.SWP_NOMOVE) == 0)
                        {
                            // Move required
                            targetRect.Left = pos.X;
                            targetRect.Top = pos.Y;
                        }

                        if ((pos.Flags & WindowPositionFlags.SWP_NOSIZE) == 0)
                        {
                            // Size change required
                            targetRect.Right = targetRect.Left + pos.Width;
                            targetRect.Bottom = targetRect.Top + pos.Height;
                        }
                        else
                        {
                            // No size change
                            targetRect.Right = targetRect.Left + rect.Width;
                            targetRect.Bottom = targetRect.Top + rect.Height;
                        }

                        // Get monitor info for target rectangle
                        var monitor = Win32.User32.MonitorFromRect(targetRect, MonitorFlag.MONITOR_DEFAULTTOPRIMARY);
                        var info = new MonitorInfo();
                        info.Size = (uint)Marshal.SizeOf(info);
                        if (Win32.User32.GetMonitorInfo(monitor, ref info))
                        {
                            // Set window position and size based on monitor info
                            pos.X = info.MonitorRect.Left;
                            pos.Y = info.MonitorRect.Top;
                            pos.Width = info.MonitorRect.Right - info.MonitorRect.Left;
                            pos.Height = info.MonitorRect.Bottom - info.MonitorRect.Top;
                            pos.Flags &= ~(WindowPositionFlags.SWP_NOSIZE | WindowPositionFlags.SWP_NOMOVE |
                            WindowPositionFlags.SWP_NOREDRAW);
                            pos.Flags |= WindowPositionFlags.SWP_NOCOPYBITS;

                            if (rect == info.MonitorRect)
                            {
                                var hwndSource = HwndSource.FromHwnd(hwnd);
                                if (hwndSource?.RootVisual is Window window)
                                {
                                    // Ensure WPF properties match Win32 position, prevents issues if user changes WPF properties after fullscreen.
                                    // This may trigger WM_WINDOWPOSCHANGING again, but there is no better timing.
                                    // WM_WINDOWPOSCHANGED cannot be used.
                                    // (e.g. changing Left after fullscreen triggers WM_WINDOWPOSCHANGING, and this code resets Left, so WM_WINDOWPOSCHANGED is not triggered, window size is correct but Left property is wrong.)
                                    var logicalPos =
                                    hwndSource.CompositionTarget.TransformFromDevice.Transform(
                                    new System.Windows.Point(pos.X, pos.Y));
                                    var logicalSize =
                                    hwndSource.CompositionTarget.TransformFromDevice.Transform(
                                    new System.Windows.Point(pos.Width, pos.Height));
                                    window.Left = logicalPos.X;
                                    window.Top = logicalPos.Y;
                                    window.Width = logicalSize.X;
                                    window.Height = logicalSize.Y;
                                }
                                else
                                {
                                    // This hwnd was from Window, if not Window now... unexpected.
                                }
                            }

                            // Copy modified structure back
                            Marshal.StructureToPtr(pos, lParam, false);
                        }
                    }
                }
                catch
                {
                    // No logging needed, just prevent crashes from unexpected logic in message loop.
                }
            }

            return IntPtr.Zero;
        }
    }

    public static partial class FullscreenHelper
    {
        static class Win32
        {
            [Flags]
            public enum ShowWindowCommands
            {
                /// <summary>
                /// Maximizes the specified window.
                /// </summary>
                SW_MAXIMIZE = 3,

                /// <summary>
                /// Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
                /// </summary>
                SW_RESTORE = 9,
            }


            internal static class Properties
            {
#if !ANSI
                public const CharSet BuildCharSet = CharSet.Unicode;
#else
 public const CharSet BuildCharSet = CharSet.Ansi;
#endif
            }

            public static class Dwmapi
            {
                public const string LibraryName = "Dwmapi.dll";

                [DllImport(LibraryName, ExactSpelling = true, PreserveSig = false)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool DwmIsCompositionEnabled();

                [DllImport("Dwmapi.dll", ExactSpelling = true, SetLastError = true)]
                public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute,
                in int pvAttribute, uint cbAttribute);
            }

            public static class User32
            {
                public const string LibraryName = "user32";

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
                public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern IntPtr MonitorFromRect(in Rectangle lprc, MonitorFlag dwFlags);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool IsIconic(IntPtr hwnd);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool SetWindowPlacement(IntPtr hWnd,
                [In] ref WINDOWPLACEMENT lpwndpl);

                [return: MarshalAs(UnmanagedType.Bool)]
                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

                [DllImport(LibraryName, ExactSpelling = true, SetLastError = true)]
                public static extern Int32 SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, Int32 x, Int32 y, Int32 cx,
                Int32 cy, Int32 wFlagslong);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

                public static IntPtr GetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex) =>
                GetWindowLongPtr(hWnd, (int)nIndex);

                public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
                {
                    return IntPtr.Size > 4
#pragma warning disable CS0618 // Type or member is obsolete
                    ? GetWindowLongPtr_x64(hWnd, nIndex)
                    : new IntPtr(GetWindowLong(hWnd, nIndex));
#pragma warning restore CS0618 // Type or member is obsolete
                }

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
                public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "GetWindowLongPtr")]
                public static extern IntPtr GetWindowLongPtr_x64(IntPtr hWnd, int nIndex);

                public static IntPtr SetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex, IntPtr dwNewLong) =>
                SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);

                public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
                {
                    return IntPtr.Size > 4
#pragma warning disable CS0618 // Type or member is obsolete
                    ? SetWindowLongPtr_x64(hWnd, nIndex, dwNewLong)
                    : new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
#pragma warning restore CS0618 // Type or member is obsolete
                }

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "SetWindowLongPtr")]
                public static extern IntPtr SetWindowLongPtr_x64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
                public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MonitorInfo
        {
            /// <summary>
            /// The size of the structure, in bytes.
            /// </summary>
            public uint Size;

            /// <summary>
            /// A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates. If the monitor is not the primary display monitor, some coordinates may be negative.
            /// </summary>
            public Rectangle MonitorRect;

            /// <summary>
            /// A RECT structure that specifies the work area rectangle of the display monitor, expressed in virtual-screen coordinates. If the monitor is not the primary display monitor, some coordinates may be negative.
            /// </summary>
            public Rectangle WorkRect;

            /// <summary>
            /// Flags representing attributes of the display monitor.
            /// </summary>
            public MonitorInfoFlag Flags;
        }

        enum MonitorInfoFlag
        {
        }

        enum MonitorFlag
        {
            /// <summary>
            /// Returns a handle to the primary display monitor.
            /// </summary>
            MONITOR_DEFAULTTOPRIMARY = 1,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WindowPosition
        {
            public IntPtr Hwnd;
            public IntPtr HwndZOrderInsertAfter;
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public WindowPositionFlags Flags;
        }

        enum HwndZOrder
        {
            /// <summary>
            /// Places the window at the top of the Z order.
            /// </summary>
            HWND_TOP = 0,
        }

        enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_TRANSITIONS_FORCEDISABLED = 3,
        }

        enum GetWindowLongFields
        {
            /// <summary>
            /// Retrieves the window styles.
            /// </summary>
            GWL_STYLE = -16,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT // WindowPlacement
        {
            public uint Size;
            public WindowPlacementFlags Flags;
            public Win32.ShowWindowCommands ShowCmd;
            public Point MinPosition;
            public Point MaxPosition;
            public Rectangle NormalPosition;
        }

        [Flags]
        public enum WindowPositionFlags
        {
            /// <summary>
            /// Discards the entire contents of the client area. If not specified, valid contents are saved and copied back after window is sized or repositioned.
            /// </summary>
            SWP_NOCOPYBITS = 0x0100,

            /// <summary>
            /// Retains the current position (ignores X and Y parameters).
            /// </summary>
            SWP_NOMOVE = 0x0002,

            /// <summary>
            /// Does not redraw changes. If set, no repainting occurs. Applies to client and nonclient area, and any part of parent window uncovered. Application must explicitly invalidate/redraw any parts needing update.
            /// </summary>
            SWP_NOREDRAW = 0x0008,

            /// <summary>
            /// Retains the current size (ignores cx and cy parameters).
            /// </summary>
            SWP_NOSIZE = 0x0001,

            /// <summary>
            /// Retains the current Z order (ignores hWndInsertAfter parameter).
            /// </summary>
            SWP_NOZORDER = 0x0004,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Rectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            /// <summary>
            /// Rectangle width
            /// </summary>
            public int Width
            {
                get { return unchecked((int)(Right - Left)); }
                set { Right = unchecked((int)(Left + value)); }
            }

            /// <summary>
            /// Rectangle height
            /// </summary>
            public int Height
            {
                get { return unchecked((int)(Bottom - Top)); }
                set { Bottom = unchecked((int)(Top + value)); }
            }

            public bool Equals(Rectangle other)
            {
                return (Left == other.Left) && (Right == other.Right) && (Top == other.Top) && (Bottom == other.Bottom);
            }

            public override bool Equals(object obj)
            {
                return obj is Rectangle rectangle && Equals(rectangle);
            }

            public static bool operator ==(Rectangle left, Rectangle right)
            {
                return left.Equals(right);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int)Left;
                    hashCode = (hashCode * 397) ^ (int)Top;
                    hashCode = (hashCode * 397) ^ (int)Right;
                    hashCode = (hashCode * 397) ^ (int)Bottom;
                    return hashCode;
                }
            }

            public static bool operator !=(Rectangle left, Rectangle right)
            {
                return !(left == right);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Point
        {
            public int X;
            public int Y;
        }

        [Flags]
        enum WindowPlacementFlags
        {
        }

        [Flags]
        enum WindowStyles
        {
            /// <summary>
            /// The window is initially maximized.
            /// </summary>
            WS_MAXIMIZE = 0x01000000,

            /// <summary>
            /// The window has a maximize button. Cannot be combined with WS_EX_CONTEXTHELP. WS_SYSMENU must also be specified.
            /// </summary>
            WS_MAXIMIZEBOX = 0x00010000,

            /// <summary>
            /// The window is initially minimized. Same as WS_ICONIC.
            /// </summary>
            WS_MINIMIZE = 0x20000000,

            /// <summary>
            /// The window has a sizing border. Same as WS_SIZEBOX.
            /// </summary>
            WS_THICKFRAME = 0x00040000,
        }
    }
}