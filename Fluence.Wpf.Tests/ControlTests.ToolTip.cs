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
using Fluence.Wpf.Controls;
using Xunit;

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

        [Fact]
        public Task ToolTip_DefaultStyle_BackgroundBrushResolvesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                object brush = Assert.IsAssignableFrom<object>(app?.TryFindResource("SolidBackgroundFillColorTertiaryBrush"));
            });
        }

        [Fact]
        public Task ToolTip_DefaultStyle_BorderBrushResolvesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                object brush = Assert.IsAssignableFrom<object>(app?.TryFindResource("SurfaceStrokeColorFlyoutBrush"));
            });
        }

        [Fact]
        public Task ToolTip_DefaultStyle_StyleRegisteredWithCorrectPropertiesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                // Default style is keyed to the Fluence ToolTip type.
                Style style = Assert.IsType<Style>(app?.TryFindResource(typeof(ToolTip)));

                // Apply manually so property setters are evaluated.
                ToolTip tt = new()
                {
                    Content = "Test",
                    Style = style,
                };

                // FontSize and MaxWidth are ordinary DPs - they resolve via Style.Apply.
                Assert.Equal(12.0, tt.FontSize, 0.01);
                Assert.Equal(320.0, tt.MaxWidth, 0.01);
                Assert.Equal(new Thickness(9, 6, 9, 8), tt.Padding);
            });
        }

        [Fact]
        public Task ToolTip_OpenFade_SettlesAtFullOpacityAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 300, Height = 200 };
                Button target = new() { Content = "Hover me" };
                ToolTip tip = new() { Content = "Tip body" };
                target.ToolTip = tip;

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.PlacementTarget = target;
                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => tip.Template?.FindName("ToolTipSurface", tip) is System.Windows.Controls.Border).ConfigureAwait(true),
                        "The tooltip template must apply once the tooltip opens.");

                    System.Windows.Controls.Border surface =
                        Assert.IsType<System.Windows.Controls.Border>(tip.Template.FindName("ToolTipSurface", tip));

                    // The 83 ms open fade (WinUI FadeInThemeAnimation parity) must settle at
                    // full opacity. The trigger-begun HoldEnd clock keeps
                    // HasAnimatedProperties true forever (see plan 011), so only the settled
                    // value is asserted.
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => surface.Opacity >= 1.0).ConfigureAwait(true),
                        "The open fade must settle at full opacity.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ToolTip_SystemPopupFade_IsSuppressedByThemeResourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                // The WPF tooltip pipeline resolves its host popup animation through this
                // system resource key, and the theme overrides it so the template
                // storyboard owns the single fade.
                object? animation = app?.TryFindResource(SystemParameters.ToolTipPopupAnimationKey);
                Assert.Equal(System.Windows.Controls.Primitives.PopupAnimation.None, animation);
            });
        }

        [Fact]
        public Task ToolTip_ThemeCycle_BrushesResolveAfterEachSwitchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys = ["SolidBackgroundFillColorTertiaryBrush", "SurfaceStrokeColorFlyoutBrush", "TextFillColorPrimaryBrush"];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.NotNull(app?.TryFindResource(key));
                    }
                }
            });
        }
    }
}
