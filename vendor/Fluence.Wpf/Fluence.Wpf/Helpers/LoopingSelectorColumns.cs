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

using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Fills and reads back the selector columns of the <see cref="DatePicker"/> and
    /// <see cref="TimePicker"/> flyouts. The two pickers share the same column contract, and
    /// each column may be either a <see cref="LoopingSelectorList"/> (the default templates) or
    /// a plain <see cref="Selector"/> supplied by a custom template, so the index a column
    /// reports is not always the index of the value it shows. Every method here handles both.
    /// </summary>
    internal static class LoopingSelectorColumns
    {
        /// <summary>
        /// Fills a column with an endlessly repeating band of <paramref name="values"/> and
        /// selects <paramref name="sourceIndex"/> in the middle band, so the user can scroll
        /// hundreds of turns in either direction before reaching an end. A column that is not a
        /// <see cref="LoopingSelectorList"/> gets the plain values and index instead.
        /// </summary>
        /// <param name="selector">The column to fill; ignored when <see langword="null"/>.</param>
        /// <param name="values">The values of one band.</param>
        /// <param name="sourceIndex">The index within <paramref name="values"/> to select.</param>
        internal static void SetLoopingSource(Selector? selector, IReadOnlyList<object> values, int sourceIndex)
        {
            if (selector is null)
            {
                return;
            }

            if (selector is LoopingSelectorList)
            {
                LoopingItemsSource looping = new(values);
                selector.ItemsSource = looping;
                selector.SelectedIndex = looping.MiddleBandStart + sourceIndex;
                return;
            }

            selector.ItemsSource = values;
            selector.SelectedIndex = sourceIndex;
        }

        /// <summary>
        /// Fills a column whose values must not repeat (the AM/PM designator column) with those
        /// values padded by hidden placeholder rows, so even the first and last value can sit
        /// under the centred selection band. A column that is not a
        /// <see cref="LoopingSelectorList"/> gets the plain values and index instead.
        /// </summary>
        /// <param name="selector">The column to fill; ignored when <see langword="null"/>.</param>
        /// <param name="values">The values to show.</param>
        /// <param name="sourceIndex">The index within <paramref name="values"/> to select.</param>
        internal static void SetPaddedSource(Selector? selector, IReadOnlyList<object> values, int sourceIndex)
        {
            if (selector is null)
            {
                return;
            }

            if (selector is LoopingSelectorList)
            {
                List<object> padded = new(values.Count + (LoopingSelectorList.PaddingItemsCount * 2));
                for (int pad = 0; pad < LoopingSelectorList.PaddingItemsCount; pad++)
                {
                    padded.Add(LoopingSelectorPlaceholder.Instance);
                }

                for (int index = 0; index < values.Count; index++)
                {
                    padded.Add(values[index]);
                }

                for (int pad = 0; pad < LoopingSelectorList.PaddingItemsCount; pad++)
                {
                    padded.Add(LoopingSelectorPlaceholder.Instance);
                }

                selector.ItemsSource = padded;
                selector.SelectedIndex = LoopingSelectorList.PaddingItemsCount + sourceIndex;
                return;
            }

            selector.ItemsSource = values;
            selector.SelectedIndex = sourceIndex;
        }

        /// <summary>
        /// Returns the index of the selected value within one band of a column filled by
        /// <see cref="SetLoopingSource"/>, which is what the picker's date and time math works
        /// in. A column with no selection reports -1.
        /// </summary>
        /// <param name="selector">The column to read; may be <see langword="null"/>.</param>
        /// <returns>The index within the band, or -1 when there is no selection.</returns>
        internal static int GetSourceIndex(Selector? selector)
        {
            int index = selector?.SelectedIndex ?? -1;
            return index < 0
                ? -1
                : selector?.ItemsSource is LoopingItemsSource looping
                    ? looping.SourceCount > 0 ? index % looping.SourceCount : -1
                    : index;
        }

        /// <summary>
        /// Returns the index of the selected value within a column filled by
        /// <see cref="SetPaddedSource"/>, discounting the leading placeholder rows. A column
        /// with no selection, or one whose selection landed on a placeholder row, reports -1.
        /// </summary>
        /// <param name="selector">The column to read; may be <see langword="null"/>.</param>
        /// <returns>The index within the values, or -1 when there is no value selected.</returns>
        internal static int GetPaddedSourceIndex(Selector? selector)
        {
            int index = selector?.SelectedIndex ?? -1;
            if (index < 0)
            {
                return -1;
            }

            if (selector is not LoopingSelectorList)
            {
                return index;
            }

            int valueCount = selector.Items.Count - (LoopingSelectorList.PaddingItemsCount * 2);
            int sourceIndex = index - LoopingSelectorList.PaddingItemsCount;
            return sourceIndex >= 0 && sourceIndex < valueCount ? sourceIndex : -1;
        }

        /// <summary>
        /// Returns how many distinct values a column filled by <see cref="SetLoopingSource"/>
        /// holds, which for a looping column is the length of one band rather than the length
        /// of the repeated list.
        /// </summary>
        /// <param name="selector">The column to measure; may be <see langword="null"/>.</param>
        /// <returns>The number of distinct values, or 0 when there is no column.</returns>
        internal static int GetSourceCount(Selector? selector)
        {
            return selector is null
                ? 0
                : selector.ItemsSource is LoopingItemsSource looping ? looping.SourceCount : selector.Items.Count;
        }

        /// <summary>
        /// Brings a plain column's selection into view. A <see cref="LoopingSelectorList"/>
        /// positions itself from its selected index, and
        /// <see cref="System.Windows.Controls.ListBox.ScrollIntoView(object)"/> would fight that
        /// by parking the row at whichever viewport edge is nearest instead of under the
        /// selection band, so looping columns are skipped.
        /// </summary>
        /// <param name="selector">The column to scroll; may be <see langword="null"/>.</param>
        internal static void ScrollSelectionIntoView(Selector? selector)
        {
            if (selector is LoopingSelectorList)
            {
                return;
            }

            if (selector is System.Windows.Controls.ListBox listBox && listBox.SelectedItem is not null)
            {
                listBox.ScrollIntoView(listBox.SelectedItem);
            }
        }
    }
}
