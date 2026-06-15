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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Shared single-threaded STA dispatcher for WPF tests so <see cref="Application.Current"/>
    /// and all windows share one dispatcher (avoids cross-thread DynamicResource failures).
    /// </summary>
    internal static class WpfTestSta
    {
        private static Dispatcher? _dispatcher;
        private static readonly Lock LockObj = new();

        internal static Dispatcher? Dispatcher => EnsureDispatcher();

        internal static Application? EnsureApplication()
        {
            return Invoke(static () =>
            {
                if (Application.Current is null)
                {
                    Application app = new()
                    {
                        ShutdownMode = ShutdownMode.OnExplicitShutdown,
                    };
                }

                return Application.Current;
            });
        }

        internal static void Invoke(Action action)
        {
            EnsureDispatcher()?.Invoke(action);
        }

        internal static T? Invoke<T>(Func<T> func) where T : class?
        {
            return EnsureDispatcher()?.Invoke(func);
        }

        /// <summary>
        /// Runs <paramref name="action"/> on the shared STA dispatcher, capturing any exception it
        /// throws and rethrowing it on the calling thread with its original stack trace preserved
        /// (via <see cref="ExceptionDispatchInfo"/>) so test assertions surface where the caller
        /// expects them. This is the single canonical implementation; per-fixture wrappers forward
        /// here.
        /// </summary>
        /// <param name="action">The action to run on the STA dispatcher.</param>
        internal static void RunOnSta(Action action)
        {
            Exception? captured = null;
            EnsureDispatcher()?.Invoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    captured = exception;
                }
            }));

            if (captured is not null)
            {
                ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        /// <summary>
        /// Drains the dispatcher queue down to <see cref="DispatcherPriority.ApplicationIdle"/> so
        /// any queued layout, render, and idle callbacks complete before the caller samples state.
        /// </summary>
        /// <param name="dispatcher">The dispatcher to drain.</param>
        internal static void DrainDispatcher(Dispatcher? dispatcher)
        {
            _ = dispatcher?.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));
        }

        /// <summary>
        /// Enumerates every descendant of <paramref name="root"/> of type <typeparamref name="T"/>
        /// walking the <b>visual</b> tree only (depth-first, pre-order). This is the lightweight
        /// variant used by control-template tests where the visual tree is the source of truth.
        /// </summary>
        /// <param name="root">The root element to start the search from.</param>
        /// <typeparam name="T">The type of descendant to find.</typeparam>
        internal static IEnumerable<T> FindVisualDescendants<T>(DependencyObject? root)
            where T : DependencyObject
        {
            if (root is null)
            {
                yield break;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (T descendant in FindVisualDescendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Enumerates every descendant of <paramref name="root"/> of type <typeparamref name="T"/>
        /// walking <b>both the visual and logical</b> trees, guarding against revisiting a node so
        /// shared subtrees and cycles do not loop. This broader variant is used by demo/full-window
        /// tests where content can live in the logical tree before (or instead of) being realized in
        /// the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of descendant to find.</typeparam>
        /// <param name="root">The root element to start the search from.</param>
        internal static IEnumerable<T> FindLogicalAndVisualDescendants<T>(DependencyObject? root)
            where T : DependencyObject
        {
            HashSet<DependencyObject> visited = [];
            foreach (T item in FindLogicalAndVisualDescendants<T>(root, visited))
            {
                yield return item;
            }
        }

        private static IEnumerable<T> FindLogicalAndVisualDescendants<T>(
            DependencyObject? root,
            HashSet<DependencyObject> visited)
            where T : DependencyObject
        {
            if (root is null || !visited.Add(root))
            {
                yield break;
            }

            if (root is T match)
            {
                yield return match;
            }

            int visualChildren = 0;
            if (root is Visual or Visual3D)
            {
                visualChildren = VisualTreeHelper.GetChildrenCount(root);
            }

            for (int i = 0; i < visualChildren; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                foreach (T item in FindLogicalAndVisualDescendants<T>(child, visited))
                {
                    yield return item;
                }
            }

            foreach (object logicalChild in LogicalTreeHelper.GetChildren(root))
            {
                if (logicalChild is DependencyObject dependencyObject)
                {
                    foreach (T item in FindLogicalAndVisualDescendants<T>(dependencyObject, visited))
                    {
                        yield return item;
                    }
                }
            }
        }

        private static Dispatcher? EnsureDispatcher()
        {
            lock (LockObj)
            {
                if (_dispatcher?.Thread.IsAlive == true)
                {
                    return _dispatcher;
                }

                Dispatcher? created = null;
                using ManualResetEventSlim ready = new(initialState: false);
                Thread thread = new(() =>
                {
                    created = Dispatcher.CurrentDispatcher;
                    ready.Set();
                    Dispatcher.Run();
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
                ready.Wait();
                _dispatcher = created;
                return _dispatcher;
            }
        }
    }
}
