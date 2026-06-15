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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FluenceTreeView = Fluence.Wpf.Controls.TreeView;
using FluenceTreeViewItem = Fluence.Wpf.Controls.TreeViewItem;
using WpfBorder = System.Windows.Controls.Border;

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

        [TestMethod]
        public void TreeView_DefaultStyle_Applies()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeView tv = new();
                _ = tv.Items.Add(new FluenceTreeViewItem { Header = "Node 1" });
                _ = tv.Items.Add(new FluenceTreeViewItem { Header = "Node 2" });
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Template applied → ScrollViewer present
                ScrollViewer? sv = FindVisualChild<ScrollViewer>(tv);
                Assert.IsNotNull(sv, "TreeView template must contain a ScrollViewer.");
                Assert.IsInstanceOfType(sv, typeof(Controls.SmoothScrollViewer),
                    "TreeView must use Fluence SmoothScrollViewer so its scrollbars use the shared Fluent style.");
                Assert.AreSame(app?.TryFindResource("ScrollViewerStyle"), sv.Style,
                    "TreeView SmoothScrollViewer must use the shared ScrollViewerStyle resource.");
                w.Close();
            });
        }

        [TestMethod]
        public void TreeViewItem_TemplateParts_Present()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new FluenceTreeViewItem { Header = "Child 1" });
                FluenceTreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ContentPresenter? cp = FindVisualChildByName<ContentPresenter>(item, "PART_Header");
                Assert.IsNotNull(cp, "PART_Header ContentPresenter must be present in TreeViewItem template.");

                ItemsPresenter? itemsPresenter = FindVisualChildByName<ItemsPresenter>(item, "ItemsHost");
                Assert.IsNotNull(itemsPresenter, "ItemsHost ItemsPresenter must be present in TreeViewItem template.");

                w.Close();
            });
        }

        [TestMethod]
        public void TreeViewItem_Expander_VisibleWhenHasChildren()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new FluenceTreeViewItem { Header = "Child 1" });
                FluenceTreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // The ToggleButton expander must be Visible when HasItems is true
                ToggleButton? expander = FindVisualChildByName<ToggleButton>(item, "Expander");
                Assert.IsNotNull(expander, "Expander ToggleButton must exist in TreeViewItem template.");
                Assert.AreEqual(Visibility.Visible, expander.Visibility,
                    "Expander must be Visible when TreeViewItem has children.");

                w.Close();
            });
        }

        [TestMethod]
        public void TreeViewItem_Expander_CollapsedWhenNoChildren()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeViewItem item = new() { Header = "Leaf" };
                FluenceTreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ToggleButton? expander = FindVisualChildByName<ToggleButton>(item, "Expander");
                Assert.IsNotNull(expander, "Expander ToggleButton must exist.");
                Assert.AreEqual(Visibility.Collapsed, expander.Visibility,
                    "Expander must be Collapsed when TreeViewItem has no children.");

                w.Close();
            });
        }

        [TestMethod]
        public void TreeViewItem_IsExpanded_MakesChildrenVisible()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new FluenceTreeViewItem { Header = "Child 1" });
                FluenceTreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Initially collapsed
                ItemsPresenter? itemsHost = FindVisualChildByName<ItemsPresenter>(item, "ItemsHost");
                Assert.IsNotNull(itemsHost, "ItemsHost must exist.");
                Assert.AreEqual(Visibility.Collapsed, itemsHost.Visibility,
                    "ItemsHost must be Collapsed when IsExpanded=False.");

                // Expand
                item.IsExpanded = true;
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(Visibility.Visible, itemsHost.Visibility,
                    "ItemsHost must be Visible when IsExpanded=True.");

                w.Close();
            });
        }

        [TestMethod]
        public void TreeViewItem_SelectedState_ChangesBackground()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeViewItem item = new() { Header = "Node A" };
                FluenceTreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? itemBorder = FindVisualChildByName<WpfBorder>(item, "ItemBorder");
                Assert.IsNotNull(itemBorder, "ItemBorder must exist in TreeViewItem template.");

                // Background must be transparent (or null) in normal state
                Brush normalBg = itemBorder.Background;

                // Select the item
                item.IsSelected = true;
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? selectedBg = itemBorder.Background as SolidColorBrush;
                SolidColorBrush? expectedBrush = app?.TryFindResource("SubtleFillColorSecondaryBrush") as SolidColorBrush;
                Assert.IsNotNull(expectedBrush, "SubtleFillColorSecondaryBrush must resolve.");
                Assert.IsNotNull(selectedBg, "ItemBorder.Background must be a SolidColorBrush when selected.");
                Assert.AreEqual(expectedBrush.Color, selectedBg.Color,
                    "Selected TreeViewItem must use SubtleFillColorSecondaryBrush per WI-5B.2/WinUI 3.");

                w.Close();
            });
        }

        [TestMethod]
        public void TreeViewItem_HoverTriggers_AreScopedToHeaderBorder()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeViewItem item = new() { Header = "Parent" };
                _ = item.Items.Add(new FluenceTreeViewItem { Header = "Child" });
                FluenceTreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.IsNotNull(item.Template, "TreeViewItem template should be applied.");
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

                Assert.IsTrue(hasHeaderHoverTrigger,
                    "TreeViewItem hover visuals should be scoped to the header border.");
                Assert.IsFalse(hasAncestorHoverTrigger,
                    "TreeViewItem hover visuals should not listen to the whole item, because child hover would light parents.");

                w.Close();
            });
        }

        [TestMethod]
        public void TreeView_ThemeCycle_StyleRemainsApplied()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeView tv = new();
                _ = tv.Items.Add(new FluenceTreeViewItem { Header = "Node 1" });
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                ScrollViewer? sv = FindVisualChild<ScrollViewer>(tv);
                Assert.IsNotNull(sv, "TreeView template must still contain ScrollViewer after theme cycle.");
                Assert.IsInstanceOfType(sv, typeof(Controls.SmoothScrollViewer),
                    "TreeView must keep Fluence SmoothScrollViewer after theme changes.");

                w.Close();
            });
        }

        [TestMethod]
        public void TreeViewItem_ChevronGlyph_PresentInExpander()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTreeViewItem item = new() { Header = "Node A" };
                _ = item.Items.Add(new FluenceTreeViewItem { Header = "Child" });
                FluenceTreeView tv = new();
                _ = tv.Items.Add(item);
                Window w = new() { Content = tv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ToggleButton? expander = FindVisualChildByName<ToggleButton>(item, "Expander");
                Assert.IsNotNull(expander, "Expander must be present.");
                TextBlock? chevron = FindVisualChildByName<TextBlock>(expander, "ChevronGlyph");
                Assert.IsNotNull(chevron,
                    "ChevronGlyph TextBlock must be present inside Expander per WI-5B.2.");
                Assert.AreEqual("\uE76C", chevron.Text,
                    "ChevronGlyph must display Segoe Fluent Icons ChevronRight (U+E76C).");

                w.Close();
            });
        }
    }
}
