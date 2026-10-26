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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.BrushAssert;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryIconsPage"/>.
    /// </summary>
    public sealed class GalleryIconsPageTests : IAsyncLifetime
    {
        private static readonly string[] SidebarHeadingNames =
        [
            "IconNameLabel", "IconTextGlyphLabel", "IconCodeGlyphLabel",
            "IconXamlLabel", "IconCSharpLabel", "IconTagsLabel",
        ];

        private static readonly string[] SidebarValueNames =
        [
            "IconNameValueText", "IconTextGlyphValueText", "IconCodeGlyphValueText",
            "IconXamlValueText", "IconCSharpValueText", "IconNoTagsText",
        ];

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        [Fact]
        public Task GalleryIconsPage_IconographyHeaderAndSearchFollowWinUiGalleryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryIconsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    List<TextBlock> titles = [.. FindVisualChildren<TextBlock>(page)
                        .Where(static text => string.Equals(text.Text, "Iconography", StringComparison.Ordinal))];
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(FindVisualChildByName<Controls.AutoSuggestBox>(page, "IconSearchBox"), exactMatch: false);

                    _ = Assert.Single(titles);
                    Assert.Equal("Search icons by name, code, or tags", search.PlaceholderText, StringComparer.Ordinal);
                    Assert.Equal(420.0, search.Width, 0.1);
                    Assert.Empty(FindVisualChildren<DemoSampleControl>(page));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test drives the page's icon search box, so it builds its own instance rather
        // than mutating the one the class shares.
        [Fact]
        public Task GalleryIconsPage_SearchFiltersCatalogAndSelectsFirstResultAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryIconsPage(), static window =>
            {
                Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(FindVisualChildByName<Controls.AutoSuggestBox>(window, "IconSearchBox"), exactMatch: false);
                Controls.ListView list = Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "IconCatalogList"), exactMatch: false);

                int totalIcons = GetIconCatalogItems(list).Count;
                Assert.True(totalIcons > 500, "Catalog should load the full Segoe Fluent Icons set before filtering.");

                search.Text = "zoom";
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                List<GalleryIconsPage.IconCatalogItem> filtered = GetIconCatalogItems(list);
                Assert.True(filtered.Count > 0, "Searching for zoom should keep matching icons.");
                Assert.True(filtered.Count < totalIcons, "Searching for zoom should remove non-matching icons.");
                foreach (GalleryIconsPage.IconCatalogItem item in filtered)
                {
                    Assert.True(item.Name.Contains("zoom", StringComparison.OrdinalIgnoreCase),
                        "Filtered icons should match the search term: " + item.Name);
                }

                TextBlock nameValue = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "IconNameValueText"), exactMatch: false);
                Assert.Equal(filtered[0].Name, nameValue.Text, StringComparer.Ordinal);
            });
        }

        // This test drives the page's icon catalog by clicking a tile, so it builds its own
        // instance rather than mutating the one the class shares.
        [Fact]
        public Task GalleryIconsPage_ClickingTileSelectsIconAndPopulatesSidebarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                GalleryIconsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.ListView list = Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "IconCatalogList"), exactMatch: false);
                    List<Button> tiles = [.. FindVisualChildren<Button>(list)
                        .Where(static tile => tile.DataContext is GalleryIconsPage.IconCatalogItem)];
                    Assert.True(tiles.Count >= 2, "The initial viewport should realize icon tiles.");

                    GalleryIconsPage.IconCatalogItem first = (GalleryIconsPage.IconCatalogItem)tiles[0].DataContext;
                    GalleryIconsPage.IconCatalogItem second = (GalleryIconsPage.IconCatalogItem)tiles[1].DataContext;
                    Border firstRing = GetSelectionRing(tiles[0]);
                    Border secondRing = GetSelectionRing(tiles[1]);
                    Assert.True(first.IsSelected, "The first icon should be selected initially so the sidebar is never empty.");
                    Assert.False(second.IsSelected, "The second icon should start unselected.");
                    Assert.Equal(1.0, firstRing.Opacity, 3);
                    Assert.Equal(0.0, secondRing.Opacity, 3);

                    tiles[1].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    TextBlock nameValue = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "IconNameValueText"), exactMatch: false);
                    Controls.FontIcon preview = Assert.IsType<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(window, "IconPreviewGlyph"), exactMatch: false);
                    Border previewPlate = Assert.IsType<Border>(FindVisualChildByName<Border>(window, "IconPreviewPlate"), exactMatch: false);
                    Assert.True(second.IsSelected, "Clicking a tile should select its icon immediately.");
                    Assert.False(first.IsSelected, "Selecting a tile should clear the previous selection.");
                    Assert.Equal(second.Name, nameValue.Text, StringComparer.Ordinal);
                    Assert.Equal(second.Glyph, preview.Glyph, StringComparer.Ordinal);
                    AssertIconPalette(tiles, previewPlate, preview, "AccentFillColorDefaultBrush");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconPalette(tiles, previewPlate, preview, "AccentFillColorDefaultBrush");
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconPalette(tiles, previewPlate, preview, "AccentFillColorDefaultBrush");

                    tiles[0].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    tiles[1].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.True(second.IsSelected, "The last rapid click should win.");
                    Assert.Equal(second.Glyph, preview.Glyph, StringComparer.Ordinal);
                    if (SystemParameters.HighContrast
                        || ApplicationThemeManager.ResolvedTheme is ApplicationTheme.HighContrast
                        || !SystemParameters.ClientAreaAnimation
                        || (RenderCapability.Tier >> 16) <= 0)
                    {
                        Assert.Equal(0.0, firstRing.Opacity, 3);
                        Assert.Equal(1.0, secondRing.Opacity, 3);
                        Assert.False(DependencyPropertyHelper.GetValueSource(secondRing, UIElement.OpacityProperty).IsAnimated);
                    }

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => !DependencyPropertyHelper.GetValueSource(firstRing, UIElement.OpacityProperty).IsAnimated
                            && !DependencyPropertyHelper.GetValueSource(secondRing, UIElement.OpacityProperty).IsAnimated).ConfigureAwait(true),
                        "Both selection ring animations should complete.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(0.0, firstRing.Opacity, 3);
                    Assert.Equal(1.0, secondRing.Opacity, 3);
                    Assert.False(DependencyPropertyHelper.GetValueSource(firstRing, UIElement.OpacityProperty).IsAnimated);
                    Assert.False(DependencyPropertyHelper.GetValueSource(secondRing, UIElement.OpacityProperty).IsAnimated);

                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(FindVisualChildByName<Controls.AutoSuggestBox>(window, "IconSearchBox"), exactMatch: false);
                    search.Text = "E71F";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Button selectedTile = Assert.IsType<Button>(FindVisualChildren<Button>(list)
                        .FirstOrDefault(static tile => tile.DataContext is GalleryIconsPage.IconCatalogItem { IsSelected: true }), exactMatch: false);
                    Border searchRing = GetSelectionRing(selectedTile);
                    Assert.Equal(1.0, searchRing.Opacity, 3);
                    Assert.False(DependencyPropertyHelper.GetValueSource(searchRing, UIElement.OpacityProperty).IsAnimated);

                    search.Text = "";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    List<Button> highContrastTiles = [.. FindVisualChildren<Button>(list)
                        .Where(static tile => tile.DataContext is GalleryIconsPage.IconCatalogItem)];
                    Border highContrastRing = GetSelectionRing(highContrastTiles[1]);
                    highContrastTiles[1].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.Equal(ApplicationTheme.HighContrast, ApplicationThemeManager.ResolvedTheme);
                    AssertIconPalette(highContrastTiles, previewPlate, preview, "TextFillColorPrimaryBrush");
                    Assert.Equal(1.0, highContrastRing.Opacity, 3);
                    Assert.False(DependencyPropertyHelper.GetValueSource(highContrastRing, UIElement.OpacityProperty).IsAnimated);

                    list.ScrollIntoView(list.Items[^1]);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.NotNull(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1));
                    List<Button> laterTiles = [.. FindVisualChildren<Button>(list)
                        .Where(static tile => tile.DataContext is GalleryIconsPage.IconCatalogItem)];
                    Assert.NotEmpty(laterTiles);
                    AssertIconPalette(laterTiles, previewPlate, preview, "TextFillColorPrimaryBrush");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    AssertIconPalette(laterTiles, previewPlate, preview, "AccentFillColorDefaultBrush");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        private static Border GetSelectionRing(Button tile)
        {
            return Assert.IsType<Border>(tile.Template.FindName("SelectionRing", tile), exactMatch: false);
        }

        private static void AssertIconPalette(IEnumerable<Button> tiles, Border previewPlate, Controls.FontIcon preview, string resourceKey)
        {
            Assert.Equal(new Thickness(1), previewPlate.BorderThickness);
            AssertBrushColor(preview.Foreground, resourceKey);
            AssertBrushColor(previewPlate.BorderBrush, resourceKey);
            foreach (Button tile in tiles)
            {
                TextBlock glyph = Assert.IsType<TextBlock>(tile.Template.FindName("IconTileGlyph", tile), exactMatch: false);
                Border ring = GetSelectionRing(tile);
                Assert.Equal(new Thickness(1), ring.BorderThickness);
                AssertBrushColor(glyph.Foreground, resourceKey);
                AssertBrushColor(ring.BorderBrush, resourceKey);
            }
        }

        // This test drives the page's icon search box, so it builds its own instance rather
        // than mutating the one the class shares.
        [Fact]
        public Task GalleryIconsPage_SidebarGlyphFieldsMatchWinUiGalleryFormatsAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryIconsPage(), static window =>
            {
                Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(FindVisualChildByName<Controls.AutoSuggestBox>(window, "IconSearchBox"), exactMatch: false);

                search.Text = "E71F";
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                AssertIconSidebarValue(window, "IconNameValueText", "CopyIconNameButton", "ZoomOut");
                AssertIconSidebarValue(window, "IconTextGlyphValueText", "CopyTextGlyphButton", "&#xE71F;");
                AssertIconSidebarValue(window, "IconCodeGlyphValueText", "CopyCodeGlyphButton", "\\uE71F");
                AssertIconSidebarValue(window, "IconXamlValueText", "CopyXamlButton", "<fluence:FontIcon Glyph=\"&#xE71F;\" />");
                AssertIconSidebarValue(window, "IconCSharpValueText", "CopyCSharpButton",
                    "FontIcon icon = new FontIcon();" + Environment.NewLine + "icon.Glyph = \"\\uE71F\";");
            });
        }

        // This test scrolls and realizes the page's virtualized icon catalog, so it builds its
        // own instance rather than mutating the one the class shares.
        [Fact]
        public Task GalleryIconsPage_IconCatalogIsScrollableAndVirtualizedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryIconsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.ListView list = Assert.IsType<Controls.ListView>(DemoTestHost.FindByName<Controls.ListView>(page, "IconCatalogList"), exactMatch: false);
                    Assert.True(list.Items.Count > 100, "Icon catalog must load enough rows to exercise virtualization.");

                    Border catalogCard = Assert.IsType<Border>(DemoTestHost.FindByName<Border>(page, "IconCatalogCard"), exactMatch: false);
                    Assert.Equal(new Thickness(0), catalogCard.Padding);
                    Assert.Equal(new CornerRadius(8), catalogCard.CornerRadius);
                    Assert.Equal(new Thickness(1), catalogCard.BorderThickness);
                    AssertBrushColor(catalogCard.Background, "SolidBackgroundFillColorBaseBrush");
                    AssertBrushColor(catalogCard.BorderBrush, "CardStrokeColorDefaultBrush");
                    Assert.Equal(new Thickness(0), list.BorderThickness);

                    Border detailsPanel = Assert.IsType<Border>(DemoTestHost.FindByName<Border>(page, "IconDetailsPanel"), exactMatch: false);
                    Assert.Equal(new Thickness(1, 0, 0, 0), detailsPanel.BorderThickness);
                    AssertBrushColor(detailsPanel.Background, "CardBackgroundFillColorDefaultBrush");
                    AssertBrushColor(detailsPanel.BorderBrush, "DividerStrokeColorDefaultBrush");

                    ScrollViewer detailsViewer = Assert.IsType<ScrollViewer>(DemoTestHost.FindByName<ScrollViewer>(page, "IconDetailsScrollViewer"), exactMatch: false);
                    Assert.Same(detailsViewer, detailsPanel.Child);
                    Assert.False(detailsViewer.Focusable);
                    Assert.Equal(ScrollBarVisibility.Auto, detailsViewer.VerticalScrollBarVisibility);
                    Assert.Equal(ScrollBarVisibility.Disabled, detailsViewer.HorizontalScrollBarVisibility);
                    Assert.Equal(0.0, detailsViewer.ScrollableWidth, 0.1);

                    ScrollViewer viewer = Assert.IsType<ScrollViewer>(DemoTestHost.FindVisualChildren<ScrollViewer>(list).FirstOrDefault(), exactMatch: false);
                    Assert.True(viewer.ViewportHeight > 0, "Icon catalog needs a bounded viewport height.");
                    Assert.True(viewer.ExtentHeight > viewer.ViewportHeight, "Icon catalog should have a scrollable extent.");
                    Assert.True(viewer.ScrollableHeight > 0, "Icon catalog should be scrollable.");

                    int realizedBeforeScroll = CountVisualChildren<ListViewItem>(list);
                    Assert.True(realizedBeforeScroll > 0, "Initial viewport should realize some row containers.");
                    Assert.True(realizedBeforeScroll < list.Items.Count / 2, "Initial layout should not realize most icon rows.");
                    Assert.Null(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1));

                    list.ScrollIntoView(list.Items[^1]);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1));

                    window.Height = 460;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.True(detailsViewer.ScrollableHeight > 0, "Short windows should keep the code and tags reachable in the details pane.");
                    detailsViewer.ScrollToBottom();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(detailsViewer.ScrollableHeight, detailsViewer.VerticalOffset, 0.5);
                    Assert.Equal(0.0, detailsViewer.ScrollableWidth, 0.1);

                    WrapPanel tags = Assert.IsType<WrapPanel>(DemoTestHost.FindByName<WrapPanel>(page, "IconTagsPanel"), exactMatch: false);
                    TextBlock noTags = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(page, "IconNoTagsText"), exactMatch: false);
                    FrameworkElement bottomContent = tags.Visibility is Visibility.Visible ? tags : noTags;
                    Assert.Equal(Visibility.Visible, bottomContent.Visibility);
                    Assert.True(bottomContent.ActualHeight > 0, "The last details field must have visible content.");
                    if (tags.Visibility is Visibility.Visible)
                    {
                        List<TextBlock> tagValues = [.. DemoTestHost.FindVisualChildren<TextBlock>(tags)];
                        Assert.NotEmpty(tagValues);
                        foreach (TextBlock tagValue in tagValues)
                        {
                            Assert.Equal(14.0, tagValue.FontSize, 0.1);
                            Assert.Equal(FontWeights.Normal, tagValue.FontWeight);
                        }
                    }
                    Point bottomTop = bottomContent.TranslatePoint(new Point(0, 0), detailsViewer);
                    Assert.InRange(bottomTop.Y, -0.5, detailsViewer.ViewportHeight);
                    Assert.InRange(bottomTop.Y + bottomContent.ActualHeight, 0, detailsViewer.ViewportHeight + 0.5);

                    detailsViewer.ScrollToTop();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Controls.Button lowerCopy = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(page, "CopyCSharpButton"), exactMatch: false);
                    Assert.True(lowerCopy.Focus(), "The lower code copy button should remain keyboard reachable.");
                    lowerCopy.BringIntoView();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Point copyTop = lowerCopy.TranslatePoint(new Point(0, 0), detailsViewer);
                    Assert.InRange(copyTop.Y, -0.5, detailsViewer.ViewportHeight);
                    Assert.InRange(copyTop.Y + lowerCopy.ActualHeight, 0, detailsViewer.ViewportHeight + 0.5);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test measures the realized icon tile geometry against the WinUI 3 Gallery's
        // measured pitch, so it builds its own instance rather than mutating the shared one.
        [Fact]
        public Task GalleryIconsPage_IconTileGeometryMatchesWinUiGalleryPitchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryIconsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.ListView list = Assert.IsType<Controls.ListView>(DemoTestHost.FindByName<Controls.ListView>(page, "IconCatalogList"), exactMatch: false);
                    List<Button> tiles = [.. DemoTestHost.FindVisualChildren<Button>(list)
                        .Where(static tile => tile.DataContext is GalleryIconsPage.IconCatalogItem)];
                    Assert.True(tiles.Count > 0, "The initial viewport should realize icon tiles.");

                    Button tile = tiles[0];
                    Assert.Equal(93.0, tile.Width, 0.1);
                    Assert.Equal(92.0, tile.Height, 0.1);
                    Assert.Equal(new Thickness(0, 0, 10, 11), tile.Margin);

                    // Pitch (tile size plus gutter) must land at 103 dip in both directions,
                    // matching the WinUI 3 Gallery's measured 140x138 px tile at a 103 dip
                    // pitch, 150% DPI, so four tiles fit the catalog's left column.
                    Assert.Equal(103.0, tile.Width + tile.Margin.Right, 0.1);
                    Assert.Equal(103.0, tile.Height + tile.Margin.Bottom, 0.1);

                    Border previewPlate = Assert.IsType<Border>(DemoTestHost.FindByName<Border>(page, "IconPreviewPlate"), exactMatch: false);
                    Controls.FontIcon previewGlyph = Assert.IsType<Controls.FontIcon>(DemoTestHost.FindByName<Controls.FontIcon>(page, "IconPreviewGlyph"), exactMatch: false);
                    ScrollViewer detailsViewer = Assert.IsType<ScrollViewer>(DemoTestHost.FindByName<ScrollViewer>(page, "IconDetailsScrollViewer"), exactMatch: false);
                    Assert.Equal(112.0, previewPlate.ActualWidth, 0.1);
                    Assert.Equal(112.0, previewPlate.ActualHeight, 0.1);
                    Assert.Equal(56.0, previewGlyph.IconFontSize, 0.1);
                    double previewCenter = previewPlate.TranslatePoint(new Point(previewPlate.ActualWidth / 2.0, 0), detailsViewer).X;
                    Assert.Equal(detailsViewer.ViewportWidth / 2.0, previewCenter, 0.5);

                    TextBlock nameLabel = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(page, "IconNameLabel"), exactMatch: false);
                    TextBlock nameValue = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(page, "IconNameValueText"), exactMatch: false);
                    Grid nameValueRow = Assert.IsType<Grid>(VisualTreeHelper.GetParent(nameValue), exactMatch: false);
                    foreach (string headingName in SidebarHeadingNames)
                    {
                        TextBlock heading = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(page, headingName), exactMatch: false);
                        Assert.Equal(16.0, heading.FontSize, 0.1);
                        Assert.Equal(FontWeights.Bold, heading.FontWeight);
                    }

                    foreach (string valueName in SidebarValueNames)
                    {
                        TextBlock value = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(page, valueName), exactMatch: false);
                        Assert.Equal(14.0, value.FontSize, 0.1);
                        Assert.Equal(FontWeights.Normal, value.FontWeight);
                        Assert.True(nameLabel.FontSize > value.FontSize, "Each heading should be larger than its details value.");
                    }

                    Assert.Equal(6.0, nameLabel.Margin.Bottom, 0.1);
                    Assert.Equal(20.0, nameValueRow.Margin.Bottom, 0.1);
                    Assert.Equal(24.0, previewPlate.Margin.Bottom, 0.1);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        private static List<GalleryIconsPage.IconCatalogItem> GetIconCatalogItems(Controls.ListView list)
        {
            List<GalleryIconsPage.IconCatalogItem> items = [];
            if (list.ItemsSource is IEnumerable<GalleryIconsPage.IconCatalogRow> rows)
            {
                foreach (GalleryIconsPage.IconCatalogRow row in rows)
                {
                    items.AddRange(row.Items);
                }
            }

            return items;
        }

        private static void AssertIconSidebarValue(Window window, string valueName, string buttonName, string expected)
        {
            TextBlock value = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, valueName), exactMatch: false);
            Controls.Button copy = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, buttonName), exactMatch: false);
            Assert.Equal(expected, value.Text, StringComparer.Ordinal);
            Assert.Equal(expected, copy.Tag as string, StringComparer.Ordinal);
        }

        private static int CountVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            int count = 0;
            foreach (T item in DemoTestHost.FindVisualChildren<T>(root))
            {
                count++;
            }

            return count;
        }
    }
}
