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

using Fluence.Wpf.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// The single-pipeline theme engine that resolves theme and accent intent into a computed
    /// <see cref="ResourceDictionary"/> and publishes it into application resources. The public
    /// facades (<c>ApplicationThemeManager</c>, <c>ApplicationAccentColorManager</c>) are thin
    /// wrappers that delegate to this engine: their <c>Apply</c>/<c>ApplySystemAccent</c> entry
    /// points call <see cref="Apply"/>, and they raise their own public events by subscribing to
    /// <see cref="Published"/>.
    /// </summary>
    internal static class FluenceThemeEngine
    {
        private const string PackBase = "pack://application:,,,/Fluence.Wpf;component/";
        private static AccentIntent _intent = AccentIntent.System;
        private static bool _initialized;

        // Test-only: when set, BuildComputedDictionary uses ColorMap's machine-independent
        // (deterministic) chrome branch so the golden-parity test does not depend on the host
        // machine's "show accent color on title bars" personalization setting. See
        // ThemeParityTests.CaptureResolved.
        private static bool _deterministicChromeForTesting;

        /// <summary>Gets the most recently resolved <see cref="AccentPalette"/>.</summary>
        internal static AccentPalette CurrentPalette { get; private set; }

        /// <summary>Gets the most recently resolved concrete theme (Light, Dark, or HighContrast).</summary>
        internal static ApplicationTheme ResolvedTheme { get; private set; } = ApplicationTheme.Light;

        /// <summary>Gets the title-bar colors computed during the most recent <see cref="Apply"/> call.
        /// Populated by <see cref="ColorMap.Build"/> so the computation lives in a single place.</summary>
        internal static (Color active, Color inactive, Color border) CurrentTitleBarColors { get; private set; }

        /// <summary>
        /// Raised after the computed dictionary has been published into application resources.
        /// Facade classes raise their own public events by subscribing here.
        /// </summary>
        internal static event EventHandler<EventArgs>? Published;

        /// <summary>
        /// Sets the accent intent that the next <see cref="Apply"/> call will use.
        /// </summary>
        /// <param name="intent">The accent intent to set.</param>
        internal static void SetAccentIntent(AccentIntent intent)
        {
            _intent = intent;
        }

        /// <summary>
        /// Resolves the theme and accent, builds the computed dictionary, and publishes it into
        /// application resources.
        /// </summary>
        /// <param name="request">The requested application theme.</param>
        internal static void Apply(ApplicationTheme request)
        {
            ApplicationTheme theme = ThemeResolver.Resolve(request);
            AccentPalette palette = AccentResolver.Resolve(_intent);
            ResolvedTheme = theme;
            CurrentPalette = palette;

            ResourceDictionary dict = BuildComputedDictionary(theme, palette);
            Publish(dict);
            Published?.Invoke(sender: null, EventArgs.Empty);
        }

        /// <summary>
        /// Builds the single computed dictionary published at slot [0] entirely in C#. Base
        /// Color tokens are read (Color entries only) from the per-theme and shared XAML via
        /// <see cref="BaseColorTables"/>; accent-derived Colors are computed by
        /// <see cref="ColorMap"/>; <see cref="BrushFactory"/> produces the solid brush twin for
        /// every Color token; and <see cref="SpecialBrushes"/> adds the non-twin brushes
        /// (elevation gradients, High-Contrast SystemColors brushes, ScrollBar track, accent
        /// overrides) plus the shared layout/shadow/focus tokens. No brush XAML is merged.
        /// </summary>
        /// <param name="theme">The application theme to use.</param>
        /// <param name="palette">The accent palette to use.</param>
        private static ResourceDictionary BuildComputedDictionary(ApplicationTheme theme, AccentPalette palette)
        {
            Dictionary<string, Color> colors = ColorMap.Build(theme, palette, deterministicChrome: _deterministicChromeForTesting);
            CurrentTitleBarColors = (colors["TitleBarActiveColor"], colors["TitleBarInactiveColor"], colors["WindowBorderColor"]);
            ResourceDictionary computed = BrushFactory.Build(colors);
            SpecialBrushes.Add(computed, colors, theme);
            computed["AcrylicNoiseBrush"] = AcrylicNoiseHelper.GetNoiseBrush(); // preserve existing token
            return computed;
        }

        /// <summary>
        /// Builds the computed color + brush <see cref="ResourceDictionary"/> for
        /// <paramref name="theme"/> using the default Windows accent (<c>#0078D4</c>),
        /// <b>without</b> publishing into application resources and <b>without</b> reading
        /// <see cref="Application.Current"/>, the registry, or DWM. The default accent is forced
        /// through <see cref="AccentResolver.Resolve"/> with an
        /// <see cref="AccentIntent.FromCustom"/> intent (the custom path runs the HSV ramp
        /// generator directly and never touches the registry or <c>DwmGetColorizationParameters</c>),
        /// and the title-bar/window-border tokens use their machine-independent theme defaults
        /// (<c>deterministicChrome</c>). The result is therefore deterministic and headless-safe,
        /// suitable for serializing a static design-time snapshot.
        /// </summary>
        /// <remarks>
        /// Runs the same <see cref="ColorMap.Build"/> -> <see cref="BrushFactory.Build"/> ->
        /// <see cref="SpecialBrushes.Add"/> sequence as the live pipeline so the snapshot stays
        /// faithful to runtime. It deliberately omits <c>AcrylicNoiseBrush</c> (a runtime-generated
        /// <see cref="ImageBrush"/>), which the live
        /// <see cref="BuildComputedDictionary"/> appends after the fact. Only
        /// <see cref="ApplicationTheme.Light"/> and <see cref="ApplicationTheme.Dark"/> are
        /// supported; high contrast is out of scope for design-time previews.
        /// </remarks>
        /// <param name="theme">The application theme to use.</param>
        internal static ResourceDictionary BuildStandalone(ApplicationTheme theme)
        {
            AccentPalette palette = AccentResolver.Resolve(AccentIntent.FromCustom(Color.FromRgb(0x00, 0x78, 0xD4)));
            Dictionary<string, Color> colors = ColorMap.Build(theme, palette, deterministicChrome: true);
            ResourceDictionary computed = BrushFactory.Build(colors);
            SpecialBrushes.Add(computed, colors, theme);
            return computed;
        }

        private static void Publish(ResourceDictionary computed)
        {
            if (Application.Current is null) { return; }
            Collection<ResourceDictionary> dicts = Application.Current.Resources.MergedDictionaries;
            if (!_initialized)
            {
                // Slot model: [0] computed, [1] Typography, [2] Generic. Insert (not Add) the
                // static slots so that any foreign dictionaries an application merged into
                // Application.Resources (e.g. via App.xaml) are pushed to index 3+ and the
                // [0]/[1]/[2] contract that DynamicResource resolution and DictionaryStabilityTests
                // depend on holds regardless of pre-existing entries.
                RemoveFluenceDictionaries(dicts);
                dicts.Insert(0, computed);
                dicts.Insert(1, Load("Themes/Typography/Typography.xaml"));
                dicts.Insert(2, Load("Themes/Generic.xaml"));
                _initialized = true;
            }
            else
            {
                dicts[0] = computed; // replace -> DynamicResource consumers re-resolve
            }
        }

        private static ResourceDictionary Load(string rel)
        {
            return new() { Source = new Uri(PackBase + rel, UriKind.Absolute) };
        }

        private static void RemoveFluenceDictionaries(Collection<ResourceDictionary> dicts)
        {
            for (int i = dicts.Count - 1; i >= 0; i--)
            {
                string s = dicts[i].Source?.OriginalString.ToLowerInvariant() ?? string.Empty;
                if (s.Contains("fluence.wpf;component", StringComparison.Ordinal) && s.Contains("themes/", StringComparison.Ordinal))
                {
                    dicts.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Test-only switch that forces the live publish pipeline to emit machine-independent
        /// title-bar / window-border chrome (the deterministic <see cref="ColorMap.Build"/> branch).
        /// The golden-parity snapshot was captured with color-prevalence OFF; without this the
        /// rebuild would read live OS personalization (HKCU DWM ColorPrevalence / AccentColor) and
        /// drift on machines that show the accent color on title bars.
        /// </summary>
        /// <param name="enabled">Whether to enable deterministic chrome for testing.</param>
        internal static void SetDeterministicChromeForTesting(bool enabled)
        {
            _deterministicChromeForTesting = enabled;
        }

        /// <summary>Resets engine state for test isolation.</summary>
        internal static void ResetForTesting()
        {
            _initialized = false;
            _deterministicChromeForTesting = false;
            _intent = AccentIntent.System;
            ResolvedTheme = ApplicationTheme.Light;
            // Seed a valid default-blue ramp rather than the zero Color value. SystemAccentColor
            // (and FluenceWindow's DWM border, which reads it on activate/deactivate) may be
            // observed between a reset and the next Apply; a default(AccentPalette) would surface
            // as #00000000, painting a transparent/black border. FromCustom avoids any registry read.
            CurrentPalette = AccentResolver.Resolve(AccentIntent.FromCustom(Color.FromRgb(0x00, 0x78, 0xD4)));
            CurrentTitleBarColors = default;
        }
    }
}
