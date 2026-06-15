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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents a flyout that displays arbitrary content on the Fluent flyout surface,
    /// mirroring the WinUI 3 <c>Flyout</c> control. The content is hosted in a
    /// <see cref="FlyoutPresenter"/> inside a light-dismiss popup.
    /// </summary>
    public class Flyout : FlyoutBase
    {
        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                nameof(Content),
                typeof(object),
                typeof(Flyout),
                new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the content shown in the flyout.
        /// </summary>
        /// <remarks>
        /// The content is hosted in an unparented popup, so it does not live in the placement
        /// target's name scope: <c>ElementName</c> (and <c>RelativeSource FindAncestor</c>
        /// walks above the presenter) bindings inside the content do not resolve. The
        /// presenter inherits the placement target's <see cref="FrameworkElement.DataContext"/>
        /// while the flyout is open, so plain <c>Binding</c> paths against the anchor's view
        /// model work as expected.
        /// </remarks>
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FlyoutPresenterStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutPresenterStyleProperty =
            DependencyProperty.Register(
                nameof(FlyoutPresenterStyle),
                typeof(Style),
                typeof(Flyout),
                new PropertyMetadata(defaultValue: null, OnFlyoutPresenterStyleChanged));

        /// <summary>
        /// Gets or sets the style applied to the <see cref="FlyoutPresenter"/> that hosts
        /// <see cref="Content"/>. When <see langword="null"/>, the default themed presenter
        /// style is used.
        /// </summary>
        public Style? FlyoutPresenterStyle
        {
            get => (Style?)GetValue(FlyoutPresenterStyleProperty);
            set => SetValue(FlyoutPresenterStyleProperty, value);
        }

        /// <summary>
        /// Creates (or returns the cached) <see cref="FlyoutPresenter"/> with its
        /// <see cref="ContentControl.Content"/> bound to <see cref="Content"/> and
        /// <see cref="FlyoutPresenterStyle"/> applied when set.
        /// </summary>
        /// <returns>The presenter hosting the flyout content.</returns>
        protected override FrameworkElement CreatePresenter()
        {
            if (_presenter is null)
            {
                _presenter = new FlyoutPresenter();
                Binding contentBinding = new(nameof(Content))
                {
                    Source = this,
                };
                _ = _presenter.SetBinding(ContentControl.ContentProperty, contentBinding);
                if (FlyoutPresenterStyle is not null)
                {
                    _presenter.Style = FlyoutPresenterStyle;
                }
            }

            return _presenter;
        }

        private static void OnFlyoutPresenterStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Flyout flyout && flyout._presenter is not null)
            {
                flyout._presenter.Style = e.NewValue as Style;
            }
        }

        /// <summary>
        /// The cached presenter instance reused across show and hide cycles.
        /// </summary>
        private FlyoutPresenter? _presenter;
    }
}
