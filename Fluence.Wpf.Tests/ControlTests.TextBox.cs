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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FluencePasswordBox = Fluence.Wpf.Controls.PasswordBox;
using FluenceTextBox = Fluence.Wpf.Controls.TextBox;
using WpfBorder = System.Windows.Controls.Border;
using WpfTextBlock = System.Windows.Controls.TextBlock;

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

                FluenceTextBox tb = new() { PlaceholderText = "Search…", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? placeholder = FindVisualChildByName<WpfTextBlock>(tb, "PlaceholderTextBlock");
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

                FluencePasswordBox pb = new() { PlaceholderText = "Password" };
                Window w = new() { Content = pb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? placeholder = FindVisualChildByName<WpfTextBlock>(pb, "PlaceholderTextBlock");
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

                FluencePasswordBox pb = new() { PlaceholderText = "Password" };
                Window w = new() { Content = pb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                MethodInfo? startCapsPoll = typeof(FluencePasswordBox).GetMethod(
                    "StartCapsPoll",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo? capsPollTimer = typeof(FluencePasswordBox).GetField(
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

                FluenceTextBox tb = new() { PlaceholderText = "Hint", PlaceholderEnabled = true };
                Window w = new() { Content = tb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? placeholder = FindVisualChildByName<WpfTextBlock>(tb, "PlaceholderTextBlock");
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

                FluenceTextBox tb = new()
                {
                    Width = 240,
                    ValidationState = ValidationState.Error,
                    Text = "Invalid value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? validationLine = FindVisualChildByName<WpfBorder>(tb, "PART_ValidationLine");
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
        public void TextBox_HelperAndValidationText_UsesSevenPixelTopMarginAndCenteredContent()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceTextBox tb = new()
                {
                    Width = 240,
                    HelperText = "Helper text",
                    Text = "Value",
                };
                Window w = new() { Content = tb, Width = 320, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfTextBlock? helper = FindVisualChildByName<WpfTextBlock>(tb, "PART_HelperText");
                Assert.IsNotNull(helper, "TextBox template must expose PART_HelperText.");
                WpfTextBlock? icon = FindVisualChildByName<WpfTextBlock>(tb, "PART_ValidationIcon");
                Assert.IsNotNull(icon, "TextBox template must expose PART_ValidationIcon.");

                StackPanel? helperRow = VisualTreeHelper.GetParent(helper) as StackPanel;
                Assert.IsNotNull(helperRow, "Helper text should be hosted in the validation/helper row.");
                Assert.AreEqual(new Thickness(12, 7, 12, 0), helperRow.Margin,
                    "Helper and validation text should sit 7px below the input chrome.");
                Assert.AreEqual(VerticalAlignment.Center, helper.VerticalAlignment,
                    "Helper text should be vertically centered with the validation icon.");
                Assert.AreEqual(VerticalAlignment.Center, icon.VerticalAlignment,
                    "Validation icon should be vertically centered with helper text.");

                w.Close();
            });
        }
    }
}
