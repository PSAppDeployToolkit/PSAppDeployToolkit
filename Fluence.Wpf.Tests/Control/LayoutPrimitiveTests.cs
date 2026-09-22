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
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Layout primitive defaults: <see cref="Controls.Border"/>, <see cref="Controls.StackPanel"/>
    /// and <see cref="Controls.DockPanel"/> each roundtrip their Fluence-only property defaults.
    /// </summary>
    public sealed class LayoutPrimitiveTests : IClassFixture<LightThemeFixture>
    {
        public LayoutPrimitiveTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task Stage3_Border_Variant_DefaultIsNoneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Border border = new();
                Assert.Equal(BorderVariant.None, border.Variant);
            });
        }

        [Fact]
        public Task Stage3_StackPanel_Spacing_DefaultZeroAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.StackPanel panel = new();
                Assert.Equal(0.0, panel.Spacing);
            });
        }

        [Fact]
        public Task Stage3_DockPanel_LastChildFill_DefaultTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.DockPanel dock = new();
                Assert.True(dock.LastChildFill);
            });
        }
    }
}
