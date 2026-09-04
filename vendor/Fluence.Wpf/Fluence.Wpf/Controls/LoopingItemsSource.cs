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
using System.Collections;
using System.Collections.Generic;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A read-only, non-generic list that presents a short source sequence repeated a fixed
    /// number of times, so a virtualizing selector can scroll through it as if the sequence
    /// looped forever. This is the WinUI looping-selector trick: rather than wrapping the
    /// scroll offset (which no WPF panel supports), the band is simply made long enough that
    /// a user starting in the middle can never reach either end.
    /// </summary>
    /// <remarks>
    /// Only the indexer, <see cref="Count"/>, and enumeration are meaningful; every mutating
    /// member throws <see cref="NotSupportedException"/>. Consumers position the selection at
    /// <see cref="MiddleBandStart"/> plus the source index so there are five hundred copies of
    /// the sequence available in each direction.
    /// </remarks>
    internal sealed class LoopingItemsSource : IList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoopingItemsSource"/> class over a
        /// snapshot of <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The values one band of the looping list repeats.</param>
        internal LoopingItemsSource(IReadOnlyList<object> source)
        {
            _source = new List<object>(source.Count);
            for (int index = 0; index < source.Count; index++)
            {
                _source.Add(source[index]);
            }
        }

        /// <summary>
        /// Gets the number of distinct values in one band, that is the length of the sequence
        /// the list repeats. A selector index maps back to a source index with
        /// <c language="csharp">index % SourceCount</c>.
        /// </summary>
        internal int SourceCount => _source.Count;

        /// <summary>
        /// Gets the index at which the middle band begins. Positioning the selection here plus
        /// the wanted source index leaves an equal number of bands above and below.
        /// </summary>
        internal int MiddleBandStart => _source.Count * (RepeatCount / 2);

        /// <inheritdoc />
        public int Count => _source.Count * RepeatCount;

        /// <inheritdoc />
        public bool IsFixedSize => true;

        /// <inheritdoc />
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public bool IsSynchronized => false;

        /// <inheritdoc />
        public object SyncRoot => this;

        /// <summary>
        /// Gets the value at <paramref name="index"/>, which is the source value at
        /// <c language="csharp">index % SourceCount</c>. Setting a value is not supported.
        /// </summary>
        /// <param name="index">The index into the repeated list.</param>
        /// <returns>The source value that <paramref name="index"/> maps onto.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside 0..<see cref="Count"/>-1.</exception>
        /// <exception cref="NotSupportedException">The list is read-only.</exception>
        public object? this[int index]
        {
            get => index >= 0 && index < Count
                ? _source[index % _source.Count]
                : throw new ArgumentOutOfRangeException(nameof(index));
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public bool Contains(object? value)
        {
            return IndexOfSourceValue(value) >= 0;
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            int count = Count;
            for (int offset = 0; offset < count; offset++)
            {
                array.SetValue(_source[offset % _source.Count], index + offset);
            }
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator()
        {
            int count = Count;
            for (int index = 0; index < count; index++)
            {
                yield return _source[index % _source.Count];
            }
        }

        /// <summary>
        /// Returns the middle-band index of <paramref name="value"/>, so a lookup by value
        /// lands in the centre of the list rather than at its start.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>The middle-band index of the value, or -1 when it is not in the source.</returns>
        public int IndexOf(object? value)
        {
            int sourceIndex = IndexOfSourceValue(value);
            return sourceIndex < 0 ? -1 : MiddleBandStart + sourceIndex;
        }

        /// <inheritdoc />
        public int Add(object? value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void Remove(object? value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns the index of <paramref name="value"/> within a single band.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index within one band, or -1 when the value is not present.</returns>
        private int IndexOfSourceValue(object? value)
        {
            for (int index = 0; index < _source.Count; index++)
            {
                if (Equals(_source[index], value))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// How many times the source sequence is repeated. One thousand bands is what WinUI's
        /// looping selectors use: long enough that the ends are unreachable in practice, short
        /// enough that the virtualizing panel's extent stays a sane number.
        /// </summary>
        private const int RepeatCount = 1000;

        /// <summary>
        /// The snapshot of the values one band repeats.
        /// </summary>
        private readonly List<object> _source;
    }
}
