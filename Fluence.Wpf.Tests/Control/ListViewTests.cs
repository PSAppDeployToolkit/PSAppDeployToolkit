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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.ListView"/> control: selection indicator and IsItemSelectable.
    /// Authority: WinUI 3 ListViewItem_themeresources.xaml
    /// (ListViewItemSelectionIndicatorCornerRadius=1.5, AccentFillColorDefaultBrush).
    /// </summary>
    public sealed class ListViewTests : IAsyncLifetime
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
        public Task ListView_ItemsLayoutGrid_WrapsItemsAcrossTheListAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView view = new() { Width = 300, Height = 200 };
                for (int index = 0; index < 6; index++)
                {
                    _ = view.Items.Add(new Border { Width = 80, Height = 40 });
                }

                Window window = new() { Content = view, Width = 400, Height = 300 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // The default layout keeps WPF's vertical panel, so nothing is pinned on the control.
                    Assert.Equal(ListViewItemsLayout.List, view.ItemsLayout);
                    Assert.Equal(DependencyProperty.UnsetValue, view.ReadLocalValue(ItemsControl.ItemsPanelProperty));

                    view.ItemsLayout = ListViewItemsLayout.Grid;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WrapPanel panel = Assert.IsType<WrapPanel>(FindVisualChild<WrapPanel>(view), exactMatch: false);
                    Assert.Equal(Orientation.Horizontal, panel.Orientation);

                    // Six 80 dip items across a 300 dip list means the run wraps rather than
                    // running off the edge, which is the whole point of the layout.
                    Border first = Assert.IsType<Border>(view.Items[0], exactMatch: false);
                    Border last = Assert.IsType<Border>(view.Items[5], exactMatch: false);
                    double firstTop = first.TransformToAncestor(panel).Transform(new Point(0, 0)).Y;
                    double lastTop = last.TransformToAncestor(panel).Transform(new Point(0, 0)).Y;
                    Assert.True(lastTop > firstTop, "The grid layout must wrap its items onto further rows.");

                    view.ItemsLayout = ListViewItemsLayout.List;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Null(FindVisualChild<WrapPanel>(view));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ListView_ItemsLayoutGrid_LeavesAConsumerPanelAloneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FrameworkElementFactory factory = new(typeof(System.Windows.Controls.Primitives.UniformGrid));
                ItemsPanelTemplate consumerPanel = new(factory);
                consumerPanel.Seal();

                Controls.ListView view = new() { Width = 300, Height = 200, ItemsPanel = consumerPanel };
                _ = view.Items.Add(new Border { Width = 80, Height = 40 });
                Window window = new() { Content = view, Width = 400, Height = 300 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    view.ItemsLayout = ListViewItemsLayout.Grid;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // A panel the consumer pinned outranks the layout.
                    Assert.Same(consumerPanel, view.ItemsPanel);
                    _ = Assert.IsType<System.Windows.Controls.Primitives.UniformGrid>(FindVisualChild<System.Windows.Controls.Primitives.UniformGrid>(view), exactMatch: false);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-3 C20  ListView SelectionIndicator
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ListView_SelectionIndicator_PresentInItemTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                _ = lv.Items.Add(new ListViewItem { Content = "Item B" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Find the first ListViewItem in the visual tree
                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);

                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_WidthIsCanonicalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);
                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);

                Assert.Equal(3.0, indicator.Width, 0.01);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_CornerRadiusIsCanonicalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);
                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);

                Assert.Equal(new CornerRadius(1.5), indicator.CornerRadius);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_BackgroundIsAccentBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);
                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("AccentFillColorDefaultBrush"));

                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(indicator.Background);
                Assert.Equal(
                    expected.Color,
                    actual.Color);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_AnimateRemove_RemovesItemFromBoundObservableCollectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                ObservableCollection<string> items = ["One", "Two", "Three"];
                Controls.ListView lv = new()
                {
                    Width = 300,
                    Height = 180,
                    ItemsSource = items,
                    ItemAnimationsEnabled = true,
                };
                Window w = new() { Content = lv, Width = 360, Height = 240 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();

                bool completed = false;
                lv.AnimateRemove("Two", delegate { completed = true; });

                bool removed = await WaitUntilAsync(w.Dispatcher, 1000, delegate
                {
                    return completed && !items.Contains("Two");
                }).ConfigureAwait(true);

                Assert.True(removed, "AnimateRemove should animate then remove the item from the bound ObservableCollection.");
                Assert.Equal(2, items.Count);
                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // IsItemSelectable
        // ---------------------------------------------------------------------------

        [Fact]
        public Task IsItemSelectable_DefaultIsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                Assert.True(lv.IsItemSelectable);
            });
        }

        [Fact]
        public Task IsItemSelectable_False_ClearsSelectionWhenSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ListView lv = new() { Width = 260, Height = 120 };
                _ = lv.Items.Add("a");
                _ = lv.Items.Add("b");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.Equal(0, lv.SelectedIndex);

                    lv.IsItemSelectable = false;
                    Assert.Equal(-1, lv.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task IsItemSelectable_False_SelectedIndexStaysMinusOne_AfterDirectSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.Equal(-1, lv.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task IsItemSelectable_False_ContainerIsNotFocusableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem container = Assert.IsType<ListViewItem>(lv.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.False(container.Focusable);
                    Assert.False(Controls.ListView.GetParentIsItemSelectable(container));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task IsItemSelectable_True_ContainerIsFocusableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = true,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem container = Assert.IsType<ListViewItem>(lv.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.True(container.Focusable);
                    Assert.True(Controls.ListView.GetParentIsItemSelectable(container));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ItemAnimationsEnabled_IndependentOfIsItemSelectableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new() { IsItemSelectable = false, ItemAnimationsEnabled = true };
                Assert.False(lv.IsItemSelectable);
                Assert.True(lv.ItemAnimationsEnabled);

                lv.ItemAnimationsEnabled = false;
                lv.IsItemSelectable = true;
                Assert.True(lv.IsItemSelectable);
                Assert.False(lv.ItemAnimationsEnabled);
            });
        }

        [Fact]
        public Task ListView_ItemAnimationsEnabled_DefaultTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView listView = new();

                Assert.True(listView.ItemAnimationsEnabled);
            });
        }

        [Fact]
        public Task ListView_HoverHighlightEnabled_DefaultTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView listView = new();

                Assert.True(listView.HoverHighlightEnabled);
            });
        }

        [Fact]
        public Task ListViewItem_DefaultChrome_UsesWinUiReferenceValuesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ListView listView = new()
                {
                    Width = 260,
                    Height = 120,
                };
                _ = listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem item = Assert.IsType<ListViewItem>(listView.ItemContainerGenerator.ContainerFromIndex(0));

                    Assert.Equal(new Thickness(12, 0, 12, 0), item.Padding);
                    Assert.Equal(HorizontalAlignment.Left, item.HorizontalContentAlignment);
                    Assert.Equal(40.0, item.MinHeight);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ListViewItem_SelectionIndicator_UsesWinUiCornerRadiusAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ListView listView = new()
                {
                    Width = 260,
                    Height = 120,
                };
                _ = listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem item = Assert.IsType<ListViewItem>(listView.ItemContainerGenerator.ContainerFromIndex(0));

                    _ = item.ApplyTemplate();
                    Border selectionIndicator = Assert.IsType<Border>(item.Template.FindName("SelectionIndicator", item));

                    // WI-3 C20: canonical ListViewItemSelectionIndicatorCornerRadius = 1.5
                    Assert.Equal(new CornerRadius(1.5), selectionIndicator.CornerRadius);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ListViewItem_SelectedState_UsesWinUiSelectedBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                Window window = new()
                {
                    Left = -20000,
                    Top = -20000,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                };
                Controls.ListView listView = new()
                {
                    Width = 260,
                    Height = 120,
                    SelectionMode = SelectionMode.Single,
                };
                _ = listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    listView.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem item = Assert.IsType<ListViewItem>(listView.ItemContainerGenerator.ContainerFromIndex(0));

                    _ = item.ApplyTemplate();
                    Border selectedOverlay = Assert.IsType<Border>(item.Template.FindName("SelectedOverlay", item));
                    Border selectionIndicator = Assert.IsType<Border>(item.Template.FindName("SelectionIndicator", item));
                    SolidColorBrush expectedSelectedBrush = Assert.IsType<SolidColorBrush>(application.Resources["SubtleFillColorSecondaryBrush"]);
                    SolidColorBrush expectedIndicatorBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentFillColorDefaultBrush"]);

                    _ = Assert.IsType<SolidColorBrush>(selectedOverlay.Background, exactMatch: false);
                    _ = Assert.IsType<SolidColorBrush>(selectionIndicator.Background, exactMatch: false);
                    Assert.Equal(expectedSelectedBrush.Color, ((SolidColorBrush)selectedOverlay.Background).Color);
                    Assert.Equal(expectedIndicatorBrush.Color, ((SolidColorBrush)selectionIndicator.Background).Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Stage3_ListView_EmptyContent_DefaultNullAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView list = new();
                Assert.Null(list.EmptyContent);
            });
        }

        [Fact]
        public Task Stage3_ListView_EmptyContent_VisibleWhenNoItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ListView list = new()
                {
                    Width = 200,
                    Height = 100,
                    EmptyContent = new TextBlock { Text = "Empty" },
                };

                try
                {
                    window.Content = list;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(list.HasItems);
                    Assert.Contains(FindVisualChildren<TextBlock>(list), static tb => string.Equals(tb.Text, "Empty", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
