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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public void ProgressBar_PausedMode_UsesCautionBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Paused, "SystemFillColorCautionBrush");
        }

        [Fact]
        public void ProgressBar_PausedMode_TracksCautionBrushAcrossThemeChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = ProgressBarMode.Paused,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? fill = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                Assert.NotNull(fill);
                SolidColorBrush? initial = fill.Background as SolidColorBrush;
                Assert.NotNull(initial);
                Color initialColor = initial.Color;

                SolidColorBrush? initialExpected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.NotNull(initialExpected);
                Assert.Equal(initialExpected.Color, initialColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.NotNull(expected);
                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.NotNull(actual);
                Assert.Equal(expected.Color, actual.Color);
                Assert.NotEqual(initialColor, actual.Color);

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_ErrorMode_UsesCriticalBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Error, "SystemFillColorCriticalBrush");
        }

        [Fact]
        public void ProgressBar_DefaultStyle_UsesWinUiThinTrackMetrics()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? track = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Track");
                Assert.NotNull(track);

                Assert.Equal(1.0, progressBar.TrackHeight, 0.1);
                Assert.Equal(1.0, track.Height, 0.1);
                Assert.Equal(3.2, progressBar.MinHeight, 0.1);
                Assert.Equal(new CornerRadius(1.5), progressBar.CornerRadius);

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_ReturningToStandardMode_RestoresAccentBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = ProgressBarMode.Error,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? fill = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                Assert.NotNull(fill);

                progressBar.ProgressMode = ProgressBarMode.Standard;
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;
                Assert.NotNull(expected);
                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.NotNull(actual);
                Assert.Equal(expected.Color, actual.Color);

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_IndicatorHost_IsClippedToRoundedGeometry()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    ProgressMode = ProgressBarMode.Indeterminate,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Grid? host = FindVisualChildByName<System.Windows.Controls.Grid>(progressBar, "ProgressBarIndicatorHost");
                Assert.NotNull(host);

                // ClipToBounds only clips rectangularly, so the translating indeterminate bars would
                // show square ends at the control edge. The control must install a rounded RectangleGeometry
                // clip matching CornerRadius so every fill/indeterminate child conforms to the rounded indicator.
                RectangleGeometry? clip = host.Clip as RectangleGeometry;
                Assert.NotNull(clip);
                Assert.Equal(progressBar.CornerRadius.TopLeft, clip.RadiusX, 0.01);
                Assert.Equal(progressBar.CornerRadius.TopLeft, clip.RadiusY, 0.01);
                Assert.True(clip.Rect.Width > 0 && clip.Rect.Height > 0,
                    "Indicator host clip must be sized to the realised host bounds.");

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_IndeterminateBars_UseWinUiWidthRatios()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    ProgressMode = ProgressBarMode.Indeterminate,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? track = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Track");
                System.Windows.Controls.Border? bar1 = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_IndeterminateBar");
                System.Windows.Controls.Border? bar2 = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_IndeterminateBar2");
                Assert.NotNull(track);
                Assert.NotNull(bar1);
                Assert.NotNull(bar2);

                double trackWidth = track.ActualWidth;
                Assert.True(trackWidth > 0, "Track must have a realised width.");
                Assert.Equal(trackWidth * 0.4, bar1.Width, 0.5);
                Assert.Equal(trackWidth * 0.6, bar2.Width, 0.5);

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_IsIndeterminate_ShowsIndeterminateBars()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? track = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Track");
                System.Windows.Controls.Border? fill = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                System.Windows.Controls.Border? bar1 = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_IndeterminateBar");
                System.Windows.Controls.Border? bar2 = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_IndeterminateBar2");
                Assert.NotNull(track);
                Assert.NotNull(fill);
                Assert.NotNull(bar1);
                Assert.NotNull(bar2);

                progressBar.IsIndeterminate = true;
                DrainDispatcher(w.Dispatcher);

                Assert.Equal(Visibility.Visible, bar1.Visibility);
                Assert.Equal(Visibility.Visible, bar2.Visibility);
                Assert.Equal(Visibility.Collapsed, fill.Visibility);
                Assert.Equal(0.0, track.Opacity, 0.001);

                progressBar.IsIndeterminate = false;
                DrainDispatcher(w.Dispatcher);

                Assert.Equal(Visibility.Collapsed, bar1.Visibility);
                Assert.Equal(Visibility.Visible, fill.Visibility);
                Assert.Equal(1.0, track.Opacity, 0.001);

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_ShowError_UsesCriticalBrush()
        {
            AssertProgressBarStatePrimitiveBrush(static bar => bar.ShowError = true, "SystemFillColorCriticalBrush");
        }

        [Fact]
        public void ProgressBar_ShowPaused_UsesCautionBrush()
        {
            AssertProgressBarStatePrimitiveBrush(static bar => bar.ShowPaused = true, "SystemFillColorCautionBrush");
        }

        [Fact]
        public void ProgressBar_Indeterminate_StopsAnimationOnUnloadAndRestartsOnReload()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    IsIndeterminate = true,
                };
                System.Windows.Controls.ContentControl host = new() { Content = progressBar };
                Window w = new() { Content = host, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TranslateTransform? translate =
                    progressBar.Template.FindName("PART_IndeterminateTranslate", progressBar) as TranslateTransform;
                TranslateTransform? translate2 =
                    progressBar.Template.FindName("PART_IndeterminateTranslate2", progressBar) as TranslateTransform;
                Assert.NotNull(translate);
                Assert.NotNull(translate2);
                Assert.True(WaitUntil(w.Dispatcher, 2000, () => translate.HasAnimatedProperties),
                    "The indeterminate animation must run while the bar is loaded.");

                host.Content = null;
                DrainDispatcher(w.Dispatcher);

                Assert.False(translate.HasAnimatedProperties,
                    "Unloading must stop the repeat-forever animation on the primary translate transform.");
                Assert.False(translate2.HasAnimatedProperties,
                    "Unloading must stop the repeat-forever animation on the secondary translate transform.");

                host.Content = progressBar;
                DrainDispatcher(w.Dispatcher);

                Assert.True(WaitUntil(w.Dispatcher, 2000, () => translate.HasAnimatedProperties),
                    "Reloading must restart the indeterminate animation.");

                w.Close();
                DrainDispatcher(w.Dispatcher);

                Assert.False(translate.HasAnimatedProperties,
                    "Closing the hosting window must leave no active animation clocks on the translate transforms.");
                Assert.False(translate2.HasAnimatedProperties,
                    "Closing the hosting window must leave no active animation clocks on the secondary translate transform.");
            });
        }

        [Fact]
        public void ProgressBar_Indeterminate_StopsAnimationWhenCollapsedAndRestartsWhenVisible()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    IsIndeterminate = true,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TranslateTransform? translate =
                    progressBar.Template.FindName("PART_IndeterminateTranslate", progressBar) as TranslateTransform;
                TranslateTransform? translate2 =
                    progressBar.Template.FindName("PART_IndeterminateTranslate2", progressBar) as TranslateTransform;
                Assert.NotNull(translate);
                Assert.NotNull(translate2);
                Assert.True(WaitUntil(w.Dispatcher, 2000, () => translate.HasAnimatedProperties),
                    "The indeterminate animation must run while the bar is loaded and visible.");

                progressBar.Visibility = Visibility.Collapsed;
                DrainDispatcher(w.Dispatcher);

                Assert.False(translate.HasAnimatedProperties,
                    "Collapsing the bar must stop the repeat-forever animation on the primary translate transform.");
                Assert.False(translate2.HasAnimatedProperties,
                    "Collapsing the bar must stop the repeat-forever animation on the secondary translate transform.");

                progressBar.Visibility = Visibility.Visible;
                DrainDispatcher(w.Dispatcher);

                Assert.True(WaitUntil(w.Dispatcher, 2000, () => translate.HasAnimatedProperties),
                    "Restoring visibility must restart the indeterminate animation.");

                w.Close();
                DrainDispatcher(w.Dispatcher);
            });
        }

        [Fact]
        public void ProgressBar_IndeterminateMode_SetsIsIndeterminate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    ProgressMode = ProgressBarMode.Indeterminate,
                };
                Assert.True(progressBar.IsIndeterminate,
                    "ProgressMode.Indeterminate must map onto the inherited IsIndeterminate primitive.");

                progressBar.ProgressMode = ProgressBarMode.Standard;
                Assert.False(progressBar.IsIndeterminate,
                    "ProgressMode.Standard must clear the inherited IsIndeterminate primitive.");
            });
        }

        [Fact]
        public void ProgressBar_DeterminateFill_AnimatesScaleXAndKeepsFullLayoutWidth()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? track = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Track");
                System.Windows.Controls.Border? fill = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                ScaleTransform? scale = progressBar.Template.FindName("PART_FillScale", progressBar) as ScaleTransform;
                Assert.NotNull(track);
                Assert.NotNull(fill);
                Assert.NotNull(scale);

                progressBar.Value = 60;
                Assert.True(WaitUntil(w.Dispatcher, 3000, () => !scale.HasAnimatedProperties && Math.Abs(scale.ScaleX - 0.6) < 0.01),
                    "The determinate fill scale must settle at Value / (Maximum - Minimum) after the 367 ms reposition animation.");
                Assert.Equal(track.ActualWidth, fill.Width, 0.5);

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_DeterminateFill_RapidValueChangesSettleAtSecondRatio()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ScaleTransform? scale = progressBar.Template.FindName("PART_FillScale", progressBar) as ScaleTransform;
                Assert.NotNull(scale);

                progressBar.Value = 30;
                progressBar.Value = 75;
                Assert.True(WaitUntil(w.Dispatcher, 3000, () => !scale.HasAnimatedProperties && Math.Abs(scale.ScaleX - 0.75) < 0.01),
                    "Interrupting a running fill animation must hand off and settle at the second value's ratio.");

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_StepMode_PositionsFillScaleAtStepRatio()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    ProgressMode = ProgressBarMode.StepProgress,
                    Steps = 4,
                    CurrentStep = 0,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? track = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Track");
                System.Windows.Controls.Border? fill = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                ScaleTransform? scale = progressBar.Template.FindName("PART_FillScale", progressBar) as ScaleTransform;
                Assert.NotNull(track);
                Assert.NotNull(fill);
                Assert.NotNull(scale);

                progressBar.CurrentStep = 2;
                Assert.True(WaitUntil(w.Dispatcher, 3000, () => !scale.HasAnimatedProperties && Math.Abs(scale.ScaleX - 0.5) < 0.01),
                    "Step mode must position the fill scale at CurrentStep / Steps.");
                Assert.Equal(track.ActualWidth, fill.Width, 0.5);

                w.Close();
            });
        }

        private static void AssertProgressBarStatePrimitiveBrush(Action<Controls.ProgressBar> applyState, string brushKey)
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
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

                System.Windows.Controls.Border? fill = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                Assert.NotNull(fill);

                SolidColorBrush? expected = app?.TryFindResource(brushKey) as SolidColorBrush;
                Assert.NotNull(expected);

                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.NotNull(actual);
                Assert.Equal(expected.Color, actual.Color);

                w.Close();
            });
        }

        [Fact]
        public void ProgressBar_DeclaresPoliteLiveSetting()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new() { Value = 50, Width = 240, Height = 24 };
                Window window = new() { Content = progressBar };
                window.Show();
                _ = progressBar.ApplyTemplate();
                DrainDispatcher(window.Dispatcher);

                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(progressBar));
                window.Close();
            });
        }

        private static void AssertProgressBarModeBrush(ProgressBarMode mode, string brushKey)
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = mode,
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? fill = FindVisualChildByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                Assert.NotNull(fill);

                SolidColorBrush? expected = app?.TryFindResource(brushKey) as SolidColorBrush;
                Assert.NotNull(expected);

                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.NotNull(actual);
                Assert.Equal(expected.Color, actual.Color);

                w.Close();
            });
        }
    }
}
