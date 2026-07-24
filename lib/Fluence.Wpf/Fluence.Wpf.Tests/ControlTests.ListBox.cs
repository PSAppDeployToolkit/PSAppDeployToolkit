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
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// ListBoxItem selection indicator tests.
    /// Authority: WinUI 3 ListViewItem_themeresources.xaml (the ListBox indicator mirrors the
    /// in-tree ListViewItem indicator: canonical 3x16 accent bar, CornerRadius 1.5, vertically
    /// centered, translate slide-in animation).
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void ListBox_SelectionIndicator_CanonicalGeometryAndCentered()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListBox lb = new();
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item A" });
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item B" });
                Window w = new() { Content = lb, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Controls.ListBoxItem? item = FindVisualChild<Controls.ListBoxItem>(lb);
                Assert.IsNotNull(item, "ListBoxItem must exist in visual tree after Show.");

                System.Windows.Controls.Border? indicator = FindVisualChildByName<System.Windows.Controls.Border>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator border must be present in ListBoxItem template.");

                Assert.AreEqual(3.0, indicator.Width, 0.01,
                    "SelectionIndicator.Width must be 3.0 (WinUI 3 canonical 3px bar).");
                Assert.AreEqual(16.0, indicator.Height, 0.01,
                    "SelectionIndicator.Height must be 16.0 to match the ListViewItem indicator.");
                Assert.AreEqual(new CornerRadius(1.5), indicator.CornerRadius,
                    "SelectionIndicator.CornerRadius must be 1.5 per WinUI 3 ListViewItemSelectionIndicatorCornerRadius.");
                Assert.AreEqual(VerticalAlignment.Center, indicator.VerticalAlignment,
                    "SelectionIndicator must be vertically centered in the item.");
                _ = Assert.IsInstanceOfType<TranslateTransform>(indicator.RenderTransform,
                    "SelectionIndicator must use the slide-in TranslateTransform; a ScaleTransform shrinks the bar and breaks vertical centering.");

                SolidColorBrush? expected = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "AccentFillColorDefaultBrush must resolve.");
                SolidColorBrush? actual = indicator.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "SelectionIndicator.Background must be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, actual.Color,
                    "SelectionIndicator.Background must be AccentFillColorDefaultBrush.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListBox_SelectionIndicator_SlidesInAtFullSizeWhenSelected()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListBox lb = new();
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item A" });
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item B" });
                Window w = new() { Content = lb, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Controls.ListBoxItem? item = FindVisualChild<Controls.ListBoxItem>(lb);
                Assert.IsNotNull(item, "ListBoxItem must exist.");
                System.Windows.Controls.Border? indicator = FindVisualChildByName<System.Windows.Controls.Border>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator must be present.");
                Assert.AreEqual(0.0, indicator.Opacity, 0.01,
                    "SelectionIndicator must be hidden while the item is unselected.");

                lb.SelectedIndex = 0;
                bool shown = WaitUntil(w.Dispatcher, 1000, () => indicator.Opacity >= 0.99);
                Assert.IsTrue(shown, "SelectionIndicator must animate to full opacity when the item is selected.");

                TranslateTransform? translate = indicator.RenderTransform as TranslateTransform;
                Assert.IsNotNull(translate, "SelectionIndicator must keep its TranslateTransform.");
                bool settled = WaitUntil(w.Dispatcher, 1000, () => System.Math.Abs(translate.X) < 0.01);
                Assert.IsTrue(settled, "SelectionIndicator must slide to its resting position when selected.");

                Assert.AreEqual(16.0, indicator.ActualHeight, 0.5,
                    "SelectionIndicator must render at its full 16px height when selected (no residual scale).");
                w.Close();
            });
        }
    }
}
