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
using System.Windows.Media;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Guards the generated design-time color + brush snapshots
    /// (<c>Fluence.Wpf/Properties/DesignTime.{Light,Dark}.xaml</c>) against drift from the theme
    /// engine, and verifies they are valid loadable XAML. The snapshot is produced by
    /// <see cref="DesignTimeResourceWriter"/> from <c>FluenceThemeEngine.BuildStandalone</c>.
    /// </summary>
    [TestClass]
    public class DesignTimeResourceTests
    {
        /// <summary>
        /// Drift guard. Regenerates each snapshot in memory and asserts it matches the committed
        /// file (newline-normalized). Any new or changed Color/brush key in the engine fails this
        /// until the files are regenerated via <see cref="RegenerateDesignTimeResources"/>.
        /// </summary>
        [TestMethod]
        public void DesignTimeResources_AreCurrent()
        {
            WpfTestSta.Invoke(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                AssertCurrent(ApplicationTheme.Light);
                AssertCurrent(ApplicationTheme.Dark);
            });
        }

        private static void AssertCurrent(ApplicationTheme theme)
        {
            string path = DesignTimeResourceWriter.PathFor(theme);
            Assert.IsTrue(File.Exists(path), string.Format("Committed design-time file is missing: {0}", path));

            string expected = Normalize(DesignTimeResourceWriter.Generate(theme));
            string actual = Normalize(File.ReadAllText(path));

            Assert.AreEqual(expected, actual, string.Format(
                "{0} is out of date with the theme engine. Run the RegenerateDesignTimeResources test and re-commit.",
                Path.GetFileName(path)));
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n");
        }

        /// <summary>
        /// Loads each generated file as a <see cref="ResourceDictionary"/> from the compiled
        /// assembly (the same pack URI a consumer would reference at design time) and asserts a
        /// representative set of keys resolve. Verifies the files are valid loadable XAML, not just
        /// string-equal to the generator.
        /// </summary>
        [TestMethod]
        public void DesignTimeResources_Load_RepresentativeKeysResolve()
        {
            WpfTestSta.Invoke(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                AssertLoads("DesignTime.Light.xaml");
                AssertLoads("DesignTime.Dark.xaml");
            });
        }

        private static void AssertLoads(string fileName)
        {
            Uri uri = new("pack://application:,,,/Fluence.Wpf;component/Properties/" + fileName, UriKind.Absolute);
            ResourceDictionary dict = new() { Source = uri };

            _ = Assert.IsInstanceOfType<SolidColorBrush>(dict["TextFillColorPrimaryBrush"],
                fileName + ": TextFillColorPrimaryBrush must be a SolidColorBrush.");
            _ = Assert.IsInstanceOfType<SolidColorBrush>(dict["AccentFillColorDefaultBrush"],
                fileName + ": AccentFillColorDefaultBrush must be a SolidColorBrush.");
            _ = Assert.IsInstanceOfType<SolidColorBrush>(dict["CardBackgroundFillColorDefaultBrush"],
                fileName + ": CardBackgroundFillColorDefaultBrush must be a SolidColorBrush.");
            _ = Assert.IsInstanceOfType<SolidColorBrush>(dict["SystemAccentColorBrush"],
                fileName + ": SystemAccentColorBrush must be a SolidColorBrush.");
            _ = Assert.IsInstanceOfType<LinearGradientBrush>(dict["ControlElevationBorderBrush"],
                fileName + ": ControlElevationBorderBrush must be a LinearGradientBrush.");
            _ = Assert.IsInstanceOfType<CornerRadius>(dict["ControlCornerRadius"],
                fileName + ": ControlCornerRadius must be a CornerRadius.");

            // Default accent #0078D4 must be the snapshot's accent.
            Color accent = (Color)dict["SystemAccentColor"];
            Assert.AreEqual(Color.FromRgb(0x00, 0x78, 0xD4), accent,
                fileName + ": SystemAccentColor must equal the default accent #0078D4.");
        }

        /// <summary>
        /// Maintainer-only regenerator. Normally <c>[Ignore]</c>d so it never runs in CI; remove the
        /// attribute (or run it explicitly) after an intentional engine change, then re-commit the
        /// updated <c>DesignTime.{Light,Dark}.xaml</c> files.
        /// </summary>
        [SlopwatchSuppress("SW001", "Maintainer-only file generator that rewrites committed DesignTime.{Light,Dark}.xaml; must not run in CI. DesignTimeResources_AreCurrent is the CI guard.")]
        [TestMethod]
        [Ignore("Maintainer-only: writes the committed DesignTime.{Light,Dark}.xaml files. Run manually after an intentional engine change.")]
        public void RegenerateDesignTimeResources()
        {
            WpfTestSta.Invoke(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                DesignTimeResourceWriter.WriteToDisk(ApplicationTheme.Light);
                DesignTimeResourceWriter.WriteToDisk(ApplicationTheme.Dark);

                Assert.IsTrue(File.Exists(DesignTimeResourceWriter.PathFor(ApplicationTheme.Light)),
                    "DesignTime.Light.xaml should exist after regeneration.");
                Assert.IsTrue(File.Exists(DesignTimeResourceWriter.PathFor(ApplicationTheme.Dark)),
                    "DesignTime.Dark.xaml should exist after regeneration.");
            });
        }
    }
}
