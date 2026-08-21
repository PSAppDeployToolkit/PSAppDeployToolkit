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
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A <see cref="TabItem"/> container used by <see cref="TabView"/> that renders an icon, a header,
    /// and an optional close button aligned with the WinUI 3 TabView visual language.
    /// </summary>
    [TemplatePart(Name = PartCloseButton, Type = typeof(ButtonBase))]
    public class TabViewItem : TabItem
    {
        // Template part names.
        private const string PartCloseButton = "PART_CloseButton";

        /// <summary>
        /// Identifies the <see cref="IsClosable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(
                nameof(IsClosable),
                typeof(bool),
                typeof(TabViewItem),
                new PropertyMetadata(defaultValue: true));

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(TabViewItem),
                new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="CloseRequested"/> routed event.
        /// </summary>
        public static readonly RoutedEvent CloseRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(CloseRequested),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(TabViewItem));

        /// <summary>
        /// Initializes static members of the TabViewItem class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that TabViewItem uses its own style by default,
        /// allowing custom styling and theming through XAML resources.</remarks>
        static TabViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TabViewItem),
                new FrameworkPropertyMetadata(typeof(TabViewItem)));
        }

        /// <summary>
        /// Gets or sets whether the per-tab close button is shown for this item.
        /// Note the effective visibility still follows the owning <see cref="TabView.CloseButtonOverlayMode"/>.
        /// </summary>
        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon shown at the leading edge of the tab header.
        /// Accepts any element (typically a <see cref="FontIcon"/> or <see cref="Image"/>).
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Raised when the user clicks the per-tab close button. The parent <see cref="TabView"/>
        /// aggregates this into <see cref="TabView.TabCloseRequested"/> for convenience.
        /// </summary>
        [SuppressMessage("Design", "S3908", Justification = "RoutedEventHandler is required by WPF's routed event infrastructure.")]
        public event RoutedEventHandler CloseRequested
        {
            add => AddHandler(CloseRequestedEvent, value);
            remove => RemoveHandler(CloseRequestedEvent, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _closeButton?.Click -= OnCloseButtonClick;
            _closeButton = GetTemplateChild(PartCloseButton) as ButtonBase;
            _closeButton?.Click += OnCloseButtonClick;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new TabViewTabCloseRequestedEventArgs(CloseRequestedEvent, this, this, DataContext ?? this));
        }

        /// <summary>
        /// Represents the close button control associated with the current context.
        /// </summary>
        private ButtonBase? _closeButton;
    }
}
