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
using System;
using System.Globalization;
using System.Windows.Automation.Peers;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="DatePicker"/> to UI Automation as a group whose name reflects the
    /// selected date in the current culture's short date format, falling back to
    /// <see cref="DatePicker.PlaceholderText"/> while no date is selected. The inner field
    /// button and selector columns surface their own interaction patterns.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="DatePicker"/> control represented by this automation peer.</param>
    public class DatePickerAutomationPeer(DatePicker owner) : FrameworkElementAutomationPeer(owner)
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "DatePicker";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            DateTime? selectedDate = DatePicker.SelectedDate;
            return selectedDate.HasValue
                ? selectedDate.Value.ToString("d", CultureInfo.CurrentCulture)
                : DatePicker.PlaceholderText;
        }

        /// <summary>
        /// Gets the associated DatePicker control that owns this instance.
        /// </summary>
        private DatePicker DatePicker => (DatePicker)Owner;
    }
}
