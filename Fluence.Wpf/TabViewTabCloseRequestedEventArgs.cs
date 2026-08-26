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
using System.Windows;

namespace Fluence.Wpf
{
    /// <summary>
    /// Event data for <see cref="TabView.TabCloseRequested"/> and <see cref="TabViewItem.CloseRequested"/>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="TabViewTabCloseRequestedEventArgs"/> class.
    /// </remarks>
    /// <param name="routedEvent">The routed event being raised.</param>
    /// <param name="source">The element raising the event.</param>
    /// <param name="tab">The <see cref="TabViewItem"/> the user has asked to close.</param>
    /// <param name="item">The bound data item, or the <see cref="TabViewItem"/> itself if no data was bound.</param>
    public class TabViewTabCloseRequestedEventArgs(RoutedEvent routedEvent, object source, TabViewItem tab, object item) : RoutedEventArgs(routedEvent, source)
    {
        /// <summary>
        /// Gets the tab container the user asked to close.
        /// </summary>
        public TabViewItem Tab { get; } = tab;

        /// <summary>
        /// Gets the data item bound to <see cref="Tab"/>, or the tab itself when items are declared inline.
        /// </summary>
        public object Item { get; } = item;
    }
}
