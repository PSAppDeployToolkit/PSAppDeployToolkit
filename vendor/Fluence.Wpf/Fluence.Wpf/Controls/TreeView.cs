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

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design tree view with Fluent hover, selection, and expand/collapse visuals.
    /// Items are represented by <see cref="TreeViewItem"/> containers.
    /// Authority: WinUI 3 TreeView_themeresources.xaml.
    /// </summary>
    public class TreeView : System.Windows.Controls.TreeView
    {
        private readonly ArrayList _selectedItems = [];
        private bool _updatingSelectionChecks;

        /// <summary>
        /// Initializes static members of the TreeView class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the TreeView control uses its own style by
        /// default, rather than inheriting the style from its base class. This is important for custom control theming
        /// in WPF.</remarks>
        static TreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TreeView),
                new FrameworkPropertyMetadata(typeof(TreeView)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeView"/> class.
        /// </summary>
        public TreeView()
        {
            SelectedItems = _selectedItems;
        }

        /// <summary>
        /// Identifies the <see cref="SelectionMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(
                nameof(SelectionMode),
                typeof(TreeViewSelectionMode),
                typeof(TreeView),
                new FrameworkPropertyMetadata(
                    TreeViewSelectionMode.Single,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnSelectionModeChanged));

        /// <summary>
        /// Gets or sets the selection mode used by the tree view.
        /// </summary>
        public TreeViewSelectionMode SelectionMode
        {
            get => (TreeViewSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        /// <summary>
        /// Gets the live list of currently selected items.
        /// </summary>
        public IList SelectedItems { get; }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeViewItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItem;
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is TreeViewItem treeViewItem)
            {
                treeViewItem.CoerceSelectionForOwner(this);
            }
        }

        /// <inheritdoc />
        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);

            if (SelectionMode is TreeViewSelectionMode.Single)
            {
                _selectedItems.Clear();
                if (e.NewValue is not null)
                {
                    _ = _selectedItems.Add(e.NewValue);
                }
            }
            else if (SelectionMode is TreeViewSelectionMode.None)
            {
                _selectedItems.Clear();
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key is Key.Space && ToggleMultipleSelectionItem(e.OriginalSource as DependencyObject))
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        internal void UpdateSelectionFromItem(TreeViewItem item, bool? isSelected)
        {
            if (_updatingSelectionChecks)
            {
                return;
            }

            if (SelectionMode is TreeViewSelectionMode.None)
            {
                _selectedItems.Clear();
                if (item.IsSelectionChecked is not false)
                {
                    item.SetCurrentValue(TreeViewItem.IsSelectionCheckedProperty, value: false);
                }

                return;
            }

            object selectedItem = GetSelectedItemValue(item);

            if (SelectionMode is TreeViewSelectionMode.Single)
            {
                _selectedItems.Clear();
                if (isSelected is true)
                {
                    _ = _selectedItems.Add(selectedItem);
                    item.SetCurrentValue(System.Windows.Controls.TreeViewItem.IsSelectedProperty, value: true);
                }

                return;
            }

            _updatingSelectionChecks = true;
            try
            {
                if (isSelected is not null)
                {
                    ApplySelectionStateToDescendants(item, isSelected.Value);
                }

                UpdateAncestorSelectionStates(item);
                RebuildMultipleSelectedItems();
            }
            finally
            {
                _updatingSelectionChecks = false;
            }
        }

        internal void ApplySelectionModeToItems()
        {
            if (SelectionMode is TreeViewSelectionMode.None)
            {
                _selectedItems.Clear();
            }
            else if (SelectionMode is TreeViewSelectionMode.Single)
            {
                _selectedItems.Clear();
                if (SelectedItem is not null)
                {
                    _ = _selectedItems.Add(SelectedItem);
                }
            }

            ApplySelectionModeToContainers(this);
        }

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                treeView.ApplySelectionModeToItems();
            }
        }

        private bool ToggleMultipleSelectionItem(DependencyObject? source)
        {
            if (SelectionMode is not TreeViewSelectionMode.Multiple)
            {
                return false;
            }

            TreeViewItem? item = FindTreeViewItemFromSource(source);
            if (item is null)
            {
                return false;
            }

            item.SetCurrentValue(TreeViewItem.IsSelectionCheckedProperty, item.IsSelectionChecked is not true);
            return true;
        }

        private static TreeViewItem? FindTreeViewItemFromSource(DependencyObject? source)
        {
            DependencyObject? current = source;
            while (current is not null)
            {
                if (current is TreeViewItem item)
                {
                    return item;
                }

                current = LogicalTreeHelper.GetParent(current) ?? GetVisualParent(current);
            }

            return null;
        }

        private static DependencyObject? GetVisualParent(DependencyObject source)
        {
            return source is Visual or Visual3D
                ? VisualTreeHelper.GetParent(source)
                : null;
        }

        private static void ApplySelectionModeToContainers(ItemsControl owner)
        {
            foreach (object item in owner.Items)
            {
                TreeViewItem? container = owner.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                container ??= item as TreeViewItem;

                if (container is null)
                {
                    continue;
                }

                TreeView? treeView = FindOwningTreeView(container);
                if (treeView is not null && treeView.SelectionMode is not TreeViewSelectionMode.Multiple)
                {
                    container.SetCurrentValue(TreeViewItem.IsSelectionCheckedProperty, value: false);
                }

                container.CoerceSelectionForOwner(treeView);
                ApplySelectionModeToContainers(container);
            }
        }

        private static TreeView? FindOwningTreeView(DependencyObject item)
        {
            ItemsControl? owner = ItemsControlFromItemContainer(item);

            while (owner is TreeViewItem treeViewItem)
            {
                owner = ItemsControlFromItemContainer(treeViewItem);
            }

            return owner as TreeView;
        }

        private static void ApplySelectionStateToDescendants(TreeViewItem item, bool isSelected)
        {
            foreach (object child in item.Items)
            {
                TreeViewItem? container = item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                container ??= child as TreeViewItem;

                if (container is null)
                {
                    continue;
                }

                container.SetCurrentValue(TreeViewItem.IsSelectionCheckedProperty, isSelected);
                ApplySelectionStateToDescendants(container, isSelected);
            }
        }

        private static void UpdateAncestorSelectionStates(TreeViewItem item)
        {
            ItemsControl? owner = ItemsControlFromItemContainer(item);
            while (owner is TreeViewItem parent)
            {
                parent.SetCurrentValue(TreeViewItem.IsSelectionCheckedProperty, GetChildSelectionState(parent));
                owner = ItemsControlFromItemContainer(parent);
            }
        }

        private static bool? GetChildSelectionState(TreeViewItem parent)
        {
            int childCount = 0;
            int checkedCount = 0;
            bool hasPartialChild = false;

            foreach (object child in parent.Items)
            {
                TreeViewItem? container = parent.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                container ??= child as TreeViewItem;

                if (container is null)
                {
                    continue;
                }

                childCount++;
                if (container.IsSelectionChecked is true)
                {
                    checkedCount++;
                }
                else if (container.IsSelectionChecked is null)
                {
                    hasPartialChild = true;
                }
            }

            return childCount switch
            {
                0 => false,
                _ when checkedCount == childCount => true,
                _ when checkedCount is 0 && !hasPartialChild => false,
                _ => null,
            };
        }

        private void RebuildMultipleSelectedItems()
        {
            _selectedItems.Clear();
            AddCheckedItems(this);
        }

        private void AddCheckedItems(ItemsControl owner)
        {
            foreach (object item in owner.Items)
            {
                TreeViewItem? container = owner.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                container ??= item as TreeViewItem;

                if (container is null)
                {
                    continue;
                }

                if (container.IsSelectionChecked is true)
                {
                    object selectedItem = GetSelectedItemValue(container);
                    if (!_selectedItems.Contains(selectedItem))
                    {
                        _ = _selectedItems.Add(selectedItem);
                    }
                }

                AddCheckedItems(container);
            }
        }

        private static object GetSelectedItemValue(TreeViewItem item)
        {
            ItemsControl? owner = ItemsControlFromItemContainer(item);
            object? generatedItem = owner?.ItemContainerGenerator.ItemFromContainer(item);

            return generatedItem is not null && generatedItem != DependencyProperty.UnsetValue
                ? generatedItem
                : item;
        }
    }
}
