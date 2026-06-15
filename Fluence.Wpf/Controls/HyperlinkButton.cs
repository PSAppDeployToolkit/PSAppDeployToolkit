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
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A hyperlink-styled button that can optionally navigate to a URI.
    /// </summary>
    /// <remarks>Inspired by WInUI's HyperlinkButton.</remarks>
    public class HyperlinkButton : System.Windows.Controls.Button
    {
        /// <summary>
        /// Initializes static members of the HyperlinkButton class and sets up the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the HyperlinkButton control uses its own default
        /// style as defined in the application's resources. This is necessary for proper theming and appearance in WPF
        /// applications.</remarks>
        static HyperlinkButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(HyperlinkButton),
                new FrameworkPropertyMetadata(typeof(HyperlinkButton)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperlinkButton"/> class.
        /// </summary>
        public HyperlinkButton()
        {
            Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Identifies the <see cref="NavigateUri"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NavigateUriProperty =
            DependencyProperty.Register(
                nameof(NavigateUri),
                typeof(Uri),
                typeof(HyperlinkButton),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the URI to navigate to when the button is clicked.
        /// </summary>
        public Uri NavigateUri
        {
            get => (Uri)GetValue(NavigateUriProperty);
            set => SetValue(NavigateUriProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(HyperlinkButton),
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
                typeof(HyperlinkButton),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

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
                typeof(HyperlinkButton),
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
        protected override void OnClick()
        {
            base.OnClick();
            if (NavigateUri is Uri uri)
            {
                _ = Process.Start(uri.AbsoluteUri);
            }
        }
    }
}
