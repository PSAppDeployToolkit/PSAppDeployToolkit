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
using System;
using System.Windows;
using System.Windows.Media;
using FluenceProgressBar = Fluence.Wpf.Controls.ProgressBar;
using WpfBorder = System.Windows.Controls.Border;
using WpfContentControl = System.Windows.Controls.ContentControl;
using WpfGrid = System.Windows.Controls.Grid;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void ProgressBar_PausedMode_UsesCautionBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Paused, "SystemFillColorCautionBrush");
        }

        [TestMethod]
        public void ProgressBar_PausedMode_TracksCautionBrushAcrossThemeChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = ProgressBarMode.Paused,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");
                SolidColorBrush? initial = fill.Background as SolidColorBrush;
                Assert.IsNotNull(initial, "PART_Fill.Background should be a SolidColorBrush.");
                Color initialColor = initial.Color;

                SolidColorBrush? initialExpected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.IsNotNull(initialExpected, "SystemFillColorCautionBrush must resolve in light theme.");
                Assert.AreEqual(initialExpected.Color, initialColor, "Paused ProgressBar should start on the caution brush.");

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "SystemFillColorCautionBrush must resolve after theme change.");
                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should remain a SolidColorBrush after theme change.");
                Assert.AreEqual(expected.Color, actual.Color, "Paused ProgressBar should track the current caution brush.");
                Assert.AreNotEqual(initialColor, actual.Color, "Paused ProgressBar fill should change when the theme caution brush changes.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_ErrorMode_UsesCriticalBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Error, "SystemFillColorCriticalBrush");
        }

        [TestMethod]
        public void ProgressBar_DefaultStyle_UsesWinUiThinTrackMetrics()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? track = FindVisualChildByName<WpfBorder>(progressBar, "PART_Track");
                Assert.IsNotNull(track, "ProgressBar template must expose PART_Track.");

                Assert.AreEqual(1.0, progressBar.TrackHeight, 0.1,
                    "ProgressBar should default to the WinUI 3 thin 1px baseline track (ProgressBarTrackHeight = 1).");
                Assert.AreEqual(1.0, track.Height, 0.1,
                    "ProgressBar track template height should follow the 1px TrackHeight.");
                Assert.AreEqual(3.2, progressBar.MinHeight, 0.1,
                    "ProgressBar default style should set the configured ProgressBarMinHeight of 3.2.");
                Assert.AreEqual(new CornerRadius(1.5), progressBar.CornerRadius,
                    "ProgressBar indicator should default to the WinUI 3 corner radius of 1.5.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_ReturningToStandardMode_RestoresAccentBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = ProgressBarMode.Error,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");

                progressBar.ProgressMode = ProgressBarMode.Standard;
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "AccentFillColorDefaultBrush must resolve.");
                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, actual.Color,
                    "ProgressBar should restore the accent brush when returning to Standard mode.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_IndicatorHost_IsClippedToRoundedGeometry()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    ProgressMode = ProgressBarMode.Indeterminate,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfGrid? host = FindVisualChildByName<WpfGrid>(progressBar, "ProgressBarIndicatorHost");
                Assert.IsNotNull(host, "ProgressBar template must expose the ProgressBarIndicatorHost panel.");

                // ClipToBounds only clips rectangularly, so the translating indeterminate bars would
                // show square ends at the control edge. The control must install a rounded RectangleGeometry
                // clip matching CornerRadius so every fill/indeterminate child conforms to the rounded indicator.
                RectangleGeometry? clip = host.Clip as RectangleGeometry;
                Assert.IsNotNull(clip, "ProgressBarIndicatorHost must carry a rounded RectangleGeometry clip (not just ClipToBounds).");
                Assert.AreEqual(progressBar.CornerRadius.TopLeft, clip.RadiusX, 0.01,
                    "Indicator host clip corner radius X must match the control CornerRadius.");
                Assert.AreEqual(progressBar.CornerRadius.TopLeft, clip.RadiusY, 0.01,
                    "Indicator host clip corner radius Y must match the control CornerRadius.");
                Assert.IsTrue(clip.Rect.Width > 0 && clip.Rect.Height > 0,
                    "Indicator host clip must be sized to the realised host bounds.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_IndeterminateBars_UseWinUiWidthRatios()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    ProgressMode = ProgressBarMode.Indeterminate,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? track = FindVisualChildByName<WpfBorder>(progressBar, "PART_Track");
                WpfBorder? bar1 = FindVisualChildByName<WpfBorder>(progressBar, "PART_IndeterminateBar");
                WpfBorder? bar2 = FindVisualChildByName<WpfBorder>(progressBar, "PART_IndeterminateBar2");
                Assert.IsNotNull(track, "ProgressBar template must expose PART_Track.");
                Assert.IsNotNull(bar1, "ProgressBar template must expose PART_IndeterminateBar.");
                Assert.IsNotNull(bar2, "ProgressBar template must expose PART_IndeterminateBar2.");

                double trackWidth = track.ActualWidth;
                Assert.IsTrue(trackWidth > 0, "Track must have a realised width.");
                Assert.AreEqual(trackWidth * 0.4, bar1.Width, 0.5,
                    "Primary indeterminate bar must be 0.4 * track width (WinUI 3 ratio).");
                Assert.AreEqual(trackWidth * 0.6, bar2.Width, 0.5,
                    "Secondary indeterminate bar must be 0.6 * track width (WinUI 3 ratio).");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_IsIndeterminate_ShowsIndeterminateBars()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? track = FindVisualChildByName<WpfBorder>(progressBar, "PART_Track");
                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                WpfBorder? bar1 = FindVisualChildByName<WpfBorder>(progressBar, "PART_IndeterminateBar");
                WpfBorder? bar2 = FindVisualChildByName<WpfBorder>(progressBar, "PART_IndeterminateBar2");
                Assert.IsNotNull(track, "ProgressBar template must expose PART_Track.");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");
                Assert.IsNotNull(bar1, "ProgressBar template must expose PART_IndeterminateBar.");
                Assert.IsNotNull(bar2, "ProgressBar template must expose PART_IndeterminateBar2.");

                progressBar.IsIndeterminate = true;
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(Visibility.Visible, bar1.Visibility,
                    "Setting the inherited IsIndeterminate must show the primary indeterminate bar.");
                Assert.AreEqual(Visibility.Visible, bar2.Visibility,
                    "Setting the inherited IsIndeterminate must show the secondary indeterminate bar.");
                Assert.AreEqual(Visibility.Collapsed, fill.Visibility,
                    "Setting the inherited IsIndeterminate must hide the determinate fill.");
                Assert.AreEqual(0.0, track.Opacity, 0.001,
                    "The baseline track must be hidden while indeterminate (WinUI 3 behavior).");

                progressBar.IsIndeterminate = false;
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(Visibility.Collapsed, bar1.Visibility,
                    "Clearing IsIndeterminate must hide the primary indeterminate bar.");
                Assert.AreEqual(Visibility.Visible, fill.Visibility,
                    "Clearing IsIndeterminate must restore the determinate fill.");
                Assert.AreEqual(1.0, track.Opacity, 0.001,
                    "The baseline track must be restored when leaving the indeterminate state.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_ShowError_UsesCriticalBrush()
        {
            AssertProgressBarStatePrimitiveBrush(static bar => bar.ShowError = true, "SystemFillColorCriticalBrush");
        }

        [TestMethod]
        public void ProgressBar_ShowPaused_UsesCautionBrush()
        {
            AssertProgressBarStatePrimitiveBrush(static bar => bar.ShowPaused = true, "SystemFillColorCautionBrush");
        }

        [TestMethod]
        public void ProgressBar_Indeterminate_StopsAnimationOnUnloadAndRestartsOnReload()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    IsIndeterminate = true,
                };
                WpfContentControl host = new() { Content = progressBar };
                Window w = new() { Content = host, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TranslateTransform? translate =
                    progressBar.Template.FindName("PART_IndeterminateTranslate", progressBar) as TranslateTransform;
                TranslateTransform? translate2 =
                    progressBar.Template.FindName("PART_IndeterminateTranslate2", progressBar) as TranslateTransform;
                Assert.IsNotNull(translate, "ProgressBar template must expose PART_IndeterminateTranslate.");
                Assert.IsNotNull(translate2, "ProgressBar template must expose PART_IndeterminateTranslate2.");
                Assert.IsTrue(WaitUntil(w.Dispatcher, 2000, () => translate.HasAnimatedProperties),
                    "The indeterminate animation must run while the bar is loaded.");

                host.Content = null;
                DrainDispatcher(w.Dispatcher);

                Assert.IsFalse(translate.HasAnimatedProperties,
                    "Unloading must stop the repeat-forever animation on the primary translate transform.");
                Assert.IsFalse(translate2.HasAnimatedProperties,
                    "Unloading must stop the repeat-forever animation on the secondary translate transform.");

                host.Content = progressBar;
                DrainDispatcher(w.Dispatcher);

                Assert.IsTrue(WaitUntil(w.Dispatcher, 2000, () => translate.HasAnimatedProperties),
                    "Reloading must restart the indeterminate animation.");

                w.Close();
                DrainDispatcher(w.Dispatcher);

                Assert.IsFalse(translate.HasAnimatedProperties,
                    "Closing the hosting window must leave no active animation clocks on the translate transforms.");
                Assert.IsFalse(translate2.HasAnimatedProperties,
                    "Closing the hosting window must leave no active animation clocks on the secondary translate transform.");
            });
        }

        [TestMethod]
        public void ProgressBar_IndeterminateMode_SetsIsIndeterminate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    ProgressMode = ProgressBarMode.Indeterminate,
                };
                Assert.IsTrue(progressBar.IsIndeterminate,
                    "ProgressMode.Indeterminate must map onto the inherited IsIndeterminate primitive.");

                progressBar.ProgressMode = ProgressBarMode.Standard;
                Assert.IsFalse(progressBar.IsIndeterminate,
                    "ProgressMode.Standard must clear the inherited IsIndeterminate primitive.");
            });
        }

        private static void AssertProgressBarStatePrimitiveBrush(Action<FluenceProgressBar> applyState, string brushKey)
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                applyState(progressBar);
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");

                SolidColorBrush? expected = app?.TryFindResource(brushKey) as SolidColorBrush;
                Assert.IsNotNull(expected, brushKey + " must resolve.");

                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, actual.Color, "ProgressBar fill should use the requested state primitive brush.");

                w.Close();
            });
        }

        private static void AssertProgressBarModeBrush(ProgressBarMode mode, string brushKey)
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = mode,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");

                SolidColorBrush? expected = app?.TryFindResource(brushKey) as SolidColorBrush;
                Assert.IsNotNull(expected, brushKey + " must resolve.");

                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, actual.Color, "ProgressBar fill should use the requested state brush.");

                w.Close();
            });
        }
    }
}
