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
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="ToggleSplitButton"/> to UI Automation with the Toggle pattern
    /// (primary half) and the ExpandCollapse pattern (flyout half). The Invoke pattern is
    /// deliberately not offered: WinUI exposes only Toggle and ExpandCollapse on its
    /// ToggleSplitButton peer, and an Invoke routed through the SplitButton peer would
    /// raise Click without toggling, diverging from a real primary-half click.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="ToggleSplitButton"/> control represented by this automation peer.</param>
    public class ToggleSplitButtonAutomationPeer(ToggleSplitButton owner) : FrameworkElementAutomationPeer(owner), IToggleProvider, IExpandCollapseProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "ToggleSplitButton";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not (PatternInterface.Toggle or PatternInterface.ExpandCollapse)
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual ToggleState ToggleState => ToggleSplitButton.IsChecked
            ? ToggleState.On
            : ToggleState.Off;

        /// <inheritdoc />
        public virtual void Toggle()
        {
            ToggleSplitButton.Toggle();
        }

        /// <inheritdoc />
        public virtual ExpandCollapseState ExpandCollapseState => ToggleSplitButton.IsFlyoutOpen
            ? ExpandCollapseState.Expanded
            : ExpandCollapseState.Collapsed;

        /// <inheritdoc />
        public virtual void Expand()
        {
            // The read-only IsFlyoutOpen reflects the secondary ToggleButton, which is
            // part of the template; flipping the part opens the popup via the control's
            // Checked/Unchecked wiring. Without an applied template this is a no-op.
            ToggleSplitButton thisButton = ToggleSplitButton;
            System.Windows.Controls.Primitives.ToggleButton? toggle = thisButton.Template?.FindName("PART_SecondaryButton", thisButton) as System.Windows.Controls.Primitives.ToggleButton;
            _ = toggle?.IsChecked = true;
        }

        /// <inheritdoc />
        public virtual void Collapse()
        {
            ToggleSplitButton thisButton = ToggleSplitButton;
            System.Windows.Controls.Primitives.ToggleButton? toggle = thisButton.Template?.FindName("PART_SecondaryButton", thisButton) as System.Windows.Controls.Primitives.ToggleButton;
            _ = toggle?.IsChecked = false;
        }

        /// <summary>
        /// Gets the associated ToggleSplitButton control that owns this element.
        /// </summary>
        private ToggleSplitButton ToggleSplitButton => (ToggleSplitButton)Owner;
    }
}
