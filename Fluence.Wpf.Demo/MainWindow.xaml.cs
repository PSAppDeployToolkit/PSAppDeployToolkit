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

using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// The main gallery window: a <c>FluenceWindow</c> with a <c>NavigationView</c> shell that
    /// drives all seventeen gallery pages. It owns the title-bar layout, the search box filtering,
    /// the back-navigation stack, and the page-creation switch.
    /// </summary>
    public partial class MainWindow : FluenceWindow
    {
        internal const string GalleryWindowTitle = "Fluence.Wpf \u2014 Control Gallery";

        // Fluent "page refresh" transition metrics: incoming content slides up 150 px while
        // fading in over 300 ms on the decelerating spline (Fluent motion-in-practice guidance
        // for top-level navigation page transitions).
        private const double PageRefreshMilliseconds = 300.0;
        private const double PageRefreshOffsetPixels = 150.0;

        // Maps each NavigationViewItem to its DemoNavigationItem metadata (title, route, keywords).
        private readonly Dictionary<NavigationViewItem, DemoNavigationItem> _navigationItemByContainer =
            [];

        // Lazily populated cache: once a page is created for a container it is reused on every
        // subsequent visit so the page state (expanded expanders, selected options) is preserved.
        private readonly Dictionary<NavigationViewItem, object> _pageByContainer =
            [];

        // Lightweight history stack used only to enable the title-bar back button; entries are
        // pushed on forward navigation and popped on back navigation.
        private readonly List<NavigationViewItem> _navigationBackStack =
            [];
        private bool _userShowIcon;
        private bool _userShowTitle;
        private ImageSource? _userIcon;
        private string _userTitle;
        private readonly DemoNavigationShellState _navigationState;
        private bool _isNavigatingBack;
        private bool _isUpdatingExtendedTitleOverlap;
        private NavigationViewItem? _currentNavigationItem;
        private System.Windows.Controls.Image? _titleBarIconView;
        private DependencyPropertyDescriptor? _extendsDpd;
        private DependencyPropertyDescriptor? _paneModeDpd;
        private DependencyPropertyDescriptor? _paneOpenDpd;
        private DependencyPropertyDescriptor? _backEnabledDpd;
        private DependencyPropertyDescriptor? _backVisibleDpd;
        private DependencyPropertyDescriptor? _paneToggleVisibleDpd;
        private object? _lastAnimatedPageContent;

        internal event EventHandler? DemoNavigationPaneStateChanged;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3366:\"this\" should not be exposed from constructors", Justification = "This is only demo code.")]
        public MainWindow()
        {
            InitializeComponent();

            Title = GalleryWindowTitle;
            SystemThemeWatcher.Watch(this);

            _userShowIcon = ShowIcon;
            _userShowTitle = ShowTitle;
            _userIcon = Icon;
            _userTitle = Title;
            _navigationState = new DemoNavigationShellState(DemoNav, ShellTitleBar);
            _navigationState.Changed += DemoNavigationState_Changed;

            DemoNav?.SelectionChanged += DemoNav_SelectionChanged;
            DemoNav?.ItemInvoked += DemoNav_ItemInvoked;

            PopulateNavigation();
            WatchTitleBarDependencies();
            ApplyTitleBarContentVisibility();
        }

        protected override void OnClosed(EventArgs e)
        {
            _extendsDpd?.RemoveValueChanged(this, OnTitleBarDependencyChanged);

            if (_paneModeDpd is not null && DemoNav is not null)
            {
                _paneModeDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_paneOpenDpd is not null && DemoNav is not null)
            {
                _paneOpenDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_backEnabledDpd is not null && DemoNav is not null)
            {
                _backEnabledDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_backVisibleDpd is not null && DemoNav is not null)
            {
                _backVisibleDpd.RemoveValueChanged(DemoNav, OnNavigationBackVisibilityChanged);
            }

            if (_paneToggleVisibleDpd is not null && DemoNav is not null)
            {
                _paneToggleVisibleDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            DemoNav?.SelectionChanged -= DemoNav_SelectionChanged;
            DemoNav?.ItemInvoked -= DemoNav_ItemInvoked;
            _navigationState.Changed -= DemoNavigationState_Changed;

            base.OnClosed(e);
        }

        private void PopulateNavigation()
        {
            if (DemoNav is null)
            {
                return;
            }

            DemoNav.Items.Clear();
            _navigationItemByContainer.Clear();
            _pageByContainer.Clear();
            _navigationBackStack.Clear();
            _currentNavigationItem = null;

            NavigationViewItem? defaultItem = null;
            foreach (DemoNavigationItem item in DemoNavigationCatalog.Items)
            {
                NavigationViewItem navItem = CreateNavigationItem(item);
                _ = DemoNav.Items.Add(navItem);
                _navigationItemByContainer[navItem] = item;
                if (item.IsDefault)
                {
                    defaultItem = navItem;
                }
            }

            // Settings is a FooterMenuItems entry (authored in XAML). Register its metadata so it
            // routes through the same EnsurePageContent / CreatePageForRoute path as catalog items.
            if (SettingsNavigationItem is NavigationViewItem settingsNavigationItem)
            {
                _navigationItemByContainer[settingsNavigationItem] =
                    new DemoNavigationItem("Settings", "settings", "settings options preferences", "", isDefault: false);
            }

            if (defaultItem is null && DemoNav.Items.Count > 0)
            {
                defaultItem = DemoNav.Items[0] as NavigationViewItem;
            }

            NavigateToItem(defaultItem);
            UpdateBackNavigationState();
        }

        private static NavigationViewItem CreateNavigationItem(DemoNavigationItem item)
        {
            return new NavigationViewItem
            {
                Content = item.Title,
                Tag = item.Route + " " + item.Keywords,
                Icon = new FontIcon { Glyph = item.Glyph, IconFontSize = 16 },
            };
        }

        private void DemoNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DemoNav.SelectedItem is not NavigationViewItem selected)
            {
                return;
            }

            if (!ReferenceEquals(_currentNavigationItem, selected))
            {
                if (!_isNavigatingBack && _currentNavigationItem is not null)
                {
                    _navigationBackStack.Add(_currentNavigationItem);
                }

                _currentNavigationItem = selected;
            }

            object? page = EnsurePageContent(selected);
            if (page is not null)
            {
                DemoNav.Content = page;
                AnimatePageInIfChanged(page);
            }

            UpdateBackNavigationState();
        }

        /// <summary>
        /// Selects the pane item whose title, route, or keywords contain the supplied tag.
        /// </summary>
        /// <param name="tag">Search tag such as "buttons", "progress ring", or "settings".</param>
        public void NavigateTo(string tag)
        {
            if (DemoNav is null || string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (NavSearchBox is not null && !string.IsNullOrWhiteSpace(NavSearchBox.Text))
            {
                NavSearchBox.Text = string.Empty;
                NavSearchBox.ItemsSource = null;

                // The programmatic clear above raises TextChanged with ProgrammaticChange, which
                // the handler ignores, so restore the pane filter explicitly.
                ApplyNavSearchFilter();
            }

            if (string.Equals(tag.Trim(), "settings", StringComparison.OrdinalIgnoreCase))
            {
                if (SettingsNavigationItem is NavigationViewItem settingsItem)
                {
                    DemoNav.SelectFooterMenuItem(settingsItem);
                }
                return;
            }

            NavigateToItem(FindFirstMatchingItem(tag));
        }

        private void NavigateToItem(NavigationViewItem? item)
        {
            if (item is null || DemoNav is null)
            {
                return;
            }

            // Footer entries (Settings) are not in DemoNav.Items; route them through the control's
            // footer selection so they raise ItemInvoked and show the footer selection indicator.
            if (ReferenceEquals(item, SettingsNavigationItem))
            {
                DemoNav.SelectFooterMenuItem(item);
                return;
            }

            if (ReferenceEquals(DemoNav.SelectedItem, item) && EnsurePageContent(item) is object page)
            {
                _currentNavigationItem ??= item;
                DemoNav.Content = page;
                AnimatePageInIfChanged(page);
                UpdateBackNavigationState();
            }
            else
            {
                DemoNav.SelectedItem = item;
            }
        }

        /// <summary>
        /// Navigates when a footer item (Settings) is invoked. Main-menu items navigate through
        /// <see cref="DemoNav_SelectionChanged"/>; footer items clear the main selection, so they are
        /// handled here instead.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void DemoNav_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
        {
            if (DemoNav is null || !ReferenceEquals(e.InvokedItemContainer, SettingsNavigationItem))
            {
                return;
            }

            object? page = EnsurePageContent(SettingsNavigationItem);
            if (page is null)
            {
                return;
            }

            if (!ReferenceEquals(DemoNav.Content, page) && !_isNavigatingBack && _currentNavigationItem is not null)
            {
                _navigationBackStack.Add(_currentNavigationItem);
            }

            _currentNavigationItem = SettingsNavigationItem;
            DemoNav.Content = page;
            AnimatePageInIfChanged(page);
            UpdateBackNavigationState();
        }

        private void UpdateBackNavigationState()
        {
            _navigationState.SetBackEnabled(_navigationBackStack.Count > 0);
            ApplyTitleBarContentVisibility();
        }

        private object? EnsurePageContent(NavigationViewItem item)
        {
            if (item is null)
            {
                return null;
            }

            if (_pageByContainer.TryGetValue(item, out object? page))
            {
                return page;
            }

            if (!_navigationItemByContainer.TryGetValue(item, out DemoNavigationItem? metadata))
            {
                return null;
            }

            page = CreatePageForRoute(metadata.Route);
            _pageByContainer[item] = page;
            return page;
        }

        private object CreatePageForRoute(string route)
        {
            return (route ?? string.Empty).ToLowerInvariant() switch
            {
                "home" => new GalleryHomePage(),
                "colors" => new GalleryColorsPage(),
                "icons" => new GalleryIconsPage(),
                "typography" => new GalleryTypographyPage(),
                "accessibility" => new GalleryAccessibilityPage(),
                "buttons" => new GalleryButtonsPage(),
                "selection" => new GallerySelectionPage(),
                "inputs" => new GalleryInputsPage(),
                "forms" => new GalleryFormsPage(),
                "data" => new GalleryDataPage(),
                "data binding" => new GalleryDataBindingPage(),
                "trees" => new GalleryTreesPage(),
                "menus" => new GalleryMenusPage(),
                "navigation" => new GalleryNavigationPage(),
                "tabs" => new GalleryTabsPage(),
                "layout" => new GalleryLayoutPage(),
                "status" => new GalleryStatusPage(),
                "settings" => new GallerySettingsPage(this),
                _ => new GalleryHomePage(),
            };
        }

        private void NavSearchBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            // Only user typing refilters and rebuilds suggestions; suggestion previews while
            // cycling with the arrow keys and programmatic clears must not reopen the popup.
            if (e.Reason is not AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            ApplyNavSearchFilter();
            UpdateNavSearchSuggestions();
        }

        private void NavSearchBox_QuerySubmitted(object sender, AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            if (e.ChosenSuggestion is DemoNavigationItem chosen)
            {
                NavigateTo(chosen.Route);
                return;
            }

            string query = (e.QueryText ?? string.Empty).Trim();
            if (query.Length is 0)
            {
                return;
            }

            NavigateToItem(FindFirstMatchingItem(query));
        }

        private void UpdateNavSearchSuggestions()
        {
            if (DemoNav is null || NavSearchBox is null)
            {
                return;
            }

            string query = (NavSearchBox.Text ?? string.Empty).Trim();
            if (query.Length is 0)
            {
                NavSearchBox.ItemsSource = null;
                return;
            }

            List<DemoNavigationItem> matches = [];
            foreach (object obj in DemoNav.Items)
            {
                if (obj is not NavigationViewItem item)
                {
                    continue;
                }

                if (_navigationItemByContainer.TryGetValue(item, out DemoNavigationItem? metadata) &&
                    ItemMatches(item, metadata, query))
                {
                    matches.Add(metadata);
                }
            }

            // Settings lives in FooterMenuItems, not DemoNav.Items; include it so the footer
            // page is reachable from the suggestion list too (WinUI Gallery does the same).
            if (SettingsNavigationItem is NavigationViewItem settingsItem &&
                _navigationItemByContainer.TryGetValue(settingsItem, out DemoNavigationItem? settingsMetadata) &&
                ItemMatches(settingsItem, settingsMetadata, query))
            {
                matches.Add(settingsMetadata);
            }

            NavSearchBox.ItemsSource = matches.Count is 0 ? null : matches;
        }

        private NavigationViewItem? FindFirstMatchingItem(string query)
        {
            if (DemoNav is null || string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string trimmed = query.Trim();
            foreach (object obj in DemoNav.Items)
            {
                if (obj is not NavigationViewItem item)
                {
                    continue;
                }

                string title = (item.Content as string) ?? string.Empty;
                _ = _navigationItemByContainer.TryGetValue(item, out DemoNavigationItem? metadata);
                if (string.Equals(title, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    (metadata is not null && string.Equals(metadata.Route, trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    return item;
                }

                if (ItemMatches(item, metadata, trimmed))
                {
                    return item;
                }
            }

            return null;
        }

        private static bool ItemMatches(NavigationViewItem item, DemoNavigationItem? metadata, string needle)
        {
            string title = (item.Content as string) ?? string.Empty;
            string tag = (item.Tag as string) ?? string.Empty;
            string route = metadata is null ? string.Empty : metadata.Route;
            string keywords = metadata is null ? string.Empty : metadata.Keywords;
            return ContainsOrdinalIgnoreCase(title + " " + tag + " " + route + " " + keywords, needle);
        }

        private static bool ContainsOrdinalIgnoreCase(string value, string needle)
        {
            return value.Contains(needle, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyNavSearchFilter()
        {
            if (DemoNav is null || NavSearchBox is null)
            {
                return;
            }

            string query = (NavSearchBox.Text ?? string.Empty).Trim();
            if (query.Length is 0)
            {
                foreach (object obj in DemoNav.Items)
                {
                    if (obj is NavigationViewItem item)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }

                return;
            }

            foreach (object obj in DemoNav.Items)
            {
                if (obj is not NavigationViewItem item)
                {
                    continue;
                }

                _ = _navigationItemByContainer.TryGetValue(item, out DemoNavigationItem? metadata);
                item.Visibility = ItemMatches(item, metadata, query)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void AnimatePageInIfChanged(object page)
        {
            if (page is null || ReferenceEquals(_lastAnimatedPageContent, page))
            {
                return;
            }

            _lastAnimatedPageContent = page;
            AnimatePageIn(page);
        }

        /// <summary>
        /// Plays the Fluent "page refresh" transition on the incoming page: the content slides
        /// up <see cref="PageRefreshOffsetPixels"/> while fading 0 to 1 over
        /// <see cref="PageRefreshMilliseconds"/> on the 0.8,0,0,1 decelerating spline (the
        /// Typography.xaml ControlFastOutSlowInKeySpline motion token, mirrored by value).
        /// Top-level navigation switches use page refresh per the Fluent page-transition
        /// guidance (slide up + fade in, 300 ms, decelerate). The start pose is seeded as a
        /// local value before the clocks start: <c>BeginAnimation</c> only takes effect on the
        /// next animation tick, so an unseeded start lets the frame composited right after the
        /// content swap render the page at its rest pose (opaque, unshifted) and the following
        /// frame snap it to the transition start - a visible flash on every navigation. The
        /// clocks hold their end value (<see cref="FillBehavior.HoldEnd"/>) and the Completed
        /// handlers stamp the rest pose and release the clocks in one dispatcher callback, so
        /// the seeded local value can never show through at either end of the transition.
        /// </summary>
        /// <param name="page">The page instance that just became the navigation content.</param>
        private static void AnimatePageIn(object page)
        {
            if (page is not FrameworkElement element)
            {
                return;
            }

            if (element.RenderTransform is not TranslateTransform translate)
            {
                translate = new TranslateTransform();
                element.RenderTransform = translate;
            }

            // Motion disabled (OS "Show animations" off, or software rendering where every
            // animated frame is a CPU composite): land at rest immediately, mirroring the
            // library-internal MotionHelper gate the demo assembly cannot reference. The rest
            // pose is stamped explicitly because an interrupted earlier transition can leave
            // its seeded start value behind as the local value.
            if (!SystemParameters.ClientAreaAnimation || (RenderCapability.Tier >> 16) <= 0)
            {
                translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                element.BeginAnimation(OpacityProperty, animation: null);
                translate.Y = 0.0;
                element.Opacity = 1.0;
                return;
            }

            // Seed the start pose so the pre-tick frame already shows it (see summary).
            translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
            element.BeginAnimation(OpacityProperty, animation: null);
            translate.Y = PageRefreshOffsetPixels;
            element.Opacity = 0.0;

            DoubleAnimationUsingKeyFrames translateAnimation = CreatePageRefreshAnimation(PageRefreshOffsetPixels, 0.0);
            translateAnimation.Completed += delegate
            {
                translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                translate.Y = 0.0;
            };

            DoubleAnimationUsingKeyFrames opacityAnimation = CreatePageRefreshAnimation(0.0, 1.0);
            opacityAnimation.Completed += delegate
            {
                element.BeginAnimation(OpacityProperty, animation: null);
                element.Opacity = 1.0;
            };

            translate.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
            element.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        /// <summary>
        /// Builds one track of the page refresh: a discrete start at time zero settling at the
        /// rest value over the page-refresh duration on the decelerating Fluent key spline
        /// (see <see cref="AnimatePageIn"/> for the mirrored motion tokens). The clock holds
        /// its end value; the caller's Completed handler stamps the rest pose and releases it.
        /// </summary>
        /// <param name="from">The discrete start value.</param>
        /// <param name="to">The rest value reached when the transition settles.</param>
        /// <returns>The keyframe animation for the track.</returns>
        private static DoubleAnimationUsingKeyFrames CreatePageRefreshAnimation(double from, double to)
        {
            return new DoubleAnimationUsingKeyFrames
            {
                FillBehavior = FillBehavior.HoldEnd,
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new SplineDoubleKeyFrame(
                        to,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(PageRefreshMilliseconds)),
                        new KeySpline(0.8, 0.0, 0.0, 1.0)),
                },
            };
        }

        /// <summary>
        /// Records the user's intended title-bar icon visibility before layout rules are applied.
        /// </summary>
        /// <param name="show">Whether the icon should be visible when layout permits it.</param>
        /// <param name="icon">The icon to apply when visible.</param>
        public void SetUserShowIcon(bool show, ImageSource? icon)
        {
            _userShowIcon = show;
            _userIcon = icon;
            ApplyTitleBarContentVisibility();
        }

        internal NavigationViewPaneDisplayMode GetDemoNavigationPaneDisplayMode()
        {
            return _navigationState.PaneDisplayMode;
        }

        internal bool IsDemoNavigationPaneOpen()
        {
            return _navigationState.IsPaneOpen;
        }

        internal void SetDemoNavigationPaneDisplayMode(NavigationViewPaneDisplayMode mode)
        {
            _navigationState.SetPaneDisplayMode(mode);
            ApplyTitleBarContentVisibility();
        }

        /// <summary>
        /// Records the user's intended title-bar title visibility before layout rules are applied.
        /// </summary>
        /// <param name="show">Whether the title should be visible when layout permits it.</param>
        /// <param name="title">The title text to apply when visible.</param>
        public void SetUserShowTitle(bool show, string title)
        {
            _userShowTitle = show;
            _userTitle = title;
            ApplyTitleBarContentVisibility();
        }

        private void WatchTitleBarDependencies()
        {
            _extendsDpd = DependencyPropertyDescriptor.FromProperty(
                ExtendsContentIntoTitleBarProperty, typeof(FluenceWindow));
            _extendsDpd?.AddValueChanged(this, OnTitleBarDependencyChanged);

            if (DemoNav is not null)
            {
                _paneModeDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.PaneDisplayModeProperty, typeof(NavigationView));
                _paneModeDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _paneOpenDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsPaneOpenProperty, typeof(NavigationView));
                _paneOpenDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _backEnabledDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackEnabledProperty, typeof(NavigationView));
                _backEnabledDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _backVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackButtonVisibleProperty, typeof(NavigationView));
                _backVisibleDpd?.AddValueChanged(DemoNav, OnNavigationBackVisibilityChanged);

                _paneToggleVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsPaneToggleButtonVisibleProperty, typeof(NavigationView));
                _paneToggleVisibleDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }
        }

        private void OnTitleBarDependencyChanged(object? sender, EventArgs e)
        {
            _navigationState.CaptureNavigationStateFromControl(ExtendsContentIntoTitleBar);
            ApplyTitleBarContentVisibility();
        }

        private void OnNavigationBackVisibilityChanged(object? sender, EventArgs e)
        {
            _navigationState.CaptureBackVisibilityFromControl();
            ApplyTitleBarContentVisibility();
        }

        private void ApplyTitleBarContentVisibility()
        {
            bool extendedTitleBar = ExtendsContentIntoTitleBar;
            bool shellTitleBarPresent = ShellTitleBar is not null;

            ShowIcon = !shellTitleBarPresent && !extendedTitleBar && _userShowIcon;
            ShowTitle = !shellTitleBarPresent && !extendedTitleBar && _userShowTitle;
            Icon = _userIcon;
            if (_userShowTitle && !string.IsNullOrWhiteSpace(_userTitle))
            {
                Title = _userTitle;
            }

            _ = NavSearchBox?.Visibility = Visibility.Visible;

            if (ShellTitleBar is not null)
            {
                ShellTitleBar.Title = _userShowTitle ? (_userTitle ?? string.Empty) : string.Empty;
                if (_userShowIcon && _userIcon is not null)
                {
                    ShellTitleBar.Icon = GetTitleBarIconView();
                }
                else
                {
                    ShellTitleBar.ClearValue(Controls.TitleBar.IconProperty);
                }
            }

            _navigationState.ApplyChrome(extendedTitleBar, shellTitleBarPresent);
            ScheduleExtendedTitleOverlapCheck();
        }

        private void DemoNavigationState_Changed(object? sender, EventArgs e)
        {
            DemoNavigationPaneStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TitleBarLayout_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleExtendedTitleOverlapCheck();
        }

        private System.Windows.Controls.Image GetTitleBarIconView()
        {
            if (_titleBarIconView is null)
            {
                _titleBarIconView = new System.Windows.Controls.Image
                {
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                RenderOptions.SetBitmapScalingMode(_titleBarIconView, BitmapScalingMode.HighQuality);
            }

            _titleBarIconView.Source = _userIcon;
            return _titleBarIconView;
        }

        private void ShellTitleBar_PaneToggleRequested(object sender, EventArgs e)
        {
            _navigationState.ToggleLeftPane();
            ApplyTitleBarContentVisibility();
        }

        private void ShellTitleBar_BackRequested(object sender, EventArgs e)
        {
            if (DemoNav is null || _navigationBackStack.Count is 0)
            {
                UpdateBackNavigationState();
                return;
            }

            NavigationViewItem previousItem = _navigationBackStack[^1];
            _navigationBackStack.RemoveAt(_navigationBackStack.Count - 1);

            _isNavigatingBack = true;
            try
            {
                NavigateToItem(previousItem);
            }
            finally
            {
                _isNavigatingBack = false;
                UpdateBackNavigationState();
            }
        }

        private void ScheduleExtendedTitleOverlapCheck()
        {
            _ = Dispatcher.BeginInvoke(new Action(UpdateExtendedTitleOverlap), DispatcherPriority.Loaded);
        }

        // Hide the title text when the centered search box would overlap it; runs after every
        // layout pass that could change the title-bar geometry.
        private void UpdateExtendedTitleOverlap()
        {
            if (_isUpdatingExtendedTitleOverlap)
            {
                return;
            }

            _isUpdatingExtendedTitleOverlap = true;
            try
            {
                if (!ExtendsContentIntoTitleBar || ShellTitleBar is null)
                {
                    return;
                }

                string desiredTitle = _userShowTitle ? (_userTitle ?? string.Empty) : string.Empty;
                if (string.IsNullOrWhiteSpace(desiredTitle))
                {
                    ShellTitleBar.Title = string.Empty;
                    return;
                }

                if (!string.Equals(ShellTitleBar.Title, desiredTitle, StringComparison.Ordinal))
                {
                    ShellTitleBar.Title = desiredTitle;
                    _ = ShellTitleBar.ApplyTemplate();
                    ShellTitleBar.UpdateLayout();
                    NavSearchBox?.UpdateLayout();
                }

                System.Windows.Controls.TextBlock? titleText = GetTitleBarTemplatePart<System.Windows.Controls.TextBlock>("PART_TitleText");
                if (titleText is null
                    || NavSearchBox is null
                    || titleText.Visibility is not Visibility.Visible
                    || NavSearchBox.Visibility is not Visibility.Visible
                    || !titleText.IsVisible
                    || !NavSearchBox.IsVisible)
                {
                    return;
                }

                if (!TryGetVisualX(titleText, this, out double titleLeft)
                    || !TryGetVisualX(NavSearchBox, this, out double searchLeft))
                {
                    return;
                }

                // If the app icon itself already overlaps the search box there is no room for the
                // title at all; clear it and bail early rather than attempting to constrain width.
                ContentPresenter? titleIcon = GetTitleBarTemplatePart<ContentPresenter>("PART_IconPresenter");
                if (titleIcon?.Visibility is Visibility.Visible
                    && titleIcon.IsVisible
                    && TryGetVisualX(titleIcon, this, out double titleIconLeft)
                    && titleIconLeft + titleIcon.ActualWidth > searchLeft - 12.0)
                {
                    titleText.ClearValue(MaxWidthProperty);
                    ShellTitleBar.Title = string.Empty;
                    return;
                }

                double availableTitleWidth = searchLeft - titleLeft - 12.0;
                if (availableTitleWidth < 48.0)
                {
                    titleText.ClearValue(MaxWidthProperty);
                    ShellTitleBar.Title = string.Empty;
                    return;
                }

                // Constrain the title width so it cannot bleed into the search box, then check
                // whether even the truncated text still collides (can happen with very long titles
                // and narrow windows).
                titleText.MaxWidth = availableTitleWidth;
                titleText.UpdateLayout();
                if (!TryGetVisualX(titleText, this, out titleLeft))
                {
                    return;
                }

                double titleRight = titleLeft + titleText.ActualWidth;
                if (titleRight > searchLeft - 11.0)
                {
                    ShellTitleBar.Title = string.Empty;
                }
            }
            finally
            {
                _isUpdatingExtendedTitleOverlap = false;
            }
        }

        // Returns the element's left-edge X position relative to the given ancestor; guards
        // TransformToAncestor with an ancestry check because the method throws if called on
        // an element that is not in the ancestor's visual subtree.
        private static bool TryGetVisualX(FrameworkElement element, Visual ancestor, out double x)
        {
            x = 0.0;
            if (!IsVisualAncestorOf(ancestor, element))
            {
                return false;
            }

            GeneralTransform transform = element.TransformToAncestor(ancestor);
            x = transform.Transform(new Point(0, 0)).X;
            return true;
        }

        // Walks the visual-parent chain rather than calling VisualTreeHelper.IsAncestorOf so that
        // non-Visual nodes (e.g. ContentPresenter inside a DataTemplate) do not break the walk.
        private static bool IsVisualAncestorOf(Visual ancestor, DependencyObject descendant)
        {
            DependencyObject? current = descendant;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                if (current is not Visual and not System.Windows.Media.Media3D.Visual3D)
                {
                    return false;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static T? FindVisualChildByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            if (root is null)
            {
                return null;
            }

            if (root is T current && string.Equals(current.Name, name, StringComparison.Ordinal))
            {
                return current;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                T? match = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }

        private T? GetTitleBarTemplatePart<T>(string partName)
            where T : FrameworkElement
        {
            if (ShellTitleBar is null)
            {
                return null;
            }

            _ = ShellTitleBar.ApplyTemplate();
            return ShellTitleBar.Template?.FindName(partName, ShellTitleBar) as T;
        }

    }
}
