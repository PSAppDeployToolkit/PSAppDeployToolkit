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
    /// Exposes <see cref="AutoSuggestBox"/> to UI Automation as an edit control with the
    /// Value pattern surfacing its <see cref="AutoSuggestBox.Text"/>.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="AutoSuggestBox"/> control represented by this automation peer.</param>
    public class AutoSuggestBoxAutomationPeer(AutoSuggestBox owner) : FrameworkElementAutomationPeer(owner), IValueProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "AutoSuggestBox";
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string baseName = base.GetNameCore();
            return !string.IsNullOrWhiteSpace(baseName)
                ? baseName
                : AutoSuggestBox.Header?.ToString() ?? string.Empty;
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not PatternInterface.Value
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual string Value => AutoSuggestBox.Text;

        /// <summary>
        /// Always <see langword="false"/>. <see cref="AutoSuggestBox"/> has no read-only mode;
        /// disabled state is conveyed via <see cref="System.Windows.UIElement.IsEnabled"/>,
        /// not <see cref="IValueProvider.IsReadOnly"/>.
        /// </summary>
        public virtual bool IsReadOnly => false;

        /// <inheritdoc />
        /// <exception cref="ElementNotEnabledException">The control is disabled.</exception>
        public virtual void SetValue(string value)
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            AutoSuggestBox.Text = value;
        }

        /// <summary>
        /// Gets the associated AutoSuggestBox control that owns this instance.
        /// </summary>
        private AutoSuggestBox AutoSuggestBox => (AutoSuggestBox)Owner;
    }
}
