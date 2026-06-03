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
using System.Runtime.InteropServices;

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// Mirrors the undocumented DWM colorization parameters returned by the ordinal-127 export of
    /// <c>dwmapi.dll</c>. Used to read the live glass/colorization color when the registry value is
    /// unavailable. Field order and types must match the native layout exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DWMCOLORIZATIONPARAMS
    {
        /// <summary>The primary colorization color as a packed <c>ARGB</c> value.</summary>
        public uint clrColor;

        /// <summary>The after-glow color.</summary>
        public uint clrAfterGlow;

        /// <summary>The colorization intensity.</summary>
        public uint nIntensity;

        /// <summary>The after-glow color balance.</summary>
        public uint clrAfterGlowBalance;

        /// <summary>The blur color balance.</summary>
        public uint clrBlurBalance;

        /// <summary>The glass reflection intensity.</summary>
        public uint clrGlassReflectionIntensity;

        /// <summary><see langword="true"/> when the colorization is opaque.</summary>
        public bool fOpaque;
    }

    /// <summary>
    /// The Win32 <c>MARGINS</c> structure passed to <c>DwmExtendFrameIntoClientArea</c>. A value of
    /// <c>-1</c> on every edge requests the "sheet of glass" full-frame extension.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MARGINS
    {
        /// <summary>Width of the left border that retains its size.</summary>
        public int cxLeftWidth;

        /// <summary>Width of the right border that retains its size.</summary>
        public int cxRightWidth;

        /// <summary>Height of the top border that retains its size.</summary>
        public int cyTopHeight;

        /// <summary>Height of the bottom border that retains its size.</summary>
        public int cyBottomHeight;

        /// <summary>Initializes the four margins independently.</summary>
        /// <param name="left">The left margin width.</param>
        /// <param name="right">The right margin width.</param>
        /// <param name="top">The top margin height.</param>
        /// <param name="bottom">The bottom margin height.</param>
        public MARGINS(int left, int right, int top, int bottom)
        {
            cxLeftWidth = left;
            cxRightWidth = right;
            cyTopHeight = top;
            cyBottomHeight = bottom;
        }

        /// <summary>Initializes all four margins to the same value.</summary>
        /// <param name="allMargins">The value applied to every edge.</param>
        public MARGINS(int allMargins)
        {
            cxLeftWidth = allMargins;
            cxRightWidth = allMargins;
            cyTopHeight = allMargins;
            cyBottomHeight = allMargins;
        }
    }

    /// <summary>
    /// The Win32 <c>RECT</c> structure (left, top, right, bottom in pixels). Right and bottom are
    /// exclusive, matching the Win32 convention.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        /// <summary>The x-coordinate of the left edge.</summary>
        public int Left;

        /// <summary>The y-coordinate of the top edge.</summary>
        public int Top;

        /// <summary>The x-coordinate of the (exclusive) right edge.</summary>
        public int Right;

        /// <summary>The y-coordinate of the (exclusive) bottom edge.</summary>
        public int Bottom;

        /// <summary>Gets the width of the rectangle.</summary>
        public readonly int Width => Right - Left;

        /// <summary>Gets the height of the rectangle.</summary>
        public readonly int Height => Bottom - Top;
    }

    /// <summary>
    /// The Win32 <c>POINT</c> structure (two 4-byte integers). Five of these compose
    /// <see cref="MINMAXINFO"/>; the 8-byte size is part of that struct's pinned 40-byte layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        /// <summary>The x-coordinate.</summary>
        public int X;

        /// <summary>The y-coordinate.</summary>
        public int Y;
    }

    /// <summary>
    /// The Win32 <c>MINMAXINFO</c> structure delivered with <c>WM_GETMINMAXINFO</c>. It is exactly
    /// five <see cref="POINT"/> values (40 bytes); the maximized size and position are clamped to
    /// the target monitor before WPF maximizes the window.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MINMAXINFO
    {
        /// <summary>Reserved; do not use.</summary>
        public POINT ptReserved;

        /// <summary>The maximized width and height.</summary>
        public POINT ptMaxSize;

        /// <summary>The position of the top-left corner when maximized.</summary>
        public POINT ptMaxPosition;

        /// <summary>The minimum tracking width and height.</summary>
        public POINT ptMinTrackSize;

        /// <summary>The maximum tracking width and height.</summary>
        public POINT ptMaxTrackSize;
    }

    /// <summary>
    /// The Win32 <c>MONITORINFO</c> structure (40 bytes: a size field, the full monitor rectangle,
    /// the work-area rectangle, and a flags field). <see cref="cbSize"/> must be initialized to the
    /// struct size before calling <c>GetMonitorInfo</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MONITORINFO
    {
        /// <summary>The size of the structure in bytes; set before the call.</summary>
        public int cbSize;

        /// <summary>The full bounding rectangle of the monitor.</summary>
        public RECT rcMonitor;

        /// <summary>The work-area rectangle (the monitor minus the taskbar and app bars).</summary>
        public RECT rcWork;

        /// <summary>Monitor flags (for example the primary-monitor bit).</summary>
        public uint dwFlags;
    }

    /// <summary>
    /// The Win32 <c>RTL_OSVERSIONINFOEXW</c> structure filled by <c>RtlGetVersion</c>. Unlike the
    /// shimmed <c>GetVersionEx</c>, <c>RtlGetVersion</c> reports the true OS build, which the DWM
    /// feature gates depend on.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct OSVERSIONINFOEX
    {
        /// <summary>The size of the structure in bytes; set before the call.</summary>
        public int OSVersionInfoSize;

        /// <summary>The major OS version.</summary>
        public int MajorVersion;

        /// <summary>The minor OS version.</summary>
        public int MinorVersion;

        /// <summary>The OS build number (the value the DWM feature gates key off).</summary>
        public int BuildNumber;

        /// <summary>The platform identifier.</summary>
        public int Revision;

        /// <summary>The OS platform id.</summary>
        public int PlatformId;

        /// <summary>The service-pack descriptor string.</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string CSDVersion;

        /// <summary>The major service-pack version.</summary>
        public ushort ServicePackMajor;

        /// <summary>The minor service-pack version.</summary>
        public ushort ServicePackMinor;

        /// <summary>The suite mask.</summary>
        public short SuiteMask;

        /// <summary>The product type.</summary>
        public byte ProductType;

        /// <summary>Reserved.</summary>
        public byte Reserved;
    }

    /// <summary>
    /// The UxTheme <c>WTA_OPTIONS</c> structure passed to <c>SetWindowThemeAttribute</c>. The
    /// <see cref="Mask"/> selects which <c>WTNCA_*</c> bits in <see cref="Flags"/> are applied.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WTA_OPTIONS
    {
        /// <summary>The <c>WTNCA_*</c> flags to set or clear.</summary>
        public uint Flags;

        /// <summary>The mask selecting which bits of <see cref="Flags"/> are honored.</summary>
        public uint Mask;
    }

    /// <summary>
    /// The shell <c>APPBARDATA</c> structure passed to <c>SHAppBarMessage</c>. <see cref="cbSize"/>
    /// must be initialized to the struct size before the call.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct APPBARDATA
    {
        /// <summary>The size of the structure in bytes; set before the call.</summary>
        public int cbSize;

        /// <summary>The handle of the appbar window (unused for the query messages here).</summary>
        public IntPtr hWnd;

        /// <summary>The callback-message id (unused for the query messages here).</summary>
        public uint uCallbackMessage;

        /// <summary>The screen edge the taskbar is docked to (one of the <c>ABE_*</c> values).</summary>
        public uint uEdge;

        /// <summary>The bounding rectangle of the appbar.</summary>
        public RECT rc;

        /// <summary>A message-specific value.</summary>
        public IntPtr lParam;
    }
}
