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
using System.Windows.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="NavigationViewItem"/> to UI Automation as a selectable list item.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    /// <param name="owner">The <see cref="NavigationViewItem"/> control represented by this automation peer.</param>
    public sealed class NavigationViewItemAutomationPeer(NavigationViewItem owner) : FrameworkElementAutomationPeer(owner), ISelectionItemProvider, IInvokeProvider
    {
        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "NavigationViewItem";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ListItem;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface is not (PatternInterface.SelectionItem or PatternInterface.Invoke)
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public bool IsSelected => NavigationViewItem.IsSelected;

        /// <inheritdoc />
        public IRawElementProviderSimple? SelectionContainer => ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) is NavigationView nav
            ? ProviderFromPeer(CreatePeerForElement(nav))
            : null;

        /// <inheritdoc />
        public void AddToSelection()
        {
            SelectItem();
        }

        /// <inheritdoc />
        public void RemoveFromSelection()
        {
        }

        /// <inheritdoc />
        public void Invoke()
        {
            if (ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) is NavigationView nav)
            {
                nav.InvokeItem(NavigationViewItem);
            }
        }

        void ISelectionItemProvider.Select()
        {
            SelectItem();
        }

        /// <summary>
        /// Selects the associated navigation view item.
        /// </summary>
        public void SelectItem()
        {
            if (ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) is NavigationView nav)
            {
                nav.SelectItemFromContainer(NavigationViewItem);
            }
        }

        /// <summary>
        /// Gets the associated NavigationViewItem that owns this instance.
        /// </summary>
        private NavigationViewItem NavigationViewItem => (NavigationViewItem)Owner;
    }
}
