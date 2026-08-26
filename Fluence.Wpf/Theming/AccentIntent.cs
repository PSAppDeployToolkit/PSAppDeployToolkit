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
        private AccentIntent(bool isSystem, Color customLight, Color customDark)
        {
            IsSystem = isSystem;
            Custom = customLight;
            CustomDark = customDark;
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
        /// Gets an <see cref="AccentIntent"/> that requests the OS accent palette.
        /// </summary>
        public static AccentIntent System { get; } = new(isSystem: true, default, default);

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
        /// Returns an <see cref="AccentIntent"/> that pins the ramp to the given color on every theme.
        /// </summary>
        /// <param name="c">The color to use as the base accent color for the generated ramp.</param>
        public static AccentIntent FromCustom(Color c)
        {
            return new(isSystem: false, c, c);
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
            return new(isSystem: false, light, dark);
        }
    }
}
