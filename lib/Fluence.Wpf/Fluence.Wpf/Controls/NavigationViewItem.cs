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

using Fluence.Wpf.Automation;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents an entry inside a <see cref="NavigationView"/> pane.
    /// </summary>
    /// <remarks>Inspired by WinUI3's NavigationView.</remarks>
    public class NavigationViewItem : ListBoxItem
    {
        /// <summary>
        /// Identifies the read-only IsPressed dependency property key for the NavigationViewItem control.
        /// </summary>
        /// <remarks>This key is used internally to set the value of the IsPressed property. Consumers
        /// should use the IsPressed property for reading the value.</remarks>
        private static readonly DependencyPropertyKey IsPressedPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsPressed",
            typeof(bool),
            typeof(NavigationViewItem),
            new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the read-only <see cref="IsPressed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty = IsPressedPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon",
            typeof(object),
            typeof(NavigationViewItem),
            new PropertyMetadata(defaultValue: null, OnIconChanged));

        /// <summary>
        /// Identifies the <see cref="InfoBadge"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBadgeProperty = DependencyProperty.Register(
            "InfoBadge",
            typeof(object),
            typeof(NavigationViewItem),
            new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="IsChildItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsChildItemProperty = DependencyProperty.Register(
            "IsChildItem",
            typeof(bool),
            typeof(NavigationViewItem),
            new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Initializes static members of the NavigationViewItem class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the NavigationViewItem control uses its own
        /// style by default, as defined in the application's resource dictionaries. This is necessary for proper
        /// theming and templating in WPF custom controls.</remarks>
        static NavigationViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NavigationViewItem),
                new FrameworkPropertyMetadata(typeof(NavigationViewItem)));
        }

        /// <summary>
        /// Gets or sets the icon content for this item (typically a <see cref="FontIcon"/>).
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets an <see cref="Controls.InfoBadge"/> element shown on this item.
        /// </summary>
        public object InfoBadge
        {
            get => GetValue(InfoBadgeProperty);
            set => SetValue(InfoBadgeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this item is a child entry in an expanded navigation section.
        /// Child entries keep their selection indicator aligned with the content column.
        /// </summary>
        public bool IsChildItem
        {
            get => (bool)GetValue(IsChildItemProperty);
            set => SetValue(IsChildItemProperty, value);
        }

        /// <summary>
        /// Gets whether the item is currently being pressed by a pointer.
        /// </summary>
        public bool IsPressed => (bool)GetValue(IsPressedProperty);

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.Key is Key.Enter or Key.Space) && IsEnabled && NavigationView.FromItemContainer(this) is NavigationView nav)
            {
                nav.InvokeItem(this);
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            SetValue(IsPressedPropertyKey, value: true);
            _ = Mouse.Capture(this, CaptureMode.SubTree);
            base.OnMouseLeftButtonDown(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            SetValue(IsPressedPropertyKey, value: false);
            _ = Mouse.Capture(element: null);
            base.OnMouseLeftButtonUp(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (IsPressed)
            {
                SetValue(IsPressedPropertyKey, value: false);
            }
            base.OnMouseLeave(e);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NavigationViewItemAutomationPeer(this);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Parent <see cref="NavigationView"/> derives from <see cref="System.Windows.Controls.Primitives.Selector"/> (not <see cref="ListBox"/>).
        /// <see cref="ListBoxItem"/> handles mouse on the bubbling route and may mark the event handled before selection sync runs;
        /// we handle preview mouse and sync selection on the parent so clicks always update selection.
        /// </remarks>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsEnabled || e.ClickCount is not 1)
            {
                base.OnPreviewMouseLeftButtonDown(e);
                return;
            }
            if (NavigationView.FromItemContainer(this) is not NavigationView nav)
            {
                base.OnPreviewMouseLeftButtonDown(e);
                return;
            }
            nav.InvokeItem(this);
            e.Handled = true;
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ApplyDefaultFontIconSize(e.NewValue as FontIcon);
        }

        private static void ApplyDefaultFontIconSize(FontIcon? icon)
        {
            if (icon?.ReadLocalValue(FontIcon.IconFontSizeProperty) == DependencyProperty.UnsetValue)
            {
                icon.SetCurrentValue(FontIcon.IconFontSizeProperty, 16.0);
            }
        }
    }
}
