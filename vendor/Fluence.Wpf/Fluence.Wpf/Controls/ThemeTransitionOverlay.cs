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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Keeps a short-lived picture of a window's previous WPF surface over its live template.
    /// The computed theme dictionary remains frozen and is published normally underneath it.
    /// The picture is also the currently visible composite when another change interrupts a fade.
    /// </summary>
    internal sealed class ThemeTransitionOverlay
    {
        private const int DurationMilliseconds = 167;
        private const long MaximumPixels = 16_000_000;

        private readonly Grid _root;
        private readonly Grid _host;
        private readonly FluenceWindow _window;
        private SnapshotVisual? _snapshot;
        private bool _pending;
        private int _generation;

        internal ThemeTransitionOverlay(FluenceWindow window, Grid root, Grid host)
        {
            _window = window;
            _root = root;
            _host = host;
        }

        /// <summary>
        /// Prepares a capture of the previous WPF surface before resource publication.
        /// </summary>
        /// <param name="previousTheme">The theme currently visible in the window.</param>
        /// <param name="nextTheme">The theme about to be published.</param>
        internal void Prepare(ApplicationTheme previousTheme, ApplicationTheme nextTheme)
        {
            _pending = false;
            if (!_window.Dispatcher.CheckAccess())
            {
                return;
            }

            if (!_window.IsVisible || _window.WindowState is WindowState.Minimized ||
                !MotionHelper.IsMotionEnabled || previousTheme is ApplicationTheme.HighContrast ||
                nextTheme is ApplicationTheme.HighContrast)
            {
                Clear();
                return;
            }

            _pending = Capture();
        }

        /// <summary>
        /// Captures the currently visible surface before resource publication. Rendering the root
        /// while an earlier overlay is still attached includes its in-progress blend, so repeated
        /// theme changes do not jump back to an obsolete endpoint.
        /// </summary>
        /// <returns>Whether a usable picture was attached.</returns>
        private bool Capture()
        {
            if (_root.ActualWidth <= 0 || _root.ActualHeight <= 0)
            {
                Clear();
                return false;
            }

            Matrix scale = PresentationSource.FromVisual(_root)?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            double pixelWidth = Math.Ceiling(_root.ActualWidth * scale.M11);
            double pixelHeight = Math.Ceiling(_root.ActualHeight * scale.M22);
            if (pixelWidth < 1 || pixelHeight < 1 || pixelWidth > int.MaxValue || pixelHeight > int.MaxValue ||
                pixelWidth * pixelHeight > MaximumPixels)
            {
                Clear();
                return false;
            }

            try
            {
                RenderTargetBitmap bitmap = new((int)pixelWidth, (int)pixelHeight,
                    96 * scale.M11, 96 * scale.M22, PixelFormats.Pbgra32);
                // The outer WindowBorder owns the background, while the named grid owns the
                // chrome and content. Keep the brush viewbox and viewport in the same local
                // coordinates to preserve their placement during capture.
                DrawingVisual surface = new();
                using (DrawingContext drawing = surface.RenderOpen())
                {
                    Rect bounds = new(0, 0, _root.ActualWidth, _root.ActualHeight);
                    VisualBrush content = new(_root)
                    {
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Viewbox = bounds,
                        ViewportUnits = BrushMappingMode.Absolute,
                        Viewport = bounds,
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                    };
                    drawing.DrawRectangle(_window.Background, pen: null, bounds);
                    drawing.DrawRectangle(content, pen: null, bounds);
                }
                bitmap.Render(surface);
                bitmap.Freeze();

                Clear();
                SnapshotVisual snapshot = new(bitmap)
                {
                    Width = _root.ActualWidth,
                    Height = _root.ActualHeight,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    IsHitTestVisible = false,
                };
                _ = _host.Children.Add(snapshot);
                _snapshot = snapshot;
                // A second theme or accent apply can run in the same dispatcher callback before
                // WPF's next render pass. Arrange the snapshot now so that the next capture sees
                // the whole previous surface, including its opaque background, rather than only
                // the newly published background behind unarranged old controls.
                _host.UpdateLayout();
                return true;
            }
            catch (InvalidOperationException)
            {
                Clear();
                return false;
            }
            catch (ArgumentException)
            {
                Clear();
                return false;
            }
        }

        /// <summary>
        /// Starts the fade after synchronous theme and backdrop event handlers have completed.
        /// </summary>
        internal void Play()
        {
            if (!_pending)
            {
                return;
            }

            _pending = false;
            int generation = _generation;
            _ = _root.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _generation || _snapshot is null)
                {
                    return;
                }

                SnapshotVisual snapshot = _snapshot;
                DoubleAnimation fade = new(1, 0, TimeSpan.FromMilliseconds(DurationMilliseconds))
                {
                    FillBehavior = FillBehavior.Stop,
                };
                fade.Completed += (_, _) =>
                {
                    if (ReferenceEquals(_snapshot, snapshot))
                    {
                        Clear();
                    }
                };
                snapshot.BeginAnimation(UIElement.OpacityProperty, fade);
            }), DispatcherPriority.Render);
        }

        /// <summary>
        /// Releases the snapshot, including when a window closes or changes template.
        /// </summary>
        internal void Clear()
        {
            _pending = false;
            _generation++;
            if (_snapshot is not null)
            {
                _snapshot.BeginAnimation(UIElement.OpacityProperty, animation: null);
                _host.Children.Remove(_snapshot);
                _snapshot = null;
            }
        }

        /// <summary>
        /// A decorative drawing surface with no automation peer. The transition therefore never
        /// duplicates window content in the accessibility tree or takes keyboard focus.
        /// </summary>
        private sealed class SnapshotVisual : FrameworkElement
        {
            private readonly BitmapSource _bitmap;

            internal SnapshotVisual(BitmapSource bitmap)
            {
                _bitmap = bitmap;
            }

            /// <inheritdoc />
            protected override void OnRender(DrawingContext drawingContext)
            {
                drawingContext.DrawImage(_bitmap, new Rect(RenderSize));
            }
        }
    }
}
