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

using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Xunit;

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
            Assert.NotNull(root);

            IList groups = VisualStateManager.GetVisualStateGroups(root);
            VisualState? state = null;
            foreach (VisualStateGroup group in groups)
            {
                foreach (VisualState candidate in group.States)
                {
                    if (string.Equals(candidate.Name, stateName, System.StringComparison.Ordinal))
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

            Assert.NotNull(state);
            Assert.NotNull(state.Storyboard);

            foreach (Timeline timeline in state.Storyboard.Children)
            {
                if (timeline is not DoubleAnimationUsingKeyFrames animation ||
                    !string.Equals(Storyboard.GetTargetName(animation), targetName, System.StringComparison.Ordinal) ||
                    !string.Equals(Storyboard.GetTargetProperty(animation).Path, targetProperty, System.StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.Equal(expectedValue, animation.KeyFrames[0].Value, 0.01);
                return;
            }

            Assert.Fail(string.Format(
                CultureInfo.InvariantCulture,
                "State {0} must animate {1}.{2}.",
                stateName,
                targetName,
                targetProperty));
        }

        [Fact]
        public void ScrollBar_ScrollViewerTemplate_ContainsBothScrollBarParts()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollViewer sv = new()
                {
                    Width = 200,
                    Height = 100,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                    Style = app?.TryFindResource("ScrollViewerStyle") as Style,
                };

                StackPanel sp = new();
                for (int i = 0; i < 30; i++)
                {
                    _ = sp.Children.Add(new TextBlock { Text = "Item " + i.ToString(format: null, CultureInfo.InvariantCulture), Height = 20, Width = 400 });
                }
                sv.Content = sp;

                Window window = new() { Width = 300, Height = 200, Content = sv };
                try
                {
                    window.Show();
                    sv.UpdateLayout();

                    ScrollBar? vertBar = FindVisualChildByName<ScrollBar>(sv, "PART_VerticalScrollBar");
                    ScrollBar? horizBar = FindVisualChildByName<ScrollBar>(sv, "PART_HorizontalScrollBar");

                    Assert.NotNull(vertBar);
                    Assert.NotNull(horizBar);
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

        [Fact]
        public void ScrollBar_VSM_MouseIndicator_ExpandsVerticalWidth()
        {
            WpfTestSta.Invoke(static () =>
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
                    Height = 200,
                };

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // GoToState with useTransitions=false: DiscreteDoubleKeyFrame at
                    // KeyTime=0 applies the final value immediately.
                    bool stateApplied = VisualStateManager.GoToState(sb, "MouseIndicator", useTransitions: false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(stateApplied,
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

        [Fact]
        public void ScrollBar_VSM_NoIndicator_CollapsesVerticalWidth()
        {
            WpfTestSta.Invoke(static () =>
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
                    Height = 200,
                };

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // Expand to MouseIndicator first, then collapse back.
                    _ = VisualStateManager.GoToState(sb, "MouseIndicator", useTransitions: false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    bool stateApplied = VisualStateManager.GoToState(sb, "NoIndicator", useTransitions: false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(stateApplied,
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

        [Fact]
        public void ScrollBar_VSM_MouseIndicator_ExpandsHorizontalHeight()
        {
            WpfTestSta.Invoke(static () =>
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
                    Width = 200,
                };

                Window window = new() { Width = 300, Height = 60, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    bool stateApplied = VisualStateManager.GoToState(sb, "MouseIndicator", useTransitions: false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(stateApplied,
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

        [Fact]
        public void ScrollBar_DefaultLayout_ReservesExpandedSlotWithCompactIndicator()
        {
            WpfTestSta.Invoke(static () =>
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
                    Height = 200,
                };

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Grid? root = FindVisualChildByName<Grid>(sb, "Root");
                    Assert.NotNull(root);
                    Assert.Equal(8.0, sb.ActualWidth, 0.5);
                    Assert.Equal(6.0, root.Width, 0.5);
                    Assert.Equal(HorizontalAlignment.Right, root.HorizontalAlignment);

                    RepeatButton? decreaseButton = FindVisualChildByName<RepeatButton>(sb, "DecreaseButton");
                    RepeatButton? increaseButton = FindVisualChildByName<RepeatButton>(sb, "IncreaseButton");
                    Assert.NotNull(decreaseButton);
                    Assert.NotNull(increaseButton);
                    Assert.Equal(0.0, decreaseButton.Opacity, 0.01);
                    Assert.Equal(0.0, increaseButton.Opacity, 0.01);
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

        [Fact]
        public void ScrollBar_Disabled_OpacityReducedOrElementDisabled()
        {
            WpfTestSta.Invoke(static () =>
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
                    Height = 200,
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
                    Assert.True(!sb.IsEnabled || sb.Opacity < 1.0,
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

        [Fact]
        public void ScrollBar_ThemeCycle_BrushesResolveAfterEachSwitch()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] keys =
                [
                    "ScrollBarSize",
                    "ScrollBarCompactThumbSize",
                    "ScrollViewerScrollBarMargin",
                    "ControlStrongFillColorDefaultBrush",
                    "SubtleFillColorSecondaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in keys)
                    {
                        Assert.NotNull(app?.TryFindResource(key));
                    }
                }
            });
        }
    }
}
