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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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

        [TestMethod]
        public void TimePicker_DefaultStyle_AppliesTemplateParts()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.TimePicker)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.TimePicker.");

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");

                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? hourList = template.FindName("PART_HourList", picker) as Selector;
                    Selector? minuteList = template.FindName("PART_MinuteList", picker) as Selector;
                    Selector? periodList = template.FindName("PART_PeriodList", picker) as Selector;
                    ButtonBase? acceptButton = template.FindName("PART_AcceptButton", picker) as ButtonBase;
                    ButtonBase? cancelButton = template.FindName("PART_CancelButton", picker) as ButtonBase;

                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be a ButtonBase so the field reads as a button.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(hourList, "PART_HourList must be a Selector hosting the hour column.");
                    Assert.IsNotNull(minuteList, "PART_MinuteList must be a Selector hosting the minute column.");
                    Assert.IsNotNull(periodList, "PART_PeriodList must be a Selector hosting the AM/PM column.");
                    Assert.IsNotNull(acceptButton, "PART_AcceptButton must be present in the template.");
                    Assert.IsNotNull(cancelButton, "PART_CancelButton must be present in the template.");
                    _ = Assert.IsInstanceOfType<Controls.ListBox>(hourList,
                        "The default template should present the hour column through the Fluence ListBox.");
                    _ = Assert.IsInstanceOfType<Controls.Button>(flyoutButton,
                        "The default template should render the field through the Fluence Button.");
                    Assert.IsFalse(popup.StaysOpen, "The selector flyout must be light-dismiss (StaysOpen=false).");
                    Assert.IsTrue(popup.AllowsTransparency, "The selector flyout must allow transparency for the rounded surface.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_SelectedTime_UpdatesFieldSegmentsAndPlaceholder()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // ClockIdentifier is pinned because this test asserts 12-hour display
                // semantics; the property default follows the machine's regional clock.
                Controls.TimePicker picker = new() { PlaceholderText = "Pick a time", ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");

                    TextBlock? hourText = template.FindName("HourSegmentText", picker) as TextBlock;
                    TextBlock? minuteText = template.FindName("MinuteSegmentText", picker) as TextBlock;
                    TextBlock? periodText = template.FindName("PeriodSegmentText", picker) as TextBlock;
                    TextBlock? placeholder = template.FindName("PlaceholderTextBlock", picker) as TextBlock;
                    FrameworkElement? segmentsHost = template.FindName("SegmentsHost", picker) as FrameworkElement;

                    Assert.IsNotNull(hourText, "HourSegmentText must be present in the default template.");
                    Assert.IsNotNull(minuteText, "MinuteSegmentText must be present in the default template.");
                    Assert.IsNotNull(periodText, "PeriodSegmentText must be present in the default template.");
                    Assert.IsNotNull(placeholder, "PlaceholderTextBlock must be present in the default template.");
                    Assert.IsNotNull(segmentsHost, "SegmentsHost must be present in the default template.");

                    Assert.AreEqual(Visibility.Visible, placeholder.Visibility,
                        "The placeholder must be visible while SelectedTime is null.");
                    Assert.AreEqual(Visibility.Collapsed, segmentsHost.Visibility,
                        "The segment row must be collapsed while SelectedTime is null.");
                    Assert.AreEqual("Pick a time", placeholder.Text, "PlaceholderText must flow into the placeholder text block.");

                    CultureInfo culture = CultureInfo.CurrentCulture;
                    picker.SelectedTime = new TimeSpan(9, 5, 0);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility,
                        "The placeholder must collapse once a time is selected.");
                    Assert.AreEqual(Visibility.Visible, segmentsHost.Visibility,
                        "The segment row must show once a time is selected.");
                    Assert.AreEqual(9.ToString(culture), hourText.Text, "The hour segment must show the unpadded 12-hour value.");
                    Assert.AreEqual(5.ToString("00", culture), minuteText.Text, "The minute segment must always be two-digit.");
                    Assert.AreEqual(ExpectedAmDesignator(culture), periodText.Text,
                        "A morning time must show the culture AM designator (with the invariant fallback).");

                    picker.SelectedTime = TimeSpan.Zero;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(12.ToString(culture), hourText.Text, "Midnight must display as hour 12 on the 12-hour clock.");
                    Assert.AreEqual(0.ToString("00", culture), minuteText.Text, "Midnight must display a two-digit zero minute.");
                    Assert.AreEqual(ExpectedAmDesignator(culture), periodText.Text, "Midnight must show the AM designator.");

                    picker.SelectedTime = new TimeSpan(12, 30, 0);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(12.ToString(culture), hourText.Text, "Noon must display as hour 12 on the 12-hour clock.");
                    Assert.AreEqual(30.ToString("00", culture), minuteText.Text, "The minute segment must show the selected minute.");
                    Assert.AreEqual(ExpectedPmDesignator(culture), periodText.Text, "Noon must show the PM designator.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_FieldClick_OpensPopupAndPopulatesColumns()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // ClockIdentifier is pinned because this test asserts the 12-hour column
                // layout; the property default follows the machine's regional clock.
                Controls.TimePicker picker = new() { ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? hourList = template.FindName("PART_HourList", picker) as Selector;
                    Selector? minuteList = template.FindName("PART_MinuteList", picker) as Selector;
                    Selector? periodList = template.FindName("PART_PeriodList", picker) as Selector;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(hourList, "PART_HourList must be present in the template.");
                    Assert.IsNotNull(minuteList, "PART_MinuteList must be present in the template.");
                    Assert.IsNotNull(periodList, "PART_PeriodList must be present in the template.");

                    picker.SelectedTime = new TimeSpan(14, 30, 0);
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "Clicking the field must open the selector flyout.");

                    // The open reveal (slide down from Y=-8 with a fade) must exist in the
                    // template and settle at rest once the 167ms storyboard completes.
                    TranslateTransform? translate =
                        template.FindName("FlyoutSurfaceTranslate", picker) as TranslateTransform;
                    Assert.IsNotNull(translate, "The TimePicker template must expose the FlyoutSurfaceTranslate reveal transform.");
                    Border? surface = template.FindName("FlyoutSurface", picker) as Border;
                    Assert.IsNotNull(surface, "The TimePicker template must expose the FlyoutSurface element.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => Math.Abs(translate.Y) < 0.001 && surface.Opacity >= 1.0),
                        "The flyout reveal must settle at Y=0 and full opacity.");

                    CultureInfo culture = CultureInfo.CurrentCulture;
                    Assert.AreEqual(12, hourList.Items.Count, "The 12-hour clock must offer the hours 1..12.");
                    Assert.AreEqual(1.ToString(culture), hourList.Items[0], "The 12-hour column must start at hour 1.");
                    Assert.AreEqual(60, minuteList.Items.Count, "MinuteIncrement 1 must offer all sixty minutes.");
                    Assert.AreEqual(0.ToString("00", culture), minuteList.Items[0], "The minute column must start at the two-digit 00.");
                    Assert.AreEqual(2, periodList.Items.Count, "The AM/PM column must offer exactly the two designators.");
                    Assert.AreEqual(ExpectedAmDesignator(culture), periodList.Items[0],
                        "The AM/PM column must lead with the culture AM designator (with the invariant fallback).");
                    Assert.AreEqual(Visibility.Visible, periodList.Visibility,
                        "The AM/PM column must be visible on the 12-hour clock.");
                    Assert.AreEqual(1, hourList.SelectedIndex, "14:30 must preselect display hour 2.");
                    Assert.AreEqual(30, minuteList.SelectedIndex, "14:30 must preselect minute 30.");
                    Assert.AreEqual(1, periodList.SelectedIndex, "14:30 must preselect the PM designator.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_TwentyFourHourClock_PopulatesHoursAndHidesPeriodColumn()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? hourList = template.FindName("PART_HourList", picker) as Selector;
                    Selector? minuteList = template.FindName("PART_MinuteList", picker) as Selector;
                    Selector? periodList = template.FindName("PART_PeriodList", picker) as Selector;
                    TextBlock? hourText = template.FindName("HourSegmentText", picker) as TextBlock;
                    TextBlock? periodText = template.FindName("PeriodSegmentText", picker) as TextBlock;
                    FrameworkElement? secondDivider = template.FindName("SecondDivider", picker) as FrameworkElement;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(hourList, "PART_HourList must be present in the template.");
                    Assert.IsNotNull(minuteList, "PART_MinuteList must be present in the template.");
                    Assert.IsNotNull(periodList, "PART_PeriodList must be present in the template.");
                    Assert.IsNotNull(hourText, "HourSegmentText must be present in the default template.");
                    Assert.IsNotNull(periodText, "PeriodSegmentText must be present in the default template.");
                    Assert.IsNotNull(secondDivider, "SecondDivider must be present in the default template.");

                    CultureInfo culture = CultureInfo.CurrentCulture;
                    Assert.AreEqual(14.ToString(culture), hourText.Text,
                        "The 24-hour clock must show the hour of day without AM/PM conversion.");
                    Assert.AreEqual(string.Empty, periodText.Text, "The 24-hour clock must not render a designator.");
                    Assert.AreEqual(Visibility.Collapsed, periodText.Visibility,
                        "The designator segment must collapse on the 24-hour clock.");
                    Assert.AreEqual(Visibility.Collapsed, secondDivider.Visibility,
                        "The designator divider must collapse on the 24-hour clock.");

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "Clicking the field must open the selector flyout.");

                    Assert.AreEqual(24, hourList.Items.Count, "The 24-hour clock must offer the hours 0..23.");
                    Assert.AreEqual(0.ToString(culture), hourList.Items[0], "The 24-hour column must start at hour 0.");
                    Assert.AreEqual(4, minuteList.Items.Count, "MinuteIncrement 15 must offer 00, 15, 30, and 45.");
                    Assert.AreEqual(14, hourList.SelectedIndex, "14:40 must preselect hour 14 on the 24-hour clock.");
                    Assert.AreEqual(2, minuteList.SelectedIndex,
                        "Minute 40 must snap down to the 30 step when MinuteIncrement is 15.");
                    Assert.AreEqual(Visibility.Collapsed, periodList.Visibility,
                        "The AM/PM column must collapse on the 24-hour clock.");
                    Assert.AreEqual(0, periodList.Items.Count, "The AM/PM column must stay empty on the 24-hour clock.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_ClockIdentifierAndMinuteIncrement_CoerceInvalidValues()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TimePicker picker = new();

                // The default is regional: 24-hour when the culture short time pattern uses
                // the 'H' specifier, otherwise 12-hour. Compute the expectation from the same
                // rule so the assertion holds on any machine culture.
                string expectedDefaultClock = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H", StringComparison.Ordinal)
                    ? "24HourClock"
                    : "12HourClock";
                Assert.AreEqual(expectedDefaultClock, picker.ClockIdentifier,
                    "The clock identifier must default to the regional clock system derived from the culture short time pattern.");
                Assert.AreEqual(1, picker.MinuteIncrement, "The minute increment must default to 1.");

                picker.ClockIdentifier = "24HourClock";
                Assert.AreEqual("24HourClock", picker.ClockIdentifier, "The 24-hour clock identifier must be accepted as-is.");

                picker.ClockIdentifier = "13HourClock";
                Assert.AreEqual("12HourClock", picker.ClockIdentifier,
                    "Unknown clock identifiers must coerce back to the 12-hour clock.");

                picker.MinuteIncrement = 0;
                Assert.AreEqual(1, picker.MinuteIncrement, "A MinuteIncrement below 1 must clamp to 1.");

                picker.MinuteIncrement = 120;
                Assert.AreEqual(59, picker.MinuteIncrement, "A MinuteIncrement above 59 must clamp to 59.");
            });
        }

        [TestMethod]
        public void TimePicker_Accept_CommitsSelectionAndRaisesSelectedTimeChanged()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // ClockIdentifier is pinned because this test asserts the 12-hour AM/PM
                // commit mapping; the property default follows the machine's regional clock.
                Controls.TimePicker picker = new() { ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? hourList = template.FindName("PART_HourList", picker) as Selector;
                    Selector? minuteList = template.FindName("PART_MinuteList", picker) as Selector;
                    Selector? periodList = template.FindName("PART_PeriodList", picker) as Selector;
                    ButtonBase? acceptButton = template.FindName("PART_AcceptButton", picker) as ButtonBase;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(hourList, "PART_HourList must be present in the template.");
                    Assert.IsNotNull(minuteList, "PART_MinuteList must be present in the template.");
                    Assert.IsNotNull(periodList, "PART_PeriodList must be present in the template.");
                    Assert.IsNotNull(acceptButton, "PART_AcceptButton must be present in the template.");

                    TimeSpan oldTime = new(9, 5, 0);
                    picker.SelectedTime = oldTime;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the accept scenario.");

                    TimePickerSelectedValueChangedEventArgs? captured = null;
                    picker.SelectedTimeChanged += (_, args) => captured = args;

                    hourList.SelectedIndex = 11;
                    minuteList.SelectedIndex = 30;
                    periodList.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(acceptButton);
                    DrainDispatcher(window.Dispatcher);

                    TimeSpan noon = new(12, 30, 0);
                    Assert.AreEqual(noon, picker.SelectedTime,
                        "Accept must map display hour 12 with PM to hour 12 (noon).");
                    Assert.IsNotNull(captured, "Accept must raise SelectedTimeChanged.");
                    Assert.AreEqual(oldTime, captured.OldTime, "OldTime must carry the previously selected time.");
                    Assert.AreEqual(noon, captured.NewTime, "NewTime must carry the committed time.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Accept must close the selector flyout.");

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must reopen for the midnight scenario.");

                    Assert.AreEqual(11, hourList.SelectedIndex, "Reopening at noon must preselect display hour 12.");
                    periodList.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(acceptButton);
                    DrainDispatcher(window.Dispatcher);

                    TimeSpan midnight = new(0, 30, 0);
                    Assert.AreEqual(midnight, picker.SelectedTime,
                        "Accept must map display hour 12 with AM to hour 0 (midnight).");
                    Assert.IsNotNull(captured, "The midnight accept must raise SelectedTimeChanged.");
                    Assert.AreEqual(noon, captured.OldTime, "OldTime must carry the noon value from the first commit.");
                    Assert.AreEqual(midnight, captured.NewTime, "NewTime must carry the committed midnight value.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_Cancel_RevertsPendingSelection()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? hourList = template.FindName("PART_HourList", picker) as Selector;
                    Selector? minuteList = template.FindName("PART_MinuteList", picker) as Selector;
                    ButtonBase? cancelButton = template.FindName("PART_CancelButton", picker) as ButtonBase;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(hourList, "PART_HourList must be present in the template.");
                    Assert.IsNotNull(minuteList, "PART_MinuteList must be present in the template.");
                    Assert.IsNotNull(cancelButton, "PART_CancelButton must be present in the template.");

                    TimeSpan original = new(9, 5, 0);
                    picker.SelectedTime = original;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the cancel scenario.");

                    bool raised = false;
                    picker.SelectedTimeChanged += (_, _) => raised = true;

                    hourList.SelectedIndex = 3;
                    minuteList.SelectedIndex = 45;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(cancelButton);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(original, picker.SelectedTime, "Cancel must leave SelectedTime unchanged.");
                    Assert.IsFalse(raised, "Cancel must not raise SelectedTimeChanged.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Cancel must close the selector flyout.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_AutomationPeer_ReportsNameFromTimeOrPlaceholder()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new() { PlaceholderText = "Pick a time" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(picker);
                    Assert.IsNotNull(peer, "TimePicker must create an automation peer.");
                    _ = Assert.IsInstanceOfType<Automation.TimePickerAutomationPeer>(peer,
                        "TimePicker must expose the TimePickerAutomationPeer.");
                    Assert.AreEqual("TimePicker", peer.GetClassName(), "The peer must report the TimePicker class name.");
                    Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType(),
                        "The peer must report the Group control type.");
                    Assert.AreEqual("Pick a time", peer.GetName(),
                        "The peer name must fall back to PlaceholderText while no time is selected.");

                    TimeSpan time = new(14, 30, 0);
                    picker.SelectedTime = time;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(DateTime.Today.Add(time).ToString("t", CultureInfo.CurrentCulture), peer.GetName(),
                        "The peer name must report the selected time in the culture short time format.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_BlankCultureDesignators_FallBackToInvariantAmPm()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? periodList = template.FindName("PART_PeriodList", picker) as Selector;
                    TextBlock? periodText = template.FindName("PeriodSegmentText", picker) as TextBlock;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(periodList, "PART_PeriodList must be present in the template.");
                    Assert.IsNotNull(periodText, "PeriodSegmentText must be present in the default template.");

                    picker.SelectedTime = new TimeSpan(14, 30, 0);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual("PM", periodText.Text,
                        "An afternoon time must fall back to the invariant PM designator when the culture designator is blank.");

                    picker.SelectedTime = new TimeSpan(9, 5, 0);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual("AM", periodText.Text,
                        "A morning time must fall back to the invariant AM designator when the culture designator is blank.");

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "Clicking the field must open the selector flyout.");

                    Assert.AreEqual(2, periodList.Items.Count, "The AM/PM column must still offer two period values.");
                    Assert.AreEqual("AM", periodList.Items[0],
                        "The AM/PM column must fall back to the invariant AM item when the culture designator is blank.");
                    Assert.AreEqual("PM", periodList.Items[1],
                        "The AM/PM column must fall back to the invariant PM item when the culture designator is blank.");
                }
                finally
                {
                    CultureInfo.CurrentCulture = originalCulture;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_FlyoutOpen_MovesKeyboardFocusIntoPopupAndCyclesTab()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(popup.Child, "The selector flyout must have a popup child root.");

                    Assert.AreEqual(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(popup.Child),
                        "Tab navigation must cycle inside the flyout popup root.");

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "Clicking the field must open the selector flyout.");

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () =>
                            popup.Child is Visual root
                            && Keyboard.FocusedElement is Visual focused
                            && focused.IsDescendantOf(root)),
                        "Opening the flyout must move keyboard focus inside the popup.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_FlyoutEscape_ClosesWithoutCommitting()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new() { ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? hourList = template.FindName("PART_HourList", picker) as Selector;
                    Selector? minuteList = template.FindName("PART_MinuteList", picker) as Selector;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(hourList, "PART_HourList must be present in the template.");
                    Assert.IsNotNull(minuteList, "PART_MinuteList must be present in the template.");

                    TimeSpan original = new(9, 5, 0);
                    picker.SelectedTime = original;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the Escape scenario.");
                    Assert.IsNotNull(popup.Child, "The selector flyout must have a popup child root.");

                    bool raised = false;
                    picker.SelectedTimeChanged += (_, _) => raised = true;

                    hourList.SelectedIndex = 3;
                    minuteList.SelectedIndex = 45;
                    DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popup.Child, Key.Escape, UIElement.PreviewKeyDownEvent);

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Escape must close the selector flyout.");
                    Assert.AreEqual(original, picker.SelectedTime, "Escape must not commit the pending column selection.");
                    Assert.IsFalse(raised, "Escape must not raise SelectedTimeChanged.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_FlyoutEnter_CommitsPendingSelection()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new() { ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? hourList = template.FindName("PART_HourList", picker) as Selector;
                    Selector? minuteList = template.FindName("PART_MinuteList", picker) as Selector;
                    Selector? periodList = template.FindName("PART_PeriodList", picker) as Selector;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(hourList, "PART_HourList must be present in the template.");
                    Assert.IsNotNull(minuteList, "PART_MinuteList must be present in the template.");
                    Assert.IsNotNull(periodList, "PART_PeriodList must be present in the template.");

                    picker.SelectedTime = new TimeSpan(9, 5, 0);
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the Enter scenario.");
                    Assert.IsNotNull(popup.Child, "The selector flyout must have a popup child root.");

                    hourList.SelectedIndex = 11;
                    minuteList.SelectedIndex = 30;
                    periodList.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popup.Child, Key.Enter, UIElement.PreviewKeyDownEvent);

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Enter must close the selector flyout.");
                    Assert.AreEqual(new TimeSpan(12, 30, 0), picker.SelectedTime,
                        "Enter must commit the pending column selection like the accept button.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_OutOfRangeSelectedTime_NormalizesFieldText()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // 24-hour clock pinned so the normalized hour value is asserted directly.
                Controls.TimePicker picker = new() { ClockIdentifier = "24HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    TextBlock? hourText = template.FindName("HourSegmentText", picker) as TextBlock;
                    TextBlock? minuteText = template.FindName("MinuteSegmentText", picker) as TextBlock;
                    Assert.IsNotNull(hourText, "HourSegmentText must be present in the default template.");
                    Assert.IsNotNull(minuteText, "MinuteSegmentText must be present in the default template.");

                    CultureInfo culture = CultureInfo.CurrentCulture;

                    // A negative span wraps to the previous-day hour like the flyout columns.
                    picker.SelectedTime = TimeSpan.FromHours(-1);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(23.ToString(culture), hourText.Text,
                        "A -1 hour span must display as hour 23, never as \"-1\".");

                    // A span past a day wraps into the day like the flyout columns.
                    picker.SelectedTime = TimeSpan.FromHours(25);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(1.ToString(culture), hourText.Text,
                        "A 25 hour span must display as hour 1.");

                    // Negative minutes normalize into 0..59.
                    picker.SelectedTime = new TimeSpan(0, -30, 0);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(30.ToString("00", culture), minuteText.Text,
                        "A -30 minute span must display minute 30, never a negative minute.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_FieldClickAfterLightDismiss_DoesNotImmediatelyReopen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.TimePicker picker = new() { ClockIdentifier = "12HourClock" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the light dismiss is simulated.");

                    // A light dismiss closes the popup outside the control's own pipeline,
                    // exactly like the StaysOpen=false dismissal on the field mousedown.
                    popup.SetCurrentValue(Popup.IsOpenProperty, value: false);
                    DrainDispatcher(window.Dispatcher);

                    // The click of the same press-release gesture must not reopen the flyout.
                    RaiseButtonClick(flyoutButton);
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsFalse(popup.IsOpen,
                        "A field click right after a light dismiss must not reopen the flyout (toggle, not flicker).");

                    // Once the lockout has elapsed, the field opens the flyout again.
                    System.Threading.Thread.Sleep(300);
                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "A field click after the lockout must reopen the flyout.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TimePicker_SurfaceBrushes_ResolveAfterThemeCycle()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.IsNotNull(app?.TryFindResource("ControlFillColorDefaultBrush"),
                    "ControlFillColorDefaultBrush (field fill) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("ControlElevationBorderBrush"),
                    "ControlElevationBorderBrush (field stroke) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("ControlStrokeColorDefaultBrush"),
                    "ControlStrokeColorDefaultBrush (field segment dividers, WinUI TimePickerSpacerFill) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("DividerStrokeColorDefaultBrush"),
                    "DividerStrokeColorDefaultBrush (flyout divider) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("TextFillColorSecondaryBrush"),
                    "TextFillColorSecondaryBrush (placeholder foreground) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("SolidBackgroundFillColorTertiaryBrush"),
                    "SolidBackgroundFillColorTertiaryBrush (selector flyout fill) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("SurfaceStrokeColorFlyoutBrush"),
                    "SurfaceStrokeColorFlyoutBrush (selector flyout stroke) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("OverlayCornerRadius"),
                    "OverlayCornerRadius (selector flyout corner radius) must resolve after a full theme cycle.");
            });
        }
    }
}
