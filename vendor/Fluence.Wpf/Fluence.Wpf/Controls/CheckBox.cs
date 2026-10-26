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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Fluent-styled check box with optional description and rounded indicator.
    /// </summary>
    [TemplatePart(Name = PART_CheckGlyph, Type = typeof(Path))]
    [TemplatePart(Name = PART_IndeterminateGlyph, Type = typeof(Path))]
    public class CheckBox : System.Windows.Controls.CheckBox
    {
        private const string PART_CheckGlyph = "PART_CheckGlyph";
        private const string PART_IndeterminateGlyph = "PART_IndeterminateGlyph";
        // AnimatedAcceptVisualSource.cpp, pinned WinUI Gallery source 7b127093: the
        // fixed 20-DIP path is 13.2548117114 DIP long with a 1.225-DIP round stroke.
        private const double CheckDashUnits = 13.2548117114 / 1.225;
        private const double StaticOrEraseMilliseconds = 1000.0 * 4 / 60;
        private const double DrawMilliseconds = 1000.0 * 19 / 60;
        private const double PressedDrawMilliseconds = 150;

        private enum IconState
        {
            NormalOff = 0,
            PointerOverOff = 1,
            PressedOff = 2,
            NormalOn = 3,
            PointerOverOn = 4,
            PressedOn = 5,
            NormalIndeterminate = 6,
            PointerOverIndeterminate = 7,
            PressedIndeterminate = 8,
        }

        private enum SegmentKind
        {
            Static = 0,
            Draw = 1,
            Erase = 2,
        }

        private static readonly DependencyProperty SegmentProgressProperty = DependencyProperty.Register(
            "SegmentProgress",
            typeof(double),
            typeof(CheckBox),
            new PropertyMetadata(0.0, OnSegmentProgressChanged));

        private readonly Queue<IconState> _queuedStates = new();
        private Path? _checkGlyph;
        private Path? _indeterminateGlyph;
        private DispatcherOperation? _pendingStateUpdate;
        private IconState _currentState;
        private SegmentKind _segmentKind;
        private int _segmentGeneration;
        private bool _segmentActive;
        private readonly bool _initialized;

        /// <summary>
        /// Initializes static members of the CheckBox class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the CheckBox control uses its own style by
        /// default, rather than inheriting the style from its base class. This is important for custom control theming
        /// and styling in WPF applications.</remarks>
        static CheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CheckBox),
                new FrameworkPropertyMetadata(typeof(CheckBox)));
        }

        /// <summary>
        /// Initializes the template-part motion lifetime.
        /// </summary>
        public CheckBox()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _initialized = true;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            StopMotion();
            base.OnApplyTemplate();
            _checkGlyph = GetTemplateChild(PART_CheckGlyph) as Path;
            _indeterminateGlyph = GetTemplateChild(PART_IndeterminateGlyph) as Path;
            SnapToResolvedState();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (!_initialized || (e.Property != IsCheckedProperty && e.Property != IsPressedProperty
                && e.Property != IsMouseOverProperty && e.Property != IsEnabledProperty))
            {
                return;
            }

            if (!IsLoaded || !MotionHelper.IsMotionEnabled)
            {
                StopMotion();
                SnapToResolvedState();
                return;
            }

            // AnimatedIcon coalesces all changes before the next layout pass to the
            // latest combined state, then queues later states while a route is active.
            _pendingStateUpdate ??= Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                new Action(CommitPendingState));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            StopMotion();
            SnapToResolvedState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopMotion();
        }

        private void CommitPendingState()
        {
            _pendingStateUpdate = null;
            if (!IsLoaded || !MotionHelper.IsMotionEnabled)
            {
                StopMotion();
                SnapToResolvedState();
                return;
            }

            IconState target = ResolveState();
            if (_segmentActive)
            {
                IconState lastTarget = _currentState;
                foreach (IconState queued in _queuedStates)
                {
                    lastTarget = queued;
                }

                // Raw WPF property notifications can leave the combined marker unchanged
                // (for example hover changing during a press). Upstream only queues a new
                // AnimatedIcon.State value after a CombinedState change.
                if (lastTarget == target)
                {
                    return;
                }

                if (_queuedStates.Count is 4)
                {
                    // QueueOne's bounded FIFO advances to the oldest queued state
                    // immediately on overflow. Put the new target behind it first
                    // so an immediate missing-marker fallback cannot strand it.
                    IconState oldest = _queuedStates.Dequeue();
                    _queuedStates.Enqueue(target);
                    ClearSegmentClock();
                    StartSegment(oldest);
                    return;
                }

                _queuedStates.Enqueue(target);
            }
            else if (_currentState != target)
            {
                StartSegment(target);
            }
        }

        private IconState ResolveState()
        {
            int common = !IsEnabled ? 0 : IsPressed ? 2 : IsMouseOver ? 1 : 0;
            int selection = IsChecked is null ? 6 : IsChecked is true ? 3 : 0;
            return (IconState)(selection + common);
        }

        private void StartSegment(IconState target)
        {
            IconState from = _currentState;
            _currentState = target; // WinUI advances its logical marker at segment start.
            if (!TryGetRoute(from, target, out SegmentKind kind, out double duration))
            {
                // AnimatedIcon's missing-marker fallback snaps. Drain queued targets
                // here as a narrow robustness correction to its stale-queue edge.
                SnapGlyphs(target);
                StartNextQueuedSegment();
                return;
            }

            _segmentKind = kind;
            _segmentActive = true;
            SetValue(SegmentProgressProperty, 0.0);
            ApplySegmentProgress(0.0);
            int generation = ++_segmentGeneration;
            DoubleAnimationUsingKeyFrames animation = new() { FillBehavior = FillBehavior.Stop };
            _ = animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            KeySpline spline = kind is SegmentKind.Draw
                ? new KeySpline(0.55, 0, 0, 1)
                : new KeySpline(0.167, 0.167, 0.833, 0.833);
            _ = animation.KeyFrames.Add(new SplineDoubleKeyFrame(1.0,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration)), spline));
            animation.Completed += delegate
            {
                if (generation != _segmentGeneration || !_segmentActive)
                {
                    return;
                }

                _segmentActive = false;
                BeginAnimation(SegmentProgressProperty, animation: null);
                SnapGlyphs(_currentState);
                StartNextQueuedSegment();
            };
            BeginAnimation(SegmentProgressProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private void StartNextQueuedSegment()
        {
            while (!_segmentActive && _queuedStates.Count > 0)
            {
                StartSegment(_queuedStates.Dequeue());
            }
        }

        private static bool TryGetRoute(IconState from, IconState to, out SegmentKind kind, out double duration)
        {
            kind = SegmentKind.Static;
            duration = StaticOrEraseMilliseconds;
            switch (from, to)
            {
                case (IconState.NormalOff, IconState.NormalOn):
                case (IconState.PointerOverOff, IconState.PointerOverOn):
                case (IconState.PressedOff, IconState.PointerOverOn):
                case (IconState.PressedOff, IconState.NormalOn):
                    kind = SegmentKind.Draw;
                    duration = DrawMilliseconds;
                    return true;
                case (IconState.PressedOff, IconState.PressedOn):
                    kind = SegmentKind.Draw;
                    duration = PressedDrawMilliseconds;
                    return true;
                case (IconState.NormalOn, IconState.NormalOff):
                case (IconState.PointerOverOn, IconState.PointerOverOff):
                case (IconState.PressedOn, IconState.PressedOff):
                case (IconState.PressedOn, IconState.PointerOverOff):
                case (IconState.PressedOn, IconState.NormalOff):
                    kind = SegmentKind.Erase;
                    return true;
                case (IconState.NormalOn, IconState.PointerOverOn):
                case (IconState.NormalOn, IconState.PressedOn):
                case (IconState.PointerOverOn, IconState.NormalOn):
                case (IconState.PointerOverOn, IconState.PressedOn):
                case (IconState.NormalOff, IconState.PointerOverOff):
                case (IconState.NormalOff, IconState.PressedOff):
                case (IconState.PointerOverOff, IconState.NormalOff):
                case (IconState.PointerOverOff, IconState.PressedOff):
                    return true;
                default:
                    return false;
            }
        }

        private static void OnSegmentProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)d;
            if (checkBox._segmentActive)
            {
                checkBox.ApplySegmentProgress((double)e.NewValue);
            }
        }

        private void ApplySegmentProgress(double progress)
        {
            if (_checkGlyph is null)
            {
                return;
            }

            if (_segmentKind is SegmentKind.Draw)
            {
                _checkGlyph.Opacity = progress <= 0 ? 0 : 1;
                _checkGlyph.StrokeDashOffset = CheckDashUnits * (1 - progress);
            }
            else if (_segmentKind is SegmentKind.Erase)
            {
                _checkGlyph.Opacity = progress >= 1 ? 0 : 1;
                _checkGlyph.StrokeDashOffset = -CheckDashUnits * progress;
            }
            else
            {
                SnapGlyphs(_currentState);
            }

            _indeterminateGlyph?.SetCurrentValue(OpacityProperty, 0.0);
        }

        private void SnapGlyphs(IconState state)
        {
            _checkGlyph?.SetCurrentValue(Shape.StrokeDashOffsetProperty, 0.0);
            _checkGlyph?.SetCurrentValue(OpacityProperty,
                state is IconState.NormalOn or IconState.PointerOverOn or IconState.PressedOn ? 1.0 : 0.0);
            _indeterminateGlyph?.SetCurrentValue(OpacityProperty,
                state is IconState.NormalIndeterminate or IconState.PointerOverIndeterminate or IconState.PressedIndeterminate ? 1.0 : 0.0);
        }

        private void SnapToResolvedState()
        {
            _currentState = ResolveState();
            SnapGlyphs(_currentState);
        }

        private void ClearSegmentClock()
        {
            _segmentGeneration++;
            _segmentActive = false;
            BeginAnimation(SegmentProgressProperty, animation: null);
        }

        private void StopMotion()
        {
            if (_pendingStateUpdate is { } pending)
            {
                _ = pending.Abort();
                _pendingStateUpdate = null;
            }

            ClearSegmentClock();
            _queuedStates.Clear();
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(CheckBox),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the check box indicator.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(CheckBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: OnDescriptionChanged));

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CheckBox checkBox)
            {
                string? description = e.NewValue as string;
                AutomationProperties.SetHelpText(
                    checkBox,
                    string.IsNullOrWhiteSpace(description) ? string.Empty : description);
            }
        }

        /// <summary>
        /// Gets or sets the description text displayed below the check box content.
        /// </summary>
        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
    }
}
