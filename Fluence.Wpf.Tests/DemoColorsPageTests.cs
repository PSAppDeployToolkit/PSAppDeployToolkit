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
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public sealed class DemoColorsPageTests
    {
        private static readonly string[] SectionNames =
        [
            "Text",
            "Fill",
            "Stroke",
            "Background",
            "Signal",
            "High Contrast",
        ];

        [TestMethod]
        public void GalleryColorsPage_NavigationRoute_LoadsConcretePage()
        {
            WpfTestSta.Invoke(static delegate
            {
                _ = EnsureDemoTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    window.NavigateTo("colors");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    NavigationView? navigationView = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(navigationView, "DemoNav should exist.");
                    Assert.IsInstanceOfType(navigationView.Content, typeof(GalleryColorsPage),
                        "The colors route should load the concrete Colors page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryColorsPage_UsesWinUiGalleryColorStructure()
        {
            WpfTestSta.Invoke(static delegate
            {
                _ = EnsureDemoTheme();
                GalleryColorsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    SmoothScrollViewer? scrollViewer = FindVisualChild<SmoothScrollViewer>(page);
                    Assert.IsNotNull(scrollViewer, "Colors page should use the shared gallery SmoothScrollViewer.");

                    TabControl? colorTabs = FindByName<TabControl>(page, "ColorSectionTabs");
                    Assert.IsNotNull(colorTabs, "Colors page should use a TabControl for the color sections.");
                    Assert.AreEqual(SectionNames.Length, colorTabs.Items.Count,
                        "Colors page should expose the six WinUI Gallery color sections as tabs.");

                    for (int i = 0; i < SectionNames.Length; i++)
                    {
                        TabItem tabItem = (TabItem)colorTabs.Items[i];
                        Assert.AreEqual(SectionNames[i], tabItem.Header as string,
                            "Unexpected Colors page section at index " + i.ToString(CultureInfo.InvariantCulture) + ".");
                    }

                    List<string> exampleTitles = [.. FindVisualChildren<System.Windows.Controls.TextBlock>(page)
                        .Where(static text => string.Equals(text.Tag as string, "ColorExampleTitle", StringComparison.Ordinal))
                        .Select(static text => text.Text)];
                    CollectionAssert.AreEqual(
                        new[] { "Text", "Accent Text", "Text On Accent" },
                        exampleTitles,
                        "The Text tab should lead with the primary WPF Gallery color examples.");

                    Assert.AreEqual(0, FindVisualChildren<WrapPanel>(page).Count(),
                        "Colors page tile sections should not use WrapPanel layout.");
                    Assert.AreEqual(0, FindVisualChildren<DemoSampleControl>(page).Count(),
                        "Colors page should be a native guidance page, not a DemoSampleControl wrapper.");

                    int totalTiles = 0;
                    bool sawSystemColorAlias = false;
                    bool sawAccentFill = false;
                    for (int i = 0; i < colorTabs.Items.Count; i++)
                    {
                        SelectTab(colorTabs, i, window.Dispatcher);

                        List<UniformGrid> rows = [.. FindVisualChildren<UniformGrid>(page)
                            .Where(static row => string.Equals((row.Parent as FrameworkElement)?.Tag as string, "ColorTokenRow", StringComparison.Ordinal))];
                        Assert.IsTrue(rows.Count > 0, "Selected Colors page section should contain token rows.");

                        foreach (UniformGrid row in rows)
                        {
                            Assert.AreEqual(row.Children.Count, row.Columns,
                                "Each token row should declare one equal-width column per tile.");
                            Assert.IsTrue(row.Columns <= 4, "Token rows should stay compact at four columns or fewer.");

                            foreach (UIElement child in row.Children)
                            {
                                FrameworkElement tile = (FrameworkElement)child;
                                string resourceKey = tile.Tag as string ?? string.Empty;
                                Assert.IsFalse(string.IsNullOrWhiteSpace(resourceKey), "Each token tile should expose its resource key.");
                                totalTiles++;
                                sawSystemColorAlias |= string.Equals(resourceKey, "SystemColorWindowTextColorBrush", StringComparison.Ordinal);
                                sawAccentFill |= string.Equals(resourceKey, "AccentFillColorDefaultBrush", StringComparison.Ordinal);
                            }
                        }
                    }

                    Assert.IsTrue(totalTiles >= 90, "Colors page should expose the WinUI-style brush catalogue through token tiles.");
                    Assert.IsTrue(sawSystemColorAlias, "High Contrast section should use SystemColor alias resources.");
                    Assert.IsTrue(sawAccentFill, "Fill section should include accent fill resources.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void GalleryColorsPage_DynamicResourceKeys_ResolveAcrossThemes()
        {
            WpfTestSta.Invoke(static delegate
            {
                Application application = EnsureDemoTheme();
                GalleryColorsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    SortedSet<string> resourceKeys = CollectColorTokenResourceKeys(page, window.Dispatcher);
                    Assert.IsTrue(resourceKeys.Count >= 90, "Colors page should expose enough token keys to cover the Fluent color families.");

                    ApplicationTheme[] themes =
                    [
                        ApplicationTheme.Light,
                        ApplicationTheme.Dark,
                        ApplicationTheme.HighContrast,
                    ];

                    List<string> unresolved = [];
                    foreach (ApplicationTheme theme in themes)
                    {
                        ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                        ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                        foreach (string resourceKey in resourceKeys)
                        {
                            if (application.TryFindResource(resourceKey) is null)
                            {
                                unresolved.Add(theme + ": " + resourceKey);
                            }
                        }
                    }

                    Assert.AreEqual(0, unresolved.Count,
                        "Colors page should only reference resource keys that resolve: " +
                        string.Join("; ", unresolved));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void GalleryColorsPage_SourceAvoidsLegacyControlsAndLiteralForegrounds()
        {
            string pageXaml = ReadRepositoryFile("Fluence.Wpf.Demo", "Pages", "GalleryColorsPage.xaml");
            string pageCode = ReadRepositoryFile("Fluence.Wpf.Demo", "Pages", "GalleryColorsPage.xaml.cs");
            string source = pageXaml + Environment.NewLine + pageCode;

            string[] forbidden =
            [
                "ColorGuidance",
                "GalleryBackgroundBrush",
                "Foreground=\"Black\"",
                "Foreground=\"White\"",
                "Foreground=\"#",
                "WrapPanel",
                "SectionSelectorHost",
                "FluenceToggleButton",
            ];

            List<string> violations = [];
            foreach (string value in forbidden)
            {
                if (source.IndexOf(value, StringComparison.Ordinal) >= 0)
                {
                    violations.Add(value);
                }
            }

            Assert.AreEqual(0, violations.Count,
                "Colors page should avoid legacy color controls and literal foregrounds: " +
                string.Join("; ", violations));
        }

        private static SortedSet<string> CollectColorTokenResourceKeys(GalleryColorsPage page, Dispatcher dispatcher)
        {
            SortedSet<string> resourceKeys = [];
            TabControl? colorTabs = FindByName<TabControl>(page, "ColorSectionTabs");
            Assert.IsNotNull(colorTabs, "Colors page should expose color section tabs.");

            for (int index = 0; index < colorTabs.Items.Count; index++)
            {
                SelectTab(colorTabs, index, dispatcher);
                foreach (UniformGrid row in FindVisualChildren<UniformGrid>(page)
                    .Where(static row => string.Equals((row.Parent as FrameworkElement)?.Tag as string, "ColorTokenRow", StringComparison.Ordinal)))
                {
                    foreach (UIElement child in row.Children)
                    {
                        if (child is FrameworkElement { Tag: string resourceKey } &&
                            !string.IsNullOrWhiteSpace(resourceKey))
                        {
                            _ = resourceKeys.Add(resourceKey);
                        }
                    }
                }
            }

            return resourceKeys;
        }

        private static void SelectTab(TabControl colorTabs, int index, Dispatcher dispatcher)
        {
            colorTabs.SelectedIndex = index;
            Drain(dispatcher);
            colorTabs.UpdateLayout();
            Drain(dispatcher);
        }

        private static Application EnsureDemoTheme()
        {
            Application application = WpfTestSta.EnsureApplication() ?? throw new InvalidOperationException("WPF application was not created.");
            foreach (Window window in (Window[])[.. application.Windows.Cast<Window>()])
            {
                window.Content = null;
                window.Close();
            }

            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            };
            application.Resources.MergedDictionaries.Add(demoShared);
            return application;
        }

        private static MainWindow CreateShownMainWindow()
        {
            MainWindow window = new()
            {
                Left = -20000,
                Top = -20000,
                Width = 1200,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
            };
            window.Show();
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
            return window;
        }

        private static Window CreateHostWindow(UIElement content)
        {
            Window window = new()
            {
                Left = -20000,
                Top = -20000,
                Width = 1040,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
                Content = content,
            };
            window.Show();
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
            return window;
        }

        private static void CloseWindowAndDrain(Window window)
        {
            window.Content = null;
            window.Close();
            Drain(window.Dispatcher);
        }

        private static void Drain(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));
        }

        private static T? FindByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            foreach (T item in FindVisualChildren<T>(root))
            {
                if (string.Equals(item.Name, name, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject root)
            where T : DependencyObject
        {
            return FindVisualChildren<T>(root).FirstOrDefault();
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? root)
            where T : DependencyObject
        {
            HashSet<DependencyObject> visited = [];
            foreach (T item in FindVisualChildren<T>(root, visited))
            {
                yield return item;
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(
            DependencyObject? root,
            HashSet<DependencyObject> visited)
            where T : DependencyObject
        {
            if (root is null || !visited.Add(root))
            {
                yield break;
            }

            if (root is T match)
            {
                yield return match;
            }

            int visualChildren = 0;
            if (root is Visual or Visual3D)
            {
                visualChildren = VisualTreeHelper.GetChildrenCount(root);
            }

            for (int i = 0; i < visualChildren; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                foreach (T item in FindVisualChildren<T>(child, visited))
                {
                    yield return item;
                }
            }

            foreach (object logicalChild in LogicalTreeHelper.GetChildren(root))
            {
                if (logicalChild is DependencyObject dependencyObject)
                {
                    foreach (T item in FindVisualChildren<T>(dependencyObject, visited))
                    {
                        yield return item;
                    }
                }
            }
        }

        private static string GetRepositoryFilePath(params string[] relativeSegments)
        {
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string[] pathParts = new string[relativeSegments.Length + 1];
            pathParts[0] = root;
            Array.Copy(relativeSegments, 0, pathParts, 1, relativeSegments.Length);
            return Path.Combine(pathParts);
        }

        private static string ReadRepositoryFile(params string[] relativeSegments)
        {
            string path = GetRepositoryFilePath(relativeSegments);
            Assert.IsTrue(File.Exists(path), "Repository file must be readable at: " + path);
            return File.ReadAllText(path);
        }
    }
}
