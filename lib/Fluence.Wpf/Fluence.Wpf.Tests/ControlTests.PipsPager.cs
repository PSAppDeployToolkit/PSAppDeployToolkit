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
        public Task PipsPager_ExceedingMaxVisiblePips_RealizesEveryPipAndClampsTheViewportAsync()
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

                    // Every page gets a pip even past MaxVisiblePips: the count no longer bounds
                    // what is realized, only how much of the run the viewport shows.
                    Assert.Equal(10, host.Children.Count);
                    Assert.Equal("Page 1", AutomationProperties.GetName(GetPipAt(host, 0)!), StringComparer.Ordinal);
                    Assert.Equal("Page 10", AutomationProperties.GetName(GetPipAt(host, 9)!), StringComparer.Ordinal);
                    Assert.True(GetPipAt(host, 0)?.IsChecked, "The first pip must be checked at page 1.");

                    // WinUI CalculateScrollViewerSize: defaultPipSize * (visible - 1) + selectedPipSize,
                    // and Fluence pips share one 20x20 box, so the viewport is 20 * MaxVisiblePips
                    // over an extent of 20 * NumberOfPages.
                    System.Windows.Controls.ScrollViewer viewer = Assert.IsAssignableFrom<System.Windows.Controls.ScrollViewer>(FindVisualChildByName<System.Windows.Controls.ScrollViewer>(pager, "PART_PipsScrollViewer"));
                    Assert.Equal(60.0, viewer.ViewportWidth, 0.5);
                    Assert.Equal(200.0, viewer.ExtentWidth, 0.5);
                    Assert.Equal(0.0, viewer.HorizontalOffset, 0.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Both halves of the bring-into-view contract. A pip asking to be brought into view must
        /// not move the pager's own viewport, which the pager scrolls on its own terms, but the
        /// request must still reach the ancestors so an app scroller brings the whole pager on
        /// screen (WinUI re-raises the same way from PipsPager::OnScrollViewerBringIntoViewRequested).
        /// </summary>
        [Fact]
        public Task PipsPager_PipBringIntoView_KeepsTheViewportStillAndScrollsTheOuterViewerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 10, MaxVisiblePips = 3 };
                System.Windows.Controls.StackPanel content = new();
                _ = content.Children.Add(new System.Windows.Controls.Border { Height = 400 });
                _ = content.Children.Add(pager);
                System.Windows.Controls.ScrollViewer outerViewer = new()
                {
                    Height = 100,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    Content = content,
                };

                try
                {
                    window.Content = outerViewer;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel host = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost"));
                    System.Windows.Controls.ScrollViewer viewer = Assert.IsAssignableFrom<System.Windows.Controls.ScrollViewer>(FindVisualChildByName<System.Windows.Controls.ScrollViewer>(pager, "PART_PipsScrollViewer"));

                    Assert.Equal(0.0, viewer.HorizontalOffset, 0.5);
                    Assert.Equal(0.0, outerViewer.VerticalOffset, 0.5);

                    // The last pip sits well past the three-pip viewport, so an unsuppressed
                    // request would scroll the run to its far end.
                    System.Windows.Controls.Primitives.ToggleButton lastPip =
                        Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(GetPipAt(host, 9));
                    lastPip.BringIntoView();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(0.0, viewer.HorizontalOffset, 0.5);
                    Assert.True(outerViewer.VerticalOffset > 0.0,
                        "The suppressed request must still be re-raised so an outer scroller brings the pager into view.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_SelectionInsideViewport_LeavesTheScrollOffsetStationaryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
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

                    System.Windows.Controls.ScrollViewer viewer = Assert.IsAssignableFrom<System.Windows.Controls.ScrollViewer>(FindVisualChildByName<System.Windows.Controls.ScrollViewer>(pager, "PART_PipsScrollViewer"));
                    Assert.Equal(0.0, viewer.HorizontalOffset, 0.5);

                    // Pages 1 through 3 all sit inside the opening viewport, so the run of pips must
                    // stay exactly where it is while the selection walks across it. The wait outlasts
                    // the 167ms scroll animation, so a viewport that did move would be caught settled
                    // at its new offset rather than mid-flight.
                    foreach (int pageIndex in new[] { 1, 2 })
                    {
                        pager.SelectedPageIndex = pageIndex;
                        WpfTestSta.DrainDispatcher(window.Dispatcher);
                        await WaitForAnimationAndDrainAsync(window.Dispatcher, 300).ConfigureAwait(true);
                        Assert.Equal(0.0, viewer.HorizontalOffset, 0.5);
                    }

                    Assert.True(GetPipAt(Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(pager, "PART_PipsHost")), 2)?.IsChecked,
                        "The third pip must be the checked one after selecting page 3.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_SelectionPastTheEdge_ScrollsTheViewportToThatEdgeOnlyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
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

                    System.Windows.Controls.ScrollViewer viewer = Assert.IsAssignableFrom<System.Windows.Controls.ScrollViewer>(FindVisualChildByName<System.Windows.Controls.ScrollViewer>(pager, "PART_PipsScrollViewer"));

                    // Page 5 sits at [80,100) while the viewport spans [0,60). Edge scrolling moves
                    // the minimum that puts it flush against the trailing edge: 100 - 60 = 40. A
                    // re-centering viewport would land on 60 instead, so this offset is what
                    // separates the two models.
                    pager.SelectedPageIndex = 4;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(viewer.HorizontalOffset - 40.0) < 0.5).ConfigureAwait(true),
                        "Selecting past the trailing edge must scroll the viewport just far enough to show the selected pip.");

                    // Back past the leading edge: the viewport spans [40,100), page 2 sits at
                    // [20,40), so the leading edge lands on the pip itself.
                    pager.SelectedPageIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(viewer.HorizontalOffset - 20.0) < 0.5).ConfigureAwait(true),
                        "Selecting past the leading edge must scroll back only as far as the selected pip.");

                    // The last page cannot scroll past the end of the run, so the offset clamps to
                    // the scrollable maximum (200 - 60).
                    pager.SelectedPageIndex = 9;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(viewer.HorizontalOffset - 140.0) < 0.5).ConfigureAwait(true),
                        "The last page must leave the viewport at the end of the pip run.");
                    Assert.Equal(140.0, viewer.ScrollableWidth, 0.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_ExternalViewportScroll_SnapsBackToThePagerTargetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
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

                    System.Windows.Controls.ScrollViewer viewer = Assert.IsAssignableFrom<System.Windows.Controls.ScrollViewer>(FindVisualChildByName<System.Windows.Controls.ScrollViewer>(pager, "PART_PipsScrollViewer"));

                    pager.SelectedPageIndex = 9;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(viewer.HorizontalOffset - 140.0) < 0.5).ConfigureAwait(true),
                        "The viewport must reach the end of the run before the external scroll.");

                    // Simulate input the hidden scrollbars cannot fully block (a bubbled Home key,
                    // wheel over a vertical pager): scroll the viewer directly, away from the
                    // pager's believed offset. The pager must snap the viewport back to its own
                    // target instead of letting the real and believed offsets desync, which used to
                    // leave the checked pip permanently outside the viewport.
                    viewer.ScrollToHorizontalOffset(0.0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(viewer.HorizontalOffset - 140.0) < 0.5).ConfigureAwait(true),
                        "An external viewport scroll must be snapped back to the pager-owned offset.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task PipsPager_VerticalOrientation_ClampsAndScrollsTheViewportOnTheVerticalAxisAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.PipsPager pager = new() { NumberOfPages = 10, MaxVisiblePips = 3 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.ScrollViewer viewer = Assert.IsAssignableFrom<System.Windows.Controls.ScrollViewer>(FindVisualChildByName<System.Windows.Controls.ScrollViewer>(pager, "PART_PipsScrollViewer"));

                    // Scroll away from the start on the horizontal axis first, so the flip has a
                    // stale cross-axis offset to release rather than a viewport already at zero.
                    pager.SelectedPageIndex = 9;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(viewer.HorizontalOffset - 140.0) < 0.5).ConfigureAwait(true),
                        "The horizontal viewport must reach the end of the pip run before the flip.");

                    pager.Orientation = System.Windows.Controls.Orientation.Vertical;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // The clamp moves to the vertical axis and the horizontal axis is freed, so the
                    // run is 3 pips tall over a 10 pip extent with nothing left scrolled sideways.
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => Math.Abs(viewer.VerticalOffset - 140.0) < 0.5).ConfigureAwait(true),
                        "The flip must re-run the edge scroll on the vertical axis for the same selection.");
                    Assert.Equal(60.0, viewer.ViewportHeight, 0.5);
                    Assert.Equal(200.0, viewer.ExtentHeight, 0.5);
                    Assert.Equal(0.0, viewer.HorizontalOffset, 0.5);

                    // Selecting back inside the vertical viewport leaves it where it is.
                    pager.SelectedPageIndex = 8;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 300).ConfigureAwait(true);
                    Assert.Equal(140.0, viewer.VerticalOffset, 0.5);
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
            return WpfTestSta.RunOnStaAsync(static () =>
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
            return WpfTestSta.RunOnStaAsync(static async () =>
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
        public Task PipsPager_PipSizeMorph_AnimatesSelectionAcrossTheViewportAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
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

                    // Selection change inside the opening viewport: the old pip's ExitActions
                    // shrink it back to 4 while the new pip grows to 6.
                    pager.SelectedPageIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 1), 6.0)).ConfigureAwait(true),
                        "The newly selected pip must animate up to the 6px selected size.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 0), 4.0)).ConfigureAwait(true),
                        "The previously selected pip must animate back to the 4px rest size.");

                    // A selection the viewport has to scroll to reaches a pip that was realized at
                    // load and has been sitting outside the viewport ever since. It must still run
                    // the same morph, and the pips it scrolled past must fall back to rest.
                    pager.SelectedPageIndex = 5;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Page 6", AutomationProperties.GetName(GetPipAt(host, 5)!), StringComparer.Ordinal);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 5), 6.0)).ConfigureAwait(true),
                        "A pip scrolled into the viewport must animate to the 6px selected size.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 1), 4.0)).ConfigureAwait(true),
                        "The pip the viewport scrolled away from must animate back to the 4px rest size.");
                    Assert.True(IsDotSize(DotAt(host, 4), 4.0), "An unselected pip must rest at 4px.");
                    Assert.True(IsDotSize(DotAt(host, 6), 4.0), "An unselected pip must rest at 4px.");
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
