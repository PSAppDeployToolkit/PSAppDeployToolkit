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

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI 3 parity uplift of the ScrollBar and ScrollViewer templates.
    /// Covers the constant width rail with the resizing thumb, the ScrollingIndicatorStates and
    /// ConsciousStates visual state groups, the auto hide driver in
    /// <see cref="Controls.ScrollBarExtensions"/>, the two by two scroll viewer layout with its corner
    /// separator, and the disabled and high contrast brush swaps.
    /// </summary>
    public partial class ControlTests
    {
        /// <summary>
        /// WinUI ScrollBarSize. The rail is this wide in every state; only the thumb resizes.
        /// </summary>
        private const double ExpectedScrollBarSize = 12.0;

        /// <summary>
        /// WinUI painted thumb width at rest (ScrollBarVerticalThumbMinWidth minus the 6 px stroke).
        /// </summary>
        private const double ExpectedThumbCollapsedSize = 2.0;

        /// <summary>
        /// WinUI painted thumb width while expanded (ScrollBarSize minus the 6 px stroke).
        /// </summary>
        private const double ExpectedThumbExpandedSize = 6.0;

        /// <summary>
        /// WinUI ScrollBarVerticalThumbMinHeight and ScrollBarHorizontalThumbMinWidth.
        /// </summary>
        private const double ExpectedThumbMinLength = 30.0;

        private static ScrollBar CreateStyledScrollBar(Application app, Orientation orientation)
        {
            ScrollBar scrollBar = new()
            {
                Orientation = orientation,
                Style = app.TryFindResource(orientation is Orientation.Vertical
                    ? "VerticalScrollBarStyle"
                    : "HorizontalScrollBarStyle") as Style,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                ViewportSize = 10,
            };

            // Size only along the scroll axis. The cross axis has to stay unset so the style's
            // ScrollBarSize setter is what decides the rail width, which is the thing under test.
            if (orientation is Orientation.Vertical)
            {
                scrollBar.Height = 200;
            }
            else
            {
                scrollBar.Width = 200;
            }

            return scrollBar;
        }

        private static void AssertScrollBarVisualStateDoubleKeyFrame(
            ScrollBar scrollBar,
            string stateName,
            string targetName,
            string targetProperty,
            double expectedValue)
        {
            Grid root = Assert.IsType<Grid>(FindVisualChildByName<Grid>(scrollBar, "Root"), exactMatch: false);

            IList groups = VisualStateManager.GetVisualStateGroups(root);
            Storyboard storyboard = Assert.IsType<Storyboard>(groups.Cast<VisualStateGroup>().SelectMany(static group => group.States.Cast<VisualState>()).FirstOrDefault(candidate => string.Equals(candidate.Name, stateName, StringComparison.Ordinal))?.Storyboard, exactMatch: false);

            foreach (Timeline timeline in storyboard.Children)
            {
                if (timeline is not DoubleAnimationUsingKeyFrames animation ||
                    !string.Equals(Storyboard.GetTargetName(animation), targetName, StringComparison.Ordinal) ||
                    !string.Equals(Storyboard.GetTargetProperty(animation).Path, targetProperty, StringComparison.Ordinal))
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

        /// <summary>
        /// The <see cref="Visibility"/> counterpart of
        /// <see cref="AssertScrollBarVisualStateDoubleKeyFrame"/>: asserts that the named state
        /// drives the named part's Visibility to <paramref name="expectedValue"/>.
        /// </summary>
        /// <param name="scrollBar">The templated scroll bar to inspect.</param>
        /// <param name="stateName">The visual state that should carry the keyframe.</param>
        /// <param name="targetName">The template part the keyframe targets.</param>
        /// <param name="expectedValue">The visibility the state lands on.</param>
        private static void AssertScrollBarVisualStateVisibilityKeyFrame(
            ScrollBar scrollBar,
            string stateName,
            string targetName,
            Visibility expectedValue)
        {
            Grid root = Assert.IsType<Grid>(FindVisualChildByName<Grid>(scrollBar, "Root"), exactMatch: false);

            IList groups = VisualStateManager.GetVisualStateGroups(root);
            Storyboard storyboard = Assert.IsType<Storyboard>(groups.Cast<VisualStateGroup>().SelectMany(static group => group.States.Cast<VisualState>()).FirstOrDefault(candidate => string.Equals(candidate.Name, stateName, StringComparison.Ordinal))?.Storyboard, exactMatch: false);

            foreach (Timeline timeline in storyboard.Children)
            {
                if (timeline is not ObjectAnimationUsingKeyFrames animation ||
                    !string.Equals(Storyboard.GetTargetName(animation), targetName, StringComparison.Ordinal) ||
                    !string.Equals(Storyboard.GetTargetProperty(animation).Path, "Visibility", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.Equal(expectedValue, animation.KeyFrames[0].Value);
                return;
            }

            Assert.Fail(string.Format(
                CultureInfo.InvariantCulture,
                "State {0} must animate {1}.Visibility.",
                stateName,
                targetName));
        }

        // ---------------------------------------------------------------------------
        // ScrollViewer template structure
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ScrollBar_ScrollViewerTemplate_ContainsBothScrollBarPartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollViewer sv = new()
                {
                    Width = 200,
                    Height = 100,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                    Style = app.TryFindResource("ScrollViewerStyle") as Style,
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

                    ScrollBar vertBar = Assert.IsType<ScrollBar>(FindVisualChildByName<ScrollBar>(sv, "PART_VerticalScrollBar"), exactMatch: false);
                    ScrollBar horizBar = Assert.IsType<ScrollBar>(FindVisualChildByName<ScrollBar>(sv, "PART_HorizontalScrollBar"), exactMatch: false);
                    Border separator = Assert.IsType<Border>(FindVisualChildByName<Border>(sv, "ScrollBarSeparator"), exactMatch: false);

                    // The bars sit in their own cells of the two by two grid and therefore stop short
                    // of each other; the separator owns the corner they would otherwise cross in.
                    Assert.Equal(0, Grid.GetRow(vertBar));
                    Assert.Equal(1, Grid.GetColumn(vertBar));
                    Assert.Equal(1, Grid.GetRow(horizBar));
                    Assert.Equal(0, Grid.GetColumn(horizBar));
                    Assert.Equal(1, Grid.GetRow(separator));
                    Assert.Equal(1, Grid.GetColumn(separator));

                    // The content presenter spans the whole grid, so the bars overlay rather than
                    // squeeze the content, matching the WinUI DefaultScrollViewerStyle.
                    ScrollContentPresenter presenter = Assert.IsType<ScrollContentPresenter>(
                        FindVisualChildByName<ScrollContentPresenter>(sv, "PART_ScrollContentPresenter"), exactMatch: false);
                    Assert.Equal(2, Grid.GetRowSpan(presenter));
                    Assert.Equal(2, Grid.GetColumnSpan(presenter));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollViewer_Background_ReachesTheTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                SolidColorBrush background = new(Colors.Magenta);
                background.Freeze();

                ScrollViewer sv = new()
                {
                    Width = 200,
                    Height = 100,
                    Background = background,
                    Style = app.TryFindResource("ScrollViewerStyle") as Style,
                    Content = new TextBlock { Text = "content" },
                };

                Window window = new() { Width = 300, Height = 200, Content = sv };
                try
                {
                    window.Show();
                    sv.UpdateLayout();

                    // The WinUI template paints Background on the layout grid. Dropping that binding
                    // silently swallows ScrollViewer.Background, which is what this guards.
                    Assert.Contains(
                        FindVisualChildren<Grid>(sv),
                        candidate => ReferenceEquals(candidate.Background, background));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // ConsciousStates: the rail stays 12 px, the thumb resizes
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ScrollBar_VSM_Expanded_WidensVerticalThumbAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Vertical);
                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    bool stateApplied = VisualStateManager.GoToState(sb, "Expanded", useTransitions: false);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(stateApplied, "GoToState('Expanded') must return true - ConsciousStates must be present.");

                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Expanded", "VerticalThumb", "Width", ExpectedThumbExpandedSize);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Expanded", "TrackBackground", "Opacity", 1.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Expanded", "DecreaseButton", "Opacity", 1.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Expanded", "IncreaseButton", "Opacity", 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollBar_VSM_Collapsed_NarrowsVerticalThumbAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Vertical);
                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    _ = VisualStateManager.GoToState(sb, "Expanded", useTransitions: false);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    bool stateApplied = VisualStateManager.GoToState(sb, "Collapsed", useTransitions: false);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(stateApplied, "GoToState('Collapsed') must return true - ConsciousStates must be present.");

                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Collapsed", "VerticalThumb", "Width", ExpectedThumbCollapsedSize);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Collapsed", "TrackBackground", "Opacity", 0.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Collapsed", "DecreaseButton", "Opacity", 0.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Collapsed", "IncreaseButton", "Opacity", 0.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollBar_VSM_Expanded_HeightensHorizontalThumbAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Horizontal);
                Window window = new() { Width = 300, Height = 60, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    bool stateApplied = VisualStateManager.GoToState(sb, "Expanded", useTransitions: false);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(stateApplied, "GoToState('Expanded') on a horizontal ScrollBar must return true.");

                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Expanded", "HorizontalThumb", "Height", ExpectedThumbExpandedSize);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Expanded", "DecreaseButton", "Opacity", 1.0);
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "Expanded", "IncreaseButton", "Opacity", 1.0);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollBar_RailWidth_StaysConstantAcrossConsciousStatesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Vertical);
                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Grid root = Assert.IsType<Grid>(FindVisualChildByName<Grid>(sb, "Root"), exactMatch: false);

                    // WinUI keeps the hit target at ScrollBarSize whether the bar is at rest or
                    // expanded; only the painted thumb changes size.
                    Assert.Equal(ExpectedScrollBarSize, root.Width, 0.01);
                    Assert.Equal(ExpectedScrollBarSize, sb.ActualWidth, 0.5);

                    _ = VisualStateManager.GoToState(sb, "Expanded", useTransitions: false);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                    sb.UpdateLayout();

                    Assert.Equal(ExpectedScrollBarSize, root.ActualWidth, 0.5);
                    Assert.Equal(ExpectedScrollBarSize, sb.ActualWidth, 0.5);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollBar_Thumb_HonoursWinUiMinimumLengthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Vertical);

                // A tiny viewport over a very long extent drives the proportional thumb below the
                // WinUI 30 px floor unless Track honours Thumb.MinHeight.
                sb.Maximum = 100000;
                sb.ViewportSize = 1;

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    sb.UpdateLayout();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Thumb thumb = Assert.IsType<Thumb>(FindVisualChildByName<Thumb>(sb, "VerticalThumb"), exactMatch: false);
                    Assert.Equal(ExpectedThumbMinLength, thumb.MinHeight, 0.01);
                    Assert.True(
                        thumb.ActualHeight >= ExpectedThumbMinLength - 0.5,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Thumb must not render shorter than the WinUI minimum of {0} px; measured {1}.",
                            ExpectedThumbMinLength,
                            thumb.ActualHeight));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // ScrollingIndicatorStates and the auto hide driver
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ScrollBar_VSM_NoIndicator_HidesAndDisablesHitTestingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Vertical);

                // This test drives the visual states directly, so the auto hide driver has to be off
                // or it competes for them: a bar with no host ScrollViewer is deliberately pinned to
                // MouseIndicator on Loaded, and it also reveals itself if the pointer happens to be
                // over the test window. Either one races the explicit GoToState below.
                Controls.ScrollBarExtensions.SetIsIndicatorEnabled(sb, value: false);

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Grid mainRoot = Assert.IsType<Grid>(FindVisualChildByName<Grid>(sb, "MainRoot"), exactMatch: false);

                    bool stateApplied = VisualStateManager.GoToState(sb, "NoIndicator", useTransitions: false);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                    Assert.True(stateApplied, "GoToState('NoIndicator') must return true - ScrollingIndicatorStates must be present.");

                    // Asserted against the storyboard definition, not the live property. A
                    // ControlTemplate's VisualStateGroup objects are shared by every ScrollBar built
                    // from that template, and the group's clock bookkeeping goes with them, so while
                    // other bars are alive GoToState can report success without the storyboard
                    // reaching this instance and the authored values survive. Every stable visual
                    // state test in this file reads the definition for that reason.
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "NoIndicator", "MainRoot", "Opacity", 0.0);
                    AssertScrollBarVisualStateVisibilityKeyFrame(sb, "NoIndicator", "MainRoot", Visibility.Collapsed);

                    // The IsHitTestVisible half is a plain template trigger keyed on IndicatorMode,
                    // per instance rather than VSM, so the live value is reliable here. It is what
                    // stops a faded rail from swallowing clicks meant for the content beneath it.
                    Controls.ScrollBarExtensions.SetIndicatorMode(sb, ScrollingIndicatorMode.None);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                    Assert.False(mainRoot.IsHitTestVisible);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollBar_TouchIndicator_ShowsNonInteractivePanningBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Vertical);

                // Driver off for the same reason as the NoIndicator test: it would otherwise pin this
                // host-less bar to MouseIndicator and overwrite the state under assertion.
                Controls.ScrollBarExtensions.SetIsIndicatorEnabled(sb, value: false);

                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Grid mainRoot = Assert.IsType<Grid>(FindVisualChildByName<Grid>(sb, "MainRoot"), exactMatch: false);

                    bool stateApplied = VisualStateManager.GoToState(sb, "TouchIndicator", useTransitions: false);
                    Controls.ScrollBarExtensions.SetIndicatorMode(sb, ScrollingIndicatorMode.TouchIndicator);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(stateApplied, "GoToState('TouchIndicator') must return true.");

                    // Definition-based for the same shared VisualStateGroup reason as the NoIndicator
                    // test: the rail stays visible, like the WinUI VerticalPanningRoot.
                    AssertScrollBarVisualStateDoubleKeyFrame(sb, "TouchIndicator", "MainRoot", "Opacity", 1.0);
                    AssertScrollBarVisualStateVisibilityKeyFrame(sb, "TouchIndicator", "MainRoot", Visibility.Visible);

                    // The trigger half is per instance, so this one reads the live value: the panning
                    // bar offers no drag target.
                    Assert.False(mainRoot.IsHitTestVisible);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollBar_InsideScrollViewer_StartsHiddenAndRevealsOnScrollAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollViewer sv = new()
                {
                    Width = 200,
                    Height = 100,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    Style = app.TryFindResource("ScrollViewerStyle") as Style,
                };

                StackPanel sp = new();
                for (int i = 0; i < 40; i++)
                {
                    _ = sp.Children.Add(new TextBlock { Text = "Item", Height = 20 });
                }
                sv.Content = sp;

                Window window = new() { Width = 300, Height = 200, Content = sv };
                try
                {
                    window.Show();
                    sv.UpdateLayout();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    ScrollBar vertBar = Assert.IsType<ScrollBar>(FindVisualChildByName<ScrollBar>(sv, "PART_VerticalScrollBar"), exactMatch: false);

                    // The style attaches the driver, which hides the bar once it finds its host. The
                    // resting state is only None while the pointer is elsewhere; a test runner whose
                    // cursor happens to sit over the window is a legitimate MouseIndicator, so that
                    // half of the contract is asserted only when the pointer is away.
                    Assert.True(Controls.ScrollBarExtensions.GetIsIndicatorEnabled(vertBar));
                    if (!sv.IsMouseOver)
                    {
                        Assert.Equal(ScrollingIndicatorMode.None, Controls.ScrollBarExtensions.GetIndicatorMode(vertBar));
                    }

                    sv.ScrollToVerticalOffset(120);
                    sv.UpdateLayout();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    // Scrolling reveals the indicator, exactly as the WinUI ScrollViewer does.
                    Assert.Equal(ScrollingIndicatorMode.MouseIndicator, Controls.ScrollBarExtensions.GetIndicatorMode(vertBar));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Disabled and theme behaviour
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ScrollBar_Disabled_DimsRootAndSwapsThumbBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ScrollBar sb = CreateStyledScrollBar(app, Orientation.Vertical);
                Window window = new() { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    _ = sb.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Grid root = Assert.IsType<Grid>(FindVisualChildByName<Grid>(sb, "Root"), exactMatch: false);
                    Border thumbVisual = Assert.IsType<Border>(FindVisualChildByName<Border>(sb, "ThumbVisual"), exactMatch: false);

                    sb.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    // WinUI dims the root to 0.5 and swaps the thumb to ScrollBarThumbFillDisabled
                    // rather than fading a live brush, which is what keeps high contrast legible.
                    Assert.Equal(0.5, root.Opacity, 0.01);
                    Assert.Equal(
                        app.TryFindResource("ControlStrongFillColorDisabledBrush"),
                        thumbVisual.Background);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ScrollBar_ThemeCycle_BrushesResolveAfterEachSwitchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] keys =
                [
                    "ScrollBarSize",
                    "ScrollBarThumbCollapsedSize",
                    "ScrollBarThumbExpandedSize",
                    "ScrollViewerScrollBarMargin",
                    "ScrollBarTrackFillBrush",
                    "ControlStrongFillColorDefaultBrush",
                    "ControlStrongFillColorDisabledBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTransparentBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in keys)
                    {
                        Assert.NotNull(app.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public Task ScrollBar_HighContrast_TrackFillFollowsSystemWindowColorAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: true);

                    // The computed AcrylicBackgroundFillColorDefault token is a fixed black in the high
                    // contrast table, so the track has to come from the live system window color or the
                    // white on black variants render an invisible rail.
                    SolidColorBrush track = Assert.IsType<SolidColorBrush>(app.TryFindResource("ScrollBarTrackFillBrush"), exactMatch: false);
                    Assert.Equal(SystemColors.WindowColor, track.Color);
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Style adoption by surfaces that host a native ScrollViewer
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ScrollViewer_FlyoutPresenter_UsesTheFluentScrollViewerStyleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style expected = Assert.IsType<Style>(app.TryFindResource("ScrollViewerStyle"), exactMatch: false);

                Controls.FlyoutPresenter presenter = new()
                {
                    Style = app.TryFindResource(typeof(Controls.FlyoutPresenter)) as Style,
                    Content = new TextBlock { Text = "content" },
                };

                Window window = new() { Width = 300, Height = 200, Content = presenter };
                try
                {
                    window.Show();
                    presenter.UpdateLayout();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    ScrollViewer inner = Assert.Single(FindVisualChildren<ScrollViewer>(presenter));
                    Assert.Same(expected, inner.Style);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
