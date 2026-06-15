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
using System.Runtime.InteropServices;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Native
{
    // SYSLIB1054 asks for [LibraryImport] source generation, but this assembly multi-targets
    // net472 (where the source generator is unavailable) and net10, and the two TFMs must expose
    // an identical interop surface. Classic [DllImport] is the only declaration form that compiles
    // on both, so the analyzer is suppressed for this single interop file. This is the documented
    // "exceptional third-party interop" carve-out; no other file may use an inline pragma.
#pragma warning disable SYSLIB1054
    /// <summary>
    /// The native interop surface for <see cref="Controls.FluenceWindow"/> and its
    /// policy/capability helpers: DWM backdrop and frame attributes, UxTheme caption suppression,
    /// immersive color queries, monitor and taskbar geometry, layered-window presentation, and the
    /// <c>RtlGetVersion</c> OS-build probe. Every method is best-effort and handle-safe so it can be
    /// called from a presentation path without throwing.
    /// </summary>
    internal static class NativeMethods
    {
        private const string Dwmapi = "dwmapi.dll";
        private const string User32 = "user32.dll";
        private const string UxTheme = "uxtheme.dll";
        private const string Ntdll = "ntdll.dll";
        private const string Shell32 = "shell32.dll";

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        // SW_* arguments for ShowWindow (winuser.h). Only the states the caption buttons need.
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        /// <summary>The <c>HWND_BROADCAST</c> pseudo-handle for broadcasting a settings change.</summary>
        public const int HWND_BROADCAST = 0xFFFF;

        /// <summary>The <c>SMTO_ABORTIFHUNG</c> flag for <see cref="SendMessageTimeout"/>.</summary>
        public const uint SMTO_ABORTIFHUNG = 0x0002;

        #region P/Invoke declarations - User32 window styles and presentation

        [DllImport(User32, SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport(User32, SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr hWnd);

        /// <summary>
        /// Returns whether <paramref name="hWnd"/> is a valid existing window handle.
        /// </summary>
        /// <param name="hWnd">The window handle to evaluate.</param>
        /// <returns><see langword="true"/> if the handle is valid; otherwise, <see langword="false"/>.</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        /// <summary>
        /// Sends a window message with a timeout. Used by the tests to broadcast
        /// <c>WM_SETTINGCHANGE</c>/<c>ImmersiveColorSet</c> so the theme watcher re-reads the palette.
        /// </summary>
        /// <param name="hWnd">The target window handle, or <see cref="HWND_BROADCAST"/> to send to all top-level windows.</param>
        /// <param name="Msg">The message to send.</param>
        /// <param name="wParam">The WPARAM to send.</param>
        /// <param name="lParam">The LPARAM to send.</param>
        /// <param name="fuFlags">The flags for the message.</param>
        /// <param name="uTimeout">The timeout for the message.</param>
        /// <param name="lpdwResult">The result of the message.</param>
        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        #endregion P/Invoke declarations - User32 window styles and presentation

        #region P/Invoke declarations - Ntdll

        [DllImport(Ntdll, SetLastError = true)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        #endregion P/Invoke declarations - Ntdll

        #region P/Invoke declarations - DWM

        /// <summary>
        /// Sets a DWM window attribute from a 4-byte integer value.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="attr">The DWM attribute to set.</param>
        /// <param name="attrValue">The value to set for the attribute.</param>
        /// <param name="attrSize">The size of the attribute value.</param>
        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// Reads a DWM window attribute into a 4-byte integer value.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="attr">The DWM attribute to read.</param>
        /// <param name="attrValue">The value of the attribute.</param>
        /// <param name="attrSize">The size of the attribute value.</param>
        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int attrValue, int attrSize);

        /// <summary>
        /// Extends the DWM frame into the client area using the supplied margins.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="pMarInset">The margins to extend the frame into the client area.</param>
        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        /// <summary>
        /// Reads the current DWM colorization color and its opaque-blend flag.
        /// </summary>
        /// <param name="pcrColorization">The colorization color.</param>
        /// <param name="pfOpaqueBlend">Whether the colorization is opaque.</param>
        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

        /// <summary>
        /// Reports whether DWM desktop composition is enabled.
        /// </summary>
        /// <param name="pfEnabled">Whether DWM desktop composition is enabled.</param>
        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmIsCompositionEnabled(out bool pfEnabled);

        /// <summary>
        /// Reads the undocumented DWM colorization parameters (ordinal-127 export).
        /// </summary>
        /// <param name="parameters">The colorization parameters.</param>
        [DllImport(Dwmapi, EntryPoint = "#127", PreserveSig = false)]
        public static extern void DwmGetColorizationParameters(out DWMCOLORIZATIONPARAMS parameters);

        #endregion P/Invoke declarations - DWM

        #region P/Invoke declarations - User32 geometry

        /// <summary>
        /// Reads the screen rectangle of a window.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="lpRect">The screen rectangle of the window.</param>
        [DllImport(User32, SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        /// <summary>
        /// Reads the client rectangle of a window.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="lpRect">The client rectangle of the window.</param>
        [DllImport(User32, SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        /// <summary>
        /// Returns the monitor handle for a window using the supplied fallback flags.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="dwFlags">The fallback flags.</param>
        [DllImport(User32)]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        /// <summary>
        /// Fills <paramref name="lpmi"/> with the monitor and work-area rectangles.
        /// </summary>
        /// <param name="hMonitor">The handle to the monitor.</param>
        /// <param name="lpmi">The monitor information structure to fill.</param>
        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        /// <summary>
        /// Acquires a device context for a window.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        /// <summary>
        /// Releases a device context acquired with <see cref="GetDC"/>.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="hdc">The handle to the device context.</param>
        [DllImport(User32, SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        #endregion P/Invoke declarations - User32 geometry

        #region P/Invoke declarations - Shell32

        /// <summary>
        /// Sends an appbar message to the shell (taskbar state and position queries).
        /// </summary>
        /// <param name="dwMessage">The appbar message to send.</param>
        /// <param name="pData">The appbar data structure.</param>
        [DllImport(Shell32, SetLastError = true)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        #endregion P/Invoke declarations - Shell32

        #region P/Invoke declarations - UxTheme immersive color set (undocumented ordinals)

        /// <summary>
        /// Returns the number of immersive color sets.
        /// </summary>
        [DllImport(UxTheme, EntryPoint = "#94", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorSetCount();

        /// <summary>
        /// Reads an immersive color from a color set by type.
        /// </summary>
        /// <param name="dwImmersiveColorSet">The immersive color set index.</param>
        /// <param name="dwImmersiveColorType">The immersive color type index.</param>
        /// <param name="bIgnoreHighContrast">Whether to ignore high contrast settings.</param>
        /// <param name="dwHighContrastCacheMode">The high contrast cache mode.</param>
        [DllImport(UxTheme, EntryPoint = "#95", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorFromColorSetEx(
            uint dwImmersiveColorSet,
            uint dwImmersiveColorType,
            bool bIgnoreHighContrast,
            uint dwHighContrastCacheMode);

        /// <summary>
        /// Resolves an immersive color type ordinal from its name.
        /// </summary>
        /// <param name="name">The name of the immersive color type.</param>
        [DllImport(UxTheme, EntryPoint = "#96", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorTypeFromName(string name);

        /// <summary>
        /// Returns the user's active immersive color-set preference index.
        /// </summary>
        /// <param name="bForceCheckRegistry">Whether to force a registry check.</param>
        /// <param name="bSkipCheckOnFail">Whether to skip the check on failure.</param>
        [DllImport(UxTheme, EntryPoint = "#98", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        /// <summary>
        /// Sets a UxTheme non-client window theme attribute (caption suppression).
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <param name="eAttribute">The attribute to set.</param>
        /// <param name="pvAttribute">A reference to the attribute value.</param>
        /// <param name="cbAttribute">The size of the attribute value.</param>
        [DllImport(UxTheme, ExactSpelling = true, PreserveSig = true)]
        public static extern int SetWindowThemeAttribute(IntPtr hwnd, int eAttribute, ref WTA_OPTIONS pvAttribute, uint cbAttribute);

        #endregion P/Invoke declarations - UxTheme immersive color set (undocumented ordinals)

        #region DWM attribute helpers

        /// <summary>
        /// Sets a 4-byte DWM window attribute and reports success. The value is copied into a local
        /// so it can be passed by reference, matching the <c>ref int pvAttribute</c> DWM contract.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="attribute">The <c>DWMWA_*</c> attribute id.</param>
        /// <param name="value">The 4-byte value to set.</param>
        /// <returns><see langword="true"/> when DWM returns <c>S_OK</c>.</returns>
        public static bool SetWindowAttribute(IntPtr hwnd, int attribute, int value)
        {
            int result = DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
            return result == 0;
        }

        /// <summary>Sets the rounded-corner preference (one of the <c>DWMWCP_*</c> values).</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="cornerPreference">The <c>DWMWCP_*</c> value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetWindowCornerPreference(IntPtr hwnd, int cornerPreference)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_WINDOW_CORNER_PREFERENCE, cornerPreference);
        }

        /// <summary>
        /// Selects the DWM immersive dark-mode window attribute id for a given OS build. The
        /// attribute moved from <see cref="NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD"/>
        /// (19) to <see cref="NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE"/> (20) starting at
        /// Windows 10 build 18362 (version 1903). Builds 17763..18361 (1809 era) must use 19, or
        /// the dark caption silently fails to apply. This selector is pure so it can be unit
        /// tested without a window handle.
        /// </summary>
        /// <param name="osBuild">The OS build number (for example <c>18362</c>).</param>
        /// <returns>The DWM attribute id to pass to <see cref="DwmSetWindowAttribute"/>.</returns>
        public static int GetImmersiveDarkModeAttribute(int osBuild)
        {
            return osBuild >= 18362
                ? NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE
                : NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
        }

        /// <summary>Enables or disables the immersive dark caption for the current OS build.</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="enabled"><see langword="true"/> to request the dark caption.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
        {
            int value = enabled ? NativeConstants.DWM_TRUE : NativeConstants.DWM_FALSE;
            return SetWindowAttribute(hwnd, GetImmersiveDarkModeAttribute(OsVersionHelper.OsBuild), value);
        }

        /// <summary>Sets the DWM system backdrop type (one of the <c>DWMSBT_*</c> values).</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="backdropType">The <c>DWMSBT_*</c> value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetSystemBackdropType(IntPtr hwnd, int backdropType)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_SYSTEMBACKDROP_TYPE, backdropType);
        }

        /// <summary>
        /// Cloaks or uncloaks a window via <see cref="NativeConstants.DWMWA_CLOAK"/>. While cloaked,
        /// DWM keeps the window fully composed off-screen and does not present it. Retained as part
        /// of the interop contract; <see cref="Controls.FluenceWindow"/> deliberately
        /// does not cloak (its first-paint flash is solved by clearing the redirection surface), so
        /// the never-cloak invariant is asserted by the harden tests via
        /// <see cref="GetWindowCloakedState"/>. Any caller that does cloak MUST guarantee a matching
        /// uncloak; a window left cloaked is invisible.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="cloak"><see langword="true"/> to cloak, <see langword="false"/> to uncloak.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetWindowCloak(IntPtr hwnd, bool cloak)
        {
            int value = cloak ? NativeConstants.DWM_TRUE : NativeConstants.DWM_FALSE;
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_CLOAK, value);
        }

        /// <summary>
        /// Reads the read-only <see cref="NativeConstants.DWMWA_CLOAKED"/> attribute, returning the
        /// reason flags for why the window is cloaked. Zero means the window is not cloaked. Returns
        /// zero on any failure (for example when DWM composition is disabled).
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns>The cloak reason flags, or zero when not cloaked or on failure.</returns>
        public static int GetWindowCloakedState(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return 0;
            }
            int result = DwmGetWindowAttribute(hwnd, NativeConstants.DWMWA_CLOAKED, out int cloaked, sizeof(int));
            return result == 0 ? cloaked : 0;
        }

        /// <summary>Toggles the legacy Windows 11 21H2 Mica effect (<c>DWMWA_MICA_EFFECT</c>).</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="enabled"><see langword="true"/> to enable legacy Mica.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetMicaEffect(IntPtr hwnd, bool enabled)
        {
            int value = enabled ? NativeConstants.DWM_TRUE : NativeConstants.DWM_FALSE;
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_MICA_EFFECT, value);
        }

        /// <summary>Sets the title-bar caption color (a <c>COLORREF</c> or a <c>DWMWA_COLOR_*</c> sentinel).</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="color">The caption color value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetCaptionColor(IntPtr hwnd, int color)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_CAPTION_COLOR, color);
        }

        /// <summary>
        /// Suppresses Win32 default non-client caption drawing so the DWM backdrop shows
        /// through cleanly. Best-effort: classic themes return <c>S_FALSE</c> which is treated
        /// as a no-op success.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> when the attribute applied (<c>S_OK</c> or <c>S_FALSE</c>).</returns>
        public static bool SuppressNonClientCaptionDraw(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }
            WTA_OPTIONS opts = new()
            {
                Flags = NativeConstants.WTNCA_NODRAWCAPTION,
                Mask = NativeConstants.WTNCA_NODRAWCAPTION,
            };
            int hr = SetWindowThemeAttribute(hwnd, NativeConstants.WTA_NONCLIENT, ref opts, (uint)Marshal.SizeOf<WTA_OPTIONS>());
            return hr >= 0; // S_OK or S_FALSE
        }

        /// <summary>Sets the window border color (a <c>COLORREF</c> or a <c>DWMWA_COLOR_*</c> sentinel).</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="color">The border color value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetBorderColor(IntPtr hwnd, int color)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_BORDER_COLOR, color);
        }

        /// <summary>
        /// Extends the DWM frame across the entire client area (the "sheet of glass" margins of
        /// <c>-1</c> on every edge), letting the backdrop composite behind the whole window.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool ExtendFrameIntoClientArea(IntPtr hwnd)
        {
            MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            int result = DwmExtendFrameIntoClientArea(hwnd, ref margins);
            return result == 0;
        }

        /// <summary>
        /// Packs a <see cref="System.Windows.Media.Color"/> into the <c>0x00BBGGRR</c> COLORREF
        /// layout that DWM color attributes such as <see cref="NativeConstants.DWMWA_BORDER_COLOR"/>
        /// expect; the alpha channel is ignored. Despite the historical "ABGR" naming, the byte
        /// order produced here is COLORREF, so callers must not reuse it for an attribute that
        /// genuinely expects ABGR with a meaningful alpha channel.
        /// </summary>
        /// <param name="color">The source color.</param>
        /// <returns>The packed COLORREF value.</returns>
        public static int ColorToColorRef(System.Windows.Media.Color color)
        {
            return (color.B << 16) | (color.G << 8) | color.R;
        }

        /// <summary>Returns whether DWM desktop composition is currently enabled.</summary>
        /// <returns><see langword="true"/> when composition is enabled.</returns>
        public static bool IsCompositionEnabled()
        {
            int result = DwmIsCompositionEnabled(out bool enabled);
            return result == 0 && enabled;
        }

        /// <summary>Rounds the window corners with the full radius (<c>DWMWCP_ROUND</c>).</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool RoundWindowCorner(IntPtr hwnd)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_WINDOW_CORNER_PREFERENCE, NativeConstants.DWMWCP_ROUND);
        }

        #endregion DWM attribute helpers

        #region Window style and presentation helpers

        /// <summary>
        /// Strips <c>WS_SYSMENU</c> from the window style so the native caption (and its buttons)
        /// stops painting over the custom Fluent caption.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        public static void HideAllWindowButtons(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_STYLE);
            _ = SetWindowLong(hwnd, GWL_STYLE, style & ~WS_SYSMENU);
        }

        /// <summary>
        /// Minimizes a window through the native <c>ShowWindow</c> API. Used as a belt-and-braces
        /// fallback from the custom caption's minimize handler so minimize is guaranteed to work
        /// even when the chrome has stripped <c>WS_SYSMENU</c>/<c>WS_MINIMIZEBOX</c> (which blocks
        /// <c>SC_MINIMIZE</c> via <c>DefWindowProc</c>), the window is <c>NoResize</c>, topmost, or
        /// shown via <c>ShowDialog()</c> inside a nested dispatcher frame. <c>ShowWindow</c> honors
        /// <c>SW_MINIMIZE</c> regardless of window styles, so it cannot be silently gated the way
        /// <c>WM_SYSCOMMAND</c> can.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> when the window is (or becomes) minimized.</returns>
        public static bool MinimizeWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && (IsIconic(hwnd) || ShowWindow(hwnd, SW_MINIMIZE));
        }

        /// <summary>Maximizes a window through the native <c>ShowWindow</c> API.</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> when the window is (or becomes) maximized.</returns>
        public static bool MaximizeWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && (IsZoomed(hwnd) || ShowWindow(hwnd, SW_MAXIMIZE));
        }

        /// <summary>Restores a window through the native <c>ShowWindow</c> API.</summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> when the restore call succeeds.</returns>
        public static bool RestoreWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && ShowWindow(hwnd, SW_RESTORE);
        }

        #endregion Window style and presentation helpers

        #region OS version and taskbar helpers

        /// <summary>
        /// Reads the true OS version via <c>RtlGetVersion</c>, which (unlike the manifest-shimmed
        /// <c>GetVersionEx</c>) reports the real build number the DWM feature gates depend on.
        /// </summary>
        /// <returns>The OS version (major, minor, build, revision).</returns>
        /// <exception cref="InvalidOperationException">Thrown when <c>RtlGetVersion</c> fails.</exception>
        public static Version GetRealOsVersion()
        {
            OSVERSIONINFOEX versionInfo = new()
            {
                OSVersionInfoSize = Marshal.SizeOf<OSVERSIONINFOEX>(),
                CSDVersion = string.Empty,
            };

            int result = RtlGetVersion(ref versionInfo);
            return result != 0
                ? throw new InvalidOperationException("RtlGetVersion failed.")
                : new Version(
                    versionInfo.MajorVersion,
                    versionInfo.MinorVersion,
                    versionInfo.BuildNumber);
        }

        /// <summary>
        /// Returns <see langword="true"/> when the Windows taskbar is currently in auto-hide
        /// mode. Queries the shell with <see cref="NativeConstants.ABM_GETSTATE"/> and tests the
        /// <see cref="NativeConstants.ABS_AUTOHIDE"/> bit of the returned state.
        /// </summary>
        /// <returns><see langword="true"/> when the taskbar is auto-hide.</returns>
        public static bool IsTaskbarAutoHide()
        {
            APPBARDATA data = new() { cbSize = Marshal.SizeOf<APPBARDATA>() };
            IntPtr state = SHAppBarMessage(NativeConstants.ABM_GETSTATE, ref data);
            return (state.ToInt64() & NativeConstants.ABS_AUTOHIDE) != 0;
        }

        /// <summary>
        /// Returns the screen edge (one of the <c>ABE_*</c> values) on which the auto-hide
        /// taskbar is docked, or <see langword="null"/> when the taskbar is not auto-hide or the
        /// query is unavailable.
        /// </summary>
        /// <param name="monitor">
        /// The monitor a caller intends to match the taskbar against. <see cref="SHAppBarMessage"/>
        /// with <see cref="NativeConstants.ABM_GETTASKBARPOS"/> reports only the primary taskbar,
        /// so this implementation returns the primary taskbar edge and ignores the monitor on
        /// multi-monitor setups. The parameter is retained so a future caller can match per
        /// monitor without an API break.
        /// </param>
        /// <returns>The auto-hide taskbar edge, or <see langword="null"/>.</returns>
        public static uint? GetAutoHideTaskbarEdge(IntPtr monitor)
        {
            _ = monitor;
            if (!IsTaskbarAutoHide())
            {
                return null;
            }
            APPBARDATA data = new() { cbSize = Marshal.SizeOf<APPBARDATA>() };
            IntPtr result = SHAppBarMessage(NativeConstants.ABM_GETTASKBARPOS, ref data);
            return result == IntPtr.Zero ? null : data.uEdge;
        }

        /// <summary>
        /// Shifts a maximized window rect inward by 2 px on the auto-hide taskbar edge so the
        /// maximized window does not fully cover the taskbar, which would block its hover-reveal.
        /// </summary>
        /// <param name="mmi">The min/max info whose maximized rect is adjusted in place.</param>
        /// <param name="edge">The auto-hide taskbar edge (one of the <c>ABE_*</c> values).</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3532:Empty \"default\" clauses should be removed", Justification = "This is deliberate.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1070:Remove redundant default switch section", Justification = "This is deliberate.")]
        public static void ApplyAutoHideTaskbarShift(ref MINMAXINFO mmi, uint edge)
        {
            switch (edge)
            {
                case NativeConstants.ABE_LEFT:
                    mmi.ptMaxPosition.X += 2;
                    mmi.ptMaxSize.X -= 2;
                    break;
                case NativeConstants.ABE_TOP:
                    mmi.ptMaxPosition.Y += 2;
                    mmi.ptMaxSize.Y -= 2;
                    break;
                case NativeConstants.ABE_RIGHT:
                    mmi.ptMaxSize.X -= 2;
                    break;
                case NativeConstants.ABE_BOTTOM:
                    mmi.ptMaxSize.Y -= 2;
                    break;
                default:
                    break;
            }
        }

        #endregion OS version and taskbar helpers
    }
#pragma warning restore SYSLIB1054
}
