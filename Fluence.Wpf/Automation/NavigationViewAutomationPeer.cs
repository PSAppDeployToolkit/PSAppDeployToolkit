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
using System.Windows.Automation.Provider;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="NavigationView"/> to UI Automation as a selection list.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="NavigationView"/> control represented by this automation peer.</param>
    public class NavigationViewAutomationPeer(NavigationView owner) : FrameworkElementAutomationPeer(owner), ISelectionProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "NavigationView";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.List;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface != PatternInterface.Selection
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual bool CanSelectMultiple => false;

        /// <inheritdoc />
        public virtual bool IsSelectionRequired => false;

        /// <inheritdoc />
        public virtual IRawElementProviderSimple[] GetSelection()
        {
            if (NavigationView.SelectedFooterItem is NavigationViewItem footerContainer)
            {
                return [ProviderFromPeer(CreatePeerForElement(footerContainer))];
            }

            object selected = NavigationView.SelectedItem;
            return selected is not null && NavigationView.ItemContainerGenerator.ContainerFromItem(selected) is NavigationViewItem container
                ? [ProviderFromPeer(CreatePeerForElement(container))]
                : [];
        }

        /// <summary>
        /// Gets the associated NavigationView instance for the current owner.
        /// </summary>
        private NavigationView NavigationView => (NavigationView)Owner;
    }
}
