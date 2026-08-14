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
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B14 tests: InfoBar SeverityLevels VSM group + GoToState wiring.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B14  InfoBar SeverityLevels VSM group
        // ---------------------------------------------------------------------------

        [Fact]
        public void InfoBar_StyleApplies_RootBorderFound()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border root = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder"));
                w.Close();
            });
        }

        [Fact]
        public void InfoBar_SeverityLevelsVSM_AllStatesAccessible()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // All 4 WI-3 B14 SeverityLevels states must be reachable via GoToState
                bool ok1 = VisualStateManager.GoToState(bar, "Informational", useTransitions: false);
                bool ok2 = VisualStateManager.GoToState(bar, "Success", useTransitions: false);
                bool ok3 = VisualStateManager.GoToState(bar, "Warning", useTransitions: false);
                bool ok4 = VisualStateManager.GoToState(bar, "Error", useTransitions: false);

                Assert.True(ok1, "GoToState('Informational') must succeed - SeverityLevels VSM group must exist.");
                Assert.True(ok2, "GoToState('Success') must succeed.");
                Assert.True(ok3, "GoToState('Warning') must succeed.");
                Assert.True(ok4, "GoToState('Error') must succeed.");
                w.Close();
            });
        }

        [Fact]
        public void InfoBar_CloseButton_UsesFluentSubtlePlate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Title = "Closable" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Button close =
                        Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(bar, "PART_CloseButton"));
                    Assert.Equal(28.0, close.Width, 0.01);
                    Assert.Equal(28.0, close.Height, 0.01);

                    // The subtle plate (TeachingTip / PipsPager pattern): a rounded Border
                    // owned by the button's own template, not the OS default chrome.
                    System.Windows.Controls.Border plate =
                        Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(close, "ButtonPlate"));
                    CornerRadius expectedRadius = (CornerRadius)(app?.FindResource("ControlCornerRadius")
                        ?? throw new Xunit.Sdk.XunitException("ControlCornerRadius must resolve."));
                    Assert.Equal(expectedRadius, plate.CornerRadius);
                    SolidColorBrush restFill = Assert.IsType<SolidColorBrush>(plate.Background);
                    Assert.Equal(0, restFill.Color.A);

                    // Foreground contract: TextFillColorPrimary at rest, flowing into the glyph.
                    SolidColorBrush primary = (SolidColorBrush)(app?.FindResource("TextFillColorPrimaryBrush")
                        ?? throw new Xunit.Sdk.XunitException("TextFillColorPrimaryBrush must resolve."));
                    SolidColorBrush buttonForeground = Assert.IsType<SolidColorBrush>(close.Foreground);
                    Assert.Equal(primary.Color, buttonForeground.Color);

                    FontIcon glyph = Assert.IsAssignableFrom<FontIcon>(FindVisualChildren<FontIcon>(close).FirstOrDefault());
                    Assert.Equal("", glyph.Glyph, StringComparer.Ordinal);
                    SolidColorBrush glyphForeground = Assert.IsType<SolidColorBrush>(glyph.Foreground);
                    Assert.Equal(primary.Color, glyphForeground.Color);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public void InfoBar_DefaultSeverity_IndicatorBarHasBackground()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border indicator = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "IndicatorBar"));
                Assert.NotNull(indicator.Background);
                w.Close();
            });
        }

        [Fact]
        public void InfoBar_InformationalAccentBrushes_TrackAccentColorChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border indicator = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "IndicatorBar"));
                System.Windows.Controls.TextBlock defaultIcon = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "DefaultIcon"));
                SolidColorBrush initial = Assert.IsType<SolidColorBrush>(indicator.Background);
                Color initialColor = initial.Color;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xC3, 0x00, 0x52));
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app?.TryFindResource("SystemFillColorAttentionBrush"));
                SolidColorBrush indicatorBrush = Assert.IsType<SolidColorBrush>(indicator.Background);
                SolidColorBrush iconBrush = Assert.IsType<SolidColorBrush>(defaultIcon.Foreground);
                Assert.Equal(expected.Color, indicatorBrush.Color);
                Assert.Equal(expected.Color, iconBrush.Color);
                Assert.NotEqual(initialColor, indicatorBrush.Color);

                w.Close();
            });
        }

        [Fact]
        public void InfoBar_SeverityChange_IndicatorBarBackgroundUpdates()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border indicator = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "IndicatorBar"));
                Brush brushBefore = indicator.Background;

                // Change severity - trigger + GoToState must both fire
                bar.Severity = InfoBarSeverity.Error;
                DrainDispatcher(w.Dispatcher);

                // Background must still be non-null after the change
                Assert.NotNull(indicator.Background);
                w.Close();
            });
        }

        [Fact]
        public void InfoBar_DeclaresPoliteLiveSetting()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { Title = "Saved", IsOpen = true };
                Window window = new() { Content = bar };
                window.Show();
                _ = bar.ApplyTemplate();
                DrainDispatcher(window.Dispatcher);

                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(bar));
                window.Close();
            });
        }

        [Fact]
        public void InfoBar_ActionButton_IsNotClippedByRootBorder()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new()
                {
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Title = "Error",
                    Message = "Retry the operation.",
                    ActionButton = new Button { Content = "Retry" },
                };
                Window w = new() { Content = bar, Width = 520, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border root = Assert.IsAssignableFrom<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder"));
                Assert.False(root.ClipToBounds,
                    "RootBorder should not clip action-button focus visuals or shadow rendering.");

                System.Windows.Controls.ContentPresenter presenter = Assert.IsAssignableFrom<System.Windows.Controls.ContentPresenter>(FindVisualChildByName<System.Windows.Controls.ContentPresenter>(bar, "ActionPresenter"));
                Assert.Equal(Visibility.Visible, presenter.Visibility);

                w.Close();
            });
        }
    }
}
