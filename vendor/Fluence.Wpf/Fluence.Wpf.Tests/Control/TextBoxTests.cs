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
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.TextBox"/> control: PlaceholderText brush, validation line
    /// and HelpText automation behavior.
    /// Authority: WinUI 3 TextBox_themeresources.xaml (TextBoxPlaceholderTextForeground → TextFillColorTertiaryBrush).
    /// </summary>
    public sealed class TextBoxTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        // ---------------------------------------------------------------------------
        // WI-3 C19  TextBox + PasswordBox PlaceholderText brush fix
        // ---------------------------------------------------------------------------

        [Fact]
        public Task TextBox_PlaceholderTextBlock_UsesSecondaryBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Controls.TextBox tb = new() { PlaceholderText = "Search…", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                TextBlock placeholder = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PlaceholderTextBlock"), exactMatch: false);

                // TextControlPlaceholderForeground/PointerOver/Focused all resolve to
                // TextFillColorSecondaryBrush (WinUI CommonStyles TextBox_themeresources.xaml).
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorSecondaryBrush"));

                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(placeholder.Foreground);
                Assert.Equal(
                    expected.Color,
                    actual.Color);
                w.Close();
            });
        }

        [Fact]
        public Task TextBox_PlaceholderTextBlock_Disabled_UsesDisabledBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Controls.TextBox tb = new() { PlaceholderText = "Search…", PlaceholderEnabled = true, IsEnabled = false };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                TextBlock placeholder = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PlaceholderTextBlock"), exactMatch: false);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorDisabledBrush"));

                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(placeholder.Foreground);
                Assert.Equal(
                    expected.Color,
                    actual.Color);
                w.Close();
            });
        }



        [Fact]
        public Task TextBox_PlaceholderTextBlock_ThemeCycle_StillSecondaryBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Controls.TextBox tb = new() { PlaceholderText = "Hint", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                TextBlock placeholder = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PlaceholderTextBlock"), exactMatch: false);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorSecondaryBrush"));

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

                Controls.TextBox tb = new()
                {
                    Width = 240,
                    ValidationState = ValidationState.Error,
                    Text = "Invalid value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border validationLine = Assert.IsType<Border>(FindVisualChildByName<Border>(tb, "PART_ValidationLine"), exactMatch: false);
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
                Controls.TextBox tb = new()
                {
                    Width = 240,
                    HelperText = "Helper text",
                    Text = "Value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                TextBlock helper = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PART_HelperText"), exactMatch: false);
                TextBlock icon = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(tb, "PART_ValidationIcon"), exactMatch: false);

                // The 9 dip gap above the helper row rides on the children rather than the panel,
                // so a field with nothing to say measures exactly the height it draws instead of
                // reserving the gap; see TextBox.xaml for why.
                StackPanel helperRow = Assert.IsType<StackPanel>(VisualTreeHelper.GetParent(helper));
                Assert.Equal(new Thickness(12, 0, 12, 0), helperRow.Margin);
                Assert.Equal(new Thickness(0, 9, 0, 0), helper.Margin);
                Assert.Equal(new Thickness(0, 9, 6, 0), icon.Margin);
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
        /// <c language="csharp">AutomationEventHandler</c> requires an out-of-process UIA client because the
        /// WPF automation event bus does not deliver events back to in-process listeners on
        /// net472 without the COM server running; therefore, this test validates observable
        /// state invariants rather than raw event counts.
        /// </summary>
        [Fact]
        public Task TextBox_ValidationError_HelpText_StableAfterAdditionalKeystrokesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
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

        [Fact]
        public Task TextBox_PlaceholderText_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TextBox textBox = new();
                const string placeholder = "Enter text here...";

                textBox.PlaceholderText = placeholder;

                Assert.Equal(placeholder, textBox.PlaceholderText, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task TextBox_ClearButtonEnabled_DefaultTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TextBox textBox = new();

                Assert.True(textBox.ClearButtonEnabled);
            });
        }

        [Fact]
        public Task TextBox_DefaultChrome_UsesWinUiReferenceValuesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border mainBorder = Assert.IsType<Border>(textBox.Template.FindName("MainBorder", textBox));
                    Button clearButton = Assert.IsType<Button>(textBox.Template.FindName("PART_ClearButton", textBox));

                    Assert.Equal(new Thickness(10, 5, 6, 6), textBox.Padding);
                    Assert.Equal(32.0, textBox.MinHeight);
                    _ = Assert.IsType<LinearGradientBrush>(mainBorder.BorderBrush, exactMatch: false);
                    Assert.Equal(30.0, clearButton.Width, 0.1);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TextBox_FocusState_ShowsAccentLineUnderneathAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                    Text = "Focused",
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    _ = textBox.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border accentLine = Assert.IsType<Border>(textBox.Template.FindName("FocusAccentLine", textBox));

                    Assert.Equal(1.0, accentLine.Opacity);
                    Assert.Equal(2.0, accentLine.Height);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TextBox_TextViewAlignsWithPlaceholder_WhenIconIsShownAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                    PlaceholderText = "With icon",
                    Icon = new Controls.FontIcon
                    {
                        Glyph = "\uE721",
                        IconFontSize = 14,
                    },
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    _ = textBox.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement placeholder = Assert.IsType<FrameworkElement>(textBox.Template.FindName("PlaceholderTextBlock", textBox), exactMatch: false);
                    FrameworkElement textView = Assert.IsType<FrameworkElement>(FindVisualChildByTypeName(textBox, "TextBoxView"), exactMatch: false);

                    double placeholderX = placeholder.TransformToAncestor(window).Transform(new Point(0, 0)).X;
                    double textViewX = textView.TransformToAncestor(window).Transform(new Point(0, 0)).X;

                    // UseLayoutRounding snaps the placeholder and the ScrollViewer content chain to whole
                    // device pixels independently, so at fractional DPI scales (e.g. 175%) the two can land
                    // one device pixel apart. Alignment is therefore asserted to the nearest device pixel.
                    double oneDevicePixelInDips = 1.0 / VisualTreeHelper.GetDpi(textBox).DpiScaleX;
                    Assert.Equal(placeholderX, textViewX, oneDevicePixelInDips + 0.01);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Stage3_TextBox_ValidationState_DefaultNoneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TextBox tb = new();
                Assert.Equal(ValidationState.None, tb.ValidationState);
            });
        }

        [Fact]
        public Task Stage3_TextBox_HelperText_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TextBox tb = new() { HelperText = "Hint" };
                Assert.Equal("Hint", tb.HelperText, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task Stage3_TextBox_CharacterCounter_ShowsWithMaxLengthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.TextBox textBox = new()
                {
                    Width = 260,
                    MaxLength = 40,
                    Text = "Hi",
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock counter = Assert.IsType<TextBlock>(textBox.Template.FindName("PART_CharacterCounter", textBox));
                    Assert.Equal("2/40", counter.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
