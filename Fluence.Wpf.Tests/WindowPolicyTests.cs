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
using Fluence.Wpf.Controls;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;
using Xunit;

namespace Fluence.Wpf.Tests
{
    // WI-2 S2.6 regression floor for WindowPolicy (internal, visible via
    // [InternalsVisibleTo("Fluence.Wpf.Tests")]). WindowPolicy is pure logic: it maps
    // the requested BackdropType + OS capabilities to an effective backdrop, a DWM plan,
    // and a template frame plan. These tests pin those mappings so a future OS-caps
    // refactor cannot silently regress the downgrade behaviour PSADT relies on for
    // Windows 10 1809+ baseline support.
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

        [Fact]
        public void ResolveEffectiveBackdrop_Auto_Win11_22H2_ReturnsMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Auto,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true));

            Assert.Equal(BackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Auto_Win11Pre22H2_LegacyMicaOnly_ReturnsMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Auto,
                Caps(legacyMica: true, roundedCorners: true, captionColor: true));

            Assert.Equal(BackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Auto_Win10_ReturnsNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Auto,
                Caps());

            Assert.Equal(BackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_None_Win11_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.None,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(BackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win22H2_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Mica,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(BackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win11Pre22H2_UsesLegacyMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Mica,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.Equal(BackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win10_DowngradesToNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Mica,
                Caps());

            Assert.Equal(BackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win22H2_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Acrylic,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(BackdropType.Acrylic, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win11Pre22H2_DowngradesToMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Acrylic,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.Equal(BackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win10_DowngradesToNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Acrylic,
                Caps());

            Assert.Equal(BackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win22H2_PassesThrough()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Tabbed,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(BackdropType.Tabbed, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win11Pre22H2_DowngradesToMica()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Tabbed,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.Equal(BackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win10_DowngradesToNone()
        {
            BackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                BackdropType.Tabbed,
                Caps());

            Assert.Equal(BackdropType.None, effective);
        }

        #endregion ResolveEffectiveBackdrop - capability matrix

        #region BuildBackdropPlan - None

        [Fact]
        public void BuildBackdropPlan_None_UsesFallbackBackground_EmitsDwmsbtNone()
        {
            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true),
                fallback);

            Assert.Equal(BackdropType.None, plan.EffectiveBackdrop);
            Assert.False(plan.UseTransparentBackground,
                "None must paint a solid background - transparency would reveal the glass frame.");
            Assert.Equal(fallback, plan.BackgroundColor);
            Assert.Equal(NativeConstants.DWMWA_COLOR_DEFAULT, plan.CaptionColor);
            Assert.True(plan.SystemBackdropType is not null,
                "On 22H2 DWM exposes DWMWA_SYSTEMBACKDROP_TYPE - None must emit DWMSBT_NONE to explicitly clear Mica/Acrylic.");
            Assert.Equal(NativeConstants.DWMSBT_NONE, plan.SystemBackdropType.Value);
            Assert.False(plan.UseLegacyMicaEffect);
        }

        [Fact]
        public void BuildBackdropPlan_None_OnWin10_OmitsSystemBackdropType()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Light,
                Caps(),
                Color.FromRgb(0xFA, 0xFA, 0xFA));

            Assert.False(plan.SystemBackdropType is not null,
                "Windows 10 does not expose DWMWA_SYSTEMBACKDROP_TYPE - the plan must not attempt to set it.");
        }

        #endregion BuildBackdropPlan - None

        #region BuildBackdropPlan - Mica (legacy path on pre-22H2)

        [Fact]
        public void BuildBackdropPlan_Mica_LegacyPath_UsesDwmMicaEffect_NotSystemBackdrop()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Mica,
                ApplicationTheme.Dark,
                Caps(legacyMica: true, roundedCorners: true),
                Colors.White);

            Assert.Equal(BackdropType.Mica, plan.EffectiveBackdrop);
            Assert.True(plan.UseTransparentBackground,
                "Mica requires a transparent window client so DWM can composite the backdrop.");
            Assert.Equal(Colors.Transparent, plan.BackgroundColor);
            Assert.Equal(NativeConstants.DWMWA_COLOR_NONE, plan.CaptionColor);
            Assert.False(plan.SystemBackdropType is not null,
                "Pre-22H2 must not emit DWMWA_SYSTEMBACKDROP_TYPE - only DWMWA_MICA_EFFECT is legal there.");
            Assert.True(plan.UseLegacyMicaEffect,
                "Pre-22H2 Win11 must set the legacy DWMWA_MICA_EFFECT attribute.");
        }

        [Fact]
        public void BuildBackdropPlan_Mica_Win22H2_UsesDwmSystemBackdropType_NotLegacy()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Mica,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true),
                Colors.White);

            Assert.Equal(BackdropType.Mica, plan.EffectiveBackdrop);
            Assert.True(plan.UseTransparentBackground);
            Assert.True(plan.SystemBackdropType is not null);
            Assert.Equal(NativeConstants.DWMSBT_MAINWINDOW, plan.SystemBackdropType.Value);
            Assert.False(plan.UseLegacyMicaEffect,
                "22H2 must use the canonical DWMWA_SYSTEMBACKDROP_TYPE path, not the legacy Mica attribute.");
        }

        #endregion BuildBackdropPlan - Mica (legacy path on pre-22H2)

        #region BuildBackdropPlan - Acrylic + Tabbed (SystemBackdropType mapping)

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win22H2_MapsToTransientWindow()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Acrylic,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true),
                Colors.White);

            Assert.Equal(BackdropType.Acrylic, plan.EffectiveBackdrop);
            Assert.Equal(NativeConstants.DWMSBT_TRANSIENTWINDOW, plan.SystemBackdropType);
        }

        [Fact]
        public void BuildBackdropPlan_Tabbed_Win22H2_MapsToTabbedWindow()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Tabbed,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true),
                Colors.White);

            Assert.Equal(BackdropType.Tabbed, plan.EffectiveBackdrop);
            Assert.Equal(NativeConstants.DWMSBT_TABBEDWINDOW, plan.SystemBackdropType);
        }

        #endregion BuildBackdropPlan - Acrylic + Tabbed (SystemBackdropType mapping)

        #region BuildBackdropPlan - Immersive dark flag

        [Fact]
        public void BuildBackdropPlan_DarkTheme_SetsImmersiveDarkMode()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Dark,
                Caps(systemBackdrop: true),
                Color.FromRgb(0x20, 0x20, 0x20));

            Assert.True(plan.UseImmersiveDarkMode,
                "Dark theme must set DWMWA_USE_IMMERSIVE_DARK_MODE so the native caption renders dark.");
        }

        [Fact]
        public void BuildBackdropPlan_LightTheme_DoesNotSetImmersiveDarkMode()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true),
                Color.FromRgb(0xFA, 0xFA, 0xFA));

            Assert.False(plan.UseImmersiveDarkMode);
        }

        #endregion BuildBackdropPlan - Immersive dark flag

        #region GetCornerPreference - enum → DWMWCP_* mapping

        [Fact]
        public void GetCornerPreference_Round_MapsToDwmwcpRound()
        {
            Assert.Equal(NativeConstants.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.Round));
        }

        [Fact]
        public void GetCornerPreference_Default_MapsToDwmwcpRound()
        {
            // FluenceWindow exposes CornerPreference.Default as "library default" - which in a
            // Fluent library means rounded on Win11. The policy normalises Default to Round.
            Assert.Equal(NativeConstants.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.Default));
        }

        [Fact]
        public void GetCornerPreference_DoNotRound_MapsToDwmwcpDoNotRound()
        {
            Assert.Equal(NativeConstants.DWMWCP_DONOTROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.DoNotRound));
        }

        [Fact]
        public void GetCornerPreference_RoundSmall_MapsToDwmwcpRoundSmall()
        {
            Assert.Equal(NativeConstants.DWMWCP_ROUNDSMALL,
                WindowPolicy.GetCornerPreference(CornerPreference.RoundSmall));
        }

        #endregion GetCornerPreference - enum → DWMWCP_* mapping

        #region CreateWindowChrome - canonical FluenceWindow chrome contract

        [Fact]
        public void CreateWindowChrome_CaptionHeight_IsZero()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.Equal(0d, chrome.CaptionHeight);
        }

        [Fact]
        public void CreateWindowChrome_GlassFrameThickness_IsMinusOneForShadow()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.Equal(new Thickness(-1), chrome.GlassFrameThickness);
        }

        [Fact]
        public void CreateWindowChrome_ResizeBorderThickness_Is4()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.Equal(new Thickness(4), chrome.ResizeBorderThickness);
        }

        [Fact]
        public void CreateWindowChrome_DisablesAeroCaptionButtons()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.False(chrome.UseAeroCaptionButtons,
                "Fluence renders its own caption buttons; the native WPF Aero caption must stay off.");
        }

        [Fact]
        public void CreateWindowChrome_NonClientFrameEdges_IsNone()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.Equal(NonClientFrameEdges.None, chrome.NonClientFrameEdges);
        }

        [Fact]
        public void CreateWindowChrome_CornerRadius_IsZero()
        {
            WindowChrome chrome = WindowPolicy.CreateWindowChrome();
            Assert.Equal(new CornerRadius(0), chrome.CornerRadius);
        }

        #endregion CreateWindowChrome - canonical FluenceWindow chrome contract

        #region GetResizeBorderThickness - maximised / non-resize matrix

        [Fact]
        public void GetResizeBorderThickness_Normal_CanResize_Returns4()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.CanResize);
            Assert.Equal(new Thickness(4), thickness);
        }

        [Fact]
        public void GetResizeBorderThickness_Normal_CanResizeWithGrip_Returns4()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.CanResizeWithGrip);
            Assert.Equal(new Thickness(4), thickness);
        }

        [Fact]
        public void GetResizeBorderThickness_Maximized_ReturnsZero()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Maximized, ResizeMode.CanResize);
            Assert.Equal(new Thickness(0), thickness);
        }

        [Fact]
        public void GetResizeBorderThickness_NoResize_ReturnsZero()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.NoResize);
            Assert.Equal(new Thickness(0), thickness);
        }

        [Fact]
        public void GetResizeBorderThickness_CanMinimize_ReturnsZero()
        {
            Thickness thickness = WindowPolicy.GetResizeBorderThickness(WindowState.Normal, ResizeMode.CanMinimize);
            Assert.Equal(new Thickness(0), thickness);
        }

        #endregion GetResizeBorderThickness - maximised / non-resize matrix

        #region BuildFramePlan - accent border selection

        [Fact]
        public void BuildFramePlan_Normal_ActiveWithAccentBorder_UsesAccentKey()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Color.FromRgb(0x00, 0x78, 0xD4));

            Assert.Equal(new Thickness(2), plan.TemplateBorderThickness);
            Assert.Equal("SystemAccentColorBrush", plan.TemplateBorderBrushResourceKey, StringComparer.Ordinal);
            Assert.NotEqual(NativeConstants.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor);
        }

        [Fact]
        public void BuildFramePlan_Normal_Inactive_UsesCardStrokeKey()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: false,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Colors.Red);

            Assert.Equal("CardStrokeColorDefaultSolidBrush", plan.TemplateBorderBrushResourceKey, StringComparer.Ordinal);
        }

        [Fact]
        public void BuildFramePlan_Maximized_TemplateBorderIsZero()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Maximized,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Colors.Red);

            Assert.Equal(new Thickness(0), plan.TemplateBorderThickness);
        }

        [Fact]
        public void BuildFramePlan_NoBorderColorCapability_KeepsDwmDefault()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(),
                accentColor: Colors.Red);

            Assert.Equal(NativeConstants.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor);
        }

        #endregion BuildFramePlan - accent border selection

        #region WindowCapabilities.Current - sanity

        [Fact]
        public void WindowCapabilities_Current_NotNull()
        {
            WindowCapabilities caps = WindowCapabilities.Current;
            Assert.NotNull(caps);
        }

        #endregion WindowCapabilities.Current - sanity

        #region GetGlassFrameThickness - dual-path

        // WPF-UI's GlassFrameThickness convention: -1 for full DWM glass extension when a
        // backdrop is active, 0.00001 for an invisible-but-resize-borderable frame when no
        // backdrop is active and no shadow is requested. The combined check makes sure we
        // don't render a visible glass-frame artifact when SystemBackdropType=None on Win11.

        [Fact]
        public void GetGlassFrameThickness_NoBackdrop_NoShadow_VeryThin()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.None, hasShadow: false);
            Assert.Equal(0.00001, t.Left, 1e-9);
            Assert.Equal(0.00001, t.Top, 1e-9);
            Assert.Equal(0.00001, t.Right, 1e-9);
            Assert.Equal(0.00001, t.Bottom, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_NoBackdrop_WithShadow_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.None, hasShadow: true);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_MicaBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Mica, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_AcrylicBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Acrylic, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_TabbedBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Tabbed, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_AutoBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(BackdropType.Auto, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        #endregion GetGlassFrameThickness - dual-path
    }
}
