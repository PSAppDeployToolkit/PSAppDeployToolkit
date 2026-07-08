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

using Fluence.Wpf.Automation;
using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A toggle switch control with On/Off content.
    /// </summary>
    /// <remarks>Inspired by WinUI's ToggleSwitch.</remarks>
    [TemplatePart(Name = PartSwitchKnob, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartSwitchThumb, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartSwitchThumbInput, Type = typeof(Thumb))]
    public class ToggleSwitch : ToggleButton
    {
        private const string PartSwitchKnob = "SwitchKnob";
        private const string PartSwitchThumb = "SwitchThumb";
        private const string PartSwitchThumbInput = "PART_SwitchThumbInput";
        private const double KnobOffOffset = 0.0;
        private const double KnobOnOffset = 20.0;
        private const double DragCommitOffset = 10.0;
        private const double DragDistanceThreshold = 1.0;
        private const double ThumbRestSize = 12.0;
        private const double ThumbHoverSize = 14.0;
        private const double ThumbPressedWidth = 17.0;
        private const double ThumbPressedHeight = 14.0;
        private const double ThumbSizeAnimationMilliseconds = 83.0;
        private const double KnobAnimationMilliseconds = 167.0;

        /// <summary>
        /// Initializes static members of the ToggleSwitch class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ToggleSwitch control uses its custom default
        /// style by associating it with the appropriate style key. This is required for custom controls to apply their
        /// styles correctly in XAML.</remarks>
        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
        }

        /// <summary>
        /// Identifies the <see cref="OnContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OnContentProperty =
            DependencyProperty.Register(
                nameof(OnContent),
                typeof(object),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the content displayed when the switch is on.
        /// </summary>
        public object OnContent
        {
            get => GetValue(OnContentProperty);
            set => SetValue(OnContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OffContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffContentProperty =
            DependencyProperty.Register(
                nameof(OffContent),
                typeof(object),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the content displayed when the switch is off.
        /// </summary>
        public object OffContent
        {
            get => GetValue(OffContentProperty);
            set => SetValue(OffContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OnContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OnContentTemplateProperty =
            DependencyProperty.Register(
                nameof(OnContentTemplate),
                typeof(DataTemplate),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the data template for the on-state content.
        /// </summary>
        public DataTemplate OnContentTemplate
        {
            get => (DataTemplate)GetValue(OnContentTemplateProperty);
            set => SetValue(OnContentTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OffContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffContentTemplateProperty =
            DependencyProperty.Register(
                nameof(OffContentTemplate),
                typeof(DataTemplate),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the data template for the off-state content.
        /// </summary>
        public DataTemplate OffContentTemplate
        {
            get => (DataTemplate)GetValue(OffContentTemplateProperty);
            set => SetValue(OffContentTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(
                nameof(HeaderContent),
                typeof(object),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the header content displayed above the switch.
        /// </summary>
        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_thumbInput is not null)
            {
                _thumbInput.PreviewMouseLeftButtonDown -= OnThumbPreviewMouseLeftButtonDown;
                _thumbInput.PreviewMouseLeftButtonUp -= OnThumbPreviewMouseLeftButtonUp;
                _thumbInput.DragStarted -= OnThumbDragStarted;
                _thumbInput.DragDelta -= OnThumbDragDelta;
                _thumbInput.DragCompleted -= OnThumbDragCompleted;
                _thumbInput.LostMouseCapture -= OnThumbLostMouseCapture;
            }

            base.OnApplyTemplate();

            _switchKnob = GetTemplateChild(PartSwitchKnob) as FrameworkElement;
            _switchThumb = GetTemplateChild(PartSwitchThumb) as FrameworkElement;
            _thumbInput = GetTemplateChild(PartSwitchThumbInput) as Thumb;
            _knobTranslate = ResolveKnobTranslate();

            if (_thumbInput is not null)
            {
                _thumbInput.PreviewMouseLeftButtonDown += OnThumbPreviewMouseLeftButtonDown;
                _thumbInput.PreviewMouseLeftButtonUp += OnThumbPreviewMouseLeftButtonUp;
                _thumbInput.DragStarted += OnThumbDragStarted;
                _thumbInput.DragDelta += OnThumbDragDelta;
                _thumbInput.DragCompleted += OnThumbDragCompleted;
                _thumbInput.LostMouseCapture += OnThumbLostMouseCapture;
            }

            UpdateKnobPosition(useAnimation: false);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToggleSwitchAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            UpdateKnobPosition(useAnimation: true);
        }

        /// <inheritdoc />
        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            UpdateKnobPosition(useAnimation: true);
        }

        /// <inheritdoc />
        protected override void OnIndeterminate(RoutedEventArgs e)
        {
            base.OnIndeterminate(e);
            UpdateKnobPosition(useAnimation: true);
        }

        private TranslateTransform? ResolveKnobTranslate()
        {
            if (_switchKnob is null)
            {
                return null;
            }

            if (_switchKnob.RenderTransform is TranslateTransform transform && !transform.IsFrozen)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, animation: null);
                return transform;
            }

            TranslateTransform mutableTransform = new();
            _switchKnob.RenderTransform = mutableTransform;
            return mutableTransform;
        }

        private void OnThumbPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _pendingClick = true;
            _dragStarted = false;
            _dragDistance = 0.0;
            AnimateThumbSize(ThumbPressedWidth, ThumbPressedHeight, clearWhenCompleted: false);
        }

        private void OnThumbPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_pendingClick || _dragStarted)
            {
                return;
            }

            CompleteThumbInteraction(IsChecked is not true);
            e.Handled = true;
        }

        private void OnThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            _pendingClick = true;
            _dragStarted = true;
            _dragDistance = 0.0;
            AnimateThumbSize(ThumbPressedWidth, ThumbPressedHeight, clearWhenCompleted: false);
            e.Handled = true;
        }

        private void OnThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_knobTranslate is null)
            {
                return;
            }

            _dragDistance += Math.Abs(e.HorizontalChange);
            SetKnobOffset(Clamp(_knobTranslate.X + e.HorizontalChange, KnobOffOffset, KnobOnOffset));
            e.Handled = true;
        }

        private void OnThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (!_pendingClick)
            {
                return;
            }

            if (e.Canceled)
            {
                CancelThumbInteraction();
            }
            else
            {
                CompleteThumbInteraction(ResolveThumbInteractionCheckedState());
            }

            e.Handled = true;
        }

        private void OnThumbLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (!_pendingClick)
            {
                return;
            }

            if (Mouse.LeftButton is MouseButtonState.Released)
            {
                CompleteThumbInteraction(ResolveThumbInteractionCheckedState());
                return;
            }

            CancelThumbInteraction();
        }

        private bool ResolveThumbInteractionCheckedState()
        {
            return _dragDistance <= DragDistanceThreshold
                ? IsChecked is not true
                : (_knobTranslate?.X ?? KnobOffOffset) >= DragCommitOffset;
        }

        private void CancelThumbInteraction()
        {
            _pendingClick = false;
            _dragStarted = false;
            _dragDistance = 0.0;
            UpdateKnobPosition(useAnimation: true);
            AnimateThumbSize(GetReleasedThumbSize(), GetReleasedThumbSize(), clearWhenCompleted: true);
        }

        private void CompleteThumbInteraction(bool nextChecked)
        {
            bool currentChecked = IsChecked is true;
            _pendingClick = false;
            _dragStarted = false;
            _dragDistance = 0.0;

            SetCurrentValue(IsCheckedProperty, nextChecked);
            if (currentChecked == nextChecked)
            {
                UpdateKnobPosition(useAnimation: true);
            }

            AnimateThumbSize(GetReleasedThumbSize(), GetReleasedThumbSize(), clearWhenCompleted: true);
        }

        private void UpdateKnobPosition(bool useAnimation)
        {
            if (_knobTranslate is null)
            {
                return;
            }

            double targetOffset = IsChecked is true ? KnobOnOffset : KnobOffOffset;
            if (!useAnimation)
            {
                SetKnobOffset(targetOffset);
                return;
            }

            double currentOffset = _knobTranslate.X;
            if (Math.Abs(currentOffset - targetOffset) <= 0.1)
            {
                SetKnobOffset(targetOffset);
                return;
            }

            int animationGeneration = ++_knobAnimationGeneration;
            DoubleAnimation animation = new(currentOffset, targetOffset, TimeSpan.FromMilliseconds(KnobAnimationMilliseconds))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop,
            };

            animation.Completed += delegate
            {
                if (animationGeneration != _knobAnimationGeneration || _knobTranslate is null)
                {
                    return;
                }

                _knobTranslate.BeginAnimation(TranslateTransform.XProperty, animation: null);
                _knobTranslate.X = targetOffset;
            };

            _knobTranslate.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private void SetKnobOffset(double offset)
        {
            if (_knobTranslate is null)
            {
                return;
            }

            _knobAnimationGeneration++;
            _knobTranslate.BeginAnimation(TranslateTransform.XProperty, animation: null);
            _knobTranslate.X = offset;
        }

        private void AnimateThumbSize(double width, double height, bool clearWhenCompleted)
        {
            if (_switchThumb is null)
            {
                return;
            }

            int animationGeneration = ++_thumbSizeAnimationGeneration;
            DoubleAnimation widthAnimation = new(_switchThumb.Width, width, TimeSpan.FromMilliseconds(ThumbSizeAnimationMilliseconds))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = clearWhenCompleted ? FillBehavior.Stop : FillBehavior.HoldEnd,
            };
            DoubleAnimation heightAnimation = new(_switchThumb.Height, height, TimeSpan.FromMilliseconds(ThumbSizeAnimationMilliseconds))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = clearWhenCompleted ? FillBehavior.Stop : FillBehavior.HoldEnd,
            };

            if (clearWhenCompleted)
            {
                heightAnimation.Completed += delegate
                {
                    if (animationGeneration != _thumbSizeAnimationGeneration || _switchThumb is null)
                    {
                        return;
                    }

                    _switchThumb.BeginAnimation(WidthProperty, animation: null);
                    _switchThumb.BeginAnimation(HeightProperty, animation: null);
                };
            }

            _switchThumb.BeginAnimation(WidthProperty, widthAnimation, HandoffBehavior.SnapshotAndReplace);
            _switchThumb.BeginAnimation(HeightProperty, heightAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        private double GetReleasedThumbSize()
        {
            return IsMouseOver ? ThumbHoverSize : ThumbRestSize;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }

        private FrameworkElement? _switchKnob;
        private FrameworkElement? _switchThumb;
        private Thumb? _thumbInput;
        private TranslateTransform? _knobTranslate;
        private bool _pendingClick;
        private bool _dragStarted;
        private double _dragDistance;
        private int _knobAnimationGeneration;
        private int _thumbSizeAnimationGeneration;
    }
}
