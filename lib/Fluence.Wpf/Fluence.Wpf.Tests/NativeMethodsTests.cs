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
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Native;
using Windows.Win32;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Pins the pure, handle-free interop selectors in <see cref="NativeMethods"/>:
    /// the immersive dark-mode attribute split (19 vs 20), the auto-hide taskbar
    /// maximized-rect shift, the maximized resize-frame margin conversion, and the
    /// accent-policy color packing. These tests
    /// are deterministic and OS-independent; they do not call any P/Invoke whose result
    /// depends on the host environment, so neither live margin reader
    /// (<see cref="NativeMethods.GetMaximizedFrameMargin(double, double)"/> and
    /// <see cref="NativeMethods.GetMaximizedFrameMargin(IntPtr)"/>) is covered here; both funnel
    /// their metrics through the pure conversion that is.
    /// </summary>
    public sealed class NativeMethodsTests
    {
        private const double Tolerance = 1e-9;

        [Fact]
        public void GetImmersiveDarkModeAttribute_Returns20_For18985AndLater()
        {
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(18985));
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(19041));
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(22000));
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(22631));
        }

        [Fact]
        public void GetImmersiveDarkModeAttribute_Returns19_ForPre18985Builds()
        {
            const DWMWINDOWATTRIBUTE DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = (DWMWINDOWATTRIBUTE)19;
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(17763));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18000));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18361));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18362));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18363));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18984));
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Left_MovesRightAndShrinksWidth()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_LEFT);

            Assert.Equal(102, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(798, mmi.ptMaxSize.X);
            Assert.Equal(600, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Top_MovesDownAndShrinksHeight()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_TOP);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(202, mmi.ptMaxPosition.Y);
            Assert.Equal(800, mmi.ptMaxSize.X);
            Assert.Equal(598, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Right_ShrinksWidthOnly()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_RIGHT);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(798, mmi.ptMaxSize.X);
            Assert.Equal(600, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Bottom_ShrinksHeightOnly()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_BOTTOM);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(800, mmi.ptMaxSize.X);
            Assert.Equal(598, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_UnrecognizedEdge_LeavesRectUnchanged()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, 99);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(800, mmi.ptMaxSize.X);
            Assert.Equal(600, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_At100Percent_SumsSizeFrameAndPaddedBorder()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 1.0, 1.0);

            Assert.Equal(8.0, margin.Left);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Right);
            Assert.Equal(8.0, margin.Bottom);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_TakesHorizontalFromXAndVerticalFromYMetrics()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 7, 4, 1.0, 1.0);

            Assert.Equal(8.0, margin.Left);
            Assert.Equal(8.0, margin.Right);
            Assert.Equal(11.0, margin.Top);
            Assert.Equal(11.0, margin.Bottom);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_At150Percent_DividesByScale()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 1.5, 1.5);

            const double expected = 16.0 / 3.0;
            Assert.Equal(expected, margin.Left, Tolerance);
            Assert.Equal(expected, margin.Top, Tolerance);
            Assert.Equal(expected, margin.Right, Tolerance);
            Assert.Equal(expected, margin.Bottom, Tolerance);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_ScalesEachAxisIndependently()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 2.0, 1.0);

            Assert.Equal(4.0, margin.Left);
            Assert.Equal(4.0, margin.Right);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Bottom);
        }

        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(-1.5, -2.0)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        public void ComputeMaximizedFrameMargin_NonPositiveScale_TreatedAsUnscaled(double dpiScaleX, double dpiScaleY)
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, dpiScaleX, dpiScaleY);

            Assert.Equal(8.0, margin.Left);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Right);
            Assert.Equal(8.0, margin.Bottom);
        }

        /// <summary>
        /// The DPI-aware reader pairs metrics taken at the window's own DPI with that same DPI as
        /// the scale, so a 150% monitor reports 150% metrics and the two cancel back to the inset a
        /// 100% monitor gets. Pinning it here is what makes mixing a system-DPI metric with a
        /// per-monitor scale visibly wrong: at 4/4/4 against a 1.5 scale the same call yields 5.33
        /// DIPs instead of 8.
        /// </summary>
        [Fact]
        public void ComputeMaximizedFrameMargin_MetricsAndScaleFromTheSameDpi_MatchTheUnscaledInset()
        {
            Thickness at96 = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 1.0, 1.0);
            Thickness at144 = NativeMethods.ComputeMaximizedFrameMargin(6, 6, 6, 1.5, 1.5);

            Assert.Equal(at96.Left, at144.Left, Tolerance);
            Assert.Equal(at96.Top, at144.Top, Tolerance);
            Assert.Equal(at96.Right, at144.Right, Tolerance);
            Assert.Equal(at96.Bottom, at144.Bottom, Tolerance);
            Assert.Equal(8.0, at144.Left, Tolerance);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_GuardsOnlyTheFailingAxis()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 2.0, 0.0);

            Assert.Equal(4.0, margin.Left);
            Assert.Equal(4.0, margin.Right);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Bottom);
        }

        [Fact]
        public void ColorToAbgr_PacksAlphaBlueGreenRed()
        {
            // #80402010 is A=0x80 R=0x40 G=0x20 B=0x10; the accent policy wants 0xAABBGGRR, so the
            // red and blue bytes swap places relative to the source ARGB.
            uint packed = NativeMethods.ColorToAbgr(Color.FromArgb(0x80, 0x40, 0x20, 0x10));

            Assert.Equal(0x80102040u, packed);
        }

        [Fact]
        public void ColorToAbgr_PreservesAlpha_UnlikeColorToColorRef()
        {
            // The DWM COLORREF packer drops alpha; the accent-policy packer must not, because the
            // alpha is the tint opacity over the blurred desktop.
            Color tint = Color.FromArgb(0xF0, 0xF9, 0xF9, 0xF9);

            Assert.Equal(0xF0F9F9F9u, NativeMethods.ColorToAbgr(tint));
            Assert.Equal(0x00F9F9F9u, NativeMethods.ColorToColorRef(tint));
        }

        [Fact]
        public void ColorToAbgr_FullyOpaqueWhite_SetsEveryByte()
        {
            Assert.Equal(0xFFFFFFFFu, NativeMethods.ColorToAbgr(Colors.White));
        }

        [Fact]
        public void ColorToAbgr_TransparentBlack_IsZero()
        {
            Assert.Equal(0u, NativeMethods.ColorToAbgr(Color.FromArgb(0, 0, 0, 0)));
        }

        private static MINMAXINFO SeedMinMaxInfo()
        {
            MINMAXINFO mmi = default;
            mmi.ptMaxPosition.X = 100;
            mmi.ptMaxPosition.Y = 200;
            mmi.ptMaxSize.X = 800;
            mmi.ptMaxSize.Y = 600;
            return mmi;
        }
    }
}
