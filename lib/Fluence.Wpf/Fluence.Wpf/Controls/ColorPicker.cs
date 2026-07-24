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
    /// slider, an optional alpha slider, a current/previous preview swatch row, and a
    /// text-entry area with an RGB/HSV representation selector, per-channel inputs, an
    /// alpha percentage input, and a hex input, mirroring the WinUI 3 <c>ColorPicker</c>.
    /// The picker keeps hue, saturation, value, and alpha as its internal source of truth
    /// so dragging across the grey axis does not accumulate RGB round-trip drift, the
    /// same approach WinUI uses.
    /// </summary>
    /// <remarks>
    /// Scope notes: WinUI's <c>ColorSpectrumShape</c> (the Ring spectrum), the
    /// <c>ColorSpectrumComponents</c> permutations, <c>Orientation</c>, and the Min/Max
    /// channel range properties are deliberately omitted. The spectrum is fixed to
    /// saturation on the x axis by value on the y axis at the selected hue, with hue on a
    /// horizontal slider serving as the third-dimension color slider. Channel and alpha
    /// text inputs commit live on every valid keystroke like WinUI; the hex input commits
    /// on Enter or focus loss, a deliberate deviation from WinUI's live hex commit.
    /// </remarks>
    [TemplatePart(Name = PART_SpectrumImage, Type = typeof(Image))]
    [TemplatePart(Name = PART_SpectrumArea, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PART_SpectrumThumb, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PART_HueSlider, Type = typeof(RangeBase))]
    [TemplatePart(Name = PART_AlphaSlider, Type = typeof(RangeBase))]
    [TemplatePart(Name = PART_HexTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_RedTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_GreenTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_BlueTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_HueTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_SaturationTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_ValueTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PART_AlphaTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    public class ColorPicker : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_SpectrumImage = "PART_SpectrumImage";
        private const string PART_SpectrumArea = "PART_SpectrumArea";
        private const string PART_SpectrumThumb = "PART_SpectrumThumb";
        private const string PART_HueSlider = "PART_HueSlider";
        private const string PART_AlphaSlider = "PART_AlphaSlider";
        private const string PART_HexTextBox = "PART_HexTextBox";
        private const string PART_RedTextBox = "PART_RedTextBox";
        private const string PART_GreenTextBox = "PART_GreenTextBox";
        private const string PART_BlueTextBox = "PART_BlueTextBox";
        private const string PART_HueTextBox = "PART_HueTextBox";
        private const string PART_SaturationTextBox = "PART_SaturationTextBox";
        private const string PART_ValueTextBox = "PART_ValueTextBox";
        private const string PART_AlphaTextBox = "PART_AlphaTextBox";

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

        /// <summary>
        /// Small step applied to saturation or value when the user presses an arrow key
        /// on the focused spectrum area. Matches a 1% increment on the 0-1 scale.
        /// </summary>
        private const double SpectrumSmallStep = 0.01;

        /// <summary>
        /// Large step applied to saturation or value when the user presses PageUp or
        /// PageDown on the focused spectrum area. Matches a 10% increment on the 0-1 scale.
        /// </summary>
        private const double SpectrumLargeStep = 0.1;

        // Frozen, theme-independent brushes shared by every picker instance: the
        // checkerboard under translucent surfaces and the hue rainbow track. Both are
        // generated in code; asset/pixel math may use literal channel values, unlike
        // template chrome which must go through canonical theme tokens.
        private static readonly Brush CheckerboardBrush = CreateCheckerboardBrush();
        private static readonly Brush HueRainbowBrush = CreateHueRainbowBrush();

        private Image? _spectrumImage;
        private FrameworkElement? _spectrumArea;
        private FrameworkElement? _spectrumThumb;
        private RangeBase? _hueSlider;
        private RangeBase? _alphaSlider;
        private System.Windows.Controls.TextBox? _hexTextBox;
        private System.Windows.Controls.TextBox? _redTextBox;
        private System.Windows.Controls.TextBox? _greenTextBox;
        private System.Windows.Controls.TextBox? _blueTextBox;
        private System.Windows.Controls.TextBox? _hueTextBox;
        private System.Windows.Controls.TextBox? _saturationTextBox;
        private System.Windows.Controls.TextBox? _valueTextBox;
        private System.Windows.Controls.TextBox? _alphaTextBox;
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
        private TextInputGroup _activeTextInputGroup;

        // Identifies which text-input group is currently committing so UpdateVisuals can
        // skip rewriting the box the user is typing in (the WPF analog of WinUI's
        // ColorUpdateReason skip; rewriting the active box would destroy the caret).
        private enum TextInputGroup
        {
            None = 0,
            Rgb = 1,
            Hsv = 2,
            Alpha = 3,
        }

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
                new FrameworkPropertyMetadata(defaultValue: null, OnPreviousColorChanged));

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
                new FrameworkPropertyMetadata(defaultValue: false, OnIsAlphaEnabledChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the alpha channel can be edited. When
        /// <see langword="false"/> (the default) the alpha slider row and the alpha text
        /// input collapse, the hex input parses and displays six digits, and turning the
        /// property off pins the picker's alpha back to 255. Programmatic
        /// <see cref="Color"/> assignments keep whatever alpha they carry.
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
                new FrameworkPropertyMetadata(defaultValue: true));

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
        /// Identifies the <see cref="IsColorPreviewVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsColorPreviewVisibleProperty =
            DependencyProperty.Register(
                nameof(IsColorPreviewVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the current/previous preview swatch row
        /// is shown.
        /// </summary>
        public bool IsColorPreviewVisible
        {
            get => (bool)GetValue(IsColorPreviewVisibleProperty);
            set => SetValue(IsColorPreviewVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsColorSliderVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsColorSliderVisibleProperty =
            DependencyProperty.Register(
                nameof(IsColorSliderVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the third-dimension color slider is
        /// shown. With the spectrum fixed to saturation by value, the third dimension is
        /// the hue slider, matching how WinUI assigns its color slider for that spectrum
        /// component pairing.
        /// </summary>
        public bool IsColorSliderVisible
        {
            get => (bool)GetValue(IsColorSliderVisibleProperty);
            set => SetValue(IsColorSliderVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsAlphaSliderVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAlphaSliderVisibleProperty =
            DependencyProperty.Register(
                nameof(IsAlphaSliderVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the alpha slider is shown. The slider
        /// renders only when both this property and <see cref="IsAlphaEnabled"/> are
        /// <see langword="true"/>, matching WinUI.
        /// </summary>
        public bool IsAlphaSliderVisible
        {
            get => (bool)GetValue(IsAlphaSliderVisibleProperty);
            set => SetValue(IsAlphaSliderVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsAlphaTextInputVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAlphaTextInputVisibleProperty =
            DependencyProperty.Register(
                nameof(IsAlphaTextInputVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the alpha percentage text input is
        /// shown. The input renders only when both this property and
        /// <see cref="IsAlphaEnabled"/> are <see langword="true"/>, matching WinUI.
        /// </summary>
        public bool IsAlphaTextInputVisible
        {
            get => (bool)GetValue(IsAlphaTextInputVisibleProperty);
            set => SetValue(IsAlphaTextInputVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsHexInputVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsHexInputVisibleProperty =
            DependencyProperty.Register(
                nameof(IsHexInputVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the hex text input is shown. The input
        /// accepts <c>#RRGGBB</c> and <c>#AARRGGBB</c> (the leading <c>#</c> is optional)
        /// and commits on Enter or when keyboard focus leaves the box.
        /// </summary>
        public bool IsHexInputVisible
        {
            get => (bool)GetValue(IsHexInputVisibleProperty);
            set => SetValue(IsHexInputVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsMoreButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMoreButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsMoreButtonVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Gets or sets a value indicating whether the text-entry area collapses behind a
        /// More/Less toggle button. When <see langword="false"/> (the default) the text
        /// inputs are always visible and no toggle is shown; when <see langword="true"/>
        /// the toggle appears and the text-entry area stays collapsed until it is checked,
        /// matching WinUI.
        /// </summary>
        public bool IsMoreButtonVisible
        {
            get => (bool)GetValue(IsMoreButtonVisibleProperty);
            set => SetValue(IsMoreButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsColorChannelTextInputVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsColorChannelTextInputVisibleProperty =
            DependencyProperty.Register(
                nameof(IsColorChannelTextInputVisible),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets a value indicating whether the RGB/HSV representation selector and
        /// the per-channel text inputs are shown. The hex input is governed separately by
        /// <see cref="IsHexInputVisible"/> and the alpha input by
        /// <see cref="IsAlphaTextInputVisible"/>, matching WinUI. Channel input commits
        /// live on every valid keystroke; Enter or focus loss normalizes the text and
        /// restores it after invalid input.
        /// </summary>
        public bool IsColorChannelTextInputVisible
        {
            get => (bool)GetValue(IsColorChannelTextInputVisibleProperty);
            set => SetValue(IsColorChannelTextInputVisibleProperty, value);
        }

        /// <summary>
        /// Occurs after <see cref="Color"/> changes, whether through the spectrum, the
        /// sliders, the channel or hex text inputs, or a programmatic update. Channel and
        /// alpha text inputs commit live, so the event can fire once per keystroke while
        /// the user types (for example "1", "12", "120"), matching WinUI.
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

            DetachChannelTextBox(_redTextBox, OnRgbTextBoxTextChanged);
            DetachChannelTextBox(_greenTextBox, OnRgbTextBoxTextChanged);
            DetachChannelTextBox(_blueTextBox, OnRgbTextBoxTextChanged);
            DetachChannelTextBox(_hueTextBox, OnHsvTextBoxTextChanged);
            DetachChannelTextBox(_saturationTextBox, OnHsvTextBoxTextChanged);
            DetachChannelTextBox(_valueTextBox, OnHsvTextBoxTextChanged);
            DetachChannelTextBox(_alphaTextBox, OnAlphaTextBoxTextChanged);

            base.OnApplyTemplate();

            _spectrumImage = GetTemplateChild(PART_SpectrumImage) as Image;
            _spectrumArea = GetTemplateChild(PART_SpectrumArea) as FrameworkElement;
            _spectrumThumb = GetTemplateChild(PART_SpectrumThumb) as FrameworkElement;
            _hueSlider = GetTemplateChild(PART_HueSlider) as RangeBase;
            _alphaSlider = GetTemplateChild(PART_AlphaSlider) as RangeBase;
            _hexTextBox = GetTemplateChild(PART_HexTextBox) as System.Windows.Controls.TextBox;
            _redTextBox = GetTemplateChild(PART_RedTextBox) as System.Windows.Controls.TextBox;
            _greenTextBox = GetTemplateChild(PART_GreenTextBox) as System.Windows.Controls.TextBox;
            _blueTextBox = GetTemplateChild(PART_BlueTextBox) as System.Windows.Controls.TextBox;
            _hueTextBox = GetTemplateChild(PART_HueTextBox) as System.Windows.Controls.TextBox;
            _saturationTextBox = GetTemplateChild(PART_SaturationTextBox) as System.Windows.Controls.TextBox;
            _valueTextBox = GetTemplateChild(PART_ValueTextBox) as System.Windows.Controls.TextBox;
            _alphaTextBox = GetTemplateChild(PART_AlphaTextBox) as System.Windows.Controls.TextBox;
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

            AttachChannelTextBox(_redTextBox, OnRgbTextBoxTextChanged);
            AttachChannelTextBox(_greenTextBox, OnRgbTextBoxTextChanged);
            AttachChannelTextBox(_blueTextBox, OnRgbTextBoxTextChanged);
            AttachChannelTextBox(_hueTextBox, OnHsvTextBoxTextChanged);
            AttachChannelTextBox(_saturationTextBox, OnHsvTextBoxTextChanged);
            AttachChannelTextBox(_valueTextBox, OnHsvTextBoxTextChanged);
            AttachChannelTextBox(_alphaTextBox, OnAlphaTextBoxTextChanged);

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
            if (e.NewValue is false && picker._alpha is not 255)
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
        /// <param name="hue">0-360</param>
        /// <param name="saturation">0-1</param>
        /// <param name="value">0-1</param>
        /// <param name="alpha">0-255</param>
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
        /// <param name="color">The color to synchronize from.</param>
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

            _spectrumBitmap ??= new WriteableBitmap(SpectrumSize, SpectrumSize, 96, 96, PixelFormats.Bgra32, palette: null);
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
                    pixels[index] = (byte)Math.Round(value * (255.0 - (saturation * (255.0 - hueBlue))), MidpointRounding.ToEven);
                    pixels[index + 1] = (byte)Math.Round(value * (255.0 - (saturation * (255.0 - hueGreen))), MidpointRounding.ToEven);
                    pixels[index + 2] = (byte)Math.Round(value * (255.0 - (saturation * (255.0 - hueRed))), MidpointRounding.ToEven);
                    pixels[index + 3] = 255;
                    index += 4;
                }
            }

            _spectrumBitmap.WritePixels(new Int32Rect(0, 0, SpectrumSize, SpectrumSize), pixels, SpectrumSize * 4, 0);
            _spectrumBitmapHue = _hue;
            _spectrumImage.SetCurrentValue(Image.SourceProperty, _spectrumBitmap);
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
                UpdateChannelTextBoxes();
                UpdateHexText();
            }
            finally
            {
                _isUpdatingVisuals = false;
            }
        }

        /// <summary>
        /// Rewrites the channel and alpha text boxes from the current model, skipping the
        /// group the user is typing in so the live per-keystroke commit never destroys the
        /// caret of the active box. RGB boxes display the <see cref="Color"/> channels
        /// exactly; HSV boxes round to integers for display only (the model keeps full
        /// precision); the alpha box displays a percentage with a trailing percent sign.
        /// </summary>
        private void UpdateChannelTextBoxes()
        {
            if (_activeTextInputGroup is not TextInputGroup.Rgb)
            {
                Color color = Color;
                _redTextBox?.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, color.R.ToString(CultureInfo.InvariantCulture));
                _greenTextBox?.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, color.G.ToString(CultureInfo.InvariantCulture));
                _blueTextBox?.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, color.B.ToString(CultureInfo.InvariantCulture));
            }

            if (_activeTextInputGroup is not TextInputGroup.Hsv)
            {
                _hueTextBox?.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, ((int)Math.Round(_hue, MidpointRounding.ToEven)).ToString(CultureInfo.InvariantCulture));
                _saturationTextBox?.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, ((int)Math.Round(_saturation * 100, MidpointRounding.ToEven)).ToString(CultureInfo.InvariantCulture));
                _valueTextBox?.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, ((int)Math.Round(_value * 100, MidpointRounding.ToEven)).ToString(CultureInfo.InvariantCulture));
            }

            if (_activeTextInputGroup is not TextInputGroup.Alpha)
            {
                int percent = (int)Math.Round(_alpha / 255.0 * 100, MidpointRounding.ToEven);
                _alphaTextBox?.SetCurrentValue(
                    System.Windows.Controls.TextBox.TextProperty,
                    string.Format(CultureInfo.InvariantCulture, "{0}%", percent));
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
            if (_previousSwatchBorder is null || previous is null)
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
        /// <param name="position">The point within the spectrum area to apply.</param>
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

        /// <summary>
        /// Handles arrow and page key presses when keyboard focus is on the spectrum area,
        /// adjusting saturation (Left/Right) or value (Up/Down/PageUp/PageDown) and routing
        /// through the HSV funnel so there is no RGB round-trip drift.
        /// </summary>
        /// <param name="e">The key event arguments.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled || _spectrumArea is null)
            {
                return;
            }

            // Only intercept when the event originates from the spectrum area so arrow keys
            // continue to work normally for sliders and text boxes elsewhere in the picker.
            if (e.OriginalSource is not DependencyObject originalSource
                || (!ReferenceEquals(originalSource, _spectrumArea)
                    && !_spectrumArea.IsAncestorOf(originalSource)))
            {
                return;
            }

            double saturationDelta;
            double valueDelta;

            if (e.Key is Key.Right)
            {
                saturationDelta = SpectrumSmallStep;
                valueDelta = 0;
            }
            else if (e.Key is Key.Left)
            {
                saturationDelta = -SpectrumSmallStep;
                valueDelta = 0;
            }
            else if (e.Key is Key.Up)
            {
                saturationDelta = 0;
                valueDelta = SpectrumSmallStep;
            }
            else if (e.Key is Key.Down)
            {
                saturationDelta = 0;
                valueDelta = -SpectrumSmallStep;
            }
            else if (e.Key is Key.PageUp)
            {
                saturationDelta = 0;
                valueDelta = SpectrumLargeStep;
            }
            else if (e.Key is Key.PageDown)
            {
                saturationDelta = 0;
                valueDelta = -SpectrumLargeStep;
            }
            else
            {
                return;
            }

            SetColorFromHsv(_hue, _saturation + saturationDelta, _value + valueDelta, _alpha);
            e.Handled = true;
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

            byte alpha = (byte)Math.Round(Math.Max(0, Math.Min(255, e.NewValue)), MidpointRounding.ToEven);
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

        private void AttachChannelTextBox(System.Windows.Controls.TextBox? textBox, TextChangedEventHandler textChangedHandler)
        {
            if (textBox is null)
            {
                return;
            }

            textBox.TextChanged += textChangedHandler;
            textBox.KeyDown += OnChannelTextBoxKeyDown;
            textBox.LostKeyboardFocus += OnChannelTextBoxLostKeyboardFocus;
        }

        private void DetachChannelTextBox(System.Windows.Controls.TextBox? textBox, TextChangedEventHandler textChangedHandler)
        {
            if (textBox is null)
            {
                return;
            }

            textBox.TextChanged -= textChangedHandler;
            textBox.KeyDown -= OnChannelTextBoxKeyDown;
            textBox.LostKeyboardFocus -= OnChannelTextBoxLostKeyboardFocus;
        }

        /// <summary>
        /// Commits a red, green, or blue channel edit live. The target color is rebuilt
        /// from the current <see cref="Color"/> plus the parsed channel and published
        /// directly through the Color DP, like the hex commit, so the typed RGB value is
        /// preserved exactly; <see cref="SyncHsvFromColor"/>'s hue/saturation retention
        /// keeps the spectrum thumb stable on greys. Invalid text is a no-op until Enter
        /// or focus loss restores it.
        /// </summary>
        /// <param name="sender">The text box that raised the event.</param>
        /// <param name="e">Event data for the text-changed event.</param>
        private void OnRgbTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingVisuals || sender is not System.Windows.Controls.TextBox box)
            {
                return;
            }

            if (!TryParseChannelValue(box.Text, 255, out int parsed))
            {
                return;
            }

            Color current = Color;
            byte channel = (byte)parsed;
            Color target = ReferenceEquals(box, _redTextBox)
                ? Color.FromArgb(current.A, channel, current.G, current.B)
                : ReferenceEquals(box, _greenTextBox)
                    ? Color.FromArgb(current.A, current.R, channel, current.B)
                    : Color.FromArgb(current.A, current.R, current.G, channel);

            _activeTextInputGroup = TextInputGroup.Rgb;
            try
            {
                SetCurrentValue(ColorProperty, target);
            }
            finally
            {
                _activeTextInputGroup = TextInputGroup.None;
            }
        }

        /// <summary>
        /// Commits a hue, saturation, or value channel edit live. Only the edited
        /// component is replaced, going straight through the HSV funnel so the untouched
        /// components keep their full precision (deliberately better than WinUI, which
        /// re-reads all three boxes and quantizes the untouched components to integers).
        /// Hue accepts 0 to 360 because the picker's model and slider use 360 inclusive.
        /// </summary>
        /// <param name="sender">The text box that raised the event.</param>
        /// <param name="e">Event data for the text-changed event.</param>
        private void OnHsvTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingVisuals || sender is not System.Windows.Controls.TextBox box)
            {
                return;
            }

            _activeTextInputGroup = TextInputGroup.Hsv;
            try
            {
                if (ReferenceEquals(box, _hueTextBox))
                {
                    if (TryParseChannelValue(box.Text, 360, out int hue))
                    {
                        SetColorFromHsv(hue, _saturation, _value, _alpha);
                    }
                }
                else if (ReferenceEquals(box, _saturationTextBox))
                {
                    if (TryParseChannelValue(box.Text, 100, out int saturation))
                    {
                        SetColorFromHsv(_hue, saturation / 100.0, _value, _alpha);
                    }
                }
                else if (TryParseChannelValue(box.Text, 100, out int value))
                {
                    SetColorFromHsv(_hue, _saturation, value / 100.0, _alpha);
                }
            }
            finally
            {
                _activeTextInputGroup = TextInputGroup.None;
            }
        }

        /// <summary>
        /// Commits an alpha percentage edit live. The box accepts 0 to 100 with an
        /// optional trailing percent sign (auto-appended on normalize), mapped to the
        /// 0 to 255 alpha byte, like WinUI's opacity input.
        /// </summary>
        /// <param name="sender">The text box that raised the event.</param>
        /// <param name="e">Event data for the text-changed event.</param>
        private void OnAlphaTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingVisuals || _alphaTextBox is null)
            {
                return;
            }

            if (!TryParseAlphaPercent(_alphaTextBox.Text, out int percent))
            {
                return;
            }

            byte alpha = (byte)Math.Round(percent / 100.0 * 255, MidpointRounding.ToEven);
            _activeTextInputGroup = TextInputGroup.Alpha;
            try
            {
                SetColorFromHsv(_hue, _saturation, _value, alpha);
            }
            finally
            {
                _activeTextInputGroup = TextInputGroup.None;
            }
        }

        private void OnChannelTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not Key.Enter)
            {
                return;
            }

            // Normalizes valid text ("050" -> "50", "50" -> "50%") and restores invalid
            // text from the model, the same observable outcome as WinUI's focus-loss
            // snapshot restore.
            UpdateVisuals();
            e.Handled = true;
        }

        private void OnChannelTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Strictly parses a non-negative invariant integer between zero and
        /// <paramref name="max"/> inclusive. Signs, group separators, and embedded
        /// whitespace are rejected; surrounding whitespace is trimmed.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="max">The inclusive upper bound for the parsed value.</param>
        /// <param name="value">The parsed integer if successful; otherwise, zero.</param>
        /// <returns><see langword="true"/> if the text was successfully parsed and is within range; otherwise, <see langword="false"/>.</returns>
        private static bool TryParseChannelValue(string text, int max, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed > max)
            {
                return false;
            }

            value = parsed;
            return true;
        }

        /// <summary>
        /// Parses the alpha box text as a 0 to 100 percentage, tolerating one trailing
        /// percent sign.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="percent">The parsed percentage value (0 to 100) if successful; otherwise, zero.</param>
        /// <returns><see langword="true"/> if the text was successfully parsed as a valid percentage; otherwise, <see langword="false"/>.</returns>
        private static bool TryParseAlphaPercent(string text, out int percent)
        {
            percent = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            if (trimmed[^1] == '%')
            {
                trimmed = trimmed[..^1];
            }

            return TryParseChannelValue(trimmed, 100, out percent);
        }

        /// <summary>
        /// Parses <c>#RRGGBB</c> / <c>#AARRGGBB</c> (leading <c>#</c> optional). Six-digit
        /// input is treated as fully opaque.
        /// </summary>
        /// <param name="text">The hex color string to parse.</param>
        /// <param name="color">The parsed color if successful; otherwise, <see langword="default"/>.</param>
        /// <returns><see langword="true"/> if the hex color string was successfully parsed; otherwise, <see langword="false"/>.</returns>
        private static bool TryParseHexColor(string text, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string hex = text.Trim();
            if (hex.StartsWith('#'.ToString(), StringComparison.Ordinal))
            {
                hex = hex[1..];
            }

            if (hex.Length is not (6 or 8))
            {
                return false;
            }

            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
            {
                return false;
            }

            if (hex.Length is 6)
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
