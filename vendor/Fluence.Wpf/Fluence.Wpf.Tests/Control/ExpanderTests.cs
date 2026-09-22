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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.Expander"/> control: chevron rotation easing
    /// (ControlFastOutSlowIn / SplineDoubleKeyFrame) and content slide.
    /// </summary>
    public sealed class ExpanderTests : IClassFixture<LightThemeFixture>
    {
        public ExpanderTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---------------------------------------------------------------------------
        // WI-3 B13  Expander chevron rotation easing
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Expander_StyleApplies_RootBorderFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Content" };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // RootBorder is the template root - proves Fluence style applied.
                    Border rootBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "RootBorder"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_ChevronPath_ExistsWithRotateTransformOnParentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = false };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Path chevron = Assert.IsType<Path>(FindVisualChildByName<Path>(expander, "Chevron"), exactMatch: false);

                    // Parent Border owns the RotateTransform.
                    Border parent = Assert.IsType<Border>(VisualTreeHelper.GetParent(chevron));

                    RotateTransform rt = Assert.IsType<RotateTransform>(parent.RenderTransform);
                    Assert.Equal(0.0, rt.Angle, 1.0);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_Expanded_ContentVisibilityIsVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = true };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // Structural check: ExpandSite ContentPresenter is present.
                    ContentPresenter site = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(expander, "ExpandSite"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_HeaderBorder_KeepsTheWholeRadiusUntilItOpensAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", CornerRadius = new CornerRadius(8) };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // WinUI parity (Expander.xaml:111, and the ExpandDown state at :64): closed, the
                    // header is the whole card and takes the control's CornerRadius whole. Only an
                    // open expander gives the bottom corners to the content tier, so a closed
                    // expander is not a card with two square corners along its bottom edge.
                    Border headerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "HeaderBorder"), exactMatch: false);
                    Assert.Equal(new CornerRadius(8), headerBorder.CornerRadius);

                    expander.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Assert.Equal(new CornerRadius(8, 8, 0, 0), headerBorder.CornerRadius);

                    expander.IsExpanded = false;
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Assert.Equal(new CornerRadius(8), headerBorder.CornerRadius);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WinUI content slide: expand/collapse translate the content behind the clip
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Expander_ContentSlideParts_PresentWithClipAndInlineTranslateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body" };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Border contentBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "PART_ContentBorder"), exactMatch: false);
                    Assert.True(contentBorder.ClipToBounds,
                        "PART_ContentBorder must clip its bounds so the content slides behind the clip.");

                    Grid grid = Assert.IsType<Grid>(VisualTreeHelper.GetParent(contentBorder));
                    Assert.Equal(2, grid.RowDefinitions.Count);

                    ContentPresenter site = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(expander, "ExpandSite"), exactMatch: false);
                    _ = Assert.IsType<TranslateTransform>(site.RenderTransform, exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_ExpandSlide_RestsAtZeroWithStarContentRowAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = false };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new Xunit.Sdk.XunitException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new Xunit.Sdk.XunitException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    Assert.Equal(0.0, grid.RowDefinitions[1].Height.Value, 0.001);

                    expander.IsExpanded = true;
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 2000, () => translate.Y < 0).ConfigureAwait(true),
                        "The expand slide must start from a negative offset (content behind the clip).");
                    Assert.True(
                        await WaitUntilAsync(w.Dispatcher, 4000,
                            () => Math.Abs(translate.Y) < 0.001 && grid.RowDefinitions[1].Height.IsStar).ConfigureAwait(true),
                        "The content must rest at translate 0 with a star content row after the expand slide.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_CollapseSlide_ClosesRowAndResetsTranslateAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = true };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new Xunit.Sdk.XunitException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new Xunit.Sdk.XunitException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    Assert.True(grid.RowDefinitions[1].Height.IsStar,
                        "Initial IsExpanded=true must open the content row without animation.");

                    expander.IsExpanded = false;
                    Assert.True(
                        await WaitUntilAsync(w.Dispatcher, 4000,
                            () => !grid.RowDefinitions[1].Height.IsStar
                                && grid.RowDefinitions[1].Height.Value < 0.001
                                && Math.Abs(translate.Y) < 0.001).ConfigureAwait(true),
                        "Collapse must close the content row at slide completion and reset the translate.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_RapidToggleMidFlight_SettlesCollapsedWithoutStuckOffsetAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = false };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new Xunit.Sdk.XunitException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new Xunit.Sdk.XunitException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    // Interrupt the 333 ms expand slide mid-flight with a collapse.
                    expander.IsExpanded = true;
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 2000, () => translate.Y < 0).ConfigureAwait(true),
                        "The expand slide must be in flight before the interrupting collapse.");
                    expander.IsExpanded = false;

                    Assert.True(
                        await WaitUntilAsync(w.Dispatcher, 4000,
                            () => !grid.RowDefinitions[1].Height.IsStar
                                && grid.RowDefinitions[1].Height.Value < 0.001
                                && Math.Abs(translate.Y) < 0.001).ConfigureAwait(true),
                        "A collapse interrupting the expand slide must settle collapsed with no stuck offset.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_ExpandUp_SlidesFromBelowIntoTopContentRowAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Controls.Expander expander = new()
                {
                    Header = "Test",
                    Content = "Body",
                    ExpandDirection = ExpandDirection.Up,
                    IsExpanded = false,
                };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new Xunit.Sdk.XunitException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new Xunit.Sdk.XunitException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    Assert.Equal(0, Grid.GetRow(contentBorder));
                    Assert.Equal(0.0, grid.RowDefinitions[0].Height.Value, 0.001);

                    expander.IsExpanded = true;
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 2000, () => translate.Y > 0).ConfigureAwait(true),
                        "The Up expand slide must start from a positive offset (content below the header).");
                    Assert.True(
                        await WaitUntilAsync(w.Dispatcher, 4000,
                            () => Math.Abs(translate.Y) < 0.001 && grid.RowDefinitions[0].Height.IsStar).ConfigureAwait(true),
                        "The Up content must rest at translate 0 with a star top row after the expand slide.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WinUI content-tier parity: PART_ContentBorder carries its own CardBackground
        // fill and CardStroke border instead of relying on a single outer card border.
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Expander_ContentBorder_CardSecondaryFillAndSeamBorderThicknessAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new()
                {
                    Header = "Test",
                    Content = "Body",
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Border contentBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "PART_ContentBorder"), exactMatch: false);

                    object? expectedBackground = w.TryFindResource("CardBackgroundFillColorSecondaryBrush");
                    Assert.Equal(expectedBackground, contentBorder.Background);

                    // Down (default) direction: the top edge is skipped so the seam against the
                    // header reads as one line rather than a doubled border, both derived live from
                    // the control's own BorderThickness and CornerRadius rather than a hardcoded literal.
                    Assert.Equal(new Thickness(2, 0, 2, 2), contentBorder.BorderThickness);
                    Assert.Equal(new CornerRadius(0, 0, 8, 8), contentBorder.CornerRadius);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_HeaderBackground_PaintsTheHeaderTierOnlyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body" };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    ToggleButton header = Assert.IsType<ToggleButton>(FindVisualChildByName<ToggleButton>(expander, "PART_ToggleButton"), exactMatch: false);
                    Border contentBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "PART_ContentBorder"), exactMatch: false);

                    // WinUI keys the two tiers separately (Expander.xaml:111,114): the header takes
                    // ExpanderHeaderBackground, which is HeaderBackground here, and the content takes
                    // the control's own Background. Left unset, the default style supplies both.
                    Assert.Equal(w.TryFindResource("CardBackgroundFillColorDefaultBrush"), expander.HeaderBackground);
                    Assert.Equal(expander.HeaderBackground, header.Background);
                    Assert.Equal(w.TryFindResource("CardBackgroundFillColorSecondaryBrush"), contentBorder.Background);

                    // Each tier answers its own property, so a consumer can recolour either one
                    // without retemplating and without disturbing the other.
                    SolidColorBrush headerFill = new(Colors.Red);
                    SolidColorBrush contentFill = new(Colors.Blue);
                    expander.HeaderBackground = headerFill;
                    expander.Background = contentFill;
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Assert.Same(headerFill, header.Background);
                    Assert.Same(contentFill, contentBorder.Background);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_ExpandUp_ContentBorderMirrorsSeamAndCornersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new()
                {
                    Header = "Test",
                    Content = "Body",
                    ExpandDirection = ExpandDirection.Up,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Border contentBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "PART_ContentBorder"), exactMatch: false);
                    Border headerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "HeaderBorder"), exactMatch: false);

                    // The header gives up corners only while the expander is open, so the mirror is
                    // asserted in that state.
                    expander.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // Up direction mirrors both tiers: content now sits above the header and owns
                    // the top corners plus the bottom-skipped border edge; the header mirrors to
                    // the bottom corners. Both derived live from the control's own BorderThickness
                    // and CornerRadius rather than a hardcoded literal.
                    Assert.Equal(new Thickness(2, 2, 2, 0), contentBorder.BorderThickness);
                    Assert.Equal(new CornerRadius(8, 8, 0, 0), contentBorder.CornerRadius);
                    Assert.Equal(new CornerRadius(0, 0, 8, 8), headerBorder.CornerRadius);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_Disabled_HeaderKeepsCardFillAndStrokeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsEnabled = false };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // WinUI parity: ExpanderHeaderDisabledBorderBrush resolves to the same
                    // CardStrokeColorDefaultBrush as every other state, and there is no disabled
                    // header background token at all, so the header must not swap to a
                    // ControlFill-disabled look the way a plain button would.
                    Border headerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "HeaderBorder"), exactMatch: false);
                    object? expectedBackground = w.TryFindResource("CardBackgroundFillColorDefaultBrush");
                    object? expectedBorderBrush = w.TryFindResource("CardStrokeColorDefaultBrush");
                    Assert.Equal(expectedBackground, headerBorder.Background);
                    Assert.Equal(expectedBorderBrush, headerBorder.BorderBrush);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_ChevronPlate_HoverAndPressedTintOnlyThePlateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander expander = new() { Header = "Test", Content = "Body" };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // WinUI parity: the header row itself never tints; only the 32x32 chevron
                    // plate does, and it rests at SubtleFillColorTransparentBrush.
                    Border chevronPlate = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "ChevronPlate"), exactMatch: false);
                    Border headerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(expander, "HeaderBorder"), exactMatch: false);

                    object? expectedRestPlate = w.TryFindResource("SubtleFillColorTransparentBrush");
                    object? expectedHeaderBackground = w.TryFindResource("CardBackgroundFillColorDefaultBrush");
                    Assert.Equal(expectedRestPlate, chevronPlate.Background);
                    Assert.Equal(expectedHeaderBackground, headerBorder.Background);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Expander_CornerRadius_DefaultAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander ex = new();
                Assert.Equal(new CornerRadius(4), ex.CornerRadius);
            });
        }

        [Fact]
        public Task Expander_Template_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.Expander ex = new() { Header = "H", Content = new TextBlock { Text = "C" }, Width = 200 };
                try
                {
                    window.Content = ex;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = ex.ApplyTemplate();
                    Assert.NotNull(ex.Template);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
