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
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryTypographyPage : Page
    {
        private const string CopyGlyph = "\uE8C8";

        /// <summary>
        /// The Segoe Fluent Icons checkmark shown for a moment after a successful copy, the
        /// way the WinUI Gallery's own CopyButton plays a success cue. Without it a copy that
        /// worked looks like nothing happened, which is what made these buttons read as dead.
        /// </summary>
        private const string CopiedGlyph = "\uE73E";

        private const string CopyToolTip = "Copy style key";

        private const string CopiedToolTip = "Style key copied to clipboard";

        private static readonly TimeSpan CopiedFeedbackDuration = TimeSpan.FromMilliseconds(1200);

        // WinUI Gallery Typography\TypographyTypeRamp.txt: the type ramp sample source is the ramp applied to TextBlocks.
        private static readonly string TypeRampXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Typography.TypeRamp",
            "    <StackPanel>\n" +
            "        <TextBlock Style=\"{StaticResource CaptionTextBlockStyle}\" Text=\"Caption\" />\n" +
            "        <TextBlock Style=\"{StaticResource BodyTextBlockStyle}\" Text=\"Body\" />\n" +
            "        <TextBlock Style=\"{StaticResource BodyStrongTextBlockStyle}\" Text=\"Body Strong\" />\n" +
            "        <TextBlock Style=\"{StaticResource BodyLargeTextBlockStyle}\" Text=\"Body Large\" />\n" +
            "        <TextBlock Style=\"{StaticResource SubtitleTextBlockStyle}\" Text=\"Subtitle\" />\n" +
            "        <TextBlock Style=\"{StaticResource TitleTextBlockStyle}\" Text=\"Title\" />\n" +
            "        <TextBlock Style=\"{StaticResource TitleLargeTextBlockStyle}\" Text=\"Title Large\" />\n" +
            "        <TextBlock Style=\"{StaticResource DisplayTextBlockStyle}\" Text=\"Display\" />\n" +
            "    </StackPanel>\n");

        private const string TypeRampCSharpSource = "using System.Windows.Controls;\n" +
                                                    "\n" +
                                                    "namespace Fluence.Wpf.Demo.Pages.Typography\n" +
                                                    "{\n" +
                                                    "    public partial class TypeRamp : UserControl\n" +
                                                    "    {\n" +
                                                    "        public TypeRamp()\n" +
                                                    "        {\n" +
                                                    "            InitializeComponent();\n" +
                                                    "        }\n" +
                                                    "    }\n" +
                                                    "}\n";

        private static readonly TypographyRow[] Rows =
        [
            new("Caption", "Small, Regular", "12/16 epx", "CaptionTextBlockStyle"),
            new("Body", "Text, Regular", "14/20 epx", "BodyTextBlockStyle"),
            new("Body Strong", "Text, SemiBold", "14/20 epx", "BodyStrongTextBlockStyle"),
            new("Body Large", "Text, Regular", "18/24 epx", "BodyLargeTextBlockStyle"),
            new("Subtitle", "Display, SemiBold", "20/28 epx", "SubtitleTextBlockStyle"),
            new("Title", "Display, SemiBold", "28/36 epx", "TitleTextBlockStyle"),
            new("Title Large", "Display, SemiBold", "40/52 epx", "TitleLargeTextBlockStyle"),
            new("Display", "Display, SemiBold", "68/92 epx", "DisplayTextBlockStyle"),
        ];

        public GalleryTypographyPage()
        {
            InitializeComponent();
            BuildTypographyTable();
            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, TypeRampXamlSource, TypeRampCSharpSource));
        }

        private void BuildTypographyTable()
        {
            TypographyTable.RowDefinitions.Clear();
            TypographyTable.Children.Clear();
            TypographyTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddHeader(0, "Example");
            AddHeader(1, "Variable Font");
            AddHeader(2, "Size/Line height");
            AddHeader(3, "Style");

            for (int i = 0; i < Rows.Length; i++)
            {
                int rowIndex = i + 1;
                TypographyTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddTypographyRow(rowIndex, Rows[i], i % 2 is 0);
            }
        }

        private void AddHeader(int column, string text)
        {
            TextBlock header = CreateTextBlock(text, "BodyStrongTextBlockStyle", new Thickness(12, 8, 16, 8));
            AddCell(header, 0, column);
        }

        private void AddTypographyRow(int rowIndex, TypographyRow row, bool shaded)
        {
            if (shaded)
            {
                Border background = new()
                {
                    CornerRadius = new CornerRadius(6),
                    IsHitTestVisible = false,
                    Margin = new Thickness(0, 2, 0, 2),
                };
                background.SetResourceReference(Border.BackgroundProperty, "SubtleFillColorSecondaryBrush");
                Grid.SetRow(background, rowIndex);
                Grid.SetColumnSpan(background, 5);
                _ = TypographyTable.Children.Add(background);
            }

            AddCell(CreateTextBlock(row.Example, row.StyleKey, new Thickness(12, 8, 16, 8)), rowIndex, 0);
            AddCell(CreateTextBlock(row.VariableFont, "CaptionTextBlockStyle", new Thickness(12, 8, 16, 8)), rowIndex, 1);
            AddCell(CreateTextBlock(row.SizeAndLineHeight, "CaptionTextBlockStyle", new Thickness(12, 8, 16, 8)), rowIndex, 2);
            AddCell(CreateTextBlock(row.StyleKey, "CaptionTextBlockStyle", new Thickness(12, 8, 16, 8)), rowIndex, 3);
            AddCell(CreateCopyButton(row.StyleKey), rowIndex, 4);
        }

        private static TextBlock CreateTextBlock(string text, string styleKey, Thickness margin)
        {
            TextBlock textBlock = new()
            {
                Margin = margin,
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            };
            textBlock.SetResourceReference(StyleProperty, styleKey);
            return textBlock;
        }

        private static Controls.Button CreateCopyButton(string styleKey)
        {
            Controls.Button button = new()
            {
                Content = new Controls.FontIcon { Glyph = CopyGlyph, IconFontSize = 16 },
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                // 16 dip of trailing inset is what the Gallery leaves between the copy button and
                // the right edge of the row.
                Margin = new Thickness(12, 8, 16, 8),
                MinWidth = 40,
                Padding = new Thickness(0),
                Tag = styleKey,
                ToolTip = CopyToolTip,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 40,
            };
            AutomationProperties.SetName(button, "Copy " + styleKey);
            button.Click += CopyStyleKey_Click;
            return button;
        }

        private void AddCell(FrameworkElement element, int row, int column)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            _ = TypographyTable.Children.Add(element);
        }

        private static void CopyStyleKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Controls.Button button)
            {
                return;
            }

            string? styleKey = button.Tag as string;
            if (string.IsNullOrWhiteSpace(styleKey))
            {
                return;
            }

            try
            {
                // The cue waits for the callback rather than firing here: the clipboard retry runs
                // on a dispatcher timer, so a copy another process is blocking has not failed yet
                // at this point and a cue shown now would claim a success that may never happen.
                DemoClipboard.SetText(styleKey, copied =>
                {
                    if (copied)
                    {
                        ShowCopiedFeedback(button);
                    }
                });
            }
            catch (ThreadStateException)
            {
                System.Diagnostics.Debug.WriteLine("Clipboard access requires an STA thread.");
            }
        }

        /// <summary>
        /// Swaps the copy glyph for a checkmark for a moment, then puts it back.
        /// </summary>
        /// <param name="button">The copy button that was clicked.</param>
        private static void ShowCopiedFeedback(Controls.Button button)
        {
            if (button.Content is not Controls.FontIcon glyph)
            {
                return;
            }

            glyph.SetCurrentValue(Controls.FontIcon.GlyphProperty, CopiedGlyph);
            button.SetCurrentValue(ToolTipProperty, CopiedToolTip);

            DispatcherTimer revert = new(DispatcherPriority.Background, button.Dispatcher)
            {
                Interval = CopiedFeedbackDuration,
            };

            revert.Tick += (_, _) =>
            {
                revert.Stop();
                glyph.SetCurrentValue(Controls.FontIcon.GlyphProperty, CopyGlyph);
                button.SetCurrentValue(ToolTipProperty, CopyToolTip);
            };

            revert.Start();
        }

        private sealed class TypographyRow(string example, string variableFont, string sizeAndLineHeight, string styleKey)
        {
            public string Example { get; } = example;

            public string VariableFont { get; } = variableFont;

            public string SizeAndLineHeight { get; } = sizeAndLineHeight;

            public string StyleKey { get; } = styleKey;
        }
    }
}
