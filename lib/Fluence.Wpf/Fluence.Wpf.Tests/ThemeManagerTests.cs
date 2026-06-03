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
            WpfTestSta.Invoke(() =>
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
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);

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
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);

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
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, false);

                SolidColorBrush? brush = app.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
                Assert.IsNotNull(brush, "TextFillColorPrimaryBrush should be defined");
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
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
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
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
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
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                int initialCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);

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

    }
}
