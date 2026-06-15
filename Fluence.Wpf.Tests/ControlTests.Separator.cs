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
using System.Windows.Controls;
using System.Windows.Media;
using FluenceSeparator = Fluence.Wpf.Controls.Separator;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Gap-audit tests: Fluent <see cref="Controls.Separator"/> control.
    /// Authority: .NET 10 WPF PresentationFramework.Fluent/Styles/Separator.xaml.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // Separator
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void Separator_DefaultStyle_Applies()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceSeparator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Template applied - Border is the root of the template
                Border? border = FindVisualChild<Border>(sep);
                Assert.IsNotNull(border, "Separator template must contain a Border.");
                w.Close();
            });
        }

        [TestMethod]
        public void Separator_Height_IsOne()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceSeparator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(1.0, sep.Height,
                    "Separator Height must be 1 per .NET 10 WPF Fluent Separator style.");
                w.Close();
            });
        }

        [TestMethod]
        public void Separator_Background_UsesDividerStrokeColorDefaultBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceSeparator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? bg = sep.Background as SolidColorBrush;
                SolidColorBrush? expected = app?.TryFindResource("DividerStrokeColorDefaultBrush") as SolidColorBrush;

                Assert.IsNotNull(expected, "DividerStrokeColorDefaultBrush must resolve.");
                Assert.IsNotNull(bg, "Separator.Background must be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, bg.Color,
                    "Separator.Background must use DividerStrokeColorDefaultBrush.");
                w.Close();
            });
        }

        [TestMethod]
        public void Separator_ThemeCycle_StyleRemainsApplied()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceSeparator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                Border? border = FindVisualChild<Border>(sep);
                Assert.IsNotNull(border, "Separator template Border must still exist after theme cycle.");
                w.Close();
            });
        }
    }
}
