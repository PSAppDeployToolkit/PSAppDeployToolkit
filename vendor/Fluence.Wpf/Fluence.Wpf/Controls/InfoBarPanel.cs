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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// The panel that lays out an <see cref="InfoBar"/> title, message and action button, WinUI's
    /// own <c language="csharp">InfoBarPanel</c> (InfoBarPanel.cpp).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The panel chooses its orientation from what fits. When every child fits on one line it lays
    /// them out horizontally, which is the single line bar the WinUI Gallery shows by default; when
    /// they do not, or when there is only one of them, or when a child in a horizontal row would be
    /// taller than the bar's own minimum height, it stacks them instead.
    /// </para>
    /// <para>
    /// Each child carries two margins rather than one, because the spacing WinUI wants between a
    /// title and a message side by side is not the spacing it wants between them stacked. The panel
    /// applies whichever matches the orientation it settled on, and it drops the leading margin of
    /// the first child and the trailing margin of the last, so the outer inset stays the panel's own
    /// padding.
    /// </para>
    /// </remarks>
    internal sealed class InfoBarPanel : Panel
    {
        /// <summary>
        /// Identifies the HorizontalOrientationMargin attached property, the margin a child takes
        /// while the panel is laying its children out on one line.
        /// </summary>
        public static readonly DependencyProperty HorizontalOrientationMarginProperty =
            DependencyProperty.RegisterAttached(
                "HorizontalOrientationMargin",
                typeof(Thickness),
                typeof(InfoBarPanel),
                new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Identifies the VerticalOrientationMargin attached property, the margin a child takes
        /// while the panel is stacking its children.
        /// </summary>
        public static readonly DependencyProperty VerticalOrientationMarginProperty =
            DependencyProperty.RegisterAttached(
                "VerticalOrientationMargin",
                typeof(Thickness),
                typeof(InfoBarPanel),
                new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Identifies the <see cref="HorizontalOrientationPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalOrientationPaddingProperty =
            DependencyProperty.Register(
                nameof(HorizontalOrientationPadding),
                typeof(Thickness),
                typeof(InfoBarPanel),
                new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <see cref="VerticalOrientationPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalOrientationPaddingProperty =
            DependencyProperty.Register(
                nameof(VerticalOrientationPadding),
                typeof(Thickness),
                typeof(InfoBarPanel),
                new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Gets or sets the padding applied around the children while they sit on one line.
        /// </summary>
        public Thickness HorizontalOrientationPadding
        {
            get => (Thickness)GetValue(HorizontalOrientationPaddingProperty);
            set => SetValue(HorizontalOrientationPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the padding applied around the children while they are stacked.
        /// </summary>
        public Thickness VerticalOrientationPadding
        {
            get => (Thickness)GetValue(VerticalOrientationPaddingProperty);
            set => SetValue(VerticalOrientationPaddingProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the panel settled on stacking its children rather than
        /// putting them on one line. Valid after a measure pass.
        /// </summary>
        internal bool IsVertical { get; private set; }

        /// <summary>
        /// Gets the margin <paramref name="element"/> takes while the panel lays out on one line.
        /// </summary>
        /// <param name="element">The child to read the margin from.</param>
        /// <returns>The horizontal orientation margin.</returns>
        public static Thickness GetHorizontalOrientationMargin(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (Thickness)element.GetValue(HorizontalOrientationMarginProperty);
        }

        /// <summary>
        /// Sets the margin <paramref name="element"/> takes while the panel lays out on one line.
        /// </summary>
        /// <param name="element">The child to set the margin on.</param>
        /// <param name="value">The margin to apply.</param>
        public static void SetHorizontalOrientationMargin(DependencyObject element, Thickness value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(HorizontalOrientationMarginProperty, value);
        }

        /// <summary>
        /// Gets the margin <paramref name="element"/> takes while the panel stacks its children.
        /// </summary>
        /// <param name="element">The child to read the margin from.</param>
        /// <returns>The vertical orientation margin.</returns>
        public static Thickness GetVerticalOrientationMargin(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (Thickness)element.GetValue(VerticalOrientationMarginProperty);
        }

        /// <summary>
        /// Sets the margin <paramref name="element"/> takes while the panel stacks its children.
        /// </summary>
        /// <param name="element">The child to set the margin on.</param>
        /// <param name="value">The margin to apply.</param>
        public static void SetVerticalOrientationMargin(DependencyObject element, Thickness value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(VerticalOrientationMarginProperty, value);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            double totalWidth = 0;
            double totalHeight = 0;
            double widthOfWidest = 0;
            double heightOfTallest = 0;
            double heightOfTallestInHorizontal = 0;
            int measuredChildren = 0;

            // The bar's own minimum height is the test for "this child is really wrapping": a title
            // and a message can each fit the width and still stand taller than one line once the
            // text wraps inside them.
            double minHeight = Parent is FrameworkElement parent
                ? parent.MinHeight - (Margin.Top + Margin.Bottom)
                : 0;

            int childCount = InternalChildren.Count;
            for (int index = 0; index < childCount; index++)
            {
                UIElement child = InternalChildren[index];
                child.Measure(availableSize);
                Size desired = child.DesiredSize;
                if (desired.Width is 0 || desired.Height is 0)
                {
                    continue;
                }

                Thickness horizontalMargin = GetHorizontalOrientationMargin(child);
                totalWidth += desired.Width
                    + (measuredChildren > 0 ? horizontalMargin.Left : 0)
                    + (index < childCount - 1 ? horizontalMargin.Right : 0);

                Thickness verticalMargin = GetVerticalOrientationMargin(child);
                totalHeight += desired.Height
                    + (measuredChildren > 0 ? verticalMargin.Top : 0)
                    + (index < childCount - 1 ? verticalMargin.Bottom : 0);

                widthOfWidest = Math.Max(widthOfWidest, desired.Width);
                heightOfTallest = Math.Max(heightOfTallest, desired.Height);
                heightOfTallestInHorizontal = Math.Max(
                    heightOfTallestInHorizontal,
                    desired.Height + horizontalMargin.Top + horizontalMargin.Bottom);

                measuredChildren++;
            }

            // A lone child is stacked rather than laid out on one line: the vertical margins are the
            // ones that give it the right inset from the bar's edges.
            IsVertical = measuredChildren is 1
                || totalWidth > availableSize.Width
                || (minHeight > 0 && heightOfTallestInHorizontal > minHeight);

            if (IsVertical)
            {
                Thickness verticalPadding = VerticalOrientationPadding;
                return new Size(
                    widthOfWidest + verticalPadding.Left + verticalPadding.Right,
                    totalHeight + verticalPadding.Top + verticalPadding.Bottom);
            }

            Thickness horizontalPadding = HorizontalOrientationPadding;
            return new Size(
                totalWidth + horizontalPadding.Left + horizontalPadding.Right,
                heightOfTallest + horizontalPadding.Top + horizontalPadding.Bottom);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (IsVertical)
            {
                ArrangeVertical();
            }
            else
            {
                ArrangeHorizontal(finalSize);
            }

            return finalSize;
        }

        private void ArrangeVertical()
        {
            Thickness padding = VerticalOrientationPadding;
            double offset = padding.Top;
            bool hasPrevious = false;

            foreach (UIElement child in InternalChildren)
            {
                Size desired = child.DesiredSize;
                if (desired.Width is 0 || desired.Height is 0)
                {
                    continue;
                }

                Thickness margin = GetVerticalOrientationMargin(child);
                offset += hasPrevious ? margin.Top : 0;
                child.Arrange(new Rect(padding.Left + margin.Left, offset, desired.Width, desired.Height));
                offset += desired.Height + margin.Bottom;
                hasPrevious = true;
            }
        }

        private void ArrangeHorizontal(Size finalSize)
        {
            Thickness padding = HorizontalOrientationPadding;
            double offset = padding.Left;
            bool hasPrevious = false;
            int childCount = InternalChildren.Count;

            for (int index = 0; index < childCount; index++)
            {
                UIElement child = InternalChildren[index];
                Size desired = child.DesiredSize;
                if (desired.Width is 0 || desired.Height is 0)
                {
                    continue;
                }

                Thickness margin = GetHorizontalOrientationMargin(child);
                offset += hasPrevious ? margin.Left : 0;

                // The last child takes whatever width is left, so a long message fills the bar
                // rather than leaving a gap before the action button.
                double width = index < childCount - 1
                    ? desired.Width
                    : Math.Max(desired.Width, finalSize.Width - offset);

                child.Arrange(new Rect(offset, padding.Top + margin.Top, width, desired.Height));
                offset += desired.Width + margin.Right;
                hasPrevious = true;
            }
        }
    }
}
