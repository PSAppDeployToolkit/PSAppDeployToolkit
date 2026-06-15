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
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Resolves an <see cref="ApplicationTheme"/> request (including <see cref="ApplicationTheme.Auto"/>)
    /// to a concrete Light, Dark, or HighContrast value by inspecting OS registry state.
    /// </summary>
    internal static class ThemeResolver
    {
        /// <summary>
        /// Returns the concrete <see cref="ApplicationTheme"/> for <paramref name="theme"/>.
        /// When <paramref name="theme"/> is <see cref="ApplicationTheme.Auto"/> the active Windows
        /// theme file and registry settings are consulted to determine Light, Dark, or HighContrast.
        /// </summary>
        /// <param name="theme">The theme to resolve.</param>
        internal static ApplicationTheme Resolve(ApplicationTheme theme)
        {
            if (theme != ApplicationTheme.Auto)
            {
                return theme;
            }

            // High contrast always wins, regardless of the active Windows theme file.
            if (RegistryHelper.IsHighContrastEnabled())
            {
                return ApplicationTheme.HighContrast;
            }

            // Dual fallback: first try HKCU\...\Themes\CurrentTheme filename so Windows 11
            // named themes (themea.theme ... themed.theme) and HC variants are recognised.
            // If the filename is unknown or absent, fall through to AppsUseLightTheme.
            string? themeFile = RegistryHelper.GetCurrentThemeFileNameLowerInvariant();
            if (themeFile?.Length > 0)
            {
                if (themeFile.Contains("hc1", StringComparison.Ordinal)
                    || themeFile.Contains("hc2", StringComparison.Ordinal)
                    || themeFile.Contains("hcblack", StringComparison.Ordinal)
                    || themeFile.Contains("hcwhite", StringComparison.Ordinal))
                {
                    // Defensive backstop in case SystemParameters.HighContrast is unset
                    // mid-transition: a Windows HC theme filename always means HighContrast.
                    return ApplicationTheme.HighContrast;
                }
                if (themeFile.Contains("dark", StringComparison.Ordinal))
                {
                    return ApplicationTheme.Dark;
                }
                if (themeFile.Contains("aero", StringComparison.Ordinal)
                    || themeFile.Contains("basic", StringComparison.Ordinal)
                    || themeFile.Contains("aerolite", StringComparison.Ordinal)
                    || themeFile.StartsWith("themea", StringComparison.Ordinal)
                    || themeFile.StartsWith("themeb", StringComparison.Ordinal)
                    || themeFile.StartsWith("themec", StringComparison.Ordinal)
                    || themeFile.StartsWith("themed", StringComparison.Ordinal))
                {
                    return ApplicationTheme.Light;
                }
            }

            return RegistryHelper.GetAppsUseLightTheme() ? ApplicationTheme.Light : ApplicationTheme.Dark;
        }
    }
}
