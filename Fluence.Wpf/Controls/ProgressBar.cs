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
    /// Fluent-styled progress bar with standard, indeterminate, and step modes.
    /// </summary>
    [TemplatePart(Name = PART_Track, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_Fill, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateBar, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateBar2, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateTranslate, Type = typeof(TranslateTransform))]
    [TemplatePart(Name = PART_IndeterminateTranslate2, Type = typeof(TranslateTransform))]
    public class ProgressBar : System.Windows.Controls.ProgressBar
    {
        // Template part names for internal elements of the control template.
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
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(new CornerRadius(2), OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the corner radius of the progress bar track and fill.
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
        /// Gets or sets the progress mode (Standard, Indeterminate, or StepProgress).
        /// </summary>
        public ProgressBarMode ProgressMode
        {
            get => (ProgressBarMode)GetValue(ProgressModeProperty);
            set => SetValue(ProgressModeProperty, value);
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
                new FrameworkPropertyMetadata(4.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the height of the progress bar track.
        /// </summary>
        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class and subscribes to size changes for layout updates.
        /// </summary>
        public ProgressBar()
        {
            SizeChanged += OnSizeChanged;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            StopIndeterminate();
            _track?.SizeChanged -= OnSizeChanged;
            _track = GetTemplateChild(PART_Track) as System.Windows.Controls.Border;
            _fill = GetTemplateChild(PART_Fill) as System.Windows.Controls.Border;
            _indeterminateBar = GetTemplateChild(PART_IndeterminateBar) as System.Windows.Controls.Border;
            _indeterminateBar2 = GetTemplateChild(PART_IndeterminateBar2) as System.Windows.Controls.Border;
            _indeterminateTranslate = GetTemplateChild(PART_IndeterminateTranslate) as TranslateTransform;
            _indeterminateTranslate2 = GetTemplateChild(PART_IndeterminateTranslate2) as TranslateTransform;
            _track?.SizeChanged += OnSizeChanged;
            ApplyProgressMode();
            UpdateFillWidth(false);
            RefreshIndeterminateLayout();
            UpdateTrackClip();
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
            bar.ApplyProgressMode();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFillWidth(ProgressMode == ProgressBarMode.StepProgress);
            RefreshIndeterminateLayout();
            UpdateTrackClip();
        }

        /// <summary>
        /// Clips the track (and therefore every fill / indeterminate child) to its rounded-rectangle
        /// geometry. WPF's <see cref="UIElement.ClipToBounds"/> clips only to the rectangular bounds, so
        /// the translating indeterminate bars (which overshoot both track edges) would otherwise show
        /// square ends where they cross the track edge. A geometry clip matching <see cref="CornerRadius"/>
        /// keeps the accent conforming to the rounded track on every animation frame, exactly as the
        /// left-anchored determinate fill already does.
        /// </summary>
        private void UpdateTrackClip()
        {
            if (_track is null)
            {
                return;
            }
            double width = _track.ActualWidth;
            double height = _track.ActualHeight;
            if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height))
            {
                _track.Clip = null;
                return;
            }
            double radius = CornerRadius.TopLeft;
            _track.Clip = new RectangleGeometry(new Rect(0, 0, width, height), radius, radius);
        }

        private void ApplyProgressMode()
        {
            if (_fill is null || _indeterminateBar is null)
            {
                return;
            }
            if (ProgressMode != ProgressBarMode.Indeterminate)
            {
                // PART_Track can report 0 width during template application. Queue one
                // layout-priority update so determinate fills render correctly after the
                // first measure/arrange pass.
                StopIndeterminate();
                _fill.Visibility = Visibility.Visible;
                _indeterminateBar.Visibility = Visibility.Collapsed;
                _ = _indeterminateBar2?.Visibility = Visibility.Collapsed;
                ApplyFillBrushForMode();
                _ = Dispatcher.BeginInvoke(() => UpdateFillWidth(false), DispatcherPriority.Loaded);
            }
            else
            {
                _fill.Visibility = Visibility.Collapsed;
                _indeterminateBar.Visibility = Visibility.Visible;
                _ = _indeterminateBar2?.Visibility = Visibility.Visible;
                RefreshIndeterminateLayout();
            }
        }

        private void ApplyFillBrushForMode()
        {
            if (_fill is null)
            {
                return;
            }
            switch (ProgressMode)
            {
                case ProgressBarMode.Error:
                    _fill.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SystemFillColorCriticalBrush");
                    break;
                case ProgressBarMode.Paused:
                    _fill.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SystemFillColorCautionBrush");
                    break;
                case ProgressBarMode.Standard:
                case ProgressBarMode.StepProgress:
                case ProgressBarMode.Indeterminate:
                    _fill.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "AccentFillColorDefaultBrush");
                    break;
                default:
                    _fill.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "AccentFillColorDefaultBrush");
                    break;
            }
        }

        private void RefreshIndeterminateLayout()
        {
            if (_track is null || _indeterminateBar is null || ProgressMode != ProgressBarMode.Indeterminate)
            {
                return;
            }
            double trackWidth = _track.ActualWidth;
            if (trackWidth <= 0 || double.IsNaN(trackWidth))
            {
                return;
            }
            _indeterminateBar.Width = trackWidth * 0.4;
            _ = _indeterminateBar2?.Width = trackWidth * 0.55;
            StartIndeterminate(trackWidth);
        }

        private void StartIndeterminate(double trackWidth)
        {
            StopIndeterminate();
            if (_indeterminateTranslate is null || _indeterminateBar is null)
            {
                return;
            }

            // WinUI 3 canonical timing: both bars on a 2.0 s repeat cycle.
            // Bar 1 travels 0 to 1.5 s then holds; bar 2 is delayed 0.75 s.
            // (Authority: WinUI_XAML/Controls/ProgressBar.xaml Indeterminate VSM)
            StartTranslateAnimation(_indeterminateTranslate, -_indeterminateBar.Width, trackWidth, TimeSpan.FromSeconds(2.0), TimeSpan.Zero);
            if (_indeterminateTranslate2 is not null && _indeterminateBar2 is not null)
            {
                StartTranslateAnimation(_indeterminateTranslate2, -_indeterminateBar2.Width, trackWidth, TimeSpan.FromSeconds(2.0), TimeSpan.FromMilliseconds(750));
            }
        }

        private static void StartTranslateAnimation(TranslateTransform target, double from, double to, TimeSpan duration, TimeSpan beginTime)
        {
            target.X = from; target.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                BeginTime = beginTime,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            }, HandoffBehavior.SnapshotAndReplace);
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
            if (_track is null || _fill is null || ProgressMode == ProgressBarMode.Indeterminate)
            {
                return;
            }

            double trackWidth = _track.ActualWidth;
            if (trackWidth <= 0 || double.IsNaN(trackWidth))
            {
                return;
            }

            double ratio;
            if (ProgressMode != ProgressBarMode.StepProgress || Steps <= 0)
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
            DoubleAnimation animation = new()
            {
                From = fromWidth,
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(280),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
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
    }
}
