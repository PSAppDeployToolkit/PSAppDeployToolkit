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
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public Task NavigationView_InFluenceWindow_LeftAndTopCoerceTitleBarExtensionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_CoercesPaneOpenAndToggleHiddenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_KeepsItemIconAndTextVisibleWithoutScrollViewerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ScrollViewer? topScrollViewer = FindVisualChildByName<ScrollViewer>(nav, NavigationView.PartPaneItemsScrollViewer);
                    Assert.Null(topScrollViewer);

                    ContentPresenter iconPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "IconPresenter"));
                    ContentPresenter contentPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "ContentPresenter"));
                    Assert.Equal(Visibility.Visible, iconPresenter.Visibility);
                    Assert.Equal(Visibility.Visible, contentPresenter.Visibility);
                    Assert.Equal(14.0, item.FontSize, 0.01);
                    FontIcon itemIcon = Assert.IsType<FontIcon>(item.Icon);
                    Assert.Equal(16.0, itemIcon.IconFontSize, 0.01);
                    Assert.Equal(new Thickness(4, 0, 2, 0), iconPresenter.Margin);
                    Assert.Equal(new Thickness(2, 0, 2, 0), contentPresenter.Margin);
                    System.Windows.Controls.Border outerBorder = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(item, "OuterBorder"));
                    ContentPresenter infoBadgePresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter"));
                    Assert.Equal(new Thickness(2, 4, 2, 4), outerBorder.Margin);
                    Assert.Equal(new Thickness(4, 0, 6, 0), outerBorder.Padding);
                    Assert.Equal(Visibility.Collapsed, infoBadgePresenter.Visibility);

                    ColumnDefinition iconColumn = Assert.IsType<ColumnDefinition>(item.Template.FindName("IconColumn", item));
                    ColumnDefinition gapColumn = Assert.IsType<ColumnDefinition>(item.Template.FindName("GapColumn", item));
                    ColumnDefinition contentColumn = Assert.IsType<ColumnDefinition>(item.Template.FindName("ContentColumn", item));
                    Assert.Equal(GridUnitType.Auto, iconColumn.Width.GridUnitType);
                    Assert.Equal(0.0, gapColumn.Width.Value, 0.01);
                    Assert.Equal(GridUnitType.Auto, contentColumn.Width.GridUnitType);

                    ContentPresenter secondIconPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(second, "IconPresenter"));
                    double textToNextIconGap = GetNavigationElementX(secondIconPresenter, nav) - GetNavigationElementRight(contentPresenter, nav);
                    Assert.Equal(24.0, textToNextIconGap, 1.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_OverflowMenuInvokesHiddenItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    Controls.Button overflowButton = Assert.IsAssignableFrom<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton"));
                    Assert.Equal(ControlAppearance.Subtle, overflowButton.Appearance);
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    Grid topItemsHost = Assert.IsAssignableFrom<Grid>(FindVisualChildByName<Grid>(nav, NavigationView.PartTopItemsHost));
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
                    Assert.NotNull(overflowButton.ContextMenu);
                    Assert.True(overflowButton.ContextMenu.Items.Count > 0,
                        "Top pane overflow menu should contain hidden navigation items.");

                    Controls.MenuItem overflowItem = Assert.IsType<Controls.MenuItem>(overflowButton.ContextMenu.Items[^1]);
                    Assert.Equal(280.0, overflowItem.MinWidth, 0.01);
                    Assert.Equal(44.0, overflowItem.MinHeight, 0.01);
                    Assert.NotNull(overflowItem.Icon);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_ReservesOverflowButtonByMovingLastFittingItemToMenuAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    Controls.Button overflowButton = Assert.IsAssignableFrom<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton"));
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    Assert.Equal(Visibility.Visible, first.Visibility);
                    Assert.Equal(Visibility.Visible, second.Visibility);
                    Assert.Equal(Visibility.Collapsed, third.Visibility);

                    double secondRight = GetNavigationElementRight(second, nav);
                    double overflowLeft = GetNavigationElementX(overflowButton, nav);
                    Assert.True(overflowLeft >= secondRight + 4.0 - 1.5,
                        "The overflow button should be laid out after the last visible item without overlapping it. "
                        + "overflowLeft=" + overflowLeft.ToString(format: null, CultureInfo.InvariantCulture) + ", secondRight=" + secondRight.ToString(format: null, CultureInfo.InvariantCulture) + ".");

                    Assert.NotNull(overflowButton.ContextMenu);
                    _ = Assert.Single(overflowButton.ContextMenu.Items);
                    Controls.MenuItem? firstOverflowItem = overflowButton.ContextMenu.Items[0] as Controls.MenuItem;
                    Assert.Equal("Three", firstOverflowItem?.Header);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_OverflowButtonStaysLeftOfClippedItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    Controls.Button overflowButton = Assert.IsAssignableFrom<Controls.Button>(FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton"));
                    Grid topItemsHost = Assert.IsAssignableFrom<Grid>(FindVisualChildByName<Grid>(nav, NavigationView.PartTopItemsHost));
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
