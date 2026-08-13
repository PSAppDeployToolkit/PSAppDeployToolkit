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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Displays the content of a <see cref="Flyout"/> on the canonical Fluent flyout surface
    /// (flyout background fill, flyout stroke, overlay corner radius). The themed template
    /// lives in <c>Themes/Controls/FlyoutPresenter.xaml</c>.
    /// </summary>
    [TemplatePart(Name = PresenterSurfacePart, Type = typeof(Border))]
    [TemplatePart(Name = PresenterTranslatePart, Type = typeof(TranslateTransform))]
    public class FlyoutPresenter : ContentControl
    {
        /// <summary>
        /// The name of the surface Border template part whose opacity the open reveal fades.
        /// </summary>
        private const string PresenterSurfacePart = "PresenterSurface";

        /// <summary>
        /// The name of the TranslateTransform template part the open reveal slides.
        /// </summary>
        private const string PresenterTranslatePart = "PresenterTranslate";

        /// <summary>
        /// The duration of the open reveal slide and fade, mirroring the value of the
        /// ControlFastAnimationDuration motion token (Themes/Typography/Typography.xaml),
        /// which code mirrors by value like the previous template storyboard did.
        /// </summary>
        private const double RevealMilliseconds = 167;

        /// <summary>
        /// The distance in device-independent pixels the surface slides in from the
        /// placement side during the open reveal.
        /// </summary>
        private const double RevealOffsetPixels = 8;

        /// <summary>
        /// Identifies the <see cref="RevealPlacement"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty RevealPlacementProperty =
            DependencyProperty.Register(
                nameof(RevealPlacement),
                typeof(PlacementMode),
                typeof(FlyoutPresenter),
                new PropertyMetadata(PlacementMode.Bottom));

        /// <summary>
        /// Initializes static members of the <see cref="FlyoutPresenter"/> class and overrides
        /// the default style metadata so the themed template in Generic.xaml applies.
        /// </summary>
        static FlyoutPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FlyoutPresenter),
                new FrameworkPropertyMetadata(typeof(FlyoutPresenter)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyoutPresenter"/> class and subscribes
        /// the open reveal to <see cref="FrameworkElement.Loaded"/>. The presenter instance is
        /// reused across popup opens and re-raises Loaded on every open, so the reveal replays
        /// each time the flyout shows.
        /// </summary>
        public FlyoutPresenter()
        {
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Gets or sets the resolved side of the placement target the flyout was requested to
        /// open on, stamped by <see cref="FlyoutBase.ShowAt"/> (via
        /// <see cref="FlyoutBase.MapPlacementSide"/>) before the host popup opens. The open
        /// reveal slides the surface in from this side. Defaults to
        /// <see cref="PlacementMode.Bottom"/>, the dominant flyout placement.
        /// </summary>
        internal PlacementMode RevealPlacement
        {
            get => (PlacementMode)GetValue(RevealPlacementProperty);
            set => SetValue(RevealPlacementProperty, value);
        }

        /// <summary>
        /// Plays the open reveal each time the presenter loads inside the host popup: the
        /// surface slides 8 px in from the side selected by <see cref="RevealPlacement"/>
        /// (Bottom slides down from above the rest position, Top slides up, Right slides
        /// right, Left slides left) while fading 0 to 1, mirroring the previous template
        /// storyboard: 167 ms on the 0.8,0,0,1 spline (the Typography.xaml
        /// ControlFastAnimationDuration and ControlFastOutSlowInKeySpline motion tokens,
        /// mirrored by value). The animations use <see cref="FillBehavior.Stop"/>; the
        /// completed handlers stamp the rest values and release the clocks so nothing stays
        /// animated once the reveal settles.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // A presenter whose Loaded fires without an applied template (a collapsed
            // declaration) has no reveal parts to animate.
            if (GetTemplateChild(PresenterTranslatePart) is not TranslateTransform translate ||
                GetTemplateChild(PresenterSurfacePart) is not Border surface)
            {
                return;
            }

            // Motion disabled (OS "Show animations" off): skip the reveal and show the surface
            // at rest immediately - translate (0,0), full opacity, no clocks.
            if (!MotionHelper.IsMotionEnabled)
            {
                translate.BeginAnimation(TranslateTransform.XProperty, animation: null);
                translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                translate.SetCurrentValue(TranslateTransform.XProperty, 0.0);
                translate.SetCurrentValue(TranslateTransform.YProperty, 0.0);
                surface.BeginAnimation(UIElement.OpacityProperty, animation: null);
                surface.SetCurrentValue(UIElement.OpacityProperty, 1.0);
                return;
            }

            PlacementMode side = RevealPlacement;
            bool slidesHorizontally = side is PlacementMode.Left or PlacementMode.Right;
            double startOffset = side is PlacementMode.Top or PlacementMode.Left
                ? RevealOffsetPixels
                : -RevealOffsetPixels;
            DependencyProperty slideProperty = slidesHorizontally ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            DependencyProperty restProperty = slidesHorizontally ? TranslateTransform.YProperty : TranslateTransform.XProperty;

            // Seed the discrete start so the first rendered frame never flashes the rest
            // position: the offset on the chosen axis, 0 on the other, fully transparent.
            translate.SetCurrentValue(slideProperty, startOffset);
            translate.SetCurrentValue(restProperty, 0.0);
            surface.SetCurrentValue(UIElement.OpacityProperty, 0.0);

            DoubleAnimationUsingKeyFrames slideAnimation = CreateRevealAnimation(startOffset, 0.0);
            slideAnimation.Completed += (_, _) =>
            {
                translate.SetCurrentValue(slideProperty, 0.0);
                translate.BeginAnimation(slideProperty, animation: null);
            };

            DoubleAnimationUsingKeyFrames fadeAnimation = CreateRevealAnimation(0.0, 1.0);
            fadeAnimation.Completed += (_, _) =>
            {
                surface.SetCurrentValue(UIElement.OpacityProperty, 1.0);
                surface.BeginAnimation(UIElement.OpacityProperty, animation: null);
            };

            translate.BeginAnimation(slideProperty, slideAnimation);
            surface.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// Builds one track of the open reveal: a discrete start at time zero settling at the
        /// rest value over the fast motion duration on the decelerating Fluent key spline
        /// (see <see cref="OnLoaded"/> for the mirrored Typography.xaml motion tokens).
        /// </summary>
        /// <param name="from">The discrete start value.</param>
        /// <param name="to">The rest value reached when the reveal settles.</param>
        /// <returns>The keyframe animation for the track.</returns>
        private static DoubleAnimationUsingKeyFrames CreateRevealAnimation(double from, double to)
        {
            return new DoubleAnimationUsingKeyFrames
            {
                FillBehavior = FillBehavior.Stop,
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new SplineDoubleKeyFrame(
                        to,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(RevealMilliseconds)),
                        new KeySpline(0.8, 0.0, 0.0, 1.0)),
                },
            };
        }
    }
}
