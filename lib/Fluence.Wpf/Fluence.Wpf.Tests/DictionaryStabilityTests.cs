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
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class DictionaryStabilityTests
    {
        public DictionaryStabilityTests()
        {
            WpfTestSta.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            });
        }

        [Fact]
        public void RepeatedThemeSwitches_NoDictionaryAccumulation()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                int baselineCount = app.Resources.MergedDictionaries.Count;

                for (int i = 0; i < 10; i++)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                }

                int finalCount = app.Resources.MergedDictionaries.Count;
                Assert.Equal(baselineCount, finalCount);
            });
        }

        [Fact]
        public void ThemeSlotIsReused()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                int countAfterFirst = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                int countAfterSecond = app.Resources.MergedDictionaries.Count;

                Assert.Equal(countAfterFirst, countAfterSecond);
            });
        }

        [Fact]
        public void AllThemeVariants_SameSlotCount()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                int lightCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);
                int darkCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);
                int hcCount = app.Resources.MergedDictionaries.Count;

                Assert.Equal(lightCount, darkCount);
                Assert.Equal(darkCount, hcCount);
            });
        }

        [Fact]
        public void FirstApply_LoadsThreeDictionaries()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                Assert.Equal(3, app.Resources.MergedDictionaries.Count);
            });
        }

        [Fact]
        public void Apply_UsesThreeSlots_ReplacesComputedSlotOnChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Collection<ResourceDictionary> dicts = app.Resources.MergedDictionaries;
                Assert.Equal(3, dicts.Count);

                object slot0 = dicts[0];
                object typography = dicts[1];
                object generic = dicts[2];

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);

                Assert.NotSame(slot0, dicts[0]);
                Assert.Same(typography, dicts[1]);
                Assert.Same(generic, dicts[2]);
            });
        }

        [Fact]
        public void AccentUpdate_DoesNotChangeDictionaryCount()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                int countBefore = app.Resources.MergedDictionaries.Count;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                int countAfter = app.Resources.MergedDictionaries.Count;

                Assert.Equal(countBefore, countAfter);
            });
        }

        [Fact]
        public void AllBrushKeys_Resolve_AfterLightDarkHcCycle()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: true);

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
                    Brush brush = Assert.IsAssignableFrom<Brush>(app.Resources[key]);
                }
            });
        }

        [Fact]
        public void InitialApply_SlotsAreComputedTypographyGeneric_InOrder()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                Collection<ResourceDictionary> dictionaries = app.Resources.MergedDictionaries;

                Assert.Equal(3, dictionaries.Count);

                // Slot [0] is the computed dictionary: no Source of its own, populated by the engine.
                Assert.Null(dictionaries[0].Source);
                Assert.True(dictionaries[0].Count > 0, "Computed slot [0] should hold resolved entries.");

                Uri typographySource = dictionaries[1].Source;
                Assert.NotNull(typographySource);
                Assert.True(typographySource.OriginalString.Contains("Typography", StringComparison.OrdinalIgnoreCase),
                    "Slot [1] Source should be Typography.xaml, but was " + typographySource.OriginalString);

                Uri genericSource = dictionaries[2].Source;
                Assert.NotNull(genericSource);
                Assert.True(genericSource.OriginalString.Contains("Generic", StringComparison.OrdinalIgnoreCase),
                    "Slot [2] Source should be Generic.xaml, but was " + genericSource.OriginalString);
            });
        }
    }
}
