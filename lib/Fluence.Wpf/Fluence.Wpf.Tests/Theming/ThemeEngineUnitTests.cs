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

using Fluence.Wpf.Theming;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Unit tests for Stage-1 engine classes: <see cref="ColorMap"/>, <see cref="BrushFactory"/>,
    /// <see cref="BaseColorTables"/>, and <see cref="FluenceThemeEngine"/> isolation smoke tests.
    /// These tests exercise every engine class directly so that unused-member diagnostics do not
    /// fire during Stage 1 (before the engine is wired to the public facades).
    /// </summary>
    [TestClass]
    public class ThemeEngineUnitTests
    {
        private static readonly Color TestBlue = Color.FromRgb(0x00, 0x78, 0xD4);

        private static AccentPalette MakeTestPalette()
        {
            return AccentResolver.Resolve(AccentIntent.FromCustom(TestBlue));
        }

        // ------------------------------------------------------------------ ColorMap --

        /// <summary>
        /// ColorMap.Build for Light must contain the core accent-derived keys with non-default colors.
        /// </summary>
        [TestMethod]
        public void ColorMap_Build_Light_ContainsAccentFillColorDefault()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.Light, p);
                Assert.IsTrue(m.ContainsKey("AccentFillColorDefault"), "AccentFillColorDefault must be present in Light.");
                Assert.AreNotEqual(default, m["AccentFillColorDefault"], "AccentFillColorDefault must be non-default.");
            });
        }

        /// <summary>
        /// ColorMap.Build for Dark must contain the core accent-derived keys.
        /// </summary>
        [TestMethod]
        public void ColorMap_Build_Dark_ContainsAccentFillColorDefault()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.Dark, p);
                Assert.IsTrue(m.ContainsKey("AccentFillColorDefault"), "AccentFillColorDefault must be present in Dark.");
            });
        }

        /// <summary>
        /// ColorMap.Build must emit the SystemAccentColor raw-ramp keys.
        /// </summary>
        [TestMethod]
        public void ColorMap_Build_Light_ContainsRawRampKeys()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.Light, p);
                Assert.IsTrue(m.ContainsKey("SystemAccentColor"), "SystemAccentColor missing.");
                Assert.IsTrue(m.ContainsKey("SystemAccentColorLight1"), "SystemAccentColorLight1 missing.");
                Assert.IsTrue(m.ContainsKey("SystemAccentColorDark3"), "SystemAccentColorDark3 missing.");
                Assert.AreEqual(TestBlue, m["SystemAccentColor"], "SystemAccentColor must equal the base accent.");
            });
        }

        /// <summary>
        /// ColorMap.Build must emit TitleBarActiveColor.
        /// </summary>
        [TestMethod]
        public void ColorMap_Build_Light_ContainsTitleBarActiveColor()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.Light, p);
                Assert.IsTrue(m.ContainsKey("TitleBarActiveColor"), "TitleBarActiveColor must be present.");
            });
        }

        /// <summary>
        /// ColorMap.Build for HighContrast must not emit AccentFillColorDisabled from the C# path
        /// (the HC guard skips both AccentFillColorDisabled and SystemFillColorAttention overrides).
        /// The test verifies the HC build produces a result and contains SystemAccentColor.
        /// </summary>
        [TestMethod]
        public void ColorMap_Build_HighContrast_ContainsSystemAccentColor()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AccentPalette p = MakeTestPalette();
                Dictionary<string, Color> m = ColorMap.Build(ApplicationTheme.HighContrast, p);
                Assert.IsGreaterThan(5, m.Count, "ColorMap.Build(HighContrast) must return a non-trivial map.");
                Assert.IsTrue(m.ContainsKey("SystemAccentColor"), "SystemAccentColor must be present even in HC.");
                // The HC C# path does NOT inject AccentFillColorDisabled or SystemFillColorAttention
                // via the guard; those may come from base XAML instead.
                Assert.AreEqual(TestBlue, m["SystemAccentColor"], "SystemAccentColor must equal the pinned custom accent.");
            });
        }

        // ------------------------------------------------------------------ BrushFactory --

        /// <summary>
        /// BrushFactory.Build must produce a frozen SolidColorBrush for each color key.
        /// </summary>
        [TestMethod]
        public void BrushFactory_Build_ProducesFrozenBrushForColorKey()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> colors = new(StringComparer.Ordinal)
                {
                    ["TestColorA"] = Colors.Red,
                    ["TestColorB"] = Color.FromArgb(0x80, 0x00, 0x80, 0xFF),
                };
                ResourceDictionary d = BrushFactory.Build(colors);

                Assert.IsTrue(d.Contains("TestColorA"), "Color key must be present.");
                Assert.IsTrue(d.Contains("TestColorABrush"), "Brush key must be present.");
                SolidColorBrush brush = (SolidColorBrush)d["TestColorABrush"];
                Assert.AreEqual(Colors.Red, brush.Color, "Brush color must match input.");
                Assert.IsTrue(brush.IsFrozen, "Brush must be frozen.");
            });
        }

        /// <summary>
        /// BrushFactory.Build must emit both the Color token and the Brush twin for semi-transparent colors.
        /// </summary>
        [TestMethod]
        public void BrushFactory_Build_PreservesAlphaChannel()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Color semiTransparent = Color.FromArgb(0x80, 0xFF, 0x00, 0x00);
                Dictionary<string, Color> colors = new(StringComparer.Ordinal) { ["AlphaColor"] = semiTransparent };
                ResourceDictionary d = BrushFactory.Build(colors);

                Color stored = (Color)d["AlphaColor"];
                Assert.AreEqual(semiTransparent, stored, "Color token must preserve alpha.");
                SolidColorBrush brush = (SolidColorBrush)d["AlphaColorBrush"];
                Assert.AreEqual((byte)0x80, brush.Color.A, "Brush alpha must match.");
            });
        }

        // ------------------------------------------------------------------ BaseColorTables --

        /// <summary>
        /// BaseColorTables.Load for Light must return a non-empty map containing known tokens.
        /// </summary>
        [TestMethod]
        public void BaseColorTables_Load_Light_ReturnsColors()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> m = BaseColorTables.Load(ApplicationTheme.Light);
                Assert.IsGreaterThan(10, m.Count, "BaseColorTables.Load(Light) must return many color entries.");
                Assert.IsTrue(m.ContainsKey("TextFillColorPrimary"), "TextFillColorPrimary must be present.");
            });
        }

        /// <summary>
        /// BaseColorTables.Load for Dark must return entries distinct from Light where expected.
        /// </summary>
        [TestMethod]
        public void BaseColorTables_Load_Dark_ReturnsColors()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> m = BaseColorTables.Load(ApplicationTheme.Dark);
                Assert.IsGreaterThan(10, m.Count, "BaseColorTables.Load(Dark) must return many color entries.");
            });
        }

        /// <summary>
        /// BaseColorTables.Load for HighContrast must return entries.
        /// </summary>
        [TestMethod]
        public void BaseColorTables_Load_HighContrast_ReturnsColors()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> m = BaseColorTables.Load(ApplicationTheme.HighContrast);
                Assert.IsGreaterThan(5, m.Count, "BaseColorTables.Load(HighContrast) must return color entries.");
            });
        }

        // ------------------------------------------------------------------ FluenceThemeEngine smoke --

        /// <summary>
        /// FluenceThemeEngine.Apply must publish a computed dictionary into application resources
        /// and populate ResolvedTheme and CurrentPalette. The test resets state before and after
        /// to avoid leaking into other fixtures.
        /// </summary>
        [TestMethod]
        public void FluenceThemeEngine_Apply_PublishesResourcesAndSetsState()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                Application app = WpfTestSta.EnsureApplication()!;
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();

                FluenceThemeEngine.SetAccentIntent(AccentIntent.FromCustom(TestBlue));
                FluenceThemeEngine.Apply(ApplicationTheme.Light);

                Assert.AreEqual(ApplicationTheme.Light, FluenceThemeEngine.ResolvedTheme, "ResolvedTheme must be Light.");
                Assert.AreEqual(TestBlue, FluenceThemeEngine.CurrentPalette.Accent, "Palette accent must match custom color.");
                Assert.IsGreaterThan(0, app.Resources.MergedDictionaries.Count, "MergedDictionaries must be non-empty after Apply.");

                // Verify at least one color key is present in the published dictionary
                object? accentFill = app.Resources.MergedDictionaries[0]["AccentFillColorDefault"];
                Assert.IsNotNull(accentFill, "AccentFillColorDefault must be present in the computed slot.");
            });

            // Tear down after the smoke test so other fixtures see a clean state
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                Application.Current!.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();
                Helpers.AcrylicNoiseHelper.ResetForTesting();
            });
        }

        /// <summary>
        /// FluenceThemeEngine.Apply called twice must replace slot [0] rather than inserting a second entry.
        /// </summary>
        [TestMethod]
        public void FluenceThemeEngine_Apply_ReplacesComputedSlotOnSecondCall()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                Application app = WpfTestSta.EnsureApplication()!;
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();

                FluenceThemeEngine.SetAccentIntent(AccentIntent.FromCustom(TestBlue));
                FluenceThemeEngine.Apply(ApplicationTheme.Light);
                int countAfterFirst = app.Resources.MergedDictionaries.Count;
                ResourceDictionary slotZeroFirst = app.Resources.MergedDictionaries[0];

                FluenceThemeEngine.Apply(ApplicationTheme.Dark);
                int countAfterSecond = app.Resources.MergedDictionaries.Count;
                ResourceDictionary slotZeroSecond = app.Resources.MergedDictionaries[0];

                Assert.AreEqual(countAfterFirst, countAfterSecond, "Dictionary count must not grow on second Apply.");
                Assert.AreNotSame(slotZeroFirst, slotZeroSecond, "Slot [0] must be replaced (not same reference) on second Apply.");
                Assert.AreEqual(ApplicationTheme.Dark, FluenceThemeEngine.ResolvedTheme, "ResolvedTheme must be Dark after second Apply.");
            });

            // Tear down
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                Application.Current!.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.ResetForTesting();
                Helpers.AcrylicNoiseHelper.ResetForTesting();
            });
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
        [TestMethod]
        public void FluenceThemeEngine_Apply_DoesNotRaisePublished_WhenApplicationIsNull()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                Application app = WpfTestSta.EnsureApplication()!;
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
                    Assert.IsNull(Application.Current, "Test precondition: Application.Current must be null.");

                    FluenceThemeEngine.Apply(ApplicationTheme.Light);
                }
                finally
                {
                    appInstanceField.SetValue(obj: null, value: app);
                    FluenceThemeEngine.Published -= OnPublished;
                }

                Assert.AreEqual(0, raised, "Published must not fire when Application.Current is null.");
                Assert.IsNotNull(Application.Current, "Application.Current must be restored for subsequent tests.");
                Assert.AreSame(app, Application.Current, "The restored Application must be the same shared instance.");
            });

            // Tear down
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                FluenceThemeEngine.ResetForTesting();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
            });
        }
    }
}
