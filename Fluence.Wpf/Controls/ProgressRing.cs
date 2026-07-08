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
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A circular progress indicator that supports both determinate and indeterminate modes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Indeterminate animation follows the WinUI 3 motion model:
    /// a private sweep-fraction dependency property pulses the arc
    /// length from zero to half the circumference and back over a 2 second linear cycle,
    /// while the template's <c>PART_IndeterminateRotate</c> transform spins the arc from
    /// 90 to 1170 degrees over the same 2 second cycle.
    /// </para>
    /// <para>
    /// The legacy orbit-dot template settings (<see cref="EllipseDiameter"/> /
    /// <see cref="EllipseOffset"/>) are retained for custom templates and compatibility with
    /// earlier Fluence versions. The default template no longer consumes them.
    /// </para>
    /// <para>
    /// Determinate mode renders a stroked arc through <see cref="ArcSegment"/>; the arc end-angle
    /// tweens for 367 ms with a WinUI-style decelerating key spline when <see cref="Value"/> changes.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_IndeterminateArc, Type = typeof(Path))]
    [TemplatePart(Name = PART_IndeterminateRotate, Type = typeof(RotateTransform))]
    [TemplatePart(Name = PART_DeterminateArc, Type = typeof(Path))]
    public class ProgressRing : Control
    {
        // Template part names.
        private const string PART_IndeterminateArc = "PART_IndeterminateArc";
        private const string PART_IndeterminateRotate = "PART_IndeterminateRotate";
        private const string PART_DeterminateArc = "PART_DeterminateArc";

        // Indeterminate arc-length pulse + rotation values (WinUI 3 model).
        private const double IndeterminatePeakSweepFraction = 0.5;
        private const double IndeterminateRotationStartAngle = 90.0;
        private const double IndeterminateRotationEndAngle = 1170.0;
        private const double FullCircleLimit = 359.99;

        // State brush resource keys applied from code-behind.
        private const string CriticalBrushKey = "SystemFillColorCriticalBrush";
        private const string CautionBrushKey = "SystemFillColorCautionBrush";

        // Animation parameters.
        private static readonly Duration IndeterminateAnimationDuration = new(TimeSpan.FromMilliseconds(2000));
        private static readonly Duration DeterminateAnimationDuration = new(TimeSpan.FromMilliseconds(367));

        /// <summary>
        /// Initializes static members of the ProgressRing class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ProgressRing control uses its custom style
        /// by default. It is called automatically before any static members are accessed or any instances are
        /// created.</remarks>
        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(typeof(ProgressRing)));
            AutomationProperties.LiveSettingProperty.OverrideMetadata(
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(AutomationLiveSetting.Polite));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressRing"/> class.
        /// </summary>
        public ProgressRing()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Identifies the <see cref="IsActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(defaultValue: true, OnIsActiveChanged));

        /// <summary>
        /// Gets or sets whether the progress ring is active and visible.
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsIndeterminate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register(
                nameof(IsIndeterminate),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(defaultValue: true, OnIsIndeterminateChanged));

        /// <summary>
        /// Gets or sets whether the ring operates in indeterminate (spinning) mode.
        /// </summary>
        public bool IsIndeterminate
        {
            get => (bool)GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnRangePropertyChanged, CoerceRingValue));

        /// <summary>
        /// Gets or sets the current progress value in determinate mode.
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnMinMaxPropertyChanged));

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(100.0, OnMinMaxPropertyChanged));

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(4.0, OnStrokeThicknessChanged));

        /// <summary>
        /// Gets or sets the thickness of the progress arc stroke.
        /// </summary>
        /// <remarks>Applies to both determinate and indeterminate arc visuals in the default template.</remarks>
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressState"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressStateProperty =
            DependencyProperty.Register(
                nameof(ProgressState),
                typeof(ProgressRingState),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(ProgressRingState.Normal, OnProgressStateChanged));

        /// <summary>
        /// Gets or sets the visual state used to color the progress arc.
        /// </summary>
        /// <remarks>
        /// Backward-compatibility alias for the orthogonal <see cref="ShowPaused"/> and
        /// <see cref="ShowError"/> flags. Setting this property maps one way onto the flags
        /// (<see cref="ProgressRingState.Normal"/> clears both); setting the flags directly
        /// leaves this property unchanged.
        /// </remarks>
        public ProgressRingState ProgressState
        {
            get => (ProgressRingState)GetValue(ProgressStateProperty);
            set => SetValue(ProgressStateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowError"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowErrorProperty =
            DependencyProperty.Register(
                nameof(ShowError),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(defaultValue: false, OnShowErrorChanged));

        /// <summary>
        /// Gets or sets whether the ring renders its arcs in the error state using the
        /// system critical brush.
        /// </summary>
        /// <remarks>
        /// In indeterminate mode the arc keeps spinning while the error state is shown.
        /// Takes precedence over <see cref="ShowPaused"/> when both flags are set. Setting
        /// this flag directly does not change the legacy <see cref="ProgressState"/> alias.
        /// </remarks>
        public bool ShowError
        {
            get => (bool)GetValue(ShowErrorProperty);
            set => SetValue(ShowErrorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowPaused"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPausedProperty =
            DependencyProperty.Register(
                nameof(ShowPaused),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(defaultValue: false, OnShowPausedChanged));

        /// <summary>
        /// Gets or sets whether the ring renders its arcs in the paused state using the
        /// system caution brush.
        /// </summary>
        /// <remarks>
        /// In indeterminate mode the spinning animation stops and a static half-circle arc is
        /// rendered instead. <see cref="ShowError"/> takes precedence when both flags are set.
        /// Setting this flag directly does not change the legacy <see cref="ProgressState"/> alias.
        /// </remarks>
        public bool ShowPaused
        {
            get => (bool)GetValue(ShowPausedProperty);
            set => SetValue(ShowPausedProperty, value);
        }

        private static readonly DependencyPropertyKey EllipseDiameterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseDiameter),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0));

        /// <summary>
        /// Identifies the read-only <see cref="EllipseDiameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EllipseDiameterProperty = EllipseDiameterPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the diameter that earlier orbit-dot templates use for each indeterminate-mode dot.
        /// The default Fluence template now uses an arc, but this value is retained for custom
        /// templates and compatibility.
        /// </summary>
        public double EllipseDiameter => (double)GetValue(EllipseDiameterProperty);

        private static readonly DependencyPropertyKey EllipseOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseOffset),
                typeof(Thickness),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(default(Thickness)));

        /// <summary>
        /// Identifies the read-only <see cref="EllipseOffset"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EllipseOffsetProperty = EllipseOffsetPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the top-margin offset used by earlier orbit-dot templates to position each dot on
        /// the orbit radius. The default Fluence template now uses an arc, but this value is retained
        /// for custom templates and compatibility.
        /// </summary>
        public Thickness EllipseOffset => (Thickness)GetValue(EllipseOffsetProperty);

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Recreate geometry objects so mutations target the current template's path elements.
            base.OnApplyTemplate();
            _indeterminateArcPath = GetTemplateChild(PART_IndeterminateArc) as Path;
            _arcPath = GetTemplateChild(PART_DeterminateArc) as Path;
            _indeterminateRotateTransform = GetTemplateChild(PART_IndeterminateRotate) as RotateTransform;

            // Any previous template's rotation animation died with its visual tree; clear the
            // running flag so UpdateIndeterminateAnimationState performs a full restart that
            // also animates the new template's rotate transform.
            _isIndeterminateAnimationRunning = false;
            _indeterminateArcSegment = new ArcSegment { SweepDirection = SweepDirection.Clockwise };
            _indeterminateFigure = new PathFigure { IsClosed = false };
            _indeterminateFigure.Segments.Add(_indeterminateArcSegment);
            _indeterminateGeometry = new PathGeometry();
            _indeterminateGeometry.Figures.Add(_indeterminateFigure);
            _determinateArcSegment = new ArcSegment { SweepDirection = SweepDirection.Clockwise };
            _determinateFigure = new PathFigure { IsClosed = false };
            _determinateFigure.Segments.Add(_determinateArcSegment);
            _determinateGeometry = new PathGeometry();
            _determinateGeometry.Figures.Add(_determinateFigure);
            UpdateTemplateSettings();
            UpdateArcStrokes();
            if (!IsIndeterminate)
            {
                // Force rendering: AnimatedFraction may already equal the target value
                // (set before the template applied), in which case the property-changed
                // callback never fires and the arc would stay blank.
                double fraction = ComputeFraction();
                AnimatedFraction = fraction;
                RenderDeterminateArc(fraction);
            }
            else
            {
                RenderIndeterminateArc();
            }
            UpdateIndeterminateAnimationState();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ProgressRingAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateTemplateSettings();
            if (!IsIndeterminate)
            {
                RenderDeterminateArc(AnimatedFraction);
            }
            else
            {
                RenderIndeterminateArc();
            }
        }

        private static readonly DependencyProperty AnimatedFractionProperty =
            DependencyProperty.Register(
                "AnimatedFraction",
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnAnimatedFractionChanged));

        private double AnimatedFraction
        {
            get => (double)GetValue(AnimatedFractionProperty);
            set => SetValue(AnimatedFractionProperty, value);
        }

        private static void OnAnimatedFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).RenderDeterminateArc((double)e.NewValue);
        }

        private static readonly DependencyProperty IndeterminateSweepFractionProperty =
            DependencyProperty.Register(
                "IndeterminateSweepFraction",
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnIndeterminateSweepFractionChanged));

        private double IndeterminateSweepFraction
        {
            get => (double)GetValue(IndeterminateSweepFractionProperty);
            set => SetValue(IndeterminateSweepFractionProperty, value);
        }

        private static void OnIndeterminateSweepFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).RenderIndeterminateArc();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateIndeterminateAnimationState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopIndeterminateAnimation();
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).UpdateIndeterminateAnimationState();
        }

        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressRing ring = (ProgressRing)d;
            if ((bool)e.NewValue)
            {
                // Switching to indeterminate: stop any in-flight value tween (otherwise its
                // Completed callback will re-render the arc geometry we just cleared), then
                // null out determinate arc data.  The code-driven sweep-fraction pulse and
                // template rotation render the indeterminate arc when the control is active.
                ring.BeginAnimation(AnimatedFractionProperty, animation: null);
                _ = ring._arcPath?.Data = null;
                ring.UpdateIndeterminateAnimationState();
            }
            else
            {
                // Switching to determinate: render arc to the current value (no transition tween).
                ring.StopIndeterminateAnimation();
                ring.AnimatedFraction = ring.ComputeFraction();
                ring.RenderDeterminateArc(ring.AnimatedFraction);
            }
        }

        private static void OnProgressStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // One-way backward-compat alias: project the enum onto the orthogonal flags.
            // The guard blocks reentrancy in case a subclass or binding writes the enum back
            // while the flags are being synchronized.
            ProgressRing ring = (ProgressRing)d;
            if (ring._isSyncingProgressState)
            {
                return;
            }
            ring._isSyncingProgressState = true;
            try
            {
                ProgressRingState state = (ProgressRingState)e.NewValue;
                ring.SetCurrentValue(ShowPausedProperty, state is ProgressRingState.Paused);
                ring.SetCurrentValue(ShowErrorProperty, state is ProgressRingState.Error);
            }
            finally
            {
                ring._isSyncingProgressState = false;
            }
        }

        private static void OnShowErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).OnStateFlagsChanged();
        }

        private static void OnShowPausedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).OnStateFlagsChanged();
        }

        private void OnStateFlagsChanged()
        {
            UpdateArcStrokes();
            UpdateIndeterminateAnimationState();
            if (!IsIndeterminate)
            {
                RenderDeterminateArc(AnimatedFraction);
            }
            AnnounceLiveRegion();
        }

        /// <summary>
        /// Raises <see cref="AutomationEvents.LiveRegionChanged"/> on this control's automation peer
        /// so Narrator announces the error or paused state change without moving focus.
        /// Uses only net472-safe APIs (no RaiseNotificationEvent).
        /// </summary>
        private void AnnounceLiveRegion()
        {
            if (!AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
            {
                return;
            }

            // CreatePeerForElement is annotated non-null, so peer is provably non-null here (CA1508
            // rejects a redundant null guard); no NullReferenceException is possible.
            AutomationPeer peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
            peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private static object CoerceRingValue(DependencyObject d, object baseValue)
        {
            ProgressRing ring = (ProgressRing)d;
            return Math.Min(Math.Max((double)baseValue, ring.Minimum), ring.Maximum);
        }

        private static void OnMinMaxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Re-coerce Value so it stays within the new bounds, then redraw.
            d.CoerceValue(ValueProperty);
            OnRangePropertyChanged(d, e);
        }

        private static void OnRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressRing ring = (ProgressRing)d;

            // Raise RangeValue ValueProperty change event when Value itself changes so Narrator
            // can read current progress on demand in determinate mode.
            if (ReferenceEquals(e.Property, ValueProperty) && !ring.IsIndeterminate)
            {
                ring.RaiseRangeValueChanged((double)e.OldValue, (double)e.NewValue);
            }

            if (ring.IsIndeterminate)
            {
                return;
            }

            // No tween before the template has applied - OnApplyTemplate will render the
            // initial frame synchronously.  Tweening here would race with the layout pass
            // and leave the arc blank when the dispatcher drains mid-animation.
            double targetFraction = ring.ComputeFraction();
            if (ring._arcPath is null)
            {
                ring.AnimatedFraction = targetFraction;
                return;
            }

            // FillBehavior.Stop keeps the private DP from being held by the animation
            // clock after completion; the completion handler commits the final value so
            // the next tween starts from the rendered arc position.  The single spline
            // keyframe approximates the WinUI value-change easing (KeySpline 0.1,0.9 0.2,1.0)
            // over 367 ms; the default SnapshotAndReplace handoff makes the tween start from
            // the currently rendered fraction.
            DoubleAnimationUsingKeyFrames animation = new()
            {
                Duration = DeterminateAnimationDuration,
                FillBehavior = FillBehavior.Stop,
            };
            _ = animation.KeyFrames.Add(
                new SplineDoubleKeyFrame(
                    targetFraction,
                    KeyTime.FromTimeSpan(DeterminateAnimationDuration.TimeSpan),
                    new KeySpline(0.1, 0.9, 0.2, 1.0)));
            animation.Completed += (s, args) => ring.AnimatedFraction = targetFraction;
            ring.BeginAnimation(AnimatedFractionProperty, animation);
        }

        /// <summary>
        /// Raises a UI Automation <see cref="System.Windows.Automation.RangeValuePatternIdentifiers.ValueProperty"/> change
        /// event so clients (e.g. Narrator) can read the current progress value on demand.
        /// Only raised in determinate mode; <see cref="ProgressRingAutomationPeer.GetPattern"/>
        /// already suppresses <see cref="PatternInterface.RangeValue"/> when indeterminate.
        /// </summary>
        /// <param name="oldValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        private void RaiseRangeValueChanged(double oldValue, double newValue)
        {
            if (!AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
            {
                return;
            }

            if ((UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this)) is ProgressRingAutomationPeer peer)
            {
                peer.RaiseValuePropertyChangedEvent(oldValue, newValue);
            }
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressRing ring = (ProgressRing)d;
            if (!ring.IsIndeterminate)
            {
                ring.RenderDeterminateArc(ring.AnimatedFraction);
            }
            else
            {
                ring.RenderIndeterminateArc();
            }
        }

        private void UpdateIndeterminateAnimationState()
        {
            bool isPausedVisual = IsPausedVisualState;
            if (!IsLoaded || !IsActive || !IsIndeterminate || isPausedVisual)
            {
                bool shouldRenderPausedFrame = IsLoaded && IsActive && IsIndeterminate && isPausedVisual;
                StopIndeterminateAnimation();
                if (shouldRenderPausedFrame)
                {
                    // Render the static paused half-arc at peak sweep with no rotation. The
                    // animation has already been stopped, parking the rotate transform at its
                    // start angle of 90 degrees.
                    IndeterminateSweepFraction = IndeterminatePeakSweepFraction;
                    RenderIndeterminateArc();
                }
            }
            else
            {
                StartIndeterminateAnimation();
            }
        }

        private void StartIndeterminateAnimation()
        {
            if (_indeterminateArcPath is null)
            {
                return;
            }
            if (_isIndeterminateAnimationRunning)
            {
                RenderIndeterminateArc();
                return;
            }
            _isIndeterminateAnimationRunning = true;
            IndeterminateSweepFraction = 0.0;
            BeginAnimation(IndeterminateSweepFractionProperty, CreateIndeterminateSweepAnimation());
            _indeterminateRotateTransform?.BeginAnimation(RotateTransform.AngleProperty, CreateIndeterminateRotationAnimation());
            RenderIndeterminateArc();
        }

        private void StopIndeterminateAnimation()
        {
            BeginAnimation(IndeterminateSweepFractionProperty, animation: null);
            _indeterminateRotateTransform?.BeginAnimation(RotateTransform.AngleProperty, animation: null);
            _isIndeterminateAnimationRunning = false;
            IndeterminateSweepFraction = 0.0;
            _ = _indeterminateRotateTransform?.Angle = IndeterminateRotationStartAngle;
            _ = _indeterminateArcPath?.Data = null;
        }

        private static DoubleAnimationUsingKeyFrames CreateIndeterminateSweepAnimation()
        {
            // Arc-length pulse: 0 -> 0.5 -> 0 of the circumference over a 2 second linear
            // cycle (keyframes at 0 s, 1 s, and 2 s of the 2 second duration).
            DoubleAnimationUsingKeyFrames animation = new()
            {
                Duration = IndeterminateAnimationDuration,
                RepeatBehavior = RepeatBehavior.Forever,
            };
            AddLinearKeyFrame(animation, 0.0, 0.0);
            AddLinearKeyFrame(animation, IndeterminatePeakSweepFraction, 0.5);
            AddLinearKeyFrame(animation, 0.0, 1.0);
            return animation;
        }

        private static DoubleAnimation CreateIndeterminateRotationAnimation()
        {
            // Linear rotation of the template transform from 90 to 1170 degrees (three full
            // turns) per 2 second cycle, matching the arc-length pulse cadence.
            return new()
            {
                From = IndeterminateRotationStartAngle,
                To = IndeterminateRotationEndAngle,
                Duration = IndeterminateAnimationDuration,
                RepeatBehavior = RepeatBehavior.Forever,
            };
        }

        private static void AddLinearKeyFrame(DoubleAnimationUsingKeyFrames animation, double value, double percent)
        {
            _ = animation.KeyFrames.Add(new LinearDoubleKeyFrame(value, KeyTime.FromPercent(percent)));
        }

        private double ComputeFraction()
        {
            double range = Maximum - Minimum;
            return range > 0 ? Math.Max(0, Math.Min(1, (Value - Minimum) / range)) : 0;
        }

        private void UpdateTemplateSettings()
        {
            double width = ActualWidth;
            if (double.IsNaN(width) || width <= 0)
            {
                width = Width;
            }
            if (double.IsNaN(width) || width <= 0)
            {
                return;
            }

            // diameter = (width × 0.1) + (1 if width ≤ 40 else 0).  Source:
            // microsoft-ui-xaml-main/src/controls/dev/ProgressRing/ProgressRing.cpp::ApplyTemplateSettings.
            double diameter = (width * 0.1) + (width <= 40.0 ? 1.0 : 0.0);
            double anchor = (width * 0.5) - diameter;
            SetValue(EllipseDiameterPropertyKey, diameter);
            SetValue(EllipseOffsetPropertyKey, new Thickness(0, anchor, 0, 0));
        }

        private void RenderIndeterminateArc()
        {
            if (_indeterminateArcPath is null)
            {
                return;
            }
            if (!IsActive || !IsIndeterminate)
            {
                _indeterminateArcPath.Data = null;
                return;
            }

            // The arc always starts at the top (angle 0); the template rotate transform
            // provides the spin, so only the sweep length changes here.
            RenderArc(_indeterminateArcPath, 0.0, IndeterminateSweepFraction * 360.0, deferForLayout: false, 0);
        }

        /// <summary>
        /// Gets whether the ring should currently present the paused visual (static half-arc):
        /// paused is requested and not overridden by the error state.
        /// </summary>
        private bool IsPausedVisualState => ShowPaused && !ShowError;

        private void UpdateArcStrokes()
        {
            // The local stroke set here intentionally outranks the ProgressState template
            // triggers, so the orthogonal error and paused flags work on their own. Clearing
            // the value restores the templated foreground, which is the accent brush by default.
            if (ShowError)
            {
                _indeterminateArcPath?.SetResourceReference(Shape.StrokeProperty, CriticalBrushKey);
                _arcPath?.SetResourceReference(Shape.StrokeProperty, CriticalBrushKey);
            }
            else if (ShowPaused)
            {
                _indeterminateArcPath?.SetResourceReference(Shape.StrokeProperty, CautionBrushKey);
                _arcPath?.SetResourceReference(Shape.StrokeProperty, CautionBrushKey);
            }
            else
            {
                _indeterminateArcPath?.ClearValue(Shape.StrokeProperty);
                _arcPath?.ClearValue(Shape.StrokeProperty);
            }
        }

        private void RenderDeterminateArc(double fraction)
        {
            if (_arcPath is null)
            {
                return;
            }

            // Defensive guard: if we've flipped to indeterminate while a tween is in flight,
            // the AnimatedFraction Completed callback can still arrive - drop it.
            if (IsIndeterminate)
            {
                _arcPath.Data = null;
                return;
            }
            if (fraction <= 0)
            {
                _arcPath.Data = null;
                return;
            }
            RenderArc(_arcPath, 0, fraction * 360.0, deferForLayout: true, fraction);
        }

        private void RenderArc(Path path, double startAngle, double sweepAngle, bool deferForLayout, double deferredFraction)
        {
            if (!TryGetArcMetrics(out double center, out double radius, out bool isLayoutSizeUnavailable))
            {
                path.Data = null;
                if (deferForLayout && isLayoutSizeUnavailable)
                {
                    // Defer determinate first render until the initial layout pass provides a size.
                    void handler(object? sender, EventArgs e)
                    {
                        LayoutUpdated -= handler;
                        if (!IsActive || IsIndeterminate)
                        {
                            return;
                        }
                        RenderDeterminateArc(deferredFraction);
                    }
                    LayoutUpdated += handler;
                }
                return;
            }

            double angle = Math.Max(0, Math.Min(sweepAngle, FullCircleLimit));
            if (angle <= 0)
            {
                path.Data = null;
                return;
            }

            // Select the pre-allocated geometry set for this path and mutate in place.
            PathFigure? figure;
            ArcSegment? segment;
            PathGeometry? geometry;
            if (ReferenceEquals(path, _indeterminateArcPath))
            {
                figure = _indeterminateFigure;
                segment = _indeterminateArcSegment;
                geometry = _indeterminateGeometry;
            }
            else
            {
                figure = _determinateFigure;
                segment = _determinateArcSegment;
                geometry = _determinateGeometry;
            }
            Point startPoint = GetArcPoint(center, radius, startAngle);
            Point endPoint = GetArcPoint(center, radius, startAngle + angle);
            _ = figure?.StartPoint = startPoint;
            if (segment is not null)
            {
                segment.Point = endPoint;
                segment.Size = new Size(radius, radius);
                segment.IsLargeArc = angle > 180;
            }

            if (!ReferenceEquals(path.Data, geometry))
            {
                path.Data = geometry;
            }
        }

        private bool TryGetArcMetrics(out double center, out double radius, out bool isLayoutSizeUnavailable)
        {
            double size = ActualWidth;
            if (double.IsNaN(size) || size <= 0)
            {
                size = Width;
            }

            if (double.IsNaN(size) || size <= 0)
            {
                center = 0;
                radius = 0;
                isLayoutSizeUnavailable = true;
                return false;
            }

            radius = (size - StrokeThickness) / 2.0;
            if (radius <= 0)
            {
                center = 0;
                isLayoutSizeUnavailable = false;
                return false;
            }
            center = size / 2.0;
            isLayoutSizeUnavailable = false;
            return true;
        }

        private static Point GetArcPoint(double center, double radius, double angle)
        {
            double angleRad = angle * Math.PI / 180.0;
            return new(center + (radius * Math.Sin(angleRad)), center - (radius * Math.Cos(angleRad)));
        }

        /// <summary>
        /// Represents the path geometry for the arc, or null if no arc is defined.
        /// </summary>
        private Path? _arcPath;

        /// <summary>
        /// Represents the path used to render the indeterminate arc, or null if the arc is not currently defined.
        /// </summary>
        /// <remarks>This field is typically used internally to manage the visual representation of an
        /// indeterminate progress indicator. It may be null when the arc is not visible or has not been
        /// initialized.</remarks>
        private Path? _indeterminateArcPath;

        /// <summary>
        /// Represents the template rotate transform that spins the indeterminate arc, or null when
        /// the current template does not provide <c>PART_IndeterminateRotate</c>.
        /// </summary>
        private RotateTransform? _indeterminateRotateTransform;

        /// <summary>
        /// Guards against reentrancy while <see cref="ProgressState"/> is being projected onto
        /// <see cref="ShowPaused"/> and <see cref="ShowError"/>.
        /// </summary>
        private bool _isSyncingProgressState;

        /// <summary>
        /// Indicates whether the indeterminate animation is currently running.
        /// </summary>
        private bool _isIndeterminateAnimationRunning;

        /// <summary>
        /// Represents the arc segment used to render the indeterminate progress indicator, or null if the segment is
        /// not defined.
        /// </summary>
        /// <remarks>This field is typically used internally to manage the visual state of an
        /// indeterminate progress control. It may be null if the arc segment has not been initialized or is not
        /// currently displayed.</remarks>
        private ArcSegment? _indeterminateArcSegment;

        /// <summary>
        /// Represents the path figure used to display the indeterminate state of a progress indicator or animation.
        /// </summary>
        /// <remarks>This field is typically used internally to cache or construct the visual
        /// representation for an indeterminate progress state. It may be null if the indeterminate state is not
        /// currently active or has not been initialized.</remarks>
        private PathFigure? _indeterminateFigure;

        /// <summary>
        /// Represents the geometry used to render the indeterminate state of a visual element.
        /// </summary>
        private PathGeometry? _indeterminateGeometry;

        /// <summary>
        /// Represents the arc segment used to render the determinate portion of the progress indicator.
        /// </summary>
        /// <remarks>This field is typically used internally to track the current state of the determinate
        /// arc in a progress visualization. It may be null if the determinate arc has not been initialized or is not
        /// currently displayed.</remarks>
        private ArcSegment? _determinateArcSegment;

        /// <summary>
        /// Represents the PathFigure used for rendering the determinate state of the progress indicator.
        /// </summary>
        /// <remarks>This field is intended for internal use to manage the visual representation of the
        /// determinate progress. It may be null if the determinate state is not currently displayed.</remarks>
        private PathFigure? _determinateFigure;

        /// <summary>
        /// Represents the geometry used to render the determinate state of the progress indicator.
        /// </summary>
        /// <remarks>This field holds the path geometry for the determinate visual representation. It is
        /// typically set or updated when the progress value changes.</remarks>
        private PathGeometry? _determinateGeometry;
    }
}
