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

using System;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// Metadata for a single gallery page: its navigation pane entry and the factory that creates
    /// the page when the shell first shows it.
    /// </summary>
    public sealed class DemoNavigationItem
    {
        private readonly Func<MainWindow?, Page> _createPage;

        private DemoNavigationItem(string title, string route, string keywords, string glyph, bool isDefault, Type pageType, Func<MainWindow?, Page> createPage)
        {
            Title = title;
            Route = route;
            Keywords = keywords;
            Glyph = glyph;
            IsDefault = isDefault;
            PageType = pageType;
            _createPage = createPage;
        }

        /// <summary>
        /// Gets the display label shown in the navigation pane.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the route key that <see cref="MainWindow.NavigateTo"/> matches to select this page.
        /// Routes are lowercase and unique across the catalog and the Settings footer entry.
        /// </summary>
        public string Route { get; }

        /// <summary>
        /// Gets additional search terms (space-separated) that allow the search box to surface
        /// this page even when the user types a synonym not present in <see cref="Title"/>.
        /// </summary>
        public string Keywords { get; }

        /// <summary>
        /// Gets the Segoe Fluent Icons Unicode code point string for this item's icon.
        /// </summary>
        public string Glyph { get; }

        /// <summary>
        /// Gets a value indicating whether this item should be selected on first load. Only one
        /// item in the catalog should have this set to <see langword="true"/>.
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// Gets the type of the page that <see cref="CreatePage"/> returns.
        /// </summary>
        public Type PageType { get; }

        /// <summary>
        /// Creates a navigation entry whose page type is taken from the factory's return type, so
        /// the type and the factory cannot disagree.
        /// </summary>
        /// <typeparam name="TPage">The gallery page type.</typeparam>
        /// <param name="title">The display label shown in the navigation pane.</param>
        /// <param name="route">The route key that selects this page.</param>
        /// <param name="keywords">Additional search terms for the search box.</param>
        /// <param name="glyph">The Segoe Fluent Icons Unicode code point string for this item's icon.</param>
        /// <param name="isDefault">Indicates whether this item should be selected on first load.</param>
        /// <param name="createPage">Creates the page. The argument is the shell window, or <see langword="null"/> when the page is created outside the shell.</param>
        /// <returns>The navigation entry.</returns>
        public static DemoNavigationItem Create<TPage>(string title, string route, string keywords, string glyph, bool isDefault, Func<MainWindow?, TPage> createPage)
            where TPage : Page
        {
            ArgumentNullException.ThrowIfNull(createPage);
            return new DemoNavigationItem(title, route, keywords, glyph, isDefault, typeof(TPage), owner => createPage(owner));
        }

        /// <summary>
        /// Creates a new instance of this entry's page.
        /// </summary>
        /// <param name="owner">The shell window that hosts the page, or <see langword="null"/> when the page is created outside the shell.</param>
        /// <returns>A new page of type <see cref="PageType"/>.</returns>
        public Page CreatePage(MainWindow? owner)
        {
            return _createPage(owner);
        }
    }
}
