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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Demo.Pages;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public void SelectionControls_OffStateBackgrounds_UseWinUiAltFillRoles()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                Controls.CheckBox checkBox = new() { Content = "Check" };
                Controls.RadioButton radioButton = new() { Content = "Radio" };
                Controls.ToggleSwitch toggleSwitch = new() { OffContent = "Off", OnContent = "On" };

                StackPanel panel = new();
                _ = panel.Children.Add(checkBox);
                _ = panel.Children.Add(radioButton);
                _ = panel.Children.Add(toggleSwitch);

                Window window = new()
                {
                    Content = panel,
                    Width = 320,
                    Height = 180,
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();
                    _ = radioButton.ApplyTemplate();
                    _ = toggleSwitch.ApplyTemplate();

                    Border indicatorFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(checkBox, "IndicatorFill"));
                    Border indicatorHover = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(checkBox, "IndicatorHover"));
                    Border indicatorPressed = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(checkBox, "IndicatorPressed"));

                    AssertBrushColor(indicatorFill.Background, "ControlAltFillColorSecondaryBrush");
                    AssertBrushColor(indicatorHover.Background, "ControlAltFillColorTertiaryBrush");
                    AssertBrushColor(indicatorPressed.Background, "ControlAltFillColorQuarternaryBrush");
                    AssertBrushColor(indicatorPressed.BorderBrush, "ControlStrongStrokeColorDisabledBrush");

                    System.Windows.Shapes.Ellipse outerEllipse = Assert.IsAssignableFrom<System.Windows.Shapes.Ellipse>(FindVisualChildByName<System.Windows.Shapes.Ellipse>(radioButton, "OuterEllipse"));
                    System.Windows.Shapes.Ellipse outerEllipseHover = Assert.IsAssignableFrom<System.Windows.Shapes.Ellipse>(FindVisualChildByName<System.Windows.Shapes.Ellipse>(radioButton, "OuterEllipseHover"));
                    System.Windows.Shapes.Ellipse outerEllipsePressed = Assert.IsAssignableFrom<System.Windows.Shapes.Ellipse>(FindVisualChildByName<System.Windows.Shapes.Ellipse>(radioButton, "OuterEllipsePressed"));

                    AssertBrushColor(outerEllipse.Fill, "ControlAltFillColorSecondaryBrush");
                    AssertBrushColor(outerEllipseHover.Fill, "ControlAltFillColorTertiaryBrush");
                    AssertBrushColor(outerEllipsePressed.Fill, "ControlAltFillColorQuarternaryBrush");
                    AssertBrushColor(outerEllipsePressed.Stroke, "ControlStrongStrokeColorDisabledBrush");

                    Border trackOff = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(toggleSwitch, "TrackOff"));
                    Border trackOffHover = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(toggleSwitch, "TrackOffHover"));
                    Border trackOffPressed = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(toggleSwitch, "TrackOffPressed"));

                    AssertBrushColor(trackOff.Background, "ControlAltFillColorSecondaryBrush");
                    AssertBrushColor(trackOff.BorderBrush, "ControlStrongStrokeColorDefaultBrush");
                    AssertBrushColor(trackOffHover.Background, "ControlAltFillColorTertiaryBrush");
                    AssertBrushColor(trackOffPressed.Background, "ControlAltFillColorQuarternaryBrush");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public void ProgressBar_TrackBackground_UsesWinUiStrongStrokeRole()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                Controls.ProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 40,
                };
                Window window = new()
                {
                    Content = progressBar,
                    Width = 300,
                    Height = 120,
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    _ = progressBar.ApplyTemplate();

                    Border track = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(progressBar, "PART_Track"));
                    AssertBrushColor(track.Background, "ControlStrongStrokeColorDefaultBrush");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public void ScrollBar_RailBackground_UsesWinUiTrackFillRole()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                ScrollBar scrollBar = new()
                {
                    Orientation = Orientation.Vertical,
                    Style = application?.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 12,
                    Height = 200,
                };
                Window window = new()
                {
                    Content = scrollBar,
                    Width = 60,
                    Height = 300,
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    _ = scrollBar.ApplyTemplate();

                    Border trackBackground = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(scrollBar, "TrackBackground"));
                    AssertBrushColor(trackBackground.Background, "ScrollBarTrackFillBrush");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public void DemoSampleControl_Chrome_UsesWinUiGalleryBackgroundRoles()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);

                DemoSampleControl sample = new()
                {
                    SampleDescription = "Sample",
                    DemoContent = new TextBlock { Text = "Body" },
                    OutputContent = new TextBlock { Text = "Output" },
                    RightRailContent = new CheckBox { Content = "Option" },
                    XamlSource = "<Grid />",
                };
                Window window = new()
                {
                    Content = sample,
                    Width = 420,
                    Height = 300,
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border sampleCard = Assert.IsType<Border>(sample.FindName("SampleCard"));
                    Controls.Expander sourceExpander = Assert.IsType<Controls.Expander>(sample.FindName("SourceExpander"));

                    Assert.Equal(new CornerRadius(8, 8, 0, 0), sampleCard.CornerRadius);
                    Grid demoRegionGrid = Assert.IsType<Grid>(sample.FindName("DemoRegionGrid"));
                    Border rightRail = Assert.IsType<Border>(sample.FindName("RightRailBorder"));
                    Border outputRegion = Assert.IsType<Border>(sample.FindName("OutputRegion"));

                    AssertBrushColor(sampleCard.Background, "SolidBackgroundFillColorBaseBrush");
                    Assert.Equal(new Thickness(0), sampleCard.Padding);
                    Assert.Equal((Thickness)sample.FindResource("DemoSampleCardPadding"), demoRegionGrid.Margin);
                    Assert.Equal(new Thickness(0), rightRail.Margin);
                    AssertBrushColor(rightRail.Background, "CardBackgroundFillColorSecondaryBrush");
                    AssertBrushColor(sourceExpander.Background, "CardBackgroundFillColorSecondaryBrush");
                    Assert.Equal("Source code", sourceExpander.Header);

                    sourceExpander.IsExpanded = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    RichTextBox sourceViewer = Assert.IsAssignableFrom<RichTextBox>(FindVisualChildByName<RichTextBox>(sourceExpander, "SourceTextViewer"));
                    Border copyButtonHost = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(sourceExpander, "CopySourceButtonHost"));
                    AssertBrushColor(sourceViewer.Background, "SystemFillColorSolidAttentionBackgroundBrush");
                    AssertBrushColor(copyButtonHost.Background, "CardBackgroundFillColorDefaultBrush");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public void DemoSharedResources_DoNotShadowNativeFluenceSurfaceRoles()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);

                    _ = Assert.IsAssignableFrom<Color>(application?.TryFindResource("CardBackgroundFillColorDefault"));
                    AssertBrushResolves("CardBackgroundFillColorDefaultBrush");
                }
            });
        }

        [Fact]
        public void DemoSharedResources_NativeBrushesResolveInLightAndHighContrast()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.HighContrast })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);

                    foreach (string key in GetNativeDemoSurfaceBrushKeys())
                    {
                        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(application?.TryFindResource(key));
                        Assert.NotEqual(Color.FromRgb(0x27, 0x27, 0x27), brush.Color);
                    }
                }
            });
        }

        [Fact]
        public void BackgroundParityBrushes_ResolveAcrossThemesAndDeterministicAccent()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                string[] keys =
                [
                    "ControlAltFillColorSecondaryBrush",
                    "ControlAltFillColorTertiaryBrush",
                    "ControlAltFillColorQuarternaryBrush",
                    "ControlAltFillColorDisabledBrush",
                    "ControlFillColorQuarternaryBrush",
                    "CardBackgroundFillColorTertiaryBrush",
                    "SolidBackgroundFillColorQuinaryBrush",
                    "SolidBackgroundFillColorSenaryBrush",
                    "ScrollBarTrackFillBrush",
                    "SolidBackgroundFillColorBaseBrush",
                    "CardBackgroundFillColorDefaultBrush",
                    "CardBackgroundFillColorSecondaryBrush",
                    "ControlFillColorDefaultBrush",
                    "TextFillColorSecondaryBrush",
                    "AccentFillColorDefaultBrush",
                    "SubtleFillColorTransparentBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "ControlOnImageFillColorDefaultBrush",
                    "ControlOnImageFillColorSecondaryBrush",
                    "ControlOnImageFillColorTertiaryBrush",
                    "ControlOnImageFillColorDisabledBrush",
                    "SurfaceStrokeColorDefaultBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "SurfaceStrokeColorInverseBrush",
                    "DividerStrokeColorDefaultBrush",
                    "LayerOnAcrylicFillColorDefaultBrush",
                    "LayerOnAccentAcrylicFillColorDefaultBrush",
                    "LayerOnMicaBaseAltFillColorDefaultBrush",
                    "LayerOnMicaBaseAltFillColorSecondaryBrush",
                    "LayerOnMicaBaseAltFillColorTertiaryBrush",
                    "LayerOnMicaBaseAltFillColorTransparentBrush",
                    "AcrylicBackgroundFillColorDefaultBrush",
                    "AcrylicBackgroundFillColorBaseBrush",
                    "SystemFillColorInformationalBrush",
                    "SystemColorWindowTextColorBrush",
                    "SystemColorWindowColorBrush",
                    "SystemColorButtonFaceColorBrush",
                    "SystemColorButtonTextColorBrush",
                    "SystemColorHighlightColorBrush",
                    "SystemColorHighlightTextColorBrush",
                    "SystemColorHotlightColorBrush",
                    "SystemColorGrayTextColorBrush",
                ];

                ApplicationTheme[] themes =
                [
                    ApplicationTheme.Light,
                    ApplicationTheme.Dark,
                    ApplicationTheme.HighContrast,
                ];

                foreach (ApplicationTheme theme in themes)
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                    foreach (string key in keys)
                    {
                        Assert.NotNull(application?.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public void PowerShellDemoScripts_FollowCanonicalBootstrap()
        {
            string scriptsRoot = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo.PowerShell");
            string[] scriptNames =
            [
                "01-HelloWorld.ps1",
                "02-ThemeAndAccent.ps1",
                "03-ControlsTour.ps1",
                "04-LoadXamlFile.ps1",
            ];
            string[] retiredScriptNames =
            [
                "Show-ControlsDemo.ps1",
                "Show-ThemeDemo.ps1",
                "Show-ProgressDemo.ps1",
            ];
            List<string> violations = [];

            foreach (string scriptName in scriptNames)
            {
                string path = Path.Combine(scriptsRoot, scriptName);
                if (!File.Exists(path))
                {
                    violations.Add(scriptName + " is missing.");
                    continue;
                }

                string source = File.ReadAllText(path);

                // Each script is self-contained and must follow the canonical bootstrap:
                // relaunch into STA (WPF requirement), create a WPF Application before theming
                // (otherwise ApplicationThemeManager.Apply has nowhere to publish brushes), and
                // apply the Fluence theme engine.
                if (!ContainsOrdinal(source, "GetApartmentState"))
                {
                    violations.Add(scriptName + " does not relaunch into STA (missing GetApartmentState guard).");
                }

                if (!ContainsOrdinal(source, "System.Windows.Application"))
                {
                    violations.Add(scriptName + " does not create a System.Windows.Application before theming.");
                }

                if (!ContainsOrdinal(source, "ApplicationThemeManager]::Apply"))
                {
                    violations.Add(scriptName + " does not call ApplicationThemeManager.Apply.");
                }
            }

            // The retired scripts must be gone, and no new script should reference their names.
            foreach (string retired in retiredScriptNames)
            {
                if (File.Exists(Path.Combine(scriptsRoot, retired)))
                {
                    violations.Add(retired + " should have been removed.");
                }
            }

            Assert.Empty(violations);
        }

        [Fact]
        public void PowerShellDemoXaml_UsesCurrentFluenceWindowProperties()
        {
            string path = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo.PowerShell", "MainWindow.xaml");
            string source = File.ReadAllText(path);

            Assert.False(ContainsOrdinal(source, "WindowCorners"),
                "PowerShell demo XAML must not use the old WindowCorners property.");
            Assert.False(ContainsOrdinal(source, "WindowBackdrop"),
                "PowerShell demo XAML must not use the old WindowBackdrop property.");
            Assert.True(ContainsOrdinal(source, "CornerStyle=\"Round\""),
                "PowerShell demo XAML should use CornerStyle.");
            Assert.True(ContainsOrdinal(source, "SystemBackdropType=\"Mica\""),
                "PowerShell demo XAML should use SystemBackdropType.");
        }

        [Fact]
        public void XamlBackgroundAndFillLiterals_AreAllowListed()
        {
            string repoRoot = FindRepoRoot();
            string[] roots =
            [
                Path.Combine(repoRoot, "Fluence.Wpf", "Themes", "Controls"),
                Path.Combine(repoRoot, "Fluence.Wpf.Demo"),
            ];
            List<string> violations = [];

            foreach (string root in roots)
            {
                foreach (string path in Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories))
                {
                    if (IsBackgroundLiteralAllowedPath(path))
                    {
                        continue;
                    }

                    string source = File.ReadAllText(path);
                    CollectBackgroundLiteralViolations(source, path, "Background", violations);
                    CollectBackgroundLiteralViolations(source, path, "Fill", violations);
                }
            }

            Assert.Empty(violations);
        }

        private static void CollectBackgroundLiteralViolations(
            string source,
            string path,
            string attributeName,
            List<string> violations)
        {
            string attributePrefix = attributeName + "=\"";
            int searchIndex = 0;
            while (searchIndex < source.Length)
            {
                int matchIndex = source.IndexOf(attributePrefix, searchIndex, StringComparison.Ordinal);
                if (matchIndex < 0)
                {
                    break;
                }

                int valueStart = matchIndex + attributePrefix.Length;
                int valueEnd = source.IndexOf('"', valueStart);
                if (valueEnd < 0)
                {
                    break;
                }

                searchIndex = valueEnd + 1;
                if (!IsWholeXamlAttribute(source, matchIndex))
                {
                    continue;
                }

                string value = source[valueStart..valueEnd];
                if (!IsLiteralBackgroundValue(value) || value.Equals("Transparent", StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsBackgroundLiteralAllowedValue(path, value))
                {
                    continue;
                }

                violations.Add(GetRepoRelativePath(path) + ": " + attributeName + "=\"" + value + "\"");
            }
        }

        private static bool ContainsOrdinal(string source, string value)
        {
            return source.Contains(value, StringComparison.Ordinal);
        }

        private static bool IsWholeXamlAttribute(string source, int attributeIndex)
        {
            if (attributeIndex is 0)
            {
                return true;
            }

            char previous = source[attributeIndex - 1];
            return !char.IsLetterOrDigit(previous) && previous != '_' && previous != ':';
        }

        private static bool IsLiteralBackgroundValue(string value)
        {
            if (value.Length is 0)
            {
                return false;
            }

            if (value[0] == '#')
            {
                return true;
            }

            if (value[0] == '{')
            {
                return false;
            }

            foreach (char character in value)
            {
                if (!char.IsLetter(character))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertBrushColor(Brush? actualBrush, string resourceKey)
        {
            SolidColorBrush actual = Assert.IsType<SolidColorBrush>(actualBrush);

            SolidColorBrush expected = Assert.IsType<SolidColorBrush>(Application.Current?.TryFindResource(resourceKey));

            Assert.Equal(expected.Color, actual.Color);
        }

        private static void AssertBrushResolves(string resourceKey)
        {
            _ = Assert.IsType<SolidColorBrush>(Application.Current?.TryFindResource(resourceKey));
        }

        private static string[] GetNativeDemoSurfaceBrushKeys()
        {
            return
            [
                "SolidBackgroundFillColorBaseBrush",
                "CardBackgroundFillColorDefaultBrush",
                "CardBackgroundFillColorSecondaryBrush",
                "ControlFillColorDefaultBrush",
                "TextFillColorSecondaryBrush",
            ];
        }

        private static void MergeDemoSharedStyles(Application? application)
        {
            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            };
            application?.Resources.MergedDictionaries.Add(demoShared);
        }

        private static bool IsBackgroundLiteralAllowedPath(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.Equals("GalleryAccessibilityPage.xaml", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBackgroundLiteralAllowedValue(string path, string value)
        {
            string fileName = Path.GetFileName(path);
            if (!fileName.Equals("GallerySettingsPage.xaml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] accentSwatches =
            [
                "#E80000",
                "#F58809",
                "#F5E70C",
                "#2BDE11",
                "#09C4DE",
                "#AA04DE",
                "#FF00E8",
            ];

            foreach (string accentSwatch in accentSwatches)
            {
                if (string.Equals(accentSwatch, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetRepoRelativePath(string path)
        {
            string root = FindRepoRoot();
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                int separatorLength = path.Length > root.Length &&
                    (path[root.Length] == Path.DirectorySeparatorChar || path[root.Length] == Path.AltDirectorySeparatorChar)
                    ? 1
                    : 0;
                return path[(root.Length + separatorLength)..];
            }

            return path;
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Fluence.Wpf.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate Fluence.Wpf.sln ancestor directory from " + AppContext.BaseDirectory);
        }
    }
}
