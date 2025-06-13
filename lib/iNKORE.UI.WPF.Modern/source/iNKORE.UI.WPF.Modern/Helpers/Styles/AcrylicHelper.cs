using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
//using Windows.Win32;

namespace iNKORE.UI.WPF.Modern.Helpers.Styles
{
    public static class Acrylic10Helper
    {
        /// <summary>
        /// Checks if the current <see cref="Windows"/> supports Aero.
        /// </summary>
        /// <returns><see langword="true"/> if Aero is supported.</returns>
        public static bool IsAeroSupported()
        {
            if (!OSVersionHelper.IsWindowsNT) { return false; }

            if (new Version(10, 0) <= OSVersionHelper.OSVersion && OSVersionHelper.OSVersion < new Version(10, 0, 22523)) { return true; }

            return false;
        }

        /// <summary>
        /// Checks if the current <see cref="Windows"/> supports selected Acrylic.
        /// </summary>
        /// <returns><see langword="true"/> if Acrylic is supported.</returns>
        public static bool IsAcrylicSupported()
        {
            if (!OSVersionHelper.IsWindowsNT) { return false; }

            //if (new Version(10, 0, 17063) <= OSVersionHelper.OSVersion && OSVersionHelper.OSVersion < new Version(10, 0, 22523)) { return true; }
            if (new Version(10, 0, 17063) <= OSVersionHelper.OSVersion) { return true; }

            return false;
        }

        /// <summary>
        /// Applies selected background effect to <see cref="Window"/> when is rendered.
        /// </summary>
        /// <param name="window">Window to apply effect.</param>
        /// <param name="force">Skip the compatibility check.</param>
        public static bool Apply(Window window, bool force = false)
        {
            //if (!force && !IsSupported()) { return false; }

            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) { return false; }

            if (window.Background is SolidColorBrush brush)
            {
                Apply(windowHandle, brush.Color, force);
            }
            else
            {
                Apply(windowHandle, Colors.Transparent, force);
            }

            return true;
        }

        /// <summary>
        /// Applies selected background effect to <c>hWnd</c> by it's pointer.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        /// <param name="color">The Gradient Color of Acrylic.</param>
        /// <param name="force">Skip the compatibility check.</param>
        public static bool Apply(IntPtr handle, Color color, bool force = false)
        {
            //if (!force && !IsSupported()) 
            //{ 
            //    return false; 
            //}

            if (handle == IntPtr.Zero) 
            { 
                return false; 
            }

            if (IsAcrylicSupported())
            {
                return TryApplyAcrylic(handle, color);
            }
            else
            {
                return false; // TryApplyAero(handle);
            }

        }

        /// <summary>
        /// Tries to remove background effects if they have been applied to the <see cref="Window"/>.
        /// </summary>
        /// <param name="window">The window from which the effect should be removed.</param>
        public static void Remove(Window window)
        {
            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) return;

            Remove(windowHandle);
        }

        /// <summary>
        /// Tries to remove all effects if they have been applied to the <c>hWnd</c>.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        public static void Remove(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;

            ACCENT_POLICY accentPolicy = new ACCENT_POLICY
            {
                AccentState = ACCENT_STATE.ACCENT_DISABLED,
            };

            int accentStructSize = Marshal.SizeOf(accentPolicy);

            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accentPolicy, accentPtr, false);

            WINCOMPATTRDATA data = new WINCOMPATTRDATA
            {
                Attribute = WINCOMPATTR.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        public static bool TryApplyAero(IntPtr handle)
        {
            ACCENT_POLICY accentPolicy = new ACCENT_POLICY
            {
                AccentState = ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
            };

            int accentStructSize = Marshal.SizeOf(accentPolicy);

            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accentPolicy, accentPtr, false);

            WINCOMPATTRDATA data = new WINCOMPATTRDATA
            {
                Attribute = WINCOMPATTR.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(handle, ref data);

            Marshal.FreeHGlobal(accentPtr);

            return true;
        }

        public static bool TryApplyAcrylic(IntPtr handle, Color backcolor)
        {
            ACCENT_POLICY accentPolicy = new ACCENT_POLICY
            {
                AccentState = ACCENT_STATE.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = (uint)backcolor.ColorToDouble(0.8)
            };

            int accentStructSize = Marshal.SizeOf(accentPolicy);

            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accentPolicy, accentPtr, false);

            WINCOMPATTRDATA data = new WINCOMPATTRDATA
            {
                Attribute = WINCOMPATTR.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(handle, ref data);

            Marshal.FreeHGlobal(accentPtr);

            return true;
        }

        private static int ColorToDouble(this Color value, double scale = 1)
        {
            return
            // Red
            value.R << 0 |
            // Green
            value.G << 8 |
            // Blue
            value.B << 16 |
            // Alpha
            (int)(value.A * scale) << 24;
        }

        /// <summary>
        /// DWM window accent state.
        /// </summary>
        private enum ACCENT_STATE
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        /// <summary>
        /// DWM window attributes.
        /// </summary>
        private enum WINCOMPATTR
        {
            WCA_UNDEFINED = 0,
            WCA_NCRENDERING_ENABLED = 1,
            WCA_NCRENDERING_POLICY = 2,
            WCA_TRANSITIONS_FORCEDISABLED = 3,
            WCA_ALLOW_NCPAINT = 4,
            WCA_CAPTION_BUTTON_BOUNDS = 5,
            WCA_NONCLIENT_RTL_LAYOUT = 6,
            WCA_FORCE_ICONIC_REPRESENTATION = 7,
            WCA_EXTENDED_FRAME_BOUNDS = 8,
            WCA_HAS_ICONIC_BITMAP = 9,
            WCA_THEME_ATTRIBUTES = 10,
            WCA_NCRENDERING_EXILED = 11,
            WCA_NCADORNMENTINFO = 12,
            WCA_EXCLUDED_FROM_LIVEPREVIEW = 13,
            WCA_VIDEO_OVERLAY_ACTIVE = 14,
            WCA_FORCE_ACTIVEWINDOW_APPEARANCE = 15,
            WCA_DISALLOW_PEEK = 16,
            WCA_CLOAK = 17,
            WCA_CLOAKED = 18,
            WCA_ACCENT_POLICY = 19,
            WCA_FREEZE_REPRESENTATION = 20,
            WCA_EVER_UNCLOAKED = 21,
            WCA_VISUAL_OWNER = 22,
            WCA_HOLOGRAPHIC = 23,
            WCA_EXCLUDED_FROM_DDA = 24,
            WCA_PASSIVEUPDATEMODE = 25,
            WCA_USEDARKMODECOLORS = 26,
            WCA_CORNER_STYLE = 27,
            WCA_PART_COLOR = 28,
            WCA_DISABLE_MOVESIZE_FEEDBACK = 29,
            WCA_LAST = 30
        }

        /// <summary>
        /// DWM window accent policy.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct ACCENT_POLICY
        {
            public ACCENT_STATE AccentState;
            public uint AccentFlags;
            public uint GradientColor;
            public uint AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINCOMPATTRDATA
        {
            public WINCOMPATTR Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        /// <summary>
        /// Sets various information regarding DWM window attributes.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetWindowCompositionAttribute(IntPtr hWnd, ref WINCOMPATTRDATA data);
    }
}
