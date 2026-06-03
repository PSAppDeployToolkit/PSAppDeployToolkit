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

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// Theme choices shown on the demo Settings page.
    /// </summary>
    public enum SettingsThemeOption
    {
        /// <summary>Follow the operating system app theme.</summary>
        System,

        /// <summary>Use the light application theme.</summary>
        Light,

        /// <summary>Use the dark application theme.</summary>
        Dark,

        /// <summary>Use the high contrast application theme.</summary>
        HighContrast
    }

    /// <summary>
    /// Navigation layout choices shown on the demo Settings page.
    /// </summary>
    public enum SettingsNavigationOption
    {
        /// <summary>Use a horizontal top navigation strip.</summary>
        Top,

        /// <summary>Use the expanded left navigation pane.</summary>
        Left,

        /// <summary>Use the compact left navigation pane.</summary>
        LeftCompact
    }

    /// <summary>
    /// Backdrop choices shown on the demo Settings page.
    /// </summary>
    public enum SettingsBackdropOption
    {
        /// <summary>Let Fluence choose the best available backdrop.</summary>
        Auto,

        /// <summary>Use the Mica backdrop.</summary>
        Mica,

        /// <summary>Use the Acrylic backdrop.</summary>
        Acrylic,

        /// <summary>Use the tabbed Mica backdrop.</summary>
        Tabbed,

        /// <summary>Use a solid window background.</summary>
        None
    }
}
