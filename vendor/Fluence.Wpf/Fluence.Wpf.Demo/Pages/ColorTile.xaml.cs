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
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Colors page brush tile, mirroring the WinUI 3 Gallery <c language="csharp">ColorTile</c> control:
    /// ordinary brush tiles show their documented brush key, while material-reference tiles show
    /// documentation links and copy a real window backdrop setting.
    /// </summary>
    /// <remarks>
    /// The page sets <see cref="Control.Background"/> to the documented brush or to a material
    /// tile's explicitly named solid preview, and <see cref="Control.Foreground"/> to the
    /// contrasting text brush with
    /// <see cref="FrameworkElement.SetResourceReference"/>, so both follow theme changes.
    /// <see cref="TileCornerRadius"/> lets the first and last tiles in a row round their outer
    /// corners to sit flush inside the rounded tile-grid surface, which WPF's <c language="csharp">Border</c>
    /// does not clip for them.
    /// </remarks>
    public partial class ColorTile : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="ColorName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorNameProperty =
            DependencyProperty.Register(
                nameof(ColorName),
                typeof(string),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="ColorExplanation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorExplanationProperty =
            DependencyProperty.Register(
                nameof(ColorExplanation),
                typeof(string),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="ColorBrushName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorBrushNameProperty =
            DependencyProperty.Register(
                nameof(ColorBrushName),
                typeof(string),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="CopyPayload"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CopyPayloadProperty =
            DependencyProperty.Register(
                nameof(CopyPayload),
                typeof(string),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="IsBackdropReference"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackdropReferenceProperty =
            DependencyProperty.Register(
                nameof(IsBackdropReference),
                typeof(bool),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: OnIsBackdropReferenceChanged));

        /// <summary>
        /// Identifies the <see cref="ShowSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowSeparatorProperty =
            DependencyProperty.Register(
                nameof(ShowSeparator),
                typeof(bool),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Identifies the <see cref="TileCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TileCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(TileCornerRadius),
                typeof(CornerRadius),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(defaultValue: default(CornerRadius)));

        /// <summary>
        /// Identifies the <see cref="AutoContrastForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoContrastForegroundProperty =
            DependencyProperty.Register(
                nameof(AutoContrastForeground),
                typeof(bool),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: OnAutoContrastForegroundChanged));

        private const string PrimaryTextKey = "TextFillColorPrimaryBrush";
        private const string OnAccentTextKey = "TextOnAccentFillColorPrimaryBrush";
        private const string BaseSurfaceKey = "SolidBackgroundFillColorBaseBrush";

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorTile"/> class.
        /// </summary>
        public ColorTile()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tile picks its own text brush by contrast.
        /// </summary>
        /// <remarks>
        /// The WinUI Gallery paints a few tiles whose brush is light in both themes (the on-accent
        /// disabled and selected-text tiles) with a literal black foreground. Fluence has no brush
        /// that is dark in every theme, so when this is set the tile picks whichever of the primary
        /// and on-accent text brushes contrasts more with its <see cref="Control.Background"/>, and
        /// re-evaluates whenever a theme change republishes that background. A
        /// <see cref="GradientBrush"/> background (for example a tile documenting an elevation
        /// border) is reduced to the equally weighted average of its gradient stops first, since
        /// there is no single tile color to compare against.
        /// </remarks>
        public bool AutoContrastForeground
        {
            get => (bool)GetValue(AutoContrastForegroundProperty);
            set => SetValue(AutoContrastForegroundProperty, value);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == BackgroundProperty && AutoContrastForeground)
            {
                ApplyAutoContrastForeground();
            }
        }

        private static void OnAutoContrastForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                ((ColorTile)d).ApplyAutoContrastForeground();
            }
        }

        private void ApplyAutoContrastForeground()
        {
            Color tileColor;
            if (Background is SolidColorBrush tileBrush)
            {
                tileColor = tileBrush.Color;
            }
            else if (Background is GradientBrush gradientBrush)
            {
                // A gradient tile (for example TextControlElevationBorderBrush) has no single
                // color; average its stops with equal weighting to get a representative color
                // for the contrast comparison below.
                tileColor = AverageGradientStopColor(gradientBrush);
            }
            else
            {
                return;
            }

            Color surface = TryFindResource(BaseSurfaceKey) is SolidColorBrush surfaceBrush ? surfaceBrush.Color : Colors.White;
            double tileLuminance = Luminance(Composite(tileColor, surface));
            double primaryLuminance = TryFindResource(PrimaryTextKey) is SolidColorBrush primary ? Luminance(Composite(primary.Color, surface)) : 0;
            double onAccentLuminance = TryFindResource(OnAccentTextKey) is SolidColorBrush onAccent ? Luminance(Composite(onAccent.Color, surface)) : 1;
            string key = Math.Abs(primaryLuminance - tileLuminance) >= Math.Abs(onAccentLuminance - tileLuminance) ? PrimaryTextKey : OnAccentTextKey;
            SetResourceReference(ForegroundProperty, key);
        }

        private static Color AverageGradientStopColor(GradientBrush gradient)
        {
            GradientStopCollection stops = gradient.GradientStops;
            if (stops.Count is 0)
            {
                return Colors.Transparent;
            }

            double a = 0;
            double r = 0;
            double g = 0;
            double b = 0;
            foreach (GradientStop stop in stops)
            {
                a += stop.Color.A;
                r += stop.Color.R;
                g += stop.Color.G;
                b += stop.Color.B;
            }

            return Color.FromArgb(
                (byte)Math.Round(a / stops.Count, MidpointRounding.AwayFromZero),
                (byte)Math.Round(r / stops.Count, MidpointRounding.AwayFromZero),
                (byte)Math.Round(g / stops.Count, MidpointRounding.AwayFromZero),
                (byte)Math.Round(b / stops.Count, MidpointRounding.AwayFromZero));
        }

        private static Color Composite(Color color, Color surface)
        {
            double alpha = color.A / 255d;
            return Color.FromRgb(
                (byte)Math.Round((color.R * alpha) + (surface.R * (1 - alpha)), MidpointRounding.AwayFromZero),
                (byte)Math.Round((color.G * alpha) + (surface.G * (1 - alpha)), MidpointRounding.AwayFromZero),
                (byte)Math.Round((color.B * alpha) + (surface.B * (1 - alpha)), MidpointRounding.AwayFromZero));
        }

        private static double Luminance(Color color)
        {
            return ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255d;
        }

        /// <summary>
        /// Gets or sets the tile name, for example <c language="text">Text / Primary</c>.
        /// </summary>
        public string ColorName
        {
            get => (string)GetValue(ColorNameProperty);
            set => SetValue(ColorNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the usage note, for example <c language="text">Rest or Hover</c>.
        /// </summary>
        public string ColorExplanation
        {
            get => (string)GetValue(ColorExplanationProperty);
            set => SetValue(ColorExplanationProperty, value);
        }

        /// <summary>
        /// Gets or sets the Fluence brush key displayed on an ordinary brush tile.
        /// Material-reference tiles leave this empty because Mica is a window material, not a brush.
        /// </summary>
        public string ColorBrushName
        {
            get => (string)GetValue(ColorBrushNameProperty);
            set => SetValue(ColorBrushNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the exact text copied by the tile's button. Ordinary tiles copy their
        /// brush key; material-reference tiles copy a <c language="xaml">SystemBackdropType</c> setting.
        /// </summary>
        public string CopyPayload
        {
            get => (string)GetValue(CopyPayloadProperty);
            set => SetValue(CopyPayloadProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tile describes a window backdrop and shows
        /// links to the WinUI material API in place of a brush-key footer.
        /// </summary>
        public bool IsBackdropReference
        {
            get => (bool)GetValue(IsBackdropReferenceProperty);
            set => SetValue(IsBackdropReferenceProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the 1px right-hand separator is shown.
        /// </summary>
        public bool ShowSeparator
        {
            get => (bool)GetValue(ShowSeparatorProperty);
            set => SetValue(ShowSeparatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of the painted tile surface.
        /// </summary>
        public CornerRadius TileCornerRadius
        {
            get => (CornerRadius)GetValue(TileCornerRadiusProperty);
            set => SetValue(TileCornerRadiusProperty, value);
        }

        private void CopyBrushNameButton_Click(object sender, RoutedEventArgs e)
        {
            string copyText = string.IsNullOrWhiteSpace(CopyPayload) ? ColorBrushName : CopyPayload;
            if (string.IsNullOrWhiteSpace(copyText))
            {
                return;
            }

            try
            {
                // The success cue waits for the callback rather than firing here: the clipboard
                // retry runs on a dispatcher timer, so a copy that another process is blocking has
                // not failed yet at this point and a cue shown now would claim a success that may
                // never happen.
                DemoClipboard.SetText(copyText, copied =>
                {
                    if (copied)
                    {
                        ShowCopiedFeedback();
                    }
                });
            }
            catch (ThreadStateException)
            {
                Debug.WriteLine("Clipboard access requires an STA thread.");
            }
        }

        /// <summary>
        /// Swaps the copy glyph for a checkmark for a moment, the way the WinUI Gallery's own
        /// CopyButton plays a success cue after a copy (CopyButton.xaml CopyToClipboardSuccessAnimation).
        /// Without it a successful copy looks like nothing happened.
        /// </summary>
        private void ShowCopiedFeedback()
        {
            if (CopyBrushNameGlyph is null)
            {
                return;
            }

            CopyBrushNameGlyph.SetCurrentValue(Controls.FontIcon.GlyphProperty, CopiedGlyph);
            CopyBrushNameButton?.SetCurrentValue(ToolTipProperty, IsBackdropReference ? BackdropCopiedToolTip : CopiedToolTip);

            _copiedRevert?.Stop();
            _copiedRevert = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = CopiedFeedbackDuration,
            };
            _copiedRevert.Tick += OnCopiedRevertTick;
            _copiedRevert.Start();
        }

        /// <summary>
        /// Puts the copy glyph and tooltip back once the success cue has been seen.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnCopiedRevertTick(object? sender, EventArgs e)
        {
            _copiedRevert?.Stop();
            _copiedRevert = null;
            CopyBrushNameGlyph?.SetCurrentValue(Controls.FontIcon.GlyphProperty, CopyGlyph);
            CopyBrushNameButton?.SetCurrentValue(ToolTipProperty, IsBackdropReference ? BackdropCopyToolTip : CopyToolTip);
        }

        private static void OnIsBackdropReferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorTile tile = (ColorTile)d;
            if (tile.CopyBrushNameButton is null)
            {
                return;
            }

            bool isBackdrop = e.NewValue is true;
            AutomationProperties.SetName(tile.CopyBrushNameButton, isBackdrop ? "Copy backdrop setting" : "Copy brush name");
            tile.CopyBrushNameButton.SetCurrentValue(ToolTipProperty, isBackdrop ? BackdropCopyToolTip : CopyToolTip);
        }

        private void DocumentationLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        /// <summary>
        /// The Segoe Fluent Icons copy glyph shown at rest.
        /// </summary>
        private const string CopyGlyph = "";

        /// <summary>
        /// The Segoe Fluent Icons checkmark glyph shown right after a copy.
        /// </summary>
        private const string CopiedGlyph = "";

        /// <summary>
        /// The rest tooltip.
        /// </summary>
        private const string CopyToolTip = "Copy brush name";

        private const string BackdropCopyToolTip = "Copy backdrop setting";

        /// <summary>
        /// The tooltip shown with the success cue, matching the Gallery's copied message.
        /// </summary>
        private const string CopiedToolTip = "Brush name copied to clipboard";

        private const string BackdropCopiedToolTip = "Backdrop setting copied to clipboard";

        /// <summary>
        /// How long the success cue stays up.
        /// </summary>
        private static readonly TimeSpan CopiedFeedbackDuration = TimeSpan.FromMilliseconds(1200);

        /// <summary>
        /// The timer that reverts the success cue, or null while no cue is showing.
        /// </summary>
        private DispatcherTimer? _copiedRevert;
    }
}
