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
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.BrushAssert;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryIconsPage"/>.
    /// </summary>
    public sealed class GalleryIconsPageTests : IAsyncLifetime
    {
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
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryIconsPage(), static window =>
            {
                Controls.ListView list = Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "IconCatalogList"), exactMatch: false);

                List<Button> tiles = [.. FindVisualChildren<Button>(list)
                    .Where(static tile => tile.DataContext is GalleryIconsPage.IconCatalogItem)];
                Assert.True(tiles.Count >= 2, "The initial viewport should realize icon tiles.");

                GalleryIconsPage.IconCatalogItem first = (GalleryIconsPage.IconCatalogItem)tiles[0].DataContext;
                GalleryIconsPage.IconCatalogItem second = (GalleryIconsPage.IconCatalogItem)tiles[1].DataContext;
                Assert.True(first.IsSelected, "The first icon should be selected initially so the sidebar is never empty.");
                Assert.False(second.IsSelected, "The second icon should start unselected.");

                tiles[1].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.True(second.IsSelected, "Clicking a tile should select its icon.");
                Assert.False(first.IsSelected, "Selecting a tile should clear the previous selection.");

                TextBlock nameValue = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "IconNameValueText"), exactMatch: false);
                Controls.FontIcon preview = Assert.IsType<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(window, "IconPreviewGlyph"), exactMatch: false);
                Assert.Equal(second.Name, nameValue.Text, StringComparer.Ordinal);
                Assert.Equal(second.Glyph, preview.Glyph, StringComparer.Ordinal);
            });
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
