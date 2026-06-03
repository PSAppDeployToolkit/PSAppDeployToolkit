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

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// Compile-time Win32, DWM, UxTheme, and shell constants shared across the native interop
    /// layer. Values are the documented operating-system ordinals and bit flags; they are pinned
    /// by the interop tests, so changing one here is a wire-contract break, not a refactor.
    /// </summary>
    internal static class NativeConstants
    {
        // ---------------------------------------------------------------------
        // DWMWINDOWATTRIBUTE ordinals (dwmapi.h). Passed to Dwm{Set,Get}WindowAttribute.
        // ---------------------------------------------------------------------

        /// <summary>Cloaks (set) the window so DWM composes it off-screen without presenting it.</summary>
        public const int DWMWA_CLOAK = 13;

        /// <summary>Reads the cloak reason flags; zero means the window is not cloaked.</summary>
        public const int DWMWA_CLOAKED = 14;

        /// <summary>Pre-1903 immersive dark-mode attribute (Windows 10 builds 17763 to 18361).</summary>
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        /// <summary>Immersive dark-mode attribute used from Windows 10 build 18362 (1903) onward.</summary>
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        /// <summary>Selects the rounded-corner preference (one of the <c>DWMWCP_*</c> values).</summary>
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        /// <summary>Sets the window border color as a <c>COLORREF</c>.</summary>
        public const int DWMWA_BORDER_COLOR = 34;

        /// <summary>Sets the title-bar caption color as a <c>COLORREF</c>.</summary>
        public const int DWMWA_CAPTION_COLOR = 35;

        /// <summary>Sets the caption text color as a <c>COLORREF</c>.</summary>
        public const int DWMWA_TEXT_COLOR = 36;

        /// <summary>Reads the visible (DPI-scaled) frame border thickness.</summary>
        public const int DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37;

        /// <summary>Selects the system backdrop (one of the <c>DWMSBT_*</c> values); build 22621+.</summary>
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        /// <summary>Legacy Mica toggle for Windows 11 21H2 (builds 22000 to 22620).</summary>
        public const int DWMWA_MICA_EFFECT = 1029;

        // ---------------------------------------------------------------------
        // UxTheme window-theme attributes (uxtheme.h). Passed to SetWindowThemeAttribute.
        // ---------------------------------------------------------------------

        /// <summary>The <c>WTA_NONCLIENT</c> attribute selector for SetWindowThemeAttribute.</summary>
        public const int WTA_NONCLIENT = 1;

        /// <summary>Suppresses the non-client caption drawing.</summary>
        public const uint WTNCA_NODRAWCAPTION = 0x00000001;

        /// <summary>Suppresses the non-client icon drawing.</summary>
        public const uint WTNCA_NODRAWICON = 0x00000002;

        /// <summary>Hides the system menu icon.</summary>
        public const uint WTNCA_NOSYSMENU = 0x00000004;

        /// <summary>Disables the mirrored help-button behavior.</summary>
        public const uint WTNCA_NOMIRRORHELP = 0x00000008;

        /// <summary>Mask of all valid <c>WTNCA_*</c> bits.</summary>
        public const uint WTNCA_VALIDBITS = 0x0000000F;

        // ---------------------------------------------------------------------
        // DWM system backdrop types (DWM_SYSTEMBACKDROP_TYPE).
        // ---------------------------------------------------------------------

        /// <summary>Let DWM pick the backdrop automatically.</summary>
        public const int DWMSBT_AUTO = 0;

        /// <summary>No system backdrop.</summary>
        public const int DWMSBT_NONE = 1;

        /// <summary>Main-window backdrop (Mica).</summary>
        public const int DWMSBT_MAINWINDOW = 2;

        /// <summary>Transient-window backdrop (Acrylic).</summary>
        public const int DWMSBT_TRANSIENTWINDOW = 3;

        /// <summary>Tabbed-window backdrop (Mica Alt).</summary>
        public const int DWMSBT_TABBEDWINDOW = 4;

        // ---------------------------------------------------------------------
        // DWM window corner preferences (DWM_WINDOW_CORNER_PREFERENCE).
        // ---------------------------------------------------------------------

        /// <summary>Let the system decide whether to round the corners.</summary>
        public const int DWMWCP_DEFAULT = 0;

        /// <summary>Never round the corners.</summary>
        public const int DWMWCP_DONOTROUND = 1;

        /// <summary>Round the corners with the full radius.</summary>
        public const int DWMWCP_ROUND = 2;

        /// <summary>Round the corners with the small radius.</summary>
        public const int DWMWCP_ROUNDSMALL = 3;

        // ---------------------------------------------------------------------
        // DWM color sentinels for the caption/border color attributes.
        // ---------------------------------------------------------------------

        /// <summary><c>DWMWA_COLOR_NONE</c>: suppress the color so the backdrop shows through.</summary>
        public const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

        /// <summary><c>DWMWA_COLOR_DEFAULT</c>: reset the color to the system default.</summary>
        public const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);

        // ---------------------------------------------------------------------
        // Window messages (winuser.h).
        // ---------------------------------------------------------------------

        /// <summary><c>WM_NCHITTEST</c>.</summary>
        public const int WM_NCHITTEST = 0x0084;

        /// <summary><c>WM_NCLBUTTONDOWN</c>.</summary>
        public const int WM_NCLBUTTONDOWN = 0x00A1;

        /// <summary><c>WM_NCLBUTTONUP</c>.</summary>
        public const int WM_NCLBUTTONUP = 0x00A2;

        /// <summary><c>WM_NCMOUSEMOVE</c>.</summary>
        public const int WM_NCMOUSEMOVE = 0x00A0;

        /// <summary><c>WM_DPICHANGED</c>.</summary>
        public const int WM_DPICHANGED = 0x02E0;

        /// <summary><c>WM_WINDOWPOSCHANGING</c>.</summary>
        public const int WM_WINDOWPOSCHANGING = 0x0046;

        /// <summary><c>WM_WINDOWPOSCHANGED</c>.</summary>
        public const int WM_WINDOWPOSCHANGED = 0x0047;

        /// <summary><c>WM_NCCALCSIZE</c>.</summary>
        public const int WM_NCCALCSIZE = 0x0083;

        /// <summary><c>WM_NCMOUSELEAVE</c>.</summary>
        public const int WM_NCMOUSELEAVE = 0x02A2;

        /// <summary><c>WM_GETMINMAXINFO</c>.</summary>
        public const int WM_GETMINMAXINFO = 0x0024;

        /// <summary><c>WM_SETTINGCHANGE</c> (alias <c>WM_WININICHANGE</c>).</summary>
        public const int WM_SETTINGCHANGE = 0x001A;

        /// <summary><c>WM_SYSCOLORCHANGE</c>.</summary>
        public const int WM_SYSCOLORCHANGE = 0x0015;

        /// <summary><c>WM_SYSCOMMAND</c>.</summary>
        public const int WM_SYSCOMMAND = 0x0112;

        /// <summary><c>WM_THEMECHANGED</c>.</summary>
        public const int WM_THEMECHANGED = 0x031A;

        /// <summary><c>WM_DWMCOLORIZATIONCOLORCHANGED</c>.</summary>
        public const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;

        /// <summary><c>WM_DWMCOMPOSITIONCHANGED</c>.</summary>
        public const int WM_DWMCOMPOSITIONCHANGED = 0x031E;

        // ---------------------------------------------------------------------
        // System commands (the wParam of WM_SYSCOMMAND, masked with 0xFFF0).
        // ---------------------------------------------------------------------

        /// <summary><c>SC_MOVE</c>.</summary>
        public const int SC_MOVE = 0xF010;

        // ---------------------------------------------------------------------
        // WM_NCHITTEST results (winuser.h).
        // ---------------------------------------------------------------------

        /// <summary><c>HTCLIENT</c>.</summary>
        public const int HTCLIENT = 1;

        /// <summary><c>HTCAPTION</c>.</summary>
        public const int HTCAPTION = 2;

        /// <summary><c>HTMINBUTTON</c>.</summary>
        public const int HTMINBUTTON = 8;

        /// <summary><c>HTMAXBUTTON</c>; returned over the maximize button to expose the snap flyout.</summary>
        public const int HTMAXBUTTON = 9;

        /// <summary><c>HTLEFT</c>.</summary>
        public const int HTLEFT = 10;

        /// <summary><c>HTRIGHT</c>.</summary>
        public const int HTRIGHT = 11;

        /// <summary><c>HTTOP</c>.</summary>
        public const int HTTOP = 12;

        /// <summary><c>HTTOPLEFT</c>.</summary>
        public const int HTTOPLEFT = 13;

        /// <summary><c>HTTOPRIGHT</c>.</summary>
        public const int HTTOPRIGHT = 14;

        /// <summary><c>HTBOTTOM</c>.</summary>
        public const int HTBOTTOM = 15;

        /// <summary><c>HTBOTTOMLEFT</c>.</summary>
        public const int HTBOTTOMLEFT = 16;

        /// <summary><c>HTBOTTOMRIGHT</c>.</summary>
        public const int HTBOTTOMRIGHT = 17;

        /// <summary><c>HTCLOSE</c>.</summary>
        public const int HTCLOSE = 20;

        // ---------------------------------------------------------------------
        // MonitorFromWindow / MonitorFromRect flags.
        // ---------------------------------------------------------------------

        /// <summary><c>MONITOR_DEFAULTTONEAREST</c>: fall back to the nearest monitor.</summary>
        public const uint MONITOR_DEFAULTTONEAREST = 2;

        // ---------------------------------------------------------------------
        // Shell AppBar (SHAppBarMessage) messages and state bits.
        // ---------------------------------------------------------------------

        /// <summary><c>ABM_GETSTATE</c>: query the always-on-top / auto-hide state.</summary>
        public const uint ABM_GETSTATE = 0x00000004;

        /// <summary><c>ABM_GETTASKBARPOS</c>: query the primary taskbar rectangle and edge.</summary>
        public const uint ABM_GETTASKBARPOS = 0x00000005;

        /// <summary><c>ABS_AUTOHIDE</c>: the auto-hide state bit returned by <c>ABM_GETSTATE</c>.</summary>
        public const int ABS_AUTOHIDE = 0x0000001;

        // ---------------------------------------------------------------------
        // AppBar edges (the uEdge field of APPBARDATA).
        // ---------------------------------------------------------------------

        /// <summary><c>ABE_LEFT</c>.</summary>
        public const uint ABE_LEFT = 0;

        /// <summary><c>ABE_TOP</c>.</summary>
        public const uint ABE_TOP = 1;

        /// <summary><c>ABE_RIGHT</c>.</summary>
        public const uint ABE_RIGHT = 2;

        /// <summary><c>ABE_BOTTOM</c>.</summary>
        public const uint ABE_BOTTOM = 3;

        // ---------------------------------------------------------------------
        // DWM boolean attribute values (a BOOL marshalled as a 4-byte int).
        // ---------------------------------------------------------------------

        /// <summary>The DWM <c>TRUE</c> value for a BOOL window attribute.</summary>
        public const int DWM_TRUE = 1;

        /// <summary>The DWM <c>FALSE</c> value for a BOOL window attribute.</summary>
        public const int DWM_FALSE = 0;

        // ---------------------------------------------------------------------
        // Registry paths (relative to HKEY_CURRENT_USER).
        // ---------------------------------------------------------------------

        /// <summary>The personalization key holding the apps/system light-theme flags.</summary>
        public const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>The themes key holding the current theme path.</summary>
        public const string ThemesRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes";

        /// <summary>The DWM key holding colorization and accent values.</summary>
        public const string DwmRegistryPath = @"Software\Microsoft\Windows\DWM";

        /// <summary>The Explorer accent key holding the accent palette.</summary>
        public const string AccentRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

        /// <summary>The Explorer advanced key holding the snap-assist flyout flag.</summary>
        public const string ExplorerAdvancedRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

        // ---------------------------------------------------------------------
        // Registry value names.
        // ---------------------------------------------------------------------

        /// <summary><c>AppsUseLightTheme</c>.</summary>
        public const string AppsUseLightTheme = "AppsUseLightTheme";

        /// <summary><c>SystemUsesLightTheme</c>.</summary>
        public const string SystemUsesLightTheme = "SystemUsesLightTheme";

        /// <summary><c>ColorPrevalence</c>.</summary>
        public const string ColorPrevalence = "ColorPrevalence";

        /// <summary><c>AccentPalette</c>.</summary>
        public const string AccentPalette = "AccentPalette";

        /// <summary><c>AccentColor</c>.</summary>
        public const string AccentColor = "AccentColor";

        /// <summary><c>AccentColorInactive</c>.</summary>
        public const string AccentColorInactive = "AccentColorInactive";

        /// <summary><c>ColorizationColor</c>.</summary>
        public const string ColorizationColor = "ColorizationColor";

        /// <summary><c>ColorizationColorBalance</c>.</summary>
        public const string ColorizationColorBalance = "ColorizationColorBalance";

        /// <summary><c>CurrentTheme</c>.</summary>
        public const string CurrentTheme = "CurrentTheme";

        /// <summary><c>EnableSnapAssistFlyout</c>.</summary>
        public const string EnableSnapAssistFlyout = "EnableSnapAssistFlyout";
    }
}
