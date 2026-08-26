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

using System.Windows;

namespace Fluence.Wpf.Markup
{
    /// <summary>
    /// A per-theme value table inside <see cref="ThemeDictionary.ThemeDictionaries"/>. The
    /// <see cref="ThemeKey"/> names the theme the table serves: <c>Light</c>, <c>Dark</c>,
    /// <c>HighContrast</c>, the high-contrast polarity keys <c>HighContrastBlack</c> and
    /// <c>HighContrastWhite</c>, or <c>Default</c> (the fallback for themes without an exact
    /// table).
    /// </summary>
    /// <remarks>
    /// The key lives on this property rather than <c>x:Key</c> because the WPF markup compiler
    /// cannot compile keyed children inside a dictionary-typed property of a
    /// <see cref="ResourceDictionary"/> subclass. Set <see cref="ThemeKey"/> before adding the
    /// table to a <see cref="ThemeDictionary"/>; changing it afterwards takes effect on the
    /// next theme change or collection mutation.
    /// </remarks>
    public sealed class ThemeResourceDictionary : ResourceDictionary
    {
        /// <summary>
        /// Gets or sets the theme this table serves: <c>Light</c>, <c>Dark</c>,
        /// <c>HighContrast</c>, <c>HighContrastBlack</c>, <c>HighContrastWhite</c>, or
        /// <c>Default</c>.
        /// </summary>
        public string ThemeKey { get; set; } = "Default";
    }
}
