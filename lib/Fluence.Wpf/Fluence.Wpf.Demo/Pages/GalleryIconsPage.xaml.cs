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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Resources;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryIconsPage : UserControl
    {
        // Tile metrics mirroring the WinUI 3 Gallery Iconography grid: near-square
        // 115x110 cards separated by 12px gutters.
        private const double TileWidth = 115.0;
        private const double TileGapWidth = 12.0;
        private const int DefaultColumns = 4;

        private static readonly Lock IconCatalogLock = new();
        private static readonly char[] SearchTermSeparators = [' '];
        private static readonly CompareInfo OrdinalIgnoreCaseCompareInfo = CultureInfo.InvariantCulture.CompareInfo;
        private static List<IconCatalogItem>? cachedIcons;

        private static readonly Uri KnownIconNamesResourceUri = new(
            "/Fluence.Wpf.Demo;component/Resources/SegoeFluentIcons.tsv",
            UriKind.Relative);

        private readonly List<IconCatalogItem> _allIcons;
        private List<IconCatalogItem> _filteredIcons;
        private IconCatalogItem? _selectedIcon;
        private int _columns = DefaultColumns;

        public GalleryIconsPage()
        {
            InitializeComponent();

            _allIcons = GetIconCatalog();
            foreach (IconCatalogItem icon in _allIcons)
            {
                icon.IsSelected = false;
            }

            _filteredIcons = _allIcons;
            RebuildRows();
            if (_allIcons.Count > 0)
            {
                SelectIcon(_allIcons[0]);
            }

            CopyIconNameButton.Click += CopyIconValueButton_Click;
            CopyTextGlyphButton.Click += CopyIconValueButton_Click;
            CopyCodeGlyphButton.Click += CopyIconValueButton_Click;
            CopyXamlButton.Click += CopyIconValueButton_Click;
            CopyCSharpButton.Click += CopyIconValueButton_Click;
        }

        private void IconSearchBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            ApplyFilter(IconSearchBox.Text);
        }

        private void IconTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: IconCatalogItem icon })
            {
                SelectIcon(icon);
            }
        }

        private void IconCatalogList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double usableWidth = e.NewSize.Width - SystemParameters.VerticalScrollBarWidth;
            int columns = Math.Max(1, (int)((usableWidth + TileGapWidth) / (TileWidth + TileGapWidth)));
            if (columns != _columns)
            {
                _columns = columns;
                RebuildRows();
            }
        }

        private static void CopyIconValueButton_Click(object sender, RoutedEventArgs e)
        {
            string? value = sender is Controls.Button button ? button.Tag as string : null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            try
            {
                Clipboard.SetText(value);
            }
            catch (ExternalException)
            {
                System.Diagnostics.Debug.WriteLine("Clipboard was unavailable while copying an icon value.");
            }
            catch (ThreadStateException)
            {
                System.Diagnostics.Debug.WriteLine("Clipboard access requires an STA thread.");
            }
        }

        private void ApplyFilter(string searchText)
        {
            _filteredIcons = FilterIcons(_allIcons, searchText);
            RebuildRows();

            bool hasResults = _filteredIcons.Count > 0;
            IconCatalogEmptyText.Visibility = hasResults ? Visibility.Collapsed : Visibility.Visible;
            if (hasResults)
            {
                SelectIcon(_filteredIcons[0]);
            }
        }

        private void RebuildRows()
        {
            IconCatalogList.ItemsSource = CreateIconRows(_filteredIcons, _columns);
        }

        private void SelectIcon(IconCatalogItem icon)
        {
            if (ReferenceEquals(_selectedIcon, icon))
            {
                return;
            }

            _ = _selectedIcon?.IsSelected = false;
            _selectedIcon = icon;
            icon.IsSelected = true;

            IconPreviewGlyph.Glyph = icon.Glyph;
            IconNameValueText.Text = icon.Name;
            IconTextGlyphValueText.Text = icon.TextGlyph;
            IconCodeGlyphValueText.Text = icon.CodeGlyph;
            IconXamlValueText.Text = icon.FontIconXaml;
            IconCSharpValueText.Text = icon.FontIconCSharp;

            CopyIconNameButton.Tag = icon.Name;
            CopyTextGlyphButton.Tag = icon.TextGlyph;
            CopyCodeGlyphButton.Tag = icon.CodeGlyph;
            CopyXamlButton.Tag = icon.FontIconXaml;
            CopyCSharpButton.Tag = icon.FontIconCSharp;

            UpdateTags(icon);
        }

        private void UpdateTags(IconCatalogItem icon)
        {
            IconTagsPanel.Children.Clear();
            foreach (string tag in icon.Tags)
            {
                _ = IconTagsPanel.Children.Add(CreateTagPill(tag));
            }

            bool hasTags = icon.Tags.Count > 0;
            IconTagsPanel.Visibility = hasTags ? Visibility.Visible : Visibility.Collapsed;
            IconNoTagsText.Visibility = hasTags ? Visibility.Collapsed : Visibility.Visible;
        }

        private static Border CreateTagPill(string tag)
        {
            TextBlock text = new()
            {
                Text = tag,
                VerticalAlignment = VerticalAlignment.Center,
            };
            text.SetResourceReference(StyleProperty, "CaptionTextBlockStyle");
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

            Border pill = new()
            {
                BorderThickness = new Thickness(1),
                Child = text,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 4, 4),
                MinHeight = 24,
                Padding = new Thickness(10, 2, 10, 3),
            };
            pill.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            pill.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            return pill;
        }

        private static List<IconCatalogItem> FilterIcons(List<IconCatalogItem> icons, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return icons;
            }

            string[] terms = searchText.Split(SearchTermSeparators, StringSplitOptions.RemoveEmptyEntries);
            List<IconCatalogItem> matches = [];
            foreach (IconCatalogItem icon in icons)
            {
                if (MatchesAllTerms(icon, terms))
                {
                    matches.Add(icon);
                }
            }

            return matches;
        }

        private static bool MatchesAllTerms(IconCatalogItem icon, string[] terms)
        {
            foreach (string term in terms)
            {
                if (!MatchesTerm(icon, term))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesTerm(IconCatalogItem icon, string term)
        {
            if (ContainsIgnoreCase(icon.Name, term)
                || ContainsIgnoreCase(icon.Code, term)
                || ContainsIgnoreCase(icon.DisplayCode, term))
            {
                return true;
            }

            foreach (string tag in icon.Tags)
            {
                if (ContainsIgnoreCase(tag, term))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsIgnoreCase(string text, string term)
        {
            // CompareInfo keeps the ordinal-ignore-case containment check available on
            // net472, where string.Contains(string, StringComparison) does not exist.
            return OrdinalIgnoreCaseCompareInfo.IndexOf(text, term, CompareOptions.OrdinalIgnoreCase) >= 0;
        }

        private static List<IconCatalogRow> CreateIconRows(List<IconCatalogItem> icons, int columns)
        {
            List<IconCatalogRow> rows = new((icons.Count + columns - 1) / columns);
            for (int index = 0; index < icons.Count; index += columns)
            {
                List<IconCatalogItem> rowItems = new(columns);
                for (int offset = 0; offset < columns && index + offset < icons.Count; offset++)
                {
                    rowItems.Add(icons[index + offset]);
                }

                rows.Add(new IconCatalogRow(rowItems));
            }

            return rows;
        }

        private static List<IconCatalogItem> GetIconCatalog()
        {
            lock (IconCatalogLock)
            {
                cachedIcons ??= LoadIconCatalog();
                return cachedIcons;
            }
        }

        private static List<IconCatalogItem> LoadIconCatalog()
        {
            Dictionary<string, string> knownNames = LoadKnownIconNames();
            Typeface typeface = new(
                new FontFamily("Segoe Fluent Icons"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            if (!typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
            {
                throw new InvalidOperationException("Segoe Fluent Icons is required to render the icons catalog.");
            }

            List<int> codes = [];
            foreach (int character in glyphTypeface.CharacterToGlyphMap.Keys)
            {
                if (character is >= 0xE000 and <= 0xF8FF)
                {
                    codes.Add(character);
                }
            }

            codes.Sort();

            List<IconCatalogItem> namedIcons = new(knownNames.Count);
            List<IconCatalogItem> unnamedIcons = [];
            foreach (int code in codes)
            {
                string codeText = code.ToString("X4", CultureInfo.InvariantCulture);
                string glyph = char.ConvertFromUtf32(code);
                if (knownNames.TryGetValue(codeText, out string? name))
                {
                    namedIcons.Add(new IconCatalogItem(name, codeText, glyph, DeriveTags(name)));
                }
                else
                {
                    unnamedIcons.Add(new IconCatalogItem("Private-use icon", codeText, glyph, []));
                }
            }

            return [.. namedIcons, .. unnamedIcons];
        }

        /// <summary>
        /// Derives lowercase search tags from a PascalCase icon name, mirroring the tag
        /// pills shown by the WinUI 3 Gallery. The shipped catalog data carries no tag
        /// column, so the name itself is the only available source.
        /// </summary>
        /// <param name="name">The PascalCase icon name to derive tags from.</param>
        /// <returns>A list of lowercase word tokens extracted from the name.</returns>
        private static List<string> DeriveTags(string name)
        {
            List<string> tags = [];
            StringBuilder word = new();
            for (int index = 0; index < name.Length; index++)
            {
                char character = name[index];
                if (!char.IsLetterOrDigit(character))
                {
                    FlushTag(tags, word);
                    continue;
                }

                bool startsNewWord = word.Length > 0
                    && char.IsUpper(character)
                    && (!char.IsUpper(name[index - 1])
                        || (index + 1 < name.Length && char.IsLower(name[index + 1])));
                if (startsNewWord)
                {
                    FlushTag(tags, word);
                }

                _ = word.Append(char.ToLowerInvariant(character));
            }

            FlushTag(tags, word);
            return tags;
        }

        private static void FlushTag(List<string> tags, StringBuilder word)
        {
            if (word.Length is 0)
            {
                return;
            }

            string tag = word.ToString();
            _ = word.Clear();
            if (tag.Length < 2 || !char.IsLetter(tag[0]) || tags.Contains(tag))
            {
                return;
            }

            tags.Add(tag);
        }

        private static Dictionary<string, string> LoadKnownIconNames()
        {
            StreamResourceInfo info = Application.GetResourceStream(KnownIconNamesResourceUri) ?? throw new InvalidOperationException("Segoe Fluent Icons name data was not found.");
            Dictionary<string, string> names = new(comparer: StringComparer.OrdinalIgnoreCase);
            using (StreamReader reader = new(info.Stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (line.Length is 0)
                    {
                        continue;
                    }

                    string[] parts = line.Split('\t');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    string name = parts[0].Trim();
                    string code = parts[1].Trim().ToUpperInvariant();
                    if (name.Length is 0 || code.Length is 0 || names.ContainsKey(code))
                    {
                        continue;
                    }

                    names.Add(code, name);
                }
            }

            return names;
        }

        public sealed class IconCatalogRow(IList<IconCatalogItem> items)
        {
            public IList<IconCatalogItem> Items { get; } = items;
        }

        public sealed class IconCatalogItem(string name, string code, string glyph, IReadOnlyList<string> tags) : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public string Name { get; } = name;

            public string Code { get; } = code;

            public string Glyph { get; } = glyph;

            public string DisplayCode { get; } = "U+" + code;

            public string TextGlyph { get; } = "&#x" + code + ";";

            public string CodeGlyph { get; } = "\\u" + code;

            public string FontIconXaml { get; } = "<ui:FontIcon Glyph=\"&#x" + code + ";\" />";

            public string FontIconCSharp { get; } =
                "FontIcon icon = new FontIcon();" + Environment.NewLine + "icon.Glyph = \"\\u" + code + "\";";

            public IReadOnlyList<string> Tags { get; } = tags;

            public bool IsSelected
            {
                get;
                set
                {
                    if (field == value)
                    {
                        return;
                    }

                    field = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
    }
}
