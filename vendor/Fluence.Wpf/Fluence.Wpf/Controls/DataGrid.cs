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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents a table of bound rows and columns built on the WPF
    /// <see cref="System.Windows.Controls.DataGrid"/>. It covers the WinUI 3 <c language="csharp">TableView</c>
    /// scenarios with WPF names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Column types, sorting, resizing, reordering, frozen leading columns, editing,
    /// <see cref="System.ComponentModel.INotifyDataErrorInfo"/> validation, grouping and filtering through
    /// <see cref="System.ComponentModel.ICollectionView"/>, and row virtualization work as they do in WPF.
    /// </para>
    /// <para>
    /// When the grid loads and <see cref="ItemsControl.GroupStyle"/> is empty, the grid shows each group under
    /// a collapsible header. <see cref="ExpandAllGroups"/> and <see cref="CollapseAllGroups"/> open or close
    /// those headers.
    /// </para>
    /// <para>
    /// A frozen <see cref="SolidColorBrush"/> assigned to <see cref="System.Windows.Controls.DataGrid.HorizontalGridLinesBrush"/>
    /// or <see cref="System.Windows.Controls.DataGrid.VerticalGridLinesBrush"/> reads back as a grid-owned copy
    /// with the same color.
    /// </para>
    /// </remarks>
    public class DataGrid : System.Windows.Controls.DataGrid
    {
        private const string GroupExpanderName = "GroupExpander";
        private const string GroupItemStyleKey = "DataGridGroupItemStyle";
        private const string TextElementStyleKey = "DataGridTextColumnElementStyle";
        private const string TextEditingElementStyleKey = "DataGridTextColumnEditingElementStyle";
        private const string CheckBoxElementStyleKey = "DataGridCheckBoxColumnElementStyle";
        private const string CheckBoxEditingElementStyleKey = "DataGridCheckBoxColumnEditingElementStyle";

        private bool _clearSelectionPending;
        private SolidColorBrush? _horizontalGridLinesBrush;
        private SolidColorBrush? _verticalGridLinesBrush;

        /// <summary>
        /// Initializes static members of the <see cref="DataGrid"/> class, overrides the default style
        /// key so the themed template in Generic.xaml applies, and keeps the grid line brushes stable
        /// across theme changes.
        /// </summary>
        static DataGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DataGrid),
                new FrameworkPropertyMetadata(typeof(DataGrid)));

            // Only the coerce callback is supplied, so the native default value and the native
            // property-changed callback are kept.
            HorizontalGridLinesBrushProperty.OverrideMetadata(
                typeof(DataGrid),
                new FrameworkPropertyMetadata(propertyChangedCallback: null, CoerceHorizontalGridLinesBrush));
            VerticalGridLinesBrushProperty.OverrideMetadata(
                typeof(DataGrid),
                new FrameworkPropertyMetadata(propertyChangedCallback: null, CoerceVerticalGridLinesBrush));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGrid"/> class.
        /// </summary>
        public DataGrid()
        {
            Columns.CollectionChanged += OnColumnsCollectionChanged;
            Loaded += OnDataGridLoaded;
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the grid's outer border.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EmptyContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EmptyContentProperty =
            DependencyProperty.Register(
                nameof(EmptyContent),
                typeof(object),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the content shown over the grid while it has no rows. Set
        /// <see cref="System.Windows.Controls.DataGrid.CanUserAddRows"/> to <see langword="false"/> when the
        /// source is editable; otherwise the new-item row counts as a row.
        /// </summary>
        public object? EmptyContent
        {
            get => GetValue(EmptyContentProperty);
            set => SetValue(EmptyContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsItemSelectable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsItemSelectableProperty =
            DependencyProperty.Register(
                nameof(IsItemSelectable),
                typeof(bool),
                typeof(DataGrid),
                new FrameworkPropertyMetadata(defaultValue: true, OnIsItemSelectableChanged));

        /// <summary>
        /// Gets or sets a value indicating whether rows and cells can be selected. Setting it to
        /// <see langword="false"/> clears the selection and suppresses the hover and selection visuals; it
        /// is the equivalent of WinUI TableView <c language="csharp">SelectionMode="None"</c>. Cells stay
        /// focusable, so keyboard navigation works, and F2 or typing still begins editing. A mouse click does
        /// not begin editing, because the native cell starts a click edit only when it is selected. A click
        /// briefly selects the row or cell and then clears it, so
        /// <see cref="System.Windows.Controls.DataGrid.SelectedCellsChanged"/>, and
        /// <see cref="System.Windows.Controls.Primitives.Selector.SelectionChanged"/> when whole rows are
        /// selected, report a selection and then its removal. With row selection, a two-way
        /// <see cref="System.Windows.Controls.Primitives.Selector.SelectedItem"/> binding sees a transient value.
        /// </summary>
        public bool IsItemSelectable
        {
            get => (bool)GetValue(IsItemSelectableProperty);
            set => SetValue(IsItemSelectableProperty, value);
        }

        /// <summary>
        /// Expands every realized group header of this grid that uses the built-in group header. Groups of a grid
        /// nested in a cell or the row details are left as they are.
        /// </summary>
        /// <remarks>
        /// Grouping turns off row virtualization unless <see cref="VirtualizingPanel.IsVirtualizingWhenGroupingProperty"/>
        /// is set, so by default every group is realized.
        /// </remarks>
        public void ExpandAllGroups()
        {
            SetGroupsExpanded(isExpanded: true);
        }

        /// <summary>
        /// Collapses every realized group header of this grid that uses the built-in group header. Groups of a grid
        /// nested in a cell or the row details are left as they are.
        /// </summary>
        public void CollapseAllGroups()
        {
            SetGroupsExpanded(isExpanded: false);
        }

        /// <summary>
        /// Clears a new row selection when <see cref="IsItemSelectable"/> is <see langword="false"/>.
        /// </summary>
        /// <param name="e">The selection change.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (!IsItemSelectable && e.AddedItems.Count > 0)
            {
                ScheduleClearSelection();
            }
        }

        /// <summary>
        /// Clears a new cell selection when <see cref="IsItemSelectable"/> is <see langword="false"/>.
        /// </summary>
        /// <param name="e">The selected-cells change.</param>
        protected override void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e)
        {
            base.OnSelectedCellsChanged(e);
            if (!IsItemSelectable && e.AddedCells.Count > 0)
            {
                ScheduleClearSelection();
            }
        }

        /// <summary>
        /// Keeps one grid-owned brush for the horizontal grid lines. Every theme or accent publish replaces
        /// the frozen theme brushes, and the native grid regenerates all row containers, ending an open cell
        /// edit, whenever the line brush instance changes. Returning the same brush with the new color keeps
        /// the edit and still repaints the lines.
        /// </summary>
        /// <param name="d">The grid.</param>
        /// <param name="baseValue">The value before coercion.</param>
        /// <returns>The grid-owned brush for a frozen solid brush, otherwise <paramref name="baseValue"/>.</returns>
        private static object CoerceHorizontalGridLinesBrush(DependencyObject d, object baseValue)
        {
            DataGrid grid = (DataGrid)d;
            return KeepGridLineBrush(baseValue, ref grid._horizontalGridLinesBrush);
        }

        /// <summary>
        /// Keeps one grid-owned brush for the vertical grid lines for the same reason as the horizontal lines.
        /// </summary>
        /// <param name="d">The grid.</param>
        /// <param name="baseValue">The value before coercion.</param>
        /// <returns>The grid-owned brush for a frozen solid brush, otherwise <paramref name="baseValue"/>.</returns>
        private static object CoerceVerticalGridLinesBrush(DependencyObject d, object baseValue)
        {
            DataGrid grid = (DataGrid)d;
            return KeepGridLineBrush(baseValue, ref grid._verticalGridLinesBrush);
        }

        private static object KeepGridLineBrush(object baseValue, ref SolidColorBrush? owned)
        {
            // Theme brushes are frozen. A brush an application set that is unfrozen or not solid passes
            // through, so the native behavior stays for it.
            if (baseValue is not SolidColorBrush { IsFrozen: true } themeBrush)
            {
                return baseValue;
            }

            if (owned is null)
            {
                owned = new SolidColorBrush(themeBrush.Color) { Opacity = themeBrush.Opacity };
            }
            else
            {
                owned.Color = themeBrush.Color;
                owned.Opacity = themeBrush.Opacity;
            }

            return owned;
        }

        private static void OnIsItemSelectableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                ((DataGrid)d).UnselectAllCells();
            }
        }

        private void ScheduleClearSelection()
        {
            // The native selection bookkeeping is not re-entrant, so the selection is cleared after the
            // current change completes. Send priority runs before the next render pass.
            if (_clearSelectionPending)
            {
                return;
            }

            _clearSelectionPending = true;
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(ClearPendingSelection));
        }

        private void ClearPendingSelection()
        {
            _clearSelectionPending = false;
            if (!IsItemSelectable)
            {
                UnselectAllCells();
            }
        }

        private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is null)
            {
                return;
            }

            foreach (object item in e.NewItems)
            {
                if (item is DataGridColumn column)
                {
                    ApplyDefaultColumnStyles(column);
                }
            }
        }

        private void OnDataGridLoaded(object sender, RoutedEventArgs e)
        {
            EnsureDefaultGroupStyle();

            // Columns declared before the grid joined a tree may not have found the theme resources yet.
            foreach (DataGridColumn column in Columns)
            {
                ApplyDefaultColumnStyles(column);
            }
        }

        private void EnsureDefaultGroupStyle()
        {
            if (GroupStyle.Count > 0)
            {
                return;
            }

            if (TryFindResource(GroupItemStyleKey) is Style style && AppliesTo(style, typeof(GroupItem)))
            {
                GroupStyle.Add(new GroupStyle { ContainerStyle = style });
            }
        }

        private static bool AppliesTo(Style style, Type elementType)
        {
            // An application resource with the same key but another TargetType throws when it is applied.
            Type? target = style.TargetType;
            return target?.IsAssignableFrom(elementType) ?? true;
        }

        private void ApplyDefaultColumnStyles(DataGridColumn column)
        {
            if (column is DataGridCheckBoxColumn)
            {
                ApplyColumnStyle(column, DataGridBoundColumn.ElementStyleProperty, CheckBoxElementStyleKey, typeof(System.Windows.Controls.CheckBox));
                ApplyColumnStyle(column, DataGridBoundColumn.EditingElementStyleProperty, CheckBoxEditingElementStyleKey, typeof(System.Windows.Controls.CheckBox));
            }
            else if (column is DataGridTextColumn)
            {
                ApplyColumnStyle(column, DataGridBoundColumn.ElementStyleProperty, TextElementStyleKey, typeof(System.Windows.Controls.TextBlock));
                ApplyColumnStyle(column, DataGridBoundColumn.EditingElementStyleProperty, TextEditingElementStyleKey, typeof(System.Windows.Controls.TextBox));
            }
        }

        private void ApplyColumnStyle(DataGridColumn column, DependencyProperty property, string resourceKey, Type elementType)
        {
            // The text and check box columns override the metadata default with their native element
            // styles, so an unset style is not null; it is a value whose base source is still Default.
            // A style the application set locally has another source and always wins.
            if (DependencyPropertyHelper.GetValueSource(column, property).BaseValueSource is BaseValueSource.Default
                && TryFindResource(resourceKey) is Style style
                && AppliesTo(style, elementType))
            {
                column.SetCurrentValue(property, style);
            }
        }

        private void SetGroupsExpanded(bool isExpanded)
        {
            foreach (GroupItem groupItem in FindGroupItems(this))
            {
                if (groupItem.Template?.FindName(GroupExpanderName, groupItem) is System.Windows.Controls.Expander expander)
                {
                    expander.SetCurrentValue(System.Windows.Controls.Expander.IsExpandedProperty, isExpanded);
                }
            }
        }

        private static IEnumerable<GroupItem> FindGroupItems(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is GroupItem groupItem)
                {
                    yield return groupItem;
                }

                // A nested ItemsControl, such as a grid in a cell template or the row details, owns its
                // own groups. Multilevel grouping nests GroupItems with no ItemsControl between them,
                // so the walk still reaches every level of this grid's groups.
                if (child is ItemsControl)
                {
                    continue;
                }

                foreach (GroupItem nested in FindGroupItems(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
