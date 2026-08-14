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
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public void NumberBox_UpButton_Click_IncrementsValueBySmallChange()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
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
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));

                    // Use the UI Automation peer's IInvokeProvider.Invoke, which calls the
                    // button's protected OnClick() and raises ClickEvent through the proper
                    // channel - equivalent to what a user click does.
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(upButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(6.0, numberBox.Value);
                    Assert.Equal("6", numberBox.Text, StringComparer.Ordinal);
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

        [Fact]
        public void NumberBox_DownButton_Click_DecrementsValueBySmallChange()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
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
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(downButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(4.0, numberBox.Value);
                    Assert.Equal("4", numberBox.Text, StringComparer.Ordinal);
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

        [Fact]
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
                    Controls.NumberBox numberBox = new()
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
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));

                    Assert.Equal(ClickMode.Press, upButton.ClickMode);
                    Assert.Equal(ClickMode.Press, downButton.ClickMode);
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

        [Fact]
        public void NumberBox_SpinButtons_AreNotTabStops()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
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

                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));
                    Assert.False(upButton.IsTabStop,
                        "Inline spin increment button should not become a separate tab stop.");
                    Assert.False(downButton.IsTabStop,
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

        [Fact]
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
                    Controls.NumberBox numberBox = new()
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

                    StackPanel spinPanel = Assert.IsType<StackPanel>(numberBox.Template.FindName("SpinPanel", numberBox));
                    Assert.Equal(0.0, spinPanel.Margin.Left);
                    Assert.Equal(1.0, spinPanel.Margin.Top);
                    Assert.Equal(2.0, spinPanel.Margin.Right);
                    Assert.Equal(1.0, spinPanel.Margin.Bottom);
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

        [Fact]
        public void NumberBox_CompactSpinPanel_ReservesLayoutWhenHidden()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
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
                    StackPanel spinPanel = Assert.IsType<StackPanel>(numberBox.Template.FindName("SpinPanel", numberBox));
                    TextBox textBox = Assert.IsType<TextBox>(numberBox.Template.FindName("PART_TextBox", numberBox));
                    Assert.Equal(Visibility.Visible, spinPanel.Visibility);
                    Assert.Equal(0.0, spinPanel.Opacity);
                    Assert.False(spinPanel.IsHitTestVisible,
                        "Invisible compact spin buttons should not receive pointer input.");

                    double heightBeforeFocus = numberBox.ActualHeight;
                    Assert.True(spinPanel.ActualWidth > 0.0,
                        "Compact mode should reserve the spin-button width to avoid layout shifts.");

                    _ = textBox.Focus();
                    _ = Keyboard.Focus(textBox);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(heightBeforeFocus, numberBox.ActualHeight, 0.1);
                    Assert.Equal(1.0, spinPanel.Opacity);
                    Assert.True(spinPanel.IsHitTestVisible,
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

        [Fact]
        public void NumberBox_DirectValue_ClampsPositiveInfinityToMaximum()
        {
            RunOnStaThread(static () =>
            {
                Controls.NumberBox numberBox = new()
                {
                    Minimum = 0,
                    Maximum = 5,
                    Value = double.PositiveInfinity,
                };

                Assert.Equal(5.0, numberBox.Value);
            });
        }

        [Fact]
        public void NumberBox_DirectValue_NormalizesReversedRangeBeforeClamping()
        {
            RunOnStaThread(static () =>
            {
                Controls.NumberBox numberBox = new()
                {
                    Minimum = 10,
                    Maximum = 0,
                    Value = 12,
                };

                Assert.Equal(10.0, numberBox.Value);
            });
        }

        [Fact]
        public void NumberBox_Click_ClampsToMaximum()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
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
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(upButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.Equal(5.0, numberBox.Value);
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

        [Fact]
        public void NumberBox_Header_BecomesAccessibleName()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new() { Header = "Quantity" };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(numberBox);
                    Assert.True(
                        string.Equals("Quantity", peer.GetName(), StringComparison.Ordinal),
                        "NumberBox Header must be the accessible name when no explicit AutomationProperties.Name is set.");

                    numberBox.SetValue(AutomationProperties.NameProperty, "Explicit");
                    Assert.True(
                        string.Equals("Explicit", peer.GetName(), StringComparison.Ordinal),
                        "Explicit AutomationProperties.Name must win over Header.");
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

        [Fact]
        public void NumberBox_Peer_LargeChange_MatchesControl()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        SmallChange = 1,
                        LargeChange = 10,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(numberBox);
                    IRangeValueProvider range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);

                    Assert.Equal(
                        10.0,
                        range.LargeChange,
                        0.001);
                    Assert.Equal(
                        1.0,
                        range.SmallChange,
                        0.001);
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
