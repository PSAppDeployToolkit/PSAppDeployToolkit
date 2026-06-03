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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Border with optional Fluent visual presets applied via theme resources.
    /// </summary>
    public class Border : System.Windows.Controls.Border
    {
        /// <summary>
        /// Identifies the <see cref="Variant"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(BorderVariant),
                typeof(Border),
                new FrameworkPropertyMetadata(BorderVariant.None, OnVariantChanged));

        /// <summary>
        /// Gets or sets the visual preset variant (None, Card, Subtle, Divider).
        /// </summary>
        public BorderVariant Variant
        {
            get => (BorderVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Border border = (Border)d;
            border.ApplyVariant();
        }

        private void ApplyVariant()
        {
            switch (Variant)
            {
                case BorderVariant.None:
                    return;
                case BorderVariant.Card:
                    SetResourceReference(BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
                    SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");
                    BorderThickness = new Thickness(1);
                    CornerRadius = new CornerRadius(8);
                    break;
                case BorderVariant.Subtle:
                    SetResourceReference(BackgroundProperty, "SubtleFillColorSecondaryBrush");
                    SetResourceReference(BorderBrushProperty, "ControlFillColorTransparentBrush");
                    BorderThickness = new Thickness(0);
                    CornerRadius = new CornerRadius(4);
                    break;
                case BorderVariant.Divider:
                    SetResourceReference(BackgroundProperty, "ControlFillColorTransparentBrush");
                    SetResourceReference(BorderBrushProperty, "DividerStrokeColorDefaultBrush");
                    BorderThickness = new Thickness(0, 0, 0, 1);
                    CornerRadius = new CornerRadius(0);
                    break;
                default:
                    return;
            }
        }
    }
}
