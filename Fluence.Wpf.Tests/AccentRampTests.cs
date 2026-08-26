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
using Fluence.Wpf.Helpers;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Pins observable properties of the fallback accent ramp generator. The system
    /// <c>AccentPalette</c> registry blob is the source of truth for any system accent
    /// (see <see cref="RegistryHelper.TryGetAccentPalette"/>); the generator only runs
    /// when that blob is unavailable or when the caller supplies a custom color.
    /// </summary>
    public class AccentRampTests
    {
        // The 8 representative accents from design.md Section 3.6.
        private static readonly Color WindowsBlue = Color.FromRgb(0x00, 0x78, 0xD4);
        private static readonly Color Mango = Color.FromRgb(0xCA, 0x50, 0x10);
        private static readonly Color Mint = Color.FromRgb(0x00, 0xB7, 0xC3);
        private static readonly Color Plum = Color.FromRgb(0x88, 0x17, 0x98);
        private static readonly Color Brick = Color.FromRgb(0xA4, 0x26, 0x2C);
        private static readonly Color Storm = Color.FromRgb(0x52, 0x5E, 0x54);
        private static readonly Color LiddyGreen = Color.FromRgb(0x49, 0x82, 0x05);
        private static readonly Color SportBlue = Color.FromRgb(0x00, 0xB2, 0x94);

        private static readonly Color[] AllAccents =
        [
            WindowsBlue, Mango, Mint, Plum, Brick, Storm, LiddyGreen, SportBlue,
        ];

        /// <summary>
        /// Theory rows: one RGB triple per representative accent from design.md Section 3.6.
        /// WPF <see cref="Color"/> is not xUnit-serializable, so rows carry the bytes and each
        /// test reconstructs the color.
        /// </summary>
        public static TheoryData<byte, byte, byte> AccentRows
        {
            get
            {
                TheoryData<byte, byte, byte> data = [];
                foreach (Color accent in AllAccents)
                {
                    data.Add(accent.R, accent.G, accent.B);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(AccentRows))]
        public void GenerateAccentRampWinaccent_IsDeterministic(byte r, byte g, byte b)
        {
            Color baseColor = Color.FromRgb(r, g, b);
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color a1, out Color a2, out Color a3, out Color a4, out Color a5, out Color a6);
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color b1, out Color b2, out Color b3, out Color b4, out Color b5, out Color b6);
            Assert.Equal(a1, b1);
            Assert.Equal(a2, b2);
            Assert.Equal(a3, b3);
            Assert.Equal(a4, b4);
            Assert.Equal(a5, b5);
            Assert.Equal(a6, b6);
        }

        [Theory]
        [MemberData(nameof(AccentRows))]
        public void GenerateAccentRampWinaccent_AllOutputsAreOpaque(byte r, byte g, byte b)
        {
            Color baseColor = Color.FromRgb(r, g, b);
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color l1, out Color l2, out Color l3,
                out Color d1, out Color d2, out Color d3);
            Assert.Equal((byte)0xFF, l1.A);
            Assert.Equal((byte)0xFF, l2.A);
            Assert.Equal((byte)0xFF, l3.A);
            Assert.Equal((byte)0xFF, d1.A);
            Assert.Equal((byte)0xFF, d2.A);
            Assert.Equal((byte)0xFF, d3.A);
        }

        [Theory]
        [MemberData(nameof(AccentRows))]
        public void GenerateAccentRampWinaccent_LightVariants_OrderedFromDimToBright(byte r, byte g, byte b)
        {
            Color baseColor = Color.FromRgb(r, g, b);
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color l1, out Color l2, out Color l3,
                out Color _, out Color _, out Color _);

            double l1V = ValueOf(l1);
            double l2V = ValueOf(l2);
            double l3V = ValueOf(l3);
            Assert.True(l3V >= l2V, $"Light3 V should be >= Light2 V for {Hex(baseColor)}");
            Assert.True(l2V >= l1V, $"Light2 V should be >= Light1 V for {Hex(baseColor)}");
        }

        [Theory]
        [MemberData(nameof(AccentRows))]
        public void GenerateAccentRampWinaccent_DarkVariants_OrderedFromBrightToDim(byte r, byte g, byte b)
        {
            Color baseColor = Color.FromRgb(r, g, b);
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color _, out Color _, out Color _,
                out Color d1, out Color d2, out Color d3);

            double d1V = ValueOf(d1);
            double d2V = ValueOf(d2);
            double d3V = ValueOf(d3);
            Assert.True(d3V <= d2V, $"Dark3 V should be <= Dark2 V for {Hex(baseColor)}");
            Assert.True(d2V <= d1V, $"Dark2 V should be <= Dark1 V for {Hex(baseColor)}");
        }

        /// <summary>
        /// Sanity check: when a system <c>AccentPalette</c> is present, the registry helper
        /// must return seven distinct opaque colors. This pins the contract relied on by
        /// <see cref="ApplicationAccentColorManager.ApplySystemAccent"/> to consume the
        /// system-supplied ramp directly instead of running the algorithm.
        /// </summary>
        /// <summary>
        /// Declarative skip condition: true when the system <c>AccentPalette</c> registry blob
        /// is present on this machine.
        /// </summary>
        public static bool SystemAccentPalettePresent =>
            RegistryHelper.TryGetAccentPalette(out Color[]? palette) && palette is not null;

        [Fact(SkipUnless = nameof(SystemAccentPalettePresent), Skip = "AccentPalette not present on this machine; cannot verify system ramp shape.")]
        public void TryGetAccentPalette_WhenPresent_ReturnsSevenOpaqueColors()
        {
            Assert.True(RegistryHelper.TryGetAccentPalette(out Color[]? palette));
            Assert.NotNull(palette);

            Assert.True(palette.Length >= 7, "AccentPalette should expose at least 7 ramp colors");
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal((byte)0xFF, palette[i].A);
            }
        }

        private static double ValueOf(Color c)
        {
            return HsvColorHelper.RgbToHsv(c).Value;
        }

        private static string Hex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }
}
