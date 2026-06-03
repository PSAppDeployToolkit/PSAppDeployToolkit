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

using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FluenceExpander = Fluence.Wpf.Controls.Expander;
using FluenceListView = Fluence.Wpf.Controls.ListView;
using WpfBorder = System.Windows.Controls.Border;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    [TestClass]
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
            new("status", typeof(GalleryStatusPage))
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

        [TestMethod]
        public void MainWindow_DirectNavigation_LoadsConcretePages()
        {
            RunOnSta(delegate
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
                        Assert.IsNotNull(content, "Navigation must create page content for tag: " + expectation.Tag);
                        Assert.AreEqual(expectation.PageType, content.GetType(), "Tag should load the concrete page directly: " + expectation.Tag);
                        Assert.AreNotEqual("GalleryControlPage", content.GetType().Name, "Generated page shell must not be used.");
                        Assert.AreNotEqual("GalleryCategoryPage", content.GetType().Name, "Category overview shell must not be used.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_InitialSelection_LoadsHomePageContent()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    object content = GetSelectedPageContent(window);
                    Assert.IsNotNull(content, "Initial home navigation must create page content.");
                    Assert.AreEqual(typeof(GalleryHomePage), content.GetType(), "The first selected page should be Home.");

                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    Assert.AreSame(content, nav.Content, "NavigationView.Content should be populated for the initial Home page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryHomePage_BrandBannerImageSwitchesWithTheme()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryHomePage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Image? image = FindByName<Image>(page, "BrandBannerImage");
                    Assert.IsNotNull(image, "Home page should expose the brand banner image.");
                    Assert.IsInstanceOfType(image.Source, typeof(BitmapImage), "The light banner PNG should load as an image source.");
                    Assert.AreEqual("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-light.png", image.Tag as string,
                        "Light theme should use the light banner graphic.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsInstanceOfType(image.Source, typeof(BitmapImage), "The dark banner PNG should load as an image source.");
                    Assert.AreEqual("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-dark.png", image.Tag as string,
                        "Dark theme should use the dark banner graphic.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-light.png", image.Tag as string,
                        "Returning to light theme should restore the light banner graphic.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryHomePage_UsesPngBannerResourcesAndGitHubLink()
        {
            string project = ReadRepositoryFile("Fluence.Wpf.Demo", "Fluence.Wpf.Demo.csproj");
            StringAssert.Contains(project, "<Resource Include=\"Resources\\fluence-wpf-banner-*.png\" />");
            StringAssert.Contains(project, "<Page Remove=\"Resources\\fluence-wpf-banner-*.xaml\" />");

            string homePage = ReadRepositoryFile("Fluence.Wpf.Demo", "Pages", "GalleryHomePage.xaml");
            StringAssert.Contains(homePage, "https://github.com/sintaxasn/fluence.wpf");
        }

        [TestMethod]
        public void DemoProjects_UseSharedFluenceIcoIcon()
        {
            const string iconPath = @"Resources\fluence-wpf-appicon-256.ico";

            AssertProjectUsesIcon("Fluence.Wpf.Demo", "Fluence.Wpf.Demo.csproj", iconPath);
            AssertProjectUsesIcon("Fluence.Wpf.Demo.Mvvm", "Fluence.Wpf.Demo.Mvvm.csproj", iconPath);

            StringAssert.Contains(ReadRepositoryFile("Fluence.Wpf.Demo", "MainWindow.xaml"),
                "Icon=\"Resources/fluence-wpf-appicon-256.ico\"");
            StringAssert.Contains(ReadRepositoryFile("Fluence.Wpf.Demo.Mvvm", "MainWindow.xaml"),
                "Icon=\"Resources/fluence-wpf-appicon-256.ico\"");

            Assert.IsTrue(File.Exists(GetRepositoryFilePath("Fluence.Wpf.Demo", "Resources", "fluence-wpf-appicon-256.ico")),
                "The gallery demo icon should exist.");
            Assert.IsTrue(File.Exists(GetRepositoryFilePath("Fluence.Wpf.Demo.Mvvm", "Resources", "fluence-wpf-appicon-256.ico")),
                "The MVVM demo icon should exist.");
        }

        [TestMethod]
        public void MainWindow_Search_NavigatesToGroupedConcretePage()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");

                    search.Text = "progress ring";
                    search.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Enter)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent
                    });
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    object content = GetSelectedPageContent(window);
                    Assert.AreEqual(typeof(GalleryStatusPage), content.GetType(), "Search should resolve progress terms to the grouped Status page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_BackRequested_WalksVisitedPagesInOrder()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Demo shell should expose a TitleBar.");

                    window.NavigateTo("buttons");
                    Drain(window.Dispatcher);
                    window.NavigateTo("trees");
                    Drain(window.Dispatcher);
                    window.NavigateTo("status");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(typeof(GalleryStatusPage), GetSelectedPageContent(window).GetType(),
                        "Setup should end on the Status gallery page.");

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.AreEqual(typeof(GalleryTreesPage), GetSelectedPageContent(window).GetType(),
                        "First Back should return to the previously visited Trees page.");

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.AreEqual(typeof(GalleryButtonsPage), GetSelectedPageContent(window).GetType(),
                        "Second Back should return to the previously visited Buttons page.");

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.AreEqual(typeof(GalleryHomePage), GetSelectedPageContent(window).GetType(),
                        "Third Back should return to the initial Home page.");

                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    Assert.IsFalse(nav.IsBackEnabled,
                        "Back should become disabled when the demo history is empty.");

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.AreEqual(typeof(GalleryHomePage), GetSelectedPageContent(window).GetType(),
                        "Back with empty history should leave the current Home page unchanged.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationCatalog_RemovesWindowingPage()
        {
            List<DemoNavigationItem> items = [.. DemoNavigationCatalog.Items];
            Assert.IsGreaterThanOrEqualTo(1, items.Count, "Navigation catalog should contain at least one entry.");
            Assert.AreEqual("Accessibility", items[items.Count - 1].Title,
                "Accessibility should be the final regular NavigationView item after Windowing moves into Settings.");
            Assert.IsFalse(items.Any(item => string.Equals(item.Title, "Windowing", StringComparison.Ordinal)),
                "Windowing should not remain as a regular NavigationView item.");
            Assert.IsFalse(items.Any(item => string.Equals(item.Route, "window", StringComparison.Ordinal)),
                "The old Windowing route should be removed from the regular navigation catalog.");
        }

        [TestMethod]
        public void GalleryPages_UseSharedWinUiGalleryPageLayout()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                Style? scrollStyle = Application.Current?.TryFindResource("GalleryPageScrollViewerStyle") as Style;
                Style? fluentScrollStyle = Application.Current?.TryFindResource("ScrollViewerStyle") as Style;
                Style? contentStyle = Application.Current?.TryFindResource("GalleryPageContentStackStyle") as Style;
                Style? contentGridStyle = Application.Current?.TryFindResource("GalleryPageContentGridStyle") as Style;
                Assert.IsNotNull(scrollStyle, "Demo shared styles should expose the gallery scroll style.");
                Assert.IsNotNull(fluentScrollStyle, "Fluence should expose the styled ScrollViewer template.");
                Assert.AreSame(fluentScrollStyle, scrollStyle.BasedOn,
                    "Gallery page scroll viewers should be based on ScrollViewerStyle so NavigationView content keeps styled scrollbars.");
                Assert.IsNotNull(contentStyle, "Demo shared styles should expose the gallery content style.");
                Assert.IsNotNull(contentGridStyle, "Demo shared styles should expose the gallery grid content style.");

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
                    new GallerySettingsPage()
                ];

                foreach (UserControl page in pages)
                {
                    Window window = CreateHostWindow(page);
                    try
                    {
                        if (page is GalleryIconsPage)
                        {
                            Grid? pageRoot = FindByName<Grid>(page, "PageRoot");
                            Assert.IsNotNull(pageRoot, "Icons should keep a named root for virtualization layout.");
                            Assert.IsNull(pageRoot.Background,
                                "Icons root should leave the page background to the NavigationView content surface.");

                            Grid? pageContent = FindByName<Grid>(page, "PageContent");
                            Assert.IsNotNull(pageContent, "Icons should keep a named content grid for bounded virtualization layout.");
                            Assert.AreSame(contentGridStyle, pageContent.Style,
                                "Icons should use the shared grid content style for its bounded catalog layout.");
                            Assert.AreEqual(new Thickness(36, 24, 36, 48), pageContent.Margin,
                                "Icons should keep the shared page margins including 48px bottom breathing room.");
                            Assert.IsTrue(double.IsPositiveInfinity(pageContent.MaxWidth),
                                "Icons should stretch instead of keeping the old max content width.");
                            Assert.AreEqual(HorizontalAlignment.Stretch, pageContent.HorizontalAlignment,
                                "Icons content should stretch within the page.");
                            continue;
                        }

                        SmoothScrollViewer? scrollViewer = FindVisualChild<SmoothScrollViewer>(page);
                        Assert.IsNotNull(scrollViewer, page.GetType().Name + " should use SmoothScrollViewer.");
                        Assert.AreSame(scrollStyle, scrollViewer.Style,
                            page.GetType().Name + " should use the shared gallery scroll style.");

                        System.Windows.Controls.StackPanel? content = scrollViewer.Content as System.Windows.Controls.StackPanel;
                        Assert.IsNotNull(content, page.GetType().Name + " should use a StackPanel content host.");
                        Assert.AreSame(contentStyle, content.Style,
                            page.GetType().Name + " should use the shared content style.");
                        Assert.AreEqual(new Thickness(36, 24, 36, 48), content.Margin,
                            page.GetType().Name + " should keep the shared page margins including 48px bottom breathing room.");
                        Assert.IsTrue(double.IsPositiveInfinity(content.MaxWidth),
                            page.GetType().Name + " should stretch instead of keeping the old max content width.");
                        Assert.AreEqual(HorizontalAlignment.Stretch, content.HorizontalAlignment,
                            page.GetType().Name + " content should stretch.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_TitleBarSearch_StaysVisibleWhenContentExtendsIntoTitleBar()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(Visibility.Visible, search.Visibility, "Search should start visible in the normal title bar.");

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Visible, search.Visibility,
                        "Search should stay visible when content extends into the title bar.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_TitleBarSearch_IsCenteredInWindow()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(230.0, search.Width, 0.01,
                        "Demo title-bar search should use the requested 230px resting width.");
                    Assert.AreEqual(230.0, search.MinWidth, 0.01,
                        "Demo title-bar search should not shrink below the requested 230px resting width.");
                    Assert.AreEqual(475.0, search.MaxWidth, 0.01,
                        "Demo title-bar search should keep the requested 475px expanded cap.");
                    Assert.AreEqual(230.0, search.ActualWidth, 0.5,
                        "Demo title-bar search should rest at 230px when not focused.");
                    Assert.AreEqual(window.ActualWidth / 2.0, GetVisualCenterX(search, window) ?? double.MaxValue, 1.0,
                        "Search should stay horizontally centered in the window.");
                    Assert.AreEqual((GetVisualCenterY(shellTitleBar, window) ?? double.MinValue) + 2.0, GetVisualCenterY(search, window) ?? double.MaxValue, 1.0,
                        "Search should sit 2px below the title-bar vertical center.");

                    Assert.IsTrue(search.Focus(), "Search should accept keyboard focus.");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(230.0, search.ActualWidth, 0.5,
                        "Demo title-bar search should not expand just because it receives focus.");
                    Assert.AreEqual(window.ActualWidth / 2.0, GetVisualCenterX(search, window) ?? double.MaxValue, 1.0,
                        "Focused search should stay horizontally centered in the window.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_UsesHorizontalNavigationChrome()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
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
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");

                    WpfButton? titleBarToggle = FindByName<WpfButton>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.IsNotNull(titleBarToggle, "Extended title bar should expose a pane toggle button.");
                    Assert.AreEqual(Visibility.Visible, titleBarToggle.Visibility,
                        "Pane toggle should move into the title bar when content extends into the title bar.");
                    Assert.AreEqual(40.0, titleBarToggle.ActualWidth, 0.5,
                        "Title-bar pane toggle should match the WinUI-canonical 40 px glyph button width.");

                    WpfTextBlock? titleBarGlyph = FindVisualChild<WpfTextBlock>(titleBarToggle);
                    Assert.IsNotNull(titleBarGlyph, "Title-bar pane toggle should render a Segoe Fluent Icons glyph.");
                    Assert.AreEqual(16.0, titleBarGlyph.FontSize, 0.01,
                        "Title-bar pane toggle glyph should match the compact title-bar glyph style.");

                    WpfButton? titleBarBack = FindByName<WpfButton>(shellTitleBar, "PART_BackButton");
                    Assert.IsNotNull(titleBarBack, "Extended title bar should expose a back button slot.");
                    Assert.AreEqual(Visibility.Visible, titleBarBack.Visibility,
                        "Visited-page history should make the title-bar back slot visible in Left mode.");
                    Assert.IsLessThan(GetVisualX(titleBarToggle, window) ?? double.MaxValue, GetVisualX(titleBarBack, window) ?? double.MaxValue,
                        "Back should occupy the first title-bar navigation slot.");
                    WpfTextBlock? titleBarBackGlyph = FindVisualChild<WpfTextBlock>(titleBarBack);
                    Assert.IsNotNull(titleBarBackGlyph, "Title-bar back button should render a Segoe Fluent Icons glyph.");

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.IsNotNull(firstItem, "DemoNav should contain a first navigation item.");
                    FontIcon? itemGlyph = FindVisualChild<FontIcon>(firstItem);
                    Assert.IsNotNull(itemGlyph, "First navigation item should render an icon.");
                    Assert.AreEqual(GetVisualCenterX(itemGlyph, window) ?? double.MaxValue, GetVisualCenterX(titleBarBackGlyph, window) ?? double.MaxValue, 2.5,
                        "The first title-bar navigation glyph should align with the NavigationViewItem glyph rail.");

                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Assert.IsNotNull(titleIcon, "Extended title bar icon presenter should exist.");
                    Assert.AreEqual(Visibility.Visible, titleIcon.Visibility,
                        "Extended title bar icon should be visible by default.");
                    Image? titleIconImage = FindVisualChild<Image>(titleIcon);
                    Assert.IsNotNull(titleIconImage, "Extended title bar icon should render an Image.");
                    Assert.AreEqual(20.0, titleIconImage.ActualWidth, 0.5,
                        "Extended title bar icon should match the larger navigation glyph size.");
                    Assert.AreEqual(20.0, titleIconImage.ActualHeight, 0.5,
                        "Extended title bar icon should match the larger navigation glyph size.");
                    Assert.IsTrue(GetVisualX(titleIcon, window) >= GetVisualX(titleBarToggle, window) + titleBarToggle.ActualWidth - 0.5,
                        "Title identity should start after the title-bar navigation slot.");

                    _ = nav.ApplyTemplate();
                    WpfButton? internalToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as WpfButton;
                    Assert.IsNotNull(internalToggle, "Internal NavigationView pane toggle should still exist in the template.");
                    Assert.AreEqual(Visibility.Collapsed, internalToggle.Visibility,
                        "Internal NavigationView pane toggle should be hidden while title-bar chrome owns it.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_FirstGlyphTracksBackAvailability()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    nav.IsBackButtonVisible = true;
                    nav.IsBackEnabled = true;

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    WpfButton? titleBarBack = FindByName<WpfButton>(shellTitleBar, "PART_BackButton");
                    WpfButton? titleBarToggle = FindByName<WpfButton>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.IsNotNull(titleBarBack, "Extended title bar should expose a back button.");
                    Assert.IsNotNull(titleBarToggle, "Extended title bar should expose a pane toggle button.");
                    Assert.AreEqual(Visibility.Visible, titleBarBack.Visibility,
                        "Back should be visible in the title bar when back navigation is enabled.");
                    Assert.AreEqual(Visibility.Visible, titleBarToggle.Visibility,
                        "Pane toggle should remain visible after back appears.");
                    Assert.IsLessThan(GetVisualX(titleBarToggle, window) ?? double.MaxValue, GetVisualX(titleBarBack, window) ?? double.MaxValue,
                        "Back should occupy the first title-bar navigation slot.");
                    Assert.AreEqual(GetVisualCenterY(titleBarBack, window) ?? double.MaxValue, GetVisualCenterY(titleBarToggle, window) ?? double.MaxValue, 1.0,
                        "Back and pane toggle should be vertically centered in the same title-bar row.");

                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Assert.IsNotNull(titleIcon, "Extended title bar icon should exist.");
                    Assert.AreEqual(Visibility.Visible, titleIcon.Visibility,
                        "Extended title bar icon should be visible while tracking title identity reflow.");
                    double? titleIconWithBackX = GetVisualX(titleIcon, window);

                    nav.IsBackEnabled = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, titleBarBack.Visibility,
                        "Back must collapse in the title bar when back navigation is disabled.");
                    Assert.AreEqual((titleIconWithBackX ?? double.MaxValue) - 42.0, GetVisualX(titleIcon, window) ?? double.MaxValue, 1.5,
                        "Title identity should shift left by the compact back rail when the back glyph collapses.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_KeepsNavigationItemsBelowTitleBar()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(42.0, window.TitleBarHeight, 0.01,
                        "The demo shell should use a compact 42px title bar.");

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.IsNotNull(firstItem, "DemoNav should contain a first navigation item.");
                    double? itemY = GetVisualY(firstItem, window);
                    Assert.IsTrue(itemY >= window.TitleBarHeight - 0.5,
                        "The first navigation item should be below the extended title bar. itemY=" + itemY + ", titleBarHeight=" + window.TitleBarHeight);
                    Assert.IsTrue(itemY <= window.TitleBarHeight + 14.0,
                        "The first navigation item should not keep the old extra title-bar spacer. itemY=" + itemY + ", titleBarHeight=" + window.TitleBarHeight);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_TopPane_UsesNonExtendedTitleBarWithoutPaneToggleChrome()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
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

                    Assert.IsFalse(window.ExtendsContentIntoTitleBar,
                        "Top NavigationView mode should keep the FluenceWindow title bar non-extended.");
                    Assert.IsTrue(nav.IsPaneOpen, "Top NavigationView mode should coerce IsPaneOpen=True.");
                    Assert.IsFalse(nav.IsPaneToggleButtonVisible,
                        "Top NavigationView mode should coerce the pane toggle hidden.");

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Demo shell should expose a TitleBar.");
                    WpfButton? titleBarToggle = FindByName<WpfButton>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.IsNotNull(titleBarToggle, "TitleBar should expose a pane toggle slot.");
                    Assert.AreEqual(Visibility.Collapsed, titleBarToggle.Visibility,
                        "Top mode should not show a pane toggle in the title bar.");
                    WpfButton? titleBarBack = FindByName<WpfButton>(shellTitleBar, "PART_BackButton");
                    Assert.IsNotNull(titleBarBack, "TitleBar should expose a back button slot.");
                    Assert.AreEqual(Visibility.Visible, titleBarBack.Visibility,
                        "Top mode should move the requested back button into the title bar.");
                    WpfTextBlock? titleBarBackGlyph = FindVisualChild<WpfTextBlock>(titleBarBack);
                    Assert.IsNotNull(titleBarBackGlyph, "Title-bar back button should render a Segoe Fluent Icons glyph.");
                    Assert.AreEqual(16.0, titleBarBackGlyph.FontSize, 0.01,
                        "Title-bar back glyph should match the compact title-bar glyph style.");
                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(titleIcon, "TitleBar should expose the app icon presenter.");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(Visibility.Visible, titleIcon.Visibility,
                        "Top mode should keep the title-bar app icon visible after the back slot.");
                    Assert.IsLessThan(GetVisualX(titleIcon, window) ?? double.MaxValue, GetVisualX(titleBarBack, window) ?? double.MaxValue,
                        "Top mode back should be the first visible title-bar item.");
                    Assert.IsLessThan(GetVisualX(search, window) ?? double.MaxValue, GetVisualX(titleBarBack, window) ?? double.MaxValue,
                        "Top mode back should appear before centered title-bar content.");

                    _ = nav.ApplyTemplate();
                    WpfButton? internalBack = nav.Template.FindName(NavigationView.PartBackButton, nav) as WpfButton;
                    WpfButton? internalToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as WpfButton;
                    Assert.IsNotNull(internalBack, "Internal NavigationView back button should exist.");
                    Assert.AreEqual(Visibility.Collapsed, internalBack.Visibility,
                        "Demo shell should suppress the internal Top pane back button while the title bar owns back navigation.");
                    Assert.IsNull(internalToggle,
                        "Top NavigationView template should not include the pane toggle button.");

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.IsNotNull(firstItem, "DemoNav should contain a first navigation item.");
                    Assert.AreEqual(Visibility.Visible, firstItem.Visibility,
                        "Top NavigationView items should stay visible in the horizontal strip.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_SettingsFooter_NavigatesToSelectableSettingsPage()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    Assert.IsNotNull(settings, "The demo shell should expose a selectable Settings footer item.");
                    Assert.IsNull(FindByName<FrameworkElement>(window, "PaneModeToggle"),
                        "The old top-mode ToggleSwitch should be removed from the demo shell.");

                    InvokeSettingsItem(settings);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsInstanceOfType(nav.Content, typeof(GallerySettingsPage),
                        "Selecting the footer Settings item should navigate to the Settings page.");
                    Assert.IsTrue(settings.IsSelected,
                        "The footer Settings item should show the same selected state as navigation list items.");
                    Assert.IsTrue(nav.FooterMenuItems.Contains(settings),
                        "Settings should live in the FooterMenuItems region.");
                    Assert.AreSame(settings, nav.SelectedFooterItem,
                        "Selecting Settings should make it the active footer selection.");
                    Assert.IsNull(nav.SelectedItem,
                        "Footer selection should clear the main-menu SelectedItem so only one region is selected.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_SettingsFooter_CollapsesLabelWhenPaneClosed()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    Assert.IsNotNull(settings, "The Settings footer item must exist.");

                    Assert.AreEqual(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode,
                        "Demo shell starts in expanded Left navigation mode.");
                    // As a FooterMenuItems entry, Settings uses the standard NavigationViewItem template:
                    // the label is collapsed/shown by the template (it is not emptied), exactly like the
                    // main menu items. Content stays "Settings" throughout.
                    Assert.AreEqual("Settings", settings.Content as string,
                        "The Settings footer item keeps its label content; the template toggles the label visual.");
                    ContentPresenter? label = FindByName<ContentPresenter>(settings, "ContentPresenter");
                    Assert.IsNotNull(label, "Footer item must expose the label ContentPresenter.");
                    Assert.AreEqual(Visibility.Visible, label.Visibility,
                        "Expanded Left mode should show the Settings label.");

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    label = FindByName<ContentPresenter>(settings, "ContentPresenter");
                    Assert.IsNotNull(label, "Footer item must still expose the label ContentPresenter after re-templating.");
                    Assert.AreEqual(Visibility.Collapsed, label.Visibility,
                        "A closed LeftCompact pane should collapse the Settings label to an icon-only entry, like the main items.");
                    Assert.AreEqual(Visibility.Visible, settings.Visibility,
                        "LeftCompact mode should keep the Settings footer item visible as a gear icon.");
                    FontIcon? settingsIcon = settings.Icon as FontIcon;
                    Assert.IsNotNull(settingsIcon, "The Settings footer item should keep its gear icon in compact mode.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_SettingsFooter_DoesNotForceTopPaneModeWhenOpened()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    Assert.IsNotNull(settings, "The Settings footer item must exist.");

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    InvokeSettingsItem(settings);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode,
                        "Opening Settings must not force the shell navigation into Top mode.");
                    Assert.IsFalse(nav.IsPaneOpen,
                        "Opening Settings must preserve the real collapsed pane state.");

                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        nav.Content as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.IsNotNull(navigationStyle, "Settings page should expose the navigation-style selector.");
                    Assert.AreEqual(2, navigationStyle.SelectedIndex,
                        "Settings should reflect the current compact pane state when opened.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GallerySettingsPage_NavigationStyleCombo_TracksExternalIsPaneOpenChanges()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");

                    window.NavigateTo("settings");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        nav.Content as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.IsNotNull(navigationStyle, "Settings page should expose the navigation-style selector.");

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneOpen = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(2, navigationStyle.SelectedIndex,
                        "A Left pane that is externally collapsed should be shown as Left compact.");

                    nav.IsPaneOpen = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(1, navigationStyle.SelectedIndex,
                        "A Left pane that is externally opened should be shown as Left.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_TopPane_OverflowButtonDoesNotOverlapTreesAtMinimumWidth()
        {
            RunOnSta(delegate
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
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    FrameworkElement? overflowButton = FindByName<FrameworkElement>(nav, NavigationView.PartTopOverflowButton);
                    Assert.IsNotNull(overflowButton, "Top pane should expose the overflow button.");
                    Assert.AreEqual(Visibility.Visible, overflowButton.Visibility,
                        "The overflow button should be visible at the minimum demo width.");
                    int visibleNavigationItems = nav.Items.OfType<NavigationViewItem>().Count(item => item.Visibility == Visibility.Visible);
                    Assert.IsTrue(visibleNavigationItems > 1,
                        "Top pane should show every navigation item that fits before the overflow button would overlap the Top toggle status.");
                    NavigationViewItem? settings = FindByName<NavigationViewItem>(window, "SettingsNavigationItem");
                    Assert.IsNotNull(settings, "Settings footer item should exist.");
                    double overflowRight = (GetVisualX(overflowButton, nav) ?? double.MaxValue) + overflowButton.ActualWidth;
                    double settingsLeft = GetVisualX(settings, nav) ?? double.MinValue;
                    Assert.IsLessThanOrEqualTo(settingsLeft - 4.0 + 1.5, overflowRight,
                        "The three-dot overflow entry should stop before it overlaps the Settings item.");

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

                    Assert.IsNotNull(trees, "DemoNav should include the Trees item.");
                    if (trees.Visibility == Visibility.Visible)
                    {
                        double treesRight = (GetVisualX(trees, nav) ?? double.MinValue) + trees.ActualWidth;
                        double overflowLeft = GetVisualX(overflowButton, nav) ?? double.MaxValue;
                        Assert.IsLessThanOrEqualTo(overflowLeft - 4.0 + 1.5, treesRight,
                            "Trees must not overlap the three-dot overflow entry.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GallerySettingsPage_NavigationStyleCombo_SwitchesPaneModeAndKeepsContentLive()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");

                    window.NavigateTo("settings");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    object settingsPage = nav.Content
                        ?? throw new InvalidOperationException("Settings navigation should create a live Settings page.");
                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        settingsPage as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.IsNotNull(navigationStyle, "Settings page should expose the navigation-style selector.");

                    navigationStyle.SelectedIndex = 1;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.AreEqual(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode,
                        "Choosing Left in Settings should move the shell navigation to Left mode.");
                    Assert.IsTrue(nav.IsPaneOpen,
                        "Choosing Left in Settings should open the left pane instead of preserving a compact state.");
                    Assert.AreSame(settingsPage, nav.Content,
                        "Changing pane mode from Settings should keep the current page content live.");

                    navigationStyle.SelectedIndex = 2;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.AreEqual(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode,
                        "Choosing Left compact in Settings should move the shell navigation to LeftCompact mode.");
                    Assert.IsFalse(nav.IsPaneOpen,
                        "Choosing Left compact in Settings should close the pane.");

                    navigationStyle.SelectedIndex = 0;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.AreEqual(NavigationViewPaneDisplayMode.Top, nav.PaneDisplayMode,
                        "Choosing Top in Settings should restore the top navigation strip.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GallerySettingsPage_NavigationStyleCombo_FollowsShellPaneToggle()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");

                    window.NavigateTo("settings");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    object settingsPage = nav.Content
                        ?? throw new InvalidOperationException("Settings navigation should create a live Settings page.");
                    Controls.ComboBox? navigationStyle = FindByName<Controls.ComboBox>(
                        settingsPage as DependencyObject,
                        "NavigationStyleComboBox");
                    Assert.IsNotNull(navigationStyle, "Settings page should expose the navigation-style selector.");

                    navigationStyle.SelectedIndex = 1;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode,
                        "The test should start from expanded Left navigation.");
                    Assert.IsTrue(nav.IsPaneOpen, "Left navigation should be expanded before the pane toggle is clicked.");
                    Assert.AreEqual(1, navigationStyle.SelectedIndex,
                        "Settings should show Left before the shell pane toggle is clicked.");

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should expose the shell pane toggle.");
                    WpfButton? titleBarToggle = FindByName<WpfButton>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.IsNotNull(titleBarToggle, "Shell title bar should expose a pane toggle button in Left navigation.");
                    Assert.AreEqual(Visibility.Visible, titleBarToggle.Visibility,
                        "The shell pane toggle should be visible in Left navigation.");

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.IsTrue(nav.GetPaneColumnWidthForTesting() > 48.0,
                        "Collapsing Left navigation should start the sidebar width animation instead of snapping to compact width.");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode,
                        "Clicking the shell pane toggle should keep the demo shell in Left mode so the sidebar can animate.");
                    Assert.IsFalse(nav.IsPaneOpen,
                        "Clicking the shell pane toggle should collapse the Left pane.");
                    Assert.AreEqual(2, navigationStyle.SelectedIndex,
                        "Settings should still show the compact visual state after the shell pane toggle collapses the Left pane.");

                    WaitForAnimationAndDrain(window.Dispatcher, 220);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.IsLessThan(280.0, nav.GetPaneColumnWidthForTesting(),
                        "Expanding Left navigation should start from the current compact width instead of snapping open.");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode,
                        "Clicking the shell pane toggle again should return to expanded Left navigation.");
                    Assert.IsTrue(nav.IsPaneOpen,
                        "Expanded Left should keep the pane open after the second pane-toggle click.");
                    Assert.AreEqual(1, navigationStyle.SelectedIndex,
                        "Settings should sync back to Left after the shell pane toggle expands the pane.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryNavigationPage_CompactSamplePaneToggleOpensPane()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryNavigationPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(page, "CompactNavigationDemo");
                    Assert.IsNotNull(nav, "Navigation page should expose the compact sample NavigationView.");
                    Assert.IsFalse(nav.IsPaneOpen, "Compact sample should start collapsed.");

                    WpfButton? paneToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as WpfButton;
                    Assert.IsNotNull(paneToggle, "Compact sample should expose the pane toggle button.");

                    Controls.Button? sampleToggle = FindByName<Controls.Button>(page, "CompactPaneToggleButton");
                    Assert.IsNull(sampleToggle,
                        "Compact sample should use NavigationView's built-in pane toggle instead of a right-rail button.");

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsTrue(nav.IsPaneOpen,
                        "Clicking the built-in compact pane toggle should open the sample pane.");

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsFalse(nav.IsPaneOpen,
                        "Clicking the built-in compact pane toggle should close the sample pane.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_TrimsTitleToSearchClearance()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 1200;
                    window.SetUserShowIcon(true, window.Icon);
                    window.SetUserShowTitle(true, "Fluence.Wpf Control Gallery Extended Title That Should Trim Before Search");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    WpfTextBlock? titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(titleText, "Extended title bar title should exist.");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(Visibility.Visible, titleText.Visibility,
                        "A long title should stay visible when there is enough room to trim before the search box.");
                    double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                    double searchLeft = GetVisualX(search, window) ?? double.MaxValue;
                    double titleClearanceRight = searchLeft - 12.0;
                    Assert.IsLessThanOrEqualTo(titleClearanceRight, titleRight,
                        "The title text should not cross the 12px search clearance.");
                    Assert.AreEqual(titleClearanceRight, titleRight, 10.0,
                        "The title text should extend close to the 12px search clearance before trimming; 10 px tolerance accounts for character-width residuals at the 14 pt title font (BodyTextBlockStyle).");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_HidesTitleTextWhenItOverlapsSearch()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 760;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(true, window.Icon);
                    window.SetUserShowTitle(true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    WpfTextBlock? titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.IsNotNull(titleIcon, "Extended title bar icon should exist.");
                    Assert.IsNotNull(titleText, "Extended title bar title should exist.");
                    Assert.AreEqual(Visibility.Visible, titleIcon.Visibility,
                        "Title icon should remain visible when title text is hidden for search clearance.");
                    if (titleText.Visibility == Visibility.Visible)
                    {
                        Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                        Assert.IsNotNull(search, "Demo search box must be present.");
                        double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window) ?? double.MaxValue;
                        Assert.IsLessThanOrEqualTo(searchLeft - 12.0, titleRight,
                            "Visible title text must keep a 12px clearance before the search box.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_DoesNotLetTitleOverlapSearchAtMinimumWidth()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 698;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(true, window.Icon);
                    window.SetUserShowTitle(true, "Fluence.Wpf Control Gallery Extended Title That Must Never Overlap Search");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(window.ActualWidth / 2.0, GetVisualCenterX(search, window) ?? double.MaxValue, 1.0,
                        "Search should stay horizontally centered in the window even when title text is constrained.");

                    WpfTextBlock? titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.IsNotNull(titleText, "Extended title bar title should exist.");
                    if (titleText.Visibility == Visibility.Visible)
                    {
                        double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window) ?? double.MaxValue;
                        Assert.IsLessThanOrEqualTo(searchLeft - 12.0, titleRight,
                            "Visible title text must keep a 12px clearance before the centered search box.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_RestoresTitleTextWhenSearchHasRoom()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);

                    window.Width = 760;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(true, window.Icon);
                    window.SetUserShowTitle(true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    WpfTextBlock? titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.IsNotNull(titleText, "Extended title bar title should exist.");
                    if (titleText.Visibility == Visibility.Visible)
                    {
                        Controls.TextBox? setupSearch = FindByName<Controls.TextBox>(window, "NavSearchBox");
                        Assert.IsNotNull(setupSearch, "Demo search box must be present.");
                        double titleRight = (GetVisualX(titleText, window) ?? double.MinValue) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(setupSearch, window) ?? double.MaxValue;
                        Assert.IsLessThanOrEqualTo(searchLeft - 12.0, titleRight,
                            "Setup should hide or trim title text before it crosses the 12px search clearance.");
                    }

                    window.Width = 1200;
                    window.SetUserShowTitle(true, "Fluence.Wpf");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(Visibility.Visible, titleText?.Visibility,
                        "Title text should return when it can fit without touching the search box.");
                    Assert.AreEqual("Fluence.Wpf", titleText?.Text,
                        "The visible title should use the current user title.");
                    Assert.IsTrue(GetVisualX(titleText, window) + titleText?.ActualWidth + 12.0 <= GetVisualX(search, window),
                        "Visible title text should keep the search clearance gap.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_TitleBarSearch_DoesNotShiftWhenChromeOptionsChange()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");

                    double? initialX = GetVisualX(search, window);

                    window.SetUserShowIcon(false, window.Icon);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0,
                        "Search should not shift when the demo hides the icon.");

                    window.SetUserShowTitle(false, window.Title);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0,
                        "Search should not shift when the demo hides the title.");

                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    window.IsMaximizeButtonVisible = Visibility.Collapsed;
                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0,
                        "Search should not shift when caption buttons are collapsed.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DemoSampleControl_ExpanderUsesInMemorySourceTabs()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<ui:Button Content=\"Save\" />",
                    CSharpSource = "private void Save_Click(object sender, RoutedEventArgs e) { }",
                    DemoContent = new WpfTextBlock { Text = "Visible sample" }
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    FluenceExpander? expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    Assert.IsNotNull(expander, "Inline source expander must exist.");
                    Assert.IsFalse(expander.IsExpanded, "Source starts collapsed.");

                    expander.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl? tabs = FindByName<TabControl>(sample, "SourceTabControl");
                    Assert.IsNotNull(tabs, "Expanded source creates a TabControl.");
                    Assert.AreEqual(2, tabs.Items.Count, "XAML plus C# source should create two tabs.");
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                    AssertSourceTab(tabs, "C#", sample.CSharpSource);

                    WpfBorder? sampleCard = FindByName<WpfBorder>(sample, "SampleCard");
                    Assert.IsNotNull(sampleCard, "Sample host should expose the sample surface.");
                    Assert.AreEqual(new CornerRadius(8, 8, 0, 0), sampleCard.CornerRadius,
                        "Sample surface should square off its bottom corners so source attaches.");
                    Assert.AreEqual(new CornerRadius(0, 0, 8, 8), expander.CornerRadius,
                        "Source expander should square off its top corners so it joins the card.");
                    Assert.AreEqual(new Thickness(1, 0, 1, 1), expander.BorderThickness,
                        "Source expander should share the card seam without a duplicate top stroke.");
                    Assert.AreEqual((GetVisualY(sampleCard, window) ?? double.MinValue) + sampleCard.ActualHeight, GetVisualY(expander, window) ?? double.MinValue, 0.5,
                        "Source expander should be attached directly below the sample surface.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DemoSampleControl_SourceRendererPreservesIndentation()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<Grid>\n    <TextBlock Text=\"Indented\" />\n</Grid>",
                    CSharpSource = "private void Save()\n{\n    string value = \"Indented\";\n}",
                    DemoContent = new WpfTextBlock { Text = "Visible sample" }
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    FluenceExpander? expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    Assert.IsNotNull(expander, "Inline source expander must exist.");
                    expander.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl? tabs = FindByName<TabControl>(sample, "SourceTabControl");
                    string renderedXaml = GetSourceTabText(tabs, "XAML");
                    string renderedCSharp = GetSourceTabText(tabs, "C#");

                    StringAssert.Contains(renderedXaml, "    <TextBlock",
                        "Rendered XAML source should preserve leading indentation.");
                    StringAssert.Contains(renderedCSharp, "    string value",
                        "Rendered C# source should preserve leading indentation.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DemoSampleControl_EmptyCSharpSourceAddsOnlyXamlTab()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                DemoSampleControl sample = new()
                {
                    SampleDescription = "Snippet",
                    XamlSource = "<ui:ToggleSwitch IsChecked=\"True\" />"
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    FluenceExpander? expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    _ = expander?.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabControl? tabs = FindByName<TabControl>(sample, "SourceTabControl");
                    Assert.AreEqual(1, tabs?.Items.Count, "XAML-only samples should not show an empty C# tab.");
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_NonHomePagesExposeInlineSourceSamples()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        if (expectation.PageType == typeof(GalleryTypographyPage))
                        {
                            continue;
                        }

                        window.NavigateTo(expectation.Tag);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        object content = GetSelectedPageContent(window);
                        DependencyObject? root = content as DependencyObject;
                        Assert.IsNotNull(root, "Page content must be visual for tag: " + expectation.Tag);

                        bool found = false;
                        foreach (DemoSampleControl sample in FindAllVisualChildren<DemoSampleControl>(root))
                        {
                            if (!string.IsNullOrWhiteSpace(sample.XamlSource))
                            {
                                found = true;
                                break;
                            }
                        }

                        Assert.IsTrue(found, "Page must expose at least one inline XAML source sample: " + expectation.PageType.Name);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryStatusPage_DeterminateProgressRingUsesNumberBoxBinding()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NumberBox? valueBox = FindByName<NumberBox>(page, "ProgressRingValueBox");
                    ProgressRing? ring = FindByName<ProgressRing>(page, "DeterminateProgressRing");
                    Assert.IsNotNull(valueBox, "Status page should expose the determinate ProgressRing NumberBox.");
                    Assert.IsNotNull(ring, "Status page should expose the determinate ProgressRing.");

                    Assert.AreEqual(1.0, valueBox.Minimum, 0.001, "ProgressRing NumberBox minimum should be 1.");
                    Assert.AreEqual(100.0, valueBox.Maximum, 0.001, "ProgressRing NumberBox maximum should be 100.");
                    Assert.AreEqual(50.0, valueBox.Value, 0.001, "ProgressRing NumberBox default should be 50.");
                    Assert.AreEqual(50.0, ring.Value, 0.001, "ProgressRing should start from the NumberBox value.");

                    valueBox.Value = 75;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(75.0, ring.Value, 0.001,
                        "Determinate ProgressRing value should update from the NumberBox binding.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryStatusPage_ProgressBarValueAllowsZero()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    NumberBox? valueBox = FindByName<NumberBox>(page, "ProgressValueNumberBox");
                    Controls.ProgressBar? progressBar = FindByName<Controls.ProgressBar>(page, "StandardProgressBar");
                    Assert.IsNotNull(valueBox, "Status page should expose the ProgressBar NumberBox.");
                    Assert.IsNotNull(progressBar, "Status page should expose the standard ProgressBar.");

                    Assert.AreEqual(0.0, progressBar.Minimum, 0.001,
                        "Standard ProgressBar should allow an empty 0 percent state.");
                    Assert.AreEqual(0.0, valueBox.Minimum, 0.001,
                        "The controlling NumberBox should allow the ProgressBar's 0 percent state.");

                    valueBox.Value = 0;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(0.0, progressBar.Value, 0.001,
                        "ProgressBar value should update from the NumberBox at 0 percent.");

                    DemoSampleControl? sample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(control => control.XamlSource.Contains("ProgressBarValue"));
                    Assert.IsNotNull(sample, "Status page should expose the ProgressBar value source sample.");
                    StringAssert.Contains(sample.XamlSource, "x:Name=\"ProgressValueNumberBox\"");
                    StringAssert.Contains(sample.XamlSource, "Minimum=\"0\"");
                    Assert.AreEqual(-1, sample.XamlSource.IndexOf("Minimum=\"1\"", StringComparison.Ordinal),
                        "ProgressBar value source should not keep the stale NumberBox minimum.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryStatusPage_SourceMatchesLiveStepAndRingValues()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    DemoSampleControl? stepSample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(control => control.XamlSource.Contains("ProgressBarSteps"));
                    DemoSampleControl? ringSample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(control => control.XamlSource.Contains("ProgressRings"));
                    Assert.IsNotNull(stepSample, "Status page should expose the step ProgressBar source sample.");
                    Assert.IsNotNull(ringSample, "Status page should expose the ProgressRing source sample.");

                    StringAssert.Contains(stepSample.XamlSource, "Steps=\"10\"");
                    StringAssert.Contains(stepSample.XamlSource, "Text=\"Step 1 of 10\"");
                    Assert.AreEqual(-1, stepSample.XamlSource.IndexOf("Steps=\"5\"", StringComparison.Ordinal),
                        "Step ProgressBar source should match the live ten-step sample.");

                    int pausedRingIndex = ringSample.XamlSource.IndexOf("x:Name=\"PausedProgressRing\"", StringComparison.Ordinal);
                    int errorRingIndex = ringSample.XamlSource.IndexOf("x:Name=\"ErrorProgressRing\"", StringComparison.Ordinal);
                    Assert.IsGreaterThanOrEqualTo(0, pausedRingIndex, "ProgressRing source should include PausedProgressRing.");
                    Assert.IsTrue(errorRingIndex > pausedRingIndex, "ProgressRing source should place ErrorProgressRing after PausedProgressRing.");
                    string pausedRingSource = ringSample.XamlSource.Substring(pausedRingIndex, errorRingIndex - pausedRingIndex);

                    StringAssert.Contains(pausedRingSource, "IsIndeterminate=\"False\"");
                    StringAssert.Contains(pausedRingSource, "ProgressState=\"{x:Static uicore:ProgressRingState.Paused}\"");
                    StringAssert.Contains(pausedRingSource, "Value=\"80\"");
                    StringAssert.Contains(ringSample.XamlSource, "Value=\"80\"");
                    Assert.AreEqual(-1, ringSample.XamlSource.IndexOf("Value=\"70\"", StringComparison.Ordinal),
                        "ProgressRing source should match the live error-state value.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryStatusPage_StepProgressBarAnimatesEdgeClicks()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryStatusPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Controls.ProgressBar? progressBar = FindByName<Controls.ProgressBar>(page, "StepProgressBar");
                    Assert.IsNotNull(progressBar, "Status page should expose the step ProgressBar.");

                    WpfBorder? track = FindByName<WpfBorder>(progressBar, "PART_Track");
                    WpfBorder? fill = FindByName<WpfBorder>(progressBar, "PART_Fill");
                    Assert.IsNotNull(track, "Step ProgressBar should expose PART_Track.");
                    Assert.IsNotNull(fill, "Step ProgressBar should expose PART_Fill.");

                    Controls.Button? backButton = FindStepButton(page, "Back");
                    Controls.Button? nextButton = FindStepButton(page, "Next");
                    Assert.IsNotNull(backButton, "Status page should expose the Back step button.");
                    Assert.IsNotNull(nextButton, "Status page should expose the Next step button.");

                    backButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, backButton));
                    WaitForAnimationAndDrain(window.Dispatcher, 340);

                    AssertStepClickStartsAwayFromTarget(nextButton, progressBar, fill, track, window.Dispatcher, 1, true);
                    WaitForAnimationAndDrain(window.Dispatcher, 340);
                    AssertStepClickStartsAwayFromTarget(nextButton, progressBar, fill, track, window.Dispatcher, 2, true);
                    WaitForAnimationAndDrain(window.Dispatcher, 340);

                    progressBar.CurrentStep = 9;
                    WaitForAnimationAndDrain(window.Dispatcher, 340);
                    AssertStepClickStartsAwayFromTarget(nextButton, progressBar, fill, track, window.Dispatcher, 10, true);
                    WaitForAnimationAndDrain(window.Dispatcher, 340);
                    AssertStepClickStartsAwayFromTarget(backButton, progressBar, fill, track, window.Dispatcher, 9, false);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryNavigationPage_CompactSourceMatchesLiveInteraction()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryNavigationPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    DemoSampleControl? sample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(control => control.XamlSource.Contains("CompactNavigationView"));
                    Assert.IsNotNull(sample, "Navigation page should expose the compact NavigationView source sample.");

                    StringAssert.Contains(sample.XamlSource, "IsBackEnabled=\"{Binding IsChecked, ElementName=BackEnabledToggle}\"");
                    StringAssert.Contains(sample.XamlSource, "IsPaneToggleButtonVisible=\"True\"");
                    Assert.AreEqual(-1, sample.XamlSource.IndexOf("CompactPaneToggleButton", StringComparison.Ordinal),
                        "Compact Navigation source should use the built-in pane toggle only.");
                    Assert.AreEqual(-1, sample.CSharpSource.IndexOf("CompactPaneToggleButton_Click", StringComparison.Ordinal),
                        "Compact Navigation source should not contain a duplicate right-rail pane toggle handler.");
                    StringAssert.Contains(sample.XamlSource, "<ui:NavigationViewItem");
                    StringAssert.Contains(sample.XamlSource, "Content=\"Settings\"");
                    Assert.AreEqual(-1, sample.XamlSource.IndexOf("IsBackEnabled=\"False\"", StringComparison.Ordinal),
                        "Compact Navigation source should not hard-code back availability.");
                    Assert.AreEqual(-1, sample.XamlSource.IndexOf("Footer content", StringComparison.Ordinal),
                        "Compact Navigation source should show the live Settings footer item.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryTabsPage_TabViewContentUsesLayerFillSurface()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryTabsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    TabView? tabView = FindByName<TabView>(page, "DemoTabView");
                    Assert.IsNotNull(tabView, "Tabs page should expose the TabView sample.");

                    foreach (TabViewItem item in tabView.Items.OfType<TabViewItem>())
                    {
                        AssertTabViewItemContentSurface(item);
                    }

                    ButtonBase? addButton = tabView.Template.FindName("PART_AddTabButton", tabView) as ButtonBase;
                    Assert.IsNotNull(addButton, "TabView sample should expose the add-tab button.");
                    addButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, addButton));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(4, tabView.Items.Count, "Adding a document should append a fourth tab.");
                    TabViewItem? selectedTab = tabView.SelectedItem as TabViewItem;
                    Assert.IsNotNull(selectedTab, "Added document should become the selected tab.");
                    AssertTabViewItemContentSurface(selectedTab);

                    DemoSampleControl? sample = FindAllVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(control => control.XamlSource.Contains("TabViewDocuments"));
                    Assert.IsNotNull(sample, "Tabs page should expose the TabView source sample.");
                    StringAssert.Contains(sample.XamlSource, "LayerFillColorDefaultBrush");
                    StringAssert.Contains(sample.CSharpSource, "LayerFillColorDefaultBrush");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryTypographyPage_TableUsesCompactRowSpacing()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryTypographyPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid? table = FindByName<Grid>(page, "TypographyTable");
                    Assert.IsNotNull(table, "Typography page should expose TypographyTable.");

                    WpfTextBlock? firstBodyCell = table.Children
                        .OfType<WpfTextBlock>()
                        .FirstOrDefault(textBlock => Grid.GetRow(textBlock) == 1 && Grid.GetColumn(textBlock) == 0);
                    Assert.IsNotNull(firstBodyCell, "Typography table should include a first body row cell.");
                    Assert.AreEqual(new Thickness(24, 8, 16, 8), firstBodyCell.Margin,
                        "Typography body cells should use reduced vertical row spacing.");

                    WpfBorder? firstShadedRow = table.Children
                        .OfType<WpfBorder>()
                        .FirstOrDefault(border => Grid.GetRow(border) == 1);
                    Assert.IsNotNull(firstShadedRow, "Typography table should include shaded row backgrounds.");
                    Assert.AreEqual(new Thickness(0, 2, 0, 2), firstShadedRow.Margin,
                        "Typography shaded row background should match the compact vertical spacing.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryTypographyPage_DirectTableKeepsCopyColumnWithoutSourceExpander()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryTypographyPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    List<DemoSampleControl> samples = [.. FindAllVisualChildren<DemoSampleControl>(page)];
                    Assert.AreEqual(0, samples.Count, "Typography page should be a direct reference table without a trailing source expander.");

                    Grid? table = FindByName<Grid>(page, "TypographyTable");
                    Assert.IsNotNull(table, "Typography page should expose TypographyTable.");

                    List<Controls.Button> copyButtons = [.. FindAllVisualChildren<Controls.Button>(table)];
                    Assert.IsNotEmpty(copyButtons, "Typography table should keep the copy column in the live table.");
                    Assert.IsTrue(copyButtons.Any(static button => "BodyTextBlockStyle".Equals(button.Tag as string, StringComparison.Ordinal)),
                        "Typography table should keep per-row style-key copy actions.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GallerySettingsPage_UsesFullWidthSettingsRowsForWindowControls()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    WpfBorder? appThemeCard = FindByName<WpfBorder>(page, "AppThemeSettingsCard");
                    WpfBorder? backdropCard = FindByName<WpfBorder>(page, "BackdropSettingsCard");
                    WpfBorder? colorsCard = FindByName<WpfBorder>(page, "ColorsSettingsCard");
                    System.Windows.Controls.ComboBox? backdrop = FindByName<System.Windows.Controls.ComboBox>(page, "BackdropComboBox");
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    System.Windows.Controls.ComboBox? minimize = FindByName<System.Windows.Controls.ComboBox>(page, "MinimizeVisibilityCombo");
                    System.Windows.Controls.ComboBox? maximize = FindByName<System.Windows.Controls.ComboBox>(page, "MaximizeVisibilityCombo");
                    System.Windows.Controls.ComboBox? close = FindByName<System.Windows.Controls.ComboBox>(page, "CloseVisibilityCombo");
                    FrameworkElement? showIcon = FindByName<FrameworkElement>(page, "ShowWindowIconToggle");
                    FrameworkElement? showTitle = FindByName<FrameworkElement>(page, "ShowWindowTitleToggle");

                    Assert.IsNotNull(appThemeCard, "Settings should expose a named app-theme card.");
                    Assert.IsNotNull(backdropCard, "Settings should expose a named backdrop card.");
                    Assert.IsNotNull(colorsCard, "Settings should expose a named colors card.");
                    Assert.IsTrue(appThemeCard.ActualWidth > 700.0, "Settings cards should stretch across the content column.");
                    Assert.AreEqual(appThemeCard.ActualWidth, backdropCard.ActualWidth, 1.0,
                        "Settings row cards should share a consistent stretched width.");
                    Assert.AreEqual(backdropCard.ActualWidth, colorsCard.ActualWidth, 1.0,
                        "The Colors row should align to the Backdrop row width.");
                    Assert.IsNotNull(backdrop, "Backdrop picker should live in Settings.");
                    Assert.IsNotNull(accentRow, "Accent swatches should use a named single-row host.");
                    Assert.IsNotNull(minimize, "Minimize caption picker should exist.");
                    Assert.IsNotNull(maximize, "Maximize caption picker should exist.");
                    Assert.IsNotNull(close, "Close caption picker should exist.");
                    Assert.IsNotNull(showIcon, "Show Icon toggle should exist.");
                    Assert.IsNotNull(showTitle, "Show Title toggle should exist.");
                    Assert.AreEqual(7, accentRow.Children.Count, "The Settings page accent picker should expose seven logo accent swatches.");
                    Assert.AreEqual(GetVisualY(accentRow.Children[0] as FrameworkElement, window) ?? double.MaxValue, GetVisualY(accentRow.Children[6] as FrameworkElement, window) ?? double.MaxValue, 1.0,
                        "All accent swatches should fit on one row.");
                    Assert.IsTrue((GetVisualX(backdrop, window) ?? double.MinValue) > (GetVisualX(appThemeCard, window) ?? double.MinValue) + 500.0,
                        "The Backdrop combo box should stay docked to the right side of its settings card.");
                    Assert.IsTrue((GetVisualY(maximize, window) ?? double.MinValue) > (GetVisualY(minimize, window) ?? double.MinValue),
                        "Caption button customization should use separate settings rows.");
                    Assert.IsTrue((GetVisualY(close, window) ?? double.MinValue) > (GetVisualY(maximize, window) ?? double.MinValue),
                        "Close button customization should appear below Maximize.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GallerySettingsPage_CompactsControlsAtNarrowWidths()
        {
            RunOnSta(delegate
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

                    Assert.IsNotNull(appTheme, "App theme picker should exist.");
                    Assert.IsNotNull(minimize, "Minimize caption picker should exist.");
                    Assert.IsNotNull(accentPanel, "Accent picker host should exist.");
                    Assert.IsNotNull(accentRow, "Accent swatches should use a named host.");
                    Assert.IsNotNull(systemAccent, "System accent button should exist.");
                    Assert.IsNotNull(repositoryActions, "Repository action host should exist.");
                    Assert.IsNotNull(copyRepository, "Copy repository button should exist.");

                    Assert.AreEqual(180.0, appTheme.Width, 0.001,
                        "Narrow Settings width should compact the main picker width.");
                    Assert.AreEqual(140.0, minimize.Width, 0.001,
                        "Narrow Settings width should compact the caption picker width.");
                    Assert.AreEqual(Orientation.Vertical, accentPanel.Orientation,
                        "Narrow Settings width should stack the accent row and system accent button.");
                    Assert.AreEqual(4, accentRow.Columns,
                        "Narrow Settings width should wrap seven accent swatches to two rows.");
                    Assert.AreEqual(2, accentRow.Rows,
                        "Narrow Settings width should reserve a second accent swatch row.");
                    Assert.AreEqual(new Thickness(0, 0, 0, 8), accentRow.Margin,
                        "Narrow Settings width should separate the wrapped accent swatches from the system accent button.");
                    Assert.AreEqual(112.0, systemAccent.MinWidth, 0.001,
                        "Narrow Settings width should keep the system accent button readable.");
                    Assert.AreEqual(Orientation.Vertical, repositoryActions.Orientation,
                        "Narrow Settings width should stack repository actions.");
                    Assert.AreEqual(new Thickness(0, 0, 0, 8), copyRepository.Margin,
                        "Narrow Settings width should separate stacked repository actions.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GallerySettingsPage_RainbowAccentSwatches_PreserveLogoColors()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    Assert.IsNotNull(accentRow, "Accent swatches should use a named single-row host.");

                    string[] expected =
                    [
                        "#E80000",
                        "#F58809",
                        "#F5E70C",
                        "#2BDE11",
                        "#09C4DE",
                        "#AA04DE",
                        "#FF00E8"
                    ];

                    Assert.AreEqual(expected.Length, accentRow.Children.Count,
                        "The Settings page accent picker should expose the seven rainbow swatches.");

                    for (int i = 0; i < expected.Length; i++)
                    {
                        FrameworkElement? swatch = accentRow.Children[i] as FrameworkElement;
                        Assert.IsNotNull(swatch, "Each accent swatch should be a FrameworkElement.");
                        Assert.AreEqual(expected[i], swatch.Tag as string,
                            "The Settings page swatches should stay in rainbow order.");

                        object converted = ColorConverter.ConvertFromString(expected[i]);
                        Assert.IsInstanceOfType(converted, typeof(Color), "Swatch Tag should be a valid color: " + expected[i]);
                    }
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    ApplicationAccentColorManager.ApplyApplicationAccent();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GallerySettingsPage_InvalidAccentSwatchTag_DoesNotChangeAccent()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GallerySettingsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    Assert.IsNotNull(accentRow, "Accent swatches should use a named single-row host.");

                    Controls.Button? swatch = accentRow.Children[0] as Controls.Button;
                    Assert.IsNotNull(swatch, "Accent swatch should be a Fluence button.");

                    Color originalAccent = Color.FromRgb(0x22, 0x44, 0x66);
                    ApplicationAccentColorManager.ApplyCustomAccent(originalAccent);

                    swatch.Tag = "#NotAColor";
                    swatch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, swatch));

                    Assert.AreEqual(originalAccent, ApplicationAccentColorManager.SystemAccentColor,
                        "Invalid accent swatch tags should be ignored without changing the active accent.");
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    ApplicationAccentColorManager.ApplyApplicationAccent();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryAccessibilityPage_KeyboardSamplesUseAlignedRows()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryAccessibilityPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid? primary = FindByName<Grid>(page, "KeyboardSupportPrimaryControls");
                    Assert.IsNotNull(primary, "Accessibility keyboard sample should use a named alignment grid.");
                    Assert.AreEqual(4, primary.ColumnDefinitions.Count,
                        "Primary keyboard sample should have four equal columns.");
                    Assert.AreEqual(2, primary.RowDefinitions.Count,
                        "Primary keyboard sample should have two aligned rows.");
                    Assert.AreEqual(8, primary.Children.Count,
                        "Primary keyboard sample should contain four controls per row.");

                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.Button button && string.Equals(button.Content as string, "Button 1", StringComparison.Ordinal);
                    }, 0, 0, "Button 1");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.Button button && string.Equals(button.Content as string, "Button 2", StringComparison.Ordinal);
                    }, 0, 1, "Button 2");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.TextBox;
                    }, 0, 2, "TextBox");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.ComboBox;
                    }, 0, 3, "ComboBox");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.CheckBox;
                    }, 1, 0, "CheckBox");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is ToggleSwitch;
                    }, 1, 1, "ToggleSwitch");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.Slider;
                    }, 1, 2, "Slider");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is HyperlinkButton;
                    }, 1, 3, "HyperlinkButton");

                    Grid? tabOrder = FindByName<Grid>(page, "KeyboardSupportExplicitOrderControls");
                    Assert.IsNotNull(tabOrder, "Explicit tab order sample should use an alignment grid.");
                    Assert.AreEqual(3, tabOrder.ColumnDefinitions.Count,
                        "Explicit tab order buttons should line up in equal columns.");
                    Assert.AreEqual(3, tabOrder.Children.Count,
                        "Explicit tab order sample should contain three aligned buttons.");
                    Assert.AreEqual(KeyboardNavigationMode.Local, KeyboardNavigation.GetTabNavigation(tabOrder),
                        "Explicit tab order buttons must be a local keyboard navigation group.");

                    HyperlinkButton? hyperlink = FindAllVisualChildren<HyperlinkButton>(primary).FirstOrDefault();
                    Controls.Button? tabOrderFirst = FindByName<Controls.Button>(page, "ExplicitTabOrderFirstButton");
                    Controls.Button? tabOrderSecond = FindByName<Controls.Button>(page, "ExplicitTabOrderSecondButton");
                    Controls.Button? tabOrderThird = FindByName<Controls.Button>(page, "ExplicitTabOrderThirdButton");
                    Assert.IsNotNull(hyperlink, "Primary keyboard sample should end with a focusable hyperlink button.");
                    AssertTabOrderButton(tabOrderFirst, 1, "Tab order: 1 (first)");
                    AssertTabOrderButton(tabOrderSecond, 2, "Tab order: 2");
                    AssertTabOrderButton(tabOrderThird, 3, "Tab order: 3");
                    AssertNextFocus(window, hyperlink, tabOrderFirst, "Tab should enter the explicit tab-order group after the preceding hyperlink.");
                    AssertNextFocus(window, tabOrderFirst, tabOrderSecond, "Explicit tab-order group should move from 1 to 2.");
                    AssertNextFocus(window, tabOrderSecond, tabOrderThird, "Explicit tab-order group should move from 2 to 3.");

                    List<DemoSampleControl> samples = [.. FindAllVisualChildren<DemoSampleControl>(page)];
                    Assert.AreEqual(4, samples.Count,
                        "Accessibility page should expose each discrete sample through DemoSampleControl.");
                    Assert.IsTrue(samples.All(sample => !string.IsNullOrWhiteSpace(sample.XamlSource)),
                        "Every accessibility sample should have inline XAML source.");
                    Assert.IsNull(FindByName<FrameworkElement>(page, "FocusAndTabOrderSourceLink"),
                        "Accessibility page should not keep legacy SourceLink placeholders.");
                    Assert.IsNull(FindByName<FrameworkElement>(page, "HighContrastMappingSourceLink"),
                        "Accessibility page should not keep legacy SourceLink placeholders.");
                    Assert.IsNull(FindByName<FrameworkElement>(page, "AutomationPropertiesSourceLink"),
                        "Accessibility page should not keep legacy SourceLink placeholders.");
                    Assert.IsNull(FindByName<FrameworkElement>(page, "RtlLayoutSourceLink"),
                        "Accessibility page should not keep legacy SourceLink placeholders.");

                    ToggleSwitch? rtlToggle = FindByName<ToggleSwitch>(page, "RtlToggle");
                    Card? rtlCard = FindByName<Card>(page, "RtlDemoCard");
                    Assert.IsNotNull(rtlToggle, "RTL sample should expose the toggle.");
                    Assert.IsNotNull(rtlCard, "RTL sample should expose the demo card.");
                    Assert.IsTrue(rtlToggle.IsChecked, "Accessibility RTL should be enabled by default.");
                    Assert.AreEqual(FlowDirection.RightToLeft, rtlCard.FlowDirection,
                        "Accessibility RTL demo card should default to mirrored layout.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryIconsPage_IconCatalogIsScrollableAndVirtualized()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryIconsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    FluenceListView? list = FindByName<FluenceListView>(page, "IconCatalogList");
                    Assert.IsNotNull(list, "Icon catalog list must exist.");
                    Assert.IsTrue(list.Items.Count > 100, "Icon catalog must load enough rows to exercise virtualization.");

                    WpfBorder? catalogCard = FindByName<WpfBorder>(page, "IconCatalogCard");
                    Assert.IsNotNull(catalogCard, "Icon catalog should be hosted in a shared card surface.");
                    Assert.AreEqual(new Thickness(16), catalogCard.Padding,
                        "Icon catalog card should use the shared demo sample card padding.");
                    Assert.AreEqual(new CornerRadius(8), catalogCard.CornerRadius,
                        "Icon catalog card should use the same 8px corner radius as other demo surfaces.");
                    Assert.AreEqual(new Thickness(1), catalogCard.BorderThickness,
                        "Icon catalog card should keep the standard 1px card stroke.");
                    AssertIconBrush(catalogCard.Background, "CardBackgroundFillColorDefaultBrush",
                        "Icon catalog card should use the shared section card background.");
                    AssertIconBrush(catalogCard.BorderBrush, "CardStrokeColorDefaultBrush",
                        "Icon catalog card should use the shared card stroke.");
                    Assert.AreEqual(new Thickness(0), list.BorderThickness,
                        "Icon catalog ListView should let the surrounding card own the stroke.");

                    ScrollViewer? viewer = FindVisualChild<ScrollViewer>(list);
                    Assert.IsNotNull(viewer, "Icon catalog list must own a ScrollViewer.");
                    Assert.IsTrue(viewer.ViewportHeight > 0, "Icon catalog needs a bounded viewport height.");
                    Assert.IsTrue(viewer.ExtentHeight > viewer.ViewportHeight, "Icon catalog should have a scrollable extent.");
                    Assert.IsTrue(viewer.ScrollableHeight > 0, "Icon catalog should be scrollable.");

                    int realizedBeforeScroll = CountVisualChildren<ListViewItem>(list);
                    Assert.IsTrue(realizedBeforeScroll > 0, "Initial viewport should realize some row containers.");
                    Assert.IsLessThan(list.Items.Count / 2, realizedBeforeScroll, "Initial layout should not realize most icon rows.");
                    Assert.IsNull(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1), "Last row should stay unrealized before scrolling.");

                    list.ScrollIntoView(list.Items[list.Items.Count - 1]);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsNotNull(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1), "Last row should realize after scrolling into view.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void AssertTabViewItemContentSurface(TabViewItem item)
        {
            WpfBorder? surface = item.Content as WpfBorder;
            Assert.IsNotNull(surface, "TabView document content should use a layer-fill surface.");
            AssertIconBrush(surface.Background, "LayerFillColorDefaultBrush",
                "TabView document content should use LayerFillColorDefaultBrush.");
        }

        private static void AssertIconBrush(Brush? actualBrush, string resourceKey, string message)
        {
            SolidColorBrush? actual = actualBrush as SolidColorBrush;
            Assert.IsNotNull(actual, message + " Actual brush must be a SolidColorBrush.");

            SolidColorBrush? expected = Application.Current?.TryFindResource(resourceKey) as SolidColorBrush;
            Assert.IsNotNull(expected, resourceKey + " must resolve.");
            Assert.AreEqual(expected.Color, actual.Color, message);
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
                    WpfButton? copy = FindByName<WpfButton>(tab.Content as DependencyObject, "CopySourceButton");
                    Assert.IsNotNull(copy, "Source tab should expose a copy button: " + expectedHeader);
                    Assert.AreEqual(expectedSource, copy.Tag as string, "Copy button should keep the in-memory source text.");
                    return;
                }
            }

            Assert.Fail("Missing source tab: " + expectedHeader);
        }

        private static string GetSourceTabText(TabControl? tabs, string expectedHeader)
        {
            Assert.IsNotNull(tabs, "Source tabs should exist.");
            foreach (object item in tabs.Items)
            {
                if (item is TabItem tab && string.Equals(tab.Header as string, expectedHeader, StringComparison.Ordinal))
                {
                    RichTextBox? viewer = FindByName<RichTextBox>(tab.Content as DependencyObject, "SourceTextViewer");
                    Assert.IsNotNull(viewer, "Source tab should expose a RichTextBox viewer: " + expectedHeader);
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
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application?.Resources.MergedDictionaries.Add(demoShared);
        }

        private static void AssertProjectUsesIcon(string projectDirectory, string projectFile, string iconPath)
        {
            string project = ReadRepositoryFile(projectDirectory, projectFile);
            StringAssert.Contains(project, "<ApplicationIcon>" + iconPath + "</ApplicationIcon>");
            StringAssert.Contains(project, "<Resource Include=\"" + iconPath + "\" />");
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
            Assert.IsTrue(File.Exists(path), "Repository file must be readable at: " + path);
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
                ShowInTaskbar = false
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
                Content = content
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
            Assert.IsNotNull(nav, "DemoNav must exist.");

            Assert.IsNotNull(nav.SelectedItem as NavigationViewItem, "A NavigationViewItem should be selected.");
            return nav.Content;
        }

        private static void InvokeTitleBarBack(TitleBar titleBar)
        {
            WpfButton? backButton = FindByName<WpfButton>(titleBar, "PART_BackButton");
            Assert.IsNotNull(backButton, "TitleBar should expose a back button.");
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
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        private static void WaitForAnimationAndDrain(Dispatcher dispatcher, int milliseconds)
        {
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(milliseconds)
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
                    Assert.AreEqual(expectedRow, Grid.GetRow(child), name + " should be in the expected row.");
                    Assert.AreEqual(expectedColumn, Grid.GetColumn(child), name + " should be in the expected column.");
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

            Assert.AreEqual(expectedContent, button.Content as string, "Explicit tab-order button content should match.");
            Assert.AreEqual(expectedTabIndex, button.TabIndex, "Explicit tab-order button should keep its documented TabIndex.");
            Assert.IsTrue(button.Focusable, "Explicit tab-order button should accept keyboard focus.");
            Assert.IsTrue(button.IsTabStop, "Explicit tab-order button should participate in keyboard tab navigation.");
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

            Assert.IsTrue(moved, "Keyboard focus should move to the next tab stop. " + message);
            Assert.AreSame(expected, Keyboard.FocusedElement, message);
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

            Assert.AreEqual(expectedStep, progressBar.CurrentStep, "Step button should update the current step.");
            double targetWidth = track.ActualWidth * expectedStep / progressBar.Steps;
            double animatedWidth = fill.Width;
            if (forward)
            {
                Assert.IsLessThan(
                    targetWidth, animatedWidth,
                    string.Format(CultureInfo.InvariantCulture,
                        "Forward step animation should start before the target width. Animated={0}, Target={1}, Step={2}.",
                        animatedWidth,
                        targetWidth,
                        expectedStep));
            }
            else
            {
                Assert.IsTrue(
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
            if (root is FrameworkElement element)
            {
                if (element.FindName(name) is T named)
                {
                    return named;
                }
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
            public string Tag { get; private set; } = tag;

            public Type PageType { get; private set; } = pageType;
        }
    }
}
