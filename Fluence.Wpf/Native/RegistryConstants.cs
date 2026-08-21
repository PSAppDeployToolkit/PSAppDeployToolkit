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

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// Compile-time Win32, DWM, UxTheme, and shell constants shared across the native interop
    /// layer. Values are the documented operating-system ordinals and bit flags; they are pinned
    /// by the interop tests, so changing one here is a wire-contract break, not a refactor.
    /// </summary>
    internal static class RegistryConstants
    {
        // ---------------------------------------------------------------------
        // Registry paths (relative to HKEY_CURRENT_USER).
        // ---------------------------------------------------------------------

        /// <summary>
        /// The personalization key holding the apps/system light-theme flags.
        /// </summary>
        public const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>
        /// The themes key holding the current theme path.
        /// </summary>
        public const string ThemesRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes";

        /// <summary>
        /// The DWM key holding colorization and accent values.
        /// </summary>
        public const string DwmRegistryPath = @"Software\Microsoft\Windows\DWM";

        /// <summary>
        /// The Explorer accent key holding the accent palette.
        /// </summary>
        public const string AccentRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

        /// <summary>
        /// The Explorer advanced key holding the snap-assist flyout flag.
        /// </summary>
        public const string ExplorerAdvancedRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

        // ---------------------------------------------------------------------
        // Registry value names.
        // ---------------------------------------------------------------------

        /// <summary>
        /// <c>AppsUseLightTheme</c>.
        /// </summary>
        public const string AppsUseLightTheme = "AppsUseLightTheme";

        /// <summary>
        /// <c>SystemUsesLightTheme</c>.
        /// </summary>
        public const string SystemUsesLightTheme = "SystemUsesLightTheme";

        /// <summary>
        /// <c>ColorPrevalence</c>.
        /// </summary>
        public const string ColorPrevalence = "ColorPrevalence";

        /// <summary>
        /// <c>AccentPalette</c>.
        /// </summary>
        public const string AccentPalette = "AccentPalette";

        /// <summary>
        /// <c>AccentColor</c>.
        /// </summary>
        public const string AccentColor = "AccentColor";

        /// <summary>
        /// <c>AccentColorInactive</c>.
        /// </summary>
        public const string AccentColorInactive = "AccentColorInactive";

        /// <summary>
        /// <c>ColorizationColor</c>.
        /// </summary>
        public const string ColorizationColor = "ColorizationColor";

        /// <summary>
        /// <c>ColorizationColorBalance</c>.
        /// </summary>
        public const string ColorizationColorBalance = "ColorizationColorBalance";

        /// <summary>
        /// <c>CurrentTheme</c>.
        /// </summary>
        public const string CurrentTheme = "CurrentTheme";

        /// <summary>
        /// <c>EnableSnapAssistFlyout</c>.
        /// </summary>
        public const string EnableSnapAssistFlyout = "EnableSnapAssistFlyout";
    }
}
