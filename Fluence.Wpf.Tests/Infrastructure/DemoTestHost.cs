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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Fluence.Wpf.Demo;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    internal static class DemoTestHost
    {
        internal static Task RunOnStaAsync(Action action)
        {
            return WpfTestSta.RunOnStaAsync(action);
        }

        internal static Application EnsureDemoTheme(WindowBackdropType backdrop = WindowBackdropType.None)
        {
            return TestApp.EnsureDemoTheme(backdrop);
        }

        internal static Window CreateHostWindow(UIElement content)
        {
            Window window = new()
            {
                Left = -20000,
                Top = -20000,
                Width = 1040,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
                Content = content,
            };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            return window;
        }

        /// <summary>
        /// Creates, shows and lays out a <see cref="MainWindow"/> off-screen. The page classes
        /// under <c language="cs">Gallery.Pages</c> reach this as
        /// <c language="cs">DemoTestHost.CreateShownMainWindow()</c> on the rare test that
        /// needs the real shell rather than its own page in isolation.
        /// </summary>
        internal static MainWindow CreateShownMainWindow()
        {
            MainWindow window = new()
            {
                Left = -20000,
                Top = -20000,
                Width = 1200,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
            };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            return window;
        }

        internal static void CloseWindow(Window window)
        {
            window.Content = null;
            window.Close();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
        }

        /// <summary>
        /// Builds <paramref name="createPage"/>, hosts it in a fresh <see cref="Window"/>, shows
        /// and lays it out, runs <paramref name="verify"/> against the window, then closes it.
        /// The demo theme is not applied here: every caller's own test class already applies it
        /// once through its <see cref="IAsyncLifetime.InitializeAsync"/>, so an inner call
        /// here would only reapply it a second time per test.
        /// </summary>
        /// <param name="createPage">Creates the page under test.</param>
        /// <param name="verify">Asserts against the shown, laid-out host window.</param>
        internal static Task RunDemoPageTestAsync(Func<FrameworkElement> createPage, Action<Window> verify)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                FrameworkElement page = createPage();
                Window window = new()
                {
                    Width = 900,
                    Height = 700,
                    Content = page,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    verify(window);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        internal static T? FindByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            return FindVisualChildren<T>(root).FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        }

        // Logical+visual descendant search with cycle guarding. Forwards to the canonical
        // WpfTestSta.FindLogicalAndVisualDescendants; the distinct name there documents how this
        // differs from the visual-only ControlTests variant (FindVisualDescendants).
        internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject? root)
            where T : DependencyObject
        {
            return WpfTestSta.FindLogicalAndVisualDescendants<T>(root);
        }

        internal static string GetRepositoryFilePath(params string[] relativeSegments)
        {
            string root = Path.GetFullPath(Path.Join(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string[] pathParts = new string[relativeSegments.Length + 1];
            pathParts[0] = root;
            Array.Copy(relativeSegments, 0, pathParts, 1, relativeSegments.Length);
            return Path.Join(pathParts);
        }

        internal static Task<string> ReadRepositoryFileAsync(params string[] relativeSegments)
        {
            string path = GetRepositoryFilePath(relativeSegments);
            return File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        }
    }
}
