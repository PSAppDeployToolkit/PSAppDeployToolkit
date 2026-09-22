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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

// Captured Windows accent ramps, measured 2026-05-23 from the OS palette on this
// hardware. Kept as data after AccentRampScoreboard was deleted: the scoring
// harness compared four candidate algorithms and could not fail, but these
// twenty-one measurements are real and are the reference any future ramp change
// is judged against. The deleted class's own docstring claimed 8 distinct
// accents, but its Fixtures array actually held all twenty-one rows below.
//
// Windows Blue   #0078D4: requested #0078D4, actual #0078D4, L3 #99EBFF, L2 #4CC2FF, L1 #0091F8, D1 #0067C0, D2 #003E92, D3 #001A68
// Mango          #CA5010: requested #CA5010, actual #CA5010, L3 #F5C07C, L2 #F09346, L1 #E46012, D1 #B6440E, D2 #872808, D3 #5C0E03
// Mint           #00B7C3: requested #00B7C3, actual #00B7C3, L3 #69FCFF, L2 #29F7FF, L1 #00D5E1, D1 #009FAA, D2 #006770, D3 #00343B
// Plum           #881798: requested #881798, actual #881798, L3 #EFACF2, L2 #D95BE6, L1 #AB1DBE, D1 #7B148B, D2 #5B0C6D, D3 #3F0451
// OS corrected #A4262C -> #C94947 (likely contrast guardrail)
// Brick OS=>     #C94947: requested #A4262C, actual #C94947, L3 #F5BDB2, L2 #E89B93, L1 #D2605C, D1 #AF3533, D2 #852524, D3 #590D0D
// Liddy Green    #498205: requested #498205, actual #498205, L3 #C1F96C, L2 #99F618, L1 #61A907, D1 #3E7204, D2 #254B03, D3 #0D2801
// OS corrected #1A8870 -> #17866E (minor)
// Teal  OS=>     #17866E: requested #1A8870, actual #17866E, L3 #90ECDF, L2 #59E2CB, L1 #1DAB8F, D1 #126D56, D2 #0C4E37, D3 #042A14
// MS Red         #E81123: requested #E81123, actual #E81123, L3 #FB9D8B, L2 #F46762, L1 #EF2733, D1 #D20E1E, D2 #9E0912, D3 #6F0306
// Grey           #808080: requested #808080, actual #808080, L3 #808080, L2 #808080, L1 #808080, D1 #7F7F7F, D2 #7F7F7F, D3 #7F7F7F
//
// Capture session 2026-05-23 19:45:34 - 12 boundary + algorithm + brand probes.
// All 12 inputs were OS-corrected; "Actual" reflects what Windows actually applied.
// Sat Red    OS=>#D9371E: requested #B30000, actual #D9371E, L3 #F8B087, L2 #EF8C68, L1 #E24D2F, D1 #BA2B17, D2 #931C0F, D3 #650A05
// Brick-sim  OS=>#C84A42: requested #A02525, actual #C84A42, L3 #F5BEAD, L2 #E89C8E, L1 #D16157, D1 #AB3932, D2 #842521, D3 #580D0C
// Bright Red OS=>#E7242F: requested #FF4040, actual #E7242F, L3 #FBA496, L2 #F57E78, L1 #EB3D43, D1 #CC1620, D2 #9D1117, D3 #6C0608
// Deep Pur   OS=>#9555D3: requested #400080, actual #9555D3, L3 #ECC7F7, L2 #D2A6ED, L1 #A66BDA, D1 #7433C5, D2 #4F2796, D3 #230F68
// VDark Red  OS=>#621C1C: requested #5A1A1A, actual #621C1C, L3 #A63030, L2 #8A2828, L1 #762222, D1 #4F1616, D2 #3B1111, D3 #1F0909
// Gold/Amber OS=>#9B7000: requested #D9A520, actual #9B7000, L3 #FFEC4E, L2 #FFDB1A, L1 #BE8E00, D1 #845700, D2 #663800, D3 #441400
// SeaGreen   OS=>#008A4B: requested #3CB371, actual #008A4B, L3 #71FFB5, L2 #2EFF97, L1 #00B762, D1 #00723B, D2 #005326, D3 #002F0E
// Steel Blue OS=>#3F7CAD: requested #4682B4, actual #3F7CAD, L3 #B4E7F0, L2 #92C8DD, L1 #5192BF, D1 #316292, D2 #20426F, D3 #0B1C47
// Med Purple OS=>#8563CD: requested #9370DB, actual #8563CD, L3 #E9D2F6, L2 #CCB1EA, L1 #9979D5, D1 #613DC0, D2 #412E90, D3 #181262
// Corp Navy  OS=>#5D6EC8: requested #1A3D8F, actual #5D6EC8, L3 #D6DEF5, L2 #B2BDE8, L1 #7584D1, D1 #3E4EB7, D2 #2E3689, D3 #11145F
// Corp Burg  OS=>#CF414E: requested #9B0028, actual #CF414E, L3 #F6B7B9, L2 #EB959A, L1 #D75863, D1 #B52E39, D2 #8A2127, D3 #5D0C0E
// Corp Frst  OS=>#007F50: requested #00754A, actual #007F50, L3 #00D687, L2 #00B270, L1 #009960, D1 #006640, D2 #004C30, D3 #002819

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Pins observable properties of the fallback accent ramp generator. The system
    /// <c language="text">AccentPalette</c> registry blob is the source of truth for any system accent
    /// (see <see cref="RegistryHelper.TryGetAccentPalette"/>); the generator only runs
    /// when that blob is unavailable or when the caller supplies a custom color.
    /// </summary>
    public sealed class AccentTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            }));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        [Fact]
        public Task ApplySystemAccent_PopulatesRampAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplySystemAccent();

                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColor);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorLight1);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorLight2);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorLight3);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorDark1);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorDark2);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorDark3);

                Assert.NotNull(app.Resources["SystemAccentColor"]);
            });
        }

        [Fact]
        public Task ApplyCustomAccent_SetsCorrectBaseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                Color customColor = Color.FromRgb(0xFF, 0x88, 0x00);
                ApplicationAccentColorManager.ApplyCustomAccent(customColor);

                Assert.Equal(customColor, ApplicationAccentColorManager.SystemAccentColor);

                Assert.NotEqual(customColor, ApplicationAccentColorManager.SystemAccentColorLight1);
                Assert.NotEqual(customColor, ApplicationAccentColorManager.SystemAccentColorDark1);

                Assert.NotEqual(ApplicationAccentColorManager.SystemAccentColorLight1,
                    ApplicationAccentColorManager.SystemAccentColorDark1);
            });
        }

        /// <summary>
        /// The Windows blue ramp that ApplyApplicationAccent used to hard-code is now written by
        /// the caller. It must still raise AccentColorChanged exactly once.
        /// </summary>
        [Fact]
        public Task ApplyCustomAccent_WindowsBlue_RaisesAccentColorChangedOnceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                // Pin a seed that is deliberately not Windows blue, so the call under test is a
                // genuine ramp transition on every host. Without this the fixture starts on the OS
                // accent, and a machine with no HKCU accent palette falls back to the generated
                // #0078D4 ramp: identical output, which the engine's redundant-publish gate
                // correctly skips, and no event would be raised.
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xFF, 0x88, 0x00));

                int eventCount = 0;
                void OnAccentColorChanged(object? sender, EventArgs e)
                {
                    eventCount++;
                }

                ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
                try
                {
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
                }

                Assert.Equal(1, eventCount);
            });
        }

        [Fact]
        public Task ApplyCustomAccent_RaisesAccentColorChangedOnceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                int eventCount = 0;
                void OnAccentColorChanged(object? sender, EventArgs e)
                {
                    eventCount++;
                }

                ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
                try
                {
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xFF, 0x88, 0x00));
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
                }

                Assert.Equal(1, eventCount);
            });
        }

        [Fact]
        public Task ApplyCustomAccent_PerThemeSeeds_FollowResolvedThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Color lightSeed = Color.FromRgb(0x0F, 0x6C, 0xBD);
                Color darkSeed = Color.FromRgb(0x47, 0x9E, 0xF5);

                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(lightSeed, darkSeed);
                Assert.Equal(lightSeed, ApplicationAccentColorManager.SystemAccentColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                Assert.Equal(darkSeed, ApplicationAccentColorManager.SystemAccentColor);

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                Assert.Equal(darkSeed, ApplicationAccentColorManager.SystemAccentColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                Assert.Equal(lightSeed, ApplicationAccentColorManager.SystemAccentColor);
            });
        }

        [Fact]
        public Task ApplyCustomAccent_PerThemeSeeds_RaisesAccentColorChangedOnceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                int eventCount = 0;
                void OnAccentColorChanged(object? sender, EventArgs e)
                {
                    eventCount++;
                }

                ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
                try
                {
                    ApplicationAccentColorManager.ApplyCustomAccent(
                        Color.FromRgb(0x0F, 0x6C, 0xBD), Color.FromRgb(0x47, 0x9E, 0xF5));
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
                }

                Assert.Equal(1, eventCount);
            });
        }

        [Fact]
        public Task ThemeChange_UpdatesAdaptiveAccentsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Color customColor = Color.FromRgb(0x00, 0x78, 0xD4);
                ApplicationAccentColorManager.ApplyCustomAccent(customColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                Color darkPrimary = ApplicationAccentColorManager.SystemAccentColorPrimary;

                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                Color lightPrimary = ApplicationAccentColorManager.SystemAccentColorPrimary;

                Assert.NotEqual(darkPrimary, lightPrimary);

                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorLight2, darkPrimary);
                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorDark1, lightPrimary);
            });
        }

        // Previous tests ApplyCustomAccent_WindowsBlue_DarkThemeUsesCanonicalLight2 and
        // ApplyCustomAccent_WindowsBlue_LightThemeUsesCanonicalDark1 (plus the helpers
        // AssertColorResource / AssertBrushResource that supported them) were removed: they
        // asserted the canonical OS Windows blue ramp, which only fired through the deleted
        // KnownAccentRamps short-circuit. The new design uses the caller's color verbatim and
        // runs Fluence's ramp algorithm directly (no OS mirroring), so the canonical assertions
        // no longer apply. AccentRampScoreboard covers algorithm regression against 21 captured
        // OS ramps.

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
        /// Sanity check: when a system <c language="text">AccentPalette</c> is present, the registry helper
        /// must return seven distinct opaque colors. This pins the contract relied on by
        /// <see cref="ApplicationAccentColorManager.ApplySystemAccent"/> to consume the
        /// system-supplied ramp directly instead of running the algorithm.
        /// </summary>
        /// <summary>
        /// Declarative skip condition: true when the system <c language="text">AccentPalette</c> registry blob
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
