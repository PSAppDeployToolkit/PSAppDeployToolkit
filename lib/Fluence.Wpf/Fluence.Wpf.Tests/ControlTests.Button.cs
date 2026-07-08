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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void Button_AccentDisabled_DarkTheme_UsesVisibleDisabledAccentTokens()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                    AssertDisabledAccentButtonUsesDarkTokens();
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_AccentDisabled_DarkThemeWithoutAccentRefresh_UsesVisibleDisabledAccentTokens()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);

                    AssertDisabledAccentButtonUsesDarkTokens();
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_ExplicitToolTip_IsNotClearedByTruncationFallback()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.ToolTip toolTip = new() { Content = "Save changes" };
                Controls.Button button = new()
                {
                    Width = 160,
                    Content = "Save",
                    ToolTip = toolTip,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreSame(toolTip, button.ToolTip,
                        "Button truncation fallback must not clear an explicit tooltip supplied by the consumer.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_IconOnly_CentersGlyphAndRestoresGapWithContent()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Controls.Button button = new()
                {
                    MinWidth = 32,
                    Padding = new Thickness(8, 4, 8, 4),
                    Icon = new Controls.FontIcon { Glyph = "", IconFontSize = 14 },
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter iconPresenter = FindVisualChildByName<ContentPresenter>(button, "IconPresenter")
                        ?? throw new InvalidOperationException("IconPresenter must exist in the Button template.");
                    Assert.AreEqual(new Thickness(0), iconPresenter.Margin,
                        "An icon-only button must drop the icon-to-text gap.");

                    Point iconCenter = iconPresenter.TranslatePoint(
                        new Point(iconPresenter.ActualWidth / 2.0, iconPresenter.ActualHeight / 2.0), button);
                    Assert.AreEqual(button.ActualWidth / 2.0, iconCenter.X, 1.0,
                        "The icon-only glyph must be horizontally centered in the button.");

                    button.Content = "Copy";
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(new Thickness(0, 0, 8, 0), iconPresenter.Margin,
                        "A button with icon and content must keep the 8px icon-to-text gap.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_Appearances_ApplyWinUiRestBrushesAndBorders()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Controls.Button standard = new() { Content = "Standard" };
                Controls.Button accent = new() { Appearance = ControlAppearance.Accent, Content = "Accent" };
                Controls.Button subtle = new() { Appearance = ControlAppearance.Subtle, Content = "Subtle" };
                StackPanel panel = new();
                _ = panel.Children.Add(standard);
                _ = panel.Children.Add(accent);
                _ = panel.Children.Add(subtle);

                Window window = new()
                {
                    Content = panel,
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertButtonChromeMatchesResources(
                        standard,
                        "ControlFillColorDefaultBrush",
                        "TextFillColorPrimaryBrush",
                        "ControlElevationBorderBrush");
                    AssertButtonChromeMatchesResources(
                        accent,
                        "AccentFillColorDefaultBrush",
                        "TextOnAccentFillColorPrimaryBrush",
                        "AccentControlElevationBorderBrush");
                    AssertButtonChromeMatchesResources(
                        subtle,
                        "SubtleFillColorTransparentBrush",
                        "TextFillColorPrimaryBrush",
                        "SubtleFillColorTransparentBrush");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_Template_UsesWinUiStateResourceMappings()
        {
            string xaml = File.ReadAllText(GetRepositoryFilePath("Fluence.Wpf", "Themes", "Controls", "Button.xaml"));
            string[] requiredStateResources =
            [
                "ControlFillColorSecondaryBrush",
                "ControlFillColorTertiaryBrush",
                "ControlFillColorDisabledBrush",
                "AccentFillColorSecondaryBrush",
                "AccentFillColorTertiaryBrush",
                "AccentFillColorDisabledBrush",
                "TextOnAccentFillColorSecondaryBrush",
                "SubtleFillColorTransparentBrush",
                "SubtleFillColorSecondaryBrush",
                "SubtleFillColorTertiaryBrush",
                "TextFillColorDisabledBrush",
            ];

            foreach (string resource in requiredStateResources)
            {
                Assert.IsTrue(
                    xaml.Contains(resource, StringComparison.Ordinal),
                    "Button template should include the WinUI state resource: " + resource);
            }

            string accentPressedBlock = GetTriggerBlock(
                xaml,
                "<Condition Property=\"IsPressed\" Value=\"True\" />",
                "<Condition Property=\"Appearance\" Value=\"Accent\" />");
            Assert.IsFalse(
                accentPressedBlock.Contains("AccentFillColorDisabledBrush", StringComparison.Ordinal),
                "Accent pressed state must not reuse the disabled accent fill as the button Background.");
            Assert.IsFalse(
                xaml.Contains("Value=\"Transparent\"", StringComparison.Ordinal),
                "Button template should use theme resources rather than literal transparent brush values.");
        }

        private static void AssertDisabledAccentButtonUsesDarkTokens()
        {
            Window window = new();
            Controls.Button button = new()
            {
                Width = 100,
                Appearance = ControlAppearance.Accent,
                Content = "Add",
                IsEnabled = false,
            };

            try
            {
                window.Content = button;
                window.Show();
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Border? restFill = button.Template.FindName("RestFill", button) as Border;

                Assert.IsNotNull(restFill, "Button template should expose RestFill.");
                Assert.IsInstanceOfType(restFill.Background, typeof(SolidColorBrush));
                Assert.IsInstanceOfType(button.Foreground, typeof(SolidColorBrush));
                Assert.AreEqual(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF), ((SolidColorBrush)restFill.Background).Color,
                    "Disabled Accent buttons in Dark theme must use the WinUI dark disabled accent fill so the disabled surface stays visible.");
                Assert.AreEqual(Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF), ((SolidColorBrush)button.Foreground).Color,
                    "Disabled Accent button text in Dark theme must use the theme's disabled on-accent text token.");
            }
            finally
            {
                window.Close();
            }
        }

        private static void AssertButtonChromeMatchesResources(
            Controls.Button button,
            string backgroundKey,
            string foregroundKey,
            string borderKey)
        {
            Border? restFill = button.Template.FindName("RestFill", button) as Border;
            Border? outerBorder = button.Template.FindName("OuterBorder", button) as Border;
            Assert.IsNotNull(restFill, "Button template should expose RestFill.");
            Assert.IsNotNull(outerBorder, "Button template should expose OuterBorder.");
            Assert.AreEqual(new Thickness(1), outerBorder.BorderThickness,
                "Button chrome should apply the WinUI one-pixel border thickness.");

            AssertBrushMatchesResource(restFill.Background, backgroundKey);
            AssertBrushMatchesResource(button.Foreground, foregroundKey);
            AssertBrushMatchesResource(outerBorder.BorderBrush, borderKey);
        }

        private static void AssertBrushMatchesResource(Brush? actual, string resourceKey)
        {
            object? expected = Application.Current.TryFindResource(resourceKey);
            Assert.IsNotNull(actual, "Actual brush should be set for " + resourceKey + ".");
            Assert.IsInstanceOfType(expected, typeof(Brush), "Resource should resolve to a Brush: " + resourceKey);

            if (actual is SolidColorBrush actualSolid && expected is SolidColorBrush expectedSolid)
            {
                Assert.AreEqual(expectedSolid.Color, actualSolid.Color, "Unexpected brush color for " + resourceKey + ".");
                return;
            }

            if (actual is LinearGradientBrush actualGradient && expected is LinearGradientBrush expectedGradient)
            {
                Assert.AreEqual(expectedGradient.GradientStops.Count, actualGradient.GradientStops.Count,
                    "Unexpected gradient stop count for " + resourceKey + ".");
                return;
            }

            Assert.AreSame(expected, actual, "Unexpected brush instance for " + resourceKey + ".");
        }

        private static string GetRepositoryFilePath(params string[] relativeSegments)
        {
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string[] pathParts = new string[relativeSegments.Length + 1];
            pathParts[0] = root;
            Array.Copy(relativeSegments, 0, pathParts, 1, relativeSegments.Length);
            return Path.Combine(pathParts);
        }

        private static string GetTriggerBlock(string xaml, string firstCondition, string secondCondition)
        {
            int firstIndex = xaml.IndexOf(firstCondition, StringComparison.Ordinal);
            Assert.IsTrue(firstIndex >= 0, "Button trigger should contain condition: " + firstCondition);
            int secondIndex = xaml.IndexOf(secondCondition, firstIndex, StringComparison.Ordinal);
            Assert.IsTrue(secondIndex >= 0, "Button trigger should contain condition: " + secondCondition);
            int endIndex = xaml.IndexOf("</MultiTrigger>", secondIndex, StringComparison.Ordinal);
            Assert.IsTrue(endIndex >= 0, "Button trigger should close after the requested conditions.");
            return xaml[firstIndex..endIndex];
        }
    }
}
