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
    /// Exposes <see cref="RatingControl"/> to UI Automation as a slider with range value.
    /// Implements <see cref="IRangeValueProvider"/> so assistive technologies such as Narrator
    /// can read and set the rating value.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="RatingControlAutomationPeer"/> class.</remarks>
    /// <param name="owner">The <see cref="RatingControl"/> control represented by this automation peer.</param>
    public class RatingControlAutomationPeer(RatingControl owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(RatingControl);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Slider;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not PatternInterface.RangeValue
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual double Value => RatingControl.Value;

        /// <inheritdoc />
        public virtual double Minimum => 0d;

        /// <inheritdoc />
        public virtual double Maximum => RatingControl.MaxRating;

        /// <inheritdoc />
        public virtual double SmallChange => 1d;

        /// <inheritdoc />
        public virtual double LargeChange => 1d;

        /// <inheritdoc />
        public virtual bool IsReadOnly => RatingControl.IsReadOnly || !RatingControl.IsEnabled;

        /// <inheritdoc />
        /// <exception cref="ElementNotEnabledException">The control is disabled.</exception>
        /// <exception cref="System.InvalidOperationException">The control is read-only.</exception>
        public virtual void SetValue(double value)
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            if (RatingControl.IsReadOnly)
            {
                throw new System.InvalidOperationException("The rating control is read-only and its value cannot be set.");
            }

            RatingControl.Value = value;
        }

        /// <summary>
        /// Raises the <see cref="RangeValuePatternIdentifiers.ValueProperty"/> property-changed event
        /// so assistive technologies are notified when the rating value changes.
        /// </summary>
        /// <param name="oldValue">The previous rating value.</param>
        /// <param name="newValue">The new rating value.</param>
        internal void RaiseValueChanged(double oldValue, double newValue)
        {
            RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
        }

        /// <summary>
        /// Gets the associated <see cref="RatingControl"/> that owns this peer.
        /// </summary>
        private RatingControl RatingControl => (RatingControl)Owner;
    }
}
