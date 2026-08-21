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
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A <see cref="TabControl"/> aligned with the WinUI 3 TabView: per-tab close buttons, a trailing
    /// "add" (+) button, horizontally scrollable tab strip with scroll navigation buttons, and
    /// WinUI-styled selection indicator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TabView"/> produces <see cref="TabViewItem"/> containers by default. Consumers can bind
    /// to <see cref="ItemsControl.ItemsSource"/> and optionally supply an <see cref="ItemsControl.ItemTemplate"/>
    /// to render header content for each item; the icon and close-button chrome are always supplied by the
    /// container template.
    /// </para>
    /// <para>
    /// Listen to <see cref="AddTabButtonClick"/> to create new tabs and to <see cref="TabCloseRequested"/>
    /// to remove a tab; this control does not itself mutate the items collection so applications remain in
    /// full control of their data model.
    /// </para>
    /// <para>
    /// <c>PART_ScrollBackButton</c> and <c>PART_ScrollForwardButton</c> are automatically
    /// shown or hidden based on whether the tab strip overflows the available width.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PartAddTabButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartScrollBackButton, Type = typeof(RepeatButton))]
    [TemplatePart(Name = PartScrollForwardButton, Type = typeof(RepeatButton))]
    [TemplatePart(Name = PartTabContentScroller, Type = typeof(ScrollViewer))]
    public class TabView : TabControl
    {
        // Template part names - must match names in the default control template.
        private const string PartAddTabButton = "PART_AddTabButton";
        private const string PartScrollBackButton = "PART_ScrollBackButton";
        private const string PartScrollForwardButton = "PART_ScrollForwardButton";
        private const string PartTabContentScroller = "PART_TabContentScroller";

        // Scroll amount for each click of the scroll navigation buttons. This is a fixed value rather than
        private const double ScrollAmount = 200.0;

        /// <summary>
        /// Identifies the <see cref="IsAddTabButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAddTabButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsAddTabButtonVisible),
                typeof(bool),
                typeof(TabView),
                new PropertyMetadata(defaultValue: true));

        /// <summary>
        /// Identifies the <see cref="TabWidthMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TabWidthModeProperty =
            DependencyProperty.Register(
                nameof(TabWidthMode),
                typeof(TabViewWidthMode),
                typeof(TabView),
                new PropertyMetadata(TabViewWidthMode.SizeToContent));

        /// <summary>
        /// Identifies the <see cref="CloseButtonOverlayMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonOverlayModeProperty =
            DependencyProperty.Register(
                nameof(CloseButtonOverlayMode),
                typeof(TabViewCloseButtonOverlayMode),
                typeof(TabView),
                new PropertyMetadata(TabViewCloseButtonOverlayMode.Auto));

        /// <summary>
        /// Identifies the <see cref="AddTabButtonClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent AddTabButtonClickEvent = EventManager.RegisterRoutedEvent(
            nameof(AddTabButtonClick),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(TabView));

        /// <summary>
        /// Identifies the <see cref="TabCloseRequested"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TabCloseRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(TabCloseRequested),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(TabView));

        static TabView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TabView),
                new FrameworkPropertyMetadata(typeof(TabView)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabView"/> class and subscribes to child
        /// <see cref="TabViewItem.CloseRequested"/> events for aggregation.
        /// </summary>
        public TabView()
        {
            AddHandler(TabViewItem.CloseRequestedEvent, new RoutedEventHandler(OnChildCloseRequested));
        }

        /// <summary>
        /// Gets or sets whether the trailing add-tab (+) button is shown.
        /// </summary>
        public bool IsAddTabButtonVisible
        {
            get => (bool)GetValue(IsAddTabButtonVisibleProperty);
            set => SetValue(IsAddTabButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets how tab widths are distributed in the tab strip.
        /// </summary>
        public TabViewWidthMode TabWidthMode
        {
            get => (TabViewWidthMode)GetValue(TabWidthModeProperty);
            set => SetValue(TabWidthModeProperty, value);
        }

        /// <summary>
        /// Gets or sets when per-tab close buttons are shown on this control's items.
        /// </summary>
        public TabViewCloseButtonOverlayMode CloseButtonOverlayMode
        {
            get => (TabViewCloseButtonOverlayMode)GetValue(CloseButtonOverlayModeProperty);
            set => SetValue(CloseButtonOverlayModeProperty, value);
        }

        /// <summary>
        /// Raised when the user clicks the add-tab (+) button. Consumers typically insert a new item
        /// into their items collection and optionally select it.
        /// </summary>
        [SuppressMessage("Design", "S3908", Justification = "RoutedEventHandler is required by WPF's routed event infrastructure.")]
        public event RoutedEventHandler AddTabButtonClick
        {
            add => AddHandler(AddTabButtonClickEvent, value);
            remove => RemoveHandler(AddTabButtonClickEvent, value);
        }

        /// <summary>
        /// Raised when the user clicks the close (×) button of a <see cref="TabViewItem"/>. The event
        /// args include the container and the bound item; consumers decide whether to remove it.
        /// </summary>
        [SuppressMessage("Design", "S3908", Justification = "RoutedEventHandler is required by WPF's routed event infrastructure.")]
        public event RoutedEventHandler TabCloseRequested
        {
            add => AddHandler(TabCloseRequestedEvent, value);
            remove => RemoveHandler(TabCloseRequestedEvent, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Templates can be replaced at runtime. Detach old part handlers before
            // reading the new template parts so repeated ApplyTemplate calls do not
            // accumulate event subscriptions.
            base.OnApplyTemplate();
            _addTabButton?.Click -= OnAddTabButtonClick;
            _scrollBackButton?.Click -= OnScrollBackClick;
            _scrollForwardButton?.Click -= OnScrollForwardClick;
            _tabContentScroller?.ScrollChanged -= OnTabScrollChanged;
            _addTabButton = GetTemplateChild(PartAddTabButton) as ButtonBase;
            _scrollBackButton = GetTemplateChild(PartScrollBackButton) as RepeatButton;
            _scrollForwardButton = GetTemplateChild(PartScrollForwardButton) as RepeatButton;
            _tabContentScroller = GetTemplateChild(PartTabContentScroller) as ScrollViewer;
            _addTabButton?.Click += OnAddTabButtonClick;
            _scrollBackButton?.Click += OnScrollBackClick;
            _scrollForwardButton?.Click += OnScrollForwardClick;
            if (_tabContentScroller is not null)
            {
                _tabContentScroller.ScrollChanged += OnTabScrollChanged;
                UpdateScrollButtonVisibility();
            }
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TabViewItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TabViewItem;
        }

        private void OnAddTabButtonClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(AddTabButtonClickEvent, this));
        }

        private void OnScrollBackClick(object sender, RoutedEventArgs e)
        {
            _tabContentScroller?.ScrollToHorizontalOffset(Math.Max(0, _tabContentScroller.HorizontalOffset - ScrollAmount));
        }

        private void OnScrollForwardClick(object sender, RoutedEventArgs e)
        {
            _tabContentScroller?.ScrollToHorizontalOffset(Math.Min(_tabContentScroller.ScrollableWidth, _tabContentScroller.HorizontalOffset + ScrollAmount));
        }

        private void OnTabScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateScrollButtonVisibility();
        }

        private void UpdateScrollButtonVisibility()
        {
            if (_tabContentScroller is null)
            {
                return;
            }
            bool hasOverflow = _tabContentScroller.ScrollableWidth > 0;
            bool canScrollBack = _tabContentScroller.HorizontalOffset > 0;
            bool canScrollForward = _tabContentScroller.HorizontalOffset < _tabContentScroller.ScrollableWidth;
            _ = _scrollBackButton?.Visibility = (hasOverflow && canScrollBack)
                ? Visibility.Visible
                : Visibility.Collapsed;
            _ = _scrollForwardButton?.Visibility = (hasOverflow && canScrollForward)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OnChildCloseRequested(object sender, RoutedEventArgs e)
        {
            if (e is not TabViewTabCloseRequestedEventArgs inner)
            {
                return;
            }

            // Consumers should handle one aggregate close request from TabView rather
            // than both the child TabViewItem event and the forwarded parent event.
            TabViewTabCloseRequestedEventArgs forwarded = new(TabCloseRequestedEvent, this, inner.Tab, inner.Item);
            RaiseEvent(forwarded);
            e.Handled = true;
        }

        /// <summary>
        /// Represents the button control used to add a new tab.
        /// </summary>
        private ButtonBase? _addTabButton;

        /// <summary>
        /// Represents the button used to scroll backward in the associated control.
        /// </summary>
        private RepeatButton? _scrollBackButton;

        /// <summary>
        /// Represents the button used to scroll content forward in the associated control.
        /// </summary>
        /// <remarks>This field typically refers to a UI element that enables users to initiate a forward
        /// scroll action, such as in a scrollable list or viewer. The field may be null if the button is not
        /// initialized or present in the control template.</remarks>
        private RepeatButton? _scrollForwardButton;

        /// <summary>
        /// Represents the scroll viewer used to display the tab content area.
        /// </summary>
        private ScrollViewer? _tabContentScroller;
    }
}
