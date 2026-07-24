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

using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-5A.1 tests for the Fluent <see cref="ToolTip"/> control.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-5A.1 ToolTip
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ToolTip_DefaultStyle_BackgroundBrushResolves()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                object? brush = app?.TryFindResource("SolidBackgroundFillColorTertiaryBrush");
                Assert.IsNotNull(brush,
                    "SolidBackgroundFillColorTertiaryBrush (ToolTip background) must resolve after theme apply.");
            });
        }

        [TestMethod]
        public void ToolTip_DefaultStyle_BorderBrushResolves()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                object? brush = app?.TryFindResource("SurfaceStrokeColorFlyoutBrush");
                Assert.IsNotNull(brush,
                    "SurfaceStrokeColorFlyoutBrush (ToolTip border) must resolve after theme apply.");
            });
        }

        [TestMethod]
        public void ToolTip_DefaultStyle_StyleRegisteredWithCorrectProperties()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // Default style is keyed to the Fluence ToolTip type.
                Style? style = app?.TryFindResource(typeof(ToolTip)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.ToolTip.");

                // Apply manually so property setters are evaluated.
                ToolTip tt = new()
                {
                    Content = "Test",
                    Style = style,
                };

                // FontSize and MaxWidth are ordinary DPs - they resolve via Style.Apply.
                Assert.AreEqual(12.0, tt.FontSize, 0.01, "ToolTip.FontSize must be 12 per Fluent style.");
                Assert.AreEqual(320.0, tt.MaxWidth, 0.01, "ToolTip.MaxWidth must be 320 per Fluent style.");
                Assert.AreEqual(new Thickness(9, 6, 9, 8), tt.Padding, "ToolTip.Padding must be 9,6,9,8 per Fluent style.");
            });
        }

        [TestMethod]
        public void ToolTip_OpenFade_SettlesAtFullOpacity()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 300, Height = 200 };
                Button target = new() { Content = "Hover me" };
                ToolTip tip = new() { Content = "Tip body" };
                target.ToolTip = tip;

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.PlacementTarget = target;
                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.Template?.FindName("ToolTipSurface", tip) is System.Windows.Controls.Border),
                        "The tooltip template must apply once the tooltip opens.");

                    System.Windows.Controls.Border? surface =
                        tip.Template.FindName("ToolTipSurface", tip) as System.Windows.Controls.Border;
                    Assert.IsNotNull(surface, "ToolTipSurface must exist in the ToolTip template.");

                    // The 83 ms open fade (WinUI FadeInThemeAnimation parity) must settle at
                    // full opacity. The trigger-begun HoldEnd clock keeps
                    // HasAnimatedProperties true forever (see plan 011), so only the settled
                    // value is asserted.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => surface.Opacity >= 1.0),
                        "The open fade must settle at full opacity.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ToolTip_SystemPopupFade_IsSuppressedByThemeResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // The WPF tooltip pipeline resolves its host popup animation through this
                // system resource key, and the theme overrides it so the template
                // storyboard owns the single fade.
                object? animation = app?.TryFindResource(SystemParameters.ToolTipPopupAnimationKey);
                Assert.AreEqual(System.Windows.Controls.Primitives.PopupAnimation.None, animation,
                    "The theme must suppress the system tooltip popup fade in favor of the template's 83 ms fade.");
            });
        }

        [TestMethod]
        public void ToolTip_ThemeCycle_BrushesResolveAfterEachSwitch()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys = ["SolidBackgroundFillColorTertiaryBrush", "SurfaceStrokeColorFlyoutBrush", "TextFillColorPrimaryBrush"];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in ToolTip theme cycle step: {1}", key, theme));
                    }
                }
            });
        }
    }
}
