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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Step 3.0 stability tests: CornerRadius tokens, FlyoutShadowEffect, and
    /// DefaultControlFocusVisualStyle must resolve in every theme.
    /// </summary>
    public class ThemeMetricsTests
    {
        private static void ResetAndApply(ApplicationTheme theme, Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();

            ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
        }

        // ---------------------------------------------------------------------------
        // ControlCornerRadius token
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ControlCornerRadius_PresentInLightThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                CornerRadius cr = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("ControlCornerRadius"));
                Assert.Equal(new CornerRadius(4), cr);
            });
        }

        [Fact]
        public Task ControlCornerRadius_PresentInDarkThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Dark, app);
                CornerRadius cr = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("ControlCornerRadius"));
                Assert.Equal(new CornerRadius(4), cr);
            });
        }

        [Fact]
        public Task ControlCornerRadius_PresentInHighContrastThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                CornerRadius cr = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("ControlCornerRadius"));
                Assert.Equal(new CornerRadius(4), cr);
            });
        }

        // ---------------------------------------------------------------------------
        // OverlayCornerRadius token
        // ---------------------------------------------------------------------------

        [Fact]
        public Task OverlayCornerRadius_PresentInLightThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                CornerRadius or_ = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("OverlayCornerRadius"));
                Assert.Equal(new CornerRadius(8), or_);
            });
        }

        [Fact]
        public Task OverlayCornerRadius_PresentInDarkThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Dark, app);
                CornerRadius or_ = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("OverlayCornerRadius"));
                Assert.Equal(new CornerRadius(8), or_);
            });
        }

        [Fact]
        public Task OverlayCornerRadius_PresentInHighContrastThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                CornerRadius or_ = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("OverlayCornerRadius"));
                Assert.Equal(new CornerRadius(8), or_);
            });
        }

        // ---------------------------------------------------------------------------
        // FlyoutShadowEffect
        // ---------------------------------------------------------------------------

        [Fact]
        public Task FlyoutShadowEffect_PresentInAllThemesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ResetAndApply(theme, app);
                    _ = Assert.IsAssignableFrom<DropShadowEffect>(app.TryFindResource("FlyoutShadowEffect"));
                }
            });
        }

        [Fact]
        public Task FlyoutShadowEffect_HasExpectedPropertiesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                DropShadowEffect fx = Assert.IsAssignableFrom<DropShadowEffect>(app.TryFindResource("FlyoutShadowEffect"));
                Assert.Equal(18.0, fx.BlurRadius, 0.01);
                Assert.Equal(270.0, fx.Direction, 0.01);
                Assert.Equal(0.22, fx.Opacity, 0.01);
                Assert.Equal(4.0, fx.ShadowDepth, 0.01);
            });
        }

        // ---------------------------------------------------------------------------
        // DefaultControlFocusVisualStyle
        // ---------------------------------------------------------------------------

        [Fact]
        public Task DefaultControlFocusVisualStyle_PresentInAllThemesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ResetAndApply(theme, app);
                    _ = Assert.IsAssignableFrom<Style>(app.TryFindResource("DefaultControlFocusVisualStyle"));
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Full theme cycle - tokens survive all three theme transitions
        // ---------------------------------------------------------------------------

        [Fact]
        public Task CornerRadiusTokens_SurviveFullThemeCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    CornerRadius cr = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("ControlCornerRadius"));
                    CornerRadius or_ = Assert.IsAssignableFrom<CornerRadius>(app.TryFindResource("OverlayCornerRadius"));
                    Assert.Equal(new CornerRadius(4), cr);
                    Assert.Equal(new CornerRadius(8), or_);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // DefaultCollectionFocusVisualStyle token
        // ---------------------------------------------------------------------------

        [Fact]
        public Task DefaultCollectionFocusVisualStyle_PresentInLightThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                _ = Assert.IsAssignableFrom<Style>(app.TryFindResource("DefaultCollectionFocusVisualStyle"));
            });
        }

        [Fact]
        public Task DefaultCollectionFocusVisualStyle_PresentInDarkThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Dark, app);
                _ = Assert.IsAssignableFrom<Style>(app.TryFindResource("DefaultCollectionFocusVisualStyle"));
            });
        }

        [Fact]
        public Task DefaultCollectionFocusVisualStyle_PresentInHighContrastThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                _ = Assert.IsAssignableFrom<Style>(app.TryFindResource("DefaultCollectionFocusVisualStyle"));
            });
        }
    }
}
