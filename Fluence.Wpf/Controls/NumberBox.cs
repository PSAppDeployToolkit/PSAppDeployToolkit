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
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Fluence.Wpf.Automation;

// IMPORTANT: every reference to RepeatButton / TextBox in this file MUST be
// fully qualified (System.Windows.Controls.Primitives.RepeatButton,
// System.Windows.Controls.TextBox). The Fluence.Wpf.Controls namespace
// defines its own RepeatButton and TextBox subclasses, and because this file
// sits inside that namespace, any unqualified reference resolves to the
// Fluence subclass. The default NumberBox template instantiates the stock
// WPF primitives, so `as RepeatButton` against the Fluence subclass silently
// returns null and the spin-button Click handlers never get attached.
// Using aliases do not work here either - C# enforces CS0576 when an alias
// collides with a namespace member, so fully-qualified names are the only
// option.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A numeric input control with optional spin buttons and min/max clamping.
    /// </summary>
    [TemplatePart(Name = PART_TextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_UpButton, Type = typeof(System.Windows.Controls.Primitives.RepeatButton))]
    [TemplatePart(Name = PART_DownButton, Type = typeof(System.Windows.Controls.Primitives.RepeatButton))]
    public class NumberBox : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_TextBox = "PART_TextBox";
        private const string PART_UpButton = "PART_UpButton";
        private const string PART_DownButton = "PART_DownButton";

        /// <summary>
        /// Initializes static members of the NumberBox class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the NumberBox control uses its custom style by
        /// associating the DefaultStyleKey with the NumberBox type. This enables the control to apply its default
        /// template as defined in themes or resource dictionaries.</remarks>
        static NumberBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NumberBox),
                new FrameworkPropertyMetadata(typeof(NumberBox)));
        }

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValuePropertyChanged,
                    CoerceValueCallback));

        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(double.MinValue, OnMinMaxPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(double.MaxValue, OnMinMaxPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="SmallChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(
                nameof(SmallChange),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(1.0));

        /// <summary>
        /// Identifies the <see cref="LargeChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(
                nameof(LargeChange),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(10.0));

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="SpinButtonPlacementMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SpinButtonPlacementModeProperty =
            DependencyProperty.Register(
                nameof(SpinButtonPlacementMode),
                typeof(NumberBoxSpinButtonPlacementMode),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(NumberBoxSpinButtonPlacementMode.Compact));

        /// <summary>
        /// Identifies the <see cref="AcceptsExpression"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AcceptsExpressionProperty =
            DependencyProperty.Register(
                nameof(AcceptsExpression),
                typeof(bool),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(
                    "0",
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTextPropertyChanged));

        /// <summary>
        /// Occurs when <see cref="Value"/> changes after coercion.
        /// </summary>
        public event EventHandler<NumberBoxValueChangedEventArgs>? ValueChanged;

        /// <summary>
        /// Gets or sets the numeric value.
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum allowed value.
        /// </summary>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum allowed value.
        /// </summary>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// Gets or sets the increment used by spin buttons.
        /// </summary>
        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        /// <summary>
        /// Gets or sets the large increment (reserved for keyboard/page navigation).
        /// </summary>
        public double LargeChange
        {
            get => (double)GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        /// <summary>
        /// Gets or sets an optional header displayed above the input.
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets where spin buttons are shown.
        /// </summary>
        public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
        {
            get => (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty);
            set => SetValue(SpinButtonPlacementModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the control may parse simple expressions (reserved).
        /// </summary>
        public bool AcceptsExpression
        {
            get => (bool)GetValue(AcceptsExpressionProperty);
            set => SetValue(AcceptsExpressionProperty, value);
        }

        /// <summary>
        /// Gets or sets watermark text shown when the text box is empty.
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Gets or sets helper text displayed below the control.
        /// </summary>
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the text representation of the value.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Updates <see cref="Value"/> from <see cref="Text"/> if parsing succeeds, and clears
        /// <see cref="Value"/> to <see cref="double.NaN"/> when the field is empty.
        /// </summary>
        /// <returns><see langword="true"/> if a number was parsed and applied; otherwise <see langword="false"/>.</returns>
        public bool TryParseText()
        {
            string s = Text ?? string.Empty;
            if (AcceptsExpression)
            {
                s = s.Trim();
            }
            // WinUI commits NaN for empty text and reads NaN as "value not set (cleared)"
            // (NumberBox.cpp:120, :488), so an emptied field clears rather than keeping the number
            // it last held and leaving the two out of step. Nothing parsed, so the result is false.
            if (string.IsNullOrWhiteSpace(s))
            {
                Value = double.NaN;
                return false;
            }
            // NumberStyles.Any accepts the culture's NaN symbol. The cleared state is reached by
            // emptying the field, not by typing that symbol, so the symbol is text that did not parse.
            if (!double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out double parsed) || double.IsNaN(parsed))
            {
                return false;
            }
            Value = parsed;
            return true;
        }

        /// <summary>
        /// Raises the <see cref="ValueChanged"/> event.
        /// </summary>
        /// <param name="oldValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnValueChanged(double oldValue, double newValue)
        {
            ValueChanged?.Invoke(this, new NumberBoxValueChangedEventArgs(oldValue, newValue));
            if (UIElementAutomationPeer.FromElement(this) is NumberBoxAutomationPeer peer)
            {
                peer.RaiseValueChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Increments <see cref="Value"/> by <see cref="SmallChange"/> with clamping. A cleared
        /// value has nothing to step from, so the call does nothing.
        /// </summary>
        protected virtual void OnUpClick()
        {
            // WinUI guards its own StepValue the same way (NumberBox.cpp:605): NaN plus SmallChange
            // is NaN, so stepping a cleared box would only produce another cleared box.
            if (double.IsNaN(Value))
            {
                return;
            }
            Value = ClampValue(Value + SmallChange);
        }

        /// <summary>
        /// Decrements <see cref="Value"/> by <see cref="SmallChange"/> with clamping. A cleared
        /// value has nothing to step from, so the call does nothing.
        /// </summary>
        protected virtual void OnDownClick()
        {
            if (double.IsNaN(Value))
            {
                return;
            }
            Value = ClampValue(Value - SmallChange);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NumberBoxAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if ((_partTextBox?.IsKeyboardFocusWithin) is false)
            {
                _ = _partTextBox.Focus();
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if ((_partTextBox?.IsKeyboardFocusWithin) is false)
            {
                _ = _partTextBox.Focus();
            }
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_partTextBox is not null)
            {
                _partTextBox.KeyDown -= OnPartTextBoxKeyDown;
                _partTextBox.LostKeyboardFocus -= OnPartTextBoxLostKeyboardFocus;
            }
            _partUpButton?.Click -= OnPartUpButtonClick;
            _partDownButton?.Click -= OnPartDownButtonClick;
            _partTextBox = GetTemplateChild(PART_TextBox) as System.Windows.Controls.TextBox;
            _partUpButton = GetTemplateChild(PART_UpButton) as System.Windows.Controls.Primitives.RepeatButton;
            _partDownButton = GetTemplateChild(PART_DownButton) as System.Windows.Controls.Primitives.RepeatButton;
            if (_partTextBox is not null)
            {
                _partTextBox.KeyDown += OnPartTextBoxKeyDown;
                _partTextBox.LostKeyboardFocus += OnPartTextBoxLostKeyboardFocus;
            }
            _partUpButton?.Click += OnPartUpButtonClick;
            _partDownButton?.Click += OnPartDownButtonClick;
            UpdateTextFromValue();
            UpdateSpinButtonsEnabled();
        }

        private static object CoerceValueCallback(DependencyObject d, object baseValue)
        {
            // NaN is the cleared state, not out-of-range input, so the bounds do not apply to it.
            // WinUI exempts it from the same coercion: InvalidInputOverwritten only runs for a value
            // that is not NaN (NumberBox.cpp:463), because NaN is how it represents "value not set"
            // (NumberBox.cpp:120). Stepping a cleared box is what has to be guarded instead, and
            // OnUpClick and OnDownClick do that, so a cleared value can no longer strand the field.
            NumberBox box = (NumberBox)d;
            double v = (double)baseValue;
            return double.IsNaN(v) ? v : box.ClampValue(v);
        }

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Value is already clamped by CoerceValueCallback; OldValue/NewValue are both committed values.
            NumberBox box = (NumberBox)d;
            box.OnValueChanged((double)e.OldValue, (double)e.NewValue);
            box.UpdateTextFromValue();
            box.UpdateSpinButtonsEnabled();
        }

        private static void OnMinMaxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Re-coerce Value so it stays within the new bounds.
            d.CoerceValue(ValueProperty);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumberBox box = (NumberBox)d;
            if (box._suppressTextSync)
            {
                return;
            }
            if (box._partTextBox is not null && !string.Equals(box._partTextBox.Text, box.Text, StringComparison.Ordinal))
            {
                box._partTextBox.Text = box.Text ?? string.Empty;
            }
        }

        private void OnPartTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter)
            {
                _ = TryParseText();
                e.Handled = true;
            }
            else if (e.Key is Key.Up)
            {
                _ = TryParseText();
                OnUpClick();
                e.Handled = true;
            }
            else if (e.Key is Key.Down)
            {
                _ = TryParseText();
                OnDownClick();
                e.Handled = true;
            }
        }

        private void OnPartTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _ = TryParseText();
        }

        private void OnPartUpButtonClick(object sender, RoutedEventArgs e)
        {
            OnUpClick();
        }

        private void OnPartDownButtonClick(object sender, RoutedEventArgs e)
        {
            OnDownClick();
        }

        private void UpdateSpinButtonsEnabled()
        {
            // A cleared value cannot be stepped, so the buttons that step it are disabled while it
            // is cleared. WinUI gates its own UpdateSpinButtonEnabled on the same test
            // (NumberBox.cpp:686).
            bool canStep = !double.IsNaN(Value);
            _partUpButton?.SetCurrentValue(IsEnabledProperty, canStep);
            _partDownButton?.SetCurrentValue(IsEnabledProperty, canStep);
        }

        private void UpdateTextFromValue()
        {
            // A cleared value has no number to show, so the field goes empty and the placeholder,
            // if the consumer set one, takes over. WinUI's UpdateTextToValue writes an empty string
            // for the same reason (NumberBox.cpp:640).
            string formatted = double.IsNaN(Value) ? string.Empty : Value.ToString(CultureInfo.CurrentCulture);
            _suppressTextSync = true;
            try
            {
                SetCurrentValue(TextProperty, formatted);
                if (_partTextBox is not null && !string.Equals(_partTextBox.Text, formatted, StringComparison.Ordinal))
                {
                    _partTextBox.Text = formatted;
                }
            }
            finally
            {
                _suppressTextSync = false;
            }
        }

        private double ClampValue(double value)
        {
            // A NaN bound is no bound. Math.Max and Math.Min propagate NaN, so left in place it
            // would have switched clamping off for every value rather than for none.
            double min = double.IsNaN(Minimum) ? double.MinValue : Minimum;
            double max = double.IsNaN(Maximum) ? double.MaxValue : Maximum;
            if (min > max)
            {
                (max, min) = (min, max);
            }
            return Math.Min(Math.Max(value, min), max);
        }

        /// <summary>
        /// Represents the TextBox control associated with this part.
        /// </summary>
        private System.Windows.Controls.TextBox? _partTextBox;

        /// <summary>
        /// Represents the up button part of the control, typically used to increment a value or scroll upward.
        /// </summary>
        /// <remarks>This field is usually assigned by the control template and may be null if the
        /// template does not define an up button part. It is intended for internal use within the control to handle
        /// user interactions related to increasing values or scrolling.</remarks>
        private System.Windows.Controls.Primitives.RepeatButton? _partUpButton;

        /// <summary>
        /// Represents the down button part of the control template, typically used to decrease a value or scroll
        /// downward.
        /// </summary>
        /// <remarks>This field is usually assigned when the control template is applied and may be null
        /// if the template part is not present. It is intended for internal use within custom controls that utilize a
        /// RepeatButton for decrementing actions.</remarks>
        private System.Windows.Controls.Primitives.RepeatButton? _partDownButton;

        /// <summary>
        /// Indicates whether text synchronization events should be suppressed.
        /// </summary>
        /// <remarks>When set to true, changes to the text will not trigger synchronization logic. This is
        /// typically used to prevent recursive or unwanted updates during programmatic changes.</remarks>
        private bool _suppressTextSync;
    }
}
