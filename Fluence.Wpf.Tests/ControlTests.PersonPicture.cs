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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Shapes;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Controls;
using Xunit;

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

        [Fact]
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
                Assert.NotNull(ellipse);
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.TextBlock? initialsText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText");
                Assert.NotNull(initialsText);

                Ellipse? imageEllipse = FindVisualChildByName<Ellipse>(pp, "PART_ImageEllipse");
                Assert.NotNull(imageEllipse);

                System.Windows.Controls.Grid? badgeGrid = FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid");
                Assert.NotNull(badgeGrid);

                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.TextBlock? initialsText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText");
                Assert.NotNull(initialsText);
                // Contact glyph U+E77B
                Assert.Equal("\uE77B", initialsText.Text, StringComparer.Ordinal);
                Assert.Contains("Segoe Fluent Icons", initialsText.FontFamily.Source, StringComparison.Ordinal);
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.TextBlock? initialsText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText");
                Assert.NotNull(initialsText);
                Assert.Equal("JD", initialsText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.TextBlock? initialsText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText");
                Assert.NotNull(initialsText);
                Assert.Equal("XY", initialsText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.TextBlock? initialsText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText");
                Assert.NotNull(initialsText);
                Assert.Equal("\uE716", initialsText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.Grid? badgeGrid = FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid");
                Assert.NotNull(badgeGrid);
                Assert.Equal(Visibility.Visible, badgeGrid.Visibility);

                System.Windows.Controls.TextBlock? badgeText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_BadgeText");
                Assert.NotNull(badgeText);
                Assert.Equal("3", badgeText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.Grid? badgeGrid = FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid");
                System.Windows.Controls.Border? badgeBackground = FindVisualChildByName<System.Windows.Controls.Border>(pp, "PART_BadgeBackground");
                System.Windows.Controls.TextBlock? badgeText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_BadgeText");
                Assert.NotNull(badgeGrid);
                Assert.NotNull(badgeBackground);
                Assert.NotNull(badgeText);
                Assert.Equal("99+", badgeText.Text, StringComparer.Ordinal);
                Assert.True(badgeGrid.ActualWidth >= badgeText.ActualWidth + 8.0,
                    "Numeric badges must use a pill surface wide enough to cover their rendered text.");
                Assert.True(badgeBackground.ActualWidth >= badgeGrid.ActualWidth,
                    "The badge background must cover the full badge layout width.");

                pp.BadgeNumber = 0;
                pp.BadgeGlyph = "\uE73E";
                DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();
                DrainDispatcher(w.Dispatcher);

                Assert.Equal("\uE73E", badgeText.Text, StringComparer.Ordinal);
                Assert.True(badgeGrid.ActualWidth >= badgeText.ActualWidth + 8.0,
                    "Glyph badges must keep enough background around the rendered glyph.");
                Assert.True(badgeBackground.ActualWidth >= badgeGrid.ActualWidth,
                    "The badge background must cover the full badge layout width.");
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.Grid? badgeGrid = FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid");
                Assert.NotNull(badgeGrid);
                Assert.Equal(Visibility.Collapsed, badgeGrid.Visibility);
                w.Close();
            });
        }

        [Fact]
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

                Assert.Equal(40.0, pp.Width);
                Assert.Equal(40.0, pp.Height);
                w.Close();
            });
        }

        [Fact]
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

                System.Windows.Controls.TextBlock? initialsText = FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText");
                Assert.NotNull(initialsText);
                w.Close();
            });
        }

        [Fact]
        public void PersonPicture_AutomationPeer_IsPersonPictureAutomationPeer()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                _ = Assert.IsAssignableFrom<PersonPictureAutomationPeer>(peer);
                w.Close();
            });
        }

        [Fact]
        public void PersonPicture_AutomationPeer_ControlTypeIsImage()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
                w.Close();
            });
        }

        [Fact]
        public void PersonPicture_AutomationPeer_GetName_ReturnsDisplayName()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal("Ada Lovelace", peer.GetName(), StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public void PersonPicture_AutomationPeer_GetName_FallsBackToInitials()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // No DisplayName, but Initials set explicitly.
                PersonPicture pp = new() { Initials = "AL" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal("AL", peer.GetName(), StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public void PersonPicture_AutomationPeer_ExplicitAutomationName_Wins()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                AutomationProperties.SetName(pp, "Profile picture for Ada");
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal("Profile picture for Ada", peer.GetName(), StringComparer.Ordinal);
                w.Close();
            });
        }
    }
}
