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
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryTabsPage"/>.
    /// </summary>
    public sealed class GalleryTabsPageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        // This test clicks the TabView add-tab button, so it builds its own instance rather
        // than mutating the one the class shares.
        [Fact]
        public Task GalleryTabsPage_TabViewContentUsesLayerFillSurfaceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryTabsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.TabView tabView = Assert.IsType<Controls.TabView>(DemoTestHost.FindByName<Controls.TabView>(page, "DemoTabView"), exactMatch: false);

                    foreach (Controls.TabViewItem item in tabView.Items.OfType<Controls.TabViewItem>())
                    {
                        AssertTabViewItemContentSurface(item);
                    }

                    ButtonBase addButton = Assert.IsType<ButtonBase>(tabView.Template.FindName("PART_AddTabButton", tabView), exactMatch: false);
                    addButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, addButton));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(4, tabView.Items.Count);
                    Controls.TabViewItem selectedTab = Assert.IsType<Controls.TabViewItem>(tabView.SelectedItem);
                    AssertTabViewItemContentSurface(selectedTab);

                    DemoSampleControl sample = Assert.IsType<DemoSampleControl>(DemoTestHost.FindVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("TabViewDocuments", StringComparison.Ordinal)), exactMatch: false);
                    Assert.Contains("LayerFillColorDefaultBrush", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("LayerFillColorDefaultBrush", sample.CSharpSource, StringComparison.Ordinal);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryTabsPage_PlacementSampleUsesLeftPlacementOnlyAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryTabsPage(), static window =>
            {
                Dictionary<string, TabItem> items = FindVisualChildren<TabItem>(window)
                    .Where(static item => item.Header is string)
                    .ToDictionary(static item => (string)item.Header, StringComparer.Ordinal);

                double infoWidth = GetExplicitHeaderWidth(items, "Inbox");
                double archiveWidth = GetExplicitHeaderWidth(items, "Archive");

                Assert.Equal(infoWidth, archiveWidth, 0.1);
                Assert.True(infoWidth > 0.0, "Placement sample tab headers should use an explicit shared width.");

                TabControl leftTabs = Assert.IsType<TabControl>(FindVisualChildByName<TabControl>(window, "LeftPlacementTabs"), exactMatch: false);
                Assert.Equal(Dock.Left, leftTabs.TabStripPlacement);

                TabControl? bottomTabs = FindVisualChildByName<TabControl>(window, "BottomPlacementTabs");
                Assert.Null(bottomTabs);
            });
        }

        private static void AssertTabViewItemContentSurface(Controls.TabViewItem item)
        {
            Border surface = Assert.IsType<Border>(item.Content);
            BrushAssert.AssertBrushColor(surface.Background, "LayerFillColorDefaultBrush");
        }

        private static double GetExplicitHeaderWidth(IDictionary<string, TabItem> items, string header)
        {
            Assert.True(items.TryGetValue(header, out TabItem? item), "TabItem should exist: " + header);
            return double.IsNaN(item.Width) ? item.MinWidth : item.Width;
        }
    }
}
