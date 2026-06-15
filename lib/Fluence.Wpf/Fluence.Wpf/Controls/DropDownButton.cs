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
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A toggle button that displays a flyout in a popup when checked.
    /// </summary>
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    public class DropDownButton : ToggleButton
    {
        // Template part names.
        private const string PART_Popup = "PART_Popup";

        /// <summary>
        /// Initializes static members of the DropDownButton class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the DropDownButton control uses its own default
        /// style by associating the control with its style metadata. This is required for custom controls to apply
        /// their styles correctly in WPF.</remarks>
        static DropDownButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DropDownButton),
                new FrameworkPropertyMetadata(typeof(DropDownButton)));
        }

        /// <summary>
        /// Identifies the <see cref="Flyout"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutProperty =
            DependencyProperty.Register(
                nameof(Flyout),
                typeof(object),
                typeof(DropDownButton),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the content displayed in the dropdown popup.
        /// </summary>
        public object Flyout
        {
            get => GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FlyoutTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutTemplateProperty =
            DependencyProperty.Register(
                nameof(FlyoutTemplate),
                typeof(DataTemplate),
                typeof(DropDownButton),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to render <see cref="Flyout"/>.
        /// </summary>
        public DataTemplate FlyoutTemplate
        {
            get => (DataTemplate)GetValue(FlyoutTemplateProperty);
            set => SetValue(FlyoutTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        /// <remarks>
        /// Shadows the <c>Control.CornerRadiusProperty</c> introduced in net6+ so the property
        /// is also available on net472 where <c>Control</c> does not declare it.
        /// </remarks>
        public static new readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(DropDownButton),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the button border.
        /// </summary>
        public new CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropdownCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropdownCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(DropdownCornerRadius),
                typeof(CornerRadius),
                typeof(DropDownButton),
                new FrameworkPropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the dropdown popup surface.
        /// </summary>
        public CornerRadius DropdownCornerRadius
        {
            get => (CornerRadius)GetValue(DropdownCornerRadiusProperty);
            set => SetValue(DropdownCornerRadiusProperty, value);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DropDownButtonAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _popup?.Closed -= OnPopupClosed;
            _popup = null;
            base.OnApplyTemplate();
            _popup = GetTemplateChild(PART_Popup) as Popup;
            if (_popup is not null)
            {
                _popup.PlacementTarget = this;
                AttachPopupHandlers(_popup);
                _popup.IsOpen = IsChecked == true;
            }
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == IsCheckedProperty && _popup is not null)
            {
                _popup.IsOpen = IsChecked == true;
            }
        }

        /// <summary>
        /// Configures popup behavior: it does not stay open without focus, and closing unchecks the button.
        /// </summary>
        /// <param name="popup">The template <see cref="Popup"/> (PART_Popup).</param>
        private void AttachPopupHandlers(Popup popup)
        {
            popup.StaysOpen = false;
            popup.Closed -= OnPopupClosed;
            popup.Closed += OnPopupClosed;
        }

        private void OnPopupClosed(object? sender, System.EventArgs e)
        {
            IsChecked = false;
        }

        /// <summary>
        /// Represents the underlying popup control instance, or null if no popup is currently associated.
        /// </summary>
        private Popup? _popup;
    }
}
