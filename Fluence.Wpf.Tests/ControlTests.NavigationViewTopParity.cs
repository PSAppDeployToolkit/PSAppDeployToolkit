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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void NavigationView_InFluenceWindow_LeftAndTopCoerceTitleBarExtension()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(window.ExtendsContentIntoTitleBar,
                        "Left NavigationView pane mode should extend FluenceWindow content into the title bar.");

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsFalse(window.ExtendsContentIntoTitleBar,
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

        [TestMethod]
        public void NavigationView_TopMode_CoercesPaneOpenAndToggleHidden()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(nav.IsPaneOpen, "Top mode should always report IsPaneOpen=True.");
                    Assert.IsFalse(nav.IsPaneToggleButtonVisible,
                        "Top mode should always report IsPaneToggleButtonVisible=False.");

                    nav.IsPaneOpen = false;
                    nav.IsPaneToggleButtonVisible = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(nav.IsPaneOpen, "Top mode should coerce runtime IsPaneOpen changes back to true.");
                    Assert.IsFalse(nav.IsPaneToggleButtonVisible,
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

        [TestMethod]
        public void NavigationView_TopMode_KeepsItemIconAndTextVisibleWithoutScrollViewer()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ScrollViewer? topScrollViewer = FindVisualChildByName<ScrollViewer>(nav, NavigationView.PartPaneItemsScrollViewer);
                    Assert.IsNull(topScrollViewer, "Top pane must not expose a scrolling pane-items strip.");

                    ContentPresenter? iconPresenter = FindVisualChildByName<ContentPresenter>(item, "IconPresenter");
                    ContentPresenter? contentPresenter = FindVisualChildByName<ContentPresenter>(item, "ContentPresenter");
                    Assert.IsNotNull(iconPresenter, "Top navigation items should still render their icon presenter.");
                    Assert.IsNotNull(contentPresenter, "Top navigation items should still render their text/content presenter.");
                    Assert.AreEqual(Visibility.Visible, iconPresenter.Visibility,
                        "Top navigation item icon presenter should stay visible.");
                    Assert.AreEqual(Visibility.Visible, contentPresenter.Visibility,
                        "Top navigation item content presenter should stay visible.");
                    Assert.AreEqual(14.0, item.FontSize, 0.01,
                        "NavigationViewItem text should be 14 pt to match the WinUI 3 BodyTextBlockStyle type-ramp rung.");
                    FontIcon? itemIcon = item.Icon as FontIcon;
                    Assert.IsNotNull(itemIcon, "Test item should use a FontIcon.");
                    Assert.AreEqual(16.0, itemIcon.IconFontSize, 0.01,
                        "NavigationViewItem glyphs should default to the compact 16px WinUI strip size.");
                    Assert.AreEqual(new Thickness(4, 0, 2, 0), iconPresenter.Margin,
                        "Top navigation item icon presenter should keep the tighter strip while adding 2px more lead-in before the icon.");
                    Assert.AreEqual(new Thickness(2, 0, 2, 0), contentPresenter.Margin,
                        "Top navigation item text presenter should use 2px horizontal spacing.");
                    System.Windows.Controls.Border? outerBorder = FindVisualChildByName<System.Windows.Controls.Border>(item, "OuterBorder");
                    ContentPresenter? infoBadgePresenter = FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter");
                    Assert.IsNotNull(outerBorder, "Top navigation item template should expose the outer border.");
                    Assert.IsNotNull(infoBadgePresenter, "Navigation item template should expose the info badge presenter.");
                    Assert.AreEqual(new Thickness(2, 4, 2, 4), outerBorder.Margin,
                        "Top navigation items should let hover and selected fills extend 2px wider on both sides.");
                    Assert.AreEqual(new Thickness(4, 0, 6, 0), outerBorder.Padding,
                        "Top navigation items should preserve content placement while widening the selected fill.");
                    Assert.AreEqual(Visibility.Collapsed, infoBadgePresenter.Visibility,
                        "Navigation items without an info badge should not reserve trailing badge space.");

                    ColumnDefinition? iconColumn = item.Template.FindName("IconColumn", item) as ColumnDefinition;
                    ColumnDefinition? gapColumn = item.Template.FindName("GapColumn", item) as ColumnDefinition;
                    ColumnDefinition? contentColumn = item.Template.FindName("ContentColumn", item) as ColumnDefinition;
                    Assert.IsNotNull(iconColumn, "Top navigation item template should expose the icon column.");
                    Assert.IsNotNull(gapColumn, "Top navigation item template should expose the icon/text gap column.");
                    Assert.IsNotNull(contentColumn, "Top navigation item template should expose the text content column.");
                    Assert.AreEqual(GridUnitType.Auto, iconColumn.Width.GridUnitType,
                        "Top navigation item icon column should size to its content instead of keeping the left-pane rail width.");
                    Assert.AreEqual(0.0, gapColumn.Width.Value, 0.01,
                        "Top navigation item icon/text gap column should not add extra spacing.");
                    Assert.AreEqual(GridUnitType.Auto, contentColumn.Width.GridUnitType,
                        "Top navigation item text column should size to content so items do not reserve a wide trailing gap.");

                    ContentPresenter? secondIconPresenter = FindVisualChildByName<ContentPresenter>(second, "IconPresenter");
                    Assert.IsNotNull(secondIconPresenter, "Second top navigation item should render its icon presenter.");
                    double textToNextIconGap = GetNavigationElementX(secondIconPresenter, nav) - GetNavigationElementRight(contentPresenter, nav);
                    Assert.AreEqual(24.0, textToNextIconGap, 1.5,
                        "Top navigation item text should include the requested inner border padding before the next item icon.");
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

        [TestMethod]
        public void NavigationView_TopMode_OverflowMenuInvokesHiddenItem()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.Button? overflowButton = FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton");
                    Assert.IsNotNull(overflowButton, "Top pane should expose a three-dot overflow button.");
                    Assert.AreEqual(ControlAppearance.Subtle, overflowButton.Appearance,
                        "Top pane overflow button should use the same subtle Fluence button chrome as other navigation strip buttons.");
                    Assert.AreEqual(Visibility.Visible, overflowButton.Visibility,
                        "Top pane overflow button should become visible when items do not fit.");
                    Grid? topItemsHost = FindVisualChildByName<Grid>(nav, NavigationView.PartTopItemsHost);
                    Assert.IsNotNull(topItemsHost, "Top pane should expose the horizontal item host.");
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
                    Assert.AreEqual(4.0, overflowButtonGap, 1.5,
                        "Top pane overflow button should sit after the last visible navigation item using the same 4px strip spacing.");
                    System.Windows.Controls.StackPanel? footer = nav.PaneFooter as System.Windows.Controls.StackPanel;
                    Assert.IsNotNull(footer, "Test setup should use a right-docked PaneFooter.");
                    Assert.IsTrue(GetNavigationElementRight(overflowButton, nav) <= GetNavigationElementX(footer, nav) + 0.5,
                        "Top pane overflow button should appear before the right-docked PaneFooter instead of docking to the strip edge.");
                    Assert.IsNotNull(overflowButton.ContextMenu, "Top pane overflow button should own a lightweight popup menu.");
                    Assert.IsTrue(overflowButton.ContextMenu.Items.Count > 0,
                        "Top pane overflow menu should contain hidden navigation items.");

                    Controls.MenuItem? overflowItem = overflowButton.ContextMenu.Items[^1] as Controls.MenuItem;
                    Assert.IsNotNull(overflowItem, "Overflow entries should be lightweight Fluence MenuItem rows.");
                    Assert.AreEqual(280.0, overflowItem.MinWidth, 0.01,
                        "Overflow entries should use the wider WinUI-style flyout row width.");
                    Assert.AreEqual(44.0, overflowItem.MinHeight, 0.01,
                        "Overflow entries should use a comfortably spaced WinUI-style row height.");
                    Assert.IsNotNull(overflowItem.Icon, "Overflow entries should include the underlying item icon.");
                    FontIcon? overflowIcon = overflowItem.Icon as FontIcon;
                    Assert.IsNotNull(overflowIcon, "Overflow entries should clone FontIcon icons.");
                    Assert.AreEqual(16.0, overflowIcon.IconFontSize, 0.01,
                        "Overflow menu glyphs should render at the compact 16px size even when the source item used a larger glyph.");
                    Assert.AreEqual("Diagnostics", overflowItem.Header,
                        "Overflow entries should show the underlying item text.");
                    overflowItem.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreSame(last, invokedItem,
                        "Clicking an overflow row should invoke the underlying NavigationViewItem without reparenting it.");
                    Assert.AreSame(last, nav.SelectedItem,
                        "Clicking an overflow row should select the underlying NavigationViewItem.");
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

        [TestMethod]
        public void NavigationView_TopMode_ReservesOverflowButtonByMovingLastFittingItemToMenu()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Controls.Button? overflowButton = FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton");
                    Assert.IsNotNull(overflowButton, "Top pane should expose a three-dot overflow button.");
                    Assert.AreEqual(Visibility.Visible, overflowButton.Visibility,
                        "The overflow button should be visible when all items fit only if the button is not reserved.");
                    Assert.AreEqual(Visibility.Visible, first.Visibility,
                        "The first item should remain on the strip.");
                    Assert.AreEqual(Visibility.Visible, second.Visibility,
                        "The second item should remain visible when it still clears the overflow button.");
                    Assert.AreEqual(Visibility.Collapsed, third.Visibility,
                        "The last item that would otherwise fit should move to the overflow menu to reserve button space.");

                    double secondRight = GetNavigationElementRight(second, nav);
                    double overflowLeft = GetNavigationElementX(overflowButton, nav);
                    Assert.IsTrue(overflowLeft >= secondRight + 4.0 - 1.5,
                        "The overflow button should be laid out after the last visible item without overlapping it. "
                        + "overflowLeft=" + overflowLeft.ToString(format: null, CultureInfo.InvariantCulture) + ", secondRight=" + secondRight.ToString(format: null, CultureInfo.InvariantCulture) + ".");

                    Assert.IsNotNull(overflowButton.ContextMenu, "Top pane overflow button should own a menu for collapsed items.");
                    Assert.AreEqual(1, overflowButton.ContextMenu.Items.Count,
                        "Measured clearance should move only the trailing item that does not fit to the overflow menu.");
                    Controls.MenuItem? firstOverflowItem = overflowButton.ContextMenu.Items[0] as Controls.MenuItem;
                    Assert.AreEqual("Three", firstOverflowItem?.Header,
                        "The moved item should appear in the overflow menu.");
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

        [TestMethod]
        public void NavigationView_TopMode_OverflowButtonStaysLeftOfClippedItem()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Controls.Button? overflowButton = FindVisualChildByName<Controls.Button>(nav, "PART_TopOverflowButton");
                    Grid? topItemsHost = FindVisualChildByName<Grid>(nav, NavigationView.PartTopItemsHost);
                    Assert.IsNotNull(overflowButton, "Top pane should expose a three-dot overflow button.");
                    Assert.IsNotNull(topItemsHost, "Top pane should expose the horizontal item host.");
                    Assert.AreEqual(Visibility.Visible, overflowButton.Visibility,
                        "Overflow button should be visible when the Trees item cannot fit cleanly.");
                    Assert.AreEqual(Visibility.Collapsed, trees.Visibility,
                        "Trees should move to overflow rather than overlap the three-dot button.");

                    double overflowLeft = GetNavigationElementX(overflowButton, nav);
                    foreach (object item in nav.Items)
                    {
                        if (item is NavigationViewItem navItem && navItem.Visibility is Visibility.Visible)
                        {
                            Assert.IsTrue(GetNavigationElementRight(navItem, nav) <= overflowLeft - 4.0 + 1.5,
                                "Visible top items must clear the overflow button. item=" + navItem.Content);
                        }
                    }

                    double hostRight = GetNavigationElementRight(topItemsHost, nav);
                    double overflowRight = GetNavigationElementRight(overflowButton, nav);
                    Assert.IsTrue(overflowRight <= hostRight - 12.0 + 1.5,
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
