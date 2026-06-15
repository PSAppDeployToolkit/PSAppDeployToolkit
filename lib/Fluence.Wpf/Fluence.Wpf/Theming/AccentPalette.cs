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

using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>The seven-rung Windows accent ramp, lightest to darkest.</summary>
    /// <param name="light3">The lightest tint on the generated accent ramp.</param>
    /// <param name="light2">The second light tint on the generated accent ramp.</param>
    /// <param name="light1">The first light tint on the generated accent ramp.</param>
    /// <param name="accent">The base accent color.</param>
    /// <param name="dark1">The first dark shade on the generated accent ramp.</param>
    /// <param name="dark2">The second dark shade on the generated accent ramp.</param>
    /// <param name="dark3">The darkest shade on the generated accent ramp.</param>
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct AccentPalette(Color light3, Color light2, Color light1, Color accent, Color dark1, Color dark2, Color dark3)
    {
        /// <summary>Gets the lightest tint on the generated accent ramp.</summary>
        public Color Light3 { get; } = light3;

        /// <summary>Gets the second light tint on the generated accent ramp.</summary>
        public Color Light2 { get; } = light2;

        /// <summary>Gets the first light tint on the generated accent ramp.</summary>
        public Color Light1 { get; } = light1;

        /// <summary>Gets the base accent color.</summary>
        public Color Accent { get; } = accent;

        /// <summary>Gets the first dark shade on the generated accent ramp.</summary>
        public Color Dark1 { get; } = dark1;

        /// <summary>Gets the second dark shade on the generated accent ramp.</summary>
        public Color Dark2 { get; } = dark2;

        /// <summary>Gets the darkest shade on the generated accent ramp.</summary>
        public Color Dark3 { get; } = dark3;
    }
}
