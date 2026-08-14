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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the rewritten <see cref="ProgressRing"/> - WinUI 3 arc-length
    /// pulse + rotation indeterminate animation plus code-driven determinate arc.
    /// </summary>
    public partial class ControlTests
    {
        // ----------------------------------------------------------------------
        // Default values + template part
        // ----------------------------------------------------------------------

        [Fact]
        public Task ProgressRing_Defaults_AreCanonicalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Assert.True(ring.IsActive, "Default IsActive must be true.");
                Assert.True(ring.IsIndeterminate, "Default IsIndeterminate must be true.");
                Assert.Equal(0.0, ring.Value);
                Assert.Equal(0.0, ring.Minimum);
                Assert.Equal(100.0, ring.Maximum);
                Assert.Equal(4.0, ring.StrokeThickness);
                Assert.Equal(ProgressRingState.Normal, ring.ProgressState);
                Assert.False(ring.ShowError, "Default ShowError must be false.");
                Assert.False(ring.ShowPaused, "Default ShowPaused must be false.");
            });
        }

        [Fact]
        public Task ProgressRing_Template_ContainsDeterminateArcPartAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path arc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_DeterminateArc"));
                w.Close();
            });
        }

        // ----------------------------------------------------------------------
        // Indeterminate template - pulsing arc path + rotate transform
        // ----------------------------------------------------------------------

        [Fact]
        public Task ProgressRing_Indeterminate_TemplateContainsAnimatedArcAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new() { IsIndeterminate = true, IsActive = true };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                await WaitForAnimationAndDrainAsync(w.Dispatcher, 200).ConfigureAwait(true);

                Path arc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_IndeterminateArc"));
                Assert.Equal(Visibility.Visible, arc.Visibility);
                Assert.NotNull(arc.Data);
                Assert.Equal(new Point(0.5, 0.5), arc.RenderTransformOrigin);

                RotateTransform rotate = Assert.IsAssignableFrom<RotateTransform>(GetIndeterminateRotateTransform(ring));
                Assert.True(rotate.HasAnimatedProperties,
                    "Active indeterminate ProgressRing should animate the template rotate transform.");

                Grid? dotHost = FindVisualChildByName<Grid>(ring, "DotHost");
                Assert.Null(dotHost);

                w.Close();
            });
        }

        // ----------------------------------------------------------------------
        // Template settings - diameter + offset match WinUI ProgressRingTemplateSettings
        // diameter = (width × 0.1) + (width ≤ 40 ? 1 : 0)
        // anchor   = (width × 0.5) - diameter
        // ----------------------------------------------------------------------

        [Fact]
        public Task ProgressRing_TemplateSettings_AtWidth32_MatchWinUiFormulaAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new() { Width = 32, Height = 32 };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // 32 × 0.1 + 1 = 4.2 ;  32 × 0.5 - 4.2 = 11.8
                Assert.Equal(4.2, ring.EllipseDiameter, 0.001);
                Assert.Equal(11.8, ring.EllipseOffset.Top, 0.001);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_TemplateSettings_AtWidth64_DropAdditiveTermAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new() { Width = 64, Height = 64 };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // 64 × 0.1 + 0 = 6.4 ;  64 × 0.5 - 6.4 = 25.6
                Assert.Equal(6.4, ring.EllipseDiameter, 0.001);
                Assert.Equal(25.6, ring.EllipseOffset.Top, 0.001);

                w.Close();
            });
        }

        // ----------------------------------------------------------------------
        // Determinate arc geometry
        // ----------------------------------------------------------------------

        [Fact]
        public Task ProgressRing_Determinate_PathDataIsPopulatedForNonZeroValueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 50,
                    Minimum = 0,
                    Maximum = 100,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path arc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_DeterminateArc"));
                Assert.NotNull(arc.Data);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_Determinate_PathDataIsNullWhenValueIsZeroAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 0,
                    Minimum = 0,
                    Maximum = 100,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path arc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_DeterminateArc"));
                Assert.Null(arc.Data);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_SwitchToIndeterminate_ClearsArcGeometryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 75,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path? arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.NotNull(arc?.Data);

                ring.IsIndeterminate = true;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Null(arc?.Data);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_SwitchToDeterminate_ClearsIndeterminateArcGeometryAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                    Value = 75,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                await WaitForAnimationAndDrainAsync(w.Dispatcher, 200).ConfigureAwait(true);

                Path indeterminateArc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_IndeterminateArc"));
                Assert.NotNull(indeterminateArc.Data);

                ring.IsIndeterminate = false;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Null(indeterminateArc.Data);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_Unloaded_ClearsIndeterminateArcGeometryAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                    IsActive = true,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                await WaitForAnimationAndDrainAsync(w.Dispatcher, 200).ConfigureAwait(true);

                Path indeterminateArc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_IndeterminateArc"));
                Assert.NotNull(indeterminateArc.Data);

                w.Close();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Null(indeterminateArc.Data);
            });
        }

        // ----------------------------------------------------------------------
        // Foreground brush honours theme tokens
        // ----------------------------------------------------------------------

        [Fact]
        public Task ProgressRing_Foreground_ResolvesToAccentFillColorDefaultBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush fg = Assert.IsType<SolidColorBrush>(ring.Foreground);
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("AccentFillColorDefaultBrush"));

                Assert.Equal(expected.Color, fg.Color);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_PausedState_UsesCautionBrushForBothArcsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Paused,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorCautionBrush"));

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                AssertPathStroke(indeterminateArc, expected);

                ring.IsIndeterminate = false;
                ring.Value = 50;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path? determinateArc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                AssertPathStroke(determinateArc, expected);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_PausedState_TracksCautionBrushAcrossThemeChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Paused,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path indeterminateArc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_IndeterminateArc"));
                SolidColorBrush initial = Assert.IsType<SolidColorBrush>(indeterminateArc.Stroke);
                Color initialColor = initial.Color;

                SolidColorBrush initialExpected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorCautionBrush"));
                Assert.Equal(initialExpected.Color, initialColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorCautionBrush"));
                AssertPathStroke(indeterminateArc, expected);
                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(indeterminateArc.Stroke);
                Assert.NotEqual(initialColor, actual.Color);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_IndeterminateAnimation_UsesArcPulseAndRotationModelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                DoubleAnimationUsingKeyFrames sweep =
                    InvokePrivateAnimationFactory<DoubleAnimationUsingKeyFrames>("CreateIndeterminateSweepAnimation");
                DoubleAnimation rotation =
                    InvokePrivateAnimationFactory<DoubleAnimation>("CreateIndeterminateRotationAnimation");

                // Arc-length pulse: sweep fraction 0 -> 0.5 -> 0 over a 2 second linear
                // cycle; percents 0 / 0.5 / 1.0 of the 2000 ms duration are 0 s / 1 s / 2 s.
                Assert.Equal(TimeSpan.FromMilliseconds(2000), sweep.Duration.TimeSpan);
                Assert.Equal(RepeatBehavior.Forever, sweep.RepeatBehavior);
                AssertKeyFrames(sweep,
                [
                    0.0, 0.5, 0.0,
                ]);
                AssertKeyFramePercents(sweep,
                [
                    0.0, 0.5, 1.0,
                ]);

                // Rotation: the template transform spins 90 -> 1170 degrees (three full
                // turns) per 2 second cycle with no easing.
                Assert.Equal(TimeSpan.FromMilliseconds(2000), rotation.Duration.TimeSpan);
                Assert.Equal(RepeatBehavior.Forever, rotation.RepeatBehavior);
                _ = Assert.NotNull(rotation.From);
                Assert.Equal(90.0, rotation.From.Value, 0.001);
                _ = Assert.NotNull(rotation.To);
                Assert.Equal(1170.0, rotation.To.Value, 0.001);
                Assert.Null(rotation.EasingFunction);
            });
        }

        [Fact]
        public Task ProgressRing_PausedState_RendersStaticIndeterminateArcAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Paused,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path indeterminateArc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_IndeterminateArc"));
                Assert.Equal(Visibility.Visible, indeterminateArc.Visibility);
                Assert.NotNull(indeterminateArc.Data);

                Rect initialBounds = indeterminateArc.Data.Bounds;
                await WaitForAnimationAndDrainAsync(w.Dispatcher, 400).ConfigureAwait(true);
                Rect laterBounds = indeterminateArc.Data.Bounds;

                Assert.Equal(initialBounds.X, laterBounds.X, 0.01);
                Assert.Equal(initialBounds.Y, laterBounds.Y, 0.01);
                Assert.Equal(initialBounds.Width, laterBounds.Width, 0.01);
                Assert.Equal(initialBounds.Height, laterBounds.Height, 0.01);
                AssertDependencyPropertyNotAnimated(ring, "IndeterminateSweepFractionProperty");

                RotateTransform rotate = Assert.IsAssignableFrom<RotateTransform>(GetIndeterminateRotateTransform(ring));
                Assert.False(rotate.HasAnimatedProperties,
                    "Paused ProgressRing must not run the rotation animation.");
                Assert.Equal(90.0, rotate.Angle, 0.01);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_ErrorState_UsesCriticalBrushThroughThemeCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Error,
                    IsActive = true,
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 50,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorCriticalBrush"));

                Path? determinateArc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                AssertPathStroke(determinateArc, expected);

                ring.IsIndeterminate = true;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                AssertPathStroke(indeterminateArc, expected);

                w.Close();
            });
        }

        // ----------------------------------------------------------------------
        // Orthogonal ShowError / ShowPaused flags + ProgressState alias
        // ----------------------------------------------------------------------

        [Fact]
        public Task ProgressRing_ShowError_ColorsArcsWithCriticalBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ShowError = true,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(ProgressRingState.Normal, ring.ProgressState);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorCriticalBrush"));

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                AssertPathStroke(indeterminateArc, expected);

                RotateTransform rotate = Assert.IsAssignableFrom<RotateTransform>(GetIndeterminateRotateTransform(ring));
                Assert.True(rotate.HasAnimatedProperties,
                    "ShowError should keep the indeterminate arc spinning.");

                ring.IsIndeterminate = false;
                ring.Value = 50;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path? determinateArc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                AssertPathStroke(determinateArc, expected);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_ShowPaused_RendersStaticCautionHalfArcAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ShowPaused = true,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(ProgressRingState.Normal, ring.ProgressState);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorCautionBrush"));

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                AssertPathStroke(indeterminateArc, expected);
                Assert.NotNull(indeterminateArc?.Data);

                double sweepFraction = GetPrivateDoubleDependencyPropertyValue(ring, "IndeterminateSweepFractionProperty");
                Assert.Equal(0.5, sweepFraction, 0.001);
                AssertDependencyPropertyNotAnimated(ring, "IndeterminateSweepFractionProperty");

                RotateTransform rotate = Assert.IsAssignableFrom<RotateTransform>(GetIndeterminateRotateTransform(ring));
                Assert.False(rotate.HasAnimatedProperties,
                    "ShowPaused must not rotate the indeterminate arc.");
                Assert.Equal(90.0, rotate.Angle, 0.01);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_Indeterminate_StopsAnimationWhenCollapsedAndRestartsWhenVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                RotateTransform rotate = Assert.IsAssignableFrom<RotateTransform>(GetIndeterminateRotateTransform(ring));
                Assert.True(await WaitUntilAsync(w.Dispatcher, 2000, () => rotate.HasAnimatedProperties).ConfigureAwait(true),
                    "The indeterminate animation must run while the ring is loaded and visible.");

                ring.Visibility = Visibility.Collapsed;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.False(rotate.HasAnimatedProperties,
                    "Collapsing the ring must stop the repeat-forever rotation animation.");
                AssertDependencyPropertyNotAnimated(ring, "IndeterminateSweepFractionProperty");

                ring.Visibility = Visibility.Visible;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.True(await WaitUntilAsync(w.Dispatcher, 2000, () => rotate.HasAnimatedProperties).ConfigureAwait(true),
                    "Restoring visibility must restart the indeterminate animation.");

                w.Close();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.False(rotate.HasAnimatedProperties,
                    "Closing the hosting window must leave no active rotation animation clocks.");
            });
        }

        [Fact]
        public Task ProgressRing_ProgressStateAlias_MapsOntoStateFlagsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ring.ProgressState = ProgressRingState.Error;
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                Assert.True(ring.ShowError, "ProgressState=Error must set ShowError.");
                Assert.False(ring.ShowPaused, "ProgressState=Error must clear ShowPaused.");

                SolidColorBrush critical = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorCriticalBrush"));
                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                AssertPathStroke(indeterminateArc, critical);

                ring.ProgressState = ProgressRingState.Paused;
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                Assert.True(ring.ShowPaused, "ProgressState=Paused must set ShowPaused.");
                Assert.False(ring.ShowError, "ProgressState=Paused must clear ShowError.");

                ring.ProgressState = ProgressRingState.Normal;
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                Assert.False(ring.ShowPaused, "ProgressState=Normal must clear ShowPaused.");
                Assert.False(ring.ShowError, "ProgressState=Normal must clear ShowError.");

                SolidColorBrush accent = Assert.IsType<SolidColorBrush>(app?.TryFindResource("AccentFillColorDefaultBrush"));
                AssertPathStroke(indeterminateArc, accent);

                w.Close();
            });
        }

        [Fact]
        public Task ProgressRing_ThemeCycle_TemplateRemainsAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Path arc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_DeterminateArc"));

                w.Close();
            });
        }

        // ----------------------------------------------------------------------
        // Live region + RangeValue accessibility
        // ----------------------------------------------------------------------

        [Fact]
        public Task ProgressRing_DeclaresPoliteLiveSettingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new() { Width = 64, Height = 64 };
                Window window = new() { Content = ring };
                window.Show();
                _ = ring.ApplyTemplate();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(ring));
                window.Close();
            });
        }

        private static void AssertPathStroke(Path? path, SolidColorBrush expected)
        {
            Assert.NotNull(path);
            SolidColorBrush actual = Assert.IsType<SolidColorBrush>(path.Stroke);
            Assert.Equal(expected.Color, actual.Color);
        }

        private static T InvokePrivateAnimationFactory<T>(string methodName)
            where T : AnimationTimeline
        {
            MethodInfo method = Assert.IsAssignableFrom<MethodInfo>(typeof(ProgressRing).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic));
            return Assert.IsAssignableFrom<T>(method.Invoke(null, parameters: null));
        }

        private static RotateTransform? GetIndeterminateRotateTransform(ProgressRing ring)
        {
            return ring.Template?.FindName("PART_IndeterminateRotate", ring) as RotateTransform;
        }

        private static DependencyProperty GetPrivateDependencyProperty(string fieldName)
        {
            FieldInfo field = Assert.IsAssignableFrom<FieldInfo>(typeof(ProgressRing).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic));
            return Assert.IsType<DependencyProperty>(field.GetValue(null));
        }

        private static double GetPrivateDoubleDependencyPropertyValue(ProgressRing ring, string fieldName)
        {
            DependencyProperty property = GetPrivateDependencyProperty(fieldName);
            return (double)ring.GetValue(property);
        }

        private static void AssertDependencyPropertyNotAnimated(ProgressRing ring, string fieldName)
        {
            DependencyProperty property = GetPrivateDependencyProperty(fieldName);
            double currentValue = (double)ring.GetValue(property);
            double baseValue = (double)ring.GetAnimationBaseValue(property);
            Assert.Equal(baseValue, currentValue, 0.01);
        }

        private static void AssertKeyFrames(DoubleAnimationUsingKeyFrames animation, double[] expectedValues)
        {
            Assert.Equal(expectedValues.Length, animation.KeyFrames.Count);
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedValues[i], animation.KeyFrames[i].Value, 0.01);
                _ = Assert.IsAssignableFrom<LinearDoubleKeyFrame>(animation.KeyFrames[i]);
            }
        }

        private static void AssertKeyFramePercents(DoubleAnimationUsingKeyFrames animation, double[] expectedPercents)
        {
            Assert.Equal(expectedPercents.Length, animation.KeyFrames.Count);
            for (int i = 0; i < expectedPercents.Length; i++)
            {
                Assert.Equal(KeyTimeType.Percent, animation.KeyFrames[i].KeyTime.Type);
                Assert.Equal(expectedPercents[i], animation.KeyFrames[i].KeyTime.Percent, 0.001);
            }
        }
    }
}
