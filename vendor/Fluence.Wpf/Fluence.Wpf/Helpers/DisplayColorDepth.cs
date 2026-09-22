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

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// The advanced-color state of the display path a window is currently on, as reported by
    /// <c language="csharp">DisplayConfigGetDeviceInfo</c> with
    /// <c language="csharp">DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO</c>. See
    /// <see cref="Native.NativeMethods.GetDisplayColorDepth"/> for how this is read, and the
    /// KNOWN_ISSUES.md entry "Translucent layers over a DWM backdrop lose alpha precision on a
    /// 10 bpc display" for the measurement that makes both fields matter: a 10-bits-per-channel
    /// Windows output with advanced color off quantises DWM's client-alpha compositing to two
    /// bits, but the same output with advanced color enabled (measured by toggling the GPU
    /// driver's 10-bit pixel format setting) composites at full precision.
    /// </summary>
    /// <remarks>
    /// A plain constructor-and-properties struct rather than a <see langword="record"/>: a record's
    /// positional properties are <see langword="init"/>-only, which needs
    /// <c language="csharp">System.Runtime.CompilerServices.IsExternalInit</c>, a type the net472 BCL does not
    /// ship (Section 4.3 of AGENTS.md). Get-only properties assigned from an ordinary constructor
    /// need no such type and already compile on every target framework this library ships, matching
    /// the pattern <see cref="BackdropPlan"/> and <see cref="WindowCapabilities"/> already use.
    /// </remarks>
    internal readonly struct DisplayColorDepth
    {
        /// <summary>
        /// Initializes a new <see cref="DisplayColorDepth"/>.
        /// </summary>
        /// <param name="bitsPerColorChannel">The reported bits per color channel, or <c language="csharp">0</c> when unknown.</param>
        /// <param name="advancedColorEnabled">Whether the display path currently has advanced color enabled.</param>
        internal DisplayColorDepth(int bitsPerColorChannel, bool advancedColorEnabled)
        {
            BitsPerColorChannel = bitsPerColorChannel;
            AdvancedColorEnabled = advancedColorEnabled;
        }

        /// <summary>
        /// Gets the reported bits per color channel, or <c language="csharp">0</c> when the display path could
        /// not be resolved (unknown; treat as 8).
        /// </summary>
        internal int BitsPerColorChannel { get; }

        /// <summary>
        /// Gets a value indicating whether the display path currently has advanced color enabled.
        /// Always <see langword="false"/> when <see cref="BitsPerColorChannel"/> is unknown.
        /// </summary>
        internal bool AdvancedColorEnabled { get; }
    }
}
