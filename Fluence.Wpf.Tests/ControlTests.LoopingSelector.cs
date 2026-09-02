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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Fluence.Wpf.Helpers;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the looping selector primitive behind the <see cref="Controls.DatePicker"/>
    /// and <see cref="Controls.TimePicker"/> flyout columns: the repeating item source, the
    /// column's default style and template part, the two-way sync between the item-unit scroll
    /// offset and the selection, the keyboard contract, and virtualization.
    /// </summary>
    public partial class ControlTests
    {
        /// <summary>
        /// The number of padding rows a looping column keeps above the selected row; the
        /// selected row is the middle one of a nine-row viewport. Aliases the control's own
        /// constant so the tests can never drift from the geometry the control actually uses.
        /// </summary>
        private const int LoopingPaddingItemsCount = Controls.LoopingSelectorList.PaddingItemsCount;

        /// <summary>
        /// Returns how many distinct values a selector column holds, which for a looping column
        /// is the length of one band rather than the length of the repeated list.
        /// </summary>
        /// <param name="selector">The column to measure.</param>
        /// <returns>The number of distinct values.</returns>
        private static int LoopingColumnSourceCount(Selector selector)
        {
            return LoopingSelectorColumns.GetSourceCount(selector);
        }

        /// <summary>
        /// Returns the index of the selected value within one band of a looping column.
        /// </summary>
        /// <param name="selector">The column to read.</param>
        /// <returns>The index within the band, or -1 when there is no selection.</returns>
        private static int LoopingColumnSourceIndex(Selector selector)
        {
            return LoopingSelectorColumns.GetSourceIndex(selector);
        }

        /// <summary>
        /// Selects a value in a looping column by its index within one band, positioning the
        /// selection in the middle band the way the pickers do. The first and last few list
        /// positions cannot be centred under the selection band, so a test must never set a raw
        /// band-relative index on a looping column.
        /// </summary>
        /// <param name="selector">The column to drive.</param>
        /// <param name="sourceIndex">The index within one band to select.</param>
        private static void SelectLoopingColumnValue(Selector selector, int sourceIndex)
        {
            Controls.LoopingItemsSource looping = Assert.IsType<Controls.LoopingItemsSource>(selector.ItemsSource);
            selector.SelectedIndex = looping.MiddleBandStart + sourceIndex;
        }

        /// <summary>
        /// Selects a value in a padded (non-looping) column by its index among the real values,
        /// skipping the leading placeholder rows.
        /// </summary>
        /// <param name="selector">The column to drive.</param>
        /// <param name="sourceIndex">The index among the real values to select.</param>
        private static void SelectPaddedColumnValue(Selector selector, int sourceIndex)
        {
            selector.SelectedIndex = LoopingPaddingItemsCount + sourceIndex;
        }

        /// <summary>
        /// Builds a list of consecutive integers rendered as invariant strings, for use as a
        /// looping column's values.
        /// </summary>
        /// <param name="count">How many values to build.</param>
        /// <returns>The values, "0" through <paramref name="count"/> minus one.</returns>
        private static List<object> BuildLoopingValues(int count)
        {
            List<object> values = [];
            for (int value = 0; value < count; value++)
            {
                values.Add(value.ToString(CultureInfo.InvariantCulture));
            }

            return values;
        }

        [Fact]
        public void LoopingItemsSource_RepeatsSourceValuesAndReportsBandMetadata()
        {
            List<object> values = BuildLoopingValues(12);
            Controls.LoopingItemsSource looping = new(values);

            Assert.Equal(12, looping.SourceCount);
            Assert.Equal(12_000, looping.Count);
            Assert.Equal(6_000, looping.MiddleBandStart);
            Assert.True(looping.IsReadOnly, "A looping item source must be read-only.");
            Assert.True(looping.IsFixedSize, "A looping item source must be fixed size.");

            // The indexer is modular, so every band shows the same values in the same order.
            Assert.Equal("0", looping[0]);
            Assert.Equal("11", looping[11]);
            Assert.Equal("0", looping[12]);
            Assert.Equal("0", looping[looping.MiddleBandStart]);
            Assert.Equal("11", looping[^1]);

            // A lookup by value lands in the middle band, not at the start of the list.
            Assert.Equal(looping.MiddleBandStart + 5, looping.IndexOf("5"));
            Assert.Equal(-1, looping.IndexOf("99"));
            Assert.True(looping.Contains("5"));
            Assert.False(looping.Contains("99"));

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => looping[-1]);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => looping[looping.Count]);
        }

        [Fact]
        public void LoopingItemsSource_Mutators_ThrowNotSupported()
        {
            IList looping = new Controls.LoopingItemsSource(BuildLoopingValues(3));

            _ = Assert.Throws<NotSupportedException>(() => looping.Add("x"));
            _ = Assert.Throws<NotSupportedException>(looping.Clear);
            _ = Assert.Throws<NotSupportedException>(() => looping.Insert(0, "x"));
            _ = Assert.Throws<NotSupportedException>(() => looping.Remove("x"));
            _ = Assert.Throws<NotSupportedException>(() => looping.RemoveAt(0));
            _ = Assert.Throws<NotSupportedException>(() => looping[0] = "x");
        }

        [Fact]
        public Task LoopingSelectorList_DefaultStyle_AppliesTemplatePartAndViewportHeightAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.LoopingSelectorList)));

                Window window = new() { Width = 300, Height = 600 };
                Controls.LoopingSelectorList list = new();

                try
                {
                    LoopingSelectorColumns.SetLoopingSource(list, BuildLoopingValues(20), 5);
                    window.Content = list;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = Assert.IsType<Controls.ListBox>(list, exactMatch: false);
                    Assert.Equal(40.0, list.ItemHeight);

                    // Nine rows: the selected row plus four above and four below it.
                    Assert.Equal(40.0 * 9, list.Height);

                    ControlTemplate template = Assert.IsType<ControlTemplate>(list.Template, exactMatch: false);
                    ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(template.FindName("PART_ScrollViewer", list));
                    Assert.True(scrollViewer.CanContentScroll,
                        "The column must scroll in item units so its offset is a row index.");

                    list.ItemHeight = 32;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(32.0 * 9, list.Height);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// A row has to be reachable by keyboard and has to show where focus landed, or the picker
        /// flyout is a dead end for anyone not using a pointer. The container style therefore keeps
        /// the ListBoxItem default <c language="csharp">IsTabStop</c> and carries the shared collection focus visual,
        /// matching ListBox.xaml; only the column itself suppresses the focus visual.
        /// </summary>
        [Fact]
        public Task LoopingSelectorList_Rows_AreKeyboardReachableWithAFocusVisualAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 300, Height = 600 };
                Controls.LoopingSelectorList list = new();

                try
                {
                    LoopingSelectorColumns.SetLoopingSource(list, BuildLoopingValues(20), 5);
                    window.Content = list;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListBoxItem row = Assert.IsType<ListBoxItem>(
                        list.ItemContainerGenerator.ContainerFromIndex(list.SelectedIndex), exactMatch: false);

                    Assert.True(row.IsTabStop, "A selector row must stay in the tab order.");
                    Assert.True(row.Focusable, "A selector row must be focusable.");
                    _ = Assert.IsType<Style>(row.FocusVisualStyle);
                    Assert.Same(app.TryFindResource("DefaultCollectionFocusVisualStyle"), row.FocusVisualStyle);
                    Assert.Null(list.FocusVisualStyle);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task LoopingSelectorList_ScrollAndSelection_StayInStepAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 300, Height = 600 };
                Controls.LoopingSelectorList list = new();

                try
                {
                    LoopingSelectorColumns.SetLoopingSource(list, BuildLoopingValues(20), 5);
                    window.Content = list;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(list.Template, exactMatch: false);
                    ScrollViewer scrollViewer = Assert.IsType<ScrollViewer>(template.FindName("PART_ScrollViewer", list));

                    int selectedIndex = list.SelectedIndex;
                    Assert.Equal(5, LoopingColumnSourceIndex(list));
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => Math.Abs(scrollViewer.VerticalOffset - (selectedIndex - LoopingPaddingItemsCount)) < 0.001).ConfigureAwait(true),
                        "Setting the selection must scroll the selected row to the middle of the viewport.");

                    // Scrolling moves the selection: the row that lands in the middle is selected.
                    scrollViewer.LineDown();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => list.SelectedIndex == selectedIndex + 1).ConfigureAwait(true),
                        "Scrolling down one row must move the selection down one value.");
                    Assert.Equal(6, LoopingColumnSourceIndex(list));

                    scrollViewer.LineUp();
                    scrollViewer.LineUp();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => list.SelectedIndex == selectedIndex - 1).ConfigureAwait(true),
                        "Scrolling up must move the selection up by the same number of values.");
                    Assert.Equal(4, LoopingColumnSourceIndex(list));

                    // Setting the selection scrolls back, so the two directions agree.
                    SelectLoopingColumnValue(list, 12);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => Math.Abs(scrollViewer.VerticalOffset - (list.SelectedIndex - LoopingPaddingItemsCount)) < 0.001).ConfigureAwait(true),
                        "Setting the selection must scroll the new selected row to the middle of the viewport.");
                    Assert.Equal(12, LoopingColumnSourceIndex(list));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task LoopingSelectorList_HomeAndEnd_DoNotMoveTheSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 300, Height = 600 };
                Controls.LoopingSelectorList list = new();

                try
                {
                    LoopingSelectorColumns.SetLoopingSource(list, BuildLoopingValues(20), 5);
                    window.Content = list;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    int selectedIndex = list.SelectedIndex;

                    // A looping column has no first or last value, so Home and End are swallowed
                    // rather than jumping the selection to an arbitrary band boundary.
                    RaiseKeyEvent(list, Key.Home, UIElement.KeyDownEvent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(selectedIndex, list.SelectedIndex);

                    RaiseKeyEvent(list, Key.End, UIElement.KeyDownEvent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(selectedIndex, list.SelectedIndex);
                    Assert.Equal(5, LoopingColumnSourceIndex(list));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task LoopingSelectorList_Virtualizes_RealizesFarFewerContainersThanItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 300, Height = 600 };
                Controls.LoopingSelectorList list = new();

                try
                {
                    LoopingSelectorColumns.SetLoopingSource(list, BuildLoopingValues(60), 30);
                    window.Content = list;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(60_000, list.Items.Count);

                    int realized = WpfTestSta.FindVisualDescendants<Controls.ListBoxItem>(list).Count();
                    Assert.True(realized > 0, "The visible rows must be realized.");
                    Assert.True(realized < 100,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "A looping column must virtualize: {0} containers realized for {1} items.",
                            realized,
                            list.Items.Count));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task LoopingSelectorList_PaddedColumn_HidesAndDisablesPlaceholderRowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 300, Height = 600 };
                Controls.LoopingSelectorList list = new();

                try
                {
                    LoopingSelectorColumns.SetPaddedSource(list, ["AM", "PM"], 0);
                    window.Content = list;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // Two values plus four padding rows at each end, so either value can be
                    // centred under the selection band.
                    Assert.Equal(2 + (LoopingPaddingItemsCount * 2), list.Items.Count);
                    Assert.Equal(LoopingPaddingItemsCount, list.SelectedIndex);
                    Assert.Equal(0, LoopingSelectorColumns.GetPaddedSourceIndex(list));

                    ListBoxItem padding = Assert.IsType<ListBoxItem>(list.ItemContainerGenerator.ContainerFromIndex(0), exactMatch: false);
                    Assert.Equal(Visibility.Hidden, padding.Visibility);
                    Assert.False(padding.IsEnabled, "A padding row must not be selectable.");

                    ListBoxItem value = Assert.IsType<ListBoxItem>(
                        list.ItemContainerGenerator.ContainerFromIndex(LoopingPaddingItemsCount), exactMatch: false);
                    Assert.Equal(Visibility.Visible, value.Visibility);
                    Assert.True(value.IsEnabled, "A real value row must stay selectable.");

                    SelectPaddedColumnValue(list, 1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(1, LoopingSelectorColumns.GetPaddedSourceIndex(list));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_HourColumn_WrapsAroundTheBandBoundaryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new()
                {
                    ClockIdentifier = "24HourClock",
                    SelectedTime = TimeSpan.Zero,
                };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(picker.Template, exactMatch: false);
                    ButtonBase flyoutButton = Assert.IsType<ButtonBase>(template.FindName("PART_FlyoutButton", picker), exactMatch: false);
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector hourList = Assert.IsType<Selector>(template.FindName("PART_HourList", picker), exactMatch: false);
                    ButtonBase acceptButton = Assert.IsType<ButtonBase>(template.FindName("PART_AcceptButton", picker), exactMatch: false);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the wrap-around scenario.");

                    Controls.LoopingItemsSource looping = Assert.IsType<Controls.LoopingItemsSource>(hourList.ItemsSource);
                    Assert.Equal(24, looping.SourceCount);
                    Assert.Equal(0, LoopingColumnSourceIndex(hourList));

                    // One row above midnight is 23:00 of the band below, not the end of a list.
                    hourList.SelectedIndex = looping.MiddleBandStart - 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(23, LoopingColumnSourceIndex(hourList));

                    RaiseButtonClick(acceptButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(new TimeSpan(23, 0, 0), picker.SelectedTime);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_DayColumn_RebuildRecentresTheClampedDayAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(picker.Template, exactMatch: false);
                    ButtonBase flyoutButton = Assert.IsType<ButtonBase>(template.FindName("PART_FlyoutButton", picker), exactMatch: false);
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector dayList = Assert.IsType<Selector>(template.FindName("PART_DayList", picker), exactMatch: false);
                    Selector monthList = Assert.IsType<Selector>(template.FindName("PART_MonthList", picker), exactMatch: false);
                    ButtonBase acceptButton = Assert.IsType<ButtonBase>(template.FindName("PART_AcceptButton", picker), exactMatch: false);

                    picker.SelectedDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Unspecified);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the day-rebuild scenario.");

                    Assert.Equal(31, LoopingColumnSourceCount(dayList));

                    SelectLoopingColumnValue(monthList, 1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // February is shorter, so the column is rebuilt, the day is clamped to the
                    // 28th, and the selection is re-centred in the fresh band rather than left
                    // at a position of the old one.
                    Assert.Equal(28, LoopingColumnSourceCount(dayList));
                    Assert.Equal(27, LoopingColumnSourceIndex(dayList));

                    Controls.LoopingItemsSource dayValues = Assert.IsType<Controls.LoopingItemsSource>(dayList.ItemsSource);
                    Assert.Equal(dayValues.MiddleBandStart + 27, dayList.SelectedIndex);

                    RaiseButtonClick(acceptButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(new DateTime(2023, 2, 28, 0, 0, 0, DateTimeKind.Unspecified), picker.SelectedDate);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_SelectedRow_AlignsWithTheHighlightBandAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new()
                {
                    ClockIdentifier = "12HourClock",
                    SelectedTime = new TimeSpan(9, 5, 0),
                };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(picker.Template, exactMatch: false);
                    ButtonBase flyoutButton = Assert.IsType<ButtonBase>(template.FindName("PART_FlyoutButton", picker), exactMatch: false);
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector hourList = Assert.IsType<Selector>(template.FindName("PART_HourList", picker), exactMatch: false);
                    Border highlight = Assert.IsType<Border>(template.FindName("HighlightRect", picker));
                    Grid selectorsGrid = Assert.IsType<Grid>(template.FindName("SelectorsGrid", picker));

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the band alignment is measured.");

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () =>
                            hourList.SelectedIndex >= 0
                            && hourList.ItemContainerGenerator.ContainerFromIndex(hourList.SelectedIndex) is FrameworkElement realized
                            && realized.ActualHeight > 0
                            && highlight.ActualHeight > 0).ConfigureAwait(true),
                        "The selected row container must be realized before it can be measured.");

                    FrameworkElement selectedRow = Assert.IsType<FrameworkElement>(
                        hourList.ItemContainerGenerator.ContainerFromIndex(hourList.SelectedIndex), exactMatch: false);

                    // The whole point of the nine-row viewport and the item-unit offset is that
                    // the selected row lands exactly on the band, so measure it rather than
                    // trusting the arithmetic.
                    Point bandTop = highlight.TranslatePoint(new Point(0, 0), selectorsGrid);
                    Point rowTop = selectedRow.TranslatePoint(new Point(0, 0), selectorsGrid);
                    Assert.Equal(bandTop.Y, rowTop.Y, 1);
                    Assert.Equal(highlight.ActualHeight, selectedRow.ActualHeight, 1);

                    // The band spans every column, so it must be at least as wide as the grid.
                    Assert.Equal(selectorsGrid.ActualWidth, highlight.ActualWidth, 1);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Pickers_HighlightBand_ResolvesBrushesAcrossAThemeCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new() { ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(picker.Template, exactMatch: false);
                    ButtonBase flyoutButton = Assert.IsType<ButtonBase>(template.FindName("PART_FlyoutButton", picker), exactMatch: false);
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the theme cycle.");

                    Border highlight = Assert.IsType<Border>(template.FindName("HighlightRect", picker));
                    Assert.False(highlight.IsHitTestVisible,
                        "The highlight band sits behind the columns and must never take input.");
                    Assert.Equal(40.0, highlight.Height);

                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // The band is accent-filled and the selected row flips to the on-accent
                    // foreground, so both tokens have to survive every theme.
                    Assert.NotNull(app.TryFindResource("AccentFillColorDefaultBrush"));
                    Assert.NotNull(app.TryFindResource("TextOnAccentFillColorPrimaryBrush"));
                    Assert.NotNull(app.TryFindResource("SubtleFillColorSecondaryBrush"));
                    Assert.NotNull(highlight.Background);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
