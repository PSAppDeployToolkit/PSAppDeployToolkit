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

namespace Fluence.Wpf.Markup
{
    /// <summary>
    /// A theme-reactive resource reference equivalent to the WinUI 3 <c>{ThemeResource}</c>
    /// markup extension. Use it to reference a resource whose value must follow the active
    /// theme, such as any canonical Fluence color or brush token:
    /// <c>{fluence:ThemeResource TextFillColorPrimaryBrush}</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This extension derives from <see cref="DynamicResourceExtension"/> and shares its exact
    /// runtime behavior. Fluence republishes its computed color and brush dictionary on every
    /// theme or accent change, so any dynamic reference to a canonical token re-resolves
    /// automatically; this type exists so markup ported from WinUI 3 keeps its
    /// theme-versus-static intent readable. Unlike WinUI, WPF markup extensions always require
    /// an XML namespace prefix.
    /// </para>
    /// <para>
    /// As with WinUI, a <c>StaticResource</c> reference to a theme-dependent value does not
    /// update when the theme changes; use this extension (or <c>DynamicResource</c>) for any
    /// value that must react to theme, accent, or high contrast at runtime.
    /// </para>
    /// </remarks>
    public sealed class ThemeResourceExtension : DynamicResourceExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeResourceExtension"/> class.
        /// </summary>
        public ThemeResourceExtension()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeResourceExtension"/> class with
        /// the given resource key.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to reference.</param>
        public ThemeResourceExtension(object resourceKey)
            : base(resourceKey)
        {
        }
    }
}
