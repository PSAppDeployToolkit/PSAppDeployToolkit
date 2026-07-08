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
using System.Windows.Automation.Peers;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Automation peer for <see cref="FontIcon"/> that excludes the purely decorative glyph from the
    /// UI Automation control and content views, matching WinUI's <c>AccessibilityView="Raw"</c> behavior.
    /// The glyph carries no meaning of its own; the labelled parent control (for example a
    /// <see cref="System.Windows.Controls.Button"/> with an <c>AutomationProperties.Name</c>) is what
    /// screen readers announce.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="FontIcon"/> that owns this peer.</param>
    public class FontIconAutomationPeer(FontIcon owner) : FrameworkElementAutomationPeer(owner)
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(FontIcon);
        }

        /// <inheritdoc />
        protected override bool IsControlElementCore()
        {
            return false;
        }

        /// <inheritdoc />
        protected override bool IsContentElementCore()
        {
            return false;
        }
    }
}
