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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fluence.Wpf.Tests
{
    // FluenceWindow defaults Window.Icon to the Fluence brand icon embedded in Fluence.Wpf.dll.
    // FluenceWindow.CreateDefaultIcon loads the square, no-background Fluence mark (an embedded
    // 256x256 PNG) once as a frozen BitmapImage, because Window.Icon drives the Win32 taskbar HICON,
    // which does not render a vector and would distort a non-square source. These tests pin both
    // halves of the contract: the default resolves to a real square BitmapSource, and a
    // consumer-assigned Icon overrides that default instead of being clobbered by it.
    public partial class ControlTests
    {
        [TestMethod]
        public void FluenceWindow_Icon_DefaultsToEmbeddedFluenceBrandIcon()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow window = new();
                try
                {
                    Assert.IsNotNull(window.Icon,
                        "FluenceWindow should default Icon to the embedded Fluence brand icon.");
                    Assert.IsInstanceOfType(window.Icon, typeof(BitmapSource),
                        "The default Icon must be a BitmapSource (the embedded square PNG), which proves " +
                        "the icon resource resolves and CreateDefaultIcon loads it for the Win32 HICON.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_Icon_ConsumerAssignedValueOverridesDefault()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow window = new();
                try
                {
                    // A 1x1 in-memory bitmap stands in for a consumer-supplied icon so the test
                    // does not depend on any external resource.
                    ImageSource consumerIcon = BitmapSource.Create(
                        1, 1, 96, 96, PixelFormats.Bgra32, palette: null, new byte[] { 0, 0, 0, 255 }, 4);
                    consumerIcon.Freeze();

                    window.Icon = consumerIcon;

                    Assert.AreSame(consumerIcon, window.Icon,
                        "A consumer-assigned Icon must override the embedded default and persist.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_DefaultIcon_IsSquareBitmapSource()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                Assert.IsNotNull(FluenceWindow.DefaultIcon,
                    "FluenceWindow.DefaultIcon should expose the embedded Fluence brand icon.");
                Assert.IsInstanceOfType(FluenceWindow.DefaultIcon, typeof(BitmapSource),
                    "DefaultIcon must be a BitmapSource so a consumer can apply it as a Win32 HICON, " +
                    "which the vector DrawingImage resource cannot reliably drive.");

                BitmapSource icon = (BitmapSource)FluenceWindow.DefaultIcon;
                Assert.AreEqual(icon.PixelWidth, icon.PixelHeight,
                    "The default icon must be square (no aspect distortion); the no-background source is an exact square.");
            });
        }
    }
}
