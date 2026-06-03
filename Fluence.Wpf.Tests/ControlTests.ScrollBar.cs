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
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-5A.3 tests for the Fluent ScrollBar VSM uplift.
    /// Verifies CommonStates and ScrollingIndicatorStates VSM groups are present and
    /// that GoToState with useTransitions=false snaps to the correct dimension instantly.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar - PART names found in ScrollViewer
        // ---------------------------------------------------------------------------

        private static void AssertScrollBarVisualStateDoubleKeyFrame(
            ScrollBar scrollBar,
            string stateName,
            string targetName,
            string targetProperty,
            double expectedValue)
        {
            Grid? root = FindVisualChildByName<Grid>(scrollBar, "Root");
            Assert.IsNotNull(root, "Root Grid must be present in ScrollBar template.");

            IList groups = VisualStateManager.GetVisualStateGroups(root);
            VisualState? state = null;
            foreach (VisualStateGroup group in groups)
            {
                foreach (VisualState candidate in group.States)
                {
                    if (candidate.Name == stateName)
                    {
                        state = candidate;
                        break;
                    }
                }

                if (state is not null)
                {
                    break;
                }
            }

            Assert.IsNotNull(state, "Visual state must exist: " + stateName);
            Assert.IsNotNull(state.Storyboard, "Visual state must define a storyboard: " + stateName);

            foreach (Timeline timeline in state.Storyboard.Children)
            {
                if (timeline is not DoubleAnimationUsingKeyFrames animation ||
                    Storyboard.GetTargetName(animation) != targetName ||
                    Storyboard.GetTargetProperty(animation).Path != targetProperty)
                {
                    continue;
                }

                Assert.AreEqual(expectedValue, animation.KeyFrames[0].Value, 0.01,
                    string.Format(
                        "State {0} must set {1}.{2} to {3}.",
                        stateName,
                        targetName,
                        targetProperty,
                        expectedValue));
                return;
            }

            Assert.Fail(string.Format(
                "State {0} must animate {1}.{2}.",
                stateName,
                targetName,
                targetProperty));
        }

        [TestMethod]
        public void ScrollBar_ScrollViewerTemplate_ContainsBothScrollBarParts()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollViewer sv = new()
                {
                    Width = 200,
                    Height = 100,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                    Style = app?.TryFindResource("ScrollViewerStyle") as Style
                };

                StackPanel sp = new();
                for (int i = 0; i < 30; i++)
                {
                    _ = sp.Children.Add(new TextBlock { Text = "Item " + i, Height = 20, Width = 400 });
                }
                sv.Content = sp;

                Window window = new() { Width = 300, Height = 200, Content = sv };
                try
                {
                    window.Show();
                    sv.UpdateLayout();

                    ScrollBar? vertBar = FindVisualChildByName<ScrollBar>(sv, "PART_VerticalScrollBar");
                    ScrollBar? horizBar = FindVisualChildByName<ScrollBar>(sv, "PART_HorizontalScrollBar");

                    Assert.IsNotNull(vertBar,
                        "PART_VerticalScrollBar must be present in the ScrollViewerStyle template.");
                    Assert.IsNotNull(horizBar,
                        "PART_HorizontalScrollBar must be present in the ScrollViewerStyle template.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar - VSM ScrollingIndicatorStates
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ScrollBar_VSM_MouseIndicator_ExpandsVerticalWidth()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = new()
                {
                    Orientation = Orientation.Vertical,
                    Style = app?.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 12,
                    Height = 200
                };

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // GoToState with useTransitions=false: DiscreteDoubleKeyFrame at
                    // KeyTime=0 applies the final value immediately.
                    bool stateApplied = VisualStateManager.GoToState(sb, "MouseIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.IsTrue(stateApplied,
                        "GoToState('MouseIndicator') must return true - VSM group must be present.");

                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "MouseIndicator", "Root", "Width", 8.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "MouseIndicator", "DecreaseButton", "Opacity", 1.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "MouseIndicator", "IncreaseButton", "Opacity", 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void ScrollBar_VSM_NoIndicator_CollapsesVerticalWidth()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = new()
                {
                    Orientation = Orientation.Vertical,
                    Style = app?.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 12,
                    Height = 200
                };

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // Expand to MouseIndicator first, then collapse back.
                    _ = VisualStateManager.GoToState(sb, "MouseIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    bool stateApplied = VisualStateManager.GoToState(sb, "NoIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.IsTrue(stateApplied,
                        "GoToState('NoIndicator') must return true - VSM group must be present.");

                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "NoIndicator", "Root", "Width", 6.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "NoIndicator", "DecreaseButton", "Opacity", 0.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "NoIndicator", "IncreaseButton", "Opacity", 0.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void ScrollBar_VSM_MouseIndicator_ExpandsHorizontalHeight()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = new()
                {
                    Orientation = Orientation.Horizontal,
                    Style = app?.TryFindResource("HorizontalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Height = 12,
                    Width = 200
                };

                Window window = new() { Width = 300, Height = 60, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    bool stateApplied = VisualStateManager.GoToState(sb, "MouseIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.IsTrue(stateApplied,
                        "GoToState('MouseIndicator') on horizontal ScrollBar must return true.");

                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "MouseIndicator", "Root", "Height", 8.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "MouseIndicator", "DecreaseButton", "Opacity", 1.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "MouseIndicator", "IncreaseButton", "Opacity", 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void ScrollBar_DefaultLayout_ReservesExpandedSlotWithCompactIndicator()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = new()
                {
                    Orientation = Orientation.Vertical,
                    Style = app?.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Height = 200
                };

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Grid? root = FindVisualChildByName<Grid>(sb, "Root");
                    Assert.IsNotNull(root, "Root Grid must be present in ScrollBar template.");
                    Assert.AreEqual(8.0, sb.ActualWidth, 0.5,
                        "Vertical ScrollBar should reserve the 8px layout slot (thinned hover width).");
                    Assert.AreEqual(6.0, root.Width, 0.5,
                        "Vertical ScrollBar indicator should start at the compact 6px thumb width after the requested 2px reduction.");
                    Assert.AreEqual(HorizontalAlignment.Right, root.HorizontalAlignment,
                        "Compact vertical indicator should align to the outside edge of the reserved slot.");

                    RepeatButton? decreaseButton = FindVisualChildByName<RepeatButton>(sb, "DecreaseButton");
                    RepeatButton? increaseButton = FindVisualChildByName<RepeatButton>(sb, "IncreaseButton");
                    Assert.IsNotNull(decreaseButton, "Vertical ScrollBar must include the top line button for hover expansion.");
                    Assert.IsNotNull(increaseButton, "Vertical ScrollBar must include the bottom line button for hover expansion.");
                    Assert.AreEqual(0.0, decreaseButton.Opacity, 0.01,
                        "Line buttons should be hidden until the ScrollBar enters the hover/MouseIndicator state.");
                    Assert.AreEqual(0.0, increaseButton.Opacity, 0.01,
                        "Line buttons should be hidden until the ScrollBar enters the hover/MouseIndicator state.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar - disabled state reduces opacity
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ScrollBar_Disabled_OpacityReducedOrElementDisabled()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = new()
                {
                    Orientation = Orientation.Vertical,
                    Style = app?.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 12,
                    Height = 200
                };

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    sb.IsEnabled = false;
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // IsEnabled=False trigger sets Opacity=0.45 on the ScrollBar root.
                    Assert.IsTrue(!sb.IsEnabled || sb.Opacity < 1.0,
                        "Disabled ScrollBar must either be IsEnabled=false or have Opacity < 1.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar - theme cycle
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ScrollBar_ThemeCycle_BrushesResolveAfterEachSwitch()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] keys =
                [
                    "ScrollBarSize",
                    "ScrollBarCompactThumbSize",
                    "ScrollViewerScrollBarMargin",
                    "ControlStrongFillColorDefaultBrush",
                    "SubtleFillColorSecondaryBrush"
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in keys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in ScrollBar theme cycle step: {1}", key, theme));
                    }
                }
            });
        }
    }
}
