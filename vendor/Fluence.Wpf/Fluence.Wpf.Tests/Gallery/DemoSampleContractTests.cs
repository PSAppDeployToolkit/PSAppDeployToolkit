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
using System.Windows.Documents;
using System.Xml;
using System.Xml.Linq;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualGeometry;

namespace Fluence.Wpf.Tests.Gallery
{
    /// <summary>
    /// Contract tests for <see cref="DemoSampleControl"/> and <see cref="DemoSamplePageWiring"/>:
    /// slot wiring, source-tab rendering, and the release-ready snippet style every visible
    /// sample must follow.
    /// </summary>
    public sealed class DemoSampleContractTests
    {
        private const string IntentionalPartialSnippetMarker = "Intentionally partial layout snippet";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        // GalleryIconsPage is a design reference page (WinUI Gallery Iconography catalog)
        // and renders directly instead of through DemoSampleControl.
        private static readonly Func<UIElement>[] SamplePageFactories =
        [
            static () => new GalleryAccessibilityPage(),
            static () => new GalleryButtonsPage(),
            static () => new GalleryTypographyPage(),
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
                    Controls.Expander expander = Assert.IsType<Controls.Expander>(DemoTestHost.FindByName<Controls.Expander>(sample, "SourceExpander"), exactMatch: false);
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

        [Fact]
        public Task DemoSampleControl_ExpanderUsesInMemorySourceTabsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<fluence:Button Content=\"Save\" />",
                    CSharpSource = "private void Save_Click(object sender, RoutedEventArgs e) { }",
                    DemoContent = new TextBlock { Text = "Visible sample" },
                };

                Window window = DemoTestHost.CreateHostWindow(sample);
                try
                {
                    Controls.Expander expander = Assert.IsType<Controls.Expander>(DemoTestHost.FindByName<Controls.Expander>(sample, "SourceExpander"), exactMatch: false);
                    Assert.False(expander.IsExpanded, "Source starts collapsed.");

                    expander.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.SelectorBar tabs = Assert.IsType<Controls.SelectorBar>(DemoTestHost.FindByName<Controls.SelectorBar>(sample, "SourceSelector"), exactMatch: false);
                    Assert.Equal(2, tabs.Items.Count);
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                    AssertSourceTab(tabs, "C#", sample.CSharpSource);

                    Border sampleCard = Assert.IsType<Border>(DemoTestHost.FindByName<Border>(sample, "SampleCard"), exactMatch: false);
                    Assert.Equal(new CornerRadius(8, 8, 0, 0), sampleCard.CornerRadius);
                    Assert.Equal(new CornerRadius(0, 0, 8, 8), expander.CornerRadius);
                    Assert.Equal(new Thickness(1, 0, 1, 1), expander.BorderThickness);
                    Assert.Equal(GetVisualY(sampleCard, window) + sampleCard.ActualHeight, GetVisualY(expander, window), 0.5);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task DemoSampleControl_SourceRendererPreservesIndentationAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<Grid>\n    <TextBlock Text=\"Indented\" />\n</Grid>",
                    CSharpSource = "private void Save()\n{\n    string value = \"Indented\";\n}",
                    DemoContent = new TextBlock { Text = "Visible sample" },
                };

                Window window = DemoTestHost.CreateHostWindow(sample);
                try
                {
                    Controls.Expander expander = Assert.IsType<Controls.Expander>(DemoTestHost.FindByName<Controls.Expander>(sample, "SourceExpander"), exactMatch: false);
                    expander.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.SelectorBar tabs = Assert.IsType<Controls.SelectorBar>(DemoTestHost.FindByName<Controls.SelectorBar>(sample, "SourceSelector"), exactMatch: false);
                    string renderedXaml = GetSourceTabText(tabs, "XAML");
                    string renderedCSharp = GetSourceTabText(tabs, "C#");

                    Assert.Contains("    <TextBlock", renderedXaml, StringComparison.Ordinal);
                    Assert.Contains("    string value", renderedCSharp, StringComparison.Ordinal);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task DemoSampleControl_EmptyCSharpSourceAddsOnlyXamlTabAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<fluence:ToggleSwitch IsChecked=\"True\" />",
                };

