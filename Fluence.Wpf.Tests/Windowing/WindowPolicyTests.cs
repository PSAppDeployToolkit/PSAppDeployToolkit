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
using Windows.Win32;
using Windows.Win32.Graphics.Dwm;
using Xunit;

namespace Fluence.Wpf.Tests.Windowing
{
    // WI-2 S2.6 regression floor for WindowPolicy (internal, visible via
    // [InternalsVisibleTo("Fluence.Wpf.Tests")]). WindowPolicy is pure logic: it maps
    // the requested WindowBackdropType + OS capabilities to an effective backdrop, a DWM plan,
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
            bool borderColor = false,
            bool legacyAcrylic = false)
        {
            return new WindowCapabilities(
                systemBackdrop,
                legacyMica,
                roundedCorners,
                captionColor,
                borderColor,
                legacyAcrylic);
        }

        /// <summary>
        /// The capability snapshot of a Windows 10 build at or past 17063: no DWM backdrop
        /// attribute of any kind, but the legacy <c language="csharp">SetWindowCompositionAttribute</c> acrylic
        /// accent state is available.
        /// </summary>
        private static WindowCapabilities Win10AcrylicCaps()
        {
            return Caps(legacyAcrylic: true);
        }

        private static readonly Color AcrylicTint = Color.FromArgb(0xF0, 0xF9, 0xF9, 0xF9);

        #region ResolveEffectiveBackdrop - capability matrix

        [Fact]
        public void ResolveEffectiveBackdrop_Auto_Win11_22H2_ReturnsMica()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Auto,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true));

            Assert.Equal(WindowBackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Auto_Win11Pre22H2_LegacyMicaOnly_ReturnsMica()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Auto,
                Caps(legacyMica: true, roundedCorners: true, captionColor: true));

            Assert.Equal(WindowBackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Auto_Win10_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Auto,
                Caps());

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_None_Win11_PassesThrough()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.None,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win22H2_PassesThrough()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Mica,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(WindowBackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win11Pre22H2_UsesLegacyMica()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Mica,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.Equal(WindowBackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win10_DowngradesToNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Mica,
                Caps());

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win22H2_PassesThrough()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(WindowBackdropType.Acrylic, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win11Pre22H2_DowngradesToMica()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.Equal(WindowBackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win10_DowngradesToNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Caps());

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win22H2_PassesThrough()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Tabbed,
                Caps(systemBackdrop: true, roundedCorners: true));

            Assert.Equal(WindowBackdropType.Tabbed, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win11Pre22H2_DowngradesToMica()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Tabbed,
                Caps(legacyMica: true, roundedCorners: true));

            Assert.Equal(WindowBackdropType.Mica, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win10_DowngradesToNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Tabbed,
                Caps());

            Assert.Equal(WindowBackdropType.None, effective);
        }

        #endregion ResolveEffectiveBackdrop - capability matrix

        #region ResolveEffectiveBackdrop / BuildBackdropPlan - HighContrast suppresses every material

        // Microsoft Learn "Materials in Windows apps": "High contrast mode: all materials are
        // suppressed; the system applies high-contrast theme colors instead." The Mica design page:
        // "In High Contrast mode, users continue to see the familiar background color of their
        // choosing in place of Mica." WinUI's SystemBackdropConfiguration.IsHighContrast is
        // documented as true when "the system or application high-contrast theme" is applied, which
        // covers the in-app HighContrast theme this library resolves, not just the OS-wide setting.
        // These rows pin that every requested backdrop downgrades to None once resolvedTheme is
        // HighContrast, on every OS capability tier, not only the Windows 10 legacy acrylic path.

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win22H2_HighContrast_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Mica,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                isTransparencyEnabled: true,
                ApplicationTheme.HighContrast);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win22H2_HighContrast_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                isTransparencyEnabled: true,
                ApplicationTheme.HighContrast);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win22H2_HighContrast_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Tabbed,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                isTransparencyEnabled: true,
                ApplicationTheme.HighContrast);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Auto_Win22H2_HighContrast_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Auto,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                isTransparencyEnabled: true,
                ApplicationTheme.HighContrast);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win21H2_HighContrast_ReturnsNone()
        {
            // 21H2 (SupportsMicaEffect only, no DWMWA_SYSTEMBACKDROP_TYPE): HighContrast must still
            // suppress Mica rather than falling through to the legacy DWMWA_MICA_EFFECT path.
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Mica,
                Caps(legacyMica: true, roundedCorners: true),
                isTransparencyEnabled: true,
                ApplicationTheme.HighContrast);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void BuildBackdropPlan_Mica_Win22H2_HighContrast_SuppressesToNone_OpaqueFallback()
        {
            Color fallback = SystemColors.WindowColor;
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Mica,
                ApplicationTheme.HighContrast,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.Equal((DWM_SYSTEMBACKDROP_TYPE?)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE, plan.SystemBackdropType);
            Assert.Equal(fallback, plan.BackgroundColor);
            Assert.False(plan.UseLegacyMicaEffect);
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win22H2_HighContrast_SuppressesToNone_OpaqueFallback()
        {
            Color fallback = SystemColors.WindowColor;
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.HighContrast,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.Equal((DWM_SYSTEMBACKDROP_TYPE?)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE, plan.SystemBackdropType);
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Tabbed_Win22H2_HighContrast_SuppressesToNone_OpaqueFallback()
        {
            Color fallback = SystemColors.WindowColor;
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Tabbed,
                ApplicationTheme.HighContrast,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.Equal((DWM_SYSTEMBACKDROP_TYPE?)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE, plan.SystemBackdropType);
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Auto_Win22H2_HighContrast_SuppressesToNone_OpaqueFallback()
        {
            Color fallback = SystemColors.WindowColor;
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Auto,
                ApplicationTheme.HighContrast,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.Equal((DWM_SYSTEMBACKDROP_TYPE?)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE, plan.SystemBackdropType);
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Mica_Win21H2_HighContrast_SuppressesToNone_NoLegacyMicaEffect()
        {
            Color fallback = SystemColors.WindowColor;
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Mica,
                ApplicationTheme.HighContrast,
                Caps(legacyMica: true, roundedCorners: true),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.False(plan.UseLegacyMicaEffect,
                "HighContrast must suppress Mica outright, not fall through to the legacy DWMWA_MICA_EFFECT path.");
            Assert.False(plan.SystemBackdropType is not null,
                "21H2 does not expose DWMWA_SYSTEMBACKDROP_TYPE - the plan must not attempt to set it.");
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        #endregion ResolveEffectiveBackdrop / BuildBackdropPlan - HighContrast suppresses every material

        #region BuildBackdropPlan - None

        [Fact]
        public void BuildBackdropPlan_None_UsesFallbackBackground_EmitsDwmsbtNone()
        {
            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.None,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true),
                fallback,
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.NotEqual(Colors.Transparent, plan.BackgroundColor);
            Assert.Equal(fallback, plan.BackgroundColor);
            Assert.Equal(PInvoke.DWMWA_COLOR_DEFAULT, plan.CaptionColor);
            Assert.Equal((DWM_SYSTEMBACKDROP_TYPE?)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE, plan.SystemBackdropType);
            Assert.False(plan.UseLegacyMicaEffect);
        }

        [Fact]
        public void BuildBackdropPlan_None_OnWin10_OmitsSystemBackdropType()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.None,
                ApplicationTheme.Light,
                Caps(),
                Color.FromRgb(0xFA, 0xFA, 0xFA),
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.False(plan.SystemBackdropType is not null,
                "Windows 10 does not expose DWMWA_SYSTEMBACKDROP_TYPE - the plan must not attempt to set it.");
        }

        #endregion BuildBackdropPlan - None

        #region BuildBackdropPlan - Mica (legacy path on pre-22H2)

        [Fact]
        public void BuildBackdropPlan_Mica_LegacyPath_UsesDwmMicaEffect_NotSystemBackdrop()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Mica,
                ApplicationTheme.Dark,
                Caps(legacyMica: true, roundedCorners: true),
                Colors.White,
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.Mica, plan.EffectiveBackdrop);
            Assert.Equal(Colors.Transparent, plan.BackgroundColor);
            Assert.Equal(PInvoke.DWMWA_COLOR_NONE, plan.CaptionColor);
            Assert.False(plan.SystemBackdropType is not null,
                "Pre-22H2 must not emit DWMWA_SYSTEMBACKDROP_TYPE - only DWMWA_MICA_EFFECT is legal there.");
            Assert.True(plan.UseLegacyMicaEffect,
                "Pre-22H2 Win11 must set the legacy DWMWA_MICA_EFFECT attribute.");
        }

        [Fact]
        public void BuildBackdropPlan_Mica_Win22H2_UsesDwmSystemBackdropType_NotLegacy()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true),
                Colors.White,
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.Mica, plan.EffectiveBackdrop);
            Assert.Equal(Colors.Transparent, plan.BackgroundColor);
            Assert.Equal(DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW, plan.SystemBackdropType);
            Assert.False(plan.UseLegacyMicaEffect,
                "22H2 must use the canonical DWMWA_SYSTEMBACKDROP_TYPE path, not the legacy Mica attribute.");
        }

        #endregion BuildBackdropPlan - Mica (legacy path on pre-22H2)

        #region BuildBackdropPlan - Acrylic + Tabbed (SystemBackdropType mapping)

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win22H2_MapsToTransientWindow()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true),
                Colors.White,
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.Acrylic, plan.EffectiveBackdrop);
            Assert.Equal(DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW, plan.SystemBackdropType);
        }

        [Fact]
        public void BuildBackdropPlan_Tabbed_Win22H2_MapsToTabbedWindow()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Tabbed,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true),
                Colors.White,
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.Equal(WindowBackdropType.Tabbed, plan.EffectiveBackdrop);
            Assert.Equal(DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TABBEDWINDOW, plan.SystemBackdropType);
        }

        #endregion BuildBackdropPlan - Acrylic + Tabbed (SystemBackdropType mapping)

        #region Windows 10 legacy acrylic (SetWindowCompositionAttribute accent policy)

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win10Legacy_TransparencyOn_StaysAcrylic()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Win10AcrylicCaps(),
                isTransparencyEnabled: true,
                ApplicationTheme.Light);

            Assert.Equal(WindowBackdropType.Acrylic, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win10Legacy_DefaultTransparencyArgument_ReturnsNone()
        {
            // The transparency argument defaults to false on purpose: a caller that does not read
            // the OS toggle must not be handed a blur the user has switched off.
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Win10AcrylicCaps());

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win10Legacy_HighContrast_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Win10AcrylicCaps(),
                isTransparencyEnabled: true,
                ApplicationTheme.HighContrast);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win10PreLegacyBuild_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Caps(),
                isTransparencyEnabled: true,
                ApplicationTheme.Light);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Tabbed_Win10Legacy_ReturnsNone()
        {
            // The legacy accent policy has no tabbed equivalent, so Tabbed keeps downgrading to
            // None on Windows 10 even where Acrylic now survives.
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Tabbed,
                Win10AcrylicCaps(),
                isTransparencyEnabled: true,
                ApplicationTheme.Light);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Mica_Win10Legacy_ReturnsNone()
        {
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Mica,
                Win10AcrylicCaps(),
                isTransparencyEnabled: true,
                ApplicationTheme.Light);

            Assert.Equal(WindowBackdropType.None, effective);
        }

        [Fact]
        public void ResolveEffectiveBackdrop_Acrylic_Win11Pre22H2_TransparencyOn_StillDowngradesToMica()
        {
            // The Win11 arms must not be reachable by the legacy-acrylic branch: 21H2 has Mica,
            // which is a better answer than a Windows 10 accent blur.
            WindowBackdropType effective = WindowPolicy.ResolveEffectiveBackdrop(
                WindowBackdropType.Acrylic,
                Caps(legacyMica: true, roundedCorners: true, captionColor: true),
                isTransparencyEnabled: true,
                ApplicationTheme.Light);

            Assert.Equal(WindowBackdropType.Mica, effective);
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win10Legacy_TransparencyOn_UsesLegacyAcrylic()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.Light,
                Win10AcrylicCaps(),
                Color.FromRgb(0xFA, 0xFA, 0xFA),
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: AcrylicTint);

            Assert.Equal(WindowBackdropType.Acrylic, plan.EffectiveBackdrop);
            Assert.True(plan.UseLegacyAcrylic,
                "Windows 10 17063+ with transparency on must take the legacy accent-policy path.");
            Assert.Equal(AcrylicTint, plan.LegacyAcrylicTintColor);
            Assert.Equal(Colors.Transparent, plan.BackgroundColor);
            Assert.Equal(PInvoke.DWMWA_COLOR_NONE, plan.CaptionColor);
            Assert.False(plan.SystemBackdropType is not null,
                "Windows 10 does not expose DWMWA_SYSTEMBACKDROP_TYPE - the plan must not attempt to set it.");
            Assert.False(plan.UseLegacyMicaEffect,
                "The legacy Mica attribute does not exist on Windows 10 and is mutually exclusive with acrylic.");
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win10Legacy_DarkTheme_CarriesDarkTintAndImmersiveDark()
        {
            Color darkTint = Color.FromArgb(0xF0, 0x2C, 0x2C, 0x2C);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.Dark,
                Win10AcrylicCaps(),
                Color.FromRgb(0x20, 0x20, 0x20),
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: darkTint);

            Assert.True(plan.UseLegacyAcrylic);
            Assert.Equal(darkTint, plan.LegacyAcrylicTintColor);
            Assert.True(plan.UseImmersiveDarkMode);
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win10Legacy_TransparencyOff_FallsBackToOpaqueNone()
        {
            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.Light,
                Win10AcrylicCaps(),
                fallback,
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: AcrylicTint);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.False(plan.UseLegacyAcrylic,
                "The OS transparency-effects toggle being off must suppress the accent policy entirely.");
            Assert.NotEqual(Colors.Transparent, plan.BackgroundColor);
            Assert.Equal(fallback, plan.BackgroundColor);
            Assert.Equal(Colors.Transparent, plan.LegacyAcrylicTintColor);
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win10Legacy_HighContrast_FallsBackToOpaqueNone()
        {
            Color fallback = Color.FromRgb(0x00, 0x00, 0x00);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.HighContrast,
                Win10AcrylicCaps(),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: AcrylicTint);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.False(plan.UseLegacyAcrylic,
                "High contrast must never be blurred - the theme exists to guarantee contrast.");
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win10PreLegacyBuild_FallsBackToOpaqueNone()
        {
            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.Light,
                Caps(),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: AcrylicTint);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.False(plan.UseLegacyAcrylic);
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Tabbed_Win10Legacy_FallsBackToOpaqueNone()
        {
            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Tabbed,
                ApplicationTheme.Light,
                Win10AcrylicCaps(),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: AcrylicTint);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.False(plan.UseLegacyAcrylic);
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Mica_Win10Legacy_FallsBackToOpaqueNone()
        {
            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                Win10AcrylicCaps(),
                fallback,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: AcrylicTint);

            Assert.Equal(WindowBackdropType.None, plan.EffectiveBackdrop);
            Assert.False(plan.UseLegacyAcrylic);
            Assert.False(plan.UseLegacyMicaEffect);
            Assert.Equal(fallback, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_Win22H2_DoesNotUseLegacyAcrylic()
        {
            // The Windows 11 path must stay byte-identical whatever the transparency toggle says:
            // DWM honours that setting itself.
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.Acrylic,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true, roundedCorners: true, captionColor: true, borderColor: true),
                Colors.White,
                isTransparencyEnabled: true,
                legacyAcrylicTintColor: AcrylicTint);

            Assert.Equal(WindowBackdropType.Acrylic, plan.EffectiveBackdrop);
            Assert.Equal(DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW, plan.SystemBackdropType);
            Assert.False(plan.UseLegacyAcrylic,
                "Windows 11 has DWM acrylic - the legacy accent policy must never be used there.");
            Assert.Equal(Colors.Transparent, plan.LegacyAcrylicTintColor);
        }

        [Fact]
        public void WindowCapabilities_LegacyAcrylicFlag_IsPlumbedThroughTheConstructor()
        {
            WindowCapabilities without = Caps();
            WindowCapabilities with = Caps(legacyAcrylic: true);

            Assert.False(without.SupportsLegacyAcrylic,
                "The legacy-acrylic capability must default to off so existing snapshots are unchanged.");
            Assert.True(with.SupportsLegacyAcrylic);
            Assert.False(with.SupportsSystemBackdropType);
            Assert.False(with.SupportsMicaEffect);
        }

        #endregion Windows 10 legacy acrylic (SetWindowCompositionAttribute accent policy)

        #region BuildBackdropPlan - Immersive dark flag

        [Fact]
        public void BuildBackdropPlan_DarkTheme_SetsImmersiveDarkMode()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.None,
                ApplicationTheme.Dark,
                Caps(systemBackdrop: true),
                Color.FromRgb(0x20, 0x20, 0x20),
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.True(plan.UseImmersiveDarkMode,
                "Dark theme must set DWMWA_USE_IMMERSIVE_DARK_MODE so the native caption renders dark.");
        }

        [Fact]
        public void BuildBackdropPlan_LightTheme_DoesNotSetImmersiveDarkMode()
        {
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdropType.None,
                ApplicationTheme.Light,
                Caps(systemBackdrop: true),
                Color.FromRgb(0xFA, 0xFA, 0xFA),
                isTransparencyEnabled: false,
                legacyAcrylicTintColor: Colors.Transparent);

            Assert.False(plan.UseImmersiveDarkMode);
        }

        #endregion BuildBackdropPlan - Immersive dark flag

        #region GetCornerPreference - enum → DWMWCP_* mapping

        [Fact]
        public void GetCornerPreference_Round_MapsToDwmwcpRound()
        {
            Assert.Equal(DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(WindowCornerPreference.Round));
        }

        [Fact]
        public void GetCornerPreference_Default_MapsToDwmwcpRound()
        {
            // FluenceWindow exposes WindowCornerPreference.Default as "library default" - which in a
            // Fluent library means rounded on Win11. The policy normalises Default to Round.
            Assert.Equal(DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(WindowCornerPreference.Default));
        }

        [Fact]
        public void GetCornerPreference_DoNotRound_MapsToDwmwcpDoNotRound()
        {
            Assert.Equal(DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND,
                WindowPolicy.GetCornerPreference(WindowCornerPreference.DoNotRound));
        }

        [Fact]
        public void GetCornerPreference_RoundSmall_MapsToDwmwcpRoundSmall()
        {
            Assert.Equal(DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL,
                WindowPolicy.GetCornerPreference(WindowCornerPreference.RoundSmall));
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
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Color.FromRgb(0x00, 0x78, 0xD4));

            Assert.Equal("SystemAccentColorBrush", plan.TemplateBorderBrushResourceKey, StringComparer.Ordinal);
            Assert.NotEqual(PInvoke.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor);
        }

        [Fact]
        public void BuildFramePlan_Normal_Inactive_UsesSurfaceStrokeKey()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                isActive: false,
                isAccentBorderEnabled: true,
                capabilities: Caps(borderColor: true),
                accentColor: Colors.Red);

            Assert.Equal("SurfaceStrokeColorDefaultBrush", plan.TemplateBorderBrushResourceKey, StringComparer.Ordinal);
        }

        [Fact]
        public void BuildFramePlan_NoBorderColorCapability_KeepsDwmDefault()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(),
                accentColor: Colors.Red);

            Assert.Equal(PInvoke.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor);
        }

        #endregion BuildFramePlan - accent border selection

        #region BuildFramePlan - template border thickness and inactive brush key

        [Fact]
        public void BuildFramePlan_BorderColorSupported_Active_TemplateThicknessIsZero()
        {
            // roundedCorners and borderColor both true reads as a real Windows 11 snapshot; the two
            // capabilities are never true independently on any shipping OS build.
            FramePlan plan = WindowPolicy.BuildFramePlan(
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(roundedCorners: true, borderColor: true),
                accentColor: Colors.Red);

            Assert.Equal(new Thickness(0), plan.TemplateBorderThickness);
        }

        [Fact]
        public void BuildFramePlan_BorderColorSupported_Inactive_TemplateThicknessIsZero()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                isActive: false,
                isAccentBorderEnabled: true,
                capabilities: Caps(roundedCorners: true, borderColor: true),
                accentColor: Colors.Red);

            Assert.Equal(new Thickness(0), plan.TemplateBorderThickness);
        }

        [Fact]
        public void BuildFramePlan_BorderColorUnsupported_Active_TemplateThicknessIsOne()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities: Caps(),
                accentColor: Colors.Red);

            Assert.Equal(new Thickness(1), plan.TemplateBorderThickness);
        }

        [Fact]
        public void BuildFramePlan_BorderColorUnsupported_Inactive_TemplateThicknessIsOne()
        {
            FramePlan plan = WindowPolicy.BuildFramePlan(
                isActive: false,
                isAccentBorderEnabled: true,
                capabilities: Caps(),
                accentColor: Colors.Red);

            Assert.Equal(new Thickness(1), plan.TemplateBorderThickness);
        }

        [Fact]
        public void BuildFramePlan_BorderColorUnsupported_Inactive_UsesSurfaceStrokeKey()
        {
            // The Windows 10 path (no DWMWA_BORDER_COLOR) must still resolve the same canonical
            // inactive brush key as the Windows 11 path; only the thickness differs by capability.
            FramePlan plan = WindowPolicy.BuildFramePlan(
                isActive: false,
                isAccentBorderEnabled: true,
                capabilities: Caps(),
                accentColor: Colors.Red);

            Assert.Equal("SurfaceStrokeColorDefaultBrush", plan.TemplateBorderBrushResourceKey, StringComparer.Ordinal);
        }

        #endregion BuildFramePlan - template border thickness and inactive brush key

        #region WindowCapabilities.Current - sanity

        [Fact]
        public void WindowCapabilities_Current_NotNull()
        {
            Assert.NotNull(WindowCapabilities.Current);
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
            Thickness t = WindowPolicy.GetGlassFrameThickness(WindowBackdropType.None, hasShadow: false);
            Assert.Equal(0.00001, t.Left, 1e-9);
            Assert.Equal(0.00001, t.Top, 1e-9);
            Assert.Equal(0.00001, t.Right, 1e-9);
            Assert.Equal(0.00001, t.Bottom, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_NoBackdrop_WithShadow_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(WindowBackdropType.None, hasShadow: true);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_MicaBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(WindowBackdropType.Mica, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_AcrylicBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(WindowBackdropType.Acrylic, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_TabbedBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(WindowBackdropType.Tabbed, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        [Fact]
        public void GetGlassFrameThickness_AutoBackdrop_FullGlass()
        {
            Thickness t = WindowPolicy.GetGlassFrameThickness(WindowBackdropType.Auto, hasShadow: false);
            Assert.Equal(-1, t.Left, 1e-9);
        }

        #endregion GetGlassFrameThickness - dual-path

        #region ResolveContentLayerPreBlend - 10 bpc alpha-quantisation gate

        // KNOWN_ISSUES.md: "Translucent layers over a DWM backdrop lose alpha precision on a
        // 10 bpc display". NavigationViewContentBackground (translucent content-layer token, the
        // key the NavigationViewContentBackgroundBrush key actually derives from) and
        // SolidBackgroundFillColorBase (opaque window base) for Light and Dark, matching the
        // canonical theme tables. LightCanonical / DarkCanonical are the WinUI
        // LayerOnMicaBaseAltFillColorTertiary token values.
        private static readonly Color LightLayerFill = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);
        private static readonly Color LightSolidBase = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3);
        private static readonly Color LightCanonical = Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9);
        private static readonly Color DarkLayerFill = Color.FromArgb(0x4C, 0x3A, 0x3A, 0x3A);
        private static readonly Color DarkSolidBase = Color.FromArgb(0xFF, 0x20, 0x20, 0x20);
        private static readonly Color DarkCanonical = Color.FromArgb(0xFF, 0x2C, 0x2C, 0x2C);

        [Fact]
        public void ResolveContentLayerPreBlend_Bpc8_ReturnsNull()
        {
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                new DisplayColorDepth(8, advancedColorEnabled: false),
                LightCanonical,
                LightLayerFill,
                LightSolidBase);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_BpcUnknown_ReturnsNull()
        {
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                new DisplayColorDepth(0, advancedColorEnabled: false),
                LightCanonical,
                LightLayerFill,
                LightSolidBase);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_BackdropNone_ReturnsNull()
        {
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.None,
                ApplicationTheme.Light,
                new DisplayColorDepth(10, advancedColorEnabled: false),
                LightCanonical,
                LightLayerFill,
                LightSolidBase);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_HighContrast_ReturnsNull()
        {
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.HighContrast,
                new DisplayColorDepth(10, advancedColorEnabled: false),
                LightCanonical,
                LightLayerFill,
                LightSolidBase);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_AdvancedColorEnabled_ReturnsNull()
        {
            // Measured: the same 10 bpc path with advanced color enabled (the GPU driver's 10-bit
            // pixel format toggle) composites client alpha at full precision, so no pre-blend
            // substitute is needed.
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                new DisplayColorDepth(10, advancedColorEnabled: true),
                LightCanonical,
                LightLayerFill,
                LightSolidBase);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_Bpc10AdvancedColorOff_Mica_Light_CanonicalPresent_ReturnsCanonical()
        {
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                new DisplayColorDepth(10, advancedColorEnabled: false),
                LightCanonical,
                LightLayerFill,
                LightSolidBase);

            Assert.Equal(LightCanonical, result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_Bpc10AdvancedColorOff_Mica_Light_CanonicalNull_ReturnsComputedFallback()
        {
            // The Light fallback composite reproduces the canonical value exactly:
            // round(0.501961 * 0xFF + 0.498039 * 0xF3) = round(249.02) = 249 = 0xF9 per channel.
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                new DisplayColorDepth(10, advancedColorEnabled: false),
                canonicalPreBlend: null,
                LightLayerFill,
                LightSolidBase);

            Assert.Equal(LightCanonical, result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_Bpc10AdvancedColorOff_Tabbed_Light_CanonicalPresent_ReturnsCanonical()
        {
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Tabbed,
                ApplicationTheme.Light,
                new DisplayColorDepth(10, advancedColorEnabled: false),
                LightCanonical,
                LightLayerFill,
                LightSolidBase);

            Assert.Equal(LightCanonical, result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_Bpc10AdvancedColorOff_Mica_Dark_CanonicalPresent_ReturnsCanonical()
        {
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Dark,
                new DisplayColorDepth(10, advancedColorEnabled: false),
                DarkCanonical,
                DarkLayerFill,
                DarkSolidBase);

            Assert.Equal(DarkCanonical, result);
        }

        [Fact]
        public void ResolveContentLayerPreBlend_Bpc10AdvancedColorOff_Mica_Dark_CanonicalNull_ReturnsComputedFallback()
        {
            // round(0.298039 * 0x3A + 0.701961 * 0x20) = round(39.749) = 40 = 0x28, per channel
            // (R=G=B for both Dark tokens), MidpointRounding.AwayFromZero. This deliberately does
            // not equal DarkCanonical (#FF2C2C2C): the fallback is a reasonable approximation, not a
            // reproduction, of the canonical WinUI value, which is why the canonical key is
            // authoritative whenever it resolves.
            Color? result = WindowPolicy.ResolveContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Dark,
                new DisplayColorDepth(10, advancedColorEnabled: false),
                canonicalPreBlend: null,
                DarkLayerFill,
                DarkSolidBase);

            Assert.Equal(Color.FromArgb(0xFF, 0x28, 0x28, 0x28), result);
        }

        [Fact]
        public void ShouldApplyContentLayerPreBlend_Bpc10AdvancedColorOff_Mica_ReturnsTrue()
        {
            Assert.True(WindowPolicy.ShouldApplyContentLayerPreBlend(
                WindowBackdropType.Mica,
                ApplicationTheme.Light,
                new DisplayColorDepth(10, advancedColorEnabled: false)));
        }

        [Fact]
        public void ShouldApplyContentLayerPreBlend_Acrylic_ReturnsFalse()
        {
            // Acrylic is excluded pending its own 0x4 measurement; see PreBlendEligibleBackdrops.
            Assert.False(WindowPolicy.ShouldApplyContentLayerPreBlend(
                WindowBackdropType.Acrylic,
                ApplicationTheme.Light,
                new DisplayColorDepth(10, advancedColorEnabled: false)));
        }

        #endregion ResolveContentLayerPreBlend - 10 bpc alpha-quantisation gate

        #region BuildBackdropPlan - Acrylic falls back to Mica when only the legacy Mica effect is available

        [Fact]
        public void BuildBackdropPlan_Acrylic_FallsBackToMica_WhenMicaEffectButNoSystemBackdrop()
        {
            // Windows 11 21H2: supports DwmSetWindowAttribute(DWMWA_MICA_EFFECT) but NOT
            // DWMWA_SYSTEMBACKDROP_TYPE. Acrylic request must downgrade to Mica.
            WindowCapabilities caps = new(
                supportsSystemBackdropType: false,
                supportsMicaEffect: true,
                supportsRoundedCorners: false,
                supportsCaptionColor: false);

            Color fallback = Color.FromRgb(0x20, 0x20, 0x20);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(WindowBackdropType.Acrylic, ApplicationTheme.Dark, caps, fallback, isTransparencyEnabled: false, legacyAcrylicTintColor: Colors.Transparent);

            // Should fall back to Mica (legacy) and use transparent background.
            Assert.Equal(Colors.Transparent, plan.BackgroundColor);
            Assert.Equal(WindowBackdropType.Mica, plan.EffectiveBackdrop);
        }

        #endregion BuildBackdropPlan - Acrylic falls back to Mica when only the legacy Mica effect is available
    }
}
