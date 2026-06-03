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
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled list view with animated item states.
    /// </summary>
    public class ListView : System.Windows.Controls.ListView
    {
        // Animation durations for item insertions and removals.
        private static readonly Duration InsertDuration = new(TimeSpan.FromMilliseconds(250));
        private static readonly Duration RemoveDuration = new(TimeSpan.FromMilliseconds(200));

        /// <summary>
        /// Initializes static members of the ListView class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ListView control uses its own default style
        /// as defined in the application's resources. This is important for proper theming and appearance in WPF
        /// applications.</remarks>
        static ListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ListView),
                new FrameworkPropertyMetadata(typeof(ListView)));
        }

        /// <summary>
        /// Identifies the <see cref="ItemAnimationsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemAnimationsEnabledProperty =
            DependencyProperty.Register(
                nameof(ItemAnimationsEnabled),
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether item animations are enabled.
        /// </summary>
        public bool ItemAnimationsEnabled
        {
            get => (bool)GetValue(ItemAnimationsEnabledProperty);
            set => SetValue(ItemAnimationsEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HoverHighlightEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverHighlightEnabledProperty =
            DependencyProperty.Register(
                nameof(HoverHighlightEnabled),
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether hover highlighting is enabled.
        /// </summary>
        public bool HoverHighlightEnabled
        {
            get => (bool)GetValue(HoverHighlightEnabledProperty);
            set => SetValue(HoverHighlightEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ViewState"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewStateProperty =
            DependencyProperty.Register(
                nameof(ViewState),
                typeof(ListViewState),
                typeof(ListView),
                new FrameworkPropertyMetadata(ListViewState.Default));

        /// <summary>
        /// Gets or sets the view state of the list view.
        /// </summary>
        public ListViewState ViewState
        {
            get => (ListViewState)GetValue(ViewStateProperty);
            set => SetValue(ViewStateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ListView),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the list view.
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
                typeof(ListView),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Content displayed when the list has no items.
        /// </summary>
        public object EmptyContent
        {
            get => GetValue(EmptyContentProperty);
            set => SetValue(EmptyContentProperty, value);
        }

        /// <summary>
        /// Attached property mirrored from the parent <see cref="ListView"/> so item templates can use
        /// <c>MultiDataTrigger</c> (each condition must use a <c>Binding</c>, not <c>Property</c>, in WPF).
        /// </summary>
        public static readonly DependencyProperty ParentIsItemSelectableProperty =
            DependencyProperty.RegisterAttached(
                "ParentIsItemSelectable",
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Sets the parent list's <see cref="IsItemSelectable"/> value on an item container for template triggers.
        /// </summary>
        /// <param name="element">The item container that receives the mirrored selection state.</param>
        /// <param name="value"><c>true</c> when the parent list allows item selection; otherwise <c>false</c>.</param>
        public static void SetParentIsItemSelectable(DependencyObject element, bool value)
        {
            element.SetValue(ParentIsItemSelectableProperty, value);
        }

        /// <summary>
        /// Gets whether the parent list allows item selection (for template triggers).
        /// </summary>
        /// <param name="element">The item container that stores the mirrored selection state.</param>
        /// <returns><c>true</c> when the parent list allows item selection; otherwise <c>false</c>.</returns>
        public static bool GetParentIsItemSelectable(DependencyObject element)
        {
            return (bool)element.GetValue(ParentIsItemSelectableProperty);
        }

        /// <summary>
        /// Identifies the <see cref="IsItemSelectable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsItemSelectableProperty =
            DependencyProperty.Register(
                nameof(IsItemSelectable),
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true, OnIsItemSelectableChanged));

        /// <summary>
        /// Gets or sets whether items can be selected and show hover/selection visuals.
        /// When false, rows are display-only; scrolling and item animations are unchanged.
        /// </summary>
        public bool IsItemSelectable
        {
            get => (bool)GetValue(IsItemSelectableProperty);
            set => SetValue(IsItemSelectableProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListView"/> class and wires the loaded event for default group styling.
        /// </summary>
        public ListView()
        {
            Loaded += OnListViewLoaded;
        }

        /// <summary>
        /// Animates out the item and then calls the provided callback.
        /// </summary>
        /// <param name="item">The item to remove from the list or bound <see cref="IList"/> after the exit animation.</param>
        /// <param name="onCompleted">An optional callback invoked after removal completes.</param>
        public void AnimateRemove(object item, Action? onCompleted)
        {
            if (!ItemAnimationsEnabled)
            {
                RemoveItem(item);
                onCompleted?.Invoke();
                return;
            }
            if (ItemContainerGenerator.ContainerFromItem(item) is not UIElement container)
            {
                RemoveItem(item);
                onCompleted?.Invoke();
                return;
            }

            if (container.RenderTransform is not TranslateTransform)
            {
                container.RenderTransform = new TranslateTransform();
            }
            DoubleAnimation opacityAnim = new(container.Opacity, 0, RemoveDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            DoubleAnimation slideAnim = new(0, -12, RemoveDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            opacityAnim.Completed += (s, e) =>
            {
                RemoveItem(item);
                onCompleted?.Invoke();
            };
            container.BeginAnimation(OpacityProperty, opacityAnim);
            container.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (!IsItemSelectable && !_suppressSelectionChange)
            {
                _suppressSelectionChange = true;
                try
                {
                    UnselectAll();
                }
                finally
                {
                    _suppressSelectionChange = false;
                }
                return;
            }
            base.OnSelectionChanged(e);
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListViewItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ListViewItem;
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            SetParentIsItemSelectable(element, IsItemSelectable);
            if (element is UIElement ui)
            {
                ui.Focusable = IsItemSelectable;
            }
            if (!ItemAnimationsEnabled || !IsLoaded)
            {
                return;
            }
            if (element is not UIElement container)
            {
                return;
            }

            container.RenderTransform = new TranslateTransform(0, 12); container.Opacity = 0;
            DoubleAnimation opacityAnim = new(0, 1, InsertDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation slideAnim = new(12, 0, InsertDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            container.BeginAnimation(OpacityProperty, opacityAnim);
            container.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        private static void OnIsItemSelectableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListView listView = (ListView)d;
            if (!(bool)e.NewValue)
            {
                listView._suppressSelectionChange = true;
                try
                {
                    listView.UnselectAll();
                }
                finally
                {
                    listView._suppressSelectionChange = false;
                }
            }
            listView.UpdateItemContainersFocusable();
        }

        private void UpdateItemContainersFocusable()
        {
            // Only realized containers exist when virtualization is active. Future
            // containers receive the same mirrored value in PrepareContainerForItemOverride.
            foreach (object? item in Items)
            {
                if (ItemContainerGenerator.ContainerFromItem(item) is DependencyObject container)
                {
                    SetParentIsItemSelectable(container, IsItemSelectable);
                    if (container is UIElement ui)
                    {
                        ui.Focusable = IsItemSelectable;
                    }
                }
            }
        }

        private void OnListViewLoaded(object sender, RoutedEventArgs e)
        {
            EnsureDefaultGroupStyle();
        }

        private void EnsureDefaultGroupStyle()
        {
            if (GroupStyle.Count > 0)
            {
                return;
            }
            if (TryFindResource("ListViewGroupItemStyle") is Style style)
            {
                GroupStyle.Add(new GroupStyle { ContainerStyle = style });
            }
        }

        private void RemoveItem(object item)
        {
            if (ItemsSource is IList list)
            {
                list.Remove(item);
                return;
            }
            Items.Remove(item);
        }

        /// <summary>
        /// Indicates whether selection change events should be suppressed.
        /// </summary>
        /// <remarks>When set to true, selection change notifications are temporarily disabled. This is
        /// typically used to prevent event handlers from responding to programmatic changes that should not trigger
        /// selection logic.</remarks>
        private bool _suppressSelectionChange;
    }
}
