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
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class TextRenderingPolicyTests
    {
        [Fact]
        public Task FluenceWindow_DefaultStyleOwnsCrispRootRenderingPolicyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                FluenceWindow window = new()
                {
                    Width = 320,
                    Height = 240,
                    Content = new Grid(),
                };

                try
                {
                    _ = window.ApplyTemplate();

                    Assert.True(window.UseLayoutRounding, "FluenceWindow should enable layout rounding at the root.");
                    Assert.True(window.SnapsToDevicePixels, "FluenceWindow should snap device pixels at the root.");
                    Assert.Equal(
                        ClearTypeHint.Auto,
                        RenderOptions.GetClearTypeHint(window));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_ChildInheritsPixelAlignmentPolicyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                System.Windows.Controls.Border child = new();
                FluenceWindow window = new()
                {
                    Width = 320,
                    Height = 240,
                    Content = child,
                };

                try
                {
                    _ = window.ApplyTemplate();

                    Assert.True(child.UseLayoutRounding, "Children should inherit root layout rounding.");
                    Assert.True(child.SnapsToDevicePixels, "Children should inherit root device-pixel snapping.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PopupBackplates_OpaqueSurfacesEnableClearTypeAsync()
        {
            // Popups with AllowsTransparency="True" are layered windows, which silently drop text
            // to grayscale anti-aliasing. RenderOptions.ClearTypeHint="Enabled" on the opaque
            // backplate restores subpixel rendering for that subtree without touching the window
            // root, which stays at Auto for the reasons documented in FluenceWindow.xaml.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                AssertHostedSurfaceEnablesClearType(new AutoSuggestBox(), "SuggestionsSurface");
                AssertHostedSurfaceEnablesClearType(new Controls.DatePicker(), "FlyoutSurface");
                AssertHostedSurfaceEnablesClearType(new TimePicker(), "FlyoutSurface");
                AssertHostedSurfaceEnablesClearType(new DropDownButton(), "FlyoutSurface");
                AssertHostedSurfaceEnablesClearType(new SplitButton(), "FlyoutSurface");
                AssertHostedSurfaceEnablesClearType(new ToggleSplitButton(), "FlyoutSurface");
                AssertHostedSurfaceEnablesClearType(new FlyoutPresenter(), "PresenterSurface");
                AssertHostedSurfaceEnablesClearType(
                    new Controls.MenuItem { Header = "Open" },
                    "SubMenuBorder");
            });
        }

        [Fact]
        public Task ContextMenuSurface_EnablesClearTypeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                // A ContextMenu only resolves its implicit style once it is hosted, so this is the
                // one surface that has to be opened rather than inspected straight off the template.
                Controls.ContextMenu contextMenu = new();
                _ = contextMenu.Items.Add(new Controls.MenuItem { Header = "Open" });

                Window window = new()
                {
                    Width = 400,
                    Height = 300,
                    ContextMenu = contextMenu,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    contextMenu.IsOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AssertSurfaceEnablesClearType(contextMenu, "MenuSurface");
                }
                finally
                {
                    contextMenu.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBoxDropdownSurface_KeepsDefaultClearTypeHintAsync()
        {
            // PART_DropdownBorder paints AcrylicBackgroundFillColorDefaultBrush (alpha F0) under a
            // noise overlay, so it is translucent and must stay at the WPF default. Forcing ClearType
            // over a translucent layered surface degrades text rather than sharpening it.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                Window window = new() { Width = 400, Height = 300 };
                Controls.ComboBox comboBox = new();

                try
                {
                    window.Content = comboBox;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border surface = FindTemplatedSurface(comboBox, "PART_DropdownBorder");

                    Assert.Equal(ClearTypeHint.Auto, RenderOptions.GetClearTypeHint(surface));
                    SolidColorBrush background = Assert.IsType<SolidColorBrush>(surface.Background);
                    Assert.True(
                        background.Color.A is not byte.MaxValue,
                        "The dropdown surface must stay translucent. " + DescribeThemeState(application, background));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public async Task ProductionSources_DoNotSetWpfTextOptionsRenderingPolicyAsync()
        {
            string repoRoot = FindRepoRoot();
            string[] productionRoots =
            [
                Path.Join(repoRoot, "Fluence.Wpf"),
                Path.Join(repoRoot, "Fluence.Wpf.Demo"),
                Path.Join(repoRoot, "Fluence.Wpf.Demo.Mvvm"),
            ];

            const string textOptionsPrefix = "TextOptions.";
            string[] bannedFragments =
            [
                textOptionsPrefix + "TextFormattingMode",
                textOptionsPrefix + "TextRenderingMode",
                textOptionsPrefix + "TextHintingMode",
                textOptionsPrefix + "SetTextFormattingMode",
                textOptionsPrefix + "SetTextRenderingMode",
                textOptionsPrefix + "SetTextHintingMode",
                textOptionsPrefix + "GetTextFormattingMode",
                textOptionsPrefix + "GetTextRenderingMode",
                textOptionsPrefix + "GetTextHintingMode",
            ];

            List<string> offenders = [];
            foreach (string path in EnumerateProductionSources(productionRoots))
            {
                offenders.AddRange(await FindBannedFragmentsAsync(path, bannedFragments).ConfigureAwait(true));
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public async Task ProductionSources_SetDevicePixelSnappingOnlyOnFluenceWindowRootAsync()
        {
            string repoRoot = FindRepoRoot();
            string[] productionRoots =
            [
                Path.Join(repoRoot, "Fluence.Wpf"),
                Path.Join(repoRoot, "Fluence.Wpf.Demo"),
                Path.Join(repoRoot, "Fluence.Wpf.Demo.Mvvm"),
            ];

            string allowedPath = Path.Join(
                "Fluence.Wpf",
                "Themes",
                "Controls",
                "FluenceWindow.xaml");
            List<string> offenders = [];
            foreach (string path in EnumerateProductionSources(productionRoots))
            {
                if (string.Equals(GetRepoRelativePath(path), allowedPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
                if (source.Contains("SnapsToDevicePixels", StringComparison.Ordinal))
                {
                    offenders.Add(GetRepoRelativePath(path));
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public Task TypographyStyles_ApplyTypeRampMetricsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                AssertTypographyMetrics(application, "CaptionTextBlockStyle", 12d, FontWeights.Regular, 16d);
                AssertTypographyMetrics(application, "BodyTextBlockStyle", 14d, FontWeights.Regular, 20d);
                AssertTypographyMetrics(application, "BodyStrongTextBlockStyle", 14d, FontWeights.SemiBold, 20d);
                AssertTypographyMetrics(application, "BodyLargeTextBlockStyle", 18d, FontWeights.Regular, 24d);
                AssertTypographyMetrics(application, "SubtitleTextBlockStyle", 20d, FontWeights.SemiBold, 28d);
                AssertTypographyMetrics(application, "TitleTextBlockStyle", 28d, FontWeights.SemiBold, 36d);
                AssertTypographyMetrics(application, "TitleLargeTextBlockStyle", 40d, FontWeights.SemiBold, 52d);
                AssertTypographyMetrics(application, "DisplayTextBlockStyle", 68d, FontWeights.SemiBold, 92d);
            });
        }

        [Fact]
        public Task TextBlockExtensions_Typography_AppliesTypeRampStyleOnlyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                System.Windows.Controls.TextBlock textBlock = new();
                textBlock.SetTypography(FluentTypography.Title);

                Assert.Same(
                    application.TryFindResource("TitleTextBlockStyle"),
                    textBlock.Style);
                Assert.Equal(28d, textBlock.FontSize, 0.01d);
                Assert.Equal(FontWeights.SemiBold, textBlock.FontWeight);
                Assert.Equal(36d, textBlock.LineHeight, 0.01d);
                Assert.Equal(LineStackingStrategy.BlockLineHeight, textBlock.LineStackingStrategy);
            });
        }

        [Fact]
        public Task TextBlockExtensions_TypographyNone_DoesNotMutateExistingMetricsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                System.Windows.Controls.TextBlock textBlock = new();
                textBlock.SetTypography(FluentTypography.Body);

                FontFamily fontFamily = new("Arial");
                textBlock.FontFamily = fontFamily;
                textBlock.FontSize = 13;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.LineHeight = 17;
                textBlock.LineStackingStrategy = LineStackingStrategy.MaxHeight;

                textBlock.SetTypography(FluentTypography.None);

                Assert.Equal(fontFamily, textBlock.FontFamily);
                Assert.Equal(13d, textBlock.FontSize, 0.01d);
                Assert.Equal(FontWeights.Bold, textBlock.FontWeight);
                Assert.Equal(17d, textBlock.LineHeight, 0.01d);
                Assert.Equal(LineStackingStrategy.MaxHeight, textBlock.LineStackingStrategy);
            });
        }

        private static void AssertHostedSurfaceEnablesClearType(Control control, string surfaceName)
        {
            Window window = new()
            {
                Width = 400,
                Height = 300,
                Content = control,
            };

            try
            {
                window.Show();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                AssertSurfaceEnablesClearType(control, surfaceName);
            }
            finally
            {
                window.Close();
            }
        }

        private static void AssertSurfaceEnablesClearType(Control control, string surfaceName)
        {
            System.Windows.Controls.Border surface = FindTemplatedSurface(control, surfaceName);

            Assert.Equal(
                ClearTypeHint.Enabled,
                RenderOptions.GetClearTypeHint(surface));

            // ClearType only helps on an opaque backplate, so the treated surface must stay opaque.
            Assert.Equal(
                byte.MaxValue,
                Assert.IsType<SolidColorBrush>(surface.Background).Color.A);
        }

        private static System.Windows.Controls.Border FindTemplatedSurface(Control control, string surfaceName)
        {
            ControlTemplate template = control.Template ??
                throw new InvalidOperationException(
                    control.GetType().Name + " did not resolve a default template.");

            return Assert.IsType<System.Windows.Controls.Border>(template.FindName(surfaceName, control));
        }

        /// <summary>
        /// Describes the published theme state behind a brush assertion. A theme-token failure is
        /// almost always "the wrong dictionary is installed" rather than "the token is wrong", and
        /// on a CI runner there is no debugger to ask, so the answer has to travel in the message.
        /// </summary>
        /// <param name="application">The test application.</param>
        /// <param name="background">The brush the assertion read.</param>
        /// <returns>A single-line description of the resolved theme and the installed dictionaries.</returns>
        private static string DescribeThemeState(Application application, SolidColorBrush background)
        {
            object? token = application.TryFindResource("AcrylicBackgroundFillColorDefault");
            string tokenText = token is Color color
                ? color.ToString(CultureInfo.InvariantCulture)
                : "missing";
            IEnumerable<string> sources = application.Resources.MergedDictionaries
                .Select(static dictionary => dictionary.Source?.ToString() ?? "computed");

            return "brush=" + background.Color.ToString(CultureInfo.InvariantCulture)
                + " token=" + tokenText
                + " requestedTheme=" + ThemeName(ApplicationThemeManager.CurrentTheme)
                + " highContrastSetting=" + SystemParameters.HighContrast.ToString(CultureInfo.InvariantCulture)
                + " mergedDictionaries=[" + string.Join(", ", sources) + "]";
        }

        /// <summary>
        /// Names a theme without <c language="csharp">Enum.GetName</c>, whose generic overload the analyzers demand on
        /// net10 and which does not exist on net472.
        /// </summary>
        /// <param name="theme">The theme to name.</param>
        /// <returns>The theme name.</returns>
        private static string ThemeName(ApplicationTheme theme)
        {
            return theme switch
            {
                ApplicationTheme.Light => "Light",
                ApplicationTheme.Dark => "Dark",
                ApplicationTheme.HighContrast => "HighContrast",
                ApplicationTheme.Auto => "Auto",
                _ => "Unknown",
            };
        }

        private static void ResetApplication(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
        }

        private static void AssertTypographyMetrics(
            Application application,
            string styleKey,
            double expectedFontSize,
            FontWeight expectedFontWeight,
            double expectedLineHeight)
        {
            Style style = Assert.IsType<Style>(application.TryFindResource(styleKey));

            System.Windows.Controls.TextBlock textBlock = new()
            {
                Style = style,
            };

            Assert.Equal(expectedFontSize, textBlock.FontSize, 0.01d);
            Assert.Equal(expectedFontWeight, textBlock.FontWeight);
            Assert.Equal(expectedLineHeight, textBlock.LineHeight, 0.01d);
            Assert.Equal(
                LineStackingStrategy.BlockLineHeight,
                textBlock.LineStackingStrategy);
            Assert.NotNull(textBlock.Foreground);
        }

        private static IEnumerable<string> EnumerateProductionSources(IEnumerable<string> roots)
        {
            foreach (string root in roots)
            {
                foreach (string extension in new[] { "*.cs", "*.xaml" })
                {
                    foreach (string path in Directory.EnumerateFiles(root, extension, SearchOption.AllDirectories))
                    {
                        if (IsGeneratedOutputPath(path))
                        {
                            continue;
                        }

                        yield return path;
                    }
                }
            }
        }

        private static bool IsGeneratedOutputPath(string path)
        {
            string normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string separator = Path.DirectorySeparatorChar.ToString();
            return normalized.Contains(separator + "bin" + separator, StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(separator + "obj" + separator, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<List<string>> FindBannedFragmentsAsync(string path, IEnumerable<string> bannedFragments)
        {
            string source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
            List<string> found = [];
            foreach (string bannedFragment in bannedFragments.Where(bannedFragment => source.Contains(bannedFragment, StringComparison.Ordinal)))
            {
                found.Add(GetRepoRelativePath(path) + ": " + bannedFragment);
            }

            return found;
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
                if (File.Exists(Path.Join(directory.FullName, "Fluence.Wpf.sln")))
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
