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
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Fluent-styled combo box with placeholder, icon, and rounded dropdown.
    /// Authority: WinUI 3 ComboBox_themeresources.xaml (FocusedStates / EditableFocusedStates VSM groups - WI-3 C18).
    /// Diverging from stock WPF, this control auto-selects index 0 when its items populate while
    /// <see cref="System.Windows.Controls.Primitives.Selector.SelectedIndex"/> is still -1 and has never been explicitly set.
    /// </summary>
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_DropdownBorder, Type = typeof(System.Windows.Controls.Border))]
    [TemplateVisualState(GroupName = "FocusedStates", Name = "Focused")]
    [TemplateVisualState(GroupName = "FocusedStates", Name = "Unfocused")]
    [TemplateVisualState(GroupName = "EditableFocusedStates", Name = "EditableFocused")]
    [TemplateVisualState(GroupName = "EditableFocusedStates", Name = "EditableUnfocused")]
    public class ComboBox : System.Windows.Controls.ComboBox
    {
        // Template part names.
        private const string PART_DropdownBorder = "PART_DropdownBorder";
        private const string PART_Popup = "PART_Popup";

        /// <summary>
        /// The duration of the dropdown open reveal slide and fade, mirroring the value of the
        /// ControlFastAnimationDuration motion token (Themes/Typography/Typography.xaml),
        /// which code mirrors by value like the previous template storyboard did.
        /// </summary>
        private const double RevealMilliseconds = 167;

        /// <summary>
        /// The distance in device-independent pixels the dropdown slides in from the control
        /// edge during the open reveal.
        /// </summary>
        private const double RevealOffsetPixels = 8;

        // Read-only dependency properties for selected content and text.
        private static readonly DependencyPropertyKey SelectedContentPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SelectedContent),
                typeof(object),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        private static readonly DependencyPropertyKey SelectedTextPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SelectedText),
                typeof(string),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="SelectedContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedContentProperty =
            SelectedContentPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="SelectedText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedTextProperty =
            SelectedTextPropertyKey.DependencyProperty;

        /// <summary>
        /// Initializes static members of the ComboBox class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ComboBox control uses its specific default
        /// style as defined in the application's theme or resource dictionaries. This is necessary for proper rendering
        /// and theming of the control in WPF applications.</remarks>
        static ComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ComboBox),
                new FrameworkPropertyMetadata(typeof(ComboBox)));
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the placeholder text displayed when no item is selected.
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Gets the content of the currently selected item.
        /// </summary>
        public object SelectedContent
        {
            get => GetValue(SelectedContentProperty);
            private set => SetValue(SelectedContentPropertyKey, value);
        }

        /// <summary>
        /// Gets the text representation of the currently selected item.
        /// </summary>
        public string SelectedText
        {
            get => (string)GetValue(SelectedTextProperty);
            private set => SetValue(SelectedTextPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the combo box.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Gets or sets the icon displayed in the combo box.
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropdownCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropdownCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(DropdownCornerRadius),
                typeof(CornerRadius),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the dropdown popup.
        /// </summary>
        public CornerRadius DropdownCornerRadius
        {
            get => (CornerRadius)GetValue(DropdownCornerRadiusProperty);
            set => SetValue(DropdownCornerRadiusProperty, value);
        }

        private static readonly DependencyPropertyKey IsDropDownOpenedUpwardPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsDropDownOpenedUpward),
                typeof(bool),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpenedUpward"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenedUpwardProperty =
            IsDropDownOpenedUpwardPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the dropdown is currently displayed above the control.
        /// </summary>
        public bool IsDropDownOpenedUpward
        {
            get => (bool)GetValue(IsDropDownOpenedUpwardProperty);
            private set => SetValue(IsDropDownOpenedUpwardPropertyKey, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _popup = GetTemplateChild(PART_Popup) as Popup;
            UpdateSelectedContent();
            UpdateFocusState(useTransitions: false);
            if (SelectedIndex == -1 && Items.Count > 0 && !IsSelectedIndexExplicitlySet())
            {
                _ = Dispatcher.BeginInvoke(TryAutoSelectFirstItem, DispatcherPriority.Loaded);
            }
        }

        /// <inheritdoc />
        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);
            UpdateDropDownDirection();

            // The reveal runs after the upward/downward decision is final for this open.
            BeginDropdownReveal();
        }

        /// <inheritdoc />
        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            IsDropDownOpenedUpward = false;
            _ = _popup?.Placement = PlacementMode.Bottom;
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedContent();
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateSelectedContent();
            TryAutoSelectFirstItem();
        }

        /// <inheritdoc />
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateFocusState(useTransitions: true);
        }

        /// <inheritdoc />
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            UpdateFocusState(useTransitions: true);
        }

        private void UpdateFocusState(bool useTransitions)
        {
            _ = VisualStateManager.GoToState(
                this,
                IsKeyboardFocusWithin ? "Focused" : "Unfocused",
                useTransitions);
        }

        private void UpdateDropDownDirection()
        {
            if (_popup is null || PresentationSource.FromVisual(this) is not PresentationSource source || source.CompositionTarget is null)
            {
                return;
            }
            double dpiY = source.CompositionTarget.TransformToDevice.M22;
            double maxHeightPx = (MaxDropDownHeight > 0 ? MaxDropDownHeight : 480) * dpiY;
            Point bottomEdge = PointToScreen(new Point(0, ActualHeight));
            Point topEdge = PointToScreen(new Point(0, 0));
            double workBottom = SystemParameters.WorkArea.Bottom * dpiY;
            double workTop = SystemParameters.WorkArea.Top * dpiY;
            double spaceBelow = workBottom - bottomEdge.Y;
            double spaceAbove = topEdge.Y - workTop;
            bool openUpward = spaceBelow < maxHeightPx && spaceAbove > spaceBelow;
            IsDropDownOpenedUpward = openUpward;
            _popup.Placement = openUpward ? PlacementMode.Top : PlacementMode.Bottom;
        }

        /// <summary>
        /// Plays the dropdown open reveal each time the dropdown opens: the dropdown surface
        /// slides 8 px in from the control edge (down from above for a downward dropdown, up
        /// from below for an upward one, per <see cref="IsDropDownOpenedUpward"/>) while
        /// fading 0 to 1, mirroring the previous template storyboard: 167 ms on the 0.8,0,0,1
        /// spline (the Typography.xaml ControlFastAnimationDuration and
        /// ControlFastOutSlowInKeySpline motion tokens, mirrored by value). The reveal moved
        /// from the template's MultiTrigger storyboards into code (FlyoutPresenter precedent)
        /// so it can consult the reduced-motion gate; a re-templated control without the
        /// canonical dropdown parts is left alone. The animations use
        /// <see cref="FillBehavior.Stop"/>; the completed handlers stamp the rest values and
        /// release the clocks so nothing stays animated once the reveal settles.
        /// </summary>
        private void BeginDropdownReveal()
        {
            if (GetTemplateChild(PART_DropdownBorder) is not System.Windows.Controls.Border border ||
                border.RenderTransform is not TranslateTransform translate)
            {
                return;
            }

            // Motion disabled (OS "Show animations" off): skip the reveal and show the
            // dropdown at rest immediately - translate 0, full opacity, no clocks.
            if (!MotionHelper.IsMotionEnabled)
            {
                translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
                translate.SetCurrentValue(TranslateTransform.YProperty, 0.0);
                border.BeginAnimation(UIElement.OpacityProperty, animation: null);
                border.SetCurrentValue(UIElement.OpacityProperty, 1.0);
                return;
            }

            double startOffset = IsDropDownOpenedUpward ? RevealOffsetPixels : -RevealOffsetPixels;

            // Seed the discrete start so the first rendered frame never flashes the rest
            // position: the offset toward the control edge, fully transparent.
            translate.SetCurrentValue(TranslateTransform.YProperty, startOffset);
            border.SetCurrentValue(UIElement.OpacityProperty, 0.0);

            DoubleAnimationUsingKeyFrames slideAnimation = CreateRevealAnimation(startOffset, 0.0);
            slideAnimation.Completed += (_, _) =>
            {
                translate.SetCurrentValue(TranslateTransform.YProperty, 0.0);
                translate.BeginAnimation(TranslateTransform.YProperty, animation: null);
            };

            DoubleAnimationUsingKeyFrames fadeAnimation = CreateRevealAnimation(0.0, 1.0);
            fadeAnimation.Completed += (_, _) =>
            {
                border.SetCurrentValue(UIElement.OpacityProperty, 1.0);
                border.BeginAnimation(UIElement.OpacityProperty, animation: null);
            };

            translate.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
            border.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// Builds one track of the dropdown open reveal: a discrete start at time zero
        /// settling at the rest value over the fast motion duration on the decelerating
        /// Fluent key spline (see <see cref="BeginDropdownReveal"/> for the mirrored
        /// Typography.xaml motion tokens).
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

        private bool IsSelectedIndexExplicitlySet()
        {
            ValueSource source = DependencyPropertyHelper.GetValueSource(this, SelectedIndexProperty);
            return source.BaseValueSource is not BaseValueSource.Default;
        }

        private void TryAutoSelectFirstItem()
        {
            if (!_isAutoSelecting && SelectedIndex == -1 && Items.Count > 0 && !IsSelectedIndexExplicitlySet())
            {
                _isAutoSelecting = true;
                try
                {
                    SelectedIndex = 0;
                }
                finally
                {
                    _isAutoSelecting = false;
                }
            }
        }

        private void UpdateSelectedContent()
        {
            object item = SelectedItem;
            if (item is ComboBoxItem comboBoxItem)
            {
                SelectedContent = comboBoxItem.Content;
                SelectedText = comboBoxItem.Content?.ToString() ?? string.Empty;
                return;
            }
            SelectedContent = item;
            SelectedText = item?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Represents the backing field for a Popup instance, which may be null if no popup is currently associated.
        /// </summary>
        /// <remarks>This field is typically used internally to store a reference to a Popup control. It
        /// is not intended for direct access outside the containing class.</remarks>
        private Popup? _popup;

        /// <summary>
        /// Indicates whether the auto-selection process is currently active.
        /// </summary>
        private bool _isAutoSelecting;
    }
}
