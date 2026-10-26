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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;

namespace Fluence.Wpf.Tests.Windowing
{
    public sealed class ThemeTransitionOverlayTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = TestApp.EnsureLibraryTheme();
                MotionHelper.OverrideIsMotionEnabled = null;
            }));
        }

        [Fact]
        public Task PreparedSnapshot_PlaysOnceAndReleasesAfterFadeAsync()
        {
            return WithShownWindowAsync(async static (window, root, host) =>
            {
                ThemeTransitionOverlay overlay = new(window, root, host);
                overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                UIElement snapshot = Assert.IsType<UIElement>(Assert.Single(host.Children), exactMatch: false);
                Assert.False(snapshot.IsHitTestVisible);

                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                // The queued render can attach the clock before its first animation timing tick.
                Assert.True(await WaitUntilAsync(window.Dispatcher, 1500,
                    () => IsAnimated(snapshot)).ConfigureAwait(true), "The prepared snapshot should begin fading.");

                // Readiness is consumed by the first Play; another publication cannot restart it.
                snapshot.BeginAnimation(UIElement.OpacityProperty, animation: null);
                Assert.False(IsAnimated(snapshot));
                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Same(snapshot, Assert.Single(host.Children));
                Assert.False(IsAnimated(snapshot));

                overlay.Prepare(ApplicationTheme.Dark, ApplicationTheme.Light);
                Assert.NotSame(snapshot, Assert.Single(host.Children));
                overlay.Play();
                Assert.True(await WaitUntilAsync(window.Dispatcher, 1500,
                    () => host.Children.Count is 0).ConfigureAwait(true), "The fade should release its visual.");
                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Empty(host.Children);
            });
        }

        [Fact]
        public Task SuppressedPreparation_ClearsAttachedSnapshotAndCannotPlayAsync()
        {
            return WithShownWindowAsync(static (window, root, host) =>
            {
                ThemeTransitionOverlay overlay = new(window, root, host);
                Action[] suppressions =
                [
                    () => overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.HighContrast),
                    () => overlay.Prepare(ApplicationTheme.HighContrast, ApplicationTheme.Dark),
                    () => { MotionHelper.OverrideIsMotionEnabled = false; overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark); MotionHelper.OverrideIsMotionEnabled = true; },
                    () => { window.WindowState = WindowState.Minimized; overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark); window.WindowState = WindowState.Normal; },
                    () => { window.Hide(); overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark); window.Show(); },
                ];

                foreach (Action suppress in suppressions)
                {
                    overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                    _ = Assert.Single(host.Children);
                    suppress();
                    Assert.Empty(host.Children);
                    overlay.Play();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Empty(host.Children);
                }

                return Task.CompletedTask;
            });
        }

        [Fact]
        public Task OffThreadPreparation_PreservesOlderFadeButConsumesReadinessAsync()
        {
            return WithShownWindowAsync(async static (window, root, host) =>
            {
                ThemeTransitionOverlay overlay = new(window, root, host);
                overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                UIElement olderSnapshot = Assert.IsType<UIElement>(Assert.Single(host.Children), exactMatch: false);

                // A worker-thread prepare must consume this still-pending capture.
                await Task.Run(() => overlay.Prepare(ApplicationTheme.Dark, ApplicationTheme.Light),
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Same(olderSnapshot, Assert.Single(host.Children));
                Assert.False(IsAnimated(olderSnapshot));

                // It must also leave an earlier in-progress fade attached and running.
                overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                UIElement fadingSnapshot = Assert.IsType<UIElement>(Assert.Single(host.Children), exactMatch: false);
                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(await WaitUntilAsync(window.Dispatcher, 1500,
                    () => IsAnimated(fadingSnapshot)).ConfigureAwait(true), "The prepared snapshot should begin fading.");
                // Hold an observable fade while the worker runs, avoiding dependence on the
                // 167 ms production duration or scheduling of the test thread pool.
                fadingSnapshot.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromSeconds(5)));
                await Task.Run(() => overlay.Prepare(ApplicationTheme.Dark, ApplicationTheme.Light),
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
                Assert.Same(fadingSnapshot, Assert.Single(host.Children));
                overlay.Play();
                Assert.True(IsAnimated(fadingSnapshot));
                overlay.Clear();
                Assert.Empty(host.Children);
            });
        }

        [Fact]
        public Task Clear_InvalidatesQueuedFadeAndPendingPreparationAsync()
        {
            return WithShownWindowAsync(static (window, root, host) =>
            {
                ThemeTransitionOverlay overlay = new(window, root, host);
                overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                overlay.Play(); // Queued at Render; Clear must invalidate the callback.
                overlay.Clear();
                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Empty(host.Children);

                overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                overlay.Clear(); // Also discard preparation that has not been played.
                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Empty(host.Children);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public Task InvalidCapture_LeavesNoSnapshotOrReadyFadeAsync()
        {
            return WithShownWindowAsync(static (window, _, host) =>
            {
                Grid emptyRoot = new();
                Grid oversizedRoot = new();
                oversizedRoot.Measure(new Size(5000, 5000));
                oversizedRoot.Arrange(new Rect(0, 0, 5000, 5000));
                Assert.True(oversizedRoot.ActualWidth * oversizedRoot.ActualHeight > 16_000_000);

                foreach (Grid invalidRoot in new[] { emptyRoot, oversizedRoot })
                {
                    ThemeTransitionOverlay overlay = new(window, invalidRoot, host);
                    overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                    overlay.Play();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Empty(host.Children);
                }

                return Task.CompletedTask;
            });
        }

        [Fact]
        public Task RapidPreparation_ReplacesAttachedSnapshotAndFinalFadeReleasesItAsync()
        {
            return WithShownWindowAsync(async static (window, root, host) =>
            {
                ThemeTransitionOverlay overlay = new(window, root, host);
                overlay.Prepare(ApplicationTheme.Light, ApplicationTheme.Dark);
                UIElement first = Assert.IsType<UIElement>(Assert.Single(host.Children), exactMatch: false);
                overlay.Play();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                // The next capture sees the current root while the prior blend is attached.
                overlay.Prepare(ApplicationTheme.Dark, ApplicationTheme.Light);
                UIElement second = Assert.IsType<UIElement>(Assert.Single(host.Children), exactMatch: false);
                Assert.NotSame(first, second);
                Assert.False(host.Children.Contains(first));
                overlay.Play();
                Assert.True(await WaitUntilAsync(window.Dispatcher, 1500,
                    () => host.Children.Count is 0).ConfigureAwait(true));
            });
        }

        [Fact]
        public Task WindowRetemplateAndClose_RemoveAttachedTransitionVisualsAsync()
        {
            return WithShownWindowAsync(static (window, root, host) =>
            {
                // Exercise FluenceWindow's ownership of its real template overlay.
                Assert.True(root.ActualWidth > 0);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                _ = Assert.Single(host.Children);
                ControlTemplate originalTemplate = window.Template;
                window.Template = new ControlTemplate(typeof(FluenceWindow))
                {
                    VisualTree = new FrameworkElementFactory(typeof(Grid)),
                };
                _ = window.ApplyTemplate();
                Assert.Empty(host.Children);

                window.Template = originalTemplate;
                _ = window.ApplyTemplate();
                window.UpdateLayout();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Grid newHost = FindPart(window, "PART_ThemeTransitionOverlayHost");
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                _ = Assert.Single(newHost.Children);
                window.Close();
                Assert.Empty(newHost.Children);
                return Task.CompletedTask;
            });
        }

        private static Task WithShownWindowAsync(Func<FluenceWindow, Grid, Grid, Task> body)
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                FluenceWindow window = new()
                {
                    Width = 480,
                    Height = 320,
                    Left = -20000,
                    Top = -20000,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.None,
                    Content = new System.Windows.Controls.Border { Background = Brushes.CornflowerBlue },
                };

                try
                {
                    window.Show();
                    await window.Dispatcher.InvokeAsync(static () => { },
                        DispatcherPriority.Loaded, TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                    await body(window, FindPart(window, "PART_ThemeTransitionRoot"),
                        FindPart(window, "PART_ThemeTransitionOverlayHost")).ConfigureAwait(true);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        private static Grid FindPart(FluenceWindow window, string name)
        {
            return Assert.IsType<Grid>(window.Template.FindName(name, window));
        }

        private static bool IsAnimated(UIElement element)
        {
            return DependencyPropertyHelper.GetValueSource(element, UIElement.OpacityProperty).IsAnimated;
        }
    }
}
