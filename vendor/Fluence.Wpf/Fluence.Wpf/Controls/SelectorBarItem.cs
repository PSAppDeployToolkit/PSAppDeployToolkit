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
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// One selectable entry of a <see cref="SelectorBar"/>, mirroring the WinUI 3
    /// <c language="csharp">SelectorBarItem</c>: an optional icon, a text label, and an accent
    /// pill that grows out from the label centre while the item is selected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WinUI's SelectorBarItem carries <see cref="Text"/> and <see cref="Icon"/> rather than
    /// arbitrary content, and this port keeps that surface. The type still inherits
    /// <see cref="ContentControl.Content"/> from its <see cref="ListBoxItem"/> base, but the
    /// default template does not present it; an item generated for a plain data item instead
    /// takes its label from <see cref="SelectorBar"/>, which fills <see cref="Text"/> in when
    /// it prepares the container.
    /// </para>
    /// <para>
    /// The selection pill is animated in code rather than from a template storyboard so the
    /// reveal can be skipped when motion is disabled, the pattern <see cref="PipsPager"/>
    /// already uses. WinUI plays the same reveal over its ComboBoxItemScaleAnimationDuration
    /// (167 ms) on the 0,0,0,1 key spline, scaling the pill from its 4 px rest width to four
    /// times that (SelectorBar.xaml SelectedNormal), and snaps straight back when the item is
    /// unselected.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_SelectionVisual, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PART_SelectionVisualScale, Type = typeof(ScaleTransform))]
    public class SelectorBarItem : ListBoxItem
    {
        /// <summary>
        /// The name of the accent pill the selection reveal fades in.
        /// </summary>
        private const string PART_SelectionVisual = "PART_SelectionVisual";

        /// <summary>
        /// The name of the pill's horizontal scale transform, the one the reveal grows.
        /// </summary>
        private const string PART_SelectionVisualScale = "PART_SelectionVisualScale";

        /// <summary>
        /// The duration of the selection reveal, mirroring the value of WinUI's
        /// ComboBoxItemScaleAnimationDuration (ComboBox_themeresources.xaml), which is the same
        /// 167 ms as the ControlFastAnimationDuration token this library publishes. Code-built
        /// animations cannot reference the XAML TimeSpan token, so the value is mirrored here.
        /// </summary>
        private const double SelectionRevealMilliseconds = 167;

        /// <summary>
        /// The horizontal scale the pill reaches while selected. WinUI's SelectedNormal state
        /// animates PillTransform.ScaleX to 4 over a 4 px rest width, so the selected pill is
        /// 16 px wide (SelectorBar.xaml, SelectorBar_themeresources.xaml).
        /// </summary>
        private const double SelectedPillScaleX = 4.0;

        /// <summary>
        /// Initializes static members of the SelectorBarItem class and overrides the default
        /// style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static SelectorBarItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SelectorBarItem),
                new FrameworkPropertyMetadata(typeof(SelectorBarItem)));
        }

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(SelectorBarItem),
                new FrameworkPropertyMetadata(defaultValue: null));

        /// <summary>
        /// Gets or sets the label shown beside the optional <see cref="Icon"/>.
        /// </summary>
        public string? Text
        {
            get => (string?)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(SelectorBarItem),
                new FrameworkPropertyMetadata(defaultValue: null));

        /// <summary>
        /// Gets or sets the icon shown before <see cref="Text"/>. The default template collapses
        /// the icon presenter while this is <see langword="null"/>, so a text-only item carries
        /// no leading gap.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _selectionVisual = GetTemplateChild(PART_SelectionVisual) as FrameworkElement;
            _selectionVisualScale = GetTemplateChild(PART_SelectionVisualScale) as ScaleTransform;

            // A container realized into an already-selected state has no transition to play:
            // stamp the pose the reveal would have ended on.
            ApplySelectionVisual(IsSelected, animate: false);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SelectorBarItemAutomationPeer(this);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Swallows the Ctrl+Click deselect gesture on the item that already carries the pill.
        /// WPF's <see cref="ListBox"/> honours it even in <see cref="SelectionMode.Single"/>,
        /// which would leave the bar with no selection at all; WinUI's SelectorBar has no
        /// deselect gesture, and the pill is the page's current destination. Ctrl+Click on any
        /// other item still moves the selection there.
        /// </remarks>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsSelected && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewMouseLeftButtonDown(e);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The keyboard half of the same gesture: Ctrl+Space toggles selection on a WPF
        /// <see cref="ListBoxItem"/>, and toggling off is what WinUI does not offer.
        /// </remarks>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (IsSelected && e.Key is Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        /// <inheritdoc />
        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            ApplySelectionVisual(selected: true, animate: true);
        }

        /// <inheritdoc />
        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);

            // WinUI's UnselectedNormal state carries no storyboard, so the pill leaves without
            // an exit animation and the incoming item's reveal is the only motion on screen.
            ApplySelectionVisual(selected: false, animate: false);
        }

        /// <summary>
        /// Drives the pill to the pose <paramref name="selected"/> calls for: full opacity at
        /// <see cref="SelectedPillScaleX"/> when selected, transparent at rest width when not.
        /// </summary>
        /// <param name="selected">Whether the item is selected.</param>
        /// <param name="animate">Whether to play the reveal rather than stamping the pose.</param>
        private void ApplySelectionVisual(bool selected, bool animate)
        {
            if (_selectionVisual is null || _selectionVisualScale is null)
            {
                return;
            }

            double targetScale = selected ? SelectedPillScaleX : 1.0;
            double targetOpacity = selected ? 1.0 : 0.0;

            if (!animate || !MotionHelper.IsMotionEnabled)
            {
                // Release any in-flight clock first: an animation holding its end value would
                // otherwise outrank the local value set here.
                _selectionVisual.BeginAnimation(OpacityProperty, animation: null);
                _selectionVisualScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation: null);
                _selectionVisual.SetCurrentValue(OpacityProperty, targetOpacity);
                _selectionVisualScale.SetCurrentValue(ScaleTransform.ScaleXProperty, targetScale);
                return;
            }

            _selectionVisual.BeginAnimation(OpacityProperty, CreateRevealAnimation(targetOpacity), HandoffBehavior.SnapshotAndReplace);
            _selectionVisualScale.BeginAnimation(ScaleTransform.ScaleXProperty, CreateRevealAnimation(targetScale), HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Builds one track of the selection reveal: the live value settling on
        /// <paramref name="to"/> over the WinUI reveal duration on the decelerating Fluent key
        /// spline.
        /// </summary>
        /// <param name="to">The value the track settles on.</param>
        /// <returns>The keyframe animation for the track.</returns>
        private static DoubleAnimationUsingKeyFrames CreateRevealAnimation(double to)
        {
            return new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(SelectionRevealMilliseconds)),
                KeyFrames =
                {
                    new SplineDoubleKeyFrame(
                        to,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(SelectionRevealMilliseconds)),
                        MotionHelper.FastOutSlowInKeySpline),
                },
            };
        }

        /// <summary>
        /// The accent pill, or null until the template is applied.
        /// </summary>
        private FrameworkElement? _selectionVisual;

        /// <summary>
        /// The pill's horizontal scale transform, or null until the template is applied.
        /// </summary>
        private ScaleTransform? _selectionVisualScale;
    }
}
