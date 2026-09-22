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

using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control.Rules
{
    /// <summary>
    /// A control whose style declares a font size smaller than the body default must render its
    /// text at that size.
    /// </summary>
    /// <remarks>
    /// String content is drawn by a TextBlock the ContentPresenter generates, and the implicit
    /// TextBlock style in Typography.xaml sizes every unstyled TextBlock at 14. An implicit style
    /// beats property value inheritance in WPF, so a template that passes its size down through
    /// TextElement.FontSize alone loses the contest and the declared size never reaches the text.
    /// Each control below therefore scopes a TextBlock style to its own presenter; this test is
    /// what keeps that scoping in place.
    /// </remarks>
    public sealed class DeclaredFontSizeTests : IClassFixture<LightThemeFixture>
    {
        public DeclaredFontSizeTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task InfoBadge_ValueText_RendersAtTheDeclaredSizeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new() { Value = 12 };
                AssertRenderedSize(badge, badge, 11.0);
            });
        }

        [Fact]
        public Task NavigationViewItemHeader_RendersAtTheDeclaredSizeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                NavigationViewItemHeader header = new() { Content = "Section" };
                AssertRenderedSize(header, header, 12.0);
            });
        }

        [Fact]
        public Task TabViewItem_Header_RendersAtTheDeclaredSizeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem tab = new() { Header = "Tab" };
                AssertRenderedSize(tab, tab, 12.0);
            });
        }

        [Fact]
        public Task ToolTip_RendersAtTheDeclaredSizeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // The library styles its own ToolTip subclass; the framework's stays on the WPF default.
                ToolTip tip = new() { Content = "Tip" };
                System.Windows.Controls.Border host = new() { ToolTip = tip };
                Window window = new() { Content = host, Width = 240, Height = 160 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    tip.IsOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.TextBlock text = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChild<System.Windows.Controls.TextBlock>(tip), exactMatch: false);
                    Assert.Equal(12.0, text.FontSize, 0.01);
                }
                finally
                {
                    tip.IsOpen = false;
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static void AssertRenderedSize(FrameworkElement content, System.Windows.Controls.Control control, double expected)
        {
            Window window = new() { Content = content, Width = 240, Height = 160 };

            try
            {
                window.Show();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                System.Windows.Controls.TextBlock text = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChild<System.Windows.Controls.TextBlock>(content), exactMatch: false);
                Assert.Equal(expected, control.FontSize, 0.01);
                Assert.Equal(expected, text.FontSize, 0.01);
                Assert.True(
                    text.ActualHeight > 0,
                    string.Format(CultureInfo.InvariantCulture, "The text must render; it measured {0}.", text.ActualHeight));
            }
            finally
            {
                CloseWindowAndDrain(window);
            }
        }
    }
}
