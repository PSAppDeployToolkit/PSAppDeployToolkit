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
using System.Windows.Shapes;
using WpfBorder = System.Windows.Controls.Border;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-6 tests: Fluent <see cref="PersonPicture"/>.
    /// Authority: WinUI 3 PersonPicture.xaml + PersonPicture_themeresources.xaml.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-6  PersonPicture
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void PersonPicture_DefaultStyle_Applies()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Background ellipse must be in visual tree
                Ellipse? ellipse = FindVisualChild<Ellipse>(pp);
                Assert.IsNotNull(ellipse, "PersonPicture template must contain an Ellipse.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_TemplateParts_Present()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText, "PART_InitialsText must be present.");

                Ellipse? imageEllipse = FindVisualChildByName<Ellipse>(pp, "PART_ImageEllipse");
                Assert.IsNotNull(imageEllipse, "PART_ImageEllipse must be present.");

                WpfGrid? badgeGrid = FindVisualChildByName<WpfGrid>(pp, "PART_BadgeGrid");
                Assert.IsNotNull(badgeGrid, "PART_BadgeGrid must be present.");

                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_NoData_ShowsPlaceholderGlyph()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // No DisplayName, no Initials, no ProfilePicture
                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                // Contact glyph U+E77B
                Assert.AreEqual("\uE77B", initialsText.Text,
                    "PersonPicture with no data must show contact glyph U+E77B.");
                StringAssert.Contains(initialsText.FontFamily.Source, "Segoe Fluent Icons",
                System.StringComparison.Ordinal, "The contact glyph must use the icon font, not the text font.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_DisplayName_GeneratesInitials()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "John Doe" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                Assert.AreEqual("JD", initialsText.Text,
                    "DisplayName='John Doe' must generate initials 'JD'.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_ExplicitInitials_Override()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "John Doe", Initials = "XY" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                Assert.AreEqual("XY", initialsText.Text,
                    "Explicit Initials='XY' must override DisplayName-derived initials.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_IsGroup_ShowsPeopleGlyph()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { IsGroup = true };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                Assert.AreEqual("\uE716", initialsText.Text,
                    "IsGroup=true must show people glyph U+E716 per WinUI 3 PersonPicture.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_BadgeNumber_MakesBadgeVisible()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { BadgeNumber = 3 };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfGrid? badgeGrid = FindVisualChildByName<WpfGrid>(pp, "PART_BadgeGrid");
                Assert.IsNotNull(badgeGrid);
                Assert.AreEqual(Visibility.Visible, badgeGrid.Visibility,
                    "BadgeNumber > 0 must make PART_BadgeGrid Visible.");

                WpfTextBlock? badgeText = FindVisualChildByName<WpfTextBlock>(pp, "PART_BadgeText");
                Assert.IsNotNull(badgeText);
                Assert.AreEqual("3", badgeText.Text,
                    "PART_BadgeText must display the BadgeNumber.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_BadgeBackground_CoversNumberAndGlyphContent()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { Width = 48, Height = 48, BadgeNumber = 150 };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();
                DrainDispatcher(w.Dispatcher);

                WpfGrid? badgeGrid = FindVisualChildByName<WpfGrid>(pp, "PART_BadgeGrid");
                WpfBorder? badgeBackground = FindVisualChildByName<WpfBorder>(pp, "PART_BadgeBackground");
                WpfTextBlock? badgeText = FindVisualChildByName<WpfTextBlock>(pp, "PART_BadgeText");
                Assert.IsNotNull(badgeGrid);
                Assert.IsNotNull(badgeBackground);
                Assert.IsNotNull(badgeText);
                Assert.AreEqual("99+", badgeText.Text);
                Assert.IsTrue(badgeGrid.ActualWidth >= badgeText.ActualWidth + 8.0,
                    "Numeric badges must use a pill surface wide enough to cover their rendered text.");
                Assert.IsTrue(badgeBackground.ActualWidth >= badgeGrid.ActualWidth,
                    "The badge background must cover the full badge layout width.");

                pp.BadgeNumber = 0;
                pp.BadgeGlyph = "\uE73E";
                DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual("\uE73E", badgeText.Text);
                Assert.IsTrue(badgeGrid.ActualWidth >= badgeText.ActualWidth + 8.0,
                    "Glyph badges must keep enough background around the rendered glyph.");
                Assert.IsTrue(badgeBackground.ActualWidth >= badgeGrid.ActualWidth,
                    "The badge background must cover the full badge layout width.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_NoBadge_BadgeCollapsed()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { BadgeNumber = 0 };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfGrid? badgeGrid = FindVisualChildByName<WpfGrid>(pp, "PART_BadgeGrid");
                Assert.IsNotNull(badgeGrid);
                Assert.AreEqual(Visibility.Collapsed, badgeGrid.Visibility,
                    "PART_BadgeGrid must be Collapsed when BadgeNumber=0 and BadgeGlyph=null.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_DefaultSize_Is40x40()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(40.0, pp.Width,
                    "PersonPicture default Width must be 40 per WinUI 3 PersonPicture spec.");
                Assert.AreEqual(40.0, pp.Height,
                    "PersonPicture default Height must be 40 per WinUI 3 PersonPicture spec.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_ThemeCycle_StyleRemainsApplied()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Alice Smith" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText,
                    "PART_InitialsText must still be present after theme cycle.");
                w.Close();
            });
        }
    }
}
