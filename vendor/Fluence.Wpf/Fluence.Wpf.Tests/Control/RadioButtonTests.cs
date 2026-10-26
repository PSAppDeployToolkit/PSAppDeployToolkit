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
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="RadioButton"/> control: Description surfaces as
    /// AutomationProperties.HelpText.
    /// </summary>
    public sealed class RadioButtonTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                MotionHelper.OverrideIsMotionEnabled = null;
                _ = TestApp.EnsureLibraryTheme();
            }));
        }

        private sealed class PressableRadioButtonProbe : RadioButton
        {
            public void SetPressed(bool value)
            {
                IsPressed = value;
            }
        }

        [Fact]
        public Task RadioButton_CheckSelection_SwitchesOpacityWithoutResizingDotAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radio = new() { Content = "Option", IsChecked = true };
                Window window = new() { Content = radio, Width = 240, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    SetHoverStateForTest(radio, value: false);
                    Assert.False(radio.IsMouseOver);

                    Ellipse dot = Assert.IsType<Ellipse>(radio.Template.FindName("PART_CheckGlyph", radio));
                    Ellipse outer = Assert.IsType<Ellipse>(radio.Template.FindName("OuterEllipse", radio));
                    Assert.Equal(20, outer.Width);
                    Assert.Equal(20, outer.Height);
                    Assert.Equal(1, outer.StrokeThickness);
                    Assert.Equal(12, dot.Width);
                    Assert.Equal(12, dot.Height);
                    Assert.Equal(1, dot.Opacity);
                    Assert.False(dot.HasAnimatedProperties, "An initially checked dot must already be at its resting size.");

                    radio.IsChecked = false;
                    Assert.Equal(0, dot.Opacity);
                    Assert.Equal(12, dot.Width);
                    Assert.False(dot.HasAnimatedProperties, "Unchecking changes visibility without shrinking the dot.");

                    radio.IsChecked = true;
                    Assert.Equal(1, dot.Opacity);
                    Assert.Equal(12, dot.Width);
                    Assert.False(dot.HasAnimatedProperties, "Checking changes visibility without growing the dot.");
                }
                finally
                {
                    SetHoverStateForTest(radio, value: false);
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task RadioButton_PressedAndDisabled_ReachWinUiGlyphSizesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                PressableRadioButtonProbe radio = new() { Content = "Option", IsChecked = true };
                Window window = new() { Content = radio, Width = 240, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    SetHoverStateForTest(radio, value: false);
                    Assert.False(radio.IsMouseOver);

                    Ellipse dot = Assert.IsType<Ellipse>(radio.Template.FindName("PART_CheckGlyph", radio));
                    System.Windows.Controls.Border pressedGlyph = Assert.IsType<System.Windows.Controls.Border>(
                        radio.Template.FindName("PART_PressedCheckGlyph", radio));
                    Assert.Equal(12, dot.Width);
                    Assert.Equal(4, pressedGlyph.Width);
                    Assert.Equal(0, pressedGlyph.Opacity);

                    radio.SetPressed(value: true);
                    Assert.True(radio.IsPressed);
                    Assert.Equal(1, pressedGlyph.Opacity);
                    Assert.True(dot.HasAnimatedProperties, "Pressing should animate the main dot size.");
                    Assert.True(pressedGlyph.HasAnimatedProperties, "Pressing should grow the separate glyph.");
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 350).ConfigureAwait(true);
                    Assert.Equal(10, dot.Width, 2);
                    Assert.Equal(10, dot.Height, 2);
                    Assert.Equal(10, pressedGlyph.Width, 2);
                    Assert.Equal(10, pressedGlyph.Height, 2);

                    radio.SetPressed(value: false);
                    Assert.Equal(0, pressedGlyph.Opacity);
                    Assert.Equal(12, dot.Width, 2);
                    Assert.Equal(4, pressedGlyph.Width, 2);

                    radio.IsEnabled = false;
                    Assert.True(dot.HasAnimatedProperties, "Disabling should animate to the 14-DIP target.");
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 250).ConfigureAwait(true);
                    Assert.Equal(14, dot.Width, 2);
                    Assert.Equal(14, dot.Height, 2);

                    radio.IsEnabled = true;
                    Assert.Equal(12, dot.Width, 2);
                    Assert.Equal(12, dot.Height, 2);
                }
                finally
                {
                    SetHoverStateForTest(radio, value: false);
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task RadioButton_HoverAndRapidReversal_UseCurrentCommonStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                PressableRadioButtonProbe radio = new() { Content = "Option", IsChecked = true };
                Window window = new() { Content = radio, Width = 240, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Ellipse dot = Assert.IsType<Ellipse>(radio.Template.FindName("PART_CheckGlyph", radio));
                    System.Windows.Controls.Border pressedGlyph = Assert.IsType<System.Windows.Controls.Border>(
                        radio.Template.FindName("PART_PressedCheckGlyph", radio));

                    // Simulate WPF's read-only hover state without moving the desktop pointer.
                    SetHoverStateForTest(radio, value: true);
                    Assert.True(radio.IsMouseOver);
                    Assert.True(dot.HasAnimatedProperties);
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 350).ConfigureAwait(true);
                    Assert.Equal(14, dot.Width, 2);

                    radio.SetPressed(value: true);
                    Assert.Equal(1, pressedGlyph.Opacity);
                    SetHoverStateForTest(radio, value: false);
                    Assert.False(radio.IsMouseOver);
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 350).ConfigureAwait(true);
                    Assert.Equal(10, dot.Width, 2);
                    Assert.Equal(10, pressedGlyph.Width, 2);

                    radio.SetPressed(value: false);
                    Assert.Equal(12, dot.Width, 2);
                    Assert.Equal(4, pressedGlyph.Width, 2);
                    Assert.Equal(0, pressedGlyph.Opacity);
                    Assert.False(dot.HasAnimatedProperties);
                    Assert.False(pressedGlyph.HasAnimatedProperties);
                }
                finally
                {
                    SetHoverStateForTest(radio, value: false);
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task RadioButton_HoverChangesDuringPress_DoNotRestartGlyphClockAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                PressableRadioButtonProbe radio = new() { Content = "Option", IsChecked = true };
                Window window = new() { Content = radio, Width = 240, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Ellipse dot = Assert.IsType<Ellipse>(radio.Template.FindName("PART_CheckGlyph", radio));
                    radio.SetPressed(value: true);
                    Assert.True(dot.HasAnimatedProperties);

                    await Task.Delay(80, TestContext.Current.CancellationToken).ConfigureAwait(true);
                    Assert.True(dot.HasAnimatedProperties);
                    SetHoverStateForTest(radio, value: true);
                    Assert.True(radio.IsMouseOver);

                    await Task.Delay(80, TestContext.Current.CancellationToken).ConfigureAwait(true);
                    Assert.True(dot.HasAnimatedProperties);
                    SetHoverStateForTest(radio, value: false);
                    Assert.False(radio.IsMouseOver);

                    await Task.Delay(125, TestContext.Current.CancellationToken).ConfigureAwait(true);
                    Assert.False(dot.HasAnimatedProperties,
                        "Hover changes during an active press must not restart its 250-ms glyph clock.");
                    Assert.Equal(10, dot.Width, 2);
                }
                finally
                {
                    SetHoverStateForTest(radio, value: false);
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static void SetHoverStateForTest(UIElement element, bool value)
        {
            // WPF's IsMouseOver getter reads an internal cache as well as its read-only DP.
            // Update both for a bounded programmatic state test; this does not simulate a physical pointer.
            FieldInfo keyField = Assert.IsType<FieldInfo>(typeof(UIElement).GetField(
                "IsMouseOverPropertyKey", BindingFlags.Static | BindingFlags.NonPublic), exactMatch: false);
            DependencyPropertyKey key = Assert.IsType<DependencyPropertyKey>(keyField.GetValue(null));
            Type flagsType = Assert.IsType<Type>(typeof(UIElement).Assembly.GetType("System.Windows.CoreFlags"), exactMatch: false);
            MethodInfo writeFlag = Assert.IsType<MethodInfo>(typeof(UIElement).GetMethod(
                "WriteFlag", BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
            _ = writeFlag.Invoke(element, [Enum.Parse(flagsType, "IsMouseOverCache"), value]);
            element.SetValue(key, value);
        }

        [Fact]
        public Task RadioButton_RetemplateAndUnload_ClearOldGlyphClocksAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MotionHelper.OverrideIsMotionEnabled = true;
                PressableRadioButtonProbe radio = new() { Content = "Option", IsChecked = true };
                System.Windows.Controls.StackPanel panel = new();
                _ = panel.Children.Add(radio);
                Window window = new() { Content = panel, Width = 240, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Ellipse oldGlyph = Assert.IsType<Ellipse>(radio.Template.FindName("PART_CheckGlyph", radio));

                    radio.SetPressed(value: true);
                    Assert.True(oldGlyph.HasAnimatedProperties);
                    radio.Template = new System.Windows.Controls.ControlTemplate(typeof(RadioButton))
                    {
                        VisualTree = new FrameworkElementFactory(typeof(System.Windows.Controls.Grid)),
                    };
                    _ = radio.ApplyTemplate();
                    Assert.False(oldGlyph.HasAnimatedProperties, "Retemplating must release the old glyph clocks.");
                    Assert.True(radio.IsChecked is true, "Retemplating must preserve the native selection state.");

                    radio.ClearValue(System.Windows.Controls.Control.TemplateProperty);
                    _ = radio.ApplyTemplate();
                    Ellipse newGlyph = Assert.IsType<Ellipse>(radio.Template.FindName("PART_CheckGlyph", radio));
                    Assert.Equal(10, newGlyph.Width);
                    Assert.False(newGlyph.HasAnimatedProperties, "A fresh template must snap to the current pressed state.");

                    radio.SetPressed(value: false);
                    radio.IsEnabled = false;
                    Assert.True(newGlyph.HasAnimatedProperties);
                    panel.Children.Remove(radio);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(newGlyph.HasAnimatedProperties, "Unloading must release active geometry clocks.");

                    radio.IsEnabled = true;
                    _ = panel.Children.Add(radio);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    SetHoverStateForTest(radio, value: false);
                    Assert.Equal(12, newGlyph.Width);
                    Assert.False(newGlyph.HasAnimatedProperties, "Reloading must snap the glyph to current state.");
                }
                finally
                {
                    SetHoverStateForTest(radio, value: false);
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // RadioButton Description -> HelpText
        // ---------------------------------------------------------------------------

        [Fact]
        public Task RadioButton_Description_SetsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Choose this option for better performance.",
                };

                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.Equals("Choose this option for better performance.", helpText, StringComparison.Ordinal),
                    $"RadioButton.Description must be surfaced as AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_DescriptionChanges_UpdatesAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Initial description.",
                };

                radioButton.Description = "Revised description.";
                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.Equals("Revised description.", helpText, StringComparison.Ordinal),
                    $"RadioButton.Description change must update AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_NullDescription_ClearsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Some description.",
                };
                radioButton.Description = null;

                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.IsNullOrWhiteSpace(helpText),
                    $"Null RadioButton.Description must clear AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_Checked_HasAccentFillAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                Window window = new();
                RadioButton radio = new()
                {
                    Content = "Test",
                    IsChecked = true,
                };

                try
                {
                    window.Content = radio;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    Ellipse checkedEllipse = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipse", radio));
                    Ellipse hoverEllipse = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipseHover", radio));
                    Ellipse pressedEllipse = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipsePressed", radio));
                    SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentFillColorDefaultBrush"]);

                    SetHoverStateForTest(radio, value: true);
                    Assert.True(radio.IsMouseOver);
                    Assert.False(radio.IsPressed);
                    Assert.Equal(0.0, checkedEllipse.Opacity);
                    Assert.Equal(1.0, hoverEllipse.Opacity);
                    Assert.Equal(0.0, pressedEllipse.Opacity);

                    SetHoverStateForTest(radio, value: false);
                    Assert.False(radio.IsMouseOver);
                    Assert.Equal(1.0, checkedEllipse.Opacity);
                    Assert.Equal(0.0, hoverEllipse.Opacity);
                    Assert.Equal(0.0, pressedEllipse.Opacity);
                    _ = Assert.IsType<SolidColorBrush>(checkedEllipse.Fill, exactMatch: false);
                    Assert.Equal(accentBrush.Color, ((SolidColorBrush)checkedEllipse.Fill).Color);
                }
                finally
                {
                    SetHoverStateForTest(radio, value: false);
                    window.Close();
                }
            });
        }

        [Fact]
        public Task RadioButton_CheckedOuterStroke_TracksLiveHighContrastRolesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                PressableRadioButtonProbe radio = new() { Content = "Option", IsChecked = true };
                Window window = new() { Content = radio, Width = 240, Height = 120 };

                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Ellipse normal = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipse", radio));
                    Ellipse hover = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipseHover", radio));
                    Ellipse pressed = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipsePressed", radio));
                    string[] keys =
                    [
                        "RadioButtonOuterEllipseCheckedStrokeBrush",
                        "RadioButtonOuterEllipseCheckedStrokePointerOverBrush",
                        "RadioButtonOuterEllipseCheckedStrokePressedBrush",
                        "RadioButtonOuterEllipseCheckedStrokeDisabledBrush",
                    ];
                    foreach (string key in keys)
                    {
                        Assert.Equal(
                            Assert.IsType<SolidColorBrush>(application.Resources[key.Replace("Stroke", "Fill", StringComparison.Ordinal)]).Color,
                            Assert.IsType<SolidColorBrush>(application.Resources[key]).Color);
                    }

                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(SystemColors.HighlightColor, Assert.IsType<SolidColorBrush>(application.Resources[keys[0]]).Color);
                    Assert.Equal(SystemColors.ControlTextColor, Assert.IsType<SolidColorBrush>(application.Resources[keys[1]]).Color);
                    Assert.Equal(SystemColors.ControlColor, Assert.IsType<SolidColorBrush>(application.Resources[keys[2]]).Color);
                    Assert.Equal(SystemColors.GrayTextColor, Assert.IsType<SolidColorBrush>(application.Resources[keys[3]]).Color);
                    Assert.Equal(SystemColors.HighlightColor, Assert.IsType<SolidColorBrush>(normal.Stroke).Color);

                    SetHoverStateForTest(radio, value: true);
                    Assert.Equal(1, hover.Opacity);
                    Assert.Equal(SystemColors.ControlTextColor, Assert.IsType<SolidColorBrush>(hover.Stroke).Color);
                    radio.SetPressed(value: true);
                    Assert.Equal(1, pressed.Opacity);
                    Assert.Equal(SystemColors.ControlColor, Assert.IsType<SolidColorBrush>(pressed.Stroke).Color);
                    radio.IsEnabled = false;
                    Assert.Equal(SystemColors.GrayTextColor, Assert.IsType<SolidColorBrush>(pressed.Stroke).Color);

                    radio.IsEnabled = true;
                    radio.SetPressed(value: false);
                    SetHoverStateForTest(radio, value: false);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(
                        Assert.IsType<SolidColorBrush>(application.Resources["RadioButtonOuterEllipseCheckedStrokeBrush"]).Color,
                        Assert.IsType<SolidColorBrush>(normal.Stroke).Color);
                }
                finally
                {
                    SetHoverStateForTest(radio, value: false);
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task RadioButton_GlyphRow_AlignsCircleAndContentWithWinUiSpacingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                RadioButton radio = new()
                {
                    Content = "Standard",
                    Width = 240,
                    Height = 40,
                };

                try
                {
                    window.Content = radio;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    System.Windows.Controls.Grid indicatorHost = Assert.IsType<System.Windows.Controls.Grid>(FindVisualChildByName<System.Windows.Controls.Grid>(radio, "IndicatorHost"), exactMatch: false);
                    System.Windows.Controls.ContentPresenter contentPresenter = Assert.IsType<System.Windows.Controls.ContentPresenter>(FindVisualChildByName<System.Windows.Controls.ContentPresenter>(radio, "ContentPresenter"), exactMatch: false);
                    Ellipse outer = Assert.IsType<Ellipse>(radio.Template.FindName("OuterEllipse", radio));

                    Assert.Equal(120, radio.MinWidth);
                    Assert.Equal(VerticalAlignment.Top, radio.VerticalContentAlignment);
                    Assert.Equal(VerticalAlignment.Top, indicatorHost.VerticalAlignment);
                    Assert.Equal(32, indicatorHost.Height);
                    Assert.Equal(new Thickness(0), indicatorHost.Margin);
                    Assert.Equal(VerticalAlignment.Top, contentPresenter.VerticalAlignment);
                    Assert.Equal(new Thickness(8, 6, 0, 0), contentPresenter.Margin);
                    Assert.Equal(6, outer.TransformToAncestor(indicatorHost).Transform(new Point()).Y, 0.01);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task RadioButton_GroupExclusivity_UnchecksOthersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                System.Windows.Controls.StackPanel panel = new();
                RadioButton radio1 = new() { Content = "A", GroupName = "TestGroup", IsChecked = true };
                RadioButton radio2 = new() { Content = "B", GroupName = "TestGroup" };
                RadioButton radio3 = new() { Content = "C", GroupName = "TestGroup" };
                _ = panel.Children.Add(radio1);
                _ = panel.Children.Add(radio2);
                _ = panel.Children.Add(radio3);

                try
                {
                    window.Content = panel;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(radio1.IsChecked is true);

                    radio2.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(false, radio1.IsChecked);
                    Assert.Equal(true, radio2.IsChecked);
                    Assert.Equal(false, radio3.IsChecked);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
