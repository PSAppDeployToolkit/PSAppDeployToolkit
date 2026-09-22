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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// Locates a <see cref="Controls.Button"/> or one of its glyph text blocks by content, shared
    /// by the gallery page tests and the demo shell tests: both search the same demo button
    /// shapes (a Fluent Button whose Content is a plain string, with an optional glyph
    /// TextBlock inside it).
    /// </summary>
    internal static class FluentButtonQueries
    {
        /// <summary>
        /// Returns the first descendant <see cref="Controls.Button"/> whose Content equals the
        /// given string, or null if none matches.
        /// </summary>
        /// <param name="root">The subtree to search.</param>
        /// <param name="content">The button's expected Content string.</param>
        internal static Controls.Button? FindFluentButtonByContent(DependencyObject root, string content)
        {
            return FindVisualChildren<Controls.Button>(root).FirstOrDefault(button => string.Equals(button.Content as string, content, StringComparison.Ordinal));
        }

        /// <summary>
        /// Returns the descendant <see cref="TextBlock"/> inside a button whose Text equals the
        /// given glyph, or null if none matches.
        /// </summary>
        /// <param name="button">The button to search.</param>
        /// <param name="glyph">The expected glyph text.</param>
        internal static TextBlock? FindButtonGlyphTextBlock(Controls.Button button, string glyph)
        {
            return FindVisualChildren<TextBlock>(button).FirstOrDefault(textBlock => string.Equals(textBlock.Text, glyph, StringComparison.Ordinal));
        }
    }
}
