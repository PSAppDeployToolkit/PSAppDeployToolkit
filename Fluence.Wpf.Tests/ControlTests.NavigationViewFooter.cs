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

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        private static NavigationView CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode mode, bool isPaneOpen)
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
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_FooterItem_Invoke_SelectsAndClearsMainSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_MainSelection_ClearsFooterSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_FooterSelectionIndicator_BecomesVisibleOnFooterSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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

                    Assert.True(footerIndicator.Opacity >= 0.9, "Selecting a footer item should reveal the footer selection indicator.");
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
        public Task NavigationView_FooterItem_StretchesToPaneWidth_InLeftOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_FooterItem_IconCentered_InLeftCompactClosedAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public Task NavigationView_Automation_GetSelection_ReportsFooterSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
