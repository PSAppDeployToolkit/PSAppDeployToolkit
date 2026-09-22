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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualGeometry;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GallerySettingsPage"/>.
    /// </summary>
    public sealed class GallerySettingsPageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        // This test needs the real shell NavigationView to track an externally applied theme
        // change, so it builds its own MainWindow rather than the page the class shares.
        [Fact]
        public Task GallerySettingsPage_AppThemeCombo_TracksExternalThemeChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    Controls.ComboBox appTheme = Assert.IsType<Controls.ComboBox>(DemoTestHost.FindByName<Controls.ComboBox>(nav.Content as DependencyObject, "AppThemeComboBox"), exactMatch: false);
                    Assert.Equal("Light", (appTheme.SelectedItem as ComboBoxItem)?.Content as string);

                    // The gallery page header's theme toggle (and any other external caller) flips
                    // the theme through ApplicationThemeManager.Apply directly, bypassing
                    // AppThemeComboBox_SelectionChanged entirely. The combo must still follow it.
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("Dark", (appTheme.SelectedItem as ComboBoxItem)?.Content as string);

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        // This test needs the real shell NavigationView pane state, so it builds its own
        // MainWindow rather than the page the class shares.
        [Fact]
        public Task GallerySettingsPage_NavigationStyleCombo_TracksExternalIsPaneOpenChangesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.ComboBox navigationStyle = Assert.IsType<Controls.ComboBox>(DemoTestHost.FindByName<Controls.ComboBox>(nav.Content as DependencyObject, "NavigationStyleComboBox"), exactMatch: false);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(2, navigationStyle.SelectedIndex);

                    nav.IsPaneOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        // This test needs the real shell NavigationView pane state, so it builds its own
        // MainWindow rather than the page the class shares.
        [Fact]
        public Task GallerySettingsPage_NavigationStyleCombo_SwitchesPaneModeAndKeepsContentLiveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    object settingsPage = nav.Content
                        ?? throw new InvalidOperationException("Settings navigation should create a live Settings page.");
                    Controls.ComboBox navigationStyle = Assert.IsType<Controls.ComboBox>(DemoTestHost.FindByName<Controls.ComboBox>(settingsPage as DependencyObject, "NavigationStyleComboBox"), exactMatch: false);

                    navigationStyle.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.True(nav.IsPaneOpen,
                        "Choosing Left in Settings should open the left pane instead of preserving a compact state.");
                    Assert.Same(settingsPage, nav.Content);

                    navigationStyle.SelectedIndex = 2;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Choosing Left compact in Settings should close the pane.");

                    navigationStyle.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(NavigationViewPaneDisplayMode.Top, nav.PaneDisplayMode);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_UsesFullWidthSettingsRowsForWindowControlsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GallerySettingsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Border appThemeCard = Assert.IsType<Border>(DemoTestHost.FindByName<Border>(page, "AppThemeSettingsCard"), exactMatch: false);
                    Border backdropCard = Assert.IsType<Border>(DemoTestHost.FindByName<Border>(page, "BackdropSettingsCard"), exactMatch: false);
                    Border colorsCard = Assert.IsType<Border>(DemoTestHost.FindByName<Border>(page, "ColorsSettingsCard"), exactMatch: false);
                    ComboBox backdrop = Assert.IsType<ComboBox>(DemoTestHost.FindByName<ComboBox>(page, "BackdropComboBox"), exactMatch: false);
                    UniformGrid accentRow = Assert.IsType<UniformGrid>(DemoTestHost.FindByName<UniformGrid>(page, "AccentSwatchRow"), exactMatch: false);
                    ComboBox minimize = Assert.IsType<ComboBox>(DemoTestHost.FindByName<ComboBox>(page, "MinimizeVisibilityCombo"), exactMatch: false);
                    ComboBox maximize = Assert.IsType<ComboBox>(DemoTestHost.FindByName<ComboBox>(page, "MaximizeVisibilityCombo"), exactMatch: false);
                    ComboBox close = Assert.IsType<ComboBox>(DemoTestHost.FindByName<ComboBox>(page, "CloseVisibilityCombo"), exactMatch: false);
                    FrameworkElement showIcon = Assert.IsType<FrameworkElement>(DemoTestHost.FindByName<FrameworkElement>(page, "ShowWindowIconToggle"), exactMatch: false);
                    FrameworkElement showTitle = Assert.IsType<FrameworkElement>(DemoTestHost.FindByName<FrameworkElement>(page, "ShowWindowTitleToggle"), exactMatch: false);

                    Assert.True(appThemeCard.ActualWidth > 700.0, "Settings cards should stretch across the content column.");
                    Assert.Equal(appThemeCard.ActualWidth, backdropCard.ActualWidth, 1.0);
                    Assert.Equal(backdropCard.ActualWidth, colorsCard.ActualWidth, 1.0);
                    Assert.Equal(7, accentRow.Children.Count);
                    Assert.Equal(GetVisualY((FrameworkElement)accentRow.Children[0], window), GetVisualY((FrameworkElement)accentRow.Children[6], window), 1.0);
                    Assert.True(GetVisualX(backdrop, window) > GetVisualX(appThemeCard, window) + 500.0,
                        "The Backdrop combo box should stay docked to the right side of its settings card.");
                    Assert.True(GetVisualY(maximize, window) > GetVisualY(minimize, window),
                        "Caption button customization should use separate settings rows.");
                    Assert.True(GetVisualY(close, window) > GetVisualY(maximize, window),
                        "Close button customization should appear below Maximize.");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test narrows the shared window's width to force the page's compact layout, so
        // it builds its own instance rather than mutating the one the class shares.
        [Fact]
        public Task GallerySettingsPage_CompactsControlsAtNarrowWidthsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GallerySettingsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    window.Width = 560;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ComboBox appTheme = Assert.IsType<ComboBox>(DemoTestHost.FindByName<ComboBox>(page, "AppThemeComboBox"), exactMatch: false);
                    ComboBox minimize = Assert.IsType<ComboBox>(DemoTestHost.FindByName<ComboBox>(page, "MinimizeVisibilityCombo"), exactMatch: false);
                    StackPanel accentPanel = Assert.IsType<StackPanel>(DemoTestHost.FindByName<StackPanel>(page, "AccentPickerPanel"), exactMatch: false);
                    UniformGrid accentRow = Assert.IsType<UniformGrid>(DemoTestHost.FindByName<UniformGrid>(page, "AccentSwatchRow"), exactMatch: false);
                    FrameworkElement systemAccent = Assert.IsType<FrameworkElement>(DemoTestHost.FindByName<FrameworkElement>(page, "SystemAccentButton"), exactMatch: false);
                    StackPanel repositoryActions = Assert.IsType<StackPanel>(DemoTestHost.FindByName<StackPanel>(page, "RepositoryActionsPanel"), exactMatch: false);
                    FrameworkElement copyRepository = Assert.IsType<FrameworkElement>(DemoTestHost.FindByName<FrameworkElement>(page, "CopyRepositoryButton"), exactMatch: false);

                    Assert.Equal(180.0, appTheme.Width, 0.001);
                    Assert.Equal(140.0, minimize.Width, 0.001);
                    Assert.Equal(Orientation.Vertical, accentPanel.Orientation);
                    Assert.Equal(4, accentRow.Columns);
                    Assert.Equal(2, accentRow.Rows);
                    Assert.Equal(new Thickness(0, 0, 0, 8), accentRow.Margin);
                    Assert.Equal(112.0, systemAccent.MinWidth, 0.001);
                    Assert.Equal(Orientation.Vertical, repositoryActions.Orientation);
                    Assert.Equal(new Thickness(0, 0, 0, 8), copyRepository.Margin);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GallerySettingsPage_RainbowAccentSwatches_PreserveLogoColorsAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GallerySettingsPage(), static window =>
            {
                UniformGrid accentRow = Assert.IsType<UniformGrid>(DemoTestHost.FindByName<UniformGrid>(window, "AccentSwatchRow"), exactMatch: false);

                string[] expected =
                [
                    "#E80000",
                    "#F58809",
                    "#F5E70C",
                    "#2BDE11",
                    "#09C4DE",
                    "#AA04DE",
                    "#FF00E8",
                ];

                Assert.Equal(expected.Length, accentRow.Children.Count);

                for (int i = 0; i < expected.Length; i++)
                {
                    FrameworkElement swatch = Assert.IsType<FrameworkElement>(accentRow.Children[i], exactMatch: false);
                    Assert.Equal(expected[i], swatch.Tag as string, StringComparer.Ordinal);

                    object converted = ColorConverter.ConvertFromString(expected[i]);
                    _ = Assert.IsType<Color>(converted, exactMatch: false);
                }
            });
        }

        // This test sets a swatch's Tag and raises its click event, so it builds its own
        // instance rather than mutating the one the class shares.
        [Fact]
        public Task GallerySettingsPage_InvalidAccentSwatchTag_DoesNotChangeAccentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GallerySettingsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    UniformGrid accentRow = Assert.IsType<UniformGrid>(DemoTestHost.FindByName<UniformGrid>(page, "AccentSwatchRow"), exactMatch: false);

                    Controls.Button swatch = Assert.IsType<Controls.Button>(accentRow.Children[0]);

                    Color originalAccent = Color.FromRgb(0x22, 0x44, 0x66);
                    ApplicationAccentColorManager.ApplyCustomAccent(originalAccent);

                    swatch.Tag = "#NotAColor";
                    swatch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, swatch));

                    Assert.Equal(originalAccent, ApplicationAccentColorManager.SystemAccentColor);
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                    DemoTestHost.CloseWindow(window);
                }
            });
        }
    }
}
