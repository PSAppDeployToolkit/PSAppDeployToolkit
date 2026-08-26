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

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// Mirrors the undocumented DWM colorization parameters returned by the ordinal-127 export of
    /// <c>dwmapi.dll</c>. Used to read the live glass/colorization color when the registry value is
    /// unavailable. Field order and types must match the native layout exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DWMCOLORIZATIONPARAMS
    {
        /// <summary>
        /// The primary colorization color as a packed <c>ARGB</c> value.
        /// </summary>
        public uint clrColor;

        /// <summary>
        /// The after-glow color.
        /// </summary>
        public uint clrAfterGlow;

        /// <summary>
        /// The colorization intensity.
        /// </summary>
        public uint nIntensity;

        /// <summary>
        /// The after-glow color balance.
        /// </summary>
        public uint clrAfterGlowBalance;

        /// <summary>
        /// The blur color balance.
        /// </summary>
        public uint clrBlurBalance;

        /// <summary>
        /// The glass reflection intensity.
        /// </summary>
        public uint clrGlassReflectionIntensity;

        /// <summary>
        /// Non-zero when the colorization is opaque.
        /// </summary>
        public int fOpaque;
    }
}
