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
using System.Windows.Data;
using System.Windows.Media;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Provides attached properties for extending TextBlock with Fluent Design features.
    /// </summary>
    public static class TextBlockExtensions
    {
        // Style keys for typography styles defined in the resource dictionaries.
        private const string CaptionTextBlockStyleKey = "CaptionTextBlockStyle";
        private const string BodyTextBlockStyleKey = "BodyTextBlockStyle";
        private const string BodyStrongTextBlockStyleKey = "BodyStrongTextBlockStyle";
        private const string BodyLargeTextBlockStyleKey = "BodyLargeTextBlockStyle";
        private const string SubtitleTextBlockStyleKey = "SubtitleTextBlockStyle";
        private const string TitleTextBlockStyleKey = "TitleTextBlockStyle";
        private const string TitleLargeTextBlockStyleKey = "TitleLargeTextBlockStyle";
        private const string DisplayTextBlockStyleKey = "DisplayTextBlockStyle";

        #region Typography

        /// <summary>
        /// Identifies the Typography attached property.
        /// </summary>
        public static readonly DependencyProperty TypographyProperty =
            DependencyProperty.RegisterAttached(
                "Typography",
                typeof(FluentTypography),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(FluentTypography.None, OnTypographyChanged));

        /// <summary>
        /// Gets the typography style for the specified TextBlock.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <returns>The requested Fluent typography style.</returns>
        public static FluentTypography GetTypography(this DependencyObject obj)
        {
            return (FluentTypography)obj.GetValue(TypographyProperty);
        }

        /// <summary>
        /// Sets the typography style for the specified TextBlock.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <param name="value">The Fluent typography style to apply.</param>
        public static void SetTypography(this DependencyObject obj, FluentTypography value)
        {
            obj.SetValue(TypographyProperty, value);
        }

        private static void OnTypographyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not System.Windows.Controls.TextBlock textBlock)
            {
                return;
            }
            FluentTypography typography = (FluentTypography)e.NewValue;
            ApplyTypography(textBlock, typography);
        }

        private static void ApplyTypography(System.Windows.Controls.TextBlock textBlock, FluentTypography typography)
        {
            if (GetTypographyStyleKey(typography) is not string styleKey)
            {
                return;
            }
            textBlock.SetResourceReference(FrameworkElement.StyleProperty, styleKey);
        }

        private static string? GetTypographyStyleKey(FluentTypography typography)
        {
            return typography switch
            {
                FluentTypography.Caption => CaptionTextBlockStyleKey,
                FluentTypography.Body => BodyTextBlockStyleKey,
                FluentTypography.BodyStrong => BodyStrongTextBlockStyleKey,
                FluentTypography.BodyLarge => BodyLargeTextBlockStyleKey,
                FluentTypography.Subtitle => SubtitleTextBlockStyleKey,
                FluentTypography.Title => TitleTextBlockStyleKey,
                FluentTypography.TitleLarge => TitleLargeTextBlockStyleKey,
                FluentTypography.Display => DisplayTextBlockStyleKey,
                FluentTypography.None or _ => null,
            };
        }

        #endregion Typography

        #region TextTrimming

        /// <summary>
        /// Identifies the TextTrimming attached property.
        /// </summary>
        public static readonly DependencyProperty TextTrimmingProperty =
            DependencyProperty.RegisterAttached(
                "TextTrimming",
                typeof(TextTrimming),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(TextTrimming.None, OnTextTrimmingChanged));

        /// <summary>
        /// Gets the value of the <see cref="TextTrimmingProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <returns>The requested text trimming mode.</returns>
        public static TextTrimming GetTextTrimming(this DependencyObject obj)
        {
            return (TextTrimming)obj.GetValue(TextTrimmingProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="TextTrimmingProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <param name="value">The text trimming mode to apply.</param>
        public static void SetTextTrimming(this DependencyObject obj, TextTrimming value)
        {
            obj.SetValue(TextTrimmingProperty, value);
        }

        private static void OnTextTrimmingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not System.Windows.Controls.TextBlock textBlock)
            {
                return;
            }
            textBlock.TextTrimming = (TextTrimming)e.NewValue;
        }

        #endregion TextTrimming

        #region IsTextSelectionEnabled

        /// <summary>
        /// Identifies the IsTextSelectionEnabled attached property.
        /// </summary>
        public static readonly DependencyProperty IsTextSelectionEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsTextSelectionEnabled",
                typeof(bool),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(defaultValue: false, OnIsTextSelectionEnabledChanged));

        /// <summary>
        /// Gets the value of the <see cref="IsTextSelectionEnabledProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <returns><see langword="true"/> if selection is enabled; otherwise <see langword="false"/>.</returns>
        public static bool GetIsTextSelectionEnabled(this DependencyObject obj)
        {
            return (bool)obj.GetValue(IsTextSelectionEnabledProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="IsTextSelectionEnabledProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <param name="value"><see langword="true"/> to enable text selection; otherwise <see langword="false"/>.</param>
        public static void SetIsTextSelectionEnabled(this DependencyObject obj, bool value)
        {
            obj.SetValue(IsTextSelectionEnabledProperty, value);
        }

        private static void OnIsTextSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not System.Windows.Controls.TextBlock textBlock)
            {
                return;
            }
            if ((bool)e.NewValue)
            {
                if (textBlock.IsLoaded)
                {
                    ApplySelectionOverlay(textBlock);
                }
                else
                {
                    textBlock.Loaded += OnTextBlockLoadedForSelection;
                }
            }
        }

        private static void OnTextBlockLoadedForSelection(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBlock textBlock = (System.Windows.Controls.TextBlock)sender;
            textBlock.Loaded -= OnTextBlockLoadedForSelection;
            ApplySelectionOverlay(textBlock);
        }

        private static void ApplySelectionOverlay(System.Windows.Controls.TextBlock textBlock)
        {
            if (!GetIsTextSelectionEnabled(textBlock))
            {
                return;
            }
            if (VisualTreeHelper.GetParent(textBlock) is not Panel parent)
            {
                return;
            }

            int index = parent.Children.IndexOf(textBlock);
            if (index < 0)
            {
                return;
            }

            parent.Children.RemoveAt(index);
            Grid grid = new();
            textBlock.Opacity = 0;
            textBlock.IsHitTestVisible = false;
            _ = grid.Children.Add(textBlock);
            System.Windows.Controls.TextBox overlay = new()
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = textBlock.Padding,
                Foreground = textBlock.Foreground,
                FontFamily = textBlock.FontFamily,
                FontSize = textBlock.FontSize,
                FontWeight = textBlock.FontWeight,
                FontStyle = textBlock.FontStyle,
                FontStretch = textBlock.FontStretch,
                TextWrapping = textBlock.TextWrapping,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsReadOnly = true,
                CaretBrush = textBlock.Foreground,
                SelectionBrush = SystemColors.HighlightBrush,
            };
            _ = overlay.SetBinding(System.Windows.Controls.TextBox.TextProperty, new Binding
            {
                Path = new PropertyPath(System.Windows.Controls.TextBlock.TextProperty),
                Source = textBlock,
                Mode = BindingMode.OneWay,
            });
            _ = grid.Children.Add(overlay);
            parent.Children.Insert(index, grid);
        }

        #endregion IsTextSelectionEnabled

        #region PlaceholderText

        /// <summary>
        /// Identifies the PlaceholderText attached property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets the placeholder text for the specified element.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns>The placeholder text.</returns>
        public static string GetPlaceholderText(this DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderTextProperty);
        }

        /// <summary>
        /// Sets the placeholder text for the specified element.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value">The placeholder text to store.</param>
        public static void SetPlaceholderText(this DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderTextProperty, value);
        }

        #endregion PlaceholderText

        #region ShowPlaceholder

        /// <summary>
        /// Identifies the ShowPlaceholder attached property.
        /// </summary>
        public static readonly DependencyProperty ShowPlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "ShowPlaceholder",
                typeof(bool),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Gets whether the placeholder should be shown.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns><see langword="true"/> when the placeholder should be shown; otherwise <see langword="false"/>.</returns>
        public static bool GetShowPlaceholder(this DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowPlaceholderProperty);
        }

        /// <summary>
        /// Sets whether the placeholder should be shown.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value"><see langword="true"/> to show the placeholder; otherwise <see langword="false"/>.</param>
        public static void SetShowPlaceholder(this DependencyObject obj, bool value)
        {
            obj.SetValue(ShowPlaceholderProperty, value);
        }

        #endregion ShowPlaceholder

        #region Icon

        /// <summary>
        /// Identifies the Icon attached property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(object),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets the icon for the specified element.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns>The icon content.</returns>
        public static object GetIcon(this DependencyObject obj)
        {
            return obj.GetValue(IconProperty);
        }

        /// <summary>
        /// Sets the icon for the specified element.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value">The icon content to store.</param>
        public static void SetIcon(this DependencyObject obj, object value)
        {
            obj.SetValue(IconProperty, value);
        }

        #endregion Icon

        #region IconPlacement

        /// <summary>
        /// Identifies the IconPlacement attached property.
        /// </summary>
        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.RegisterAttached(
                "IconPlacement",
                typeof(ElementPlacement),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(ElementPlacement.Left));

        /// <summary>
        /// Gets the icon placement for the specified element.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <returns>The requested icon placement.</returns>
        public static ElementPlacement GetIconPlacement(this DependencyObject obj)
        {
            return (ElementPlacement)obj.GetValue(IconPlacementProperty);
        }

        /// <summary>
        /// Sets the icon placement for the specified element.
        /// </summary>
        /// <param name="obj">The target dependency object.</param>
        /// <param name="value">The icon placement to apply.</param>
        public static void SetIconPlacement(this DependencyObject obj, ElementPlacement value)
        {
            obj.SetValue(IconPlacementProperty, value);
        }

        #endregion IconPlacement
    }
}
