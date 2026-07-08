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
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;

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

        [TestMethod]
        public void RatingControl_DefaultStyle_Applies()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new();
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // PART_StarsPanel must be present after template is applied.
                System.Windows.Controls.StackPanel? panel = FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel");
                Assert.IsNotNull(panel, "PART_StarsPanel must be present in RatingControl template.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_DefaultMaxRating_GeneratesFiveStars()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new();
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel? panel = FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel");
                Assert.IsNotNull(panel);
                Assert.AreEqual(5, panel.Children.Count,
                    "Default MaxRating=5 must generate 5 star TextBlocks.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_Value_UpdatesFilledStars()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 3 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel? panel = FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel");
                Assert.IsNotNull(panel);

                // Stars 1-3 must be filled (U+E735), stars 4-5 must be empty (U+E734).
                int filledCount = 0;
                foreach (System.Windows.Controls.TextBlock star in panel.Children)
                {
                    if (string.Equals(star.Text, "\uE735", System.StringComparison.Ordinal))
                    {
                        filledCount++;
                    }
                }

                Assert.AreEqual(3, filledCount,
                    "Value=3 must fill exactly 3 stars with U+E735 (StarFilled).");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_FilledStars_UseAccentBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 2 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel? panel = FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel");
                Assert.IsNotNull(panel);

                SolidColorBrush? accentBrush = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;
                Assert.IsNotNull(accentBrush, "AccentFillColorDefaultBrush must resolve.");

                // First two stars (filled) must use AccentFillColorDefaultBrush.
                System.Windows.Controls.TextBlock? star1 = panel.Children[0] as System.Windows.Controls.TextBlock;
                SolidColorBrush? star1Fg = star1?.Foreground as SolidColorBrush;
                Assert.IsNotNull(star1Fg, "First filled star Foreground must be a SolidColorBrush.");
                Assert.AreEqual(accentBrush.Color, star1Fg.Color,
                    "Filled stars must use AccentFillColorDefaultBrush per WinUI 3 RatingControl.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_EmptyStars_UseSecondaryTextBrush()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 0 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel? panel = FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel");
                Assert.IsNotNull(panel);

                SolidColorBrush? secondaryBrush = app?.TryFindResource("TextFillColorSecondaryBrush") as SolidColorBrush;
                Assert.IsNotNull(secondaryBrush, "TextFillColorSecondaryBrush must resolve.");

                System.Windows.Controls.TextBlock? star = panel.Children[0] as System.Windows.Controls.TextBlock;
                SolidColorBrush? starFg = star?.Foreground as SolidColorBrush;
                Assert.IsNotNull(starFg, "Empty star Foreground must be a SolidColorBrush.");
                Assert.AreEqual(secondaryBrush.Color, starFg.Color,
                    "Empty stars must use TextFillColorSecondaryBrush per WinUI 3 RatingControl.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_Caption_ShowsWhenSet()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 4, Caption = "4.0" };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock? caption = FindVisualChildByName<System.Windows.Controls.TextBlock>(rc, "PART_Caption");
                Assert.IsNotNull(caption, "PART_Caption must be present.");
                Assert.AreEqual(Visibility.Visible, caption.Visibility,
                    "PART_Caption must be Visible when Caption is set.");
                Assert.AreEqual("4.0", caption.Text,
                    "PART_Caption.Text must match the Caption property.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_Caption_CollapsedWhenEmpty()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Caption = string.Empty };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock? caption = FindVisualChildByName<System.Windows.Controls.TextBlock>(rc, "PART_Caption");
                Assert.IsNotNull(caption, "PART_Caption must be present.");
                Assert.AreEqual(Visibility.Collapsed, caption.Visibility,
                    "PART_Caption must be Collapsed when Caption is empty.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_Value_CoercedToMaxRating()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { MaxRating = 3 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Setting Value above MaxRating must clamp it.
                rc.Value = 10.0;
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(3.0, rc.Value,
                    "Value must be coerced to MaxRating when set above MaxRating.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_ThemeCycle_StyleRemainsApplied()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                RatingControl rc = new() { Value = 3 };
                Window w = new() { Content = rc, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.StackPanel? panel = FindVisualChildByName<System.Windows.Controls.StackPanel>(rc, "PART_StarsPanel");
                Assert.IsNotNull(panel,
                    "PART_StarsPanel must still be present after theme cycle.");
                w.Close();
            });
        }

        [TestMethod]
        public void RatingControl_AutomationPeer_ExposesRangeValueAndIsKeyboardSettable()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                RatingControl rating = new() { Value = 2 };
                Window window = new() { Content = rating, Width = 300, Height = 100 };
                window.Show();
                _ = rating.ApplyTemplate();
                DrainDispatcher(window.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(rating);
                Assert.IsInstanceOfType(peer, typeof(Automation.RatingControlAutomationPeer));
                Assert.AreEqual(AutomationControlType.Slider, peer.GetAutomationControlType());

                IRangeValueProvider range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);
                Assert.AreEqual(2.0, range.Value, 0.001);

                // Keyboard: Right arrow raises the rating.
                Assert.IsTrue(rating.Focusable, "RatingControl must be focusable.");
                Assert.IsTrue(rating.IsTabStop, "RatingControl must be a tab stop.");
                _ = rating.Focus();
                DrainDispatcher(window.Dispatcher);

                PresentationSource? source = PresentationSource.FromVisual(rating);
                Assert.IsNotNull(source, "RatingControl must have a presentation source once the window is shown.");
                rating.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
                {
                    RoutedEvent = Keyboard.KeyDownEvent,
                });
                DrainDispatcher(window.Dispatcher);
                Assert.AreEqual(3.0, rating.Value, 0.001, "Right arrow should increase the rating by one.");

                window.Close();
            });
        }

        [TestMethod]
        public void RatingControl_Peer_SetValue_RespectsReadOnlyAndDisabled()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                RatingControl rating = new() { Value = 2 };
                Window window = new() { Content = rating, Width = 300, Height = 100 };
                window.Show();
                _ = rating.ApplyTemplate();
                DrainDispatcher(window.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(rating);
                IRangeValueProvider range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);

                // Read-only: SetValue must throw and leave the value unchanged.
                rating.IsReadOnly = true;
                DrainDispatcher(window.Dispatcher);
                Assert.IsTrue(range.IsReadOnly, "Peer must report read-only when the control is read-only.");
                _ = Assert.ThrowsExactly<System.InvalidOperationException>(() => range.SetValue(4.0));
                Assert.AreEqual(2.0, rating.Value, 0.001, "A read-only control's value must not change via UIA.");

                // Disabled: SetValue must throw ElementNotEnabledException.
                rating.IsReadOnly = false;
                rating.IsEnabled = false;
                DrainDispatcher(window.Dispatcher);
                _ = Assert.ThrowsExactly<System.Windows.Automation.ElementNotEnabledException>(() => range.SetValue(4.0));
                Assert.AreEqual(2.0, rating.Value, 0.001, "A disabled control's value must not change via UIA.");

                // Enabled and writable: SetValue applies.
                rating.IsEnabled = true;
                DrainDispatcher(window.Dispatcher);
                range.SetValue(4.0);
                Assert.AreEqual(4.0, rating.Value, 0.001, "An enabled, writable control should accept the new value.");

                window.Close();
            });
        }
    }
}
