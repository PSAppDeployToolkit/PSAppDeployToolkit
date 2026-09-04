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
using System.Reflection;
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
    /// Tests for the WinUI-style <see cref="Controls.TimePicker"/> control: default style
    /// and template parts, field segment rendering for both clock systems, flyout
    /// population (12 vs 24 hours, minute increment, AM/PM column visibility),
    /// accept/cancel commit semantics including the 12 AM / 12 PM hour mapping,
    /// property coercion, automation peer naming, and surface brush theming.
    /// </summary>
    public partial class ControlTests
    {
        /// <summary>
        /// Mirrors the control's designator fallback: the culture AM designator, or the
        /// invariant "AM" when the culture (notably several .NET Framework NLS locales)
        /// reports an empty one.
        /// </summary>
        /// <param name="culture">The culture to check for the AM designator.</param>
        /// <returns>The expected AM designator.</returns>
        private static string ExpectedAmDesignator(CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(culture.DateTimeFormat.AMDesignator) ? "AM" : culture.DateTimeFormat.AMDesignator;
        }

        /// <summary>
        /// Mirrors the control's designator fallback: the culture PM designator, or the
        /// invariant "PM" when the culture reports an empty one.
        /// </summary>
        /// <param name="culture">The culture to check for the PM designator.</param>
        /// <returns>The expected PM designator.</returns>
        private static string ExpectedPmDesignator(CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(culture.DateTimeFormat.PMDesignator) ? "PM" : culture.DateTimeFormat.PMDesignator;
        }

        [Fact]
        public Task TimePicker_DefaultStyle_AppliesTemplatePartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.TimePicker)));

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new();

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
                    Selector minuteList = Assert.IsType<Selector>(template.FindName("PART_MinuteList", picker), exactMatch: false);
                    Selector periodList = Assert.IsType<Selector>(template.FindName("PART_PeriodList", picker), exactMatch: false);
                    ButtonBase acceptButton = Assert.IsType<ButtonBase>(template.FindName("PART_AcceptButton", picker), exactMatch: false);
                    ButtonBase cancelButton = Assert.IsType<ButtonBase>(template.FindName("PART_CancelButton", picker), exactMatch: false);

                    _ = Assert.IsType<Controls.ListBox>(hourList, exactMatch: false);
                    _ = Assert.IsType<Controls.Button>(flyoutButton, exactMatch: false);
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
        public Task TimePicker_SelectedTime_UpdatesFieldSegmentsAndPlaceholderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // ClockIdentifier is pinned because this test asserts 12-hour display
                // semantics; the property default follows the machine's regional clock.
                Controls.TimePicker picker = new() { PlaceholderText = "Pick a time", ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(picker.Template, exactMatch: false);

                    TextBlock hourText = Assert.IsType<TextBlock>(template.FindName("HourSegmentText", picker));
                    TextBlock minuteText = Assert.IsType<TextBlock>(template.FindName("MinuteSegmentText", picker));
                    TextBlock periodText = Assert.IsType<TextBlock>(template.FindName("PeriodSegmentText", picker));
                    TextBlock placeholder = Assert.IsType<TextBlock>(template.FindName("PlaceholderTextBlock", picker));
                    FrameworkElement segmentsHost = Assert.IsType<FrameworkElement>(template.FindName("SegmentsHost", picker), exactMatch: false);


                    Assert.Equal(Visibility.Visible, placeholder.Visibility);
                    Assert.Equal(Visibility.Collapsed, segmentsHost.Visibility);
                    Assert.Equal("Pick a time", placeholder.Text, StringComparer.Ordinal);

                    CultureInfo culture = CultureInfo.CurrentCulture;
                    picker.SelectedTime = new TimeSpan(9, 5, 0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
                    Assert.Equal(Visibility.Visible, segmentsHost.Visibility);
                    Assert.Equal(9.ToString(culture), hourText.Text, StringComparer.Ordinal);
                    Assert.Equal(5.ToString("00", culture), minuteText.Text, StringComparer.Ordinal);
                    Assert.Equal(ExpectedAmDesignator(culture), periodText.Text, StringComparer.Ordinal);

                    picker.SelectedTime = TimeSpan.Zero;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(12.ToString(culture), hourText.Text, StringComparer.Ordinal);
                    Assert.Equal(0.ToString("00", culture), minuteText.Text, StringComparer.Ordinal);
                    Assert.Equal(ExpectedAmDesignator(culture), periodText.Text, StringComparer.Ordinal);

                    picker.SelectedTime = new TimeSpan(12, 30, 0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(12.ToString(culture), hourText.Text, StringComparer.Ordinal);
                    Assert.Equal(30.ToString("00", culture), minuteText.Text, StringComparer.Ordinal);
                    Assert.Equal(ExpectedPmDesignator(culture), periodText.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_FieldClick_OpensPopupAndPopulatesColumnsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // ClockIdentifier is pinned because this test asserts the 12-hour column
                // layout; the property default follows the machine's regional clock.
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
                    Selector hourList = Assert.IsType<Selector>(template.FindName("PART_HourList", picker), exactMatch: false);
                    Selector minuteList = Assert.IsType<Selector>(template.FindName("PART_MinuteList", picker), exactMatch: false);
                    Selector periodList = Assert.IsType<Selector>(template.FindName("PART_PeriodList", picker), exactMatch: false);

                    picker.SelectedTime = new TimeSpan(14, 30, 0);
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

                    // The hour and minute columns loop, so their item count is a thousand
                    // repeats of the band and only the band length and the modular selection
                    // index are meaningful. The AM/PM column is padded instead of looped.
                    CultureInfo culture = CultureInfo.CurrentCulture;
                    Assert.Equal(12, LoopingColumnSourceCount(hourList));
                    Assert.Equal(1.ToString(culture), hourList.Items[0]);
                    Assert.Equal(60, LoopingColumnSourceCount(minuteList));
                    Assert.Equal(0.ToString("00", culture), minuteList.Items[0]);
                    Assert.Equal(2 + (LoopingPaddingItemsCount * 2), periodList.Items.Count);
                    Assert.Equal(ExpectedAmDesignator(culture), periodList.Items[LoopingPaddingItemsCount]);
                    Assert.Equal(ExpectedPmDesignator(culture), periodList.Items[LoopingPaddingItemsCount + 1]);
                    Assert.Equal(Visibility.Visible, periodList.Visibility);
                    Assert.Equal(1, LoopingColumnSourceIndex(hourList));
                    Assert.Equal(30, LoopingColumnSourceIndex(minuteList));
                    Assert.Equal(LoopingPaddingItemsCount + 1, periodList.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_TwentyFourHourClock_PopulatesHoursAndHidesPeriodColumnAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new()
                {
                    ClockIdentifier = "24HourClock",
                    MinuteIncrement = 15,
                    SelectedTime = new TimeSpan(14, 40, 0),
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
                    Selector minuteList = Assert.IsType<Selector>(template.FindName("PART_MinuteList", picker), exactMatch: false);
                    Selector periodList = Assert.IsType<Selector>(template.FindName("PART_PeriodList", picker), exactMatch: false);
                    TextBlock hourText = Assert.IsType<TextBlock>(template.FindName("HourSegmentText", picker));
                    TextBlock periodText = Assert.IsType<TextBlock>(template.FindName("PeriodSegmentText", picker));
                    FrameworkElement secondDivider = Assert.IsType<FrameworkElement>(template.FindName("SecondDivider", picker), exactMatch: false);

                    CultureInfo culture = CultureInfo.CurrentCulture;
                    Assert.Equal(14.ToString(culture), hourText.Text, StringComparer.Ordinal);
                    Assert.Equal(string.Empty, periodText.Text, StringComparer.Ordinal);
                    Assert.Equal(Visibility.Collapsed, periodText.Visibility);
                    Assert.Equal(Visibility.Collapsed, secondDivider.Visibility);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "Clicking the field must open the selector flyout.");

                    Assert.Equal(24, LoopingColumnSourceCount(hourList));
                    Assert.Equal(0.ToString(culture), hourList.Items[0]);
                    Assert.Equal(4, LoopingColumnSourceCount(minuteList));
                    Assert.Equal(14, LoopingColumnSourceIndex(hourList));
                    Assert.Equal(2, LoopingColumnSourceIndex(minuteList));
                    Assert.Equal(Visibility.Collapsed, periodList.Visibility);
                    Assert.Empty(periodList.Items);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_ClockIdentifierAndMinuteIncrement_CoerceInvalidValuesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TimePicker picker = new();

                // The default is regional: 24-hour when the culture short time pattern uses
                // the 'H' specifier, otherwise 12-hour. Compute the expectation from the same
                // rule so the assertion holds on any machine culture.
                string expectedDefaultClock = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H", StringComparison.Ordinal)
                    ? "24HourClock"
                    : "12HourClock";
                Assert.Equal(expectedDefaultClock, picker.ClockIdentifier, StringComparer.Ordinal);
                Assert.Equal(1, picker.MinuteIncrement);

                picker.ClockIdentifier = "24HourClock";
                Assert.Equal("24HourClock", picker.ClockIdentifier, StringComparer.Ordinal);

                picker.ClockIdentifier = "13HourClock";
                Assert.Equal("12HourClock", picker.ClockIdentifier, StringComparer.Ordinal);

                picker.MinuteIncrement = 0;
                Assert.Equal(1, picker.MinuteIncrement);

                picker.MinuteIncrement = 120;
                Assert.Equal(59, picker.MinuteIncrement);
            });
        }

        [Fact]
        public Task TimePicker_Accept_CommitsSelectionAndRaisesSelectedTimeChangedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // ClockIdentifier is pinned because this test asserts the 12-hour AM/PM
                // commit mapping; the property default follows the machine's regional clock.
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
                    Selector hourList = Assert.IsType<Selector>(template.FindName("PART_HourList", picker), exactMatch: false);
                    Selector minuteList = Assert.IsType<Selector>(template.FindName("PART_MinuteList", picker), exactMatch: false);
                    Selector periodList = Assert.IsType<Selector>(template.FindName("PART_PeriodList", picker), exactMatch: false);
                    ButtonBase acceptButton = Assert.IsType<ButtonBase>(template.FindName("PART_AcceptButton", picker), exactMatch: false);

                    TimeSpan oldTime = new(9, 5, 0);
                    picker.SelectedTime = oldTime;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the accept scenario.");

                    TimePickerSelectedValueChangedEventArgs? captured = null;
                    picker.SelectedTimeChanged += (_, args) => captured = args;

                    SelectLoopingColumnValue(hourList, 11);
                    SelectLoopingColumnValue(minuteList, 30);
                    SelectPaddedColumnValue(periodList, 1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(acceptButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TimeSpan noon = new(12, 30, 0);
                    Assert.Equal(noon, picker.SelectedTime);
                    Assert.NotNull(captured);
                    Assert.Equal(oldTime, captured.OldTime);
                    Assert.Equal(noon, captured.NewTime);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Accept must close the selector flyout.");

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must reopen for the midnight scenario.");

                    Assert.Equal(11, LoopingColumnSourceIndex(hourList));
                    SelectPaddedColumnValue(periodList, 0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(acceptButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TimeSpan midnight = new(0, 30, 0);
                    Assert.Equal(midnight, picker.SelectedTime);
                    Assert.NotNull(captured);
                    Assert.Equal(noon, captured.OldTime);
                    Assert.Equal(midnight, captured.NewTime);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_Cancel_RevertsPendingSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new();

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
                    Selector minuteList = Assert.IsType<Selector>(template.FindName("PART_MinuteList", picker), exactMatch: false);
                    ButtonBase cancelButton = Assert.IsType<ButtonBase>(template.FindName("PART_CancelButton", picker), exactMatch: false);

                    TimeSpan original = new(9, 5, 0);
                    picker.SelectedTime = original;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the cancel scenario.");

                    bool raised = false;
                    picker.SelectedTimeChanged += (_, _) => raised = true;

                    SelectLoopingColumnValue(hourList, 3);
                    SelectLoopingColumnValue(minuteList, 45);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(cancelButton);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(original, picker.SelectedTime);
                    Assert.False(raised, "Cancel must not raise SelectedTimeChanged.");
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
        public Task TimePicker_AutomationPeer_ReportsNameFromTimeOrPlaceholderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new() { PlaceholderText = "Pick a time" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer peer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(picker), exactMatch: false);
                    _ = Assert.IsType<Automation.TimePickerAutomationPeer>(peer, exactMatch: false);
                    Assert.Equal("TimePicker", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                    Assert.Equal("Pick a time", peer.GetName(), StringComparer.Ordinal);

                    TimeSpan time = new(14, 30, 0);
                    picker.SelectedTime = time;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(DateTime.Today.Add(time).ToString("t", CultureInfo.CurrentCulture), peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_BlankCultureDesignators_FallBackToInvariantAmPmAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                // Simulate the .NET Framework NLS locales (de-DE, fr-FR, sv-SE, it-IT) that
                // report empty AM/PM designators; restore the thread culture in finally.
                CultureInfo originalCulture = CultureInfo.CurrentCulture;
                CultureInfo blankDesignatorCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                blankDesignatorCulture.DateTimeFormat.AMDesignator = string.Empty;
                blankDesignatorCulture.DateTimeFormat.PMDesignator = string.Empty;

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new() { ClockIdentifier = "12HourClock" };

                try
                {
                    CultureInfo.CurrentCulture = blankDesignatorCulture;

                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(picker.Template, exactMatch: false);
                    ButtonBase flyoutButton = Assert.IsType<ButtonBase>(template.FindName("PART_FlyoutButton", picker), exactMatch: false);
                    Popup popup = Assert.IsType<Popup>(template.FindName("PART_Popup", picker));
                    Selector periodList = Assert.IsType<Selector>(template.FindName("PART_PeriodList", picker), exactMatch: false);
                    TextBlock periodText = Assert.IsType<TextBlock>(template.FindName("PeriodSegmentText", picker));

                    picker.SelectedTime = new TimeSpan(14, 30, 0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("PM", periodText.Text, StringComparer.Ordinal);

                    picker.SelectedTime = new TimeSpan(9, 5, 0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("AM", periodText.Text, StringComparer.Ordinal);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "Clicking the field must open the selector flyout.");

                    Assert.Equal(2 + (LoopingPaddingItemsCount * 2), periodList.Items.Count);
                    Assert.Equal("AM", periodList.Items[LoopingPaddingItemsCount]);
                    Assert.Equal("PM", periodList.Items[LoopingPaddingItemsCount + 1]);
                }
                finally
                {
                    CultureInfo.CurrentCulture = originalCulture;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_FlyoutOpen_MovesKeyboardFocusIntoPopupAndCyclesTabAsync()
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
                    UIElement popupChild = Assert.IsType<UIElement>(popup.Child, exactMatch: false);

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
        public Task TimePicker_FlyoutEscape_ClosesWithoutCommittingAsync()
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
                    Selector hourList = Assert.IsType<Selector>(template.FindName("PART_HourList", picker), exactMatch: false);
                    Selector minuteList = Assert.IsType<Selector>(template.FindName("PART_MinuteList", picker), exactMatch: false);

                    TimeSpan original = new(9, 5, 0);
                    picker.SelectedTime = original;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the Escape scenario.");
                    UIElement popupChild = Assert.IsType<UIElement>(popup.Child, exactMatch: false);

                    bool raised = false;
                    picker.SelectedTimeChanged += (_, _) => raised = true;

                    SelectLoopingColumnValue(hourList, 3);
                    SelectLoopingColumnValue(minuteList, 45);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popupChild, Key.Escape, UIElement.PreviewKeyDownEvent);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Escape must close the selector flyout.");
                    Assert.Equal(original, picker.SelectedTime);
                    Assert.False(raised, "Escape must not raise SelectedTimeChanged.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_FlyoutEnter_CommitsPendingSelectionAsync()
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
                    Selector hourList = Assert.IsType<Selector>(template.FindName("PART_HourList", picker), exactMatch: false);
                    Selector minuteList = Assert.IsType<Selector>(template.FindName("PART_MinuteList", picker), exactMatch: false);
                    Selector periodList = Assert.IsType<Selector>(template.FindName("PART_PeriodList", picker), exactMatch: false);

                    picker.SelectedTime = new TimeSpan(9, 5, 0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => popup.IsOpen).ConfigureAwait(true),
                        "The selector flyout must open before the Enter scenario.");
                    UIElement popupChild = Assert.IsType<UIElement>(popup.Child, exactMatch: false);

                    SelectLoopingColumnValue(hourList, 11);
                    SelectLoopingColumnValue(minuteList, 30);
                    SelectPaddedColumnValue(periodList, 1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popupChild, Key.Enter, UIElement.PreviewKeyDownEvent);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !popup.IsOpen).ConfigureAwait(true),
                        "Enter must close the selector flyout.");
                    Assert.Equal(new TimeSpan(12, 30, 0), picker.SelectedTime);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_OutOfRangeSelectedTime_NormalizesFieldTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // 24-hour clock pinned so the normalized hour value is asserted directly.
                Controls.TimePicker picker = new() { ClockIdentifier = "24HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsType<ControlTemplate>(picker.Template, exactMatch: false);
                    TextBlock hourText = Assert.IsType<TextBlock>(template.FindName("HourSegmentText", picker));
                    TextBlock minuteText = Assert.IsType<TextBlock>(template.FindName("MinuteSegmentText", picker));

                    CultureInfo culture = CultureInfo.CurrentCulture;

                    // A negative span wraps to the previous-day hour like the flyout columns.
                    picker.SelectedTime = TimeSpan.FromHours(-1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(23.ToString(culture), hourText.Text, StringComparer.Ordinal);

                    // A span past a day wraps into the day like the flyout columns.
                    picker.SelectedTime = TimeSpan.FromHours(25);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(1.ToString(culture), hourText.Text, StringComparer.Ordinal);

                    // Negative minutes normalize into 0..59.
                    picker.SelectedTime = new TimeSpan(0, -30, 0);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(30.ToString("00", culture), minuteText.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_FieldClickAfterLightDismiss_DoesNotImmediatelyReopenAsync()
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
                        "The selector flyout must open before the light dismiss is simulated.");

                    // A light dismiss closes the popup outside the control's own pipeline,
                    // exactly like the StaysOpen=false dismissal on the field mousedown.
                    popup.SetCurrentValue(Popup.IsOpenProperty, value: false);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // The lockout is a 250 ms Environment.TickCount window, and the dispatcher
                    // drain above can outlast it on a loaded CI runner, so first prove the dismiss
                    // armed it, then re-arm it so the second click is deterministically inside it.
                    FieldInfo dismissTickField = Assert.IsType<FieldInfo>(
                        typeof(Controls.TimePicker).GetField("_lastLightDismissTick", BindingFlags.NonPublic | BindingFlags.Instance), exactMatch: false);
                    Assert.NotNull(dismissTickField.GetValue(picker));
                    dismissTickField.SetValue(picker, Environment.TickCount);

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
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TimePicker_SurfaceBrushes_ResolveAfterThemeCycleAsync()
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
    }
}
