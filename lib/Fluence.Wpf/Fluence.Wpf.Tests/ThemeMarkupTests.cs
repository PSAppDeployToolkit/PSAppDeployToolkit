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
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Fluence.Wpf.Markup;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for <see cref="ThemeResourceExtension"/> and <see cref="ThemeDictionary"/>.
    /// </summary>
    public class ThemeMarkupTests
    {
        private const string XamlNamespaces =
            "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
            "xmlns:fluence=\"http://schemas.fluencewpf.com\"";

        public ThemeMarkupTests()
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
        public void ThemeResource_OnElement_UpdatesAcrossThemeChange()
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                const string xaml =
                    $"<TextBlock {XamlNamespaces} Text=\"probe\" " +
                    "Foreground=\"{fluence:ThemeResource TextFillColorPrimaryBrush}\" />";
                TextBlock probe = (TextBlock)XamlReader.Parse(xaml);

                Window window = NewTestWindow(probe);
                try
                {
                    Color lightColor = AssertForegroundMatchesToken(probe);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Color darkColor = AssertForegroundMatchesToken(probe);

                    Assert.NotEqual(lightColor, darkColor);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ThemeResource_InStyleSetter_UpdatesAcrossThemeChange()
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                const string xaml =
                    $"<Style {XamlNamespaces} TargetType=\"TextBlock\">" +
                    "<Setter Property=\"Foreground\" Value=\"{fluence:ThemeResource TextFillColorPrimaryBrush}\" />" +
                    "</Style>";
                Style style = (Style)XamlReader.Parse(xaml);

                TextBlock probe = new() { Text = "probe", Style = style };
                Window window = NewTestWindow(probe);
                try
                {
                    Color lightColor = AssertForegroundMatchesToken(probe);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Color darkColor = AssertForegroundMatchesToken(probe);

                    Assert.NotEqual(lightColor, darkColor);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ThemeDictionary_XamlParsed_SwapsValuesAcrossStandardThemeCycle()
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                const string xaml =
                    $"<fluence:ThemeDictionary {XamlNamespaces}>" +
                    "<fluence:ThemeDictionary.ThemeDictionaries>" +
                    "<fluence:ThemeResourceDictionary ThemeKey=\"Light\">" +
                    "<SolidColorBrush x:Key=\"ProbeBrush\" Color=\"#EEEEEE\" />" +
                    "<x:String x:Key=\"ProbeString\">Light theme</x:String>" +
                    "</fluence:ThemeResourceDictionary>" +
                    "<fluence:ThemeResourceDictionary ThemeKey=\"Dark\">" +
                    "<SolidColorBrush x:Key=\"ProbeBrush\" Color=\"#333333\" />" +
                    "<x:String x:Key=\"ProbeString\">Dark theme</x:String>" +
                    "</fluence:ThemeResourceDictionary>" +
                    "<fluence:ThemeResourceDictionary ThemeKey=\"HighContrast\">" +
                    "<SolidColorBrush x:Key=\"ProbeBrush\" Color=\"#00FF00\" />" +
                    "<x:String x:Key=\"ProbeString\">High contrast theme</x:String>" +
                    "</fluence:ThemeResourceDictionary>" +
                    "</fluence:ThemeDictionary.ThemeDictionaries>" +
                    "</fluence:ThemeDictionary>";
                ThemeDictionary themeDictionary = (ThemeDictionary)XamlReader.Parse(xaml);

                TextBlock probe = new();
                probe.SetResourceReference(TextBlock.TextProperty, "ProbeString");
                Window window = NewTestWindow(probe);
                window.Resources.MergedDictionaries.Add(themeDictionary);
                try
                {
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Light theme", probe.Text, StringComparer.Ordinal);
                    AssertProbeBrush(window, Color.FromRgb(0xEE, 0xEE, 0xEE));

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Dark theme", probe.Text, StringComparer.Ordinal);
                    AssertProbeBrush(window, Color.FromRgb(0x33, 0x33, 0x33));

                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("High contrast theme", probe.Text, StringComparer.Ordinal);
                    AssertProbeBrush(window, Color.FromRgb(0x00, 0xFF, 0x00));

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Light theme", probe.Text, StringComparer.Ordinal);
                    AssertProbeBrush(window, Color.FromRgb(0xEE, 0xEE, 0xEE));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ThemeDictionary_CodeFirst_UsesDefaultFallback()
        {
            WpfTestSta.Invoke(() =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                ThemeDictionary themeDictionary = new()
                {
                    ThemeDictionaries =
                    {
                        new ThemeResourceDictionary { ThemeKey = "Light", ["ProbeValue"] = "light" },
                        new ThemeResourceDictionary { ThemeKey = "Default", ["ProbeValue"] = "fallback" },
                    },
                };

                Window window = NewTestWindow(new TextBlock());
                window.Resources.MergedDictionaries.Add(themeDictionary);
                try
                {
                    Assert.Equal("light", window.TryFindResource("ProbeValue"));

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None);
                    Assert.Equal("fallback", window.TryFindResource("ProbeValue"));

                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None);
                    Assert.Equal("fallback", window.TryFindResource("ProbeValue"));

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);
                    Assert.Equal("light", window.TryFindResource("ProbeValue"));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ThemeDictionary_HighContrast_PrefersPolarityTableThenGeneric()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                ThemeDictionary withPolarity = new()
                {
                    ThemeDictionaries =
                    {
                        new ThemeResourceDictionary { ThemeKey = "HighContrastBlack", ["ProbeValue"] = "black" },
                        new ThemeResourceDictionary { ThemeKey = "HighContrastWhite", ["ProbeValue"] = "white" },
                        new ThemeResourceDictionary { ThemeKey = "HighContrast", ["ProbeValue"] = "generic" },
                        new ThemeResourceDictionary { ThemeKey = "Default", ["ProbeValue"] = "fallback" },
                    },
                };
                ThemeDictionary genericOnly = new()
                {
                    ThemeDictionaries =
                    {
                        new ThemeResourceDictionary { ThemeKey = "HighContrast", ["OtherProbeValue"] = "generic" },
                        new ThemeResourceDictionary { ThemeKey = "Default", ["OtherProbeValue"] = "fallback" },
                    },
                };

                Window window = NewTestWindow(new TextBlock());
                window.Resources.MergedDictionaries.Add(withPolarity);
                window.Resources.MergedDictionaries.Add(genericOnly);
                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None);

                    // Polarity is judged from the live system window color with the same
                    // luminance formula the library uses, so the expectation is stable on
                    // any machine, high contrast active or not.
                    Color windowColor = SystemColors.WindowColor;
                    double luminance = (0.299 * windowColor.R) + (0.587 * windowColor.G) + (0.114 * windowColor.B);
                    string expected = luminance < 128.0 ? "black" : "white";

                    Assert.Equal(expected, window.TryFindResource("ProbeValue"));
                    Assert.Equal("generic", window.TryFindResource("OtherProbeValue"));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ThemeDictionary_InSealedStyleResources_DoesNotThrowOnThemeChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                ThemeDictionary themeDictionary = new()
                {
                    ThemeDictionaries =
                    {
                        new ThemeResourceDictionary { ThemeKey = "Light", ["ProbeValue"] = "light" },
                        new ThemeResourceDictionary { ThemeKey = "Dark", ["ProbeValue"] = "dark" },
                    },
                };

                Style style = new(typeof(TextBlock)) { Resources = themeDictionary };
                style.Seal();
                Assert.True(themeDictionary.IsReadOnly,
                    "Sealing the style should propagate read-only into its resources; the guard under test depends on it.");

                // Without the read-only guard this throws InvalidOperationException from the
                // static Changed handler and crashes the theme apply.
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                Assert.Equal("light", themeDictionary["ProbeValue"]);
            });
        }

        [Fact]
        public void ThemeDictionary_Discarded_IsGarbageCollected()
        {
            WpfTestSta.Invoke(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);

                WeakReference tracker = CreateDiscardedThemeDictionary();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Assert.False(tracker.IsAlive,
                    "A discarded ThemeDictionary must be collectable; the static theme subscription must not pin it.");

                // The subscription must also keep working with dead entries in its list.
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);
            });
        }

        private static Color AssertForegroundMatchesToken(TextBlock probe)
        {
            SolidColorBrush tokenBrush = Assert.IsType<SolidColorBrush>(Application.Current.TryFindResource("TextFillColorPrimaryBrush"));

            SolidColorBrush foreground = Assert.IsType<SolidColorBrush>(probe.Foreground);
            Assert.Equal(tokenBrush.Color, foreground.Color);
            return foreground.Color;
        }

        private static void AssertProbeBrush(Window window, Color expected)
        {
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(window.TryFindResource("ProbeBrush"));
            Assert.Equal(expected, brush.Color);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateDiscardedThemeDictionary()
        {
            ThemeDictionary discarded = new()
            {
                ThemeDictionaries =
                {
                    new ThemeResourceDictionary { ThemeKey = "Light", ["ProbeValue"] = "light" },
                    new ThemeResourceDictionary { ThemeKey = "Dark", ["ProbeValue"] = "dark" },
                },
            };
            return new WeakReference(discarded);
        }

        private static Window NewTestWindow(UIElement content)
        {
            Window window = new()
            {
                Width = 200,
                Height = 200,
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowStyle = WindowStyle.None,
                Content = content,
            };
            window.Show();
            return window;
        }
    }
}
