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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Shared gallery page header modelled on the WinUI 3 Gallery's <c language="text">Controls/PageHeader.xaml</c>:
    /// the page title on its own row, then an action row with the Documentation and Source drop-downs
    /// on the left and the theme toggle, copy-link and favorite buttons on the right.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DocsAnchor"/> and <see cref="DocsDocument"/> build the Documentation link
    /// (<c language="text">docs/&lt;DocsDocument&gt;#&lt;DocsAnchor&gt;</c> on the repository's main branch); the
    /// drop-down is hidden while the anchor is empty. The Source drop-down always offers the hosting
    /// page's XAML and code-behind on GitHub, resolved from the page type the header is placed in,
    /// plus the optional <see cref="ControlSourcePath"/> for the library file the page demonstrates.
    /// The copy-link button copies the Documentation link when there is one, else the page XAML link.
    /// </para>
    /// </remarks>
    public partial class GalleryPageHeader : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(GalleryPageHeader),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="DocsAnchor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocsAnchorProperty =
            DependencyProperty.Register(
                nameof(DocsAnchor),
                typeof(string),
                typeof(GalleryPageHeader),
                new FrameworkPropertyMetadata(string.Empty, OnLinksChanged));

        /// <summary>
        /// Identifies the <see cref="DocsDocument"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocsDocumentProperty =
            DependencyProperty.Register(
                nameof(DocsDocument),
                typeof(string),
                typeof(GalleryPageHeader),
                new FrameworkPropertyMetadata("controls.md", OnLinksChanged));

        /// <summary>
        /// Identifies the <see cref="ControlSourcePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ControlSourcePathProperty =
            DependencyProperty.Register(
                nameof(ControlSourcePath),
                typeof(string),
                typeof(GalleryPageHeader),
                new FrameworkPropertyMetadata(string.Empty, OnLinksChanged));

        private const string RepositoryBlobRoot = "sintaxasn/Fluence.Wpf/blob/main/";
        private const string PagesFolder = "Fluence.Wpf.Demo/Pages/";

        private static readonly Uri RepositoryBlobBaseUri = new UriBuilder("https", "github.com", -1, RepositoryBlobRoot).Uri;

        /// <summary>
        /// Initializes a new instance of the <see cref="GalleryPageHeader"/> class.
        /// </summary>
        public GalleryPageHeader()
        {
            InitializeComponent();
            UpdateLinks();
            UpdateFavoriteState(isFavorite: false);
            Loaded += GalleryPageHeader_Loaded;
            Unloaded += GalleryPageHeader_Unloaded;
        }

        /// <summary>
        /// Gets or sets the page title.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the heading slug inside <see cref="DocsDocument"/> the Documentation link opens.
        /// Empty hides the Documentation drop-down.
        /// </summary>
        public string DocsAnchor
        {
            get => (string)GetValue(DocsAnchorProperty);
            set => SetValue(DocsAnchorProperty, value);
        }

        /// <summary>
        /// Gets or sets the file under <c language="text">docs/</c> the Documentation link opens. Defaults to <c language="text">controls.md</c>.
        /// </summary>
        public string DocsDocument
        {
            get => (string)GetValue(DocsDocumentProperty);
            set => SetValue(DocsDocumentProperty, value);
        }

        /// <summary>
        /// Gets or sets the repository-relative path of the library source the page demonstrates,
        /// for example <c language="text">Fluence.Wpf/Themes/Controls/Button.xaml</c>. Empty hides the section.
        /// </summary>
        public string ControlSourcePath
        {
            get => (string)GetValue(ControlSourcePathProperty);
            set => SetValue(ControlSourcePathProperty, value);
        }

        /// <summary>
        /// Gets the Documentation link, or <see langword="null"/> when <see cref="DocsAnchor"/> is empty.
        /// </summary>
        public Uri? DocumentationUri =>
            string.IsNullOrWhiteSpace(DocsAnchor)
                ? null
                : new Uri(RepositoryBlobBaseUri, "docs/" + DocsDocument + "#" + DocsAnchor);

        /// <summary>
        /// Gets the GitHub link to the hosting page's XAML, or <see langword="null"/> when the header is not inside a gallery page.
        /// </summary>
        public Uri? PageXamlUri { get; private set; }

        private static void OnLinksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GalleryPageHeader header)
            {
                header.UpdateLinks();
            }
        }

        private static string DescribeDocument(string document)
        {
            return document switch
            {
                "controls.md" => "Control reference",
                "theming.md" => "Theming guide",
                "getting-started.md" => "Getting started",
                _ => document,
            };
        }

        private void GalleryPageHeader_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateThemeToggleEnabled();
            ResolvePageSourceLinks();
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;
        }

        private void GalleryPageHeader_Unloaded(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
        }

        private void ApplicationThemeManager_Changed(object? sender, ThemeChangedEventArgs e)
        {
            UpdateThemeToggleEnabled();
        }

        private void UpdateThemeToggleEnabled()
        {
            ThemeToggleButton.IsEnabled = ApplicationThemeManager.ResolvedTheme is not ApplicationTheme.HighContrast;
        }

        private void UpdateLinks()
        {
            Uri? docs = DocumentationUri;
            DocsDropDownButton.Visibility = docs is null ? Visibility.Collapsed : Visibility.Visible;
            DocsLink.Content = DescribeDocument(DocsDocument);
            DocsLink.ToolTip = docs?.AbsoluteUri;
            if (docs is not null)
            {
                DocsLink.NavigateUri = docs;
            }

            bool hasControlSource = !string.IsNullOrWhiteSpace(ControlSourcePath);
            ControlSourcePanel.Visibility = hasControlSource ? Visibility.Visible : Visibility.Collapsed;
            if (hasControlSource)
            {
                Uri controlSource = new(RepositoryBlobBaseUri, ControlSourcePath.Replace('\\', '/'));
                ControlSourceLink.Content = System.IO.Path.GetFileName(ControlSourcePath) ?? ControlSourcePath;
                ControlSourceLink.ToolTip = controlSource.AbsoluteUri;
                ControlSourceLink.NavigateUri = controlSource;
            }
        }

        // The header is placed inside a gallery Page; its type name is the page file name, so the
        // GitHub links to the page XAML and code-behind can be derived rather than declared. The
        // header itself is a UserControl, and so are the sample controls it can sit beside, so the
        // walk looks for a Page in this namespace rather than any FrameworkElement.
        private void ResolvePageSourceLinks()
        {
            DependencyObject? current = VisualTreeHelper.GetParent(this);
            Page? page = null;
            while (current is not null)
            {
                if (current is Page candidate &&
                    string.Equals(candidate.GetType().Namespace, typeof(GalleryPageHeader).Namespace, StringComparison.Ordinal))
                {
                    page = candidate;
                    break;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            if (page is null)
            {
                PageXamlUri = null;
                PageSourcePanel.Visibility = Visibility.Collapsed;
                return;
            }

            string pageFile = PagesFolder + page.GetType().Name + ".xaml";
            PageXamlUri = new Uri(RepositoryBlobBaseUri, pageFile);
            Uri pageCode = new(RepositoryBlobBaseUri, pageFile + ".cs");
            PageXamlLink.NavigateUri = PageXamlUri;
            PageXamlLink.ToolTip = PageXamlUri.AbsoluteUri;
            PageCodeLink.NavigateUri = pageCode;
            PageCodeLink.ToolTip = pageCode.AbsoluteUri;
            PageSourcePanel.Visibility = Visibility.Visible;
        }

        private void CopyLinkButton_Click(object sender, RoutedEventArgs e)
        {
            Uri? link = DocumentationUri ?? PageXamlUri;
            if (link is not null)
            {
                DemoClipboard.SetText(link.AbsoluteUri);
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationThemeManager.ResolvedTheme is ApplicationTheme.HighContrast)
            {
                return;
            }

            MainWindow? owner = Application.Current?.MainWindow as MainWindow;
            ApplicationTheme next = ApplicationThemeManager.ResolvedTheme is ApplicationTheme.Dark
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;
            ApplicationThemeManager.Apply(next, owner?.SystemBackdropType ?? WindowBackdropType.Auto);
        }

        private void FavoriteToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateFavoriteState(FavoriteToggleButton.IsChecked is true);
        }

        private void UpdateFavoriteState(bool isFavorite)
        {
            FavoriteIcon.Glyph = isFavorite ? "\uE735" : "\uE734";
            string text = isFavorite ? "Remove from favorites" : "Add to favorites";
            AutomationProperties.SetName(FavoriteToggleButton, text);
            FavoriteToggleButton.ToolTip = text;
        }
    }
}
