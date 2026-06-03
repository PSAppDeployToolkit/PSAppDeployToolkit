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

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled button with multiple appearance modes.
    /// </summary>
    public class Button : System.Windows.Controls.Button
    {
        /// <summary>
        /// Initializes static members of the Button class and overrides the default style metadata for the control.
        /// </summary>
        /// <remarks>This static constructor ensures that the Button control uses its own style by
        /// default, rather than inheriting the style from its base class. This is important for custom control
        /// development in WPF, as it allows the control to be styled appropriately when used in XAML.</remarks>
        static Button()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Button),
                new FrameworkPropertyMetadata(typeof(Button)));
        }

        /// <summary>
        /// Identifies the <see cref="Appearance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AppearanceProperty =
            DependencyProperty.Register(
                nameof(Appearance),
                typeof(ControlAppearance),
                typeof(Button),
                new FrameworkPropertyMetadata(ControlAppearance.Standard));

        /// <summary>
        /// Gets or sets the visual appearance of the button.
        /// </summary>
        public ControlAppearance Appearance
        {
            get => (ControlAppearance)GetValue(AppearanceProperty);
            set => SetValue(AppearanceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(Button),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the button.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(Button),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the button.
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.Register(
                nameof(IconPlacement),
                typeof(ElementPlacement),
                typeof(Button),
                new FrameworkPropertyMetadata(ElementPlacement.Left));

        /// <summary>
        /// Gets or sets the placement of the icon relative to the content.
        /// </summary>
        public ElementPlacement IconPlacement
        {
            get => (ElementPlacement)GetValue(IconPlacementProperty);
            set => SetValue(IconPlacementProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _mainContentPresenter?.SizeChanged -= OnMainContentPresenterSizeChanged;
            base.OnApplyTemplate();
            _mainContentPresenter = GetTemplateChild("MainContentPresenter") as ContentPresenter;
            _mainContentPresenter?.SizeChanged += OnMainContentPresenterSizeChanged;
            UpdateTruncationToolTip();
        }

        /// <inheritdoc />
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            UpdateTruncationToolTip();
        }

        private void OnMainContentPresenterSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTruncationToolTip();
        }

        private void UpdateTruncationToolTip()
        {
            if (!_hasAutomaticToolTip && ReadLocalValue(ToolTipProperty) != DependencyProperty.UnsetValue)
            {
                return;
            }
            if (Content is not string text || string.IsNullOrWhiteSpace(text))
            {
                ClearAutomaticToolTip();
                return;
            }
            if (_mainContentPresenter is null)
            {
                return;
            }
            if (FindVisualChild<System.Windows.Controls.TextBlock>(_mainContentPresenter) is not System.Windows.Controls.TextBlock textBlock)
            {
                ClearAutomaticToolTip();
                return;
            }
            FormattedText formattedText = new(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                textBlock.Foreground,
                new NumberSubstitution(NumberCultureSource.Text, CultureInfo.CurrentCulture, NumberSubstitutionMethod.AsCulture),
                TextFormattingMode.Display,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            double textWidth = formattedText.WidthIncludingTrailingWhitespace;
            if (textBlock.ActualWidth > 0 && textWidth > textBlock.ActualWidth + 0.5)
            {
                SetAutomaticToolTip(text);
            }
            else
            {
                ClearAutomaticToolTip();
            }
        }

        private void SetAutomaticToolTip(string text)
        {
            _hasAutomaticToolTip = true;
            SetValue(ToolTipProperty, text);
        }

        private void ClearAutomaticToolTip()
        {
            if (!_hasAutomaticToolTip)
            {
                return;
            }
            _hasAutomaticToolTip = false;
            ClearValue(ToolTipProperty);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent is null)
            {
                return null;
            }
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    return match;
                }

                if (FindVisualChild<T>(child) is T descendant)
                {
                    return descendant;
                }
            }
            return null;
        }

        /// <summary>
        /// Represents the main content presenter used to display content within the control.
        /// </summary>
        private ContentPresenter? _mainContentPresenter;

        /// <summary>
        /// Indicates whether an automatic tooltip is enabled.
        /// </summary>
        private bool _hasAutomaticToolTip;
    }
}
