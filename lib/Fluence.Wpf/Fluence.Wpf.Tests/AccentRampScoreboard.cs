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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Scores candidate accent-ramp algorithms against a real dataset of OS-generated ramps
    /// captured manually from Windows 11 Settings -> Personalize -> Colors (8 distinct accents
    /// covering Windows defaults and custom hex values). Each fixture stores the requested
    /// color, the color the OS actually applied (some inputs get OS-corrected for contrast),
    /// and the 6 ramp stops captured from HKCU AccentPalette.
    /// </summary>
    [TestClass]
    public class AccentRampScoreboard
    {
        public TestContext? TestContext { get; set; }

        private sealed class Fixture(string name, Color requested, Color actual, Color l3, Color l2, Color l1, Color d1, Color d2, Color d3)
        {
            public string Name { get; } = name;
            public Color Requested { get; } = requested;
            public Color Actual { get; } = actual;
            public Color L3 { get; } = l3;
            public Color L2 { get; } = l2;
            public Color L1 { get; } = l1;
            public Color D1 { get; } = d1;
            public Color D2 { get; } = d2;
            public Color D3 { get; } = d3;
        }

        private static readonly Fixture[] Fixtures =
        [
            new("Windows Blue   #0078D4", FromHex(0x00, 0x78, 0xD4), FromHex(0x00, 0x78, 0xD4),
                FromHex(0x99, 0xEB, 0xFF), FromHex(0x4C, 0xC2, 0xFF), FromHex(0x00, 0x91, 0xF8),
                FromHex(0x00, 0x67, 0xC0), FromHex(0x00, 0x3E, 0x92), FromHex(0x00, 0x1A, 0x68)),

            new("Mango          #CA5010", FromHex(0xCA, 0x50, 0x10), FromHex(0xCA, 0x50, 0x10),
                FromHex(0xF5, 0xC0, 0x7C), FromHex(0xF0, 0x93, 0x46), FromHex(0xE4, 0x60, 0x12),
                FromHex(0xB6, 0x44, 0x0E), FromHex(0x87, 0x28, 0x08), FromHex(0x5C, 0x0E, 0x03)),

            new("Mint           #00B7C3", FromHex(0x00, 0xB7, 0xC3), FromHex(0x00, 0xB7, 0xC3),
                FromHex(0x69, 0xFC, 0xFF), FromHex(0x29, 0xF7, 0xFF), FromHex(0x00, 0xD5, 0xE1),
                FromHex(0x00, 0x9F, 0xAA), FromHex(0x00, 0x67, 0x70), FromHex(0x00, 0x34, 0x3B)),

            new("Plum           #881798", FromHex(0x88, 0x17, 0x98), FromHex(0x88, 0x17, 0x98),
                FromHex(0xEF, 0xAC, 0xF2), FromHex(0xD9, 0x5B, 0xE6), FromHex(0xAB, 0x1D, 0xBE),
                FromHex(0x7B, 0x14, 0x8B), FromHex(0x5B, 0x0C, 0x6D), FromHex(0x3F, 0x04, 0x51)),

            // OS corrected #A4262C -> #C94947 (likely contrast guardrail)
            new("Brick OS=>     #C94947", FromHex(0xA4, 0x26, 0x2C), FromHex(0xC9, 0x49, 0x47),
                FromHex(0xF5, 0xBD, 0xB2), FromHex(0xE8, 0x9B, 0x93), FromHex(0xD2, 0x60, 0x5C),
                FromHex(0xAF, 0x35, 0x33), FromHex(0x85, 0x25, 0x24), FromHex(0x59, 0x0D, 0x0D)),

            new("Liddy Green    #498205", FromHex(0x49, 0x82, 0x05), FromHex(0x49, 0x82, 0x05),
                FromHex(0xC1, 0xF9, 0x6C), FromHex(0x99, 0xF6, 0x18), FromHex(0x61, 0xA9, 0x07),
                FromHex(0x3E, 0x72, 0x04), FromHex(0x25, 0x4B, 0x03), FromHex(0x0D, 0x28, 0x01)),

            // OS corrected #1A8870 -> #17866E (minor)
            new("Teal  OS=>     #17866E", FromHex(0x1A, 0x88, 0x70), FromHex(0x17, 0x86, 0x6E),
                FromHex(0x90, 0xEC, 0xDF), FromHex(0x59, 0xE2, 0xCB), FromHex(0x1D, 0xAB, 0x8F),
                FromHex(0x12, 0x6D, 0x56), FromHex(0x0C, 0x4E, 0x37), FromHex(0x04, 0x2A, 0x14)),

            new("MS Red         #E81123", FromHex(0xE8, 0x11, 0x23), FromHex(0xE8, 0x11, 0x23),
                FromHex(0xFB, 0x9D, 0x8B), FromHex(0xF4, 0x67, 0x62), FromHex(0xEF, 0x27, 0x33),
                FromHex(0xD2, 0x0E, 0x1E), FromHex(0x9E, 0x09, 0x12), FromHex(0x6F, 0x03, 0x06)),

            new("Grey           #808080", FromHex(0x80, 0x80, 0x80), FromHex(0x80, 0x80, 0x80),
                FromHex(0x80, 0x80, 0x80), FromHex(0x80, 0x80, 0x80), FromHex(0x80, 0x80, 0x80),
                FromHex(0x7F, 0x7F, 0x7F), FromHex(0x7F, 0x7F, 0x7F), FromHex(0x7F, 0x7F, 0x7F)),

            // Capture session 2026-05-23 19:45:34 - 12 boundary + algorithm + brand probes.
            // All 12 inputs were OS-corrected; "Actual" reflects what Windows actually applied.
            new("Sat Red    OS=>#D9371E", FromHex(0xB3, 0x00, 0x00), FromHex(0xD9, 0x37, 0x1E),
                FromHex(0xF8, 0xB0, 0x87), FromHex(0xEF, 0x8C, 0x68), FromHex(0xE2, 0x4D, 0x2F),
                FromHex(0xBA, 0x2B, 0x17), FromHex(0x93, 0x1C, 0x0F), FromHex(0x65, 0x0A, 0x05)),

            new("Brick-sim  OS=>#C84A42", FromHex(0xA0, 0x25, 0x25), FromHex(0xC8, 0x4A, 0x42),
                FromHex(0xF5, 0xBE, 0xAD), FromHex(0xE8, 0x9C, 0x8E), FromHex(0xD1, 0x61, 0x57),
                FromHex(0xAB, 0x39, 0x32), FromHex(0x84, 0x25, 0x21), FromHex(0x58, 0x0D, 0x0C)),

            new("Bright Red OS=>#E7242F", FromHex(0xFF, 0x40, 0x40), FromHex(0xE7, 0x24, 0x2F),
                FromHex(0xFB, 0xA4, 0x96), FromHex(0xF5, 0x7E, 0x78), FromHex(0xEB, 0x3D, 0x43),
                FromHex(0xCC, 0x16, 0x20), FromHex(0x9D, 0x11, 0x17), FromHex(0x6C, 0x06, 0x08)),

            new("Deep Pur   OS=>#9555D3", FromHex(0x40, 0x00, 0x80), FromHex(0x95, 0x55, 0xD3),
                FromHex(0xEC, 0xC7, 0xF7), FromHex(0xD2, 0xA6, 0xED), FromHex(0xA6, 0x6B, 0xDA),
                FromHex(0x74, 0x33, 0xC5), FromHex(0x4F, 0x27, 0x96), FromHex(0x23, 0x0F, 0x68)),

            new("VDark Red  OS=>#621C1C", FromHex(0x5A, 0x1A, 0x1A), FromHex(0x62, 0x1C, 0x1C),
                FromHex(0xA6, 0x30, 0x30), FromHex(0x8A, 0x28, 0x28), FromHex(0x76, 0x22, 0x22),
                FromHex(0x4F, 0x16, 0x16), FromHex(0x3B, 0x11, 0x11), FromHex(0x1F, 0x09, 0x09)),

            new("Gold/Amber OS=>#9B7000", FromHex(0xD9, 0xA5, 0x20), FromHex(0x9B, 0x70, 0x00),
                FromHex(0xFF, 0xEC, 0x4E), FromHex(0xFF, 0xDB, 0x1A), FromHex(0xBE, 0x8E, 0x00),
                FromHex(0x84, 0x57, 0x00), FromHex(0x66, 0x38, 0x00), FromHex(0x44, 0x14, 0x00)),

            new("SeaGreen   OS=>#008A4B", FromHex(0x3C, 0xB3, 0x71), FromHex(0x00, 0x8A, 0x4B),
                FromHex(0x71, 0xFF, 0xB5), FromHex(0x2E, 0xFF, 0x97), FromHex(0x00, 0xB7, 0x62),
                FromHex(0x00, 0x72, 0x3B), FromHex(0x00, 0x53, 0x26), FromHex(0x00, 0x2F, 0x0E)),

            new("Steel Blue OS=>#3F7CAD", FromHex(0x46, 0x82, 0xB4), FromHex(0x3F, 0x7C, 0xAD),
                FromHex(0xB4, 0xE7, 0xF0), FromHex(0x92, 0xC8, 0xDD), FromHex(0x51, 0x92, 0xBF),
                FromHex(0x31, 0x62, 0x92), FromHex(0x20, 0x42, 0x6F), FromHex(0x0B, 0x1C, 0x47)),

            new("Med Purple OS=>#8563CD", FromHex(0x93, 0x70, 0xDB), FromHex(0x85, 0x63, 0xCD),
                FromHex(0xE9, 0xD2, 0xF6), FromHex(0xCC, 0xB1, 0xEA), FromHex(0x99, 0x79, 0xD5),
                FromHex(0x61, 0x3D, 0xC0), FromHex(0x41, 0x2E, 0x90), FromHex(0x18, 0x12, 0x62)),

            new("Corp Navy  OS=>#5D6EC8", FromHex(0x1A, 0x3D, 0x8F), FromHex(0x5D, 0x6E, 0xC8),
                FromHex(0xD6, 0xDE, 0xF5), FromHex(0xB2, 0xBD, 0xE8), FromHex(0x75, 0x84, 0xD1),
                FromHex(0x3E, 0x4E, 0xB7), FromHex(0x2E, 0x36, 0x89), FromHex(0x11, 0x14, 0x5F)),

            new("Corp Burg  OS=>#CF414E", FromHex(0x9B, 0x00, 0x28), FromHex(0xCF, 0x41, 0x4E),
                FromHex(0xF6, 0xB7, 0xB9), FromHex(0xEB, 0x95, 0x9A), FromHex(0xD7, 0x58, 0x63),
                FromHex(0xB5, 0x2E, 0x39), FromHex(0x8A, 0x21, 0x27), FromHex(0x5D, 0x0C, 0x0E)),

            new("Corp Frst  OS=>#007F50", FromHex(0x00, 0x75, 0x4A), FromHex(0x00, 0x7F, 0x50),
                FromHex(0x00, 0xD6, 0x87), FromHex(0x00, 0xB2, 0x70), FromHex(0x00, 0x99, 0x60),
                FromHex(0x00, 0x66, 0x40), FromHex(0x00, 0x4C, 0x30), FromHex(0x00, 0x28, 0x19)),
        ];

        [TestMethod]
        public void Score_AllAlgorithms_AgainstCapturedFixtures()
        {
            int totalCurrent = 0, totalF = 0, totalG = 0, totalH = 0;

            TestContext?.WriteLine("Fixture                    | Current | F     | G     | H");
            TestContext?.WriteLine("---------------------------+---------+-------+-------+-------");

            foreach (Fixture fix in Fixtures)
            {
                int dCurrent = Score(fix, RunCurrent);
                int dF = Score(fix, RunCandidateF);
                int dG = Score(fix, RunCandidateG);
                int dH = Score(fix, RunCandidateH);

                totalCurrent += dCurrent;
                totalF += dF;
                totalG += dG;
                totalH += dH;

                TestContext?.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,-26} | {1,7} | {2,5} | {3,5} | {4,5}", fix.Name, dCurrent, dF, dG, dH));
            }

            TestContext?.WriteLine("---------------------------+---------+-------+-------+-------");
            TestContext?.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,-26} | {1,7} | {2,5} | {3,5} | {4,5}", "TOTAL (lower = better)", totalCurrent, totalF, totalG, totalH));
            TestContext?.WriteLine("");
            int stops = Fixtures.Length * 6;
            TestContext?.WriteLine(string.Format(CultureInfo.InvariantCulture, "Mean per stop ({0} stops):  | {1,7:F1} | {2,5:F1} | {3,5:F1} | {4,5:F1}", stops, totalCurrent / (double)stops, totalF / (double)stops, totalG / (double)stops, totalH / (double)stops));

            Assert.IsTrue(Fixtures.Length > 0);
        }

        private static int Score(Fixture fix, Func<Color, (Color, Color, Color, Color, Color, Color)> algo)
        {
            (Color l3, Color l2, Color l1, Color d1, Color d2, Color d3) = algo(fix.Actual);
            return Delta(l3, fix.L3) + Delta(l2, fix.L2) + Delta(l1, fix.L1)
                 + Delta(d1, fix.D1) + Delta(d2, fix.D2) + Delta(d3, fix.D3);
        }

        private static int Delta(Color a, Color b)
        {
            return Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
        }

        // Algorithm A: current Fluence (HsvColorHelper.GenerateAccentRampWinaccent).
        private static (Color, Color, Color, Color, Color, Color) RunCurrent(Color baseColor)
        {
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color l1, out Color l2, out Color l3,
                out Color d1, out Color d2, out Color d3);
            return (l3, l2, l1, d1, d2, d3);
        }

        // Candidate F: HSL L-shift + S=1 clip (constant deltas tuned to Windows blue).
        private static (Color, Color, Color, Color, Color, Color) RunCandidateF(Color baseColor)
        {
            return (ShiftHsl(baseColor, +0.384, 0), ShiftHsl(baseColor, +0.233, 0), ShiftHsl(baseColor, +0.070, 0),
                    ShiftHsl(baseColor, -0.040, 0), ShiftHsl(baseColor, -0.130, 0), ShiftHsl(baseColor, -0.212, 0));
        }

        // Candidate G: F + linear hue rotation dH = -40 * dL deg (tuned to Windows blue).
        private static (Color, Color, Color, Color, Color, Color) RunCandidateG(Color baseColor)
        {
            return (ShiftHsl(baseColor, +0.384, -15.36), ShiftHsl(baseColor, +0.233, -9.32), ShiftHsl(baseColor, +0.070, -2.80),
                    ShiftHsl(baseColor, -0.040, +1.60), ShiftHsl(baseColor, -0.130, +5.20), ShiftHsl(baseColor, -0.212, +8.48));
        }

        // Candidate H (new): target-L approach. Each stop has a fixed L target derived from
        // the mean of the captured fixtures. Saturation pinned to 1 unless input is achromatic.
        // Hue preserved from input.
        private static (Color, Color, Color, Color, Color, Color) RunCandidateH(Color baseColor)
        {
            return (ShiftHslToTarget(baseColor, 0.78), ShiftHslToTarget(baseColor, 0.62), ShiftHslToTarget(baseColor, 0.47),
                    ShiftHslToTarget(baseColor, 0.37), ShiftHslToTarget(baseColor, 0.25), ShiftHslToTarget(baseColor, 0.16));
        }

        private static Color ShiftHsl(Color baseColor, double dL, double dHdeg)
        {
            RgbToHsl(baseColor, out double h, out double s, out double l);
            if (s < 0.05)
            {
                return baseColor; // Achromatic input -> keep as-is.
            }
            s = 1.0;
            h = (((h + dHdeg) % 360) + 360) % 360;
            l = Math.Max(0, Math.Min(1, l + dL));
            return HslToRgb(h, s, l);
        }

        private static Color ShiftHslToTarget(Color baseColor, double targetL)
        {
            RgbToHsl(baseColor, out double h, out double s, out double _);
            if (s < 0.05)
            {
                // Achromatic input: stay grey at targetL.
                byte g = (byte)Math.Round(targetL * 255, MidpointRounding.ToEven);
                return Color.FromArgb(0xFF, g, g, g);
            }
            return HslToRgb(h, 1.0, targetL);
        }

        private static void RgbToHsl(Color c, out double h, out double s, out double l)
        {
            const double Epsilon = 1e-9;
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2.0;
            double d = max - min;
            if (d < Epsilon)
            {
                h = 0;
                s = 0;
                return;
            }
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            h = Math.Abs(max - r) < Epsilon
                ? ((g - b) / d) + (g < b ? 6 : 0)
                : Math.Abs(max - g) < Epsilon
                    ? ((b - r) / d) + 2
                    : ((r - g) / d) + 4;
            h *= 60;
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            const double Epsilon = 1e-9;
            if (s < Epsilon)
            {
                byte v = (byte)Math.Round(l * 255, MidpointRounding.ToEven);
                return Color.FromArgb(0xFF, v, v, v);
            }
            double q = l < 0.5 ? l * (1 + s) : l + s - (l * s);
            double p = (2 * l) - q;
            double hk = h / 360.0;
            double r = HueToRgb(p, q, hk + (1.0 / 3.0));
            double g = HueToRgb(p, q, hk);
            double b = HueToRgb(p, q, hk - (1.0 / 3.0));
            return Color.FromArgb(0xFF,
                (byte)Math.Round(r * 255, MidpointRounding.ToEven),
                (byte)Math.Round(g * 255, MidpointRounding.ToEven),
                (byte)Math.Round(b * 255, MidpointRounding.ToEven));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0)
            {
                t++;
            }
            if (t > 1)
            {
                t--;
            }
            return t < 1.0 / 6.0
                ? p + ((q - p) * 6 * t)
                : t < 1.0 / 2.0
                    ? q
                    : t < 2.0 / 3.0
                        ? p + ((q - p) * ((2.0 / 3.0) - t) * 6)
                        : p;
        }

        private static Color FromHex(byte r, byte g, byte b)
        {
            return Color.FromArgb(0xFF, r, g, b);
        }
    }
}
