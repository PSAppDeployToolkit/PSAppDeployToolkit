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

using Fluence.Wpf.Demo.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using FluenceExpander = Fluence.Wpf.Controls.Expander;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public sealed class DemoSamplePageWiringTests
    {
        private const string IntentionalPartialSnippetMarker = "Intentionally partial layout snippet";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static readonly Func<UIElement>[] SamplePageFactories =
        [
            static () => new GalleryIconsPage(),
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

        [TestMethod]
        public void DemoSamplePageWiring_MovesSlotContentAndAppliesTypedSources()
        {
            DemoTestHost.RunOnSta(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                WpfTextBlock demoContent = new() { Text = "Demo" };
                WpfTextBlock outputContent = new() { Text = "Output" };
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

                Assert.AreSame(demoContent, sample.DemoContent, "Demo slot content should move into the sample.");
                Assert.AreSame(outputContent, sample.OutputContent, "Output slot content should move into the sample.");
                Assert.AreSame(rightRailContent, sample.RightRailContent, "Right rail slot content should move into the sample.");
                Assert.IsNull(demoSlot.Content, "Demo slot content should be cleared after transfer.");
                Assert.IsNull(outputSlot.Content, "Output slot content should be cleared after transfer.");
                Assert.IsNull(rightRailSlot.Content, "Right rail slot content should be cleared after transfer.");
                Assert.AreEqual("<Grid />", sample.XamlSource);
                Assert.AreEqual("public void Demo() { }", sample.CSharpSource);
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsSourceCountMismatch()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(new DemoSampleControl());
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsDuplicateSourceSlots()
        {
            DemoTestHost.RunOnSta(delegate
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

        [TestMethod]
        public void DemoSamplePageWiring_RejectsUnusedContentSlots()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot02DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsZeroContentSlot()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot00DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsDuplicateContentSlots()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot01DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(CreateSlot("DemoSampleSlot01DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSampleControl_ReloadsExpandedSourceTabsWhenSourceChanges()
        {
            DemoTestHost.RunOnSta(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                DemoSampleControl sample = new()
                {
                    DemoContent = new WpfTextBlock { Text = "Body" },
                    XamlSource = "<Grid />",
                };
                Window window = DemoTestHost.CreateHostWindow(sample);
                try
                {
                    FluenceExpander? expander = DemoTestHost.FindByName<FluenceExpander>(sample, "SourceExpander");
                    Assert.IsNotNull(expander, "Source expander should exist.");
                    expander.IsExpanded = true;
                    DemoTestHost.Drain(window.Dispatcher);
                    window.UpdateLayout();

                    AssertSourceCopyTag(sample, "<Grid />");
                    sample.XamlSource = "<StackPanel />";
                    DemoTestHost.Drain(window.Dispatcher);
                    window.UpdateLayout();

                    AssertSourceCopyTag(sample, "<StackPanel />");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [TestMethod]
        public void GallerySamplePages_AllVisibleDemoSamplesExposeSource()
        {
            DemoTestHost.RunOnSta(static delegate
            {
                foreach (Func<UIElement> factory in SamplePageFactories)
                {
                    _ = DemoTestHost.EnsureDemoTheme();
                    UIElement page = factory();
                    Window window = DemoTestHost.CreateHostWindow(page);
                    try
                    {
                        List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                        Assert.IsTrue(samples.Count > 0, "Page should expose DemoSampleControl samples: " + page.GetType().Name);
                        foreach (DemoSampleControl sample in samples.Where(static sample => sample.Visibility == Visibility.Visible))
                        {
                            Assert.IsFalse(string.IsNullOrWhiteSpace(sample.XamlSource),
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

        [TestMethod]
        public void GallerySamplePages_SourceContractsMatchDisplayedClasses()
        {
            DemoTestHost.RunOnSta(static delegate
            {
                foreach (DemoSampleControl sample in CreateVisibleSamples())
                {
                    string xamlSource = sample.XamlSource.Trim();
                    string csharpSource = sample.CSharpSource.Trim();
                    if (IsIntentionalPartialSnippet(xamlSource))
                    {
                        Assert.IsTrue(
                            string.IsNullOrWhiteSpace(csharpSource),
                            "Intentional partial snippets should not display a code-behind class.");
                        continue;
                    }

                    XDocument document = ParseXamlSource(xamlSource, sample.SampleDescription);
                    Assert.AreEqual(
                        "UserControl",
                        document.Root?.Name.LocalName,
                        "Self-contained displayed XAML should use a UserControl root: " + sample.SampleDescription);

                    string xamlClass = document.Root?.Attribute(XamlNamespace + "Class")?.Value
                        ?? throw new AssertFailedException("Displayed UserControl XAML must declare x:Class: " + sample.SampleDescription);
                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(csharpSource),
                        "Displayed UserControl XAML should include matching C# source: " + sample.SampleDescription);
                    Assert.IsTrue(
                        csharpSource.Contains("InitializeComponent();", StringComparison.Ordinal),
                        "Displayed C# source should use the UserControl InitializeComponent pattern: " + sample.SampleDescription);
                    Assert.AreEqual(
                        xamlClass,
                        GetDeclaredPartialClassName(csharpSource),
                        "Displayed XAML x:Class must match the C# namespace and partial class: " + sample.SampleDescription);
                }
            });
        }

        [TestMethod]
        public void GallerySamplePages_CSharpSourcesUseReleaseReadySnippetStyle()
        {
            DemoTestHost.RunOnSta(static delegate
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
            TabControl? tabs = DemoTestHost.FindByName<TabControl>(sample, "SourceTabControl");
            Assert.IsNotNull(tabs, "Source tabs should exist.");
            Assert.AreEqual(1, tabs.Items.Count, "XAML-only sample should expose one source tab.");
            TabItem tab = (TabItem)tabs.Items[0];
            WpfButton? copy = DemoTestHost.FindByName<WpfButton>(tab.Content as DependencyObject, "CopySourceButton");
            Assert.IsNotNull(copy, "Source tab should expose the copy button.");
            Assert.AreEqual(expectedSource, copy.Tag as string);
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
                        .Where(static sample => sample.Visibility == Visibility.Visible));
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
                throw new AssertFailedException("Displayed XAML source must be well formed: " + sampleDescription, exception);
            }
        }

        private static string GetDeclaredPartialClassName(string csharpSource)
        {
            string namespaceName = GetNamespaceName(csharpSource);
            foreach (string line in SplitLines(csharpSource))
            {
                string trimmed = line.Trim();
                const string classPrefix = "public partial class ";
                if (!trimmed.StartsWith(classPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string classRemainder = trimmed[classPrefix.Length..];
                int classNameEnd = classRemainder.IndexOfAny([' ', ':']);
                string className = classNameEnd < 0 ? classRemainder : classRemainder[..classNameEnd];
                return namespaceName + "." + className;
            }

            throw new AssertFailedException("Displayed C# source must declare a public partial class.");
        }

        private static string GetNamespaceName(string csharpSource)
        {
            foreach (string line in SplitLines(csharpSource))
            {
                string trimmed = line.Trim();
                const string namespacePrefix = "namespace ";
                if (trimmed.StartsWith(namespacePrefix, StringComparison.Ordinal))
                {
                    return trimmed[namespacePrefix.Length..].Trim();
                }
            }

            throw new AssertFailedException("Displayed C# source must declare a namespace.");
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
                bool startsOnBoundary = index == 0 || !IsWordCharacter(text[index - 1]);
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
