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
using System.Windows.Threading;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Provides the attached properties that drive the WinUI style scrolling indicator on the
    /// <see cref="ScrollBar"/> instances inside a Fluence scroll viewer template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WinUI 3 gives its ScrollViewer template a <c language="xaml">ScrollingIndicatorStates</c> visual
    /// state group and pushes an <c language="xaml">IndicatorMode</c> value down into both scroll bars, so
    /// a bar stays hidden until the pointer enters the scroller or the content actually scrolls, then
    /// fades out again after a delay. WPF has no equivalent framework plumbing:
    /// <see cref="ScrollBar"/> carries no indicator property and nothing calls
    /// <see cref="VisualStateManager.GoToState"/> for those states. This class supplies the missing driver.
    /// </para>
    /// <para>
    /// The keyed Fluence scroll bar styles set <see cref="IsIndicatorEnabledProperty"/>, which attaches a
    /// per instance behavior. The behavior watches the templated parent scroll viewer and the bar's own
    /// pointer state, writes <see cref="IndicatorModeProperty"/>, and moves both the
    /// <c language="xaml">ScrollingIndicatorStates</c> and <c language="xaml">ConsciousStates</c> groups.
    /// A bar that is not hosted in a <see cref="ScrollViewer"/> has no auto hide source, so it stays on
    /// <see cref="ScrollingIndicatorMode.MouseIndicator"/> and remains visible.
    /// </para>
    /// </remarks>
    public static class ScrollBarExtensions
    {
        /// <summary>
        /// Name of the ScrollingIndicatorStates state that hides the bar entirely.
        /// </summary>
        private const string NoIndicatorState = "NoIndicator";

        /// <summary>
        /// Name of the ScrollingIndicatorStates state that shows the interactive rail.
        /// </summary>
        private const string MouseIndicatorState = "MouseIndicator";

        /// <summary>
        /// Name of the ScrollingIndicatorStates state that shows the touch panning bar.
        /// </summary>
        private const string TouchIndicatorState = "TouchIndicator";

        /// <summary>
        /// Name of the ConsciousStates state that widens the rail and reveals the line buttons.
        /// </summary>
        private const string ExpandedState = "Expanded";

        /// <summary>
        /// Name of the ConsciousStates state that narrows the rail back to the thumb sliver.
        /// </summary>
        private const string CollapsedState = "Collapsed";

        /// <summary>
        /// How long the indicator stays visible after the last scroll or after the pointer leaves the
        /// scroller. Mirrors the WinUI ScrollBarContractDelay and ScrollViewerSeparatorContractDelay
        /// theme resources.
        /// </summary>
        private static readonly TimeSpan ContractDelay = TimeSpan.FromSeconds(2);

        #region IsIndicatorEnabled

        /// <summary>
        /// Identifies the IsIndicatorEnabled attached property.
        /// </summary>
        public static readonly DependencyProperty IsIndicatorEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsIndicatorEnabled",
                typeof(bool),
                typeof(ScrollBarExtensions),
                new FrameworkPropertyMetadata(defaultValue: false, OnIsIndicatorEnabledChanged));

        /// <summary>
        /// Gets whether the scrolling indicator behavior is attached to the specified scroll bar.
        /// </summary>
        /// <param name="obj">The scroll bar to read from.</param>
        /// <returns><see langword="true"/> when the behavior is attached; otherwise <see langword="false"/>.</returns>
        public static bool GetIsIndicatorEnabled(this ScrollBar obj)
        {
            return (bool)obj.GetValue(IsIndicatorEnabledProperty);
        }

        /// <summary>
        /// Sets whether the scrolling indicator behavior is attached to the specified scroll bar. The
        /// Fluence scroll bar styles set this to <see langword="true"/>; clearing it leaves the bar
        /// permanently visible.
        /// </summary>
        /// <param name="obj">The scroll bar to write to.</param>
        /// <param name="value"><see langword="true"/> to attach the behavior; otherwise <see langword="false"/>.</param>
        public static void SetIsIndicatorEnabled(this ScrollBar obj, bool value)
        {
            obj.SetValue(IsIndicatorEnabledProperty, value);
        }

        #endregion IsIndicatorEnabled

        #region IndicatorMode

        /// <summary>
        /// Identifies the IndicatorMode attached property.
        /// </summary>
        public static readonly DependencyProperty IndicatorModeProperty =
            DependencyProperty.RegisterAttached(
                "IndicatorMode",
                typeof(ScrollingIndicatorMode),
                typeof(ScrollBarExtensions),
                new FrameworkPropertyMetadata(ScrollingIndicatorMode.MouseIndicator, OnIndicatorModeChanged));

        /// <summary>
        /// Gets the scrolling indicator the specified scroll bar is currently showing.
        /// </summary>
        /// <param name="obj">The scroll bar to read from.</param>
        /// <returns>The active <see cref="ScrollingIndicatorMode"/>.</returns>
        public static ScrollingIndicatorMode GetIndicatorMode(this ScrollBar obj)
        {
            return (ScrollingIndicatorMode)obj.GetValue(IndicatorModeProperty);
        }

        /// <summary>
        /// Sets the scrolling indicator the specified scroll bar shows. The attached behavior writes this
        /// as the pointer and scroll state change; an application can also drive it directly.
        /// </summary>
        /// <param name="obj">The scroll bar to write to.</param>
        /// <param name="value">The indicator to show.</param>
        public static void SetIndicatorMode(this ScrollBar obj, ScrollingIndicatorMode value)
        {
            obj.SetValue(IndicatorModeProperty, value);
        }

        #endregion IndicatorMode

        #region Behavior plumbing

        /// <summary>
        /// Holds the per instance behavior object. A static extension class cannot carry per control
        /// state, so the state travels with the scroll bar in its own property store.
        /// </summary>
        private static readonly DependencyProperty BehaviorProperty =
            DependencyProperty.RegisterAttached(
                "Behavior",
                typeof(ScrollBarIndicatorBehavior),
                typeof(ScrollBarExtensions),
                new FrameworkPropertyMetadata(defaultValue: null));

        private static void OnIsIndicatorEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollBar scrollBar)
            {
                return;
            }

            if (d.GetValue(BehaviorProperty) is ScrollBarIndicatorBehavior existing)
            {
                existing.Detach();
                d.ClearValue(BehaviorProperty);
            }

            if (e.NewValue is true)
            {
                ScrollBarIndicatorBehavior behavior = new(scrollBar);
                d.SetValue(BehaviorProperty, behavior);
                behavior.Attach();
            }
        }

        private static void OnIndicatorModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d.GetValue(BehaviorProperty) as ScrollBarIndicatorBehavior)?.UpdateStates(useTransitions: true);
        }

        /// <summary>
        /// Owns the indicator state machine for one <see cref="ScrollBar"/>: the pointer subscriptions on
        /// the bar and on its templated parent scroll viewer, and the contract delay timer.
        /// </summary>
        private sealed class ScrollBarIndicatorBehavior
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ScrollBarIndicatorBehavior"/> class.
            /// </summary>
            /// <param name="owner">The scroll bar this behavior drives.</param>
            internal ScrollBarIndicatorBehavior(ScrollBar owner)
            {
                _owner = owner;
            }

            /// <summary>
            /// Subscribes to the owner's lifetime and pointer events and paints the initial state.
            /// </summary>
            internal void Attach()
            {
                _owner.Loaded += OnLoaded;
                _owner.Unloaded += OnUnloaded;
                _owner.MouseEnter += OnBarMouseEnter;
                _owner.MouseLeave += OnBarMouseLeave;
                _owner.IsMouseCaptureWithinChanged += OnCaptureWithinChanged;
                UpdateStates(useTransitions: false);
            }

            /// <summary>
            /// Unsubscribes everything and stops the contract timer, so a bar whose indicator is turned
            /// off leaves nothing running.
            /// </summary>
            internal void Detach()
            {
                _owner.Loaded -= OnLoaded;
                _owner.Unloaded -= OnUnloaded;
                _owner.MouseEnter -= OnBarMouseEnter;
                _owner.MouseLeave -= OnBarMouseLeave;
                _owner.IsMouseCaptureWithinChanged -= OnCaptureWithinChanged;
                UnhookScroller();
                StopTimer();
            }

            /// <summary>
            /// Moves both visual state groups to match the current indicator mode and pointer state.
            /// </summary>
            /// <param name="useTransitions">
            /// <see langword="true"/> to run the expand and contract transitions;
            /// <see langword="false"/> to snap, which is what the initial paint wants.
            /// </param>
            internal void UpdateStates(bool useTransitions)
            {
                // WinUI ships duplicate ExpandedWithoutAnimation and CollapsedWithoutAnimation states
                // because its framework runs state storyboards even with animations disabled. WPF has
                // the useTransitions flag instead, and every state storyboard here holds its final
                // value at KeyTime 0, so suppressing transitions is the same reduced motion result
                // without duplicating four states.
                bool animate = useTransitions && MotionHelper.IsMotionEnabled;

                _ = VisualStateManager.GoToState(_owner, IndicatorStateName(_owner.GetIndicatorMode()), animate);
                bool expanded = _owner.IsMouseOver || _owner.IsMouseCaptureWithin;
                _ = VisualStateManager.GoToState(_owner, expanded ? ExpandedState : CollapsedState, animate);
            }

            /// <summary>
            /// Maps an indicator mode onto the visual state name used by the scroll bar templates. The
            /// state for <see cref="ScrollingIndicatorMode.None"/> is called NoIndicator, matching WinUI.
            /// </summary>
            /// <param name="mode">The mode to map.</param>
            /// <returns>The visual state name.</returns>
            private static string IndicatorStateName(ScrollingIndicatorMode mode)
            {
                return mode switch
                {
                    ScrollingIndicatorMode.None => NoIndicatorState,
                    ScrollingIndicatorMode.TouchIndicator => TouchIndicatorState,
                    ScrollingIndicatorMode.MouseIndicator => MouseIndicatorState,
                    _ => NoIndicatorState,
                };
            }

            private void OnLoaded(object sender, RoutedEventArgs e)
            {
                HookScroller();

                // A bar with no host scroll viewer has nothing to reveal it, so it stays visible.
                _owner.SetIndicatorMode(_scroller?.IsMouseOver is not false
                    ? ScrollingIndicatorMode.MouseIndicator
                    : ScrollingIndicatorMode.None);
                UpdateStates(useTransitions: false);
            }

            private void OnUnloaded(object sender, RoutedEventArgs e)
            {
                StopTimer();
            }

            private void OnBarMouseEnter(object sender, MouseEventArgs e)
            {
                StopTimer();
                _owner.SetIndicatorMode(ScrollingIndicatorMode.MouseIndicator);
                UpdateStates(useTransitions: true);
            }

            private void OnBarMouseLeave(object sender, MouseEventArgs e)
            {
                UpdateStates(useTransitions: true);
                if (_scroller?.IsMouseOver is false)
                {
                    RestartTimer();
                }
            }

            private void OnCaptureWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                UpdateStates(useTransitions: true);
                if (e.NewValue is false && _scroller?.IsMouseOver is false && !_owner.IsMouseOver)
                {
                    RestartTimer();
                }
            }

            private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
            {
                if (e.VerticalChange is 0 && e.HorizontalChange is 0)
                {
                    return;
                }

                // A live stylus device means the scroll came from touch or pen, which WinUI answers with
                // the thin panning bar rather than the interactive rail.
                _owner.SetIndicatorMode(Stylus.CurrentStylusDevice is null
                    ? ScrollingIndicatorMode.MouseIndicator
                    : ScrollingIndicatorMode.TouchIndicator);
                RestartTimer();
            }

            private void OnScrollerMouseEnter(object sender, MouseEventArgs e)
            {
                StopTimer();
                _owner.SetIndicatorMode(ScrollingIndicatorMode.MouseIndicator);
            }

            private void OnScrollerMouseLeave(object sender, MouseEventArgs e)
            {
                RestartTimer();
            }

            private void OnContractTick(object? sender, EventArgs e)
            {
                StopTimer();
                if (_owner.IsMouseOver || _owner.IsMouseCaptureWithin)
                {
                    return;
                }

                if (_scroller?.IsMouseOver is true)
                {
                    return;
                }

                _owner.SetIndicatorMode(ScrollingIndicatorMode.None);
            }

            private void HookScroller()
            {
                if (_scroller is not null)
                {
                    return;
                }

                _scroller = _owner.TemplatedParent as ScrollViewer;
                if (_scroller is null)
                {
                    return;
                }

                _scroller.ScrollChanged += OnScrollChanged;
                _scroller.MouseEnter += OnScrollerMouseEnter;
                _scroller.MouseLeave += OnScrollerMouseLeave;
            }

            private void UnhookScroller()
            {
                if (_scroller is null)
                {
                    return;
                }

                _scroller.ScrollChanged -= OnScrollChanged;
                _scroller.MouseEnter -= OnScrollerMouseEnter;
                _scroller.MouseLeave -= OnScrollerMouseLeave;
                _scroller = null;
            }

            private void RestartTimer()
            {
                if (_timer is null)
                {
                    _timer = new DispatcherTimer(DispatcherPriority.Normal, _owner.Dispatcher)
                    {
                        Interval = ContractDelay,
                    };
                    _timer.Tick += OnContractTick;
                }

                _timer.Stop();
                _timer.Start();
            }

            private void StopTimer()
            {
                _timer?.Stop();
            }

            /// <summary>
            /// The scroll bar this behavior drives.
            /// </summary>
            private readonly ScrollBar _owner;

            /// <summary>
            /// The templated parent scroll viewer, when the bar is hosted in one.
            /// </summary>
            private ScrollViewer? _scroller;

            /// <summary>
            /// Delays the fade out after the last scroll or pointer exit.
            /// </summary>
            private DispatcherTimer? _timer;
        }

        #endregion Behavior plumbing
    }
}
