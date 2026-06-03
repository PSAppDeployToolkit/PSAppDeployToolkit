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

using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A section header element for use inside a <see cref="NavigationView"/> pane.
    /// Not selectable -- passes through the item container generator unmodified.
    /// </summary>
    public class NavigationViewItemHeader : ContentControl
    {
        /// <summary>
        /// Initializes static members of the NavigationViewItemHeader class and overrides default style and focus
        /// behavior.
        /// </summary>
        /// <remarks>This static constructor sets the default style key and marks the control as not
        /// focusable by default. These overrides ensure that NavigationViewItemHeader uses its custom style and cannot
        /// receive keyboard focus.</remarks>
        static NavigationViewItemHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NavigationViewItemHeader),
                new FrameworkPropertyMetadata(typeof(NavigationViewItemHeader)));

            FocusableProperty.OverrideMetadata(
                typeof(NavigationViewItemHeader),
                new FrameworkPropertyMetadata(false));
        }
    }
}
