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
using System.Runtime.InteropServices;

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// Mirrors the undocumented <c>WINDOWCOMPOSITIONATTRIBDATA</c> structure that
    /// <c>SetWindowCompositionAttribute</c> takes by reference. It is a tagged pointer: the
    /// attribute id selects which payload type <see cref="Data"/> points at, and
    /// <see cref="SizeOfData"/> must match that payload exactly or the call is rejected. Fluence
    /// only ever sends <c>WCA_ACCENT_POLICY</c> with an <see cref="ACCENT_POLICY"/> payload. Field
    /// order and types must match the native layout exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWCOMPOSITIONATTRIBDATA
    {
        /// <summary>
        /// The composition attribute id, one of the <c>WCA_*</c> constants on
        /// <see cref="NativeMethods"/>.
        /// </summary>
        public int Attrib;

        /// <summary>
        /// A pointer to the payload for <see cref="Attrib"/>. The memory must stay fixed for the
        /// duration of the call.
        /// </summary>
        public IntPtr Data;

        /// <summary>
        /// The size in bytes of the payload <see cref="Data"/> points at.
        /// </summary>
        /// <remarks>
        /// Native declares this field as <c>SIZE_T</c>, which is eight bytes on x64, so the managed
        /// <see cref="int"/> covers only its low dword. That is safe solely because the struct is
        /// zero-initialized and the four bytes of trailing padding the layout adds after this field
        /// are therefore zero, which is exactly the high dword the callee reads. Never write to the
        /// padding or reuse an instance without re-zeroing it, or the size arrives with garbage in
        /// its top half and the call is rejected.
        /// </remarks>
        public int SizeOfData;
    }
}
