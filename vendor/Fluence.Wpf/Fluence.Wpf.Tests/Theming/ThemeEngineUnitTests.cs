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
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Fluence.Wpf.Theming;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Unit tests for Stage-1 engine classes: <see cref="ColorMap"/>, <see cref="BrushFactory"/>,
    /// <see cref="BaseColorTables"/>, and <see cref="FluenceThemeEngine"/> isolation smoke tests.
    /// These tests exercise every engine class directly so that unused-member diagnostics do not
    /// fire during Stage 1 (before the engine is wired to the public facades).
    /// </summary>
    public class ThemeEngineUnitTests
    {
        private static readonly Color TestBlue = Color.FromRgb(0x00, 0x78, 0xD4);

        private static AccentPalette MakeTestPalette()
        {
            return AccentResolver.Resolve(AccentIntent.FromCustomGenerated(TestBlue), ApplicationTheme.Light);
        }

        // ------------------------------------------------------------------ ColorMap --

        /// <summary>
        /// ColorMap.Build for each theme must contain the core accent-derived key.
        /// </summary>
        /// <param name="theme">The theme to build the map for.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        public Task ColorMap_Build_ContainsAccentFillColorDefaultAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(theme, p);
                Assert.True(m.TryGetValue("AccentFillColorDefault", out Color accentFillColorDefault), $"AccentFillColorDefault must be present in {theme}.");
                Assert.NotEqual(default, accentFillColorDefault);
            });
        }

        /// <summary>
        /// ColorMap.Build must emit the SystemAccentColor raw-ramp keys.
        /// </summary>
        [Fact]
        public Task ColorMap_Build_Light_ContainsRawRampKeysAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.Light, p);
                Assert.True(m.TryGetValue("SystemAccentColor", out Color systemAccentColor), "SystemAccentColor missing.");
                Assert.True(m.ContainsKey("SystemAccentColorLight1"), "SystemAccentColorLight1 missing.");
                Assert.True(m.ContainsKey("SystemAccentColorDark3"), "SystemAccentColorDark3 missing.");
                Assert.Equal(TestBlue, systemAccentColor);
            });
        }

        /// <summary>
        /// ColorMap.Build must emit TitleBarActiveColor.
        /// </summary>
        [Fact]
        public Task ColorMap_Build_Light_ContainsTitleBarActiveColorAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.Light, p);
                Assert.True(m.ContainsKey("TitleBarActiveColor"), "TitleBarActiveColor must be present.");
            });
        }

        /// <summary>
        /// ColorMap.Build for HighContrast must still emit the raw SystemAccentColor value so
        /// callers can inspect the requested palette. The control-facing accent state tokens are
        /// resolved separately from the live high-contrast SystemColors pair.
        /// </summary>
        [Fact]
        public Task ColorMap_Build_HighContrast_ContainsSystemAccentColorAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.HighContrast, p);
                Assert.True(m.Count > 5, "ColorMap.Build(HighContrast) must return a non-trivial map.");
                Assert.True(m.TryGetValue("SystemAccentColor", out Color systemAccentColor), "SystemAccentColor must be present even in HC.");
                Assert.Equal(TestBlue, systemAccentColor);
            });
        }

        /// <summary>
        /// ColorMap.Build keeps the shared WinUI high-contrast accent family neutral while
        /// publishing distinct control-state colors for checked and accent buttons.
        /// </summary>
        [Fact]
        public Task ColorMap_Build_HighContrast_UsesSystemAccentStateColorsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.HighContrast, p);
                Color highlight = SystemColors.HighlightColor;
                Color highlightText = SystemColors.HighlightTextColor;
                Color grayText = SystemColors.GrayTextColor;

                Color window = SystemColors.WindowColor;
                Color windowText = SystemColors.WindowTextColor;
                Assert.Equal(window, m["AccentFillColorSelectedTextBackground"]);
                Assert.Equal(window, m["AccentFillColorDefault"]);
                Assert.Equal(window, m["AccentFillColorSecondary"]);
                Assert.Equal(window, m["AccentFillColorTertiary"]);
                Assert.Equal(window, m["AccentFillColorDisabled"]);
                Assert.Equal(windowText, m["AccentTextFillColorPrimary"]);
                Assert.Equal(windowText, m["AccentTextFillColorSecondary"]);
                Assert.Equal(windowText, m["AccentTextFillColorTertiary"]);
                Assert.Equal(grayText, m["AccentTextFillColorDisabled"]);
                Assert.Equal(windowText, m["TextOnAccentFillColorSelectedText"]);
                Assert.Equal(windowText, m["TextOnAccentFillColorPrimary"]);
                Assert.Equal(windowText, m["TextOnAccentFillColorSecondary"]);
                Assert.Equal(grayText, m["TextOnAccentFillColorDisabled"]);
                Assert.Equal(highlight, m["AccentButtonBackground"]);
                Assert.Equal(highlight, m["AccentButtonBackgroundPointerOver"]);
                Assert.Equal(window, m["AccentButtonBackgroundPressed"]);
                Assert.Equal(highlightText, m["AccentButtonForeground"]);
                Assert.Equal(windowText, m["AccentButtonForegroundPressed"]);
                Assert.Equal(highlight, m["ToggleButtonBackgroundChecked"]);
                Assert.Equal(SystemColors.ControlTextColor, m["ToggleButtonBackgroundCheckedPointerOver"]);
                Assert.Equal(highlightText, m["ToggleButtonBackgroundCheckedPressed"]);
            });
        }

        // ------------------------------------------------------------------ BrushFactory --

        /// <summary>
        /// BrushFactory.Build must produce a frozen SolidColorBrush for each color key.
        /// </summary>
        [Fact]
        public Task BrushFactory_Build_ProducesFrozenBrushForColorKeyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> colors = new(StringComparer.Ordinal)
                {
                    ["TestColorA"] = Colors.Red,
                    ["TestColorB"] = Color.FromArgb(0x80, 0x00, 0x80, 0xFF),
                };
                ResourceDictionary d = BrushFactory.Build(colors);

                Assert.True(d.Contains("TestColorA"), "Color key must be present.");
                Assert.True(d.Contains("TestColorABrush"), "Brush key must be present.");
                SolidColorBrush brush = (SolidColorBrush)d["TestColorABrush"];
                Assert.Equal(Colors.Red, brush.Color);
                Assert.True(brush.IsFrozen, "Brush must be frozen.");
            });
        }

        /// <summary>
        /// BrushFactory.Build must emit both the Color token and the Brush twin for semi-transparent colors.
        /// </summary>
        [Fact]
        public Task BrushFactory_Build_PreservesAlphaChannelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Color semiTransparent = Color.FromArgb(0x80, 0xFF, 0x00, 0x00);
                Dictionary<string, Color> colors = new(StringComparer.Ordinal) { ["AlphaColor"] = semiTransparent };
                ResourceDictionary d = BrushFactory.Build(colors);

                Color stored = (Color)d["AlphaColor"];
                Assert.Equal(semiTransparent, stored);
                SolidColorBrush brush = (SolidColorBrush)d["AlphaColorBrush"];
                Assert.Equal((byte)0x80, brush.Color.A);
            });
        }

        // ------------------------------------------------------------------ BaseColorTables --

        /// <summary>
        /// BaseColorTables.Load for each theme must return a non-empty map; Light additionally
        /// pins a known token.
        /// </summary>
        /// <param name="theme">The theme to load base color tables for.</param>
        /// <param name="minimumCount">The minimum number of entries expected.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light, 10)]
        [InlineData(ApplicationTheme.Dark, 10)]
        [InlineData(ApplicationTheme.HighContrast, 5)]
        public Task BaseColorTables_Load_ReturnsColorsAsync(ApplicationTheme theme, int minimumCount)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> m = BaseColorTables.Load(theme);
                Assert.True(m.Count > minimumCount, $"BaseColorTables.Load({theme}) must return many color entries.");
                if (theme is ApplicationTheme.Light)
                {
                    Assert.True(m.ContainsKey("TextFillColorPrimary"), "TextFillColorPrimary must be present.");
                }
            });
        }

        // ------------------------------------------------------------------ FluenceThemeEngine smoke --

        /// <summary>
        /// FluenceThemeEngine.Apply must publish a computed dictionary into application resources
        /// and populate ResolvedTheme and CurrentPalette. The test resets state before and after
        /// to avoid leaking into other fixtures.
        /// </summary>
        [Fact]
        public async Task FluenceThemeEngine_Apply_PublishesResourcesAndSetsStateAsync()
        {
            await WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();

                FluenceThemeEngine.SetAccentIntent(AccentIntent.FromCustom(TestBlue));
                _ = FluenceThemeEngine.Apply(ApplicationTheme.Light);

                Assert.Equal(ApplicationTheme.Light, FluenceThemeEngine.ResolvedTheme);
                Assert.Equal(TestBlue, FluenceThemeEngine.CurrentPalette.Accent);
                Assert.True(app.Resources.MergedDictionaries.Count > 0, "MergedDictionaries must be non-empty after Apply.");

                // Verify at least one color key is present in the published dictionary
                object accentFill = Assert.IsType<object>(app.Resources.MergedDictionaries[0]["AccentFillColorDefault"], exactMatch: false);
            }).ConfigureAwait(true);

            // Tear down after the smoke test so other fixtures see a clean state
            await WpfTestSta.RunOnStaAsync(static () =>
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();
                Helpers.AcrylicNoiseHelper.ResetForTesting();
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// FluenceThemeEngine.Apply called twice must replace slot [0] rather than inserting a second entry.
        /// </summary>
        [Fact]
        public async Task FluenceThemeEngine_Apply_ReplacesComputedSlotOnSecondCallAsync()
        {
            await WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();

                FluenceThemeEngine.SetAccentIntent(AccentIntent.FromCustom(TestBlue));
                _ = FluenceThemeEngine.Apply(ApplicationTheme.Light);
                int countAfterFirst = app.Resources.MergedDictionaries.Count;
                ResourceDictionary slotZeroFirst = app.Resources.MergedDictionaries[0];

                _ = FluenceThemeEngine.Apply(ApplicationTheme.Dark);
                int countAfterSecond = app.Resources.MergedDictionaries.Count;
                ResourceDictionary slotZeroSecond = app.Resources.MergedDictionaries[0];

                Assert.Equal(countAfterFirst, countAfterSecond);
                Assert.NotSame(slotZeroFirst, slotZeroSecond);
                Assert.Equal(ApplicationTheme.Dark, FluenceThemeEngine.ResolvedTheme);
            }).ConfigureAwait(true);

            // Tear down
            await WpfTestSta.RunOnStaAsync(static () =>
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();
                Helpers.AcrylicNoiseHelper.ResetForTesting();
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// FluenceThemeEngine.Apply must not raise Published when Application.Current is null (the
        /// headless / early-startup case), because Publish's early-return means nothing was actually
        /// published into application resources. Application.Current is a process-wide static shared
        /// by every fixture on WpfTestSta's single STA dispatcher, so this test cannot call
        /// Application.Shutdown() to reach that state: WPF forbids constructing a second Application
        /// in the same AppDomain, which would permanently break every later fixture. Instead the live
        /// Application instance is detached from the private static backing field for the duration of
        /// the Apply call and restored immediately afterward, leaving the shared instance untouched.
        /// </summary>
        [Fact]
        public async Task FluenceThemeEngine_Apply_DoesNotRaisePublished_WhenApplicationIsNullAsync()
        {
            await WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                FieldInfo appInstanceField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Application._appInstance field not found; WPF internals changed.");

                FluenceThemeEngine.ResetForTesting();
                FluenceThemeEngine.SetAccentIntent(AccentIntent.FromCustom(TestBlue));

                int raised = 0;
                void OnPublished(object? sender, EventArgs e) { raised++; }
                FluenceThemeEngine.Published += OnPublished;
                try
                {
                    appInstanceField.SetValue(obj: null, value: null);
                    Assert.Null(Application.Current);

                    _ = FluenceThemeEngine.Apply(ApplicationTheme.Light);
                }
                finally
                {
                    appInstanceField.SetValue(obj: null, value: app);
                    FluenceThemeEngine.Published -= OnPublished;
                }

                Assert.Equal(0, raised);
                Assert.NotNull(Application.Current);
                Assert.Same(app, Application.Current);
            }).ConfigureAwait(true);

            // Tear down
            await WpfTestSta.RunOnStaAsync(static () =>
            {
                FluenceThemeEngine.ResetForTesting();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
            }).ConfigureAwait(true);
        }
    }
}
