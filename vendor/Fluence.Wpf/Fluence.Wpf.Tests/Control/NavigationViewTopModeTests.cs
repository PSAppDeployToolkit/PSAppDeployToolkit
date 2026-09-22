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

using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="NavigationView"/> control in Top pane
    /// display mode: FluenceWindow title-bar coercion, pane-open/toggle coercion, item layout
    /// without a pane ScrollViewer, the overflow menu (invocation, item recovery, reflow, and
    /// the exact-fit boundary grace), and the Top footer's icon-only rendering and indicator.
    /// </summary>
    public sealed class NavigationViewTopModeTests : IClassFixture<LightThemeFixture>
    {
        public NavigationViewTopModeTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task NavigationView_TopFooterIndicator_CentersUnderFooterItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = NavigationViewTests.CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Top, isPaneOpen: true);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectFooterMenuItem(footer);
                    _ = await WaitUntilAsync(window.Dispatcher, 600, () => (nav.GetFooterSelectionIndicatorForTesting()?.Opacity ?? 0.0) >= 0.9).ConfigureAwait(true);
                    window.UpdateLayout();

                    FrameworkElement footerIndicator = nav.GetFooterSelectionIndicatorForTesting()
                        ?? throw new Xunit.Sdk.XunitException("PART_FooterSelectionIndicator should exist in the Top pane template.");

                    // The pre-fix bug: the indicator's coordinate host was a zero-size Canvas that was
                    // not an ancestor of the footer item, so the transform failed and the indicator
                    // snapped to the left edge of the footer region. Compare the indicator's rendered
                    // center to the footer item's center in a shared ancestor (nav) to confirm it now
                    // sits under the gear regardless of which element is the internal host.
                    double indicatorCenterX = footerIndicator
                        .TransformToAncestor(nav)
                        .Transform(new Point(footerIndicator.Width / 2.0, footerIndicator.Height / 2.0)).X;
                    double itemCenterX = footer
                        .TransformToAncestor(nav)
                        .Transform(new Point(footer.ActualWidth / 2.0, footer.ActualHeight / 2.0)).X;

                    Assert.Equal(itemCenterX, indicatorCenterX, 1.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopFooterItem_RendersIconOnlyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                try
                {
                    window.Content = NavigationViewTests.CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Top, isPaneOpen: true);
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter label = FindVisualChildByName<ContentPresenter>(footer, "ContentPresenter")
                        ?? throw new Xunit.Sdk.XunitException("Footer item template should expose the label content presenter.");
                    ContentPresenter icon = FindVisualChildByName<ContentPresenter>(footer, "IconPresenter")
                        ?? throw new Xunit.Sdk.XunitException("Footer item template should expose the icon presenter.");

                    Assert.False(label.IsVisible,
                        "In Top mode a footer item (e.g. Settings) must render gear-only: its label content presenter should be collapsed.");
                    Assert.True(icon.IsVisible,
                        "In Top mode a footer item must still show its icon.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopFooterItem_KeepsLabel_InLeftAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                try
                {
                    window.Content = NavigationViewTests.CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter label = FindVisualChildByName<ContentPresenter>(footer, "ContentPresenter")
                        ?? throw new Xunit.Sdk.XunitException("Footer item template should expose the label content presenter.");

                    Assert.True(label.IsVisible,
                        "The gear-only rule is scoped to Top mode; an open Left pane footer item must keep its label.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMainItem_KeepsLabelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = NavigationViewTests.CreateNavWithFooterItem(out _, NavigationViewPaneDisplayMode.Top, isPaneOpen: true);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    NavigationViewItem mainItem = (NavigationViewItem)nav.Items[0]!;
                    ContentPresenter label = FindVisualChildByName<ContentPresenter>(mainItem, "ContentPresenter")
                        ?? throw new Xunit.Sdk.XunitException("Main item template should expose the label content presenter.");

                    Assert.True(label.IsVisible,
                        "Top-level (non-footer) items must keep their labels in Top mode; only footer items collapse to icon-only.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopFooterIndicator_AnimatesOnSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = NavigationViewTests.CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Top, isPaneOpen: true);
                    nav.SelectedIndex = 0;
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement footerIndicator = nav.GetFooterSelectionIndicatorForTesting()
                        ?? throw new Xunit.Sdk.XunitException("PART_FooterSelectionIndicator should exist in the Top pane template.");

                    // Selecting the footer item should fade/scale the indicator in (animate), not snap.
                    nav.SelectFooterMenuItem(footer);
                    WpfTestSta.DrainDispatcher(window.Dispatcher); // runs the queued RefreshIndicators that starts the animation
                    Assert.True(footerIndicator.HasAnimatedProperties,
                        "Selecting a footer item in Top mode should animate the indicator in, not snap it to full opacity.");

                    bool shown = await WaitUntilAsync(window.Dispatcher, 600, () => footerIndicator.Opacity >= 0.9).ConfigureAwait(true);
                    Assert.True(shown, "The footer indicator should reach full opacity after the fade-in completes.");

                    // Navigating away (exiting Settings) should animate the indicator back out.
                    nav.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(footerIndicator.HasAnimatedProperties,
                        "Leaving the footer item should animate the indicator out, not hide it instantly.");

                    bool hidden = await WaitUntilAsync(window.Dispatcher, 600, () => footerIndicator.Opacity <= 0.1).ConfigureAwait(true);
                    Assert.True(hidden, "The footer indicator should fade to hidden after the footer item is deselected.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_InFluenceWindow_LeftAndTopCoerceTitleBarExtensionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FluenceWindow window = new()
                {
                    Width = 640,
                    Height = 420,
                    ExtendsContentIntoTitleBar = false,
                };

                try
                {
                    NavigationView nav = new()
                    {
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(window.ExtendsContentIntoTitleBar,
                        "Left NavigationView pane mode should extend FluenceWindow content into the title bar.");

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(window.ExtendsContentIntoTitleBar,
                        "Top NavigationView pane mode should disable FluenceWindow content extension into the title bar.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_CoercesPaneOpenAndToggleHiddenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 520,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        IsPaneOpen = false,
                        IsPaneToggleButtonVisible = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(nav.IsPaneOpen, "Top mode should always report IsPaneOpen=True.");
                    Assert.False(nav.IsPaneToggleButtonVisible,
                        "Top mode should always report IsPaneToggleButtonVisible=False.");

                    nav.IsPaneOpen = false;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(nav.IsPaneOpen, "Top mode should coerce runtime IsPaneOpen changes back to true.");
                    Assert.False(nav.IsPaneToggleButtonVisible,
                        "Top mode should coerce runtime IsPaneToggleButtonVisible changes back to false.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_KeepsItemIconAndTextVisibleWithoutScrollViewerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 640,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    NavigationViewItem item = new()
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F" },
                    };
                    NavigationViewItem second = new()
                    {
                        Content = "Design",
                        Icon = new FontIcon { Glyph = "\uE790" },
                    };
                    _ = nav.Items.Add(item);
                    _ = nav.Items.Add(second);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ScrollViewer? topScrollViewer = FindVisualChildByName<ScrollViewer>(nav, NavigationView.PART_PaneItemsScrollViewer);
                    Assert.Null(topScrollViewer);

                    ContentPresenter iconPresenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "IconPresenter"), exactMatch: false);
                    ContentPresenter contentPresenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "ContentPresenter"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, iconPresenter.Visibility);
                    Assert.Equal(Visibility.Visible, contentPresenter.Visibility);
                    Assert.Equal(14.0, item.FontSize, 0.01);
                    FontIcon itemIcon = Assert.IsType<FontIcon>(item.Icon);
                    Assert.Equal(16.0, itemIcon.IconFontSize, 0.01);
                    Assert.Equal(new Thickness(4, 0, 2, 0), iconPresenter.Margin);
                    Assert.Equal(new Thickness(2, 0, 2, 0), contentPresenter.Margin);
                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(item, "OuterBorder"), exactMatch: false);
                    ContentPresenter infoBadgePresenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter"), exactMatch: false);
                    Assert.Equal(new Thickness(2, 4, 2, 4), outerBorder.Margin);
                    Assert.Equal(new Thickness(4, 0, 6, 0), outerBorder.Padding);
                    Assert.Equal(Visibility.Collapsed, infoBadgePresenter.Visibility);

                    ColumnDefinition iconColumn = Assert.IsType<ColumnDefinition>(item.Template.FindName("IconColumn", item));
                    ColumnDefinition gapColumn = Assert.IsType<ColumnDefinition>(item.Template.FindName("GapColumn", item));
                    ColumnDefinition contentColumn = Assert.IsType<ColumnDefinition>(item.Template.FindName("ContentColumn", item));
                    Assert.Equal(GridUnitType.Auto, iconColumn.Width.GridUnitType);
                    Assert.Equal(0.0, gapColumn.Width.Value, 0.01);
                    Assert.Equal(GridUnitType.Auto, contentColumn.Width.GridUnitType);

                    ContentPresenter secondIconPresenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(second, "IconPresenter"), exactMatch: false);
                    double textToNextIconGap = GetNavigationElementX(secondIconPresenter, nav) - GetNavigationElementRight(contentPresenter, nav);
                    Assert.Equal(24.0, textToNextIconGap, 1.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_OverflowMenuInvokesHiddenItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 300,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        PaneFooter = new System.Windows.Controls.StackPanel { Width = 88, Height = 36 },
                    };
                    NavigationViewItem first = new() { Content = "Home", Icon = new FontIcon { Glyph = "\uE80F" } };
                    NavigationViewItem last = new() { Content = "Diagnostics", Icon = new FontIcon { Glyph = "\uE8A7", IconFontSize = 20 } };
                    _ = nav.Items.Add(first);
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Design", Icon = new FontIcon { Glyph = "\uE790" } });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Controls", Icon = new FontIcon { Glyph = "\uECAA" } });
                    _ = nav.Items.Add(last);

                    object? invokedItem = null;
                    nav.ItemInvoked += (_, e) => invokedItem = e.InvokedItem;

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.Button overflowButton = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton"), exactMatch: false);
                    Assert.Equal(ControlAppearance.Subtle, overflowButton.Appearance);
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    Grid topItemsHost = Assert.IsType<Grid>(FindVisualChildByName<Grid>(nav, NavigationView.PART_TopItemsHost), exactMatch: false);
                    double visibleItemsRight = double.MinValue;
                    foreach (object item in nav.Items)
                    {
                        if (item is NavigationViewItem navItem && navItem.Visibility is Visibility.Visible)
                        {
                            double itemRight = GetNavigationElementRight(navItem, nav);
                            if (itemRight > visibleItemsRight)
                            {
                                visibleItemsRight = itemRight;
                            }
                        }
                    }

                    double overflowButtonGap = GetNavigationElementX(overflowButton, nav) - visibleItemsRight;
                    Assert.Equal(4.0, overflowButtonGap, 1.5);
                    System.Windows.Controls.StackPanel footer = Assert.IsType<System.Windows.Controls.StackPanel>(nav.PaneFooter);
                    Assert.True(GetNavigationElementRight(overflowButton, nav) <= GetNavigationElementX(footer, nav) + 0.5,
                        "Top pane overflow button should appear before the right-docked PaneFooter instead of docking to the strip edge.");
                    System.Windows.Controls.ContextMenu overflowButtonContextMenu = Assert.IsType<System.Windows.Controls.ContextMenu>(overflowButton.ContextMenu, exactMatch: false);
                    Assert.True(overflowButtonContextMenu.Items.Count > 0,
                        "Top pane overflow menu should contain hidden navigation items.");

                    Controls.MenuItem overflowItem = Assert.IsType<Controls.MenuItem>(overflowButtonContextMenu.Items[^1]);
                    Assert.Equal(280.0, overflowItem.MinWidth, 0.01);
                    Assert.Equal(44.0, overflowItem.MinHeight, 0.01);
                    FontIcon overflowIcon = Assert.IsType<FontIcon>(overflowItem.Icon);
                    Assert.Equal(16.0, overflowIcon.IconFontSize, 0.01);
                    Assert.Equal("Diagnostics", overflowItem.Header);
                    overflowItem.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Same(last, invokedItem);
                    Assert.Same(last, nav.SelectedItem);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_ReservesOverflowButtonByMovingLastFittingItemToMenuAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 220,
                        Height = 240,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    NavigationViewItem first = new() { Content = "One", Icon = new FontIcon { Glyph = "\uE80F" } };
                    NavigationViewItem second = new() { Content = "Two", Icon = new FontIcon { Glyph = "\uE790" } };
                    NavigationViewItem third = new() { Content = "Three", Icon = new FontIcon { Glyph = "\uE8A7" } };
                    _ = nav.Items.Add(first);
                    _ = nav.Items.Add(second);
                    _ = nav.Items.Add(third);

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.Button overflowButton = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    Assert.Equal(Visibility.Visible, first.Visibility);
                    Assert.Equal(Visibility.Visible, second.Visibility);
                    Assert.Equal(Visibility.Collapsed, third.Visibility);

                    double secondRight = GetNavigationElementRight(second, nav);
                    double overflowLeft = GetNavigationElementX(overflowButton, nav);
                    Assert.True(overflowLeft >= secondRight + 4.0 - 1.5,
                        "The overflow button should be laid out after the last visible item without overlapping it. "
                        + "overflowLeft=" + overflowLeft.ToString(format: null, CultureInfo.InvariantCulture) + ", secondRight=" + secondRight.ToString(format: null, CultureInfo.InvariantCulture) + ".");

                    System.Windows.Controls.ContextMenu overflowButtonContextMenu = Assert.IsType<System.Windows.Controls.ContextMenu>(overflowButton.ContextMenu, exactMatch: false);
                    _ = Assert.Single(overflowButtonContextMenu.Items);
                    Controls.MenuItem? firstOverflowItem = overflowButtonContextMenu.Items[0] as Controls.MenuItem;
                    Assert.Equal("Three", firstOverflowItem?.Header);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_OverflowButtonStaysLeftOfClippedItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 212,
                        Height = 240,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    NavigationViewItem first = new() { Content = "One", Icon = new FontIcon { Glyph = "\uE80F" } };
                    NavigationViewItem second = new() { Content = "Two", Icon = new FontIcon { Glyph = "\uE790" } };
                    NavigationViewItem trees = new() { Content = "Trees", Icon = new FontIcon { Glyph = "\uE8B7" } };
                    _ = nav.Items.Add(first);
                    _ = nav.Items.Add(second);
                    _ = nav.Items.Add(trees);

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.Button overflowButton = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton"), exactMatch: false);
                    Grid topItemsHost = Assert.IsType<Grid>(FindVisualChildByName<Grid>(nav, NavigationView.PART_TopItemsHost), exactMatch: false);
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    Assert.Equal(Visibility.Collapsed, trees.Visibility);

                    double overflowLeft = GetNavigationElementX(overflowButton, nav);
                    foreach (object item in nav.Items)
                    {
                        if (item is NavigationViewItem navItem && navItem.Visibility is Visibility.Visible)
                        {
                            Assert.True(GetNavigationElementRight(navItem, nav) <= overflowLeft - 4.0 + 1.5,
                                "Visible top items must clear the overflow button. item=" + navItem.Content);
                        }
                    }

                    double hostRight = GetNavigationElementRight(topItemsHost, nav);
                    double overflowRight = GetNavigationElementRight(overflowButton, nav);
                    Assert.True(overflowRight <= hostRight - 12.0 + 1.5,
                        "The overflow button should reserve 12px at the right edge of the top items host.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_ItemWidthChangeReflowsOverflowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 520,
                        Height = 240,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    NavigationViewItem first = new() { Content = "One", Icon = new FontIcon { Glyph = "\uE80F" } };
                    NavigationViewItem second = new() { Content = "Two", Icon = new FontIcon { Glyph = "\uE790" } };
                    NavigationViewItem third = new() { Content = "Three", Icon = new FontIcon { Glyph = "\uE8A7" } };
                    _ = nav.Items.Add(first);
                    _ = nav.Items.Add(second);
                    _ = nav.Items.Add(third);

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.Button overflowButton = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, NavigationView.PART_TopOverflowButton), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, overflowButton.Visibility);
                    Assert.Equal(Visibility.Visible, first.Visibility);

                    // Growing an item has to evict the width the overflow pass cached for it, otherwise
                    // the next pass keeps fitting the strip against the stale, narrower measurement.
                    first.Width = 480;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    Assert.Equal(Visibility.Collapsed, first.Visibility);
                    Assert.Equal(Visibility.Visible, second.Visibility);
                    Assert.Equal(Visibility.Visible, third.Visibility);

                    System.Windows.Controls.ContextMenu overflowButtonContextMenu = Assert.IsType<System.Windows.Controls.ContextMenu>(overflowButton.ContextMenu, exactMatch: false);
                    Controls.MenuItem overflowItem = Assert.IsType<Controls.MenuItem>(Assert.Single(overflowButtonContextMenu.Items));
                    Assert.Same(first, overflowItem.Tag);
                    Assert.Equal("One", overflowItem.Header);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_ShrinkingOverflowedItemRecoversItToStripAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 300,
                        Height = 240,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    NavigationViewItem first = new() { Content = "One", Width = 80 };
                    NavigationViewItem second = new() { Content = "Two", Width = 80 };
                    NavigationViewItem third = new() { Content = "Three", Width = 200 };
                    _ = nav.Items.Add(first);
                    _ = nav.Items.Add(second);
                    _ = nav.Items.Add(third);

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, third.Visibility);

                    // An item collapsed in the overflow menu is never measured, so no SizeChanged
                    // fires for it. Shrinking it must still evict the cached natural width (via the
                    // measure-affecting property change) and recover it to the strip; before the
                    // property-change eviction it stayed pinned in the menu on the stale 200px cache.
                    third.Width = 40;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Visible, third.Visibility);
                    Controls.Button overflowButton = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, NavigationView.PART_TopOverflowButton), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, overflowButton.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_ExactFitBoundaryDoesNotFlapLastItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 240,
                        Height = 240,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    NavigationViewItem first = new() { Content = "One", Width = 80 };
                    NavigationViewItem second = new() { Content = "Two", Width = 80 };
                    NavigationViewItem third = new() { Content = "Three", Width = 80 };
                    _ = nav.Items.Add(first);
                    _ = nav.Items.Add(second);
                    _ = nav.Items.Add(third);

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Grid topItemsHost = Assert.IsType<Grid>(FindVisualChildByName<Grid>(nav, NavigationView.PART_TopItemsHost), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, third.Visibility);

                    // The strip measures each item at its natural width (the explicit 80 plus the
                    // template's outer chrome), so derive the exact-fit total from a live measure
                    // instead of the raw Width values.
                    double totalItemWidth = first.DesiredSize.Width * 3.0;

                    // Grow the window so the strip is 2px past exact fit: inside the 5px recovery
                    // grace. The overflowed item must stay in the menu, because taking the
                    // all-items-fit exit here is precisely the strip/menu flap the grace damps.
                    double hostDeficit = totalItemWidth - topItemsHost.ActualWidth;
                    nav.Width += hostDeficit + 2.0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, third.Visibility);

                    // Clearing the grace (8px past exact fit) must recover the item.
                    nav.Width += 6.0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Visible, third.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static double GetNavigationElementX(FrameworkElement element, NavigationView ancestor)
        {
            return element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).X;
        }

        private static double GetNavigationElementRight(FrameworkElement element, NavigationView ancestor)
        {
            return GetNavigationElementX(element, ancestor) + element.ActualWidth;
        }
    }
}
