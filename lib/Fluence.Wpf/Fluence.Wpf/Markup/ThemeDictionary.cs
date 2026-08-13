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
using System.Collections.ObjectModel;
using System.Windows;

namespace Fluence.Wpf.Markup
{
    /// <summary>
    /// A resource dictionary with per-theme value tables, equivalent to WinUI 3
    /// <c>ResourceDictionary.ThemeDictionaries</c>. Populate <see cref="ThemeDictionaries"/>
    /// with <see cref="ThemeResourceDictionary"/> tables keyed <c>Light</c>, <c>Dark</c>,
    /// <c>HighContrast</c>, or <c>Default</c>; the matching table is selected automatically on
    /// every theme change and any <c>DynamicResource</c> (or <c>ThemeResource</c>) reference
    /// into it re-resolves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Selection picks the table whose <see cref="ThemeResourceDictionary.ThemeKey"/> matches
    /// the resolved theme reported by <see cref="ApplicationThemeManager.ResolvedTheme"/> and
    /// falls back to the <c>Default</c> table when the exact theme key is absent. Under high
    /// contrast, the WinUI polarity keys <c>HighContrastBlack</c> (dark schemes) and
    /// <c>HighContrastWhite</c> (light schemes) are tried before the generic
    /// <c>HighContrast</c> key; polarity is judged from the live system window luminance, so
    /// custom schemes classify by how their background reads. Unlike WinUI,
    /// the tables carry their key on the <c>ThemeKey</c> property rather than <c>x:Key</c>: the
    /// WPF markup compiler cannot compile keyed children inside a dictionary-typed property of
    /// a <see cref="ResourceDictionary"/> subclass. In XAML:
    /// </para>
    /// <code language="xaml"><![CDATA[
    /// <Grid.Resources>
    ///     <fluence:ThemeDictionary>
    ///         <fluence:ThemeDictionary.ThemeDictionaries>
    ///             <fluence:ThemeResourceDictionary ThemeKey="Light">
    ///                 <SolidColorBrush x:Key="HeroBrush" Color="#EEEEEE" />
    ///             </fluence:ThemeResourceDictionary>
    ///             <fluence:ThemeResourceDictionary ThemeKey="Dark">
    ///                 <SolidColorBrush x:Key="HeroBrush" Color="#333333" />
    ///             </fluence:ThemeResourceDictionary>
    ///         </fluence:ThemeDictionary.ThemeDictionaries>
    ///     </fluence:ThemeDictionary>
    /// </Grid.Resources>
    /// ]]></code>
    /// <para>
    /// The type is equally usable from code, which suits values only known at runtime:
    /// </para>
    /// <code><![CDATA[
    /// ThemeDictionary icons = new()
    /// {
    ///     ThemeDictionaries =
    ///     {
    ///         new ThemeResourceDictionary { ThemeKey = "Light", ["AppIconImageSource"] = lightIcon },
    ///         new ThemeResourceDictionary { ThemeKey = "Dark", ["AppIconImageSource"] = darkIcon },
    ///     },
    /// };
    /// window.Resources.MergedDictionaries.Add(icons);
    /// ]]></code>
    /// <para>
    /// Use it in element, window, or application resources. It is not a replacement for the
    /// three application-level merged dictionaries owned by
    /// <see cref="ApplicationThemeManager"/>, and its own <see cref="ResourceDictionary.MergedDictionaries"/>
    /// collection is owned by the selection mechanism: entries added there by callers are
    /// discarded on the next swap. In a scope WPF seals read-only (<c>Style.Resources</c>,
    /// template resources) the dictionary keeps the selection made before sealing instead of
    /// swapping. As in WinUI, a <c>StaticResource</c> reference into a theme dictionary does
    /// not update after a theme change; reference its keys with <c>DynamicResource</c> or
    /// <see cref="ThemeResourceExtension"/>.
    /// </para>
    /// <para>
    /// Instances are tracked with weak references, so a discarded <see cref="ThemeDictionary"/>
    /// never leaks through the static theme-change subscription.
    /// </para>
    /// </remarks>
    public sealed class ThemeDictionary : ResourceDictionary
    {
        private const string DefaultKey = "Default";
        private const string HighContrastBlackKey = "HighContrastBlack";
        private const string HighContrastWhiteKey = "HighContrastWhite";

        private static readonly List<WeakReference<ThemeDictionary>> Instances = [];
        private static bool _subscribed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeDictionary"/> class.
        /// </summary>
        public ThemeDictionary()
        {
            ThemeDictionaries = new ThemeResourceDictionaryCollection(this);
            Register(this);
        }

