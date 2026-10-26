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
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

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
    /// <c language="xaml">PART_ScrollBackButton</c> and <c language="xaml">PART_ScrollForwardButton</c> are automatically
    /// shown or hidden based on whether the tab strip overflows the available width.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_AddTabButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_ScrollBackButton, Type = typeof(System.Windows.Controls.Primitives.RepeatButton))]
    [TemplatePart(Name = PART_ScrollForwardButton, Type = typeof(System.Windows.Controls.Primitives.RepeatButton))]
    [TemplatePart(Name = PART_TabContentScroller, Type = typeof(ScrollViewer))]
    public class TabView : TabControl
    {
        // Template part names - must match names in the default control template.
        private const string PART_AddTabButton = "PART_AddTabButton";
        private const string PART_ScrollBackButton = "PART_ScrollBackButton";
        private const string PART_ScrollForwardButton = "PART_ScrollForwardButton";
        private const string PART_TabContentScroller = "PART_TabContentScroller";

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
            typeof(EventHandler<TabViewTabCloseRequestedEventArgs>),
            typeof(TabView));

        static TabView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TabView),
                new FrameworkPropertyMetadata(typeof(TabView)));
            FlowDirectionProperty.OverrideMetadata(typeof(TabView), new FrameworkPropertyMetadata(FlowDirection.LeftToRight, OnFlowDirectionChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabView"/> class and subscribes to child
        /// <see cref="TabViewItem.CloseRequested"/> events for aggregation.
        /// </summary>
        public TabView()
        {
            AddHandler(TabViewItem.CloseRequestedEvent, new EventHandler<TabViewTabCloseRequestedEventArgs>(OnChildCloseRequested));
            PreviewKeyDown += OnTabPreviewKeyDown;
            KeyDown += OnTabKeyDown;
            Loaded += OnTabLoaded;
            Unloaded += OnTabUnloaded;
            SizeChanged += OnTabSizeChanged;
            ItemContainerGenerator.StatusChanged += OnKeyboardContainersStatusChanged;
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
        public event EventHandler<TabViewTabCloseRequestedEventArgs> TabCloseRequested
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
            CancelSelectedHeaderVisibility();
            base.OnApplyTemplate();
            _addTabButton?.Click -= OnAddTabButtonClick;
            _scrollBackButton?.Click -= OnScrollBackClick;
            _scrollForwardButton?.Click -= OnScrollForwardClick;
            _tabContentScroller?.ScrollChanged -= OnTabScrollChanged;
            _tabContentScroller?.SizeChanged -= OnTabScrollerSizeChanged;
            _addTabButton = GetTemplateChild(PART_AddTabButton) as ButtonBase;
            _scrollBackButton = GetTemplateChild(PART_ScrollBackButton) as System.Windows.Controls.Primitives.RepeatButton;
            _scrollForwardButton = GetTemplateChild(PART_ScrollForwardButton) as System.Windows.Controls.Primitives.RepeatButton;
            _tabContentScroller = GetTemplateChild(PART_TabContentScroller) as ScrollViewer;
            _addTabButton?.Click += OnAddTabButtonClick;
            _scrollBackButton?.Click += OnScrollBackClick;
            _scrollForwardButton?.Click += OnScrollForwardClick;
            if (_tabContentScroller is not null)
            {
                AttachScrollerHandlers();
                UpdateScrollButtonVisibility();
            }
            ReconcileKeyboardHeaders();
            ScheduleSelectedHeaderVisibility();
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

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            // Containers are generated lazily during layout, after OnItemsChanged has run, so
            // the freshly prepared container must trigger its own leading-separator refresh.
            // This also covers ItemsSource-bound items, where the container is not the data item
            // itself and RelativeSource PreviousData binding has no adjacency information to bind to.
            UpdateLeadingSeparators();
            if (element is TabViewItem header)
            {
                TrackKeyboardHeader(header);
                UpdateAddButtonTabStop();
            }
            ScheduleSelectedHeaderVisibility();
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            // Adds, removes, moves, and resets can all change which tab is first or adjacent to
            // the selection; refresh the realized containers immediately (new containers refresh
            // again when prepared).
            UpdateLeadingSeparators();
            ReconcileKeyboardHeaders();
            ScheduleSelectedHeaderVisibility();
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // The selected tab and its immediate neighbors need their leading separator
            // recomputed whenever the selection moves.
            UpdateLeadingSeparators();
            ScheduleSelectedHeaderVisibility();
        }

        private static void OnFlowDirectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((TabView)sender).ScheduleSelectedHeaderVisibility();
        }

        private void OnTabLoaded(object sender, RoutedEventArgs e)
        {
            AttachScrollerHandlers();
            ReconcileKeyboardHeaders();
            ScheduleSelectedHeaderVisibility();
        }

        private void OnTabUnloaded(object sender, RoutedEventArgs e)
        {
            CancelSelectedHeaderVisibility();
            foreach (TabViewItem header in _keyboardHeaders)
            {
                UntrackKeyboardHeader(header);
            }
            _keyboardHeaders.Clear();
            _tabContentScroller?.ScrollChanged -= OnTabScrollChanged;
            _tabContentScroller?.SizeChanged -= OnTabScrollerSizeChanged;
        }

        private void AttachScrollerHandlers()
        {
            if (_tabContentScroller is not null)
            {
                _tabContentScroller.ScrollChanged -= OnTabScrollChanged;
                _tabContentScroller.SizeChanged -= OnTabScrollerSizeChanged;
                _tabContentScroller.ScrollChanged += OnTabScrollChanged;
                _tabContentScroller.SizeChanged += OnTabScrollerSizeChanged;
            }
        }

        private void OnTabScrollerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleSelectedHeaderVisibility(preserveManualScroll: true);
        }

        private void OnTabSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleSelectedHeaderVisibility();
        }

        private void OnSelectedHeaderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleSelectedHeaderVisibility();
        }

        private void OnSelectedContainerStatusChanged(object? sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status is GeneratorStatus.ContainersGenerated)
            {
                ScheduleSelectedHeaderVisibility();
            }
        }

        private void CancelSelectedHeaderVisibility()
        {
            CancelPendingHeaderVisibility();
            _selectedHeader?.SizeChanged -= OnSelectedHeaderSizeChanged;
            _selectedHeader = null;
        }

        private void CancelPendingHeaderVisibility()
        {
            _visibilityGeneration++;
            _ = _selectedHeaderOperation?.Abort();
            _selectedHeaderOperation = null;
            ItemContainerGenerator.StatusChanged -= OnSelectedContainerStatusChanged;
        }

        private void ScheduleSelectedHeaderVisibility(bool preserveManualScroll = false)
        {
            if (preserveManualScroll && _manualStripScroll)
            {
                return;
            }
            _manualStripScroll = false;
            CancelSelectedHeaderVisibility();
            if (!IsLoaded || _tabContentScroller is not { } scroller || SelectedItem is not { } selected)
            {
                return;
            }

            int generation = _visibilityGeneration;
            _selectedHeaderOperation = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (generation != _visibilityGeneration || !IsLoaded
                    || !ReferenceEquals(scroller, _tabContentScroller) || !ReferenceEquals(selected, SelectedItem))
                {
                    return;
                }
                _selectedHeaderOperation = null;
                TabViewItem? header = selected as TabViewItem ?? ItemContainerGenerator.ContainerFromItem(selected) as TabViewItem;
                if (header is null)
                {
                    if (ItemContainerGenerator.Status is not GeneratorStatus.ContainersGenerated)
                    {
                        ItemContainerGenerator.StatusChanged += OnSelectedContainerStatusChanged;
                    }
                    return;
                }
                _selectedHeader = header;
                header.SizeChanged += OnSelectedHeaderSizeChanged;
                EnsureSelectedHeaderVisible(header, scroller);
            }));
        }

        private static void EnsureSelectedHeaderVisible(TabViewItem header, ScrollViewer scroller)
        {
            FrameworkElement viewport = scroller.Template?.FindName("PART_ScrollContentPresenter", scroller) as FrameworkElement ?? scroller;
            double width = scroller.ViewportWidth;
            if (width <= 0 || !header.IsVisible || !header.IsArrangeValid || !viewport.IsAncestorOf(header))
            {
                return;
            }

            Rect bounds = header.TransformToAncestor(viewport).TransformBounds(new Rect(header.RenderSize));
            double delta = 0;
            if (bounds.Width > width || bounds.Left < 0)
            {
                // In the inherited-flow strip, WPF mirrors above the viewport. Its local
                // left edge is the physical leading edge in both LTR and RTL.
                delta = bounds.Left;
            }
            else if (bounds.Right > width)
            {
                delta = bounds.Right - width;
            }

            double offset = Math.Max(0, Math.Min(scroller.ScrollableWidth, scroller.HorizontalOffset + delta));
            if (Math.Abs(offset - scroller.HorizontalOffset) > 0.5)
            {
                scroller.ScrollToHorizontalOffset(offset);
            }
        }

        private void OnTabPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || Keyboard.Modifiers is not ModifierKeys.None
                || _addTabButton is not { IsVisible: true, IsEnabled: true })
            {
                return;
            }

            TabViewItem? last = GetLastKeyboardHeader();
            Key forward = FlowDirection is FlowDirection.LeftToRight ? Key.Right : Key.Left;
            Key back = FlowDirection is FlowDirection.LeftToRight ? Key.Left : Key.Right;
            if (last is not null && e.Key == forward && ReferenceEquals(Keyboard.FocusedElement, last))
            {
                e.Handled = _addTabButton.Focus();
            }
            else if (last is not null && e.Key == back && ReferenceEquals(Keyboard.FocusedElement, _addTabButton))
            {
                e.Handled = last.Focus();
            }
        }

        private void OnTabKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled && TryRequestClose(e.Key, Keyboard.Modifiers))
            {
                e.Handled = true;
            }
        }

        internal bool TryRequestClose(Key key, ModifierKeys modifiers)
        {
            if (key is not Key.F4 || modifiers is not ModifierKeys.Control || SelectedItem is null)
            {
                return false;
            }

            TabViewItem? selected = SelectedItem as TabViewItem ?? ItemContainerGenerator.ContainerFromItem(SelectedItem) as TabViewItem;
            if (selected is not { IsClosable: true })
            {
                return false;
            }
            selected.RequestClose();
            return true;
        }

        private TabViewItem? GetLastKeyboardHeader()
        {
            for (int index = Items.Count - 1; index >= 0; index--)
            {
                if (ItemContainerGenerator.ContainerFromIndex(index) is TabViewItem { IsVisible: true, IsEnabled: true, Focusable: true } item)
                {
                    return item;
                }
            }
            return null;
        }

        private void OnKeyboardContainersStatusChanged(object? sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status is GeneratorStatus.ContainersGenerated)
            {
                ReconcileKeyboardHeaders();
            }
        }

        private void ReconcileKeyboardHeaders()
        {
            HashSet<TabViewItem> current = [];
            for (int index = 0; index < Items.Count; index++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(index) is TabViewItem header)
                {
                    _ = current.Add(header);
                }
            }
            foreach (TabViewItem header in _keyboardHeaders)
            {
                if (!current.Contains(header))
                {
                    UntrackKeyboardHeader(header);
                }
            }
            _keyboardHeaders.IntersectWith(current);
            foreach (TabViewItem header in current)
            {
                TrackKeyboardHeader(header);
            }
            UpdateAddButtonTabStop();
        }

        private void TrackKeyboardHeader(TabViewItem header)
        {
            if (_keyboardHeaders.Add(header))
            {
                header.IsVisibleChanged += OnHeaderKeyboardAvailabilityChanged;
                header.IsEnabledChanged += OnHeaderKeyboardAvailabilityChanged;
                header.FocusableChanged += OnHeaderKeyboardAvailabilityChanged;
            }
        }

        private void UntrackKeyboardHeader(TabViewItem header)
        {
            header.IsVisibleChanged -= OnHeaderKeyboardAvailabilityChanged;
            header.IsEnabledChanged -= OnHeaderKeyboardAvailabilityChanged;
            header.FocusableChanged -= OnHeaderKeyboardAvailabilityChanged;
        }

        private void OnHeaderKeyboardAvailabilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateAddButtonTabStop();
        }

        private void UpdateAddButtonTabStop()
        {
            _addTabButton?.SetCurrentValue(IsTabStopProperty, GetLastKeyboardHeader() is null);
        }

        /// <summary>
        /// Reapplies <see cref="TabViewItem.LeadingSeparatorVisibility"/> across all realized
        /// containers: the separator is collapsed for the first tab in the strip and for either
        /// tab flanking the current selection, matching the WinUI TabViewItemSeparator behavior.
        /// </summary>
        private void UpdateLeadingSeparators()
        {
            int lastIndex = Items.Count - 1;
            for (int index = 0; index <= lastIndex; index++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(index) is not TabViewItem container)
                {
                    continue;
                }

                bool isFirst = index is 0;
                bool isSelected = index == SelectedIndex;
                bool previousIsSelected = index > 0 && index - 1 == SelectedIndex;
                container.LeadingSeparatorVisibility = isFirst || isSelected || previousIsSelected
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private void OnAddTabButtonClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(AddTabButtonClickEvent, this));
        }

        private void OnScrollBackClick(object sender, RoutedEventArgs e)
        {
            _manualStripScroll = true;
            CancelPendingHeaderVisibility();
            _tabContentScroller?.ScrollToHorizontalOffset(Math.Max(0, _tabContentScroller.HorizontalOffset - ScrollAmount));
        }

        private void OnScrollForwardClick(object sender, RoutedEventArgs e)
        {
            _manualStripScroll = true;
            CancelPendingHeaderVisibility();
            _tabContentScroller?.ScrollToHorizontalOffset(Math.Min(_tabContentScroller.ScrollableWidth, _tabContentScroller.HorizontalOffset + ScrollAmount));
        }

        private void OnTabScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateScrollButtonVisibility();
            if (e.ExtentWidthChange is not 0 || e.ViewportWidthChange is not 0)
            {
                ScheduleSelectedHeaderVisibility(preserveManualScroll: true);
            }
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

        private void OnChildCloseRequested(object? sender, TabViewTabCloseRequestedEventArgs e)
        {
            // Consumers should handle one aggregate close request from TabView rather
            // than both the child TabViewItem event and the forwarded parent event.
            TabViewTabCloseRequestedEventArgs forwarded = new(TabCloseRequestedEvent, this, e.Tab, e.Item);
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
        private System.Windows.Controls.Primitives.RepeatButton? _scrollBackButton;

        /// <summary>
        /// Represents the button used to scroll content forward in the associated control.
        /// </summary>
        /// <remarks>This field typically refers to a UI element that enables users to initiate a forward
        /// scroll action, such as in a scrollable list or viewer. The field may be null if the button is not
        /// initialized or present in the control template.</remarks>
        private System.Windows.Controls.Primitives.RepeatButton? _scrollForwardButton;

        /// <summary>
        /// Represents the scroll viewer used to display the tab content area.
        /// </summary>
        private ScrollViewer? _tabContentScroller;
        private DispatcherOperation? _selectedHeaderOperation;
        private TabViewItem? _selectedHeader;
        private int _visibilityGeneration;
        private bool _manualStripScroll;
        private readonly HashSet<TabViewItem> _keyboardHeaders = [];
    }
}
