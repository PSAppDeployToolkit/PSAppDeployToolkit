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
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

// IMPORTANT: every reference to TextBlock / ListBox in this file MUST be fully qualified
// (System.Windows.Controls.TextBlock, System.Windows.Controls.ListBox). The
// Fluence.Wpf.Controls namespace defines its own TextBlock / ListBox subclasses, and
// because this file sits inside that namespace, any unqualified reference resolves to the
// Fluence subclass. The template part contract is typed against the stock WPF base types
// so both the default template (which hosts the Fluence controls) and custom templates
// resolve correctly. See AutoSuggestBox.cs for the same pattern.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A control that lets the user pick a calendar date from day, month, and year selector
    /// columns hosted in a light-dismiss flyout, mirroring the WinUI 3 <c>DatePicker</c>.
    /// The always-visible field is a button-styled row showing the selected day, month name,
    /// and year ordered by the current culture's short date pattern; the flyout commits the
    /// pending column selection through its accept button and discards it through cancel.
    /// </summary>
    [TemplatePart(Name = PART_FlyoutButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_DayList, Type = typeof(Selector))]
    [TemplatePart(Name = PART_MonthList, Type = typeof(Selector))]
    [TemplatePart(Name = PART_YearList, Type = typeof(Selector))]
    [TemplatePart(Name = PART_AcceptButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_CancelButton, Type = typeof(ButtonBase))]
    public class DatePicker : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_FlyoutButton = "PART_FlyoutButton";
        private const string PART_Popup = "PART_Popup";
        private const string PART_DayList = "PART_DayList";
        private const string PART_MonthList = "PART_MonthList";
        private const string PART_YearList = "PART_YearList";
        private const string PART_AcceptButton = "PART_AcceptButton";
        private const string PART_CancelButton = "PART_CancelButton";

        // Optional named template children that render the field segments. A custom
        // template may omit them; every access is null-guarded.
        private const string SegmentsHostName = "SegmentsHost";
        private const string FirstSegmentTextName = "FirstSegmentText";
        private const string SecondSegmentTextName = "SecondSegmentText";
        private const string ThirdSegmentTextName = "ThirdSegmentText";
        private const string FirstDividerName = "FirstDivider";
        private const string SecondDividerName = "SecondDivider";
        private const string SelectorsGridName = "SelectorsGrid";

        /// <summary>
        /// Initializes static members of the DatePicker class and overrides the default
        /// style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static DatePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DatePicker),
                new FrameworkPropertyMetadata(typeof(DatePicker)));
        }

        /// <summary>
        /// Identifies the <see cref="SelectedDate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(
                nameof(SelectedDate),
                typeof(DateTime?),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(
defaultValue: null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedDateChanged));

        /// <summary>
        /// Gets or sets the currently selected date, or <see langword="null"/> when no date
        /// has been picked yet. Changing this property raises <see cref="SelectedDateChanged"/>.
        /// </summary>
        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinYear"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinYearProperty =
            DependencyProperty.Register(
                nameof(MinYear),
                typeof(int),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(1900));

        /// <summary>
        /// Gets or sets the first year offered by the year selector column.
        /// </summary>
        public int MinYear
        {
            get => (int)GetValue(MinYearProperty);
            set => SetValue(MinYearProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxYear"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxYearProperty =
            DependencyProperty.Register(
                nameof(MaxYear),
                typeof(int),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(2100));

        /// <summary>
        /// Gets or sets the last year offered by the year selector column.
        /// </summary>
        public int MaxYear
        {
            get => (int)GetValue(MaxYearProperty);
            set => SetValue(MaxYearProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DayVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DayVisibleProperty =
            DependencyProperty.Register(
                nameof(DayVisible),
                typeof(bool),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(defaultValue: true, OnFieldVisibilityChanged));

        /// <summary>
        /// Gets or sets whether the day segment and selector column are shown.
        /// </summary>
        public bool DayVisible
        {
            get => (bool)GetValue(DayVisibleProperty);
            set => SetValue(DayVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MonthVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthVisibleProperty =
            DependencyProperty.Register(
                nameof(MonthVisible),
                typeof(bool),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(defaultValue: true, OnFieldVisibilityChanged));

        /// <summary>
        /// Gets or sets whether the month segment and selector column are shown.
        /// </summary>
        public bool MonthVisible
        {
            get => (bool)GetValue(MonthVisibleProperty);
            set => SetValue(MonthVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="YearVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearVisibleProperty =
            DependencyProperty.Register(
                nameof(YearVisible),
                typeof(bool),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(defaultValue: true, OnFieldVisibilityChanged));

        /// <summary>
        /// Gets or sets whether the year segment and selector column are shown.
        /// </summary>
        public bool YearVisible
        {
            get => (bool)GetValue(YearVisibleProperty);
            set => SetValue(YearVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the optional header content shown above the field.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(DatePicker),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text shown in the field while
        /// <see cref="SelectedDate"/> is <see langword="null"/> (for example "Pick a date").
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Occurs after <see cref="SelectedDate"/> changes, whether through the flyout's
        /// accept button or a programmatic update.
        /// </summary>
        public event EventHandler<DatePickerSelectedValueChangedEventArgs>? SelectedDateChanged;

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            // Namespace-qualified: System.Windows.Automation.Peers also declares a
            // DatePickerAutomationPeer (for the stock WPF DatePicker).
            return new Automation.DatePickerAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Unsubscribe-first so re-templating never leaves stale handlers behind.
            _flyoutButton?.Click -= OnFlyoutButtonClick;
            _acceptButton?.Click -= OnAcceptButtonClick;
            _cancelButton?.Click -= OnCancelButtonClick;
            _monthList?.SelectionChanged -= OnMonthOrYearSelectionChanged;
            _yearList?.SelectionChanged -= OnMonthOrYearSelectionChanged;
            _popup?.Closed -= OnPopupClosed;
            _popupRoot?.PreviewKeyDown -= OnPopupPreviewKeyDown;

            base.OnApplyTemplate();

            _flyoutButton = GetTemplateChild(PART_FlyoutButton) as ButtonBase;
            _popup = GetTemplateChild(PART_Popup) as Popup;
            _dayList = GetTemplateChild(PART_DayList) as Selector;
            _monthList = GetTemplateChild(PART_MonthList) as Selector;
            _yearList = GetTemplateChild(PART_YearList) as Selector;
            _acceptButton = GetTemplateChild(PART_AcceptButton) as ButtonBase;
            _cancelButton = GetTemplateChild(PART_CancelButton) as ButtonBase;
            _selectorsGrid = GetTemplateChild(SelectorsGridName) as Grid;
            _segmentsHost = GetTemplateChild(SegmentsHostName) as Grid;
            _segmentTexts =
            [
                GetTemplateChild(FirstSegmentTextName) as System.Windows.Controls.TextBlock,
                GetTemplateChild(SecondSegmentTextName) as System.Windows.Controls.TextBlock,
                GetTemplateChild(ThirdSegmentTextName) as System.Windows.Controls.TextBlock,
            ];
            _segmentDividers =
            [
                GetTemplateChild(FirstDividerName) as FrameworkElement,
                GetTemplateChild(SecondDividerName) as FrameworkElement,
            ];

            _flyoutButton?.Click += OnFlyoutButtonClick;
            _acceptButton?.Click += OnAcceptButtonClick;
            _cancelButton?.Click += OnCancelButtonClick;
            _monthList?.SelectionChanged += OnMonthOrYearSelectionChanged;
            _yearList?.SelectionChanged += OnMonthOrYearSelectionChanged;
            _popup?.Closed += OnPopupClosed;

            _popupRoot = _popup?.Child;
            if (_popupRoot is not null)
            {
                // Keep Tab cycling inside the flyout instead of escaping to the window behind
                // it, and intercept Escape (cancel) / Enter (accept) at the popup root.
                KeyboardNavigation.SetTabNavigation(_popupRoot, KeyboardNavigationMode.Cycle);
                _popupRoot.PreviewKeyDown += OnPopupPreviewKeyDown;
            }

            UpdateFieldText();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0091:Sender should be 'this' for instance events", Justification = "The method is static.")]
        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DatePicker picker = (DatePicker)d;
            picker.UpdateFieldText();
            picker.SelectedDateChanged?.Invoke(
                picker,
                new DatePickerSelectedValueChangedEventArgs(e.OldValue as DateTime?, e.NewValue as DateTime?));
        }

        private static void OnFieldVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DatePicker picker = (DatePicker)d;
            picker.UpdateFieldText();
            picker.ApplySelectorColumnLayout();
        }

        /// <summary>
        /// Returns day, month, and year in the order they first occur in the current
        /// culture's short date pattern, so the field segments and selector columns read
        /// naturally (for example month-day-year for en-US, day-month-year for en-GB).
        /// Quoted literals inside exotic patterns are not skipped; for short date patterns
        /// they do not contain the d/M/y specifier characters in practice.
        /// </summary>
        /// <param name="format">The <see cref="DateTimeFormatInfo"/> to evaluate.</param>
        /// <returns>A list of <see cref="DateField"/> values in the order they appear in the short date pattern.</returns>
        private static List<DateField> GetCultureOrderedFields(DateTimeFormatInfo format)
        {
            List<DateField> ordered = [];
            foreach (char patternChar in format.ShortDatePattern)
            {
                if (patternChar == 'd' && !ordered.Contains(DateField.Day))
                {
                    ordered.Add(DateField.Day);
                }
                else if (patternChar == 'M' && !ordered.Contains(DateField.Month))
                {
                    ordered.Add(DateField.Month);
                }
                else if (patternChar == 'y' && !ordered.Contains(DateField.Year))
                {
                    ordered.Add(DateField.Year);
                }
            }

            // Defensive fallback for patterns that omit a specifier entirely.
            if (!ordered.Contains(DateField.Month))
            {
                ordered.Add(DateField.Month);
            }

            if (!ordered.Contains(DateField.Day))
            {
                ordered.Add(DateField.Day);
            }

            if (!ordered.Contains(DateField.Year))
            {
                ordered.Add(DateField.Year);
            }

            return ordered;
        }

        /// <summary>
        /// Formats one field of <paramref name="date"/> for display: the day and year as
        /// culture-formatted numbers, the month as the culture's full Gregorian month name
        /// (see <see cref="GetGregorianFormat"/>).
        /// </summary>
        /// <param name="field">The field to format.</param>
        /// <param name="date">The date to format.</param>
        /// <param name="culture">The culture to use for formatting.</param>
        /// <returns>The formatted string for the specified field.</returns>
        private static string FormatSegment(DateField field, DateTime date, CultureInfo culture)
        {
            return field == DateField.Day
                ? date.Day.ToString(culture)
                : field == DateField.Month
                    ? GetGregorianFormat(culture).GetMonthName(date.Month)
                    : date.Year.ToString(culture);
        }

        /// <summary>
        /// Returns the culture's date format pinned to the Gregorian calendar so month names
        /// match the Gregorian <see cref="DateTime"/> day/year math the control performs.
        /// Cultures whose default calendar is non-Gregorian (for example Um Al Qura) would
        /// otherwise pair Gregorian numbers with the names of a different calendar. Falls back
        /// to the invariant format for the rare culture without an optional Gregorian calendar.
        /// </summary>
        /// <param name="culture">The culture to evaluate.</param>
        /// <returns>The <see cref="DateTimeFormatInfo"/> pinned to the Gregorian calendar.</returns>
        private static DateTimeFormatInfo GetGregorianFormat(CultureInfo culture)
        {
            DateTimeFormatInfo format = culture.DateTimeFormat;
            if (format.Calendar is GregorianCalendar)
            {
                return format;
            }

            foreach (System.Globalization.Calendar optionalCalendar in culture.OptionalCalendars)
            {
                if (optionalCalendar is GregorianCalendar gregorian)
                {
                    DateTimeFormatInfo pinned = (DateTimeFormatInfo)format.Clone();
                    pinned.Calendar = gregorian;
                    return pinned;
                }
            }

            return DateTimeFormatInfo.InvariantInfo;
        }

        /// <summary>
        /// Returns the WinUI 3 star-width weight for a field column (78* for day and year,
        /// 132* for the wider month column).
        /// </summary>
        /// <param name="field">The field to evaluate.</param>
        /// <returns>The star-width weight for the specified field.</returns>
        private static double GetFieldStarWidth(DateField field)
        {
            return field == DateField.Month ? 132 : 78;
        }

        /// <summary>
        /// Builds the day column items 1..<paramref name="dayCount"/>.
        /// </summary>
        /// <param name="dayCount">The number of days to generate.</param>
        /// <returns>A list of day numbers from 1 to <paramref name="dayCount"/>.</returns>
        private static List<int> BuildDayItems(int dayCount)
        {
            List<int> days = [];
            for (int day = 1; day <= dayCount; day++)
            {
                days.Add(day);
            }

            return days;
        }

        /// <summary>
        /// Scrolls the selector's current selection into view when it is a ListBox.
        /// </summary>
        /// <param name="selector">The selector to scroll.</param>
        private static void ScrollSelectionIntoView(Selector? selector)
        {
            if (selector is System.Windows.Controls.ListBox listBox && listBox.SelectedItem is not null)
            {
                listBox.ScrollIntoView(listBox.SelectedItem);
            }
        }

        private void OnFlyoutButtonClick(object sender, RoutedEventArgs e)
        {
            if (_popup is null || IsWithinLightDismissReopenLockout())
            {
                return;
            }

            PopulateSelectorColumns();
            _popup.SetCurrentValue(Popup.IsOpenProperty, value: true);

            // Item containers exist only after the popup child's first layout pass, so defer
            // the focus move to Loaded priority (below Render) like the other in-tree
            // post-layout callbacks.
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(MoveFocusIntoPopup));
        }

        /// <summary>
        /// Returns whether a flyout-button click arrived within the short lockout after a
        /// light dismiss. Clicking the open field light-dismisses the StaysOpen=false popup
        /// on the mouse press, and without this guard the same click would immediately
        /// reopen it, so the field could never close its own flyout.
        /// </summary>
        private bool IsWithinLightDismissReopenLockout()
        {
            return _lastLightDismissTick.HasValue
                && unchecked(Environment.TickCount - _lastLightDismissTick.Value) < LightDismissReopenLockoutMilliseconds;
        }

        /// <summary>
        /// Records when the popup closes through a light dismiss (any close that did not run
        /// through <see cref="ClosePopup"/>), arming the reopen lockout consumed by
        /// <see cref="OnFlyoutButtonClick"/>. Accept, cancel, and Escape closes do not arm it,
        /// so programmatic close-and-reopen flows stay instant.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnPopupClosed(object? sender, EventArgs e)
        {
            if (_popupSelfClosing)
            {
                _popupSelfClosing = false;
                return;
            }

            _lastLightDismissTick = Environment.TickCount;
        }

        private void OnAcceptButtonClick(object sender, RoutedEventArgs e)
        {
            CommitPendingSelection();
            ClosePopup();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            ClosePopup();
        }

        /// <summary>
        /// Handles flyout keyboard interaction at the popup root: Escape discards the pending
        /// column selection (the cancel-button path) and Enter commits it (the accept-button
        /// path). Enter is left alone while a flyout command button has keyboard focus so the
        /// button's native click handling wins.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnPopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || _popup?.IsOpen != true)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                ClosePopup();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                IInputElement? focused = Keyboard.FocusedElement;
                if (ReferenceEquals(focused, _acceptButton) || ReferenceEquals(focused, _cancelButton))
                {
                    return;
                }

                CommitPendingSelection();
                ClosePopup();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Moves keyboard focus to the selected item of the first visible selector column once
        /// the open flyout has laid out, so the flyout is immediately keyboard operable.
        /// </summary>
        private void MoveFocusIntoPopup()
        {
            if (_popup?.IsOpen != true)
            {
                return;
            }

            Selector? column = GetFirstVisibleSelectorColumn();
            if (column is null)
            {
                return;
            }

            int index = Math.Max(column.SelectedIndex, 0);
            if (column.ItemContainerGenerator.ContainerFromIndex(index) is IInputElement container)
            {
                _ = Keyboard.Focus(container);
            }
            else
            {
                _ = column.Focus();
            }
        }

        /// <summary>
        /// Returns the first visible selector column in the culture order of the visible
        /// fields (see <see cref="GetOrderedVisibleFields"/>).
        /// </summary>
        private Selector? GetFirstVisibleSelectorColumn()
        {
            foreach (DateField field in GetOrderedVisibleFields())
            {
                Selector? column = GetSelector(field);
                if (column is not null && column.Visibility == Visibility.Visible)
                {
                    return column;
                }
            }

            return null;
        }

        private void OnMonthOrYearSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressColumnSync)
            {
                return;
            }

            RefreshDayColumn();
        }

        /// <summary>
        /// Fills the three selector columns from <see cref="SelectedDate"/> (or today when
        /// unset, clamped into the <see cref="MinYear"/>..<see cref="MaxYear"/> range) and
        /// highlights the matching items. The columns are plain scrollable lists; WinUI's
        /// infinitely looping selectors are a deliberate v1 omission.
        /// </summary>
        private void PopulateSelectorColumns()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            int minYear = MinYear;
            int maxYear = Math.Max(minYear, MaxYear);
            DateTime baseDate = SelectedDate ?? DateTime.Today;
            int year = Math.Min(Math.Max(baseDate.Year, minYear), maxYear);
            int month = baseDate.Month;
            int day = Math.Min(baseDate.Day, DateTime.DaysInMonth(year, month));
            _flyoutBaseDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
            _populatedMinYear = minYear;

            _suppressColumnSync = true;
            try
            {
                if (_monthList is not null)
                {
                    // Gregorian-pinned twelve month names so they match the Gregorian
                    // day/year math; alternate calendars are out of scope.
                    DateTimeFormatInfo gregorianFormat = GetGregorianFormat(culture);
                    List<string> months = [];
                    for (int monthIndex = 1; monthIndex <= 12; monthIndex++)
                    {
                        months.Add(gregorianFormat.GetMonthName(monthIndex));
                    }

                    _monthList.ItemsSource = months;
                    _monthList.SelectedIndex = month - 1;
                }

                if (_yearList is not null)
                {
                    List<int> years = [];
                    for (int yearValue = minYear; yearValue <= maxYear; yearValue++)
                    {
                        years.Add(yearValue);
                    }

                    _yearList.ItemsSource = years;
                    _yearList.SelectedIndex = year - minYear;
                }

                if (_dayList is not null)
                {
                    _dayList.ItemsSource = BuildDayItems(DateTime.DaysInMonth(year, month));
                    _dayList.SelectedIndex = day - 1;
                }
            }
            finally
            {
                _suppressColumnSync = false;
            }

            ApplySelectorColumnLayout();
            ScrollSelectionIntoView(_dayList);
            ScrollSelectionIntoView(_monthList);
            ScrollSelectionIntoView(_yearList);
        }

        /// <summary>
        /// Rebuilds the day column to 1..DaysInMonth for the pending month and year,
        /// clamping the pending day when the new month is shorter.
        /// </summary>
        private void RefreshDayColumn()
        {
            if (_dayList is null)
            {
                return;
            }

            int dayCount = DateTime.DaysInMonth(GetPendingYear(), GetPendingMonth());
            if (_dayList.Items.Count == dayCount)
            {
                return;
            }

            int day = Math.Min(GetPendingDay(), dayCount);
            _suppressColumnSync = true;
            try
            {
                _dayList.ItemsSource = BuildDayItems(dayCount);
                _dayList.SelectedIndex = day - 1;
            }
            finally
            {
                _suppressColumnSync = false;
            }

            ScrollSelectionIntoView(_dayList);
        }

        /// <summary>
        /// Commits the pending day, month, and year column values into
        /// <see cref="SelectedDate"/>, clamping the day to the month length.
        /// </summary>
        private void CommitPendingSelection()
        {
            int year = GetPendingYear();
            int month = GetPendingMonth();
            int day = Math.Min(GetPendingDay(), DateTime.DaysInMonth(year, month));
            SetCurrentValue(SelectedDateProperty, new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified));
        }

        private void ClosePopup()
        {
            if (_popup?.IsOpen == true)
            {
                // Closing through the control's own pipeline must not arm the light-dismiss
                // reopen lockout; Popup.Closed is raised synchronously from the set below.
                _popupSelfClosing = true;
                _popup.SetCurrentValue(Popup.IsOpenProperty, value: false);
            }
        }

        private int GetPendingDay()
        {
            return _dayList?.SelectedIndex >= 0
                ? _dayList.SelectedIndex + 1
                : _flyoutBaseDate.Day;
        }

        private int GetPendingMonth()
        {
            return _monthList?.SelectedIndex >= 0
                ? _monthList.SelectedIndex + 1
                : _flyoutBaseDate.Month;
        }

        private int GetPendingYear()
        {
            return _yearList?.SelectedIndex >= 0
                ? _populatedMinYear + _yearList.SelectedIndex
                : _flyoutBaseDate.Year;
        }

        /// <summary>
        /// Returns the selector column belonging to <paramref name="field"/>.
        /// </summary>
        /// <param name="field">The field to evaluate.</param>
        /// <returns>The selector column for the specified field.</returns>
        private Selector? GetSelector(DateField field)
        {
            return field == DateField.Day
                ? _dayList
                : field == DateField.Month ? _monthList : _yearList;
        }

        /// <summary>
        /// Returns the fields shown by the control: the culture order filtered by
        /// <see cref="DayVisible"/>, <see cref="MonthVisible"/>, and <see cref="YearVisible"/>.
        /// </summary>
        private List<DateField> GetOrderedVisibleFields()
        {
            List<DateField> visible = [];
            foreach (DateField field in GetCultureOrderedFields(CultureInfo.CurrentCulture.DateTimeFormat))
            {
                bool isVisible = (field == DateField.Day && DayVisible)
                    || (field == DateField.Month && MonthVisible)
                    || (field == DateField.Year && YearVisible);
                if (isVisible)
                {
                    visible.Add(field);
                }
            }

            return visible;
        }

        /// <summary>
        /// Writes the selected date into the field segments in culture order, collapsing
        /// unused segments and dividers. The placeholder swap for a null
        /// <see cref="SelectedDate"/> is handled by the template trigger.
        /// </summary>
        private void UpdateFieldText()
        {
            if (_segmentTexts.Length < 3)
            {
                return;
            }

            CultureInfo culture = CultureInfo.CurrentCulture;
            List<DateField> fields = GetOrderedVisibleFields();
            DateTime? selectedDate = SelectedDate;
            for (int position = 0; position < 3; position++)
            {
                System.Windows.Controls.TextBlock? segment = _segmentTexts[position];
                if (segment is null)
                {
                    continue;
                }

                if (position < fields.Count)
                {
                    segment.Text = selectedDate.HasValue
                        ? FormatSegment(fields[position], selectedDate.Value, culture)
                        : string.Empty;
                    segment.HorizontalAlignment = fields[position] == DateField.Month
                        ? HorizontalAlignment.Left
                        : HorizontalAlignment.Center;
                    segment.Visibility = Visibility.Visible;
                }
                else
                {
                    segment.Text = string.Empty;
                    segment.Visibility = Visibility.Collapsed;
                }
            }

            if (_segmentDividers.Length >= 2)
            {
                _segmentDividers[0]?.SetCurrentValue(VisibilityProperty, fields.Count >= 2 ? Visibility.Visible : Visibility.Collapsed);
                _segmentDividers[1]?.SetCurrentValue(VisibilityProperty, fields.Count >= 3 ? Visibility.Visible : Visibility.Collapsed);
            }

            ApplySegmentColumnWidths(fields);
        }

        /// <summary>
        /// Sizes the field's three segment columns (grid columns 0, 2, and 4; the dividers
        /// sit in the Auto columns between them) to the WinUI star weights, zeroing columns
        /// beyond the visible field count.
        /// </summary>
        /// <param name="fields">The list of visible fields in culture order.</param>
        private void ApplySegmentColumnWidths(List<DateField> fields)
        {
            if (_segmentsHost is null || _segmentsHost.ColumnDefinitions.Count < 5)
            {
                return;
            }

            for (int position = 0; position < 3; position++)
            {
                ColumnDefinition column = _segmentsHost.ColumnDefinitions[position * 2];
                column.Width = position < fields.Count
                    ? new GridLength(GetFieldStarWidth(fields[position]), GridUnitType.Star)
                    : new GridLength(0);
            }
        }

        /// <summary>
        /// Orders the selector columns to match the culture order of the visible fields,
        /// collapsing the columns of hidden fields.
        /// </summary>
        private void ApplySelectorColumnLayout()
        {
            if (_selectorsGrid is null || _selectorsGrid.ColumnDefinitions.Count < 3)
            {
                return;
            }

            foreach (ColumnDefinition column in _selectorsGrid.ColumnDefinitions)
            {
                column.Width = new GridLength(0);
            }

            List<DateField> orderedVisible = GetOrderedVisibleFields();
            int position = 0;
            foreach (DateField field in orderedVisible)
            {
                Selector? selector = GetSelector(field);
                if (selector is null || position >= _selectorsGrid.ColumnDefinitions.Count)
                {
                    continue;
                }

                selector.SetCurrentValue(VisibilityProperty, Visibility.Visible);
                Grid.SetColumn(selector, position);
                _selectorsGrid.ColumnDefinitions[position].Width = new GridLength(GetFieldStarWidth(field), GridUnitType.Star);
                position++;
            }

            HideSelectorIfAbsent(DateField.Day, orderedVisible);
            HideSelectorIfAbsent(DateField.Month, orderedVisible);
            HideSelectorIfAbsent(DateField.Year, orderedVisible);
        }

        /// <summary>
        /// Collapses the selector column for <paramref name="field"/> when it is not in the
        /// visible field set.
        /// </summary>
        /// <param name="field">The field to evaluate.</param>
        /// <param name="visibleFields">The list of visible fields in culture order.</param>
        private void HideSelectorIfAbsent(DateField field, List<DateField> visibleFields)
        {
            if (!visibleFields.Contains(field))
            {
                GetSelector(field)?.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
            }
        }

        /// <summary>
        /// The button-styled field that opens the flyout.
        /// </summary>
        private ButtonBase? _flyoutButton;

        /// <summary>
        /// The light-dismiss popup hosting the selector columns.
        /// </summary>
        private Popup? _popup;

        /// <summary>
        /// The popup child root that carries the flyout keyboard handling (Tab cycle plus
        /// Escape/Enter interception).
        /// </summary>
        private UIElement? _popupRoot;

        /// <summary>
        /// The day selector column.
        /// </summary>
        private Selector? _dayList;

        /// <summary>
        /// The month selector column.
        /// </summary>
        private Selector? _monthList;

        /// <summary>
        /// The year selector column.
        /// </summary>
        private Selector? _yearList;

        /// <summary>
        /// The flyout button that commits the pending column selection.
        /// </summary>
        private ButtonBase? _acceptButton;

        /// <summary>
        /// The flyout button that discards the pending column selection.
        /// </summary>
        private ButtonBase? _cancelButton;

        /// <summary>
        /// The grid hosting the three selector columns inside the flyout.
        /// </summary>
        private Grid? _selectorsGrid;

        /// <summary>
        /// The grid hosting the field segments and their dividers.
        /// </summary>
        private Grid? _segmentsHost;

        /// <summary>
        /// The field segment text blocks in template position order.
        /// </summary>
        private System.Windows.Controls.TextBlock?[] _segmentTexts = [];

        /// <summary>
        /// The thin dividers separating the field segments.
        /// </summary>
        private FrameworkElement?[] _segmentDividers = [];

        /// <summary>
        /// The date the flyout columns were populated from; supplies fallback components
        /// for columns without a selection.
        /// </summary>
        private DateTime _flyoutBaseDate;

        /// <summary>
        /// The first year the year column was populated with; maps a year list index back
        /// to its year value.
        /// </summary>
        private int _populatedMinYear;

        /// <summary>
        /// Guards against re-entrancy while the columns are being repopulated.
        /// </summary>
        private bool _suppressColumnSync;

        /// <summary>
        /// Set while <see cref="ClosePopup"/> closes the popup so <see cref="OnPopupClosed"/>
        /// can tell the control's own closes apart from light dismisses.
        /// </summary>
        private bool _popupSelfClosing;

        /// <summary>
        /// The <see cref="Environment.TickCount"/> of the last light dismiss, or
        /// <see langword="null"/> when none has occurred; arms the field-click reopen lockout.
        /// </summary>
        private int? _lastLightDismissTick;

        /// <summary>
        /// How long after a light dismiss a flyout-button click is ignored, covering the
        /// press-then-release span of the field click that caused the dismiss.
        /// </summary>
        private const int LightDismissReopenLockoutMilliseconds = 250;

        /// <summary>
        /// Identifies one of the three date fields the control presents.
        /// </summary>
        private enum DateField
        {
            /// <summary>The day-of-month field.</summary>
            Day = 0,

            /// <summary>The month field.</summary>
            Month = 1,

            /// <summary>The year field.</summary>
            Year = 2,
        }
    }
}
