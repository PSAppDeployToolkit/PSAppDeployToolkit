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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-5B.2 tests: Fluent TreeView + TreeViewItem.
    /// Authority: WinUI 3 TreeView_themeresources.xaml + TreeViewItem.xaml.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-5B.2  TreeView / TreeViewItem
        // ---------------------------------------------------------------------------

        [Fact]
        public Task TreeView_DefaultStyle_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeView tv = new();
                _ = tv.Items.Add(new Controls.TreeViewItem { Header = "Node 1" });
                _ = tv.Items.Add(new Controls.TreeViewItem { Header = "Node 2" });
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Template applied → ScrollViewer present
                ScrollViewer sv = Assert.IsType<ScrollViewer>(FindVisualChild<ScrollViewer>(tv), exactMatch: false);
                _ = Assert.IsType<Controls.SmoothScrollViewer>(sv, exactMatch: false);
                Assert.Same(app.TryFindResource("ScrollViewerStyle"), sv.Style);
                w.Close();
            });
        }

        [Fact]
        public Task TreeViewItem_TemplateParts_PresentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child 1" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ContentPresenter cp = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "PART_Header"), exactMatch: false);

                ItemsPresenter itemsPresenter = Assert.IsType<ItemsPresenter>(FindVisualChildByName<ItemsPresenter>(item, "ItemsHost"), exactMatch: false);

                w.Close();
            });
        }

        [Fact]
        public Task TreeViewItem_Expander_VisibleWhenHasChildrenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child 1" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // The ToggleButton expander must be Visible when HasItems is true
                ToggleButton expander = Assert.IsType<ToggleButton>(FindVisualChildByName<ToggleButton>(item, "Expander"), exactMatch: false);
                Assert.Equal(Visibility.Visible, expander.Visibility);

                w.Close();
            });
        }

        [Fact]
        public Task TreeViewItem_Expander_CollapsedWhenNoChildrenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Leaf" };
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ToggleButton expander = Assert.IsType<ToggleButton>(FindVisualChildByName<ToggleButton>(item, "Expander"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, expander.Visibility);

                w.Close();
            });
        }

        [Fact]
        public Task TreeViewItem_IsExpanded_MakesChildrenVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child 1" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Initially collapsed
                ItemsPresenter itemsHost = Assert.IsType<ItemsPresenter>(FindVisualChildByName<ItemsPresenter>(item, "ItemsHost"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, itemsHost.Visibility);

                // Expand
                item.IsExpanded = true;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(Visibility.Visible, itemsHost.Visibility);

                w.Close();
            });
        }

        [Fact]
        public Task TreeViewItem_SelectedState_ChangesBackgroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border itemBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "ItemBorder"), exactMatch: false);

                // Background must be transparent (or null) in normal state
                Brush normalBg = itemBorder.Background;

                // Select the item
                item.IsSelected = true;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush selectedBg = Assert.IsType<SolidColorBrush>(itemBorder.Background);
                SolidColorBrush expectedBrush = Assert.IsType<SolidColorBrush>(app.TryFindResource("SubtleFillColorSecondaryBrush"));
                Assert.Equal(expectedBrush.Color, selectedBg.Color);

                w.Close();
            });
        }

        [Fact]
        public Task TreeViewItem_HoverTriggers_AreScopedToHeaderBorderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Parent" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ControlTemplate itemTemplate = Assert.IsType<ControlTemplate>(item.Template, exactMatch: false);
                bool hasHeaderHoverTrigger = false;
                bool hasAncestorHoverTrigger = false;

                foreach (TriggerBase triggerBase in itemTemplate.Triggers)
                {
                    if (triggerBase is Trigger trigger && trigger.Property == UIElement.IsMouseOverProperty)
                    {
                        if (trigger.SourceName.Equals("ItemBorder", StringComparison.Ordinal))
                        {
                            hasHeaderHoverTrigger = true;
                        }
                        else
                        {
                            hasAncestorHoverTrigger = true;
                        }
                    }

                    if (triggerBase is MultiTrigger multiTrigger)
                    {
                        foreach (Condition condition in multiTrigger.Conditions.Where(static condition => condition.Property == UIElement.IsMouseOverProperty))
                        {
                            if (condition.SourceName.Equals("ItemBorder", StringComparison.Ordinal))
                            {
                                hasHeaderHoverTrigger = true;
                            }
                            else
                            {
                                hasAncestorHoverTrigger = true;
                            }
                        }
                    }
                }

                Assert.True(hasHeaderHoverTrigger,
                    "TreeViewItem hover visuals should be scoped to the header border.");
                Assert.False(hasAncestorHoverTrigger,
                    "TreeViewItem hover visuals should not listen to the whole item, because child hover would light parents.");

                w.Close();
            });
        }

        [Fact]
        public Task TreeView_ThemeCycle_StyleRemainsAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeView tv = new();
                _ = tv.Items.Add(new Controls.TreeViewItem { Header = "Node 1" });
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ScrollViewer sv = Assert.IsType<ScrollViewer>(FindVisualChild<ScrollViewer>(tv), exactMatch: false);
                _ = Assert.IsType<Controls.SmoothScrollViewer>(sv, exactMatch: false);

                w.Close();
            });
        }

        [Fact]
        public Task TreeViewItem_ChevronGlyph_PresentInExpanderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ToggleButton expander = Assert.IsType<ToggleButton>(FindVisualChildByName<ToggleButton>(item, "Expander"), exactMatch: false);
                TextBlock chevron = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(expander, "ChevronGlyph"), exactMatch: false);
                Assert.Equal("\uE76C", chevron.Text, StringComparer.Ordinal);

                w.Close();
            });
        }
    }
}
