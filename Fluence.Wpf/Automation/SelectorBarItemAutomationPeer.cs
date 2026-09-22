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

using System.Windows.Automation.Peers;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="SelectorBarItem"/> to UI Automation with the item's
    /// <see cref="SelectorBarItem.Text"/> as its name.
    /// </summary>
    /// <remarks>
    /// The base <see cref="ListBoxItemWrapperAutomationPeer"/> derives the name from the item's
    /// content, which a SelectorBar item leaves unused: the label lives on
    /// <see cref="SelectorBarItem.Text"/> instead, following WinUI's own item surface. Without
    /// this peer a text-only item announces nothing.
    /// </remarks>
    /// <param name="owner">The <see cref="SelectorBarItem"/> represented by this automation peer.</param>
    public class SelectorBarItemAutomationPeer(SelectorBarItem owner) : ListBoxItemWrapperAutomationPeer(owner)
    {
        /// <summary>
        /// The class and localized control type name WinUI reports for the item.
        /// </summary>
        private const string SelectorBarItemControlType = "SelectorBarItem";

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return SelectorBarItemControlType;
        }

        /// <inheritdoc />
        /// <remarks>
        /// WinUI's own peer resolves the name in four steps (SelectorBarItemAutomationPeer.cpp):
        /// an explicit automation name, then <see cref="SelectorBarItem.Text"/>, then the string
        /// form of the item's child content, then the control type name. This peer follows the
        /// same order, so an icon-only item still announces something.
        /// </remarks>
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            SelectorBarItem item = (SelectorBarItem)Owner;
            string? text = item.Text;
            if (text is not null && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string? content = item.Content?.ToString();
            return content is not null && !string.IsNullOrWhiteSpace(content) ? content : SelectorBarItemControlType;
        }

        /// <inheritdoc />
        /// <remarks>
        /// WinUI reports "SelectorBarItem" as the localized control type rather than letting the
        /// item fall back to the generic list-item announcement.
        /// </remarks>
        protected override string GetLocalizedControlTypeCore()
        {
            return SelectorBarItemControlType;
        }
    }
}
