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
        public void TreeView_DefaultStyle_Applies()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeView tv = new();
                _ = tv.Items.Add(new Controls.TreeViewItem { Header = "Node 1" });
                _ = tv.Items.Add(new Controls.TreeViewItem { Header = "Node 2" });
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Template applied → ScrollViewer present
                ScrollViewer sv = Assert.IsAssignableFrom<ScrollViewer>(FindVisualChild<ScrollViewer>(tv));
                _ = Assert.IsAssignableFrom<Controls.SmoothScrollViewer>(sv);
                Assert.Same(app?.TryFindResource("ScrollViewerStyle"), sv.Style);
                w.Close();
            });
        }

        [Fact]
        public void TreeViewItem_TemplateParts_Present()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child 1" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ContentPresenter cp = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "PART_Header"));

                ItemsPresenter itemsPresenter = Assert.IsAssignableFrom<ItemsPresenter>(FindVisualChildByName<ItemsPresenter>(item, "ItemsHost"));

                w.Close();
            });
        }

        [Fact]
        public void TreeViewItem_Expander_VisibleWhenHasChildren()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child 1" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // The ToggleButton expander must be Visible when HasItems is true
                ToggleButton expander = Assert.IsAssignableFrom<ToggleButton>(FindVisualChildByName<ToggleButton>(item, "Expander"));
                Assert.Equal(Visibility.Visible, expander.Visibility);

                w.Close();
            });
        }

        [Fact]
        public void TreeViewItem_Expander_CollapsedWhenNoChildren()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Leaf" };
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ToggleButton expander = Assert.IsAssignableFrom<ToggleButton>(FindVisualChildByName<ToggleButton>(item, "Expander"));
                Assert.Equal(Visibility.Collapsed, expander.Visibility);

                w.Close();
            });
        }

        [Fact]
        public void TreeViewItem_IsExpanded_MakesChildrenVisible()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child 1" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Initially collapsed
                ItemsPresenter itemsHost = Assert.IsAssignableFrom<ItemsPresenter>(FindVisualChildByName<ItemsPresenter>(item, "ItemsHost"));
                Assert.Equal(Visibility.Collapsed, itemsHost.Visibility);

                // Expand
                item.IsExpanded = true;
                DrainDispatcher(w.Dispatcher);

                Assert.Equal(Visibility.Visible, itemsHost.Visibility);

                w.Close();
            });
        }

        [Fact]
        public void TreeViewItem_SelectedState_ChangesBackground()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Border itemBorder = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(item, "ItemBorder"));

                // Background must be transparent (or null) in normal state
                Brush normalBg = itemBorder.Background;

                // Select the item
                item.IsSelected = true;
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush selectedBg = Assert.IsType<SolidColorBrush>(itemBorder.Background);
                SolidColorBrush expectedBrush = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SubtleFillColorSecondaryBrush"));
                Assert.Equal(expectedBrush.Color, selectedBg.Color);

                w.Close();
            });
        }

        [Fact]
        public void TreeViewItem_HoverTriggers_AreScopedToHeaderBorder()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Parent" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.NotNull(item.Template);
                bool hasHeaderHoverTrigger = false;
                bool hasAncestorHoverTrigger = false;

                foreach (TriggerBase triggerBase in item.Template.Triggers)
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
                        foreach (Condition condition in multiTrigger.Conditions)
                        {
                            if (condition.Property == UIElement.IsMouseOverProperty)
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
                }

                Assert.True(hasHeaderHoverTrigger,
                    "TreeViewItem hover visuals should be scoped to the header border.");
                Assert.False(hasAncestorHoverTrigger,
                    "TreeViewItem hover visuals should not listen to the whole item, because child hover would light parents.");

                w.Close();
            });
        }

        [Fact]
        public void TreeView_ThemeCycle_StyleRemainsApplied()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeView tv = new();
                _ = tv.Items.Add(new Controls.TreeViewItem { Header = "Node 1" });
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                ScrollViewer sv = Assert.IsAssignableFrom<ScrollViewer>(FindVisualChild<ScrollViewer>(tv));
                _ = Assert.IsAssignableFrom<Controls.SmoothScrollViewer>(sv);

                w.Close();
            });
        }

        [Fact]
        public void TreeViewItem_ChevronGlyph_PresentInExpander()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new Controls.TreeViewItem { Header = "Child" });
                Controls.TreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ToggleButton expander = Assert.IsAssignableFrom<ToggleButton>(FindVisualChildByName<ToggleButton>(item, "Expander"));
                TextBlock chevron = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(expander, "ChevronGlyph"));
                Assert.Equal("\uE76C", chevron.Text, StringComparer.Ordinal);

                w.Close();
            });
        }
    }
}
