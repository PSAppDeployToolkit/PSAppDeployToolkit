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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled password box with reveal toggle support.
    /// </summary>
    [TemplatePart(Name = "PART_PasswordBox", Type = typeof(System.Windows.Controls.PasswordBox))]
    [TemplatePart(Name = "PART_RevealTextBox", Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = "PART_RevealButton", Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = "PART_CapsLockIndicator", Type = typeof(FrameworkElement))]
    public class PasswordBox : Control
    {
        // Precompiled regexes for password strength evaluation.
        private static readonly Regex LowercasePasswordRegex = new("[a-z]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex UppercasePasswordRegex = new("[A-Z]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex DigitPasswordRegex = new("[0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex SymbolPasswordRegex = new("[^a-zA-Z0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Initializes static members of the PasswordBox class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the PasswordBox control uses its custom style by
        /// default. It is called automatically by the .NET runtime before any instances of PasswordBox are created or
        /// any static members are referenced.</remarks>
        static PasswordBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(typeof(PasswordBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordBox"/> class.
        /// </summary>
        public PasswordBox()
        {
            _capsPollTick = OnCapsPollTick;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Identifies the <see cref="Password"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PasswordChar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.Register(
                nameof(PasswordChar),
                typeof(char),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata('\u2022')); // bullet character

        /// <summary>
        /// Gets or sets the masking character for the password.
        /// </summary>
        public char PasswordChar
        {
            get => (char)GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RevealButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RevealButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(RevealButtonEnabled),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the reveal button is enabled.
        /// </summary>
        public bool RevealButtonEnabled
        {
            get => (bool)GetValue(RevealButtonEnabledProperty);
            set => SetValue(RevealButtonEnabledProperty, value);
        }

        private static readonly DependencyPropertyKey IsPasswordRevealedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsPasswordRevealed),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsPasswordRevealed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPasswordRevealedProperty =
            IsPasswordRevealedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the password is currently revealed.
        /// </summary>
        public bool IsPasswordRevealed
        {
            get => (bool)GetValue(IsPasswordRevealedProperty);
            private set => SetValue(IsPasswordRevealedPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text.
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Gets or sets the maximum length of the password.
        /// </summary>
        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowCapsLockIndicator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCapsLockIndicatorProperty =
            DependencyProperty.Register(
                nameof(ShowCapsLockIndicator),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(true, OnChromePropertyChanged));

        /// <summary>
        /// Gets or sets whether the Caps Lock indicator is shown when Caps Lock is active.
        /// </summary>
        public bool ShowCapsLockIndicator
        {
            get => (bool)GetValue(ShowCapsLockIndicatorProperty);
            set => SetValue(ShowCapsLockIndicatorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowPasswordStrength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPasswordStrengthProperty =
            DependencyProperty.Register(
                nameof(ShowPasswordStrength),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(true, OnChromePropertyChanged));

        /// <summary>
        /// Gets or sets whether the password strength meter is displayed.
        /// </summary>
        public bool ShowPasswordStrength
        {
            get => (bool)GetValue(ShowPasswordStrengthProperty);
            set => SetValue(ShowPasswordStrengthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PasswordStrength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PasswordStrengthProperty =
            DependencyProperty.Register(
                nameof(PasswordStrength),
                typeof(int),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Strength score from 0 (weakest) to 4 (strongest). Updated when <see cref="Password"/> changes unless overridden by binding.
        /// </summary>
        public int PasswordStrength
        {
            get => (int)GetValue(PasswordStrengthProperty);
            set => SetValue(PasswordStrengthProperty, value);
        }

        /// <summary>
        /// Selects all text in the password field.
        /// </summary>
        public void SelectAll()
        {
            if (IsPasswordRevealed && _revealTextBox is not null)
            {
                _revealTextBox.SelectAll();
            }
            else
            {
                _passwordBox?.SelectAll();
            }
        }

        private static void OnChromePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox box = (PasswordBox)d;
            box.UpdateCapsLockIndicator();
            box.UpdateStrengthMeter();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_passwordBox is not null)
            {
                _passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;
                _passwordBox.GotKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _passwordBox.LostKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _passwordBox.PreviewKeyDown -= OnInnerPreviewKeyDown;
            }
            if (_revealTextBox is not null)
            {
                _revealTextBox.TextChanged -= OnRevealTextBoxTextChanged;
                _revealTextBox.GotKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _revealTextBox.LostKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _revealTextBox.PreviewKeyDown -= OnInnerPreviewKeyDown;
            }
            if (_revealButton is not null)
            {
                _revealButton.PreviewMouseLeftButtonDown -= OnRevealButtonDown;
                _revealButton.PreviewMouseLeftButtonUp -= OnRevealButtonUp;
                _revealButton.MouseLeave -= OnRevealButtonLeave;
            }
            StopCapsPoll();

            _passwordBox = GetTemplateChild("PART_PasswordBox") as System.Windows.Controls.PasswordBox;
            _revealTextBox = GetTemplateChild("PART_RevealTextBox") as System.Windows.Controls.TextBox;
            _revealButton = GetTemplateChild("PART_RevealButton") as System.Windows.Controls.Button;
            if (_passwordBox is not null)
            {
                _passwordBox.PasswordChanged += OnPasswordBoxPasswordChanged;
                _passwordBox.Password = Password ?? string.Empty;
            }
            if (_revealTextBox is not null)
            {
                _revealTextBox.TextChanged += OnRevealTextBoxTextChanged;
                _revealTextBox.Text = Password ?? string.Empty;
            }
            if (_revealButton is not null)
            {
                _revealButton.PreviewMouseLeftButtonDown += OnRevealButtonDown;
                _revealButton.PreviewMouseLeftButtonUp += OnRevealButtonUp;
                _revealButton.MouseLeave += OnRevealButtonLeave;
            }
            if (_passwordBox is not null)
            {
                _passwordBox.GotKeyboardFocus += OnInnerKeyboardFocusChanged;
                _passwordBox.LostKeyboardFocus += OnInnerKeyboardFocusChanged;
                _passwordBox.PreviewKeyDown += OnInnerPreviewKeyDown;
            }
            if (_revealTextBox is not null)
            {
                _revealTextBox.GotKeyboardFocus += OnInnerKeyboardFocusChanged;
                _revealTextBox.LostKeyboardFocus += OnInnerKeyboardFocusChanged;
                _revealTextBox.PreviewKeyDown += OnInnerPreviewKeyDown;
            }
            UpdatePasswordStrengthFromPassword();
            UpdateCapsLockIndicator();
            UpdateStrengthMeter();
        }

        private void OnCapsPollTick(object? sender, EventArgs e)
        {
            UpdateCapsLockIndicator();
        }

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox control = (PasswordBox)d;
            if (control._isUpdatingPassword)
            {
                return;
            }
            control._isUpdatingPassword = true;
            try
            {
                _ = control._passwordBox?.Password = (string)e.NewValue ?? string.Empty;
                _ = control._revealTextBox?.Text = (string?)e.NewValue ?? string.Empty;
            }
            finally
            {
                control._isUpdatingPassword = false;
            }
            control.UpdatePasswordStrengthFromPassword();
            control.UpdateStrengthMeter();
        }

        private void StartCapsPoll()
        {
            if (_capsPollTimer is not null)
            {
                return;
            }
            _capsPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _capsPollTimer.Tick += _capsPollTick;
            _capsPollTimer.Start();
        }

        private void StopCapsPoll()
        {
            if (_capsPollTimer is null)
            {
                return;
            }
            _capsPollTimer.Tick -= _capsPollTick;
            _capsPollTimer.Stop();
            _capsPollTimer = null;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopCapsPoll();
        }

        private void OnInnerKeyboardFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                UpdateCapsLockIndicator();
                if (IsKeyboardFocusWithin)
                {
                    StartCapsPoll();
                }
                else
                {
                    StopCapsPoll();
                }
            }, DispatcherPriority.Input);
        }

        private void OnInnerPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.CapsLock)
            {
                _ = Dispatcher.BeginInvoke(new Action(UpdateCapsLockIndicator), DispatcherPriority.Input);
            }
        }

        private void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingPassword)
            {
                return;
            }
            _isUpdatingPassword = true;
            try
            {
                Password = _passwordBox?.Password ?? string.Empty;
                _ = _revealTextBox?.Text = _passwordBox?.Password ?? string.Empty;

                UpdatePasswordStrengthFromPassword();
                UpdateStrengthMeter();
            }
            finally
            {
                _isUpdatingPassword = false;
            }
        }

        private void OnRevealTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPassword)
            {
                return;
            }
            _isUpdatingPassword = true;
            try
            {
                Password = _revealTextBox?.Text ?? string.Empty;
                _ = _passwordBox?.Password = _revealTextBox?.Text ?? string.Empty;

                UpdatePasswordStrengthFromPassword();
                UpdateStrengthMeter();
            }
            finally
            {
                _isUpdatingPassword = false;
            }
        }

        private void UpdatePasswordStrengthFromPassword()
        {
            string pwd = Password ?? string.Empty;
            PasswordStrength = ComputePasswordStrength(pwd);
        }

        private static int ComputePasswordStrength(string password)
        {
            if (password.Length == 0)
            {
                return 0;
            }

            int score = 0;
            if (password.Length >= 6)
            {
                score++;
            }
            if (password.Length >= 10)
            {
                score++;
            }
            if (HasLowercasePasswordCharacter(password) && HasUppercasePasswordCharacter(password))
            {
                score++;
            }
            if (HasDigitPasswordCharacter(password))
            {
                score++;
            }
            if (HasSymbolPasswordCharacter(password))
            {
                score++;
            }
            return Math.Min(4, score);
        }

        private static bool HasLowercasePasswordCharacter(string password)
        {
            return LowercasePasswordRegex.IsMatch(password);
        }

        private static bool HasUppercasePasswordCharacter(string password)
        {
            return UppercasePasswordRegex.IsMatch(password);
        }

        private static bool HasDigitPasswordCharacter(string password)
        {
            return DigitPasswordRegex.IsMatch(password);
        }

        private static bool HasSymbolPasswordCharacter(string password)
        {
            return SymbolPasswordRegex.IsMatch(password);
        }

        private void UpdateCapsLockIndicator()
        {
            if (GetTemplateChild("PART_CapsLockIndicator") is not UIElement el)
            {
                return;
            }
            bool capsOn = Keyboard.IsKeyToggled(Key.CapsLock);
            bool show = ShowCapsLockIndicator && IsKeyboardFocusWithin && capsOn;
            el.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateStrengthMeter()
        {
            string brushKey = PasswordStrength <= 1
                ? "SystemFillColorCriticalBrush"
                : PasswordStrength == 2
                ? "SystemFillColorCautionBrush"
                : "SystemFillColorSuccessBrush";

            for (int i = 0; i < 4; i++)
            {
                if (GetTemplateChild("PART_StrengthSegment" + i) is not System.Windows.Controls.Border segment)
                {
                    continue;
                }
                if (!ShowPasswordStrength)
                {
                    segment.Visibility = Visibility.Collapsed;
                    continue;
                }
                segment.Visibility = Visibility.Visible;
                bool active = PasswordStrength > i;
                segment.Opacity = active ? 1.0 : 0.25;
                segment.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, brushKey);
            }
            if (GetTemplateChild("PART_StrengthMeter") is UIElement container)
            {
                container.Visibility = ShowPasswordStrength ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnRevealButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsPasswordRevealed = true;
        }

        private void OnRevealButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsPasswordRevealed = false;
        }

        private void OnRevealButtonLeave(object sender, MouseEventArgs e)
        {
            IsPasswordRevealed = false;
        }

        /// <summary>
        /// Represents a reference to the underlying PasswordBox control used for secure password input.
        /// </summary>
        /// <remarks>This field is intended for internal use to interact with the PasswordBox control in
        /// the user interface. It may be null if the control has not been initialized.</remarks>
        private System.Windows.Controls.PasswordBox? _passwordBox;

        /// <summary>
        /// Represents the underlying TextBox control used for revealing text input.
        /// </summary>
        private System.Windows.Controls.TextBox? _revealTextBox;

        /// <summary>
        /// Represents the button control used to reveal additional content or information.
        /// </summary>
        private System.Windows.Controls.Button? _revealButton;

        /// <summary>
        /// Indicates whether the password is currently being updated programmatically to prevent recursive updates.
        /// </summary>
        private bool _isUpdatingPassword;

        /// <summary>
        /// Represents the timer used to periodically poll the Caps Lock state.
        /// </summary>
        private DispatcherTimer? _capsPollTimer;

        /// <summary>
        /// Represents the event handler invoked on each polling tick for the Caps Lock state.
        /// </summary>
        private readonly EventHandler _capsPollTick;
    }
}
