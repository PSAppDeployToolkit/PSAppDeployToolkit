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
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        [TestMethod]
        public void ColorPicker_DefaultStyle_AppliesTemplateParts()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.ColorPicker)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.ColorPicker.");

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");

                    Image? spectrumImage = template.FindName("PART_SpectrumImage", picker) as Image;
                    FrameworkElement? spectrumArea = template.FindName("PART_SpectrumArea", picker) as FrameworkElement;
                    FrameworkElement? spectrumThumb = template.FindName("PART_SpectrumThumb", picker) as FrameworkElement;
                    RangeBase? hueSlider = template.FindName("PART_HueSlider", picker) as RangeBase;
                    RangeBase? alphaSlider = template.FindName("PART_AlphaSlider", picker) as RangeBase;
                    TextBox? hexTextBox = template.FindName("PART_HexTextBox", picker) as TextBox;

                    Assert.IsNotNull(spectrumImage, "PART_SpectrumImage must be an Image hosting the spectrum bitmap.");
                    Assert.IsNotNull(spectrumArea, "PART_SpectrumArea must be present as the spectrum input surface.");
                    Assert.IsNotNull(spectrumThumb, "PART_SpectrumThumb must be present as the selection ellipse.");
                    Assert.IsNotNull(hueSlider, "PART_HueSlider must be a RangeBase hosting the hue channel.");
                    Assert.IsNotNull(alphaSlider, "PART_AlphaSlider must be a RangeBase hosting the alpha channel.");
                    Assert.IsNotNull(hexTextBox, "PART_HexTextBox must be a TextBox hosting the hex input.");
                    _ = Assert.IsInstanceOfType<Controls.TextBox>(hexTextBox,
                        "The default template should present the hex input through the Fluence TextBox.");

                    Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), picker.Color,
                        "The default Color must be opaque red (#FFFF0000).");
                    Assert.AreEqual(0d, hueSlider.Minimum, "The hue slider must span 0 to 360 degrees.");
                    Assert.AreEqual(360d, hueSlider.Maximum, "The hue slider must span 0 to 360 degrees.");
                    Assert.AreEqual(0d, alphaSlider.Minimum, "The alpha slider must span 0 to 255.");
                    Assert.AreEqual(255d, alphaSlider.Maximum, "The alpha slider must span 0 to 255.");
                    Assert.IsNull(picker.PreviousColor, "PreviousColor must default to null.");
                    Assert.IsFalse(picker.IsAlphaEnabled, "IsAlphaEnabled must default to false.");
                    Assert.IsTrue(picker.IsColorSpectrumVisible, "IsColorSpectrumVisible must default to true.");
                    Assert.IsTrue(picker.IsColorChannelTextInputVisible,
                        "IsColorChannelTextInputVisible must default to true.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_SpectrumBitmap_IsGenerated256x256AfterTemplateApply()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
                    Image? spectrumImage = template.FindName("PART_SpectrumImage", picker) as Image;
                    Assert.IsNotNull(spectrumImage, "PART_SpectrumImage must be present in the template.");

                    WriteableBitmap? bitmap = spectrumImage.Source as WriteableBitmap;
                    Assert.IsNotNull(bitmap, "The spectrum image must be backed by a WriteableBitmap after template apply.");
                    Assert.AreEqual(256, bitmap.PixelWidth, "The spectrum bitmap must be 256 pixels wide.");
                    Assert.AreEqual(256, bitmap.PixelHeight, "The spectrum bitmap must be 256 pixels tall.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_SetColor_RaisesColorChangedAndUpdatesHexText()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
                    TextBox? hexTextBox = template.FindName("PART_HexTextBox", picker) as TextBox;
                    Assert.IsNotNull(hexTextBox, "PART_HexTextBox must be present in the template.");
                    Assert.AreEqual("#FF0000", hexTextBox.Text,
                        "The hex box must show the six-digit default color while alpha is disabled.");

                    ColorPickerColorChangedEventArgs? changed = null;
                    int raiseCount = 0;
                    picker.ColorChanged += (sender, e) =>
                    {
                        changed = e;
                        raiseCount++;
                    };

                    Color target = Color.FromArgb(255, 0, 120, 212);
                    picker.Color = target;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1, raiseCount, "A single Color assignment must raise ColorChanged exactly once.");
                    Assert.IsNotNull(changed, "ColorChanged must supply event args.");
                    Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), changed.OldColor,
                        "ColorChanged must report the previous color as OldColor.");
                    Assert.AreEqual(target, changed.NewColor, "ColorChanged must report the assigned color as NewColor.");
                    Assert.AreEqual("#0078D4", hexTextBox.Text,
                        "A programmatic Color assignment must refresh the hex box text.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_HexEntry_CommitsOnEnterAndInvalidInputReverts()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
                    TextBox? hexTextBox = template.FindName("PART_HexTextBox", picker) as TextBox;
                    Assert.IsNotNull(hexTextBox, "PART_HexTextBox must be present in the template.");

                    PresentationSource? source = PresentationSource.FromVisual(hexTextBox);
                    Assert.IsNotNull(source, "The hex box must have a presentation source once the window is shown.");

                    hexTextBox.Text = "#FF0078D4";
                    hexTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 0, 120, 212), picker.Color,
                        "An eight-digit hex entry must commit on Enter; with alpha disabled it stays opaque.");
                    Assert.AreEqual("#0078D4", hexTextBox.Text,
                        "Committed input must normalize to the six-digit display while alpha is disabled.");

                    hexTextBox.Text = "00b294";
                    hexTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 0, 178, 148), picker.Color,
                        "Six-digit lowercase input without a leading # must still commit.");
                    Assert.AreEqual("#00B294", hexTextBox.Text, "Committed input must normalize to uppercase with a #.");

                    hexTextBox.Text = "not-a-color";
                    hexTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 0, 178, 148), picker.Color,
                        "Invalid hex input must not change the color.");
                    Assert.AreEqual("#00B294", hexTextBox.Text,
                        "Invalid hex input must revert the text to the current color.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_HueSlider_UpdatesColorAtFullSaturationAndValue()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
                    RangeBase? hueSlider = template.FindName("PART_HueSlider", picker) as RangeBase;
                    TextBox? hexTextBox = template.FindName("PART_HexTextBox", picker) as TextBox;
                    Assert.IsNotNull(hueSlider, "PART_HueSlider must be present in the template.");
                    Assert.IsNotNull(hexTextBox, "PART_HexTextBox must be present in the template.");

                    ColorPickerColorChangedEventArgs? changed = null;
                    picker.ColorChanged += (sender, e) => changed = e;

                    // The default red sits at the S=1, V=1 fixed point, so a hue change maps
                    // exactly onto the pure hue color.
                    hueSlider.Value = 120;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 0, 255, 0), picker.Color,
                        "Hue 120 at S=1, V=1 must produce pure green.");
                    Assert.IsNotNull(changed, "A hue slider change must raise ColorChanged.");
                    Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), changed.OldColor,
                        "ColorChanged must report the pre-drag color as OldColor.");
                    Assert.AreEqual(Color.FromArgb(255, 0, 255, 0), changed.NewColor,
                        "ColorChanged must report the post-drag color as NewColor.");
                    Assert.AreEqual("#00FF00", hexTextBox.Text, "The hex box must follow hue slider edits.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_AlphaSlider_CollapsedByDefaultAndFunctionalWhenEnabled()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
                    FrameworkElement? alphaSection = template.FindName("AlphaSection", picker) as FrameworkElement;
                    RangeBase? alphaSlider = template.FindName("PART_AlphaSlider", picker) as RangeBase;
                    TextBox? hexTextBox = template.FindName("PART_HexTextBox", picker) as TextBox;
                    Assert.IsNotNull(alphaSection, "AlphaSection must be present in the default template.");
                    Assert.IsNotNull(alphaSlider, "PART_AlphaSlider must be present in the template.");
                    Assert.IsNotNull(hexTextBox, "PART_HexTextBox must be present in the template.");

                    Assert.AreEqual(Visibility.Collapsed, alphaSection.Visibility,
                        "The alpha row must be collapsed while IsAlphaEnabled is false.");

                    picker.IsAlphaEnabled = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Visible, alphaSection.Visibility,
                        "The alpha row must show once IsAlphaEnabled is true.");
                    Assert.AreEqual("#FFFF0000", hexTextBox.Text,
                        "Enabling alpha must switch the hex box to the eight-digit format.");

                    alphaSlider.Value = 128;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(128, 255, 0, 0), picker.Color,
                        "An alpha slider edit must flow into Color.A while RGB is preserved.");
                    Assert.AreEqual("#80FF0000", hexTextBox.Text,
                        "The hex box must show the edited alpha in the eight-digit format.");

                    picker.IsAlphaEnabled = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, alphaSection.Visibility,
                        "The alpha row must collapse again when IsAlphaEnabled returns to false.");
                    Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), picker.Color,
                        "Disabling alpha must pin the picker back to a fully opaque color.");
                    Assert.AreEqual("#FF0000", hexTextBox.Text,
                        "Disabling alpha must switch the hex box back to the six-digit format.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_SpectrumPoint_UpdatesSaturationAndValuePreservingHue()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
                    FrameworkElement? spectrumArea = template.FindName("PART_SpectrumArea", picker) as FrameworkElement;
                    RangeBase? hueSlider = template.FindName("PART_HueSlider", picker) as RangeBase;
                    Assert.IsNotNull(spectrumArea, "PART_SpectrumArea must be present in the template.");
                    Assert.IsNotNull(hueSlider, "PART_HueSlider must be present in the template.");
                    Assert.IsTrue(spectrumArea.ActualWidth > 0 && spectrumArea.ActualHeight > 0,
                        "The spectrum area must have a layout size once the window is shown.");

                    hueSlider.Value = 120;
                    DrainDispatcher(window.Dispatcher);

                    // Top-left corner: saturation 0, value 1 - white, regardless of hue.
                    // The mouse handlers funnel through ApplySpectrumPoint with the mouse
                    // captured, so driving the mapping directly keeps the test deterministic.
                    picker.ApplySpectrumPoint(new Point(0, 0));
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 255, 255, 255), picker.Color,
                        "The top-left spectrum corner must map to white (S=0, V=1).");
                    Assert.AreEqual(120d, hueSlider.Value,
                        "Moving onto the grey axis must not reset the hue channel.");

                    // Top-right corner: saturation 1, value 1 - the retained hue reappears.
                    picker.ApplySpectrumPoint(new Point(spectrumArea.ActualWidth, 0));
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 0, 255, 0), picker.Color,
                        "The top-right spectrum corner must restore the retained hue at S=1, V=1.");

                    // Bottom edge: value 0 - black.
                    picker.ApplySpectrumPoint(new Point(spectrumArea.ActualWidth, spectrumArea.ActualHeight));
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 0, 0, 0), picker.Color,
                        "The bottom spectrum edge must map to black (V=0).");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_PreviousColor_TogglesPreviousSwatch()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
                    Border? currentSwatch = template.FindName("CurrentSwatchBorder", picker) as Border;
                    Border? previousSwatch = template.FindName("PreviousSwatchBorder", picker) as Border;
                    Assert.IsNotNull(currentSwatch, "CurrentSwatchBorder must be present in the default template.");
                    Assert.IsNotNull(previousSwatch, "PreviousSwatchBorder must be present in the default template.");

                    Assert.AreEqual(Visibility.Collapsed, previousSwatch.Visibility,
                        "The previous swatch must be collapsed while PreviousColor is null.");

                    SolidColorBrush? currentBrush = currentSwatch.Background as SolidColorBrush;
                    Assert.IsNotNull(currentBrush, "The current swatch must show the current color.");
                    Assert.AreEqual(picker.Color, currentBrush.Color,
                        "The current swatch brush must match the current color.");

                    picker.PreviousColor = Colors.Blue;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Visible, previousSwatch.Visibility,
                        "The previous swatch must show once PreviousColor is set.");
                    SolidColorBrush? previousBrush = previousSwatch.Background as SolidColorBrush;
                    Assert.IsNotNull(previousBrush, "The previous swatch must show the previous color.");
                    Assert.AreEqual(Colors.Blue, previousBrush.Color,
                        "The previous swatch brush must match PreviousColor.");

                    picker.PreviousColor = null;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, previousSwatch.Visibility,
                        "Clearing PreviousColor must collapse the previous swatch again.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_AutomationPeer_ReportsClassTypeAndHexName()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 640 };
                Controls.ColorPicker picker = new();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(picker);
                    Assert.IsNotNull(peer, "ColorPicker must create an automation peer.");
                    _ = Assert.IsInstanceOfType<Automation.ColorPickerAutomationPeer>(peer,
                        "ColorPicker must expose the ColorPickerAutomationPeer.");
                    Assert.AreEqual("ColorPicker", peer.GetClassName(), "The peer must report the ColorPicker class name.");
                    Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType(),
                        "The peer must report the Group control type.");
                    Assert.AreEqual("#FF0000", peer.GetName(),
                        "The peer name must fall back to the hex string of the current color.");

                    picker.Color = Color.FromArgb(255, 0, 120, 212);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual("#0078D4", peer.GetName(), "The peer name must track the current color.");

                    AutomationProperties.SetName(picker, "Accent color");
                    Assert.AreEqual("Accent color", peer.GetName(),
                        "An explicit AutomationProperties.Name must win over the hex fallback.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ColorPicker_SurfaceBrushes_ResolveAfterThemeCycle()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                Assert.IsNotNull(app?.TryFindResource("ControlStrokeColorDefaultBrush"),
                    "ControlStrokeColorDefaultBrush (spectrum, track, and swatch outlines) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("TextFillColorPrimaryBrush"),
                    "TextFillColorPrimaryBrush (control foreground) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("AccentFillColorDefaultBrush"),
                    "AccentFillColorDefaultBrush (channel slider thumb fill) must resolve after a full theme cycle.");
                Assert.IsNotNull(app?.TryFindResource("ControlFillColorDefaultBrush"),
                    "ControlFillColorDefaultBrush (hex text box fill) must resolve after a full theme cycle.");
            });
        }

        // Boilerplate runner for the option-surface tests: constructs the picker on the
        // STA thread, shows it, asserts the template applied, and hands both to verify.
        private static void RunColorPickerOptionTest(Func<Controls.ColorPicker> createPicker, Action<Controls.ColorPicker, ControlTemplate, Window> verify)
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 520, Height = 860 };
                Controls.ColorPicker picker = createPicker();

                try
                {
                    window.Content = picker;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "ColorPicker must receive its themed template.");
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
            PresentationSource? source = PresentationSource.FromVisual(textBox);
            Assert.IsNotNull(source, "The text box must have a presentation source once the window is shown.");
            textBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
            {
                RoutedEvent = Keyboard.KeyDownEvent,
            });
        }

        private static T GetTemplateElement<T>(ControlTemplate template, Controls.ColorPicker picker, string name) where T : class
        {
            T? element = template.FindName(name, picker) as T;
            Assert.IsNotNull(element, name + " must be present in the default template.");
            return element;
        }

        [TestMethod]
        public void ColorPicker_OptionSurfaceDefaults_MatchWinUi()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, _, _) =>
                {
                    Assert.IsTrue(picker.IsColorPreviewVisible, "IsColorPreviewVisible must default to true.");
                    Assert.IsTrue(picker.IsColorSliderVisible, "IsColorSliderVisible must default to true.");
                    Assert.IsTrue(picker.IsHexInputVisible, "IsHexInputVisible must default to true.");
                    Assert.IsFalse(picker.IsMoreButtonVisible, "IsMoreButtonVisible must default to false.");
                    Assert.IsTrue(picker.IsAlphaSliderVisible, "IsAlphaSliderVisible must default to true.");
                    Assert.IsTrue(picker.IsAlphaTextInputVisible, "IsAlphaTextInputVisible must default to true.");
                });
        }

        [TestMethod]
        public void ColorPicker_IsColorPreviewVisible_TogglesSwatchSection()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    FrameworkElement swatchSection = GetTemplateElement<FrameworkElement>(template, picker, "SwatchSection");
                    Assert.AreEqual(Visibility.Visible, swatchSection.Visibility);

                    picker.IsColorPreviewVisible = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Collapsed, swatchSection.Visibility,
                        "Turning IsColorPreviewVisible off must collapse the swatch row.");

                    picker.IsColorPreviewVisible = true;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Visible, swatchSection.Visibility);
                });
        }

        [TestMethod]
        public void ColorPicker_IsColorSliderVisible_TogglesHueSection()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    FrameworkElement hueSection = GetTemplateElement<FrameworkElement>(template, picker, "HueSection");
                    FrameworkElement spectrumSection = GetTemplateElement<FrameworkElement>(template, picker, "SpectrumSection");
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    picker.IsColorSliderVisible = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, hueSection.Visibility,
                        "Turning IsColorSliderVisible off must collapse the third-dimension hue slider row.");
                    Assert.AreEqual(Visibility.Visible, spectrumSection.Visibility,
                        "Hiding the color slider must not affect the spectrum.");
                    Assert.AreEqual(Visibility.Visible, hexTextBox.Visibility,
                        "Hiding the color slider must not affect the hex input.");
                });
        }

        [TestMethod]
        public void ColorPicker_IsHexInputVisible_TogglesHexTextBoxOnly()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");
                    FrameworkElement representationComboBox = GetTemplateElement<FrameworkElement>(template, picker, "ColorRepresentationComboBox");
                    FrameworkElement channelPanel = GetTemplateElement<FrameworkElement>(template, picker, "ColorChannelTextInputPanel");

                    picker.IsHexInputVisible = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, hexTextBox.Visibility,
                        "Turning IsHexInputVisible off must collapse the hex box.");
                    Assert.AreEqual(Visibility.Visible, representationComboBox.Visibility,
                        "Hiding the hex input must not affect the representation selector.");
                    Assert.AreEqual(Visibility.Visible, channelPanel.Visibility,
                        "Hiding the hex input must not affect the channel inputs.");
                });
        }

        [TestMethod]
        public void ColorPicker_AlphaVisibilityFlags_AndWithIsAlphaEnabled()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker { IsAlphaEnabled = true },
                (picker, template, window) =>
                {
                    FrameworkElement alphaSection = GetTemplateElement<FrameworkElement>(template, picker, "AlphaSection");
                    FrameworkElement alphaInputPanel = GetTemplateElement<FrameworkElement>(template, picker, "AlphaInputPanel");

                    Assert.AreEqual(Visibility.Visible, alphaSection.Visibility, "Alpha enabled: the slider row shows.");
                    Assert.AreEqual(Visibility.Visible, alphaInputPanel.Visibility, "Alpha enabled: the alpha input shows.");

                    picker.IsAlphaSliderVisible = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Collapsed, alphaSection.Visibility,
                        "IsAlphaSliderVisible=false must collapse the slider row even while alpha is enabled.");
                    Assert.AreEqual(Visibility.Visible, alphaInputPanel.Visibility,
                        "IsAlphaSliderVisible must not affect the alpha text input.");

                    picker.IsAlphaSliderVisible = true;
                    picker.IsAlphaTextInputVisible = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Visible, alphaSection.Visibility,
                        "IsAlphaTextInputVisible must not affect the alpha slider row.");
                    Assert.AreEqual(Visibility.Collapsed, alphaInputPanel.Visibility,
                        "IsAlphaTextInputVisible=false must collapse the alpha text input.");

                    picker.IsAlphaTextInputVisible = true;
                    picker.IsAlphaEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Collapsed, alphaSection.Visibility,
                        "Disabling alpha must collapse the slider row regardless of the visibility flags.");
                    Assert.AreEqual(Visibility.Collapsed, alphaInputPanel.Visibility,
                        "Disabling alpha must collapse the alpha text input regardless of the visibility flags.");
                });
        }

        [TestMethod]
        public void ColorPicker_MoreButton_DefaultCollapsedWithTextEntryVisible()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, _) =>
                {
                    FrameworkElement moreButton = GetTemplateElement<FrameworkElement>(template, picker, "MoreButton");
                    FrameworkElement textEntryGrid = GetTemplateElement<FrameworkElement>(template, picker, "TextEntryGrid");

                    Assert.AreEqual(Visibility.Collapsed, moreButton.Visibility,
                        "With IsMoreButtonVisible false (the default) no More toggle is shown.");
                    Assert.AreEqual(Visibility.Visible, textEntryGrid.Visibility,
                        "With IsMoreButtonVisible false the text-entry area is always visible.");
                });
        }

        [TestMethod]
        public void ColorPicker_MoreButton_TogglesTextEntryGridAndLabel()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker { IsMoreButtonVisible = true },
                (picker, template, window) =>
                {
                    Controls.ToggleButton moreButton = GetTemplateElement<Controls.ToggleButton>(template, picker, "MoreButton");
                    FrameworkElement textEntryGrid = GetTemplateElement<FrameworkElement>(template, picker, "TextEntryGrid");
                    TextBlock moreButtonLabel = GetTemplateElement<TextBlock>(template, picker, "MoreButtonLabel");

                    Assert.AreEqual(Visibility.Visible, moreButton.Visibility, "IsMoreButtonVisible must show the toggle.");
                    Assert.AreEqual(Visibility.Collapsed, textEntryGrid.Visibility,
                        "In More mode the text-entry grid stays collapsed until the toggle is checked.");
                    Assert.AreEqual("More", moreButtonLabel.Text);

                    moreButton.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Visible, textEntryGrid.Visibility,
                        "Checking the More toggle must reveal the text-entry grid.");
                    Assert.AreEqual("Less", moreButtonLabel.Text, "The checked toggle must read Less.");

                    moreButton.IsChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, textEntryGrid.Visibility,
                        "Unchecking the More toggle must collapse the text-entry grid again.");
                    Assert.AreEqual("More", moreButtonLabel.Text);
                });
        }

        [TestMethod]
        public void ColorPicker_ColorRepresentationComboBox_SwapsRgbAndHsvPanels()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    Controls.ComboBox representationComboBox = GetTemplateElement<Controls.ComboBox>(template, picker, "ColorRepresentationComboBox");
                    FrameworkElement rgbPanel = GetTemplateElement<FrameworkElement>(template, picker, "RgbChannelPanel");
                    FrameworkElement hsvPanel = GetTemplateElement<FrameworkElement>(template, picker, "HsvChannelPanel");

                    Assert.AreEqual(0, representationComboBox.SelectedIndex, "The selector must default to RGB.");
                    Assert.AreEqual(Visibility.Visible, rgbPanel.Visibility);
                    Assert.AreEqual(Visibility.Collapsed, hsvPanel.Visibility);

                    representationComboBox.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, rgbPanel.Visibility,
                        "Selecting HSV must collapse the RGB channel panel.");
                    Assert.AreEqual(Visibility.Visible, hsvPanel.Visibility,
                        "Selecting HSV must reveal the HSV channel panel.");
                });
        }

        [TestMethod]
        public void ColorPicker_RgbTextEntry_CommitsLivePreservingExactRgb()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox redTextBox = GetTemplateElement<TextBox>(template, picker, "PART_RedTextBox");
                    TextBox greenTextBox = GetTemplateElement<TextBox>(template, picker, "PART_GreenTextBox");
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    Assert.AreEqual("255", redTextBox.Text, "The red box must show the default color's channel.");
                    Assert.AreEqual("0", greenTextBox.Text, "The green box must show the default color's channel.");

                    redTextBox.Text = "10";
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 10, 0, 0), picker.Color,
                        "A red channel edit must commit live with the typed value preserved exactly, no Enter needed.");
                    Assert.AreEqual("10", redTextBox.Text,
                        "The live commit must not rewrite the box the user is typing in.");
                    Assert.AreEqual("#0A0000", hexTextBox.Text, "The hex box must follow the live channel commit.");
                });
        }

        [TestMethod]
        public void ColorPicker_HsvTextEntry_GoesThroughHsvModelWithoutQuantizingSiblings()
        {
            RunColorPickerOptionTest(
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
                    DrainDispatcher(window.Dispatcher);

                    hueTextBox.Text = "240";
                    DrainDispatcher(window.Dispatcher);

                    Color expected = Helpers.HsvColorHelper.WithAlpha(
                        Helpers.HsvColorHelper.HsvToRgb(240, fractionalSaturation, 1.0), 255);
                    Color quantized = Helpers.HsvColorHelper.WithAlpha(
                        Helpers.HsvColorHelper.HsvToRgb(240, 0.50, 1.0), 255);

                    Assert.AreEqual(expected, picker.Color,
                        "A hue text edit must replace only the hue component, keeping the fractional saturation.");
                    Assert.AreNotEqual(quantized, picker.Color,
                        "The untouched saturation must not be quantized to the displayed integer percentage.");
                    Assert.AreEqual(240d, hueSlider.Value, "The hue slider must follow the hue text edit.");
                });
        }

        [TestMethod]
        public void ColorPicker_ChannelTextEntry_InvalidInputRestoredOnEnter()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox redTextBox = GetTemplateElement<TextBox>(template, picker, "PART_RedTextBox");

                    redTextBox.Text = "999";
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), picker.Color,
                        "An out-of-range channel entry must not change the color.");

                    RaiseEnterKey(redTextBox);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual("255", redTextBox.Text,
                        "Enter must restore invalid channel text from the current color.");
                });
        }

        [TestMethod]
        public void ColorPicker_AlphaTextEntry_ParsesPercentAndNormalizes()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker { IsAlphaEnabled = true },
                (picker, template, window) =>
                {
                    TextBox alphaTextBox = GetTemplateElement<TextBox>(template, picker, "PART_AlphaTextBox");

                    Assert.AreEqual("100%", alphaTextBox.Text, "The alpha box must display a percentage with a percent sign.");

                    alphaTextBox.Text = "50";
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(128, picker.Color.A, "A 50 percent alpha entry must map to alpha byte 128.");

                    RaiseEnterKey(alphaTextBox);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual("50%", alphaTextBox.Text, "Enter must normalize the alpha text with the percent sign.");

                    alphaTextBox.Text = "200";
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(128, picker.Color.A, "An out-of-range alpha entry must not change the alpha.");

                    RaiseEnterKey(alphaTextBox);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual("50%", alphaTextBox.Text, "Enter must restore invalid alpha text from the model.");
                });
        }

        [TestMethod]
        public void ColorPicker_IsColorChannelTextInputVisible_CollapsesChannelPanelNotHexOrAlpha()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker { IsAlphaEnabled = true },
                (picker, template, window) =>
                {
                    FrameworkElement representationComboBox = GetTemplateElement<FrameworkElement>(template, picker, "ColorRepresentationComboBox");
                    FrameworkElement channelPanel = GetTemplateElement<FrameworkElement>(template, picker, "ColorChannelTextInputPanel");
                    FrameworkElement alphaInputPanel = GetTemplateElement<FrameworkElement>(template, picker, "AlphaInputPanel");
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    picker.IsColorChannelTextInputVisible = false;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, representationComboBox.Visibility,
                        "Hiding the channel input must collapse the representation selector.");
                    Assert.AreEqual(Visibility.Collapsed, channelPanel.Visibility,
                        "Hiding the channel input must collapse the channel panel.");
                    Assert.AreEqual(Visibility.Visible, hexTextBox.Visibility,
                        "Hiding the channel input must leave the hex input visible (governed by IsHexInputVisible).");
                    Assert.AreEqual(0, System.Windows.Controls.Grid.GetColumn(hexTextBox),
                        "With the channel input hidden the hex box shifts into the freed left column, matching WinUI.");
                    Assert.AreEqual(Visibility.Visible, alphaInputPanel.Visibility,
                        "Hiding the channel input must leave the alpha input visible, matching WinUI.");
                });
        }

        [TestMethod]
        public void ColorPicker_HexMaxLength_TracksIsAlphaEnabled()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, window) =>
                {
                    TextBox hexTextBox = GetTemplateElement<TextBox>(template, picker, "PART_HexTextBox");

                    Assert.AreEqual(7, hexTextBox.MaxLength,
                        "With alpha disabled the hex box must cap at seven characters (#RRGGBB).");

                    picker.IsAlphaEnabled = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(9, hexTextBox.MaxLength,
                        "With alpha enabled the hex box must cap at nine characters (#AARRGGBB).");
                });
        }

        [TestMethod]
        public void ColorPicker_SpectrumArea_IsFocusableTabStop()
        {
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker(),
                (picker, template, _) =>
                {
                    FrameworkElement spectrumArea = GetTemplateElement<FrameworkElement>(
                        template, picker, "PART_SpectrumArea");

                    Assert.IsTrue(spectrumArea.Focusable,
                        "PART_SpectrumArea must be Focusable so keyboard users can reach the spectrum.");
                    Assert.IsTrue(KeyboardNavigation.GetIsTabStop(spectrumArea),
                        "PART_SpectrumArea must be a tab stop so it is reachable via Tab.");
                    string automationName = AutomationProperties.GetName(spectrumArea);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(automationName),
                        "PART_SpectrumArea must have a non-empty AutomationProperties.Name.");
                    Assert.AreEqual("Color spectrum", automationName,
                        "The AutomationProperties.Name on PART_SpectrumArea must be \"Color spectrum\".");
                });
        }

        [TestMethod]
        public void ColorPicker_SpectrumKeyboard_RightKeyIncreasesSaturation()
        {
            // Start with a mid-saturation color: FromRgb(128, 64, 64) has saturation ~0.5
            // (Max=128, Min=64, S=(128-64)/128=0.5) so pressing Right has room to increase it.
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker { Color = Color.FromRgb(0x80, 0x40, 0x40) },
                (picker, template, window) =>
                {
                    FrameworkElement spectrumArea = GetTemplateElement<FrameworkElement>(
                        template, picker, "PART_SpectrumArea");
                    Color colorBefore = picker.Color;

                    _ = spectrumArea.Focus();
                    DrainDispatcher(window.Dispatcher);

                    PresentationSource? source = PresentationSource.FromVisual(spectrumArea);
                    Assert.IsNotNull(source,
                        "PART_SpectrumArea must have a PresentationSource once the window is shown.");

                    spectrumArea.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    DrainDispatcher(window.Dispatcher);

                    Color colorAfter = picker.Color;
                    Assert.AreNotEqual(colorBefore, colorAfter,
                        "Key.Right on the focused spectrum must change the color.");

                    // For this orange hue saturation steps right -> R channel brightens.
                    Assert.IsTrue(
                        colorAfter.R >= colorBefore.R,
                        "Pressing Right on the spectrum must increase saturation, brightening the hue channel.");
                });
        }

        [TestMethod]
        public void ColorPicker_SpectrumKeyboard_UpKeyIncreasesValue()
        {
            // Start dark so Value (brightness) has room to increase.
            RunColorPickerOptionTest(
                () => new Controls.ColorPicker { Color = Color.FromRgb(0x40, 0x20, 0x00) },
                (picker, template, window) =>
                {
                    FrameworkElement spectrumArea = GetTemplateElement<FrameworkElement>(
                        template, picker, "PART_SpectrumArea");
                    Color colorBefore = picker.Color;

                    _ = spectrumArea.Focus();
                    DrainDispatcher(window.Dispatcher);

                    PresentationSource? source = PresentationSource.FromVisual(spectrumArea);
                    Assert.IsNotNull(source,
                        "PART_SpectrumArea must have a PresentationSource once the window is shown.");

                    spectrumArea.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Up)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    DrainDispatcher(window.Dispatcher);

                    Color colorAfter = picker.Color;
                    Assert.AreNotEqual(colorBefore, colorAfter,
                        "Key.Up on the focused spectrum must change the color.");

                    // Value (brightness) increases: at least one channel brightens.
                    Assert.IsTrue(
                        colorAfter.R > colorBefore.R || colorAfter.G > colorBefore.G || colorAfter.B > colorBefore.B,
                        "Pressing Up on the spectrum must increase Value, making channels brighter.");
                });
        }
    }
}
