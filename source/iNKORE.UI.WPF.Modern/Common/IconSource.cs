// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Windows;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern.Controls;


namespace iNKORE.UI.WPF.Modern.Common
{
    /// <summary>
    /// Represents the base class for an icon source.
    /// </summary>
    public class IconSource : DependencyObject
    {
        /// <summary>
        /// Identifies the <see cref="Foreground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(IconSource));

        /// <summary>
        /// Gets or sets a brush that describes the foreground color.
        /// </summary>
        /// <returns>
        /// The brush that paints the foreground of the control. The default is <see langword="null"/>, (a null brush) which is
        /// evaluated as Transparent for rendering.
        /// </returns>
        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Foreground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultSizeProperty =
            DependencyProperty.Register(
                nameof(DefaultSize),
                typeof(Size?),
                typeof(IconSource),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a size value that is applied when generating the icon. This is useful in some cases that you can only pass an IconSource instead of an actual element and want to specify the size.
        /// </summary>
        public Size? DefaultSize
        {
            get => (Size?)GetValue(DefaultSizeProperty);
            set => SetValue(DefaultSizeProperty, value);
        }


        /// <summary>
        /// Creates an icon UI element.
        /// </summary>
        /// <returns>An icon UI element.</returns>
        public IconElement CreateIconElement()
        {
            var element = CreateIconElementCore();
            if (DefaultSize != null && element != null)
            {
                element.Width = DefaultSize.Value.Width;
                element.Height = DefaultSize.Value.Height;
            }

            return element;
        }

        /// <summary>
        /// Creates an icon UI element.
        /// </summary>
        /// <returns>An icon UI element.</returns>
        protected virtual IconElement CreateIconElementCore() => null;
    }
}
