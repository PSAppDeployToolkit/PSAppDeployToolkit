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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

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

        private static Color GetResolvedBrushColor(Application application, string brushKey)
        {
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(application.Resources[brushKey]);
            return brush.Color;
        }

        private static Color GetSolidColor(Brush? brush)
        {
            return Assert.IsType<SolidColorBrush>(brush, exactMatch: false).Color;
        }

        // Constructs the control inside the STA action: FrameworkElement creation on
        // the xUnit worker thread throws, so the factory must run on the STA thread.
        private static Task RunToggleButtonTestAsync<T>(Func<T> createToggleButton, Action<Application, T> verify)
            where T : Controls.ToggleButton
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                T toggleButton = createToggleButton();
                Window window = new();

                try
                {
                    window.Content = toggleButton;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    verify(application, toggleButton);
                }
                finally
                {
                    window.Close();
                    _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task ToggleButton_Defaults_AreWinUiCanonAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                static (_, toggleButton) =>
                {
                    Assert.Equal(new CornerRadius(4), toggleButton.CornerRadius);
                    Assert.Equal(ControlAppearance.Standard, toggleButton.Appearance);
                    Assert.Equal(false, toggleButton.IsChecked);
                    Assert.False(toggleButton.IsThreeState);
                    Assert.Equal(32.0, toggleButton.MinHeight);
                    Assert.Equal(new Thickness(1), toggleButton.BorderThickness);
                });
        }

        [Fact]
        public Task ToggleButton_DefaultStyle_TemplateExposesRestFillAndBackdropAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                static (_, toggleButton) =>
                {
                    Assert.NotNull(FindVisualChildByName<Border>(toggleButton, "RestFill"));
                    Assert.NotNull(FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop"));
                    Assert.NotNull(FindVisualChildByName<Border>(toggleButton, "OuterBorder"));
                });
        }

        [Fact]
        public Task ToggleButton_Checked_UsesAccentFillBackdropAndOnAccentTextAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                static (application, toggleButton) =>
                {
                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "RestFill"), exactMatch: false);
                    Border backdrop = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(restFill.Background));
                    Assert.Equal(1.0, backdrop.Opacity);
                    Assert.Equal(GetResolvedBrushColor(application, "TextOnAccentFillColorPrimaryBrush"), GetSolidColor(toggleButton.Foreground));
                });
        }

        [Fact]
        public Task ToggleButton_CheckedPressed_UsesAccentTertiaryFillAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new PressableToggleButtonProbe
                {
                    Content = "Probe",
                    IsChecked = true,
                },
                static (application, probe) =>
                {
                    probe.SetPressed(value: true);
                    WpfTestSta.DrainDispatcher(probe.Dispatcher);
                    probe.UpdateLayout();

                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(probe, "RestFill"), exactMatch: false);
                    Border outerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(probe, "OuterBorder"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "AccentFillColorTertiaryBrush"), GetSolidColor(restFill.Background));
                    Assert.Equal(GetResolvedBrushColor(application, "TextOnAccentFillColorSecondaryBrush"), GetSolidColor(probe.Foreground));
                    Assert.Equal(GetResolvedBrushColor(application, "ControlFillColorTransparentBrush"), GetSolidColor(outerBorder.BorderBrush));
                });
        }

        [Fact]
        public Task ToggleButton_Indeterminate_RestKeepsDefaultFillAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsThreeState = true,
                    IsChecked = null,
                    IsHitTestVisible = false,
                },
                static (application, toggleButton) =>
                {
                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "RestFill"), exactMatch: false);
                    Border backdrop = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "ControlFillColorDefaultBrush"), GetSolidColor(restFill.Background));
                    Assert.Equal(0.0, backdrop.Opacity);
                    Assert.Equal(GetResolvedBrushColor(application, "TextFillColorPrimaryBrush"), GetSolidColor(toggleButton.Foreground));
                });
        }

        [Fact]
        public Task ToggleButton_IndeterminatePressed_UsesControlTertiaryFillAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new PressableToggleButtonProbe
                {
                    Content = "Probe",
                    IsThreeState = true,
                    IsChecked = null,
                },
                static (application, probe) =>
                {
                    probe.SetPressed(value: true);
                    WpfTestSta.DrainDispatcher(probe.Dispatcher);
                    probe.UpdateLayout();

                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(probe, "RestFill"), exactMatch: false);
                    Border outerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(probe, "OuterBorder"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "ControlFillColorTertiaryBrush"), GetSolidColor(restFill.Background));
                    Assert.Equal(GetResolvedBrushColor(application, "TextFillColorSecondaryBrush"), GetSolidColor(probe.Foreground));
                    Assert.Equal(GetResolvedBrushColor(application, "ControlStrokeColorDefaultBrush"), GetSolidColor(outerBorder.BorderBrush));
                });
        }

        [Fact]
        public Task ToggleButton_IndeterminateDisabled_UsesDisabledFillAndTextAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsThreeState = true,
                    IsChecked = null,
                    IsEnabled = false,
                },
                static (application, toggleButton) =>
                {
                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "RestFill"), exactMatch: false);
                    Border outerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "OuterBorder"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "ControlFillColorDisabledBrush"), GetSolidColor(restFill.Background));
                    Assert.Equal(GetResolvedBrushColor(application, "TextFillColorDisabledBrush"), GetSolidColor(toggleButton.Foreground));
                    Assert.Equal(GetResolvedBrushColor(application, "ControlStrokeColorDefaultBrush"), GetSolidColor(outerBorder.BorderBrush));
                });
        }

        [Fact]
        public Task ToggleButton_CheckedDisabled_UsesAccentDisabledFillAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsEnabled = false,
                },
                static (application, toggleButton) =>
                {
                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "RestFill"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "AccentFillColorDisabledBrush"), GetSolidColor(restFill.Background));
                    Assert.Equal(GetResolvedBrushColor(application, "TextOnAccentFillColorDisabledBrush"), GetSolidColor(toggleButton.Foreground));
                });
        }

        [Fact]
        public Task ToggleButton_AppearanceAccent_StillRendersCheckedAccentVisualsAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    Appearance = ControlAppearance.Accent,
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                static (application, toggleButton) =>
                {
                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "RestFill"), exactMatch: false);
                    Border backdrop = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "AccentFillBackdrop"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(restFill.Background));
                    Assert.Equal(1.0, backdrop.Opacity);
                    Assert.Equal(GetResolvedBrushColor(application, "TextOnAccentFillColorPrimaryBrush"), GetSolidColor(toggleButton.Foreground));
                });
        }

        [Fact]
        public Task ToggleButton_CheckedTriggers_OrderedRestHoverPressedAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                static (_, toggleButton) =>
                {
                    ControlTemplate template = Assert.IsType<ControlTemplate>(toggleButton.Template, exactMatch: false);
                    TriggerCollection triggers = template.Triggers;

                    int checkedRestIndex = FindTriggerIndex(triggers, static triggerBase =>
                        triggerBase is Trigger trigger
                        && trigger.Property == System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty
                        && Equals(trigger.Value, true));
                    int checkedHoverIndex = FindTriggerIndex(triggers, static triggerBase => IsToggleHoverTrigger(triggerBase, isCheckedValue: true));
                    int checkedPressedIndex = FindTriggerIndex(triggers, static triggerBase => IsTogglePressedTrigger(triggerBase, isCheckedValue: true));
                    int indeterminateHoverIndex = FindTriggerIndex(triggers, static triggerBase => IsToggleHoverTrigger(triggerBase, isCheckedValue: null));
                    int indeterminatePressedIndex = FindTriggerIndex(triggers, static triggerBase => IsTogglePressedTrigger(triggerBase, isCheckedValue: null));

                    Assert.True(checkedRestIndex >= 0, "The checked rest trigger should exist.");
                    Assert.True(checkedHoverIndex >= 0, "The checked hover trigger should exist.");
                    Assert.True(checkedPressedIndex >= 0, "The checked pressed trigger should exist.");
                    Assert.True(indeterminateHoverIndex >= 0, "The indeterminate hover trigger should exist.");
                    Assert.True(indeterminatePressedIndex >= 0, "The indeterminate pressed trigger should exist.");
                    Assert.True(checkedRestIndex < checkedHoverIndex && checkedHoverIndex < checkedPressedIndex,
                        "Checked triggers must be ordered rest, hover, pressed so WPF last-wins precedence keeps hover and pressed tints visible.");
                    Assert.True(indeterminateHoverIndex < indeterminatePressedIndex,
                        "Indeterminate hover must precede indeterminate pressed for last-wins precedence.");
                });
        }

        [Fact]
        public Task ToggleButton_ThemeCycle_CheckedBrushesReResolveAsync()
        {
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                static (application, toggleButton) =>
                {
                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    WpfTestSta.DrainDispatcher(toggleButton.Dispatcher);
                    toggleButton.UpdateLayout();

                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "RestFill"), exactMatch: false);

                    Assert.Equal(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(restFill.Background));
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
            return multiTrigger.Conditions.Any(condition => condition.Property == property && Equals(condition.Value, value));
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
