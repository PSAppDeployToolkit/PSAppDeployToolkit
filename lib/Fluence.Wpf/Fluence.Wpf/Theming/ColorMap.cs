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
using System.Collections.Generic;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Builds a complete color map for a given theme and accent palette by merging
    /// base theme colors with all accent-derived values. This is the single place where
    /// every accent-derived Color token is computed; the engine then overlays these
    /// values (and their brush twins) on top of the authored base dictionaries.
    /// </summary>
    internal static class ColorMap
    {
        /// <summary>
        /// Builds a <see cref="Dictionary{TKey,TValue}"/> mapping every canonical Color key
        /// to its resolved <see cref="Color"/> for the given <paramref name="theme"/> and
        /// <paramref name="p">accent palette</paramref>.
        /// </summary>
        /// <param name="theme">The resolved concrete theme.</param>
        /// <param name="p">The resolved accent ramp.</param>
        /// <param name="deterministicChrome">
        /// When <see langword="true"/>, the title-bar/window-border tokens are set to their
        /// machine-independent theme defaults (the no-color-prevalence values) and no registry,
        /// DWM, or OS-version probe is performed. Used by
        /// <see cref="FluenceThemeEngine.BuildStandalone"/> so the design-time snapshot is byte
        /// stable across machines. The live pipeline calls with the default (<see langword="false"/>),
        /// preserving the registry-driven chrome behavior.
        /// </param>
        internal static Dictionary<string, Color> Build(ApplicationTheme theme, AccentPalette p, bool deterministicChrome = false)
        {
            Dictionary<string, Color> m = BaseColorTables.Load(theme);
            bool dark = theme is ApplicationTheme.Dark;

            // Raw ramp (theme-independent keys)
            m["SystemAccentColor"] = p.Accent;
            m["SystemAccentColorLight1"] = p.Light1;
            m["SystemAccentColorLight2"] = p.Light2;
            m["SystemAccentColorLight3"] = p.Light3;
            m["SystemAccentColorDark1"] = p.Dark1;
            m["SystemAccentColorDark2"] = p.Dark2;
            m["SystemAccentColorDark3"] = p.Dark3;

            // Theme-adaptive primary/secondary/tertiary (from UpdateThemeAdaptiveColors)
            m["SystemAccentColorPrimary"] = dark ? p.Light2 : p.Dark1;
            m["SystemAccentColorSecondary"] = dark ? p.Light1 : p.Dark2;
            m["SystemAccentColorTertiary"] = dark ? p.Accent : p.Dark3;

            // Accent fill (from UpdateResources isDark branch). Secondary/Tertiary carry alpha.
            Color fill = dark ? p.Light2 : p.Dark1;
            m["AccentFillColorDefault"] = fill;
            m["AccentFillColorSecondary"] = HsvColorHelper.WithAlpha(fill, 0xE6);
            m["AccentFillColorTertiary"] = HsvColorHelper.WithAlpha(fill, 0xCC);

            // Accent text (from UpdateAccentTextBrushes)
            m["AccentTextFillColorPrimary"] = dark ? p.Light3 : p.Dark2;
            m["AccentTextFillColorSecondary"] = dark ? p.Light3 : p.Dark3;
            m["AccentTextFillColorTertiary"] = dark ? p.Light2 : p.Dark1;
            m["AccentTextFillColorDisabled"] = dark ? Color.FromArgb(0x5D, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x5C, 0, 0, 0);

            // Text-on-accent (from UpdateTextOnAccentColors; gate on SystemAccentColorPrimary)
            bool whiteOnAccent = HsvColorHelper.ShouldUseWhiteText(m["SystemAccentColorPrimary"]);
            m["TextOnAccentFillColorPrimary"] = whiteOnAccent ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xFF, 0, 0, 0);
            m["TextOnAccentFillColorSecondary"] = whiteOnAccent ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x80, 0, 0, 0);
            m["TextOnAccentFillColorDisabled"] = dark ? Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            // TextOnAccentFillColorSelectedText is theme-independent (always white)
            m["TextOnAccentFillColorSelectedText"] = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            // AccentFillColorSelectedTextBackground equals the base accent color
            m["AccentFillColorSelectedTextBackground"] = p.Accent;

            // Disabled accent + attention (from UpdateDisabledAccentFill / UpdateSystemAttentionFill), skip for HC
            if (theme is not ApplicationTheme.HighContrast)
            {
                m["AccentFillColorDisabled"] = dark ? Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x37, 0, 0, 0);
                m["SystemFillColorAttention"] = dark ? p.Light2 : p.Accent;
            }

            // Title-bar colors (from UpdateTitleBarColors)
            Color titleBarActive;
            Color titleBarInactive;
            if (deterministicChrome)
            {
                // Machine-independent defaults (the no-color-prevalence, Windows-11 branch values):
                // no registry, DWM, or OS-version probe. Keeps the design-time snapshot byte stable.
                titleBarActive = dark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xFF, 0xFF, 0xFF);
                m["TitleBarActiveColor"] = titleBarActive;
                m["TitleBarInactiveColor"] = titleBarActive;
                m["WindowBorderColor"] = titleBarActive;
                return m;
            }

            if (RegistryHelper.GetColorPrevalence())
            {
                titleBarActive = !RegistryHelper.TryGetDwmAccentColor(out Color dwmAccent)
                    ? p.Accent
                    : dwmAccent;
                titleBarInactive = !RegistryHelper.TryGetDwmAccentColorInactive(out Color inactive)
                    ? dark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xFF, 0xFF, 0xFF)
                    : inactive;
            }
            else
            {
                titleBarActive = dark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xFF, 0xFF, 0xFF);
                titleBarInactive = dark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xFF, 0xFF, 0xFF);
            }

            // Use the RtlGetVersion-based OsVersionHelper (not Environment.OSVersion, which is
            // shimmed/version-capped for apps without a supportedOS manifest entry and would
            // mis-detect Windows 11 as pre-22000) to match the rest of the library.
            Color windowBorder = !OsVersionHelper.IsWindows11
                && RegistryHelper.TryGetColorizationBalance(out Color colorizationColor, out int balance)
                ? HsvColorHelper.BlendColors(colorizationColor, Color.FromRgb(0xD9, 0xD9, 0xD9), balance)
                : titleBarActive;

            m["TitleBarActiveColor"] = titleBarActive;
            m["TitleBarInactiveColor"] = titleBarInactive;
            m["WindowBorderColor"] = windowBorder;

            return m;
        }
    }
}
