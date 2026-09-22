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
using System.Windows.Threading;
using Fluence.Wpf.Helpers;

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
            SynchronizeOffset(CurrentVerticalOffsetProperty, VerticalOffset);
            SynchronizeOffset(CurrentHorizontalOffsetProperty, HorizontalOffset);
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
            if (e.ExtentHeightChange is not 0 || e.ViewportHeightChange is not 0
                || (e.VerticalChange is not 0 && IsExternalOffset(CurrentVerticalOffsetProperty, VerticalOffset)))
            {
                _targetVerticalOffset = Clamp(VerticalOffset, 0, ScrollableHeight);
                SynchronizeOffset(CurrentVerticalOffsetProperty, _targetVerticalOffset);
            }
            if (e.ExtentWidthChange is not 0 || e.ViewportWidthChange is not 0
                || (e.HorizontalChange is not 0 && IsExternalOffset(CurrentHorizontalOffsetProperty, HorizontalOffset)))
            {
                _targetHorizontalOffset = Clamp(HorizontalOffset, 0, ScrollableWidth);
                SynchronizeOffset(CurrentHorizontalOffsetProperty, _targetHorizontalOffset);
            }
        }

        private bool IsExternalOffset(DependencyProperty property, double offset)
        {
            // A clock may already have advanced while its next send waits for layout. Compare
            // against the last value actually sent, not the clock's newer effective value.
            double requested = property == CurrentVerticalOffsetProperty ? _requestedVerticalOffset : _requestedHorizontalOffset;
            return Math.Abs(offset - requested) > 1.0;
        }

        private void SynchronizeOffset(DependencyProperty property, double offset)
        {
            // These private properties have no consumer bindings. SetValue replaces their
            // animation base; SetCurrentValue would only override the effective value, and
            // removing the clock would expose the old base and queue a scroll back to it.
            SetValue(property, offset);
            BeginAnimation(property, animation: null);
            if (property == CurrentVerticalOffsetProperty)
            {
                _ = _pendingVerticalScroll?.Abort();
                _pendingVerticalScroll = null;
                _requestedVerticalOffset = offset;
            }
            else
            {
                _ = _pendingHorizontalScroll?.Abort();
                _pendingHorizontalScroll = null;
                _requestedHorizontalOffset = offset;
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
            ((SmoothScrollViewer)d).QueueAnimatedOffset(CurrentVerticalOffsetProperty, (double)e.NewValue);
        }

        private static readonly DependencyProperty CurrentHorizontalOffsetProperty =
            DependencyProperty.Register(
                "CurrentHorizontalOffset",
                typeof(double),
                typeof(SmoothScrollViewer),
                new PropertyMetadata(0.0, OnCurrentHorizontalOffsetChanged));

        private static void OnCurrentHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothScrollViewer)d).QueueAnimatedOffset(CurrentHorizontalOffsetProperty, (double)e.NewValue);
        }

        private void QueueAnimatedOffset(DependencyProperty property, double offset)
        {
            // The WPF scroll command queue replaces consecutive offset commands on the
            // same axis. Sending from a clock callback can therefore erase a consumer's queued
            // scroll request before layout publishes it. Let layout drain that queue first.
            // Loaded follows Render/layout but precedes Input, so wheel input cannot starve
            // animated offsets. An external offset change cancels the pending send.
            if (property == CurrentVerticalOffsetProperty)
            {
                _ = _pendingVerticalScroll?.Abort();
                _pendingVerticalScroll = Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() => ApplyOffset(property, offset)));
            }
            else
            {
                _ = _pendingHorizontalScroll?.Abort();
                _pendingHorizontalScroll = Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                    new Action(() => ApplyOffset(property, offset)));
            }
        }

        private void ApplyOffset(DependencyProperty property, double offset)
        {
            if (property == CurrentVerticalOffsetProperty)
            {
                _pendingVerticalScroll = null;
                _requestedVerticalOffset = offset;
                ScrollToVerticalOffset(offset);
            }
            else
            {
                _pendingHorizontalScroll = null;
                _requestedHorizontalOffset = offset;
                ScrollToHorizontalOffset(offset);
            }
        }

        private void AnimateTo(DependencyProperty property, double to)
        {
            // Motion disabled (OS "Show animations" off): release any in-flight tween and jump
            // straight to the target offset; the DP change callback performs the actual scroll.
            if (!MotionHelper.IsMotionEnabled)
            {
                SynchronizeOffset(property, to);
                ApplyOffset(property, to);
                return;
            }

            DoubleAnimation animation = new()
            {
                From = property == CurrentVerticalOffsetProperty ? VerticalOffset : HorizontalOffset,
                To = to,
                Duration = ScrollDuration,
                EasingFunction = SharedEase,
            };
            animation.Freeze();
            BeginAnimation(property, animation);
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

        private double _requestedVerticalOffset;
        private double _requestedHorizontalOffset;
        private DispatcherOperation? _pendingVerticalScroll;
        private DispatcherOperation? _pendingHorizontalScroll;

        /// <summary>
        /// Provides a shared instance of a cubic easing function configured for ease-out transitions.
        /// </summary>
        /// <remarks>This static field can be reused to apply a consistent cubic ease-out animation across
        /// multiple operations, reducing object allocations.</remarks>
        private static readonly CubicEase SharedEase = new() { EasingMode = EasingMode.EaseOut };
    }
}
