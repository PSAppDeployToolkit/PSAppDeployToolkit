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
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A <see cref="SplitButton"/> whose primary half toggles a checked state instead of
    /// firing a plain action: clicking it flips <see cref="IsChecked"/> and then raises
    /// <see cref="SplitButton.Click"/>, while the secondary "chevron" half still opens the
    /// flyout. The canonical WinUI 3 ToggleSplitButton pattern, used when the primary half
    /// switches a mode on or off and the flyout chooses which variant of that mode applies.
    /// </summary>
    public class ToggleSplitButton : SplitButton
    {
        /// <summary>
        /// Initializes static members of the ToggleSplitButton class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ToggleSplitButton control uses its custom
        /// style by associating the control with its default style key. This enables the control to be styled
        /// appropriately in XAML themes.</remarks>
        static ToggleSplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ToggleSplitButton),
                new FrameworkPropertyMetadata(typeof(ToggleSplitButton)));
        }

        /// <summary>
        /// Identifies the <see cref="IsChecked"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool),
                typeof(ToggleSplitButton),
                new FrameworkPropertyMetadata(
                    defaultValue: false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsCheckedPropertyChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the toggle split button is checked.
        /// A primary-half click flips this value before <see cref="SplitButton.Click"/> is raised.
        /// </summary>
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        /// <summary>
        /// Raised when <see cref="IsChecked"/> changes, whether from a primary-half click,
        /// the Toggle automation pattern, a binding, or a direct property set.
        /// </summary>
        /// <remarks>
        /// Unlike WinUI, the event also fires for values applied before the control is
        /// loaded (for example markup-set initial values), matching how the WPF
        /// <see cref="System.Windows.Controls.Primitives.ToggleButton"/> raises its
        /// Checked and Unchecked events.
        /// </remarks>
        public event EventHandler<ToggleSplitButtonIsCheckedChangedEventArgs>? IsCheckedChanged;

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToggleSplitButtonAutomationPeer(this);
        }

        /// <summary>
        /// Toggles <see cref="IsChecked"/> and then runs the inherited click behavior,
        /// mirroring the WinUI ToggleSplitButton primary-click contract: a
        /// <see cref="SplitButton.Click"/> handler observes the already-flipped state.
        /// </summary>
        /// <param name="sender">The primary button template part that raised the click.</param>
        /// <param name="e">The routed event data from the primary button.</param>
        protected override void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            Toggle();
            base.OnPrimaryButtonClick(sender, e);
        }

        /// <summary>
        /// Flips <see cref="IsChecked"/> without clearing bindings or consumer-set local values.
        /// </summary>
        internal void Toggle()
        {
            SetCurrentValue(IsCheckedProperty, !IsChecked);
        }

        private void RaiseIsCheckedChanged(bool newValue)
        {
            IsCheckedChanged?.Invoke(this, new ToggleSplitButtonIsCheckedChangedEventArgs(newValue));
        }

        private static void OnIsCheckedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ToggleSplitButton button)
            {
                return;
            }

            bool newValue = (bool)e.NewValue;
            button.RaiseIsCheckedChanged(newValue);

            // Mirror WinUI: notify UI Automation clients that the Toggle pattern state changed.
            if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged)
                && UIElementAutomationPeer.FromElement(button) is AutomationPeer peer)
            {
                ToggleState oldState = (bool)e.OldValue ? ToggleState.On : ToggleState.Off;
                ToggleState newState = newValue ? ToggleState.On : ToggleState.Off;
                peer.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldState, newState);
            }
        }
    }
}
