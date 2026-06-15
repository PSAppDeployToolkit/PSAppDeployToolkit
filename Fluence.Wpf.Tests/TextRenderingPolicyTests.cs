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

using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfBorder = System.Windows.Controls.Border;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class TextRenderingPolicyTests
    {
        [TestMethod]
        public void FluenceWindow_DefaultStyleOwnsCrispRootRenderingPolicy()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = WpfTestSta.EnsureApplication();
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

                    Assert.IsTrue(window.UseLayoutRounding, "FluenceWindow should enable layout rounding at the root.");
                    Assert.IsTrue(window.SnapsToDevicePixels, "FluenceWindow should snap device pixels at the root.");
                    Assert.AreEqual(
                        ClearTypeHint.Auto,
                        RenderOptions.GetClearTypeHint(window),
                        "FluenceWindow must leave RenderOptions.ClearTypeHint at the WPF default (Auto) so the renderer picks ClearType for opaque surfaces and grayscale anti-aliasing for translucent surfaces. Forcing Enabled blocks the fallback and produces soft text over Mica / Acrylic / accent-backdrop layers.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_ChildInheritsPixelAlignmentPolicy()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                WpfBorder child = new();
                FluenceWindow window = new()
                {
                    Width = 320,
                    Height = 240,
                    Content = child,
                };

                try
                {
                    _ = window.ApplyTemplate();

                    Assert.IsTrue(child.UseLayoutRounding, "Children should inherit root layout rounding.");
                    Assert.IsTrue(child.SnapsToDevicePixels, "Children should inherit root device-pixel snapping.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ProductionSources_DoNotSetWpfTextOptionsRenderingPolicy()
        {
            string repoRoot = FindRepoRoot();
            string[] productionRoots =
            [
                Path.Combine(repoRoot, "Fluence.Wpf"),
                Path.Combine(repoRoot, "Fluence.Wpf.Demo"),
                Path.Combine(repoRoot, "Fluence.Wpf.Demo.Mvvm"),
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

            string[] offenders =
            [
                .. EnumerateProductionSources(productionRoots)
                    .SelectMany(path => FindBannedFragments(path, bannedFragments)),
            ];

            Assert.AreEqual(
                0,
                offenders.Length,
                "Production sources should not set WPF TextOptions rendering policy: " + string.Join(Environment.NewLine, offenders));
        }

        [TestMethod]
        public void ProductionSources_SetDevicePixelSnappingOnlyOnFluenceWindowRoot()
        {
            string repoRoot = FindRepoRoot();
            string[] productionRoots =
            [
                Path.Combine(repoRoot, "Fluence.Wpf"),
                Path.Combine(repoRoot, "Fluence.Wpf.Demo"),
                Path.Combine(repoRoot, "Fluence.Wpf.Demo.Mvvm"),
            ];

            string allowedPath = Path.Combine(
                "Fluence.Wpf",
                "Themes",
                "Controls",
                "FluenceWindow.xaml");
            string[] offenders =
            [
                .. EnumerateProductionSources(productionRoots)
                    .Where(path => !string.Equals(GetRepoRelativePath(path), allowedPath, StringComparison.OrdinalIgnoreCase) && File.ReadAllText(path).IndexOf("SnapsToDevicePixels", StringComparison.Ordinal) >= 0)
                    .Select(GetRepoRelativePath),
            ];

            Assert.AreEqual(
                0,
                offenders.Length,
                "Only FluenceWindow should set SnapsToDevicePixels: " + string.Join(Environment.NewLine, offenders));
        }

        [TestMethod]
        public void TypographyStyles_ApplyTypeRampMetrics()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = WpfTestSta.EnsureApplication();
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

        [TestMethod]
        public void TextBlockExtensions_Typography_AppliesTypeRampStyleOnly()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                ResetApplication(application);

                WpfTextBlock textBlock = new();
                textBlock.SetTypography(FluentTypography.Title);

                Assert.AreSame(
                    application?.TryFindResource("TitleTextBlockStyle"),
                    textBlock.Style,
                    "Attached typography should use the named XAML style resource as its source of truth.");
                Assert.AreEqual(28d, textBlock.FontSize, 0.01d);
                Assert.AreEqual(FontWeights.SemiBold, textBlock.FontWeight);
                Assert.AreEqual(36d, textBlock.LineHeight, 0.01d);
                Assert.AreEqual(LineStackingStrategy.BlockLineHeight, textBlock.LineStackingStrategy);
            });
        }

        [TestMethod]
        public void TextBlockExtensions_TypographyNone_DoesNotMutateExistingMetrics()
        {
            WpfTestSta.Invoke(static () =>
            {
                WpfTextBlock textBlock = new();
                textBlock.SetTypography(FluentTypography.Body);

                FontFamily fontFamily = new("Arial");
                textBlock.FontFamily = fontFamily;
                textBlock.FontSize = 13;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.LineHeight = 17;
                textBlock.LineStackingStrategy = LineStackingStrategy.MaxHeight;

                textBlock.SetTypography(FluentTypography.None);

                Assert.AreEqual(fontFamily, textBlock.FontFamily);
                Assert.AreEqual(13d, textBlock.FontSize, 0.01d);
                Assert.AreEqual(FontWeights.Bold, textBlock.FontWeight);
                Assert.AreEqual(17d, textBlock.LineHeight, 0.01d);
                Assert.AreEqual(LineStackingStrategy.MaxHeight, textBlock.LineStackingStrategy);
            });
        }

        private static void ResetApplication(Application? application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
        }

        private static void AssertTypographyMetrics(
            Application? application,
            string styleKey,
            double expectedFontSize,
            FontWeight expectedFontWeight,
            double expectedLineHeight)
        {
            Style? style = application?.TryFindResource(styleKey) as Style;
            Assert.IsNotNull(style, styleKey + " should resolve.");

            WpfTextBlock textBlock = new()
            {
                Style = style,
            };

            Assert.AreEqual(expectedFontSize, textBlock.FontSize, 0.01d, styleKey + " should set FontSize.");
            Assert.AreEqual(expectedFontWeight, textBlock.FontWeight, styleKey + " should set FontWeight.");
            Assert.AreEqual(expectedLineHeight, textBlock.LineHeight, 0.01d, styleKey + " should set LineHeight.");
            Assert.AreEqual(
                LineStackingStrategy.BlockLineHeight,
                textBlock.LineStackingStrategy,
                styleKey + " should set line-height stacking.");
            Assert.IsNotNull(textBlock.Foreground, styleKey + " should resolve a foreground brush.");
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
            return normalized.IndexOf(separator + "bin" + separator, StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf(separator + "obj" + separator, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<string> FindBannedFragments(string path, IEnumerable<string> bannedFragments)
        {
            string source = File.ReadAllText(path);
            foreach (string bannedFragment in bannedFragments)
            {
                if (source.IndexOf(bannedFragment, StringComparison.Ordinal) >= 0)
                {
                    yield return GetRepoRelativePath(path) + ": " + bannedFragment;
                }
            }
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
