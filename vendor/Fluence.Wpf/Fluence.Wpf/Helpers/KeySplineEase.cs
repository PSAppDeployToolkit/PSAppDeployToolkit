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
using System.Windows;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// An easing function shaped by a WinUI key spline, for the code-built animations that cannot
    /// use a <see cref="SplineDoubleKeyFrame"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WinUI expresses its motion curves as key splines: the two control points of a cubic bezier
    /// whose ends are pinned at (0,0) and (1,1). A XAML keyframe takes one directly, but an
    /// animation built in code takes an <see cref="IEasingFunction"/>, and WPF ships none that
    /// accepts a bezier, so the curve is evaluated here instead of being approximated by the
    /// nearest <see cref="CubicEase"/>.
    /// </para>
    /// <para>
    /// The bezier is parametric, so progress along x has to be solved for before y can be read.
    /// Newton's method converges in a couple of steps for the curves in use; the bisection
    /// fallback covers the flat stretches where the derivative is too small to step from.
    /// </para>
    /// </remarks>
    internal sealed class KeySplineEase : EasingFunctionBase
    {
        private readonly double _x1;
        private readonly double _y1;
        private readonly double _x2;
        private readonly double _y2;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeySplineEase"/> class.
        /// </summary>
        /// <param name="x1">The x coordinate of the first control point.</param>
        /// <param name="y1">The y coordinate of the first control point.</param>
        /// <param name="x2">The x coordinate of the second control point.</param>
        /// <param name="y2">The y coordinate of the second control point.</param>
        internal KeySplineEase(double x1, double y1, double x2, double y2)
        {
            _x1 = x1;
            _y1 = y1;
            _x2 = x2;
            _y2 = y2;

            // A key spline already describes the whole curve, ease-in through ease-out. WPF's
            // default EasingMode is EaseOut, which evaluates 1 - EaseInCore(1 - t) and so mirrors
            // whatever it is given: a decelerating spline would come out accelerating. EaseIn is
            // what passes the curve through untouched.
            EasingMode = EasingMode.EaseIn;
        }

        /// <inheritdoc />
        protected override double EaseInCore(double normalizedTime)
        {
            if (normalizedTime <= 0)
            {
                return 0;
            }

            if (normalizedTime >= 1)
            {
                return 1;
            }

            double t = SolveForX(normalizedTime);
            return Bezier(t, _y1, _y2);
        }

        /// <inheritdoc />
        protected override Freezable CreateInstanceCore()
        {
            return new KeySplineEase(_x1, _y1, _x2, _y2);
        }

        /// <summary>
        /// Evaluates one axis of the cubic bezier at <paramref name="t"/>, with the end points
        /// pinned at 0 and 1 as a key spline's are.
        /// </summary>
        /// <param name="t">The curve parameter.</param>
        /// <param name="p1">The first control point's coordinate on this axis.</param>
        /// <param name="p2">The second control point's coordinate on this axis.</param>
        /// <returns>The coordinate at <paramref name="t"/>.</returns>
        private static double Bezier(double t, double p1, double p2)
        {
            double inverse = 1 - t;
            return (3 * inverse * inverse * t * p1) + (3 * inverse * t * t * p2) + (t * t * t);
        }

        /// <summary>
        /// Returns the curve parameter whose x coordinate is <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The x coordinate to solve for, between 0 and 1.</param>
        /// <returns>The curve parameter.</returns>
        private double SolveForX(double x)
        {
            const double tolerance = 1e-6;
            double t = x;

            for (int iteration = 0; iteration < 8; iteration++)
            {
                double error = Bezier(t, _x1, _x2) - x;
                if (Math.Abs(error) < tolerance)
                {
                    return t;
                }

                double derivative = BezierDerivative(t, _x1, _x2);
                if (Math.Abs(derivative) < tolerance)
                {
                    break;
                }

                t -= error / derivative;
            }

            double low = 0;
            double high = 1;
            t = x;
            for (int iteration = 0; iteration < 32; iteration++)
            {
                double value = Bezier(t, _x1, _x2);
                if (Math.Abs(value - x) < tolerance)
                {
                    break;
                }

                if (value > x)
                {
                    high = t;
                }
                else
                {
                    low = t;
                }

                t = (low + high) / 2;
            }

            return t;
        }

        /// <summary>
        /// Evaluates the derivative of one axis of the cubic bezier at <paramref name="t"/>.
        /// </summary>
        /// <param name="t">The curve parameter.</param>
        /// <param name="p1">The first control point's coordinate on this axis.</param>
        /// <param name="p2">The second control point's coordinate on this axis.</param>
        /// <returns>The rate of change at <paramref name="t"/>.</returns>
        private static double BezierDerivative(double t, double p1, double p2)
        {
            double inverse = 1 - t;
            return (3 * inverse * inverse * p1) + (6 * inverse * t * (p2 - p1)) + (3 * t * t * (1 - p2));
        }
    }
}
