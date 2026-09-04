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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Provides attached properties that extend the sealed <see cref="System.Windows.Controls.PasswordBox"/>
    /// with the Fluent Design chrome this library ships: a placeholder, a rounded corner radius, a reveal
    /// (peek) button, a Caps Lock indicator, and a password strength meter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="System.Windows.Controls.PasswordBox"/> is sealed, so this library cannot subclass it the way
    /// it subclasses <see cref="System.Windows.Controls.TextBox"/>. Sealing blocks inheritance but not
    /// templating, so the library styles the native control instead. Everything that would otherwise be a
    /// dependency property on a derived type lives here as an attached property. The control a consumer
    /// places in their tree stays a real
    /// <see cref="System.Windows.Controls.PasswordBox"/>: one focus target, one automation element, the
    /// operating system secure store behind <see cref="System.Windows.Controls.PasswordBox.SecurePassword"/>,
    /// and the native clipboard, IME, and context menu policy.
    /// </para>
    /// <para>
    /// The implicit Fluent style sets <see cref="IsFluentDecoratedProperty"/>, which is what attaches the
    /// behavior. An application that wants the native behavior back can clear it.
    /// </para>
    /// </remarks>
    public static class PasswordBoxExtensions
    {
        /// <summary>
        /// The scroll viewer in the Fluent template that hosts the native password text view.
        /// </summary>
        private const string PartContentHost = "PART_ContentHost";

        /// <summary>
        /// The chrome border that carries the background, stroke, and corner radius.
        /// </summary>
        private const string PartMainBorder = "MainBorder";

        /// <summary>
        /// The placeholder shown while the field is empty.
        /// </summary>
        private const string PartPlaceholder = "PlaceholderTextBlock";

        /// <summary>
        /// The button that reveals (peeks at) the password.
        /// </summary>
        private const string PartRevealButton = "PART_RevealButton";

        /// <summary>
        /// The read-only, non-focusable field that shows the plaintext while peeking.
        /// </summary>
        private const string PartRevealDisplay = "PART_RevealDisplay";

        /// <summary>
        /// The Caps Lock warning shown below the field.
        /// </summary>
        private const string PartCapsLockIndicator = "PART_CapsLockIndicator";

        /// <summary>
        /// The container of the strength segments.
        /// </summary>
        private const string PartStrengthMeter = "PART_StrengthMeter";

        /// <summary>
        /// The prefix shared by the strength segment part names.
        /// </summary>
        private const string PartStrengthSegmentPrefix = "PART_StrengthSegment";

        /// <summary>
        /// The number of segments in the strength meter, matching the 0 to 4 score range.
        /// </summary>
        private const int StrengthSegmentCount = 4;

        /// <summary>
        /// How often the Caps Lock state is sampled while the field has keyboard focus.
        /// </summary>
        private const double CapsPollIntervalMilliseconds = 300;

        /// <summary>
        /// The accessible name of the reveal button while the password is hidden.
        /// </summary>
        private const string ShowPasswordAutomationName = "Show password";

        /// <summary>
        /// The accessible name of the reveal button while the password is revealed.
        /// </summary>
        private const string HidePasswordAutomationName = "Hide password";

        #region IsFluentDecorated

        /// <summary>
        /// Identifies the IsFluentDecorated attached property.
        /// </summary>
        public static readonly DependencyProperty IsFluentDecoratedProperty =
            DependencyProperty.RegisterAttached(
                "IsFluentDecorated",
                typeof(bool),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(defaultValue: false, OnIsFluentDecoratedChanged));

        /// <summary>
        /// Gets whether the Fluent password box behavior is attached to the specified element.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns><see langword="true"/> when the behavior is attached; otherwise <see langword="false"/>.</returns>
        public static bool GetIsFluentDecorated(this System.Windows.Controls.PasswordBox obj)
        {
            return (bool)obj.GetValue(IsFluentDecoratedProperty);
        }

        /// <summary>
        /// Sets whether the Fluent password box behavior is attached to the specified element. The implicit
        /// Fluent style sets this to <see langword="true"/>; clearing it restores the native behavior.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <param name="value"><see langword="true"/> to attach the behavior; otherwise <see langword="false"/>.</param>
        public static void SetIsFluentDecorated(this System.Windows.Controls.PasswordBox obj, bool value)
        {
            obj.SetValue(IsFluentDecoratedProperty, value);
        }

        #endregion IsFluentDecorated

        #region PlaceholderText

        /// <summary>
        /// Identifies the PlaceholderText attached property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(string.Empty, OnChromeChanged));

        /// <summary>
        /// Gets the placeholder text shown while the field is empty.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns>The placeholder text.</returns>
        public static string GetPlaceholderText(this System.Windows.Controls.PasswordBox obj)
        {
            return (string)obj.GetValue(PlaceholderTextProperty);
        }

        /// <summary>
        /// Sets the placeholder text shown while the field is empty.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <param name="value">The placeholder text to store.</param>
        public static void SetPlaceholderText(this System.Windows.Controls.PasswordBox obj, string value)
        {
            obj.SetValue(PlaceholderTextProperty, value);
        }

        #endregion PlaceholderText

        #region CornerRadius

        /// <summary>
        /// Identifies the CornerRadius attached property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(new CornerRadius(4), OnChromeChanged));

        /// <summary>
        /// Gets the corner radius of the password box chrome.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns>The corner radius.</returns>
        public static CornerRadius GetCornerRadius(this System.Windows.Controls.PasswordBox obj)
        {
            return (CornerRadius)obj.GetValue(CornerRadiusProperty);
        }

        /// <summary>
        /// Sets the corner radius of the password box chrome.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <param name="value">The corner radius to apply.</param>
        public static void SetCornerRadius(this System.Windows.Controls.PasswordBox obj, CornerRadius value)
        {
            obj.SetValue(CornerRadiusProperty, value);
        }

        #endregion CornerRadius

        #region RevealButtonEnabled

        /// <summary>
        /// Identifies the RevealButtonEnabled attached property.
        /// </summary>
        public static readonly DependencyProperty RevealButtonEnabledProperty =
            DependencyProperty.RegisterAttached(
                "RevealButtonEnabled",
                typeof(bool),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(defaultValue: true, OnChromeChanged));

        /// <summary>
        /// Gets whether the reveal button is offered once the field holds a password.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns><see langword="true"/> when the reveal button is enabled; otherwise <see langword="false"/>.</returns>
        public static bool GetRevealButtonEnabled(this System.Windows.Controls.PasswordBox obj)
        {
            return (bool)obj.GetValue(RevealButtonEnabledProperty);
        }

        /// <summary>
        /// Sets whether the reveal button is offered once the field holds a password.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <param name="value"><see langword="true"/> to enable the reveal button; otherwise <see langword="false"/>.</param>
        public static void SetRevealButtonEnabled(this System.Windows.Controls.PasswordBox obj, bool value)
        {
            obj.SetValue(RevealButtonEnabledProperty, value);
        }

        #endregion RevealButtonEnabled

        #region ShowCapsLockIndicator

        /// <summary>
        /// Identifies the ShowCapsLockIndicator attached property.
        /// </summary>
        public static readonly DependencyProperty ShowCapsLockIndicatorProperty =
            DependencyProperty.RegisterAttached(
                "ShowCapsLockIndicator",
                typeof(bool),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(defaultValue: false, OnChromeChanged));

        /// <summary>
        /// Gets whether the Caps Lock indicator is shown while Caps Lock is active. Opt in per box.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns><see langword="true"/> when the indicator is enabled; otherwise <see langword="false"/>.</returns>
        public static bool GetShowCapsLockIndicator(this System.Windows.Controls.PasswordBox obj)
        {
            return (bool)obj.GetValue(ShowCapsLockIndicatorProperty);
        }

        /// <summary>
        /// Sets whether the Caps Lock indicator is shown while Caps Lock is active.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <param name="value"><see langword="true"/> to enable the indicator; otherwise <see langword="false"/>.</param>
        public static void SetShowCapsLockIndicator(this System.Windows.Controls.PasswordBox obj, bool value)
        {
            obj.SetValue(ShowCapsLockIndicatorProperty, value);
        }

        #endregion ShowCapsLockIndicator

        #region ShowPasswordStrength

        /// <summary>
        /// Identifies the ShowPasswordStrength attached property.
        /// </summary>
        public static readonly DependencyProperty ShowPasswordStrengthProperty =
            DependencyProperty.RegisterAttached(
                "ShowPasswordStrength",
                typeof(bool),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(defaultValue: false, OnChromeChanged));

        /// <summary>
        /// Gets whether the password strength meter is displayed. Opt in per box.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns><see langword="true"/> when the meter is shown; otherwise <see langword="false"/>.</returns>
        public static bool GetShowPasswordStrength(this System.Windows.Controls.PasswordBox obj)
        {
            return (bool)obj.GetValue(ShowPasswordStrengthProperty);
        }

        /// <summary>
        /// Sets whether the password strength meter is displayed.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <param name="value"><see langword="true"/> to show the meter; otherwise <see langword="false"/>.</param>
        public static void SetShowPasswordStrength(this System.Windows.Controls.PasswordBox obj, bool value)
        {
            obj.SetValue(ShowPasswordStrengthProperty, value);
        }

        #endregion ShowPasswordStrength

        #region PasswordStrength

        /// <summary>
        /// Identifies the PasswordStrength attached property.
        /// </summary>
        public static readonly DependencyProperty PasswordStrengthProperty =
            DependencyProperty.RegisterAttached(
                "PasswordStrength",
                typeof(int),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(0, OnChromeChanged));

        /// <summary>
        /// Gets the strength score from 0 (weakest) to 4 (strongest). The behavior recomputes it whenever the
        /// password changes; a caller may overwrite it to score the password itself.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns>The strength score.</returns>
        public static int GetPasswordStrength(this System.Windows.Controls.PasswordBox obj)
        {
            return (int)obj.GetValue(PasswordStrengthProperty);
        }

        /// <summary>
        /// Sets the strength score from 0 (weakest) to 4 (strongest).
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <param name="value">The strength score to store.</param>
        public static void SetPasswordStrength(this System.Windows.Controls.PasswordBox obj, int value)
        {
            obj.SetValue(PasswordStrengthProperty, value);
        }

        #endregion PasswordStrength

        #region IsPasswordRevealed

        private static readonly DependencyPropertyKey IsPasswordRevealedPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsPasswordRevealed",
                typeof(bool),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(defaultValue: false, OnIsPasswordRevealedChanged));

        /// <summary>
        /// Identifies the read-only IsPasswordRevealed attached property.
        /// </summary>
        public static readonly DependencyProperty IsPasswordRevealedProperty =
            IsPasswordRevealedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the password is currently revealed (peeked at).
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns><see langword="true"/> while the password is revealed; otherwise <see langword="false"/>.</returns>
        public static bool GetIsPasswordRevealed(this System.Windows.Controls.PasswordBox obj)
        {
            return (bool)obj.GetValue(IsPasswordRevealedProperty);
        }

        #endregion IsPasswordRevealed

        #region HasPassword

        private static readonly DependencyPropertyKey HasPasswordPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "HasPassword",
                typeof(bool),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the read-only HasPassword attached property.
        /// </summary>
        public static readonly DependencyProperty HasPasswordProperty =
            HasPasswordPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the field currently holds a password. The Fluent template uses this to hide the
        /// placeholder and the reveal button, because the native
        /// <see cref="System.Windows.Controls.PasswordBox.Password"/> is not a dependency property and so
        /// cannot be observed by a trigger.
        /// </summary>
        /// <param name="obj">The password box to read from or write to.</param>
        /// <returns><see langword="true"/> when the field holds a password; otherwise <see langword="false"/>.</returns>
        public static bool GetHasPassword(this System.Windows.Controls.PasswordBox obj)
        {
            return (bool)obj.GetValue(HasPasswordProperty);
        }

        #endregion HasPassword

        #region Behavior plumbing

        /// <summary>
        /// Holds the per-instance behavior object. A static extension class cannot carry per-control state,
        /// so the state travels with the control in its own property store.
        /// </summary>
        private static readonly DependencyProperty BehaviorProperty =
            DependencyProperty.RegisterAttached(
                "Behavior",
                typeof(PasswordBoxBehavior),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(defaultValue: null));

        private static void OnIsFluentDecoratedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not System.Windows.Controls.PasswordBox passwordBox)
            {
                return;
            }

            if (d.GetValue(BehaviorProperty) is PasswordBoxBehavior existing)
            {
                existing.Detach();
                d.ClearValue(BehaviorProperty);
            }

            if (e.NewValue is true)
            {
                PasswordBoxBehavior behavior = new(passwordBox);
                d.SetValue(BehaviorProperty, behavior);
                behavior.Attach();
            }
        }

        private static void OnChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d.GetValue(BehaviorProperty) as PasswordBoxBehavior)?.UpdateChrome();
        }

        private static void OnIsPasswordRevealedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Every path that flips the revealed state lands here, mouse press-and-hold and keyboard
            // activation alike, so the overlay and the button's accessible name never drift apart.
            (d.GetValue(BehaviorProperty) as PasswordBoxBehavior)?.OnRevealStateChanged();
        }

        /// <summary>
        /// Owns everything the Fluent chrome needs for one <see cref="System.Windows.Controls.PasswordBox"/>:
        /// the template parts, the Caps Lock poll timer, and the reveal gesture state.
        /// </summary>
        private sealed class PasswordBoxBehavior
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PasswordBoxBehavior"/> class.
            /// </summary>
            /// <param name="owner">The password box this behavior decorates.</param>
            internal PasswordBoxBehavior(System.Windows.Controls.PasswordBox owner)
            {
                _owner = owner;
                _strengthSegments = new System.Windows.Controls.Border?[StrengthSegmentCount];
            }

            /// <summary>
            /// Subscribes to the owner's lifetime, password, and focus events and paints the initial chrome.
            /// </summary>
            internal void Attach()
            {
                _owner.Loaded += OnLoaded;
                _owner.Unloaded += OnUnloaded;
                _owner.PasswordChanged += OnPasswordChanged;
                _owner.GotKeyboardFocus += OnKeyboardFocusChanged;
                _owner.LostKeyboardFocus += OnKeyboardFocusChanged;
                _owner.PreviewKeyDown += OnPreviewKeyDown;
                Refresh();
            }

            /// <summary>
            /// Unsubscribes everything and stops the Caps Lock timer, so a box that is undecorated or
            /// unloaded leaves nothing running.
            /// </summary>
            internal void Detach()
            {
                _owner.Loaded -= OnLoaded;
                _owner.Unloaded -= OnUnloaded;
                _owner.PasswordChanged -= OnPasswordChanged;
                _owner.GotKeyboardFocus -= OnKeyboardFocusChanged;
                _owner.LostKeyboardFocus -= OnKeyboardFocusChanged;
                _owner.PreviewKeyDown -= OnPreviewKeyDown;
                UnhookRevealButton();
                StopCapsPoll();
            }

            /// <summary>
            /// Repaints every piece of chrome from the current state.
            /// </summary>
            internal void Refresh()
            {
                EnsureParts();
                UpdateHasPassword();
                UpdateStrengthScore();
                UpdateChromeCore();
                UpdateRevealDisplay();
                UpdateRevealButtonAccessibleName();
            }

            /// <summary>
            /// Refreshes the Caps Lock indicator and the strength meter.
            /// </summary>
            internal void UpdateChrome()
            {
                EnsureParts();
                UpdateChromeCore();
            }

            /// <summary>
            /// Writes every part the Fluent template cannot drive with a trigger. Password is not a
            /// dependency property on the native control, and a prefixed attached-property binding path does
            /// not resolve at runtime from a BAML theme dictionary, so this method writes them.
            /// </summary>
            private void UpdateChromeCore()
            {
                UpdateCornerRadius();
                UpdatePlaceholder();
                UpdateRevealButtonVisibility();
                UpdateCapsLockIndicator();
                UpdateStrengthMeter();
            }

            private void UpdateCornerRadius()
            {
                _ = _mainBorder?.CornerRadius = _owner.GetCornerRadius();
            }

            private void UpdatePlaceholder()
            {
                if (_placeholder is null)
                {
                    return;
                }
                _placeholder.Text = _owner.GetPlaceholderText();
                _placeholder.Visibility = _owner.Password.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            }

            private void UpdateRevealButtonVisibility()
            {
                if (_revealButton is null)
                {
                    return;
                }
                bool show = _owner.GetRevealButtonEnabled() && _owner.Password.Length > 0;
                _revealButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }

            /// <summary>
            /// Reacts to the revealed state flipping in either direction.
            /// </summary>
            internal void OnRevealStateChanged()
            {
                UpdateRevealDisplay();
                UpdateRevealButtonAccessibleName();
            }

            // Precompiled regexes for password strength evaluation.
            private static readonly Regex LowercasePasswordRegex = new("[a-z]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
            private static readonly Regex UppercasePasswordRegex = new("[A-Z]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
            private static readonly Regex DigitPasswordRegex = new("[0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
            private static readonly Regex SymbolPasswordRegex = new("[^a-zA-Z0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

            private static int ComputePasswordStrength(string password)
            {
                if (password.Length is 0)
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
                if (LowercasePasswordRegex.IsMatch(password) && UppercasePasswordRegex.IsMatch(password))
                {
                    score++;
                }
                if (DigitPasswordRegex.IsMatch(password))
                {
                    score++;
                }
                if (SymbolPasswordRegex.IsMatch(password))
                {
                    score++;
                }
                return Math.Min(4, score);
            }

            private void OnLoaded(object sender, RoutedEventArgs e)
            {
                Refresh();
            }

            private void OnUnloaded(object sender, RoutedEventArgs e)
            {
                StopCapsPoll();
            }

            private void OnPasswordChanged(object sender, RoutedEventArgs e)
            {
                EnsureParts();
                UpdateHasPassword();
                UpdateStrengthScore();
                UpdateChromeCore();
                UpdateRevealDisplay();
            }

            private void OnKeyboardFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
            {
                _ = _owner.Dispatcher.BeginInvoke(new Action(OnFocusSettled), DispatcherPriority.Input);
            }

            private void OnFocusSettled()
            {
                UpdateCapsLockIndicator();
                if (_owner.IsKeyboardFocusWithin)
                {
                    StartCapsPoll();
                }
                else
                {
                    StopCapsPoll();
                }
            }

            private void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key is Key.CapsLock)
                {
                    _ = _owner.Dispatcher.BeginInvoke(new Action(UpdateCapsLockIndicator), DispatcherPriority.Input);
                }
            }

            private void OnCapsPollTick(object? sender, EventArgs e)
            {
                UpdateCapsLockIndicator();
            }

            private void StartCapsPoll()
            {
                if (_capsPollTimer is not null)
                {
                    return;
                }
                _capsPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(CapsPollIntervalMilliseconds) };
                _capsPollTimer.Tick += OnCapsPollTick;
                _capsPollTimer.Start();
            }

            private void StopCapsPoll()
            {
                if (_capsPollTimer is null)
                {
                    return;
                }
                _capsPollTimer.Tick -= OnCapsPollTick;
                _capsPollTimer.Stop();
                _capsPollTimer = null;
            }

            /// <summary>
            /// Resolves the template parts once per applied template. Checks the visual child count rather
            /// than catching, because <see cref="FrameworkTemplate.FindName(string, FrameworkElement)"/>
            /// throws when the template has not been applied yet.
            /// </summary>
            private void EnsureParts()
            {
                if (_owner.Template is null || ReferenceEquals(_resolvedTemplate, _owner.Template))
                {
                    return;
                }
                if (VisualTreeHelper.GetChildrenCount(_owner) is 0 && !_owner.ApplyTemplate())
                {
                    return;
                }

                UnhookRevealButton();
                _resolvedTemplate = _owner.Template;
                _mainBorder = _owner.Template.FindName(PartMainBorder, _owner) as System.Windows.Controls.Border;
                _placeholder = _owner.Template.FindName(PartPlaceholder, _owner) as System.Windows.Controls.TextBlock;
                _contentHost = _owner.Template.FindName(PartContentHost, _owner) as System.Windows.Controls.ScrollViewer;
                _revealDisplay = _owner.Template.FindName(PartRevealDisplay, _owner) as System.Windows.Controls.TextBox;
                _revealButton = _owner.Template.FindName(PartRevealButton, _owner) as System.Windows.Controls.Button;
                _capsLockIndicator = _owner.Template.FindName(PartCapsLockIndicator, _owner) as UIElement;
                _strengthMeter = _owner.Template.FindName(PartStrengthMeter, _owner) as UIElement;
                for (int i = 0; i < StrengthSegmentCount; i++)
                {
                    _strengthSegments[i] = _owner.Template.FindName(
                        PartStrengthSegmentPrefix + i.ToString(CultureInfo.InvariantCulture),
                        _owner) as System.Windows.Controls.Border;
                }
                HookRevealButton();
            }

            private void HookRevealButton()
            {
                if (_revealButton is null)
                {
                    return;
                }
                _revealButton.Focusable = true;
                _revealButton.IsTabStop = true;
                _revealButton.PreviewMouseLeftButtonDown += OnRevealButtonDown;
                _revealButton.PreviewMouseLeftButtonUp += OnRevealButtonUp;
                _revealButton.MouseLeave += OnRevealButtonLeave;
                _revealButton.Click += OnRevealButtonClick;
                UpdateRevealButtonAccessibleName();
            }

            private void UnhookRevealButton()
            {
                if (_revealButton is null)
                {
                    return;
                }
                _revealButton.PreviewMouseLeftButtonDown -= OnRevealButtonDown;
                _revealButton.PreviewMouseLeftButtonUp -= OnRevealButtonUp;
                _revealButton.MouseLeave -= OnRevealButtonLeave;
                _revealButton.Click -= OnRevealButtonClick;
            }

            private void OnRevealButtonDown(object sender, MouseButtonEventArgs e)
            {
                _isMouseRevealActive = true;
                SetRevealed(revealed: true);
            }

            private void OnRevealButtonUp(object sender, MouseButtonEventArgs e)
            {
                // Do not reset _isMouseRevealActive here; Click fires after Up and the
                // Click handler reads the flag to determine whether to toggle. Leave and
                // Click are the two paths that reset the flag.
                SetRevealed(revealed: false);
            }

            private void OnRevealButtonLeave(object sender, MouseEventArgs e)
            {
                _isMouseRevealActive = false;
                SetRevealed(revealed: false);
            }

            private void OnRevealButtonClick(object sender, RoutedEventArgs e)
            {
                if (_isMouseRevealActive)
                {
                    // Click fired as part of a mouse press-and-release cycle; the password
                    // was already hidden by OnRevealButtonUp, so just clear the flag.
                    _isMouseRevealActive = false;
                }
                else
                {
                    // No mouse gesture active: Space or Enter keyboard activation.
                    SetRevealed(!_owner.GetIsPasswordRevealed());
                }
            }

            private void SetRevealed(bool revealed)
            {
                _owner.SetValue(IsPasswordRevealedPropertyKey, revealed);
            }

            private void UpdateHasPassword()
            {
                _owner.SetValue(HasPasswordPropertyKey, _owner.Password.Length > 0);
            }

            private void UpdateStrengthScore()
            {
                _owner.SetPasswordStrength(ComputePasswordStrength(_owner.Password));
            }

            /// <summary>
            /// Fills or clears the peek overlay. The plaintext only exists in the visual tree while the
            /// password is actually revealed; hiding it wipes the overlay immediately.
            /// </summary>
            private void UpdateRevealDisplay()
            {
                EnsureParts();
                bool revealed = _owner.GetIsPasswordRevealed();

                // Fade the secure text view instead of collapsing it: the caret, the selection, and hit
                // testing stay live behind the overlay, so typing continues into the real control.
                _ = _contentHost?.Opacity = revealed ? 0.0 : 1.0;
                _ = _revealDisplay?.Visibility = revealed ? Visibility.Visible : Visibility.Collapsed;

                if (_revealDisplay is null)
                {
                    return;
                }
                if (revealed)
                {
                    _revealDisplay.Text = _owner.Password;
                    if (_contentHost is not null)
                    {
                        // Keep the revealed text aligned with the scrolled position of the masked text so a
                        // password longer than the field does not jump when it is peeked at.
                        _revealDisplay.ScrollToHorizontalOffset(_contentHost.HorizontalOffset);
                    }
                }
                else if (_revealDisplay.Text.Length > 0)
                {
                    _revealDisplay.Clear();
                }
            }

            private void UpdateRevealButtonAccessibleName()
            {
                if (_revealButton is not null)
                {
                    AutomationProperties.SetName(
                        _revealButton,
                        _owner.GetIsPasswordRevealed() ? HidePasswordAutomationName : ShowPasswordAutomationName);
                }
            }

            private void UpdateCapsLockIndicator()
            {
                if (_capsLockIndicator is null)
                {
                    return;
                }
                bool capsOn = Keyboard.IsKeyToggled(Key.CapsLock);
                bool show = _owner.GetShowCapsLockIndicator() && _owner.IsKeyboardFocusWithin && capsOn;
                _capsLockIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }

            private void UpdateStrengthMeter()
            {
                bool showMeter = _owner.GetShowPasswordStrength();
                int strength = _owner.GetPasswordStrength();
                string brushKey = strength <= 1
                    ? "SystemFillColorCriticalBrush"
                    : strength is 2
                    ? "SystemFillColorCautionBrush"
                    : "SystemFillColorSuccessBrush";

                for (int i = 0; i < StrengthSegmentCount; i++)
                {
                    System.Windows.Controls.Border? segment = _strengthSegments[i];
                    if (segment is null)
                    {
                        continue;
                    }
                    if (!showMeter)
                    {
                        segment.Visibility = Visibility.Collapsed;
                        continue;
                    }
                    segment.Visibility = Visibility.Visible;
                    segment.Opacity = strength > i ? 1.0 : 0.25;
                    segment.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, brushKey);
                }
                _ = _strengthMeter?.Visibility = showMeter ? Visibility.Visible : Visibility.Collapsed;
            }

            /// <summary>
            /// The decorated password box.
            /// </summary>
            private readonly System.Windows.Controls.PasswordBox _owner;

            /// <summary>
            /// The strength meter segments, indexed by score threshold.
            /// </summary>
            private readonly System.Windows.Controls.Border?[] _strengthSegments;

            /// <summary>
            /// The template the cached parts were resolved from, used to detect retemplating.
            /// </summary>
            private System.Windows.Controls.ControlTemplate? _resolvedTemplate;

            /// <summary>
            /// The chrome border carrying the corner radius.
            /// </summary>
            private System.Windows.Controls.Border? _mainBorder;

            /// <summary>
            /// The placeholder shown while the field is empty.
            /// </summary>
            private System.Windows.Controls.TextBlock? _placeholder;

            /// <summary>
            /// The scroll viewer hosting the native password text view.
            /// </summary>
            private System.Windows.Controls.ScrollViewer? _contentHost;

            /// <summary>
            /// The read-only overlay that shows the plaintext while peeking.
            /// </summary>
            private System.Windows.Controls.TextBox? _revealDisplay;

            /// <summary>
            /// The reveal button.
            /// </summary>
            private System.Windows.Controls.Button? _revealButton;

            /// <summary>
            /// The Caps Lock warning element.
            /// </summary>
            private UIElement? _capsLockIndicator;

            /// <summary>
            /// The container of the strength segments.
            /// </summary>
            private UIElement? _strengthMeter;

            /// <summary>
            /// Samples the Caps Lock state while the field has keyboard focus.
            /// </summary>
            private DispatcherTimer? _capsPollTimer;

            /// <summary>
            /// Tracks whether a mouse press-and-hold is currently active on the reveal button. Prevents the
            /// keyboard-toggle Click handler from interfering with the mouse reveal gesture.
            /// </summary>
            private bool _isMouseRevealActive;
        }

        #endregion Behavior plumbing
    }
}
