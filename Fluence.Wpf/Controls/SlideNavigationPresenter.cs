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
using System.Windows.Media;
using System.Windows.Media.Animation;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A content host that plays the WinUI 3 slide navigation transition whenever its
    /// <see cref="ContentControl.Content"/> changes: the outgoing content fades out as it slides
    /// away, and the incoming content slides in behind it from the side named by
    /// <see cref="TransitionEffect"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the WPF counterpart of navigating a WinUI <c language="csharp">Frame</c> with a
    /// <c language="csharp">SlideNavigationTransitionInfo</c>. The timings and offsets are the
    /// ones WinUI's own implementation uses for the horizontal effects
    /// (dxaml/phone/lib/ThemeTransitions.cpp, SlideNavigationTransitionInfo::CreateStoryboards):
    /// the outgoing content translates 150 px and fades to transparent over 150 ms on the
    /// 0.7,0,1,0.5 spline, and the incoming content waits out those 150 ms at a 200 px offset,
    /// then settles to rest over a further 300 ms on the 0.1,0.9,0.2,1 spline. Both offsets are
    /// mirrored for <see cref="SlideNavigationTransitionEffect.FromLeft"/>.
    /// </para>
    /// <para>
    /// The template carries two presenters so the two halves overlap the way they do in WinUI.
    /// The presenter that is not showing the current content is emptied once the transition
    /// settles, so only one copy of the content tree stays alive between transitions. The outgoing
    /// presenter is not hit-testable; the incoming one stays live for the whole slide, where WinUI
    /// takes hit testing off both (NavigateTransitionHelper::RemoveHitTestVisbility). A WPF clock
    /// that is replaced mid-flight never raises Completed, and restoring hit testing from that
    /// handler risked leaving the content permanently dead to the mouse. Both halves
    /// hold their end pose until that teardown runs, the way WinUI's own storyboards do; with
    /// FillBehavior.Stop the outgoing content would revert to full opacity at its rest position
    /// the moment its 150 ms clock ended and cover the arriving content for the remaining 300 ms.
    /// When motion is disabled the content is swapped with no animation at all.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_CurrentPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_PreviousPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_CurrentTranslate, Type = typeof(TranslateTransform))]
    [TemplatePart(Name = PART_PreviousTranslate, Type = typeof(TranslateTransform))]
    public class SlideNavigationPresenter : ContentControl
    {
        /// <summary>
        /// The name of the presenter showing the current content.
        /// </summary>
        private const string PART_CurrentPresenter = "PART_CurrentPresenter";

        /// <summary>
        /// The name of the presenter that holds the outgoing content while it slides away.
        /// </summary>
        private const string PART_PreviousPresenter = "PART_PreviousPresenter";

        /// <summary>
        /// The name of the current presenter's horizontal slide transform.
        /// </summary>
        private const string PART_CurrentTranslate = "PART_CurrentTranslate";

        /// <summary>
        /// The name of the outgoing presenter's horizontal slide transform.
        /// </summary>
        private const string PART_PreviousTranslate = "PART_PreviousTranslate";

        /// <summary>
        /// How long the outgoing content takes to leave, and how long the incoming content waits
        /// before it starts moving (WinUI outDuration).
        /// </summary>
        private const double ExitMilliseconds = 150;

        /// <summary>
        /// How long the incoming content takes to settle once it starts moving (WinUI inDuration).
        /// </summary>
        private const double EntranceMilliseconds = 300;

        /// <summary>
        /// How far the outgoing content travels, in device-independent pixels (WinUI
        /// translationExitOffset).
        /// </summary>
        private const double ExitOffsetPixels = 150;

        /// <summary>
        /// How far out the incoming content starts, in device-independent pixels (WinUI
        /// translationEntranceOffset, negated there and mirrored by the effect factor here).
        /// </summary>
        private const double EntranceOffsetPixels = 200;

        /// <summary>
        /// Initializes static members of the SlideNavigationPresenter class and overrides the
        /// default style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static SlideNavigationPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SlideNavigationPresenter),
                new FrameworkPropertyMetadata(typeof(SlideNavigationPresenter)));
        }

        /// <summary>
        /// Identifies the <see cref="TransitionEffect"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TransitionEffectProperty =
            DependencyProperty.Register(
                nameof(TransitionEffect),
                typeof(SlideNavigationTransitionEffect),
                typeof(SlideNavigationPresenter),
                new FrameworkPropertyMetadata(SlideNavigationTransitionEffect.FromRight),
                IsTransitionEffectDeclared);

        /// <summary>
        /// Rejects a <see cref="TransitionEffect"/> value the enumeration does not declare. The
        /// enumeration keeps WinUI's numbering and leaves 0 to WinUI's unported
        /// <c language="text">FromBottom</c>, so <c language="csharp">default</c> and a value
        /// ported from WinUI code that used <c language="text">FromBottom</c> both land here.
        /// Failing the set is better than silently playing the horizontal effect the caller did
        /// not ask for.
        /// </summary>
        /// <param name="value">The proposed value.</param>
        /// <returns><see langword="true"/> when the value is a declared member.</returns>
        private static bool IsTransitionEffectDeclared(object value)
        {
            return value is SlideNavigationTransitionEffect.FromLeft or SlideNavigationTransitionEffect.FromRight;
        }

        /// <summary>
        /// Gets or sets the side the incoming content enters from on the next content change.
        /// Set it before assigning <see cref="ContentControl.Content"/>, the way a WinUI caller
        /// passes a fresh transition info to each <c language="csharp">Frame.Navigate</c> call:
        /// forward through a set of peers is
        /// <see cref="SlideNavigationTransitionEffect.FromRight"/>, backward is
        /// <see cref="SlideNavigationTransitionEffect.FromLeft"/>.
        /// </summary>
        public SlideNavigationTransitionEffect TransitionEffect
        {
            get => (SlideNavigationTransitionEffect)GetValue(TransitionEffectProperty);
            set => SetValue(TransitionEffectProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _currentPresenter = GetTemplateChild(PART_CurrentPresenter) as ContentPresenter;
            _previousPresenter = GetTemplateChild(PART_PreviousPresenter) as ContentPresenter;
            _currentTranslate = GetTemplateChild(PART_CurrentTranslate) as TranslateTransform;
            _previousTranslate = GetTemplateChild(PART_PreviousTranslate) as TranslateTransform;

            // The first content never animates: a presenter that slid its initial content in
            // would play a transition nobody navigated to.
            _previousPresenter?.SetCurrentValue(ContentPresenter.ContentProperty, value: null);
        }

        /// <inheritdoc />
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            PlayTransition(oldContent);
        }

        /// <summary>
        /// Runs the two halves of the transition for a content change: the old content leaves
        /// from the outgoing presenter while the new one, already in the current presenter,
        /// slides in behind it.
        /// </summary>
        /// <param name="oldContent">The content that was showing before the change.</param>
        private void PlayTransition(object? oldContent)
        {
            if (_currentPresenter is null || _previousPresenter is null ||
                _currentTranslate is null || _previousTranslate is null)
            {
                return;
            }

            // A change that lands mid-transition settles the one in flight first. Besides
            // releasing its clocks, that frees the element the outgoing presenter still holds:
            // navigating straight back to it would otherwise hand the same element to the
            // current presenter while it is still parented by the outgoing one.
            StopTransition();

            if (oldContent is null || !MotionHelper.IsMotionEnabled)
            {
                return;
            }

            // The outgoing content has to leave the current presenter first: a UIElement cannot
            // sit in two visual trees at once, and ContentControl has already handed the new
            // content to the current presenter by the time this runs.
            _previousPresenter.SetCurrentValue(ContentPresenter.ContentProperty, oldContent);

            double direction = TransitionEffect is SlideNavigationTransitionEffect.FromLeft ? 1.0 : -1.0;

            _previousPresenter.SetCurrentValue(OpacityProperty, 1.0);
            _previousTranslate.SetCurrentValue(TranslateTransform.XProperty, 0.0);
            _currentPresenter.SetCurrentValue(OpacityProperty, 0.0);
            _currentTranslate.SetCurrentValue(TranslateTransform.XProperty, -EntranceOffsetPixels * direction);

            _previousPresenter.BeginAnimation(OpacityProperty, CreateExitOpacityAnimation(), HandoffBehavior.SnapshotAndReplace);
            _previousTranslate.BeginAnimation(TranslateTransform.XProperty, CreateExitSlideAnimation(direction), HandoffBehavior.SnapshotAndReplace);
            _currentPresenter.BeginAnimation(OpacityProperty, CreateEntranceOpacityAnimation(), HandoffBehavior.SnapshotAndReplace);

            DoubleAnimationUsingKeyFrames entranceSlide = CreateEntranceSlideAnimation(direction);
            entranceSlide.Completed += OnEntranceCompleted;
            _currentTranslate.BeginAnimation(TranslateTransform.XProperty, entranceSlide, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Releases both clocks, stamps the rest pose, and drops the outgoing content so the
        /// presenter holds one content tree between transitions.
        /// </summary>
        private void StopTransition()
        {
            _previousPresenter?.BeginAnimation(OpacityProperty, animation: null);
            _previousTranslate?.BeginAnimation(TranslateTransform.XProperty, animation: null);
            _currentPresenter?.BeginAnimation(OpacityProperty, animation: null);
            _currentTranslate?.BeginAnimation(TranslateTransform.XProperty, animation: null);

            _previousPresenter?.SetCurrentValue(ContentPresenter.ContentProperty, value: null);
            _previousPresenter?.SetCurrentValue(OpacityProperty, 0.0);
            _previousTranslate?.SetCurrentValue(TranslateTransform.XProperty, 0.0);
            _currentPresenter?.SetCurrentValue(OpacityProperty, 1.0);
            _currentTranslate?.SetCurrentValue(TranslateTransform.XProperty, 0.0);
        }

        /// <summary>
        /// Settles the transition once the incoming content has arrived.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnEntranceCompleted(object? sender, EventArgs e)
        {
            StopTransition();
        }

        /// <summary>
        /// Builds the outgoing fade: opaque until the slide starts, transparent when it ends.
        /// WinUI registers both as discrete frames, so the outgoing content holds full opacity
        /// for the whole of its travel and then cuts out.
        /// </summary>
        /// <returns>The outgoing opacity animation.</returns>
        private static DoubleAnimationUsingKeyFrames CreateExitOpacityAnimation()
        {
            return new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(ExitMilliseconds)),
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ExitMilliseconds))),
                },
            };
        }

        /// <summary>
        /// Builds the outgoing slide: rest to the exit offset over the exit duration on WinUI's
        /// accelerating outgoing spline.
        /// </summary>
        /// <param name="direction">1 for FromLeft, -1 for FromRight.</param>
        /// <returns>The outgoing slide animation.</returns>
        private static DoubleAnimationUsingKeyFrames CreateExitSlideAnimation(double direction)
        {
            return new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(ExitMilliseconds)),
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new SplineDoubleKeyFrame(
                        ExitOffsetPixels * direction,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ExitMilliseconds)),
                        new KeySpline(0.7, 0.0, 1.0, 0.5)),
                },
            };
        }

        /// <summary>
        /// Builds the incoming fade: transparent until the outgoing content has left, opaque
        /// from that instant on. WinUI registers both as discrete frames.
        /// </summary>
        /// <returns>The incoming opacity animation.</returns>
        private static DoubleAnimationUsingKeyFrames CreateEntranceOpacityAnimation()
        {
            return new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(ExitMilliseconds + EntranceMilliseconds)),
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ExitMilliseconds))),
                },
            };
        }

        /// <summary>
        /// Builds the incoming slide: held at the entrance offset while the outgoing content
        /// leaves, then settling at rest over the entrance duration on WinUI's decelerating
        /// incoming spline.
        /// </summary>
        /// <param name="direction">1 for FromLeft, -1 for FromRight.</param>
        /// <returns>The incoming slide animation.</returns>
        private static DoubleAnimationUsingKeyFrames CreateEntranceSlideAnimation(double direction)
        {
            return new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(ExitMilliseconds + EntranceMilliseconds)),
                KeyFrames =
                {
                    // WPF holds the property's base value until the first key frame, so the
                    // offset is stated at time zero as well as at the hand-off; WinUI's own
                    // storyboard needs only the hand-off frame because its transition target
                    // starts from the offset.
                    new DiscreteDoubleKeyFrame(
                        -EntranceOffsetPixels * direction,
                        KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new DiscreteDoubleKeyFrame(
                        -EntranceOffsetPixels * direction,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ExitMilliseconds))),
                    new SplineDoubleKeyFrame(
                        0.0,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ExitMilliseconds + EntranceMilliseconds)),
                        new KeySpline(0.1, 0.9, 0.2, 1.0)),
                },
            };
        }

        /// <summary>
        /// The presenter showing the current content, or null until the template is applied.
        /// </summary>
        private ContentPresenter? _currentPresenter;

        /// <summary>
        /// The presenter holding the outgoing content, or null until the template is applied.
        /// </summary>
        private ContentPresenter? _previousPresenter;

        /// <summary>
        /// The current presenter's slide transform, or null until the template is applied.
        /// </summary>
        private TranslateTransform? _currentTranslate;

        /// <summary>
        /// The outgoing presenter's slide transform, or null until the template is applied.
        /// </summary>
        private TranslateTransform? _previousTranslate;
    }
}
