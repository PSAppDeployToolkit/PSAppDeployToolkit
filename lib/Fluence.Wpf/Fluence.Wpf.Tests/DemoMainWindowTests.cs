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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public sealed class DemoMainWindowTests
    {
        private static readonly DemoPageExpectation[] PageExpectations =
        [
            new("icons", typeof(GalleryIconsPage)),
            new("typography", typeof(GalleryTypographyPage)),
            new("accessibility", typeof(GalleryAccessibilityPage)),
            new("buttons", typeof(GalleryButtonsPage)),
            new("selection", typeof(GallerySelectionPage)),
            new("inputs", typeof(GalleryInputsPage)),
            new("data binding", typeof(GalleryDataBindingPage)),
            new("data", typeof(GalleryDataPage)),
            new("trees", typeof(GalleryTreesPage)),
            new("menus", typeof(GalleryMenusPage)),
            new("navigation", typeof(GalleryNavigationPage)),
            new("tabs", typeof(GalleryTabsPage)),
            new("layout", typeof(GalleryLayoutPage)),
            new("status", typeof(GalleryStatusPage)),
        ];

        [Fact]
        public Task MainWindow_DirectNavigation_LoadsConcretePagesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        window.NavigateTo(expectation.Tag);
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();
                        WpfTestSta.DrainDispatcher(window.Dispatcher);

                        object content = Assert.IsAssignableFrom<object>(GetSelectedPageContent(window));
                        Assert.Equal(expectation.PageType, content.GetType());
                        Assert.NotEqual("GalleryControlPage", content.GetType().Name, StringComparer.Ordinal);
                        Assert.NotEqual("GalleryCategoryPage", content.GetType().Name, StringComparer.Ordinal);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_InitialSelection_LoadsHomePageContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    object content = Assert.IsAssignableFrom<object>(GetSelectedPageContent(window));
                    Assert.Equal(typeof(GalleryHomePage), content.GetType());

                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    Assert.Same(content, nav.Content);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryHomePage_HeroSwapsHeaderLockupWithThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryHomePage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    System.Windows.Controls.Image image = Assert.IsAssignableFrom<System.Windows.Controls.Image>(FindByName<System.Windows.Controls.Image>(page, "BrandHeroImage"));

                    DrawingImage light = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderLightDrawingImage"));
                    DrawingImage dark = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderDarkDrawingImage"));

                    // The hero shows the lockup drawn for the active theme and swaps on
                    // theme changes via the page's ThemeDictionary (no code-behind).
                    Assert.Same(light, image.Source);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(dark, image.Source);

                    // High contrast has no fixed polarity, so the page picks whichever
                    // variant reads against the live system window color.
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(ReferenceEquals(image.Source, light) || ReferenceEquals(image.Source, dark),
                        "High contrast should show one of the two header lockups.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(light, image.Source);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public async Task GalleryHomePage_UsesHeaderLockupHeroAndGitHubLinkAsync()
        {
            string homePage = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "Pages", "GalleryHomePage.xaml").ConfigureAwait(true);
            Assert.Contains("FluenceHeaderLightDrawingImage", homePage, StringComparison.Ordinal);
            Assert.Contains("https://github.com/sintaxasn/fluence.wpf", homePage, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Library_EmbedsXamlBrandIcons_AndDemosSetBrandApplicationIconAsync()
        {
            // The Fluence brand icon ships as resolution-independent vector DrawingImages in
            // Fluence.Wpf\Themes\Icons\FluenceIcons.xaml (merged into Generic.xaml), replacing the
            // multi-resolution assets\Fluence.ico that previously dominated the library binary.
            // FluenceWindow rasterizes the brand vector for its default Window.Icon, so neither demo
            // sets Icon= in XAML (both inherit the embedded default at runtime). The demo executables
            // do set ApplicationIcon to the brand .ico so the .exe file icon in Explorer is the brand mark.
            string libraryProject = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Fluence.Wpf.csproj").ConfigureAwait(true);
            Assert.False(libraryProject.Contains("Fluence.ico", StringComparison.Ordinal),
                "The library should no longer embed assets\\Fluence.ico now that the brand icon is a XAML vector.");
            Assert.Contains("<PackageIcon>Fluence_Icon_Light_128.png</PackageIcon>", libraryProject, StringComparison.Ordinal);

            // The three brand DrawingImages live in a dedicated icon dictionary that is merged into
            // Generic.xaml so the keys resolve from application resources.
            Assert.True(File.Exists(DemoTestHost.GetRepositoryFilePath("Fluence.Wpf", "Themes", "Icons", "FluenceIcons.xaml")),
                "The brand icon dictionary should exist at Fluence.Wpf\\Themes\\Icons\\FluenceIcons.xaml.");
            string iconDictionary = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Themes", "Icons", "FluenceIcons.xaml").ConfigureAwait(true);
            Assert.Contains("FluenceIconBrandDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("FluenceIconLightDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("FluenceIconDarkDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("Themes/Icons/FluenceIcons.xaml", await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Themes", "Generic.xaml").ConfigureAwait(true), StringComparison.Ordinal);

            // Both demo executables set their ApplicationIcon to the Fluence brand .ico so the .exe
            // shows the brand mark in Explorer and on a pre-launch taskbar pin.
            string galleryProject = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "Fluence.Wpf.Demo.csproj").ConfigureAwait(true);
            Assert.Contains("<ApplicationIcon>", galleryProject, StringComparison.Ordinal);
            Assert.Contains("Fluence_Icon_Light.ico", galleryProject, StringComparison.Ordinal);
            string mvvmProject = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo.Mvvm", "Fluence.Wpf.Demo.Mvvm.csproj").ConfigureAwait(true);
            Assert.Contains("<ApplicationIcon>", mvvmProject, StringComparison.Ordinal);
            Assert.Contains("Fluence_Icon_Light.ico", mvvmProject, StringComparison.Ordinal);

            Assert.False((await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "MainWindow.xaml").ConfigureAwait(true)).Contains("Icon=\"", StringComparison.Ordinal),
                "The gallery demo window should inherit the embedded FluenceWindow icon, not set Icon= itself.");
            Assert.False((await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo.Mvvm", "MainWindow.xaml").ConfigureAwait(true)).Contains("Icon=\"", StringComparison.Ordinal),
                "The MVVM demo window should inherit the embedded FluenceWindow icon, not set Icon= itself.");

            // The retired .ico is gone from the tree.
            Assert.False(File.Exists(DemoTestHost.GetRepositoryFilePath("assets", "Fluence.ico")),
                "assets\\Fluence.ico should be deleted once the XAML vector icons replace it.");
        }

        [Fact]
        public Task MainWindow_Search_NavigatesToGroupedConcretePageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));

                    search.Text = "progress ring";
                    search.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Enter)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    object content = GetSelectedPageContent(window);
                    Assert.Equal(typeof(GalleryStatusPage), content.GetType());
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_BackRequested_WalksVisitedPagesInOrderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));

                    window.NavigateTo("buttons");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.NavigateTo("trees");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.NavigateTo("status");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(typeof(GalleryStatusPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryTreesPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryButtonsPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryHomePage), GetSelectedPageContent(window).GetType());

                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    Assert.False(nav.IsBackEnabled,
                        "Back should become disabled when the demo history is empty.");

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryHomePage), GetSelectedPageContent(window).GetType());
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_NavigationCatalog_RemovesWindowingPage()
        {
            List<DemoNavigationItem> items = [.. DemoNavigationCatalog.Items];
            Assert.True(items.Count >= 1, "Navigation catalog should contain at least one entry.");
            Assert.Equal("Accessibility", items[^1].Title, StringComparer.Ordinal);
            Assert.False(items.Exists(static item => string.Equals(item.Title, "Windowing", StringComparison.Ordinal)),
                "Windowing should not remain as a regular NavigationView item.");
            Assert.False(items.Exists(static item => string.Equals(item.Route, "window", StringComparison.Ordinal)),
                "The old Windowing route should be removed from the regular navigation catalog.");
        }

        [Fact]
        public Task GalleryPages_UseSharedWinUiGalleryPageLayoutAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                Style scrollStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("GalleryPageScrollViewerStyle"));
                Style fluentScrollStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("ScrollViewerStyle"));
                Style contentStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("GalleryPageContentStackStyle"));
                Style contentGridStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("GalleryPageContentGridStyle"));
                Assert.Same(fluentScrollStyle, scrollStyle.BasedOn);

                UserControl[] pages =
                [
                    new GalleryHomePage(),
                    new GalleryIconsPage(),
                    new GalleryTypographyPage(),
                    new GalleryAccessibilityPage(),
                    new GalleryButtonsPage(),
                    new GallerySelectionPage(),
                    new GalleryInputsPage(),
                    new GalleryFormsPage(),
                    new GalleryDataPage(),
                    new GalleryDataBindingPage(),
                    new GalleryTreesPage(),
                    new GalleryMenusPage(),
                    new GalleryNavigationPage(),
                    new GalleryTabsPage(),
                    new GalleryLayoutPage(),
                    new GalleryStatusPage(),
                    new GallerySettingsPage(),
                ];

                foreach (UserControl page in pages)
                {
                    Window window = CreateHostWindow(page);
                    try
                    {
                        if (page is GalleryIconsPage)
                        {
                            Grid pageRoot = Assert.IsAssignableFrom<Grid>(FindByName<Grid>(page, "PageRoot"));
                            Assert.Null(pageRoot.Background);

                            Grid pageContent = Assert.IsAssignableFrom<Grid>(FindByName<Grid>(page, "PageContent"));
                            Assert.Same(contentGridStyle, pageContent.Style);
                            Assert.Equal(new Thickness(36, 24, 36, 48), pageContent.Margin);
                            Assert.True(double.IsPositiveInfinity(pageContent.MaxWidth),
                                "Icons should stretch instead of keeping the old max content width.");
                            Assert.Equal(HorizontalAlignment.Stretch, pageContent.HorizontalAlignment);
                            continue;
                        }

                        SmoothScrollViewer scrollViewer = Assert.IsAssignableFrom<SmoothScrollViewer>(FindVisualChild<SmoothScrollViewer>(page));
                        Assert.Same(scrollStyle, scrollViewer.Style);

                        System.Windows.Controls.StackPanel content = Assert.IsType<System.Windows.Controls.StackPanel>(scrollViewer.Content);
                        Assert.Same(contentStyle, content.Style);
                        Assert.Equal(new Thickness(36, 24, 36, 48), content.Margin);
                        Assert.True(double.IsPositiveInfinity(content.MaxWidth),
                            page.GetType().Name + " should stretch instead of keeping the old max content width.");
                        Assert.Equal(HorizontalAlignment.Stretch, content.HorizontalAlignment);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            });
        }

        [Fact]
        public Task MainWindow_TitleBarSearch_StaysVisibleWhenContentExtendsIntoTitleBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                    Assert.Equal(Visibility.Visible, search.Visibility);

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Visible, search.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TitleBarSearch_IsCenteredInWindowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                    Assert.Equal(300.0, search.Width, 0.01);
                    Assert.Equal(300.0, search.MinWidth, 0.01);
                    Assert.Equal(475.0, search.MaxWidth, 0.01);
                    Assert.Equal(300.0, search.ActualWidth, 0.5);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window), 1.0);
                    Assert.Equal(GetVisualCenterY(shellTitleBar, window) + 4.0, GetVisualCenterY(search, window), 1.0);

                    Assert.True(search.Focus(), "Search should accept keyboard focus.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(300.0, search.ActualWidth, 0.5);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window), 1.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_UsesHorizontalNavigationChromeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    window.NavigateTo("buttons");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));

                    System.Windows.Controls.Button titleBarToggle = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton"));
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);
                    Assert.Equal(40.0, titleBarToggle.ActualWidth, 0.5);

                    System.Windows.Controls.TextBlock titleBarGlyph = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChild<System.Windows.Controls.TextBlock>(titleBarToggle));
                    Assert.Equal(16.0, titleBarGlyph.FontSize, 0.01);

                    System.Windows.Controls.Button titleBarBack = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_BackButton"));
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(titleBarToggle, window), "Back should occupy the first title-bar navigation slot.");
                    System.Windows.Controls.TextBlock titleBarBackGlyph = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChild<System.Windows.Controls.TextBlock>(titleBarBack));

                    NavigationViewItem firstItem = Assert.IsType<NavigationViewItem>(nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null);
                    FontIcon itemGlyph = Assert.IsAssignableFrom<FontIcon>(FindVisualChild<FontIcon>(firstItem));
                    Assert.Equal(GetVisualCenterX(itemGlyph, window), GetVisualCenterX(titleBarBackGlyph, window), 2.5);

                    ContentPresenter titleIcon = Assert.IsAssignableFrom<ContentPresenter>(FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"));
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    System.Windows.Controls.Image titleIconImage = Assert.IsAssignableFrom<System.Windows.Controls.Image>(FindVisualChild<System.Windows.Controls.Image>(titleIcon));
                    Assert.Equal(20.0, titleIconImage.ActualWidth, 0.5);
                    Assert.Equal(20.0, titleIconImage.ActualHeight, 0.5);
                    Assert.True(GetVisualX(titleIcon, window) >= GetVisualX(titleBarToggle, window) + titleBarToggle.ActualWidth - 0.5,
                        "Title identity should start after the title-bar navigation slot.");

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button internalToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartPaneToggleButton, nav));
                    Assert.Equal(Visibility.Collapsed, internalToggle.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_FirstGlyphTracksBackAvailabilityAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    nav.IsBackButtonVisible = true;
                    nav.IsBackEnabled = true;

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    System.Windows.Controls.Button titleBarBack = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_BackButton"));
                    System.Windows.Controls.Button titleBarToggle = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton"));
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(titleBarToggle, window), "Back should occupy the first title-bar navigation slot.");
                    Assert.Equal(GetVisualCenterY(titleBarBack, window), GetVisualCenterY(titleBarToggle, window), 1.0);

                    ContentPresenter titleIcon = Assert.IsAssignableFrom<ContentPresenter>(FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"));
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    double titleIconWithBackX = GetVisualX(titleIcon, window);

                    nav.IsBackEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, titleBarBack.Visibility);
                    Assert.Equal(titleIconWithBackX - 42.0, GetVisualX(titleIcon, window), 1.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_KeepsNavigationItemsBelowTitleBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(42.0, window.TitleBarHeight, 0.01);

                    NavigationViewItem firstItem = Assert.IsType<NavigationViewItem>(nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null);
                    double? itemY = GetVisualY(firstItem, window);
                    Assert.True(itemY >= window.TitleBarHeight - 0.5,
                        "The first navigation item should be below the extended title bar. itemY=" + itemY.Value.ToString(format: null, CultureInfo.InvariantCulture) + ", titleBarHeight=" + window.TitleBarHeight.ToString(CultureInfo.InvariantCulture));
                    Assert.True(itemY <= window.TitleBarHeight + 14.0,
                        "The first navigation item should not keep the old extra title-bar spacer. itemY=" + itemY.Value.ToString(format: null, CultureInfo.InvariantCulture) + ", titleBarHeight=" + window.TitleBarHeight.ToString(CultureInfo.InvariantCulture));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TopPane_UsesNonExtendedTitleBarWithoutPaneToggleChromeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    window.ExtendsContentIntoTitleBar = false;
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    nav.IsPaneOpen = false;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    nav.IsBackEnabled = true;
                    nav.IsBackButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(window.ExtendsContentIntoTitleBar,
                        "Top NavigationView mode should keep the FluenceWindow title bar non-extended.");
                    Assert.True(nav.IsPaneOpen, "Top NavigationView mode should coerce IsPaneOpen=True.");
                    Assert.False(nav.IsPaneToggleButtonVisible,
                        "Top NavigationView mode should coerce the pane toggle hidden.");

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    System.Windows.Controls.Button titleBarToggle = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton"));
                    Assert.Equal(Visibility.Collapsed, titleBarToggle.Visibility);
                    System.Windows.Controls.Button titleBarBack = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_BackButton"));
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    System.Windows.Controls.TextBlock titleBarBackGlyph = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChild<System.Windows.Controls.TextBlock>(titleBarBack));
                    Assert.Equal(16.0, titleBarBackGlyph.FontSize, 0.01);
                    ContentPresenter titleIcon = Assert.IsAssignableFrom<ContentPresenter>(FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"));
                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(titleIcon, window), "Top mode back should be the first visible title-bar item.");
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(search, window), "Top mode back should appear before centered title-bar content.");

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button internalBack = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartBackButton, nav));
                    System.Windows.Controls.Button? internalToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    Assert.Equal(Visibility.Collapsed, internalBack.Visibility);
                    Assert.Null(internalToggle);

                    NavigationViewItem firstItem = Assert.IsType<NavigationViewItem>(nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null);
                    Assert.Equal(Visibility.Visible, firstItem.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsFooter_NavigatesToSelectableSettingsPageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    NavigationViewItem settings = Assert.IsAssignableFrom<NavigationViewItem>(FindByName<NavigationViewItem>(window, "SettingsNavigationItem"));
                    Assert.Null(FindByName<FrameworkElement>(window, "PaneModeToggle"));

                    InvokeSettingsItem(settings);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    _ = Assert.IsAssignableFrom<GallerySettingsPage>(nav.Content);
                    Assert.True(settings.IsSelected,
                        "The footer Settings item should show the same selected state as navigation list items.");
                    Assert.True(nav.FooterMenuItems.Contains(settings),
                        "Settings should live in the FooterMenuItems region.");
                    Assert.Same(settings, nav.SelectedFooterItem);
                    Assert.Null(nav.SelectedItem);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsFooter_CollapsesLabelWhenPaneClosedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    NavigationViewItem settings = Assert.IsAssignableFrom<NavigationViewItem>(FindByName<NavigationViewItem>(window, "SettingsNavigationItem"));

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    // As a FooterMenuItems entry, Settings uses the standard NavigationViewItem template:
                    // the label is collapsed/shown by the template (it is not emptied), exactly like the
                    // main menu items. Content stays "Settings" throughout.
                    Assert.Equal("Settings", settings.Content as string, StringComparer.Ordinal);
                    ContentPresenter label = Assert.IsAssignableFrom<ContentPresenter>(FindByName<ContentPresenter>(settings, "ContentPresenter"));
                    Assert.Equal(Visibility.Visible, label.Visibility);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    label = Assert.IsAssignableFrom<ContentPresenter>(FindByName<ContentPresenter>(settings, "ContentPresenter"));
                    Assert.Equal(Visibility.Collapsed, label.Visibility);
                    Assert.Equal(Visibility.Visible, settings.Visibility);
                    FontIcon settingsIcon = Assert.IsType<FontIcon>(settings.Icon);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsFooter_DoesNotForceTopPaneModeWhenOpenedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    NavigationViewItem settings = Assert.IsAssignableFrom<NavigationViewItem>(FindByName<NavigationViewItem>(window, "SettingsNavigationItem"));

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    InvokeSettingsItem(settings);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Opening Settings must preserve the real collapsed pane state.");

                    Controls.ComboBox navigationStyle = Assert.IsAssignableFrom<Controls.ComboBox>(FindByName<Controls.ComboBox>(nav.Content as DependencyObject, "NavigationStyleComboBox"));
                    Assert.Equal(2, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_NavigationStyleCombo_TracksExternalIsPaneOpenChangesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.ComboBox navigationStyle = Assert.IsAssignableFrom<Controls.ComboBox>(FindByName<Controls.ComboBox>(nav.Content as DependencyObject, "NavigationStyleComboBox"));

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(2, navigationStyle.SelectedIndex);

                    nav.IsPaneOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TopPane_OverflowButtonDoesNotOverlapTreesAtMinimumWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    window.Width = 698;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement overflowButton = Assert.IsAssignableFrom<FrameworkElement>(FindByName<FrameworkElement>(nav, NavigationView.PartTopOverflowButton));
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    int visibleNavigationItems = nav.Items.OfType<NavigationViewItem>().Count(static item => item.Visibility is Visibility.Visible);
                    Assert.True(visibleNavigationItems > 1,
                        "Top pane should show every navigation item that fits before the overflow button would overlap the Top toggle status.");
                    NavigationViewItem settings = Assert.IsAssignableFrom<NavigationViewItem>(FindByName<NavigationViewItem>(window, "SettingsNavigationItem"));
                    double overflowRight = GetVisualX(overflowButton, nav) + overflowButton.ActualWidth;
                    double settingsLeft = GetVisualX(settings, nav);
                    Assert.True(overflowRight <= settingsLeft - 4.0 + 1.5, "The three-dot overflow entry should stop before it overlaps the Settings item.");

                    NavigationViewItem trees = Assert.IsType<NavigationViewItem>(nav.Items.OfType<NavigationViewItem>().FirstOrDefault(navItem => string.Equals(navItem.Content as string, "Trees", StringComparison.Ordinal)));
                    if (trees.Visibility is Visibility.Visible)
                    {
                        double treesRight = GetVisualX(trees, nav) + trees.ActualWidth;
                        double overflowLeft = GetVisualX(overflowButton, nav);
                        Assert.True(treesRight <= overflowLeft - 4.0 + 1.5, "Trees must not overlap the three-dot overflow entry.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_NavigationStyleCombo_SwitchesPaneModeAndKeepsContentLiveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    object settingsPage = nav.Content
                        ?? throw new InvalidOperationException("Settings navigation should create a live Settings page.");
                    Controls.ComboBox navigationStyle = Assert.IsAssignableFrom<Controls.ComboBox>(FindByName<Controls.ComboBox>(settingsPage as DependencyObject, "NavigationStyleComboBox"));

                    navigationStyle.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.True(nav.IsPaneOpen,
                        "Choosing Left in Settings should open the left pane instead of preserving a compact state.");
                    Assert.Same(settingsPage, nav.Content);

                    navigationStyle.SelectedIndex = 2;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Choosing Left compact in Settings should close the pane.");

                    navigationStyle.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.Top, nav.PaneDisplayMode);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_NavigationStyleCombo_FollowsShellPaneToggleAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    object settingsPage = nav.Content
                        ?? throw new InvalidOperationException("Settings navigation should create a live Settings page.");
                    Controls.ComboBox navigationStyle = Assert.IsAssignableFrom<Controls.ComboBox>(FindByName<Controls.ComboBox>(settingsPage as DependencyObject, "NavigationStyleComboBox"));

                    navigationStyle.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.True(nav.IsPaneOpen, "Left navigation should be expanded before the pane toggle is clicked.");
                    Assert.Equal(1, navigationStyle.SelectedIndex);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    System.Windows.Controls.Button titleBarToggle = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton"));
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.True(nav.GetPaneColumnWidthForTesting() > 48.0,
                        "Collapsing Left navigation should start the sidebar width animation instead of snapping to compact width.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Clicking the shell pane toggle should collapse the Left pane.");
                    Assert.Equal(2, navigationStyle.SelectedIndex);

                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 220).ConfigureAwait(true);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.True(nav.GetPaneColumnWidthForTesting() < 280.0, "Expanding Left navigation should start from the current compact width instead of snapping open.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.True(nav.IsPaneOpen,
                        "Expanded Left should keep the pane open after the second pane-toggle click.");
                    Assert.Equal(1, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryNavigationPage_CompactSamplePaneToggleOpensPaneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryNavigationPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(page, "CompactNavigationDemo"));
                    Assert.False(nav.IsPaneOpen, "Compact sample should start collapsed.");

                    System.Windows.Controls.Button paneToggle = Assert.IsType<System.Windows.Controls.Button>(nav.Template.FindName(NavigationView.PartPaneToggleButton, nav));

                    Controls.Button? sampleToggle = FindByName<Controls.Button>(page, "CompactPaneToggleButton");
                    Assert.Null(sampleToggle);

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(nav.IsPaneOpen,
                        "Clicking the built-in compact pane toggle should open the sample pane.");

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(nav.IsPaneOpen,
                        "Clicking the built-in compact pane toggle should close the sample pane.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_TrimsTitleToSearchClearanceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 1200;
                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Trim Before Search");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    System.Windows.Controls.TextBlock titleText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText"));
                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                    Assert.Equal(Visibility.Visible, titleText.Visibility);
                    double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                    double searchLeft = GetVisualX(search, window);
                    double titleClearanceRight = searchLeft - 12.0;
                    Assert.True(titleRight <= titleClearanceRight, "The title text should not cross the 12px search clearance.");
                    Assert.Equal(titleClearanceRight, titleRight, 10.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_HidesTitleTextWhenItOverlapsSearchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 760;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    ContentPresenter titleIcon = Assert.IsAssignableFrom<ContentPresenter>(FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"));
                    System.Windows.Controls.TextBlock titleText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText"));
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                        double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window);
                        Assert.True(titleRight <= searchLeft - 12.0, "Visible title text must keep a 12px clearance before the search box.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_DoesNotLetTitleOverlapSearchAtMinimumWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 698;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Must Never Overlap Search");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window), 1.0);

                    System.Windows.Controls.TextBlock titleText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText"));
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window);
                        Assert.True(titleRight <= searchLeft - 12.0, "Visible title text must keep a 12px clearance before the centered search box.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_RestoresTitleTextWhenSearchHasRoomAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 760;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TitleBar shellTitleBar = Assert.IsAssignableFrom<TitleBar>(FindByName<TitleBar>(window, "ShellTitleBar"));
                    System.Windows.Controls.TextBlock titleText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText"));
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        Controls.TextBox setupSearch = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                        double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(setupSearch, window);
                        Assert.True(titleRight <= searchLeft - 12.0, "Setup should hide or trim title text before it crosses the 12px search clearance.");
                    }

                    window.Width = 1200;
                    window.SetUserShowTitle(show: true, "Fluence.Wpf");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    titleText = Assert.IsType<System.Windows.Controls.TextBlock>(FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText"));
                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));
                    Assert.Equal(Visibility.Visible, titleText.Visibility);
                    Assert.Equal("Fluence.Wpf", titleText.Text, StringComparer.Ordinal);
                    Assert.True(GetVisualX(titleText, window) + titleText.ActualWidth + 12.0 <= GetVisualX(search, window),
                        "Visible title text should keep the search clearance gap.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TitleBarSearch_DoesNotShiftWhenChromeOptionsChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Controls.TextBox search = Assert.IsAssignableFrom<Controls.TextBox>(FindByName<Controls.TextBox>(window, "NavSearchBox"));

                    double? initialX = GetVisualX(search, window);

                    window.SetUserShowIcon(show: false, window.Icon);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window), 1.0);

                    window.SetUserShowTitle(show: false, window.Title);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window), 1.0);

                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    window.IsMaximizeButtonVisible = Visibility.Collapsed;
                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window), 1.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DemoSampleControl_ExpanderUsesInMemorySourceTabsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<fluence:Button Content=\"Save\" />",
                    CSharpSource = "private void Save_Click(object sender, RoutedEventArgs e) { }",
                    DemoContent = new System.Windows.Controls.TextBlock { Text = "Visible sample" },
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    Controls.Expander expander = Assert.IsAssignableFrom<Controls.Expander>(FindByName<Controls.Expander>(sample, "SourceExpander"));
                    Assert.False(expander.IsExpanded, "Source starts collapsed.");

                    expander.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl tabs = Assert.IsAssignableFrom<TabControl>(FindByName<TabControl>(sample, "SourceTabControl"));
                    Assert.Equal(2, tabs.Items.Count);
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                    AssertSourceTab(tabs, "C#", sample.CSharpSource);

                    System.Windows.Controls.Border sampleCard = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(sample, "SampleCard"));
                    Assert.Equal(new CornerRadius(8, 8, 0, 0), sampleCard.CornerRadius);
                    Assert.Equal(new CornerRadius(0, 0, 8, 8), expander.CornerRadius);
                    Assert.Equal(new Thickness(1, 0, 1, 1), expander.BorderThickness);
                    Assert.Equal(GetVisualY(sampleCard, window) + sampleCard.ActualHeight, GetVisualY(expander, window), 0.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DemoSampleControl_SourceRendererPreservesIndentationAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<Grid>\n    <TextBlock Text=\"Indented\" />\n</Grid>",
                    CSharpSource = "private void Save()\n{\n    string value = \"Indented\";\n}",
                    DemoContent = new System.Windows.Controls.TextBlock { Text = "Visible sample" },
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    Controls.Expander expander = Assert.IsAssignableFrom<Controls.Expander>(FindByName<Controls.Expander>(sample, "SourceExpander"));
                    expander.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl tabs = Assert.IsAssignableFrom<TabControl>(FindByName<TabControl>(sample, "SourceTabControl"));
                    string renderedXaml = GetSourceTabText(tabs, "XAML");
                    string renderedCSharp = GetSourceTabText(tabs, "C#");

                    Assert.Contains("    <TextBlock", renderedXaml, StringComparison.Ordinal);
                    Assert.Contains("    string value", renderedCSharp, StringComparison.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DemoSampleControl_EmptyCSharpSourceAddsOnlyXamlTabAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<fluence:ToggleSwitch IsChecked=\"True\" />",
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    Controls.Expander? expander = FindByName<Controls.Expander>(sample, "SourceExpander");
                    _ = expander?.IsExpanded = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl? tabs = FindByName<TabControl>(sample, "SourceTabControl");
                    Assert.Equal(1, tabs?.Items.Count);
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_NonHomePagesExposeInlineSourceSamplesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        // Design-reference catalog pages (Typography, Iconography) render
                        // directly without DemoSampleControl source samples.
                        if (expectation.PageType == typeof(GalleryTypographyPage)
                            || expectation.PageType == typeof(GalleryIconsPage))
                        {
                            continue;
                        }

                        window.NavigateTo(expectation.Tag);
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();
                        WpfTestSta.DrainDispatcher(window.Dispatcher);

                        object content = GetSelectedPageContent(window);
                        DependencyObject root = Assert.IsAssignableFrom<DependencyObject>(content);

                        bool found = FindAllVisualChildren<DemoSampleControl>(root).Any(sample => !string.IsNullOrWhiteSpace(sample.XamlSource));
                        Assert.True(found, "Page must expose at least one inline XAML source sample: " + expectation.PageType.Name);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryStatusPage_DeterminateProgressRingUsesNumberBoxBindingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NumberBox valueBox = Assert.IsAssignableFrom<NumberBox>(FindByName<NumberBox>(page, "ProgressRingValueBox"));
                    ProgressRing ring = Assert.IsAssignableFrom<ProgressRing>(FindByName<ProgressRing>(page, "DeterminateProgressRing"));

                    Assert.Equal(1.0, valueBox.Minimum, 0.001);
                    Assert.Equal(100.0, valueBox.Maximum, 0.001);
                    Assert.Equal(50.0, valueBox.Value, 0.001);
                    Assert.Equal(50.0, ring.Value, 0.001);

                    valueBox.Value = 75;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(75.0, ring.Value, 0.001);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryStatusPage_ProgressBarValueAllowsZeroAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NumberBox valueBox = Assert.IsAssignableFrom<NumberBox>(FindByName<NumberBox>(page, "ProgressValueNumberBox"));
                    Controls.ProgressBar progressBar = Assert.IsAssignableFrom<Controls.ProgressBar>(FindByName<Controls.ProgressBar>(page, "StandardProgressBar"));

                    Assert.Equal(0.0, progressBar.Minimum, 0.001);
                    Assert.Equal(0.0, valueBox.Minimum, 0.001);

                    valueBox.Value = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(0.0, progressBar.Value, 0.001);

                    DemoSampleControl sample = Assert.IsAssignableFrom<DemoSampleControl>(FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressBarValue", StringComparison.Ordinal)));
                    Assert.Contains("x:Name=\"ProgressValueNumberBox\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("Minimum=\"0\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, sample.XamlSource.IndexOf("Minimum=\"1\"", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryStatusPage_SourceMatchesLiveStepAndRingValuesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    DemoSampleControl stepSample = Assert.IsAssignableFrom<DemoSampleControl>(FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressBarSteps", StringComparison.Ordinal)));
                    DemoSampleControl ringSample = Assert.IsAssignableFrom<DemoSampleControl>(FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressRings", StringComparison.Ordinal)));

                    Assert.Contains("Steps=\"10\"", stepSample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("Text=\"Step 1 of 10\"", stepSample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, stepSample.XamlSource.IndexOf("Steps=\"5\"", StringComparison.Ordinal));

                    int pausedRingIndex = ringSample.XamlSource.IndexOf("x:Name=\"PausedProgressRing\"", StringComparison.Ordinal);
                    int errorRingIndex = ringSample.XamlSource.IndexOf("x:Name=\"ErrorProgressRing\"", StringComparison.Ordinal);
                    Assert.True(pausedRingIndex >= 0, "ProgressRing source should include PausedProgressRing.");
                    Assert.True(errorRingIndex > pausedRingIndex, "ProgressRing source should place ErrorProgressRing after PausedProgressRing.");
                    string pausedRingSource = ringSample.XamlSource[pausedRingIndex..errorRingIndex];

                    Assert.Contains("IsIndeterminate=\"False\"", pausedRingSource, StringComparison.Ordinal);
                    Assert.Contains("ProgressState=\"{x:Static fluence:ProgressRingState.Paused}\"", pausedRingSource, StringComparison.Ordinal);
                    Assert.Contains("Value=\"80\"", pausedRingSource, StringComparison.Ordinal);
                    Assert.Contains("Value=\"80\"", ringSample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, ringSample.XamlSource.IndexOf("Value=\"70\"", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryStatusPage_StepProgressBarAnimatesEdgeClicksAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Controls.ProgressBar progressBar = Assert.IsAssignableFrom<Controls.ProgressBar>(FindByName<Controls.ProgressBar>(page, "StepProgressBar"));

                    System.Windows.Controls.Border track = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(progressBar, "PART_Track"));
                    System.Windows.Controls.Border fill = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(progressBar, "PART_Fill"));

                    Controls.Button backButton = Assert.IsAssignableFrom<Controls.Button>(FindStepButton(page, "Back"));
                    Controls.Button nextButton = Assert.IsAssignableFrom<Controls.Button>(FindStepButton(page, "Next"));

                    backButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, backButton));
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 340).ConfigureAwait(true);

                    await AssertStepClickStartsAwayFromTargetAsync(nextButton, progressBar, fill, track, window.Dispatcher, 1, forward: true).ConfigureAwait(true);
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 340).ConfigureAwait(true);
                    await AssertStepClickStartsAwayFromTargetAsync(nextButton, progressBar, fill, track, window.Dispatcher, 2, forward: true).ConfigureAwait(true);
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 340).ConfigureAwait(true);

                    progressBar.CurrentStep = 9;
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 340).ConfigureAwait(true);
                    await AssertStepClickStartsAwayFromTargetAsync(nextButton, progressBar, fill, track, window.Dispatcher, 10, forward: true).ConfigureAwait(true);
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 340).ConfigureAwait(true);
                    await AssertStepClickStartsAwayFromTargetAsync(backButton, progressBar, fill, track, window.Dispatcher, 9, forward: false).ConfigureAwait(true);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryNavigationPage_CompactSourceMatchesLiveInteractionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryNavigationPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    DemoSampleControl sample = Assert.IsAssignableFrom<DemoSampleControl>(FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("CompactNavigationView", StringComparison.Ordinal)));

                    Assert.Contains("IsBackEnabled=\"{Binding IsChecked, ElementName=BackEnabledToggle}\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("IsPaneToggleButtonVisible=\"True\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, sample.XamlSource.IndexOf("CompactPaneToggleButton", StringComparison.Ordinal));
                    Assert.Equal(-1, sample.CSharpSource.IndexOf("CompactPaneToggleButton_Click", StringComparison.Ordinal));
                    Assert.Contains("<fluence:NavigationViewItem", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("Content=\"Settings\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, sample.XamlSource.IndexOf("IsBackEnabled=\"False\"", StringComparison.Ordinal));
                    Assert.Equal(-1, sample.XamlSource.IndexOf("Footer content", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryTabsPage_TabViewContentUsesLayerFillSurfaceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryTabsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    TabView tabView = Assert.IsAssignableFrom<TabView>(FindByName<TabView>(page, "DemoTabView"));

                    foreach (TabViewItem item in tabView.Items.OfType<TabViewItem>())
                    {
                        AssertTabViewItemContentSurface(item);
                    }

                    ButtonBase addButton = Assert.IsAssignableFrom<ButtonBase>(tabView.Template.FindName("PART_AddTabButton", tabView));
                    addButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, addButton));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(4, tabView.Items.Count);
                    TabViewItem selectedTab = Assert.IsType<TabViewItem>(tabView.SelectedItem);
                    AssertTabViewItemContentSurface(selectedTab);

                    DemoSampleControl sample = Assert.IsAssignableFrom<DemoSampleControl>(FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("TabViewDocuments", StringComparison.Ordinal)));
                    Assert.Contains("LayerFillColorDefaultBrush", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("LayerFillColorDefaultBrush", sample.CSharpSource, StringComparison.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryTypographyPage_TableUsesCompactRowSpacingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryTypographyPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid table = Assert.IsAssignableFrom<Grid>(FindByName<Grid>(page, "TypographyTable"));

                    System.Windows.Controls.TextBlock firstBodyCell = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(table.Children
                        .OfType<System.Windows.Controls.TextBlock>().FirstOrDefault(static textBlock => Grid.GetRow(textBlock) is 1 && Grid.GetColumn(textBlock) is 0));
                    Assert.Equal(new Thickness(24, 8, 16, 8), firstBodyCell.Margin);

                    System.Windows.Controls.Border firstShadedRow = Assert.IsAssignableFrom<System.Windows.Controls.Border>(table.Children
                        .OfType<System.Windows.Controls.Border>().FirstOrDefault(static border => Grid.GetRow(border) is 1));
                    Assert.Equal(new Thickness(0, 2, 0, 2), firstShadedRow.Margin);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryTypographyPage_DirectTableKeepsCopyColumnWithoutSourceExpanderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryTypographyPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    List<DemoSampleControl> samples = [.. FindAllVisualChildren<DemoSampleControl>(page)];
                    Assert.Empty(samples);

                    Grid table = Assert.IsAssignableFrom<Grid>(FindByName<Grid>(page, "TypographyTable"));

                    List<Controls.Button> copyButtons = [.. FindAllVisualChildren<Controls.Button>(table)];
                    Assert.NotEmpty(copyButtons);
                    Assert.True(copyButtons.Exists(static button => "BodyTextBlockStyle".Equals(button.Tag as string, StringComparison.Ordinal)),
                        "Typography table should keep per-row style-key copy actions.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_UsesFullWidthSettingsRowsForWindowControlsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    System.Windows.Controls.Border appThemeCard = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(page, "AppThemeSettingsCard"));
                    System.Windows.Controls.Border backdropCard = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(page, "BackdropSettingsCard"));
                    System.Windows.Controls.Border colorsCard = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(page, "ColorsSettingsCard"));
                    System.Windows.Controls.ComboBox backdrop = Assert.IsAssignableFrom<System.Windows.Controls.ComboBox>(FindByName<System.Windows.Controls.ComboBox>(page, "BackdropComboBox"));
                    UniformGrid accentRow = Assert.IsAssignableFrom<UniformGrid>(FindByName<UniformGrid>(page, "AccentSwatchRow"));
                    System.Windows.Controls.ComboBox minimize = Assert.IsAssignableFrom<System.Windows.Controls.ComboBox>(FindByName<System.Windows.Controls.ComboBox>(page, "MinimizeVisibilityCombo"));
                    System.Windows.Controls.ComboBox maximize = Assert.IsAssignableFrom<System.Windows.Controls.ComboBox>(FindByName<System.Windows.Controls.ComboBox>(page, "MaximizeVisibilityCombo"));
                    System.Windows.Controls.ComboBox close = Assert.IsAssignableFrom<System.Windows.Controls.ComboBox>(FindByName<System.Windows.Controls.ComboBox>(page, "CloseVisibilityCombo"));
                    FrameworkElement showIcon = Assert.IsAssignableFrom<FrameworkElement>(FindByName<FrameworkElement>(page, "ShowWindowIconToggle"));
                    FrameworkElement showTitle = Assert.IsAssignableFrom<FrameworkElement>(FindByName<FrameworkElement>(page, "ShowWindowTitleToggle"));

                    Assert.True(appThemeCard.ActualWidth > 700.0, "Settings cards should stretch across the content column.");
                    Assert.Equal(appThemeCard.ActualWidth, backdropCard.ActualWidth, 1.0);
                    Assert.Equal(backdropCard.ActualWidth, colorsCard.ActualWidth, 1.0);
                    Assert.Equal(7, accentRow.Children.Count);
                    Assert.Equal(GetVisualY((FrameworkElement)accentRow.Children[0], window), GetVisualY((FrameworkElement)accentRow.Children[6], window), 1.0);
                    Assert.True(GetVisualX(backdrop, window) > GetVisualX(appThemeCard, window) + 500.0,
                        "The Backdrop combo box should stay docked to the right side of its settings card.");
                    Assert.True(GetVisualY(maximize, window) > GetVisualY(minimize, window),
                        "Caption button customization should use separate settings rows.");
                    Assert.True(GetVisualY(close, window) > GetVisualY(maximize, window),
                        "Close button customization should appear below Maximize.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_CompactsControlsAtNarrowWidthsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    window.Width = 560;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.ComboBox appTheme = Assert.IsAssignableFrom<System.Windows.Controls.ComboBox>(FindByName<System.Windows.Controls.ComboBox>(page, "AppThemeComboBox"));
                    System.Windows.Controls.ComboBox minimize = Assert.IsAssignableFrom<System.Windows.Controls.ComboBox>(FindByName<System.Windows.Controls.ComboBox>(page, "MinimizeVisibilityCombo"));
                    System.Windows.Controls.StackPanel accentPanel = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindByName<System.Windows.Controls.StackPanel>(page, "AccentPickerPanel"));
                    UniformGrid accentRow = Assert.IsAssignableFrom<UniformGrid>(FindByName<UniformGrid>(page, "AccentSwatchRow"));
                    FrameworkElement systemAccent = Assert.IsAssignableFrom<FrameworkElement>(FindByName<FrameworkElement>(page, "SystemAccentButton"));
                    System.Windows.Controls.StackPanel repositoryActions = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindByName<System.Windows.Controls.StackPanel>(page, "RepositoryActionsPanel"));
                    FrameworkElement copyRepository = Assert.IsAssignableFrom<FrameworkElement>(FindByName<FrameworkElement>(page, "CopyRepositoryButton"));


                    Assert.Equal(180.0, appTheme.Width, 0.001);
                    Assert.Equal(140.0, minimize.Width, 0.001);
                    Assert.Equal(Orientation.Vertical, accentPanel.Orientation);
                    Assert.Equal(4, accentRow.Columns);
                    Assert.Equal(2, accentRow.Rows);
                    Assert.Equal(new Thickness(0, 0, 0, 8), accentRow.Margin);
                    Assert.Equal(112.0, systemAccent.MinWidth, 0.001);
                    Assert.Equal(Orientation.Vertical, repositoryActions.Orientation);
                    Assert.Equal(new Thickness(0, 0, 0, 8), copyRepository.Margin);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_RainbowAccentSwatches_PreserveLogoColorsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    UniformGrid accentRow = Assert.IsAssignableFrom<UniformGrid>(FindByName<UniformGrid>(page, "AccentSwatchRow"));

                    string[] expected =
                    [
                        "#E80000",
                        "#F58809",
                        "#F5E70C",
                        "#2BDE11",
                        "#09C4DE",
                        "#AA04DE",
                        "#FF00E8",
                    ];

                    Assert.Equal(expected.Length, accentRow.Children.Count);

                    for (int i = 0; i < expected.Length; i++)
                    {
                        FrameworkElement swatch = Assert.IsAssignableFrom<FrameworkElement>(accentRow.Children[i]);
                        Assert.Equal(expected[i], swatch.Tag as string, StringComparer.Ordinal);

                        object converted = ColorConverter.ConvertFromString(expected[i]);
                        _ = Assert.IsAssignableFrom<Color>(converted);
                    }
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    ApplicationAccentColorManager.ApplyApplicationAccent();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_InvalidAccentSwatchTag_DoesNotChangeAccentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    UniformGrid accentRow = Assert.IsAssignableFrom<UniformGrid>(FindByName<UniformGrid>(page, "AccentSwatchRow"));

                    Controls.Button swatch = Assert.IsType<Controls.Button>(accentRow.Children[0]);

                    Color originalAccent = Color.FromRgb(0x22, 0x44, 0x66);
                    ApplicationAccentColorManager.ApplyCustomAccent(originalAccent);

                    swatch.Tag = "#NotAColor";
                    swatch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, swatch));

                    Assert.Equal(originalAccent, ApplicationAccentColorManager.SystemAccentColor);
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    ApplicationAccentColorManager.ApplyApplicationAccent();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryAccessibilityPage_KeyboardSamplesUseAlignedRowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryAccessibilityPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid primary = Assert.IsAssignableFrom<Grid>(FindByName<Grid>(page, "KeyboardSupportPrimaryControls"));
                    Assert.Equal(4, primary.ColumnDefinitions.Count);
                    Assert.Equal(2, primary.RowDefinitions.Count);
                    Assert.Equal(8, primary.Children.Count);

                    AssertGridCell(primary, static child => child is Controls.Button button && string.Equals(button.Content as string, "Button 1", StringComparison.Ordinal), 0, 0, "Button 1");
                    AssertGridCell(primary, static child => child is Controls.Button button && string.Equals(button.Content as string, "Button 2", StringComparison.Ordinal), 0, 1, "Button 2");
                    AssertGridCell(primary, static child => child is Controls.TextBox, 0, 2, "TextBox");
                    AssertGridCell(primary, static child => child is Controls.ComboBox, 0, 3, "ComboBox");
                    AssertGridCell(primary, static child => child is Controls.CheckBox, 1, 0, "CheckBox");
                    AssertGridCell(primary, static child => child is ToggleSwitch, 1, 1, "ToggleSwitch");
                    AssertGridCell(primary, static child => child is Controls.Slider, 1, 2, "Slider");
                    AssertGridCell(primary, static child => child is HyperlinkButton, 1, 3, "HyperlinkButton");

                    Grid tabOrder = Assert.IsAssignableFrom<Grid>(FindByName<Grid>(page, "KeyboardSupportExplicitOrderControls"));
                    Assert.Equal(3, tabOrder.ColumnDefinitions.Count);
                    Assert.Equal(3, tabOrder.Children.Count);
                    Assert.Equal(KeyboardNavigationMode.Local, KeyboardNavigation.GetTabNavigation(tabOrder));

                    HyperlinkButton hyperlink = Assert.IsAssignableFrom<HyperlinkButton>(FindAllVisualChildren<HyperlinkButton>(primary).FirstOrDefault());
                    Controls.Button? tabOrderFirst = FindByName<Controls.Button>(page, "ExplicitTabOrderFirstButton");
                    Controls.Button? tabOrderSecond = FindByName<Controls.Button>(page, "ExplicitTabOrderSecondButton");
                    Controls.Button? tabOrderThird = FindByName<Controls.Button>(page, "ExplicitTabOrderThirdButton");
                    AssertTabOrderButton(tabOrderFirst, 1, "Tab order: 1 (first)");
                    AssertTabOrderButton(tabOrderSecond, 2, "Tab order: 2");
                    AssertTabOrderButton(tabOrderThird, 3, "Tab order: 3");
                    AssertNextFocus(window, hyperlink, tabOrderFirst, "Tab should enter the explicit tab-order group after the preceding hyperlink.");
                    AssertNextFocus(window, tabOrderFirst, tabOrderSecond, "Explicit tab-order group should move from 1 to 2.");
                    AssertNextFocus(window, tabOrderSecond, tabOrderThird, "Explicit tab-order group should move from 2 to 3.");

                    List<DemoSampleControl> samples = [.. FindAllVisualChildren<DemoSampleControl>(page)];
                    Assert.Equal(6, samples.Count);
                    Assert.True(samples.TrueForAll(static sample => !string.IsNullOrWhiteSpace(sample.XamlSource)),
                        "Every accessibility sample should have inline XAML source.");
                    Assert.Null(FindByName<FrameworkElement>(page, "FocusAndTabOrderSourceLink"));
                    Assert.Null(FindByName<FrameworkElement>(page, "HighContrastMappingSourceLink"));
                    Assert.Null(FindByName<FrameworkElement>(page, "AutomationPropertiesSourceLink"));
                    Assert.Null(FindByName<FrameworkElement>(page, "RtlLayoutSourceLink"));

                    ToggleSwitch rtlToggle = Assert.IsAssignableFrom<ToggleSwitch>(FindByName<ToggleSwitch>(page, "RtlToggle"));
                    Card rtlCard = Assert.IsAssignableFrom<Card>(FindByName<Card>(page, "RtlDemoCard"));
                    Assert.True(rtlToggle.IsChecked, "Accessibility RTL should be enabled by default.");
                    Assert.Equal(FlowDirection.RightToLeft, rtlCard.FlowDirection);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryIconsPage_IconCatalogIsScrollableAndVirtualizedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                EnsureTheme();
                GalleryIconsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Controls.ListView list = Assert.IsAssignableFrom<Controls.ListView>(FindByName<Controls.ListView>(page, "IconCatalogList"));
                    Assert.True(list.Items.Count > 100, "Icon catalog must load enough rows to exercise virtualization.");

                    System.Windows.Controls.Border catalogCard = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(page, "IconCatalogCard"));
                    Assert.Equal(new Thickness(0), catalogCard.Padding);
                    Assert.Equal(new CornerRadius(8), catalogCard.CornerRadius);
                    Assert.Equal(new Thickness(1), catalogCard.BorderThickness);
                    AssertIconBrush(catalogCard.Background, "SolidBackgroundFillColorBaseBrush");
                    AssertIconBrush(catalogCard.BorderBrush, "CardStrokeColorDefaultBrush");
                    Assert.Equal(new Thickness(0), list.BorderThickness);

                    System.Windows.Controls.Border detailsPanel = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindByName<System.Windows.Controls.Border>(page, "IconDetailsPanel"));
                    Assert.Equal(new Thickness(1, 0, 0, 0), detailsPanel.BorderThickness);
                    AssertIconBrush(detailsPanel.Background, "CardBackgroundFillColorDefaultBrush");
                    AssertIconBrush(detailsPanel.BorderBrush, "DividerStrokeColorDefaultBrush");

                    ScrollViewer viewer = Assert.IsAssignableFrom<ScrollViewer>(FindVisualChild<ScrollViewer>(list));
                    Assert.True(viewer.ViewportHeight > 0, "Icon catalog needs a bounded viewport height.");
                    Assert.True(viewer.ExtentHeight > viewer.ViewportHeight, "Icon catalog should have a scrollable extent.");
                    Assert.True(viewer.ScrollableHeight > 0, "Icon catalog should be scrollable.");

                    int realizedBeforeScroll = CountVisualChildren<ListViewItem>(list);
                    Assert.True(realizedBeforeScroll > 0, "Initial viewport should realize some row containers.");
                    Assert.True(realizedBeforeScroll < list.Items.Count / 2, "Initial layout should not realize most icon rows.");
                    Assert.Null(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1));

                    list.ScrollIntoView(list.Items[^1]);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void AssertTabViewItemContentSurface(TabViewItem item)
        {
            System.Windows.Controls.Border surface = Assert.IsType<System.Windows.Controls.Border>(item.Content);
            AssertIconBrush(surface.Background, "LayerFillColorDefaultBrush");
        }

        private static void AssertIconBrush(Brush? actualBrush, string resourceKey)
        {
            SolidColorBrush actual = Assert.IsType<SolidColorBrush>(actualBrush);

            SolidColorBrush expected = Assert.IsType<SolidColorBrush>(Application.Current?.TryFindResource(resourceKey));
            Assert.Equal(expected.Color, actual.Color);
        }

        private static void AssertSourceTab(TabControl? tabs, string expectedHeader, string expectedSource)
        {
            if (tabs is null)
            {
                return;
            }
            foreach (object item in tabs.Items)
            {
                if (item is TabItem tab && string.Equals(tab.Header as string, expectedHeader, StringComparison.Ordinal))
                {
                    System.Windows.Controls.Button copy = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(tab.Content as DependencyObject, "CopySourceButton"));
                    Assert.Equal(expectedSource, copy.Tag as string, StringComparer.Ordinal);
                    return;
                }
            }

            Assert.Fail("Missing source tab: " + expectedHeader);
        }

        private static string GetSourceTabText(TabControl tabs, string expectedHeader)
        {
            foreach (object item in tabs.Items)
            {
                if (item is TabItem tab && string.Equals(tab.Header as string, expectedHeader, StringComparison.Ordinal))
                {
                    RichTextBox viewer = Assert.IsAssignableFrom<RichTextBox>(FindByName<RichTextBox>(tab.Content as DependencyObject, "SourceTextViewer"));
                    TextRange textRange = new(viewer.Document.ContentStart, viewer.Document.ContentEnd);
                    return textRange.Text;
                }
            }

            Assert.Fail("Missing source tab: " + expectedHeader);
            return string.Empty;
        }

        private static void EnsureTheme()
        {
            Application application = WpfTestSta.EnsureApplication();
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            };
            application.Resources.MergedDictionaries.Add(demoShared);
        }

        private static MainWindow CreateShownMainWindow()
        {
            MainWindow window = new()
            {
                Left = -20000,
                Top = -20000,
                Width = 1200,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
            };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            return window;
        }

        private static Window CreateHostWindow(UIElement content)
        {
            Window window = new()
            {
                Left = -20000,
                Top = -20000,
                Width = 1040,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
                Content = content,
            };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            return window;
        }

        private static object GetSelectedPageContent(MainWindow window)
        {
            NavigationView nav = Assert.IsAssignableFrom<NavigationView>(FindByName<NavigationView>(window, "DemoNav"));

            Assert.NotNull(nav.SelectedItem as NavigationViewItem);
            return nav.Content;
        }

        private static void InvokeTitleBarBack(TitleBar titleBar)
        {
            System.Windows.Controls.Button backButton = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindByName<System.Windows.Controls.Button>(titleBar, "PART_BackButton"));
            backButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, backButton));
            WpfTestSta.DrainDispatcher(titleBar.Dispatcher);
        }

        private static void InvokeSettingsItem(NavigationViewItem settingsItem)
        {
            // Settings is a FooterMenuItems entry; drive selection through the same control path a
            // click/keyboard invocation uses (raises ItemInvoked and shows the footer indicator).
            NavigationView.FromItemContainer(settingsItem)?.SelectFooterMenuItem(settingsItem);
            WpfTestSta.DrainDispatcher(settingsItem.Dispatcher);
        }

        private static double GetVisualX(FrameworkElement element, Visual ancestor)
        {
            return element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).X;
        }

        private static double GetVisualY(FrameworkElement element, Visual ancestor)
        {
            return element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).Y;
        }

        private static double GetVisualCenterX(FrameworkElement element, Visual ancestor)
        {
            return GetVisualX(element, ancestor) + (element.ActualWidth / 2.0);
        }

        private static double GetVisualCenterY(FrameworkElement element, Visual ancestor)
        {
            return GetVisualY(element, ancestor) + (element.ActualHeight / 2.0);
        }

        private static async Task WaitForAnimationAndDrainAsync(Dispatcher dispatcher, int milliseconds)
        {
            // Awaiting resumes on the dispatcher via its synchronization context, so the
            // dispatcher keeps pumping (animations advance) while the delay elapses.
            await Task.Delay(milliseconds, TestContext.Current.CancellationToken).ConfigureAwait(true);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
        }

        private static void AssertGridCell(Grid grid, Predicate<UIElement> match, int expectedRow, int expectedColumn, string name)
        {
            foreach (UIElement child in grid.Children)
            {
                if (match(child))
                {
                    Assert.Equal(expectedRow, Grid.GetRow(child));
                    Assert.Equal(expectedColumn, Grid.GetColumn(child));
                    return;
                }
            }

            Assert.Fail("Expected control was not found in the grid: " + name);
        }

        private static void AssertTabOrderButton(Controls.Button? button, int expectedTabIndex, string expectedContent)
        {
            if (button is null)
            {
                Assert.Fail("Expected explicit tab-order button was not found: " + expectedContent);
                return;
            }

            Assert.Equal(expectedContent, button.Content as string, StringComparer.Ordinal);
            Assert.Equal(expectedTabIndex, button.TabIndex);
            Assert.True(button.Focusable, "Explicit tab-order button should accept keyboard focus.");
            Assert.True(button.IsTabStop, "Explicit tab-order button should participate in keyboard tab navigation.");
        }

        private static void AssertNextFocus(
            Window window,
            FrameworkElement? source,
            FrameworkElement? expected,
            string message)
        {
            if (source is null)
            {
                Assert.Fail("Focus source was not found. " + message);
                return;
            }

            if (expected is null)
            {
                Assert.Fail("Expected next focus target was not found. " + message);
                return;
            }

            _ = source.Focus();
            FocusManager.SetFocusedElement(window, source);
            _ = Keyboard.Focus(source);
            WpfTestSta.DrainDispatcher(window.Dispatcher);

            TraversalRequest request = new(FocusNavigationDirection.Next);
            bool moved = source.MoveFocus(request);
            WpfTestSta.DrainDispatcher(window.Dispatcher);

            Assert.True(moved, "Keyboard focus should move to the next tab stop. " + message);
            Assert.Same(expected, Keyboard.FocusedElement);
        }

        private static Controls.Button? FindStepButton(DependencyObject root, string tag)
        {
            return FindAllVisualChildren<Controls.Button>(root)
                .FirstOrDefault(button => string.Equals(button.Tag as string, tag, StringComparison.Ordinal));
        }

        private static async Task AssertStepClickStartsAwayFromTargetAsync(
            Controls.Button button,
            Controls.ProgressBar progressBar,
            FrameworkElement fill,
            FrameworkElement track,
            Dispatcher dispatcher,
            int expectedStep,
            bool forward)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
            await WaitForAnimationAndDrainAsync(dispatcher, 40).ConfigureAwait(true);

            Assert.Equal(expectedStep, progressBar.CurrentStep);
            double targetWidth = track.ActualWidth * expectedStep / progressBar.Steps;

            // The determinate fill is laid out at the full track width and animates
            // PART_FillScale.ScaleX in [0,1], so the visually rendered progress width is
            // the track width multiplied by the current (possibly animating) scale.
            ScaleTransform fillScale = Assert.IsType<ScaleTransform>(fill.RenderTransform);
            double animatedWidth = track.ActualWidth * fillScale.ScaleX;
            if (forward)
            {
                Assert.True(animatedWidth < targetWidth, string.Format(CultureInfo.InvariantCulture,
                        "Forward step animation should start before the target width. Animated={0}, Target={1}, Step={2}.",
                        animatedWidth,
                        targetWidth,
                        expectedStep));
            }
            else
            {
                Assert.True(
                    animatedWidth > targetWidth,
                    string.Format(CultureInfo.InvariantCulture,
                        "Backward step animation should start after the target width. Animated={0}, Target={1}, Step={2}.",
                        animatedWidth,
                        targetWidth,
                        expectedStep));
            }
        }

        private static T? FindByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            return root is not FrameworkElement element || element.FindName(name) is not T named
                ? FindAllVisualChildren<T>(root).FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal))
                : named;
        }

        private static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject? root)
            where T : DependencyObject
        {
            HashSet<DependencyObject> visited = [];
            foreach (T result in FindAllVisualChildren<T>(root, visited))
            {
                yield return result;
            }
        }

        private static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject? root, HashSet<DependencyObject> visited)
            where T : DependencyObject
        {
            if (root is null)
            {
                yield break;
            }

            if (visited.Contains(root))
            {
                yield break;
            }

            _ = visited.Add(root);

            if (root is T current)
            {
                yield return current;
            }

            int visualCount;
            try
            {
                visualCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch (InvalidOperationException)
            {
                visualCount = 0;
            }

            for (int i = 0; i < visualCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                foreach (T result in FindAllVisualChildren<T>(child, visited))
                {
                    yield return result;
                }
            }

            foreach (object logicalChild in LogicalTreeHelper.GetChildren(root))
            {
                if (logicalChild is not DependencyObject logical)
                {
                    continue;
                }

                foreach (T result in FindAllVisualChildren<T>(logical, visited))
                {
                    yield return result;
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject root)
            where T : DependencyObject
        {
            return FindAllVisualChildren<T>(root).FirstOrDefault();
        }

        private static int CountVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            int count = 0;
            foreach (T item in FindAllVisualChildren<T>(root))
            {
                count++;
            }

            return count;
        }

        private sealed class DemoPageExpectation(string tag, Type pageType)
        {
            public string Tag { get; } = tag;

            public Type PageType { get; } = pageType;
        }
    }
}
