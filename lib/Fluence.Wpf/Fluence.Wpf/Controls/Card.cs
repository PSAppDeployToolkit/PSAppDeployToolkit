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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Fluent-styled card container with optional header, footer, and icon.
    /// </summary>
    public class Card : ContentControl
    {
        /// <summary>
        /// Initializes static members of the Card class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the Card control uses its custom style by
        /// default. It is called automatically before any static members are accessed or any instances are
        /// created.</remarks>
        static Card()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Card),
                new FrameworkPropertyMetadata(typeof(Card)));
        }

        /// <summary>
        /// Identifies the <see cref="Variant"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(CardVariant),
                typeof(Card),
                new FrameworkPropertyMetadata(CardVariant.Default));

        /// <summary>
        /// Gets or sets the visual variant of the card (Default, Outlined, Filled, Subtle).
        /// </summary>
        public CardVariant Variant
        {
            get => (CardVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(Card),
                new FrameworkPropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the card.
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
                typeof(Card),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the card header.
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(Card),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the header content of the card.
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register(
                nameof(HeaderTemplate),
                typeof(DataTemplate),
                typeof(Card),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the data template for the header content.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Footer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register(
                nameof(Footer),
                typeof(object),
                typeof(Card),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the footer content of the card.
        /// </summary>
        public object Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FooterTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterTemplateProperty =
            DependencyProperty.Register(
                nameof(FooterTemplate),
                typeof(DataTemplate),
                typeof(Card),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the data template for the footer content.
        /// </summary>
        public DataTemplate FooterTemplate
        {
            get => (DataTemplate)GetValue(FooterTemplateProperty);
            set => SetValue(FooterTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsClickable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsClickableProperty =
            DependencyProperty.Register(
                nameof(IsClickable),
                typeof(bool),
                typeof(Card),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the card responds to mouse click interactions.
        /// </summary>
        public bool IsClickable
        {
            get => (bool)GetValue(IsClickableProperty);
            set => SetValue(IsClickableProperty, value);
        }

        private static readonly DependencyPropertyKey IsPressedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsPressed),
                typeof(bool),
                typeof(Card),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsPressed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty =
            IsPressedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the card is currently pressed.
        /// </summary>
        public bool IsPressed
        {
            get => (bool)GetValue(IsPressedProperty);
            private set => SetValue(IsPressedPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="Click"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Click),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(Card));

        /// <summary>
        /// Occurs when a clickable card is activated by a mouse left-button release
        /// that began with a press inside the card bounds.
        /// </summary>
        [SuppressMessage("Design", "S3908", Justification = "RoutedEventHandler is required by WPF's routed event infrastructure.")]
        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (IsClickable && IsEnabled && e.Key is Key.Enter or Key.Space)
            {
                IsPressed = true;
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (IsClickable && IsEnabled && e.Key is Key.Enter or Key.Space)
            {
                bool wasPressed = IsPressed;
                IsPressed = false;
                if (wasPressed)
                {
                    RaiseEvent(new RoutedEventArgs(ClickEvent, this));
                }
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (IsClickable && IsEnabled)
            {
                IsPressed = true;
                _ = CaptureMouse();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            bool wasPressed = IsPressed;
            if (wasPressed)
            {
                IsPressed = false;
                ReleaseMouseCapture();
            }
            if (wasPressed && IsClickable && IsEnabled)
            {
                RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            }
        }

        /// <inheritdoc />
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            IsPressed = false;
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (IsPressed)
            {
                IsPressed = false;
                ReleaseMouseCapture();
            }
        }
    }
}
