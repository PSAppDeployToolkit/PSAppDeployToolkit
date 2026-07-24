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

using Fluence.Wpf.Native;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// READ-ONLY PROBE - enumerates every immersive color set Windows ships and dumps the
    /// Light3/2/1, Base, Dark1/2/3 ramp for each. No registry writes, no system mutations;
    /// purely a call to <c>uxtheme.dll</c> ordinals (#94, #95, #96).
    /// <para>
    /// If this reports a row for every Windows-default accent (Mango, Mint, Plum, etc.) with
    /// its full 7-color ramp, then <c>ApplyCustomAccent(color)</c> can look up the row at
    /// runtime instead of relying on a manually-captured <c>KnownAccentRamps</c> table.
    /// </para>
    /// </summary>
    [TestClass]
    public class ImmersiveColorSetProbe
    {
        public TestContext? TestContext { get; set; }

        // Canonical immersive color type names. The OS resolves these to type IDs that are
        // stable for the process lifetime.
        private const string NameBase = "ImmersiveSystemAccent";
        private const string NameLight1 = "ImmersiveSystemAccentLight1";
        private const string NameLight2 = "ImmersiveSystemAccentLight2";
        private const string NameLight3 = "ImmersiveSystemAccentLight3";
        private const string NameDark1 = "ImmersiveSystemAccentDark1";
        private const string NameDark2 = "ImmersiveSystemAccentDark2";
        private const string NameDark3 = "ImmersiveSystemAccentDark3";

        // Result (2026-05-23 run on Win11 Pro for Workstations, build 26100+):
        //   * 50 color sets reported, ALL with the same base color (the user's current
        //     active accent). Sets are per-context variants (high-contrast, etc.), not a
        //     pre-baked table of every Windows default accent. Cross-check found only the
        //     currently-active base; Mango/Mint/Plum/Brick/Storm/Liddy/SportBlue all absent.
        //   * Active set index 100 exceeds reported count of 50 (likely an unrelated
        //     pref index, not a slot into this list).
        //   * Conclusion: uxtheme color sets do NOT expose per-color ramps for arbitrary
        //     accents. Cannot be used to look up custom-accent ramps without first changing
        //     the user's system accent in Settings.
        [Ignore("Read-only probe; result documented 2026-05-23: only the active accent is exposed.")]
        [TestMethod]
        public void Probe_EnumerateColorSets_DumpsAllRamps()
        {
            uint count = NativeMethods.GetImmersiveColorSetCount();
            TestContext?.WriteLine($"Total immersive color sets reported by OS: {count}");

            uint typeBase = NativeMethods.GetImmersiveColorTypeFromName(NameBase);
            uint typeLight1 = NativeMethods.GetImmersiveColorTypeFromName(NameLight1);
            uint typeLight2 = NativeMethods.GetImmersiveColorTypeFromName(NameLight2);
            uint typeLight3 = NativeMethods.GetImmersiveColorTypeFromName(NameLight3);
            uint typeDark1 = NativeMethods.GetImmersiveColorTypeFromName(NameDark1);
            uint typeDark2 = NativeMethods.GetImmersiveColorTypeFromName(NameDark2);
            uint typeDark3 = NativeMethods.GetImmersiveColorTypeFromName(NameDark3);

            TestContext?.WriteLine($"Type IDs: Base={typeBase} L1={typeLight1} L2={typeLight2} L3={typeLight3} D1={typeDark1} D2={typeDark2} D3={typeDark3}");

            uint activeSet = NativeMethods.GetImmersiveUserColorSetPreference(bForceCheckRegistry: false, bSkipCheckOnFail: false);
            TestContext?.WriteLine($"Active set index (user's current): {activeSet}");
            TestContext?.WriteLine("");

            const uint validTypeId = 0xFFFFFFFF;
            if (typeBase == validTypeId || count == 0)
            {
                Assert.Inconclusive("Immersive color APIs returned no data on this machine.");
                return;
            }

            HashSet<int> seenBases = [];
            int duplicateBaseCount = 0;

            TestContext?.WriteLine("Set | Base    | L3      L2      L1      Base    D1      D2      D3");
            TestContext?.WriteLine("----+---------+--------------------------------------------------------");

            for (uint i = 0; i < count; i++)
            {
                uint baseRaw = NativeMethods.GetImmersiveColorFromColorSetEx(i, typeBase, bIgnoreHighContrast: true, dwHighContrastCacheMode: 0);
                Color baseColor = DecodeAbgrToRgb(baseRaw);
                int packed = Pack(baseColor);

                if (!seenBases.Add(packed))
                {
                    duplicateBaseCount++;
                    continue; // skip duplicates to keep the log readable
                }

                uint l1Raw = NativeMethods.GetImmersiveColorFromColorSetEx(i, typeLight1, bIgnoreHighContrast: true, 0);
                uint l2Raw = NativeMethods.GetImmersiveColorFromColorSetEx(i, typeLight2, bIgnoreHighContrast: true, 0);
                uint l3Raw = NativeMethods.GetImmersiveColorFromColorSetEx(i, typeLight3, bIgnoreHighContrast: true, 0);
                uint d1Raw = NativeMethods.GetImmersiveColorFromColorSetEx(i, typeDark1, bIgnoreHighContrast: true, 0);
                uint d2Raw = NativeMethods.GetImmersiveColorFromColorSetEx(i, typeDark2, bIgnoreHighContrast: true, 0);
                uint d3Raw = NativeMethods.GetImmersiveColorFromColorSetEx(i, typeDark3, bIgnoreHighContrast: true, 0);

                string activeMarker = (i == activeSet) ? "*" : " ";
                TestContext?.WriteLine($"{i,3} | {Hex(baseColor)}{activeMarker}| {Hex(DecodeAbgrToRgb(l3Raw))} {Hex(DecodeAbgrToRgb(l2Raw))} {Hex(DecodeAbgrToRgb(l1Raw))} {Hex(baseColor)} {Hex(DecodeAbgrToRgb(d1Raw))} {Hex(DecodeAbgrToRgb(d2Raw))} {Hex(DecodeAbgrToRgb(d3Raw))}");
            }

            TestContext?.WriteLine("");
            TestContext?.WriteLine($"Unique base colors: {seenBases.Count} (of {count} total sets, {duplicateBaseCount.ToString(CultureInfo.InvariantCulture)} skipped duplicates)");
            TestContext?.WriteLine("");

            // Cross-check against our design.md candidate list.
            Color[] candidates =
            [
                Color.FromRgb(0x00, 0x78, 0xD4), // Default Blue
                Color.FromRgb(0xCA, 0x50, 0x10), // Mango
                Color.FromRgb(0x00, 0xB7, 0xC3), // Mint
                Color.FromRgb(0x88, 0x17, 0x98), // Plum
                Color.FromRgb(0xA4, 0x26, 0x2C), // Brick
                Color.FromRgb(0x52, 0x5E, 0x54), // Storm
                Color.FromRgb(0x49, 0x82, 0x05), // Liddy Green
                Color.FromRgb(0x00, 0xB2, 0x94), // Sport Blue
            ];

            TestContext?.WriteLine("Cross-check vs design.md candidate accents:");
            foreach (Color c in candidates)
            {
                bool present = seenBases.Contains(Pack(c));
                TestContext?.WriteLine($"  {Hex(c)} -> {(present ? "PRESENT" : "NOT FOUND")}");
            }

            Assert.IsTrue(count > 0, "Expected at least one immersive color set");
        }

        private static Color DecodeAbgrToRgb(uint nativeColor)
        {
            // Native layout (per uxtheme #95): low byte = R, then G, then B, then A.
            // Match the sample's extraction: A=(>>24), R=(>>0), G=(>>8), B=(>>16).
            byte r = (byte)(nativeColor & 0xFF);
            byte g = (byte)((nativeColor >> 8) & 0xFF);
            byte b = (byte)((nativeColor >> 16) & 0xFF);
            byte a = (byte)((nativeColor >> 24) & 0xFF);
            return Color.FromArgb(a is 0 ? (byte)0xFF : a, r, g, b);
        }

        private static int Pack(Color c)
        {
            return c.R | (c.G << 8) | (c.B << 16);
        }

        private static string Hex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }
}