        /// <summary>
        /// Gets the per-theme tables. Each table's <see cref="ThemeResourceDictionary.ThemeKey"/>
        /// names the theme it serves: <c>Light</c>, <c>Dark</c>, <c>HighContrast</c>, or
        /// <c>Default</c> (the fallback used when the resolved theme has no exact entry).
        /// </summary>
        public ThemeResourceDictionaryCollection ThemeDictionaries { get; }

        /// <summary>
        /// Re-evaluates the selected table after the theme table collection was mutated.
        /// </summary>
        internal void OnThemeDictionariesChanged()
        {
            ApplyResolvedTheme(ApplicationThemeManager.ResolvedTheme);
        }

        /// <summary>
        /// Swaps the merged table to the one matching the given resolved theme, falling back to
        /// the <c>Default</c> table.
        /// </summary>
        /// <param name="resolvedTheme">The concrete theme to select a table for.</param>
        private void ApplyResolvedTheme(ApplicationTheme resolvedTheme)
        {
            if (IsReadOnly)
            {
                // WPF seals resource dictionaries hosted in Style.Resources or template
                // resources; mutating MergedDictionaries would throw from the static theme
                // handler. Such scopes keep the selection made before sealing.
                return;
            }

            ResourceDictionary? selected = SelectTable(resolvedTheme);
            Collection<ResourceDictionary> merged = MergedDictionaries;
            if (merged.Count == (selected is null ? 0 : 1) && (selected is null || ReferenceEquals(merged[0], selected)))
            {
                return;
            }

            merged.Clear();
            if (selected is not null)
            {
                merged.Add(selected);
            }
        }

        /// <summary>
        /// Picks the table for the given resolved theme. High contrast first tries the
        /// polarity-specific key (<c>HighContrastBlack</c> for dark schemes,
        /// <c>HighContrastWhite</c> for light schemes, judged by the live system window
        /// luminance), then the generic <c>HighContrast</c> key; every theme falls back to
        /// <c>Default</c>, mirroring the WinUI lookup order.
        /// </summary>
        /// <param name="resolvedTheme">The concrete theme to select a table for.</param>
        private ResourceDictionary? SelectTable(ApplicationTheme resolvedTheme)
        {
            if (resolvedTheme is ApplicationTheme.HighContrast)
            {
                ResourceDictionary? polarity = FindTable(
                    IsHighContrastSchemeDark() ? HighContrastBlackKey : HighContrastWhiteKey);
                if (polarity is not null)
                {
                    return polarity;
                }
            }

            return FindTable(resolvedTheme.ToString()) ?? FindTable(DefaultKey);
        }

        /// <summary>
        /// Judges the active high-contrast scheme's polarity from the live system window
        /// color: Aquatic-style white-on-black schemes are dark, Desert-style black-on-white
        /// schemes are light. Custom schemes classify by the same luminance threshold.
        /// </summary>
        private static bool IsHighContrastSchemeDark()
        {
            System.Windows.Media.Color window = SystemColors.WindowColor;
            double luminance = (0.299 * window.R) + (0.587 * window.G) + (0.114 * window.B);
            return luminance < 128.0;
        }

        /// <summary>
        /// Finds the table registered under the given theme key, if any.
        /// </summary>
        /// <param name="themeKey">The theme name to look up.</param>
        private ResourceDictionary? FindTable(string themeKey)
        {
            foreach (ThemeResourceDictionary table in ThemeDictionaries)
            {
                if (string.Equals(table.ThemeKey, themeKey, StringComparison.Ordinal))
                {
                    return table;
                }
            }

            return null;
        }

        /// <summary>
        /// Tracks the instance weakly and lazily hooks the single static theme subscription.
        /// </summary>
        /// <param name="dictionary">The instance to track.</param>
        private static void Register(ThemeDictionary dictionary)
        {
            lock (Instances)
            {
                if (!_subscribed)
                {
                    ApplicationThemeManager.Changed += OnThemeChanged;
                    _subscribed = true;
                }

                Instances.Add(new WeakReference<ThemeDictionary>(dictionary));
            }
        }

        /// <summary>
        /// Re-applies the selection on every live instance and prunes collected ones.
        /// </summary>
        /// <param name="sender">Unused event sender.</param>
        /// <param name="e">The theme-change payload carrying the resolved theme.</param>
        private static void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            List<ThemeDictionary> alive = [];
            lock (Instances)
            {
                for (int i = Instances.Count - 1; i >= 0; i--)
                {
                    if (Instances[i].TryGetTarget(out ThemeDictionary? dictionary))
                    {
                        alive.Add(dictionary);
                    }
                    else
                    {
                        Instances.RemoveAt(i);
                    }
                }
            }

            foreach (ThemeDictionary dictionary in alive)
            {
                dictionary.ApplyResolvedTheme(e.Theme);
            }
        }
    }
}
