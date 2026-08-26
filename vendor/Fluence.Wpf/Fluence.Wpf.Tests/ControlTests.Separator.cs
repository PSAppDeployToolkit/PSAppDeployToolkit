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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

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

        [Fact]
        public Task Separator_DefaultStyle_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Template applied - Border is the root of the template
                Border border = Assert.IsAssignableFrom<Border>(FindVisualChild<Border>(sep));
                w.Close();
            });
        }

        [Fact]
        public Task Separator_Height_IsOneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(1.0, sep.Height);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_Background_UsesDividerStrokeColorDefaultBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush bg = Assert.IsType<SolidColorBrush>(sep.Background);
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("DividerStrokeColorDefaultBrush"));

                Assert.Equal(expected.Color, bg.Color);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_ThemeCycle_StyleRemainsAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border border = Assert.IsAssignableFrom<Border>(FindVisualChild<Border>(sep));
                w.Close();
            });
        }
    }
}
