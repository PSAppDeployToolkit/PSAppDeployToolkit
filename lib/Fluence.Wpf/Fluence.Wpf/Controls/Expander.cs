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
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled Expander with animated expand/collapse.
    /// </summary>
    /// <remarks>
    /// The content reveal mirrors the WinUI 3 Expander: the content presenter slides behind
    /// the already-clipped content border by the border's measured height, sliding in over
    /// 333 ms on expand and out over 167 ms on collapse. The discrete row swap remains the
    /// layout mechanism: the content row takes its space at expand start and releases it at
    /// collapse completion, so the slide itself is cosmetic-only inside the clip.
    /// </remarks>
    [TemplatePart(Name = PART_ContentBorder, Type = typeof(System.Windows.Controls.Border))]
    public class Expander : System.Windows.Controls.Expander
    {
        private const string PART_ContentBorder = "PART_ContentBorder";
        private const string ExpandSiteName = "ExpandSite";
        private const string Row0DefName = "Row0Def";
        private const string Row1DefName = "Row1Def";

        // WinUI Expander.xaml ExpandDown/CollapseDown splines (0,0,0,1 / 1,1,0,1), 333/167 ms.
        // These are the WinUI-literal Expander values, deliberately not the repo-wide 0.8,0,0,1
        // spline. 333 ms mirrors the Typography.xaml ControlSlowAnimationDuration token and
        // 167 ms mirrors ControlFastAnimationDuration; code-built animations cannot reference
        // the XAML TimeSpan tokens, so the values are mirrored here (ContentDialog pattern).
        private const double ExpandSlideMilliseconds = 333;
        private const double CollapseSlideMilliseconds = 167;

        // Fully qualified: Fluence.Wpf.Controls declares its own Border type, and the template
        // part is the plain WPF Border, so the unqualified name would never match the cast.
        private System.Windows.Controls.Border? _contentBorder;
        private TranslateTransform? _contentTranslate;
        private RowDefinition? _row0Def;
        private RowDefinition? _row1Def;
        private int _expandAnimationGeneration;
        private int _pendingExpandGeneration;
        private bool _pendingExpandFromRest;
        private double _pendingExpandCapturedY;

        /// <summary>
        /// Initializes static members of the Expander class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the Expander control uses its own style by
        /// default, rather than inheriting the style from its base class. This is important for custom control
        /// development in WPF, as it enables the control to apply its default template automatically.</remarks>
        static Expander()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Expander),
                new FrameworkPropertyMetadata(typeof(Expander)));
            ExpandDirectionProperty.OverrideMetadata(
                typeof(Expander),
                new FrameworkPropertyMetadata(
                    ExpandDirection.Down,
                    static (d, _) => ((Expander)d).ApplySteadyState()));
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(Expander),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the expander chrome.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets the row definition that hosts the content for the current expand direction
        /// (row 0 for <see cref="ExpandDirection.Up"/>, row 1 otherwise).
        /// </summary>
        private RowDefinition? ContentRow => ExpandDirection is ExpandDirection.Up ? _row0Def : _row1Def;

        /// <summary>
        /// Gets the row definition that hosts the header for the current expand direction.
        /// </summary>
        private RowDefinition? HeaderRow => ExpandDirection is ExpandDirection.Up ? _row1Def : _row0Def;

        /// <summary>
        /// Gets the sign of the fully hidden slide offset: the content hides above the clip
        /// (negative) when expanding down, and below it (positive) when expanding up.
        /// </summary>
        private double SlideSign => ExpandDirection is ExpandDirection.Up ? 1.0 : -1.0;

        /// <summary>
        /// Resolves the template parts that drive the content slide and applies the steady
        /// state for the current <see cref="System.Windows.Controls.Expander.IsExpanded"/>
        /// value without animation.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _expandAnimationGeneration++;
            LayoutUpdated -= OnExpandSlideLayoutReady;
            _contentBorder = GetTemplateChild(PART_ContentBorder) as System.Windows.Controls.Border;
            _row0Def = Template?.FindName(Row0DefName, this) as RowDefinition;
            _row1Def = Template?.FindName(Row1DefName, this) as RowDefinition;
            _contentTranslate = null;

            if (_contentBorder is null || _row0Def is null || _row1Def is null)
            {
                // Re-templated without the canonical parts (for example a consumer template
                // that owns its own motion): leave the replacement template alone.
                _contentBorder = null;
                _row0Def = null;
                _row1Def = null;
                return;
            }

            if (Template?.FindName(ExpandSiteName, this) is ContentPresenter expandSite)
            {
                // Inline mutable transform (never a frozen resource) so the code-driven slide
                // can retarget it mid-flight.
                _contentTranslate = new TranslateTransform();
                expandSite.RenderTransform = _contentTranslate;
            }

            ApplySteadyState();
        }

        /// <summary>
        /// Raises the base Expanded event, gives the content row its layout space, and starts
        /// the WinUI slide-in of the content from behind the clipped content border.
        /// </summary>
        protected override void OnExpanded()
        {
            base.OnExpanded();
            AnimateExpand();
        }

        /// <summary>
        /// Raises the base Collapsed event and starts the WinUI slide-out of the content
        /// behind the clipped content border, releasing the content row's layout space only
        /// when the slide completes.
        /// </summary>
        protected override void OnCollapsed()
        {
            base.OnCollapsed();
            AnimateCollapse();
        }

        /// <summary>
        /// Applies the non-animated steady state for the current expand direction and
        /// <see cref="System.Windows.Controls.Expander.IsExpanded"/> value: header row auto,
        /// content row star or zero, slide offset at rest. Cancels any in-flight slide.
        /// </summary>
        private void ApplySteadyState()
        {
            RowDefinition? contentRow = ContentRow;
            RowDefinition? headerRow = HeaderRow;
            if (contentRow is null || headerRow is null)
            {
                return;
            }

            _expandAnimationGeneration++;
            if (_contentTranslate is not null)
            {
                _contentTranslate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                _contentTranslate.Y = 0;
            }

            headerRow.Height = GridLength.Auto;
            contentRow.Height = IsExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        /// <summary>
        /// Opens the content row immediately and defers the slide-in one layout pass so the
        /// content border has its expanded height to measure the start offset from.
        /// </summary>
        private void AnimateExpand()
        {
            RowDefinition? contentRow = ContentRow;
            RowDefinition? headerRow = HeaderRow;
            if (contentRow is null || headerRow is null)
            {
                return;
            }

            // Motion disabled (OS "Show animations" off): open the content row directly at
            // its steady state instead of playing the slide-in.
            if (_contentBorder is null || _contentTranslate is null || !MotionHelper.IsMotionEnabled)
            {
                ApplySteadyState();
                return;
            }

            _expandAnimationGeneration++;

            // Capture-and-hold (NavigationView.AnimateIndicator pattern): read the live
            // animated offset BEFORE releasing the clock so a re-expand mid-collapse
            // continues the slide from wherever the content visually is right now.
            bool fromCollapsedRest = !contentRow.Height.IsStar;
            double capturedY = _contentTranslate.Y;
            _contentTranslate.BeginAnimation(TranslateTransform.YProperty, animation: null);
            _contentTranslate.Y = capturedY;

            // The discrete row swap stays the layout mechanism: the content takes its space
            // up front and the slide is cosmetic-only inside the clipped content border.
            headerRow.Height = GridLength.Auto;
            contentRow.Height = new GridLength(1, GridUnitType.Star);

            // Defer the slide one layout pass: a one-shot LayoutUpdated hook fires after the
            // row swap has laid out but before the frame renders, so the content never
            // flashes at rest before the slide seeds its start offset.
            _pendingExpandGeneration = _expandAnimationGeneration;
            _pendingExpandFromRest = fromCollapsedRest;
            _pendingExpandCapturedY = capturedY;
            LayoutUpdated -= OnExpandSlideLayoutReady;
            LayoutUpdated += OnExpandSlideLayoutReady;
        }

        /// <summary>
        /// One-shot <c>LayoutUpdated</c> hook that starts the deferred expand slide once the
        /// content row has taken its layout space.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnExpandSlideLayoutReady(object? sender, EventArgs e)
        {
            LayoutUpdated -= OnExpandSlideLayoutReady;
            if (_pendingExpandGeneration != _expandAnimationGeneration)
            {
                return;
            }

            StartExpandSlide(_pendingExpandGeneration, _pendingExpandFromRest, _pendingExpandCapturedY);
        }

        /// <summary>
        /// Starts the expand slide from the fully hidden offset (or the captured mid-flight
        /// offset) to rest, committing the rest offset when the clock completes.
        /// </summary>
        /// <param name="generation">The animation generation this slide belongs to.</param>
        /// <param name="fromCollapsedRest">True when expanding from the collapsed rest state.</param>
        /// <param name="capturedY">The live offset captured when the expand was requested.</param>
        private void StartExpandSlide(int generation, bool fromCollapsedRest, double capturedY)
        {
            if (_contentBorder is null || _contentTranslate is null)
            {
                return;
            }

            TranslateTransform translate = _contentTranslate;
            double contentHeight = _contentBorder.ActualHeight;
            if (contentHeight <= 0)
            {
                translate.Y = 0;
                return;
            }

            double startY = fromCollapsedRest ? SlideSign * contentHeight : capturedY;

            // Base value equals the end value so FillBehavior.Stop cannot flash a stale
            // offset when the clock completes (ContentDialog.BeginOpenAnimation pattern).
            translate.Y = 0;

            // WinUI Expander.xaml ExpandDownStoryboard: TranslateY from -contentHeight to 0
            // over 333 ms (ControlSlowAnimationDuration) on KeySpline 0.0,0.0,0.0,1.0; the Up
            // direction mirrors the sign.
            DoubleAnimationUsingKeyFrames slide = new()
            {
                FillBehavior = FillBehavior.Stop,
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(startY, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new SplineDoubleKeyFrame(
                        0.0,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ExpandSlideMilliseconds)),
                        new KeySpline(0.0, 0.0, 0.0, 1.0)),
                },
            };
            slide.Completed += (sender, args) =>
            {
                if (generation != _expandAnimationGeneration)
                {
                    return;
                }

                translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                translate.Y = 0;
            };
            translate.BeginAnimation(TranslateTransform.YProperty, slide, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Slides the content out behind the clip over the collapse duration, then closes the
        /// content row and resets the slide offset when the clock completes.
        /// </summary>
        private void AnimateCollapse()
        {
            RowDefinition? contentRow = ContentRow;
            RowDefinition? headerRow = HeaderRow;
            if (contentRow is null || headerRow is null)
            {
                return;
            }

            double contentHeight = _contentBorder?.ActualHeight ?? 0;
            if (_contentBorder is null || _contentTranslate is null || contentHeight <= 0 || !MotionHelper.IsMotionEnabled)
            {
                // Nothing measurable to slide (never laid out) or motion disabled by the OS
                // "Show animations" setting: close the row directly.
                ApplySteadyState();
                return;
            }

            _expandAnimationGeneration++;
            int generation = _expandAnimationGeneration;
            TranslateTransform translate = _contentTranslate;

            // Capture-and-hold: a collapse requested mid-expand starts the slide-out from the
            // content's current animated offset instead of snapping to rest first.
            double capturedY = translate.Y;
            translate.BeginAnimation(TranslateTransform.YProperty, animation: null);

            // Base value equals the post-collapse rest value; the running clock masks it.
            translate.Y = 0;

            double hiddenY = SlideSign * contentHeight;

            // WinUI Expander.xaml CollapseDownStoryboard mapped onto the Fluence row swap:
            // TranslateY from 0 to -contentHeight over 167 ms (ControlFastAnimationDuration)
            // on KeySpline 1.0,1.0,0.0,1.0 (Up mirrors the sign); the row releases its layout
            // space only when the slide completes so the retreat stays visible.
            DoubleAnimationUsingKeyFrames slide = new()
            {
                FillBehavior = FillBehavior.Stop,
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(capturedY, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new SplineDoubleKeyFrame(
                        hiddenY,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(CollapseSlideMilliseconds)),
                        new KeySpline(1.0, 1.0, 0.0, 1.0)),
                },
            };
            slide.Completed += (sender, args) =>
            {
                if (generation != _expandAnimationGeneration)
                {
                    return;
                }

                translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                translate.Y = 0;
                contentRow.Height = new GridLength(0);
            };
            translate.BeginAnimation(TranslateTransform.YProperty, slide, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
