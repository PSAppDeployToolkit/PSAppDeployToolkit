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
    /// WI-5A.2 tests for Fluent <see cref="ContextMenu"/> and <see cref="MenuItem"/>.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-5A.2 ContextMenu
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ContextMenu_DefaultStyle_StyleRegistered()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(ContextMenu)) as Style;
                Assert.IsNotNull(style,
                    "A default Style must be registered for Fluence.Wpf.Controls.ContextMenu.");
            });
        }

        [TestMethod]
        public void ContextMenu_DefaultStyle_BackgroundAndBorderBrushResolve()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Assert.IsNotNull(app?.TryFindResource("SolidBackgroundFillColorTertiaryBrush"),
                    "SolidBackgroundFillColorTertiaryBrush (ContextMenu background) must resolve.");
                Assert.IsNotNull(app?.TryFindResource("SurfaceStrokeColorFlyoutBrush"),
                    "SurfaceStrokeColorFlyoutBrush (ContextMenu border) must resolve.");
            });
        }

        [TestMethod]
        public void ContextMenu_DefaultStyle_HasDropShadowSetterTrue()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(ContextMenu)) as Style;
                Assert.IsNotNull(style);

                // HasDropShadow only activates when the Popup opens; verify the
                // Setter is present and declared True rather than applying the style
                // without a live popup (which returns the default value).
                bool found = false;
                foreach (SetterBase? setter in style.Setters)
                {
                    if (setter is Setter s && s.Property == System.Windows.Controls.ContextMenu.HasDropShadowProperty
                        && true.Equals(s.Value))
                    {
                        found = true;
                        break;
                    }
                }
                Assert.IsTrue(found,
                    "ContextMenu style must contain <Setter Property='HasDropShadow' Value='True'/>.");
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.2 MenuItem
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void MenuItem_DefaultStyle_StyleRegistered()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(MenuItem)) as Style;
                Assert.IsNotNull(style,
                    "A default Style must be registered for Fluence.Wpf.Controls.MenuItem.");
            });
        }

        [TestMethod]
        public void MenuItem_DefaultStyle_HoverBrushResolves()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Assert.IsNotNull(app?.TryFindResource("SubtleFillColorSecondaryBrush"),
                    "SubtleFillColorSecondaryBrush (MenuItem hover) must resolve.");
                Assert.IsNotNull(app?.TryFindResource("SubtleFillColorTertiaryBrush"),
                    "SubtleFillColorTertiaryBrush (MenuItem pressed) must resolve.");
            });
        }

        [TestMethod]
        public void MenuItem_DefaultStyle_FontSize14()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                MenuItem mi = new() { Header = "Test" };
                Style? style = app?.TryFindResource(typeof(MenuItem)) as Style;
                Assert.IsNotNull(style);
                mi.Style = style;
                Assert.AreEqual(14.0, mi.FontSize, 0.01,
                    "MenuItem.FontSize must be 14 per Fluent style.");
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.2 Theme cycle
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ContextMenu_ThemeCycle_BrushesResolveAfterEachSwitch()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] keys =
                [
                    "SolidBackgroundFillColorTertiaryBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "DividerStrokeColorDefaultBrush"
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in keys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in ContextMenu theme cycle step: {1}", key, theme));
                    }
                }
            });
        }
    }
}
