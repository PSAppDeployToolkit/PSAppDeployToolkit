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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// The filler item a <see cref="LoopingSelectorList"/> uses to pad a short, non-looping
    /// column (the AM/PM designator column, for instance) so its real values can still be
    /// centred under the selection band.
    /// </summary>
    /// <remarks>
    /// A selector viewport is nine rows tall with the selected row in the middle, so a column
    /// needs four rows of slack above its first value and below its last one. The padding rows
    /// carry this singleton; <see cref="LoopingSelectorList.PrepareContainerForItemOverride"/>
    /// recognises it and makes those containers hidden and disabled, so they occupy their row
    /// height without being visible, hit-testable, or selectable.
    /// </remarks>
    internal sealed class LoopingSelectorPlaceholder
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="LoopingSelectorPlaceholder"/> class
        /// from being created outside the type; identity is what marks a padding row, so there
        /// is exactly one instance.
        /// </summary>
        private LoopingSelectorPlaceholder()
        {
        }

        /// <summary>
        /// Gets the single placeholder instance used for every padding row.
        /// </summary>
        internal static LoopingSelectorPlaceholder Instance { get; } = new();

        /// <summary>
        /// Returns an empty string so a padding row renders nothing even if a custom item
        /// template binds it before the container is hidden.
        /// </summary>
        /// <returns>An empty string.</returns>
        public override string ToString()
        {
            return string.Empty;
        }
    }
}
