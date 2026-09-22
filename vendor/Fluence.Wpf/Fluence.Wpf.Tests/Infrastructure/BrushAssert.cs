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

using System.Windows;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// Resolves a canonical theme brush key and compares colours. The suite asserts brush roles by
    /// colour rather than by instance, because the theme engine rebuilds every brush on each apply.
    /// </summary>
    internal static class BrushAssert
    {
        /// <summary>
        /// Asserts that <paramref name="actual"/> is a <see cref="SolidColorBrush"/> whose colour
        /// equals the colour of the brush the current application resolves for
        /// <paramref name="expectedResourceKey"/>.
        /// </summary>
        /// <param name="actual">The brush read off the element under test.</param>
        /// <param name="expectedResourceKey">The canonical WinUI-style brush key, for example
        /// <c language="xaml">ControlStrongStrokeColorDefaultBrush</c>.</param>
        internal static void AssertBrushColor(Brush? actual, string expectedResourceKey)
        {
            Color expected = SolidColor(Application.Current?.TryFindResource(expectedResourceKey));
            Assert.Equal(expected, SolidColor(actual));
        }

        /// <summary>
        /// Returns the colour of the <see cref="SolidColorBrush"/> that
        /// <paramref name="application"/> resolves for <paramref name="resourceKey"/>, failing the
        /// test if the key does not resolve to one.
        /// </summary>
        /// <param name="application">The application whose resources to read.</param>
        /// <param name="resourceKey">The canonical WinUI-style brush key.</param>
        internal static Color ResolvedColor(Application application, string resourceKey)
        {
            return SolidColor(application.TryFindResource(resourceKey));
        }

        /// <summary>
        /// Returns the colour of <paramref name="resource"/>, failing the test if it is not a
        /// <see cref="SolidColorBrush"/>. The suite reads brush colours far more often than brush
        /// instances, because the theme engine rebuilds every brush on each apply. Accepting
        /// <see cref="object"/> rather than <see cref="Brush"/> keeps a resource lookup that
        /// resolved to something other than a brush (a <see cref="Color"/>, a <see cref="Style"/>)
        /// visible in the assertion failure message instead of collapsing it to null first.
        /// </summary>
        /// <param name="resource">The value to read, typically straight off an element under test
        /// or out of a resource lookup.</param>
        internal static Color SolidColor(object? resource)
        {
            return Assert.IsType<SolidColorBrush>(resource).Color;
        }
    }
}
