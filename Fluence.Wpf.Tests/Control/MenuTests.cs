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
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the Fluent Menu style.
    /// </summary>
    public sealed class MenuTests : IAsyncLifetime
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
        public Task Menu_StyleApplies_BackgroundIsTransparentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Menu menu = new();
                _ = menu.Items.Add(new MenuItem { Header = "File" });
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Background must be Transparent (from style setter)
                SolidColorBrush bg = Assert.IsType<SolidColorBrush>(menu.Background);
                Assert.Equal(
                    Colors.Transparent,
                    bg.Color);
                w.Close();
            });
        }

        [Fact]
        public Task Menu_StyleApplies_BorderThicknessIsZeroAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Menu menu = new();
                _ = menu.Items.Add(new MenuItem { Header = "Edit" });
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(
                    new Thickness(0),
                    menu.BorderThickness);
                w.Close();
            });
        }

        [Fact]
        public Task Menu_AcceptsMenuItemItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Menu menu = new();
                MenuItem item1 = new() { Header = "File" };
                MenuItem item2 = new() { Header = "Edit" };
                _ = menu.Items.Add(item1);
                _ = menu.Items.Add(item2);
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(2, menu.Items.Count);
                _ = Assert.IsType<MenuItem>(menu.Items[0], exactMatch: false);
                _ = Assert.IsType<MenuItem>(menu.Items[1], exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task Menu_ThemeCycle_BackgroundRemainsTransparentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Menu menu = new();
                _ = menu.Items.Add(new MenuItem { Header = "View" });
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush bg = Assert.IsType<SolidColorBrush>(menu.Background);
                Assert.Equal(
                    Colors.Transparent,
                    bg.Color);
                w.Close();
            });
        }

        [Fact]
        public Task Menu_TopLevelItem_UsesMenuBarItemMetricsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MenuItem fileItem = new() { Header = "File" };
                MenuItem viewItem = new() { Header = "View" };
                Menu menu = new();
                _ = menu.Items.Add(fileItem);
                _ = menu.Items.Add(viewItem);
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // A direct child of Menu with no submenu resolves to TopLevelItem, not
                // SubmenuItem, so the new label-only metrics apply.
                Assert.Equal(System.Windows.Controls.MenuItemRole.TopLevelItem, fileItem.Role);

                System.Windows.Controls.Border bd = FindVisualChildByName<System.Windows.Controls.Border>(fileItem, "Bd")
                    ?? throw new InvalidOperationException("Bd template part not found.");
                System.Windows.Controls.Grid contentGrid = FindVisualChildByName<System.Windows.Controls.Grid>(fileItem, "ContentGrid")
                    ?? throw new InvalidOperationException("ContentGrid template part not found.");

                // MenuBarItemButtonPadding = 10,4,10,4 and MenuBarItemMargin = 4,4,4,4
                // (WinUI 3 MenuBar_themeresources.xaml), applied directly on Bd.
                Assert.Equal(new Thickness(10, 4, 10, 4), bd.Padding);
                Assert.Equal(new Thickness(4, 4, 4, 4), bd.Margin);

                // The flyout item's own 4,0 margin is neutralized for a top-level item so it
                // does not add hidden width on top of Bd's own padding.
                Assert.Equal(new Thickness(0), contentGrid.Margin);

                // Icon/checkmark, gap, input-gesture and chevron columns collapse to zero width.
                Assert.Equal(new GridLength(0), contentGrid.ColumnDefinitions[0].Width);
                Assert.Equal(new GridLength(0), contentGrid.ColumnDefinitions[1].Width);
                Assert.Equal(new GridLength(0), contentGrid.ColumnDefinitions[3].Width);
                Assert.Equal(new GridLength(0), contentGrid.ColumnDefinitions[4].Width);

                // Rendered width tracks the label plus 20 dip of padding (10 left + 10 right),
                // not the 100-plus dip a flyout-style item would carry for the same label.
                Assert.True(
                    fileItem.ActualWidth is > 20 and < 100,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected a label-sized top-level item, got ActualWidth={0}.",
                        fileItem.ActualWidth));

                // The Menu bar itself floors at WinUI's MenuBarHeight (MinHeight, not Height,
                // per WinUI 3 MenuBar.xaml DefaultMenuBarStyle) so taller consumer content is
                // not clipped.
                Assert.Equal(40d, menu.MinHeight);

                w.Close();
            });
        }
    }
}
