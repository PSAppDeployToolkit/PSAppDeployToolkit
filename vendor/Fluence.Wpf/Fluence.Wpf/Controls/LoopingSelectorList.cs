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
using System.Windows.Input;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// The selector column used by the <see cref="DatePicker"/> and <see cref="TimePicker"/>
    /// flyouts, mirroring the WinUI 3 <c language="csharp">LoopingSelector</c>. Rows are a fixed
    /// <see cref="ItemHeight"/> tall, the viewport shows exactly nine of them, and the row in
    /// the middle is always the selected one: scrolling moves the selection and setting the
    /// selection scrolls the row under the flyout's highlight band.
    /// </summary>
    /// <remarks>
    /// Feed the column a <c language="csharp">LoopingItemsSource</c> to make it wrap endlessly, or a plain list
    /// padded with <c language="xaml">LoopingSelectorPlaceholder</c> rows when the values must not repeat (the
    /// AM/PM designator column). Both cases are handled by the picker code through
    /// <c language="csharp">Fluence.Wpf.Helpers.LoopingSelectorColumns</c>; nothing here assumes a particular
    /// item type.
    /// </remarks>
    [TemplatePart(Name = PART_ScrollViewer, Type = typeof(ScrollViewer))]
    public sealed class LoopingSelectorList : ListBox
    {
        /// <summary>
        /// The name of the scroll viewer template part that owns the column's item-unit
        /// vertical offset.
        /// </summary>
        private const string PART_ScrollViewer = "PART_ScrollViewer";

        /// <summary>
        /// Initializes static members of the <see cref="LoopingSelectorList"/> class and
        /// overrides the default style metadata so the column picks up its themed template
        /// from Generic.xaml.
        /// </summary>
        static LoopingSelectorList()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(LoopingSelectorList),
                new FrameworkPropertyMetadata(typeof(LoopingSelectorList)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopingSelectorList"/> class and sizes
        /// its viewport to the nine rows the selection band geometry assumes.
        /// </summary>
        public LoopingSelectorList()
        {
            UpdateViewportHeight();

            // The template viewer pans (PanningMode=VerticalOnly), and a pan can settle on a
            // fractional item-unit offset that leaves the column resting between two rows, off
            // the highlight band. The viewer marks manipulation events handled internally, so
            // listen with handledEventsToo and snap to the nearest whole row once the pan (and
            // its inertia) completes.
            AddHandler(
                ManipulationCompletedEvent,
                new EventHandler<ManipulationCompletedEventArgs>(OnPanCompleted),
                handledEventsToo: true);
        }

        private void OnPanCompleted(object? sender, ManipulationCompletedEventArgs e)
        {
            if (_scrollViewer is null)
            {
                return;
            }

            double offset = _scrollViewer.VerticalOffset;
            double settledOffset = Math.Round(offset, MidpointRounding.AwayFromZero);
            if (Math.Abs(offset - settledOffset) > 0.001)
            {
                _scrollViewer.ScrollToVerticalOffset(settledOffset);
            }
        }

        /// <summary>
        /// The number of padding rows above (and below) the selected row. The selected row is
        /// always the middle one of the viewport, so this is both the offset from the first
        /// visible row to the selection and the amount of slack a non-looping column needs at
        /// each end.
        /// </summary>
        internal const int PaddingItemsCount = 4;

        /// <summary>
        /// The number of rows the viewport shows: the selected row plus
        /// <see cref="PaddingItemsCount"/> rows above and below it.
        /// </summary>
        internal const int ViewportItemsCount = (PaddingItemsCount * 2) + 1;

        /// <summary>
        /// Identifies the <see cref="ItemHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                nameof(ItemHeight),
                typeof(double),
                typeof(LoopingSelectorList),
                new FrameworkPropertyMetadata(
                    40.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnItemHeightChanged));

        /// <summary>
        /// Gets or sets the height of a single row. The column's own height is derived from it
        /// (nine rows), and the default item container style binds each container to it, so the
        /// item-unit scroll offset and the flyout's highlight band always line up. The default
        /// is 40, the WinUI <c language="xaml">TimePickerFlyoutPresenterItemHeight</c>.
        /// </summary>
        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        /// <summary>
        /// Identifies the read-only <see cref="SuppressItemMouseOver"/> dependency property.
        /// </summary>
        private static readonly DependencyPropertyKey SuppressItemMouseOverPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SuppressItemMouseOver),
                typeof(bool),
                typeof(LoopingSelectorList),
                new FrameworkPropertyMetadata(defaultValue: false));

        /// <summary>
        /// Identifies the <see cref="SuppressItemMouseOver"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SuppressItemMouseOverProperty =
            SuppressItemMouseOverPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether item hover visuals are suppressed because the column
        /// is being scrolled. Rows sliding under a stationary pointer would otherwise light up
        /// one after another, which reads as flicker rather than as hover; the flag clears on
        /// the first genuine pointer move after the scroll.
        /// </summary>
        public bool SuppressItemMouseOver => (bool)GetValue(SuppressItemMouseOverProperty);

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Unsubscribe-first so re-templating never leaves stale handlers behind.
            _scrollViewer?.ScrollChanged -= OnScrollViewerScrollChanged;

            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild(PART_ScrollViewer) as ScrollViewer;
            _scrollViewer?.ScrollChanged += OnScrollViewerScrollChanged;

            ApplyScrollOffset();
        }

        /// <summary>
        /// Hides and disables the container of a padding row so it occupies its row height
        /// without being visible, hit-testable, or selectable.
        /// </summary>
        /// <param name="element">The container being prepared.</param>
        /// <param name="item">The item the container is being prepared for.</param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is FrameworkElement container && item is LoopingSelectorPlaceholder)
            {
                container.SetValue(IsEnabledProperty, value: false);
                container.SetValue(VisibilityProperty, Visibility.Hidden);
            }
        }

        /// <summary>
        /// Clears the padding-row state from a container. Containers are recycled, so a
        /// container that once held a padding row would otherwise stay hidden and disabled
        /// when it is reused for a real value.
        /// </summary>
        /// <param name="element">The container being cleared.</param>
        /// <param name="item">The item the container held.</param>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is FrameworkElement container)
            {
                container.ClearValue(IsEnabledProperty);
                container.ClearValue(VisibilityProperty);
            }

            base.ClearContainerForItemOverride(element, item);
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // A selection that came from the scroll offset already sits at the right offset, so
            // re-applying it here would fight the scroll that is still in progress.
            if (!_isSyncingFromScroll)
            {
                ApplyScrollOffset();
            }
        }

        /// <summary>
        /// Maps the navigation keys onto scrolling rather than onto selection movement, since
        /// the selection follows the offset. Home and End are swallowed: on a looping column
        /// there is no meaningful first or last value, and on a padded column both ends are
        /// placeholder rows. Without the template's scroll viewer there is nothing to scroll, so
        /// the keys are left to the base ListBox behavior rather than swallowed.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_scrollViewer is null
                || e.Key is not (Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End))
            {
                base.OnKeyDown(e);
                return;
            }

            if (e.Key is Key.Up)
            {
                _scrollViewer.LineUp();
            }
            else if (e.Key is Key.Down)
            {
                _scrollViewer.LineDown();
            }
            else if (e.Key is Key.PageUp)
            {
                _scrollViewer.PageUp();
            }
            else if (e.Key is Key.PageDown)
            {
                _scrollViewer.PageDown();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Scrolls one row per wheel notch instead of the system's multi-line step, so a notch
        /// moves the selection by exactly one value. Deltas accumulate across events before a
        /// row is stepped: a precision touchpad delivers many sub-notch deltas per physical
        /// notch, and stepping a full row per event would make one gentle notch jump several
        /// values.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            if (e.Handled || _scrollViewer is null || e.Delta is 0)
            {
                return;
            }

            // Direction flips drop the opposite-sign remainder so a reversal responds instantly.
            if (Math.Sign(_wheelDeltaAccumulator) != Math.Sign(e.Delta))
            {
                _wheelDeltaAccumulator = 0;
            }

            _wheelDeltaAccumulator += e.Delta;
            int lines = _wheelDeltaAccumulator / Mouse.MouseWheelDeltaForOneLine;
            _wheelDeltaAccumulator -= lines * Mouse.MouseWheelDeltaForOneLine;

            if (lines is not 0)
            {
                DisableItemMouseOver();
            }

            for (int line = 0; line < Math.Abs(lines); line++)
            {
                if (lines > 0)
                {
                    _scrollViewer.LineUp();
                }
                else
                {
                    _scrollViewer.LineDown();
                }
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // Scrolling under a stationary pointer raises one synthetic move; only a move after
            // that one is a genuine pointer move that should restore hover visuals.
            if (_ignoreNextMouseMove)
            {
                _ignoreNextMouseMove = false;
            }
            else if (SuppressItemMouseOver)
            {
                ClearValue(SuppressItemMouseOverPropertyKey);
            }
        }

        /// <summary>
        /// Resizes the column to nine rows whenever <see cref="ItemHeight"/> changes.
        /// </summary>
        /// <param name="d">The column whose row height changed.</param>
        /// <param name="e">The event data.</param>
        private static void OnItemHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LoopingSelectorList)d).UpdateViewportHeight();
        }

        /// <summary>
        /// Converts a row index into the item-unit vertical offset that centres it.
        /// </summary>
        /// <param name="index">The row index to centre.</param>
        /// <returns>The vertical offset, in item units, that puts the row in the middle.</returns>
        private static double IndexToOffset(int index)
        {
            return index - PaddingItemsCount;
        }

        /// <summary>
        /// Converts an item-unit vertical offset into the index of the centred row.
        /// </summary>
        /// <param name="offset">The vertical offset, in item units.</param>
        /// <returns>The index of the row in the middle of the viewport.</returns>
        private static int OffsetToIndex(double offset)
        {
            return (int)offset + PaddingItemsCount;
        }

        /// <summary>
        /// Sizes the column to exactly <see cref="ViewportItemsCount"/> rows. The item-unit
        /// offset math and the flyout's centred highlight band both depend on the viewport
        /// being that tall, so the height is derived rather than left to the layout.
        /// </summary>
        private void UpdateViewportHeight()
        {
            SetCurrentValue(HeightProperty, ItemHeight * ViewportItemsCount);
        }

        /// <summary>
        /// Keeps the selection and the scroll offset in step. An extent change means the panel
        /// has just measured a new item source, so any offset requested before that was clamped
        /// against a stale extent and has to be re-applied; otherwise the row that scrolled into
        /// the middle of the viewport becomes the selected one.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange is not 0)
            {
                ApplyScrollOffset();
                return;
            }

            if (e.VerticalChange is 0)
            {
                return;
            }

            DisableItemMouseOver();

            int index = OffsetToIndex(e.VerticalOffset);
            if (index < 0 || index >= Items.Count || index == SelectedIndex)
            {
                return;
            }

            _isSyncingFromScroll = true;
            try
            {
                SetCurrentValue(SelectedIndexProperty, index);
            }
            finally
            {
                _isSyncingFromScroll = false;
            }
        }

        /// <summary>
        /// Scrolls the selected row to the middle of the viewport. This replaces
        /// <see cref="System.Windows.Controls.ListBox.ScrollIntoView(object)"/>, which only
        /// guarantees the row is somewhere on screen and would leave the selection off the
        /// highlight band.
        /// </summary>
        private void ApplyScrollOffset()
        {
            int index = SelectedIndex;
            if (_scrollViewer is null || index < 0)
            {
                return;
            }

            _scrollViewer.ScrollToVerticalOffset(IndexToOffset(index));
        }

        /// <summary>
        /// Suppresses item hover visuals for the duration of a scroll and arms the one-move
        /// grace that keeps the synthetic pointer move the scroll itself raises from clearing
        /// the suppression immediately.
        /// </summary>
        private void DisableItemMouseOver()
        {
            if (_ignoreNextMouseMove)
            {
                return;
            }

            _ignoreNextMouseMove = true;

            if (!SuppressItemMouseOver)
            {
                SetValue(SuppressItemMouseOverPropertyKey, value: true);
            }
        }

        /// <summary>
        /// The template's scroll viewer, which owns the column's item-unit vertical offset.
        /// </summary>
        private ScrollViewer? _scrollViewer;

        /// <summary>
        /// Set while a scroll is driving the selection, so the resulting selection change does
        /// not scroll again.
        /// </summary>
        private bool _isSyncingFromScroll;

        /// <summary>
        /// Set when a scroll suppressed hover visuals, so the synthetic pointer move that
        /// follows the scroll does not immediately restore them.
        /// </summary>
        private bool _ignoreNextMouseMove;

        /// <summary>
        /// Wheel delta carried between events so sub-notch deltas (precision touchpads) add up
        /// to whole 120-unit notches before a row is stepped.
        /// </summary>
        private int _wheelDeltaAccumulator;
    }
}
