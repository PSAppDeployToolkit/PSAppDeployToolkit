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
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

// IMPORTANT: every reference to Image / Border in this file MUST be fully qualified
// (System.Windows.Controls.Image, System.Windows.Controls.Border). This file declares
// Fluence.Wpf.Controls.Image and sits inside that namespace, so an unqualified Image
// resolves to this control and the template part contract would reference itself. The
// parts are typed against the stock WPF types because the default template hosts stock
// elements. A using alias would read better but RCS1056 bans alias directives, and the
// repo has no other alias; see ColorPicker.cs and DatePicker.cs for the same pattern.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design image presenter that frames a picture with a theme-aware 1px stroke and
    /// rounded-corner clipping while delegating natural sizing, stretch semantics, and DPI
    /// handling to a real inner <see cref="System.Windows.Controls.Image"/>.
    /// Authority: in-tree precedent (PersonPicture stroke tokens, FontIcon non-interactive shape);
    /// WinUI 3 ships no styled Image control, so the frame follows the Card stroke idiom.
    /// </summary>
    [TemplatePart(Name = PART_Image, Type = typeof(System.Windows.Controls.Image))]
    [TemplatePart(Name = PART_ImageBorder, Type = typeof(System.Windows.Controls.Border))]
    public class Image : Control
    {
        // Template part names.
        private const string PART_Image = "PART_Image";
        private const string PART_ImageBorder = "PART_ImageBorder";

        /// <summary>
        /// Initializes static members of the Image class and overrides the default style key to
        /// associate the control with its style.
        /// </summary>
        /// <remarks>This static constructor ensures that the Image control uses the correct
        /// default style as defined in the application's resources. This is necessary for custom
        /// controls to apply their styles properly in WPF.</remarks>
        static Image()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Image),
                new FrameworkPropertyMetadata(typeof(Image)));
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(ImageSource),
                typeof(Image),
                new FrameworkPropertyMetadata(defaultValue: null));

        /// <summary>
        /// Gets or sets the image source to display. When <see langword="null"/> (default)
        /// nothing is drawn inside the frame.
        /// </summary>
        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(Stretch),
                typeof(Image),
                new FrameworkPropertyMetadata(Stretch.Uniform));

        /// <summary>
        /// Gets or sets how the image fills the available space. The default is
        /// <see cref="Stretch.Uniform"/>, preserving the source aspect ratio.
        /// </summary>
        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(Image),
                new FrameworkPropertyMetadata(new CornerRadius(4), OnCornerRadiusChanged));

        /// <summary>
        /// Gets or sets the corner radius of the frame. The stroke border template-binds this
        /// value directly; the inner image is clipped in code using the top-left radius uniformly
        /// (the WPF Border corner-clip idiom). Set to 0 to disable clipping.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _image = GetTemplateChild(PART_Image) as System.Windows.Controls.Image;
            UpdateImageClip();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new Automation.ImageAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateImageClip();
        }

        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Image)d).UpdateImageClip();
        }

        /// <summary>
        /// Applies or clears the rounded-corner clip on the inner image. The template stretches
        /// the inner image across the whole root grid, so the control's render size is the image
        /// element's coordinate space; the radius is clamped to half the smaller dimension.
        /// </summary>
        private void UpdateImageClip()
        {
            if (_image is null)
            {
                return;
            }
            double radius = CornerRadius.TopLeft;
            Size size = RenderSize;
            if (radius <= 0 || size.Width <= 0 || size.Height <= 0)
            {
                _image.Clip = null;
                return;
            }
            double clamped = Math.Min(radius, Math.Min(size.Width, size.Height) / 2);
            RectangleGeometry clip = new(new Rect(size), clamped, clamped);
            clip.Freeze();
            _image.Clip = clip;
        }

        /// <summary>
        /// The inner WPF image element that renders <see cref="Source"/>, or null before the
        /// template is applied.
        /// </summary>
        private System.Windows.Controls.Image? _image;
    }
}
