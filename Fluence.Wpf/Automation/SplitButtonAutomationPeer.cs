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

using Fluence.Wpf.Controls;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="SplitButton"/> to UI Automation with the Invoke pattern
    /// (primary half) and the ExpandCollapse pattern (flyout half).
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="SplitButton"/> control represented by this automation peer.</param>
    public class SplitButtonAutomationPeer(SplitButton owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider, IExpandCollapseProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "SplitButton";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not (PatternInterface.Invoke or PatternInterface.ExpandCollapse)
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual ExpandCollapseState ExpandCollapseState => SplitButton.IsFlyoutOpen
            ? ExpandCollapseState.Expanded
            : ExpandCollapseState.Collapsed;

        /// <inheritdoc />
        public virtual void Expand()
        {
            // The read-only IsFlyoutOpen reflects the secondary ToggleButton, which is
            // part of the template. Automation clients opening a SplitButton without a
            // visual tree see no-op behavior; with a template applied, the overridden
            // PropertyChanged wiring flips the popup via the secondary button.
            SplitButton thisButton = SplitButton;
            System.Windows.Controls.Primitives.ToggleButton? toggle = thisButton.Template?.FindName("PART_SecondaryButton", thisButton) as System.Windows.Controls.Primitives.ToggleButton;
            _ = toggle?.IsChecked = true;
        }

        /// <inheritdoc />
        public virtual void Collapse()
        {
            SplitButton thisButton = SplitButton;
            System.Windows.Controls.Primitives.ToggleButton? toggle = thisButton.Template?.FindName("PART_SecondaryButton", thisButton) as System.Windows.Controls.Primitives.ToggleButton;
            _ = toggle?.IsChecked = false;
        }

        /// <inheritdoc />
        public virtual void Invoke()
        {
            // Route Invoke to the primary half by raising SplitButton.Click and executing Command.
            SplitButton button = SplitButton;
            button.RaiseEvent(new RoutedEventArgs(SplitButton.ClickEvent, button));
            if (button.Command is not ICommand command)
            {
                return;
            }

            object parameter = button.CommandParameter;
            if (command is RoutedCommand routedCommand)
            {
                IInputElement target = button.CommandTarget;
                if (routedCommand.CanExecute(parameter, target))
                {
                    routedCommand.Execute(parameter, target);
                }
            }
            else if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        /// <summary>
        /// Gets the associated SplitButton control that owns this element.
        /// </summary>
        private SplitButton SplitButton => (SplitButton)Owner;
    }
}
