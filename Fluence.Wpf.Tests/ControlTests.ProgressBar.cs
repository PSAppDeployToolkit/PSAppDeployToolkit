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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Media;
using FluenceProgressBar = Fluence.Wpf.Controls.ProgressBar;
using WpfBorder = System.Windows.Controls.Border;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void ProgressBar_PausedMode_UsesCautionBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Paused, "SystemFillColorCautionBrush");
        }

        [TestMethod]
        public void ProgressBar_PausedMode_TracksCautionBrushAcrossThemeChange()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = ProgressBarMode.Paused
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");
                SolidColorBrush? initial = fill.Background as SolidColorBrush;
                Assert.IsNotNull(initial, "PART_Fill.Background should be a SolidColorBrush.");
                Color initialColor = initial.Color;

                SolidColorBrush? initialExpected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.IsNotNull(initialExpected, "SystemFillColorCautionBrush must resolve in light theme.");
                Assert.AreEqual(initialExpected.Color, initialColor, "Paused ProgressBar should start on the caution brush.");

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorCautionBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "SystemFillColorCautionBrush must resolve after theme change.");
                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should remain a SolidColorBrush after theme change.");
                Assert.AreEqual(expected.Color, actual.Color, "Paused ProgressBar should track the current caution brush.");
                Assert.AreNotEqual(initialColor, actual.Color, "Paused ProgressBar fill should change when the theme caution brush changes.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_ErrorMode_UsesCriticalBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Error, "SystemFillColorCriticalBrush");
        }

        [TestMethod]
        public void ProgressBar_DefaultStyle_UsesFourPixelTrackHeight()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? track = FindVisualChildByName<WpfBorder>(progressBar, "PART_Track");
                Assert.IsNotNull(track, "ProgressBar template must expose PART_Track.");

                Assert.AreEqual(4.0, progressBar.TrackHeight, 0.1,
                    "ProgressBar default style should set a 4px track height, close to the WinUI 3 reference (ProgressBarMinHeight = 3).");
                Assert.AreEqual(4.0, track.Height, 0.1,
                    "ProgressBar track template height should follow the 4px TrackHeight.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_ReturningToStandardMode_RestoresAccentBrush()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = ProgressBarMode.Error
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");

                progressBar.ProgressMode = ProgressBarMode.Standard;
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "AccentFillColorDefaultBrush must resolve.");
                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, actual.Color,
                    "ProgressBar should restore the accent brush when returning to Standard mode.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressBar_Track_IsClippedToRoundedGeometry()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    ProgressMode = ProgressBarMode.Indeterminate
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? track = FindVisualChildByName<WpfBorder>(progressBar, "PART_Track");
                Assert.IsNotNull(track, "ProgressBar template must expose PART_Track.");

                // ClipToBounds only clips rectangularly, so the translating indeterminate bars would
                // show square ends at the track edge. The control must install a rounded RectangleGeometry
                // clip matching CornerRadius so every fill/indeterminate child conforms to the rounded track.
                RectangleGeometry? clip = track.Clip as RectangleGeometry;
                Assert.IsNotNull(clip, "PART_Track must carry a rounded RectangleGeometry clip (not just ClipToBounds).");
                Assert.AreEqual(progressBar.CornerRadius.TopLeft, clip.RadiusX, 0.01,
                    "Track clip corner radius X must match the control CornerRadius.");
                Assert.AreEqual(progressBar.CornerRadius.TopLeft, clip.RadiusY, 0.01,
                    "Track clip corner radius Y must match the control CornerRadius.");
                Assert.IsTrue(clip.Rect.Width > 0 && clip.Rect.Height > 0,
                    "Track clip must be sized to the realised track bounds.");

                w.Close();
            });
        }

        private static void AssertProgressBarModeBrush(ProgressBarMode mode, string brushKey)
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = mode
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");

                SolidColorBrush? expected = app?.TryFindResource(brushKey) as SolidColorBrush;
                Assert.IsNotNull(expected, brushKey + " must resolve.");

                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, actual.Color, "ProgressBar fill should use the requested state brush.");

                w.Close();
            });
        }
    }
}
