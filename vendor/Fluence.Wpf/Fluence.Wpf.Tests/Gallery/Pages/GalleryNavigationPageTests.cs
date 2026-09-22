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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryNavigationPage"/>.
    /// </summary>
    public sealed class GalleryNavigationPageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        // This test drives the page's compact NavigationView pane toggle, so it builds its own
        // instance rather than mutating the one the class shares.
        [Fact]
        public Task GalleryNavigationPage_CompactSamplePaneToggleOpensPaneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryNavigationPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(page, "CompactNavigationDemo"), exactMatch: false);
                    Assert.False(nav.IsPaneOpen, "Compact sample should start collapsed.");

                    Button paneToggle = Assert.IsType<Button>(nav.Template.FindName(Controls.NavigationView.PART_PaneToggleButton, nav));

                    Controls.Button? sampleToggle = DemoTestHost.FindByName<Controls.Button>(page, "CompactPaneToggleButton");
                    Assert.Null(sampleToggle);

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(nav.IsPaneOpen,
                        "Clicking the built-in compact pane toggle should open the sample pane.");

                    paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(nav.IsPaneOpen,
                        "Clicking the built-in compact pane toggle should close the sample pane.");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryNavigationPage_CompactSourceMatchesLiveInteractionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryNavigationPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    DemoSampleControl sample = Assert.IsType<DemoSampleControl>(DemoTestHost.FindVisualChildren<DemoSampleControl>(page)
                        .FirstOrDefault(static control => control.XamlSource.Contains("CompactNavigationView", StringComparison.Ordinal)), exactMatch: false);

                    Assert.Contains("IsBackEnabled=\"{Binding IsChecked, ElementName=BackEnabledToggle}\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("IsPaneToggleButtonVisible=\"True\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, sample.XamlSource.IndexOf("CompactPaneToggleButton", StringComparison.Ordinal));
                    Assert.Equal(-1, sample.CSharpSource.IndexOf("CompactPaneToggleButton_Click", StringComparison.Ordinal));
                    Assert.Contains("<fluence:NavigationViewItem", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Contains("Content=\"Settings\"", sample.XamlSource, StringComparison.Ordinal);
                    Assert.Equal(-1, sample.XamlSource.IndexOf("IsBackEnabled=\"False\"", StringComparison.Ordinal));
                    Assert.Equal(-1, sample.XamlSource.IndexOf("Footer content", StringComparison.Ordinal));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test drives the page's compact NavigationView pane toggle, so it builds its own
        // instance rather than mutating the one the class shares.
        [Fact]
        public Task GalleryNavigationPage_CompactSampleShowsBackAndPaneToggleButtonsAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryNavigationPage(), static window =>
            {
                Controls.NavigationView compact = Assert.IsType<Controls.NavigationView>(FindVisualChildByName<Controls.NavigationView>(window, "CompactNavigationDemo"), exactMatch: false);
                Controls.CheckBox backEnabled = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "BackEnabledToggle"), exactMatch: false);

                // The sample starts with back disabled so its own toggle begins in the state its
                // description describes, rather than already switched on.
                Assert.False(backEnabled.IsChecked.GetValueOrDefault(),
                    "Compact navigation sample should start with the back button disabled.");
                Assert.False(compact.IsBackEnabled,
                    "The sample's back state follows its Back enabled toggle.");
                Assert.True(compact.IsPaneToggleButtonVisible,
                    "Compact navigation sample should explicitly show the pane toggle button.");

                Button back = Assert.IsType<Button>(compact.Template.FindName(Controls.NavigationView.PART_BackButton, compact));
                Button paneToggle = Assert.IsType<Button>(compact.Template.FindName(Controls.NavigationView.PART_PaneToggleButton, compact));

                // The pane shows its back button only while it is both visible and enabled, so a
                // disabled back button is not drawn at all.
                Assert.Equal(Visibility.Collapsed, back.Visibility);
                Assert.Equal(Visibility.Visible, paneToggle.Visibility);

                // Ticking the toggle brings it back, which is the point of the sample.
                backEnabled.SetCurrentValue(ToggleButton.IsCheckedProperty, value: true);
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(compact.IsBackEnabled);
                Assert.Equal(Visibility.Visible, back.Visibility);
                backEnabled.SetCurrentValue(ToggleButton.IsCheckedProperty, value: false);
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Null(FindVisualChildByName<Controls.Button>(window, "CompactPaneToggleButton"));

                Assert.False(compact.IsPaneOpen,
                    "Compact navigation sample should start with the compact pane closed.");
                paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(compact.IsPaneOpen,
                    "The built-in pane toggle should open the compact pane.");
                paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.False(compact.IsPaneOpen,
                    "The built-in pane toggle should close the compact pane on subsequent clicks.");
            });
        }

        [Fact]
        public Task GalleryNavigationPage_IconsAreDefaultSizeAndInfoBadgePaneStartsExpandedAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryNavigationPage(), static window =>
            {
                Controls.NavigationView leftNavigation = Assert.IsType<Controls.NavigationView>(FindVisualChildByName<Controls.NavigationView>(window, "LeftNavigationDemo"), exactMatch: false);

                List<Controls.FontIcon> leftIcons = [.. FindVisualChildren<Controls.FontIcon>(leftNavigation)];
                Assert.True(leftIcons.Count >= 3, "Left navigation sample should expose item icons.");
                Assert.True(leftIcons.TrueForAll(static icon => Math.Abs(icon.IconFontSize - 16d) < 0.1),
                    "NavigationView item icons should align with the compact pane glyph size.");

                Controls.NavigationView badgeNavigation = Assert.IsType<Controls.NavigationView>(FindVisualChildren<Controls.NavigationView>(window).FirstOrDefault(static nav => string.Equals(nav.Header as string, "Inbox", StringComparison.Ordinal)), exactMatch: false);
                Assert.Equal(NavigationViewPaneDisplayMode.Left, badgeNavigation.PaneDisplayMode);
                Assert.True(badgeNavigation.IsPaneOpen,
                    "InfoBadge NavigationView sample should keep the pane open.");
            });
        }
    }
}
