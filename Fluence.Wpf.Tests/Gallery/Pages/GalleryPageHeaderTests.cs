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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    public sealed class GalleryPageHeaderTests
    {
        [Fact]
        public Task GalleryPageHeader_DocsDropDown_TracksDocsAnchorVisibilityAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.DropDownButton docsButton = Assert.IsType<Controls.DropDownButton>(DemoTestHost.FindByName<Controls.DropDownButton>(header, "DocsDropDownButton"), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, docsButton.Visibility);
                    Assert.Null(header.DocumentationUri);

                    header.DocsAnchor = "basic-actions";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, docsButton.Visibility);
                    Assert.Equal("https://github.com/sintaxasn/Fluence.Wpf/blob/main/docs/controls.md#basic-actions", header.DocumentationUri?.AbsoluteUri, StringComparer.Ordinal);

                    header.DocsDocument = "theming.md";
                    header.DocsAnchor = "canonical-token-families";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("https://github.com/sintaxasn/Fluence.Wpf/blob/main/docs/theming.md#canonical-token-families", header.DocumentationUri?.AbsoluteUri, StringComparer.Ordinal);
                    Controls.HyperlinkButton docsLink = Assert.IsType<Controls.HyperlinkButton>(header.FindName("DocsLink"), exactMatch: false);
                    Assert.Equal("Theming guide", docsLink.Content as string, StringComparer.Ordinal);
                    Assert.Equal(header.DocumentationUri, docsLink.NavigateUri);

                    header.DocsAnchor = string.Empty;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, docsButton.Visibility);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_ThemeToggle_DisabledUnderHighContrastEnabledUnderLightAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.Button themeToggle = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "ThemeToggleButton"), exactMatch: false);
                    Assert.True(themeToggle.IsEnabled, "The theme toggle should be enabled while the resolved theme is Light.");

                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(themeToggle.IsEnabled, "The theme toggle should be disabled and inert while the resolved theme is HighContrast.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(themeToggle.IsEnabled, "The theme toggle should re-enable once the resolved theme leaves HighContrast.");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_ThemeToggle_ClickFromLightResolvesToDarkAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme);

                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.Button themeToggle = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "ThemeToggleButton"), exactMatch: false);

                    themeToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, themeToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);

                    // The click above resolved the theme to Dark. Restore the neutral Light
                    // state the neighbouring tests in this file expect at their own start, so
                    // this test never leaves Dark applied for whatever runs after it.
                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                    _ = DemoTestHost.EnsureDemoTheme();
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_ThemeToggle_PreservesShellBackdropAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                Application application = DemoTestHost.EnsureDemoTheme();
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme);

                // ThemeToggleButton_Click resolves its owner through Application.Current.MainWindow
                // (see GalleryPageHeader.xaml.cs), exactly as App.xaml.cs assigns it at startup
                // (MainWindow = mainWindow;). The backdrop is preserved only when a real MainWindow
                // occupies that property, so this test hosts the header inside one instead of the
                // plain Window the other tests in this file use.
                MainWindow window = new()
                {
                    Left = -20000,
                    Top = -20000,
                    Width = 1200,
                    Height = 900,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                };
                application.MainWindow = window;
                window.Show();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                try
                {
                    // Mirror GallerySettingsPage.BackdropComboBox_SelectionChanged: set the shell's
                    // backdrop DP and apply it through the theme manager together.
                    window.SystemBackdropType = WindowBackdropType.Acrylic;
                    ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, WindowBackdropType.Acrylic);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.NavigateTo("buttons");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    GalleryPageHeader header = Assert.Single(DemoTestHost.FindVisualChildren<GalleryPageHeader>(window));
                    Controls.Button themeToggle = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "ThemeToggleButton"), exactMatch: false);

                    themeToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, themeToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme);
                    Assert.Equal(WindowBackdropType.Acrylic, ApplicationThemeManager.CurrentBackdrop);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    application.MainWindow = null;

                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                    _ = DemoTestHost.EnsureDemoTheme();
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_FavoriteToggle_FlipsGlyphWhenCheckedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.ToggleButton favoriteToggle = Assert.IsType<Controls.ToggleButton>(DemoTestHost.FindByName<Controls.ToggleButton>(header, "FavoriteToggleButton"), exactMatch: false);
                    Controls.FontIcon favoriteIcon = Assert.IsType<Controls.FontIcon>(DemoTestHost.FindByName<Controls.FontIcon>(header, "FavoriteIcon"), exactMatch: false);
                    Assert.Equal("\uE734", favoriteIcon.Glyph);

                    favoriteToggle.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("\uE735", favoriteIcon.Glyph);

                    favoriteToggle.IsChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("\uE734", favoriteIcon.Glyph);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // WinUI Gallery PageHeader.xaml: the title sits alone on row 0 in TitleTextBlockStyle; row 1 holds the
        // Documentation and Source drop-downs on the left and theme, copy-link and favorite on the right.
        [Fact]
        public Task GalleryPageHeader_Layout_MirrorsWinUiPageHeaderRowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                Application application = DemoTestHost.EnsureDemoTheme();
                GalleryPageHeader header = new() { Title = "Buttons", DocsAnchor = "basic-actions" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    TextBlock title = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(header, "TitleTextBlock"), exactMatch: false);
                    Assert.Same(application.TryFindResource("TitleTextBlockStyle"), title.Style);
                    Assert.Equal(0, Grid.GetRow(title));

                    Controls.DropDownButton docs = Assert.IsType<Controls.DropDownButton>(DemoTestHost.FindByName<Controls.DropDownButton>(header, "DocsDropDownButton"), exactMatch: false);
                    Controls.DropDownButton source = Assert.IsType<Controls.DropDownButton>(DemoTestHost.FindByName<Controls.DropDownButton>(header, "SourceDropDownButton"), exactMatch: false);
                    Controls.Button theme = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "ThemeToggleButton"), exactMatch: false);
                    Controls.Button copyLink = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "CopyLinkButton"), exactMatch: false);
                    Controls.ToggleButton favorite = Assert.IsType<Controls.ToggleButton>(DemoTestHost.FindByName<Controls.ToggleButton>(header, "FavoriteToggleButton"), exactMatch: false);

                    // Left to right on the action row, below the title.
                    double titleBottom = title.TranslatePoint(new Point(0, title.ActualHeight), header).Y;
                    foreach (FrameworkElement element in new FrameworkElement[] { docs, source, theme, copyLink, favorite })
                    {
                        Assert.True(element.TranslatePoint(new Point(0, 0), header).Y >= titleBottom, element.Name + " should sit below the title row.");
                    }

                    double[] lefts = [.. new FrameworkElement[] { docs, source, theme, copyLink, favorite }.Select(element => element.TranslatePoint(new Point(0, 0), header).X)];
                    for (int i = 1; i < lefts.Length; i++)
                    {
                        Assert.True(lefts[i] > lefts[i - 1], "Action row controls should run left to right in WinUI PageHeader order.");
                    }

                    // 31 dip with a 0.5 tolerance: UseLayoutRounding snaps the button to whole
                    // device pixels, so a 31 dip request renders 46 px at 150% scale, which reads
                    // back as 30.67 dip. The WinUI 3 Gallery's own action buttons measure 40 x 31.3.
                    Assert.Equal(31, theme.ActualHeight, 0.5);
                    Assert.Equal(40, theme.ActualWidth, 0.5);
                    Assert.Equal(31, copyLink.ActualHeight, 0.5);
                    Assert.Equal(40, copyLink.ActualWidth, 0.5);
                    Assert.Equal(31, favorite.ActualHeight, 0.5);
                    Assert.Equal(40, favorite.ActualWidth, 0.5);
                    Assert.Equal("Copy link", AutomationProperties.GetName(copyLink), StringComparer.Ordinal);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // The Source drop-down derives the page XAML and code-behind links from the hosting page type.
        [Fact]
        public Task GalleryPageHeader_SourceLinks_ResolveFromHostingPageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                GalleryButtonsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    GalleryPageHeader header = Assert.Single(DemoTestHost.FindVisualChildren<GalleryPageHeader>(page));
                    Controls.HyperlinkButton xaml = Assert.IsType<Controls.HyperlinkButton>(header.FindName("PageXamlLink"), exactMatch: false);
                    Controls.HyperlinkButton code = Assert.IsType<Controls.HyperlinkButton>(header.FindName("PageCodeLink"), exactMatch: false);
                    Assert.Equal("https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml", xaml.NavigateUri.AbsoluteUri, StringComparer.Ordinal);
                    Assert.Equal("https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml.cs", code.NavigateUri.AbsoluteUri, StringComparer.Ordinal);
                    Assert.Equal(xaml.NavigateUri, header.PageXamlUri);

                    StackPanel controlSource = Assert.IsType<StackPanel>(header.FindName("ControlSourcePanel"), exactMatch: false);
                    Assert.Equal(string.IsNullOrWhiteSpace(header.ControlSourcePath) ? Visibility.Collapsed : Visibility.Visible, controlSource.Visibility);

                    header.ControlSourcePath = "Fluence.Wpf/Themes/Controls/Button.xaml";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, controlSource.Visibility);
                    Controls.HyperlinkButton control = Assert.IsType<Controls.HyperlinkButton>(header.FindName("ControlSourceLink"), exactMatch: false);
                    Assert.Equal("https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Themes/Controls/Button.xaml", control.NavigateUri.AbsoluteUri, StringComparer.Ordinal);
                    Assert.Equal("Button.xaml", control.Content as string, StringComparer.Ordinal);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }
    }
}
