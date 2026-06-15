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
    /// Snapshot of the DWM window-feature capabilities available on the current OS build.
    /// Capabilities are probed once per call-site via <see cref="Current"/>; callers hold the
    /// snapshot for the duration of one <c>ApplyWindowShell</c> pass rather than querying the OS
    /// on every individual attribute write.
    /// </summary>
    /// <param name="supportsSystemBackdropType">Whether the current OS build supports the <c>SystemBackdropType</c> attribute.</param>
    /// <param name="supportsMicaEffect">Whether the current OS build supports the legacy Mica effect.</param>
    /// <param name="supportsRoundedCorners">Whether the current OS build supports rounded corners.</param>
    /// <param name="supportsCaptionColor">Whether the current OS build supports setting the caption color.</param>
    /// <param name="supportsBorderColor">Whether the current OS build supports setting the border color.</param>
    internal sealed class WindowCapabilities(
        bool supportsSystemBackdropType,
        bool supportsMicaEffect,
        bool supportsRoundedCorners,
        bool supportsCaptionColor,
        bool supportsBorderColor = false)
    {
        /// <summary>
        /// Gets a value indicating whether the OS supports
        /// <c>DWMWA_SYSTEMBACKDROP_TYPE</c> (attribute 38). <see langword="true"/> on Windows 11
        /// 22H2 (build 22621) and later; <see langword="false"/> on earlier builds. When
        /// <see langword="true"/>, the canonical <c>DWMSBT_*</c> values must be used instead of
        /// the legacy <c>DWMWA_MICA_EFFECT</c> attribute.
        /// </summary>
        internal bool SupportsSystemBackdropType { get; } = supportsSystemBackdropType;

        /// <summary>
        /// Gets a value indicating whether the OS supports the legacy Mica toggle
        /// (<c>DWMWA_MICA_EFFECT</c>, attribute 1029). <see langword="true"/> on Windows 11 21H2
        /// (builds 22000 to 22620); <see langword="false"/> on Windows 10 or Windows 11 22H2+.
        /// Mutually exclusive with <see cref="SupportsSystemBackdropType"/>: when both would be
        /// <see langword="true"/>, <see cref="SupportsSystemBackdropType"/> wins and this is
        /// <see langword="false"/>.
        /// </summary>
        internal bool SupportsMicaEffect { get; } = supportsMicaEffect;

        /// <summary>
        /// Gets a value indicating whether the OS supports the DWM rounded-corner preference
        /// (<c>DWMWA_WINDOW_CORNER_PREFERENCE</c>, attribute 33). <see langword="true"/> on any
        /// Windows 11 build (build 22000+); <see langword="false"/> on Windows 10.
        /// </summary>
        internal bool SupportsRoundedCorners { get; } = supportsRoundedCorners;

        /// <summary>
        /// Gets a value indicating whether the OS supports setting the DWM caption color via
        /// <c>DWMWA_CAPTION_COLOR</c> (attribute 35). <see langword="true"/> on any Windows 11
        /// build; <see langword="false"/> on Windows 10.
        /// </summary>
        internal bool SupportsCaptionColor { get; } = supportsCaptionColor;

        /// <summary>
        /// Gets a value indicating whether the OS supports setting the DWM border color via
        /// <c>DWMWA_BORDER_COLOR</c> (attribute 34). <see langword="true"/> on any Windows 11
        /// build; <see langword="false"/> on Windows 10.
        /// </summary>
        internal bool SupportsBorderColor { get; } = supportsBorderColor;

        /// <summary>
        /// Gets a <see cref="WindowCapabilities"/> snapshot for the current OS build by
        /// delegating to <see cref="OsVersionHelper"/>, which caches the OS version after the
        /// first probe. Always returns a non-null instance; Windows 10 builds get a snapshot
        /// where all capability flags are <see langword="false"/>.
        /// </summary>
        internal static WindowCapabilities Current => new(
            OsVersionHelper.SupportsSystemBackdropType,
            OsVersionHelper.SupportsMicaEffect,
            OsVersionHelper.SupportsRoundedCorners,
            OsVersionHelper.SupportsCaptionColor,
            OsVersionHelper.SupportsBorderColor);
    }
}
