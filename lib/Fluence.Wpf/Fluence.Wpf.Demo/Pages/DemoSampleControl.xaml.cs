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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Reusable card that presents a single control demonstration on a gallery page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each card is divided into four zones:
    /// <list type="number">
    ///   <item><description><b>Description</b> - a bold label above the card set via <see cref="SampleDescription"/>.</description></item>
    ///   <item><description><b>Live demo</b> - the area that hosts the actual running control, provided via <see cref="DemoContent"/>. Optional <see cref="OutputContent"/> sits beneath it to display interaction results.</description></item>
    ///   <item><description><b>Options rail</b> - a collapsible right panel for property toggles, provided via <see cref="RightRailContent"/>. Hidden when empty.</description></item>
    ///   <item><description><b>Source expander</b> - a collapsible section below the card with XAML and C# tabs showing copy-enabled source code. Hidden when both <see cref="XamlSource"/> and <see cref="CSharpSource"/> are empty.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Because WPF error MC3093 prevents naming controls inside a property element, named live
    /// controls are declared in hidden <c>ContentControl</c> slots at the page root and transferred
    /// into this card by <c>DemoSamplePageWiring.Apply</c>.
    /// </para>
    /// </remarks>
    public partial class DemoSampleControl : ContentControl
    {
        private enum SourceLanguage
        {
            PlainText,
            Xaml,
            CSharp
        }

        private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "case",
            "catch",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sealed",
            "short",
            "sizeof",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "var",
            "virtual",
            "void",
            "volatile",
            "while"
        };

        /// <summary>Identifies the <see cref="SampleDescription"/> dependency property.</summary>
        public static readonly DependencyProperty SampleDescriptionProperty =
            DependencyProperty.Register(
                "SampleDescription",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSampleDescriptionChanged));

        /// <summary>Identifies the <see cref="XamlSource"/> dependency property.</summary>
        public static readonly DependencyProperty XamlSourceProperty =
            DependencyProperty.Register(
                "XamlSource",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSourceChanged));

        /// <summary>Identifies the <see cref="CSharpSource"/> dependency property.</summary>
        public static readonly DependencyProperty CSharpSourceProperty =
            DependencyProperty.Register(
                "CSharpSource",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSourceChanged));

        /// <summary>Identifies the <see cref="DemoContent"/> dependency property.</summary>
        public static readonly DependencyProperty DemoContentProperty =
            DependencyProperty.Register(
                "DemoContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null, OnDemoContentChanged));

        /// <summary>Identifies the <see cref="OutputContent"/> dependency property.</summary>
        public static readonly DependencyProperty OutputContentProperty =
            DependencyProperty.Register(
                "OutputContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null, OnOutputContentChanged));

        /// <summary>Identifies the <see cref="RightRailContent"/> dependency property.</summary>
        public static readonly DependencyProperty RightRailContentProperty =
            DependencyProperty.Register(
                "RightRailContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null, OnRightRailContentChanged));

        private bool _sourceLoaded;

        public DemoSampleControl()
        {
            InitializeComponent();
            UpdateSampleDescriptionVisibility();
            UpdateDemoContentVisibility();
            UpdateOutputVisibility();
            UpdateRightRailVisibility();
            UpdateSourceVisibility();
        }

        /// <summary>
        /// Gets or sets the bold label displayed above the sample card. The label is hidden when
        /// this value is empty or whitespace.
        /// </summary>
        public string SampleDescription
        {
            get => (string)GetValue(SampleDescriptionProperty);
            set => SetValue(SampleDescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the XAML source text shown in the source expander's XAML tab. The
        /// expander is hidden when both this and <see cref="CSharpSource"/> are empty.
        /// </summary>
        public string XamlSource
        {
            get => (string)GetValue(XamlSourceProperty);
            set => SetValue(XamlSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the C# source text shown in the source expander's C# tab. The expander
        /// is hidden when both this and <see cref="XamlSource"/> are empty.
        /// </summary>
        public string CSharpSource
        {
            get => (string)GetValue(CSharpSourceProperty);
            set => SetValue(CSharpSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the live control displayed in the demo region of the card. When
        /// <see langword="null"/>, the card body and source expander corners are adjusted to
        /// indicate there is no live preview.
        /// </summary>
        public object? DemoContent
        {
            get => GetValue(DemoContentProperty);
            set => SetValue(DemoContentProperty, value);
        }

        /// <summary>
        /// Gets or sets optional content displayed beneath the live demo to show interaction
        /// results (for example, a click counter or selected-value readout). Hidden when
        /// <see langword="null"/>.
        /// </summary>
        public object? OutputContent
        {
            get => GetValue(OutputContentProperty);
            set => SetValue(OutputContentProperty, value);
        }

        /// <summary>
        /// Gets or sets optional content for the right-side options rail (property toggles,
        /// radio buttons, etc.). The rail collapses automatically when this is
        /// <see langword="null"/>.
        /// </summary>
        public object? RightRailContent
        {
            get => GetValue(RightRailContentProperty);
            set => SetValue(RightRailContentProperty, value);
        }

        private static void OnSampleDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateSampleDescriptionVisibility();
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.ResetSource();
            }
        }

        private static void OnDemoContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateDemoContentVisibility();
            }
        }

        private static void OnOutputContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateOutputVisibility();
            }
        }

        private static void OnRightRailContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateRightRailVisibility();
            }
        }

        private void UpdateSampleDescriptionVisibility()
        {
            if (SampleDescriptionTextBlock is null)
            {
                return;
            }

            SampleDescriptionTextBlock.Visibility = string.IsNullOrWhiteSpace(SampleDescription)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateSourceVisibility()
        {
            if (SourceExpander is null)
            {
                return;
            }

            SourceExpander.Visibility = string.IsNullOrWhiteSpace(XamlSource) && string.IsNullOrWhiteSpace(CSharpSource)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateDemoContentVisibility()
        {
            if (SampleCard is null || SourceExpander is null)
            {
                return;
            }

            if (DemoContent is null)
            {
                SampleCard.Visibility = Visibility.Collapsed;
                SourceExpander.BorderThickness = new Thickness(1);
                SourceExpander.CornerRadius = new CornerRadius(8);
                return;
            }

            SampleCard.Visibility = Visibility.Visible;
            SourceExpander.BorderThickness = new Thickness(1, 0, 1, 1);
            SourceExpander.CornerRadius = new CornerRadius(0, 0, 8, 8);
        }

        private void UpdateOutputVisibility()
        {
            if (OutputRegion is null)
            {
                return;
            }

            OutputRegion.Visibility = OutputContent is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateRightRailVisibility()
        {
            if (RightRailBorder is null)
            {
                return;
            }

            RightRailBorder.Visibility = RightRailContent is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ResetSource()
        {
            _sourceLoaded = false;
            SourceTabControl?.Items.Clear();

            UpdateSourceVisibility();
            if (SourceExpander is not null && SourceExpander.IsExpanded)
            {
                LoadSourceTabs();
            }
        }

        private void SourceExpander_Expanded(object sender, RoutedEventArgs e)
        {
            LoadSourceTabs();
        }

        private void LoadSourceTabs()
        {
            if (_sourceLoaded || (string.IsNullOrWhiteSpace(XamlSource) && string.IsNullOrWhiteSpace(CSharpSource)))
            {
                return;
            }

            _sourceLoaded = true;
            SourceTabControl.Items.Clear();
            if (!string.IsNullOrWhiteSpace(XamlSource))
            {
                AddSourceTab("XAML", XamlSource, SourceLanguage.Xaml);
            }

            if (!string.IsNullOrWhiteSpace(CSharpSource))
            {
                AddSourceTab("C#", CSharpSource, SourceLanguage.CSharp);
            }
        }

        private void AddSourceTab(string header, string source, SourceLanguage language)
        {
            TabItem tab = new()
            {
                Header = header,
                Content = CreateSourcePane(source, language)
            };
            _ = SourceTabControl.Items.Add(tab);

            if (SourceTabControl.SelectedIndex < 0)
            {
                SourceTabControl.SelectedIndex = 0;
            }
        }

        private Grid CreateSourcePane(string source, SourceLanguage language)
        {
            Grid panel = new();

            RichTextBox viewer = CreateSourceViewer(source, language);
            _ = panel.Children.Add(viewer);

            Border copyButtonHost = CreateCopyButtonHost(CreateCopyButton(source));
            _ = panel.Children.Add(copyButtonHost);

            return panel;
        }

        private static Border CreateCopyButtonHost(Controls.Button copyButton)
        {
            Border border = new()
            {
                Name = "CopySourceButtonHost",
                Child = copyButton,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = GetThicknessResource("DemoSourceCopyButtonHostMargin", new Thickness(0, 8, 8, 0)),
                VerticalAlignment = VerticalAlignment.Top
            };
            border.SetResourceReference(BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            return border;
        }

        private Controls.Button CreateCopyButton(string source)
        {
            Controls.Button button = new()
            {
                Name = "CopySourceButton",
                Appearance = ControlAppearance.Subtle,
                Icon = new Controls.FontIcon { Glyph = "\uE8C8", IconFontSize = 14 },
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 0,
                Padding = GetThicknessResource("DemoSourceCopyButtonPadding", new Thickness(8, 4, 8, 4)),
                Tag = source
            };
            button.Click += OnCopySourceButtonClick;
            return button;
        }

        private void OnCopySourceButtonClick(object sender, RoutedEventArgs e)
        {
            string? source = sender is FrameworkElement element ? element.Tag as string : null;
            if (!string.IsNullOrWhiteSpace(source))
            {
                Clipboard.SetText(source);
            }
        }

        private static RichTextBox CreateSourceViewer(string source, SourceLanguage language)
        {
            RichTextBox viewer = new()
            {
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                MinHeight = 220,
                Name = "SourceTextViewer",
                Padding = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            viewer.SetResourceReference(BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
            viewer.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
            viewer.Document = CreateSourceDocument(source, language);
            return viewer;
        }

        private static FlowDocument CreateSourceDocument(string source, SourceLanguage language)
        {
            FlowDocument document = new()
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                PagePadding = GetThicknessResource("DemoSourceCodeDocumentPadding", new Thickness(12))
            };
            document.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorPrimaryBrush");

            Paragraph paragraph = new()
            {
                LineHeight = 18,
                Margin = new Thickness(0)
            };
            document.Blocks.Add(paragraph);

            string normalized = (source ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                AddFormattedLine(paragraph, lines[i], language);
                if (i < lines.Length - 1)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }
            }

            return document;
        }

        // The following methods implement a lightweight hand-rolled tokenizer for XAML and C#.
        // A third-party syntax-highlighting library is intentionally avoided to keep the demo
        // dependency-free; the tokenizer only needs to colorize the read-only source preview
        // (keywords, string literals, comments, XML tag punctuation) and correctness on
        // edge-cases is a secondary concern.

        private static void AddFormattedLine(Paragraph paragraph, string line, SourceLanguage language)
        {
            if (language == SourceLanguage.Xaml)
            {
                AddXamlLine(paragraph, line);
                return;
            }

            if (language == SourceLanguage.CSharp)
            {
                AddCSharpLine(paragraph, line);
                return;
            }

            AddRun(paragraph, line, "TextFillColorPrimaryBrush");
        }

        private static void AddXamlLine(Paragraph paragraph, string line)
        {
            int index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "<!--"))
                {
                    AddRun(paragraph, line.Substring(index), "TextFillColorSecondaryBrush");
                    return;
                }

                char current = line[index];
                if (current is '"' or '\'')
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (current is '<' or '>' or '/')
                {
                    AddRun(paragraph, line.Substring(index, 1), "AccentTextFillColorPrimaryBrush");
                    index++;
                    continue;
                }

                if (IsXamlNameStart(current))
                {
                    int start = index;
                    while (index < line.Length && IsXamlNameChar(line[index]))
                    {
                        index++;
                    }

                    string name = line.Substring(start, index - start);
                    int next = SkipWhiteSpace(line, index);
                    string resourceKey = next < line.Length && line[next] == '='
                        ? "SystemFillColorSuccessBrush"
                        : "AccentTextFillColorPrimaryBrush";
                    AddRun(paragraph, name, resourceKey);
                    continue;
                }

                int plainStart = index;
                while (index < line.Length &&
                       line[index] != '<' &&
                       line[index] != '>' &&
                       line[index] != '/' &&
                       line[index] != '"' &&
                       line[index] != '\'' &&
                       !IsXamlNameStart(line[index]))
                {
                    index++;
                }

                AddRun(paragraph, line.Substring(plainStart, index - plainStart), "TextFillColorPrimaryBrush");
            }
        }

        private static void AddCSharpLine(Paragraph paragraph, string line)
        {
            int index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "//"))
                {
                    AddRun(paragraph, line.Substring(index), "TextFillColorSecondaryBrush");
                    return;
                }

                char current = line[index];
                if (current == '"')
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (current == '\'' && index + 2 < line.Length)
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (char.IsLetter(current) || current == '_')
                {
                    int start = index;
                    while (index < line.Length && (char.IsLetterOrDigit(line[index]) || line[index] == '_'))
                    {
                        index++;
                    }

                    string word = line.Substring(start, index - start);
                    AddRun(paragraph, word, CSharpKeywords.Contains(word)
                        ? "AccentTextFillColorPrimaryBrush"
                        : "TextFillColorPrimaryBrush");
                    continue;
                }

                int plainStart = index;
                while (index < line.Length &&
                       !StartsWith(line, index, "//") &&
                       line[index] != '"' &&
                       line[index] != '\'' &&
                       !char.IsLetter(line[index]) &&
                       line[index] != '_')
                {
                    index++;
                }

                AddRun(paragraph, line.Substring(plainStart, index - plainStart), "TextFillColorPrimaryBrush");
            }
        }

        private static void AddRun(Paragraph paragraph, string text, string resourceKey)
        {
            if (text.Length == 0)
            {
                return;
            }

            Run run = new(text);
            run.SetResourceReference(TextElement.ForegroundProperty, resourceKey);
            paragraph.Inlines.Add(run);
        }

        private static bool StartsWith(string text, int index, string value)
        {
            return index + value.Length <= text.Length &&
                   string.Compare(text, index, value, 0, value.Length, StringComparison.Ordinal) == 0;
        }

        private static int FindQuotedTextEnd(string text, int start, char quote)
        {
            int index = start + 1;
            while (index < text.Length)
            {
                if (text[index] == '\\')
                {
                    index += 2;
                    continue;
                }

                if (text[index] == quote)
                {
                    return index + 1;
                }

                index++;
            }

            return text.Length;
        }

        private static int SkipWhiteSpace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            return index;
        }

        private static bool IsXamlNameStart(char value)
        {
            return char.IsLetter(value) || value == '_' || value == ':';
        }

        private static bool IsXamlNameChar(char value)
        {
            return char.IsLetterOrDigit(value) ||
                   value == '_' ||
                   value == ':' ||
                   value == '.' ||
                   value == '-';
        }

        private static Thickness GetThicknessResource(string key, Thickness fallback)
        {
            return Application.Current?.TryFindResource(key) is Thickness value ? value : fallback;
        }
    }
}
