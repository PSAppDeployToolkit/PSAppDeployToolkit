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

using Fluence.Wpf.Helpers;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

// IMPORTANT: every reference to TextBox / Border / Image in this file MUST be fully
// qualified (System.Windows.Controls.TextBox, System.Windows.Controls.Border, ...).
// The Fluence.Wpf.Controls namespace defines its own TextBox / Border subclasses, and
// because this file sits inside that namespace, any unqualified reference resolves to
// the Fluence subclass. The template part contract is typed against the stock WPF base
// types so both the default template (which hosts the Fluence controls) and custom
// templates resolve correctly. See DatePicker.cs for the same pattern.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A control that lets the user pick a color from a saturation/value spectrum, a hue
    /// slider, an optional alpha slider, a current/previous preview swatch row, and an
    /// optional hex text input, mirroring the WinUI 3 <c>ColorPicker</c> essentials.
    /// The picker keeps hue, saturation, value, and alpha as its internal source of truth
    /// so dragging across the grey axis does not accumulate RGB round-trip drift, the
    /// same approach WinUI uses.
    /// </summary>
    /// <remarks>
    /// v1 scope notes: WinUI's <c>IsMoreButtonVisible</c> collapsed-input mode, the
    /// per-channel RGB/HSV number boxes, and the <c>ColorSpectrumComponents</c>
    /// permutations are deliberately omitted. The spectrum is fixed to saturation on the
    /// x axis by value on the y axis at the selected hue, with hue on a horizontal slider.
    /// </remarks>
    [TemplatePart(Name = PART_SpectrumImage, Type = typeof(System.Windows.Controls.Image))]
    [TemplatePart(Name = PART_SpectrumArea, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PART_SpectrumThumb, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PART_HueSlider, Type = typeof(RangeBase))]
    [TemplatePart(Name = PART_AlphaSlider, Type = typeof(RangeBase))]
    [TemplatePart(Name = PART_HexTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    public class ColorPicker : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_SpectrumImage = "PART_SpectrumImage";
        private const string PART_SpectrumArea = "PART_SpectrumArea";
        private const string PART_SpectrumThumb = "PART_SpectrumThumb";
        private const string PART_HueSlider = "PART_HueSlider";
        private const string PART_AlphaSlider = "PART_AlphaSlider";
        private const string PART_HexTextBox = "PART_HexTextBox";

        // Optional named template children. A custom template may omit them; every access
        // is null-guarded. The gradient and checkerboard backgrounds are generated in code
        // because color math belongs in code, not hard-coded template values.
        private const string HueGradientBorderName = "HueGradientBorder";
        private const string AlphaGradientBorderName = "AlphaGradientBorder";
        private const string AlphaCheckerboardBorderName = "AlphaCheckerboardBorder";
        private const string SwatchCheckerboardBorderName = "SwatchCheckerboardBorder";
        private const string CurrentSwatchBorderName = "CurrentSwatchBorder";
        private const string PreviousSwatchBorderName = "PreviousSwatchBorder";

        /// <summary>
        /// Pixel size of the square saturation/value spectrum bitmap.
        /// </summary>
        private const int SpectrumSize = 256;

        // Frozen, theme-independent brushes shared by every picker instance: the
        // checkerboard under translucent surfaces and the hue rainbow track. Both are
        // generated in code; asset/pixel math may use literal channel values, unlike
        // template chrome which must go through canonical theme tokens.
        private static readonly Brush CheckerboardBrush = CreateCheckerboardBrush();
        private static readonly Brush HueRainbowBrush = CreateHueRainbowBrush();

        private System.Windows.Controls.Image? _spectrumImage;
        private FrameworkElement? _spectrumArea;
        private FrameworkElement? _spectrumThumb;
        private RangeBase? _hueSlider;
        private RangeBase? _alphaSlider;
        private System.Windows.Controls.TextBox? _hexTextBox;
        private System.Windows.Controls.Border? _alphaGradientBorder;
        private System.Windows.Controls.Border? _currentSwatchBorder;
        private System.Windows.Controls.Border? _previousSwatchBorder;
        private TranslateTransform? _spectrumThumbTransform;
        private WriteableBitmap? _spectrumBitmap;
        private byte[]? _spectrumPixels;
        private double _spectrumBitmapHue = -1;

        // Internal HSV + alpha model. These fields are the source of truth while the user
        // drags so repeated RGB -> HSV -> RGB conversions do not drift (WinUI does the same).
        private double _hue;
        private double _saturation = 1;
        private double _value = 1;
        private byte _alpha = 255;
        private bool _isUpdatingColor;
        private bool _isUpdatingVisuals;
        private bool _isDraggingSpectrum;

        /// <summary>
        /// Initializes static members of the ColorPicker class and overrides the default
        /// style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(
                    Colors.Red,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnColorChanged));

        /// <summary>
        /// Gets or sets the currently selected color. Defaults to opaque red. Changing this
        /// property raises <see cref="ColorChanged"/>; values assigned from outside the
        /// picker re-derive the internal hue/saturation/value model.
        /// </summary>
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreviousColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviousColorProperty =
            DependencyProperty.Register(
                nameof(PreviousColor),
                typeof(Color?),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(null, OnPreviousColorChanged));

        /// <summary>
        /// Gets or sets the comparison color shown next to the current color in the preview
        /// swatch row, or <see langword="null"/> to hide the comparison swatch.
        /// </summary>
        public Color? PreviousColor
        {
            get => (Color?)GetValue(PreviousColorProperty);
            set => SetValue(PreviousColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsAlphaEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAlphaEnabledProperty =
            DependencyProperty.Register(
                nameof(IsAlphaEnabled),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(false, OnIsAlphaEnabledChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the alpha channel can be edited. When
        /// <see langword="false"/> (the default) the alpha slider row collapses, the hex
        /// input parses and displays six digits, and turning the property off pins the
        /// picker's alpha back to 255. Programmatic <see cref="Color"/> assignments keep
        /// whatever alpha they carry.
        /// </summary>
        public bool IsAlphaEnabled
        {
            get => (bool)GetValue(IsAlphaEnabledProperty);
            set => SetValue(IsAlphaEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsColorSpectrumVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsColorSpectrumVisibleProperty =
            DependencyProperty.Register(
                nameof(IsColorSpectrumVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the saturation/value spectrum square is
        /// shown. The hue and alpha sliders remain available when it is hidden.
        /// </summary>
        public bool IsColorSpectrumVisible
        {
            get => (bool)GetValue(IsColorSpectrumVisibleProperty);
            set => SetValue(IsColorSpectrumVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsColorChannelTextInputVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsColorChannelTextInputVisibleProperty =
            DependencyProperty.Register(
                nameof(IsColorChannelTextInputVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the hex text input row is shown. The
        /// input accepts <c>#RRGGBB</c> and <c>#AARRGGBB</c> (the leading <c>#</c> is
        /// optional) and commits on Enter or when keyboard focus leaves the box.
        /// </summary>
        public bool IsColorChannelTextInputVisible
        {
            get => (bool)GetValue(IsColorChannelTextInputVisibleProperty);
            set => SetValue(IsColorChannelTextInputVisibleProperty, value);
        }

        // Note: WinUI's IsMoreButtonVisible (which collapses the text inputs behind a
        // "More" expander) is deliberately omitted for v1; IsColorChannelTextInputVisible
        // covers the show/hide use case directly.

        /// <summary>
        /// Occurs after <see cref="Color"/> changes, whether through the spectrum, the
        /// sliders, the hex input, or a programmatic update.
        /// </summary>
        public event EventHandler<ColorPickerColorChangedEventArgs>? ColorChanged;

        /// <summary>
        /// Creates the frozen checkerboard brush rendered beneath translucent surfaces
        /// (the alpha slider track and the preview swatches).
        /// </summary>
        private static Brush CreateCheckerboardBrush()
        {
            SolidColorBrush light = new(Color.FromRgb(255, 255, 255));
            light.Freeze();
            SolidColorBrush dark = new(Color.FromRgb(204, 204, 204));
            dark.Freeze();

            GeometryGroup darkSquares = new();
            darkSquares.Children.Add(new RectangleGeometry(new Rect(0, 0, 4, 4)));
            darkSquares.Children.Add(new RectangleGeometry(new Rect(4, 4, 4, 4)));

            DrawingGroup drawing = new();
            drawing.Children.Add(new GeometryDrawing
            {
                Brush = light,
                Geometry = new RectangleGeometry(new Rect(0, 0, 8, 8)),
            });
            drawing.Children.Add(new GeometryDrawing
            {
                Brush = dark,
                Geometry = darkSquares,
            });

            DrawingBrush brush = new(drawing)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 8, 8),
                ViewportUnits = BrushMappingMode.Absolute,
            };
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Creates the frozen hue rainbow brush for the hue slider track. The left edge is
        /// hue 0 and the right edge is hue 360 (which wraps back to red), matching the
        /// slider's Minimum 0 / Maximum 360 left-to-right orientation. Seven stops, one per
        /// 60 degree primary/secondary, all derived through
        /// <see cref="HsvColorHelper.HsvToRgb"/> so no literal colors appear here either.
        /// </summary>
        private static Brush CreateHueRainbowBrush()
        {
            GradientStopCollection stops = [];
            for (int i = 0; i <= 6; i++)
            {
                stops.Add(new GradientStop(HsvColorHelper.HsvToRgb(i * 60.0, 1, 1), i / 6.0));
            }

            LinearGradientBrush brush = new(stops, new Point(0, 0.5), new Point(1, 0.5));
            brush.Freeze();
            return brush;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            // Namespace-qualified to stay clear of the stock WPF automation peers; see
            // DatePicker.OnCreateAutomationPeer for the same pattern.
            return new Automation.ColorPickerAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Unsubscribe-first so re-templating never leaves stale handlers behind.
            if (_spectrumArea is not null)
            {
                _spectrumArea.MouseLeftButtonDown -= OnSpectrumMouseLeftButtonDown;
                _spectrumArea.MouseMove -= OnSpectrumMouseMove;
                _spectrumArea.MouseLeftButtonUp -= OnSpectrumMouseLeftButtonUp;
                _spectrumArea.LostMouseCapture -= OnSpectrumLostMouseCapture;
                _spectrumArea.SizeChanged -= OnSpectrumAreaSizeChanged;
            }

            _hueSlider?.ValueChanged -= OnHueSliderValueChanged;
            _alphaSlider?.ValueChanged -= OnAlphaSliderValueChanged;

            if (_hexTextBox is not null)
            {
                _hexTextBox.KeyDown -= OnHexTextBoxKeyDown;
                _hexTextBox.LostKeyboardFocus -= OnHexTextBoxLostKeyboardFocus;
            }

            base.OnApplyTemplate();

            _spectrumImage = GetTemplateChild(PART_SpectrumImage) as System.Windows.Controls.Image;
            _spectrumArea = GetTemplateChild(PART_SpectrumArea) as FrameworkElement;
            _spectrumThumb = GetTemplateChild(PART_SpectrumThumb) as FrameworkElement;
            _hueSlider = GetTemplateChild(PART_HueSlider) as RangeBase;
            _alphaSlider = GetTemplateChild(PART_AlphaSlider) as RangeBase;
            _hexTextBox = GetTemplateChild(PART_HexTextBox) as System.Windows.Controls.TextBox;
            _alphaGradientBorder = GetTemplateChild(AlphaGradientBorderName) as System.Windows.Controls.Border;
            _currentSwatchBorder = GetTemplateChild(CurrentSwatchBorderName) as System.Windows.Controls.Border;
            _previousSwatchBorder = GetTemplateChild(PreviousSwatchBorderName) as System.Windows.Controls.Border;

            // The hue rainbow and the two checkerboards are static, theme-independent
            // brushes painted once per template apply, so their hosts stay locals here.
            System.Windows.Controls.Border? hueGradientBorder = GetTemplateChild(HueGradientBorderName) as System.Windows.Controls.Border;
            System.Windows.Controls.Border? alphaCheckerboardBorder = GetTemplateChild(AlphaCheckerboardBorderName) as System.Windows.Controls.Border;
            System.Windows.Controls.Border? swatchCheckerboardBorder = GetTemplateChild(SwatchCheckerboardBorderName) as System.Windows.Controls.Border;

            if (_spectrumArea is not null)
            {
                _spectrumArea.MouseLeftButtonDown += OnSpectrumMouseLeftButtonDown;
                _spectrumArea.MouseMove += OnSpectrumMouseMove;
                _spectrumArea.MouseLeftButtonUp += OnSpectrumMouseLeftButtonUp;
                _spectrumArea.LostMouseCapture += OnSpectrumLostMouseCapture;
                _spectrumArea.SizeChanged += OnSpectrumAreaSizeChanged;
            }

            _hueSlider?.ValueChanged += OnHueSliderValueChanged;
            _alphaSlider?.ValueChanged += OnAlphaSliderValueChanged;

            if (_hexTextBox is not null)
            {
                _hexTextBox.KeyDown += OnHexTextBoxKeyDown;
                _hexTextBox.LostKeyboardFocus += OnHexTextBoxLostKeyboardFocus;
            }

            _spectrumThumbTransform = _spectrumThumb is null ? null : new TranslateTransform();
            _spectrumThumb?.SetCurrentValue(RenderTransformProperty, _spectrumThumbTransform);

            hueGradientBorder?.SetCurrentValue(System.Windows.Controls.Border.BackgroundProperty, HueRainbowBrush);
            alphaCheckerboardBorder?.SetCurrentValue(System.Windows.Controls.Border.BackgroundProperty, CheckerboardBrush);
            swatchCheckerboardBorder?.SetCurrentValue(System.Windows.Controls.Border.BackgroundProperty, CheckerboardBrush);

            // Force a regeneration so the (possibly new) PART_SpectrumImage receives the
            // bitmap even when the cached hue already matches.
            _spectrumBitmapHue = -1;
            UpdateSpectrumBitmap();
            UpdateVisuals();
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorPicker picker = (ColorPicker)d;
            picker.OnColorPropertyChanged((Color)e.OldValue, (Color)e.NewValue);
        }

        private static void OnPreviousColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorPicker)d).UpdatePreviousSwatch();
        }

        private static void OnIsAlphaEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorPicker picker = (ColorPicker)d;
            if (e.NewValue is false && picker._alpha != 255)
            {
                // Disabling alpha pins the picker back to fully opaque, like WinUI.
                picker.SetColorFromHsv(picker._hue, picker._saturation, picker._value, 255);
            }
            else
            {
                // Refresh the hex format (six vs eight digits) and the alpha slider sync.
                picker.UpdateVisuals();
            }
        }

        private void OnColorPropertyChanged(Color oldColor, Color newColor)
        {
            if (!_isUpdatingColor)
            {
                // External assignment: re-derive the HSV model from the new RGB value.
                SyncHsvFromColor(newColor);
                UpdateSpectrumBitmap();
                UpdateVisuals();
            }

            ColorChanged?.Invoke(this, new ColorPickerColorChangedEventArgs(oldColor, newColor));
        }

        /// <summary>
        /// Central funnel for every picker-driven change: clamps and stores the HSV + alpha
        /// model, publishes the resulting <see cref="Color"/> through
        /// <see cref="DependencyObject.SetCurrentValue"/> (so bindings survive), and
        /// refreshes the visuals even when the RGB value is unchanged (the spectrum thumb
        /// and hue slider can move while the color stays the same, e.g. on the grey axis).
        /// </summary>
        private void SetColorFromHsv(double hue, double saturation, double value, byte alpha)
        {
            _hue = Math.Max(0, Math.Min(360, hue));
            _saturation = Math.Max(0, Math.Min(1, saturation));
            _value = Math.Max(0, Math.Min(1, value));
            _alpha = alpha;

            Color newColor = HsvColorHelper.WithAlpha(HsvColorHelper.HsvToRgb(_hue, _saturation, _value), _alpha);
            _isUpdatingColor = true;
            try
            {
                SetCurrentValue(ColorProperty, newColor);
            }
            finally
            {
                _isUpdatingColor = false;
            }

            UpdateSpectrumBitmap();
            UpdateVisuals();
        }

        /// <summary>
        /// Re-derives the HSV model from an externally assigned color. Achromatic colors
        /// collapse hue (and black additionally collapses saturation) to zero in the
        /// RGB to HSV conversion; the previous component is retained in those cases so the
        /// spectrum thumb and hue slider do not jump while the color sits on the grey axis.
        /// WinUI's ColorPicker behaves the same way.
        /// </summary>
        private void SyncHsvFromColor(Color color)
        {
            (double hue, double saturation, double value) = HsvColorHelper.RgbToHsv(color);
            if (saturation > 0)
            {
                _hue = hue;
            }

            if (value > 0)
            {
                _saturation = saturation;
            }

            _value = value;
            _alpha = color.A;
        }

        /// <summary>
        /// Regenerates the 256 x 256 saturation/value spectrum bitmap for the current hue.
        /// Per pixel this uses the standard HSV expansion
        /// channel = V * (255 - S * (255 - hueChannel)), so the only full conversion per
        /// regeneration is the pure hue color from <see cref="HsvColorHelper.HsvToRgb"/>.
        /// Skipped when the cached bitmap already matches the hue.
        /// </summary>
        private void UpdateSpectrumBitmap()
        {
            if (_spectrumImage is null)
            {
                return;
            }

            if (_spectrumBitmap is not null && Math.Abs(_spectrumBitmapHue - _hue) < 0.001)
            {
                return;
            }

            _spectrumBitmap ??= new WriteableBitmap(SpectrumSize, SpectrumSize, 96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = _spectrumPixels ??= new byte[SpectrumSize * SpectrumSize * 4];

            Color hueColor = HsvColorHelper.HsvToRgb(_hue, 1, 1);
            double hueRed = hueColor.R;
            double hueGreen = hueColor.G;
            double hueBlue = hueColor.B;

            int index = 0;
            for (int y = 0; y < SpectrumSize; y++)
            {
                double value = 1.0 - (y / (double)(SpectrumSize - 1));
                for (int x = 0; x < SpectrumSize; x++)
                {
                    double saturation = x / (double)(SpectrumSize - 1);
                    pixels[index] = (byte)Math.Round(value * (255.0 - (saturation * (255.0 - hueBlue))));
                    pixels[index + 1] = (byte)Math.Round(value * (255.0 - (saturation * (255.0 - hueGreen))));
                    pixels[index + 2] = (byte)Math.Round(value * (255.0 - (saturation * (255.0 - hueRed))));
                    pixels[index + 3] = 255;
                    index += 4;
                }
            }

            _spectrumBitmap.WritePixels(new Int32Rect(0, 0, SpectrumSize, SpectrumSize), pixels, SpectrumSize * 4, 0);
            _spectrumBitmapHue = _hue;
            _spectrumImage.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, _spectrumBitmap);
        }

        /// <summary>
        /// Pushes the current HSV + alpha model into every visual: slider positions (guarded
        /// so their ValueChanged handlers do not feed back), the spectrum thumb, the alpha
        /// gradient, the preview swatches, and the hex text.
        /// </summary>
        private void UpdateVisuals()
        {
            _isUpdatingVisuals = true;
            try
            {
                _hueSlider?.SetCurrentValue(RangeBase.ValueProperty, _hue);
                _alphaSlider?.SetCurrentValue(RangeBase.ValueProperty, (double)_alpha);
                UpdateSpectrumThumb();
                UpdateAlphaGradient();
                UpdateSwatches();
                UpdateHexText();
            }
            finally
            {
                _isUpdatingVisuals = false;
            }
        }

        /// <summary>
        /// Positions the selection ellipse over the spectrum (x = saturation, y = inverted
        /// value) and flips its ring between white and black based on the luminance of the
        /// color underneath, the same contrast rule WinUI applies to its selection ellipse.
        /// </summary>
        private void UpdateSpectrumThumb()
        {
            if (_spectrumThumb is null || _spectrumArea is null)
            {
                return;
            }

            if (_spectrumThumb is Shape thumbShape)
            {
                Color opaque = HsvColorHelper.HsvToRgb(_hue, _saturation, _value);
                thumbShape.SetCurrentValue(
                    Shape.StrokeProperty,
                    HsvColorHelper.ShouldUseWhiteText(opaque) ? Brushes.White : Brushes.Black);
            }

            if (_spectrumThumbTransform is null)
            {
                return;
            }

            double width = _spectrumArea.ActualWidth;
            double height = _spectrumArea.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _spectrumThumbTransform.X = (_saturation * width) - (_spectrumThumb.ActualWidth / 2);
            _spectrumThumbTransform.Y = ((1 - _value) * height) - (_spectrumThumb.ActualHeight / 2);
        }

        /// <summary>
        /// Rebuilds the alpha slider's transparent-to-opaque gradient over the checkerboard
        /// so the track always previews the current hue/saturation/value.
        /// </summary>
        private void UpdateAlphaGradient()
        {
            if (_alphaGradientBorder is null)
            {
                return;
            }

            Color opaque = HsvColorHelper.HsvToRgb(_hue, _saturation, _value);
            LinearGradientBrush brush = new(HsvColorHelper.WithAlpha(opaque, 0), HsvColorHelper.WithAlpha(opaque, 255), 0.0);
            brush.Freeze();
            _alphaGradientBorder.SetCurrentValue(System.Windows.Controls.Border.BackgroundProperty, brush);
        }

        private void UpdateSwatches()
        {
            if (_currentSwatchBorder is not null)
            {
                SolidColorBrush brush = new(Color);
                brush.Freeze();
                _currentSwatchBorder.SetCurrentValue(System.Windows.Controls.Border.BackgroundProperty, brush);
            }

            UpdatePreviousSwatch();
        }

        private void UpdatePreviousSwatch()
        {
            Color? previous = PreviousColor;
            if (_previousSwatchBorder is null || !previous.HasValue)
            {
                return;
            }

            SolidColorBrush brush = new(previous.Value);
            brush.Freeze();
            _previousSwatchBorder.SetCurrentValue(System.Windows.Controls.Border.BackgroundProperty, brush);
        }

        private void UpdateHexText()
        {
            _hexTextBox?.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, GetHexDisplayText());
        }

        /// <summary>
        /// Formats the current color for the hex input and the automation peer:
        /// <c>#AARRGGBB</c> when <see cref="IsAlphaEnabled"/>, otherwise <c>#RRGGBB</c>.
        /// </summary>
        internal string GetHexDisplayText()
        {
            Color color = Color;
            return IsAlphaEnabled
                ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B)
                : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        /// <summary>
        /// Maps a point inside the spectrum area to saturation (x, 0 to 1 left to right) and
        /// value (y, 1 to 0 top to bottom) and routes it through the central HSV funnel.
        /// Internal so the test suite can drive the drag math deterministically; the mouse
        /// handlers funnel through here.
        /// </summary>
        internal void ApplySpectrumPoint(Point position)
        {
            if (_spectrumArea is null)
            {
                return;
            }

            double width = _spectrumArea.ActualWidth;
            double height = _spectrumArea.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            double saturation = Math.Max(0, Math.Min(1, position.X / width));
            double value = 1 - Math.Max(0, Math.Min(1, position.Y / height));
            SetColorFromHsv(_hue, saturation, value, _alpha);
        }

        private void OnSpectrumMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_spectrumArea is null)
            {
                return;
            }

            _isDraggingSpectrum = _spectrumArea.CaptureMouse();
            ApplySpectrumPoint(e.GetPosition(_spectrumArea));
            e.Handled = true;
        }

        private void OnSpectrumMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSpectrum || _spectrumArea is null)
            {
                return;
            }

            ApplySpectrumPoint(e.GetPosition(_spectrumArea));
        }

        private void OnSpectrumMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingSpectrum)
            {
                return;
            }

            _isDraggingSpectrum = false;
            _spectrumArea?.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnSpectrumLostMouseCapture(object sender, MouseEventArgs e)
        {
            _isDraggingSpectrum = false;
        }

        private void OnSpectrumAreaSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSpectrumThumb();
        }

        private void OnHueSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingVisuals)
            {
                return;
            }

            SetColorFromHsv(e.NewValue, _saturation, _value, _alpha);
        }

        private void OnAlphaSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingVisuals)
            {
                return;
            }

            byte alpha = (byte)Math.Round(Math.Max(0, Math.Min(255, e.NewValue)));
            SetColorFromHsv(_hue, _saturation, _value, alpha);
        }

        private void OnHexTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not Key.Enter)
            {
                return;
            }

            CommitHexText();
            e.Handled = true;
        }

        private void OnHexTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CommitHexText();
        }

        /// <summary>
        /// Parses the hex box; valid input is published directly through the Color DP (no
        /// HSV round trip, so the typed RGB value is preserved exactly), invalid input
        /// reverts the text to the current color.
        /// </summary>
        private void CommitHexText()
        {
            if (_hexTextBox is null)
            {
                return;
            }

            if (TryParseHexColor(_hexTextBox.Text, out Color parsed))
            {
                byte alpha = IsAlphaEnabled ? parsed.A : (byte)255;
                Color target = Color.FromArgb(alpha, parsed.R, parsed.G, parsed.B);
                SetCurrentValue(ColorProperty, target);
            }

            // Normalize valid input and revert invalid input to the current color.
            UpdateHexText();
        }

        /// <summary>
        /// Parses <c>#RRGGBB</c> / <c>#AARRGGBB</c> (leading <c>#</c> optional). Six-digit
        /// input is treated as fully opaque.
        /// </summary>
        private static bool TryParseHexColor(string text, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string hex = text.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length is not (6 or 8))
            {
                return false;
            }

            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
            {
                return false;
            }

            if (hex.Length == 6)
            {
                argb |= 0xFF000000;
            }

            color = Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF));
            return true;
        }
    }
}
