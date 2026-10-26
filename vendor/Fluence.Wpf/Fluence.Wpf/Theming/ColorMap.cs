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
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Helpers;

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
        private const string AccentFillDefaultKey = "AccentFillColorDefault";
        private const string AccentFillSecondaryKey = "AccentFillColorSecondary";
        private const string AccentFillTertiaryKey = "AccentFillColorTertiary";
        private const string AccentFillDisabledKey = "AccentFillColorDisabled";
        private const string TextOnAccentPrimaryKey = "TextOnAccentFillColorPrimary";
        private const string TextOnAccentSecondaryKey = "TextOnAccentFillColorSecondary";
        private const string TextOnAccentDisabledKey = "TextOnAccentFillColorDisabled";

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
            Color primary = p.PrimaryOverride ?? (dark ? p.Light2 : p.Dark1);
            m["SystemAccentColorPrimary"] = primary;
            m["SystemAccentColorSecondary"] = dark ? p.Light1 : p.Dark2;
            m["SystemAccentColorTertiary"] = dark ? p.Accent : p.Dark3;

            // Accent fill (from UpdateResources isDark branch). Secondary/Tertiary carry alpha.
            Color fill = primary;
            m[AccentFillDefaultKey] = fill;
            m[AccentFillSecondaryKey] = HsvColorHelper.WithAlpha(fill, 0xE6);
            m[AccentFillTertiaryKey] = HsvColorHelper.WithAlpha(fill, 0xCC);

            // Accent text (from UpdateAccentTextBrushes)
            m["AccentTextFillColorPrimary"] = dark ? p.Light3 : p.Dark2;
            m["AccentTextFillColorSecondary"] = dark ? p.Light3 : p.Dark3;
            m["AccentTextFillColorTertiary"] = dark ? p.Light2 : p.Dark1;
            m["AccentTextFillColorDisabled"] = dark ? Color.FromArgb(0x5D, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x5C, 0, 0, 0);

            // Text-on-accent (from UpdateTextOnAccentColors; gate on SystemAccentColorPrimary)
            bool whiteOnAccent = HsvColorHelper.ShouldUseWhiteText(m["SystemAccentColorPrimary"]);
            m[TextOnAccentPrimaryKey] = whiteOnAccent ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xFF, 0, 0, 0);
            m[TextOnAccentSecondaryKey] = whiteOnAccent ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x80, 0, 0, 0);
            m[TextOnAccentDisabledKey] = dark ? Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            // TextOnAccentFillColorSelectedText is normally white. High contrast uses
            // the neutral window-text role from WinUI CommonStyles below.
            m["TextOnAccentFillColorSelectedText"] = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            // AccentFillColorSelectedTextBackground equals the base accent color
            m["AccentFillColorSelectedTextBackground"] = p.Accent;

            if (theme is ApplicationTheme.HighContrast)
            {
                ApplyHighContrastAccentColors(m);
            }
            else
            {
                // Disabled accent + attention (from UpdateDisabledAccentFill / UpdateSystemAttentionFill)
                m[AccentFillDisabledKey] = dark ? Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x37, 0, 0, 0);
                m["SystemFillColorAttention"] = dark ? p.Light2 : p.Accent;

                // Accent acrylic fallbacks. WinUI builds these as AcrylicBrush recipes whose
                // FallbackColor is the accent ramp entry itself (AcrylicBrush_themeresources.xaml:49,
                // :51 for the Default dictionary, :101, :103 for Light): dark takes Dark1 for Default
                // and Dark2 for Base, light takes Light3 for both. A WPF popup surface is not
                // DWM-composited, so the fallback is what this library publishes, exactly as it does
                // for the non-accent AcrylicBackgroundFillColor* pair in the per-theme tables.
                m["AccentAcrylicBackgroundFillColorDefault"] = dark ? p.Dark1 : p.Light3;
                m["AccentAcrylicBackgroundFillColorBase"] = dark ? p.Dark2 : p.Light3;
            }

            AddAccentControlStateColors(m, theme);

            if (theme is ApplicationTheme.HighContrast)
            {
                Color window = SystemColors.WindowColor;
                m["TitleBarActiveColor"] = window;
                m["TitleBarInactiveColor"] = window;
                m["WindowBorderColor"] = SystemColors.WindowTextColor;
                return m;
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

        private static void ApplyHighContrastAccentColors(Dictionary<string, Color> m)
        {
            Color window = SystemColors.WindowColor;
            Color windowText = SystemColors.WindowTextColor;
            Color grayText = SystemColors.GrayTextColor;

            // WinUI Common_themeresources_any.xaml defines neutral HC common brushes.
            // Individual controls choose their own highlight/hover/pressed pair through
            // control-specific state resources rather than changing this shared family.
            m["AccentFillColorSelectedTextBackground"] = window;
            m[AccentFillDefaultKey] = window;
            m[AccentFillSecondaryKey] = window;
            m[AccentFillTertiaryKey] = window;
            m[AccentFillDisabledKey] = window;

            m["AccentTextFillColorPrimary"] = windowText;
            m["AccentTextFillColorSecondary"] = windowText;
            m["AccentTextFillColorTertiary"] = windowText;
            m["AccentTextFillColorDisabled"] = grayText;

            m["TextOnAccentFillColorSelectedText"] = windowText;
            m[TextOnAccentPrimaryKey] = windowText;
            m[TextOnAccentSecondaryKey] = windowText;
            m[TextOnAccentDisabledKey] = grayText;

            m["SystemFillColorAttention"] = windowText;
            m["AccentAcrylicBackgroundFillColorDefault"] = window;
            m["AccentAcrylicBackgroundFillColorBase"] = window;
        }

        private static void AddAccentControlStateColors(Dictionary<string, Color> m, ApplicationTheme theme)
        {
            // WinUI Button_themeresources.xaml gives accent buttons separate high-contrast
            // state resources; the CommonStyles accent family remains neutral in HC.
            if (theme is ApplicationTheme.HighContrast)
            {
                Color highlight = SystemColors.HighlightColor;
                Color highlightText = SystemColors.HighlightTextColor;
                Color window = SystemColors.WindowColor;
                Color windowText = SystemColors.WindowTextColor;
                Color grayText = SystemColors.GrayTextColor;
                Color controlText = SystemColors.ControlTextColor;
                Color control = SystemColors.ControlColor;

                m["AccentButtonBackground"] = highlight;
                m["AccentButtonBackgroundPointerOver"] = highlight;
                m["AccentButtonBackgroundPressed"] = window;
                m["AccentButtonBackgroundDisabled"] = window;
                m["AccentButtonForeground"] = highlightText;
                m["AccentButtonForegroundPointerOver"] = highlightText;
                m["AccentButtonForegroundPressed"] = windowText;
                m["AccentButtonForegroundDisabled"] = grayText;

                // ToggleButton_themeresources.xaml uses ButtonText/ButtonFace for the
                // checked hover and press states rather than the accent button pair.
                m["ToggleButtonBackgroundChecked"] = highlight;
                m["ToggleButtonBackgroundCheckedPointerOver"] = controlText;
                m["ToggleButtonBackgroundCheckedPressed"] = highlightText;
                m["ToggleButtonBackgroundCheckedDisabled"] = window;
                m["ToggleButtonForegroundChecked"] = highlightText;
                m["ToggleButtonForegroundCheckedPointerOver"] = control;
                m["ToggleButtonForegroundCheckedPressed"] = highlight;
                m["ToggleButtonForegroundCheckedDisabled"] = grayText;

                m["CheckBoxCheckBackgroundFillChecked"] = highlight;
                m["CheckBoxCheckBackgroundFillCheckedPointerOver"] = controlText;
                m["CheckBoxCheckBackgroundFillCheckedPressed"] = control;
                m["CheckBoxCheckBackgroundFillCheckedDisabled"] = grayText;
                m["CheckBoxCheckGlyphForegroundChecked"] = highlightText;
                m["CheckBoxCheckGlyphForegroundCheckedPointerOver"] = control;
                m["CheckBoxCheckGlyphForegroundCheckedPressed"] = controlText;
                m["CheckBoxCheckGlyphForegroundCheckedDisabled"] = window;
                m["CheckBoxCheckBackgroundFillIndeterminate"] = highlight;
                m["CheckBoxCheckBackgroundFillIndeterminatePointerOver"] = highlightText;
                m["CheckBoxCheckBackgroundStrokeIndeterminatePointerOver"] = highlight;
                m["CheckBoxCheckBackgroundFillIndeterminatePressed"] = highlight;
                m["CheckBoxCheckBackgroundFillIndeterminateDisabled"] = grayText;
                m["CheckBoxCheckGlyphForegroundIndeterminate"] = highlightText;
                m["CheckBoxCheckGlyphForegroundIndeterminatePointerOver"] = highlight;
                m["CheckBoxCheckGlyphForegroundIndeterminatePressed"] = highlightText;
                m["CheckBoxCheckGlyphForegroundIndeterminateDisabled"] = window;

                m["RadioButtonOuterEllipseCheckedFill"] = highlightText;
                m["RadioButtonOuterEllipseCheckedFillPointerOver"] = control;
                m["RadioButtonOuterEllipseCheckedFillPressed"] = control;
                m["RadioButtonOuterEllipseCheckedFillDisabled"] = window;
                m["RadioButtonOuterEllipseCheckedStroke"] = highlight;
                m["RadioButtonOuterEllipseCheckedStrokePointerOver"] = controlText;
                m["RadioButtonOuterEllipseCheckedStrokePressed"] = control;
                m["RadioButtonOuterEllipseCheckedStrokeDisabled"] = grayText;
                m["RadioButtonCheckGlyphFill"] = highlight;
                m["RadioButtonCheckGlyphFillPointerOver"] = controlText;
                m["RadioButtonCheckGlyphFillPressed"] = controlText;
                m["RadioButtonCheckGlyphFillDisabled"] = grayText;

                m["ToggleSwitchFillOn"] = highlight;
                m["ToggleSwitchFillOnPointerOver"] = control;
                m["ToggleSwitchFillOnPressed"] = control;
                m["ToggleSwitchFillOnDisabled"] = window;
                m["ToggleSwitchKnobFillOn"] = highlightText;
                m["ToggleSwitchKnobFillOnPointerOver"] = controlText;
                m["ToggleSwitchKnobFillOnPressed"] = controlText;
                m["ToggleSwitchKnobFillOnDisabled"] = grayText;

                m["SliderThumbBackground"] = highlight;
                m["SliderThumbBackgroundPointerOver"] = highlight;
                m["SliderThumbBackgroundPressed"] = SystemColors.HotTrackColor;
                m["SliderThumbBackgroundDisabled"] = grayText;
                m["SliderTrackValueFill"] = highlight;
                m["SliderTrackValueFillPointerOver"] = highlight;
                m["SliderTrackValueFillPressed"] = highlight;
                m["SliderTrackValueFillDisabled"] = grayText;

                m["HyperlinkButtonForeground"] = SystemColors.HotTrackColor;
                m["HyperlinkButtonForegroundPointerOver"] = windowText;
                m["HyperlinkButtonForegroundPressed"] = windowText;
                m["HyperlinkButtonForegroundDisabled"] = grayText;

                m["SplitButtonBackgroundChecked"] = highlight;
                m["SplitButtonBackgroundCheckedPointerOver"] = controlText;
                m["SplitButtonBackgroundCheckedPressed"] = control;
                m["SplitButtonBackgroundCheckedDisabled"] = grayText;
                m["SplitButtonForegroundChecked"] = highlightText;
                m["SplitButtonForegroundCheckedPointerOver"] = control;
                m["SplitButtonForegroundCheckedPressed"] = controlText;
                m["SplitButtonForegroundCheckedDisabled"] = window;
                return;
            }

            m["AccentButtonBackground"] = m[AccentFillDefaultKey];
            m["AccentButtonBackgroundPointerOver"] = m[AccentFillSecondaryKey];
            m["AccentButtonBackgroundPressed"] = m[AccentFillTertiaryKey];
            m["AccentButtonBackgroundDisabled"] = m[AccentFillDisabledKey];
            m["AccentButtonForeground"] = m[TextOnAccentPrimaryKey];
            m["AccentButtonForegroundPointerOver"] = m[TextOnAccentPrimaryKey];
            m["AccentButtonForegroundPressed"] = m[TextOnAccentSecondaryKey];
            m["AccentButtonForegroundDisabled"] = m[TextOnAccentDisabledKey];

            m["ToggleButtonBackgroundChecked"] = m[AccentFillDefaultKey];
            m["ToggleButtonBackgroundCheckedPointerOver"] = m[AccentFillSecondaryKey];
            m["ToggleButtonBackgroundCheckedPressed"] = m[AccentFillTertiaryKey];
            m["ToggleButtonBackgroundCheckedDisabled"] = m[AccentFillDisabledKey];
            m["ToggleButtonForegroundChecked"] = m[TextOnAccentPrimaryKey];
            m["ToggleButtonForegroundCheckedPointerOver"] = m[TextOnAccentPrimaryKey];
            m["ToggleButtonForegroundCheckedPressed"] = m[TextOnAccentSecondaryKey];
            m["ToggleButtonForegroundCheckedDisabled"] = m[TextOnAccentDisabledKey];

            m["CheckBoxCheckBackgroundFillChecked"] = m[AccentFillDefaultKey];
            m["CheckBoxCheckBackgroundFillCheckedPointerOver"] = m[AccentFillSecondaryKey];
            m["CheckBoxCheckBackgroundFillCheckedPressed"] = m[AccentFillTertiaryKey];
            m["CheckBoxCheckBackgroundFillCheckedDisabled"] = m[AccentFillDisabledKey];
            m["CheckBoxCheckGlyphForegroundChecked"] = m[TextOnAccentPrimaryKey];
            m["CheckBoxCheckGlyphForegroundCheckedPointerOver"] = m[TextOnAccentPrimaryKey];
            m["CheckBoxCheckGlyphForegroundCheckedPressed"] = m[TextOnAccentSecondaryKey];
            m["CheckBoxCheckGlyphForegroundCheckedDisabled"] = m[TextOnAccentDisabledKey];
            m["CheckBoxCheckBackgroundFillIndeterminate"] = m[AccentFillDefaultKey];
            m["CheckBoxCheckBackgroundFillIndeterminatePointerOver"] = m[AccentFillSecondaryKey];
            m["CheckBoxCheckBackgroundStrokeIndeterminatePointerOver"] = m[AccentFillSecondaryKey];
            m["CheckBoxCheckBackgroundFillIndeterminatePressed"] = m[AccentFillTertiaryKey];
            m["CheckBoxCheckBackgroundFillIndeterminateDisabled"] = m[AccentFillDisabledKey];
            m["CheckBoxCheckGlyphForegroundIndeterminate"] = m[TextOnAccentPrimaryKey];
            m["CheckBoxCheckGlyphForegroundIndeterminatePointerOver"] = m[TextOnAccentPrimaryKey];
            m["CheckBoxCheckGlyphForegroundIndeterminatePressed"] = m[TextOnAccentSecondaryKey];
            m["CheckBoxCheckGlyphForegroundIndeterminateDisabled"] = m[TextOnAccentDisabledKey];

            m["RadioButtonOuterEllipseCheckedFill"] = m[AccentFillDefaultKey];
            m["RadioButtonOuterEllipseCheckedFillPointerOver"] = m[AccentFillSecondaryKey];
            m["RadioButtonOuterEllipseCheckedFillPressed"] = m[AccentFillTertiaryKey];
            m["RadioButtonOuterEllipseCheckedFillDisabled"] = m[AccentFillDisabledKey];
            m["RadioButtonOuterEllipseCheckedStroke"] = m[AccentFillDefaultKey];
            m["RadioButtonOuterEllipseCheckedStrokePointerOver"] = m[AccentFillSecondaryKey];
            m["RadioButtonOuterEllipseCheckedStrokePressed"] = m[AccentFillTertiaryKey];
            m["RadioButtonOuterEllipseCheckedStrokeDisabled"] = m[AccentFillDisabledKey];
            m["RadioButtonCheckGlyphFill"] = m[TextOnAccentPrimaryKey];
            m["RadioButtonCheckGlyphFillPointerOver"] = m[TextOnAccentPrimaryKey];
            m["RadioButtonCheckGlyphFillPressed"] = m[TextOnAccentSecondaryKey];
            m["RadioButtonCheckGlyphFillDisabled"] = m[TextOnAccentPrimaryKey];

            m["ToggleSwitchFillOn"] = m[AccentFillDefaultKey];
            m["ToggleSwitchFillOnPointerOver"] = m[AccentFillSecondaryKey];
            m["ToggleSwitchFillOnPressed"] = m[AccentFillTertiaryKey];
            m["ToggleSwitchFillOnDisabled"] = m[AccentFillDisabledKey];
            m["ToggleSwitchKnobFillOn"] = m[TextOnAccentPrimaryKey];
            m["ToggleSwitchKnobFillOnPointerOver"] = m[TextOnAccentPrimaryKey];
            m["ToggleSwitchKnobFillOnPressed"] = m[TextOnAccentSecondaryKey];
            m["ToggleSwitchKnobFillOnDisabled"] = m[TextOnAccentDisabledKey];

            m["SliderThumbBackground"] = m[AccentFillDefaultKey];
            m["SliderThumbBackgroundPointerOver"] = m[AccentFillSecondaryKey];
            m["SliderThumbBackgroundPressed"] = m[AccentFillTertiaryKey];
            m["SliderThumbBackgroundDisabled"] = m[AccentFillDisabledKey];
            m["SliderTrackValueFill"] = m[AccentFillDefaultKey];
            m["SliderTrackValueFillPointerOver"] = m[AccentFillSecondaryKey];
            m["SliderTrackValueFillPressed"] = m[AccentFillTertiaryKey];
            m["SliderTrackValueFillDisabled"] = m[AccentFillDisabledKey];

            m["HyperlinkButtonForeground"] = m["AccentTextFillColorPrimary"];
            m["HyperlinkButtonForegroundPointerOver"] = m["AccentTextFillColorSecondary"];
            m["HyperlinkButtonForegroundPressed"] = m["AccentTextFillColorTertiary"];
            m["HyperlinkButtonForegroundDisabled"] = m["AccentTextFillColorDisabled"];

            m["SplitButtonBackgroundChecked"] = m[AccentFillDefaultKey];
            m["SplitButtonBackgroundCheckedPointerOver"] = m[AccentFillSecondaryKey];
            m["SplitButtonBackgroundCheckedPressed"] = m[AccentFillTertiaryKey];
            m["SplitButtonBackgroundCheckedDisabled"] = m[AccentFillDisabledKey];
            m["SplitButtonForegroundChecked"] = m[TextOnAccentPrimaryKey];
            m["SplitButtonForegroundCheckedPointerOver"] = m[TextOnAccentPrimaryKey];
            m["SplitButtonForegroundCheckedPressed"] = m[TextOnAccentSecondaryKey];
            m["SplitButtonForegroundCheckedDisabled"] = m["TextFillColorDisabled"];
        }
    }
}
