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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;
using Xunit;

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

        [Fact]
        public Task TabView_PART_ScrollBackButton_ExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 2" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Primitives.RepeatButton btn = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollBackButton"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task TabView_PART_ScrollForwardButton_ExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 2" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Primitives.RepeatButton btn = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollForwardButton"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task TabView_PART_TabContentScroller_ExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ScrollViewer sv = Assert.IsType<ScrollViewer>(FindVisualChildByName<ScrollViewer>(tv, "PART_TabContentScroller"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task TabView_ScrollButtons_HiddenWhenNoTabOverflowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "A" });
                _ = tv.Items.Add(new TabViewItem { Header = "B" });
                // Wide window: 2 short tabs will not overflow a 700px wide control
                Window w = new() { Content = tv, Width = 700, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Primitives.RepeatButton back = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollBackButton"), exactMatch: false);
                System.Windows.Controls.Primitives.RepeatButton fwd = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollForwardButton"), exactMatch: false);

                Assert.Equal(
                    Visibility.Collapsed, back.Visibility);
                Assert.Equal(
                    Visibility.Collapsed, fwd.Visibility);
                w.Close();
            });
        }
    }
}
