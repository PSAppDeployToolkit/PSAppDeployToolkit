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
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void NumberBox_UpButton_Click_IncrementsValueBySmallChange()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Fluent.NumberBox numberBox = new()
                    {
                        Value = 5,
                        SmallChange = 1,
                        SpinButtonPlacementMode = SpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton? upButton = numberBox.Template.FindName("PART_UpButton", numberBox) as RepeatButton;
                    Assert.IsNotNull(upButton, "NumberBox template must expose PART_UpButton.");

                    // Use the UI Automation peer's IInvokeProvider.Invoke, which calls the
                    // button's protected OnClick() and raises ClickEvent through the proper
                    // channel - equivalent to what a user click does.
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(upButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(6.0, numberBox.Value,
                        "PART_UpButton.Click must increment NumberBox.Value by SmallChange.");
                    Assert.AreEqual("6", numberBox.Text,
                        "NumberBox.Text must mirror Value after an increment.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NumberBox_DownButton_Click_DecrementsValueBySmallChange()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Fluent.NumberBox numberBox = new()
                    {
                        Value = 5,
                        SmallChange = 1,
                        SpinButtonPlacementMode = SpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton? downButton = numberBox.Template.FindName("PART_DownButton", numberBox) as RepeatButton;
                    Assert.IsNotNull(downButton, "NumberBox template must expose PART_DownButton.");

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(downButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(4.0, numberBox.Value,
                        "PART_DownButton.Click must decrement NumberBox.Value by SmallChange.");
                    Assert.AreEqual("4", numberBox.Text,
                        "NumberBox.Text must mirror Value after a decrement.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NumberBox_SpinButton_UsesClickModePress()
        {
            // Regression: the spin buttons must fire Click immediately on MouseDown so
            // a quick press-release updates the value. With the default ClickMode=Release
            // the internal RepeatButton timer only raises Click after Delay elapses
            // (~250 ms on most systems), which users perceive as "the button is broken."
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Fluent.NumberBox numberBox = new()
                    {
                        Value = 0,
                        SpinButtonPlacementMode = SpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton? upButton = numberBox.Template.FindName("PART_UpButton", numberBox) as RepeatButton;
                    RepeatButton? downButton = numberBox.Template.FindName("PART_DownButton", numberBox) as RepeatButton;
                    Assert.IsNotNull(upButton);
                    Assert.IsNotNull(downButton);

                    Assert.AreEqual(ClickMode.Press, upButton.ClickMode,
                        "PART_UpButton must use ClickMode=Press so a quick press-release fires Click immediately.");
                    Assert.AreEqual(ClickMode.Press, downButton.ClickMode,
                        "PART_DownButton must use ClickMode=Press so a quick press-release fires Click immediately.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NumberBox_SpinButtons_AreNotTabStops()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Fluent.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = SpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();

                    RepeatButton? upButton = numberBox.Template.FindName("PART_UpButton", numberBox) as RepeatButton;
                    RepeatButton? downButton = numberBox.Template.FindName("PART_DownButton", numberBox) as RepeatButton;
                    Assert.IsNotNull(upButton, "NumberBox template must expose PART_UpButton.");
                    Assert.IsNotNull(downButton, "NumberBox template must expose PART_DownButton.");
                    Assert.IsFalse(upButton.IsTabStop,
                        "Inline spin increment button should not become a separate tab stop.");
                    Assert.IsFalse(downButton.IsTabStop,
                        "Inline spin decrement button should not become a separate tab stop.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NumberBox_SpinPanel_HasWinUiCanonicalMargin()
        {
            // WI-3 A7: WinUI canonical SpinPanel margin is "0,1,2,1" (2px right inset from
            // border edge).  Before this fix Fluence used "0,1,0,1" which butted the buttons
            // flush against the right border of the control.
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Fluent.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = SpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();

                    StackPanel? spinPanel = numberBox.Template.FindName("SpinPanel", numberBox) as StackPanel;
                    Assert.IsNotNull(spinPanel, "NumberBox template must expose SpinPanel.");
                    Assert.AreEqual(0.0, spinPanel.Margin.Left,
                        "SpinPanel.Margin.Left must be 0.");
                    Assert.AreEqual(1.0, spinPanel.Margin.Top,
                        "SpinPanel.Margin.Top must be 1 (WinUI canonical vertical inset).");
                    Assert.AreEqual(2.0, spinPanel.Margin.Right,
                        "SpinPanel.Margin.Right must be 2 (WinUI canonical right inset from border edge).");
                    Assert.AreEqual(1.0, spinPanel.Margin.Bottom,
                        "SpinPanel.Margin.Bottom must be 1 (WinUI canonical vertical inset).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NumberBox_CompactSpinPanel_ReservesLayoutWhenHidden()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Fluent.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = SpinButtonPlacementMode.Compact,
                        Width = 180,
                    };
                    window.Content = numberBox;
                    window.Width = 260;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    StackPanel? spinPanel = numberBox.Template.FindName("SpinPanel", numberBox) as StackPanel;
                    TextBox? textBox = numberBox.Template.FindName("PART_TextBox", numberBox) as TextBox;
                    Assert.IsNotNull(spinPanel, "NumberBox template must expose SpinPanel.");
                    Assert.IsNotNull(textBox, "NumberBox template must expose PART_TextBox.");
                    Assert.AreEqual(Visibility.Visible, spinPanel.Visibility,
                        "Compact mode should reserve spin-panel layout space while the buttons are hidden.");
                    Assert.AreEqual(0.0, spinPanel.Opacity,
                        "Compact mode should hide the reserved spin panel visually before hover or focus.");
                    Assert.IsFalse(spinPanel.IsHitTestVisible,
                        "Invisible compact spin buttons should not receive pointer input.");

                    double heightBeforeFocus = numberBox.ActualHeight;
                    Assert.IsTrue(spinPanel.ActualWidth > 0.0,
                        "Compact mode should reserve the spin-button width to avoid layout shifts.");

                    _ = textBox.Focus();
                    _ = Keyboard.Focus(textBox);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(heightBeforeFocus, numberBox.ActualHeight, 0.1,
                        "Showing compact spin buttons on focus should not change NumberBox height.");
                    Assert.AreEqual(1.0, spinPanel.Opacity,
                        "Compact spin buttons should become visible while the NumberBox has keyboard focus.");
                    Assert.IsTrue(spinPanel.IsHitTestVisible,
                        "Visible compact spin buttons should receive pointer input.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NumberBox_DirectValue_ClampsPositiveInfinityToMaximum()
        {
            RunOnStaThread(static () =>
            {
                Fluent.NumberBox numberBox = new()
                {
                    Minimum = 0,
                    Maximum = 5,
                    Value = double.PositiveInfinity,
                };

                Assert.AreEqual(5.0, numberBox.Value,
                    "Direct Value assignment must use the same maximum clamp as spin-button changes.");
            });
        }

        [TestMethod]
        public void NumberBox_DirectValue_NormalizesReversedRangeBeforeClamping()
        {
            RunOnStaThread(static () =>
            {
                Fluent.NumberBox numberBox = new()
                {
                    Minimum = 10,
                    Maximum = 0,
                    Value = 12,
                };

                Assert.AreEqual(10.0, numberBox.Value,
                    "Direct Value assignment must normalize reversed Minimum/Maximum before clamping.");
            });
        }

        [TestMethod]
        public void NumberBox_Click_ClampsToMaximum()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Fluent.NumberBox numberBox = new()
                    {
                        Minimum = 0,
                        Maximum = 5,
                        Value = 5,
                        SmallChange = 1,
                        SpinButtonPlacementMode = SpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton? upButton = numberBox.Template.FindName("PART_UpButton", numberBox) as RepeatButton;
                    Assert.IsNotNull(upButton);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(upButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(5.0, numberBox.Value,
                        "Up-click at Maximum must clamp Value to Maximum (no overshoot).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
