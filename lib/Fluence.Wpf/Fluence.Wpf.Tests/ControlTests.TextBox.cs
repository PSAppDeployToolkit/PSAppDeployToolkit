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
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 C19 tests: TextBox PlaceholderText uses TextFillColorTertiaryBrush;
    /// PasswordBox PlaceholderText uses TextFillColorTertiaryBrush.
    /// Authority: WinUI 3 TextBox_themeresources.xaml (TextBoxPlaceholderTextForeground → TextFillColorTertiaryBrush).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 C19  TextBox + PasswordBox PlaceholderText brush fix
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void TextBox_PlaceholderTextBlock_UsesTertiaryBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new() { PlaceholderText = "Search…", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TextBlock? placeholder = FindVisualChildByName<TextBlock>(tb, "PlaceholderTextBlock");
                Assert.IsNotNull(placeholder, "PlaceholderTextBlock must be present in TextBox template.");

                SolidColorBrush? expected = app?.TryFindResource("TextFillColorTertiaryBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "TextFillColorTertiaryBrush resource must resolve.");

                SolidColorBrush? actual = placeholder.Foreground as SolidColorBrush;
                Assert.IsNotNull(actual, "PlaceholderTextBlock.Foreground must be a SolidColorBrush.");
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "TextBox PlaceholderTextBlock.Foreground must be TextFillColorTertiaryBrush per WI-3 C19.");
                w.Close();
            });
        }

        [TestMethod]
        public void PasswordBox_PlaceholderTextBlock_UsesTertiaryBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.PasswordBox pb = new() { PlaceholderText = "Password" };
                Window w = new() { Content = pb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TextBlock? placeholder = FindVisualChildByName<TextBlock>(pb, "PlaceholderTextBlock");
                Assert.IsNotNull(placeholder, "PlaceholderTextBlock must be present in PasswordBox template.");

                SolidColorBrush? expected = app?.TryFindResource("TextFillColorTertiaryBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "TextFillColorTertiaryBrush resource must resolve.");

                SolidColorBrush? actual = placeholder.Foreground as SolidColorBrush;
                Assert.IsNotNull(actual, "PlaceholderTextBlock.Foreground must be a SolidColorBrush.");
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "PasswordBox PlaceholderTextBlock.Foreground must be TextFillColorTertiaryBrush per WI-3 C19.");
                w.Close();
            });
        }

        [TestMethod]
        public void PasswordBox_Unloaded_StopsCapsLockPollingTimer()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.PasswordBox pb = new() { PlaceholderText = "Password" };
                Window w = new() { Content = pb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                MethodInfo? startCapsPoll = typeof(Controls.PasswordBox).GetMethod(
                    "StartCapsPoll",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo? capsPollTimer = typeof(Controls.PasswordBox).GetField(
                    "_capsPollTimer",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(startCapsPoll, "PasswordBox must expose the expected internal caps-poll start method.");
                Assert.IsNotNull(capsPollTimer, "PasswordBox must keep the expected caps-poll timer field.");

                _ = startCapsPoll.Invoke(pb, parameters: null);
                Assert.IsNotNull(capsPollTimer.GetValue(pb), "The caps-poll timer should be active after polling starts.");

                pb.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, pb));
                DrainDispatcher(w.Dispatcher);

                Assert.IsNull(capsPollTimer.GetValue(pb), "PasswordBox must stop caps-polling when unloaded.");
                w.Close();
            });
        }

        [TestMethod]
        public void TextBox_PlaceholderTextBlock_ThemeCycle_StillTertiaryBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new() { PlaceholderText = "Hint", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                TextBlock? placeholder = FindVisualChildByName<TextBlock>(tb, "PlaceholderTextBlock");
                Assert.IsNotNull(placeholder, "PlaceholderTextBlock must remain present after theme cycle.");

                SolidColorBrush? expected = app?.TryFindResource("TextFillColorTertiaryBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "TextFillColorTertiaryBrush must resolve after theme cycle.");

                SolidColorBrush? actual = placeholder.Foreground as SolidColorBrush;
                Assert.IsNotNull(actual);
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "PlaceholderTextBlock.Foreground must track TextFillColorTertiaryBrush after theme cycle.");
                w.Close();
            });
        }

        [TestMethod]
        public void TextBox_ValidationLine_IsHiddenUntilFocused()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationState = ValidationState.Error,
                    Text = "Invalid value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Border? validationLine = FindVisualChildByName<Border>(tb, "PART_ValidationLine");
                Assert.IsNotNull(validationLine, "TextBox template must expose PART_ValidationLine.");
                Assert.AreEqual(0.0, validationLine.Opacity, 0.001, "Validation underline should be hidden before focus.");

                FocusManager.SetFocusedElement(w, tb);
                _ = Keyboard.Focus(tb);
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(1.0, validationLine.Opacity, 0.001, "Validation underline should appear while focused.");
                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorCriticalBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "SystemFillColorCriticalBrush must resolve.");
                SolidColorBrush? actual = validationLine.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "Validation underline should use a brush background.");
                Assert.AreEqual(expected.Color, actual.Color, "Error validation underline should use the critical brush.");

                w.Close();
            });
        }

        [TestMethod]
        public void TextBox_HelperAndValidationText_UsesNinePixelTopMarginAndCenteredContent()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    HelperText = "Helper text",
                    Text = "Value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TextBlock? helper = FindVisualChildByName<TextBlock>(tb, "PART_HelperText");
                Assert.IsNotNull(helper, "TextBox template must expose PART_HelperText.");
                TextBlock? icon = FindVisualChildByName<TextBlock>(tb, "PART_ValidationIcon");
                Assert.IsNotNull(icon, "TextBox template must expose PART_ValidationIcon.");

                StackPanel? helperRow = VisualTreeHelper.GetParent(helper) as StackPanel;
                Assert.IsNotNull(helperRow, "Helper text should be hosted in the validation/helper row.");
                Assert.AreEqual(new Thickness(12, 9, 12, 0), helperRow.Margin,
                    "Helper and validation text should sit 9px below the input chrome.");
                Assert.AreEqual(VerticalAlignment.Center, helper.VerticalAlignment,
                    "Helper text should be vertically centered with the validation icon.");
                Assert.AreEqual(VerticalAlignment.Center, icon.VerticalAlignment,
                    "Validation icon should be vertically centered with helper text.");

                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // Task 9 -- HelpText a11y: validation message surfaced via AutomationProperties
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void TextBox_ValidationError_SetsHelpText()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Value is required",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                string helpText = AutomationProperties.GetHelpText(tb);
                Assert.AreEqual(
                    "Value is required",
                    helpText,
                    "AutomationProperties.HelpText must equal ValidationMessage when ValidationState is Error.");

                w.Close();
            });
        }

        [TestMethod]
        public void TextBox_ValidationNone_ClearsHelpText()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Temp error",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Transition back to None.
                tb.ValidationState = ValidationState.None;
                DrainDispatcher(w.Dispatcher);

                string helpText = AutomationProperties.GetHelpText(tb);
                Assert.AreEqual(
                    string.Empty,
                    helpText,
                    "AutomationProperties.HelpText must be cleared when ValidationState returns to None.");

                w.Close();
            });
        }

        [TestMethod]
        public void TextBox_ValidationWarning_SetsHelpText()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Check the value",
                    ValidationState = ValidationState.Warning,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                string helpText = AutomationProperties.GetHelpText(tb);
                Assert.AreEqual(
                    "Check the value",
                    helpText,
                    "AutomationProperties.HelpText must equal ValidationMessage when ValidationState is Warning.");

                w.Close();
            });
        }

        [TestMethod]
        public void TextBox_ValidationSuccess_ClearsHelpText()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Value is required",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Error state must have set HelpText first (precondition).
                Assert.AreEqual(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb),
                    "Precondition: Error state must set HelpText.");

                // Transition to Success -- HelpText must be cleared.
                tb.ValidationState = ValidationState.Success;
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(
                    string.Empty,
                    AutomationProperties.GetHelpText(tb),
                    "AutomationProperties.HelpText must be cleared when ValidationState transitions to Success.");

                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // Announce-gating: ShouldAnnounce tracks last announced state+message
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Verifies that typing additional characters while the control remains in Error state
        /// with the same ValidationMessage does not reset the tracked announce state (i.e. the
        /// gating fields remain stable). Asserted indirectly by confirming HelpText stays
        /// consistent (the idempotent path) and that the control compiles and functions with
        /// the gating fields present. A reliable in-process event-frequency count via
        /// <c>AutomationEventHandler</c> requires an out-of-process UIA client because the
        /// WPF automation event bus does not deliver events back to in-process listeners on
        /// net472 without the COM server running; therefore, this test validates observable
        /// state invariants rather than raw event counts.
        /// </summary>
        [TestMethod]
        public void TextBox_ValidationError_HelpText_StableAfterAdditionalKeystrokes()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Value is required",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Precondition: HelpText is set after the initial Error transition.
                Assert.AreEqual(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb),
                    "Precondition: HelpText must be set after entering Error state.");

                // Simulate repeated keystrokes while staying in Error with the same message.
                // Each Text assignment triggers OnTextChanged -> UpdateHelperText without
                // changing ValidationState or ValidationMessage.
                tb.Text = "a";
                DrainDispatcher(w.Dispatcher);
                tb.Text = "ab";
                DrainDispatcher(w.Dispatcher);
                tb.Text = "abc";
                DrainDispatcher(w.Dispatcher);

                // HelpText must remain stable -- UpdateHelperText is idempotent for SetHelpText.
                Assert.AreEqual(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb),
                    "HelpText must remain stable while ValidationState and ValidationMessage are unchanged.");

                // Transition to None resets tracked state, then re-entering Error fires fresh.
                tb.ValidationState = ValidationState.None;
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(
                    string.Empty,
                    AutomationProperties.GetHelpText(tb),
                    "HelpText must be cleared after transitioning to None.");

                tb.ValidationState = ValidationState.Error;
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb),
                    "HelpText must be set again after re-entering Error state following a None reset.");

                w.Close();
            });
        }
    }
}
