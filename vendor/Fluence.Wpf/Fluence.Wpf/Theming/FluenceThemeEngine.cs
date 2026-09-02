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
using System.Windows.Media;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// The single-pipeline theme engine that resolves theme and accent intent into a computed
    /// <see cref="ResourceDictionary"/> and publishes it into application resources. The public
    /// facades (<c language="csharp">ApplicationThemeManager</c>, <c language="csharp">ApplicationAccentColorManager</c>) are thin
    /// wrappers that delegate to this engine: their <c language="csharp">Apply</c>/<c language="csharp">ApplySystemAccent</c> entry
    /// points call <see cref="Apply"/>, and they raise their own public events by subscribing to
    /// <see cref="Published"/>.
    /// </summary>
    internal static class FluenceThemeEngine
    {
        private const string PackBase = "pack://application:,,,/Fluence.Wpf;component/";

        /// <summary>
        /// Key stamped into every dictionary published at slot [0], so a previously published one
        /// can be recognised and removed when the slots are seeded again. Typography and Generic are
        /// identified by their pack URI; a computed dictionary is built in code and has no
        /// <see cref="ResourceDictionary.Source"/>, so it needs a marker of its own.
        /// </summary>
        internal const string ComputedDictionaryMarker = "FluenceComputedDictionary";
        private static AccentIntent _intent = AccentIntent.System;
        private static bool _initialized;

        // Test-only: when set, Apply uses ColorMap's machine-independent (deterministic) chrome
        // branch so the golden-parity test does not depend on the host machine's "show accent color
        // on title bars" personalization setting. See ThemeParityTests.CaptureResolved.
        private static bool _deterministicChromeForTesting;

        // Redundant-publish gate. _publishedFingerprint describes the output of the last successful
        // Publish and _publishedDictionary is the instance that reached slot [0]; both are set only
        // together, and only after Publish reported success. See Apply.
        private static PublishFingerprint? _publishedFingerprint;
        private static ResourceDictionary? _publishedDictionary;

        /// <summary>
        /// Gets the most recently resolved <see cref="AccentPalette"/>.
        /// </summary>
        internal static AccentPalette CurrentPalette { get; private set; }

        /// <summary>
        /// Gets the most recently resolved concrete theme (Light, Dark, or HighContrast).
        /// </summary>
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
        /// <remarks>
        /// The build is gated on a <see cref="PublishFingerprint"/> of everything that determines the
        /// published output. Windows emits several theme-relevant broadcasts (ImmersiveColorSet,
        /// WM_THEMECHANGED, WM_DWMCOLORIZATIONCOLORCHANGED) for a single user action, and the
        /// <c language="csharp">SystemThemeWatcher</c> debounce does not collapse all of them, so an ungated pipeline
        /// would republish slot [0] repeatedly and force every <c language="xaml">DynamicResource</c> consumer in the
        /// tree to re-resolve for no visible change. When the fingerprint equals the last one that
        /// was actually published, and that dictionary is still installed at slot [0], the call
        /// returns without touching <see cref="BrushFactory"/>, <see cref="Publish"/>, or
        /// <see cref="Published"/>. The engine state properties are still assigned, but they are
        /// identical by construction: the resolved theme and every rung of the accent ramp are part
        /// of the fingerprint. The fingerprint also carries the transparency-effects setting, which
        /// changes no computed color but does change what a window's backdrop should be, so the
        /// gate lets that toggle through to <see cref="Published"/> instead of swallowing it.
        /// </remarks>
        /// <param name="request">The requested application theme.</param>
        /// <returns><see langword="true"/> when a dictionary was rebuilt and published (and
        /// <see cref="Published"/> raised); <see langword="false"/> when the call was gated out as
        /// redundant or when <see cref="Publish"/> found no <see cref="Application.Current"/>.</returns>
        internal static bool Apply(ApplicationTheme request)
        {
            ApplicationTheme theme = ThemeResolver.Resolve(request);
            AccentPalette palette = AccentResolver.Resolve(_intent, theme);
            Dictionary<string, Color> colors = ColorMap.Build(theme, palette, deterministicChrome: _deterministicChromeForTesting);

            ResolvedTheme = theme;
            CurrentPalette = palette;
            CurrentTitleBarColors = (colors["TitleBarActiveColor"], colors["TitleBarInactiveColor"], colors["WindowBorderColor"]);

            PublishFingerprint fingerprint = PublishFingerprint.Capture(theme, colors);
            if (_publishedFingerprint is not null && IsPublishedDictionaryInstalled() && _publishedFingerprint.Matches(fingerprint))
            {
                return false;
            }

            ResourceDictionary dict = BuildComputedDictionary(colors, theme);
            if (!Publish(dict))
            {
                // Application.Current was null. Leave the stored fingerprint alone so the next call
                // retries the publish instead of believing this dictionary reached slot [0].
                return false;
            }

            _publishedFingerprint = fingerprint;
            _publishedDictionary = dict;
            Published?.Invoke(sender: null, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> when the dictionary from the last successful publish is
        /// still the live slot [0]. Anything that clears or replaces application resources behind
        /// the engine's back (a test fixture, or a consumer rebuilding
        /// <see cref="Application.Current"/>.Resources) must force a republish even when the
        /// fingerprint is unchanged, because the computed dictionary is no longer installed.
        /// </summary>
        private static bool IsPublishedDictionaryInstalled()
        {
            if (_publishedDictionary is null || Application.Current is null)
            {
                return false;
            }

            Collection<ResourceDictionary> dicts = Application.Current.Resources.MergedDictionaries;
            return dicts.Count > 0 && ReferenceEquals(dicts[0], _publishedDictionary);
        }

        /// <summary>
        /// Builds the single computed dictionary published at slot [0] entirely in C#. The
        /// <paramref name="colors"/> map arrives from <see cref="ColorMap.Build"/>, which reads the
        /// base Color tokens (Color entries only) from the per-theme XAML via
        /// <see cref="BaseColorTables"/> and computes every accent-derived Color;
        /// <see cref="BrushFactory"/> produces the solid brush twin for
        /// every Color token; and <see cref="SpecialBrushes"/> adds the non-twin brushes
        /// (elevation gradients, High-Contrast SystemColors brushes, ScrollBar track, accent
        /// overrides) plus the shared layout/shadow/focus tokens. No brush XAML is merged.
        /// </summary>
        /// <param name="colors">The computed color map built by <see cref="ColorMap.Build"/>.</param>
        /// <param name="theme">The application theme to use.</param>
        private static ResourceDictionary BuildComputedDictionary(Dictionary<string, Color> colors, ApplicationTheme theme)
        {
            ResourceDictionary computed = BrushFactory.Build(colors);
            SpecialBrushes.Add(computed, colors, theme);
            computed["AcrylicNoiseBrush"] = AcrylicNoiseHelper.GetNoiseBrush(); // preserve existing token
            computed[ComputedDictionaryMarker] = true;
            return computed;
        }

        /// <summary>
        /// Builds the computed color + brush <see cref="ResourceDictionary"/> for
        /// <paramref name="theme"/> using the default Windows accent (<c language="text">#0078D4</c>),
        /// <b>without</b> publishing into application resources and <b>without</b> reading
        /// <see cref="Application.Current"/>, the registry, or DWM. The default accent is forced
        /// through <see cref="AccentResolver.Resolve"/> with an
        /// <see cref="AccentIntent.FromCustom(Color)"/> intent (the custom path runs the HSV ramp
        /// generator directly and never touches the registry or <c language="csharp">DwmGetColorizationParameters</c>),
        /// and the title-bar/window-border tokens use their machine-independent theme defaults
        /// (<c language="csharp">deterministicChrome</c>). The result is therefore deterministic and headless-safe,
        /// suitable for serializing a static design-time snapshot.
        /// </summary>
        /// <remarks>
        /// Runs the same <see cref="ColorMap.Build"/> -> <see cref="BrushFactory.Build"/> ->
        /// <see cref="SpecialBrushes.Add"/> sequence as the live pipeline so the snapshot stays
        /// faithful to runtime. It deliberately omits <c language="xaml">AcrylicNoiseBrush</c> (a runtime-generated
        /// <see cref="ImageBrush"/>), which the live
        /// <see cref="BuildComputedDictionary"/> appends after the fact. Only
        /// <see cref="ApplicationTheme.Light"/> and <see cref="ApplicationTheme.Dark"/> are
        /// supported; high contrast is out of scope for design-time previews.
        /// </remarks>
        /// <param name="theme">The application theme to use.</param>
        internal static ResourceDictionary BuildStandalone(ApplicationTheme theme)
        {
            AccentPalette palette = AccentResolver.Resolve(AccentIntent.FromCustom(Color.FromRgb(0x00, 0x78, 0xD4)), theme);
            Dictionary<string, Color> colors = ColorMap.Build(theme, palette, deterministicChrome: true);
            ResourceDictionary computed = BrushFactory.Build(colors);
            SpecialBrushes.Add(computed, colors, theme);
            return computed;
        }

        /// <summary>
        /// Publishes <paramref name="computed"/> into application resources.
        /// </summary>
        /// <param name="computed">The computed dictionary to publish.</param>
        /// <returns><see langword="true"/> if the dictionary was actually published; <see langword="false"/>
        /// if <see cref="Application.Current"/> was null and the call was a no-op.</returns>
        private static bool Publish(ResourceDictionary computed)
        {
            if (Application.Current is null) { return false; }
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

            return true;
        }

        private static ResourceDictionary Load(string rel)
        {
            return new() { Source = new Uri(PackBase + rel, UriKind.Absolute) };
        }

        private static void RemoveFluenceDictionaries(Collection<ResourceDictionary> dicts)
        {
            for (int i = dicts.Count - 1; i >= 0; i--)
            {
                // A computed dictionary is built in code and has no Source, so it cannot be
                // recognised by URI the way Typography and Generic can. It carries a marker key
                // instead. Leaving a previously published one behind is not cosmetic: WPF resolves
                // merged dictionaries last-wins, so a stale computed dictionary sitting past the
                // freshly inserted slot [0] shadows every token the new one publishes.
                if (dicts[i].Contains(ComputedDictionaryMarker))
                {
                    dicts.RemoveAt(i);
                    continue;
                }

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

        /// <summary>
        /// Resets engine state for test isolation.
        /// </summary>
        internal static void ResetForTesting()
        {
            _initialized = false;
            _deterministicChromeForTesting = false;
            _publishedFingerprint = null;
            _publishedDictionary = null;
            _intent = AccentIntent.System;
            ResolvedTheme = ApplicationTheme.Light;
            // Seed a valid default-blue ramp rather than the zero Color value. SystemAccentColor
            // (and FluenceWindow's DWM border, which reads it on activate/deactivate) may be
            // observed between a reset and the next Apply; a default(AccentPalette) would surface
            // as #00000000, painting a transparent/black border. FromCustom avoids any registry read.
            CurrentPalette = AccentResolver.Resolve(AccentIntent.FromCustom(Color.FromRgb(0x00, 0x78, 0xD4)), ApplicationTheme.Light);
            CurrentTitleBarColors = default;
        }
    }
}
