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
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fluence.Wpf.Helpers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// The native interop surface for <see cref="Controls.FluenceWindow"/> and its
    /// policy/capability helpers: DWM backdrop and frame attributes, UxTheme caption suppression,
    /// immersive color queries, monitor and taskbar geometry, layered-window presentation, and the
    /// <c>RtlGetVersion</c> OS-build probe. Every method is best-effort and handle-safe so it can be
    /// called from a presentation path without throwing.
    /// </summary>
    internal static class NativeMethods
    {
        private const string UxTheme = "uxtheme.dll";

        #region P/Invoke declarations

        /// <summary>
        /// Reads the undocumented DWM colorization parameters (ordinal-127 export).
        /// </summary>
        /// <param name="parameters">The colorization parameters.</param>
        [DllImport("dwmapi.dll", EntryPoint = "#127", PreserveSig = false), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern void DwmGetColorizationParameters(out DWMCOLORIZATIONPARAMS parameters);

        /// <summary>
        /// Sends an appbar message to the shell (taskbar state and position queries).
        /// </summary>
        /// <param name="dwMessage">The appbar message to send.</param>
        /// <param name="pData">The appbar data structure.</param>
        [DllImport("shell32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        /// <summary>
        /// Returns the number of immersive color sets.
        /// </summary>
        [DllImport(UxTheme, EntryPoint = "#94", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint GetImmersiveColorSetCount();

        /// <summary>
        /// Reads an immersive color from a color set by type.
        /// </summary>
        /// <param name="dwImmersiveColorSet">The immersive color set index.</param>
        /// <param name="dwImmersiveColorType">The immersive color type index.</param>
        /// <param name="bIgnoreHighContrast">Whether to ignore high contrast settings.</param>
        /// <param name="dwHighContrastCacheMode">The high contrast cache mode.</param>
        [DllImport(UxTheme, EntryPoint = "#95", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);

        /// <summary>
        /// Resolves an immersive color type ordinal from its name.
        /// </summary>
        /// <param name="name">The name of the immersive color type.</param>
        [DllImport(UxTheme, EntryPoint = "#96", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint GetImmersiveColorTypeFromName(string name);

        /// <summary>
        /// Returns the user's active immersive color-set preference index.
        /// </summary>
        /// <param name="bForceCheckRegistry">Whether to force a registry check.</param>
        /// <param name="bSkipCheckOnFail">Whether to skip the check on failure.</param>
        [DllImport(UxTheme, EntryPoint = "#98", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        #endregion P/Invoke declarations

        #region DWM attribute helpers

        /// <summary>
        /// Sets a 4-byte DWM window attribute and reports success. The value is copied into a local
        /// so it can be passed by reference, matching the <c>ref int pvAttribute</c> DWM contract.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="attribute">The <c>DWMWA_*</c> attribute id.</param>
        /// <param name="value">The 4-byte value to set.</param>
        /// <returns><see langword="true"/> when DWM returns <c>S_OK</c>.</returns>
        public static bool SetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, uint value)
        {
            Span<byte> valueSpan = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(valueSpan, value);
            int result = PInvoke.DwmSetWindowAttribute((HWND)hwnd, attribute, valueSpan);
            return result is 0;
        }

        /// <summary>
        /// Sets the rounded-corner preference (one of the <c>DWMWCP_*</c> values).
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="cornerPreference">The <c>DWMWCP_*</c> value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetWindowCornerPreference(IntPtr hwnd, DWM_WINDOW_CORNER_PREFERENCE cornerPreference)
        {
            return SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, (uint)cornerPreference);
        }

        /// <summary>
        /// Selects the DWM immersive dark-mode window attribute id for a given OS build. The
        /// attribute moved from DWMWA_USE_IMMERSIVE_DARK_MODE_OLD
        /// (19) to DWMWA_USE_IMMERSIVE_DARK_MODE (20) starting at
        /// Windows 10 build 18362 (version 1903). Builds 17763..18361 (1809 era) must use 19, or
        /// the dark caption silently fails to apply. This selector is pure so it can be unit
        /// tested without a window handle.
        /// </summary>
        /// <param name="osBuild">The OS build number (for example <c>18362</c>).</param>
        /// <returns>The DWM attribute id to pass to DwmSetWindowAttribute.</returns>
        public static DWMWINDOWATTRIBUTE GetImmersiveDarkModeAttribute(int osBuild)
        {
            const DWMWINDOWATTRIBUTE DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = (DWMWINDOWATTRIBUTE)19;
            return osBuild >= 18362
                ? DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE
                : DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
        }

        /// <summary>
        /// Enables or disables the immersive dark caption for the current OS build.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="enabled"><see langword="true"/> to request the dark caption.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
        {
            uint value = enabled ? 1u : 0u;
            return SetWindowAttribute(hwnd, GetImmersiveDarkModeAttribute(OsVersionHelper.OsBuild), value);
        }

        /// <summary>
        /// Sets the DWM system backdrop type (one of the <c>DWMSBT_*</c> values).
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="backdropType">The <c>DWMSBT_*</c> value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetSystemBackdropType(IntPtr hwnd, DWM_SYSTEMBACKDROP_TYPE backdropType)
        {
            return SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, (uint)backdropType);
        }

        /// <summary>
        /// Cloaks or uncloaks a window via DWMWA_CLOAK. While cloaked,
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
            uint value = cloak ? 1u : 0u;
            return SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAK, value);
        }

        /// <summary>
        /// Reads the read-only DWMWA_CLOAKED attribute, returning the
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
            Span<byte> cloakedSpan = stackalloc byte[sizeof(int)];
            int result = PInvoke.DwmGetWindowAttribute((HWND)hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, cloakedSpan);
            return result is 0 ? BinaryPrimitives.ReadInt32LittleEndian(cloakedSpan) : 0;
        }

        /// <summary>
        /// Toggles the legacy Windows 11 21H2 Mica effect (<c>DWMWA_MICA_EFFECT</c>).
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="enabled"><see langword="true"/> to enable legacy Mica.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetMicaEffect(IntPtr hwnd, bool enabled)
        {
            const DWMWINDOWATTRIBUTE DWMWA_MICA_EFFECT = (DWMWINDOWATTRIBUTE)1029;
            uint value = enabled ? 1u : 0u;
            return SetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, value);
        }

        /// <summary>
        /// Sets the title-bar caption color (a <c>COLORREF</c> or a <c>DWMWA_COLOR_*</c> sentinel).
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="color">The caption color value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetCaptionColor(IntPtr hwnd, uint color)
        {
            return SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, color);
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
            Span<byte> optsSpan = stackalloc byte[Marshal.SizeOf<WTA_OPTIONS>()];
            ref WTA_OPTIONS optsRef = ref Unsafe.As<byte, WTA_OPTIONS>(ref MemoryMarshal.GetReference(optsSpan));
            optsRef = new()
            {
                dwFlags = PInvoke.WTNCA_NODRAWCAPTION,
                dwMask = PInvoke.WTNCA_NODRAWCAPTION,
            };
            int hr = PInvoke.SetWindowThemeAttribute((HWND)hwnd, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, optsSpan);
            return hr >= 0; // S_OK or S_FALSE
        }

        /// <summary>
        /// Sets the window border color (a <c>COLORREF</c> or a <c>DWMWA_COLOR_*</c> sentinel).
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="color">The border color value.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool SetBorderColor(IntPtr hwnd, uint color)
        {
            return SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, color);
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
            int result = PInvoke.DwmExtendFrameIntoClientArea((HWND)hwnd, in margins);
            return result is 0;
        }

        /// <summary>
        /// Packs a <see cref="System.Windows.Media.Color"/> into the <c>0x00BBGGRR</c> COLORREF
        /// layout that DWM color attributes such as DWMWA_BORDER_COLOR
        /// expect; the alpha channel is ignored. Despite the historical "ABGR" naming, the byte
        /// order produced here is COLORREF, so callers must not reuse it for an attribute that
        /// genuinely expects ABGR with a meaningful alpha channel.
        /// </summary>
        /// <param name="color">The source color.</param>
        /// <returns>The packed COLORREF value.</returns>
        public static uint ColorToColorRef(System.Windows.Media.Color color)
        {
            return (uint)((color.B << 16) | (color.G << 8) | color.R);
        }

        /// <summary>
        /// Returns whether DWM desktop composition is currently enabled.
        /// </summary>
        /// <returns><see langword="true"/> when composition is enabled.</returns>
        public static bool IsCompositionEnabled()
        {
            int result = PInvoke.DwmIsCompositionEnabled(out BOOL enabled);
            return result is 0 && enabled;
        }

        /// <summary>
        /// Rounds the window corners with the full radius (<c>DWMWCP_ROUND</c>).
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool RoundWindowCorner(IntPtr hwnd)
        {
            return SetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
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
            WINDOW_STYLE style = (WINDOW_STYLE)PInvoke.GetWindowLong((HWND)hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            _ = PInvoke.SetWindowLong((HWND)hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)(style & ~WINDOW_STYLE.WS_SYSMENU));
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
            return hwnd != IntPtr.Zero && (PInvoke.IsIconic((HWND)hwnd) || PInvoke.ShowWindow((HWND)hwnd, SHOW_WINDOW_CMD.SW_MINIMIZE));
        }

        /// <summary>
        /// Maximizes a window through the native <c>ShowWindow</c> API.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> when the window is (or becomes) maximized.</returns>
        public static bool MaximizeWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && (PInvoke.IsZoomed((HWND)hwnd) || PInvoke.ShowWindow((HWND)hwnd, SHOW_WINDOW_CMD.SW_MAXIMIZE));
        }

        /// <summary>
        /// Restores a window through the native <c>ShowWindow</c> API.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <returns><see langword="true"/> when the restore call succeeds.</returns>
        public static bool RestoreWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && PInvoke.ShowWindow((HWND)hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
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
            OSVERSIONINFOW versionInfo = new()
            {
                dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOW>(),
            };

            int result = Windows.Wdk.PInvoke.RtlGetVersion(ref versionInfo);
            return result is not 0
                ? throw new InvalidOperationException("RtlGetVersion failed.")
                : new Version(
                    (int)versionInfo.dwMajorVersion,
                    (int)versionInfo.dwMinorVersion,
                    (int)versionInfo.dwBuildNumber);
        }

        /// <summary>
        /// Returns <see langword="true"/> when the Windows taskbar is currently in auto-hide
        /// mode. Queries the shell with ABM_GETSTATE and tests the
        /// ABS_AUTOHIDE bit of the returned state.
        /// </summary>
        /// <returns><see langword="true"/> when the taskbar is auto-hide.</returns>
        public static bool IsTaskbarAutoHide()
        {
            APPBARDATA data = new() { cbSize = Marshal.SizeOf<APPBARDATA>() };
            IntPtr state = SHAppBarMessage(PInvoke.ABM_GETSTATE, ref data);
            return (state.ToInt64() & PInvoke.ABS_AUTOHIDE) != 0;
        }

        /// <summary>
        /// Returns the screen edge (one of the <c>ABE_*</c> values) on which the auto-hide
        /// taskbar is docked, or <see langword="null"/> when the taskbar is not auto-hide or the
        /// query is unavailable.
        /// </summary>
        /// <param name="monitor">
        /// The monitor a caller intends to match the taskbar against. <see cref="SHAppBarMessage"/>
        /// with ABM_GETTASKBARPOS reports only the primary taskbar,
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
            IntPtr result = SHAppBarMessage(PInvoke.ABM_GETTASKBARPOS, ref data);
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
                case PInvoke.ABE_LEFT:
                    mmi.ptMaxPosition.X += 2;
                    mmi.ptMaxSize.X -= 2;
                    break;
                case PInvoke.ABE_TOP:
                    mmi.ptMaxPosition.Y += 2;
                    mmi.ptMaxSize.Y -= 2;
                    break;
                case PInvoke.ABE_RIGHT:
                    mmi.ptMaxSize.X -= 2;
                    break;
                case PInvoke.ABE_BOTTOM:
                    mmi.ptMaxSize.Y -= 2;
                    break;
                default:
                    break;
            }
        }

        #endregion OS version and taskbar helpers
    }
}
