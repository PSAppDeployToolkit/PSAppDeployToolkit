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

using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 C21 tests: Card elevation shadow - Default variant has distinct drop shadow;
    /// Subtle (and other flat variants) have no shadow.
    /// Authority: WinUI 3 card elevation pattern (LayerFillColorDefaultBrush elevation context).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 C21  Card elevation shadow
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Card_DefaultVariant_IsFlatAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // A Fluent card surface is flat: background, 1 px stroke, radius. Elevation belongs
                // to transient surfaces, so no variant, Default included, carries an effect.
                System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);
                Assert.Null(outerBorder.Effect);
                w.Close();
            });
        }

        [Fact]
        public Task Card_Background_IsPaintedByExactlyOneLayerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush cardFill = Assert.IsType<SolidColorBrush>(app.TryFindResource("CardBackgroundFillColorDefaultBrush"));

                // The card fill token is translucent (#B3FFFFFF in Light). Painting it on two nested
                // borders composites it with itself and renders the card more opaque than the token
                // specifies, so exactly one element in the template may carry it.
                int painters = FindVisualChildren<System.Windows.Controls.Border>(card)
                    .Count(b => b.Background is SolidColorBrush brush && brush.Color == cardFill.Color);

                Assert.Equal(1, painters);
                w.Close();
            });
        }

        [Fact]
        public Task Card_SubtleVariant_NoElevationShadowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Subtle, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                Assert.Null(outerBorder.Effect);
                w.Close();
            });
        }

        [Fact]
        public Task Card_OutlinedVariant_NoElevationShadowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Outlined, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                Assert.Null(outerBorder.Effect);
                w.Close();
            });
        }

        [Fact]
        public Task Card_FilledVariant_NoElevationShadowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Filled, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                Assert.Null(outerBorder.Effect);
                w.Close();
            });
        }

        [Fact]
        public Task Card_Surface_CarriesStrokeAndRadiusOnTheSameElementAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                // One element owns fill, stroke and radius, so there is no second border to drift.
                SolidColorBrush expectedStroke = Assert.IsType<SolidColorBrush>(app.TryFindResource("CardStrokeColorDefaultBrush"));
                SolidColorBrush actualStroke = Assert.IsType<SolidColorBrush>(outerBorder.BorderBrush);
                Assert.Equal(expectedStroke.Color, actualStroke.Color);
                Assert.Equal(new Thickness(1), outerBorder.BorderThickness);
                Assert.Equal(new CornerRadius(8), outerBorder.CornerRadius);

                // The style routes the radius through OverlayCornerRadius so a consumer can retheme it.
                Assert.Equal(app.TryFindResource("OverlayCornerRadius"), card.CornerRadius);
                w.Close();
            });
        }
    }
}
