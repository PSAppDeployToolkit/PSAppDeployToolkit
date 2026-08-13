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
using System.Runtime.ExceptionServices;
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

        private static void RunOnSta(Action action)
        {
            Exception? captured = null;
            WpfTestSta.Dispatcher?.Invoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
            }));

            if (captured is not null)
            {
                ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        [Fact]
        public void MainWindow_DirectNavigation_LoadsConcretePages()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        window.NavigateTo(expectation.Tag);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        object content = GetSelectedPageContent(window);
                        Assert.NotNull(content);
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
        public void MainWindow_InitialSelection_LoadsHomePageContent()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    object content = GetSelectedPageContent(window);
                    Assert.NotNull(content);
                    Assert.Equal(typeof(GalleryHomePage), content.GetType());

                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    Assert.Same(content, nav.Content);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void GalleryHomePage_HeroSwapsHeaderLockupWithTheme()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryHomePage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    System.Windows.Controls.Image? image = FindByName<System.Windows.Controls.Image>(page, "BrandHeroImage");
                    Assert.NotNull(image);

                    DrawingImage? light = Application.Current.TryFindResource("FluenceHeaderLightDrawingImage") as DrawingImage;
                    DrawingImage? dark = Application.Current.TryFindResource("FluenceHeaderDarkDrawingImage") as DrawingImage;
                    Assert.NotNull(light);
                    Assert.NotNull(dark);

                    // The hero shows the lockup drawn for the active theme and swaps on
                    // theme changes via the page's ThemeDictionary (no code-behind).
                    Assert.Same(light, image.Source);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.Same(dark, image.Source);

                    // High contrast has no fixed polarity, so the page picks whichever
                    // variant reads against the live system window color.
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.True(ReferenceEquals(image.Source, light) || ReferenceEquals(image.Source, dark),
                        "High contrast should show one of the two header lockups.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.Same(light, image.Source);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void GalleryHomePage_UsesHeaderLockupHeroAndGitHubLink()
        {
            string homePage = ReadRepositoryFile("Fluence.Wpf.Demo", "Pages", "GalleryHomePage.xaml");
            Assert.Contains("FluenceHeaderLightDrawingImage", homePage, StringComparison.Ordinal);
            Assert.Contains("https://github.com/sintaxasn/fluence.wpf", homePage, StringComparison.Ordinal);
        }

        [Fact]
        public void Library_EmbedsXamlBrandIcons_AndDemosSetBrandApplicationIcon()
        {
            // The Fluence brand icon ships as resolution-independent vector DrawingImages in
            // Fluence.Wpf\Themes\Icons\FluenceIcons.xaml (merged into Generic.xaml), replacing the
            // multi-resolution assets\Fluence.ico that previously dominated the library binary.
            // FluenceWindow rasterizes the brand vector for its default Window.Icon, so neither demo
            // sets Icon= in XAML (both inherit the embedded default at runtime). The demo executables
            // do set ApplicationIcon to the brand .ico so the .exe file icon in Explorer is the brand mark.
            string libraryProject = ReadRepositoryFile("Fluence.Wpf", "Fluence.Wpf.csproj");
            Assert.False(libraryProject.Contains("Fluence.ico", StringComparison.Ordinal),
                "The library should no longer embed assets\\Fluence.ico now that the brand icon is a XAML vector.");
            Assert.Contains("<PackageIcon>Fluence_Icon_Light_128.png</PackageIcon>", libraryProject, StringComparison.Ordinal);

            // The three brand DrawingImages live in a dedicated icon dictionary that is merged into
            // Generic.xaml so the keys resolve from application resources.
            Assert.True(File.Exists(GetRepositoryFilePath("Fluence.Wpf", "Themes", "Icons", "FluenceIcons.xaml")),
                "The brand icon dictionary should exist at Fluence.Wpf\\Themes\\Icons\\FluenceIcons.xaml.");
            string iconDictionary = ReadRepositoryFile("Fluence.Wpf", "Themes", "Icons", "FluenceIcons.xaml");
            Assert.Contains("FluenceIconBrandDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("FluenceIconLightDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("FluenceIconDarkDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("Themes/Icons/FluenceIcons.xaml", ReadRepositoryFile("Fluence.Wpf", "Themes", "Generic.xaml"), StringComparison.Ordinal);

            // Both demo executables set their ApplicationIcon to the Fluence brand .ico so the .exe
            // shows the brand mark in Explorer and on a pre-launch taskbar pin.
            string galleryProject = ReadRepositoryFile("Fluence.Wpf.Demo", "Fluence.Wpf.Demo.csproj");
            Assert.Contains("<ApplicationIcon>", galleryProject, StringComparison.Ordinal);
            Assert.Contains("Fluence_Icon_Light.ico", galleryProject, StringComparison.Ordinal);
            string mvvmProject = ReadRepositoryFile("Fluence.Wpf.Demo.Mvvm", "Fluence.Wpf.Demo.Mvvm.csproj");
            Assert.Contains("<ApplicationIcon>", mvvmProject, StringComparison.Ordinal);
            Assert.Contains("Fluence_Icon_Light.ico", mvvmProject, StringComparison.Ordinal);

            Assert.False(ReadRepositoryFile("Fluence.Wpf.Demo", "MainWindow.xaml").Contains("Icon=\"", StringComparison.Ordinal),
                "The gallery demo window should inherit the embedded FluenceWindow icon, not set Icon= itself.");
            Assert.False(ReadRepositoryFile("Fluence.Wpf.Demo.Mvvm", "MainWindow.xaml").Contains("Icon=\"", StringComparison.Ordinal),
                "The MVVM demo window should inherit the embedded FluenceWindow icon, not set Icon= itself.");

            // The retired .ico is gone from the tree.
            Assert.False(File.Exists(GetRepositoryFilePath("assets", "Fluence.ico")),
                "assets\\Fluence.ico should be deleted once the XAML vector icons replace it.");
        }

        [Fact]
        public void MainWindow_Search_NavigatesToGroupedConcretePage()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(search);

                    search.Text = "progress ring";
                    search.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Enter)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });
                    Drain(window.Dispatcher);
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
        public void MainWindow_BackRequested_WalksVisitedPagesInOrder()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);

                    window.NavigateTo("buttons");
                    Drain(window.Dispatcher);
                    window.NavigateTo("trees");
                    Drain(window.Dispatcher);
                    window.NavigateTo("status");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(typeof(GalleryStatusPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryTreesPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryButtonsPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryHomePage), GetSelectedPageContent(window).GetType());

                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
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
        public void GalleryPages_UseSharedWinUiGalleryPageLayout()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                Style? scrollStyle = Application.Current?.TryFindResource("GalleryPageScrollViewerStyle") as Style;
                Style? fluentScrollStyle = Application.Current?.TryFindResource("ScrollViewerStyle") as Style;
                Style? contentStyle = Application.Current?.TryFindResource("GalleryPageContentStackStyle") as Style;
                Style? contentGridStyle = Application.Current?.TryFindResource("GalleryPageContentGridStyle") as Style;
                Assert.NotNull(scrollStyle);
                Assert.NotNull(fluentScrollStyle);
                Assert.Same(fluentScrollStyle, scrollStyle.BasedOn);
                Assert.NotNull(contentStyle);
                Assert.NotNull(contentGridStyle);

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
                            Grid? pageRoot = FindByName<Grid>(page, "PageRoot");
                            Assert.NotNull(pageRoot);
                            Assert.Null(pageRoot.Background);

                            Grid? pageContent = FindByName<Grid>(page, "PageContent");
                            Assert.NotNull(pageContent);
                            Assert.Same(contentGridStyle, pageContent.Style);
                            Assert.Equal(new Thickness(36, 24, 36, 48), pageContent.Margin);
                            Assert.True(double.IsPositiveInfinity(pageContent.MaxWidth),
                                "Icons should stretch instead of keeping the old max content width.");
                            Assert.Equal(HorizontalAlignment.Stretch, pageContent.HorizontalAlignment);
                            continue;
                        }

                        SmoothScrollViewer? scrollViewer = FindVisualChild<SmoothScrollViewer>(page);
                        Assert.NotNull(scrollViewer);
                        Assert.Same(scrollStyle, scrollViewer.Style);

                        System.Windows.Controls.StackPanel? content = scrollViewer.Content as System.Windows.Controls.StackPanel;
                        Assert.NotNull(content);
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
        public void MainWindow_TitleBarSearch_StaysVisibleWhenContentExtendsIntoTitleBar()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(search);
                    Assert.Equal(Visibility.Visible, search.Visibility);

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
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
        public void MainWindow_TitleBarSearch_IsCenteredInWindow()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(shellTitleBar);
                    Assert.NotNull(search);
                    Assert.Equal(300.0, search.Width, 0.01);
                    Assert.Equal(300.0, search.MinWidth, 0.01);
                    Assert.Equal(475.0, search.MaxWidth, 0.01);
                    Assert.Equal(300.0, search.ActualWidth, 0.5);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window) ?? double.MaxValue, 1.0);
                    Assert.Equal((GetVisualCenterY(shellTitleBar, window) ?? double.MinValue) + 4.0, GetVisualCenterY(search, window) ?? double.MaxValue, 1.0);

                    Assert.True(search.Focus(), "Search should accept keyboard focus.");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(300.0, search.ActualWidth, 0.5);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window) ?? double.MaxValue, 1.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_ExtendedTitleBar_UsesHorizontalNavigationChrome()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    window.NavigateTo("buttons");
                    Drain(window.Dispatcher);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);

                    System.Windows.Controls.Button? titleBarToggle = FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.NotNull(titleBarToggle);
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);
                    Assert.Equal(40.0, titleBarToggle.ActualWidth, 0.5);

                    System.Windows.Controls.TextBlock? titleBarGlyph = FindVisualChild<System.Windows.Controls.TextBlock>(titleBarToggle);
                    Assert.NotNull(titleBarGlyph);
                    Assert.Equal(16.0, titleBarGlyph.FontSize, 0.01);

                    System.Windows.Controls.Button? titleBarBack = FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_BackButton");
                    Assert.NotNull(titleBarBack);
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    Assert.True((GetVisualX(titleBarBack, window) ?? double.MaxValue) < (GetVisualX(titleBarToggle, window) ?? double.MaxValue), "Back should occupy the first title-bar navigation slot.");
                    System.Windows.Controls.TextBlock? titleBarBackGlyph = FindVisualChild<System.Windows.Controls.TextBlock>(titleBarBack);
                    Assert.NotNull(titleBarBackGlyph);

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.NotNull(firstItem);
                    FontIcon? itemGlyph = FindVisualChild<FontIcon>(firstItem);
                    Assert.NotNull(itemGlyph);
                    Assert.Equal(GetVisualCenterX(itemGlyph, window) ?? double.MaxValue, GetVisualCenterX(titleBarBackGlyph, window) ?? double.MaxValue, 2.5);

                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Assert.NotNull(titleIcon);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    System.Windows.Controls.Image? titleIconImage = FindVisualChild<System.Windows.Controls.Image>(titleIcon);
                    Assert.NotNull(titleIconImage);
                    Assert.Equal(20.0, titleIconImage.ActualWidth, 0.5);
                    Assert.Equal(20.0, titleIconImage.ActualHeight, 0.5);
                    Assert.True(GetVisualX(titleIcon, window) >= GetVisualX(titleBarToggle, window) + titleBarToggle.ActualWidth - 0.5,
                        "Title identity should start after the title-bar navigation slot.");

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button? internalToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    Assert.NotNull(internalToggle);
                    Assert.Equal(Visibility.Collapsed, internalToggle.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_ExtendedTitleBar_FirstGlyphTracksBackAvailability()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    nav.IsBackButtonVisible = true;
                    nav.IsBackEnabled = true;

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);
                    System.Windows.Controls.Button? titleBarBack = FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_BackButton");
                    System.Windows.Controls.Button? titleBarToggle = FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.NotNull(titleBarBack);
                    Assert.NotNull(titleBarToggle);
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);
                    Assert.True((GetVisualX(titleBarBack, window) ?? double.MaxValue) < (GetVisualX(titleBarToggle, window) ?? double.MaxValue), "Back should occupy the first title-bar navigation slot.");
                    Assert.Equal(GetVisualCenterY(titleBarBack, window) ?? double.MaxValue, GetVisualCenterY(titleBarToggle, window) ?? double.MaxValue, 1.0);

                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Assert.NotNull(titleIcon);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    double? titleIconWithBackX = GetVisualX(titleIcon, window);

                    nav.IsBackEnabled = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, titleBarBack.Visibility);
                    Assert.Equal((titleIconWithBackX ?? double.MaxValue) - 42.0, GetVisualX(titleIcon, window) ?? double.MaxValue, 1.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_ExtendedTitleBar_KeepsNavigationItemsBelowTitleBar()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(42.0, window.TitleBarHeight, 0.01);

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.NotNull(firstItem);
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
        public void MainWindow_TopPane_UsesNonExtendedTitleBarWithoutPaneToggleChrome()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    window.ExtendsContentIntoTitleBar = false;
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    nav.IsPaneOpen = false;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    nav.IsBackEnabled = true;
                    nav.IsBackButtonVisible = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.False(window.ExtendsContentIntoTitleBar,
                        "Top NavigationView mode should keep the FluenceWindow title bar non-extended.");
                    Assert.True(nav.IsPaneOpen, "Top NavigationView mode should coerce IsPaneOpen=True.");
                    Assert.False(nav.IsPaneToggleButtonVisible,
                        "Top NavigationView mode should coerce the pane toggle hidden.");

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);
                    System.Windows.Controls.Button? titleBarToggle = FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.NotNull(titleBarToggle);
                    Assert.Equal(Visibility.Collapsed, titleBarToggle.Visibility);
                    System.Windows.Controls.Button? titleBarBack = FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_BackButton");
                    Assert.NotNull(titleBarBack);
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    System.Windows.Controls.TextBlock? titleBarBackGlyph = FindVisualChild<System.Windows.Controls.TextBlock>(titleBarBack);
                    Assert.NotNull(titleBarBackGlyph);
                    Assert.Equal(16.0, titleBarBackGlyph.FontSize, 0.01);
                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(titleIcon);
                    Assert.NotNull(search);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    Assert.True((GetVisualX(titleBarBack, window) ?? double.MaxValue) < (GetVisualX(titleIcon, window) ?? double.MaxValue), "Top mode back should be the first visible title-bar item.");
                    Assert.True((GetVisualX(titleBarBack, window) ?? double.MaxValue) < (GetVisualX(search, window) ?? double.MaxValue), "Top mode back should appear before centered title-bar content.");

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button? internalBack = nav.Template.FindName(NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    System.Windows.Controls.Button? internalToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    Assert.NotNull(internalBack);
                    Assert.Equal(Visibility.Collapsed, internalBack.Visibility);
                    Assert.Null(internalToggle);

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.NotNull(firstItem);
                    Assert.Equal(Visibility.Visible, firstItem.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_SettingsFooter_NavigatesToSelectableSettingsPage()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.NotNull(nav);
                    Assert.NotNull(settings);
                    Assert.Null(FindByName<FrameworkElement>(window, "PaneModeToggle"));

                    InvokeSettingsItem(settings);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

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
        public void MainWindow_SettingsFooter_CollapsesLabelWhenPaneClosed()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.NotNull(nav);
                    Assert.NotNull(settings);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    // As a FooterMenuItems entry, Settings uses the standard NavigationViewItem template:
                    // the label is collapsed/shown by the template (it is not emptied), exactly like the
                    // main menu items. Content stays "Settings" throughout.
                    Assert.Equal("Settings", settings.Content as string, StringComparer.Ordinal);
                    ContentPresenter? label = FindByName<ContentPresenter>(settings, "ContentPresenter");
                    Assert.NotNull(label);
                    Assert.Equal(Visibility.Visible, label.Visibility);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    label = FindByName<ContentPresenter>(settings, "ContentPresenter");
                    Assert.NotNull(label);
                    Assert.Equal(Visibility.Collapsed, label.Visibility);
                    Assert.Equal(Visibility.Visible, settings.Visibility);
                    FontIcon? settingsIcon = settings.Icon as FontIcon;
                    Assert.NotNull(settingsIcon);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_SettingsFooter_DoesNotForceTopPaneModeWhenOpened()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.NotNull(nav);
                    Assert.NotNull(settings);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    InvokeSettingsItem(settings);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Opening Settings must preserve the real collapsed pane state.");

                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        nav.Content as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.NotNull(navigationStyle);
                    Assert.Equal(2, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void GallerySettingsPage_NavigationStyleCombo_TracksExternalIsPaneOpenChanges()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);

                    window.NavigateTo("settings");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        nav.Content as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.NotNull(navigationStyle);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneOpen = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(2, navigationStyle.SelectedIndex);

                    nav.IsPaneOpen = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(1, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_TopPane_OverflowButtonDoesNotOverlapTreesAtMinimumWidth()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    window.Width = 698;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    FrameworkElement? overflowButton = FindByName<FrameworkElement>(nav, NavigationView.PartTopOverflowButton);
                    Assert.NotNull(overflowButton);
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    int visibleNavigationItems = nav.Items.OfType<NavigationViewItem>().Count(static item => item.Visibility is Visibility.Visible);
                    Assert.True(visibleNavigationItems > 1,
                        "Top pane should show every navigation item that fits before the overflow button would overlap the Top toggle status.");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.NotNull(settings);
                    double overflowRight = (GetVisualX(overflowButton, nav) ?? double.MaxValue) + overflowButton.ActualWidth;
                    double settingsLeft = GetVisualX(settings, nav) ?? double.MinValue;
                    Assert.True(overflowRight <= settingsLeft - 4.0 + 1.5, "The three-dot overflow entry should stop before it overlaps the Settings item.");

                    NavigationViewItem? trees = null;
                    foreach (object item in nav.Items)
                    {
                        if (item is NavigationViewItem navItem
                            && string.Equals(navItem.Content as string, "Trees", StringComparison.Ordinal))
                        {
                            trees = navItem;
                            break;
                        }
                    }

                    Assert.NotNull(trees);
                    if (trees.Visibility is Visibility.Visible)
                    {
                        double treesRight = (GetVisualX(trees, nav) ?? double.MinValue) + trees.ActualWidth;
                        double overflowLeft = GetVisualX(overflowButton, nav) ?? double.MaxValue;
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
        public void GallerySettingsPage_NavigationStyleCombo_SwitchesPaneModeAndKeepsContentLive()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);

                    window.NavigateTo("settings");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    object settingsPage = nav.Content
                        ?? throw new InvalidOperationException("Settings navigation should create a live Settings page.");
                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        settingsPage as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.NotNull(navigationStyle);

                    navigationStyle.SelectedIndex = 1;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.True(nav.IsPaneOpen,
                        "Choosing Left in Settings should open the left pane instead of preserving a compact state.");
                    Assert.Same(settingsPage, nav.Content);

                    navigationStyle.SelectedIndex = 2;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Choosing Left compact in Settings should close the pane.");

                    navigationStyle.SelectedIndex = 0;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.Top, nav.PaneDisplayMode);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void GallerySettingsPage_NavigationStyleCombo_FollowsShellPaneToggle()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);

                    window.NavigateTo("settings");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    object settingsPage = nav.Content
                        ?? throw new InvalidOperationException("Settings navigation should create a live Settings page.");
                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        settingsPage as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.NotNull(navigationStyle);

                    navigationStyle.SelectedIndex = 1;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.True(nav.IsPaneOpen, "Left navigation should be expanded before the pane toggle is clicked.");
                    Assert.Equal(1, navigationStyle.SelectedIndex);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);
                    System.Windows.Controls.Button? titleBarToggle = FindByName<System.Windows.Controls.Button>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.NotNull(titleBarToggle);
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.True(nav.GetPaneColumnWidthForTesting() > 48.0,
                        "Collapsing Left navigation should start the sidebar width animation instead of snapping to compact width.");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Clicking the shell pane toggle should collapse the Left pane.");
                    Assert.Equal(2, navigationStyle.SelectedIndex);

                    WaitForAnimationAndDrain(window.Dispatcher, 220);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.True(nav.GetPaneColumnWidthForTesting() < 280.0, "Expanding Left navigation should start from the current compact width instead of snapping open.");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

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
        public void GalleryNavigationPage_CompactSamplePaneToggleOpensPane()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryNavigationPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(page, "CompactNavigationDemo");
                    Assert.NotNull(nav);
                    Assert.False(nav.IsPaneOpen, "Compact sample should start collapsed.");

                    System.Windows.Controls.Button? paneToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    Assert.NotNull(paneToggle);

                    Controls.Button? sampleToggle = FindByName<Controls.Button>(page, "CompactPaneToggleButton");
                    Assert.Null(sampleToggle);

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.True(nav.IsPaneOpen,
                        "Clicking the built-in compact pane toggle should open the sample pane.");

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

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
        public void MainWindow_ExtendedTitleBar_TrimsTitleToSearchClearance()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 1200;
                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Trim Before Search");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);
                    System.Windows.Controls.TextBlock? titleText = FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(titleText);
                    Assert.NotNull(search);
                    Assert.Equal(Visibility.Visible, titleText.Visibility);
                    double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                    double searchLeft = GetVisualX(search, window) ?? double.MaxValue;
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
        public void MainWindow_ExtendedTitleBar_HidesTitleTextWhenItOverlapsSearch()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 760;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);
                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    System.Windows.Controls.TextBlock? titleText = FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.NotNull(titleIcon);
                    Assert.NotNull(titleText);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                        Assert.NotNull(search);
                        double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window) ?? double.MaxValue;
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
        public void MainWindow_ExtendedTitleBar_DoesNotLetTitleOverlapSearchAtMinimumWidth()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 698;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Must Never Overlap Search");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(shellTitleBar);
                    Assert.NotNull(search);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window) ?? double.MaxValue, 1.0);

                    System.Windows.Controls.TextBlock? titleText = FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.NotNull(titleText);
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window) ?? double.MaxValue;
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
        public void MainWindow_ExtendedTitleBar_RestoresTitleTextWhenSearchHasRoom()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.NotNull(nav);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 760;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.NotNull(shellTitleBar);
                    System.Windows.Controls.TextBlock? titleText = FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.NotNull(titleText);
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        Controls.TextBox? setupSearch = FindByName<Controls.TextBox>(window, "NavSearchBox");
                        Assert.NotNull(setupSearch);
                        double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(setupSearch, window) ?? double.MaxValue;
                        Assert.True(titleRight <= searchLeft - 12.0, "Setup should hide or trim title text before it crosses the 12px search clearance.");
                    }

                    window.Width = 1200;
                    window.SetUserShowTitle(show: true, "Fluence.Wpf");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    titleText = FindByName<System.Windows.Controls.TextBlock>(shellTitleBar, "PART_TitleText");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(search);
                    Assert.Equal(Visibility.Visible, titleText?.Visibility);
                    Assert.Equal("Fluence.Wpf", titleText?.Text, StringComparer.Ordinal);
                    Assert.True(GetVisualX(titleText, window) + titleText?.ActualWidth + 12.0 <= GetVisualX(search, window),
                        "Visible title text should keep the search clearance gap.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_TitleBarSearch_DoesNotShiftWhenChromeOptionsChange()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.NotNull(search);

                    double? initialX = GetVisualX(search, window);

                    window.SetUserShowIcon(show: false, window.Icon);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0);

                    window.SetUserShowTitle(show: false, window.Title);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0);

                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    window.IsMaximizeButtonVisible = Visibility.Collapsed;
                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void DemoSampleControl_ExpanderUsesInMemorySourceTabs()
        {
            RunOnSta(static delegate
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
                    Controls.Expander? expander = FindByName<Controls.Expander>(sample, "SourceExpander");
                    Assert.NotNull(expander);
                    Assert.False(expander.IsExpanded, "Source starts collapsed.");

                    expander.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl? tabs = FindByName<TabControl>(sample, "SourceTabControl");
                    Assert.NotNull(tabs);
                    Assert.Equal(2, tabs.Items.Count);
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                    AssertSourceTab(tabs, "C#", sample.CSharpSource);

                    System.Windows.Controls.Border? sampleCard = FindByName<System.Windows.Controls.Border>(sample, "SampleCard");
                    Assert.NotNull(sampleCard);
                    Assert.Equal(new CornerRadius(8, 8, 0, 0), sampleCard.CornerRadius);
                    Assert.Equal(new CornerRadius(0, 0, 8, 8), expander.CornerRadius);
                    Assert.Equal(new Thickness(1, 0, 1, 1), expander.BorderThickness);
                    Assert.Equal((GetVisualY(sampleCard, window) ?? double.MinValue) + sampleCard.ActualHeight, GetVisualY(expander, window) ?? double.MinValue, 0.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void DemoSampleControl_SourceRendererPreservesIndentation()
        {
            RunOnSta(static delegate
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
                    Controls.Expander? expander = FindByName<Controls.Expander>(sample, "SourceExpander");
                    Assert.NotNull(expander);
                    expander.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl? tabs = FindByName<TabControl>(sample, "SourceTabControl");
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
        public void DemoSampleControl_EmptyCSharpSourceAddsOnlyXamlTab()
        {
            RunOnSta(static delegate
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
                    Drain(window.Dispatcher);
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
        public void MainWindow_NonHomePagesExposeInlineSourceSamples()
        {
            RunOnSta(static delegate
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
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        object content = GetSelectedPageContent(window);
                        DependencyObject? root = content as DependencyObject;
                        Assert.NotNull(root);

                        bool found = false;
                        foreach (DemoSampleControl sample in FindAllVisualChildren<DemoSampleControl>(root))
                        {
                            if (!string.IsNullOrWhiteSpace(sample.XamlSource))
                            {
                                found = true;
                                break;
                            }
                        }

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
        public void GalleryStatusPage_DeterminateProgressRingUsesNumberBoxBinding()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NumberBox? valueBox = FindByName<NumberBox>(page, "ProgressRingValueBox");
                    ProgressRing? ring = FindByName<ProgressRing>(page, "DeterminateProgressRing");
                    Assert.NotNull(valueBox);
                    Assert.NotNull(ring);

                    Assert.Equal(1.0, valueBox.Minimum, 0.001);
                    Assert.Equal(100.0, valueBox.Maximum, 0.001);
                    Assert.Equal(50.0, valueBox.Value, 0.001);
                    Assert.Equal(50.0, ring.Value, 0.001);

                    valueBox.Value = 75;
                    Drain(window.Dispatcher);
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
        public void GalleryStatusPage_ProgressBarValueAllowsZero()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NumberBox? valueBox = FindByName<NumberBox>(page, "ProgressValueNumberBox");
                    Controls.ProgressBar? progressBar = FindByName<Controls.ProgressBar>(page, "StandardProgressBar");
                    Assert.NotNull(valueBox);
                    Assert.NotNull(progressBar);

                    Assert.Equal(0.0, progressBar.Minimum, 0.001);
                    Assert.Equal(0.0, valueBox.Minimum, 0.001);

                    valueBox.Value = 0;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(0.0, progressBar.Value, 0.001);

                    DemoSampleControl? sample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressBarValue", StringComparison.Ordinal));
                    Assert.NotNull(sample);
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
        public void GalleryStatusPage_SourceMatchesLiveStepAndRingValues()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    DemoSampleControl? stepSample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressBarSteps", StringComparison.Ordinal));
                    DemoSampleControl? ringSample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("ProgressRings", StringComparison.Ordinal));
                    Assert.NotNull(stepSample);
                    Assert.NotNull(ringSample);

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
        public void GalleryStatusPage_StepProgressBarAnimatesEdgeClicks()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Controls.ProgressBar? progressBar = FindByName<Controls.ProgressBar>(page, "StepProgressBar");
                    Assert.NotNull(progressBar);

                    System.Windows.Controls.Border? track = FindByName<System.Windows.Controls.Border>(progressBar, "PART_Track");
                    System.Windows.Controls.Border? fill = FindByName<System.Windows.Controls.Border>(progressBar, "PART_Fill");
                    Assert.NotNull(track);
                    Assert.NotNull(fill);

                    Controls.Button? backButton = FindStepButton(page, "Back");
                    Controls.Button? nextButton = FindStepButton(page, "Next");
                    Assert.NotNull(backButton);
                    Assert.NotNull(nextButton);

                    backButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, backButton));
                    WaitForAnimationAndDrain(window.Dispatcher, 340);

                    AssertStepClickStartsAwayFromTarget(nextButton, progressBar, fill, track, window.Dispatcher, 1, forward: true);
                    WaitForAnimationAndDrain(window.Dispatcher, 340);
                    AssertStepClickStartsAwayFromTarget(nextButton, progressBar, fill, track, window.Dispatcher, 2, forward: true);
                    WaitForAnimationAndDrain(window.Dispatcher, 340);

                    progressBar.CurrentStep = 9;
                    WaitForAnimationAndDrain(window.Dispatcher, 340);
                    AssertStepClickStartsAwayFromTarget(nextButton, progressBar, fill, track, window.Dispatcher, 10, forward: true);
                    WaitForAnimationAndDrain(window.Dispatcher, 340);
                    AssertStepClickStartsAwayFromTarget(backButton, progressBar, fill, track, window.Dispatcher, 9, forward: false);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void GalleryNavigationPage_CompactSourceMatchesLiveInteraction()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryNavigationPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    DemoSampleControl? sample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("CompactNavigationView", StringComparison.Ordinal));
                    Assert.NotNull(sample);

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
        public void GalleryTabsPage_TabViewContentUsesLayerFillSurface()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryTabsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    TabView? tabView = FindByName<TabView>(page, "DemoTabView");
                    Assert.NotNull(tabView);

                    foreach (TabViewItem item in tabView.Items.OfType<TabViewItem>())
                    {
                        AssertTabViewItemContentSurface(item);
                    }

                    ButtonBase? addButton = tabView.Template.FindName("PART_AddTabButton", tabView) as ButtonBase;
                    Assert.NotNull(addButton);
                    addButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, addButton));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.Equal(4, tabView.Items.Count);
                    TabViewItem? selectedTab = tabView.SelectedItem as TabViewItem;
                    Assert.NotNull(selectedTab);
                    AssertTabViewItemContentSurface(selectedTab);

                    DemoSampleControl? sample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("TabViewDocuments", StringComparison.Ordinal));
                    Assert.NotNull(sample);
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
        public void GalleryTypographyPage_TableUsesCompactRowSpacing()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryTypographyPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid? table = FindByName<Grid>(page, "TypographyTable");
                    Assert.NotNull(table);

                    System.Windows.Controls.TextBlock? firstBodyCell = table.Children
                        .OfType<System.Windows.Controls.TextBlock>()
                        .FirstOrDefault(static textBlock => Grid.GetRow(textBlock) is 1 && Grid.GetColumn(textBlock) is 0);
                    Assert.NotNull(firstBodyCell);
                    Assert.Equal(new Thickness(24, 8, 16, 8), firstBodyCell.Margin);

                    System.Windows.Controls.Border? firstShadedRow = table.Children
                        .OfType<System.Windows.Controls.Border>()
                        .FirstOrDefault(static border => Grid.GetRow(border) is 1);
                    Assert.NotNull(firstShadedRow);
                    Assert.Equal(new Thickness(0, 2, 0, 2), firstShadedRow.Margin);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void GalleryTypographyPage_DirectTableKeepsCopyColumnWithoutSourceExpander()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryTypographyPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    List<DemoSampleControl> samples = [.. FindAllVisualChildren<DemoSampleControl>(page)];
                    Assert.Empty(samples);

                    Grid? table = FindByName<Grid>(page, "TypographyTable");
                    Assert.NotNull(table);

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
        public void GallerySettingsPage_UsesFullWidthSettingsRowsForWindowControls()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    System.Windows.Controls.Border? appThemeCard = FindByName<System.Windows.Controls.Border>(page, "AppThemeSettingsCard");
                    System.Windows.Controls.Border? backdropCard = FindByName<System.Windows.Controls.Border>(page, "BackdropSettingsCard");
                    System.Windows.Controls.Border? colorsCard = FindByName<System.Windows.Controls.Border>(page, "ColorsSettingsCard");
                    System.Windows.Controls.ComboBox? backdrop = FindByName<System.Windows.Controls.ComboBox>(page, "BackdropComboBox");
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    System.Windows.Controls.ComboBox? minimize = FindByName<System.Windows.Controls.ComboBox>(page, "MinimizeVisibilityCombo");
                    System.Windows.Controls.ComboBox? maximize = FindByName<System.Windows.Controls.ComboBox>(page, "MaximizeVisibilityCombo");
                    System.Windows.Controls.ComboBox? close = FindByName<System.Windows.Controls.ComboBox>(page, "CloseVisibilityCombo");
                    FrameworkElement? showIcon = FindByName<FrameworkElement>(page, "ShowWindowIconToggle");
                    FrameworkElement? showTitle = FindByName<FrameworkElement>(page, "ShowWindowTitleToggle");

                    Assert.NotNull(appThemeCard);
                    Assert.NotNull(backdropCard);
                    Assert.NotNull(colorsCard);
                    Assert.True(appThemeCard.ActualWidth > 700.0, "Settings cards should stretch across the content column.");
                    Assert.Equal(appThemeCard.ActualWidth, backdropCard.ActualWidth, 1.0);
                    Assert.Equal(backdropCard.ActualWidth, colorsCard.ActualWidth, 1.0);
                    Assert.NotNull(backdrop);
                    Assert.NotNull(accentRow);
                    Assert.NotNull(minimize);
                    Assert.NotNull(maximize);
                    Assert.NotNull(close);
                    Assert.NotNull(showIcon);
                    Assert.NotNull(showTitle);
                    Assert.Equal(7, accentRow.Children.Count);
                    Assert.Equal(GetVisualY(accentRow.Children[0] as FrameworkElement, window) ?? double.MaxValue, GetVisualY(accentRow.Children[6] as FrameworkElement, window) ?? double.MaxValue, 1.0);
                    Assert.True((GetVisualX(backdrop, window) ?? double.MinValue) > (GetVisualX(appThemeCard, window) ?? double.MinValue) + 500.0,
                        "The Backdrop combo box should stay docked to the right side of its settings card.");
                    Assert.True((GetVisualY(maximize, window) ?? double.MinValue) > (GetVisualY(minimize, window) ?? double.MinValue),
                        "Caption button customization should use separate settings rows.");
                    Assert.True((GetVisualY(close, window) ?? double.MinValue) > (GetVisualY(maximize, window) ?? double.MinValue),
                        "Close button customization should appear below Maximize.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void GallerySettingsPage_CompactsControlsAtNarrowWidths()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    window.Width = 560;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    System.Windows.Controls.ComboBox? appTheme = FindByName<System.Windows.Controls.ComboBox>(page, "AppThemeComboBox");
                    System.Windows.Controls.ComboBox? minimize = FindByName<System.Windows.Controls.ComboBox>(page, "MinimizeVisibilityCombo");
                    System.Windows.Controls.StackPanel? accentPanel = FindByName<System.Windows.Controls.StackPanel>(page, "AccentPickerPanel");
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    FrameworkElement? systemAccent = FindByName<FrameworkElement>(page, "SystemAccentButton");
                    System.Windows.Controls.StackPanel? repositoryActions = FindByName<System.Windows.Controls.StackPanel>(page, "RepositoryActionsPanel");
                    FrameworkElement? copyRepository = FindByName<FrameworkElement>(page, "CopyRepositoryButton");

                    Assert.NotNull(appTheme);
                    Assert.NotNull(minimize);
                    Assert.NotNull(accentPanel);
                    Assert.NotNull(accentRow);
                    Assert.NotNull(systemAccent);
                    Assert.NotNull(repositoryActions);
                    Assert.NotNull(copyRepository);

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
        public void GallerySettingsPage_RainbowAccentSwatches_PreserveLogoColors()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    Assert.NotNull(accentRow);

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
                        FrameworkElement? swatch = accentRow.Children[i] as FrameworkElement;
                        Assert.NotNull(swatch);
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
        public void GallerySettingsPage_InvalidAccentSwatchTag_DoesNotChangeAccent()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    Assert.NotNull(accentRow);

                    Controls.Button? swatch = accentRow.Children[0] as Controls.Button;
                    Assert.NotNull(swatch);

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
        public void GalleryAccessibilityPage_KeyboardSamplesUseAlignedRows()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryAccessibilityPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid? primary = FindByName<Grid>(page, "KeyboardSupportPrimaryControls");
                    Assert.NotNull(primary);
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

                    Grid? tabOrder = FindByName<Grid>(page, "KeyboardSupportExplicitOrderControls");
                    Assert.NotNull(tabOrder);
                    Assert.Equal(3, tabOrder.ColumnDefinitions.Count);
                    Assert.Equal(3, tabOrder.Children.Count);
                    Assert.Equal(KeyboardNavigationMode.Local, KeyboardNavigation.GetTabNavigation(tabOrder));

                    HyperlinkButton? hyperlink = FindAllVisualChildren<HyperlinkButton>(primary).FirstOrDefault();
                    Controls.Button? tabOrderFirst = FindByName<Controls.Button>(page, "ExplicitTabOrderFirstButton");
                    Controls.Button? tabOrderSecond = FindByName<Controls.Button>(page, "ExplicitTabOrderSecondButton");
                    Controls.Button? tabOrderThird = FindByName<Controls.Button>(page, "ExplicitTabOrderThirdButton");
                    Assert.NotNull(hyperlink);
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

                    ToggleSwitch? rtlToggle = FindByName<ToggleSwitch>(page, "RtlToggle");
                    Card? rtlCard = FindByName<Card>(page, "RtlDemoCard");
                    Assert.NotNull(rtlToggle);
                    Assert.NotNull(rtlCard);
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
        public void GalleryIconsPage_IconCatalogIsScrollableAndVirtualized()
        {
            RunOnSta(static delegate
            {
                EnsureTheme();
                GalleryIconsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Controls.ListView? list = FindByName<Controls.ListView>(page, "IconCatalogList");
                    Assert.NotNull(list);
                    Assert.True(list.Items.Count > 100, "Icon catalog must load enough rows to exercise virtualization.");

                    System.Windows.Controls.Border? catalogCard = FindByName<System.Windows.Controls.Border>(page, "IconCatalogCard");
                    Assert.NotNull(catalogCard);
                    Assert.Equal(new Thickness(0), catalogCard.Padding);
                    Assert.Equal(new CornerRadius(8), catalogCard.CornerRadius);
                    Assert.Equal(new Thickness(1), catalogCard.BorderThickness);
                    AssertIconBrush(catalogCard.Background, "SolidBackgroundFillColorBaseBrush");
                    AssertIconBrush(catalogCard.BorderBrush, "CardStrokeColorDefaultBrush");
                    Assert.Equal(new Thickness(0), list.BorderThickness);

                    System.Windows.Controls.Border? detailsPanel = FindByName<System.Windows.Controls.Border>(page, "IconDetailsPanel");
                    Assert.NotNull(detailsPanel);
                    Assert.Equal(new Thickness(1, 0, 0, 0), detailsPanel.BorderThickness);
                    AssertIconBrush(detailsPanel.Background, "CardBackgroundFillColorDefaultBrush");
                    AssertIconBrush(detailsPanel.BorderBrush, "DividerStrokeColorDefaultBrush");

                    ScrollViewer? viewer = FindVisualChild<ScrollViewer>(list);
                    Assert.NotNull(viewer);
                    Assert.True(viewer.ViewportHeight > 0, "Icon catalog needs a bounded viewport height.");
                    Assert.True(viewer.ExtentHeight > viewer.ViewportHeight, "Icon catalog should have a scrollable extent.");
                    Assert.True(viewer.ScrollableHeight > 0, "Icon catalog should be scrollable.");

                    int realizedBeforeScroll = CountVisualChildren<ListViewItem>(list);
                    Assert.True(realizedBeforeScroll > 0, "Initial viewport should realize some row containers.");
                    Assert.True(realizedBeforeScroll < list.Items.Count / 2, "Initial layout should not realize most icon rows.");
                    Assert.Null(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1));

                    list.ScrollIntoView(list.Items[^1]);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

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
            System.Windows.Controls.Border? surface = item.Content as System.Windows.Controls.Border;
            Assert.NotNull(surface);
            AssertIconBrush(surface.Background, "LayerFillColorDefaultBrush");
        }

        private static void AssertIconBrush(Brush? actualBrush, string resourceKey)
        {
            SolidColorBrush? actual = actualBrush as SolidColorBrush;
            Assert.NotNull(actual);

            SolidColorBrush? expected = Application.Current?.TryFindResource(resourceKey) as SolidColorBrush;
            Assert.NotNull(expected);
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
                    System.Windows.Controls.Button? copy = FindByName<System.Windows.Controls.Button>(tab.Content as DependencyObject, "CopySourceButton");
                    Assert.NotNull(copy);
                    Assert.Equal(expectedSource, copy.Tag as string, StringComparer.Ordinal);
                    return;
                }
            }

            Assert.Fail("Missing source tab: " + expectedHeader);
        }

        private static string GetSourceTabText(TabControl? tabs, string expectedHeader)
        {
            Assert.NotNull(tabs);
            foreach (object item in tabs.Items)
            {
                if (item is TabItem tab && string.Equals(tab.Header as string, expectedHeader, StringComparison.Ordinal))
                {
                    RichTextBox? viewer = FindByName<RichTextBox>(tab.Content as DependencyObject, "SourceTextViewer");
                    Assert.NotNull(viewer);
                    TextRange textRange = new(viewer.Document.ContentStart, viewer.Document.ContentEnd);
                    return textRange.Text;
                }
            }

            Assert.Fail("Missing source tab: " + expectedHeader);
            return string.Empty;
        }

        private static void EnsureTheme()
        {
            Application? application = WpfTestSta.EnsureApplication();
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            };
            application?.Resources.MergedDictionaries.Add(demoShared);
        }

        private static string GetRepositoryFilePath(params string[] relativeSegments)
        {
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string[] pathParts = new string[relativeSegments.Length + 1];
            pathParts[0] = root;
            Array.Copy(relativeSegments, 0, pathParts, 1, relativeSegments.Length);
            return Path.Combine(pathParts);
        }

        private static string ReadRepositoryFile(params string[] relativeSegments)
        {
            string path = GetRepositoryFilePath(relativeSegments);
            Assert.True(File.Exists(path), "Repository file must be readable at: " + path);
            return File.ReadAllText(path);
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
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
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
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
            return window;
        }

        private static object GetSelectedPageContent(MainWindow window)
        {
            NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
            Assert.NotNull(nav);

            Assert.NotNull(nav.SelectedItem as NavigationViewItem);
            return nav.Content;
        }

        private static void InvokeTitleBarBack(TitleBar titleBar)
        {
            System.Windows.Controls.Button? backButton = FindByName<System.Windows.Controls.Button>(titleBar, "PART_BackButton");
            Assert.NotNull(backButton);
            backButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, backButton));
            Drain(titleBar.Dispatcher);
        }

        private static void InvokeSettingsItem(NavigationViewItem settingsItem)
        {
            // Settings is a FooterMenuItems entry; drive selection through the same control path a
            // click/keyboard invocation uses (raises ItemInvoked and shows the footer indicator).
            NavigationView.FromItemContainer(settingsItem)?.SelectFooterMenuItem(settingsItem);
            Drain(settingsItem.Dispatcher);
        }

        private static double? GetVisualX(FrameworkElement? element, Visual ancestor)
        {
            return element?.TransformToAncestor(ancestor).Transform(new Point(0, 0)).X;
        }

        private static double? GetVisualY(FrameworkElement? element, Visual ancestor)
        {
            return element?.TransformToAncestor(ancestor).Transform(new Point(0, 0)).Y;
        }

        private static double? GetVisualCenterX(FrameworkElement element, Visual ancestor)
        {
            return GetVisualX(element, ancestor) + (element.ActualWidth / 2.0);
        }

        private static double? GetVisualCenterY(FrameworkElement element, Visual ancestor)
        {
            return GetVisualY(element, ancestor) + (element.ActualHeight / 2.0);
        }

        private static void Drain(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));
        }

        private static void WaitForAnimationAndDrain(Dispatcher dispatcher, int milliseconds)
        {
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(milliseconds),
            };
            timer.Tick += delegate
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
            Drain(dispatcher);
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
            Drain(window.Dispatcher);

            TraversalRequest request = new(FocusNavigationDirection.Next);
            bool moved = source.MoveFocus(request);
            Drain(window.Dispatcher);

            Assert.True(moved, "Keyboard focus should move to the next tab stop. " + message);
            Assert.Same(expected, Keyboard.FocusedElement);
        }

        private static Controls.Button? FindStepButton(DependencyObject root, string tag)
        {
            return FindAllVisualChildren<Controls.Button>(root)
                .FirstOrDefault(button => string.Equals(button.Tag as string, tag, StringComparison.Ordinal));
        }

        private static void AssertStepClickStartsAwayFromTarget(
            Controls.Button button,
            Controls.ProgressBar progressBar,
            FrameworkElement fill,
            FrameworkElement track,
            Dispatcher dispatcher,
            int expectedStep,
            bool forward)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
            WaitForAnimationAndDrain(dispatcher, 40);

            Assert.Equal(expectedStep, progressBar.CurrentStep);
            double targetWidth = track.ActualWidth * expectedStep / progressBar.Steps;

            // The determinate fill is laid out at the full track width and animates
            // PART_FillScale.ScaleX in [0,1], so the visually rendered progress width is
            // the track width multiplied by the current (possibly animating) scale.
            ScaleTransform? fillScale = fill.RenderTransform as ScaleTransform;
            Assert.NotNull(fillScale);
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
            if (root is FrameworkElement element && element.FindName(name) is T named)
            {
                return named;
            }

            foreach (T item in FindAllVisualChildren<T>(root))
            {
                if (string.Equals(item.Name, name, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
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
