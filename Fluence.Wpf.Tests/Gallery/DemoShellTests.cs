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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherDelayWaits;
using static Fluence.Wpf.Tests.Infrastructure.FluentButtonQueries;
using static Fluence.Wpf.Tests.Infrastructure.VisualGeometry;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery
{
    /// <summary>
    /// The demo gallery shell (<see cref="MainWindow"/>): navigation between pages, the
    /// Settings page's theme, accent, and backdrop controls, and caption button defaults, plus
    /// every gallery page that has not been split into its own test class (Buttons, Inputs,
    /// Selection, Trees, Layout, Data, Data binding, Forms) and a handful of cross-page tests.
    /// </summary>
    public sealed class DemoShellTests : IAsyncLifetime
    {
        // The four TreeView samples the Trees page hosts, all deliberately borderless.
        private static readonly string[] BorderlessTreeSampleNames =
            ["HierarchyTreeView", "SelectionTreeView", "MultiSelectTreeView", "ExpansionTreeView"];

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        [Fact]
        public Task MainWindow_TitleBarSearchBox_CentresOnTheTitleBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow
                    {
                        Width = 1200,
                        Height = 800,
                    };
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.TitleBar titleBar = FindVisualChild<Controls.TitleBar>(window)
                        ?? throw new InvalidOperationException("The shell title bar is missing.");
                    FrameworkElement search = FindVisualChildByName<FrameworkElement>(window, "NavSearchBox")
                        ?? throw new InvalidOperationException("The title bar search box is missing.");

                    double searchCentre = search.TransformToAncestor(titleBar).Transform(new Point(0, 0)).X
                        + (search.ActualWidth / 2.0);

                    // The title bar spans the window, so its centre is the window's centre; the
                    // caption buttons overlay the right end rather than shortening it.
                    // The title bar spans the window, so its centre is the window's centre; the
                    // caption buttons overlay the right end rather than shortening it.
                    Assert.Equal(titleBar.ActualWidth / 2.0, searchCentre, 1.0);

                    // Vertically the box must both sit centred and measure what it draws: while the
                    // field reserved a helper row it never showed, its layout box ran 9 dip taller
                    // than its chrome and the visible field sat high in the bar.
                    Point searchTopLeft = search.TransformToAncestor(titleBar).Transform(new Point(0, 0));
                    Assert.Equal(titleBar.ActualHeight / 2.0, searchTopLeft.Y + (search.ActualHeight / 2.0), 0.5);
                    Assert.True(
                        search.ActualHeight < 40.0,
                        string.Format(CultureInfo.InvariantCulture, "The search box must not reserve an unused helper row; it measured {0}.", search.ActualHeight));
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_AccentColorButtons_UseButtonControlAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    List<Controls.Button> accentSwatchButtons = [.. FindVisualChildren<Controls.Button>(window).Where(static b => b.Tag is string hex && hex.Length > 0 && hex[0] == '#')];

                    List<string> expectedSwatches =
                    [
                        "#E80000",
                        "#F58809",
                        "#F5E70C",
                        "#2BDE11",
                        "#09C4DE",
                        "#AA04DE",
                        "#FF00E8",
                    ];

                    Assert.Equal(expectedSwatches, accentSwatchButtons.ConvertAll(static b => (string)b.Tag));
                    foreach (Controls.Button swatch in accentSwatchButtons)
                    {
                        _ = Assert.IsType<Controls.Button>(swatch, exactMatch: false);
                    }
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsSelectors_UseExpectedControlsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox"), exactMatch: false);
                    _ = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "NavigationStyleComboBox"), exactMatch: false);
                    _ = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "BackdropComboBox"), exactMatch: false);
                    _ = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle"), exactMatch: false);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_AppThemeComboBox_UpdatesStateLabelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, WindowBackdropType.Auto);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ComboBox themeComboBox = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox"), exactMatch: false);
                    TextBlock themeStateLabel = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "ThemeStateLabel"), exactMatch: false);

                    themeComboBox.SelectedIndex = 2;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Current: Dark", themeStateLabel.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_DemoButtons_RenderTheirIconsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button iconLeftButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);
                    Controls.Button iconRightButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"), exactMatch: false);

                    AssertButtonShowsGlyph(iconLeftButton, "\uE774");
                    AssertButtonShowsGlyph(iconRightButton, "\uE8D6");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_StandardDemoButtonIcons_UsePrimaryTextBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button iconLeftButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);
                    Controls.Button iconRightButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"), exactMatch: false);
                    SolidColorBrush expectedBrush = Assert.IsType<SolidColorBrush>(application.Resources["TextFillColorPrimaryBrush"]);

                    TextBlock iconLeftGlyph = Assert.IsType<TextBlock>(FindButtonIconTextBlock(iconLeftButton), exactMatch: false);
                    TextBlock iconRightGlyph = Assert.IsType<TextBlock>(FindButtonIconTextBlock(iconRightButton), exactMatch: false);

                    _ = Assert.IsType<SolidColorBrush>(iconLeftGlyph.Foreground, exactMatch: false);
                    _ = Assert.IsType<SolidColorBrush>(iconRightGlyph.Foreground, exactMatch: false);
                    Assert.Equal(expectedBrush.Color, ((SolidColorBrush)iconLeftGlyph.Foreground).Color);
                    Assert.Equal(expectedBrush.Color, ((SolidColorBrush)iconRightGlyph.Foreground).Color);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TabSelection_ActivatesExpectedContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);
                    Assert.NotNull(FindFluentButtonByContent(window, "Icon Left"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Inputs").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.TextBox>(window, "CharCountTextBox"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Status").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ProgressBar>(window, "StepProgressBar"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Data").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ListView>(window, "EmptyStateListView"));
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_NavigationView_UsesFlatGalleryTaxonomyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));
                    List<string> pages = [];
                    foreach (object? obj in nav.Items)
                    {
                        if (obj is not Controls.NavigationViewItem item || item.Content is not string content)
                        {
                            continue;
                        }

                        Assert.Null(item.InfoBadge);
                        pages.Add(content);
                    }

                    Assert.Contains("Home", pages, StringComparer.Ordinal);
                    Assert.Contains("Buttons", pages, StringComparer.Ordinal);
                    Assert.Contains("Selection", pages, StringComparer.Ordinal);
                    Assert.Contains("Inputs", pages, StringComparer.Ordinal);
                    Assert.Contains("Typography", pages, StringComparer.Ordinal);
                    Assert.Contains("Icons", pages, StringComparer.Ordinal);
                    Assert.False(pages.Contains("Windowing"), "Windowing controls should move to Settings rather than the main navigation list.");
                    Assert.False(pages.Contains("Button"), "Demo navigation should use grouped pages, not generated per-control pages.");
                    Assert.False(pages.Contains("Fundamentals"), "Demo navigation should not expose the old Fundamentals section.");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_CaptionButtons_DefaultOverridesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(window.IsMinimizable);
                    Assert.True(window.IsMaximizable);
                    Assert.True(window.IsClosable);

                    Button closeButton = Assert.IsType<Button>(window.Template.FindName("PART_CloseButton", window));
                    Assert.Equal(Visibility.Visible, closeButton.Visibility);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ThemeWatcherToggle_UpdatesLabelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ToggleSwitch toggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle"), exactMatch: false);
                    TextBlock label = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "SystemThemeLabel"), exactMatch: false);

                    Assert.True(toggle.IsChecked is true, "ThemeWatcherToggle should default to checked.");
                    Assert.Equal("Watching: Yes", label.Text, StringComparer.Ordinal);

                    toggle.IsChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Watching: No", label.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_IconLeftButton_IconIsVerticallyCenteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button button = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);

                    TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindButtonGlyphTextBlock(button, "\uE774"), exactMatch: false);
                    Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
                    Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
                    double buttonCenterY = buttonOrigin.Y + (button.ActualHeight / 2.0);
                    double glyphCenterY = glyphOrigin.Y + (glyphTextBlock.ActualHeight / 2.0);

                    Assert.Equal(buttonCenterY, glyphCenterY, 1.0);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_StandardButtonIcons_AreInsideButtonBoundsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button iconLeftButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);
                    Controls.Button iconRightButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"), exactMatch: false);

                    AssertGlyphWithinButtonBounds(window, iconLeftButton, "\uE774");
                    AssertGlyphWithinButtonBounds(window, iconRightButton, "\uE8D6");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SelectionDemoCombo_SelectionUpdatesIndexAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);

                    Controls.ComboBox combo = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo"), exactMatch: false);
                    Assert.Equal(3, combo.Items.Count);

                    combo.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, combo.SelectedIndex);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ComboBoxPage_InitialComboBoxesHaveNoSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

                    DependencyObject selectedContent = Assert.IsType<DependencyObject>(nav.Content, exactMatch: false);

                    List<Controls.ComboBox> comboBoxes = [.. FindVisualChildren<Controls.ComboBox>(selectedContent)];
                    Assert.True(comboBoxes.Count >= 2, "ComboBox page should display multiple ComboBox examples.");

                    foreach (Controls.ComboBox comboBox in comboBoxes)
                    {
                        Assert.Equal(-1, comboBox.SelectedIndex);
                    }
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task DemoMainWindow_SelectingNavPage_DoesNotThrowAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, WindowBackdropType.Auto);
                ApplicationAccentColorManager.ApplySystemAccent();

                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    window.UpdateLayout();

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);
                    Assert.NotNull(nav.SelectedItem);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task GalleryInputsPage_SliderSamplesIncludeHorizontalAndVerticalTicksAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryInputsPage(), static window =>
            {
                Controls.Slider horizontal = Assert.IsType<Controls.Slider>(FindVisualChildByName<Controls.Slider>(window, "HorizontalTickSlider"), exactMatch: false);
                Controls.Slider vertical = Assert.IsType<Controls.Slider>(FindVisualChildByName<Controls.Slider>(window, "VerticalTickSlider"), exactMatch: false);

                Assert.NotEqual(TickPlacement.None, horizontal.TickPlacement);
                Assert.NotEqual(TickPlacement.None, vertical.TickPlacement);
                Assert.True(horizontal.TickFrequency > 0);
                Assert.True(vertical.TickFrequency > 0);
            });
        }

        [Fact]
        public Task GalleryButtonsPage_LoadsWithoutDataBindingErrorsAsync()
        {
            // Regression: the flyout ShadowCaster in DropDownButton, SplitButton and ToggleSplitButton bound its
            // MinWidth by ElementName from inside the Popup, which logged "Cannot find source for binding with
            // reference 'ElementName=OuterBorder'" (Data Error 4) on every page load.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                using BindingErrorListener listener = new();
                PresentationTraceSources.Refresh();
                SourceLevels previousLevel = PresentationTraceSources.DataBindingSource.Switch.Level;
                _ = PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                Window window = new()
                {
                    Width = 900,
                    Height = 700,
                    Content = new GalleryButtonsPage(),
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Empty(listener.Messages);
                }
                finally
                {
                    PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
                    PresentationTraceSources.DataBindingSource.Switch.Level = previousLevel;
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task GalleryButtonsPage_GraphicalButtonClickReportsAutomationNameAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.Button button = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, "GraphicalButton"), exactMatch: false);
                TextBlock output = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "GraphicalButtonOutputText"), exactMatch: false);
                Image image = Assert.IsType<Image>(button.Content, exactMatch: false);

                Assert.NotNull(image.Source);
                Assert.Equal(50.0, button.Width, 0.1);
                Assert.Equal(50.0, button.Height, 0.1);
                Assert.True(string.IsNullOrWhiteSpace(output.Text), "The graphical sample output starts empty.");

                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Equal("You clicked: Pie", output.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryButtonsPage_RepeatButtonIncrementsNearbyCountTextAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.RepeatButton button = Assert.IsType<Controls.RepeatButton>(FindVisualChildByName<Controls.RepeatButton>(window, "RepeatCounterButton"), exactMatch: false);
                TextBlock count = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "RepeatButtonCountText"), exactMatch: false);
                Controls.RepeatButton? accentRepeat = FindRepeatButtonByContent(window, "Accent repeat");

                Assert.Null(accentRepeat);
                Assert.Equal("Clicks: 0", count.Text, StringComparer.Ordinal);

                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Equal("Clicks: 2", count.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryButtonsPage_ToggleButtonSampleUpdatesStateTextAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.ToggleButton wrapToggle = Assert.IsType<Controls.ToggleButton>(FindVisualChildByName<Controls.ToggleButton>(window, "WrapToggleButton"), exactMatch: false);
                Controls.ToggleButton threeStateToggle = Assert.IsType<Controls.ToggleButton>(FindVisualChildByName<Controls.ToggleButton>(window, "ThreeStateToggleButton"), exactMatch: false);
                TextBlock stateText = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "ToggleButtonStateText"), exactMatch: false);

                Assert.True(threeStateToggle.IsThreeState, "The three-state sample should opt into IsThreeState.");
                Assert.Equal("Wrap text: Off", stateText.Text, StringComparer.Ordinal);

                wrapToggle.IsChecked = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("Wrap text: On", stateText.Text, StringComparer.Ordinal);

                wrapToggle.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("Wrap text: Off", stateText.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryButtonsPage_ToggleSplitButtonSampleTogglesStateTextAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.ToggleSplitButton listToggle = Assert.IsType<Controls.ToggleSplitButton>(FindVisualChildByName<Controls.ToggleSplitButton>(window, "ListToggleSplitButton"), exactMatch: false);
                TextBlock stateText = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "ToggleSplitButtonStateText"), exactMatch: false);

                Assert.Equal("List formatting: Off", stateText.Text, StringComparer.Ordinal);

                Button primary = Assert.IsType<Button>(listToggle.Template?.FindName("PART_PrimaryButton", listToggle));

                primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.True(listToggle.IsChecked, "Clicking the primary half should check the sample.");
                Assert.Equal("List formatting: Bulleted list", stateText.Text, StringComparer.Ordinal);

                primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.False(listToggle.IsChecked, "A second primary click should uncheck the sample.");
                Assert.Equal("List formatting: Off", stateText.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GallerySelectionPage_CheckBoxSamplesMatchWinUIGalleryStatesAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.CheckBox twoState = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "TwoStateCheckBox"), exactMatch: false);
                Controls.CheckBox threeState = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "ThreeStateCheckBox"), exactMatch: false);
                Controls.CheckBox selectAll = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "SelectAllCheckBox"), exactMatch: false);
                Controls.CheckBox optionOne = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionOneCheckBox"), exactMatch: false);
                Controls.CheckBox optionTwo = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionTwoCheckBox"), exactMatch: false);
                Controls.CheckBox optionThree = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionThreeCheckBox"), exactMatch: false);

                Assert.False(twoState.IsThreeState);
                Assert.True(threeState.IsThreeState);

                selectAll.IsChecked = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.True(optionOne.IsChecked.GetValueOrDefault());
                Assert.True(optionTwo.IsChecked.GetValueOrDefault());
                Assert.True(optionThree.IsChecked.GetValueOrDefault());

                optionTwo.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Null(selectAll.IsChecked);
            });
        }

        [Fact]
        public Task GallerySelectionPage_RatingAndRequestedToggleSamplesArePresentAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.RatingControl rating = Assert.IsType<Controls.RatingControl>(FindVisualChildByName<Controls.RatingControl>(window, "RatingSample"), exactMatch: false);
                Controls.RatingControl readOnlyRating = Assert.IsType<Controls.RatingControl>(FindVisualChildByName<Controls.RatingControl>(window, "ReadOnlyRatingSample"), exactMatch: false);
                TextBlock workHeader = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "WorkToggleHeaderText"), exactMatch: false);
                Controls.ToggleSwitch workToggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch"), exactMatch: false);
                TextBlock workLabel = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "WorkToggleStateText"), exactMatch: false);
                Controls.ProgressRing ring = Assert.IsType<Controls.ProgressRing>(FindVisualChildByName<Controls.ProgressRing>(window, "WorkToggleProgressRing"), exactMatch: false);

                Assert.Equal(1, CountVisualChildren<Controls.ToggleSwitch>(window));
                Assert.Null(FindVisualChildByName<Controls.ToggleSwitch>(window, "SimpleToggleSwitch"));
                Assert.Null(FindVisualChildByName<TextBlock>(window, "SimpleToggleStateText"));
                Assert.Equal("Toggle work", workHeader.Text, StringComparer.Ordinal);
                Assert.True(workToggle.IsChecked.GetValueOrDefault());
                Assert.Equal("On", workLabel.Text, StringComparer.Ordinal);
                Assert.True(ring.IsIndeterminate);
                Assert.Equal(new Thickness(24, 0, 0, 0), ring.Margin);

                workToggle.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.False(ring.IsActive);

                workToggle.IsChecked = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(ring.IsActive);
            });
        }

        [Fact]
        public Task GalleryTreesPage_IncludesMultipleSelectionTreeViewAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
            {
                Controls.TreeView treeView = Assert.IsType<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView"), exactMatch: false);

                Assert.Equal(TreeViewSelectionMode.Multiple, treeView.SelectionMode);
            });
        }

        [Fact]
        public Task GalleryLayoutPage_ExpanderStartsCollapsedAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryLayoutPage(), static window =>
            {
                Controls.Expander expander = Assert.IsType<Controls.Expander>(FindVisualChildByName<Controls.Expander>(window, "AdvancedOptionsExpander"), exactMatch: false);

                Assert.False(expander.IsExpanded, "Layout page Expander sample should be collapsed by default.");
            });
        }

        [Fact]
        public Task GalleryDataPage_ListBoxSamplesExposeSelectionModesAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryDataPage(), static window =>
            {
                Controls.ListBox singleSelect = Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox"), exactMatch: false);
                Controls.ListBox multiSelect = Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox"), exactMatch: false);

                Assert.Equal(SelectionMode.Single, singleSelect.SelectionMode);
                Assert.Equal(SelectionMode.Extended, multiSelect.SelectionMode);
                Assert.True(singleSelect.Items.Count > 0, "Single-selection ListBox sample should contain items.");
                Assert.True(multiSelect.SelectedItems.Count >= 2,
                    "Multi-selection ListBox sample should start with multiple items selected.");
            });
        }

        [Fact]
        public async Task GalleryDataAndTreeSamplesExposeThemedBordersAsync()
        {
            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryDataPage(), static window =>
            {
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "SimpleListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "RichListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox"), exactMatch: false));
            }).ConfigureAwait(true);

            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
            {
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "BoundListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "SelectionModeListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "DataTemplateListView"), exactMatch: false));
            }).ConfigureAwait(true);

            // The tree samples carry no card of their own: the WinUI 3 Gallery's TreeView page
            // shows the control directly on the sample surface, and the library default is a
            // transparent, borderless TreeView. Collection samples on the Data pages above keep
            // their themed border, which is what the Gallery's own list samples show.
            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
            {
                foreach (string name in BorderlessTreeSampleNames)
                {
                    Controls.TreeView tree = Assert.IsType<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, name), exactMatch: false);
                    Assert.Equal(new Thickness(0), tree.BorderThickness);
                }
            }).ConfigureAwait(true);
        }

        [Fact]
        public Task GalleryButtonsPage_EnableCheckBoxControlsOnlyTheStandardButtonAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.CheckBox enable = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "ButtonEnableCheckBox"), exactMatch: false);
                Controls.Button standard = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Standard XAML button"), exactMatch: false);
                Controls.Button accent = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Accent style button"), exactMatch: false);
                Controls.Button subtle = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Subtle style button"), exactMatch: false);
                Controls.Button? disabled = FindFluentButtonByContent(window, "Disabled");

                Assert.Null(disabled);

                Assert.True(standard.IsEnabled);
                Assert.True(accent.IsEnabled);
                Assert.True(subtle.IsEnabled);

                enable.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.False(standard.IsEnabled, "Enable toggle should disable the Standard button.");
                Assert.True(accent.IsEnabled, "The Accent button lives in its own sample and must not follow the Standard sample's toggle.");
                Assert.True(subtle.IsEnabled, "The Subtle button lives in its own sample and must not follow the Standard sample's toggle.");
            });
        }

        [Fact]
        public Task GalleryButtonsPage_SubtleButtonsUseWinUiTransparentRestBorderAndToggleButtonSampleIsRemovedAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.Button subtle = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Subtle style button"), exactMatch: false);
                Controls.Button refresh = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Refresh"), exactMatch: false);

                AssertBrushIsTransparent(subtle.BorderBrush);
                AssertBrushIsTransparent(refresh.BorderBrush);
                Assert.Null(FindToggleButtonByContent(window, "Bold"));
                Assert.Null(FindToggleButtonByContent(window, "Pinned"));
            });
        }

        [Fact]
        public Task GalleryButtonsPage_DemoContentPresenterCentersButtonGroupsAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                List<DemoSampleControl> samples = [.. FindVisualChildren<DemoSampleControl>(window)];
                Assert.True(samples.Count > 0, "Buttons page should render DemoSampleControl samples.");

                foreach (DemoSampleControl sample in samples)
                {
                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(sample.FindName("DemoContentPresenter"));
                    Assert.Equal(VerticalAlignment.Center, presenter.VerticalAlignment);
                    Assert.Equal(HorizontalAlignment.Stretch, presenter.HorizontalAlignment);
                }
            });
        }

        [Fact]
        public Task GallerySelectionPage_BasicRadioGroupStartsAtGroupLeftEdgeAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.RadioButton optionA = Assert.IsType<Controls.RadioButton>(FindRadioButtonByContent(window, "Option A"), exactMatch: false);
                Controls.RadioButton optionB = Assert.IsType<Controls.RadioButton>(FindRadioButtonByContent(window, "Option B"), exactMatch: false);
                Controls.RadioButton optionC = Assert.IsType<Controls.RadioButton>(FindRadioButtonByContent(window, "Option C"), exactMatch: false);

                Assert.Equal(0.0, optionA.Margin.Left);
                Assert.Equal(16.0, optionA.Margin.Right);
                Assert.Equal(0.0, optionB.Margin.Left);
                Assert.Equal(0.0, optionC.Margin.Left);
            });
        }

        [Fact]
        public Task GalleryDataBindingPage_AddItemRailIsWiderAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
            {
                Controls.TextBox newItemBox = Assert.IsType<Controls.TextBox>(FindVisualChildByName<Controls.TextBox>(window, "NewItemBox"), exactMatch: false);
                StackPanel rightRailStack = Assert.IsType<StackPanel>(newItemBox.Parent);

                Assert.Equal(320.0, rightRailStack.MinWidth, 0.1);
                Assert.Equal(320.0, newItemBox.Width, 0.1);
            });
        }

        [Fact]
        public Task GalleryFormsPage_ActionsAlignAndOutputHasStableSpaceAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryFormsPage(), static window =>
            {
                Controls.Button signIn = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, "SignInButton"), exactMatch: false);
                StackPanel? checkoutButtons = FindVisualChildByName<StackPanel>(window, "CheckoutButtonsPanel");
                Controls.Button placeOrder = Assert.IsType<Controls.Button>(checkoutButtons?.Children.OfType<Controls.Button>().FirstOrDefault(), exactMatch: false);
                List<Border> outputRegions = [.. FindVisualChildren<DemoSampleControl>(window)
                    .Select(static sample => sample.FindName("OutputRegion") as Border)
                    .Where(static border => border is not null)
                    .Cast<Border>()];

                Assert.Equal(0.0, signIn.Margin.Left);
                Assert.Equal(0.0, placeOrder.Margin.Left);
                Assert.True(outputRegions.Count > 0, "Forms page should expose output regions.");
                Assert.True(outputRegions.TrueForAll(static region => region.MinWidth >= 220.0),
                    "Output regions should reserve enough room for status text.");
            });
        }

        [Fact]
        public async Task GalleryPages_RemoveRequestedOutputRegionsAsync()
        {
            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryInputsPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "CharCountLabel"))).ConfigureAwait(true);

            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "ItemCountLabel"))).ConfigureAwait(true);

            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "TreeSelectionLabel"))).ConfigureAwait(true);

            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryNavigationPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "CompactNavigationOutputText"))).ConfigureAwait(true);
        }

        [Fact]
        public Task GalleryFormsPage_CheckoutFieldsUseStableNamesAndAlignOptionalInputAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryFormsPage(), static window =>
            {
                Grid checkoutGrid = Assert.IsType<Grid>(FindVisualChildByName<Grid>(window, "CheckoutFieldsGrid"), exactMatch: false);
                Controls.NumberBox quantity = Assert.IsType<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "QuantityNumberBox"), exactMatch: false);
                Controls.TextBox optional = Assert.IsType<Controls.TextBox>(FindVisualChildByName<Controls.TextBox>(window, "OptionalTextBox"), exactMatch: false);
                Controls.CheckBox gift = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "GiftCheckBox"), exactMatch: false);
                StackPanel actions = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "CheckoutButtonsPanel"), exactMatch: false);

                Assert.Equal(3, checkoutGrid.ColumnDefinitions.Count);
                Assert.Equal(0, Grid.GetColumn(quantity));
                Assert.Equal(2, Grid.GetColumn(optional));
                Assert.Equal(VerticalAlignment.Bottom, optional.VerticalAlignment);
            });
        }

        [Fact]
        public Task GalleryDataPage_ListBackgroundsAndPersonPicturesUseExpectedAssetsAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryDataPage(), static window =>
            {
                Border simpleBackground = Assert.IsType<Border>(FindVisualChildByName<Border>(window, "SimpleListViewBackground"), exactMatch: false);
                Border richBackground = Assert.IsType<Border>(FindVisualChildByName<Border>(window, "RichListViewBackground"), exactMatch: false);
                StackPanel emptyStateActions = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "EmptyStateActionsPanel"), exactMatch: false);

                Assert.Equal(HorizontalAlignment.Center, emptyStateActions.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, emptyStateActions.VerticalAlignment);
                Assert.True(emptyStateActions.Children.OfType<Controls.Button>().All(static button => button.MinWidth >= 140.0),
                    "EmptyContent action buttons should be wider than the default compact command width.");

                List<Controls.PersonPicture> personPictures = [.. FindVisualChildren<Controls.PersonPicture>(window)];
                WrapPanel personPicturePanel = Assert.IsType<WrapPanel>(personPictures.FirstOrDefault()?.Parent);
                Assert.Equal(5, personPictures.Count);
                Assert.Equal(5, personPictures.Count(static picture => picture.ProfilePicture is not null));
                Assert.Equal(HorizontalAlignment.Center, personPicturePanel.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, personPicturePanel.VerticalAlignment);
                Assert.True(personPictures.Exists(static picture => picture.ProfilePicture?.ToString(CultureInfo.InvariantCulture).IndexOf("PersonPictureMadisonButler.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should include the Madison Butler portrait asset.");
                Assert.False(personPictures.Exists(static picture => picture.ProfilePicture?.ToString(CultureInfo.InvariantCulture).IndexOf("PersonPictureOscarWard.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should remove the extra Oscar Ward portrait.");
                Assert.False(personPictures.Exists(static picture => !string.IsNullOrWhiteSpace(picture.Initials)),
                    "PersonPicture sample should remove the initials fallback entry.");
                Assert.False(personPictures.Exists(static picture => picture.IsGroup),
                    "PersonPicture sample should remove the invalid group glyph entry.");
            });
        }

        [Fact]
        public async Task GalleryPages_RightRailControlsUseRequestedAlignmentAsync()
        {
            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
            {
                StackPanel selectionRail = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "SelectionModeRail"), exactMatch: false);
                Assert.Equal(VerticalAlignment.Center, selectionRail.VerticalAlignment);
            }).ConfigureAwait(true);

            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
            {
                StackPanel treeExpansionActions = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "TreeExpansionActionsPanel"), exactMatch: false);
                Assert.Equal(HorizontalAlignment.Center, treeExpansionActions.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, treeExpansionActions.VerticalAlignment);
                Assert.True(treeExpansionActions.Children.OfType<Controls.Button>().All(static button => button.MinWidth >= 140.0),
                    "Tree expansion buttons should be wider than the default compact command width.");
            }).ConfigureAwait(true);

            await DemoTestHost.RunDemoPageTestAsync(static () => new GalleryAccessibilityPage(), static window =>
            {
                string[] buttonNames =
                [
                    "AutomationNewDocumentButton",
                    "AutomationOpenFileButton",
                    "AutomationSaveButton",
                    "AutomationDeleteButton",
                    "AutomationShareButton",
                ];

                foreach (string buttonName in buttonNames)
                {
                    Controls.Button button = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, buttonName), exactMatch: false);
                    Assert.Equal(36.0, button.Width, 0.1);
                    Assert.Equal(36.0, button.Height, 0.1);
                    Assert.Equal(36.0, button.MinWidth, 0.1);
                    Assert.Equal(0.0, button.Padding.Left, 0.1);
                }
            }).ConfigureAwait(true);
        }

        [Fact]
        public Task GalleryLayoutPage_SeparatesStructuralPrimitiveSamplesAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryLayoutPage(), static window =>
            {
                List<string> descriptions = [.. FindVisualChildren<DemoSampleControl>(window).Select(static sample => sample.SampleDescription)];

                Assert.True(descriptions.Exists(static description => description.Contains("Separator", StringComparison.OrdinalIgnoreCase)),
                    "Layout page should have a dedicated Separator DemoSampleControl.");
                Assert.True(descriptions.Exists(static description => description.Contains("DockPanel", StringComparison.OrdinalIgnoreCase)),
                    "Layout page should have a dedicated DockPanel DemoSampleControl.");
                Assert.True(descriptions.Exists(static description => description.Contains("Expander", StringComparison.OrdinalIgnoreCase)),
                    "Layout page should have a dedicated Expander DemoSampleControl.");

                Controls.Expander dockPanelExpander = Assert.IsType<Controls.Expander>(FindVisualChildByName<Controls.Expander>(window, "DockPanelOptionsExpander"), exactMatch: false);
                _ = Assert.IsType<DockPanel>(dockPanelExpander.Header, exactMatch: false);
                _ = Assert.IsType<DockPanel>(dockPanelExpander.Content, exactMatch: false);
            });
        }

        [Fact]
        public Task MainWindow_DirectNavigation_LoadsConcretePagesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        window.NavigateTo(expectation.Tag);
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();
                        WpfTestSta.DrainDispatcher(window.Dispatcher);

                        object content = Assert.IsType<object>(GetSelectedPageContent(window), exactMatch: false);
                        Assert.Equal(expectation.PageType, content.GetType());
                        Assert.NotEqual("GalleryControlPage", content.GetType().Name, StringComparer.Ordinal);
                        Assert.NotEqual("GalleryCategoryPage", content.GetType().Name, StringComparer.Ordinal);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_InitialSelection_LoadsHomePageContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    object content = Assert.IsType<object>(GetSelectedPageContent(window), exactMatch: false);
                    Assert.Equal(typeof(GalleryHomePage), content.GetType());

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    Frame frame = Assert.IsType<Frame>(nav.Content, exactMatch: false);
                    Assert.Same(content, frame.Content);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public async Task Library_EmbedsXamlBrandIcons_AndDemosSetBrandApplicationIconAsync()
        {
            // The Fluence brand icon ships as resolution-independent vector DrawingImages in
            // Fluence.Wpf\Themes\Icons\FluenceIcons.xaml (merged into Generic.xaml), replacing the
            // multi-resolution assets\Fluence.ico that previously dominated the library binary.
            // FluenceWindow rasterizes the brand vector for its default Window.Icon, so neither demo
            // sets Icon= in XAML (both inherit the embedded default at runtime). The demo executables
            // do set ApplicationIcon to the brand .ico so the .exe file icon in Explorer is the brand mark.
            string libraryProject = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Fluence.Wpf.csproj").ConfigureAwait(true);
            Assert.False(libraryProject.Contains("Fluence.ico", StringComparison.Ordinal),
                "The library should no longer embed assets\\Fluence.ico now that the brand icon is a XAML vector.");
            Assert.Contains("<PackageIcon>Fluence_Icon_Light_128.png</PackageIcon>", libraryProject, StringComparison.Ordinal);

            // The three brand DrawingImages live in a dedicated icon dictionary that is merged into
            // Generic.xaml so the keys resolve from application resources.
            Assert.True(File.Exists(DemoTestHost.GetRepositoryFilePath("Fluence.Wpf", "Themes", "Icons", "FluenceIcons.xaml")),
                "The brand icon dictionary should exist at Fluence.Wpf\\Themes\\Icons\\FluenceIcons.xaml.");
            string iconDictionary = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Themes", "Icons", "FluenceIcons.xaml").ConfigureAwait(true);
            Assert.Contains("FluenceIconBrandDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("FluenceIconLightDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("FluenceIconDarkDrawingImage", iconDictionary, StringComparison.Ordinal);
            Assert.Contains("Themes/Icons/FluenceIcons.xaml", await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Themes", "Generic.xaml").ConfigureAwait(true), StringComparison.Ordinal);

            // Both demo executables set their ApplicationIcon to the Fluence brand .ico so the .exe
            // shows the brand mark in Explorer and on a pre-launch taskbar pin.
            string galleryProject = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "Fluence.Wpf.Demo.csproj").ConfigureAwait(true);
            Assert.Contains("<ApplicationIcon>", galleryProject, StringComparison.Ordinal);
            Assert.Contains("Fluence_Icon_Light.ico", galleryProject, StringComparison.Ordinal);
            string mvvmProject = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo.Mvvm", "Fluence.Wpf.Demo.Mvvm.csproj").ConfigureAwait(true);
            Assert.Contains("<ApplicationIcon>", mvvmProject, StringComparison.Ordinal);
            Assert.Contains("Fluence_Icon_Light.ico", mvvmProject, StringComparison.Ordinal);

            Assert.False((await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "MainWindow.xaml").ConfigureAwait(true)).Contains("Icon=\"", StringComparison.Ordinal),
                "The gallery demo window should inherit the embedded FluenceWindow icon, not set Icon= itself.");
            Assert.False((await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo.Mvvm", "MainWindow.xaml").ConfigureAwait(true)).Contains("Icon=\"", StringComparison.Ordinal),
                "The MVVM demo window should inherit the embedded FluenceWindow icon, not set Icon= itself.");

            // The retired .ico is gone from the tree.
            Assert.False(File.Exists(DemoTestHost.GetRepositoryFilePath("assets", "Fluence.ico")),
                "assets\\Fluence.ico should be deleted once the XAML vector icons replace it.");
        }

        [Fact]
        public Task MainWindow_Search_NavigatesToGroupedConcretePageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);

                    search.Text = "progress ring";
                    search.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Enter)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    object content = GetSelectedPageContent(window);
                    Assert.Equal(typeof(GalleryStatusPage), content.GetType());
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_BackRequested_WalksVisitedPagesInOrderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);

                    window.NavigateTo("buttons");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.NavigateTo("trees");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.NavigateTo("status");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(typeof(GalleryStatusPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryTreesPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryButtonsPage), GetSelectedPageContent(window).GetType());

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryHomePage), GetSelectedPageContent(window).GetType());

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    Assert.False(nav.IsBackEnabled,
                        "Back should become disabled when the demo history is empty.");

                    InvokeTitleBarBack(shellTitleBar);
                    Assert.Equal(typeof(GalleryHomePage), GetSelectedPageContent(window).GetType());
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MainWindow_NavigationCatalog_RemovesWindowingPage()
        {
            List<DemoNavigationItem> items = [.. DemoNavigationCatalog.Items];
            Assert.True(items.Count >= 1, "Navigation catalog should contain at least one entry.");
            Assert.Equal("Accessibility", items[^1].Title, StringComparer.Ordinal);
            Assert.False(items.Exists(static item => string.Equals(item.Title, "Windowing", StringComparison.Ordinal)),
                "Windowing should not remain as a regular NavigationView item.");
            Assert.False(items.Exists(static item => string.Equals(item.Route, "window", StringComparison.Ordinal)),
                "The old Windowing route should be removed from the regular navigation catalog.");
        }

        [Fact]
        public Task GalleryPages_UseSharedWinUiGalleryPageLayoutAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                Style scrollStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("GalleryPageScrollViewerStyle"));
                Style fluentScrollStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("ScrollViewerStyle"));
                Style contentStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("GalleryPageContentStackStyle"));
                Style contentGridStyle = Assert.IsType<Style>(Application.Current?.TryFindResource("GalleryPageContentGridStyle"));
                Assert.Same(fluentScrollStyle, scrollStyle.BasedOn);

                FrameworkElement[] pages =
                [
                    new GalleryHomePage(),
                    new GalleryIconsPage(),
                    new GalleryTypographyPage(),
                    new GalleryAccessibilityPage(),
                    new GalleryButtonsPage(),
                    new GallerySelectionPage(),
                    new GalleryInputsPage(),
                    new GalleryFormsPage(),
                    new GalleryDataPage(),
                    new GalleryDataBindingPage(),
                    new GalleryTreesPage(),
                    new GalleryMenusPage(),
                    new GalleryNavigationPage(),
                    new GalleryTabsPage(),
                    new GalleryLayoutPage(),
                    new GalleryStatusPage(),
                    new GallerySettingsPage(),
                    new GalleryColorsPage(),
                ];

                foreach (FrameworkElement page in pages)
                {
                    Window window = DemoTestHost.CreateHostWindow(page);
                    try
                    {
                        // The WinUI Gallery's own pages are Pages, not UserControls, and these
                        // mirror them.
                        _ = Assert.IsType<Page>(page, exactMatch: false);

                        // Home mirrors the WinUI Gallery home page, which has no page title
                        // header; every other gallery page carries exactly one.
                        if (page is not GalleryHomePage)
                        {
                            GalleryPageHeader header = Assert.Single(DemoTestHost.FindVisualChildren<GalleryPageHeader>(page));
                            Assert.False(string.IsNullOrWhiteSpace(header.Title),
                                page.GetType().Name + " should expose exactly one GalleryPageHeader with a non-empty Title.");
                        }

                        if (page is GalleryIconsPage)
                        {
                            Grid pageRoot = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "PageRoot"), exactMatch: false);
                            Assert.Null(pageRoot.Background);

                            Grid pageContent = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "PageContent"), exactMatch: false);
                            Assert.Same(contentGridStyle, pageContent.Style);
                            Assert.Equal(new Thickness(45, 24, 44, 0), pageContent.Margin);
                            Assert.True(double.IsPositiveInfinity(pageContent.MaxWidth),
                                "Icons should stretch instead of keeping the old max content width.");
                            Assert.Equal(HorizontalAlignment.Stretch, pageContent.HorizontalAlignment);
                            continue;
                        }

                        if (page is GalleryColorsPage)
                        {
                            // Colors follows the Gallery's own Color page: a Grid whose header,
                            // intro, snippet and SelectorBar rows are locked, with only the last
                            // star row scrolling.
                            Grid colorsRoot = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "PageRoot"), exactMatch: false);
                            Assert.Null(colorsRoot.Background);

                            Grid colorsContent = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "PageContent"), exactMatch: false);
                            Assert.Same(contentGridStyle, colorsContent.Style);
                            Assert.Equal(new Thickness(45, 24, 44, 0), colorsContent.Margin);
                            Assert.Equal(5, colorsContent.RowDefinitions.Count);
                            Assert.Equal(GridLength.Auto, colorsContent.RowDefinitions[0].Height);
                            Assert.Equal(new GridLength(1, GridUnitType.Star), colorsContent.RowDefinitions[4].Height);

                            Controls.SlideNavigationPresenter sectionPresenter = Assert.IsType<Controls.SlideNavigationPresenter>(DemoTestHost.FindByName<Controls.SlideNavigationPresenter>(page, "ColorSectionPresenter"), exactMatch: false);
                            Assert.Equal(4, Grid.GetRow(sectionPresenter));

                            // The scroll host lives inside the presenter, one per section, so the
                            // scrollbar belongs to the section and travels with it.
                            Controls.SmoothScrollViewer sectionScroll = Assert.IsType<Controls.SmoothScrollViewer>(sectionPresenter.Content, exactMatch: false);
                            Assert.Same(scrollStyle, sectionScroll.Style);
                            Assert.True(sectionScroll.Focusable, "The section scroll host takes focus so it answers Home, End and the page keys.");

                            // Only the section content scrolls: the locked rows sit outside it.
                            Assert.Empty(DemoTestHost.FindVisualChildren<GalleryPageHeader>(sectionScroll));
                            Assert.Empty(DemoTestHost.FindVisualChildren<Controls.SelectorBar>(sectionScroll));
                            continue;
                        }

                        if (page is GalleryHomePage)
                        {
                            // Home has no page header to lock, so it keeps the plain scroll host
                            // over a stack panel that carries the page content margin. That margin
                            // sits on the scrolling content here, so it keeps the trailing bottom
                            // inset the other pages move onto DemoPageScrollContentMargin.
                            Controls.SmoothScrollViewer homeScroll = Assert.IsType<Controls.SmoothScrollViewer>(DemoTestHost.FindVisualChildren<Controls.SmoothScrollViewer>(page).FirstOrDefault(), exactMatch: false);
                            Assert.Same(scrollStyle, homeScroll.Style);

                            StackPanel homeContent = Assert.IsType<StackPanel>(homeScroll.Content);
                            Assert.Same(contentStyle, homeContent.Style);
                            Assert.Equal(new Thickness(45, 24, 44, 48), homeContent.Margin);
                            Assert.Equal(HorizontalAlignment.Stretch, homeContent.HorizontalAlignment);
                            continue;
                        }

                        // Every other page follows the WinUI Gallery shape: a content Grid whose
                        // header (and description, where there is one) rows are locked, with the
                        // scroll host in the trailing star row.
                        Grid pageContentGrid = Assert.IsType<Grid>(
                            DemoTestHost.FindVisualChildren<Grid>(page).FirstOrDefault(grid => ReferenceEquals(grid.Style, contentGridStyle)),
                            exactMatch: false);
                        Assert.Equal(new Thickness(45, 24, 44, 0), pageContentGrid.Margin);
                        Assert.True(double.IsPositiveInfinity(pageContentGrid.MaxWidth),
                            page.GetType().Name + " should stretch instead of keeping the old max content width.");
                        Assert.Equal(HorizontalAlignment.Stretch, pageContentGrid.HorizontalAlignment);
                        Assert.Equal(
                            new GridLength(1, GridUnitType.Star),
                            pageContentGrid.RowDefinitions[^1].Height);

                        Controls.SmoothScrollViewer scrollViewer = Assert.IsType<Controls.SmoothScrollViewer>(DemoTestHost.FindVisualChildren<Controls.SmoothScrollViewer>(page).FirstOrDefault(), exactMatch: false);
                        Assert.Same(scrollStyle, scrollViewer.Style);
                        Assert.Equal(pageContentGrid.RowDefinitions.Count - 1, Grid.GetRow(scrollViewer));

                        // The locking invariant: the page header never scrolls with the samples.
                        Assert.Empty(DemoTestHost.FindVisualChildren<GalleryPageHeader>(scrollViewer));

                        // WinUI Gallery geometry: the Gallery puts its page ScrollViewer outside
                        // the page content margin, so the rail rides the window frame. The scroll
                        // host gives the whole right page margin back to reach it and the content
                        // pays the same amount again, so the cards still end where the title does.
                        // The Colors page does the same, one scroll host per section.
                        Assert.Equal(new Thickness(0, 0, -44, 0), scrollViewer.Margin);

                        StackPanel content = Assert.IsType<StackPanel>(scrollViewer.Content);
                        Assert.Equal(new Thickness(0, 0, 44, 48), content.Margin);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            });
        }

        [Fact]
        public Task MainWindow_TitleBarSearch_StaysVisibleWhenContentExtendsIntoTitleBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, search.Visibility);

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Visible, search.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TitleBarSearch_IsNotClippedAsync()
        {
            // Reported from the shell: the search box was cut off along its bottom edge. A fixed
            // Height under what the box measures arranges its chrome short, and WPF clips at the
            // layout boundary, taking the bottom border with it.
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(
                        search.ActualHeight >= search.DesiredSize.Height - 0.5,
                        "The search box is arranged shorter than it measured, so its bottom edge is clipped.");

                    // And it sits inside the title bar it lives in, rather than overhanging it.
                    Controls.TitleBar titleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    Point searchBottom = search.TransformToAncestor(titleBar).Transform(new Point(0, search.ActualHeight));
                    Assert.True(
                        searchBottom.Y <= titleBar.ActualHeight + 0.5,
                        "The search box overhangs the bottom of the title bar.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TitleBarSearch_IsCenteredInWindowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                    Assert.Equal(320.0, search.Width, 0.01);
                    Assert.Equal(320.0, search.MinWidth, 0.01);
                    Assert.Equal(475.0, search.MaxWidth, 0.01);
                    Assert.Equal(320.0, search.ActualWidth, 0.5);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window), 1.0);
                    Assert.Equal(GetVisualCenterY(shellTitleBar, window) + 0.5, GetVisualCenterY(search, window), 1.0);

                    // AutoSuggestBox forwards keyboard focus to its inner PART_TextBox, so
                    // Focus() reports false while focus genuinely lands within the control.
                    _ = search.Focus();
                    Assert.True(search.IsKeyboardFocusWithin, "Search should accept keyboard focus.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(320.0, search.ActualWidth, 0.5);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window), 1.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_UsesHorizontalNavigationChromeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    window.NavigateTo("buttons");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);

                    Button titleBarToggle = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(shellTitleBar, "PART_PaneToggleButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);
                    Assert.Equal(40.0, titleBarToggle.ActualWidth, 0.5);

                    TextBlock titleBarGlyph = Assert.IsType<TextBlock>(DemoTestHost.FindVisualChildren<TextBlock>(titleBarToggle).FirstOrDefault(), exactMatch: false);
                    Assert.Equal(16.0, titleBarGlyph.FontSize, 0.01);

                    Button titleBarBack = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(shellTitleBar, "PART_BackButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(titleBarToggle, window), "Back should occupy the first title-bar navigation slot.");
                    TextBlock titleBarBackGlyph = Assert.IsType<TextBlock>(DemoTestHost.FindVisualChildren<TextBlock>(titleBarBack).FirstOrDefault(), exactMatch: false);

                    Controls.NavigationViewItem firstItem = Assert.IsType<Controls.NavigationViewItem>(nav.Items.Count > 0 ? nav.Items[0] as Controls.NavigationViewItem : null);
                    Controls.FontIcon itemGlyph = Assert.IsType<Controls.FontIcon>(DemoTestHost.FindVisualChildren<Controls.FontIcon>(firstItem).FirstOrDefault(), exactMatch: false);
                    Assert.Equal(GetVisualCenterX(itemGlyph, window), GetVisualCenterX(titleBarBackGlyph, window), 2.5);

                    ContentPresenter titleIcon = Assert.IsType<ContentPresenter>(DemoTestHost.FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    Image titleIconImage = Assert.IsType<Image>(DemoTestHost.FindVisualChildren<Image>(titleIcon).FirstOrDefault(), exactMatch: false);
                    Assert.Equal(16.0, titleIconImage.ActualWidth, 0.5);
                    Assert.Equal(16.0, titleIconImage.ActualHeight, 0.5);
                    Assert.True(GetVisualX(titleIcon, window) >= GetVisualX(titleBarToggle, window) + titleBarToggle.ActualWidth - 0.5,
                        "Title identity should start after the title-bar navigation slot.");

                    _ = nav.ApplyTemplate();
                    Button internalToggle = Assert.IsType<Button>(nav.Template.FindName(Controls.NavigationView.PART_PaneToggleButton, nav));
                    Assert.Equal(Visibility.Collapsed, internalToggle.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_FirstGlyphTracksBackAvailabilityAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    nav.IsBackButtonVisible = true;
                    nav.IsBackEnabled = true;

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    Button titleBarBack = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(shellTitleBar, "PART_BackButton"), exactMatch: false);
                    Button titleBarToggle = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(shellTitleBar, "PART_PaneToggleButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(titleBarToggle, window), "Back should occupy the first title-bar navigation slot.");
                    Assert.Equal(GetVisualCenterY(titleBarBack, window), GetVisualCenterY(titleBarToggle, window), 1.0);

                    ContentPresenter titleIcon = Assert.IsType<ContentPresenter>(DemoTestHost.FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    double titleIconWithBackX = GetVisualX(titleIcon, window);

                    nav.IsBackEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, titleBarBack.Visibility);
                    Assert.Equal(titleIconWithBackX - 42.0, GetVisualX(titleIcon, window), 1.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_KeepsNavigationItemsBelowTitleBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;

                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // FluenceWindow.DefaultTitleBarHeight, the WinUI 3 canonical expanded title bar.
                    Assert.Equal(48.0, window.TitleBarHeight, 0.01);

                    Controls.NavigationViewItem firstItem = Assert.IsType<Controls.NavigationViewItem>(nav.Items.Count > 0 ? nav.Items[0] as Controls.NavigationViewItem : null);
                    double? itemY = GetVisualY(firstItem, window);
                    Assert.True(itemY >= window.TitleBarHeight - 0.5,
                        "The first navigation item should be below the extended title bar. itemY=" + itemY.Value.ToString(format: null, CultureInfo.InvariantCulture) + ", titleBarHeight=" + window.TitleBarHeight.ToString(CultureInfo.InvariantCulture));
                    Assert.True(itemY <= window.TitleBarHeight + 14.0,
                        "The first navigation item should not keep the old extra title-bar spacer. itemY=" + itemY.Value.ToString(format: null, CultureInfo.InvariantCulture) + ", titleBarHeight=" + window.TitleBarHeight.ToString(CultureInfo.InvariantCulture));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TopPane_UsesNonExtendedTitleBarWithoutPaneToggleChromeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    window.ExtendsContentIntoTitleBar = false;
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    nav.IsPaneOpen = false;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    nav.IsBackEnabled = true;
                    nav.IsBackButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(window.ExtendsContentIntoTitleBar,
                        "Top NavigationView mode should keep the FluenceWindow title bar non-extended.");
                    Assert.True(nav.IsPaneOpen, "Top NavigationView mode should coerce IsPaneOpen=True.");
                    Assert.False(nav.IsPaneToggleButtonVisible,
                        "Top NavigationView mode should coerce the pane toggle hidden.");

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    Button titleBarToggle = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(shellTitleBar, "PART_PaneToggleButton"), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, titleBarToggle.Visibility);
                    Button titleBarBack = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(shellTitleBar, "PART_BackButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleBarBack.Visibility);
                    TextBlock titleBarBackGlyph = Assert.IsType<TextBlock>(DemoTestHost.FindVisualChildren<TextBlock>(titleBarBack).FirstOrDefault(), exactMatch: false);
                    Assert.Equal(16.0, titleBarBackGlyph.FontSize, 0.01);
                    ContentPresenter titleIcon = Assert.IsType<ContentPresenter>(DemoTestHost.FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"), exactMatch: false);
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(titleIcon, window), "Top mode back should be the first visible title-bar item.");
                    Assert.True(GetVisualX(titleBarBack, window) < GetVisualX(search, window), "Top mode back should appear before centered title-bar content.");

                    _ = nav.ApplyTemplate();
                    Button internalBack = Assert.IsType<Button>(nav.Template.FindName(Controls.NavigationView.PART_BackButton, nav));
                    Button? internalToggle = nav.Template.FindName(Controls.NavigationView.PART_PaneToggleButton, nav) as Button;
                    Assert.Equal(Visibility.Collapsed, internalBack.Visibility);
                    Assert.Null(internalToggle);

                    Controls.NavigationViewItem firstItem = Assert.IsType<Controls.NavigationViewItem>(nav.Items.Count > 0 ? nav.Items[0] as Controls.NavigationViewItem : null);
                    Assert.Equal(Visibility.Visible, firstItem.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsFooter_NavigatesToSelectableSettingsPageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    Controls.NavigationViewItem settings = Assert.IsType<Controls.NavigationViewItem>(DemoTestHost.FindByName<Controls.NavigationViewItem>(window, "SettingsNavigationItem"), exactMatch: false);
                    Assert.Null(DemoTestHost.FindByName<FrameworkElement>(window, "PaneModeToggle"));

                    InvokeSettingsItem(settings);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // A footer invocation clears the main selection, so read the hosting frame
                    // directly rather than through the selected-item helper.
                    Frame settingsFrame = Assert.IsType<Frame>(nav.Content, exactMatch: false);
                    _ = Assert.IsType<GallerySettingsPage>(settingsFrame.Content, exactMatch: false);
                    Assert.True(settings.IsSelected,
                        "The footer Settings item should show the same selected state as navigation list items.");
                    Assert.True(nav.FooterMenuItems.Contains(settings),
                        "Settings should live in the FooterMenuItems region.");
                    Assert.Same(settings, nav.SelectedFooterItem);
                    Assert.Null(nav.SelectedItem);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsFooter_CollapsesLabelWhenPaneClosedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    Controls.NavigationViewItem settings = Assert.IsType<Controls.NavigationViewItem>(DemoTestHost.FindByName<Controls.NavigationViewItem>(window, "SettingsNavigationItem"), exactMatch: false);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    // As a FooterMenuItems entry, Settings uses the standard NavigationViewItem template:
                    // the label is collapsed/shown by the template (it is not emptied), exactly like the
                    // main menu items. Content stays "Settings" throughout.
                    Assert.Equal("Settings", settings.Content as string, StringComparer.Ordinal);
                    ContentPresenter label = Assert.IsType<ContentPresenter>(DemoTestHost.FindByName<ContentPresenter>(settings, "ContentPresenter"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, label.Visibility);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    label = Assert.IsType<ContentPresenter>(DemoTestHost.FindByName<ContentPresenter>(settings, "ContentPresenter"), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, label.Visibility);
                    Assert.Equal(Visibility.Visible, settings.Visibility);
                    Controls.FontIcon settingsIcon = Assert.IsType<Controls.FontIcon>(settings.Icon);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsFooter_DoesNotForceTopPaneModeWhenOpenedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    Controls.NavigationViewItem settings = Assert.IsType<Controls.NavigationViewItem>(DemoTestHost.FindByName<Controls.NavigationViewItem>(window, "SettingsNavigationItem"), exactMatch: false);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nav.IsPaneOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    InvokeSettingsItem(settings);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.LeftCompact, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Opening Settings must preserve the real collapsed pane state.");

                    Controls.ComboBox navigationStyle = Assert.IsType<Controls.ComboBox>(DemoTestHost.FindByName<Controls.ComboBox>(nav.Content as DependencyObject, "NavigationStyleComboBox"), exactMatch: false);
                    Assert.Equal(2, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TopPane_OverflowButtonDoesNotOverlapTreesAtMinimumWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    window.Width = 698;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement overflowButton = Assert.IsType<FrameworkElement>(DemoTestHost.FindByName<FrameworkElement>(nav, Controls.NavigationView.PART_TopOverflowButton), exactMatch: false);
                    Assert.Equal(Visibility.Visible, overflowButton.Visibility);
                    int visibleNavigationItems = nav.Items.OfType<Controls.NavigationViewItem>().Count(static item => item.Visibility is Visibility.Visible);
                    Assert.True(visibleNavigationItems > 1,
                        "Top pane should show every navigation item that fits before the overflow button would overlap the Top toggle status.");
                    Controls.NavigationViewItem settings = Assert.IsType<Controls.NavigationViewItem>(DemoTestHost.FindByName<Controls.NavigationViewItem>(window, "SettingsNavigationItem"), exactMatch: false);
                    double overflowRight = GetVisualX(overflowButton, nav) + overflowButton.ActualWidth;
                    double settingsLeft = GetVisualX(settings, nav);
                    Assert.True(overflowRight <= settingsLeft - 4.0 + 1.5, "The three-dot overflow entry should stop before it overlaps the Settings item.");

                    Controls.NavigationViewItem trees = Assert.IsType<Controls.NavigationViewItem>(nav.Items.OfType<Controls.NavigationViewItem>().FirstOrDefault(static navItem => string.Equals(navItem.Content as string, "Trees", StringComparison.Ordinal)));
                    if (trees.Visibility is Visibility.Visible)
                    {
                        double treesRight = GetVisualX(trees, nav) + trees.ActualWidth;
                        double overflowLeft = GetVisualX(overflowButton, nav);
                        Assert.True(treesRight <= overflowLeft - 4.0 + 1.5, "Trees must not overlap the three-dot overflow entry.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        // This test drives the shell's title-bar pane toggle to observe the sidebar-width
        // animation, so it is a shell test even though its name carries the GallerySettingsPage
        // prefix. See Task 26's method-to-class mapping for the reconciliation.
        [Fact]
        public Task GallerySettingsPage_NavigationStyleCombo_FollowsShellPaneToggleAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static delegate
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
                    Assert.True(nav.IsPaneOpen, "Left navigation should be expanded before the pane toggle is clicked.");
                    Assert.Equal(1, navigationStyle.SelectedIndex);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    Button titleBarToggle = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(shellTitleBar, "PART_PaneToggleButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleBarToggle.Visibility);

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.True(nav.GetPaneColumnWidthForTesting() > 48.0,
                        "Collapsing Left navigation should start the sidebar width animation instead of snapping to compact width.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.False(nav.IsPaneOpen,
                        "Clicking the shell pane toggle should collapse the Left pane.");
                    Assert.Equal(2, navigationStyle.SelectedIndex);

                    await WaitForAnimationAndDrainByDelayAsync(window.Dispatcher, 220).ConfigureAwait(true);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    titleBarToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, titleBarToggle));
                    Assert.True(nav.GetPaneColumnWidthForTesting() < 280.0, "Expanding Left navigation should start from the current compact width instead of snapping open.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);
                    Assert.True(nav.IsPaneOpen,
                        "Expanded Left should keep the pane open after the second pane-toggle click.");
                    Assert.Equal(1, navigationStyle.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_TrimsTitleToSearchClearanceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 1200;
                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Trim Before Search");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    TextBlock titleText = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(shellTitleBar, "PART_TitleText"), exactMatch: false);
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleText.Visibility);
                    double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                    double searchLeft = GetVisualX(search, window);
                    double titleClearanceRight = searchLeft - 12.0;
                    Assert.True(titleRight <= titleClearanceRight, "The title text should not cross the 12px search clearance.");
                    Assert.Equal(titleClearanceRight, titleRight, 10.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_HidesTitleTextWhenItOverlapsSearchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 760;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    ContentPresenter titleIcon = Assert.IsType<ContentPresenter>(DemoTestHost.FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter"), exactMatch: false);
                    TextBlock titleText = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(shellTitleBar, "PART_TitleText"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleIcon.Visibility);
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                        double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window);
                        Assert.True(titleRight <= searchLeft - 12.0, "Visible title text must keep a 12px clearance before the search box.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_DoesNotLetTitleOverlapSearchAtMinimumWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 698;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Must Never Overlap Search");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                    Assert.Equal(window.ActualWidth / 2.0, GetVisualCenterX(search, window), 1.0);

                    TextBlock titleText = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(shellTitleBar, "PART_TitleText"), exactMatch: false);
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(search, window);
                        Assert.True(titleRight <= searchLeft - 12.0, "Visible title text must keep a 12px clearance before the centered search box.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ExtendedTitleBar_RestoresTitleTextWhenSearchHasRoomAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nav.IsPaneToggleButtonVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.Width = 760;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(show: true, window.Icon);
                    window.SetUserShowTitle(show: true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.TitleBar shellTitleBar = Assert.IsType<Controls.TitleBar>(DemoTestHost.FindByName<Controls.TitleBar>(window, "ShellTitleBar"), exactMatch: false);
                    TextBlock titleText = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(shellTitleBar, "PART_TitleText"), exactMatch: false);
                    if (titleText.Visibility is Visibility.Visible)
                    {
                        Controls.AutoSuggestBox setupSearch = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                        double titleRight = GetVisualX(titleText, window) + titleText.ActualWidth;
                        double searchLeft = GetVisualX(setupSearch, window);
                        Assert.True(titleRight <= searchLeft - 12.0, "Setup should hide or trim title text before it crosses the 12px search clearance.");
                    }

                    window.Width = 1200;
                    window.SetUserShowTitle(show: true, "Fluence.Wpf");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    titleText = Assert.IsType<TextBlock>(DemoTestHost.FindByName<TextBlock>(shellTitleBar, "PART_TitleText"));
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, titleText.Visibility);
                    Assert.Equal("Fluence.Wpf", titleText.Text, StringComparer.Ordinal);
                    Assert.True(GetVisualX(titleText, window) + titleText.ActualWidth + 12.0 <= GetVisualX(search, window),
                        "Visible title text should keep the search clearance gap.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TitleBarSearch_DoesNotShiftWhenChromeOptionsChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(DemoTestHost.FindByName<Controls.AutoSuggestBox>(window, "NavSearchBox"), exactMatch: false);

                    double? initialX = GetVisualX(search, window);

                    window.SetUserShowIcon(show: false, window.Icon);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window), 1.0);

                    window.SetUserShowTitle(show: false, window.Title);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window), 1.0);

                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    window.IsMaximizeButtonVisible = Visibility.Collapsed;
                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(initialX ?? double.MaxValue, GetVisualX(search, window), 1.0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_NonHomePagesExposeInlineSourceSamplesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        // The Iconography catalog page renders directly without DemoSampleControl source samples.
                        if (expectation.PageType == typeof(GalleryIconsPage))
                        {
                            continue;
                        }

                        window.NavigateTo(expectation.Tag);
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();
                        WpfTestSta.DrainDispatcher(window.Dispatcher);

                        object content = GetSelectedPageContent(window);
                        DependencyObject root = Assert.IsType<DependencyObject>(content, exactMatch: false);

                        bool found = DemoTestHost.FindVisualChildren<DemoSampleControl>(root).Any(static sample => !string.IsNullOrWhiteSpace(sample.XamlSource));
                        Assert.True(found, "Page must expose at least one inline XAML source sample: " + expectation.PageType.Name);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static async Task SelectMainWindowNavPageAsync(MainWindow window, Dispatcher dispatcher, string itemContent)
        {
            Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

            window.NavigateTo(itemContent);
            WpfTestSta.DrainDispatcher(dispatcher);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Loaded, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ContextIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(dispatcher);

            Controls.NavigationViewItem? selected = nav.SelectedItem as Controls.NavigationViewItem;
            string? selectedLabel = selected is null ? null : selected.Content as string;
            string? selectedTag = selected is null ? null : selected.Tag as string;
            bool matchesRequest = string.Equals(selectedLabel, itemContent, StringComparison.OrdinalIgnoreCase) ||
                (selectedTag?.IndexOf(itemContent, StringComparison.OrdinalIgnoreCase) >= 0);
            if (selected is null || nav.Content is null || !matchesRequest)
            {
                Assert.Fail(string.Format("Navigation item '{0}' should exist.", itemContent));
            }
        }

        /// <summary>
        /// Returns the gallery page the shell is showing. The pages are Page objects, so the shell
        /// hosts them in a Frame rather than assigning the navigation view's content directly.
        /// </summary>
        /// <param name="window">The shell window.</param>
        /// <returns>The hosted page.</returns>
        private static object GetSelectedPageContent(MainWindow window)
        {
            Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);

            Assert.NotNull(nav.SelectedItem as Controls.NavigationViewItem);
            Frame frame = Assert.IsType<Frame>(nav.Content, exactMatch: false);
            return frame.Content;
        }

        private static void InvokeTitleBarBack(Controls.TitleBar titleBar)
        {
            Button backButton = Assert.IsType<Button>(DemoTestHost.FindByName<Button>(titleBar, "PART_BackButton"), exactMatch: false);
            backButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, backButton));
            WpfTestSta.DrainDispatcher(titleBar.Dispatcher);
        }

        private static void InvokeSettingsItem(Controls.NavigationViewItem settingsItem)
        {
            // Settings is a FooterMenuItems entry; drive selection through the same control path a
            // click/keyboard invocation uses (raises ItemInvoked and shows the footer indicator).
            Controls.NavigationView.FromItemContainer(settingsItem)?.SelectFooterMenuItem(settingsItem);
            WpfTestSta.DrainDispatcher(settingsItem.Dispatcher);
        }

        private sealed class DemoPageExpectation(string tag, Type pageType)
        {
            public string Tag { get; } = tag;

            public Type PageType { get; } = pageType;
        }

        private static readonly DemoPageExpectation[] PageExpectations =
        [
            new("icons", typeof(GalleryIconsPage)),
            new("typography", typeof(GalleryTypographyPage)),
            new("accessibility", typeof(GalleryAccessibilityPage)),
            new("buttons", typeof(GalleryButtonsPage)),
            new("selection", typeof(GallerySelectionPage)),
            new("inputs", typeof(GalleryInputsPage)),
            new("data binding", typeof(GalleryDataBindingPage)),
            new("data", typeof(GalleryDataPage)),
            new("trees", typeof(GalleryTreesPage)),
            new("menus", typeof(GalleryMenusPage)),
            new("navigation", typeof(GalleryNavigationPage)),
            new("tabs", typeof(GalleryTabsPage)),
            new("layout", typeof(GalleryLayoutPage)),
            new("status", typeof(GalleryStatusPage)),
        ];

        private static void AssertButtonShowsGlyph(Controls.Button button, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindButtonGlyphTextBlock(button, glyph), exactMatch: false);
            Assert.True(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.True(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");
        }

        private static TextBlock? FindButtonIconTextBlock(Controls.Button button)
        {
            return FindVisualChildren<TextBlock>(button).FirstOrDefault(static textBlock => textBlock.FontFamily is FontFamily fontFamily && fontFamily.Source?.IndexOf("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void AssertGlyphWithinButtonBounds(Window window, Controls.Button button, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindButtonGlyphTextBlock(button, glyph), exactMatch: false);

            Assert.True(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.True(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");

            Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            double buttonRight = buttonOrigin.X + button.ActualWidth;
            double buttonBottom = buttonOrigin.Y + button.ActualHeight;
            double glyphRight = glyphOrigin.X + glyphTextBlock.ActualWidth;
            double glyphBottom = glyphOrigin.Y + glyphTextBlock.ActualHeight;

            Assert.True(glyphOrigin.X >= buttonOrigin.X - 0.5, "Expected button glyph should not render left of the button.");
            Assert.True(glyphOrigin.Y >= buttonOrigin.Y - 0.5, "Expected button glyph should not render above the button.");
            Assert.True(glyphRight <= buttonRight + 0.5, "Expected button glyph should not render right of the button.");
            Assert.True(glyphBottom <= buttonBottom + 0.5, "Expected button glyph should not render below the button.");
        }

        private static void AssertControlHasThemedBorder(System.Windows.Controls.Control control)
        {
            Assert.Equal(new Thickness(1), control.BorderThickness);
            Assert.NotNull(control.BorderBrush);
        }

        // Visual-tree-only walker, distinct from GalleryIconsPageTests' logical-plus-visual
        // CountVisualChildren: this one counts the ToggleSwitch sample on the Selection page,
        // which is always realized in the visual tree.
        private static int CountVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            int count = 0;
            foreach (T child in FindVisualChildren<T>(root))
            {
                count++;
            }

            return count;
        }

        private static Controls.RepeatButton? FindRepeatButtonByContent(DependencyObject root, string content)
        {
            return FindVisualChildren<Controls.RepeatButton>(root).FirstOrDefault(repeatButton => string.Equals(repeatButton.Content as string, content, StringComparison.Ordinal));
        }

        private static void AssertBrushIsTransparent(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                Assert.Equal(0, solid.Color.A);
            }
        }

        private static Controls.ToggleButton? FindToggleButtonByContent(DependencyObject root, string content)
        {
            return FindVisualChildren<Controls.ToggleButton>(root).FirstOrDefault(button => string.Equals(button.Content as string, content, StringComparison.Ordinal));
        }

        private static Controls.RadioButton? FindRadioButtonByContent(DependencyObject root, string content)
        {
            return FindVisualChildren<Controls.RadioButton>(root).FirstOrDefault(radioButton => string.Equals(radioButton.Content as string, content, StringComparison.Ordinal));
        }

        private sealed class BindingErrorListener : TraceListener
        {
            public List<string> Messages { get; } = [];

            public override void Write(string? message)
            {
                // WPF writes the trace header through Write; only the message line matters.
            }

            public override void WriteLine(string? message)
            {
                if (message is not null && !string.IsNullOrWhiteSpace(message))
                {
                    Messages.Add(message);
                }
            }
        }
    }
}
