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
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B11 tests: Slider thumb scale animations.
    /// WinUI canonical: hover 1.167, pressed 0.86, ControlFastOutSlowIn easing.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B11  Slider thumb scale
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void Slider_StyleApplies_PartTrackFound()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Slider slider = new() { Value = 50, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Track? track = FindVisualChildByName<Track>(slider, "PART_Track");
                Assert.IsNotNull(track, "PART_Track must exist in Slider visual tree after template applied.");
                w.Close();
            });
        }

        [TestMethod]
        public void Slider_DefaultState_ThumbScaleIsOne()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Slider slider = new() { Value = 50, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Thumb's template root Grid has a ScaleTransform named ThumbScale.
                Thumb? thumb = FindVisualChild<Thumb>(slider);
                Assert.IsNotNull(thumb, "Thumb must exist in Slider visual tree.");

                System.Windows.Controls.Grid? grid = FindVisualChild<System.Windows.Controls.Grid>(thumb);
                Assert.IsNotNull(grid, "Thumb template root Grid must exist.");

                ScaleTransform? scale = grid.RenderTransform as ScaleTransform;
                Assert.IsNotNull(scale, "Thumb root Grid RenderTransform must be a ScaleTransform.");
                Assert.AreEqual(1.0, scale.ScaleX, 0.001,
                    "Default ScaleX must be 1.0 (WinUI Slider_themeresources.xaml: thumb starts unscaled).");
                Assert.AreEqual(1.0, scale.ScaleY, 0.001,
                    "Default ScaleY must be 1.0.");
                w.Close();
            });
        }

        [TestMethod]
        public void Slider_ThumbTemplate_HasEllipseAndInnerDot()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Slider slider = new() { Value = 30, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumbEllipse = FindVisualChildByName<Ellipse>(slider, "ThumbEllipse");
                Ellipse? innerDot = FindVisualChildByName<Ellipse>(slider, "ThumbInnerDot");

                Assert.IsNotNull(thumbEllipse, "ThumbEllipse element must exist in Slider template.");
                Assert.IsNotNull(innerDot, "ThumbInnerDot element must exist in Slider template.");
                w.Close();
            });
        }
    }
}
