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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// ToggleButton parity tests: WinUI checked/indeterminate state visuals, the
    /// last-wins trigger ordering (rest before hover before pressed), and theme
    /// re-resolution of the checked accent brushes.
    /// </summary>
    public partial class ControlTests
    {
        // ButtonBase.IsPressed has a protected setter, so a probe subclass can drive
        // the pressed triggers for real brush assertions (IsMouseOver stays read-only
        // and is covered structurally by the trigger-order test instead).
        private sealed class PressableToggleButtonProbe : Controls.ToggleButton
        {
            public void SetPressed(bool value)
            {
                IsPressed = value;
            }
        }

        private static Color GetResolvedBrushColor(Application? application, string brushKey)
        {
            SolidColorBrush? brush = application?.Resources[brushKey] as SolidColorBrush;
            Assert.IsNotNull(brush, brushKey + " should resolve to a SolidColorBrush.");
            return brush.Color;
        }

        private static Color GetSolidColor(Brush? brush, string elementDescription)
        {
            Assert.IsInstanceOfType(brush, typeof(SolidColorBrush), elementDescription + " should use a SolidColorBrush.");
            return ((SolidColorBrush)brush).Color;
        }

        // Constructs the control inside the STA action: FrameworkElement creation on
        // the MSTest worker thread throws, so the factory must run on the STA thread.
        private static void RunToggleButtonTest<T>(Func<T> createToggleButton, Action<Application?, T> verify)
            where T : Controls.ToggleButton
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                T toggleButton = createToggleButton();
                Window window = new();

                try
                {
                    window.Content = toggleButton;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    verify(application, toggleButton);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ToggleButton_Defaults_AreWinUiCanon()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, toggleButton) =>
                {
                    Assert.AreEqual(new CornerRadius(4), toggleButton.CornerRadius);
                    Assert.AreEqual(ControlAppearance.Standard, toggleButton.Appearance);
                    Assert.AreEqual(false, toggleButton.IsChecked);
                    Assert.IsFalse(toggleButton.IsThreeState);
                    Assert.AreEqual(32.0, toggleButton.MinHeight);
                    Assert.AreEqual(new Thickness(1), toggleButton.BorderThickness);
                });
        }

        [TestMethod]
        public void ToggleButton_DefaultStyle_TemplateExposesRestFillAndBackdrop()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, toggleButton) =>
                {
                    Assert.IsNotNull(FindVisualChildByName<Border>(toggleButton, "RestFill"), "RestFill border should exist in the template.");
                    Assert.IsNotNull(FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop"), "AccentFillBackdrop border should exist in the template.");
                    Assert.IsNotNull(FindVisualChildByName<Border>(toggleButton, "OuterBorder"), "OuterBorder should exist in the template.");
                });
        }

        [TestMethod]
        public void ToggleButton_Checked_UsesAccentFillBackdropAndOnAccentText()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                (application, toggleButton) =>
                {
                    Border? restFill = FindVisualChildByName<Border>(toggleButton, "RestFill");
                    Border? backdrop = FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop");

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(backdrop);
                    Assert.AreEqual(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(restFill.Background, "Checked RestFill"));
                    Assert.AreEqual(1.0, backdrop.Opacity, "Checked state should reveal the opaque accent backdrop layer.");
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextOnAccentFillColorPrimaryBrush"), GetSolidColor(toggleButton.Foreground, "Checked foreground"));
                });
        }

        [TestMethod]
        public void ToggleButton_CheckedPressed_UsesAccentTertiaryFill()
        {
            RunToggleButtonTest(
                () => new PressableToggleButtonProbe
                {
                    Content = "Probe",
                    IsChecked = true,
                },
                (application, probe) =>
                {
                    probe.SetPressed(value: true);
                    DrainDispatcher(probe.Dispatcher);
                    probe.UpdateLayout();

                    Border? restFill = FindVisualChildByName<Border>(probe, "RestFill");
                    Border? outerBorder = FindVisualChildByName<Border>(probe, "OuterBorder");

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(outerBorder);
                    Assert.AreEqual(GetResolvedBrushColor(application, "AccentFillColorTertiaryBrush"), GetSolidColor(restFill.Background, "Checked pressed RestFill"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextOnAccentFillColorSecondaryBrush"), GetSolidColor(probe.Foreground, "Checked pressed foreground"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlFillColorTransparentBrush"), GetSolidColor(outerBorder.BorderBrush, "Checked pressed border"));
                });
        }

        [TestMethod]
        public void ToggleButton_Indeterminate_RestKeepsDefaultFill()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsThreeState = true,
                    IsChecked = null,
                    IsHitTestVisible = false,
                },
                (application, toggleButton) =>
                {
                    Border? restFill = FindVisualChildByName<Border>(toggleButton, "RestFill");
                    Border? backdrop = FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop");

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(backdrop);
                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlFillColorDefaultBrush"), GetSolidColor(restFill.Background, "Indeterminate RestFill"));
                    Assert.AreEqual(0.0, backdrop.Opacity, "Indeterminate rest should not reveal the accent backdrop.");
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextFillColorPrimaryBrush"), GetSolidColor(toggleButton.Foreground, "Indeterminate foreground"));
                });
        }

        [TestMethod]
        public void ToggleButton_IndeterminatePressed_UsesControlTertiaryFill()
        {
            RunToggleButtonTest(
                () => new PressableToggleButtonProbe
                {
                    Content = "Probe",
                    IsThreeState = true,
                    IsChecked = null,
                },
                (application, probe) =>
                {
                    probe.SetPressed(value: true);
                    DrainDispatcher(probe.Dispatcher);
                    probe.UpdateLayout();

                    Border? restFill = FindVisualChildByName<Border>(probe, "RestFill");
                    Border? outerBorder = FindVisualChildByName<Border>(probe, "OuterBorder");

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(outerBorder);
                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlFillColorTertiaryBrush"), GetSolidColor(restFill.Background, "Indeterminate pressed RestFill"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextFillColorSecondaryBrush"), GetSolidColor(probe.Foreground, "Indeterminate pressed foreground"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlStrokeColorDefaultBrush"), GetSolidColor(outerBorder.BorderBrush, "Indeterminate pressed border"));
                });
        }

        [TestMethod]
        public void ToggleButton_IndeterminateDisabled_UsesDisabledFillAndText()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsThreeState = true,
                    IsChecked = null,
                    IsEnabled = false,
                },
                (application, toggleButton) =>
                {
                    Border? restFill = FindVisualChildByName<Border>(toggleButton, "RestFill");
                    Border? outerBorder = FindVisualChildByName<Border>(toggleButton, "OuterBorder");

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(outerBorder);
                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlFillColorDisabledBrush"), GetSolidColor(restFill.Background, "Indeterminate disabled RestFill"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextFillColorDisabledBrush"), GetSolidColor(toggleButton.Foreground, "Indeterminate disabled foreground"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlStrokeColorDefaultBrush"), GetSolidColor(outerBorder.BorderBrush, "Indeterminate disabled border"));
                });
        }

        [TestMethod]
        public void ToggleButton_CheckedDisabled_UsesAccentDisabledFill()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsEnabled = false,
                },
                (application, toggleButton) =>
                {
                    Border? restFill = FindVisualChildByName<Border>(toggleButton, "RestFill");

                    Assert.IsNotNull(restFill);
                    Assert.AreEqual(GetResolvedBrushColor(application, "AccentFillColorDisabledBrush"), GetSolidColor(restFill.Background, "Checked disabled RestFill"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextOnAccentFillColorDisabledBrush"), GetSolidColor(toggleButton.Foreground, "Checked disabled foreground"));
                });
        }

        [TestMethod]
        public void ToggleButton_AppearanceAccent_StillRendersCheckedAccentVisuals()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    Appearance = ControlAppearance.Accent,
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                (application, toggleButton) =>
                {
                    Border? restFill = FindVisualChildByName<Border>(toggleButton, "RestFill");
                    Border? backdrop = FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop");

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(backdrop);
                    Assert.AreEqual(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(restFill.Background, "Accent-appearance checked RestFill"));
                    Assert.AreEqual(1.0, backdrop.Opacity, "Appearance values must not disable the canonical checked visuals.");
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextOnAccentFillColorPrimaryBrush"), GetSolidColor(toggleButton.Foreground, "Accent-appearance checked foreground"));
                });
        }

        [TestMethod]
        public void ToggleButton_CheckedTriggers_OrderedRestHoverPressed()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, toggleButton) =>
                {
                    ControlTemplate? template = toggleButton.Template;
                    Assert.IsNotNull(template, "The default style should supply a template.");
                    TriggerCollection triggers = template.Triggers;

                    int checkedRestIndex = FindTriggerIndex(triggers, triggerBase =>
                        triggerBase is Trigger trigger
                        && trigger.Property == System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty
                        && Equals(trigger.Value, true));
                    int checkedHoverIndex = FindTriggerIndex(triggers, triggerBase => IsToggleHoverTrigger(triggerBase, isCheckedValue: true));
                    int checkedPressedIndex = FindTriggerIndex(triggers, triggerBase => IsTogglePressedTrigger(triggerBase, isCheckedValue: true));
                    int indeterminateHoverIndex = FindTriggerIndex(triggers, triggerBase => IsToggleHoverTrigger(triggerBase, isCheckedValue: null));
                    int indeterminatePressedIndex = FindTriggerIndex(triggers, triggerBase => IsTogglePressedTrigger(triggerBase, isCheckedValue: null));

                    Assert.IsTrue(checkedRestIndex >= 0, "The checked rest trigger should exist.");
                    Assert.IsTrue(checkedHoverIndex >= 0, "The checked hover trigger should exist.");
                    Assert.IsTrue(checkedPressedIndex >= 0, "The checked pressed trigger should exist.");
                    Assert.IsTrue(indeterminateHoverIndex >= 0, "The indeterminate hover trigger should exist.");
                    Assert.IsTrue(indeterminatePressedIndex >= 0, "The indeterminate pressed trigger should exist.");
                    Assert.IsTrue(checkedRestIndex < checkedHoverIndex && checkedHoverIndex < checkedPressedIndex,
                        "Checked triggers must be ordered rest, hover, pressed so WPF last-wins precedence keeps hover and pressed tints visible.");
                    Assert.IsTrue(indeterminateHoverIndex < indeterminatePressedIndex,
                        "Indeterminate hover must precede indeterminate pressed for last-wins precedence.");
                });
        }

        [TestMethod]
        public void ToggleButton_ThemeCycle_CheckedBrushesReResolve()
        {
            RunToggleButtonTest(
                () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                (application, toggleButton) =>
                {
                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    DrainDispatcher(toggleButton.Dispatcher);
                    toggleButton.UpdateLayout();

                    Border? restFill = FindVisualChildByName<Border>(toggleButton, "RestFill");

                    Assert.IsNotNull(restFill);
                    Assert.AreEqual(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(restFill.Background, "Checked RestFill after theme cycle"));
                    ThemeTestHelpers.AssertKeyThemeBrushesResolve(application);
                });
        }

        private static int FindTriggerIndex(TriggerCollection triggers, Func<TriggerBase, bool> predicate)
        {
            for (int index = 0; index < triggers.Count; index++)
            {
                if (predicate(triggers[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool HasTriggerCondition(MultiTrigger multiTrigger, DependencyProperty property, object? value)
        {
            foreach (Condition condition in multiTrigger.Conditions)
            {
                if (condition.Property == property && Equals(condition.Value, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsToggleHoverTrigger(TriggerBase triggerBase, object? isCheckedValue)
        {
            return triggerBase is MultiTrigger multiTrigger
                && multiTrigger.Conditions.Count is 3
                && HasTriggerCondition(multiTrigger, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, isCheckedValue)
                && HasTriggerCondition(multiTrigger, UIElement.IsMouseOverProperty, value: true)
                && HasTriggerCondition(multiTrigger, System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, value: false);
        }

        private static bool IsTogglePressedTrigger(TriggerBase triggerBase, object? isCheckedValue)
        {
            return triggerBase is MultiTrigger multiTrigger
                && multiTrigger.Conditions.Count is 2
                && HasTriggerCondition(multiTrigger, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, isCheckedValue)
                && HasTriggerCondition(multiTrigger, System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, value: true);
        }
    }
}
