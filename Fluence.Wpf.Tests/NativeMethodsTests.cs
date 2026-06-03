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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Pins the pure, handle-free interop selectors in <see cref="NativeMethods"/>:
    /// the immersive dark-mode attribute split (19 vs 20) and the auto-hide taskbar
    /// maximized-rect shift. These tests are deterministic and OS-independent; they do
    /// not call any P/Invoke whose result depends on the host environment.
    /// </summary>
    [TestClass]
    public sealed class NativeMethodsTests
    {
        [TestMethod]
        public void DwmCloakAttributeIds_MatchDwmApiContract()
        {
            // DWMWA_CLOAK (set) and DWMWA_CLOAKED (read-only) are fixed DWMWINDOWATTRIBUTE ordinals.
            // A typo here silently disables the first-paint flash guard, so pin the wire values.
            // Read via reflection so the assertion is a runtime comparison (MSTEST0032 rejects
            // comparing two compile-time constants).
            int cloak = ReadConstant("DWMWA_CLOAK");
            int cloaked = ReadConstant("DWMWA_CLOAKED");
            Assert.AreEqual(13, cloak, "DWMWA_CLOAK must be DWMWINDOWATTRIBUTE ordinal 13.");
            Assert.AreEqual(14, cloaked, "DWMWA_CLOAKED must be DWMWINDOWATTRIBUTE ordinal 14.");
        }

        private static int ReadConstant(string name)
        {
            System.Reflection.FieldInfo? field = typeof(NativeConstants).GetField(
                name,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(field, "Expected constant '" + name + "' on NativeConstants.");
            object? value = field.GetValue(null);
            Assert.IsNotNull(value, "Constant '" + name + "' must have a value.");
            return (int)value;
        }

        [TestMethod]
        public void GetImmersiveDarkModeAttribute_Returns20_For18362AndLater()
        {
            Assert.AreEqual(NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(18362));
            Assert.AreEqual(NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(19041));
            Assert.AreEqual(NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(22000));
            Assert.AreEqual(NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(22631));
        }

        [TestMethod]
        public void GetImmersiveDarkModeAttribute_Returns19_ForPre18362Builds()
        {
            Assert.AreEqual(NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(17763));
            Assert.AreEqual(NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18000));
            Assert.AreEqual(NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18361));
        }

        [TestMethod]
        public void ApplyAutoHideTaskbarShift_Left_MovesRightAndShrinksWidth()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, NativeConstants.ABE_LEFT);

            Assert.AreEqual(102, mmi.ptMaxPosition.X);
            Assert.AreEqual(200, mmi.ptMaxPosition.Y);
            Assert.AreEqual(798, mmi.ptMaxSize.X);
            Assert.AreEqual(600, mmi.ptMaxSize.Y);
        }

        [TestMethod]
        public void ApplyAutoHideTaskbarShift_Top_MovesDownAndShrinksHeight()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, NativeConstants.ABE_TOP);

            Assert.AreEqual(100, mmi.ptMaxPosition.X);
            Assert.AreEqual(202, mmi.ptMaxPosition.Y);
            Assert.AreEqual(800, mmi.ptMaxSize.X);
            Assert.AreEqual(598, mmi.ptMaxSize.Y);
        }

        [TestMethod]
        public void ApplyAutoHideTaskbarShift_Right_ShrinksWidthOnly()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, NativeConstants.ABE_RIGHT);

            Assert.AreEqual(100, mmi.ptMaxPosition.X);
            Assert.AreEqual(200, mmi.ptMaxPosition.Y);
            Assert.AreEqual(798, mmi.ptMaxSize.X);
            Assert.AreEqual(600, mmi.ptMaxSize.Y);
        }

        [TestMethod]
        public void ApplyAutoHideTaskbarShift_Bottom_ShrinksHeightOnly()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, NativeConstants.ABE_BOTTOM);

            Assert.AreEqual(100, mmi.ptMaxPosition.X);
            Assert.AreEqual(200, mmi.ptMaxPosition.Y);
            Assert.AreEqual(800, mmi.ptMaxSize.X);
            Assert.AreEqual(598, mmi.ptMaxSize.Y);
        }

        [TestMethod]
        public void ApplyAutoHideTaskbarShift_UnrecognizedEdge_LeavesRectUnchanged()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, 99);

            Assert.AreEqual(100, mmi.ptMaxPosition.X);
            Assert.AreEqual(200, mmi.ptMaxPosition.Y);
            Assert.AreEqual(800, mmi.ptMaxSize.X);
            Assert.AreEqual(600, mmi.ptMaxSize.Y);
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
