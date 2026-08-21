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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Loads per-theme Color tokens from the XAML resource dictionaries. The few
    /// theme-independent tokens (the Windows close-button brand colors) are seeded in
    /// code by <see cref="AddSharedColors"/>; only the per-theme Color-only tables are
    /// read from XAML. Brushes are built entirely in C# by <see cref="BrushFactory"/>
    /// and <see cref="SpecialBrushes"/>.
    /// </summary>
    internal static class BaseColorTables
    {
        private const string PackBase = "pack://application:,,,/Fluence.Wpf;component/";

        /// <summary>
        /// Returns a dictionary of Color keys for the given <paramref name="theme"/>,
        /// combining shared tokens and per-theme overrides.
        /// </summary>
        /// <param name="theme">The theme to load.</param>
        internal static Dictionary<string, Color> Load(ApplicationTheme theme)
        {
            Dictionary<string, Color> map = new(StringComparer.Ordinal);
            AddSharedColors(map);
            ReadColors(PackBase + "Themes/Colors/Theme." + Name(theme) + ".xaml", map);
            return map;
        }

        /// <summary>
        /// Seeds the theme-independent Color tokens that are identical across Light, Dark, and
        /// HighContrast. These are the canonical Windows close-button brand colors - the shell uses
        /// the same hover and pressed red and the white foreground in every theme - so they are
        /// defined here in code rather than duplicated across the per-theme XAML tables. Seeded
        /// before the per-theme table so a future theme could still override them.
        /// </summary>
        /// <param name="map">The color table to seed.</param>
        private static void AddSharedColors(Dictionary<string, Color> map)
        {
            map["WindowCloseButtonBackgroundPointerOver"] = Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C);
            map["WindowCloseButtonBackgroundPressed"] = Color.FromArgb(0xFF, 0xB4, 0x27, 0x1C);
            map["WindowCloseButtonForegroundPointerOver"] = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        }

        private static string Name(ApplicationTheme t)
        {
            return t switch
            {
                ApplicationTheme.Dark => "Dark",
                ApplicationTheme.HighContrast => "HighContrast",
                ApplicationTheme.Light => "Light",
                ApplicationTheme.Auto => "Light",
                _ => "Light",
            };
        }

        private static void ReadColors(string uri, Dictionary<string, Color> map)
        {
            ResourceDictionary d = new() { Source = new Uri(uri, UriKind.Absolute) };
            foreach (object key in d.Keys)
            {
                if (key is string ks && d[ks] is Color c) { map[ks] = c; }
            }
        }
    }
}
