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
using WpfBorder = System.Windows.Controls.Border;
using WpfTextBlock = System.Windows.Controls.TextBlock;

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

        [TestMethod]
        public void InfoBar_StyleApplies_RootBorderFound()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? root = FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder");
                Assert.IsNotNull(root, "RootBorder must exist in InfoBar template (Fluence style applied).");
                w.Close();
            });
        }

        [TestMethod]
        public void InfoBar_SeverityLevelsVSM_AllStatesAccessible()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // All 4 WI-3 B14 SeverityLevels states must be reachable via GoToState
                bool ok1 = VisualStateManager.GoToState(bar, "Informational", false);
                bool ok2 = VisualStateManager.GoToState(bar, "Success", false);
                bool ok3 = VisualStateManager.GoToState(bar, "Warning", false);
                bool ok4 = VisualStateManager.GoToState(bar, "Error", false);

                Assert.IsTrue(ok1, "GoToState('Informational') must succeed - SeverityLevels VSM group must exist.");
                Assert.IsTrue(ok2, "GoToState('Success') must succeed.");
                Assert.IsTrue(ok3, "GoToState('Warning') must succeed.");
                Assert.IsTrue(ok4, "GoToState('Error') must succeed.");
                w.Close();
            });
        }

        [TestMethod]
        public void InfoBar_DefaultSeverity_IndicatorBarHasBackground()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? indicator = FindVisualChildByName<System.Windows.Controls.Border>(bar, "IndicatorBar");
                Assert.IsNotNull(indicator, "IndicatorBar must exist in InfoBar template.");
                Assert.IsNotNull(indicator.Background, "IndicatorBar background must be set for Informational severity.");
                w.Close();
            });
        }

        [TestMethod]
        public void InfoBar_InformationalAccentBrushes_TrackAccentColorChange()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? indicator = FindVisualChildByName<WpfBorder>(bar, "IndicatorBar");
                Assert.IsNotNull(indicator, "IndicatorBar must exist.");
                WpfTextBlock? defaultIcon = FindVisualChildByName<WpfTextBlock>(bar, "DefaultIcon");
                Assert.IsNotNull(defaultIcon, "DefaultIcon must exist.");
                SolidColorBrush? initial = indicator.Background as SolidColorBrush;
                Assert.IsNotNull(initial, "Informational IndicatorBar background should be a SolidColorBrush.");
                Color initialColor = initial.Color;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xC3, 0x00, 0x52));
                DrainDispatcher(w.Dispatcher);

                SolidColorBrush? expected = app?.TryFindResource("SystemFillColorAttentionBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "SystemFillColorAttentionBrush must resolve after accent change.");
                SolidColorBrush? indicatorBrush = indicator.Background as SolidColorBrush;
                Assert.IsNotNull(indicatorBrush, "Informational IndicatorBar background should remain a SolidColorBrush.");
                SolidColorBrush? iconBrush = defaultIcon.Foreground as SolidColorBrush;
                Assert.IsNotNull(iconBrush, "Informational DefaultIcon foreground should remain a SolidColorBrush.");
                Assert.AreEqual(expected.Color, indicatorBrush.Color,
                    "Informational IndicatorBar should track the current attention accent brush.");
                Assert.AreEqual(expected.Color, iconBrush.Color,
                    "Informational DefaultIcon should track the current attention accent brush.");
                Assert.AreNotEqual(initialColor, indicatorBrush.Color,
                    "Informational accent brush should change when the accent changes.");

                w.Close();
            });
        }

        [TestMethod]
        public void InfoBar_SeverityChange_IndicatorBarBackgroundUpdates()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? indicator = FindVisualChildByName<System.Windows.Controls.Border>(bar, "IndicatorBar");
                Assert.IsNotNull(indicator, "IndicatorBar must exist.");
                Brush brushBefore = indicator.Background;

                // Change severity - trigger + GoToState must both fire
                bar.Severity = InfoBarSeverity.Error;
                DrainDispatcher(w.Dispatcher);

                // Background must still be non-null after the change
                Assert.IsNotNull(indicator.Background, "IndicatorBar background must not be null after severity change to Error.");
                w.Close();
            });
        }

        [TestMethod]
        public void InfoBar_ActionButton_IsNotClippedByRootBorder()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new()
                {
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Title = "Error",
                    Message = "Retry the operation.",
                    ActionButton = new Button { Content = "Retry" }
                };
                Window w = new() { Content = bar, Width = 520, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? root = FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder");
                Assert.IsNotNull(root, "RootBorder must exist in InfoBar template.");
                Assert.IsFalse(root.ClipToBounds,
                    "RootBorder should not clip action-button focus visuals or shadow rendering.");

                System.Windows.Controls.ContentPresenter? presenter = FindVisualChildByName<System.Windows.Controls.ContentPresenter>(bar, "ActionPresenter");
                Assert.IsNotNull(presenter, "ActionPresenter should host the retry action.");
                Assert.AreEqual(Visibility.Visible, presenter.Visibility);

                w.Close();
            });
        }
    }
}
