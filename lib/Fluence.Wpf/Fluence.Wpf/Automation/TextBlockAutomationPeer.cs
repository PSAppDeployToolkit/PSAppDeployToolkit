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

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="TextBlock"/> to UI Automation as a text element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TextBlock"/> wraps a <see cref="System.Windows.Controls.TextBlock"/> inside a
    /// <see cref="System.Windows.Controls.ContentControl"/> template to support the Fluent typography
    /// ramp. Without this peer, WPF creates a generic peer that reports <c>ControlType.Pane</c>,
    /// placing a spurious container in the UIA tree and breaking
    /// <c>AutomationProperties.LabeledBy</c> relationships that expect <c>ControlType.Text</c>.
    /// </para>
    /// <para>
    /// Only <see cref="TextBlock"/> instances with an explicit
    /// <see cref="AutomationProperties.NameProperty"/> are visible in the UIA control view.
    /// Instances without a name are excluded so decorative body-copy text is not announced;
    /// the name is never derived from <see cref="TextBlock.Text"/> automatically.
    /// </para>
    /// </remarks>
    /// <param name="owner">The <see cref="TextBlock"/> control represented by this automation peer.</param>
    public class TextBlockAutomationPeer(TextBlock owner) : FrameworkElementAutomationPeer(owner)
    {
        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(TextBlock);
        }

        /// <inheritdoc />
        protected override bool IsControlElementCore()
        {
            return !string.IsNullOrWhiteSpace(AutomationProperties.GetName(Owner));
        }
    }
}
