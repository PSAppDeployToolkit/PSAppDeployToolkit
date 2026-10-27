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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Windowing
{
    // FluenceWindow leaves Window.Icon unset for consumers and exposes an opt-in Fluence brand icon.
    // The square, no-background mark is an embedded 256x256 PNG because Window.Icon drives the Win32
    // taskbar HICON, which does not render a vector and would distort a non-square source.
    public sealed class WindowIconTests : IClassFixture<LightThemeFixture>
    {
        public WindowIconTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task FluenceWindow_Icon_IsUnsetByDefaultAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                FluenceWindow window = new();
                try
                {
                    Assert.Null(window.Icon);
                    Assert.Same(DependencyProperty.UnsetValue, window.ReadLocalValue(Window.IconProperty));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_Icon_StyleSetterIsRespectedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                FluenceWindow window = new();
                try
                {
                    ImageSource styleIcon = BitmapSource.Create(
                        1, 1, 96, 96, PixelFormats.Bgra32, palette: null, new byte[] { 0, 0, 0, 255 }, 4);
                    styleIcon.Freeze();

                    Style style = new(typeof(FluenceWindow));
                    style.Setters.Add(new Setter(Window.IconProperty, styleIcon));
                    window.Style = style;

                    Assert.Same(styleIcon, window.Icon);
                    Assert.Same(DependencyProperty.UnsetValue, window.ReadLocalValue(Window.IconProperty));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_Icon_ConsumerAssignedValueIsPreservedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                FluenceWindow window = new();
                try
                {
                    // A 1x1 in-memory bitmap stands in for a consumer-supplied icon so the test
                    // does not depend on any external resource.
                    ImageSource consumerIcon = BitmapSource.Create(
                        1, 1, 96, 96, PixelFormats.Bgra32, palette: null, new byte[] { 0, 0, 0, 255 }, 4);
                    consumerIcon.Freeze();

                    window.Icon = consumerIcon;

                    Assert.Same(consumerIcon, window.Icon);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_DefaultIcon_IsSquareBitmapSourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                BitmapSource icon = Assert.IsType<BitmapSource>(FluenceWindow.DefaultIcon, exactMatch: false);
                Assert.Equal(icon.PixelWidth, icon.PixelHeight);
            });
        }
    }
}
