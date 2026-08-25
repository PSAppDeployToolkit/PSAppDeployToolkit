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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Regression tests for the SizeToContent inset-border fix in <see cref="Controls.FluenceWindow"/>.
    /// A <see cref="Window"/> with an active <see cref="Window.SizeToContent"/> sizes its HWND to the
    /// latest content-desired size, but the template root <c>Border</c> (the window's only outline)
    /// was left arranged one layout pass behind the realised client area, so it floated inset from
    /// the true window edge on every side with the backdrop showing through the strip between them.
    /// The fix re-arranges the root visual to the full client area whenever SizeToContent is active,
    /// while keeping SizeToContent's auto-grow behavior.
    /// </summary>
    public class FluenceWindowSizeToContentTests
    {
        /// <summary>
        /// Tolerance (in DIPs) between the window's client size and the template root border's
        /// arranged size. Layout rounding can introduce a sub-pixel difference; anything larger is the
        /// multi-DIP inset that floated the border away from the window edge.
        /// </summary>
        private const double FillTolerance = 1.0;

        private static void ResetAndApply(Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
        }

        private static Border FindWindowBorder(Controls.FluenceWindow window)
        {
            Border? border = WpfTestSta
                .FindVisualDescendants<Border>(window)
                .FirstOrDefault(static b => string.Equals(b.Name, "PART_WindowBorder", StringComparison.Ordinal));
            return border ?? throw new InvalidOperationException(
                "Expected the template root Border named 'WindowBorder' to be present after Show().");
        }

        private static StackPanel BuildContent()
        {
            StackPanel panel = new() { Margin = new Thickness(24) };
            foreach (string label in new[] { "Full name", "Age", "Country", "Start date" })
            {
                _ = panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });
                _ = panel.Children.Add(new TextBox { Margin = new Thickness(0, 0, 0, 12), MinWidth = 240 });
            }
            return panel;
        }

        /// <summary>
        /// A SizeToContent window must arrange its template root border to fill the realised client
        /// area (the window's ActualWidth/ActualHeight), exactly as a fixed-size window already does.
        /// Before the fix the border was inset several DIPs on every edge, which read as the window
        /// outline floating away from the window edge over the backdrop. This assertion fails on the
        /// pre-fix code.
        /// </summary>
        [Fact]
        public Task SizeToContentWindow_TemplateBorder_FillsClientAreaAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(app);

                Controls.FluenceWindow window = new()
                {
                    Title = "SizeToContent fill",
                    SystemBackdropType = BackdropType.None,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Content = BuildContent(),
                };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Border border = FindWindowBorder(window);

                    Assert.True(window.ActualWidth > 0 && window.ActualHeight > 0,
                        "The window must have a realised non-zero size after Show() with SizeToContent.");

                    // The root border must coincide with the client area (window ActualWidth/Height
                    // equal the client area in DIPs). A larger gap is the inset that floated the
                    // window outline away from the window edge, exposing the backdrop behind it.
                    Assert.Equal(window.ActualWidth, border.ActualWidth, FillTolerance);
                    Assert.Equal(window.ActualHeight, border.ActualHeight, FillTolerance);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        /// <summary>
        /// The fix must not freeze SizeToContent: when the content grows at runtime (the scenario the
        /// PowerShell dialogs rely on when their validation InfoBar opens), the window must still grow
        /// AND the template root border must still fill the new, larger client area (stay
        /// single-bordered after growing).
        /// </summary>
        [Fact]
        public Task SizeToContentWindow_StillGrowsAndStaysFilled_WhenContentGrowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(app);

                StackPanel panel = BuildContent();
                Controls.FluenceWindow window = new()
                {
                    Title = "SizeToContent grow",
                    SystemBackdropType = BackdropType.None,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Content = panel,
                };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    double heightBeforeGrow = window.ActualHeight;

                    // Simulate the validation InfoBar opening: add a tall row so the window auto-grows.
                    // Settle via a dispatcher drain (not a synchronous UpdateLayout): UpdateLayout would
                    // itself force the fill, masking whether the fix is what keeps the border flush
                    // after a SizeToContent-driven grow.
                    _ = panel.Children.Add(new Border { Height = 120, Margin = new Thickness(0, 12, 0, 0) });
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(window.ActualHeight > heightBeforeGrow,
                        "SizeToContent must remain active so the window grows when its content grows.");

                    Border border = FindWindowBorder(window);
                    Assert.Equal(window.ActualWidth, border.ActualWidth, FillTolerance);
                    Assert.Equal(window.ActualHeight, border.ActualHeight, FillTolerance);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        /// <summary>
        /// A fixed-size window already renders with the borders coincident; the fill correction must
        /// be a no-op for it (its template root border fills the client area before and after the
        /// fix). This pins that the fix does not regress fixed-size windows.
        /// </summary>
        [Fact]
        public Task FixedSizeWindow_TemplateBorder_FillsClientAreaAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(app);

                Controls.FluenceWindow window = new()
                {
                    Title = "Fixed size",
                    SystemBackdropType = BackdropType.None,
                    ShowInTaskbar = false,
                    Width = 420,
                    Height = 320,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Content = BuildContent(),
                };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Border border = FindWindowBorder(window);
                    Assert.Equal(window.ActualWidth, border.ActualWidth, FillTolerance);
                    Assert.Equal(window.ActualHeight, border.ActualHeight, FillTolerance);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }
    }
}
