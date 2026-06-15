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

using Fluence.Wpf.Automation;
using Fluence.Wpf.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A navigation control with a collapsible pane and content area, similar to WinUI NavigationView.
    /// Uses a single shared selection indicator that animates between items.
    /// </summary>
    [TemplatePart(Name = PartBackButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PartContentPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartPaneItemsScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PartPaneToggleButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PartSelectionIndicator, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartFooterItemsHost, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PartFooterSelectionIndicator, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartPaneColumn, Type = typeof(ColumnDefinition))]
    [TemplatePart(Name = PartTopItemsHost, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartTopOverflowButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonVisible")]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonCollapsed")]
    public class NavigationView : Selector
    {
        /// <summary>
        /// Name of the back button template part.
        /// </summary>
        public const string PartBackButton = "PART_BackButton";

        /// <summary>
        /// Name of the main content presenter template part.
        /// </summary>
        public const string PartContentPresenter = "PART_ContentPresenter";

        /// <summary>
        /// Name of the scroll viewer that hosts pane items.
        /// </summary>
        public const string PartPaneItemsScrollViewer = "PART_PaneItemsScrollViewer";

        /// <summary>
        /// Name of the pane collapse/expand toggle button.
        /// </summary>
        public const string PartPaneToggleButton = "PART_PaneToggleButton";

        /// <summary>
        /// Name of the shared selection indicator element.
        /// </summary>
        public const string PartSelectionIndicator = "PART_SelectionIndicator";

        /// <summary>
        /// Name of the items host that renders <see cref="FooterMenuItems"/>.
        /// </summary>
        public const string PartFooterItemsHost = "PART_FooterItemsHost";

        /// <summary>
        /// Name of the selection indicator element for the footer items region.
        /// </summary>
        public const string PartFooterSelectionIndicator = "PART_FooterSelectionIndicator";

        /// <summary>
        /// Name of the top pane items host template part.
        /// </summary>
        public const string PartTopItemsHost = "PART_TopItemsHost";

        /// <summary>
        /// Name of the top pane overflow button template part.
        /// </summary>
        public const string PartTopOverflowButton = "PART_TopOverflowButton";

        private const string PartPaneColumn = "PaneColumn";
        private const double PaneClosedWidth = 48.0;
        private const double PaneClosedWithBackWidth = 96.0;
        private const double PaneOpenWidth = 320.0;
        private const double PaneAnimationMilliseconds = 167.0;

        private static readonly DependencyProperty IsTopOverflowCollapsedProperty =
            DependencyProperty.RegisterAttached(
                "IsTopOverflowCollapsed",
                typeof(bool),
                typeof(NavigationView),
                new PropertyMetadata(defaultValue: false));

        /// <summary>
        /// Internal inheritable attached flag marking the footer items region. The Top pane template
        /// sets it on <c>PART_FooterItemsHost</c>, so it inherits onto the footer
        /// <see cref="NavigationViewItem"/>s; the item template reads it to render those items
        /// icon-only in Top mode. The Left/LeftCompact templates do not set it, scoping the gear-only
        /// rule to Top mode. Inheritance (rather than a code marker) keeps the rule confined to Top
        /// without a per-item pane-mode binding. Not public API.
        /// </summary>
        internal static readonly DependencyProperty IsFooterItemProperty =
            DependencyProperty.RegisterAttached(
                "IsFooterItem",
                typeof(bool),
                typeof(NavigationView),
                new FrameworkPropertyMetadata(defaultValue: false, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Sets the <see cref="IsFooterItemProperty"/> flag on <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element on which to set the flag.</param>
        /// <param name="value">The value to set.</param>
        internal static void SetIsFooterItem(DependencyObject element, bool value)
        {
            element?.SetValue(IsFooterItemProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="IsFooterItemProperty"/> flag from <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element from which to get the flag.</param>
        /// <returns>The value of the flag.</returns>
        internal static bool GetIsFooterItem(DependencyObject element)
        {
            return element is not null && (bool)element.GetValue(IsFooterItemProperty);
        }

        // Margins and offsets used in indicator and top overflow positioning calculations.
        // The indicator sits just inside the selected item's rounded OuterBorder (Margin 4 + 2px
        // stroke), flush against the inner edge with no padding gap, rather than floating in the
        // pane to the left of the item.
        private const double NavigationItemOuterHorizontalMargin = 9.0;
        private const double NavigationItemChildIndicatorOffset = 44.0;
        private const double TopOverflowReservedEndPadding = 12.0;

        /// <summary>
        /// Identifies the <see cref="PaneDisplayMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneDisplayModeProperty = DependencyProperty.Register(
            "PaneDisplayMode",
            typeof(NavigationViewPaneDisplayMode),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
                NavigationViewPaneDisplayMode.Left,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnPaneDisplayModeChanged));

        /// <summary>
        /// Identifies the <see cref="SelectionFollowsFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionFollowsFocusProperty = DependencyProperty.Register(
            "SelectionFollowsFocus",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the <see cref="IsBackButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackButtonVisibleProperty = DependencyProperty.Register(
            "IsBackButtonVisible",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(defaultValue: false, OnBackButtonStateChanged));

        /// <summary>
        /// Identifies the <see cref="IsBackEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackEnabledProperty = DependencyProperty.Register(
            "IsBackEnabled",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(defaultValue: true, OnBackButtonStateChanged));

        /// <summary>
        /// Identifies the <see cref="IsPaneToggleButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty = DependencyProperty.Register(
            "IsPaneToggleButtonVisible",
            typeof(bool),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
defaultValue: true,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
propertyChangedCallback: null,
                CoerceIsPaneToggleButtonVisible));

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="HeaderTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            "HeaderTemplate",
            typeof(DataTemplate),
            typeof(NavigationView),
            new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="PaneHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneHeaderProperty = DependencyProperty.Register(
            "PaneHeader",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="PaneFooter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneFooterProperty = DependencyProperty.Register(
            "PaneFooter",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="ContentBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentBackgroundProperty = DependencyProperty.Register(
            "ContentBackground",
            typeof(Brush),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
