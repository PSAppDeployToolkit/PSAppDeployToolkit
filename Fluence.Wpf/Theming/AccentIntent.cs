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

using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Describes the source of the accent color (OS system or caller-supplied custom seeds,
    /// optionally differing between light and dark themes).
    /// </summary>
    internal readonly struct AccentIntent
    {
        private readonly AccentPalette? _capturedLightPalette;
        private readonly AccentPalette? _capturedDarkPalette;

        private AccentIntent(bool isSystem, Color customLight, Color customDark, bool isExact, bool hasExplicitDark,
            AccentPalette? capturedLightPalette, AccentPalette? capturedDarkPalette)
        {
            IsSystem = isSystem;
            Custom = customLight;
            CustomDark = customDark;
            IsExact = isExact;
            HasExplicitDark = hasExplicitDark;
            _capturedLightPalette = capturedLightPalette;
            _capturedDarkPalette = capturedDarkPalette;
        }

        /// <summary>
        /// Gets a value indicating whether the OS system accent should be resolved.
        /// </summary>
        public bool IsSystem { get; }

        /// <summary>
        /// Gets the caller-supplied seed used on the light theme (and every theme when the
        /// intent carries a single seed); valid only when <see cref="IsSystem"/> is <see langword="false"/>.
        /// </summary>
        public Color Custom { get; }

        /// <summary>
        /// Gets the caller-supplied seed used on dark and high-contrast themes; equals
        /// <see cref="Custom"/> when the intent carries a single seed. Valid only when
        /// <see cref="IsSystem"/> is <see langword="false"/>.
        /// </summary>
        public Color CustomDark { get; }

        /// <summary>
        /// Gets a value indicating whether the supplied color is the visible primary accent.
        /// </summary>
        public bool IsExact { get; }

        /// <summary>
        /// Gets a value indicating whether the caller supplied an exact dark-theme color.
        /// </summary>
        public bool HasExplicitDark { get; }

        /// <summary>
        /// Gets an <see cref="AccentIntent"/> that requests the OS accent palette.
        /// </summary>
        public static AccentIntent System { get; } = new(isSystem: true, default, default, isExact: false, hasExplicitDark: false,
            capturedLightPalette: null, capturedDarkPalette: null);

        /// <summary>
        /// Returns the custom seed appropriate for the given resolved theme: the dark seed on
        /// <see cref="ApplicationTheme.Dark"/> and <see cref="ApplicationTheme.HighContrast"/>,
        /// otherwise the light seed.
        /// </summary>
        /// <param name="resolvedTheme">The concrete theme the engine resolved for this apply.</param>
        public Color CustomFor(ApplicationTheme resolvedTheme)
        {
            return resolvedTheme is ApplicationTheme.Dark or ApplicationTheme.HighContrast ? CustomDark : Custom;
        }

        /// <summary>
        /// Returns the OS palette captured when the matching custom seed was applied, if any.
        /// Unlike the system intent, a custom intent does not re-read this palette on theme changes.
        /// </summary>
        /// <param name="resolvedTheme">The concrete theme the engine resolved for this apply.</param>
        public AccentPalette? CapturedPaletteFor(ApplicationTheme resolvedTheme)
        {
            return resolvedTheme is ApplicationTheme.Dark or ApplicationTheme.HighContrast
                ? _capturedDarkPalette
                : _capturedLightPalette;
        }

        /// <summary>
        /// Returns an <see cref="AccentIntent"/> that pins the ramp to the given color on every theme.
        /// </summary>
        /// <param name="c">The base accent color for the selected palette.</param>
        public static AccentIntent FromCustom(Color c)
        {
            return FromCustom(c, AccentResolver.CaptureCurrentSystemPalette());
        }

        /// <summary>
        /// Creates a custom intent against a supplied system-palette snapshot. A matching base
        /// color keeps the captured shades; an unmatched color uses the generated ramp.
        /// </summary>
        /// <param name="c">The caller's custom ramp seed.</param>
        /// <param name="systemPalette">The system palette visible when the intent is created.</param>
        internal static AccentIntent FromCustom(Color c, AccentPalette? systemPalette)
        {
            return CreateCustom(c, c, isExact: false, hasExplicitDark: false, systemPalette);
        }

        /// <summary>
        /// Returns an <see cref="AccentIntent"/> with per-theme seeds: <paramref name="light"/>
        /// drives the ramp on the light theme; <paramref name="dark"/> drives it on dark and
        /// high-contrast themes.
        /// </summary>
        /// <param name="light">The ramp seed for the light theme.</param>
        /// <param name="dark">The ramp seed for dark and high-contrast themes.</param>
        public static AccentIntent FromCustom(Color light, Color dark)
        {
            return CreateCustom(light, dark, isExact: false, hasExplicitDark: true,
                AccentResolver.CaptureCurrentSystemPalette());
        }

        /// <summary>
        /// Creates a deterministic custom intent without reading the current OS palette.
        /// Design-time resources and the pre-apply default use this path.
        /// </summary>
        /// <param name="c">The custom ramp seed.</param>
        internal static AccentIntent FromCustomGenerated(Color c)
        {
            return CreateCustom(c, c, isExact: false, hasExplicitDark: false, systemPalette: null);
        }

        /// <summary>
        /// Returns an intent whose light-theme primary is the supplied color. The dark-theme
        /// primary is derived from the resolved accent ramp.
        /// </summary>
        /// <param name="light">The exact visible primary accent on the light theme.</param>
        public static AccentIntent FromCustomExact(Color light)
        {
            return CreateCustom(light, light, isExact: true, hasExplicitDark: false,
                AccentResolver.CaptureCurrentSystemPalette());
        }

        /// <summary>
        /// Returns an intent with exact visible primary accents for light and dark themes.
        /// </summary>
        /// <param name="light">The exact visible primary accent on the light theme.</param>
        /// <param name="dark">The exact visible primary accent on the dark theme.</param>
        public static AccentIntent FromCustomExact(Color light, Color dark)
        {
            return CreateCustom(light, dark, isExact: true, hasExplicitDark: true,
                AccentResolver.CaptureCurrentSystemPalette());
        }

        private static AccentIntent CreateCustom(Color light, Color dark, bool isExact, bool hasExplicitDark,
            AccentPalette? systemPalette)
        {
            AccentPalette? lightPalette = systemPalette is { } palette && palette.Accent == light ? palette : null;
            AccentPalette? darkPalette = systemPalette is { } darkCandidate && darkCandidate.Accent == dark
                ? darkCandidate
                : null;
            return new(isSystem: false, light, dark, isExact, hasExplicitDark, lightPalette, darkPalette);
        }
    }
}
