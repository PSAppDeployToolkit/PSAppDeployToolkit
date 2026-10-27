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

using Fluence.Wpf.Theming;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public sealed class ThemeResolverTests
    {
        [Theory]
        [InlineData("aero", false, ApplicationTheme.Dark)]
        [InlineData("basic", false, ApplicationTheme.Dark)]
        [InlineData("aerolite", false, ApplicationTheme.Dark)]
        [InlineData("themea", false, ApplicationTheme.Dark)]
        [InlineData("themeb", false, ApplicationTheme.Dark)]
        [InlineData("themec", false, ApplicationTheme.Dark)]
        [InlineData("themed", false, ApplicationTheme.Dark)]
        [InlineData("dark", true, ApplicationTheme.Light)]
        [InlineData("customdark", true, ApplicationTheme.Light)]
        [InlineData(null, false, ApplicationTheme.Dark)]
        [InlineData(null, true, ApplicationTheme.Light)]
        [InlineData("", false, ApplicationTheme.Dark)]
        [InlineData("", true, ApplicationTheme.Light)]
        [InlineData("custom", false, ApplicationTheme.Dark)]
        [InlineData("custom", true, ApplicationTheme.Light)]
        public void ResolveAuto_UsesAppModeRegardlessOfOrdinaryThemeFilename(string? filename, bool appLight, ApplicationTheme expected)
        {
            Assert.Equal(expected, ThemeResolver.ResolveAuto(isHighContrastEnabled: false, filename, appLight));
        }

        [Theory]
        [InlineData(true, null, false)]
        [InlineData(true, null, true)]
        [InlineData(true, "aero", false)]
        [InlineData(true, "aero", true)]
        [InlineData(true, "dark", false)]
        [InlineData(true, "dark", true)]
        [InlineData(false, "hc1", false)]
        [InlineData(false, "hc1", true)]
        [InlineData(false, "hc2", false)]
        [InlineData(false, "hc2", true)]
        [InlineData(false, "hcblack", false)]
        [InlineData(false, "hcblack", true)]
        [InlineData(false, "hcwhite", false)]
        [InlineData(false, "hcwhite", true)]
        public void ResolveAuto_HighContrastAlwaysWins(bool highContrast, string? filename, bool appLight)
        {
            Assert.Equal(ApplicationTheme.HighContrast, ThemeResolver.ResolveAuto(highContrast, filename, appLight));
        }

        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        [InlineData(ApplicationTheme.HighContrast)]
        public void Resolve_ExplicitRequestIsPreserved(ApplicationTheme theme)
        {
            Assert.Equal(theme, ThemeResolver.Resolve(theme));
        }
    }
}
