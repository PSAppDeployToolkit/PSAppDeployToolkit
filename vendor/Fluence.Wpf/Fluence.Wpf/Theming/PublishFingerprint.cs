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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// An immutable snapshot of every input that determines the computed dictionary
    /// <see cref="FluenceThemeEngine"/> publishes at slot [0]. Two fingerprints that
    /// <see cref="Matches"/> each other therefore describe byte-identical published output, which
    /// lets the engine skip a redundant rebuild and republish (and the events that follow it) when
    /// the OS fires several theme or accent broadcasts for a single user action.
    /// </summary>
    /// <remarks>
    /// The four captured inputs are exhaustive for the pipeline as it stands:
    /// <list type="bullet">
    /// <item><description>The resolved concrete theme, which selects the
    /// <see cref="SpecialBrushes"/> branch (elevation gradients versus high-contrast overrides)
    /// and the light/dark accent split.</description></item>
    /// <item><description>The full computed color map from <see cref="ColorMap.Build"/>. It already
    /// subsumes the accent palette (all seven ramp rungs are keys in the map), the per-theme base
    /// tables, the registry-driven title-bar and window-border chrome, and the deterministic-chrome
    /// test switch, so none of those needs a separate field.</description></item>
    /// <item><description>A snapshot of the live <see cref="SystemColors"/> members that
    /// <see cref="SpecialBrushes"/> reads directly rather than through the color map. Note that
    /// <c>AddSystemColorAliases</c> reads them in <b>every</b> theme, not only high contrast, so the
    /// snapshot is unconditional.</description></item>
    /// <item><description>The Settings "Transparency effects" toggle
    /// (<c>RegistryHelper.GetEnableTransparency</c>). It changes no computed color, so on its own
    /// the map cannot see it, but the Windows 10 legacy-acrylic path in
    /// <c>Fluence.Wpf.Controls.FluenceWindow</c> re-reads the setting from the publish chain: the
    /// toggle broadcasts ImmersiveColorSet, and if the gate swallowed that broadcast no
    /// <c>Changed</c> event would fire and every open window would keep the acrylic the old setting
    /// asked for. Carrying the flag makes the toggle a fingerprint mismatch and so a
    /// republish.</description></item>
    /// </list>
    /// The map is compared by count plus pairwise lookup rather than hashed: at roughly 250 entries
    /// that is cheaper than building a hash and cannot collide.
    /// <para>
    /// One published entry is deliberately outside the fingerprint: <c>AcrylicNoiseBrush</c>, which
    /// <see cref="FluenceThemeEngine"/> appends from <c>AcrylicNoiseHelper.GetNoiseBrush</c>. It is a
    /// deterministic process-wide singleton that depends on neither theme nor accent, so it can never
    /// be the reason two applies differ. If that brush ever becomes theme, accent, or DPI dependent,
    /// whatever it varies on must be added here or the gate will skip a change that should ship.
    /// </para>
    /// </remarks>
    internal sealed class PublishFingerprint
    {
        private readonly ApplicationTheme _theme;
        private readonly IReadOnlyDictionary<string, Color> _colors;
        private readonly Color[] _systemColors;
        private readonly bool _transparencyEnabled;

        private PublishFingerprint(
            ApplicationTheme theme,
            IReadOnlyDictionary<string, Color> colors,
            Color[] systemColors,
            bool transparencyEnabled)
        {
            _theme = theme;
            _colors = colors;
            _systemColors = systemColors;
            _transparencyEnabled = transparencyEnabled;
        }

        /// <summary>
        /// Captures a fingerprint for the given resolved theme and computed color map, reading the
        /// live <see cref="SystemColors"/> members and the transparency-effects setting as part of
        /// the snapshot.
        /// </summary>
        /// <param name="theme">The resolved concrete theme (Light, Dark, or HighContrast).</param>
        /// <param name="colors">
        /// The computed color map. The caller must not mutate it after this call; the engine builds
        /// a fresh map on every <see cref="FluenceThemeEngine.Apply"/>, so the retained reference is
        /// never shared with a later pipeline run.
        /// </param>
        internal static PublishFingerprint Capture(ApplicationTheme theme, IReadOnlyDictionary<string, Color> colors)
        {
            return Capture(theme, colors, RegistryHelper.GetEnableTransparency());
        }

        /// <summary>
        /// Captures a fingerprint with the transparency-effects flag supplied rather than read from
        /// the registry, so the flag's contribution to <see cref="Matches"/> can be exercised
        /// without writing to the user's Personalize key.
        /// </summary>
        /// <param name="theme">The resolved concrete theme (Light, Dark, or HighContrast).</param>
        /// <param name="colors">The computed color map, under the same no-mutation contract as the
        /// two-argument overload.</param>
        /// <param name="transparencyEnabled">The transparency-effects setting to record.</param>
        internal static PublishFingerprint Capture(
            ApplicationTheme theme,
            IReadOnlyDictionary<string, Color> colors,
            bool transparencyEnabled)
        {
            return new PublishFingerprint(theme, colors, CaptureSystemColors(), transparencyEnabled);
        }

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="other"/> would produce an identical
        /// published dictionary to this one.
        /// </summary>
        /// <param name="other">The fingerprint to compare against.</param>
        internal bool Matches(PublishFingerprint other)
        {
            return _theme == other._theme
                && _transparencyEnabled == other._transparencyEnabled
                && SystemColorsEqual(_systemColors, other._systemColors)
                && ColorMapsEqual(_colors, other._colors);
        }

        /// <summary>
        /// Order-independent value comparison of two color maps: equal counts and, for every key in
        /// <paramref name="left"/>, a matching key and <see cref="Color"/> in <paramref name="right"/>.
        /// </summary>
        /// <param name="left">The first color map.</param>
        /// <param name="right">The second color map.</param>
        internal static bool ColorMapsEqual(IReadOnlyDictionary<string, Color> left, IReadOnlyDictionary<string, Color> right)
        {
            return ReferenceEquals(left, right) || (left.Count == right.Count && !left.Any(entry => !right.TryGetValue(entry.Key, out Color candidate) || candidate != entry.Value));
        }

        /// <summary>
        /// Snapshots every live <see cref="SystemColors"/> member that <see cref="SpecialBrushes"/>
        /// reads: the eight <c>AddSystemColorAliases</c> values, which are emitted in every theme,
        /// plus <see cref="SystemColors.ControlDarkColor"/> and
        /// <see cref="SystemColors.ControlLightColor"/>, which only the high-contrast branch reads.
        /// Capturing the union unconditionally keeps the gate a single comparison and costs two
        /// extra property reads.
        /// </summary>
        private static Color[] CaptureSystemColors()
        {
            return
            [
                SystemColors.WindowColor,
                SystemColors.WindowTextColor,
                SystemColors.GrayTextColor,
                SystemColors.HighlightColor,
                SystemColors.HighlightTextColor,
                SystemColors.HotTrackColor,
                SystemColors.ControlColor,
                SystemColors.ControlTextColor,
                SystemColors.ControlDarkColor,
                SystemColors.ControlLightColor,
            ];
        }

        private static bool SystemColorsEqual(Color[] left, Color[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
