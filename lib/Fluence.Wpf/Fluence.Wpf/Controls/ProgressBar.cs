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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Fluent-styled progress bar with determinate, indeterminate, error, and paused states plus an
    /// optional step mode.
    /// </summary>
    [TemplatePart(Name = IndicatorHostName, Type = typeof(System.Windows.Controls.Grid))]
    [TemplatePart(Name = PART_Track, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_Fill, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateBar, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateBar2, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateTranslate, Type = typeof(TranslateTransform))]
    [TemplatePart(Name = PART_IndeterminateTranslate2, Type = typeof(TranslateTransform))]
    public class ProgressBar : System.Windows.Controls.ProgressBar
    {
        // Template part names for internal elements of the control template.
        private const string IndicatorHostName = "ProgressBarIndicatorHost";
        private const string PART_Track = "PART_Track";
        private const string PART_Fill = "PART_Fill";
        private const string PART_IndeterminateBar = "PART_IndeterminateBar";
        private const string PART_IndeterminateBar2 = "PART_IndeterminateBar2";
        private const string PART_IndeterminateTranslate = "PART_IndeterminateTranslate";
        private const string PART_IndeterminateTranslate2 = "PART_IndeterminateTranslate2";

        /// <summary>
        /// Initializes static members of the ProgressBar class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ProgressBar control uses its custom style by
        /// default. It is called automatically before any static members are accessed or any instances are
        /// created.</remarks>
        static ProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(typeof(ProgressBar)));
            IsIndeterminateProperty.OverrideMetadata(
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(false, OnIsIndeterminateChanged));
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(new CornerRadius(1.5), OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the corner radius of the progress indicator (the determinate fill and the
        /// indeterminate bars), matching the WinUI 3 default of 1.5.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressModeProperty =
            DependencyProperty.Register(
                nameof(ProgressMode),
                typeof(ProgressBarMode),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(ProgressBarMode.Standard, OnProgressModeChanged));

        /// <summary>
        /// Gets or sets the progress mode. This is a one-way backward-compatibility alias that maps onto
        /// the WinUI-orthogonal primitives <see cref="System.Windows.Controls.ProgressBar.IsIndeterminate"/>,
        /// <see cref="ShowError"/>, and <see cref="ShowPaused"/>; changing those primitives does not write
        /// back to this property.
        /// </summary>
        public ProgressBarMode ProgressMode
        {
            get => (ProgressBarMode)GetValue(ProgressModeProperty);
            set => SetValue(ProgressModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowError"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowErrorProperty =
            DependencyProperty.Register(
                nameof(ShowError),
                typeof(bool),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(false, OnStatePrimitiveChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the progress bar reports an error state. When set, the
        /// determinate fill and the indeterminate bars use the critical system brush, matching the WinUI 3
        /// ProgressBar.ShowError contract. Takes precedence over <see cref="ShowPaused"/>.
        /// </summary>
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
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(false, OnStatePrimitiveChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the progress bar reports a paused state. When set (and
        /// <see cref="ShowError"/> is not), the determinate fill and the indeterminate bars use the caution
        /// system brush, matching the WinUI 3 ProgressBar.ShowPaused contract.
        /// </summary>
        public bool ShowPaused
        {
            get => (bool)GetValue(ShowPausedProperty);
            set => SetValue(ShowPausedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Steps"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register(
                nameof(Steps),
                typeof(int),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(0, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the total number of steps in StepProgress mode.
        /// </summary>
        public int Steps
        {
            get => (int)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CurrentStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentStepProperty =
            DependencyProperty.Register(
                nameof(CurrentStep),
                typeof(int),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(0, OnAnimatedLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the current step in StepProgress mode.
        /// </summary>
        public int CurrentStep
        {
            get => (int)GetValue(CurrentStepProperty);
            set => SetValue(CurrentStepProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStepMarkers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStepMarkersProperty =
            DependencyProperty.Register(
                nameof(ShowStepMarkers),
                typeof(bool),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(true, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets whether step markers are shown in StepProgress mode.
        /// </summary>
        public bool ShowStepMarkers
        {
            get => (bool)GetValue(ShowStepMarkersProperty);
            set => SetValue(ShowStepMarkersProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TrackHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackHeightProperty =
            DependencyProperty.Register(
                nameof(TrackHeight),
                typeof(double),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(1.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the height of the thin baseline track behind the progress indicator,
        /// matching the WinUI 3 default of 1.
        /// </summary>
        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class and subscribes to size changes for layout updates.
        /// Loaded and Unloaded are also wired so the repeat-forever indeterminate animation only
        /// runs while the control is in a live visual tree, mirroring <see cref="ProgressRing"/>.
        /// </summary>
        public ProgressBar()
        {
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            StopIndeterminate();
            _track?.SizeChanged -= OnSizeChanged;
            _indicatorHost?.SizeChanged -= OnSizeChanged;
            _track = GetTemplateChild(PART_Track) as System.Windows.Controls.Border;
            _indicatorHost = GetTemplateChild(IndicatorHostName) as System.Windows.Controls.Grid;
            _fill = GetTemplateChild(PART_Fill) as System.Windows.Controls.Border;
            _indeterminateBar = GetTemplateChild(PART_IndeterminateBar) as System.Windows.Controls.Border;
            _indeterminateBar2 = GetTemplateChild(PART_IndeterminateBar2) as System.Windows.Controls.Border;
            _indeterminateTranslate = GetTemplateChild(PART_IndeterminateTranslate) as TranslateTransform;
            _indeterminateTranslate2 = GetTemplateChild(PART_IndeterminateTranslate2) as TranslateTransform;
            _track?.SizeChanged += OnSizeChanged;
            _indicatorHost?.SizeChanged += OnSizeChanged;
            ApplyProgressMode();
            UpdateFillWidth(false);
            RefreshIndeterminateLayout();
            UpdateIndicatorHostClip();
        }

        /// <inheritdoc />
        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            UpdateFillWidth();
        }

        /// <inheritdoc />
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            base.OnMinimumChanged(oldMinimum, newMinimum);
            UpdateFillWidth(false);
        }

        /// <inheritdoc />
        protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            base.OnMaximumChanged(oldMaximum, newMaximum);
            UpdateFillWidth(false);
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            bar.UpdateFillWidth(false);
            bar.RefreshIndeterminateLayout();
            bar.UpdateIndicatorHostClip();
        }

        private static void OnAnimatedLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            bar.UpdateFillWidth();
            bar.RefreshIndeterminateLayout();
        }

        private static void OnProgressModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            bar.SyncPrimitivesFromMode((ProgressBarMode)e.NewValue);
        }

        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            if (bar._syncingMode)
            {
                return;
            }
            bar.ApplyProgressMode();
            bar.UpdateFillWidth(false);
        }

        private static void OnStatePrimitiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            if (bar._syncingMode)
            {
                return;
            }
            bar.ApplyProgressMode();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFillWidth(_stepMode);
            RefreshIndeterminateLayout();
            UpdateIndicatorHostClip();
        }

        /// <summary>
        /// Re-applies the resolved visual state when the control (re)enters a live visual tree,
        /// restarting the indeterminate animation that <see cref="OnUnloaded"/> stopped.
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyProgressMode();
        }

        /// <summary>
        /// Stops the repeat-forever indeterminate animation when the control leaves the visual
        /// tree so closed windows do not leak rooted animation clocks.
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopIndeterminate();
        }

        /// <summary>
        /// Clips the indicator host (and therefore the determinate fill and both indeterminate bars) to its
        /// rounded-rectangle geometry. WPF's <see cref="UIElement.ClipToBounds"/> clips only to the
        /// rectangular bounds, so the translating indeterminate bars (which overshoot both edges of the
        /// control) would otherwise show square ends where they cross the edge. A geometry clip matching
        /// <see cref="CornerRadius"/> keeps the accent conforming to the rounded indicator on every animation
        /// frame, exactly as the left-anchored determinate fill already does.
        /// </summary>
        private void UpdateIndicatorHostClip()
        {
            if (_indicatorHost is null)
            {
                return;
            }
            double width = _indicatorHost.ActualWidth;
            double height = _indicatorHost.ActualHeight;
            if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height))
            {
                _indicatorHost.Clip = null;
                return;
            }
            double radius = CornerRadius.TopLeft;
            _indicatorHost.Clip = new RectangleGeometry(new Rect(0, 0, width, height), radius, radius);
        }

        /// <summary>
        /// Maps the legacy <see cref="ProgressMode"/> alias onto the WinUI-orthogonal primitives
        /// (<see cref="System.Windows.Controls.ProgressBar.IsIndeterminate"/>, <see cref="ShowError"/>,
        /// <see cref="ShowPaused"/>, and the internal step flag). The mapping is one-way: changing the
        /// primitives never writes back to <see cref="ProgressMode"/>, and <see cref="_syncingMode"/>
        /// suppresses the primitive callbacks so the resolver runs exactly once per mode change.
        /// </summary>
        private void SyncPrimitivesFromMode(ProgressBarMode mode)
        {
            if (_syncingMode)
            {
                return;
            }
            _syncingMode = true;
            try
            {
                IsIndeterminate = mode == ProgressBarMode.Indeterminate;
                ShowError = mode == ProgressBarMode.Error;
                ShowPaused = mode == ProgressBarMode.Paused;
                _stepMode = mode == ProgressBarMode.StepProgress;
            }
            finally
            {
                _syncingMode = false;
            }
            ApplyProgressMode();
        }

        /// <summary>
        /// Resolves the visual state from the three primitives
        /// (<see cref="System.Windows.Controls.ProgressBar.IsIndeterminate"/>, <see cref="ShowError"/>,
        /// <see cref="ShowPaused"/>): toggles determinate-versus-indeterminate element visibility, starts or
        /// stops the indeterminate animation, hides the baseline track while indeterminate, and applies the
        /// matching state brush to the fill and both indeterminate bars.
        /// </summary>
        private void ApplyProgressMode()
        {
            if (_fill is null || _indeterminateBar is null)
            {
                return;
            }
            ApplyStateBrush();
            if (!IsIndeterminate)
            {
                // PART_Track can report 0 width during template application. Queue one
                // layout-priority update so determinate fills render correctly after the
                // first measure/arrange pass.
                StopIndeterminate();
                _ = _track?.Opacity = 1.0;
                _fill.Visibility = Visibility.Visible;
                _indeterminateBar.Visibility = Visibility.Collapsed;
                _ = _indeterminateBar2?.Visibility = Visibility.Collapsed;
                _ = Dispatcher.BeginInvoke(() => UpdateFillWidth(false), DispatcherPriority.Loaded);
            }
            else
            {
                _ = _track?.Opacity = 0.0;
                _fill.Visibility = Visibility.Collapsed;
                _indeterminateBar.Visibility = Visibility.Visible;
                _ = _indeterminateBar2?.Visibility = Visibility.Visible;
                RefreshIndeterminateLayout();
            }
        }

        /// <summary>
        /// Applies the state brush to the determinate fill and both indeterminate bars: critical when
        /// <see cref="ShowError"/> is set, caution when <see cref="ShowPaused"/> is set, otherwise the
        /// default accent fill.
        /// </summary>
        private void ApplyStateBrush()
        {
            string brushKey = ShowError
                ? "SystemFillColorCriticalBrush"
                : ShowPaused ? "SystemFillColorCautionBrush" : "AccentFillColorDefaultBrush";
            _fill?.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, brushKey);
            _indeterminateBar?.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, brushKey);
            _indeterminateBar2?.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, brushKey);
        }

        private void RefreshIndeterminateLayout()
        {
            if (_track is null || _indeterminateBar is null || !IsIndeterminate)
            {
                return;
            }
            double trackWidth = _track.ActualWidth;
            if (trackWidth <= 0 || double.IsNaN(trackWidth))
            {
                return;
            }
            _indeterminateBar.Width = trackWidth * 0.4;
            _ = _indeterminateBar2?.Width = trackWidth * 0.6;
            StartIndeterminate(trackWidth);
        }

        private void StartIndeterminate(double trackWidth)
        {
            StopIndeterminate();

            // Only animate while loaded: the repeat-forever clocks would otherwise stay rooted
            // after the hosting window closes. OnLoaded restarts the animation on re-entry.
            if (!IsLoaded || _indeterminateTranslate is null || _indeterminateBar is null)
            {
                return;
            }

            // WinUI 3 canonical indeterminate storyboard, recomputed from the live track width.
            // Bar one spans 40 percent of the track. It begins just off the left edge and eases
            // across to roughly 120 percent by 1.5 s on KeySpline 0.4 0.0 0.6 1.0, then holds.
            // Bar two spans 60 percent. It waits off the left edge until 0.75 s, then eases to
            // about 100 percent by 2.0 s on the same spline. Both repeat forever on a 2.0 s cycle.
            // Authority: WinUI ProgressBar.xaml indeterminate visual state.
            DoubleAnimationUsingKeyFrames bar1Animation = new()
            {
                Duration = new Duration(TimeSpan.FromSeconds(2.0)),
                RepeatBehavior = RepeatBehavior.Forever
            };
            _ = bar1Animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(trackWidth * -0.4, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            _ = bar1Animation.KeyFrames.Add(new SplineDoubleKeyFrame(trackWidth * 1.2, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5)), new KeySpline(0.4, 0.0, 0.6, 1.0)));
            _ = bar1Animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(trackWidth * 1.2, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0))));
            _indeterminateTranslate.X = trackWidth * -0.4;
            _indeterminateTranslate.BeginAnimation(TranslateTransform.XProperty, bar1Animation, HandoffBehavior.SnapshotAndReplace);

            if (_indeterminateTranslate2 is not null && _indeterminateBar2 is not null)
            {
                DoubleAnimationUsingKeyFrames bar2Animation = new()
                {
                    Duration = new Duration(TimeSpan.FromSeconds(2.0)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                _ = bar2Animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(trackWidth * -0.9, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                _ = bar2Animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(trackWidth * -0.9, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.75))));
                _ = bar2Animation.KeyFrames.Add(new SplineDoubleKeyFrame(trackWidth * 0.996, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0)), new KeySpline(0.4, 0.0, 0.6, 1.0)));
                _indeterminateTranslate2.X = trackWidth * -0.9;
                _indeterminateTranslate2.BeginAnimation(TranslateTransform.XProperty, bar2Animation, HandoffBehavior.SnapshotAndReplace);
            }
        }

        private void StopIndeterminate()
        {
            if (_indeterminateTranslate is not null)
            {
                _indeterminateTranslate.BeginAnimation(TranslateTransform.XProperty, null);
                _indeterminateTranslate.X = 0;
            }
            if (_indeterminateTranslate2 is not null)
            {
                _indeterminateTranslate2.BeginAnimation(TranslateTransform.XProperty, null);
                _indeterminateTranslate2.X = 0;
            }
        }

        private void UpdateFillWidth(bool animate = true)
        {
            if (_track is null || _fill is null || IsIndeterminate)
            {
                return;
            }

            double trackWidth = _track.ActualWidth;
            if (trackWidth <= 0 || double.IsNaN(trackWidth))
            {
                return;
            }

            double ratio;
            if (!_stepMode || Steps <= 0)
            {
                double min = Minimum; double max = Maximum;
                if (Math.Abs(max - min) >= double.Epsilon)
                {
                    ratio = (Value - min) / (max - min);
                    ratio = Math.Max(0, Math.Min(1, ratio));
                }
                else
                {
                    ratio = 0;
                }
            }
            else
            {
                int step = Math.Max(0, Math.Min(CurrentStep, Steps));
                ratio = step / (double)Steps;
            }

            double targetWidth = trackWidth * ratio;
            if (!animate)
            {
                _fillAnimationVersion++;
                _fill.BeginAnimation(WidthProperty, null);
                _fill.Width = targetWidth;
                return;
            }

            double fromWidth = _fill.Width;
            if (double.IsNaN(fromWidth) || fromWidth < 0)
            {
                fromWidth = _fill.ActualWidth;
            }

            if (double.IsNaN(fromWidth) || fromWidth < 0)
            {
                fromWidth = 0;
            }

            if (Math.Abs(fromWidth - targetWidth) < 0.1)
            {
                _fillAnimationVersion++;
                _fill.BeginAnimation(WidthProperty, null);
                _fill.Width = targetWidth;
                return;
            }

            _fillAnimationVersion++;
            int animationVersion = _fillAnimationVersion;
            _fill.BeginAnimation(WidthProperty, null);
            _fill.Width = fromWidth;

            // WinUI RepositionThemeAnimation approximation: a single 367 ms spline keyframe with
            // KeySpline (0.1,0.9 0.2,1.0) (fast start, long settle), interpolating from the committed
            // base width set just above.
            DoubleAnimationUsingKeyFrames animation = new()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(367)),
                FillBehavior = FillBehavior.Stop
            };
            _ = animation.KeyFrames.Add(new SplineDoubleKeyFrame(targetWidth, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(367)), new KeySpline(0.1, 0.9, 0.2, 1.0)));
            animation.Completed += delegate
            {
                if (animationVersion == _fillAnimationVersion && _fill is not null)
                {
                    _fill.BeginAnimation(WidthProperty, null);
                    _fill.Width = targetWidth;
                }
            };
            _fill.BeginAnimation(WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Represents the track element of the control, typically used to display the background or progress area.
        /// </summary>
        /// <remarks>This field is intended for internal use within the control's implementation and is
        /// not intended to be accessed directly by consumers of the API.</remarks>
        private System.Windows.Controls.Border? _track;

        /// <summary>
        /// Represents the clipped host panel that contains the determinate fill and both indeterminate bars.
        /// </summary>
        private System.Windows.Controls.Grid? _indicatorHost;

        /// <summary>
        /// Represents the fill border element used within the control.
        /// </summary>
        private System.Windows.Controls.Border? _fill;

        /// <summary>
        /// Represents the border control used to display the indeterminate progress bar.
        /// </summary>
        private System.Windows.Controls.Border? _indeterminateBar;

        /// <summary>
        /// Represents the secondary indeterminate progress bar control.
        /// </summary>
        private System.Windows.Controls.Border? _indeterminateBar2;

        /// <summary>
        /// Represents the translate transform used for indeterminate animation states.
        /// </summary>
        private TranslateTransform? _indeterminateTranslate;

        /// <summary>
        /// Represents the secondary translate transform used for indeterminate animation states.
        /// </summary>
        private TranslateTransform? _indeterminateTranslate2;

        /// <summary>
        /// Tracks the active determinate fill animation so replaced clocks cannot commit stale widths.
        /// </summary>
        private int _fillAnimationVersion;

        /// <summary>
        /// Guards the one-way <see cref="ProgressMode"/> alias sync so the suppressed primitive callbacks
        /// do not re-run the state resolver while the alias is writing the primitives.
        /// </summary>
        private bool _syncingMode;

        /// <summary>
        /// Tracks whether the legacy StepProgress mode drives the determinate fill ratio.
        /// </summary>
        private bool _stepMode;
    }
}
