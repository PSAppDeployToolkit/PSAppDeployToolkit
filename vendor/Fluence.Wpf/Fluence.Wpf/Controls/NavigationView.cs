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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A navigation control with a collapsible pane and content area, similar to WinUI NavigationView.
    /// Uses a single shared selection indicator that animates between items.
    /// </summary>
    [TemplatePart(Name = PART_BackButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_ContentPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_PaneItemsScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PART_PaneToggleButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_SelectionIndicator, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PART_FooterItemsHost, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PART_FooterSelectionIndicator, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartPaneColumn, Type = typeof(ColumnDefinition))]
    [TemplatePart(Name = PART_TopItemsHost, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PART_TopOverflowButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonVisible")]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonCollapsed")]
    public class NavigationView : Selector
    {
        /// <summary>
        /// Name of the back button template part.
        /// </summary>
        internal const string PART_BackButton = "PART_BackButton";

        /// <summary>
        /// Name of the main content presenter template part.
        /// </summary>
        internal const string PART_ContentPresenter = "PART_ContentPresenter";

        /// <summary>
        /// Name of the scroll viewer that hosts pane items.
        /// </summary>
        internal const string PART_PaneItemsScrollViewer = "PART_PaneItemsScrollViewer";

        /// <summary>
        /// Name of the pane collapse/expand toggle button.
        /// </summary>
        internal const string PART_PaneToggleButton = "PART_PaneToggleButton";

        /// <summary>
        /// Name of the shared selection indicator element.
        /// </summary>
        internal const string PART_SelectionIndicator = "PART_SelectionIndicator";

        /// <summary>
        /// Name of the items host that renders <see cref="FooterMenuItems"/>.
        /// </summary>
        internal const string PART_FooterItemsHost = "PART_FooterItemsHost";

        /// <summary>
        /// Name of the selection indicator element for the footer items region.
        /// </summary>
        internal const string PART_FooterSelectionIndicator = "PART_FooterSelectionIndicator";

        /// <summary>
        /// Name of the top pane items host template part.
        /// </summary>
        internal const string PART_TopItemsHost = "PART_TopItemsHost";

        /// <summary>
        /// Name of the top pane overflow button template part.
        /// </summary>
        internal const string PART_TopOverflowButton = "PART_TopOverflowButton";

        private const string PartPaneColumn = "PaneColumn";
        private const double PaneClosedWidth = 48.0;
        private const double PaneOpenWidth = 320.0;
        // WinUI's SplitView opens its pane over 350 ms and closes it over 120 ms, both on the
        // 0.1,0.9 0.2,1.0 key spline (SplitView_themeresources.xaml:65,236). The asymmetry is the
        // point: the pane leaves quickly and arrives unhurried. WinUI translates an overlay pane
        // where this animates an inline column's width, but the curve and the timings carry.
        private const double PaneOpenAnimationMilliseconds = 350.0;
        private const double PaneCloseAnimationMilliseconds = 120.0;

        private static readonly DependencyProperty IsTopOverflowCollapsedProperty =
            DependencyProperty.RegisterAttached(
                "IsTopOverflowCollapsed",
                typeof(bool),
                typeof(NavigationView),
                new PropertyMetadata(defaultValue: false));

        /// <summary>
        /// Caches the natural width a top-pane item last measured at, so the overflow pass measures an
        /// item only when its size actually changed. The value lives on the item, so it is released
        /// with the container and cannot keep a removed item alive. <see cref="double.NaN"/> means
        /// "not measured yet".
        /// </summary>
        private static readonly DependencyProperty TopOverflowItemWidthProperty =
            DependencyProperty.RegisterAttached(
                "TopOverflowItemWidth",
                typeof(double),
                typeof(NavigationView),
                new PropertyMetadata(double.NaN));

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
        // The indicator sits flush with the left edge of the selected item's painted pill, drawn
        // over the pill fill, which is where WinUI puts it: a top-level selected item in the WinUI
        // 3 Gallery measures its indicator's left edge at the same x as the pill's. The pill starts
        // at the item's own origin plus NavigationViewItemButtonMargin (4), so this is that 4.
        private const double NavigationItemOuterHorizontalMargin = 4.0;
        // Gap between the bottom edge of a top-mode item and the indicator under it. WinUI's top
        // item template gives its indicator a 4 dip bottom margin inside the item.
        private const double TopIndicatorBottomInset = 4.0;
        private const double TopOverflowReservedEndPadding = 12.0;

        // Width an item must clear beyond the fitting limit before the overflow pass brings it back
        // out of the menu. Without the dead band a slow drag across an item's threshold flaps that
        // item between the strip and the menu. WinUI reserves the same 5px through
        // m_topNavigationRecoveryGracePeriodWidth.
        private const double TopOverflowRecoveryGraceWidth = 5.0;

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
            new PropertyMetadata(OnPaneFooterChanged));

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

        private static readonly DependencyPropertyKey PaneFooterSeparatorVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PaneFooterSeparatorVisibility),
                typeof(Visibility),
                typeof(NavigationView),
                new FrameworkPropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Identifies the <see cref="PaneFooterSeparatorVisibility"/> dependency property. Internal:
        /// this is an implementation detail of the default template, not a consumer-facing DP.
        /// </summary>
        internal static readonly DependencyProperty PaneFooterSeparatorVisibilityProperty =
            PaneFooterSeparatorVisibilityPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey HostExtendsContentIntoTitleBarPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HostExtendsContentIntoTitleBar),
                typeof(bool),
                typeof(NavigationView),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the <see cref="HostExtendsContentIntoTitleBar"/> dependency property. Internal:
        /// this is an implementation detail of the default template, not a consumer-facing DP.
        /// </summary>
        internal static readonly DependencyProperty HostExtendsContentIntoTitleBarProperty =
            HostExtendsContentIntoTitleBarPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey HostHasTitleBarPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HostHasTitleBar),
                typeof(bool),
                typeof(NavigationView),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the <see cref="HostHasTitleBar"/> dependency property. Internal: this is an
        /// implementation detail of the default template, not a consumer-facing DP.
        /// </summary>
        internal static readonly DependencyProperty HostHasTitleBarProperty =
            HostHasTitleBarPropertyKey.DependencyProperty;

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
            UpdatePaneFooterSeparatorVisibility();
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
        /// WinUI <c language="xaml">NavigationView.FooterMenuItems</c> region.
        /// </summary>
        public ObservableCollection<object> FooterMenuItems => (ObservableCollection<object>)GetValue(FooterMenuItemsProperty);

        /// <summary>
        /// Gets the visibility of the divider drawn between the scrolling menu items and the pane
        /// footer (WinUI's VisualItemsSeparator). Visible only while the footer holds something to
        /// divide from, which is pinned footer menu items, free-form <see cref="PaneFooter"/>
        /// content, or both. Internal: an implementation detail of the default template, bound to
        /// it via <c language="csharp">RelativeSource TemplatedParent</c> rather than exposed to
        /// consumers.
        /// </summary>
        internal Visibility PaneFooterSeparatorVisibility => (Visibility)GetValue(PaneFooterSeparatorVisibilityProperty);

        /// <summary>
        /// Gets a value indicating whether the owning <see cref="FluenceWindow"/> extends its
        /// content into the title bar. Mirrored onto this control so the default template can read
        /// it through <c language="csharp">RelativeSource TemplatedParent</c>. A
        /// <c language="csharp">FindAncestor</c> binding would read the same value, but WPF
        /// re-evaluates one while the window is tearing the visual tree down, when the ancestor is
        /// already unreachable, and logs a binding error per trigger for every NavigationView on
        /// the way out. Internal: an implementation detail of the template.
        /// </summary>
        internal bool HostExtendsContentIntoTitleBar => (bool)GetValue(HostExtendsContentIntoTitleBarProperty);

        /// <summary>
        /// Gets a value indicating whether the owning <see cref="FluenceWindow"/> carries title bar
        /// content of its own, which the pane uses to decide whether to draw its own back and pane
        /// toggle buttons. Mirrored for the same reason as
        /// <see cref="HostExtendsContentIntoTitleBar"/>. Internal: an implementation detail of the
        /// template.
        /// </summary>
        internal bool HostHasTitleBar => (bool)GetValue(HostHasTitleBarProperty);

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
            _backButton = GetTemplateChild(PART_BackButton) as System.Windows.Controls.Button;
            _backButton?.Click += OnBackButtonClick;
            _paneToggleButton = GetTemplateChild(PART_PaneToggleButton) as System.Windows.Controls.Button;
            _paneToggleButton?.Click += OnPaneToggleButtonClick;
            _topItemsHost = GetTemplateChild(PART_TopItemsHost) as FrameworkElement;
            if (GetTemplateChild(PART_TopOverflowButton) is System.Windows.Controls.Button topOverflowButton)
            {
                _topOverflowButton = topOverflowButton;
                _topOverflowButton.Click += OnTopOverflowButtonClick;
            }
            else
            {
                _topOverflowButton = null;
            }

            _paneColumn = GetTemplateChild(PartPaneColumn) as ColumnDefinition;
            _selectionIndicator = GetTemplateChild(PART_SelectionIndicator) as FrameworkElement;
            _indicatorHost = _selectionIndicator is not null ? VisualTreeHelper.GetParent(_selectionIndicator) as FrameworkElement : null;
            _footerSelectionIndicator = GetTemplateChild(PART_FooterSelectionIndicator) as FrameworkElement;

            // The footer indicator host must be an ancestor of the footer items so that
            // CalculateIndicatorPosition's TransformToAncestor succeeds. In Left/LeftCompact the
            // indicator is a direct child of the Grid that also hosts PART_FooterItemsHost; in Top it
            // sits in a zero-size Canvas inside that same Grid (the Canvas fills the cell at its
            // origin, so its coordinate space matches the Grid's). Resolving the host from the items
            // host's parent therefore works for every pane mode, where using the indicator's immediate
            // parent (the Canvas in Top mode) is not an ancestor of the items and the transform fails.
            FrameworkElement? footerItemsHost = GetTemplateChild(PART_FooterItemsHost) as FrameworkElement;
            _footerIndicatorHost = (footerItemsHost is not null ? VisualTreeHelper.GetParent(footerItemsHost) as FrameworkElement : null)
                ?? (_footerSelectionIndicator is not null ? VisualTreeHelper.GetParent(_footerSelectionIndicator) as FrameworkElement : null);
            foreach (NavigationViewItem entry in FooterMenuItems.OfType<NavigationViewItem>())
            {
                HookFooterItem(entry);
            }
            _indicatorPositioned = false;
            StopAnimation();
            CoerceTopPaneProperties();
            UpdateTitleBarExtensionForPaneMode();
            UpdateBackButtonState(useTransitions: false);
            ApplyPaneColumnWidthOnTemplateApplied();

            // A different pane template measures the items differently, so widths cached under the
            // previous template must not carry into the first overflow pass under this one.
            foreach (NavigationViewItem navItem in GetTopNavigationItems())
            {
                navItem.ClearValue(TopOverflowItemWidthProperty);
            }

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
            _ = Dispatcher.BeginInvoke(new Action(() => RefreshIndicators(animate: true)), DispatcherPriority.Loaded);
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
                navItem.ClearValue(TopOverflowItemWidthProperty);
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
            if ((item?.IsEnabled) is not true)
            {
                return;
            }
            bool isFooter = IsFooterItem(item);
            object invokedItem = isFooter ? item : GetDataFromContainer(item);
            ItemInvoked?.Invoke(this, new NavigationViewItemInvokedEventArgs(invokedItem, item, isSettingsInvoked: false));
            SelectItemFromContainer(item);
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

            foreach (NavigationViewItem footerItem in FooterMenuItems.OfType<NavigationViewItem>().Where(footerItem => !ReferenceEquals(footerItem, item)))
            {
                footerItem.IsSelected = false;
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

            UpdatePaneFooterSeparatorVisibility();
            ScheduleIndicatorPosition(animate: false);
        }

        private static void OnPaneFooterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NavigationView)d).UpdatePaneFooterSeparatorVisibility();
        }

        /// <summary>
        /// Shows the divider above the pane footer only when there is something below it to divide
        /// from: pinned footer menu items, free-form <see cref="PaneFooter"/> content, or both. A
        /// pane with an empty footer would otherwise draw a rule against nothing.
        /// </summary>
        private void UpdatePaneFooterSeparatorVisibility()
        {
            bool hasFooterContent = FooterMenuItems.Count > 0 || PaneFooter is not null;
            SetValue(
                PaneFooterSeparatorVisibilityPropertyKey,
                hasFooterContent ? Visibility.Visible : Visibility.Collapsed);
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
            footerItem.ClearValue(TopOverflowItemWidthProperty);
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
        /// Transitions the back button to the correct <c language="xaml">BackButtonStates</c> VSM state
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
            // Evict the cached natural width so the next pass re-measures this item. A zero width is
            // the arrange of an item this control just moved into the overflow menu, not a content
            // change, so it must not throw away the width the pass measured moments earlier. An
            // arrange at (effectively) the cached width is not a content change either - it is the
            // strip arranging an item the pass just recovered from the menu - so only a genuinely
            // different width evicts; otherwise every recovery would trigger a pointless re-measure.
            if (e.NewSize.Width > 0.0 && sender is NavigationViewItem navItem)
            {
                double cachedWidth = (double)navItem.GetValue(TopOverflowItemWidthProperty);
                if (double.IsNaN(cachedWidth) || Math.Abs(cachedWidth - e.NewSize.Width) > 0.5)
                {
                    navItem.ClearValue(TopOverflowItemWidthProperty);
                }
            }

            ScheduleTopOverflowUpdate();
        }

        /// <summary>
        /// Returns whether the top-overflow pass has a cached natural width for the item.
        /// </summary>
        /// <param name="item">The top-pane item to check.</param>
        internal static bool HasCachedTopItemWidth(NavigationViewItem item)
        {
            return !double.IsNaN((double)item.GetValue(TopOverflowItemWidthProperty));
        }

        /// <summary>
        /// Evicts the item's cached natural width and schedules an overflow pass on its owning
        /// <see cref="NavigationView"/>. Called by <see cref="NavigationViewItem"/> when a
        /// measure-affecting property changes, because an item sitting collapsed inside the
        /// overflow menu is never measured and so never raises the <c language="csharp">SizeChanged</c> that is the
        /// ordinary eviction path - without this, shrinking an overflowed item's content leaves it
        /// pinned in the menu on its stale (larger) cached width forever.
        /// </summary>
        /// <param name="item">The top-pane item whose cached width is stale.</param>
        internal static void InvalidateTopItemWidth(NavigationViewItem item)
        {
            item.ClearValue(TopOverflowItemWidthProperty);
            FromItemContainer(item)?.ScheduleTopOverflowUpdate();
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

            if (newMode is NavigationViewPaneDisplayMode.LeftCompact)
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
            return nav.PaneDisplayMode is NavigationViewPaneDisplayMode.Top || (bool)baseValue;
        }

        private static object CoerceIsPaneToggleButtonVisible(DependencyObject d, object baseValue)
        {
            _ = baseValue;
            NavigationView nav = (NavigationView)d;
            return nav.PaneDisplayMode is not NavigationViewPaneDisplayMode.Top;
        }

        private void CoerceTopPaneProperties()
        {
            CoerceValue(IsPaneOpenProperty);
            CoerceValue(IsPaneToggleButtonVisibleProperty);
        }

        private void UpdateTitleBarExtensionForPaneMode()
        {
            // Only the window's own shell pane drives the window's title bar extension. A
            // NavigationView nested in page content is a control on the page, so letting it write
            // FluenceWindow.ExtendsContentIntoTitleBar would let a Top mode sample switch the whole
            // shell's title bar off the moment the page loaded.
            if (!IsShellNavigationView() || Window.GetWindow(this) is not FluenceWindow window || _updatingTitleBarExtension)
            {
                return;
            }

            bool? desiredValue = null;
            if (PaneDisplayMode is NavigationViewPaneDisplayMode.Left)
            {
                desiredValue = true;
            }
            else if (PaneDisplayMode is NavigationViewPaneDisplayMode.Top)
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
                _titleBarContentDescriptor ??= DependencyPropertyDescriptor.FromProperty(
                    FluenceWindow.TitleBarProperty,
                    typeof(FluenceWindow));
                _titleBarContentDescriptor?.AddValueChanged(_titleBarExtensionWindow, OnTitleBarExtensionChanged);
            }

            UpdateHostTitleBarState();
        }

        private void DetachTitleBarWindowWatcher()
        {
            if (_titleBarExtensionWindow is not null)
            {
                _titleBarExtensionDescriptor?.RemoveValueChanged(_titleBarExtensionWindow, OnTitleBarExtensionChanged);
                _titleBarContentDescriptor?.RemoveValueChanged(_titleBarExtensionWindow, OnTitleBarExtensionChanged);
                _titleBarExtensionWindow = null;
            }

            UpdateHostTitleBarState();
        }

        private void OnTitleBarExtensionChanged(object? sender, EventArgs e)
        {
            UpdateHostTitleBarState();
            UpdateTitleBarExtensionForPaneMode();
        }

        /// <summary>
        /// Copies the owning window's title bar state onto this control so the default template can
        /// read it from the templated parent instead of walking up to the window itself. The walk
        /// is the problem: during shutdown WPF re-evaluates the template's triggers after the
        /// control has left the visual tree, the ancestor is no longer reachable, and each one logs
        /// a "cannot find source" binding error. Mirroring the two values here keeps the triggers
        /// on a source that is always resolvable.
        /// </summary>
        private void UpdateHostTitleBarState()
        {
            // Only the window's own shell pane answers to the window's title bar. A NavigationView
            // nested inside page content (a gallery sample, say) is a control on the page, not the
            // window's navigation surface, so the window's title bar neither hosts its chrome nor
            // sets its pane height; it keeps drawing its own back and pane toggle buttons.
            FluenceWindow? window = IsShellNavigationView() ? _titleBarExtensionWindow : null;
            SetValue(HostExtendsContentIntoTitleBarPropertyKey, window?.ExtendsContentIntoTitleBar is true);
            SetValue(HostHasTitleBarPropertyKey, window?.TitleBar is not null);
        }

        /// <summary>
        /// Reports whether this is the window's shell navigation pane rather than one nested in the
        /// content of another. Nesting is the test because the shell pane hosts the content every
        /// other pane on screen sits inside, so an ancestor NavigationView means this one belongs
        /// to a page rather than to the window.
        /// </summary>
        /// <returns><see langword="true"/> when no ancestor is a <see cref="NavigationView"/>.</returns>
        private bool IsShellNavigationView()
        {
            DependencyObject? ancestor = VisualTreeHelper.GetParent(this);
            while (ancestor is not null)
            {
                if (ancestor is NavigationView)
                {
                    return false;
                }

                ancestor = VisualTreeHelper.GetParent(ancestor);
            }

            return true;
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
            if (!useAnimation || !MotionHelper.IsMotionEnabled)
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
            bool opening = targetWidth > currentWidth;
            GridLengthAnimation animation = new()
            {
                From = new GridLength(currentWidth),
                To = new GridLength(targetWidth),
                Duration = new Duration(TimeSpan.FromMilliseconds(
                    opening ? PaneOpenAnimationMilliseconds : PaneCloseAnimationMilliseconds)),
                EasingFunction = new KeySplineEase(0.1, 0.9, 0.2, 1.0),
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
            return current.GridUnitType is GridUnitType.Pixel
                ? current.Value
                : GetClosedPaneWidth();
        }

        /// <summary>
        /// The width of the closed pane, which is the compact rail. It does not depend on the back
        /// button: the back button and the pane toggle share the chrome row above the rail and the
        /// row is free to run wider than the rail, so showing the back button moves the toggle
        /// along rather than widening the pane. Widening it here made enabling the back button look
        /// like the pane had been opened, which is what pressing the toggle is for.
        /// </summary>
        /// <returns>The closed pane width in device independent pixels.</returns>
        private static double GetClosedPaneWidth()
        {
            return PaneClosedWidth;
        }

        private void ScheduleIndicatorPosition(bool animate)
        {
            _ = Dispatcher.BeginInvoke(new Action(() => RefreshIndicators(animate)), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Repositions both the main and footer selection indicators. The selection model guarantees
        /// at most one region (main menu or footer) owns the selection, so at most one indicator shows.
        /// </summary>
        /// <param name="animate">Indicates whether to animate the indicator movement.</param>
        private void RefreshIndicators(bool animate)
        {
            PositionIndicator(animate);
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

            bool topMode = PaneDisplayMode is NavigationViewPaneDisplayMode.Top;
            bool shouldShow = IsLoaded
                && (SelectedFooterItem?.IsVisible) is true
                && SelectedFooterItem.ActualHeight > 0;

            if (!shouldShow)
            {
                // Animate the indicator out when leaving a selected footer item (e.g. navigating
                // away from Settings); snap to hidden when nothing was showing or animation is off.
                // Every pane mode fades it, as WinUI does: the indicator is one element whose
                // appearance is animated by NavigationViewItemPresenter regardless of orientation,
                // and only the axis it scales along follows the pane mode.
                bool wasVisible = _footerSelectionIndicator.Opacity > 0.01;
                AnimateFooterIndicatorVisibility(appearing: false, topMode, animate && wasVisible);
                return;
            }

            Point targetPosition = CalculateIndicatorPosition(SelectedFooterItem!, _footerSelectionIndicator, _footerIndicatorHost, topMode);

            bool wasHidden = _footerSelectionIndicator.Opacity < 0.01;
            StopFooterAnimation();
            EnsureMutableTransform(_footerSelectionIndicator);
            TransformGroup group = (TransformGroup)_footerSelectionIndicator.RenderTransform;
            TranslateTransform translate = (TranslateTransform)group.Children[1];
            translate.X = targetPosition.X;
            translate.Y = targetPosition.Y;

            // Fade + scale the indicator in when it first appears on a footer item; a reflow while it
            // is already shown just repositions it at full opacity.
            AnimateFooterIndicatorVisibility(appearing: true, topMode, animate && wasHidden);
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

            _footerAnimationGeneration++;

            // Capture-and-hold: read the current animated values BEFORE any clock is released so a
            // retarget mid-flight continues from wherever the indicator visually is right now.
            double currentScale = 1.0;
            double currentOpacity = _footerSelectionIndicator.Opacity;
            if (_footerSelectionIndicator.RenderTransform is TransformGroup liveGroup
                && liveGroup.Children.Count >= 2
                && liveGroup.Children[0] is ScaleTransform liveScale)
            {
                currentScale = topMode ? liveScale.ScaleX : liveScale.ScaleY;
            }

            // EnsureMutableTransform releases every indicator clock (properties snap to their base
            // values); the captured values are then written back as the new base values.
            EnsureMutableTransform(_footerSelectionIndicator);
            TransformGroup group = (TransformGroup)_footerSelectionIndicator.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            DependencyProperty scaleProperty = topMode ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty;

            double toOpacity = appearing ? 1.0 : 0.0;
            if (!animate || !MotionHelper.IsMotionEnabled)
            {
                scale.ScaleX = 1.0;
                scale.ScaleY = 1.0;
                _footerSelectionIndicator.Opacity = toOpacity;
                return;
            }

            int animationId = _footerAnimationGeneration;
            double toScale = appearing ? 1.0 : 0.72;
            Duration duration = new(TimeSpan.FromMilliseconds(appearing ? 140.0 : 90.0));
            CubicEase ease = new() { EasingMode = appearing ? EasingMode.EaseOut : EasingMode.EaseIn };

            // An appear from fully hidden legitimately starts at 0.72 scale / 0.0 opacity; any other
            // start continues from the captured live values.
            if (appearing && currentOpacity < 0.01)
            {
                currentScale = 0.72;
                currentOpacity = 0.0;
            }

            // Hold the start state; the cross axis stays at 1.0 so only the indicator's length scales.
            scale.ScaleX = topMode ? currentScale : 1.0;
            scale.ScaleY = topMode ? 1.0 : currentScale;
            _footerSelectionIndicator.Opacity = currentOpacity;

            // To-only animations (no From): each begins from the live base value held above, so a
            // retarget mid-flight hands off smoothly instead of replaying from the seeded start.
            DoubleAnimation scaleAnimation = new()
            {
                To = toScale,
                Duration = duration,
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop,
            };
            DoubleAnimation opacityAnimation = new()
            {
                To = toOpacity,
                Duration = duration,
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

        private void PositionIndicator(bool animate)
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

            bool topMode = PaneDisplayMode is NavigationViewPaneDisplayMode.Top;
            Point targetPosition = CalculateIndicatorPosition(nvi, _selectionIndicator, _indicatorHost, topMode);
            if (!animate || !_indicatorPositioned || !MotionHelper.IsMotionEnabled)
            {
                SnapIndicator(targetPosition);
                return;
            }

            Point currentPosition = GetCurrentIndicatorPosition();
            AnimateIndicator(currentPosition, targetPosition, topMode);
        }

        /// <summary>
        /// Calculates the translate position for the supplied indicator relative to its host Grid.
        /// </summary>
        /// <param name="item">The navigation view item for which to calculate the indicator position.</param>
        /// <param name="indicator">The indicator element.</param>
        /// <param name="host">The host element containing the indicator.</param>
        /// <param name="topMode">Indicates whether the navigation view is in top mode.</param>
        /// <returns>The calculated position for the indicator.</returns>
        private static Point CalculateIndicatorPosition(NavigationViewItem item, FrameworkElement indicator, FrameworkElement host, bool topMode)
        {
            try
            {
                GeneralTransform transform = item.TransformToAncestor(host);
                Point itemPos = transform.Transform(new Point(0, 0));
                if (topMode)
                {
                    // The indicator is bottom aligned inside a host that spans the whole 48 dip bar,
                    // while the items are centred in it, so at rest the indicator lies on the bar's
                    // bottom edge rather than under the item it marks. WinUI makes the indicator a
                    // child of the top item itself, inset from the item's own bottom edge, so the
                    // translate here lifts it by the difference.
                    double y = itemPos.Y + item.ActualHeight - TopIndicatorBottomInset - host.ActualHeight;
                    return new Point(itemPos.X + ((item.ActualWidth - indicator.Width) / 2.0), y);
                }

                // Depth never moves the indicator. WinUI applies Depth() * c_itemIndentation (31,
                // NavigationViewItemBase.h:63) to the presenter's ContentGrid alone
                // (NavigationViewItemPresenter.cpp:264-276), and the indicator sits in a sibling
                // wrapper grid nothing writes to (NavigationView_themeresources.xaml:601-604), so
                // the selection rail stays one straight column down the pane at any tree depth.
                double x = itemPos.X + NavigationItemOuterHorizontalMargin;
                return new Point(x, itemPos.Y + ((item.ActualHeight - indicator.Height) / 2.0));
            }
            catch (InvalidOperationException ex)
            {
                // TransformToAncestor throws this when the item is not under the host yet, which
                // happens while a pane template is being swapped; the indicator is repositioned on
                // the next pass, so the fallback offset is only ever seen for one frame.
                Debug.WriteLine($"NavigationView indicator transform failed: {ex}");
                return new Point(0, 0);
            }
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
            bool topMode)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            _indicatorAnimationGeneration++;

            // Capture-and-hold: read the current animated values BEFORE any clock is released so a
            // retarget mid-flight continues from wherever the indicator visually is right now.
            double currentX = fromPosition.X;
            double currentY = fromPosition.Y;
            double currentScaleX = 1.0;
            double currentScaleY = 1.0;
            double currentOpacity = _selectionIndicator.Opacity;
            if (_selectionIndicator.RenderTransform is TransformGroup liveGroup
                && liveGroup.Children.Count >= 2
                && liveGroup.Children[0] is ScaleTransform liveScale
                && liveGroup.Children[1] is TranslateTransform liveTranslate)
            {
                currentX = liveTranslate.X;
                currentY = liveTranslate.Y;
                currentScaleX = liveScale.ScaleX;
                currentScaleY = liveScale.ScaleY;
            }

            // EnsureMutableTransform releases every indicator clock (properties snap to their base
            // values); the captured values are then written back as the new base values.
            EnsureMutableTransform(_selectionIndicator);
            TransformGroup group = (TransformGroup)_selectionIndicator.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform translate = (TranslateTransform)group.Children[1];
            int animationId = _indicatorAnimationGeneration;
            DependencyProperty axisProperty = topMode ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            DependencyProperty scaleProperty = topMode ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty;
            double fromAxis = topMode ? fromPosition.X : fromPosition.Y;
            double toAxis = topMode ? toPosition.X : toPosition.Y;
            double direction = toAxis < fromAxis ? -1.0 : 1.0;

            translate.X = currentX;
            translate.Y = currentY;
            scale.ScaleX = currentScaleX;
            scale.ScaleY = currentScaleY;
            _selectionIndicator.Opacity = currentOpacity;

            // Cross-axis correction: the non-animated axis must sit at the new resting values.
            if (topMode)
            {
                translate.Y = toPosition.Y;
                scale.ScaleY = 1.0;
            }
            else
            {
                translate.X = toPosition.X;
                scale.ScaleX = 1.0;
            }

            // WinUI's indicator never blinks out between items, and it does not glide across the
            // gap either: for a move inside one list it plays a rubber band (NavigationView.cpp,
            // PlayIndicatorAnimations). The bar holds its old position and stretches until it spans
            // both items, then its offset and its scale origin snap together at the one third mark,
            // so the same stretched bar is now anchored at the destination and contracts into it.
            // The snap is invisible because the bar covers both positions at that instant.
            //
            // WinUI animates Offset, Scale and CenterPoint on two per-item indicators. This port has
            // one pane-level bar, so the three animations run on its own transform, and the scale
            // origin stands in for CenterPoint.
            double axisLength = GetIndicatorLength(topMode);
            double distance = Math.Abs(toAxis - fromAxis);
            double peakScale = axisLength > 0 ? (distance / axisLength) + 1.0 : 1.0;
            bool forward = toAxis > fromAxis;

            Duration travelDuration = new(TimeSpan.FromMilliseconds(600));
            TimeSpan snapTime = TimeSpan.FromMilliseconds(200);

            // Offset: held, then stepped to the destination at the snap. WinUI uses a step easing
            // function for the same reason; the stretch is what carries the eye across the gap.
            DoubleAnimationUsingKeyFrames axisAnimation = new()
            {
                Duration = travelDuration,
                FillBehavior = FillBehavior.Stop,
            };
            _ = axisAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(fromAxis, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            _ = axisAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(toAxis, KeyTime.FromTimeSpan(snapTime)));

            // Scale: out to span the gap on WinUI's accelerating ramp, back to rest on its
            // decelerating settle.
            DoubleAnimationUsingKeyFrames scaleAnimation = new()
            {
                Duration = travelDuration,
                FillBehavior = FillBehavior.Stop,
            };
            _ = scaleAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(
                peakScale,
                KeyTime.FromTimeSpan(snapTime),
                new KeySpline(0.9, 0.1, 1.0, 0.2)));
            _ = scaleAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(
                1.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600)),
                new KeySpline(0.1, 0.9, 0.2, 1.0)));

            // The scale origin is WinUI's CenterPoint: it sits on the edge the bar grows from, and
            // snaps to the opposite edge as the offset lands, so the stretch that grew toward the
            // destination becomes the stretch that contracts into it.
            Point growOrigin = topMode
                ? new Point(forward ? 0.0 : 1.0, 0.5)
                : new Point(0.5, forward ? 0.0 : 1.0);
            Point settleOrigin = topMode
                ? new Point(forward ? 1.0 : 0.0, 0.5)
                : new Point(0.5, forward ? 1.0 : 0.0);
            PointAnimationUsingKeyFrames originAnimation = new()
            {
                Duration = travelDuration,
                FillBehavior = FillBehavior.Stop,
            };
            _ = originAnimation.KeyFrames.Add(new DiscretePointKeyFrame(growOrigin, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            _ = originAnimation.KeyFrames.Add(new DiscretePointKeyFrame(settleOrigin, KeyTime.FromTimeSpan(snapTime)));

            // Full opacity throughout: any clock left from an interrupted move is released so it
            // cannot fade this one out underneath.
            _selectionIndicator.BeginAnimation(OpacityProperty, animation: null);
            _selectionIndicator.Opacity = 1.0;

            axisAnimation.Completed += delegate
            {
                if (animationId != _indicatorAnimationGeneration)
                {
                    return;
                }

                translate.BeginAnimation(axisProperty, animation: null);
                scale.BeginAnimation(scaleProperty, animation: null);
                _selectionIndicator.BeginAnimation(RenderTransformOriginProperty, animation: null);

                translate.X = toPosition.X;
                translate.Y = toPosition.Y;
                scale.ScaleX = 1.0;
                scale.ScaleY = 1.0;
                _selectionIndicator.RenderTransformOrigin = new Point(0.5, 0.5);
                _selectionIndicator.Opacity = 1.0;
                _indicatorPositioned = true;
            };

            _indicatorPositioned = true;
            _selectionIndicator.BeginAnimation(RenderTransformOriginProperty, originAnimation, HandoffBehavior.SnapshotAndReplace);
            translate.BeginAnimation(axisProperty, axisAnimation, HandoffBehavior.SnapshotAndReplace);
            scale.BeginAnimation(scaleProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
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
            if (IsFooterItem(navItem))
            {
                SelectFooterItem(navItem);
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
            if (_topOverflowButton?.ContextMenu is null || _topOverflowButton.ContextMenu.Items.Count is 0)
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
                HashSet<NavigationViewItem>? recoveringItems = null;
                foreach (NavigationViewItem navItem in navItems.Where(static navItem => (bool)navItem.GetValue(IsTopOverflowCollapsedProperty)))
                {
                    // Items the previous pass hid are candidates for recovery in this one, and only
                    // those items pay the recovery grace below.
                    recoveringItems ??= [];
                    _ = recoveringItems.Add(navItem);
                    navItem.Visibility = Visibility.Visible;
                    navItem.ClearValue(IsTopOverflowCollapsedProperty);
                }

                if (PaneDisplayMode is not NavigationViewPaneDisplayMode.Top || _topOverflowButton is null || _topItemsHost is null)
                {
                    HideTopOverflowChrome();
                    return;
                }

                _topOverflowButton.Visibility = Visibility.Collapsed;
                _topOverflowButton.ContextMenu = null;
                SetTopOverflowButtonOffset(0.0);

                // PART_TopItemsHost is the star-sized column of the pane header grid, so the width it
                // was last arranged at does not depend on how many items are visible; un-collapsing the
                // items above cannot change it, and no forced layout pass is needed to read it. A width
                // of zero means the host has not been arranged yet: bail, because that arrange raises
                // SizeChanged on this control or on its items, either of which schedules another pass.
                double availableWidth = _topItemsHost.ActualWidth;
                if (availableWidth <= 0.0)
                {
                    ClearTopOverflowMenu();
                    return;
                }

                double totalItemWidth = 0.0;
                foreach (NavigationViewItem navItem in navItems.Where(static navItem => navItem.Visibility is Visibility.Visible))
                {
                    totalItemWidth += GetTopItemWidth(navItem);
                }

                _topOverflowButton.Visibility = Visibility.Visible;

                // MeasureElementWidth measures the button itself, so no separate Measure pass here.
                double overflowButtonWidth = MeasureElementWidth(_topOverflowButton);

                // The all-items-fit exit pays the same recovery grace as the per-item loop when the
                // previous pass had anything in the menu. Without it, a width oscillating one pixel
                // around the exact-fit total alternates between this exit (everything visible) and
                // the loop below (tail collapsed), flapping the last item - the exact flap the grace
                // exists to damp. A steady state with nothing overflowed keeps the plain limit.
                double allFitWidthLimit = recoveringItems is null
                    ? availableWidth
                    : Math.Max(0.0, availableWidth - TopOverflowRecoveryGraceWidth);
                if (totalItemWidth <= allFitWidthLimit)
                {
                    HideTopOverflowChrome();
                    return;
                }

                double visibleItemsWidthLimit = Math.Max(
                    0.0,
                    availableWidth - overflowButtonWidth - TopOverflowReservedEndPadding);

                // An item that is already in the menu has to clear the limit by the recovery grace
                // before it returns to the strip, so a drag that walks the pane width back and forth
                // across a threshold does not flap that item. Items that are visible keep the plain
                // limit, which leaves a steady-state pass byte-identical to the previous one.
                double recoveryWidthLimit = Math.Max(0.0, visibleItemsWidthLimit - TopOverflowRecoveryGraceWidth);
                double usedWidth = 0.0;
                List<NavigationViewItem> overflowItems = [];

                foreach (NavigationViewItem navItem in navItems)
                {
                    if (navItem.Visibility is not Visibility.Visible)
                    {
                        continue;
                    }

                    double itemWidth = GetTopItemWidth(navItem);
                    double itemWidthLimit = recoveringItems?.Contains(navItem) is true
                        ? recoveryWidthLimit
                        : visibleItemsWidthLimit;
                    if (usedWidth + itemWidth <= itemWidthLimit)
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

                if (overflowItems.Count is 0)
                {
                    HideTopOverflowChrome();
                    return;
                }

                SetTopOverflowButtonOffset(overflowOffset);
                _topOverflowButton.ContextMenu = SyncTopOverflowMenu(overflowItems);
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

        /// <summary>
        /// Collapses the overflow button, resets its offset, detaches its menu, and empties the
        /// reused menu. The single exit path for every pass that ends with nothing in overflow.
        /// </summary>
        private void HideTopOverflowChrome()
        {
            if (_topOverflowButton is not null)
            {
                _topOverflowButton.Visibility = Visibility.Collapsed;
                _topOverflowButton.ContextMenu = null;
                SetTopOverflowButtonOffset(0.0);
            }

            ClearTopOverflowMenu();
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

        /// <summary>
        /// Brings the reused overflow menu in line with <paramref name="overflowItems"/>. The menu and
        /// its entries are created on first overflow and then updated in place, so a resize that
        /// changes which items overflow does not allocate a fresh menu or re-wire its click handlers.
        /// </summary>
        /// <param name="overflowItems">The top-pane items currently hidden behind the overflow button.</param>
        /// <returns>The overflow menu, carrying one entry per hidden item in strip order.</returns>
        private ContextMenu SyncTopOverflowMenu(IReadOnlyList<NavigationViewItem> overflowItems)
        {
            _topOverflowMenu ??= new ContextMenu();
            ItemCollection menuItems = _topOverflowMenu.Items;

            while (menuItems.Count > overflowItems.Count)
            {
                int lastIndex = menuItems.Count - 1;
                if (menuItems[lastIndex] is MenuItem staleItem)
                {
                    staleItem.Click -= OnTopOverflowMenuItemClick;
                }

                menuItems.RemoveAt(lastIndex);
            }

            for (int index = 0; index < overflowItems.Count; index++)
            {
                NavigationViewItem navItem = overflowItems[index];
                MenuItem menuItem;
                if (index < menuItems.Count && menuItems[index] is MenuItem reusedItem)
                {
                    menuItem = reusedItem;
                }
                else
                {
                    menuItem = new()
                    {
                        MinWidth = 280,
                        MinHeight = 44,
                    };
                    menuItem.Click += OnTopOverflowMenuItemClick;
                    _ = menuItems.Add(menuItem);
                }

                menuItem.Header = GetOverflowItemText(navItem);
                SyncOverflowIcon(menuItem, navItem);
                menuItem.Tag = navItem;
            }

            return _topOverflowMenu;
        }

        /// <summary>
        /// Drops every entry from the reused overflow menu and unhooks its click handler. Entries
        /// hold their source <see cref="NavigationViewItem"/> in <see cref="FrameworkElement.Tag"/>,
        /// so a pass that ends with nothing in overflow has to empty the menu or it keeps pinning
        /// containers the pane no longer shows.
        /// </summary>
        private void ClearTopOverflowMenu()
        {
            if (_topOverflowMenu is null)
            {
                return;
            }

            ItemCollection menuItems = _topOverflowMenu.Items;
            foreach (object entry in menuItems)
            {
                if (entry is MenuItem staleItem)
                {
                    staleItem.Click -= OnTopOverflowMenuItemClick;
                    staleItem.Tag = null;
                }
            }

            menuItems.Clear();
        }

        /// <summary>
        /// Brings one menu entry's icon in line with its source item, reusing the icon already on
        /// the entry whenever it renders the same glyph. A resize pass runs for every few pixels of
        /// drag, so recreating an identical <see cref="FontIcon"/> each time would allocate an
        /// element (and a resource reference) per entry per pass for no visual change.
        /// </summary>
        /// <param name="menuItem">The overflow menu entry to update.</param>
        /// <param name="navItem">The top-pane item the entry stands in for.</param>
        private static void SyncOverflowIcon(MenuItem menuItem, NavigationViewItem navItem)
        {
            if (navItem.Icon is not FontIcon fontIcon)
            {
                menuItem.Icon = null;
                return;
            }

            if (menuItem.Icon is FontIcon currentIcon
                && string.Equals(currentIcon.Glyph, fontIcon.Glyph, StringComparison.Ordinal)
                && Equals(currentIcon.IconFontFamily, fontIcon.IconFontFamily)
                && currentIcon.MirroredWhenRightToLeft == fontIcon.MirroredWhenRightToLeft)
            {
                return;
            }

            FontIcon overflowIcon = new()
            {
                Glyph = fontIcon.Glyph,
                IconFontFamily = fontIcon.IconFontFamily,
                IconFontSize = 16.0,
                MirroredWhenRightToLeft = fontIcon.MirroredWhenRightToLeft,
            };
            overflowIcon.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");

            menuItem.Icon = overflowIcon;
        }

        private static string GetOverflowItemText(NavigationViewItem navItem)
        {
            string text = navItem.Content as string
                ?? navItem.Content?.ToString()
                ?? navItem.Tag as string
                ?? string.Empty;

            return string.IsNullOrWhiteSpace(text) ? navItem.GetType().Name : text;
        }

        /// <summary>
        /// Returns the natural width of a top-pane item, measuring it only when the cached width is
        /// missing. A zero measurement means the container is not realized yet and is never cached, so
        /// the next pass measures it again once it is.
        /// </summary>
        /// <param name="navItem">The top-pane item to size.</param>
        /// <returns>The item's natural width, or zero when it cannot be measured yet.</returns>
        private static double GetTopItemWidth(NavigationViewItem navItem)
        {
            double cachedWidth = (double)navItem.GetValue(TopOverflowItemWidthProperty);
            if (!double.IsNaN(cachedWidth))
            {
                return cachedWidth;
            }

            double measuredWidth = MeasureElementWidth(navItem);
            if (measuredWidth > 0.0)
            {
                navItem.SetValue(TopOverflowItemWidthProperty, measuredWidth);
            }

            return measuredWidth;
        }

        private static double MeasureElementWidth(FrameworkElement element)
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

        /// <summary>
        /// The single overflow menu, created on the first overflow and then reused. Rebuilding it per
        /// pass would allocate a fresh menu and re-wire its click handlers on every resize tick.
        /// </summary>
        private ContextMenu? _topOverflowMenu;

        private FluenceWindow? _titleBarExtensionWindow;

        private DependencyPropertyDescriptor? _titleBarExtensionDescriptor;

        private DependencyPropertyDescriptor? _titleBarContentDescriptor;

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
