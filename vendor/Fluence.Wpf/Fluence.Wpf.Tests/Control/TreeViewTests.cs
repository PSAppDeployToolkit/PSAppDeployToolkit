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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.TreeView"/> / <see cref="Controls.TreeViewItem"/>
    /// pair: default style, template parts, expander visibility and glyph, selection background,
    /// hover trigger scoping, theme cycling, and the Single/Multiple/None selection modes.
    /// Authority: WinUI 3 TreeView_themeresources.xaml + TreeViewItem.xaml.
    /// </summary>
    public sealed class TreeViewTests : IAsyncLifetime
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
        public Task TreeView_SelectionIndicator_StaysInOneColumnAtEveryDepthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TreeViewItem child = new() { Header = "Child" };
                Controls.TreeViewItem root = new() { Header = "Root", IsExpanded = true };
                _ = root.Items.Add(child);

                Controls.TreeView tree = new() { Width = 260, Height = 200 };
                _ = tree.Items.Add(root);
                Window window = new() { Content = tree, Width = 320, Height = 260 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border rootRail = FindIndicator(root);
                    Border childRail = FindIndicator(child);

                    // Each level nests one item template inside a 20 dip indent, so the child's rail
                    // would step right with it. The rail is pulled back by its own depth instead, and
                    // the selection column stays straight however deep the tree runs.
                    double rootX = rootRail.TransformToAncestor(tree).Transform(new Point(0, 0)).X;
                    double childX = childRail.TransformToAncestor(tree).Transform(new Point(0, 0)).X;
                    Assert.Equal(rootX, childX, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TreeView_MultipleSelection_HidesTheSelectionIndicatorAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TreeViewItem item = new() { Header = "Item" };
                Controls.TreeView tree = new() { Width = 260, Height = 200, SelectionMode = TreeViewSelectionMode.Multiple };
                _ = tree.Items.Add(item);
                Window window = new() { Content = tree, Width = 320, Height = 260 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // The checkbox carries the selection read in Multiple mode, so the rail would be
                    // a second answer to the same question.
                    Assert.Equal(Visibility.Collapsed, FindIndicator(item).Visibility);

                    tree.SelectionMode = TreeViewSelectionMode.Single;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Visible, FindIndicator(item).Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static Border FindIndicator(Controls.TreeViewItem item)
        {
            return Assert.IsType<Border>(
                FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);
        }

        [Fact]
        public Task TreeView_DefaultStyle_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

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

        [Fact]
        public Task TreeView_DefaultSelectionModeIsSingleWithLiveSelectedItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TreeView treeView = new();

                Assert.Equal(TreeViewSelectionMode.Single, treeView.SelectionMode);
                Assert.NotNull(treeView.SelectedItems);
                Assert.Empty(treeView.SelectedItems);
            });
        }

        [Fact]
        public Task TreeView_MultipleSelectionShowsCheckboxAndSyncsSelectedItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.TreeViewItem first = new() { Header = "First" };
                    Controls.TreeViewItem second = new() { Header = "Second" };
                    Controls.TreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple,
                    };
                    _ = treeView.Items.Add(first);
                    _ = treeView.Items.Add(second);
                    window.Content = treeView;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // CheckBox here is System.Windows.Controls.CheckBox: this file imports
                    // System.Windows.Controls, not Fluence.Wpf.Controls, unlike the
                    // Controls.TreeViewItem above, which is the Fluence type.
                    CheckBox firstCheckBox = Assert.IsType<CheckBox>(FindVisualChildByName<CheckBox>(first, "SelectionCheckBox"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, firstCheckBox.Visibility);
                    Assert.True(firstCheckBox.IsThreeState,
                        "Multiple-selection TreeViewItem checkbox should support indeterminate parent state.");

                    first.IsSelectionChecked = true;
                    second.IsSelectionChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(2, treeView.SelectedItems.Count);
                    Assert.Contains(first, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(second, treeView.SelectedItems.Cast<object>());

                    first.IsSelectionChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    _ = Assert.Single(treeView.SelectedItems);
                    Assert.DoesNotContain(first, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(second, treeView.SelectedItems.Cast<object>());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TreeView_MultipleSelectionSpaceTogglesItemCheckStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.TreeViewItem item = new() { Header = "Contracts" };
                    Controls.TreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple,
                    };
                    _ = treeView.Items.Add(item);
                    window.Content = treeView;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = treeView.ApplyTemplate();
                    _ = item.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TreeViewItem keyboardItem =
                        treeView.ItemContainerGenerator.ContainerFromItem(item) as Controls.TreeViewItem ?? item;

                    _ = keyboardItem.ApplyTemplate();
                    _ = keyboardItem.Focus();
                    _ = Keyboard.Focus(keyboardItem);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    keyboardItem.IsSelectionChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(keyboardItem.ToggleMultipleSelectionFromKeyboard(),
                        "Focused TreeViewItem should accept Space in Multiple selection mode.");

                    Assert.Equal(true, keyboardItem.IsSelectionChecked);
                    Assert.Contains(item, treeView.SelectedItems.Cast<object>());

                    Assert.True(keyboardItem.ToggleMultipleSelectionFromKeyboard(),
                        "Focused TreeViewItem should accept Space again in Multiple selection mode.");

                    Assert.Equal(false, keyboardItem.IsSelectionChecked);
                    Assert.DoesNotContain(item, treeView.SelectedItems.Cast<object>());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TreeView_NoneSelectionHidesCheckboxAndClearsSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.TreeViewItem item = new() { Header = "Leaf" };
                    Controls.TreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple,
                    };
                    _ = treeView.Items.Add(item);
                    window.Content = treeView;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    item.IsSelectionChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = Assert.Single(treeView.SelectedItems);

                    treeView.SelectionMode = TreeViewSelectionMode.None;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    CheckBox checkBox = Assert.IsType<CheckBox>(FindVisualChildByName<CheckBox>(item, "SelectionCheckBox"), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, checkBox.Visibility);
                    Assert.Equal(false, item.IsSelectionChecked);
                    Assert.Empty(treeView.SelectedItems);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TreeView_MultipleSelectionCheckbox_StaysCompactAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.TreeViewItem item = new() { Header = "Contracts" };
                    Controls.TreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple,
                    };
                    _ = treeView.Items.Add(item);
                    window.Content = treeView;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    CheckBox checkBox = Assert.IsType<CheckBox>(FindVisualChildByName<CheckBox>(item, "SelectionCheckBox"), exactMatch: false);

                    // A content-less checkbox living in TreeViewItem's Auto-width selection
                    // column must not inherit the WinUI DefaultCheckBoxStyle MinWidth of 120
                    // (Fluence.Wpf/Themes/Controls/CheckBox.xaml), or every row grows a wide
                    // dead-click gap between the box and the header text.
                    Assert.True(checkBox.ActualWidth < 40,
                        string.Format(CultureInfo.InvariantCulture, "SelectionCheckBox should stay compact (no MinWidth 120 inheritance); actual width was {0}.", checkBox.ActualWidth));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TreeView_MultipleSelection_RemovalAndResetDiscardDetachedItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TreeView tree = new() { SelectionMode = TreeViewSelectionMode.Multiple };
                Controls.TreeViewItem parent = new() { Header = "Parent", IsExpanded = true };
                Controls.TreeViewItem first = new() { Header = "First" };
                Controls.TreeViewItem second = new() { Header = "Second" };
                _ = tree.Items.Add(parent);
                _ = parent.Items.Add(first);
                _ = parent.Items.Add(second);
                Window window = new() { Content = tree, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    first.IsSelectionChecked = true;
                    Assert.Contains(first, tree.SelectedItems.Cast<object>());
                    parent.Items.Remove(first);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Empty(tree.SelectedItems);
                    Assert.False(parent.IsSelectionChecked);
                    second.IsSelectionChecked = true;
                    parent.Items.Clear();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Empty(tree.SelectedItems);
                    Assert.False(parent.IsSelectionChecked);
                    parent.IsSelectionChecked = true;
                    tree.Items.Remove(parent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Empty(tree.SelectedItems);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TreeView_MultipleSelection_ItemsSourceReplacementDiscardsOldSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                System.Collections.ObjectModel.ObservableCollection<string> originalItems = ["Old"];
                System.Collections.ObjectModel.ObservableCollection<string> replacementItems = ["New"];
                Controls.TreeView tree = new() { SelectionMode = TreeViewSelectionMode.Multiple, ItemsSource = originalItems };
                Window window = new() { Content = tree, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Controls.TreeViewItem item = Assert.IsType<Controls.TreeViewItem>(tree.ItemContainerGenerator.ContainerFromIndex(0));
                    item.IsSelectionChecked = true;
                    Assert.Contains("Old", tree.SelectedItems.Cast<object>());
                    tree.ItemsSource = replacementItems;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Empty(tree.SelectedItems);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TreeView_MultipleSelection_CollapsedBoundChildrenPreserveParentUntilEmptyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                System.Collections.ObjectModel.ObservableCollection<string> children = ["First", "Second"];
                Controls.TreeView tree = new()
                {
                    SelectionMode = TreeViewSelectionMode.Multiple,
                    ItemsSource = new[] { children },
                    ItemTemplate = new HierarchicalDataTemplate { ItemsSource = new System.Windows.Data.Binding() },
                };
                Window window = new() { Content = tree, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Controls.TreeViewItem parent = Assert.IsType<Controls.TreeViewItem>(tree.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.False(parent.IsExpanded);
                    Assert.Equal(2, parent.Items.Count);
                    Assert.Null(parent.ItemContainerGenerator.ContainerFromIndex(0));
                    parent.IsSelectionChecked = true;
                    Assert.Same(children, Assert.Single(tree.SelectedItems));

                    children.RemoveAt(0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(parent.IsSelectionChecked);
                    Assert.Same(children, Assert.Single(tree.SelectedItems));
                    Assert.Null(parent.ItemContainerGenerator.ContainerFromIndex(0));

                    children.Add("Third");
                    children[0] = "Replacement";
                    children.Move(0, 1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(parent.IsSelectionChecked);
                    Assert.Same(children, Assert.Single(tree.SelectedItems));
                    Assert.False(parent.IsExpanded);
                    Assert.Null(parent.ItemContainerGenerator.ContainerFromIndex(0));

                    children.Clear();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(parent.IsSelectionChecked);
                    Assert.Empty(tree.SelectedItems);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task TreeView_MultipleSelection_UncheckedAdditionDoesNotRebuildSelectionAsync(bool addChild)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Controls.TreeViewItem selected = new() { Header = "Selected" };
                Controls.TreeViewItem parent = new() { Header = "Parent", IsExpanded = true };
                Controls.TreeView tree = new() { SelectionMode = TreeViewSelectionMode.Multiple };
                _ = tree.Items.Add(selected);
                _ = tree.Items.Add(parent);
                Window window = new() { Content = tree, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    selected.IsSelectionChecked = true;
                    System.Collections.IEnumerator selection = tree.SelectedItems.GetEnumerator();
                    ItemsControl owner = addChild ? parent : tree;
                    _ = owner.Items.Add(new Controls.TreeViewItem { Header = "Unchecked" });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // Clearing and repopulating the live selection invalidates its enumerator,
                    // even if it ends up with the same values. An unrelated addition needs neither.
                    Assert.True(selection.MoveNext());
                    Assert.Same(selected, selection.Current);
                    Assert.False(selection.MoveNext());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
        [Fact]
        public Task TreeView_MultipleSelection_AdditionsWithSelectionStateStillReconcileAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TreeViewItem parent = new() { Header = "Parent", IsExpanded = true };
                Controls.TreeViewItem selected = new() { Header = "Selected" };
                _ = parent.Items.Add(selected);
                Controls.TreeView tree = new() { SelectionMode = TreeViewSelectionMode.Multiple };
                _ = tree.Items.Add(parent);
                Window window = new() { Content = tree, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    parent.IsSelectionChecked = true;
                    _ = parent.Items.Add(new Controls.TreeViewItem { Header = "Unchecked" });
                    Assert.Null(parent.IsSelectionChecked);
                    Assert.Same(selected, Assert.Single(tree.SelectedItems));

                    Controls.TreeViewItem addedParent = new() { Header = "Added parent" };
                    Controls.TreeViewItem addedSelected = new() { Header = "Added selected", IsSelectionChecked = true };
                    _ = addedParent.Items.Add(addedSelected);
                    _ = tree.Items.Add(addedParent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Contains(selected, tree.SelectedItems.Cast<object>());
                    Assert.Contains(addedSelected, tree.SelectedItems.Cast<object>());
                    Assert.DoesNotContain(parent, tree.SelectedItems.Cast<object>());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
        [Fact]
        public Task TreeView_MultipleSelectionCascadesAndComputesParentStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.TreeViewItem parent = new() { Header = "Documents", IsExpanded = true };
                    Controls.TreeViewItem first = new() { Header = "Contracts" };
                    Controls.TreeViewItem second = new() { Header = "Invoices" };
                    Controls.TreeViewItem third = new() { Header = "Receipts" };
                    _ = parent.Items.Add(first);
                    _ = parent.Items.Add(second);
                    _ = parent.Items.Add(third);

                    Controls.TreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple,
                    };
                    _ = treeView.Items.Add(parent);
                    window.Content = treeView;
                    window.Width = 320;
                    window.Height = 240;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    parent.IsSelectionChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(true, first.IsSelectionChecked);
                    Assert.Equal(true, second.IsSelectionChecked);
                    Assert.Equal(true, third.IsSelectionChecked);
                    Assert.Contains(parent, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(first, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(second, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(third, treeView.SelectedItems.Cast<object>());

                    second.IsSelectionChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Null(parent.IsSelectionChecked);
                    Assert.DoesNotContain(parent, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(first, treeView.SelectedItems.Cast<object>());
                    Assert.DoesNotContain(second, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(third, treeView.SelectedItems.Cast<object>());

                    first.IsSelectionChecked = false;
                    third.IsSelectionChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(false, parent.IsSelectionChecked);
                    Assert.Empty(treeView.SelectedItems);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
