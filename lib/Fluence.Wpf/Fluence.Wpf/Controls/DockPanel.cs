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
    /// Dock panel with uniform spacing between consecutive docked children.
    /// </summary>
    public class DockPanel : Panel
    {
        /// <summary>
        /// Identifies the <see cref="Spacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(DockPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Gets or sets the uniform spacing between consecutive docked children.
        /// </summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LastChildFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LastChildFillProperty =
            DependencyProperty.Register(
                nameof(LastChildFill),
                typeof(bool),
                typeof(DockPanel),
                new FrameworkPropertyMetadata(defaultValue: true, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Gets or sets whether the last child element stretches to fill the remaining space.
        /// </summary>
        public bool LastChildFill
        {
            get => (bool)GetValue(LastChildFillProperty);
            set => SetValue(LastChildFillProperty, value);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            if (count is 0)
            {
                return new(0, 0);
            }

            Size available = availableSize;
            bool lastFill = LastChildFill;
            int lastIndex = lastFill ? count - 1 : count;
            double maxWidth = 0;
            double maxHeight = 0;
            double accumulatedWidth = 0;
            double accumulatedHeight = 0;
            for (int i = 0; i < lastIndex; i++)
            {
                UIElement child = children[i];
                if (child is null)
                {
                    continue;
                }

                child.Measure(available);
                Size desired = child.DesiredSize;
                switch (System.Windows.Controls.DockPanel.GetDock(child))
                {
                    case Dock.Left:
                    case Dock.Right:
                        maxHeight = Math.Max(maxHeight, accumulatedHeight + desired.Height);
                        accumulatedWidth += desired.Width;
                        available = new Size(Math.Max(0, available.Width - desired.Width - Spacing), available.Height);
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        maxWidth = Math.Max(maxWidth, accumulatedWidth + desired.Width);
                        accumulatedHeight += desired.Height;
                        available = new Size(available.Width, Math.Max(0, available.Height - desired.Height - Spacing));
                        break;
                    default:
                        continue;
                }
            }
            maxWidth = Math.Max(maxWidth, accumulatedWidth);
            maxHeight = Math.Max(maxHeight, accumulatedHeight);
            if (lastFill && count > 0)
            {
                UIElement child = children[count - 1];
                if (child is not null)
                {
                    child.Measure(available);
                    Size desired = child.DesiredSize;
                    maxWidth = Math.Max(maxWidth, accumulatedWidth + desired.Width);
                    maxHeight = Math.Max(maxHeight, accumulatedHeight + desired.Height);
                }
            }
            return new(maxWidth, maxHeight);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElementCollection children = InternalChildren;
            int count = children.Count;
            if (count is 0)
            {
                return finalSize;
            }

            Rect remaining = new(0, 0, finalSize.Width, finalSize.Height);
            bool lastFill = LastChildFill;
            int lastIndex = lastFill ? count - 1 : count;
            for (int i = 0; i < lastIndex; i++)
            {
                UIElement child = children[i];
                if (child is null)
                {
                    continue;
                }

                Dock dock = System.Windows.Controls.DockPanel.GetDock(child);
                Size desired = child.DesiredSize;
                switch (dock)
                {
                    case Dock.Left:
                        {
                            double width = Math.Min(desired.Width, remaining.Width);
                            child.Arrange(new Rect(remaining.Left, remaining.Top, width, remaining.Height));
                            remaining.X += width + Spacing;
                            remaining.Width = Math.Max(0, remaining.Width - width - Spacing);
                            break;
                        }
                    case Dock.Right:
                        {
                            double width = Math.Min(desired.Width, remaining.Width);
                            child.Arrange(new Rect(remaining.Right - width, remaining.Top, width, remaining.Height));
                            remaining.Width = Math.Max(0, remaining.Width - width - Spacing);
                            break;
                        }
                    case Dock.Top:
                        {
                            double height = Math.Min(desired.Height, remaining.Height);
                            child.Arrange(new Rect(remaining.Left, remaining.Top, remaining.Width, height));
                            remaining.Y += height + Spacing;
                            remaining.Height = Math.Max(0, remaining.Height - height - Spacing);
                            break;
                        }
                    case Dock.Bottom:
                        {
                            double height = Math.Min(desired.Height, remaining.Height);
                            child.Arrange(new Rect(remaining.Left, remaining.Bottom - height, remaining.Width, height));
                            remaining.Height = Math.Max(0, remaining.Height - height - Spacing);
                            break;
                        }
                    default:
                        continue;
                }
            }
            if (lastFill && count > 0)
            {
                UIElement child = children[count - 1];
                child?.Arrange(remaining);
            }
            return finalSize;
        }
    }
}
