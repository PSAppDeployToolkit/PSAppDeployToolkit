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

using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Fluence.Wpf.Controls;
using Xunit;

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

        [Fact]
        public Task ContextMenu_DefaultStyle_StyleRegisteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app?.TryFindResource(typeof(ContextMenu)));
            });
        }

        [Fact]
        public Task ContextMenu_DefaultStyle_BackgroundAndBorderBrushResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Assert.NotNull(app?.TryFindResource("SolidBackgroundFillColorTertiaryBrush"));
                Assert.NotNull(app?.TryFindResource("SurfaceStrokeColorFlyoutBrush"));
            });
        }

        [Fact]
        public Task ContextMenu_DefaultStyle_HasDropShadowSetterTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app?.TryFindResource(typeof(ContextMenu)));

                // HasDropShadow only activates when the Popup opens; verify the
                // Setter is present and declared True rather than applying the style
                // without a live popup (which returns the default value).
                bool found = style.Setters.OfType<Setter>().Any(s => s.Property == System.Windows.Controls.ContextMenu.HasDropShadowProperty && true.Equals(s.Value));
                Assert.True(found, "ContextMenu style must contain <Setter Property='HasDropShadow' Value='True'/>.");
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.2 MenuItem
        // ---------------------------------------------------------------------------

        [Fact]
        public Task MenuItem_DefaultStyle_StyleRegisteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app?.TryFindResource(typeof(MenuItem)));
            });
        }

        [Fact]
        public Task MenuItem_DefaultStyle_HoverBrushResolvesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Assert.NotNull(app?.TryFindResource("SubtleFillColorSecondaryBrush"));
                Assert.NotNull(app?.TryFindResource("SubtleFillColorTertiaryBrush"));
            });
        }

        [Fact]
        public Task MenuItem_DefaultStyle_FontSize14Async()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                MenuItem mi = new()
                {
                    Header = "Test",
                    Style = Assert.IsType<Style>(app?.TryFindResource(typeof(MenuItem))),
                };
                Assert.Equal(14.0, mi.FontSize, 0.01);
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.2 Theme cycle
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ContextMenu_ThemeCycle_BrushesResolveAfterEachSwitchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] keys =
                [
                    "SolidBackgroundFillColorTertiaryBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "DividerStrokeColorDefaultBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in keys)
                    {
                        Assert.NotNull(app?.TryFindResource(key));
                    }
                }
            });
        }
    }
}
