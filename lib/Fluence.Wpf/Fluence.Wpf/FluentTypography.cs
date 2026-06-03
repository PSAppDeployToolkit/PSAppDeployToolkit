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

namespace Fluence.Wpf
{
    /// <summary>
    /// Specifies the typography style for text elements following Windows 11 Fluent Design.
    /// </summary>
    public enum FluentTypography
    {
        /// <summary>
        /// No typography style applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// Caption text: 12px, Regular.
        /// </summary>
        Caption = 1,

        /// <summary>
        /// Body text: 14px, Regular.
        /// </summary>
        Body = 2,

        /// <summary>
        /// Body strong text: 14px, SemiBold.
        /// </summary>
        BodyStrong = 3,

        /// <summary>
        /// Body large text: 18px, Regular.
        /// </summary>
        BodyLarge = 4,

        /// <summary>
        /// Subtitle text: 20px, SemiBold.
        /// </summary>
        Subtitle = 5,

        /// <summary>
        /// Title text: 28px, SemiBold.
        /// </summary>
        Title = 6,

        /// <summary>
        /// Title large text: 40px, Regular.
        /// </summary>
        TitleLarge = 7,

        /// <summary>
        /// Display text: 68px, SemiBold.
        /// </summary>
        Display = 8,
    }
}
