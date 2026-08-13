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
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public void InfoBar_GetSeverityGlyph_MatchesTemplateGlyphs()
        {
            Assert.Equal("", InfoBar.GetSeverityGlyph(InfoBarSeverity.Informational), StringComparer.Ordinal);
            Assert.Equal("", InfoBar.GetSeverityGlyph(InfoBarSeverity.Success), StringComparer.Ordinal);
            Assert.Equal("", InfoBar.GetSeverityGlyph(InfoBarSeverity.Warning), StringComparer.Ordinal);
            Assert.Equal("", InfoBar.GetSeverityGlyph(InfoBarSeverity.Error), StringComparer.Ordinal);
        }

        [Fact]
        public void InfoBar_GetSeverityBrushKey_ResolvesToThemeBrush()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);
                foreach (InfoBarSeverity severity in new[]
                {
                    InfoBarSeverity.Informational, InfoBarSeverity.Success,
                    InfoBarSeverity.Warning, InfoBarSeverity.Error,
                })
                {
                    string key = InfoBar.GetSeverityBrushKey(severity);
                    _ = Assert.IsAssignableFrom<Brush>(Application.Current.TryFindResource(key));
                }
            });
        }
    }
}
