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
using Fluence.Wpf.Theming;
using System;
using System.Windows.Media;

namespace Fluence.Wpf
{
    /// <summary>
    /// Manages system and custom accent colors and publishes them as <c>DynamicResource</c> brush keys aligned with Windows 11.
    /// </summary>
    /// <remarks>
    /// Call <see cref="ApplySystemAccent"/>, <see cref="ApplyApplicationAccent"/>, or <see cref="ApplyCustomAccent"/> after
    /// <see cref="ApplicationThemeManager.Apply"/> so theme-dependent primary/secondary/tertiary accents resolve correctly.
    /// </remarks>
    /// <example>
    /// <code>
    /// ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica, updateAccent: true);
    /// ApplicationAccentColorManager.ApplySystemAccent();
    /// </code>
    /// </example>
    public static class ApplicationAccentColorManager
    {
        /// <summary>
        /// Occurs after accent ramp colors and application resources have been updated.
        /// </summary>
        public static event EventHandler<EventArgs>? AccentColorChanged;

        /// <summary>
        /// Initializes static members of the ApplicationAccentColorManager class and subscribes once
        /// to the theme engine so <see cref="AccentColorChanged"/> is raised after every publish.
        /// </summary>
        /// <remarks>This static constructor is called automatically before any static members are
        /// accessed or any instances are created.</remarks>
        static ApplicationAccentColorManager()
        {
            FluenceThemeEngine.Published += static (_, _) => AccentColorChanged?.Invoke(sender: null, EventArgs.Empty);
        }

        /// <summary>
        /// Forces the static constructor to run, wiring the <see cref="AccentColorChanged"/> subscription
        /// before the first <see cref="FluenceThemeEngine.Apply"/> call. Called by
        /// <see cref="ApplicationThemeManager.Apply"/> before the engine fires its first publish so that
        /// the initial <see cref="AccentColorChanged"/> event is never missed.
        /// </summary>
        internal static void EnsureInitialized()
        {
            // Intentionally empty: the static constructor runs as a side effect of the first
            // reference to any member of this class. This method is the lightweight trigger.
        }

        private static AccentPalette Palette => FluenceThemeEngine.CurrentPalette;

        private static bool IsDark => FluenceThemeEngine.ResolvedTheme is ApplicationTheme.Dark;

        /// <summary>
        /// Gets the current base system accent color (ARGB). Default is a Windows blue until <see cref="ApplySystemAccent"/> runs.
        /// </summary>
        public static Color SystemAccentColor => Palette.Accent;

        /// <summary>
        /// Gets the first light tint on the generated accent ramp. Default matches <see cref="SystemAccentColor"/> until the ramp is loaded.
        /// </summary>
        public static Color SystemAccentColorLight1 => Palette.Light1;

        /// <summary>
        /// Gets the second light tint on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight2 => Palette.Light2;

        /// <summary>
        /// Gets the lightest tint on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight3 => Palette.Light3;

        /// <summary>
        /// Gets the first dark shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark1 => Palette.Dark1;

        /// <summary>
        /// Gets the second dark shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark2 => Palette.Dark2;

        /// <summary>
        /// Gets the darkest shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark3 => Palette.Dark3;

        /// <summary>
        /// Gets the primary accent color used for emphasis surfaces.
        /// </summary>
        public static Color SystemAccentColorPrimary => IsDark ? Palette.Light2 : Palette.Dark1;

        /// <summary>
        /// Gets the secondary accent color used for layered emphasis.
        /// </summary>
        public static Color SystemAccentColorSecondary => IsDark ? Palette.Light1 : Palette.Dark2;

        /// <summary>
        /// Gets the tertiary accent color used for subtle accent fills.
        /// </summary>
        public static Color SystemAccentColorTertiary => IsDark ? Palette.Accent : Palette.Dark3;

        /// <summary>
        /// Gets a value indicating whether Windows is configured to show accent color on title bars and window borders.
        /// </summary>
        public static bool IsAccentColorOnTitleBarsEnabled => RegistryHelper.GetColorPrevalence();

        /// <summary>
        /// Gets the active titlebar color (from DWM AccentColor or default gray).
        /// </summary>
        public static Color TitleBarActiveColor => ResolveTitleBarColors().active;

        /// <summary>
        /// Gets the inactive titlebar color (from DWM AccentColorInactive or default gray).
        /// </summary>
        public static Color TitleBarInactiveColor => ResolveTitleBarColors().inactive;

        /// <summary>
        /// Gets the window border color (titlebar active on Win11, blended on Win10).
        /// </summary>
        public static Color WindowBorderColor => ResolveTitleBarColors().border;

        /// <summary>
        /// Sets the accent intent to the live Windows accent palette and re-applies the current theme.
        /// </summary>
        public static void ApplySystemAccent()
        {
            FluenceThemeEngine.SetAccentIntent(AccentIntent.System);
            FluenceThemeEngine.Apply(ApplicationThemeManager.CurrentTheme);
        }

        /// <summary>
        /// Applies the default application accent (Windows blue) and regenerates the accent ramp.
        /// </summary>
        public static void ApplyApplicationAccent()
        {
            ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
        }

        /// <summary>
        /// Applies a custom base accent color and regenerates the accent ramp and theme resources.
        /// </summary>
        /// <param name="color">The accent color to use as the ramp base.</param>
        public static void ApplyCustomAccent(Color color)
        {
            FluenceThemeEngine.SetAccentIntent(AccentIntent.FromCustom(color));
            FluenceThemeEngine.Apply(ApplicationThemeManager.CurrentTheme);
        }

        internal static void RefreshAccent()
        {
            FluenceThemeEngine.Apply(ApplicationThemeManager.CurrentTheme);
        }

        internal static void ResetForTesting()
        {
            FluenceThemeEngine.ResetForTesting();
        }

        // Reads the title-bar colors already computed by ColorMap during the last Apply so that
        // the same calculation lives in exactly one place (ColorMap.Build).
        private static (Color active, Color inactive, Color border) ResolveTitleBarColors()
        {
            return FluenceThemeEngine.CurrentTitleBarColors;
        }
    }
}
