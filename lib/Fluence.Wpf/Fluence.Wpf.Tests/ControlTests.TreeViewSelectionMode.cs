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

using System.Linq;
using System.Windows;
using System.Windows.Input;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public void TreeView_DefaultSelectionModeIsSingleWithLiveSelectedItems()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    Controls.TreeView treeView = new();

                    Assert.Equal(TreeViewSelectionMode.Single, treeView.SelectionMode);
                    Assert.NotNull(treeView.SelectedItems);
                    Assert.Empty(treeView.SelectedItems);
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void TreeView_MultipleSelectionShowsCheckboxAndSyncsSelectedItems()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.CheckBox? firstCheckBox = FindVisualChildByName<System.Windows.Controls.CheckBox>(first, "SelectionCheckBox");
                    Assert.NotNull(firstCheckBox);
                    Assert.Equal(Visibility.Visible, firstCheckBox.Visibility);
                    Assert.True(firstCheckBox.IsThreeState,
                        "Multiple-selection TreeViewItem checkbox should support indeterminate parent state.");

                    first.IsSelectionChecked = true;
                    second.IsSelectionChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(2, treeView.SelectedItems.Count);
                    Assert.Contains(first, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(second, treeView.SelectedItems.Cast<object>());

                    first.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    _ = Assert.Single(treeView.SelectedItems);
                    Assert.DoesNotContain(first, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(second, treeView.SelectedItems.Cast<object>());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void TreeView_MultipleSelectionSpaceTogglesItemCheckState()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = treeView.ApplyTemplate();
                    _ = item.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TreeViewItem keyboardItem =
                        treeView.ItemContainerGenerator.ContainerFromItem(item) as Controls.TreeViewItem ?? item;

                    _ = keyboardItem.ApplyTemplate();
                    _ = keyboardItem.Focus();
                    _ = Keyboard.Focus(keyboardItem);
                    DrainDispatcher(window.Dispatcher);
                    keyboardItem.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void TreeView_NoneSelectionHidesCheckboxAndClearsSelection()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);

                    item.IsSelectionChecked = true;
                    DrainDispatcher(window.Dispatcher);
                    _ = Assert.Single(treeView.SelectedItems);

                    treeView.SelectionMode = TreeViewSelectionMode.None;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.CheckBox? checkBox = FindVisualChildByName<System.Windows.Controls.CheckBox>(item, "SelectionCheckBox");
                    Assert.NotNull(checkBox);
                    Assert.Equal(Visibility.Collapsed, checkBox.Visibility);
                    Assert.Equal(false, item.IsSelectionChecked);
                    Assert.Empty(treeView.SelectedItems);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void TreeView_MultipleSelectionCascadesAndComputesParentState()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    parent.IsSelectionChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(true, first.IsSelectionChecked);
                    Assert.Equal(true, second.IsSelectionChecked);
                    Assert.Equal(true, third.IsSelectionChecked);
                    Assert.Contains(parent, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(first, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(second, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(third, treeView.SelectedItems.Cast<object>());

                    second.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.Null(parent.IsSelectionChecked);
                    Assert.DoesNotContain(parent, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(first, treeView.SelectedItems.Cast<object>());
                    Assert.DoesNotContain(second, treeView.SelectedItems.Cast<object>());
                    Assert.Contains(third, treeView.SelectedItems.Cast<object>());

                    first.IsSelectionChecked = false;
                    third.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(false, parent.IsSelectionChecked);
                    Assert.Empty(treeView.SelectedItems);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
