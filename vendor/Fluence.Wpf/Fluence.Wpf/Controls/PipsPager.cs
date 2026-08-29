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
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

// IMPORTANT: every reference to ToggleButton / ButtonBase / Panel in this file
// is fully qualified (System.Windows.Controls.Primitives.ToggleButton,
// System.Windows.Controls.Primitives.ButtonBase, System.Windows.Controls.Panel).
// The Fluence.Wpf.Controls namespace defines its own ToggleButton, Button, and
// StackPanel subclasses, and because this file sits inside that namespace, any
// unqualified reference resolves to the Fluence subclass. The default PipsPager
// template instantiates the stock WPF primitives, so an unqualified cast against
// the Fluence subclass would silently return null and the pager would never wire
// its parts. See NumberBox.cs for the same constraint.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A page indicator mirroring the WinUI 3 <c language="csharp">PipsPager</c>: a horizontal or vertical run
    /// of round pip dots, one per visible page, with the selected pip rendered larger in the
    /// accent fill. Clicking a pip selects its page, optional previous/next chevron buttons
    /// step the selection, and arrow keys move the selection while keyboard focus is inside
    /// the pager.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One pip per page is generated in code into the <c language="xaml">PART_PipsHost</c> panel (the same
    /// approach as <see cref="RatingControl"/>) and the whole run is hosted in the
    /// <c language="xaml">PART_PipsScrollViewer</c> viewport. When <see cref="NumberOfPages"/> exceeds
    /// <see cref="MaxVisiblePips"/> the viewport is clamped to <see cref="MaxVisiblePips"/>
    /// pip boxes along the orientation axis and stays put while the selection moves inside
    /// it, scrolling only far enough to bring a selection that has left the viewport back to
    /// the nearest edge. Subscribe to <see cref="SelectedIndexChanged"/> to react to
    /// selection moves from any input path.
    /// </para>
    /// <para>
    /// Pips are realized eagerly rather than virtualized, so a pager is meant for the page
    /// counts a page indicator is readable at, not for thousands of pages. One WinUI behavior
    /// remains a deliberate omission: the scale-down of the pips at the viewport edges.
    /// Navigation buttons in <see cref="PipsPagerButtonVisibility.VisibleOnPointerOver"/>
    /// mode collapse when the pointer leaves, so the pager's desired size changes with hover.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_PreviousButton, Type = typeof(System.Windows.Controls.Primitives.ButtonBase))]
    [TemplatePart(Name = PART_NextButton, Type = typeof(System.Windows.Controls.Primitives.ButtonBase))]
    [TemplatePart(Name = PART_PipsHost, Type = typeof(Panel))]
    [TemplatePart(Name = PART_PipsScrollViewer, Type = typeof(ScrollViewer))]
    public class PipsPager : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_PreviousButton = "PART_PreviousButton";
        private const string PART_NextButton = "PART_NextButton";
        private const string PART_PipsHost = "PART_PipsHost";
        private const string PART_PipsScrollViewer = "PART_PipsScrollViewer";

        // Resource key of the ToggleButton style applied to every generated pip.
        private const string PipStyleKey = "PipsPagerPipStyle";

        // Every pip occupies one fixed square touch target from PipsPagerPipStyle (the dot
        // inside it morphs between 4, 5, and 6 px, but the box never changes). WinUI sizes the
        // viewport from a separate rest and selected box, so the two sizes are kept as distinct
        // arguments to CalculateViewportExtent even though this template feeds it one value.
        private const double PipBoxSize = 20.0;

        // Typography.xaml ControlFastAnimationDuration, mirrored by value because a code-built
        // animation cannot reference the resource (the ContentDialog and ComboBox precedent).
        private const double ScrollAnimationMilliseconds = 167.0;

        private System.Windows.Controls.Primitives.ButtonBase? _previousButton;
        private System.Windows.Controls.Primitives.ButtonBase? _nextButton;
        private Panel? _pipsHost;
        private ScrollViewer? _pipsScrollViewer;

        // Offset the viewport is scrolling toward along the orientation axis. Read as the
        // starting point for the next edge-scroll so that selections arriving faster than the
        // animation still compose from the destination rather than from a mid-flight offset.
        private double _targetScrollOffset;

        // Offset most recently written into the viewport by ApplyScrollOffset. A ScrollChanged
        // whose offset differs from this is external input the hidden scrollbars cannot fully
        // block (Home/End bubbling from a focused pip, wheel over a vertical pager) and must be
        // snapped back to the pager-owned target, or the believed and real offsets desync and
        // later edge-scroll decisions leave the selected pip outside the viewport.
        private double _lastAppliedScrollOffset;

        /// <summary>
        /// Initializes static members of the PipsPager class and overrides the default style
        /// metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static PipsPager()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PipsPager),
                new FrameworkPropertyMetadata(typeof(PipsPager)));
        }

        /// <summary>
        /// Identifies the <see cref="NumberOfPages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NumberOfPagesProperty =
            DependencyProperty.Register(
                nameof(NumberOfPages),
                typeof(int),
                typeof(PipsPager),
                new FrameworkPropertyMetadata(0, OnNumberOfPagesChanged, CoerceNumberOfPages));

        /// <summary>
        /// Gets or sets the total number of pages represented by the pager. Negative values
        /// coerce to 0. Default is 0 (no pips).
        /// </summary>
        public int NumberOfPages
        {
            get => (int)GetValue(NumberOfPagesProperty);
            set => SetValue(NumberOfPagesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedPageIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedPageIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedPageIndex),
                typeof(int),
                typeof(PipsPager),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedPageIndexChanged,
                    CoerceSelectedPageIndex));

        /// <summary>
        /// Gets or sets the zero-based index of the selected page. Values coerce into
        /// [0, <see cref="NumberOfPages"/> - 1], and to 0 while the pager has no pages.
        /// Binds two-way by default.
        /// </summary>
        public int SelectedPageIndex
        {
            get => (int)GetValue(SelectedPageIndexProperty);
            set => SetValue(SelectedPageIndexProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxVisiblePips"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxVisiblePipsProperty =
            DependencyProperty.Register(
                nameof(MaxVisiblePips),
                typeof(int),
                typeof(PipsPager),
                new FrameworkPropertyMetadata(5, OnMaxVisiblePipsChanged, CoerceMaxVisiblePips));

        /// <summary>
        /// Gets or sets the maximum number of pips visible at once. Every page still gets a
        /// pip; when <see cref="NumberOfPages"/> exceeds this count the pip run scrolls inside
        /// a viewport this many pips long. Values below 1 coerce to 1. Default is 5, matching
        /// WinUI.
        /// </summary>
        public int MaxVisiblePips
        {
            get => (int)GetValue(MaxVisiblePipsProperty);
            set => SetValue(MaxVisiblePipsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Orientation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(PipsPager),
                new FrameworkPropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        /// <summary>
        /// Gets or sets whether the pips flow horizontally or vertically. The default
        /// template also swaps the navigation chevrons between left/right and up/down to
        /// match. Default is <see cref="Orientation.Horizontal"/>.
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreviousButtonVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviousButtonVisibilityProperty =
            DependencyProperty.Register(
                nameof(PreviousButtonVisibility),
                typeof(PipsPagerButtonVisibility),
                typeof(PipsPager),
                new FrameworkPropertyMetadata(PipsPagerButtonVisibility.Collapsed));

        /// <summary>
        /// Gets or sets when the previous-page chevron button is shown. The button is
        /// disabled while the first page is selected. Default is
        /// <see cref="PipsPagerButtonVisibility.Collapsed"/>, matching WinUI.
        /// </summary>
        public PipsPagerButtonVisibility PreviousButtonVisibility
        {
            get => (PipsPagerButtonVisibility)GetValue(PreviousButtonVisibilityProperty);
            set => SetValue(PreviousButtonVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NextButtonVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NextButtonVisibilityProperty =
            DependencyProperty.Register(
                nameof(NextButtonVisibility),
                typeof(PipsPagerButtonVisibility),
                typeof(PipsPager),
                new FrameworkPropertyMetadata(PipsPagerButtonVisibility.Collapsed));

        /// <summary>
        /// Gets or sets when the next-page chevron button is shown. The button is disabled
        /// while the last page is selected. Default is
        /// <see cref="PipsPagerButtonVisibility.Collapsed"/>, matching WinUI.
        /// </summary>
        public PipsPagerButtonVisibility NextButtonVisibility
        {
            get => (PipsPagerButtonVisibility)GetValue(NextButtonVisibilityProperty);
            set => SetValue(NextButtonVisibilityProperty, value);
        }

        /// <summary>
        /// Occurs after <see cref="SelectedPageIndex"/> has changed from any input path
        /// (pip click, navigation buttons, arrow keys, or a programmatic set). The event args
        /// carry the previous and the new zero-based page index.
        /// </summary>
        public event EventHandler<PipsPagerSelectedIndexChangedEventArgs>? SelectedIndexChanged;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _previousButton?.Click -= OnPreviousButtonClick;
            _nextButton?.Click -= OnNextButtonClick;
            UnhookPips();
            _pipsHost?.RequestBringIntoView -= OnPipsBringIntoViewRequested;
            _pipsHost?.KeyDown -= OnPipsHostKeyDown;
            _pipsScrollViewer?.ScrollChanged -= OnPipsScrollViewerScrollChanged;

            _previousButton = GetTemplateChild(PART_PreviousButton) as System.Windows.Controls.Primitives.ButtonBase;
            _nextButton = GetTemplateChild(PART_NextButton) as System.Windows.Controls.Primitives.ButtonBase;
            _pipsHost = GetTemplateChild(PART_PipsHost) as Panel;
            _pipsScrollViewer = GetTemplateChild(PART_PipsScrollViewer) as ScrollViewer;

            _previousButton?.Click += OnPreviousButtonClick;
            _nextButton?.Click += OnNextButtonClick;
            _pipsHost?.RequestBringIntoView += OnPipsBringIntoViewRequested;
            _pipsHost?.KeyDown += OnPipsHostKeyDown;
            _pipsScrollViewer?.ScrollChanged += OnPipsScrollViewerScrollChanged;

            ResetScrollOffset();
            UpdatePips();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new Automation.PipsPagerAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            TryMoveSelection(e);
        }

        private void OnPipsHostKeyDown(object sender, KeyEventArgs e)
        {
            // The pips sit inside PART_PipsScrollViewer, and a ScrollViewer claims the arrow keys
            // for its own line scrolling as the event bubbles past it, well before the pager's
            // OnKeyDown would see them. Moving the selection here, on the host panel one element
            // below the viewport, keeps arrow-key navigation working and leaves the viewport
            // offset entirely pager-driven. Keys arriving from the chevron buttons are outside
            // the viewport and still reach OnKeyDown normally.
            TryMoveSelection(e);
        }

        private void TryMoveSelection(KeyEventArgs e)
        {
            if (e.Handled || NumberOfPages <= 0)
            {
                return;
            }

            if (e.Key is Key.Left or Key.Up)
            {
                SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex - 1);
                e.Handled = true;
            }
            else if (e.Key is Key.Right or Key.Down)
            {
                SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex + 1);
                e.Handled = true;
            }
            else if (e.Key is Key.Home)
            {
                SetCurrentValue(SelectedPageIndexProperty, 0);
                e.Handled = true;
            }
            else if (e.Key is Key.End)
            {
                SetCurrentValue(SelectedPageIndexProperty, NumberOfPages - 1);
                e.Handled = true;
            }
            else if (e.Key is Key.PageUp or Key.PageDown)
            {
                // No page-jump semantics in WinUI's PipsPager, but left unhandled these bubble to
                // PART_PipsScrollViewer, which pages the viewport itself and desyncs the real
                // offset from the pager-owned target.
                e.Handled = true;
            }
        }

        private static object CoerceNumberOfPages(DependencyObject d, object baseValue)
        {
            int proposed = (int)baseValue;
            return proposed < 0 ? 0 : baseValue;
        }

        private static void OnNumberOfPagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PipsPager pager = (PipsPager)d;
            pager.CoerceValue(SelectedPageIndexProperty);
            pager.UpdatePips();
        }

        private static object CoerceSelectedPageIndex(DependencyObject d, object baseValue)
        {
            PipsPager pager = (PipsPager)d;
            int proposed = (int)baseValue;
            int lastIndex = pager.NumberOfPages - 1;
            return lastIndex < 0 || proposed < 0
                ? 0
                : proposed > lastIndex ? lastIndex : baseValue;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0091:Sender should be 'this' for instance events", Justification = "The method is static.")]
        private static void OnSelectedPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PipsPager pager = (PipsPager)d;
            pager.UpdatePips();
            pager.SelectedIndexChanged?.Invoke(
                pager,
                new PipsPagerSelectedIndexChangedEventArgs((int)e.OldValue, (int)e.NewValue));
        }

        private static object CoerceMaxVisiblePips(DependencyObject d, object baseValue)
        {
            int proposed = (int)baseValue;
            return proposed < 1 ? 1 : baseValue;
        }

        private static void OnMaxVisiblePipsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PipsPager)d).UpdatePips();
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The offset lives on whichever axis the pips flow along, so a flip has to release
            // the old axis before the new one is driven; a stale cross-axis offset would leave
            // the run scrolled with no way to scroll it back.
            PipsPager pager = (PipsPager)d;
            pager.ResetScrollOffset();
            pager.UpdatePips();
        }

        private void OnPreviousButtonClick(object sender, RoutedEventArgs e)
        {
            // Coercion clamps at the first page.
            SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex - 1);
        }

        private void OnNextButtonClick(object sender, RoutedEventArgs e)
        {
            // Coercion clamps at the last page.
            SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex + 1);
        }

        private void OnPipClick(object sender, RoutedEventArgs e)
        {
            if (_pipsHost is null || sender is not System.Windows.Controls.Primitives.ToggleButton pip)
            {
                return;
            }

            int pageIndex = _pipsHost.Children.IndexOf(pip);
            if (pageIndex < 0)
            {
                return;
            }

            SetCurrentValue(SelectedPageIndexProperty, pageIndex);

            // Re-clicking the already selected pip toggles its IsChecked off without changing
            // SelectedPageIndex (no change callback fires), so re-assert the pip states.
            UpdatePips();
        }

        /// <summary>
        /// Rebuilds or refreshes the pip run, resizes the viewport, and re-runs the edge
        /// scroll and the navigation button states. The host is rebuilt only when the page
        /// count changed; otherwise the realized pips just refresh their checked state and the
        /// viewport animates to its new offset. Keyboard focus follows the selected pip
        /// whenever it was inside the host, so arrow-key and click interaction stay coherent.
        /// </summary>
        private void UpdatePips()
        {
            UpdateNavigationButtonStates();
            UpdateViewportSize();
            if (_pipsHost is null)
            {
                return;
            }

            bool keyboardFocusWasInside = _pipsHost.IsKeyboardFocusWithin;

            if (_pipsHost.Children.Count != NumberOfPages)
            {
                // A page-count change also moves the content extent, so the offset it lands on
                // has nothing to animate from; snap instead of tweening from stale geometry.
                RebuildPips();
                UpdateScrollOffset(animate: false);
            }
            else
            {
                RefreshPipStates();
                UpdateScrollOffset(animate: true);
            }

            if (keyboardFocusWasInside)
            {
                FocusSelectedPip();
            }
        }

        private void RebuildPips()
        {
            if (_pipsHost is null)
            {
                return;
            }

            // Pips are index-stable (a count change only grows or trims the tail), so adjust the
            // tail in place instead of tearing every pip down: a full clear re-created, re-styled,
            // and re-templated the whole run on any page-count delta.
            UIElementCollection pips = _pipsHost.Children;
            int pageCount = NumberOfPages;
            while (pips.Count > pageCount)
            {
                int lastIndex = pips.Count - 1;
                if (pips[lastIndex] is System.Windows.Controls.Primitives.ToggleButton stalePip)
                {
                    stalePip.Click -= OnPipClick;
                }

                pips.RemoveAt(lastIndex);
            }

            for (int pageIndex = pips.Count; pageIndex < pageCount; pageIndex++)
            {
                System.Windows.Controls.Primitives.ToggleButton pip = new();
                pip.SetResourceReference(StyleProperty, PipStyleKey);
                AutomationProperties.SetName(
                    pip,
                    string.Format(CultureInfo.InvariantCulture, "Page {0}", pageIndex + 1));
                pip.Click += OnPipClick;
                _ = pips.Add(pip);
            }

            RefreshPipStates();
        }

        private void RefreshPipStates()
        {
            if (_pipsHost is null)
            {
                return;
            }

            for (int pageIndex = 0; pageIndex < _pipsHost.Children.Count; pageIndex++)
            {
                if (_pipsHost.Children[pageIndex] is System.Windows.Controls.Primitives.ToggleButton pip)
                {
                    pip.IsChecked = pageIndex == SelectedPageIndex;
                }
            }
        }

        private void UnhookPips()
        {
            if (_pipsHost is null)
            {
                return;
            }

            for (int offset = 0; offset < _pipsHost.Children.Count; offset++)
            {
                if (_pipsHost.Children[offset] is System.Windows.Controls.Primitives.ToggleButton pip)
                {
                    pip.Click -= OnPipClick;
                }
            }
        }

        private void FocusSelectedPip()
        {
            if (_pipsHost is null)
            {
                return;
            }

            int pageIndex = SelectedPageIndex;
            if (pageIndex >= 0
                && pageIndex < _pipsHost.Children.Count
                && _pipsHost.Children[pageIndex] is System.Windows.Controls.Primitives.ToggleButton pip)
            {
                _ = pip.Focus();
            }
        }

        private void UpdateNavigationButtonStates()
        {
            _ = _previousButton?.IsEnabled = NumberOfPages > 0 && SelectedPageIndex > 0;
            _ = _nextButton?.IsEnabled = NumberOfPages > 0 && SelectedPageIndex < NumberOfPages - 1;
        }

        /// <summary>
        /// Backs the animated scroll offset of the pip viewport. <see cref="ScrollViewer"/>
        /// exposes its offsets read-only, so they cannot be animation targets directly; the
        /// pager animates this property instead and pushes each value into the viewport from
        /// the change callback (the <see cref="SmoothScrollViewer"/> precedent).
        /// </summary>
        private static readonly DependencyProperty ScrollOffsetProperty =
            DependencyProperty.Register(
                "ScrollOffset",
                typeof(double),
                typeof(PipsPager),
                new PropertyMetadata(0.0, OnScrollOffsetChanged));

        private static void OnScrollOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PipsPager)d).ApplyScrollOffset((double)e.NewValue);
        }

        /// <summary>
        /// WinUI's <c language="cpp">PipsPager::CalculateScrollViewerSize</c>: the viewport spans the pages it
        /// can show, sized as every pip but the selected one at the rest box size plus one
        /// selected box. This template uses a single square box for both states, so both
        /// arguments arrive as <see cref="PipBoxSize"/>; keeping them separate means a future
        /// per-state pip box slots straight into the same formula.
        /// </summary>
        /// <param name="defaultPipSize">Length of an unselected pip box along the axis.</param>
        /// <param name="selectedPipSize">Length of the selected pip box along the axis.</param>
        /// <param name="numberOfPages">Total pages, and so the total pip count.</param>
        /// <param name="maxVisiblePips">Most pips the viewport may show at once.</param>
        /// <returns>The viewport length along the orientation axis, or 0 when nothing shows.</returns>
        private static double CalculateViewportExtent(
            double defaultPipSize,
            double selectedPipSize,
            int numberOfPages,
            int maxVisiblePips)
        {
            if (numberOfPages <= 0 || maxVisiblePips <= 0)
            {
                return 0.0;
            }

            int pipsToDisplay = Math.Min(maxVisiblePips, numberOfPages);
            return (defaultPipSize * (pipsToDisplay - 1)) + selectedPipSize;
        }

        private void OnPipsBringIntoViewRequested(object sender, RequestBringIntoViewEventArgs e)
        {
            // Focusing a pip makes WPF ask the enclosing viewport to scroll that pip into view,
            // which would both jump past the pager's own animation and re-align the run in a way
            // the edge-scroll never asked for. The pager owns the offset outright, and it has
            // already put the selection in view. WinUI suppresses the same request in
            // PipsPager::OnScrollViewerBringIntoViewRequested. Handling it on the host panel
            // rather than on the viewer matters: ScrollViewer services the request from a class
            // handler, which runs before any instance handler attached to the viewer itself.
            e.Handled = true;

            // Suppressing the request must not cost the pager its own visibility: WinUI re-raises
            // for the ancestors (PipsPager::OnScrollViewerBringIntoViewRequested asks the viewer
            // itself to come into view) so an app ScrollViewer further out still brings the whole
            // pager on screen when a pip takes focus. The new request originates at the viewer, so
            // it routes away from PART_PipsHost and cannot re-enter this handler, and the viewer's
            // own class handler ignores a request whose target is the viewer.
            _pipsScrollViewer?.BringIntoView();
        }

        private void OnPipsScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Rebuilding the pips or flipping orientation moves the extent and the viewport, and
            // the viewer clamps its own offset against the geometry it had at the time. Recompute
            // the edge scroll once the new geometry is in, so the target itself is re-clamped
            // against the realized pip size and the viewport the viewer actually got.
            if (e.ExtentWidthChange is not 0
                || e.ExtentHeightChange is not 0
                || e.ViewportWidthChange is not 0
                || e.ViewportHeightChange is not 0)
            {
                UpdateScrollOffset(animate: false);
                return;
            }

            // An offset-only change the pager did not write is external input (see
            // _lastAppliedScrollOffset); snap the viewport back to the pager-owned target.
            double actualOffset = Orientation is Orientation.Horizontal ? e.HorizontalOffset : e.VerticalOffset;
            if (Math.Abs(actualOffset - _lastAppliedScrollOffset) > 0.5)
            {
                ApplyScrollOffset(_targetScrollOffset);
            }
        }

        /// <summary>
        /// Clamps the viewport to <see cref="MaxVisiblePips"/> pip boxes along the orientation
        /// axis and frees the cross axis, so a flip cannot leave the previous axis pinned.
        /// </summary>
        private void UpdateViewportSize()
        {
            if (_pipsScrollViewer is null)
            {
                return;
            }

            double pipExtent = GetPipBoxExtent();
            double extent = CalculateViewportExtent(pipExtent, pipExtent, NumberOfPages, MaxVisiblePips);
            if (Orientation is Orientation.Horizontal)
            {
                _pipsScrollViewer.MaxWidth = extent;
                _pipsScrollViewer.MaxHeight = double.PositiveInfinity;
            }
            else
            {
                _pipsScrollViewer.MaxHeight = extent;
                _pipsScrollViewer.MaxWidth = double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Moves the viewport the minimum distance that brings the selected pip back inside it,
        /// which is none at all while the selection is already in view. This is WinUI's
        /// edge-scrolling model: the run of pips stays still under a moving selection and only
        /// slides once the selection would leave the viewport, so the pips do not re-center
        /// under the pointer on every step.
        /// </summary>
        /// <param name="animate">
        /// True to tween to the new offset, false to snap. Structural changes snap, because the
        /// offset they land on has no meaningful geometry to travel from.
        /// </param>
        private void UpdateScrollOffset(bool animate)
        {
            if (_pipsScrollViewer is null || NumberOfPages <= 0)
            {
                return;
            }

            // Prefer the realized geometry: the pip's arranged box (a consumer may restyle
            // PipsPagerPipStyle away from the 20px default) and the viewport the viewer actually
            // got (a parent can arrange the pager narrower than MaxVisiblePips boxes). The
            // theoretical values are the fallback for the passes that run before first arrange.
            double pipExtent = GetPipBoxExtent();
            double viewport = Orientation is Orientation.Horizontal
                ? _pipsScrollViewer.ViewportWidth
                : _pipsScrollViewer.ViewportHeight;
            if (viewport <= 0.0)
            {
                viewport = CalculateViewportExtent(pipExtent, pipExtent, NumberOfPages, MaxVisiblePips);
            }

            double contentExtent = Orientation is Orientation.Horizontal
                ? _pipsScrollViewer.ExtentWidth
                : _pipsScrollViewer.ExtentHeight;
            if (contentExtent <= 0.0)
            {
                contentExtent = NumberOfPages * pipExtent;
            }

            double maxOffset = Math.Max(0.0, contentExtent - viewport);
            double pipStart = SelectedPageIndex * pipExtent;
            double pipEnd = pipStart + pipExtent;

            double offset = Clamp(_targetScrollOffset, 0.0, maxOffset);
            if (pipStart < offset)
            {
                offset = pipStart;
            }
            else if (pipEnd > offset + viewport)
            {
                offset = pipEnd - viewport;
            }

            offset = Clamp(offset, 0.0, maxOffset);
            bool offsetMoved = Math.Abs(offset - _targetScrollOffset) > 0.01;
            _targetScrollOffset = offset;

            // A selection that stayed inside the viewport leaves the offset alone: restarting an
            // animation toward the offset the viewport already holds would be pure churn.
            if (offsetMoved || !animate)
            {
                AnimateScrollOffsetTo(offset, animate);
            }
        }

        private void AnimateScrollOffsetTo(double offset, bool animate)
        {
            if (!animate || !MotionHelper.IsMotionEnabled)
            {
                // Release any in-flight tween first: an animation holding its end value would
                // otherwise outrank the local value this sets. ApplyScrollOffset then runs
                // unconditionally, because an unchanged property value raises no callback and the
                // viewport may still have reset its own offset behind a geometry change.
                BeginAnimation(ScrollOffsetProperty, animation: null);
                SetCurrentValue(ScrollOffsetProperty, offset);
                ApplyScrollOffset(offset);
                return;
            }

            // ControlFastAnimationDuration on ControlFastOutSlowInKeySpline (0.8,0,0,1), the
            // motion tokens the pip size morph in PipsPager.xaml already rides.
            DoubleAnimationUsingKeyFrames animation = new()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(ScrollAnimationMilliseconds)),
                KeyFrames =
                {
                    new SplineDoubleKeyFrame(
                        offset,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ScrollAnimationMilliseconds)),
                        new KeySpline(0.8, 0.0, 0.0, 1.0)),
                },
            };
            BeginAnimation(ScrollOffsetProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Returns the realized length of one pip box along the orientation axis, falling back to
        /// the template's theoretical <see cref="PipBoxSize"/> until the first pip is arranged.
        /// </summary>
        private double GetPipBoxExtent()
        {
            if (_pipsHost?.Children.Count > 0 && _pipsHost.Children[0] is UIElement pip)
            {
                double extent = Orientation is Orientation.Horizontal
                    ? pip.RenderSize.Width
                    : pip.RenderSize.Height;
                if (extent > 0.0)
                {
                    return extent;
                }
            }

            return PipBoxSize;
        }

        private void ApplyScrollOffset(double offset)
        {
            if (_pipsScrollViewer is null)
            {
                return;
            }

            _lastAppliedScrollOffset = offset;
            if (Orientation is Orientation.Horizontal)
            {
                _pipsScrollViewer.ScrollToHorizontalOffset(offset);
            }
            else
            {
                _pipsScrollViewer.ScrollToVerticalOffset(offset);
            }
        }

        private void ResetScrollOffset()
        {
            if (_pipsScrollViewer is not null)
            {
                _pipsScrollViewer.ScrollToHorizontalOffset(0.0);
                _pipsScrollViewer.ScrollToVerticalOffset(0.0);
            }

            BeginAnimation(ScrollOffsetProperty, animation: null);
            SetCurrentValue(ScrollOffsetProperty, 0.0);
            _targetScrollOffset = 0.0;
            _lastAppliedScrollOffset = 0.0;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
