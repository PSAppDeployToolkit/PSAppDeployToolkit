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

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Builds the read-only, theme-brushed source viewers shared by <see cref="DemoSampleControl"/>
    /// (the source expander tabs) and <see cref="DemoCodePresenter"/> (inline snippets).
    /// </summary>
    /// <remarks>
    /// A lightweight hand-rolled tokenizer for XAML and C#. A third-party syntax-highlighting
    /// library is intentionally avoided to keep the demo dependency-free; the tokenizer only needs
    /// to colorize a read-only preview (keywords, string literals, comments, XML tag punctuation)
    /// and correctness on edge cases is a secondary concern. Every color is a theme brush resource
    /// reference so the viewers follow Light, Dark and High Contrast.
    /// </remarks>
    internal static class DemoSourceHighlighter
    {
        private const string PrimaryTextKey = "TextFillColorPrimaryBrush";
        private const string SecondaryTextKey = "TextFillColorSecondaryBrush";
        private const string StringLiteralKey = "SystemFillColorCautionBrush";
        private const string KeywordKey = "AccentTextFillColorPrimaryBrush";
        private const string AttributeNameKey = "SystemFillColorSuccessBrush";

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
            "while",
        };

        /// <summary>
        /// Creates a read-only <see cref="RichTextBox"/> showing <paramref name="source"/> colorized for <paramref name="language"/>.
        /// </summary>
        /// <param name="source">The source text.</param>
        /// <param name="language">The language to colorize.</param>
        /// <param name="fontSize">The code font size.</param>
        /// <param name="lineHeight">The line height for the code font size.</param>
        /// <param name="pagePadding">The document padding.</param>
        /// <returns>The viewer, transparent, unbordered, with the theme monospace font.</returns>
        internal static RichTextBox CreateViewer(string source, DemoSourceLanguage language, double fontSize, double lineHeight, Thickness pagePadding)
        {
            RichTextBox viewer = new()
            {
                BorderThickness = new Thickness(0),
                FontSize = fontSize,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                Padding = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
            // WinUI Gallery: code sits directly on its host tier, no opaque plate.
            viewer.SetResourceReference(Control.BackgroundProperty, "SubtleFillColorTransparentBrush");
            viewer.SetResourceReference(Control.ForegroundProperty, PrimaryTextKey);
            viewer.SetResourceReference(Control.FontFamilyProperty, "DemoMonospaceFontFamily");
            viewer.Document = CreateDocument(source, language, fontSize, lineHeight, pagePadding);
            return viewer;
        }

        /// <summary>
        /// Creates the colorized <see cref="FlowDocument"/> for <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source text.</param>
        /// <param name="language">The language to colorize.</param>
        /// <param name="fontSize">The code font size.</param>
        /// <param name="lineHeight">The line height for the code font size.</param>
        /// <param name="pagePadding">The document padding.</param>
        /// <returns>A single-paragraph document, one <see cref="Run"/> per token.</returns>
        internal static FlowDocument CreateDocument(string source, DemoSourceLanguage language, double fontSize, double lineHeight, Thickness pagePadding)
        {
            FlowDocument document = new()
            {
                FontSize = fontSize,
                PagePadding = pagePadding,
            };
            document.SetResourceReference(TextElement.ForegroundProperty, PrimaryTextKey);
            document.SetResourceReference(TextElement.FontFamilyProperty, "DemoMonospaceFontFamily");

            Paragraph paragraph = new()
            {
                LineHeight = lineHeight,
                Margin = new Thickness(0),
            };
            document.Blocks.Add(paragraph);

            string normalized = (source ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
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

        private static void AddFormattedLine(Paragraph paragraph, string line, DemoSourceLanguage language)
        {
            if (language is DemoSourceLanguage.Xaml)
            {
                AddXamlLine(paragraph, line);
                return;
            }

            if (language is DemoSourceLanguage.CSharp)
            {
                AddCSharpLine(paragraph, line);
                return;
            }

            AddRun(paragraph, line, PrimaryTextKey);
        }

        private static void AddXamlLine(Paragraph paragraph, string line)
        {
            int index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "<!--"))
                {
                    AddRun(paragraph, line[index..], SecondaryTextKey);
                    return;
                }

                char current = line[index];
                if (current is '"' or '\'')
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line[index..end], StringLiteralKey);
                    index = end;
                    continue;
                }

                if (current is '<' or '>' or '/')
                {
                    AddRun(paragraph, line[index..(index + 1)], KeywordKey);
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

                    string name = line[start..index];
                    int next = SkipWhiteSpace(line, index);
                    string resourceKey = next < line.Length && line[next] == '='
                        ? AttributeNameKey
                        : KeywordKey;
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

                AddRun(paragraph, line[plainStart..index], PrimaryTextKey);
            }
        }

        private static void AddCSharpLine(Paragraph paragraph, string line)
        {
            int index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "//"))
                {
                    AddRun(paragraph, line[index..], SecondaryTextKey);
                    return;
                }

                char current = line[index];
                if (current == '"')
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line[index..end], StringLiteralKey);
                    index = end;
                    continue;
                }

                if (current == '\'' && index + 2 < line.Length)
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line[index..end], StringLiteralKey);
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

                    string word = line[start..index];
                    AddRun(paragraph, word, CSharpKeywords.Contains(word)
                        ? KeywordKey
                        : PrimaryTextKey);
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

                AddRun(paragraph, line[plainStart..index], PrimaryTextKey);
            }
        }

        private static void AddRun(Paragraph paragraph, string text, string resourceKey)
        {
            if (text.Length is 0)
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
                string.Compare(text, index, value, 0, value.Length, StringComparison.Ordinal) is 0;
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
    }
}
