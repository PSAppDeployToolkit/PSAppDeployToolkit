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
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents an icon that uses a glyph from a font.
    /// </summary>
    [TemplatePart(Name = PART_Mirror, Type = typeof(ScaleTransform))]
    [TemplatePart(Name = PART_Rotate, Type = typeof(RotateTransform))]
    public class FontIcon : Control
    {
        // Template part names.
        private const string PART_Mirror = "PART_Mirror";
        private const string PART_Rotate = "PART_Rotate";

        /// <summary>
        /// Initializes static members of the FontIcon class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the FontIcon control uses its own style by
        /// default. This is necessary for custom controls to apply their styles correctly in WPF.</remarks>
        static FontIcon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FontIcon),
                new FrameworkPropertyMetadata(typeof(FontIcon)));
        }

        /// <summary>
        /// Identifies the <see cref="Glyph"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(
                nameof(Glyph),
                typeof(string),
                typeof(FontIcon),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the glyph character to display.
        /// </summary>
        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconFontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconFontFamilyProperty =
            DependencyProperty.Register(
                nameof(IconFontFamily),
                typeof(FontFamily),
                typeof(FontIcon),
                new FrameworkPropertyMetadata(new FontFamily("Segoe Fluent Icons")));

        /// <summary>
        /// Gets or sets the font family used for the icon glyph.
        /// </summary>
        public FontFamily IconFontFamily
        {
            get => (FontFamily)GetValue(IconFontFamilyProperty);
            set => SetValue(IconFontFamilyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconFontSizeProperty =
            DependencyProperty.Register(
                nameof(IconFontSize),
                typeof(double),
                typeof(FontIcon),
                new FrameworkPropertyMetadata(16.0));

        /// <summary>
        /// Gets or sets the font size of the icon glyph.
        /// </summary>
        public double IconFontSize
        {
            get => (double)GetValue(IconFontSizeProperty);
            set => SetValue(IconFontSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Rotation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.Register(
                nameof(Rotation),
                typeof(double),
                typeof(FontIcon),
                new FrameworkPropertyMetadata(0.0, OnRotationChanged));

        /// <summary>
        /// Gets or sets the rotation angle in degrees.
        /// </summary>
        public double Rotation
        {
            get => (double)GetValue(RotationProperty);
            set => SetValue(RotationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MirroredWhenRightToLeft"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MirroredWhenRightToLeftProperty =
            DependencyProperty.Register(
                nameof(MirroredWhenRightToLeft),
                typeof(bool),
                typeof(FontIcon),
                new FrameworkPropertyMetadata(defaultValue: false, OnMirroredWhenRightToLeftChanged));

        /// <summary>
        /// Gets or sets whether the glyph is horizontally flipped when
        /// <see cref="FrameworkElement.FlowDirection"/> is <see cref="FlowDirection.RightToLeft"/>.
        /// </summary>
        public bool MirroredWhenRightToLeft
        {
            get => (bool)GetValue(MirroredWhenRightToLeftProperty);
            set => SetValue(MirroredWhenRightToLeftProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableTransitions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableTransitionsProperty =
            DependencyProperty.Register(
                nameof(EnableTransitions),
                typeof(bool),
                typeof(FontIcon),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Gets or sets whether high-quality bitmap scaling is used during transitions.
        /// </summary>
        public bool EnableTransitions
        {
            get => (bool)GetValue(EnableTransitionsProperty);
            set => SetValue(EnableTransitionsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSpinning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSpinningProperty =
            DependencyProperty.Register(
                nameof(IsSpinning),
                typeof(bool),
                typeof(FontIcon),
                new FrameworkPropertyMetadata(defaultValue: false, OnIsSpinningChanged));

        /// <summary>
        /// Gets or sets whether the icon continuously spins.
        /// </summary>
        public bool IsSpinning
        {
            get => (bool)GetValue(IsSpinningProperty);
            set => SetValue(IsSpinningProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild(PART_Rotate) is RotateTransform rotate)
            {
                rotate.Angle = Rotation;
            }

            ApplyMirrorState();
            ApplySpinState();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new Fluence.Wpf.Automation.FontIconAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == FlowDirectionProperty)
            {
                ApplyMirrorState();
            }
        }

        private static void OnRotationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FontIcon icon = (FontIcon)d;
            if (icon.GetTemplateChild(PART_Rotate) is RotateTransform rotate)
            {
                rotate.Angle = (double)e.NewValue;
            }
        }

        private static void OnMirroredWhenRightToLeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FontIcon)d).ApplyMirrorState();
        }

        private static void OnIsSpinningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FontIcon icon = (FontIcon)d;
            icon.ApplySpinState();
        }

        private void ApplyMirrorState()
        {
            if (GetTemplateChild(PART_Mirror) is not ScaleTransform mirror)
            {
                return;
            }

            mirror.ScaleX = (MirroredWhenRightToLeft && FlowDirection is FlowDirection.RightToLeft) ? -1 : 1;
        }

        private void ApplySpinState()
        {
            if (GetTemplateChild(PART_Rotate) is not RotateTransform rotate)
            {
                return;
            }

            rotate.BeginAnimation(RotateTransform.AngleProperty, animation: null);

            if (!IsSpinning)
            {
                rotate.Angle = Rotation;
                return;
            }

            DoubleAnimation animation = new()
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1.1)),
                RepeatBehavior = RepeatBehavior.Forever,
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
