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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    public sealed class GallerySmoothScrollingTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
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
        [InlineData(false)]
        [InlineData(true)]
        public Task GalleryWheelBurstAndExternalScrollReachRequestedOffsetsAsync(bool colorsPage)
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                Page page = colorsPage ? new GalleryColorsPage() : new GalleryDataPage();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.SmoothScrollViewer viewer = Assert.IsType<Controls.SmoothScrollViewer>(FindVisualChild<Controls.SmoothScrollViewer>(page));
                    Assert.True(viewer.ScrollableHeight > 288);
                    viewer.ScrollDuration = new Duration(TimeSpan.FromMilliseconds(100));
                    for (int index = 0; index < 4; index++)
                    {
                        viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.MouseWheelEvent });
                    }

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => Math.Abs(viewer.VerticalOffset - 288) < 0.01).ConfigureAwait(true));
                    viewer.ScrollToVerticalOffset(100);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(100, viewer.VerticalOffset);
                    viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.MouseWheelEvent });
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => Math.Abs(viewer.VerticalOffset - 172) < 0.01).ConfigureAwait(true));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }
    }
}
