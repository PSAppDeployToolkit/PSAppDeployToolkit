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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public class ThemeManagerTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            }));
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return default;
        }

        [Theory]
        [InlineData(ApplicationTheme.Light, 0xE4, 0x00, 0x00, 0x00)]
        [InlineData(ApplicationTheme.Dark, 0xFF, 0xFF, 0xFF, 0xFF)]
        public Task Apply_Theme_TextFillColorPrimaryMatchesAsync(ApplicationTheme theme, byte a, byte r, byte g, byte b)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);

                Color textColor = Assert.IsType<Color>(app.Resources["TextFillColorPrimary"]);

                Assert.Equal(a, textColor.A);
                Assert.Equal(r, textColor.R);
                Assert.Equal(g, textColor.G);
                Assert.Equal(b, textColor.B);
            });
        }

        [Fact]
        public Task Apply_HighContrast_UsesSystemColorsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);

                SolidColorBrush brush = Assert.IsType<SolidColorBrush>(app.Resources["TextFillColorPrimaryBrush"]);
            });
        }

        [Fact]
        public Task Apply_HighContrast_CloseButtonUsesSystemHighlight_NotBrandRedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);

                SolidColorBrush pointerOver = Assert.IsType<SolidColorBrush>(app.Resources["WindowCloseButtonBackgroundPointerOverBrush"]);
                SolidColorBrush pressed = Assert.IsType<SolidColorBrush>(app.Resources["WindowCloseButtonBackgroundPressedBrush"]);
                SolidColorBrush foreground = Assert.IsType<SolidColorBrush>(app.Resources["WindowCloseButtonForegroundPointerOverBrush"]);


                Assert.Equal(SystemColors.HighlightColor, pointerOver.Color);
                Assert.Equal(SystemColors.HighlightColor, pressed.Color);
                Assert.Equal(SystemColors.HighlightTextColor, foreground.Color);
            });
        }

        [Fact]
        public Task Apply_FiresChangedExactlyOnceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                int eventCount = 0;
                void handler(object? sender, ThemeChangedEventArgs e) { eventCount++; }

                ApplicationThemeManager.Changed += handler;
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    Assert.Equal(1, eventCount);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                }
            });
        }

        [Fact]
        public Task TwoRapidApplies_FiresExactlyTwoEventsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                int eventCount = 0;
                void handler(object? sender, ThemeChangedEventArgs e) { eventCount++; }

                ApplicationThemeManager.Changed += handler;
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    Assert.Equal(2, eventCount);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                }
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
        public Task Apply_ExplicitTheme_ResolvedThemeMatchesAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                Assert.Equal(theme, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Auto)]
        public Task Apply_ResolvedThemeNeverReturnsAutoAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                Assert.NotEqual(ApplicationTheme.Auto, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Fact]
        public Task ResolvedTheme_TracksLastAppliedThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                Assert.Equal(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme);

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                Assert.Equal(ApplicationTheme.HighContrast, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Fact]
        public Task ResolvedTheme_RemainsConsistentAfterAccentChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                ApplicationTheme themeBeforeAccent = ApplicationThemeManager.ResolvedTheme;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xFF, 0x00, 0x00));

                Assert.Equal(themeBeforeAccent, ApplicationThemeManager.ResolvedTheme);
                Assert.NotEqual(ApplicationTheme.Auto, ApplicationThemeManager.ResolvedTheme);
            });
        }

        [Fact]
        public Task ResolvedTheme_DefaultsToLight_BeforeFirstApplyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme));
        }

    }
}
