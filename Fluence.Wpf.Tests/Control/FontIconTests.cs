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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.FontIcon"/> control: default font family and glyph roundtrip,
    /// rotation and spin state roundtrips, and the spin animation's pause/resume/stop behavior
    /// across visibility and load state changes.
    /// </summary>
    public sealed class FontIconTests : IClassFixture<LightThemeFixture>
    {
        public FontIconTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task FontIcon_DefaultFontFamily_IsSegoeFluentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.FontIcon fontIcon = new();

                Assert.Equal("Segoe Fluent Icons", fontIcon.IconFontFamily.Source, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task FontIcon_GlyphProperty_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.FontIcon fontIcon = new();
                const string testGlyph = "\uE710";

                fontIcon.Glyph = testGlyph;

                Assert.Equal(testGlyph, fontIcon.Glyph, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task Stage3_FontIcon_Rotation_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.FontIcon icon = new() { Rotation = 33 };
                Assert.Equal(33.0, icon.Rotation);
            });
        }

        [Fact]
        public Task Stage3_FontIcon_IsSpinning_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.FontIcon icon = new() { IsSpinning = true };
                Assert.True(icon.IsSpinning);
            });
        }

        [Fact]
        public Task Stage3_FontIcon_Spin_PausesWhenCollapsed_ResumesWhenVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.FontIcon icon = new()
                {
                    Glyph = "\uE72C",
                    IsSpinning = true,
                };

                try
                {
                    window.Content = icon;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    RotateTransform rotate = Assert.IsType<RotateTransform>(icon.Template.FindName("PART_Rotate", icon));
                    Assert.True(rotate.HasAnimatedProperties, "Spin animation must run while the icon is loaded and visible.");

                    icon.Visibility = Visibility.Collapsed;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(rotate.HasAnimatedProperties, "Spin animation must stop while the icon is collapsed.");
                    Assert.Equal(icon.Rotation, rotate.Angle);

                    icon.Visibility = Visibility.Visible;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(rotate.HasAnimatedProperties, "Spin animation must resume when the icon becomes visible again.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Stage3_FontIcon_Spin_StopsWhenUnloadedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.FontIcon icon = new()
                {
                    Glyph = "\uE72C",
                    IsSpinning = true,
                };

                try
                {
                    window.Content = icon;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    RotateTransform rotate = Assert.IsType<RotateTransform>(icon.Template.FindName("PART_Rotate", icon));
                    Assert.True(rotate.HasAnimatedProperties, "Spin animation must run while the icon is loaded and visible.");

                    window.Content = null;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(rotate.HasAnimatedProperties, "Spin animation must stop when the icon is unloaded.");
                    Assert.Equal(icon.Rotation, rotate.Angle);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Stage3_FontIcon_EnableTransitions_DefaultTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.FontIcon icon = new();
                Assert.True(icon.EnableTransitions);
            });
        }
    }
}
