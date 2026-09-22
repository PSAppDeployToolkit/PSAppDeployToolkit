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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryHomePage"/>.
    /// </summary>
    public sealed class GalleryHomePageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        [Fact]
        public Task GalleryHomePage_HeroSwapsHeaderLockupWithThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Image image = Assert.IsType<Image>(DemoTestHost.FindByName<Image>(page, "BrandHeroImage"), exactMatch: false);

                    DrawingImage light = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderLightDrawingImage"));
                    DrawingImage dark = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderDarkDrawingImage"));

                    // The hero shows the lockup drawn for the active theme and swaps on
                    // theme changes via the page's ThemeDictionary (no code-behind).
                    Assert.Same(light, image.Source);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(dark, image.Source);

                    // High contrast has no fixed polarity, so the page picks whichever
                    // variant reads against the live system window color.
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(ReferenceEquals(image.Source, light) || ReferenceEquals(image.Source, dark),
                        "High contrast should show one of the two header lockups.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(light, image.Source);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryHomePage_UsesProminentBrandAndAccessibleSocialLinksAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Image brand = Find<Image>(page, "BrandHeroImage");
                    Assert.InRange(brand.ActualWidth, 560, 640);
                    Assert.Equal(Stretch.Uniform, brand.Stretch);
                    Assert.Equal("Fluence.WPF", AutomationProperties.GetName(brand), StringComparer.Ordinal);

                    (string Name, string Destination, string AccessibleName)[] links =
                    [
                        ("GitHubLink", "https://github.com/sintaxasn/fluence.wpf", "Fluence.WPF on GitHub"),
                        ("LinkedInLink", "https://linkedin.com/in/sintaxasn", "Dan Cunningham on LinkedIn"),
                    ];
                    foreach ((string name, string destination, string accessibleName) in links)
                    {
                        Controls.HyperlinkButton link = Find<Controls.HyperlinkButton>(page, name);
                        Assert.Equal(destination, link.NavigateUri.AbsoluteUri, StringComparer.Ordinal);
                        Assert.Equal(accessibleName, AutomationProperties.GetName(link), StringComparer.Ordinal);
                        Assert.Equal(accessibleName, link.ToolTip);
                        Assert.True(link.Focusable);
                        Assert.InRange(link.ActualWidth, 36, 48);
                        Assert.InRange(link.ActualHeight, 36, 48);
                        System.Windows.Shapes.Path icon = Assert.IsType<System.Windows.Shapes.Path>(link.Icon);
                        Assert.False(icon.Data.IsEmpty());
                        Assert.True(icon.IsVisible);
                        Assert.InRange(icon.ActualWidth, 17.0, 19.0);
                        Assert.InRange(icon.ActualHeight, 17.0, 19.0);
                        RenderTargetBitmap renderedIcon = new(18, 18, 96, 96, PixelFormats.Pbgra32);
                        renderedIcon.Render(icon);
                        byte[] pixels = new byte[18 * 18 * 4];
                        renderedIcon.CopyPixels(pixels, 18 * 4, 0);
                        Assert.Contains(pixels, static channel => channel > 0);
                        Assert.Equal(link.Foreground, icon.Fill);
                        Assert.True(link.TranslatePoint(default, page).Y >= brand.TranslatePoint(default, page).Y + brand.ActualHeight);
                    }

                    Assert.Null(DemoTestHost.FindByName<FrameworkElement>(page, "HeroPreviewCard"));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task HomeActions_NavigateToTheirControlPagesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView navigation = Find<Controls.NavigationView>(window, "DemoNav");
                    Frame frame = Assert.IsType<Frame>(navigation.Content, exactMatch: false);
                    (string Route, Type PageType)[] destinations =
                    [
                        ("menus", typeof(GalleryMenusPage)),
                        ("trees", typeof(GalleryTreesPage)),
                        ("settings", typeof(GallerySettingsPage)),
                    ];
                    foreach ((string route, Type pageType) in destinations)
                    {
                        window.NavigateTo("home");
                        Settle(window);
                        GalleryHomePage home = Assert.IsType<GalleryHomePage>(frame.Content);
                        Controls.Card card = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Card>(home),
                            candidate => string.Equals(candidate.Tag as string, route, StringComparison.Ordinal));
                        Assert.True(card.IsClickable);
                        Invoke(card);
                        Settle(window);
                        Assert.Equal(pageType, frame.Content.GetType());
                    }

                    window.NavigateTo("home");
                    Settle(window);
                    GalleryHomePage page = Assert.IsType<GalleryHomePage>(frame.Content);
                    Invoke(Find<Controls.Button>(page, "ExploreControlsButton"));
                    Settle(window);
                    _ = Assert.IsType<GalleryButtonsPage>(frame.Content);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task HomeLayout_ReflowsWithoutHorizontalOverflowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Image brand = Find<Image>(page, "BrandHeroImage");
                    UniformGrid catalog = Find<UniformGrid>(page, "FeaturedControlsGrid");
                    UniformGrid foundations = Find<UniformGrid>(page, "FoundationLinksGrid");
                    Controls.SmoothScrollViewer scroll = Assert.Single(DemoTestHost.FindVisualChildren<Controls.SmoothScrollViewer>(page));
                    (double Width, int Columns)[] sizes = [(1100, 3), (720, 2), (460, 1), (1100, 3)];
                    foreach ((double width, int columns) in sizes)
                    {
                        window.Width = width;
                        Settle(window);
                        Assert.Equal(columns, catalog.Columns);
                        Assert.Equal(columns is 3 ? 3 : 1, foundations.Columns);
                        Assert.InRange(brand.ActualWidth, 1, 640);
                        Assert.True(brand.ActualWidth <= scroll.ViewportWidth);
                        Assert.True(scroll.ExtentWidth <= scroll.ViewportWidth + 1,
                            "The home page must fit the viewport without horizontal clipping.");
                        if (columns is 3)
                        {
                            Assert.InRange(brand.ActualWidth, 560, 640);
                        }
                    }
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        private static T Find<T>(DependencyObject root, string name)
            where T : FrameworkElement
        {
            return Assert.IsType<T>(DemoTestHost.FindByName<T>(root, name), exactMatch: false);
        }

        private static void Invoke(UIElement element)
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(element);
            IInvokeProvider provider = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
            provider.Invoke();
            WpfTestSta.DrainDispatcher(element.Dispatcher);
        }

        private static void Settle(Window window)
        {
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
        }
    }
}
