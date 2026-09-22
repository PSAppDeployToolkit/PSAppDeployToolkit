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
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class NumberBoxTests : IClassFixture<LightThemeFixture>
    {
        public NumberBoxTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task NumberBox_UpButton_Click_IncrementsValueBySmallChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        Value = 5,
                        SmallChange = 1,
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));

                    // Use the UI Automation peer's IInvokeProvider.Invoke, which calls the
                    // button's protected OnClick() and raises ClickEvent through the proper
                    // channel - equivalent to what a user click does.
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(upButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(6.0, numberBox.Value);
                    Assert.Equal("6", numberBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_DownButton_Click_DecrementsValueBySmallChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        Value = 5,
                        SmallChange = 1,
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(downButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(4.0, numberBox.Value);
                    Assert.Equal("4", numberBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_SpinButton_UsesClickModePressAsync()
        {
            // Regression: the spin buttons must fire Click immediately on MouseDown so
            // a quick press-release updates the value. With the default ClickMode=Release
            // the internal RepeatButton timer only raises Click after Delay elapses
            // (~250 ms on most systems), which users perceive as "the button is broken."
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        Value = 0,
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
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
                }
            });
        }

        [Fact]
        public Task NumberBox_SpinButtons_AreNotTabStopsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
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
                }
            });
        }

        [Fact]
        public Task NumberBox_SpinPanel_HasWinUiCanonicalMarginAsync()
        {
            // WinUI has no spin panel: NumberBox.xaml places UpSpinButton and DownSpinButton
            // directly in the template grid with Margin="4" and Margin="0,4,4,4", so the pair
            // sits 4px inside the field on every edge and the two buttons touch. Fluence keeps
            // the StackPanel for the compact-mode reveal, so the panel carries no inset of its
            // own and the two buttons carry WinUI's.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();

                    StackPanel spinPanel = Assert.IsType<StackPanel>(numberBox.Template.FindName("SpinPanel", numberBox));
                    Assert.Equal(new Thickness(0), spinPanel.Margin);
                    Assert.Equal(VerticalAlignment.Stretch, spinPanel.VerticalAlignment);

                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));
                    Assert.Equal(new Thickness(4), upButton.Margin);
                    Assert.Equal(new Thickness(0, 4, 4, 4), downButton.Margin);
                    Assert.Equal(VerticalAlignment.Stretch, upButton.VerticalAlignment);
                    Assert.Equal(VerticalAlignment.Stretch, downButton.VerticalAlignment);
                    Assert.Equal(32.0, upButton.MinWidth);
                    Assert.Equal(32.0, downButton.MinWidth);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_CompactSpinPanel_ReservesLayoutWhenHiddenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                        Width = 180,
                    };
                    window.Content = numberBox;
                    window.Width = 260;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(heightBeforeFocus, numberBox.ActualHeight, 0.1);
                    Assert.Equal(1.0, spinPanel.Opacity);
                    Assert.True(spinPanel.IsHitTestVisible,
                        "Visible compact spin buttons should receive pointer input.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_DirectValue_ClampsPositiveInfinityToMaximumAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
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
        public Task NumberBox_DirectValue_NaN_ClearsTheValueAndTheTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // NaN is the cleared state, not out-of-range input, so the bounds do not apply to it
                // and it survives coercion. WinUI reads NaN the same way (NumberBox.cpp:120, :463).
                // What the bounds do have to be protected from is stepping a cleared value, which
                // NumberBox_Click_OnACleared* below covers.
                Controls.NumberBox numberBox = new()
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 42,
                };
                Assert.Equal(42.0, numberBox.Value);

                numberBox.Value = double.NaN;

                Assert.True(double.IsNaN(numberBox.Value));
                Assert.Equal(string.Empty, numberBox.Text);
            });
        }

        [Fact]
        public Task NumberBox_PlaceholderText_ShowsWhileTheValueIsClearedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    // PlaceholderText was a declared property no template read. The cleared state is
                    // what it exists to label, so the template shows it for an empty field.
                    Controls.NumberBox numberBox = new()
                    {
                        PlaceholderText = "Quantity",
                        Value = double.NaN,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    TextBlock placeholder = Assert.IsType<TextBlock>(numberBox.Template.FindName("PlaceholderTextBlock", numberBox));

                    Assert.Equal("Quantity", placeholder.Text);
                    Assert.Equal(Visibility.Visible, placeholder.Visibility);

                    numberBox.Value = 3;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_EmptyText_ClearsTheValueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // An emptied field is the cleared state, so Value follows it to NaN rather than
                // keeping a number the field no longer shows (WinUI NumberBox.cpp:488). Nothing
                // parsed, so TryParseText still reports false.
                Controls.NumberBox numberBox = new()
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 42,
                };
                Assert.Equal("42", numberBox.Text);

                numberBox.Text = string.Empty;

                Assert.False(numberBox.TryParseText());
                Assert.True(double.IsNaN(numberBox.Value));
            });
        }

        [Fact]
        public Task NumberBox_Click_OnAClearedValue_DoesNothingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    // NaN plus SmallChange is NaN, so a cleared box cannot be stepped back into
                    // range. The spin buttons are disabled while it is cleared and the step itself
                    // is guarded, so neither the pointer nor a UIA client can strand the field.
                    Controls.NumberBox numberBox = new()
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Value = double.NaN,
                        SmallChange = 1,
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));

                    Assert.False(upButton.IsEnabled, "A cleared value cannot be stepped up.");
                    Assert.False(downButton.IsEnabled, "A cleared value cannot be stepped down.");

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(upButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    _ = Assert.Throws<ElementNotEnabledException>(invoke.Invoke);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(double.IsNaN(numberBox.Value));

                    numberBox.Value = 7;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(upButton.IsEnabled, "A number can be stepped again.");
                    Assert.True(downButton.IsEnabled, "A number can be stepped again.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_TypedNaN_IsRejectedLikeAnyUnparseableTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // NumberStyles.Any accepts the culture's NaN symbol, so "NaN" parses and used to
                // commit. It is not a number the bounds can place, so it is treated as text that did
                // not parse: TryParseText reports false and Value keeps what it had.
                Controls.NumberBox numberBox = new()
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 42,
                };
                Assert.Equal(42.0, numberBox.Value);

                numberBox.Text = double.NaN.ToString(CultureInfo.CurrentCulture);

                Assert.False(numberBox.TryParseText());
                Assert.Equal(42.0, numberBox.Value);
            });
        }

        [Fact]
        public Task NumberBox_NaNBound_DoesNotSwitchClampingOffAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // Math.Max and Math.Min propagate NaN, so a NaN Minimum used to return NaN from the
                // clamp and the coercion then let every value through unclamped. A NaN bound is no
                // bound on that side, and the other side still holds.
                Controls.NumberBox numberBox = new()
                {
                    Minimum = double.NaN,
                    Maximum = 100,
                    Value = 500,
                };

                Assert.Equal(100.0, numberBox.Value);

                numberBox.Value = -500;
                Assert.Equal(-500.0, numberBox.Value);
            });
        }

        [Fact]
        public Task NumberBox_DirectValue_NormalizesReversedRangeBeforeClampingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
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
        public Task NumberBox_Click_ClampsToMaximumAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        Minimum = 0,
                        Maximum = 5,
                        Value = 5,
                        SmallChange = 1,
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(upButton);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(5.0, numberBox.Value);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_Header_BecomesAccessibleNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new() { Header = "Quantity" };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

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
                }
            });
        }

        [Fact]
        public Task NumberBox_Peer_LargeChange_MatchesControlAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

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
                }
            });
        }

        [Fact]
        public Task NumberBox_DefaultStyle_LoadsPartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.NumberBox numberBox = new() { Width = 160, Value = 3 };
                try
                {
                    window.Content = numberBox;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = numberBox.ApplyTemplate();
                    Assert.NotNull(numberBox.Template.FindName("PART_TextBox", numberBox));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task NumberBox_Value_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.NumberBox box = new() { Value = 42.5 };
                Assert.Equal(42.5, box.Value, 0.001);
            });
        }

        [Fact]
        public Task NumberBox_InlineSpinButtons_AreBorderlessAtRestAsync()
        {
            // WinUI 3 renders inline spin buttons as bare chevrons at rest: no border, no fill.
            // NumberBox_perf2026.xaml remaps RepeatButtonBackground / RepeatButtonBorderBrush to
            // the TextControlButton* tokens (TextBox_themeresources.xaml), where the border is
            // ControlFillColorTransparent in every state and the rest fill is transparent too.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));

                    BrushAssert.AssertBrushColor(upButton.BorderBrush, "ControlFillColorTransparentBrush");
                    BrushAssert.AssertBrushColor(upButton.Background, "SubtleFillColorTransparentBrush");
                    BrushAssert.AssertBrushColor(downButton.BorderBrush, "ControlFillColorTransparentBrush");
                    BrushAssert.AssertBrushColor(downButton.Background, "SubtleFillColorTransparentBrush");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
