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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.FluentButtonQueries;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class ButtonTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        [Fact]
        public Task Button_FontProperties_ReachTheContentTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    // The scoped TextBlock style extends the implicit one, which sets FontFamily,
                    // FontSize and FontWeight, and a Setter beats the value the control would
                    // otherwise inherit down. Without all three rebinds the style silently wins and
                    // a consumer's declared font never renders.
                    FontFamily declared = new("Consolas");
                    Controls.Button button = new()
                    {
                        Content = "Weighted",
                        FontFamily = declared,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                    };
                    window.Content = button;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock text = Assert.IsType<TextBlock>(FindVisualChild<TextBlock>(button), exactMatch: false);

                    Assert.Equal(FontWeights.Bold, text.FontWeight);
                    Assert.Equal(20.0, text.FontSize);
                    Assert.Equal(declared.Source, text.FontFamily.Source, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_AccentDisabled_DarkTheme_UsesVisibleDisabledAccentTokensAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                AssertDisabledAccentButtonUsesDarkTokens();
            });
        }

        [Fact]
        public Task Button_AccentDisabled_DarkThemeWithoutAccentRefresh_UsesVisibleDisabledAccentTokensAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);

                AssertDisabledAccentButtonUsesDarkTokens();
            });
        }

        [Fact]
        public Task Button_RestFill_InsetByStrokeExceptForAccentAsync()
        {
            // WinUI DefaultButtonStyle is BackgroundSizing=InnerBorderEdge (fill stops at the stroke's inner edge,
            // so the stroke composites over the surface); AccentButtonStyle is OuterBorderEdge (fill under the stroke).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.Button standard = new() { Width = 120, Content = "Standard" };
                Controls.Button accent = new() { Width = 120, Content = "Accent", Appearance = ControlAppearance.Accent };
                StackPanel panel = new();
                _ = panel.Children.Add(standard);
                _ = panel.Children.Add(accent);

                try
                {
                    window.Content = panel;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border standardFill = Assert.IsType<Border>(standard.Template.FindName("RestFill", standard));
                    Border standardStroke = Assert.IsType<Border>(standard.Template.FindName("OuterBorder", standard));
                    Assert.Equal(standardStroke.BorderThickness, standardFill.BorderThickness);
                    Assert.Equal(new Thickness(1), standardFill.BorderThickness);
                    Assert.Null(standardFill.BorderBrush);

                    Border accentFill = Assert.IsType<Border>(accent.Template.FindName("RestFill", accent));
                    Assert.Equal(new Thickness(0), accentFill.BorderThickness);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_ExplicitToolTip_IsNotClearedByTruncationFallbackAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ToolTip toolTip = new() { Content = "Save changes" };
                Controls.Button button = new()
                {
                    Width = 160,
                    Content = "Save",
                    ToolTip = toolTip,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Same(toolTip, button.ToolTip);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_IconOnly_CentersGlyphAndRestoresGapWithContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.Button button = new()
                {
                    MinWidth = 32,
                    Padding = new Thickness(8, 4, 8, 4),
                    Icon = new Controls.FontIcon { Glyph = "\uE8C8", IconFontSize = 14 },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter iconPresenter = FindVisualChildByName<ContentPresenter>(button, "IconPresenter")
                        ?? throw new InvalidOperationException("IconPresenter must exist in the Button template.");
                    Assert.Equal(new Thickness(0), iconPresenter.Margin);

                    Point iconCenter = iconPresenter.TranslatePoint(
                        new Point(iconPresenter.ActualWidth / 2.0, iconPresenter.ActualHeight / 2.0), button);
                    Assert.Equal(button.ActualWidth / 2.0, iconCenter.X, 1.0);

                    button.Content = "Copy";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(new Thickness(0, 0, 8, 0), iconPresenter.Margin);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_Appearances_ApplyWinUiRestBrushesAndBordersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Controls.Button standard = new() { Content = "Standard" };
                Controls.Button accent = new() { Appearance = ControlAppearance.Accent, Content = "Accent" };
                Controls.Button subtle = new() { Appearance = ControlAppearance.Subtle, Content = "Subtle" };
                StackPanel panel = new();
                _ = panel.Children.Add(standard);
                _ = panel.Children.Add(accent);
                _ = panel.Children.Add(subtle);

                Window window = new()
                {
                    Content = panel,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertButtonChromeMatchesResources(
                        standard,
                        "ControlFillColorDefaultBrush",
                        "TextFillColorPrimaryBrush",
                        "ControlElevationBorderBrush");
                    AssertButtonChromeMatchesResources(
                        accent,
                        "AccentFillColorDefaultBrush",
                        "TextOnAccentFillColorPrimaryBrush",
                        "AccentControlElevationBorderBrush");
                    AssertButtonChromeMatchesResources(
                        subtle,
                        "SubtleFillColorTransparentBrush",
                        "TextFillColorPrimaryBrush",
                        "SubtleFillColorTransparentBrush");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public async Task Button_Template_UsesWinUiStateResourceMappingsAsync()
        {
            string xaml = await File.ReadAllTextAsync(DemoTestHost.GetRepositoryFilePath("Fluence.Wpf", "Themes", "Controls", "Button.xaml"), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string[] requiredStateResources =
            [
                "ControlFillColorSecondaryBrush",
                "ControlFillColorTertiaryBrush",
                "ControlFillColorDisabledBrush",
                "AccentFillColorSecondaryBrush",
                "AccentFillColorTertiaryBrush",
                "AccentFillColorDisabledBrush",
                "TextOnAccentFillColorSecondaryBrush",
                "SubtleFillColorTransparentBrush",
                "SubtleFillColorSecondaryBrush",
                "SubtleFillColorTertiaryBrush",
                "TextFillColorDisabledBrush",
            ];

            foreach (string resource in requiredStateResources)
            {
                Assert.True(
                    xaml.Contains(resource, StringComparison.Ordinal),
                    "Button template should include the WinUI state resource: " + resource);
            }

            string accentPressedBlock = GetTriggerBlock(
                xaml,
                "<Condition Property=\"IsPressed\" Value=\"True\" />",
                "<Condition Property=\"Appearance\" Value=\"Accent\" />");
            Assert.False(
                accentPressedBlock.Contains("AccentFillColorDisabledBrush", StringComparison.Ordinal),
                "Accent pressed state must not reuse the disabled accent fill as the button Background.");
            Assert.False(
                xaml.Contains("Value=\"Transparent\"", StringComparison.Ordinal),
                "Button template should use theme resources rather than literal transparent brush values.");
        }

        [Fact]
        public Task Button_DefaultAppearance_IsStandardAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Button button = new();

                Assert.Equal(ControlAppearance.Standard, button.Appearance);
            });
        }

        [Fact]
        public Task Button_AccentAppearance_CanBeSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Button button = new()
                {
                    Appearance = ControlAppearance.Accent,
                };

                Assert.Equal(ControlAppearance.Accent, button.Appearance);
            });
        }

        [Fact]
        public Task Button_AccentAppearance_UsesAccentBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Window window = new();
                Controls.Button button = new()
                {
                    Width = 140,
                    Content = "Accent",
                    Appearance = ControlAppearance.Accent,
                    IsHitTestVisible = false,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border restFill = Assert.IsType<Border>(button.Template.FindName("RestFill", button));
                    SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentFillColorDefaultBrush"]);

                    _ = Assert.IsType<SolidColorBrush>(restFill.Background, exactMatch: false);
                    Assert.Equal(accentBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_LeftIconContentGroup_RemainsCenteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.Button button = new()
                {
                    Width = 180,
                    Content = "With Icon",
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE710",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertContentGroupIsCentered(window, button, "With Icon", "\uE710");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_RightIconContentGroup_RemainsCenteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.Button button = new()
                {
                    Width = 180,
                    Content = "Icon Right",
                    IconPlacement = ElementPlacement.Right,
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE72A",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertContentGroupIsCentered(window, button, "Icon Right", "\uE72A");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_LeftIcon_RendersGlyphAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.Button button = new()
                {
                    Width = 180,
                    Content = "With Icon",
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE710",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindVisualChildren<TextBlock>(button).FirstOrDefault(static textBlock => string.Equals(textBlock.Text, "\uE710", StringComparison.Ordinal)));
                    Assert.True(glyphTextBlock.IsVisible, "Left-placed button icons should be visible, not just present in the tree.");
                    Assert.True(glyphTextBlock.ActualWidth > 0, "Left-placed button icons should occupy layout space.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Button_AccentAppearance_UsesDistinctWinUiStateBrushesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Window window = new();
                Controls.Button button = new()
                {
                    Width = 140,
                    Content = "Accent",
                    Appearance = ControlAppearance.Accent,
                    IsHitTestVisible = false,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border restFill = Assert.IsType<Border>(button.Template.FindName("RestFill", button));
                    Border outerBorder = Assert.IsType<Border>(button.Template.FindName("OuterBorder", button));
                    SolidColorBrush accentDefaultBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentFillColorDefaultBrush"]);
                    LinearGradientBrush accentBorderBrush = Assert.IsType<LinearGradientBrush>(application.Resources["AccentControlElevationBorderBrush"]);
                    SolidColorBrush accentSecondaryBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentFillColorSecondaryBrush"]);
                    SolidColorBrush accentTertiaryBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentFillColorTertiaryBrush"]);
                    FontFamily fluentFontFamily = Assert.IsType<FontFamily>(application.Resources["FluentFontFamily"]);
                    TextBlock contentText = Assert.IsType<TextBlock>(FindVisualChildren<TextBlock>(button).FirstOrDefault(static tb => string.Equals(tb.Text, "Accent", StringComparison.Ordinal)));

                    _ = Assert.IsType<SolidColorBrush>(restFill.Background, exactMatch: false);
                    _ = Assert.IsType<LinearGradientBrush>(outerBorder.BorderBrush, exactMatch: false);
                    Assert.Equal(accentDefaultBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                    Assert.Equal(accentBorderBrush.GradientStops.Count, ((LinearGradientBrush)outerBorder.BorderBrush).GradientStops.Count);
                    Assert.Null(outerBorder.Effect);
                    Assert.Equal(fluentFontFamily.Source, button.FontFamily.Source, StringComparer.Ordinal);
                    Assert.Equal(fluentFontFamily.Source, contentText.FontFamily.Source, StringComparer.Ordinal);
                    Assert.NotEqual(accentDefaultBrush.Color, accentSecondaryBrush.Color);
                    Assert.NotEqual(accentDefaultBrush.Color, accentTertiaryBrush.Color);
                    Assert.True(accentSecondaryBrush.Color.A < accentDefaultBrush.Color.A, "Accent pointer-over brush should be visually distinct from default.");
                    Assert.True(accentTertiaryBrush.Color.A < accentSecondaryBrush.Color.A, "Accent pressed brush should progress further than pointer-over.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void AssertContentGroupIsCentered(Window window, Controls.Button button, string content, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindButtonGlyphTextBlock(button, glyph), exactMatch: false);
            ContentPresenter textPresenter = Assert.IsType<ContentPresenter>(FindVisualChildren<ContentPresenter>(button).FirstOrDefault(presenter => string.Equals(presenter.Content as string, content, StringComparison.Ordinal)), exactMatch: false);

            Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            double buttonCenter = buttonOrigin.X + (button.ActualWidth / 2.0);

            Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            Point contentOrigin = textPresenter.TransformToAncestor(window).Transform(new Point(0, 0));
            double groupLeft = Math.Min(glyphOrigin.X, contentOrigin.X);
            double groupRight = Math.Max(glyphOrigin.X + glyphTextBlock.ActualWidth, contentOrigin.X + textPresenter.ActualWidth);
            double groupCenter = groupLeft + ((groupRight - groupLeft) / 2.0);

            Assert.Equal(buttonCenter, groupCenter, 1.0);
        }

        private static void AssertDisabledAccentButtonUsesDarkTokens()
        {
            Window window = new();
            Controls.Button button = new()
            {
                Width = 100,
                Appearance = ControlAppearance.Accent,
                Content = "Add",
                IsEnabled = false,
            };

            try
            {
                window.Content = button;
                window.Show();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Border restFill = Assert.IsType<Border>(button.Template.FindName("RestFill", button));

                _ = Assert.IsType<SolidColorBrush>(restFill.Background, exactMatch: false);
                _ = Assert.IsType<SolidColorBrush>(button.Foreground, exactMatch: false);
                Assert.Equal(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF), ((SolidColorBrush)restFill.Background).Color);
                Assert.Equal(Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF), ((SolidColorBrush)button.Foreground).Color);
            }
            finally
            {
                window.Close();
            }
        }

        private static void AssertButtonChromeMatchesResources(
            Controls.Button button,
            string backgroundKey,
            string foregroundKey,
            string borderKey)
        {
            Border restFill = Assert.IsType<Border>(button.Template.FindName("RestFill", button));
            Border outerBorder = Assert.IsType<Border>(button.Template.FindName("OuterBorder", button));
            Assert.Equal(new Thickness(1), outerBorder.BorderThickness);

            AssertBrushMatchesResource(restFill.Background, backgroundKey);
            AssertBrushMatchesResource(button.Foreground, foregroundKey);
            AssertBrushMatchesResource(outerBorder.BorderBrush, borderKey);
        }

        private static void AssertBrushMatchesResource(Brush actual, string resourceKey)
        {
            Brush expected = Assert.IsType<Brush>(Application.Current.TryFindResource(resourceKey), exactMatch: false);

            if (actual is SolidColorBrush actualSolid && expected is SolidColorBrush expectedSolid)
            {
                Assert.Equal(expectedSolid.Color, actualSolid.Color);
                return;
            }

            if (actual is LinearGradientBrush actualGradient && expected is LinearGradientBrush expectedGradient)
            {
                Assert.Equal(expectedGradient.GradientStops.Count, actualGradient.GradientStops.Count);
                return;
            }

            Assert.Same(expected, actual);
        }

        private static string GetTriggerBlock(string xaml, string firstCondition, string secondCondition)
        {
            int firstIndex = xaml.IndexOf(firstCondition, StringComparison.Ordinal);
            Assert.True(firstIndex >= 0, "Button trigger should contain condition: " + firstCondition);
            int secondIndex = xaml.IndexOf(secondCondition, firstIndex, StringComparison.Ordinal);
            Assert.True(secondIndex >= 0, "Button trigger should contain condition: " + secondCondition);
            int endIndex = xaml.IndexOf("</MultiTrigger>", secondIndex, StringComparison.Ordinal);
            Assert.True(endIndex >= 0, "Button trigger should close after the requested conditions.");
            return xaml[firstIndex..endIndex];
        }
    }
}
