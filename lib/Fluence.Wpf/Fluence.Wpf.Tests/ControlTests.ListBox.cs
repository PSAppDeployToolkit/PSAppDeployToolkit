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
using System.Windows.Media;
using Xunit;

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
        [Fact]
        public Task ListBox_SelectionIndicator_CanonicalGeometryAndCenteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListBox lb = new();
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item A" });
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item B" });
                Window w = new() { Content = lb, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Controls.ListBoxItem item = Assert.IsAssignableFrom<Controls.ListBoxItem>(FindVisualChild<Controls.ListBoxItem>(lb));

                System.Windows.Controls.Border indicator = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(item, "SelectionIndicator"));

                Assert.Equal(3.0, indicator.Width, 0.01);
                Assert.Equal(16.0, indicator.Height, 0.01);
                Assert.Equal(new CornerRadius(1.5), indicator.CornerRadius);
                Assert.Equal(VerticalAlignment.Center, indicator.VerticalAlignment);
                _ = Assert.IsAssignableFrom<TranslateTransform>(indicator.RenderTransform);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("AccentFillColorDefaultBrush"));
                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(indicator.Background);
                Assert.Equal(expected.Color, actual.Color);
                w.Close();
            });
        }

        [Fact]
        public Task ListBox_SelectionIndicator_SlidesInAtFullSizeWhenSelectedAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ListBox lb = new();
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item A" });
                _ = lb.Items.Add(new Controls.ListBoxItem { Content = "Item B" });
                Window w = new() { Content = lb, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Controls.ListBoxItem item = Assert.IsAssignableFrom<Controls.ListBoxItem>(FindVisualChild<Controls.ListBoxItem>(lb));
                System.Windows.Controls.Border indicator = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(item, "SelectionIndicator"));
                Assert.Equal(0.0, indicator.Opacity, 0.01);

                lb.SelectedIndex = 0;
                bool shown = await WaitUntilAsync(w.Dispatcher, 1000, () => indicator.Opacity >= 0.99).ConfigureAwait(true);
                Assert.True(shown, "SelectionIndicator must animate to full opacity when the item is selected.");

                TranslateTransform translate = Assert.IsType<TranslateTransform>(indicator.RenderTransform);
                bool settled = await WaitUntilAsync(w.Dispatcher, 1000, () => System.Math.Abs(translate.X) < 0.01).ConfigureAwait(true);
                Assert.True(settled, "SelectionIndicator must slide to its resting position when selected.");

                Assert.Equal(16.0, indicator.ActualHeight, 0.5);
                w.Close();
            });
        }
    }
}
