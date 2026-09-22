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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests.Smoke
{
    /// <summary>
    /// The net8.0-windows smoke lane. Fluence.Wpf.Tests covers net472 and net10.0-windows; this
    /// lane exists so the third shipped binary is loaded and exercised rather than merely built,
    /// packed and published. It stays deliberately shallow: the theme pipeline runs, a window
    /// hosting a handful of controls renders, and templates and theme brushes resolve. Behaviour is
    /// the full suite's job on the other two frameworks, where the source is identical.
    /// </summary>
    public sealed class Net8SmokeTests
    {
        [Fact]
        public Task ThemePipeline_AppliesEveryThemeAndKeepsTheThreeSlotsAsync()
        {
            return RunOnStaAsync(static () =>
            {
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ApplicationThemeManager.Apply(theme);
                    Assert.Equal(theme, ApplicationThemeManager.CurrentTheme);
                    Assert.Equal(3, Application.Current.Resources.MergedDictionaries.Count);

                    object? brush = Application.Current.TryFindResource("TextFillColorPrimaryBrush");
                    _ = Assert.IsType<Brush>(brush, exactMatch: false);
                }
            });
        }

        [Fact]
        public Task Accent_AppliesACustomRampAsync()
        {
            return RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xC5, 0x0F, 0x1F));

                object? accent = Application.Current.TryFindResource("AccentFillColorDefaultBrush");
                SolidColorBrush resolved = Assert.IsType<SolidColorBrush>(accent, exactMatch: false);
                Assert.NotEqual(default, resolved.Color);

                ApplicationAccentColorManager.ApplySystemAccent();
            });
        }

        [Fact]
        public Task Controls_ApplyTheirTemplatesInAShownWindowAsync()
        {
            return RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light);

                Button button = new() { Content = "Smoke" };
                TextBox textBox = new() { Text = "Smoke" };
                ProgressBar progressBar = new() { Value = 40 };
                InfoBadge badge = new() { Value = 3 };
                System.Windows.Controls.StackPanel panel = new();
                _ = panel.Children.Add(button);
                _ = panel.Children.Add(textBox);
                _ = panel.Children.Add(progressBar);
                _ = panel.Children.Add(badge);

                Window window = new() { Content = panel, Width = 320, Height = 240 };

                try
                {
                    window.Show();
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.NotNull(button.Template);
                    Assert.NotNull(textBox.Template);
                    Assert.NotNull(progressBar.Template);
                    Assert.NotNull(badge.Template);
                    Assert.True(button.ActualHeight > 0, "The button must lay out.");
                    Assert.True(badge.ActualHeight > 0, "The badge must lay out.");
                }
                finally
                {
                    window.Close();
                    Drain(window.Dispatcher);
                }
            });
        }

        [Fact]
        public Task FluenceWindow_ShowsAndClosesAsync()
        {
            return RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);

                FluenceWindow window = new() { Width = 320, Height = 240 };

                try
                {
                    window.Show();
                    Drain(window.Dispatcher);
                    Assert.True(window.IsLoaded, "The window must load.");
                }
                finally
                {
                    window.Close();
                    Drain(window.Dispatcher);
                }
            });
        }

        /// <summary>
        /// Runs the test body on the lane's STA dispatcher, the way Fluence.Wpf.Tests does through
        /// its own WpfTestSta helper. That helper lives in a project targeting the other two
        /// frameworks, so this lane carries its own small copy.
        /// </summary>
        /// <param name="body">The test body.</param>
        /// <returns>A task that completes when the body has run.</returns>
        private static Task RunOnStaAsync(Action body)
        {
            return EnsureDispatcher().InvokeAsync(body).Task;
        }

        /// <summary>
        /// Returns the lane's STA dispatcher, starting its thread and the application on first use.
        /// </summary>
        /// <returns>The dispatcher every test body runs on.</returns>
        private static Dispatcher EnsureDispatcher()
        {
            if (_dispatcher is not null)
            {
                return _dispatcher;
            }

            using ManualResetEventSlim ready = new(initialState: false);
            Thread thread = new(() =>
            {
                _dispatcher = Dispatcher.CurrentDispatcher;
                if (Application.Current is null)
                {
                    _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                }

                ready.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait(TestContext.Current.CancellationToken);
            return _dispatcher!;
        }

        /// <summary>
        /// Drains the dispatcher queue so a shown window has laid out before the test samples it.
        /// </summary>
        /// <param name="dispatcher">The dispatcher to drain.</param>
        private static void Drain(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));
        }

        /// <summary>
        /// The lane's STA dispatcher, created once for the whole assembly.
        /// </summary>
        private static Dispatcher? _dispatcher;
    }
}
