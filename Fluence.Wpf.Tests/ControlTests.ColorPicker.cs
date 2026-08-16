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
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.ColorPicker"/> control: default style
    /// and template parts, the 256 x 256 saturation/value spectrum bitmap, ColorChanged
    /// old/new payloads, hex entry commit/normalize/revert semantics, hue and alpha slider
    /// channel edits with hue retention across the grey axis, the alpha row visibility
    /// contract, the previous-color swatch, automation peer naming, and surface brush
    /// theming.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public Task ColorPicker_DefaultStyle_AppliesTemplatePartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.ColorPicker)));

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);

                    Image spectrumImage = Assert.IsType<Image>(template.FindName("PART_SpectrumImage", picker));
                    FrameworkElement spectrumArea = Assert.IsAssignableFrom<FrameworkElement>(template.FindName("PART_SpectrumArea", picker));
                    FrameworkElement spectrumThumb = Assert.IsAssignableFrom<FrameworkElement>(template.FindName("PART_SpectrumThumb", picker));
                    RangeBase hueSlider = Assert.IsAssignableFrom<RangeBase>(template.FindName("PART_HueSlider", picker));
                    RangeBase alphaSlider = Assert.IsAssignableFrom<RangeBase>(template.FindName("PART_AlphaSlider", picker));
                    TextBox hexTextBox = Assert.IsAssignableFrom<TextBox>(template.FindName("PART_HexTextBox", picker));

                    Assert.Equal(Color.FromArgb(255, 255, 0, 0), picker.Color);
                    Assert.Equal(0d, hueSlider.Minimum);
                    Assert.Equal(360d, hueSlider.Maximum);
                    Assert.Equal(0d, alphaSlider.Minimum);
                    Assert.Equal(255d, alphaSlider.Maximum);
                    Assert.Null(picker.PreviousColor);
                    Assert.False(picker.IsAlphaEnabled, "IsAlphaEnabled must default to false.");
                    Assert.True(picker.IsColorSpectrumVisible, "IsColorSpectrumVisible must default to true.");
                    Assert.True(picker.IsColorChannelTextInputVisible,
                        "IsColorChannelTextInputVisible must default to true.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_SpectrumBitmap_IsGenerated256x256AfterTemplateApplyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    Image spectrumImage = Assert.IsType<Image>(template.FindName("PART_SpectrumImage", picker));

                    WriteableBitmap bitmap = Assert.IsType<WriteableBitmap>(spectrumImage.Source);
                    Assert.Equal(256, bitmap.PixelWidth);
                    Assert.Equal(256, bitmap.PixelHeight);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_SetColor_RaisesColorChangedAndUpdatesHexTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    TextBox hexTextBox = Assert.IsAssignableFrom<TextBox>(template.FindName("PART_HexTextBox", picker));
                    Assert.Equal("#FF0000", hexTextBox.Text, StringComparer.Ordinal);

                    ColorPickerColorChangedEventArgs? changed = null;
                    int raiseCount = 0;
                    picker.ColorChanged += (sender, e) =>
                    {
                        changed = e;
                        raiseCount++;
                    };

                    Color target = Color.FromArgb(255, 0, 120, 212);
                    picker.Color = target;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1, raiseCount);
                    Assert.NotNull(changed);
                    Assert.Equal(Color.FromArgb(255, 255, 0, 0), changed.OldColor);
                    Assert.Equal(target, changed.NewColor);
                    Assert.Equal("#0078D4", hexTextBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_HexEntry_CommitsOnEnterAndInvalidInputRevertsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    TextBox hexTextBox = Assert.IsAssignableFrom<TextBox>(template.FindName("PART_HexTextBox", picker));

                    PresentationSource source = Assert.IsAssignableFrom<PresentationSource>(PresentationSource.FromVisual(hexTextBox));

                    hexTextBox.Text = "#FF0078D4";
                    hexTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 0, 120, 212), picker.Color);
                    Assert.Equal("#0078D4", hexTextBox.Text, StringComparer.Ordinal);

                    hexTextBox.Text = "00b294";
                    hexTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 0, 178, 148), picker.Color);
                    Assert.Equal("#00B294", hexTextBox.Text, StringComparer.Ordinal);

                    hexTextBox.Text = "not-a-color";
                    hexTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 0, 178, 148), picker.Color);
                    Assert.Equal("#00B294", hexTextBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_HueSlider_UpdatesColorAtFullSaturationAndValueAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    RangeBase hueSlider = Assert.IsAssignableFrom<RangeBase>(template.FindName("PART_HueSlider", picker));
                    TextBox hexTextBox = Assert.IsAssignableFrom<TextBox>(template.FindName("PART_HexTextBox", picker));

                    ColorPickerColorChangedEventArgs? changed = null;
                    picker.ColorChanged += (sender, e) => changed = e;

                    // The default red sits at the S=1, V=1 fixed point, so a hue change maps
                    // exactly onto the pure hue color.
                    hueSlider.Value = 120;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 0, 255, 0), picker.Color);
                    Assert.NotNull(changed);
                    Assert.Equal(Color.FromArgb(255, 255, 0, 0), changed.OldColor);
                    Assert.Equal(Color.FromArgb(255, 0, 255, 0), changed.NewColor);
                    Assert.Equal("#00FF00", hexTextBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_AlphaSlider_CollapsedByDefaultAndFunctionalWhenEnabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    FrameworkElement alphaSection = Assert.IsAssignableFrom<FrameworkElement>(template.FindName("AlphaSection", picker));
                    RangeBase alphaSlider = Assert.IsAssignableFrom<RangeBase>(template.FindName("PART_AlphaSlider", picker));
                    TextBox hexTextBox = Assert.IsAssignableFrom<TextBox>(template.FindName("PART_HexTextBox", picker));

                    Assert.Equal(Visibility.Collapsed, alphaSection.Visibility);

                    picker.IsAlphaEnabled = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Visible, alphaSection.Visibility);
                    Assert.Equal("#FFFF0000", hexTextBox.Text, StringComparer.Ordinal);

                    alphaSlider.Value = 128;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(128, 255, 0, 0), picker.Color);
                    Assert.Equal("#80FF0000", hexTextBox.Text, StringComparer.Ordinal);

                    picker.IsAlphaEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, alphaSection.Visibility);
                    Assert.Equal(Color.FromArgb(255, 255, 0, 0), picker.Color);
                    Assert.Equal("#FF0000", hexTextBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_SpectrumPoint_UpdatesSaturationAndValuePreservingHueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    FrameworkElement spectrumArea = Assert.IsAssignableFrom<FrameworkElement>(template.FindName("PART_SpectrumArea", picker));
                    RangeBase hueSlider = Assert.IsAssignableFrom<RangeBase>(template.FindName("PART_HueSlider", picker));
                    Assert.True(spectrumArea.ActualWidth > 0 && spectrumArea.ActualHeight > 0,
                        "The spectrum area must have a layout size once the window is shown.");

                    hueSlider.Value = 120;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // Top-left corner: saturation 0, value 1 - white, regardless of hue.
                    // The mouse handlers funnel through ApplySpectrumPoint with the mouse
                    // captured, so driving the mapping directly keeps the test deterministic.
                    picker.ApplySpectrumPoint(new Point(0, 0));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 255, 255, 255), picker.Color);
                    Assert.Equal(120d, hueSlider.Value);

                    // Top-right corner: saturation 1, value 1 - the retained hue reappears.
                    picker.ApplySpectrumPoint(new Point(spectrumArea.ActualWidth, 0));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 0, 255, 0), picker.Color);

                    // Bottom edge: value 0 - black.
                    picker.ApplySpectrumPoint(new Point(spectrumArea.ActualWidth, spectrumArea.ActualHeight));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 0, 0, 0), picker.Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_PreviousColor_TogglesPreviousSwatchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    Border currentSwatch = Assert.IsType<Border>(template.FindName("CurrentSwatchBorder", picker));
                    Border previousSwatch = Assert.IsType<Border>(template.FindName("PreviousSwatchBorder", picker));

                    Assert.Equal(Visibility.Collapsed, previousSwatch.Visibility);

                    SolidColorBrush currentBrush = Assert.IsType<SolidColorBrush>(currentSwatch.Background);
                    Assert.Equal(picker.Color, currentBrush.Color);

                    picker.PreviousColor = Colors.Blue;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Visible, previousSwatch.Visibility);
                    SolidColorBrush previousBrush = Assert.IsType<SolidColorBrush>(previousSwatch.Background);
                    Assert.Equal(Colors.Blue, previousBrush.Color);

                    picker.PreviousColor = null;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, previousSwatch.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_AutomationPeer_ReportsClassTypeAndHexNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer peer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(picker));
                    _ = Assert.IsAssignableFrom<Automation.ColorPickerAutomationPeer>(peer);
                    Assert.Equal("ColorPicker", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                    Assert.Equal("#FF0000", peer.GetName(), StringComparer.Ordinal);

                    picker.Color = Color.FromArgb(255, 0, 120, 212);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("#0078D4", peer.GetName(), StringComparer.Ordinal);

                    AutomationProperties.SetName(picker, "Accent color");
                    Assert.Equal("Accent color", peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ColorPicker_SurfaceBrushes_ResolveAfterThemeCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.NotNull(app.TryFindResource("ControlStrokeColorDefaultBrush"));
                Assert.NotNull(app.TryFindResource("TextFillColorPrimaryBrush"));
                Assert.NotNull(app.TryFindResource("AccentFillColorDefaultBrush"));
                Assert.NotNull(app.TryFindResource("ControlFillColorDefaultBrush"));
            });
        }

        // Boilerplate runner for the option-surface tests: constructs the picker on the
        // STA thread, shows it, asserts the template applied, and hands both to verify.
        private static Task RunColorPickerOptionTestAsync(Func<Controls.ColorPicker> createPicker, Action<Controls.ColorPicker, ControlTemplate, Window> verify)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 520, Height = 860 };
                Controls.ColorPicker picker = createPicker();

                try
                {
                    window.Content = picker;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate template = Assert.IsAssignableFrom<ControlTemplate>(picker.Template);
                    verify(picker, template, window);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void RaiseEnterKey(TextBox textBox)
        {
            PresentationSource source = Assert.IsAssignableFrom<PresentationSource>(PresentationSource.FromVisual(textBox));
            textBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
            {
                RoutedEvent = Keyboard.KeyDownEvent,
            });
        }

        private static T GetTemplateElement<T>(ControlTemplate template, Controls.ColorPicker picker, string name) where T : class
        {
            return Assert.IsAssignableFrom<T>(template.FindName(name, picker));
        }

        [Fact]
        public Task ColorPicker_OptionSurfaceDefaults_MatchWinUiAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, _, _) =>
                {
                    Assert.True(picker.IsColorPreviewVisible, "IsColorPreviewVisible must default to true.");
                    Assert.True(picker.IsColorSliderVisible, "IsColorSliderVisible must default to true.");
                    Assert.True(picker.IsHexInputVisible, "IsHexInputVisible must default to true.");
                    Assert.False(picker.IsMoreButtonVisible, "IsMoreButtonVisible must default to false.");
                    Assert.True(picker.IsAlphaSliderVisible, "IsAlphaSliderVisible must default to true.");
                    Assert.True(picker.IsAlphaTextInputVisible, "IsAlphaTextInputVisible must default to true.");
                });
        }

        [Fact]
        public Task ColorPicker_IsColorPreviewVisible_TogglesSwatchSectionAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    FrameworkElement swatchSection = GetTemplateElement<FrameworkElement>(template, picker, "SwatchSection");
                    Assert.Equal(Visibility.Visible, swatchSection.Visibility);

                    picker.IsColorPreviewVisible = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, swatchSection.Visibility);

                    picker.IsColorPreviewVisible = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, swatchSection.Visibility);
                });
        }

        [Fact]
        public Task ColorPicker_IsColorSliderVisible_TogglesHueSectionAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    FrameworkElement hueSection = GetTemplateElement<FrameworkElement>(template, picker, "HueSection");
                    FrameworkElement spectrumSection = GetTemplateElement<FrameworkElement>(template, picker, "SpectrumSection");
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    picker.IsColorSliderVisible = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, hueSection.Visibility);
                    Assert.Equal(Visibility.Visible, spectrumSection.Visibility);
                    Assert.Equal(Visibility.Visible, hexTextBox.Visibility);
                });
        }

        [Fact]
        public Task ColorPicker_IsHexInputVisible_TogglesHexTextBoxOnlyAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");
                    FrameworkElement representationComboBox = GetTemplateElement<FrameworkElement>(template, picker, "ColorRepresentationComboBox");
                    FrameworkElement channelPanel = GetTemplateElement<FrameworkElement>(template, picker, "ColorChannelTextInputPanel");

                    picker.IsHexInputVisible = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, hexTextBox.Visibility);
                    Assert.Equal(Visibility.Visible, representationComboBox.Visibility);
                    Assert.Equal(Visibility.Visible, channelPanel.Visibility);
                });
        }

        [Fact]
        public Task ColorPicker_AlphaVisibilityFlags_AndWithIsAlphaEnabledAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker { IsAlphaEnabled = true },
                (picker, template, window) =>
                {
                    FrameworkElement alphaSection = GetTemplateElement<FrameworkElement>(template, picker, "AlphaSection");
                    FrameworkElement alphaInputPanel = GetTemplateElement<FrameworkElement>(template, picker, "AlphaInputPanel");

                    Assert.Equal(Visibility.Visible, alphaSection.Visibility);
                    Assert.Equal(Visibility.Visible, alphaInputPanel.Visibility);

                    picker.IsAlphaSliderVisible = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, alphaSection.Visibility);
                    Assert.Equal(Visibility.Visible, alphaInputPanel.Visibility);

                    picker.IsAlphaSliderVisible = true;
                    picker.IsAlphaTextInputVisible = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, alphaSection.Visibility);
                    Assert.Equal(Visibility.Collapsed, alphaInputPanel.Visibility);

                    picker.IsAlphaTextInputVisible = true;
                    picker.IsAlphaEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, alphaSection.Visibility);
                    Assert.Equal(Visibility.Collapsed, alphaInputPanel.Visibility);
                });
        }

        [Fact]
        public Task ColorPicker_MoreButton_DefaultCollapsedWithTextEntryVisibleAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, _) =>
                {
                    FrameworkElement moreButton = GetTemplateElement<FrameworkElement>(template, picker, "MoreButton");
                    FrameworkElement textEntryGrid = GetTemplateElement<FrameworkElement>(template, picker, "TextEntryGrid");

                    Assert.Equal(Visibility.Collapsed, moreButton.Visibility);
                    Assert.Equal(Visibility.Visible, textEntryGrid.Visibility);
                });
        }

        [Fact]
        public Task ColorPicker_MoreButton_TogglesTextEntryGridAndLabelAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker { IsMoreButtonVisible = true },
                (picker, template, window) =>
                {
                    Controls.ToggleButton moreButton = GetTemplateElement<Controls.ToggleButton>(template, picker, "MoreButton");
                    FrameworkElement textEntryGrid = GetTemplateElement<FrameworkElement>(template, picker, "TextEntryGrid");
                    TextBlock moreButtonLabel = GetTemplateElement<TextBlock>(template, picker, "MoreButtonLabel");

                    Assert.Equal(Visibility.Visible, moreButton.Visibility);
                    Assert.Equal(Visibility.Collapsed, textEntryGrid.Visibility);
                    Assert.Equal("More", moreButtonLabel.Text, StringComparer.Ordinal);

                    moreButton.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Visible, textEntryGrid.Visibility);
                    Assert.Equal("Less", moreButtonLabel.Text, StringComparer.Ordinal);

                    moreButton.IsChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, textEntryGrid.Visibility);
                    Assert.Equal("More", moreButtonLabel.Text, StringComparer.Ordinal);
                });
        }

        [Fact]
        public Task ColorPicker_ColorRepresentationComboBox_SwapsRgbAndHsvPanelsAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    Controls.ComboBox representationComboBox = GetTemplateElement<Controls.ComboBox>(template, picker, "ColorRepresentationComboBox");
                    FrameworkElement rgbPanel = GetTemplateElement<FrameworkElement>(template, picker, "RgbChannelPanel");
                    FrameworkElement hsvPanel = GetTemplateElement<FrameworkElement>(template, picker, "HsvChannelPanel");

                    Assert.Equal(0, representationComboBox.SelectedIndex);
                    Assert.Equal(Visibility.Visible, rgbPanel.Visibility);
                    Assert.Equal(Visibility.Collapsed, hsvPanel.Visibility);

                    representationComboBox.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, rgbPanel.Visibility);
                    Assert.Equal(Visibility.Visible, hsvPanel.Visibility);
                });
        }

        [Fact]
        public Task ColorPicker_RgbTextEntry_CommitsLivePreservingExactRgbAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox redTextBox = GetTemplateElement<TextBox>(template, picker, "PART_RedTextBox");
                    TextBox greenTextBox = GetTemplateElement<TextBox>(template, picker, "PART_GreenTextBox");
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    Assert.Equal("255", redTextBox.Text, StringComparer.Ordinal);
                    Assert.Equal("0", greenTextBox.Text, StringComparer.Ordinal);

                    redTextBox.Text = "10";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 10, 0, 0), picker.Color);
                    Assert.Equal("10", redTextBox.Text, StringComparer.Ordinal);
                    Assert.Equal("#0A0000", hexTextBox.Text, StringComparer.Ordinal);
                });
        }

        [Fact]
        public Task ColorPicker_HsvTextEntry_GoesThroughHsvModelWithoutQuantizingSiblingsAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    FrameworkElement spectrumArea = GetTemplateElement<FrameworkElement>(template, picker, "PART_SpectrumArea");
                    TextBox hueTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HueTextBox");
                    RangeBase hueSlider = GetTemplateElement<RangeBase>(template, picker, "PART_HueSlider");

                    // Park saturation on a fractional value the integer display would
                    // quantize away (0.503 displays as 50).
                    const double fractionalSaturation = 0.503;
                    picker.ApplySpectrumPoint(new Point(spectrumArea.ActualWidth * fractionalSaturation, 0));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    hueTextBox.Text = "240";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Color expected = Helpers.HsvColorHelper.WithAlpha(
                        Helpers.HsvColorHelper.HsvToRgb(240, fractionalSaturation, 1.0), 255);
                    Color quantized = Helpers.HsvColorHelper.WithAlpha(
                        Helpers.HsvColorHelper.HsvToRgb(240, 0.50, 1.0), 255);

                    Assert.Equal(expected, picker.Color);
                    Assert.NotEqual(quantized, picker.Color);
                    Assert.Equal(240d, hueSlider.Value);
                });
        }

        [Fact]
        public Task ColorPicker_ChannelTextEntry_InvalidInputRestoredOnEnterAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox redTextBox = GetTemplateElement<TextBox>(template, picker, "PART_RedTextBox");

                    redTextBox.Text = "999";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Color.FromArgb(255, 255, 0, 0), picker.Color);

                    RaiseEnterKey(redTextBox);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("255", redTextBox.Text, StringComparer.Ordinal);
                });
        }

        [Fact]
        public Task ColorPicker_AlphaTextEntry_ParsesPercentAndNormalizesAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker { IsAlphaEnabled = true },
                (picker, template, window) =>
                {
                    TextBox alphaTextBox = GetTemplateElement<TextBox>(template, picker, "PART_AlphaTextBox");

                    Assert.Equal("100%", alphaTextBox.Text, StringComparer.Ordinal);

                    alphaTextBox.Text = "50";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(128, picker.Color.A);

                    RaiseEnterKey(alphaTextBox);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("50%", alphaTextBox.Text, StringComparer.Ordinal);

                    alphaTextBox.Text = "200";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(128, picker.Color.A);

                    RaiseEnterKey(alphaTextBox);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("50%", alphaTextBox.Text, StringComparer.Ordinal);
                });
        }

        [Fact]
        public Task ColorPicker_IsColorChannelTextInputVisible_CollapsesChannelPanelNotHexOrAlphaAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker { IsAlphaEnabled = true },
                (picker, template, window) =>
                {
                    FrameworkElement representationComboBox = GetTemplateElement<FrameworkElement>(template, picker, "ColorRepresentationComboBox");
                    FrameworkElement channelPanel = GetTemplateElement<FrameworkElement>(template, picker, "ColorChannelTextInputPanel");
                    FrameworkElement alphaInputPanel = GetTemplateElement<FrameworkElement>(template, picker, "AlphaInputPanel");
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    picker.IsColorChannelTextInputVisible = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(Visibility.Collapsed, representationComboBox.Visibility);
                    Assert.Equal(Visibility.Collapsed, channelPanel.Visibility);
                    Assert.Equal(Visibility.Visible, hexTextBox.Visibility);
                    Assert.Equal(0, Grid.GetColumn(hexTextBox));
                    Assert.Equal(Visibility.Visible, alphaInputPanel.Visibility);
                });
        }

        [Fact]
        public Task ColorPicker_HexMaxLength_TracksIsAlphaEnabledAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    Assert.Equal(7, hexTextBox.MaxLength);

                    picker.IsAlphaEnabled = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(9, hexTextBox.MaxLength);
                });
        }

        [Fact]
        public Task ColorPicker_SpectrumArea_IsFocusableTabStopAsync()
        {
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker(),
                (picker, template, _) =>
                {
                    FrameworkElement spectrumArea = GetTemplateElement<FrameworkElement>(
                        template, picker, "PART_SpectrumArea");

                    Assert.True(spectrumArea.Focusable,
                        "PART_SpectrumArea must be Focusable so keyboard users can reach the spectrum.");
                    Assert.True(KeyboardNavigation.GetIsTabStop(spectrumArea),
                        "PART_SpectrumArea must be a tab stop so it is reachable via Tab.");
                    string automationName = AutomationProperties.GetName(spectrumArea);
                    Assert.False(string.IsNullOrWhiteSpace(automationName),
                        "PART_SpectrumArea must have a non-empty AutomationProperties.Name.");
                    Assert.Equal("Color spectrum", automationName, StringComparer.Ordinal);
                });
        }

        [Fact]
        public Task ColorPicker_SpectrumKeyboard_RightKeyIncreasesSaturationAsync()
        {
            // Start with a mid-saturation color: FromRgb(128, 64, 64) has saturation ~0.5
            // (Max=128, Min=64, S=(128-64)/128=0.5) so pressing Right has room to increase it.
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker { Color = Color.FromRgb(0x80, 0x40, 0x40) },
                (picker, template, window) =>
                {
                    FrameworkElement spectrumArea = GetTemplateElement<FrameworkElement>(
                        template, picker, "PART_SpectrumArea");
                    Color colorBefore = picker.Color;

                    _ = spectrumArea.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    PresentationSource source = Assert.IsAssignableFrom<PresentationSource>(PresentationSource.FromVisual(spectrumArea));

                    spectrumArea.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Color colorAfter = picker.Color;
                    Assert.NotEqual(colorBefore, colorAfter);

                    // For this orange hue saturation steps right -> R channel brightens.
                    Assert.True(
                        colorAfter.R >= colorBefore.R,
                        "Pressing Right on the spectrum must increase saturation, brightening the hue channel.");
                });
        }

        [Fact]
        public Task ColorPicker_SpectrumKeyboard_UpKeyIncreasesValueAsync()
        {
            // Start dark so Value (brightness) has room to increase.
            return RunColorPickerOptionTestAsync(
                () => new Controls.ColorPicker { Color = Color.FromRgb(0x40, 0x20, 0x00) },
                (picker, template, window) =>
                {
                    FrameworkElement spectrumArea = GetTemplateElement<FrameworkElement>(
                        template, picker, "PART_SpectrumArea");
                    Color colorBefore = picker.Color;

                    _ = spectrumArea.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    PresentationSource source = Assert.IsAssignableFrom<PresentationSource>(PresentationSource.FromVisual(spectrumArea));

                    spectrumArea.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Up)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Color colorAfter = picker.Color;
                    Assert.NotEqual(colorBefore, colorAfter);

                    // Value (brightness) increases: at least one channel brightens.
                    Assert.True(
                        colorAfter.R > colorBefore.R || colorAfter.G > colorBefore.G || colorAfter.B > colorBefore.B,
                        "Pressing Up on the spectrum must increase Value, making channels brighter.");
                });
        }
    }
}
