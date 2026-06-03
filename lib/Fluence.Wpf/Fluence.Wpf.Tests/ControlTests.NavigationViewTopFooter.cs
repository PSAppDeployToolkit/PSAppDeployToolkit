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
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void NavigationView_TopFooterIndicator_CentersUnderFooterItem()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Top, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectFooterMenuItem(footer);
                    _ = WaitUntil(window.Dispatcher, 600, () => (nav.GetFooterSelectionIndicatorForTesting()?.Opacity ?? 0.0) >= 0.9);
                    window.UpdateLayout();

                    FrameworkElement footerIndicator = nav.GetFooterSelectionIndicatorForTesting()
                        ?? throw new AssertFailedException("PART_FooterSelectionIndicator should exist in the Top pane template.");

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

                    Assert.AreEqual(itemCenterX, indicatorCenterX, 1.5,
                        "Top footer indicator should be horizontally centered under the footer item, not snapped to the left edge. Indicator center: "
                        + indicatorCenterX.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + ", item center: " + itemCenterX.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
        public void NavigationView_TopFooterItem_RendersIconOnly()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Top, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter label = FindVisualChildByName<ContentPresenter>(footer, "ContentPresenter")
                        ?? throw new AssertFailedException("Footer item template should expose the label content presenter.");
                    ContentPresenter icon = FindVisualChildByName<ContentPresenter>(footer, "IconPresenter")
                        ?? throw new AssertFailedException("Footer item template should expose the icon presenter.");

                    Assert.IsFalse(label.IsVisible,
                        "In Top mode a footer item (e.g. Settings) must render gear-only: its label content presenter should be collapsed.");
                    Assert.IsTrue(icon.IsVisible,
                        "In Top mode a footer item must still show its icon.");
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
        public void NavigationView_TopFooterItem_KeepsLabel_InLeft()
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

                    ContentPresenter label = FindVisualChildByName<ContentPresenter>(footer, "ContentPresenter")
                        ?? throw new AssertFailedException("Footer item template should expose the label content presenter.");

                    Assert.IsTrue(label.IsVisible,
                        "The gear-only rule is scoped to Top mode; an open Left pane footer item must keep its label.");
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
        public void NavigationView_TopMainItem_KeepsLabel()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out _, NavigationViewPaneDisplayMode.Top, true);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    NavigationViewItem mainItem = (NavigationViewItem)nav.Items[0]!;
                    ContentPresenter label = FindVisualChildByName<ContentPresenter>(mainItem, "ContentPresenter")
                        ?? throw new AssertFailedException("Main item template should expose the label content presenter.");

                    Assert.IsTrue(label.IsVisible,
                        "Top-level (non-footer) items must keep their labels in Top mode; only footer items collapse to icon-only.");
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
        public void NavigationView_TopFooterIndicator_AnimatesOnSelection()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                try
                {
                    NavigationView nav = CreateNavWithFooterItem(out NavigationViewItem footer, NavigationViewPaneDisplayMode.Top, true);
                    nav.SelectedIndex = 0;
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement footerIndicator = nav.GetFooterSelectionIndicatorForTesting()
                        ?? throw new AssertFailedException("PART_FooterSelectionIndicator should exist in the Top pane template.");

                    // Selecting the footer item should fade/scale the indicator in (animate), not snap.
                    nav.SelectFooterMenuItem(footer);
                    DrainDispatcher(window.Dispatcher); // runs the queued RefreshIndicators that starts the animation
                    Assert.IsTrue(footerIndicator.HasAnimatedProperties,
                        "Selecting a footer item in Top mode should animate the indicator in, not snap it to full opacity.");

                    bool shown = WaitUntil(window.Dispatcher, 600, () => footerIndicator.Opacity >= 0.9);
                    Assert.IsTrue(shown, "The footer indicator should reach full opacity after the fade-in completes.");

                    // Navigating away (exiting Settings) should animate the indicator back out.
                    nav.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(footerIndicator.HasAnimatedProperties,
                        "Leaving the footer item should animate the indicator out, not hide it instantly.");

                    bool hidden = WaitUntil(window.Dispatcher, 600, () => footerIndicator.Opacity <= 0.1);
                    Assert.IsTrue(hidden, "The footer indicator should fade to hidden after the footer item is deselected.");
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
