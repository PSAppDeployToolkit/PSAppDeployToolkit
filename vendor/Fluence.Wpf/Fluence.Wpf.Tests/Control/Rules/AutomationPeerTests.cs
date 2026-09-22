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
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control.Rules
{
    /// <summary>
    /// Task-A2 and Task-A3 tests: automation peer SetValue implementations reject writes to a
    /// disabled control by throwing <see cref="ElementNotEnabledException"/>, matching
    /// the UIA IValueProvider/IRangeValueProvider contract already honored by
    /// <see cref="RatingControlAutomationPeer"/>; <see cref="DropDownButtonAutomationPeer"/>
    /// reports the correct UIA control type; and <see cref="NumberBox"/> routes value changes to
    /// its automation peer so UIA clients (Narrator) observe the current value instead of a stale
    /// one.
    /// </summary>
    public sealed class AutomationPeerTests : IClassFixture<LightThemeFixture>
    {
        public AutomationPeerTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // Spy automation peer that records whether NumberBox routed a Value change through
        // RaiseValueChanged, so the OnValueChanged wiring can be verified without standing up a
        // real UIA client listener.
        private sealed class NumberBoxValueChangedSpyPeer(NumberBox owner) : NumberBoxAutomationPeer(owner)
        {
            public int RaiseValueChangedCallCount { get; private set; }

            public double LastOldValue { get; private set; }

            public double LastNewValue { get; private set; }

            internal override void RaiseValueChanged(double oldValue, double newValue)
            {
                RaiseValueChangedCallCount++;
                LastOldValue = oldValue;
                LastNewValue = newValue;
                base.RaiseValueChanged(oldValue, newValue);
            }
        }

        // Installs the spy peer above in place of the real NumberBoxAutomationPeer.
        private sealed class NumberBoxWithSpyPeer : NumberBox
        {
            public NumberBoxValueChangedSpyPeer? SpyPeer { get; private set; }

            protected override AutomationPeer OnCreateAutomationPeer()
            {
                SpyPeer = new NumberBoxValueChangedSpyPeer(this);
                return SpyPeer;
            }
        }

        [Fact]
        public Task NumberBox_Disabled_RangeValueProvider_SetValue_ThrowsElementNotEnabledExceptionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NumberBox numberBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = numberBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(numberBox);
                    IRangeValueProvider rangeValueProvider = Assert.IsType<IRangeValueProvider>(peer.GetPattern(PatternInterface.RangeValue), exactMatch: false);

                    _ = Assert.Throws<ElementNotEnabledException>(() => rangeValueProvider.SetValue(5d));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_Disabled_RangeValueProvider_IsReadOnly_ReturnsFalseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NumberBox numberBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = numberBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(numberBox);
                    IRangeValueProvider rangeValueProvider = Assert.IsType<IRangeValueProvider>(peer.GetPattern(PatternInterface.RangeValue), exactMatch: false);

                    Assert.False(rangeValueProvider.IsReadOnly,
                        "A disabled NumberBox has no read-only mode; disabled state is conveyed by IsEnabled, not IsReadOnly.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_Disabled_ValueProvider_SetValue_ThrowsElementNotEnabledExceptionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    AutoSuggestBox autoSuggestBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = autoSuggestBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = autoSuggestBox.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(autoSuggestBox);
                    IValueProvider valueProvider = Assert.IsType<IValueProvider>(peer.GetPattern(PatternInterface.Value), exactMatch: false);

                    _ = Assert.Throws<ElementNotEnabledException>(() => valueProvider.SetValue("hello"));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_Disabled_ValueProvider_IsReadOnly_ReturnsFalseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    AutoSuggestBox autoSuggestBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = autoSuggestBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = autoSuggestBox.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(autoSuggestBox);
                    IValueProvider valueProvider = Assert.IsType<IValueProvider>(peer.GetPattern(PatternInterface.Value), exactMatch: false);

                    Assert.False(valueProvider.IsReadOnly,
                        "A disabled AutoSuggestBox has no read-only mode; disabled state is conveyed by IsEnabled, not IsReadOnly.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DropDownButton_AutomationPeer_ReportsButtonControlTypeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    DropDownButton dropDownButton = new() { Content = "Open", Width = 120, Height = 32 };
                    window.Content = dropDownButton;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    _ = dropDownButton.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(dropDownButton);
                    Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_ValueChanged_RaisesAutomationPeerValueChangedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    NumberBoxWithSpyPeer numberBox = new()
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Value = 10,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = numberBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(numberBox);
                    _ = Assert.IsType<NumberBoxValueChangedSpyPeer>(peer, exactMatch: false);

                    numberBox.Value = 42;

                    Assert.Equal(1, numberBox.SpyPeer!.RaiseValueChangedCallCount);
                    Assert.Equal(10d, numberBox.SpyPeer.LastOldValue);
                    Assert.Equal(42d, numberBox.SpyPeer.LastNewValue);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        /// <summary>
        /// TitleBar is the shell surface of every Fluence app and reported as a bare
        /// FrameworkElement to a screen reader until it has a peer of its own.
        /// </summary>
        [Fact]
        public Task TitleBar_AutomationPeer_ReportsTitleBarControlTypeAndTitleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TitleBar titleBar = new() { Title = "Fluence Gallery" };
                AutomationPeer peer = Assert.IsType<AutomationPeer>(
                    UIElementAutomationPeer.CreatePeerForElement(titleBar), exactMatch: false);

                _ = Assert.IsType<TitleBarAutomationPeer>(peer, exactMatch: false);
                Assert.Equal("TitleBar", peer.GetClassName(), StringComparer.Ordinal);
                Assert.Equal(AutomationControlType.TitleBar, peer.GetAutomationControlType());
                Assert.Equal("Fluence Gallery", peer.GetName(), StringComparer.Ordinal);
            });
        }

        /// <summary>
        /// An explicit AutomationProperties.Name wins over the Title, matching WinUI's own
        /// TitleBarAutomationPeer.
        /// </summary>
        [Fact]
        public Task TitleBar_AutomationPeer_PrefersExplicitAutomationNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TitleBar titleBar = new() { Title = "Fluence Gallery" };
                AutomationProperties.SetName(titleBar, "Application title bar");

                AutomationPeer peer = Assert.IsType<AutomationPeer>(
                    UIElementAutomationPeer.CreatePeerForElement(titleBar), exactMatch: false);
                Assert.Equal("Application title bar", peer.GetName(), StringComparer.Ordinal);
            });
        }

        /// <summary>
        /// InfoBadge's count has no other accessible representation: the number lives in a
        /// template TextBlock with no name, so without a peer a screen reader reads nothing.
        /// A Value of -1 is the dot form and carries no number to announce.
        /// </summary>
        /// <param name="value">The badge value to set.</param>
        /// <param name="expectedName">The name the peer must report.</param>
        [Theory]
        [InlineData(5, "5")]
        [InlineData(0, "0")]
        [InlineData(-1, "")]
        public Task InfoBadge_AutomationPeer_ReportsValueAsNameAsync(int value, string expectedName)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                InfoBadge badge = new() { Value = value };
                AutomationPeer peer = Assert.IsType<AutomationPeer>(
                    UIElementAutomationPeer.CreatePeerForElement(badge), exactMatch: false);

                _ = Assert.IsType<InfoBadgeAutomationPeer>(peer, exactMatch: false);
                Assert.Equal("InfoBadge", peer.GetClassName(), StringComparer.Ordinal);
                Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
                Assert.Equal(expectedName, peer.GetName(), StringComparer.Ordinal);
            });
        }

        /// <summary>
        /// FlyoutPresenter is the container every flyout renders into, so one peer gives the
        /// whole family a control type instead of a bare FrameworkElement.
        /// </summary>
        [Fact]
        public Task FlyoutPresenter_AutomationPeer_ReportsGroupControlTypeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FlyoutPresenter presenter = new() { Content = "Body" };
                Window window = new() { Content = presenter, Width = 200, Height = 200 };
                window.Show();
                _ = presenter.ApplyTemplate();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                try
                {
                    AutomationPeer peer = Assert.IsType<AutomationPeer>(
                        UIElementAutomationPeer.CreatePeerForElement(presenter), exactMatch: false);

                    _ = Assert.IsType<FlyoutPresenterAutomationPeer>(peer, exactMatch: false);
                    Assert.Equal("FlyoutPresenter", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                    Assert.True(peer.IsControlElement(), "A flyout container must stay in the control view.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // The action providers (Invoke, Toggle, Expand, Collapse, Select) carry the same contract as
        // SetValue: a disabled control refuses with ElementNotEnabledException. Input never reaches a
        // disabled control, but a UIA client on the same desktop calls these methods directly, so
        // without the guard a host's IsEnabled gate on an action could be walked around from another
        // process, with no focus and no visual cue.

        [Fact]
        public Task SplitButton_Disabled_InvokeAndExpandCollapse_ThrowElementNotEnabledExceptionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                int clicks = 0;
                SplitButton button = new() { Content = "Run", IsEnabled = false };
                button.Click += (_, _) => clicks++;
                Window window = new() { Content = button, Width = 200, Height = 100 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    SplitButtonAutomationPeer peer = Assert.IsType<SplitButtonAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(button));
                    IInvokeProvider invoke = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
                    IExpandCollapseProvider expand = Assert.IsType<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse), exactMatch: false);

                    _ = Assert.Throws<ElementNotEnabledException>(invoke.Invoke);
                    _ = Assert.Throws<ElementNotEnabledException>(expand.Expand);
                    _ = Assert.Throws<ElementNotEnabledException>(expand.Collapse);

                    Assert.Equal(0, clicks);
                    Assert.False(button.IsFlyoutOpen);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ToggleSplitButton_Disabled_ToggleAndExpandCollapse_ThrowElementNotEnabledExceptionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ToggleSplitButton button = new() { Content = "Bold", IsEnabled = false };
                Window window = new() { Content = button, Width = 200, Height = 100 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ToggleSplitButtonAutomationPeer peer = Assert.IsType<ToggleSplitButtonAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(button));
                    IToggleProvider toggle = Assert.IsType<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle), exactMatch: false);
                    IExpandCollapseProvider expand = Assert.IsType<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse), exactMatch: false);

                    _ = Assert.Throws<ElementNotEnabledException>(toggle.Toggle);
                    _ = Assert.Throws<ElementNotEnabledException>(expand.Expand);
                    _ = Assert.Throws<ElementNotEnabledException>(expand.Collapse);

                    Assert.False(button.IsChecked);
                    Assert.False(button.IsFlyoutOpen);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_Disabled_Toggle_ThrowsElementNotEnabledExceptionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ToggleSwitch toggleSwitch = new() { IsEnabled = false };
                Window window = new() { Content = toggleSwitch, Width = 200, Height = 100 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ToggleSwitchAutomationPeer peer = Assert.IsType<ToggleSwitchAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(toggleSwitch));
                    IToggleProvider toggle = Assert.IsType<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle), exactMatch: false);

                    _ = Assert.Throws<ElementNotEnabledException>(toggle.Toggle);
                    Assert.False(toggleSwitch.IsChecked);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DropDownButton_Disabled_ExpandCollapse_ThrowElementNotEnabledExceptionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                DropDownButton button = new() { Content = "More", IsEnabled = false };
                Window window = new() { Content = button, Width = 200, Height = 100 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    DropDownButtonAutomationPeer peer = Assert.IsType<DropDownButtonAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(button));
                    IExpandCollapseProvider expand = Assert.IsType<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse), exactMatch: false);

                    _ = Assert.Throws<ElementNotEnabledException>(expand.Expand);
                    _ = Assert.Throws<ElementNotEnabledException>(expand.Collapse);
                    Assert.False(button.IsChecked);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_Disabled_InvokeAndSelect_ThrowElementNotEnabledExceptionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                NavigationView nav = new();
                NavigationViewItem home = new() { Content = "Home" };
                NavigationViewItem admin = new() { Content = "Admin", IsEnabled = false };
                _ = nav.Items.Add(home);
                _ = nav.Items.Add(admin);
                nav.SelectedItem = home;
                Window window = new() { Content = nav, Width = 400, Height = 300 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    NavigationViewItemAutomationPeer peer = Assert.IsType<NavigationViewItemAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(admin));
                    IInvokeProvider invoke = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
                    ISelectionItemProvider selection = Assert.IsType<ISelectionItemProvider>(peer.GetPattern(PatternInterface.SelectionItem), exactMatch: false);

                    // A disabled entry is the standard way a host greys out a section the current
                    // user may not open; the peer must not open it for them.
                    _ = Assert.Throws<ElementNotEnabledException>(invoke.Invoke);
                    _ = Assert.Throws<ElementNotEnabledException>(selection.Select);
                    _ = Assert.Throws<ElementNotEnabledException>(selection.AddToSelection);
                    _ = Assert.Throws<ElementNotEnabledException>(peer.SelectItem);

                    Assert.Same(home, nav.SelectedItem);
                    Assert.False(admin.IsSelected);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