defaultValue: null,
                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="IsPaneOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPaneOpenProperty = DependencyProperty.Register(
            "IsPaneOpen",
            typeof(bool),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(defaultValue: true, OnIsPaneOpenChanged, CoerceIsPaneOpen));

        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(propertyChangedCallback: null));

        private static readonly DependencyPropertyKey FooterMenuItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            "FooterMenuItems",
            typeof(ObservableCollection<object>),
            typeof(NavigationView),
            new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="FooterMenuItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterMenuItemsProperty = FooterMenuItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Initializes static members of the NavigationView class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the NavigationView control uses its own default
        /// style by associating it with the appropriate style key. This is necessary for custom controls to apply their
        /// styles correctly in XAML-based applications.</remarks>
        static NavigationView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NavigationView),
                new FrameworkPropertyMetadata(typeof(NavigationView)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationView"/> class.
        /// </summary>
        public NavigationView()
        {
            SetValue(FooterMenuItemsPropertyKey, new ObservableCollection<object>());
            FooterMenuItems.CollectionChanged += OnFooterMenuItemsChanged;
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Occurs when a navigation item is invoked before selection changes.
        /// </summary>
        public event EventHandler<NavigationViewItemInvokedEventArgs>? ItemInvoked;

        /// <summary>
        /// Occurs when the back button is invoked.
        /// </summary>
        public event EventHandler<NavigationViewBackRequestedEventArgs>? BackRequested;

        /// <summary>
        /// Occurs when the pane is opening (expanded in left mode).
        /// </summary>
        public event EventHandler? PaneOpening;

        /// <summary>
        /// Occurs when the pane has closed (collapsed in left mode).
        /// </summary>
        public event EventHandler? PaneClosed;

        /// <summary>
        /// Gets or sets whether the pane is shown on the left or across the top.
        /// </summary>
        public NavigationViewPaneDisplayMode PaneDisplayMode
        {
            get => (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty);
            set => SetValue(PaneDisplayModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether keyboard focus on an item selects it immediately.
        /// </summary>
        public bool SelectionFollowsFocus
        {
            get => (bool)GetValue(SelectionFollowsFocusProperty);
            set => SetValue(SelectionFollowsFocusProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the back button is shown.
        /// </summary>
        public bool IsBackButtonVisible
        {
            get => (bool)GetValue(IsBackButtonVisibleProperty);
            set => SetValue(IsBackButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the back button can be invoked.
        /// </summary>
        public bool IsBackEnabled
        {
            get => (bool)GetValue(IsBackEnabledProperty);
            set => SetValue(IsBackEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the pane collapse/expand toggle button is shown in left pane modes.
        /// </summary>
        public bool IsPaneToggleButtonVisible
        {
            get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
            set => SetValue(IsPaneToggleButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets header content displayed beside the navigation chrome.
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the DataTemplate used to display the <see cref="Header"/>.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets content at the start of the pane chrome (title area).
        /// </summary>
        public object PaneHeader
        {
            get => GetValue(PaneHeaderProperty);
            set => SetValue(PaneHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets content at the end of the pane (footer).
        /// </summary>
        public object PaneFooter
        {
            get => GetValue(PaneFooterProperty);
            set => SetValue(PaneFooterProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush for the content area.
        /// </summary>
        public Brush ContentBackground
        {
            get => (Brush)GetValue(ContentBackgroundProperty);
            set => SetValue(ContentBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the left pane is expanded.
        /// </summary>
        public bool IsPaneOpen
        {
            get => (bool)GetValue(IsPaneOpenProperty);
            set => SetValue(IsPaneOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the content hosted in the main area.
        /// </summary>
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets the collection of pinned footer entries, rendered below the main menu items.
        /// Footer entries are <see cref="NavigationViewItem"/> instances that participate in the
        /// same single-selection model and selection indicator as the main menu, mirroring the
        /// WinUI <c>NavigationView.FooterMenuItems</c> region.
        /// </summary>
        public ObservableCollection<object> FooterMenuItems => (ObservableCollection<object>)GetValue(FooterMenuItemsProperty);

        /// <summary>
        /// Gets the currently selected footer item, or <see langword="null"/> when the active
        /// selection is in the main menu region (or nothing is selected).
        /// </summary>
        internal NavigationViewItem? SelectedFooterItem { get; private set; }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _backButton?.Click -= OnBackButtonClick;
            _paneToggleButton?.Click -= OnPaneToggleButtonClick;
            _topOverflowButton?.Click -= OnTopOverflowButtonClick;
            StopPaneColumnAnimation();
            base.OnApplyTemplate();
            _backButton = GetTemplateChild(PartBackButton) as System.Windows.Controls.Button;
            _backButton?.Click += OnBackButtonClick;
            _paneToggleButton = GetTemplateChild(PartPaneToggleButton) as System.Windows.Controls.Button;
            _paneToggleButton?.Click += OnPaneToggleButtonClick;
            _topItemsHost = GetTemplateChild(PartTopItemsHost) as FrameworkElement;
            if (GetTemplateChild(PartTopOverflowButton) is System.Windows.Controls.Button topOverflowButton)
            {
                _topOverflowButton = topOverflowButton;
                _topOverflowButton.Click += OnTopOverflowButtonClick;
            }
            else
            {
                _topOverflowButton = null;
            }

            _paneColumn = GetTemplateChild(PartPaneColumn) as ColumnDefinition;
            _selectionIndicator = GetTemplateChild(PartSelectionIndicator) as FrameworkElement;
            _indicatorHost = _selectionIndicator is not null ? VisualTreeHelper.GetParent(_selectionIndicator) as FrameworkElement : null;
            _footerSelectionIndicator = GetTemplateChild(PartFooterSelectionIndicator) as FrameworkElement;

            // The footer indicator host must be an ancestor of the footer items so that
            // CalculateIndicatorPosition's TransformToAncestor succeeds. In Left/LeftCompact the
            // indicator is a direct child of the Grid that also hosts PART_FooterItemsHost; in Top it
            // sits in a zero-size Canvas inside that same Grid (the Canvas fills the cell at its
            // origin, so its coordinate space matches the Grid's). Resolving the host from the items
            // host's parent therefore works for every pane mode, where using the indicator's immediate
            // parent (the Canvas in Top mode) is not an ancestor of the items and the transform fails.
            FrameworkElement? footerItemsHost = GetTemplateChild(PartFooterItemsHost) as FrameworkElement;
            _footerIndicatorHost = (footerItemsHost is not null ? VisualTreeHelper.GetParent(footerItemsHost) as FrameworkElement : null)
                ?? (_footerSelectionIndicator is not null ? VisualTreeHelper.GetParent(_footerSelectionIndicator) as FrameworkElement : null);
            foreach (object entry in FooterMenuItems)
            {
                if (entry is NavigationViewItem footerItem)
                {
                    HookFooterItem(footerItem);
                }
            }
            _indicatorPositioned = false;
            StopAnimation();
            CoerceTopPaneProperties();
            UpdateTitleBarExtensionForPaneMode();
            UpdateBackButtonState(useTransitions: false);
            ApplyPaneColumnWidthOnTemplateApplied();
            ScheduleTopOverflowUpdate();
            ScheduleIndicatorPosition(animate: false);
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            NavigationViewItem? previousItem = e.RemovedItems.Count > 0
                ? ResolveNavigationViewItem(e.RemovedItems[0])
                : null;
            base.OnSelectionChanged(e);
            if (SelectedItem is not null && SelectedFooterItem is not null)
            {
                SelectedFooterItem.IsSelected = false;
                SelectedFooterItem = null;
            }
            _ = Dispatcher.BeginInvoke(new Action(() => RefreshIndicators(animate: true, previousItem)), DispatcherPriority.Loaded);
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            ScheduleTopOverflowUpdate();
        }

        /// <inheritdoc />
        protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewGotKeyboardFocus(e);
            if (!SelectionFollowsFocus)
            {
                return;
            }
            if (FindNavigationViewItem(e.NewFocus as DependencyObject) is not NavigationViewItem navItem)
            {
                return;
            }

            object fromContainer = ItemContainerGenerator.ItemFromContainer(navItem);
            if (fromContainer != DependencyProperty.UnsetValue && fromContainer is not null)
            {
                if (!ReferenceEquals(SelectedItem, fromContainer))
                {
                    SelectedItem = fromContainer;
                }
            }
            else if (!ReferenceEquals(SelectedItem, navItem))
            {
                SelectedItem = navItem;
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NavigationViewAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is NavigationViewItem or NavigationViewItemHeader or NavigationViewItemSeparator;
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new NavigationViewItem();
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is NavigationViewItem navItem)
            {
                navItem.Selected -= OnNavigationViewItemSelected;
                navItem.Selected += OnNavigationViewItemSelected;
                navItem.Loaded -= OnNavigationViewItemLoaded;
                navItem.Loaded += OnNavigationViewItemLoaded;
                navItem.SizeChanged -= OnNavigationViewItemSizeChanged;
                navItem.SizeChanged += OnNavigationViewItemSizeChanged;
                navItem.IsVisibleChanged -= OnNavigationViewItemIsVisibleChanged;
                navItem.IsVisibleChanged += OnNavigationViewItemIsVisibleChanged;
            }
        }

        /// <inheritdoc />
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is NavigationViewItem navItem)
            {
                navItem.Selected -= OnNavigationViewItemSelected;
                navItem.Loaded -= OnNavigationViewItemLoaded;
                navItem.SizeChanged -= OnNavigationViewItemSizeChanged;
                navItem.IsVisibleChanged -= OnNavigationViewItemIsVisibleChanged;
            }
            base.ClearContainerForItemOverride(element, item);
        }

        /// <summary>
        /// Raises <see cref="BackRequested"/> as the back button would. Used by unit tests.
        /// </summary>
        internal void RaiseBackRequestedForTesting()
        {
            OnBackButtonClick(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Returns the shared selection indicator element from the current template, if resolved.
        /// Used by unit tests.
        /// </summary>
        internal FrameworkElement? GetSelectionIndicatorForTesting()
        {
            return _selectionIndicator;
        }

        /// <summary>
        /// Returns the footer selection indicator element from the current template, if resolved.
        /// Used by unit tests.
        /// </summary>
        internal FrameworkElement? GetFooterSelectionIndicatorForTesting()
        {
            return _footerSelectionIndicator;
        }

        internal double GetPaneColumnWidthForTesting()
        {
            return _paneColumn?.Width.Value ?? double.NaN;
        }

        internal Point CalculateDepartPositionForTesting(
            Point fromPosition,
            NavigationViewItem? previousItem,
            bool topMode,
            double direction)
        {
            return CalculateDepartPosition(fromPosition, previousItem, topMode, direction);
        }

        /// <summary>
        /// Programmatically selects a <see cref="FooterMenuItems"/> entry as if the user had invoked
        /// it: clears any main-menu selection, marks the footer item selected, moves the footer
        /// selection indicator, and raises <see cref="ItemInvoked"/>. No-op if the item is not a
        /// current footer entry.
        /// </summary>
        /// <param name="item">The footer item to select.</param>
        public void SelectFooterMenuItem(NavigationViewItem item)
        {
            if (item is null || !FooterMenuItems.Contains(item))
            {
                return;
            }
            InvokeItem(item);
        }

        internal void InvokeItem(NavigationViewItem item)
        {
            if (item?.IsEnabled != true)
            {
                return;
            }
            bool isFooter = IsFooterItem(item);
            object invokedItem = isFooter ? item : GetDataFromContainer(item);
            ItemInvoked?.Invoke(this, new NavigationViewItemInvokedEventArgs(invokedItem, item, isSettingsInvoked: false));
            if (isFooter)
            {
                SelectFooterItem(item);
            }
            else
            {
                SelectItemFromContainer(item);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="item"/> belongs to the <see cref="FooterMenuItems"/> region.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns><see langword="true"/> if the item belongs to the footer region; otherwise, <see langword="false"/>.</returns>
        private bool IsFooterItem(NavigationViewItem item)
        {
            return FooterMenuItems.Contains(item);
        }

        /// <summary>
        /// Selects a footer item, clearing any main-menu selection so that exactly one region owns
        /// the selection at a time, then schedules the footer selection indicator to reposition.
        /// </summary>
        /// <param name="item">The footer item to select.</param>
        private void SelectFooterItem(NavigationViewItem item)
        {
            if (item is null)
            {
                return;
            }

            foreach (object entry in FooterMenuItems)
            {
                if (entry is NavigationViewItem footerItem && !ReferenceEquals(footerItem, item))
                {
                    footerItem.IsSelected = false;
                }
            }

            SelectedFooterItem = item;
            item.IsSelected = true;

            // Clearing the main selection raises OnSelectionChanged (which refreshes the indicators).
            // When the main selection was already empty no event fires, so refresh explicitly too.
            if (SelectedItem is not null)
            {
                SelectedItem = null;
            }
            else
            {
                ScheduleIndicatorPosition(animate: true);
            }
        }

        private void OnFooterMenuItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (object entry in e.OldItems)
                {
                    if (entry is NavigationViewItem footerItem)
                    {
                        UnhookFooterItem(footerItem);
                        if (ReferenceEquals(footerItem, SelectedFooterItem))
                        {
                            SelectedFooterItem = null;
                        }
                    }
                }
            }

            if (e.NewItems is not null)
            {
                foreach (object entry in e.NewItems)
                {
                    if (entry is NavigationViewItem footerItem)
                    {
                        HookFooterItem(footerItem);
                    }
                }
            }

            ScheduleIndicatorPosition(animate: false);
        }

        private void HookFooterItem(NavigationViewItem footerItem)
        {
            footerItem.Loaded -= OnNavigationViewItemLoaded;
            footerItem.Loaded += OnNavigationViewItemLoaded;
            footerItem.SizeChanged -= OnNavigationViewItemSizeChanged;
            footerItem.SizeChanged += OnNavigationViewItemSizeChanged;
            footerItem.IsVisibleChanged -= OnFooterItemIsVisibleChanged;
            footerItem.IsVisibleChanged += OnFooterItemIsVisibleChanged;
        }

        private void UnhookFooterItem(NavigationViewItem footerItem)
        {
            footerItem.Loaded -= OnNavigationViewItemLoaded;
            footerItem.SizeChanged -= OnNavigationViewItemSizeChanged;
            footerItem.IsVisibleChanged -= OnFooterItemIsVisibleChanged;
        }

        private void OnFooterItemIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ScheduleIndicatorPosition(animate: false);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Do not null the resolved template parts here. WPF preserves a control's template tree
            // across an unload/reload (e.g. a cached page being revisited) and does NOT re-run
            // OnApplyTemplate, so nulling the parts would strand the selection indicator and chrome
            // handlers after the control reloads. OnApplyTemplate re-resolves the parts (with a
            // detach/reattach) if the template is genuinely re-applied. Here we only release the
            // external window watcher and stop in-flight animations, and reset the positioned flag
            // so the indicator re-snaps to the current selection on reload.
            DetachTitleBarWindowWatcher();
            StopAnimation();
            StopPaneColumnAnimation();
            _indicatorPositioned = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachTitleBarWindowWatcher();
            CoerceTopPaneProperties();
            UpdateTitleBarExtensionForPaneMode();
            ScheduleTopOverflowUpdate();
            // Reposition the selection indicator on (re)load so it tracks the current selection even
            // when the control was reloaded without OnApplyTemplate running again.
            ScheduleIndicatorPosition(animate: false);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleTopOverflowUpdate();
        }

        private static void OnBackButtonStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationView nav = (NavigationView)d;
            nav.UpdateBackButtonState(useTransitions: true);
            nav.UpdatePaneColumnWidth(useAnimation: false);
        }

        /// <summary>
        /// Transitions the back button to the correct <c>BackButtonStates</c> VSM state
        /// based on <see cref="IsBackButtonVisible"/>. Called without transitions on
        /// initial template application; with transitions on runtime changes.
        /// </summary>
        /// <param name="useTransitions">Indicates whether to use visual transitions.</param>
        private void UpdateBackButtonState(bool useTransitions)
        {
            bool isVisible = IsBackButtonVisible && IsBackEnabled;
            string stateName = isVisible ? "BackButtonVisible" : "BackButtonCollapsed";
            _ = VisualStateManager.GoToState(this, stateName, useTransitions);
            if (_backButton is not null)
            {
                _backButton.BeginAnimation(VisibilityProperty, animation: null);
                _backButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, new NavigationViewBackRequestedEventArgs());
        }

        private void OnPaneToggleButtonClick(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(IsPaneOpenProperty, !IsPaneOpen);
        }

        private void OnNavigationViewItemSelected(object sender, RoutedEventArgs e)
        {
            if (sender is not NavigationViewItem navItem)
            {
                return;
            }
            SelectItemFromContainer(navItem);
        }

        private void OnNavigationViewItemLoaded(object sender, RoutedEventArgs e)
        {
            if (SelectedItem is not null || SelectedFooterItem is not null)
            {
                ScheduleIndicatorPosition(animate: false);
            }

            ScheduleTopOverflowUpdate();
        }

        private void OnNavigationViewItemSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleTopOverflowUpdate();
        }

        private void OnNavigationViewItemIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_updatingTopOverflow && sender is NavigationViewItem navItem)
            {
                navItem.ClearValue(IsTopOverflowCollapsedProperty);
                ScheduleTopOverflowUpdate();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0091:Sender should be 'this' for instance events", Justification = "The method is static.")]
        private static void OnIsPaneOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationView nav = (NavigationView)d;
            bool nowOpen = (bool)e.NewValue;
            if (nowOpen)
            {
                nav.PaneOpening?.Invoke(nav, EventArgs.Empty);
            }
            else
            {
                nav.PaneClosed?.Invoke(nav, EventArgs.Empty);
            }
            nav._indicatorPositioned = false;
            nav.UpdatePaneColumnWidth(useAnimation: true);
            nav.ScheduleTopOverflowUpdate();
            nav.ScheduleIndicatorPosition(animate: false);
        }

        private static void OnPaneDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationView nav = (NavigationView)d;
            NavigationViewPaneDisplayMode oldMode = (NavigationViewPaneDisplayMode)e.OldValue;
            NavigationViewPaneDisplayMode newMode = (NavigationViewPaneDisplayMode)e.NewValue;

            // Left and LeftCompact use different pane templates, so the switch swaps the template and
            // its PaneColumn; the width cannot animate on the old column (it is about to be discarded).
            // Capture the current width now and hand it to the new template's OnApplyTemplate, which
            // animates its fresh column from it - the same GridLength flight as the collapse/expand
            // toggle. Transitions to/from Top have no pane-column animation, so they snap.
            bool animatePaneWidth = IsLeftFamilyMode(oldMode) && IsLeftFamilyMode(newMode);
            double fromWidth = nav.GetCurrentPaneColumnWidth();

            if (newMode == NavigationViewPaneDisplayMode.LeftCompact)
            {
                nav.SetCurrentValue(IsPaneOpenProperty, value: false);
            }
            nav.CoerceTopPaneProperties();
            nav.UpdateTitleBarExtensionForPaneMode();
            nav._indicatorPositioned = false;

            if (animatePaneWidth)
            {
                nav._pendingPaneWidthAnimationFrom = fromWidth;
            }
            else
            {
                nav._pendingPaneWidthAnimationFrom = null;
                nav.UpdatePaneColumnWidth(useAnimation: false);
            }

            nav.ScheduleTopOverflowUpdate();
            nav.ScheduleIndicatorPosition(animate: false);
        }

        private static bool IsLeftFamilyMode(NavigationViewPaneDisplayMode mode)
        {
            return mode is NavigationViewPaneDisplayMode.Left or NavigationViewPaneDisplayMode.LeftCompact;
        }

        private static object CoerceIsPaneOpen(DependencyObject d, object baseValue)
        {
            NavigationView nav = (NavigationView)d;
            return nav.PaneDisplayMode == NavigationViewPaneDisplayMode.Top || (bool)baseValue;
        }

        private static object CoerceIsPaneToggleButtonVisible(DependencyObject d, object baseValue)
        {
            _ = baseValue;
            NavigationView nav = (NavigationView)d;
            return nav.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
        }

        private void CoerceTopPaneProperties()
        {
            CoerceValue(IsPaneOpenProperty);
            CoerceValue(IsPaneToggleButtonVisibleProperty);
        }

        private void UpdateTitleBarExtensionForPaneMode()
        {
            if (Window.GetWindow(this) is not FluenceWindow window || _updatingTitleBarExtension)
            {
                return;
            }

            bool? desiredValue = null;
            if (PaneDisplayMode == NavigationViewPaneDisplayMode.Left)
            {
                desiredValue = true;
            }
            else if (PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                desiredValue = false;
            }

            if (desiredValue is null || window.ExtendsContentIntoTitleBar == desiredValue.Value)
            {
                return;
            }

            _updatingTitleBarExtension = true;
            try
            {
                window.SetCurrentValue(FluenceWindow.ExtendsContentIntoTitleBarProperty, desiredValue.Value);
            }
            finally
            {
                _updatingTitleBarExtension = false;
            }
        }

        private void AttachTitleBarWindowWatcher()
        {
            FluenceWindow? window = Window.GetWindow(this) as FluenceWindow;
            if (ReferenceEquals(window, _titleBarExtensionWindow))
            {
                return;
            }

            DetachTitleBarWindowWatcher();
            _titleBarExtensionWindow = window;

            if (_titleBarExtensionWindow is not null)
            {
                _titleBarExtensionDescriptor ??= DependencyPropertyDescriptor.FromProperty(
                    FluenceWindow.ExtendsContentIntoTitleBarProperty,
                    typeof(FluenceWindow));
                _titleBarExtensionDescriptor?.AddValueChanged(_titleBarExtensionWindow, OnTitleBarExtensionChanged);
            }
        }

        private void DetachTitleBarWindowWatcher()
        {
            if (_titleBarExtensionWindow is not null)
            {
                _titleBarExtensionDescriptor?.RemoveValueChanged(_titleBarExtensionWindow, OnTitleBarExtensionChanged);
                _titleBarExtensionWindow = null;
            }
        }

        private void OnTitleBarExtensionChanged(object? sender, EventArgs e)
        {
            UpdateTitleBarExtensionForPaneMode();
        }

        /// <summary>
        /// Sets the freshly templated pane column width during <see cref="OnApplyTemplate"/>. When a
        /// Left &lt;-&gt; LeftCompact switch is in flight (<see cref="_pendingPaneWidthAnimationFrom"/> is
        /// set), the new column animates from the captured pre-swap width to its target, continuing
        /// the collapse/expand-style flight across the template swap; otherwise the width snaps.
        /// </summary>
        private void ApplyPaneColumnWidthOnTemplateApplied()
        {
            if (_pendingPaneWidthAnimationFrom is double fromWidth
                && _paneColumn is not null
                && PaneDisplayMode is NavigationViewPaneDisplayMode.Left or NavigationViewPaneDisplayMode.LeftCompact)
            {
                _pendingPaneWidthAnimationFrom = null;
                _paneColumn.Width = new GridLength(fromWidth);
                UpdatePaneColumnWidth(useAnimation: true);
                return;
            }

            _pendingPaneWidthAnimationFrom = null;
            UpdatePaneColumnWidth(useAnimation: false);
        }

        private void UpdatePaneColumnWidth(bool useAnimation)
        {
            if (_paneColumn is null)
            {
                return;
            }

            if (PaneDisplayMode is not NavigationViewPaneDisplayMode.Left and not NavigationViewPaneDisplayMode.LeftCompact)
            {
                StopPaneColumnAnimation();
                return;
            }

            double targetWidth = IsPaneOpen ? PaneOpenWidth : GetClosedPaneWidth();
            if (!useAnimation)
            {
                StopPaneColumnAnimation();
                _paneColumn.Width = new GridLength(targetWidth);
                return;
            }

            double currentWidth = GetCurrentPaneColumnWidth();
            if (Math.Abs(currentWidth - targetWidth) <= 0.1)
            {
                StopPaneColumnAnimation();
                _paneColumn.Width = new GridLength(targetWidth);
                return;
            }

            ColumnDefinition paneColumn = _paneColumn;
            int animationGeneration = ++_paneColumnAnimationGeneration;
            GridLengthAnimation animation = new()
            {
                From = new GridLength(currentWidth),
                To = new GridLength(targetWidth),
                Duration = new Duration(TimeSpan.FromMilliseconds(PaneAnimationMilliseconds)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop,
            };

            animation.Completed += delegate
            {
                if (animationGeneration != _paneColumnAnimationGeneration || !ReferenceEquals(paneColumn, _paneColumn))
                {
                    return;
                }

                paneColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation: null);
                paneColumn.Width = new GridLength(targetWidth);
            };

            paneColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private double GetCurrentPaneColumnWidth()
        {
            if (_paneColumn is null)
            {
                return GetClosedPaneWidth();
            }

            GridLength current = _paneColumn.Width;
            return current.GridUnitType == GridUnitType.Pixel
                ? current.Value
                : GetClosedPaneWidth();
        }

        private double GetClosedPaneWidth()
        {
            return IsBackButtonVisible && IsBackEnabled ? PaneClosedWithBackWidth : PaneClosedWidth;
        }

        private void ScheduleIndicatorPosition(bool animate)
        {
            _ = Dispatcher.BeginInvoke(new Action(() => RefreshIndicators(animate, previousItem: null)), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Repositions both the main and footer selection indicators. The selection model guarantees
        /// at most one region (main menu or footer) owns the selection, so at most one indicator shows.
        /// </summary>
        /// <param name="animate">Indicates whether to animate the indicator movement.</param>
        /// <param name="previousItem">The previously selected item, if any.</param>
        private void RefreshIndicators(bool animate, NavigationViewItem? previousItem)
        {
            PositionIndicator(animate, previousItem);
            PositionFooterIndicator(animate);
        }

        /// <summary>
        /// Snaps the footer selection indicator onto the selected footer item, or hides it when no
        /// footer item is selected. The footer region typically holds a single item, so the indicator
        /// does not fly laterally; instead it fades and scales in when a footer item becomes selected
        /// and out when it is deselected, matching the feel of the main region's arrive/depart.
        /// </summary>
        /// <param name="animate">Indicates whether to animate the indicator movement.</param>
        private void PositionFooterIndicator(bool animate)
        {
            if (_footerSelectionIndicator is null || _footerIndicatorHost is null)
            {
                return;
            }

            bool topMode = PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
            bool shouldShow = IsLoaded
                && SelectedFooterItem?.IsVisible == true
                && SelectedFooterItem.ActualHeight > 0;

            if (!shouldShow)
            {
                if (topMode)
                {
                    // Animate the indicator out when leaving a selected footer item (e.g. navigating
                    // away from Settings); snap to hidden when nothing was showing or animation is off.
                    bool wasVisible = _footerSelectionIndicator.Opacity > 0.01;
                    AnimateFooterIndicatorVisibility(appearing: false, topMode: true, animate && wasVisible);
                }
                else
                {
                    // Left / LeftCompact keep the historical instant hide.
                    StopFooterAnimation();
                    _footerSelectionIndicator.Opacity = 0.0;
                }
                return;
            }

            Point targetPosition = CalculateIndicatorPosition(SelectedFooterItem!, _footerSelectionIndicator, _footerIndicatorHost, topMode);

            if (!topMode)
            {
                // Left / LeftCompact keep the historical snap (no fade/scale flight).
                StopFooterAnimation();
                SnapIndicatorCore(_footerSelectionIndicator, targetPosition);
                return;
            }

            bool wasHidden = _footerSelectionIndicator.Opacity < 0.01;
            StopFooterAnimation();
            EnsureMutableTransform(_footerSelectionIndicator);
            TransformGroup group = (TransformGroup)_footerSelectionIndicator.RenderTransform;
            TranslateTransform translate = (TranslateTransform)group.Children[1];
            translate.X = targetPosition.X;
            translate.Y = targetPosition.Y;

            // Fade + scale the indicator in when it first appears on a footer item; a reflow while it
            // is already shown just repositions it at full opacity.
            AnimateFooterIndicatorVisibility(appearing: true, topMode: true, animate && wasHidden);
        }

        /// <summary>
        /// Fades and scales the footer selection indicator in (<paramref name="appearing"/> is
        /// <see langword="true"/>) or out, mirroring the main indicator's arrive/depart easing. When
        /// <paramref name="animate"/> is <see langword="false"/> the indicator snaps directly to the
        /// target opacity and scale. The scaled axis follows the indicator orientation: horizontal in
        /// Top mode, vertical otherwise.
        /// </summary>
        /// <param name="appearing">Indicates whether the indicator is appearing or disappearing.</param>
        /// <param name="topMode">Indicates whether the navigation view is in top mode.</param>
        /// <param name="animate">Indicates whether to animate the indicator visibility change.</param>
        private void AnimateFooterIndicatorVisibility(bool appearing, bool topMode, bool animate)
        {
            if (_footerSelectionIndicator is null)
            {
                return;
            }

            StopFooterAnimation();
            EnsureMutableTransform(_footerSelectionIndicator);
            TransformGroup group = (TransformGroup)_footerSelectionIndicator.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            DependencyProperty scaleProperty = topMode ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty;

            double toOpacity = appearing ? 1.0 : 0.0;
            if (!animate)
            {
                scale.ScaleX = 1.0;
                scale.ScaleY = 1.0;
                _footerSelectionIndicator.Opacity = toOpacity;
                return;
            }

            int animationId = _footerAnimationGeneration;
            double fromScale = appearing ? 0.72 : 1.0;
            double toScale = appearing ? 1.0 : 0.72;
            double fromOpacity = appearing ? 0.0 : 1.0;
            Duration duration = new(TimeSpan.FromMilliseconds(appearing ? 140.0 : 90.0));
            CubicEase ease = new() { EasingMode = appearing ? EasingMode.EaseOut : EasingMode.EaseIn };

            // Seed the start state; the cross axis stays at 1.0 so only the indicator's length scales.
            scale.ScaleX = topMode ? fromScale : 1.0;
            scale.ScaleY = topMode ? 1.0 : fromScale;
            _footerSelectionIndicator.Opacity = fromOpacity;

            DoubleAnimation scaleAnimation = new(fromScale, toScale, duration)
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop,
            };
            DoubleAnimation opacityAnimation = new(fromOpacity, toOpacity, duration)
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop,
            };

            opacityAnimation.Completed += delegate
            {
                if (animationId != _footerAnimationGeneration)
                {
                    return;
                }
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation: null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation: null);
                _footerSelectionIndicator.BeginAnimation(OpacityProperty, animation: null);
                scale.ScaleX = 1.0;
                scale.ScaleY = 1.0;
                _footerSelectionIndicator.Opacity = toOpacity;
            };

            scale.BeginAnimation(scaleProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
            _footerSelectionIndicator.BeginAnimation(OpacityProperty, opacityAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Cancels any in-flight footer-indicator animations and bumps the generation guard so their
        /// completion callbacks no-op.
        /// </summary>
        private void StopFooterAnimation()
        {
            _footerAnimationGeneration++;
            if (_footerSelectionIndicator is null)
            {
                return;
            }
            _footerSelectionIndicator.BeginAnimation(OpacityProperty, animation: null);
            if (_footerSelectionIndicator.RenderTransform is TransformGroup group && group.Children.Count >= 2)
            {
                if (group.Children[0] is ScaleTransform scale && !scale.IsFrozen)
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation: null);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation: null);
                }
                if (group.Children[1] is TranslateTransform translate && !translate.IsFrozen)
                {
                    translate.BeginAnimation(TranslateTransform.XProperty, animation: null);
                    translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                }
            }
        }

        private void PositionIndicator(bool animate, NavigationViewItem? previousItem)
        {
            if (_selectionIndicator is null || _indicatorHost is null)
            {
                return;
            }
            if (!IsLoaded)
            {
                return;
            }
            if (SelectedItem is null)
            {
                HideIndicator();
                return;
            }
            if (ResolveNavigationViewItem(SelectedItem) is not NavigationViewItem nvi || !nvi.IsVisible || nvi.ActualHeight is 0)
            {
                HideIndicator();
                return;
            }

            bool topMode = PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
            Point targetPosition = CalculateIndicatorPosition(nvi, _selectionIndicator, _indicatorHost, topMode);
            if (!animate || !_indicatorPositioned)
            {
                SnapIndicator(targetPosition);
                return;
            }

            Point currentPosition = GetCurrentIndicatorPosition();
            AnimateIndicator(currentPosition, targetPosition, topMode, previousItem, nvi);
        }

        /// <summary>
        /// Calculates the translate position for the supplied indicator relative to its host Grid.
        /// </summary>
        /// <param name="item">The navigation view item for which to calculate the indicator position.</param>
        /// <param name="indicator">The indicator element.</param>
        /// <param name="host">The host element containing the indicator.</param>
        /// <param name="topMode">Indicates whether the navigation view is in top mode.</param>
        /// <returns>The calculated position for the indicator.</returns>
        private Point CalculateIndicatorPosition(NavigationViewItem item, FrameworkElement indicator, FrameworkElement host, bool topMode)
        {
            try
            {
                GeneralTransform transform = item.TransformToAncestor(host);
                Point itemPos = transform.Transform(new Point(0, 0));
                if (topMode)
                {
                    return new Point(itemPos.X + ((item.ActualWidth - indicator.Width) / 2.0), 0.0);
                }

                double x = itemPos.X + NavigationItemOuterHorizontalMargin;
                if (ShouldIndentSelectionIndicator(item, topMode))
                {
                    x += NavigationItemChildIndicatorOffset;
                }
                return new Point(x, itemPos.Y + ((item.ActualHeight - indicator.Height) / 2.0));
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                return new Point(0, 0);
            }
        }

        private bool ShouldIndentSelectionIndicator(NavigationViewItem item, bool topMode)
        {
            return !topMode && item?.IsChildItem == true && (IsPaneOpen || (PaneDisplayMode != NavigationViewPaneDisplayMode.Left && PaneDisplayMode != NavigationViewPaneDisplayMode.LeftCompact));
        }

        private Point GetCurrentIndicatorPosition()
        {
            return _selectionIndicator?.RenderTransform is TransformGroup group && group.Children.Count >= 2 && group.Children[1] is TranslateTransform translate
                ? new Point(translate.X, translate.Y)
                : new Point(0, 0);
        }

        /// <summary>
        /// Immediately places the main indicator at the target offset with no animation.
        /// </summary>
        /// <param name="targetPosition">The target position for the indicator.</param>
        /// <exception cref="InvalidOperationException">Thrown if the selection indicator template part is missing.</exception>
        private void SnapIndicator(Point targetPosition)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            StopAnimation();
            SnapIndicatorCore(_selectionIndicator, targetPosition);
            _indicatorPositioned = true;
        }

        /// <summary>
        /// Snaps an arbitrary indicator element to the supplied offset with scale reset and full opacity.
        /// </summary>
        /// <param name="indicator">The indicator element to snap.</param>
        /// <param name="targetPosition">The target position for the indicator.</param>
        private static void SnapIndicatorCore(FrameworkElement indicator, Point targetPosition)
        {
            EnsureMutableTransform(indicator);
            TransformGroup group = (TransformGroup)indicator.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform translate = (TranslateTransform)group.Children[1];

            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;
            translate.X = targetPosition.X;
            translate.Y = targetPosition.Y;

            indicator.Opacity = 1.0;
        }

        private void AnimateIndicator(
            Point fromPosition,
            Point toPosition,
            bool topMode,
            NavigationViewItem? previousItem,
            NavigationViewItem targetItem)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            StopAnimation(); EnsureMutableTransform(_selectionIndicator);
            TransformGroup group = (TransformGroup)_selectionIndicator.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform translate = (TranslateTransform)group.Children[1];
            int animationId = _indicatorAnimationGeneration;
            DependencyProperty axisProperty = topMode ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            DependencyProperty scaleProperty = topMode ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty;
            double fromAxis = topMode ? fromPosition.X : fromPosition.Y;
            double toAxis = topMode ? toPosition.X : toPosition.Y;
            double direction = toAxis < fromAxis ? -1.0 : 1.0;

            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;
            translate.X = fromPosition.X;
            translate.Y = fromPosition.Y;
            _selectionIndicator.Opacity = 1.0;

            Point departPosition = CalculateDepartPosition(fromPosition, previousItem, topMode, direction);
            Point arriveStartPosition = CalculateArriveStartPosition(toPosition, targetItem, topMode, direction);
            double departAxis = topMode ? departPosition.X : departPosition.Y;
            double arriveStartAxis = topMode ? arriveStartPosition.X : arriveStartPosition.Y;
            Duration departDuration = new(TimeSpan.FromMilliseconds(90));
            Duration arriveDuration = new(TimeSpan.FromMilliseconds(140));
            CubicEase departEase = new() { EasingMode = EasingMode.EaseIn };
            CubicEase arriveEase = new() { EasingMode = EasingMode.EaseOut };

            DoubleAnimation departAxisAnimation = new(fromAxis, departAxis, departDuration)
            {
                EasingFunction = departEase,
                FillBehavior = FillBehavior.Stop,
            };
            DoubleAnimation departOpacityAnimation = new(1.0, 0.0, departDuration)
            {
                EasingFunction = departEase,
                FillBehavior = FillBehavior.Stop,
            };
            DoubleAnimation departScaleAnimation = new(1.0, 0.72, departDuration)
            {
                EasingFunction = departEase,
                FillBehavior = FillBehavior.Stop,
            };

            departAxisAnimation.Completed += delegate
            {
                if (animationId != _indicatorAnimationGeneration)
                {
                    return;
                }
                translate.BeginAnimation(axisProperty, animation: null);
                scale.BeginAnimation(scaleProperty, animation: null);
                _selectionIndicator.BeginAnimation(OpacityProperty, animation: null);
                if (topMode)
                {
                    translate.X = arriveStartPosition.X;
                    translate.Y = toPosition.Y;
                    scale.ScaleX = 0.72;
                    scale.ScaleY = 1.0;
                }
                else
                {
                    translate.X = toPosition.X;
                    translate.Y = arriveStartPosition.Y;
                    scale.ScaleX = 1.0;
                    scale.ScaleY = 0.72;
                }
                _selectionIndicator.Opacity = 0.0;

                DoubleAnimation arriveAxisAnimation = new(arriveStartAxis, toAxis, arriveDuration)
                {
                    EasingFunction = arriveEase,
                    FillBehavior = FillBehavior.Stop,
                };
                DoubleAnimation arriveOpacityAnimation = new(0.0, 1.0, arriveDuration)
                {
                    EasingFunction = arriveEase,
                    FillBehavior = FillBehavior.Stop,
                };
                DoubleAnimation arriveScaleAnimation = new(0.72, 1.0, arriveDuration)
                {
                    EasingFunction = arriveEase,
                    FillBehavior = FillBehavior.Stop,
                };

                arriveAxisAnimation.Completed += delegate
                {
                    if (animationId != _indicatorAnimationGeneration)
                    {
                        return;
                    }

                    translate.BeginAnimation(axisProperty, animation: null);
                    scale.BeginAnimation(scaleProperty, animation: null);
                    _selectionIndicator.BeginAnimation(OpacityProperty, animation: null);

                    translate.X = toPosition.X;
                    translate.Y = toPosition.Y;
                    scale.ScaleX = 1.0;
                    scale.ScaleY = 1.0;
                    _selectionIndicator.Opacity = 1.0;
                    _indicatorPositioned = true;
                };
                translate.BeginAnimation(axisProperty, arriveAxisAnimation, HandoffBehavior.SnapshotAndReplace);
                scale.BeginAnimation(scaleProperty, arriveScaleAnimation, HandoffBehavior.SnapshotAndReplace);
                _selectionIndicator.BeginAnimation(OpacityProperty, arriveOpacityAnimation, HandoffBehavior.SnapshotAndReplace);
            };
            _indicatorPositioned = true;
            translate.BeginAnimation(axisProperty, departAxisAnimation, HandoffBehavior.SnapshotAndReplace);
            scale.BeginAnimation(scaleProperty, departScaleAnimation, HandoffBehavior.SnapshotAndReplace);
            _selectionIndicator.BeginAnimation(OpacityProperty, departOpacityAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        private Point CalculateDepartPosition(
            Point fromPosition,
            NavigationViewItem? previousItem,
            bool topMode,
            double direction)
        {
            double length = GetIndicatorLength(topMode);
            if (topMode)
            {
                double x = fromPosition.X + (direction * length);
                if (previousItem?.IsVisible == true && previousItem.ActualWidth > 0)
                {
                    try
                    {
                        GeneralTransform transform = previousItem.TransformToAncestor(_indicatorHost);
                        Point itemPos = transform.Transform(new Point(0, 0));
                        x = direction > 0 ? itemPos.X + previousItem.ActualWidth : itemPos.X - length;
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        return new Point(x, fromPosition.Y);
                    }
                }
                return new Point(x, fromPosition.Y);
            }

            double y = fromPosition.Y + (direction * length);
            if (previousItem?.IsVisible == true && previousItem.ActualHeight > 0)
            {
                try
                {
                    GeneralTransform transform = previousItem.TransformToAncestor(_indicatorHost);
                    Point itemPos = transform.Transform(new Point(0, 0));
                    y = direction > 0 ? itemPos.Y + previousItem.ActualHeight : itemPos.Y - length;
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    return new Point(fromPosition.X, y);
                }
            }
            return new Point(fromPosition.X, y);
        }

        private Point CalculateArriveStartPosition(
            Point toPosition,
            NavigationViewItem targetItem,
            bool topMode,
            double direction)
        {
            double length = GetIndicatorLength(topMode);
            if (topMode)
            {
                double x = toPosition.X - (direction * length);
                if (targetItem?.IsVisible == true && targetItem.ActualWidth > 0)
                {
                    try
                    {
                        GeneralTransform transform = targetItem.TransformToAncestor(_indicatorHost);
                        Point itemPos = transform.Transform(new Point(0, 0));
                        x = direction > 0 ? itemPos.X - length : itemPos.X + targetItem.ActualWidth;
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        return new Point(x, toPosition.Y);
                    }
                }

                return new Point(x, toPosition.Y);
            }

            double y = toPosition.Y - (direction * length);
            if (targetItem?.IsVisible == true && targetItem.ActualHeight > 0)
            {
                try
                {
                    GeneralTransform transform = targetItem.TransformToAncestor(_indicatorHost);
                    Point itemPos = transform.Transform(new Point(0, 0));
                    y = direction > 0 ? itemPos.Y - length : itemPos.Y + targetItem.ActualHeight;
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    return new Point(toPosition.X, y);
                }
            }
            return new Point(toPosition.X, y);
        }

        private double GetIndicatorLength(bool topMode)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            double actual = topMode ? _selectionIndicator.ActualWidth : _selectionIndicator.ActualHeight;
            if (actual > 0)
            {
                return actual;
            }
            double explicitLength = topMode ? _selectionIndicator.Width : _selectionIndicator.Height;
            return explicitLength > 0 ? explicitLength : 16.0;
        }

        private void HideIndicator()
        {
            StopAnimation();
            _ = _selectionIndicator?.Opacity = 0;
            _indicatorPositioned = false;
        }

        private void StopAnimation()
        {
            _indicatorAnimationGeneration++;
            if (_selectionIndicator is null)
            {
                return;
            }

            _selectionIndicator.BeginAnimation(OpacityProperty, animation: null);
            if (_selectionIndicator.RenderTransform is TransformGroup group && group.Children.Count >= 2)
            {
                if (group.Children[0] is ScaleTransform scale && !scale.IsFrozen)
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation: null);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation: null);
                }
                if (group.Children[1] is TranslateTransform translate && !translate.IsFrozen)
                {
                    translate.BeginAnimation(TranslateTransform.XProperty, animation: null);
                    translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                }
            }
        }

        private void StopPaneColumnAnimation()
        {
            _paneColumnAnimationGeneration++;
            _paneColumn?.BeginAnimation(ColumnDefinition.WidthProperty, animation: null);
        }

        /// <summary>
        /// Replaces frozen XAML-defined transforms with mutable instances on the supplied indicator.
        /// </summary>
        /// <param name="indicator">The indicator element to update.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0194:Merge is expressions on the same value", Justification = "Implementing this alters behaviour.")]
        private static void EnsureMutableTransform(FrameworkElement indicator)
        {
            indicator.BeginAnimation(OpacityProperty, animation: null);
            if (indicator.RenderTransform as TransformGroup is not TransformGroup group || group.IsFrozen || group.Children.Count < 2 || group.Children[0] is not ScaleTransform s || group.Children[1] is not TranslateTransform t || s.IsFrozen || t.IsFrozen)
            {
                TransformGroup newGroup = new();
                newGroup.Children.Add(new ScaleTransform(1.0, 1.0));
                newGroup.Children.Add(new TranslateTransform(0, 0));
                indicator.RenderTransform = newGroup;
                return;
            }
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform translate = (TranslateTransform)group.Children[1];
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation: null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation: null);
            translate.BeginAnimation(TranslateTransform.XProperty, animation: null);
            translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
        }

        private NavigationViewItem? ResolveNavigationViewItem(object? item)
        {
            return item is not NavigationViewItem nvi
                ? ItemContainerGenerator.ContainerFromItem(item) as NavigationViewItem
                : nvi;
        }

        internal void SelectItemFromContainer(NavigationViewItem navItem)
        {
            if (navItem is null)
            {
                return;
            }
            object data = GetDataFromContainer(navItem);
            if (!ReferenceEquals(SelectedItem, data))
            {
                SelectedItem = data;
            }
        }

        private object GetDataFromContainer(NavigationViewItem navItem)
        {
            object data = ItemContainerGenerator.ItemFromContainer(navItem);
            return (data != DependencyProperty.UnsetValue && data is not null) ? data : navItem;
        }

        private void OnTopOverflowButtonClick(object sender, RoutedEventArgs e)
        {
            if (_topOverflowButton?.ContextMenu is null || _topOverflowButton.ContextMenu.Items.Count == 0)
            {
                return;
            }

            _topOverflowButton.ContextMenu.PlacementTarget = _topOverflowButton;
            _topOverflowButton.ContextMenu.Placement = PlacementMode.Bottom;
            _topOverflowButton.ContextMenu.IsOpen = true;
        }

        private void OnTopOverflowMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem { Tag: NavigationViewItem navItem })
            {
                InvokeItem(navItem);
            }
        }

        private void ScheduleTopOverflowUpdate()
        {
            if (_topOverflowUpdateScheduled)
            {
                return;
            }

            _topOverflowUpdateScheduled = true;
            _ = Dispatcher.BeginInvoke(
                new Action(UpdateTopOverflow),
                DispatcherPriority.Loaded);
        }

        private void UpdateTopOverflow()
        {
            _topOverflowUpdateScheduled = false;

            if (_updatingTopOverflow)
            {
                return;
            }

            _updatingTopOverflow = true;
            try
            {
                List<NavigationViewItem> navItems = GetTopNavigationItems();
                foreach (NavigationViewItem navItem in navItems)
                {
                    if ((bool)navItem.GetValue(IsTopOverflowCollapsedProperty))
                    {
                        navItem.Visibility = Visibility.Visible;
                        navItem.ClearValue(IsTopOverflowCollapsedProperty);
                    }
                }

                if (PaneDisplayMode != NavigationViewPaneDisplayMode.Top || _topOverflowButton is null || _topItemsHost is null)
                {
                    if (_topOverflowButton is not null)
                    {
                        _topOverflowButton.Visibility = Visibility.Collapsed;
                        _topOverflowButton.ContextMenu = null;
                        SetTopOverflowButtonOffset(0.0);
                    }

                    return;
                }

                _topOverflowButton.Visibility = Visibility.Collapsed;
                _topOverflowButton.ContextMenu = null;
                SetTopOverflowButtonOffset(0.0);
                UpdateLayout();

                double availableWidth = _topItemsHost.ActualWidth;
                if (availableWidth <= 0.0)
                {
                    return;
                }

                double totalItemWidth = 0.0;
                foreach (NavigationViewItem navItem in navItems)
                {
                    if (navItem.Visibility == Visibility.Visible)
                    {
                        totalItemWidth += GetElementWidth(navItem);
                    }
                }

                _topOverflowButton.Visibility = Visibility.Visible;
                _topOverflowButton.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                double overflowButtonWidth = GetElementWidth(_topOverflowButton);
                if (totalItemWidth <= availableWidth)
                {
                    _topOverflowButton.Visibility = Visibility.Collapsed;
                    SetTopOverflowButtonOffset(0.0);
                    return;
                }

                double visibleItemsWidthLimit = Math.Max(
                    0.0,
                    availableWidth - overflowButtonWidth - TopOverflowReservedEndPadding);
                double usedWidth = 0.0;
                List<NavigationViewItem> overflowItems = [];

                foreach (NavigationViewItem navItem in navItems)
                {
                    if (navItem.Visibility != Visibility.Visible)
                    {
                        continue;
                    }

                    double itemWidth = GetElementWidth(navItem);
                    if (usedWidth + itemWidth <= visibleItemsWidthLimit)
                    {
                        usedWidth += itemWidth;
                    }
                    else
                    {
                        navItem.SetValue(IsTopOverflowCollapsedProperty, value: true);
                        navItem.Visibility = Visibility.Collapsed;
                        overflowItems.Add(navItem);
                    }
                }

                double overflowOffset = usedWidth;

                if (overflowItems.Count == 0)
                {
                    _topOverflowButton.Visibility = Visibility.Collapsed;
                    SetTopOverflowButtonOffset(0.0);
                    return;
                }

                SetTopOverflowButtonOffset(overflowOffset);
                _topOverflowButton.ContextMenu = CreateTopOverflowMenu(overflowItems);
            }
            finally
            {
                _updatingTopOverflow = false;
            }
        }

        private List<NavigationViewItem> GetTopNavigationItems()
        {
            List<NavigationViewItem> navItems = [];
            foreach (object item in Items)
            {
                NavigationViewItem? navItem = item as NavigationViewItem
                    ?? ItemContainerGenerator.ContainerFromItem(item) as NavigationViewItem;

                if (navItem is not null)
                {
                    navItems.Add(navItem);
                }
            }

            return navItems;
        }

        private void SetTopOverflowButtonOffset(double x)
        {
            if (_topOverflowButton is null)
            {
                return;
            }

            _topOverflowButton.RenderTransform = null;
            if (x > 0.0)
            {
                _topOverflowButton.RenderTransform = new TranslateTransform(Math.Max(0.0, x), 0.0);
            }
        }

        private System.Windows.Controls.ContextMenu CreateTopOverflowMenu(IReadOnlyList<NavigationViewItem> overflowItems)
        {
            ContextMenu menu = new();
            foreach (NavigationViewItem navItem in overflowItems)
            {
                MenuItem menuItem = new()
                {
                    Header = GetOverflowItemText(navItem),
                    Icon = CreateOverflowIcon(navItem),
                    MinWidth = 280,
                    MinHeight = 44,
                    Tag = navItem,
                };
                menuItem.Click += OnTopOverflowMenuItemClick;
                _ = menu.Items.Add(menuItem);
            }

            return menu;
        }

        private static object? CreateOverflowIcon(NavigationViewItem navItem)
        {
            if (navItem.Icon is not FontIcon fontIcon)
            {
                return null;
            }

            FontIcon overflowIcon = new()
            {
                Glyph = fontIcon.Glyph,
                IconFontFamily = fontIcon.IconFontFamily,
                IconFontSize = 16.0,
                MirroredWhenRightToLeft = fontIcon.MirroredWhenRightToLeft,
            };
            overflowIcon.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");

            return overflowIcon;
        }

        private static string GetOverflowItemText(NavigationViewItem navItem)
        {
            string text = navItem.Content as string
                ?? navItem.Content?.ToString()
                ?? navItem.Tag as string
                ?? string.Empty;

            return string.IsNullOrWhiteSpace(text) ? navItem.GetType().Name : text;
        }

        private static double GetElementWidth(FrameworkElement element)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double desiredWidth = Math.Max(element.DesiredSize.Width, element.MinWidth);
            return desiredWidth > 0.0 ? desiredWidth : element.ActualWidth;
        }

        private static NavigationViewItem? FindNavigationViewItem(DependencyObject? focused)
        {
            DependencyObject? current = focused;
            while (current is not null)
            {
                if (current is NavigationViewItem asItem)
                {
                    return asItem;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Walks up the visual tree from <paramref name="container"/> to the owning
        /// <see cref="NavigationView"/>. Unlike <see cref="ItemsControl.ItemsControlFromItemContainer"/>,
        /// this resolves correctly for items hosted in the nested <see cref="FooterMenuItems"/> host.
        /// </summary>
        /// <param name="container">The container element from which to start the search.</param>
        /// <returns>The owning <see cref="NavigationView"/> if found; otherwise, <see langword="null"/>.</returns>
        internal static NavigationView? FromItemContainer(DependencyObject? container)
        {
            DependencyObject? current = container;
            while (current is not null)
            {
                if (current is NavigationView nav)
                {
                    return nav;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Represents a reference to the back navigation button control.
        /// </summary>
        private System.Windows.Controls.Button? _backButton;

        /// <summary>
        /// Represents the toggle button control used to show or hide a pane within the user interface.
        /// </summary>
        private System.Windows.Controls.Button? _paneToggleButton;

        private FrameworkElement? _topItemsHost;

        private System.Windows.Controls.Button? _topOverflowButton;

        private FluenceWindow? _titleBarExtensionWindow;

        private DependencyPropertyDescriptor? _titleBarExtensionDescriptor;

        private ColumnDefinition? _paneColumn;

        /// <summary>
        /// Represents the visual element used to indicate the current selection within the user interface.
        /// </summary>
        private FrameworkElement? _selectionIndicator;

        /// <summary>
        /// Represents the host element for displaying an indicator within the user interface.
        /// </summary>
        private FrameworkElement? _indicatorHost;

        /// <summary>
        /// Selection indicator element for the footer region.
        /// </summary>
        private FrameworkElement? _footerSelectionIndicator;

        /// <summary>
        /// Host (coordinate parent) of <see cref="_footerSelectionIndicator"/>.
        /// </summary>
        private FrameworkElement? _footerIndicatorHost;


        /// <summary>
        /// Stores the current generation or version of the indicator animation.
        /// </summary>
        /// <remarks>This field is typically used to track changes or updates to the animation state,
        /// allowing the system to determine if a new animation sequence should be started or if the current one remains
        /// valid.</remarks>
        private int _indicatorAnimationGeneration;

        /// <summary>
        /// Generation counter guarding footer-indicator fade/scale animations, so a superseded
        /// arrive/depart animation's completion callback does not stomp a newer one.
        /// </summary>
        private int _footerAnimationGeneration;

        private int _paneColumnAnimationGeneration;

        /// <summary>
        /// Pane width captured at a Left &lt;-&gt; LeftCompact display-mode switch, consumed by the next
        /// <see cref="OnApplyTemplate"/> so the new template's pane column animates from it (the swap
        /// discards the old column, so the flight cannot run on the original element).
        /// </summary>
        private double? _pendingPaneWidthAnimationFrom;

        private bool _topOverflowUpdateScheduled;

        private bool _updatingTopOverflow;

        private bool _updatingTitleBarExtension;

        /// <summary>
        /// Indicates whether the indicator has been positioned.
        /// </summary>
        private bool _indicatorPositioned;
    }
}
