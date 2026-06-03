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

using Fluence.Wpf.Native;
using Microsoft.Win32;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Probes the per-user Windows 11 Snap Layout flyout preference. The flyout appears
    /// when the user hovers a window's maximize button and Windows reports the hit-region
    /// as <c>HTMAXBUTTON</c>. Without the preference check, returning <c>HTMAXBUTTON</c>
    /// shows the flyout even for users who disabled it in Settings.
    /// </summary>
    internal static class SnapLayoutHelper
    {
        /// <summary>
        /// Returns <see langword="true"/> when the user has the Windows 11 Snap Layout flyout
        /// enabled. Defaults to <see langword="true"/> when the registry value is absent (the
        /// Windows 11 default), so the flyout is shown unless the user explicitly turned it off.
        /// </summary>
        /// <remarks>
        /// The preference is stored under
        /// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced</c>
        /// as <c>EnableSnapAssistFlyout</c> (DWORD). A missing value, a non-DWORD value, or any
        /// value other than 0 is treated as enabled.
        /// </remarks>
        /// <returns>
        ///   <see langword="true"/> when the Snap Layout flyout is enabled or not configured;
        ///   <see langword="false"/> when the user has explicitly disabled it.
        /// </returns>
        internal static bool IsSnapLayoutEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.ExplorerAdvancedRegistryPath);
            return key?.GetValue(NativeConstants.EnableSnapAssistFlyout) is not int value || value != 0;
        }
    }
}
