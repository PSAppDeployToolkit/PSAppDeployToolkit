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
using System.Threading.Tasks;
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
        public Task PersonPicture_DefaultStyle_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Background ellipse must be in visual tree
                Ellipse ellipse = Assert.IsAssignableFrom<Ellipse>(FindVisualChild<Ellipse>(pp));
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_TemplateParts_PresentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock initialsText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText"));

                Ellipse imageEllipse = Assert.IsAssignableFrom<Ellipse>(FindVisualChildByName<Ellipse>(pp, "PART_ImageEllipse"));

                System.Windows.Controls.Grid badgeGrid = Assert.IsAssignableFrom<System.Windows.Controls.Grid>(FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid"));

                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_NoData_ShowsPlaceholderGlyphAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                // No DisplayName, no Initials, no ProfilePicture
                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock initialsText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText"));
                // Contact glyph U+E77B
                Assert.Equal("\uE77B", initialsText.Text, StringComparer.Ordinal);
                Assert.Contains("Segoe Fluent Icons", initialsText.FontFamily.Source, StringComparison.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_DisplayName_GeneratesInitialsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "John Doe" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock initialsText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText"));
                Assert.Equal("JD", initialsText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_ExplicitInitials_OverrideAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "John Doe", Initials = "XY" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock initialsText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText"));
                Assert.Equal("XY", initialsText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_IsGroup_ShowsPeopleGlyphAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { IsGroup = true };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock initialsText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText"));
                Assert.Equal("\uE716", initialsText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_BadgeNumber_MakesBadgeVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { BadgeNumber = 3 };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Grid badgeGrid = Assert.IsAssignableFrom<System.Windows.Controls.Grid>(FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid"));
                Assert.Equal(Visibility.Visible, badgeGrid.Visibility);

                System.Windows.Controls.TextBlock badgeText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_BadgeText"));
                Assert.Equal("3", badgeText.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_BadgeBackground_CoversNumberAndGlyphContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { Width = 48, Height = 48, BadgeNumber = 150 };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Grid badgeGrid = Assert.IsAssignableFrom<System.Windows.Controls.Grid>(FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid"));
                System.Windows.Controls.Border badgeBackground = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(pp, "PART_BadgeBackground"));
                System.Windows.Controls.TextBlock badgeText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_BadgeText"));
                Assert.Equal("99+", badgeText.Text, StringComparer.Ordinal);
                Assert.True(badgeGrid.ActualWidth >= badgeText.ActualWidth + 8.0,
                    "Numeric badges must use a pill surface wide enough to cover their rendered text.");
                Assert.True(badgeBackground.ActualWidth >= badgeGrid.ActualWidth,
                    "The badge background must cover the full badge layout width.");

                pp.BadgeNumber = 0;
                pp.BadgeGlyph = "\uE73E";
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal("\uE73E", badgeText.Text, StringComparer.Ordinal);
                Assert.True(badgeGrid.ActualWidth >= badgeText.ActualWidth + 8.0,
                    "Glyph badges must keep enough background around the rendered glyph.");
                Assert.True(badgeBackground.ActualWidth >= badgeGrid.ActualWidth,
                    "The badge background must cover the full badge layout width.");
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_NoBadge_BadgeCollapsedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { BadgeNumber = 0 };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Grid badgeGrid = Assert.IsAssignableFrom<System.Windows.Controls.Grid>(FindVisualChildByName<System.Windows.Controls.Grid>(pp, "PART_BadgeGrid"));
                Assert.Equal(Visibility.Collapsed, badgeGrid.Visibility);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_DefaultSize_Is40x40Async()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new();
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(40.0, pp.Width);
                Assert.Equal(40.0, pp.Height);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_ThemeCycle_StyleRemainsAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Alice Smith" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock initialsText = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(pp, "PART_InitialsText"));
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_AutomationPeer_IsPersonPictureAutomationPeerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                _ = Assert.IsAssignableFrom<PersonPictureAutomationPeer>(peer);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_AutomationPeer_ControlTypeIsImageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_AutomationPeer_GetName_ReturnsDisplayNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal("Ada Lovelace", peer.GetName(), StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_AutomationPeer_GetName_FallsBackToInitialsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                // No DisplayName, but Initials set explicitly.
                PersonPicture pp = new() { Initials = "AL" };
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal("AL", peer.GetName(), StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task PersonPicture_AutomationPeer_ExplicitAutomationName_WinsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                PersonPicture pp = new() { DisplayName = "Ada Lovelace" };
                AutomationProperties.SetName(pp, "Profile picture for Ada");
                Window w = new() { Content = pp, Width = 200, Height = 200 };
                w.Show();
                _ = pp.ApplyTemplate();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(pp);
                Assert.Equal("Profile picture for Ada", peer.GetName(), StringComparer.Ordinal);
                w.Close();
            });
        }
    }
}
