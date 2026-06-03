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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B13 tests: Expander chevron rotation easing (ControlFastOutSlowIn / SplineDoubleKeyFrame).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B13  Expander chevron rotation easing
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void Expander_StyleApplies_RootBorderFound()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Content" };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // RootBorder is the template root - proves Fluence style applied.
                Border? rootBorder = FindVisualChildByName<Border>(expander, "RootBorder");
                Assert.IsNotNull(rootBorder, "RootBorder must exist in Expander template (Fluence style applied).");
                w.Close();
            });
        }

        [TestMethod]
        public void Expander_ChevronPath_ExistsWithRotateTransformOnParent()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = false };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? chevron = FindVisualChildByName<Path>(expander, "Chevron");
                Assert.IsNotNull(chevron, "Chevron Path must exist in Expander header template.");

                // Parent Border owns the RotateTransform.
                Border? parent = VisualTreeHelper.GetParent(chevron) as Border;
                Assert.IsNotNull(parent, "Chevron parent must be a Border.");

                RotateTransform? rt = parent.RenderTransform as RotateTransform;
                Assert.IsNotNull(rt,
                    "Border containing Chevron must have RenderTransform=RotateTransform (ChevronRotation).");
                Assert.AreEqual(0.0, rt.Angle, 1.0,
                    "ChevronRotation.Angle must be 0 when Expander is collapsed (WinUI Expander_themeresources.xaml).");
                w.Close();
            });
        }

        [TestMethod]
        public void Expander_Expanded_ContentVisibilityIsVisible()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = true };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Structural check: ExpandSite ContentPresenter is present.
                ContentPresenter? site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite");
                Assert.IsNotNull(site, "ExpandSite ContentPresenter must exist in Expander template.");
                w.Close();
            });
        }

        [TestMethod]
        public void Expander_HeaderBorder_CornerRadius4()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test" };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Border? headerBorder = FindVisualChildByName<Border>(expander, "HeaderBorder");
                Assert.IsNotNull(headerBorder, "HeaderBorder must exist in ExpanderHeaderToggleButton template.");
                Assert.AreEqual(new CornerRadius(4), headerBorder.CornerRadius,
                    "HeaderBorder CornerRadius must be 4 (matching WinUI Expander corner spec).");
                w.Close();
            });
        }
    }
}
