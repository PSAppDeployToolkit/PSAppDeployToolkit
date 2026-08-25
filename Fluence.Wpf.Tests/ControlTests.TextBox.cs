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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xunit;

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

        [Fact]
        public Task TextBox_PlaceholderTextBlock_UsesTertiaryBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new() { PlaceholderText = "Search…", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                TextBlock placeholder = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PlaceholderTextBlock"));

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorTertiaryBrush"));

                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(placeholder.Foreground);
                Assert.Equal(
                    expected.Color,
                    actual.Color);
                w.Close();
            });
        }



        [Fact]
        public Task TextBox_PlaceholderTextBlock_ThemeCycle_StillTertiaryBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new() { PlaceholderText = "Hint", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                TextBlock placeholder = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PlaceholderTextBlock"));

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorTertiaryBrush"));

                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(placeholder.Foreground);
                Assert.Equal(
                    expected.Color,
                    actual.Color);
                w.Close();
            });
        }

        [Fact]
        public Task TextBox_ValidationLine_IsHiddenUntilFocusedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationState = ValidationState.Error,
                    Text = "Invalid value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border validationLine = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(tb, "PART_ValidationLine"));
                Assert.Equal(0.0, validationLine.Opacity, 0.001);

                FocusManager.SetFocusedElement(w, tb);
                _ = Keyboard.Focus(tb);
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(1.0, validationLine.Opacity, 0.001);
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorCriticalBrush"));
                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(validationLine.Background);
                Assert.Equal(expected.Color, actual.Color);

                w.Close();
            });
        }

        [Fact]
        public Task TextBox_HelperAndValidationText_UsesNinePixelTopMarginAndCenteredContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    HelperText = "Helper text",
                    Text = "Value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                TextBlock helper = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PART_HelperText"));
                TextBlock icon = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PART_ValidationIcon"));

                StackPanel helperRow = Assert.IsType<StackPanel>(VisualTreeHelper.GetParent(helper));
                Assert.Equal(new Thickness(12, 9, 12, 0), helperRow.Margin);
                Assert.Equal(VerticalAlignment.Center, helper.VerticalAlignment);
                Assert.Equal(VerticalAlignment.Center, icon.VerticalAlignment);

                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // Task 9 -- HelpText a11y: validation message surfaced via AutomationProperties
        // ---------------------------------------------------------------------------

        [Fact]
        public Task TextBox_ValidationError_SetsHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Value is required",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                string helpText = AutomationProperties.GetHelpText(tb);
                Assert.Equal(
                    "Value is required",
                    helpText, StringComparer.Ordinal);

                w.Close();
            });
        }

        [Fact]
        public Task TextBox_ValidationNone_ClearsHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Temp error",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Transition back to None.
                tb.ValidationState = ValidationState.None;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                string helpText = AutomationProperties.GetHelpText(tb);
                Assert.Equal(
                    string.Empty,
                    helpText, StringComparer.Ordinal);

                w.Close();
            });
        }

        [Fact]
        public Task TextBox_ValidationWarning_SetsHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Check the value",
                    ValidationState = ValidationState.Warning,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                string helpText = AutomationProperties.GetHelpText(tb);
                Assert.Equal(
                    "Check the value",
                    helpText, StringComparer.Ordinal);

                w.Close();
            });
        }

        [Fact]
        public Task TextBox_ValidationSuccess_ClearsHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Value is required",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Error state must have set HelpText first (precondition).
                Assert.Equal(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb), StringComparer.Ordinal);

                // Transition to Success -- HelpText must be cleared.
                tb.ValidationState = ValidationState.Success;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(
                    string.Empty,
                    AutomationProperties.GetHelpText(tb), StringComparer.Ordinal);

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
        [Fact]
        public Task TextBox_ValidationError_HelpText_StableAfterAdditionalKeystrokesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationMessage = "Value is required",
                    ValidationState = ValidationState.Error,
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Precondition: HelpText is set after the initial Error transition.
                Assert.Equal(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb), StringComparer.Ordinal);

                // Simulate repeated keystrokes while staying in Error with the same message.
                // Each Text assignment triggers OnTextChanged -> UpdateHelperText without
                // changing ValidationState or ValidationMessage.
                tb.Text = "a";
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                tb.Text = "ab";
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                tb.Text = "abc";
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // HelpText must remain stable -- UpdateHelperText is idempotent for SetHelpText.
                Assert.Equal(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb), StringComparer.Ordinal);

                // Transition to None resets tracked state, then re-entering Error fires fresh.
                tb.ValidationState = ValidationState.None;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(
                    string.Empty,
                    AutomationProperties.GetHelpText(tb), StringComparer.Ordinal);

                tb.ValidationState = ValidationState.Error;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(
                    "Value is required",
                    AutomationProperties.GetHelpText(tb), StringComparer.Ordinal);

                w.Close();
            });
        }
    }
}
