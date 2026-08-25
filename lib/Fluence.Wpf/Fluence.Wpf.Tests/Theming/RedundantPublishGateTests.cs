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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Theming;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Covers the redundant-publish gate in <see cref="FluenceThemeEngine"/>: an Apply whose
    /// computed output is identical to the last published one must not rebuild slot [0], must not
    /// replace it, and must not raise the publish events, while an Apply that changes anything the
    /// caller can observe still does.
    /// </summary>
    public class RedundantPublishGateTests : IAsyncLifetime
    {
        private static readonly Color PinnedAccent = Color.FromRgb(0x00, 0x78, 0xD4);
        private static readonly Color OtherAccent = Color.FromRgb(0xFF, 0x88, 0x00);

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
        /// Two identical consecutive Apply calls must publish once. The second call is the shape a
        /// duplicate ImmersiveColorSet broadcast takes: same theme, same accent, same backdrop.
        /// </summary>
        [Fact]
        public Task IdenticalConsecutiveApplies_PublishOnceAndKeepSlotZeroInstanceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();

                int changedCount = 0;
                void OnChanged(object? sender, ThemeChangedEventArgs e) { changedCount++; }

                ApplicationThemeManager.Changed += OnChanged;
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                    ResourceDictionary afterFirst = dicts[0];

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                    ResourceDictionary afterSecond = dicts[0];

                    Assert.Equal(1, changedCount);
                    Assert.Same(afterFirst, afterSecond);
                    Assert.Equal(3, dicts.Count);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= OnChanged;
                }
            });
        }

        /// <summary>
        /// A real theme change must still rebuild and replace slot [0] and raise Changed each time.
        /// </summary>
        [Fact]
        public Task AlternatingThemes_PublishEveryTimeAndReplaceSlotZeroAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();
                ResourceDictionary seeded = dicts[0];

                int changedCount = 0;
                void OnChanged(object? sender, ThemeChangedEventArgs e) { changedCount++; }

                ApplicationThemeManager.Changed += OnChanged;
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                    ResourceDictionary afterDark = dicts[0];

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                    ResourceDictionary afterLight = dicts[0];

                    Assert.Equal(2, changedCount);
                    Assert.NotSame(seeded, afterDark);
                    Assert.NotSame(afterDark, afterLight);
                    Assert.Equal(3, dicts.Count);
                    Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= OnChanged;
                }
            });
        }

        /// <summary>
        /// Changing the accent intent to a different seed changes the whole ramp, so the gate must
        /// let the publish through.
        /// </summary>
        [Fact]
        public Task DifferentCustomAccent_RepublishesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();
                ResourceDictionary before = dicts[0];

                int accentCount = 0;
                void OnAccentColorChanged(object? sender, EventArgs e) { accentCount++; }

                ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
                try
                {
                    ApplicationAccentColorManager.ApplyCustomAccent(OtherAccent);

                    Assert.Equal(1, accentCount);
                    Assert.NotSame(before, dicts[0]);
                    Assert.Equal(OtherAccent, ApplicationAccentColorManager.SystemAccentColor);
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
                }
            });
        }

        /// <summary>
        /// Re-applying the accent seed that is already pinned produces an identical ramp, so the
        /// gate must suppress both the rebuild and AccentColorChanged.
        /// </summary>
        [Fact]
        public Task SameCustomAccentTwice_DoesNotRepublishAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();
                ResourceDictionary before = dicts[0];

                int accentCount = 0;
                void OnAccentColorChanged(object? sender, EventArgs e) { accentCount++; }

                ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
                try
                {
                    ApplicationAccentColorManager.ApplyCustomAccent(PinnedAccent);

                    Assert.Equal(0, accentCount);
                    Assert.Same(before, dicts[0]);
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
                }
            });
        }

        /// <summary>
        /// Same theme, different backdrop. No computed Color or brush depends on the backdrop, so
        /// slot [0] must be left alone; the requested backdrop is still user-observable state, so
        /// CurrentBackdrop updates and Changed fires. This is the documented split between "the
        /// computed output moved" and "the caller's request moved".
        /// </summary>
        [Fact]
        public Task SameThemeDifferentBackdrop_FiresChangedWithoutRepublishingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();
                ResourceDictionary before = dicts[0];

                int changedCount = 0;
                void OnChanged(object? sender, ThemeChangedEventArgs e) { changedCount++; }

                ApplicationThemeManager.Changed += OnChanged;
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.Mica, updateAccent: false);

                    Assert.Equal(1, changedCount);
                    Assert.Same(before, dicts[0]);
                    Assert.Equal(BackdropType.Mica, ApplicationThemeManager.CurrentBackdrop);

                    // A second call with the same backdrop is redundant in both respects.
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.Mica, updateAccent: false);

                    Assert.Equal(1, changedCount);
                    Assert.Same(before, dicts[0]);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= OnChanged;
                }
            });
        }

        /// <summary>
        /// The SystemThemeWatcher path: an OS settings broadcast under a pinned (non-Auto) theme
        /// re-enters through <see cref="ApplicationThemeManager.Apply"/> with an unchanged request.
        /// When a fingerprint input moved anyway (the OS accent palette, the Settings "Transparency
        /// effects" toggle), the resulting publish must raise Changed even though the requested
        /// theme and backdrop did not move, because <c>FluenceWindow</c> re-applies its backdrop
        /// only from Changed. The moved input is simulated by re-pointing the accent intent without
        /// an apply, so exactly one publish-relevant input changes between two identical requests.
        /// </summary>
        [Fact]
        public Task UnchangedRequest_PublishOnlyChange_StillRaisesChangedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();
                ResourceDictionary before = dicts[0];

                int changedCount = 0;
                void OnChanged(object? sender, ThemeChangedEventArgs e) { changedCount++; }

                FluenceThemeEngine.SetAccentIntent(AccentIntent.FromCustom(OtherAccent));
                ApplicationThemeManager.Changed += OnChanged;
                try
                {
                    ApplicationThemeManager.Apply(
                        ApplicationThemeManager.CurrentTheme,
                        ApplicationThemeManager.CurrentBackdrop,
                        updateAccent: false);

                    Assert.Equal(1, changedCount);
                    Assert.NotSame(before, dicts[0]);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= OnChanged;
                }
            });
        }

        /// <summary>
        /// ResetForTesting must clear the stored fingerprint so the next Apply re-seeds the three
        /// slots from scratch instead of believing the previous dictionary is still current.
        /// </summary>
        [Fact]
        public Task ResetForTesting_ForcesTheNextApplyToPublishAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();
                ResourceDictionary before = dicts[0];

                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                Collection<ResourceDictionary> after = Application.Current.Resources.MergedDictionaries;
                Assert.Equal(3, after.Count);
                Assert.NotSame(before, after[0]);
                _ = Assert.IsType<Color>(after[0]["TextFillColorPrimary"]);
            });
        }

        /// <summary>
        /// If something replaces slot [0] behind the engine's back, an otherwise redundant Apply
        /// must republish: the fingerprint describes output that is no longer installed.
        /// </summary>
        [Fact]
        public Task SlotZeroReplacedExternally_ForcesRepublishAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Collection<ResourceDictionary> dicts = SeedPinnedLightTheme();

                ResourceDictionary foreign = [];
                dicts[0] = foreign;

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                Assert.Equal(3, dicts.Count);
                Assert.NotSame(foreign, dicts[0]);
                _ = Assert.IsType<Color>(dicts[0]["TextFillColorPrimary"]);
            });
        }

        // ------------------------------------------------------------------ fingerprint comparer --

        /// <summary>
        /// Two maps with the same keys and values must compare equal regardless of insertion order.
        /// </summary>
        [Fact]
        public void ColorMapsEqual_EqualMaps_ReturnsTrue()
        {
            Dictionary<string, Color> left = new(StringComparer.Ordinal)
            {
                ["A"] = Colors.Red,
                ["B"] = Color.FromArgb(0x80, 0x00, 0x80, 0xFF),
            };
            Dictionary<string, Color> right = new(StringComparer.Ordinal)
            {
                ["B"] = Color.FromArgb(0x80, 0x00, 0x80, 0xFF),
                ["A"] = Colors.Red,
            };

            Assert.True(PublishFingerprint.ColorMapsEqual(left, right));
        }

        /// <summary>
        /// A single differing value, including an alpha-only difference, must compare unequal.
        /// </summary>
        [Fact]
        public void ColorMapsEqual_DifferingValue_ReturnsFalse()
        {
            Dictionary<string, Color> left = new(StringComparer.Ordinal)
            {
                ["A"] = Color.FromArgb(0xFF, 0x10, 0x20, 0x30),
            };
            Dictionary<string, Color> right = new(StringComparer.Ordinal)
            {
                ["A"] = Color.FromArgb(0xFE, 0x10, 0x20, 0x30),
            };

            Assert.False(PublishFingerprint.ColorMapsEqual(left, right));
        }

        /// <summary>
        /// A differing key count must compare unequal, and so must a same-count map whose keys differ.
        /// </summary>
        [Fact]
        public void ColorMapsEqual_DifferingKeys_ReturnsFalse()
        {
            Dictionary<string, Color> left = new(StringComparer.Ordinal)
            {
                ["A"] = Colors.Red,
            };
            Dictionary<string, Color> extra = new(StringComparer.Ordinal)
            {
                ["A"] = Colors.Red,
                ["B"] = Colors.Red,
            };
            Dictionary<string, Color> renamed = new(StringComparer.Ordinal)
            {
                ["C"] = Colors.Red,
            };

            Assert.False(PublishFingerprint.ColorMapsEqual(left, extra));
            Assert.False(PublishFingerprint.ColorMapsEqual(left, renamed));
        }

        /// <summary>
        /// The resolved theme is part of the fingerprint in its own right: two runs that somehow
        /// produced the same color map under different themes still select different
        /// <c>SpecialBrushes</c> branches and must not be treated as interchangeable.
        /// </summary>
        [Fact]
        public Task Fingerprint_SameColorsDifferentTheme_DoesNotMatchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> colors = new(StringComparer.Ordinal) { ["A"] = Colors.Red };

                PublishFingerprint light = PublishFingerprint.Capture(ApplicationTheme.Light, colors);
                PublishFingerprint sameLight = PublishFingerprint.Capture(ApplicationTheme.Light, colors);
                PublishFingerprint dark = PublishFingerprint.Capture(ApplicationTheme.Dark, colors);

                Assert.True(light.Matches(sameLight));
                Assert.False(light.Matches(dark));
            });
        }

        /// <summary>
        /// The Settings "Transparency effects" toggle moves no computed color and no system color,
        /// so the map-plus-theme half of the fingerprint is blind to it. The flag has to make the
        /// comparison fail on its own, because the Windows 10 legacy-acrylic path re-reads the
        /// setting only when a publish raises Changed; a gated-out toggle leaves every open window
        /// on the acrylic the previous setting asked for.
        /// </summary>
        [Fact]
        public Task Fingerprint_SameColorsDifferentTransparencySetting_DoesNotMatchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Dictionary<string, Color> colors = new(StringComparer.Ordinal) { ["A"] = Colors.Red };

                PublishFingerprint transparencyOn = PublishFingerprint.Capture(ApplicationTheme.Light, colors, transparencyEnabled: true);
                PublishFingerprint sameOn = PublishFingerprint.Capture(ApplicationTheme.Light, colors, transparencyEnabled: true);
                PublishFingerprint transparencyOff = PublishFingerprint.Capture(ApplicationTheme.Light, colors, transparencyEnabled: false);

                Assert.True(transparencyOn.Matches(sameOn));
                Assert.False(transparencyOn.Matches(transparencyOff));
                Assert.False(transparencyOff.Matches(transparencyOn));
            });
        }

        /// <summary>
        /// Guards the fingerprint's SystemColors snapshot against drift: every live
        /// <see cref="SystemColors"/> member that <c>SpecialBrushes.cs</c> reads must also be
        /// captured by <c>PublishFingerprint.cs</c>, or a high-contrast variant switch that moves
        /// only the missing member would be swallowed by the redundant-publish gate. The two lists
        /// are compile-time property accesses with no runtime registry to diff, so this scans the
        /// two source files and compares the referenced member sets.
        /// </summary>
        [Fact]
        public async Task Fingerprint_CapturesEverySystemColorSpecialBrushesReadsAsync()
        {
            string root = ThemeParityTests.FindRepoRoot();
            string specialBrushes = await File.ReadAllTextAsync(
                Path.Join(root, "Fluence.Wpf", "Theming", "SpecialBrushes.cs"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            string fingerprint = await File.ReadAllTextAsync(
                Path.Join(root, "Fluence.Wpf", "Theming", "PublishFingerprint.cs"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            HashSet<string> reads = ExtractSystemColorMembers(specialBrushes);
            HashSet<string> captured = ExtractSystemColorMembers(fingerprint);

            Assert.True(
                reads.Count >= 8,
                "Expected SpecialBrushes to read several SystemColors members; the source scan found " + reads.Count.ToString(CultureInfo.InvariantCulture) + ".");
            reads.ExceptWith(captured);
            Assert.True(
                reads.Count is 0,
                "SystemColors members read by SpecialBrushes but missing from PublishFingerprint.CaptureSystemColors: " + string.Join(", ", reads));
        }

        /// <summary>
        /// Collects every <c>SystemColors.XxxColor</c> member referenced in the given C# source.
        /// </summary>
        /// <param name="source">The C# source text to scan.</param>
        private static HashSet<string> ExtractSystemColorMembers(string source)
        {
            HashSet<string> members = new(StringComparer.Ordinal);
            const string prefix = "SystemColors.";
            int index = source.IndexOf(prefix, StringComparison.Ordinal);
            while (index >= 0)
            {
                int start = index + prefix.Length;
                int end = start;
                while (end < source.Length && (char.IsLetterOrDigit(source[end]) || source[end] == '_'))
                {
                    end++;
                }

                string member = source[start..end];
                if (member.EndsWith("Color", StringComparison.Ordinal))
                {
                    _ = members.Add(member);
                }
                index = source.IndexOf(prefix, end, StringComparison.Ordinal);
            }
            return members;
        }

        /// <summary>
        /// Seeds the three resource slots with the Light theme and a pinned accent seed, so that
        /// every assertion in this fixture is independent of the host machine's OS accent color,
        /// and returns the live merged-dictionary collection.
        /// </summary>
        private static Collection<ResourceDictionary> SeedPinnedLightTheme()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
            ApplicationAccentColorManager.ApplyCustomAccent(PinnedAccent);
            return Application.Current.Resources.MergedDictionaries;
        }
    }
}
