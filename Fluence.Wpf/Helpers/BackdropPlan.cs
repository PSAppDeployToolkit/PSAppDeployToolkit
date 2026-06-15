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

using System.Windows.Media;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Carries the resolved DWM backdrop instructions computed by
    /// <see cref="Controls.WindowPolicy.BuildBackdropPlan"/>. All fields are
    /// immutable after construction; callers read them and apply the instructions to the window
    /// handle without re-querying the OS.
    /// </summary>
    /// <param name="effectiveBackdrop">The backdrop type that will actually be applied after capability downgrade.</param>
    /// <param name="useTransparentBackground">Indicates whether the window's client background must be transparent.</param>
    /// <param name="backgroundColor">The color that should be set on both <c>Window.Background</c> and <c>HwndSource.CompositionTarget.BackgroundColor</c>.</param>
    /// <param name="captionColor">The value to write to <c>DWMWA_CAPTION_COLOR</c>.</param>
    /// <param name="systemBackdropType">The <c>DWMSBT_*</c> value to write via <c>DWMWA_SYSTEMBACKDROP_TYPE</c>, or <see langword="null"/> when the OS does not expose that attribute.</param>
    /// <param name="useLegacyMicaEffect">Indicates whether the legacy 21H2 Mica effect should be applied.</param>
    /// <param name="useImmersiveDarkMode">Indicates whether immersive dark mode should be applied.</param>
    internal sealed class BackdropPlan(
        BackdropType effectiveBackdrop,
        bool useTransparentBackground,
        Color backgroundColor,
        int captionColor,
        int? systemBackdropType,
        bool useLegacyMicaEffect,
        bool useImmersiveDarkMode)
    {
        /// <summary>
        /// Gets the backdrop type that will actually be applied after capability downgrade. For
        /// example, <see cref="BackdropType.Acrylic"/> on a pre-22H2 build resolves to
        /// <see cref="BackdropType.Mica"/>, and any transparent backdrop on Windows 10 resolves
        /// to <see cref="BackdropType.None"/>.
        /// </summary>
        internal BackdropType EffectiveBackdrop { get; } = effectiveBackdrop;

        /// <summary>
        /// Gets a value indicating whether the window's client background must be transparent.
        /// <see langword="true"/> for any active DWM system backdrop (Mica, Acrylic, Tabbed);
        /// <see langword="false"/> for <see cref="BackdropType.None"/>, which paints a solid
        /// fallback color to avoid revealing the default-black redirection surface.
        /// </summary>
        internal bool UseTransparentBackground { get; } = useTransparentBackground;

        /// <summary>
        /// Gets the <see cref="Color"/> that should be set on both
        /// <c>Window.Background</c> and <c>HwndSource.CompositionTarget.BackgroundColor</c>.
        /// <see cref="Colors.Transparent"/> when a system backdrop is active;
        /// the theme fallback color when <see cref="EffectiveBackdrop"/> is
        /// <see cref="BackdropType.None"/>.
        /// </summary>
        internal Color BackgroundColor { get; private set; } = backgroundColor;

        /// <summary>
        /// Gets the value to write to <c>DWMWA_CAPTION_COLOR</c>.
        /// <see cref="Native.NativeConstants.DWMWA_COLOR_NONE"/> when a transparent
        /// backdrop is active (so the system backdrop shows through the caption strip);
        /// <see cref="Native.NativeConstants.DWMWA_COLOR_DEFAULT"/> for
        /// <see cref="BackdropType.None"/> (leave the DWM default in place).
        /// </summary>
        internal int CaptionColor { get; } = captionColor;

        /// <summary>
        /// Gets the <c>DWMSBT_*</c> value to write via <c>DWMWA_SYSTEMBACKDROP_TYPE</c>, or
        /// <see langword="null"/> when the OS does not expose that attribute (pre-22H2 or
        /// Windows 10). A <see langword="null"/> value must not be written to DWM.
        /// </summary>
        internal int? SystemBackdropType { get; private set; } = systemBackdropType;

        /// <summary>
        /// Gets a value indicating whether the legacy 21H2 Mica effect
        /// (<c>DWMWA_MICA_EFFECT</c> attribute 1029) should be applied. <see langword="true"/>
        /// only on Windows 11 pre-22H2 builds that support <c>DWMWA_MICA_EFFECT</c> but not
        /// the canonical <c>DWMWA_SYSTEMBACKDROP_TYPE</c>. Mutually exclusive with a non-null
        /// <see cref="SystemBackdropType"/>.
        /// </summary>
        internal bool UseLegacyMicaEffect { get; } = useLegacyMicaEffect;

        /// <summary>
        /// Gets a value indicating whether <c>DWMWA_USE_IMMERSIVE_DARK_MODE</c> should be set
        /// on the window handle. <see langword="true"/> when the resolved application theme is
        /// <see cref="ApplicationTheme.Dark"/>; the correct DWM attribute ordinal (19 or 20)
        /// is selected at apply-time by
        /// <see cref="Native.NativeMethods.GetImmersiveDarkModeAttribute"/>.
        /// </summary>
        internal bool UseImmersiveDarkMode { get; } = useImmersiveDarkMode;
    }
}
