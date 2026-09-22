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

using System.Windows.Controls.Primitives;
using Fluence.Wpf.Helpers;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// The one set of looping-column measurement and selection helpers shared by the
    /// <see cref="Controls.DatePicker"/>, <see cref="Controls.TimePicker"/> and
    /// <see cref="Controls.LoopingSelectorList"/> tests. Every test class brings these into scope
    /// with a using static Fluence.Wpf.Tests.Infrastructure.LoopingSelectorTestSupport directive so
    /// the call sites read exactly as they did when each partial carried its own private copy.
    /// </summary>
    internal static class LoopingSelectorTestSupport
    {
        /// <summary>
        /// The number of padding rows a looping column keeps above the selected row; the
        /// selected row is the middle one of a nine-row viewport. Aliases the control's own
        /// constant so the tests can never drift from the geometry the control actually uses.
        /// </summary>
        internal const int LoopingPaddingItemsCount = Controls.LoopingSelectorList.PaddingItemsCount;

        /// <summary>
        /// Returns how many distinct values a selector column holds, which for a looping column
        /// is the length of one band rather than the length of the repeated list.
        /// </summary>
        /// <param name="selector">The column to measure.</param>
        /// <returns>The number of distinct values.</returns>
        internal static int LoopingColumnSourceCount(Selector selector)
        {
            return LoopingSelectorColumns.GetSourceCount(selector);
        }

        /// <summary>
        /// Returns the index of the selected value within one band of a looping column.
        /// </summary>
        /// <param name="selector">The column to read.</param>
        /// <returns>The index within the band, or -1 when there is no selection.</returns>
        internal static int LoopingColumnSourceIndex(Selector selector)
        {
            return LoopingSelectorColumns.GetSourceIndex(selector);
        }

        /// <summary>
        /// Selects a value in a looping column by its index within one band, positioning the
        /// selection in the middle band the way the pickers do. The first and last few list
        /// positions cannot be centred under the selection band, so a test must never set a raw
        /// band-relative index on a looping column.
        /// </summary>
        /// <param name="selector">The column to drive.</param>
        /// <param name="sourceIndex">The index within one band to select.</param>
        internal static void SelectLoopingColumnValue(Selector selector, int sourceIndex)
        {
            Controls.LoopingItemsSource looping = Assert.IsType<Controls.LoopingItemsSource>(selector.ItemsSource);
            selector.SelectedIndex = looping.MiddleBandStart + sourceIndex;
        }

        /// <summary>
        /// Selects a value in a padded (non-looping) column by its index among the real values,
        /// skipping the leading placeholder rows.
        /// </summary>
        /// <param name="selector">The column to drive.</param>
        /// <param name="sourceIndex">The index among the real values to select.</param>
        internal static void SelectPaddedColumnValue(Selector selector, int sourceIndex)
        {
            selector.SelectedIndex = LoopingPaddingItemsCount + sourceIndex;
        }
    }
}
