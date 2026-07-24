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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryHomePage : UserControl
    {
        public GalleryHomePage()
        {
            InitializeComponent();

            // The theme manager events are static, so scope the subscription to
            // Loaded/Unloaded (the FluenceWindow lifetime pattern) instead of the
            // constructor, keeping abandoned page instances collectable.
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // Resolve the hero for the active theme immediately: the XAML default is
            // the light lockup, and correcting it only on Loaded would let a dark
            // theme's first layout pass measure the wrong image.
            UpdateHeroImage();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Remove-before-add keeps the subscription idempotent if Loaded fires
            // again without an intervening Unloaded (re-hosting scenarios).
            ApplicationThemeManager.Changed -= OnThemeChanged;
            ApplicationThemeManager.Changed += OnThemeChanged;
            UpdateHeroImage();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Changed -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            UpdateHeroImage();
        }

        // Swaps the hero lockup to the variant drawn for the active theme: the light
        // artwork (near-black wordmark) on light themes, the dark artwork (white
        // wordmark) on dark themes. SetResourceReference keeps the swap dynamic so a
        // later theme dictionary replacement re-resolves without another lookup here.
        private void UpdateHeroImage()
        {
            BrandHeroImage.SetResourceReference(
                System.Windows.Controls.Image.SourceProperty,
                UseDarkHeader() ? "FluenceHeaderDarkDrawingImage" : "FluenceHeaderLightDrawingImage");
        }

        // High contrast has no fixed polarity (Aquatic is white-on-black, Desert is
        // black-on-white), so under high contrast the variant follows the live system
        // window luminance. The plain themes map directly.
        private static bool UseDarkHeader()
        {
            ApplicationTheme theme = ApplicationThemeManager.CurrentTheme;
            if (theme is ApplicationTheme.HighContrast)
            {
                System.Windows.Media.Color window = SystemColors.WindowColor;
                double luminance = (0.299 * window.R) + (0.587 * window.G) + (0.114 * window.B);
                return luminance < 128.0;
            }

            return theme is ApplicationTheme.Dark;
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
