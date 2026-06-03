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

namespace Fluence.Wpf
{
    /// <summary>
    /// Provides data for the <see cref="NavigationView.ItemInvoked"/> event.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="NavigationViewItemInvokedEventArgs"/> class.
    /// </remarks>
    /// <param name="invokedItem">The data item that was invoked.</param>
    /// <param name="invokedItemContainer">The navigation item container that was invoked.</param>
    /// <param name="isSettingsInvoked">A value indicating whether the settings entry was invoked.</param>
    public class NavigationViewItemInvokedEventArgs(object invokedItem, NavigationViewItem invokedItemContainer, bool isSettingsInvoked) : EventArgs
    {
        /// <summary>
        /// Gets the data item that was invoked.
        /// </summary>
        public object InvokedItem { get; private set; } = invokedItem;

        /// <summary>
        /// Gets the navigation item container that was invoked.
        /// </summary>
        public NavigationViewItem InvokedItemContainer { get; private set; } = invokedItemContainer;

        /// <summary>
        /// Gets a value indicating whether the settings entry was invoked.
        /// </summary>
        public bool IsSettingsInvoked { get; private set; } = isSettingsInvoked;
    }
}
