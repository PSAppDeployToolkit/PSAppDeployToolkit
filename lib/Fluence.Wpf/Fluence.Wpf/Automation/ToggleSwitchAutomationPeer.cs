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
    /// Exposes <see cref="ToggleSwitch"/> to UI Automation with the Toggle pattern.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="ToggleSwitch"/> control represented by this automation peer.</param>
    public class ToggleSwitchAutomationPeer(ToggleSwitch owner) : FrameworkElementAutomationPeer(owner), IToggleProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "ToggleSwitch";
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string baseName = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(baseName))
            {
                return baseName;
            }

            // Access HeaderContent inline (not via a local): a local would let CA1508 pin its
            // non-null flow state and flag the ?. below as dead, but the DP default is null so the
            // header can be null at runtime. Re-reading the property keeps the ?. valid and safe,
            // mirroring NumberBoxAutomationPeer.
            return ToggleSwitch.HeaderContent switch
            {
                string s => s,
                System.Windows.Controls.TextBlock tb => tb.Text,
                _ => ToggleSwitch.HeaderContent?.ToString() ?? string.Empty,
            };
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not PatternInterface.Toggle
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual ToggleState ToggleState => ToggleSwitch.IsChecked is bool isChecked
            ? !isChecked ? ToggleState.Off : ToggleState.On
            : ToggleState.Indeterminate;

        /// <inheritdoc />
        public virtual void Toggle()
        {
            bool? current = ToggleSwitch.IsChecked;
            ToggleSwitch.IsChecked = current is not true;
        }

        /// <summary>
        /// Gets the associated ToggleSwitch control that owns this element.
        /// </summary>
        private ToggleSwitch ToggleSwitch => (ToggleSwitch)Owner;
    }
}
