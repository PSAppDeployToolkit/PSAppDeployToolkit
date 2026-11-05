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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryDataGridPage"/>: three DataGrid samples and the catalog entry.
    /// </summary>
    public sealed class GalleryDataGridPageTests : IAsyncLifetime
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
        public Task GalleryDataGridPage_RendersThreePopulatedGridsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryDataGridPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);

                try
                {
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                    Assert.Equal(3, samples.Count);

                    List<Controls.DataGrid> grids = [.. DemoTestHost.FindVisualChildren<Controls.DataGrid>(page)];
                    Assert.Equal(3, grids.Count);
                    Assert.Equal(1000, grids[0].Items.Count);
                    Assert.True(grids[1].IsGrouping, "The second sample groups its rows.");
                    Assert.True(grids[2].Items.Count > 0, "The editing sample starts with rows.");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryDataGridPage_EditingGrid_FitsColumnsWithOutputInOptionsRailAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // The shell opens at its minimum width, which leaves the page the least room the gallery allows.
                MainWindow window = DemoTestHost.CreateShownMainWindow();

                try
                {
                    window.NavigateTo("data grid");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    GalleryDataGridPage page = Assert.Single(DemoTestHost.FindVisualChildren<GalleryDataGridPage>(window));
                    DemoSampleControl sample = DemoTestHost.FindVisualChildren<DemoSampleControl>(page).ElementAt(2);
                    Visual rail = Assert.IsType<Visual>(sample.FindName("RightRailBorder"), exactMatch: false);
                    TextBlock output = Assert.IsType<TextBlock>(page.FindName("EditResultText"));

                    // The edit result sits in the options rail under the row buttons, so the demo area has no output column.
                    Assert.Null(sample.OutputContent);
                    Assert.True(output.IsDescendantOf(rail), "The edit result must sit in the options rail.");

                    // The columns share the grid's width, so the grid needs no horizontal scrolling.
                    Controls.DataGrid grid = DemoTestHost.FindVisualChildren<Controls.DataGrid>(page).ElementAt(2);
                    ScrollViewer scroller = Assert.IsType<ScrollViewer>(grid.Template.FindName("DG_ScrollViewer", grid), exactMatch: false);
                    Assert.True(scroller.ExtentWidth <= scroller.ViewportWidth + 1, "The editing grid's columns must fit without horizontal scrolling.");

                    // A longer result message must wrap rather than widen the rail and take width from the grid.
                    double width = grid.ActualWidth;
                    output.Text = "Validation error: Quantity cannot be negative.";
                    window.UpdateLayout();
                    Assert.Equal(width, grid.ActualWidth);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public void DemoNavigationCatalog_ListsDataGridAfterData()
        {
            List<string> routes = [.. DemoNavigationCatalog.Items.Select(static item => item.Route)];

            int data = routes.IndexOf("data");
            Assert.True(data >= 0, "The Data route must exist.");
            Assert.Equal("data grid", routes[data + 1]);
        }
    }
}
