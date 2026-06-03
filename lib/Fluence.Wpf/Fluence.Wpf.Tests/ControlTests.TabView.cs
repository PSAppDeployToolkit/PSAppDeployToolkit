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
using WpfRepeatButton = System.Windows.Controls.Primitives.RepeatButton;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B16 tests: TabView PART_ScrollBackButton + PART_ScrollForwardButton scroll controls.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B16  TabView scroll buttons
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void TabView_PART_ScrollBackButton_ExistsInTemplate()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 2" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfRepeatButton? btn = FindVisualChildByName<WpfRepeatButton>(tv, "PART_ScrollBackButton");
                Assert.IsNotNull(btn, "PART_ScrollBackButton (RepeatButton) must exist in TabView template.");
                w.Close();
            });
        }

        [TestMethod]
        public void TabView_PART_ScrollForwardButton_ExistsInTemplate()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 2" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfRepeatButton? btn = FindVisualChildByName<WpfRepeatButton>(tv, "PART_ScrollForwardButton");
                Assert.IsNotNull(btn, "PART_ScrollForwardButton (RepeatButton) must exist in TabView template.");
                w.Close();
            });
        }

        [TestMethod]
        public void TabView_PART_TabContentScroller_ExistsInTemplate()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ScrollViewer? sv = FindVisualChildByName<ScrollViewer>(tv, "PART_TabContentScroller");
                Assert.IsNotNull(sv, "PART_TabContentScroller (ScrollViewer) must exist in TabView template.");
                w.Close();
            });
        }

        [TestMethod]
        public void TabView_ScrollButtons_HiddenWhenNoTabOverflow()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "A" });
                _ = tv.Items.Add(new TabViewItem { Header = "B" });
                // Wide window: 2 short tabs will not overflow a 700px wide control
                Window w = new() { Content = tv, Width = 700, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfRepeatButton? back = FindVisualChildByName<WpfRepeatButton>(tv, "PART_ScrollBackButton");
                WpfRepeatButton? fwd = FindVisualChildByName<WpfRepeatButton>(tv, "PART_ScrollForwardButton");
                Assert.IsNotNull(back, "PART_ScrollBackButton must be in template.");
                Assert.IsNotNull(fwd, "PART_ScrollForwardButton must be in template.");

                Assert.AreEqual(
                    Visibility.Collapsed, back.Visibility,
                    "ScrollBackButton must be Collapsed when tabs do not overflow.");
                Assert.AreEqual(
                    Visibility.Collapsed, fwd.Visibility,
                    "ScrollForwardButton must be Collapsed when tabs do not overflow.");
                w.Close();
            });
        }
    }
}
