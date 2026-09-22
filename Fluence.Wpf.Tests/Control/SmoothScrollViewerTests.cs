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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class SmoothScrollViewerTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                MotionHelper.OverrideIsMotionEnabled = null;
                _ = TestApp.EnsureLibraryTheme();
            }));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public Task WheelAfterExternalScrollContinuesFromLiveOffsetAsync(bool motion, bool wheelFirst)
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                MotionHelper.OverrideIsMotionEnabled = motion;
                ScrollProbe viewer = new()
                {
                    Content = new System.Windows.Controls.Border { Height = 3000 },
                    ScrollDuration = new Duration(System.TimeSpan.Zero),
                };
                Window window = new() { Content = viewer, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    if (wheelFirst)
                    {
                        viewer.Wheel(-120);
                        Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => System.Math.Abs(viewer.VerticalOffset - 72) < 0.01).ConfigureAwait(true));
                    }
                    viewer.ScrollToVerticalOffset(1000);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(1000, viewer.VerticalOffset);
                    viewer.Wheel(-120);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => System.Math.Abs(viewer.VerticalOffset - 1072) < 0.01).ConfigureAwait(true));
                    Assert.Equal(1072, viewer.VerticalOffset);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task WheelAnimationAfterExternalScrollDoesNotRewindAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                ScrollProbe viewer = new()
                {
                    Content = new System.Windows.Controls.Border { Height = 3000 },
                    ScrollDuration = new Duration(System.TimeSpan.FromMilliseconds(100)),
                };
                Window window = new() { Content = viewer, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    viewer.ScrollToVerticalOffset(1000);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    double minimumOffset = viewer.VerticalOffset;
                    viewer.ScrollChanged += (_, _) => minimumOffset = System.Math.Min(minimumOffset, viewer.VerticalOffset);
                    viewer.Wheel(-120);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => System.Math.Abs(viewer.VerticalOffset - 1072) < 0.01).ConfigureAwait(true));
                    Assert.Equal(1000, minimumOffset);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ExternalScrollDuringWheelAnimationCancelsOldDestinationAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                ScrollProbe viewer = new()
                {
                    Content = new System.Windows.Controls.Border { Height = 3000 },
                    ScrollDuration = new Duration(System.TimeSpan.FromMilliseconds(300)),
                };
                Window window = new() { Content = viewer, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    viewer.Wheel(-120);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => viewer.VerticalOffset is > 1 and < 71).ConfigureAwait(true));
                    viewer.ScrollToVerticalOffset(1000);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(1000, viewer.VerticalOffset);
                    double minimumOffset = viewer.VerticalOffset;
                    viewer.ScrollChanged += (_, _) => minimumOffset = System.Math.Min(minimumOffset, viewer.VerticalOffset);
                    viewer.Wheel(-120);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => System.Math.Abs(viewer.VerticalOffset - 1072) < 0.01).ConfigureAwait(true));
                    Assert.Equal(1000, minimumOffset);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task WheelAnimationMakesProgressWhileInputRemainsQueuedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                ScrollProbe viewer = new()
                {
                    Content = new System.Windows.Controls.Border { Height = 3000 },
                    ScrollDuration = new Duration(System.TimeSpan.FromMilliseconds(100)),
                };
                Window window = new() { Content = viewer, Width = 300, Height = 240 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    viewer.Wheel(-120);
                    System.Diagnostics.Stopwatch timeout = System.Diagnostics.Stopwatch.StartNew();
                    System.Windows.Threading.DispatcherFrame frame = new();

                    void ProcessInput()
                    {
                        if (viewer.VerticalOffset > 1 || timeout.ElapsedMilliseconds >= 1000)
                        {
                            frame.Continue = false;
                            return;
                        }

                        _ = viewer.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                            new System.Action(ProcessInput));
                    }

                    _ = viewer.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                        new System.Action(ProcessInput));
                    System.Windows.Threading.Dispatcher.PushFrame(frame);
                    Assert.True(viewer.VerticalOffset > 1,
                        "Wheel animation should progress before the queued input workload ends.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
        private sealed class ScrollProbe : Controls.SmoothScrollViewer
        {
            internal void Wheel(int delta)
            {
                OnMouseWheel(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, delta) { RoutedEvent = MouseWheelEvent });
            }
        }
    }
}
