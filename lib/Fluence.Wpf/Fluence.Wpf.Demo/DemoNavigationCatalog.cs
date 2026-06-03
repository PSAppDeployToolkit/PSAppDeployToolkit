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

using System.Collections.Generic;

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// Metadata for a single entry in the gallery's left navigation pane.
    /// </summary>
    public sealed class DemoNavigationItem(string title, string route, string keywords, string glyph, bool isDefault)
    {
        /// <summary>Gets the display label shown in the navigation pane.</summary>
        public string Title { get; private set; } = title;

        /// <summary>
        /// Gets the route key used by <c>MainWindow.CreatePageForRoute</c> to instantiate the
        /// correct gallery page. Must match a case in that switch exactly.
        /// </summary>
        public string Route { get; private set; } = route;

        /// <summary>
        /// Gets additional search terms (space-separated) that allow the search box to surface
        /// this page even when the user types a synonym not present in <see cref="Title"/>.
        /// </summary>
        public string Keywords { get; private set; } = keywords;

        /// <summary>Gets the Segoe Fluent Icons Unicode code point string for this item's icon.</summary>
        public string Glyph { get; private set; } = glyph;

        /// <summary>
        /// Gets a value indicating whether this item should be selected on first load. Only one
        /// item in the catalog should have this set to <see langword="true"/>.
        /// </summary>
        public bool IsDefault { get; private set; } = isDefault;
    }

    /// <summary>
    /// The single source of truth for the gallery navigation menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every entry in <see cref="Items"/> drives one row in the left-pane <c>NavigationView</c>.
    /// The <c>Route</c> property of each entry is the key that <c>MainWindow.CreatePageForRoute</c>
    /// uses in its switch to instantiate the matching gallery page. To add a new gallery page:
    /// <list type="number">
    ///   <item><description>Add a new <see cref="DemoNavigationItem"/> to the <c>CatalogItems</c> array below.</description></item>
    ///   <item><description>Add a corresponding <c>case</c> to <c>MainWindow.CreatePageForRoute</c>.</description></item>
    /// </list>
    /// The Settings page is intentionally absent from this list because it lives in the
    /// <c>NavigationView.FooterMenuItems</c> slot and is registered separately in
    /// <c>MainWindow.PopulateNavigation</c>.
    /// </para>
    /// </remarks>
    public static class DemoNavigationCatalog
    {
        private static readonly DemoNavigationItem[] CatalogItems =
        [
            new("Home", "home", "overview welcome start", "\uE80F", true),
            new("Colors", "colors", "color brush swatch theme resource high contrast accent", "\uE790", false),
            new("Icons", "icons", "fonticon icon segoe fluent symbols", "\uED58", false),
            new("Typography", "typography", "text textblock font style type ramp", "\uE8D2", false),
            new("Buttons", "buttons", "button dropdownbutton splitbutton hyperlinkbutton repeatbutton togglebutton accent icon", "\uE8E5", false),
            new("Selection", "selection", "checkbox radio radiobutton toggleswitch combobox rating slider", "\uE73E", false),
            new("Inputs", "inputs", "textbox passwordbox numberbox slider text input validation", "\uE70F", false),
            new("Forms", "forms", "signin checkout settings form text input", "\uE7C3", false),
            new("Data", "data", "card listbox listview collection empty state", "\uE8FD", false),
            new("Data binding", "data binding", "observablecollection binding selection datatemplate", "\uE8FD", false),
            new("Trees", "trees", "treeview hierarchy expand collapse selection", "\uE8EB", false),
            new("Menus", "menus", "menu menubar contextmenu tooltip dropdown split flyout command", "\uE115", false),
            new("Navigation", "navigation", "navigationview pane left top compact", "\uE700", false),
            new("Tabs", "tabs", "tabcontrol tabview tab tabs document close add", "\uF22C", false),
            new("Layout", "layout", "border dockpanel stackpanel expander separator layout surface", "\uECA5", false),
            new("Status", "status", "infobar infobadge progressbar progressring personpicture progress ring busy", "\uE916", false),
            new("Accessibility", "accessibility", "screen reader narrator automation keyboard focus contrast rtl", "\uE776", false)
        ];

        /// <summary>Gets the ordered sequence of navigation items that populate the gallery left pane.</summary>
        public static IEnumerable<DemoNavigationItem> Items => CatalogItems;
    }
}
