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
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 A10 tests: InfoBadge DisplayKindStates VSM group.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 A10  InfoBadge DisplayKindStates VSM group
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void InfoBadge_DisplayKindStates_GroupExists()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBadge badge = new();
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Verify the VSM group is present in the template.
                IList groups = VisualStateManager.GetVisualStateGroups(
                    FindVisualChild<Grid>(badge));
                bool found = false;
                if (groups is not null)
                {
                    foreach (object? g in groups)
                    {
                        if (g is VisualStateGroup vsg && vsg.Name == "DisplayKindStates")
                        { found = true; break; }
                    }
                }
                Assert.IsTrue(found, "InfoBadge template must contain a VisualStateGroup named 'DisplayKindStates'.");
                w.Close();
            });
        }

        [TestMethod]
        public void InfoBadge_DefaultState_IsDot()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // Default: Value=-1, no IconSource → Dot state.
                InfoBadge badge = new();
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // DotIndicator should be visible; BadgeBorder should be collapsed.
                Ellipse? dot = FindVisualChildByName<Ellipse>(badge, "DotIndicator");
                System.Windows.Controls.Border? border = FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder");
                Assert.IsNotNull(dot, "DotIndicator element must exist.");
                Assert.IsNotNull(border, "BadgeBorder element must exist.");
                Assert.AreEqual(Visibility.Visible, dot.Visibility, "DotIndicator must be Visible in Dot state.");
                Assert.AreEqual(Visibility.Collapsed, border.Visibility, "BadgeBorder must be Collapsed in Dot state.");
                w.Close();
            });
        }

        [TestMethod]
        public void InfoBadge_ValueSet_ShowsBadgeBorder()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBadge badge = new() { Value = 5 };
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? dot = FindVisualChildByName<Ellipse>(badge, "DotIndicator");
                System.Windows.Controls.Border? border = FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder");
                Assert.AreEqual(Visibility.Collapsed, dot?.Visibility, "DotIndicator must be Collapsed when Value >= 0.");
                Assert.AreEqual(Visibility.Visible, border?.Visibility, "BadgeBorder must be Visible when Value >= 0.");
                w.Close();
            });
        }

        [TestMethod]
        public void InfoBadge_ValueBadge_UsesStableScreenshotPillMetrics()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBadge badge = new() { Value = 12 };
                Window w = new() { Content = badge, Width = 100, Height = 80 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);
                    w.UpdateLayout();

                    System.Windows.Controls.Border? border = FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder");
                    ContentPresenter? content = FindVisualChildByName<ContentPresenter>(badge, "ContentArea");
                    Assert.IsNotNull(border, "Value badge must render through BadgeBorder.");
                    Assert.IsNotNull(content, "Value badge must render centered text through ContentArea.");
                    Assert.AreEqual(34.0, border.MinWidth, 0.1,
                        "Value badges should keep the stable pill width shown in the Navigation sample screenshot.");
                    Assert.AreEqual(24.0, border.MinHeight, 0.1,
                        "Value badges should keep the stable pill height shown in the Navigation sample screenshot.");
                    Assert.AreEqual(24.0, border.MaxHeight, 0.1,
                        "Value badges should not stretch taller than the screenshot pill height.");
                    Assert.AreEqual(border.Padding.Left, border.Padding.Right, 0.01,
                        "Value badge horizontal padding should be symmetric.");
                    Assert.AreEqual(border.Padding.Top, border.Padding.Bottom, 0.01,
                        "Value badge vertical padding should be symmetric.");
                    Assert.IsTrue(border.Padding.Top > border.Padding.Left,
                        "Value badge vertical padding should be taller than the horizontal padding.");
                    Assert.AreEqual(HorizontalAlignment.Center, content.HorizontalAlignment,
                        "Value badge text should be centered horizontally.");
                    Assert.AreEqual(VerticalAlignment.Center, content.VerticalAlignment,
                        "Value badge text should be centered vertically.");
                    Assert.AreEqual(FontWeights.SemiBold, TextElement.GetFontWeight(content),
                        "Value badge text should render bold.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void InfoBadge_DisplayKindStates_HasAllFourStates()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBadge badge = new();
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                IList groups = VisualStateManager.GetVisualStateGroups(FindVisualChild<Grid>(badge));
                VisualStateGroup? dkg = null;
                if (groups is not null)
                {
                    foreach (object? g in groups)
                    {
                        if (g is VisualStateGroup vsg && vsg.Name == "DisplayKindStates") { dkg = vsg; break; }
                    }
                }
                Assert.IsNotNull(dkg, "DisplayKindStates group must exist.");

                HashSet<string> stateNames = [];
                foreach (object? s in dkg.States)
                {
                    if (s is VisualState vs)
                    {
                        _ = stateNames.Add(vs.Name);
                    }
                }
                Assert.IsTrue(stateNames.Contains("Dot"), "DisplayKindStates must include 'Dot'.");
                Assert.IsTrue(stateNames.Contains("Icon"), "DisplayKindStates must include 'Icon'.");
                Assert.IsTrue(stateNames.Contains("FontIcon"), "DisplayKindStates must include 'FontIcon'.");
                Assert.IsTrue(stateNames.Contains("Value"), "DisplayKindStates must include 'Value'.");
                w.Close();
            });
        }
    }
}
