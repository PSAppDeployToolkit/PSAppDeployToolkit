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
        public Task GalleryHomePage_HeroSwapsSvgDerivedBannerWithThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Image image = Find<Image>(page, "BrandHeroImage");
                    DrawingImage banner = Assert.IsType<DrawingImage>(image.Source);
                    Assert.Equal(new Rect(0, 0, 4800, 2521), banner.Drawing.Bounds);
                    DrawingImage brandIcon = Assert.IsType<DrawingImage>(
                        Application.Current.TryFindResource("FluenceIconBrandDrawingImage"));
                    Assert.InRange(brandIcon.Drawing.Bounds.Width, 1024, 1025);
                    Assert.Equal(1024, brandIcon.Drawing.Bounds.Height);

                    // The hero uses native WPF drawings generated from the supplied
                    // SVG banners and swaps via the page's ThemeDictionary.
                    Assert.Same(page.TryFindResource("HomeBannerLightDrawingImage"), banner);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(page.TryFindResource("HomeBannerDarkDrawingImage"), image.Source);

                    // High contrast has no fixed polarity, so the page picks whichever
                    // variant reads against the live system window color.
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Color background = SystemColors.WindowColor;
                    double luminance = (0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B);
                    Assert.Same(page.TryFindResource(luminance < 128.0 ? "HomeBannerDarkDrawingImage" : "HomeBannerLightDrawingImage"), image.Source);

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(page.TryFindResource("HomeBannerLightDrawingImage"), image.Source);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryHomePage_UsesProminentAccessibleBrandAsync()
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
                    Assert.Equal("Fluence.WPF banner", AutomationProperties.GetName(brand), StringComparer.Ordinal);
                    Assert.Contains(".NET 4.7.2, 8, and 10 or later", AutomationProperties.GetHelpText(brand), StringComparison.Ordinal);
                    Assert.True(brand.IsVisible);
                    Assert.InRange(brand.ActualHeight, 310, 340);
                    RenderTargetBitmap renderedBrand = new(620, 326, 96, 96, PixelFormats.Pbgra32);
                    renderedBrand.Render(brand);
                    byte[] pixels = new byte[620 * 326 * 4];
                    renderedBrand.CopyPixels(pixels, 620 * 4, 0);
                    Assert.True(HasVisiblePixels(pixels, 620, 210, 20, 410, 150), "The embedded fan should render.");
                    Assert.True(HasVisiblePixels(pixels, 620, 100, 150, 520, 215), "The Fluence.WPF wordmark should render.");
                    Assert.True(HasVisiblePixels(pixels, 620, 5, 260, 615, 325), "The banner support line should render.");
                    UniformGrid catalog = Find<UniformGrid>(page, "FeaturedControlsGrid");
                    Assert.True(catalog.TranslatePoint(default, page).Y >= brand.TranslatePoint(default, page).Y + brand.ActualHeight);

                    (string Route, string FileName)[] artwork =
                    [
                        ("buttons", "Button.png"),
                        ("selection", "Checkbox.png"),
                        ("inputs", "TextBox.png"),
                        ("navigation", "NavigationView.png"),
                        ("tabs", "TabView.png"),
                        ("menus", "MenuFlyout.png"),
                        ("data", "ListView.png"),
                        ("trees", "TreeView.png"),
                        ("status", "InfoBar.png"),
                    ];
                    foreach ((string route, string fileName) in artwork)
                    {
                        Controls.Card card = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Card>(catalog),
                            candidate => string.Equals(candidate.Tag as string, route, StringComparison.Ordinal));
                        Image illustration = Assert.IsType<Image>(card.Icon);
                        BitmapSource bitmap = Assert.IsType<BitmapSource>(illustration.Source, exactMatch: false);
                        BitmapSource expected = BitmapFrame.Create(new Uri(
                            "pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/" + fileName,
                            UriKind.Absolute));
                        Assert.Equal(expected.PixelWidth, bitmap.PixelWidth);
                        Assert.Equal(expected.PixelHeight, bitmap.PixelHeight);
                        Assert.Equal(expected.Format, bitmap.Format);
                        int stride = ((bitmap.PixelWidth * bitmap.Format.BitsPerPixel) + 7) / 8;
                        byte[] actualPixels = new byte[stride * bitmap.PixelHeight];
                        byte[] expectedPixels = new byte[stride * expected.PixelHeight];
                        bitmap.CopyPixels(actualPixels, stride, 0);
                        expected.CopyPixels(expectedPixels, stride, 0);
                        Assert.Equal(expectedPixels, actualPixels);
                        Assert.InRange(illustration.ActualWidth, 47, 49);
                        Assert.InRange(illustration.ActualHeight, 47, 49);
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
                    Controls.Card buttonCard = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Card>(page),
                        static candidate => string.Equals(candidate.Tag as string, "buttons", StringComparison.Ordinal));
                    Assert.True(buttonCard.IsClickable);
                    Invoke(buttonCard);
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
                        Assert.InRange(brand.ActualHeight / brand.ActualWidth, 0.52, 0.53);
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

        private static bool HasVisiblePixels(byte[] pixels, int stridePixels, int left, int top, int right, int bottom)
        {
            for (int y = top; y < bottom; y++)
            {
                for (int x = left; x < right; x++)
                {
                    int pixelIndex = (y * stridePixels) + x;
                    if (pixels[(pixelIndex * 4) + 3] > 20)
                    {
                        return true;
                    }
                }
            }

            return false;
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
