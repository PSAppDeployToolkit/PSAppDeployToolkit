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
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the rewritten <see cref="ProgressRing"/> - arc-based indeterminate
    /// animation plus code-driven determinate arc.
    /// </summary>
    public partial class ControlTests
    {
        // ──────────────────────────────────────────────────────────────────────
        // Default values + template part
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Defaults_AreCanonical()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Assert.IsTrue(ring.IsActive, "Default IsActive must be true.");
                Assert.IsTrue(ring.IsIndeterminate, "Default IsIndeterminate must be true.");
                Assert.AreEqual(0.0, ring.Value, "Default Value must be 0.");
                Assert.AreEqual(0.0, ring.Minimum, "Default Minimum must be 0.");
                Assert.AreEqual(100.0, ring.Maximum, "Default Maximum must be 100.");
                Assert.AreEqual(4.0, ring.StrokeThickness, "Default StrokeThickness must be 4.");
            });
        }

        [TestMethod]
        public void ProgressRing_Template_ContainsDeterminateArcPart()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "ProgressRing template must contain PART_DeterminateArc.");
                w.Close();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Indeterminate template - caterpillar arc path
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Indeterminate_TemplateContainsCaterpillarArc()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new() { IsIndeterminate = true, IsActive = true };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);
                WaitForAnimationAndDrain(w.Dispatcher, 200);

                Path? arc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                Assert.IsNotNull(arc, "ProgressRing template must contain PART_IndeterminateArc.");
                Assert.AreEqual(Visibility.Visible, arc.Visibility,
                    "Indeterminate arc should be visible when IsActive=True and IsIndeterminate=True.");
                Assert.IsNotNull(arc.Data,
                    "Indeterminate arc Path.Data should be populated by the caterpillar geometry renderer.");

                Grid? dotHost = FindVisualChildByName<Grid>(ring, "DotHost");
                Assert.IsNull(dotHost, "Default ProgressRing template should no longer use the legacy orbit-dot host.");

                w.Close();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Template settings - diameter + offset match WinUI ProgressRingTemplateSettings
        // diameter = (width × 0.1) + (width ≤ 40 ? 1 : 0)
        // anchor   = (width × 0.5) − diameter
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_TemplateSettings_AtWidth32_MatchWinUiFormula()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new() { Width = 32, Height = 32 };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // 32 × 0.1 + 1 = 4.2 ;  32 × 0.5 − 4.2 = 11.8
                Assert.AreEqual(4.2, ring.EllipseDiameter, 0.001,
                    "EllipseDiameter at Width=32 must be 4.2 ((32×0.1)+1).");
                Assert.AreEqual(11.8, ring.EllipseOffset.Top, 0.001,
                    "EllipseOffset.Top at Width=32 must be 11.8 ((32×0.5)−4.2).");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_TemplateSettings_AtWidth64_DropAdditiveTerm()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new() { Width = 64, Height = 64 };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // 64 × 0.1 + 0 = 6.4 ;  64 × 0.5 − 6.4 = 25.6
                Assert.AreEqual(6.4, ring.EllipseDiameter, 0.001,
                    "EllipseDiameter at Width=64 must be 6.4 (no +1 additive when width > 40).");
                Assert.AreEqual(25.6, ring.EllipseOffset.Top, 0.001,
                    "EllipseOffset.Top at Width=64 must be 25.6.");

                w.Close();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Determinate arc geometry
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Determinate_PathDataIsPopulatedForNonZeroValue()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 50,
                    Minimum = 0,
                    Maximum = 100
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "PART_DeterminateArc must exist.");
                Assert.IsNotNull(arc.Data,
                    "Determinate arc Path.Data must be populated when Value > 0 and IsIndeterminate=false.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_Determinate_PathDataIsNullWhenValueIsZero()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 0,
                    Minimum = 0,
                    Maximum = 100
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "PART_DeterminateArc must exist.");
                Assert.IsNull(arc.Data,
                    "Determinate arc Path.Data must be null when Value=0 (no arc to draw).");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_SwitchToIndeterminate_ClearsArcGeometry()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 75
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc?.Data, "Pre-condition: arc has geometry in determinate mode.");

                ring.IsIndeterminate = true;
                DrainDispatcher(w.Dispatcher);

                Assert.IsNull(arc?.Data,
                    "Switching to indeterminate must clear the determinate arc geometry.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_SwitchToDeterminate_ClearsIndeterminateArcGeometry()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                    Value = 75
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);
                WaitForAnimationAndDrain(w.Dispatcher, 200);

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                Assert.IsNotNull(indeterminateArc, "PART_IndeterminateArc must exist.");
                Assert.IsNotNull(indeterminateArc.Data, "Pre-condition: indeterminate arc has geometry.");

                ring.IsIndeterminate = false;
                DrainDispatcher(w.Dispatcher);

                Assert.IsNull(indeterminateArc.Data,
                    "Switching to determinate must clear the indeterminate arc geometry.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_Unloaded_ClearsIndeterminateArcGeometry()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                    IsActive = true
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);
                WaitForAnimationAndDrain(w.Dispatcher, 200);

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                Assert.IsNotNull(indeterminateArc, "PART_IndeterminateArc must exist.");
                Assert.IsNotNull(indeterminateArc.Data, "Pre-condition: indeterminate arc has geometry.");

                w.Close();
                DrainDispatcher(w.Dispatcher);

                Assert.IsNull(indeterminateArc.Data,
                    "Unloading an active indeterminate ProgressRing must clear repeat-forever animation geometry.");
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Foreground brush honours theme tokens
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Foreground_ResolvesToAccentFillColorDefaultBrush()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? fg = ring.Foreground as SolidColorBrush;
                SolidColorBrush? expected = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;

                Assert.IsNotNull(expected, "AccentFillColorDefaultBrush must resolve.");
                Assert.IsNotNull(fg, "ProgressRing.Foreground must be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, fg.Color,
                    "ProgressRing.Foreground must default to AccentFillColorDefaultBrush.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_PausedState_UsesCautionBrushForBothArcs()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Paused,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "SystemFillColorCautionBrush must resolve.");

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                AssertPathStroke(indeterminateArc, expected, "Paused indeterminate arc should use the caution brush.");

                ring.IsIndeterminate = false;
                ring.Value = 50;
                DrainDispatcher(w.Dispatcher);

                Path? determinateArc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                AssertPathStroke(determinateArc, expected, "Paused determinate arc should use the caution brush.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_PausedState_TracksCautionBrushAcrossThemeChange()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Paused,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                Assert.IsNotNull(indeterminateArc, "PART_IndeterminateArc must exist.");
                SolidColorBrush? initial = indeterminateArc.Stroke as SolidColorBrush;
                Assert.IsNotNull(initial, "Paused indeterminate arc stroke should be a SolidColorBrush.");
                Color initialColor = initial.Color;

                SolidColorBrush? initialExpected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.IsNotNull(initialExpected, "SystemFillColorCautionBrush must resolve in light theme.");
                Assert.AreEqual(initialExpected.Color, initialColor, "Paused ProgressRing should start on the caution brush.");

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "SystemFillColorCautionBrush must resolve after theme change.");
                AssertPathStroke(indeterminateArc, expected, "Paused ProgressRing should track the current caution brush.");
                SolidColorBrush? actual = indeterminateArc.Stroke as SolidColorBrush;
                Assert.IsNotNull(actual, "Paused indeterminate arc stroke should remain a SolidColorBrush.");
                Assert.AreNotEqual(initialColor, actual.Color,
                    "Paused ProgressRing arc should change when the theme caution brush changes.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_IndeterminateAnimation_UsesAvaloniaKeyframes()
        {
            WpfTestSta.Invoke(() =>
            {
                DoubleAnimationUsingKeyFrames start = InvokePrivateAnimationFactory("CreateIndeterminateStartAnimation");
                DoubleAnimationUsingKeyFrames sweep = InvokePrivateAnimationFactory("CreateIndeterminateSweepAnimation");

                Assert.AreEqual(TimeSpan.FromMilliseconds(4000), start.Duration.TimeSpan,
                    "Indeterminate start animation should use the configured 4 second cadence.");
                Assert.AreEqual(TimeSpan.FromMilliseconds(4000), sweep.Duration.TimeSpan,
                    "Indeterminate sweep animation should use the configured 4 second cadence.");
                Assert.AreEqual(RepeatBehavior.Forever, start.RepeatBehavior);
                Assert.AreEqual(RepeatBehavior.Forever, sweep.RepeatBehavior);

                AssertKeyFrames(start,
                [
                    -720.0, -540.0, -360.0, -180.0, 0.0, 180.0, 360.0, 540.0, 720.0
                ]);
                AssertKeyFrames(sweep,
                [
                    0.0, 50.0, 100.0, 50.0, 5.0, 50.0, 100.0, 50.0, 0.0
                ]);
                AssertKeyFramePercents(start,
                [
                    0.0, 0.125, 0.25, 0.325, 0.5, 0.625, 0.75, 0.875, 1.0
                ]);
                AssertKeyFramePercents(sweep,
                [
                    0.0, 0.125, 0.25, 0.325, 0.5, 0.625, 0.75, 0.875, 1.0
                ]);
            });
        }

        [TestMethod]
        public void ProgressRing_PausedState_RendersStaticIndeterminateArc()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Paused,
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                Assert.IsNotNull(indeterminateArc, "PART_IndeterminateArc must exist.");
                Assert.AreEqual(Visibility.Visible, indeterminateArc.Visibility,
                    "Paused indeterminate ProgressRing should remain visible.");
                Assert.IsNotNull(indeterminateArc.Data,
                    "Paused indeterminate ProgressRing should render a static arc.");

                Rect initialBounds = indeterminateArc.Data.Bounds;
                WaitForAnimationAndDrain(w.Dispatcher, 400);
                Rect laterBounds = indeterminateArc.Data.Bounds;

                Assert.AreEqual(initialBounds.X, laterBounds.X, 0.01,
                    "Paused ProgressRing should not animate the arc position.");
                Assert.AreEqual(initialBounds.Y, laterBounds.Y, 0.01,
                    "Paused ProgressRing should not animate the arc position.");
                Assert.AreEqual(initialBounds.Width, laterBounds.Width, 0.01,
                    "Paused ProgressRing should not animate the arc shape.");
                Assert.AreEqual(initialBounds.Height, laterBounds.Height, 0.01,
                    "Paused ProgressRing should not animate the arc shape.");
                AssertDependencyPropertyNotAnimated(ring, "IndeterminateStartAngleProperty",
                    "Paused ProgressRing should not have an active start-angle animation clock.");
                AssertDependencyPropertyNotAnimated(ring, "IndeterminateSweepAngleProperty",
                    "Paused ProgressRing should not have an active sweep-angle animation clock.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_ErrorState_UsesCriticalBrushThroughThemeCycle()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new()
                {
                    ProgressState = ProgressRingState.Error,
                    IsActive = true,
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 50
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorCriticalBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "SystemFillColorCriticalBrush must resolve.");

                Path? determinateArc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                AssertPathStroke(determinateArc, expected, "Error determinate arc should use the critical brush.");

                ring.IsIndeterminate = true;
                DrainDispatcher(w.Dispatcher);

                Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                AssertPathStroke(indeterminateArc, expected, "Error indeterminate arc should use the critical brush.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_ThemeCycle_TemplateRemainsApplied()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ProgressRing ring = new();
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                Path? arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "PART_DeterminateArc must still exist after theme cycle.");

                w.Close();
            });
        }

        private static void AssertPathStroke(Path? path, SolidColorBrush expected, string message)
        {
            Assert.IsNotNull(path, "Expected template path to exist.");
            SolidColorBrush? actual = path.Stroke as SolidColorBrush;
            Assert.IsNotNull(actual, "Path stroke should be a SolidColorBrush.");
            Assert.AreEqual(expected.Color, actual.Color, message);
        }

        private static DoubleAnimationUsingKeyFrames InvokePrivateAnimationFactory(string methodName)
        {
            MethodInfo? method = typeof(ProgressRing).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "ProgressRing should expose private factory: " + methodName);
            DoubleAnimationUsingKeyFrames? animation = method.Invoke(null, null) as DoubleAnimationUsingKeyFrames;
            Assert.IsNotNull(animation, methodName + " should return keyframe animation.");
            return animation;
        }

        private static void AssertDependencyPropertyNotAnimated(ProgressRing ring, string fieldName, string message)
        {
            FieldInfo? field = typeof(ProgressRing).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "ProgressRing should expose private dependency property field: " + fieldName);
            DependencyProperty? property = field.GetValue(null) as DependencyProperty;
            Assert.IsNotNull(property, fieldName + " should be a dependency property.");

            double currentValue = (double)ring.GetValue(property);
            double baseValue = (double)ring.GetAnimationBaseValue(property);
            Assert.AreEqual(baseValue, currentValue, 0.01, message);
        }

        private static void AssertKeyFrames(DoubleAnimationUsingKeyFrames animation, double[] expectedValues)
        {
            Assert.AreEqual(expectedValues.Length, animation.KeyFrames.Count, "Unexpected keyframe count.");
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.AreEqual(expectedValues[i], animation.KeyFrames[i].Value, 0.01, "Unexpected keyframe value at index " + i);
                Assert.IsInstanceOfType(animation.KeyFrames[i], typeof(LinearDoubleKeyFrame),
                    "ProgressRing indeterminate keyframes should be linear.");
            }
        }

        private static void AssertKeyFramePercents(DoubleAnimationUsingKeyFrames animation, double[] expectedPercents)
        {
            Assert.AreEqual(expectedPercents.Length, animation.KeyFrames.Count, "Unexpected keyframe count.");
            for (int i = 0; i < expectedPercents.Length; i++)
            {
                Assert.AreEqual(KeyTimeType.Percent, animation.KeyFrames[i].KeyTime.Type, "KeyTime should be percent at index " + i);
                Assert.AreEqual(expectedPercents[i], animation.KeyFrames[i].KeyTime.Percent, 0.001, "Unexpected keyframe percent at index " + i);
            }
        }
    }
}
