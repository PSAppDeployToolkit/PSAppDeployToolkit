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
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Animates a <see cref="GridLength"/> dependency property (such as
    /// <see cref="ColumnDefinition"/>.<c>Width</c> or <see cref="RowDefinition"/>.<c>Height</c>)
    /// between two absolute pixel values using a <see cref="IEasingFunction"/>.
    /// </summary>
    /// <remarks>
    /// WPF ships no GridLength animator because <see cref="GridLength"/> is a struct with
    /// a <c>GridUnitType</c>. This implementation interpolates only the numeric value and
    /// preserves <see cref="GridUnitType.Pixel"/>, which is sufficient for navigation
    /// pane expand / collapse transitions.
    /// </remarks>
    public class GridLengthAnimation : AnimationTimeline
    {
        /// <summary>
        /// Identifies the <see cref="From"/> dependency property. A sentinel
        /// <see cref="GridLength"/> with <see cref="GridUnitType"/> = <see cref="GridUnitType.Auto"/>
        /// means "use the animated property's current value" (the standard WPF
        /// <c>Storyboard.To</c>-only pattern). Consumers who want an explicit
        /// starting width set <c>From</c> to a pixel value.
        /// </summary>
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register(
                nameof(From),
                typeof(GridLength),
                typeof(GridLengthAnimation),
                new PropertyMetadata(new GridLength(0, GridUnitType.Auto)));

        /// <summary>
        /// Identifies the <see cref="To"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register(
                nameof(To),
                typeof(GridLength),
                typeof(GridLengthAnimation),
                new PropertyMetadata(new GridLength(0, GridUnitType.Pixel)));

        /// <summary>
        /// Identifies the <see cref="EasingFunction"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register(
                nameof(EasingFunction),
                typeof(IEasingFunction),
                typeof(GridLengthAnimation),
                new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the starting value of the animation.
        /// </summary>
        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        /// <summary>
        /// Gets or sets the ending value of the animation.
        /// </summary>
        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        /// <summary>
        /// Gets or sets the easing function applied to the animation.
        /// </summary>
        public IEasingFunction EasingFunction
        {
            get => (IEasingFunction)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="Type"/> of value produced by this animation
        /// (<see cref="GridLength"/>).
        /// </summary>
        public override Type TargetPropertyType => typeof(GridLength);

        /// <summary>
        /// Returns the interpolated <see cref="GridLength"/> for the current animation time.
        /// </summary>
        /// <param name="defaultOriginValue">Default origin (unused; <see cref="From"/> wins).</param>
        /// <param name="defaultDestinationValue">Default destination (unused; <see cref="To"/> wins).</param>
        /// <param name="animationClock">Clock providing the normalised progress.</param>
        /// <returns>The interpolated current <see cref="GridLength"/>.</returns>
        /// <exception cref="InvalidOperationException">If <see cref="From"/> or <see cref="To"/> is not in pixels.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="animationClock"/> is <see langword="null"/>.</exception>
        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            // If From is left at its Auto sentinel (the common "To-only" case), start
            // from the property's current animated base value - this is what WPF does
            // for a DoubleAnimation with only To set, and is what keeps a reverse
            // collapse (280 -> 48) from snapping to 0 on the first frame.
            if (animationClock is null)
            {
                throw new ArgumentNullException(nameof(animationClock));
            }
            GridLength fromLength = From; double fromValue;
            if (fromLength.GridUnitType == GridUnitType.Auto)
            {
                GridLength originLength = defaultOriginValue is not GridLength origin
                    ? new GridLength(0d, GridUnitType.Pixel)
                    : origin;
                fromValue = originLength.Value;
            }
            else
            {
                fromValue = fromLength.Value;
            }
            double progress = animationClock.CurrentProgress ?? 0d;
            if (EasingFunction is not null)
            {
                progress = EasingFunction.Ease(progress);
            }
            double current = fromValue + ((To.Value - fromValue) * progress);
            return new GridLength(current, GridUnitType.Pixel);
        }

        /// <summary>
        /// Creates a new, frozen clone of this animation.
        /// </summary>
        /// <returns>A fresh <see cref="GridLengthAnimation"/> instance.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }
    }
}
