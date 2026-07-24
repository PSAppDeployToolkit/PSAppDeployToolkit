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
    /// Exposes <see cref="PersonPicture"/> to UI Automation as an image element with
    /// an accessible name derived from the control's identity text.
    /// Narrator announces who the avatar represents using <see cref="PersonPicture.DisplayName"/>
    /// when set, falling back to <see cref="PersonPicture.Initials"/>, and always deferring to
    /// an explicit <see cref="System.Windows.Automation.AutomationProperties.NameProperty"/> value first.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="PersonPictureAutomationPeer"/> class.</remarks>
    /// <param name="owner">The <see cref="PersonPicture"/> control represented by this automation peer.</param>
    public class PersonPictureAutomationPeer(PersonPicture owner) : FrameworkElementAutomationPeer(owner)
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(PersonPicture);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Image;
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            // Explicit AutomationProperties.Name wins; fall back to DisplayName then Initials.
            string baseName = base.GetNameCore();
            return !string.IsNullOrWhiteSpace(baseName)
                ? baseName
                : !string.IsNullOrWhiteSpace(PersonPicture.DisplayName)
                    ? PersonPicture.DisplayName
                    : !string.IsNullOrWhiteSpace(PersonPicture.Initials)
                        ? PersonPicture.Initials
                        : baseName;
        }

        /// <summary>
        /// Gets the associated <see cref="PersonPicture"/> that owns this peer.
        /// </summary>
        private PersonPicture PersonPicture => (PersonPicture)Owner;
    }
}
