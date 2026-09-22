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
    /// Reusable card that presents a single control demonstration on a gallery page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each card is divided into four zones:
    /// <list type="number">
    ///   <item><description><b>Description</b> - a bold label above the card set via <see cref="SampleDescription"/>.</description></item>
    ///   <item><description><b>Live demo</b> - the area that hosts the actual running control, provided via <see cref="DemoContent"/>. Optional <see cref="OutputContent"/> sits beneath it to display interaction results.</description></item>
    ///   <item><description><b>Options rail</b> - a collapsible right panel for property toggles, provided via <see cref="RightRailContent"/>. Hidden when empty.</description></item>
    ///   <item><description><b>Source expander</b> - a collapsible section below the card with XAML and C# tabs showing copy-enabled source code. Hidden when both <see cref="XamlSource"/> and <see cref="CSharpSource"/> are empty.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Because WPF error MC3093 prevents naming controls inside a property element, named live
    /// controls are declared in hidden <c language="csharp">ContentControl</c> slots at the page root and transferred
    /// into this card by <c language="csharp">DemoSamplePageWiring.Apply</c>.
    /// </para>
    /// </remarks>
    public partial class DemoSampleControl : ContentControl
    {
        private const double SourceFontSize = 12;
        private const double SourceLineHeight = 18;
        private const double SourceViewerMinHeight = 220;

        /// <summary>
        /// Identifies the <see cref="SampleDescription"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SampleDescriptionProperty =
            DependencyProperty.Register(
                "SampleDescription",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSampleDescriptionChanged));

        /// <summary>
        /// Identifies the <see cref="XamlSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XamlSourceProperty =
            DependencyProperty.Register(
                "XamlSource",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSourceChanged));

        /// <summary>
        /// Identifies the <see cref="CSharpSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CSharpSourceProperty =
            DependencyProperty.Register(
                "CSharpSource",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSourceChanged));

        /// <summary>
        /// Identifies the <see cref="DemoContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DemoContentProperty =
            DependencyProperty.Register(
                "DemoContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(defaultValue: null, OnDemoContentChanged));

        /// <summary>
        /// Identifies the <see cref="OutputContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OutputContentProperty =
            DependencyProperty.Register(
                "OutputContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(defaultValue: null, OnOutputContentChanged));

        /// <summary>
        /// Identifies the <see cref="RightRailContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RightRailContentProperty =
            DependencyProperty.Register(
                "RightRailContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(defaultValue: null, OnRightRailContentChanged));

        private bool _sourceLoaded;

        public DemoSampleControl()
        {
            InitializeComponent();
            UpdateSampleDescriptionVisibility();
            UpdateDemoContentVisibility();
            UpdateOutputVisibility();
            UpdateRightRailVisibility();
            UpdateSourceVisibility();
        }

        /// <summary>
        /// Gets or sets the bold label displayed above the sample card. The label is hidden when
        /// this value is empty or whitespace.
        /// </summary>
        public string SampleDescription
        {
            get => (string)GetValue(SampleDescriptionProperty);
            set => SetValue(SampleDescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the XAML source text shown in the source expander's XAML tab. The
        /// expander is hidden when both this and <see cref="CSharpSource"/> are empty.
        /// </summary>
        public string XamlSource
        {
            get => (string)GetValue(XamlSourceProperty);
            set => SetValue(XamlSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the C# source text shown in the source expander's C# tab. The expander
        /// is hidden when both this and <see cref="XamlSource"/> are empty.
        /// </summary>
        public string CSharpSource
        {
            get => (string)GetValue(CSharpSourceProperty);
            set => SetValue(CSharpSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the live control displayed in the demo region of the card. When
        /// <see langword="null"/>, the card body and source expander corners are adjusted to
        /// indicate there is no live preview.
        /// </summary>
        public object? DemoContent
        {
            get => GetValue(DemoContentProperty);
            set => SetValue(DemoContentProperty, value);
        }

        /// <summary>
        /// Gets or sets optional content displayed beneath the live demo to show interaction
        /// results (for example, a click counter or selected-value readout). Hidden when
        /// <see langword="null"/>.
        /// </summary>
        public object? OutputContent
        {
            get => GetValue(OutputContentProperty);
            set => SetValue(OutputContentProperty, value);
        }

        /// <summary>
        /// Gets or sets optional content for the right-side options rail (property toggles,
        /// radio buttons, etc.). The rail collapses automatically when this is
        /// <see langword="null"/>.
        /// </summary>
        public object? RightRailContent
        {
            get => GetValue(RightRailContentProperty);
            set => SetValue(RightRailContentProperty, value);
        }

        private static void OnSampleDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateSampleDescriptionVisibility();
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.ResetSource();
            }
        }

        private static void OnDemoContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateDemoContentVisibility();
            }
        }

        private static void OnOutputContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateOutputVisibility();
            }
        }

        private static void OnRightRailContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateRightRailVisibility();
            }
        }

        private void UpdateSampleDescriptionVisibility()
        {
            if (SampleDescriptionTextBlock is null)
            {
                return;
            }

            SampleDescriptionTextBlock.Visibility = string.IsNullOrWhiteSpace(SampleDescription)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateSourceVisibility()
        {
            if (SourceExpander is null)
            {
                return;
            }

            SourceExpander.Visibility = string.IsNullOrWhiteSpace(XamlSource) && string.IsNullOrWhiteSpace(CSharpSource)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateDemoContentVisibility()
        {
            if (SampleCard is null || SourceExpander is null)
            {
                return;
            }

            if (DemoContent is null)
            {
                SampleCard.Visibility = Visibility.Collapsed;
                SourceExpander.BorderThickness = new Thickness(1);
                SourceExpander.CornerRadius = new CornerRadius(8);
                return;
            }

            SampleCard.Visibility = Visibility.Visible;
            SourceExpander.BorderThickness = new Thickness(1, 0, 1, 1);
            SourceExpander.CornerRadius = new CornerRadius(0, 0, 8, 8);
        }

        private void UpdateOutputVisibility()
        {
            if (OutputRegion is null)
            {
                return;
            }

            OutputRegion.Visibility = OutputContent is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateRightRailVisibility()
        {
            if (RightRailBorder is null)
            {
                return;
            }

            RightRailBorder.Visibility = RightRailContent is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ResetSource()
        {
            _sourceLoaded = false;
            SourceSelector?.Items.Clear();
            SourceContentHost?.SetCurrentValue(ContentProperty, value: null);

            UpdateSourceVisibility();
            if ((SourceExpander?.IsExpanded) is true)
            {
                LoadSourceTabs();
            }
        }

        private void SourceExpander_Expanded(object sender, RoutedEventArgs e)
        {
            LoadSourceTabs();
        }

        private void LoadSourceTabs()
        {
            if (_sourceLoaded || (string.IsNullOrWhiteSpace(XamlSource) && string.IsNullOrWhiteSpace(CSharpSource)))
            {
                return;
            }

            _sourceLoaded = true;
            SourceSelector.Items.Clear();
            if (!string.IsNullOrWhiteSpace(XamlSource))
            {
                AddSourceTab("XAML", XamlSource, DemoSourceLanguage.Xaml);
            }

            if (!string.IsNullOrWhiteSpace(CSharpSource))
            {
                AddSourceTab("C#", CSharpSource, DemoSourceLanguage.CSharp);
            }
        }

        private void AddSourceTab(string header, string source, DemoSourceLanguage language)
        {
            // The pane hangs off the item rather than being rebuilt on every selection change,
            // the way the WinUI Gallery keeps one SampleCodePresenter per SelectorBarItem.
            Controls.SelectorBarItem item = new()
            {
                Text = header,
                Tag = CreateSourcePane(source, language),
            };
            _ = SourceSelector.Items.Add(item);

            if (SourceSelector.SelectedIndex < 0)
            {
                SourceSelector.SelectedIndex = 0;
            }
        }

        private void SourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object? pane = (SourceSelector.SelectedItem as Controls.SelectorBarItem)?.Tag;
            SourceContentHost.SetCurrentValue(ContentProperty, pane);
        }

        private static Grid CreateSourcePane(string source, DemoSourceLanguage language)
        {
            Grid panel = new();

            RichTextBox viewer = DemoSourceHighlighter.CreateViewer(source, language, SourceFontSize, SourceLineHeight, GetThicknessResource("DemoSourceCodeDocumentPadding", new Thickness(12)));
            viewer.Name = "SourceTextViewer";
            viewer.MinHeight = SourceViewerMinHeight;
            _ = panel.Children.Add(viewer);

            Border copyButtonHost = CreateCopyButtonHost(CreateCopyButton(source));
            _ = panel.Children.Add(copyButtonHost);

            return panel;
        }

        private static Border CreateCopyButtonHost(Controls.Button copyButton)
        {
            Border border = new()
            {
                Name = "CopySourceButtonHost",
                BorderThickness = new Thickness(1),
                Child = copyButton,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = GetThicknessResource("DemoSourceCopyButtonHostMargin", new Thickness(0, 8, 8, 0)),
                VerticalAlignment = VerticalAlignment.Top,
            };
            border.SetResourceReference(BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            border.SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            return border;
        }

        private static Controls.Button CreateCopyButton(string source)
        {
            Controls.Button button = new()
            {
                Name = "CopySourceButton",
                Appearance = ControlAppearance.Subtle,
                Icon = new Controls.FontIcon { Glyph = "\uE8C8", IconFontSize = 14 },
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 0,
                Padding = GetThicknessResource("DemoSourceCopyButtonPadding", new Thickness(8, 4, 8, 4)),
                Tag = source,
            };
            button.Click += OnCopySourceButtonClick;
            return button;
        }

        private static void OnCopySourceButtonClick(object sender, RoutedEventArgs e)
        {
            string? source = sender is FrameworkElement element ? element.Tag as string : null;
            if (!string.IsNullOrWhiteSpace(source))
            {
                DemoClipboard.SetText(source);
            }
        }

        private static Thickness GetThicknessResource(string key, Thickness fallback)
        {
            return Application.Current?.TryFindResource(key) is Thickness value ? value : fallback;
        }
    }
}
