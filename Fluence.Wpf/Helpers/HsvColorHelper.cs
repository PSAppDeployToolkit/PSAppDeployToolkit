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
using System.Windows.Media;

namespace Fluence.Wpf.Helpers
{
    internal static class HsvColorHelper
    {
        internal static (double Hue, double Saturation, double Value) RgbToHsv(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double hue = 0;
            double saturation = 0;
            double value = max;
            if (delta > 0)
            {
                hue = max == r ? 60 * ((g - b) / delta % 6) : max == g ? 60 * (((b - r) / delta) + 2) : 60 * (((r - g) / delta) + 4);
                if (hue < 0)
                {
                    hue += 360;
                }
                saturation = max > 0 ? delta / max : 0;
            }
            return (hue, saturation, value);
        }

        internal static Color HsvToRgb(double hue, double saturation, double value)
        {
            hue %= 360;
            if (hue < 0)
            {
                hue += 360;
            }
            saturation = Math.Max(0, Math.Min(1, saturation));
            value = Math.Max(0, Math.Min(1, value));
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60 % 2) - 1));
            double m = value - c;
            double r, g, b;
            if (hue < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (hue < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (hue < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (hue < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (hue < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }
            return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        internal static Color GetLightVariant(Color baseColor, int level)
        {
            (double hue, double saturation, double value) = RgbToHsv(baseColor);
            double saturationReduction;
            double valueIncrease;
            switch (level)
            {
                case 1:
                    saturationReduction = 0.12;
                    valueIncrease = 0.10;
                    break;
                case 2:
                    saturationReduction = 0.24;
                    valueIncrease = 0.18;
                    break;
                case 3:
                    saturationReduction = 0.36;
                    valueIncrease = 0.26;
                    break;
                default:
                    saturationReduction = 0;
                    valueIncrease = 0;
                    break;
            }
            double newSaturation = Math.Max(saturation - saturationReduction, 0);
            double newValue = Math.Min(value + valueIncrease, 1);
            return HsvToRgb(hue, newSaturation, newValue);
        }

        internal static Color GetDarkVariant(Color baseColor, int level)
        {
            (double hue, double saturation, double value) = RgbToHsv(baseColor);
            double saturationIncrease;
            double valueDecrease;
            switch (level)
            {
                case 1:
                    saturationIncrease = 0.08;
                    valueDecrease = 0.12;
                    break;
                case 2:
                    saturationIncrease = 0.12;
                    valueDecrease = 0.20;
                    break;
                case 3:
                    saturationIncrease = 0.16;
                    valueDecrease = 0.28;
                    break;
                default:
                    saturationIncrease = 0;
                    valueDecrease = 0;
                    break;
            }
            double newSaturation = Math.Min(saturation + saturationIncrease, 1);
            double newValue = Math.Max(value - valueDecrease, 0);
            return HsvToRgb(hue, newSaturation, newValue);
        }

        internal static Color Lighten(Color color, double amount)
        {
            (double hue, double saturation, double value) = RgbToHsv(color);
            double newValue = Math.Min(value + amount, 1);
            return HsvToRgb(hue, saturation, newValue);
        }

        internal static Color Darken(Color color, double amount)
        {
            (double hue, double saturation, double value) = RgbToHsv(color);
            double newValue = Math.Max(value - amount, 0);
            return HsvToRgb(hue, saturation, newValue);
        }

        internal static Color WithAlpha(Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        /// <summary>
        /// Determines whether white text should be used on a given background color.
        /// Uses the same weighted formula as Windows (winaccent): (5*G + 2*R + B) &lt;= 1024.
        /// </summary>
        internal static bool ShouldUseWhiteText(Color color)
        {
            return ((5 * color.G) + (2 * color.R) + color.B) <= 1024;
        }

        /// <summary>
        /// Linear RGB blend matching Windows' palette generation.
        /// <paramref name="intensity"/> is 0-100 weight toward <paramref name="c1"/>.
        /// </summary>
        internal static Color BlendColors(Color c1, Color c2, double intensity)
        {
            double scaled = intensity * 255.0 / 100.0;
            double inv = 255.0 - scaled;
            byte r = (byte)Math.Round(((c1.R * scaled) + (c2.R * inv)) / 255.0);
            byte g = (byte)Math.Round(((c1.G * scaled) + (c2.G * inv)) / 255.0);
            byte b = (byte)Math.Round(((c1.B * scaled) + (c2.B * inv)) / 255.0);
            return Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// Boosts saturation in HLS color space by the given factor (capped at 1.0).
        /// Matches winaccent's increase_saturation which uses Python colorsys HLS (not HSV).
        /// </summary>
        internal static Color IncreaseSaturationHls(Color color, double factor)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            double h = 0;
            double l = (max + min) / 2.0;
            double s = 0;
            if (max != min)
            {
                double d = max - min;
                s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
                h = max == r ? ((g - b) / d) + (g < b ? 6 : 0) : max == g ? ((b - r) / d) + 2 : ((r - g) / d) + 4;
                h /= 6.0;
            }
            s = Math.Min(1.0, s * factor);
            return HlsToRgb(h, l, s);
        }

        /// <summary>
        /// Generates a six-shade accent ramp from a base color by applying lightness deltas
        /// in HSL space with saturation pinned to the chroma ceiling. For achromatic inputs
        /// (HSL S near zero), the base is returned unchanged for every stop so greys do not
        /// suddenly grow saturation.
        /// <para>
        /// Deltas are spaced ~10-12% apart on the L axis so adjacent stops (e.g. Light1
        /// next to the base, or Dark1 next to the base) are perceptibly distinct - controls
        /// that reference different rungs of the ramp for hover / pressed / focus states
        /// then show visible variation. An earlier calibration (Candidate F, fitted against
        /// 21 OS-captured AccentPalette ramps) used much tighter near-base steps (Light1
        /// +7%, Dark1 -4%) which approximated the OS perceptual transform but made adjacent
        /// stops nearly indistinguishable in control templates. We do not mirror the OS
        /// projection of arbitrary user input into the Fluent-compatible subspace - by
        /// design we use the user-supplied base verbatim - so the wider spread here is
        /// the right tradeoff for usable control highlights. See KNOWN_ISSUES.md "OS-
        /// transform modeling for the accent ramp" for the broader rationale.
        /// </para>
        /// </summary>
        internal static void GenerateAccentRampWinaccent(
            Color baseColor,
            out Color light1, out Color light2, out Color light3,
            out Color dark1, out Color dark2, out Color dark3)
        {
            light3 = ShiftHslMaxSat(baseColor, +0.36);
            light2 = ShiftHslMaxSat(baseColor, +0.24);
            light1 = ShiftHslMaxSat(baseColor, +0.12);
            dark1 = ShiftHslMaxSat(baseColor, -0.10);
            dark2 = ShiftHslMaxSat(baseColor, -0.20);
            dark3 = ShiftHslMaxSat(baseColor, -0.30);
        }

        /// <summary>
        /// Shifts a color's HSL lightness by <paramref name="dL"/> with saturation pinned to
        /// the chroma ceiling (1.0). Achromatic inputs (S &lt; 0.05) are returned unchanged so
        /// greys do not develop hue.
        /// </summary>
        internal static Color ShiftHslMaxSat(Color baseColor, double dL)
        {
            RgbToHsl(baseColor, out double h, out double s, out double l);
            if (s < 0.05)
            {
                return baseColor;
            }
            l = Math.Max(0, Math.Min(1, l + dL));
            return HslToRgb(h, 1.0, l);
        }

        /// <summary>
        /// Converts <see cref="Color"/> to HSL space (Hue in degrees [0..360], Saturation
        /// and Lightness in [0..1]).
        /// </summary>
        internal static void RgbToHsl(Color color, out double hue, out double saturation, out double lightness)
        {
            const double Epsilon = 1e-9;
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            lightness = (max + min) / 2.0;
            double d = max - min;
            if (d < Epsilon)
            {
                hue = 0;
                saturation = 0;
                return;
            }
            saturation = lightness > 0.5 ? d / (2 - max - min) : d / (max + min);
            hue = Math.Abs(max - r) < Epsilon
                ? ((g - b) / d) + (g < b ? 6 : 0)
                : Math.Abs(max - g) < Epsilon
                    ? ((b - r) / d) + 2
                    : ((r - g) / d) + 4;
            hue *= 60;
        }

        private static Color HslToRgb(double hue, double saturation, double lightness)
        {
            const double Epsilon = 1e-9;
            if (saturation < Epsilon)
            {
                byte v = (byte)Math.Round(lightness * 255);
                return Color.FromArgb(0xFF, v, v, v);
            }
            // HlsToRgb / HueToChannel below take hue in normalized [0..1] form (h / 6).
            double normalizedHue = hue / 360.0;
            return HlsToRgb(normalizedHue, lightness, saturation);
        }

        private static Color HlsToRgb(double h, double l, double s)
        {
            double r, g, b;
            if (s is not 0)
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - (l * s);
                double p = (2 * l) - q;
                r = HueToChannel(p, q, h + (1.0 / 3.0));
                g = HueToChannel(p, q, h);
                b = HueToChannel(p, q, h - (1.0 / 3.0));
            }
            else
            {
                r = g = b = l;
            }
            return Color.FromRgb((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
        }

        private static double HueToChannel(double p, double q, double t)
        {
            if (t < 0)
            {
                t += 1;
            }
            if (t > 1)
            {
                t -= 1;
            }
            return t < 1.0 / 6.0 ? p + ((q - p) * 6 * t) : t < 1.0 / 2.0 ? q : t < 2.0 / 3.0 ? p + ((q - p) * ((2.0 / 3.0) - t) * 6) : p;
        }

    }
}
