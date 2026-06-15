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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A scroll viewer that animates scrolling with easing for a smooth experience.
    /// </summary>
    [TemplatePart(Name = "PART_ScrollContentPresenter", Type = typeof(ScrollContentPresenter))]
    [TemplatePart(Name = "PART_VerticalScrollBar", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "PART_HorizontalScrollBar", Type = typeof(ScrollBar))]
    public class SmoothScrollViewer : ScrollViewer
    {
        /// <summary>
        /// Identifies the <see cref="ScrollDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollDurationProperty =
            DependencyProperty.Register(
                "ScrollDuration",
                typeof(Duration),
                typeof(SmoothScrollViewer),
                new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(250))));

        /// <summary>
        /// Gets or sets the duration of the smooth scroll animation.
        /// </summary>
        public Duration ScrollDuration
        {
            get => (Duration)GetValue(ScrollDurationProperty);
            set => SetValue(ScrollDurationProperty, value);
        }

        /// <summary>
        /// Initializes static members of the SmoothScrollViewer class.
        /// </summary>
        /// <remarks>This static constructor is called automatically to perform type-level initialization
        /// before any static members are accessed or any instances are created.</remarks>

        static SmoothScrollViewer()
        {
            SharedEase.Freeze();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _targetVerticalOffset = VerticalOffset;
            _targetHorizontalOffset = HorizontalOffset;
        }

        /// <inheritdoc />
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // Animate an internal DP rather than repeatedly calling ScrollTo* from the
            // wheel handler. The DP callback performs the actual scroll, while the target
            // offset lets quick wheel input coalesce into a single eased destination.
            bool isHorizontal = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            double delta = e.Delta * 0.6;
            if (isHorizontal)
            {
                double scrollable = ScrollableWidth;
                _targetHorizontalOffset = Clamp(_targetHorizontalOffset, 0, scrollable);
                double newTarget = Clamp(_targetHorizontalOffset - delta, 0, scrollable);
                if (newTarget == _targetHorizontalOffset)
                {
                    return;
                }
                _targetHorizontalOffset = newTarget;
                AnimateTo(CurrentHorizontalOffsetProperty, _targetHorizontalOffset);
            }
            else
            {
                double scrollable = ScrollableHeight;
                _targetVerticalOffset = Clamp(_targetVerticalOffset, 0, scrollable);
                double newTarget = Clamp(_targetVerticalOffset - delta, 0, scrollable);
                if (newTarget == _targetVerticalOffset)
                {
                    return;
                }
                _targetVerticalOffset = newTarget;
                AnimateTo(CurrentVerticalOffsetProperty, _targetVerticalOffset);
            }
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            base.OnScrollChanged(e);
            if (e.ExtentHeightChange is not 0 || e.ViewportHeightChange is not 0)
            {
                _targetVerticalOffset = Clamp(VerticalOffset, 0, ScrollableHeight);
            }
            if (e.ExtentWidthChange is not 0 || e.ViewportWidthChange is not 0)
            {
                _targetHorizontalOffset = Clamp(HorizontalOffset, 0, ScrollableWidth);
            }
        }

        private static readonly DependencyProperty CurrentVerticalOffsetProperty =
            DependencyProperty.Register(
                "CurrentVerticalOffset",
                typeof(double),
                typeof(SmoothScrollViewer),
                new PropertyMetadata(0.0, OnCurrentVerticalOffsetChanged));

        private static void OnCurrentVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothScrollViewer)d).ScrollToVerticalOffset((double)e.NewValue);
        }

        private static readonly DependencyProperty CurrentHorizontalOffsetProperty =
            DependencyProperty.Register(
                "CurrentHorizontalOffset",
                typeof(double),
                typeof(SmoothScrollViewer),
                new PropertyMetadata(0.0, OnCurrentHorizontalOffsetChanged));

        private static void OnCurrentHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothScrollViewer)d).ScrollToHorizontalOffset((double)e.NewValue);
        }

        private void AnimateTo(DependencyProperty property, double to)
        {
            DoubleAnimation animation = new()
            {
                To = to,
                Duration = ScrollDuration,
                EasingFunction = SharedEase,
            };
            animation.Freeze();
            BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        /// <summary>
        /// Represents the target vertical offset value used for scrolling or positioning operations.
        /// </summary>
        private double _targetVerticalOffset;

        /// <summary>
        /// Represents the target horizontal offset value used for scrolling or positioning operations.
        /// </summary>
        private double _targetHorizontalOffset;

        /// <summary>
        /// Provides a shared instance of a cubic easing function configured for ease-out transitions.
        /// </summary>
        /// <remarks>This static field can be reused to apply a consistent cubic ease-out animation across
        /// multiple operations, reducing object allocations.</remarks>
        private static readonly CubicEase SharedEase = new() { EasingMode = EasingMode.EaseOut };
    }
}
