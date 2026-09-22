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
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Test seam over <see cref="NativeMethods.GetDisplayColorDepth"/>, mirroring
    /// <see cref="MotionHelper.OverrideIsMotionEnabled"/>: a live desktop's display path cannot be
    /// forced into a 10-bits-per-channel, advanced-color-off state from a unit test, so
    /// <see cref="Controls.FluenceWindow"/> reads the depth through this indirection instead of calling
    /// <see cref="NativeMethods"/> directly.
    /// </summary>
    internal static class DisplayDepthProbe
    {
        /// <summary>
        /// Gets or sets the test seam. When non-<see langword="null"/>, overrides the live
        /// <see cref="NativeMethods.GetDisplayColorDepth"/> probe. Tests must reset this to
        /// <see langword="null"/> in a <see langword="finally"/> block.
        /// </summary>
        internal static Func<IntPtr, DisplayColorDepth>? Override { get; set; }

        /// <summary>
        /// Returns the display color depth for the monitor hosting <paramref name="hwnd"/>, via
        /// <see cref="Override"/> when set, otherwise via
        /// <see cref="NativeMethods.GetDisplayColorDepth"/>.
        /// </summary>
        /// <param name="hwnd">The window handle whose monitor is probed.</param>
        /// <returns>The resolved <see cref="DisplayColorDepth"/>.</returns>
        internal static DisplayColorDepth GetColorDepth(IntPtr hwnd)
        {
            return Override?.Invoke(hwnd) ?? NativeMethods.GetDisplayColorDepth(hwnd);
        }
    }
}
