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
using System.Windows.Media.Imaging;

namespace Fluence.Wpf.Helpers
{
    internal static class AcrylicNoiseHelper
    {
        internal static ImageBrush GetNoiseBrush()
        {
            if (_cachedBrush is not null)
            {
                return _cachedBrush;
            }

            const int size = 128;
            const int stride = size * 4;
            byte[] pixels = new byte[size * stride];
            Random rng = new(42);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte gray = (byte)rng.Next(0, 256);
                pixels[i] = gray; // B
                pixels[i + 1] = gray; // G
                pixels[i + 2] = gray; // R
                pixels[i + 3] = 12;   // A  (~5 % opacity per pixel)
            }
            BitmapSource bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            bitmap.Freeze();

            ImageBrush brush = new(bitmap)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, size, size),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };
            brush.Freeze();
            _cachedBrush = brush;
            return _cachedBrush;
        }

        internal static void ResetForTesting()
        {
            _cachedBrush = null;
        }

        /// <summary>
        /// Stores a cached instance of an ImageBrush for reuse.
        /// </summary>
        /// <remarks>This field is intended to improve performance by avoiding repeated creation of
        /// ImageBrush instances. It may be null if the brush has not yet been initialized or has been
        /// cleared.</remarks>
        private static ImageBrush? _cachedBrush;
    }
}
