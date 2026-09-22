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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A horizontal row of peer destinations, mirroring the WinUI 3
    /// <c language="csharp">SelectorBar</c>: one <see cref="SelectorBarItem"/> per destination,
    /// exactly one of them selected, with an accent pill under the selected label. Use it to
    /// switch between sibling views of the same page rather than to host the views themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The control derives from <see cref="ListBox"/>, so selection, keyboard navigation, and
    /// the selection automation pattern come from the framework. WinUI's SelectorBar is always
    /// single-select, so <see cref="System.Windows.Controls.ListBox.SelectionMode"/> is coerced to
    /// <see cref="SelectionMode.Single"/> and a consumer cannot widen it.
    /// </para>
    /// <para>
    /// Items may be declared inline as <see cref="SelectorBarItem"/> elements or come from
    /// <see cref="ItemsControl.ItemsSource"/>. For a generated container the label is bound to
    /// <see cref="ItemsControl.DisplayMemberPath"/> when one is set (honouring
    /// <see cref="ItemsControl.ItemStringFormat"/>) and taken from the item's own string form
    /// otherwise, because the WinUI-shaped <see cref="SelectorBarItem.Text"/> is what the default
    /// template presents rather than the inherited content.
    /// </para>
    /// </remarks>
    public class SelectorBar : ListBox
    {
        /// <summary>
        /// Guards the re-entrant selection write in <see cref="OnSelectionChanged"/>, so putting
        /// the previous item back does not recurse through the change it raises.
        /// </summary>
        private bool _restoringSelection;

        /// <summary>
        /// Initializes static members of the SelectorBar class, overrides the default style
        /// metadata so the control picks up its themed template from Generic.xaml, and pins
        /// <see cref="System.Windows.Controls.ListBox.SelectionMode"/> to single selection.
        /// </summary>
        static SelectorBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SelectorBar),
                new FrameworkPropertyMetadata(typeof(SelectorBar)));
            SelectionModeProperty.OverrideMetadata(
                typeof(SelectorBar),
                new FrameworkPropertyMetadata(
                    SelectionMode.Single,
                    propertyChangedCallback: null,
                    CoerceSelectionMode));
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new SelectorBarItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is SelectorBarItem;
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is not SelectorBarItem container || ReferenceEquals(container, item))
            {
                return;
            }

            // The template presents Text, not Content, so a container generated for a data item
            // needs its label filled in. A Text the consumer set through ItemContainerStyle or
            // an item template wins: only an unset one is filled.
            if (container.ReadLocalValue(SelectorBarItem.TextProperty) != DependencyProperty.UnsetValue)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(DisplayMemberPath))
            {
                container.SetCurrentValue(SelectorBarItem.TextProperty, ResolveItemText(item));
                return;
            }

            // DisplayMemberPath has to be bound rather than read: ItemsControl turns the path into
            // an ItemTemplateSelector, leaving the container's Content as the raw data item, and
            // the template never presents Content. The binding also keeps the label live when the
            // named property raises PropertyChanged.
            Binding binding = new(DisplayMemberPath)
            {
                Source = item,
                StringFormat = ItemStringFormat,
            };
            _ = container.SetBinding(SelectorBarItem.TextProperty, binding);
        }

        /// <summary>
        /// Returns the label for a generated container with no
        /// <see cref="ItemsControl.DisplayMemberPath"/>: the data item's own string form.
        /// </summary>
        /// <param name="item">The data item the container was generated for.</param>
        /// <returns>The label text, or null when the item has none.</returns>
        private static string? ResolveItemText(object? item)
        {
            return item switch
            {
                null => null,
                string text => text,
                IFormattable formattable => formattable.ToString(format: null, CultureInfo.CurrentCulture),
                _ => item.ToString(),
            };
        }

        /// <inheritdoc />
        /// <remarks>
        /// The bar never settles with nothing selected. <see cref="SelectorBarItem"/> already
        /// swallows the two deselect gestures WPF's <see cref="ListBox"/> honours in
        /// <see cref="SelectionMode.Single"/>, so this is the net under anything else that empties
        /// the selection: the item that just left is put back, because the pill is the page's
        /// current destination and WinUI's SelectorBar has no state without one.
        /// </remarks>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_restoringSelection || SelectedItem is not null || Items.Count is 0 || e.RemovedItems.Count is 0)
            {
                return;
            }

            object? previous = e.RemovedItems[0];
            if (previous is null || !Items.Contains(previous))
            {
                return;
            }

            _restoringSelection = true;
            try
            {
                SetCurrentValue(SelectedItemProperty, previous);
            }
            finally
            {
                _restoringSelection = false;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// WinUI's SelectorBar selects the current item, or the first one, when the bar takes
        /// focus with nothing selected (SelectorBar.cpp OnGotFocus), so a keyboard user never
        /// lands on a bar with no pill.
        /// </remarks>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            if (SelectedIndex >= 0 || Items.Count is 0)
            {
                return;
            }

            // The item focus actually landed on comes first, as WinUI's does; index 0 is only the
            // fallback, and a disabled item is skipped rather than selected.
            object? target = ItemFromFocusedContainer(e.NewFocus as DependencyObject)
                ?? FirstSelectableItem();

            if (target is not null)
            {
                SetCurrentValue(SelectedItemProperty, target);
            }
        }

        /// <summary>
        /// Walks up from the newly focused element to the item container it belongs to and returns
        /// the item that container carries, when the container can take the selection.
        /// </summary>
        /// <param name="focused">The element that took focus.</param>
        /// <returns>The item to select, or <see langword="null"/> when there is none.</returns>
        private object? ItemFromFocusedContainer(DependencyObject? focused)
        {
            DependencyObject? current = focused;
            while (current is not null && !ReferenceEquals(current, this))
            {
                if (current is SelectorBarItem container && container.IsEnabled)
                {
                    object item = ItemContainerGenerator.ItemFromContainer(container);
                    return ReferenceEquals(item, DependencyProperty.UnsetValue) ? container : item;
                }

                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        /// <summary>
        /// Returns the first item whose container is enabled, so keyboard focus never parks the
        /// pill on a disabled destination.
        /// </summary>
        /// <returns>The first selectable item, or <see langword="null"/> when every item is disabled.</returns>
        private object? FirstSelectableItem()
        {
            foreach (object? item in Items)
            {
                if (ItemContainerGenerator.ContainerFromItem(item) is SelectorBarItem container && !container.IsEnabled)
                {
                    continue;
                }

                return item;
            }

            return null;
        }

        /// <summary>
        /// Keeps <see cref="System.Windows.Controls.ListBox.SelectionMode"/> at
        /// <see cref="SelectionMode.Single"/>: WinUI's SelectorBar has no
        /// multi-select mode, and the accent pill reads as one current destination.
        /// </summary>
        /// <param name="d">The selector bar the value was set on.</param>
        /// <param name="baseValue">The value the consumer asked for.</param>
        /// <returns>Always <see cref="SelectionMode.Single"/>.</returns>
        private static object CoerceSelectionMode(DependencyObject d, object baseValue)
        {
            return SelectionMode.Single;
        }
    }
}
