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
    /// Exposes <see cref="ProgressRing"/> to UI Automation as a progress indicator.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="ProgressRing"/> control represented by this automation peer.</param>
    public class ProgressRingAutomationPeer(ProgressRing owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "ProgressRing";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ProgressBar;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not PatternInterface.RangeValue || ProgressRing.IsIndeterminate
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual double Value => ProgressRing.Value;

        /// <inheritdoc />
        public virtual double Minimum => ProgressRing.Minimum;

        /// <inheritdoc />
        public virtual double Maximum => ProgressRing.Maximum;

        /// <inheritdoc />
        public virtual double SmallChange => 1;

        /// <inheritdoc />
        public virtual double LargeChange => 10;

        /// <inheritdoc />
        public virtual bool IsReadOnly => true;

        /// <inheritdoc />
        public virtual void SetValue(double value)
        {
        }

        /// <summary>
        /// Raises the <see cref="RangeValuePatternIdentifiers.ValueProperty"/> property-changed event
        /// so UI Automation clients can read the current progress value on demand.
        /// Only meaningful in determinate mode; <see cref="GetPattern"/> already suppresses
        /// <see cref="PatternInterface.RangeValue"/> when the ring is indeterminate.
        /// </summary>
        /// <param name="oldValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        internal void RaiseValuePropertyChangedEvent(double oldValue, double newValue)
        {
            RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
        }

        /// <summary>
        /// Gets the associated ProgressRing control instance.
        /// </summary>
        private ProgressRing ProgressRing => (ProgressRing)Owner;
    }
}
