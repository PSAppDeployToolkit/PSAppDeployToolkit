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

using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Inline, syntax-highlighted code snippet, mirroring the WinUI 3 Gallery
    /// <c language="csharp">SampleCodePresenter</c> in its inline state: colorized code directly on the
    /// page with no plate, and an optional copy button.
    /// </summary>
    /// <remarks>
    /// The viewer comes from <see cref="DemoSourceHighlighter"/>, so a snippet here and the source
    /// expander on a <see cref="DemoSampleControl"/> colorize identically. Font size follows the
    /// control's <see cref="Control.FontSize"/>; the line height and document padding come from
    /// the <c language="xaml">DemoCodePresenterLineHeight</c> and <c language="xaml">DemoCodePresenterPadding</c>
    /// demo tokens.
    /// </remarks>
    public partial class DemoCodePresenter : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Code"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CodeProperty =
            DependencyProperty.Register(
                nameof(Code),
                typeof(string),
                typeof(DemoCodePresenter),
                new FrameworkPropertyMetadata(string.Empty, OnPresentationChanged));

        /// <summary>
        /// Identifies the <see cref="CodeLanguage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CodeLanguageProperty =
            DependencyProperty.Register(
                nameof(CodeLanguage),
                typeof(DemoSourceLanguage),
                typeof(DemoCodePresenter),
                new FrameworkPropertyMetadata(DemoSourceLanguage.Xaml, OnPresentationChanged));

        /// <summary>
        /// Identifies the <see cref="IsCopyButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCopyButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsCopyButtonVisible),
                typeof(bool),
                typeof(DemoCodePresenter),
                new FrameworkPropertyMetadata(defaultValue: true, OnPresentationChanged));

        private const string LineHeightKey = "DemoCodePresenterLineHeight";
        private const string PaddingKey = "DemoCodePresenterPadding";

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoCodePresenter"/> class.
        /// </summary>
        public DemoCodePresenter()
        {
            InitializeComponent();
            Loaded += DemoCodePresenter_Loaded;
        }

        /// <summary>
        /// Gets or sets the snippet text.
        /// </summary>
        public string Code
        {
            get => (string)GetValue(CodeProperty);
            set => SetValue(CodeProperty, value);
        }

        /// <summary>
        /// Gets or sets the source language used to colorize <see cref="Code"/>.
        /// </summary>
        public DemoSourceLanguage CodeLanguage
        {
            get => (DemoSourceLanguage)GetValue(CodeLanguageProperty);
            set => SetValue(CodeLanguageProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the floating copy button is shown.
        /// </summary>
        public bool IsCopyButtonVisible
        {
            get => (bool)GetValue(IsCopyButtonVisibleProperty);
            set => SetValue(IsCopyButtonVisibleProperty, value);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == FontSizeProperty && IsLoaded)
            {
                RenderCode();
            }
        }

        private static void OnPresentationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoCodePresenter presenter && presenter.IsLoaded)
            {
                presenter.RenderCode();
            }
        }

        private void DemoCodePresenter_Loaded(object sender, RoutedEventArgs e)
        {
            RenderCode();
        }

        private void RenderCode()
        {
            double lineHeight = TryFindResource(LineHeightKey) is double height ? height : FontSize * 20 / 14;
            Thickness padding = TryFindResource(PaddingKey) is Thickness value ? value : default;
            CodeHost.Content = DemoSourceHighlighter.CreateViewer(Code, CodeLanguage, FontSize, lineHeight, padding);
            CopyButtonHost.Visibility = IsCopyButtonVisible ? Visibility.Visible : Visibility.Collapsed;
            CopyCodeButton.Tag = Code;
        }

        private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Code))
            {
                DemoClipboard.SetText(Code);
            }
        }
    }
}
