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
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Helpers
{
    internal static class RegistryHelper
    {
        internal static bool GetAppsUseLightTheme()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.PersonalizeRegistryPath);
            return key?.GetValue(NativeConstants.AppsUseLightTheme) is not int intValue || intValue != 0;
        }

        internal static bool GetSystemUsesLightTheme()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.PersonalizeRegistryPath);
            return key?.GetValue(NativeConstants.SystemUsesLightTheme) is not int intValue || intValue != 0;
        }

        internal static bool GetColorPrevalence()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath);
            return key?.GetValue(NativeConstants.ColorPrevalence) is not int intValue || intValue != 0;
        }

        internal static bool TryGetAccentPalette(out Color[]? palette)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.AccentRegistryPath);
            if (key?.GetValue(NativeConstants.AccentPalette) is byte[] bytes && bytes.Length >= 32)
            {
                palette = new Color[8];
                for (int i = 0; i < 8; i++)
                {
                    int offset = i * 4;
                    byte r = bytes[offset];
                    byte g = bytes[offset + 1];
                    byte b = bytes[offset + 2];
                    byte a = bytes[offset + 3];
                    palette[i] = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                }
                return true;
            }
            palette = null;
            return false;
        }

        internal static Color GetAccentColor()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.AccentRegistryPath);
            if (key?.GetValue(NativeConstants.AccentColor) is int intValue)
            {
                uint color = unchecked((uint)intValue);
                byte a = (byte)((color >> 24) & 0xFF);
                byte b = (byte)((color >> 16) & 0xFF);
                byte g = (byte)((color >> 8) & 0xFF);
                byte r = (byte)(color & 0xFF);
                return Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
            }
            return Color.FromRgb(0x00, 0x78, 0xD4);
        }

        internal static bool IsHighContrastEnabled()
        {
            return SystemParameters.HighContrast;
        }

        /// <summary>
        /// Reads the active Windows theme file name from <c>HKCU\...\Themes\CurrentTheme</c>,
        /// strips the directory and extension, and returns the lowercased base name. Returns
        /// <see langword="null"/> when the value is missing or empty. Used by <c>ResolveTheme</c> as a
        /// defensive dual-fallback ahead of <c>AppsUseLightTheme</c> so that named Windows 11
        /// themes (e.g. <c>themea.theme</c>) and high-contrast variants are recognised.
        /// </summary>
        internal static string? GetCurrentThemeFileNameLowerInvariant()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.ThemesRegistryPath);
            if (key?.GetValue(NativeConstants.CurrentTheme) is not string fullPath || string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }
            string fileName = System.IO.Path.GetFileNameWithoutExtension(fullPath);
            return string.IsNullOrWhiteSpace(fileName) ? null : fileName.ToLowerInvariant();
        }

        /// <summary>
        /// Reads DWM AccentColor (ABGR DWORD) used for the active titlebar when ColorPrevalence is on.
        /// </summary>
        /// <param name="color">The accent color as a <see cref="Color"/> struct. Returns transparent black if the registry value is missing or invalid.</param>
        internal static bool TryGetDwmAccentColor(out Color color)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath);
            if (key?.GetValue(NativeConstants.AccentColor) is int intValue)
            {
                uint raw = unchecked((uint)intValue);
                byte a = (byte)((raw >> 24) & 0xFF);
                byte b = (byte)((raw >> 16) & 0xFF);
                byte g = (byte)((raw >> 8) & 0xFF);
                byte r = (byte)(raw & 0xFF);
                color = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                return true;
            }
            color = default;
            return false;
        }

        /// <summary>
        /// Reads DWM AccentColorInactive (ABGR DWORD) for the inactive titlebar.
        /// </summary>
        /// <param name="color">The accent color as a <see cref="Color"/> struct. Returns transparent black if the registry value is missing or invalid.</param>
        internal static bool TryGetDwmAccentColorInactive(out Color color)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath);
            if (key?.GetValue(NativeConstants.AccentColorInactive) is int intValue)
            {
                uint raw = unchecked((uint)intValue);
                byte a = (byte)((raw >> 24) & 0xFF);
                byte b = (byte)((raw >> 16) & 0xFF);
                byte g = (byte)((raw >> 8) & 0xFF);
                byte r = (byte)(raw & 0xFF);
                color = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                return true;
            }
            color = default;
            return false;
        }

        /// <summary>
        /// Reads DWM ColorizationColor (ARGB) and ColorizationColorBalance for Win10 border blending.
        /// </summary>
        /// <param name="colorizationColor">The colorization color as a <see cref="Color"/> struct. Returns transparent black if the registry value is missing or invalid.</param>
        /// <param name="balance">The colorization balance as an <see cref="int"/>. Returns 0 if the registry value is missing or invalid.</param>
        internal static bool TryGetColorizationBalance(out Color colorizationColor, out int balance)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath);
            if (key?.GetValue(NativeConstants.ColorizationColor) is int colorInt && key?.GetValue(NativeConstants.ColorizationColorBalance) is int balanceInt)
            {
                uint raw = unchecked((uint)colorInt);
                byte a = (byte)((raw >> 24) & 0xFF);
                byte r = (byte)((raw >> 16) & 0xFF);
                byte g = (byte)((raw >> 8) & 0xFF);
                byte b = (byte)(raw & 0xFF);
                colorizationColor = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                balance = balanceInt;
                return true;
            }
            colorizationColor = default;
            balance = 0;
            return false;
        }
    }
}
