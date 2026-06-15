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

using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled text box with placeholder, clear button, and icon support.
    /// </summary>
    [TemplatePart(Name = PART_ContentHost, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PART_ClearButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_CharacterCounter, Type = typeof(System.Windows.Controls.TextBlock))]
    [TemplatePart(Name = PART_ValidationLine, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_ValidationIcon, Type = typeof(System.Windows.Controls.TextBlock))]
    [TemplatePart(Name = PART_HelperText, Type = typeof(System.Windows.Controls.TextBlock))]
    public class TextBox : System.Windows.Controls.TextBox
    {
        // Template part names.
        private const string PART_ContentHost = "PART_ContentHost";
        private const string PART_ClearButton = "PART_ClearButton";
        private const string PART_CharacterCounter = "PART_CharacterCounter";
        private const string PART_ValidationLine = "PART_ValidationLine";
        private const string PART_ValidationIcon = "PART_ValidationIcon";
        private const string PART_HelperText = "PART_HelperText";

        /// <summary>
        /// Initializes static members of the TextBox class and overrides the default style metadata for the control.
        /// </summary>
        /// <remarks>This static constructor ensures that the TextBox control uses its own style by
        /// default, rather than inheriting the style from its base class. This is important for proper theming and
        /// appearance in WPF applications.</remarks>
        static TextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TextBox),
                new FrameworkPropertyMetadata(typeof(TextBox)));
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(TextBox),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text displayed when the text box is empty.
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderEnabledProperty =
            DependencyProperty.Register(
                nameof(PlaceholderEnabled),
                typeof(bool),
                typeof(TextBox),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets whether the placeholder text is enabled.
        /// </summary>
        public bool PlaceholderEnabled
        {
            get => (bool)GetValue(PlaceholderEnabledProperty);
            set => SetValue(PlaceholderEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(TextBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the icon displayed in the text box.
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.Register(
                nameof(IconPlacement),
                typeof(ElementPlacement),
                typeof(TextBox),
                new FrameworkPropertyMetadata(ElementPlacement.Left));

        /// <summary>
        /// Gets or sets the placement of the icon relative to the text.
        /// </summary>
        public ElementPlacement IconPlacement
        {
            get => (ElementPlacement)GetValue(IconPlacementProperty);
            set => SetValue(IconPlacementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ClearButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(ClearButtonEnabled),
                typeof(bool),
                typeof(TextBox),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets whether the clear button is shown when the text box has content and focus.
        /// </summary>
        public bool ClearButtonEnabled
        {
            get => (bool)GetValue(ClearButtonEnabledProperty);
            set => SetValue(ClearButtonEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(TextBox),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the text box.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HelperText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HelperTextProperty =
            DependencyProperty.Register(
                nameof(HelperText),
                typeof(string),
                typeof(TextBox),
                new FrameworkPropertyMetadata(string.Empty, OnChromePropertyChanged));

        /// <summary>
        /// Gets or sets the helper text displayed below the text box.
        /// </summary>
        public string HelperText
        {
            get => (string)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ValidationMessage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationMessageProperty =
            DependencyProperty.Register(
                nameof(ValidationMessage),
                typeof(string),
                typeof(TextBox),
                new FrameworkPropertyMetadata(string.Empty, OnChromePropertyChanged));

        /// <summary>
        /// Gets or sets the validation message displayed when a validation state is active.
        /// </summary>
        public string ValidationMessage
        {
            get => (string)GetValue(ValidationMessageProperty);
            set => SetValue(ValidationMessageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ValidationState"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationStateProperty =
            DependencyProperty.Register(
                nameof(ValidationState),
                typeof(ValidationState),
                typeof(TextBox),
                new FrameworkPropertyMetadata(ValidationState.None, OnChromePropertyChanged));

        /// <summary>
        /// Gets or sets the current validation state of the text box.
        /// </summary>
        public ValidationState ValidationState
        {
            get => (ValidationState)GetValue(ValidationStateProperty);
            set => SetValue(ValidationStateProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBox"/> class and wires text-changed handling.
        /// </summary>
        public TextBox()
        {
            TextChanged += OnTextChanged;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == MaxLengthProperty)
            {
                UpdateCharacterCounter();
            }
        }

        private static void OnChromePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox box = (TextBox)d;
            box.UpdateHelperText();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCounter();
            UpdateHelperText();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _clearButton?.Click -= OnClearButtonClick;
            _clearButton = GetTemplateChild(PART_ClearButton) as System.Windows.Controls.Button;
            _clearButton?.Click += OnClearButtonClick;
            UpdateCharacterCounter();
            UpdateHelperText();
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            Clear();
            _ = Focus();
        }

        private void UpdateCharacterCounter()
        {
            if (GetTemplateChild(PART_CharacterCounter) is not System.Windows.Controls.TextBlock counter)
            {
                return;
            }
            if (MaxLength <= 0)
            {
                counter.Visibility = Visibility.Collapsed;
                return;
            }
            counter.Visibility = Visibility.Visible;
            counter.Text = string.Format(CultureInfo.CurrentCulture, "{0}/{1}", Text?.Length ?? 0, MaxLength);
        }

        private void UpdateHelperText()
        {
            System.Windows.Controls.TextBlock? icon = GetTemplateChild(PART_ValidationIcon) as System.Windows.Controls.TextBlock;
            if (GetTemplateChild(PART_HelperText) is not System.Windows.Controls.TextBlock helper)
            {
                return;
            }

            if (ValidationState != ValidationState.None)
            {
                string message = !string.IsNullOrWhiteSpace(ValidationMessage) ? ValidationMessage : HelperText;
                helper.Text = message;
                helper.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
                if (icon is not null)
                {
                    icon.Visibility = helper.Visibility;
                    switch (ValidationState)
                    {
                        case ValidationState.Success:
                            icon.Text = "\uE73E";
                            icon.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SystemFillColorSuccessBrush");
                            break;
                        case ValidationState.Warning:
                            icon.Text = "\uE7BA";
                            icon.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SystemFillColorCautionBrush");
                            break;
                        case ValidationState.Error:
                            icon.Text = "\uE783";
                            icon.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SystemFillColorCriticalBrush");
                            break;
                        case ValidationState.None:
                            break;
                        default:
                            icon.Visibility = Visibility.Collapsed;
                            break;
                    }
                }
                switch (ValidationState)
                {
                    case ValidationState.Success:
                        helper.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SystemFillColorSuccessBrush");
                        break;
                    case ValidationState.Warning:
                        helper.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SystemFillColorCautionBrush");
                        break;
                    case ValidationState.Error:
                        helper.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SystemFillColorCriticalBrush");
                        break;
                    case ValidationState.None:
                        break;
                    default:
                        return;
                }
                return;
            }
            _ = icon?.Visibility = Visibility.Collapsed;
            helper.Text = HelperText;
            helper.Visibility = string.IsNullOrWhiteSpace(HelperText) ? Visibility.Collapsed : Visibility.Visible;
            helper.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        }

        /// <summary>
        /// Represents a reference to the clear button control.
        /// </summary>
        private System.Windows.Controls.Button? _clearButton;
    }
}
