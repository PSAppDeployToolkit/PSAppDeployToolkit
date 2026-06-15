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

using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 A5-A6 tests: ComboBox and DropDownButton popup border CornerRadius
    /// tracks <c>OverlayCornerRadius</c> (8px) via DynamicResource.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 A5  ComboBox popup CornerRadius
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ComboBox_DropdownCornerRadius_DefaultEqualsOverlayCornerRadius()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");

                ComboBox cb = new();
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(overlayRadius, cb.DropdownCornerRadius,
                    "ComboBox.DropdownCornerRadius must equal OverlayCornerRadius after default style applies.");
                w.Close();
            });
        }

        [TestMethod]
        public void ComboBox_DropdownCornerRadius_ValueIs8()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ComboBox cb = new();
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(new CornerRadius(8), cb.DropdownCornerRadius,
                    "ComboBox.DropdownCornerRadius must be 8 (OverlayCornerRadius).");
                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // WI-3 A6  DropDownButton popup CornerRadius
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void DropDownButton_DropdownCornerRadius_DefaultEqualsOverlayCornerRadius()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");

                DropDownButton ddb = new();
                Window w = new() { Content = ddb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(overlayRadius, ddb.DropdownCornerRadius,
                    "DropDownButton.DropdownCornerRadius must equal OverlayCornerRadius after default style applies.");
                w.Close();
            });
        }

        [TestMethod]
        public void DropDownButton_DropdownCornerRadius_ValueIs8()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                DropDownButton ddb = new();
                Window w = new() { Content = ddb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(new CornerRadius(8), ddb.DropdownCornerRadius,
                    "DropDownButton.DropdownCornerRadius must be 8 (OverlayCornerRadius).");
                w.Close();
            });
        }
    }
}
