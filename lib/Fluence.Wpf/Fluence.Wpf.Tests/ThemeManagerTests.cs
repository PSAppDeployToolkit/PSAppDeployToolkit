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

using System.Windows;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class ThemeManagerTests
    {
        public ThemeManagerTests()
        {
            WpfTestSta.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            });
        }

        [Theory]
        [InlineData(ApplicationTheme.Light, 0xE4, 0x00, 0x00, 0x00)]
        [InlineData(ApplicationTheme.Dark, 0xFF, 0xFF, 0xFF, 0xFF)]
        public void Apply_Theme_TextFillColorPrimaryMatches(ApplicationTheme theme, byte a, byte r, byte g, byte b)
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: false);

                Color? textColor = app.Resources["TextFillColorPrimary"] as Color?;
                _ = Assert.NotNull(textColor);

                Assert.Equal(a, textColor.Value.A);
                Assert.Equal(r, textColor.Value.R);
                Assert.Equal(g, textColor.Value.G);
                Assert.Equal(b, textColor.Value.B);
            });
        }

        [Fact]
        public void Apply_HighContrast_UsesSystemColors()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);

                SolidColorBrush? brush = app.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
                Assert.NotNull(brush);
            });
        }

        [Fact]
        public void Apply_HighContrast_CloseButtonUsesSystemHighlight_NotBrandRed()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);

                SolidColorBrush? pointerOver = app.Resources["WindowCloseButtonBackgroundPointerOverBrush"] as SolidColorBrush;
                SolidColorBrush? pressed = app.Resources["WindowCloseButtonBackgroundPressedBrush"] as SolidColorBrush;
                SolidColorBrush? foreground = app.Resources["WindowCloseButtonForegroundPointerOverBrush"] as SolidColorBrush;

                Assert.NotNull(pointerOver);
                Assert.NotNull(pressed);
                Assert.NotNull(foreground);

                Assert.Equal(SystemColors.HighlightColor, pointerOver.Color);
                Assert.Equal(SystemColors.HighlightColor, pressed.Color);
                Assert.Equal(SystemColors.HighlightTextColor, foreground.Color);
            });
        }

        [Fact]
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
                    Assert.Equal(1, eventCount);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                }
            });
        }

        [Fact]
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
                    Assert.Equal(2, eventCount);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                }
            });
        }

        [Fact]
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
                Assert.Equal(initialCount, finalCount);
            });
        }

        [Fact]
        public void IsSystemInDarkMode_IsInverseOfRegistrySystemLight()
        {
            bool registryLight = Helpers.RegistryHelper.GetSystemUsesLightTheme();
            bool result = ApplicationThemeManager.IsSystemInDarkMode;
            Assert.Equal(!registryLight, result);
        }

        [Fact]
        public void IsAppInDarkMode_IsInverseOfRegistryAppsLight()
        {
            bool registryLight = Helpers.RegistryHelper.GetAppsUseLightTheme();
            bool result = ApplicationThemeManager.IsAppInDarkMode;
            Assert.Equal(!registryLight, result);
        }

        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        [InlineData(ApplicationTheme.HighContrast)]
        public void Apply_ExplicitTheme_ResolvedThemeMatches(ApplicationTheme theme)
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: false);
                Assert.Equal(theme, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Auto)]
        public void Apply_ResolvedThemeNeverReturnsAuto(ApplicationTheme theme)
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: false);
                Assert.NotEqual(ApplicationTheme.Auto, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Fact]
        public void ResolvedTheme_TracksLastAppliedTheme()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                Assert.Equal(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme);

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);
                Assert.Equal(ApplicationTheme.HighContrast, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Fact]
        public void ResolvedTheme_RemainsConsistentAfterAccentChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                ApplicationTheme themeBeforeAccent = ApplicationThemeManager.ResolvedTheme;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xFF, 0x00, 0x00));

                Assert.Equal(themeBeforeAccent, ApplicationThemeManager.ResolvedTheme);
                Assert.NotEqual(ApplicationTheme.Auto, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Fact]
        public void ResolvedTheme_DefaultsToLight_BeforeFirstApply()
        {
            WpfTestSta.Invoke(static () =>
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme));
        }

    }
}
