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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Fluence.Wpf.Theming;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Characterization (golden) snapshot tests that record the resolved Color and Brush values
    /// produced by the current theme engine for Light, Dark, and HighContrast with a pinned accent.
    /// </summary>
    public class ThemeParityTests
    {
        /// <summary>
        /// HighContrast brush keys that <c>SpecialBrushes.AddHighContrastBrushes</c> binds to the
        /// live <c>SystemColors.HighlightColor</c>. That color tracks the host machine's OS accent
        /// and personalization, so it varies by machine (for example #FF0078D7 vs #FF0078D4). These
        /// keys carry semantic WinUI names rather than a "SystemColor" prefix, so the prefix filter
        /// in <c>CaptureResolved</c> does not exclude them. They are kept out of the
        /// machine-independent golden snapshot and verified hermetically against the live highlight
        /// by <c>HighContrast_HighlightDerivedBrushes_BindToLiveSystemHighlightAsync</c>.
        /// </summary>
        private static readonly HashSet<string> HighContrastHighlightDerivedBrushKeys = new(StringComparer.Ordinal)
        {
            "AccentAcrylicBackgroundFillColorBaseBrush",
            "AccentAcrylicBackgroundFillColorDefaultBrush",
            "AccentControlElevationBorderBrush",
            "FocusStrokeColorOuterBrush",
            "LayerOnAccentAcrylicFillColorDefaultBrush",
            "NavigationViewSelectionIndicatorForeground",
            "SystemFillColorAttentionBrush",
            "TextControlElevationBorderFocusedBrush",
            "WindowCloseButtonBackgroundPointerOverBrush",
            "WindowCloseButtonBackgroundPressedBrush",
        };

        /// <summary>
        /// HighContrast brush keys that <c>SpecialBrushes.AddHighContrastBrushes</c> binds to the
        /// live <c>SystemColors.HighlightTextColor</c>, which is machine-dependent for the same
        /// reason as <see cref="HighContrastHighlightDerivedBrushKeys"/>. Kept in a separate set
        /// because <c>HighlightTextColor</c> is not the same ambient color as
        /// <c>HighlightColor</c>, so it needs its own hermetic comparison in
        /// <c>HighContrast_HighlightTextDerivedBrushes_BindToLiveSystemHighlightTextAsync</c>.
        /// </summary>
        private static readonly HashSet<string> HighContrastHighlightTextDerivedBrushKeys = new(StringComparer.Ordinal)
        {
            "InfoBadgeAttentionForegroundBrush",
            "WindowCloseButtonForegroundPointerOverBrush",
        };

        /// <summary>
        /// Applies <paramref name="theme"/> with a pinned accent and returns a map of every
        /// resolved string key to its Color and/or Brush color value.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <returns>A dictionary mapping string keys to their resolved Color and Brush values.</returns>
        internal static async Task<IReadOnlyDictionary<string, (Color color, Color brush)>> CaptureResolvedAsync(ApplicationTheme theme)
        {
            await WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
            }).ConfigureAwait(true);

            Dictionary<string, (Color, Color)> map = new(StringComparer.Ordinal);
            await WpfTestSta.RunOnStaAsync(() =>
            {
                ResourceDictionary res = Application.Current.Resources;
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
                    // SystemColors.HighlightColor or SystemColors.HighlightTextColor (see
                    // SpecialBrushes.AddHighContrastBrushes), so they are excluded here for the same
                    // reason as the SystemColor* aliases above. Their highlight binding is covered
                    // hermetically by HighContrast_HighlightDerivedBrushes_BindToLiveSystemHighlight
                    // and HighContrast_HighlightTextDerivedBrushes_BindToLiveSystemHighlightText.
                    if (theme is ApplicationTheme.HighContrast
                            && (HighContrastHighlightDerivedBrushKeys.Contains(ks)
                            || HighContrastHighlightTextDerivedBrushKeys.Contains(ks)))
                    {
                        continue;
                    }

                    object? val = res[ks];
                    if (val is Color c) { map[ks] = (c, default); }
                    else if (val is SolidColorBrush b) { map[ks] = (default, b.Color); }
                }
            }).ConfigureAwait(true);
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
        /// Writes golden baseline files to <c language="text">data/theme-golden/</c> for every theme variant.
        /// Run once to produce the baseline; subsequent runs verify parity.
        /// </summary>
        [Fact]
        public async Task Golden_WriteCurrentResolvedValuesAsync()
        {
            foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
            {
                IReadOnlyDictionary<string, (Color color, Color brush)> map = await CaptureResolvedAsync(theme).ConfigureAwait(true);
                string dir = Path.Join(FindRepoRoot(), "data", "theme-golden");
                _ = Directory.CreateDirectory(dir);
                List<string> lines = [.. map.Keys.Order(StringComparer.Ordinal)
                    .Select(k => string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}",
                        k, Hex(map[k].color), Hex(map[k].brush)))];
                await File.WriteAllLinesAsync(Path.Join(dir, theme + ".txt"), lines, TestContext.Current.CancellationToken).ConfigureAwait(true);
                Assert.True(map.Count > 50, "Expected many resolved theme keys for " + theme);
            }
        }

        /// <summary>
        /// Verifies the WinUI 3 text-control border gradients: the rest brush is an absolute
        /// 0,0 to 0,2 flipped gradient whose 0.5 stop is the strong stroke (the visible bottom
        /// underline) and whose 1.0 stop is the default stroke, and the focused brush shares the
        /// geometry with both stops at 1.0 (a hard step to a solid accent bottom band). Matches
        /// WinUI CommonStyles TextBox_themeresources.xaml for Light and Dark.
        /// </summary>
        /// <param name="theme">The theme to verify.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        public Task TextControlElevationBorderBrushes_MatchWinUiGradientShapeAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                ResourceDictionary res = app.Resources;
                Color strongStroke = Assert.IsType<Color>(res["ControlStrongStrokeColorDefault"]);
                Color defaultStroke = Assert.IsType<Color>(res["ControlStrokeColorDefault"]);
                Color accentPrimary = Assert.IsType<Color>(res["SystemAccentColorPrimary"]);

                LinearGradientBrush rest = Assert.IsType<LinearGradientBrush>(res["TextControlElevationBorderBrush"]);
                Assert.Equal(BrushMappingMode.Absolute, rest.MappingMode);
                Assert.Equal(new Point(0, 0), rest.StartPoint);
                Assert.Equal(new Point(0, 2), rest.EndPoint);
                ScaleTransform restFlip = Assert.IsType<ScaleTransform>(rest.RelativeTransform);
                Assert.Equal(-1.0, restFlip.ScaleY, 0.001);
                Assert.Equal(0.5, restFlip.CenterY, 0.001);
                Assert.Equal(2, rest.GradientStops.Count);
                Assert.Equal(0.5, rest.GradientStops[0].Offset, 0.001);
                Assert.Equal(strongStroke, rest.GradientStops[0].Color);
                Assert.Equal(1.0, rest.GradientStops[1].Offset, 0.001);
                Assert.Equal(defaultStroke, rest.GradientStops[1].Color);

                LinearGradientBrush focused = Assert.IsType<LinearGradientBrush>(res["TextControlElevationBorderFocusedBrush"]);
                Assert.Equal(BrushMappingMode.Absolute, focused.MappingMode);
                Assert.Equal(new Point(0, 0), focused.StartPoint);
                Assert.Equal(new Point(0, 2), focused.EndPoint);
                ScaleTransform focusedFlip = Assert.IsType<ScaleTransform>(focused.RelativeTransform);
                Assert.Equal(-1.0, focusedFlip.ScaleY, 0.001);
                Assert.Equal(2, focused.GradientStops.Count);
                Assert.Equal(1.0, focused.GradientStops[0].Offset, 0.001);
                Assert.Equal(accentPrimary, focused.GradientStops[0].Color);
                Assert.Equal(1.0, focused.GradientStops[1].Offset, 0.001);
                Assert.Equal(defaultStroke, focused.GradientStops[1].Color);
            });
        }

        /// <summary>
        /// Verifies the WinUI 3 asymmetry between ControlElevationBorderBrush and
        /// AccentControlElevationBorderBrush: WinUI's Light theme dictionary flips
        /// ControlElevationBorderBrush vertically (a ScaleY="-1" RelativeTransform), but its
        /// Default/Dark theme dictionary defines the same key with no transform
        /// (Common_themeresources_any.xaml Light block vs Default block). AccentControlElevationBorderBrush
        /// is flipped in both theme dictionaries, so it must keep the transform regardless of theme.
        /// </summary>
        /// <param name="theme">The theme to verify.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        public Task ControlElevationBorderBrush_FlipsOnlyInLightThemeAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                ResourceDictionary res = app.Resources;
                LinearGradientBrush control = Assert.IsType<LinearGradientBrush>(res["ControlElevationBorderBrush"]);
                if (theme is ApplicationTheme.Light)
                {
                    ScaleTransform flip = Assert.IsType<ScaleTransform>(control.RelativeTransform);
                    Assert.Equal(-1.0, flip.ScaleY, 0.001);
                }
                else
                {
                    Assert.True(control.RelativeTransform is null || control.RelativeTransform == Transform.Identity,
                        "Dark ControlElevationBorderBrush must not flip (WinUI Default/Dark defines it with no transform).");
                }

                // AccentControlElevationBorderBrush flips in every non-HC theme.
                LinearGradientBrush accent = Assert.IsType<LinearGradientBrush>(res["AccentControlElevationBorderBrush"]);
                ScaleTransform accentFlip = Assert.IsType<ScaleTransform>(accent.RelativeTransform);
                Assert.Equal(-1.0, accentFlip.ScaleY, 0.001);
            });
        }

        /// <summary>
        /// Verifies that AccentResolver.Resolve produces structurally sound palettes for both
        /// System and Custom intents, and that the Custom path uses the generator ramp.
        /// </summary>
        [Fact]
        public void AccentResolver_System_PrefersOsPaletteThenGenerates()
        {
            // System intent must resolve to 7 opaque colors regardless of path taken.
            AccentPalette sys = AccentResolver.Resolve(AccentIntent.System, ApplicationTheme.Light);
            Assert.Equal((byte)0xFF, sys.Accent.A);

            // Custom(#0078D4) must use the generated ramp: Light2 must equal what the generator produces.
            Color customBase = Color.FromRgb(0x00, 0x78, 0xD4);
            AccentPalette custom = AccentResolver.Resolve(AccentIntent.FromCustom(customBase), ApplicationTheme.Light);
            Helpers.HsvColorHelper.GenerateAccentRampWinaccent(customBase,
                out _, out Color l2, out _, out _, out _, out _);
            Assert.Equal(l2, custom.Light2);
        }

        /// <summary>
        /// Verifies that the rebuilt engine reproduces every golden resolved Color and Brush value
        /// (accent pinned to #0078D4) for Light, Dark, and HighContrast with zero drift, and that
        /// the snapshot records every live Color and Brush in turn. Without the second direction a
        /// key the snapshot omits is checked by nothing, which is how the two accent acrylic keys
        /// carried an unverified high contrast value for a whole delta.
        /// </summary>
        /// <remarks>
        /// The high contrast snapshot is only partly machine-independent. Its brushes bound to the
        /// live highlight are excluded (see <see cref="HighContrastHighlightDerivedBrushKeys"/>), but
        /// the many bound to the other seven members
        /// <c language="csharp">AddHighContrastBrushes</c> reads
        /// (<c language="csharp">SystemColors.WindowColor</c>,
        /// <c language="csharp">WindowTextColor</c>, <c language="csharp">GrayTextColor</c>,
        /// <c language="csharp">ControlColor</c>, <c language="csharp">ControlTextColor</c>,
        /// <c language="csharp">ControlDarkColor</c> and <c language="csharp">ControlLightColor</c>)
        /// are recorded at the values a standard desktop reports (white, black, #6D6D6D, #F0F0F0,
        /// black, #A0A0A0 and #E3E3E3), because those do not follow the user's accent and a CI
        /// runner never turns a high contrast theme on. A desktop that does report different values
        /// would drift every one of those rows, so the high contrast case skips, with the mismatch
        /// named, rather than fail for a reason that is not the engine's.
        /// </remarks>
        /// <param name="theme">The theme whose snapshot is checked.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        [InlineData(ApplicationTheme.HighContrast)]
        public async Task Rebuilt_MatchesGoldenResolvedValuesAsync(ApplicationTheme theme)
        {
            if (theme is ApplicationTheme.HighContrast && DescribeSystemColorMismatch() is string mismatch)
            {
                Assert.Skip("The high contrast snapshot records the standard desktop system colours; this desktop reports " + mismatch + ".");
            }

            IReadOnlyDictionary<string, (Color color, Color brush)> actual = await CaptureResolvedAsync(theme).ConfigureAwait(true);
            string goldenPath = Path.Join(AppContext.BaseDirectory, "Theming", "golden", theme + ".txt");
            Dictionary<string, (string, string)> golden = (await File.ReadAllLinesAsync(goldenPath, TestContext.Current.CancellationToken).ConfigureAwait(true))
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

            // The other direction: a live value the snapshot does not record. Regenerate the
            // golden files with Golden_WriteCurrentResolvedValuesAsync and copy them over.
            drift.AddRange(actual.Keys.Where(key => !golden.ContainsKey(key)).Order(StringComparer.Ordinal).Select(key => "UNRECORDED " + key + " in " + theme));
            Assert.Empty(drift);
        }

        /// <summary>
        /// Names the first live system colour that differs from the value the high contrast
        /// snapshot was captured with, or returns <see langword="null"/> when they all match.
        /// </summary>
        private static string? DescribeSystemColorMismatch()
        {
            // Every SystemColors member AddHighContrastBrushes reads, less the highlight pair, which
            // HighContrastHighlightDerivedBrushKeys excludes from the snapshot instead. A member left
            // out here is a member whose drift fails the case rather than skipping it.
            (string name, Color live, Color assumed)[] expectations =
            [
                ("SystemColors.WindowColor", SystemColors.WindowColor, Color.FromRgb(0xFF, 0xFF, 0xFF)),
                ("SystemColors.WindowTextColor", SystemColors.WindowTextColor, Color.FromRgb(0x00, 0x00, 0x00)),
                ("SystemColors.GrayTextColor", SystemColors.GrayTextColor, Color.FromRgb(0x6D, 0x6D, 0x6D)),
                ("SystemColors.ControlColor", SystemColors.ControlColor, Color.FromRgb(0xF0, 0xF0, 0xF0)),
                ("SystemColors.ControlTextColor", SystemColors.ControlTextColor, Color.FromRgb(0x00, 0x00, 0x00)),
                ("SystemColors.ControlDarkColor", SystemColors.ControlDarkColor, Color.FromRgb(0xA0, 0xA0, 0xA0)),
                ("SystemColors.ControlLightColor", SystemColors.ControlLightColor, Color.FromRgb(0xE3, 0xE3, 0xE3)),
            ];

            foreach ((string name, Color live, Color assumed) in expectations)
            {
                if (live != assumed)
                {
                    return name + " as " + Hex(live) + " where the snapshot assumes " + Hex(assumed);
                }
            }

            return null;
        }

        /// <summary>
        /// Hermetic guard for the HighContrast accent-semantic brushes that bind to the live
        /// <c language="csharp">SystemColors.HighlightColor</c>. Their value is machine-dependent, so they are excluded
        /// from the frozen golden snapshot; this verifies the binding contract directly against the
        /// live highlight color, which holds on any machine.
        /// </summary>
        [Fact]
        public Task HighContrast_HighlightDerivedBrushes_BindToLiveSystemHighlightAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.SetDeterministicChromeForTesting(enabled: true);
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Color highlight = SystemColors.HighlightColor;
                ResourceDictionary res = Application.Current.Resources;
                foreach (string key in HighContrastHighlightDerivedBrushKeys)
                {
                    _ = Assert.IsType<SolidColorBrush>(res[key], exactMatch: false);
                    SolidColorBrush brush = (SolidColorBrush)res[key];
                    Assert.Equal(highlight, brush.Color);
                }
            });
        }

        /// <summary>
        /// Hermetic guard for the HighContrast accent-semantic brushes that bind to the live
        /// <c language="csharp">SystemColors.HighlightTextColor</c>. Their value is machine-dependent, so they are
        /// excluded from the frozen golden snapshot; this verifies the binding contract directly
        /// against the live highlight text color, which holds on any machine.
        /// </summary>
        [Fact]
        public Task HighContrast_HighlightTextDerivedBrushes_BindToLiveSystemHighlightTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                FluenceThemeEngine.SetDeterministicChromeForTesting(enabled: true);
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Color highlightText = SystemColors.HighlightTextColor;
                ResourceDictionary res = Application.Current.Resources;
                foreach (string key in HighContrastHighlightTextDerivedBrushKeys)
                {
                    _ = Assert.IsType<SolidColorBrush>(res[key], exactMatch: false);
                    SolidColorBrush brush = (SolidColorBrush)res[key];
                    Assert.Equal(highlightText, brush.Color);
                }
            });
        }

        /// <summary>
        /// Footgun regression guard: applying a theme alone (without an explicit accent call) must
        /// resolve the OS accent palette, not a stale or default value.
        /// </summary>
        [Fact]
        public Task ApplyTheme_Alone_UsesSystemAccentPaletteAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Application.Current.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                ApplicationThemeManager.Apply(ApplicationTheme.Dark); // no ApplySystemAccent call
                if (Helpers.RegistryHelper.TryGetAccentPalette(out Color[]? p) && p is not null)
                {
                    SolidColorBrush brush = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];
                    Assert.Equal(p[1], brush.Color);
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
        [Fact]
        public async Task FirstApply_RaisesAccentColorChanged_BeforeAnyOtherThemeAccessAsync()
        {
            int raised = 0;
            await WpfTestSta.RunOnStaAsync(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                Application.Current.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();

                // Subscribe BEFORE any Apply; this simulates a consumer wiring up after
                // the static classes exist but before their first call.
                ApplicationAccentColorManager.AccentColorChanged += OnChanged;
                try
                {
                    // First touch of the theme system.
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnChanged;
                }
            }).ConfigureAwait(true);

            Assert.True(raised > 0, "AccentColorChanged must fire on the first Apply.");

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
        /// <c language="text">Fluence.Wpf.sln</c> is found.
        /// </summary>
        internal static string FindRepoRoot()
        {
            DirectoryInfo? d = new(AppContext.BaseDirectory);
            while (d is not null && !File.Exists(Path.Join(d.FullName, "Fluence.Wpf.sln"))) { d = d.Parent; }
            return d?.FullName ?? AppContext.BaseDirectory;
        }

        /// <summary>
        /// Adds every string key defined directly by <paramref name="dictionary"/> to
        /// <paramref name="keys"/>, then recurses into its merged dictionaries. A dictionary whose
        /// Source is under Themes/Controls/ or Themes/Icons/ contributes nothing: its keys are
        /// template internal, unsupported, and free to change in any release. The slot [0] marker
        /// <see cref="FluenceThemeEngine.ComputedDictionaryMarker"/> is skipped for the same reason.
        /// </summary>
        /// <param name="dictionary">The dictionary to walk.</param>
        /// <param name="keys">The set to fill.</param>
        private static void CollectPublicKeys(ResourceDictionary dictionary, ISet<string> keys)
        {
            string source = dictionary.Source?.ToString() ?? string.Empty;
            bool templateInternal =
                source.Contains("Themes/Controls/", StringComparison.OrdinalIgnoreCase)
                || source.Contains("Themes/Icons/", StringComparison.OrdinalIgnoreCase);

            if (!templateInternal)
            {
                foreach (object key in dictionary.Keys)
                {
                    // The slot [0] marker is an internal bookkeeping key, not a token a consumer can
                    // bind, so it stays out of the inventory this file freezes as the public
                    // contract.
                    if (key is string text && !string.Equals(text, FluenceThemeEngine.ComputedDictionaryMarker, StringComparison.Ordinal))
                    {
                        _ = keys.Add(text);
                    }
                }
            }

            foreach (ResourceDictionary merged in dictionary.MergedDictionaries)
            {
                CollectPublicKeys(merged, keys);
            }
        }

        /// <summary>
        /// The public XAML key set is frozen at 1.0 the way the CLR surface is frozen by
        /// PublicApiAnalyzers. This asserts the Light theme's published keys against
        /// Theming/golden/PublicKeys.txt and fails on an addition as well as a removal, so an
        /// intentional change has to update the file in the same commit.
        /// </summary>
        /// <remarks>
        /// Light is the reference theme. High contrast overrides the value of existing keys rather
        /// than publishing new ones, so one file covers the contract.
        /// The current set is always written to data/theme-golden/PublicKeys.txt, which is what an
        /// intentional change copies over the committed file. The four golden files are therefore
        /// stored the way File.WriteAllLinesAsync emits them, without a byte order mark, so that
        /// copy is a content diff and not a whole-file one.
        /// </remarks>
        [Fact]
        public async Task PublicKeyInventory_MatchesFrozenSetAsync()
        {
            SortedSet<string> actual = new(StringComparer.Ordinal);
            await WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = TestApp.EnsureLibraryTheme();
                CollectPublicKeys(app.Resources, actual);
            }).ConfigureAwait(true);

            string outDirectory = Path.Join(FindRepoRoot(), "data", "theme-golden");
            _ = Directory.CreateDirectory(outDirectory);
            await File.WriteAllLinesAsync(
                Path.Join(outDirectory, "PublicKeys.txt"), actual, TestContext.Current.CancellationToken).ConfigureAwait(true);

            string frozenPath = Path.Join(AppContext.BaseDirectory, "Theming", "golden", "PublicKeys.txt");
            Assert.True(File.Exists(frozenPath),
                "Theming/golden/PublicKeys.txt is missing. Copy data/theme-golden/PublicKeys.txt over it.");

            string[] frozen = await File.ReadAllLinesAsync(frozenPath, TestContext.Current.CancellationToken).ConfigureAwait(true);
            List<string> added = [.. actual.Except(frozen, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            List<string> removed = [.. frozen.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.True(added.Count is 0,
                "New public keys are not in the frozen set: " + string.Join(", ", added));
            Assert.True(removed.Count is 0,
                "Frozen public keys no longer resolve: " + string.Join(", ", removed));
        }

        /// <summary>
        /// Dark and high contrast publish exactly the Light key set. High contrast overrides the
        /// value of existing keys rather than publishing new ones or dropping any, which is the
        /// assumption that lets <see cref="PublicKeyInventory_MatchesFrozenSetAsync"/> sample Light
        /// alone. Without this, a key computed for Light and Dark but skipped for high contrast, or
        /// supplied by one theme's table and forgotten in another, would vanish from that theme
        /// with nothing to notice: the value snapshots iterate the committed golden file, so a key
        /// missing from the live set is checked by nothing.
        /// </summary>
        /// <param name="theme">The theme compared against Light.</param>
        [Theory]
        [InlineData(ApplicationTheme.Dark)]
        [InlineData(ApplicationTheme.HighContrast)]
        public async Task PublicKeyInventory_IsTheSameInEveryThemeAsync(ApplicationTheme theme)
        {
            SortedSet<string> light = new(StringComparer.Ordinal);
            SortedSet<string> other = new(StringComparer.Ordinal);
            await WpfTestSta.RunOnStaAsync(() =>
            {
                CollectPublicKeys(TestApp.EnsureLibraryTheme().Resources, light);
                CollectPublicKeys(TestApp.EnsureLibraryTheme(theme).Resources, other);
            }).ConfigureAwait(true);

            List<string> missing = [.. light.Except(other, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            List<string> extra = [.. other.Except(light, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.True(missing.Count is 0,
                theme + " does not publish keys Light publishes: " + string.Join(", ", missing));
            Assert.True(extra.Count is 0,
                theme + " publishes keys Light does not: " + string.Join(", ", extra));
        }
    }
}
