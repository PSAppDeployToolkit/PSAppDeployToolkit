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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-6 tests: Fluent <see cref="RatingControl"/>.
    /// Authority: WinUI 3 RatingControl_themeresources.xaml + RatingControl.xaml.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-6  RatingControl
        // ---------------------------------------------------------------------------

        [Fact]
        public Task RatingControl_DefaultStyle_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new();
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // PART_StarsPanel must be present after template is applied.
                System.Windows.Controls.StackPanel panel = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel"));
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_DefaultMaxRating_GeneratesFiveStarsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new();
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel panel = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel"));
                Assert.Equal(5, panel.Children.Count);
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_Value_UpdatesFilledStarsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 3 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel panel = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel"));

                // Stars 1-3 must be filled (U+E735), stars 4-5 must be empty (U+E734).
                int filledCount = 0;
                foreach (System.Windows.Controls.TextBlock star in panel.Children)
                {
                    if (string.Equals(star.Text, "\uE735", StringComparison.Ordinal))
                    {
                        filledCount++;
                    }
                }

                Assert.Equal(3, filledCount);
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_FilledStars_UseAccentBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 2 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel panel = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel"));

                SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(app?.TryFindResource("AccentFillColorDefaultBrush"));

                // First two stars (filled) must use AccentFillColorDefaultBrush.
                System.Windows.Controls.TextBlock? star1 = panel.Children[0] as System.Windows.Controls.TextBlock;
                SolidColorBrush star1Fg = Assert.IsType<SolidColorBrush>(star1?.Foreground);
                Assert.Equal(accentBrush.Color, star1Fg.Color);
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_EmptyStars_UseSecondaryTextBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 0 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel panel = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel"));

                SolidColorBrush secondaryBrush = Assert.IsType<SolidColorBrush>(app?.TryFindResource("TextFillColorSecondaryBrush"));

                System.Windows.Controls.TextBlock? star = panel.Children[0] as System.Windows.Controls.TextBlock;
                SolidColorBrush starFg = Assert.IsType<SolidColorBrush>(star?.Foreground);
                Assert.Equal(secondaryBrush.Color, starFg.Color);
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_Caption_ShowsWhenSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 4, Caption = "4.0" };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock caption = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(rc, "PART_Caption"));
                Assert.Equal(Visibility.Visible, caption.Visibility);
                Assert.Equal("4.0", caption.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_Caption_CollapsedWhenEmptyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Caption = string.Empty };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock caption = Assert.IsAssignableFrom<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(rc, "PART_Caption"));
                Assert.Equal(Visibility.Collapsed, caption.Visibility);
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_Value_CoercedToMaxRatingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { MaxRating = 3 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Setting Value above MaxRating must clamp it.
                rc.Value = 10.0;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(3.0, rc.Value);
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_ThemeCycle_StyleRemainsAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 3 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel panel = Assert.IsAssignableFrom<System.Windows.Controls.StackPanel>(FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel"));
                w.Close();
            });
        }

        [Fact]
        public Task RatingControl_AutomationPeer_ExposesRangeValueAndIsKeyboardSettableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(application);
                RatingControl rating = new() { Value = 2 };
                Window window = new() { Content = rating, Width = 300, Height = 100 };
                window.Show();
                _ = rating.ApplyTemplate();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(rating);
                _ = Assert.IsAssignableFrom<Automation.RatingControlAutomationPeer>(peer);
                Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());

                IRangeValueProvider range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);
                Assert.Equal(2.0, range.Value, 0.001);

                // Keyboard: Right arrow raises the rating.
                Assert.True(rating.Focusable, "RatingControl must be focusable.");
                Assert.True(rating.IsTabStop, "RatingControl must be a tab stop.");
                _ = rating.Focus();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                PresentationSource source = Assert.IsAssignableFrom<PresentationSource>(PresentationSource.FromVisual(rating));
                rating.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
                {
                    RoutedEvent = Keyboard.KeyDownEvent,
                });
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal(3.0, rating.Value, 0.001);

                window.Close();
            });
        }

        [Fact]
        public Task RatingControl_Peer_SetValue_RespectsReadOnlyAndDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(application);
                RatingControl rating = new() { Value = 2 };
                Window window = new() { Content = rating, Width = 300, Height = 100 };
                window.Show();
                _ = rating.ApplyTemplate();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(rating);
                IRangeValueProvider range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);

                // Read-only: SetValue must throw and leave the value unchanged.
                rating.IsReadOnly = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(range.IsReadOnly, "Peer must report read-only when the control is read-only.");
                _ = Assert.Throws<InvalidOperationException>(() => range.SetValue(4.0));
                Assert.Equal(2.0, rating.Value, 0.001);

                // Disabled: SetValue must throw ElementNotEnabledException.
                rating.IsReadOnly = false;
                rating.IsEnabled = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                _ = Assert.Throws<System.Windows.Automation.ElementNotEnabledException>(() => range.SetValue(4.0));
                Assert.Equal(2.0, rating.Value, 0.001);

                // Enabled and writable: SetValue applies.
                rating.IsEnabled = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                range.SetValue(4.0);
                Assert.Equal(4.0, rating.Value, 0.001);

                window.Close();
            });
        }
    }
}
