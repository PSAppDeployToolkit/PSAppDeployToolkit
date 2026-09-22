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
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;
using Windows.Win32;
using Windows.Win32.Graphics.Dwm;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Pure-logic policy layer for <see cref="FluenceWindow"/>. All methods are stateless and
    /// side-effect-free so they can be unit tested without a window handle. The class maps the
    /// requested <see cref="WindowBackdropType"/> and OS capabilities to concrete DWM instructions and
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
        ///     Routes all title-bar hits through <c language="csharp">WM_NCHITTEST</c> so the custom caption can
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
        ///     <c language="csharp">DWMWA_WINDOW_CORNER_PREFERENCE</c> to avoid clipping the DWM backdrop.
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
        /// When a DWM backdrop is active (<see cref="WindowBackdropType.Mica"/>,
        /// <see cref="WindowBackdropType.Acrylic"/>, <see cref="WindowBackdropType.Tabbed"/>, or
        /// <see cref="WindowBackdropType.Auto"/>), the thickness is <c language="csharp">-1</c> so DWM extends the glass
        /// into the client area and the backdrop shows through. The same <c language="csharp">-1</c> is used when
        /// the caller requests a drop shadow without a backdrop, because the shadow is rendered
        /// via the DWM glass frame. When neither is active the thickness is a very-thin-but-nonzero
        /// value (<c language="csharp">0.00001</c>) so the resize border continues to hit-test while
        /// <see cref="WindowChrome"/>'s renderer does not paint a visible glass-frame artifact.
        /// This dual-path is intentional: setting <c language="csharp">-1</c> with
        /// <see cref="WindowBackdropType.None"/> on Windows 11 renders a visible glass artifact,
        /// so the tiny nonzero value is the correct prevention mechanism.
        /// <para>
        /// The input is the <em>requested</em> backdrop, not the effective one, so a Windows 10
        /// window that asked for a backdrop it cannot get still receives <c language="csharp">-1</c>. That is
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
        internal static Thickness GetGlassFrameThickness(WindowBackdropType backdrop, bool hasShadow)
        {
            return backdrop is not WindowBackdropType.None || hasShadow
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
        ///   <c language="csharp">Thickness(4)</c> when the window can be resized in a normal state;
        ///   <c language="csharp">Thickness(0)</c> when maximized, <see cref="ResizeMode.NoResize"/>, or
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
        /// The plan has two independent halves:
        /// <list type="bullet">
        ///   <item>
        ///     <term>WPF-template border</term>
        ///     <description>
        ///       Active window with accent borders enabled gets a border keyed to
        ///       <c language="xaml">SystemAccentColorBrush</c>. Inactive windows revert to
        ///       <c language="xaml">SurfaceStrokeColorDefaultBrush</c>. The thickness follows
        ///       <paramref name="capabilities"/> rather than activation: zero when
        ///       <see cref="WindowCapabilities.SupportsBorderColor"/> is <see langword="true"/> (Windows 11 owns the
        ///       outer border via DWM), one device-independent pixel otherwise (Windows 10, where the template
        ///       border is the only edge the window shows). The maximized 0-thick border is not decided here: it is
        ///       a template trigger on <c language="csharp">WindowState</c> in
        ///       <c language="xaml">Themes/Controls/FluenceWindow.xaml</c>, so this plan never sees window state.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>DWM border color</term>
        ///     <description>
        ///       When the OS supports <c language="csharp">DWMWA_BORDER_COLOR</c> and the window is active with
        ///       accent borders, the COLORREF derived from <paramref name="accentColor"/> is
        ///       emitted. Otherwise
        ///       DWMWA_COLOR_DEFAULT is used, which tells DWM to
        ///       restore its own border.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="isActive">
        ///   <see langword="true"/> when <see cref="FluenceWindow"/> is the foreground window.
        /// </param>
        /// <param name="isAccentBorderEnabled">
        ///   <see langword="true"/> when <c language="csharp">ApplicationAccentColorManager.IsAccentColorOnTitleBarsEnabled</c>
        ///   is set.
        /// </param>
        /// <param name="capabilities">The OS capability snapshot.</param>
        /// <param name="accentColor">The current system accent color.</param>
        /// <returns>A <see cref="FramePlan"/> describing the border to apply.</returns>
        internal static FramePlan BuildFramePlan(
            bool isActive,
            bool isAccentBorderEnabled,
            WindowCapabilities capabilities,
            Color accentColor)
        {
            string templateBorderBrushResourceKey = !isActive || !isAccentBorderEnabled
                ? "SurfaceStrokeColorDefaultBrush"
                : "SystemAccentColorBrush";

            Thickness templateBorderThickness = capabilities.SupportsBorderColor
                ? new Thickness(0)
                : new Thickness(1);

            uint dwmBorderColor = PInvoke.DWMWA_COLOR_DEFAULT;
            if (capabilities.SupportsBorderColor && isActive && isAccentBorderEnabled)
            {
                dwmBorderColor = NativeMethods.ColorToColorRef(accentColor);
            }

            return new FramePlan(templateBorderBrushResourceKey, templateBorderThickness, dwmBorderColor);
        }

        /// <summary>
        /// Resolves the <see cref="WindowBackdropType"/> that will actually be applied after
        /// downgrading for OS capability gaps.
        /// </summary>
        /// <remarks>
        /// Downgrade rules:
        /// <list type="bullet">
        ///   <item>
        ///     <term>HighContrast</term>
        ///     <description>
        ///       Suppresses every material outright and resolves straight to
        ///       <see cref="WindowBackdropType.None"/>, before any OS-capability branching runs. Per
        ///       Microsoft Learn "Materials in Windows apps": "High contrast mode: all materials are
        ///       suppressed; the system applies high-contrast theme colors instead," and the Mica
        ///       design page: "In High Contrast mode, users continue to see the familiar background
        ///       color of their choosing in place of Mica." WinUI's
        ///       <c language="csharp">SystemBackdropConfiguration.IsHighContrast</c> is documented as
        ///       true when "the system or application high-contrast theme" is applied, which covers
        ///       this library's in-app <see cref="ApplicationTheme.HighContrast"/> the same way it
        ///       covers the OS-wide setting.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Auto / Mica</term>
        ///     <description>
        ///       Resolves to <see cref="WindowBackdropType.Mica"/> when either
        ///       <c language="csharp">DWMWA_SYSTEMBACKDROP_TYPE</c> (22H2+) or the legacy
        ///       <c language="csharp">DWMWA_MICA_EFFECT</c> (21H2) is available; falls back to
        ///       <see cref="WindowBackdropType.None"/> on Windows 10.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Acrylic / Tabbed</term>
        ///     <description>
        ///       Passes through on 22H2+. Downgrades to Mica on pre-22H2 Win11 (only
        ///       <c language="csharp">DWMWA_MICA_EFFECT</c> is available there). On Windows 10, Acrylic survives as
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
        /// <param name="requestedBackdrop">The <see cref="WindowBackdropType"/> requested by the caller.</param>
        /// <param name="capabilities">The OS capability snapshot.</param>
        /// <param name="isTransparencyEnabled">
        ///   <see langword="true"/> when the OS transparency-effects toggle is on. Defaults to
        ///   <see langword="false"/>, which suppresses the Windows 10 legacy acrylic path: a caller
        ///   that does not read the setting must not get a blur the user has turned off.
        /// </param>
        /// <param name="resolvedTheme">
        ///   The resolved application theme. <see cref="ApplicationTheme.HighContrast"/> suppresses
        ///   every DWM material outright and short-circuits to <see cref="WindowBackdropType.None"/> before
        ///   any capability branching runs; every other value only affects the Windows 10 legacy
        ///   acrylic branch indirectly, by virtue of never triggering that short-circuit.
        /// </param>
        /// <returns>The effective <see cref="WindowBackdropType"/> to apply.</returns>
        internal static WindowBackdropType ResolveEffectiveBackdrop(
            WindowBackdropType requestedBackdrop,
            WindowCapabilities capabilities,
            bool isTransparencyEnabled = false,
            ApplicationTheme resolvedTheme = ApplicationTheme.Light)
        {
            // Microsoft Learn "Materials in Windows apps": "High contrast mode: all materials are
            // suppressed; the system applies high-contrast theme colors instead." The Mica design
            // page: "In High Contrast mode, users continue to see the familiar background color of
            // their choosing in place of Mica." WinUI's SystemBackdropConfiguration.IsHighContrast is
            // documented as true when "the system or application high-contrast theme" is applied,
            // which covers this library's in-app HighContrast theme, not only an OS-wide setting. So
            // this check runs first, ahead of any OS-capability downgrade, and covers Mica, Acrylic,
            // and Tabbed on every Windows version, not only the Windows 10 legacy acrylic path.
            return resolvedTheme is ApplicationTheme.HighContrast
                ? WindowBackdropType.None
                : requestedBackdrop switch
                {
                    WindowBackdropType.Auto or WindowBackdropType.Mica =>
                        capabilities.SupportsSystemBackdropType || capabilities.SupportsMicaEffect
                            ? WindowBackdropType.Mica
                            : WindowBackdropType.None,

                    WindowBackdropType.Acrylic or WindowBackdropType.Tabbed =>
                        ResolveTransparentBackdrop(requestedBackdrop, capabilities, isTransparencyEnabled),

                    WindowBackdropType.None or _ => requestedBackdrop,
                };
        }

        /// <summary>
        /// Resolves <see cref="WindowBackdropType.Acrylic"/> and <see cref="WindowBackdropType.Tabbed"/> against
        /// the three mutually exclusive transparency mechanisms, newest first.
        /// </summary>
        /// <remarks>
        /// <list type="number">
        ///   <item>
        ///     <c language="csharp">DWMWA_SYSTEMBACKDROP_TYPE</c> (22H2+) expresses both requests natively, so the
        ///     request passes through untouched.
        ///   </item>
        ///   <item>
        ///     <c language="csharp">DWMWA_MICA_EFFECT</c> (21H2) expresses neither, so both downgrade to the Mica it
        ///     does express, which is closer to the request than an opaque window.
        ///   </item>
        ///   <item>
        ///     Windows 10 has neither attribute but does have the legacy accent policy, which
        ///     expresses acrylic and nothing else. Acrylic therefore survives when the build
        ///     supports it and the user has transparency effects on. Tabbed has no legacy equivalent
        ///     and downgrades to None. The caller (<see cref="ResolveEffectiveBackdrop"/>) already
        ///     returns <see cref="WindowBackdropType.None"/> before reaching this method whenever the
        ///     resolved theme is <see cref="ApplicationTheme.HighContrast"/>, so this method never
        ///     needs to consult the theme itself.
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="requestedBackdrop">Either <see cref="WindowBackdropType.Acrylic"/> or <see cref="WindowBackdropType.Tabbed"/>.</param>
        /// <param name="capabilities">The OS capability snapshot.</param>
        /// <param name="isTransparencyEnabled">Whether the OS transparency-effects toggle is on.</param>
        /// <returns>The effective <see cref="WindowBackdropType"/> to apply.</returns>
        private static WindowBackdropType ResolveTransparentBackdrop(
            WindowBackdropType requestedBackdrop,
            WindowCapabilities capabilities,
            bool isTransparencyEnabled)
        {
            return capabilities.SupportsSystemBackdropType
                ? requestedBackdrop
                : capabilities.SupportsMicaEffect
                ? WindowBackdropType.Mica
                : requestedBackdrop is WindowBackdropType.Acrylic
                    && capabilities.SupportsLegacyAcrylic
                    && isTransparencyEnabled
                ? WindowBackdropType.Acrylic
                : WindowBackdropType.None;
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
        ///     <c language="csharp">None</c> effective backdrop gets a solid fallback background and
        ///     DWMWA_COLOR_DEFAULT for the caption. On 22H2+ the plan
        ///     emits DWMSBT_NONE to explicitly clear any previous Mica
        ///     or Acrylic; on Windows 10 no <c language="csharp">DWMWA_SYSTEMBACKDROP_TYPE</c> write is attempted.
        ///   </item>
        ///   <item>
        ///     Mica on pre-22H2 Win11 uses the legacy <c language="csharp">DWMWA_MICA_EFFECT</c> path
        ///     (<see cref="BackdropPlan.UseLegacyMicaEffect"/> = <see langword="true"/>), never
        ///     the canonical <c language="csharp">DWMWA_SYSTEMBACKDROP_TYPE</c>.
        ///   </item>
        ///   <item>
        ///     Acrylic that survives on Windows 10 takes the legacy accent path
        ///     (<see cref="BackdropPlan.UseLegacyAcrylic"/> = <see langword="true"/>) and writes no
        ///     DWM attribute at all; the tint travels in the plan instead.
        ///   </item>
        ///   <item>
        ///     Any other active backdrop maps to a <c language="csharp">DWMSBT_*</c> value via
        ///     <see cref="MapSystemBackdropType"/>.
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="requestedBackdrop">The <see cref="WindowBackdropType"/> requested by the caller.</param>
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
        ///   The tint color to record for the legacy acrylic accent policy: the
        ///   <c language="xaml">AcrylicBackgroundFillColorDefault</c> theme token's RGB with its alpha
        ///   replaced by a forced <c language="csharp">0xF0</c> (see
        ///   <see cref="FluenceWindow.GetLegacyAcrylicTintColor"/>), because the token itself is opaque.
        ///   Ignored on every path except the Windows 10 legacy acrylic one.
        /// </param>
        /// <returns>A <see cref="BackdropPlan"/> describing all DWM writes to perform.</returns>
        internal static BackdropPlan BuildBackdropPlan(
            WindowBackdropType requestedBackdrop,
            ApplicationTheme resolvedTheme,
            WindowCapabilities capabilities,
            Color fallbackBackgroundColor,
            bool isTransparencyEnabled,
            Color legacyAcrylicTintColor)
        {
            WindowBackdropType effectiveBackdrop = ResolveEffectiveBackdrop(
                requestedBackdrop,
                capabilities,
                isTransparencyEnabled,
                resolvedTheme);
            bool isDark = resolvedTheme is ApplicationTheme.Dark;

            // None path: solid background, default caption color, explicit DWMSBT_NONE on 22H2+.
            if (effectiveBackdrop is WindowBackdropType.None)
            {
                DWM_SYSTEMBACKDROP_TYPE? clearedSystemBackdrop = capabilities.SupportsSystemBackdropType
                    ? DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE
                    : null;

                return new BackdropPlan(
                    WindowBackdropType.None,
                    fallbackBackgroundColor,
                    PInvoke.DWMWA_COLOR_DEFAULT,
                    clearedSystemBackdrop,
                    useLegacyMicaEffect: false,
                    isDark,
                    useLegacyAcrylic: false,
                    Colors.Transparent);
            }

            // Mica on pre-22H2 Win11: legacy DWMWA_MICA_EFFECT; no DWMWA_SYSTEMBACKDROP_TYPE.
            if (effectiveBackdrop is WindowBackdropType.Mica
                && !capabilities.SupportsSystemBackdropType
                && capabilities.SupportsMicaEffect)
            {
                return new BackdropPlan(
                    WindowBackdropType.Mica,
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
            if (effectiveBackdrop is WindowBackdropType.Acrylic && !capabilities.SupportsSystemBackdropType)
            {
                return new BackdropPlan(
                    WindowBackdropType.Acrylic,
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
                Colors.Transparent,
                PInvoke.DWMWA_COLOR_NONE,
                MapSystemBackdropType(effectiveBackdrop),
                useLegacyMicaEffect: false,
                isDark,
                useLegacyAcrylic: false,
                Colors.Transparent);
        }

        /// <summary>
        /// Maps a <see cref="WindowCornerPreference"/> value to the corresponding
        /// <c language="csharp">DWMWCP_*</c> constant for <c language="csharp">DWMWA_WINDOW_CORNER_PREFERENCE</c>.
        /// </summary>
        /// <remarks>
        /// <see cref="WindowCornerPreference.Default"/> and <see cref="WindowCornerPreference.Round"/> both
        /// map to DWMWCP_ROUND because <c language="csharp">Default</c> in the
        /// Fluence library means "the library default," which is rounded on Windows 11.
        /// </remarks>
        /// <param name="preference">The requested corner style.</param>
        /// <returns>The <c language="csharp">DWMWCP_*</c> constant to write via
        /// <c language="csharp">DWMWA_WINDOW_CORNER_PREFERENCE</c>.</returns>
        internal static DWM_WINDOW_CORNER_PREFERENCE GetCornerPreference(WindowCornerPreference preference)
        {
            return preference switch
            {
                WindowCornerPreference.DoNotRound => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND,
                WindowCornerPreference.RoundSmall => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL,
                WindowCornerPreference.Default or WindowCornerPreference.Round => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND,
                _ => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND,
            };
        }

        /// <summary>
        /// The effective backdrops for which <see cref="ShouldApplyContentLayerPreBlend"/> applies
        /// the opaque pre-blend. Only <see cref="WindowBackdropType.Mica"/> was measured quantising client
        /// alpha under <c language="csharp">DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO</c> flags <c language="text">0x4</c> (10 bpc,
        /// advanced color off); see the KNOWN_ISSUES.md entry "Translucent layers over a DWM
        /// backdrop lose alpha precision on a 10 bpc display". <see cref="WindowBackdropType.Tabbed"/> is
        /// included by inference, not measurement: it is the same DWM material family
        /// (<c language="csharp">DWMSBT_MAINWINDOW</c> and <c language="csharp">DWMSBT_TABBEDWINDOW</c>) as Mica, but it was
        /// measured only under flags <c language="text">0x7</c> (advanced color on), where it read full precision
        /// like everything else. <see cref="WindowBackdropType.Acrylic"/> is excluded: it too was measured
        /// only under <c language="text">0x7</c> and read full precision there, and was never measured under
        /// <c language="text">0x4</c>, so there is no basis yet to include or exclude it on the quantising flag.
        /// Kept as a single array so an Acrylic measurement under <c language="text">0x4</c> can extend (or leave
        /// unchanged) the set with a one-line change.
        /// </summary>
        private static readonly WindowBackdropType[] PreBlendEligibleBackdrops = [WindowBackdropType.Mica, WindowBackdropType.Tabbed];

        /// <summary>
        /// Returns whether <see cref="ResolveContentLayerPreBlend"/> should substitute an opaque
        /// content-layer color for the given effective backdrop, theme, and display color depth.
        /// </summary>
        /// <remarks>
        /// Measured fact (KNOWN_ISSUES.md, "Translucent layers over a DWM backdrop lose alpha
        /// precision on a 10 bpc display"): on a display path reporting
        /// <c language="csharp">bitsPerColorChannel = 10</c> with advanced color <em>disabled</em>, DWM composites a
        /// window's client alpha over a system backdrop with 2-bit alpha, so a translucent
        /// content-layer token reads darker than it specifies. The same 10 bpc path with advanced
        /// color <em>enabled</em> (measured by toggling the GPU driver's 10-bit pixel format
        /// setting) was measured to composite at full precision, so the pre-blend must not apply
        /// there. This method is pure: it takes the already-resolved backdrop, theme, and display
        /// state and returns a value, so it can be unit tested without a window handle, a live
        /// display, or DWM.
        /// </remarks>
        /// <param name="effectiveBackdrop">The backdrop that will actually be applied after capability downgrade (see <see cref="ResolveEffectiveBackdrop"/>).</param>
        /// <param name="resolvedTheme">The resolved application theme.</param>
        /// <param name="colorDepth">The display color depth for the monitor hosting the window.</param>
        /// <returns>
        ///   <see langword="false"/> when <paramref name="colorDepth"/> reports 8 bpc or fewer
        ///   (including unknown), advanced color is enabled, the theme is
        ///   <see cref="ApplicationTheme.HighContrast"/>, or <paramref name="effectiveBackdrop"/> is
        ///   not one of <see cref="PreBlendEligibleBackdrops"/>; otherwise <see langword="true"/>.
        /// </returns>
        internal static bool ShouldApplyContentLayerPreBlend(
            WindowBackdropType effectiveBackdrop,
            ApplicationTheme resolvedTheme,
            DisplayColorDepth colorDepth)
        {
            return colorDepth.BitsPerColorChannel > 8
                && !colorDepth.AdvancedColorEnabled
                && resolvedTheme is not ApplicationTheme.HighContrast
                && Array.IndexOf(PreBlendEligibleBackdrops, effectiveBackdrop) >= 0;
        }

        /// <summary>
        /// Resolves the opaque, pre-blended replacement for a translucent content-layer token when
        /// the active display path cannot composite its alpha at full precision.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Per AGENTS.md 4.2, WinUI 3 CommonStyles is the authority for visual tokens, and WinUI
        /// already ships the pre-blended value this substitution needs:
        /// <c language="text">LayerOnMicaBaseAltFillColorTertiary</c> (<c language="text">#FFF9F9F9</c> Light,
        /// <c language="text">#FF2C2C2C</c> Dark). When <paramref name="canonicalPreBlend"/> is supplied (the caller
        /// resolved that key), it is returned as-is. The straight-alpha composite of
        /// <paramref name="layerFill"/> over <paramref name="solidBase"/> is only a fallback for a
        /// consumer whose theme dictionary does not define that key, computed per channel with the
        /// alpha forced to <c language="csharp">0xFF</c>
        /// (<c language="csharp">round(layerFill.A/255 * layerFill.channel + (1 - layerFill.A/255) * solidBase.channel)</c>,
        /// <see cref="MidpointRounding.AwayFromZero"/>); for the canonical Light tokens
        /// (<c language="text">#80FFFFFF</c> over <c language="text">#FFF3F3F3</c>) it reproduces the same
        /// <c language="text">#FFF9F9F9</c> the canonical key already carries.
        /// </para>
        /// <para>
        /// The caller applies the result as the bottom-most layer of the affected control (see
        /// <see cref="FluenceWindow.ApplyBackdrop"/>): a pre-blended opaque plate restores
        /// the brightness the token specifies, and everything WPF composites on top of it is then
        /// blended by WPF itself at full precision, unaffected by the DWM quantisation.
        /// </para>
        /// </remarks>
        /// <param name="effectiveBackdrop">The backdrop that will actually be applied after capability downgrade (see <see cref="ResolveEffectiveBackdrop"/>).</param>
        /// <param name="resolvedTheme">The resolved application theme.</param>
        /// <param name="colorDepth">The display color depth for the monitor hosting the window.</param>
        /// <param name="canonicalPreBlend">The resolved <c language="text">LayerOnMicaBaseAltFillColorTertiary</c> token, or <see langword="null"/> when the theme dictionary does not define it.</param>
        /// <param name="layerFill">The translucent content-layer token color (for example <c language="xaml">NavigationViewContentBackground</c>), used only for the fallback composite.</param>
        /// <param name="solidBase">The opaque window-base token color the layer is composited over (for example <c language="xaml">SolidBackgroundFillColorBase</c>), used only for the fallback composite.</param>
        /// <returns>
        ///   The opaque pre-blended color to substitute, or <see langword="null"/> when
        ///   <see cref="ShouldApplyContentLayerPreBlend"/> reports no substitution is needed.
        /// </returns>
        internal static Color? ResolveContentLayerPreBlend(
            WindowBackdropType effectiveBackdrop,
            ApplicationTheme resolvedTheme,
            DisplayColorDepth colorDepth,
            Color? canonicalPreBlend,
            Color layerFill,
            Color solidBase)
        {
            if (!ShouldApplyContentLayerPreBlend(effectiveBackdrop, resolvedTheme, colorDepth))
            {
                return null;
            }
            if (canonicalPreBlend is Color canonical)
            {
                return canonical;
            }

            double alpha = layerFill.A / 255.0;
            byte r = ComposeChannel(alpha, layerFill.R, solidBase.R);
            byte g = ComposeChannel(alpha, layerFill.G, solidBase.G);
            byte b = ComposeChannel(alpha, layerFill.B, solidBase.B);
            return Color.FromArgb(0xFF, r, g, b);
        }

        /// <summary>
        /// Straight-alpha composites one 8-bit color channel of <see cref="ResolveContentLayerPreBlend"/>,
        /// rounding away from zero.
        /// </summary>
        /// <param name="alpha">The foreground alpha in the <c language="csharp">[0, 1]</c> range.</param>
        /// <param name="foreground">The foreground channel value.</param>
        /// <param name="background">The background channel value.</param>
        /// <returns>The composited channel value.</returns>
        private static byte ComposeChannel(double alpha, byte foreground, byte background)
        {
            double composited = (alpha * foreground) + ((1.0 - alpha) * background);
            return (byte)Math.Round(composited, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Maps an effective <see cref="WindowBackdropType"/> to the <c language="csharp">DWMSBT_*</c> constant for
        /// <c language="csharp">DWMWA_SYSTEMBACKDROP_TYPE</c>. Only called when the OS supports that attribute
        /// (22H2+) and the effective backdrop is not <see cref="WindowBackdropType.None"/>.
        /// </summary>
        /// <param name="backdropType">The effective backdrop type after capability resolution.</param>
        /// <returns>The <c language="csharp">DWMSBT_*</c> constant for the system backdrop.</returns>
        private static DWM_SYSTEMBACKDROP_TYPE MapSystemBackdropType(WindowBackdropType backdropType)
        {
            return backdropType switch
            {
                WindowBackdropType.Acrylic => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW,
                WindowBackdropType.Tabbed => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TABBEDWINDOW,
                WindowBackdropType.Mica or WindowBackdropType.Auto or WindowBackdropType.None or _ =>
                    DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW,
            };
        }
    }
}
