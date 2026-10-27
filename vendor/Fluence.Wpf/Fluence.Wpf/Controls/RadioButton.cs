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
using System.Windows.Automation;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled radio button with optional description text.
    /// </summary>
    /// <remarks>Inspired by WInUI's RadioButton.</remarks>
    [TemplatePart(Name = PART_CheckGlyph, Type = typeof(Ellipse))]
    [TemplatePart(Name = PART_PressedCheckGlyph, Type = typeof(System.Windows.Controls.Border))]
    public class RadioButton : System.Windows.Controls.RadioButton
    {
        private const string PART_CheckGlyph = "PART_CheckGlyph";
        private const string PART_PressedCheckGlyph = "PART_PressedCheckGlyph";
        private const double RestGlyphSize = 12;
        private const double PointerOverGlyphSize = 14;
        private const double PressedGlyphSize = 10;
        private const double PressedOverlayRestSize = 4;
        private const int NormalAnimationMilliseconds = 250;
        private const int FastAnimationMilliseconds = 167;

        private enum CommonState
        {
            Normal = 0,
            PointerOver = 1,
            Pressed = 2,
            Disabled = 3,
        }

        /// <summary>
        /// Initializes static members of the RadioButton class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the RadioButton control uses its default style
        /// as defined in the application's theme or resource dictionaries. This is typically required for custom
        /// controls to apply their visual appearance correctly.</remarks>
        static RadioButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(RadioButton),
                new FrameworkPropertyMetadata(typeof(RadioButton)));
        }

        /// <summary>
        /// Initializes a radio button and its template-part motion lifetime.
        /// </summary>
        public RadioButton()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            ClearGlyphClocks();
            base.OnApplyTemplate();

            _checkGlyph = GetTemplateChild(PART_CheckGlyph) as Ellipse;
            _pressedCheckGlyph = GetTemplateChild(PART_PressedCheckGlyph) as System.Windows.Controls.Border;
            UpdateGlyphGeometry(animate: false);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == IsMouseOverProperty || e.Property == IsEnabledProperty || e.Property == IsPressedProperty)
            {
                UpdateGlyphGeometry(animate: true);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(RadioButton),
                new FrameworkPropertyMetadata(propertyChangedCallback: OnDescriptionChanged));

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadioButton radioButton)
            {
                string? description = e.NewValue as string;
                AutomationProperties.SetHelpText(
                    radioButton,
                    string.IsNullOrWhiteSpace(description) ? string.Empty : description);
            }
        }

        /// <summary>
        /// Gets or sets the description text displayed below the radio button content.
        /// </summary>
        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(RadioButton),
                new FrameworkPropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Gets or sets the corner radius. Defaults to 10 for a round indicator.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateGlyphGeometry(animate: false);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ClearGlyphClocks();
            UpdateGlyphGeometry(animate: false);
        }

        private void UpdateGlyphGeometry(bool animate)
        {
            if (_checkGlyph is null)
            {
                return;
            }

            bool useAnimation = animate && IsLoaded && MotionHelper.IsMotionEnabled;
            CommonState state = !IsEnabled ? CommonState.Disabled : IsPressed ? CommonState.Pressed : IsMouseOver ? CommonState.PointerOver : CommonState.Normal;
            if (useAnimation && state.Equals(_commonState))
            {
                return;
            }

            _commonState = state;
            double targetSize = state is CommonState.Pressed ? PressedGlyphSize
                : state is CommonState.Normal ? RestGlyphSize : PointerOverGlyphSize;
            int duration = state is CommonState.Disabled ? FastAnimationMilliseconds : NormalAnimationMilliseconds;

            if (!useAnimation || targetSize == RestGlyphSize)
            {
                SetCheckGlyphSize(targetSize);
            }
            else
            {
                AnimateCheckGlyphSize(targetSize, duration);
            }

            if (_pressedCheckGlyph is null)
            {
                return;
            }

            bool showPressedGlyph = state is CommonState.Pressed;
            _pressedCheckGlyph.Opacity = showPressedGlyph ? 1 : 0;
            if (showPressedGlyph && useAnimation)
            {
                AnimatePressedGlyphSize();
            }
            else
            {
                SetPressedGlyphSize(showPressedGlyph ? PressedGlyphSize : PressedOverlayRestSize);
            }
        }

        private void AnimateCheckGlyphSize(double targetSize, int durationMilliseconds)
        {
            if (_checkGlyph is null)
            {
                return;
            }

            Ellipse glyph = _checkGlyph;
            int generation = ++_checkGlyphAnimationGeneration;
            DoubleAnimationUsingKeyFrames width = CreateSizeAnimation(targetSize, durationMilliseconds);
            DoubleAnimationUsingKeyFrames height = CreateSizeAnimation(targetSize, durationMilliseconds);
            height.Completed += delegate
            {
                if (generation == _checkGlyphAnimationGeneration && ReferenceEquals(glyph, _checkGlyph))
                {
                    SetCheckGlyphSize(targetSize);
                }
            };
            glyph.BeginAnimation(WidthProperty, width, HandoffBehavior.SnapshotAndReplace);
            glyph.BeginAnimation(HeightProperty, height, HandoffBehavior.SnapshotAndReplace);
        }

        private void AnimatePressedGlyphSize()
        {
            if (_pressedCheckGlyph is null)
            {
                return;
            }

            System.Windows.Controls.Border glyph = _pressedCheckGlyph;
            int generation = ++_pressedGlyphAnimationGeneration;
            DoubleAnimationUsingKeyFrames width = CreateSizeAnimation(PressedGlyphSize, FastAnimationMilliseconds);
            DoubleAnimationUsingKeyFrames height = CreateSizeAnimation(PressedGlyphSize, FastAnimationMilliseconds);
            height.Completed += delegate
            {
                if (generation == _pressedGlyphAnimationGeneration && ReferenceEquals(glyph, _pressedCheckGlyph))
                {
                    SetPressedGlyphSize(PressedGlyphSize);
                }
            };
            glyph.BeginAnimation(WidthProperty, width, HandoffBehavior.SnapshotAndReplace);
            glyph.BeginAnimation(HeightProperty, height, HandoffBehavior.SnapshotAndReplace);
        }

        private static DoubleAnimationUsingKeyFrames CreateSizeAnimation(double targetSize, int durationMilliseconds)
        {
            DoubleAnimationUsingKeyFrames animation = new() { FillBehavior = FillBehavior.Stop };
            _ = animation.KeyFrames.Add(new SplineDoubleKeyFrame(targetSize,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMilliseconds)),
                MotionHelper.FastOutSlowInKeySpline));
            return animation;
        }

        private void SetCheckGlyphSize(double size)
        {
            _checkGlyphAnimationGeneration++;
            if (_checkGlyph is not null)
            {
                _checkGlyph.BeginAnimation(WidthProperty, animation: null);
                _checkGlyph.BeginAnimation(HeightProperty, animation: null);
                _checkGlyph.Width = size;
                _checkGlyph.Height = size;
            }
        }

        private void SetPressedGlyphSize(double size)
        {
            _pressedGlyphAnimationGeneration++;
            if (_pressedCheckGlyph is not null)
            {
                _pressedCheckGlyph.BeginAnimation(WidthProperty, animation: null);
                _pressedCheckGlyph.BeginAnimation(HeightProperty, animation: null);
                _pressedCheckGlyph.Width = size;
                _pressedCheckGlyph.Height = size;
            }
        }

        private void ClearGlyphClocks()
        {
            _checkGlyphAnimationGeneration++;
            _pressedGlyphAnimationGeneration++;
            _checkGlyph?.BeginAnimation(WidthProperty, animation: null);
            _checkGlyph?.BeginAnimation(HeightProperty, animation: null);
            _pressedCheckGlyph?.BeginAnimation(WidthProperty, animation: null);
            _pressedCheckGlyph?.BeginAnimation(HeightProperty, animation: null);
        }

        private Ellipse? _checkGlyph;
        private System.Windows.Controls.Border? _pressedCheckGlyph;
        private int _checkGlyphAnimationGeneration;
        private int _pressedGlyphAnimationGeneration;
        private CommonState _commonState;
    }
}