                Window window = DemoTestHost.CreateHostWindow(sample);
                try
                {
                    Controls.Expander? expander = DemoTestHost.FindByName<Controls.Expander>(sample, "SourceExpander");
                    _ = expander?.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.SelectorBar? tabs = DemoTestHost.FindByName<Controls.SelectorBar>(sample, "SourceSelector");
                    Assert.Equal(1, tabs?.Items.Count);
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test drives the SourceExpander via its own DemoSampleControl instance; there is
        // no shared page in this class to mutate.
        [Fact]
        public Task DemoSampleControl_SourceExpander_ReopensAfterCollapseAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                _ = DemoTestHost.EnsureDemoTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Sample",
                    DemoContent = new TextBlock { Text = "Body" },
                    XamlSource = "<Grid />",
                    CSharpSource = "public void Demo() { }",
                };
                Window window = new()
                {
                    Content = sample,
                    Width = 480,
                    Height = 360,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.Expander expander = Assert.IsType<Controls.Expander>(sample.FindName("SourceExpander"));

                    expander.IsExpanded = true;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, milliseconds: 4000, () => SourceContentRowHeight(expander) > 1d).ConfigureAwait(true),
                        "First expand should open the source content row.");

                    expander.IsExpanded = false;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, milliseconds: 4000, () => SourceContentRowHeight(expander) <= 0.5d).ConfigureAwait(true),
                        "Collapse should close the source content row.");

                    // Regression guard: re-expanding after a collapse must reopen the row.
                    // The collapse animation keeps filling and so holds the row closed, and a
                    // filling animation outranks the trigger setter, so the expand path has to
                    // re-animate the row back open or the dropdown never comes back.
                    expander.IsExpanded = true;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, milliseconds: 4000, () => SourceContentRowHeight(expander) > 1d).ConfigureAwait(true),
                        "Re-expanding after a collapse must reopen the source content row.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static double SourceContentRowHeight(Controls.Expander expander)
        {
            // The sample control hosts the stock Expander now rather than a copy of its template,
            // so the content tier is the control's own row, which the expand and collapse open and
            // close. The row is the honest measure: the border inside it keeps its desired height
            // while the row that hosts it is closed.
            RowDefinition? row = expander.Template?.FindName("Row1Def", expander) as RowDefinition;
            return row?.ActualHeight ?? 0d;
        }

        private static void AssertSourceTab(Controls.SelectorBar? tabs, string expectedHeader, string expectedSource)
        {
            if (tabs is null)
            {
                return;
            }
            foreach (object item in tabs.Items)
            {
                if (item is Controls.SelectorBarItem tab && string.Equals(tab.Text, expectedHeader, StringComparison.Ordinal))
                {
                    Button copy = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(tab.Tag as DependencyObject, "CopySourceButton"), exactMatch: false);
                    Assert.Equal(expectedSource, copy.Tag as string, StringComparer.Ordinal);
                    return;
                }
            }

            Assert.Fail("Missing source selector item: " + expectedHeader);
        }

        private static string GetSourceTabText(Controls.SelectorBar tabs, string expectedHeader)
        {
            foreach (object item in tabs.Items)
            {
                if (item is Controls.SelectorBarItem tab && string.Equals(tab.Text, expectedHeader, StringComparison.Ordinal))
                {
                    RichTextBox viewer = Assert.IsType<RichTextBox>(DemoTestHost.FindByName<RichTextBox>(tab.Tag as DependencyObject, "SourceTextViewer"), exactMatch: false);
                    TextRange textRange = new(viewer.Document.ContentStart, viewer.Document.ContentEnd);
                    return textRange.Text;
                }
            }

            Assert.Fail("Missing source selector item: " + expectedHeader);
            return string.Empty;
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
            Controls.SelectorBar tabs = Assert.IsType<Controls.SelectorBar>(DemoTestHost.FindByName<Controls.SelectorBar>(sample, "SourceSelector"), exactMatch: false);
            _ = Assert.Single(tabs.Items);
            Controls.SelectorBarItem tab = (Controls.SelectorBarItem)tabs.Items[0];
            Button copy = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(tab.Tag as DependencyObject, "CopySourceButton"), exactMatch: false);
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
