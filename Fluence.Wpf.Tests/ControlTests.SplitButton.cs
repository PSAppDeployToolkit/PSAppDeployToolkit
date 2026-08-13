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

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B17 tests: SplitButton Appearance property + accent divider stroke.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B17  SplitButton accent divider stroke
        // ---------------------------------------------------------------------------

        [Fact]
        public void SplitButton_AppearanceProperty_DefaultIsStandard()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                SplitButton btn = new();
                Assert.Equal(
                    ControlAppearance.Standard,
                    btn.Appearance);
            });
        }

        [Fact]
        public void SplitButton_AppearanceProperty_CanBeSetToAccent()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                SplitButton btn = new() { Appearance = ControlAppearance.Accent, Content = "Go" };
                Window w = new() { Content = btn, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.Equal(
                    ControlAppearance.Accent,
                    btn.Appearance);
                w.Close();
            });
        }

        [Fact]
        public void SplitButton_DividerRectangle_PresentInTemplate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                SplitButton btn = new() { Content = "Test" };
                Window w = new() { Content = btn, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Rectangle? divider = FindVisualChildByName<Rectangle>(btn, "Divider");
                Assert.NotNull(divider);
                Assert.NotNull(divider.Fill);
                w.Close();
            });
        }

        [Fact]
        public void SplitButton_FocusVisuals_UseKeyboardOnlyFocusVisualStyle()
        {
            // The per-half focus rings previously lived in the template behind
            // IsKeyboardFocused triggers, which mouse clicks also satisfy, so the rings
            // rendered on click. Each half now carries the DefaultControlFocusVisualStyle
            // adorner instead, which WPF shows only for keyboard navigation (Tab),
            // matching DropDownButton.
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                SplitButton button = new()
                {
                    Content = "Send",
                    Width = 160,
                };
                Window window = new() { Content = button, Width = 260, Height = 120 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = button.ApplyTemplate();
                    System.Windows.Controls.Button? primaryButton = button.Template.FindName("PART_PrimaryButton", button) as System.Windows.Controls.Button;
                    System.Windows.Controls.Primitives.ToggleButton? secondaryButton = button.Template.FindName("PART_SecondaryButton", button) as System.Windows.Controls.Primitives.ToggleButton;
                    Style? focusVisualStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;

                    Assert.NotNull(primaryButton);
                    Assert.NotNull(secondaryButton);
                    Assert.NotNull(focusVisualStyle);
                    Assert.Same(focusVisualStyle, primaryButton.FocusVisualStyle);
                    Assert.Same(focusVisualStyle, secondaryButton.FocusVisualStyle);
                    Assert.Null(FindVisualChildByName<System.Windows.Controls.Border>(button, "PrimaryFocusOuter"));
                    Assert.Null(FindVisualChildByName<System.Windows.Controls.Border>(button, "SecondaryFocusOuter"));
                }
                finally
                {
                    Keyboard.ClearFocus();
                    window.Close();
                }
            });
        }

        [Fact]
        public void SplitButton_Accent_DividerFillDiffersFromStandard()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // Standard appearance - get divider color
                SplitButton btnStd = new() { Appearance = ControlAppearance.Standard, Content = "Std" };
                Window wStd = new() { Content = btnStd, Width = 300, Height = 100 };
                wStd.Show();
                DrainDispatcher(wStd.Dispatcher);

                Rectangle? dividerStd = FindVisualChildByName<Rectangle>(btnStd, "Divider");
                Assert.NotNull(dividerStd);
                SolidColorBrush? stdBrush = dividerStd.Fill as SolidColorBrush;
                Assert.NotNull(stdBrush);
                wStd.Close();

                // Accent appearance - get divider color
                SplitButton btnAcc = new() { Appearance = ControlAppearance.Accent, Content = "Acc" };
                Window wAcc = new() { Content = btnAcc, Width = 300, Height = 100 };
                wAcc.Show();
                DrainDispatcher(wAcc.Dispatcher);

                Rectangle? dividerAcc = FindVisualChildByName<Rectangle>(btnAcc, "Divider");
                Assert.NotNull(dividerAcc);
                SolidColorBrush? accBrush = dividerAcc.Fill as SolidColorBrush;
                Assert.NotNull(accBrush);

                Assert.NotEqual(
                    stdBrush.Color,
                    accBrush.Color);
                wAcc.Close();
            });
        }
    }
}
