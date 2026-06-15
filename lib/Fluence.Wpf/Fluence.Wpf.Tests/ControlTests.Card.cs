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
using System.Windows.Media.Effects;
using WpfBorder = System.Windows.Controls.Border;

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

        [TestMethod]
        public void Card_DefaultVariant_HasElevationShadowOnOuterBorder()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? outerBorder = FindVisualChildByName<WpfBorder>(card, "OuterBorder");
                Assert.IsNotNull(outerBorder, "OuterBorder must exist in Card template.");

                Assert.IsNotNull(outerBorder.Effect,
                    "Card Default variant: OuterBorder.Effect must be non-null (elevation shadow) per WI-3 C21.");
                Assert.IsInstanceOfType(outerBorder.Effect, typeof(DropShadowEffect),
                    "Card Default variant: OuterBorder.Effect must be DropShadowEffect.");
                w.Close();
            });
        }

        [TestMethod]
        public void Card_SubtleVariant_NoElevationShadow()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Subtle, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? outerBorder = FindVisualChildByName<WpfBorder>(card, "OuterBorder");
                Assert.IsNotNull(outerBorder, "OuterBorder must exist in Card template.");

                Assert.IsNull(outerBorder.Effect,
                    "Card Subtle variant: OuterBorder.Effect must be null (no shadow) per WI-3 C21.");
                w.Close();
            });
        }

        [TestMethod]
        public void Card_OutlinedVariant_NoElevationShadow()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Outlined, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? outerBorder = FindVisualChildByName<WpfBorder>(card, "OuterBorder");
                Assert.IsNotNull(outerBorder, "OuterBorder must exist in Card template.");

                Assert.IsNull(outerBorder.Effect,
                    "Card Outlined variant: OuterBorder.Effect must be null per WI-3 C21.");
                w.Close();
            });
        }

        [TestMethod]
        public void Card_FilledVariant_NoElevationShadow()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Filled, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? outerBorder = FindVisualChildByName<WpfBorder>(card, "OuterBorder");
                Assert.IsNotNull(outerBorder, "OuterBorder must exist in Card template.");

                Assert.IsNull(outerBorder.Effect,
                    "Card Filled variant: OuterBorder.Effect must be null per WI-3 C21.");
                w.Close();
            });
        }

        [TestMethod]
        public void Card_DefaultVariant_ShadowHasCorrectProfile()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? outerBorder = FindVisualChildByName<WpfBorder>(card, "OuterBorder");
                Assert.IsNotNull(outerBorder, "OuterBorder must exist.");
                DropShadowEffect? shadow = outerBorder.Effect as DropShadowEffect;
                Assert.IsNotNull(shadow, "Effect must be DropShadowEffect.");

                // WI-3 C21: subtle elevation - soft blur, low opacity, downward direction
                Assert.IsTrue(shadow.BlurRadius is >= 4 and <= 16,
                    $"BlurRadius {shadow.BlurRadius} outside expected range [4,16].");
                Assert.IsTrue(shadow.Opacity is > 0 and <= 0.2,
                    $"Opacity {shadow.Opacity} outside expected range (0, 0.2].");
                w.Close();
            });
        }
    }
}
