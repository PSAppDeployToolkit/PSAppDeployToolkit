/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Fluence.WindowProbe
{
    internal sealed class NativeSnapshot
    {
        private const int DwmwaCloaked = 14;
        private const int DwmwaCornerPreference = 33;
        private const int DwmwaSystemBackdropType = 38;
        private readonly ProbeOptions _options;
        private readonly Window _window;

        internal NativeSnapshot(ProbeOptions options, Window window)
        {
            _options = options;
            _window = window;
        }

        internal ProbeRecord Capture(IntPtr hwnd, string phase)
        {
            ProbeRecord record = new()
            {
                UtcTimestamp = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                Phase = phase,
                Mode = _options.Mode,
                RequestedTheme = _options.Theme,
                RequestedBackdrop = _options.Backdrop,
                RequestedRenderMode = _options.RenderMode,
                RenderCapabilityTier = RenderCapability.Tier,
                RenderCapabilityTierLevel = RenderCapability.Tier >> 16,
                RequestedTopmost = _options.Topmost,
                TitleArea = string.Equals(_options.Mode, "fluence", StringComparison.Ordinal) ? "Fluence custom 48 DIP" : string.Equals(_options.Mode, "chrome-dwm", StringComparison.Ordinal) ? "probe custom 48 DIP" : "native caption with metric-based spacer",
                ContentOriginStatus = "not available",
                WindowBackground = (_window.Background as SolidColorBrush)?.Color.ToString(CultureInfo.InvariantCulture) ?? _window.Background?.ToString(CultureInfo.InvariantCulture) ?? "null",
                TargetBackground = "not available",
                HwndStatus = hwnd == IntPtr.Zero ? "not issued: no HWND" : "available",
                HwndStyleStatus = hwnd == IntPtr.Zero ? "not issued: no HWND" : "pending",
                DpiStatus = hwnd == IntPtr.Zero ? "not issued: no HWND" : "pending",
                DwmBackdropQueryStatus = hwnd == IntPtr.Zero ? "not issued: no HWND" : "pending",
                DwmCornerQueryStatus = hwnd == IntPtr.Zero ? "not issued: no HWND" : "pending",
                DwmCloakedQueryStatus = hwnd == IntPtr.Zero ? "not issued: no HWND" : "pending",
                ProbeBackdropSetHResult = _options.ProbeBackdropSetHResult,
                ProbeBackdropSetStatus = _options.ProbeBackdropSetStatus,
                ProbeDarkModeSetHResult = _options.ProbeDarkModeSetHResult,
                ProbeDarkModePrimaryHResult = _options.ProbeDarkModePrimaryHResult,
                ProbeDarkModeFallbackHResult = _options.ProbeDarkModeFallbackHResult,
                ProbeDarkModeSetStatus = _options.ProbeDarkModeSetStatus,
                ProbeCornerSetHResult = _options.ProbeCornerSetHResult,
                ProbeCornerSetStatus = _options.ProbeCornerSetStatus,
                HighContrast = SystemParameters.HighContrast,
                RemoteSession = NativeMethods.GetSystemMetrics(0x1000) is not 0,
                OsVersion = Environment.OSVersion.VersionString,
                Runtime = RuntimeInformation.FrameworkDescription,
            };

            if (hwnd == IntPtr.Zero)
            {
                return record;
            }

            HwndSource? source = HwndSource.FromHwnd(hwnd);
            record.TargetBackground = source?.CompositionTarget?.BackgroundColor.ToString(CultureInfo.InvariantCulture) ?? "not available";
            NativeMethods.SetLastError(0);
            long style = IntPtr.Size is 8 ? NativeMethods.GetWindowLongPtr64(hwnd, -16).ToInt64() : NativeMethods.GetWindowLong32(hwnd, -16);
            int styleError = Marshal.GetLastWin32Error();
            record.HwndStyleWin32Error = styleError;
            if (style is 0 && styleError is not 0)
            {
                record.HwndStyleStatus = "failed";
            }
            else
            {
                record.HwndStyle = style;
                record.HwndStyleStatus = "success";
            }

            if (_window.Tag is FrameworkElement contentPanel && PresentationSource.FromVisual(contentPanel) is not null)
            {
                Point contentOrigin = contentPanel.PointToScreen(new Point(0, 0));
                record.ContentOriginPhysicalX = contentOrigin.X;
                record.ContentOriginPhysicalY = contentOrigin.Y;
                record.ContentOriginStatus = "success";
            }
            if (NativeMethods.GetWindowRect(hwnd, out NativeRect rect))
            {
                record.RectLeft = rect.Left;
                record.RectTop = rect.Top;
                record.RectRight = rect.Right;
                record.RectBottom = rect.Bottom;
            }
            else
            {
                record.HwndStatus = "GetWindowRect failed: " + Marshal.GetLastWin32Error().ToString(CultureInfo.InvariantCulture);
            }

            try
            {
                uint dpi = NativeMethods.GetDpiForWindow(hwnd);
                record.Dpi = dpi is 0 ? null : (int?)dpi;
                record.DpiStatus = dpi is 0 ? "failed: zero" : "success";
            }
            catch (EntryPointNotFoundException)
            {
                record.DpiStatus = "GetDpiForWindow unavailable";
            }

            ReadDwm(hwnd, DwmwaSystemBackdropType, out int? backdrop, out int backdropHr);
            record.DwmBackdrop = backdrop;
            record.DwmBackdropQueryHResult = backdropHr;
            record.DwmBackdropQueryStatus = backdropHr >= 0 ? "success" : "failed";
            ReadDwm(hwnd, DwmwaCornerPreference, out int? corner, out int cornerHr);
            record.DwmCorner = corner;
            record.DwmCornerQueryHResult = cornerHr;
            record.DwmCornerQueryStatus = cornerHr >= 0 ? "success" : "failed";
            ReadDwm(hwnd, DwmwaCloaked, out int? cloaked, out int cloakedHr);
            record.DwmCloaked = cloaked;
            record.DwmCloakedQueryHResult = cloakedHr;
            record.DwmCloakedQueryStatus = cloakedHr >= 0 ? "success" : "failed";
            return record;
        }

        internal static int SetBackdrop(IntPtr hwnd, string backdrop)
        {
            int value = backdrop switch { "Mica" => 2, "Acrylic" => 3, "Tabbed" => 4, _ => 1 };
            return NativeMethods.DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref value, sizeof(int));
        }

        internal static void SetDarkMode(IntPtr hwnd, string theme, out int primaryHResult, out int? fallbackHResult)
        {
            int value = string.Equals(theme, "Dark", StringComparison.Ordinal) ? 1 : 0;
            primaryHResult = NativeMethods.DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
            fallbackHResult = primaryHResult >= 0 ? null : NativeMethods.DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
        }

        internal static int SetCornerPreference(IntPtr hwnd)
        {
            int value = 2;
            return NativeMethods.DwmSetWindowAttribute(hwnd, DwmwaCornerPreference, ref value, sizeof(int));
        }

        private static void ReadDwm(IntPtr hwnd, int attribute, out int? value, out int hresult)
        {
            hresult = NativeMethods.DwmGetWindowAttribute(hwnd, attribute, out int raw, sizeof(int));
            value = hresult >= 0 ? raw : null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        // Shared .NET Framework and modern-WPF interop keeps one DllImport signature per native call.
#pragma warning disable SYSLIB1054
        private static class NativeMethods
        {
            [DllImport("dwmapi.dll", PreserveSig = true)]
            internal static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);

            [DllImport("dwmapi.dll", PreserveSig = true)]
            internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

            [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
            internal static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

            [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
            internal static extern int GetWindowLong32(IntPtr hwnd, int index);

            [DllImport("kernel32.dll")]
            internal static extern void SetLastError(int errorCode);

            [DllImport("user32.dll")]
            internal static extern uint GetDpiForWindow(IntPtr hwnd);

            [DllImport("user32.dll")]
            internal static extern int GetSystemMetrics(int index);
        }
#pragma warning restore SYSLIB1054
    }
}
