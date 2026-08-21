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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using Fluence.Wpf.Demo.Pages;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public sealed class DemoSamplePageWiringTests
    {
        private const string IntentionalPartialSnippetMarker = "Intentionally partial layout snippet";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        // GalleryIconsPage is a design reference page (WinUI Gallery Iconography catalog)
        // and renders directly instead of through DemoSampleControl, like Typography.
        private static readonly Func<UIElement>[] SamplePageFactories =
        [
            static () => new GalleryAccessibilityPage(),
            static () => new GalleryButtonsPage(),
            static () => new GallerySelectionPage(),
            static () => new GalleryInputsPage(),
            static () => new GalleryFormsPage(),
            static () => new GalleryDataPage(),
            static () => new GalleryDataBindingPage(),
            static () => new GalleryTreesPage(),
            static () => new GalleryMenusPage(),
            static () => new GalleryNavigationPage(),
            static () => new GalleryTabsPage(),
            static () => new GalleryLayoutPage(),
            static () => new GalleryStatusPage(),
        ];

        [Fact]
        public Task DemoSamplePageWiring_MovesSlotContentAndAppliesTypedSourcesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                TextBlock demoContent = new() { Text = "Demo" };
                TextBlock outputContent = new() { Text = "Output" };
                CheckBox rightRailContent = new() { Content = "Option" };
                ContentControl demoSlot = CreateSlot("DemoSampleSlot01DemoContentHost", demoContent);
                ContentControl outputSlot = CreateSlot("DemoSampleSlot01OutputContentHost", outputContent);
                ContentControl rightRailSlot = CreateSlot("DemoSampleSlot01RightRailContentHost", rightRailContent);
                DemoSampleControl sample = new();
                StackPanel root = new();
                _ = root.Children.Add(demoSlot);
                _ = root.Children.Add(outputSlot);
                _ = root.Children.Add(rightRailSlot);
                _ = root.Children.Add(sample);

                DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", "public void Demo() { }"));

                Assert.Same(demoContent, sample.DemoContent);
                Assert.Same(outputContent, sample.OutputContent);
                Assert.Same(rightRailContent, sample.RightRailContent);
                Assert.Null(demoSlot.Content);
                Assert.Null(outputSlot.Content);
                Assert.Null(rightRailSlot.Content);
                Assert.Equal("<Grid />", sample.XamlSource, StringComparer.Ordinal);
                Assert.Equal("public void Demo() { }", sample.CSharpSource, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task DemoSamplePageWiring_RejectsSourceCountMismatchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(new DemoSampleControl());
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [Fact]
        public Task DemoSamplePageWiring_RejectsDuplicateSourceSlotsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(
                        root,
                        new DemoSampleSource(1, "<Grid />", string.Empty),
                        new DemoSampleSource(1, "<StackPanel />", string.Empty)));
            });
        }

        [Fact]
        public Task DemoSamplePageWiring_RejectsUnusedContentSlotsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot02DemoContentHost", new TextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [Fact]
        public Task DemoSamplePageWiring_RejectsZeroContentSlotAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot00DemoContentHost", new TextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [Fact]
        public Task DemoSamplePageWiring_RejectsDuplicateContentSlotsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot01DemoContentHost", new TextBlock()));
                _ = root.Children.Add(CreateSlot("DemoSampleSlot01DemoContentHost", new TextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [Fact]
        public Task DemoSampleControl_ReloadsExpandedSourceTabsWhenSourceChangesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                DemoSampleControl sample = new()
                {
                    DemoContent = new TextBlock { Text = "Body" },
                    XamlSource = "<Grid />",
                };
                Window window = DemoTestHost.CreateHostWindow(sample);
                try
                {
                    Controls.Expander expander = Assert.IsAssignableFrom<Controls.Expander>(DemoTestHost.FindByName<Controls.Expander>(sample, "SourceExpander"));
                    expander.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertSourceCopyTag(sample, "<Grid />");
                    sample.XamlSource = "<StackPanel />";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertSourceCopyTag(sample, "<StackPanel />");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GallerySamplePages_AllVisibleDemoSamplesExposeSourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                foreach (Func<UIElement> factory in SamplePageFactories)
                {
                    _ = DemoTestHost.EnsureDemoTheme();
                    UIElement page = factory();
                    Window window = DemoTestHost.CreateHostWindow(page);
                    try
                    {
                        List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                        Assert.True(samples.Count > 0, "Page should expose DemoSampleControl samples: " + page.GetType().Name);
                        foreach (DemoSampleControl sample in samples.Where(static sample => sample.Visibility is Visibility.Visible))
                        {
                            Assert.False(string.IsNullOrWhiteSpace(sample.XamlSource),
                                "Visible DemoSampleControl should expose XAML source: " + page.GetType().Name);
                        }
                    }
                    finally
                    {
                        DemoTestHost.CloseWindow(window);
                    }
                }
            });
        }

        [Fact]
        public Task GallerySamplePages_SourceContractsMatchDisplayedClassesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                foreach (DemoSampleControl sample in CreateVisibleSamples())
                {
                    string xamlSource = sample.XamlSource.Trim();
                    string csharpSource = sample.CSharpSource.Trim();
                    if (IsIntentionalPartialSnippet(xamlSource))
                    {
                        Assert.True(
                            string.IsNullOrWhiteSpace(csharpSource),
                            "Intentional partial snippets should not display a code-behind class.");
                        continue;
                    }

                    XDocument document = ParseXamlSource(xamlSource, sample.SampleDescription);
                    Assert.Equal(
                        "UserControl",
                        document.Root?.Name.LocalName, StringComparer.Ordinal);

                    string xamlClass = document.Root?.Attribute(XamlNamespace + "Class")?.Value
                        ?? throw new Xunit.Sdk.XunitException("Displayed UserControl XAML must declare x:Class: " + sample.SampleDescription);
                    Assert.False(
                        string.IsNullOrWhiteSpace(csharpSource),
                        "Displayed UserControl XAML should include matching C# source: " + sample.SampleDescription);
                    Assert.True(
                        csharpSource.Contains("InitializeComponent();", StringComparison.Ordinal),
                        "Displayed C# source should use the UserControl InitializeComponent pattern: " + sample.SampleDescription);
                    Assert.Equal(
                        xamlClass,
                        GetDeclaredPartialClassName(csharpSource), StringComparer.Ordinal);
                }
            });
        }

        [Fact]
        public Task GallerySamplePages_CSharpSourcesUseReleaseReadySnippetStyleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                foreach (DemoSampleControl sample in CreateVisibleSamples())
                {
                    string csharpSource = sample.CSharpSource;
                    if (string.IsNullOrWhiteSpace(csharpSource))
                    {
                        continue;
                    }

                    AssertDoesNotContainVar(csharpSource, sample.SampleDescription);
                    AssertNoUninitializedNonNullableSnippetProperties(csharpSource, sample.SampleDescription);
                }
            });
        }

        private static ContentControl CreateSlot(string name, object content)
        {
            return new ContentControl
            {
                Name = name,
                Content = content,
                Visibility = Visibility.Collapsed,
            };
        }

        private static void AssertSourceCopyTag(DemoSampleControl sample, string expectedSource)
        {
            TabControl tabs = Assert.IsAssignableFrom<TabControl>(DemoTestHost.FindByName<TabControl>(sample, "SourceTabControl"));
            _ = Assert.Single(tabs.Items);
            TabItem tab = (TabItem)tabs.Items[0];
            Button copy = Assert.IsAssignableFrom<Button>(DemoTestHost.FindByName<Button>(tab.Content as DependencyObject, "CopySourceButton"));
            Assert.Equal(expectedSource, copy.Tag as string, StringComparer.Ordinal);
        }

        private static List<DemoSampleControl> CreateVisibleSamples()
        {
            List<DemoSampleControl> samples = [];
            foreach (Func<UIElement> factory in SamplePageFactories)
            {
                _ = DemoTestHost.EnsureDemoTheme();
                UIElement page = factory();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    samples.AddRange(DemoTestHost.FindVisualChildren<DemoSampleControl>(page)
                        .Where(static sample => sample.Visibility is Visibility.Visible));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            }

            return samples;
        }

        private static bool IsIntentionalPartialSnippet(string xamlSource)
        {
            return xamlSource.StartsWith("<!--", StringComparison.Ordinal) &&
                xamlSource.Contains(IntentionalPartialSnippetMarker, StringComparison.Ordinal);
        }

        private static XDocument ParseXamlSource(string xamlSource, string sampleDescription)
        {
            try
            {
                return XDocument.Parse(xamlSource);
            }
            catch (XmlException exception)
            {
                throw new Xunit.Sdk.XunitException("Displayed XAML source must be well formed: " + sampleDescription, exception);
            }
        }

        private static string GetDeclaredPartialClassName(string csharpSource)
        {
            const string classPrefix = "public partial class ";
            string namespaceName = GetNamespaceName(csharpSource);
            if (SplitLines(csharpSource).Select(static line => line.Trim()).FirstOrDefault(static line => line.StartsWith(classPrefix, StringComparison.Ordinal)) is not string line)
            {
                throw new Xunit.Sdk.XunitException("Displayed C# source must declare a public partial class.");
            }
            string classRemainder = line[classPrefix.Length..];
            int classNameEnd = classRemainder.IndexOfAny([' ', ':']);
            string className = classNameEnd < 0 ? classRemainder : classRemainder[..classNameEnd];
            return namespaceName + "." + className;
        }

        private static string GetNamespaceName(string csharpSource)
        {
            const string namespacePrefix = "namespace ";
            return SplitLines(csharpSource).Select(static line => line.Trim()).FirstOrDefault(static line => line.StartsWith(namespacePrefix, StringComparison.Ordinal)) is not string line
                ? throw new Xunit.Sdk.XunitException("Displayed C# source must declare a namespace.")
                : line[namespacePrefix.Length..].Trim();
        }

        private static void AssertDoesNotContainVar(string csharpSource, string sampleDescription)
        {
            int lineNumber = 0;
            foreach (string line in SplitLines(csharpSource))
            {
                lineNumber++;
                if (ContainsWord(line, "var"))
                {
                    Assert.Fail("Displayed C# source should use explicit types: " + sampleDescription + " line " + lineNumber.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void AssertNoUninitializedNonNullableSnippetProperties(string csharpSource, string sampleDescription)
        {
            int lineNumber = 0;
            foreach (string line in SplitLines(csharpSource))
            {
                lineNumber++;
                string trimmed = line.Trim();
                bool isNonNullableAutoProperty =
                    (trimmed.StartsWith("public string ", StringComparison.Ordinal) ||
                     trimmed.StartsWith("public Brush ", StringComparison.Ordinal)) &&
                    trimmed.Contains("{ get; set; }", StringComparison.Ordinal) &&
                    !trimmed.Contains("=", StringComparison.Ordinal);

                if (isNonNullableAutoProperty)
                {
                    Assert.Fail("Displayed C# source should initialize non-nullable auto properties: " + sampleDescription + " line " + lineNumber.ToString(format: null, CultureInfo.InvariantCulture));
                }
            }
        }

        private static string[] SplitLines(string text)
        {
            return text.Split(["\r\n", "\n"], StringSplitOptions.None);
        }

        private static bool ContainsWord(string text, string word)
        {
            int index = text.IndexOf(word, StringComparison.Ordinal);
            while (index >= 0)
            {
                bool startsOnBoundary = index is 0 || !IsWordCharacter(text[index - 1]);
                int end = index + word.Length;
                bool endsOnBoundary = end == text.Length || !IsWordCharacter(text[end]);
                if (startsOnBoundary && endsOnBoundary)
                {
                    return true;
                }

                index = text.IndexOf(word, index + word.Length, StringComparison.Ordinal);
            }

            return false;
        }

        private static bool IsWordCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private static void AssertThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Assert.Fail("Expected InvalidOperationException.");
        }
    }
}
