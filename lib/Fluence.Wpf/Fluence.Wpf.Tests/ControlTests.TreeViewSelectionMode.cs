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
using System.Windows;
using System.Windows.Input;
using FluentTreeView = Fluence.Wpf.Controls.TreeView;
using FluentTreeViewItem = Fluence.Wpf.Controls.TreeViewItem;
using WpfCheckBox = System.Windows.Controls.CheckBox;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void TreeView_DefaultSelectionModeIsSingleWithLiveSelectedItems()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    FluentTreeView treeView = new();

                    Assert.AreEqual(TreeViewSelectionMode.Single, treeView.SelectionMode);
                    Assert.IsNotNull(treeView.SelectedItems, "SelectedItems should be a live collection.");
                    Assert.AreEqual(0, treeView.SelectedItems.Count);
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

        [TestMethod]
        public void TreeView_MultipleSelectionShowsCheckboxAndSyncsSelectedItems()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    FluentTreeViewItem first = new() { Header = "First" };
                    FluentTreeViewItem second = new() { Header = "Second" };
                    FluentTreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple
                    };
                    _ = treeView.Items.Add(first);
                    _ = treeView.Items.Add(second);
                    window.Content = treeView;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfCheckBox? firstCheckBox = FindVisualChildByName<WpfCheckBox>(first, "SelectionCheckBox");
                    Assert.IsNotNull(firstCheckBox, "Multiple-selection TreeViewItem template should expose a checkbox.");
                    Assert.AreEqual(Visibility.Visible, firstCheckBox.Visibility,
                        "TreeViewItem checkbox should be visible when the owning TreeView is in Multiple mode.");
                    Assert.IsTrue(firstCheckBox.IsThreeState,
                        "Multiple-selection TreeViewItem checkbox should support indeterminate parent state.");

                    first.IsSelectionChecked = true;
                    second.IsSelectionChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(2, treeView.SelectedItems.Count);
                    CollectionAssert.Contains(treeView.SelectedItems, first);
                    CollectionAssert.Contains(treeView.SelectedItems, second);

                    first.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1, treeView.SelectedItems.Count);
                    CollectionAssert.DoesNotContain(treeView.SelectedItems, first);
                    CollectionAssert.Contains(treeView.SelectedItems, second);
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

        [TestMethod]
        public void TreeView_MultipleSelectionSpaceTogglesItemCheckState()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    FluentTreeViewItem item = new() { Header = "Contracts" };
                    FluentTreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple
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

                    FluentTreeViewItem keyboardItem =
                        treeView.ItemContainerGenerator.ContainerFromItem(item) as FluentTreeViewItem ?? item;

                    _ = keyboardItem.ApplyTemplate();
                    _ = keyboardItem.Focus();
                    _ = Keyboard.Focus(keyboardItem);
                    DrainDispatcher(window.Dispatcher);
                    keyboardItem.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(keyboardItem.ToggleMultipleSelectionFromKeyboard(),
                        "Focused TreeViewItem should accept Space in Multiple selection mode.");

                    Assert.AreEqual(true, keyboardItem.IsSelectionChecked,
                        "Space should check a focused TreeViewItem in Multiple selection mode.");
                    CollectionAssert.Contains(treeView.SelectedItems, item);

                    Assert.IsTrue(keyboardItem.ToggleMultipleSelectionFromKeyboard(),
                        "Focused TreeViewItem should accept Space again in Multiple selection mode.");

                    Assert.AreEqual(false, keyboardItem.IsSelectionChecked,
                        "Space should uncheck a focused TreeViewItem in Multiple selection mode.");
                    CollectionAssert.DoesNotContain(treeView.SelectedItems, item);
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

        [TestMethod]
        public void TreeView_NoneSelectionHidesCheckboxAndClearsSelection()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    FluentTreeViewItem item = new() { Header = "Leaf" };
                    FluentTreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple
                    };
                    _ = treeView.Items.Add(item);
                    window.Content = treeView;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);

                    item.IsSelectionChecked = true;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(1, treeView.SelectedItems.Count);

                    treeView.SelectionMode = TreeViewSelectionMode.None;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfCheckBox? checkBox = FindVisualChildByName<WpfCheckBox>(item, "SelectionCheckBox");
                    Assert.IsNotNull(checkBox, "TreeViewItem template should keep the checkbox part available.");
                    Assert.AreEqual(Visibility.Collapsed, checkBox.Visibility,
                        "TreeViewItem checkbox should be hidden when selection is disabled.");
                    Assert.AreEqual(false, item.IsSelectionChecked,
                        "TreeViewItem checked state should be cleared when SelectionMode=None.");
                    Assert.AreEqual(0, treeView.SelectedItems.Count,
                        "TreeView.SelectedItems should be cleared when SelectionMode=None.");
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

        [TestMethod]
        public void TreeView_MultipleSelectionCascadesAndComputesParentState()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    FluentTreeViewItem parent = new() { Header = "Documents", IsExpanded = true };
                    FluentTreeViewItem first = new() { Header = "Contracts" };
                    FluentTreeViewItem second = new() { Header = "Invoices" };
                    FluentTreeViewItem third = new() { Header = "Receipts" };
                    _ = parent.Items.Add(first);
                    _ = parent.Items.Add(second);
                    _ = parent.Items.Add(third);

                    FluentTreeView treeView = new()
                    {
                        SelectionMode = TreeViewSelectionMode.Multiple
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

                    Assert.AreEqual(true, first.IsSelectionChecked);
                    Assert.AreEqual(true, second.IsSelectionChecked);
                    Assert.AreEqual(true, third.IsSelectionChecked);
                    CollectionAssert.Contains(treeView.SelectedItems, parent);
                    CollectionAssert.Contains(treeView.SelectedItems, first);
                    CollectionAssert.Contains(treeView.SelectedItems, second);
                    CollectionAssert.Contains(treeView.SelectedItems, third);

                    second.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsNull(parent.IsSelectionChecked,
                        "Parent should become indeterminate when fewer than all child items are checked.");
                    CollectionAssert.DoesNotContain(treeView.SelectedItems, parent);
                    CollectionAssert.Contains(treeView.SelectedItems, first);
                    CollectionAssert.DoesNotContain(treeView.SelectedItems, second);
                    CollectionAssert.Contains(treeView.SelectedItems, third);

                    first.IsSelectionChecked = false;
                    third.IsSelectionChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(false, parent.IsSelectionChecked,
                        "Parent should be unchecked when none of its child items are checked.");
                    Assert.AreEqual(0, treeView.SelectedItems.Count);
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
