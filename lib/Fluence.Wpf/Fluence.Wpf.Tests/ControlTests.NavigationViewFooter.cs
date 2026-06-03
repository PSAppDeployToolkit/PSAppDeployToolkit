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
using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        private static NavigationView CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode mode, bool isPaneOpen)
        {
            footer = new NavigationViewItem
            {
                Content = "Settings",
                Icon = new FontIcon { Glyph = "", IconFontSize = 16 }
            };
            NavigationView nav = new()
            {
                Width = 600,
                Height = 400,
                PaneDisplayMode = mode,
                IsPaneOpen = isPaneOpen
            };
            _ = nav.Items.Add(new NavigationViewItem { Content = "Home", Icon = new FontIcon { Glyph = "" } });
            _ = nav.Items.Add(new NavigationViewItem { Content = "Docs", Icon = new FontIcon { Glyph = "" } });
            nav.FooterMenuItems.Add(footer);
            return nav;
        }

        [TestMethod]
        public void NavigationView_FooterItem_ResolvesOwningNavigationView()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // The fix that lets footer-hosted items invoke: an item resolves its owning
                    // NavigationView by ancestor walk, not via ItemsControlFromItemContainer.
                    Assert.AreSame(nav, NavigationView.FromItemContainer(footer),
                        "A FooterMenuItems entry must resolve its owning NavigationView via the visual tree.");
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
        public void NavigationView_FooterItem_Invoke_SelectsAndClearsMainSelection()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, true);
                    nav.SelectedIndex = 0;
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    List<NavigationViewItem> invoked = [];
                    nav.ItemInvoked += (_, e) => invoked.Add(e.InvokedItemContainer);

                    nav.SelectFooterMenuItem(footer);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreSame(footer, nav.SelectedFooterItem, "Invoking a footer item makes it the active footer selection.");
                    Assert.IsTrue(footer.IsSelected, "The invoked footer item should be marked selected.");
                    Assert.IsNull(nav.SelectedItem, "Footer selection must clear the main-menu SelectedItem.");
                    Assert.AreEqual(1, invoked.Count, "Invoking a footer item should raise ItemInvoked exactly once.");
                    Assert.AreSame(footer, invoked[0], "ItemInvoked should report the footer container.");
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
        public void NavigationView_MainSelection_ClearsFooterSelection()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectFooterMenuItem(footer);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreSame(footer, nav.SelectedFooterItem, "Footer should be selected before selecting a main item.");

                    nav.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsNull(nav.SelectedFooterItem, "Selecting a main item must clear the footer selection.");
                    Assert.IsFalse(footer.IsSelected, "The footer item should be deselected when a main item is selected.");
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
        public void NavigationView_FooterSelectionIndicator_BecomesVisibleOnFooterSelection()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement? footerIndicator = nav.GetFooterSelectionIndicatorForTesting();
                    Assert.IsNotNull(footerIndicator, "PART_FooterSelectionIndicator should exist in the Left pane template.");
                    Assert.AreEqual(0.0, footerIndicator!.Opacity, 0.01, "Footer indicator should be hidden before any footer selection.");

                    nav.SelectFooterMenuItem(footer);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsGreaterThanOrEqualTo(0.9, footerIndicator.Opacity,
                        "Selecting a footer item should reveal the footer selection indicator.");
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
        public void NavigationView_FooterItem_StretchesToPaneWidth_InLeftOpen()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    // Settle until the footer item has stretched to the asserted pane width.
                    _ = WaitUntil(window.Dispatcher, 2000, () => { window.UpdateLayout(); return footer.ActualWidth > 200.0; });
                    window.UpdateLayout();

                    // The footer item lives in a stretching StackPanel, so its hover/selection surface
                    // spans the pane width rather than the "Settings" text width (the original bug).
                    Assert.IsGreaterThan(200.0, footer.ActualWidth,
                        "An open Left pane footer item should stretch to the pane width, not the content width. Measured: " + footer.ActualWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
        public void NavigationView_FooterItem_IconCentered_InLeftCompactClosed()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.LeftCompact, false);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    ContentPresenter? iconPresenter = FindVisualChildByName<ContentPresenter>(footer, "IconPresenter");
                    Assert.IsNotNull(iconPresenter, "Footer item template should expose the icon presenter.");
                    Point iconOffset = iconPresenter!.TransformToAncestor(footer).Transform(new Point(0, 0));
                    Assert.IsGreaterThanOrEqualTo(4.0 - 0.5, iconOffset.X,
                        "Closed LeftCompact footer icon should not be clipped on the left edge.");
                    Assert.IsLessThanOrEqualTo(44.0 + 0.5, iconOffset.X + iconPresenter.ActualWidth,
                        "Closed LeftCompact footer icon should stay inside the 40px icon slot, aligned with the main items.");
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
        public void NavigationView_Automation_GetSelection_ReportsFooterSelection()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Left, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    NavigationViewAutomationPeer peer = new(nav);
                    ISelectionProvider selectionProvider = peer;
                    Assert.AreEqual(0, selectionProvider.GetSelection().Length,
                        "With nothing selected, the automation peer should report an empty selection.");

                    nav.SelectFooterMenuItem(footer);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1, selectionProvider.GetSelection().Length,
                        "When a footer item is selected, the automation peer should report it as the single selection.");
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
    }
}
