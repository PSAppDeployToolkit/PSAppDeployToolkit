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
    /// Exposes <see cref="NumberBox"/> to UI Automation as a spinner with range value.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="NumberBox"/> control represented by this automation peer.</param>
    public class NumberBoxAutomationPeer(NumberBox owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "NumberBox";
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string baseName = base.GetNameCore();
            return !string.IsNullOrWhiteSpace(baseName)
                ? baseName
                : NumberBox.Header?.ToString() ?? string.Empty;
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Spinner;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not PatternInterface.RangeValue
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual double Value => NumberBox.Value;

        /// <inheritdoc />
        public virtual double Minimum => NumberBox.Minimum;

        /// <inheritdoc />
        public virtual double Maximum => NumberBox.Maximum;

        /// <inheritdoc />
        public virtual double SmallChange => NumberBox.SmallChange;

        /// <inheritdoc />
        public virtual double LargeChange => NumberBox.LargeChange;

        /// <summary>
        /// Always <see langword="false"/>. <see cref="NumberBox"/> has no read-only mode;
        /// disabled state is conveyed via <see cref="System.Windows.UIElement.IsEnabled"/>,
        /// not <see cref="IRangeValueProvider.IsReadOnly"/>.
        /// </summary>
        public virtual bool IsReadOnly => false;

        /// <inheritdoc />
        /// <exception cref="ElementNotEnabledException">The control is disabled.</exception>
        public virtual void SetValue(double value)
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            NumberBox.Value = value;
        }

        /// <summary>
        /// Raises the <see cref="RangeValuePatternIdentifiers.ValueProperty"/> property-changed event
        /// so UI Automation clients (Narrator) observe the current value instead of a stale one.
        /// </summary>
        /// <param name="oldValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        internal virtual void RaiseValueChanged(double oldValue, double newValue)
        {
            RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
        }

        /// <summary>
        /// Gets the associated NumberBox control that owns this instance.
        /// </summary>
        private NumberBox NumberBox => (NumberBox)Owner;
    }
}
