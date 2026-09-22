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

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// The one set of ancestor-relative visual position helpers for the suite. Task 26 lifted
    /// these out of <c language="cs">DemoMainWindowTests</c> once a second and a third
    /// destination class needed them; every test class brings these into scope with a using static
    /// Fluence.Wpf.Tests.Infrastructure.VisualGeometry directive so the call sites read exactly as
    /// they did when the shell test class carried its own private copy.
    /// </summary>
    internal static class VisualGeometry
    {
        /// <summary>
        /// Returns the X coordinate of <paramref name="element"/>'s top-left corner in
        /// <paramref name="ancestor"/>'s coordinate space.
        /// </summary>
        /// <param name="element">The element whose position is read.</param>
        /// <param name="ancestor">The visual ancestor to measure against.</param>
        internal static double GetVisualX(FrameworkElement element, Visual ancestor)
        {
            return element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).X;
        }

        /// <summary>
        /// Returns the Y coordinate of <paramref name="element"/>'s top-left corner in
        /// <paramref name="ancestor"/>'s coordinate space.
        /// </summary>
        /// <param name="element">The element whose position is read.</param>
        /// <param name="ancestor">The visual ancestor to measure against.</param>
        internal static double GetVisualY(FrameworkElement element, Visual ancestor)
        {
            return element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).Y;
        }

        /// <summary>
        /// Returns the X coordinate of <paramref name="element"/>'s horizontal center in
        /// <paramref name="ancestor"/>'s coordinate space.
        /// </summary>
        /// <param name="element">The element whose center is read.</param>
        /// <param name="ancestor">The visual ancestor to measure against.</param>
        internal static double GetVisualCenterX(FrameworkElement element, Visual ancestor)
        {
            return GetVisualX(element, ancestor) + (element.ActualWidth / 2.0);
        }

        /// <summary>
        /// Returns the Y coordinate of <paramref name="element"/>'s vertical center in
        /// <paramref name="ancestor"/>'s coordinate space.
        /// </summary>
        /// <param name="element">The element whose center is read.</param>
        /// <param name="ancestor">The visual ancestor to measure against.</param>
        internal static double GetVisualCenterY(FrameworkElement element, Visual ancestor)
        {
            return GetVisualY(element, ancestor) + (element.ActualHeight / 2.0);
        }
    }
}
