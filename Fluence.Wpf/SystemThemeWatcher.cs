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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Win32;

namespace Fluence.Wpf
{
    /// <summary>
    /// Subscribes a <see cref="Window"/> to settings change notifications, coalescing pending theme and accent refreshes.
    /// </summary>
    /// <remarks>
    /// Used with <see cref="ApplicationThemeManager"/> to refresh resources when the user changes Windows light/dark or contrast while the app runs.
    /// </remarks>
    public static class SystemThemeWatcher
    {
        /// <summary>
        /// Begins watching the specified window for system theme and accent changes.
        /// </summary>
        /// <param name="window">The WPF window to associate with the watcher. Must not be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="window"/> is <see langword="null"/>.</exception>
        public static void Watch(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);
            lock (_lock)
            {
                if (FindWatchedWindow(window) is not null)
                {
                    return;
                }
                WatchedWindow watched = new(window);
                _watchedWindows.Add(watched);
                // Auto-release on close so a consumer that forgets UnWatch (or an app that exits
                // without an explicit teardown) cannot leak the window, its HwndSource hook, or
                // this static registry entry.
                window.Closed += OnWindowClosed;
                if (new WindowInteropHelper(window).Handle == IntPtr.Zero)
                {
                    // Loaded is later than SourceInitialized. A caller inside or after
                    // SourceInitialized already has a handle and must attach immediately,
                    // even though Loaded has not fired yet.
                    window.SourceInitialized += OnWindowSourceInitialized;
                }
                else
                {
                    AttachHook(watched);
                }
            }
        }

        /// <summary>
        /// Stops watching the specified window and removes Win32 hooks.
        /// </summary>
        /// <param name="window">The window previously passed to <see cref="Watch"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="window"/> is <see langword="null"/>.</exception>
        public static void UnWatch(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);
            lock (_lock)
            {
                if (FindWatchedWindow(window) is not WatchedWindow watched)
                {
                    return;
                }
                DetachHook(watched);
                _ = _watchedWindows.Remove(watched);
                window.SourceInitialized -= OnWindowSourceInitialized;
                window.Closed -= OnWindowClosed;
            }
        }

        private static void OnWindowClosed(object? sender, EventArgs e)
        {
            if (sender is Window window)
            {
                // UnWatch is idempotent: if FluenceWindow.OnClosed (or the consumer) already
                // detached, FindWatchedWindow returns null and this is a no-op.
                UnWatch(window);
            }
        }

        private static void OnWindowSourceInitialized(object? sender, EventArgs e)
        {
            if (sender is not Window window)
            {
                return;
            }
            window.SourceInitialized -= OnWindowSourceInitialized;
            lock (_lock)
            {
                if (FindWatchedWindow(window) is WatchedWindow watched)
                {
                    AttachHook(watched);
                }
            }
        }

        private static void AttachHook(WatchedWindow watched)
        {
            if (watched.IsHooked)
            {
                return;
            }
            WindowInteropHelper helper = new(watched.Window);
            IntPtr handle = helper.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            HwndSource source = HwndSource.FromHwnd(handle);
            if (source is null)
            {
                return;
            }
            watched.HwndSource = source;
            source.AddHook(WndProc);
            watched.IsHooked = true;
        }

        private static void DetachHook(WatchedWindow watched)
        {
            if (!watched.IsHooked || watched.HwndSource is null)
            {
                return;
            }
            watched.HwndSource.RemoveHook(WndProc);
            watched.IsHooked = false;
            watched.HwndSource = null;
        }

        private static WatchedWindow? FindWatchedWindow(Window window)
        {
            for (int i = 0; i < _watchedWindows.Count; i++)
            {
                if (ReferenceEquals(_watchedWindows[i].Window, window))
                {
                    return _watchedWindows[i];
                }
            }
            return null;
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            bool isThemeRelevant = (uint)msg switch
            {
                // WM_SETTINGCHANGE fires for every settings-class change (region, sound, policy,
                // file types, etc.). Only the "ImmersiveColorSet" broadcast carries a
                // theme/accent change. Ignore unrelated broadcasts rather than scheduling
                // unnecessary refreshes; relevant messages share one pending dispatcher operation.
                PInvoke.WM_SETTINGCHANGE => IsImmersiveColorSetBroadcast(lParam),
                PInvoke.WM_DWMCOLORIZATIONCOLORCHANGED => true,
                PInvoke.WM_THEMECHANGED => true,
                PInvoke.WM_SYSCOLORCHANGE => true,
                _ => false,
            };

            if (!isThemeRelevant)
            {
                return IntPtr.Zero;
            }

            RequestRefresh();
            return IntPtr.Zero;
        }

        internal static void ResetForTesting()
        {
            DispatcherOperation? pending;
            lock (_lock)
            {
                pending = _pendingRefresh;
                _pendingRefresh = null;
            }
            _ = pending?.Abort();
        }

        internal static void RequestRefresh()
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is not { HasShutdownStarted: false, HasShutdownFinished: false })
            {
                return;
            }
            lock (_lock)
            {
                if (_pendingRefresh?.Status is DispatcherOperationStatus.Pending or DispatcherOperationStatus.Executing)
                {
                    return;
                }
                _pendingRefresh = dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(RefreshOnApplicationDispatcher));
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> when a WM_SETTINGCHANGE lParam points to the Unicode string
        /// <c language="csharp">"ImmersiveColorSet"</c>, the canonical signal Personalization sends when the
        /// user changes the Windows accent or app/system theme. WPF's HwndSource uses the
        /// Unicode window-class variant, so the string is UTF-16 LE.
        /// </summary>
        /// <param name="lParam">A pointer to a Unicode string containing the setting change information.</param>
        private static bool IsImmersiveColorSetBroadcast(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero)
            {
                return false;
            }
            string? text = Marshal.PtrToStringUni(lParam);
            return !string.IsNullOrWhiteSpace(text)
                && text.Equals("ImmersiveColorSet", StringComparison.OrdinalIgnoreCase);
        }

        private static void RefreshOnApplicationDispatcher()
        {
            lock (_lock)
            {
                _pendingRefresh = null;
            }
            // Clear pending state before Apply and release the lock before synchronous
            // Changed handlers run. A newer notification can then queue a follow-up.
            ApplicationThemeManager.Apply(
                ApplicationThemeManager.CurrentTheme,
                ApplicationThemeManager.CurrentBackdrop);
        }

        /// <summary>
        /// Represents a window being monitored for changes or events within the application.
        /// </summary>
        /// <param name="window">The window instance to be tracked by this object. Cannot be null.</param>
        private sealed class WatchedWindow(Window window)
        {
            internal Window Window { get; } = window;
            internal HwndSource? HwndSource { get; set; }
            internal bool IsHooked { get; set; }
        }

        /// <summary>
        /// Contains the collection of windows currently being monitored for changes.
        /// </summary>
        /// <remarks>This list is intended for internal use to track windows of interest. It should not be
        /// modified directly outside of the containing class.</remarks>
        private static readonly List<WatchedWindow> _watchedWindows = [];

        /// <summary>
        /// Provides a synchronization object for thread-safe operations within the containing class.
        /// </summary>
        /// <remarks>Use this object with lock statements to ensure that critical sections are accessed by
        /// only one thread at a time. This helps prevent race conditions and ensures data consistency in multithreaded
        /// scenarios.</remarks>
        private static readonly Lock _lock = new();

        /// <summary>
        /// The one refresh waiting on the application dispatcher, protected by <see cref="_lock"/>.
        /// </summary>
        private static DispatcherOperation? _pendingRefresh;
    }
}
