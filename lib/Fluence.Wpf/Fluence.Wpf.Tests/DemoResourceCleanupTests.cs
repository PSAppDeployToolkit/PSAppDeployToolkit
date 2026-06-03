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

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public sealed class DemoResourceCleanupTests
    {
        [TestMethod]
        public void DesignTimeResourceDictionaries_Load()
        {
            DemoTestHost.RunOnSta(delegate
            {
                ResourceDictionary library = new()
                {
                    Source = new Uri("/Fluence.Wpf;component/Properties/DesignTimeResources.xaml", UriKind.Relative)
                };
                ResourceDictionary demo = new()
                {
                    Source = new Uri("/Fluence.Wpf.Demo;component/Properties/DesignTimeResources.xaml", UriKind.Relative)
                };

                // Brushes are built at runtime by FluenceThemeEngine and are not present in design-time
                // resources. Verify the Color tokens and control templates load correctly.
                Assert.IsNotNull(library["TextFillColorPrimary"],
                    "Library design-time resources should resolve theme Color tokens.");
                Assert.IsNotNull(demo["DemoSampleCardPadding"],
                    "Demo design-time resources should resolve demo shared styles.");
            });
        }

        [TestMethod]
        public void DemoThemeStartup_AppendsDemoStylesAfterFluenceDictionaries()
        {
            DemoTestHost.RunOnSta(delegate
            {
                Application application = WpfTestSta.EnsureApplication() ?? throw new InvalidOperationException("WPF application was not created.");
                try
                {
                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                    application.Resources.MergedDictionaries.Clear();
                    application.Resources.Clear();

                    ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica);
                    ApplicationAccentColorManager.ApplySystemAccent();
                    DemoTestHost.AddDemoSharedStyles(application);

                    Collection<ResourceDictionary> dictionaries = application.Resources.MergedDictionaries;
                    Assert.AreEqual(4, dictionaries.Count, "Demo startup should append one demo dictionary after the three Fluence dictionaries.");
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.IsFalse(IsDemoSharedStyles(dictionaries[i]),
                            "Fluence theme slot " + i + " should not be occupied by demo styles.");
                    }

                    Assert.IsTrue(IsDemoSharedStyles(dictionaries[3]),
                        "DemoSharedStyles should be appended after the Fluence theme slots.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.Mica);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.Mica);

                    Assert.AreEqual(4, dictionaries.Count, "Theme changes should not duplicate demo styles.");
                    Assert.IsTrue(IsDemoSharedStyles(dictionaries[3]),
                        "DemoSharedStyles should stay after the three Fluence dictionaries.");
                }
                finally
                {
                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                    application.Resources.MergedDictionaries.Clear();
                    application.Resources.Clear();
                }
            });
        }

        private static bool IsDemoSharedStyles(ResourceDictionary dictionary)
        {
            return dictionary.Source is not null &&
                dictionary.Source.OriginalString.IndexOf("DemoSharedStyles.xaml", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
