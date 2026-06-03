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
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Resources;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryIconsPage : UserControl
    {
        private const string IconCatalogXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Icons.IconCatalog""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid
        MinHeight=""48""
        VerticalAlignment=""Center"">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""56"" />
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""96"" />
        </Grid.ColumnDefinitions>

        <ui:FontIcon
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Glyph=""&#xE713;""
            IconFontSize=""24"" />
        <TextBlock
            Grid.Column=""1""
            HorizontalAlignment=""Left""
            VerticalAlignment=""Center""
            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
            Text=""Settings"" />
        <TextBlock
            Grid.Column=""2""
            HorizontalAlignment=""Left""
            VerticalAlignment=""Center""
            FontFamily=""Consolas""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""U+E713"" />
    </Grid>
</UserControl>
";

        private const string IconCatalogCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Icons
{
    public partial class IconCatalog : UserControl
    {
        public IconCatalog()
        {
            InitializeComponent();
        }
    }
}
";

        private const int IconsPerRow = 4;
        private static readonly object IconRowsLock = new();
        private static List<IconCatalogRow>? cachedIconRows;
        private static int cachedIconCount;

        private static readonly Uri KnownIconNamesResourceUri = new(
            "/Fluence.Wpf.Demo;component/Resources/SegoeFluentIcons.tsv",
            UriKind.Relative);

        public GalleryIconsPage()
        {
            InitializeComponent();

            List<IconCatalogRow> rows = GetIconRows();
            IconCatalogList.ItemsSource = rows;
            IconCatalogCountText.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0:N0} Segoe Fluent Icons",
                cachedIconCount);

            DemoSampleControl sample = new()
            {
                SampleDescription = string.Empty,
                XamlSource = IconCatalogXamlSource,
                CSharpSource = IconCatalogCSharpSource,
                DemoContent = FontIconSampleContent
            };
            Grid.SetRow(sample, 2);
            PageContent.Children.Remove(FontIconSampleContent);
            _ = PageContent.Children.Add(sample);
        }

        private static List<IconCatalogRow> GetIconRows()
        {
            lock (IconRowsLock)
            {
                if (cachedIconRows is null)
                {
                    List<IconCatalogItem> icons = LoadIconCatalog();
                    cachedIconCount = icons.Count;
                    cachedIconRows = CreateIconRows(icons);
                }

                return cachedIconRows;
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
                    namedIcons.Add(new IconCatalogItem(name, codeText, glyph));
                }
                else
                {
                    unnamedIcons.Add(new IconCatalogItem("Private-use icon", codeText, glyph));
                }
            }

            List<IconCatalogItem> icons = [.. namedIcons, .. unnamedIcons];
            return icons;
        }

        private static List<IconCatalogRow> CreateIconRows(List<IconCatalogItem> icons)
        {
            List<IconCatalogRow> rows = new((icons.Count + IconsPerRow - 1) / IconsPerRow);
            for (int index = 0; index < icons.Count; index += IconsPerRow)
            {
                List<IconCatalogItem> rowItems = new(IconsPerRow);
                for (int offset = 0; offset < IconsPerRow && index + offset < icons.Count; offset++)
                {
                    rowItems.Add(icons[index + offset]);
                }

                rows.Add(new IconCatalogRow(rowItems));
            }

            return rows;
        }

        private static Dictionary<string, string> LoadKnownIconNames()
        {
            StreamResourceInfo info = Application.GetResourceStream(KnownIconNamesResourceUri) ?? throw new InvalidOperationException("Segoe Fluent Icons name data was not found.");
            Dictionary<string, string> names = new(StringComparer.OrdinalIgnoreCase);
            using (StreamReader reader = new(info.Stream, Encoding.UTF8, true))
            {
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (line.Length == 0)
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
                    if (name.Length == 0 || code.Length == 0 || names.ContainsKey(code))
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
            public IList<IconCatalogItem> Items { get; private set; } = items;
        }

        public sealed class IconCatalogItem(string name, string code, string glyph)
        {
            public string Name { get; private set; } = name;

            public string Code { get; private set; } = code;

            public string DisplayCode { get; private set; } = "U+" + code;

            public string Glyph { get; private set; } = glyph;
        }
    }
}
