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
using Fluence.Wpf.Demo.Pages;

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// The single source of truth for the gallery pages: their navigation entries, order, routes,
    /// and page factories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every entry in <see cref="Items"/> drives one row in the left-pane <c language="csharp">NavigationView</c>,
    /// in array order, and <c language="csharp">MainWindow</c> creates the page through the entry's
    /// <see cref="DemoNavigationItem.CreatePage"/> factory. The gallery shell and sample contract tests
    /// read their page lists from this catalog too. To add a gallery page, add one
    /// <see cref="DemoNavigationItem"/> to the <c language="csharp">CatalogItems</c> array below.
    /// </para>
    /// <para>
    /// The Settings page is not in <see cref="Items"/> because it lives in the
    /// <c language="xaml">NavigationView.FooterMenuItems</c> slot. Its entry is <see cref="SettingsItem"/>.
    /// </para>
    /// </remarks>
    public static class DemoNavigationCatalog
    {
        private static readonly DemoNavigationItem[] CatalogItems =
        [
            DemoNavigationItem.Create("Home", "home", "overview welcome start", "\uE80F", isDefault: true, static _ => new GalleryHomePage()),
            DemoNavigationItem.Create("Colors", "colors", "color brush swatch theme resource high contrast accent", "\uE790", isDefault: false, static _ => new GalleryColorsPage()),
            DemoNavigationItem.Create("Icons", "icons", "fonticon icon segoe fluent symbols", "\uED58", isDefault: false, static _ => new GalleryIconsPage()),
            DemoNavigationItem.Create("Typography", "typography", "text textblock font style type ramp", "\uE8D2", isDefault: false, static _ => new GalleryTypographyPage()),
            DemoNavigationItem.Create("Buttons", "buttons", "button dropdownbutton splitbutton hyperlinkbutton repeatbutton togglebutton togglesplitbutton splittogglebutton accent icon", "\uE8E5", isDefault: false, static _ => new GalleryButtonsPage()),
            DemoNavigationItem.Create("Selection", "selection", "checkbox radio radiobutton toggleswitch combobox rating slider", "\uE73E", isDefault: false, static _ => new GallerySelectionPage()),
            DemoNavigationItem.Create("Inputs", "inputs", "textbox passwordbox numberbox slider text input validation", "\uE70F", isDefault: false, static _ => new GalleryInputsPage()),
            DemoNavigationItem.Create("Forms", "forms", "signin checkout settings form text input", "\uE7C3", isDefault: false, static _ => new GalleryFormsPage()),
            DemoNavigationItem.Create("Data", "data", "card listbox listview collection empty state", "\uE8FD", isDefault: false, static _ => new GalleryDataPage()),
            DemoNavigationItem.Create("DataGrid", "data grid", "datagrid table tableview grid rows columns sort group filter edit validation", "\uE80A", isDefault: false, static _ => new GalleryDataGridPage()),
            DemoNavigationItem.Create("Data binding", "data binding", "observablecollection binding selection datatemplate", "\uE8FD", isDefault: false, static _ => new GalleryDataBindingPage()),
            DemoNavigationItem.Create("Trees", "trees", "treeview hierarchy expand collapse selection", "\uE8EB", isDefault: false, static _ => new GalleryTreesPage()),
            DemoNavigationItem.Create("Menus", "menus", "menu menubar contextmenu tooltip dropdown split flyout command dialog message box contentdialog teachingtip commandbar", "\uE115", isDefault: false, static _ => new GalleryMenusPage()),
            DemoNavigationItem.Create("Navigation", "navigation", "navigationview pane left top compact", "\uE700", isDefault: false, static _ => new GalleryNavigationPage()),
            DemoNavigationItem.Create("Tabs", "tabs", "tabcontrol tabview tab tabs document close add", "\uF22C", isDefault: false, static _ => new GalleryTabsPage()),
            DemoNavigationItem.Create("Layout", "layout", "border dockpanel stackpanel expander separator layout surface", "\uECA5", isDefault: false, static _ => new GalleryLayoutPage()),
            DemoNavigationItem.Create("Status", "status", "infobar infobadge progressbar progressring personpicture progress ring busy", "\uE916", isDefault: false, static _ => new GalleryStatusPage()),
            DemoNavigationItem.Create("Accessibility", "accessibility", "screen reader narrator automation keyboard focus contrast rtl", "\uE776", isDefault: false, static _ => new GalleryAccessibilityPage()),
        ];

        /// <summary>
        /// Gets the ordered sequence of navigation items that populate the gallery left pane.
        /// </summary>
        public static IEnumerable<DemoNavigationItem> Items => CatalogItems;

        /// <summary>
        /// Gets the Settings entry. The shell shows it in the navigation footer, not in <see cref="Items"/>,
        /// and passes itself to the page so Settings can change the shell's backdrop and chrome.
        /// </summary>
        public static DemoNavigationItem SettingsItem { get; } =
            DemoNavigationItem.Create("Settings", "settings", "settings options preferences", "", isDefault: false, static owner => new GallerySettingsPage(owner));
    }
}
