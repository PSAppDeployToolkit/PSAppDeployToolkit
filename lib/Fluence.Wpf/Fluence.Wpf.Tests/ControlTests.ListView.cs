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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        [TestMethod]
        public void ListView_SelectionIndicator_PresentInItemTemplate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                _ = lv.Items.Add(new ListViewItem { Content = "Item B" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Find the first ListViewItem in the visual tree
                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist in visual tree after Show.");

                Border? indicator = FindVisualChildByName<Border>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator,
                    "SelectionIndicator border must be present in ListViewItem template per WI-3 C20.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_SelectionIndicator_WidthIsCanonical()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist.");
                Border? indicator = FindVisualChildByName<Border>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator must be present.");

                Assert.AreEqual(3.0, indicator.Width, 0.01,
                    "SelectionIndicator.Width must be 3.0 (WinUI 3 canonical 3px bar) per WI-3 C20.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_SelectionIndicator_CornerRadiusIsCanonical()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist.");
                Border? indicator = FindVisualChildByName<Border>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator must be present.");

                Assert.AreEqual(new CornerRadius(1.5), indicator.CornerRadius,
                    "SelectionIndicator.CornerRadius must be 1.5 per WinUI 3 ListViewItemSelectionIndicatorCornerRadius.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_SelectionIndicator_BackgroundIsAccentBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist.");
                Border? indicator = FindVisualChildByName<Border>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator must be present.");

                SolidColorBrush? expected = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "AccentFillColorDefaultBrush must resolve.");

                SolidColorBrush? actual = indicator.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "SelectionIndicator.Background must be a SolidColorBrush.");
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "SelectionIndicator.Background must be AccentFillColorDefaultBrush per WI-3 C20.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_AnimateRemove_RemovesItemFromBoundObservableCollection()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
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
                DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();

                bool completed = false;
                lv.AnimateRemove("Two", delegate { completed = true; });

                bool removed = WaitUntil(w.Dispatcher, 1000, delegate
                {
                    return completed && !items.Contains("Two");
                });

                Assert.IsTrue(removed, "AnimateRemove should animate then remove the item from the bound ObservableCollection.");
                Assert.AreEqual(2, items.Count);
                w.Close();
            });
        }
    }
}
