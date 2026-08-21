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

using Fluence.Wpf.Controls;
using System;

namespace Fluence.Wpf
{
    /// <summary>
    /// Provides data for the <see cref="AutoSuggestBox.TextChanged"/> event.
    /// </summary>
    public class AutoSuggestBoxTextChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the reason the text changed.
        /// </summary>
        public AutoSuggestionBoxTextChangeReason Reason { get; set; }

        /// <summary>
        /// Returns whether the text of the <see cref="AutoSuggestBox"/> that raised this event
        /// is unchanged since the event was raised. Use this WinUI parity helper to discard
        /// stale asynchronous filtering results. Returns <see langword="true"/> for instances
        /// not raised by an <see cref="AutoSuggestBox"/>.
        /// </summary>
        /// <returns><see langword="true"/> when the owning box text still matches the text
        /// captured when the event was raised; otherwise <see langword="false"/>.</returns>
        public bool CheckCurrent()
        {
            return _owner is null || string.Equals(_owner.Text, _textSnapshot, StringComparison.Ordinal);
        }

        /// <summary>
        /// Captures the owning box and its text at raise time so <see cref="CheckCurrent"/>
        /// can detect later changes.
        /// </summary>
        /// <param name="owner">The box raising the event.</param>
        /// <param name="text">The text at raise time.</param>
        internal void Capture(AutoSuggestBox owner, string text)
        {
            _owner = owner;
            _textSnapshot = text;
        }

        /// <summary>
        /// The box that raised this event, when raised by a control.
        /// </summary>
        private AutoSuggestBox? _owner;

        /// <summary>
        /// The text of the owning box at the moment the event was raised.
        /// </summary>
        private string? _textSnapshot;
    }
}
