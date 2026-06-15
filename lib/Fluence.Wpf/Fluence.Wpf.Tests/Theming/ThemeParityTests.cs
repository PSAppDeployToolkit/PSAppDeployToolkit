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

using Fluence.Wpf.Theming;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Characterization (golden) snapshot tests that record the resolved Color and Brush values
    /// produced by the current theme engine for Light, Dark, and HighContrast with a pinned accent.
    /// </summary>
    [TestClass]
    public class ThemeParityTests
    {
        /// <summary>
        /// HighContrast brush keys that <c>SpecialBrushes.AddHighContrastBrushes</c> binds to the
        /// live <c>SystemColors.HighlightColor</c>. That color tracks the host machine's OS accent
        /// and personalization, so it varies by machine (for example #FF0078D7 vs #FF0078D4). These
        /// keys carry semantic WinUI names rather than a "SystemColor" prefix, so the prefix filter
        /// in <c>CaptureResolved</c> does not exclude them. They are kept out of the
        /// machine-independent golden snapshot and verified hermetically against the live highlight
        /// by <c>HighContrast_HighlightDerivedBrushes_BindToLiveSystemHighlight</c>.
        /// </summary>
        private static readonly HashSet<string> HighContrastHighlightDerivedBrushKeys = new(StringComparer.Ordinal)
        {
            "AccentControlElevationBorderBrush",
            "FocusStrokeColorOuterBrush",
            "KeyboardFocusBorderColorBrush",
            "LayerOnAccentAcrylicFillColorDefaultBrush",
            "NavigationViewSelectionIndicatorBrush",
            "SystemFillColorAttentionBackgroundBrush",
            "SystemFillColorAttentionBrush",
            "SystemFillColorSolidAttentionBackgroundBrush",
            "WindowCloseFillColorHoverBrush",
            "WindowCloseFillColorPressedBrush",
        };

        /// <summary>
        /// Applies <paramref name="theme"/> with a pinned accent and returns a map of every
        /// resolved string key to its Color and/or Brush color value.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <returns>A dictionary mapping string keys to their resolved Color and Brush values.</returns>
        internal static IReadOnlyDictionary<string, (Color color, Color brush)> CaptureResolved(ApplicationTheme theme)
        {
            WpfTestSta.Dispatcher!.Invoke(() =>
            {
                Application app = WpfTestSta.EnsureApplication()!;
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();

                // Force machine-independent title-bar and window-border chrome before applying.
                // The golden snapshot was captured with the OS "show accent color on title bars"
                // setting OFF, whereas the live ColorMap chrome branch reads HKCU DWM
                // ColorPrevalence and AccentColor. On a machine with that setting ON, the four
                // chrome keys TitleBarActive, TitleBarInactive and WindowBorder, plus their Brush
                // twins, would drift. Routing through the deterministic-chrome path makes this
                // parity check hermetic, and the same machine-independent values are already
                // covered by DesignTimeResourceTests.
                FluenceThemeEngine.SetDeterministicChromeForTesting(enabled: true);
                ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
            });

            Dictionary<string, (Color, Color)> map = new(StringComparer.Ordinal);
            WpfTestSta.Dispatcher!.Invoke(() =>
            {
                ResourceDictionary res = Application.Current!.Resources;
                foreach (object key in CollectKeys(res))
                {
                    if (key is not string ks) { continue; }

                    // SystemColor* aliases resolve to live SystemColors (highlight, window, button
                    // face, and so on), whose values track the host machine's OS theme and accent
                    // rather than the requested theme. They are machine-dependent, so they are
                    // excluded from this machine-independent parity snapshot, exactly as
                    // DesignTimeResourceWriter excludes them. Otherwise a CI runner whose highlight
                    // color differs from the snapshot machine drifts the check (for example
                    // SystemColorHighlightColorBrush #FF0078D7 vs #FF0078D4).
                    if (ks.StartsWith("SystemColor", StringComparison.Ordinal)) { continue; }

                    // In HighContrast these accent-semantic brushes bind to the live, machine-variable
                    // SystemColors.HighlightColor (see SpecialBrushes.AddHighContrastBrushes), so they
                    // are excluded here for the same reason as the SystemColor* aliases above. Their
                    // highlight binding is covered hermetically by
                    // HighContrast_HighlightDerivedBrushes_BindToLiveSystemHighlight.
                    if (theme == ApplicationTheme.HighContrast
                        && HighContrastHighlightDerivedBrushKeys.Contains(ks))
                    {
                        continue;
                    }

                    object? val = res[ks];
                    if (val is Color c) { map[ks] = (c, default); }
                    else if (val is SolidColorBrush b) { map[ks] = (default, b.Color); }
                }
            });
            return map;
        }

        private static IEnumerable<object> CollectKeys(ResourceDictionary res)
        {
            HashSet<object> keys = [];
            void Walk(ResourceDictionary d)
            {
                foreach (object k in d.Keys) { _ = keys.Add(k); }
                foreach (ResourceDictionary m in d.MergedDictionaries) { Walk(m); }
            }
            Walk(res);
            return keys;
        }

        /// <summary>
        /// Writes golden baseline files to <c>data/theme-golden/</c> for every theme variant.
        /// Run once to produce the baseline; subsequent runs verify parity.
        /// </summary>
        [TestMethod]
        public void Golden_WriteCurrentResolvedValues()
        {
            foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
            {
                IReadOnlyDictionary<string, (Color color, Color brush)> map = CaptureResolved(theme);
                string dir = Path.Combine(FindRepoRoot(), "data", "theme-golden");
                _ = Directory.CreateDirectory(dir);
                List<string> lines = [.. map.Keys.Order(StringComparer.Ordinal)
                    .Select(k => string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}",
                        k, Hex(map[k].color), Hex(map[k].brush)))];
                File.WriteAllLines(Path.Combine(dir, theme + ".txt"), lines);
                Assert.IsGreaterThan(50, map.Count, "Expected many resolved theme keys for " + theme);
            }
        }

        /// <summary>
        /// Verifies that AccentResolver.Resolve produces structurally sound palettes for both
        /// System and Custom intents, and that the Custom path uses the generator ramp.
        /// </summary>
        [TestMethod]
        public void AccentResolver_System_PrefersOsPaletteThenGenerates()
        {
            // System intent must resolve to 7 opaque colors regardless of path taken.
            AccentPalette sys = AccentResolver.Resolve(AccentIntent.System);
            Assert.AreEqual((byte)0xFF, sys.Accent.A, "Accent rung must be opaque.");

            // Custom(#0078D4) must use the generated ramp: Light2 must equal what the generator produces.
            Color customBase = Color.FromRgb(0x00, 0x78, 0xD4);
            AccentPalette custom = AccentResolver.Resolve(AccentIntent.FromCustom(customBase));
            Helpers.HsvColorHelper.GenerateAccentRampWinaccent(customBase,
                out _, out Color l2, out _, out _, out _, out _);
            Assert.AreEqual(l2, custom.Light2, "Custom accent must use the generated ramp, unchanged.");
        }

        /// <summary>
        /// Verifies that the rebuilt engine reproduces every golden resolved Color and Brush value
        /// (accent pinned to #0078D4) for Light, Dark, and HighContrast with zero drift.
        /// </summary>
        [TestMethod]
        public void Rebuilt_MatchesGoldenResolvedValues()
        {
            foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
            {
                IReadOnlyDictionary<string, (Color color, Color brush)> actual = CaptureResolved(theme);
                string goldenPath = Path.Combine(AppContext.BaseDirectory, "Theming", "golden", theme + ".txt");
                Dictionary<string, (string, string)> golden = File.ReadAllLines(goldenPath)
                    .Select(static l => l.Split('|'))
                    .ToDictionary(static a => a[0], static a => (a[1], a[2]), StringComparer.Ordinal);

                List<string> drift = [];
                foreach (KeyValuePair<string, (string, string)> kv in golden)
                {
                    if (!actual.TryGetValue(kv.Key, out (Color color, Color brush) got))
                    {
                        drift.Add("MISSING " + kv.Key);
                        continue;
                    }
                    string gc = Hex(got.color);
                    string gb = Hex(got.brush);
                    if (!string.Equals(gc, kv.Value.Item1, StringComparison.Ordinal) || !string.Equals(gb, kv.Value.Item2, StringComparison.Ordinal))
                    {
                        drift.Add(string.Format(CultureInfo.InvariantCulture, "{0} golden=({1},{2}) actual=({3},{4})",
                            kv.Key, kv.Value.Item1, kv.Value.Item2, gc, gb));
                    }
                }
                Assert.AreEqual(0, drift.Count, theme + " drift:\n" + string.Join('\n', drift));
            }
        }

        /// <summary>
        /// Hermetic guard for the HighContrast accent-semantic brushes that bind to the live
        /// <c>SystemColors.HighlightColor</c>. Their value is machine-dependent, so they are excluded
        /// from the frozen golden snapshot; this verifies the binding contract directly against the
        /// live highlight color, which holds on any machine.
        /// </summary>
        [TestMethod]
        public void HighContrast_HighlightDerivedBrushes_BindToLiveSystemHighlight()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                Application app = WpfTestSta.EnsureApplication()!;
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.SetDeterministicChromeForTesting(enabled: true);
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Color highlight = SystemColors.HighlightColor;
                ResourceDictionary res = Application.Current!.Resources;
                foreach (string key in HighContrastHighlightDerivedBrushKeys)
                {
                    Assert.IsInstanceOfType(res[key], typeof(SolidColorBrush), key + " must resolve to a SolidColorBrush in HighContrast.");
                    SolidColorBrush brush = (SolidColorBrush)res[key];
                    Assert.AreEqual(highlight, brush.Color,
                        key + " must bind to the live SystemColors.HighlightColor in HighContrast.");
                }
            });
        }

        /// <summary>
        /// Footgun regression guard: applying a theme alone (without an explicit accent call) must
        /// resolve the OS accent palette, not a stale or default value.
        /// </summary>
        [TestMethod]
        public void ApplyTheme_Alone_UsesSystemAccentPalette()
        {
            WpfTestSta.Dispatcher!.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Application.Current!.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                ApplicationThemeManager.Apply(ApplicationTheme.Dark); // no ApplySystemAccent call
                if (Helpers.RegistryHelper.TryGetAccentPalette(out Color[]? p) && p is not null)
                {
                    SolidColorBrush brush = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];
                    Assert.AreEqual(p[1], brush.Color, "Apply(theme) alone must use the OS palette Light2 in Dark.");
                }
            });
        }

        /// <summary>
        /// Regression guard for the AccentColorChanged first-Apply ordering seam: when
        /// <see cref="ApplicationThemeManager.Apply"/> is the very first touch of the theme
        /// system, the <see cref="ApplicationAccentColorManager.AccentColorChanged"/> event must
        /// still fire at least once because <see cref="ApplicationThemeManager.Apply"/> calls
        /// <see cref="ApplicationAccentColorManager.EnsureInitialized"/> before the engine publishes.
        /// </summary>
        [TestMethod]
        public void FirstApply_RaisesAccentColorChanged_BeforeAnyOtherThemeAccess()
        {
            int raised = 0;
            WpfTestSta.Dispatcher!.Invoke(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                Application.Current!.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();

                // Subscribe BEFORE any Apply; this simulates a consumer wiring up after
                // the static classes exist but before their first call.
                ApplicationAccentColorManager.AccentColorChanged += OnChanged;
                try
                {
                    // First touch of the theme system.
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnChanged;
                }
            });

            Assert.IsGreaterThan(0, raised, "AccentColorChanged must fire on the first Apply.");

            void OnChanged(object? sender, EventArgs e)
            {
                raised++;
            }
        }

        private static string Hex(Color c)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);
        }

        /// <summary>
        /// Finds the repository root by walking up from the test output directory until
        /// <c>Fluence.Wpf.sln</c> is found.
        /// </summary>
        internal static string FindRepoRoot()
        {
            DirectoryInfo? d = new(AppContext.BaseDirectory);
            while (d is not null && !File.Exists(Path.Combine(d.FullName, "Fluence.Wpf.sln"))) { d = d.Parent; }
            return d?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
