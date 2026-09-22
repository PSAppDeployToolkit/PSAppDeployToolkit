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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryColorsPage"/>, a direct catalog page that mirrors the WinUI 3 Gallery
    /// Color page (six sections of <see cref="ColorPageExample"/> cards and <see cref="ColorTile"/> rows)
    /// and, like Icons, renders without a <see cref="DemoSampleControl"/>.
    /// </summary>
    public sealed class GalleryColorsPageTests : IAsyncLifetime
    {
        // Transcribed from WinUIGallery/Controls/DesignGuidance/ColorSections/*Section.xaml: the
        // ColorPageExample titles in order and the tile-row column counts in order, per SelectorBar item.
        private static readonly SectionExpectation[] Sections =
        [
            new("Text", ["Text", "Accent Text", "Text On Accent"], [4, 4, 2, 2]),
            new("Fill", ["Control Fill", "Control Alt Fill", "Neutral Solid", "Neutral Strong", "Subtle Fill", "Control On Image Fill", "Accent Fill"], [4, 3, 3, 2, 1, 2, 4, 4, 3, 2]),
            new("Stroke", ["Card Stroke", "Control Elevation (gradient strokes)", "Control Stroke", "Control Strong Stroke", "Surface Stroke", "Divider Stroke", "Focus Stroke"], [2, 3, 2, 4, 3, 2, 2, 1, 2]),
            new("Background", ["Card Background", "Smoke Background", "Layer", "Layer on Acrylic", "Layer on Mica Base Alt", "Solid Background", "Acrylic Background", "Accent Acrylic Background"], [3, 1, 2, 1, 4, 4, 3, 2, 2]),
            new("Signal", ["System"], [3, 3, 3, 3, 1]),
            new("High Contrast", [], [4, 4, 4, 4, 4]),
        ];

        private const int ExpectedTileCount = 136;

        private Window? _host;
        private GalleryColorsPage? _page;

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                _ = TestApp.EnsureDemoTheme();
                _page = new GalleryColorsPage();
                _host = DemoTestHost.CreateHostWindow(_page);
            }));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                if (_host is not null)
                {
                    DemoTestHost.CloseWindow(_host);
                    _host = null;
                }

                _page = null;
            }));
        }

        // This test needs the real shell NavigationView to reach the Colors page by route, so
        // it builds its own MainWindow rather than the page the class shares.
        [Fact]
        public Task GalleryColorsPage_NavigationRoute_LoadsConcretePageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    window.NavigateTo("colors");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.NavigationView navigationView = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);

                    // The shell hosts its Page content in a Frame, which is what WPF allows to
                    // parent a Page.
                    Frame frame = Assert.IsType<Frame>(navigationView.Content, exactMatch: false);
                    _ = Assert.IsType<GalleryColorsPage>(frame.Content, exactMatch: false);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryColorsPage_ExposesGalleryPageHeaderWithColorsTitleAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                GalleryPageHeader header = Assert.IsType<GalleryPageHeader>(DemoTestHost.FindVisualChildren<GalleryPageHeader>(page).FirstOrDefault(), exactMatch: false);
                Assert.Equal("Colors", header.Title, StringComparer.Ordinal);
                // Colors documents the theme tokens, so its Documentation link targets docs/theming.md rather than docs/controls.md.
                Assert.Equal("theming.md", header.DocsDocument, StringComparer.Ordinal);
                Assert.Equal("canonical-token-families", header.DocsAnchor, StringComparer.Ordinal);
                Assert.Equal("Fluence.Wpf/Themes/Colors/Theme.Light.xaml", header.ControlSourcePath, StringComparer.Ordinal);

                // WinUI Gallery ColorPage.xaml: an inline SampleCodePresenter without a copy button, then the SelectorBar.
                DemoCodePresenter snippet = Assert.Single(DemoTestHost.FindVisualChildren<DemoCodePresenter>(page));
                Assert.Contains("TextFillColorPrimaryBrush", snippet.Code, StringComparison.Ordinal);
                Assert.Equal(DemoSourceLanguage.Xaml, snippet.CodeLanguage);
                Assert.False(snippet.IsCopyButtonVisible);
                _ = Assert.Single(DemoTestHost.FindVisualChildren<RichTextBox>(snippet));
                // The section switcher is the library SelectorBar now, not a restyled TabControl.
                Controls.SelectorBar selector = ColorSelector(page);
                Assert.Equal(Sections.Length, selector.Items.Count);
                _ = Assert.IsType<Controls.SlideNavigationPresenter>(DemoTestHost.FindByName<Controls.SlideNavigationPresenter>(page, "ColorSectionPresenter"), exactMatch: false);
            });
        }

        [Fact]
        public Task GalleryColorsPage_MirrorsWinUiGalleryColorSectionsAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                _ = Assert.IsType<Controls.SmoothScrollViewer>(DemoTestHost.FindVisualChildren<Controls.SmoothScrollViewer>(page).FirstOrDefault(), exactMatch: false);
                Assert.Empty(DemoTestHost.FindVisualChildren<DemoSampleControl>(page));
                Assert.Empty(DemoTestHost.FindVisualChildren<WrapPanel>(page));

                Controls.SelectorBar selector = ColorSelector(page);
                Assert.Equal(Sections.Length, selector.Items.Count);

                int totalTiles = 0;
                for (int i = 0; i < Sections.Length; i++)
                {
                    SectionExpectation expected = Sections[i];
                    Controls.SelectorBarItem item = (Controls.SelectorBarItem)selector.Items[i];
                    Assert.Equal(expected.Title, item.Text, StringComparer.Ordinal);

                    SelectSection(selector, i, page.Dispatcher);

                    List<string> exampleTitles = [.. DemoTestHost.FindVisualChildren<ColorPageExample>(SectionPanel(page)).Select(static example => example.Title)];
                    Assert.Equal(expected.ExampleTitles, exampleTitles, StringComparer.Ordinal);

                    List<UniformGrid> rows = TileRows(SectionPanel(page));
                    Assert.Equal(expected.RowColumns, rows.Select(static row => row.Columns));
                    foreach (UniformGrid row in rows)
                    {
                        Assert.True(row.Columns <= 4, "WinUI Gallery tile rows never exceed four columns.");
                        Assert.Equal(row.Columns * row.Rows, row.Children.Count);
                        totalTiles += row.Children.Count;
                    }
                }

                Assert.Equal(ExpectedTileCount, totalTiles);
            });
        }

        [Fact]
        public Task GalleryColorsPage_TilesArePaintedWithTheirOwnBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                Application application = WpfTestSta.EnsureApplication();
                Controls.SelectorBar selector = ColorSelector(page);
                CornerRadius middle = Assert.IsType<CornerRadius>(application.TryFindResource("DemoColorTileMiddleCornerRadius"), exactMatch: false);
                Color surfaceColor = Assert.IsType<SolidColorBrush>(application.TryFindResource("SolidBackgroundFillColorBaseBrush"), exactMatch: false).Color;

                for (int i = 0; i < selector.Items.Count; i++)
                {
                    SelectSection(selector, i, page.Dispatcher);
                    foreach (UniformGrid row in TileRows(SectionPanel(page)))
                    {
                        // WinUI Gallery GalleryTileGridStyle: the row sits on the base solid background inside a 1px card stroke.
                        Border surface = Assert.IsType<Border>(row.Parent, exactMatch: false);
                        Assert.Equal(surfaceColor, Assert.IsType<SolidColorBrush>(surface.Background, exactMatch: false).Color);
                        Assert.Equal(new Thickness(1), surface.BorderThickness);

                        List<ColorTile> tiles = [.. row.Children.Cast<ColorTile>()];
                        for (int t = 0; t < tiles.Count; t++)
                        {
                            ColorTile tile = tiles[t];
                            if (tile.Tag is null)
                            {
                                // A high contrast palette tile paints one of the fixed palettes the
                                // Gallery prints, not a live theme brush.
                                continue;
                            }

                            string key = Assert.IsType<string>(tile.Tag, exactMatch: false);
                            Assert.Equal(key, tile.ColorBrushName, StringComparer.Ordinal);
                            Assert.Same(application.TryFindResource(key), tile.Background);
                            Assert.NotNull(tile.Foreground);

                            int column = t % row.Columns;
                            bool edge = column is 0 || column == row.Columns - 1;
                            bool cornerRow = row.Rows is 1 || t / row.Columns is 0 || t / row.Columns == row.Rows - 1;
                            Assert.Equal(edge && cornerRow, tile.TileCornerRadius != middle);
                            Assert.Equal(column < row.Columns - 1, tile.ShowSeparator);
                        }
                    }
                }
            });
        }

        [Fact]
        public Task GalleryColorsPage_ExamplePanelsUseGalleryLayeringAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                Application application = WpfTestSta.EnsureApplication();
                Controls.SelectorBar selector = ColorSelector(page);

                for (int i = 0; i < selector.Items.Count; i++)
                {
                    SelectSection(selector, i, page.Dispatcher);
                    foreach (ColorPageExample example in DemoTestHost.FindVisualChildren<ColorPageExample>(SectionPanel(page)))
                    {
                        // WinUI Gallery ColorPageExample backgrounds: quarternary solid, except the accent and smoke groups.
                        string expectedKey = example.Title switch
                        {
                            "Text On Accent" => "AccentFillColorDefaultBrush",
                            "Smoke Background" => "SmokeFillColorDefaultBrush",
                            _ => "SolidBackgroundFillColorQuarternaryBrush",
                        };
                        Assert.Same(application.TryFindResource(expectedKey), example.Background);
                        Assert.NotNull(example.ExampleContent);
                    }
                }
            });
        }

        /// <summary>
        /// Text Control / Border, Accent Acrylic Background / Base and Accent Acrylic Background /
        /// Default resolve a pale fill in Light theme (a light grey elevation border and pale cyan
        /// acrylics), so the AlwaysWhiteText foreground they used to carry was unreadable. They now
        /// pick their foreground by contrast via <see cref="ColorTile.AutoContrastForeground"/>.
        /// </summary>
        [Fact]
        public Task GalleryColorsPage_ContrastSensitiveTiles_PickReadableForegroundInLightThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                Application application = WpfTestSta.EnsureApplication();
                Controls.SelectorBar selector = ColorSelector(page);
                Brush alwaysWhite = Assert.IsType<SolidColorBrush>(application.TryFindResource("TextOnAccentFillColorSelectedTextBrush"), exactMatch: false);

                string[] brushKeys =
                [
                    "TextControlElevationBorderBrush",
                    "AccentAcrylicBackgroundFillColorBaseBrush",
                    "AccentAcrylicBackgroundFillColorDefaultBrush",
                ];

                foreach (string brushKey in brushKeys)
                {
                    ColorTile tile = FindTileByBrushKey(page, selector, brushKey);
                    Assert.True(tile.AutoContrastForeground, brushKey + " should pick its foreground by contrast.");
                    Assert.NotSame(alwaysWhite, tile.Foreground);
                }
            });
        }

        /// <summary>
        /// The Control On Image Fill example floats a control over a generated sample photo
        /// instead of a flat accent fill, mirroring the WinUI Gallery example of a control over
        /// imagery. See <c language="text">GalleryColorsPage.CreateControlOnImageExample</c> and
        /// <c language="text">Resources/SampleMedia/ControlOnImageSample.png</c>.
        /// </summary>
        [Fact]
        public Task GalleryColorsPage_ControlOnImageFillExample_HostsImageWithSourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                Controls.SelectorBar selector = ColorSelector(page);
                ColorPageExample example = FindExampleByTitle(page, selector, "Control On Image Fill");

                Controls.Image photo = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Image>(example));
                Assert.NotNull(photo.Source);
            });
        }

        [Fact]
        public Task GalleryColorsPage_TileCopyButtonsCarryBrushKeysAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                Controls.SelectorBar selector = ColorSelector(page);
                SortedSet<string> copiedKeys = new(StringComparer.Ordinal);

                for (int i = 0; i < selector.Items.Count; i++)
                {
                    SelectSection(selector, i, page.Dispatcher);
                    foreach (ColorTile tile in DemoTestHost.FindVisualChildren<ColorTile>(SectionPanel(page)))
                    {
                        Controls.Button copy = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Button>(tile));
                        Assert.Equal(tile.ColorBrushName, copy.Tag as string, StringComparer.Ordinal);
                        Assert.Equal("Copy brush name", AutomationProperties.GetName(copy), StringComparer.Ordinal);
                        _ = copiedKeys.Add(tile.ColorBrushName);
                    }
                }

                Assert.Contains("TextFillColorPrimaryBrush", copiedKeys);
                Assert.Contains("AccentFillColorDefaultBrush", copiedKeys);
                Assert.Contains("SystemColorWindowTextColorBrush", copiedKeys);
            });
        }

        // This test cycles the app theme, so it builds its own page rather than mutating the one the class shares.
        [Fact]
        public Task GalleryColorsPage_BrushKeys_ResolveAcrossThemesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                GalleryColorsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    SortedSet<string> resourceKeys = new(StringComparer.Ordinal);
                    Controls.SelectorBar selector = ColorSelector(page);
                    for (int i = 0; i < selector.Items.Count; i++)
                    {
                        SelectSection(selector, i, window.Dispatcher);
                        foreach (ColorTile tile in DemoTestHost.FindVisualChildren<ColorTile>(SectionPanel(page)).Where(static tile => tile.Tag is not null))
                        {
                            // Palette tiles name a Color rather than a live brush; they carry no Tag.
                            _ = resourceKeys.Add(tile.ColorBrushName);
                        }
                    }

                    Assert.True(resourceKeys.Count >= 100, "Colors page should expose the WinUI brush catalogue.");

                    ApplicationTheme[] themes =
                    [
                        ApplicationTheme.Light,
                        ApplicationTheme.Dark,
                        ApplicationTheme.HighContrast,
                    ];

                    List<string> unresolved = [];
                    foreach (ApplicationTheme theme in themes)
                    {
                        ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                        ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                        // Elevation strokes resolve as gradient brushes, so only presence is asserted.
                        unresolved.AddRange(resourceKeys.Where(key => application.TryFindResource(key) is not Brush).Select(key => theme + ": " + key));
                    }

                    Assert.Empty(unresolved);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public async Task GalleryColorsPage_SourceHasNoLiteralGeometryOrColorsAsync()
        {
            string[] files =
            [
                "GalleryColorsPage.xaml",
                "GalleryColorsPage.xaml.cs",
                "ColorTile.xaml",
                "ColorPageExample.xaml",
            ];

            string[] forbidden =
            [
                "Foreground=\"Black\"",
                "Foreground=\"White\"",
                "=\"#",
                "WrapPanel",
                "ThemeDictionary",
                "new Thickness(",
                "new CornerRadius(",
                "DemoSampleControl",
                "DemoCodeSampleTextStyle",
            ];

            foreach (string file in files)
            {
                string source = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "Pages", file).ConfigureAwait(true);
                Assert.DoesNotContain(forbidden, value => source.Contains(value, StringComparison.Ordinal));
                if (file.EndsWith(".xaml", StringComparison.Ordinal))
                {
                    // Every geometry attribute in the page XAML is a resource reference or a binding, never a literal.
                    string? literal = FindLiteralGeometryAttribute(source);
                    Assert.True(literal is null, file + " carries a literal geometry attribute: " + literal);
                }
            }
        }

        private static string? FindLiteralGeometryAttribute(string source)
        {
            foreach (string token in (ReadOnlySpan<string>)["Margin=\"", "Padding=\"", "CornerRadius=\"", "FontSize=\"", "Width=\"", "Height=\"", "MinWidth=\"", "MinHeight=\"", "BorderThickness=\""])
            {
                int index = source.IndexOf(token, StringComparison.Ordinal);
                while (index >= 0)
                {
                    bool wordBoundary = index is 0 || !char.IsLetterOrDigit(source[index - 1]);
                    int valueStart = index + token.Length;
                    if (wordBoundary && valueStart < source.Length)
                    {
                        string value = source.Substring(valueStart, Math.Min(8, source.Length - valueStart));
                        if (value[0] is not '{' and not '*' && !value.StartsWith("Auto", StringComparison.Ordinal))
                        {
                            return token + value;
                        }
                    }

                    index = source.IndexOf(token, valueStart, StringComparison.Ordinal);
                }
            }

            return null;
        }

        private GalleryColorsPage Page()
        {
            return _page ?? throw new InvalidOperationException("Page was not initialized.");
        }

        private static Controls.SelectorBar ColorSelector(GalleryColorsPage page)
        {
            return Assert.IsType<Controls.SelectorBar>(DemoTestHost.FindByName<Controls.SelectorBar>(page, "ColorSectionSelector"), exactMatch: false);
        }

        /// <summary>
        /// The copy button copies the brush key and acknowledges it with the Gallery's success cue:
        /// the glyph swaps to a checkmark and the tooltip reports the copy. The cue is tied to the
        /// copy actually landing, so what is asserted here is that the two agree. The clipboard is
        /// a single machine wide resource that this suite cannot own (another process holding it
        /// makes both a copy and a seeded read fail, and a stale value then reads exactly like a
        /// fresh copy), so the round trip itself is out of scope; the invariant that matters, and
        /// the one the cue previously broke, is that the button never claims a copy it did not make.
        /// </summary>
        [Fact]
        public Task GalleryColorsPage_CopyButton_CueAgreesWithWhetherTheCopyLandedAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                GalleryColorsPage page = Page();
                ColorTile tile = DemoTestHost.FindVisualChildren<ColorTile>(page).First();
                Controls.Button copy = Assert.IsType<Controls.Button>(
                    DemoTestHost.FindByName<Controls.Button>(tile, "CopyBrushNameButton"), exactMatch: false);
                Controls.FontIcon glyph = Assert.IsType<Controls.FontIcon>(
                    DemoTestHost.FindByName<Controls.FontIcon>(tile, "CopyBrushNameGlyph"), exactMatch: false);

                Assert.True(copy.IsHitTestVisible, "The copy button must stay clickable.");
                Assert.Equal("", glyph.Glyph, StringComparer.Ordinal);
                Assert.Equal("Copy brush name", copy.ToolTip as string, StringComparer.Ordinal);

                copy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                // DemoClipboard retries on a background priority dispatcher timer, five attempts
                // 40 ms apart, so the outcome is not settled when the click returns.
                await DispatcherDelayWaits.WaitForAnimationAndDrainByDelayAsync(page.Dispatcher, 400).ConfigureAwait(true);

                // Exactly one of the two states, never a checkmark over the resting tooltip or the
                // reverse, which is what an unconditional cue produced.
                if (string.Equals(glyph.Glyph, "", StringComparison.Ordinal))
                {
                    Assert.Equal("Brush name copied to clipboard", copy.ToolTip as string, StringComparer.Ordinal);
                }
                else
                {
                    Assert.Equal("", glyph.Glyph, StringComparer.Ordinal);
                    Assert.Equal("Copy brush name", copy.ToolTip as string, StringComparer.Ordinal);
                }
            });
        }

        /// <summary>
        /// WPF's Page sets its own Foreground from SystemColors instead of inheriting it, so page
        /// text rendered black on the dark theme until a theme change re-resolved it. The demo's
        /// implicit Page style puts the theme brush back.
        /// </summary>
        [Fact]
        public Task GalleryColorsPage_InDarkTheme_StartsWithThemeForegroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = DemoTestHost.EnsureDemoTheme();
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);

                GalleryColorsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Color expected = Assert.IsType<SolidColorBrush>(application.TryFindResource("TextFillColorPrimaryBrush"), exactMatch: false).Color;
                    GalleryPageHeader header = DemoTestHost.FindVisualChildren<GalleryPageHeader>(page).First();
                    TextBlock title = DemoTestHost.FindVisualChildren<TextBlock>(header).First();

                    Assert.Equal(expected, Assert.IsType<SolidColorBrush>(title.Foreground, exactMatch: false).Color);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                }
            });
        }

        /// <summary>
        /// The page content fills the shell, which matters most in Top pane mode where the pane
        /// gives its width back to the content. Tile labels wrap when the column is narrow enough,
        /// exactly as the Gallery does at its own width.
        /// </summary>
        [Fact]
        public Task GalleryColorsPage_ContentStretchesToTheShellWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = Page();
                Grid content = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "PageContent"), exactMatch: false);
                Assert.True(double.IsPositiveInfinity(content.MaxWidth), "The page content must not cap its width.");
                Assert.Equal(HorizontalAlignment.Stretch, content.HorizontalAlignment);

                Grid root = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "PageRoot"), exactMatch: false);
                Thickness margin = content.Margin;
                Assert.Equal(root.ActualWidth - margin.Left - margin.Right, content.ActualWidth, 1);
            });
        }

        private static Controls.SlideNavigationPresenter SectionPresenter(GalleryColorsPage page)
        {
            return Assert.IsType<Controls.SlideNavigationPresenter>(DemoTestHost.FindByName<Controls.SlideNavigationPresenter>(page, "ColorSectionPresenter"), exactMatch: false);
        }

        private static DependencyObject SectionPanel(GalleryColorsPage page)
        {
            return Assert.IsType<DependencyObject>(SectionPresenter(page).Content, exactMatch: false);
        }

        private static List<UniformGrid> TileRows(DependencyObject section)
        {
            return [.. DemoTestHost.FindVisualChildren<UniformGrid>(section).Where(static row => row.Children.Count > 0 && row.Children[0] is ColorTile)];
        }

        private static void SelectSection(Controls.SelectorBar selector, int index, Dispatcher dispatcher)
        {
            selector.SelectedIndex = index;
            WpfTestSta.DrainDispatcher(dispatcher);
            selector.UpdateLayout();
            WpfTestSta.DrainDispatcher(dispatcher);
        }

        private static ColorTile FindTileByBrushKey(GalleryColorsPage page, Controls.SelectorBar selector, string brushKey)
        {
            for (int i = 0; i < selector.Items.Count; i++)
            {
                SelectSection(selector, i, page.Dispatcher);
                ColorTile? match = DemoTestHost.FindVisualChildren<ColorTile>(SectionPanel(page))
                    .FirstOrDefault(tile => string.Equals(tile.ColorBrushName, brushKey, StringComparison.Ordinal));
                if (match is not null)
                {
                    return match;
                }
            }

            throw new InvalidOperationException("No tile found for brush key " + brushKey);
        }

        private static ColorPageExample FindExampleByTitle(GalleryColorsPage page, Controls.SelectorBar selector, string title)
        {
            for (int i = 0; i < selector.Items.Count; i++)
            {
                SelectSection(selector, i, page.Dispatcher);
                ColorPageExample? match = DemoTestHost.FindVisualChildren<ColorPageExample>(SectionPanel(page))
                    .FirstOrDefault(example => string.Equals(example.Title, title, StringComparison.Ordinal));
                if (match is not null)
                {
                    return match;
                }
            }

            throw new InvalidOperationException("No example found with title " + title);
        }

        private sealed class SectionExpectation(string title, string[] exampleTitles, int[] rowColumns)
        {
            public string Title { get; } = title;

            public string[] ExampleTitles { get; } = exampleTitles;

            public int[] RowColumns { get; } = rowColumns;
        }
    }
}
