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

using System.Collections.ObjectModel;

namespace Fluence.Wpf.Markup
{
    /// <summary>
    /// The collection behind <see cref="ThemeDictionary.ThemeDictionaries"/>. Every mutation
    /// re-evaluates the owner's selected table, so tables added after construction (the XAML
    /// parse order) or at runtime take effect immediately.
    /// </summary>
    public sealed class ThemeResourceDictionaryCollection : Collection<ThemeResourceDictionary>
    {
        private readonly ThemeDictionary _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeResourceDictionaryCollection"/>
        /// class for the given owner.
        /// </summary>
        /// <param name="owner">The dictionary whose selection is re-evaluated on mutation.</param>
        internal ThemeResourceDictionaryCollection(ThemeDictionary owner)
        {
            _owner = owner;
        }

        /// <inheritdoc />
        protected override void InsertItem(int index, ThemeResourceDictionary item)
        {
            base.InsertItem(index, item);
            _owner.OnThemeDictionariesChanged();
        }

        /// <inheritdoc />
        protected override void SetItem(int index, ThemeResourceDictionary item)
        {
            base.SetItem(index, item);
            _owner.OnThemeDictionariesChanged();
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            _owner.OnThemeDictionariesChanged();
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            base.ClearItems();
            _owner.OnThemeDictionariesChanged();
        }
    }
}
