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
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Theming;

namespace Fluence.Wpf
{
    /// <summary>
    /// Manages system and custom accent colors and publishes them as <c language="xaml">DynamicResource</c> brush keys aligned with Windows 11.
    /// </summary>
    /// <remarks>
    /// <see cref="ApplicationThemeManager.Apply"/> uses the Windows accent palette by default.
    /// Call <see cref="ApplyCustomAccent(Color)"/> to pin a custom ramp seed,
    /// <see cref="ApplyCustomAccent(Color, bool)"/> with <c language="csharp">exact: true</c> for an exact visible light-theme primary, or
    /// <see cref="ApplySystemAccent"/> to return to the Windows palette.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// ApplicationThemeManager.Apply(ApplicationTheme.Auto, WindowBackdropType.Mica);
    /// ApplicationAccentColorManager.ApplyCustomAccent(System.Windows.Media.Colors.CornflowerBlue);
    /// </code>
    /// </example>
    public static class ApplicationAccentColorManager
    {
        /// <summary>
        /// Occurs after the theme engine publishes application resources, whether the publish
        /// follows a theme apply or an accent apply. A redundant apply raises no event.
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
        /// Gets the current base accent color (ARGB). A theme apply loads the Windows accent
        /// palette by default; before the first apply, this returns the fallback blue.
        /// </summary>
        public static Color SystemAccentColor => Palette.Accent;

        /// <summary>
        /// Gets the first light tint on the resolved accent ramp. Default matches <see cref="SystemAccentColor"/> until the ramp is loaded.
        /// </summary>
        public static Color SystemAccentColorLight1 => Palette.Light1;

        /// <summary>
        /// Gets the second light tint on the resolved accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight2 => Palette.Light2;

        /// <summary>
        /// Gets the lightest tint on the resolved accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight3 => Palette.Light3;

        /// <summary>
        /// Gets the first dark shade on the resolved accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark1 => Palette.Dark1;

        /// <summary>
        /// Gets the second dark shade on the resolved accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark2 => Palette.Dark2;

        /// <summary>
        /// Gets the darkest shade on the resolved accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark3 => Palette.Dark3;

        /// <summary>
        /// Gets the primary accent color used for emphasis surfaces.
        /// </summary>
        public static Color SystemAccentColorPrimary => Palette.PrimaryOverride ?? (IsDark ? Palette.Light2 : Palette.Dark1);

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
            _ = FluenceThemeEngine.Apply(ApplicationThemeManager.CurrentTheme);
        }

        /// <summary>
        /// Applies a custom accent seed and republishes theme resources. When the seed matches
        /// the active Windows accent, its seven-rung palette is captured; otherwise a ramp is generated.
        /// </summary>
        /// <param name="color">The accent color to use as the ramp base.</param>
        [SuppressMessage("Minor Code Smell", "S3254:Default parameter value should not be passed as an argument", Justification = "The released CLR signature must forward to the new optional-flag overload.")]
        public static void ApplyCustomAccent(Color color)
        {
            ApplyCustomAccent(color, exact: false);
        }

        /// <summary>
        /// Applies a custom accent and republishes theme resources. By default the color is a
        /// ramp seed. With <paramref name="exact"/> enabled, it is the visible light-theme
        /// primary; the dark-theme primary still comes from the resolved ramp's Light2 shade.
        /// </summary>
        /// <param name="color">The accent seed, or the exact light-theme primary.</param>
        /// <param name="exact">Whether to use <paramref name="color"/> as the visible light-theme primary.</param>
        [SuppressMessage("ApiDesign", "RS0027:API with optional parameter should have the most parameters", Justification = "The requested optional flag coexists with the released one- and two-color CLR signatures for binary compatibility.")]
        [SuppressMessage("Major Code Smell", "S3427:Optional parameter value should not overlap overloads", Justification = "The released one-color CLR signature is retained for binary compatibility.")]
        public static void ApplyCustomAccent(Color color, bool exact = false)
        {
            FluenceThemeEngine.SetAccentIntent(exact ? AccentIntent.FromCustomExact(color) : AccentIntent.FromCustom(color));
            _ = FluenceThemeEngine.Apply(ApplicationThemeManager.CurrentTheme);
        }

        /// <summary>
        /// Applies per-theme custom accent seeds and republishes theme resources. A seed matching
        /// the active Windows accent captures its seven-rung palette; other seeds use generated
        /// ramps. The selected palette stays pinned across later theme changes.
        /// </summary>
        /// <param name="lightThemeAccent">The ramp seed used on the light theme.</param>
        /// <param name="darkThemeAccent">The ramp seed used on dark and high-contrast themes.</param>
        [SuppressMessage("Minor Code Smell", "S3254:Default parameter value should not be passed as an argument", Justification = "The released CLR signature must forward to the new optional-flag overload.")]
        public static void ApplyCustomAccent(Color lightThemeAccent, Color darkThemeAccent)
        {
            ApplyCustomAccent(lightThemeAccent, darkThemeAccent, exact: false);
        }

        /// <summary>
        /// Applies per-theme custom accents and republishes theme resources. By default both
        /// colors are ramp seeds. With <paramref name="exact"/> enabled, they are the visible
        /// primary colors in light and dark themes respectively.
        /// </summary>
        /// <param name="lightThemeAccent">The light-theme seed or exact primary.</param>
        /// <param name="darkThemeAccent">The dark-theme seed or exact primary, and the high-contrast ramp seed.</param>
        /// <param name="exact">Whether to use the supplied colors as visible light- and dark-theme primaries.</param>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Both requested optional-flag forms coexist with the released CLR signatures for binary compatibility.")]
        [SuppressMessage("Major Code Smell", "S3427:Optional parameter value should not overlap overloads", Justification = "The released two-color CLR signature is retained for binary compatibility.")]
        public static void ApplyCustomAccent(Color lightThemeAccent, Color darkThemeAccent, bool exact = false)
        {
            FluenceThemeEngine.SetAccentIntent(exact
                ? AccentIntent.FromCustomExact(lightThemeAccent, darkThemeAccent)
                : AccentIntent.FromCustom(lightThemeAccent, darkThemeAccent));
            _ = FluenceThemeEngine.Apply(ApplicationThemeManager.CurrentTheme);
        }

        /// <summary>
        /// Applies an exact visible primary accent on the light theme. The dark-theme primary
        /// is derived from the resolved ramp, and the intent follows subsequent theme changes.
        /// High contrast retains its system state colors and uses this color as its raw ramp seed.
        /// </summary>
        /// <param name="lightThemeAccent">The exact primary accent color on the light theme.</param>
        public static void ApplyCustomAccentExact(Color lightThemeAccent)
        {
            ApplyCustomAccent(lightThemeAccent, exact: true);
        }

        /// <summary>
        /// Applies exact visible primary accents on light and dark themes. The intent follows
        /// subsequent theme changes. High contrast retains its system state colors and uses the
        /// dark-theme color as its raw ramp seed.
        /// </summary>
        /// <param name="lightThemeAccent">The exact primary accent color on the light theme.</param>
        /// <param name="darkThemeAccent">The exact primary accent color on the dark theme.</param>
        public static void ApplyCustomAccentExact(Color lightThemeAccent, Color darkThemeAccent)
        {
            ApplyCustomAccent(lightThemeAccent, darkThemeAccent, exact: true);
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
