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
using System.Windows;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class ThemeTestHelpersTests
    {
        [TestMethod]
        public void StandardThemeCycle_ResolvesKeyBrushes()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                app?.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplySystemAccent();

                ThemeTestHelpers.ApplyStandardThemeCycle(BackdropType.None, updateAccent: true);
                ThemeTestHelpers.AssertKeyThemeBrushesResolve(app);
            });
        }

#if NET10_0_OR_GREATER
        [TestMethod]
        public void VisualTreeHelper_GetDpi_ReturnsPositiveScale()
        {
            WpfTestSta.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                Window window = new() { Width = 200, Height = 120 };
                try
                {
                    Controls.Border panel = new() { Width = 10, Height = 10 };
                    window.Content = panel;
                    window.Show();
                    window.Dispatcher.Invoke(static () => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                    DpiScale dpi = System.Windows.Media.VisualTreeHelper.GetDpi(panel);
                    Assert.IsTrue(dpi.PixelsPerDip > 0, "DpiScale.PixelsPerDip should be positive.");
                }
                finally
                {
                    window.Close();
                }
            });
        }
#endif
    }
}
