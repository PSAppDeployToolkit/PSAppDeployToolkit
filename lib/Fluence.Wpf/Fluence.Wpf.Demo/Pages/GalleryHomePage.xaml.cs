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
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryHomePage : UserControl
    {
        private static readonly Uri LightBannerUri =
            new("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-light.png", UriKind.Absolute);

        private static readonly Uri DarkBannerUri =
            new("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-dark.png", UriKind.Absolute);

        private Uri? _currentBannerUri;

        public GalleryHomePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            UpdateBrandBanner();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;
            UpdateBrandBanner();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
        }

        private void ApplicationThemeManager_Changed(object? sender, ThemeChangedEventArgs e)
        {
            UpdateBrandBanner(e.Theme);
        }

        private void UpdateBrandBanner()
        {
            ApplicationTheme theme = ApplicationThemeManager.CurrentTheme;
            if (theme is ApplicationTheme.Light or ApplicationTheme.Dark or ApplicationTheme.HighContrast)
            {
                UpdateBrandBanner(theme);
                return;
            }

            UpdateBrandBanner(IsCurrentBackgroundDark() ? ApplicationTheme.Dark : ApplicationTheme.Light);
        }

        private void UpdateBrandBanner(ApplicationTheme theme)
        {
            Uri bannerUri = theme is ApplicationTheme.Light or ApplicationTheme.HighContrast ? LightBannerUri : DarkBannerUri;
            if (Equals(_currentBannerUri, bannerUri))
            {
                return;
            }

            BrandBannerImage.Source = new BitmapImage(bannerUri);
            BrandBannerImage.Tag = bannerUri.OriginalString;
            _currentBannerUri = bannerUri;
        }

        private static bool IsCurrentBackgroundDark()
        {
            Application app = Application.Current;
            if (app?.TryFindResource("SolidBackgroundFillColorBaseBrush") is not SolidColorBrush brush)
            {
                return ApplicationThemeManager.CurrentTheme != ApplicationTheme.Light;
            }

            Color color = brush.Color;
            double red = color.R / 255.0;
            double green = color.G / 255.0;
            double blue = color.B / 255.0;
            return ((red * 0.2126) + (green * 0.7152) + (blue * 0.0722)) < 0.5;
        }

        // Handles a click on any featured-control or action Card tile; reads the Card's
        // Tag string and routes to the matching gallery page via host.NavigateTo(tag).
        private void Card_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Card card)
            {
                return;
            }

            if (card.Tag is not string tag || string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (Window.GetWindow(this) is MainWindow host)
            {
                host.NavigateTo(tag);
            }
        }

        private void GitHubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
