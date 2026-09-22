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
    /// <summary>
    /// Contract tests for keys the 1.0 surface deliberately publishes or deliberately withholds:
    /// the two aliases kept for downstream consumers, and the dead keys removed at 1.0.
    /// </summary>
    public sealed class ResourceAliasTests : IAsyncLifetime
    {
        /// <inheritdoc />
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return default;
        }

        /// <summary>
        /// The nine Color keys removed at 1.0, and their nine Brush twins, must not resolve in
        /// any theme. A key that quietly came back would be frozen by the next release.
        /// </summary>
        /// <param name="theme">The theme to apply before probing.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        [InlineData(ApplicationTheme.HighContrast)]
        public Task RemovedKeys_DoNotResolveInAnyThemeAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = TestApp.EnsureLibraryTheme(theme);

                string[] removed =
                [
                    "WindowCloseFillColorHover",
                    "WindowCloseFillColorPressed",
                    "WindowCloseForegroundHover",
                    "WindowCloseForegroundPressed",
                    "ControlStrokeColorTertiary",
                    "SystemFillColorInformational",
                    "KeyboardFocusBorderColor",
                    "NavigationViewContentSeparator",
                    "TextPlaceholderColor",
                ];

                foreach (string key in removed)
                {
                    Assert.Null(app.TryFindResource(key));
                    Assert.Null(app.TryFindResource(key + "Brush"));
                }

                // The WinUI-named caption button keys that replaced the WindowClose* pair must
                // still resolve, so the removal did not take the live ones with it.
                Assert.NotNull(app.TryFindResource("WindowCloseButtonBackgroundPointerOverBrush"));
                Assert.NotNull(app.TryFindResource("WindowCloseButtonBackgroundPressedBrush"));
                Assert.NotNull(app.TryFindResource("WindowCloseButtonForegroundPointerOverBrush"));
                Assert.NotNull(app.TryFindResource("SystemFillColorAttentionBrush"));
                Assert.NotNull(app.TryFindResource("FocusStrokeColorOuterBrush"));
            });
        }

        /// <summary>
        /// ContentControlThemeFontFamily is the WinUI name for the family FluentFontFamily
        /// carries. Both ship for the life of 1.x: PSADT binds FluentFontFamily with
        /// DynamicResource, where a rename would fail silently. This test is also the guard that
        /// keeps the two literals in Typography.xaml in step.
        /// </summary>
        [Fact]
        public Task FontFamilyAlias_ResolvesToTheSameFamilyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = TestApp.EnsureLibraryTheme();

                FontFamily legacy = Assert.IsType<FontFamily>(app.TryFindResource("FluentFontFamily"));
                FontFamily winui = Assert.IsType<FontFamily>(app.TryFindResource("ContentControlThemeFontFamily"));
                Assert.Equal(legacy.Source, winui.Source, StringComparer.Ordinal);
            });
        }

        /// <summary>
        /// ApplicationPageBackgroundThemeBrush is the WinUI name for the brush
        /// ApplicationBackgroundBrush carries, and both must be the same instance in every theme.
        /// </summary>
        /// <param name="theme">The theme to apply before probing.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        [InlineData(ApplicationTheme.HighContrast)]
        public Task BackgroundBrushAlias_ResolvesToTheSameBrushAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = TestApp.EnsureLibraryTheme(theme);

                SolidColorBrush legacy = Assert.IsType<SolidColorBrush>(app.TryFindResource("ApplicationBackgroundBrush"));
                SolidColorBrush winui = Assert.IsType<SolidColorBrush>(app.TryFindResource("ApplicationPageBackgroundThemeBrush"));
                Assert.Same(legacy, winui);
                Assert.Equal(legacy.Color, winui.Color);

                // The Color key behind them both keeps shipping too.
                _ = Assert.IsType<Color>(app.TryFindResource("ApplicationBackgroundColor"));
            });
        }
    }
}
