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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualGeometry;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="NavigationView"/> control in Left and
    /// LeftCompact pane display modes: pane chrome, item selection and invocation, the shared
    /// selection indicator, back button states, content offset, header rendering, pane width
    /// animation, and surface brush theming.
    /// </summary>
    public sealed class NavigationViewTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        /// <summary>
        /// Returns the StackPanel that hosts a NavigationView's realized items.
        /// </summary>
        /// <param name="nav">The navigation view to inspect.</param>
        /// <returns>The items host panel, or <see langword="null"/> when not yet realized.</returns>
        private static System.Windows.Controls.StackPanel? GetNavigationViewItemsHostPanel(NavigationView nav)
        {
            ItemsPresenter? presenter = FindVisualChild<ItemsPresenter>(nav);
            if (presenter is null)
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(presenter);
            return childCount < 1 ? null : VisualTreeHelper.GetChild(presenter, 0) as System.Windows.Controls.StackPanel;
        }

        private static async Task AssertContentOffsetEventuallyAsync(
            Window window,
            FrameworkElement nav,
            FrameworkElement presenter,
            double expectedOffset)
        {
            _ = await WaitUntilAsync(window.Dispatcher, 3000, delegate
            {
                window.UpdateLayout();
                return Math.Abs(GetContentOffsetX(nav, presenter) - expectedOffset) <= 1.0;
            }).ConfigureAwait(true);

            window.UpdateLayout();
            Assert.Equal(expectedOffset, GetContentOffsetX(nav, presenter), 1.0);
        }

        private static double GetContentOffsetX(FrameworkElement nav, FrameworkElement presenter)
        {
            return presenter.TransformToAncestor(nav).Transform(new Point(0, 0)).X;
        }

        private static void AssertPaneToggleVisible(NavigationView nav)
        {
            _ = nav.ApplyTemplate();
            System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(
                nav.Template.FindName(NavigationView.PART_PaneToggleButton, nav), exactMatch: false);
            Assert.Equal(Visibility.Visible, paneToggle.Visibility);
        }

        [Fact]
        public Task DemoMainWindow_LeftPaneFooterIcon_StaysLeftAnchored_WhileCollapsedAsync()
        {
            // Regression: the Settings footer item must keep its icon at the pane's left edge at every
            // pane width. As a FooterMenuItems entry it is hosted in a stretching StackPanel (like the
            // main items), so the fixed 40px icon column keeps the icon anchored at the left regardless
            // of the animating pane width. We force intermediate closed pane widths against the real
            // gallery MainWindow and assert the footer icon stays at the left.
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                // Demo opt-in: this test constructs the real gallery Demo.MainWindow, whose
                // pages (e.g. GalleryHomePage.xaml, GalleryPageScrollViewerStyle) are styled from
                // Fluence.Wpf.Demo/Resources/DemoSharedStyles.xaml, not the library theme.
                _ = TestApp.EnsureDemoTheme();
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);

                Demo.MainWindow mw = new()
                {
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    mw.Show();
                    WpfTestSta.DrainDispatcher(mw.Dispatcher);
                    // Settle until the shell's NavigationView is realized rather than padding a
                    // fixed delay; returns as soon as the visual tree is up.
                    _ = await WaitUntilAsync(mw.Dispatcher, 2000, () => FindVisualChildByName<NavigationView>(mw, "DemoNav") is not null).ConfigureAwait(true);
                    mw.UpdateLayout();

                    NavigationView nav = Assert.IsType<NavigationView>(FindVisualChildByName<NavigationView>(mw, "DemoNav"), exactMatch: false);
                    NavigationViewItem footer = Assert.IsType<NavigationViewItem>(nav.FooterMenuItems.Count > 0 ? nav.FooterMenuItems[0] as NavigationViewItem : null);
                    ContentPresenter footerIcon = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(footer, "IconPresenter"), exactMatch: false);

                    nav.IsPaneOpen = false;
                    await WaitForAnimationAndDrainAsync(mw.Dispatcher, 300).ConfigureAwait(true);
                    mw.UpdateLayout();

                    ColumnDefinition paneColumn = Assert.IsType<ColumnDefinition>(nav.Template.FindName("PaneColumn", nav));

                    foreach (double width in (IReadOnlyList<double>)[96.0, 160.0, 240.0, 320.0])
                    {
                        paneColumn!.BeginAnimation(ColumnDefinition.WidthProperty, animation: null);
                        paneColumn.Width = new GridLength(width);
                        mw.UpdateLayout();
                        double footerIconX = footerIcon!.TransformToAncestor(nav).Transform(new Point(0, 0)).X;
                        Assert.True(footerIconX <= 28.0, "Collapsed footer icon must stay anchored near the pane left edge (not centered/sliding) at pane width " +
                            width.ToString(System.Globalization.CultureInfo.InvariantCulture) + "; measured x=" +
                            footerIconX.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
                    }
                }
                finally
                {
                    mw.Content = null;
                    mw.Close();
                    WpfTestSta.DrainDispatcher(mw.Dispatcher);
                }
            });
        }


        [Fact]
        public Task NavigationView_CompactRailWidth_DoesNotFollowTheBackButtonAsync()
        {
            // Regression: the closed pane width was 96 while the back button showed and 48 while it
            // did not, so enabling the back button animated the rail wider and looked as though the
            // pane had been opened. The back button and the pane toggle share the chrome row above
            // the rail and that row may run wider than the rail, so the back button moves the
            // toggle along instead. Only IsPaneOpen changes the rail's width.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 320,
                        Height = 240,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneToggleButtonVisible = true,
                        IsBackButtonVisible = true,
                        IsBackEnabled = false,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Dashboard" });

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ColumnDefinition paneColumn = Assert.IsType<ColumnDefinition>(
                        nav.Template.FindName("PaneColumn", nav), exactMatch: false);
                    double closedWidth = paneColumn.Width.Value;

                    nav.SetCurrentValue(NavigationView.IsBackEnabledProperty, value: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(48.0, closedWidth, 0.5);
                    Assert.Equal(closedWidth, paneColumn.Width.Value, 0.5);

                    // The toggle is still there, pushed along by the back button rather than
                    // replaced by it, and the chrome row is free to run wider than the rail.
                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_BackButton, nav));
                    System.Windows.Controls.Button toggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_PaneToggleButton, nav));
                    Assert.Equal(Visibility.Visible, back.Visibility);
                    Assert.Equal(Visibility.Visible, toggle.Visibility);
                    Assert.Equal(48.0, toggle.TransformToAncestor(nav).Transform(new Point(0, 0)).X, 1.0);

                    // Being laid out at 48 is not enough: the chrome row overhangs the rail and the
                    // content column is declared after the pane, so without a z-order of its own the
                    // pane toggle would be painted over and unclickable. Hit-test the point the user
                    // aims at and require the toggle to be what answers.
                    Point toggleCentre = toggle.TransformToAncestor(nav).Transform(new Point(toggle.ActualWidth / 2, toggle.ActualHeight / 2));
                    HitTestResult hit = Assert.IsType<HitTestResult>(VisualTreeHelper.HitTest(nav, toggleCentre), exactMatch: false);
                    Assert.True(
                        IsDescendantOf(hit.VisualHit, toggle),
                        "The pane toggle must be the topmost element at its own centre; the content column is covering it.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static bool IsDescendantOf(DependencyObject? candidate, DependencyObject ancestor)
        {
            DependencyObject? current = candidate;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        [Fact]
        public Task NavigationView_NestedInContent_KeepsItsOwnPaneChromeAsync()
        {
            // A window whose title bar hosts the navigation chrome suppresses the shell pane's own
            // back and pane toggle buttons, so the two are not drawn twice. That rule is about the
            // window's own pane: a NavigationView nested in page content is a control on the page
            // and has to keep its buttons, or a sample with IsPaneToggleButtonVisible="True" shows
            // no toggle at all.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FluenceWindow window = new()
                {
                    ExtendsContentIntoTitleBar = true,
                    TitleBar = new TitleBar(),
                    Width = 640,
                    Height = 400,
                };

                NavigationView shell = new()
                {
                    PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    IsPaneToggleButtonVisible = true,
                };
                _ = shell.Items.Add(new NavigationViewItem { Content = "Home" });

                NavigationView nested = new()
                {
                    PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                    IsPaneToggleButtonVisible = true,
                    Width = 320,
                    Height = 200,
                };
                _ = nested.Items.Add(new NavigationViewItem { Content = "Dashboard" });

                shell.Content = nested;
                window.Content = shell;

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = shell.ApplyTemplate();
                    _ = nested.ApplyTemplate();

                    System.Windows.Controls.Button shellToggle = Assert.IsType<System.Windows.Controls.Button>(shell.Template.FindName(NavigationView.PART_PaneToggleButton, shell));
                    System.Windows.Controls.Button nestedToggle = Assert.IsType<System.Windows.Controls.Button>(nested.Template.FindName(NavigationView.PART_PaneToggleButton, nested));

                    Assert.Equal(Visibility.Collapsed, shellToggle.Visibility);
                    Assert.Equal(Visibility.Visible, nestedToggle.Visibility);

                    // The mirror is what the template reads, so assert it directly too.
                    Assert.True(shell.HostHasTitleBar);
                    Assert.False(nested.HostHasTitleBar);
                    Assert.True(shell.HostExtendsContentIntoTitleBar);
                    Assert.False(nested.HostExtendsContentIntoTitleBar);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_MirrorsHostTitleBarState_AndReleasesItOnUnloadAsync()
        {
            // The pane's triggers need the owning window's title bar state. Read through a
            // FindAncestor binding they re-evaluate while the window is tearing the visual tree
            // down, when the ancestor is already gone, and WPF logs a "cannot find source" binding
            // error per trigger for every NavigationView on the way out. The state is mirrored
            // onto the control instead, so the triggers bind to a source that always resolves.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FluenceWindow window = new()
                {
                    ExtendsContentIntoTitleBar = true,
                    Width = 480,
                    Height = 320,
                };

                NavigationView navigation = new() { PaneDisplayMode = NavigationViewPaneDisplayMode.Left };
                _ = navigation.Items.Add(new NavigationViewItem { Content = "Home" });
                window.Content = navigation;

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(navigation.HostExtendsContentIntoTitleBar);

                    // Top mode drives the window the other way (UpdateTitleBarExtensionForPaneMode),
                    // so this exercises the change notification rather than only the initial read.
                    // Setting the window property directly would not: Left mode forces it back to
                    // true on the spot.
                    navigation.SetCurrentValue(NavigationView.PaneDisplayModeProperty, NavigationViewPaneDisplayMode.Top);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(window.ExtendsContentIntoTitleBar);
                    Assert.False(navigation.HostExtendsContentIntoTitleBar);

                    navigation.SetCurrentValue(NavigationView.PaneDisplayModeProperty, NavigationViewPaneDisplayMode.Left);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(window.ExtendsContentIntoTitleBar);
                    Assert.True(navigation.HostExtendsContentIntoTitleBar);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }

                // Out of the tree the mirror reports the default rather than a stale reading of a
                // window that is on its way out.
                Assert.False(navigation.HostExtendsContentIntoTitleBar);
                Assert.False(navigation.HostHasTitleBar);
            });
        }

        [Fact]
        public Task NavigationView_PaneDisplayMode_Left_RendersVerticalPaneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsType<System.Windows.Controls.StackPanel>(GetNavigationViewItemsHostPanel(nav), exactMatch: false);
                    Assert.Equal(Orientation.Vertical, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneDisplayMode_Top_RendersHorizontalPaneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsType<System.Windows.Controls.StackPanel>(GetNavigationViewItemsHostPanel(nav), exactMatch: false);
                    Assert.Equal(Orientation.Horizontal, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneItemsScrollViewer_UsesFluentScrollViewerStyleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.LeftCompact, isPaneOpen: false);
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_ClosedPaneKeepsIconFooterVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationViewItem footer = new()
                    {
                        Content = "Settings",
                        Icon = new FontIcon { Glyph = "\uE713", IconFontSize = 20 },
                    };
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false,
                        PaneFooter = footer,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border footerHost = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneFooterHost"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, footerHost.Visibility);
                    Assert.True(footer.ActualWidth >= 48.0 - 0.5, "LeftCompact footer navigation items should receive the full compact pane width so their icons are visible.");

                    nav.IsPaneOpen = true;
                    // Settle until the footer host reaches the asserted Visible state.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => footerHost.Visibility is Visibility.Visible).ConfigureAwait(true);

                    Assert.Equal(Visibility.Visible, footerHost.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftClosedPaneItemsKeepFullIconWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationViewItem messages = new()
                    {
                        Content = "Messages",
                        Icon = new FontIcon { Glyph = "\uE8BD", IconFontSize = 20 },
                        IsSelected = true,
                    };
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = false,
                    };
                    _ = nav.Items.Add(messages);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(48.0, nav.GetPaneColumnWidthForTesting(), 0.01);
                    Assert.True(messages.ActualWidth >= 48.0 - 0.5, "Closed Left navigation items should receive the full compact pane width so icons are not clipped.");

                    ContentPresenter iconPresenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(messages, "IconPresenter"), exactMatch: false);
                    Point iconOffset = iconPresenter.TransformToAncestor(messages).Transform(new Point(0, 0));
                    Assert.True(iconOffset.X >= 4.0 - 0.5, "Closed Left icon should not be clipped on the left edge.");
                    Assert.True(iconOffset.X + iconPresenter.ActualWidth <= 44.0 + 0.5, "Closed Left icon should stay inside the 40px icon slot.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_ClosedPaneItemsKeepFullIconWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationViewItem messages = new()
                    {
                        Content = "Messages",
                        Icon = new FontIcon { Glyph = "\uE8BD", IconFontSize = 20 },
                        IsSelected = true,
                    };
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false,
                    };
                    _ = nav.Items.Add(messages);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(48.0, nav.GetPaneColumnWidthForTesting(), 0.01);
                    Assert.True(messages.ActualWidth >= 48.0 - 0.5, "Closed LeftCompact navigation items should receive the full compact pane width so icons are not clipped.");

                    ContentPresenter iconPresenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(messages, "IconPresenter"), exactMatch: false);
                    Point iconOffset = iconPresenter.TransformToAncestor(messages).Transform(new Point(0, 0));
                    Assert.True(iconOffset.X >= 4.0 - 0.5, "Closed LeftCompact icon should not be clipped on the left edge.");
                    Assert.True(iconOffset.X + iconPresenter.ActualWidth <= 44.0 + 0.5, "Closed LeftCompact icon should stay inside the 40px icon slot.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftPaneToggleGlyph_IsOffsetToAlignWithItemIconsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FontIcon glyph = Assert.IsType<FontIcon>(FindVisualChildByName<FontIcon>(nav, "PaneToggleGlyph"), exactMatch: false);
                    Assert.Equal(2.0, glyph.Margin.Left, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftChrome_BackPrecedesPaneToggleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsBackButtonVisible = true,
                        IsBackEnabled = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_BackButton, nav));
                    System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_PaneToggleButton, nav));

                    System.Windows.Controls.StackPanel chrome = Assert.IsType<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(nav, "PaneChrome"), exactMatch: false);
                    Assert.Equal(Orientation.Horizontal, chrome.Orientation);
                    // WinUI's chrome buttons are 40 by 36 inside a 4,2 margin (NavigationBackButton.xaml:8-14,
                    // PaneToggleButtonWidth/Height 40 and 36), which fills the same 48 by 40 slot the
                    // rail reserves.
                    Assert.Equal(40.0, back.ActualWidth, 0.5);
                    Assert.Equal(36.0, back.ActualHeight, 0.5);
                    Assert.Equal(40.0, paneToggle.ActualWidth, 0.5);
                    Assert.Equal(36.0, paneToggle.ActualHeight, 0.5);

                    Point backPoint = back.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Point paneTogglePoint = paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.True(backPoint.X < paneTogglePoint.X, "Back button should be the first glyph, before the pane toggle.");
                    Assert.Equal(backPoint.Y, paneTogglePoint.Y, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_DefaultFontIconSizeIs16Async()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    FontIcon icon = new() { Glyph = "\uE80F" };
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One", Icon = icon });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(16.0, icon.IconFontSize, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_Template_RendersInfoBadgeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    FontIcon badge = new() { Glyph = "\uE70D", IconFontSize = 12 };
                    NavigationViewItem item = new()
                    {
                        Content = "Section",
                        Icon = new FontIcon { Glyph = "\uE8FD", IconFontSize = 20 },
                        InfoBadge = badge,
                    };

                    window.Content = item;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter"), exactMatch: false);
                    Assert.Same(badge, presenter.Content);
                    Assert.True(double.IsNaN(presenter.Width) || presenter.Width >= 34.0,
                        "NavigationViewItem must not constrain InfoBadge value pills to the old 24px slot.");
                    Assert.Equal(HorizontalAlignment.Center, presenter.HorizontalAlignment);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_SelectedItem_UpdatesOnItemClickAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = false,
                    };
                    NavigationViewItem item0 = new() { Content = "Zero" };
                    NavigationViewItem item1 = new() { Content = "One" };
                    _ = nav.Items.Add(item0);
                    _ = nav.Items.Add(item1);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, nav.SelectedIndex);
                    Assert.Same(item1, nav.SelectedItem);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_ItemInvoked_FiresBeforeSelectionChangesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    NavigationViewItem item0 = new() { Content = "Zero" };
                    NavigationViewItem item1 = new() { Content = "One" };
                    _ = nav.Items.Add(item0);
                    _ = nav.Items.Add(item1);
                    nav.SelectedItem = item0;
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    List<string> calls = [];
                    NavigationViewItemInvokedEventArgs? invokedArgs = null;
                    nav.ItemInvoked += (sender, e) =>
                    {
                        invokedArgs = e;
                        calls.Add("invoked:" + e.InvokedItemContainer.Content);
                    };
                    nav.SelectionChanged += delegate
                    {
                        calls.Add("selection:" + ((NavigationViewItem)nav.SelectedItem).Content);
                    };

                    AutomationPeer peer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(item1), exactMatch: false);
                    IInvokeProvider invokeProvider = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);

                    invokeProvider.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.NotNull(invokedArgs);
                    Assert.Same(item1, invokedArgs.InvokedItemContainer);
                    Assert.Same(item1, invokedArgs.InvokedItem);
                    Assert.False(invokedArgs.IsSettingsInvoked,
                        "Regular pane item invocation should not be reported as settings invocation.");
                    Assert.Equal(["invoked:One", "selection:One"], calls, StringComparer.Ordinal);
                    Assert.Same(item1, nav.SelectedItem);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_SelectionFollowsFocus_True_SelectsOnFocusAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Zero" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    FrameworkElement container1 = Assert.IsType<FrameworkElement>(nav.ItemContainerGenerator.ContainerFromIndex(1), exactMatch: false);
                    _ = Keyboard.Focus(container1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_SelectionFollowsFocus_False_DoesNotSelectOnFocusAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = false,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Zero" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    FrameworkElement container1 = Assert.IsType<FrameworkElement>(nav.ItemContainerGenerator.ContainerFromIndex(1), exactMatch: false);
                    _ = Keyboard.Focus(container1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(0, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_IsBackButtonVisible_False_HidesBackButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = false,
                        IsBackEnabled = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_BackButton, nav));
                    Assert.Equal(Visibility.Collapsed, back.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_IsBackEnabled_False_CollapsesBackButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = true,
                        IsBackEnabled = false,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_BackButton, nav));
                    System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_PaneToggleButton, nav));
                    Assert.Equal(Visibility.Collapsed, back.Visibility);

                    // With no back button the toggle takes the chrome's first slot: its own 4 dip
                    // inset, which is WinUI's (NavigationBackButton.xaml:14, Margin 4,2), leaving the
                    // 40 dip button centred in the 48 dip rail.
                    Assert.Equal(4.0, paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0)).X, 0.5);
                    Assert.Equal(24.0, paneToggle.TransformToAncestor(nav).Transform(new Point(paneToggle.ActualWidth / 2, 0)).X, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftModes_ForcePaneToggleVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                NavigationViewPaneDisplayMode[] modes =
                [
                    NavigationViewPaneDisplayMode.Left,
                    NavigationViewPaneDisplayMode.LeftCompact,
                ];

                foreach (NavigationViewPaneDisplayMode mode in modes)
                {
                    Window window = new();

                    try
                    {
                        NavigationView nav = new()
                        {
                            Width = 400,
                            Height = 320,
                            PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                            IsPaneToggleButtonVisible = false,
                        };
                        _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                        window.Content = nav;
                        window.Show();
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();

                        Assert.False(nav.IsPaneToggleButtonVisible,
                            "Top mode should keep the pane toggle hidden before switching to " + mode + ".");

                        nav.PaneDisplayMode = mode;
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();

                        Assert.True(nav.IsPaneToggleButtonVisible,
                            mode + " should coerce the pane toggle visible after switching from Top.");
                        AssertPaneToggleVisible(nav);

                        nav.IsPaneToggleButtonVisible = false;
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();

                        Assert.True(nav.IsPaneToggleButtonVisible,
                            mode + " should coerce runtime attempts to hide the pane toggle back to visible.");
                        AssertPaneToggleVisible(nav);
                    }
                    finally
                    {
                        CloseWindowAndDrain(window);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_BackRequested_FiresOnBackClickAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = true,
                        IsBackEnabled = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    bool fired = false;
                    void handler(object? sender, NavigationViewBackRequestedEventArgs e) { fired = true; }
                    nav.BackRequested += handler;
                    _ = nav.ApplyTemplate();
                    nav.RaiseBackRequestedForTesting();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(fired);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_ThemeSwitch_UpdatesBrushesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(application.Resources.MergedDictionaries.Count > 0);
                    Color lightBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Color darkBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    Assert.NotEqual(lightBase, darkBase);
                    nav.UpdateLayout();
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_SharedIndicator_ExistsInTemplate_AndVisibleWhenSelectedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    NavigationViewItem item0 = new() { Content = "One" };
                    NavigationViewItem item1 = new() { Content = "Two" };
                    _ = nav.Items.Add(item0);
                    _ = nav.Items.Add(item1);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    Assert.Equal(0.0, indicator.Opacity, 0.01);

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_PreTemplateSelection_PositionsSharedIndicatorAfterTemplateAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    NavigationViewItem item = new()
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 },
                    };
                    _ = nav.Items.Add(item);
                    nav.SelectedItem = item;

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_SharedIndicator_TracksHorizontalItemPlacementAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 },
                    });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Child", IsChildItem = true });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    double iconItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.Equal(4.0, iconItemX, 0.5);

                    nav.SelectedIndex = 1;
                    // Settle until the indicator finishes travelling to the child item.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(GetSelectionIndicatorTranslate(indicator).X - iconItemX) <= 0.5).ConfigureAwait(true);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // A child item's indicator sits at the same x as a top-level item's. WinUI gives
                    // depth indentation to the presenter's ContentGrid alone, never to the indicator
                    // wrapper beside it, so the rail stays one straight column down the pane.
                    double childItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.Equal(iconItemX, childItemX, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_SharedIndicator_AnimatesBetweenSelectionsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 },
                    });
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Settings",
                        Icon = new FontIcon { Glyph = "\uE713", IconFontSize = 20 },
                    });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    TranslateTransform translate = GetSelectionIndicatorTranslate(indicator);
                    Assert.False(translate.HasAnimatedProperties,
                        "Initial selection should snap before later changes animate.");

                    nav.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(translate.HasAnimatedProperties,
                        "Changing selection should animate the shared indicator transform.");
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 600).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_RapidReselection_IndicatorSettlesOnFinalTargetAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 },
                    });
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Search",
                        Icon = new FontIcon { Glyph = "\uE721", IconFontSize = 20 },
                    });
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Settings",
                        Icon = new FontIcon { Glyph = "\uE713", IconFontSize = 20 },
                    });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    TranslateTransform translate = GetSelectionIndicatorTranslate(indicator);
                    ScaleTransform scale = GetSelectionIndicatorScale(indicator);
                    double homeY = translate.Y;

                    // Reference pass: settle on the last item once to learn its resting slot.
                    nav.SelectedIndex = 2;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, delegate
                        {
                            return !translate.HasAnimatedProperties
                                && Math.Abs(indicator.Opacity - 1.0) <= 0.01
                                && Math.Abs(translate.Y - homeY) > 1.0;
                        }).ConfigureAwait(true),
                        "Reference selection of the last item should settle the indicator on its slot.");
                    double settingsX = translate.X;
                    double settingsY = translate.Y;

                    // Back to the first item so the rapid burst has to cross multiple slots.
                    nav.SelectedIndex = 0;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, delegate
                        {
                            return !translate.HasAnimatedProperties
                                && Math.Abs(indicator.Opacity - 1.0) <= 0.01
                                && Math.Abs(translate.Y - homeY) <= 0.5;
                        }).ConfigureAwait(true),
                        "The indicator should settle back on the first item before the rapid burst.");

                    // Rapid burst: retarget to the middle item and then immediately to the last
                    // item without draining, interrupting the in-flight depart/arrive sequence.
                    nav.SelectedIndex = 1;
                    nav.SelectedIndex = 2;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, delegate
                        {
                            return Math.Abs(translate.Y - settingsY) <= 0.5
                                && Math.Abs(indicator.Opacity - 1.0) <= 0.01
                                && Math.Abs(scale.ScaleX - 1.0) <= 0.01
                                && Math.Abs(scale.ScaleY - 1.0) <= 0.01;
                        }).ConfigureAwait(true),
                        "After a rapid mid-flight retarget, the indicator should settle on the final item's slot.");
                    Assert.Equal(settingsX, translate.X, 0.5);
                    Assert.Equal(settingsY, translate.Y, 0.5);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                    Assert.Equal(1.0, scale.ScaleX, 0.01);
                    Assert.Equal(1.0, scale.ScaleY, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_IndicatorTravelsWithoutFadingOutAsync()
        {
            // Replaces the two tests that pinned the old slide-out and slide-in: the indicator used
            // to fade to nothing, jump the gap and fade back in, so it never visibly crossed the
            // space between two items. WinUI holds opacity at 1 for a move inside one list and plays
            // a stretch-and-settle instead (NavigationView.cpp:2185-2234), which is what this now
            // asserts: the bar stays visible throughout and stretches past its own length mid-flight.
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Two" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Three" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border indicator = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(nav, "PART_SelectionIndicator"), exactMatch: false);
                    TransformGroup group = Assert.IsType<TransformGroup>(indicator.RenderTransform);
                    ScaleTransform scale = Assert.IsType<ScaleTransform>(group.Children[0]);
                    TranslateTransform translate = Assert.IsType<TranslateTransform>(group.Children[1]);

                    double startY = translate.Y;

                    // Travel two items down, sampling while the animation runs.
                    nav.SelectedIndex = 2;

                    double minimumOpacity = 1.0;
                    double peakScale = 1.0;
                    for (int sample = 0; sample < 40; sample++)
                    {
                        minimumOpacity = Math.Min(minimumOpacity, indicator.Opacity);
                        peakScale = Math.Max(peakScale, scale.ScaleY);
                        await Task.Delay(10, TestContext.Current.CancellationToken).ConfigureAwait(true);
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                    }

                    Assert.True(
                        minimumOpacity > 0.99,
                        "The indicator must stay at full opacity while it travels; it used to fade out and back in.");
                    Assert.True(
                        peakScale > 1.05,
                        "The indicator must stretch across the gap mid-flight rather than keeping its rest length.");

                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, delegate
                        {
                            return Math.Abs(scale.ScaleY - 1.0) <= 0.01 && translate.Y > startY;
                        }).ConfigureAwait(true),
                        "The indicator should settle at its rest length on the new item.");
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_TopLevelIconlessItem_DoesNotUseChildIndicatorIndentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 },
                    });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "No icon top-level" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    double iconItemX = GetSelectionIndicatorTranslate(indicator).X;

                    nav.SelectedIndex = 1;
                    // Settle until the indicator returns to the icon-item offset (the asserted value).
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(GetSelectionIndicatorTranslate(indicator).X - iconItemX) <= 0.5).ConfigureAwait(true);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    double noIconItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.Equal(iconItemX, noIconItemX, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_FocusVisual_StaysInsideItemBoundsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();

                Style style = Assert.IsType<Style>(application.TryFindResource("NavigationViewItemFocusVisual"));
                ControlTemplate template = Assert.IsType<ControlTemplate>(style.Setters.OfType<Setter>().FirstOrDefault(static setter => setter.Property == System.Windows.Controls.Control.TemplateProperty)?.Value as ControlTemplate);
                DependencyObject root = Assert.IsType<DependencyObject>(template.LoadContent(), exactMatch: false);

                foreach (System.Windows.Controls.Border border in FindVisualChildren<System.Windows.Controls.Border>(root))
                {
                    Assert.True(border.Margin.Left >= 0.0 && border.Margin.Right >= 0.0,
                        "Navigation item focus strokes should stay inside the selected item bounds horizontally.");
                }
            });
        }

        [Fact]
        public Task NavigationView_SharedIndicator_HidesWhenSelectionClearedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.Equal(1.0, indicator?.Opacity ?? 0.0, 0.01);

                    nav.SelectedItem = null;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(0.0, indicator?.Opacity ?? 1.0, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_SharedIndicator_VisibleWhenSelectedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Alpha" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Beta" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_Indicator_SitsUnderTheSelectedItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    NavigationViewItem alpha = new() { Content = "Alpha" };
                    NavigationViewItem beta = new() { Content = "Beta" };
                    _ = nav.Items.Add(alpha);
                    _ = nav.Items.Add(beta);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 700).ConfigureAwait(true);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);

                    // The indicator is bottom aligned in a host that spans the whole 48 dip bar, so
                    // without the lift applied in CalculateIndicatorPosition it lands on the bar's
                    // bottom edge instead of under the item. WinUI insets it 4 dip from the item's
                    // own bottom edge.
                    double indicatorBottom = indicator.TransformToAncestor(nav).Transform(new Point(0, indicator.ActualHeight)).Y;
                    double itemBottom = beta.TransformToAncestor(nav).Transform(new Point(0, beta.ActualHeight)).Y;
                    Assert.Equal(itemBottom - 4.0, indicatorBottom, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_FullThemeCycle_NoExceptionsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ApplicationTheme[] themes =
                    [
                        ApplicationTheme.Light,
                        ApplicationTheme.Dark,
                        ApplicationTheme.HighContrast,
                        ApplicationTheme.Auto,
                    ];

                    for (int i = 0; i < themes.Length; i++)
                    {
                        ApplicationThemeManager.Apply(themes[i], WindowBackdropType.None);
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        nav.UpdateLayout();

                        Assert.Equal(themes[i], ApplicationThemeManager.CurrentTheme);
                        Assert.True(nav.IsLoaded,
                            "NavigationView should remain loaded after a theme change.");
                    }
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneModeSwitch_IndicatorSurvivesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneCollapse_IndicatorSurvivesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);

                    nav.IsPaneOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_DisabledState_ChangesForegroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    NavigationViewItem item = new() { Content = "Disabled" };
                    _ = nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Brush enabledForeground = item.Foreground;

                    item.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Brush disabledForeground = item.Foreground;
                    Assert.NotEqual(enabledForeground, disabledForeground);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_PaneClosedInitially_ContentStartsAt48px_InlineAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = false,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(48.0, offset.X, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_ContentStarts42pxBelowWindowTopAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(42.0, offset.Y, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_HeaderContentUsesAutoHeightAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                        Header = new System.Windows.Controls.Border { Width = 100, Height = 20 },
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(20.0, offset.Y, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_PaneToggle_ResizesPushingContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 320.0).ConfigureAwait(true);

                    nav.IsPaneOpen = false;
                    Assert.True(nav.GetPaneColumnWidthForTesting() > 48.0, "Closing Left mode should animate from the expanded width instead of snapping immediately to 48.");
                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 48.0).ConfigureAwait(true);

                    nav.IsPaneOpen = true;
                    Assert.True(nav.GetPaneColumnWidthForTesting() < 320.0, "Opening Left mode should animate from the compact width instead of snapping immediately to 320.");
                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 320.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // Switching pane display mode between Left and LeftCompact animates the pane width with the
        // same GridLength flight as the collapse/expand toggle, instead of snapping.
        [Fact]
        public Task NavigationView_PaneDisplayModeChange_AnimatesPaneWidth_LeftAndLeftCompactAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    // Settle until the open pane reaches the asserted 320px expanded width.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(nav.GetPaneColumnWidthForTesting() - 320.0) <= 0.5).ConfigureAwait(true);
                    window.UpdateLayout();
                    Assert.Equal(320.0, nav.GetPaneColumnWidthForTesting(), 0.5);

                    // Left -> LeftCompact: the control coerces IsPaneOpen=false; the pane width must
                    // animate down rather than snap straight to 48 (the bug the mode-change handler had).
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(nav.GetPaneColumnWidthForTesting() > 48.0, "Switching Left -> LeftCompact should animate the pane width, not snap immediately to 48.");
                    _ = await WaitUntilAsync(window.Dispatcher, 600, () => nav.GetPaneColumnWidthForTesting() <= 48.5).ConfigureAwait(true);
                    Assert.Equal(48.0, nav.GetPaneColumnWidthForTesting(), 0.5);

                    // LeftCompact -> Left, reopened the way an app does it (open the pane, then switch
                    // mode): the pane width must animate back up rather than snap to 320.
                    nav.IsPaneOpen = true;
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(nav.GetPaneColumnWidthForTesting() < 320.0, "Switching LeftCompact -> Left (reopened) should animate the pane width, not snap immediately to 320.");
                    _ = await WaitUntilAsync(window.Dispatcher, 600, () => nav.GetPaneColumnWidthForTesting() >= 319.5).ConfigureAwait(true);
                    Assert.Equal(320.0, nav.GetPaneColumnWidthForTesting(), 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // LeftCompact pane still resizes inline and pushes sibling content.
        [Fact]
        public Task NavigationView_LeftCompact_PaneOpen_ContentStartsAt320px_InlineAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Pane-open enter animation is 167 ms (CubicEase EaseOut). Settle until the pane
                    // reaches its 320px open width rather than padding past HoldEnd.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(nav.GetPaneColumnWidthForTesting() - 320.0) <= 0.5).ConfigureAwait(true);
                    window.UpdateLayout();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 320.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_HeaderContentUsesAutoHeightAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true,
                        Header = new System.Windows.Controls.Border { Width = 100, Height = 20 },
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 300).ConfigureAwait(true);
                    window.UpdateLayout();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(20.0, offset.Y, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_PaneClosed_ContentStartsAt48px_InlineAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 48.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_BackEnabledClosedPane_KeepsPaneToggleVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true,
                        IsBackButtonVisible = true,
                        IsBackEnabled = true,
                        IsPaneToggleButtonVisible = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_BackButton, nav));
                    System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_PaneToggleButton, nav));
                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);
                    Assert.Equal(Visibility.Visible, back.Visibility);
                    Assert.Equal(Visibility.Visible, paneToggle.Visibility);
                    // The back button takes the first chrome slot and pushes the toggle to 48,
                    // so the chrome row runs wider than the rail it sits above.
                    Assert.Equal(48.0, paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0)).X, 1.0);

                    // The rail itself stays compact: the content starts at 48, not at the 96 the
                    // chrome row occupies. Widening the rail for the back button made enabling it
                    // look like the pane had been opened.
                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 48.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_PaneToggle_ResizesPushingContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Pane enter animation is 167 ms (CubicEase). Settle until the pane reaches its
                    // 320px open width before sampling layout, rather than padding past HoldEnd.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(nav.GetPaneColumnWidthForTesting() - 320.0) <= 0.5).ConfigureAwait(true);
                    window.UpdateLayout();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PART_ContentPresenter), exactMatch: false);

                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 320.0).ConfigureAwait(true);

                    nav.IsPaneOpen = false;
                    Assert.True(nav.GetPaneColumnWidthForTesting() > 48.0, "Closing LeftCompact should animate from the current expanded width instead of snapping immediately to 48.");
                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 48.0).ConfigureAwait(true);

                    nav.IsPaneOpen = true;
                    Assert.True(nav.GetPaneColumnWidthForTesting() < 320.0, "Opening LeftCompact should animate from the current compact width instead of snapping immediately to 320.");
                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 320.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // NavigationView.ContentBackground must default to NavigationViewContentBackgroundBrush
        // (semi-transparent tint that allows Mica/Acrylic backdrop to show through the content area).
        [Fact]
        public Task NavigationView_ContentBackground_DefaultStyle_ResolvesToSolidBackgroundFillColorBaseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SolidColorBrush expected = Assert.IsType<SolidColorBrush>(application.TryFindResource("NavigationViewContentBackgroundBrush"));
                    SolidColorBrush actual = Assert.IsType<SolidColorBrush>(nav.ContentBackground);

                    Assert.Equal(expected.Color, actual.Color);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // WI-1 F3 supporting guard: NavigationViewItemHeader must be a first-class pane child
        // (placed via Items), styled distinctly from NavigationViewItem, and not selectable.
        [Fact]
        public Task NavigationView_Header_InPane_IsRendered_NotSelectableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    NavigationViewItemHeader header = new() { Content = "Input" };
                    NavigationViewItem item = new() { Content = "Buttons" };
                    _ = nav.Items.Add(header);
                    _ = nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    NavigationViewItemHeader renderedHeader = Assert.IsType<NavigationViewItemHeader>(FindVisualChild<NavigationViewItemHeader>(nav), exactMatch: false);
                    Assert.False(renderedHeader.Focusable, "Header must not be focusable.");
                    Assert.Null(nav.SelectedItem);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-3 B15  NavigationView pane header LayerFillColorAltBrush + BackButtonStates VSM
        // ---------------------------------------------------------------------------

        [Fact]
        public Task NavigationView_BackButtonStates_BothStatesAccessibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new() { Width = 700, Height = 500 };
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // WI-3 B15: BackButtonStates VSM group must expose both states
                    bool okVisible = VisualStateManager.GoToState(nav, "BackButtonVisible", useTransitions: false);
                    bool okCollapsed = VisualStateManager.GoToState(nav, "BackButtonCollapsed", useTransitions: false);

                    Assert.True(okVisible, "GoToState('BackButtonVisible') must succeed - BackButtonStates VSM group required.");
                    Assert.True(okCollapsed, "GoToState('BackButtonCollapsed') must succeed.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_IsBackButtonVisible_True_ShowsBackButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new() { Width = 700, Height = 500, IsBackButtonVisible = true };
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PART_BackButton, nav));
                    Assert.Equal(Visibility.Visible, back.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // NavigationView_CompactPane_BackgroundIsLayerFillColorAlt REMOVED (WI-3 B15 revert).
        // Replaced by NavigationView_PaneBorders_AreTransparent below.

        // NavigationView.ContentBackground must resolve to NavigationViewContentBackgroundBrush
        // across all themes (semi-transparent tint; color changes per theme file).
        [Fact]
        public Task NavigationView_ContentBackground_ResolvesToSolidBackgroundFillColorBaseBrush_AcrossThemesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 640,
                        Height = 400,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.NotNull(nav.ContentBackground);
                    Assert.NotNull(application.TryFindResource("NavigationViewContentBackgroundBrush"));

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.NotNull(nav.ContentBackground);
                    Assert.NotNull(application.TryFindResource("NavigationViewContentBackgroundBrush"));

                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.NotNull(nav.ContentBackground);
                    Assert.NotNull(application.TryFindResource("NavigationViewContentBackgroundBrush"));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // NavigationView_Left_PaneBorder_UsesLayerFillColorAltBrush REMOVED (WI-3 B15 revert).
        // NavigationView_LeftCompact_PaneBorder_UsesLayerFillColorAltBrush REMOVED (WI-3 B15 revert).
        // Both replaced by NavigationView_PaneBorders_AreTransparent below.

        [Fact]
        public Task NavigationView_PaneBorders_AreTransparentAsync()
        {
            // Regression guard: pane borders (PaneBorder, CompactPane, PaneHeaderBorder) must
            // be Transparent (or null) so the DWM Mica/Acrylic backdrop shows through. The
            // WI-3 B15 commit wrongly set them to LayerFillColorAltBrush, which blocked the
            // backdrop entirely. This test asserts the reverted state is preserved.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // ---- Left pane ----
                Window winLeft = new();
                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    winLeft.Content = nav;
                    winLeft.Show();
                    WpfTestSta.DrainDispatcher(winLeft.Dispatcher);
                    winLeft.UpdateLayout();

                    System.Windows.Controls.Border paneBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneBorder"), exactMatch: false);
                    AssertBrushIsTransparent(paneBorder.Background,
                        "PaneBorder.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winLeft);
                }

                // ---- LeftCompact pane ----
                Window winCompact = new();
                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    winCompact.Content = nav;
                    winCompact.Show();
                    WpfTestSta.DrainDispatcher(winCompact.Dispatcher);
                    winCompact.UpdateLayout();

                    System.Windows.Controls.Border compactPane = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "CompactPane"), exactMatch: false);
                    AssertBrushIsTransparent(compactPane.Background,
                        "CompactPane.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winCompact);
                }

                // ---- Top pane ----
                Window winTop = new();
                try
                {
                    NavigationView nav = new()
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    winTop.Content = nav;
                    winTop.Show();
                    WpfTestSta.DrainDispatcher(winTop.Dispatcher);
                    winTop.UpdateLayout();

                    System.Windows.Controls.Border paneHeader = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneHeaderBorder"), exactMatch: false);
                    AssertBrushIsTransparent(paneHeader.Background,
                        "PaneHeaderBorder.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winTop);
                }
            });
        }

        /// <summary>
        /// Asserts that <paramref name="brush"/> is Brushes.Transparent or a SolidColorBrush
        /// whose alpha channel is zero - i.e. effectively transparent. A null brush fails: an
        /// unresolved <c language="xaml">DynamicResource</c> also renders as no background, so treating null as
        /// a pass would hide a broken binding behind the same visual result as a correct one.
        /// </summary>
        /// <param name="brush">The brush to check for transparency.</param>
        /// <param name="message">The message to display if the assertion fails, naming the element under test.</param>
        private static void AssertBrushIsTransparent(Brush brush, string message)
        {
            if (brush is null)
            {
                Assert.Fail(message + " Actual: null (unresolved DynamicResource, not a deliberate transparent brush).");
            }

            if (brush == Brushes.Transparent)
            {
                return;
            }

            if (brush is SolidColorBrush solid && solid.Color.A is 0)
            {
                return;
            }

            Assert.Fail(message + " Actual: " + brush);
        }

        private static void AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode mode, bool isPaneOpen)
        {
            Application application = WpfTestSta.EnsureApplication();
            Style expected = Assert.IsType<Style>(application.TryFindResource("ScrollViewerStyle"));

            Window window = new();
            try
            {
                NavigationView nav = new()
                {
                    Width = 640,
                    Height = 420,
                    PaneDisplayMode = mode,
                    IsPaneOpen = isPaneOpen,
                };
                _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });

                window.Content = nav;
                window.Show();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(FindVisualChildByName<ScrollViewer>(nav, NavigationView.PART_PaneItemsScrollViewer), exactMatch: false);
                _ = Assert.IsType<SmoothScrollViewer>(scrollViewer, exactMatch: false);
                Assert.Same(expected, scrollViewer.Style);
            }
            finally
            {
                CloseWindowAndDrain(window);
            }
        }

        private static TranslateTransform GetSelectionIndicatorTranslate(FrameworkElement indicator)
        {
            TransformGroup group = Assert.IsType<TransformGroup>(indicator.RenderTransform);
            Assert.True(group.Children.Count >= 2, "Selection indicator TransformGroup must contain scale and translate transforms.");
            return Assert.IsType<TranslateTransform>(group.Children[1]);
        }

        private static ScaleTransform GetSelectionIndicatorScale(FrameworkElement indicator)
        {
            TransformGroup group = Assert.IsType<TransformGroup>(indicator.RenderTransform);
            Assert.True(group.Children.Count >= 2, "Selection indicator TransformGroup must contain scale and translate transforms.");
            return Assert.IsType<ScaleTransform>(group.Children[0]);
        }

        [Fact]
        public Task NavigationViewItem_Template_HasNoInnerSelectionIndicatorAsync()
        {
            // Regression: per-item Border named "SelectionIndicator" was duplicating the
            // pane-level PART_SelectionIndicator (animated by NavigationView code-behind),
            // producing two visible accent pills on the selected item. The pane-level
            // indicator is canonical (WinUI 3) and is wired in NavigationView.cs; the
            // per-item one must NOT exist in the template.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationViewItem item = new()
                    {
                        Content = "Item",
                        IsSelected = true,
                    };
                    window.Content = item;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border? inner = FindVisualChildByName<System.Windows.Controls.Border>(item, "SelectionIndicator");
                    Assert.Null(inner);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        /// <summary>
        /// Builds a NavigationView with a Left/Top-Home/Docs main pane and one footer item,
        /// shared by <see cref="NavigationViewTests"/> and <see cref="NavigationViewTopModeTests"/>.
        /// </summary>
        /// <param name="footer">Receives the created footer item.</param>
        /// <param name="mode">The pane display mode to apply.</param>
        /// <param name="isPaneOpen">Whether the pane starts open.</param>
        /// <returns>The constructed navigation view.</returns>
        internal static NavigationView CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode mode, bool isPaneOpen)
        {
            footer = new NavigationViewItem
            {
                Content = "Settings",
                Icon = new FontIcon { Glyph = "\uE713", IconFontSize = 16 },
            };
            NavigationView nav = new()
            {
                Width = 600,
                Height = 400,
                PaneDisplayMode = mode,
                IsPaneOpen = isPaneOpen,
            };
            _ = nav.Items.Add(new NavigationViewItem { Content = "Home", Icon = new FontIcon { Glyph = "\uE80F" } });
            _ = nav.Items.Add(new NavigationViewItem { Content = "Docs", Icon = new FontIcon { Glyph = "\uE8A5" } });
            nav.FooterMenuItems.Add(footer);
            return nav;
        }

        [Fact]
        public Task NavigationView_FooterItem_ResolvesOwningNavigationViewAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // The fix that lets footer-hosted items invoke: an item resolves its owning
                    // NavigationView by ancestor walk, not via ItemsControlFromItemContainer.
                    Assert.Same(nav, NavigationView.FromItemContainer(footer));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_FooterItem_Invoke_SelectsAndClearsMainSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    nav.SelectedIndex = 0;
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    List<NavigationViewItem> invoked = [];
                    nav.ItemInvoked += (_, e) => invoked.Add(e.InvokedItemContainer);

                    nav.SelectFooterMenuItem(footer);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Same(footer, nav.SelectedFooterItem);
                    Assert.True(footer.IsSelected, "The invoked footer item should be marked selected.");
                    Assert.Null(nav.SelectedItem);
                    _ = Assert.Single(invoked);
                    Assert.Same(footer, invoked[0]);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_MainSelection_ClearsFooterSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectFooterMenuItem(footer);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(footer, nav.SelectedFooterItem);

                    nav.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Null(nav.SelectedFooterItem);
                    Assert.False(footer.IsSelected, "The footer item should be deselected when a main item is selected.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_FooterSelectionIndicator_BecomesVisibleOnFooterSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement footerIndicator = Assert.IsType<FrameworkElement>(nav.GetFooterSelectionIndicatorForTesting(), exactMatch: false);
                    Assert.Equal(0.0, footerIndicator!.Opacity, 0.01);

                    nav.SelectFooterMenuItem(footer);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // The indicator now fades in over 140 ms in every pane mode, so the assertion
                    // samples opacity once that flight has landed rather than on the frame the
                    // selection was made.
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 300).ConfigureAwait(true);

                    Assert.True(footerIndicator.Opacity >= 0.9, "Selecting a footer item should reveal the footer selection indicator.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_FooterItem_StretchesToPaneWidth_InLeftOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();
                try
                {
                    window.Content = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    // Settle until the footer item has stretched to the asserted pane width.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => { window.UpdateLayout(); return footer.ActualWidth > 200.0; }).ConfigureAwait(true);
                    window.UpdateLayout();

                    // The footer item lives in a stretching StackPanel, so its hover/selection surface
                    // spans the pane width rather than the "Settings" text width (the original bug).
                    Assert.True(footer.ActualWidth > 200.0, "An open Left pane footer item should stretch to the pane width, not the content width. Measured: " + footer.ActualWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_FooterItem_IconCentered_InLeftCompactClosedAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();
                try
                {
                    window.Content = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.LeftCompact, isPaneOpen: false);
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 300).ConfigureAwait(true);
                    window.UpdateLayout();

                    ContentPresenter iconPresenter = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(footer, "IconPresenter"), exactMatch: false);
                    Point iconOffset = iconPresenter!.TransformToAncestor(footer).Transform(new Point(0, 0));
                    Assert.True(iconOffset.X >= 4.0 - 0.5, "Closed LeftCompact footer icon should not be clipped on the left edge.");
                    Assert.True(iconOffset.X + iconPresenter.ActualWidth <= 44.0 + 0.5, "Closed LeftCompact footer icon should stay inside the 40px icon slot, aligned with the main items.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Automation_FooterSupportsSelectionAndInvocationAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                Window window = new() { Content = nav };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    // Ask UIA for the window root so provider conversion does not depend on
                    // an external accessibility client having already connected the peer tree.
                    IntPtr windowHandle = new WindowInteropHelper(window).Handle;
                    Task<AutomationElement> rootTask = Task.Run(() => AutomationElement.FromHandle(windowHandle), TestContext.Current.CancellationToken);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 5000, () => rootTask.IsCompleted).ConfigureAwait(true), "UI Automation did not connect the window root.");
                    AutomationElement root = await rootTask.ConfigureAwait(true);
                    Assert.NotNull(root);
                    NavigationViewItemAutomationPeer peer = Assert.IsType<NavigationViewItemAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(footer));
                    int invocationCount = 0;
                    nav.ItemInvoked += (_, _) => invocationCount++;
                    IRawElementProviderSimple? selectionContainer = peer.SelectionContainer;
                    Assert.NotNull(selectionContainer);
                    Assert.Equal("NavigationView", selectionContainer.GetPropertyValue(AutomationElementIdentifiers.ClassNameProperty.Id));
                    peer.SelectItem();
                    Assert.Same(footer, nav.SelectedFooterItem);
                    Assert.True(peer.IsSelected);
                    Assert.Equal(0, invocationCount);
                    peer.Invoke();
                    Assert.Equal(1, invocationCount);
                    Assert.Same(footer, nav.SelectedFooterItem);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Automation_GetSelection_ReportsFooterSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ISelectionProvider selectionProvider = (NavigationViewAutomationPeer)new(nav);
                    Assert.Empty(selectionProvider.GetSelection());

                    nav.SelectFooterMenuItem(footer);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    _ = Assert.Single(selectionProvider.GetSelection());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_AfterUnloadReload_SelectionIndicatorStillUpdatesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new();
                ContentControl host = new();
                window.Content = host;

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    NavigationViewItem home = new() { Content = "Home", Icon = new FontIcon { Glyph = "\uE80F" } };
                    NavigationViewItem files = new() { Content = "Files", Icon = new FontIcon { Glyph = "\uE8B7" } };
                    _ = nav.Items.Add(home);
                    _ = nav.Items.Add(files);

                    host.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    nav.SelectedItem = home;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 400).ConfigureAwait(true);

                    // Simulate navigating away from the cached page and back: the NavigationView is
                    // unloaded (template parts nulled) and reloaded against the same instance.
                    host.Content = null;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    host.Content = nav;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 400).ConfigureAwait(true);

                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);
                    double homeY = GetSelectionIndicatorTranslate(indicator).Y;

                    nav.InvokeItem(files);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Settle until the indicator slide animation has reached its hold-end (no longer
                    // animating), so the sampled filesY is the final settled offset.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => !GetSelectionIndicatorTranslate(indicator).HasAnimatedProperties).ConfigureAwait(true);

                    Assert.Same(files, nav.SelectedItem);
                    double filesY = GetSelectionIndicatorTranslate(indicator).Y;
                    Assert.NotEqual(homeY, filesY, 0.5);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_Left_OuterBorder_MatchesWinUiPillGeometryAsync()
        {
            // Regression: OuterBorder used to carry Margin="4,2,0,2" with BorderThickness="2" inside
            // the item's MinHeight="36", which lost 4 dip of painted height to the margin and 2 dip of
            // left inset to the invisible (BorderBrush-less) border band, while the 0 right margin left
            // the pill off-centre in the pane. WinUI NavigationViewItemButtonMargin = 4,2 and
            // NavigationViewItemOnLeftMinHeight = 36 require a symmetric 4,2,4,2 margin, no border
            // thickness, and a 36 dip painted pill (with the 2+2 dip margin keeping the 40 dip pitch).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 320,
                        Height = 200,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                    };
                    NavigationViewItem item = new() { Content = "Item" };
                    _ = nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(item, "OuterBorder"), exactMatch: false);

                    Assert.Equal(new Thickness(4, 2, 4, 2), outerBorder.Margin);
                    Assert.Equal(new Thickness(0), outerBorder.BorderThickness);
                    Assert.Equal(36.0, outerBorder.ActualHeight, 0.5);
                    Assert.Equal(40.0, item.ActualHeight, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_PaneItemsAndFooterSitSymmetricallyInThePaneAsync()
        {
            // Regression: the open Left pane gave its items scroller Padding="0,4,8,4" and its
            // footer Padding="0,0,8,0", a right-only gutter reserving room for the overlay
            // scrollbar. Every item then sat 4 dip from the pane's left edge (its own
            // NavigationViewItemButtonMargin) but 12 from the right, which reads as an off-centre
            // selection pill. WinUI keeps the pane symmetric and lets the overlay rail ride over
            // the items, so the two gaps have to match. The footer divider is measured too: it
            // spans the pane rather than stopping short at the footer's inset.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 320,
                        Height = 400,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                    };

                    NavigationViewItem item = new() { Content = "Item" };
                    _ = nav.Items.Add(item);
                    nav.FooterMenuItems.Add(new NavigationViewItem { Content = "Settings" });

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border pane = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneBorder"), exactMatch: false);
                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(item, "OuterBorder"), exactMatch: false);

                    double left = GetVisualX(outerBorder, pane);
                    double right = pane.ActualWidth - (left + outerBorder.ActualWidth);
                    Assert.Equal(left, right, 1.0);

                    // The divider reaches both pane edges, so it is not inset by the footer.
                    NavigationViewItemSeparator divider = Assert.IsType<NavigationViewItemSeparator>(
                        FindVisualChild<NavigationViewItemSeparator>(pane), exactMatch: false);
                    Assert.Equal(Visibility.Visible, divider.Visibility);
                    Assert.Equal(0.0, GetVisualX(divider, pane), 1.0);
                    Assert.Equal(pane.ActualWidth, divider.ActualWidth, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_SharedSelectionIndicator_IsThreeDipWideAsync()
        {
            // WinUI NavigationViewSelectionIndicatorWidth = 3 (Height = 16, already correct). Locks the
            // pane-level indicator's geometry so a future edit cannot silently narrow the pill marker.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 320,
                        Height = 200,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                    };
                    NavigationViewItem item = new() { Content = "Item" };
                    _ = nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    FrameworkElement indicator = Assert.IsType<FrameworkElement>(nav.GetSelectionIndicatorForTesting(), exactMatch: false);

                    Assert.Equal(3.0, indicator.Width, 0.01);
                    Assert.Equal(16.0, indicator.Height, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_InfoBadge_StaysOnAClosedPaneAsync()
        {
            // WinUI's ClosedCompactAndTopLevelItem state spans the badge across the item and pins it
            // to the top right corner over the icon (NavigationView_themeresources.xaml:590-594).
            // Fluence collapsed it instead, which is exactly the pane state a badge is for: a rail
            // with no labels, where the badge is the only thing reporting the item's state.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 320,
                        Height = 240,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false,
                    };
                    NavigationViewItem item = new()
                    {
                        Content = "Inbox",
                        Icon = new FontIcon { Glyph = "" },
                        InfoBadge = new InfoBadge { Value = 12 },
                    };
                    _ = nav.Items.Add(item);

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter badgeHost = Assert.IsType<ContentPresenter>(
                        FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, badgeHost.Visibility);
                    Assert.Equal(HorizontalAlignment.Right, badgeHost.HorizontalAlignment);
                    Assert.Equal(VerticalAlignment.Top, badgeHost.VerticalAlignment);

                    InfoBadge badge = Assert.IsType<InfoBadge>(FindVisualChild<InfoBadge>(item), exactMatch: false);
                    Assert.True(badge.ActualWidth > 0, "The badge must render on a closed pane.");

                    // Pinned to the item's own top right corner rather than parked in the label column.
                    Point badgeTopRight = badgeHost.TransformToAncestor(item).Transform(new Point(badgeHost.ActualWidth, 0));
                    Assert.True(
                        badgeTopRight.X <= item.ActualWidth,
                        "The badge must sit inside the rail, not past its right edge.");
                    Assert.True(
                        badgeTopRight.X > item.ActualWidth / 2,
                        "The badge must sit in the right half of the item, over the icon's trailing corner.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Theory]
        [InlineData(NavigationViewPaneDisplayMode.Left)]
        [InlineData(NavigationViewPaneDisplayMode.LeftCompact)]
        public Task NavigationViewItem_ClosedPane_GivesTheIconItsFullColumnAsync(NavigationViewPaneDisplayMode mode)
        {
            // Regression: the open pane's 14 dip content inset (WinUI's ContentGrid margin) was
            // applying on a closed pane too, so the 40 dip icon column was arranged into roughly 26
            // and every glyph on the rail was clipped. WinUI zeroes the same margin in its
            // ClosedCompactAndTopLevelItem state (NavigationView_themeresources.xaml:589).
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 320,
                        Height = 240,
                        PaneDisplayMode = mode,
                        IsPaneOpen = false,
                    };
                    NavigationViewItem item = new()
                    {
                        Content = "Colors",
                        Icon = new FontIcon { Glyph = "" },
                    };
                    _ = nav.Items.Add(item);

                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Grid contentGrid = Assert.IsType<Grid>(FindVisualChildByName<Grid>(item, "ContentGrid"), exactMatch: false);
                    ContentPresenter iconPresenter = Assert.IsType<ContentPresenter>(
                        FindVisualChildByName<ContentPresenter>(item, "IconPresenter"), exactMatch: false);

                    Assert.Equal(new Thickness(0), contentGrid.Margin);

                    // The decisive check: the grid must be arranged at least as wide as the icon
                    // column it wants, or WPF clips it at the layout boundary and takes the glyph's
                    // right edge with it. A 14 dip inset here left 26 for a 40 dip column.
                    Assert.Equal(40.0, contentGrid.ColumnDefinitions[0].ActualWidth, 0.5);
                    Assert.True(
                        contentGrid.ActualWidth >= contentGrid.DesiredSize.Width - 0.5,
                        "The item's content grid must not be arranged narrower than it measured, or the icon is clipped.");

                    // And the glyph itself lands inside the rail.
                    Point iconRight = iconPresenter.TransformToAncestor(item).Transform(new Point(iconPresenter.ActualWidth, 0));
                    Assert.True(
                        iconRight.X <= item.ActualWidth + 0.5,
                        "The icon must fit inside the item on a closed pane.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
