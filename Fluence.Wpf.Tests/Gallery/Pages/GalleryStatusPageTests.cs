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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherDelayWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryStatusPage"/>.
    /// </summary>
    public sealed class GalleryStatusPageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        // This test drives the page's NumberBox, so it builds its own instance rather
        // than mutating the one the class shares.
        [Fact]
        public Task GalleryStatusPage_DeterminateProgressRingUsesNumberBoxBindingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryStatusPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.NumberBox valueBox = Assert.IsType<Controls.NumberBox>(DemoTestHost.FindByName<Controls.NumberBox>(page, "ProgressRingValueBox"), exactMatch: false);
                    Controls.ProgressRing ring = Assert.IsType<Controls.ProgressRing>(DemoTestHost.FindByName<Controls.ProgressRing>(page, "DeterminateProgressRing"), exactMatch: false);

                    Assert.Equal(1.0, valueBox.Minimum, 0.001);
                    Assert.Equal(100.0, valueBox.Maximum, 0.001);
                    Assert.Equal(50.0, valueBox.Value, 0.001);
                    Assert.Equal(50.0, ring.Value, 0.001);

                    valueBox.Value = 75;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(75.0, ring.Value, 0.001);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test drives the page's NumberBox, so it builds its own instance rather
        // than mutating the one the class shares.
        [Fact]
        public Task GalleryStatusPage_ProgressBarValueAllowsZeroAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryStatusPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.NumberBox valueBox = Assert.IsType<Controls.NumberBox>(DemoTestHost.FindByName<Controls.NumberBox>(page, "ProgressValueNumberBox"), exactMatch: false);
                    Controls.ProgressBar progressBar = Assert.IsType<Controls.ProgressBar>(DemoTestHost.FindByName<Controls.ProgressBar>(page, "StandardProgressBar"), exactMatch: false);

                    Assert.Equal(0.0, progressBar.Minimum, 0.001);
                    Assert.Equal(0.0, valueBox.Minimum, 0.001);

                    valueBox.Value = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(0.0, progressBar.Value, 0.001);

                    DemoSampleControl sample = Assert.IsType<DemoSampleControl>(DemoTestHost.FindVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressBarValue", StringComparison.Ordinal)), exactMatch: false);
                    Assert.Contains("x:Name=\"ProgressValueNumberBox\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("Minimum=\"0\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, sample.XamlSource.IndexOf("Minimum=\"1\"", StringComparison.Ordinal));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryStatusPage_SourceMatchesLiveStepAndRingValuesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryStatusPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    DemoSampleControl stepSample = Assert.IsType<DemoSampleControl>(DemoTestHost.FindVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressBarSteps", StringComparison.Ordinal)), exactMatch: false);
                    DemoSampleControl ringSample = Assert.IsType<DemoSampleControl>(DemoTestHost.FindVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressRings", StringComparison.Ordinal)), exactMatch: false);

                    Assert.Contains("Steps=\"10\"", stepSample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("Text=\"Step 1 of 10\"", stepSample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, stepSample.XamlSource.IndexOf("Steps=\"5\"", StringComparison.Ordinal));

                    int pausedRingIndex = ringSample.XamlSource.IndexOf("x:Name=\"PausedProgressRing\"", StringComparison.Ordinal);
                    int errorRingIndex = ringSample.XamlSource.IndexOf("x:Name=\"ErrorProgressRing\"", StringComparison.Ordinal);
                    Assert.True(pausedRingIndex >= 0, "ProgressRing source should include PausedProgressRing.");
                    Assert.True(errorRingIndex > pausedRingIndex, "ProgressRing source should place ErrorProgressRing after PausedProgressRing.");
                    string pausedRingSource = ringSample.XamlSource[pausedRingIndex..errorRingIndex];

                    Assert.Contains("IsIndeterminate=\"False\"", pausedRingSource, StringComparison.Ordinal);
                    Assert.Contains("ProgressState=\"{x:Static fluence:ProgressRingState.Paused}\"", pausedRingSource, StringComparison.Ordinal);
                    Assert.Contains("Value=\"80\"", pausedRingSource, StringComparison.Ordinal);
                    Assert.Contains("Value=\"80\"", ringSample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, ringSample.XamlSource.IndexOf("Value=\"70\"", StringComparison.Ordinal));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test drives the page's step ProgressBar buttons, so it builds its own instance
        // rather than mutating the one the class shares.
        [Fact]
        public Task GalleryStatusPage_StepProgressBarAnimatesEdgeClicksAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                GalleryStatusPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.ProgressBar progressBar = Assert.IsType<Controls.ProgressBar>(DemoTestHost.FindByName<Controls.ProgressBar>(page, "StepProgressBar"), exactMatch: false);

                    System.Windows.Controls.Border track = Assert.IsType<System.Windows.Controls.Border>(DemoTestHost.FindByName<System.Windows.Controls.Border>(progressBar, "PART_Track"), exactMatch: false);
                    System.Windows.Controls.Border fill = Assert.IsType<System.Windows.Controls.Border>(DemoTestHost.FindByName<System.Windows.Controls.Border>(progressBar, "PART_Fill"), exactMatch: false);

                    Controls.Button backButton = Assert.IsType<Controls.Button>(FindStepButton(page, "Back"), exactMatch: false);
                    Controls.Button nextButton = Assert.IsType<Controls.Button>(FindStepButton(page, "Next"), exactMatch: false);

                    backButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, backButton));
                    await WaitForAnimationAndDrainByDelayAsync(window.Dispatcher, 340).ConfigureAwait(true);

                    await AssertStepClickStartsAwayFromTargetAsync(nextButton, progressBar, fill, track, window.Dispatcher, 1, forward: true).ConfigureAwait(true);
                    await WaitForAnimationAndDrainByDelayAsync(window.Dispatcher, 340).ConfigureAwait(true);
                    await AssertStepClickStartsAwayFromTargetAsync(nextButton, progressBar, fill, track, window.Dispatcher, 2, forward: true).ConfigureAwait(true);
                    await WaitForAnimationAndDrainByDelayAsync(window.Dispatcher, 340).ConfigureAwait(true);

                    progressBar.CurrentStep = 9;
                    await WaitForAnimationAndDrainByDelayAsync(window.Dispatcher, 340).ConfigureAwait(true);
                    await AssertStepClickStartsAwayFromTargetAsync(nextButton, progressBar, fill, track, window.Dispatcher, 10, forward: true).ConfigureAwait(true);
                    await WaitForAnimationAndDrainByDelayAsync(window.Dispatcher, 340).ConfigureAwait(true);
                    await AssertStepClickStartsAwayFromTargetAsync(backButton, progressBar, fill, track, window.Dispatcher, 9, forward: false).ConfigureAwait(true);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test drives the page's NumberBox, so it builds its own instance rather
        // than mutating the one the class shares.
        [Fact]
        public Task GalleryStatusPage_NumberBoxDrivesFirstProgressBarAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryStatusPage(), static window =>
            {
                Controls.NumberBox numberBox = Assert.IsType<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "ProgressValueNumberBox"), exactMatch: false);
                Controls.ProgressBar progressBar = Assert.IsType<Controls.ProgressBar>(FindVisualChildByName<Controls.ProgressBar>(window, "StandardProgressBar"), exactMatch: false);
                Controls.ToggleSwitch indeterminateToggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "IndeterminateToggle"), exactMatch: false);
                Controls.NumberBox progressRingValueBox = Assert.IsType<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "ProgressRingValueBox"), exactMatch: false);

                Assert.Equal(HorizontalAlignment.Center, numberBox.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, numberBox.VerticalAlignment);
                Assert.Equal(HorizontalAlignment.Center, indeterminateToggle.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, indeterminateToggle.VerticalAlignment);
                Assert.Equal(HorizontalAlignment.Center, progressRingValueBox.HorizontalAlignment);
                Assert.Equal("On / Off", indeterminateToggle.OnContent as string, StringComparer.Ordinal);
                Assert.Equal("On / Off", indeterminateToggle.OffContent as string, StringComparer.Ordinal);
                Assert.Equal(0d, numberBox.Minimum);
                Assert.Equal(100d, numberBox.Maximum);
                Assert.Null(FindVisualChildByName<Controls.Slider>(window, "ProgressSlider"));
                Assert.Null(FindVisualChildByName<System.Windows.Controls.TextBlock>(window, "SliderValueLabel"));

                numberBox.Value = 73d;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.Equal(73d, progressBar.Value, 0.1);

                numberBox.Value = 0d;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.Equal(0d, progressBar.Value, 0.1);

                indeterminateToggle.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("On / Off", indeterminateToggle.OffContent as string, StringComparer.Ordinal);
            });
        }

        private static Controls.Button? FindStepButton(DependencyObject root, string tag)
        {
            return DemoTestHost.FindVisualChildren<Controls.Button>(root)
                .FirstOrDefault(button => string.Equals(button.Tag as string, tag, StringComparison.Ordinal));
        }

        private static async Task AssertStepClickStartsAwayFromTargetAsync(
            Controls.Button button,
            Controls.ProgressBar progressBar,
            FrameworkElement fill,
            FrameworkElement track,
            Dispatcher dispatcher,
            int expectedStep,
            bool forward)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
            await WaitForAnimationAndDrainByDelayAsync(dispatcher, 40).ConfigureAwait(true);

            Assert.Equal(expectedStep, progressBar.CurrentStep);
            double targetWidth = track.ActualWidth * expectedStep / progressBar.Steps;

            // The determinate fill is laid out at the full track width and animates
            // PART_FillScale.ScaleX in [0,1], so the visually rendered progress width is
            // the track width multiplied by the current (possibly animating) scale.
            ScaleTransform fillScale = Assert.IsType<ScaleTransform>(fill.RenderTransform);
            double animatedWidth = track.ActualWidth * fillScale.ScaleX;
            if (forward)
            {
                Assert.True(animatedWidth < targetWidth, string.Format(CultureInfo.InvariantCulture,
                        "Forward step animation should start before the target width. Animated={0}, Target={1}, Step={2}.",
                        animatedWidth,
                        targetWidth,
                        expectedStep));
            }
            else
            {
                Assert.True(
                    animatedWidth > targetWidth,
                    string.Format(CultureInfo.InvariantCulture,
                        "Backward step animation should start after the target width. Animated={0}, Target={1}, Step={2}.",
                        animatedWidth,
                        targetWidth,
                        expectedStep));
            }
        }
    }
}
