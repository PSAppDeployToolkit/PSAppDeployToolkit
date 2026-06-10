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
// resolve correctly. See DatePicker.cs for the same pattern.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A control that lets the user pick a time of day from hour, minute, and (in 12-hour
    /// mode) AM/PM selector columns hosted in a light-dismiss flyout, mirroring the WinUI 3
    /// <c>TimePicker</c>. The always-visible field is a button-styled row showing the
    /// selected hour, two-digit minute, and culture AM/PM designator; the flyout commits the
    /// pending column selection through its accept button and discards it through cancel.
    /// </summary>
    [TemplatePart(Name = PART_FlyoutButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_HourList, Type = typeof(Selector))]
    [TemplatePart(Name = PART_MinuteList, Type = typeof(Selector))]
    [TemplatePart(Name = PART_PeriodList, Type = typeof(Selector))]
    [TemplatePart(Name = PART_AcceptButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_CancelButton, Type = typeof(ButtonBase))]
    public class TimePicker : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_FlyoutButton = "PART_FlyoutButton";
        private const string PART_Popup = "PART_Popup";
        private const string PART_HourList = "PART_HourList";
        private const string PART_MinuteList = "PART_MinuteList";
        private const string PART_PeriodList = "PART_PeriodList";
        private const string PART_AcceptButton = "PART_AcceptButton";
        private const string PART_CancelButton = "PART_CancelButton";

        // Optional named template children that render the field segments. A custom
        // template may omit them; every access is null-guarded.
        private const string HourSegmentTextName = "HourSegmentText";
        private const string MinuteSegmentTextName = "MinuteSegmentText";
        private const string PeriodSegmentTextName = "PeriodSegmentText";

        /// <summary>
        /// The <see cref="ClockIdentifier"/> value selecting the 12-hour clock with an
        /// AM/PM designator column.
        /// </summary>
        private const string TwelveHourClock = "12HourClock";

        /// <summary>
        /// The <see cref="ClockIdentifier"/> value selecting the 24-hour clock without a
        /// designator column.
        /// </summary>
        private const string TwentyFourHourClock = "24HourClock";

        /// <summary>
        /// Initializes static members of the TimePicker class and overrides the default
        /// style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TimePicker),
                new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

        /// <summary>
        /// Identifies the <see cref="SelectedTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedTime),
                typeof(TimeSpan?),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedTimeChanged));

        /// <summary>
        /// Gets or sets the currently selected time of day, or <see langword="null"/> when
        /// no time has been picked yet. Changing this property raises
        /// <see cref="SelectedTimeChanged"/>.
        /// </summary>
        public TimeSpan? SelectedTime
        {
            get => (TimeSpan?)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ClockIdentifier"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClockIdentifierProperty =
            DependencyProperty.Register(
                nameof(ClockIdentifier),
                typeof(string),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(
                    GetDefaultClockIdentifier(),
                    OnClockIdentifierChanged,
                    CoerceClockIdentifier));

        /// <summary>
        /// Gets or sets the clock system used by the field and the hour column:
        /// "12HourClock" (hours 1..12 plus an AM/PM designator column) or "24HourClock"
        /// (hours 0..23, no designator column). Any other value is coerced back to
        /// "12HourClock". The default follows the user's regional clock: "24HourClock" when
        /// the current culture's short time pattern uses the 24-hour 'H' specifier, otherwise
        /// "12HourClock". An explicitly set value always wins over the regional default.
        /// </summary>
        public string ClockIdentifier
        {
            get => (string)GetValue(ClockIdentifierProperty);
            set => SetValue(ClockIdentifierProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinuteIncrement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinuteIncrementProperty =
            DependencyProperty.Register(
                nameof(MinuteIncrement),
                typeof(int),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(1, null, CoerceMinuteIncrement));

        /// <summary>
        /// Gets or sets the step between the offered minute values (for example 15 offers
        /// 00, 15, 30, and 45). Values are clamped into 1..59.
        /// </summary>
        public int MinuteIncrement
        {
            get => (int)GetValue(MinuteIncrementProperty);
            set => SetValue(MinuteIncrementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(TimePicker),
                new FrameworkPropertyMetadata(null));

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
                typeof(TimePicker),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text shown in the field while
        /// <see cref="SelectedTime"/> is <see langword="null"/> (for example "Pick a time").
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Occurs after <see cref="SelectedTime"/> changes, whether through the flyout's
        /// accept button or a programmatic update.
        /// </summary>
        public event EventHandler<TimePickerSelectedValueChangedEventArgs>? SelectedTimeChanged;

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            // Namespace-qualified to stay clear of the stock WPF automation peers; see
            // DatePicker.OnCreateAutomationPeer for the same pattern.
            return new Automation.TimePickerAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Unsubscribe-first so re-templating never leaves stale handlers behind.
            // Unlike DatePicker, the columns have no interdependency (no day-length style
            // refresh), so no SelectionChanged handlers are wired.
            _flyoutButton?.Click -= OnFlyoutButtonClick;
            _acceptButton?.Click -= OnAcceptButtonClick;
            _cancelButton?.Click -= OnCancelButtonClick;
            _popup?.Closed -= OnPopupClosed;
            _popupRoot?.PreviewKeyDown -= OnPopupPreviewKeyDown;

            base.OnApplyTemplate();

            _flyoutButton = GetTemplateChild(PART_FlyoutButton) as ButtonBase;
            _popup = GetTemplateChild(PART_Popup) as Popup;
            _hourList = GetTemplateChild(PART_HourList) as Selector;
            _minuteList = GetTemplateChild(PART_MinuteList) as Selector;
            _periodList = GetTemplateChild(PART_PeriodList) as Selector;
            _acceptButton = GetTemplateChild(PART_AcceptButton) as ButtonBase;
            _cancelButton = GetTemplateChild(PART_CancelButton) as ButtonBase;
            _hourSegmentText = GetTemplateChild(HourSegmentTextName) as System.Windows.Controls.TextBlock;
            _minuteSegmentText = GetTemplateChild(MinuteSegmentTextName) as System.Windows.Controls.TextBlock;
            _periodSegmentText = GetTemplateChild(PeriodSegmentTextName) as System.Windows.Controls.TextBlock;

            _flyoutButton?.Click += OnFlyoutButtonClick;
            _acceptButton?.Click += OnAcceptButtonClick;
            _cancelButton?.Click += OnCancelButtonClick;
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

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimePicker picker = (TimePicker)d;
            picker.UpdateFieldText();
            picker.SelectedTimeChanged?.Invoke(
                picker,
                new TimePickerSelectedValueChangedEventArgs(e.OldValue as TimeSpan?, e.NewValue as TimeSpan?));
        }

        private static void OnClockIdentifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The hour display (1..12 vs 0..23) and the designator segment both depend on
            // the clock system; the segment, divider, and column collapse in 24-hour mode
            // is handled by the template's ClockIdentifier trigger.
            ((TimePicker)d).UpdateFieldText();
        }

        /// <summary>
        /// Computes the regional default for <see cref="ClockIdentifier"/>: "24HourClock" when
        /// the current culture's short time pattern uses the 24-hour 'H' specifier, otherwise
        /// "12HourClock". Captured once as the dependency property's default metadata value;
        /// explicitly set values always win.
        /// </summary>
        private static string GetDefaultClockIdentifier()
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H")
                ? TwentyFourHourClock
                : TwelveHourClock;
        }

        /// <summary>
        /// Coerces <see cref="ClockIdentifier"/> to one of its two supported values,
        /// falling back to the 12-hour clock for anything unrecognized.
        /// </summary>
        private static object CoerceClockIdentifier(DependencyObject d, object? baseValue)
        {
            return baseValue is string identifier && string.Equals(identifier, TwentyFourHourClock, StringComparison.Ordinal)
                ? TwentyFourHourClock
                : TwelveHourClock;
        }

        /// <summary>
        /// Clamps <see cref="MinuteIncrement"/> into the supported 1..59 range.
        /// </summary>
        private static object CoerceMinuteIncrement(DependencyObject d, object baseValue)
        {
            return Math.Min(Math.Max((int)baseValue, 1), 59);
        }

        /// <summary>
        /// Maps an hour of day to its 12-hour display value, where both midnight (hour 0)
        /// and noon (hour 12) show as 12.
        /// </summary>
        private static int GetTwelveHourDisplayHour(int hour)
        {
            int displayHour = hour % 12;
            return displayHour == 0 ? 12 : displayHour;
        }

        /// <summary>
        /// Returns the culture's AM designator, falling back to the invariant "AM" when the
        /// culture provides none. On .NET Framework NLS many locales (de-DE, fr-FR, sv-SE,
        /// it-IT) report empty designators, which would otherwise render two blank,
        /// indistinguishable period values.
        /// </summary>
        private static string GetAmDesignator(CultureInfo culture)
        {
            string designator = culture.DateTimeFormat.AMDesignator;
            return string.IsNullOrWhiteSpace(designator) ? "AM" : designator;
        }

        /// <summary>
        /// Returns the culture's PM designator, falling back to the invariant "PM" when the
        /// culture provides none. See <see cref="GetAmDesignator"/> for the .NET Framework
        /// NLS rationale.
        /// </summary>
        private static string GetPmDesignator(CultureInfo culture)
        {
            string designator = culture.DateTimeFormat.PMDesignator;
            return string.IsNullOrWhiteSpace(designator) ? "PM" : designator;
        }

        /// <summary>
        /// Returns the current culture's AM or PM designator for an hour of day, with the
        /// invariant "AM"/"PM" fallback for cultures whose designators are empty.
        /// </summary>
        private static string GetPeriodDesignator(int hour, CultureInfo culture)
        {
            return hour >= 12 ? GetPmDesignator(culture) : GetAmDesignator(culture);
        }

        /// <summary>
        /// Scrolls the selector's current selection into view when it is a ListBox.
        /// </summary>
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
            _popup.SetCurrentValue(Popup.IsOpenProperty, true);

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
        private void OnPopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || _popup is null || !_popup.IsOpen)
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
            if (_popup is null || !_popup.IsOpen)
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
        /// Returns the first visible selector column in display order (hour, minute, AM/PM);
        /// the AM/PM column is collapsed by the template trigger in 24-hour mode.
        /// </summary>
        private Selector? GetFirstVisibleSelectorColumn()
        {
            Selector?[] columns = [_hourList, _minuteList, _periodList];
            foreach (Selector? column in columns)
            {
                if (column is not null && column.Visibility == Visibility.Visible)
                {
                    return column;
                }
            }

            return null;
        }

        /// <summary>
        /// Fills the selector columns from <see cref="SelectedTime"/> (or the current time
        /// of day when unset), snapping the minute down to the nearest
        /// <see cref="MinuteIncrement"/> step, and highlights the matching items. The
        /// columns are plain scrollable lists; WinUI's infinitely looping selectors are a
        /// deliberate v1 omission.
        /// </summary>
        private void PopulateSelectorColumns()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            bool twentyFourHour = IsTwentyFourHourClock;
            int minuteIncrement = MinuteIncrement;
            TimeSpan baseTime = SelectedTime ?? DateTime.Now.TimeOfDay;

            // Normalize out-of-range programmatic values (negative spans, spans past a
            // day) into a valid hour and minute of day before deriving list indices.
            int hour = ((baseTime.Hours % 24) + 24) % 24;
            int minute = ((baseTime.Minutes % 60) + 60) % 60;
            minute -= minute % minuteIncrement;
            _flyoutBaseTime = new TimeSpan(hour, minute, 0);
            _populatedMinuteIncrement = minuteIncrement;
            _populatedTwentyFourHourClock = twentyFourHour;

            if (_hourList is not null)
            {
                List<string> hours = [];
                int firstHour = twentyFourHour ? 0 : 1;
                int lastHour = twentyFourHour ? 23 : 12;
                for (int hourValue = firstHour; hourValue <= lastHour; hourValue++)
                {
                    hours.Add(hourValue.ToString(culture));
                }

                _hourList.ItemsSource = hours;
                _hourList.SelectedIndex = twentyFourHour ? hour : GetTwelveHourDisplayHour(hour) - 1;
            }

            if (_minuteList is not null)
            {
                List<string> minutes = [];
                for (int minuteValue = 0; minuteValue < 60; minuteValue += minuteIncrement)
                {
                    minutes.Add(minuteValue.ToString("00", culture));
                }

                _minuteList.ItemsSource = minutes;
                _minuteList.SelectedIndex = minute / minuteIncrement;
            }

            if (_periodList is not null)
            {
                if (twentyFourHour)
                {
                    // The column is collapsed by the template trigger; clear any stale
                    // 12-hour items so they cannot leak into a later commit.
                    _periodList.ClearValue(ItemsControl.ItemsSourceProperty);
                }
                else
                {
                    List<string> periods = [GetAmDesignator(culture), GetPmDesignator(culture)];
                    _periodList.ItemsSource = periods;
                    _periodList.SelectedIndex = hour >= 12 ? 1 : 0;
                }
            }

            ScrollSelectionIntoView(_hourList);
            ScrollSelectionIntoView(_minuteList);
            ScrollSelectionIntoView(_periodList);
        }

        /// <summary>
        /// Commits the pending hour, minute, and AM/PM column values into
        /// <see cref="SelectedTime"/>.
        /// </summary>
        private void CommitPendingSelection()
        {
            SetCurrentValue(SelectedTimeProperty, new TimeSpan(GetPendingHour(), GetPendingMinute(), 0));
        }

        private void ClosePopup()
        {
            if (_popup is not null && _popup.IsOpen)
            {
                // Closing through the control's own pipeline must not arm the light-dismiss
                // reopen lockout; Popup.Closed is raised synchronously from the set below.
                _popupSelfClosing = true;
                _popup.SetCurrentValue(Popup.IsOpenProperty, false);
            }
        }

        /// <summary>
        /// Returns the pending hour of day (0..23). In 12-hour mode the display hour and
        /// the AM/PM column combine so that 12 AM maps to hour 0 and 12 PM maps to hour 12.
        /// </summary>
        private int GetPendingHour()
        {
            if (_hourList is null || _hourList.SelectedIndex < 0)
            {
                return _flyoutBaseTime.Hours;
            }

            if (_populatedTwentyFourHourClock)
            {
                return _hourList.SelectedIndex;
            }

            int displayHour = _hourList.SelectedIndex + 1;
            return (displayHour % 12) + (GetPendingIsPm() ? 12 : 0);
        }

        private int GetPendingMinute()
        {
            return _minuteList is not null && _minuteList.SelectedIndex >= 0
                ? _minuteList.SelectedIndex * _populatedMinuteIncrement
                : _flyoutBaseTime.Minutes;
        }

        /// <summary>
        /// Returns whether the pending AM/PM column selection is PM, falling back to the
        /// populated base time when the column has no selection.
        /// </summary>
        private bool GetPendingIsPm()
        {
            return _periodList is not null && _periodList.SelectedIndex >= 0
                ? _periodList.SelectedIndex == 1
                : _flyoutBaseTime.Hours >= 12;
        }

        /// <summary>
        /// Writes the selected time into the field segments: the hour per
        /// <see cref="ClockIdentifier"/>, the minute always two-digit, and the culture's
        /// AM/PM designator in 12-hour mode. Out-of-range values (negative spans, spans past
        /// a day) are normalized into a valid hour and minute of day with the same math as
        /// <see cref="PopulateSelectorColumns"/>, so the field can never display an hour such
        /// as "-1". The placeholder swap for a null <see cref="SelectedTime"/> and the
        /// designator collapse in 24-hour mode are handled by template triggers.
        /// </summary>
        private void UpdateFieldText()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            TimeSpan? selectedTime = SelectedTime;
            bool twentyFourHour = IsTwentyFourHourClock;
            int hour = selectedTime.HasValue ? ((selectedTime.Value.Hours % 24) + 24) % 24 : 0;
            int minute = selectedTime.HasValue ? ((selectedTime.Value.Minutes % 60) + 60) % 60 : 0;

            string hourText = !selectedTime.HasValue
                ? string.Empty
                : twentyFourHour
                    ? hour.ToString(culture)
                    : GetTwelveHourDisplayHour(hour).ToString(culture);
            _hourSegmentText?.SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty, hourText);

            string minuteText = selectedTime.HasValue
                ? minute.ToString("00", culture)
                : string.Empty;
            _minuteSegmentText?.SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty, minuteText);

            string periodText = selectedTime.HasValue && !twentyFourHour
                ? GetPeriodDesignator(hour, culture)
                : string.Empty;
            _periodSegmentText?.SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty, periodText);
        }

        /// <summary>
        /// Gets whether <see cref="ClockIdentifier"/> selects the 24-hour clock.
        /// </summary>
        private bool IsTwentyFourHourClock => string.Equals(ClockIdentifier, TwentyFourHourClock, StringComparison.Ordinal);

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
        /// The hour selector column.
        /// </summary>
        private Selector? _hourList;

        /// <summary>
        /// The minute selector column.
        /// </summary>
        private Selector? _minuteList;

        /// <summary>
        /// The AM/PM designator selector column (12-hour clock only).
        /// </summary>
        private Selector? _periodList;

        /// <summary>
        /// The flyout button that commits the pending column selection.
        /// </summary>
        private ButtonBase? _acceptButton;

        /// <summary>
        /// The flyout button that discards the pending column selection.
        /// </summary>
        private ButtonBase? _cancelButton;

        /// <summary>
        /// The field segment showing the hour.
        /// </summary>
        private System.Windows.Controls.TextBlock? _hourSegmentText;

        /// <summary>
        /// The field segment showing the two-digit minute.
        /// </summary>
        private System.Windows.Controls.TextBlock? _minuteSegmentText;

        /// <summary>
        /// The field segment showing the AM/PM designator (12-hour clock only).
        /// </summary>
        private System.Windows.Controls.TextBlock? _periodSegmentText;

        /// <summary>
        /// The time the flyout columns were populated from; supplies fallback components
        /// for columns without a selection.
        /// </summary>
        private TimeSpan _flyoutBaseTime;

        /// <summary>
        /// The minute increment the minute column was populated with; maps a minute list
        /// index back to its minute value.
        /// </summary>
        private int _populatedMinuteIncrement = 1;

        /// <summary>
        /// Whether the hour column was populated for the 24-hour clock; selects the index
        /// mapping used when committing.
        /// </summary>
        private bool _populatedTwentyFourHourClock;

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
    }
}
