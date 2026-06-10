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
    /// Event data for <see cref="BreadcrumbBar.ItemClicked"/>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="BreadcrumbBarItemClickedEventArgs"/> class.
    /// </remarks>
    /// <param name="item">The data item of the clicked crumb, or the <see cref="BreadcrumbBarItem"/> itself when it was added directly.</param>
    /// <param name="index">The zero-based position of the clicked crumb in the bar's items collection.</param>
    public sealed class BreadcrumbBarItemClickedEventArgs(object? item, int index) : EventArgs
    {
        /// <summary>
        /// Gets the data item of the clicked crumb, or the <see cref="BreadcrumbBarItem"/>
        /// itself when it was added directly to the items collection.
        /// </summary>
        public object? Item { get; } = item;

        /// <summary>
        /// Gets the zero-based position of the clicked crumb in the bar's items collection.
        /// </summary>
        public int Index { get; } = index;
    }
}
