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
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Filters a <see cref="CornerRadius"/> or <see cref="Thickness"/> so only the edge named by
    /// <see cref="Edge"/> is kept and the opposite edge is zeroed.
    /// </summary>
    /// <remarks>
    /// Mirrors WinUI's <c language="csharp">TopCornerRadiusFilterConverter</c> and
    /// <c language="csharp">BottomCornerRadiusFilterConverter</c> (Expander.xaml:35,64,98),
    /// extended to also filter <see cref="Thickness"/> so the seam edge shared between two
    /// adjoining tiers (for example the Expander header and content borders) can be derived from
    /// the control's own <c language="csharp">BorderThickness</c> rather than a hardcoded literal.
    /// </remarks>
    internal sealed class CornerRadiusFilterConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets which edge of the value is kept; the opposite edge is zeroed.
        /// </summary>
        public CornerRadiusFilterEdge Edge { get; set; }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                CornerRadius cornerRadius => Edge is CornerRadiusFilterEdge.Top
                    ? new CornerRadius(cornerRadius.TopLeft, cornerRadius.TopRight, 0, 0)
                    : new CornerRadius(0, 0, cornerRadius.BottomRight, cornerRadius.BottomLeft),
                Thickness thickness => Edge is CornerRadiusFilterEdge.Top
                    ? new Thickness(thickness.Left, thickness.Top, thickness.Right, 0)
                    : new Thickness(thickness.Left, 0, thickness.Right, thickness.Bottom),
                _ => value,
            };
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
