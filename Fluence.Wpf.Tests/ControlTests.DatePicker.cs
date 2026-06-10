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
using System.Collections.Generic;
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
    /// Tests for the WinUI-style <see cref="Controls.DatePicker"/> control: default style
    /// and template parts, field segment rendering, flyout population, accept/cancel
    /// commit semantics, day-count adjustment, automation peer naming, and surface brush
    /// theming.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void DatePicker_DefaultStyle_AppliesTemplateParts()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.DatePicker)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.DatePicker.");

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");

                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? dayList = template.FindName("PART_DayList", picker) as Selector;
                    Selector? monthList = template.FindName("PART_MonthList", picker) as Selector;
                    Selector? yearList = template.FindName("PART_YearList", picker) as Selector;
                    ButtonBase? acceptButton = template.FindName("PART_AcceptButton", picker) as ButtonBase;
                    ButtonBase? cancelButton = template.FindName("PART_CancelButton", picker) as ButtonBase;

                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be a ButtonBase so the field reads as a button.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(dayList, "PART_DayList must be a Selector hosting the day column.");
                    Assert.IsNotNull(monthList, "PART_MonthList must be a Selector hosting the month column.");
                    Assert.IsNotNull(yearList, "PART_YearList must be a Selector hosting the year column.");
                    Assert.IsNotNull(acceptButton, "PART_AcceptButton must be present in the template.");
                    Assert.IsNotNull(cancelButton, "PART_CancelButton must be present in the template.");
                    _ = Assert.IsInstanceOfType<Controls.ListBox>(dayList,
                        "The default template should present the day column through the Fluence ListBox.");
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
        public void DatePicker_SelectedDate_UpdatesFieldSegmentsAndPlaceholder()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new() { PlaceholderText = "Pick a date" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");

                    TextBlock? first = template.FindName("FirstSegmentText", picker) as TextBlock;
                    TextBlock? second = template.FindName("SecondSegmentText", picker) as TextBlock;
                    TextBlock? third = template.FindName("ThirdSegmentText", picker) as TextBlock;
                    TextBlock? placeholder = template.FindName("PlaceholderTextBlock", picker) as TextBlock;
                    FrameworkElement? segmentsHost = template.FindName("SegmentsHost", picker) as FrameworkElement;

                    Assert.IsNotNull(first, "FirstSegmentText must be present in the default template.");
                    Assert.IsNotNull(second, "SecondSegmentText must be present in the default template.");
                    Assert.IsNotNull(third, "ThirdSegmentText must be present in the default template.");
                    Assert.IsNotNull(placeholder, "PlaceholderTextBlock must be present in the default template.");
                    Assert.IsNotNull(segmentsHost, "SegmentsHost must be present in the default template.");

                    Assert.AreEqual(Visibility.Visible, placeholder.Visibility,
                        "The placeholder must be visible while SelectedDate is null.");
                    Assert.AreEqual(Visibility.Collapsed, segmentsHost.Visibility,
                        "The segment row must be collapsed while SelectedDate is null.");
                    Assert.AreEqual("Pick a date", placeholder.Text, "PlaceholderText must flow into the placeholder text block.");

                    DateTime date = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = date;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility,
                        "The placeholder must collapse once a date is selected.");
                    Assert.AreEqual(Visibility.Visible, segmentsHost.Visibility,
                        "The segment row must show once a date is selected.");

                    CultureInfo culture = CultureInfo.CurrentCulture;
                    List<string> expected =
                    [
                        date.Day.ToString(culture),
                        culture.DateTimeFormat.GetMonthName(date.Month),
                        date.Year.ToString(culture),
                    ];
                    List<string> actual = [first.Text, second.Text, third.Text];
                    CollectionAssert.AreEquivalent(expected, actual,
                        "The three segments must show the day, culture month name, and year (in culture short-date order).");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DatePicker_FieldClick_OpensPopupAndPopulatesColumns()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? dayList = template.FindName("PART_DayList", picker) as Selector;
                    Selector? monthList = template.FindName("PART_MonthList", picker) as Selector;
                    Selector? yearList = template.FindName("PART_YearList", picker) as Selector;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(dayList, "PART_DayList must be present in the template.");
                    Assert.IsNotNull(monthList, "PART_MonthList must be present in the template.");
                    Assert.IsNotNull(yearList, "PART_YearList must be present in the template.");

                    picker.SelectedDate = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "Clicking the field must open the selector flyout.");

                    // The open reveal (slide down from Y=-8 with a fade) must exist in the
                    // template and settle at rest once the 167ms storyboard completes.
                    TranslateTransform? translate =
                        template.FindName("FlyoutSurfaceTranslate", picker) as TranslateTransform;
                    Assert.IsNotNull(translate, "The DatePicker template must expose the FlyoutSurfaceTranslate reveal transform.");
                    Border? surface = template.FindName("FlyoutSurface", picker) as Border;
                    Assert.IsNotNull(surface, "The DatePicker template must expose the FlyoutSurface element.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => Math.Abs(translate.Y) < 0.001 && surface.Opacity >= 1.0),
                        "The flyout reveal must settle at Y=0 and full opacity.");

                    Assert.AreEqual(12, monthList.Items.Count, "The month column must offer the twelve culture month names.");
                    Assert.AreEqual(31, dayList.Items.Count, "The day column must offer 31 days for May.");
                    Assert.AreEqual(picker.MaxYear - picker.MinYear + 1, yearList.Items.Count,
                        "The year column must span MinYear..MaxYear inclusive.");
                    Assert.AreEqual(4, monthList.SelectedIndex, "May must be preselected in the month column.");
                    Assert.AreEqual(16, dayList.SelectedIndex, "Day 17 must be preselected in the day column.");
                    Assert.AreEqual(2024 - picker.MinYear, yearList.SelectedIndex, "2024 must be preselected in the year column.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DatePicker_Accept_CommitsSelectionAndRaisesSelectedDateChanged()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? dayList = template.FindName("PART_DayList", picker) as Selector;
                    Selector? monthList = template.FindName("PART_MonthList", picker) as Selector;
                    Selector? yearList = template.FindName("PART_YearList", picker) as Selector;
                    ButtonBase? acceptButton = template.FindName("PART_AcceptButton", picker) as ButtonBase;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(dayList, "PART_DayList must be present in the template.");
                    Assert.IsNotNull(monthList, "PART_MonthList must be present in the template.");
                    Assert.IsNotNull(yearList, "PART_YearList must be present in the template.");
                    Assert.IsNotNull(acceptButton, "PART_AcceptButton must be present in the template.");

                    DateTime oldDate = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = oldDate;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the accept scenario.");

                    DatePickerSelectedValueChangedEventArgs? captured = null;
                    picker.SelectedDateChanged += (_, args) => captured = args;

                    monthList.SelectedIndex = 0;
                    yearList.SelectedIndex = 2025 - picker.MinYear;
                    dayList.SelectedIndex = 9;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(acceptButton);
                    DrainDispatcher(window.Dispatcher);

                    DateTime expected = new(2025, 1, 10, 0, 0, 0, DateTimeKind.Unspecified);
                    Assert.AreEqual(expected, picker.SelectedDate, "Accept must commit the three column values into SelectedDate.");
                    Assert.IsNotNull(captured, "Accept must raise SelectedDateChanged.");
                    Assert.AreEqual(oldDate, captured.OldDate, "OldDate must carry the previously selected date.");
                    Assert.AreEqual(expected, captured.NewDate, "NewDate must carry the committed date.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Accept must close the selector flyout.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DatePicker_Cancel_RevertsPendingSelection()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? dayList = template.FindName("PART_DayList", picker) as Selector;
                    Selector? monthList = template.FindName("PART_MonthList", picker) as Selector;
                    ButtonBase? cancelButton = template.FindName("PART_CancelButton", picker) as ButtonBase;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(dayList, "PART_DayList must be present in the template.");
                    Assert.IsNotNull(monthList, "PART_MonthList must be present in the template.");
                    Assert.IsNotNull(cancelButton, "PART_CancelButton must be present in the template.");

                    DateTime original = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = original;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the cancel scenario.");

                    bool raised = false;
                    picker.SelectedDateChanged += (_, _) => raised = true;

                    monthList.SelectedIndex = 0;
                    dayList.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(cancelButton);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(original, picker.SelectedDate, "Cancel must leave SelectedDate unchanged.");
                    Assert.IsFalse(raised, "Cancel must not raise SelectedDateChanged.");
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
        public void DatePicker_DayColumn_AdjustsToMonthLength()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? dayList = template.FindName("PART_DayList", picker) as Selector;
                    Selector? monthList = template.FindName("PART_MonthList", picker) as Selector;
                    Selector? yearList = template.FindName("PART_YearList", picker) as Selector;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(dayList, "PART_DayList must be present in the template.");
                    Assert.IsNotNull(monthList, "PART_MonthList must be present in the template.");
                    Assert.IsNotNull(yearList, "PART_YearList must be present in the template.");

                    picker.SelectedDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Unspecified);
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the day-count scenario.");

                    Assert.AreEqual(31, dayList.Items.Count, "January must offer 31 days.");
                    Assert.AreEqual(30, dayList.SelectedIndex, "Day 31 must be preselected.");

                    monthList.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(28, dayList.Items.Count, "February 2023 must shrink the day column to 28 days.");
                    Assert.AreEqual(27, dayList.SelectedIndex, "The pending day must clamp to 28 when February is chosen.");

                    yearList.SelectedIndex = 2024 - picker.MinYear;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(29, dayList.Items.Count, "February 2024 (leap year) must grow the day column to 29 days.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DatePicker_AutomationPeer_ReportsNameFromDateOrPlaceholder()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new() { PlaceholderText = "Pick a date" };

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(picker);
                    Assert.IsNotNull(peer, "DatePicker must create an automation peer.");
                    _ = Assert.IsInstanceOfType<Automation.DatePickerAutomationPeer>(peer,
                        "DatePicker must expose the DatePickerAutomationPeer.");
                    Assert.AreEqual("DatePicker", peer.GetClassName(), "The peer must report the DatePicker class name.");
                    Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType(),
                        "The peer must report the Group control type.");
                    Assert.AreEqual("Pick a date", peer.GetName(),
                        "The peer name must fall back to PlaceholderText while no date is selected.");

                    DateTime date = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = date;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(date.ToString("d", CultureInfo.CurrentCulture), peer.GetName(),
                        "The peer name must report the selected date in the culture short date format.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DatePicker_FlyoutOpen_MovesKeyboardFocusIntoPopupAndCyclesTab()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
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
        public void DatePicker_FlyoutEscape_ClosesWithoutCommitting()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? dayList = template.FindName("PART_DayList", picker) as Selector;
                    Selector? monthList = template.FindName("PART_MonthList", picker) as Selector;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(dayList, "PART_DayList must be present in the template.");
                    Assert.IsNotNull(monthList, "PART_MonthList must be present in the template.");

                    DateTime original = new(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    picker.SelectedDate = original;
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the Escape scenario.");
                    Assert.IsNotNull(popup.Child, "The selector flyout must have a popup child root.");

                    bool raised = false;
                    picker.SelectedDateChanged += (_, _) => raised = true;

                    monthList.SelectedIndex = 0;
                    dayList.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popup.Child, Key.Escape, UIElement.PreviewKeyDownEvent);

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Escape must close the selector flyout.");
                    Assert.AreEqual(original, picker.SelectedDate, "Escape must not commit the pending column selection.");
                    Assert.IsFalse(raised, "Escape must not raise SelectedDateChanged.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DatePicker_FlyoutEnter_CommitsPendingSelection()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    Selector? dayList = template.FindName("PART_DayList", picker) as Selector;
                    Selector? monthList = template.FindName("PART_MonthList", picker) as Selector;
                    Selector? yearList = template.FindName("PART_YearList", picker) as Selector;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(dayList, "PART_DayList must be present in the template.");
                    Assert.IsNotNull(monthList, "PART_MonthList must be present in the template.");
                    Assert.IsNotNull(yearList, "PART_YearList must be present in the template.");

                    picker.SelectedDate = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Unspecified);
                    DrainDispatcher(window.Dispatcher);

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the Enter scenario.");
                    Assert.IsNotNull(popup.Child, "The selector flyout must have a popup child root.");

                    monthList.SelectedIndex = 0;
                    yearList.SelectedIndex = 2025 - picker.MinYear;
                    dayList.SelectedIndex = 9;
                    DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(popup.Child, Key.Enter, UIElement.PreviewKeyDownEvent);

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Enter must close the selector flyout.");
                    Assert.AreEqual(new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), picker.SelectedDate,
                        "Enter must commit the pending column selection like the accept button.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DatePicker_SurfaceBrushes_ResolveAfterThemeCycle()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.IsNotNull(app?.TryFindResource("ControlFillColorDefaultBrush"),
                    "ControlFillColorDefaultBrush (field fill) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("ControlElevationBorderBrush"),
                    "ControlElevationBorderBrush (field stroke) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("ControlStrokeColorDefaultBrush"),
                    "ControlStrokeColorDefaultBrush (field segment dividers, WinUI DatePickerSpacerFill) must resolve after a full theme cycle.");
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

        [TestMethod]
        public void DatePicker_NonGregorianDefaultCulture_UsesGregorianMonthNames()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                CultureInfo originalCulture = CultureInfo.CurrentCulture;
                Window window = new() { Width = 500, Height = 400 };

                try
                {
                    // ar-SA defaults to the Um Al Qura calendar, so unpinned month names
                    // would belong to a different calendar than the Gregorian day/year math.
                    CultureInfo culture = CultureInfo.GetCultureInfo("ar-SA");
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                    DateTimeFormatInfo gregorianFormat = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
                    GregorianCalendar? gregorian = null;
                    foreach (System.Globalization.Calendar optionalCalendar in culture.OptionalCalendars)
                    {
                        if (optionalCalendar is GregorianCalendar candidate)
                        {
                            gregorian = candidate;
                            break;
                        }
                    }

                    if (gregorian is null)
                    {
                        Assert.Inconclusive("ar-SA offers no optional Gregorian calendar on this runtime.");
                        return;
                    }

                    gregorianFormat.Calendar = gregorian;
                    DateTime march = new(2024, 3, 15, 0, 0, 0, DateTimeKind.Unspecified);
                    string expectedMonthName = gregorianFormat.GetMonthName(3);

                    Controls.DatePicker picker = new() { SelectedDate = march };
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    TextBlock? first = template.FindName("FirstSegmentText", picker) as TextBlock;
                    TextBlock? second = template.FindName("SecondSegmentText", picker) as TextBlock;
                    TextBlock? third = template.FindName("ThirdSegmentText", picker) as TextBlock;
                    Assert.IsNotNull(first, "FirstSegmentText must be present in the default template.");
                    Assert.IsNotNull(second, "SecondSegmentText must be present in the default template.");
                    Assert.IsNotNull(third, "ThirdSegmentText must be present in the default template.");

                    List<string> segments = [first.Text, second.Text, third.Text];
                    Assert.IsTrue(segments.Contains(expectedMonthName),
                        string.Format(
                            "The month segment must show the Gregorian month name '{0}' (segments: {1}).",
                            expectedMonthName,
                            string.Join(" | ", segments)));

                    string defaultCalendarName = culture.DateTimeFormat.GetMonthName(3);
                    if (!string.Equals(defaultCalendarName, expectedMonthName, StringComparison.Ordinal))
                    {
                        Assert.IsFalse(segments.Contains(defaultCalendarName),
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

        [TestMethod]
        public void DatePicker_FieldClickAfterLightDismiss_DoesNotImmediatelyReopen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };
                Controls.DatePicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");
                    ButtonBase? flyoutButton = template.FindName("PART_FlyoutButton", picker) as ButtonBase;
                    Popup? popup = template.FindName("PART_Popup", picker) as Popup;
                    ButtonBase? acceptButton = template.FindName("PART_AcceptButton", picker) as ButtonBase;
                    Assert.IsNotNull(flyoutButton, "PART_FlyoutButton must be present in the template.");
                    Assert.IsNotNull(popup, "PART_Popup must be present in the template.");
                    Assert.IsNotNull(acceptButton, "PART_AcceptButton must be present in the template.");

                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
                        "The selector flyout must open before the light dismiss is simulated.");

                    // A light dismiss closes the popup outside the control's own pipeline,
                    // exactly like the StaysOpen=false dismissal on the field mousedown.
                    popup.SetCurrentValue(Popup.IsOpenProperty, false);
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

                    // Accept-driven closes do not arm the lockout: an immediate reopen works.
                    RaiseButtonClick(acceptButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !popup.IsOpen),
                        "Accept must close the selector flyout.");
                    RaiseButtonClick(flyoutButton);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => popup.IsOpen),
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
