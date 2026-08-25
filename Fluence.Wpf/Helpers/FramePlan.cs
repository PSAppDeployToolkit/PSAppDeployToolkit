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

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Carries the resolved window-border frame instructions computed by
    /// <see cref="Controls.WindowPolicy.BuildFramePlan"/>. The plan separates the
    /// WPF-template border (driven by <see cref="TemplateBorderThickness"/> and
    /// <see cref="TemplateBorderBrushResourceKey"/>) from the DWM border color
    /// (<see cref="DwmBorderColor"/>), because only some OS builds support the DWM side.
    /// DWM draws the outer border and the template border sits just inside it, theme-matched to
    /// the window surface unless the window is active with accent borders enabled.
    /// </summary>
    /// <param name="templateBorderThickness">The thickness of the WPF-template border element.</param>
    /// <param name="templateBorderBrushResourceKey">The <c>DynamicResource</c> key for the border brush.</param>
    /// <param name="dwmBorderColor">
    ///   The value for <c>DWMWA_BORDER_COLOR</c>: the accent COLORREF for an active window with
    ///   accent borders on a capable OS, otherwise DWMWA_COLOR_DEFAULT.
    /// </param>
    internal sealed class FramePlan(
        Thickness templateBorderThickness,
        string templateBorderBrushResourceKey,
        uint dwmBorderColor)
    {
        /// <summary>
        /// Gets the thickness of the WPF-template border element. <c>Thickness(2)</c> in the
        /// normal window state; <c>Thickness(0)</c> when maximized
        /// (a border at the monitor edge would clip against the taskbar or other monitors).
        /// This is the only border the window paints, so the value is applied to
        /// <c>FluenceWindow.BorderThickness</c> rather than left to a template trigger.
        /// </summary>
        internal Thickness TemplateBorderThickness { get; } = templateBorderThickness;

        /// <summary>
        /// Gets the <c>DynamicResource</c> key for the border brush to apply to the template
        /// border element. <c>"SystemAccentColorBrush"</c> when the window is active and accent
        /// borders are enabled; <c>"CardStrokeColorDefaultSolidBrush"</c> when the window is
        /// inactive or accent borders are off.
        /// </summary>
        internal string TemplateBorderBrushResourceKey { get; } = templateBorderBrushResourceKey;

        /// <summary>
        /// Gets the value to write to <c>DWMWA_BORDER_COLOR</c>. The accent COLORREF when the OS
        /// exposes the attribute and the window is active with accent borders enabled, so the DWM
        /// border and the template border agree; DWMWA_COLOR_DEFAULT otherwise, which leaves DWM
        /// drawing its own adaptive border. A caller must check
        /// <see cref="WindowCapabilities.SupportsBorderColor"/> before writing this value to the
        /// DWM attribute; the plan records the sentinel regardless so the caller does not need a
        /// separate null check.
        /// </summary>
        internal uint DwmBorderColor { get; } = dwmBorderColor;
    }
}
