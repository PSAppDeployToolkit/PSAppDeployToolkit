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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-5B.1 tests: Fluent Menu style.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-5B.1  Menu
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void Menu_StyleApplies_BackgroundIsTransparent()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Menu menu = new();
                _ = menu.Items.Add(new MenuItem { Header = "File" });
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Background must be Transparent (from style setter)
                SolidColorBrush? bg = menu.Background as SolidColorBrush;
                Assert.IsNotNull(bg, "Menu.Background must be a SolidColorBrush.");
                Assert.AreEqual(
                    Colors.Transparent,
                    bg.Color,
                    "Menu.Background must be Transparent per Fluent WI-5B.1 style.");
                w.Close();
            });
        }

        [TestMethod]
        public void Menu_StyleApplies_BorderThicknessIsZero()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Menu menu = new();
                _ = menu.Items.Add(new MenuItem { Header = "Edit" });
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(
                    new Thickness(0),
                    menu.BorderThickness,
                    "Menu.BorderThickness must be 0 per Fluent WI-5B.1 style.");
                w.Close();
            });
        }

        [TestMethod]
        public void Menu_AcceptsMenuItemItems()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Menu menu = new();
                MenuItem item1 = new() { Header = "File" };
                MenuItem item2 = new() { Header = "Edit" };
                _ = menu.Items.Add(item1);
                _ = menu.Items.Add(item2);
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(2, menu.Items.Count, "Menu must hold the two added Fluence MenuItem items.");
                Assert.IsInstanceOfType(menu.Items[0], typeof(MenuItem),
                    "Items[0] must be Fluence.Wpf.Controls.MenuItem.");
                Assert.IsInstanceOfType(menu.Items[1], typeof(MenuItem),
                    "Items[1] must be Fluence.Wpf.Controls.MenuItem.");
                w.Close();
            });
        }

        [TestMethod]
        public void Menu_ThemeCycle_BackgroundRemainsTransparent()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Menu menu = new();
                _ = menu.Items.Add(new MenuItem { Header = "View" });
                Window w = new() { Content = menu, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? bg = menu.Background as SolidColorBrush;
                Assert.IsNotNull(bg, "Menu.Background must remain a SolidColorBrush after theme cycle.");
                Assert.AreEqual(
                    Colors.Transparent,
                    bg.Color,
                    "Menu.Background must remain Transparent after theme cycle.");
                w.Close();
            });
        }
    }
}
