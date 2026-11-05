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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;

namespace Fluence.Wpf.Tests.Gallery
{
    /// <summary>
    /// The gallery page lists that the shell and sample contract tests iterate. Every list is
    /// derived from <see cref="DemoNavigationCatalog"/>, so a page added to the catalog is covered
    /// without another registration. A page leaves a list only through an explicit exclusion here.
    /// </summary>
    internal static class GalleryPageCatalog
    {
        // Pages that render directly instead of through DemoSampleControl, because the WinUI
        // Gallery surface each one mirrors has no source expander: the Home landing page, the
        // Iconography catalog, and the Color page. The demo-sample-page SPEC.md catalog surfaces
        // section documents the Icons and Colors exceptions.
        private static readonly Type[] DirectRenderPageTypes =
        [
            typeof(GalleryHomePage),
            typeof(GalleryIconsPage),
            typeof(GalleryColorsPage),
        ];

        /// <summary>
        /// Gets the navigation pane entries in catalog order. Settings is excluded because it is a
        /// footer item that <see cref="MainWindow.NavigateTo"/> selects in the footer, not the pane.
        /// </summary>
        public static IReadOnlyList<DemoNavigationItem> NavigationItems { get; } = [.. DemoNavigationCatalog.Items];

        /// <summary>
        /// Gets every page the shell can show: the navigation pane entries, then the Settings footer entry.
        /// </summary>
        public static IReadOnlyList<DemoNavigationItem> AllItems { get; } = [.. NavigationItems, DemoNavigationCatalog.SettingsItem];

        /// <summary>
        /// Gets the navigation pane entries whose pages render their samples through
        /// <see cref="DemoSampleControl"/>: every pane entry except the direct-render pages.
        /// </summary>
        /// <exception cref="InvalidOperationException">An exclusion names a page that is no longer in the catalog.</exception>
        public static IReadOnlyList<DemoNavigationItem> SampleItems
        {
            get
            {
                foreach (Type excluded in DirectRenderPageTypes)
                {
                    if (!NavigationItems.Any(item => item.PageType == excluded))
                    {
                        throw new InvalidOperationException(
                            excluded.Name + " is excluded from the DemoSampleControl page list but is not in DemoNavigationCatalog. Remove the stale exclusion.");
                    }
                }

                return [.. NavigationItems.Where(static item => Array.IndexOf(DirectRenderPageTypes, item.PageType) < 0)];
            }
        }

        /// <summary>
        /// Creates a catalog entry's page outside the shell, as the page tests host it.
        /// </summary>
        /// <param name="item">The catalog entry.</param>
        /// <returns>A new page with no owning shell window.</returns>
        public static Page CreatePage(DemoNavigationItem item)
        {
            return item.CreatePage(owner: null);
        }
    }
}
