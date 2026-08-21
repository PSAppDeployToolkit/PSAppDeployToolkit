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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        private static void CloseWindowAndDrain(Window window)
        {
            window.Content = null;
            window.UpdateLayout();
            window.Close();
            WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
        }

        // Pump the dispatcher for `milliseconds` so any in-flight storyboard
        // (e.g. the LeftCompact pane's 167 ms Width animation) reaches its
        // HoldEnd state before the test samples layout values.
        private static async Task WaitForAnimationAndDrainAsync(Dispatcher dispatcher, int milliseconds)
        {
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(
                TimeSpan.FromMilliseconds(milliseconds),
                DispatcherPriority.Normal,
                delegate { frame.Continue = false; },
                dispatcher);
            timer.Start();
            Dispatcher.PushFrame(frame);
            timer.Stop();
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
        }

        private static async Task<bool> WaitUntilAsync(Dispatcher dispatcher, int milliseconds, Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            do
            {
                await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                if (condition())
                {
                    return true;
                }

                DispatcherFrame frame = new();
                DispatcherTimer timer = new(
                    TimeSpan.FromMilliseconds(16),
                    DispatcherPriority.Normal,
                    delegate { frame.Continue = false; },
                    dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }
            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested);

            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            return condition();
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
            System.Windows.Controls.Button paneToggle = Assert.IsAssignableFrom<System.Windows.Controls.Button>(
                nav.Template.FindName(NavigationView.PartPaneToggleButton, nav));
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
                Application application = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.Mica, updateAccent: true);

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

                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindVisualChildByName<NavigationView>(mw, "DemoNav"));
                    NavigationViewItem footer = Assert.IsType<NavigationViewItem>(nav.FooterMenuItems.Count > 0 ? nav.FooterMenuItems[0] as NavigationViewItem : null);
                    ContentPresenter footerIcon = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(footer!, "IconPresenter"));

                    nav.IsPaneOpen = false;
                    await WaitForAnimationAndDrainAsync(mw.Dispatcher, 300).ConfigureAwait(true);
                    mw.UpdateLayout();

                    ColumnDefinition paneColumn = Assert.IsType<ColumnDefinition>(nav.Template.FindName("PaneColumn", nav));

                    foreach (double width in new[] { 96.0, 160.0, 240.0, 320.0 })
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
        public Task NavigationView_PaneDisplayMode_Left_RendersVerticalPaneAsync()
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

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(GetNavigationViewItemsHostPanel(nav));
                    Assert.Equal(Orientation.Vertical, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneDisplayMode_Top_RendersHorizontalPaneAsync()
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

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(GetNavigationViewItemsHostPanel(nav));
                    Assert.Equal(Orientation.Horizontal, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneItemsScrollViewer_UsesFluentScrollViewerStyleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.Left, isPaneOpen: true);
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.LeftCompact, isPaneOpen: false);
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_ClosedPaneKeepsIconFooterVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    System.Windows.Controls.Border footerHost = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneFooterHost"));
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftClosedPaneItemsKeepFullIconWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter iconPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(messages, "IconPresenter"));
                    Point iconOffset = iconPresenter.TransformToAncestor(messages).Transform(new Point(0, 0));
                    Assert.True(iconOffset.X >= 4.0 - 0.5, "Closed Left icon should not be clipped on the left edge.");
                    Assert.True(iconOffset.X + iconPresenter.ActualWidth <= 44.0 + 0.5, "Closed Left icon should stay inside the 40px icon slot.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_ClosedPaneItemsKeepFullIconWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter iconPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(messages, "IconPresenter"));
                    Point iconOffset = iconPresenter.TransformToAncestor(messages).Transform(new Point(0, 0));
                    Assert.True(iconOffset.X >= 4.0 - 0.5, "Closed LeftCompact icon should not be clipped on the left edge.");
                    Assert.True(iconOffset.X + iconPresenter.ActualWidth <= 44.0 + 0.5, "Closed LeftCompact icon should stay inside the 40px icon slot.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftPaneToggleGlyph_IsOffsetToAlignWithItemIconsAsync()
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
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FontIcon glyph = Assert.IsAssignableFrom<FontIcon>(FindVisualChildByName<FontIcon>(nav, "PaneToggleGlyph"));
                    Assert.Equal(2.0, glyph.Margin.Left, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftChrome_BackPrecedesPaneToggleAsync()
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

                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartBackButton, nav));
                    System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartPaneToggleButton, nav));

                    System.Windows.Controls.StackPanel chrome = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(nav, "PaneChrome"));
                    Assert.Equal(Orientation.Horizontal, chrome.Orientation);
                    Assert.Equal(48.0, back.ActualWidth, 0.5);
                    Assert.Equal(48.0, paneToggle.ActualWidth, 0.5);

                    Point backPoint = back.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Point paneTogglePoint = paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.True(backPoint.X < paneTogglePoint.X, "Back button should be the first glyph, before the pane toggle.");
                    Assert.Equal(backPoint.Y, paneTogglePoint.Y, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_DefaultFontIconSizeIs16Async()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_Template_RendersInfoBadgeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter"));
                    Assert.Same(badge, presenter.Content);
                    Assert.True(double.IsNaN(presenter.Width) || presenter.Width >= 34.0,
                        "NavigationViewItem must not constrain InfoBadge value pills to the old 24px slot.");
                    Assert.Equal(HorizontalAlignment.Center, presenter.HorizontalAlignment);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_SelectedItem_UpdatesOnItemClickAsync()
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_ItemInvoked_FiresBeforeSelectionChangesAsync()
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

                    AutomationPeer peer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(item1));
                    IInvokeProvider invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));

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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_SelectionFollowsFocus_True_SelectsOnFocusAsync()
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
                    FrameworkElement container1 = Assert.IsAssignableFrom<FrameworkElement>(nav.ItemContainerGenerator.ContainerFromIndex(1));
                    _ = Keyboard.Focus(container1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_SelectionFollowsFocus_False_DoesNotSelectOnFocusAsync()
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
                    FrameworkElement container1 = Assert.IsAssignableFrom<FrameworkElement>(nav.ItemContainerGenerator.ContainerFromIndex(1));
                    _ = Keyboard.Focus(container1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(0, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_IsBackButtonVisible_False_HidesBackButtonAsync()
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
                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartBackButton, nav));
                    Assert.Equal(Visibility.Collapsed, back.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_IsBackEnabled_False_CollapsesBackButtonAsync()
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
                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartBackButton, nav));
                    System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartPaneToggleButton, nav));
                    Assert.Equal(Visibility.Collapsed, back.Visibility);
                    Assert.Equal(0.0, paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0)).X, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftModes_ForcePaneToggleVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
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
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_BackRequested_FiresOnBackClickAsync()
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_ThemeSwitch_UpdatesBrushesAsync()
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
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(application.Resources.MergedDictionaries.Count > 0);
                    Color lightBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Color darkBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    Assert.NotEqual(lightBase, darkBase);
                    nav.UpdateLayout();
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_SharedIndicator_ExistsInTemplate_AndVisibleWhenSelectedAsync()
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
                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_PreTemplateSelection_PositionsSharedIndicatorAfterTemplateAppliedAsync()
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_SharedIndicator_TracksHorizontalItemPlacementAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
                    double iconItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.Equal(9.0, iconItemX, 0.5);

                    nav.SelectedIndex = 1;
                    // Settle until the indicator slide reaches the asserted child-item offset.
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(GetSelectionIndicatorTranslate(indicator).X - 53.0) <= 0.5).ConfigureAwait(true);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    double childItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.Equal(53.0, childItemX, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_SharedIndicator_AnimatesBetweenSelectionsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_RapidReselection_IndicatorSettlesOnFinalTargetAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_IndicatorExitsVerticallyBeforeChangingParentChildIndentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                        Content = "Parent",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 },
                    });
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Child",
                        IsChildItem = true,
                    });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
                    TranslateTransform translate = GetSelectionIndicatorTranslate(indicator);
                    double parentX = translate.X;
                    double parentY = translate.Y;
                    NavigationViewItem parentItem = Assert.IsType<NavigationViewItem>(nav.Items[0]);
                    Point departPosition = nav.CalculateDepartPositionForTesting(
                        new Point(parentX, parentY),
                        parentItem,
topMode: false,
                        1.0);
                    Assert.Equal(parentX, departPosition.X, 0.5);
                    Assert.True(departPosition.Y > parentY, "The downward depart leg should move below the parent before the child inset X is applied.");

                    nav.SelectedIndex = 1;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, delegate
                        {
                            return Math.Abs(translate.X - 53.0) <= 0.5 && Math.Abs(indicator.Opacity - 1.0) <= 0.01;
                        }).ConfigureAwait(true),
                        "After the depart/arrive animation completes, the child item indicator should become visible at the child inset.");
                    Assert.Equal(53.0, translate.X, 0.5);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_IndicatorExitsUpwardWhenNewSelectionIsAboveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                        Content = "Parent",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 },
                    });
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Child",
                        IsChildItem = true,
                    });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
                    TranslateTransform translate = GetSelectionIndicatorTranslate(indicator);
                    double childX = translate.X;
                    double childY = translate.Y;
                    NavigationViewItem childItem = Assert.IsType<NavigationViewItem>(nav.Items[1]);
                    Point departPosition = nav.CalculateDepartPositionForTesting(
                        new Point(childX, childY),
                        childItem,
topMode: false,
                        -1.0);
                    Assert.Equal(childX, departPosition.X, 0.5);
                    Assert.True(departPosition.Y < childY, "The upward depart leg should move above the child before the parent X is applied.");

                    nav.SelectedIndex = 0;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, delegate
                        {
                            return Math.Abs(translate.X - 9.0) <= 0.5 && Math.Abs(indicator.Opacity - 1.0) <= 0.01;
                        }).ConfigureAwait(true),
                        "After the depart/arrive animation completes, the parent item indicator should become visible at the parent inset.");
                    Assert.Equal(9.0, translate.X, 0.5);
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftMode_TopLevelIconlessItem_DoesNotUseChildIndicatorIndentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_FocusVisual_StaysInsideItemBoundsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    Style style = Assert.IsType<Style>(application.TryFindResource("NavigationViewItemFocusVisual"));
                    ControlTemplate template = Assert.IsType<ControlTemplate>(style.Setters.OfType<Setter>().FirstOrDefault(static setter => setter.Property == Control.TemplateProperty)?.Value as ControlTemplate);
                    DependencyObject root = Assert.IsAssignableFrom<DependencyObject>(template.LoadContent());

                    foreach (System.Windows.Controls.Border border in FindVisualChildren<System.Windows.Controls.Border>(root))
                    {
                        Assert.True(border.Margin.Left >= 0.0 && border.Margin.Right >= 0.0,
                            "Navigation item focus strokes should stay inside the selected item bounds horizontally.");
                    }
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_SharedIndicator_HidesWhenSelectionClearedAsync()
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_TopMode_SharedIndicator_VisibleWhenSelectedAsync()
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_FullThemeCycle_NoExceptionsAsync()
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
                        ApplicationThemeManager.Apply(themes[i], BackdropType.None, updateAccent: true);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneModeSwitch_IndicatorSurvivesAsync()
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
                    Assert.Equal(1.0, indicator.Opacity, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_PaneCollapse_IndicatorSurvivesAsync()
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

                    FrameworkElement indicator = Assert.IsAssignableFrom<FrameworkElement>(nav.GetSelectionIndicatorForTesting());
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_DisabledState_ChangesForegroundAsync()
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_PaneClosedInitially_ContentStartsAt48px_InlineAsync()
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(48.0, offset.X, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_ContentStarts42pxBelowWindowTopAsync()
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(42.0, offset.Y, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_HeaderContentUsesAutoHeightAsync()
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(20.0, offset.Y, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_PaneToggle_ResizesPushingContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // LeftCompact pane still resizes inline and pushes sibling content.
        [Fact]
        public Task NavigationView_LeftCompact_PaneOpen_ContentStartsAt320px_InlineAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 320.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_HeaderContentUsesAutoHeightAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.Equal(20.0, offset.Y, 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_PaneClosed_ContentStartsAt48px_InlineAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 48.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_BackEnabledClosedPane_KeepsPaneToggleVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartBackButton, nav));
                    System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartPaneToggleButton, nav));
                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));
                    Assert.Equal(Visibility.Visible, back.Visibility);
                    Assert.Equal(Visibility.Visible, paneToggle.Visibility);
                    Assert.Equal(48.0, paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0)).X, 1.0);
                    await AssertContentOffsetEventuallyAsync(window, nav, presenter, 96.0).ConfigureAwait(true);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_PaneToggle_ResizesPushingContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter));

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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    NavigationViewItemHeader renderedHeader = Assert.IsAssignableFrom<NavigationViewItemHeader>(FindVisualChild<NavigationViewItemHeader>(nav));
                    Assert.False(renderedHeader.Focusable, "Header must not be focusable.");
                    Assert.Null(nav.SelectedItem);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_IsBackButtonVisible_True_ShowsBackButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new() { Width = 700, Height = 500, IsBackButtonVisible = true };
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.Button back = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartBackButton, nav));
                    Assert.Equal(Visibility.Visible, back.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.NotNull(nav.ContentBackground);
                    Assert.NotNull(application.TryFindResource("NavigationViewContentBackgroundBrush"));

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

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

                    System.Windows.Controls.Border paneBorder = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneBorder"));
                    AssertBrushIsTransparentOrNull(paneBorder.Background,
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

                    System.Windows.Controls.Border compactPane = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "CompactPane"));
                    AssertBrushIsTransparentOrNull(compactPane.Background,
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

                    System.Windows.Controls.Border paneHeader = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneHeaderBorder"));
                    AssertBrushIsTransparentOrNull(paneHeader.Background,
                        "PaneHeaderBorder.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winTop);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        /// <summary>
        /// Asserts that <paramref name="brush"/> is null, Brushes.Transparent, or a
        /// SolidColorBrush whose alpha channel is zero - i.e. effectively transparent.
        /// </summary>
        /// <param name="brush">The brush to check for transparency.</param>
        /// <param name="message">The message to display if the assertion fails.</param>
        private static void AssertBrushIsTransparentOrNull(Brush brush, string message)
        {
            if (brush is null)
            {
                return; // null == no background == transparent
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

                ScrollViewer scrollViewer = Assert.IsAssignableFrom<ScrollViewer>(FindVisualChildByName<ScrollViewer>(nav, NavigationView.PartPaneItemsScrollViewer));
                _ = Assert.IsAssignableFrom<SmoothScrollViewer>(scrollViewer);
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
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
