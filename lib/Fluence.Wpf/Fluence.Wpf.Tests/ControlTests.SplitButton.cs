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

using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

        [TestMethod]
        public void SplitButton_AppearanceProperty_DefaultIsStandard()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                SplitButton btn = new();
                Assert.AreEqual(
                    ControlAppearance.Standard,
                    btn.Appearance,
                    "SplitButton.Appearance must default to Standard.");
            });
        }

        [TestMethod]
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

                Assert.AreEqual(
                    ControlAppearance.Accent,
                    btn.Appearance,
                    "SplitButton.Appearance must reflect Accent after being set.");
                w.Close();
            });
        }

        [TestMethod]
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
                Assert.IsNotNull(divider, "Divider (Rectangle) must be present in SplitButton template.");
                Assert.IsNotNull(divider.Fill, "Divider.Fill must not be null.");
                w.Close();
            });
        }

        [TestMethod]
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

                    Assert.IsNotNull(primaryButton, "SplitButton template should expose PART_PrimaryButton.");
                    Assert.IsNotNull(secondaryButton, "SplitButton template should expose PART_SecondaryButton.");
                    Assert.IsNotNull(focusVisualStyle, "DefaultControlFocusVisualStyle should resolve from the computed dictionary.");
                    Assert.AreSame(focusVisualStyle, primaryButton.FocusVisualStyle,
                        "The primary half must use the FocusVisualStyle adorner so the focus ring shows only for keyboard navigation, never on click.");
                    Assert.AreSame(focusVisualStyle, secondaryButton.FocusVisualStyle,
                        "The secondary half must use the FocusVisualStyle adorner so the focus ring shows only for keyboard navigation, never on click.");
                    Assert.IsNull(FindVisualChildByName<System.Windows.Controls.Border>(button, "PrimaryFocusOuter"),
                        "The always-on in-template primary focus borders must be gone; they rendered on mouse click.");
                    Assert.IsNull(FindVisualChildByName<System.Windows.Controls.Border>(button, "SecondaryFocusOuter"),
                        "The always-on in-template secondary focus borders must be gone; they rendered on mouse click.");
                }
                finally
                {
                    Keyboard.ClearFocus();
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                Assert.IsNotNull(dividerStd, "Divider must be in template.");
                SolidColorBrush? stdBrush = dividerStd.Fill as SolidColorBrush;
                Assert.IsNotNull(stdBrush, "Divider.Fill must be a SolidColorBrush in Standard mode.");
                wStd.Close();

                // Accent appearance - get divider color
                SplitButton btnAcc = new() { Appearance = ControlAppearance.Accent, Content = "Acc" };
                Window wAcc = new() { Content = btnAcc, Width = 300, Height = 100 };
                wAcc.Show();
                DrainDispatcher(wAcc.Dispatcher);

                Rectangle? dividerAcc = FindVisualChildByName<Rectangle>(btnAcc, "Divider");
                Assert.IsNotNull(dividerAcc, "Divider must be in accent template.");
                SolidColorBrush? accBrush = dividerAcc.Fill as SolidColorBrush;
                Assert.IsNotNull(accBrush, "Divider.Fill must be a SolidColorBrush in Accent mode.");

                Assert.AreNotEqual(
                    stdBrush.Color,
                    accBrush.Color,
                    "Divider color must differ between Standard (ControlStrokeColorDefaultBrush) "
                    + "and Accent (ControlStrokeColorOnAccentSecondaryBrush) states - WI-3 B17.");
                wAcc.Close();
            });
        }
    }
}
