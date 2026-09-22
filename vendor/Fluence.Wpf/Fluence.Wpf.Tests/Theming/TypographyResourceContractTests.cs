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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public class TypographyResourceContractTests
    {
        /// <summary>
        /// ControlFastOutSlowInKeySpline must match the WinUI 3 canonical decelerate curve
        /// (Common_themeresources_any.xaml: P1=(0,0), P2=(0,1)). A prior value of 0.8,0,0,1
        /// disagreed with the comment above it, which already claimed the canonical curve.
        /// </summary>
        [Fact]
        public Task ControlFastOutSlowInKeySpline_MatchesWinUiCanonicalCurveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                try
                {
                    KeySpline spline = Assert.IsType<KeySpline>(application.TryFindResource("ControlFastOutSlowInKeySpline"));
                    Assert.Equal(0.0, spline.ControlPoint1.X, 0.0001);
                    Assert.Equal(0.0, spline.ControlPoint1.Y, 0.0001);
                    Assert.Equal(0.0, spline.ControlPoint2.X, 0.0001);
                    Assert.Equal(1.0, spline.ControlPoint2.Y, 0.0001);
                }
                finally
                {
                    application.Resources.MergedDictionaries.Clear();
                    application.Resources.Clear();
                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                }
            });
        }

        /// <summary>
        /// Code-built reveals cannot resolve a theme resource, so they ride
        /// MotionHelper.FastOutSlowInKeySpline, a by-value mirror of the published
        /// ControlFastOutSlowInKeySpline token. The mirror drifted once already: the token moved
        /// to the WinUI canonical curve while seven inline copies stayed on the old one, and
        /// nothing failed. This pins the two together.
        /// </summary>
        [Fact]
        public Task MotionHelperKeySpline_MirrorsPublishedTokenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                try
                {
                    KeySpline published = Assert.IsType<KeySpline>(application.TryFindResource("ControlFastOutSlowInKeySpline"));
                    KeySpline mirrored = MotionHelper.FastOutSlowInKeySpline;

                    Assert.Equal(published.ControlPoint1.X, mirrored.ControlPoint1.X, 0.0001);
                    Assert.Equal(published.ControlPoint1.Y, mirrored.ControlPoint1.Y, 0.0001);
                    Assert.Equal(published.ControlPoint2.X, mirrored.ControlPoint2.X, 0.0001);
                    Assert.Equal(published.ControlPoint2.Y, mirrored.ControlPoint2.Y, 0.0001);
                }
                finally
                {
                    application.Resources.MergedDictionaries.Clear();
                    application.Resources.Clear();
                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                }
            });
        }

        [Fact]
        public Task TextBlockExtensions_Typography_UsesNamedTextBlockStyleResourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                try
                {
                    System.Windows.Controls.TextBlock textBlock = new();
                    textBlock.SetTypography(FluentTypography.BodyLarge);

                    Assert.Same(
                        application.TryFindResource("BodyLargeTextBlockStyle"),
                        textBlock.Style);
                }
                finally
                {
                    application.Resources.MergedDictionaries.Clear();
                    application.Resources.Clear();
                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                }
            });
        }
    }
}
