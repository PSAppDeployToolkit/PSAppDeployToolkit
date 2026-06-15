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

using Fluence.Wpf.Controls;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;

namespace Fluence.Wpf.Tests
{
    // WI-2 S2.6 regression floor for WindowPolicy (internal, visible via
    // [InternalsVisibleTo("Fluence.Wpf.Tests")]). WindowPolicy is pure logic: it maps
    // the requested BackdropType + OS capabilities to an effective backdrop, a DWM plan,
    // and a template frame plan. These tests pin those mappings so a future OS-caps
    // refactor cannot silently regress the downgrade behaviour PSADT relies on for
    // Windows 10 1809+ baseline support.
    [TestClass]
    public class WindowPolicyTests
    {
        private static WindowCapabilities Caps(
            bool systemBackdrop = false,
            bool legacyMica = false,
            bool roundedCorners = false,
            bool captionColor = false,
            bool borderColor = false)
        {
            return new WindowCapabilities(
                systemBackdrop,
                legacyMica,
                roundedCorners,
                captionColor,
                borderColor);
        }

        #region ResolveEffectiveBackdrop - capability matrix

        [TestMethod]
        public void ResolveEffectiveBackdrop_Auto_Win11_22H2_ReturnsMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Auto,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true));

            Assert.AreEqual(BackdropType.Mica, effective,
                "Auto on a 22H2+ build with system backdrop support must pick Mica.");
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Auto_Win11Pre22H2_LegacyMicaOnly_ReturnsMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Auto,
                Caps(legacyMica: true, roundedCorners: true, captionColor: true));

            Assert.AreEqual(BackdropType.Mica, effective,
                "Auto on pre-22H2 Win11 (no DWMWA_SYSTEMBACKDROP_TYPE, legacy DWMWA_MICA_EFFECT available) must still land on Mica via the legacy path.");
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Auto_Win10_ReturnsNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Auto,
                Caps());

            Assert.AreEqual(BackdropType.None, effective,
                "Auto on Windows 10 (no Mica, no system backdrop) must downgrade to None.");
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_None_Win11_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.None,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.AreEqual(BackdropType.None, effective,
                "Explicit None must never be upgraded, even when the OS supports richer backdrops.");
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Mica_Win22H2_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Mica,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.AreEqual(BackdropType.Mica, effective);
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Mica_Win11Pre22H2_UsesLegacyMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Mica,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.AreEqual(BackdropType.Mica, effective,
                "Explicit Mica on pre-22H2 Win11 must still resolve to Mica via the legacy DWMWA_MICA_EFFECT path.");
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Mica_Win10_DowngradesToNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Mica,
                Caps());

            Assert.AreEqual(BackdropType.None, effective,
                "Explicit Mica on Windows 10 must downgrade to None - we never emit a DWM call the OS cannot satisfy.");
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Acrylic_Win22H2_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Acrylic,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.AreEqual(BackdropType.Acrylic, effective);
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Acrylic_Win11Pre22H2_DowngradesToMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Acrylic,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.AreEqual(BackdropType.Mica, effective,
                "Acrylic on pre-22H2 has no DWMSBT_TRANSIENTWINDOW; must prefer legacy Mica over None.");
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Acrylic_Win10_DowngradesToNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Acrylic,
                Caps());

            Assert.AreEqual(BackdropType.None, effective);
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Tabbed_Win22H2_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Tabbed,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.AreEqual(BackdropType.Tabbed, effective);
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Tabbed_Win11Pre22H2_DowngradesToMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Tabbed,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.AreEqual(BackdropType.Mica, effective);
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_Tabbed_Win10_DowngradesToNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Tabbed,
                Caps());

            Assert.AreEqual(BackdropType.None, effective);
        }

        #endregion ResolveEffectiveBackdrop - capability matrix

        #region BuildBackdropPlan - None

        [TestMethod]
        public void BuildBackdropPlan_None_UsesFallbackBackground_EmitsDwmsbtNone()
        {
            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true),
                fallback);

            Assert.AreEqual(BackdropType.None, plan.EffectiveBackdrop);
            Assert.IsFalse(plan.UseTransparentBackground,
                "None must paint a solid background - transparency would reveal the glass frame.");
            Assert.AreEqual(fallback, plan.BackgroundColor);
            Assert.AreEqual(NativeConstants.DWMWA_COLOR_DEFAULT, plan.CaptionColor,
                "None must leave the DWM caption color at its default (system-managed).");
            Assert.IsTrue(plan.SystemBackdropType.HasValue,
                "On 22H2 DWM exposes DWMWA_SYSTEMBACKDROP_TYPE - None must emit DWMSBT_NONE to explicitly clear Mica/Acrylic.");
            Assert.AreEqual(NativeConstants.DWMSBT_NONE, plan.SystemBackdropType.Value);
            Assert.IsFalse(plan.UseLegacyMicaEffect);
        }

        [TestMethod]
        public void BuildBackdropPlan_None_OnWin10_OmitsSystemBackdropType()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Light,
                Caps(),
                Color.FromRgb(0xFA, 0xFA, 0xFA));

            Assert.IsFalse(plan.SystemBackdropType.HasValue,
                "Windows 10 does not expose DWMWA_SYSTEMBACKDROP_TYPE - the plan must not attempt to set it.");
        }

        #endregion BuildBackdropPlan - None

        #region BuildBackdropPlan - Mica (legacy path on pre-22H2)

        [TestMethod]
        public void BuildBackdropPlan_Mica_LegacyPath_UsesDwmMicaEffect_NotSystemBackdrop()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Mica,
                ApplicationTheme.Dark,
                Caps(legacyMica: true, roundedCorners: true),
                Colors.White);

            Assert.AreEqual(BackdropType.Mica, plan.EffectiveBackdrop);
            Assert.IsTrue(plan.UseTransparentBackground,
                "Mica requires a transparent window client so DWM can composite the backdrop.");
            Assert.AreEqual(Colors.Transparent, plan.BackgroundColor);
            Assert.AreEqual(NativeConstants.DWMWA_COLOR_NONE, plan.CaptionColor,
                "Mica must force DWMWA_COLOR_NONE on the caption so the system backdrop shows through.");
            Assert.IsFalse(plan.SystemBackdropType.HasValue,
                "Pre-22H2 must not emit DWMWA_SYSTEMBACKDROP_TYPE - only DWMWA_MICA_EFFECT is legal there.");
            Assert.IsTrue(plan.UseLegacyMicaEffect,
                "Pre-22H2 Win11 must set the legacy DWMWA_MICA_EFFECT attribute.");
        }

        [TestMethod]
        public void BuildBackdropPlan_Mica_Win22H2_UsesDwmSystemBackdropType_NotLegacy()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Mica,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true),
                Colors.White);

            Assert.AreEqual(BackdropType.Mica, plan.EffectiveBackdrop);
            Assert.IsTrue(plan.UseTransparentBackground);
            Assert.IsTrue(plan.SystemBackdropType.HasValue);
            Assert.AreEqual(NativeConstants.DWMSBT_MAINWINDOW, plan.SystemBackdropType.Value,
                "22H2 Mica must emit DWMSBT_MAINWINDOW via DWMWA_SYSTEMBACKDROP_TYPE.");
            Assert.IsFalse(plan.UseLegacyMicaEffect,
                "22H2 must use the canonical DWMWA_SYSTEMBACKDROP_TYPE path, not the legacy Mica attribute.");
        }

        #endregion BuildBackdropPlan - Mica (legacy path on pre-22H2)

        #region BuildBackdropPlan - Acrylic + Tabbed (SystemBackdropType mapping)

        [TestMethod]
        public void BuildBackdropPlan_Acrylic_Win22H2_MapsToTransientWindow()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Acrylic,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true),
                Colors.White);

            Assert.AreEqual(BackdropType.Acrylic, plan.EffectiveBackdrop);
            Assert.AreEqual(NativeConstants.DWMSBT_TRANSIENTWINDOW, plan.SystemBackdropType,
                "Acrylic must map to DWMSBT_TRANSIENTWINDOW.");
        }

        [TestMethod]
        public void BuildBackdropPlan_Tabbed_Win22H2_MapsToTabbedWindow()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Tabbed,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true),
                Colors.White);

            Assert.AreEqual(BackdropType.Tabbed, plan.EffectiveBackdrop);
            Assert.AreEqual(NativeConstants.DWMSBT_TABBEDWINDOW, plan.SystemBackdropType,
                "Tabbed must map to DWMSBT_TABBEDWINDOW.");
        }

        #endregion BuildBackdropPlan - Acrylic + Tabbed (SystemBackdropType mapping)

        #region BuildBackdropPlan - Immersive dark flag

        [TestMethod]
        public void BuildBackdropPlan_DarkTheme_SetsImmersiveDarkMode()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Dark,
                Caps(systemBackdrop: true),
                Color.FromRgb(0x20, 0x20, 0x20));

            Assert.IsTrue(plan.UseImmersiveDarkMode,
                "Dark theme must set DWMWA_USE_IMMERSIVE_DARK_MODE so the native caption renders dark.");
        }

        [TestMethod]
        public void BuildBackdropPlan_LightTheme_DoesNotSetImmersiveDarkMode()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true),
                Color.FromRgb(0xFA, 0xFA, 0xFA));

            Assert.IsFalse(plan.UseImmersiveDarkMode);
        }

        #endregion BuildBackdropPlan - Immersive dark flag

        #region GetCornerPreference - enum → DWMWCP_* mapping

        [TestMethod]
        public void GetCornerPreference_Round_MapsToDwmwcpRound()
        {
            Assert.AreEqual(NativeConstants.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.Round));
        }

        [TestMethod]
        public void GetCornerPreference_Default_MapsToDwmwcpRound()
        {
            // FluenceWindow exposes CornerPreference.Default as "library default" - which in a
            // Fluent library means rounded on Win11. The policy normalises Default to Round.
            Assert.AreEqual(NativeConstants.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.Default));
        }

        [TestMethod]
        public void GetCornerPreference_DoNotRound_MapsToDwmwcpDoNotRound()
        {
            Assert.AreEqual(NativeConstants.DWMWCP_DONOTROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.DoNotRound));
        }

        [TestMethod]
        public void GetCornerPreference_RoundSmall_MapsToDwmwcpRoundSmall()
        {
            Assert.AreEqual(NativeConstants.DWMWCP_ROUNDSMALL,
                WindowPolicy.GetCornerPreference(CornerPreference.RoundSmall));
        }

        #endregion GetCornerPreference - enum → DWMWCP_* mapping

        #region CreateWindowChrome - canonical FluenceWindow chrome contract

        [TestMethod]
        public void CreateWindowChrome_CaptionHeight_IsZero()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.AreEqual(0d, chrome.CaptionHeight,
                "CaptionHeight is hard-coded to 0 so all title-bar hits route through WM_NCHITTEST.");
        }

        [TestMethod]
        public void CreateWindowChrome_GlassFrameThickness_IsMinusOneForShadow()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.AreEqual(new Thickness(-1), chrome.GlassFrameThickness,
                "GlassFrameThickness = -1 opts into the full DWM glass frame, which is what gives Fluence windows their drop shadow.");
        }

        [TestMethod]
        public void CreateWindowChrome_ResizeBorderThickness_Is4()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.AreEqual(new Thickness(4), chrome.ResizeBorderThickness,
                "4dp resize border matches WinUI 3 / .NET 10 WPF FluentWindow - preserves the invisible hit-margin that lets users grab the edge.");
        }

        [TestMethod]
        public void CreateWindowChrome_DisablesAeroCaptionButtons()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.IsFalse(chrome.UseAeroCaptionButtons,
                "Fluence renders its own caption buttons; the native WPF Aero caption must stay off.");
        }

        [TestMethod]
        public void CreateWindowChrome_NonClientFrameEdges_IsNone()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.AreEqual(NonClientFrameEdges.None, chrome.NonClientFrameEdges,
                "NonClientFrameEdges.None is required so the client area extends into the caption strip and the custom caption paints.");
        }

        [TestMethod]
        public void CreateWindowChrome_CornerRadius_IsZero()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.AreEqual(new CornerRadius(0), chrome.CornerRadius,
                "WindowChrome.CornerRadius must be 0 - rounded corners are driven by DWMWA_WINDOW_CORNER_PREFERENCE, not the WPF chrome (WPF-side rounding would clip DWM's Mica).");
        }

        #endregion CreateWindowChrome - canonical FluenceWindow chrome contract

        #region GetResizeBorderThickness - maximised / non-resize matrix

        [TestMethod]
        public void GetResizeBorderThickness_Normal_CanResize_Returns4()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.CanResize);
            Assert.AreEqual(new Thickness(4), thickness);
        }

        [TestMethod]
        public void GetResizeBorderThickness_Normal_CanResizeWithGrip_Returns4()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.CanResizeWithGrip);
            Assert.AreEqual(new Thickness(4), thickness);
        }

        [TestMethod]
        public void GetResizeBorderThickness_Maximized_ReturnsZero()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Maximized, ResizeMode.CanResize);
            Assert.AreEqual(new Thickness(0), thickness,
                "A maximised window has no resize handles - 4dp would bleed over the taskbar.");
        }

        [TestMethod]
        public void GetResizeBorderThickness_NoResize_ReturnsZero()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.NoResize);
            Assert.AreEqual(new Thickness(0), thickness,
                "ResizeMode.NoResize must emit a zero-thickness hit margin - PSADT fluent dialogs rely on this.");
        }

        [TestMethod]
        public void GetResizeBorderThickness_CanMinimize_ReturnsZero()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.CanMinimize);
            Assert.AreEqual(new Thickness(0), thickness,
                "ResizeMode.CanMinimize forbids drag-resize; the hit margin must be zero.");
        }

        #endregion GetResizeBorderThickness - maximised / non-resize matrix

        #region BuildFramePlan - accent border selection

        [TestMethod]
        public void BuildFramePlan_Normal_ActiveWithAccentBorder_UsesAccentKey()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Color.FromRgb(0x00, 0x78, 0xD4));

            Assert.AreEqual(new Thickness(2), plan.TemplateBorderThickness);
            Assert.AreEqual("SystemAccentColorBrush", plan.TemplateBorderBrushResourceKey,
                "Active window with accent border enabled must bind to SystemAccentColorBrush via the template.");
            Assert.AreNotEqual(NativeConstants.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor,
                "When the OS supports DWMWA_BORDER_COLOR and the window is active, we should emit an ABGR value, not the default sentinel.");
        }

        [TestMethod]
        public void BuildFramePlan_Normal_Inactive_UsesCardStrokeKey()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: false,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Colors.Red);

            Assert.AreEqual("CardStrokeColorDefaultSolidBrush", plan.TemplateBorderBrushResourceKey,
                "Inactive windows must revert to CardStrokeColorDefaultSolidBrush - the accent border is an activation cue.");
        }

        [TestMethod]
        public void BuildFramePlan_Maximized_TemplateBorderIsZero()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Maximized,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Colors.Red);

            Assert.AreEqual(new Thickness(0), plan.TemplateBorderThickness,
                "A maximised window must not paint a 2dp template border - it would clip against the taskbar / monitor edge.");
        }

        [TestMethod]
        public void BuildFramePlan_NoBorderColorCapability_KeepsDwmDefault()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(),
                accentColor: Colors.Red);

            Assert.AreEqual(NativeConstants.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor,
                "Windows 10 does not expose DWMWA_BORDER_COLOR - the plan must keep the default sentinel rather than push an unsupported value.");
        }

        #endregion BuildFramePlan - accent border selection

        #region WindowCapabilities.Current - sanity

        [TestMethod]
        public void WindowCapabilities_Current_NotNull()
        {
            WindowCapabilities caps = WindowCapabilities.Current;
            Assert.IsNotNull(caps, "WindowCapabilities.Current must always resolve - it shields callers from OS-version probing.");
        }

        #endregion WindowCapabilities.Current - sanity

        #region GetGlassFrameThickness - dual-path

        // WPF-UI's GlassFrameThickness convention: -1 for full DWM glass extension when a
        // backdrop is active, 0.00001 for an invisible-but-resize-borderable frame when no
        // backdrop is active and no shadow is requested. The combined check makes sure we
        // don't render a visible glass-frame artifact when SystemBackdropType=None on Win11.

        [TestMethod]
        public void GetGlassFrameThickness_NoBackdrop_NoShadow_VeryThin()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.None, hasShadow: false);
            Assert.AreEqual(0.00001, t.Left, 1e-9, "No backdrop + no shadow must use the thin-but-nonzero glass frame.");
            Assert.AreEqual(0.00001, t.Top, 1e-9);
            Assert.AreEqual(0.00001, t.Right, 1e-9);
            Assert.AreEqual(0.00001, t.Bottom, 1e-9);
        }

        [TestMethod]
        public void GetGlassFrameThickness_NoBackdrop_WithShadow_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.None, hasShadow: true);
            Assert.AreEqual(-1, t.Left, 1e-9, "Shadow requested without backdrop still extends the DWM glass frame.");
        }

        [TestMethod]
        public void GetGlassFrameThickness_MicaBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Mica, hasShadow: false);
            Assert.AreEqual(-1, t.Left, 1e-9, "Mica backdrop must extend the glass into the client area.");
        }

        [TestMethod]
        public void GetGlassFrameThickness_AcrylicBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Acrylic, hasShadow: false);
            Assert.AreEqual(-1, t.Left, 1e-9, "Acrylic backdrop must extend the glass into the client area.");
        }

        [TestMethod]
        public void GetGlassFrameThickness_TabbedBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Tabbed, hasShadow: false);
            Assert.AreEqual(-1, t.Left, 1e-9, "Tabbed backdrop must extend the glass into the client area.");
        }

        [TestMethod]
        public void GetGlassFrameThickness_AutoBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Auto, hasShadow: false);
            Assert.AreEqual(-1, t.Left, 1e-9, "Auto backdrop is treated as backdrop-active for chrome purposes.");
        }

        #endregion GetGlassFrameThickness - dual-path
    }
}
