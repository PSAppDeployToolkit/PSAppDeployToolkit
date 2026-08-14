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

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 C20 tests: ListView/ListViewItem selection indicator.
    /// Authority: WinUI 3 ListViewItem_themeresources.xaml
    /// (ListViewItemSelectionIndicatorCornerRadius=1.5, AccentFillColorDefaultBrush).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 C20  ListView SelectionIndicator
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ListView_SelectionIndicator_PresentInItemTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                _ = lv.Items.Add(new ListViewItem { Content = "Item B" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Find the first ListViewItem in the visual tree
                ListViewItem item = Assert.IsAssignableFrom<ListViewItem>(FindVisualChild<ListViewItem>(lv));

                Border indicator = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"));
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_WidthIsCanonicalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsAssignableFrom<ListViewItem>(FindVisualChild<ListViewItem>(lv));
                Border indicator = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"));

                Assert.Equal(3.0, indicator.Width, 0.01);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_CornerRadiusIsCanonicalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsAssignableFrom<ListViewItem>(FindVisualChild<ListViewItem>(lv));
                Border indicator = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"));

                Assert.Equal(new CornerRadius(1.5), indicator.CornerRadius);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_BackgroundIsAccentBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsAssignableFrom<ListViewItem>(FindVisualChild<ListViewItem>(lv));
                Border indicator = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"));

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("AccentFillColorDefaultBrush"));

                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(indicator.Background);
                Assert.Equal(
                    expected.Color,
                    actual.Color);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_AnimateRemove_RemovesItemFromBoundObservableCollectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ObservableCollection<string> items = ["One", "Two", "Three"];
                Controls.ListView lv = new()
                {
                    Width = 300,
                    Height = 180,
                    ItemsSource = items,
                    ItemAnimationsEnabled = true,
                };
                Window w = new() { Content = lv, Width = 360, Height = 240 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();

                bool completed = false;
                lv.AnimateRemove("Two", delegate { completed = true; });

                bool removed = await WaitUntilAsync(w.Dispatcher, 1000, delegate
                {
                    return completed && !items.Contains("Two");
                }).ConfigureAwait(true);

                Assert.True(removed, "AnimateRemove should animate then remove the item from the bound ObservableCollection.");
                Assert.Equal(2, items.Count);
                w.Close();
            });
        }
    }
}
