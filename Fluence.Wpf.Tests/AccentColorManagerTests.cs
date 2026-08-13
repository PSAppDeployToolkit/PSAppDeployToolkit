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
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class AccentColorManagerTests
    {
        public AccentColorManagerTests()
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
        public void ApplySystemAccent_PopulatesRamp()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);
                ApplicationAccentColorManager.ApplySystemAccent();

                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColor);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorLight1);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorLight2);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorLight3);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorDark1);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorDark2);
                Assert.NotEqual(default, ApplicationAccentColorManager.SystemAccentColorDark3);

                object accentResource = app.Resources["SystemAccentColor"];
                Assert.NotNull(accentResource);
            });
        }

        [Fact]
        public void ApplyCustomAccent_SetsCorrectBase()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                Color customColor = Color.FromRgb(0xFF, 0x88, 0x00);
                ApplicationAccentColorManager.ApplyCustomAccent(customColor);

                Assert.Equal(customColor, ApplicationAccentColorManager.SystemAccentColor);

                Assert.NotEqual(customColor, ApplicationAccentColorManager.SystemAccentColorLight1);
                Assert.NotEqual(customColor, ApplicationAccentColorManager.SystemAccentColorDark1);

                Assert.NotEqual(ApplicationAccentColorManager.SystemAccentColorLight1,
                    ApplicationAccentColorManager.SystemAccentColorDark1);
            });
        }

        [Fact]
        public void ApplyApplicationAccent_RaisesAccentColorChangedOnce()
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                int eventCount = 0;
                void OnAccentColorChanged(object? sender, EventArgs e)
                {
                    eventCount++;
                }

                ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
                try
                {
                    ApplicationAccentColorManager.ApplyApplicationAccent();
                }
                finally
                {
                    ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
                }

                Assert.Equal(1, eventCount);
            });
        }

        [Fact]
        public void ApplyCustomAccent_RaisesAccentColorChangedOnce()
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

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
        public void ApplyCustomAccent_PerThemeSeeds_FollowResolvedTheme()
        {
            WpfTestSta.Invoke(static () =>
            {
                Color lightSeed = Color.FromRgb(0x0F, 0x6C, 0xBD);
                Color darkSeed = Color.FromRgb(0x47, 0x9E, 0xF5);

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);
                ApplicationAccentColorManager.ApplyCustomAccent(lightSeed, darkSeed);
                Assert.Equal(lightSeed, ApplicationAccentColorManager.SystemAccentColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None);
                Assert.Equal(darkSeed, ApplicationAccentColorManager.SystemAccentColor);

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None);
                Assert.Equal(darkSeed, ApplicationAccentColorManager.SystemAccentColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);
                Assert.Equal(lightSeed, ApplicationAccentColorManager.SystemAccentColor);
            });
        }

        [Fact]
        public void ApplyCustomAccent_PerThemeSeeds_RaisesAccentColorChangedOnce()
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

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
        public void ThemeChange_UpdatesAdaptiveAccents()
        {
            WpfTestSta.Invoke(static () =>
            {
                Color customColor = Color.FromRgb(0x00, 0x78, 0xD4);
                ApplicationAccentColorManager.ApplyCustomAccent(customColor);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                Color darkPrimary = ApplicationAccentColorManager.SystemAccentColorPrimary;

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
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
        // OS ramps; see docs/_internal/theme-rewrite/design.md for the rationale.
    }
}
