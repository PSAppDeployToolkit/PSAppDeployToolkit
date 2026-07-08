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
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

        [TestMethod]
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
                Assert.IsNotNull(thumb, "SwitchThumb Ellipse must exist in ToggleSwitch template.");
                Thumb? input = FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput");
                Assert.IsNotNull(input, "PART_SwitchThumbInput must exist so the switch can support drag gestures.");
                w.Close();
            });
        }

        [TestMethod]
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
                Assert.IsNotNull(thumb, "SwitchThumb must exist.");
                Assert.AreEqual(12.0, thumb.Width, 0.001,
                    "Default knob Width must be 12 (WinUI ToggleSwitch_themeresources.xaml SwitchKnobOff normal state).");
                Assert.AreEqual(12.0, thumb.Height, 0.001,
                    "Default knob Height must be 12.");
                w.Close();
            });
        }

        [TestMethod]
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
                Assert.AreEqual(20.0, tx.X, 0.5,
                    "Knob X translate must be ~20 when IsChecked=True (WinUI ToggleSwitch_themeresources.xaml checked state).");
                w.Close();
            });
        }

        [TestMethod]
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
                Assert.AreEqual(0.0, tx.X, 0.5,
                    "Knob X translate must be 0 when IsChecked=False.");
                w.Close();
            });
        }

        [TestMethod]
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
                Assert.AreEqual(0.0, tx.X, 0.5, "Knob starts on the unchecked side.");

                ts.IsChecked = true;
                Assert.IsTrue(tx.X < 20.0,
                    "Programmatic toggle should start an animation instead of snapping directly to the checked side.");

                WaitForAnimationAndDrain(w.Dispatcher, 250);
                Assert.AreEqual(20.0, tx.X, 0.5, "Knob finishes on the checked side.");
                w.Close();
            });
        }

        [TestMethod]
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
                Assert.IsNotNull(thumb, "SwitchThumb must exist.");
                Thumb? input = FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput");
                Assert.IsNotNull(input, "PART_SwitchThumbInput must exist.");
                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);

                DragStartedEventArgs started = new(0, 0)
                {
                    RoutedEvent = Thumb.DragStartedEvent,
                };
                input.RaiseEvent(started);
                WaitForAnimationAndDrain(w.Dispatcher, 120);
                Assert.AreEqual(17.0, thumb.Width, 0.5, "Pressed/dragged thumb should widen to 17px.");
                Assert.AreEqual(14.0, thumb.Height, 0.5, "Pressed/dragged thumb should expand to 14px high.");

                DragDeltaEventArgs delta = new(20, 0)
                {
                    RoutedEvent = Thumb.DragDeltaEvent,
                };
                input.RaiseEvent(delta);
                Assert.AreEqual(20.0, tx.X, 0.5, "Dragging to the right should move the knob to the checked side.");

                DragCompletedEventArgs completed = new(20, 0, false)
                {
                    RoutedEvent = Thumb.DragCompletedEvent,
                };
                input.RaiseEvent(completed);
                Assert.AreEqual(true, ts.IsChecked, "Completing a right-side drag should commit the checked state.");
                WaitForAnimationAndDrain(w.Dispatcher, 250);
                Assert.AreEqual(20.0, tx.X, 0.5, "Committed drag should leave the knob on the checked side.");
                Assert.AreEqual(12.0, thumb.Width, 0.5, "Released thumb should return to rest width.");
                Assert.AreEqual(12.0, thumb.Height, 0.5, "Released thumb should return to rest height.");

                w.Close();
            });
        }

        [TestMethod]
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
                Assert.IsNotNull(input, "PART_SwitchThumbInput must exist.");

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

                Assert.AreEqual(true, ts.IsChecked,
                    "Releasing a click through capture loss should commit the opposite ToggleSwitch state.");

                WaitForAnimationAndDrain(w.Dispatcher, 250);
                TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                Assert.AreEqual(20.0, tx.X, 0.5, "Clicked switch should finish on the checked side.");

                w.Close();
            });
        }

        [TestMethod]
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
                Assert.IsNotNull(input, "PART_SwitchThumbInput must exist.");
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
                Assert.AreEqual(20.0, tx.X, 0.5, "Dragging to the right should move the knob to the checked side.");

                MouseEventArgs lostCapture = new(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.LostMouseCaptureEvent,
                };
                input.RaiseEvent(lostCapture);

                Assert.AreEqual(true, ts.IsChecked,
                    "Releasing a dragged thumb on the checked side through capture loss should commit the checked state.");

                WaitForAnimationAndDrain(w.Dispatcher, 250);
                Assert.AreEqual(20.0, tx.X, 0.5, "Committed drag should finish on the checked side.");

                w.Close();
            });
        }

        [TestMethod]
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
                    Assert.IsTrue(
                        string.Equals("Airplane mode", peer.GetName(), StringComparison.Ordinal),
                        "ToggleSwitch HeaderContent must be the accessible name when no explicit AutomationProperties.Name is set.");

                    ts.SetValue(AutomationProperties.NameProperty, "Explicit");
                    Assert.IsTrue(
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

        private static TranslateTransform GetToggleSwitchKnobTranslate(ToggleSwitch toggleSwitch)
        {
            FrameworkElement? knob = FindVisualChildByName<FrameworkElement>(toggleSwitch, "SwitchKnob");
            Assert.IsNotNull(knob, "SwitchKnob must exist.");
            TranslateTransform? tx = knob.RenderTransform as TranslateTransform;
            Assert.IsNotNull(tx, "SwitchKnob RenderTransform must be a TranslateTransform.");
            return tx;
        }
    }
}
