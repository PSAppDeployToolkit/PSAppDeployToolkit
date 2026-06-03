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
using Microsoft.Win32;
using System;
using System.Threading;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// MANUAL EXPERIMENT - measures whether writing the DWM/Explorer accent color values
    /// causes Windows to regenerate the <c>AccentPalette</c> registry blob. If it does, we
    /// can read the new palette and use the OS-generated ramp for any custom color (no
    /// algorithm needed). If it does not, this approach is not viable and Fluence must rely
    /// on the HSL ramp algorithm in <c>HsvColorHelper.GenerateAccentRampWinaccent</c>.
    /// <para>
    /// **WARNING**: This experiment temporarily modifies the user's actual system accent
    /// color. The Start menu, taskbar, every Win11 app, and DWM window borders will see the
    /// new color until the restore step runs. The restore is in a try/finally and broadcasts
    /// <c>WM_SETTINGCHANGE</c>, but expect a flash of the experimental color (Mango = #CA5010).
    /// Do not run while screen-sharing or recording. All tests in this class are
    /// <c>[Ignore]</c>'d by default; remove the attribute to run.
    /// </para>
    /// </summary>
    [TestClass]
    public class AccentPaletteRegenerationExperiment
    {
        public TestContext? TestContext { get; set; }

        private const string DwmKey = NativeConstants.DwmRegistryPath;
        private const string AccentKey = NativeConstants.AccentRegistryPath;
        private const string DwmAccentColorValue = NativeConstants.AccentColor;
        private const string DwmAccentColorInactiveValue = NativeConstants.AccentColorInactive;
        private const string AccentPaletteValue = NativeConstants.AccentPalette;
        private const string AccentColorMenuValue = "AccentColorMenu";
        private const string StartColorMenuValue = "StartColorMenu";


        /// <summary>
        /// Probe 1: write only to <c>DWM\AccentColor</c> (the simplest signal) and check whether
        /// AccentPalette regenerates after a 1s wait.
        /// </summary>
        [Ignore("Modifies user's system accent. Remove [Ignore] to run manually.")]
        [TestMethod]
        public void Experiment_WriteDwmAccentColor_DoesAccentPaletteRegenerate()
        {
            // Mango - clearly distinguishable from any Windows default the user might be on.
            Color experimentalAccent = Color.FromRgb(0xCA, 0x50, 0x10);

            RunExperiment(experimentalAccent, writeMode: WriteMode.DwmAccentColorOnly, waitMs: 1000);
        }

        /// <summary>
        /// Probe 2: write to all four user-facing accent values (DWM + Explorer\Accent) and check.
        /// </summary>
        [Ignore("Modifies user's system accent. Remove [Ignore] to run manually.")]
        [TestMethod]
        public void Experiment_WriteAllAccentValues_DoesAccentPaletteRegenerate()
        {
            Color experimentalAccent = Color.FromRgb(0xCA, 0x50, 0x10);

            RunExperiment(experimentalAccent, writeMode: WriteMode.AllAccentValues, waitMs: 1000);
        }

        /// <summary>
        /// Probe 3: write all accent values AND broadcast WM_SETTINGCHANGE with the
        /// "ImmersiveColorSet" lParam (Personalize sends this), then check.
        /// Result (2026-05-23 run, build 22000+): palette did NOT regenerate. Windows
        /// computes <c>AccentPalette</c> from a private themecpl/themeui code path only
        /// when the user picks an accent in Settings; registry writes are ignored.
        /// </summary>
        [Ignore("Modifies user's system accent. Result confirmed 2026-05-23: palette does NOT regenerate.")]
        [TestMethod]
        public void Experiment_WriteAllAndBroadcast_DoesAccentPaletteRegenerate()
        {
            Color experimentalAccent = Color.FromRgb(0xCA, 0x50, 0x10);

            RunExperiment(experimentalAccent, writeMode: WriteMode.AllAccentValuesAndBroadcast, waitMs: 1500);

            // Observational probe - the actual outcome is logged via TestContext.WriteLine
            // inside RunExperiment. Confirms the restore step left the palette readable.
            object? restored = ReadRaw(Registry.CurrentUser, AccentKey, AccentPaletteValue);
            Assert.IsNotNull(restored, "AccentPalette must be readable after restore");
        }

        private enum WriteMode
        {
            DwmAccentColorOnly,
            AllAccentValues,
            AllAccentValuesAndBroadcast,
        }

        private void RunExperiment(Color experimentalAccent, WriteMode writeMode, int waitMs)
        {
            // Save originals.
            object? originalDwmAccent = ReadRaw(Registry.CurrentUser, DwmKey, DwmAccentColorValue);
            object? originalDwmAccentInactive = ReadRaw(Registry.CurrentUser, DwmKey, DwmAccentColorInactiveValue);
            object? originalAccentColor = ReadRaw(Registry.CurrentUser, AccentKey, NativeConstants.AccentColor);
            object? originalAccentColorMenu = ReadRaw(Registry.CurrentUser, AccentKey, AccentColorMenuValue);
            object? originalStartColorMenu = ReadRaw(Registry.CurrentUser, AccentKey, StartColorMenuValue);
            byte[]? originalPalette = ReadRaw(Registry.CurrentUser, AccentKey, AccentPaletteValue) as byte[];

            TestContext?.WriteLine($"=== Experiment: {writeMode} ===");
            TestContext?.WriteLine($"Original DWM AccentColor: {FormatDwordValue(originalDwmAccent)}");
            TestContext?.WriteLine($"Original AccentPalette base (offset 12): {FormatPaletteBase(originalPalette)}");

            try
            {
                int dwmEncoded = EncodeAbgrDword(experimentalAccent); // DWM uses ABGR DWORD
                int explorerEncoded = EncodeAbgrDword(experimentalAccent); // Explorer also stores ABGR

                WriteDword(Registry.CurrentUser, DwmKey, DwmAccentColorValue, dwmEncoded);

                if (writeMode != WriteMode.DwmAccentColorOnly)
                {
                    WriteDword(Registry.CurrentUser, DwmKey, DwmAccentColorInactiveValue, dwmEncoded);
                    WriteDword(Registry.CurrentUser, AccentKey, NativeConstants.AccentColor, explorerEncoded);
                    WriteDword(Registry.CurrentUser, AccentKey, AccentColorMenuValue, explorerEncoded);
                    WriteDword(Registry.CurrentUser, AccentKey, StartColorMenuValue, explorerEncoded);
                }

                if (writeMode == WriteMode.AllAccentValuesAndBroadcast)
                {
                    _ = NativeMethods.SendMessageTimeout(new IntPtr(NativeMethods.HWND_BROADCAST),
                        NativeConstants.WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet",
                        NativeMethods.SMTO_ABORTIFHUNG, 1000, out IntPtr _);
                }

                Thread.Sleep(waitMs);

                byte[]? newPalette = ReadRaw(Registry.CurrentUser, AccentKey, AccentPaletteValue) as byte[];
                TestContext?.WriteLine($"After AccentPalette base (offset 12): {FormatPaletteBase(newPalette)}");

                bool paletteChanged = !PaletteEquals(originalPalette, newPalette);
                bool baseMatchesExperimental = newPalette is not null && newPalette.Length >= 16
                    && newPalette[12] == experimentalAccent.R
                    && newPalette[13] == experimentalAccent.G
                    && newPalette[14] == experimentalAccent.B;

                TestContext?.WriteLine($"Palette bytes changed: {paletteChanged}");
                TestContext?.WriteLine($"Palette base matches experimental color: {baseMatchesExperimental}");

                if (baseMatchesExperimental && newPalette is not null)
                {
                    TestContext?.WriteLine("=== OS regenerated the palette - Option A viable ===");
                    for (int i = 0; i < 7; i++)
                    {
                        TestContext?.WriteLine($"  palette[{i}] = #{newPalette[i * 4]:X2}{newPalette[(i * 4) + 1]:X2}{newPalette[(i * 4) + 2]:X2}");
                    }
                }
                else
                {
                    TestContext?.WriteLine("=== Palette did NOT regenerate to match experimental color - Option A NOT viable in this mode ===");
                }
            }
            finally
            {
                // Restore originals.
                RestoreRaw(Registry.CurrentUser, DwmKey, DwmAccentColorValue, originalDwmAccent);
                RestoreRaw(Registry.CurrentUser, DwmKey, DwmAccentColorInactiveValue, originalDwmAccentInactive);
                RestoreRaw(Registry.CurrentUser, AccentKey, NativeConstants.AccentColor, originalAccentColor);
                RestoreRaw(Registry.CurrentUser, AccentKey, AccentColorMenuValue, originalAccentColorMenu);
                RestoreRaw(Registry.CurrentUser, AccentKey, StartColorMenuValue, originalStartColorMenu);
                if (originalPalette is not null)
                {
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(AccentKey, writable: true);
                    key.SetValue(AccentPaletteValue, originalPalette, RegistryValueKind.Binary);
                }
                // Broadcast restore.
                _ = NativeMethods.SendMessageTimeout(new IntPtr(NativeMethods.HWND_BROADCAST),
                    NativeConstants.WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet",
                    NativeMethods.SMTO_ABORTIFHUNG, 1000, out IntPtr _);
            }
        }

        private static int EncodeAbgrDword(Color c)
        {
            // DWM AccentColor / Explorer AccentColor are ABGR DWORDs with FF alpha for opaque.
            // Bit layout: [31..24]=A, [23..16]=B, [15..8]=G, [7..0]=R.
            return unchecked((int)(((uint)0xFF << 24) | ((uint)c.B << 16) | ((uint)c.G << 8) | c.R));
        }

        private static string FormatDwordValue(object? raw)
        {
            if (raw is int i)
            {
                uint u = unchecked((uint)i);
                byte r = (byte)(u & 0xFF);
                byte g = (byte)((u >> 8) & 0xFF);
                byte b = (byte)((u >> 16) & 0xFF);
                byte a = (byte)(u >> 24);
                return $"0x{u:X8} (ABGR -> R={r:X2} G={g:X2} B={b:X2} A={a:X2})";
            }
            return raw?.ToString() ?? "<absent>";
        }

        private static string FormatPaletteBase(byte[]? palette)
        {
            return palette is null || palette.Length < 16
                ? "<absent or too short>"
                : $"#{palette[12]:X2}{palette[13]:X2}{palette[14]:X2}";
        }

        private static bool PaletteEquals(byte[]? a, byte[]? b)
        {
            if (a is null && b is null)
            {
                return true;
            }
            if (a is null || b is null || a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static object? ReadRaw(RegistryKey root, string path, string valueName)
        {
            using RegistryKey? key = root.OpenSubKey(path);
            return key?.GetValue(valueName);
        }

        private static void WriteDword(RegistryKey root, string path, string valueName, int value)
        {
            using RegistryKey key = root.CreateSubKey(path, writable: true);
            key.SetValue(valueName, value, RegistryValueKind.DWord);
        }

        private static void RestoreRaw(RegistryKey root, string path, string valueName, object? saved)
        {
            using RegistryKey key = root.CreateSubKey(path, writable: true);
            if (saved is null)
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
            else if (saved is int intValue)
            {
                key.SetValue(valueName, intValue, RegistryValueKind.DWord);
            }
            else
            {
                key.SetValue(valueName, saved);
            }
        }
    }
}
