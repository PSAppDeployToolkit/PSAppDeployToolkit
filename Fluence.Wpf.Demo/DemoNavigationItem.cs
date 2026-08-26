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

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// Metadata for a single entry in the gallery's left navigation pane.
    /// </summary>
    /// <param name="title">The display label shown in the navigation pane.</param>
    /// <param name="route">The route key used to instantiate the correct gallery page.</param>
    /// <param name="keywords">Additional search terms for the search box.</param>
    /// <param name="glyph">The Segoe Fluent Icons Unicode code point string for this item's icon.</param>
    /// <param name="isDefault">Indicates whether this item should be selected on first load.</param>
    public sealed class DemoNavigationItem(string title, string route, string keywords, string glyph, bool isDefault)
    {
        /// <summary>
        /// Gets the display label shown in the navigation pane.
        /// </summary>
        public string Title { get; } = title;

        /// <summary>
        /// Gets the route key used by <c>MainWindow.CreatePageForRoute</c> to instantiate the
        /// correct gallery page. Must match a case in that switch exactly.
        /// </summary>
        public string Route { get; } = route;

        /// <summary>
        /// Gets additional search terms (space-separated) that allow the search box to surface
        /// this page even when the user types a synonym not present in <see cref="Title"/>.
        /// </summary>
        public string Keywords { get; } = keywords;

        /// <summary>
        /// Gets the Segoe Fluent Icons Unicode code point string for this item's icon.
        /// </summary>
        public string Glyph { get; } = glyph;

        /// <summary>
        /// Gets a value indicating whether this item should be selected on first load. Only one
        /// item in the catalog should have this set to <see langword="true"/>.
        /// </summary>
        public bool IsDefault { get; } = isDefault;
    }
}
