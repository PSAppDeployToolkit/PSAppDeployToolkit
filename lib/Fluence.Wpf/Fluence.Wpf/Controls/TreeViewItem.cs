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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design tree view item with full-row hover highlight, animated chevron,
    /// and WinUI 3-canonical background brush states.
    /// Authority: WinUI 3 TreeView_themeresources.xaml + TreeViewItem.xaml.
    /// </summary>
    [TemplatePart(Name = PART_Header, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_ItemsHost, Type = typeof(ItemsPresenter))]
    public class TreeViewItem : System.Windows.Controls.TreeViewItem
    {
        // Template part names for the header and items host elements in the control template.
        private const string PART_Header = "PART_Header";
        private const string PART_ItemsHost = "ItemsHost";
        private const string SelectionCheckBoxPart = "SelectionCheckBox";

        /// <summary>
        /// Initializes static members of the TreeViewItem class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that TreeViewItem uses its own style by default,
        /// rather than inheriting the style from its base class. This is important for applying custom control
        /// templates and visual styles specific to TreeViewItem.</remarks>
        static TreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TreeViewItem),
                new FrameworkPropertyMetadata(typeof(TreeViewItem)));
        }

        /// <summary>
        /// Identifies the <see cref="IsSelectionChecked"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSelectionCheckedProperty =
            DependencyProperty.Register(
                nameof(IsSelectionChecked),
                typeof(bool?),
                typeof(TreeViewItem),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsSelectionCheckedChanged));

        /// <summary>
        /// Gets or sets whether this item is checked in a multiple-selection tree view.
        /// A <see langword="null"/> value represents an indeterminate parent state.
        /// </summary>
        public bool? IsSelectionChecked
        {
            get => (bool?)GetValue(IsSelectionCheckedProperty);
            set => SetValue(IsSelectionCheckedProperty, value);
        }

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
        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);

            TreeView? owner = FindOwningTreeView();
            if (owner is null)
            {
                return;
            }

            if (owner.SelectionMode == TreeViewSelectionMode.None)
            {
                SetCurrentValue(IsSelectedProperty, false);
            }
            else if (owner.SelectionMode == TreeViewSelectionMode.Multiple)
            {
                SetCurrentValue(IsSelectionCheckedProperty, true);
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space && ToggleMultipleSelectionFromKeyboard())
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space && ToggleMultipleSelectionFromKeyboard())
            {
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        internal void CoerceSelectionForOwner(TreeView? owner)
        {
            if (owner is null)
            {
                return;
            }

            if (owner.SelectionMode != TreeViewSelectionMode.Multiple && IsSelectionChecked != false)
            {
                SetCurrentValue(IsSelectionCheckedProperty, false);
            }
        }

        private static void OnIsSelectionCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TreeViewItem item)
            {
                return;
            }

            TreeView? owner = item.FindOwningTreeView();
            if (owner is null)
            {
                return;
            }

            bool? isChecked = (bool?)e.NewValue;
            if (owner.SelectionMode != TreeViewSelectionMode.Multiple)
            {
                if (isChecked != false && !item._isKeyboardSelectionToggle)
                {
                    item.SetCurrentValue(IsSelectionCheckedProperty, false);
                }

                return;
            }

            owner.UpdateSelectionFromItem(item, isChecked);
        }

        internal bool ToggleMultipleSelectionFromKeyboard()
        {
            TreeView? owner = FindOwningTreeView();
            if (owner is null)
            {
                CheckBox? selectionCheckBox = GetTemplateChild(SelectionCheckBoxPart) as CheckBox;
                if (selectionCheckBox?.Visibility != Visibility.Visible)
                {
                    return false;
                }
            }
            else if (owner.SelectionMode != TreeViewSelectionMode.Multiple)
            {
                return false;
            }

            bool nextState = IsSelectionChecked != true;
            _isKeyboardSelectionToggle = true;
            try
            {
                SetValue(IsSelectionCheckedProperty, nextState);
                if (GetTemplateChild(SelectionCheckBoxPart) is CheckBox checkBox)
                {
                    checkBox.SetValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, nextState);
                }
            }
            finally
            {
                _isKeyboardSelectionToggle = false;
            }

            return true;
        }

        private TreeView? FindOwningTreeView()
        {
            ItemsControl? owner = ItemsControlFromItemContainer(this);
            if (owner is null)
            {
                return FindAncestorTreeView(this);
            }

            while (owner is TreeViewItem treeViewItem)
            {
                owner = ItemsControlFromItemContainer(treeViewItem);
            }

            return owner as TreeView ?? FindAncestorTreeView(this);
        }

        private static TreeView? FindAncestorTreeView(DependencyObject source)
        {
            DependencyObject? current = LogicalTreeHelper.GetParent(source) ?? GetVisualParent(source);
            while (current is not null)
            {
                if (current is TreeView treeView)
                {
                    return treeView;
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

        private bool _isKeyboardSelectionToggle;
    }
}
