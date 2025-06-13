using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Helpers;
using iNKORE.UI.WPF.Modern.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Helpers.Styles
{
    public enum BackdropType
    {
        None = 1,
        Mica = 2,
        Acrylic = 3,
        Tabbed = 4,

        Acrylic10,
        Acrylic11,
    }

    public static class BackdropHelper
    {
        /// <summary>
        /// Checks if the current <see cref="Windows"/> supports selected <see cref="BackdropType"/>.
        /// </summary>
        /// <param name="type">Background type to check.</param>
        /// <returns><see langword="true"/> if <see cref="BackdropType"/> is supported.</returns>
        public static bool IsSupported(this BackdropType type)
        {
            if (!OSVersionHelper.IsWindowsNT) { return false; }

            return type switch
            {
                BackdropType.None => true, // OSVersionHelper.OSVersion >= new Version(10, 0, 21996), // Insider with new API                
                BackdropType.Tabbed => OSVersionHelper.OSVersion >= new Version(10, 0, 22523),
                BackdropType.Mica => OSVersionHelper.OSVersion >= new Version(10, 0, 21996),
                BackdropType.Acrylic11 => OSVersionHelper.OSVersion >= new Version(10, 0, 22523),
                BackdropType.Acrylic10 => Acrylic10Helper.IsAcrylicSupported(),
                BackdropType.Acrylic => IsSupported(BackdropType.Acrylic10) || IsSupported(BackdropType.Acrylic11),
                _ => false
            };
        }

        public static bool IsManualBackgroundNeeded(this BackdropType type)
        {
            return GetActualBackdropType(type) switch
            {
                BackdropType.None => true, // OSVersionHelper.OSVersion >= new Version(10, 0, 21996), // Insider with new API                
                BackdropType.Tabbed => false,
                BackdropType.Mica => false,
                BackdropType.Acrylic11 => false,
                BackdropType.Acrylic10 => false,
                _ => true
            };
        }

        /// <summary>
        /// Applies selected background effect to <see cref="Window"/> when is rendered.
        /// </summary>
        /// <param name="window">Window to apply effect.</param>
        /// <param name="type">Background type.</param>
        /// <param name="force">Skip the compatibility check.</param>
        public static bool Apply(Window window, BackdropType type, bool force = false)
        {
            if (!force && !type.IsSupported()) { return false; }

            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) { return false; }

            Apply(windowHandle, type, force, WindowHelper.GetAcrylic10Color(window));

            return true;
        }

        /// <summary>
        /// Applies selected background effect to <c>hWnd</c> by it's pointer.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        /// <param name="type">Background type.</param>
        /// <param name="force">Skip the compatibility check.</param>
        public static bool Apply(IntPtr handle, BackdropType type, bool force = false, Color? acrylic10Color = null)
        {
            if (!force && !type.IsSupported()) { return false; }

            if (handle == IntPtr.Zero) { return false; }

            // Fixes from: https://github.com/iNKORE-NET/UI.WPF.Modern/pull/63
            var captionColor = -2; //DWMWA_COLOR_NONE - 0xFFFFFFFE
            DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR,
                ref captionColor,
                Marshal.SizeOf(typeof(int)));

            return type switch
            {
                BackdropType.None => TryApplyNone(handle),
                BackdropType.Mica => TryApplyMica(handle),
                BackdropType.Acrylic11 => TryApplyAcrylic(handle),
                BackdropType.Acrylic10 => Acrylic10Helper.TryApplyAcrylic(handle, acrylic10Color ?? Colors.Transparent),
                BackdropType.Acrylic => Apply(handle, GetActualBackdropType(type), force, acrylic10Color),
                BackdropType.Tabbed => TryApplyTabbed(handle),
                _ => false
            };
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

        public static BackdropType GetActualBackdropType(this BackdropType type)
        {
            if (type == BackdropType.Acrylic)
            {
                return IsSupported(BackdropType.Acrylic11) ? BackdropType.Acrylic11 : BackdropType.Acrylic10;
            }

            return type;
        }

        /// <summary>
        /// Tries to remove all effects if they have been applied to the <c>hWnd</c>.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        public static void Remove(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;

            Acrylic10Helper.Remove(handle);


            int pvAttribute = (int)DWMAPI.PvAttribute.Disable;
            int backdropPvAttribute = (int)DWMAPI.DWMSBT.DWMSBT_DISABLE;

            RemoveDarkMode(handle);

            DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT, ref pvAttribute,
                Marshal.SizeOf(typeof(int)));

            DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropPvAttribute,
                Marshal.SizeOf(typeof(int)));

            // Fixes from: https://github.com/iNKORE-NET/UI.WPF.Modern/pull/63
            var captionColor = -1; //DWMWA_COLOR_DEFAULT - 0xFFFFFFFF
            DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR,
                ref captionColor,
                Marshal.SizeOf(typeof(int)));
        }

        /// <summary>
        /// Tries to inform the operating system that this window uses dark mode.
        /// </summary>
        /// <param name="window">Window to apply effect.</param>
        public static void ApplyDarkMode(this Window window)
        {
            if (window == null)
                return;

            try
            {
                var windowHandle = new WindowInteropHelper(window).EnsureHandle();

                if (windowHandle == IntPtr.Zero) return;

                ApplyDarkMode(windowHandle);
            }
            catch { }
        }

        /// <summary>
        /// Tries to inform the operating system that this <c>hWnd</c> uses dark mode.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        public static void ApplyDarkMode(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;

            var pvAttribute = (int)DWMAPI.PvAttribute.Enable;
            var dwAttribute = DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

            if (OSVersionHelper.OSVersion < new Version(10, 0, 18985))
            {
                dwAttribute = DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
            }

            DWMAPI.DwmSetWindowAttribute(handle, dwAttribute,
                ref pvAttribute,
                Marshal.SizeOf(typeof(int)));
        }

        /// <summary>
        /// Tries to clear the dark theme usage information.
        /// </summary>
        /// <param name="window">Window to remove effect.</param>
        public static void RemoveDarkMode(this Window window)
        {
            if (window == null)
                return;

            try
            {
                var windowHandle = new WindowInteropHelper(window).EnsureHandle();

                if (windowHandle == IntPtr.Zero) return;

                RemoveDarkMode(windowHandle);
            }
            catch { }
        }

        /// <summary>
        /// Tries to clear the dark theme usage information.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        public static void RemoveDarkMode(IntPtr handle)
        {
            if (handle == IntPtr.Zero) { return; }

            var pvAttribute = (int)DWMAPI.PvAttribute.Disable;
            var dwAttribute = DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

            if (OSVersionHelper.OSVersion < new Version(10, 0, 18985))
            {
                dwAttribute = DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
            }

            DWMAPI.DwmSetWindowAttribute(handle, dwAttribute,
                ref pvAttribute,
                Marshal.SizeOf(typeof(int)));
        }

        /// <summary>
        /// Tries to remove default TitleBar from <c>hWnd</c>.
        /// </summary>
        /// <param name="window">Window to remove effect.</param>
        public static void RemoveTitleBar(this Window window)
        {
            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) return;

            RemoveTitleBar(windowHandle);
        }

        /// <summary>
        /// Tries to remove default TitleBar from <c>hWnd</c>.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        /// <returns><see langowrd="false"/> is problem occurs.</returns>
        private static bool RemoveTitleBar(IntPtr handle)
        {
            // Hide default TitleBar
            // https://stackoverflow.com/questions/743906/how-to-hide-close-button-in-wpf-window
            try
            {
                User32.SetWindowLong(handle, -16, User32.GetWindowLong(handle, -16) & ~0x80000);

                return true;
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e);
#endif
                return false;
            }
        }

        private static bool TryApplyNone(IntPtr handle)
        {
            if (OSVersionHelper.OSVersion >= new Version(10, 0, 22523))
            {
                int backdropPvAttribute = (int)DWMAPI.DWMSBT.DWMSBT_AUTO;

                DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropPvAttribute,
                    Marshal.SizeOf(typeof(int)));

                return true;
            }
            else
            {
                Remove(handle);
                return true;
            }
        }

        private static bool TryApplyTabbed(IntPtr handle)
        {
            int backdropPvAttribute = (int)DWMAPI.DWMSBT.DWMSBT_TABBEDWINDOW;

            DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropPvAttribute,
                Marshal.SizeOf(typeof(int)));

            return true;
        }

        private static bool TryApplyMica(IntPtr handle)
        {
            int backdropPvAttribute;

            if (OSVersionHelper.OSVersion >= new Version(10, 0, 22523))
            {
                backdropPvAttribute = (int)DWMAPI.DWMSBT.DWMSBT_MAINWINDOW;

                DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropPvAttribute,
                    Marshal.SizeOf(typeof(int)));

                return true;
            }

            if (!RemoveTitleBar(handle)) { return false; }

            backdropPvAttribute = (int)DWMAPI.PvAttribute.Enable;

            DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT,
                ref backdropPvAttribute,
                Marshal.SizeOf(typeof(int)));

            return true;
        }

        private static bool TryApplyAcrylic(IntPtr handle)
        {
            int backdropPvAttribute = (int)DWMAPI.DWMSBT.DWMSBT_TRANSIENTWINDOW;

            DWMAPI.DwmSetWindowAttribute(handle, DWMAPI.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropPvAttribute,
                Marshal.SizeOf(typeof(int)));

            return true;
        }
    }
}
