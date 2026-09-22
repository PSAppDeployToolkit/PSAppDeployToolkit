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
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public class DictionaryStabilityTests : IAsyncLifetime
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
            GC.SuppressFinalize(this);
            return default;
        }

        /// <summary>
        /// Twenty Light and Dark switches must not grow the merged dictionary collection, whether or
        /// not a distinct custom accent is pinned before the switching loop. Pinning a color far from
        /// the default Windows blue is a genuine accent-intent and ramp transition that clears the
        /// redundant-publish gate on any host, unlike re-pinning the System intent (already the
        /// resting state after test reset), which the gate would swallow as a no-op.
        /// </summary>
        /// <param name="pinDistinctAccentFirst">Whether to pin a non-default custom accent before switching.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task RepeatedThemeSwitches_NoDictionaryAccumulationAsync(bool pinDistinctAccentFirst)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                if (pinDistinctAccentFirst)
                {
                    // Deliberately far from the default Windows blue (#0078D4): guarantees a real
                    // ramp transition past the redundant-publish gate on any host, unlike re-pinning
                    // the System intent that Apply(Light, ...) above already resolved.
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xC8, 0x1E, 0x7A));
                }

                int baselineCount = app.Resources.MergedDictionaries.Count;

                for (int i = 0; i < 10; i++)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                }

                int finalCount = app.Resources.MergedDictionaries.Count;
                Assert.Equal(baselineCount, finalCount);
            });
        }

        [Fact]
        public Task ReInitialization_DropsThePreviouslyPublishedComputedDictionaryAsync()
        {
            // A computed dictionary has no Source, so it cannot be recognised by pack URI the way
            // Typography and Generic are. Seeding the slots again (which every test isolation reset
            // forces) used to insert a fresh one at [0] and leave the previous one further down the
            // list, where WPF's last-wins merge order let it answer lookups the new one owned. On a
            // machine that had published high contrast at some point, later Light applies then
            // resolved opaque high contrast tokens.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                Assert.Equal(3, app.Resources.MergedDictionaries.Count);

                // AcrylicBackgroundFillColorDefault is opaque in every theme (it is the WinUI
                // AcrylicBrush FallbackColor, used as a solid plate), so alpha can no longer
                // distinguish "still resolving HC's stale value" from "resolving Light's value".
                // Compare against Light's own RGB instead, which HC's black token cannot satisfy.
                Color acrylic = Assert.IsType<Color>(app.TryFindResource("AcrylicBackgroundFillColorDefault"));
                Assert.Equal(Color.FromRgb(0xF9, 0xF9, 0xF9), acrylic);
            });
        }

        /// <summary>
        /// FluenceWindow.GetLegacyAcrylicTintColor forces a fixed 0xF0 alpha onto the now-opaque
        /// AcrylicBackgroundFillColorDefault token's RGB for the Windows 10 legacy acrylic accent
        /// policy, since the token itself no longer carries a translucent alpha (it is the WinUI
        /// AcrylicBrush FallbackColor, used opaque everywhere else as a plate color).
        /// </summary>
        /// <param name="theme">The theme to apply before reading the tint.</param>
        /// <param name="r">The expected red channel of the token's opaque RGB.</param>
        /// <param name="g">The expected green channel of the token's opaque RGB.</param>
        /// <param name="b">The expected blue channel of the token's opaque RGB.</param>
        [Theory]
        [InlineData(ApplicationTheme.Light, (byte)0xF9, (byte)0xF9, (byte)0xF9)]
        [InlineData(ApplicationTheme.Dark, (byte)0x2C, (byte)0x2C, (byte)0x2C)]
        public Task LegacyAcrylicTintColor_ForcesFixedAlphaOntoOpaqueTokenAsync(ApplicationTheme theme, byte r, byte g, byte b)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);

                FluenceWindow window = new();
                try
                {
                    MethodInfo method = Assert.IsType<MethodInfo>(
                        typeof(FluenceWindow).GetMethod("GetLegacyAcrylicTintColor", BindingFlags.Instance | BindingFlags.NonPublic),
                        exactMatch: false);
                    Color tint = Assert.IsType<Color>(method.Invoke(window, parameters: null));

                    Assert.Equal(Color.FromArgb(0xF0, r, g, b), tint);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ThemeSlotIsReusedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                int countAfterFirst = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                int countAfterSecond = app.Resources.MergedDictionaries.Count;

                Assert.Equal(countAfterFirst, countAfterSecond);
            });
        }

        [Fact]
        public Task AllThemeVariants_SameSlotCountAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                int lightCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                int darkCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                int hcCount = app.Resources.MergedDictionaries.Count;

                Assert.Equal(lightCount, darkCount);
                Assert.Equal(darkCount, hcCount);
            });
        }

        [Fact]
        public Task FirstApply_LoadsThreeDictionariesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                Assert.Equal(3, app.Resources.MergedDictionaries.Count);
            });
        }

        [Fact]
        public Task Apply_UsesThreeSlots_ReplacesComputedSlotOnChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                Collection<ResourceDictionary> dicts = app.Resources.MergedDictionaries;
                Assert.Equal(3, dicts.Count);

                object slot0 = dicts[0];
                object typography = dicts[1];
                object generic = dicts[2];

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);

                Assert.NotSame(slot0, dicts[0]);
                Assert.Same(typography, dicts[1]);
                Assert.Same(generic, dicts[2]);
            });
        }

        [Fact]
        public Task AccentUpdate_DoesNotChangeDictionaryCountAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                int countBefore = app.Resources.MergedDictionaries.Count;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                int countAfter = app.Resources.MergedDictionaries.Count;

                Assert.Equal(countBefore, countAfter);
            });
        }

        [Fact]
        public Task AllBrushKeys_Resolve_AfterLightDarkHcCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);

                string[] keyBrushNames =
                [
                    "TextFillColorPrimaryBrush",
                    "AccentFillColorDefaultBrush",
                    "SubtleFillColorSecondaryBrush",
                    "ControlStrokeColorDefaultBrush",
                    "CardBackgroundFillColorDefaultBrush",
                ];

                foreach (string? key in keyBrushNames)
                {
                    _ = Assert.IsType<Brush>(app.Resources[key], exactMatch: false);
                }
            });
        }

        [Fact]
        public Task InitialApply_SlotsAreComputedTypographyGeneric_InOrderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                Collection<ResourceDictionary> dictionaries = app.Resources.MergedDictionaries;

                Assert.Equal(3, dictionaries.Count);

                // Slot [0] is the computed dictionary: no Source of its own, populated by the engine.
                Assert.Null(dictionaries[0].Source);
                Assert.True(dictionaries[0].Count > 0, "Computed slot [0] should hold resolved entries.");

                Uri typographySource = Assert.IsType<Uri>(dictionaries[1].Source);
                Assert.True(typographySource.OriginalString.Contains("Typography", StringComparison.OrdinalIgnoreCase),
                    "Slot [1] Source should be Typography.xaml, but was " + typographySource.OriginalString);

                Uri genericSource = Assert.IsType<Uri>(dictionaries[2].Source);
                Assert.True(genericSource.OriginalString.Contains("Generic", StringComparison.OrdinalIgnoreCase),
                    "Slot [2] Source should be Generic.xaml, but was " + genericSource.OriginalString);
            });
        }
    }
}
