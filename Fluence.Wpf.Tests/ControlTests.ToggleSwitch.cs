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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B12 tests: ToggleSwitch knob easing (SplineDoubleKeyFrame / ControlFastOutSlowIn).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B12  ToggleSwitch knob easing
        // ---------------------------------------------------------------------------

        [Fact]
        public void ToggleSwitch_StyleApplies_SwitchThumbFound()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new();
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.NotNull(thumb);
                Thumb? input = FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput");
                Assert.NotNull(input);
                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_DefaultState_ThumbWidth12()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.NotNull(thumb);
                Assert.Equal(12.0, thumb.Width, 0.001);
                Assert.Equal(12.0, thumb.Height, 0.001);

                ScaleTransform scale = GetToggleSwitchThumbScale(ts);
                Assert.Equal(1.0, scale.ScaleX, 0.001);
                Assert.Equal(1.0, scale.ScaleY, 0.001);
                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_Checked_ThumbTranslateIs20()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = true };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                Assert.Equal(20.0, tx.X, 0.5);
                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_Unchecked_ThumbTranslateIsZero()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                Assert.Equal(0.0, tx.X, 0.5);
                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_ProgrammaticToggle_AnimatesKnobToCheckedSide()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                Assert.Equal(0.0, tx.X, 0.5);

                ts.IsChecked = true;
                Assert.True(tx.X < 20.0,
                    "Programmatic toggle should start an animation instead of snapping directly to the checked side.");

                WaitForAnimationAndDrain(w.Dispatcher, 250);
                Assert.Equal(20.0, tx.X, 0.5);
                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_DragInput_ExpandsThumbAndCommitsCheckedState()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.NotNull(thumb);
                Thumb? input = FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput");
                Assert.NotNull(input);
                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                ScaleTransform scale = GetToggleSwitchThumbScale(ts);

                DragStartedEventArgs started = new(0, 0)
                {
                    RoutedEvent = Thumb.DragStartedEvent,
                };
                input.RaiseEvent(started);
                WaitForAnimationAndDrain(w.Dispatcher, 120);
                Assert.Equal(17.0 / 12.0, scale.ScaleX, 0.05);
                Assert.Equal(14.0 / 12.0, scale.ScaleY, 0.05);
                Assert.Equal(12.0, thumb.Width, 0.001);
                Assert.Equal(12.0, thumb.Height, 0.001);

                DragDeltaEventArgs delta = new(20, 0)
                {
                    RoutedEvent = Thumb.DragDeltaEvent,
                };
                input.RaiseEvent(delta);
                Assert.Equal(20.0, tx.X, 0.5);

                DragCompletedEventArgs completed = new(20, 0, false)
                {
                    RoutedEvent = Thumb.DragCompletedEvent,
                };
                input.RaiseEvent(completed);
                Assert.Equal(true, ts.IsChecked);
                WaitForAnimationAndDrain(w.Dispatcher, 250);
                Assert.Equal(20.0, tx.X, 0.5);
                Assert.Equal(1.0, scale.ScaleX, 0.05);
                Assert.Equal(1.0, scale.ScaleY, 0.05);
                Assert.Equal(12.0, thumb.Width, 0.001);

                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_PressedCodePath_ScalesThumbWithoutLayoutSizeChange()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.NotNull(thumb);
                Thumb? input = FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput");
                Assert.NotNull(input);
                ScaleTransform scale = GetToggleSwitchThumbScale(ts);
                Assert.Equal(1.0, scale.ScaleX, 0.001);

                MouseButtonEventArgs pressed = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                };
                input.RaiseEvent(pressed);
                WaitForAnimationAndDrain(w.Dispatcher, 120);

                Assert.Equal(17.0 / 12.0, scale.ScaleX, 0.05);
                Assert.Equal(14.0 / 12.0, scale.ScaleY, 0.05);
                Assert.Equal(12.0, thumb.Width, 0.001);
                Assert.Equal(12.0, thumb.Height, 0.001);

                MouseEventArgs lostCapture = new(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.LostMouseCaptureEvent,
                };
                input.RaiseEvent(lostCapture);
                WaitForAnimationAndDrain(w.Dispatcher, 250);
                Assert.Equal(1.0, scale.ScaleX, 0.05);

                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_ClickReleaseThroughCaptureLoss_CommitsCheckedState()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Thumb? input = FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput");
                Assert.NotNull(input);

                MouseButtonEventArgs pressed = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                };
                input.RaiseEvent(pressed);

                MouseEventArgs lostCapture = new(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.LostMouseCaptureEvent,
                };
                input.RaiseEvent(lostCapture);

                Assert.Equal(true, ts.IsChecked);

                WaitForAnimationAndDrain(w.Dispatcher, 250);
                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                Assert.Equal(20.0, tx.X, 0.5);

                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_DragReleaseThroughCaptureLoss_CommitsNearestState()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Thumb? input = FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput");
                Assert.NotNull(input);
                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);

                DragStartedEventArgs started = new(0, 0)
                {
                    RoutedEvent = Thumb.DragStartedEvent,
                };
                input.RaiseEvent(started);

                DragDeltaEventArgs delta = new(20, 0)
                {
                    RoutedEvent = Thumb.DragDeltaEvent,
                };
                input.RaiseEvent(delta);
                Assert.Equal(20.0, tx.X, 0.5);

                MouseEventArgs lostCapture = new(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.LostMouseCaptureEvent,
                };
                input.RaiseEvent(lostCapture);

                Assert.Equal(true, ts.IsChecked);

                WaitForAnimationAndDrain(w.Dispatcher, 250);
                Assert.Equal(20.0, tx.X, 0.5);

                w.Close();
            });
        }

        [Fact]
        public void ToggleSwitch_HeaderContent_BecomesAccessibleName()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    ToggleSwitch ts = new() { HeaderContent = "Airplane mode" };
                    window.Content = ts;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(ts);
                    Assert.True(
                        string.Equals("Airplane mode", peer.GetName(), StringComparison.Ordinal),
                        "ToggleSwitch HeaderContent must be the accessible name when no explicit AutomationProperties.Name is set.");

                    ts.SetValue(AutomationProperties.NameProperty, "Explicit");
                    Assert.True(
                        string.Equals("Explicit", peer.GetName(), StringComparison.Ordinal),
                        "Explicit AutomationProperties.Name must win over HeaderContent.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        private static ScaleTransform GetToggleSwitchThumbScale(ToggleSwitch toggleSwitch)
        {
            Ellipse? thumb = FindVisualChildByName<Ellipse>(toggleSwitch, "SwitchThumb");
            Assert.NotNull(thumb);
            ScaleTransform? scale = thumb.RenderTransform as ScaleTransform;
            Assert.NotNull(scale);
            return scale;
        }

        private static TranslateTransform GetToggleSwitchKnobTranslate(ToggleSwitch toggleSwitch)
        {
            FrameworkElement? knob = FindVisualChildByName<FrameworkElement>(toggleSwitch, "SwitchKnob");
            Assert.NotNull(knob);
            TranslateTransform? tx = knob.RenderTransform as TranslateTransform;
            Assert.NotNull(tx);
            return tx;
        }
    }
}
