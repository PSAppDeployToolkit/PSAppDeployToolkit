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

using System.Windows;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class CaptionButtonChromeTests
    {
        [Fact]
        public void Minimize_NoResize_Hides()
        {
            CaptionButtonChrome.GetMinimizeChrome(
                ResizeMode.NoResize,
                out Visibility vis,
                out bool en);

            Assert.Equal(Visibility.Collapsed, vis);
            Assert.False(en);
        }

        [Fact]
        public void Minimize_CanResize_ShowsEnabled()
        {
            CaptionButtonChrome.GetMinimizeChrome(
                ResizeMode.CanResize,
                out Visibility vis,
                out bool en);

            Assert.Equal(Visibility.Visible, vis);
            Assert.True(en);
        }

        [Fact]
        public void MaximizeRestore_CanResize_Normal_ShowsMaximizeOnly()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanResize,
                WindowState.Normal,
                out Visibility maxVis,
                out Visibility restVis,
                out bool maxEn,
                out bool restEn);

            Assert.Equal(Visibility.Visible, maxVis);
            Assert.Equal(Visibility.Collapsed, restVis);
            Assert.True(maxEn);
            Assert.False(restEn);
        }

        [Fact]
        public void MaximizeRestore_CanResize_Maximized_ShowsRestoreOnly()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanResize,
                WindowState.Maximized,
                out Visibility maxVis,
                out Visibility restVis,
                out bool maxEn,
                out bool restEn);

            Assert.Equal(Visibility.Collapsed, maxVis);
            Assert.Equal(Visibility.Visible, restVis);
            Assert.False(maxEn);
            Assert.True(restEn);
        }

        [Fact]
        public void MaximizeRestore_CanMinimize_DisablesBoth()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanMinimize,
                WindowState.Normal,
                out Visibility maxVis,
                out _,
                out bool maxEn,
                out bool restEn);

            Assert.Equal(Visibility.Visible, maxVis);
            Assert.False(maxEn);
            Assert.False(restEn);
        }

        [Fact]
        public void Close_VisibleAndEnabled()
        {
            CaptionButtonChrome.GetCloseChrome(
                out Visibility vis,
                out bool en);

            Assert.Equal(Visibility.Visible, vis);
            Assert.True(en);
        }
    }
}
