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
using System;
using System.Windows;
using System.Windows.Media.Effects;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Step 3.0 stability tests: CornerRadius tokens, FlyoutShadowEffect, and
    /// DefaultControlFocusVisualStyle must resolve in every theme.
    /// </summary>
    [TestClass]
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

            ApplicationThemeManager.Apply(theme, BackdropType.None, true);
        }

        // ---------------------------------------------------------------------------
        // ControlCornerRadius token
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ControlCornerRadius_PresentInLightTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                object? cr = app?.TryFindResource("ControlCornerRadius");
                Assert.IsNotNull(cr, "ControlCornerRadius must resolve in Light theme.");
                Assert.AreEqual(new CornerRadius(4), (CornerRadius)cr,
                    "ControlCornerRadius must equal CornerRadius(4) in Light theme.");
            });
        }

        [TestMethod]
        public void ControlCornerRadius_PresentInDarkTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Dark, app);
                object? cr = app?.TryFindResource("ControlCornerRadius");
                Assert.IsNotNull(cr, "ControlCornerRadius must resolve in Dark theme.");
                Assert.AreEqual(new CornerRadius(4), (CornerRadius)cr,
                    "ControlCornerRadius must equal CornerRadius(4) in Dark theme.");
            });
        }

        [TestMethod]
        public void ControlCornerRadius_PresentInHighContrastTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                object? cr = app?.TryFindResource("ControlCornerRadius");
                Assert.IsNotNull(cr, "ControlCornerRadius must resolve in HighContrast theme.");
                Assert.AreEqual(new CornerRadius(4), (CornerRadius)cr,
                    "ControlCornerRadius must equal CornerRadius(4) in HighContrast theme.");
            });
        }

        // ---------------------------------------------------------------------------
        // OverlayCornerRadius token
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void OverlayCornerRadius_PresentInLightTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                object? or_ = app?.TryFindResource("OverlayCornerRadius");
                Assert.IsNotNull(or_, "OverlayCornerRadius must resolve in Light theme.");
                Assert.AreEqual(new CornerRadius(8), (CornerRadius)or_,
                    "OverlayCornerRadius must equal CornerRadius(8) in Light theme.");
            });
        }

        [TestMethod]
        public void OverlayCornerRadius_PresentInDarkTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Dark, app);
                object? or_ = app?.TryFindResource("OverlayCornerRadius");
                Assert.IsNotNull(or_, "OverlayCornerRadius must resolve in Dark theme.");
                Assert.AreEqual(new CornerRadius(8), (CornerRadius)or_,
                    "OverlayCornerRadius must equal CornerRadius(8) in Dark theme.");
            });
        }

        [TestMethod]
        public void OverlayCornerRadius_PresentInHighContrastTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                object? or_ = app?.TryFindResource("OverlayCornerRadius");
                Assert.IsNotNull(or_, "OverlayCornerRadius must resolve in HighContrast theme.");
                Assert.AreEqual(new CornerRadius(8), (CornerRadius)or_,
                    "OverlayCornerRadius must equal CornerRadius(8) in HighContrast theme.");
            });
        }

        // ---------------------------------------------------------------------------
        // FlyoutShadowEffect
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void FlyoutShadowEffect_PresentInAllThemes()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ResetAndApply(theme, app);
                    object? fx = app?.TryFindResource("FlyoutShadowEffect");
                    Assert.IsNotNull(fx,
                        "FlyoutShadowEffect must resolve in theme: " + theme);
                    Assert.IsInstanceOfType(fx, typeof(DropShadowEffect),
                        "FlyoutShadowEffect must be a DropShadowEffect in theme: " + theme);
                }
            });
        }

        [TestMethod]
        public void FlyoutShadowEffect_HasExpectedProperties()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                DropShadowEffect? fx = (DropShadowEffect?)app?.TryFindResource("FlyoutShadowEffect");
                Assert.IsNotNull(fx);
                Assert.AreEqual(18.0, fx.BlurRadius, 0.01, "FlyoutShadowEffect.BlurRadius must be 18.");
                Assert.AreEqual(270.0, fx.Direction, 0.01, "FlyoutShadowEffect.Direction must be 270.");
                Assert.AreEqual(0.22, fx.Opacity, 0.01, "FlyoutShadowEffect.Opacity must be 0.22.");
                Assert.AreEqual(4.0, fx.ShadowDepth, 0.01, "FlyoutShadowEffect.ShadowDepth must be 4.");
            });
        }

        // ---------------------------------------------------------------------------
        // DefaultControlFocusVisualStyle
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void DefaultControlFocusVisualStyle_PresentInAllThemes()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ResetAndApply(theme, app);
                    object? style = app?.TryFindResource("DefaultControlFocusVisualStyle");
                    Assert.IsNotNull(style,
                        "DefaultControlFocusVisualStyle must resolve in theme: " + theme);
                    Assert.IsInstanceOfType(style, typeof(Style),
                        "DefaultControlFocusVisualStyle must be a Style in theme: " + theme);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Full theme cycle - tokens survive all three theme transitions
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void CornerRadiusTokens_SurviveFullThemeCycle()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    object? cr = app?.TryFindResource("ControlCornerRadius");
                    object? or_ = app?.TryFindResource("OverlayCornerRadius");
                    Assert.IsNotNull(cr, "ControlCornerRadius must survive theme switch to: " + theme);
                    Assert.IsNotNull(or_, "OverlayCornerRadius must survive theme switch to: " + theme);
                    Assert.AreEqual(new CornerRadius(4), (CornerRadius)cr,
                        "ControlCornerRadius value must be 4 after switch to: " + theme);
                    Assert.AreEqual(new CornerRadius(8), (CornerRadius)or_,
                        "OverlayCornerRadius value must be 8 after switch to: " + theme);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // DefaultCollectionFocusVisualStyle token
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void DefaultCollectionFocusVisualStyle_PresentInLightTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                object? style = app?.TryFindResource("DefaultCollectionFocusVisualStyle");
                Assert.IsNotNull(style,
                    "DefaultCollectionFocusVisualStyle must resolve in Light theme.");
                Assert.IsInstanceOfType(style, typeof(Style),
                    "DefaultCollectionFocusVisualStyle must be a Style.");
            });
        }

        [TestMethod]
        public void DefaultCollectionFocusVisualStyle_PresentInDarkTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Dark, app);
                object? style = app?.TryFindResource("DefaultCollectionFocusVisualStyle");
                Assert.IsNotNull(style,
                    "DefaultCollectionFocusVisualStyle must resolve in Dark theme.");
            });
        }

        [TestMethod]
        public void DefaultCollectionFocusVisualStyle_PresentInHighContrastTheme()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.HighContrast, app);
                object? style = app?.TryFindResource("DefaultCollectionFocusVisualStyle");
                Assert.IsNotNull(style,
                    "DefaultCollectionFocusVisualStyle must resolve in HighContrast theme.");
            });
        }
    }
}
