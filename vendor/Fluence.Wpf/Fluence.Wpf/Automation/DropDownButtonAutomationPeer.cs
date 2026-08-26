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
    /// Exposes <see cref="DropDownButton"/> to UI Automation with the ExpandCollapse pattern.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="DropDownButton"/> control represented by this automation peer.</param>
    public class DropDownButtonAutomationPeer(DropDownButton owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "DropDownButton";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not PatternInterface.ExpandCollapse
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual ExpandCollapseState ExpandCollapseState => DropDownButton.IsChecked is true
            ? ExpandCollapseState.Expanded
            : ExpandCollapseState.Collapsed;

        /// <inheritdoc />
        public virtual void Expand()
        {
            DropDownButton.IsChecked = true;
        }

        /// <inheritdoc />
        public virtual void Collapse()
        {
            DropDownButton.IsChecked = false;
        }

        /// <summary>
        /// Gets the associated drop-down button for this element.
        /// </summary>
        private DropDownButton DropDownButton => (DropDownButton)Owner;
    }
}
