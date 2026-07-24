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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class SnapLayoutHelperTests
    {
        // The helper reads HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\EnableSnapAssistFlyout.
        // We save the live user value, exercise each fixture path, then restore. Cleanup is in the finally
        // block of each test so a thrown assertion still restores the user's prior state.

        private const string KeyPath = NativeConstants.ExplorerAdvancedRegistryPath;
        private const string ValueName = NativeConstants.EnableSnapAssistFlyout;

        [TestMethod]
        public void IsSnapLayoutEnabled_RegistryAbsent_ReturnsTrue()
        {
            object? saved = ReadRaw();
            try
            {
                DeleteValue();
                bool actual = SnapLayoutHelper.IsSnapLayoutEnabled();
                Assert.IsTrue(actual, "Default Win11 behavior is enabled when registry value is absent.");
            }
            finally
            {
                RestoreRaw(saved);
            }
        }

        [TestMethod]
        public void IsSnapLayoutEnabled_RegistryZero_ReturnsFalse()
        {
            object? saved = ReadRaw();
            try
            {
                WriteInt(0);
                bool actual = SnapLayoutHelper.IsSnapLayoutEnabled();
                Assert.IsFalse(actual, "Value of 0 means user opted out.");
            }
            finally
            {
                RestoreRaw(saved);
            }
        }

        [TestMethod]
        public void IsSnapLayoutEnabled_RegistryOne_ReturnsTrue()
        {
            object? saved = ReadRaw();
            try
            {
                WriteInt(1);
                bool actual = SnapLayoutHelper.IsSnapLayoutEnabled();
                Assert.IsTrue(actual, "Value of 1 means user opted in.");
            }
            finally
            {
                RestoreRaw(saved);
            }
        }

        private static object? ReadRaw()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName);
        }

        private static void RestoreRaw(object? saved)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
            if (saved is null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            else if (saved is int intValue)
            {
                key.SetValue(ValueName, intValue, RegistryValueKind.DWord);
            }
            else
            {
                key.SetValue(ValueName, saved);
            }
        }

        private static void WriteInt(int value)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
            key.SetValue(ValueName, value, RegistryValueKind.DWord);
        }

        private static void DeleteValue()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
