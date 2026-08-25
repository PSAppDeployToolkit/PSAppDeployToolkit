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

using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using Windows.Win32;
using Windows.Win32.Graphics.Dwm;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Pure-logic policy layer for <see cref="FluenceWindow"/>. All methods are stateless and
    /// side-effect-free so they can be unit tested without a window handle. The class maps the
    /// requested <see cref="BackdropType"/> and OS capabilities to concrete DWM instructions and
    /// WPF <see cref="WindowChrome"/> parameters, insulating the window code-behind from the
    /// capability-detection and downgrade rules.
    /// </summary>
    internal static class WindowPolicy
    {
        /// <summary>
        /// Constructs a canonical <see cref="WindowChrome"/> for a <see cref="FluenceWindow"/>.
        /// The chrome is fixed regardless of backdrop or resize mode; per-state adjustments are
        /// made afterward via <see cref="GetResizeBorderThickness"/> and
        /// <see cref="GetGlassFrameThickness"/>.
        /// </summary>
        /// <remarks>
        /// Parameter choices:
        /// <list type="bullet">
        ///   <item><term>CaptionHeight = 0</term><description>
        ///     Routes all title-bar hits through <c>WM_NCHITTEST</c> so the custom caption can
        ///     distinguish drag, resize, snap, and button regions.
        ///   </description></item>
        ///   <item><term>GlassFrameThickness = -1</term><description>
        ///     Extends DWM glass into the full client area; overridden per-window by
        ///     <see cref="GetGlassFrameThickness"/> when no backdrop is active.
        ///   </description></item>
        ///   <item><term>ResizeBorderThickness = 4</term><description>
        ///     Matches the WinUI 3 / .NET 10 WPF FluentWindow invisible hit margin.
        ///   </description></item>
        ///   <item><term>UseAeroCaptionButtons = false</term><description>
        ///     Disables the native Aero caption buttons; Fluence renders its own.
        ///   </description></item>
        ///   <item><term>NonClientFrameEdges = None</term><description>
        ///     Lets the client area extend into the caption strip.
        ///   </description></item>
        ///   <item><term>CornerRadius = 0</term><description>
        ///     WPF-side rounding is off; rounded corners are driven by
        ///     <c>DWMWA_WINDOW_CORNER_PREFERENCE</c> to avoid clipping the DWM backdrop.
        ///   </description></item>
        /// </list>
        /// </remarks>
        /// <returns>A new <see cref="WindowChrome"/> with the canonical Fluence settings.</returns>
        internal static WindowChrome CreateWindowChrome()
        {
            return new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = new Thickness(4),
                UseAeroCaptionButtons = false,
                NonClientFrameEdges = NonClientFrameEdges.None,
            };
        }

        /// <summary>
        /// Returns the <see cref="WindowChrome.GlassFrameThickness"/> appropriate for the given
        /// backdrop and shadow state.
        /// </summary>
        /// <remarks>
        /// When a DWM backdrop is active (<see cref="BackdropType.Mica"/>,
        /// <see cref="BackdropType.Acrylic"/>, <see cref="BackdropType.Tabbed"/>, or
        /// <see cref="BackdropType.Auto"/>), the thickness is <c>-1</c> so DWM extends the glass
        /// into the client area and the backdrop shows through. The same <c>-1</c> is used when
        /// the caller requests a drop shadow without a backdrop, because the shadow is rendered
        /// via the DWM glass frame. When neither is active the thickness is a very-thin-but-nonzero
        /// value (<c>0.00001</c>) so the resize border continues to hit-test while
        /// <see cref="WindowChrome"/>'s renderer does not paint a visible glass-frame artifact.
        /// This dual-path is intentional: setting <c>-1</c> with
        /// <see cref="BackdropType.None"/> on Windows 11 renders a visible glass artifact,
        /// so the tiny nonzero value is the correct prevention mechanism.
        /// <para>
        /// The input is the <em>requested</em> backdrop, not the effective one, so a Windows 10
        /// window that asked for a backdrop it cannot get still receives <c>-1</c>. That is
        /// harmless on Windows 11 and matches the shipped behavior, but it is unverified against
        /// the Windows 10 legacy acrylic path: if visual verification on real Windows 10 hardware
        /// shows a glass-frame artifact around an accent-blurred window, switch this to be driven
        /// by the effective backdrop from <see cref="ResolveEffectiveBackdrop"/> instead.
        /// </para>
        /// </remarks>
        /// <param name="backdrop">The requested (not necessarily effective) backdrop type.</param>
        /// <param name="hasShadow">
        ///   <see langword="true"/> when <see cref="FluenceWindow.HasShadow"/> is set, requiring
        ///   the DWM glass frame for shadow rendering even without a system backdrop.
        /// </param>
        /// <returns>The glass-frame thickness to assign to
        /// <see cref="WindowChrome.GlassFrameThickness"/>.</returns>
        internal static Thickness GetGlassFrameThickness(BackdropType backdrop, bool hasShadow)
        {
            return backdrop is not BackdropType.None || hasShadow
                ? new Thickness(-1)
                : new Thickness(0.00001);
        }

        /// <summary>
        /// Returns the <see cref="WindowChrome.ResizeBorderThickness"/> appropriate for the given
        /// window state and resize mode.
        /// </summary>
        /// <remarks>
        /// A maximized window has no visible resize handles; using 4 dp would let the border bleed
        /// over the taskbar or adjacent monitors. <see cref="ResizeMode.NoResize"/> and
        /// <see cref="ResizeMode.CanMinimize"/> forbid drag-resize so their hit margin is also
        /// zero.
        /// </remarks>
        /// <param name="windowState">The current <see cref="WindowState"/>.</param>
        /// <param name="resizeMode">The current <see cref="ResizeMode"/>.</param>
        /// <returns>
        ///   <c>Thickness(4)</c> when the window can be resized in a normal state;
        ///   <c>Thickness(0)</c> when maximized, <see cref="ResizeMode.NoResize"/>, or
        ///   <see cref="ResizeMode.CanMinimize"/>.
        /// </returns>
        internal static Thickness GetResizeBorderThickness(WindowState windowState, ResizeMode resizeMode)
        {
            return windowState is WindowState.Maximized
                || resizeMode is ResizeMode.NoResize
                or ResizeMode.CanMinimize
                ? new Thickness(0)
                : new Thickness(4);
        }

        /// <summary>
        /// Computes the <see cref="FramePlan"/> that governs the window border appearance.
        /// </summary>
        /// <remarks>
        /// The plan has two halves, and DWM owns the outer one:
        /// <list type="bullet">
        ///   <item>
        ///     <term>WPF-template border</term>
        ///     <description>
        ///       An active window with accent borders enabled gets a 2 dp border keyed to
        ///       <c>SystemAccentColorBrush</c>. Inactive windows, and windows whose accent borders
        ///       are off, get <c>CardStrokeColorDefaultSolidBrush</c>, which is theme-matched to the
        ///       window surface and so reads as part of it rather than as a second outline.
        ///       Maximized windows get a 0-thick border in every state, because a border at the
        ///       monitor edge would clip against the taskbar or an adjacent monitor.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>DWM border color</term>
        ///     <description>
        ///       When the OS supports <c>DWMWA_BORDER_COLOR</c> and the window is active with accent
        ///       borders, the COLORREF derived from <paramref name="accentColor"/> is emitted.
        ///       Otherwise DWMWA_COLOR_DEFAULT is used, which tells DWM to draw its own border.
        ///     </description>
        ///   </item>
        /// </list>
        /// <para>
        /// The DWM border is deliberately not suppressed with the DWMWA_COLOR_NONE sentinel. Doing
        /// that leaves the template hairline as the only outline, and a brush painted inside the
        /// client area composites over the window's own surface, never over what is behind the
        /// window. In Light theme that reads as a bright ring around the window on a dark desktop
        /// whatever stroke token is chosen, because no light-surface blend can be as dark as the
        /// border Windows itself draws. The DWM border composites semi-transparently against the
        /// desktop, which is exactly why it adapts, so Fluence lets it do that job and keeps the
        /// template border theme-matched underneath it.
        /// </para>
        /// </remarks>
        /// <param name="windowState">The current <see cref="WindowState"/>.</param>
        /// <param name="isActive">
        ///   <see langword="true"/> when <see cref="FluenceWindow"/> is the foreground window.
        /// </param>
        /// <param name="isAccentBorderEnabled">
        ///   <see langword="true"/> when <c>ApplicationAccentColorManager.IsAccentColorOnTitleBarsEnabled</c>
        ///   is set.
        /// </param>
        /// <param name="capabilities">The OS capability snapshot.</param>
        /// <param name="accentColor">The current system accent color.</param>
        /// <returns>A <see cref="FramePlan"/> describing the border to apply.</returns>
        internal static FramePlan BuildFramePlan(
            WindowState windowState,
            bool isActive,
            bool isAccentBorderEnabled,
            WindowCapabilities capabilities,
            Color accentColor)
        {
            // Where DWM can draw the border, it is the only border: it composites against the
            // desktop, so it is the one outline that can look right over any backdrop. A template
            // border underneath it paints inside the client area over the window's own surface, so
            // whatever token it uses shows as a pale line between the system border and the
            // content, which is the halo this window used to draw in Light theme. Windows 10 has no
            // DWMWA_BORDER_COLOR, so there the template border is still the outline and keeps its
            // 2 dp. The thickness does not vary with activation in either case: it feeds layout, and
            // a per-state value would shift the content every time the window is focused.
            Thickness templateBorderThickness = windowState is WindowState.Maximized || capabilities.SupportsBorderColor
                ? new Thickness(0)
                : new Thickness(2);

            string templateBorderBrushResourceKey = !isActive || !isAccentBorderEnabled
                ? "CardStrokeColorDefaultSolidBrush"
                : "SystemAccentColorBrush";

            uint dwmBorderColor = PInvoke.DWMWA_COLOR_DEFAULT;
            if (capabilities.SupportsBorderColor && isActive && isAccentBorderEnabled)
            {
                dwmBorderColor = NativeMethods.ColorToColorRef(accentColor);
            }

            return new FramePlan(templateBorderThickness, templateBorderBrushResourceKey, dwmBorderColor);
        }

        /// <summary>
        /// Resolves the <see cref="BackdropType"/> that will actually be applied after
        /// downgrading for OS capability gaps.
        /// </summary>
        /// <remarks>
        /// Downgrade rules:
        /// <list type="bullet">
        ///   <item>
        ///     <term>Auto / Mica</term>
        ///     <description>
        ///       Resolves to <see cref="BackdropType.Mica"/> when either
        ///       <c>DWMWA_SYSTEMBACKDROP_TYPE</c> (22H2+) or the legacy
        ///       <c>DWMWA_MICA_EFFECT</c> (21H2) is available; falls back to
        ///       <see cref="BackdropType.None"/> on Windows 10.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Acrylic / Tabbed</term>
        ///     <description>
        ///       Passes through on 22H2+. Downgrades to Mica on pre-22H2 Win11 (only
        ///       <c>DWMWA_MICA_EFFECT</c> is available there). On Windows 10, Acrylic survives as
        ///       itself when the legacy accent path is usable (see
        ///       <see cref="ResolveTransparentBackdrop"/>) and otherwise downgrades to None;
        ///       Tabbed always downgrades to None there, because the legacy accent policy has no
        ///       tabbed equivalent.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>None</term>
        ///     <description>Never upgraded, regardless of OS capabilities.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="requestedBackdrop">The <see cref="BackdropType"/> requested by the caller.</param>
        /// <param name="capabilities">The OS capability snapshot.</param>
        /// <param name="isTransparencyEnabled">
        ///   <see langword="true"/> when the OS transparency-effects toggle is on. Defaults to
        ///   <see langword="false"/>, which suppresses the Windows 10 legacy acrylic path: a caller
        ///   that does not read the setting must not get a blur the user has turned off.
        /// </param>
        /// <param name="resolvedTheme">
        ///   The resolved application theme. Only <see cref="ApplicationTheme.HighContrast"/>
        ///   changes the outcome, by suppressing the Windows 10 legacy acrylic path.
        /// </param>
        /// <returns>The effective <see cref="BackdropType"/> to apply.</returns>
        internal static BackdropType ResolveEffectiveBackdrop(
            BackdropType requestedBackdrop,
            WindowCapabilities capabilities,
            bool isTransparencyEnabled = false,
            ApplicationTheme resolvedTheme = ApplicationTheme.Light)
        {
            return requestedBackdrop switch
            {
                BackdropType.Auto or BackdropType.Mica =>
                    capabilities.SupportsSystemBackdropType || capabilities.SupportsMicaEffect
                        ? BackdropType.Mica
                        : BackdropType.None,

                BackdropType.Acrylic or BackdropType.Tabbed =>
                    ResolveTransparentBackdrop(requestedBackdrop, capabilities, isTransparencyEnabled, resolvedTheme),

                BackdropType.None or _ => requestedBackdrop,
            };
        }

        /// <summary>
        /// Resolves <see cref="BackdropType.Acrylic"/> and <see cref="BackdropType.Tabbed"/> against
        /// the three mutually exclusive transparency mechanisms, newest first.
        /// </summary>
        /// <remarks>
        /// <list type="number">
        ///   <item>
        ///     <c>DWMWA_SYSTEMBACKDROP_TYPE</c> (22H2+) expresses both requests natively, so the
        ///     request passes through untouched.
        ///   </item>
        ///   <item>
        ///     <c>DWMWA_MICA_EFFECT</c> (21H2) expresses neither, so both downgrade to the Mica it
        ///     does express, which is closer to the request than an opaque window.
        ///   </item>
        ///   <item>
        ///     Windows 10 has neither attribute but does have the legacy accent policy, which
        ///     expresses acrylic and nothing else. Acrylic therefore survives when the build
        ///     supports it, the user has transparency effects on, and the theme is not high
        ///     contrast; high contrast is excluded because a blurred desktop behind text defeats
        ///     the contrast guarantee the theme exists to make. Tabbed has no legacy equivalent
        ///     and downgrades to None.
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="requestedBackdrop">Either <see cref="BackdropType.Acrylic"/> or <see cref="BackdropType.Tabbed"/>.</param>
        /// <param name="capabilities">The OS capability snapshot.</param>
        /// <param name="isTransparencyEnabled">Whether the OS transparency-effects toggle is on.</param>
        /// <param name="resolvedTheme">The resolved application theme.</param>
        /// <returns>The effective <see cref="BackdropType"/> to apply.</returns>
        private static BackdropType ResolveTransparentBackdrop(
            BackdropType requestedBackdrop,
            WindowCapabilities capabilities,
            bool isTransparencyEnabled,
            ApplicationTheme resolvedTheme)
        {
            return capabilities.SupportsSystemBackdropType
                ? requestedBackdrop
                : capabilities.SupportsMicaEffect
                ? BackdropType.Mica
                : requestedBackdrop is BackdropType.Acrylic
                    && capabilities.SupportsLegacyAcrylic
                    && isTransparencyEnabled
                    && resolvedTheme is not ApplicationTheme.HighContrast
                ? BackdropType.Acrylic
                : BackdropType.None;
        }

        /// <summary>
        /// Builds the complete <see cref="BackdropPlan"/> for a window from the requested backdrop
        /// type, the current theme, and the OS capability snapshot.
        /// </summary>
        /// <remarks>
        /// The plan is computed in one pass:
        /// <list type="number">
        ///   <item>
        ///     <see cref="ResolveEffectiveBackdrop"/> downgrades the request for the current OS.
        ///   </item>
        ///   <item>
        ///     <c>None</c> effective backdrop gets a solid fallback background and
        ///     DWMWA_COLOR_DEFAULT for the caption. On 22H2+ the plan
        ///     emits DWMSBT_NONE to explicitly clear any previous Mica
        ///     or Acrylic; on Windows 10 no <c>DWMWA_SYSTEMBACKDROP_TYPE</c> write is attempted.
        ///   </item>
        ///   <item>
        ///     Mica on pre-22H2 Win11 uses the legacy <c>DWMWA_MICA_EFFECT</c> path
        ///     (<see cref="BackdropPlan.UseLegacyMicaEffect"/> = <see langword="true"/>), never
        ///     the canonical <c>DWMWA_SYSTEMBACKDROP_TYPE</c>.
        ///   </item>
        ///   <item>
        ///     Acrylic that survives on Windows 10 takes the legacy accent path
        ///     (<see cref="BackdropPlan.UseLegacyAcrylic"/> = <see langword="true"/>) and writes no
        ///     DWM attribute at all; the tint travels in the plan instead.
        ///   </item>
        ///   <item>
        ///     Any other active backdrop maps to a <c>DWMSBT_*</c> value via
        ///     <see cref="MapSystemBackdropType"/>.
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="requestedBackdrop">The <see cref="BackdropType"/> requested by the caller.</param>
        /// <param name="resolvedTheme">The resolved application theme (used for immersive dark mode).</param>
        /// <param name="capabilities">The OS capability snapshot.</param>
        /// <param name="fallbackBackgroundColor">
        ///   The opaque background color to use when no DWM backdrop is active.
        /// </param>
        /// <param name="isTransparencyEnabled">
        ///   <see langword="true"/> when the OS transparency-effects toggle is on. Gates the
        ///   Windows 10 legacy acrylic path only; every DWM backdrop path ignores it, because DWM
        ///   already honours the setting itself.
        /// </param>
        /// <param name="legacyAcrylicTintColor">
        ///   The tint color to record for the legacy acrylic accent policy, normally the
        ///   <c>AcrylicBackgroundFillColorDefault</c> theme token. Ignored on every path except the
        ///   Windows 10 legacy acrylic one.
        /// </param>
        /// <returns>A <see cref="BackdropPlan"/> describing all DWM writes to perform.</returns>
        internal static BackdropPlan BuildBackdropPlan(
            BackdropType requestedBackdrop,
            ApplicationTheme resolvedTheme,
            WindowCapabilities capabilities,
            Color fallbackBackgroundColor,
            bool isTransparencyEnabled,
            Color legacyAcrylicTintColor)
        {
            BackdropType effectiveBackdrop = ResolveEffectiveBackdrop(
                requestedBackdrop,
                capabilities,
                isTransparencyEnabled,
                resolvedTheme);
            bool isDark = resolvedTheme is ApplicationTheme.Dark;

            // None path: solid background, default caption color, explicit DWMSBT_NONE on 22H2+.
            if (effectiveBackdrop is BackdropType.None)
            {
                DWM_SYSTEMBACKDROP_TYPE? clearedSystemBackdrop = capabilities.SupportsSystemBackdropType
                    ? DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE
                    : null;

                return new BackdropPlan(
                    BackdropType.None,
                    useTransparentBackground: false,
                    fallbackBackgroundColor,
                    PInvoke.DWMWA_COLOR_DEFAULT,
                    clearedSystemBackdrop,
                    useLegacyMicaEffect: false,
                    isDark,
                    useLegacyAcrylic: false,
                    Colors.Transparent);
            }

            // Mica on pre-22H2 Win11: legacy DWMWA_MICA_EFFECT; no DWMWA_SYSTEMBACKDROP_TYPE.
            if (effectiveBackdrop is BackdropType.Mica
                && !capabilities.SupportsSystemBackdropType
                && capabilities.SupportsMicaEffect)
            {
                return new BackdropPlan(
                    BackdropType.Mica,
                    useTransparentBackground: true,
                    Colors.Transparent,
                    PInvoke.DWMWA_COLOR_NONE,
                    systemBackdropType: null,
                    useLegacyMicaEffect: true,
                    isDark,
                    useLegacyAcrylic: false,
                    Colors.Transparent);
            }

            // Acrylic on Windows 10: legacy SetWindowCompositionAttribute accent policy. Reaching
            // here means ResolveEffectiveBackdrop already confirmed the build supports the acrylic
            // accent state, transparency effects are on, and the theme is not high contrast, so the
            // only remaining discriminator is the absence of DWMWA_SYSTEMBACKDROP_TYPE. The caption
            // color is recorded with the same DWMWA_COLOR_NONE semantics as the DWM backdrops even
            // though Windows 10 never exposes DWMWA_CAPTION_COLOR for the caller to write.
            if (effectiveBackdrop is BackdropType.Acrylic && !capabilities.SupportsSystemBackdropType)
            {
                return new BackdropPlan(
                    BackdropType.Acrylic,
                    useTransparentBackground: true,
                    Colors.Transparent,
                    PInvoke.DWMWA_COLOR_NONE,
                    systemBackdropType: null,
                    useLegacyMicaEffect: false,
                    isDark,
                    useLegacyAcrylic: true,
                    legacyAcrylicTintColor);
            }

            // All other active backdrops on 22H2+: canonical DWMWA_SYSTEMBACKDROP_TYPE path.
            return new BackdropPlan(
                effectiveBackdrop,
                useTransparentBackground: true,
                Colors.Transparent,
                PInvoke.DWMWA_COLOR_NONE,
                MapSystemBackdropType(effectiveBackdrop),
                useLegacyMicaEffect: false,
                isDark,
                useLegacyAcrylic: false,
                Colors.Transparent);
        }

        /// <summary>
        /// Maps a <see cref="CornerPreference"/> value to the corresponding
        /// <c>DWMWCP_*</c> constant for <c>DWMWA_WINDOW_CORNER_PREFERENCE</c>.
        /// </summary>
        /// <remarks>
        /// <see cref="CornerPreference.Default"/> and <see cref="CornerPreference.Round"/> both
        /// map to DWMWCP_ROUND because <c>Default</c> in the
        /// Fluence library means "the library default," which is rounded on Windows 11.
        /// </remarks>
        /// <param name="preference">The requested corner style.</param>
        /// <returns>The <c>DWMWCP_*</c> constant to write via
        /// <c>DWMWA_WINDOW_CORNER_PREFERENCE</c>.</returns>
        internal static DWM_WINDOW_CORNER_PREFERENCE GetCornerPreference(CornerPreference preference)
        {
            return preference switch
            {
                CornerPreference.DoNotRound => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND,
                CornerPreference.RoundSmall => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL,
                CornerPreference.Default or CornerPreference.Round => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND,
                _ => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND,
            };
        }

        /// <summary>
        /// Maps an effective <see cref="BackdropType"/> to the <c>DWMSBT_*</c> constant for
        /// <c>DWMWA_SYSTEMBACKDROP_TYPE</c>. Only called when the OS supports that attribute
        /// (22H2+) and the effective backdrop is not <see cref="BackdropType.None"/>.
        /// </summary>
        /// <param name="backdropType">The effective backdrop type after capability resolution.</param>
        /// <returns>The <c>DWMSBT_*</c> constant for the system backdrop.</returns>
        private static DWM_SYSTEMBACKDROP_TYPE MapSystemBackdropType(BackdropType backdropType)
        {
            return backdropType switch
            {
                BackdropType.Acrylic => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW,
                BackdropType.Tabbed => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TABBEDWINDOW,
                BackdropType.Mica or BackdropType.Auto or BackdropType.None or _ =>
                    DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW,
            };
        }
    }
}
