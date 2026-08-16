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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.DatePicker"/> control: default style
    /// and template parts, field segment rendering, flyout population, accept/cancel
    /// commit semantics, day-count adjustment, automation peer naming, and surface brush
    /// theming.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public Task DatePicker_DefaultStyle_AppliesTemplatePartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.DatePicker)));

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);

                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector dayList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_DayList", picker));
                    Selector monthList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_MonthList", picker));
                    Selector yearList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_YearList", picker));
                    ButtonBase acceptButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_AcceptButton", picker));
                    ButtonBase cancelButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_CancelButton", picker));

                    _ = Assert.IsAssignableFrom<Controls.ListBox>(dayList);
                    _ = Assert.IsAssignableFrom<Controls.Button>(flyoutButton);
                    Assert.False(popup.StaysOpen, "The selector flyout must be light-dismiss (StaysOpen=false).");
                    Assert.True(popup.AllowsTransparency, "The selector flyout must allow transparency for the rounded surface.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_SelectedDate_UpdatesFieldSegmentsAndPlaceholderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new() { PlaceholderText = "Pick a date" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);

                    TextBlock first = Assert.IsType<TextBlock>(template.FindName("FirstSegmentText", picker));
                    TextBlock second = Assert.IsType<TextBlock>(template.FindName("SecondSegmentText", picker));
                    TextBlock third = Assert.IsType<TextBlock>(template.FindName("ThirdSegmentText", picker));
                    TextBlock placeholder = Assert.IsType<TextBlock>(template.FindName("PlaceholderTextBlock", picker));
                    FrameworkElement segmentsHost = Assert.IsAssignableFrom<FrameworkElement>(template.FindName("SegmentsHost", picker));


                    Assert.Equal(Visibility.Visible, placeholder.Visibility);
                    Assert.Equal(Visibility.Collapsed, segmentsHost.Visibility);
                    Assert.Equal("Pick a date", placeholder.Text, StringComparer.Ordinal);

                    DateTime date = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = date;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
                    Assert.Equal(Visibility.Visible, segmentsHost.Visibility);

                    CultureInfo culture = CultureInfo.CurrentCulture;
                    List<string> expected =
                    [
                        date.Day.ToString(culture),
                        culture.DateTimeFormat.GetMonthName(date.Month),
                        date.Year.ToString(culture),
                    ];
                    List<string> actual = [first.Text, second.Text, third.Text];
                    Assert.Equivalent(expected, actual);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_FieldClick_OpensPopupAndPopulatesColumnsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
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

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector dayList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_DayList", picker));
                    Selector monthList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_MonthList", picker));
                    Selector yearList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_YearList", picker));

                    picker.SelectedDate = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "Clicking the field must open the selector flyout.");

                    // The open reveal (slide down from Y=-8 with a fade) must exist in the
                    // template and settle at rest once the 167ms storyboard completes.
                    TranslateTransform translate =
                        Assert.IsType<TranslateTransform>(template.FindName("FlyoutSurfaceTranslate", picker));
                    Border surface = Assert.IsType<Border>(template.FindName("FlyoutSurface", picker));
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => Math.Abs(translate.Y) < 0.001 && surface.Opacity >= 1.0).ConfigureAwait(true),
                        "The flyout reveal must settle at Y=0 and full opacity.");

                    Assert.Equal(12, monthList.Items.Count);
                    Assert.Equal(31, dayList.Items.Count);
                    Assert.Equal(picker.MaxYear - picker.MinYear + 1, yearList.Items.Count);
                    Assert.Equal(4, monthList.SelectedIndex);
                    Assert.Equal(16, dayList.SelectedIndex);
                    Assert.Equal(2024 - picker.MinYear, yearList.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_Accept_CommitsSelectionAndRaisesSelectedDateChangedAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
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

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector dayList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_DayList", picker));
                    Selector monthList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_MonthList", picker));
                    Selector yearList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_YearList", picker));
                    ButtonBase acceptButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_AcceptButton", picker));

                    DateTime oldDate = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = oldDate;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the accept scenario.");

                    DatePickerSelectedValueChangedEventArgs? captured = null;
                    picker.SelectedDateChanged += (_, args) => captured = args;

                    monthList.SelectedIndex = 0;
                    yearList.SelectedIndex = 2025 - picker.MinYear;
                    dayList.SelectedIndex = 9;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(acceptButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    DateTime expected = new(2025, 1, 10, 0, 0, 0, DateTimeKind.Unspecified);
                    Assert.Equal(expected, picker.SelectedDate);
                    Assert.NotNull(captured);
                    Assert.Equal(oldDate, captured.OldDate);
                    Assert.Equal(expected, captured.NewDate);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Accept must close the selector flyout.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_Cancel_RevertsPendingSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
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

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector dayList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_DayList", picker));
                    Selector monthList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_MonthList", picker));
                    ButtonBase cancelButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_CancelButton", picker));

                    DateTime original = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = original;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the cancel scenario.");

                    bool raised = false;
                    picker.SelectedDateChanged += (_, _) => raised = true;

                    monthList.SelectedIndex = 0;
                    dayList.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(cancelButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(original, picker.SelectedDate);
                    Assert.False(raised, "Cancel must not raise SelectedDateChanged.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Cancel must close the selector flyout.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_DayColumn_AdjustsToMonthLengthAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
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

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector dayList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_DayList", picker));
                    Selector monthList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_MonthList", picker));
                    Selector yearList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_YearList", picker));

                    picker.SelectedDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Unspecified);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the day-count scenario.");

                    Assert.Equal(31, dayList.Items.Count);
                    Assert.Equal(30, dayList.SelectedIndex);

                    monthList.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(28, dayList.Items.Count);
                    Assert.Equal(27, dayList.SelectedIndex);

                    yearList.SelectedIndex = 2024 - picker.MinYear;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(29, dayList.Items.Count);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_AutomationPeer_ReportsNameFromDateOrPlaceholderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new() { PlaceholderText = "Pick a date" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer peer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(picker));
                    _ = Assert.IsAssignableFrom<Automation.DatePickerAutomationPeer>(peer);
                    Assert.Equal("DatePicker", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                    Assert.Equal("Pick a date", peer.GetName(), StringComparer.Ordinal);

                    DateTime date = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = date;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(date.ToString("d", CultureInfo.CurrentCulture), peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_FlyoutOpen_MovesKeyboardFocusIntoPopupAndCyclesTabAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new()
                {
                    SelectedDate = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified),
                };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    UIElement popupChild = Assert.IsAssignableFrom<UIElement>(popup.Child);

                    Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(popupChild));

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "Clicking the field must open the selector flyout.");

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () =>
                            popupChild is Visual root
                            && Keyboard.FocusedElement is Visual focused
                            && focused.IsDescendantOf(root)).ConfigureAwait(true),
                        "Opening the flyout must move keyboard focus inside the popup.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_FlyoutEscape_ClosesWithoutCommittingAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
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

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    UIElement popupChild = Assert.IsAssignableFrom<UIElement>(popup.Child);
                    Selector dayList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_DayList", picker));
                    Selector monthList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_MonthList", picker));

                    DateTime original = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = original;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the Escape scenario.");

                    bool raised = false;
                    picker.SelectedDateChanged += (_, _) => raised = true;

                    monthList.SelectedIndex = 0;
                    dayList.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popupChild, Key.Escape, UIElement.PreviewKeyDownEvent);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Escape must close the selector flyout.");
                    Assert.Equal(original, picker.SelectedDate);
                    Assert.False(raised, "Escape must not raise SelectedDateChanged.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_FlyoutEnter_CommitsPendingSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
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

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    UIElement popupChild = Assert.IsAssignableFrom<UIElement>(popup.Child);
                    Selector dayList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_DayList", picker));
                    Selector monthList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_MonthList", picker));
                    Selector yearList = Assert.IsAssignableFrom<Selector>(template.FindName("PART_YearList", picker));

                    picker.SelectedDate = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the Enter scenario.");

                    monthList.SelectedIndex = 0;
                    yearList.SelectedIndex = 2025 - picker.MinYear;
                    dayList.SelectedIndex = 9;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popupChild, Key.Enter, UIElement.PreviewKeyDownEvent);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Enter must close the selector flyout.");
                    Assert.Equal(new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), picker.SelectedDate);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_SurfaceBrushes_ResolveAfterThemeCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.NotNull(app.TryFindResource("ControlFillColorDefaultBrush"));
                Assert.NotNull(app.TryFindResource("ControlElevationBorderBrush"));
                Assert.NotNull(app.TryFindResource("ControlStrokeColorDefaultBrush"));
                Assert.NotNull(app.TryFindResource("DividerStrokeColorDefaultBrush"));
                Assert.NotNull(app.TryFindResource("TextFillColorSecondaryBrush"));
                Assert.NotNull(app.TryFindResource("SolidBackgroundFillColorTertiaryBrush"));
                Assert.NotNull(app.TryFindResource("SurfaceStrokeColorFlyoutBrush"));
                Assert.NotNull(app.TryFindResource("OverlayCornerRadius"));
            });
        }

        [Fact]
        public Task DatePicker_NonGregorianDefaultCulture_UsesGregorianMonthNamesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                CultureInfo originalCulture = CultureInfo.CurrentCulture;
                Window window = new() { Width = 500, Height = 400 };

                try
                {
                    // ar-SA defaults to the Um Al Qura calendar, so unpinned month names
                    // would belong to a different calendar than the Gregorian day/year math.
                    CultureInfo culture = CultureInfo.GetCultureInfo("ar-SA");
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                    if (culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault() is not GregorianCalendar gregorian)
                    {
                        Assert.Skip("ar-SA offers no optional Gregorian calendar on this runtime.");
                        return;
                    }

                    DateTimeFormatInfo gregorianFormat = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
                    gregorianFormat.Calendar = gregorian;
                    DateTime march = new(2024, 3, 15, 0, 0, 0, DateTimeKind.Unspecified);
                    string expectedMonthName = gregorianFormat.GetMonthName(3);

                    Controls.DatePicker picker = new() { SelectedDate = march };
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    TextBlock first = Assert.IsType<TextBlock>(template.FindName("FirstSegmentText", picker));
                    TextBlock second = Assert.IsType<TextBlock>(template.FindName("SecondSegmentText", picker));
                    TextBlock third = Assert.IsType<TextBlock>(template.FindName("ThirdSegmentText", picker));

                    List<string> segments = [first.Text, second.Text, third.Text];
                    Assert.True(segments.Contains(expectedMonthName),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The month segment must show the Gregorian month name '{0}' (segments: {1}).",
                            expectedMonthName,
                            string.Join(" | ", segments)));

                    string defaultCalendarName = culture.DateTimeFormat.GetMonthName(3);
                    if (!string.Equals(defaultCalendarName, expectedMonthName, StringComparison.Ordinal))
                    {
                        Assert.False(segments.Contains(defaultCalendarName),
                            "The month segment must not show the non-Gregorian default-calendar month name.");
                    }
                }
                finally
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DatePicker_FieldClickAfterLightDismiss_DoesNotImmediatelyReopenAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
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

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    ButtonBase flyoutButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_FlyoutButton", picker));
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    ButtonBase acceptButton = Assert.IsAssignableFrom<ButtonBase>(template.FindName("PART_AcceptButton", picker));

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the light dismiss is simulated.");

                    // A light dismiss closes the popup outside the control's own pipeline,
                    // exactly like the StaysOpen=false dismissal on the field mousedown.
                    popup.SetCurrentValue(Popup.IsOpenProperty, value: false);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // The click of the same press-release gesture must not reopen the flyout.
                    RaiseButtonClick(flyoutButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(popup.IsOpen,
                        "A field click right after a light dismiss must not reopen the flyout (toggle, not flicker).");

                    // Once the lockout has elapsed, the field opens the flyout again.
                    await Task.Delay(300, TestContext.Current.CancellationToken).ConfigureAwait(true);
                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "A field click after the lockout must reopen the flyout.");

                    // Accept-driven closes do not arm the lockout: an immediate reopen works.
                    RaiseButtonClick(acceptButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Accept must close the selector flyout.");
                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "A field click right after an accept close must reopen the flyout immediately.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void RaiseButtonClick(ButtonBase button)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
    }
}
