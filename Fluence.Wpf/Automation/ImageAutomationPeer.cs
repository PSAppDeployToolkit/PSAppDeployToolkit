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

using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="Controls.Image"/> to UI Automation as an image element, but only once the
    /// consumer has given it an accessible name. An unnamed image is treated as decorative and is
    /// dropped from both the control and content views, so assistive technology never announces a
    /// bare unlabelled image element.
    /// Authority: WinUI keeps <c>Image</c> at <c>AccessibilityView="Raw"</c> until it is named, and
    /// Microsoft's UI Automation guidance is that decorative graphics stay out of the tree entirely.
    /// In-tree precedent: <see cref="FontIconAutomationPeer"/> for the both-views exclusion, and
    /// <see cref="TextBlockAutomationPeer"/> for keying that exclusion off the accessible name.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="ImageAutomationPeer"/> class.</remarks>
    /// <param name="owner">The <see cref="Controls.Image"/> control represented by this automation peer.</param>
    public class ImageAutomationPeer(Controls.Image owner) : FrameworkElementAutomationPeer(owner)
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(Controls.Image);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Image;
        }

        /// <inheritdoc />
        protected override bool IsControlElementCore()
        {
            return HasAccessibleName();
        }

        /// <inheritdoc />
        protected override bool IsContentElementCore()
        {
            return HasAccessibleName();
        }

        /// <summary>
        /// Gets a value indicating whether the consumer has given this image an accessible name,
        /// either directly through <see cref="AutomationProperties.NameProperty"/> or indirectly
        /// through <see cref="AutomationProperties.LabeledByProperty"/>.
        /// <para>
        /// Both views are gated on this. An image differs from a <see cref="TextBlockAutomationPeer"/>,
        /// which leaves the content view alone: a text element still carries its own readable text
        /// when unnamed, whereas an unnamed image carries nothing, so leaving it in the content view
        /// would make a screen reader announce an empty image. That is the noise this control exists
        /// to avoid.
        /// </para>
        /// <para>
        /// <see cref="AutomationProperties.LabeledByProperty"/> is honoured as well as
        /// <see cref="AutomationProperties.NameProperty"/> because labelling by another element is a
        /// legitimate way to name an image, and the base peer already resolves its name through it.
        /// Checking only the name would silently drop a properly labelled image out of the tree.
        /// </para>
        /// </summary>
        /// <returns><see langword="true"/> when the image has an accessible name; otherwise <see langword="false"/>.</returns>
        private bool HasAccessibleName()
        {
            return !string.IsNullOrWhiteSpace(AutomationProperties.GetName(Owner))
                || AutomationProperties.GetLabeledBy(Owner) is not null;
        }
    }
}
