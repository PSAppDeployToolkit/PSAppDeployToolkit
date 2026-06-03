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

using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Resolves an <see cref="AccentIntent"/> to a concrete <see cref="AccentPalette"/>,
    /// preferring the OS registry palette, then generating from DWM color, then falling
    /// back to the default Windows blue.
    /// </summary>
    internal static class AccentResolver
    {
        private static readonly Color DefaultAccent = Color.FromRgb(0x00, 0x78, 0xD4);

        /// <summary>
        /// Returns an <see cref="AccentPalette"/> for the given <paramref name="intent"/>.
        /// </summary>
        internal static AccentPalette Resolve(AccentIntent intent)
        {
            if (intent.IsSystem && RegistryHelper.TryGetAccentPalette(out Color[]? p) && p is not null && p.Length >= 7)
            {
                // OS palette order: [Light3, Light2, Light1, Accent, Dark1, Dark2, Dark3, (8th reserved)].
                // The length guard is defensive: a short or malformed registry blob falls through to
                // the generated ramp rather than throwing IndexOutOfRangeException on the Apply hot path.
                return new AccentPalette(p[0], p[1], p[2], p[3], p[4], p[5], p[6]);
            }
            Color baseColor = intent.IsSystem ? GetDwmAccentOrDefault() : intent.Custom;
            return Generate(baseColor);
        }

        private static AccentPalette Generate(Color baseColor)
        {
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color l1, out Color l2, out Color l3, out Color d1, out Color d2, out Color d3);
            return new AccentPalette(l3, l2, l1, baseColor, d1, d2, d3);
        }

        private static Color GetDwmAccentOrDefault()
        {
            // DwmGetColorizationParameters is an undocumented DWM entry (#127). On builds where the
            // ordinal is absent or remapped the CLR raises EntryPointNotFoundException (or
            // DllNotFoundException if dwmapi is unavailable); on builds where it exists but rejects
            // the call it raises SEHException/COMException. All of these fall back to default blue
            // rather than escaping into the Apply hot path.
            try
            {
                NativeMethods.DwmGetColorizationParameters(out DWMCOLORIZATIONPARAMS prm);
                uint c = prm.clrColor;
                return Color.FromRgb((byte)((c >> 16) & 0xFF), (byte)((c >> 8) & 0xFF), (byte)(c & 0xFF));
            }
            catch (COMException)
            {
                return DefaultAccent;
            }
            catch (SEHException)
            {
                return DefaultAccent;
            }
            catch (EntryPointNotFoundException)
            {
                return DefaultAccent;
            }
            catch (DllNotFoundException)
            {
                return DefaultAccent;
            }
        }
    }
}
