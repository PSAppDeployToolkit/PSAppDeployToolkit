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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Builds a <see cref="ResourceDictionary"/> from a color map, publishing each Color token
    /// and a frozen <see cref="SolidColorBrush"/> twin (key + "Brush") for every entry.
    /// </summary>
    internal static class BrushFactory
    {
        // Color keys whose canonical brush twin drops the "Color" suffix (e.g. ApplicationBackgroundColor
        // -> ApplicationBackgroundBrush, not ApplicationBackgroundColorBrush). BrushFactory skips the
        // auto-twin for these keys; SpecialBrushes.Add emits the correctly-named brush instead.
        private static readonly System.Collections.Generic.HashSet<string> NoAutoTwinKeys =
            new(System.StringComparer.Ordinal) { "ApplicationBackgroundColor" };

        /// <summary>
        /// Returns a <see cref="ResourceDictionary"/> containing every Color token and its
        /// corresponding frozen <see cref="SolidColorBrush"/> from <paramref name="colors"/>.
        /// Color keys in the suppress-auto-twin list are published as Color tokens only; their
        /// brush twin uses an irregular name and is emitted by <see cref="SpecialBrushes"/>.
        /// </summary>
        internal static ResourceDictionary Build(IReadOnlyDictionary<string, Color> colors)
        {
            ResourceDictionary d = [];
            foreach (KeyValuePair<string, Color> kv in colors)
            {
                d[kv.Key] = kv.Value;                    // publish the Color token
                if (!NoAutoTwinKeys.Contains(kv.Key))
                {
                    d[kv.Key + "Brush"] = Frozen(kv.Value);  // and its frozen brush twin
                }
            }
            return d;
        }

        private static SolidColorBrush Frozen(Color c)
        {
            SolidColorBrush b = new(c);
            b.Freeze();
            return b;
        }
    }
}
