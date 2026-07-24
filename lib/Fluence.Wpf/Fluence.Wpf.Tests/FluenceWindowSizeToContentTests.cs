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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Regression tests for the SizeToContent double-border fix in <see cref="Controls.FluenceWindow"/>.
    /// A <see cref="Window"/> with an active <see cref="Window.SizeToContent"/> sizes its HWND to the
    /// latest content-desired size, but the template root <c>Border</c> (the accent-bordered window
    /// chrome) was left arranged one layout pass behind the realised client area, so it floated
    /// inside the DWM accent border on every edge. The fix re-arranges the root visual to the full
    /// client area whenever SizeToContent is active, while keeping SizeToContent's auto-grow behavior.
    /// </summary>
    [TestClass]
    public class FluenceWindowSizeToContentTests
    {
        /// <summary>
        /// Tolerance (in DIPs) between the window's client size and the template root border's
        /// arranged size. Layout rounding can introduce a sub-pixel difference; anything larger is the
        /// multi-DIP inset that produced the double border.
        /// </summary>
        private const double FillTolerance = 1.0;

        private static void RunOnStaThread(Action action)
        {
            Exception? captured = null;
            WpfTestSta.Dispatcher?.Invoke(new Action(delegate
            {
                try { action(); }
                catch (Exception ex) { captured = ex; }
            }));

            if (captured is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        private static void DrainDispatcher()
        {
            // ApplicationIdle is below layout/render priority, so a drain at this level lets all
            // queued measure/arrange/render and SizeToContent-driven resize callbacks complete before
            // the caller samples the border - the same level WpfTestSta.DrainDispatcher uses.
            _ = WpfTestSta.Dispatcher?.Invoke(
                DispatcherPriority.ApplicationIdle,
                new Action(static () => { }));
        }

        private static void ResetAndApply(Application? app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
        }

        private static Border FindWindowBorder(Controls.FluenceWindow window)
        {
            Border? border = WpfTestSta
                .FindVisualDescendants<Border>(window)
                .FirstOrDefault(static b => string.Equals(b.Name, "WindowBorder", StringComparison.Ordinal));
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
        /// Before the fix the border was inset several DIPs on every edge, which read as a second
        /// accent border floating inside the DWM border. This assertion fails on the pre-fix code.
        /// </summary>
        [TestMethod]
        public void SizeToContentWindow_TemplateBorder_FillsClientArea()
        {
            RunOnStaThread(static () =>
            {
                Application? app = WpfTestSta.EnsureApplication();
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
                    DrainDispatcher();

                    Border border = FindWindowBorder(window);

                    Assert.IsTrue(window.ActualWidth > 0 && window.ActualHeight > 0,
                        "The window must have a realised non-zero size after Show() with SizeToContent.");

                    // The root border must coincide with the client area (window ActualWidth/Height
                    // equal the client area in DIPs). A larger gap is the inset that floated the
                    // template accent border inside the DWM accent border (the double-border bug).
                    Assert.AreEqual(window.ActualWidth, border.ActualWidth, FillTolerance,
                        "SizeToContent window: template root border width must fill the client area (no inset).");
                    Assert.AreEqual(window.ActualHeight, border.ActualHeight, FillTolerance,
                        "SizeToContent window: template root border height must fill the client area (no inset).");
                }
                finally
                {
                    window.Close();
                    DrainDispatcher();
                }
            });
        }

        /// <summary>
        /// The fix must not freeze SizeToContent: when the content grows at runtime (the scenario the
        /// PowerShell dialogs rely on when their validation InfoBar opens), the window must still grow
        /// AND the template root border must still fill the new, larger client area (stay
        /// single-bordered after growing).
        /// </summary>
        [TestMethod]
        public void SizeToContentWindow_StillGrowsAndStaysFilled_WhenContentGrows()
        {
            RunOnStaThread(static () =>
            {
                Application? app = WpfTestSta.EnsureApplication();
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
                    DrainDispatcher();

                    double heightBeforeGrow = window.ActualHeight;

                    // Simulate the validation InfoBar opening: add a tall row so the window auto-grows.
                    // Settle via a dispatcher drain (not a synchronous UpdateLayout): UpdateLayout would
                    // itself force the fill, masking whether the fix is what keeps the border flush
                    // after a SizeToContent-driven grow.
                    _ = panel.Children.Add(new Border { Height = 120, Margin = new Thickness(0, 12, 0, 0) });
                    DrainDispatcher();

                    Assert.IsTrue(window.ActualHeight > heightBeforeGrow,
                        "SizeToContent must remain active so the window grows when its content grows.");

                    Border border = FindWindowBorder(window);
                    Assert.AreEqual(window.ActualWidth, border.ActualWidth, FillTolerance,
                        "After auto-grow the template root border width must still fill the client area.");
                    Assert.AreEqual(window.ActualHeight, border.ActualHeight, FillTolerance,
                        "After auto-grow the template root border height must still fill the client area (single border preserved).");
                }
                finally
                {
                    window.Close();
                    DrainDispatcher();
                }
            });
        }

        /// <summary>
        /// A fixed-size window already renders with the borders coincident; the fill correction must
        /// be a no-op for it (its template root border fills the client area before and after the
        /// fix). This pins that the fix does not regress fixed-size windows.
        /// </summary>
        [TestMethod]
        public void FixedSizeWindow_TemplateBorder_FillsClientArea()
        {
            RunOnStaThread(static () =>
            {
                Application? app = WpfTestSta.EnsureApplication();
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
                    DrainDispatcher();

                    Border border = FindWindowBorder(window);
                    Assert.AreEqual(window.ActualWidth, border.ActualWidth, FillTolerance,
                        "Fixed-size window: template root border width must fill the client area.");
                    Assert.AreEqual(window.ActualHeight, border.ActualHeight, FillTolerance,
                        "Fixed-size window: template root border height must fill the client area.");
                }
                finally
                {
                    window.Close();
                    DrainDispatcher();
                }
            });
        }
    }
}
