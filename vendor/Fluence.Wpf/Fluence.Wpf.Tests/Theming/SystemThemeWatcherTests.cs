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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public sealed class SystemThemeWatcherTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                SystemThemeWatcher.ResetForTesting();
                _ = TestApp.EnsureLibraryTheme();
            }));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                SystemThemeWatcher.ResetForTesting();
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
            }));
        }

        [Fact]
        public Task RequestRefresh_BurstBeforeDrain_QueuesOneRefreshAsync()
        {
            return ObserveRefreshesAsync(static () =>
            {
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(static () => { }));
                SystemThemeWatcher.RequestRefresh();
                DispatcherOperation first = Assert.IsType<DispatcherOperation>(GetPendingRefresh());
                SystemThemeWatcher.RequestRefresh();
                Assert.Same(first, GetPendingRefresh());
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                Assert.Equal(DispatcherOperationStatus.Completed, first.Status);
                Assert.Null(GetPendingRefresh());
            });
        }

        [Fact]
        public Task RequestRefresh_AfterDrain_QueuesAnotherRefreshAsync()
        {
            return ObserveRefreshesAsync(static () =>
            {
                SystemThemeWatcher.RequestRefresh();
                DispatcherOperation first = Assert.IsType<DispatcherOperation>(GetPendingRefresh());
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                Assert.Equal(DispatcherOperationStatus.Completed, first.Status);
                Assert.Null(GetPendingRefresh());
                SystemThemeWatcher.RequestRefresh();
                DispatcherOperation second = Assert.IsType<DispatcherOperation>(GetPendingRefresh());
                Assert.NotSame(first, second);
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                Assert.Equal(DispatcherOperationStatus.Completed, second.Status);
                Assert.Null(GetPendingRefresh());
            });
        }

        [Fact]
        public Task RequestRefresh_AfterAbortedOperation_CanQueueAgainAsync()
        {
            return ObserveRefreshesAsync(static () =>
            {
                SystemThemeWatcher.RequestRefresh();
                DispatcherOperation first = Assert.IsType<DispatcherOperation>(GetPendingRefresh());
                Assert.True(first.Abort());
                Assert.Equal(DispatcherOperationStatus.Aborted, first.Status);
                SystemThemeWatcher.RequestRefresh();
                DispatcherOperation second = Assert.IsType<DispatcherOperation>(GetPendingRefresh());
                Assert.NotSame(first, second);
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                Assert.Equal(DispatcherOperationStatus.Completed, second.Status);
                Assert.Null(GetPendingRefresh());
            });
        }

        [Theory]
        [InlineData(0x031A, null, 1)]
        [InlineData(0x0015, null, 1)]
        [InlineData(0x0320, null, 1)]
        [InlineData(0x001A, "iMmErSiVeCoLoRsEt", 1)]
        [InlineData(0x001A, "UnrelatedSetting", 0)]
        public Task WndProc_RelevantMessagesReachRefreshQueue_UnrelatedSettingDoesNotAsync(int message, string? setting, int expectedCount)
        {
            return ObserveRefreshesAsync(() =>
            {
                IntPtr pointer = setting is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(setting);
                try
                {
                    MethodInfo method = Assert.IsType<MethodInfo>(
                        typeof(SystemThemeWatcher).GetMethod("WndProc", BindingFlags.NonPublic | BindingFlags.Static),
                        exactMatch: false);
                    _ = method.Invoke(null, [IntPtr.Zero, message, IntPtr.Zero, pointer, false]);
                    object? pending = GetPendingRefresh();
                    Assert.Equal(expectedCount > 0, pending is not null);
                    DispatcherOperation? first = pending is null ? null : Assert.IsType<DispatcherOperation>(pending);
                    WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                    Assert.Null(GetPendingRefresh());
                    if (expectedCount > 0)
                    {
                        Assert.Equal(DispatcherOperationStatus.Completed, first!.Status);
                        _ = method.Invoke(null, [IntPtr.Zero, message, IntPtr.Zero, pointer, false]);
                        DispatcherOperation second = Assert.IsType<DispatcherOperation>(GetPendingRefresh());
                        Assert.NotSame(first, second);
                        WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                        Assert.Equal(DispatcherOperationStatus.Completed, second.Status);
                        Assert.Null(GetPendingRefresh());
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pointer);
                }
            });
        }

        private static object? GetPendingRefresh()
        {
            FieldInfo field = Assert.IsType<FieldInfo>(
                typeof(SystemThemeWatcher).GetField("_pendingRefresh", BindingFlags.NonPublic | BindingFlags.Static),
                exactMatch: false);
            return field.GetValue(null);
        }

        private static Task ObserveRefreshesAsync(Action scenario)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Dispatcher dispatcher = Application.Current.Dispatcher;
                try
                {
                    scenario();
                }
                finally
                {
                    SystemThemeWatcher.ResetForTesting();
                    WpfTestSta.DrainDispatcher(dispatcher);
                }
            });
        }
    }
}
