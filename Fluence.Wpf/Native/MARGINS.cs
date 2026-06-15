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

using System.Runtime.InteropServices;

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// The Win32 <c>MARGINS</c> structure passed to <c>DwmExtendFrameIntoClientArea</c>. A value of
    /// <c>-1</c> on every edge requests the "sheet of glass" full-frame extension.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MARGINS
    {
        /// <summary>
        /// Width of the left border that retains its size.
        /// </summary>
        public int cxLeftWidth;

        /// <summary>
        /// Width of the right border that retains its size.
        /// </summary>
        public int cxRightWidth;

        /// <summary>
        /// Height of the top border that retains its size.
        /// </summary>
        public int cyTopHeight;

        /// <summary>
        /// Height of the bottom border that retains its size.
        /// </summary>
        public int cyBottomHeight;

        /// <summary>
        /// Initializes the four margins independently.
        /// </summary>
        /// <param name="left">The left margin width.</param>
        /// <param name="right">The right margin width.</param>
        /// <param name="top">The top margin height.</param>
        /// <param name="bottom">The bottom margin height.</param>
        public MARGINS(int left, int right, int top, int bottom)
        {
            cxLeftWidth = left;
            cxRightWidth = right;
            cyTopHeight = top;
            cyBottomHeight = bottom;
        }

        /// <summary>
        /// Initializes all four margins to the same value.
        /// </summary>
        /// <param name="allMargins">The value applied to every edge.</param>
        public MARGINS(int allMargins)
        {
            cxLeftWidth = allMargins;
            cxRightWidth = allMargins;
            cyTopHeight = allMargins;
            cyBottomHeight = allMargins;
        }
    }
}
