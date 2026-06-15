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
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

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
    /// A page indicator mirroring the WinUI 3 <c>PipsPager</c>: a horizontal or vertical run
    /// of round pip dots, one per visible page, with the selected pip rendered larger in the
    /// accent fill. Clicking a pip selects its page, optional previous/next chevron buttons
    /// step the selection, and arrow keys move the selection while keyboard focus is inside
    /// the pager.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pips are generated in code into the <c>PART_PipsHost</c> panel (the same approach as
    /// <see cref="RatingControl"/>). When <see cref="NumberOfPages"/> exceeds
    /// <see cref="MaxVisiblePips"/>, the pager shows a sliding window of
    /// <see cref="MaxVisiblePips"/> pips centered on the selection where possible and clamped
    /// at the ends. Subscribe to <see cref="SelectedIndexChanged"/> to react to selection
    /// moves from any input path.
    /// </para>
    /// <para>
    /// Two WinUI behaviors are deliberate v1 omissions: the scale-down animation of edge pips
    /// inside the scrolling viewport (the window simply re-renders), and WinUI's
    /// edge-scrolling window (which keeps the window still until the selection reaches its
    /// edge) in favor of the simpler centered window. Navigation buttons in
    /// <see cref="PipsPagerButtonVisibility.VisibleOnPointerOver"/> mode collapse when the
    /// pointer leaves, so the pager's desired size changes with hover.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_PreviousButton, Type = typeof(System.Windows.Controls.Primitives.ButtonBase))]
    [TemplatePart(Name = PART_NextButton, Type = typeof(System.Windows.Controls.Primitives.ButtonBase))]
    [TemplatePart(Name = PART_PipsHost, Type = typeof(Panel))]
    public class PipsPager : Control
    {
        // Template part names. These must match the names used in the default control template.
        private const string PART_PreviousButton = "PART_PreviousButton";
        private const string PART_NextButton = "PART_NextButton";
        private const string PART_PipsHost = "PART_PipsHost";

        // Resource key of the ToggleButton style applied to every generated pip.
        private const string PipStyleKey = "PipsPagerPipStyle";

        private System.Windows.Controls.Primitives.ButtonBase? _previousButton;
        private System.Windows.Controls.Primitives.ButtonBase? _nextButton;
        private Panel? _pipsHost;

        // Page index of the first pip currently realized in PART_PipsHost; -1 until built.
        private int _windowStart = -1;

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
        /// Gets or sets the maximum number of pips realized at once. When
        /// <see cref="NumberOfPages"/> exceeds this count, the pager shows a sliding window
        /// of this many pips centered on the selection where possible. Values below 1 coerce
        /// to 1. Default is 5, matching WinUI.
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
                new FrameworkPropertyMetadata(Orientation.Horizontal));

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

            _previousButton = GetTemplateChild(PART_PreviousButton) as System.Windows.Controls.Primitives.ButtonBase;
            _nextButton = GetTemplateChild(PART_NextButton) as System.Windows.Controls.Primitives.ButtonBase;
            _pipsHost = GetTemplateChild(PART_PipsHost) as Panel;
            _windowStart = -1;

            _previousButton?.Click += OnPreviousButtonClick;
            _nextButton?.Click += OnNextButtonClick;

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

            int offset = _pipsHost.Children.IndexOf(pip);
            if (offset < 0)
            {
                return;
            }

            SetCurrentValue(SelectedPageIndexProperty, _windowStart + offset);

            // Re-clicking the already selected pip toggles its IsChecked off without changing
            // SelectedPageIndex (no change callback fires), so re-assert the pip states.
            UpdatePips();
        }

        /// <summary>
        /// Recomputes the visible pip window and the navigation button states. The host is
        /// rebuilt only when the window moved or resized; otherwise the realized pips just
        /// refresh their checked state. Keyboard focus follows the selected pip whenever it
        /// was inside the host, so arrow-key and click interaction stay coherent across
        /// window rebuilds.
        /// </summary>
        private void UpdatePips()
        {
            UpdateNavigationButtonStates();
            if (_pipsHost is null)
            {
                return;
            }

            bool keyboardFocusWasInside = _pipsHost.IsKeyboardFocusWithin;

            int pageCount = NumberOfPages;
            int windowSize = Math.Min(MaxVisiblePips, pageCount);
            int start = 0;
            if (pageCount > windowSize)
            {
                // Center the window on the selection, clamped to the valid range.
                start = SelectedPageIndex - ((windowSize - 1) / 2);
                start = Math.Max(0, Math.Min(start, pageCount - windowSize));
            }

            if (_pipsHost.Children.Count != windowSize || _windowStart != start)
            {
                RebuildPips(start, windowSize);
            }
            else
            {
                RefreshPipStates();
            }

            if (keyboardFocusWasInside)
            {
                FocusSelectedPip();
            }
        }

        private void RebuildPips(int start, int windowSize)
        {
            if (_pipsHost is null)
            {
                return;
            }

            UnhookPips();
            _pipsHost.Children.Clear();
            _windowStart = start;

            for (int offset = 0; offset < windowSize; offset++)
            {
                int pageIndex = start + offset;
                System.Windows.Controls.Primitives.ToggleButton pip = new();
                pip.SetResourceReference(StyleProperty, PipStyleKey);
                AutomationProperties.SetName(
                    pip,
                    string.Format(CultureInfo.InvariantCulture, "Page {0}", pageIndex + 1));
                pip.Click += OnPipClick;
                _ = _pipsHost.Children.Add(pip);
            }

            RefreshPipStates();
        }

        private void RefreshPipStates()
        {
            if (_pipsHost is null)
            {
                return;
            }

            for (int offset = 0; offset < _pipsHost.Children.Count; offset++)
            {
                if (_pipsHost.Children[offset] is System.Windows.Controls.Primitives.ToggleButton pip)
                {
                    pip.IsChecked = _windowStart + offset == SelectedPageIndex;
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

            int offset = SelectedPageIndex - _windowStart;
            if (offset >= 0
                && offset < _pipsHost.Children.Count
                && _pipsHost.Children[offset] is System.Windows.Controls.Primitives.ToggleButton pip)
            {
                _ = pip.Focus();
            }
        }

        private void UpdateNavigationButtonStates()
        {
            _ = _previousButton?.IsEnabled = NumberOfPages > 0 && SelectedPageIndex > 0;
            _ = _nextButton?.IsEnabled = NumberOfPages > 0 && SelectedPageIndex < NumberOfPages - 1;
        }
    }
}
