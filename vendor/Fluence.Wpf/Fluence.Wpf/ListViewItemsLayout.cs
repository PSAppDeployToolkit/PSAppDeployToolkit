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

namespace Fluence.Wpf
{
    /// <summary>
    /// Specifies how a <see cref="Controls.ListView"/> arranges its items: one per row down the
    /// list, or wrapped across it as a grid of tiles.
    /// </summary>
    /// <remarks>
    /// This is a Fluence property with no WinUI counterpart, because WinUI ships the two arrangements
    /// as separate controls (<c language="text">ListView</c> and <c language="text">GridView</c>).
    /// It is deliberately not named after WinUI's <c language="text">GridView</c>: in WPF,
    /// <see cref="System.Windows.Controls.ListView.View"/> already takes a
    /// <see cref="System.Windows.Controls.GridView"/>, and that one means a column view.
    /// </remarks>
    public enum ListViewItemsLayout
    {
        /// <summary>
        /// One item per row, in WPF's own virtualizing vertical panel. The default.
        /// </summary>
        List = 0,

        /// <summary>
        /// Items wrap across the list as tiles, the way WinUI's <c language="text">GridView</c>
        /// lays them out with its <c language="text">ItemsWrapGrid</c>.
        /// </summary>
        Grid = 1,
    }
}
