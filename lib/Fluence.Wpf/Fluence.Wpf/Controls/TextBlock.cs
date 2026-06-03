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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design enhanced TextBlock that supports the FluentTypography type ramp.
    /// </summary>
    [TemplatePart(Name = PART_TextBlock, Type = typeof(System.Windows.Controls.TextBlock))]
    public class TextBlock : ContentControl
    {
        // The name of the TextBlock part in the control template.
        private const string PART_TextBlock = "PART_TextBlock";

        /// <summary>
        /// Initializes static members of the TextBlock class.
        /// </summary>
        /// <remarks>This static constructor overrides the default style key property metadata to
        /// associate the TextBlock type with its default style. This ensures that the correct style is applied to all
        /// instances of TextBlock.</remarks>
        static TextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(typeof(TextBlock)));
        }

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Typography"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TypographyProperty =
            DependencyProperty.Register(
                nameof(Typography),
                typeof(FluentTypography),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    FluentTypography.Body,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TextWrapping"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(
                nameof(TextWrapping),
                typeof(TextWrapping),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    TextWrapping.NoWrap,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTextPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TextTrimming"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextTrimmingProperty =
            DependencyProperty.Register(
                nameof(TextTrimming),
                typeof(TextTrimming),
                typeof(TextBlock),
                new FrameworkPropertyMetadata(
                    TextTrimming.None,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTextPropertyChanged));

        /// <summary>
        /// Gets or sets the text displayed by the inner <see cref="System.Windows.Controls.TextBlock"/>.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the Fluent typography style applied to the inner text.
        /// </summary>
        public FluentTypography Typography
        {
            get => (FluentTypography)GetValue(TypographyProperty);
            set => SetValue(TypographyProperty, value);
        }

        /// <summary>
        /// Gets or sets how text wraps within the inner <see cref="System.Windows.Controls.TextBlock"/>.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        /// <summary>
        /// Gets or sets how text is trimmed when it overflows the layout area.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _partTextBlock = GetTemplateChild(PART_TextBlock) as System.Windows.Controls.TextBlock;
            SyncPartTextBlock();
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBlock control = (TextBlock)d;
            control.SyncPartTextBlock();
        }

        private void SyncPartTextBlock()
        {
            if (_partTextBlock is null)
            {
                return;
            }
            _partTextBlock.Text = Text ?? string.Empty;
            TextBlockExtensions.SetTypography(_partTextBlock, Typography);
            _partTextBlock.TextWrapping = TextWrapping;
            _partTextBlock.TextTrimming = TextTrimming;
        }

        /// <summary>
        /// Represents the underlying TextBlock control used for displaying text content.
        /// </summary>
        /// <remarks>This field is typically used internally to reference the template part associated
        /// with text display in a custom control. It may be null if the template has not been applied or does not
        /// contain a TextBlock with the expected part name.</remarks>
        private System.Windows.Controls.TextBlock? _partTextBlock;
    }
}
