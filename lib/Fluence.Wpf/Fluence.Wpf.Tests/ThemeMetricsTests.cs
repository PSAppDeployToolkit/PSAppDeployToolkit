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
        private static void RunOnStaThread(Action action)
        {
            Exception? captured = null;
            WpfTestSta.Dispatcher?.Invoke(new Action(delegate
            {
                try { action(); }
                catch (Exception ex) { captured = ex; }
            }));

            if (captured is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        private static Application? EnsureApp()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static void ResetAndApply(ApplicationTheme theme, Application? app = null)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app?.Resources.MergedDictionaries.Clear();

            ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
        }

        // ---------------------------------------------------------------------------
        // ControlCornerRadius token
        // ---------------------------------------------------------------------------

        [Fact]
        public void ControlCornerRadius_PresentInLightTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                object? cr = app?.TryFindResource("ControlCornerRadius");
                Assert.NotNull(cr);
                Assert.Equal(new CornerRadius(4), (CornerRadius)cr);
            });
        }

        [Fact]
        public void ControlCornerRadius_PresentInDarkTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Dark, app);
                object? cr = app?.TryFindResource("ControlCornerRadius");
                Assert.NotNull(cr);
                Assert.Equal(new CornerRadius(4), (CornerRadius)cr);
            });
        }

        [Fact]
        public void ControlCornerRadius_PresentInHighContrastTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                object? cr = app?.TryFindResource("ControlCornerRadius");
                Assert.NotNull(cr);
                Assert.Equal(new CornerRadius(4), (CornerRadius)cr);
            });
        }

        // ---------------------------------------------------------------------------
        // OverlayCornerRadius token
        // ---------------------------------------------------------------------------

        [Fact]
        public void OverlayCornerRadius_PresentInLightTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                object? or_ = app?.TryFindResource("OverlayCornerRadius");
                Assert.NotNull(or_);
                Assert.Equal(new CornerRadius(8), (CornerRadius)or_);
            });
        }

        [Fact]
        public void OverlayCornerRadius_PresentInDarkTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Dark, app);
                object? or_ = app?.TryFindResource("OverlayCornerRadius");
                Assert.NotNull(or_);
                Assert.Equal(new CornerRadius(8), (CornerRadius)or_);
            });
        }

        [Fact]
        public void OverlayCornerRadius_PresentInHighContrastTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                object? or_ = app?.TryFindResource("OverlayCornerRadius");
                Assert.NotNull(or_);
                Assert.Equal(new CornerRadius(8), (CornerRadius)or_);
            });
        }

        // ---------------------------------------------------------------------------
        // FlyoutShadowEffect
        // ---------------------------------------------------------------------------

        [Fact]
        public void FlyoutShadowEffect_PresentInAllThemes()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ResetAndApply(theme, app);
                    object? fx = app?.TryFindResource("FlyoutShadowEffect");
                    Assert.NotNull(fx);
                    _ = Assert.IsAssignableFrom<DropShadowEffect>(fx);
                }
            });
        }

        [Fact]
        public void FlyoutShadowEffect_HasExpectedProperties()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                DropShadowEffect? fx = (DropShadowEffect?)app?.TryFindResource("FlyoutShadowEffect");
                Assert.NotNull(fx);
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
        public void DefaultControlFocusVisualStyle_PresentInAllThemes()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ResetAndApply(theme, app);
                    object? style = app?.TryFindResource("DefaultControlFocusVisualStyle");
                    Assert.NotNull(style);
                    _ = Assert.IsAssignableFrom<Style>(style);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Full theme cycle - tokens survive all three theme transitions
        // ---------------------------------------------------------------------------

        [Fact]
        public void CornerRadiusTokens_SurviveFullThemeCycle()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    object? cr = app?.TryFindResource("ControlCornerRadius");
                    object? or_ = app?.TryFindResource("OverlayCornerRadius");
                    Assert.NotNull(cr);
                    Assert.NotNull(or_);
                    Assert.Equal(new CornerRadius(4), (CornerRadius)cr);
                    Assert.Equal(new CornerRadius(8), (CornerRadius)or_);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // DefaultCollectionFocusVisualStyle token
        // ---------------------------------------------------------------------------

        [Fact]
        public void DefaultCollectionFocusVisualStyle_PresentInLightTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                object? style = app?.TryFindResource("DefaultCollectionFocusVisualStyle");
                Assert.NotNull(style);
                _ = Assert.IsAssignableFrom<Style>(style);
            });
        }

        [Fact]
        public void DefaultCollectionFocusVisualStyle_PresentInDarkTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Dark, app);
                object? style = app?.TryFindResource("DefaultCollectionFocusVisualStyle");
                Assert.NotNull(style);
            });
        }

        [Fact]
        public void DefaultCollectionFocusVisualStyle_PresentInHighContrastTheme()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                object? style = app?.TryFindResource("DefaultCollectionFocusVisualStyle");
                Assert.NotNull(style);
            });
        }
    }
}
