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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Covers the Fluent chrome that <see cref="Controls.PasswordBoxExtensions"/> puts on the sealed
    /// <see cref="PasswordBox"/>. The control under test is the native WPF password box. The library styles
    /// and decorates it rather than subclassing it, because the type is sealed.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public Task PasswordBox_ImplicitFluentStyle_AppliesToNativeControlAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window);

                    Assert.True(Controls.PasswordBoxExtensions.GetIsFluentDecorated(box),
                        "The implicit Fluent style must attach the password box behavior.");
                    _ = Assert.IsType<ScrollViewer>(box.Template.FindName("PART_ContentHost", box));
                    Assert.True(box.Focusable,
                        "The native password box must stay focusable; the old wrapper pushed focus onto an inner field.");
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_AutomationPeer_ReportsPasswordEditAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window, "secret");

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(box);
                    _ = Assert.IsType<PasswordBoxAutomationPeer>(peer, exactMatch: false);
                    Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
                    Assert.True(peer.IsPassword(),
                        "PasswordBox peer must report IsPassword=true so Narrator suppresses reading the value aloud.");
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_AccessibleName_IsCarriedByTheControlItselfAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = new() { Width = 200 };
                    AutomationProperties.SetName(box, "Enter your password");
                    _ = ShowPasswordBox(window, box);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(box);

                    // The focusable element and the named element are now the same object, so there is
                    // nothing to forward: the caller's prompt is what a screen reader announces.
                    Assert.Equal("Enter your password", peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_Placeholder_TracksHasPasswordAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = new() { Width = 200 };
                    Controls.PasswordBoxExtensions.SetPlaceholderText(box, "Password");
                    _ = ShowPasswordBox(window, box);

                    TextBlock placeholder = Assert.IsType<TextBlock>(box.Template.FindName("PlaceholderTextBlock", box));

                    Assert.False(Controls.PasswordBoxExtensions.GetHasPassword(box));
                    Assert.Equal(Visibility.Visible, placeholder.Visibility);
                    Assert.Equal("Password", placeholder.Text, StringComparer.Ordinal);

                    box.Password = "secret";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(Controls.PasswordBoxExtensions.GetHasPassword(box),
                        "Password is not a dependency property, so HasPassword is what the template triggers observe.");
                    Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_PlaceholderTextBlock_UsesTertiaryBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = new() { Width = 200 };
                    Controls.PasswordBoxExtensions.SetPlaceholderText(box, "Password");
                    _ = ShowPasswordBox(window, box);

                    TextBlock placeholder = Assert.IsType<TextBlock>(box.Template.FindName("PlaceholderTextBlock", box));
                    SolidColorBrush expected = Assert.IsType<SolidColorBrush>(application.TryFindResource("TextFillColorTertiaryBrush"));
                    SolidColorBrush actual = Assert.IsType<SolidColorBrush>(placeholder.Foreground);

                    Assert.Equal(expected.Color, actual.Color);
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_AttachedDefaults_MatchTheRetiredControlAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                PasswordBox box = new();

                Assert.True(Controls.PasswordBoxExtensions.GetRevealButtonEnabled(box), "The reveal button must stay enabled by default.");
                Assert.False(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box));
                Assert.False(Controls.PasswordBoxExtensions.GetShowCapsLockIndicator(box), "Caps Lock indicator must be opt-in by default.");
                Assert.False(Controls.PasswordBoxExtensions.GetShowPasswordStrength(box), "Password strength meter must be opt-in by default.");
                Assert.Equal(new CornerRadius(4), Controls.PasswordBoxExtensions.GetCornerRadius(box));

                Controls.PasswordBoxExtensions.SetShowCapsLockIndicator(box, value: true);
                Controls.PasswordBoxExtensions.SetShowPasswordStrength(box, value: true);
                Assert.True(Controls.PasswordBoxExtensions.GetShowCapsLockIndicator(box));
                Assert.True(Controls.PasswordBoxExtensions.GetShowPasswordStrength(box));
            });
        }

        [Fact]
        public Task PasswordBox_RevealButton_IsKeyboardOperableAndNamedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window, "secret");
                    Button revealButton = FindRevealButton(box);

                    string accessibleName = AutomationProperties.GetName(revealButton);
                    Assert.False(string.IsNullOrWhiteSpace(accessibleName),
                        "PART_RevealButton must have a non-empty AutomationProperties.Name for screen readers.");
                    Assert.True(revealButton.Focusable,
                        "PART_RevealButton must be focusable so keyboard users can Tab to it.");

                    // Invoking via IInvokeProvider simulates Space/Enter keyboard activation.
                    IInvokeProvider invoke = GetInvokeProvider(revealButton);
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box),
                        "Invoking PART_RevealButton (keyboard Space/Enter path) must reveal the password.");

                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box),
                        "A second invocation of PART_RevealButton must hide the password again.");
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_RevealButton_MousePressAndHold_IsTransientAsync()
        {
            // Regression test: a mouse press-and-release must NOT leave the password revealed.
            // Contract: press-and-hold = transient reveal; release = hide immediately.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window, "secret");
                    Button revealButton = FindRevealButton(box);

                    RaiseMouseButton(revealButton, UIElement.PreviewMouseLeftButtonDownEvent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box),
                        "Password must be revealed while the mouse button is held down (press-and-hold).");
                    Assert.Equal("Hide password", AutomationProperties.GetName(revealButton), StringComparer.Ordinal);

                    RaiseMouseButton(revealButton, UIElement.PreviewMouseLeftButtonUpEvent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box),
                        "Password must be hidden immediately after mouse button is released.");
                    Assert.Equal("Show password", AutomationProperties.GetName(revealButton), StringComparer.Ordinal);

                    // WPF fires Click after MouseLeftButtonUp completes; the Click handler must not toggle
                    // the state back on for a gesture that was a press-and-release.
                    revealButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, revealButton));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box),
                        "Password must stay hidden after Click fires following a mouse press-and-release.");

                    // Verify the gesture left no stale state: keyboard invocation must still toggle correctly.
                    IInvokeProvider invoke = GetInvokeProvider(revealButton);
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box),
                        "Keyboard invocation after a mouse press-and-release must be able to reveal the password.");
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(Controls.PasswordBoxExtensions.GetIsPasswordRevealed(box),
                        "Second keyboard invocation must hide the password again.");
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_PeekOverlay_IsInertAndClearedWhenHiddenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window, "CorrectHorse7!");
                    TextBox overlay = Assert.IsType<TextBox>(box.Template.FindName("PART_RevealDisplay", box));

                    Assert.False(overlay.Focusable, "The peek overlay must never take focus away from the password box.");
                    Assert.False(overlay.IsTabStop, "The peek overlay must never be a tab stop.");
                    Assert.True(overlay.IsReadOnly, "The peek overlay must not be editable.");
                    Assert.Equal(Visibility.Collapsed, overlay.Visibility);
                    Assert.Equal(string.Empty, overlay.Text, StringComparer.Ordinal);

                    IInvokeProvider invoke = GetInvokeProvider(FindRevealButton(box));
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Visible, overlay.Visibility);
                    Assert.Equal("CorrectHorse7!", overlay.Text, StringComparer.Ordinal);

                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, overlay.Visibility);
                    Assert.Equal(string.Empty, overlay.Text, StringComparer.Ordinal);
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_Unloaded_StopsCapsLockPollingTimerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window);
                    object behavior = GetPasswordBoxBehavior(box);

                    MethodInfo startCapsPoll = Assert.IsType<MethodInfo>(
                        behavior.GetType().GetMethod("StartCapsPoll", BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
                    FieldInfo capsPollTimer = Assert.IsType<FieldInfo>(
                        behavior.GetType().GetField("_capsPollTimer", BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);

                    _ = startCapsPoll.Invoke(behavior, parameters: null);
                    DispatcherTimer timer = Assert.IsType<DispatcherTimer>(capsPollTimer.GetValue(behavior));
                    Assert.True(timer.IsEnabled, "The Caps Lock poll timer must be running once started.");

                    box.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, box));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Null(capsPollTimer.GetValue(behavior));
                    Assert.False(timer.IsEnabled, "Unloading the box must stop the Caps Lock poll timer.");
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_StrengthMeter_ScoresAndOptsInAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window);
                    StackPanel meter = Assert.IsType<StackPanel>(box.Template.FindName("PART_StrengthMeter", box));

                    Assert.Equal(0, Controls.PasswordBoxExtensions.GetPasswordStrength(box));
                    Assert.Equal(Visibility.Collapsed, meter.Visibility);

                    box.Password = "Aa1!aaaaaa";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(Controls.PasswordBoxExtensions.GetPasswordStrength(box) >= 3,
                        "A long password mixing cases, a digit, and a symbol must score at least 3.");

                    box.Password = "a";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(0, Controls.PasswordBoxExtensions.GetPasswordStrength(box));

                    Controls.PasswordBoxExtensions.SetShowPasswordStrength(box, value: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Visible, meter.Visibility);
                    Border firstSegment = Assert.IsType<Border>(box.Template.FindName("PART_StrengthSegment0", box));
                    Assert.Equal(Visibility.Visible, firstSegment.Visibility);
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_DefaultChrome_UsesWinUiReferenceValuesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window, "secret", width: 260);

                    Border mainBorder = Assert.IsType<Border>(box.Template.FindName("MainBorder", box));
                    Button revealButton = FindRevealButton(box);

                    Assert.Equal(new Thickness(10, 5, 6, 6), box.Padding);
                    Assert.Equal(32.0, box.MinHeight);
                    Assert.Equal(new CornerRadius(4), mainBorder.CornerRadius);
                    _ = Assert.IsType<LinearGradientBrush>(mainBorder.BorderBrush, exactMatch: false);
                    Assert.Equal(30.0, revealButton.Width, 0.1);
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_FocusState_ShowsAccentLineUnderneathAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window, "Focused", width: 260);
                    Border accentLine = Assert.IsType<Border>(box.Template.FindName("FocusAccentLine", box));

                    _ = box.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(box.IsKeyboardFocusWithin,
                        "Focus must land on the password box itself, not on an inner field.");
                    Assert.Equal(1.0, accentLine.Opacity);
                    Assert.Equal(2.0, accentLine.Height);
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        [Fact]
        public Task PasswordBox_SecureStore_StaysAuthoritativeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    PasswordBox box = ShowPasswordBox(window);

                    Assert.Equal(0, box.SecurePassword.Length);
                    Assert.False(Controls.PasswordBoxExtensions.GetHasPassword(box));

                    box.Password = "CorrectHorse7!";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // The chrome reads the native store; it never becomes the store itself.
                    Assert.Equal("CorrectHorse7!".Length, box.SecurePassword.Length);
                    Assert.True(Controls.PasswordBoxExtensions.GetHasPassword(box));

                    box.Clear();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(0, box.SecurePassword.Length);
                    Assert.False(Controls.PasswordBoxExtensions.GetHasPassword(box));
                    Assert.Equal(0, Controls.PasswordBoxExtensions.GetPasswordStrength(box));
                }
                finally
                {
                    ClosePasswordBoxTest(window, application, genericDictionary);
                }
            });
        }

        private static PasswordBox ShowPasswordBox(Window window, string password = "", double width = 200)
        {
            PasswordBox box = new()
            {
                Width = width,
                Password = password,
            };
            return ShowPasswordBox(window, box);
        }

        private static PasswordBox ShowPasswordBox(Window window, PasswordBox box)
        {
            window.Content = box;
            window.Width = 320;
            window.Height = 140;
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            _ = box.ApplyTemplate();
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            return box;
        }

        private static void ClosePasswordBoxTest(Window window, Application application, ResourceDictionary? genericDictionary)
        {
            CloseWindowAndDrain(window);
            if (genericDictionary is not null)
            {
                _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
            }
        }

        private static Button FindRevealButton(PasswordBox box)
        {
            return Assert.IsType<Button>(box.Template.FindName("PART_RevealButton", box));
        }

        private static IInvokeProvider GetInvokeProvider(Button button)
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
            return (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
        }

        private static void RaiseMouseButton(Button button, RoutedEvent routedEvent)
        {
            MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = routedEvent,
                Source = button,
            };
            button.RaiseEvent(args);
        }

        // Reaches the private behavior object the extensions class parks on the decorated box, so the
        // Caps Lock timer lifetime can be asserted the way it was on the retired wrapper control.
        private static object GetPasswordBoxBehavior(PasswordBox box)
        {
            FieldInfo behaviorField = Assert.IsType<FieldInfo>(
                typeof(Controls.PasswordBoxExtensions).GetField("BehaviorProperty", BindingFlags.Static | BindingFlags.NonPublic), exactMatch: false);
            DependencyProperty behaviorProperty = Assert.IsType<DependencyProperty>(behaviorField.GetValue(null));
            return box.GetValue(behaviorProperty)
                ?? throw new Xunit.Sdk.XunitException("The Fluent style must attach a password box behavior.");
        }
    }
}
