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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class DictionaryStabilityTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            WpfTestSta.Invoke(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            });
        }

        [TestMethod]
        public void RepeatedThemeSwitches_NoDictionaryAccumulation()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                int baselineCount = app.Resources.MergedDictionaries.Count;

                for (int i = 0; i < 10; i++)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                }

                int finalCount = app.Resources.MergedDictionaries.Count;
                Assert.AreEqual(baselineCount, finalCount,
                    string.Format("Dictionary count should remain at {0}, but was {1} after 20 switches",
                        baselineCount, finalCount));
            });
        }

        [TestMethod]
        public void ThemeSlotIsReused()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                int countAfterFirst = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
                int countAfterSecond = app.Resources.MergedDictionaries.Count;

                Assert.AreEqual(countAfterFirst, countAfterSecond,
                    "Theme dictionary slot should be reused, not added");
            });
        }

        [TestMethod]
        public void AllThemeVariants_SameSlotCount()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                int lightCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
                int darkCount = app.Resources.MergedDictionaries.Count;

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, false);
                int hcCount = app.Resources.MergedDictionaries.Count;

                Assert.AreEqual(lightCount, darkCount, "Light and Dark should use same slot count");
                Assert.AreEqual(darkCount, hcCount, "Dark and HighContrast should use same slot count");
            });
        }

        [TestMethod]
        public void FirstApply_LoadsThreeDictionaries()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);

                Assert.AreEqual(3, app.Resources.MergedDictionaries.Count,
                    "Initial Apply should load exactly 3 dictionaries ([0] computed, [1] Typography, [2] Generic).");
            });
        }

        [TestMethod]
        public void Apply_UsesThreeSlots_ReplacesComputedSlotOnChange()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                Collection<ResourceDictionary> dicts = app.Resources.MergedDictionaries;
                Assert.AreEqual(3, dicts.Count, "Three slots after the first Apply.");

                object slot0 = dicts[0];
                object typography = dicts[1];
                object generic = dicts[2];

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);

                Assert.AreNotSame(slot0, dicts[0], "Computed slot [0] is replaced on theme change.");
                Assert.AreSame(typography, dicts[1], "Typography slot [1] identity is stable across theme change.");
                Assert.AreSame(generic, dicts[2], "Generic slot [2] identity is stable across theme change.");
            });
        }

        [TestMethod]
        public void AccentUpdate_DoesNotChangeDictionaryCount()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                int countBefore = app.Resources.MergedDictionaries.Count;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                int countAfter = app.Resources.MergedDictionaries.Count;

                Assert.AreEqual(countBefore, countAfter,
                    "Applying a custom accent should not change the merged dictionary count.");
            });
        }

        [TestMethod]
        public void AllBrushKeys_Resolve_AfterLightDarkHcCycle()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, true);

                string[] keyBrushNames =
                [
                    "TextFillColorPrimaryBrush",
                    "AccentFillColorDefaultBrush",
                    "SubtleFillColorSecondaryBrush",
                    "ControlStrokeColorDefaultBrush",
                    "CardBackgroundFillColorDefaultBrush"
                ];

                foreach (string? key in keyBrushNames)
                {
                    Brush? brush = app.Resources[key] as Brush;
                    Assert.IsNotNull(brush, string.Format("Brush key '{0}' should resolve to non-null after Light->Dark->HC cycle.", key));
                }
            });
        }

        [TestMethod]
        public void InitialApply_SlotsAreComputedTypographyGeneric_InOrder()
        {
            WpfTestSta.Invoke(() =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                Collection<ResourceDictionary> dictionaries = app.Resources.MergedDictionaries;

                Assert.AreEqual(3, dictionaries.Count);

                // Slot [0] is the computed dictionary: no Source of its own, populated by the engine.
                Assert.IsNull(dictionaries[0].Source, "Computed slot [0] should have no Source URI.");
                Assert.IsTrue(dictionaries[0].Count > 0, "Computed slot [0] should hold resolved entries.");

                Uri typographySource = dictionaries[1].Source;
                Assert.IsNotNull(typographySource, "Typography slot [1] should have a Source URI.");
                Assert.IsTrue(typographySource.OriginalString.IndexOf("Typography", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Slot [1] Source should be Typography.xaml, but was " + typographySource.OriginalString);

                Uri genericSource = dictionaries[2].Source;
                Assert.IsNotNull(genericSource, "Generic slot [2] should have a Source URI.");
                Assert.IsTrue(genericSource.OriginalString.IndexOf("Generic", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Slot [2] Source should be Generic.xaml, but was " + genericSource.OriginalString);
            });
        }
    }
}
