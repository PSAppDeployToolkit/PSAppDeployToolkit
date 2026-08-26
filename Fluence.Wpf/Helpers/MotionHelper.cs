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

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Central gate for whether Fluence controls play motion. Respects the Windows
    /// "Show animations in Windows" accessibility setting via
    /// <see cref="SystemParameters.ClientAreaAnimation"/>, and additionally requires
    /// hardware-accelerated rendering. Controls consult this gate at each code-driven
    /// animation entry point and jump to their final visual state when motion is
    /// disabled, matching how Windows itself behaves with the toggle off.
    /// </summary>
    internal static class MotionHelper
    {
        /// <summary>
        /// Gets or sets the test seam. When non-null, overrides the OS setting.
        /// Reset to null in test cleanup.
        /// </summary>
        internal static bool? OverrideIsMotionEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating whether animations should play. When
        /// <see cref="OverrideIsMotionEnabled"/> is null, motion requires both conditions:
        /// the Windows "Show animations in Windows" accessibility setting is on
        /// (<see cref="SystemParameters.ClientAreaAnimation"/>), and the process is rendering
        /// with hardware acceleration. Software rendering (render tier 0) makes every animated
        /// frame a CPU composite, so motion is dropped rather than played back at a stutter.
        /// The rendering tier lives in the high word of <see cref="RenderCapability.Tier"/>, so
        /// it must be shifted right by 16 before comparison; the raw value is not a tier number.
        /// </summary>
        internal static bool IsMotionEnabled =>
            OverrideIsMotionEnabled ?? (SystemParameters.ClientAreaAnimation && (RenderCapability.Tier >> 16) > 0);
    }
}
