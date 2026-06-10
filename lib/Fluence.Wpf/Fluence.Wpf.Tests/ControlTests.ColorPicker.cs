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
            RunOnStaThread(() =>
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
            RunOnStaThread(() =>
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
    }
}
