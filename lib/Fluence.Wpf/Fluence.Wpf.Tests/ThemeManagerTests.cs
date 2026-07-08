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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class ThemeManagerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            WpfTestSta.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            });
        }

        [TestMethod]
        public void Apply_Light_TextBrushIsDarkOnLight()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                Color? textColor = app.Resources["TextFillColorPrimary"] as Color?;
                Assert.IsNotNull(textColor, "TextFillColorPrimary should be defined");

                Assert.AreEqual((byte)0xE4, textColor.Value.A, "Alpha should be 0xE4");
                Assert.AreEqual((byte)0x00, textColor.Value.R, "Red should be 0x00");
                Assert.AreEqual((byte)0x00, textColor.Value.G, "Green should be 0x00");
                Assert.AreEqual((byte)0x00, textColor.Value.B, "Blue should be 0x00");
            });
        }

        [TestMethod]
        public void Apply_Dark_TextBrushIsLightOnDark()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);

                Color? textColor = app.Resources["TextFillColorPrimary"] as Color?;
                Assert.IsNotNull(textColor, "TextFillColorPrimary should be defined");

                Assert.AreEqual((byte)0xFF, textColor.Value.A, "Alpha should be 0xFF");
                Assert.AreEqual((byte)0xFF, textColor.Value.R, "Red should be 0xFF");
                Assert.AreEqual((byte)0xFF, textColor.Value.G, "Green should be 0xFF");
                Assert.AreEqual((byte)0xFF, textColor.Value.B, "Blue should be 0xFF");
            });
        }

        [TestMethod]
        public void Apply_HighContrast_UsesSystemColors()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);

                SolidColorBrush? brush = app.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
                Assert.IsNotNull(brush, "TextFillColorPrimaryBrush should be defined");
            });
        }

        [TestMethod]
        public void Apply_HighContrast_CloseButtonUsesSystemHighlight_NotBrandRed()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);

                SolidColorBrush? pointerOver = app.Resources["WindowCloseButtonBackgroundPointerOverBrush"] as SolidColorBrush;
                SolidColorBrush? pressed = app.Resources["WindowCloseButtonBackgroundPressedBrush"] as SolidColorBrush;
                SolidColorBrush? foreground = app.Resources["WindowCloseButtonForegroundPointerOverBrush"] as SolidColorBrush;

                Assert.IsNotNull(pointerOver, "WindowCloseButtonBackgroundPointerOverBrush should be defined");
                Assert.IsNotNull(pressed, "WindowCloseButtonBackgroundPressedBrush should be defined");
                Assert.IsNotNull(foreground, "WindowCloseButtonForegroundPointerOverBrush should be defined");

                Assert.AreEqual(SystemColors.HighlightColor, pointerOver.Color,
                    "Close button hover must use SystemColors.HighlightColor in High Contrast, not brand red.");
                Assert.AreEqual(SystemColors.HighlightColor, pressed.Color,
                    "Close button pressed must use SystemColors.HighlightColor in High Contrast, not brand red.");
                Assert.AreEqual(SystemColors.HighlightTextColor, foreground.Color,
                    "Close button foreground must use SystemColors.HighlightTextColor in High Contrast.");
            });
        }

        [TestMethod]
        public void Apply_FiresChangedExactlyOnce()
        {
            WpfTestSta.Invoke(() =>
            {
                int eventCount = 0;
                void handler(object? sender, ThemeChangedEventArgs e) { eventCount++; }

                ApplicationThemeManager.Changed += handler;
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                    Assert.AreEqual(1, eventCount, "Changed event should fire exactly once");
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                }
            });
        }

        [TestMethod]
        public void TwoRapidApplies_FiresExactlyTwoEvents()
        {
            WpfTestSta.Invoke(() =>
            {
                int eventCount = 0;
                void handler(object? sender, ThemeChangedEventArgs e) { eventCount++; }

                ApplicationThemeManager.Changed += handler;
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                    Assert.AreEqual(2, eventCount, "Changed event should fire exactly twice");
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                }
            });
        }

        [TestMethod]
        public void FiveSwitches_DictionaryCountStable()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                int initialCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);

                int finalCount = app.Resources.MergedDictionaries.Count;
                Assert.AreEqual(initialCount, finalCount, "Dictionary count should remain stable after multiple switches");
            });
        }

        [TestMethod]
        public void IsSystemInDarkMode_IsInverseOfRegistrySystemLight()
        {
            bool registryLight = Helpers.RegistryHelper.GetSystemUsesLightTheme();
            bool result = ApplicationThemeManager.IsSystemInDarkMode;
            Assert.AreEqual(!registryLight, result);
        }

        [TestMethod]
        public void IsAppInDarkMode_IsInverseOfRegistryAppsLight()
        {
            bool registryLight = Helpers.RegistryHelper.GetAppsUseLightTheme();
            bool result = ApplicationThemeManager.IsAppInDarkMode;
            Assert.AreEqual(!registryLight, result);
        }

        [TestMethod]
        public void Apply_Light_ResolvedThemeIsLight()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                Assert.AreEqual(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme must be Light after Apply(Light).");
            });
        }

        [TestMethod]
        public void Apply_Dark_ResolvedThemeIsDark()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                Assert.AreEqual(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme must be Dark after Apply(Dark).");
            });
        }

        [TestMethod]
        public void Apply_HighContrast_ResolvedThemeIsHighContrast()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);
                Assert.AreEqual(ApplicationTheme.HighContrast, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme must be HighContrast after Apply(HighContrast).");
            });
        }

        [TestMethod]
        public void Apply_ExplicitTheme_ResolvedThemeNeverReturnsAuto()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                Assert.AreNotEqual(ApplicationTheme.Auto, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme must never be Auto; it must always be a concrete resolved value.");
            });
        }

        [TestMethod]
        public void Apply_Auto_ResolvedThemeIsNotAuto()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.None, updateAccent: false);
                Assert.AreNotEqual(ApplicationTheme.Auto, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme must never be Auto; even when CurrentTheme is Auto it must resolve to a concrete value.");
            });
        }

        [TestMethod]
        public void ResolvedTheme_TracksLastAppliedTheme()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                Assert.AreEqual(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme should be Light after first Apply(Light).");

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                Assert.AreEqual(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme should update to Dark after Apply(Dark).");

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);
                Assert.AreEqual(ApplicationTheme.HighContrast, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme should update to HighContrast after Apply(HighContrast).");
            });
        }

        [TestMethod]
        public void ResolvedTheme_RemainsConsistentAfterAccentChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                ApplicationTheme themeBeforeAccent = ApplicationThemeManager.ResolvedTheme;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xFF, 0x00, 0x00));

                Assert.AreEqual(themeBeforeAccent, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme should remain the same concrete theme after an accent change via ApplyCustomAccent.");
                Assert.AreNotEqual(ApplicationTheme.Auto, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme must never be Auto after an accent pipeline run.");
            });
        }

        [TestMethod]
        public void ResolvedTheme_DefaultsToLight_BeforeFirstApply()
        {
            WpfTestSta.Invoke(static () =>
            {
                Assert.AreEqual(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme,
                    "ResolvedTheme must default to Light before the first theme pipeline run.");
            });
        }

    }
}
