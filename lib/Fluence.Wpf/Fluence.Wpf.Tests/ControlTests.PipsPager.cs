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
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.PipsPager"/>.
    /// </summary>
    public partial class ControlTests
    {
        private static System.Windows.Controls.Primitives.ToggleButton? GetPipAt(System.Windows.Controls.StackPanel host, int offset)
        {
            return offset >= 0 && offset < host.Children.Count
                ? host.Children[offset] as System.Windows.Controls.Primitives.ToggleButton
                : null;
        }

        [Fact]
        public Task PipsPager_DefaultStyle_AppliesAndTemplatePartsResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.PipsPager)));

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new();

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(0, pager.NumberOfPages);
                    Assert.Equal(0, pager.SelectedPageIndex);
                    Assert.Equal(5, pager.MaxVisiblePips);
                    Assert.Equal(System.Windows.Controls.Orientation.Horizontal, pager.Orientation);
                    Assert.Equal(PipsPagerButtonVisibility.Collapsed, pager.PreviousButtonVisibility);
                    Assert.Equal(PipsPagerButtonVisibility.Collapsed, pager.NextButtonVisibility);

                    System.Windows.Controls.Button previous = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_PreviousButton"));
                    System.Windows.Controls.Button next = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_NextButton"));
                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));
                    Assert.Empty(host.Children);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_FivePages_RendersFivePipsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 5 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));
                    Assert.Equal(5, host.Children.Count);

                    for (int offset = 0; offset < 5; offset++)
                    {
                        System.Windows.Controls.Primitives.ToggleButton pip = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, offset));
                        Assert.Equal(offset is 0, pip.IsChecked);
                        Assert.Equal(
                            string.Format(CultureInfo.InvariantCulture, "Page {0}", offset + 1),
                            AutomationProperties.GetName(pip), StringComparer.Ordinal);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_PipClick_SelectsPageAndRaisesSelectedIndexChangedAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 5 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    int oldIndex = -1;
                    int newIndex = -1;
                    int raiseCount = 0;
                    pager.SelectedIndexChanged += (_, args) =>
                    {
                        oldIndex = args.OldIndex;
                        newIndex = args.NewIndex;
                        raiseCount++;
                    };

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));

                    System.Windows.Controls.Primitives.ToggleButton pip = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, 3));

                    pip.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, pip));
                    Assert.Equal(3, pager.SelectedPageIndex);
                    Assert.Equal(1, raiseCount);
                    Assert.Equal(0, oldIndex);
                    Assert.Equal(3, newIndex);

                    Assert.True(GetPipAt(host, 3)?.IsChecked,
                        "The clicked pip must render as the selected pip.");
                    Assert.False(GetPipAt(host, 0)?.IsChecked,
                        "The previously selected pip must uncheck.");

                    // Re-clicking the selected pip must not move the selection or re-raise.
                    pip.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, pip));
                    Assert.Equal(3, pager.SelectedPageIndex);
                    Assert.Equal(1, raiseCount);
                    Assert.True(GetPipAt(host, 3)?.IsChecked,
                        "Re-clicking the selected pip must keep it checked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_NavigationButtons_ChangeSelectionAndRespectBoundsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new()
                {
                    NumberOfPages = 3,
                    PreviousButtonVisibility = PipsPagerButtonVisibility.Visible,
                    NextButtonVisibility = PipsPagerButtonVisibility.Visible,
                };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Button previous = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_PreviousButton"));
                    System.Windows.Controls.Button next = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_NextButton"));

                    Assert.False(previous.IsEnabled, "The previous button must be disabled at the first page.");
                    Assert.True(next.IsEnabled, "The next button must be enabled while pages remain ahead.");

                    next.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, next));
                    Assert.Equal(1, pager.SelectedPageIndex);
                    Assert.True(previous.IsEnabled, "The previous button must enable once off the first page.");

                    next.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, next));
                    Assert.Equal(2, pager.SelectedPageIndex);
                    Assert.False(next.IsEnabled, "The next button must be disabled at the last page.");

                    // Raising Click bypasses IsEnabled, so this also proves the coercion clamp.
                    next.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, next));
                    Assert.Equal(2, pager.SelectedPageIndex);

                    previous.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, previous));
                    Assert.Equal(1, pager.SelectedPageIndex);

                    previous.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, previous));
                    previous.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, previous));
                    Assert.Equal(0, pager.SelectedPageIndex);
                    Assert.False(previous.IsEnabled,
                        "The previous button must be disabled again at the first page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_MaxVisiblePips_WindowsAroundSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 10, MaxVisiblePips = 3 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));

                    // Selection at the leading edge: the window clamps to the first pages.
                    Assert.Equal(3, host.Children.Count);
                    Assert.Equal("Page 1", AutomationProperties.GetName(GetPipAt(host, 0)!), StringComparer.Ordinal);
                    Assert.True(GetPipAt(host, 0)?.IsChecked, "The first pip must be checked at page 1.");

                    // Mid-range selection: the window centers on the selected page.
                    pager.SelectedPageIndex = 5;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(3, host.Children.Count);
                    Assert.Equal("Page 5", AutomationProperties.GetName(GetPipAt(host, 0)!), StringComparer.Ordinal);
                    Assert.Equal("Page 7", AutomationProperties.GetName(GetPipAt(host, 2)!), StringComparer.Ordinal);
                    Assert.True(GetPipAt(host, 1)?.IsChecked,
                        "The centered window must check its middle pip.");

                    // Selection at the trailing edge: the window clamps to the last pages.
                    pager.SelectedPageIndex = 9;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(3, host.Children.Count);
                    Assert.Equal("Page 8", AutomationProperties.GetName(GetPipAt(host, 0)!), StringComparer.Ordinal);
                    Assert.Equal("Page 10", AutomationProperties.GetName(GetPipAt(host, 2)!), StringComparer.Ordinal);
                    Assert.True(GetPipAt(host, 2)?.IsChecked,
                        "The clamped window must check its last pip for the last page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_SelectedPageIndex_CoercesIntoRangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.PipsPager pager = new() { NumberOfPages = 5 };
                Assert.Equal(0, pager.SelectedPageIndex);

                pager.SelectedPageIndex = -3;
                Assert.Equal(0, pager.SelectedPageIndex);

                pager.SelectedPageIndex = 99;
                Assert.Equal(4, pager.SelectedPageIndex);

                int oldIndex = -1;
                int newIndex = -1;
                pager.SelectedIndexChanged += (_, args) =>
                {
                    oldIndex = args.OldIndex;
                    newIndex = args.NewIndex;
                };

                pager.NumberOfPages = 3;
                Assert.Equal(2, pager.SelectedPageIndex);
                Assert.Equal(4, oldIndex);
                Assert.Equal(2, newIndex);

                pager.NumberOfPages = 0;
                Assert.Equal(0, pager.SelectedPageIndex);

                pager.NumberOfPages = -7;
                Assert.Equal(0, pager.NumberOfPages);
            });
        }

        [Fact]
        public Task PipsPager_VerticalOrientation_StacksPipsVerticallyAndSwapsChevronsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // The chevron FontIcons are button content, so they only enter the visual tree
                // once the navigation buttons are visible and have applied their templates.
                Controls.PipsPager pager = new()
                {
                    NumberOfPages = 3,
                    PreviousButtonVisibility = PipsPagerButtonVisibility.Visible,
                    NextButtonVisibility = PipsPagerButtonVisibility.Visible,
                };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));
                    Controls.FontIcon previousGlyph = Assert.IsAssignableFrom<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(pager, "PreviousGlyph"));
                    Controls.FontIcon nextGlyph = Assert.IsAssignableFrom<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(pager, "NextGlyph"));

                    Assert.Equal(System.Windows.Controls.Orientation.Horizontal, host.Orientation);
                    Assert.Equal("\uE76B", previousGlyph.Glyph, StringComparer.Ordinal);
                    Assert.Equal("\uE76C", nextGlyph.Glyph, StringComparer.Ordinal);

                    pager.Orientation = System.Windows.Controls.Orientation.Vertical;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(System.Windows.Controls.Orientation.Vertical, host.Orientation);
                    Assert.Equal("\uE70E", previousGlyph.Glyph, StringComparer.Ordinal);
                    Assert.Equal("\uE70D", nextGlyph.Glyph, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_ButtonVisibilityEnum_ControlsNavigationButtonVisibilityAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 3 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Button previous = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_PreviousButton"));
                    System.Windows.Controls.Button next = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_NextButton"));

                    Assert.Equal(Visibility.Collapsed, previous.Visibility);
                    Assert.Equal(Visibility.Collapsed, next.Visibility);

                    pager.PreviousButtonVisibility = PipsPagerButtonVisibility.Visible;
                    pager.NextButtonVisibility = PipsPagerButtonVisibility.Visible;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, previous.Visibility);
                    Assert.Equal(Visibility.Visible, next.Visibility);

                    // VisibleOnPointerOver shows the buttons only while the pointer is over the
                    // pager (template MultiTrigger on IsMouseOver); without hover they collapse.
                    pager.PreviousButtonVisibility = PipsPagerButtonVisibility.VisibleOnPointerOver;
                    pager.NextButtonVisibility = PipsPagerButtonVisibility.VisibleOnPointerOver;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(pager.IsMouseOver, "The pager must not be hovered in this headless test.");
                    Assert.Equal(Visibility.Collapsed, previous.Visibility);
                    Assert.Equal(Visibility.Collapsed, next.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_ArrowKeys_MoveSelectionWhileFocusIsInsideAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 5 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));

                    System.Windows.Controls.Primitives.ToggleButton pip = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, 0));
                    _ = pip.Focus();

                    PresentationSource source = Assert.IsAssignableFrom<PresentationSource>(PresentationSource.FromVisual(pip));

                    pip.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.Equal(1, pager.SelectedPageIndex);

                    pip = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, 1));
                    pip.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Left)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.Equal(0, pager.SelectedPageIndex);

                    pip = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, 0));
                    pip.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Left)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.Equal(0, pager.SelectedPageIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_ThemeCycle_PipBrushesResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys =
                [
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "TextFillColorDisabledBrush",
                    "ControlStrongFillColorDefaultBrush",
                    "ControlStrongFillColorDisabledBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.NotNull(app.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public Task PipsPager_PipFills_UseNeutralStrongFillRolesAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new()
                {
                    NumberOfPages = 3,
                    PreviousButtonVisibility = PipsPagerButtonVisibility.Visible,
                    NextButtonVisibility = PipsPagerButtonVisibility.Visible,
                };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));

                    object strongFill = Assert.IsAssignableFrom<object>(app.TryFindResource("ControlStrongFillColorDefaultBrush"));

                    // WinUI maps PipsPagerNavigationButtonForeground at rest to
                    // ControlStrongFillColorDefaultBrush; the chevron buttons must share the same
                    // neutral strong fill as the pips when not hovered or pressed. The next button
                    // is enabled at the first page, so its Foreground reflects the rest setter
                    // (the previous button is disabled at page 0 and shows the disabled brush).
                    System.Windows.Controls.Button nextButton = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_NextButton"));
                    Assert.True(nextButton.IsEnabled, "The next button must be enabled at the first page.");
                    Assert.Same(strongFill, nextButton.Foreground);

                    // WinUI maps PipsPagerNavigationButtonForegroundDisabled to
                    // ControlStrongFillColorDisabledBrush (not the text disabled fill). The
                    // previous button is disabled at page 0, so its Foreground must reflect that
                    // disabled setter.
                    object strongFillDisabled = Assert.IsAssignableFrom<object>(app.TryFindResource("ControlStrongFillColorDisabledBrush"));
                    System.Windows.Controls.Button previousButton = Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(pager, "PART_PreviousButton"));
                    Assert.False(previousButton.IsEnabled, "The previous button must be disabled at the first page.");
                    Assert.Same(strongFillDisabled, previousButton.Foreground);

                    System.Windows.Controls.Primitives.ToggleButton selectedPip = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, 0));
                    System.Windows.Controls.Primitives.ToggleButton restPip = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, 1));

                    System.Windows.Shapes.Ellipse selectedDot =
                        Assert.IsAssignableFrom<System.Windows.Shapes.Ellipse>(FindVisualChildByName<System.Windows.Shapes.Ellipse>(selectedPip, "Pip"));
                    System.Windows.Shapes.Ellipse restDot =
                        Assert.IsAssignableFrom<System.Windows.Shapes.Ellipse>(FindVisualChildByName<System.Windows.Shapes.Ellipse>(restPip, "Pip"));

                    // WinUI PipsPager pips are neutral: rest and selected dots both use the
                    // strong fill (PipsPagerSelectionIndicatorForeground / ...Selected); the
                    // selected pip is distinguished by size, not by the accent color.
                    Assert.Same(strongFill, restDot.Fill);
                    Assert.Same(strongFill, selectedDot.Fill);
                    Assert.Equal(4.0, restDot.Width, 0.01);

                    // The selected size is animated (83ms ControlFasterAnimationDuration), so
                    // sample the dot until the storyboard settles at the 6px selected size.
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(selectedDot.Width - 6.0) < 0.01).ConfigureAwait(true),
                        "The selected pip dot must grow to the 6px selected size.");

                    pager.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(app.TryFindResource("ControlStrongFillColorDisabledBrush"), restDot.Fill);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_PipSizeMorph_AnimatesSelectionAndSurvivesWindowRebuildAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 10, MaxVisiblePips = 3 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));

                    static System.Windows.Shapes.Ellipse? DotAt(System.Windows.Controls.StackPanel pipsHost, int offset)
                    {
                        System.Windows.Controls.Primitives.ToggleButton? pip = GetPipAt(pipsHost, offset);
                        return pip is null
                            ? null
                            : FindVisualChildByName<System.Windows.Shapes.Ellipse>(pip, "Pip");
                    }

                    static bool IsDotSize(System.Windows.Shapes.Ellipse? dot, double size)
                    {
                        return dot is not null
                            && Math.Abs(dot.Width - size) < 0.01
                            && Math.Abs(dot.Height - size) < 0.01;
                    }

                    // Pips are created with IsChecked already true, so the IsChecked
                    // EnterActions must run when the template applies and settle the
                    // selected dot at 6x6 (83ms ControlFasterAnimationDuration morph).
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 0), 6.0)).ConfigureAwait(true),
                        "The initially selected pip must animate to the 6px selected size at load.");
                    Assert.True(IsDotSize(DotAt(host, 1), 4.0), "An unselected pip must rest at 4px.");

                    // In-place selection change (the window stays clamped at the start):
                    // the old pip's ExitActions shrink it back to 4 while the new pip grows to 6.
                    pager.SelectedPageIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 1), 6.0)).ConfigureAwait(true),
                        "The newly selected pip must animate up to the 6px selected size.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 0), 4.0)).ConfigureAwait(true),
                        "The previously selected pip must animate back to the 4px rest size.");

                    // Window rebuild (mid-range selection recreates the pips): the recreated
                    // selected pip must still land at 6x6 because its trigger condition is
                    // already true when the recreated template applies.
                    pager.SelectedPageIndex = 5;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Page 5", AutomationProperties.GetName(GetPipAt(host, 0)!), StringComparer.Ordinal);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 1), 6.0)).ConfigureAwait(true),
                        "A recreated selected pip must animate to the 6px selected size.");
                    Assert.True(IsDotSize(DotAt(host, 0), 4.0), "A recreated unselected pip must rest at 4px.");
                    Assert.True(IsDotSize(DotAt(host, 2), 4.0), "A recreated unselected pip must rest at 4px.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_AutomationPeer_ReportsGroupClassNameAndNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 3 };
                AutomationProperties.SetName(pager, "Gallery pager");

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer peer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(pager));
                    _ = Assert.IsAssignableFrom<Automation.PipsPagerAutomationPeer>(peer);
                    Assert.Equal("PipsPager", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                    Assert.Equal("Gallery pager", peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
